using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using WinAobscanFast.Enums;
using WinAobscanFast.Extensions;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Utils;

public class MemoryRegionUtils
{
    internal static List<MEMORY_BASIC_INFORMATION> GetRegions(SafeProcessHandle processHandle,
                                                              MemoryAccess accessFilter,
                                                              nint searchStart,
                                                              nint searchEnd)
    {
        nint address = 0;
        var regions = new List<MEMORY_BASIC_INFORMATION>();

        while (address < searchEnd)
        {
            if (Native.VirtualQueryEx(processHandle, address, out var mbi, Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>()) == 0)
                break;

            nint regionStart = mbi.BaseAddress;
            nint regionSize = mbi.RegionSize;
            nint regionEnd = regionStart + regionSize;

            bool isOverlapping = regionEnd > searchStart && regionStart < searchEnd;

            if (isOverlapping)
            {
                bool isValidState = mbi.State == MemoryState.MEM_COMMIT;

                bool isNotGuard = (mbi.Protect & MemoryProtect.PAGE_GUARD) == 0;
                bool isNotNoAccess = (mbi.Protect & MemoryProtect.PAGE_NOACCESS) == 0;

                if (isValidState && isNotGuard && isNotNoAccess)
                {
                    bool meetsFilter = true;
                    bool isReadable = mbi.IsReadableRegion();
                    bool isWritable = mbi.IsWritableRegion();
                    bool isExecutable = mbi.IsExecutableRegion();

                    if (accessFilter.HasFlag(MemoryAccess.Readable) && !isReadable) meetsFilter = false;
                    if (accessFilter.HasFlag(MemoryAccess.Writable) && !isWritable) meetsFilter = false;
                    if (accessFilter.HasFlag(MemoryAccess.Executable) && !isExecutable) meetsFilter = false;

                    if (meetsFilter)
                    {
                        regions.Add(mbi);
                    }
                }
            }

            address = regionEnd;
        }

        return regions;
    }
}
