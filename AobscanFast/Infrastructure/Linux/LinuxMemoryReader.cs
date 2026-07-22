using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

public sealed class LinuxMemoryReader : IMemoryAccessor, IMemoryRegionEnumerator
{
    private readonly RemoteProcessMemoryAccessor _memoryAccessor;
    private readonly RemoteProcessRegionEnumerator _regionEnumerator;

    public LinuxMemoryReader(SafeHandle processHandle)
    {
        ArgumentNullException.ThrowIfNull(processHandle);
        _memoryAccessor = new RemoteProcessMemoryAccessor(processHandle);
        _regionEnumerator = new RemoteProcessRegionEnumerator(processHandle);
    }

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
        => _regionEnumerator.GetRegions(minAddress, maxAddress, access);

    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
        => _memoryAccessor.ReadMemory(baseAddress, buffer, out bytesRead);

    public bool WriteMemory(nint baseAddress, ReadOnlySpan<byte> buffer, out nuint bytesWritten)
        => _memoryAccessor.WriteMemory(baseAddress, buffer, out bytesWritten);
}
