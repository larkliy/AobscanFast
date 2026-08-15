using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

/// <summary>Matches a compiled pattern against a memory chunk.</summary>
public interface IPatternMatcher
{
    /// <summary>Scans a buffer and appends match addresses to the result list.</summary><param name="range">The address range represented by <paramref name="buffer"/>.</param><param name="pattern">The pattern to match.</param><param name="results">The destination result list.</param><param name="buffer">The bytes read for the range.</param><param name="maxResults">The maximum results to append, or zero for no limit.</param>
    void ScanChunk(in MemoryRange range, AobPattern pattern,
                   List<nint> results, ReadOnlySpan<byte> buffer, int maxResults = 0);
}
