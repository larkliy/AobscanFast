using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace AobscanFast.Infrastructure.Windows;

public sealed class RemoteProcessRegionEnumerator(SafeHandle processHandle) : IMemoryRegionEnumerator
{
    public unsafe List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        nint currentAddress = minAddress;
        var regions = new List<MemoryRange>(256);

        while (currentAddress < maxAddress)
        {
            if (PInvoke.VirtualQueryEx(processHandle, currentAddress.ToPointer(), out var memoryInfo) == 0)
                break;

            if (WindowsMemoryProtectionEvaluator.IsScannable(memoryInfo, access))
                regions.Add(new MemoryRange((nint)memoryInfo.BaseAddress, (nint)memoryInfo.RegionSize));

            currentAddress = (nint)memoryInfo.BaseAddress + (nint)memoryInfo.RegionSize;
        }

        return regions;
    }
}
