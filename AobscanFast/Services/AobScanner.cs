using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Services;

/// <summary>Provides high-level methods for scanning process memory for byte patterns.</summary>
public sealed class AobScanner
{
    private readonly IProcessHandler _processHandler;
    private readonly IPatternParserResolver _patternParserResolver;
    private readonly ScanOrchestrator _scanOrchestrator;

    /// <summary>Initializes a scanner with custom memory enumeration and access implementations.</summary>
    /// <param name="regionEnumerator">The component that enumerates scannable memory regions.</param>
    /// <param name="memoryAccessor">The component that reads and writes process memory.</param>
    public AobScanner(IMemoryRegionEnumerator regionEnumerator, IMemoryAccessor memoryAccessor)
        : this(NullProcessHandler.Instance, new PatternParserResolver(), new ScanOrchestrator(regionEnumerator, memoryAccessor, new PatternMatcherResolver(), new RegionProcessor()))
    {
    }

    /// <summary>Initializes a scanner with custom process, region, and memory implementations.</summary>
    /// <param name="processHandler">The component used for process and module lookup.</param>
    /// <param name="regionEnumerator">The component that enumerates scannable memory regions.</param>
    /// <param name="memoryAccessor">The component that reads and writes process memory.</param>
    public AobScanner(IProcessHandler processHandler, IMemoryRegionEnumerator regionEnumerator, IMemoryAccessor memoryAccessor)
        : this(processHandler, new PatternParserResolver(), new ScanOrchestrator(regionEnumerator, memoryAccessor, new PatternMatcherResolver(), new RegionProcessor()))
    {
    }

    /// <summary>Initializes a scanner with all core services explicitly supplied.</summary>
    /// <param name="processHandler">The component used for process and module lookup.</param>
    /// <param name="patternParserResolver">The resolver used to select a parser for string patterns.</param>
    /// <param name="scanOrchestrator">The component that executes scans.</param>
    public AobScanner(
        IProcessHandler processHandler,
        IPatternParserResolver patternParserResolver,
        ScanOrchestrator scanOrchestrator)
    {
        _processHandler = processHandler ?? throw new ArgumentNullException(nameof(processHandler));
        _patternParserResolver = patternParserResolver ?? throw new ArgumentNullException(nameof(patternParserResolver));
        _scanOrchestrator = scanOrchestrator ?? throw new ArgumentNullException(nameof(scanOrchestrator));
    }

    /// <summary>Scans memory for a pattern expressed in AOB string syntax.</summary>
    /// <param name="patternInput">A space-separated pattern containing exact bytes and supported wildcards.</param>
    /// <param name="options">Scan boundaries and execution options, or default options when omitted.</param>
    /// <param name="ct">A token used to cancel the scan.</param>
    /// <returns>Addresses of all matches, subject to <see cref="AobScanOptions.MaxResults"/>.</returns>
    public List<nint> Scan(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        AobPattern pattern = parser.Parse(patternInput);
        try
        {
            return Scan(pattern, options, ct);
        }
        finally
        {
            pattern.Clear();
        }
    }

    /// <summary>Scans memory for a pattern and returns the first match found.</summary>
    /// <param name="patternInput">A space-separated pattern containing exact bytes and supported wildcards.</param>
    /// <param name="options">Scan boundaries and execution options, or default options when omitted.</param>
    /// <param name="ct">A token used to cancel the scan.</param>
    /// <returns>The address of a match, or <see langword="null"/> when no match exists.</returns>
    public nint? ScanFirst(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        AobPattern pattern = parser.Parse(patternInput);
        try
        {
            return ScanFirst(pattern, options, ct);
        }
        finally
        {
            pattern.Clear();
        }
    }

    /// <summary>Scans a named module in a process for a pattern expressed in AOB string syntax.</summary>
    /// <param name="processId">The operating-system process identifier.</param><param name="moduleName">The module name.</param><param name="patternInput">The AOB pattern.</param><param name="ct">A cancellation token.</param>
    /// <returns>Addresses of matches, or an empty list when the module is not found.</returns>
    public List<nint> ScanModule(uint processId, string moduleName, string patternInput, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        AobPattern pattern = parser.Parse(patternInput);
        try
        {
            return ScanModule(processId, moduleName, pattern, ct);
        }
        finally
        {
            pattern.Clear();
        }
    }

    /// <summary>Scans a named module and returns the first match for an AOB string pattern.</summary>
    /// <param name="processId">The operating-system process identifier.</param><param name="moduleName">The module name.</param><param name="patternInput">The AOB pattern.</param><param name="ct">A cancellation token.</param>
    /// <returns>The first matching address, or <see langword="null"/> when the module or match is absent.</returns>
    public nint? ScanModuleFirst(uint processId, string moduleName, string patternInput, CancellationToken ct = default)
    {
        var parser = _patternParserResolver.Resolve(patternInput);
        AobPattern pattern = parser.Parse(patternInput);
        try
        {
            return ScanModuleFirst(processId, moduleName, pattern, ct);
        }
        finally
        {
            pattern.Clear();
        }
    }

    /// <summary>Scans a named module for a compiled pattern.</summary>
    /// <param name="processId">The operating-system process identifier.</param><param name="moduleName">The module name.</param><param name="pattern">The compiled pattern.</param><param name="ct">A cancellation token.</param>
    /// <returns>Addresses of matches, or an empty list when the module is not found.</returns>
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
                MaxScanAddress = checked(moduleInfo.Value.BaseAddress + (nint)moduleInfo.Value.Size)
            },
            ct);
    }

    /// <summary>Scans a named module and returns the first match for a compiled pattern.</summary>
    /// <param name="processId">The operating-system process identifier.</param><param name="moduleName">The module name.</param><param name="pattern">The compiled pattern.</param><param name="ct">A cancellation token.</param>
    /// <returns>The first matching address, or <see langword="null"/> when the module or match is absent.</returns>
    public nint? ScanModuleFirst(uint processId, string moduleName, AobPattern pattern, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var moduleInfo = _processHandler.GetModuleInfo(processId, moduleName);
        if (moduleInfo is null)
            return null;

        return _scanOrchestrator.ScanFirst(
            pattern,
            new AobScanOptions
            {
                MinScanAddress = moduleInfo.Value.BaseAddress,
                MaxScanAddress = checked(moduleInfo.Value.BaseAddress + (nint)moduleInfo.Value.Size)
            },
            ct);
    }

    /// <summary>Scans memory for a compiled pattern.</summary>
    /// <param name="pattern">The compiled pattern.</param><param name="options">Scan options, or defaults when omitted.</param><param name="ct">A cancellation token.</param>
    /// <returns>Addresses of all matches.</returns>
    public List<nint> Scan(AobPattern pattern, AobScanOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        options ??= new();
        return _scanOrchestrator.Scan(pattern, options, ct);
    }

    /// <summary>Scans memory for a compiled pattern and returns the first match found.</summary>
    /// <param name="pattern">The compiled pattern.</param><param name="options">Scan options, or defaults when omitted.</param><param name="ct">A cancellation token.</param>
    /// <returns>The first matching address, or <see langword="null"/> when no match exists.</returns>
    public nint? ScanFirst(AobPattern pattern, AobScanOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        options ??= new();
        return _scanOrchestrator.ScanFirst(pattern, options, ct);
    }
}
