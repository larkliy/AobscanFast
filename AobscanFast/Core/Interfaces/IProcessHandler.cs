using System.Runtime.InteropServices;

namespace AobscanFast.Core.Interfaces;

/// <summary>Provides platform-specific process and module operations.</summary>
public interface IProcessHandler
{
    /// <summary>Opens a process and returns its native handle.</summary><param name="processId">The process identifier.</param><returns>An owned safe process handle.</returns>
    SafeHandle OpenProcess(uint processId);
    /// <summary>Finds a process identifier by name.</summary><param name="processName">The process name.</param><param name="index">The zero-based occurrence to return.</param><returns>The identifier, or <see langword="null"/> when no matching occurrence exists.</returns>
    uint? FindIdByName(string processName, int index = 0);
    /// <summary>Gets a module's base address and size.</summary><param name="processId">The process identifier.</param><param name="moduleName">The module name.</param><returns>Module information, or <see langword="null"/> when the module is absent.</returns>
    (nint BaseAddress, uint Size)? GetModuleInfo(uint processId, string moduleName);
}
