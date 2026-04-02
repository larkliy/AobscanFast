using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Matching;

internal static class MatcherFactory
{
    private static readonly SolidMatcher s_solid = new();
    private static readonly MaskMatcher s_mask = new();

    public static IPatternMatcher GetMatcher(AobPattern pattern)
        => pattern.HasMask ? s_mask : s_solid;
}
