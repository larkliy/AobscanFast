using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Abstractions;

internal interface IPatternMatcher
{
    void ScanChunk(in MemoryRange range, AobPattern pattern,
                   List<nint> results, ReadOnlySpan<byte> buffer);
}
