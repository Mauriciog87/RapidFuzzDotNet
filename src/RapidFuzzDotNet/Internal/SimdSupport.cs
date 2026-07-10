using System.Numerics;
using System.Runtime.InteropServices;

namespace RapidFuzz.Internal;

internal static class SimdSupport
{
    public static int PreferredWordStride
    {
        get
        {
            if (!Vector.IsHardwareAccelerated)
            {
                return 1;
            }

            return Vector<ulong>.Count >= 4 ? 4 : Math.Max(Vector<ulong>.Count, 1);
        }
    }

    public static void ApplySimilarityCutoff(Span<double> scores, double scoreCutoff)
    {
        if (scoreCutoff <= 0.0 || scores.IsEmpty)
        {
            return;
        }

        int index = 0;

        if (Vector.IsHardwareAccelerated && scores.Length >= Vector<double>.Count)
        {
            Vector<double> cutoff = new(scoreCutoff);
            int vectorWidth = Vector<double>.Count;

            for (; index + vectorWidth <= scores.Length; index += vectorWidth)
            {
                Vector<double> values = new(scores.Slice(index, vectorWidth));
                Vector<long> mask = Vector.GreaterThanOrEqual(values, cutoff);
                Vector<double> filtered = Vector.ConditionalSelect(mask, values, Vector<double>.Zero);
                filtered.CopyTo(scores[index..]);
            }
        }

        for (; index < scores.Length; index++)
        {
            if (scores[index] < scoreCutoff)
            {
                scores[index] = 0.0;
            }
        }
    }

    public static void ApplySimilarityCutoff(Span<int> scores, int scoreCutoff)
    {
        if (scoreCutoff <= 0 || scores.IsEmpty)
        {
            return;
        }

        int index = 0;

        if (Vector.IsHardwareAccelerated && scores.Length >= Vector<int>.Count)
        {
            Vector<int> cutoff = new(scoreCutoff);
            int vectorWidth = Vector<int>.Count;

            for (; index + vectorWidth <= scores.Length; index += vectorWidth)
            {
                Vector<int> values = new(scores.Slice(index, vectorWidth));
                Vector<int> mask = Vector.GreaterThanOrEqual(values, cutoff);
                Vector<int> filtered = Vector.ConditionalSelect(mask, values, Vector<int>.Zero);
                filtered.CopyTo(scores[index..]);
            }
        }

        for (; index < scores.Length; index++)
        {
            if (scores[index] < scoreCutoff)
            {
                scores[index] = 0;
            }
        }
    }

    public static int CommonPrefixLength(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int maximumLength)
    {
        int maximum = Math.Min(Math.Min(first.Length, second.Length), maximumLength);
        int index = 0;

        if (Vector.IsHardwareAccelerated && maximum >= Vector<ushort>.Count)
        {
            ReadOnlySpan<ushort> firstValues = MemoryMarshal.Cast<char, ushort>(first);
            ReadOnlySpan<ushort> secondValues = MemoryMarshal.Cast<char, ushort>(second);
            int vectorWidth = Vector<ushort>.Count;

            for (; index + vectorWidth <= maximum; index += vectorWidth)
            {
                Vector<ushort> firstVector = new(firstValues.Slice(index, vectorWidth));
                Vector<ushort> secondVector = new(secondValues.Slice(index, vectorWidth));

                if (!Vector.EqualsAll(firstVector, secondVector))
                {
                    break;
                }
            }
        }

        for (; index < maximum; index++)
        {
            if (first[index] != second[index])
            {
                return index;
            }
        }

        return maximum;
    }

    public static int CommonPostfixLength(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int maximumLength)
    {
        int maximum = Math.Min(Math.Min(first.Length, second.Length), maximumLength);
        int length = 0;

        while (length < maximum && first[first.Length - length - 1] == second[second.Length - length - 1])
        {
            length++;
        }

        return length;
    }

    public static int CountMismatches(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        int length = Math.Min(first.Length, second.Length);
        int mismatches = 0;
        int index = 0;

        if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count)
        {
            ReadOnlySpan<ushort> firstValues = MemoryMarshal.Cast<char, ushort>(first);
            ReadOnlySpan<ushort> secondValues = MemoryMarshal.Cast<char, ushort>(second);
            int vectorWidth = Vector<ushort>.Count;

            for (; index + vectorWidth <= length; index += vectorWidth)
            {
                Vector<ushort> firstVector = new(firstValues.Slice(index, vectorWidth));
                Vector<ushort> secondVector = new(secondValues.Slice(index, vectorWidth));

                if (Vector.EqualsAll(firstVector, secondVector))
                {
                    continue;
                }

                for (int offset = 0; offset < vectorWidth; offset++)
                {
                    if (first[index + offset] != second[index + offset])
                    {
                        mismatches++;
                    }
                }
            }
        }

        for (; index < length; index++)
        {
            if (first[index] != second[index])
            {
                mismatches++;
            }
        }

        return mismatches;
    }
}
