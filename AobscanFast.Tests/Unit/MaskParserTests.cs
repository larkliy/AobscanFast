using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;
using System;
using Xunit;

namespace AobscanFast.Tests.Unit;

public class MaskParserTests
{
    private readonly MaskParser _parser = new();

    [Fact]
    public void CanParse_WithQuestionMark_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("AA ? BB"));
        Assert.True(_parser.CanParse("?"));
        Assert.True(_parser.CanParse("??"));
    }

    [Fact]
    public void CanParse_WithoutQuestionMark_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("AA BB CC"));
        Assert.False(_parser.CanParse("AA"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CanParse_EmptyOrWhitespace_ReturnsFalse(string input)
    {
        Assert.False(_parser.CanParse(input));
    }

    [Fact]
    public void CanParse_Null_ReturnsFalse()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.False(_parser.CanParse(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public void Parse_ValidMask_ReturnsCorrectPattern()
    {
        var pattern = _parser.Parse("AA ? BB ?? CC");

        Assert.Equal(new byte[] { 0xAA, 0x00, 0xBB, 0x00, 0xCC }, pattern.Bytes.ToArray());
        Assert.Equal(new byte[] { 0xFF, 0x00, 0xFF, 0x00, 0xFF }, pattern.Mask.ToArray());
        Assert.True(pattern.HasMask);
    }

    [Theory]
    [InlineData("AAA")]
    [InlineData("A")]
    [InlineData("AA ? A BB")]
    [InlineData("AA ?? AAA BB")]
    public void Parse_InvalidMaskedByteTokenLength_ThrowsFormatException(string input)
    {
        var ex = Assert.Throws<FormatException>(() => _parser.Parse(input));
        Assert.Contains("Invalid masked byte token", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(input));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    // The error "Pattern must contain at least one byte token" is thrown when length is 0,
    // which can happen if input consists only of empty parts.
    // Note: The MaskParser currently throws ArgumentException for empty/whitespace via ArgumentException.ThrowIfNullOrWhiteSpace(input);
    // If we wanted to hit that FormatException, we'd need an input that passes the whitespace check
    // but produces 0 valid parts. Given it uses Split(' ') and ignores empty parts, an input with just spaces
    // is caught by ArgumentException.ThrowIfNullOrWhiteSpace first.
}
