namespace RapidFuzz.Distance.Experimental;

internal sealed class TextBatchIntegerMetricCore
{
    private readonly GenericBatchIntegerMetricCore<char> core;

    public TextBatchIntegerMetricCore(int capacity, GenericBatchMetric metric)
    {
        core = new GenericBatchIntegerMetricCore<char>(capacity, metric);
    }

    public TextBatchIntegerMetricCore(IEnumerable<string> sources, GenericBatchMetric metric)
    {
        ArgumentNullException.ThrowIfNull(sources);

        ICollection<string>? sourceCollection = sources as ICollection<string>;
        core = new GenericBatchIntegerMetricCore<char>(sourceCollection?.Count ?? 0, metric);

        foreach (string source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("Sources cannot contain null values.", nameof(sources));
            }

            Insert(source);
        }
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        core.Insert(source.AsSpan());
    }

    public int[] Distances(string target, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        return core.Distances(target.AsSpan(), scoreCutoff, scoreHint);
    }

    public void Distances(string target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        core.Distances(target.AsSpan(), destination, scoreCutoff, scoreHint);
    }

    public int[] Similarities(string target, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        return core.Similarities(target.AsSpan(), scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<int> destination, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        core.Similarities(target.AsSpan(), destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedDistances(string target, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        return core.NormalizedDistances(target.AsSpan(), scoreCutoff, scoreHint);
    }

    public void NormalizedDistances(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        core.NormalizedDistances(target.AsSpan(), destination, scoreCutoff, scoreHint);
    }

    public double[] NormalizedSimilarities(string target, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        return core.NormalizedSimilarities(target.AsSpan(), scoreCutoff, scoreHint);
    }

    public void NormalizedSimilarities(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);
        core.NormalizedSimilarities(target.AsSpan(), destination, scoreCutoff, scoreHint);
    }
}
