using AobscanFast.Core.Abstractions;
using AobscanFast.Core.Models;
using AobscanFast.Enums.Windows;
using AobscanFast.Utils;

namespace AobscanFast.Core.Implementations.Unix;

internal class UnixMemoryReader(ProcessInfo processInfo) : IMemoryReader
{
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
        => throw new NotImplementedException();

    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        bytesRead = (nuint)Native.pread((int)processInfo.ProcessHandle, buffer, (nuint)buffer.Length, baseAddress);
        return (int)bytesRead == buffer.Length;
    }
}
