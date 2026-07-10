using System.Buffers;
using System.Numerics;

namespace RapidFuzz.Internal;

internal readonly struct PatternMatchVector
{
    private readonly Dictionary<char, ulong> masks;

    public PatternMatchVector(ReadOnlySpan<char> pattern)
    {
        if (pattern.Length > MaximumPatternLength)
        {
            throw new ArgumentOutOfRangeException(nameof(pattern), "The pattern length is too large for a single bit vector.");
        }

        masks = new Dictionary<char, ulong>();

        for (int i = 0; i < pattern.Length; i++)
        {
            masks.TryGetValue(pattern[i], out ulong mask);
            masks[pattern[i]] = mask | (1UL << i);
        }
    }

    public const int MaximumPatternLength = 64;

    public ulong this[char value]
    {
        get => GetMask(value);
    }

    internal ulong GetMask(char value)
    {
        masks.TryGetValue(value, out ulong mask);
        return mask;
    }

    public int LcsSimilarity(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        ulong state = 0;

        for (int i = 0; i < text.Length; i++)
        {
            ulong matches = this[text[i]] | state;
            ulong shifted = (state << 1) | 1UL;
            state = matches & ~(matches - shifted);
        }

        return BitOperations.PopCount(state);
    }

    public int LcsSimilarityUnrolled(ReadOnlySpan<char> text, int scoreCutoff)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        ulong state = 0;
        int index = 0;

        for (; index + 7 < text.Length; index += 8)
        {
            state = AdvanceLcsState(state, text[index]);
            state = AdvanceLcsState(state, text[index + 1]);
            state = AdvanceLcsState(state, text[index + 2]);
            state = AdvanceLcsState(state, text[index + 3]);
            state = AdvanceLcsState(state, text[index + 4]);
            state = AdvanceLcsState(state, text[index + 5]);
            state = AdvanceLcsState(state, text[index + 6]);
            state = AdvanceLcsState(state, text[index + 7]);

            if (scoreCutoff > 0)
            {
                int currentSimilarity = BitOperations.PopCount(state);
                int remaining = text.Length - index - 8;

                if (currentSimilarity + remaining < scoreCutoff)
                {
                    return 0;
                }
            }
        }

        for (; index < text.Length; index++)
        {
            state = AdvanceLcsState(state, text[index]);
        }

        int similarity = BitOperations.PopCount(state);
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public int LevenshteinDistance(int patternLength, ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return patternLength;
        }

        ulong positive = ulong.MaxValue;
        ulong negative = 0;
        int score = patternLength;
        ulong highBit = 1UL << (patternLength - 1);

        for (int i = 0; i < text.Length; i++)
        {
            ulong equal = this[text[i]];
            ulong vertical = equal | negative;
            ulong horizontal = (((equal & positive) + positive) ^ positive) | equal;
            ulong positiveHorizontal = negative | ~(horizontal | positive);
            ulong negativeHorizontal = positive & horizontal;

            if ((positiveHorizontal & highBit) != 0)
            {
                score++;
            }
            else if ((negativeHorizontal & highBit) != 0)
            {
                score--;
            }

            positiveHorizontal = (positiveHorizontal << 1) | 1UL;
            negativeHorizontal <<= 1;
            positive = negativeHorizontal | ~(vertical | positiveHorizontal);
            negative = positiveHorizontal & vertical;
        }

        return score;
    }

    public static int LcsSimilarity(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        if (pattern.IsEmpty || text.IsEmpty)
        {
            return 0;
        }

        PatternMatchVector vector = new(pattern);
        return vector.LcsSimilarityUnrolled(text, 0);
    }

    public static int LevenshteinDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        if (pattern.IsEmpty)
        {
            return text.Length;
        }

        PatternMatchVector vector = new(pattern);
        return vector.LevenshteinDistance(pattern.Length, text);
    }

    private ulong AdvanceLcsState(ulong state, char value)
    {
        ulong matches = this[value] | state;
        ulong shifted = (state << 1) | 1UL;
        return matches & ~(matches - shifted);
    }
}

internal sealed class BlockPatternMatchVector
{
    private const int BlockSize = 64;
    private const int AsciiAlphabetSize = 256;
    private const int StackBlockLimit = 32;
    private readonly ulong[] asciiMasks;
    private readonly Dictionary<char, ulong[]> masks;
    private readonly ulong lastBlockMask;

