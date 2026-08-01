using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
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

        if (!IsMemoryAccessible(baseAddress, buffer.Length, MemoryAccess.Readable))
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

        if (!IsMemoryAccessible(baseAddress, buffer.Length, MemoryAccess.Writable))
        {
            bytesWritten = 0;
            return false;
        }

        buffer.CopyTo(new Span<byte>(baseAddress.ToPointer(), buffer.Length));
        bytesWritten = (nuint)buffer.Length;
        return true;
    }

    private static bool IsMemoryAccessible(nint address, int length, MemoryAccess requiredAccess)
    {
        ulong current = (ulong)address;
        ulong requestedEnd = checked(current + (ulong)length);

        while (current < requestedEnd)
        {
            if (PInvoke.VirtualQuery((void*)current, out var mbi) == 0)
                return false;

            if (!WindowsMemoryProtectionEvaluator.IsScannable(mbi, requiredAccess))
                return false;

            ulong regionStart = (ulong)mbi.BaseAddress;
            ulong regionEnd = checked(regionStart + mbi.RegionSize);
            if (current < regionStart || regionEnd <= current)
                return false;

            current = Math.Min(regionEnd, requestedEnd);
        }

        return true;
    }
}
