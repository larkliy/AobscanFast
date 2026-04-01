using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Matching;

internal static class MatcherFactory
{
    private static readonly SolidMatcher _solid = new();
    private static readonly MaskMatcher _mask = new();

    public static IPatternMatcher GetMatcher(AobPattern pattern)
        => pattern.HasMask ? _mask : _solid;
}
