using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;

namespace AobscanFast.Infrastructure.Linux;

/// <summary>Enumerates mapped regions in the current Linux process.</summary>
public sealed class CurrentProcessRegionEnumerator : IMemoryRegionEnumerator
{
    /// <inheritdoc/>
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return LinuxMapsParser.Parse("/proc/self/maps", minAddress, maxAddress, access);
    }
}
