using RapidFuzz;

namespace RapidFuzzDotNet.Tests;

public sealed class FuzzTests
{
    [Fact]
    public void RatioMatchesRapidFuzzExample()
    {
        double score = Fuzz.Ratio("this is a test", "this is a test!");

        Assert.Equal(96.55172413793103, score, 10);
    }

    [Fact]
    public void RatioAppliesScoreCutoff()
    {
        double score = Fuzz.Ratio("this is a test", "this is a test!", 97.0);

        Assert.Equal(0.0, score);
    }

    [Fact]
    public void PartialRatioFindsEmbeddedMatch()
    {
        double score = Fuzz.PartialRatio("this is a test", "this is a test!");

        Assert.Equal(100.0, score);
    }

    [Fact]
    public void PartialRatioAlignmentFindsEmbeddedMatch()
    {
        ScoreAlignment alignment = Fuzz.PartialRatioAlignment("test", "xx test yy");

        Assert.Equal(100.0, alignment.Score);
        Assert.Equal(0, alignment.SourceStart);
        Assert.Equal(4, alignment.SourceEnd);
        Assert.Equal(3, alignment.DestinationStart);
        Assert.Equal(7, alignment.DestinationEnd);
    }

    [Fact]
    public void PartialRatioAlignmentFindsLongEmbeddedMatch()
    {
        string source = string.Concat(Enumerable.Repeat("abcdefghij", 8));
        string target = "prefix-" + source + "-suffix";

        ScoreAlignment alignment = Fuzz.PartialRatioAlignment(source, target);

        Assert.Equal(100.0, alignment.Score);
        Assert.Equal(7, alignment.DestinationStart);
        Assert.Equal(7 + source.Length, alignment.DestinationEnd);
    }

    [Fact]
    public void TokenSortRatioIgnoresTokenOrder()
    {
        double score = Fuzz.TokenSortRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear");

        Assert.Equal(100.0, score);
    }

    [Fact]
    public void TokenSetRatioReturnsPerfectScoreForSubset()
    {
        double score = Fuzz.TokenSetRatio("fuzzy was a bear but not a dog", "fuzzy was a bear");

        Assert.Equal(100.0, score);
    }

    [Fact]
    public void TokenSetRatioReducesScoreForExplicitDisagreement()
    {
        double score = Fuzz.TokenSetRatio("fuzzy was a bear but not a dog", "fuzzy was a bear but not a cat");

        Assert.InRange(score, 90.0, 95.0);
    }

    [Theory]
    [InlineData("fuzzy was a bear but not a dog", "fuzzy was a bear but not a cat", 94.73684210526316)]
    [InlineData("new york mets", "new york yankees", 81.81818181818181)]
    [InlineData("abcd", "XbcY", 57.14285714285714)]
    public void PartialRatioMatchesUpstreamRegressionCases(string source, string target, double expected)
    {
        double score = Fuzz.PartialRatio(source, target);

        Assert.Equal(expected, score, 10);
    }

    [Fact]
    public void PartialTokenScorersMatchUpstreamIntersectionBehavior()
    {
        string source = "fuzzy was a bear but not a dog";
        string target = "fuzzy was a bear but not a cat";

        Assert.Equal(100.0, Fuzz.PartialTokenSetRatio(source, target));
        Assert.Equal(100.0, Fuzz.PartialTokenRatio(source, target));
    }

    [Fact]
    public void TokenScorersHandleDuplicateWhitespaceTokens()
    {
        string source = "  beta   alpha alpha   gamma ";
        string target = "gamma beta   delta alpha";
        CachedTokenSortRatio cachedTokenSortRatio = new(source);
        CachedTokenSetRatio cachedTokenSetRatio = new(source);
        CachedPartialTokenRatio cachedPartialTokenRatio = new(source);

        Assert.Equal(Fuzz.TokenSortRatio(source, target), cachedTokenSortRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.TokenSetRatio(source, target), cachedTokenSetRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialTokenRatio(source, target), cachedPartialTokenRatio.Similarity(target), 10);
        Assert.Equal(100.0, Fuzz.PartialTokenSetRatio(source, target));
    }

