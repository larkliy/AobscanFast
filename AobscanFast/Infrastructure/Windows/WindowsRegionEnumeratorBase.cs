using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using Windows.Win32.System.Memory;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Base class for enumerating Windows process memory regions.</summary>
internal abstract class WindowsRegionEnumeratorBase : IMemoryRegionEnumerator
{
    /// <summary>Queries memory information at the specified address.</summary>
    protected abstract nuint QueryMemory(nint address, out MEMORY_BASIC_INFORMATION memoryInfo);

    /// <inheritdoc/>
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        nint currentAddress = minAddress;
        var regions = new List<MemoryRange>(256);

        while (currentAddress < maxAddress)
        {
            if (QueryMemory(currentAddress, out var memoryInfo) == 0)
                break;

            unsafe
            {
                nint regionStart = (nint)memoryInfo.BaseAddress;
                nint regionEnd = checked(regionStart + (nint)memoryInfo.RegionSize);
                nint scanStart = Math.Max(regionStart, minAddress);
                nint scanEnd = Math.Min(regionEnd, maxAddress);

                if (scanEnd > scanStart && WindowsMemoryProtectionEvaluator.IsScannable(memoryInfo, access))
                    regions.Add(new MemoryRange(scanStart, scanEnd - scanStart));

                if (regionEnd <= currentAddress)
                    break;

                currentAddress = regionEnd;
            }
        }

        return regions;
    }
}
