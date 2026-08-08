namespace AobscanFast.Core.Interfaces;

/// <summary>Selects a parser for AOB pattern text.</summary>
public interface IPatternParserResolver
{
    /// <summary>Resolves the parser that accepts the supplied text.</summary><param name="patternText">The pattern text.</param><returns>A compatible parser.</returns>
    IPatternParser Resolve(string patternText);
}
