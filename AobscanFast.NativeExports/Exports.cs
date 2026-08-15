using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using AobscanFast.Core.Models;
using AobscanFast.Services;

namespace AobscanFast.NativeExports;

/// <summary>
/// Native exports for the AobscanFast scanner, published as a NativeAOT shared library.
/// All functions return an error code (0 = OK); the last error message is available
/// through <see cref="GetLastError"/> on the same calling thread.
/// </summary>
public static unsafe class Exports
{
    private const int AobOk = 0;
    private const int AobErrorInvalidArgument = -1;
    private const int AobErrorInvalidHandle = -2;
    private const int AobErrorInternal = -3;

    private const nint DefaultChunkSize = 256 * 1024;

    private static readonly ConcurrentDictionary<int, ScannerEntry> Scanners = new();
    private static int _nextHandle;

    [ThreadStatic]
    private static string? _lastError;

    /// <summary>Creates a scanner for the current process. Returns the scanner handle, or 0 on failure.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scanner_create_current")]
    public static int CreateCurrentScanner()
    {
        try
        {
            return Add(new ScannerEntry(AobScannerFactory.ForCurrentProcess(), (uint)Environment.ProcessId));
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>Creates a scanner for a remote process by its OS process id. Returns the scanner handle, or 0 on failure.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scanner_create_remote_pid")]
    public static int CreateRemoteScanner(uint processId)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var processHandler = new Infrastructure.Windows.WinProcessHandler();
                SafeHandle processHandle = processHandler.OpenProcess(processId);
                return AddRemote(processId, processHandle);
            }

            if (OperatingSystem.IsLinux())
            {
                var processHandler = new Infrastructure.Linux.LinuxProcessHandler();
                SafeHandle processHandle = processHandler.OpenProcess(processId);
                return AddRemote(processId, processHandle);
            }

            throw new PlatformNotSupportedException("AobscanFast supports Windows and Linux.");
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>Releases a scanner handle previously returned by a create function.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scanner_destroy")]
    public static void DestroyScanner(int handle)
    {
        if (Scanners.TryRemove(handle, out ScannerEntry? entry))
            entry.Dispose();
    }

    /// <summary>Finds the first process whose name matches, starting at <paramref name="index"/>. Returns the pid, or 0 when not found.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_find_pid_by_name")]
    public static int FindProcessIdByName(byte* processNameUtf8, int index)
    {
        try
        {
            string processName = ReadUtf8(processNameUtf8);

            if (OperatingSystem.IsWindows())
                return (int)(new Infrastructure.Windows.WinProcessHandler().FindIdByName(processName, index) ?? 0);

            if (OperatingSystem.IsLinux())
                return (int)(new Infrastructure.Linux.LinuxProcessHandler().FindIdByName(processName, index) ?? 0);

            throw new PlatformNotSupportedException("AobscanFast supports Windows and Linux.");
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>
    /// Scans memory for an AOB pattern and writes up to <paramref name="capacity"/> matches into <paramref name="results"/>.
    /// <paramref name="batchSize"/> limits the number of matches gathered per call (0 = unlimited, capped by capacity);
    /// when <paramref name="countWritten"/> equals the effective batch, callers should resume from the last address + 1.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scan")]
    public static int Scan(
        int handle,
        byte* patternUtf8,
        ulong minAddress,
        ulong maxAddress,
        long chunkSize,
        int maxParallel,
        int batchSize,
        ulong* results,
        int capacity,
        int* countWritten)
    {
        try
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (capacity > 0 && results is null)
                throw new ArgumentNullException(nameof(results));
            if (batchSize < 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize));

            int limit = batchSize > 0 ? Math.Min(batchSize, capacity) : capacity;
            List<nint> found = GetEntry(handle).Execute(scanner => scanner.Scan(
                ReadUtf8(patternUtf8),
                CreateOptions(minAddress, maxAddress, chunkSize, maxParallel, limit)));

            int count = Math.Min(found.Count, capacity);
            if (countWritten is not null)
                *countWritten = count;

            if (results is not null)
                for (int i = 0; i < count; i++)
                    results[i] = (ulong)found[i];

            return AobOk;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>Scans memory and returns the first match in <paramref name="outAddress"/> (0 when no match exists).</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scan_first")]
    public static int ScanFirst(
        int handle,
        byte* patternUtf8,
        ulong minAddress,
        ulong maxAddress,
        long chunkSize,
        int maxParallel,
        ulong* outAddress)
    {
        try
        {
            nint? found = GetEntry(handle).Execute(scanner => scanner.ScanFirst(
                ReadUtf8(patternUtf8),
                CreateOptions(minAddress, maxAddress, chunkSize, maxParallel, 1)));

            if (outAddress is not null)
                *outAddress = found is null ? 0 : (ulong)found.Value;

            return AobOk;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>Scans a named module for an AOB pattern. A zero capacity query returns the required result count.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scan_module")]
    public static int ScanModule(
        int handle,
        byte* moduleUtf8,
        byte* patternUtf8,
        ulong* results,
        int capacity,
        int* countWritten)
    {
        try
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (capacity > 0 && results is null)
                throw new ArgumentNullException(nameof(results));
            ScannerEntry entry = GetEntry(handle);
            List<nint> found = entry.Execute(scanner =>
                scanner.ScanModule(entry.ProcessId, ReadUtf8(moduleUtf8), ReadUtf8(patternUtf8)));

            if (capacity == 0)
            {
                if (countWritten is not null)
                    *countWritten = found.Count;
                return AobOk;
            }

            int count = Math.Min(found.Count, capacity);
            if (countWritten is not null)
                *countWritten = found.Count;

            if (results is not null)
                for (int i = 0; i < count; i++)
                    results[i] = (ulong)found[i];

            return AobOk;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>Scans a named module and returns the first match in <paramref name="outAddress"/> (0 when none exists).</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scan_module_first")]
    public static int ScanModuleFirst(
        int handle,
        byte* moduleUtf8,
        byte* patternUtf8,
        ulong* outAddress)
    {
        try
        {
            ScannerEntry entry = GetEntry(handle);
            nint? found = entry.Execute(scanner =>
                scanner.ScanModuleFirst(entry.ProcessId, ReadUtf8(moduleUtf8), ReadUtf8(patternUtf8)));

            if (outAddress is not null)
                *outAddress = found is null ? 0 : (ulong)found.Value;

            return AobOk;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>
    /// Copies the last error message of the calling thread into <paramref name="buffer"/> as UTF-8
    /// and returns the full message length in bytes (excluding the terminator).
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_last_error")]
    public static int GetLastError(byte* buffer, int capacity)
    {
        byte[] message = Encoding.UTF8.GetBytes(_lastError ?? string.Empty);

        if (buffer is not null && capacity > 0)
        {
            int count = Math.Min(message.Length, capacity - 1);
            if (count > 0)
                Marshal.Copy(message, 0, (nint)buffer, count);
            buffer[count] = 0;
        }

        return message.Length;
    }

    private static int Add(ScannerEntry entry)
    {
        int handle = Interlocked.Increment(ref _nextHandle);
        if (!Scanners.TryAdd(handle, entry))
            throw new InvalidOperationException("Failed to register the scanner handle.");

        return handle;
    }

    private static int AddRemote(uint processId, SafeHandle processHandle)
    {
        try
        {
            return Add(new ScannerEntry(
                AobScannerFactory.ForRemoteProcess(processHandle),
                processId,
                processHandle));
        }
        catch
        {
            processHandle.Dispose();
            throw;
        }
    }

    private static ScannerEntry GetEntry(int handle)
    {
        if (!Scanners.TryGetValue(handle, out ScannerEntry? entry))
            throw new KeyNotFoundException($"Unknown scanner handle: {handle}.");

        return entry;
    }

    private static string ReadUtf8(byte* value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return Marshal.PtrToStringUTF8((nint)value)
            ?? throw new InvalidOperationException("The string is not valid UTF-8.");
    }

    private static AobScanOptions CreateOptions(ulong minAddress, ulong maxAddress, long chunkSize, int maxParallel, int maxResults)
    {
        if (maxParallel != -1 && maxParallel <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallel));
        if (maxResults < 0)
            throw new ArgumentOutOfRangeException(nameof(maxResults));

        return new AobScanOptions
        {
            MinScanAddress = checked((nint)minAddress),
            MaxScanAddress = maxAddress == 0 ? nint.MaxValue : checked((nint)maxAddress),
            ChunkSize = chunkSize <= 0 ? DefaultChunkSize : checked((nint)chunkSize),
            MaxDegreeOfParallelism = maxParallel,
            MaxResults = maxResults
        };
    }

    private static int Fail(Exception exception)
    {
        _lastError = exception.Message;
        return exception switch
        {
            ArgumentException or OverflowException => AobErrorInvalidArgument,
            KeyNotFoundException => AobErrorInvalidHandle,
            _ => AobErrorInternal
        };
    }

    private sealed class ScannerEntry(AobScanner scanner, uint processId, SafeHandle? ownedHandle = null) : IDisposable
    {
        private readonly Lock _syncRoot = new();
        private bool _disposed;

        public uint ProcessId { get; } = processId;

        public T Execute<T>(Func<AobScanner, T> operation)
        {
            lock (_syncRoot)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return operation(scanner);
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                ownedHandle?.Dispose();
                _disposed = true;
            }
        }
    }
}
