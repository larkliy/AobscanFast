using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Services;
using NSubstitute;

namespace AobscanFast.Tests.Integration;

public class AobScannerIntegrationTests
{
    private readonly IMemoryRegionEnumerator _regionEnumerator = Substitute.For<IMemoryRegionEnumerator>();
    private readonly IMemoryAccessor _memoryAccessor = Substitute.For<IMemoryAccessor>();
    private readonly IProcessHandler _handler = Substitute.For<IProcessHandler>();

    [Fact]
    public void Scan_NoRegions_ReturnsEmpty()
    {
        _regionEnumerator.GetRegions(Arg.Any<nint>(), Arg.Any<nint>(), Arg.Any<MemoryAccess>()).Returns([]);

        var scanner = new AobScanner(_handler, _regionEnumerator, _memoryAccessor);
        var results = scanner.Scan("AA BB CC");

        Assert.Empty(results);
    }

    [Fact]
    public void Scan_CancellationRequested_ThrowsOrReturnsPartial()
    {
        _regionEnumerator.GetRegions(Arg.Any<nint>(), Arg.Any<nint>(), Arg.Any<MemoryAccess>()).Returns([new(0x1000, 64)]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var scanner = new AobScanner(_handler, _regionEnumerator, _memoryAccessor);

        Assert.Throws<OperationCanceledException>(() => scanner.Scan("AA BB", ct: cts.Token));
    }

    [Fact]
    public void ScanFirst_NoResults_ReturnsNull()
    {
        _regionEnumerator.GetRegions(Arg.Any<nint>(), Arg.Any<nint>(), Arg.Any<MemoryAccess>()).Returns([]);

        var scanner = new AobScanner(_handler, _regionEnumerator, _memoryAccessor);

        Assert.Null(scanner.ScanFirst("AA BB CC"));
    }
}
