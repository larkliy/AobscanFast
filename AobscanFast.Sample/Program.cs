using System.Diagnostics;
using System.Runtime.InteropServices;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Services;

bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

Console.WriteLine($"Platform: {(isWindows ? "Windows" : isLinux ? "Linux" : "Unknown")}");
Console.WriteLine($"Process ID: {Environment.ProcessId}\n");

// ============================================================================
// 1. Current process — direct memory read/write
// ============================================================================
Console.WriteLine("=== 1. Current Process Memory Access ===");

IMemoryAccessor currentAccessor = isWindows
    ? new AobscanFast.Infrastructure.Windows.CurrentProcessMemoryAccessor()
    : new AobscanFast.Infrastructure.Linux.CurrentProcessMemoryAccessor();

Span<byte> writeBuf = [0xDE, 0xAD, 0xBE, 0xEF];
Span<byte> readBuf = stackalloc byte[writeBuf.Length];

unsafe
{
    fixed (byte* p = writeBuf)
    {
        if (currentAccessor.WriteMemory((nint)p, writeBuf, out _) &&
            currentAccessor.ReadMemory((nint)p, readBuf, out _))
        {
            bool match = writeBuf.SequenceEqual(readBuf);
            Console.WriteLine($"  Write + Read: {(match ? "OK" : "DATA MISMATCH")}");
            Console.WriteLine($"  Data: {BitConverter.ToString(readBuf.ToArray())}");
        }
    }
}

// ============================================================================
// 2. Current process — region enumeration
// ============================================================================
Console.WriteLine("\n=== 2. Current Process Memory Regions ===");

IMemoryRegionEnumerator currentEnumerator = isWindows
    ? new AobscanFast.Infrastructure.Windows.CurrentProcessRegionEnumerator()
    : new AobscanFast.Infrastructure.Linux.CurrentProcessRegionEnumerator();

#pragma warning disable CS8778
var regions = currentEnumerator.GetRegions(0, (nint)long.MaxValue, MemoryAccess.Readable);
#pragma warning restore CS8778
Console.WriteLine($"  Total readable regions: {regions.Count}");
foreach (var r in regions.Take(5))
    Console.WriteLine($"    0x{r.BaseAddress:X16}  size={r.Size,10}");
if (regions.Count > 5)
    Console.WriteLine($"    ... and {regions.Count - 5} more");

// ============================================================================
// 3. Current process — AobScanner (self-scan for benchmarks)
// ============================================================================
Console.WriteLine("\n=== 3. AobScanner — Current Process ===");

var localScanner = AobScannerFactory.ForCurrentProcess();
string testPattern = "DE AD BE EF";

var scanResult = localScanner.ScanFirst(testPattern);
Console.WriteLine($"  ScanFirst(\"{testPattern}\"): {(scanResult.HasValue ? $"0x{scanResult.Value:X}" : "not found")}");

const int iterations = 100;
var sw = Stopwatch.StartNew();
int totalFound = 0;

for (int i = 0; i < iterations; i++)
{
    var results = localScanner.Scan(testPattern);
    totalFound = results.Count;
}

sw.Stop();
double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
Console.WriteLine($"  Benchmark ({iterations}x Scan): avg={avgMs:F3}ms, found={totalFound}");

// ============================================================================
// 4. Remote process — open, read regions, scan
// ============================================================================
Console.WriteLine("\n=== 4. Remote Process ===");

IProcessHandler handler = isWindows
    ? new AobscanFast.Infrastructure.Windows.WinProcessHandler()
    : new AobscanFast.Infrastructure.Linux.LinuxProcessHandler();

string[] targetNames = isWindows ? ["notepad.exe"] : ["bash", "zsh", "sh"];
uint? targetPid = null;

foreach (var name in targetNames)
{
    targetPid = handler.FindIdByName(name);
    if (targetPid.HasValue)
    {
        Console.WriteLine($"  Target: {name} (PID={targetPid.Value})");
        break;
    }
}

if (targetPid.HasValue)
{
    using var handle = handler.OpenProcess(targetPid.Value);

    IMemoryAccessor remoteAccessor = isWindows
        ? new AobscanFast.Infrastructure.Windows.RemoteProcessMemoryAccessor(handle)
        : new AobscanFast.Infrastructure.Linux.RemoteProcessMemoryAccessor(handle);

    IMemoryRegionEnumerator remoteEnumerator = isWindows
        ? new AobscanFast.Infrastructure.Windows.RemoteProcessRegionEnumerator(handle)
        : new AobscanFast.Infrastructure.Linux.RemoteProcessRegionEnumerator(handle);

#pragma warning disable CS8778
    var remoteRegions = remoteEnumerator.GetRegions(0, (nint)long.MaxValue, MemoryAccess.Readable);
#pragma warning restore CS8778

    if (remoteRegions.Count > 0)
    {
        Span<byte> buf = stackalloc byte[64];
        if (remoteAccessor.ReadMemory(remoteRegions[0].BaseAddress, buf, out var read))
        {
            Console.WriteLine($"  Remote read: {read} bytes from 0x{remoteRegions[0].BaseAddress:X}");
            Console.WriteLine($"  Data: {BitConverter.ToString(buf[..(int)read].ToArray())}");
        }
    }

    var remoteScanner = new AobScanner(handler, remoteEnumerator, remoteAccessor);
    Console.WriteLine("  AobScanner created (remote process)");
    Console.WriteLine($"  Regions to scan: {remoteRegions.Count}");
}
else
{
    Console.WriteLine($"  Target process not found.");
    Console.WriteLine($"  Try starting: {(isWindows ? "notepad.exe" : "any process from the list: bash, zsh, sh")}");
}

Console.WriteLine("\nDone.");
