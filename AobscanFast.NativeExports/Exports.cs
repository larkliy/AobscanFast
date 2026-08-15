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

    private static readonly ConcurrentDictionary<int, AobScanner> Scanners = new();
    private static int _nextHandle;

    [ThreadStatic]
    private static string? _lastError;

    /// <summary>Creates a scanner for the current process. Returns the scanner handle, or 0 on failure.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scanner_create_current")]
    public static int CreateCurrentScanner()
    {
        try
        {
            return Add(AobScannerFactory.ForCurrentProcess());
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
                return Add(AobScannerFactory.ForRemoteProcess(processHandler.OpenProcess(processId)));
            }

            if (OperatingSystem.IsLinux())
            {
                var processHandler = new Infrastructure.Linux.LinuxProcessHandler();
                return Add(AobScannerFactory.ForRemoteProcess(processHandler.OpenProcess(processId)));
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
        Scanners.TryRemove(handle, out _);
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

            int limit = batchSize > 0 ? batchSize : capacity;
            List<nint> found = GetScanner(handle).Scan(
                ReadUtf8(patternUtf8),
                CreateOptions(minAddress, maxAddress, chunkSize, maxParallel, limit));

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
            nint? found = GetScanner(handle).ScanFirst(
                ReadUtf8(patternUtf8),
                CreateOptions(minAddress, maxAddress, chunkSize, maxParallel, 1));

            if (outAddress is not null)
                *outAddress = found is null ? 0 : (ulong)found.Value;

            return AobOk;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    /// <summary>Scans a named module for an AOB pattern and writes up to <paramref name="capacity"/> matches into <paramref name="results"/>.</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scan_module")]
    public static int ScanModule(
        int handle,
        uint processId,
        byte* moduleUtf8,
        byte* patternUtf8,
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

            List<nint> found = GetScanner(handle).ScanModule(processId, ReadUtf8(moduleUtf8), ReadUtf8(patternUtf8));

            int count = Math.Min(found.Count, batchSize > 0 ? batchSize : capacity);
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

    /// <summary>Scans a named module and returns the first match in <paramref name="outAddress"/> (0 when none exists).</summary>
    [UnmanagedCallersOnly(EntryPoint = "aob_scan_module_first")]
    public static int ScanModuleFirst(
        int handle,
        uint processId,
        byte* moduleUtf8,
        byte* patternUtf8,
        ulong* outAddress)
    {
        try
        {
            nint? found = GetScanner(handle).ScanModuleFirst(processId, ReadUtf8(moduleUtf8), ReadUtf8(patternUtf8));

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

    private static int Add(AobScanner scanner)
    {
        int handle = Interlocked.Increment(ref _nextHandle);
        if (!Scanners.TryAdd(handle, scanner))
            throw new InvalidOperationException("Failed to register the scanner handle.");

        return handle;
    }

    private static AobScanner GetScanner(int handle)
    {
        if (!Scanners.TryGetValue(handle, out AobScanner? scanner))
            throw new KeyNotFoundException($"Unknown scanner handle: {handle}.");

        return scanner;
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
}