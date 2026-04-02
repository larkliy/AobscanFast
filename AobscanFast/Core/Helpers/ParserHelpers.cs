

namespace AobscanFast.Core.Helpers;

internal class ParserHelpers
{
    public static (byte[] BestSequence, int Offset) FindLongestSolidRun(ReadOnlySpan<byte> pBytes, ReadOnlySpan<byte> pMask)
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
            return ([], 0);

        return (pBytes.Slice(bestStart, bestLength).ToArray(), bestStart);
    }
}
