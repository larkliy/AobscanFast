using AobscanFast.Infrastructure.Windows;
using System.Runtime.InteropServices;

namespace AobscanFast.Services;

public static class AobScannerFactory
{
    public static AobScanner ForCurrentProcess()
        => new(new CurrentProcessRegionEnumerator(), new CurrentProcessMemoryAccessor());

    public static AobScanner ForRemoteProcess(SafeHandle processHandle)
        => new(new RemoteProcessRegionEnumerator(processHandle), new RemoteProcessMemoryAccessor(processHandle));
}
