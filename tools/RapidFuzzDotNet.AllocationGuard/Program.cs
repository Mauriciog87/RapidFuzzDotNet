using System.Globalization;
using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;

namespace RapidFuzzDotNet.AllocationGuard;

public static class Program
{
    public static int Main(string[] args)
    {
        bool updateBaseline = args.Contains("--update-baseline", StringComparer.Ordinal);
        AllocationScenario[] scenarios = CreateScenarios();
        int failures = 0;

        for (int i = 0; i < scenarios.Length; i++)
        {
            AllocationScenario scenario = scenarios[i];
            scenario.BaselineScore();
            scenario.CandidateScore();
            long baselineAllocated = Measure(scenario.BaselineScore, scenario.Iterations);
            long candidateAllocated = Measure(scenario.CandidateScore, scenario.Iterations);
            Console.WriteLine(FormatResult(scenario, baselineAllocated, candidateAllocated));

            if (updateBaseline)
            {
                Console.WriteLine(FormatBaselineUpdate(scenario, baselineAllocated, candidateAllocated));
                continue;
            }

            if (baselineAllocated > scenario.BaselineBudget)
            {
                failures++;
                Console.Error.WriteLine($"{scenario.Name} {scenario.BaselineName} allocations exceeded budget {scenario.BaselineBudget.ToString(CultureInfo.InvariantCulture)}.");
            }

            if (candidateAllocated > scenario.CandidateBudget)
            {
                failures++;
                Console.Error.WriteLine($"{scenario.Name} {scenario.CandidateName} allocations exceeded budget {scenario.CandidateBudget.ToString(CultureInfo.InvariantCulture)}.");
            }

            if (scenario.CandidateMustNotExceedBaseline && candidateAllocated > baselineAllocated)
            {
                failures++;
                Console.Error.WriteLine($"{scenario.Name} {scenario.CandidateName} allocations exceeded {scenario.BaselineName} allocations.");
            }

            long relativeBudget = (long)Math.Floor(baselineAllocated * scenario.CandidateMaximumFraction);

            if (candidateAllocated > relativeBudget)
            {
                failures++;
                Console.Error.WriteLine($"{scenario.Name} {scenario.CandidateName} allocations exceeded relative budget {relativeBudget.ToString(CultureInfo.InvariantCulture)}.");
            }
        }

        return failures == 0 ? 0 : 1;
    }

