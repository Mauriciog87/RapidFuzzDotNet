namespace RapidFuzz.Distance;

internal static class DistanceHelpers
{
    public static int ApplyDistanceCutoff(int distance, int scoreCutoff)
    {
        if (distance <= scoreCutoff)
        {
            return distance;
        }

        return scoreCutoff == int.MaxValue ? distance : scoreCutoff + 1;
    }

    public static void ValidateScoreCutoff(int scoreCutoff)
    {
        if (scoreCutoff < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreCutoff), "The score cutoff must be greater than or equal to zero.");
        }
    }

    public static void ValidateScoreHint(int scoreHint)
    {
        if (scoreHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreHint), "The score hint must be greater than or equal to zero.");
        }
    }

    public static void ValidateNormalizedCutoff(double scoreCutoff)
    {
        if (double.IsNaN(scoreCutoff) || scoreCutoff < 0.0 || scoreCutoff > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreCutoff), "The score cutoff must be between 0 and 1.");
        }
    }

    public static void ValidateNormalizedHint(double scoreHint)
    {
        if (double.IsNaN(scoreHint) || scoreHint < 0.0 || scoreHint > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreHint), "The score hint must be between 0 and 1.");
        }
    }

    public static int SimilarityFromDistance(int maximum, int distance, int scoreCutoff)
    {
        int similarity = maximum - distance;
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int InitialScoreHint(int scoreHint, int scoreCutoff)
    {
        if (scoreHint >= scoreCutoff)
        {
            return scoreCutoff;
        }

        return Math.Min(Math.Max(scoreHint, 31), scoreCutoff);
    }

    public static int NextScoreHint(int scoreHint, int scoreCutoff)
    {
        if (scoreHint >= scoreCutoff / 2)
        {
            return scoreCutoff;
        }

        return Math.Min(scoreHint * 2, scoreCutoff);
    }

    public static double NormalizedDistanceFromDistance(int maximum, int distance, double scoreCutoff)
    {
        if (maximum == 0)
        {
            return 0.0;
        }

        double normalizedDistance = (double)distance / maximum;
        return normalizedDistance <= scoreCutoff ? normalizedDistance : 1.0;
    }

    public static double NormalizedSimilarityFromDistance(int maximum, int distance, double scoreCutoff)
    {
        if (maximum == 0)
        {
            return 1.0;
        }

        double similarity = 1.0 - ((double)distance / maximum);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static int TrimCommonAffixes(ref ReadOnlySpan<char> first, ref ReadOnlySpan<char> second)
    {
        int prefixLength = CommonPrefixLength(first, second);

        first = first[prefixLength..];
        second = second[prefixLength..];

        int suffixLength = CommonSuffixLength(first, second);

        if (suffixLength > 0)
        {
            first = first[..^suffixLength];
            second = second[..^suffixLength];
        }

        return prefixLength + suffixLength;
    }

    private static int CommonPrefixLength(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        int length = Math.Min(first.Length, second.Length);

        for (int i = 0; i < length; i++)
        {
            if (first[i] != second[i])
            {
                return i;
            }
        }

        return length;
    }

    private static int CommonSuffixLength(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        int length = Math.Min(first.Length, second.Length);

        for (int i = 0; i < length; i++)
        {
            if (first[^(i + 1)] != second[^(i + 1)])
            {
                return i;
            }
        }

        return length;
    }
}