    [Theory]
    [MemberData(nameof(TokenParityCases))]
    public void TokenScorersMatchRapidFuzzParityCases(
        string source,
        string target,
        double tokenSortRatio,
        double partialTokenSortRatio,
        double tokenSetRatio,
        double partialTokenSetRatio,
        double tokenRatio,
        double partialTokenRatio,
        double wRatio,
        double qRatio)
    {
        Assert.Equal(tokenSortRatio, Fuzz.TokenSortRatio(source, target), 10);
        Assert.Equal(partialTokenSortRatio, Fuzz.PartialTokenSortRatio(source, target), 10);
        Assert.Equal(tokenSetRatio, Fuzz.TokenSetRatio(source, target), 10);
        Assert.Equal(partialTokenSetRatio, Fuzz.PartialTokenSetRatio(source, target), 10);
        Assert.Equal(tokenRatio, Fuzz.TokenRatio(source, target), 10);
        Assert.Equal(partialTokenRatio, Fuzz.PartialTokenRatio(source, target), 10);
        Assert.Equal(wRatio, Fuzz.WRatio(source, target), 10);
        Assert.Equal(qRatio, Fuzz.QRatio(source, target), 10);
    }

    [Theory]
    [MemberData(nameof(TokenParityCases))]
    public void CachedTokenScorersMatchTokenParityCases(
        string source,
        string target,
        double tokenSortRatio,
        double partialTokenSortRatio,
        double tokenSetRatio,
        double partialTokenSetRatio,
        double tokenRatio,
        double partialTokenRatio,
        double wRatio,
        double qRatio)
    {
        CachedTokenSortRatio cachedTokenSortRatio = new(source);
        CachedPartialTokenSortRatio cachedPartialTokenSortRatio = new(source);
        CachedTokenSetRatio cachedTokenSetRatio = new(source);
        CachedPartialTokenSetRatio cachedPartialTokenSetRatio = new(source);
        CachedTokenRatio cachedTokenRatio = new(source);
        CachedPartialTokenRatio cachedPartialTokenRatio = new(source);
        CachedWRatio cachedWRatio = new(source);
        CachedQRatio cachedQRatio = new(source);

        Assert.Equal(tokenSortRatio, cachedTokenSortRatio.Similarity(target), 10);
        Assert.Equal(partialTokenSortRatio, cachedPartialTokenSortRatio.Similarity(target), 10);
        Assert.Equal(tokenSetRatio, cachedTokenSetRatio.Similarity(target), 10);
        Assert.Equal(partialTokenSetRatio, cachedPartialTokenSetRatio.Similarity(target), 10);
        Assert.Equal(tokenRatio, cachedTokenRatio.Similarity(target), 10);
        Assert.Equal(partialTokenRatio, cachedPartialTokenRatio.Similarity(target), 10);
        Assert.Equal(wRatio, cachedWRatio.Similarity(target), 10);
        Assert.Equal(qRatio, cachedQRatio.Similarity(target), 10);
    }

    [Fact]
    public void GenericFuzzScorersMatchEquivalentCharacterSequences()
    {
        int[] source = [1, 2, 3, 4];
        int[] target = [0, 1, 2, 3, 4, 5];
        string sourceText = "\u0001\u0002\u0003\u0004";
        string targetText = "\u0000\u0001\u0002\u0003\u0004\u0005";

        ScoreAlignment characterAlignment = Fuzz.PartialRatioAlignment(sourceText, targetText);
        ScoreAlignment genericAlignment = Fuzz.PartialRatioAlignment<int>(source, target);

        Assert.Equal(Fuzz.Ratio(sourceText, targetText), Fuzz.Ratio<int>(source, target), 10);
        Assert.Equal(Fuzz.PartialRatio(sourceText, targetText), Fuzz.PartialRatio<int>(source, target), 10);
        Assert.Equal(characterAlignment.Score, genericAlignment.Score, 10);
        Assert.Equal(characterAlignment.SourceStart, genericAlignment.SourceStart);
        Assert.Equal(characterAlignment.SourceEnd, genericAlignment.SourceEnd);
        Assert.Equal(characterAlignment.DestinationStart, genericAlignment.DestinationStart);
        Assert.Equal(characterAlignment.DestinationEnd, genericAlignment.DestinationEnd);
    }

