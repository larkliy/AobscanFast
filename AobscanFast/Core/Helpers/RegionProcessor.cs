using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using System.Runtime.InteropServices;

namespace AobscanFast.Core.Helpers;

/// <summary>Provides default memory range merging and chunk planning.</summary>
public sealed class RegionProcessor : IMemoryRangePlanner
{
    /// <inheritdoc/>
    public List<MemoryRange> CreateScanChunks(List<MemoryRange> ranges, int patternLength, nint chunkSize)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        if (patternLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(patternLength), "Pattern length must be greater than zero.");

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");

        if (patternLength > chunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size", nameof(patternLength));

        var result = new List<MemoryRange>(ranges.Count);
        nint overlap = patternLength - 1;

        foreach (ref readonly var range in CollectionsMarshal.AsSpan(ranges))
        {
            ValidateRange(range, nameof(ranges));

            nint currentAddress = range.BaseAddress;
            nint remainingBytes = range.Size;

            while (remainingBytes >= patternLength)
            {
                nint chunkLength = Math.Min(remainingBytes, chunkSize);

                result.Add(new MemoryRange(currentAddress, chunkLength));

                if (chunkLength == remainingBytes)
                    break;

                nint advance = chunkLength - overlap;
                currentAddress = checked(currentAddress + advance);
                remainingBytes -= advance;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public List<MemoryRange> MergeAdjacentRegions(List<MemoryRange> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        if (regions.Count == 0)
            return regions;

        var sorted = new List<MemoryRange>(regions);
        sorted.Sort((a, b) => a.BaseAddress.CompareTo(b.BaseAddress));

        foreach (ref readonly var range in CollectionsMarshal.AsSpan(sorted))
            ValidateRange(range, nameof(regions));

        var span = CollectionsMarshal.AsSpan(sorted);
        var result = new List<MemoryRange>(sorted.Count);
        MemoryRange currentRange = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            ref readonly var nextRange = ref span[i];

            nint currentEnd = checked(currentRange.BaseAddress + currentRange.Size);
            nint nextEnd = checked(nextRange.BaseAddress + nextRange.Size);

            if (currentEnd >= nextRange.BaseAddress)
            {
                nint mergedEnd = Math.Max(currentEnd, nextEnd);
                nint mergedSize = checked(mergedEnd - currentRange.BaseAddress);
                currentRange = new MemoryRange(currentRange.BaseAddress, mergedSize);
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

    private static void ValidateRange(in MemoryRange range, string parameterName)
    {
        if (range.Size < 0)
            throw new ArgumentException("Memory range size cannot be negative.", parameterName);

        try
        {
            _ = checked(range.BaseAddress + range.Size);
        }
        catch (OverflowException ex)
        {
            throw new ArgumentException("Memory range end address is outside the native integer range.", parameterName, ex);
        }
    }
}
