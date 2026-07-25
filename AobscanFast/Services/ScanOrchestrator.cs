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
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(options);

        if (pattern.Length == 0)
            throw new ArgumentException("Pattern must contain at least one byte.", nameof(pattern));

        nint chunkSize = options.ChunkSize > 0 ? options.ChunkSize : 256 * 1024;

        if (pattern.Length >= chunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size. Increase ChunkSize in AobScanOptions.", nameof(pattern));

        var matcher = _patternMatcherResolver.Resolve(pattern);
        var rawRegions = _regionEnumerator.GetRegions(options.MinScanAddress, options.MaxScanAddress, options.MemoryAccess);
        var mergedRegions = _memoryRangePlanner.MergeAdjacentRegions(rawRegions);
        var scanChunks = _memoryRangePlanner.CreateScanChunks(mergedRegions, pattern.Length, chunkSize);

        int initialCapacity = options.MaxResults > 0
            ? Math.Min(1024, options.MaxResults)
            : 1024;

        var results = new List<nint>(initialCapacity);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Lock syncRoot = new();

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism > 0
                ? options.MaxDegreeOfParallelism
                : -1
        };

        Parallel.ForEach(
            scanChunks,
            parallelOptions,
            () => new List<nint>(64),
            (chunk, state, localResults) =>
            {
                if (cts.IsCancellationRequested)
                {
                    state.Stop();
                    return localResults;
                }

                int chunkBufSize = (int)chunk.Size;
                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(chunkBufSize);
                Span<byte> buffer = rentedBuffer.AsSpan(0, chunkBufSize);

                try
                {
                    if (_memoryAccessor.ReadMemory(chunk.BaseAddress, buffer, out _))
                        matcher.ScanChunk(chunk, pattern, localResults, buffer);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }

                return localResults;
            },
            localResults =>
            {
                if (localResults.Count == 0)
                    return;

                lock (syncRoot)
                {
                    if (options.MaxResults > 0)
                    {
                        int remaining = options.MaxResults - results.Count;
                        if (remaining <= 0)
                            return;

                        int take = Math.Min(remaining, localResults.Count);
                        results.AddRange(localResults.GetRange(0, take));

                        if (results.Count >= options.MaxResults)
                            cts.Cancel();
                    }
                    else
                    {
                        results.AddRange(localResults);
                    }
                }
            });

        return results;
    }
}
