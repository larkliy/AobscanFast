using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

/// <summary>Parses one supported textual pattern format.</summary>
public interface IPatternParser
{
    /// <summary>Determines whether this parser accepts the input format.</summary><param name="input">The pattern text.</param><returns><see langword="true"/> when this parser can parse the input.</returns>
    bool CanParse(string input);
    /// <summary>Parses pattern text.</summary><param name="input">The pattern text.</param><returns>The compiled pattern.</returns>
    AobPattern Parse(string input);
}
