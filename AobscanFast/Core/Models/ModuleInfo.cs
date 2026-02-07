namespace AobscanFast.Core.Models;

internal struct ModuleInfo(nint baseAddress, uint size)
{
    public nint BaseAddress { get; set; } = baseAddress;

    public uint Size { get; set; } = size;
}