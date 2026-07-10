using RapidFuzz.Internal;
using RapidFuzz.Distance;

namespace RapidFuzz;

public sealed class CachedRatio
{
    private readonly CachedIndel indel;

    public CachedRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        indel = new CachedIndel(source);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        if (scoreCutoff > 100.0)
        {
            return 0.0;
        }

        double score = indel.NormalizedSimilarity(target) * 100.0;
        return score >= scoreCutoff ? score : 0.0;
    }
}

public sealed class CachedPartialRatio
{
    private readonly string source;

    public CachedPartialRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Fuzz.PartialRatio(source, target, scoreCutoff);
    }

    public ScoreAlignment Alignment(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Fuzz.PartialRatioAlignment(source, target, scoreCutoff);
    }
}

public sealed class CachedRatio<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;

    public CachedRatio(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
    }

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        return Fuzz.Ratio<T>(source, target, scoreCutoff);
    }
}

public sealed class CachedPartialRatio<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;

    public CachedPartialRatio(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
    }

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        return Fuzz.PartialRatio<T>(source, target, scoreCutoff);
    }

    public ScoreAlignment Alignment(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        return Fuzz.PartialRatioAlignment<T>(source, target, scoreCutoff);
    }
}

public sealed class CachedTokenSortRatio
{
    private readonly CachedRatio ratio;

    public CachedTokenSortRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        TokenizedString sourceTokens = StringHelpers.CreateTokenizedString(source);
        ratio = new CachedRatio(sourceTokens.Sorted);
    }

    public CachedTokenSortRatio(ReadOnlySpan<char> source)
    {
        TokenizedString sourceTokens = StringHelpers.CreateTokenizedString(source);
        ratio = new CachedRatio(sourceTokens.Sorted);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return ratio.Similarity(targetTokens.Sorted, scoreCutoff);
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return ratio.Similarity(targetTokens.Sorted, scoreCutoff);
    }
}

public sealed class CachedPartialTokenSortRatio
{
    private readonly TokenizedString sourceTokens;

    public CachedPartialTokenSortRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        sourceTokens = StringHelpers.CreateTokenizedString(source);
    }

    public CachedPartialTokenSortRatio(ReadOnlySpan<char> source)
    {
        sourceTokens = StringHelpers.CreateTokenizedString(source);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return Fuzz.PartialTokenSortRatio(sourceTokens, targetTokens, scoreCutoff);
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return Fuzz.PartialTokenSortRatio(sourceTokens, targetTokens, scoreCutoff);
    }
}

public sealed class CachedTokenSetRatio
{
    private readonly TokenizedString sourceTokens;

    public CachedTokenSetRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        sourceTokens = StringHelpers.CreateTokenizedString(source);
    }

    public CachedTokenSetRatio(ReadOnlySpan<char> source)
    {
        sourceTokens = StringHelpers.CreateTokenizedString(source);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return Fuzz.TokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return Fuzz.TokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
    }
}

public sealed class CachedPartialTokenSetRatio
{
    private readonly TokenizedString sourceTokens;

    public CachedPartialTokenSetRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        sourceTokens = StringHelpers.CreateTokenizedString(source);
    }

    public CachedPartialTokenSetRatio(ReadOnlySpan<char> source)
    {
        sourceTokens = StringHelpers.CreateTokenizedString(source);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return Fuzz.PartialTokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        return Fuzz.PartialTokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
    }
}

public sealed class CachedTokenRatio
{
    private readonly TokenizedString sourceTokens;
    private readonly CachedRatio sortedRatio;

    public CachedTokenRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        sourceTokens = StringHelpers.CreateTokenizedString(source);
        sortedRatio = new CachedRatio(sourceTokens.Sorted);
    }

    public CachedTokenRatio(ReadOnlySpan<char> source)
    {
        sourceTokens = StringHelpers.CreateTokenizedString(source);
        sortedRatio = new CachedRatio(sourceTokens.Sorted);
    }

    internal CachedTokenRatio(TokenizedString sourceTokens)
    {
        this.sourceTokens = sourceTokens;
        sortedRatio = new CachedRatio(sourceTokens.Sorted);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        double tokenSetScore = Fuzz.TokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
        double tokenSortScore = sortedRatio.Similarity(targetTokens.Sorted, scoreCutoff);
        double score = Math.Max(tokenSetScore, tokenSortScore);
        return score >= scoreCutoff ? score : 0.0;
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        double tokenSetScore = Fuzz.TokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
        double tokenSortScore = sortedRatio.Similarity(targetTokens.Sorted, scoreCutoff);
        double score = Math.Max(tokenSetScore, tokenSortScore);
        return score >= scoreCutoff ? score : 0.0;
    }
}

