namespace RapidFuzz.Distance;

public static partial class Prefix
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

        int maximum = Math.Max(first.Length, second.Length);
        int distance = maximum - Similarity(first, second);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
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

        int maximum = Math.Max(first.Length, second.Length);
        int distance = maximum - Similarity(first, second);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
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

        int length = Math.Min(first.Length, second.Length);
        int similarity = 0;

        while (similarity < length && first[similarity] == second[similarity])
        {
            similarity++;
        }

        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = 0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, scoreCutoff, 0);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return SequenceMetrics.PrefixSimilarity(first, second, scoreCutoff);
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

        if (maximum == 0)
        {
            return 1.0;
        }

        double similarity = (double)Similarity(first, second) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
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

        if (maximum == 0)
        {
            return 1.0;
        }

        double similarity = (double)Similarity(first, second) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }
}
