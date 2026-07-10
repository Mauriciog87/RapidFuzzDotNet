using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;

namespace RapidFuzzDotNet.Tests;

public sealed class ExperimentalTests
{
    [Fact]
    public void MultiFuzzScorersMatchStaticScorers()
    {
        string[] sources = ["fuzzy wuzzy was a bear", "new york mets", ""];
        string target = "wuzzy fuzzy was a bear";

        AssertScores(
            new MultiRatio(sources).Similarities(target),
            Fuzz.Ratio(sources[0], target),
            Fuzz.Ratio(sources[1], target),
            Fuzz.Ratio(sources[2], target));

        AssertScores(
            new MultiPartialRatio(sources).Similarities(target),
            Fuzz.PartialRatio(sources[0], target),
            Fuzz.PartialRatio(sources[1], target),
            Fuzz.PartialRatio(sources[2], target));

        AssertScores(
            new MultiTokenSortRatio(sources).Similarities(target),
            Fuzz.TokenSortRatio(sources[0], target),
            Fuzz.TokenSortRatio(sources[1], target),
            Fuzz.TokenSortRatio(sources[2], target));

        AssertScores(
            new MultiPartialTokenSortRatio(sources).Similarities(target),
            Fuzz.PartialTokenSortRatio(sources[0], target),
            Fuzz.PartialTokenSortRatio(sources[1], target),
            Fuzz.PartialTokenSortRatio(sources[2], target));

        AssertScores(
            new MultiTokenSetRatio(sources).Similarities(target),
            Fuzz.TokenSetRatio(sources[0], target),
            Fuzz.TokenSetRatio(sources[1], target),
            Fuzz.TokenSetRatio(sources[2], target));

        AssertScores(
            new MultiPartialTokenSetRatio(sources).Similarities(target),
            Fuzz.PartialTokenSetRatio(sources[0], target),
            Fuzz.PartialTokenSetRatio(sources[1], target),
            Fuzz.PartialTokenSetRatio(sources[2], target));

        AssertScores(
            new MultiTokenRatio(sources).Similarities(target),
            Fuzz.TokenRatio(sources[0], target),
            Fuzz.TokenRatio(sources[1], target),
            Fuzz.TokenRatio(sources[2], target));

        AssertScores(
            new MultiPartialTokenRatio(sources).Similarities(target),
            Fuzz.PartialTokenRatio(sources[0], target),
            Fuzz.PartialTokenRatio(sources[1], target),
            Fuzz.PartialTokenRatio(sources[2], target));

        AssertScores(
            new MultiQRatio(sources).Similarities(target),
            Fuzz.QRatio(sources[0], target),
            Fuzz.QRatio(sources[1], target),
            Fuzz.QRatio(sources[2], target));

        AssertScores(
            new MultiWRatio(sources).Similarities(target),
            Fuzz.WRatio(sources[0], target),
            Fuzz.WRatio(sources[1], target),
            Fuzz.WRatio(sources[2], target));
    }

    [Fact]
    public void MultiScorersExposeCountAndApplyCutoff()
    {
        string[] sources = ["alpha", "beta"];
        MultiRatio scorer = new(sources);

        double[] scores = scorer.Similarities("alpha", 90.0);

        Assert.Equal(2, scorer.Count);
        Assert.Equal(100.0, scores[0]);
        Assert.Equal(0.0, scores[1]);
    }

    [Fact]
    public void MultiFuzzScorersCanInsertSourcesAndWriteToSpan()
    {
        MultiRatio scorer = new(2);
        double[] scores = new double[2];

        scorer.Insert("alpha");
        scorer.Insert("beta");
        scorer.Similarities("alpha", scores);

        Assert.Equal(2, scorer.Count);
        Assert.Equal(100.0, scores[0]);
        Assert.Equal(Fuzz.Ratio("beta", "alpha"), scores[1], 10);
    }

    [Fact]
    public void MultiFuzzScorersRejectSmallDestinationSpan()
    {
        MultiRatio scorer = new(["alpha", "beta"]);
        double[] scores = new double[1];

        Assert.Throws<ArgumentException>(() => scorer.Similarities("alpha", scores));
    }

