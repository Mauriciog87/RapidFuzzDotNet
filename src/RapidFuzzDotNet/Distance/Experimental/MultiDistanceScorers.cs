namespace RapidFuzz.Distance.Experimental;

using RapidFuzz.Internal;

public sealed class MultiLevenshtein
{
    private readonly TextBatchIntegerMetricCore? batchCore;
    private readonly MultiIntegerDistanceScorerCore? scalarCore;

    public MultiLevenshtein(int capacity)
        : this(capacity, LevenshteinWeights.Default)
    {
    }

    public MultiLevenshtein(int capacity, LevenshteinWeights weights)
    {
        if (weights == LevenshteinWeights.Default)
        {
            batchCore = new TextBatchIntegerMetricCore(capacity, GenericBatchMetric.Levenshtein);
        }
        else
        {
            scalarCore = new MultiIntegerDistanceScorerCore(capacity, source => CreateScorer(source, weights));
        }
    }

    public MultiLevenshtein(IEnumerable<string> sources)
        : this(sources, LevenshteinWeights.Default)
    {
    }

    public MultiLevenshtein(IEnumerable<string> sources, LevenshteinWeights weights)
    {
        ArgumentNullException.ThrowIfNull(sources);

        ICollection<string>? sourceCollection = sources as ICollection<string>;
        int capacity = sourceCollection?.Count ?? 0;

        if (weights == LevenshteinWeights.Default)
        {
            batchCore = new TextBatchIntegerMetricCore(capacity, GenericBatchMetric.Levenshtein);
        }
        else
        {
            scalarCore = new MultiIntegerDistanceScorerCore(capacity, source => CreateScorer(source, weights));
        }

        foreach (string source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null values.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => batchCore?.Count ?? scalarCore!.Count;

    public void Insert(string source)
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

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return batchCore?.Distances(target, scoreCutoff, scoreHint)
            ?? scalarCore!.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
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

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return batchCore?.Similarities(target, scoreCutoff, scoreHint)
            ?? scalarCore!.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
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

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return batchCore?.NormalizedDistances(target, scoreCutoff, scoreHint)
            ?? scalarCore!.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
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

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return batchCore?.NormalizedSimilarities(target, scoreCutoff, scoreHint)
            ?? scalarCore!.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
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

    private static IntegerDistanceScorer CreateScorer(string source, LevenshteinWeights weights)
    {
        CachedLevenshtein scorer = new(source, weights);
        return new IntegerDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

public sealed class MultiIndel
{
    private readonly TextBatchIntegerMetricCore core;

    public MultiIndel(int capacity)
    {
        core = new TextBatchIntegerMetricCore(capacity, GenericBatchMetric.Indel);
    }

    public MultiIndel(IEnumerable<string> sources)
    {
        core = new TextBatchIntegerMetricCore(sources, GenericBatchMetric.Indel);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }
}

public sealed class MultiLcsSeq
{
    private readonly TextBatchIntegerMetricCore core;

    public MultiLcsSeq(int capacity)
    {
        core = new TextBatchIntegerMetricCore(capacity, GenericBatchMetric.Lcs);
    }

    public MultiLcsSeq(IEnumerable<string> sources)
    {
        core = new TextBatchIntegerMetricCore(sources, GenericBatchMetric.Lcs);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }
}

public sealed class MultiOsa
{
    private readonly TextBatchIntegerMetricCore core;

    public MultiOsa(int capacity)
    {
        core = new TextBatchIntegerMetricCore(capacity, GenericBatchMetric.Osa);
    }

    public MultiOsa(IEnumerable<string> sources)
    {
        core = new TextBatchIntegerMetricCore(sources, GenericBatchMetric.Osa);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }
}

public sealed class MultiHamming
{
    private readonly MultiIntegerDistanceScorerCore core;

    public MultiHamming(int capacity, bool pad = true)
    {
        core = new MultiIntegerDistanceScorerCore(capacity, source => CreateScorer(source, pad));
    }

    public MultiHamming(IEnumerable<string> sources, bool pad = true)
    {
        core = new MultiIntegerDistanceScorerCore(sources, source => CreateScorer(source, pad));
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }

    private static IntegerDistanceScorer CreateScorer(string source, bool pad)
    {
        CachedHamming scorer = new(source, pad);
        return new IntegerDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

public sealed class MultiDamerauLevenshtein
{
    private readonly MultiIntegerDistanceScorerCore core;

    public MultiDamerauLevenshtein(int capacity)
    {
        core = new MultiIntegerDistanceScorerCore(capacity, CreateScorer);
    }

    public MultiDamerauLevenshtein(IEnumerable<string> sources)
    {
        core = new MultiIntegerDistanceScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }

    private static IntegerDistanceScorer CreateScorer(string source)
    {
        CachedDamerauLevenshtein scorer = new(source);
        return new IntegerDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

public sealed class MultiPrefix
{
    private readonly MultiIntegerDistanceScorerCore core;

    public MultiPrefix(int capacity)
    {
        core = new MultiIntegerDistanceScorerCore(capacity, CreateScorer);
    }

    public MultiPrefix(IEnumerable<string> sources)
    {
        core = new MultiIntegerDistanceScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }

    private static IntegerDistanceScorer CreateScorer(string source)
    {
        CachedPrefix scorer = new(source);
        return new IntegerDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

public sealed class MultiPostfix
{
    private readonly MultiIntegerDistanceScorerCore core;

    public MultiPostfix(int capacity)
    {
        core = new MultiIntegerDistanceScorerCore(capacity, CreateScorer);
    }

    public MultiPostfix(IEnumerable<string> sources)
    {
        core = new MultiIntegerDistanceScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public int[] Distances(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff = 0, int scoreHint = 0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }

    private static IntegerDistanceScorer CreateScorer(string source)
    {
        CachedPostfix scorer = new(source);
        return new IntegerDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

public sealed class MultiJaro
{
    private readonly MultiDoubleDistanceScorerCore core;

    public MultiJaro(int capacity)
    {
        core = new MultiDoubleDistanceScorerCore(capacity, CreateScorer, vectorizeSimilarityCutoff: true);
    }

    public MultiJaro(IEnumerable<string> sources)
    {
        core = new MultiDoubleDistanceScorerCore(sources, CreateScorer, vectorizeSimilarityCutoff: true);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Distances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }

    private static DoubleDistanceScorer CreateScorer(string source)
    {
        CachedJaro scorer = new(source);
        return new DoubleDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

public sealed class MultiJaroWinkler
{
    private readonly MultiDoubleDistanceScorerCore core;

    public MultiJaroWinkler(int capacity, double prefixWeight = 0.1)
    {
        core = new MultiDoubleDistanceScorerCore(capacity, source => CreateScorer(source, prefixWeight), vectorizeSimilarityCutoff: true);
    }

    public MultiJaroWinkler(IEnumerable<string> sources, double prefixWeight = 0.1)
    {
        core = new MultiDoubleDistanceScorerCore(sources, source => CreateScorer(source, prefixWeight), vectorizeSimilarityCutoff: true);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Distances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.Distances(target, scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.Distances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return core.NormalizedDistances(target, scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        core.NormalizedDistances(target, destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.NormalizedSimilarities(target, scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.NormalizedSimilarities(target, destination, scoreCutoff, scoreHint);
    }

    private static DoubleDistanceScorer CreateScorer(string source, double prefixWeight)
    {
        CachedJaroWinkler scorer = new(source, prefixWeight);
        return new DoubleDistanceScorer(
            scorer.Distance,
            scorer.Similarity,
            scorer.NormalizedDistance,
            scorer.NormalizedSimilarity);
    }
}

internal delegate int IntegerScoreFunction(string target, int scoreCutoff, int scoreHint);

internal delegate double NormalizedScoreFunction(string target, double scoreCutoff, double scoreHint);

internal delegate double DoubleScoreFunction(string target, double scoreCutoff, double scoreHint);

internal readonly record struct IntegerDistanceScorer(
    IntegerScoreFunction Distance,
    IntegerScoreFunction Similarity,
    NormalizedScoreFunction NormalizedDistance,
    NormalizedScoreFunction NormalizedSimilarity);

internal readonly record struct DoubleDistanceScorer(
    DoubleScoreFunction Distance,
    DoubleScoreFunction Similarity,
    DoubleScoreFunction NormalizedDistance,
    DoubleScoreFunction NormalizedSimilarity);

internal sealed class MultiIntegerDistanceScorerCore
{
    private readonly Func<string, IntegerDistanceScorer> scorerFactory;
    private readonly List<IntegerDistanceScorer> scorers;

    public MultiIntegerDistanceScorerCore(
        int capacity,
        Func<string, IntegerDistanceScorer> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(scorerFactory);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        this.scorerFactory = scorerFactory;
        scorers = new List<IntegerDistanceScorer>(capacity);
    }

    public MultiIntegerDistanceScorerCore(
        IEnumerable<string> sources,
        Func<string, IntegerDistanceScorer> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(scorerFactory);

        this.scorerFactory = scorerFactory;
        ICollection<string>? sourceCollection = sources as ICollection<string>;
        scorers = new List<IntegerDistanceScorer>(sourceCollection?.Count ?? 0);

        foreach (string source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null values.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => scorers.Count;

    public void Insert(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        scorers.Add(scorerFactory(source));
    }

    public int[] Distances(string target, int scoreCutoff, int scoreHint)
    {
        int[] results = new int[scorers.Count];
        Distances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.Distance);
    }

    public int[] Similarities(string target, int scoreCutoff, int scoreHint)
    {
        int[] results = new int[scorers.Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        ScoreSimilarity(target, destination, scoreCutoff, scoreHint, static scorer => scorer.Similarity);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        NormalizedDistances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.NormalizedDistance);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        NormalizedSimilarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.NormalizedSimilarity);
    }

    private void Score(
        string target,
        Span<int> destination,
        int scoreCutoff,
        int scoreHint,
        Func<IntegerDistanceScorer, IntegerScoreFunction> scorerSelector)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (destination.Length < scorers.Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int i = 0; i < scorers.Count; i++)
        {
            IntegerScoreFunction scorer = scorerSelector(scorers[i]);
            destination[i] = scorer(target, scoreCutoff, scoreHint);
        }
    }

    private void ScoreSimilarity(
        string target,
        Span<int> destination,
        int scoreCutoff,
        int scoreHint,
        Func<IntegerDistanceScorer, IntegerScoreFunction> scorerSelector)
    {
        ArgumentNullException.ThrowIfNull(target);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        if (destination.Length < scorers.Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int i = 0; i < scorers.Count; i++)
        {
            IntegerScoreFunction scorer = scorerSelector(scorers[i]);
            destination[i] = scorer(target, 0, scoreHint);
        }

        SimdSupport.ApplySimilarityCutoff(destination[..scorers.Count], scoreCutoff);
    }

    private void Score(
        string target,
        Span<double> destination,
        double scoreCutoff,
        double scoreHint,
        Func<IntegerDistanceScorer, NormalizedScoreFunction> scorerSelector)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (destination.Length < scorers.Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int i = 0; i < scorers.Count; i++)
        {
            NormalizedScoreFunction scorer = scorerSelector(scorers[i]);
            destination[i] = scorer(target, scoreCutoff, scoreHint);
        }
    }
}

internal sealed class MultiDoubleDistanceScorerCore
{
    private readonly Func<string, DoubleDistanceScorer> scorerFactory;
    private readonly List<DoubleDistanceScorer> scorers;
    private readonly bool vectorizeSimilarityCutoff;

    public MultiDoubleDistanceScorerCore(
        int capacity,
        Func<string, DoubleDistanceScorer> scorerFactory,
        bool vectorizeSimilarityCutoff = false)
    {
        ArgumentNullException.ThrowIfNull(scorerFactory);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        this.scorerFactory = scorerFactory;
        this.vectorizeSimilarityCutoff = vectorizeSimilarityCutoff;
        scorers = new List<DoubleDistanceScorer>(capacity);
    }

    public MultiDoubleDistanceScorerCore(
        IEnumerable<string> sources,
        Func<string, DoubleDistanceScorer> scorerFactory,
        bool vectorizeSimilarityCutoff = false)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(scorerFactory);

        this.scorerFactory = scorerFactory;
        this.vectorizeSimilarityCutoff = vectorizeSimilarityCutoff;
        ICollection<string>? sourceCollection = sources as ICollection<string>;
        scorers = new List<DoubleDistanceScorer>(sourceCollection?.Count ?? 0);

        foreach (string source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null values.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => scorers.Count;

    public void Insert(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        scorers.Add(scorerFactory(source));
    }

    public double[] Distances(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        Distances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.Distance);
    }

    public double[] Similarities(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        if (vectorizeSimilarityCutoff)
        {
            ScoreSimilarity(target, destination, scoreCutoff, scoreHint, static scorer => scorer.Similarity);
        }
        else
        {
            Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.Similarity);
        }
    }

    public double[] NormalizedDistances(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        NormalizedDistances(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.NormalizedDistance);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        NormalizedSimilarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        if (vectorizeSimilarityCutoff)
        {
            ScoreSimilarity(target, destination, scoreCutoff, scoreHint, static scorer => scorer.NormalizedSimilarity);
        }
        else
        {
            Score(target, destination, scoreCutoff, scoreHint, static scorer => scorer.NormalizedSimilarity);
        }
    }

    private void Score(
        string target,
        Span<double> destination,
        double scoreCutoff,
        double scoreHint,
        Func<DoubleDistanceScorer, DoubleScoreFunction> scorerSelector)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (destination.Length < scorers.Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int i = 0; i < scorers.Count; i++)
        {
            DoubleScoreFunction scorer = scorerSelector(scorers[i]);
            destination[i] = scorer(target, scoreCutoff, scoreHint);
        }
    }

    private void ScoreSimilarity(
        string target,
        Span<double> destination,
        double scoreCutoff,
        double scoreHint,
        Func<DoubleDistanceScorer, DoubleScoreFunction> scorerSelector)
    {
        ArgumentNullException.ThrowIfNull(target);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        if (destination.Length < scorers.Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int i = 0; i < scorers.Count; i++)
        {
            DoubleScoreFunction scorer = scorerSelector(scorers[i]);
            destination[i] = scorer(target, 0.0, scoreHint);
        }

        SimdSupport.ApplySimilarityCutoff(destination[..scorers.Count], scoreCutoff);
    }
}
