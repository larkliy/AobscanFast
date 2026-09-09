using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace AobscanFast.Infrastructure.Windows;

/// <summary>Enumerates accessible regions in a remote Windows process.</summary>
public sealed class RemoteProcessRegionEnumerator : IMemoryRegionEnumerator
{
    private readonly SafeHandle _processHandle;

    /// <summary>Initializes an enumerator for an open Windows process handle.</summary>
    public RemoteProcessRegionEnumerator(SafeHandle processHandle)
    {
        ArgumentNullException.ThrowIfNull(processHandle);
        if (processHandle.IsInvalid || processHandle.IsClosed)
            throw new ArgumentException("A valid open process handle is required.", nameof(processHandle));

        _processHandle = processHandle;
    }

    /// <inheritdoc/>
    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return new InternalEnumerator(_processHandle).GetRegions(minAddress, maxAddress, access);
    }

    private sealed class InternalEnumerator : WindowsRegionEnumeratorBase
    {
        private readonly SafeHandle _processHandle;

        public InternalEnumerator(SafeHandle processHandle)
        {
            _processHandle = processHandle;
        }

        protected override nuint QueryMemory(nint address, out MEMORY_BASIC_INFORMATION memoryInfo)
        {
            unsafe
            {
                return PInvoke.VirtualQueryEx(_processHandle, address.ToPointer(), out memoryInfo);
            }
        }
    }
}
