using AobscanFast.Core.Helpers;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;
using System.Globalization;

namespace AobscanFast.Core.Parsing;

internal class MaskParser : IPatternParser
{
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
                ReadOnlySpan<char> part = input.AsSpan(range);

                if (part.Length == 0) continue;

                if (part.SequenceEqual("?".AsSpan()) || part.SequenceEqual("??".AsSpan()))
                {
                    pBytes[length] = 0;
                    pMask[length] = 0;
                }
                else
                {
                    if (part.Length != 2)
                        throw new FormatException($"Invalid masked byte token '{part.ToString()}'.");

                    pBytes[length] = byte.Parse(part, NumberStyles.HexNumber);
                    pMask[length] = 0xFF;
                }
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
}
