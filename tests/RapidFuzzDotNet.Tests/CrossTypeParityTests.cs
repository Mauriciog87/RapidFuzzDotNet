using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;
using System.Numerics;

namespace RapidFuzzDotNet.Tests;

public sealed class CrossTypeParityTests
{
    [Fact]
    public void CrossTypeStaticScorersMatchCanonicalSequences()
    {
        byte[] source = [1, 2, 3, 4, 5];
        int[] target = [1, 3, 2, 4, 6];
        int[] canonicalSource = Array.ConvertAll(source, static value => (int)value);
        ByteIntComparer comparer = new();

        Assert.Equal(Levenshtein.Distance(canonicalSource, target), Levenshtein.Distance<byte, int>(source, target, comparer));
        Assert.Equal(Levenshtein.Similarity(canonicalSource, target), Levenshtein.Similarity<byte, int>(source, target, comparer));
        Assert.Equal(Levenshtein.NormalizedDistance(canonicalSource, target), Levenshtein.NormalizedDistance<byte, int>(source, target, comparer), 12);
        Assert.Equal(Levenshtein.NormalizedSimilarity(canonicalSource, target), Levenshtein.NormalizedSimilarity<byte, int>(source, target, comparer), 12);
        Assert.Equal(Indel.Distance(canonicalSource, target), Indel.Distance<byte, int>(source, target, comparer));
        Assert.Equal(Indel.Similarity(canonicalSource, target), Indel.Similarity<byte, int>(source, target, comparer));
        Assert.Equal(LcsSeq.Distance(canonicalSource, target), LcsSeq.Distance<byte, int>(source, target, comparer));
        Assert.Equal(LcsSeq.Similarity(canonicalSource, target), LcsSeq.Similarity<byte, int>(source, target, comparer));
        Assert.Equal(Hamming.Distance(canonicalSource, target), Hamming.Distance<byte, int>(source, target, comparer));
        Assert.Equal(Osa.Distance(canonicalSource, target), Osa.Distance<byte, int>(source, target, comparer));
        Assert.Equal(DamerauLevenshtein.Distance(canonicalSource, target), DamerauLevenshtein.Distance<byte, int>(source, target, comparer));
        Assert.Equal(Prefix.Similarity(canonicalSource, target), Prefix.Similarity<byte, int>(source, target, comparer));
        Assert.Equal(Postfix.Similarity(canonicalSource, target), Postfix.Similarity<byte, int>(source, target, comparer));
        Assert.Equal(Jaro.Similarity(canonicalSource, target), Jaro.Similarity<byte, int>(source, target, comparer), 12);
        Assert.Equal(JaroWinkler.Similarity(canonicalSource, target), JaroWinkler.Similarity<byte, int>(source, target, comparer), 12);
        Assert.Equal(Fuzz.Ratio(canonicalSource, target), Fuzz.Ratio<byte, int>(source, target, comparer), 12);
        Assert.Equal(Fuzz.QRatio(canonicalSource, target), Fuzz.QRatio<byte, int>(source, target, comparer), 12);
        Assert.Equal(Fuzz.PartialRatio(canonicalSource, target), Fuzz.PartialRatio<byte, int>(source, target, comparer), 12);
    }

    [Fact]
    public void CrossTypeEditOperationsMatchCanonicalScripts()
    {
        byte[] source = [1, 2, 3, 4];
        int[] target = [1, 3, 5, 4, 6];
        int[] canonicalSource = Array.ConvertAll(source, static value => (int)value);
        ByteIntComparer comparer = new();
        EditOperations levenshtein = Levenshtein.Editops<byte, int>(source, target, comparer);
        EditOperations indel = Indel.Editops<byte, int>(source, target, comparer);
        EditOperations lcs = LcsSeq.Editops<byte, int>(source, target, comparer);

        Assert.Equal(target, levenshtein.ApplyTo<int>(canonicalSource, target));
        Assert.Equal(target, levenshtein.ToOpcodes().ApplyTo<int>(canonicalSource, target));
        Assert.Equal(target, indel.ApplyTo<int>(canonicalSource, target));
        Assert.Equal(target, lcs.ApplyTo<int>(canonicalSource, target));

        byte[] hammingSource = [1, 2, 3];
        int[] hammingTarget = [1, 4, 3];
        int[] canonicalHammingSource = Array.ConvertAll(hammingSource, static value => (int)value);
        EditOperations hamming = Hamming.Editops<byte, int>(hammingSource, hammingTarget, comparer);
        Assert.Equal(hammingTarget, hamming.ApplyTo<int>(canonicalHammingSource, hammingTarget));
        Assert.Equal(hammingTarget, Hamming.Opcodes<byte, int>(hammingSource, hammingTarget, comparer).ApplyTo<int>(canonicalHammingSource, hammingTarget));
    }

