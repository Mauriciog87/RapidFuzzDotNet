using System.Buffers;

namespace RapidFuzz.Distance;

public static partial class DamerauLevenshtein
{
    private const int StackLimit = 256;

    public static int Distance(string first, string second, int scoreCutoff = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static int Distance(string first, string second, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff = int.MaxValue)
    {
        return Distance(first, second, scoreCutoff, int.MaxValue);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        return DistanceCore(first, second, scoreCutoff);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, scoreCutoff, int.MaxValue);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        return SequenceMetrics.DamerauLevenshteinDistance(first, second, scoreCutoff);
    }

    private static int DistanceCore(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        int minimumDistance = Math.Abs(first.Length - second.Length);

        if (minimumDistance > scoreCutoff)
        {
            return DistanceHelpers.ApplyDistanceCutoff(minimumDistance, scoreCutoff);
        }

        DistanceHelpers.TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty || second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(Math.Max(first.Length, second.Length), scoreCutoff);
        }

        if (second.Length > first.Length)
        {
            ReadOnlySpan<char> temporary = first;
            first = second;
            second = temporary;
        }

        int rowSize = second.Length + 2;
        int storageSize = checked(rowSize * 3);
        int[]? rented = null;
        Span<int> storage = rowSize <= StackLimit
            ? stackalloc int[storageSize]
            : (rented = ArrayPool<int>.Shared.Rent(storageSize)).AsSpan(0, storageSize);

        try
        {
            Span<int> fr = storage[..rowSize];
            Span<int> previous = storage.Slice(rowSize, rowSize);
            Span<int> current = storage.Slice(rowSize * 2, rowSize);
            int maximumValue = Math.Max(first.Length, second.Length) + 1;

            fr.Fill(maximumValue);
            previous.Fill(maximumValue);
            current[0] = maximumValue;

            for (int index = 1; index < rowSize; index++)
            {
                current[index] = index - 1;
            }

            Span<int> asciiLastRows = stackalloc int[128];
            asciiLastRows.Fill(-1);
            Dictionary<char, int>? unicodeLastRows = null;

            for (int row = 1; row <= first.Length; row++)
            {
                Span<int> temporary = current;
                current = previous;
                previous = temporary;
                int lastMatchColumn = -1;
                int lastPreviousValue = current[1];
                current[1] = row;
                int transpositionBase = maximumValue;
                char firstValue = first[row - 1];

                for (int column = 1; column <= second.Length; column++)
                {
                    char secondValue = second[column - 1];
                    int diagonal = previous[column] + (firstValue == secondValue ? 0 : 1);
                    int insertion = current[column] + 1;
                    int deletion = previous[column + 1] + 1;
                    int value = Math.Min(diagonal, Math.Min(insertion, deletion));

                    if (firstValue == secondValue)
                    {
                        lastMatchColumn = column;
                        fr[column + 1] = previous[column - 1];
                        transpositionBase = lastPreviousValue;
                    }
                    else
                    {
                        int lastMatchingRow = GetLastRow(secondValue, asciiLastRows, unicodeLastRows);

                        if (column - lastMatchColumn == 1)
                        {
                            value = Math.Min(value, fr[column + 1] + row - lastMatchingRow);
                        }
                        else if (row - lastMatchingRow == 1)
                        {
                            value = Math.Min(value, transpositionBase + column - lastMatchColumn);
                        }
                    }

                    lastPreviousValue = current[column + 1];
                    current[column + 1] = value;
                }

                SetLastRow(firstValue, row, asciiLastRows, ref unicodeLastRows);
            }

            return DistanceHelpers.ApplyDistanceCutoff(current[second.Length + 1], scoreCutoff);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    private static int GetLastRow(char value, ReadOnlySpan<int> asciiLastRows, Dictionary<char, int>? unicodeLastRows)
    {
        if (value < asciiLastRows.Length)
        {
            return asciiLastRows[value];
        }

        return unicodeLastRows is not null && unicodeLastRows.TryGetValue(value, out int row) ? row : -1;
    }

    private static void SetLastRow(char value, int row, Span<int> asciiLastRows, ref Dictionary<char, int>? unicodeLastRows)
    {
        if (value < asciiLastRows.Length)
        {
            asciiLastRows[value] = row;
            return;
        }

        unicodeLastRows ??= [];
        unicodeLastRows[value] = row;
    }

    public static int Similarity(string first, string second, int scoreCutoff = 0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static int Similarity(string first, string second, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff = 0)
    {
        return Similarity(first, second, scoreCutoff, 0);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = 0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, scoreCutoff, 0);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 1.0)
    {
        return NormalizedDistance(first, second, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedDistance(first, second, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        return NormalizedSimilarity(first, second, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0.0;
        }

        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedSimilarity(first, second, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0.0;
        }

        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }
}
