using System.Runtime.InteropServices;

namespace AobscanFast.Utils;

internal partial class Native
{
    [LibraryImport("libc")]
    public static partial nint pread(int fd, Span<byte> buffer, nuint count, long offset);
}
