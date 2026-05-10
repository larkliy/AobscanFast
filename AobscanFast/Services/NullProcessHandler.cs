using AobscanFast.Core.Interfaces;
using System.Runtime.InteropServices;

namespace AobscanFast.Services;

internal sealed class NullProcessHandler : IProcessHandler
{
    public static NullProcessHandler Instance { get; } = new();

    private NullProcessHandler()
    {
    }

    public uint? FindIdByName(string processName, int index = 0) => throw CreateNotConfiguredException();

    public (nint BaseAddress, uint Size)? GetModuleInfo(uint processId, string moduleName) => throw CreateNotConfiguredException();

    public SafeHandle OpenProcess(uint processId) => throw CreateNotConfiguredException();

    private static InvalidOperationException CreateNotConfiguredException()
        => new("Process operations require an IProcessHandler. Use the AobScanner constructor that accepts a process handler.");
}
