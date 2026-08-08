using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Combines remote Windows memory access and region enumeration.</summary>
public sealed class WinMemoryReader : IMemoryAccessor, IMemoryRegionEnumerator
{
    private readonly RemoteProcessMemoryAccessor _memoryAccessor;
    private readonly RemoteProcessRegionEnumerator _regionEnumerator;

    /// <summary>Initializes a reader for an open Windows process handle.</summary><param name="processHandle">The process handle.</param>
    public WinMemoryReader(SafeHandle processHandle)
    {
        _memoryAccessor = new RemoteProcessMemoryAccessor(processHandle);
        _regionEnumerator = new RemoteProcessRegionEnumerator(processHandle);
    }

    /// <inheritdoc/>
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
        => _regionEnumerator.GetRegions(minAddress, maxAddress, access);

    /// <inheritdoc/>
    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
        => _memoryAccessor.ReadMemory(baseAddress, buffer, out bytesRead);

    /// <inheritdoc/>
    public bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
        => _memoryAccessor.WriteMemory(baseAddress, buffer, out bytesWritten);
}
