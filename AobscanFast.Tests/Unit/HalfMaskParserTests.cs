using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class HalfMaskParserTests
{
    private readonly HalfMaskParser _parser = new();

    [Fact]
    public void Parse_FirstNibbleWildcard()
    {
        var pattern = _parser.Parse("?A");

        Assert.Equal(new byte[] { 0x0A }, pattern.Bytes);
        Assert.Equal(new byte[] { 0x0F }, pattern.Mask);
        Assert.True(pattern.HasMask);
    }

    [Fact]
    public void Parse_SecondNibbleWildcard()
    {
        var pattern = _parser.Parse("B?");

        Assert.Equal(new byte[] { 0xB0 }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xF0 }, pattern.Mask);
    }

    [Fact]
    public void Parse_SingleNibbleWildcard()
    {
        var pattern = _parser.Parse("?");

        // "?" token length 1 => hChar='0', lChar='?' => val=0x00, mask=0xF0
        Assert.Equal(new byte[] { 0x00 }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xF0 }, pattern.Mask);
    }

    [Fact]
    public void Parse_SingleHexCharLowerNibble()
    {
        var pattern = _parser.Parse("A");

        // "A" token length 1 => hChar='0', lChar='A' => val=0x0A, mask=0xFF
        Assert.Equal(new byte[] { 0x0A }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xFF }, pattern.Mask);
    }

    [Fact]
    public void Parse_MultipleHalfMaskBytes()
    {
        var pattern = _parser.Parse("?A B? ?C D?");

        Assert.Equal(new byte[] { 0x0A, 0xB0, 0x0C, 0xD0 }, pattern.Bytes);
        Assert.Equal(new byte[] { 0x0F, 0xF0, 0x0F, 0xF0 }, pattern.Mask);
    }

    [Fact]
    public void Parse_MixedFullAndHalfMaskTotally()
    {
        var pattern = _parser.Parse("AA ?B C? ??");

        Assert.Equal(new byte[] { 0xAA, 0x0B, 0xC0, 0x00 }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xFF, 0x0F, 0xF0, 0x00 }, pattern.Mask);

        Assert.Equal(new byte[] { 0xAA }, pattern.SearchSequence);
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_SearchSequencePicksLongestSolidRun()
    {
        var pattern = _parser.Parse("?A BB CC D?");

        Assert.Equal(new byte[] { 0xBB, 0xCC }, pattern.SearchSequence);
        Assert.Equal(1, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_SearchSequenceAtStart()
    {
        var pattern = _parser.Parse("AA BB ?C");

        Assert.Equal(new byte[] { 0xAA, 0xBB }, pattern.SearchSequence);
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_SearchSequenceAtEnd()
    {
        var pattern = _parser.Parse("?A BB CC");

        Assert.Equal(new byte[] { 0xBB, 0xCC }, pattern.SearchSequence);
        Assert.Equal(1, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_AllWildcards_NoSolidRun()
    {
        var pattern = _parser.Parse("? ? ?");

        Assert.Empty(pattern.SearchSequence);
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
    }

    [Fact]
    public void Parse_Whitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse("   "));
    }

    [Fact]
    public void Parse_TabSeparated_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => _parser.Parse("?A\tB?"));
        Assert.Contains("Invalid half-mask token", ex.Message);
    }

    [Fact]
    public void Parse_TokenTooLong_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => _parser.Parse("A?B"));
        Assert.Contains("Invalid half-mask token", ex.Message);
    }
}
