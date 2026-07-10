namespace RapidFuzz.Experimental;

using RapidFuzz.Distance.Experimental;

public sealed partial class MultiRatio<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchIntegerMetricCore<T> core;

    public MultiRatio(int capacity) => core = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Indel);

    public MultiRatio(IEnumerable<T[]> sources) => core = new GenericBatchIntegerMetricCore<T>(sources, GenericBatchMetric.Indel);

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public double[] Similarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.Ratios(target, scoreCutoff, scoreHint, false);

    public void Similarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.Ratios(target, destination, scoreCutoff, scoreHint, false);
}

public sealed partial class MultiQRatio<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericBatchIntegerMetricCore<T> core;

    public MultiQRatio(int capacity) => core = new GenericBatchIntegerMetricCore<T>(capacity, GenericBatchMetric.Indel);

    public MultiQRatio(IEnumerable<T[]> sources) => core = new GenericBatchIntegerMetricCore<T>(sources, GenericBatchMetric.Indel);

    public int Count => core.Count;

    public void Insert(ReadOnlySpan<T> source) => core.Insert(source);

    public double[] Similarities(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.Ratios(target, scoreCutoff, scoreHint, true);

    public void Similarities(ReadOnlySpan<T> target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0) =>
        core.Ratios(target, destination, scoreCutoff, scoreHint, true);
}
