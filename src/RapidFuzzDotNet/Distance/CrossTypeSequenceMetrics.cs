using System.Buffers;

namespace RapidFuzz.Distance;

internal static class CrossTypeSequenceMetrics
{
    private const int StackThreshold = 256;

    public static int LevenshteinDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        LevenshteinWeights weights,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        int columnCount = second.Length + 1;
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = columnCount <= StackThreshold
            ? stackalloc int[columnCount]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        Span<int> current = columnCount <= StackThreshold
            ? stackalloc int[columnCount]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);

        try
        {
            for (int column = 0; column < columnCount; column++)
            {
                previous[column] = column * weights.InsertCost;
            }

            for (int row = 1; row <= first.Length; row++)
            {
                current[0] = row * weights.DeleteCost;

                for (int column = 1; column <= second.Length; column++)
                {
                    int substitutionCost = comparer.Equals(first[row - 1], second[column - 1])
                        ? 0
                        : weights.ReplaceCost;
                    int deletion = previous[column] + weights.DeleteCost;
                    int insertion = current[column - 1] + weights.InsertCost;
                    int substitution = previous[column - 1] + substitutionCost;
                    current[column] = Math.Min(Math.Min(deletion, insertion), substitution);
                }

                Span<int> swap = previous;
                previous = current;
                current = swap;
            }

            return DistanceHelpers.ApplyDistanceCutoff(previous[second.Length], scoreCutoff);
        }
        finally
        {
            if (rentedPrevious is not null)
            {
                ArrayPool<int>.Shared.Return(rentedPrevious, clearArray: true);
            }

            if (rentedCurrent is not null)
            {
                ArrayPool<int>.Shared.Return(rentedCurrent, clearArray: true);
            }
        }
    }

    public static int LcsSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        int columnCount = second.Length + 1;
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = columnCount <= StackThreshold
            ? stackalloc int[columnCount]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        Span<int> current = columnCount <= StackThreshold
            ? stackalloc int[columnCount]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        previous.Clear();
        current.Clear();

        try
        {
            for (int row = 1; row <= first.Length; row++)
            {
                for (int column = 1; column <= second.Length; column++)
                {
                    current[column] = comparer.Equals(first[row - 1], second[column - 1])
                        ? previous[column - 1] + 1
                        : Math.Max(previous[column], current[column - 1]);
                }

                Span<int> swap = previous;
                previous = current;
                current = swap;
                current.Clear();
            }

            int similarity = previous[second.Length];
            return similarity >= scoreCutoff ? similarity : 0;
        }
        finally
        {
            if (rentedPrevious is not null)
            {
                ArrayPool<int>.Shared.Return(rentedPrevious, clearArray: true);
            }

            if (rentedCurrent is not null)
            {
                ArrayPool<int>.Shared.Return(rentedCurrent, clearArray: true);
            }
        }
    }

    public static EditOperations LevenshteinEditops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        int[,] distances = new int[first.Length + 1, second.Length + 1];

        for (int row = 0; row <= first.Length; row++)
        {
            distances[row, 0] = row;
        }

        for (int column = 0; column <= second.Length; column++)
        {
            distances[0, column] = column;
        }

        for (int row = 1; row <= first.Length; row++)
        {
            for (int column = 1; column <= second.Length; column++)
            {
                int cost = comparer.Equals(first[row - 1], second[column - 1]) ? 0 : 1;
                distances[row, column] = Math.Min(
                    Math.Min(distances[row - 1, column] + 1, distances[row, column - 1] + 1),
                    distances[row - 1, column - 1] + cost);
            }
        }

        List<EditOp> operations = [];
        int sourcePosition = first.Length;
        int destinationPosition = second.Length;

        while (sourcePosition > 0 || destinationPosition > 0)
        {
            if (sourcePosition > 0
                && destinationPosition > 0
                && comparer.Equals(first[sourcePosition - 1], second[destinationPosition - 1])
                && distances[sourcePosition, destinationPosition] == distances[sourcePosition - 1, destinationPosition - 1])
            {
                sourcePosition--;
                destinationPosition--;
            }
            else if (sourcePosition > 0
                && destinationPosition > 0
                && distances[sourcePosition, destinationPosition] == distances[sourcePosition - 1, destinationPosition - 1] + 1)
            {
                sourcePosition--;
                destinationPosition--;
                operations.Add(new EditOp(EditOperation.Replace, sourcePosition, destinationPosition));
            }
            else if (sourcePosition > 0
                && distances[sourcePosition, destinationPosition] == distances[sourcePosition - 1, destinationPosition] + 1)
            {
                sourcePosition--;
                operations.Add(new EditOp(EditOperation.Delete, sourcePosition, destinationPosition));
            }
            else
            {
                destinationPosition--;
                operations.Add(new EditOp(EditOperation.Insert, sourcePosition, destinationPosition));
            }
        }

        operations.Reverse();
        return new EditOperations(operations, first.Length, second.Length);
    }

    public static EditOperations LcsEditops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        int[,] similarities = new int[first.Length + 1, second.Length + 1];

        for (int row = 1; row <= first.Length; row++)
        {
            for (int column = 1; column <= second.Length; column++)
            {
                similarities[row, column] = comparer.Equals(first[row - 1], second[column - 1])
                    ? similarities[row - 1, column - 1] + 1
                    : Math.Max(similarities[row - 1, column], similarities[row, column - 1]);
            }
        }

        List<EditOp> operations = [];
        int sourcePosition = first.Length;
        int destinationPosition = second.Length;

        while (sourcePosition > 0 || destinationPosition > 0)
        {
            if (sourcePosition > 0
                && destinationPosition > 0
                && comparer.Equals(first[sourcePosition - 1], second[destinationPosition - 1]))
            {
                sourcePosition--;
                destinationPosition--;
            }
            else if (destinationPosition > 0
                && (sourcePosition == 0
                    || similarities[sourcePosition, destinationPosition - 1] >= similarities[sourcePosition - 1, destinationPosition]))
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

        operations.Reverse();
        return new EditOperations(operations, first.Length, second.Length);
    }

    public static int HammingDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (!pad && first.Length != second.Length)
        {
            throw new ArgumentException("Sequences must have the same length when padding is disabled.");
        }

        int length = Math.Min(first.Length, second.Length);
        int distance = Math.Abs(first.Length - second.Length);

        for (int index = 0; index < length; index++)
        {
            if (!comparer.Equals(first[index], second[index]))
            {
                distance++;
            }
        }

        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public static EditOperations HammingEditops<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        bool pad)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);

        if (!pad && first.Length != second.Length)
        {
            throw new ArgumentException("Sequences must have the same length when padding is disabled.");
        }

        List<EditOp> operations = [];
        int length = Math.Min(first.Length, second.Length);

        for (int index = 0; index < length; index++)
        {
            if (!comparer.Equals(first[index], second[index]))
            {
                operations.Add(new EditOp(EditOperation.Replace, index, index));
            }
        }

        for (int index = length; index < first.Length; index++)
        {
            operations.Add(new EditOp(EditOperation.Delete, index, second.Length));
        }

        for (int index = length; index < second.Length; index++)
        {
            operations.Add(new EditOp(EditOperation.Insert, first.Length, index));
        }

        return new EditOperations(operations, first.Length, second.Length);
    }

    public static int OsaDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        int columnCount = second.Length + 1;
        int storageLength = columnCount * 3;
        int[]? rented = null;
        Span<int> storage = storageLength <= StackThreshold
            ? stackalloc int[storageLength]
            : (rented = ArrayPool<int>.Shared.Rent(storageLength)).AsSpan(0, storageLength);
        Span<int> previousPrevious = storage[..columnCount];
        Span<int> previous = storage.Slice(columnCount, columnCount);
        Span<int> current = storage.Slice(columnCount * 2, columnCount);

        try
        {
            for (int column = 0; column < columnCount; column++)
            {
                previous[column] = column;
            }

            for (int row = 1; row <= first.Length; row++)
            {
                current[0] = row;

                for (int column = 1; column <= second.Length; column++)
                {
                    int cost = comparer.Equals(first[row - 1], second[column - 1]) ? 0 : 1;
                    int distance = Math.Min(
                        Math.Min(previous[column] + 1, current[column - 1] + 1),
                        previous[column - 1] + cost);

                    if (row > 1
                        && column > 1
                        && comparer.Equals(first[row - 1], second[column - 2])
                        && comparer.Equals(first[row - 2], second[column - 1]))
                    {
                        distance = Math.Min(distance, previousPrevious[column - 2] + 1);
                    }

                    current[column] = distance;
                }

                Span<int> swap = previousPrevious;
                previousPrevious = previous;
                previous = current;
                current = swap;
            }

            return DistanceHelpers.ApplyDistanceCutoff(previous[second.Length], scoreCutoff);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented, clearArray: true);
            }
        }
    }

    public static int DamerauLevenshteinDistance<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        int minimumDistance = Math.Abs(first.Length - second.Length);

        if (minimumDistance > scoreCutoff)
        {
            return DistanceHelpers.ApplyDistanceCutoff(minimumDistance, scoreCutoff);
        }

        TrimCommonAffixes(ref first, ref second, comparer);

        if (first.IsEmpty || second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(Math.Max(first.Length, second.Length), scoreCutoff);
        }

        int rowSize = second.Length + 2;
        int storageLength = checked(rowSize * 3 + second.Length);
        int[]? rented = null;
        Span<int> storage = rowSize <= StackThreshold
            ? stackalloc int[storageLength]
            : (rented = ArrayPool<int>.Shared.Rent(storageLength)).AsSpan(0, storageLength);

        try
        {
            Span<int> fr = storage[..rowSize];
            Span<int> previous = storage.Slice(rowSize, rowSize);
            Span<int> current = storage.Slice(rowSize * 2, rowSize);
            Span<int> lastMatchingRows = storage.Slice(rowSize * 3, second.Length);
            int maximumValue = Math.Max(first.Length, second.Length) + 1;
            fr.Fill(maximumValue);
            previous.Fill(maximumValue);
            lastMatchingRows.Fill(-1);
            current[0] = maximumValue;

            for (int index = 1; index < rowSize; index++)
            {
                current[index] = index - 1;
            }

            for (int row = 1; row <= first.Length; row++)
            {
                Span<int> temporary = current;
                current = previous;
                previous = temporary;
                int lastMatchColumn = -1;
                int lastPreviousValue = current[1];
                current[1] = row;
                int transpositionBase = maximumValue;
                TLeft firstValue = first[row - 1];

                for (int column = 1; column <= second.Length; column++)
                {
                    bool equal = comparer.Equals(firstValue, second[column - 1]);
                    int diagonal = previous[column] + (equal ? 0 : 1);
                    int insertion = current[column] + 1;
                    int deletion = previous[column + 1] + 1;
                    int value = Math.Min(diagonal, Math.Min(insertion, deletion));

                    if (equal)
                    {
                        lastMatchColumn = column;
                        fr[column + 1] = previous[column - 1];
                        transpositionBase = lastPreviousValue;
                    }
                    else
                    {
                        int lastMatchingRow = lastMatchingRows[column - 1];

                        if (column - lastMatchColumn == 1)
                        {
                            value = Math.Min(value, fr[column + 1] + row - lastMatchingRow);
                        }
                        else if (row - lastMatchingRow == 1)
                        {
                            value = Math.Min(value, transpositionBase + column - lastMatchColumn);
                        }
                    }

                    lastPreviousValue = current[column + 1];
                    current[column + 1] = value;
                }

                for (int column = 0; column < second.Length; column++)
                {
                    if (comparer.Equals(firstValue, second[column]))
                    {
                        lastMatchingRows[column] = row;
                    }
                }
            }

            return DistanceHelpers.ApplyDistanceCutoff(current[second.Length + 1], scoreCutoff);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    public static int PrefixSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        int length = Math.Min(first.Length, second.Length);
        int similarity = 0;

        while (similarity < length && comparer.Equals(first[similarity], second[similarity]))
        {
            similarity++;
        }

        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int PostfixSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        int scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        int length = Math.Min(first.Length, second.Length);
        int similarity = 0;

        while (similarity < length
            && comparer.Equals(first[first.Length - similarity - 1], second[second.Length - similarity - 1]))
        {
            similarity++;
        }

        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static double JaroSimilarity<TLeft, TRight>(
        ReadOnlySpan<TLeft> first,
        ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer,
        double scoreCutoff)
        where TLeft : notnull
        where TRight : notnull
    {
        ArgumentNullException.ThrowIfNull(comparer);
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            double emptyScore = first.IsEmpty && second.IsEmpty ? 1.0 : 0.0;
            return emptyScore >= scoreCutoff ? emptyScore : 0.0;
        }

        if (first.Length == 1 && second.Length == 1)
        {
            double singleScore = comparer.Equals(first[0], second[0]) ? 1.0 : 0.0;
            return singleScore >= scoreCutoff ? singleScore : 0.0;
        }

        int firstLength = first.Length;
        int secondLength = second.Length;
        bool[]? rentedFirst = null;
        bool[]? rentedSecond = null;
        Span<bool> firstMatches = firstLength <= StackThreshold
            ? stackalloc bool[firstLength]
            : (rentedFirst = ArrayPool<bool>.Shared.Rent(firstLength)).AsSpan(0, firstLength);
        Span<bool> secondMatches = secondLength <= StackThreshold
            ? stackalloc bool[secondLength]
            : (rentedSecond = ArrayPool<bool>.Shared.Rent(secondLength)).AsSpan(0, secondLength);
        firstMatches.Clear();
        secondMatches.Clear();

        try
        {
            int matchDistance = Math.Max(firstLength, secondLength) / 2 - 1;
            matchDistance = Math.Max(matchDistance, 0);
            int matches = 0;

            for (int firstIndex = 0; firstIndex < firstLength; firstIndex++)
            {
                int start = Math.Max(0, firstIndex - matchDistance);
                int end = Math.Min(firstIndex + matchDistance + 1, secondLength);

                for (int secondIndex = start; secondIndex < end; secondIndex++)
                {
                    if (secondMatches[secondIndex] || !comparer.Equals(first[firstIndex], second[secondIndex]))
                    {
                        continue;
                    }

                    firstMatches[firstIndex] = true;
                    secondMatches[secondIndex] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0)
            {
                return 0.0;
            }

            int secondMatchIndex = 0;
            int transpositions = 0;

            for (int firstIndex = 0; firstIndex < firstLength; firstIndex++)
            {
                if (!firstMatches[firstIndex])
                {
                    continue;
                }

                while (!secondMatches[secondMatchIndex])
                {
                    secondMatchIndex++;
                }

                if (!comparer.Equals(first[firstIndex], second[secondMatchIndex]))
                {
                    transpositions++;
                }

                secondMatchIndex++;
            }

            double matchCount = matches;
            int halfTranspositions = transpositions / 2;
            double similarity = (matchCount / firstLength
                + matchCount / secondLength
                + (matchCount - halfTranspositions) / matchCount) / 3.0;
            return similarity >= scoreCutoff ? similarity : 0.0;
        }
        finally
        {
            if (rentedFirst is not null)
            {
                ArrayPool<bool>.Shared.Return(rentedFirst, clearArray: true);
            }

            if (rentedSecond is not null)
            {
                ArrayPool<bool>.Shared.Return(rentedSecond, clearArray: true);
            }
        }
    }

    private static void TrimCommonAffixes<TLeft, TRight>(
        ref ReadOnlySpan<TLeft> first,
        ref ReadOnlySpan<TRight> second,
        ISequenceEqualityComparer<TLeft, TRight> comparer)
        where TLeft : notnull
        where TRight : notnull
    {
        int commonLength = Math.Min(first.Length, second.Length);
        int prefixLength = 0;

        while (prefixLength < commonLength && comparer.Equals(first[prefixLength], second[prefixLength]))
        {
            prefixLength++;
        }

        first = first[prefixLength..];
        second = second[prefixLength..];
        commonLength = Math.Min(first.Length, second.Length);
        int suffixLength = 0;

        while (suffixLength < commonLength
            && comparer.Equals(first[first.Length - suffixLength - 1], second[second.Length - suffixLength - 1]))
        {
            suffixLength++;
        }

        if (suffixLength > 0)
        {
            first = first[..^suffixLength];
            second = second[..^suffixLength];
        }
    }
}
