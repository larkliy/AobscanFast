using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace WinAobscanFast;

public readonly struct Pattern
{
    public readonly byte[] Bytes;
    public readonly byte[] Mask;

    public readonly byte[] SearchSequence;
    public readonly int SearchSequenceOffset;

    private Pattern(byte[] bytes, byte[] mask, byte[] searchSequence, int searchSequenceOffset)
    {
        Bytes = bytes;
        Mask = mask;
        SearchSequence = searchSequence;
        SearchSequenceOffset = searchSequenceOffset;
    }

    public static Pattern Create(string pattern)
    {
        string[] tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        byte[] pBytes = new byte[tokens.Length];
        byte[] pMask = new byte[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            if (token == "?" || token == "??")
            {
                pBytes[i] = byte.MinValue;
                pMask[i] = byte.MinValue;
            }
            else
            {
                pBytes[i] = byte.Parse(token, NumberStyles.HexNumber);
                pMask[i] = byte.MaxValue;
            }
        }

        if (!pMask.Any(p => p == byte.MaxValue))
            throw new FormatException("A pattern cannot consist of masks alone.");

        var (bestSeq, offset) = FindLongestSolidRun(pBytes, pMask);

        return new Pattern(pBytes, pMask, bestSeq, offset);
    }

    private static (byte[] BestSequence, int Offset) FindLongestSolidRun(ReadOnlySpan<byte> pBytes, ReadOnlySpan<byte> pMask)
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
    public bool IsMatch(ReadOnlySpan<byte> pData)
    {
        int length = Bytes.Length;

        if (pData.Length < length)
            return false;

        ReadOnlySpan<byte> pMask = Mask;
        ReadOnlySpan<byte> pBytes = Bytes;

        int vecSize = Vector<byte>.Count;
        int i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            int simdEnd = length - vecSize;

            while (i <= simdEnd)
            {
                var vBytes = new Vector<byte>(pBytes[i..]);
                var vMask = new Vector<byte>(pMask[i..]);
                var vData = new Vector<byte>(pData[i..]);

                if (!Vector.EqualsAll(vData & vMask, vBytes))
                    return false;

                i += vecSize;
            }
        }

        for (; i < length; i++)
        {
            if ((pData[i] & pMask[i]) != pBytes[i])
                return false;
        }

        return true;
    }
}
