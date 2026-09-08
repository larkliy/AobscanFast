using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;
using System.Runtime.InteropServices;
using AobscanFast.Core.Matching;
using System.Diagnostics;

namespace AobscanFast.Services;

/// <summary>Coordinates memory enumeration, chunking, reads, matching, and cancellation.</summary>
public sealed partial class ScanOrchestrator
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
    [LibraryImport("AobscanFastNativeC", EntryPoint = "nativec_scan_solid")]
    private static unsafe partial nint NativeScanSolid(
        byte* buf, nint bufLen,
        byte* patt, nint pattLen,
        nint* outResults, nint maxResults
    );

    private static List<List<MemoryRange>> GroupChanksInfoBatches(List<MemoryRange> chunks, int maxBatchSize = 4 * 1024 * 1024)
    {
        var batches = new List<List<MemoryRange>>();
        var curBatch = new List<MemoryRange>();
        long curBatchSize = 0;

        foreach (var chunk in chunks)
        {
            if (curBatchSize + chunk.Size > maxBatchSize && curBatch.Count > 0)
            {
                batches.Add(curBatch);
                curBatch = new List<MemoryRange>();
                curBatchSize = 0;
            }

            curBatch.Add(chunk);
            curBatchSize += chunk.Size;
        }

        if (curBatch.Count > 0) batches.Add(curBatch);
        return batches;
    }

    private unsafe List<nint> ScanCore(AobPattern pattern, AobScanOptions options, int effectiveMaxResults, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        if (pattern.Length > options.ChunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size. Increase ChunkSize in AobScanOptions.", nameof(pattern));

        var swTotal = Stopwatch.StartNew();
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
        
        const int selfBatchBytes = 4 * 1024 * 1024;
        var chunkBatches = GroupChanksInfoBatches(scanChunks, selfBatchBytes);

        var results = new List<nint>(effectiveMaxResults > 0 ? Math.Min(1024, effectiveMaxResults) : 1024);
        
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(selfBatchBytes + 1024);

        try
        {
            fixed (byte* bufferPointer = rentedBuffer)
            {
                fixed (byte* pPattern = pattern.BytesSpan)
                {
                    foreach (var batch in chunkBatches)
                    {
                        if (ct.IsCancellationRequested) break;

                        var effectiveChunks = new List<(nint EffectiveBase, int Size, int OffsetInBuffer)>(batch.Count);
                        int batchTotalSize = 0;
                        nint prevChunkEnd = nint.MinValue;

                        foreach (var chunk in batch)
                        {
                            int chunkSize = checked((int)chunk.Size);
                            nint chunkEnd = chunk.BaseAddress + chunkSize;
                            nint effectiveBase = chunk.BaseAddress;
                            int effectiveSize = chunkSize;

                            if (chunk.BaseAddress < prevChunkEnd)
                            {
                                nint skipBytes = prevChunkEnd - chunk.BaseAddress;
                                effectiveBase = chunk.BaseAddress + skipBytes;
                                effectiveSize = chunkSize - (int)skipBytes;
                            }

                            if (effectiveSize > 0)
                            {
                                effectiveChunks.Add((effectiveBase, effectiveSize, batchTotalSize));
                                batchTotalSize += effectiveSize;
                            }
                            prevChunkEnd = chunkEnd;
                        }

                        if (batchTotalSize == 0) continue;

                        Span<byte> bufferSpan = rentedBuffer.AsSpan(0, batchTotalSize);
                        foreach (var ec in effectiveChunks)
                        {
                            var subSpan = bufferSpan.Slice(ec.OffsetInBuffer, ec.Size);
                            _memoryAccessor.ReadMemory(ec.EffectiveBase, subSpan, out _);
                        }

                        int localLimit = 1024;
                        nint* localOffsets = stackalloc nint[localLimit];

                        nint foundInBatch = NativeScanSolid(
                            bufferPointer, batchTotalSize,
                            pPattern, pattern.Length,
                            localOffsets, localLimit
                        );

                        if (foundInBatch == 0) continue;

                        int chunkIndex = 0;
                        nint bufferAddress = (nint)bufferPointer;

                        for (int i = 0; i < foundInBatch; i++)
                        {
                            nint offsetInBatch = localOffsets[i];

                            while (chunkIndex < effectiveChunks.Count && 
                                offsetInBatch >= effectiveChunks[chunkIndex].OffsetInBuffer + effectiveChunks[chunkIndex].Size)
                            {
                                chunkIndex++;
                            }

                            if (chunkIndex < effectiveChunks.Count)
                            {
                                var ec = effectiveChunks[chunkIndex];
                                nint realAddr = ec.EffectiveBase + (offsetInBatch - ec.OffsetInBuffer);

                                if (!IsRangeOverlap(realAddr, pattern.Length, bufferAddress, rentedBuffer.Length) &&
                                    !IsExcluded(realAddr, pattern.Length, excludedRanges))
                                {
                                    results.Add(realAddr);

                                    if (effectiveMaxResults > 0 && results.Count >= effectiveMaxResults)
                                    {
                                        goto LimitReached; // Быстрый выход из всех циклов
                                    }
                                }
                            }
                        }
                    }
                }
            }

        LimitReached:;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: false);
        }

        ct.ThrowIfCancellationRequested();

        if (results.Count > 1)
        {
            results.Sort();
            int uniqueCount = 1;
            for (int i = 1; i < results.Count; i++)
            {
                if (results[i] != results[uniqueCount - 1])
                    results[uniqueCount++] = results[i];
            }
            results.RemoveRange(uniqueCount, results.Count - uniqueCount);
        }
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

    private static bool IsRangeOverlap/*Overlarp*/(nint address, int length, nint rangeAddress, int rangeLength)
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
