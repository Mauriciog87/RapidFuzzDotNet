using System.Buffers;

namespace RapidFuzz.Distance;

internal static class SequenceMetrics
{
    private const int StackLimit = 256;

    public static int LevenshteinDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        LevenshteinWeights weights,
        int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length * weights.InsertCost, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length * weights.DeleteCost, scoreCutoff);
        }

        int columnCount = second.Length + 1;
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = columnCount <= StackLimit
            ? stackalloc int[columnCount]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        Span<int> current = columnCount <= StackLimit
            ? stackalloc int[columnCount]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        try
        {
            for (int column = 0; column < columnCount; column++)
            {
                previous[column] = column * weights.InsertCost;
            }

            for (int row = 1; row <= first.Length; row++)
            {
                current[0] = row * weights.DeleteCost;
                int rowMinimum = current[0];

                for (int column = 1; column <= second.Length; column++)
                {
                    int substitutionCost = comparer.Equals(first[row - 1], second[column - 1]) ? 0 : weights.ReplaceCost;
                    int deletion = previous[column] + weights.DeleteCost;
                    int insertion = current[column - 1] + weights.InsertCost;
                    int substitution = previous[column - 1] + substitutionCost;
                    int value = Math.Min(Math.Min(deletion, insertion), substitution);

                    current[column] = value;

                    if (value < rowMinimum)
                    {
                        rowMinimum = value;
                    }
                }

                if (rowMinimum > scoreCutoff)
                {
                    return scoreCutoff == int.MaxValue ? rowMinimum : scoreCutoff + 1;
                }

                Span<int> temporary = previous;
                previous = current;
                current = temporary;
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

    public static int LevenshteinMaximum(int firstLength, int secondLength, LevenshteinWeights weights)
    {
        int sharedLength = Math.Min(firstLength, secondLength);
        int replacementMaximum = Math.Min(weights.ReplaceCost, weights.InsertCost + weights.DeleteCost);
        int extraCost = firstLength > secondLength
            ? (firstLength - secondLength) * weights.DeleteCost
            : (secondLength - firstLength) * weights.InsertCost;

        return (sharedLength * replacementMaximum) + extraCost;
    }

    public static EditOperations LevenshteinEditops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        int[,] distances = BuildLevenshteinMatrix(first, second);
        List<EditOp> operations = [];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int row = first.Length;
        int column = second.Length;

        while (row > 0 || column > 0)
        {
            if (row > 0 && column > 0 && comparer.Equals(first[row - 1], second[column - 1]))
            {
                row--;
                column--;
            }
            else if (row > 0 && column > 0 && distances[row, column] == distances[row - 1, column - 1] + 1)
            {
                row--;
                column--;
                operations.Add(new EditOp(EditOperation.Replace, row, column));
            }
            else if (row > 0 && distances[row, column] == distances[row - 1, column] + 1)
            {
                row--;
                operations.Add(new EditOp(EditOperation.Delete, row, column));
            }
            else
            {
                column--;
                operations.Add(new EditOp(EditOperation.Insert, row, column));
            }
        }

        operations.Reverse();
        return new EditOperations(operations, first.Length, second.Length);
    }

    public static int LcsSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.IsEmpty || second.IsEmpty)
        {
            return 0;
        }

        if (scoreCutoff > first.Length || scoreCutoff > second.Length)
        {
            return 0;
        }

        int commonLength = TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty || second.IsEmpty)
        {
            return commonLength >= scoreCutoff ? commonLength : 0;
        }

        int adjustedCutoff = Math.Max(0, scoreCutoff - commonLength);

        int columnCount = second.Length + 1;
        int[]? rentedPrevious = null;
        int[]? rentedCurrent = null;
        Span<int> previous = columnCount <= StackLimit
            ? stackalloc int[columnCount]
            : (rentedPrevious = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        Span<int> current = columnCount <= StackLimit
            ? stackalloc int[columnCount]
            : (rentedCurrent = ArrayPool<int>.Shared.Rent(columnCount)).AsSpan(0, columnCount);
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

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

                Span<int> temporary = previous;
                previous = current;
                current = temporary;
                current.Clear();
            }

            int similarity = commonLength + previous[second.Length];
            return similarity >= scoreCutoff && previous[second.Length] >= adjustedCutoff ? similarity : 0;
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

    public static EditOperations LcsEditops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        int[,] similarities = BuildLcsMatrix(first, second);
        List<EditOp> operations = [];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int row = first.Length;
        int column = second.Length;

        while (row > 0 || column > 0)
        {
            if (row > 0 && column > 0 && comparer.Equals(first[row - 1], second[column - 1]))
            {
                row--;
                column--;
            }
            else if (column > 0 && (row == 0 || similarities[row, column - 1] >= similarities[row - 1, column]))
            {
                column--;
                operations.Add(new EditOp(EditOperation.Insert, row, column));
            }
            else
            {
                row--;
                operations.Add(new EditOp(EditOperation.Delete, row, column));
            }
        }

        operations.Reverse();
        return new EditOperations(operations, first.Length, second.Length);
    }

    public static int HammingDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);
        ValidateHammingLengths(first.Length, second.Length, pad);

        int length = Math.Min(first.Length, second.Length);
        int distance = Math.Abs(first.Length - second.Length);
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int i = 0; i < length; i++)
        {
            if (!comparer.Equals(first[i], second[i]))
            {
                distance++;

                if (distance > scoreCutoff)
                {
                    return scoreCutoff == int.MaxValue ? distance : scoreCutoff + 1;
                }
            }
        }

        return DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
    }

    public static EditOperations HammingEditops<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad)
        where T : notnull, IEquatable<T>
    {
        ValidateHammingLengths(first.Length, second.Length, pad);

        List<EditOp> operations = [];
        int length = Math.Min(first.Length, second.Length);
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int i = 0; i < length; i++)
        {
            if (!comparer.Equals(first[i], second[i]))
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

    public static int PrefixSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        int length = Math.Min(first.Length, second.Length);
        int similarity = 0;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        while (similarity < length && comparer.Equals(first[similarity], second[similarity]))
        {
            similarity++;
        }

        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int PostfixSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        int length = Math.Min(first.Length, second.Length);
        int similarity = 0;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        while (similarity < length && comparer.Equals(first[first.Length - similarity - 1], second[second.Length - similarity - 1]))
        {
            similarity++;
        }

        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int OsaDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(second.Length, scoreCutoff);
        }

        if (second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(first.Length, scoreCutoff);
        }

        if (second.Length > first.Length)
        {
            ReadOnlySpan<T> temporary = first;
            first = second;
            second = temporary;
        }

        int columnCount = second.Length + 1;
        int storageLength = checked(columnCount * 3);
        int[]? rented = null;
        Span<int> storage = columnCount <= StackLimit
            ? stackalloc int[storageLength]
            : (rented = ArrayPool<int>.Shared.Rent(storageLength)).AsSpan(0, storageLength);

        try
        {
            Span<int> previousPrevious = storage[..columnCount];
            Span<int> previous = storage.Slice(columnCount, columnCount);
            Span<int> current = storage.Slice(columnCount * 2, columnCount);
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int column = 0; column < columnCount; column++)
            {
                previous[column] = column;
            }

            for (int row = 1; row <= first.Length; row++)
            {
                current[0] = row;
                int rowMinimum = current[0];

                for (int column = 1; column <= second.Length; column++)
                {
                    int substitutionCost = comparer.Equals(first[row - 1], second[column - 1]) ? 0 : 1;
                    int deletion = previous[column] + 1;
                    int insertion = current[column - 1] + 1;
                    int substitution = previous[column - 1] + substitutionCost;
                    int value = Math.Min(Math.Min(deletion, insertion), substitution);

                    if (row > 1
                        && column > 1
                        && comparer.Equals(first[row - 1], second[column - 2])
                        && comparer.Equals(first[row - 2], second[column - 1]))
                    {
                        value = Math.Min(value, previousPrevious[column - 2] + 1);
                    }

                    current[column] = value;
                    rowMinimum = Math.Min(rowMinimum, value);
                }

                if (rowMinimum > scoreCutoff)
                {
                    return DistanceHelpers.ApplyDistanceCutoff(rowMinimum, scoreCutoff);
                }

                Span<int> temporary = previousPrevious;
                previousPrevious = previous;
                previous = current;
                current = temporary;
            }

            return DistanceHelpers.ApplyDistanceCutoff(previous[second.Length], scoreCutoff);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    public static int DamerauLevenshteinDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateScoreCutoff(scoreCutoff);

        if (first.SequenceEqual(second))
        {
            return 0;
        }

        int minimumDistance = Math.Abs(first.Length - second.Length);

        if (minimumDistance > scoreCutoff)
        {
            return DistanceHelpers.ApplyDistanceCutoff(minimumDistance, scoreCutoff);
        }

        TrimCommonAffixes(ref first, ref second);

        if (first.IsEmpty || second.IsEmpty)
        {
            return DistanceHelpers.ApplyDistanceCutoff(Math.Max(first.Length, second.Length), scoreCutoff);
        }

        if (second.Length > first.Length)
        {
            ReadOnlySpan<T> temporary = first;
            first = second;
            second = temporary;
        }

        int rowSize = second.Length + 2;
        int storageLength = checked(rowSize * 3);
        int[]? rented = null;
        Span<int> storage = rowSize <= StackLimit
            ? stackalloc int[storageLength]
            : (rented = ArrayPool<int>.Shared.Rent(storageLength)).AsSpan(0, storageLength);

        try
        {
            Span<int> fr = storage[..rowSize];
            Span<int> previous = storage.Slice(rowSize, rowSize);
            Span<int> current = storage.Slice(rowSize * 2, rowSize);
            int maximumValue = Math.Max(first.Length, second.Length) + 1;
            Dictionary<T, int> lastRows = [];
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            fr.Fill(maximumValue);
            previous.Fill(maximumValue);
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
                T firstValue = first[row - 1];

                for (int column = 1; column <= second.Length; column++)
                {
                    T secondValue = second[column - 1];
                    bool equal = comparer.Equals(firstValue, secondValue);
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
                        int lastMatchingRow = lastRows.TryGetValue(secondValue, out int matchedRow) ? matchedRow : -1;

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

                lastRows[firstValue] = row;
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

    public static double JaroSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);

        if (scoreCutoff > 1.0)
        {
            return 0.0;
        }

        if (first.IsEmpty && second.IsEmpty)
        {
            return 1.0;
        }

        if (first.IsEmpty || second.IsEmpty)
        {
            return 0.0;
        }

        int matchDistance = Math.Max(Math.Max(first.Length, second.Length) / 2 - 1, 0);
        bool[] firstMatches = new bool[first.Length];
        bool[] secondMatches = new bool[second.Length];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int matches = 0;

        for (int firstIndex = 0; firstIndex < first.Length; firstIndex++)
        {
            int start = Math.Max(0, firstIndex - matchDistance);
            int end = Math.Min(firstIndex + matchDistance + 1, second.Length);

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

        int transpositions = 0;
        int matchedSecondIndex = 0;

        for (int firstIndex = 0; firstIndex < first.Length; firstIndex++)
        {
            if (!firstMatches[firstIndex])
            {
                continue;
            }

            while (!secondMatches[matchedSecondIndex])
            {
                matchedSecondIndex++;
            }

            if (!comparer.Equals(first[firstIndex], second[matchedSecondIndex]))
            {
                transpositions++;
            }

            matchedSecondIndex++;
        }

        double matchCount = matches;
        int halfTranspositions = transpositions / 2;
        double similarity = ((matchCount / first.Length)
            + (matchCount / second.Length)
            + ((matchCount - halfTranspositions) / matchCount)) / 3.0;

        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static int CommonPrefixLength<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int maximumLength)
        where T : notnull, IEquatable<T>
    {
        int maximum = Math.Min(Math.Min(first.Length, second.Length), maximumLength);
        int length = 0;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        while (length < maximum && comparer.Equals(first[length], second[length]))
        {
            length++;
        }

        return length;
    }

    public static int TrimCommonAffixes<T>(ref ReadOnlySpan<T> first, ref ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        int prefixLength = CommonPrefixLength(first, second, int.MaxValue);

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

    public static void ValidateHammingLengths(int firstLength, int secondLength, bool pad)
    {
        if (!pad && firstLength != secondLength)
        {
            throw new ArgumentException("Sequences must have equal length when padding is disabled.");
        }
    }

    private static int[,] BuildLevenshteinMatrix<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        int[,] distances = new int[first.Length + 1, second.Length + 1];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

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
                int substitutionCost = comparer.Equals(first[row - 1], second[column - 1]) ? 0 : 1;
                int deletion = distances[row - 1, column] + 1;
                int insertion = distances[row, column - 1] + 1;
                int substitution = distances[row - 1, column - 1] + substitutionCost;
                distances[row, column] = Math.Min(Math.Min(deletion, insertion), substitution);
            }
        }

        return distances;
    }

    private static int CommonSuffixLength<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        int maximum = Math.Min(first.Length, second.Length);
        int length = 0;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        while (length < maximum && comparer.Equals(first[^(length + 1)], second[^(length + 1)]))
        {
            length++;
        }

        return length;
    }

    private static int[,] BuildLcsMatrix<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
        where T : notnull, IEquatable<T>
    {
        int[,] similarities = new int[first.Length + 1, second.Length + 1];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int row = 1; row <= first.Length; row++)
        {
            for (int column = 1; column <= second.Length; column++)
            {
                similarities[row, column] = comparer.Equals(first[row - 1], second[column - 1])
                    ? similarities[row - 1, column - 1] + 1
                    : Math.Max(similarities[row - 1, column], similarities[row, column - 1]);
            }
        }

        return similarities;
    }
}
