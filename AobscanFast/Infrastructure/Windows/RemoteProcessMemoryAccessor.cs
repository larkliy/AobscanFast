using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace AobscanFast.Infrastructure.Windows;

public sealed class RemoteProcessMemoryAccessor(SafeHandle processHandle) : IMemoryAccessor
{
    public unsafe bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
        => PInvoke.ReadProcessMemory(processHandle, baseAddress.ToPointer(), buffer, out bytesRead);

    public unsafe bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
        => PInvoke.WriteProcessMemory(processHandle, baseAddress.ToPointer(), buffer, out bytesWritten);
}
