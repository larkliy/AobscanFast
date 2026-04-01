namespace AobscanFast.Core.Models;

public class AobScanOptions
{
    public MemoryAccess MemoryAccess { get; init; } = MemoryAccess.Readable;
    public nint MinScanAddress { get; init; } = 0;
    public nint MaxScanAddress { get; init; } = nint.MaxValue;
}