    [Fact]
    public void CrossTypeCachedScorersMatchStaticScorersAndCopySource()
    {
        byte[] source = [1, 2, 3, 4, 5];
        int[] target = [1, 3, 2, 4, 6];
        ByteIntComparer comparer = new();
        CachedLevenshtein<byte> levenshtein = new(source);
        CachedIndel<byte> indel = new(source);
        CachedHamming<byte> hamming = new(source);
        CachedLcsSeq<byte> lcs = new(source);
        CachedOsa<byte> osa = new(source);
        CachedDamerauLevenshtein<byte> damerau = new(source);
        CachedJaro<byte> jaro = new(source);
        CachedJaroWinkler<byte> jaroWinkler = new(source);
        CachedPrefix<byte> prefix = new(source);
        CachedPostfix<byte> postfix = new(source);
        CachedRatio<byte> ratio = new(source);
        CachedPartialRatio<byte> partialRatio = new(source);
        CachedQRatio<byte> qRatio = new(source);

        source[0] = 9;

        Assert.Equal(Levenshtein.Distance<byte, int>([1, 2, 3, 4, 5], target, comparer), levenshtein.Distance(target, comparer));
        Assert.Equal(Indel.Distance<byte, int>([1, 2, 3, 4, 5], target, comparer), indel.Distance(target, comparer));
        Assert.Equal(Hamming.Distance<byte, int>([1, 2, 3, 4, 5], target, comparer), hamming.Distance(target, comparer));
        Assert.Equal(LcsSeq.Distance<byte, int>([1, 2, 3, 4, 5], target, comparer), lcs.Distance(target, comparer));
        Assert.Equal(Osa.Distance<byte, int>([1, 2, 3, 4, 5], target, comparer), osa.Distance(target, comparer));
        Assert.Equal(DamerauLevenshtein.Distance<byte, int>([1, 2, 3, 4, 5], target, comparer), damerau.Distance(target, comparer));
        Assert.Equal(Jaro.Similarity<byte, int>([1, 2, 3, 4, 5], target, comparer), jaro.Similarity(target, comparer), 12);
        Assert.Equal(JaroWinkler.Similarity<byte, int>([1, 2, 3, 4, 5], target, comparer), jaroWinkler.Similarity(target, comparer), 12);
        Assert.Equal(Prefix.Similarity<byte, int>([1, 2, 3, 4, 5], target, comparer), prefix.Similarity(target, comparer));
        Assert.Equal(Postfix.Similarity<byte, int>([1, 2, 3, 4, 5], target, comparer), postfix.Similarity(target, comparer));
        Assert.Equal(Fuzz.Ratio<byte, int>([1, 2, 3, 4, 5], target, comparer), ratio.Similarity(target, comparer), 12);
        Assert.Equal(Fuzz.PartialRatio<byte, int>([1, 2, 3, 4, 5], target, comparer), partialRatio.Similarity(target, comparer), 12);
        Assert.Equal(Fuzz.QRatio<byte, int>([1, 2, 3, 4, 5], target, comparer), qRatio.Similarity(target, comparer), 12);
    }

    [Fact]
    public void CrossTypeComparersSupportRecordsAndCutoffs()
    {
        SampleValue[] source = [new(1), new(2), new(3), new(4)];
        int[] target = [1, 2, 5, 4];
        SampleIntComparer comparer = new();

        Assert.Equal(1, Levenshtein.Distance<SampleValue, int>(source, target, comparer));
        Assert.Equal(1, Levenshtein.Distance<SampleValue, int>(source, target, comparer, 0));
        Assert.Equal(0.75, Levenshtein.NormalizedSimilarity<SampleValue, int>(source, target, comparer), 12);
        Assert.Equal(75.0, Fuzz.Ratio<SampleValue, int>(source, target, comparer), 12);
        Assert.Equal(0.0, Fuzz.QRatio<SampleValue, int>(ReadOnlySpan<SampleValue>.Empty, target, comparer));
    }

    [Fact]
    public void CrossTypeCharAndRecordScorersPreserveCutoffsAndHints()
    {
        int[] canonicalSource = [0, 1, 2, 3, 4, 5];
        int[] target = [0, 2, 1, 3, 6, 5];
        char[] characterSource = canonicalSource.Select(static value => (char)('a' + value)).ToArray();
        SampleValue[] recordSource = canonicalSource.Select(static value => new SampleValue(value)).ToArray();

        AssertCrossTypeParity(characterSource, canonicalSource, target, new CharIntComparer());
        AssertCrossTypeParity(recordSource, canonicalSource, target, new SampleIntComparer());
    }

