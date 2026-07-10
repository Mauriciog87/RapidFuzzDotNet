using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public static partial class LcsSeq
{
    private static readonly byte[][] MblevenMatrix =
    [
        [0x00],
        [0x01],
        [0x09, 0x06],
        [0x01],
        [0x05],
        [0x09, 0x06],
        [0x25, 0x19, 0x16],
        [0x05],
        [0x15],
        [0x96, 0x66, 0x5A, 0x99, 0x69, 0xA5],
        [0x25, 0x19, 0x16],
        [0x65, 0x56, 0x95, 0x59],
        [0x15],
        [0x55]
    ];

    public static int Distance(string first, string second, int scoreCutoff = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static int Distance(string first, string second, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff = int.MaxValue)
    {
        return Distance(first, second, scoreCutoff, int.MaxValue);
    }

    public static int Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int effectiveCutoff = Math.Min(scoreCutoff, maximum);
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(first, second, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return DistanceCore(first, second, scoreCutoff);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, scoreCutoff, int.MaxValue);
    }

    public static int Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int effectiveCutoff = Math.Min(scoreCutoff, maximum);
        int currentHint = DistanceHelpers.InitialScoreHint(scoreHint, effectiveCutoff);

        while (currentHint < effectiveCutoff)
        {
            int hintedDistance = DistanceCore(first, second, currentHint);

            if (hintedDistance <= currentHint)
            {
                return hintedDistance;
            }

            currentHint = DistanceHelpers.NextScoreHint(currentHint, effectiveCutoff);
        }

        return DistanceCore(first, second, scoreCutoff);
    }

    private static int DistanceCore<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        int maximum = Math.Max(first.Length, second.Length);
        int similarityCutoff = scoreCutoff >= maximum ? 0 : maximum - scoreCutoff;
        int distance = maximum - SequenceMetrics.LcsSimilarity(first, second, similarityCutoff);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    private static int DistanceCore(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        int maximum = Math.Max(first.Length, second.Length);
        int similarityCutoff = scoreCutoff >= maximum ? 0 : maximum - scoreCutoff;
        int distance = maximum - SimilarityCore(first, second, similarityCutoff);
        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public static int Similarity(string first, string second, int scoreCutoff = 0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static int Similarity(string first, string second, int scoreCutoff, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff = 0)
    {
        return Similarity(first, second, scoreCutoff, 0);
    }

    public static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff, int scoreHint)
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int similarity = SimilarityCore(first, second, scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = 0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, scoreCutoff, 0);
    }

    public static int Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        DistanceHelpers.ValidateScoreHint(scoreHint);

        int commonLength = SequenceMetrics.TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty || second.IsEmpty)
        {
            return commonLength >= scoreCutoff ? commonLength : 0;
        }

        int remainingCutoff = Math.Max(0, scoreCutoff - commonLength);
        ReadOnlySpan<T> pattern = first.Length <= second.Length ? first : second;
        ReadOnlySpan<T> text = first.Length <= second.Length ? second : first;
        int similarity = commonLength + PooledGenericPatternMetrics.LcsSimilarity(pattern, text, remainingCutoff);
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedDistance(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 1.0)
    {
        return NormalizedDistance(first, second, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedDistance(first, second, scoreCutoff, 1.0);
    }

    public static double NormalizedDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);
        int distanceCutoff = (int)Math.Floor(maximum * scoreCutoff);
        int distanceHint = (int)Math.Floor(maximum * scoreHint);
        int distance = Distance(first, second, distanceCutoff, distanceHint);
        return DistanceHelpers.NormalizedDistanceFromDistance(maximum, distance, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NormalizedSimilarity(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        return NormalizedSimilarity(first, second, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        int similarityCutoff = (int)Math.Ceiling(maximum * scoreCutoff);
        double similarity = (double)SimilarityCore(first, second, similarityCutoff) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return NormalizedSimilarity(first, second, scoreCutoff, 0.0);
    }

    public static double NormalizedSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double scoreCutoff,
        double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        int maximum = Math.Max(first.Length, second.Length);

        if (maximum == 0)
        {
            return 1.0;
        }

        int similarityCutoff = (int)Math.Ceiling(maximum * scoreCutoff);
        double similarity = (double)SequenceMetrics.LcsSimilarity(first, second, similarityCutoff) / maximum;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static EditOperations Editops(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Editops(first.AsSpan(), second.AsSpan());
    }

    public static EditOperations Editops(string first, string second, int scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Editops(first.AsSpan(), second.AsSpan(), scoreHint);
    }

    public static EditOperations Editops(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return Editops(first, second, int.MaxValue);
    }

    public static EditOperations Editops(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreHint)
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);

        DirectionMatrix matrix = BuildTraceMatrix(first, second);
        List<EditOp> operations = [];
        int sourcePosition = first.Length;
        int destinationPosition = second.Length;

        while (sourcePosition > 0 || destinationPosition > 0)
        {
            if (sourcePosition == 0)
            {
                destinationPosition--;
                operations.Add(new EditOp(EditOperation.Insert, sourcePosition, destinationPosition));
            }
            else if (destinationPosition == 0)
            {
                sourcePosition--;
                operations.Add(new EditOp(EditOperation.Delete, sourcePosition, destinationPosition));
            }
            else
            {
                byte direction = matrix.Get(sourcePosition, destinationPosition);

                if (direction == PackedDirectionMatrix.Equal)
                {
                    sourcePosition--;
                    destinationPosition--;
                }
                else if (direction == PackedDirectionMatrix.Insert)
                {
                    destinationPosition--;
                    operations.Add(new EditOp(EditOperation.Insert, sourcePosition, destinationPosition));
                }
                else
                {
                    sourcePosition--;
                    operations.Add(new EditOp(EditOperation.Delete, sourcePosition, destinationPosition));
                }
            }
        }

        operations.Reverse();
        return new EditOperations(operations, first.Length, second.Length);
    }

    public static EditOperations Editops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        return Editops(first, second, int.MaxValue);
    }

    public static EditOperations Editops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreHint(scoreHint);
        return SequenceMetrics.LcsEditops(first, second);
    }

    internal static int Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return SimilarityCore(first, second, 0);
    }

    private static int SimilarityCore(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        if (first.IsEmpty || second.IsEmpty)
        {
            return 0;
        }

        if (scoreCutoff > first.Length || scoreCutoff > second.Length)
        {
            return 0;
        }

        int maxMisses = first.Length + second.Length - (2 * scoreCutoff);

        if (maxMisses == 0 || (maxMisses == 1 && first.Length == second.Length))
        {
            return first.SequenceEqual(second) ? first.Length : 0;
        }

        if (maxMisses < Math.Abs(first.Length - second.Length))
        {
            return 0;
        }

        int commonLength = DistanceHelpers.TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty || second.IsEmpty)
        {
            return commonLength >= scoreCutoff ? commonLength : 0;
        }

        int adjustedCutoff = scoreCutoff >= commonLength ? scoreCutoff - commonLength : 0;

        if (maxMisses < 5 && adjustedCutoff > 0)
        {
            int mblevenSimilarity = commonLength + MblevenSimilarity(first, second, adjustedCutoff);
            return mblevenSimilarity >= scoreCutoff ? mblevenSimilarity : 0;
        }

        ReadOnlySpan<char> rows = first;
        ReadOnlySpan<char> columns = second;

        if (columns.Length > rows.Length)
        {
            rows = second;
            columns = first;
        }

        int similarity;

        if (columns.Length <= PatternMatchVector.MaximumPatternLength)
        {
            PatternMatchVector vector = new(columns);
            similarity = commonLength + vector.LcsSimilarityUnrolled(rows, adjustedCutoff);
        }
        else
        {
            similarity = commonLength + PooledAsciiPatternMetrics.LcsSimilarity(columns, rows, adjustedCutoff);
        }

        return similarity >= scoreCutoff ? similarity : 0;
    }

    internal static int Similarity(BlockPatternMatchVector vector, ReadOnlySpan<char> second)
    {
        return vector.LcsSimilarity(second);
    }

    private static DirectionMatrix BuildTraceMatrix(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        int columnCount = second.Length + 1;
        DirectionMatrix directions = TraceMatrixFactory.Create(first.Length + 1, columnCount);
        int[] previous = new int[columnCount];
        int[] current = new int[columnCount];

        for (int column = 0; column < columnCount; column++)
        {
            directions.Set(0, column, PackedDirectionMatrix.Insert);
        }

        for (int row = 1; row <= first.Length; row++)
        {
            directions.Set(row, 0, PackedDirectionMatrix.Delete);

            for (int column = 1; column <= second.Length; column++)
            {
                if (first[row - 1] == second[column - 1])
                {
                    current[column] = previous[column - 1] + 1;
                    directions.Set(row, column, PackedDirectionMatrix.Equal);
                }
                else if (current[column - 1] >= previous[column])
                {
                    current[column] = current[column - 1];
                    directions.Set(row, column, PackedDirectionMatrix.Insert);
                }
                else
                {
                    current[column] = previous[column];
                    directions.Set(row, column, PackedDirectionMatrix.Delete);
                }
            }

            int[] temp = previous;
            previous = current;
            current = temp;
            Array.Clear(current);
        }

        return directions;
    }

    private static int MblevenSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, int scoreCutoff)
    {
        if (first.Length < second.Length)
        {
            return MblevenSimilarity(second, first, scoreCutoff);
        }

        int lengthDifference = first.Length - second.Length;
        int maxMisses = first.Length + second.Length - (2 * scoreCutoff);
        int opsIndex = ((maxMisses + (maxMisses * maxMisses)) / 2) + lengthDifference - 1;

        if (opsIndex < 0 || opsIndex >= MblevenMatrix.Length)
        {
            return 0;
        }

        byte[] possibleOperations = MblevenMatrix[opsIndex];
        int maximumLength = 0;

        for (int operationIndex = 0; operationIndex < possibleOperations.Length; operationIndex++)
        {
            byte operations = possibleOperations[operationIndex];

            if (operations == 0)
            {
                break;
            }

            int firstIndex = 0;
            int secondIndex = 0;
            int currentLength = 0;

            while (firstIndex < first.Length && secondIndex < second.Length)
            {
                if (first[firstIndex] != second[secondIndex])
                {
                    if (operations == 0)
                    {
                        break;
                    }

                    if ((operations & 1) != 0)
                    {
                        firstIndex++;
                    }
                    else if ((operations & 2) != 0)
                    {
                        secondIndex++;
                    }

                    operations >>= 2;
                }
                else
                {
                    currentLength++;
                    firstIndex++;
                    secondIndex++;
                }
            }

            maximumLength = Math.Max(maximumLength, currentLength);
        }

        return maximumLength >= scoreCutoff ? maximumLength : 0;
    }

}
