using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;

namespace AobscanFast.Core.Helpers;

public sealed class RegionProcessor : IMemoryRangePlanner
{
    public List<MemoryRange> CreateScanChunks(List<MemoryRange> ranges, int patternLength)
    {
        const nint chunkSize = 256 * 1024;

        ArgumentNullException.ThrowIfNull(ranges);

        if (patternLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(patternLength), "Pattern length must be greater than zero.");

        if (patternLength >= chunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size", nameof(patternLength));

        var result = new List<MemoryRange>(ranges.Count * 5);
        nint overlap = patternLength - 1;

        foreach (ref readonly var range in CollectionsMarshal.AsSpan(ranges))
        {
            nint currentAddress = range.BaseAddress;
            nint remainingBytes = range.Size;

            while (remainingBytes >= patternLength)
            {
                nint chunkLength = Math.Min(remainingBytes, chunkSize);

                result.Add(new MemoryRange(currentAddress, chunkLength));

                if (chunkLength == remainingBytes)
                    break;

                nint advance = chunkLength - overlap;
                currentAddress += advance;
                remainingBytes -= advance;
            }
        }

        return result;
    }

    public List<MemoryRange> MergeAdjacentRegions(List<MemoryRange> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        if (regions.Count == 0)
            return regions;

        var result = new List<MemoryRange>(regions.Count);
        var span = CollectionsMarshal.AsSpan(regions);

        MemoryRange currentRange = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            ref readonly var nextRange = ref span[i];

            if (currentRange.BaseAddress + currentRange.Size == nextRange.BaseAddress)
            {
                currentRange = new MemoryRange(currentRange.BaseAddress, currentRange.Size + nextRange.Size);
            }
            else
            {
                result.Add(currentRange);
                currentRange = nextRange;
            }
        }

        result.Add(currentRange);

        return result;
    }
}