    [Fact]
    public void CrossTypePartialRatioChecksBothEqualLengthOrientations()
    {
        byte[] source = [1, 2, 1, 2, 3, 1, 2, 1, 2, 3];
        int[] target = [2, 1, 2, 4, 3, 2, 1, 2, 4, 3];
        int[] canonicalSource = Array.ConvertAll(source, static value => (int)value);
        ByteIntComparer comparer = new();

        Assert.Equal(Fuzz.PartialRatio(canonicalSource, target), Fuzz.PartialRatio<byte, int>(source, target, comparer), 12);
        Assert.Equal(
            Fuzz.PartialRatioAlignment(canonicalSource, target).Score,
            Fuzz.PartialRatioAlignment<byte, int>(source, target, comparer).Score,
            12);
    }

    [Fact]
    public void CrossTypeMultiScorersMatchStaticScorers()
    {
        byte[][] sources =
        [
            [1, 2, 3, 4],
            [1, 3, 4, 5],
            [5, 4, 3, 2]
        ];
        int[] target = [1, 3, 2, 4];
        ByteIntComparer comparer = new();
        MultiLevenshtein<byte> levenshtein = new(sources);
        MultiIndel<byte> indel = new(sources);
        MultiLcsSeq<byte> lcs = new(sources);
        MultiOsa<byte> osa = new(sources);
        MultiJaro<byte> jaro = new(sources);
        MultiJaroWinkler<byte> jaroWinkler = new(sources);
        MultiRatio<byte> ratio = new(sources);
        MultiQRatio<byte> qRatio = new(sources);

        int[] expectedLevenshtein = Array.ConvertAll(sources, source => Levenshtein.Distance<byte, int>(source, target, comparer));
        int[] expectedIndel = Array.ConvertAll(sources, source => Indel.Distance<byte, int>(source, target, comparer));
        int[] expectedLcs = Array.ConvertAll(sources, source => LcsSeq.Similarity<byte, int>(source, target, comparer));
        int[] expectedOsa = Array.ConvertAll(sources, source => Osa.Distance<byte, int>(source, target, comparer));
        double[] expectedJaro = Array.ConvertAll(sources, source => Jaro.Similarity<byte, int>(source, target, comparer));
        double[] expectedJaroWinkler = Array.ConvertAll(sources, source => JaroWinkler.Similarity<byte, int>(source, target, comparer));
        double[] expectedRatio = Array.ConvertAll(sources, source => Fuzz.Ratio<byte, int>(source, target, comparer));
        double[] expectedQRatio = Array.ConvertAll(sources, source => Fuzz.QRatio<byte, int>(source, target, comparer));

        Assert.Equal(expectedLevenshtein, levenshtein.Distances(target, comparer));
        Assert.Equal(expectedIndel, indel.Distances(target, comparer));
        Assert.Equal(expectedLcs, lcs.Similarities(target, comparer));
        Assert.Equal(expectedOsa, osa.Distances(target, comparer));
        Assert.Equal(expectedJaro, jaro.Similarities(target, comparer));
        Assert.Equal(expectedJaroWinkler, jaroWinkler.Similarities(target, comparer));
        Assert.Equal(expectedRatio, ratio.Similarities(target, comparer));
        Assert.Equal(expectedQRatio, qRatio.Similarities(target, comparer));

        int[] insufficient = new int[sources.Length - 1];
        Assert.Throws<ArgumentException>(() => levenshtein.Distances(target, insufficient, comparer));
    }

    [Fact]
    public void MultiJaroVectorAndFallbackPathsMatchStaticBitForBit()
    {
        int vectorWidth = Vector<ulong>.Count;
        List<int[]> sources = new(vectorWidth * 2 + 3);

        for (int index = 0; index < vectorWidth * 2; index++)
        {
            int length = index == 0 ? 0 : index == 1 ? 65 : index % 3 == 0 ? 1 : index % 3 == 1 ? 63 : 64;
            int[] source = new int[length];

            for (int position = 0; position < length; position++)
            {
                source[position] = (position * 7 + index) % 19;
            }

            sources.Add(source);
        }

        sources.Add([]);
        sources.Add(Enumerable.Range(0, 65).Select(static value => value % 13).ToArray());
        sources.Add([1, 1, 1, 1, 1]);
        int[] target = Enumerable.Range(0, 71).Select(static value => (value * 5) % 19).ToArray();
        MultiJaro<int> jaro = new(sources);
        MultiJaroWinkler<int> jaroWinkler = new(sources);
        double[] actualJaro = jaro.Similarities(target);
        double[] actualJaroWinkler = jaroWinkler.Similarities(target);

        for (int index = 0; index < sources.Count; index++)
        {
            Assert.Equal(Jaro.Similarity(sources[index], target), actualJaro[index]);
            Assert.Equal(JaroWinkler.Similarity(sources[index], target), actualJaroWinkler[index]);
        }

        double[] cutoffScores = jaro.Similarities(target, 0.8);

        for (int index = 0; index < sources.Count; index++)
        {
            double expected = Jaro.Similarity(sources[index], target, 0.8);
            Assert.Equal(expected, cutoffScores[index]);
        }
    }

