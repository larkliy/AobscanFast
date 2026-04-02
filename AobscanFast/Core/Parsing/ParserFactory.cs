

using AobscanFast.Core.Interfaces;

namespace AobscanFast.Core.Parsing;

internal static class ParserFactory
{
    private static readonly SolidParser s_solid = new();
    private static readonly MaskParser s_mask = new();
    private static readonly HalfMaskParser s_halfMask = new();

    public static IPatternParser GetParser(string input)
    {
        if (!input.Contains('?'))
            return s_solid;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '?')
            {
                bool isHalfMask =
                    (i > 0 && input[i - 1] != ' ' && input[i - 1] != '?') ||
                    (i < input.Length - 1 && input[i + 1] != ' ' && input[i + 1] != '?');

                if (isHalfMask)
                    return s_halfMask;
            }
        }

        return s_mask;
    }
}