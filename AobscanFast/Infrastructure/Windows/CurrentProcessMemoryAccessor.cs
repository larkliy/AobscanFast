using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Reads and writes the current Windows process address space.</summary>
public sealed unsafe class CurrentProcessMemoryAccessor : IMemoryAccessor, ISelfProcessMemoryAccessor
{
    private static readonly SafeProcessHandle CurrentProcessHandle =
        new((nint)PInvoke.GetCurrentProcess().Value, ownsHandle: false);

    /// <inheritdoc/>
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

        return PInvoke.ReadProcessMemory(CurrentProcessHandle, baseAddress.ToPointer(), buffer, out bytesRead);
    }

    /// <inheritdoc/>
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

        return PInvoke.WriteProcessMemory(CurrentProcessHandle, baseAddress.ToPointer(), buffer, out bytesWritten);
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
