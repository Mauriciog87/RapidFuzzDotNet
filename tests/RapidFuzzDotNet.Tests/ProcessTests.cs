using RapidFuzz;

namespace RapidFuzzDotNet.Tests;

public sealed class ProcessTests
{
    [Fact]
    public void ExtractOneReturnsBestChoice()
    {
        string[] choices = ["new york mets", "new york yankees", "atlanta braves"];

        ExtractedResult<string>? result = Process.ExtractOne("new york mets", choices);

        Assert.NotNull(result);
        Assert.Equal("new york mets", result.Choice);
        Assert.Equal(0, result.Index);
    }

    [Fact]
    public void ExtractSortsByScoreThenIndex()
    {
        string[] choices = ["alpha", "alpha", "alp"];

        List<ExtractedResult<string>> results = Process.Extract("alpha", choices, scorer: Fuzz.Ratio, limit: null);

        Assert.Collection(
            results,
            first => Assert.Equal(0, first.Index),
            second => Assert.Equal(1, second.Index),
            third => Assert.Equal(2, third.Index));
    }

    [Fact]
    public void ExtractAppliesLimit()
    {
        string[] choices = ["alpha", "alp", "beta"];

        List<ExtractedResult<string>> results = Process.Extract("alpha", choices, scorer: Fuzz.Ratio, limit: 2);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ExtractAppliesProcessor()
    {
        string[] choices = ["THIS IS A WORD"];

        ExtractedResult<string>? result = Process.ExtractOne("this is a word", choices, scorer: Fuzz.QRatio, processor: Utils.DefaultProcess);

        Assert.NotNull(result);
        Assert.Equal(100.0, result.Score);
    }

    [Fact]
    public void ExtractOneReturnsNullWhenNoChoicePassesCutoff()
    {
        string[] choices = ["beta"];

        ExtractedResult<string>? result = Process.ExtractOne("alpha", choices, scorer: Fuzz.Ratio, scoreCutoff: 90.0);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDefaultScorerMatchesDirectWRatio()
    {
        string query = "new york mets";
        string[] choices = ["new york mets", "new york yankees", "atlanta braves"];

        List<ExtractedResult<string>> results = Process.Extract(query, choices, limit: null);

        foreach (ExtractedResult<string> result in results)
        {
            Assert.Equal(Fuzz.WRatio(query, result.Choice), result.Score, 10);
        }
    }

    [Fact]
    public void ExtractCustomScorerUsesProvidedDelegate()
    {
        int calls = 0;
        string[] choices = ["alpha", "alp", "beta"];
        Scorer scorer = (query, choice, scoreCutoff) =>
        {
            calls++;
            return Fuzz.Ratio(query, choice, scoreCutoff);
        };

        List<ExtractedResult<string>> results = Process.Extract("alpha", choices, scorer: scorer, limit: null);

        Assert.Equal(choices.Length, calls);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ExtractOneCanSelectNonStringChoices()
    {
        SearchChoice[] choices =
        [
            new SearchChoice("new york mets", 1962),
            new SearchChoice("new york yankees", 1901),
            new SearchChoice("atlanta braves", 1871)
        ];

        ExtractedResult<SearchChoice>? result = Process.ExtractOne(
            "NEW YORK METS",
            choices,
            static choice => choice.Name,
            scorer: Fuzz.QRatio,
            processor: Utils.DefaultProcess);

        Assert.NotNull(result);
        Assert.Equal("new york mets", result.Choice.Name);
        Assert.Equal(1962, result.Choice.Founded);
        Assert.Equal(0, result.Index);
        Assert.Equal(100.0, result.Score);
    }

    [Fact]
    public void ExtractGenericSortsAndLimitsChoices()
    {
        SearchChoice[] choices =
        [
            new SearchChoice("alpha", 1),
            new SearchChoice("alp", 2),
            new SearchChoice("beta", 3)
        ];

        List<ExtractedResult<SearchChoice>> results = Process.Extract(
            "alpha",
            choices,
            static choice => choice.Name,
            scorer: Fuzz.Ratio,
            limit: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Choice.Founded);
        Assert.Equal(2, results[1].Choice.Founded);
    }

    [Fact]
    public void ExtractIterYieldsInInputOrderAndIsLazy()
    {
        int enumerated = 0;
        IEnumerable<string> choices = EnumerateChoices(() => enumerated++);

        IEnumerable<ExtractedResult<string>> results = Process.ExtractIter("alpha", choices, scorer: Fuzz.Ratio);

        Assert.Equal(0, enumerated);

        List<ExtractedResult<string>> materialized = results.ToList();

        Assert.Equal(2, enumerated);
        Assert.Equal("beta", materialized[0].Choice);
        Assert.Equal("alpha", materialized[1].Choice);
    }

    [Fact]
    public void CdistReturnsScoreMatrix()
    {
        string[] queries = ["alpha", "beta"];
        string[] choices = ["alpha", "alp"];

        double[,] scores = Process.Cdist(queries, choices, scorer: Fuzz.Ratio);

        Assert.Equal(2, scores.GetLength(0));
        Assert.Equal(2, scores.GetLength(1));
        Assert.Equal(100.0, scores[0, 0]);
        Assert.Equal(Fuzz.Ratio("beta", "alp"), scores[1, 1], 10);
    }

    [Fact]
    public void CpdistReturnsPairwiseScores()
    {
        string[] queries = ["alpha", "beta"];
        string[] choices = ["alpha", "alp"];

        double[] scores = Process.Cpdist(queries, choices, scorer: Fuzz.Ratio);

        Assert.Equal(2, scores.Length);
        Assert.Equal(100.0, scores[0]);
        Assert.Equal(Fuzz.Ratio("beta", "alp"), scores[1], 10);
        Assert.Throws<ArgumentException>(() => Process.Cpdist(["alpha"], ["alpha", "beta"]));
    }

    [Fact]
    public void CdistGenericUsesSelectorsAndProcessor()
    {
        SearchChoice[] queries = [new SearchChoice("NEW YORK METS", 1962)];
        SearchChoice[] choices =
        [
            new SearchChoice("new york mets", 1962),
            new SearchChoice("atlanta braves", 1871)
        ];

        double[,] scores = Process.Cdist(
            queries,
            choices,
            static choice => choice.Name,
            static choice => choice.Name,
            scorer: Fuzz.QRatio,
            processor: Utils.DefaultProcess);

        Assert.Equal(1, scores.GetLength(0));
        Assert.Equal(2, scores.GetLength(1));
        Assert.Equal(100.0, scores[0, 0]);
        Assert.True(scores[0, 1] < 100.0);
    }

    [Fact]
    public void ProcessMatchesPythonRapidFuzzObservedExamples()
    {
        string[] choices = ["new york mets", "new york yankees", "atlanta braves"];
        string[] ratioChoices = ["alpha", "alpha", "alp"];
        string[] iterChoices = ["beta", "alpha"];

        ExtractedResult<string>? one = Process.ExtractOne("new york mets", choices);
        List<ExtractedResult<string>> extracted = Process.Extract("alpha", ratioChoices, scorer: Fuzz.Ratio, limit: null);
        List<ExtractedResult<string>> iterated = Process.ExtractIter("alpha", iterChoices, scorer: Fuzz.Ratio).ToList();
        List<ExtractedResult<string>> cutoff = Process.Extract("alpha", ["beta"], scorer: Fuzz.Ratio, scoreCutoff: 90.0);
        double[,] cdist = Process.Cdist(["alpha", "beta"], ["alpha", "alp"], scorer: Fuzz.Ratio);
        double[] cpdist = Process.Cpdist(["alpha", "beta"], ["alpha", "alp"], scorer: Fuzz.Ratio);
        List<ExtractedResult<string>> processed = Process.Extract(
            "this is a word",
            ["THIS IS A WORD"],
            scorer: Fuzz.QRatio,
            processor: Utils.DefaultProcess);

        Assert.NotNull(one);
        Assert.Equal("new york mets", one.Choice);
        Assert.Equal(100.0, one.Score);
        Assert.Equal(0, one.Index);
        Assert.Collection(
            extracted,
            first =>
            {
                Assert.Equal("alpha", first.Choice);
                Assert.Equal(100.0, first.Score);
                Assert.Equal(0, first.Index);
            },
            second =>
            {
                Assert.Equal("alpha", second.Choice);
                Assert.Equal(100.0, second.Score);
                Assert.Equal(1, second.Index);
            },
            third =>
            {
                Assert.Equal("alp", third.Choice);
                Assert.Equal(75.0, third.Score);
                Assert.Equal(2, third.Index);
            });
        Assert.Collection(
            iterated,
            first =>
            {
                Assert.Equal("beta", first.Choice);
                Assert.Equal(22.22222222222222, first.Score, 10);
                Assert.Equal(0, first.Index);
            },
            second =>
            {
                Assert.Equal("alpha", second.Choice);
                Assert.Equal(100.0, second.Score);
                Assert.Equal(1, second.Index);
            });
        Assert.Empty(cutoff);
        Assert.Equal(100.0, cdist[0, 0]);
        Assert.Equal(75.0, cdist[0, 1]);
        Assert.Equal(22.22222222222222, cdist[1, 0], 10);
        Assert.Equal(28.57142857142857, cdist[1, 1], 10);
        Assert.Equal(100.0, cpdist[0]);
        Assert.Equal(28.57142857142857, cpdist[1], 10);
        Assert.Collection(
            processed,
            result =>
            {
                Assert.Equal("THIS IS A WORD", result.Choice);
                Assert.Equal(100.0, result.Score);
                Assert.Equal(0, result.Index);
            });
    }

    [Fact]
    public void ProcessKnownScorersUseCachedPathWithoutChangingScores()
    {
        string query = "fuzzy wuzzy was a bear";
        string[] choices =
        [
            "wuzzy fuzzy was a bear",
            "new york mets",
            "fuzzy was a bear"
        ];
        Scorer[] scorers =
        [
            Fuzz.Ratio,
            Fuzz.PartialRatio,
            Fuzz.TokenSortRatio,
            Fuzz.PartialTokenSortRatio,
            Fuzz.TokenSetRatio,
            Fuzz.PartialTokenSetRatio,
            Fuzz.TokenRatio,
            Fuzz.PartialTokenRatio,
            Fuzz.QRatio,
            Fuzz.WRatio
        ];

        for (int scorerIndex = 0; scorerIndex < scorers.Length; scorerIndex++)
        {
            Scorer scorer = scorers[scorerIndex];
            List<ExtractedResult<string>> results = Process.Extract(query, choices, scorer: scorer, limit: null, scoreCutoff: 60.0);

            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                ExtractedResult<string> result = results[resultIndex];
                Assert.Equal(scorer(query, result.Choice, 60.0), result.Score, 10);
            }
        }
    }

    [Fact]
    public void ProcessGenericMatricesApplyCutoffProcessorAndSelectors()
    {
        SearchChoice[] queries =
        [
            new SearchChoice("THIS IS A WORD", 1),
            new SearchChoice("OTHER VALUE", 2)
        ];
        SearchChoice[] choices =
        [
            new SearchChoice("this is a word", 3),
            new SearchChoice("different", 4)
        ];

        double[,] cdist = Process.Cdist(
            queries,
            choices,
            static choice => choice.Name,
            static choice => choice.Name,
            scorer: Fuzz.QRatio,
            processor: Utils.DefaultProcess,
            scoreCutoff: 90.0);
        double[] cpdist = Process.Cpdist(
            queries,
            choices,
            static choice => choice.Name,
            static choice => choice.Name,
            scorer: Fuzz.QRatio,
            processor: Utils.DefaultProcess,
            scoreCutoff: 90.0);

        Assert.Equal(100.0, cdist[0, 0]);
        Assert.Equal(0.0, cdist[0, 1]);
        Assert.Equal(0.0, cdist[1, 0]);
        Assert.Equal(0.0, cdist[1, 1]);
        Assert.Equal(100.0, cpdist[0]);
        Assert.Equal(0.0, cpdist[1]);
    }

    [Fact]
    public void ProcessRejectsInvalidLimitsAndNullSelectedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Process.Extract("alpha", ["alpha"], limit: -1));
        Assert.Throws<ArgumentException>(() => Process.Extract("alpha", [new SearchChoice(null!, 1)], static choice => choice.Name));
        Assert.Throws<ArgumentException>(() => Process.Cdist([new SearchChoice(null!, 1)], ["alpha"], static choice => choice.Name, static choice => choice));
        Assert.Throws<ArgumentException>(() => Process.Cpdist(["alpha"], [new SearchChoice(null!, 1)], static choice => choice, static choice => choice.Name));
    }

