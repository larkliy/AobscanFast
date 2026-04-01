using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Matching;

internal class SolidMatcher : IPatternMatcher
{
    public void ScanChunk(in MemoryRange range, AobPattern pattern, List<nint> results, ReadOnlySpan<byte> buffer)
    {
        int currentOffset = 0;
        var remaining = buffer;

        while (true)
        {
            int hitIndex;
            if ((hitIndex = remaining.IndexOf(pattern.Bytes)) == -1)
                break;

            results.Add(range.BaseAddress + currentOffset + hitIndex);

            int advance = hitIndex + 1;
            currentOffset += advance;
            remaining = buffer[currentOffset..];
        }
    }
}
