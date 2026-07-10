namespace RapidFuzz.Distance.Experimental;

internal static class CrossTypeMultiCore
{
    public static int[] IntegerDistances<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        int scoreCutoff,
        int scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        int[] results = new int[core.Count];
        IntegerDistances(core, target, results, comparer, metric, scoreCutoff, scoreHint);
        return results;
    }

    public static void IntegerDistances<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        Span<int> destination,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        int scoreCutoff,
        int scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateDestination(core.Count, destination.Length);

        for (int index = 0; index < core.Count; index++)
        {
            ReadOnlySpan<TSource> source = core.GetSource(index);
            destination[index] = metric switch
            {
                CrossTypeIntegerMetric.Levenshtein => Levenshtein.Distance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Indel => Indel.Distance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Lcs => LcsSeq.Distance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Osa => Osa.Distance(source, target, comparer, scoreCutoff, scoreHint),
                _ => throw new InvalidOperationException("The cross-type metric is invalid.")
            };
        }
    }

    public static int[] IntegerSimilarities<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        int scoreCutoff,
        int scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        int[] results = new int[core.Count];
        IntegerSimilarities(core, target, results, comparer, metric, scoreCutoff, scoreHint);
        return results;
    }

    public static void IntegerSimilarities<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        Span<int> destination,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        int scoreCutoff,
        int scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateDestination(core.Count, destination.Length);

        for (int index = 0; index < core.Count; index++)
        {
            ReadOnlySpan<TSource> source = core.GetSource(index);
            destination[index] = metric switch
            {
                CrossTypeIntegerMetric.Levenshtein => Levenshtein.Similarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Indel => Indel.Similarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Lcs => LcsSeq.Similarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Osa => Osa.Similarity(source, target, comparer, scoreCutoff, scoreHint),
                _ => throw new InvalidOperationException("The cross-type metric is invalid.")
            };
        }
    }

    public static double[] IntegerNormalizedDistances<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        double[] results = new double[core.Count];
        IntegerNormalizedDistances(core, target, results, comparer, metric, scoreCutoff, scoreHint);
        return results;
    }

    public static void IntegerNormalizedDistances<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        Span<double> destination,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(core.Count, destination.Length);

        for (int index = 0; index < core.Count; index++)
        {
            ReadOnlySpan<TSource> source = core.GetSource(index);
            destination[index] = metric switch
            {
                CrossTypeIntegerMetric.Levenshtein => Levenshtein.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Indel => Indel.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Lcs => LcsSeq.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Osa => Osa.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint),
                _ => throw new InvalidOperationException("The cross-type metric is invalid.")
            };
        }
    }

    public static double[] IntegerNormalizedSimilarities<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        double[] results = new double[core.Count];
        IntegerNormalizedSimilarities(core, target, results, comparer, metric, scoreCutoff, scoreHint);
        return results;
    }

    public static void IntegerNormalizedSimilarities<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        Span<double> destination,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeIntegerMetric metric,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(core.Count, destination.Length);

        for (int index = 0; index < core.Count; index++)
        {
            ReadOnlySpan<TSource> source = core.GetSource(index);
            destination[index] = metric switch
            {
                CrossTypeIntegerMetric.Levenshtein => Levenshtein.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Indel => Indel.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Lcs => LcsSeq.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeIntegerMetric.Osa => Osa.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint),
                _ => throw new InvalidOperationException("The cross-type metric is invalid.")
            };
        }
    }

    public static double[] JaroScores<TSource, TTarget>(
        GenericMultiDoubleDistanceScorerCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeDoubleScore score,
        double prefixWeight,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        double[] results = new double[core.Count];
        JaroScores(core, target, results, comparer, score, prefixWeight, scoreCutoff, scoreHint);
        return results;
    }

    public static void JaroScores<TSource, TTarget>(
        GenericMultiDoubleDistanceScorerCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        Span<double> destination,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        CrossTypeDoubleScore score,
        double prefixWeight,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(core.Count, destination.Length);

        for (int index = 0; index < core.Count; index++)
        {
            ReadOnlySpan<TSource> source = core.GetSource(index);
            destination[index] = score switch
            {
                CrossTypeDoubleScore.JaroDistance => Jaro.Distance(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeDoubleScore.JaroSimilarity => Jaro.Similarity(source, target, comparer, scoreCutoff, scoreHint),
                CrossTypeDoubleScore.JaroWinklerDistance => JaroWinkler.Distance(source, target, comparer, prefixWeight, scoreCutoff, scoreHint),
                CrossTypeDoubleScore.JaroWinklerSimilarity => JaroWinkler.Similarity(source, target, comparer, prefixWeight, scoreCutoff, scoreHint),
                _ => throw new InvalidOperationException("The cross-type score is invalid.")
            };
        }
    }

    public static double[] Ratios<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        bool quick,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        double[] results = new double[core.Count];
        Ratios(core, target, results, comparer, quick, scoreCutoff, scoreHint);
        return results;
    }

    public static void Ratios<TSource, TTarget>(
        GenericBatchIntegerMetricCore<TSource> core,
        ReadOnlySpan<TTarget> target,
        Span<double> destination,
        ISequenceEqualityComparer<TSource, TTarget> comparer,
        bool quick,
        double scoreCutoff,
        double scoreHint)
        where TSource : notnull, IEquatable<TSource>
        where TTarget : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);
        ValidateDestination(core.Count, destination.Length);

        for (int index = 0; index < core.Count; index++)
        {
            ReadOnlySpan<TSource> source = core.GetSource(index);
            destination[index] = quick
                ? Fuzz.QRatio(source, target, comparer, scoreCutoff)
                : Fuzz.Ratio(source, target, comparer, scoreCutoff);
        }
    }

    private static void ValidateDestination(int count, int destination)
    {
        if (destination < count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }
    }
}

internal enum CrossTypeIntegerMetric
{
    Levenshtein,
    Indel,
    Lcs,
    Osa
}

internal enum CrossTypeDoubleScore
{
    JaroDistance,
    JaroSimilarity,
    JaroWinklerDistance,
    JaroWinklerSimilarity
}
