using System.Buffers;
using System.Runtime.CompilerServices;

namespace AobscanFast.Core.Models.Pattern;

public sealed class AobPattern
{
    public byte[] Bytes { get; init; } = [];
    public byte[]? Mask { get; init; }
    public byte[]? SearchSequence { get; init; }
    public int SearchSequenceOffset { get; init; }

    public bool HasMask => Mask is not null;

    private AobPattern() { }

    public static AobPattern Parse(string input)
    {
        byte[] pooledBytes = ArrayPool<byte>.Shared.Rent(input.Length);
        byte[] pooledMask = ArrayPool<byte>.Shared.Rent(input.Length);

        byte[] bytes = null!;
        byte[] mask = null!;
        byte[] searchSequence = null!;
        int searchSequenceOffset = 0;

        try
        {
            if (!input.Contains("??"))
            {
                Span<byte> pBytes = pooledBytes;
                ReadOnlySpan<char> patternSpan = input;
                int pos = 0;
                foreach (var range in patternSpan.Split(' '))
                {
                    ReadOnlySpan<char> part = patternSpan[range];
                    pooledBytes[pos] = HexToByte(part);
                    pos++;
                }

                bytes = pBytes[..pos].ToArray();
            }
            else
            {
                Span<byte> pBytes = pooledBytes.AsSpan(0, input.Length);
                Span<byte> pMask = pooledMask.AsSpan(0, input.Length);

                int length = 0;
                foreach (var range in input.AsSpan().Split(' '))
                {
                    ReadOnlySpan<char> token = input.AsSpan(range);

                    if (token.Length >= 1 && token[0] == '?')
                    {
                        pBytes[length] = 0;
                        pMask[length] = 0;
                    }
                    else
                    {
                        pBytes[length] = HexToByte(token);
                        pMask[length] = 0xFF;
                    }
                    length++;
                }

                var finalBytes = pBytes[..length].ToArray();
                var finalMask = pMask[..length].ToArray();

                if (!finalMask.Any(b => b == 0xFF))
                    throw new FormatException("A pattern cannot consist of masks alone.");

                var (bestSeq, offset) = FindLongestSolidRun(finalBytes, finalMask);

                bytes = finalBytes;
                mask = finalMask;
                searchSequence = bestSeq;
                searchSequenceOffset = offset;
            }

            return new AobPattern 
            {
                Bytes = bytes,
                Mask = mask,
                SearchSequence = searchSequence,
                SearchSequenceOffset = searchSequenceOffset,
            };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooledBytes);
            ArrayPool<byte>.Shared.Return(pooledMask);
        }
    }

    private static (byte[] BestSequence, int Offset) FindLongestSolidRun(ReadOnlySpan<byte> pBytes,
                                                                     ReadOnlySpan<byte> pMask)
    {
        int bestStart = 0;
        int bestLength = 0;

        int currentStart = 0;
        int currentLength = 0;

        for (int i = 0; i < pMask.Length; i++)
        {
            if (pMask[i] == byte.MaxValue)
            {
                if (currentLength == 0)
                    currentStart = i;

                currentLength++;

                if (currentLength > bestLength)
                {
                    bestStart = currentStart;
                    bestLength = currentLength;
                }
            }
            else
            {
                currentLength = 0;
            }
        }

        if (bestLength == 0)
            return ([], -1);

        return (pBytes.Slice(bestStart, bestLength).ToArray(), bestStart);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte HexToByte(ReadOnlySpan<char> s)
    {
        int h = s[0];
        int l = s[1];
        h = (h > '9') ? (h & ~0x20) - 'A' + 10 : (h - '0');
        l = (l > '9') ? (l & ~0x20) - 'A' + 10 : (l - '0');
        return (byte)((h << 4) | l);
    }
}
