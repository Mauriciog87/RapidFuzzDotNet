using System.Buffers;
using System.Numerics;
using RapidFuzz.Internal;

namespace RapidFuzz.Distance;

public static partial class Jaro
{
    public static double Distance(string first, string second, double scoreCutoff = 1.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static double Distance(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Distance(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static double Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 1.0)
    {
        return Distance(first, second, scoreCutoff, 1.0);
    }

    public static double Distance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarityCutoff = scoreCutoff >= 1.0 ? 0.0 : 1.0 - scoreCutoff;
        double distance = 1.0 - SimilarityCore(first, second, similarityCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public static double Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, scoreCutoff, 1.0);
    }

    public static double Distance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff, double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarityCutoff = scoreCutoff >= 1.0 ? 0.0 : 1.0 - scoreCutoff;
        GenericJaroPattern<T> pattern = new(first);
        double distance = 1.0 - pattern.Similarity(second, similarityCutoff);
        return distance <= scoreCutoff ? distance : 1.0;
    }

    public static double Similarity(string first, string second, double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), scoreCutoff);
    }

    public static double Similarity(string first, string second, double scoreCutoff, double scoreHint)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return Similarity(first.AsSpan(), second.AsSpan(), scoreCutoff, scoreHint);
    }

    public static double Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        return Similarity(first, second, scoreCutoff, 0.0);
    }

    public static double Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarity = SimilarityCore(first, second, scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, scoreCutoff, 0.0);
    }

    public static double Similarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff, double scoreHint)
        where T : notnull, IEquatable<T>
    {
        DistanceHelpers.ValidateNormalizedCutoff(scoreCutoff);
        DistanceHelpers.ValidateNormalizedHint(scoreHint);

        double similarity = PooledGenericPatternMetrics.JaroSimilarity(first, second, scoreCutoff);
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff = 1.0)
    {
        return Distance(first, second, scoreCutoff);
    }

    public static double NormalizedDistance(string first, string second, double scoreCutoff, double scoreHint)
    {
        return Distance(first, second, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 1.0)
    {
        return Distance(first, second, scoreCutoff);
    }

    public static double NormalizedDistance(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        return Distance(first, second, scoreCutoff, scoreHint);
    }

    public static double NormalizedDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 1.0)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, scoreCutoff);
    }

    public static double NormalizedDistance<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff, double scoreHint)
        where T : notnull, IEquatable<T>
    {
        return Distance(first, second, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff = 0.0)
    {
        return Similarity(first, second, scoreCutoff);
    }

    public static double NormalizedSimilarity(string first, string second, double scoreCutoff, double scoreHint)
    {
        return Similarity(first, second, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff = 0.0)
    {
        return Similarity(first, second, scoreCutoff);
    }

    public static double NormalizedSimilarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff, double scoreHint)
    {
        return Similarity(first, second, scoreCutoff, scoreHint);
    }

    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff = 0.0)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, scoreCutoff);
    }

    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, double scoreCutoff, double scoreHint)
        where T : notnull, IEquatable<T>
    {
        return Similarity(first, second, scoreCutoff, scoreHint);
    }

    internal static double Similarity(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return SimilarityForCharacters(first, second, 0.0);
    }

    internal static double SimilarityForCharacters(ReadOnlySpan<char> first, ReadOnlySpan<char> second, double scoreCutoff)
    {
        return SimilarityCore(first, second, scoreCutoff);
    }

    internal static double Similarity(JaroPattern pattern, ReadOnlySpan<char> second, double scoreCutoff)
    {
        return SimilarityCore(pattern.Source.AsSpan(), second, scoreCutoff, pattern);
    }

    private static double SimilarityCore(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        double scoreCutoff,
        JaroPattern? cachedPattern = null)
    {
        if (scoreCutoff > 1.0)
        {
            return 0.0;
        }

        if (first.IsEmpty && second.IsEmpty)
        {
            return 1.0;
        }

        if (!PassesLengthFilter(first.Length, second.Length, scoreCutoff))
        {
            return 0.0;
        }

        int firstOriginalLength = first.Length;
        int secondOriginalLength = second.Length;

        if (firstOriginalLength == 1 && secondOriginalLength == 1)
        {
            return first[0] == second[0] ? 1.0 : 0.0;
        }

        int matchDistance = TrimOutOfBoundSuffix(ref first, ref second);
        int commonPrefixLength = cachedPattern is null ? CommonPrefixLength(first, second) : 0;

        if (commonPrefixLength > 0)
        {
            first = first[commonPrefixLength..];
            second = second[commonPrefixLength..];
        }

        if (first.IsEmpty || second.IsEmpty)
        {
            return CalculateSimilarity(firstOriginalLength, secondOriginalLength, commonPrefixLength, 0, scoreCutoff);
        }

        int flaggedCharacters;
        int transpositions;

        if (first.Length <= PatternMatchVector.MaximumPatternLength
            && second.Length <= PatternMatchVector.MaximumPatternLength
            && cachedPattern?.SmallPattern is PatternMatchVector cachedSmallPattern)
        {
            WordFlags flags = FlagSimilarCharacters(cachedSmallPattern, second, matchDistance);
            flaggedCharacters = BitOperations.PopCount(flags.PatternFlags);
            transpositions = CountTranspositions(cachedSmallPattern, second, flags);
        }
        else if (first.Length <= PatternMatchVector.MaximumPatternLength
            && second.Length <= PatternMatchVector.MaximumPatternLength
            && cachedPattern is null)
        {
            PatternMatchVector vector = new(first);
            WordFlags flags = FlagSimilarCharacters(vector, second, matchDistance);
            flaggedCharacters = BitOperations.PopCount(flags.PatternFlags);
            transpositions = CountTranspositions(vector, second, flags);
        }
        else
        {
            BlockPatternMatchVector vector = cachedPattern?.BlockPattern ?? new BlockPatternMatchVector(first);
            (flaggedCharacters, transpositions) = FlagAndCountBlock(vector, first.Length, second, matchDistance);
        }

        int matches = commonPrefixLength + flaggedCharacters;

        if (!PassesCommonCharacterFilter(firstOriginalLength, secondOriginalLength, matches, scoreCutoff))
        {
            return 0.0;
        }

        return CalculateSimilarity(firstOriginalLength, secondOriginalLength, matches, transpositions, scoreCutoff);
    }

    private static bool PassesLengthFilter(int firstLength, int secondLength, double scoreCutoff)
    {
        if (firstLength == 0 || secondLength == 0)
        {
            return false;
        }

        double minimumLength = Math.Min(firstLength, secondLength);
        double similarity = ((minimumLength / firstLength) + (minimumLength / secondLength) + 1.0) / 3.0;
        return similarity >= scoreCutoff;
    }

    private static bool PassesCommonCharacterFilter(int firstLength, int secondLength, int commonCharacters, double scoreCutoff)
    {
        if (commonCharacters == 0)
        {
            return false;
        }

        double similarity = (((double)commonCharacters / firstLength) + ((double)commonCharacters / secondLength) + 1.0) / 3.0;
        return similarity >= scoreCutoff;
    }

    private static int TrimOutOfBoundSuffix(ref ReadOnlySpan<char> first, ref ReadOnlySpan<char> second)
    {
        int matchDistance;

        if (second.Length > first.Length)
        {
            matchDistance = (second.Length / 2) - 1;
            int maximumSecondLength = first.Length + matchDistance;

            if (second.Length > maximumSecondLength)
            {
                second = second[..maximumSecondLength];
            }
        }
        else
        {
            matchDistance = (first.Length / 2) - 1;
            int maximumFirstLength = second.Length + matchDistance;

            if (first.Length > maximumFirstLength)
            {
                first = first[..maximumFirstLength];
            }
        }

        return Math.Max(matchDistance, 0);
    }

    private static int CommonPrefixLength(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return SimdSupport.CommonPrefixLength(first, second, int.MaxValue);
    }

    private static double CalculateSimilarity(
        int firstLength,
        int secondLength,
        int commonCharacters,
        int transpositions,
        double scoreCutoff)
    {
        if (commonCharacters == 0)
        {
            return 0.0;
        }

        double matchCount = commonCharacters;
        int halfTranspositions = transpositions / 2;
        double similarity = ((matchCount / firstLength)
            + (matchCount / secondLength)
            + ((matchCount - halfTranspositions) / matchCount)) / 3.0;
        return similarity >= scoreCutoff ? similarity : 0.0;
    }

    private static WordFlags FlagSimilarCharacters(PatternMatchVector vector, ReadOnlySpan<char> text, int bound)
    {
        ulong patternFlags = 0;
        ulong textFlags = 0;
        ulong boundMask = BitMaskLsb(bound + 1);
        int index = 0;
        int prefixLimit = Math.Min(bound, text.Length);

        for (; index < prefixLimit; index++)
        {
            ulong patternMask = vector.GetMask(text[index]) & boundMask & ~patternFlags;
            ulong matchingBit = LowestBit(patternMask);
            patternFlags |= matchingBit;

            if (patternMask != 0)
            {
                textFlags |= 1UL << index;
            }

            boundMask = (boundMask << 1) | 1UL;
        }

        for (; index < text.Length; index++)
        {
            ulong patternMask = vector.GetMask(text[index]) & boundMask & ~patternFlags;
            ulong matchingBit = LowestBit(patternMask);
            patternFlags |= matchingBit;

            if (patternMask != 0)
            {
                textFlags |= 1UL << index;
            }

            boundMask <<= 1;
        }

        return new WordFlags(patternFlags, textFlags);
    }

    private static int CountTranspositions(PatternMatchVector vector, ReadOnlySpan<char> text, WordFlags flags)
    {
        ulong patternFlags = flags.PatternFlags;
        ulong textFlags = flags.TextFlags;
        int transpositions = 0;

        while (textFlags != 0)
        {
            ulong patternFlag = LowestBit(patternFlags);
            int textIndex = BitOperations.TrailingZeroCount(textFlags);

            if ((vector.GetMask(text[textIndex]) & patternFlag) == 0)
            {
                transpositions++;
            }

            textFlags = ResetLowestBit(textFlags);
            patternFlags ^= patternFlag;
        }

        return transpositions;
    }

    private static (int CommonCharacters, int Transpositions) FlagAndCountBlock(
        BlockPatternMatchVector vector,
        int patternLength,
        ReadOnlySpan<char> text,
        int bound)
    {
        int patternWordCount = Math.Max(1, (patternLength + 63) / 64);
        int textWordCount = Math.Max(1, (text.Length + 63) / 64);
        ulong[] rentedPatternFlags = ArrayPool<ulong>.Shared.Rent(patternWordCount);
        ulong[] rentedTextFlags = ArrayPool<ulong>.Shared.Rent(textWordCount);
        Span<ulong> patternFlags = rentedPatternFlags.AsSpan(0, patternWordCount);
        Span<ulong> textFlags = rentedTextFlags.AsSpan(0, textWordCount);

        patternFlags.Clear();
        textFlags.Clear();

        try
        {
            FlagSimilarCharactersBlock(vector, patternLength, text, bound, patternFlags, textFlags);
            int commonCharacters = CountCommonCharacters(patternFlags, textFlags);
            int transpositions = CountTranspositionsBlock(vector, text, patternFlags, textFlags, commonCharacters);
            return (commonCharacters, transpositions);
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(rentedPatternFlags, clearArray: true);
            ArrayPool<ulong>.Shared.Return(rentedTextFlags, clearArray: true);
        }
    }

    private static void FlagSimilarCharactersBlock(
        BlockPatternMatchVector vector,
        int patternLength,
        ReadOnlySpan<char> text,
        int bound,
        Span<ulong> patternFlags,
        Span<ulong> textFlags)
    {
        SearchBoundMask mask = new()
        {
            WordCount = 1 + (Math.Min(bound + 1, patternLength) / 64),
            EmptyWords = 0,
            LastMask = BitMaskLsb(Math.Min(bound + 1, patternLength) & 63),
            FirstMask = ulong.MaxValue
        };

        for (int index = 0; index < text.Length; index++)
        {
            FlagSimilarCharactersBlockStep(vector, text[index], patternFlags, textFlags, index, mask);

            if (index + bound + 1 < patternLength)
            {
                mask.LastMask = (mask.LastMask << 1) | 1UL;

                if (index + bound + 2 < patternLength && mask.LastMask == ulong.MaxValue)
                {
                    mask.LastMask = 0;
                    mask.WordCount++;
                }
            }

            if (index >= bound)
            {
                mask.FirstMask <<= 1;

                if (mask.FirstMask == 0)
                {
                    mask.FirstMask = ulong.MaxValue;
                    mask.WordCount--;
                    mask.EmptyWords++;
                }
            }
        }
    }

    private static void FlagSimilarCharactersBlockStep(
        BlockPatternMatchVector vector,
        char value,
        Span<ulong> patternFlags,
        Span<ulong> textFlags,
        int textIndex,
        SearchBoundMask mask)
    {
        int textWord = textIndex / 64;
        int textPosition = textIndex & 63;
        int word = mask.EmptyWords;
        int lastWord = word + mask.WordCount;

        if (word >= patternFlags.Length)
        {
            return;
        }

        if (mask.WordCount == 1)
        {
            ulong patternMask = GetBlockMask(vector, value, word, patternFlags.Length)
                & mask.LastMask
                & mask.FirstMask
                & ~patternFlags[word];
            AddFlag(patternMask, patternFlags, textFlags, word, textWord, textPosition);
            return;
        }

        if (mask.FirstMask != 0)
        {
            ulong patternMask = GetBlockMask(vector, value, word, patternFlags.Length)
                & mask.FirstMask
                & ~patternFlags[word];

            if (patternMask != 0)
            {
                AddFlag(patternMask, patternFlags, textFlags, word, textWord, textPosition);
                return;
            }

            word++;

            if (word >= patternFlags.Length)
            {
                return;
            }
        }

        int stride = value < 256 ? SimdSupport.PreferredWordStride : 1;

        if (stride >= 4)
        {
            for (; word + 3 < lastWord - 1 && word + 3 < patternFlags.Length; word += 4)
            {
                ulong firstMask = vector.GetMask(value, word) & ~patternFlags[word];
                ulong secondMask = vector.GetMask(value, word + 1) & ~patternFlags[word + 1];
                ulong thirdMask = vector.GetMask(value, word + 2) & ~patternFlags[word + 2];
                ulong fourthMask = vector.GetMask(value, word + 3) & ~patternFlags[word + 3];

                if (firstMask != 0)
                {
                    AddFlag(firstMask, patternFlags, textFlags, word, textWord, textPosition);
                    return;
                }

                if (secondMask != 0)
                {
                    AddFlag(secondMask, patternFlags, textFlags, word + 1, textWord, textPosition);
                    return;
                }

                if (thirdMask != 0)
                {
                    AddFlag(thirdMask, patternFlags, textFlags, word + 2, textWord, textPosition);
                    return;
                }

                if (fourthMask != 0)
                {
                    AddFlag(fourthMask, patternFlags, textFlags, word + 3, textWord, textPosition);
                    return;
                }
            }
        }

        if (stride >= 2)
        {
            for (; word + 1 < lastWord - 1 && word + 1 < patternFlags.Length; word += 2)
            {
                ulong firstMask = vector.GetMask(value, word) & ~patternFlags[word];
                ulong secondMask = vector.GetMask(value, word + 1) & ~patternFlags[word + 1];

                if (firstMask != 0)
                {
                    AddFlag(firstMask, patternFlags, textFlags, word, textWord, textPosition);
                    return;
                }

                if (secondMask != 0)
                {
                    AddFlag(secondMask, patternFlags, textFlags, word + 1, textWord, textPosition);
                    return;
                }
            }
        }

        for (; word < lastWord - 1 && word < patternFlags.Length; word++)
        {
            ulong patternMask = GetBlockMask(vector, value, word, patternFlags.Length) & ~patternFlags[word];

            if (patternMask != 0)
            {
                AddFlag(patternMask, patternFlags, textFlags, word, textWord, textPosition);
                return;
            }
        }

        if (mask.LastMask != 0 && word < patternFlags.Length)
        {
            ulong patternMask = GetBlockMask(vector, value, word, patternFlags.Length)
                & mask.LastMask
                & ~patternFlags[word];
            AddFlag(patternMask, patternFlags, textFlags, word, textWord, textPosition);
        }
    }

    private static void AddFlag(
        ulong patternMask,
        Span<ulong> patternFlags,
        Span<ulong> textFlags,
        int patternWord,
        int textWord,
        int textPosition)
    {
        ulong matchingBit = LowestBit(patternMask);
        patternFlags[patternWord] |= matchingBit;

        if (patternMask != 0)
        {
            textFlags[textWord] |= 1UL << textPosition;
        }
    }

    private static int CountCommonCharacters(ReadOnlySpan<ulong> patternFlags, ReadOnlySpan<ulong> textFlags)
    {
        ReadOnlySpan<ulong> flags = patternFlags.Length < textFlags.Length ? patternFlags : textFlags;
        int commonCharacters = 0;

        for (int i = 0; i < flags.Length; i++)
        {
            commonCharacters += BitOperations.PopCount(flags[i]);
        }

        return commonCharacters;
    }

    private static int CountTranspositionsBlock(
        BlockPatternMatchVector vector,
        ReadOnlySpan<char> text,
        ReadOnlySpan<ulong> patternFlags,
        ReadOnlySpan<ulong> textFlags,
        int commonCharacters)
    {
        int textWord = 0;
        int patternWord = 0;
        int textWordStart = 0;
        ulong textFlag = textFlags[textWord];
        ulong patternFlag = patternFlags[patternWord];
        int transpositions = 0;
        int remainingCharacters = commonCharacters;

        while (remainingCharacters > 0)
        {
            while (textFlag == 0)
            {
                textWord++;
                textWordStart += 64;
                textFlag = textFlags[textWord];
            }

            while (textFlag != 0)
            {
                while (patternFlag == 0)
                {
                    patternWord++;
                    patternFlag = patternFlags[patternWord];
                }

                ulong currentPatternFlag = LowestBit(patternFlag);
                int textIndex = textWordStart + BitOperations.TrailingZeroCount(textFlag);

                if ((vector.GetMask(text[textIndex], patternWord) & currentPatternFlag) == 0)
                {
                    transpositions++;
                }

                textFlag = ResetLowestBit(textFlag);
                patternFlag ^= currentPatternFlag;
                remainingCharacters--;
            }
        }

        return transpositions;
    }

    private static ulong GetBlockMask(BlockPatternMatchVector vector, char value, int word, int wordCount)
    {
        return word < wordCount ? vector.GetMask(value, word) : 0;
    }

    private static ulong BitMaskLsb(int bitCount)
    {
        if (bitCount <= 0)
        {
            return 0;
        }

        return bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1UL;
    }

    private static ulong LowestBit(ulong value)
    {
        return value & (0UL - value);
    }

    private static ulong ResetLowestBit(ulong value)
    {
        return value & (value - 1UL);
    }

    private readonly record struct WordFlags(ulong PatternFlags, ulong TextFlags);

    private struct SearchBoundMask
    {
        public int WordCount;
        public int EmptyWords;
        public ulong LastMask;
        public ulong FirstMask;
    }
}

internal sealed class JaroPattern
{
    public JaroPattern(string source)
    {
        Source = source;
        BlockPattern = new BlockPatternMatchVector(source);

        if (source.Length <= PatternMatchVector.MaximumPatternLength)
        {
            SmallPattern = new PatternMatchVector(source);
        }
    }

    public string Source { get; }

    public PatternMatchVector? SmallPattern { get; }

    public BlockPatternMatchVector BlockPattern { get; }
}
