namespace RapidFuzz.Distance.Experimental;

using RapidFuzz.Internal;

public sealed class MultiLevenshtein<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchIntegerMetricCore<T>? batchCore;
    private readonly GenericMultiIntegerDistanceScorerCore<T>? scalarCore;

    public MultiLevenshtein(int capacity)
        : this(capacity, LevenshteinWeights.Default)
    {
    }

    public MultiLevenshtein(int capacity, LevenshteinWeights weights)
    {
        if (weights == LevenshteinWeights.Default)
        {
            batchCore = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Levenshtein);
        }
        else
        {
            scalarCore = new GenericMultiIntegerDistanceScorerCore<T>(capacity, source => CreateScorer(source, weights));
        }
    }

    public MultiLevenshtein(IEnumerable<T[]> sources)
        : this(sources, LevenshteinWeights.Default)
    {
    }

    public MultiLevenshtein(IEnumerable<T[]> sources, LevenshteinWeights weights)
    {
        ArgumentNullException.ThrowIfNull(sources);

        ICollection<T[]>? sourceCollection = sources as ICollection<T[]>;
        int capacity = sourceCollection?.Count ?? 0;

        if (weights == LevenshteinWeights.Default)
        {
            batchCore = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Levenshtein);
        }
        else
        {
            scalarCore = new GenericMultiIntegerDistanceScorerCore<T>(capacity, source => CreateScorer(source, weights));
        }

        foreach (T[] source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null arrays.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => batchCore?.Count ?? scalarCore!.Count;

    public void Insert(ReadOnlySpan<T> source)
    {
        if (batchCore is not null)
        {
            batchCore.Insert(source);
        }
        else
        {
            scalarCore!.Insert(source);
        }
    }

    public int[] Distances(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0) =>
        batchCore?.Distances(target, scoreCutoff, scoreHint) ?? scalarCore!.Distances(target, scoreCutoff, scoreHint);

    public void Distances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0) =>
        DispatchDistances(target, destination, scoreCutoff, scoreHint);

    public int[] Similarities(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0) =>
        batchCore?.Similarities(target, scoreCutoff, scoreHint) ?? scalarCore!.Similarities(target, scoreCutoff, scoreHint);

    public void Similarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0) =>
        DispatchSimilarities(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        batchCore?.NormalizedDistances(target, scoreCutoff, scoreHint) ?? scalarCore!.NormalizedDistances(target, scoreCutoff, scoreHint);

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        DispatchNormalizedDistances(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        batchCore?.NormalizedSimilarities(target, scoreCutoff, scoreHint) ?? scalarCore!.NormalizedSimilarities(target, scoreCutoff, scoreHint);

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        DispatchNormalizedSimilarities(target, destination, scoreCutoff, scoreHint);

    private void DispatchDistances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        if (batchCore is not null)
        {
            batchCore.Distances(target, destination, scoreCutoff, scoreHint);
        }
        else
        {
            scalarCore!.Distances(target, destination, scoreCutoff, scoreHint);
        }
    }

    private void DispatchSimilarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        if (batchCore is not null)
        {
            batchCore.Similarities(target, destination, scoreCutoff, scoreHint);
        }
        else
        {
            scalarCore!.Similarities(target, destination, scoreCutoff, scoreHint);
        }
    }

    private void DispatchNormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        if (batchCore is not null)
        {
            batchCore.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
        }
        else
        {
            scalarCore!.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
        }
    }

    private void DispatchNormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        if (batchCore is not null)
        {
            batchCore.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
        }
        else
        {
            scalarCore!.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
        }
    }

    private static GenericIntegerDistanceScorer<T> CreateScorer(ReadOnlySpan<T> source, LevenshteinWeights weights)
    {
        CachedLevenshtein<T> scorer = new(source, weights);
        return new GenericIntegerDistanceScorer<T>(scorer.Distance, scorer.Similarity, scorer.NormalizedDistance, scorer.NormalizedSimilarity);
    }
}

public sealed class MultiIndel<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchIntegerMetricCore<T> core;

    public MultiIndel(int capacity) => core = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Indel);

    public MultiIndel(IEnumerable<T[]> sources) => core = new GenericBatchIntegerMetricCore<T>(sources, GenericBatchMetric.Indel);

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public int[] Distances(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0) => core.Distances(target, scoreCutoff, scoreHint);

    public void Distances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0) =>
        core.Distances(target, destination, scoreCutoff, scoreHint);

    public int[] Similarities(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0) => core.Similarities(target, scoreCutoff, scoreHint);

    public void Similarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0) =>
        core.Similarities(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, scoreCutoff, scoreHint);

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, scoreCutoff, scoreHint);

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);

}

