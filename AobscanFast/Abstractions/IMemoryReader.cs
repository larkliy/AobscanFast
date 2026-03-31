using AobscanFast.Core.Models;
using AobscanFast.Infrastructure;

namespace AobscanFast.Abstractions;

public interface IMemoryReader
{
    List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);

    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
}