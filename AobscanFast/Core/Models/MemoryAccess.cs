namespace AobscanFast.Core.Models;

/// <summary>Specifies required virtual memory protection flags.</summary>
[Flags]
public enum MemoryAccess
{
	/// <summary>No memory access requirement.</summary>
	None = 0,
	/// <summary>Readable memory.</summary>
	Readable = 1,
	/// <summary>Writable memory.</summary>
	Writable = 2,
	/// <summary>Executable memory.</summary>
	Executable = 4
}
