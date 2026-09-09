using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class MaskParserTests
{
    private readonly MaskParser _parser = new();

    [Fact]
    public void CanParse_ValidInput_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("AA ?? BB"));
        Assert.True(_parser.CanParse("?"));
    }

    [Fact]
    public void CanParse_NoWildcard_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("AA BB CC"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CanParse_NullOrWhiteSpace_ReturnsFalse(string? input)
    {
        Assert.False(_parser.CanParse(input!));
    }

    [Fact]
    public void Parse_SingleWildcard()
    {
        var pattern = _parser.Parse("?");

        Assert.Equal(new byte[] { 0x00 }, pattern.Bytes.ToArray());
        Assert.Equal(new byte[] { 0x00 }, pattern.Mask.ToArray());
        Assert.True(pattern.HasMask);
    }

    [Fact]
    public void Parse_DoubleWildcard()
    {
        var pattern = _parser.Parse("??");

        Assert.Equal(new byte[] { 0x00 }, pattern.Bytes.ToArray());
        Assert.Equal(new byte[] { 0x00 }, pattern.Mask.ToArray());
        Assert.True(pattern.HasMask);
    }

    [Fact]
    public void Parse_ExactByte()
    {
        var pattern = _parser.Parse("AA");

        Assert.Equal(new byte[] { 0xAA }, pattern.Bytes.ToArray());
        Assert.Equal(new byte[] { 0xFF }, pattern.Mask.ToArray());
    }

    [Fact]
    public void Parse_MixedBytesAndWildcards()
    {
        var pattern = _parser.Parse("AA ? BB ?? CC");

        Assert.Equal(new byte[] { 0xAA, 0x00, 0xBB, 0x00, 0xCC }, pattern.Bytes.ToArray());
        Assert.Equal(new byte[] { 0xFF, 0x00, 0xFF, 0x00, 0xFF }, pattern.Mask.ToArray());
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
    public void Parse_TokenTooShort_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => _parser.Parse("A"));
        Assert.Contains("Invalid masked byte token", ex.Message);
    }

    [Fact]
    public void Parse_TokenTooLong_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => _parser.Parse("AAA"));
        Assert.Contains("Invalid masked byte token", ex.Message);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("G1")]
    [InlineData("1G")]
    [InlineData("-1")]
    public void Parse_InvalidHexCharacter_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => _parser.Parse(input));
    }
}
