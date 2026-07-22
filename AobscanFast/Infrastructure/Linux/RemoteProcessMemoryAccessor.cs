using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

public sealed class RemoteProcessMemoryAccessor : IMemoryAccessor
{
    private readonly int _fd;

    public RemoteProcessMemoryAccessor(SafeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        _fd = (int)handle.DangerousGetHandle();
    }

    public unsafe bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        if (buffer.IsEmpty)
        {
            bytesRead = 0;
            return true;
        }

        fixed (byte* buf = buffer)
        {
            long result = NativeMethods.pread(_fd, buf, (nuint)buffer.Length, (long)baseAddress);
            if (result < 0)
            {
                bytesRead = 0;
                return false;
            }

            bytesRead = (nuint)result;
            return true;
        }
    }

    public unsafe bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
    {
        if (buffer.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        fixed (byte* buf = buffer)
        {
            long result = NativeMethods.pwrite(_fd, buf, (nuint)buffer.Length, (long)baseAddress);
            if (result < 0)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = (nuint)result;
            return true;
        }
    }
}
