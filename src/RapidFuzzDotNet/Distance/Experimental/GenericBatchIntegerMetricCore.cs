using System.Buffers;
using RapidFuzz.Internal;

namespace RapidFuzz.Distance.Experimental;

internal sealed class GenericBatchIntegerMetricCore<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchPatternScorer<T> scorer;
    private readonly GenericBatchMetric metric;
    private readonly List<T[]> sources;

    public GenericBatchIntegerMetricCore(int capacity, GenericBatchMetric metric)
    {
        scorer = new GenericBatchPatternScorer<T>(capacity);
        this.metric = metric;
        sources = new List<T[]>(capacity);
    }

    public GenericBatchIntegerMetricCore(IEnumerable<T[]> sources, GenericBatchMetric metric)
    {
        ArgumentNullException.ThrowIfNull(sources);

        ICollection<T[]>? sourceCollection = sources as ICollection<T[]>;
        scorer = new GenericBatchPatternScorer<T>(sourceCollection?.Count ?? 0);
        this.metric = metric;
        this.sources = new List<T[]>(sourceCollection?.Count ?? 0);

        foreach (T[] source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null arrays.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => scorer.Count;

    public void Insert(ReadOnlySpan<T> source)
    {
        T[] copy = source.ToArray();
        sources.Add(copy);
        scorer.Insert(copy);
    }

    public ReadOnlySpan<T> GetSource(int index) => sources[index];

    public int[] Distances(ReadOnlySpan<T> target, int scoreCutoff, int scoreHint)
    {
        int[] results = new int[Count];
        Distances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateDestination(destination.Length);

        if (metric == GenericBatchMetric.Levenshtein)
        {
            scorer.LevenshteinDistances(target, destination, scoreCutoff);
            return;
        }

        if (metric == GenericBatchMetric.Osa)
        {
            scorer.OsaDistances(target, destination, scoreCutoff);
            return;
        }

        int[]? rented = null;
        Span<int> scores = RentScores(ref rented);

        try
        {
            FillScores(target, scores);

            for (int index = 0; index < Count; index++)
            {
                int maximum = Maximum(index, target.Length);
                int distance = maximum - SimilarityFromBase(scores[index]);
                destination[index] = DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
            }
        }
        finally
        {
            ReturnScores(rented);
        }
    }

    public int[] Similarities(ReadOnlySpan<T> target, int scoreCutoff, int scoreHint)
    {
        int[] results = new int[Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateDestination(destination.Length);

        if (metric == GenericBatchMetric.Lcs)
        {
            scorer.LcsSimilarities(target, destination, scoreCutoff);
            return;
        }

        int[]? rented = null;
        Span<int> scores = RentScores(ref rented);

        try
        {
            FillScores(target, scores);

            for (int index = 0; index < Count; index++)
            {
                int similarity = metric == GenericBatchMetric.Indel
                    ? 2 * scores[index]
                    : Maximum(index, target.Length) - scores[index];
                destination[index] = similarity >= scoreCutoff ? similarity : 0;
            }
        }
        finally
        {
            ReturnScores(rented);
        }
    }

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        NormalizedDistances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(destination.Length);

        int[]? rented = null;
        Span<int> scores = RentScores(ref rented);

        try
        {
            FillScores(target, scores);

            for (int index = 0; index < Count; index++)
            {
                int maximum = Maximum(index, target.Length);
                int distance = DistanceFromBase(index, target.Length, scores[index]);
                double normalized = maximum == 0 ? 0.0 : (double)distance / maximum;
                destination[index] = normalized <= scoreCutoff ? normalized : 1.0;
            }
        }
        finally
        {
            ReturnScores(rented);
        }
    }

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        NormalizedSimilarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateDestination(destination.Length);

        int[]? rented = null;
        Span<int> scores = RentScores(ref rented);

        try
        {
            FillScores(target, scores);

            for (int index = 0; index < Count; index++)
            {
                int maximum = Maximum(index, target.Length);
                int similarity = SimilarityFromMetric(index, target.Length, scores[index]);
                double normalized = maximum == 0 ? 1.0 : (double)similarity / maximum;
                destination[index] = normalized >= scoreCutoff ? normalized : 0.0;
            }
        }
        finally
        {
            ReturnScores(rented);
        }
    }

    public double[] Ratios(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint, bool quick)
    {
        double[] results = new double[Count];
        Ratios(target, results, scoreCutoff, scoreHint, quick);
        return results;
    }

    public void Ratios(
        ReadOnlySpan<T> target,
        Span<double> destination,
        double scoreCutoff,
        double scoreHint,
        bool quick)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);
        ValidateDestination(destination.Length);

        if (metric != GenericBatchMetric.Indel)
        {
            throw new InvalidOperationException("Ratios require the Indel batch metric.");
        }

        int[]? rented = null;
        Span<int> scores = RentScores(ref rented);

        try
        {
            FillScores(target, scores);

            for (int index = 0; index < Count; index++)
            {
                int sourceLength = scorer.GetLength(index);

                if (quick && (sourceLength == 0 || target.IsEmpty))
                {
                    destination[index] = 0.0;
                    continue;
                }

                int maximum = sourceLength + target.Length;
                int distance = maximum - (2 * scores[index]);
                double ratio = DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, 0.0) * 100.0;
                destination[index] = ratio >= scoreCutoff ? ratio : 0.0;
            }
        }
        finally
        {
            ReturnScores(rented);
        }
    }

    private Span<int> RentScores(ref int[]? rented)
    {
        rented = ArrayPool<int>.Shared.Rent(Math.Max(Count, 1));
        return rented.AsSpan(0, Count);
    }

    private void FillScores(ReadOnlySpan<T> target, Span<int> scores)
    {
        if (metric is GenericBatchMetric.Lcs or GenericBatchMetric.Indel)
        {
            scorer.LcsSimilarities(target, scores, 0);
        }
        else if (metric == GenericBatchMetric.Levenshtein)
        {
            scorer.LevenshteinDistances(target, scores, int.MaxValue);
        }
        else
        {
            scorer.OsaDistances(target, scores, int.MaxValue);
        }
    }

    private static void ReturnScores(int[]? rented)
    {
        if (rented is not null)
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private int DistanceFromBase(int index, int targetLength, int baseScore)
    {
        return metric is GenericBatchMetric.Lcs or GenericBatchMetric.Indel
            ? Maximum(index, targetLength) - SimilarityFromBase(baseScore)
            : baseScore;
    }

    private int SimilarityFromMetric(int index, int targetLength, int baseScore)
    {
        return metric is GenericBatchMetric.Lcs or GenericBatchMetric.Indel
            ? SimilarityFromBase(baseScore)
            : Maximum(index, targetLength) - baseScore;
    }

    private int SimilarityFromBase(int baseScore) => metric == GenericBatchMetric.Indel ? 2 * baseScore : baseScore;

    private int Maximum(int index, int targetLength)
    {
        int sourceLength = scorer.GetLength(index);
        return metric == GenericBatchMetric.Indel
            ? sourceLength + targetLength
            : Math.Max(sourceLength, targetLength);
    }

    private void ValidateDestination(int destination)
    {
        if (destination < Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }
    }
}

internal enum GenericBatchMetric
{
    Levenshtein,
    Indel,
    Lcs,
    Osa
}
