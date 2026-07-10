using RapidFuzz.Distance;
using RapidFuzz.Internal;

namespace RapidFuzz;

public static class Fuzz
{
    public static double Ratio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        return Ratio(prepared.First.AsSpan(), prepared.Second.AsSpan(), scoreCutoff);
    }

    public static double Ratio(string first, string second, double scoreCutoff)
    {
        return Ratio(first, second, scoreCutoff, null);
    }

    public static double Ratio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        double score = Indel.NormalizedSimilarity(first, second) * 100.0;
        return score >= scoreCutoff ? score : 0.0;
    }

    public static double Ratio<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        ValidateScoreCutoff(scoreCutoff);

        double score = Indel.NormalizedSimilarity(first, second) * 100.0;
        return score >= scoreCutoff ? score : 0.0;
    }

    public static double PartialRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        return PartialRatio(prepared.First.AsSpan(), prepared.Second.AsSpan(), scoreCutoff);
    }

    public static double PartialRatio(string first, string second, double scoreCutoff)
    {
        return PartialRatio(first, second, scoreCutoff, null);
    }

    public static double PartialRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        return PartialRatioAlignment(first, second, scoreCutoff).Score;
    }

    public static double PartialRatio<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return PartialRatioAlignment(first, second, scoreCutoff).Score;
    }

    public static ScoreAlignment PartialRatioAlignment(
        string first,
        string second,
        double scoreCutoff = 0.0,
        Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        return PartialRatioAlignment(prepared.First.AsSpan(), prepared.Second.AsSpan(), scoreCutoff);
    }

    public static ScoreAlignment PartialRatioAlignment(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.Length > second.Length)
        {
            ScoreAlignment result = PartialRatioAlignment(second, first, scoreCutoff);
            return new ScoreAlignment(
                result.Score,
                result.DestinationStart,
                result.DestinationEnd,
                result.SourceStart,
                result.SourceEnd);
        }

        if (scoreCutoff > 100.0)
        {
            return new ScoreAlignment(0.0, 0, first.Length, 0, first.Length);
        }

        if (first.IsEmpty || second.IsEmpty)
        {
            double emptyScore = first.IsEmpty && second.IsEmpty ? 100.0 : 0.0;
            double cutoffScore = emptyScore >= scoreCutoff ? emptyScore : 0.0;
            return new ScoreAlignment(cutoffScore, 0, first.Length, 0, first.Length);
        }

        ScoreAlignment alignment = PartialRatioAlignmentWithShorterSource(first, second, scoreCutoff);

        if (alignment.Score != 100.0 && first.Length == second.Length)
        {
            double nextCutoff = Math.Max(scoreCutoff, alignment.Score);
            ScoreAlignment reversed = PartialRatioAlignmentWithShorterSource(second, first, nextCutoff);

            if (reversed.Score > alignment.Score)
            {
                return new ScoreAlignment(
                    reversed.Score,
                    reversed.DestinationStart,
                    reversed.DestinationEnd,
                    reversed.SourceStart,
                    reversed.SourceEnd);
            }
        }

        return alignment;
    }

    public static ScoreAlignment PartialRatioAlignment<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.Length > second.Length)
        {
            ScoreAlignment result = PartialRatioAlignment(second, first, scoreCutoff);
            return new ScoreAlignment(
                result.Score,
                result.DestinationStart,
                result.DestinationEnd,
                result.SourceStart,
                result.SourceEnd);
        }

        if (scoreCutoff > 100.0)
        {
            return new ScoreAlignment(0.0, 0, first.Length, 0, first.Length);
        }

        if (first.IsEmpty || second.IsEmpty)
        {
            double emptyScore = first.IsEmpty && second.IsEmpty ? 100.0 : 0.0;
            double cutoffScore = emptyScore >= scoreCutoff ? emptyScore : 0.0;
            return new ScoreAlignment(cutoffScore, 0, first.Length, 0, first.Length);
        }

        ScoreAlignment alignment = PartialRatioAlignmentWithShorterSource(first, second, scoreCutoff);

        if (alignment.Score != 100.0 && first.Length == second.Length)
        {
            double nextCutoff = Math.Max(scoreCutoff, alignment.Score);
            ScoreAlignment reversed = PartialRatioAlignmentWithShorterSource(second, first, nextCutoff);

            if (reversed.Score > alignment.Score)
            {
                return new ScoreAlignment(
                    reversed.Score,
                    reversed.DestinationStart,
                    reversed.DestinationEnd,
                    reversed.SourceStart,
                    reversed.SourceEnd);
            }
        }

        return alignment;
    }

    public static double TokenSortRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(prepared.First);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(prepared.Second);
        return TokenSortRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double TokenSortRatio(string first, string second, double scoreCutoff)
    {
        return TokenSortRatio(first, second, scoreCutoff, null);
    }

    public static double TokenSortRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);
        return TokenSortRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double PartialTokenSortRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(prepared.First);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(prepared.Second);
        return PartialTokenSortRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double PartialTokenSortRatio(string first, string second, double scoreCutoff)
    {
        return PartialTokenSortRatio(first, second, scoreCutoff, null);
    }

    public static double PartialTokenSortRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);
        return PartialTokenSortRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double TokenSetRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(prepared.First);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(prepared.Second);
        return TokenSetRatio(firstTokens.TokenSet, secondTokens.TokenSet, scoreCutoff);
    }

    public static double TokenSetRatio(string first, string second, double scoreCutoff)
    {
        return TokenSetRatio(first, second, scoreCutoff, null);
    }

    public static double TokenSetRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);

        if (firstTokens.UniqueTokens.Count == 0 || secondTokens.UniqueTokens.Count == 0)
        {
            return 0.0;
        }

        return TokenSetRatio(firstTokens.TokenSet, secondTokens.TokenSet, scoreCutoff);
    }

    public static double PartialTokenSetRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(prepared.First);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(prepared.Second);
        return PartialTokenSetRatio(firstTokens.TokenSet, secondTokens.TokenSet, scoreCutoff);
    }

    public static double PartialTokenSetRatio(string first, string second, double scoreCutoff)
    {
        return PartialTokenSetRatio(first, second, scoreCutoff, null);
    }

    public static double PartialTokenSetRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);

        if (firstTokens.UniqueTokens.Count == 0 || secondTokens.UniqueTokens.Count == 0)
        {
            return 0.0;
        }

        return PartialTokenSetRatio(firstTokens.TokenSet, secondTokens.TokenSet, scoreCutoff);
    }

    public static double TokenRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(prepared.First);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(prepared.Second);
        return TokenRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double TokenRatio(string first, string second, double scoreCutoff)
    {
        return TokenRatio(first, second, scoreCutoff, null);
    }

    public static double TokenRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);
        return TokenRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double PartialTokenRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(prepared.First);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(prepared.Second);
        return PartialTokenRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double PartialTokenRatio(string first, string second, double scoreCutoff)
    {
        return PartialTokenRatio(first, second, scoreCutoff, null);
    }

    public static double PartialTokenRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
        TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);
        return PartialTokenRatio(firstTokens, secondTokens, scoreCutoff);
    }

    public static double QRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        return QRatio(prepared.First.AsSpan(), prepared.Second.AsSpan(), scoreCutoff);
    }

    public static double QRatio(string first, string second, double scoreCutoff)
    {
        return QRatio(first, second, scoreCutoff, null);
    }

    public static double QRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            return 0.0;
        }

        return Ratio(first, second, scoreCutoff);
    }

    public static double QRatio<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            return 0.0;
        }

        return Ratio<T>(first, second, scoreCutoff);
    }

    public static double WRatio(string first, string second, double scoreCutoff = 0.0, Func<string, string>? processor = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        (string First, string Second) prepared = Prepare(first, second, processor);
        return WRatioCore(prepared.First, prepared.Second, scoreCutoff);
    }

    public static double WRatio(string first, string second, double scoreCutoff)
    {
        return WRatio(first, second, scoreCutoff, null);
    }

    public static double WRatio(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            return 0.0;
        }

        double baseScore = Ratio(first, second);
        double lengthRatio = (double)Math.Max(first.Length, second.Length) / Math.Min(first.Length, second.Length);
        double score;

        if (lengthRatio < 1.5)
        {
            score = Math.Max(baseScore, TokenRatio(first, second) * 0.95);
        }
        else
        {
            double partialScale = lengthRatio > 8.0 ? 0.6 : 0.9;
            score = Math.Max(
                baseScore,
                Math.Max(
                    PartialRatio(first, second) * partialScale,
                    PartialTokenRatio(first, second) * partialScale * 0.95));
        }

        return score >= scoreCutoff ? score : 0.0;
    }

    private static double WRatioCore(string first, string second, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        if (first.Length == 0 || second.Length == 0)
        {
            return 0.0;
        }

        double baseScore = Ratio(first.AsSpan(), second.AsSpan());
        double lengthRatio = (double)Math.Max(first.Length, second.Length) / Math.Min(first.Length, second.Length);
        double score;

        if (lengthRatio < 1.5)
        {
            TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
            TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);
            score = Math.Max(baseScore, TokenRatio(firstTokens, secondTokens, 0.0) * 0.95);
        }
        else
        {
            double partialScale = lengthRatio > 8.0 ? 0.6 : 0.9;
            TokenizedString firstTokens = StringHelpers.CreateTokenizedString(first);
            TokenizedString secondTokens = StringHelpers.CreateTokenizedString(second);
            score = Math.Max(
                baseScore,
                Math.Max(
                    PartialRatio(first.AsSpan(), second.AsSpan()) * partialScale,
                    PartialTokenRatio(firstTokens, secondTokens, 0.0) * partialScale * 0.95));
        }

        return score >= scoreCutoff ? score : 0.0;
    }

    private static ScoreAlignment PartialRatioAlignmentWithShorterSource(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        double scoreCutoff)
    {
        double bestScore = 0.0;
        int bestDestinationStart = 0;
        int bestDestinationEnd = first.Length;
        CharacterSet firstCharacters = CreateCharacterSet(first);

        if (second.Length > first.Length)
        {
            HashSet<int> matchedStarts = ScoreMatchingBlockCandidates(
                first,
                second,
                scoreCutoff,
                ref bestScore,
                ref bestDestinationStart,
                ref bestDestinationEnd);

            if (bestScore >= 100.0)
            {
                return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
            }

            for (int i = 0; i <= second.Length - first.Length; i++)
            {
                if (matchedStarts.Contains(i))
                {
                    continue;
                }

                if (ScoreEqualLengthCandidate(
                    first,
                    second,
                    i,
                    scoreCutoff,
                    ref bestScore,
                    ref bestDestinationStart,
                    ref bestDestinationEnd))
                {
                    return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
                }
            }
        }

        for (int length = 1; length < first.Length; length++)
        {
            ReadOnlySpan<char> candidate = second[..length];

            if (!firstCharacters.Contains(candidate[^1]))
            {
                continue;
            }

            double score = Ratio(first, candidate, Math.Max(scoreCutoff, bestScore));

            if (score > bestScore)
            {
                bestScore = score;
                bestDestinationStart = 0;
                bestDestinationEnd = length;

                if (bestScore >= 100.0)
                {
                    return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
                }
            }
        }

        for (int start = second.Length - first.Length; start < second.Length; start++)
        {
            ReadOnlySpan<char> candidate = second[start..];

            if (!firstCharacters.Contains(candidate[0]))
            {
                continue;
            }

            double score = Ratio(first, candidate, Math.Max(scoreCutoff, bestScore));

            if (score > bestScore)
            {
                bestScore = score;
                bestDestinationStart = start;
                bestDestinationEnd = second.Length;

                if (bestScore >= 100.0)
                {
                    return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
                }
            }
        }

        double cutoffScore = bestScore >= scoreCutoff ? bestScore : 0.0;
        return new ScoreAlignment(cutoffScore, 0, first.Length, bestDestinationStart, bestDestinationEnd);
    }

    private static ScoreAlignment PartialRatioAlignmentWithShorterSource<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        double bestScore = 0.0;
        int bestDestinationStart = 0;
        int bestDestinationEnd = first.Length;

        for (int start = 0; start <= second.Length - first.Length; start++)
        {
            if (ScoreEqualLengthCandidate(
                first,
                second,
                start,
                scoreCutoff,
                ref bestScore,
                ref bestDestinationStart,
                ref bestDestinationEnd))
            {
                return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
            }
        }

        for (int length = 1; length < first.Length; length++)
        {
            double score = Ratio(first, second[..length], Math.Max(scoreCutoff, bestScore));

            if (score > bestScore)
            {
                bestScore = score;
                bestDestinationStart = 0;
                bestDestinationEnd = length;

                if (bestScore >= 100.0)
                {
                    return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
                }
            }
        }

        for (int start = second.Length - first.Length; start < second.Length; start++)
        {
            double score = Ratio(first, second[start..], Math.Max(scoreCutoff, bestScore));

            if (score > bestScore)
            {
                bestScore = score;
                bestDestinationStart = start;
                bestDestinationEnd = second.Length;

                if (bestScore >= 100.0)
                {
                    return new ScoreAlignment(100.0, 0, first.Length, bestDestinationStart, bestDestinationEnd);
                }
            }
        }

        double cutoffScore = bestScore >= scoreCutoff ? bestScore : 0.0;
        return new ScoreAlignment(cutoffScore, 0, first.Length, bestDestinationStart, bestDestinationEnd);
    }

    private static HashSet<int> ScoreMatchingBlockCandidates(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        double scoreCutoff,
        ref double bestScore,
        ref int bestDestinationStart,
        ref int bestDestinationEnd)
    {
        HashSet<int> starts = [];
        MatchingBlock[] matchingBlocks = Indel.Opcodes(first, second).GetMatchingBlocks();
        int maximumStart = second.Length - first.Length;

        for (int i = 0; i < matchingBlocks.Length; i++)
        {
            MatchingBlock block = matchingBlocks[i];

            if (block.Length == 0)
            {
                continue;
            }

            int alignedStart = Math.Clamp(block.DestinationPosition - block.SourcePosition, 0, maximumStart);
            int trailingStart = Math.Clamp(block.DestinationPosition + block.Length - first.Length, 0, maximumStart);

            if (starts.Add(alignedStart)
                && ScoreEqualLengthCandidate(
                    first,
                    second,
                    alignedStart,
                    scoreCutoff,
                    ref bestScore,
                    ref bestDestinationStart,
                    ref bestDestinationEnd))
            {
                break;
            }

            if (starts.Add(trailingStart)
                && ScoreEqualLengthCandidate(
                    first,
                    second,
                    trailingStart,
                    scoreCutoff,
                    ref bestScore,
                    ref bestDestinationStart,
                    ref bestDestinationEnd))
            {
                break;
            }
        }

        return starts;
    }

    private static bool ScoreEqualLengthCandidate(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        int destinationStart,
        double scoreCutoff,
        ref double bestScore,
        ref int bestDestinationStart,
        ref int bestDestinationEnd)
    {
        double score = Ratio(first, second.Slice(destinationStart, first.Length), Math.Max(scoreCutoff, bestScore));

        if (score > bestScore)
        {
            bestScore = score;
            bestDestinationStart = destinationStart;
            bestDestinationEnd = destinationStart + first.Length;
        }

        return bestScore >= 100.0;
    }

    private static bool ScoreEqualLengthCandidate<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        int destinationStart,
        double scoreCutoff,
        ref double bestScore,
        ref int bestDestinationStart,
        ref int bestDestinationEnd)
        where T : notnull, IEquatable<T>
    {
        double score = Ratio(first, second.Slice(destinationStart, first.Length), Math.Max(scoreCutoff, bestScore));

        if (score > bestScore)
        {
            bestScore = score;
            bestDestinationStart = destinationStart;
            bestDestinationEnd = destinationStart + first.Length;
        }

        return bestScore >= 100.0;
    }

    private static CharacterSet CreateCharacterSet(ReadOnlySpan<char> value)
    {
        CharacterSet characters = new();

        for (int i = 0; i < value.Length; i++)
        {
            characters.Add(value[i]);
        }

        return characters;
    }

    private static (string First, string Second) Prepare(string first, string second, Func<string, string>? processor)
    {
        if (processor is null)
        {
            return (first, second);
        }

        return (processor(first), processor(second));
    }

    internal static double TokenSetRatio(TokenSet firstSet, TokenSet secondSet, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        if (scoreCutoff > 100.0)
        {
            return 0.0;
        }

        if (firstSet.Tokens.Count == 0 || secondSet.Tokens.Count == 0)
        {
            return 0.0;
        }

        TokenSetParts parts = StringHelpers.CompareTokenSets(firstSet, secondSet);

        if (parts.Intersection.Count > 0 && (parts.FirstDifference.Count == 0 || parts.SecondDifference.Count == 0))
        {
            return scoreCutoff <= 100.0 ? 100.0 : 0.0;
        }

        double score = Math.Max(
            Ratio(parts.CombinedFirst, parts.CombinedSecond),
            Math.Max(Ratio(parts.SortedIntersection, parts.CombinedFirst), Ratio(parts.SortedIntersection, parts.CombinedSecond)));

        return score >= scoreCutoff ? score : 0.0;
    }

    internal static double TokenSortRatio(TokenizedString first, TokenizedString second, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        return Ratio(first.Sorted, second.Sorted, scoreCutoff);
    }

    internal static double PartialTokenSortRatio(TokenizedString first, TokenizedString second, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        return PartialRatio(first.Sorted, second.Sorted, scoreCutoff);
    }

    internal static double TokenRatio(TokenizedString first, TokenizedString second, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        double score = Math.Max(
            TokenSetRatio(first.TokenSet, second.TokenSet, 0.0),
            TokenSortRatio(first, second, 0.0));
        return score >= scoreCutoff ? score : 0.0;
    }

    internal static double PartialTokenRatio(TokenizedString first, TokenizedString second, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        double score = Math.Max(
            PartialTokenSetRatio(first.TokenSet, second.TokenSet, 0.0),
            PartialTokenSortRatio(first, second, 0.0));
        return score >= scoreCutoff ? score : 0.0;
    }

    internal static double PartialTokenSetRatio(TokenSet firstSet, TokenSet secondSet, double scoreCutoff)
    {
        ValidateScoreCutoff(scoreCutoff);

        if (scoreCutoff > 100.0)
        {
            return 0.0;
        }

        if (firstSet.Tokens.Count == 0 || secondSet.Tokens.Count == 0)
        {
            return 0.0;
        }

        TokenSetParts parts = StringHelpers.CompareTokenSets(firstSet, secondSet);

        if (parts.Intersection.Count > 0)
        {
            return 100.0;
        }

        double score = Math.Max(
            PartialRatio(parts.CombinedFirst, parts.CombinedSecond),
            Math.Max(PartialRatio(parts.SortedIntersection, parts.CombinedFirst), PartialRatio(parts.SortedIntersection, parts.CombinedSecond)));

        return score >= scoreCutoff ? score : 0.0;
    }

    internal static void ValidateScoreCutoff(double scoreCutoff)
    {
        if (double.IsNaN(scoreCutoff) || scoreCutoff < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreCutoff), "The score cutoff must be non-negative.");
        }
    }
}
