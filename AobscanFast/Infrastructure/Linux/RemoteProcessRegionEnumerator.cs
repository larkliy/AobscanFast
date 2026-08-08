using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

/// <summary>Enumerates mapped regions in a remote Linux process.</summary>
public sealed class RemoteProcessRegionEnumerator : IMemoryRegionEnumerator
{
    private readonly uint _processId;

    /// <summary>Initializes an enumerator for an open Linux process handle.</summary><param name="handle">The process handle.</param>
    public RemoteProcessRegionEnumerator(SafeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (handle is not SafeProcessHandle processHandle || processHandle.IsInvalid || processHandle.IsClosed)
            throw new ArgumentException("A Linux process handle created by LinuxProcessHandler is required.", nameof(handle));

        _processId = processHandle.ProcessId;
    }

    /// <inheritdoc/>
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return LinuxMapsParser.Parse($"/proc/{_processId}/maps", minAddress, maxAddress, access);
    }
}
