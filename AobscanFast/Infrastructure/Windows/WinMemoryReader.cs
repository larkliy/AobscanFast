using AobscanFast.Services;
using AobscanFast.Abstractions;
using AobscanFast.Extensions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Memory;

using static Windows.Win32.System.Memory.VIRTUAL_ALLOCATION_TYPE;
using static Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS;
using AobscanFast.Core.Models;

namespace AobscanFast.Infrastructure.Windows;

public unsafe class WinMemoryReader(SafeHandle processHandle) : IMemoryReader
{
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        nint currentAddress = minAddress;
        var regions = new List<MemoryRange>(256);
        nint mbiSize = Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (currentAddress < maxAddress)
        {
            if (PInvoke.VirtualQueryEx(processHandle, currentAddress.ToPointer(), out var mbi) == 0)
                break;

            bool isCommit = mbi.State == MEM_COMMIT;
            bool isGuard = (mbi.Protect & PAGE_GUARD) != 0;
            bool isNoAccess = (mbi.Protect & PAGE_NOACCESS) != 0;

            if (isCommit && !isGuard && !isNoAccess)
                if (CheckAccess(ref mbi, access))
                    regions.Add(new MemoryRange((nint)mbi.BaseAddress, (nint)mbi.RegionSize));

            currentAddress = (nint)mbi.BaseAddress + (nint)mbi.RegionSize;
        }

        return regions;
    }

    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        return PInvoke.ReadProcessMemory(processHandle, baseAddress.ToPointer(), buffer, out bytesRead);
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
