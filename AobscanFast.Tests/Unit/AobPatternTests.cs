using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class AobPatternTests
{
    private static AobPattern ParsePattern(string input)
    {
        var parser = ParserFactory.GetParser(input);
        return parser.Parse(input);
    }

    [Fact]
    public void Parse_SolidPattern_BytesParsedCorrectly()
    {
        var pattern = ParsePattern("AA BB CC DD");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, pattern.Bytes);
        Assert.False(pattern.HasMask);
        Assert.Null(pattern.Mask);
        Assert.Null(pattern.SearchSequence);
    }

    [Fact]
    public void Parse_SolidPattern_LowerCase()
    {
        var pattern = ParsePattern("aa bb cc");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, pattern.Bytes);
    }

    [Fact]
    public void Parse_MixedCase()
    {
        var pattern = ParsePattern("aB Cd eF");

        Assert.Equal(new byte[] { 0xAB, 0xCD, 0xEF }, pattern.Bytes);
    }

    [Fact]
    public void Parse_SingleByte()
    {
        var pattern = ParsePattern("FF");

        Assert.Single(pattern.Bytes);
        Assert.Equal(0xFF, pattern.Bytes[0]);
    }

    [Fact]
    public void Parse_PatternWithMask_HasMaskTrue()
    {
        var pattern = ParsePattern("AA ?? CC");

        Assert.True(pattern.HasMask);
        Assert.NotNull(pattern.Mask);
    }

    [Fact]
    public void Parse_PatternWithMask_BytesAndMaskCorrect()
    {
        var pattern = ParsePattern("AA ?? CC DD");

        Assert.Equal(new byte[] { 0xAA, 0x00, 0xCC, 0xDD }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xFF, 0x00, 0xFF, 0xFF }, pattern.Mask);
    }

    [Fact]
    public void Parse_PatternWithMask_SearchSequenceIsLongestSolidRun()
    {
        // "AA ?? CC DD EE" -> longest solid run is CC DD EE at offset 2
        var pattern = ParsePattern("AA ?? CC DD EE");

        Assert.Equal(new byte[] { 0xCC, 0xDD, 0xEE }, pattern.SearchSequence);
        Assert.Equal(2, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_PatternWithMask_SearchSequencePicksLongest()
    {
        // "AA BB ?? DD EE FF 11" -> "DD EE FF 11" (length 4) beats "AA BB" (length 2)
        var pattern = ParsePattern("AA BB ?? DD EE FF 11");

        Assert.Equal(new byte[] { 0xDD, 0xEE, 0xFF, 0x11 }, pattern.SearchSequence);
        Assert.Equal(3, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_MaskAtEnd()
    {
        var pattern = ParsePattern("AA BB CC ??");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, pattern.SearchSequence);
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_MaskAtStart()
    {
        var pattern = ParsePattern("?? BB CC DD");

        Assert.Equal(new byte[] { 0xBB, 0xCC, 0xDD }, pattern.SearchSequence);
        Assert.Equal(1, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_HalfMask_FirstNibble_ParsesCorrectly()
    {
        var pattern = ParsePattern("?A");

        Assert.Equal(new byte[] { 0x0A }, pattern.Bytes);
        Assert.Equal(new byte[] { 0x0F }, pattern.Mask);
        Assert.True(pattern.HasMask);
    }

    [Fact]
    public void Parse_HalfMask_SecondNibble_ParsesCorrectly()
    {
        var pattern = ParsePattern("B?");

        Assert.Equal(new byte[] { 0xB0 }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xF0 }, pattern.Mask);
    }

    [Fact]
    public void Parse_HalfMask_Mixed_ParsesCorrectly()
    {
        var pattern = ParsePattern("AA ?B C? ??");

        Assert.Equal(new byte[] { 0xAA, 0x0B, 0xC0, 0x00 }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xFF, 0x0F, 0xF0, 0x00 }, pattern.Mask);

        Assert.Equal(new byte[] { 0xAA }, pattern.SearchSequence);
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }
}