using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;
using RapidFuzz;
using RapidFuzz.Distance;

namespace RapidFuzzDotNet.Benchmarks;

public sealed class CrossLanguageBenchmarkCase
{
    public CrossLanguageBenchmarkCase(
        string caseId,
        string category,
        string algorithm,
        string mode,
        string source,
        string target,
        double scoreCutoff)
    {
        CaseId = caseId;
        Category = category;
        Algorithm = algorithm;
        Mode = mode;
        Source = source;
        Target = target;
        ScoreCutoff = scoreCutoff;
    }

    public string CaseId { get; }

    public string Category { get; }

    public string Algorithm { get; }

    public string Mode { get; }

    public string Source { get; }

    public string Target { get; }

    public double ScoreCutoff { get; }

    public override string ToString()
    {
        return CaseId;
    }
}

public sealed class CrossLanguageInputState
{
    public CrossLanguageInputState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        Source = benchmarkCase.Source;
        Target = benchmarkCase.Target;
        ScoreCutoff = benchmarkCase.ScoreCutoff;
    }

    public string Source { get; }

    public string Target { get; }

    public double ScoreCutoff { get; }
}

public sealed class CrossLanguageCachedState<TCached>
{
    public CrossLanguageCachedState(TCached scorer, string target)
    {
        Scorer = scorer;
        Target = target;
    }

    public TCached Scorer { get; }

    public string Target { get; }
}

public interface ICrossLanguageBenchmarkScorer<TState>
{
    static abstract string Category { get; }

    static abstract string Algorithm { get; }

    static abstract string Mode { get; }

    static abstract TState CreateState(CrossLanguageBenchmarkCase benchmarkCase);

    static abstract double Measure(TState state);
}

public interface ICrossLanguageMode
{
    static abstract string Name { get; }

    static abstract bool UsesCutoff { get; }
}

public readonly struct CrossLanguageStaticMode : ICrossLanguageMode
{
    public static string Name => "static";

    public static bool UsesCutoff => false;
}

public readonly struct CrossLanguageCutoffMode : ICrossLanguageMode
{
    public static string Name => "cutoff";

    public static bool UsesCutoff => true;
}

public interface ICrossLanguageCoreAlgorithm
{
    static abstract string Name { get; }

    static abstract double Measure(CrossLanguageInputState state, bool usesCutoff);
}

public interface ICrossLanguageFuzzAlgorithm
{
    static abstract string Name { get; }

    static abstract double Measure(CrossLanguageInputState state, bool usesCutoff);
}

[SuppressMessage("Design", "CA1000", Justification = "Static interface dispatch excludes dynamic dispatch from benchmark measurements.")]
public readonly struct CrossLanguageCoreScorer<TAlgorithm, TMode>
    : ICrossLanguageBenchmarkScorer<CrossLanguageInputState>
    where TAlgorithm : struct, ICrossLanguageCoreAlgorithm
    where TMode : struct, ICrossLanguageMode
{
    public static string Category => "core";

    public static string Algorithm => TAlgorithm.Name;

    public static string Mode => TMode.Name;

    public static CrossLanguageInputState CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageInputState(benchmarkCase);
    }

    public static double Measure(CrossLanguageInputState state)
    {
        return TAlgorithm.Measure(state, TMode.UsesCutoff);
    }
}

[SuppressMessage("Design", "CA1000", Justification = "Static interface dispatch excludes dynamic dispatch from benchmark measurements.")]
public readonly struct CrossLanguageFuzzScorer<TAlgorithm, TMode>
    : ICrossLanguageBenchmarkScorer<CrossLanguageInputState>
    where TAlgorithm : struct, ICrossLanguageFuzzAlgorithm
    where TMode : struct, ICrossLanguageMode
{
    public static string Category => "fuzz";

    public static string Algorithm => TAlgorithm.Name;

    public static string Mode => TMode.Name;

    public static CrossLanguageInputState CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageInputState(benchmarkCase);
    }

    public static double Measure(CrossLanguageInputState state)
    {
        return TAlgorithm.Measure(state, TMode.UsesCutoff);
    }
}

