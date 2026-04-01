using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Interfaces;

public interface IPatternMatcher
{
    void ScanChunk(in MemoryRange range, AobPattern pattern,
                   List<nint> results, ReadOnlySpan<byte> buffer);
}
