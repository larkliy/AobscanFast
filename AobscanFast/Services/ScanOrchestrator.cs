using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AobscanFast.Services;

/// <summary>Coordinates memory enumeration, chunking, reads, matching, and cancellation.</summary>
public sealed class ScanOrchestrator
{
    private readonly IMemoryRegionEnumerator _regionEnumerator;
    private readonly IMemoryAccessor _memoryAccessor;
    private readonly IPatternMatcherResolver _patternMatcherResolver;
    private readonly IMemoryRangePlanner _memoryRangePlanner;

    /// <summary>Initializes a scan coordinator with its required services.</summary>
    /// <param name="regionEnumerator">The region enumerator.</param><param name="memoryAccessor">The memory accessor.</param><param name="patternMatcherResolver">The pattern matcher resolver.</param><param name="memoryRangePlanner">The range planner.</param>
    public ScanOrchestrator(
        IMemoryRegionEnumerator regionEnumerator,
        IMemoryAccessor memoryAccessor,
        IPatternMatcherResolver patternMatcherResolver,
        IMemoryRangePlanner memoryRangePlanner)
    {
        _regionEnumerator = regionEnumerator ?? throw new ArgumentNullException(nameof(regionEnumerator));
        _memoryAccessor = memoryAccessor ?? throw new ArgumentNullException(nameof(memoryAccessor));
        _patternMatcherResolver = patternMatcherResolver ?? throw new ArgumentNullException(nameof(patternMatcherResolver));
        _memoryRangePlanner = memoryRangePlanner ?? throw new ArgumentNullException(nameof(memoryRangePlanner));
    }

    /// <summary>Scans memory and returns matching addresses.</summary>
    /// <param name="pattern">The compiled pattern.</param><param name="options">The scan options.</param><param name="ct">A cancellation token.</param><returns>Matching addresses.</returns>
    public List<nint> Scan(AobPattern pattern, AobScanOptions options, CancellationToken ct)
        => ScanCore(pattern, options, options?.MaxResults ?? 0, ct);

    /// <summary>Scans memory and returns the first matching address.</summary>
    /// <param name="pattern">The compiled pattern.</param><param name="options">The scan options.</param><param name="ct">A cancellation token.</param><returns>A matching address, or <see langword="null"/> when none exists.</returns>
    public nint? ScanFirst(AobPattern pattern, AobScanOptions options, CancellationToken ct)
    {
        List<nint> results = ScanCore(pattern, options, 1, ct);
        return results.Count == 0 ? null : results[0];
    }

