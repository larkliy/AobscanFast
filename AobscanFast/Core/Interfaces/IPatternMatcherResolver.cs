using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

/// <summary>Selects a matcher optimized for a compiled pattern.</summary>
public interface IPatternMatcherResolver
{
    /// <summary>Resolves a matcher for the supplied pattern.</summary><param name="pattern">The compiled pattern.</param><returns>A compatible matcher.</returns>
    IPatternMatcher Resolve(AobPattern pattern);
}
