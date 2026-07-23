namespace AobscanFast.Core.Models;

public class AobScanOptions
{
    public MemoryAccess MemoryAccess { get; init; } = MemoryAccess.Readable;
    public nint MinScanAddress { get; init; } = 0;
    public nint MaxScanAddress { get; init; } = nint.MaxValue;
    public nint ChunkSize { get; init; } = 256 * 1024;
    public int MaxDegreeOfParallelism { get; init; } = -1;
    public int MaxResults { get; init; } = 0;
}