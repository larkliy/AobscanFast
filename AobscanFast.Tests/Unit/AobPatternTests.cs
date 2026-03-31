using AobscanFast.Core.Models.Pattern;
using System;
using System.Collections.Generic;
using System.Text;

namespace AobscanFast.Tests.Unit;

public class AobPatternTests
{
    [Fact]
    public void Parse_SolidPattern_BytesParsedCorrectly()
    {
        var pattern = AobPattern.Parse("AA BB CC DD");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, pattern.Bytes);
        Assert.False(pattern.HasMask);
        Assert.Null(pattern.Mask);
        Assert.Null(pattern.SearchSequence);
    }

    [Fact]
    public void Parse_SolidPattern_LowerCase()
    {
        var pattern = AobPattern.Parse("aa bb cc");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, pattern.Bytes);
    }

    [Fact]
    public void Parse_MixedCase()
    {
        var pattern = AobPattern.Parse("aB Cd eF");

        Assert.Equal(new byte[] { 0xAB, 0xCD, 0xEF }, pattern.Bytes);
    }

    [Fact]
    public void Parse_SingleByte()
    {
        var pattern = AobPattern.Parse("FF");

        Assert.Single(pattern.Bytes);
        Assert.Equal(0xFF, pattern.Bytes[0]);
    }

    [Fact]
    public void Parse_PatternWithMask_HasMaskTrue()
    {
        var pattern = AobPattern.Parse("AA ?? CC");

        Assert.True(pattern.HasMask);
        Assert.NotNull(pattern.Mask);
    }

    [Fact]
    public void Parse_PatternWithMask_BytesAndMaskCorrect()
    {
        var pattern = AobPattern.Parse("AA ?? CC DD");

        Assert.Equal(new byte[] { 0xAA, 0x00, 0xCC, 0xDD }, pattern.Bytes);
        Assert.Equal(new byte[] { 0xFF, 0x00, 0xFF, 0xFF }, pattern.Mask);
    }

    [Fact]
    public void Parse_PatternWithMask_SearchSequenceIsLongestSolidRun()
    {
        // "AA ?? CC DD EE" -> longest solid run is CC DD EE at offset 2
        var pattern = AobPattern.Parse("AA ?? CC DD EE");

        Assert.Equal(new byte[] { 0xCC, 0xDD, 0xEE }, pattern.SearchSequence);
        Assert.Equal(2, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_PatternWithMask_SearchSequencePicksLongest()
    {
        // "AA BB ?? DD EE FF 11" -> "DD EE FF 11" (length 4) beats "AA BB" (length 2)
        var pattern = AobPattern.Parse("AA BB ?? DD EE FF 11");

        Assert.Equal(new byte[] { 0xDD, 0xEE, 0xFF, 0x11 }, pattern.SearchSequence);
        Assert.Equal(3, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_MaskAtEnd()
    {
        var pattern = AobPattern.Parse("AA BB CC ??");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, pattern.SearchSequence);
        Assert.Equal(0, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_MaskAtStart()
    {
        var pattern = AobPattern.Parse("?? BB CC DD");

        Assert.Equal(new byte[] { 0xBB, 0xCC, 0xDD }, pattern.SearchSequence);
        Assert.Equal(1, pattern.SearchSequenceOffset);
    }

    [Fact]
    public void Parse_AllMasks_Throws()
    {
        Assert.Throws<FormatException>(() => AobPattern.Parse("?? ?? ??"));
    }
}
