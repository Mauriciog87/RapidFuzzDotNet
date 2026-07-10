using System.Buffers;
using System.Numerics;

namespace RapidFuzz.Internal;

internal static class PooledGenericPatternMetrics
{
    private const int BlockSize = 64;
    private const int StackBlockLimit = 32;
    private const int StackMatchLimit = 256;

    public static double JaroSimilarity<T>(ReadOnlySpan<T> source, ReadOnlySpan<T> target, double scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        if (scoreCutoff > 1.0)
        {
            return 0.0;
        }

        if (source.IsEmpty && target.IsEmpty)
        {
            return 1.0;
        }

        if (source.IsEmpty || target.IsEmpty)
        {
            return 0.0;
        }

        if (source.Length == 1 && target.Length == 1)
        {
            return EqualityComparer<T>.Default.Equals(source[0], target[0]) ? 1.0 : 0.0;
        }

        PooledPatternIndex<T> index = new(source);
        int flagCount = index.BlockCount;
        int matchCapacity = Math.Min(source.Length, target.Length);
        ulong[]? rentedFlags = null;
        int[]? rentedMatches = null;
        Span<ulong> sourceFlags = flagCount <= StackMatchLimit
            ? stackalloc ulong[flagCount]
            : (rentedFlags = ArrayPool<ulong>.Shared.Rent(flagCount)).AsSpan(0, flagCount);
        Span<int> targetMatches = matchCapacity <= StackMatchLimit
            ? stackalloc int[matchCapacity]
            : (rentedMatches = ArrayPool<int>.Shared.Rent(matchCapacity)).AsSpan(0, matchCapacity);
        sourceFlags.Clear();

        try
        {
            int matchDistance = Math.Max(Math.Max(source.Length, target.Length) / 2 - 1, 0);
            int matches = 0;

            for (int targetIndex = 0; targetIndex < target.Length; targetIndex++)
            {
                int start = Math.Max(0, targetIndex - matchDistance);
                int stop = Math.Min(source.Length, targetIndex + matchDistance + 1);
                int sourceIndex = FindMatch(ref index, target[targetIndex], start, stop, sourceFlags);

                if (sourceIndex < 0)
                {
                    continue;
                }

                sourceFlags[sourceIndex / BlockSize] |= 1UL << (sourceIndex & (BlockSize - 1));
                targetMatches[matches++] = targetIndex;
            }

            if (matches == 0)
            {
                return 0.0;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            int transpositions = 0;
            int matchIndex = 0;

            for (int block = 0; block < sourceFlags.Length; block++)
            {
                ulong flags = sourceFlags[block];

                while (flags != 0UL)
                {
                    int sourceIndex = (block * BlockSize) + BitOperations.TrailingZeroCount(flags);

                    if (!comparer.Equals(source[sourceIndex], target[targetMatches[matchIndex]]))
                    {
                        transpositions++;
                    }

                    matchIndex++;
                    flags &= flags - 1UL;
                }
            }

            double matchCount = matches;
            int halfTranspositions = transpositions / 2;
            double similarity = ((matchCount / source.Length)
                + (matchCount / target.Length)
                + ((matchCount - halfTranspositions) / matchCount)) / 3.0;
            return similarity >= scoreCutoff ? similarity : 0.0;
        }
        finally
        {
            if (rentedFlags is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedFlags);
            }

            if (rentedMatches is not null)
            {
                ArrayPool<int>.Shared.Return(rentedMatches);
            }

            index.Dispose();
        }
    }

