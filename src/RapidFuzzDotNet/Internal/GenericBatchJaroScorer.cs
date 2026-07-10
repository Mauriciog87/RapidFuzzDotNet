using System.Numerics;
using RapidFuzz.Distance;

namespace RapidFuzz.Internal;

internal sealed class GenericBatchJaroScorer<T>
    where T : notnull, IEquatable<T>
{
    private const int MaximumVectorizedLength = 64;
    private readonly List<GenericJaroPattern<T>> patterns;
    private readonly List<Dictionary<T, Vector<ulong>>> vectorMasks;
    private readonly double prefixWeight;
    private readonly bool winkler;

    public GenericBatchJaroScorer(int capacity, double prefixWeight, bool winkler)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        patterns = new List<GenericJaroPattern<T>>(capacity);
        vectorMasks = new List<Dictionary<T, Vector<ulong>>>(capacity / Vector<ulong>.Count);
        this.prefixWeight = prefixWeight;
        this.winkler = winkler;
    }

    public int Count => patterns.Count;

    public void Insert(ReadOnlySpan<T> source)
    {
        patterns.Add(new GenericJaroPattern<T>(source));

        if (patterns.Count % Vector<ulong>.Count == 0)
        {
            vectorMasks.Add(CreateVectorMasks(patterns.Count - Vector<ulong>.Count));
        }
    }

    public double[] Similarities(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(destination.Length);
        Score(target, destination, scoreCutoff);
    }

    public double[] Distances(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        Distances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(destination.Length);
        Score(target, destination, 0.0);

        for (int index = 0; index < Count; index++)
        {
            double distance = 1.0 - destination[index];
            destination[index] = distance <= scoreCutoff ? distance : 1.0;
        }
    }

    private void Score(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff)
    {
        int vectorWidth = Vector<ulong>.Count;
        int index = 0;

        while (index < Count)
        {
            if (Vector.IsHardwareAccelerated
                && !target.IsEmpty
                && index % vectorWidth == 0
                && index + vectorWidth <= Count
                && CanVectorize(index, vectorWidth))
            {
                ScoreVector(target, destination, scoreCutoff, index, vectorMasks[index / vectorWidth]);
                index += vectorWidth;
                continue;
            }

            destination[index] = ScoreScalar(patterns[index], target, scoreCutoff);
            index++;
        }
    }

    private void ScoreVector(
        ReadOnlySpan<T> target,
        Span<double> destination,
        double scoreCutoff,
        int startIndex,
        Dictionary<T, Vector<ulong>> batchMasks)
    {
        int vectorWidth = Vector<ulong>.Count;
        Span<ulong> rangeValues = stackalloc ulong[vectorWidth];
        Span<ulong> candidateValues = stackalloc ulong[vectorWidth];
        Span<ulong> selectedValues = stackalloc ulong[vectorWidth];
        Span<ulong> flagValues = stackalloc ulong[vectorWidth];
        Span<int> patternLengths = stackalloc int[vectorWidth];
        Span<int> matchDistances = stackalloc int[vectorWidth];
        Span<int> matchCounts = stackalloc int[vectorWidth];
        Span<int> targetMatches = stackalloc int[vectorWidth * MaximumVectorizedLength];
        Vector<ulong> sourceFlags = Vector<ulong>.Zero;
        bool equalLengths = true;

        for (int lane = 0; lane < vectorWidth; lane++)
        {
            int patternLength = patterns[startIndex + lane].Length;
            patternLengths[lane] = patternLength;
            matchDistances[lane] = Math.Max(Math.Max(patternLength, target.Length) / 2 - 1, 0);
            equalLengths &= lane == 0 || patternLength == patternLengths[0];
        }

        for (int targetIndex = 0; targetIndex < target.Length; targetIndex++)
        {
            Vector<ulong> range;

            if (equalLengths)
            {
                int start = Math.Max(0, targetIndex - matchDistances[0]);
                int stop = Math.Min(patternLengths[0], targetIndex + matchDistances[0] + 1);
                range = new Vector<ulong>(CreateRangeMask(start, stop));
            }
            else
            {
                for (int lane = 0; lane < vectorWidth; lane++)
                {
                    int start = Math.Max(0, targetIndex - matchDistances[lane]);
                    int stop = Math.Min(patternLengths[lane], targetIndex + matchDistances[lane] + 1);
                    rangeValues[lane] = CreateRangeMask(start, stop);
                }

                range = new Vector<ulong>(rangeValues);
            }

            Vector<ulong> equal = batchMasks.TryGetValue(target[targetIndex], out Vector<ulong> masks)
                ? masks
                : Vector<ulong>.Zero;
            Vector<ulong> candidates = equal & range & ~sourceFlags;
            candidates.CopyTo(candidateValues);

            for (int lane = 0; lane < vectorWidth; lane++)
            {
                ulong candidate = candidateValues[lane];

                if (candidate == 0UL)
                {
                    selectedValues[lane] = 0UL;
                    continue;
                }

                selectedValues[lane] = 1UL << BitOperations.TrailingZeroCount(candidate);
                int matchIndex = matchCounts[lane];
                targetMatches[(lane * MaximumVectorizedLength) + matchIndex] = targetIndex;
                matchCounts[lane] = matchIndex + 1;
            }

            Vector<ulong> selected = new(selectedValues);
            sourceFlags |= selected;
        }

        sourceFlags.CopyTo(flagValues);

        for (int lane = 0; lane < vectorWidth; lane++)
        {
            int index = startIndex + lane;
            int matches = matchCounts[lane];

            if (matches == 0)
            {
                destination[index] = 0.0;
                continue;
            }

            GenericJaroPattern<T> pattern = patterns[index];
            int transpositions = CountTranspositions(
                pattern.Source,
                target,
                flagValues[lane],
                targetMatches.Slice(lane * MaximumVectorizedLength, matches));
            double matchCount = matches;
            int halfTranspositions = transpositions / 2;
            double similarity = ((matchCount / pattern.Length)
                + (matchCount / target.Length)
                + ((matchCount - halfTranspositions) / matchCount)) / 3.0;

            if (winkler && similarity > 0.7)
            {
                int prefixLength = SequenceMetrics.CommonPrefixLength(pattern.Source, target, 4);
                similarity = Math.Min(similarity + prefixLength * prefixWeight * (1.0 - similarity), 1.0);
            }

            destination[index] = similarity >= scoreCutoff ? similarity : 0.0;
        }
    }

    private bool CanVectorize(int startIndex, int count)
    {
        for (int lane = 0; lane < count; lane++)
        {
            int length = patterns[startIndex + lane].Length;

            if (length is <= 0 or > MaximumVectorizedLength)
            {
                return false;
            }
        }

        return true;
    }

    private Dictionary<T, Vector<ulong>> CreateVectorMasks(int startIndex)
    {
        int vectorWidth = Vector<ulong>.Count;
        Dictionary<T, ulong[]> laneMasks = [];

        for (int lane = 0; lane < vectorWidth; lane++)
        {
            ReadOnlySpan<T> source = patterns[startIndex + lane].Source;

            for (int sourceIndex = 0; sourceIndex < source.Length && sourceIndex < MaximumVectorizedLength; sourceIndex++)
            {
                if (!laneMasks.TryGetValue(source[sourceIndex], out ulong[]? masks))
                {
                    masks = new ulong[vectorWidth];
                    laneMasks.Add(source[sourceIndex], masks);
                }

                masks[lane] |= 1UL << sourceIndex;
            }
        }

        Dictionary<T, Vector<ulong>> result = new(laneMasks.Count);

        foreach (KeyValuePair<T, ulong[]> entry in laneMasks)
        {
            result.Add(entry.Key, new Vector<ulong>(entry.Value));
        }

        return result;
    }

    private double ScoreScalar(GenericJaroPattern<T> pattern, ReadOnlySpan<T> target, double scoreCutoff)
    {
        return winkler
            ? pattern.WinklerSimilarity(target, prefixWeight, scoreCutoff)
            : pattern.Similarity(target, scoreCutoff);
    }

    private static int CountTranspositions(
        ReadOnlySpan<T> source,
        ReadOnlySpan<T> target,
        ulong sourceFlags,
        ReadOnlySpan<int> targetMatches)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int transpositions = 0;
        int matchIndex = 0;

        while (sourceFlags != 0UL)
        {
            int sourceIndex = BitOperations.TrailingZeroCount(sourceFlags);

            if (!comparer.Equals(source[sourceIndex], target[targetMatches[matchIndex]]))
            {
                transpositions++;
            }

            matchIndex++;
            sourceFlags &= sourceFlags - 1UL;
        }

        return transpositions;
    }

    private static ulong CreateRangeMask(int start, int stop)
    {
        if (start >= stop)
        {
            return 0UL;
        }

        ulong lower = ulong.MaxValue << start;
        ulong upper = stop == MaximumVectorizedLength ? ulong.MaxValue : (1UL << stop) - 1UL;
        return lower & upper;
    }

    private void ValidateDestination(int destination)
    {
        if (destination < Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }
    }
}
