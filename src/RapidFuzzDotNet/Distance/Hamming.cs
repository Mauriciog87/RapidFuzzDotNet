using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public static partial class Hamming
{
    public static int Distance(string first, string second, bool pad = true, int scoreCutoff = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), pad, scoreCutoff);
    }

    public static int Distance(string first, string second, bool pad, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), pad, scoreCutoff, scoreHint);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad = true, int scoreCutoff = int.MaxValue)
    {
        return Distance(first, second, pad, scoreCutoff, int.MaxValue);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateLengths(first, second, pad);

        int length = Math.Min(first.Length, second.Length);
        int distance = Math.Abs(first.Length - second.Length) + SimdSupport.CountMismatches(first[..length], second[..length]);

        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad = true, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, pad, scoreCutoff, int.MaxValue);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return SequenceMetrics.HammingDistance(first, second, pad, scoreCutoff);
    }

    public static int Similarity(string first, string second, bool pad = true, int scoreCutoff = 0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), pad, scoreCutoff);
    }

    public static int Similarity(string first, string second, bool pad, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), pad, scoreCutoff, scoreHint);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad = true, int scoreCutoff = 0)
    {
        return Similarity(first, second, pad, scoreCutoff, 0);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateLengths(first, second, pad);

        int maximum = pad ? Math.Max(first.Length, second.Length) : first.Length;
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, pad, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad = true, int scoreCutoff = 0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, pad, scoreCutoff, 0);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);
        SequenceMetrics.ValidateHammingLengths(first.Length, second.Length, pad);

        int maximum = pad ? Math.Max(first.Length, second.Length) : first.Length;
        int distanceCutoff = maximum - scoreCutoff;
        int distanceHint = Math.Max(0, maximum - scoreHint);
        int distance = Distance(first, second, pad, distanceCutoff, distanceHint);
        return DistanceHelpers.SimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, bool pad = true, double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), pad, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, bool pad, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), pad, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad = true, double scoreCutoff = 1.0)
    {
        return NormalizedDistance(first, second, pad, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateLengths(first, second, pad);

        int maximum = pad ? Math.Max(first.Length, second.Length) : first.Length;
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, pad, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        bool pad = true,
        double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedDistance(first, second, pad, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        bool pad,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        SequenceMetrics.ValidateHammingLengths(first.Length, second.Length, pad);

        int maximum = pad ? Math.Max(first.Length, second.Length) : first.Length;
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, pad, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, bool pad = true, double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), pad, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, bool pad, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), pad, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad = true, double scoreCutoff = 0.0)
    {
        return NormalizedSimilarity(first, second, pad, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        ValidateLengths(first, second, pad);

        int maximum = pad ? Math.Max(first.Length, second.Length) : first.Length;
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, pad, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        bool pad = true,
        double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedSimilarity(first, second, pad, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        bool pad,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);
        SequenceMetrics.ValidateHammingLengths(first.Length, second.Length, pad);

        int maximum = pad ? Math.Max(first.Length, second.Length) : first.Length;
        int distanceCutoff = (int)Math.Floor(maximum * (1.0 - scoreCutoff));
        int distanceHint = (int)Math.Floor(maximum * (1.0 - scoreHint));
        int distance = Distance(first, second, pad, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedSimilarityFromDistance(maximum, distance, scoreCutoff);
    }

    public static EditOperations Editops(string first, string second, bool pad = true)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Editops(first.AsSpan(), second.AsSpan(), pad);
    }

    public static EditOperations Editops(string first, string second, bool pad, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Editops(first.AsSpan(), second.AsSpan(), pad, scoreHint);
    }

    public static EditOperations Editops(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad = true)
    {
        return Editops(first, second, pad, int.MaxValue);
    }

    public static EditOperations Editops(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad, int scoreHint)
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        ValidateLengths(first, second, pad);

        List<EditOp> operations = [];
        int length = Math.Min(first.Length, second.Length);

        for (int i = 0; i < length; i++)
        {
            if (first[i] != second[i])
            {
                operations.Add(new EditOp(EditOperation.Replace, i, i));
            }
        }

        for (int i = length; i < first.Length; i++)
        {
            operations.Add(new EditOp(EditOperation.Delete, i, second.Length));
        }

        for (int i = length; i < second.Length; i++)
        {
            operations.Add(new EditOp(EditOperation.Insert, first.Length, i));
        }

        return new EditOperations(operations, first.Length, second.Length);
    }

    public static EditOperations Editops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad = true)
        where T : notnull, IEquatable<T>
    {
        return Editops(first, second, pad, int.MaxValue);
    }

    public static EditOperations Editops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return SequenceMetrics.HammingEditops(first, second, pad);
    }

    private static void ValidateLengths(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool pad)
    {
        if (!pad && first.Length != second.Length)
        {
            throw new ArgumentException("Sequences must have equal length when padding is disabled.");
        }
    }
}
