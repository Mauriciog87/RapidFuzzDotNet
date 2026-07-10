using System.Buffers;
using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public static class Levenshtein
{
    private const int StackLimit = 256;
    private static readonly byte[][] MblevenMatrix =
    [
        [0x03],
        [0x01],
        [0x0F, 0x09, 0x06],
        [0x0D, 0x07],
        [0x05],
        [0x3F, 0x27, 0x2D, 0x39, 0x36, 0x1E, 0x1B],
        [0x3D, 0x37, 0x1F, 0x25, 0x19, 0x16],
        [0x35, 0x1D, 0x17],
        [0x15]
    ];

    public static int Distance(string first, string second, int scoreCutoff = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff);
    }

    public static int Distance(string first, string second, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Distance(string first, string second, LevenshteinWeights weights, int scoreCutoff = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), weights, scoreCutoff);
    }

    public static int Distance(
        string first,
        string second,
        LevenshteinWeights weights,
        int scoreCutoff,
        int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), weights, scoreCutoff, scoreHint);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff = int.MaxValue)
    {
        return Distance(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff, int scoreHint)
    {
        return Distance(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Distance(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        int scoreCutoff = int.MaxValue)
    {
        return Distance(first, second, weights, scoreCutoff, int.MaxValue);
    }

    public static int Distance(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        int scoreCutoff,
        int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int effectiveCutoff = Math.Min(scoreCutoff, Maximum(first.Length, second.Length, weights));
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(first, second, weights, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return DistanceCore(first, second, weights, scoreCutoff);
    }

    public static int Distance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, weights, scoreCutoff, int.MaxValue);
    }

    public static int Distance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        int scoreCutoff,
        int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int effectiveCutoff = Math.Min(scoreCutoff, SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights));
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = SequenceMetrics.LevenshteinDistance(first, second, weights, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return SequenceMetrics.LevenshteinDistance(first, second, weights, scoreCutoff);
    }

    private static int DistanceCore(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        DistanceHelpers.TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length * weights.InsertCost, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length * weights.DeleteCost, scoreCutoff);
        }

        if (weights == LevenshteinWeights.Default)
        {
            int lengthDifference = Math.Abs(first.Length - second.Length);

            if (lengthDifference > scoreCutoff)
            {
                return scoreCutoff == int.MaxValue ? lengthDifference : scoreCutoff + 1;
            }

            int shortestLength = Math.Min(first.Length, second.Length);

            if (scoreCutoff < 4)
            {
                return MblevenDistance(first, second, scoreCutoff);
            }

            if (scoreCutoff < 64 && scoreCutoff * 8 < shortestLength)
            {
                return SmallBandHyyrroeDistance(first, second, scoreCutoff);
            }

            ReadOnlySpan<char> pattern = first.Length <= second.Length ? first : second;
            ReadOnlySpan<char> text = first.Length <= second.Length ? second : first;

            int bitParallelDistance = pattern.Length <= PatternMatchVector.MaximumPatternLength
                ? PatternMatchVector.LevenshteinDistance(pattern, text)
                : new BlockPatternMatchVector(pattern).LevenshteinDistance(text);

            return DistanceHelpers.ApplyDistanceCutoff(bitParallelDistance, scoreCutoff);
        }

        int length = second.Length + 1;
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = length <= StackLimit
            ? stackalloc int[length]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(length)).AsSpan(0, length);
        Span<int> current = length <= StackLimit
            ? stackalloc int[length]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(length)).AsSpan(0, length);

        try
        {
            for (int j = 0; j < previous.Length; j++)
            {
                previous[j] = j * weights.InsertCost;
            }

            for (int i = 1; i <= first.Length; i++)
            {
                current[0] = i * weights.DeleteCost;
                int rowMinimum = current[0];

                for (int j = 1; j <= second.Length; j++)
                {
                    int substitutionCost = first[i - 1] == second[j - 1] ? 0 : weights.ReplaceCost;
                    int deletion = previous[j] + weights.DeleteCost;
                    int insertion = current[j - 1] + weights.InsertCost;
                    int substitution = previous[j - 1] + substitutionCost;
                    int value = Math.Min(Math.Min(deletion, insertion), substitution);

                    current[j] = value;

                    if (value < rowMinimum)
                    {
                        rowMinimum = value;
                    }
                }

                if (rowMinimum > scoreCutoff)
                {
                    return scoreCutoff == int.MaxValue ? rowMinimum : scoreCutoff + 1;
                }

                Span<int> temp = previous;
                previous = current;
                current = temp;
            }

            return DistanceHelpers.ApplyDistanceCutoff(previous[second.Length], scoreCutoff);
        }
        finally
        {
            if (rentedPrevious is not null)
            {
                ArrayPool<int>.Shared.Return(rentedPrevious);
            }

            if (rentedCurrent is not null)
            {
                ArrayPool<int>.Shared.Return(rentedCurrent);
            }
        }
    }

    private static int BandedDefaultDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        int lengthDifference = Math.Abs(first.Length - second.Length);

        if (lengthDifference > scoreCutoff)
        {
            return scoreCutoff + 1;
        }

        int sentinel = scoreCutoff + 1;
        int length = second.Length + 1;
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = length <= StackLimit
            ? stackalloc int[length]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(length)).AsSpan(0, length);
        Span<int> current = length <= StackLimit
            ? stackalloc int[length]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(length)).AsSpan(0, length);

        try
        {
            previous.Fill(sentinel);
            current.Fill(sentinel);

            int initialLimit = Math.Min(second.Length, scoreCutoff);

            for (int j = 0; j <= initialLimit; j++)
            {
                previous[j] = j;
            }

            for (int i = 1; i <= first.Length; i++)
            {
                int columnStart = Math.Max(1, i - scoreCutoff);
                int columnEnd = Math.Min(second.Length, i + scoreCutoff);
                current[0] = i <= scoreCutoff ? i : sentinel;

                if (columnStart > 1)
                {
                    current[columnStart - 1] = sentinel;
                }

                int rowMinimum = sentinel;

                for (int j = columnStart; j <= columnEnd; j++)
                {
                    int substitutionCost = first[i - 1] == second[j - 1] ? 0 : 1;
                    int deletion = previous[j] + 1;
                    int insertion = current[j - 1] + 1;
                    int substitution = previous[j - 1] + substitutionCost;
                    int value = Math.Min(Math.Min(deletion, insertion), substitution);

                    if (value > sentinel)
                    {
                        value = sentinel;
                    }

                    current[j] = value;

                    if (value < rowMinimum)
                    {
                        rowMinimum = value;
                    }
                }

                if (columnEnd < second.Length)
                {
                    current[columnEnd + 1] = sentinel;
                }

                if (rowMinimum > scoreCutoff)
                {
                    return sentinel;
                }

                Span<int> temp = previous;
                previous = current;
                current = temp;
            }

            return previous[second.Length] <= scoreCutoff ? previous[second.Length] : sentinel;
        }
        finally
        {
            if (rentedPrevious is not null)
            {
                ArrayPool<int>.Shared.Return(rentedPrevious);
            }

            if (rentedCurrent is not null)
            {
                ArrayPool<int>.Shared.Return(rentedCurrent);
            }
        }
    }

    private static int SmallBandHyyrroeDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        int lengthDifference = Math.Abs(first.Length - second.Length);

        if (lengthDifference > scoreCutoff)
        {
            return scoreCutoff + 1;
        }

        ReadOnlySpan<char> pattern = first.Length <= second.Length ? first : second;
        ReadOnlySpan<char> text = first.Length <= second.Length ? second : first;

        if (pattern.Length <= PatternMatchVector.MaximumPatternLength)
        {
            PatternMatchVector vector = new(pattern);
            int distance = vector.LevenshteinDistance(pattern.Length, text);
            return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
        }

        return BandedDefaultDistance(first, second, scoreCutoff);
    }

    private static int MblevenDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        if (first.Length < second.Length)
        {
            return MblevenDistance(second, first, scoreCutoff);
        }

        int lengthDifference = first.Length - second.Length;

        if (scoreCutoff == 0)
        {
            return first.SequenceEqual(second) ? 0 : 1;
        }

        if (scoreCutoff == 1)
        {
            return scoreCutoff + (lengthDifference == 1 || first.Length != 1 ? 1 : 0);
        }

        int opsIndex = ((scoreCutoff + (scoreCutoff * scoreCutoff)) / 2) + lengthDifference - 1;

        if (opsIndex < 0 || opsIndex >= MblevenMatrix.Length)
        {
            return BandedDefaultDistance(first, second, scoreCutoff);
        }

        byte[] possibleOperations = MblevenMatrix[opsIndex];
        int distance = scoreCutoff + 1;

        for (int operationIndex = 0; operationIndex < possibleOperations.Length; operationIndex++)
        {
            byte operations = possibleOperations[operationIndex];

            if (operations == 0)
            {
                break;
            }

            int firstIndex = 0;
            int secondIndex = 0;
            int currentDistance = 0;

            while (firstIndex < first.Length && secondIndex < second.Length)
            {
                if (first[firstIndex] != second[secondIndex])
                {
                    currentDistance++;

                    if (operations == 0)
                    {
                        break;
                    }

                    if ((operations & 1) != 0)
                    {
                        firstIndex++;
                    }

                    if ((operations & 2) != 0)
                    {
                        secondIndex++;
                    }

                    operations >>= 2;
                }
                else
                {
                    firstIndex++;
                    secondIndex++;
                }
            }

            currentDistance += (first.Length - firstIndex) + (second.Length - secondIndex);
            distance = Math.Min(distance, currentDistance);
        }

        return distance <= scoreCutoff ? distance : scoreCutoff + 1;
    }

    public static int Similarity(string first, string second, int scoreCutoff = 0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff);
    }

    public static int Similarity(string first, string second, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Similarity(string first, string second, LevenshteinWeights weights, int scoreCutoff = 0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), weights, scoreCutoff);
    }

    public static int Similarity(
        string first,
        string second,
        LevenshteinWeights weights,
        int scoreCutoff,
        int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), weights, scoreCutoff, scoreHint);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff = 0)
    {
        return Similarity(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff, int scoreHint)
    {
        return Similarity(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = 0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Similarity(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        int scoreCutoff = 0)
    {
        return Similarity(first, second, weights, scoreCutoff, 0);
    }

    public static int Similarity(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        int scoreCutoff,
        int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Maximum(first.Length, second.Length, weights);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, weights, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0;
        }

        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static int Similarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        int scoreCutoff = 0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, weights, scoreCutoff, 0);
    }

    public static int Similarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        int scoreCutoff,
        int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, weights, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0;
        }

        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(
        string first,
        string second,
        LevenshteinWeights weights,
        double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), weights, scoreCutoff);
    }

    public static double NormalizedDistance(
        string first,
        string second,
        LevenshteinWeights weights,
        double scoreCutoff,
        double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), weights, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 1.0)
    {
        return NormalizedDistance(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        return NormalizedDistance(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedDistance(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        return NormalizedDistance(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        double scoreCutoff = 1.0)
    {
        return NormalizedDistance(first, second, weights, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        double scoreCutoff,
        double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Maximum(first.Length, second.Length, weights);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, weights, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedDistance(first, second, weights, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, weights, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(
        string first,
        string second,
        LevenshteinWeights weights,
        double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), weights, scoreCutoff);
    }

    public static double NormalizedSimilarity(
        string first,
        string second,
        LevenshteinWeights weights,
        double scoreCutoff,
        double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), weights, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        return NormalizedSimilarity(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        return NormalizedSimilarity(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedSimilarity(first, second, LevenshteinWeights.Default, scoreCutoff);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        return NormalizedSimilarity(first, second, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        double scoreCutoff = 0.0)
    {
        return NormalizedSimilarity(first, second, weights, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        LevenshteinWeights weights,
        double scoreCutoff,
        double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Maximum(first.Length, second.Length, weights);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, weights, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0.0;
        }

        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedSimilarity(first, second, weights, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, weights, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0.0;
        }

        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static EditOperations Editops(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Editops(first.AsSpan(), second.AsSpan());
    }

    public static EditOperations Editops(string first, string second, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Editops(first.AsSpan(), second.AsSpan(), scoreHint);
    }

    public static EditOperations Editops(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return Editops(first, second, int.MaxValue);
    }

    public static EditOperations Editops(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreHint)
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);

        DirectionMatrix matrix = BuildTraceMatrix(first, second);
        List<EditOp> operations = [];
        int sourcePosition = first.Length;
        int destinationPosition = second.Length;

        while (sourcePosition > 0 || destinationPosition > 0)
        {
            if (sourcePosition == 0)
            {
                destinationPosition--;
                operations.Add(new EditOp(EditOperation.Insert, sourcePosition, destinationPosition));
            }
            else if (destinationPosition == 0)
            {
                sourcePosition--;
                operations.Add(new EditOp(EditOperation.Delete, sourcePosition, destinationPosition));
            }
            else
            {
                byte direction = matrix.Get(sourcePosition, destinationPosition);

                if (direction == PackedDirectionMatrix.Equal)
                {
                    sourcePosition--;
                    destinationPosition--;
                }
                else if (direction == PackedDirectionMatrix.Replace)
                {
                    sourcePosition--;
                    destinationPosition--;
                    operations.Add(new EditOp(EditOperation.Replace, sourcePosition, destinationPosition));
                }
                else if (direction == PackedDirectionMatrix.Delete)
                {
                    sourcePosition--;
                    operations.Add(new EditOp(EditOperation.Delete, sourcePosition, destinationPosition));
                }
                else
                {
                    destinationPosition--;
                    operations.Add(new EditOp(EditOperation.Insert, sourcePosition, destinationPosition));
                }
            }
        }

        operations.Reverse();
        return new EditOperations(operations, first.Length, second.Length);
    }

    public static EditOperations Editops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        return Editops(first, second, int.MaxValue);
    }

    public static EditOperations Editops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return SequenceMetrics.LevenshteinEditops(first, second);
    }

    public static Opcodes Opcodes(string first, string second)
    {
        return Editops(first, second).ToOpcodes();
    }

    public static Opcodes Opcodes(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return Editops(first, second).ToOpcodes();
    }

    public static Opcodes Opcodes<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        return Editops(first, second).ToOpcodes();
    }

    private static DirectionMatrix BuildTraceMatrix(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        int columnCount = second.Length + 1;
        DirectionMatrix directions = TraceMatrixFactory.Create(first.Length + 1, columnCount);
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = columnCount <= StackLimit
            ? stackalloc int[columnCount]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        Span<int> current = columnCount <= StackLimit
            ? stackalloc int[columnCount]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);

        try
        {
            for (int column = 0; column < columnCount; column++)
            {
                previous[column] = column;
                directions.Set(0, column, PackedDirectionMatrix.Insert);
            }

            for (int row = 1; row <= first.Length; row++)
            {
                current[0] = row;
                directions.Set(row, 0, PackedDirectionMatrix.Delete);

                for (int column = 1; column <= second.Length; column++)
                {
                    int substitutionCost = first[row - 1] == second[column - 1] ? 0 : 1;
                    int deletion = previous[column] + 1;
                    int insertion = current[column - 1] + 1;
                    int substitution = previous[column - 1] + substitutionCost;
                    int value = Math.Min(Math.Min(deletion, insertion), substitution);

                    current[column] = value;

                    if (substitutionCost == 0 && substitution <= deletion && substitution <= insertion)
                    {
                        directions.Set(row, column, PackedDirectionMatrix.Equal);
                    }
                    else if (substitution <= deletion && substitution <= insertion)
                    {
                        directions.Set(row, column, PackedDirectionMatrix.Replace);
                    }
                    else if (deletion <= insertion)
                    {
                        directions.Set(row, column, PackedDirectionMatrix.Delete);
                    }
                    else
                    {
                        directions.Set(row, column, PackedDirectionMatrix.Insert);
                    }
                }

                Span<int> temp = previous;
                previous = current;
                current = temp;
            }

            return directions;
        }
        finally
        {
            if (rentedPrevious is not null)
            {
                ArrayPool<int>.Shared.Return(rentedPrevious);
            }

            if (rentedCurrent is not null)
            {
                ArrayPool<int>.Shared.Return(rentedCurrent);
            }
        }
    }

    private static int Maximum(int firstLength, int secondLength, LevenshteinWeights weights)
    {
        int sharedLength = Math.Min(firstLength, secondLength);
        int replacementMaximum = Math.Min(weights.ReplaceCost, weights.InsertCost + weights.DeleteCost);
        int extraCost = firstLength > secondLength
            ? (firstLength - secondLength) * weights.DeleteCost
            : (secondLength - firstLength) * weights.InsertCost;

        return (sharedLength * replacementMaximum) + extraCost;
    }

}
