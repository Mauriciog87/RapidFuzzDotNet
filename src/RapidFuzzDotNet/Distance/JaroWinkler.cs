using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public static partial class JaroWinkler
{
    public static double Distance(string first, string second, double prefixWeight = 0.1, double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), prefixWeight, scoreCutoff);
    }

    public static double Distance(string first, string second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), prefixWeight, scoreCutoff, scoreHint);
    }

    public static double Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight = 0.1, double scoreCutoff = 1.0)
    {
        return Distance(first, second, prefixWeight, scoreCutoff, 1.0);
    }

    public static double Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        ValidatePrefixWeight(prefixWeight);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double distance = 1.0 - SimilarityCore(first, second, prefixWeight, 1.0 - scoreCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public static double Distance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight = 0.1,
        double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, prefixWeight, scoreCutoff, 1.0);
    }

    public static double Distance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        ValidatePrefixWeight(prefixWeight);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double distance = 1.0 - SimilarityCore(first, second, prefixWeight, 1.0 - scoreCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public static double Similarity(string first, string second, double prefixWeight = 0.1, double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), prefixWeight, scoreCutoff);
    }

    public static double Similarity(string first, string second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), prefixWeight, scoreCutoff, scoreHint);
    }

    public static double Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight = 0.1, double scoreCutoff = 0.0)
    {
        return Similarity(first, second, prefixWeight, scoreCutoff, 0.0);
    }

    public static double Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        ValidatePrefixWeight(prefixWeight);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarity = SimilarityCore(first, second, prefixWeight, scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double Similarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight = 0.1,
        double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, prefixWeight, scoreCutoff, 0.0);
    }

    public static double Similarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        ValidatePrefixWeight(prefixWeight);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarity = SimilarityCore(first, second, prefixWeight, scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double NormalizedDistance(string first, string second, double prefixWeight = 0.1, double scoreCutoff = 1.0)
    {
        return Distance(first, second, prefixWeight, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        return Distance(first, second, prefixWeight, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight = 0.1, double scoreCutoff = 1.0)
    {
        return Distance(first, second, prefixWeight, scoreCutoff);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        return Distance(first, second, prefixWeight, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight = 0.1,
        double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, prefixWeight, scoreCutoff);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, prefixWeight, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(string first, string second, double prefixWeight = 0.1, double scoreCutoff = 0.0)
    {
        return Similarity(first, second, prefixWeight, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        return Similarity(first, second, prefixWeight, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight = 0.1, double scoreCutoff = 0.0)
    {
        return Similarity(first, second, prefixWeight, scoreCutoff);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight, double scoreCutoff, double scoreHint)
    {
        return Similarity(first, second, prefixWeight, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight = 0.1,
        double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, prefixWeight, scoreCutoff);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, prefixWeight, scoreCutoff, scoreHint);
    }

    internal static double Similarity(JaroPattern pattern, ReadOnlySpan<char> second, double prefixWeight, double scoreCutoff)
    {
        return SimilarityCore(pattern.Source.AsSpan(), second, prefixWeight, scoreCutoff, pattern);
    }

    private static double Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double prefixWeight)
    {
        return SimilarityCore(first, second, prefixWeight, 0.0);
    }

    private static double SimilarityCore(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        double prefixWeight,
        double scoreCutoff,
        JaroPattern? pattern = null)
    {
        double jaroCutoff = JaroScoreCutoff(prefixWeight, scoreCutoff);
        double similarity = pattern is null
            ? Jaro.SimilarityForCharacters(first, second, jaroCutoff)
            : Jaro.Similarity(pattern, second, jaroCutoff);

        if (similarity <= 0.7)
        {
            return similarity >= scoreCutoff ? similarity : 0.0;
        }

        int prefixLength = CommonPrefixLength(first, second);
        double boosted = similarity + (prefixLength * prefixWeight * (1.0 - similarity));
        double result = Math.Min(boosted, 1.0);
        return result >= scoreCutoff ? result : 0.0;
    }

    private static double SimilarityCore<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight,
        double scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        double jaroCutoff = JaroScoreCutoff(prefixWeight, scoreCutoff);
        double similarity = Jaro.Similarity(first, second, jaroCutoff);

        if (similarity <= 0.7)
        {
            return similarity >= scoreCutoff ? similarity : 0.0;
        }

        int prefixLength = SequenceMetrics.CommonPrefixLength(first, second, 4);
        double boosted = similarity + (prefixLength * prefixWeight * (1.0 - similarity));
        double result = Math.Min(boosted, 1.0);
        return result >= scoreCutoff ? result : 0.0;
    }

    private static double JaroScoreCutoff(double prefixWeight, double scoreCutoff)
    {
        if (scoreCutoff <= 0.7)
        {
            return scoreCutoff;
        }

        double maximumBoost = 4.0 * prefixWeight;

        if (maximumBoost >= 1.0)
        {
            return 0.0;
        }

        return Math.Max(0.7, (scoreCutoff - maximumBoost) / (1.0 - maximumBoost));
    }

    private static int CommonPrefixLength(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return SimdSupport.CommonPrefixLength(first, second, 4);
    }

    internal static void ValidatePrefixWeight(double prefixWeight)
    {
        if (double.IsNaN(prefixWeight) || prefixWeight < 0.0 || prefixWeight > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(prefixWeight), "The prefix weight must be between 0 and 0.25.");
        }
    }
}
