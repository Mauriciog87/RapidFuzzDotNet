using System.Numerics;
using RapidFuzz.Distance;

namespace RapidFuzz.Internal;

internal sealed class GenericBatchPatternScorer<T>
    where T : notnull, IEquatable<T>
{
    private readonly List<BatchPattern> patterns;

    public GenericBatchPatternScorer(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        patterns = new List<BatchPattern>(capacity);
    }

    public int Count => patterns.Count;

    public int GetLength(int index) => patterns[index].Pattern.Length;

    public void Insert(ReadOnlySpan<T> source) => patterns.Add(new BatchPattern(new GenericPatternMatchVector<T>(source)));

    public void LevenshteinDistances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        ValidateDestination(destination.Length);
        Score(target, destination, scoreCutoff, BatchMetric.Levenshtein);
    }

    public void LcsSimilarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        ValidateDestination(destination.Length);
        Score(target, destination, scoreCutoff, BatchMetric.Lcs);
    }

    public void OsaDistances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        ValidateDestination(destination.Length);
        Score(target, destination, scoreCutoff, BatchMetric.Osa);
    }

    private void Score(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, BatchMetric metric)
    {
        int vectorWidth = Vector<ulong>.Count;
        Span<int> laneIndexes = stackalloc int[vectorWidth];
        int laneCount = 0;

        for (int index = 0; index < Count; index++)
        {
            int patternLength = patterns[index].Pattern.Length;

            if (Vector.IsHardwareAccelerated && patternLength is > 0 and <= 64)
            {
                laneIndexes[laneCount] = index;
                laneCount++;

                if (laneCount == vectorWidth)
                {
                    ScoreVector(target, destination, scoreCutoff, metric, laneIndexes);
                    laneCount = 0;
                }

                continue;
            }

            destination[index] = ScoreScalar(patterns[index].Pattern, target, scoreCutoff, metric);
        }

        for (int lane = 0; lane < laneCount; lane++)
        {
            int index = laneIndexes[lane];
            destination[index] = ScoreScalar(patterns[index].Pattern, target, scoreCutoff, metric);
        }
    }

    private void ScoreVector(
        ReadOnlySpan<T> target,
        Span<int> destination,
        int scoreCutoff,
        BatchMetric metric,
        ReadOnlySpan<int> laneIndexes)
    {
        if (metric == BatchMetric.Lcs)
        {
            ScoreLcsVector(target, destination, scoreCutoff, laneIndexes);
            return;
        }

        int vectorWidth = Vector<ulong>.Count;
        Span<ulong> equalValues = stackalloc ulong[vectorWidth];
        Span<ulong> highBitValues = stackalloc ulong[vectorWidth];
        Span<ulong> positiveChangeValues = stackalloc ulong[vectorWidth];
        Span<ulong> negativeChangeValues = stackalloc ulong[vectorWidth];
        Span<int> scores = stackalloc int[vectorWidth];

        for (int lane = 0; lane < vectorWidth; lane++)
        {
            int patternLength = patterns[laneIndexes[lane]].Pattern.Length;
            highBitValues[lane] = 1UL << (patternLength - 1);
            scores[lane] = patternLength;
        }

        Vector<ulong> highBits = new(highBitValues);
        Vector<ulong> positive = new(ulong.MaxValue);
        Vector<ulong> negative = Vector<ulong>.Zero;
        Vector<ulong> diagonal = Vector<ulong>.Zero;
        Vector<ulong> previousEqual = Vector<ulong>.Zero;

        for (int targetIndex = 0; targetIndex < target.Length; targetIndex++)
        {
            FillEqualValues(target[targetIndex], laneIndexes, equalValues);
            Vector<ulong> equal = new(equalValues);
            Vector<ulong> currentDiagonal;

            if (metric == BatchMetric.Osa)
            {
                Vector<ulong> transposition = ((~diagonal & equal) << 1) & previousEqual;
                currentDiagonal = ((((equal & positive) + positive) ^ positive) | equal | negative | transposition);
            }
            else
            {
                Vector<ulong> vertical = equal | negative;
                currentDiagonal = (((equal & positive) + positive) ^ positive) | equal;
                diagonal = vertical;
            }

            Vector<ulong> positiveHorizontal = negative | ~(currentDiagonal | positive);
            Vector<ulong> negativeHorizontal = currentDiagonal & positive;
            Vector<ulong> positiveChanges = positiveHorizontal & highBits;
            Vector<ulong> negativeChanges = negativeHorizontal & highBits;
            positiveChanges.CopyTo(positiveChangeValues);
            negativeChanges.CopyTo(negativeChangeValues);

            for (int lane = 0; lane < vectorWidth; lane++)
            {
                if (positiveChangeValues[lane] != 0UL)
                {
                    scores[lane]++;
                }
                else if (negativeChangeValues[lane] != 0UL)
                {
                    scores[lane]--;
                }
            }

            positiveHorizontal = (positiveHorizontal << 1) | Vector<ulong>.One;
            negativeHorizontal <<= 1;

            if (metric == BatchMetric.Osa)
            {
                positive = negativeHorizontal | ~(currentDiagonal | positiveHorizontal);
                negative = positiveHorizontal & currentDiagonal;
                diagonal = currentDiagonal;
                previousEqual = equal;
            }
            else
            {
                positive = negativeHorizontal | ~(diagonal | positiveHorizontal);
                negative = positiveHorizontal & diagonal;
            }
        }

        for (int lane = 0; lane < vectorWidth; lane++)
        {
            destination[laneIndexes[lane]] = DistanceHelpers.ApplyDistanceCutoff(scores[lane], scoreCutoff);
        }
    }

    private void ScoreLcsVector(
        ReadOnlySpan<T> target,
        Span<int> destination,
        int scoreCutoff,
        ReadOnlySpan<int> laneIndexes)
    {
        int vectorWidth = Vector<ulong>.Count;
        Span<ulong> equalValues = stackalloc ulong[vectorWidth];
        Span<ulong> stateValues = stackalloc ulong[vectorWidth];
        Vector<ulong> state = Vector<ulong>.Zero;

        for (int targetIndex = 0; targetIndex < target.Length; targetIndex++)
        {
            FillEqualValues(target[targetIndex], laneIndexes, equalValues);
            Vector<ulong> matches = new Vector<ulong>(equalValues) | state;
            Vector<ulong> shifted = (state << 1) | Vector<ulong>.One;
            state = matches & ~(matches - shifted);
        }

        state.CopyTo(stateValues);

        for (int lane = 0; lane < vectorWidth; lane++)
        {
            int similarity = BitOperations.PopCount(stateValues[lane]);
            destination[laneIndexes[lane]] = similarity >= scoreCutoff ? similarity : 0;
        }
    }

    private void FillEqualValues(T value, ReadOnlySpan<int> laneIndexes, Span<ulong> equalValues)
    {
        for (int lane = 0; lane < laneIndexes.Length; lane++)
        {
            equalValues[lane] = patterns[laneIndexes[lane]].Pattern.GetMask(value, 0);
        }
    }

    private static int ScoreScalar(
        GenericPatternMatchVector<T> pattern,
        ReadOnlySpan<T> target,
        int scoreCutoff,
        BatchMetric metric)
    {
        return metric switch
        {
            BatchMetric.Levenshtein => DistanceHelpers.ApplyDistanceCutoff(pattern.LevenshteinDistance(target), scoreCutoff),
            BatchMetric.Lcs => pattern.LcsSimilarity(target, scoreCutoff),
            BatchMetric.Osa => pattern.OsaDistance(target, scoreCutoff),
            _ => throw new InvalidOperationException("The batch metric is invalid.")
        };
    }

    private void ValidateDestination(int destination)
    {
        if (destination < Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }
    }

    private readonly record struct BatchPattern(GenericPatternMatchVector<T> Pattern);

    private enum BatchMetric
    {
        Levenshtein,
        Lcs,
        Osa
    }
}
