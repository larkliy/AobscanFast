using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using AobscanFast.Structs.Windows;
using AobscanFast.Enums.Windows;

namespace AobscanFast.Utils;

internal partial class Native
{
    [LibraryImport("kernel32.dll")]
    public static partial SafeWaitHandle CreateToolhelp32Snapshot(CreateToolhelpSnapshotFlags dwFlags, uint th32ProcessID);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Process32FirstW(SafeHandle hSnapshot, ref PROCESSENTRY32W lppe);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Process32NextW(SafeHandle hSnapshot, ref PROCESSENTRY32W lppe);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Module32FirstW(SafeHandle hSnapshot, ref MODULEENTRY32W lpme);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Module32NextW(SafeHandle hSnapshot, ref MODULEENTRY32W lpme);

    [LibraryImport("kernel32.dll")]
    public static partial nint OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [LibraryImport("kernel32.dll")]
    public static partial nint VirtualQueryEx(
        nint hProcess,
        nint lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer,
        nint dwLength);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nint lpBaseAddress, Span<byte> lpBuffer, nuint nSize, out nuint lpNumberOfBytesRead);
}
