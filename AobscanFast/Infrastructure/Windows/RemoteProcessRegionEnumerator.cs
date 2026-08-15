using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Enumerates accessible regions in a remote Windows process.</summary>
public sealed class RemoteProcessRegionEnumerator : IMemoryRegionEnumerator
{
    private readonly SafeHandle _processHandle;

    /// <summary>Initializes an enumerator for an open Windows process handle.</summary>
    public RemoteProcessRegionEnumerator(SafeHandle processHandle)
    {
        ArgumentNullException.ThrowIfNull(processHandle);
        if (processHandle.IsInvalid || processHandle.IsClosed)
            throw new ArgumentException("A valid open process handle is required.", nameof(processHandle));

        _processHandle = processHandle;
    }

    /// <inheritdoc/>
    public unsafe List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        nint currentAddress = minAddress;
        var regions = new List<MemoryRange>(256);

        while (currentAddress < maxAddress)
        {
            if (PInvoke.VirtualQueryEx(_processHandle, currentAddress.ToPointer(), out var memoryInfo) == 0)
                break;

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

        return regions;
    }
}
