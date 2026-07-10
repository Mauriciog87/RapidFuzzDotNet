namespace RapidFuzz.Internal;

internal static class StringHelpers
{
    public static string SortTokens(ReadOnlySpan<char> value)
    {
        return CreateTokenizedString(value).Sorted;
    }

    public static TokenSet CreateTokenSet(ReadOnlySpan<char> value)
    {
        return CreateTokenizedString(value).TokenSet;
    }

    public static TokenizedString CreateTokenizedString(ReadOnlySpan<char> value)
    {
        return CreateTokenizedString(value.ToString());
    }

    public static TokenizedString CreateTokenizedString(string value)
    {
        int tokenCount = CountTokens(value);

        if (tokenCount == 0)
        {
            return TokenizedString.Empty;
        }

        TokenRange[] sortedTokens = CreateTokenRanges(value, tokenCount);
        Array.Sort(sortedTokens, TokenRangeComparer.Instance);
        TokenRange[] uniqueTokens = CreateUniqueTokens(sortedTokens);

        return new TokenizedString(sortedTokens, new TokenSet(uniqueTokens));
    }

    public static List<string> Tokenize(ReadOnlySpan<char> value)
    {
        int tokenCount = CountTokens(value);

        if (tokenCount == 0)
        {
            return [];
        }

        string source = value.ToString();
        TokenRange[] tokens = CreateTokenRanges(source, tokenCount);
        List<string> result = new(tokenCount);

        for (int i = 0; i < tokens.Length; i++)
        {
            result.Add(tokens[i].ToString());
        }

        return result;
    }

    private static TokenRange[] CreateTokenRanges(string source, int tokenCount)
    {
        TokenRange[] tokens = new TokenRange[tokenCount];
        int tokenIndex = 0;
        int tokenStart = -1;

        for (int i = 0; i < source.Length; i++)
        {
            if (char.IsWhiteSpace(source[i]))
            {
                AddToken(source, tokens, ref tokenIndex, tokenStart, i);
                tokenStart = -1;
            }
            else if (tokenStart < 0)
            {
                tokenStart = i;
            }
        }

        AddToken(source, tokens, ref tokenIndex, tokenStart, source.Length);
        return tokens;
    }

    private static TokenRange[] CreateUniqueTokens(TokenRange[] sortedTokens)
    {
        int uniqueCount = 0;

        for (int i = 0; i < sortedTokens.Length; i++)
        {
            if (i == 0 || !sortedTokens[i - 1].ContentEquals(sortedTokens[i]))
            {
                uniqueCount++;
            }
        }

        TokenRange[] uniqueTokens = new TokenRange[uniqueCount];
        int uniqueIndex = 0;

        for (int i = 0; i < sortedTokens.Length; i++)
        {
            if (i == 0 || !sortedTokens[i - 1].ContentEquals(sortedTokens[i]))
            {
                uniqueTokens[uniqueIndex] = sortedTokens[i];
                uniqueIndex++;
            }
        }

        return uniqueTokens;
    }

    private static int CountTokens(ReadOnlySpan<char> value)
    {
        int count = 0;
        bool insideToken = false;

        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                insideToken = false;
            }
            else if (!insideToken)
            {
                count++;
                insideToken = true;
            }
        }

        return count;
    }

    public static string JoinSorted(IEnumerable<string> tokens)
    {
        List<string> sortedTokens = tokens.ToList();
        sortedTokens.Sort(StringComparer.Ordinal);
        return string.Join(' ', sortedTokens);
    }

    public static TokenSetParts CompareTokenSets(TokenSet first, TokenSet second)
    {
        List<TokenRange> intersection = [];
        List<TokenRange> firstDifference = [];
        List<TokenRange> secondDifference = [];
        int firstIndex = 0;
        int secondIndex = 0;

        while (firstIndex < first.Tokens.Count && secondIndex < second.Tokens.Count)
        {
            TokenRange firstToken = first.Tokens[firstIndex];
            TokenRange secondToken = second.Tokens[secondIndex];
            int comparison = TokenRangeComparer.Instance.Compare(firstToken, secondToken);

            if (comparison == 0)
            {
                intersection.Add(firstToken);
                firstIndex++;
                secondIndex++;
            }
            else if (comparison < 0)
            {
                firstDifference.Add(firstToken);
                firstIndex++;
            }
            else
            {
                secondDifference.Add(secondToken);
                secondIndex++;
            }
        }

        while (firstIndex < first.Tokens.Count)
        {
            firstDifference.Add(first.Tokens[firstIndex]);
            firstIndex++;
        }

        while (secondIndex < second.Tokens.Count)
        {
            secondDifference.Add(second.Tokens[secondIndex]);
            secondIndex++;
        }

        return new TokenSetParts(
            intersection,
            firstDifference,
            secondDifference);
    }

    private static void AddToken(string source, TokenRange[] tokens, ref int tokenIndex, int tokenStart, int tokenEnd)
    {
        if (tokenStart >= 0 && tokenEnd > tokenStart)
        {
            tokens[tokenIndex] = new TokenRange(source, tokenStart, tokenEnd - tokenStart);
            tokenIndex++;
        }
    }

    internal static string JoinOrdered(IReadOnlyList<TokenRange> first, IReadOnlyList<TokenRange> second)
    {
        if (first.Count == 0)
        {
            return JoinTokens(second);
        }

        if (second.Count == 0)
        {
            return JoinTokens(first);
        }

        return JoinTokens(first, second);
    }

    internal static string JoinTokens(IReadOnlyList<TokenRange> tokens)
    {
        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        return string.Create(MeasureJoinedTokens(tokens), tokens, WriteTokens);
    }

    private static string JoinTokens(IReadOnlyList<TokenRange> first, IReadOnlyList<TokenRange> second)
    {
        int size = MeasureJoinedTokens(first) + MeasureJoinedTokens(second) + 1;
        return string.Create(size, (First: first, Second: second), WriteTokenPair);
    }

    private static void WriteTokens(Span<char> destination, IReadOnlyList<TokenRange> tokens)
    {
        int position = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            if (i > 0)
            {
                destination[position] = ' ';
                position++;
            }

            tokens[i].Span.CopyTo(destination[position..]);
            position += tokens[i].Length;
        }
    }

    private static void WriteTokenPair(
        Span<char> destination,
        (IReadOnlyList<TokenRange> First, IReadOnlyList<TokenRange> Second) tokens)
    {
        int position = WriteTokens(destination, tokens.First, 0, false);
        WriteTokens(destination, tokens.Second, position, true);
    }

    private static int WriteTokens(Span<char> destination, IReadOnlyList<TokenRange> tokens, int position, bool prependSpace)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (prependSpace || i > 0)
            {
                destination[position] = ' ';
                position++;
            }

            tokens[i].Span.CopyTo(destination[position..]);
            position += tokens[i].Length;
        }

        return position;
    }

    internal static int MeasureJoinedTokens(IReadOnlyList<TokenRange> tokens)
    {
        int size = Math.Max(0, tokens.Count - 1);

        for (int i = 0; i < tokens.Count; i++)
        {
            size += tokens[i].Length;
        }

        return size;
    }
}