public sealed class MultiLcsSeq<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchIntegerMetricCore<T> core;

    public MultiLcsSeq(int capacity) => core = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Lcs);

    public MultiLcsSeq(IEnumerable<T[]> sources) => core = new GenericBatchIntegerMetricCore<T>(sources, GenericBatchMetric.Lcs);

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public int[] Distances(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0) => core.Distances(target, scoreCutoff, scoreHint);

    public void Distances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0) =>
        core.Distances(target, destination, scoreCutoff, scoreHint);

    public int[] Similarities(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0) => core.Similarities(target, scoreCutoff, scoreHint);

    public void Similarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0) =>
        core.Similarities(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, scoreCutoff, scoreHint);

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, scoreCutoff, scoreHint);

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);

}

public sealed class MultiOsa<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchIntegerMetricCore<T> core;

    public MultiOsa(int capacity) => core = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Osa);

    public MultiOsa(IEnumerable<T[]> sources) => core = new GenericBatchIntegerMetricCore<T>(sources, GenericBatchMetric.Osa);

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public int[] Distances(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0) => core.Distances(target, scoreCutoff, scoreHint);

    public void Distances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0) =>
        core.Distances(target, destination, scoreCutoff, scoreHint);

    public int[] Similarities(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0) => core.Similarities(target, scoreCutoff, scoreHint);

    public void Similarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0) =>
        core.Similarities(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, scoreCutoff, scoreHint);

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, scoreCutoff, scoreHint);

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);

}

public sealed class MultiJaro<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericMultiDoubleDistanceScorerCore<T> core;

    public MultiJaro(int capacity) => core = new GenericMultiDoubleDistanceScorerCore<T>(capacity, CreateScorer);

    public MultiJaro(IEnumerable<T[]> sources) => core = new GenericMultiDoubleDistanceScorerCore<T>(sources, CreateScorer);

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public double[] Distances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) => core.Distances(target, scoreCutoff, scoreHint);

    public void Distances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.Distances(target, destination, scoreCutoff, scoreHint);

    public double[] Similarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) => core.Similarities(target, scoreCutoff, scoreHint);

    public void Similarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.Similarities(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) => core.NormalizedDistances(target, scoreCutoff, scoreHint);

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) => core.NormalizedSimilarities(target, scoreCutoff, scoreHint);

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);

    private static GenericDoubleDistanceScorer<T> CreateScorer(ReadOnlySpan<T> source)
    {
        CachedJaro<T> scorer = new(source);
        return new GenericDoubleDistanceScorer<T>(scorer.Distance, scorer.Similarity, scorer.NormalizedDistance, scorer.NormalizedSimilarity);
    }
}

public sealed class MultiJaroWinkler<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericMultiDoubleDistanceScorerCore<T> core;

    public MultiJaroWinkler(int capacity, double prefixWeight = 0.1)
    {
        JaroWinkler.ValidatePrefixWeight(prefixWeight);
        core = new GenericMultiDoubleDistanceScorerCore<T>(capacity, source => CreateScorer(source, prefixWeight));
    }

    public MultiJaroWinkler(IEnumerable<T[]> sources, double prefixWeight = 0.1)
    {
        JaroWinkler.ValidatePrefixWeight(prefixWeight);
        core = new GenericMultiDoubleDistanceScorerCore<T>(sources, source => CreateScorer(source, prefixWeight));
    }

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public double[] Distances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) => core.Distances(target, scoreCutoff, scoreHint);

    public void Distances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.Distances(target, destination, scoreCutoff, scoreHint);

    public double[] Similarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) => core.Similarities(target, scoreCutoff, scoreHint);

    public void Similarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.Similarities(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0) => core.NormalizedDistances(target, scoreCutoff, scoreHint);

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0) =>
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) => core.NormalizedSimilarities(target, scoreCutoff, scoreHint);

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);

    private static GenericDoubleDistanceScorer<T> CreateScorer(ReadOnlySpan<T> source, double prefixWeight)
    {
        CachedJaroWinkler<T> scorer = new(source, prefixWeight);
        return new GenericDoubleDistanceScorer<T>(scorer.Distance, scorer.Similarity, scorer.NormalizedDistance, scorer.NormalizedSimilarity);
    }
}

internal delegate int GenericIntegerScoreFunction<T>(ReadOnlySpan<T> target, int scoreCutoff, int scoreHint)
    where T : notnull, IEquatable<T>;

