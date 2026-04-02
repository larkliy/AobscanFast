using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models.Pattern;
using System.Buffers;
using System.Globalization;

namespace AobscanFast.Core.Parsing;

internal class SolidParser : IPatternParser
{
    public AobPattern Parse(string input)
    {
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

                pooledBytes[pos] = byte.Parse(part, NumberStyles.HexNumber);
                pos++;
            }

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
