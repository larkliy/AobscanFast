using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

/// <summary>Reads and writes the current Linux process address space.</summary>
public sealed unsafe class CurrentProcessMemoryAccessor : IMemoryAccessor, ISelfProcessMemoryAccessor
{
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

        fixed (byte* destination = buffer)
        {
            var local = new NativeMethods.IoVector
            {
                Base = destination,
                Length = (nuint)buffer.Length
            };
            var remote = new NativeMethods.IoVector
            {
                Base = baseAddress.ToPointer(),
                Length = (nuint)buffer.Length
            };

            nint result;
            do
            {
                result = NativeMethods.process_vm_readv(
                    Environment.ProcessId,
                    &local,
                    1,
                    &remote,
                    1,
                    0);
            } while (result < 0 && Marshal.GetLastPInvokeError() == NativeMethods.EINTR);

            bytesRead = result > 0 ? (nuint)result : 0;
            return result == buffer.Length;
        }
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

        fixed (byte* source = buffer)
        {
            var local = new NativeMethods.IoVector
            {
                Base = source,
                Length = (nuint)buffer.Length
            };
            var remote = new NativeMethods.IoVector
            {
                Base = baseAddress.ToPointer(),
                Length = (nuint)buffer.Length
            };

            nint result;
            do
            {
                result = NativeMethods.process_vm_writev(
                    Environment.ProcessId,
                    &local,
                    1,
                    &remote,
                    1,
                    0);
            } while (result < 0 && Marshal.GetLastPInvokeError() == NativeMethods.EINTR);

            bytesWritten = result > 0 ? (nuint)result : 0;
            return result == buffer.Length;
        }
    }
}
