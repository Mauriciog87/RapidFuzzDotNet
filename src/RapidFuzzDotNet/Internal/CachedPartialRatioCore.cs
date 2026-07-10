using System.Buffers;

namespace RapidFuzz.Internal;

internal interface ICachedRatioAdapter<T>
    where T : notnull, IEquatable<T>
{
    int Distance(ReadOnlySpan<T> target);

    double Similarity(ReadOnlySpan<T> target, double scoreCutoff);
}

internal readonly struct CachedStringRatioAdapter : ICachedRatioAdapter<char>
{
    private readonly CachedRatio ratio;

    public CachedStringRatioAdapter(CachedRatio ratio)
    {
        this.ratio = ratio;
    }

    public int Distance(ReadOnlySpan<char> target)
    {
        return ratio.Distance(target);
    }

    public double Similarity(ReadOnlySpan<char> target, double scoreCutoff)
    {
        return ratio.Similarity(target, scoreCutoff);
    }
}

internal readonly struct CachedGenericRatioAdapter<T> : ICachedRatioAdapter<T>
    where T : notnull, IEquatable<T>
{
    private readonly CachedRatio<T> ratio;

    public CachedGenericRatioAdapter(CachedRatio<T> ratio)
    {
        this.ratio = ratio;
    }

    public int Distance(ReadOnlySpan<T> target)
    {
        return ratio.Distance(target);
    }

    public double Similarity(ReadOnlySpan<T> target, double scoreCutoff)
    {
        return ratio.Similarity(target, scoreCutoff);
    }
}

internal static class CachedPartialRatioCore
{
    public static ScoreAlignment Alignment<T, TRatio>(
        ReadOnlySpan<T> source,
        ReadOnlySpan<T> target,
        TRatio ratio,
        HashSet<T> sourceSymbols,
        double scoreCutoff)
        where T : notnull, IEquatable<T>
        where TRatio : struct, ICachedRatioAdapter<T>
    {
        if (source.IsEmpty || target.IsEmpty)
        {
            double emptyScore = source.IsEmpty && target.IsEmpty ? 100.0 : 0.0;
            double cutoffScore = emptyScore >= scoreCutoff ? emptyScore : 0.0;
            return new ScoreAlignment(cutoffScore, 0, source.Length, 0, target.Length);
        }

        if (source.Length <= target.Length)
        {
            ScoreAlignment alignment = AlignmentWithShorterSource(source, target, ratio, sourceSymbols, scoreCutoff);

            if (source.Length == target.Length && alignment.Score < 100.0)
            {
                double reverseCutoff = Math.Max(scoreCutoff, alignment.Score);
                CachedRatio<T> equalLengthRatio = new(target);
                HashSet<T> equalLengthSymbols = new(target.ToArray());
                CachedGenericRatioAdapter<T> equalLengthAdapter = new(equalLengthRatio);
                ScoreAlignment equalLengthReversed = AlignmentWithShorterSource(
                    target,
                    source,
                    equalLengthAdapter,
                    equalLengthSymbols,
                    reverseCutoff);

                if (equalLengthReversed.Score > alignment.Score)
                {
                    return new ScoreAlignment(
                        equalLengthReversed.Score,
                        equalLengthReversed.DestinationStart,
                        equalLengthReversed.DestinationEnd,
                        equalLengthReversed.SourceStart,
                        equalLengthReversed.SourceEnd);
                }
            }

            return alignment;
        }

        CachedRatio<T> reversedRatio = new(target);
        HashSet<T> reversedSymbols = new(target.ToArray());
        CachedGenericRatioAdapter<T> reversedAdapter = new(reversedRatio);
        ScoreAlignment reversed = AlignmentWithShorterSource(target, source, reversedAdapter, reversedSymbols, scoreCutoff);
        return new ScoreAlignment(
            reversed.Score,
            reversed.DestinationStart,
            reversed.DestinationEnd,
            reversed.SourceStart,
            reversed.SourceEnd);
    }

    private static ScoreAlignment AlignmentWithShorterSource<T, TRatio>(
        ReadOnlySpan<T> source,
        ReadOnlySpan<T> target,
        TRatio ratio,
        HashSet<T> sourceSymbols,
        double scoreCutoff)
        where T : notnull, IEquatable<T>
        where TRatio : struct, ICachedRatioAdapter<T>
    {
        ScoreAlignment result = new(0.0, 0, source.Length, 0, source.Length);
        int lengthDifference = target.Length - source.Length;

        if (lengthDifference > 0)
        {
            ScoreAlignment equalLength = ScoreEqualLengthWindows(source, target, ratio, scoreCutoff);
            result = equalLength;

            if (result.Score == 100.0)
            {
                return result;
            }

            scoreCutoff = Math.Max(scoreCutoff, result.Score);
        }

        for (int length = 1; length < source.Length; length++)
        {
            ReadOnlySpan<T> candidate = target[..length];

            if (!sourceSymbols.Contains(candidate[^1]))
            {
                continue;
            }

            double score = ratio.Similarity(candidate, scoreCutoff);

            if (score > result.Score)
            {
                scoreCutoff = score;
                result = new ScoreAlignment(score, 0, source.Length, 0, length);

                if (score == 100.0)
                {
                    return result;
                }
            }
        }

        int trailingStart = target.Length - source.Length;

        for (int start = trailingStart; start < target.Length; start++)
        {
            ReadOnlySpan<T> candidate = target[start..];

            if (!sourceSymbols.Contains(candidate[0]))
            {
                continue;
            }

            double score = ratio.Similarity(candidate, scoreCutoff);

            if (score > result.Score)
            {
                scoreCutoff = score;
                result = new ScoreAlignment(score, 0, source.Length, start, target.Length);

                if (score == 100.0)
                {
                    return result;
                }
            }
        }

        return result.Score >= scoreCutoff
            ? result
            : new ScoreAlignment(0.0, result.SourceStart, result.SourceEnd, result.DestinationStart, result.DestinationEnd);
    }

