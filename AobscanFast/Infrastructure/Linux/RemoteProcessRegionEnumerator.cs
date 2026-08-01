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

        if (handle is not SafeProcessHandle processHandle || processHandle.IsInvalid || processHandle.IsClosed)
            throw new ArgumentException("A Linux process handle created by LinuxProcessHandler is required.", nameof(handle));

        _processId = processHandle.ProcessId;
    }

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return LinuxMapsParser.Parse($"/proc/{_processId}/maps", minAddress, maxAddress, access);
    }
}
