using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinAobscanFast.Core.Models;

namespace WinAobscanFast.Utils;

public static class RegionChunker
{
    public static List<MemoryRange> CreateWorkChunks(List<MemoryRange> osRegions, int patternLength)
    {
        var list = new List<MemoryRange>(2048);

        const long chunkSize = 256 * 1024;

        int overlap = patternLength - 1;

        ref var rangeRef = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(osRegions));

        nuint len = (nuint)osRegions.Count;

        for (nuint i = 0; i < len; i++)
        {
            ref var range = ref Unsafe.Add(ref rangeRef, i);

            nint currentPtr = range.BaseAddress;
            long remaining = range.Size;

            while (remaining > 0)
            {
                long sizeToRead = remaining > chunkSize ? chunkSize : remaining;

                if (sizeToRead < patternLength)
                    break;

                list.Add(new MemoryRange(currentPtr, (nint)sizeToRead));

                if (sizeToRead == remaining)
                    break;

                long step = sizeToRead - overlap;

                currentPtr += (nint)step;
                remaining -= step;
            }
        }

        return list;
    }
}