    [Fact]
    public void MultiScorersRejectNullSources()
    {
        IEnumerable<string> sources = NullSources();

        Assert.Throws<ArgumentException>(() => new MultiRatio(sources));
    }

    [Fact]
    public void MultiDistanceScorersMatchStaticScorers()
    {
        string[] sources = ["kitten", "sitting", ""];
        string target = "sitting";

        AssertScores(
            new MultiLevenshtein(sources).Distances(target),
            Levenshtein.Distance(sources[0], target),
            Levenshtein.Distance(sources[1], target),
            Levenshtein.Distance(sources[2], target));

        AssertScores(
            new MultiIndel(sources).Similarities(target),
            Indel.Similarity(sources[0], target),
            Indel.Similarity(sources[1], target),
            Indel.Similarity(sources[2], target));

        AssertScores(
            new MultiLcsSeq(sources).NormalizedSimilarities(target),
            LcsSeq.NormalizedSimilarity(sources[0], target),
            LcsSeq.NormalizedSimilarity(sources[1], target),
            LcsSeq.NormalizedSimilarity(sources[2], target));

        AssertScores(
            new MultiOsa(sources).Distances(target),
            Osa.Distance(sources[0], target),
            Osa.Distance(sources[1], target),
            Osa.Distance(sources[2], target));

        AssertScores(
            new MultiJaro(sources).Similarities(target),
            Jaro.Similarity(sources[0], target),
            Jaro.Similarity(sources[1], target),
            Jaro.Similarity(sources[2], target));

        AssertScores(
            new MultiJaroWinkler(sources).NormalizedDistances(target),
            JaroWinkler.NormalizedDistance(sources[0], target),
            JaroWinkler.NormalizedDistance(sources[1], target),
            JaroWinkler.NormalizedDistance(sources[2], target));
    }

    [Fact]
    public void AdditionalMultiDistanceScorersMatchStaticScorers()
    {
        string[] sources = ["karolin", "kathrin", ""];
        string target = "kathrin";
        string[] affixSources = ["prefix-alpha", "prefix-beta", "alpha-end"];
        string affixTarget = "prefix-gamma-end";

        AssertScores(
            new MultiHamming(sources).Distances(target),
            Hamming.Distance(sources[0], target),
            Hamming.Distance(sources[1], target),
            Hamming.Distance(sources[2], target));

        AssertScores(
            new MultiDamerauLevenshtein(sources).Distances(target),
            DamerauLevenshtein.Distance(sources[0], target),
            DamerauLevenshtein.Distance(sources[1], target),
            DamerauLevenshtein.Distance(sources[2], target));

        AssertScores(
            new MultiPrefix(affixSources).Similarities(affixTarget),
            Prefix.Similarity(affixSources[0], affixTarget),
            Prefix.Similarity(affixSources[1], affixTarget),
            Prefix.Similarity(affixSources[2], affixTarget));

        AssertScores(
            new MultiPostfix(affixSources).NormalizedSimilarities(affixTarget),
            Postfix.NormalizedSimilarity(affixSources[0], affixTarget),
            Postfix.NormalizedSimilarity(affixSources[1], affixTarget),
            Postfix.NormalizedSimilarity(affixSources[2], affixTarget));
    }

    [Fact]
    public void MultiJaroScorersApplyVectorizedSimilarityCutoff()
    {
        string[] sources =
        [
            "MARTHA",
            "DIXON",
            "CRATE",
            "completely different",
            "DWAYNE"
        ];
        string target = "MARHTA";
        double[] jaroScores = new double[sources.Length];
        double[] jaroWinklerScores = new double[sources.Length];
        MultiJaro jaro = new(sources);
        MultiJaroWinkler jaroWinkler = new(sources);

        jaro.Similarities(target, jaroScores, 0.8);
        jaroWinkler.NormalizedSimilarities(target, jaroWinklerScores, 0.8);

        for (int i = 0; i < sources.Length; i++)
        {
            double expectedJaro = Jaro.Similarity(sources[i], target, 0.8);
            double expectedJaroWinkler = JaroWinkler.NormalizedSimilarity(sources[i], target, 0.1, 0.8);

            Assert.Equal(expectedJaro, jaroScores[i], 12);
            Assert.Equal(expectedJaroWinkler, jaroWinklerScores[i], 12);
        }
    }