public sealed class CachedPartialTokenRatio
{
    private readonly TokenizedString sourceTokens;
    private readonly CachedPartialRatio partialTokenSortRatio;

    public CachedPartialTokenRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        sourceTokens = StringHelpers.CreateTokenizedString(source);
        partialTokenSortRatio = new CachedPartialRatio(sourceTokens.Sorted);
    }

    public CachedPartialTokenRatio(ReadOnlySpan<char> source)
    {
        sourceTokens = StringHelpers.CreateTokenizedString(source);
        partialTokenSortRatio = new CachedPartialRatio(sourceTokens.Sorted);
    }

    internal CachedPartialTokenRatio(TokenizedString sourceTokens)
    {
        this.sourceTokens = sourceTokens;
        partialTokenSortRatio = new CachedPartialRatio(sourceTokens.Sorted);
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        double tokenSetScore = Fuzz.PartialTokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
        double tokenSortScore = partialTokenSortRatio.Similarity(targetTokens.Sorted, scoreCutoff, scoreHint);
        double score = Math.Max(tokenSetScore, tokenSortScore);
        return score >= scoreCutoff ? score : 0.0;
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        TokenizedString targetTokens = StringHelpers.CreateTokenizedString(target);
        double tokenSetScore = Fuzz.PartialTokenSetRatio(sourceTokens.TokenSet, targetTokens.TokenSet, scoreCutoff);
        double tokenSortScore = partialTokenSortRatio.Similarity(targetTokens.Sorted, scoreCutoff, scoreHint);
        double score = Math.Max(tokenSetScore, tokenSortScore);
        return score >= scoreCutoff ? score : 0.0;
    }
}

public sealed class CachedQRatio
{
    private readonly string source;
    private readonly CachedRatio ratio;

    public CachedQRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
        ratio = new CachedRatio(source);
    }

    public CachedQRatio(ReadOnlySpan<char> source)
        : this(source.ToString())
    {
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        Fuzz.ValidateScoreCutoff(scoreCutoff);

        if (source.Length == 0 || target.Length == 0)
        {
            return 0.0;
        }

        return ratio.Similarity(target, scoreCutoff, scoreHint);
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        if (source.Length == 0 || target.IsEmpty)
        {
            return 0.0;
        }

        return Fuzz.Ratio(source.AsSpan(), target, scoreCutoff);
    }
}

public sealed class CachedQRatio<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;
    private readonly CachedRatio<T> ratio;

    public CachedQRatio(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
        ratio = new CachedRatio<T>(this.source);
    }

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        if (source.Length == 0 || target.IsEmpty)
        {
            return 0.0;
        }

        return ratio.Similarity(target, scoreCutoff, scoreHint);
    }
}

public sealed class CachedWRatio
{
    private readonly string source;
    private readonly CachedRatio ratio;
    private readonly CachedPartialRatio partialRatio;
    private readonly CachedTokenRatio tokenRatio;
    private readonly CachedPartialTokenRatio partialTokenRatio;

    public CachedWRatio(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
        TokenizedString sourceTokens = StringHelpers.CreateTokenizedString(source);
        ratio = new CachedRatio(source);
        partialRatio = new CachedPartialRatio(source);
        tokenRatio = new CachedTokenRatio(sourceTokens);
        partialTokenRatio = new CachedPartialTokenRatio(sourceTokens);
    }

    public CachedWRatio(ReadOnlySpan<char> source)
        : this(source.ToString())
    {
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        Fuzz.ValidateScoreCutoff(scoreCutoff);
        Fuzz.ValidateScoreCutoff(scoreHint);

        if (source.Length == 0 || target.Length == 0)
        {
            return 0.0;
        }

        double baseScore = ratio.Similarity(target, scoreCutoff);
        double lengthRatio = (double)Math.Max(source.Length, target.Length) / Math.Min(source.Length, target.Length);
        double score;

        if (lengthRatio < 1.5)
        {
            score = Math.Max(baseScore, tokenRatio.Similarity(target, scoreCutoff) * 0.95);
        }
        else
        {
            double partialScale = lengthRatio > 8.0 ? 0.6 : 0.9;
            score = Math.Max(
                baseScore,
                Math.Max(
                    partialRatio.Similarity(target, scoreCutoff) * partialScale,
                    partialTokenRatio.Similarity(target, scoreCutoff) * partialScale * 0.95));
        }

        return score >= scoreCutoff ? score : 0.0;
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Similarity(target.ToString(), scoreCutoff, scoreHint);
    }
}