    [Fact]
    public void GenericCachedFuzzScorersMatchStaticSequenceScorers()
    {
        int[] source = [1, 2, 3, 4];
        int[] target = [0, 1, 2, 3, 4, 5];
        CachedRatio<int> ratio = new(source);
        CachedPartialRatio<int> partialRatio = new(source);
        ScoreAlignment staticAlignment = Fuzz.PartialRatioAlignment<int>(source, target);
        ScoreAlignment cachedAlignment = partialRatio.Alignment(target);

        Assert.Equal(Fuzz.Ratio<int>(source, target), ratio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialRatio<int>(source, target), partialRatio.Similarity(target), 10);
        Assert.Equal(staticAlignment.Score, cachedAlignment.Score, 10);
        Assert.Equal(staticAlignment.SourceStart, cachedAlignment.SourceStart);
        Assert.Equal(staticAlignment.SourceEnd, cachedAlignment.SourceEnd);
        Assert.Equal(staticAlignment.DestinationStart, cachedAlignment.DestinationStart);
        Assert.Equal(staticAlignment.DestinationEnd, cachedAlignment.DestinationEnd);
        Assert.Equal(0.0, ratio.Similarity(target, Fuzz.Ratio<int>(source, target) + 0.0001));
    }

    [Fact]
    public void CachedWRatioMatchesStaticScorerAcrossTokenBranches()
    {
        string source = "alpha beta gamma delta";
        string target = "gamma alpha beta epsilon";
        CachedWRatio cachedWRatio = new(source);
        double score = Fuzz.WRatio(source, target);

        Assert.Equal(score, cachedWRatio.Similarity(target), 10);
        Assert.Equal(0.0, cachedWRatio.Similarity(target, score + 0.0001));
    }

