using AobscanFast.Core.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace AobscanFast.Infrastructure.Windows;

public class WinProcessHandler : IProcessHandler
{
    public uint? FindIdByName(string processName, int index = 0)
    {
        var processes = Process.GetProcessesByName(processName);

        if (processes == null || processes.Length == 0)
            return null;

        uint processId = 0;
        int processIndex = 0;

        Array.ForEach(processes, p =>
        {
            if (p.ProcessName == processName && processIndex == index)
                processId = (uint)p.Id;
            else
                processIndex++;

            p.Dispose();
        });

        return processId == 0 ? null : processId;
    }

    public (nint BaseAddress, uint Size)? GetModuleInfo(uint processId, string moduleName)
    {
        using var process = Process.GetProcessById((int)processId);

        foreach (ProcessModule module in process.Modules)
        {
            if (module.ModuleName == moduleName) 
                return (module.BaseAddress, (uint)module.ModuleMemorySize);
        }

        return null;
    }

    public SafeHandle OpenProcess(uint processId)
    {
        return PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, processId);
    }
}
