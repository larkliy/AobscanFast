namespace AobscanFast.Core.Interfaces;

public interface IMemoryAccessor
{
    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
    bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten);
}
