using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

public interface IPatternParser
{
    AobPattern Parse(string input);
}
