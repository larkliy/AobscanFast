using AobscanFast.Core.Models;

namespace AobscanFast.Core.Interfaces;

public interface IMemoryRegionEnumerator
{
    List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);
}
