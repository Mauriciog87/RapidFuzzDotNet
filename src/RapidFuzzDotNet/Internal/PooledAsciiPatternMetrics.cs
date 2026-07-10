using System.Buffers;
using System.Numerics;

namespace RapidFuzz.Internal;

internal static class PooledAsciiPatternMetrics
{
    private const int AlphabetSize = 256;
    private const int BlockSize = 64;
    private const int StackBlockLimit = 32;

    public static int LcsSimilarity(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, int scoreCutoff)
    {
        if (!IsAscii(pattern))
        {
            BlockPatternMatchVector vector = new(pattern);
            return vector.LcsSimilarityBlockwise(text, scoreCutoff);
        }

        int blockCount = (pattern.Length + BlockSize - 1) / BlockSize;
        int maskLength = checked(AlphabetSize * blockCount);
        ulong[] rentedMasks = ArrayPool<ulong>.Shared.Rent(maskLength);
        Span<ulong> masks = rentedMasks.AsSpan(0, maskLength);
        masks.Clear();
        ulong[]? rentedState = null;
        Span<ulong> state = blockCount <= StackBlockLimit
            ? stackalloc ulong[blockCount]
            : (rentedState = ArrayPool<ulong>.Shared.Rent(blockCount)).AsSpan(0, blockCount);
        state.Clear();
        ulong lastBlockMask = LastBlockMask(pattern.Length);

        try
        {
            FillMasks(pattern, masks, blockCount);

            for (int index = 0; index < text.Length; index++)
            {
                char value = text[index];
                ulong shiftCarry = 1UL;
                ulong borrow = 0UL;

                for (int block = 0; block < blockCount; block++)
                {
                    ulong stateBlock = state[block];
                    ulong matches = GetMask(masks, blockCount, value, block) | stateBlock;
                    ulong shifted = (stateBlock << 1) | shiftCarry;
                    shiftCarry = stateBlock >> (BlockSize - 1);
                    ulong difference = matches - shifted - borrow;
                    ulong nextBorrow = matches < shifted || (borrow != 0UL && matches == shifted) ? 1UL : 0UL;
                    state[block] = (matches & ~difference) & ValidMask(block, blockCount, lastBlockMask);
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
            ArrayPool<ulong>.Shared.Return(rentedMasks, clearArray: true);

            if (rentedState is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedState, clearArray: true);
            }
        }
    }

    public static int LevenshteinDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        if (!IsAscii(pattern))
        {
            return new BlockPatternMatchVector(pattern).LevenshteinDistance(text);
        }

        int blockCount = (pattern.Length + BlockSize - 1) / BlockSize;
        int maskLength = checked(AlphabetSize * blockCount);
        ulong[] rentedMasks = ArrayPool<ulong>.Shared.Rent(maskLength);
        Span<ulong> masks = rentedMasks.AsSpan(0, maskLength);
        masks.Clear();
        int storageLength = checked(blockCount * 5);
        ulong[]? rentedStorage = null;
        Span<ulong> storage = blockCount <= StackBlockLimit
            ? stackalloc ulong[storageLength]
            : (rentedStorage = ArrayPool<ulong>.Shared.Rent(storageLength)).AsSpan(0, storageLength);
        storage.Clear();
        ulong lastBlockMask = LastBlockMask(pattern.Length);

        try
        {
            FillMasks(pattern, masks, blockCount);
            Span<ulong> positive = storage[..blockCount];
            Span<ulong> negative = storage.Slice(blockCount, blockCount);
            Span<ulong> horizontal = storage.Slice(blockCount * 2, blockCount);
            Span<ulong> positiveHorizontal = storage.Slice(blockCount * 3, blockCount);
            Span<ulong> negativeHorizontal = storage.Slice(blockCount * 4, blockCount);
            positive.Fill(ulong.MaxValue);
            positive[^1] &= lastBlockMask;
            int highBlock = (pattern.Length - 1) / BlockSize;
            ulong highBit = 1UL << ((pattern.Length - 1) & (BlockSize - 1));
            int score = pattern.Length;

            for (int index = 0; index < text.Length; index++)
            {
                char value = text[index];
                ulong carry = 0UL;

                for (int block = 0; block < blockCount; block++)
                {
                    ulong equal = GetMask(masks, blockCount, value, block);
                    ulong addend = equal & positive[block];
                    ulong sum = addend + positive[block] + carry;
                    ulong nextCarry = sum < addend || (carry != 0UL && sum == addend) ? 1UL : 0UL;
                    horizontal[block] = ((sum ^ positive[block]) | equal) & ValidMask(block, blockCount, lastBlockMask);
                    carry = nextCarry;
                }

                for (int block = 0; block < blockCount; block++)
                {
                    ulong validMask = ValidMask(block, blockCount, lastBlockMask);
                    ulong equal = GetMask(masks, blockCount, value, block);
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

                ShiftLeftOne(positiveHorizontal, 1UL, blockCount, lastBlockMask);
                ShiftLeftOne(negativeHorizontal, 0UL, blockCount, lastBlockMask);

                for (int block = 0; block < blockCount; block++)
                {
                    ulong validMask = ValidMask(block, blockCount, lastBlockMask);
                    ulong equal = GetMask(masks, blockCount, value, block);
                    ulong vertical = (equal | negative[block]) & validMask;
                    positive[block] = (negativeHorizontal[block] | ~(vertical | positiveHorizontal[block])) & validMask;
                    negative[block] = positiveHorizontal[block] & vertical;
                }
            }

            return score;
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(rentedMasks, clearArray: true);

            if (rentedStorage is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedStorage, clearArray: true);
            }
        }
    }

    private static bool IsAscii(ReadOnlySpan<char> pattern)
    {
        for (int index = 0; index < pattern.Length; index++)
        {
            if (pattern[index] >= AlphabetSize)
            {
                return false;
            }
        }

        return true;
    }

    private static void FillMasks(ReadOnlySpan<char> pattern, Span<ulong> masks, int blockCount)
    {
        for (int index = 0; index < pattern.Length; index++)
        {
            int block = index / BlockSize;
            masks[(pattern[index] * blockCount) + block] |= 1UL << (index & (BlockSize - 1));
        }
    }

    private static ulong GetMask(ReadOnlySpan<ulong> masks, int blockCount, char value, int block)
    {
        return value < AlphabetSize ? masks[(value * blockCount) + block] : 0UL;
    }

    private static ulong LastBlockMask(int patternLength)
    {
        int lastBlockBits = patternLength & (BlockSize - 1);
        return lastBlockBits == 0 ? ulong.MaxValue : (1UL << lastBlockBits) - 1UL;
    }

    private static ulong ValidMask(int block, int blockCount, ulong lastBlockMask)
    {
        return block == blockCount - 1 ? lastBlockMask : ulong.MaxValue;
    }

    private static void ShiftLeftOne(Span<ulong> values, ulong initialCarry, int blockCount, ulong lastBlockMask)
    {
        ulong carry = initialCarry;

        for (int block = 0; block < values.Length; block++)
        {
            ulong nextCarry = values[block] >> (BlockSize - 1);
            values[block] = ((values[block] << 1) | carry) & ValidMask(block, blockCount, lastBlockMask);
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
}
