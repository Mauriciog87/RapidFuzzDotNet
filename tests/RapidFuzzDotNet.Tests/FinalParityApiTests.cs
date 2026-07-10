using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;
using System.Numerics;

namespace RapidFuzzDotNet.Tests;

public sealed class FinalParityApiTests
{
    [Fact]
    public void GenericQRatioMatchesRatioExceptForEmptySequences()
    {
        int[] first = [1, 2, 3, 4];
        int[] second = [1, 2, 5, 4];

        Assert.Equal(Fuzz.Ratio<int>(first, second), Fuzz.QRatio<int>(first, second));
        Assert.Equal(0.0, Fuzz.QRatio<int>(ReadOnlySpan<int>.Empty, ReadOnlySpan<int>.Empty));
        Assert.Equal(0.0, Fuzz.QRatio<int>(first, ReadOnlySpan<int>.Empty));
        Assert.Equal(0.0, Fuzz.QRatio<int>(first, second, 80.0));
    }

    [Fact]
    public void GenericCachedQRatioCopiesSource()
    {
        int[] source = [1, 2, 3];
        CachedQRatio<int> scorer = new(source);

        source[0] = 9;

        Assert.Equal(100.0, scorer.Similarity([1, 2, 3]));
        Assert.Equal(0.0, scorer.Similarity(ReadOnlySpan<int>.Empty));
    }

    [Fact]
    public void CachedTokenSpanOverloadsMatchStaticScorers()
    {
        ReadOnlySpan<char> source = "alpha beta beta gamma".AsSpan();
        ReadOnlySpan<char> target = "gamma alpha beta delta".AsSpan();

        Assert.Equal(Fuzz.TokenSortRatio(source, target), new CachedTokenSortRatio(source).Similarity(target));
        Assert.Equal(Fuzz.PartialTokenSortRatio(source, target), new CachedPartialTokenSortRatio(source).Similarity(target));
        Assert.Equal(Fuzz.TokenSetRatio(source, target), new CachedTokenSetRatio(source).Similarity(target));
        Assert.Equal(Fuzz.PartialTokenSetRatio(source, target), new CachedPartialTokenSetRatio(source).Similarity(target));
        Assert.Equal(Fuzz.TokenRatio(source, target), new CachedTokenRatio(source).Similarity(target));
        Assert.Equal(Fuzz.PartialTokenRatio(source, target), new CachedPartialTokenRatio(source).Similarity(target));
        Assert.Equal(Fuzz.QRatio(source, target), new CachedQRatio(source).Similarity(target));
        Assert.Equal(Fuzz.WRatio(source.ToString(), target.ToString()), new CachedWRatio(source).Similarity(target));
    }