public readonly struct CrossLanguageLevenshteinAlgorithm : ICrossLanguageCoreAlgorithm
{
    public static string Name => "levenshtein";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Levenshtein.Distance(state.Source, state.Target, (int)state.ScoreCutoff)
            : Levenshtein.Distance(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageIndelAlgorithm : ICrossLanguageCoreAlgorithm
{
    public static string Name => "indel";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Indel.Distance(state.Source, state.Target, (int)state.ScoreCutoff)
            : Indel.Distance(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageLcsSeqAlgorithm : ICrossLanguageCoreAlgorithm
{
    public static string Name => "lcs_seq";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? LcsSeq.Similarity(state.Source, state.Target, (int)state.ScoreCutoff)
            : LcsSeq.Similarity(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageJaroAlgorithm : ICrossLanguageCoreAlgorithm
{
    public static string Name => "jaro";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Jaro.Similarity(state.Source, state.Target, state.ScoreCutoff)
            : Jaro.Similarity(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageJaroWinklerAlgorithm : ICrossLanguageCoreAlgorithm
{
    public static string Name => "jaro_winkler";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? JaroWinkler.Similarity(state.Source, state.Target, 0.1, state.ScoreCutoff)
            : JaroWinkler.Similarity(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageRatioCoreAlgorithm : ICrossLanguageCoreAlgorithm
{
    public static string Name => "ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.Ratio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.Ratio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageRatioFuzzAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.Ratio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.Ratio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguagePartialRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "partial_ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.PartialRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.PartialRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageTokenSortRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "token_sort_ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.TokenSortRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.TokenSortRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageTokenSetRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "token_set_ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.TokenSetRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.TokenSetRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguagePartialTokenSortRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "partial_token_sort_ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.PartialTokenSortRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.PartialTokenSortRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguagePartialTokenSetRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "partial_token_set_ratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.PartialTokenSetRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.PartialTokenSetRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageQRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "qratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.QRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.QRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageWRatioAlgorithm : ICrossLanguageFuzzAlgorithm
{
    public static string Name => "wratio";

    public static double Measure(CrossLanguageInputState state, bool usesCutoff)
    {
        return usesCutoff
            ? Fuzz.WRatio(state.Source, state.Target, state.ScoreCutoff)
            : Fuzz.WRatio(state.Source, state.Target);
    }
}

public readonly struct CrossLanguageCachedLevenshteinScorer
    : ICrossLanguageBenchmarkScorer<CrossLanguageCachedState<CachedLevenshtein>>
{
    public static string Category => "core";

    public static string Algorithm => "levenshtein";

    public static string Mode => "cached";

    public static CrossLanguageCachedState<CachedLevenshtein> CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageCachedState<CachedLevenshtein>(new CachedLevenshtein(benchmarkCase.Source), benchmarkCase.Target);
    }

    public static double Measure(CrossLanguageCachedState<CachedLevenshtein> state)
    {
        return state.Scorer.Distance(state.Target);
    }
}

public readonly struct CrossLanguageCachedIndelScorer
    : ICrossLanguageBenchmarkScorer<CrossLanguageCachedState<CachedIndel>>
{
    public static string Category => "core";

    public static string Algorithm => "indel";

    public static string Mode => "cached";

    public static CrossLanguageCachedState<CachedIndel> CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageCachedState<CachedIndel>(new CachedIndel(benchmarkCase.Source), benchmarkCase.Target);
    }

    public static double Measure(CrossLanguageCachedState<CachedIndel> state)
    {
        return state.Scorer.Distance(state.Target);
    }
}

public readonly struct CrossLanguageCachedLcsSeqScorer
    : ICrossLanguageBenchmarkScorer<CrossLanguageCachedState<CachedLcsSeq>>
{
    public static string Category => "core";

    public static string Algorithm => "lcs_seq";

    public static string Mode => "cached";

    public static CrossLanguageCachedState<CachedLcsSeq> CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageCachedState<CachedLcsSeq>(new CachedLcsSeq(benchmarkCase.Source), benchmarkCase.Target);
    }

    public static double Measure(CrossLanguageCachedState<CachedLcsSeq> state)
    {
        return state.Scorer.Similarity(state.Target);
    }
}

public readonly struct CrossLanguageCachedJaroScorer
    : ICrossLanguageBenchmarkScorer<CrossLanguageCachedState<CachedJaro>>
{
    public static string Category => "core";

    public static string Algorithm => "jaro";

    public static string Mode => "cached";

    public static CrossLanguageCachedState<CachedJaro> CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageCachedState<CachedJaro>(new CachedJaro(benchmarkCase.Source), benchmarkCase.Target);
    }

    public static double Measure(CrossLanguageCachedState<CachedJaro> state)
    {
        return state.Scorer.Similarity(state.Target);
    }
}

public readonly struct CrossLanguageCachedJaroWinklerScorer
    : ICrossLanguageBenchmarkScorer<CrossLanguageCachedState<CachedJaroWinkler>>
{
    public static string Category => "core";

    public static string Algorithm => "jaro_winkler";

    public static string Mode => "cached";

    public static CrossLanguageCachedState<CachedJaroWinkler> CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageCachedState<CachedJaroWinkler>(new CachedJaroWinkler(benchmarkCase.Source), benchmarkCase.Target);
    }

    public static double Measure(CrossLanguageCachedState<CachedJaroWinkler> state)
    {
        return state.Scorer.Similarity(state.Target);
    }
}

public readonly struct CrossLanguageCachedRatioScorer
    : ICrossLanguageBenchmarkScorer<CrossLanguageCachedState<CachedRatio>>
{
    public static string Category => "core";

    public static string Algorithm => "ratio";

    public static string Mode => "cached";

    public static CrossLanguageCachedState<CachedRatio> CreateState(CrossLanguageBenchmarkCase benchmarkCase)
    {
        return new CrossLanguageCachedState<CachedRatio>(new CachedRatio(benchmarkCase.Source), benchmarkCase.Target);
    }

    public static double Measure(CrossLanguageCachedState<CachedRatio> state)
    {
        return state.Scorer.Similarity(state.Target);
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public abstract class CrossLanguageIndividualBenchmark<TScorer, TState>
    where TScorer : struct, ICrossLanguageBenchmarkScorer<TState>
{
    private TState state = default!;

    [ParamsSource(nameof(Cases))]
    public CrossLanguageBenchmarkCase Case { get; set; } = null!;

    public IEnumerable<CrossLanguageBenchmarkCase> Cases =>
        CrossLanguageCorpus.GetCases(TScorer.Category, TScorer.Algorithm, TScorer.Mode, "dotnet");

    [GlobalSetup]
    public void Setup()
    {
        state = TScorer.CreateState(Case);
    }

    [Benchmark]
    public double Execute()
    {
        return TScorer.Measure(state);
    }
}

public class CrossLanguageLevenshteinStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageLevenshteinAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageLevenshteinCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageLevenshteinAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageLevenshteinCachedBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCachedLevenshteinScorer, CrossLanguageCachedState<CachedLevenshtein>> { }
public class CrossLanguageIndelStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageIndelAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageIndelCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageIndelAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageIndelCachedBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCachedIndelScorer, CrossLanguageCachedState<CachedIndel>> { }
public class CrossLanguageLcsSeqStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageLcsSeqAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageLcsSeqCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageLcsSeqAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageLcsSeqCachedBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCachedLcsSeqScorer, CrossLanguageCachedState<CachedLcsSeq>> { }
public class CrossLanguageJaroStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageJaroAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageJaroCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageJaroAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageJaroCachedBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCachedJaroScorer, CrossLanguageCachedState<CachedJaro>> { }
public class CrossLanguageJaroWinklerStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageJaroWinklerAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageJaroWinklerCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageJaroWinklerAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageJaroWinklerCachedBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCachedJaroWinklerScorer, CrossLanguageCachedState<CachedJaroWinkler>> { }
public class CrossLanguageRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageRatioCoreAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCoreScorer<CrossLanguageRatioCoreAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageRatioCachedBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageCachedRatioScorer, CrossLanguageCachedState<CachedRatio>> { }
public class CrossLanguageFuzzRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageRatioFuzzAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageFuzzRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageRatioFuzzAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguagePartialRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguagePartialRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguagePartialRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguagePartialRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageTokenSortRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageTokenSortRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageTokenSortRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageTokenSortRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageTokenSetRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageTokenSetRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageTokenSetRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageTokenSetRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguagePartialTokenSortRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguagePartialTokenSortRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguagePartialTokenSortRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguagePartialTokenSortRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguagePartialTokenSetRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguagePartialTokenSetRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguagePartialTokenSetRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguagePartialTokenSetRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageQRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageQRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageQRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageQRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }
public class CrossLanguageWRatioStaticBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageWRatioAlgorithm, CrossLanguageStaticMode>, CrossLanguageInputState> { }
public class CrossLanguageWRatioCutoffBenchmarks : CrossLanguageIndividualBenchmark<CrossLanguageFuzzScorer<CrossLanguageWRatioAlgorithm, CrossLanguageCutoffMode>, CrossLanguageInputState> { }

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class CrossLanguageBatchBenchmarks
{
    private string[] queries = [];
    private string[] choices = [];
    private Scorer scorer = Fuzz.Ratio;

    [ParamsSource(nameof(Cases))]
    public CrossLanguageBenchmarkCase Case { get; set; } = null!;

    public static IEnumerable<CrossLanguageBenchmarkCase> Cases => CrossLanguageCorpus.GetCases("batch", null, null, "dotnet");

    [GlobalSetup]
    public void Setup()
    {
        queries = CrossLanguageCorpus.ReadLines("queries.txt");
        choices = CrossLanguageCorpus.ReadLines("choices.txt");
        scorer = Case.Algorithm switch
        {
            "ratio" => Fuzz.Ratio,
            "partial_ratio" => Fuzz.PartialRatio,
            "token_sort_ratio" => Fuzz.TokenSortRatio,
            "token_set_ratio" => Fuzz.TokenSetRatio,
            "partial_token_sort_ratio" => Fuzz.PartialTokenSortRatio,
            "partial_token_set_ratio" => Fuzz.PartialTokenSetRatio,
            "qratio" => Fuzz.QRatio,
            "wratio" => Fuzz.WRatio,
            _ => throw new InvalidOperationException("Unknown cross-language batch scorer.")
        };
    }

    [Benchmark]
    public double[,] Execute()
    {
        return Process.Cdist(queries, choices, scorer);
    }
}

internal static class CrossLanguageCorpus
{
    private static readonly Dictionary<string, int> TierRanks = new(StringComparer.Ordinal)
    {
        ["smoke"] = 0,
        ["common"] = 1,
        ["all"] = 2
    };

    public static IEnumerable<CrossLanguageBenchmarkCase> GetCases(
        string category,
        string? algorithm,
        string? mode,
        string runtime)
    {
        string directory = ResolveDirectory();
        Dictionary<string, (string Source, string Target)> pairs = ReadPairs(Path.Combine(directory, "pairs.tsv"));
        string selectedTier = Environment.GetEnvironmentVariable("RAPIDFUZZ_BENCHMARK_TIER") ?? "common";

        if (!TierRanks.TryGetValue(selectedTier, out int selectedRank))
        {
            throw new InvalidOperationException("Unknown cross-language benchmark tier.");
        }

        string[] lines = File.ReadAllLines(Path.Combine(directory, "cases.tsv"));

        for (int index = 1; index < lines.Length; index++)
        {
            string[] fields = lines[index].Split('\t');

            if (fields.Length != 8 || fields[1] != category || (algorithm is not null && fields[2] != algorithm) ||
                (mode is not null && fields[3] != mode) || !ContainsRuntime(fields[7], runtime))
            {
                continue;
            }

            if (!TierRanks.TryGetValue(fields[6], out int minimumRank) || minimumRank > selectedRank)
            {
                continue;
            }

            string source = string.Empty;
            string target = string.Empty;

            if (fields[4].Length > 0)
            {
                (source, target) = pairs[fields[4]];
            }

            yield return new CrossLanguageBenchmarkCase(
                fields[0],
                fields[1],
                fields[2],
                fields[3],
                source,
                target,
                double.Parse(fields[5], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    public static string[] ReadLines(string fileName)
    {
        return File.ReadAllLines(Path.Combine(ResolveDirectory(), fileName));
    }

    public static string ResolveDirectory()
    {
        string? configured = Environment.GetEnvironmentVariable("RAPIDFUZZ_BENCHMARK_CORPUS");
        string directory = configured is null
            ? Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "benchmark-corpus")
            : Path.GetFullPath(configured);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Cross-language benchmark corpus was not found at '{directory}'.");
        }

        return directory;
    }

    private static Dictionary<string, (string Source, string Target)> ReadPairs(string path)
    {
        Dictionary<string, (string Source, string Target)> pairs = new(StringComparer.Ordinal);
        string[] lines = File.ReadAllLines(path);

        for (int index = 1; index < lines.Length; index++)
        {
            string[] fields = lines[index].Split('\t');

            if (fields.Length != 5 || !pairs.TryAdd(fields[0], (fields[3], fields[4])))
            {
                throw new InvalidOperationException("Invalid or duplicate cross-language benchmark pair.");
            }
        }

        return pairs;
    }

    private static bool ContainsRuntime(string runtimes, string runtime)
    {
        string[] values = runtimes.Split(',');

        for (int index = 0; index < values.Length; index++)
        {
            if (string.Equals(values[index], runtime, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
