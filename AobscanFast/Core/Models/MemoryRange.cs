namespace AobscanFast.Core.Models;

/// <summary>Describes a contiguous virtual memory range.</summary>
/// <param name="BaseAddress">The starting virtual address.</param>
/// <param name="Size">The range size in bytes.</param>
public readonly record struct MemoryRange(nint BaseAddress, nint Size);
