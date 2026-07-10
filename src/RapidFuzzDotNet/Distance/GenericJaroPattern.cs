using System.Buffers;
using System.Numerics;
using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

internal sealed class GenericJaroPattern<T>
    where T : notnull, IEquatable<T>
{
    private const int StackLimit = 256;
    private readonly T[] source;
    private readonly GenericPatternMatchVector<T> pattern;

    public GenericJaroPattern(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
        pattern = new GenericPatternMatchVector<T>(this.source);
    }

    public ReadOnlySpan<T> Source => source;

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff)
    {
        if (scoreCutoff > 1.0)
        {
            return 0.0;
        }

        if (source.Length == 0 && target.IsEmpty)
        {
            return 1.0;
        }

        if (source.Length == 0 || target.IsEmpty)
        {
            return 0.0;
        }

        if (source.Length == 1 && target.Length == 1)
        {
            return EqualityComparer<T>.Default.Equals(source[0], target[0]) ? 1.0 : 0.0;
        }

        int flagCount = pattern.BlockCount;
        int matchCapacity = Math.Min(source.Length, target.Length);
        ulong[]? rentedFlags = null;
        int[]? rentedMatches = null;
        Span<ulong> sourceFlags = flagCount <= StackLimit
            ? stackalloc ulong[flagCount]
            : (rentedFlags = ArrayPool<ulong>.Shared.Rent(flagCount)).AsSpan(0, flagCount);
        Span<int> targetMatches = matchCapacity <= StackLimit
            ? stackalloc int[matchCapacity]
            : (rentedMatches = ArrayPool<int>.Shared.Rent(matchCapacity)).AsSpan(0, matchCapacity);
        sourceFlags.Clear();

        try
        {
            int matchDistance = Math.Max(Math.Max(source.Length, target.Length) / 2 - 1, 0);
            int matches = 0;

            for (int targetIndex = 0; targetIndex < target.Length; targetIndex++)
            {
                int start = Math.Max(0, targetIndex - matchDistance);
                int stop = Math.Min(source.Length, targetIndex + matchDistance + 1);
                int sourceIndex = FindMatch(target[targetIndex], start, stop, sourceFlags);

                if (sourceIndex < 0)
                {
                    continue;
                }

                sourceFlags[sourceIndex / 64] |= 1UL << (sourceIndex & 63);
                targetMatches[matches] = targetIndex;
                matches++;
            }

            if (matches == 0)
            {
                return 0.0;
            }

            int transpositions = CountTranspositions(sourceFlags, target, targetMatches[..matches]);
            double matchCount = matches;
            int halfTranspositions = transpositions / 2;
            double similarity = ((matchCount / source.Length)
                + (matchCount / target.Length)
                + ((matchCount - halfTranspositions) / matchCount)) / 3.0;
            return similarity >= scoreCutoff ? similarity : 0.0;
        }
        finally
        {
            if (rentedFlags is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedFlags);
            }

            if (rentedMatches is not null)
            {
                ArrayPool<int>.Shared.Return(rentedMatches);
            }
        }
    }

    public double WinklerSimilarity(ReadOnlySpan<T> target, double prefixWeight, double scoreCutoff)
    {
        double jaroCutoff = GetJaroCutoff(prefixWeight, scoreCutoff);
        double similarity = Similarity(target, jaroCutoff);

        if (similarity <= 0.7)
        {
            return similarity >= scoreCutoff ? similarity : 0.0;
        }

        int prefixLength = SequenceMetrics.CommonPrefixLength(source, target, 4);
        double boosted = similarity + (prefixLength * prefixWeight * (1.0 - similarity));
        double result = Math.Min(boosted, 1.0);
        return result >= scoreCutoff ? result : 0.0;
    }

    private int FindMatch(T value, int start, int stop, ReadOnlySpan<ulong> sourceFlags)
    {
        if (start >= stop)
        {
            return -1;
        }

        int firstBlock = start / 64;
        int lastBlock = (stop - 1) / 64;

        for (int block = firstBlock; block <= lastBlock; block++)
        {
            ulong rangeMask = ulong.MaxValue;

            if (block == firstBlock)
            {
                rangeMask &= ulong.MaxValue << (start & 63);
            }

            if (block == lastBlock && (stop & 63) != 0)
            {
                rangeMask &= (1UL << (stop & 63)) - 1UL;
            }

            ulong candidates = pattern.GetMask(value, block) & rangeMask & ~sourceFlags[block];

            if (candidates != 0UL)
            {
                return (block * 64) + BitOperations.TrailingZeroCount(candidates);
            }
        }

        return -1;
    }

    private int CountTranspositions(
        ReadOnlySpan<ulong> sourceFlags,
        ReadOnlySpan<T> target,
        ReadOnlySpan<int> targetMatches)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int transpositions = 0;
        int matchIndex = 0;

        for (int block = 0; block < sourceFlags.Length; block++)
        {
            ulong flags = sourceFlags[block];

            while (flags != 0UL)
            {
                int sourceIndex = (block * 64) + BitOperations.TrailingZeroCount(flags);

                if (!comparer.Equals(source[sourceIndex], target[targetMatches[matchIndex]]))
                {
                    transpositions++;
                }

                matchIndex++;
                flags &= flags - 1UL;
            }
        }

        return transpositions;
    }

    private static double GetJaroCutoff(double prefixWeight, double scoreCutoff)
    {
        if (scoreCutoff <= 0.7)
        {
            return scoreCutoff;
        }

        double maximumBoost = 4.0 * prefixWeight;
        return maximumBoost >= 1.0
            ? 0.0
            : Math.Max(0.7, (scoreCutoff - maximumBoost) / (1.0 - maximumBoost));
    }
}
