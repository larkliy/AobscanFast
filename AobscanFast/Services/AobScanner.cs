using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Services;

public sealed class AobScanner
{
    private readonly IProcessHandler _processHandler;
    private readonly IPatternParserResolver _patternParserResolver;
    private readonly ScanOrchestrator _scanOrchestrator;

    public AobScanner(IMemoryRegionEnumerator regionEnumerator, IMemoryAccessor memoryAccessor)
        : this(NullProcessHandler.Instance, new PatternParserResolver(), new ScanOrchestrator(regionEnumerator, memoryAccessor, new PatternMatcherResolver(), new RegionProcessor()))
    {
    }

    public AobScanner(IProcessHandler processHandler, IMemoryRegionEnumerator regionEnumerator, IMemoryAccessor memoryAccessor)
        : this(processHandler, new PatternParserResolver(), new ScanOrchestrator(regionEnumerator, memoryAccessor, new PatternMatcherResolver(), new RegionProcessor()))
    {
    }

    public AobScanner(
        IProcessHandler processHandler,
        IPatternParserResolver patternParserResolver,
        ScanOrchestrator scanOrchestrator)
    {
        _processHandler = processHandler ?? throw new ArgumentNullException(nameof(processHandler));
        _patternParserResolver = patternParserResolver ?? throw new ArgumentNullException(nameof(patternParserResolver));
        _scanOrchestrator = scanOrchestrator ?? throw new ArgumentNullException(nameof(scanOrchestrator));
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

        return _scanOrchestrator.Scan(
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
        return _scanOrchestrator.Scan(pattern, options, ct);
    }

    public nint? ScanFirst(AobPattern pattern, AobScanOptions? options = null, CancellationToken ct = default)
        => GetFirstOrNull(Scan(pattern, options, ct));

    private static nint? GetFirstOrNull(List<nint> results)
        => results.Count > 0 ? results[0] : null;
}
