using AobscanFast.Abstractions;
using AobscanFast.Core.Matching;

namespace AobscanFast.Core.Models.Pattern;

internal static class MatcherFactory
{
    private static readonly SolidMatcher _solid = new();
    private static readonly MaskMatcher _mask = new();

    public static IPatternMatcher GetMatcher(AobPattern pattern)
        => pattern.HasMask ? _mask : _solid;
}
