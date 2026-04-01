using AobscanFast.Core.Models;

namespace AobscanFast.Core.Interfaces;

public interface IMemoryReader
{
    List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);

    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
}