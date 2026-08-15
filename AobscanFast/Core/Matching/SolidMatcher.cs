using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Matching;

internal sealed class SolidMatcher : IPatternMatcher
{
    public void ScanChunk(in MemoryRange range, AobPattern pattern, List<nint> results, ReadOnlySpan<byte> buffer, int maxResults = 0)
    {
        int currentOffset = 0;
        var remaining = buffer;

        while (true)
        {
            int hitIndex;
            if ((hitIndex = remaining.IndexOf(pattern.BytesSpan)) == -1)
                break;

            results.Add(range.BaseAddress + currentOffset + hitIndex);
            if (maxResults > 0 && results.Count >= maxResults)
                break;

            int advance = hitIndex + 1;
            currentOffset += advance;
            remaining = buffer[currentOffset..];
        }
    }
}
