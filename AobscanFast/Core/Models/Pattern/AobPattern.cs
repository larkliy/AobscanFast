using AobscanFast.Core.Helpers;
using System.Text;

namespace AobscanFast.Core.Models.Pattern;

public sealed class AobPattern
{
    public byte[] Bytes { get; init; } = [];
    public byte[]? Mask { get; init; }
    public byte[]? SearchSequence { get; init; }
    public int SearchSequenceOffset { get; init; }
    public int Length => Bytes.Length;

    public bool HasMask => Mask is not null;

    public static AobPattern FromBytes(byte[] input, byte[]? mask = null)
    {
        if (mask is not null)
        {
            var (seq, offset) = ParserHelpers.FindLongestSolidRun(input, mask);

            return new AobPattern
            {
                Bytes = input,
                Mask = mask,
                SearchSequence = seq,
                SearchSequenceOffset = offset
            };
        }

        return new AobPattern
        {
            Bytes = input,
            Mask = mask
        };
    }

    public static AobPattern FromString(string input, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        byte[] convertedBytes = encoding.GetBytes(input);

        return new()
        {
            Bytes = convertedBytes
        };
    }
}
