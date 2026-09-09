using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class SolidParserTests
{
    private readonly SolidParser _parser = new();

    [Theory]
    [InlineData("AA BB")]
    [InlineData("11 22 33")]
    [InlineData("FF")]
    public void CanParse_SolidPattern_ReturnsTrue(string input)
    {
        Assert.True(_parser.CanParse(input));
    }

    [Theory]
    [InlineData("AA ??")]
    [InlineData("?A")]
    [InlineData("B?")]
    public void CanParse_ContainsWildcard_ReturnsFalse(string input)
    {
        Assert.False(_parser.CanParse(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CanParse_NullOrWhiteSpace_ReturnsFalse(string input)
    {
        Assert.False(_parser.CanParse(input));
    }

    [Fact]
    public void CanParse_Null_ReturnsFalse()
    {
        Assert.False(_parser.CanParse(null!));
    }

    [Fact]
    public void Parse_ValidSolidTokens_ReturnsPattern()
    {
        var pattern = _parser.Parse("AA BB CC");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, pattern.Bytes.ToArray());
        Assert.False(pattern.HasMask);
        Assert.True(pattern.SearchSequence.IsEmpty); // AobPattern with null mask sets SearchSequence to null
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_ExtraSpaces_AreIgnored()
    {
        var pattern = _parser.Parse("AA   BB  CC");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, pattern.Bytes.ToArray());
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

    [Theory]
    [InlineData("A")]
    [InlineData("AAA")]
    [InlineData("AA B CC")]
    public void Parse_InvalidTokenLength_ThrowsFormatException(string input)
    {
        var ex = Assert.Throws<FormatException>(() => _parser.Parse(input));
        Assert.Contains("Invalid solid byte token", ex.Message);
    }

    [Theory]
    [InlineData("GG")]
    [InlineData("XX")]
    [InlineData("-1")]
    public void Parse_InvalidHexCharacter_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => _parser.Parse(input));
    }
}
