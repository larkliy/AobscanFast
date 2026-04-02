using AobscanFast.Core.Matching;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Core.Parsing;

namespace AobscanFast.Tests.Unit;

public class MatcherFactoryTests
{
    private static AobPattern ParsePattern(string input)
    {
        var parser = ParserFactory.GetParser(input);
        return parser.Parse(input);
    }

    [Fact]
    public void GetMatcher_SolidPattern_ReturnsSolidMatcher()
    {
        var pattern = ParsePattern("AA BB CC");

        var matcher = MatcherFactory.GetMatcher(pattern);

        Assert.IsType<SolidMatcher>(matcher);
    }

    [Fact]
    public void GetMatcher_MaskPattern_ReturnsMaskMatcher()
    {
        var pattern = ParsePattern("AA ?? CC");

        var matcher = MatcherFactory.GetMatcher(pattern);

        Assert.IsType<MaskMatcher>(matcher);
    }
}
