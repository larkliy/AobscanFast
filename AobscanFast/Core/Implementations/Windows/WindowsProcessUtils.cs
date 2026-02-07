using AobscanFast.Core.Models;
using AobscanFast.Enums.Windows;
using AobscanFast.Structs.Windows;
using AobscanFast.Utils;
using System.Runtime.CompilerServices;

namespace AobscanFast.Core.Implementations.Windows;

internal class WindowsProcessUtils
{
    public static ProcessInfo OpenProcess(uint processId)
    {
        var processInfo = Native.OpenProcess(ProcessAccessFlags.PROCESS_ALL_ACCESS, false, processId);
        if (processInfo == 0 || processInfo == -1)
            throw new InvalidOperationException($"Could not open process with ID {processId}. Process may not exist or access may be denied.");
        
        return new(processInfo);
    }

    [SkipLocalsInit]
    public static uint FindByName(ReadOnlySpan<char> name)
    {
        name = TrimExeExtension(name);

        var pe32 = new PROCESSENTRY32W { dwSize = (uint)Unsafe.SizeOf<PROCESSENTRY32W>() };

        using var snapshot = Native.CreateToolhelp32Snapshot(CreateToolhelpSnapshotFlags.TH32CS_SNAPPROCESS, 0);

        if (snapshot.IsInvalid)
            throw new InvalidOperationException("Could not create process snapshot. Process enumeration failed.");

        if (!Native.Process32FirstW(snapshot, ref pe32))
            throw new InvalidOperationException("Could not enumerate processes. Process32FirstW failed.");

        do
        {
            ReadOnlySpan<char> currentFullname = pe32.szExeFile;

            int nullIndex = currentFullname.IndexOf('\0');

            ReadOnlySpan<char> currentName = nullIndex >= 0 ? currentFullname[..nullIndex] : currentFullname;

            currentName = TrimExeExtension(currentName);

            if (currentName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return pe32.th32ProcessID;

        } while (Native.Process32NextW(snapshot, ref pe32));

        throw new ArgumentException($"Process '{name}' not found.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ReadOnlySpan<char> TrimExeExtension(ReadOnlySpan<char> s)
            => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;
    }

    [SkipLocalsInit]
    public static (nint BaseAddress, uint Size) GetModule(uint processId, string moduleName)
    {
        var me32 = new MODULEENTRY32W { dwSize = (uint)Unsafe.SizeOf<MODULEENTRY32W>() };

        using var snapshot = Native.CreateToolhelp32Snapshot(CreateToolhelpSnapshotFlags.TH32CS_SNAPMODULE | CreateToolhelpSnapshotFlags.TH32CS_SNAPMODULE32, processId);

        if (snapshot.IsInvalid)
            return (0, 0);

        if (!Native.Module32FirstW(snapshot, ref me32))
            return (0, 0);

        do
        {
            ReadOnlySpan<char> currentFullname = me32.szModule;
            int nullIndex = currentFullname.IndexOf('\0');
            ReadOnlySpan<char> currentName = nullIndex >= 0 ? currentFullname[..nullIndex] : currentFullname;

            if (currentName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                return (me32.modBaseAddr, me32.modBaseSize);


        } while (Native.Module32NextW(snapshot, ref me32));

        throw new FileNotFoundException($"Module '{moduleName}' not found in process {processId}.");
    }
}
