using AobscanFast.Enums.Windows;

namespace AobscanFast.Core;

public class AobScanOptions(MemoryAccess memoryAccess = MemoryAccess.None,
                            nint? minScanAddress = null,
                            nint? maxScanAddress = null)
{
    public MemoryAccess MemoryAccess { get; set; } = memoryAccess == MemoryAccess.None ? MemoryAccess.Readable : memoryAccess;
    public nint? MinScanAddress { get; set; } = minScanAddress ?? 0;
    public nint? MaxScanAddress { get; set; } = maxScanAddress ?? nint.MaxValue;
}
