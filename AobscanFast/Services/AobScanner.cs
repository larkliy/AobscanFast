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
    private readonly IMemoryReader _memoryReader;
    private readonly IPatternParserResolver _patternParserResolver;
    private readonly IPatternMatcherResolver _patternMatcherResolver;
    private readonly IMemoryRangePlanner _memoryRangePlanner;

    public AobScanner(IMemoryReader memoryReader)
        : this(NullProcessHandler.Instance, memoryReader, new PatternParserResolver(), new PatternMatcherResolver(), new RegionProcessor())
    {
    }

    public AobScanner(IProcessHandler processHandler, IMemoryReader memoryReader)
        : this(processHandler, memoryReader, new PatternParserResolver(), new PatternMatcherResolver(), new RegionProcessor())
    {
    }

    internal AobScanner(
        IProcessHandler processHandler,
        IMemoryReader memoryReader,
        IPatternParserResolver patternParserResolver,
        IPatternMatcherResolver patternMatcherResolver,
        IMemoryRangePlanner memoryRangePlanner)
    {
        _processHandler = processHandler ?? throw new ArgumentNullException(nameof(processHandler));
        _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        _patternParserResolver = patternParserResolver ?? throw new ArgumentNullException(nameof(patternParserResolver));
        _patternMatcherResolver = patternMatcherResolver ?? throw new ArgumentNullException(nameof(patternMatcherResolver));
        _memoryRangePlanner = memoryRangePlanner ?? throw new ArgumentNullException(nameof(memoryRangePlanner));
    }

    public List<nint> Scan(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        var pattern = parser.Parse(patternInput);

        return Scan(pattern, options, ct);
    }

    public nint? ScanFirst(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
        => GetFirstOrNull(Scan(patternInput, options, ct));

    public List<nint> ScanModule(uint processId, string moduleName, string patternInput, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        var pattern = parser.Parse(patternInput);

        return ScanModule(processId, moduleName, pattern, ct);
    }

    public nint? ScanModuleFirst(uint processId, string moduleName, string patternInput, CancellationToken ct = default)
        => GetFirstOrNull(ScanModule(processId, moduleName, patternInput, ct));

    public List<nint> ScanModule(uint processId, string moduleName, AobPattern pattern, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var moduleInfo = _processHandler.GetModuleInfo(processId, moduleName);

        if (moduleInfo is null)
            return [];

        var options = new AobScanOptions
        {
            MinScanAddress = moduleInfo.Value.BaseAddress,
            MaxScanAddress = moduleInfo.Value.BaseAddress + (nint)moduleInfo.Value.Size
        };

        return Scan(pattern, options, ct);
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

        var rawRegions = _memoryReader.GetRegions(
            options.MinScanAddress,
            options.MaxScanAddress,
            options.MemoryAccess);

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
                    if (_memoryReader.ReadMemory(chunk.BaseAddress, buffer, out _))
                    {
                        matcher.ScanChunk(chunk, pattern, localResults, buffer);
                    }
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
                {
                    results.AddRange(localResults);
                }
            });

        return results;
    }

    public nint? ScanFirst(AobPattern pattern, AobScanOptions? options = null, CancellationToken ct = default)
        => GetFirstOrNull(Scan(pattern, options, ct));

    private static nint? GetFirstOrNull(List<nint> results)
        => results.Count > 0 ? results[0] : null;
}
