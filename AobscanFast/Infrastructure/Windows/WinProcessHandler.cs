using Windows.Win32;
using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Windows.Win32.System.Threading;
using Windows.Win32.System.Diagnostics.ToolHelp;

namespace AobscanFast.Infrastructure.Windows;

public class WinProcessHandler : IProcessHandler
{
    public uint? FindIdByName(string processName, int index = 0)
    {
        using var hSnapshot = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);
        
        var entry32 = new PROCESSENTRY32W { dwSize = (uint)Unsafe.SizeOf<PROCESSENTRY32W>() };

        if (!PInvoke.Process32FirstW(hSnapshot, ref entry32))
            return null;

        do
        {
            if (entry32.szExeFile.AsReadOnlySpan().SliceAtNull().Equals(processName, StringComparison.OrdinalIgnoreCase))
                return entry32.th32ProcessID;

        } while (PInvoke.Process32NextW(hSnapshot, ref entry32));

        return null;
    }

    public unsafe (nint BaseAddress, uint Size)? GetModuleInfo(uint processId, string moduleName)
    {
        using var hSnapshot = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPMODULE, 0);

        var entry32 = new MODULEENTRY32W { dwSize = (uint)Unsafe.SizeOf<MODULEENTRY32W>() };

        if (!PInvoke.Module32FirstW(hSnapshot, ref entry32))
            return null;

        do
        {
            if (entry32.szModule.AsReadOnlySpan().SliceAtNull().Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                return ((nint)entry32.modBaseAddr, entry32.modBaseSize);

        } while (PInvoke.Module32NextW(hSnapshot, ref entry32));

        return null;
    }

    public SafeHandle OpenProcess(uint processId)
    {
        return PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, processId);
    }
}
