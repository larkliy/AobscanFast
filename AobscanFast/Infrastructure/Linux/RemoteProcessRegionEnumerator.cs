using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

public sealed class RemoteProcessRegionEnumerator : IMemoryRegionEnumerator
{
    private readonly uint _processId;

    public RemoteProcessRegionEnumerator(SafeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        _processId = ((SafeProcessHandle)handle).ProcessId;
    }

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return LinuxMapsParser.Parse($"/proc/{_processId}/maps", minAddress, maxAddress, access);
    }
}