    private unsafe List<nint> ScanCore(AobPattern pattern, AobScanOptions options, int effectiveMaxResults, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        if (pattern.Length > options.ChunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size. Increase ChunkSize in AobScanOptions.", nameof(pattern));

        var matcher = _patternMatcherResolver.Resolve(pattern);
        using MemoryHandle bytesHandle = pattern.Bytes.Pin();
        using MemoryHandle maskHandle = pattern.Mask.IsEmpty ? default : pattern.Mask.Pin();
        using MemoryHandle searchSequenceHandle = pattern.SearchSequence.IsEmpty ? default : pattern.SearchSequence.Pin();
        MemoryRange[] excludedRanges = CreateExcludedRanges(
            pattern,
            bytesHandle.Pointer,
            maskHandle.Pointer,
            searchSequenceHandle.Pointer);
        var rawRegions = _regionEnumerator.GetRegions(options.MinScanAddress, options.MaxScanAddress, options.MemoryAccess);
        var mergedRegions = _memoryRangePlanner.MergeAdjacentRegions(rawRegions);
        var scanChunks = _memoryRangePlanner.CreateScanChunks(mergedRegions, pattern.Length, options.ChunkSize);

        int initialCapacity = effectiveMaxResults > 0
            ? Math.Min(1024, effectiveMaxResults)
            : 1024;

        var results = new List<nint>(initialCapacity);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Lock syncRoot = new();
        bool internalLimitReached = false;

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = _memoryAccessor is ISelfProcessMemoryAccessor
                ? 1
                : options.MaxDegreeOfParallelism > 0
                ? options.MaxDegreeOfParallelism
                : -1
        };

        try
        {
            Parallel.ForEach(
                scanChunks,
                parallelOptions,
                (chunk, state) =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }

                    var chunkResults = new List<nint>(64);
                    int chunkBufSize = checked((int)chunk.Size);

                    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(chunkBufSize);
                    Span<byte> buffer = rentedBuffer.AsSpan(0, chunkBufSize);
                    nint bufferAddress = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(rentedBuffer));

                    try
                    {
                        _memoryAccessor.ReadMemory(chunk.BaseAddress, buffer, out nuint bytesRead);
                        if (bytesRead > (nuint)buffer.Length)
                            throw new InvalidOperationException("Memory accessor returned more bytes than the supplied buffer can hold.");

                        int validLength = checked((int)bytesRead);
                        if (validLength >= pattern.Length)
                        {
                            var actualRange = new MemoryRange(chunk.BaseAddress, validLength);
                            matcher.ScanChunk(actualRange, pattern, chunkResults, buffer[..validLength]);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(
                            rentedBuffer,
                            clearArray: _memoryAccessor is ISelfProcessMemoryAccessor);
                    }

                    chunkResults.RemoveAll(address => IsRangeOverlap(address, pattern.Length, bufferAddress, rentedBuffer.Length));
                    chunkResults.RemoveAll(address => IsExcluded(address, pattern.Length, excludedRanges));

                    if (chunkResults.Count == 0)
                        return;

                    bool limitReached = false;
                    lock (syncRoot)
                    {
                        if (effectiveMaxResults > 0)
                        {
                            int remaining = effectiveMaxResults - results.Count;
                            if (remaining > 0)
                            {
                                int take = Math.Min(remaining, chunkResults.Count);
                                results.AddRange(chunkResults.GetRange(0, take));
                            }

                            if (results.Count >= effectiveMaxResults)
                            {
                                internalLimitReached = true;
                                limitReached = true;
                            }
                        }
                        else
                        {
                            results.AddRange(chunkResults);
                        }
                    }

                    if (limitReached)
                    {
                        state.Stop();
                        if (!ct.IsCancellationRequested)
                            cts.Cancel();
                    }
                });
        }
        catch (OperationCanceledException) when (internalLimitReached && !ct.IsCancellationRequested)
        {
        }

        ct.ThrowIfCancellationRequested();

        return results;
    }

    private static unsafe MemoryRange[] CreateExcludedRanges(
        AobPattern pattern,
        void* bytesPointer,
        void* maskPointer,
        void* searchSequencePointer)
    {
        var ranges = new List<MemoryRange>(3);
        AddPinnedRange(ranges, bytesPointer, pattern.Bytes.Length);
        AddPinnedRange(ranges, maskPointer, pattern.Mask.Length);
        AddPinnedRange(ranges, searchSequencePointer, pattern.SearchSequence.Length);
        return [.. ranges];
    }

    private static unsafe void AddPinnedRange(List<MemoryRange> ranges, void* pointer, int length)
    {
        if (pointer is not null && length > 0)
            ranges.Add(new MemoryRange((nint)pointer, length));
    }

    private static bool IsExcluded(nint address, int length, MemoryRange[] excludedRanges)
    {
        nint end = checked(address + length);
        foreach (MemoryRange excluded in excludedRanges)
        {
            nint excludedEnd = checked(excluded.BaseAddress + excluded.Size);
            if (address < excludedEnd && end > excluded.BaseAddress)
                return true;
        }

        return false;
    }

    private static bool IsRangeOverlap(nint address, int length, nint rangeAddress, int rangeLength)
    {
        nint end = checked(address + length);
        nint rangeEnd = checked(rangeAddress + rangeLength);
        return address < rangeEnd && end > rangeAddress;
    }

    private static void ValidateOptions(AobScanOptions options)
    {
        if (options.ChunkSize <= 0 || options.ChunkSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(options.ChunkSize), "Chunk size must be between 1 and Int32.MaxValue.");

        if (options.MaxDegreeOfParallelism != -1 && options.MaxDegreeOfParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxDegreeOfParallelism));

        if (options.MaxResults < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxResults));

        if (options.MinScanAddress > options.MaxScanAddress)
            throw new ArgumentException("MinScanAddress cannot exceed MaxScanAddress.", nameof(options));
    }
}
