namespace AobscanFast.Core.Interfaces;

public interface IPatternParserResolver
{
    IPatternParser Resolve(string patternText);
}
