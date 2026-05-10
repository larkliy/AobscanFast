using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Matching;

internal sealed class PatternMatcherResolver : IPatternMatcherResolver
{
    private static readonly SolidMatcher s_solidMatcher = new();
    private static readonly MaskMatcher s_maskMatcher = new();

    public IPatternMatcher Resolve(AobPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        return pattern.HasMask ? s_maskMatcher : s_solidMatcher;
    }
}
