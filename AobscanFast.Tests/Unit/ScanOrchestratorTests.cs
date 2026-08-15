using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Services;

namespace AobscanFast.Tests.Unit;

public class ScanOrchestratorTests
{
    [Fact]
    public void Scan_MaxResults_ReturnsLimitWithoutCancellationException()
    {
        var accessor = new TestMemoryAccessor([0xAA, 0xAA, 0xAA, 0xAA]);
        var orchestrator = CreateOrchestrator(accessor, [new(0x1000, 4)]);

        var results = orchestrator.Scan(
            AobPattern.FromBytes([0xAA]),
            new AobScanOptions { ChunkSize = 4, MaxResults = 2 },
            default);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Scan_PartialRead_DoesNotScanUnreadTail()
    {
        var accessor = new TestMemoryAccessor([0xAA, 0xBB, 0xCC], bytesToRead: 2);
        var orchestrator = CreateOrchestrator(accessor, [new(0x1000, 3)]);

        var results = orchestrator.Scan(
            AobPattern.FromBytes([0xCC]),
            new AobScanOptions { ChunkSize = 3 },
            default);

        Assert.Empty(results);
    }

    [Fact]
    public void Scan_FailedPartialRead_ScansValidPrefix()
    {
        var accessor = new TestMemoryAccessor([0xAA], returnsSuccess: false);
        var orchestrator = CreateOrchestrator(accessor, [new(0x1000, 1)]);

        var results = orchestrator.Scan(
            AobPattern.FromBytes([0xAA]),
            new AobScanOptions { ChunkSize = 1 },
            default);

        Assert.Single(results);
    }

    [Theory]
    [InlineData(0, -1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, -2, 0)]
    [InlineData(1, -1, -1)]
    public void Scan_InvalidOptions_Throw(nint chunkSize, int parallelism, int maxResults)
    {
        var orchestrator = CreateOrchestrator(new TestMemoryAccessor([]), []);
        var options = new AobScanOptions
        {
            ChunkSize = chunkSize,
            MaxDegreeOfParallelism = parallelism,
            MaxResults = maxResults
        };

        Assert.ThrowsAny<ArgumentException>(() => orchestrator.Scan(AobPattern.FromBytes([0xAA]), options, default));
    }

    [Fact]
    public void ScanFirst_ReturnsMatch()
    {
        var accessor = new TestMemoryAccessor([0x00, 0xAA, 0x00]);
        var orchestrator = CreateOrchestrator(accessor, [new(0x1000, 3)]);

        nint? result = orchestrator.ScanFirst(
            AobPattern.FromBytes([0xAA]),
            new AobScanOptions { ChunkSize = 3 },
            default);

        Assert.Equal((nint)0x1001, result);
    }

    [Fact]
    public void Scan_SelfProcessAccessor_UsesSequentialPooledReads()
    {
        var accessor = new SelfProcessTestMemoryAccessor([0x00, 0xAA, 0x00]);
        var orchestrator = CreateOrchestrator(accessor, [new(0x1000, 3)]);

        var results = orchestrator.Scan(
            AobPattern.FromBytes([0xAA]),
            new AobScanOptions { ChunkSize = 3 },
            default);

        Assert.Equal([(nint)0x1001], results);
        Assert.Equal(1, accessor.ReadCalls);
    }

    private static ScanOrchestrator CreateOrchestrator(IMemoryAccessor accessor, List<MemoryRange> ranges)
        => new(new TestRegionEnumerator(ranges), accessor, new PatternMatcherResolver(), new RegionProcessor());

    private sealed class TestRegionEnumerator(List<MemoryRange> ranges) : IMemoryRegionEnumerator
    {
        public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access) => ranges;
    }

    private sealed class TestMemoryAccessor(byte[] data, int? bytesToRead = null, bool returnsSuccess = true) : IMemoryAccessor
    {
        public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
        {
            int length = Math.Min(bytesToRead ?? data.Length, Math.Min(data.Length, buffer.Length));
            data.AsSpan(0, length).CopyTo(buffer);
            bytesRead = (nuint)length;
            return returnsSuccess;
        }

        public bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }
    }

    private sealed class SelfProcessTestMemoryAccessor(byte[] data) : IMemoryAccessor, ISelfProcessMemoryAccessor
    {
        public int ReadCalls { get; private set; }

        public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
        {
            ReadCalls++;
            data.AsSpan(0, Math.Min(data.Length, buffer.Length)).CopyTo(buffer);
            bytesRead = (nuint)Math.Min(data.Length, buffer.Length);
            return true;
        }

        public bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }
    }
}
