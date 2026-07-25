using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace AobscanFast.Core.Parsing;

internal class HalfMaskParser : IPatternParser
{
    public bool CanParse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        ReadOnlySpan<char> span = input;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] != '?')
                continue;

            bool prevIsHex = i > 0 && span[i - 1] != ' ' && span[i - 1] != '?';
            bool nextIsHex = i < span.Length - 1 && span[i + 1] != ' ' && span[i + 1] != '?';

            if (prevIsHex || nextIsHex)
                return true;
        }

        return false;
    }

    public AobPattern Parse(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        byte[] pooledBytes = ArrayPool<byte>.Shared.Rent(input.Length);
        byte[] pooledMask = ArrayPool<byte>.Shared.Rent(input.Length);

        try
        {
            Span<byte> pBytes = pooledBytes.AsSpan(0, input.Length);
            Span<byte> pMask = pooledMask.AsSpan(0, input.Length);

            int length = 0;
            foreach (var range in input.AsSpan().Split(' '))
            {
                ReadOnlySpan<char> token = input.AsSpan(range);

                if (token.Length == 0) continue;

                if (token.Length is < 1 or > 2)
                    throw new FormatException($"Invalid half-mask token '{token.ToString()}'.");

                ParseMaskedToken(token, out byte b, out byte m);

                pBytes[length] = b;
                pMask[length] = m;
                length++;
            }

            if (length == 0)
                throw new FormatException("Pattern must contain at least one byte token.");

            var finalBytes = pBytes[..length].ToArray();
            var finalMask = pMask[..length].ToArray();

            var (bestSeq, offset) = ParserHelpers.FindLongestSolidRun(finalBytes, finalMask);

            return new AobPattern
            {
                Bytes = finalBytes,
                Mask = finalMask,
                SearchSequence = bestSeq,
                SearchSequenceOffset = offset
            };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooledBytes);
            ArrayPool<byte>.Shared.Return(pooledMask);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseMaskedToken(ReadOnlySpan<char> token, out byte val, out byte mask)
    {
        char hChar = token.Length >= 2 ? token[0] : '0';
        char lChar = token.Length >= 2 ? token[1] : token[0];

        int hVal = 0, hMask;
        if (hChar == '?')
        {
            hMask = 0x0;
        }
        else
        {
            hMask = 0xF;
            hVal = CharToHex(hChar);
        }

        int lVal = 0, lMask;
        if (lChar == '?')
        {
            lMask = 0x0;
        }
        else
        {
            lMask = 0xF;
            lVal = CharToHex(lChar);
        }

        val = (byte)((hVal << 4) | lVal);
        mask = (byte)((hMask << 4) | lMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CharToHex(char c)
    {
        int val = c;
        return (val > '9') ? (val & ~0x20) - 'A' + 10 : (val - '0');
    }
}
