using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;
using System.Globalization;

namespace AobscanFast.Core.Parsing;

internal class SolidParser : IPatternParser
{
    public bool CanParse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        return !input.Contains('?');
    }

    public AobPattern Parse(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        byte[] pooledBytes = ArrayPool<byte>.Shared.Rent(input.Length);

        try
        {
            Span<byte> pBytes = pooledBytes;
            ReadOnlySpan<char> patternSpan = input;
            int pos = 0;
            foreach (var range in patternSpan.Split(' '))
            {
                ReadOnlySpan<char> part = patternSpan[range];

                if (part.Length == 0) continue;

                if (part.Length != 2)
                    throw new FormatException($"Invalid solid byte token '{part}'.");

                pooledBytes[pos] = byte.Parse(part, NumberStyles.HexNumber);
                pos++;
            }

            if (pos == 0)
                throw new FormatException("Pattern must contain at least one byte token.");

            return new AobPattern
            {
                Bytes = pBytes[..pos].ToArray()
            };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooledBytes);
        }
    }
}
