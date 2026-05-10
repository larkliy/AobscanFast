using AobscanFast.Core.Interfaces;

namespace AobscanFast.Core.Parsing;

internal sealed class PatternParserResolver : IPatternParserResolver
{
    private static readonly SolidParser s_solidParser = new();
    private static readonly MaskParser s_maskParser = new();
    private static readonly HalfMaskParser s_halfMaskParser = new();

    public IPatternParser Resolve(string patternText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patternText);

        if (!patternText.Contains('?'))
            return s_solidParser;

        for (int i = 0; i < patternText.Length; i++)
        {
            if (patternText[i] != '?')
                continue;

            bool isHalfMask =
                (i > 0 && patternText[i - 1] != ' ' && patternText[i - 1] != '?') ||
                (i < patternText.Length - 1 && patternText[i + 1] != ' ' && patternText[i + 1] != '?');

            if (isHalfMask)
                return s_halfMaskParser;
        }

        return s_maskParser;
    }
}
