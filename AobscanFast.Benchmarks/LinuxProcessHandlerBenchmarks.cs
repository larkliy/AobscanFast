using BenchmarkDotNet.Attributes;
using System.IO;

namespace AobscanFast.Benchmarks;

[MemoryDiagnoser]
public class LinuxProcessHandlerBenchmarks
{
    private string[] _lines = null!;
    private string _moduleName = "libc.so.6";

    [GlobalSetup]
    public void Setup()
    {
        _lines = new[]
        {
            "7f9a15000000-7f9a15200000 r-xp 00000000 08:02 131072 /usr/lib/libc.so.6",
            "7f9a15200000-7f9a15400000 ---p 00200000 08:02 131072 /usr/lib/libc.so.6",
            "7f9a15400000-7f9a15404000 r--p 00200000 08:02 131072 /usr/lib/libc.so.6",
            "7f9a15404000-7f9a15406000 rw-p 00204000 08:02 131072 /usr/lib/libc.so.6",
            "7f9a15406000-7f9a1540a000 rw-p 00000000 00:00 0",
            "7f9a1540a000-7f9a1540b000 r--p 00000000 08:02 131072 /usr/lib/libc.so.6 (deleted)"
        };
        var list = new System.Collections.Generic.List<string>();
        for (int i=0; i<100; i++) list.AddRange(_lines);
        _lines = list.ToArray();
    }

    [Benchmark(Baseline = true)]
    public int ParseString()
    {
        int count = 0;
        foreach (string line in _lines)
        {
            System.ReadOnlySpan<char> span = line.AsSpan();
            int pathIdx = FindPathStart(span);
            if (pathIdx < 0) continue;

            string pathLine = span[pathIdx..].Trim().ToString();
            if (pathLine.Length == 0) continue;

            const string deletedSuffix = " (deleted)";
            if (pathLine.EndsWith(deletedSuffix, System.StringComparison.Ordinal))
                pathLine = pathLine[..^deletedSuffix.Length];

            string fileName = System.IO.Path.GetFileName(pathLine);
            if (!fileName.Equals(_moduleName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            int dashIdx = span.IndexOf('-');
            if (dashIdx < 0) continue;

            int spaceAfterEnd = span.Slice(dashIdx + 1).IndexOf(' ');
            if (spaceAfterEnd < 0) continue;

            if (!long.TryParse(span[..dashIdx], System.Globalization.NumberStyles.HexNumber, null, out long start))
                continue;

            if (!long.TryParse(span.Slice(dashIdx + 1, spaceAfterEnd), System.Globalization.NumberStyles.HexNumber, null, out long end))
                continue;

            count++;
        }
        return count;
    }

    [Benchmark]
    public int ParseSpan()
    {
        int count = 0;
        foreach (string line in _lines)
        {
            System.ReadOnlySpan<char> span = line.AsSpan();
            int pathIdx = FindPathStart(span);
            if (pathIdx < 0) continue;

            System.ReadOnlySpan<char> pathLine = span[pathIdx..].Trim();
            if (pathLine.Length == 0) continue;

            System.ReadOnlySpan<char> deletedSuffix = " (deleted)";
            if (pathLine.EndsWith(deletedSuffix, System.StringComparison.Ordinal))
                pathLine = pathLine[..^deletedSuffix.Length];

            System.ReadOnlySpan<char> fileName = System.IO.Path.GetFileName(pathLine);
            if (!fileName.Equals(_moduleName.AsSpan(), System.StringComparison.OrdinalIgnoreCase))
                continue;

            int dashIdx = span.IndexOf('-');
            if (dashIdx < 0) continue;

            int spaceAfterEnd = span.Slice(dashIdx + 1).IndexOf(' ');
            if (spaceAfterEnd < 0) continue;

            if (!long.TryParse(span[..dashIdx], System.Globalization.NumberStyles.HexNumber, null, out long start))
                continue;

            if (!long.TryParse(span.Slice(dashIdx + 1, spaceAfterEnd), System.Globalization.NumberStyles.HexNumber, null, out long end))
                continue;

            count++;
        }
        return count;
    }

    private static int FindPathStart(System.ReadOnlySpan<char> line)
    {
        int position = 0;
        for (int field = 0; field < 5; field++)
        {
            while (position < line.Length && char.IsWhiteSpace(line[position]))
                position++;
            while (position < line.Length && !char.IsWhiteSpace(line[position]))
                position++;
        }

        while (position < line.Length && char.IsWhiteSpace(line[position]))
            position++;

        return position < line.Length ? position : -1;
    }
}
