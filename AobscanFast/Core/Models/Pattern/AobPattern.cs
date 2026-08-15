using AobscanFast.Core.Helpers;
using System.Text;

namespace AobscanFast.Core.Models.Pattern;

/// <summary>Represents a byte pattern and its optional bit mask.</summary>
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

	/// <summary>Gets the pattern bytes. The returned memory cannot mutate the pattern.</summary>
	public ReadOnlyMemory<byte> Bytes => _bytes;
	/// <summary>Gets the optional mask, or an empty memory value for an unmasked pattern.</summary>
	public ReadOnlyMemory<byte> Mask => _mask;
	/// <summary>Gets the longest contiguous fully specified byte sequence used as a search prefilter.</summary>
	public ReadOnlyMemory<byte> SearchSequence => _searchSequence;
	/// <summary>Gets the offset of <see cref="SearchSequence"/> within <see cref="Bytes"/>.</summary>
	public int SearchSequenceOffset { get; }
	/// <summary>Gets the number of bytes in the pattern.</summary>
	public int Length => _bytes.Length;

	/// <summary>Gets a value indicating whether this pattern has a mask.</summary>
	public bool HasMask => _mask is not null;

    internal ReadOnlySpan<byte> BytesSpan => _bytes;
    internal ReadOnlySpan<byte> MaskSpan => _mask;
    internal ReadOnlySpan<byte> SearchSequenceSpan => _searchSequence;

    internal void Clear()
    {
        Array.Clear(_bytes);
        if (_mask is not null)
            Array.Clear(_mask);
        if (_searchSequence is not null)
            Array.Clear(_searchSequence);
    }

	/// <summary>Creates a pattern from bytes and an optional bit mask.</summary>
	/// <param name="input">The pattern bytes. The array is copied.</param>
	/// <param name="mask">An optional mask with the same length as <paramref name="input"/>. Set bits are matched.</param>
	/// <returns>A new immutable <see cref="AobPattern"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">The input is empty, the mask length differs, or input contains bits outside the mask.</exception>
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

	/// <summary>Creates an exact byte pattern from the encoded text.</summary>
	/// <param name="input">The text to encode.</param>
	/// <param name="encoding">The encoding to use, or UTF-8 when omitted.</param>
	/// <returns>A new unmasked <see cref="AobPattern"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="input"/> is empty.</exception>
	public static AobPattern FromString(string input, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            throw new ArgumentException("Pattern must contain at least one character.", nameof(input));

        encoding ??= Encoding.UTF8;
        return FromBytes(encoding.GetBytes(input));
    }
}