internal delegate double GenericNormalizedScoreFunction<T>(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    where T : notnull, IEquatable<T>;

internal delegate GenericIntegerDistanceScorer<T> GenericIntegerScorerFactory<T>(ReadOnlySpan<T> source)
    where T : notnull, IEquatable<T>;

internal delegate GenericDoubleDistanceScorer<T> GenericDoubleScorerFactory<T>(ReadOnlySpan<T> source)
    where T : notnull, IEquatable<T>;

internal readonly record struct GenericIntegerDistanceScorer<T>(
    GenericIntegerScoreFunction<T> Distance,
    GenericIntegerScoreFunction<T> Similarity,
    GenericNormalizedScoreFunction<T> NormalizedDistance,
    GenericNormalizedScoreFunction<T> NormalizedSimilarity)
    where T : notnull, IEquatable<T>;

internal readonly record struct GenericDoubleDistanceScorer<T>(
    GenericNormalizedScoreFunction<T> Distance,
    GenericNormalizedScoreFunction<T> Similarity,
    GenericNormalizedScoreFunction<T> NormalizedDistance,
    GenericNormalizedScoreFunction<T> NormalizedSimilarity)
    where T : notnull, IEquatable<T>;

internal sealed class GenericMultiIntegerDistanceScorerCore<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericIntegerScorerFactory<T> scorerFactory;
    private readonly List<GenericIntegerDistanceScorer<T>> scorers;

    public GenericMultiIntegerDistanceScorerCore(int capacity, GenericIntegerScorerFactory<T> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(scorerFactory);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        this.scorerFactory = scorerFactory;
        scorers = new List<GenericIntegerDistanceScorer<T>>(capacity);
    }

    public GenericMultiIntegerDistanceScorerCore(IEnumerable<T[]> sources, GenericIntegerScorerFactory<T> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(scorerFactory);

        this.scorerFactory = scorerFactory;
        ICollection<T[]>? sourceCollection = sources as ICollection<T[]>;
        scorers = new List<GenericIntegerDistanceScorer<T>>(sourceCollection?.Count ?? 0);

        foreach (T[] source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null arrays.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => scorers.Count;

    public void Insert(ReadOnlySpan<T> source) => scorers.Add(scorerFactory(source));

    public int[] Distances(ReadOnlySpan<T> target, int scoreCutoff, int scoreHint)
    {
        int[] results = new int[Count];
        Distances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, true);

    public int[] Similarities(ReadOnlySpan<T> target, int scoreCutoff, int scoreHint)
    {
        int[] results = new int[Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, false);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        NormalizedDistances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, true);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        NormalizedSimilarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, false);

    private void Score(ReadOnlySpan<T> target, Span<int> destination, int scoreCutoff, int scoreHint, bool distance)
    {
        ValidateDestination(destination.Length);

        for (int index = 0; index < Count; index++)
        {
            GenericIntegerScoreFunction<T> scorer = distance ? scorers[index].Distance : scorers[index].Similarity;
            destination[index] = scorer(target, scoreCutoff, scoreHint);
        }
    }

    private void Score(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint, bool distance)
    {
        ValidateDestination(destination.Length);

        for (int index = 0; index < Count; index++)
        {
            GenericNormalizedScoreFunction<T> scorer = distance
                ? scorers[index].NormalizedDistance
                : scorers[index].NormalizedSimilarity;
            destination[index] = scorer(target, scoreCutoff, scoreHint);
        }
    }

    private void ValidateDestination(int destination)
    {
        if (destination < Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }
    }
}

internal sealed class GenericMultiDoubleDistanceScorerCore<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericDoubleScorerFactory<T> scorerFactory;
    private readonly List<GenericDoubleDistanceScorer<T>> scorers;

    public GenericMultiDoubleDistanceScorerCore(int capacity, GenericDoubleScorerFactory<T> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(scorerFactory);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        this.scorerFactory = scorerFactory;
        scorers = new List<GenericDoubleDistanceScorer<T>>(capacity);
    }

    public GenericMultiDoubleDistanceScorerCore(IEnumerable<T[]> sources, GenericDoubleScorerFactory<T> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(scorerFactory);

        this.scorerFactory = scorerFactory;
        ICollection<T[]>? sourceCollection = sources as ICollection<T[]>;
        scorers = new List<GenericDoubleDistanceScorer<T>>(sourceCollection?.Count ?? 0);

        foreach (T[] source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null arrays.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => scorers.Count;

    public void Insert(ReadOnlySpan<T> source) => scorers.Add(scorerFactory(source));

    public double[] Distances(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        Distances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, true, false);

    public double[] Similarities(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, false, false);

    public double[] NormalizedDistances(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        NormalizedDistances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedDistances(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, true, true);

    public double[] NormalizedSimilarities(ReadOnlySpan<T> target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[Count];
        NormalizedSimilarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedSimilarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff, double scoreHint) =>
        Score(target, destination, scoreCutoff, scoreHint, false, true);

    private void Score(
        ReadOnlySpan<T> target,
        Span<double> destination,
        double scoreCutoff,
        double scoreHint,
        bool distance,
        bool normalized)
    {
        if (destination.Length < Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int index = 0; index < Count; index++)
        {
            GenericNormalizedScoreFunction<T> scorer = normalized
                ? distance ? scorers[index].NormalizedDistance : scorers[index].NormalizedSimilarity
                : distance ? scorers[index].Distance : scorers[index].Similarity;
            destination[index] = scorer(target, scoreCutoff, scoreHint);
        }

        if (!distance)
        {
            SimdSupport.ApplySimilarityCutoff(destination[..Count], scoreCutoff);
        }
    }
}
