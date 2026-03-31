using AobscanFast.Core.Models;
using AobscanFast.Services;

namespace AobscanFast.Tests.Unit;

public class RegionProcessorTests
{
    [Fact]
    public void MergeRegions_EmptyList_ReturnsEmpty()
    {
        var result = RegionProcessor.MergeRegions([]);

        Assert.Empty(result);
    }

    [Fact]
    public void MergeRegions_SingleRegion_ReturnsSame()
    {
        var regions = new List<MemoryRange>
        {
            new(0x1000, 0x100)
        };

        var result = RegionProcessor.MergeRegions(regions);

        Assert.Single(result);
        Assert.Equal(0x1000, result[0].BaseAddress);
        Assert.Equal(0x100, result[0].Size);
    }

    [Fact]
    public void MergeRegions_TwoAdjacent_MergesIntoOne()
    {
        var regions = new List<MemoryRange>
        {
            new(0x1000, 0x100),
            new(0x1100, 0x200),
        };

        var result = RegionProcessor.MergeRegions(regions);

        Assert.Single(result);
        Assert.Equal(0x1000, result[0].BaseAddress);
        Assert.Equal(0x300, result[0].Size);
    }

    [Fact]
    public void MergeRegions_ThreeAdjacent_MergesAll()
    {
        var regions = new List<MemoryRange>
        {
            new(0x1000, 0x100),
            new(0x1100, 0x100),
            new(0x1200, 0x100),
        };

        var result = RegionProcessor.MergeRegions(regions);

        Assert.Single(result);
        Assert.Equal(0x1000, result[0].BaseAddress);
        Assert.Equal(0x300, result[0].Size);
    }

    [Fact]
    public void MergeRegions_NonAdjacent_KeepsSeparate()
    {
        var regions = new List<MemoryRange>
        {
            new(0x1000, 0x100),
            new(0x2000, 0x100),
        };

        var result = RegionProcessor.MergeRegions(regions);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void MergeRegions_MixedAdjacentAndGap()
    {
        var regions = new List<MemoryRange>
        {
            new(0x1000, 0x100),
            new(0x1100, 0x100),  // adjacent to first
            new(0x3000, 0x200),  // gap
            new(0x3200, 0x100),  // adjacent to third
        };

        var result = RegionProcessor.MergeRegions(regions);

        Assert.Equal(2, result.Count);
        Assert.Equal(0x1000, result[0].BaseAddress);
        Assert.Equal(0x200, result[0].Size);
        Assert.Equal(0x3000, result[1].BaseAddress);
        Assert.Equal(0x300, result[1].Size);
    }


    [Fact]
    public void CreateMemoryChunks_SmallRegion_SingleChunk()
    {
        var ranges = new List<MemoryRange> { new(0x1000, 100) };

        var result = RegionProcessor.CreateMemoryChunks(ranges, 4);

        Assert.Single(result);
        Assert.Equal(0x1000, result[0].BaseAddress);
        Assert.Equal(100, result[0].Size);
    }

    [Fact]
    public void CreateMemoryChunks_RegionSmallerThanPattern_NoChunks()
    {
        var ranges = new List<MemoryRange> { new(0x1000, 3) };

        var result = RegionProcessor.CreateMemoryChunks(ranges, 4);

        Assert.Empty(result);
    }

    [Fact]
    public void CreateMemoryChunks_ExactPatternLength_SingleChunk()
    {
        var ranges = new List<MemoryRange> { new(0x1000, 4) };

        var result = RegionProcessor.CreateMemoryChunks(ranges, 4);

        Assert.Single(result);
        Assert.Equal(4, result[0].Size);
    }

    [Fact]
    public void CreateMemoryChunks_LargeRegion_ChunksOverlap()
    {
        nint chunkSize = 256 * 1024;
        int patternLen = 8;
        nint regionSize = chunkSize + 100;

        var ranges = new List<MemoryRange> { new(0x0, regionSize) };

        var result = RegionProcessor.CreateMemoryChunks(ranges, patternLen);

        Assert.Equal(2, result.Count);
        // First chunk is full size
        Assert.Equal(0, result[0].BaseAddress);
        Assert.Equal(chunkSize, result[0].Size);
        // Second chunk starts at chunkSize - (patternLen - 1) for overlap
        nint expectedStart = chunkSize - (patternLen - 1);
        Assert.Equal(expectedStart, result[1].BaseAddress);
    }

    [Fact]
    public void CreateMemoryChunks_PatternTooLarge_Throws()
    {
        var ranges = new List<MemoryRange> { new(0x1000, 1000) };

        Assert.Throws<ArgumentException>(
            () => RegionProcessor.CreateMemoryChunks(ranges, 256 * 1024));
    }
}