    [Fact]
    public void CrossTypeJaroUsesUpstreamHalfTranspositionRounding()
    {
        byte[] source = [2, 1, 0, 0, 0, 0];
        int[] target = [0, 2, 1, 0, 0, 0];
        int[] canonicalSource = Array.ConvertAll(source, static value => (int)value);
        ByteIntComparer comparer = new();

        Assert.Equal(Jaro.Similarity(canonicalSource, target), Jaro.Similarity<byte, int>(source, target, comparer));
        Assert.Equal(JaroWinkler.Similarity(canonicalSource, target), JaroWinkler.Similarity<byte, int>(source, target, comparer));
    }

    [Fact]
    public void CrossTypeScorersRejectNullComparer()
    {
        byte[] source = [1, 2, 3];
        int[] target = [1, 2, 3];
        ISequenceEqualityComparer<byte, int> comparer = null!;

        Assert.Throws<ArgumentNullException>(() => Levenshtein.Distance<byte, int>(source, target, comparer));
        Assert.Throws<ArgumentNullException>(() => Fuzz.Ratio<byte, int>(source, target, comparer));
        Assert.Throws<ArgumentNullException>(() => Jaro.Similarity<byte, int>(source, target, comparer));
    }

    private static void AssertCrossTypeParity<TSource>(
        TSource[] source,
        int[] canonicalSource,
        int[] target,
        ISequenceEqualityComparer<TSource, int> comparer)
        where TSource : notnull, IEquatable<TSource>
    {
        Assert.Equal(Levenshtein.Distance<int>(canonicalSource, target, 4, 2), Levenshtein.Distance(source, target, comparer, 4, 2));
        Assert.Equal(Levenshtein.Similarity<int>(canonicalSource, target, 1, 1), Levenshtein.Similarity(source, target, comparer, 1, 1));
        Assert.Equal(Indel.Distance<int>(canonicalSource, target, 5, 3), Indel.Distance(source, target, comparer, 5, 3));
        Assert.Equal(LcsSeq.Similarity<int>(canonicalSource, target, 1, 1), LcsSeq.Similarity(source, target, comparer, 1, 1));
        Assert.Equal(Hamming.Distance<int>(canonicalSource, target, false, 4, 2), Hamming.Distance(source, target, comparer, false, 4, 2));
        Assert.Equal(Osa.Distance<int>(canonicalSource, target, 4, 2), Osa.Distance(source, target, comparer, 4, 2));
        Assert.Equal(DamerauLevenshtein.Distance<int>(canonicalSource, target, 4, 2), DamerauLevenshtein.Distance(source, target, comparer, 4, 2));
        Assert.Equal(Prefix.Similarity<int>(canonicalSource, target, 1, 1), Prefix.Similarity(source, target, comparer, 1, 1));
        Assert.Equal(Postfix.Similarity<int>(canonicalSource, target, 1, 1), Postfix.Similarity(source, target, comparer, 1, 1));
        Assert.Equal(Jaro.Similarity<int>(canonicalSource, target, 0.2, 0.1), Jaro.Similarity(source, target, comparer, 0.2, 0.1));
        Assert.Equal(JaroWinkler.Similarity<int>(canonicalSource, target, 0.1, 0.2, 0.1), JaroWinkler.Similarity(source, target, comparer, 0.1, 0.2, 0.1));
        Assert.Equal(Fuzz.Ratio<int>(canonicalSource, target, 20.0), Fuzz.Ratio(source, target, comparer, 20.0));
        Assert.Equal(Fuzz.QRatio<int>(canonicalSource, target, 20.0), Fuzz.QRatio(source, target, comparer, 20.0));
        Assert.Equal(Fuzz.PartialRatio<int>(canonicalSource, target, 20.0), Fuzz.PartialRatio(source, target, comparer, 20.0));
    }

    private sealed class ByteIntComparer : ISequenceEqualityComparer<byte, int>
    {
        public bool Equals(byte left, int right)
        {
            return left == right;
        }
    }

    private sealed class CharIntComparer : ISequenceEqualityComparer<char, int>
    {
        public bool Equals(char left, int right)
        {
            return left - 'a' == right;
        }
    }

    private sealed class SampleIntComparer : ISequenceEqualityComparer<SampleValue, int>
    {
        public bool Equals(SampleValue left, int right)
        {
            return left.Value == right;
        }
    }

    private readonly record struct SampleValue(int Value);
}
