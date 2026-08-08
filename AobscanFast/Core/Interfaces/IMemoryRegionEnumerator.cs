using AobscanFast.Core.Models;

namespace AobscanFast.Core.Interfaces;

/// <summary>Enumerates process memory ranges matching requested access flags.</summary>
public interface IMemoryRegionEnumerator
{
    /// <summary>Gets ranges between the supplied addresses that satisfy the requested access.</summary>
    /// <param name="minAddress">The inclusive lower address.</param><param name="maxAddress">The exclusive upper address.</param><param name="access">Required memory access flags.</param>
    /// <returns>A list of scannable memory ranges.</returns>
    List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);
}
