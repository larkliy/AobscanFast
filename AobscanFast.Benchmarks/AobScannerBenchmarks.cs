using System.Runtime.InteropServices;
using AobscanFast.Core.Models;
using AobscanFast.Services;
using BenchmarkDotNet.Attributes;

namespace AobscanFast.Benchmarks;

/// <summary>
/// Benchmarks the in-process scanner over a pinned 64 MiB buffer.
/// The buffer is filled with deterministic pseudo-random data and the pattern
/// is planted at three known offsets (25%, 50%, 75%).
/// </summary>
[MemoryDiagnoser]
public class AobScannerBenchmarks
{
    private const int BufferSize = 64 * 1024 * 1024;
    private static readonly byte[] PatternBytes = [0x48, 0x8B, 0x01, 0x02, 0x03, 0xAA];

    private GCHandle _handle;
    private byte[] _buffer = null!;
    private AobScanner _scanner = null!;
    private AobScanOptions _options = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _buffer = new byte[BufferSize];
        new Random(42).NextBytes(_buffer);

        int[] offsets = [BufferSize / 4, BufferSize / 2, 3 * BufferSize / 4];
        foreach (int offset in offsets)
            PatternBytes.CopyTo(_buffer, offset);

        _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
        nint baseAddress = _handle.AddrOfPinnedObject();

        _scanner = AobScannerFactory.ForCurrentProcess();
        _options = new AobScanOptions
        {
            MinScanAddress = baseAddress,
            MaxScanAddress = checked(baseAddress + BufferSize)
        };
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _handle.Free();

    [Benchmark]
    public List<nint> Scan_SolidPattern() => _scanner.Scan("48 8B 01 02 03 AA", _options);

    [Benchmark]
    public List<nint> Scan_ByteMaskPattern() => _scanner.Scan("48 8B ?? ?? ?? AA", _options);

    [Benchmark]
    public List<nint> Scan_NibbleMaskPattern() => _scanner.Scan("4? 8? ?? ?? ?? A?", _options);

    [Benchmark]
    public nint? ScanFirst_SolidPattern() => _scanner.ScanFirst("48 8B 01 02 03 AA", _options);
}