using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Matching;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;

namespace AobscanFast.Services;

public class AobScanner(IProcessHandler processHandler, IMemoryReader memoryReader)
{
    private readonly Lock _syncRoot = new();

    public List<nint> ScanModule(uint processId, string moduleName, string pattern, CancellationToken ct = default)
    {
        var modInfo = processHandler.GetModuleInfo(processId, moduleName);

        if (modInfo == null)
            return [];

        var options = new AobScanOptions
        {
            MinScanAddress = modInfo.Value.BaseAddress,
            MaxScanAddress = (nint)(modInfo.Value.BaseAddress + modInfo.Value.Size)
        };

        return Scan(pattern, options, ct);
    }

    public List<nint> Scan(string patternInput, AobScanOptions? options = null, CancellationToken ct = default)
    {
        options ??= new();

        var pattern = AobPattern.Parse(patternInput);
        var matcher = MatcherFactory.GetMatcher(pattern);

        var rawRegions = memoryReader.GetRegions(
            options.MinScanAddress,
            options.MaxScanAddress,
            options.MemoryAccess);

        var mergedRegions = RegionProcessor.MergeRegions(rawRegions);
        var chunks = RegionProcessor.CreateMemoryChunks(mergedRegions, pattern.Bytes.Length);

        var finalResults = new List<nint>(1024);

        Parallel.ForEach(chunks, 

            new ParallelOptions { CancellationToken = ct },
            () => new List<nint>(64),

            (chunk, state, localList) =>
            {
                int size = (int)chunk.Size;
                byte[] rentedArray = ArrayPool<byte>.Shared.Rent(size);
                Span<byte> buffer = rentedArray.AsSpan(0, size);

                try
                {
                    if (memoryReader.ReadMemory(chunk.BaseAddress, buffer, out _))
                    {
                        matcher.ScanChunk(chunk, pattern, localList, buffer);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedArray);
                }
                return localList;
            },
            localList =>
            {
                lock (_syncRoot) 
                    finalResults.AddRange(localList);
            });

        return finalResults;
    }
}
