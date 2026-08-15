using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Reads and writes a remote Windows process through an open process handle.</summary>
public sealed class RemoteProcessMemoryAccessor : IMemoryAccessor
{
    private readonly SafeHandle _processHandle;

    /// <summary>Initializes an accessor for an open Windows process handle.</summary>
    public RemoteProcessMemoryAccessor(SafeHandle processHandle)
    {
        ArgumentNullException.ThrowIfNull(processHandle);
        if (processHandle.IsInvalid || processHandle.IsClosed)
            throw new ArgumentException("A valid open process handle is required.", nameof(processHandle));

        _processHandle = processHandle;
    }

    /// <inheritdoc/>
    public unsafe bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
        => PInvoke.ReadProcessMemory(_processHandle, baseAddress.ToPointer(), buffer, out bytesRead);

    /// <inheritdoc/>
    public unsafe bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
        => PInvoke.WriteProcessMemory(_processHandle, baseAddress.ToPointer(), buffer, out bytesWritten);
}