    private static AllocationScenario[] CreateScenarios()
    {
        string denseWhitespaceSource = "   alpha   beta\tbeta\r\ngamma   ";
        string denseWhitespaceTarget = "gamma alpha   beta epsilon";
        string duplicateSource = "alpha alpha alpha beta beta gamma";
        string duplicateTarget = "gamma beta alpha alpha delta";
        string uniqueSource = string.Join(' ', Enumerable.Range(0, 32).Select(index => "token" + index));
        string uniqueTarget = string.Join(' ', Enumerable.Range(16, 32).Select(index => "token" + index));
        string longSource = string.Concat(Enumerable.Repeat("alpha beta gamma delta ", 24));
        string longTarget = string.Concat(Enumerable.Repeat("gamma beta epsilon delta ", 24));
        string shortSource = "alpha";
        string shortTarget = "alp";
        string partialSource = string.Concat(Enumerable.Repeat("alpha beta gamma ", 12));
        string partialTarget = "zero " + partialSource + " omega";
        string longDistanceSource = string.Concat(Enumerable.Repeat("abcdefghij", 32));
        string longDistanceTarget = string.Concat(Enumerable.Repeat("abcdxfghiy", 32));
        string[] multiDistanceSources =
        [
            string.Concat(Enumerable.Repeat("kitten", 24)),
            string.Concat(Enumerable.Repeat("sitting", 24)),
            string.Concat(Enumerable.Repeat("smitten", 24)),
            string.Concat(Enumerable.Repeat("written", 24))
        ];
        string multiDistanceTarget = string.Concat(Enumerable.Repeat("sitting", 24));
        string[] multiFuzzSources =
        [
            "fuzzy wuzzy was a bear fuzzy wuzzy had no hair",
            "new york mets versus new york yankees",
            "alpha beta gamma delta epsilon zeta eta theta",
            "token token duplicate duplicate alpha beta"
        ];
        string multiFuzzTarget = "wuzzy fuzzy was a bear with alpha beta duplicate tokens";
        CachedTokenRatio denseWhitespaceTokenRatio = new(denseWhitespaceSource);
        CachedPartialTokenRatio duplicatePartialTokenRatio = new(duplicateSource);
        CachedWRatio uniqueWRatio = new(uniqueSource);
        CachedTokenSortRatio longTokenSortRatio = new(longSource);
        CachedTokenSetRatio longTokenSetRatio = new(longSource);
        CachedRatio shortRatio = new(shortSource);
        CachedPartialRatio partialRatio = new(partialSource);
        CachedLevenshtein cachedLevenshtein = new(longDistanceSource);
        CachedIndel cachedIndel = new(longDistanceSource);
        CachedLcsSeq cachedLcsSeq = new(longDistanceSource);
        MultiLevenshtein multiLevenshtein = new(multiDistanceSources);
        MultiJaro multiJaro = new(multiFuzzSources);
        MultiWRatio multiWRatio = new(multiFuzzSources);
        int[] multiLevenshteinBuffer = new int[multiDistanceSources.Length];
        double[] multiJaroBuffer = new double[multiFuzzSources.Length];
        double[] multiWRatioBuffer = new double[multiFuzzSources.Length];
        string damerauSource = string.Concat(Enumerable.Range(0, 1024).Select(index => (char)('a' + (index % 23))));
        char[] damerauTargetCharacters = damerauSource.ToCharArray();

        for (int index = 8; index + 1 < damerauTargetCharacters.Length; index += 37)
        {
            (damerauTargetCharacters[index], damerauTargetCharacters[index + 1]) =
                (damerauTargetCharacters[index + 1], damerauTargetCharacters[index]);
        }

        string damerauTarget = new(damerauTargetCharacters);
        int[] genericSource = Enumerable.Range(0, 320).Select(index => index % 17).ToArray();
        int[] genericTarget = Enumerable.Range(0, 320).Select(index => (index + 1) % 17).ToArray();
        CachedLevenshtein<int> genericCachedLevenshtein = new(genericSource);
        int genericBatchCount = Math.Max(8, System.Numerics.Vector<ulong>.Count * 2);
        int[][] genericBatchSources = Enumerable.Range(0, genericBatchCount)
            .Select(offset => Enumerable.Range(0, 48).Select(index => (index + offset) % 13).ToArray())
            .ToArray();
        MultiLevenshtein<int> genericMultiLevenshtein = new(genericBatchSources);
        int[] genericMultiBuffer = new int[genericBatchSources.Length];
        CachedTokenRatio spanTokenRatio = new(denseWhitespaceSource.AsSpan());

        return
        [
            new AllocationScenario(
                "dense whitespace token ratio",
                "static",
                () => Fuzz.TokenRatio(denseWhitespaceSource, denseWhitespaceTarget),
                "cached",
                () => denseWhitespaceTokenRatio.Similarity(denseWhitespaceTarget),
                2500,
                1200,
                true),
            new AllocationScenario(
                "duplicate partial token ratio",
                "static",
                () => Fuzz.PartialTokenRatio(duplicateSource, duplicateTarget),
                "cached",
                () => duplicatePartialTokenRatio.Similarity(duplicateTarget),
                36000,
                35000,
                true),
            new AllocationScenario(
                "unique weighted ratio",
                "static",
                () => Fuzz.WRatio(uniqueSource, uniqueTarget),
                "cached",
                () => uniqueWRatio.Similarity(uniqueTarget),
                32000,
                12000,
                true),
            new AllocationScenario(
                "long token sort ratio",
                "static",
                () => Fuzz.TokenSortRatio(longSource, longTarget),
                "cached",
                () => longTokenSortRatio.Similarity(longTarget),
                25000,
                5000,
                true),
            new AllocationScenario(
                "long token set ratio",
                "static",
                () => Fuzz.TokenSetRatio(longSource, longTarget),
                "cached",
                () => longTokenSetRatio.Similarity(longTarget),
                7000,
                5000,
                true),
            new AllocationScenario(
                "short ratio",
                "static",
                () => Fuzz.Ratio(shortSource, shortTarget),
                "cached",
                () => shortRatio.Similarity(shortTarget),
                256,
                256,
                true),
            new AllocationScenario(
                "long partial ratio",
                "static",
                () => Fuzz.PartialRatio(partialSource, partialTarget),
                "cached",
                () => partialRatio.Similarity(partialTarget),
                18000,
                18000,
                true),
            new AllocationScenario(
                "long levenshtein distance",
                "static",
                () => Levenshtein.Distance(longDistanceSource, longDistanceTarget, 128, 128),
                "cached",
                () => cachedLevenshtein.Distance(longDistanceTarget, 128, 128),
                12000,
                512,
                true),
            new AllocationScenario(
                "long indel distance",
                "static",
                () => Indel.Distance(longDistanceSource, longDistanceTarget, 256, 256),
                "cached",
                () => cachedIndel.Distance(longDistanceTarget, 256, 256),
                12000,
                512,
                true),
            new AllocationScenario(
                "long lcs similarity",
                "static",
                () => LcsSeq.Similarity(longDistanceSource, longDistanceTarget, 192, 192),
                "cached",
                () => cachedLcsSeq.Similarity(longDistanceTarget, 192, 192),
                12000,
                512,
                true),
            new AllocationScenario(
                "multi levenshtein distances",
                "static batch",
                () => SumLevenshteinDistances(multiDistanceSources, multiDistanceTarget),
                "multi span",
                () =>
                {
                    multiLevenshtein.Distances(multiDistanceTarget, multiLevenshteinBuffer, 64);
                    return Sum(multiLevenshteinBuffer);
                },
                22000,
                512,
                true),
            new AllocationScenario(
                "multi jaro similarities",
                "static batch",
                () => SumJaroSimilarities(multiFuzzSources, multiFuzzTarget),
                "multi span",
                () =>
                {
                    multiJaro.Similarities(multiFuzzTarget, multiJaroBuffer, 0.75);
                    return Sum(multiJaroBuffer);
                },
                6000,
                512,
                true),
            new AllocationScenario(
                "multi weighted ratio",
                "static batch",
                () => SumWRatios(multiFuzzSources, multiFuzzTarget),
                "multi span",
                () =>
                {
                    multiWRatio.Similarities(multiFuzzTarget, multiWRatioBuffer, 60.0);
                    return Sum(multiWRatioBuffer);
                },
                25000,
                15000,
                true),
            new AllocationScenario(
                "linear damerau 1024",
                "matrix reference",
                () => DamerauMatrixDistance(damerauSource, damerauTarget),
                "linear pooled",
                () => DamerauLevenshtein.Distance(damerauSource, damerauTarget),
                5000000,
                500000,
                true,
                4,
                0.1),
            new AllocationScenario(
                "generic cached levenshtein",
                "static",
                () => Levenshtein.Distance<int>(genericSource, genericTarget),
                "cached",
                () => genericCachedLevenshtein.Distance(genericTarget),
                1024,
                1024,
                true),
            new AllocationScenario(
                "generic multi simd",
                "static batch",
                () => SumGenericLevenshteinDistances(genericBatchSources, genericTarget),
                "multi span",
                () =>
                {
                    genericMultiLevenshtein.Distances(genericTarget, genericMultiBuffer);
                    return Sum(genericMultiBuffer);
                },
                4096,
                1024,
                true),
            new AllocationScenario(
                "token span cached",
                "static span",
                () => Fuzz.TokenRatio(denseWhitespaceSource.AsSpan(), denseWhitespaceTarget.AsSpan()),
                "cached span",
                () => spanTokenRatio.Similarity(denseWhitespaceTarget.AsSpan()),
                2500,
                1200,
                true),
            new AllocationScenario(
                "generic multi construction",
                "cached array",
                () => ConstructCachedGeneric(genericBatchSources),
                "multi construction",
                () => new MultiLevenshtein<int>(genericBatchSources).Count,
                20000,
                20000,
                false)
        ];
    }

