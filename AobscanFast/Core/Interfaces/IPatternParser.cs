using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

public interface IPatternParser
{
    bool CanParse(string input);
    AobPattern Parse(string input);
}
