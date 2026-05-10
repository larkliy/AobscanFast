using AobscanFast.Core.Interfaces;

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

        buffer.CopyTo(new Span<byte>(baseAddress.ToPointer(), buffer.Length));
        bytesWritten = (nuint)buffer.Length;
        return true;
    }
}