    private static long Measure(Func<double> scorer, int iterations)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        double total = 0.0;

        for (int i = 0; i < iterations; i++)
        {
            total += scorer();
        }

        if (double.IsNaN(total))
        {
            throw new InvalidOperationException("Invalid scorer result.");
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        return (after - before) / iterations;
    }

    private static string FormatResult(AllocationScenario scenario, long baselineAllocated, long candidateAllocated)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{scenario.Name}: {scenario.BaselineName}={baselineAllocated}B {scenario.CandidateName}={candidateAllocated}B budgets={scenario.BaselineBudget}B/{scenario.CandidateBudget}B");
    }

    private static string FormatBaselineUpdate(AllocationScenario scenario, long baselineAllocated, long candidateAllocated)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"baseline {scenario.Name}: {scenario.BaselineName}={baselineAllocated} {scenario.CandidateName}={candidateAllocated}");
    }

    private static double SumLevenshteinDistances(string[] sources, string target)
    {
        int total = 0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += Levenshtein.Distance(sources[index], target, 64);
        }

        return total;
    }

    private static double SumGenericLevenshteinDistances(int[][] sources, int[] target)
    {
        int total = 0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += Levenshtein.Distance<int>(sources[index], target);
        }

        return total;
    }

    private static double ConstructCachedGeneric(int[][] sources)
    {
        int count = 0;

        for (int index = 0; index < sources.Length; index++)
        {
            CachedLevenshtein<int> scorer = new(sources[index]);
            count += scorer.Distance(ReadOnlySpan<int>.Empty);
        }

        return count;
    }

    private static int DamerauMatrixDistance(string first, string second)
    {
        int maximumDistance = first.Length + second.Length;
        int[,] matrix = new int[first.Length + 2, second.Length + 2];
        Dictionary<char, int> lastRows = [];
        matrix[0, 0] = maximumDistance;

        for (int row = 0; row <= first.Length; row++)
        {
            matrix[row + 1, 0] = maximumDistance;
            matrix[row + 1, 1] = row;
        }

        for (int column = 0; column <= second.Length; column++)
        {
            matrix[0, column + 1] = maximumDistance;
            matrix[1, column + 1] = column;
        }

        for (int row = 1; row <= first.Length; row++)
        {
            int lastMatchColumn = 0;

            for (int column = 1; column <= second.Length; column++)
            {
                int lastMatchingRow = lastRows.GetValueOrDefault(second[column - 1]);
                int previousMatchColumn = lastMatchColumn;
                int substitutionCost = 1;

                if (first[row - 1] == second[column - 1])
                {
                    substitutionCost = 0;
                    lastMatchColumn = column;
                }

                int substitution = matrix[row, column] + substitutionCost;
                int insertion = matrix[row + 1, column] + 1;
                int deletion = matrix[row, column + 1] + 1;
                int transposition = matrix[lastMatchingRow, previousMatchColumn]
                    + row - lastMatchingRow
                    + column - previousMatchColumn
                    - 1;
                matrix[row + 1, column + 1] = Math.Min(Math.Min(substitution, insertion), Math.Min(deletion, transposition));
            }

            lastRows[first[row - 1]] = row;
        }

        return matrix[first.Length + 1, second.Length + 1];
    }

    private static double SumJaroSimilarities(string[] sources, string target)
    {
        double total = 0.0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += Jaro.Similarity(sources[index], target, 0.75);
        }

        return total;
    }

    private static double SumWRatios(string[] sources, string target)
    {
        double total = 0.0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += Fuzz.WRatio(sources[index], target, 60.0);
        }

        return total;
    }

    private static double Sum(int[] values)
    {
        int total = 0;

        for (int index = 0; index < values.Length; index++)
        {
            total += values[index];
        }

        return total;
    }

    private static double Sum(double[] values)
    {
        double total = 0.0;

        for (int index = 0; index < values.Length; index++)
        {
            total += values[index];
        }

        return total;
    }
}

internal sealed class AllocationScenario
{
    public AllocationScenario(
        string name,
        string baselineName,
        Func<double> baselineScore,
        string candidateName,
        Func<double> candidateScore,
        long baselineBudget,
        long candidateBudget,
        bool candidateMustNotExceedBaseline,
        int iterations = 512,
        double candidateMaximumFraction = 1.0)
    {
        Name = name;
        BaselineName = baselineName;
        BaselineScore = baselineScore;
        CandidateName = candidateName;
        CandidateScore = candidateScore;
        BaselineBudget = baselineBudget;
        CandidateBudget = candidateBudget;
        CandidateMustNotExceedBaseline = candidateMustNotExceedBaseline;
        Iterations = iterations;
        CandidateMaximumFraction = candidateMaximumFraction;
    }

    public string Name { get; }

    public string BaselineName { get; }

    public Func<double> BaselineScore { get; }

    public string CandidateName { get; }

    public Func<double> CandidateScore { get; }

    public long BaselineBudget { get; }

    public long CandidateBudget { get; }

    public bool CandidateMustNotExceedBaseline { get; }

    public int Iterations { get; }

    public double CandidateMaximumFraction { get; }
}