    public static int LcsSimilarity<T>(ReadOnlySpan<T> pattern, ReadOnlySpan<T> text, int scoreCutoff)
        where T : notnull, IEquatable<T>
    {
        if (pattern.IsEmpty || text.IsEmpty)
        {
            return 0;
        }

        PooledPatternIndex<T> index = new(pattern);
        int blockCount = index.BlockCount;
        ulong[]? rentedState = null;
        Span<ulong> state = blockCount <= StackBlockLimit
            ? stackalloc ulong[blockCount]
            : (rentedState = ArrayPool<ulong>.Shared.Rent(blockCount)).AsSpan(0, blockCount);
        state.Clear();

        try
        {
            for (int textIndex = 0; textIndex < text.Length; textIndex++)
            {
                ulong shiftCarry = 1UL;
                ulong borrow = 0UL;

                for (int block = 0; block < blockCount; block++)
                {
                    ulong stateBlock = state[block];
                    ulong matches = index.GetMask(text[textIndex], block) | stateBlock;
                    ulong shifted = (stateBlock << 1) | shiftCarry;
                    shiftCarry = stateBlock >> (BlockSize - 1);
                    ulong difference = matches - shifted - borrow;
                    ulong nextBorrow = matches < shifted || (borrow != 0UL && matches == shifted) ? 1UL : 0UL;
                    state[block] = (matches & ~difference) & ValidMask(pattern.Length, blockCount, block);
                    borrow = nextBorrow;
                }

                if (scoreCutoff > 0 && (textIndex & 7) == 7)
                {
                    int currentSimilarity = PopCount(state);
                    int remaining = text.Length - textIndex - 1;

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
            if (rentedState is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedState);
            }

            index.Dispose();
        }
    }

    public static int LevenshteinDistance<T>(ReadOnlySpan<T> pattern, ReadOnlySpan<T> text)
        where T : notnull, IEquatable<T>
    {
        if (pattern.IsEmpty)
        {
            return text.Length;
        }

        if (text.IsEmpty)
        {
            return pattern.Length;
        }

        PooledPatternIndex<T> index = new(pattern);
        int blockCount = index.BlockCount;
        int storageLength = checked(blockCount * 5);
        ulong[]? rentedStorage = null;
        Span<ulong> storage = blockCount <= StackBlockLimit
            ? stackalloc ulong[storageLength]
            : (rentedStorage = ArrayPool<ulong>.Shared.Rent(storageLength)).AsSpan(0, storageLength);

        try
        {
            Span<ulong> positive = storage[..blockCount];
            Span<ulong> negative = storage.Slice(blockCount, blockCount);
            Span<ulong> horizontal = storage.Slice(blockCount * 2, blockCount);
            Span<ulong> positiveHorizontal = storage.Slice(blockCount * 3, blockCount);
            Span<ulong> negativeHorizontal = storage.Slice(blockCount * 4, blockCount);
            positive.Fill(ulong.MaxValue);
            positive[^1] &= LastBlockMask(pattern.Length);
            negative.Clear();
            int highBlock = (pattern.Length - 1) / BlockSize;
            ulong highBit = 1UL << ((pattern.Length - 1) & (BlockSize - 1));
            int score = pattern.Length;

            for (int textIndex = 0; textIndex < text.Length; textIndex++)
            {
                ulong carry = 0UL;

                for (int block = 0; block < blockCount; block++)
                {
                    ulong equal = index.GetMask(text[textIndex], block);
                    ulong addend = equal & positive[block];
                    ulong sum = addend + positive[block] + carry;
                    ulong nextCarry = sum < addend || (carry != 0UL && sum == addend) ? 1UL : 0UL;
                    horizontal[block] = ((sum ^ positive[block]) | equal) & ValidMask(pattern.Length, blockCount, block);
                    carry = nextCarry;
                }

                for (int block = 0; block < blockCount; block++)
                {
                    ulong validMask = ValidMask(pattern.Length, blockCount, block);
                    ulong equal = index.GetMask(text[textIndex], block);
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

                for (int block = 0; block < blockCount; block++)
                {
                    ulong validMask = ValidMask(pattern.Length, blockCount, block);
                    ulong equal = index.GetMask(text[textIndex], block);
                    ulong vertical = (equal | negative[block]) & validMask;
                    positive[block] = (negativeHorizontal[block] | ~(vertical | positiveHorizontal[block])) & validMask;
                    negative[block] = positiveHorizontal[block] & vertical;
                }
            }

            return score;
        }
        finally
        {
            if (rentedStorage is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedStorage);
            }

            index.Dispose();
        }
    }

    private static ulong ValidMask(int patternLength, int blockCount, int block)
    {
        return block == blockCount - 1 ? LastBlockMask(patternLength) : ulong.MaxValue;
    }

    private static ulong LastBlockMask(int patternLength)
    {
        int lastBlockBits = patternLength & (BlockSize - 1);
        return lastBlockBits == 0 ? ulong.MaxValue : (1UL << lastBlockBits) - 1UL;
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

    private static void ShiftLeftOne(Span<ulong> values, ulong carry)
    {
        for (int index = 0; index < values.Length; index++)
        {
            ulong nextCarry = values[index] >> (BlockSize - 1);
            values[index] = (values[index] << 1) | carry;
            carry = nextCarry;
        }
    }

    private static int FindMatch<T>(
        scoped ref PooledPatternIndex<T> index,
        T value,
        int start,
        int stop,
        scoped ReadOnlySpan<ulong> sourceFlags)
        where T : notnull, IEquatable<T>
    {
        if (start >= stop)
        {
            return -1;
        }

        int firstBlock = start / BlockSize;
        int lastBlock = (stop - 1) / BlockSize;

        for (int block = firstBlock; block <= lastBlock; block++)
        {
            ulong rangeMask = ulong.MaxValue;

            if (block == firstBlock)
            {
                rangeMask &= ulong.MaxValue << (start & (BlockSize - 1));
            }

            if (block == lastBlock && (stop & (BlockSize - 1)) != 0)
            {
                rangeMask &= (1UL << (stop & (BlockSize - 1))) - 1UL;
            }

            ulong candidates = index.GetMask(value, block) & rangeMask & ~sourceFlags[block];

            if (candidates != 0UL)
            {
                return (block * BlockSize) + BitOperations.TrailingZeroCount(candidates);
            }
        }

        return -1;
    }

    private ref struct PooledPatternIndex<T>
        where T : notnull, IEquatable<T>
    {
        private readonly int[] buckets;
        private readonly T[] keys;
        private readonly ulong[] masks;
        private readonly int bucketLength;
        private readonly int maskLength;
        private readonly EqualityComparer<T> comparer;
        private int symbolCount;

        public PooledPatternIndex(ReadOnlySpan<T> pattern)
        {
            BlockCount = (pattern.Length + BlockSize - 1) / BlockSize;
            bucketLength = GetBucketLength(pattern.Length);
            maskLength = checked(pattern.Length * BlockCount);
            buckets = ArrayPool<int>.Shared.Rent(bucketLength);
            keys = ArrayPool<T>.Shared.Rent(pattern.Length);
            masks = ArrayPool<ulong>.Shared.Rent(maskLength);
            comparer = EqualityComparer<T>.Default;
            symbolCount = 0;
            buckets.AsSpan(0, bucketLength).Clear();
            masks.AsSpan(0, maskLength).Clear();

            for (int patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
            {
                int symbolIndex = GetOrAdd(pattern[patternIndex]);
                int block = patternIndex / BlockSize;
                masks[(symbolIndex * BlockCount) + block] |= 1UL << (patternIndex & (BlockSize - 1));
            }
        }

        public int BlockCount { get; }

        public ulong GetMask(T value, int block)
        {
            int symbolIndex = Find(value);
            return symbolIndex >= 0 ? masks[(symbolIndex * BlockCount) + block] : 0UL;
        }

        public void Dispose()
        {
            ArrayPool<int>.Shared.Return(buckets);
            ArrayPool<T>.Shared.Return(keys, clearArray: true);
            ArrayPool<ulong>.Shared.Return(masks);
        }

        private int GetOrAdd(T value)
        {
            int bucket = GetBucket(value);

            while (buckets[bucket] != 0)
            {
                int symbolIndex = buckets[bucket] - 1;

                if (comparer.Equals(keys[symbolIndex], value))
                {
                    return symbolIndex;
                }

                bucket = (bucket + 1) & (bucketLength - 1);
            }

            int addedIndex = symbolCount++;
            keys[addedIndex] = value;
            buckets[bucket] = addedIndex + 1;
            return addedIndex;
        }

        private int Find(T value)
        {
            int bucket = GetBucket(value);

            while (buckets[bucket] != 0)
            {
                int symbolIndex = buckets[bucket] - 1;

                if (comparer.Equals(keys[symbolIndex], value))
                {
                    return symbolIndex;
                }

                bucket = (bucket + 1) & (bucketLength - 1);
            }

            return -1;
        }

        private int GetBucket(T value)
        {
            return comparer.GetHashCode(value) & (bucketLength - 1);
        }

        private static int GetBucketLength(int patternLength)
        {
            int minimum = checked(patternLength * 2);
            int length = 2;

            while (length < minimum)
            {
                length = checked(length * 2);
            }

            return length;
        }
    }
}
