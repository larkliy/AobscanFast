using AobscanFast.Core.Models;

namespace AobscanFast.Core.Interfaces;

public interface IMemoryRangePlanner
{
    List<MemoryRange> MergeAdjacentRegions(List<MemoryRange> regions);
    List<MemoryRange> CreateScanChunks(List<MemoryRange> ranges, int patternLength);
}
