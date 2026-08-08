using System.Globalization;
using System.Runtime.InteropServices;
using AobscanFast.Core.Interfaces;

namespace AobscanFast.Infrastructure.Linux;

/// <summary>Provides process and module operations using Linux procfs.</summary>
public class LinuxProcessHandler : IProcessHandler
{
    /// <inheritdoc/>
    public uint? FindIdByName(string processName, int index = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);

        int matchIndex = 0;

        foreach (string procDir in Directory.EnumerateDirectories("/proc"))
        {
            ReadOnlySpan<char> dirName = Path.GetFileName(procDir.AsSpan());

            if (!uint.TryParse(dirName, NumberStyles.None, null, out uint pid))
                continue;

            string commPath = $"/proc/{pid}/comm";

            string? comm;
            try
            {
                comm = File.ReadAllText(commPath)?.Trim();
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrEmpty(comm))
                continue;

            if (comm.Equals(processName, StringComparison.OrdinalIgnoreCase))
            {
                if (matchIndex == index)
                    return pid;

                matchIndex++;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public unsafe (nint BaseAddress, uint Size)? GetModuleInfo(uint processId, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        string mapsPath = $"/proc/{processId}/maps";
        nint? baseAddress = null;
        nint lastEnd = 0;

        try
        {
            string[] lines = File.ReadAllLines(mapsPath);

            foreach (string line in lines)
            {
                ReadOnlySpan<char> span = line.AsSpan();

                int pathIdx = FindPathStart(span);
                if (pathIdx < 0) continue;

                string pathLine = span[pathIdx..].Trim().ToString();
                if (pathLine.Length == 0) continue;

                string fileName = Path.GetFileName(pathLine);
                if (!fileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    continue;

                int dashIdx = span.IndexOf('-');
                if (dashIdx < 0) continue;

                int spaceAfterEnd = span.Slice(dashIdx + 1).IndexOf(' ');
                if (spaceAfterEnd < 0) continue;

                if (!long.TryParse(span[..dashIdx], NumberStyles.HexNumber, null, out long start))
                    continue;

                if (!long.TryParse(span.Slice(dashIdx + 1, spaceAfterEnd), NumberStyles.HexNumber, null, out long end))
                    continue;

                if (baseAddress is null)
                    baseAddress = (nint)start;

                if (end > lastEnd)
                    lastEnd = (nint)end;
            }
        }
        catch
        {
            return null;
        }

        if (baseAddress is null)
            return null;

        return (baseAddress.Value, (uint)(lastEnd - baseAddress.Value));
    }

    /// <inheritdoc/>
    public SafeHandle OpenProcess(uint processId)
    {
        string path = $"/proc/{processId}/mem";
        int fd = NativeMethods.open(path, NativeMethods.O_RDWR);

        if (fd < 0)
        {
            int error = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException($"Failed to open {path} (errno={error}). Ensure the target process is accessible.");
        }

        return new SafeProcessHandle(fd, processId);
    }

    private static int FindPathStart(ReadOnlySpan<char> line)
    {
        int spaceCount = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ' ')
            {
                spaceCount++;
                if (spaceCount == 4)
                {
                    int pathStart = i + 1;
                    while (pathStart < line.Length && line[pathStart] == ' ')
                        pathStart++;
                    return pathStart < line.Length ? pathStart : -1;
                }
            }
        }
        return -1;
    }
}
