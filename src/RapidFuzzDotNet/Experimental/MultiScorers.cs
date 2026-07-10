namespace RapidFuzz.Experimental;

using RapidFuzz;

public sealed class MultiRatio
{
    private readonly TextBatchRatioCore core;

    public MultiRatio(int capacity)
    {
        core = new TextBatchRatioCore(capacity, false, false);
    }

    public MultiRatio(IEnumerable<string> sources)
    {
        core = new TextBatchRatioCore(sources, false, false);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }
}

public sealed class MultiPartialRatio
{
    private readonly MultiScorerCore core;

    public MultiPartialRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiPartialRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedPartialRatio scorer = new(source);
        return scorer.Similarity;
    }
}

public sealed class MultiTokenSortRatio
{
    private readonly TextBatchRatioCore core;

    public MultiTokenSortRatio(int capacity)
    {
        core = new TextBatchRatioCore(capacity, false, true);
    }

    public MultiTokenSortRatio(IEnumerable<string> sources)
    {
        core = new TextBatchRatioCore(sources, false, true);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }
}

public sealed class MultiPartialTokenSortRatio
{
    private readonly MultiScorerCore core;

    public MultiPartialTokenSortRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiPartialTokenSortRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedPartialTokenSortRatio scorer = new(source);
        return scorer.Similarity;
    }
}

public sealed class MultiTokenSetRatio
{
    private readonly MultiScorerCore core;

    public MultiTokenSetRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiTokenSetRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedTokenSetRatio scorer = new(source);
        return scorer.Similarity;
    }
}

public sealed class MultiPartialTokenSetRatio
{
    private readonly MultiScorerCore core;

    public MultiPartialTokenSetRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiPartialTokenSetRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedPartialTokenSetRatio scorer = new(source);
        return scorer.Similarity;
    }
}

public sealed class MultiTokenRatio
{
    private readonly MultiScorerCore core;

    public MultiTokenRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiTokenRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedTokenRatio scorer = new(source);
        return scorer.Similarity;
    }
}

public sealed class MultiPartialTokenRatio
{
    private readonly MultiScorerCore core;

    public MultiPartialTokenRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiPartialTokenRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedPartialTokenRatio scorer = new(source);
        return scorer.Similarity;
    }
}

public sealed class MultiQRatio
{
    private readonly TextBatchRatioCore core;

    public MultiQRatio(int capacity)
    {
        core = new TextBatchRatioCore(capacity, true, false);
    }

    public MultiQRatio(IEnumerable<string> sources)
    {
        core = new TextBatchRatioCore(sources, true, false);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }
}

public sealed class MultiWRatio
{
    private readonly MultiScorerCore core;

    public MultiWRatio(int capacity)
    {
        core = new MultiScorerCore(capacity, CreateScorer);
    }

    public MultiWRatio(IEnumerable<string> sources)
    {
        core = new MultiScorerCore(sources, CreateScorer);
    }

    public int Count => core.Count;

    public void Insert(string source)
    {
        core.Insert(source);
    }

    public double[] Similarities(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return core.Similarities(target, scoreCutoff, scoreHint);
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        core.Similarities(target, destination, scoreCutoff, scoreHint);
    }

    private static Func<string, double, double, double> CreateScorer(string source)
    {
        CachedWRatio scorer = new(source);
        return scorer.Similarity;
    }
}

internal sealed class MultiScorerCore
{
    private readonly Func<string, Func<string, double, double, double>> scorerFactory;
    private readonly List<Func<string, double, double, double>> scorers;

    public MultiScorerCore(
        int capacity,
        Func<string, Func<string, double, double, double>> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(scorerFactory);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative.");
        }

        this.scorerFactory = scorerFactory;
        scorers = new List<Func<string, double, double, double>>(capacity);
    }

    public MultiScorerCore(
        IEnumerable<string> sources,
        Func<string, Func<string, double, double, double>> scorerFactory)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(scorerFactory);

        this.scorerFactory = scorerFactory;
        ICollection<string>? sourceCollection = sources as ICollection<string>;
        scorers = new List<Func<string, double, double, double>>(sourceCollection?.Count ?? 0);

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

    public double[] Similarities(string target, double scoreCutoff, double scoreHint)
    {
        double[] results = new double[scorers.Count];
        Similarities(target, results, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities(string target, Span<double> destination, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (destination.Length < scorers.Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }

        for (int i = 0; i < scorers.Count; i++)
        {
            destination[i] = scorers[i](target, scoreCutoff, scoreHint);
        }
    }
}