    public BlockPatternMatchVector(ReadOnlySpan<char> pattern)
    {
        Length = pattern.Length;
        BlockCount = (pattern.Length + BlockSize - 1) / BlockSize;
        asciiMasks = new ulong[AsciiAlphabetSize * BlockCount];
        masks = new Dictionary<char, ulong[]>();
        int lastBlockBits = pattern.Length & (BlockSize - 1);
        lastBlockMask = lastBlockBits == 0 ? ulong.MaxValue : (1UL << lastBlockBits) - 1UL;

        for (int i = 0; i < pattern.Length; i++)
        {
            int block = i / BlockSize;
            ulong mask = 1UL << (i & (BlockSize - 1));
            char value = pattern[i];

            if (value < AsciiAlphabetSize)
            {
                asciiMasks[(value * BlockCount) + block] |= mask;
            }
            else
            {
                if (!masks.TryGetValue(value, out ulong[]? existing))
                {
                    existing = new ulong[BlockCount];
                    masks[value] = existing;
                }

                existing[block] |= mask;
            }
        }
    }

    public int Length { get; }

    internal int BlockCount { get; }

    public int LcsSimilarity(ReadOnlySpan<char> text)
    {
        return LcsSimilarityBlockwise(text, 0);
    }

    public int LcsSimilarityBlockwise(ReadOnlySpan<char> text, int scoreCutoff)
    {
        if (Length == 0 || text.IsEmpty)
        {
            return 0;
        }

        Span<ulong> state = BlockCount <= StackBlockLimit ? stackalloc ulong[BlockCount] : new ulong[BlockCount];

        for (int i = 0; i < text.Length; i++)
        {
            char value = text[i];
            ulong shiftCarry = 1UL;
            ulong borrow = 0UL;

            for (int block = 0; block < BlockCount; block++)
            {
                ulong stateBlock = state[block];
                ulong matches = GetMask(value, block) | stateBlock;
                ulong shifted = (stateBlock << 1) | shiftCarry;
                shiftCarry = stateBlock >> (BlockSize - 1);
                ulong difference = matches - shifted - borrow;
                ulong nextBorrow = matches < shifted || (borrow != 0UL && matches == shifted) ? 1UL : 0UL;
                state[block] = (matches & ~difference) & ValidMask(block);
                borrow = nextBorrow;
            }

            if (scoreCutoff > 0 && (i & 7) == 7)
            {
                int currentSimilarity = PopCount(state);
                int remaining = text.Length - i - 1;

                if (currentSimilarity + remaining < scoreCutoff)
                {
                    return 0;
                }
            }
        }

        int similarity = PopCount(state);
        return similarity >= scoreCutoff ? similarity : 0;
    }

    public int LevenshteinDistance(ReadOnlySpan<char> text)
    {
        if (Length == 0)
        {
            return text.Length;
        }

        if (text.IsEmpty)
        {
            return Length;
        }

        Span<ulong> positive = BlockCount <= StackBlockLimit ? stackalloc ulong[BlockCount] : new ulong[BlockCount];
        Span<ulong> negative = BlockCount <= StackBlockLimit ? stackalloc ulong[BlockCount] : new ulong[BlockCount];
        Span<ulong> horizontal = BlockCount <= StackBlockLimit ? stackalloc ulong[BlockCount] : new ulong[BlockCount];
        Span<ulong> positiveHorizontal = BlockCount <= StackBlockLimit ? stackalloc ulong[BlockCount] : new ulong[BlockCount];
        Span<ulong> negativeHorizontal = BlockCount <= StackBlockLimit ? stackalloc ulong[BlockCount] : new ulong[BlockCount];
        positive.Fill(ulong.MaxValue);
        positive[^1] &= lastBlockMask;
        int highBlock = (Length - 1) / BlockSize;
        ulong highBit = 1UL << ((Length - 1) & (BlockSize - 1));
        int score = Length;

        for (int i = 0; i < text.Length; i++)
        {
            char value = text[i];
            ulong carry = 0UL;

            for (int block = 0; block < BlockCount; block++)
            {
                ulong equal = GetMask(value, block);
                ulong addend = equal & positive[block];
                ulong sum = addend + positive[block] + carry;
                ulong nextCarry = sum < addend || (carry != 0UL && sum == addend) ? 1UL : 0UL;
                horizontal[block] = ((sum ^ positive[block]) | equal) & ValidMask(block);
                carry = nextCarry;
            }

            for (int block = 0; block < BlockCount; block++)
            {
                ulong validMask = ValidMask(block);
                ulong equal = GetMask(value, block);
                ulong vertical = (equal | negative[block]) & validMask;
                positiveHorizontal[block] = (negative[block] | ~(horizontal[block] | positive[block])) & validMask;
                negativeHorizontal[block] = (positive[block] & horizontal[block]) & validMask;
            }

            if ((positiveHorizontal[highBlock] & highBit) != 0UL)
            {
                score++;
            }
            else if ((negativeHorizontal[highBlock] & highBit) != 0UL)
            {
                score--;
            }

            ShiftLeftOne(positiveHorizontal, 1UL);
            ShiftLeftOne(negativeHorizontal, 0UL);

            for (int block = 0; block < BlockCount; block++)
            {
                ulong validMask = ValidMask(block);
                ulong equal = GetMask(value, block);
                ulong vertical = (equal | negative[block]) & validMask;
                positive[block] = (negativeHorizontal[block] | ~(vertical | positiveHorizontal[block])) & validMask;
                negative[block] = positiveHorizontal[block] & vertical;
            }
        }

        return score;
    }

