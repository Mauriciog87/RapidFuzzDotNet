namespace RapidFuzz.Distance;

public static partial class Levenshtein
{
    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        return Distance(first, second, comparer, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        LevenshteinWeights weights,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.LevenshteinDistance(first, second, comparer, weights, scoreCutoff);
    }

    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        return Similarity(first, second, comparer, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        LevenshteinWeights weights,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights);

        if (scoreCutoff > maximum)
        {
            return 0;
        }

        int distance = Distance(first, second, comparer, weights, maximum - scoreCutoff, maximum - scoreHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return NormalizedDistance(first, second, comparer, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        LevenshteinWeights weights,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights);
        int distance = Distance(
            first,
            second,
            comparer,
            weights,
            (int)Math.Floor(maximum * scoreCutoff),
            (int)Math.Floor(maximum * scoreHint));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return NormalizedSimilarity(first, second, comparer, LevenshteinWeights.Default, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        LevenshteinWeights weights,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = SequenceMetrics.LevenshteinMaximum(first.Length, second.Length, weights);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, comparer, weights, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static EditOperations Editops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeSequenceMetrics.LevenshteinEditops(first, second, comparer);
    }

    public static Opcodes Opcodes<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        return Editops(first, second, comparer).ToOpcodes();
    }
}

public static partial class LcsSeq
{
    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.LcsSimilarity(first, second, comparer, scoreCutoff);
    }

    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int similarityCutoff = Math.Max(0, maximum - scoreCutoff);
        int similarity = Similarity(first, second, comparer, similarityCutoff);
        return DistanceHelpers.ApplyDistanceCutoff(maximum - similarity, scoreCutoff);
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(
            first,
            second,
            comparer,
            (int)Math.Floor(maximum * scoreCutoff),
            (int)Math.Floor(maximum * scoreHint));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        int similarity = Similarity(first, second, comparer, (int)Math.Ceiling(maximum * scoreCutoff));
        double normalized = (double)similarity / maximum;
        return normalized >= scoreCutoff ? normalized : 0.0;
    }

    public static EditOperations Editops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeSequenceMetrics.LcsEditops(first, second, comparer);
    }

    public static Opcodes Opcodes<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        return Editops(first, second, comparer).ToOpcodes();
    }
}

public static partial class Indel
{
    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = first.Length + second.Length;
        int lcsCutoff = Math.Max(0, (maximum - scoreCutoff + 1) / 2);
        int lcs = CrossTypeSequenceMetrics.LcsSimilarity(first, second, comparer, lcsCutoff);
        return DistanceHelpers.ApplyDistanceCutoff(maximum - 2 * lcs, scoreCutoff);
    }

    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int lcs = CrossTypeSequenceMetrics.LcsSimilarity(first, second, comparer, (scoreCutoff + 1) / 2);
        int similarity = lcs * 2;
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = first.Length + second.Length;
        int distance = Distance(
            first,
            second,
            comparer,
            (int)Math.Floor(maximum * scoreCutoff),
            (int)Math.Floor(maximum * scoreHint));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = first.Length + second.Length;

        if (maximum == 0)
        {
            return 1.0;
        }

        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, comparer, distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0.0;
        }

        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static EditOperations Editops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeSequenceMetrics.LcsEditops(first, second, comparer);
    }

    public static Opcodes Opcodes<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        return Editops(first, second, comparer).ToOpcodes();
    }
}

public static partial class Hamming
{
    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad = true,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.HammingDistance(first, second, comparer, pad, scoreCutoff);
    }

    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad = true,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, pad, maximum - Math.Min(scoreCutoff, maximum));
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad = true,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, pad, (int)Math.Floor(maximum * scoreCutoff));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad = true,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, pad, maximum);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static EditOperations Editops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad = true)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeSequenceMetrics.HammingEditops(first, second, comparer, pad);
    }

    public static Opcodes Opcodes<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad = true)
        where TLeft : notnull
        where TRight : notnull
    {
        return Editops(first, second, comparer, pad).ToOpcodes();
    }
}

