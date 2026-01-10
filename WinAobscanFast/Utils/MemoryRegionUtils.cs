using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using WinAobscanFast.Core.Models;
using WinAobscanFast.Enums;
using WinAobscanFast.Core.Extensions;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Utils;

public class MemoryRegionUtils
{
    public static List<MemoryRange> GetRegions(SafeProcessHandle processHandle,
                                               MemoryAccess accessFilter,
                                               nint searchStart,
                                               nint searchEnd)
    {
        nint currentAddress = searchStart;
        var regions = new List<MemoryRange>(256);
        int mbiSize = Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (currentAddress < searchEnd)
        {
            if (Native.VirtualQueryEx(processHandle, currentAddress, out var mbi, mbiSize) == 0)
                break;

            nint regionStart = mbi.BaseAddress;
            nint regionSize = mbi.RegionSize;
            nint regionEnd = regionStart + regionSize;

            bool isCommit = mbi.State == MemoryState.MEM_COMMIT;
            bool isGuard = (mbi.Protect & MemoryProtect.PAGE_GUARD) != 0;
            bool isNoAccess = (mbi.Protect & MemoryProtect.PAGE_NOACCESS) != 0;

            if (isCommit && !isGuard && !isNoAccess)
            {
                if (CheckAccess(ref mbi, accessFilter))
                {
                    nint realStart = regionStart < searchStart ? searchStart : regionStart;
                    nint realEnd = regionEnd > searchEnd ? searchEnd : regionEnd;

                    nint realSize = realEnd - realStart;

                    if (realSize > 0)
                    {
                        regions.Add(new MemoryRange(realStart, realSize));
                    }
                }
            }

            currentAddress = regionEnd;

            if (regionSize == 0) break;
        }

        return regions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckAccess(ref MEMORY_BASIC_INFORMATION mbi, MemoryAccess accessFilter)
    {
        if ((accessFilter & MemoryAccess.Readable) != 0 && !mbi.IsReadableRegion()) return false;
        if ((accessFilter & MemoryAccess.Writable) != 0 && !mbi.IsWritableRegion()) return false;
        if ((accessFilter & MemoryAccess.Executable) != 0 && !mbi.IsExecutableRegion()) return false;

        return true;
    }
}
