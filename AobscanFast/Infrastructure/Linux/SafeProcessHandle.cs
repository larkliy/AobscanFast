using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

internal sealed class SafeProcessHandle : SafeHandle
{
    private readonly uint _processId;

    private SafeProcessHandle() : base((IntPtr)(-1), true) => _processId = 0;

    public SafeProcessHandle(int fd, uint processId) : base((IntPtr)fd, true)
    {
        _processId = processId;
    }

    public uint ProcessId => _processId;

    public override bool IsInvalid => handle == (IntPtr)(-1);

    protected override bool ReleaseHandle()
    {
        return NativeMethods.close((int)handle) == 0;
    }
}
