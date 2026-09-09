using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Enumerates committed, accessible regions in the current Windows process.</summary>
public sealed class CurrentProcessRegionEnumerator : IMemoryRegionEnumerator
{
    /// <inheritdoc/>
    public unsafe List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return new InternalEnumerator().GetRegions(minAddress, maxAddress, access);
    }

    private sealed class InternalEnumerator : WindowsRegionEnumeratorBase
    {
        protected override unsafe nuint QueryMemory(void* address, out MEMORY_BASIC_INFORMATION memoryInfo)
        {
            return PInvoke.VirtualQuery(address, out memoryInfo);
        }
    }
}
