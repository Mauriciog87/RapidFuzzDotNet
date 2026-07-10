namespace RapidFuzz.Distance;

public sealed partial class CachedLevenshtein<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return Levenshtein.Distance(source, target, comparer, weights, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return Levenshtein.Similarity(source, target, comparer, weights, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Levenshtein.NormalizedDistance(source, target, comparer, weights, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Levenshtein.NormalizedSimilarity(source, target, comparer, weights, scoreCutoff, scoreHint);
    }

    public EditOperations Editops<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return Levenshtein.Editops(source, target, comparer);
    }

    public Opcodes Opcodes<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return Levenshtein.Opcodes(source, target, comparer);
    }
}

public sealed partial class CachedIndel<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return Indel.Distance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return Indel.Similarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Indel.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Indel.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public EditOperations Editops<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return Indel.Editops(source, target, comparer);
    }

    public Opcodes Opcodes<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return Indel.Opcodes(source, target, comparer);
    }
}

public sealed partial class CachedHamming<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return Hamming.Distance(source, target, comparer, pad, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return Hamming.Similarity(source, target, comparer, pad, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Hamming.NormalizedDistance(source, target, comparer, pad, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Hamming.NormalizedSimilarity(source, target, comparer, pad, scoreCutoff, scoreHint);
    }

    public EditOperations Editops<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return Hamming.Editops(source, target, comparer, pad);
    }

    public Opcodes Opcodes<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return Hamming.Opcodes(source, target, comparer, pad);
    }
}

public sealed partial class CachedLcsSeq<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return LcsSeq.Distance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return LcsSeq.Similarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return LcsSeq.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return LcsSeq.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public EditOperations Editops<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return LcsSeq.Editops(source, target, comparer);
    }

    public Opcodes Opcodes<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer)
        where TTarget : notnull
    {
        return LcsSeq.Opcodes(source, target, comparer);
    }
}

public sealed partial class CachedOsa<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return Osa.Distance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return Osa.Similarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Osa.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Osa.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedDamerauLevenshtein<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return DamerauLevenshtein.Distance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return DamerauLevenshtein.Similarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return DamerauLevenshtein.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return DamerauLevenshtein.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedJaro<T>
{
    public double Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Jaro.Distance(pattern.Source, target, comparer, scoreCutoff, scoreHint);
    }

    public double Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Jaro.Similarity(pattern.Source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Distance(target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Similarity(target, comparer, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedJaroWinkler<T>
{
    public double Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return JaroWinkler.Distance(pattern.Source, target, comparer, prefixWeight, scoreCutoff, scoreHint);
    }

    public double Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return JaroWinkler.Similarity(pattern.Source, target, comparer, prefixWeight, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Distance(target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Similarity(target, comparer, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedPrefix<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return Prefix.Distance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return Prefix.Similarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Prefix.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Prefix.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedPostfix<T>
{
    public int Distance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = int.MaxValue, int scoreHint = int.MaxValue)
        where TTarget : notnull
    {
        return Postfix.Distance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public int Similarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, int scoreCutoff = 0, int scoreHint = 0)
        where TTarget : notnull
    {
        return Postfix.Similarity(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 1.0, double scoreHint = 1.0)
        where TTarget : notnull
    {
        return Postfix.NormalizedDistance(source, target, comparer, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity<TTarget>(ReadOnlySpan<TTarget> target, ISequenceEqualityComparer<T, TTarget> comparer, double scoreCutoff = 0.0, double scoreHint = 0.0)
        where TTarget : notnull
    {
        return Postfix.NormalizedSimilarity(source, target, comparer, scoreCutoff, scoreHint);
    }
}
