using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;
using System.Buffers;

namespace AobscanFast.Services;

public sealed class AobScanner
{
    private readonly Lock _syncRoot = new();
    private readonly IProcessHandler _processHandler;
    private readonly IMemoryRegionEnumerator _regionEnumerator;
    private readonly IMemoryAccessor _memoryAccessor;
    private readonly IPatternParserResolver _patternParserResolver;
    private readonly IPatternMatcherResolver _patternMatcherResolver;
    private readonly IMemoryRangePlanner _memoryRangePlanner;

    public AobScanner(IMemoryRegionEnumerator regionEnumerator, IMemoryAccessor memoryAccessor)
        : this(NullProcessHandler.Instance, regionEnumerator, memoryAccessor, new PatternParserResolver(), new PatternMatcherResolver(), new RegionProcessor())
    {
    }

    public AobScanner(IProcessHandler processHandler, IMemoryRegionEnumerator regionEnumerator, IMemoryAccessor memoryAccessor)
        : this(processHandler, regionEnumerator, memoryAccessor, new PatternParserResolver(), new PatternMatcherResolver(), new RegionProcessor())
    {
    }

    internal AobScanner(
        IProcessHandler processHandler,
        IMemoryRegionEnumerator regionEnumerator,
        IMemoryAccessor memoryAccessor,
        IPatternParserResolver patternParserResolver,
        IPatternMatcherResolver patternMatcherResolver,
        IMemoryRangePlanner memoryRangePlanner)
    {
        _processHandler = processHandler ?? throw new ArgumentNullException(nameof(processHandler));
        _regionEnumerator = regionEnumerator ?? throw new ArgumentNullException(nameof(regionEnumerator));
        _memoryAccessor = memoryAccessor ?? throw new ArgumentNullException(nameof(memoryAccessor));
        _patternParserResolver = patternParserResolver ?? throw new ArgumentNullException(nameof(patternParserResolver));
        _patternMatcherResolver = patternMatcherResolver ?? throw new ArgumentNullException(nameof(patternMatcherResolver));
        _memoryRangePlanner = memoryRangePlanner ?? throw new ArgumentNullException(nameof(memoryRangePlanner));
    }

    public List<nint> Scan(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        return Scan(parser.Parse(patternInput), options, ct);
    }

    public nint? ScanFirst(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
        => GetFirstOrNull(Scan(patternInput, options, ct));

    public List<nint> ScanModule(uint processId, string moduleName, string patternInput, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        return ScanModule(processId, moduleName, parser.Parse(patternInput), ct);
    }

    public nint? ScanModuleFirst(uint processId, string moduleName, string patternInput, CancellationToken ct = default)
        => GetFirstOrNull(ScanModule(processId, moduleName, patternInput, ct));

    public List<nint> ScanModule(uint processId, string moduleName, AobPattern pattern, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var moduleInfo = _processHandler.GetModuleInfo(processId, moduleName);

        if (moduleInfo is null)
            return [];

        return Scan(
            pattern,
            new AobScanOptions
            {
                MinScanAddress = moduleInfo.Value.BaseAddress,
                MaxScanAddress = moduleInfo.Value.BaseAddress + (nint)moduleInfo.Value.Size
            },
            ct);
    }

    public nint? ScanModuleFirst(uint processId, string moduleName, AobPattern pattern, CancellationToken ct = default)
        => GetFirstOrNull(ScanModule(processId, moduleName, pattern, ct));

    public List<nint> Scan(AobPattern pattern, AobScanOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        options ??= new();

        if (pattern.Length == 0)
            throw new ArgumentException("Pattern must contain at least one byte.", nameof(pattern));

        var matcher = _patternMatcherResolver.Resolve(pattern);
        var rawRegions = _regionEnumerator.GetRegions(options.MinScanAddress, options.MaxScanAddress, options.MemoryAccess);
        var mergedRegions = _memoryRangePlanner.MergeAdjacentRegions(rawRegions);
        var scanChunks = _memoryRangePlanner.CreateScanChunks(mergedRegions, pattern.Length);
        var results = new List<nint>(1024);

        Parallel.ForEach(
            scanChunks,
            new ParallelOptions { CancellationToken = ct },
            () => new List<nint>(64),
            (chunk, state, localResults) =>
            {
                int chunkSize = (int)chunk.Size;
                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(chunkSize);
                Span<byte> buffer = rentedBuffer.AsSpan(0, chunkSize);

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
                lock (_syncRoot)
                    results.AddRange(localResults);
            });

        return results;
    }

    public nint? ScanFirst(AobPattern pattern, AobScanOptions? options = null, CancellationToken ct = default)
        => GetFirstOrNull(Scan(pattern, options, ct));

    private static nint? GetFirstOrNull(List<nint> results)
        => results.Count > 0 ? results[0] : null;
}
