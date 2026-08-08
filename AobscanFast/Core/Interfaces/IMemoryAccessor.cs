namespace AobscanFast.Core.Interfaces;

/// <summary>Reads and writes virtual memory for a process.</summary>
public interface IMemoryAccessor
{
    /// <summary>Reads memory into a caller-provided buffer.</summary>
    /// <param name="baseAddress">The address to read from.</param><param name="buffer">The destination buffer.</param><param name="bytesRead">The number of bytes actually read, including a partial prefix.</param>
    /// <returns><see langword="true"/> when the native operation succeeds; implementations may return <see langword="false"/> for an incomplete operation.</returns>
    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
    /// <summary>Writes bytes to virtual memory.</summary>
    /// <param name="baseAddress">The address to write to.</param><param name="buffer">The source bytes.</param><param name="bytesWritten">The number of bytes actually written.</param>
    /// <returns><see langword="true"/> when the native operation succeeds.</returns>
    bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten);
}
