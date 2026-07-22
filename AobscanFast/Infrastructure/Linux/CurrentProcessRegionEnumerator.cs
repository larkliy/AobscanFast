using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;

namespace AobscanFast.Infrastructure.Linux;

public sealed class CurrentProcessRegionEnumerator : IMemoryRegionEnumerator
{
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return LinuxMapsParser.Parse("/proc/self/maps", minAddress, maxAddress, access);
    }
}
