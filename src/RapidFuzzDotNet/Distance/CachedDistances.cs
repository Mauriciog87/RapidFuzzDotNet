using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public sealed class CachedLevenshtein
{
    private readonly string source;
    private readonly LevenshteinWeights weights;
    private readonly PatternMatchVector? defaultSmallPattern;
    private readonly BlockPatternMatchVector? defaultPattern;

    public CachedLevenshtein(string source)
        : this(source, LevenshteinWeights.Default)
    {
    }

    public CachedLevenshtein(string source, LevenshteinWeights weights)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
        this.weights = weights;

        if (weights == LevenshteinWeights.Default)
        {
            if (source.Length <= PatternMatchVector.MaximumPatternLength)
            {
                defaultSmallPattern = new PatternMatchVector(source);
            }
            else
            {
                defaultPattern = new BlockPatternMatchVector(source);
            }
        }
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (defaultSmallPattern.HasValue || defaultPattern is not null)
        {
            return Distance(target.AsSpan(), scoreCutoff, scoreHint);
        }

        return Levenshtein.Distance(source, target, weights, scoreCutoff, scoreHint);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (defaultSmallPattern.HasValue || defaultPattern is not null)
        {
            DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
            DistanceHelpers.ValidateScoreHint(scoreHint);

            int maximum = Math.Max(source.Length, target.Length);
            int distanceCutoff = maximum - scoreCutoff;
            int distanceHint = Math.Max(0, maximum - scoreHint);
            int distance = Distance(target.AsSpan(), distanceCutoff, distanceHint);

            if (distance > distanceCutoff)
            {
                return 0;
            }

            return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
        }

        return Levenshtein.Similarity(source, target, weights, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (defaultSmallPattern.HasValue || defaultPattern is not null)
        {
            DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
            DistanceHelpers.ValidateNormalizedHint(scoreHint);

            int maximum = Math.Max(source.Length, target.Length);
            int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
            int distanceHint = (int)Math.Floor(maximum * scoreHint);
            int distance = Distance(target.AsSpan(), distanceCutoff, distanceHint);
            return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
        }

        return Levenshtein.NormalizedDistance(source, target, weights, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (defaultSmallPattern.HasValue || defaultPattern is not null)
        {
            DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
            DistanceHelpers.ValidateNormalizedHint(scoreHint);

            int maximum = Math.Max(source.Length, target.Length);
            int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
            int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
            int distance = Distance(target.AsSpan(), distanceCutoff, distanceHint);

            if (distance > distanceCutoff)
            {
                return 0.0;
            }

            return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
        }

        return Levenshtein.NormalizedSimilarity(source, target, weights, scoreCutoff, scoreHint);
    }

    public EditOperations Editops(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Levenshtein.Editops(source, target);
    }

    public Opcodes Opcodes(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Levenshtein.Opcodes(source, target);
    }

    private int Distance(ReadOnlySpan<char> target, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int effectiveCutoff = Math.Min(scoreCutoff, Math.Max(source.Length, target.Length));
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(target, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return DistanceCore(target, scoreCutoff);
    }

    private int DistanceCore(ReadOnlySpan<char> target, int scoreCutoff)
    {
        if (source.Length == 0)
        {
            return DistanceHelpers.ApplyDistanceCutoff(target.Length, scoreCutoff);
        }

        int lengthDifference = Math.Abs(source.Length - target.Length);

        if (lengthDifference > scoreCutoff)
        {
            return scoreCutoff == int.MaxValue ? lengthDifference : scoreCutoff + 1;
        }

        int distance = defaultSmallPattern.HasValue
            ? defaultSmallPattern.Value.LevenshteinDistance(source.Length, target)
            : defaultPattern!.LevenshteinDistance(target);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }
}

public sealed partial class CachedIndel
{
    private readonly string source;
    private readonly PatternMatchVector? smallPattern;
    private readonly BlockPatternMatchVector? pattern;

    public CachedIndel(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;

        if (source.Length <= PatternMatchVector.MaximumPatternLength)
        {
            smallPattern = new PatternMatchVector(source);
        }
        else
        {
            pattern = new BlockPatternMatchVector(source);
        }
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Distance(target.AsSpan(), scoreCutoff, scoreHint);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = source.Length + target.Length;
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(target.AsSpan(), distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0;
        }

        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = source.Length + target.Length;
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(target.AsSpan(), distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = source.Length + target.Length;
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(target.AsSpan(), distanceCutoff, distanceHint);

        if (distance > distanceCutoff)
        {
            return 0.0;
        }

        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public EditOperations Editops(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Indel.Editops(source, target);
    }

    public Opcodes Opcodes(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Indel.Opcodes(source, target);
    }

    internal int Distance(ReadOnlySpan<char> target, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int effectiveCutoff = Math.Min(scoreCutoff, source.Length + target.Length);
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(target, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return DistanceCore(target, scoreCutoff);
    }

    private int DistanceCore(ReadOnlySpan<char> target, int scoreCutoff)
    {
        int lcsLength = smallPattern.HasValue ? smallPattern.Value.LcsSimilarity(target) : pattern!.LcsSimilarity(target);
        int distance = source.Length + target.Length - (2 * lcsLength);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }
}

public sealed class CachedHamming
{
    private readonly string source;
    private readonly bool pad;

    public CachedHamming(string source, bool pad = true)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
        this.pad = pad;
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Hamming.Distance(source, target, pad, scoreCutoff, scoreHint);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Hamming.Similarity(source, target, pad, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Hamming.NormalizedDistance(source, target, pad, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Hamming.NormalizedSimilarity(source, target, pad, scoreCutoff, scoreHint);
    }

    public EditOperations Editops(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Hamming.Editops(source, target, pad);
    }
}

public sealed class CachedLcsSeq
{
    private readonly string source;
    private readonly PatternMatchVector? smallPattern;
    private readonly BlockPatternMatchVector? pattern;

    public CachedLcsSeq(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;

        if (source.Length <= PatternMatchVector.MaximumPatternLength)
        {
            smallPattern = new PatternMatchVector(source);
        }
        else
        {
            pattern = new BlockPatternMatchVector(source);
        }
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int effectiveCutoff = Math.Min(scoreCutoff, maximum);
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(target.AsSpan(), currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        int distance = DistanceCore(target.AsSpan(), scoreCutoff);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int similarity = SimilarityCore(target.AsSpan(), scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distance = DistanceCore(target.AsSpan(), distanceCutoff);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        int similarityCutoff = (int)Math.Ceiling(maximum * scoreCutoff);
        double similarity = (double)SimilarityCore(target.AsSpan(), similarityCutoff) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public EditOperations Editops(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return LcsSeq.Editops(source, target);
    }

    private int DistanceCore(ReadOnlySpan<char> target, int scoreCutoff)
    {
        int maximum = Math.Max(source.Length, target.Length);
        int similarityCutoff = scoreCutoff >= maximum ? 0 : maximum - scoreCutoff;
        return maximum - SimilarityCore(target, similarityCutoff);
    }

    private int SimilarityCore(ReadOnlySpan<char> target, int scoreCutoff)
    {
        return smallPattern.HasValue
            ? smallPattern.Value.LcsSimilarityUnrolled(target, scoreCutoff)
            : pattern!.LcsSimilarityBlockwise(target, scoreCutoff);
    }
}

public sealed class CachedOsa
{
    private readonly string source;
    private readonly PatternMatchVector? smallPattern;
    private readonly BlockPatternMatchVector? pattern;

    public CachedOsa(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;

        if (source.Length < PatternMatchVector.MaximumPatternLength)
        {
            smallPattern = new PatternMatchVector(source);
        }
        else
        {
            pattern = new BlockPatternMatchVector(source);
        }
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        if (source.Length == 0)
        {
            return DistanceHelpers.ApplyDistanceCutoff(target.Length, scoreCutoff);
        }

        int minimumDistance = Math.Abs(source.Length - target.Length);

        if (minimumDistance > scoreCutoff)
        {
            return DistanceHelpers.ApplyDistanceCutoff(minimumDistance, scoreCutoff);
        }

        return smallPattern.HasValue
            ? Osa.Distance(smallPattern.Value, source.Length, target.AsSpan(), scoreCutoff)
            : Osa.Distance(pattern!, source.Length, target.AsSpan(), scoreCutoff);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }
}

public sealed class CachedDamerauLevenshtein
{
    private readonly string source;

    public CachedDamerauLevenshtein(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return DamerauLevenshtein.Distance(source, target, scoreCutoff, scoreHint);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return DamerauLevenshtein.Similarity(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return DamerauLevenshtein.NormalizedDistance(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return DamerauLevenshtein.NormalizedSimilarity(source, target, scoreCutoff, scoreHint);
    }
}

public sealed class CachedJaro
{
    private readonly JaroPattern pattern;

    public CachedJaro(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        pattern = new JaroPattern(source);
    }

    public double Distance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarityCutoff = scoreCutoff >= 1.0 ? 0.0 : 1.0 - scoreCutoff;
        double distance = 1.0 - Jaro.Similarity(pattern, target.AsSpan(), similarityCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarity = Jaro.Similarity(pattern, target.AsSpan(), scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Distance(target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Similarity(target, scoreCutoff, scoreHint);
    }
}

public sealed class CachedJaroWinkler
{
    private readonly JaroPattern pattern;
    private readonly double prefixWeight;

    public CachedJaroWinkler(string source, double prefixWeight = 0.1)
    {
        ArgumentNullException.ThrowIfNull(source);
        JaroWinkler.ValidatePrefixWeight(prefixWeight);

        pattern = new JaroPattern(source);
        this.prefixWeight = prefixWeight;
    }

    public double Distance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double distance = 1.0 - JaroWinkler.Similarity(pattern, target.AsSpan(), prefixWeight, 1.0 - scoreCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public double Similarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarity = JaroWinkler.Similarity(pattern, target.AsSpan(), prefixWeight, scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Distance(target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Similarity(target, scoreCutoff, scoreHint);
    }
}

public sealed class CachedPrefix
{
    private readonly string source;

    public CachedPrefix(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Prefix.Distance(source, target, scoreCutoff, scoreHint);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Prefix.Similarity(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Prefix.NormalizedDistance(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Prefix.NormalizedSimilarity(source, target, scoreCutoff, scoreHint);
    }
}

public sealed class CachedPostfix
{
    private readonly string source;

    public CachedPostfix(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.source = source;
    }

    public int Distance(string target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Postfix.Distance(source, target, scoreCutoff, scoreHint);
    }

    public int Similarity(string target, int scoreCutoff = 0, int scoreHint = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Postfix.Similarity(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(string target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Postfix.NormalizedDistance(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(string target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        ArgumentNullException.ThrowIfNull(target);

        return Postfix.NormalizedSimilarity(source, target, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedLevenshtein<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;
    private readonly LevenshteinWeights weights;
    private readonly GenericPatternMatchVector<T>? pattern;

    public CachedLevenshtein(ReadOnlySpan<T> source)
        : this(source, LevenshteinWeights.Default)
    {
    }

    public CachedLevenshtein(ReadOnlySpan<T> source, LevenshteinWeights weights)
    {
        this.source = source.ToArray();
        this.weights = weights;

        if (weights == LevenshteinWeights.Default)
        {
            pattern = new GenericPatternMatchVector<T>(this.source);
        }
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        if (pattern is null)
        {
            return Levenshtein.Distance(source, target, weights, scoreCutoff, scoreHint);
        }

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int minimumDistance = Math.Abs(source.Length - target.Length);

        if (minimumDistance > scoreCutoff)
        {
            return DistanceHelpers.ApplyDistanceCutoff(minimumDistance, scoreCutoff);
        }

        int distance = pattern.LevenshteinDistance(target);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        if (pattern is null)
        {
            return Levenshtein.Similarity(source, target, weights, scoreCutoff, scoreHint);
        }

        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        if (pattern is null)
        {
            return Levenshtein.NormalizedDistance(source, target, weights, scoreCutoff, scoreHint);
        }

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        if (pattern is null)
        {
            return Levenshtein.NormalizedSimilarity(source, target, weights, scoreCutoff, scoreHint);
        }

        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public EditOperations Editops(ReadOnlySpan<T> target)
    {
        return Levenshtein.Editops(source, target);
    }

    public Opcodes Opcodes(ReadOnlySpan<T> target)
    {
        return Levenshtein.Opcodes(source, target);
    }
}

public sealed partial class CachedIndel<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;
    private readonly GenericPatternMatchVector<T> pattern;

    public CachedIndel(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
        pattern = new GenericPatternMatchVector<T>(this.source);
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int lcsLength = pattern.LcsSimilarity(target, 0);
        int distance = source.Length + target.Length - (2 * lcsLength);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = source.Length + target.Length;
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = source.Length + target.Length;
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = source.Length + target.Length;
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public EditOperations Editops(ReadOnlySpan<T> target)
    {
        return Indel.Editops(source, target);
    }

    public Opcodes Opcodes(ReadOnlySpan<T> target)
    {
        return Indel.Opcodes(source, target);
    }
}

public sealed partial class CachedHamming<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;
    private readonly bool pad;

    public CachedHamming(ReadOnlySpan<T> source, bool pad = true)
    {
        this.source = source.ToArray();
        this.pad = pad;
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return Hamming.Distance(source, target, pad, scoreCutoff, scoreHint);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return Hamming.Similarity(source, target, pad, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Hamming.NormalizedDistance(source, target, pad, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Hamming.NormalizedSimilarity(source, target, pad, scoreCutoff, scoreHint);
    }

    public EditOperations Editops(ReadOnlySpan<T> target)
    {
        return Hamming.Editops(source, target, pad);
    }
}

public sealed partial class CachedLcsSeq<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;
    private readonly GenericPatternMatchVector<T> pattern;

    public CachedLcsSeq(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
        pattern = new GenericPatternMatchVector<T>(this.source);
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int similarityCutoff = scoreCutoff >= maximum ? 0 : maximum - scoreCutoff;
        int distance = maximum - pattern.LcsSimilarity(target, similarityCutoff);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        return pattern.LcsSimilarity(target, scoreCutoff);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distance = Distance(target, distanceCutoff);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        int similarityCutoff = (int)Math.Ceiling(maximum * scoreCutoff);
        double similarity = (double)pattern.LcsSimilarity(target, similarityCutoff) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public EditOperations Editops(ReadOnlySpan<T> target)
    {
        return LcsSeq.Editops(source, target);
    }
}

public sealed partial class CachedOsa<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;
    private readonly GenericPatternMatchVector<T> pattern;

    public CachedOsa(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
        pattern = new GenericPatternMatchVector<T>(this.source);
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int minimumDistance = Math.Abs(source.Length - target.Length);

        if (minimumDistance > scoreCutoff)
        {
            return DistanceHelpers.ApplyDistanceCutoff(minimumDistance, scoreCutoff);
        }

        return pattern.OsaDistance(target, scoreCutoff);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(source.Length, target.Length);
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(target, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }
}

public sealed partial class CachedDamerauLevenshtein<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;

    public CachedDamerauLevenshtein(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return DamerauLevenshtein.Distance(source, target, scoreCutoff, scoreHint);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return DamerauLevenshtein.Similarity(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return DamerauLevenshtein.NormalizedDistance(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return DamerauLevenshtein.NormalizedSimilarity(source, target, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedJaro<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericJaroPattern<T> pattern;

    public CachedJaro(ReadOnlySpan<T> source)
    {
        pattern = new GenericJaroPattern<T>(source);
    }

    public double Distance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarityCutoff = scoreCutoff >= 1.0 ? 0.0 : 1.0 - scoreCutoff;
        double distance = 1.0 - pattern.Similarity(target, similarityCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        return pattern.Similarity(target, scoreCutoff);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Distance(target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Similarity(target, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedJaroWinkler<T>
    where T : notnull, IEquatable<T>
{
    private readonly GenericJaroPattern<T> pattern;
    private readonly double prefixWeight;

    public CachedJaroWinkler(ReadOnlySpan<T> source, double prefixWeight = 0.1)
    {
        JaroWinkler.ValidatePrefixWeight(prefixWeight);

        pattern = new GenericJaroPattern<T>(source);
        this.prefixWeight = prefixWeight;
    }

    public double Distance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double distance = 1.0 - pattern.WinklerSimilarity(target, prefixWeight, 1.0 - scoreCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        return pattern.WinklerSimilarity(target, prefixWeight, scoreCutoff);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Distance(target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Similarity(target, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedPrefix<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;

    public CachedPrefix(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return Prefix.Distance(source, target, scoreCutoff, scoreHint);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return Prefix.Similarity(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Prefix.NormalizedDistance(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Prefix.NormalizedSimilarity(source, target, scoreCutoff, scoreHint);
    }
}

public sealed partial class CachedPostfix<T>
    where T : notnull, IEquatable<T>
{
    private readonly T[] source;

    public CachedPostfix(ReadOnlySpan<T> source)
    {
        this.source = source.ToArray();
    }

    public int Distance(ReadOnlySpan<T> target, int scoreCutoff = int.MaxValue, int scoreHint = 0)
    {
        return Postfix.Distance(source, target, scoreCutoff, scoreHint);
    }

    public int Similarity(ReadOnlySpan<T> target, int scoreCutoff = 0, int scoreHint = 0)
    {
        return Postfix.Similarity(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedDistance(ReadOnlySpan<T> target, double scoreCutoff = 1.0, double scoreHint = 1.0)
    {
        return Postfix.NormalizedDistance(source, target, scoreCutoff, scoreHint);
    }

    public double NormalizedSimilarity(ReadOnlySpan<T> target, double scoreCutoff = 0.0, double scoreHint = 0.0)
    {
        return Postfix.NormalizedSimilarity(source, target, scoreCutoff, scoreHint);
    }
}