    [Fact]
    public void MultiJaroScorersRejectInvalidVectorizedCutoffs()
    {
        MultiJaro jaro = new(["alpha", "beta"]);
        MultiJaroWinkler jaroWinkler = new(["alpha", "beta"]);

        Assert.Throws<ArgumentOutOfRangeException>(() => jaro.Similarities("alpha", -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => jaroWinkler.NormalizedSimilarities("alpha", 1.1));
    }

    [Fact]
    public void MultiDistanceScorersExposeCountAndApplyCutoff()
    {
        string[] sources = ["alpha", "beta"];
        MultiLevenshtein scorer = new(sources);

        int[] distances = scorer.Distances("alpha", 1);
        double[] normalizedSimilarities = scorer.NormalizedSimilarities("alpha", 0.9);

        Assert.Equal(2, scorer.Count);
        Assert.Equal(0, distances[0]);
        Assert.Equal(2, distances[1]);
        Assert.Equal(1.0, normalizedSimilarities[0]);
        Assert.Equal(0.0, normalizedSimilarities[1]);
    }

    [Fact]
    public void MultiDistanceScorersCanInsertSourcesAndWriteToSpan()
    {
        MultiLevenshtein scorer = new(2);
        int[] distances = new int[2];
        double[] normalizedSimilarities = new double[2];

        scorer.Insert("kitten");
        scorer.Insert("sitting");
        scorer.Distances("sitting", distances);
        scorer.NormalizedSimilarities("sitting", normalizedSimilarities);

        Assert.Equal(2, scorer.Count);
        Assert.Equal(3, distances[0]);
        Assert.Equal(0, distances[1]);
        Assert.Equal(Levenshtein.NormalizedSimilarity("kitten", "sitting"), normalizedSimilarities[0], 12);
        Assert.Equal(1.0, normalizedSimilarities[1]);
    }

    [Fact]
    public void MultiDistanceScorersMatchStaticScorersForLongSources()
    {
        string[] sources = [Repeated("abcdefghij", 8), Repeated("Zbcdefghij", 8), ""];
        string target = Repeated("abcdefghij", 8);

        AssertScores(
            new MultiLevenshtein(sources).Distances(target),
            Levenshtein.Distance(sources[0], target),
            Levenshtein.Distance(sources[1], target),
            Levenshtein.Distance(sources[2], target));

        AssertScores(
            new MultiIndel(sources).Distances(target),
            Indel.Distance(sources[0], target),
            Indel.Distance(sources[1], target),
            Indel.Distance(sources[2], target));

        AssertScores(
            new MultiLcsSeq(sources).Similarities(target),
            LcsSeq.Similarity(sources[0], target),
            LcsSeq.Similarity(sources[1], target),
            LcsSeq.Similarity(sources[2], target));
    }

    [Fact]
    public void MultiIntegerSimilaritiesApplyCutoffAndPreserveDestinationTail()
    {
        string[] sources = ["alpha", "alpaca", "beta", "alphabet"];
        string target = "alpha";
        int scoreCutoff = 4;
        int[] expected = new int[sources.Length];
        int[] destination = [-1, -1, -1, -1, -1];
        MultiLcsSeq scorer = new(sources);

        for (int i = 0; i < sources.Length; i++)
        {
            expected[i] = LcsSeq.Similarity(sources[i], target, scoreCutoff);
        }

        scorer.Similarities(target, destination, scoreCutoff);

        AssertScores(scorer.Similarities(target, scoreCutoff), expected);
        Assert.Equal(-1, destination[^1]);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], destination[i]);
        }
    }

    [Fact]
    public void UpstreamMultiDistanceWraparoundFixturesRemainStable()
    {
        string[] sources = ["a", "b", "aa", "bb"];
        MultiLevenshtein levenshtein = new(sources);
        MultiLcsSeq lcsSeq = new(sources);

        AssertScores(levenshtein.Distances(Repeated("b", 256)), 256, 255, 256, 254);
        AssertScores(levenshtein.Distances(Repeated("b", 300)), 300, 299, 300, 298);
        AssertScores(levenshtein.Distances(Repeated("b", 512)), 512, 511, 512, 510);
        AssertScores(lcsSeq.Distances(Repeated("b", 256)), 256, 255, 256, 254);
        AssertScores(lcsSeq.Distances(Repeated("b", 300)), 300, 299, 300, 298);
        AssertScores(lcsSeq.Distances(Repeated("b", 512)), 512, 511, 512, 510);
    }

    [Fact]
    public void MultiDistanceScorersRejectSmallDestinationSpan()
    {
        MultiLevenshtein scorer = new(["alpha", "beta"]);
        int[] distances = new int[1];

        Assert.Throws<ArgumentException>(() => scorer.Distances("alpha", distances));
    }

    [Fact]
    public void MultiScorersPreserveResultsWithScoreHint()
    {
        string[] sources = ["kitten", "sitting"];
        MultiRatio ratio = new(sources);
        MultiLevenshtein levenshtein = new(sources);

        AssertScores(
            ratio.Similarities("sitting", scoreHint: 50.0),
            ratio.Similarities("sitting"));
        AssertScores(
            levenshtein.Distances("sitting", scoreHint: 10),
            levenshtein.Distances("sitting"));
    }

    [Fact]
    public void MultiScorersMatchStaticAndCachedCutoffs()
    {
        string[] sources = ["kitten", "sitting", "smitten"];
        string target = "sitting";
        MultiLevenshtein levenshtein = new(sources);
        MultiIndel indel = new(sources);
        MultiJaro jaro = new(sources);
        MultiRatio ratio = new(sources);
        CachedLevenshtein cachedLevenshtein0 = new(sources[0]);
        CachedLevenshtein cachedLevenshtein1 = new(sources[1]);
        CachedLevenshtein cachedLevenshtein2 = new(sources[2]);
        CachedIndel cachedIndel0 = new(sources[0]);
        CachedIndel cachedIndel1 = new(sources[1]);
        CachedIndel cachedIndel2 = new(sources[2]);
        CachedJaro cachedJaro0 = new(sources[0]);
        CachedJaro cachedJaro1 = new(sources[1]);
        CachedJaro cachedJaro2 = new(sources[2]);
        CachedRatio cachedRatio0 = new(sources[0]);
        CachedRatio cachedRatio1 = new(sources[1]);
        CachedRatio cachedRatio2 = new(sources[2]);

        AssertScores(
            levenshtein.Distances(target, 3),
            cachedLevenshtein0.Distance(target, 3),
            cachedLevenshtein1.Distance(target, 3),
            cachedLevenshtein2.Distance(target, 3));
        AssertScores(
            indel.Similarities(target, 8),
            cachedIndel0.Similarity(target, 8),
            cachedIndel1.Similarity(target, 8),
            cachedIndel2.Similarity(target, 8));
        AssertScores(
            jaro.Similarities(target, 0.85),
            cachedJaro0.Similarity(target, 0.85),
            cachedJaro1.Similarity(target, 0.85),
            cachedJaro2.Similarity(target, 0.85));
        AssertScores(
            ratio.Similarities(target, 80.0),
            cachedRatio0.Similarity(target, 80.0),
            cachedRatio1.Similarity(target, 80.0),
            cachedRatio2.Similarity(target, 80.0));
    }

    private static void AssertScores(double[] actual, params double[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], 10);
        }
    }

    private static void AssertScores(int[] actual, params int[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    private static IEnumerable<string> NullSources()
    {
        yield return "alpha";
        yield return null!;
    }

    private static string Repeated(string value, int count)
    {
        return string.Concat(Enumerable.Repeat(value, count));
    }
}
