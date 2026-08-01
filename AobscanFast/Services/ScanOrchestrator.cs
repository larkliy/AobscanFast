using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;

namespace AobscanFast.Services;

public sealed class ScanOrchestrator
{
    private readonly IMemoryRegionEnumerator _regionEnumerator;
    private readonly IMemoryAccessor _memoryAccessor;
    private readonly IPatternMatcherResolver _patternMatcherResolver;
    private readonly IMemoryRangePlanner _memoryRangePlanner;

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

    public List<nint> Scan(AobPattern pattern, AobScanOptions options, CancellationToken ct)
        => ScanCore(pattern, options, options?.MaxResults ?? 0, ct);

    public nint? ScanFirst(AobPattern pattern, AobScanOptions options, CancellationToken ct)
    {
        List<nint> results = ScanCore(pattern, options, 1, ct);
        return results.Count == 0 ? null : results[0];
    }

    private List<nint> ScanCore(AobPattern pattern, AobScanOptions options, int effectiveMaxResults, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        if (pattern.Length > options.ChunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size. Increase ChunkSize in AobScanOptions.", nameof(pattern));

        var matcher = _patternMatcherResolver.Resolve(pattern);
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
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism > 0
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

                    int chunkBufSize = checked((int)chunk.Size);
                    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(chunkBufSize);
                    Span<byte> buffer = rentedBuffer.AsSpan(0, chunkBufSize);
                    var chunkResults = new List<nint>(64);

                    try
                    {
                        bool success = _memoryAccessor.ReadMemory(chunk.BaseAddress, buffer, out nuint bytesRead);
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
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }

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
