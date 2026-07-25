using AobscanFast.Core.Interfaces;

namespace AobscanFast.Core.Parsing;

internal sealed class PatternParserResolver : IPatternParserResolver
{
    private readonly IPatternParser[] _parsers;

    public PatternParserResolver()
        : this([new SolidParser(), new HalfMaskParser(), new MaskParser()])
    {
    }

    public PatternParserResolver(IEnumerable<IPatternParser> parsers)
    {
        _parsers = (parsers ?? throw new ArgumentNullException(nameof(parsers)))
            .Where(static p => p is not null)
            .ToArray();
    }

    public IPatternParser Resolve(string patternText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patternText);

        foreach (var parser in _parsers)
        {
            if (parser.CanParse(patternText))
                return parser;
        }

        throw new NotSupportedException($"No parser registered that can handle pattern '{patternText}'.");
    }
}
