using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class PatternParserResolverTests
{
    private readonly PatternParserResolver _resolver = new();

    [Fact]
    public void Resolve_SolidPattern_ReturnsSolidParser()
    {
        var result = _resolver.Resolve("AA BB CC");

        Assert.IsType<SolidParser>(result);
    }

    [Fact]
    public void Resolve_MaskPattern_ReturnsMaskParser()
    {
        var result = _resolver.Resolve("AA ?? CC");

        Assert.IsType<MaskParser>(result);
    }

    [Fact]
    public void Resolve_HalfMaskFirstNibble_ReturnsHalfMaskParser()
    {
        var result = _resolver.Resolve("?A");

        Assert.IsType<HalfMaskParser>(result);
    }

    [Fact]
    public void Resolve_HalfMaskSecondNibble_ReturnsHalfMaskParser()
    {
        var result = _resolver.Resolve("B?");

        Assert.IsType<HalfMaskParser>(result);
    }

    [Fact]
    public void Resolve_HalfMaskMiddleNibble_ReturnsHalfMaskParser()
    {
        var result = _resolver.Resolve("A?B");

        Assert.IsType<HalfMaskParser>(result);
    }

    [Fact]
    public void Resolve_MixedHalfAndFullMaskReturnsHalfMaskParser()
    {
        var result = _resolver.Resolve("AA ?? ?B");

        Assert.IsType<HalfMaskParser>(result);
    }

    [Fact]
    public void Resolve_OnlyWildcardBytes_ReturnsMaskParser()
    {
        var result = _resolver.Resolve("?? ?? ??");

        Assert.IsType<MaskParser>(result);
    }

    [Fact]
    public void Resolve_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _resolver.Resolve(""));
    }

    [Fact]
    public void Resolve_Whitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _resolver.Resolve("   "));
    }

    [Fact]
    public void Resolve_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(null!));
    }
}
