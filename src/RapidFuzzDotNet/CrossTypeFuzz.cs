using RapidFuzz.Distance;

namespace RapidFuzz;

public static partial class Fuzz
{
    public static double Ratio<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        ValidateScoreCutoff(scoreCutoff);
        double score = Indel.NormalizedSimilarity(first, second, comparer) * 100.0;
        return score >= scoreCutoff ? score : 0.0;
    }

    public static double QRatio<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            return 0.0;
        }

        return Ratio(first, second, comparer, scoreCutoff);
    }

    public static double PartialRatio<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        return PartialRatioAlignment(first, second, comparer, scoreCutoff).Score;
    }

    public static ScoreAlignment PartialRatioAlignment<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff = 0.0)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            double emptyScore = first.IsEmpty && second.IsEmpty ? 100.0 : 0.0;
            return new ScoreAlignment(emptyScore >= scoreCutoff ? emptyScore : 0.0, 0, first.Length, 0, second.Length);
        }

        double bestScore = 0.0;
        int sourceStart = 0;
        int sourceEnd = first.Length;
        int destinationStart = 0;
        int destinationEnd = second.Length;

        if (first.Length <= second.Length)
        {
            int maximumStart = second.Length - first.Length;

            for (int start = 0; start <= maximumStart; start++)
            {
                double score = Ratio(first, second.Slice(start, first.Length), comparer, Math.Max(scoreCutoff, bestScore));

                if (score > bestScore)
                {
                    bestScore = score;
                    destinationStart = start;
                    destinationEnd = start + first.Length;
                }

                if (bestScore == 100.0)
                {
                    break;
                }
            }

            for (int length = 1; length < first.Length && bestScore < 100.0; length++)
            {
                double leading = Ratio(first, second[..length], comparer, Math.Max(scoreCutoff, bestScore));

                if (leading > bestScore)
                {
                    bestScore = leading;
                    destinationStart = 0;
                    destinationEnd = length;
                }

                int trailingStart = second.Length - length;
                double trailing = Ratio(first, second[trailingStart..], comparer, Math.Max(scoreCutoff, bestScore));

                if (trailing > bestScore)
                {
                    bestScore = trailing;
                    destinationStart = trailingStart;
                    destinationEnd = second.Length;
                }
            }

            if (first.Length == second.Length)
            {
                for (int length = 1; length < second.Length && bestScore < 100.0; length++)
                {
                    double leading = Ratio(first[..length], second, comparer, Math.Max(scoreCutoff, bestScore));

                    if (leading > bestScore)
                    {
                        bestScore = leading;
                        sourceStart = 0;
                        sourceEnd = length;
                        destinationStart = 0;
                        destinationEnd = second.Length;
                    }

                    int trailingStart = first.Length - length;
                    double trailing = Ratio(first[trailingStart..], second, comparer, Math.Max(scoreCutoff, bestScore));

                    if (trailing > bestScore)
                    {
                        bestScore = trailing;
                        sourceStart = trailingStart;
                        sourceEnd = first.Length;
                        destinationStart = 0;
                        destinationEnd = second.Length;
                    }
                }
            }
        }
        else
        {
            int maximumStart = first.Length - second.Length;

            for (int start = 0; start <= maximumStart; start++)
            {
                double score = Ratio(first.Slice(start, second.Length), second, comparer, Math.Max(scoreCutoff, bestScore));

                if (score > bestScore)
                {
                    bestScore = score;
                    sourceStart = start;
                    sourceEnd = start + second.Length;
                }

                if (bestScore == 100.0)
                {
                    break;
                }
            }

            for (int length = 1; length < second.Length && bestScore < 100.0; length++)
            {
                double leading = Ratio(first[..length], second, comparer, Math.Max(scoreCutoff, bestScore));

                if (leading > bestScore)
                {
                    bestScore = leading;
                    sourceStart = 0;
                    sourceEnd = length;
                }

                int trailingStart = first.Length - length;
                double trailing = Ratio(first[trailingStart..], second, comparer, Math.Max(scoreCutoff, bestScore));

                if (trailing > bestScore)
                {
                    bestScore = trailing;
                    sourceStart = trailingStart;
                    sourceEnd = first.Length;
                }
            }
        }

        double cutoffScore = bestScore >= scoreCutoff ? bestScore : 0.0;
        return new ScoreAlignment(cutoffScore, sourceStart, sourceEnd, destinationStart, destinationEnd);
    }
}
