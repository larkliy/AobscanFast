using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace AobscanFast.Core.Matching
{
    internal sealed class MaskMatcher : IPatternMatcher
    {
        public void ScanChunk(in MemoryRange range, AobPattern pattern, List<nint> results, ReadOnlySpan<byte> buffer)
        {
            int lastValidPatternStart = (int)(range.Size - pattern.Length);
            ReadOnlySpan<byte> searchSequence = pattern.SearchSequenceSpan;

            if (searchSequence.Length == 0)
            {
                bool isFullyWildcard = pattern.MaskSpan.IndexOfAnyExcept((byte)0) < 0;
                for (int patternStart = 0; patternStart <= lastValidPatternStart; patternStart++)
                {
                    if (isFullyWildcard || IsMatch(pattern, buffer.Slice(patternStart, pattern.Length)))
                        results.Add(range.BaseAddress + patternStart);
                }

                return;
            }

            int lastValidSeqPos = lastValidPatternStart + pattern.SearchSequenceOffset;
            int currentOffset = 0;

            while (true)
            {
                int remainingLength = lastValidSeqPos - currentOffset + searchSequence.Length;
                if (remainingLength < searchSequence.Length)
                    break;

                int hitIndex;
                if ((hitIndex = buffer.Slice(currentOffset, remainingLength).IndexOf(searchSequence)) == -1)
                    break;

                int foundSeqPos = currentOffset + hitIndex;
                int patternStartPos = foundSeqPos - pattern.SearchSequenceOffset;

                if (patternStartPos >= 0)
                {
                    var candidateBytes = buffer.Slice(patternStartPos, pattern.Length);
                    if (IsMatch(pattern, candidateBytes))
                        results.Add(range.BaseAddress + patternStartPos);
                }

                currentOffset += hitIndex + 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMatch(AobPattern pattern, ReadOnlySpan<byte> data)
        {
            nuint length = (nuint)pattern.Length;

            if ((nuint)data.Length < length)
                return false;

            ReadOnlySpan<byte> mask = pattern.MaskSpan;
            if (mask.IsEmpty)
                return false;

            ref byte pBytes = ref MemoryMarshal.GetReference(pattern.BytesSpan);
            ref byte pMask = ref MemoryMarshal.GetReference(mask);
            ref byte pData = ref MemoryMarshal.GetReference(data);

            nuint i = 0;

            if (Vector512.IsHardwareAccelerated && length >= (nuint)Vector512<byte>.Count)
            {
                nuint limit = length - (nuint)Vector512<byte>.Count;
                while (i <= limit)
                {
                    var vData = Vector512.LoadUnsafe(ref pData, i);
                    var vMask = Vector512.LoadUnsafe(ref pMask, i);
                    var vBytes = Vector512.LoadUnsafe(ref pBytes, i);

                    if ((vData & vMask) != vBytes)
                        return false;

                    i += (nuint)Vector512<byte>.Count;
                }
            }

            if (Vector256.IsHardwareAccelerated && (length - i) >= (nuint)Vector256<byte>.Count)
            {
                nuint limit = length - (nuint)Vector256<byte>.Count;
                while (i <= limit)
                {
                    var vData = Vector256.LoadUnsafe(ref pData, i);
                    var vMask = Vector256.LoadUnsafe(ref pMask, i);
                    var vBytes = Vector256.LoadUnsafe(ref pBytes, i);

                    if ((vData & vMask) != vBytes)
                        return false;

                    i += (nuint)Vector256<byte>.Count;
                }
            }

            if (Vector128.IsHardwareAccelerated && (length - i) >= (nuint)Vector128<byte>.Count)
            {
                nuint limit = length - (nuint)Vector128<byte>.Count;
                while (i <= limit)
                {
                    var vData = Vector128.LoadUnsafe(ref pData, i);
                    var vMask = Vector128.LoadUnsafe(ref pMask, i);
                    var vBytes = Vector128.LoadUnsafe(ref pBytes, i);

                    if ((vData & vMask) != vBytes)
                        return false;

                    i += (nuint)Vector128<byte>.Count;
                }
            }

            while (i < length)
            {
                if ((Unsafe.Add(ref pData, i) & Unsafe.Add(ref pMask, i)) != Unsafe.Add(ref pBytes, i))
                    return false;
                i++;
            }

            return true;
        }
    }
}
