using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class MaskMatcherTests
{
    private readonly MaskMatcher _matcher = new();


    private static AobPattern ParsePattern(string input)
    {
        var parser = ParserFactory.GetParser(input);
        return parser.Parse(input);
    }

    [Fact]
    public void ScanChunk_WildcardMiddle_Matches()
    {
        var pattern = ParsePattern("AA ?? CC");
        var buffer = new byte[] { 0xAA, 0x42, 0xCC, 0x00 };
        var range = new MemoryRange(0x1000, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
        Assert.Equal((nint)0x1000, results[0]);
    }

    [Fact]
    public void ScanChunk_WildcardStart_Matches()
    {
        var pattern = ParsePattern("?? BB CC");
        var buffer = new byte[] { 0xFF, 0xBB, 0xCC };
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
        Assert.Equal((nint)0x0, results[0]);
    }

    [Fact]
    public void ScanChunk_WildcardEnd_Matches()
    {
        var pattern = ParsePattern("AA BB ??");
        var buffer = new byte[] { 0xAA, 0xBB, 0x99 };
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
    }

    [Fact]
    public void ScanChunk_NoMatch_Empty()
    {
        var pattern = ParsePattern("AA ?? DD");
        var buffer = new byte[] { 0xAA, 0x42, 0xCC }; // last byte CC != DD
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Empty(results);
    }

    [Fact]
    public void ScanChunk_MultipleWildcardMatches()
    {
        var pattern = ParsePattern("AA ?? CC");
        var buffer = new byte[] { 0xAA, 0x11, 0xCC, 0x00, 0xAA, 0x22, 0xCC };
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Equal(2, results.Count);
        Assert.Equal((nint)0x0, results[0]);
        Assert.Equal((nint)0x4, results[1]);
    }

    [Fact]
    public void ScanChunk_MultipleConsecutiveWildcards()
    {
        var pattern = ParsePattern("AA ?? ?? DD");
        var buffer = new byte[] { 0xAA, 0x00, 0x00, 0xDD };
        var range = new MemoryRange(0x0, buffer.Length);
        var results = new List<nint>();

        _matcher.ScanChunk(range, pattern, results, buffer);

        Assert.Single(results);
    }

    [Fact]
    public void ScanChunk_WildcardMatchesAnyByte()
    {
        var pattern = ParsePattern("AA ?? CC");
        var range = new MemoryRange(0x0, 3);
        var results = new List<nint>();

        for (int b = 0; b <= 0xFF; b++)
        {
            results.Clear();
            var buffer = new byte[] { 0xAA, (byte)b, 0xCC };
            _matcher.ScanChunk(range, pattern, results, buffer);
            Assert.Single(results);
        }
    }
}
