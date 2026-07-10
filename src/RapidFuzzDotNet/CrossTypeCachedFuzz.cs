namespace RapidFuzz;

public sealed partial class CachedRatio<T>
{
    public double Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Fuzz.Ratio(source, target, comparer, scoreCutoff);
    }
}

public sealed partial class CachedPartialRatio<T>
{
    public double Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Fuzz.PartialRatio(source, target, comparer, scoreCutoff);
    }

    public ScoreAlignment Alignment<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0)
        where TTarget : notnull
    {
        return Fuzz.PartialRatioAlignment(source, target, comparer, scoreCutoff);
    }
}

public sealed partial class CachedQRatio<T>
{
    public double Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Fuzz.QRatio(source, target, comparer, scoreCutoff);
    }
}
