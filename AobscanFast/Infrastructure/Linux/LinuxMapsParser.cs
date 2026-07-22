using System.Globalization;
using AobscanFast.Core.Models;

namespace AobscanFast.Infrastructure.Linux;

internal static class LinuxMapsParser
{
    public static List<MemoryRange> Parse(string mapsPath, nint minAddress, nint maxAddress, MemoryAccess access)
    {
        var regions = new List<MemoryRange>(256);
        string[] lines = File.ReadAllLines(mapsPath);

        foreach (string line in lines)
        {
            ReadOnlySpan<char> span = line.AsSpan();

            int dashIdx = span.IndexOf('-');
            if (dashIdx < 0) continue;

            int spaceAfterEnd = span.Slice(dashIdx + 1).IndexOf(' ');
            if (spaceAfterEnd < 0) continue;

            if (!long.TryParse(span[..dashIdx], NumberStyles.HexNumber, null, out long start))
                continue;

            if (!long.TryParse(span.Slice(dashIdx + 1, spaceAfterEnd), NumberStyles.HexNumber, null, out long end))
                continue;

            if (start >= maxAddress || end <= minAddress)
                continue;

            int permsStart = dashIdx + 1 + spaceAfterEnd + 1;
            if (permsStart + 3 >= span.Length) continue;

            if ((access & MemoryAccess.Readable) != 0 && span[permsStart] != 'r')
                continue;

            if ((access & MemoryAccess.Writable) != 0 && span[permsStart + 1] != 'w')
                continue;

            if ((access & MemoryAccess.Executable) != 0 && span[permsStart + 2] != 'x')
                continue;

            nint regionStart = (nint)Math.Max(start, minAddress);
            nint regionEnd = (nint)Math.Min(end, maxAddress);
            nint regionSize = regionEnd - regionStart;

            if (regionSize > 0)
                regions.Add(new MemoryRange(regionStart, regionSize));
        }

        return regions;
    }
}
