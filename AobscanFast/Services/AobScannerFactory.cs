using System.Runtime.InteropServices;
using LinuxInfrastructure = AobscanFast.Infrastructure.Linux;
using WindowsInfrastructure = AobscanFast.Infrastructure.Windows;

namespace AobscanFast.Services;

/// <summary>Creates scanners configured for the current or a remote process.</summary>
public static class AobScannerFactory
{
    /// <summary>Creates a scanner for the current process.</summary>
    /// <returns>A platform-specific scanner.</returns>
    /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows or Linux.</exception>
    public static AobScanner ForCurrentProcess()
    {
        if (OperatingSystem.IsWindows())
            return new(new WindowsInfrastructure.WinProcessHandler(), new WindowsInfrastructure.CurrentProcessRegionEnumerator(), new WindowsInfrastructure.CurrentProcessMemoryAccessor());

        if (OperatingSystem.IsLinux())
            return new(new LinuxInfrastructure.LinuxProcessHandler(), new LinuxInfrastructure.CurrentProcessRegionEnumerator(), new LinuxInfrastructure.CurrentProcessMemoryAccessor());

        throw new PlatformNotSupportedException("AobscanFast supports Windows and Linux.");
    }

    /// <summary>Creates a scanner for a remote process represented by an open native handle.</summary>
    /// <param name="processHandle">An open process handle. The caller retains ownership of the handle.</param>
    /// <returns>A platform-specific scanner.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="processHandle"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows or Linux.</exception>
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
