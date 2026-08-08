using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

/// <summary>Reads and writes a remote Linux process through its process handle.</summary>
public sealed class RemoteProcessMemoryAccessor : IMemoryAccessor
{
    private readonly SafeHandle _handle;

    /// <summary>Initializes an accessor for an open Linux process handle.</summary><param name="handle">The process handle.</param>
    public RemoteProcessMemoryAccessor(SafeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (handle.IsInvalid || handle.IsClosed)
            throw new ArgumentException("A valid open process handle is required.", nameof(handle));

        _handle = handle;
    }

    /// <inheritdoc/>
    public unsafe bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        if (buffer.IsEmpty)
        {
            bytesRead = 0;
            return true;
        }

        fixed (byte* buf = buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                long result = NativeMethods.pread(
                    _handle,
                    buf + total,
                    (nuint)(buffer.Length - total),
                    checked((long)baseAddress + total));

                if (result > 0)
                {
                    total = checked(total + (int)result);
                    continue;
                }

                if (result < 0 && Marshal.GetLastPInvokeError() == NativeMethods.EINTR)
                    continue;

                bytesRead = (nuint)total;
                return false;
            }

            bytesRead = (nuint)total;
            return true;
        }
    }

    /// <inheritdoc/>
    public unsafe bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
    {
        if (buffer.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        fixed (byte* buf = buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                long result = NativeMethods.pwrite(
                    _handle,
                    buf + total,
                    (nuint)(buffer.Length - total),
                    checked((long)baseAddress + total));

                if (result > 0)
                {
                    total = checked(total + (int)result);
                    continue;
                }

                if (result < 0 && Marshal.GetLastPInvokeError() == NativeMethods.EINTR)
                    continue;

                bytesWritten = (nuint)total;
                return false;
            }

            bytesWritten = (nuint)total;
            return true;
        }
    }
}
