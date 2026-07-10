using RapidFuzz.Distance;

namespace RapidFuzzDotNet.Tests;

public sealed class DistanceTests
{
    [Fact]
    public void LevenshteinDistanceReturnsEditDistance()
    {
        Assert.Equal(2, Levenshtein.Distance("lewenstein", "levenshtein"));
    }

    [Fact]
    public void LevenshteinNormalizedSimilarityMatchesRapidFuzzExample()
    {
        double score = Levenshtein.NormalizedSimilarity("lewenstein", "levenshtein");

        Assert.Equal(0.8181818181818181, score, 12);
    }

    [Fact]
    public void LevenshteinNormalizedSimilarityAppliesCutoff()
    {
        double score = Levenshtein.NormalizedSimilarity("lewenstein", "levenshtein", 0.85);

        Assert.Equal(0.0, score);
    }

    [Fact]
    public void LevenshteinWeightsMatchUpstreamCases()
    {
        LevenshteinWeights weights = new(1, 1, 2);

        Assert.Equal(0, Levenshtein.Distance("aaaa", "aaaa", weights));
        Assert.Equal(1, Levenshtein.Distance("aaaa", "aaa", weights));
        Assert.Equal(2, Levenshtein.Distance("abaa", "baaa", weights));
        Assert.Equal(2, Levenshtein.Distance("aaaa", "aaab", weights));
        Assert.Equal(8, Levenshtein.Distance("aaaa", "bbbb", weights));
        Assert.Equal(0.8571, Levenshtein.NormalizedSimilarity("aaaa", "aaa", weights), 4);
        Assert.Equal(0.75, Levenshtein.NormalizedSimilarity("abaa", "baaa", weights), 4);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3, 5, 4, 3, 3)]
    [InlineData("CA", "ABC", 3, 3, 1, 3, 2)]
    [InlineData("qabxcd", "abycdf", 3, 4, 4, 3, 3)]
    public void DistanceAlgorithmsMatchUpstreamRepresentativeCases(
        string source,
        string target,
        int levenshteinDistance,
        int indelDistance,
        int lcsSimilarity,
        int osaDistance,
        int damerauLevenshteinDistance)
    {
        Assert.Equal(levenshteinDistance, Levenshtein.Distance(source, target));
        Assert.Equal(indelDistance, Indel.Distance(source, target));
        Assert.Equal(lcsSimilarity, LcsSeq.Similarity(source, target));
        Assert.Equal(osaDistance, Osa.Distance(source, target));
        Assert.Equal(damerauLevenshteinDistance, DamerauLevenshtein.Distance(source, target));
    }

    [Theory]
    [MemberData(nameof(AdditionalParityCases))]
    public void DistanceAlgorithmsMatchAdditionalRapidFuzzParityCases(
        string source,
        string target,
        int levenshteinDistance,
        int lcsSimilarity,
        int lcsDistance,
        int indelDistance,
        int indelSimilarity,
        int osaDistance,
        double jaroSimilarity,
        double jaroWinklerSimilarity)
    {
        Assert.Equal(levenshteinDistance, Levenshtein.Distance(source, target));
        Assert.Equal(lcsSimilarity, LcsSeq.Similarity(source, target));
        Assert.Equal(lcsDistance, LcsSeq.Distance(source, target));
        Assert.Equal(indelDistance, Indel.Distance(source, target));
        Assert.Equal(indelSimilarity, Indel.Similarity(source, target));
        Assert.Equal(osaDistance, Osa.Distance(source, target));
        Assert.Equal(jaroSimilarity, Jaro.Similarity(source, target), 12);
        Assert.Equal(jaroWinklerSimilarity, JaroWinkler.Similarity(source, target), 12);
    }

    [Theory]
    [InlineData("abc", "axc", 1, 1)]
    [InlineData("abc", "xyz", 2, 3)]
    [InlineData("abcdef", "abqdef", 0, 1)]
    [InlineData("abcdef", "azced", 2, 3)]
    public void LevenshteinSmallCutoffsUseMblevenCompatibleSentinels(
        string source,
        string target,
        int scoreCutoff,
        int expected)
    {
        Assert.Equal(expected, Levenshtein.Distance(source, target, scoreCutoff));
    }

    [Fact]
    public void UpstreamLevenshteinMblevenFixturesRemainStable()
    {
        string source = "South Korea";
        string target = "North Korea";
        LevenshteinWeights weighted = new(1, 1, 2);

        Assert.Equal(2, Levenshtein.Distance(source, target));
        Assert.Equal(2, Levenshtein.Distance(source, target, 4));
        Assert.Equal(2, Levenshtein.Distance(source, target, 3));
        Assert.Equal(2, Levenshtein.Distance(source, target, 2));
        Assert.Equal(2, Levenshtein.Distance(source, target, 1));
        Assert.Equal(1, Levenshtein.Distance(source, target, 0));
        Assert.Equal(4, Levenshtein.Distance(source, target, weighted));
        Assert.Equal(4, Levenshtein.Distance(source, target, weighted, 4));
        Assert.Equal(4, Levenshtein.Distance(source, target, weighted, 3));
        Assert.Equal(3, Levenshtein.Distance(source, target, weighted, 2));
        Assert.Equal(2, Levenshtein.Distance(source, target, weighted, 1));
        Assert.Equal(1, Levenshtein.Distance(source, target, weighted, 0));

        source = "aabc";
        target = "cccd";

        Assert.Equal(4, Levenshtein.Distance(source, target));
        Assert.Equal(4, Levenshtein.Distance(source, target, 4));
        Assert.Equal(4, Levenshtein.Distance(source, target, 3));
        Assert.Equal(3, Levenshtein.Distance(source, target, 2));
        Assert.Equal(2, Levenshtein.Distance(source, target, 1));
        Assert.Equal(1, Levenshtein.Distance(source, target, 0));
        Assert.Equal(6, Levenshtein.Distance(source, target, weighted));
        Assert.Equal(6, Levenshtein.Distance(source, target, weighted, 6));
        Assert.Equal(6, Levenshtein.Distance(source, target, weighted, 5));
        Assert.Equal(5, Levenshtein.Distance(source, target, weighted, 4));
        Assert.Equal(4, Levenshtein.Distance(source, target, weighted, 3));
        Assert.Equal(3, Levenshtein.Distance(source, target, weighted, 2));
        Assert.Equal(2, Levenshtein.Distance(source, target, weighted, 1));
        Assert.Equal(1, Levenshtein.Distance(source, target, weighted, 0));
    }

    [Theory]
    [MemberData(nameof(UpstreamLevenshteinBandedCases))]
    public void UpstreamLevenshteinBandedFixturesRemainStable(
        string source,
        string target,
        int distance,
        int scoreCutoff,
        int cutoffResult)
    {
        Assert.Equal(distance, Levenshtein.Distance(source, target));
        Assert.Equal(cutoffResult, Levenshtein.Distance(source, target, scoreCutoff));
    }

    [Fact]
    public void LevenshteinSmallBandPathMatchesDynamicProgramming()
    {
        string source = Repeated("abcdefghij", 4);
        string target = "abcdxfghijabcdefghijabcxefghijabcdefghij";

        Assert.Equal(ExpectedLevenshteinDistance(source, target), Levenshtein.Distance(source, target, 4));
    }

    [Fact]
    public void UpstreamLcsAndIndelMblevenFixturesRemainStable()
    {
        string source = "South Korea";
        string target = "North Korea";

        Assert.Equal(9, LcsSeq.Similarity(source, target));
        Assert.Equal(9, LcsSeq.Similarity(source, target, 9));
        Assert.Equal(0, LcsSeq.Similarity(source, target, 10));
        Assert.Equal(2, LcsSeq.Distance(source, target));
        Assert.Equal(2, LcsSeq.Distance(source, target, 4));
        Assert.Equal(2, LcsSeq.Distance(source, target, 3));
        Assert.Equal(2, LcsSeq.Distance(source, target, 2));
        Assert.Equal(2, LcsSeq.Distance(source, target, 1));
        Assert.Equal(1, LcsSeq.Distance(source, target, 0));
        Assert.Equal(4, Indel.Distance(source, target));
        Assert.Equal(4, Indel.Distance(source, target, 5));
        Assert.Equal(4, Indel.Distance(source, target, 4));
        Assert.Equal(4, Indel.Distance(source, target, 3));
        Assert.Equal(3, Indel.Distance(source, target, 2));
        Assert.Equal(2, Indel.Distance(source, target, 1));
        Assert.Equal(1, Indel.Distance(source, target, 0));

        source = "aabc";
        target = "cccd";

        Assert.Equal(6, Indel.Distance(source, target));
        Assert.Equal(6, Indel.Distance(source, target, 6));
        Assert.Equal(6, Indel.Distance(source, target, 5));
        Assert.Equal(5, Indel.Distance(source, target, 4));
        Assert.Equal(4, Indel.Distance(source, target, 3));
        Assert.Equal(3, Indel.Distance(source, target, 2));
        Assert.Equal(2, Indel.Distance(source, target, 1));
        Assert.Equal(1, Indel.Distance(source, target, 0));
    }

    [Fact]
    public void LcsSeqCutoffsUseUpstreamMissThresholds()
    {
        Assert.Equal(3, LcsSeq.Similarity("abcdef", "azced", 3));
        Assert.Equal(0, LcsSeq.Similarity("abcdef", "azced", 4));
        Assert.Equal(3, LcsSeq.Distance("abcdef", "azced", 3));
        Assert.Equal(3, LcsSeq.Distance("abcdef", "azced", 2));
    }

    [Fact]
    public void LevenshteinEditopsCanApplyDestination()
    {
        string source = "Lorem ipsum.";
        string destination = "XYZLorem ABC iPsum";

        EditOperations operations = Levenshtein.Editops(source, destination);
        Opcodes opcodes = operations.ToOpcodes();

        Assert.Equal(destination, operations.ApplyTo(source, destination));
        Assert.Equal(destination, opcodes.ApplyTo(source, destination));
        Assert.Equal(destination, opcodes.ToEditOperations().ApplyTo(source, destination));
        Assert.Equal(source.Length, operations.SourceLength);
        Assert.Equal(destination.Length, operations.DestinationLength);
        Assert.NotEmpty(opcodes);
    }

    [Fact]
    public void UpstreamLevenshteinEditopsFuzzingRegressionsApplyDestinations()
    {
        AssertLevenshteinEditopsApply("b", "aaaaaaaaaaaaaaaabbaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        AssertLevenshteinEditopsApply("aa", "abb");
        AssertLevenshteinEditopsApply(Repeated("abb", 512), Repeated("ccccca", 512));
    }

    [Fact]
    public void SmallAlphabetDistancesMatchIndependentReferenceImplementations()
    {
        List<string> values = EnumerateStrings("abc", 4);

        foreach (string source in values)
        {
            foreach (string target in values)
            {
                int expectedLevenshtein = ExpectedLevenshteinDistance(source, target);
                int expectedLcs = ExpectedLcsSimilarity(source, target);
                int expectedIndel = source.Length + target.Length - (2 * expectedLcs);
                int expectedHamming = ExpectedHammingDistance(source, target);
                int expectedOsa = ExpectedOsaDistance(source, target);

                Assert.Equal(expectedLevenshtein, Levenshtein.Distance(source, target));
                Assert.Equal(expectedLcs, LcsSeq.Similarity(source, target));
                Assert.Equal(expectedIndel, Indel.Distance(source, target));
                Assert.Equal(expectedHamming, Hamming.Distance(source, target));
                Assert.Equal(expectedOsa, Osa.Distance(source, target));
            }
        }
    }

    [Fact]
    public void EditopsAndOpcodesCanInvertTransformations()
    {
        string source = "qabxcd";
        string destination = "abycdf";

        EditOperations operations = Levenshtein.Editops(source, destination);
        Opcodes opcodes = operations.ToOpcodes();

        Assert.Equal(source, operations.Inverse().ApplyTo(destination, source));
        Assert.Equal(source, opcodes.Inverse().ApplyTo(destination, source));
    }

    [Fact]
    public void EditopsAndOpcodesCanReverseAndCompareStructurally()
    {
        EditOperations operations = Levenshtein.Editops("kitten", "sitting");
        Opcodes opcodes = operations.ToOpcodes();

        Assert.Equal(operations, operations.Reverse().Reverse());
        Assert.Equal(opcodes, opcodes.Reverse().Reverse());
        Assert.NotEqual(operations, operations.Reverse());
        Assert.NotEqual(opcodes, opcodes.Reverse());
    }

    [Fact]
    public void EditopsAndOpcodesRejectInvalidCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EditOperations([new EditOp(EditOperation.Replace, 1, 0)], 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Opcodes([new Opcode(EditOperation.Insert, 1, 0, 0, 1)], 1, 1));
    }

    [Fact]
    public void EditopsCanRemoveSubsequence()
    {
        string source = "abc";
        string destination = "axbc";
        EditOperations operations = Levenshtein.Editops(source, destination);
        EditOperations subsequence = new([operations[0]], operations.SourceLength, operations.DestinationLength);

        EditOperations result = operations.RemoveSubsequence(subsequence);

        Assert.Empty(result);
        Assert.Equal(source.Length, result.SourceLength);
        Assert.Equal(destination.Length, result.DestinationLength);
    }

    [Fact]
    public void EditopsRejectSubsequenceThatIsNotPresent()
    {
        EditOperations operations = Levenshtein.Editops("abc", "axbc");
        EditOperations subsequence = new(
            [new EditOp(EditOperation.Delete, 0, 0)],
            operations.SourceLength,
            operations.DestinationLength);

        Assert.Throws<ArgumentException>(() => operations.RemoveSubsequence(subsequence));
    }

    [Fact]
    public void OpcodesExposeMatchingBlocks()
    {
        Opcodes opcodes = Levenshtein.Opcodes("abxcd", "abcd");
        MatchingBlock[] blocks = opcodes.GetMatchingBlocks();

        Assert.Equal(new MatchingBlock(0, 0, 2), blocks[0]);
        Assert.Equal(new MatchingBlock(3, 2, 2), blocks[1]);
        Assert.Equal(new MatchingBlock(5, 4, 0), blocks[^1]);
        Assert.Equal(blocks, opcodes.ToEditOperations().GetMatchingBlocks());
    }

    [Fact]
    public void IndelDistanceCountsInsertionsAndDeletions()
    {
        Assert.Equal(1, Indel.Distance("this is a test", "this is a test!"));
    }

    [Fact]
    public void IndelNormalizedSimilarityHandlesEmptyInputs()
    {
        Assert.Equal(1.0, Indel.NormalizedSimilarity("", ""));
    }

    [Fact]
    public void IndelEditopsAndOpcodesCanApplyDestination()
    {
        string source = "qabxcd";
        string destination = "abycdf";

        EditOperations operations = Indel.Editops(source, destination);
        Opcodes opcodes = Indel.Opcodes(source, destination);

        Assert.Equal(destination, operations.ApplyTo(source, destination));
        Assert.Equal(destination, opcodes.ApplyTo(source, destination));
        Assert.Equal(destination, opcodes.ToEditOperations().ApplyTo(source, destination));
    }

    [Fact]
    public void HammingDistanceMatchesUpstreamCases()
    {
        Assert.Equal(0, Hamming.Distance("aaaa", "aaaa"));
        Assert.Equal(1, Hamming.Distance("aaaa", "abaa"));
        Assert.Equal(1, Hamming.Distance("aaaa", "aaba"));
        Assert.Equal(2, Hamming.Distance("abaa", "aaba"));
        Assert.Equal(1, Hamming.Distance("aaaa", "aaaaa"));
    }

    [Fact]
    public void HammingWithoutPaddingRequiresEqualLengths()
    {
        Assert.Throws<ArgumentException>(() => Hamming.Distance("aaaa", "aaaaa", pad: false));
    }

    [Fact]
    public void HammingEditopsCanApplyDestination()
    {
        string source = "Lorem ipsum.";
        string destination = "XYZLorem ABC iPsum";

        EditOperations operations = Hamming.Editops(source, destination);

        Assert.Equal(destination, operations.ApplyTo(source, destination));
        Assert.Equal(source.Length, operations.SourceLength);
        Assert.Equal(destination.Length, operations.DestinationLength);
    }

    [Fact]
    public void LcsSeqReturnsExpectedScores()
    {
        Assert.Equal(3, LcsSeq.Similarity("abcdef", "acf"));
        Assert.Equal(3, LcsSeq.Distance("abcdef", "acf"));
        Assert.Equal(0.5, LcsSeq.NormalizedSimilarity("abcdef", "acf"));
        Assert.Equal("acf", LcsSeq.Editops("abcdef", "acf").ApplyTo("abcdef", "acf"));
    }

    [Fact]
    public void CommonAffixTrimmingPreservesDistanceScores()
    {
        string prefix = new('a', 128);
        string suffix = new('z', 128);
        string source = prefix + "kitten" + suffix;
        string target = prefix + "sitting" + suffix;

        Assert.Equal(3, Levenshtein.Distance(source, target));
        Assert.Equal(260, LcsSeq.Similarity(source, target));
        Assert.Equal(3, LcsSeq.Distance(source, target));
    }

    [Theory]
    [MemberData(nameof(LongBlockVectorCases))]
    public void LongInputsUseBlockVectorAndReturnExpectedScores(
        string source,
        string target,
        int levenshteinDistance,
        int lcsSimilarity,
        int indelDistance)
    {
        Assert.Equal(levenshteinDistance, Levenshtein.Distance(source, target));
        Assert.Equal(lcsSimilarity, LcsSeq.Similarity(source, target));
        Assert.Equal(indelDistance, Indel.Distance(source, target));
    }

    [Theory]
    [MemberData(nameof(BlockBoundaryParityCases))]
    public void BlockBoundaryInputsMatchIndependentDynamicProgramming(string source, string target)
    {
        int expectedLevenshteinDistance = ExpectedLevenshteinDistance(source, target);
        int expectedLcsSimilarity = ExpectedLcsSimilarity(source, target);
        int expectedIndelDistance = source.Length + target.Length - (2 * expectedLcsSimilarity);
        CachedLevenshtein cachedLevenshtein = new(source);
        CachedLcsSeq cachedLcsSeq = new(source);
        CachedIndel cachedIndel = new(source);

        Assert.Equal(expectedLevenshteinDistance, Levenshtein.Distance(source, target));
        Assert.Equal(expectedLcsSimilarity, LcsSeq.Similarity(source, target));
        Assert.Equal(expectedIndelDistance, Indel.Distance(source, target));
        Assert.Equal(expectedLevenshteinDistance, cachedLevenshtein.Distance(target));
        Assert.Equal(expectedLcsSimilarity, cachedLcsSeq.Similarity(target));
        Assert.Equal(expectedIndelDistance, cachedIndel.Distance(target));
    }

    [Theory]
    [MemberData(nameof(LongBlockVectorCases))]
    public void CachedLongInputsMatchStaticScorers(
        string source,
        string target,
        int levenshteinDistance,
        int lcsSimilarity,
        int indelDistance)
    {
        CachedLevenshtein levenshtein = new(source);
        CachedLcsSeq lcsSeq = new(source);
        CachedIndel indel = new(source);

        Assert.Equal(levenshteinDistance, levenshtein.Distance(target, scoreHint: levenshteinDistance));
        Assert.Equal(lcsSimilarity, lcsSeq.Similarity(target, scoreHint: lcsSimilarity));
        Assert.Equal(indelDistance, indel.Distance(target, scoreHint: indelDistance));
        Assert.Equal(Levenshtein.NormalizedSimilarity(source, target), levenshtein.NormalizedSimilarity(target, scoreHint: 0.5), 12);
        Assert.Equal(LcsSeq.NormalizedSimilarity(source, target), lcsSeq.NormalizedSimilarity(target, scoreHint: 0.5), 12);
        Assert.Equal(Indel.NormalizedSimilarity(source, target), indel.NormalizedSimilarity(target, scoreHint: 0.5), 12);
    }

    [Fact]
    public void LongInputCutoffsReturnSentinels()
    {
        string source = Repeated("abcdefghij", 8);
        string target = Repeated("Zbcdefghij", 8);

        Assert.Equal(6, Levenshtein.Distance(source, target, 5, 4));
        Assert.Equal(11, Indel.Distance(source, target, 10, 8));
        Assert.Equal(0, LcsSeq.Similarity(source, target, 73, 73));
    }

    [Fact]
    public void HintedSelectorsMatchFullDistancesForLongInputs()
    {
        string source = Repeated("abcdefghi", 18);
        string target = Repeated("abcxefghi", 18) + "tail";
        CachedLevenshtein cachedLevenshtein = new(source);
        CachedIndel cachedIndel = new(source);
        CachedLcsSeq cachedLcsSeq = new(source);

        Assert.Equal(Levenshtein.Distance(source, target), Levenshtein.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Indel.Distance(source, target), Indel.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(LcsSeq.Distance(source, target), LcsSeq.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Levenshtein.Distance(source, target), cachedLevenshtein.Distance(target, scoreHint: 1));
        Assert.Equal(Indel.Distance(source, target), cachedIndel.Distance(target, scoreHint: 1));
        Assert.Equal(LcsSeq.Distance(source, target), cachedLcsSeq.Distance(target, scoreHint: 1));
    }

    [Fact]
    public void CompactTraceEditopsApplyLongDestinations()
    {
        string source = Repeated("abcdnopq", 24);
        string target = "prefix" + Repeated("abxdnop", 24) + "suffix";

        EditOperations levenshteinOperations = Levenshtein.Editops(source, target);
        EditOperations lcsOperations = LcsSeq.Editops(source, target);
        Opcodes levenshteinOpcodes = Levenshtein.Opcodes(source, target);
        Opcodes indelOpcodes = Indel.Opcodes(source, target);

        Assert.Equal(target, levenshteinOperations.ApplyTo(source, target));
        Assert.Equal(target, lcsOperations.ApplyTo(source, target));
        Assert.Equal(target, levenshteinOpcodes.ApplyTo(source, target));
        Assert.Equal(target, indelOpcodes.ApplyTo(source, target));
    }

    [Theory]
    [InlineData("abcdefgh", "azcedfgh")]
    [InlineData("abcdefghi", "axcyezghi")]
    [InlineData("abcdefghijklmnop", "abxdxfghxjklxnop")]
    public void LcsUnrolledPathMatchesIndependentDynamicProgramming(string source, string target)
    {
        int expected = ExpectedLcsSimilarity(source, target);

        Assert.Equal(expected, LcsSeq.Similarity(source, target));
        Assert.Equal(expected, new CachedLcsSeq(source).Similarity(target));
    }

    [Theory]
    [MemberData(nameof(BlockBoundaryParityCases))]
    public void LcsBlockwiseCutoffsMatchIndependentDynamicProgramming(string source, string target)
    {
        int expected = ExpectedLcsSimilarity(source, target);

        Assert.Equal(expected, LcsSeq.Similarity(source, target, Math.Max(0, expected - 1)));
        Assert.Equal(0, LcsSeq.Similarity(source, target, expected + 1));
        Assert.Equal(expected, new CachedLcsSeq(source).Similarity(target, Math.Max(0, expected - 1)));
    }

    [Fact]
    public void RandomizedDistanceParityMatchesIndependentDynamicProgramming()
    {
        Random random = new(8675309);

        for (int iteration = 0; iteration < 64; iteration++)
        {
            string source = RandomString(random, random.Next(0, 20));
            string target = RandomString(random, random.Next(0, 20));
            int expectedLevenshtein = ExpectedLevenshteinDistance(source, target);
            int expectedLcs = ExpectedLcsSimilarity(source, target);
            int expectedIndel = source.Length + target.Length - (2 * expectedLcs);

            Assert.Equal(expectedLevenshtein, Levenshtein.Distance(source, target, int.MaxValue, 1));
            Assert.Equal(expectedLcs, LcsSeq.Similarity(source, target));
            Assert.Equal(expectedIndel, Indel.Distance(source, target, int.MaxValue, 1));
            Assert.Equal(target, Levenshtein.Editops(source, target).ApplyTo(source, target));
            Assert.Equal(target, Indel.Editops(source, target).ApplyTo(source, target));
        }
    }

    [Fact]
    public void PrefixAndPostfixReturnCommonAffixScores()
    {
        Assert.Equal(7, Prefix.Similarity("prefix-alpha", "prefix-beta"));
        Assert.Equal(5, Postfix.Similarity("alpha-end", "beta-end"));
    }

    [Fact]
    public void OsaHandlesAdjacentTransposition()
    {
        Assert.Equal(1, Osa.Distance("CA", "AC"));
        Assert.Equal(3, Osa.Distance("a" + new string('a', 64) + "CAa", "b" + new string('a', 64) + "ACb"));
    }

    [Fact]
    public void DamerauLevenshteinHandlesUnrestrictedTransposition()
    {
        Assert.Equal(1, DamerauLevenshtein.Distance("abaa", "baaa"));
        Assert.Equal(2, DamerauLevenshtein.Distance("CA", "ABC"));
        Assert.Equal(0.75, DamerauLevenshtein.NormalizedSimilarity("abaa", "baaa"), 4);
    }

    [Fact]
    public void JaroMatchesKnownExamples()
    {
        Assert.Equal(0.9444444444444445, Jaro.Similarity("MARTHA", "MARHTA"), 12);
        Assert.Equal(0.2333333333333333, Jaro.Distance("DIXON", "DICKSONX"), 12);
    }

    [Fact]
    public void JaroWinklerMatchesKnownExamples()
    {
        Assert.Equal(0.9611111111111111, JaroWinkler.Similarity("MARTHA", "MARHTA"), 12);
        Assert.Equal(0.8133333333333332, JaroWinkler.Similarity("DIXON", "DICKSONX"), 12);
    }

    [Theory]
    [InlineData("DWAYNE", "DUANE", 0.8222222222222223, 0.8400000000000001)]
    [InlineData("CRATE", "TRACE", 0.7333333333333334, 0.7333333333333334)]
    public void JaroAndJaroWinklerMatchAdditionalKnownExamples(
        string source,
        string target,
        double jaroSimilarity,
        double jaroWinklerSimilarity)
    {
        Assert.Equal(jaroSimilarity, Jaro.Similarity(source, target), 12);
        Assert.Equal(jaroWinklerSimilarity, JaroWinkler.Similarity(source, target), 12);
    }

    [Theory]
    [MemberData(nameof(LongJaroOsaParityCases))]
    public void LongJaroAndOsaCasesMatchRapidFuzzParity(
        string source,
        string target,
        double jaroSimilarity,
        double jaroWinklerSimilarity,
        int osaDistance,
        double osaNormalizedSimilarity)
    {
        CachedJaro cachedJaro = new(source);
        CachedJaroWinkler cachedJaroWinkler = new(source);

        Assert.Equal(jaroSimilarity, Jaro.Similarity(source, target), 12);
        Assert.Equal(jaroSimilarity, cachedJaro.Similarity(target), 12);
        Assert.Equal(jaroWinklerSimilarity, JaroWinkler.Similarity(source, target), 12);
        Assert.Equal(jaroWinklerSimilarity, cachedJaroWinkler.Similarity(target), 12);
        Assert.Equal(osaDistance, Osa.Distance(source, target));
        Assert.Equal(osaNormalizedSimilarity, Osa.NormalizedSimilarity(source, target), 12);
    }

    [Fact]
    public void CachedDistanceScorersMatchStaticScorers()
    {
        string source = "kitten";
        string target = "sitting";

        CachedLevenshtein levenshtein = new(source);
        CachedIndel indel = new(source);
        CachedLcsSeq lcsSeq = new(source);
        CachedOsa osa = new(source);
        CachedDamerauLevenshtein damerauLevenshtein = new(source);
        CachedJaro jaro = new(source);
        CachedJaroWinkler jaroWinkler = new(source);
        CachedPrefix prefix = new(source);
        CachedPostfix postfix = new(source);

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
        Assert.Equal(Jaro.NormalizedDistance(source, target), jaro.NormalizedDistance(target), 12);
        Assert.Equal(Jaro.NormalizedSimilarity(source, target), jaro.NormalizedSimilarity(target), 12);

        Assert.Equal(JaroWinkler.Distance(source, target), jaroWinkler.Distance(target), 12);
        Assert.Equal(JaroWinkler.Similarity(source, target), jaroWinkler.Similarity(target), 12);
        Assert.Equal(JaroWinkler.NormalizedDistance(source, target), jaroWinkler.NormalizedDistance(target), 12);
        Assert.Equal(JaroWinkler.NormalizedSimilarity(source, target), jaroWinkler.NormalizedSimilarity(target), 12);

        Assert.Equal(Prefix.Distance(source, target), prefix.Distance(target));
        Assert.Equal(Prefix.Similarity(source, target), prefix.Similarity(target));
        Assert.Equal(Prefix.NormalizedDistance(source, target), prefix.NormalizedDistance(target), 12);
        Assert.Equal(Prefix.NormalizedSimilarity(source, target), prefix.NormalizedSimilarity(target), 12);

        Assert.Equal(Postfix.Distance(source, target), postfix.Distance(target));
        Assert.Equal(Postfix.Similarity(source, target), postfix.Similarity(target));
        Assert.Equal(Postfix.NormalizedDistance(source, target), postfix.NormalizedDistance(target), 12);
        Assert.Equal(Postfix.NormalizedSimilarity(source, target), postfix.NormalizedSimilarity(target), 12);
    }

    [Fact]
    public void CachedDistanceScorersPreserveResultsWithScoreHint()
    {
        string source = "kitten";
        string target = "sitting";
        CachedLevenshtein levenshtein = new(source);
        CachedIndel indel = new(source);
        CachedJaro jaro = new(source);

        Assert.Equal(levenshtein.Distance(target), levenshtein.Distance(target, scoreHint: 10));
        Assert.Equal(indel.NormalizedSimilarity(target), indel.NormalizedSimilarity(target, scoreHint: 0.5), 12);
        Assert.Equal(jaro.Similarity(target), jaro.Similarity(target, scoreHint: 0.5), 12);
    }

    [Fact]
    public void CachedHammingMatchesStaticScorer()
    {
        string source = "karolin";
        string target = "kathrin";

        CachedHamming hamming = new(source);

        Assert.Equal(Hamming.Distance(source, target), hamming.Distance(target));
        Assert.Equal(Hamming.Similarity(source, target), hamming.Similarity(target));
        Assert.Equal(Hamming.NormalizedDistance(source, target), hamming.NormalizedDistance(target), 12);
        Assert.Equal(Hamming.NormalizedSimilarity(source, target), hamming.NormalizedSimilarity(target), 12);
        Assert.Equal(target, hamming.Editops(target).ApplyTo(source, target));
    }

    [Fact]
    public void StaticDistanceScorersPreserveResultsWithScoreHint()
    {
        string source = "kitten";
        string target = "sitting";

        Assert.Equal(Levenshtein.Distance(source, target), Levenshtein.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Levenshtein.Similarity(source, target), Levenshtein.Similarity(source, target, 0, 1));
        Assert.Equal(Levenshtein.NormalizedDistance(source, target), Levenshtein.NormalizedDistance(source, target, 1.0, 0.1), 12);
        Assert.Equal(Levenshtein.NormalizedSimilarity(source, target), Levenshtein.NormalizedSimilarity(source, target, 0.0, 0.1), 12);

        Assert.Equal(Indel.Distance(source, target), Indel.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Indel.Similarity(source, target), Indel.Similarity(source, target, 0, 1));
        Assert.Equal(Indel.NormalizedDistance(source, target), Indel.NormalizedDistance(source, target, 1.0, 0.1), 12);
        Assert.Equal(Indel.NormalizedSimilarity(source, target), Indel.NormalizedSimilarity(source, target, 0.0, 0.1), 12);

        Assert.Equal(Hamming.Distance(source, target), Hamming.Distance(source, target, true, int.MaxValue, 1));
        Assert.Equal(LcsSeq.Distance(source, target), LcsSeq.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Osa.Distance(source, target), Osa.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(DamerauLevenshtein.Distance(source, target), DamerauLevenshtein.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Jaro.Similarity(source, target), Jaro.Similarity(source, target, 0.0, 0.1), 12);
        Assert.Equal(JaroWinkler.Similarity(source, target), JaroWinkler.Similarity(source, target, 0.1, 0.0, 0.1), 12);
        Assert.Equal(Prefix.Distance(source, target), Prefix.Distance(source, target, int.MaxValue, 1));
        Assert.Equal(Postfix.Distance(source, target), Postfix.Distance(source, target, int.MaxValue, 1));
    }

    [Fact]
    public void GenericSequenceDistancesMatchExpectedValues()
    {
        int[] source = [1, 2, 3, 4];
        int[] target = [1, 3, 2, 4, 5];

        Assert.Equal(3, Levenshtein.Distance<int>(source, target));
        Assert.Equal(3, LcsSeq.Similarity<int>(source, target));
        Assert.Equal(3, Indel.Distance<int>(source, target));
        Assert.Equal(3, Hamming.Distance<int>(source, target));
        Assert.Equal(2, Osa.Distance<int>(source, target));
        Assert.Equal(2, DamerauLevenshtein.Distance<int>(source, target));
        Assert.Equal(1, Prefix.Similarity<int>(source, target));
        Assert.Equal(0, Postfix.Similarity<int>(source, target));
    }

    [Fact]
    public void GenericSequenceScorersSupportWeightsAndNormalizedScores()
    {
        int[] source = [1, 2, 3, 4];
        int[] target = [1, 3, 2, 4];
        LevenshteinWeights weights = new(1, 1, 2);

        Assert.Equal(2, Levenshtein.Distance<int>(source, target, weights));
        Assert.Equal(0.75, Levenshtein.NormalizedSimilarity<int>(source, target, weights), 12);
        Assert.Equal(LcsSeq.NormalizedSimilarity<int>(source, target), Indel.NormalizedSimilarity<int>(source, target), 12);
    }

    [Fact]
    public void GenericJaroScorersMatchEquivalentCharacterSequences()
    {
        int[] source = [1, 2, 3, 4, 5];
        int[] target = [1, 3, 2, 4, 5];

        Assert.Equal(Jaro.Similarity("\u0001\u0002\u0003\u0004\u0005", "\u0001\u0003\u0002\u0004\u0005"), Jaro.Similarity<int>(source, target), 12);
        Assert.Equal(
            JaroWinkler.Similarity("\u0001\u0002\u0003\u0004\u0005", "\u0001\u0003\u0002\u0004\u0005"),
            JaroWinkler.Similarity<int>(source, target),
            12);
    }

    [Fact]
    public void GenericLevenshteinEditopsApplyIntegerSequences()
    {
        int[] source = [1, 2, 3, 4];
        int[] target = [1, 3, 4, 5];

        EditOperations editOperations = Levenshtein.Editops<int>(source, target);
        Opcodes opcodes = Levenshtein.Opcodes<int>(source, target);

        Assert.Equal(target, editOperations.ApplyTo<int>(source, target));
        Assert.Equal(target, opcodes.ApplyTo<int>(source, target));
        Assert.Equal(Levenshtein.Distance<int>(source, target), editOperations.Count);
    }

    [Fact]
    public void GenericLcsAndIndelEditopsApplyRecordSequences()
    {
        SequenceToken[] source = [new(1), new(2), new(3), new(4)];
        SequenceToken[] target = [new(1), new(3), new(4), new(5)];

        EditOperations lcsEditOperations = LcsSeq.Editops<SequenceToken>(source, target);
        Opcodes indelOpcodes = Indel.Opcodes<SequenceToken>(source, target);

        Assert.Equal(target, lcsEditOperations.ApplyTo<SequenceToken>(source, target));
        Assert.Equal(target, indelOpcodes.ApplyTo<SequenceToken>(source, target));
    }

    [Fact]
    public void GenericHammingEditopsApplyPaddedCharacterSequences()
    {
        char[] source = ['a', 'b', 'c'];
        char[] target = ['a', 'x', 'c', 'd'];

        EditOperations editOperations = Hamming.Editops<char>(source, target);

        Assert.Equal(target, editOperations.ApplyTo<char>(source, target));
        Assert.Throws<ArgumentException>(() => Hamming.Editops<char>(source, target, false));
    }

    [Fact]
    public void StaticScoreHintsRejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Levenshtein.Distance("a", "b", int.MaxValue, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Indel.NormalizedSimilarity("a", "b", 0.0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Hamming.Editops("a", "b", true, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Jaro.Similarity("a", "b", 0.0, 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => JaroWinkler.Distance("a", "b", 0.1, 1.0, -0.1));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("abc", "acb")]
    [InlineData("kitten", "sitting")]
    [InlineData("fuzzy wuzzy", "wuzzy fuzzy")]
    public void DistanceSymmetryInvariantsHold(string source, string target)
    {
        Assert.Equal(Levenshtein.Distance(source, target), Levenshtein.Distance(target, source));
        Assert.Equal(Indel.Distance(source, target), Indel.Distance(target, source));
        Assert.Equal(Hamming.Distance(source, target), Hamming.Distance(target, source));
        Assert.Equal(LcsSeq.Similarity(source, target), LcsSeq.Similarity(target, source));
        Assert.Equal(Osa.Distance(source, target), Osa.Distance(target, source));
        Assert.Equal(DamerauLevenshtein.Distance(source, target), DamerauLevenshtein.Distance(target, source));
        Assert.Equal(Jaro.Similarity(source, target), Jaro.Similarity(target, source), 12);
    }

    [Theory]
    [InlineData("abc", "xyz", 2, 3)]
    [InlineData("kitten", "sitting", 2, 3)]
    [InlineData("Saturday", "Sunday", 2, 3)]
    public void DistanceCutoffsReturnSentinelAboveCutoff(string source, string target, int scoreCutoff, int expected)
    {
        Assert.Equal(expected, Levenshtein.Distance(source, target, scoreCutoff, 1));
        Assert.Equal(expected, Osa.Distance(source, target, scoreCutoff, 1));
    }

    [Fact]
    public void EditopsWithScoreHintApplyDestination()
    {
        string source = "qabxcd";
        string target = "abycdf";

        Assert.Equal(target, Levenshtein.Editops(source, target, 1).ApplyTo(source, target));
        Assert.Equal(target, Indel.Editops(source, target, 1).ApplyTo(source, target));
        Assert.Equal(target, LcsSeq.Editops(source, target, 1).ApplyTo(source, target));
    }

    public static TheoryData<string, string, int, int, int> LongBlockVectorCases => new()
    {
        { Repeated("abcdefghij", 8), Repeated("Zbcdefghij", 8), 8, 72, 16 },
        { Repeated("abçdéζηθ", 10), Repeated("Ωbçdéζηθ", 10), 10, 70, 20 },
        { Repeated("abcdefghij", 8), "X" + Repeated("abcdefghij", 8) + "Y", 2, 80, 2 },
        { Repeated("abcdefghij", 8), Repeated("Zbcdefghij", 8) + "tail", 12, 72, 20 }
    };

    public static TheoryData<string, string, int, int, int> UpstreamLevenshteinBandedCases => new()
    {
        {
            "kkkkbbbbfkkkkkkibfkkkafakkfekgkkkkkkkkkkbdbbddddddddddafkkkekkkhkk",
            "khddddddddkkkkdgkdikkccccckcckkkekkkkdddddddddddafkkhckkkkkdckkkcc",
            36,
            31,
            32
        },
        {
            "ccddcddddddddddddddddddddddddddddddddddddddddddddddddddddaaaaaaaaaaa",
            "aaaaaaaaaaaaaadddddddddbddddddddddddddddddddddddddddddddddbddddddddd",
            26,
            31,
            26
        },
        {
            "accccccccccaaaaaaaccccccccccccccccccccccccccccccacccccccccccccccccccccccccccccccccccccccccccccccccaaaaaaaaaaaaacccccccccccccccccccccc",
            "ccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccbcccb",
            24,
            25,
            24
        },
        {
            "miiiiiiiiiiliiiiiiibghiiaaaaaaaaaaaaaaacccfccccedddaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaajaaaaaaaabghiiaaaaaaaaaaaaaaacccfccccedddaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaajjdim",
            27,
            27,
            27
        }
    };

    public static TheoryData<string, string> BlockBoundaryParityCases => new()
    {
        { BoundarySource(63), BoundaryTarget(63) },
        { BoundarySource(64), BoundaryTarget(64) },
        { BoundarySource(65), BoundaryTarget(65) },
        { BoundarySource(128), BoundaryTarget(128) },
        { BoundarySource(129), BoundaryTarget(129) },
        { Repeated("ab\u00e7d\u03b7", 26), Repeated("ab\u00e7x\u03b7", 26) }
    };

    public static TheoryData<string, string, int, int, int, int, int, int, double, double> AdditionalParityCases => new()
    {
        { "xabcdxefgh", "abxdxefg", 3, 7, 3, 4, 14, 3, 0.8916666666666666, 0.8916666666666666 },
        { "prefixAAAAAsuffix", "prefixAAAABsuffix", 1, 16, 1, 2, 32, 1, 0.9607843137254902, 0.9764705882352941 },
        { Repeated("abc", 30), Repeated("acb", 30), 60, 60, 30, 60, 120, 30, 0.8888888888888888, 0.8999999999999999 }
    };

    public static TheoryData<string, string, double, double, int, double> LongJaroOsaParityCases => new()
    {
        { new string('a', 70) + "bcdef" + new string('z', 15), new string('a', 68) + "xbcdyf" + new string('z', 18), 0.9707729468599035, 0.982463768115942, 6, 0.9347826086956522 },
        { Repeated("0123456789", 9), Repeated("0012345678", 8) + "99", 0.7693224932249323, 0.7923902439024391, 17, 0.8111111111111111 },
        { Repeated("abcdefghij", 12), Repeated("abcdxfghij", 12), 0.9333333333333332, 0.96, 12, 0.9 }
    };

    private static string Repeated(string value, int count)
    {
        return string.Concat(Enumerable.Repeat(value, count));
    }

    private static void AssertLevenshteinEditopsApply(string source, string target)
    {
        EditOperations operations = Levenshtein.Editops(source, target);

        Assert.Equal(target, operations.ApplyTo(source, target));
    }

    private static List<string> EnumerateStrings(string alphabet, int maxLength)
    {
        List<string> values = new() { string.Empty };

        for (int length = 1; length <= maxLength; length++)
        {
            AppendStrings(values, alphabet, new char[length], 0);
        }

        return values;
    }

    private static void AppendStrings(List<string> values, string alphabet, char[] buffer, int index)
    {
        if (index == buffer.Length)
        {
            values.Add(new string(buffer));
            return;
        }

        for (int i = 0; i < alphabet.Length; i++)
        {
            buffer[index] = alphabet[i];
            AppendStrings(values, alphabet, buffer, index + 1);
        }
    }

    private static string BoundarySource(int length)
    {
        char[] value = new char[length];
        Array.Fill(value, 'a');

        for (int i = 1; i < value.Length; i += 7)
        {
            value[i] = 'b';
        }

        return new string(value);
    }

    private static string BoundaryTarget(int length)
    {
        char[] value = new char[length + (length % 2)];
        Array.Fill(value, 'a');

        for (int i = 2; i < value.Length; i += 9)
        {
            value[i] = 'c';
        }

        return new string(value);
    }

    private static string RandomString(Random random, int length)
    {
        char[] value = new char[length];

        for (int i = 0; i < value.Length; i++)
        {
            value[i] = (char)('a' + random.Next(0, 5));
        }

        return new string(value);
    }

    private static int ExpectedLevenshteinDistance(string source, string target)
    {
        int[,] distances = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (int j = 0; j <= target.Length; j++)
        {
            distances[0, j] = j;
        }

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;
                int deletion = distances[i - 1, j] + 1;
                int insertion = distances[i, j - 1] + 1;
                int substitution = distances[i - 1, j - 1] + substitutionCost;
                distances[i, j] = Math.Min(Math.Min(deletion, insertion), substitution);
            }
        }

        return distances[source.Length, target.Length];
    }

    private static int ExpectedHammingDistance(string source, string target)
    {
        int distance = Math.Abs(source.Length - target.Length);
        int length = Math.Min(source.Length, target.Length);

        for (int i = 0; i < length; i++)
        {
            if (source[i] != target[i])
            {
                distance++;
            }
        }

        return distance;
    }

    private static int ExpectedOsaDistance(string source, string target)
    {
        int[,] distances = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (int j = 0; j <= target.Length; j++)
        {
            distances[0, j] = j;
        }

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;
                int deletion = distances[i - 1, j] + 1;
                int insertion = distances[i, j - 1] + 1;
                int substitution = distances[i - 1, j - 1] + substitutionCost;
                int distance = Math.Min(Math.Min(deletion, insertion), substitution);

                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                {
                    distance = Math.Min(distance, distances[i - 2, j - 2] + 1);
                }

                distances[i, j] = distance;
            }
        }

        return distances[source.Length, target.Length];
    }

    private static int ExpectedLcsSimilarity(string source, string target)
    {
        int[,] similarities = new int[source.Length + 1, target.Length + 1];

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                similarities[i, j] = source[i - 1] == target[j - 1]
                    ? similarities[i - 1, j - 1] + 1
                    : Math.Max(similarities[i - 1, j], similarities[i, j - 1]);
            }
        }

        return similarities[source.Length, target.Length];
    }

    private readonly record struct SequenceToken(int Value);
}
