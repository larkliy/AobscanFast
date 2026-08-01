using System.Runtime.InteropServices;
using LinuxInfrastructure = AobscanFast.Infrastructure.Linux;
using WindowsInfrastructure = AobscanFast.Infrastructure.Windows;

namespace AobscanFast.Services;

public static class AobScannerFactory
{
    public static AobScanner ForCurrentProcess()
    {
        if (OperatingSystem.IsWindows())
            return new(new WindowsInfrastructure.WinProcessHandler(), new WindowsInfrastructure.CurrentProcessRegionEnumerator(), new WindowsInfrastructure.CurrentProcessMemoryAccessor());

        if (OperatingSystem.IsLinux())
            return new(new LinuxInfrastructure.LinuxProcessHandler(), new LinuxInfrastructure.CurrentProcessRegionEnumerator(), new LinuxInfrastructure.CurrentProcessMemoryAccessor());

        throw new PlatformNotSupportedException("AobscanFast supports Windows and Linux.");
    }

    public static AobScanner ForRemoteProcess(SafeHandle processHandle)
    {
        ArgumentNullException.ThrowIfNull(processHandle);

        if (OperatingSystem.IsWindows())
            return new(new WindowsInfrastructure.WinProcessHandler(), new WindowsInfrastructure.RemoteProcessRegionEnumerator(processHandle), new WindowsInfrastructure.RemoteProcessMemoryAccessor(processHandle));

        if (OperatingSystem.IsLinux())
            return new(new LinuxInfrastructure.LinuxProcessHandler(), new LinuxInfrastructure.RemoteProcessRegionEnumerator(processHandle), new LinuxInfrastructure.RemoteProcessMemoryAccessor(processHandle));

        throw new PlatformNotSupportedException("AobscanFast supports Windows and Linux.");
    }
}
