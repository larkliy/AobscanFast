using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    public bool IsMatch(ReadOnlySpan<byte> data)
    {
        nuint length = (nuint)Bytes.Length;

        if ((nuint)data.Length < length)
            return false;

        ref var pBytes = ref MemoryMarshal.GetArrayDataReference(Bytes);
        ref var pMask = ref MemoryMarshal.GetArrayDataReference(Mask);
        ref var pData = ref MemoryMarshal.GetReference(data);

        nuint i = 0;
        nuint vecSize = (nuint)Vector<byte>.Count;

        if (Vector.IsHardwareAccelerated && length >= vecSize)
        {
            nuint lastVecOffset = length - vecSize;

            while (i <= lastVecOffset)
            {
                var vBytes = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref pBytes, i));
                var vMask = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref pMask, i));
                var vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref pData, i));

                if (!Vector.EqualsAll(vData & vMask, vBytes))
                    return false;

                i += vecSize;
            }

        }

        for (; i < length; i++)
        {
            if ((Unsafe.Add(ref pData, i) & Unsafe.Add(ref pMask, i)) != Unsafe.Add(ref pBytes, i))
                return false;
        }

        return true;
    }
}
