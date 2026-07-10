using RapidFuzz.Distance.Experimental;

namespace RapidFuzz.Experimental;

public sealed partial class MultiRatio<T>
{
    public double[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return CrossTypeMultiCore.Ratios(core, target, comparer, false, scoreCutoff, scoreHint);
    }

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        CrossTypeMultiCore.Ratios(core, target, destination, comparer, false, scoreCutoff, scoreHint);
    }
}

public sealed partial class MultiQRatio<T>
{
    public double[] Similarities<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return CrossTypeMultiCore.Ratios(core, target, comparer, true, scoreCutoff, scoreHint);
    }

    public void Similarities<TTarget>(ReadOnlySpan<TTarget> target, Span<double> destination, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        CrossTypeMultiCore.Ratios(core, target, destination, comparer, true, scoreCutoff, scoreHint);
    }
}
