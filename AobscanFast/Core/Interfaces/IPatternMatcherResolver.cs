using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

public interface IPatternMatcherResolver
{
    IPatternMatcher Resolve(AobPattern pattern);
}
