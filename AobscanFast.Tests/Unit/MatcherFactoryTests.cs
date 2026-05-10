using AobscanFast.Core.Matching;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class PatternMatcherResolverTests
{
    private static AobPattern ParsePattern(string input)
    {
        var parser = new PatternParserResolver().Resolve(input);
        return parser.Parse(input);
    }

    [Fact]
    public void Resolve_SolidPattern_ReturnsSolidMatcher()
    {
        var pattern = ParsePattern("AA BB CC");

        var matcher = new PatternMatcherResolver().Resolve(pattern);

        Assert.IsType<SolidMatcher>(matcher);
    }

    [Fact]
    public void Resolve_MaskPattern_ReturnsMaskMatcher()
    {
        var pattern = ParsePattern("AA ?? CC");

        var matcher = new PatternMatcherResolver().Resolve(pattern);

        Assert.IsType<MaskMatcher>(matcher);
    }
}