    private static ScoreAlignment ScoreEqualLengthWindows<T, TRatio>(
        ReadOnlySpan<T> source,
        ReadOnlySpan<T> target,
        TRatio ratio,
        double scoreCutoff)
        where T : notnull, IEquatable<T>
        where TRatio : struct, ICachedRatioAdapter<T>
    {
        int windowCount = target.Length - source.Length;

        if (windowCount <= 0)
        {
            return new ScoreAlignment(0.0, 0, source.Length, 0, source.Length);
        }

        int maximum = source.Length * 2;
        int cutoffDistance = (int)Math.Ceiling(maximum * (1.0 - scoreCutoff / 100.0));
        int bestDistance = int.MaxValue;
        int destinationStart = 0;
        int[] scores = ArrayPool<int>.Shared.Rent(windowCount);
        PartialRatioWindow[] windows = ArrayPool<PartialRatioWindow>.Shared.Rent(Math.Max(2, windowCount * 2));
        PartialRatioWindow[] nextWindows = ArrayPool<PartialRatioWindow>.Shared.Rent(Math.Max(2, windowCount * 2));
        scores.AsSpan(0, windowCount).Fill(-1);
        windows[0] = new PartialRatioWindow(0, windowCount - 1);
        int currentCount = 1;

        try
        {
            while (currentCount > 0)
            {
                int nextCount = 0;

                for (int index = 0; index < currentCount; index++)
                {
                    PartialRatioWindow window = windows[index];
                    int firstDistance = ScoreWindow(source, target, ratio, scores, window.Start);

                    if (firstDistance <= cutoffDistance)
                    {
                        cutoffDistance = firstDistance;
                        bestDistance = firstDistance;
                        destinationStart = window.Start;

                        if (bestDistance == 0)
                        {
                            return new ScoreAlignment(100.0, 0, source.Length, destinationStart, destinationStart + source.Length);
                        }
                    }

                    int lastDistance = ScoreWindow(source, target, ratio, scores, window.End);

                    if (lastDistance <= cutoffDistance)
                    {
                        cutoffDistance = lastDistance;
                        bestDistance = lastDistance;
                        destinationStart = window.End;

                        if (bestDistance == 0)
                        {
                            return new ScoreAlignment(100.0, 0, source.Length, destinationStart, destinationStart + source.Length);
                        }
                    }

                    int cellDifference = window.End - window.Start;

                    if (cellDifference <= 1)
                    {
                        continue;
                    }

                    int knownEdits = Math.Abs(firstDistance - lastDistance);
                    int maximumImprovement = ((cellDifference - knownEdits / 2) / 2) * 2;
                    int minimumDistance = Math.Min(firstDistance, lastDistance) - maximumImprovement;

                    if (minimumDistance < cutoffDistance)
                    {
                        int center = cellDifference / 2;
                        nextWindows[nextCount++] = new PartialRatioWindow(window.Start, window.Start + center);
                        nextWindows[nextCount++] = new PartialRatioWindow(window.Start + center, window.End);
                    }
                }

                PartialRatioWindow[] swap = windows;
                windows = nextWindows;
                nextWindows = swap;
                currentCount = nextCount;
            }

            if (bestDistance == int.MaxValue)
            {
                return new ScoreAlignment(0.0, 0, source.Length, 0, source.Length);
            }

            double score = (1.0 - (double)bestDistance / maximum) * 100.0;
            double cutoffScore = score >= scoreCutoff ? score : 0.0;
            return new ScoreAlignment(cutoffScore, 0, source.Length, destinationStart, destinationStart + source.Length);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(scores, clearArray: true);
            ArrayPool<PartialRatioWindow>.Shared.Return(windows, clearArray: true);
            ArrayPool<PartialRatioWindow>.Shared.Return(nextWindows, clearArray: true);
        }
    }

    private static int ScoreWindow<T, TRatio>(
        ReadOnlySpan<T> source,
        ReadOnlySpan<T> target,
        TRatio ratio,
        Span<int> scores,
        int start)
        where T : notnull, IEquatable<T>
        where TRatio : struct, ICachedRatioAdapter<T>
    {
        int score = scores[start];

        if (score >= 0)
        {
            return score;
        }

        score = ratio.Distance(target.Slice(start, source.Length));
        scores[start] = score;
        return score;
    }

    private readonly record struct PartialRatioWindow(int Start, int End);
}
