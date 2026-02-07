using System.Buffers;
using System.Runtime.CompilerServices;
using AobscanFast.Core.Abstractions;
using AobscanFast.Core.Models;
using AobscanFast.Utils;

namespace AobscanFast.Core;

public class AobScan
{
    private readonly Lock _syncRoot = new();
    private readonly IMemoryReader _memoryReader;

    private AobScan(IMemoryReader memoryReader) => _memoryReader = memoryReader;

    public static List<nint> ScanProcess(string process, string pattern, AobScanOptions? scanOptions = null)
    {
        using var processInfo = ProcessMemoryFactory.OpenProcessByName(process);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(processInfo);

        var aobscan = new AobScan(memoryReader);
        return aobscan.Scan(pattern, scanOptions);
    }

    public static async Task<List<nint>> ScanProcessAsync(string process, string pattern, AobScanOptions? scanOptions = null, CancellationToken cancellationToken = default)
    {
        using var processInfo = ProcessMemoryFactory.OpenProcessByName(process);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(processInfo);

        var aobscan = new AobScan(memoryReader);
        return await aobscan.ScanAsync(pattern, scanOptions, cancellationToken);
    }

    public static List<nint> ScanModule(string processName, string moduleName, string pattern)
    {
        uint pid = ProcessMemoryFactory.FindProcessIdByName(processName);
        var moduleInfo = ProcessMemoryFactory.GetModule(pid, moduleName);
        Console.WriteLine(moduleInfo.Size);
        var scanOptions = new AobScanOptions(
            minScanAddress: moduleInfo.BaseAddress,
            maxScanAddress: (nint?)(moduleInfo.BaseAddress + moduleInfo.Size));

        using var handle = ProcessMemoryFactory.OpenProcessById(pid);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(handle);

        var aobscan = new AobScan(memoryReader);
        return aobscan.Scan(pattern, scanOptions);
    }

    public static async Task<List<nint>> ScanModuleAsync(string processName, string moduleName, string pattern, CancellationToken cancellationToken = default)
    {
        uint pid = ProcessMemoryFactory.FindProcessIdByName(processName);
        var moduleInfo = ProcessMemoryFactory.GetModule(pid, moduleName);

        var scanOptions = new AobScanOptions(
            minScanAddress: moduleInfo.BaseAddress,
            maxScanAddress: (nint?)(moduleInfo.BaseAddress + moduleInfo.Size));

        using var processInfo = ProcessMemoryFactory.OpenProcessById(pid);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(processInfo);

        var aobscan = new AobScan(memoryReader);
        return await aobscan.ScanAsync(pattern, scanOptions, cancellationToken);
    }

    public List<nint> Scan(string input)
        => Scan(input, null);

    public Task<List<nint>> ScanAsync(string input, CancellationToken cancellationToken = default)
        => ScanAsync(input, null, cancellationToken);

    public Task<List<nint>> ScanAsync(string input, AobScanOptions? scanOptions, CancellationToken cancellationToken = default) 
        => Task.Run(() => Scan(input, scanOptions, cancellationToken), cancellationToken);

    public List<nint> Scan(string input, AobScanOptions? scanOptions, CancellationToken cancellationToken = default)
    {
        scanOptions ??= new();
        var finalResults = new List<nint>(capacity: 1024);
        var pattern = Pattern.Create(input);
        var rawRegions = _memoryReader.GetRegions((nint)scanOptions.MinScanAddress!,
                                                  (nint)scanOptions.MaxScanAddress!,
                                                  scanOptions.MemoryAccess);

        var chunks = RegionChunker.CreateMemoryChunks(rawRegions, pattern.Bytes.Length);

        Parallel.ForEach(chunks,
            new ParallelOptions { CancellationToken = cancellationToken },
            () => new List<nint>(capacity: 64),
            (regionChunk, loopState, threadLocalList) =>
            {
                int size = (int)regionChunk.Size;
                if (size <= 0) return threadLocalList;

                byte[] rentedArray = ArrayPool<byte>.Shared.Rent(size);
                Span<byte> buffer = rentedArray.AsSpan(0, size);

                try
                {
                    if (_memoryReader.ReadMemory(regionChunk.BaseAddress, buffer, out _))
                        ScanChunk(in regionChunk, threadLocalList, in pattern, size, in buffer);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedArray);
                }

                return threadLocalList;
            },
            localList =>
            {
                lock (_syncRoot)
                    finalResults.AddRange(localList);
            });

        return finalResults;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ScanChunk(
        in MemoryRange mbi,
        List<nint> threadLocalList,
        in Pattern pattern,
        int regionsSize,
        in Span<byte> buffer)
    {
        int seqOffset = pattern.SearchSequenceOffset;
        var searchSeq = pattern.SearchSequence;
        int patternLength = pattern.Bytes.Length;
        int searchSeqLength = searchSeq.Length;

        int lastValidPatternStart = regionsSize - patternLength;
        int lastValidSeqPos = lastValidPatternStart + seqOffset;
        int currentOffset = 0;

        while (true)
        {
            int remainingLength = lastValidSeqPos - currentOffset + searchSeqLength;
            if (remainingLength < searchSeqLength)
                break;

            int hitIndex;
            if ((hitIndex = buffer.Slice(currentOffset, remainingLength).IndexOf(searchSeq)) == -1)
                break;

            int foundSeqPos = currentOffset + hitIndex;
            int patternStartPos = foundSeqPos - seqOffset;

            if (patternStartPos >= 0)
            {
                var candidateBytes = buffer.Slice(patternStartPos, patternLength);
                if (pattern.IsMatch(ref candidateBytes))
                    threadLocalList.Add(mbi.BaseAddress + patternStartPos);
            }

            currentOffset += hitIndex + 1;
        }
    }
}
