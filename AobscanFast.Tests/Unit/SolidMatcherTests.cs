using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Tests.Unit;

public class SolidMatcherTests
{
    private readonly SolidMatcher _matcher = new();

    [Fact]
    public void ScanChunk_PatternAtStart_Found()
    {
        var pattern = AobPattern.Parse("AA BB CC");
        var buffer = new byte[] { 0xAA, 0xBB, 0xCC, 0x00, 0x00 };
        var range = new MemoryRange(0x1000, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
        Assert.Equal((nint)0x1000, results[0]);
    }

    [Fact]
    public void ScanChunk_PatternInMiddle_Found()
    {
        var pattern = AobPattern.Parse("BB CC");
        var buffer = new byte[] { 0x00, 0xBB, 0xCC, 0x00 };
        var range = new MemoryRange(0x2000, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
        Assert.Equal((nint)0x2001, results[0]);
    }

    [Fact]
    public void ScanChunk_PatternAtEnd_Found()
    {
        var pattern = AobPattern.Parse("CC DD");
        var buffer = new byte[] { 0x00, 0x00, 0xCC, 0xDD };
        var range = new MemoryRange(0x3000, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
        Assert.Equal((nint)0x3002, results[0]);
    }

    [Fact]
    public void ScanChunk_NoMatch_Empty()
    {
        var pattern = AobPattern.Parse("FF EE");
        var buffer = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var range = new MemoryRange(0x1000, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Empty(results);
    }

    [Fact]
    public void ScanChunk_MultipleMatches_AllFound()
    {
        var pattern = AobPattern.Parse("AA BB");
        var buffer = new byte[] { 0xAA, 0xBB, 0x00, 0xAA, 0xBB };
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Equal(2, results.Count);
        Assert.Equal((nint)0x0, results[0]);
        Assert.Equal((nint)0x3, results[1]);
    }

    [Fact]
    public void ScanChunk_OverlappingMatches_AllFound()
    {
        var pattern = AobPattern.Parse("AA AA");
        var buffer = new byte[] { 0xAA, 0xAA, 0xAA };
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Equal(2, results.Count);
        Assert.Equal((nint)0x0, results[0]);
        Assert.Equal((nint)0x1, results[1]);
    }
}