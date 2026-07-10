namespace RapidFuzz.Distance;

public static class Indel
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

        int effectiveCutoff = Math.Min(scoreCutoff, first.Length + second.Length);
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(first, second, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
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

        int effectiveCutoff = Math.Min(scoreCutoff, first.Length + second.Length);
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(first, second, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return DistanceCore(first, second, scoreCutoff);
    }

    private static int DistanceCore<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length, scoreCutoff);
        }

        int maximum = first.Length + second.Length;
        int lcsCutoff = scoreCutoff >= maximum ? 0 : ((maximum - scoreCutoff) + 1) / 2;
        int lcsLength = SequenceMetrics.LcsSimilarity(first, second, lcsCutoff);
        int distance = first.Length + second.Length - (2 * lcsLength);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    private static int DistanceCore(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length, scoreCutoff);
        }

        int maximum = first.Length + second.Length;
        int lcsCutoff = scoreCutoff >= maximum ? 0 : ((maximum - scoreCutoff) + 1) / 2;
        int lcsLength = LcsSeq.Similarity(first, second, lcsCutoff);
        int distance = first.Length + second.Length - (2 * lcsLength);
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

        int maximum = first.Length + second.Length;
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0;
        }

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

        int maximum = first.Length + second.Length;
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);

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

        int maximum = first.Length + second.Length;
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

        int maximum = first.Length + second.Length;
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

        int maximum = first.Length + second.Length;
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

        int maximum = first.Length + second.Length;
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, distanceCutoff, distanceHint);

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

        return LcsSeq.Editops(first, second);
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
        return LcsSeq.Editops(first, second);
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
}
