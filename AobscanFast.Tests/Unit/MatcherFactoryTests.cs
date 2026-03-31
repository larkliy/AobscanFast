using AobscanFast.Core.Matching;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Tests.Unit;

public class MatcherFactoryTests
{
    [Fact]
    public void GetMatcher_SolidPattern_ReturnsSolidMatcher()
    {
        var pattern = AobPattern.Parse("AA BB CC");

        var matcher = MatcherFactory.GetMatcher(pattern);

        Assert.IsType<SolidMatcher>(matcher);
    }

    [Fact]
    public void GetMatcher_MaskPattern_ReturnsMaskMatcher()
    {
        var pattern = AobPattern.Parse("AA ?? CC");

        var matcher = MatcherFactory.GetMatcher(pattern);

        Assert.IsType<MaskMatcher>(matcher);
    }
}
