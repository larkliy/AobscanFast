using AobscanFast.Core.Models;

namespace AobscanFast.Core.Interfaces;

/// <summary>Normalizes memory ranges and divides them into scan chunks.</summary>
public interface IMemoryRangePlanner
{
    /// <summary>Merges overlapping and adjacent ranges.</summary><param name="regions">The ranges to merge.</param><returns>Sorted, non-overlapping ranges.</returns>
    List<MemoryRange> MergeAdjacentRegions(List<MemoryRange> regions);
    /// <summary>Creates overlapping chunks suitable for scanning a pattern.</summary><param name="ranges">The ranges to divide.</param><param name="patternLength">The pattern length in bytes.</param><param name="chunkSize">The maximum chunk size in bytes.</param><returns>The scan chunks.</returns>
    List<MemoryRange> CreateScanChunks(List<MemoryRange> ranges, int patternLength, nint chunkSize);
}