    [Fact]
    public void CachedTokenScorersPreserveStaticResultsWithCutoffsAndHints()
    {
        string source = "alpha beta beta gamma delta";
        string target = "gamma alpha beta epsilon delta";
        CachedTokenRatio tokenRatio = new(source);
        CachedPartialTokenRatio partialTokenRatio = new(source);
        CachedWRatio wRatio = new(source);

        double tokenScore = Fuzz.TokenRatio(source, target);
        double partialTokenScore = Fuzz.PartialTokenRatio(source, target);
        double weightedScore = Fuzz.WRatio(source, target);

        Assert.Equal(tokenScore, tokenRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(partialTokenScore, partialTokenRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(weightedScore, wRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(0.0, tokenRatio.Similarity(target, tokenScore + 0.0001, 70.0));
        Assert.Equal(0.0, partialTokenRatio.Similarity(target, partialTokenScore + 0.0001, 70.0));
        Assert.Equal(0.0, wRatio.Similarity(target, weightedScore + 0.0001, 70.0));
    }

    [Theory]
    [MemberData(nameof(TokenizationStressCases))]
    public void CachedTokenScorersMatchStaticAcrossTokenizationStressCases(string source, string target)
    {
        CachedTokenSortRatio tokenSortRatio = new(source);
        CachedPartialTokenSortRatio partialTokenSortRatio = new(source);
        CachedTokenSetRatio tokenSetRatio = new(source);
        CachedPartialTokenSetRatio partialTokenSetRatio = new(source);
        CachedTokenRatio tokenRatio = new(source);
        CachedPartialTokenRatio partialTokenRatio = new(source);
        CachedWRatio wRatio = new(source);
        CachedQRatio qRatio = new(source);

        double tokenSortScore = Fuzz.TokenSortRatio(source, target);
        double partialTokenSortScore = Fuzz.PartialTokenSortRatio(source, target);
        double tokenSetScore = Fuzz.TokenSetRatio(source, target);
        double partialTokenSetScore = Fuzz.PartialTokenSetRatio(source, target);
        double tokenScore = Fuzz.TokenRatio(source, target);
        double partialTokenScore = Fuzz.PartialTokenRatio(source, target);
        double weightedScore = Fuzz.WRatio(source, target);
        double qScore = Fuzz.QRatio(source, target);

        Assert.Equal(tokenSortScore, tokenSortRatio.Similarity(target), 10);
        Assert.Equal(partialTokenSortScore, partialTokenSortRatio.Similarity(target), 10);
        Assert.Equal(tokenSetScore, tokenSetRatio.Similarity(target), 10);
        Assert.Equal(partialTokenSetScore, partialTokenSetRatio.Similarity(target), 10);
        Assert.Equal(tokenScore, tokenRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(partialTokenScore, partialTokenRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(weightedScore, wRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(qScore, qRatio.Similarity(target, scoreHint: 70.0), 10);
        Assert.Equal(Fuzz.TokenSortRatio(source, target, 80.0), tokenSortRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.PartialTokenSortRatio(source, target, 80.0), partialTokenSortRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.TokenSetRatio(source, target, 80.0), tokenSetRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.PartialTokenSetRatio(source, target, 80.0), partialTokenSetRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.TokenRatio(source, target, 80.0), tokenRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.PartialTokenRatio(source, target, 80.0), partialTokenRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.WRatio(source, target, 80.0), wRatio.Similarity(target, 80.0, 70.0), 10);
        Assert.Equal(Fuzz.QRatio(source, target, 80.0), qRatio.Similarity(target, 80.0, 70.0), 10);
    }

    [Fact]
    public void UpstreamFuzzFixturesRemainStable()
    {
        Assert.Equal(100.0, Fuzz.Ratio("new york mets", "new york mets"));
        Assert.Equal(65.0, Fuzz.Ratio("new york mets", "the wonderful new york mets"));
        Assert.Equal(100.0, Fuzz.PartialRatio("new york mets", "the wonderful new york mets"));
        Assert.Equal(100.0, Fuzz.TokenSetRatio("new york mets vs atlanta braves", "atlanta braves vs new york mets"));
        Assert.Equal(50.0, Fuzz.TokenSetRatio("a{", "{b"));
        Assert.Equal(90.0, Fuzz.WRatio("new york mets", "the wonderful new york mets"));
        Assert.Equal(33.33333333333333, Fuzz.PartialRatio("001", "220222"), 10);
    }

    [Fact]
    public void UpstreamPartialRatioAlignmentIssue231Fixture()
    {
        string source = "er merkantilismus f/rderte handel und verkehr mit teils marktkonformen, teils dirigistischen ma_nahmen.";
        string target = "ils marktkonformen, teils dirigistischen ma_nahmen. an der schwelle zum 19. jahrhundert entstand ein neu";
        ScoreAlignment alignment = Fuzz.PartialRatioAlignment(source, target);

        Assert.Equal(66.23376623376623, alignment.Score, 10);
        Assert.Equal(0, alignment.SourceStart);
        Assert.Equal(103, alignment.SourceEnd);
        Assert.Equal(0, alignment.DestinationStart);
        Assert.Equal(51, alignment.DestinationEnd);
    }

    [Fact]
    public void FuzzScorersReturnZeroWhenCutoffIsAboveMaximum()
    {
        CachedRatio cachedRatio = new("alpha");
        CachedPartialRatio cachedPartialRatio = new("alpha");
        CachedPartialTokenSetRatio cachedPartialTokenSetRatio = new("alpha beta");

        Assert.Equal(0.0, Fuzz.Ratio("alpha", "alpha", 100.1));
        Assert.Equal(0.0, Fuzz.PartialRatio("alpha", "alpha", 100.1));
        Assert.Equal(0.0, Fuzz.TokenSortRatio("alpha beta", "beta alpha", 100.1));
        Assert.Equal(0.0, Fuzz.PartialTokenSetRatio("alpha beta", "alpha gamma", 100.1));
        Assert.Equal(0.0, cachedRatio.Similarity("alpha", 100.1));
        Assert.Equal(0.0, cachedPartialRatio.Similarity("alpha", 100.1));
        Assert.Equal(0.0, cachedPartialTokenSetRatio.Similarity("alpha gamma", 100.1));
    }

    [Fact]
    public void FuzzScorersRejectInvalidCutoffs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fuzz.Ratio("alpha", "alpha", -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fuzz.PartialRatio("alpha", "alpha", double.NaN));
    }

    [Fact]
    public void EmptyInputsMatchUpstreamFuzzBehavior()
    {
        Assert.Equal(100.0, Fuzz.Ratio("", ""));
        Assert.Equal(100.0, Fuzz.PartialRatio("", ""));
        Assert.Equal(100.0, Fuzz.TokenSortRatio("", ""));
        Assert.Equal(0.0, Fuzz.TokenSetRatio("", ""));
        Assert.Equal(100.0, Fuzz.PartialTokenSortRatio("", ""));
        Assert.Equal(0.0, Fuzz.PartialTokenSetRatio("", ""));
        Assert.Equal(100.0, Fuzz.TokenRatio("", ""));
        Assert.Equal(100.0, Fuzz.PartialTokenRatio("", ""));
        Assert.Equal(0.0, Fuzz.WRatio("", ""));
        Assert.Equal(0.0, Fuzz.QRatio("", ""));
    }

    [Fact]
    public void QRatioDoesNotPreprocessByDefault()
    {
        double score = Fuzz.QRatio("this is a word", "THIS IS A WORD");

        Assert.InRange(score, 20.0, 22.0);
    }

    [Fact]
    public void QRatioCanUseDefaultProcess()
    {
        double score = Fuzz.QRatio("this is a word", "THIS IS A WORD", processor: Utils.DefaultProcess);

        Assert.Equal(100.0, score);
    }

    [Fact]
    public void WRatioImprovesWithDefaultProcess()
    {
        double rawScore = Fuzz.WRatio("this is a word", "THIS IS A WORD");
        double processedScore = Fuzz.WRatio("this is a word", "THIS IS A WORD", processor: Utils.DefaultProcess);

        Assert.True(processedScore > rawScore);
        Assert.Equal(100.0, processedScore);
    }

    [Fact]
    public void CachedFuzzScorersMatchStaticScorers()
    {
        string source = "fuzzy wuzzy was a bear";
        string target = "wuzzy fuzzy was a bear";

        CachedRatio ratio = new(source);
        CachedPartialRatio partialRatio = new(source);
        CachedTokenSortRatio tokenSortRatio = new(source);
        CachedPartialTokenSortRatio partialTokenSortRatio = new(source);
        CachedTokenSetRatio tokenSetRatio = new(source);
        CachedPartialTokenSetRatio partialTokenSetRatio = new(source);
        CachedTokenRatio tokenRatio = new(source);
        CachedPartialTokenRatio partialTokenRatio = new(source);
        CachedQRatio qRatio = new(source);
        CachedWRatio wRatio = new(source);

        Assert.Equal(Fuzz.Ratio(source, target), ratio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialRatio(source, target), partialRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialRatioAlignment(source, target).Score, partialRatio.Alignment(target).Score, 10);
        Assert.Equal(Fuzz.TokenSortRatio(source, target), tokenSortRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialTokenSortRatio(source, target), partialTokenSortRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.TokenSetRatio(source, target), tokenSetRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialTokenSetRatio(source, target), partialTokenSetRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.TokenRatio(source, target), tokenRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.PartialTokenRatio(source, target), partialTokenRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.QRatio(source, target), qRatio.Similarity(target), 10);
        Assert.Equal(Fuzz.WRatio(source, target), wRatio.Similarity(target), 10);
    }

    [Fact]
    public void CachedFuzzScorersApplyScoreCutoff()
    {
        string source = "this is a test";
        string target = "this is a test!";

        CachedRatio ratio = new(source);
        CachedPartialRatio partialRatio = new(source);
        CachedWRatio wRatio = new(source);

        Assert.Equal(0.0, ratio.Similarity(target, 97.0));
        Assert.Equal(100.0, partialRatio.Similarity(target, 100.0));
        Assert.Equal(0.0, wRatio.Similarity("completely different", 99.0));
    }

    [Fact]
    public void CachedFuzzScorersPreserveResultsWithScoreHint()
    {
        string source = "fuzzy wuzzy was a bear";
        string target = "wuzzy fuzzy was a bear";
        string longSource = string.Concat(Enumerable.Repeat("abcdefghij", 8));
        string longTarget = string.Concat(Enumerable.Repeat("Zbcdefghij", 8));
        CachedRatio ratio = new(source);
        CachedPartialRatio partialRatio = new(source);
        CachedWRatio wRatio = new(source);
        CachedRatio longRatio = new(longSource);

        Assert.Equal(ratio.Similarity(target), ratio.Similarity(target, scoreHint: 50.0), 10);
        Assert.Equal(partialRatio.Similarity(target), partialRatio.Similarity(target, scoreHint: 50.0), 10);
        Assert.Equal(wRatio.Similarity(target), wRatio.Similarity(target, scoreHint: 50.0), 10);
        Assert.Equal(Fuzz.Ratio(longSource, longTarget), longRatio.Similarity(longTarget, scoreHint: 80.0), 10);
        Assert.Equal(0.0, longRatio.Similarity(longTarget, 100.1, 80.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => longRatio.Similarity(longTarget, scoreHint: -1.0));
    }

    [Fact]
    public void CachedRatioMatchesStaticRatioAtCutoffBoundary()
    {
        string source = "alpha beta beta gamma";
        string target = "alpha beta epsilon gamma";
        CachedRatio ratio = new(source);

        Assert.Equal(Fuzz.Ratio(source, target, 80.0), ratio.Similarity(target, 80.0), 10);
        Assert.Equal(Fuzz.Ratio(source, target, 80.0), ratio.Similarity(target, 80.0, 70.0), 10);
    }

    public static TheoryData<string, string, double, double, double, double, double, double, double, double> TokenParityCases => new()
    {
        { "new york mets vs atlanta braves", "atlanta braves vs new york mets", 100.0, 100.0, 100.0, 100.0, 100.0, 100.0, 95.0, 45.16129032258065 },
        { "alpha beta beta gamma", "beta gamma delta", 75.67567567567568, 90.32258064516128, 81.25, 100.0, 81.25, 100.0, 77.1875, 54.054054054054056 },
        { "alpha beta gamma", "gamma beta alpha alpha", 84.21052631578947, 100.0, 100.0, 100.0, 100.0, 100.0, 95.0, 52.63157894736843 },
        { "short phrase", "a much longer phrase with short phrase inside", 42.10526315789473, 100.0, 100.0, 100.0, 100.0, 100.0, 90.0, 42.10526315789473 },
        { "alpha beta gamma delta epsilon", "gamma alpha epsilon beta zeta", 81.35593220338984, 79.3103448275862, 94.91525423728814, 100.0, 94.91525423728814, 100.0, 90.16949152542372, 54.23728813559322 },
        { "abc abc def ghi", "ghi def abc xyz", 73.33333333333334, 84.61538461538461, 100.0, 100.0, 100.0, 100.0, 95.0, 33.333333333333336 },
        { "the quick brown fox jumps", "quick fox brown jumps over", 82.35294117647058, 80.0, 91.30434782608695, 100.0, 91.30434782608695, 100.0, 86.7391304347826, 66.66666666666667 }
    };

    public static TheoryData<string, string> TokenizationStressCases => new()
    {
        { "   alpha   beta\tbeta\r\ngamma   ", "gamma alpha   beta epsilon" },
        { "alpha alpha alpha beta beta gamma", "gamma beta alpha alpha delta" },
        { string.Join(' ', Enumerable.Range(0, 32).Select(index => "token" + index)), string.Join(' ', Enumerable.Range(16, 32).Select(index => "token" + index)) },
        { string.Concat(Enumerable.Repeat("alpha beta gamma delta ", 24)), string.Concat(Enumerable.Repeat("gamma beta epsilon delta ", 24)) },
        { "ma\u00f1ana cafe\u0301 resume\u0301 alpha\t\u03b2eta \u03b2eta", "cafe ma\u00f1ana beta resume\u0301 alpha" }
    };
}