    [Fact]
    public void EditOperationsSliceNormalizesBoundsAndPreservesLengths()
    {
        EditOperations operations = new(
        [
            new EditOp(EditOperation.Replace, 0, 0),
            new EditOp(EditOperation.Insert, 1, 1),
            new EditOp(EditOperation.Delete, 2, 2),
            new EditOp(EditOperation.Replace, 3, 2)
        ],
        4,
        3);

        EditOperations slice = operations.Slice(-3, int.MaxValue, 2);
        EditOperations removed = operations.RemoveSlice(-3, int.MaxValue, 2);

        Assert.Equal([operations[1], operations[3]], slice.ToArray());
        Assert.Equal([operations[0], operations[2]], removed.ToArray());
        Assert.Equal(4, slice.SourceLength);
        Assert.Equal(3, slice.DestinationLength);
        Assert.Equal(4, operations.Count);
        Assert.Empty(operations.Slice(3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => operations.Slice(0, 4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => operations.RemoveSlice(0, 4, -1));
    }

    [Fact]
    public void FullEditOperationAndOpcodeSlicesRoundTrip()
    {
        string source = "qabxcd";
        string destination = "abycdf";
        EditOperations editOperations = Levenshtein.Editops(source, destination);
        Opcodes opcodes = editOperations.ToOpcodes();

        EditOperations editSlice = editOperations.Slice(int.MinValue, int.MaxValue);
        Opcodes opcodeSlice = opcodes.Slice(int.MinValue, int.MaxValue);

        Assert.Equal(destination, editSlice.ApplyTo(source, destination));
        Assert.Equal(destination, opcodeSlice.ApplyTo(source, destination));
        Assert.Equal(editOperations, editSlice);
        Assert.Equal(opcodes, opcodeSlice);
        Assert.Throws<ArgumentOutOfRangeException>(() => opcodes.Slice(0, opcodes.Count, 0));
    }

    [Fact]
    public void GenericMultiDistanceScorersMatchCachedScorers()
    {
        int[][] sources = [[1, 2, 3], [1, 3, 2], []];
        int[] target = [1, 2, 4];

        Assert.Equal(
            sources.Select(source => new CachedLevenshtein<int>(source).Distance(target, 2)).ToArray(),
            new MultiLevenshtein<int>(sources).Distances(target, 2));
        Assert.Equal(
            sources.Select(source => new CachedIndel<int>(source).Similarity(target, 2)).ToArray(),
            new MultiIndel<int>(sources).Similarities(target, 2));
        Assert.Equal(
            sources.Select(source => new CachedLcsSeq<int>(source).Distance(target, 2)).ToArray(),
            new MultiLcsSeq<int>(sources).Distances(target, 2));
        Assert.Equal(
            sources.Select(source => new CachedOsa<int>(source).Distance(target, 2)).ToArray(),
            new MultiOsa<int>(sources).Distances(target, 2));
        Assert.Equal(
            sources.Select(source => new CachedJaro<int>(source).Similarity(target, 0.5)).ToArray(),
            new MultiJaro<int>(sources).Similarities(target, 0.5));
        Assert.Equal(
            sources.Select(source => new CachedJaroWinkler<int>(source).Similarity(target, 0.5)).ToArray(),
            new MultiJaroWinkler<int>(sources).Similarities(target, 0.5));
    }

    [Fact]
    public void GenericMultiFuzzScorersCopySourcesAndMatchCachedScorers()
    {
        SequenceToken[] mutable = [new SequenceToken(1), new SequenceToken(2), new SequenceToken(3)];
        SequenceToken[][] sources = [mutable, [new SequenceToken(1), new SequenceToken(3)], []];
        SequenceToken[] target = [new SequenceToken(1), new SequenceToken(2), new SequenceToken(4)];
        MultiRatio<SequenceToken> ratio = new(sources);
        MultiQRatio<SequenceToken> qRatio = new(sources);

        mutable[0] = new SequenceToken(9);

        Assert.Equal(
            Fuzz.Ratio<SequenceToken>([new SequenceToken(1), new SequenceToken(2), new SequenceToken(3)], target),
            ratio.Similarities(target)[0]);
        Assert.Equal(
            new double[]
            {
                Fuzz.QRatio<SequenceToken>([new SequenceToken(1), new SequenceToken(2), new SequenceToken(3)], target),
                Fuzz.QRatio<SequenceToken>([new SequenceToken(1), new SequenceToken(3)], target),
                Fuzz.QRatio<SequenceToken>(ReadOnlySpan<SequenceToken>.Empty, target)
            },
            qRatio.Similarities(target));
    }

    [Fact]
    public void GenericMultiScorersValidateDestinationAndCapacity()
    {
        int[] target = [1, 2, 3];
        MultiLevenshtein<int> distance = new([[1], [2]]);
        MultiRatio<int> ratio = new([[1], [2]]);

        Assert.Throws<ArgumentException>(() => distance.Distances(target, new int[1]));
        Assert.Throws<ArgumentException>(() => ratio.Similarities(target, new double[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultiOsa<int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultiQRatio<int>(-1));
    }

    [Fact]
    public void GenericMultiSimdMatchesScalarAcrossWordBoundaries()
    {
        int sourceCount = (Vector<ulong>.Count * 2) + 3;
        int[][] sources = new int[sourceCount][];

        for (int index = 0; index < sourceCount - 3; index++)
        {
            sources[index] = Enumerable.Range(0, 12 + index).Select(value => value % 7).ToArray();
        }

        sources[^3] = Enumerable.Range(0, 63).Select(value => value % 11).ToArray();
        sources[^2] = Enumerable.Range(0, 64).Select(value => value % 11).ToArray();
        sources[^1] = Enumerable.Range(0, 65).Select(value => value % 11).ToArray();
        int[] target = Enumerable.Range(0, 48).Select(value => (value + 1) % 11).ToArray();

        Assert.Equal(
            sources.Select(source => Levenshtein.Distance<int>(source, target)).ToArray(),
            new MultiLevenshtein<int>(sources).Distances(target));
        Assert.Equal(
            sources.Select(source => Indel.Distance<int>(source, target)).ToArray(),
            new MultiIndel<int>(sources).Distances(target));
        Assert.Equal(
            sources.Select(source => LcsSeq.Similarity<int>(source, target)).ToArray(),
            new MultiLcsSeq<int>(sources).Similarities(target));
        Assert.Equal(
            sources.Select(source => Osa.Distance<int>(source, target)).ToArray(),
            new MultiOsa<int>(sources).Distances(target));
        Assert.Equal(
            sources.Select(source => Fuzz.Ratio<int>(source, target)).ToArray(),
            new MultiRatio<int>(sources).Similarities(target));
        Assert.Equal(
            sources.Select(source => Fuzz.QRatio<int>(source, target)).ToArray(),
            new MultiQRatio<int>(sources).Similarities(target));
    }

    [Fact]
    public void GenericMultiWeightedLevenshteinUsesScalarFallback()
    {
        int[][] sources = [[1, 2, 3], [3, 2, 1]];
        int[] target = [1, 3, 2];
        LevenshteinWeights weights = new(1, 2, 3);
        MultiLevenshtein<int> scorer = new(sources, weights);

        Assert.Equal(
            sources.Select(source => Levenshtein.Distance<int>(source, target, weights)).ToArray(),
            scorer.Distances(target));
    }

    private readonly record struct SequenceToken(int Value);
}
