using AobscanFast.Core.Interfaces;
using Windows.Win32;
using Windows.Win32.System.Memory;

using static Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS;
using static Windows.Win32.System.Memory.VIRTUAL_ALLOCATION_TYPE;

namespace AobscanFast.Infrastructure.Windows;

public sealed unsafe class CurrentProcessMemoryAccessor : IMemoryAccessor
{
    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        if (buffer.IsEmpty)
        {
            bytesRead = 0;
            return true;
        }

        if (baseAddress == 0)
        {
            bytesRead = 0;
            return false;
        }

        if (!IsMemoryReadable(baseAddress, buffer.Length))
        {
            bytesRead = 0;
            return false;
        }

        new ReadOnlySpan<byte>(baseAddress.ToPointer(), buffer.Length).CopyTo(buffer);
        bytesRead = (nuint)buffer.Length;
        return true;
    }

    public bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
    {
        if (buffer.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        if (baseAddress == 0)
        {
            bytesWritten = 0;
            return false;
        }

        if (!IsMemoryReadable(baseAddress, buffer.Length))
        {
            bytesWritten = 0;
            return false;
        }

        buffer.CopyTo(new Span<byte>(baseAddress.ToPointer(), buffer.Length));
        bytesWritten = (nuint)buffer.Length;
        return true;
    }

    private static bool IsMemoryReadable(nint address, int length)
    {
        if (PInvoke.VirtualQuery(address.ToPointer(), out var mbi) == 0)
            return false;

        if (mbi.State != MEM_COMMIT)
            return false;

        var protect = mbi.Protect;
        if ((protect & PAGE_NOACCESS) != 0)
            return false;

        if ((protect & PAGE_GUARD) != 0)
            return false;

        ulong regionEnd = (ulong)mbi.BaseAddress + mbi.RegionSize;
        ulong requestedEnd = (ulong)address + (ulong)length;

        return requestedEnd <= regionEnd;
    }
}
