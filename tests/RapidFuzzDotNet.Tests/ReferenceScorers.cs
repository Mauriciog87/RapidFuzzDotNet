namespace RapidFuzzDotNet.Tests;

public static class ReferenceScorers
{
    public static int LevenshteinDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        int columnCount = second.Length + 1;
        int[] previous = new int[columnCount];
        int[] current = new int[columnCount];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int column = 0; column < columnCount; column++)
        {
            previous[column] = column;
        }

        for (int row = 1; row <= first.Length; row++)
        {
            current[0] = row;

            for (int column = 1; column <= second.Length; column++)
            {
                int substitutionCost = comparer.Equals(first[row - 1], second[column - 1]) ? 0 : 1;
                int deletion = previous[column] + 1;
                int insertion = current[column - 1] + 1;
                int substitution = previous[column - 1] + substitutionCost;
                current[column] = Math.Min(Math.Min(deletion, insertion), substitution);
            }

            int[] temporary = previous;
            previous = current;
            current = temporary;
        }

        int distance = previous[second.Length];
        return distance <= scoreCutoff ? distance : scoreCutoff + 1;
    }

    public static int LcsSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = 0)
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

        int similarity = similarities[first.Length, second.Length];
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public static int IndelDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        int similarity = LcsSimilarity(first, second);
        int distance = first.Length + second.Length - (2 * similarity);
        return distance <= scoreCutoff ? distance : scoreCutoff + 1;
    }

    public static int HammingDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, bool pad, int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        if (!pad && first.Length != second.Length)
        {
            throw new ArgumentException("Sequences must have equal length when padding is disabled.");
        }

        int length = Math.Min(first.Length, second.Length);
        int distance = Math.Abs(first.Length - second.Length);
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int index = 0; index < length; index++)
        {
            if (!comparer.Equals(first[index], second[index]))
            {
                distance++;
            }
        }

        return distance <= scoreCutoff ? distance : scoreCutoff + 1;
    }

    public static int OsaDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int scoreCutoff = int.MaxValue)
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
                int distance = Math.Min(Math.Min(deletion, insertion), substitution);

                if (row > 1
                    && column > 1
                    && comparer.Equals(first[row - 1], second[column - 2])
                    && comparer.Equals(first[row - 2], second[column - 1]))
                {
                    distance = Math.Min(distance, distances[row - 2, column - 2] + 1);
                }

                distances[row, column] = distance;
            }
        }

        int result = distances[first.Length, second.Length];
        return result <= scoreCutoff ? result : scoreCutoff + 1;
    }

    public static int DamerauLevenshteinDistance<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        int scoreCutoff = int.MaxValue)
        where T : notnull, IEquatable<T>
    {
        int maximumDistance = first.Length + second.Length;
        int[,] matrix = new int[first.Length + 2, second.Length + 2];
        Dictionary<T, int> lastRows = new();
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        matrix[0, 0] = maximumDistance;

        for (int row = 0; row <= first.Length; row++)
        {
            matrix[row + 1, 0] = maximumDistance;
            matrix[row + 1, 1] = row;
        }

        for (int column = 0; column <= second.Length; column++)
        {
            matrix[0, column + 1] = maximumDistance;
            matrix[1, column + 1] = column;
        }

        for (int row = 1; row <= first.Length; row++)
        {
            int lastMatchColumn = 0;

            for (int column = 1; column <= second.Length; column++)
            {
                int lastMatchingRow = lastRows.TryGetValue(second[column - 1], out int storedRow) ? storedRow : 0;
                int previousMatchColumn = lastMatchColumn;
                int substitutionCost = 1;

                if (comparer.Equals(first[row - 1], second[column - 1]))
                {
                    substitutionCost = 0;
                    lastMatchColumn = column;
                }

                int substitution = matrix[row, column] + substitutionCost;
                int insertion = matrix[row + 1, column] + 1;
                int deletion = matrix[row, column + 1] + 1;
                int transposition = matrix[lastMatchingRow, previousMatchColumn]
                    + (row - lastMatchingRow - 1)
                    + 1
                    + (column - previousMatchColumn - 1);
                matrix[row + 1, column + 1] = Math.Min(Math.Min(substitution, insertion), Math.Min(deletion, transposition));
            }

            lastRows[first[row - 1]] = row;
        }

        int result = matrix[first.Length + 1, second.Length + 1];
        return result <= scoreCutoff ? result : scoreCutoff + 1;
    }

    public static double JaroSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
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
        double similarity = ((matchCount / first.Length)
            + (matchCount / second.Length)
            + ((matchCount - (transpositions / 2)) / matchCount)) / 3.0;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double JaroWinklerSimilarity<T>(
        ReadOnlySpan<T> first,
        ReadOnlySpan<T> second,
        double prefixWeight = 0.1,
        double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        double jaroSimilarity = JaroSimilarity(first, second, scoreCutoff);

        if (jaroSimilarity == 0.0)
        {
            return 0.0;
        }

        int prefixLength = CommonPrefixLength(first, second, 4);
        double similarity = jaroSimilarity + (prefixLength * prefixWeight * (1.0 - jaroSimilarity));
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double Ratio<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        int maximum = first.Length + second.Length;

        if (maximum == 0)
        {
            return 100.0;
        }

        int distance = IndelDistance(first, second);
        double similarity = (1.0 - ((double)distance / maximum)) * 100.0;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double PartialRatio<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        if (first.Length == 0 && second.Length == 0)
        {
            return 100.0;
        }

        if (first.Length == 0 || second.Length == 0)
        {
            return 0.0;
        }

        if (first.Length > second.Length)
        {
            return PartialRatio(second, first, scoreCutoff);
        }

        if (first.Length == second.Length)
        {
            return Math.Max(PartialRatioImpl(first, second, scoreCutoff), PartialRatioImpl(second, first, scoreCutoff));
        }

        return PartialRatioImpl(first, second, scoreCutoff);
    }

    private static double PartialRatioImpl<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        double result = 0.0;

        for (int offset = -first.Length; offset < second.Length; offset++)
        {
            int start = Math.Max(0, offset);
            int end = Math.Min(second.Length, offset + first.Length);

            if (end <= start)
            {
                continue;
            }

            double score = Ratio(first, second.Slice(start, end - start), scoreCutoff);

            if (score > result)
            {
                result = score;
            }
        }

        return result >= scoreCutoff ? result : 0.0;
    }

    private static int CommonPrefixLength<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int maximumLength)
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
}
