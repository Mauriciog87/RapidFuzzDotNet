using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public static partial class Osa
{
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

        if (scoreHint < scoreCutoff)
        {
            int hintedDistance = DistanceCore(first, second, scoreHint);

            if (hintedDistance <= scoreHint)
            {
                return hintedDistance;
            }
        }

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

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        SequenceMetrics.TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length, scoreCutoff);
        }

        ReadOnlySpan<T> pattern = first.Length <= second.Length ? first : second;
        ReadOnlySpan<T> text = first.Length <= second.Length ? second : first;
        GenericPatternMatchVector<T> vector = new(pattern);
        return vector.OsaDistance(text, scoreCutoff);
    }

    private static int DistanceCore(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        DistanceHelpers.TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length, scoreCutoff);
        }

        if (second.Length > first.Length)
        {
            ReadOnlySpan<char> temporary = first;
            first = second;
            second = temporary;
        }

        int lengthDifference = first.Length - second.Length;

        if (lengthDifference > scoreCutoff)
        {
            return scoreCutoff + 1;
        }

        if (second.Length < PatternMatchVector.MaximumPatternLength)
        {
            PatternMatchVector vector = new(second);
            return HyyrroeDistance(vector, second.Length, first, scoreCutoff);
        }

        BlockPatternMatchVector blockVector = new(second);
        return HyyrroeBlockDistance(blockVector, second.Length, first, scoreCutoff);
    }

    private static int HyyrroeDistance(
        PatternMatchVector vector,
        int patternLength,
        ReadOnlySpan<char> text,
        int scoreCutoff)
    {
        ulong positive = ulong.MaxValue;
        ulong negative = 0;
        ulong diagonal = 0;
        ulong previousPatternMask = 0;
        int distance = patternLength;
        ulong highBit = 1UL << (patternLength - 1);

        for (int i = 0; i < text.Length; i++)
        {
            ulong patternMask = vector.GetMask(text[i]);
            ulong transposition = (((~diagonal) & patternMask) << 1) & previousPatternMask;
            diagonal = (((patternMask & positive) + positive) ^ positive) | patternMask | negative | transposition;
            ulong positiveHorizontal = negative | ~(diagonal | positive);
            ulong negativeHorizontal = diagonal & positive;

            if ((positiveHorizontal & highBit) != 0)
            {
                distance++;
            }
            else if ((negativeHorizontal & highBit) != 0)
            {
                distance--;
            }

            positiveHorizontal = (positiveHorizontal << 1) | 1UL;
            negativeHorizontal <<= 1;
            positive = negativeHorizontal | ~(diagonal | positiveHorizontal);
            negative = positiveHorizontal & diagonal;
            previousPatternMask = patternMask;
        }

        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    internal static int Distance(
        PatternMatchVector vector,
        int patternLength,
        ReadOnlySpan<char> target,
        int scoreCutoff)
    {
        return HyyrroeDistance(vector, patternLength, target, scoreCutoff);
    }

    private static int HyyrroeBlockDistance(
        BlockPatternMatchVector vector,
        int patternLength,
        ReadOnlySpan<char> text,
        int scoreCutoff)
    {
        int wordCount = vector.BlockCount;
        ulong lastBit = 1UL << ((patternLength - 1) & 63);
        int distance = patternLength;
        OsaBlockRow[] previousRows = new OsaBlockRow[wordCount + 1];
        OsaBlockRow[] currentRows = new OsaBlockRow[wordCount + 1];

        InitializeRows(previousRows);
        InitializeRows(currentRows);

        for (int row = 0; row < text.Length; row++)
        {
            ulong positiveCarry = 1;
            ulong negativeCarry = 0;

            for (int word = 0; word < wordCount; word++)
            {
                ulong negative = previousRows[word + 1].Negative;
                ulong positive = previousRows[word + 1].Positive;
                ulong diagonal = previousRows[word + 1].Diagonal;
                ulong previousDiagonal = previousRows[word].Diagonal;
                ulong previousPatternMask = previousRows[word + 1].PatternMask;
                ulong previousWordPatternMask = currentRows[word].PatternMask;
                ulong patternMask = vector.GetMask(text[row], word);
                ulong transposition = ((((~diagonal) & patternMask) << 1)
                    | (((~previousDiagonal) & previousWordPatternMask) >> 63)) & previousPatternMask;
                ulong diagonalInput = patternMask | negativeCarry;

                diagonal = (((diagonalInput & positive) + positive) ^ positive)
                    | diagonalInput
                    | negative
                    | transposition;

                ulong positiveHorizontal = negative | ~(diagonal | positive);
                ulong negativeHorizontal = diagonal & positive;

                if (word == wordCount - 1)
                {
                    if ((positiveHorizontal & lastBit) != 0)
                    {
                        distance++;
                    }
                    else if ((negativeHorizontal & lastBit) != 0)
                    {
                        distance--;
                    }
                }

                ulong previousPositiveCarry = positiveCarry;
                positiveCarry = positiveHorizontal >> 63;
                positiveHorizontal = (positiveHorizontal << 1) | previousPositiveCarry;

                ulong previousNegativeCarry = negativeCarry;
                negativeCarry = negativeHorizontal >> 63;
                negativeHorizontal = (negativeHorizontal << 1) | previousNegativeCarry;

                currentRows[word + 1].Positive = negativeHorizontal | ~(diagonal | positiveHorizontal);
                currentRows[word + 1].Negative = positiveHorizontal & diagonal;
                currentRows[word + 1].Diagonal = diagonal;
                currentRows[word + 1].PatternMask = patternMask;
            }

            OsaBlockRow[] temporary = previousRows;
            previousRows = currentRows;
            currentRows = temporary;
        }

        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    internal static int Distance(
        BlockPatternMatchVector vector,
        int patternLength,
        ReadOnlySpan<char> target,
        int scoreCutoff)
    {
        return HyyrroeBlockDistance(vector, patternLength, target, scoreCutoff);
    }

    private static void InitializeRows(OsaBlockRow[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].Positive = ulong.MaxValue;
        }
    }

    private struct OsaBlockRow
    {
        public ulong Positive;
        public ulong Negative;
        public ulong Diagonal;
        public ulong PatternMask;
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
