using RapidFuzz;
using RapidFuzz.Distance;

namespace RapidFuzzDotNet.Tests;

public sealed class GenericCachedAndFuzzingParityTests
{
    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamLevenshteinDistanceFuzzTargetMatchesReference(int[] source, int[] target)
    {
        int expected = ReferenceScorers.LevenshteinDistance(source, target);

        Assert.Equal(expected, Levenshtein.Distance(source, target));
        Assert.Equal(expected <= 2 ? expected : 3, Levenshtein.Distance(source, target, 2));
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamLevenshteinEditopsFuzzTargetRoundTrips(int[] source, int[] target)
    {
        EditOperations editops = Levenshtein.Editops(source, target);
        Opcodes opcodes = Levenshtein.Opcodes(source, target);

        Assert.Equal(target, editops.ApplyTo(source, target));
        Assert.Equal(target, opcodes.ApplyTo(source, target));
        Assert.Equal(ReferenceScorers.LevenshteinDistance(source, target), editops.Count);
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamIndelDistanceFuzzTargetMatchesReference(int[] source, int[] target)
    {
        int expected = ReferenceScorers.IndelDistance(source, target);

        Assert.Equal(expected, Indel.Distance(source, target));
        Assert.Equal(expected <= 4 ? expected : 5, Indel.Distance(source, target, 4));
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamIndelEditopsFuzzTargetRoundTrips(int[] source, int[] target)
    {
        EditOperations editops = Indel.Editops(source, target);
        Opcodes opcodes = Indel.Opcodes(source, target);

        Assert.Equal(target, editops.ApplyTo(source, target));
        Assert.Equal(target, opcodes.ApplyTo(source, target));
        Assert.Equal(ReferenceScorers.IndelDistance(source, target), editops.Count);
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamLcsSimilarityFuzzTargetMatchesReference(int[] source, int[] target)
    {
        int expected = ReferenceScorers.LcsSimilarity(source, target);

        Assert.Equal(expected, LcsSeq.Similarity(source, target));
        Assert.Equal(expected >= 2 ? expected : 0, LcsSeq.Similarity(source, target, 2));
        Assert.Equal(target, LcsSeq.Editops(source, target).ApplyTo(source, target));
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamOsaDistanceFuzzTargetMatchesReference(int[] source, int[] target)
    {
        int expected = ReferenceScorers.OsaDistance(source, target);

        Assert.Equal(expected, Osa.Distance(source, target));
        Assert.Equal(expected <= 3 ? expected : 4, Osa.Distance(source, target, 3));
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamDamerauLevenshteinDistanceFuzzTargetMatchesReference(int[] source, int[] target)
    {
        int expected = ReferenceScorers.DamerauLevenshteinDistance(source, target);

        Assert.Equal(expected, DamerauLevenshtein.Distance(source, target));
        Assert.Equal(expected <= 3 ? expected : 4, DamerauLevenshtein.Distance(source, target, 3));
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamJaroSimilarityFuzzTargetMatchesReference(int[] source, int[] target)
    {
        double expected = ReferenceScorers.JaroSimilarity(source, target);

        Assert.Equal(expected, Jaro.Similarity(source, target), 12);
        Assert.Equal(expected >= 0.75 ? expected : 0.0, Jaro.Similarity(source, target, 0.75), 12);
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamJaroWinklerSimilarityFuzzTargetMatchesReference(int[] source, int[] target)
    {
        double expected = ReferenceScorers.JaroWinklerSimilarity(source, target);

        Assert.Equal(expected, JaroWinkler.Similarity(source, target), 12);
        Assert.Equal(expected >= 0.75 ? expected : 0.0, JaroWinkler.Similarity(source, target, 0.1, 0.75, 0.75), 12);
    }

    [Theory]
    [MemberData(nameof(IntegerSequenceCases))]
    public void UpstreamPartialRatioFuzzTargetMatchesReference(int[] source, int[] target)
    {
        double expected = ReferenceScorers.PartialRatio(source, target);

        Assert.Equal(expected, Fuzz.PartialRatio(source, target), 10);
        Assert.Equal(expected, Fuzz.PartialRatio(target, source), 10);
        Assert.Equal(expected >= 80.0 ? expected : 0.0, Fuzz.PartialRatio(source, target, 80.0), 10);
    }

    [Fact]
    public void GenericCachedDistanceScorersMatchStaticScorers()
    {
        int[] source = [1, 2, 3, 5, 8, 13];
        int[] target = [1, 3, 5, 8, 21];

        CachedLevenshtein<int> levenshtein = new(source);
        CachedIndel<int> indel = new(source);
        CachedHamming<int> hamming = new(source);
        CachedLcsSeq<int> lcsSeq = new(source);
        CachedOsa<int> osa = new(source);
        CachedDamerauLevenshtein<int> damerauLevenshtein = new(source);
        CachedJaro<int> jaro = new(source);
        CachedJaroWinkler<int> jaroWinkler = new(source);
        CachedPrefix<int> prefix = new(source);
        CachedPostfix<int> postfix = new(source);

        Assert.Equal(Levenshtein.Distance(source, target), levenshtein.Distance(target));
        Assert.Equal(Levenshtein.Similarity(source, target), levenshtein.Similarity(target));
        Assert.Equal(Levenshtein.NormalizedDistance(source, target), levenshtein.NormalizedDistance(target), 12);
        Assert.Equal(Levenshtein.NormalizedSimilarity(source, target), levenshtein.NormalizedSimilarity(target), 12);
        Assert.Equal(target, levenshtein.Editops(target).ApplyTo(source, target));
        Assert.Equal(target, levenshtein.Opcodes(target).ApplyTo(source, target));
        Assert.Equal(Indel.Distance(source, target), indel.Distance(target));
        Assert.Equal(Indel.Similarity(source, target), indel.Similarity(target));
        Assert.Equal(Indel.NormalizedDistance(source, target), indel.NormalizedDistance(target), 12);
        Assert.Equal(Indel.NormalizedSimilarity(source, target), indel.NormalizedSimilarity(target), 12);
        Assert.Equal(target, indel.Editops(target).ApplyTo(source, target));
        Assert.Equal(target, indel.Opcodes(target).ApplyTo(source, target));
        Assert.Equal(Hamming.Distance(source, target), hamming.Distance(target));
        Assert.Equal(Hamming.Similarity(source, target), hamming.Similarity(target));
        Assert.Equal(Hamming.NormalizedDistance(source, target), hamming.NormalizedDistance(target), 12);
        Assert.Equal(Hamming.NormalizedSimilarity(source, target), hamming.NormalizedSimilarity(target), 12);
        Assert.Equal(target, hamming.Editops(target).ApplyTo(source, target));
        Assert.Equal(LcsSeq.Distance(source, target), lcsSeq.Distance(target));
        Assert.Equal(LcsSeq.Similarity(source, target), lcsSeq.Similarity(target));
        Assert.Equal(LcsSeq.NormalizedDistance(source, target), lcsSeq.NormalizedDistance(target), 12);
        Assert.Equal(LcsSeq.NormalizedSimilarity(source, target), lcsSeq.NormalizedSimilarity(target), 12);
        Assert.Equal(target, lcsSeq.Editops(target).ApplyTo(source, target));
        Assert.Equal(Osa.Distance(source, target), osa.Distance(target));
        Assert.Equal(Osa.Similarity(source, target), osa.Similarity(target));
        Assert.Equal(Osa.NormalizedDistance(source, target), osa.NormalizedDistance(target), 12);
        Assert.Equal(Osa.NormalizedSimilarity(source, target), osa.NormalizedSimilarity(target), 12);
        Assert.Equal(DamerauLevenshtein.Distance(source, target), damerauLevenshtein.Distance(target));
        Assert.Equal(DamerauLevenshtein.Similarity(source, target), damerauLevenshtein.Similarity(target));
        Assert.Equal(DamerauLevenshtein.NormalizedDistance(source, target), damerauLevenshtein.NormalizedDistance(target), 12);
        Assert.Equal(DamerauLevenshtein.NormalizedSimilarity(source, target), damerauLevenshtein.NormalizedSimilarity(target), 12);
        Assert.Equal(Jaro.Distance(source, target), jaro.Distance(target), 12);
        Assert.Equal(Jaro.Similarity(source, target), jaro.Similarity(target), 12);
        Assert.Equal(JaroWinkler.Distance(source, target), jaroWinkler.Distance(target), 12);
        Assert.Equal(JaroWinkler.Similarity(source, target), jaroWinkler.Similarity(target), 12);
        Assert.Equal(Prefix.Distance(source, target), prefix.Distance(target));
        Assert.Equal(Prefix.Similarity(source, target), prefix.Similarity(target));
        Assert.Equal(Postfix.Distance(source, target), postfix.Distance(target));
        Assert.Equal(Postfix.Similarity(source, target), postfix.Similarity(target));
    }

    [Fact]
    public void GenericCachedDistanceScorersCopySource()
    {
        int[] originalSource = [1, 2, 3, 4];
        int[] source = [1, 2, 3, 4];
        int[] target = [1, 2, 4];
        CachedLevenshtein<int> cached = new(source);

        source[2] = 99;

        Assert.Equal(1, cached.Distance(target));
        Assert.Equal(Levenshtein.Distance(originalSource, target), cached.Distance(target));
    }

    [Fact]
    public void GenericCachedEditopsApplyToMultipleSequenceTypes()
    {
        char[] charSource = ['a', 'b', 'c'];
        char[] charTarget = ['a', 'x', 'c', 'd'];
        byte[] byteSource = [1, 2, 3];
        byte[] byteTarget = [1, 3, 4];
        ComparableToken[] tokenSource = [new ComparableToken(1), new ComparableToken(2), new ComparableToken(3)];
        ComparableToken[] tokenTarget = [new ComparableToken(1), new ComparableToken(4), new ComparableToken(3)];

        CachedLevenshtein<char> charCached = new(charSource);
        CachedIndel<byte> byteCached = new(byteSource);
        CachedHamming<ComparableToken> tokenCached = new(tokenSource);

        Assert.Equal(charTarget, charCached.Opcodes(charTarget).ApplyTo(charSource, charTarget));
        Assert.Equal(byteTarget, byteCached.Opcodes(byteTarget).ApplyTo(byteSource, byteTarget));
        Assert.Equal(tokenTarget, tokenCached.Editops(tokenTarget).ApplyTo(tokenSource, tokenTarget));
    }

    [Fact]
    public void GenericCachedCutoffsAndScoreHintsMatchStaticScorers()
    {
        int[] source = [4, 8, 15, 16, 23, 42];
        int[] target = [4, 8, 15, 23, 42, 108];
        CachedLevenshtein<int> cachedLevenshtein = new(source);
        CachedIndel<int> cachedIndel = new(source);
        CachedJaro<int> cachedJaro = new(source);

        Assert.Equal(Levenshtein.Distance(source, target, 2, 1), cachedLevenshtein.Distance(target, 2, 1));
        Assert.Equal(Levenshtein.Similarity(source, target, 4, 3), cachedLevenshtein.Similarity(target, 4, 3));
        Assert.Equal(Indel.Distance(source, target, 4, 2), cachedIndel.Distance(target, 4, 2));
        Assert.Equal(Indel.NormalizedSimilarity(source, target, 0.7, 0.5), cachedIndel.NormalizedSimilarity(target, 0.7, 0.5), 12);
        Assert.Equal(Jaro.Similarity(source, target, 0.7, 0.5), cachedJaro.Similarity(target, 0.7, 0.5), 12);
    }

    [Fact]
    public void ReferenceScorersCoverBytesCharsAndRecordStructs()
    {
        byte[] byteSource = [1, 2, 3, 4];
        byte[] byteTarget = [1, 3, 4, 5];
        char[] charSource = ['n', 'e', 'w', ' ', 'y', 'o', 'r', 'k'];
        char[] charTarget = ['n', 'e', 'w', 'a', 'r', 'k'];
        ComparableToken[] tokenSource = [new ComparableToken(1), new ComparableToken(2), new ComparableToken(3)];
        ComparableToken[] tokenTarget = [new ComparableToken(1), new ComparableToken(3), new ComparableToken(5)];

        Assert.Equal(ReferenceScorers.LevenshteinDistance(byteSource, byteTarget), Levenshtein.Distance(byteSource, byteTarget));
        Assert.Equal(ReferenceScorers.IndelDistance(charSource, charTarget), Indel.Distance(charSource, charTarget));
        Assert.Equal(ReferenceScorers.OsaDistance(tokenSource, tokenTarget), Osa.Distance(tokenSource, tokenTarget));
        Assert.Equal(ReferenceScorers.JaroSimilarity(charSource, charTarget), Jaro.Similarity(charSource, charTarget), 12);
    }

    public static IEnumerable<object[]> IntegerSequenceCases()
    {
        yield return new object[] { Array.Empty<int>(), Array.Empty<int>() };
        yield return new object[] { Array.Empty<int>(), new int[] { 1, 2, 3 } };
        yield return new object[] { new int[] { 1, 2, 3 }, Array.Empty<int>() };
        yield return new object[] { new int[] { 1 }, new int[] { 1 } };
        yield return new object[] { new int[] { 1 }, new int[] { 2 } };
        yield return new object[] { new int[] { 1, 2 }, new int[] { 2, 1 } };
        yield return new object[] { new int[] { 1, 2, 3, 2, 1 }, new int[] { 1, 3, 2, 4, 1 } };
        int[] repeatedSource = [1, 2, 3, 4];
        int[] repeatedTarget = [1, 3, 4, 5];
        yield return new object[] { Multiply(repeatedSource, 16), Multiply(repeatedTarget, 16) };

        Random random = new(8675309);

        for (int index = 0; index < 24; index++)
        {
            int firstLength = random.Next(0, 18);
            int secondLength = random.Next(0, 18);
            yield return new object[] { RandomSequence(random, firstLength, 7), RandomSequence(random, secondLength, 7) };
        }
    }

    private static int[] RandomSequence(Random random, int length, int alphabetSize)
    {
        int[] result = new int[length];

        for (int index = 0; index < result.Length; index++)
        {
            result[index] = random.Next(alphabetSize);
        }

        return result;
    }

    private static int[] Multiply(int[] source, int count)
    {
        int[] result = new int[source.Length * count];

        for (int index = 0; index < count; index++)
        {
            source.CopyTo(result.AsSpan(index * source.Length, source.Length));
        }

        return result;
    }

    private readonly record struct ComparableToken(int Value);
}