internal readonly struct TokenRange
{
    public TokenRange(string source, int start, int length)
    {
        Source = source;
        Start = start;
        Length = length;
    }

    public string Source { get; }

    public int Start { get; }

    public int Length { get; }

    public ReadOnlySpan<char> Span => Source.AsSpan(Start, Length);

    public bool ContentEquals(TokenRange other)
    {
        return Span.SequenceEqual(other.Span);
    }

    public override string ToString()
    {
        return Span.ToString();
    }
}

internal sealed class TokenRangeComparer : IComparer<TokenRange>
{
    public static readonly TokenRangeComparer Instance = new();

    public int Compare(TokenRange first, TokenRange second)
    {
        int length = Math.Min(first.Length, second.Length);

        for (int i = 0; i < length; i++)
        {
            int comparison = first.Span[i].CompareTo(second.Span[i]);

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return first.Length.CompareTo(second.Length);
    }
}

internal sealed class TokenSet
{
    public static readonly TokenSet Empty = new(Array.Empty<TokenRange>());

    private string? joined;

    public TokenSet(IReadOnlyList<TokenRange> tokens)
    {
        Tokens = tokens;
        Size = StringHelpers.MeasureJoinedTokens(tokens);
    }

    public IReadOnlyList<TokenRange> Tokens { get; }

    public int WordCount => Tokens.Count;

    public int Size { get; }

    public string Joined => joined ??= StringHelpers.JoinTokens(Tokens);
}

internal sealed class TokenizedString
{
    public static readonly TokenizedString Empty = new(Array.Empty<TokenRange>(), TokenSet.Empty);

    private string? sorted;

    public TokenizedString(IReadOnlyList<TokenRange> sortedTokens, TokenSet tokenSet)
    {
        SortedTokens = sortedTokens;
        TokenSet = tokenSet;
        Size = StringHelpers.MeasureJoinedTokens(sortedTokens);
    }

    public IReadOnlyList<TokenRange> SortedTokens { get; }

    public int WordCount => SortedTokens.Count;

    public int Size { get; }

    public string Sorted => sorted ??= StringHelpers.JoinTokens(SortedTokens);

    public IReadOnlyList<TokenRange> UniqueTokens => TokenSet.Tokens;

    public TokenSet TokenSet { get; }
}

internal sealed class TokenSetParts
{
    private string? sortedIntersection;
    private string? combinedFirst;
    private string? combinedSecond;

    public TokenSetParts(
        IReadOnlyList<TokenRange> intersection,
        IReadOnlyList<TokenRange> firstDifference,
        IReadOnlyList<TokenRange> secondDifference)
    {
        Intersection = intersection;
        FirstDifference = firstDifference;
        SecondDifference = secondDifference;
    }

    public IReadOnlyList<TokenRange> Intersection { get; }

    public IReadOnlyList<TokenRange> FirstDifference { get; }

    public IReadOnlyList<TokenRange> SecondDifference { get; }

    public string SortedIntersection => sortedIntersection ??= StringHelpers.JoinTokens(Intersection);

    public string CombinedFirst => combinedFirst ??= StringHelpers.JoinOrdered(Intersection, FirstDifference);

    public string CombinedSecond => combinedSecond ??= StringHelpers.JoinOrdered(Intersection, SecondDifference);
}
