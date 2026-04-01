using System.Runtime.InteropServices;

namespace AobscanFast.Core.Interfaces;

public interface IProcessHandler
{
    SafeHandle OpenProcess(uint processId);
    uint? FindIdByName(string processName, int index = 0);
    (nint BaseAddress, uint Size)? GetModuleInfo(uint processId, string moduleName);
}