    internal ulong GetMask(char value, int block)
    {
        if (value < AsciiAlphabetSize)
        {
            return asciiMasks[(value * BlockCount) + block];
        }

        return masks.TryGetValue(value, out ulong[]? mask) ? mask[block] : 0UL;
    }

    private ulong ValidMask(int block)
    {
        return block == BlockCount - 1 ? lastBlockMask : ulong.MaxValue;
    }

    private void ShiftLeftOne(Span<ulong> values, ulong initialCarry)
    {
        ulong carry = initialCarry;

        for (int block = 0; block < values.Length; block++)
        {
            ulong nextCarry = values[block] >> (BlockSize - 1);
            values[block] = ((values[block] << 1) | carry) & ValidMask(block);
            carry = nextCarry;
        }
    }

    private static int PopCount(ReadOnlySpan<ulong> values)
    {
        int count = 0;

        for (int i = 0; i < values.Length; i++)
        {
            count += BitOperations.PopCount(values[i]);
        }

        return count;
    }
}

internal sealed class GenericPatternMatchVector<T>
    where T : notnull, IEquatable<T>
{
    private const int BlockSize = 64;
    private const int StackBlockLimit = 32;
    private readonly Dictionary<T, int> symbolIndexes;
    private readonly ulong[] masks;
    private readonly ulong lastBlockMask;

    public GenericPatternMatchVector(ReadOnlySpan<T> pattern)
    {
        Length = pattern.Length;
        BlockCount = (pattern.Length + BlockSize - 1) / BlockSize;
        symbolIndexes = [];

        for (int index = 0; index < pattern.Length; index++)
        {
            if (!symbolIndexes.ContainsKey(pattern[index]))
            {
                symbolIndexes[pattern[index]] = symbolIndexes.Count;
            }
        }

        masks = new ulong[symbolIndexes.Count * BlockCount];
        int lastBlockBits = pattern.Length & (BlockSize - 1);
        lastBlockMask = lastBlockBits == 0 ? ulong.MaxValue : (1UL << lastBlockBits) - 1UL;

        for (int index = 0; index < pattern.Length; index++)
        {
            int symbolIndex = symbolIndexes[pattern[index]];
            int block = index / BlockSize;
            masks[(symbolIndex * BlockCount) + block] |= 1UL << (index & (BlockSize - 1));
        }
    }

    public int Length { get; }

    public int BlockCount { get; }

    public ulong GetMask(T value, int block)
    {
        return symbolIndexes.TryGetValue(value, out int symbolIndex)
            ? masks[(symbolIndex * BlockCount) + block]
            : 0UL;
    }

    public int LcsSimilarity(ReadOnlySpan<T> text, int scoreCutoff)
    {
        if (Length == 0 || text.IsEmpty)
        {
            return 0;
        }

        ulong[]? rented = null;
        Span<ulong> state = BlockCount <= StackBlockLimit
            ? stackalloc ulong[BlockCount]
            : (rented = ArrayPool<ulong>.Shared.Rent(BlockCount)).AsSpan(0, BlockCount);
        state.Clear();

        try
        {
            for (int index = 0; index < text.Length; index++)
            {
                ulong shiftCarry = 1UL;
                ulong borrow = 0UL;

                for (int block = 0; block < BlockCount; block++)
                {
                    ulong stateBlock = state[block];
                    ulong matches = GetMask(text[index], block) | stateBlock;
                    ulong shifted = (stateBlock << 1) | shiftCarry;
                    shiftCarry = stateBlock >> (BlockSize - 1);
                    ulong difference = matches - shifted - borrow;
                    ulong nextBorrow = matches < shifted || (borrow != 0UL && matches == shifted) ? 1UL : 0UL;
                    state[block] = (matches & ~difference) & ValidMask(block);
                    borrow = nextBorrow;
                }

                if (scoreCutoff > 0 && (index & 7) == 7)
                {
                    int currentSimilarity = PopCount(state);
                    int remaining = text.Length - index - 1;

                    if (currentSimilarity + remaining < scoreCutoff)
                    {
                        return 0;
                    }
                }
            }

            int similarity = PopCount(state);
            return similarity >= scoreCutoff ? similarity : 0;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<ulong>.Shared.Return(rented);
            }
        }
    }

    public int LevenshteinDistance(ReadOnlySpan<T> text)
    {
        if (Length == 0)
        {
            return text.Length;
        }

        if (text.IsEmpty)
        {
            return Length;
        }

        int storageLength = checked(BlockCount * 5);
        ulong[]? rented = null;
        Span<ulong> storage = BlockCount <= StackBlockLimit
            ? stackalloc ulong[storageLength]
            : (rented = ArrayPool<ulong>.Shared.Rent(storageLength)).AsSpan(0, storageLength);

        try
        {
            Span<ulong> positive = storage[..BlockCount];
            Span<ulong> negative = storage.Slice(BlockCount, BlockCount);
            Span<ulong> horizontal = storage.Slice(BlockCount * 2, BlockCount);
            Span<ulong> positiveHorizontal = storage.Slice(BlockCount * 3, BlockCount);
            Span<ulong> negativeHorizontal = storage.Slice(BlockCount * 4, BlockCount);
            positive.Fill(ulong.MaxValue);
            positive[^1] &= lastBlockMask;
            negative.Clear();
            int highBlock = (Length - 1) / BlockSize;
            ulong highBit = 1UL << ((Length - 1) & (BlockSize - 1));
            int score = Length;

            for (int index = 0; index < text.Length; index++)
            {
                ulong carry = 0UL;

                for (int block = 0; block < BlockCount; block++)
                {
                    ulong equal = GetMask(text[index], block);
                    ulong addend = equal & positive[block];
                    ulong sum = addend + positive[block] + carry;
                    ulong nextCarry = sum < addend || (carry != 0UL && sum == addend) ? 1UL : 0UL;
                    horizontal[block] = ((sum ^ positive[block]) | equal) & ValidMask(block);
                    carry = nextCarry;
                }

                for (int block = 0; block < BlockCount; block++)
                {
                    ulong validMask = ValidMask(block);
                    ulong equal = GetMask(text[index], block);
                    ulong vertical = (equal | negative[block]) & validMask;
                    positiveHorizontal[block] = (negative[block] | ~(horizontal[block] | positive[block])) & validMask;
                    negativeHorizontal[block] = (positive[block] & horizontal[block]) & validMask;
                }

                if ((positiveHorizontal[highBlock] & highBit) != 0UL)
                {
                    score++;
                }
                else if ((negativeHorizontal[highBlock] & highBit) != 0UL)
                {
                    score--;
                }

                ShiftLeftOne(positiveHorizontal, 1UL);
                ShiftLeftOne(negativeHorizontal, 0UL);

                for (int block = 0; block < BlockCount; block++)
                {
                    ulong validMask = ValidMask(block);
                    ulong equal = GetMask(text[index], block);
                    ulong vertical = (equal | negative[block]) & validMask;
                    positive[block] = (negativeHorizontal[block] | ~(vertical | positiveHorizontal[block])) & validMask;
                    negative[block] = positiveHorizontal[block] & vertical;
                }
            }

            return score;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<ulong>.Shared.Return(rented);
            }
        }
    }

    public int OsaDistance(ReadOnlySpan<T> text, int scoreCutoff)
    {
        if (Length == 0)
        {
            return Distance.DistanceHelpers.ApplyDistanceCutoff(text.Length, scoreCutoff);
        }

        if (text.IsEmpty)
        {
            return Distance.DistanceHelpers.ApplyDistanceCutoff(Length, scoreCutoff);
        }

        int rowCount = BlockCount + 1;
        GenericOsaBlockRow[] rentedRows = ArrayPool<GenericOsaBlockRow>.Shared.Rent(rowCount * 2);
        Span<GenericOsaBlockRow> previousRows = rentedRows.AsSpan(0, rowCount);
        Span<GenericOsaBlockRow> currentRows = rentedRows.AsSpan(rowCount, rowCount);
        previousRows.Clear();
        currentRows.Clear();

        try
        {
            for (int index = 0; index < rowCount; index++)
            {
                previousRows[index].Positive = ulong.MaxValue;
                currentRows[index].Positive = ulong.MaxValue;
            }

            ulong lastBit = 1UL << ((Length - 1) & (BlockSize - 1));
            int distance = Length;

            for (int row = 0; row < text.Length; row++)
            {
                ulong positiveCarry = 1UL;
                ulong negativeCarry = 0UL;

                for (int block = 0; block < BlockCount; block++)
                {
                    ulong negative = previousRows[block + 1].Negative;
                    ulong positive = previousRows[block + 1].Positive;
                    ulong diagonal = previousRows[block + 1].Diagonal;
                    ulong previousDiagonal = previousRows[block].Diagonal;
                    ulong previousPatternMask = previousRows[block + 1].PatternMask;
                    ulong previousBlockPatternMask = currentRows[block].PatternMask;
                    ulong patternMask = GetMask(text[row], block);
                    ulong transposition = ((((~diagonal) & patternMask) << 1)
                        | (((~previousDiagonal) & previousBlockPatternMask) >> 63)) & previousPatternMask;
                    ulong diagonalInput = patternMask | negativeCarry;

                    diagonal = (((diagonalInput & positive) + positive) ^ positive)
                        | diagonalInput
                        | negative
                        | transposition;

                    ulong positiveHorizontal = negative | ~(diagonal | positive);
                    ulong negativeHorizontal = diagonal & positive;

                    if (block == BlockCount - 1)
                    {
                        if ((positiveHorizontal & lastBit) != 0UL)
                        {
                            distance++;
                        }
                        else if ((negativeHorizontal & lastBit) != 0UL)
                        {
                            distance--;
                        }
                    }

                    ulong previousPositiveCarry = positiveCarry;
                    positiveCarry = positiveHorizontal >> 63;
                    positiveHorizontal = (positiveHorizontal << 1) | previousPositiveCarry;

                    ulong previousNegativeCarry = negativeCarry;
                    negativeCarry = negativeHorizontal >> 63;
                    negativeHorizontal = (negativeHorizontal << 1) | previousNegativeCarry;

                    currentRows[block + 1].Positive = negativeHorizontal | ~(diagonal | positiveHorizontal);
                    currentRows[block + 1].Negative = positiveHorizontal & diagonal;
                    currentRows[block + 1].Diagonal = diagonal;
                    currentRows[block + 1].PatternMask = patternMask;
                }

                Span<GenericOsaBlockRow> temporary = previousRows;
                previousRows = currentRows;
                currentRows = temporary;
            }

            return Distance.DistanceHelpers.ApplyDistanceCutoff(distance, scoreCutoff);
        }
        finally
        {
            ArrayPool<GenericOsaBlockRow>.Shared.Return(rentedRows, clearArray: true);
        }
    }

    private ulong ValidMask(int block)
    {
        return block == BlockCount - 1 ? lastBlockMask : ulong.MaxValue;
    }

    private void ShiftLeftOne(Span<ulong> values, ulong initialCarry)
    {
        ulong carry = initialCarry;

        for (int block = 0; block < values.Length; block++)
        {
            ulong nextCarry = values[block] >> (BlockSize - 1);
            values[block] = ((values[block] << 1) | carry) & ValidMask(block);
            carry = nextCarry;
        }
    }

    private static int PopCount(ReadOnlySpan<ulong> values)
    {
        int count = 0;

        for (int index = 0; index < values.Length; index++)
        {
            count += BitOperations.PopCount(values[index]);
        }

        return count;
    }

    private struct GenericOsaBlockRow
    {
        public ulong Positive;
        public ulong Negative;
        public ulong Diagonal;
        public ulong PatternMask;
    }
}
