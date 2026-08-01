using System.Runtime.InteropServices;

namespace AobscanFast.Infrastructure.Linux;

internal static class NativeMethods
{
    internal const int O_RDWR = 2;
    internal const int EINTR = 4;

    [DllImport("libc", SetLastError = true)]
    internal static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    internal static extern unsafe long pread(SafeHandle fd, byte* buf, nuint count, long offset);

    [DllImport("libc", SetLastError = true)]
    internal static extern unsafe long pwrite(SafeHandle fd, byte* buf, nuint count, long offset);

    [DllImport("libc", SetLastError = true)]
    internal static extern int close(int fd);
}
