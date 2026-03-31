using AobscanFast.Abstractions;
using AobscanFast.Core.Models;
using AobscanFast.Infrastructure;
using AobscanFast.Services;
using NSubstitute;

namespace AobscanFast.Tests.Integration
{
    public class AobScannerIntegrationTests
    {
        private readonly IMemoryReader _reader = Substitute.For<IMemoryReader>();

        [Fact]
        public void Scan_NoRegions_ReturnsEmpty()
        {
            _reader.GetRegions(Arg.Any<nint>(), Arg.Any<nint>(), Arg.Any<MemoryAccess>()).Returns([]);

            var scanner = new AobScanner(_reader);
            var results = scanner.Scan("AA BB CC");

            Assert.Empty(results);
        }

        [Fact]
        public void Scan_CancellationRequested_ThrowsOrReturnsPartial()
        {
            _reader.GetRegions(Arg.Any<nint>(), Arg.Any<nint>(), Arg.Any<MemoryAccess>()).Returns([new(0x1000, 64)]);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var scanner = new AobScanner(_reader);

            Assert.Throws<OperationCanceledException>(() => scanner.Scan("AA BB", ct: cts.Token));
        }
    }
}
