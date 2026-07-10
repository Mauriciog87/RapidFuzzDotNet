namespace RapidFuzz.Experimental;

using RapidFuzz.Distance.Experimental;
using RapidFuzz.Internal;

internal sealed class TextBatchRatioCore
{
    private readonly GenericBatchIntegerMetricCore<char> core;
    private readonly bool quick;
    private readonly bool sortTokens;

    public TextBatchRatioCore(int capacity, bool quick, bool sortTokens)
    {
        core = new GenericBatchIntegerMetricCore<char>(capacity, GenericBatchMetric.Indel);
        this.quick = quick;
        this.sortTokens = sortTokens;
    }

    public TextBatchRatioCore(IEnumerable<string> sources, bool quick, bool sortTokens)
    {
        ArgumentNullException.ThrowIfNull(sources);

        ICollection<string>? sourceCollection = sources as ICollection<string>;
        core = new GenericBatchIntegerMetricCore<char>(sourceCollection?.Count ?? 0, GenericBatchMetric.Indel);
        this.quick = quick;
        this.sortTokens = sortTokens;

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

        string prepared = sortTokens ? StringHelpers.CreateTokenizedString(source).Sorted : source;
        core.Insert(prepared.AsSpan());
    }

    public double[] Similarities(string target, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);

        string prepared = sortTokens ? StringHelpers.CreateTokenizedString(target).Sorted : target;
        return core.Ratios(prepared.AsSpan(), scoreCutoff, scoreHint, quick);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);

        string prepared = sortTokens ? StringHelpers.CreateTokenizedString(target).Sorted : target;
        core.Ratios(prepared.AsSpan(), destination, scoreCutoff, scoreHint, quick);
    }
}
