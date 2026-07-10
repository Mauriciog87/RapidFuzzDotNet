namespace RapidFuzz.Distance.Experimental;

public sealed partial class MultiLevenshtein<T>
{
    public int[] Distances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0)
        where TTarget : notnull
    {
        int[] results = new int[Count];
        Distances(target, results, comparer, scoreCutoff, scoreHint);
        return results;
    }

    public void Distances<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0)
        where TTarget : notnull
    {
        ValidateCrossTypeDestination(destination.Length);
        ArgumentNullException.ThrowIfNull(comparer);

        for (int index = 0; index < Count; index++)
        {
            destination[index] = Levenshtein.Distance(GetCrossTypeSource(index), target, comparer, CrossTypeWeights, scoreCutoff, scoreHint);
        }
    }

    public int[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        int[] results = new int[Count];
        Similarities(target, results, comparer, scoreCutoff, scoreHint);
        return results;
    }

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        ValidateCrossTypeDestination(destination.Length);
        ArgumentNullException.ThrowIfNull(comparer);

        for (int index = 0; index < Count; index++)
        {
            destination[index] = Levenshtein.Similarity(GetCrossTypeSource(index), target, comparer, CrossTypeWeights, scoreCutoff, scoreHint);
        }
    }

    public double[] NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        double[] results = new double[Count];
        NormalizedDistances(target, results, comparer, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        ValidateCrossTypeDestination(destination.Length);
        ArgumentNullException.ThrowIfNull(comparer);

        for (int index = 0; index < Count; index++)
        {
            destination[index] = Levenshtein.NormalizedDistance(GetCrossTypeSource(index), target, comparer, CrossTypeWeights, scoreCutoff, scoreHint);
        }
    }

    public double[] NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        double[] results = new double[Count];
        NormalizedSimilarities(target, results, comparer, scoreCutoff, scoreHint);
        return results;
    }

    public void NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        ValidateCrossTypeDestination(destination.Length);
        ArgumentNullException.ThrowIfNull(comparer);

        for (int index = 0; index < Count; index++)
        {
            destination[index] = Levenshtein.NormalizedSimilarity(GetCrossTypeSource(index), target, comparer, CrossTypeWeights, scoreCutoff, scoreHint);
        }
    }

    private void ValidateCrossTypeDestination(int destination)
    {
        if (destination < Count)
        {
            throw new ArgumentException("The destination span is smaller than the scorer count.", nameof(destination));
        }
    }
}

public sealed partial class MultiIndel<T>
{
    public int[] Distances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerDistances(core, target, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public void Distances<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerDistances(core, target, destination, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public int[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerSimilarities(core, target, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerSimilarities(core, target, destination, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public double[] NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedDistances(core, target, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public void NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedDistances(core, target, destination, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedSimilarities(core, target, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);

    public void NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedSimilarities(core, target, destination, comparer, CrossTypeIntegerMetric.Indel, scoreCutoff, scoreHint);
}

public sealed partial class MultiLcsSeq<T>
{
    public int[] Distances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerDistances(core, target, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public void Distances<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerDistances(core, target, destination, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public int[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerSimilarities(core, target, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerSimilarities(core, target, destination, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public double[] NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedDistances(core, target, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public void NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedDistances(core, target, destination, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedSimilarities(core, target, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);

    public void NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedSimilarities(core, target, destination, comparer, CrossTypeIntegerMetric.Lcs, scoreCutoff, scoreHint);
}

public sealed partial class MultiOsa<T>
{
    public int[] Distances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerDistances(core, target, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public void Distances<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerDistances(core, target, destination, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public int[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerSimilarities(core, target, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<int> destination, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerSimilarities(core, target, destination, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public double[] NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedDistances(core, target, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public void NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedDistances(core, target, destination, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedSimilarities(core, target, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);

    public void NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.IntegerNormalizedSimilarities(core, target, destination, comparer, CrossTypeIntegerMetric.Osa, scoreCutoff, scoreHint);
}

public sealed partial class MultiJaro<T>
{
    public double[] Distances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, comparer, CrossTypeDoubleScore.JaroDistance, 0.0, scoreCutoff, scoreHint);

    public void Distances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, destination, comparer, CrossTypeDoubleScore.JaroDistance, 0.0, scoreCutoff, scoreHint);

    public double[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, comparer, CrossTypeDoubleScore.JaroSimilarity, 0.0, scoreCutoff, scoreHint);

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, destination, comparer, CrossTypeDoubleScore.JaroSimilarity, 0.0, scoreCutoff, scoreHint);

    public double[] NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        Distances(target, comparer, scoreCutoff, scoreHint);

    public void NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        Distances(target, destination, comparer, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        Similarities(target, comparer, scoreCutoff, scoreHint);

    public void NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        Similarities(target, destination, comparer, scoreCutoff, scoreHint);
}

public sealed partial class MultiJaroWinkler<T>
{
    public double[] Distances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, comparer, CrossTypeDoubleScore.JaroWinklerDistance, prefixWeight, scoreCutoff, scoreHint);

    public void Distances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, destination, comparer, CrossTypeDoubleScore.JaroWinklerDistance, prefixWeight, scoreCutoff, scoreHint);

    public double[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, comparer, CrossTypeDoubleScore.JaroWinklerSimilarity, prefixWeight, scoreCutoff, scoreHint);

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        CrossTypeMultiCore.JaroScores(core, target, destination, comparer, CrossTypeDoubleScore.JaroWinklerSimilarity, prefixWeight, scoreCutoff, scoreHint);

    public double[] NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        Distances(target, comparer, scoreCutoff, scoreHint);

    public void NormalizedDistances<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0) where TTarget : notnull =>
        Distances(target, destination, comparer, scoreCutoff, scoreHint);

    public double[] NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        Similarities(target, comparer, scoreCutoff, scoreHint);

    public void NormalizedSimilarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0) where TTarget : notnull =>
        Similarities(target, destination, comparer, scoreCutoff, scoreHint);
}
