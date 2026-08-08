namespace AobscanFast.Core.Models;

/// <summary>Configures memory range, concurrency, and result limits for a scan.</summary>
public class AobScanOptions
{
	/// <summary>Gets the memory protection flags required for regions to be scanned.</summary>
	public MemoryAccess MemoryAccess { get; init; } = MemoryAccess.Readable;
	/// <summary>Gets the inclusive lower address of the scan range.</summary>
	public nint MinScanAddress { get; init; } = 0;
	/// <summary>Gets the exclusive upper address of the scan range.</summary>
	public nint MaxScanAddress { get; init; } = nint.MaxValue;
	/// <summary>Gets the size, in bytes, of each scan chunk.</summary>
	public nint ChunkSize { get; init; } = 256 * 1024;
	/// <summary>Gets the maximum number of concurrent scan operations. A value of -1 uses the platform default.</summary>
	public int MaxDegreeOfParallelism { get; init; } = -1;
	/// <summary>Gets the maximum number of matches to return. A value of 0 means no limit.</summary>
	public int MaxResults { get; init; } = 0;
}