    [Fact]
    public void ExtractOneResolvesTiesByInputOrder()
    {
        string[] choices = ["alpha", "alpha", "alp"];

        ExtractedResult<string>? result = Process.ExtractOne("alpha", choices, scorer: Fuzz.Ratio);

        Assert.NotNull(result);
        Assert.Equal(0, result.Index);
        Assert.Equal(100.0, result.Score);
    }

    [Fact]
    public void ProcessMatricesSupportEmptyInputsAndRejectPairLengthMismatch()
    {
        double[,] noQueries = Process.Cdist([], ["alpha"]);
        double[,] noChoices = Process.Cdist(["alpha"], []);
        double[] noPairs = Process.Cpdist([], []);

        Assert.Equal(0, noQueries.GetLength(0));
        Assert.Equal(1, noQueries.GetLength(1));
        Assert.Equal(1, noChoices.GetLength(0));
        Assert.Equal(0, noChoices.GetLength(1));
        Assert.Empty(noPairs);
        Assert.Throws<ArgumentException>(() => Process.Cpdist(["alpha"], []));
    }

    private static IEnumerable<string> EnumerateChoices(Action onEnumerated)
    {
        onEnumerated();
        yield return "beta";
        onEnumerated();
        yield return "alpha";
    }

    private sealed record SearchChoice(string Name, int Founded);
}
