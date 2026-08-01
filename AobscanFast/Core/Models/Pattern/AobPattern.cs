using AobscanFast.Core.Helpers;
using System.Text;

namespace AobscanFast.Core.Models.Pattern;

public sealed class AobPattern
{
    private readonly byte[] _bytes;
    private readonly byte[]? _mask;
    private readonly byte[]? _searchSequence;

    private AobPattern(byte[] bytes, byte[]? mask, byte[]? searchSequence, int searchSequenceOffset)
    {
        _bytes = bytes;
        _mask = mask;
        _searchSequence = searchSequence;
        SearchSequenceOffset = searchSequenceOffset;
    }

    public ReadOnlyMemory<byte> Bytes => _bytes;
    public ReadOnlyMemory<byte> Mask => _mask;
    public ReadOnlyMemory<byte> SearchSequence => _searchSequence;
    public int SearchSequenceOffset { get; }
    public int Length => _bytes.Length;

    public bool HasMask => _mask is not null;

    internal ReadOnlySpan<byte> BytesSpan => _bytes;
    internal ReadOnlySpan<byte> MaskSpan => _mask;
    internal ReadOnlySpan<byte> SearchSequenceSpan => _searchSequence;

    public static AobPattern FromBytes(byte[] input, byte[]? mask = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            throw new ArgumentException("Pattern must contain at least one byte.", nameof(input));

        if (mask is not null && mask.Length != input.Length)
            throw new ArgumentException("Mask length must match pattern length.", nameof(mask));

        byte[] bytesCopy = (byte[])input.Clone();
        byte[]? maskCopy = mask is null ? null : (byte[])mask.Clone();

        if (maskCopy is null)
            return new AobPattern(bytesCopy, null, null, 0);

        for (int i = 0; i < bytesCopy.Length; i++)
        {
            if ((bytesCopy[i] & maskCopy[i]) != bytesCopy[i])
                throw new ArgumentException("Pattern bytes cannot contain bits outside the mask.", nameof(input));
        }

        var (sequence, offset) = ParserHelpers.FindLongestSolidRun(bytesCopy, maskCopy);
        return new AobPattern(bytesCopy, maskCopy, sequence, offset);
    }

    public static AobPattern FromString(string input, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            throw new ArgumentException("Pattern must contain at least one character.", nameof(input));

        encoding ??= Encoding.UTF8;
        return FromBytes(encoding.GetBytes(input));
    }
}