public static partial class Osa
{
    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.OsaDistance(first, second, comparer, scoreCutoff);
    }

    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeIntegerSimilarity(first, second, comparer, scoreCutoff, scoreHint, Distance);
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeIntegerNormalizedDistance(first, second, comparer, scoreCutoff, scoreHint, Distance);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return CrossTypeIntegerNormalizedSimilarity(first, second, comparer, scoreCutoff, scoreHint, Distance);
    }

    private static int CrossTypeIntegerSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff,
        int scoreHint,
        CrossTypeIntegerDistance<TLeft, TRight> distanceFunction)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = distanceFunction(first, second, comparer, maximum - Math.Min(scoreCutoff, maximum), maximum - Math.Min(scoreHint, maximum));
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    private static double CrossTypeIntegerNormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff,
        double scoreHint,
        CrossTypeIntegerDistance<TLeft, TRight> distanceFunction)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = distanceFunction(first, second, comparer, (int)Math.Floor(maximum * scoreCutoff), (int)Math.Floor(maximum * scoreHint));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    private static double CrossTypeIntegerNormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff,
        double scoreHint,
        CrossTypeIntegerDistance<TLeft, TRight> distanceFunction)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = distanceFunction(
            first,
            second,
            comparer,
            (int)Math.Floor(maximum * (1.0 - scoreCutoff)),
            (int)Math.Floor(maximum * (1.0 - scoreHint)));
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    private delegate int CrossTypeIntegerDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff,
        int scoreHint)
        where TLeft : notnull
        where TRight : notnull;
}

public static partial class DamerauLevenshtein
{
    public static int Distance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = int.MaxValue,
        int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.DamerauLevenshteinDistance(first, second, comparer, scoreCutoff);
    }

    public static int Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff = 0,
        int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, maximum - Math.Min(scoreCutoff, maximum));
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 1.0,
        double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, (int)Math.Floor(maximum * scoreCutoff));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, (int)Math.Floor(maximum * (1.0 - scoreCutoff)));
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }
}

public static partial class Prefix
{
    public static int Similarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.PrefixSimilarity(first, second, comparer, scoreCutoff);
    }

    public static int Distance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        return DistanceHelpers.ApplyDistanceCutoff(maximum - Similarity(first, second, comparer), scoreCutoff);
    }

    public static double NormalizedDistance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, (int)Math.Floor(maximum * scoreCutoff));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        double similarity = (double)Similarity(first, second, comparer) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }
}

public static partial class Postfix
{
    public static int Similarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return CrossTypeSequenceMetrics.PostfixSimilarity(first, second, comparer, scoreCutoff);
    }

    public static int Distance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        return DistanceHelpers.ApplyDistanceCutoff(maximum - Similarity(first, second, comparer), scoreCutoff);
    }

    public static double NormalizedDistance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);
        int distance = Distance(first, second, comparer, (int)Math.Floor(maximum * scoreCutoff));
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        int maximum = Math.Max(first.Length, second.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        double similarity = (double)Similarity(first, second, comparer) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }
}

public static partial class Jaro
{
    public static double Similarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        return CrossTypeSequenceMetrics.JaroSimilarity(first, second, comparer, scoreCutoff);
    }

    public static double Distance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        double similarity = Similarity(first, second, comparer, 1.0 - scoreCutoff);
        double distance = 1.0 - similarity;
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public static double NormalizedDistance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return Distance(first, second, comparer, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return Similarity(first, second, comparer, scoreCutoff, scoreHint);
    }
}

public static partial class JaroWinkler
{
    public static double Similarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double prefixWeight = 0.1,
        double scoreCutoff = 0.0,
        double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        ValidatePrefixWeight(prefixWeight);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        double similarity = CrossTypeSequenceMetrics.JaroSimilarity(first, second, comparer, 0.0);

        if (similarity > 0.7)
        {
            int prefixLength = Math.Min(4, Math.Min(first.Length, second.Length));
            int commonPrefix = 0;

            while (commonPrefix < prefixLength && comparer.Equals(first[commonPrefix], second[commonPrefix]))
            {
                commonPrefix++;
            }

            similarity += commonPrefix * prefixWeight * (1.0 - similarity);
        }

        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double Distance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double prefixWeight = 0.1, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        double similarity = Similarity(first, second, comparer, prefixWeight, 1.0 - scoreCutoff);
        double distance = 1.0 - similarity;
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public static double NormalizedDistance<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double prefixWeight = 0.1, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return Distance(first, second, comparer, prefixWeight, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity<TLeft, TRight>(ReadOnlySpan<TLeft> first, ReadOnlySpan<TRight> second, ISequenceEqualityComparer<TLeft, TRight> comparer, double prefixWeight = 0.1, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return Similarity(first, second, comparer, prefixWeight, scoreCutoff, scoreHint);
    }
}
