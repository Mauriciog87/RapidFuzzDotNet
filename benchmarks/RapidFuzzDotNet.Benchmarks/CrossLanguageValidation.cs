using System.Globalization;
using System.Text;
using System.Text.Json;
using RapidFuzz;
using RapidFuzz.Distance;

namespace RapidFuzzDotNet.Benchmarks;

internal static class CrossLanguageValidation
{
    private const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;
    private const double QuantizationScale = 1_000_000_000_000.0;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static int Run(string[] args)
    {
        (string outputPath, string runtimeLabel) = ParseArguments(args);
        List<CrossLanguageValidationResult> results = [];

        foreach (CrossLanguageBenchmarkCase benchmarkCase in CrossLanguageCorpus.GetCases("core", null, null, "dotnet"))
        {
            double value = MeasureIndividual(benchmarkCase);
            results.Add(new CrossLanguageValidationResult(benchmarkCase.CaseId, value, null, 1, [value]));
        }

        foreach (CrossLanguageBenchmarkCase benchmarkCase in CrossLanguageCorpus.GetCases("fuzz", null, null, "dotnet"))
        {
            double value = MeasureIndividual(benchmarkCase);
            results.Add(new CrossLanguageValidationResult(benchmarkCase.CaseId, value, null, 1, [value]));
        }

        string[] queries = CrossLanguageCorpus.ReadLines("queries.txt");
        string[] choices = CrossLanguageCorpus.ReadLines("choices.txt");

        foreach (CrossLanguageBenchmarkCase benchmarkCase in CrossLanguageCorpus.GetCases("batch", null, null, "dotnet"))
        {
            Scorer scorer = CreateScorer(benchmarkCase.Algorithm);
            double[,] matrix = Process.Cdist(queries, choices, scorer);
            (string digest, double[] samples) = CalculateDigest(matrix);
            results.Add(new CrossLanguageValidationResult(benchmarkCase.CaseId, null, digest, matrix.Length, samples));
        }

        string metadataPath = Path.Combine(CrossLanguageCorpus.ResolveDirectory(), "metadata.json");
        using JsonDocument metadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
        string corpusHash = metadata.RootElement.GetProperty("corpus_sha256").GetString()
            ?? throw new InvalidOperationException("Corpus metadata does not contain a SHA-256 value.");
        CrossLanguageValidationDocument document = new(runtimeLabel, corpusHash, results);
        string? directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, JsonSerializer.Serialize(document, SerializerOptions) + Environment.NewLine, new UTF8Encoding(false));
        return 0;
    }

    private static (string OutputPath, string RuntimeLabel) ParseArguments(string[] args)
    {
        string? outputPath = null;
        string runtimeLabel = "dotnet";

        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];

            if (option == "--output" && index + 1 < args.Length)
            {
                outputPath = Path.GetFullPath(args[++index]);
            }
            else if (option == "--runtime" && index + 1 < args.Length)
            {
                runtimeLabel = args[++index];
            }
            else
            {
                throw new ArgumentException($"Unknown cross-language validation option '{option}'.", nameof(args));
            }
        }

        if (outputPath is null)
        {
            throw new ArgumentException("Cross-language validation requires --output.", nameof(args));
        }

        return (outputPath, runtimeLabel);
    }

    private static double MeasureIndividual(CrossLanguageBenchmarkCase benchmarkCase)
    {
        if (benchmarkCase.Category == "fuzz")
        {
            return MeasureFuzz(benchmarkCase);
        }

        bool usesCutoff = benchmarkCase.Mode == "cutoff";

        return (benchmarkCase.Algorithm, benchmarkCase.Mode) switch
        {
            ("levenshtein", "cached") => new CachedLevenshtein(benchmarkCase.Source).Distance(benchmarkCase.Target),
            ("indel", "cached") => new CachedIndel(benchmarkCase.Source).Distance(benchmarkCase.Target),
            ("lcs_seq", "cached") => new CachedLcsSeq(benchmarkCase.Source).Similarity(benchmarkCase.Target),
            ("jaro", "cached") => new CachedJaro(benchmarkCase.Source).Similarity(benchmarkCase.Target),
            ("jaro_winkler", "cached") => new CachedJaroWinkler(benchmarkCase.Source).Similarity(benchmarkCase.Target),
            ("ratio", "cached") => new CachedRatio(benchmarkCase.Source).Similarity(benchmarkCase.Target),
            ("levenshtein", _) => CrossLanguageLevenshteinAlgorithm.Measure(new CrossLanguageInputState(benchmarkCase), usesCutoff),
            ("indel", _) => CrossLanguageIndelAlgorithm.Measure(new CrossLanguageInputState(benchmarkCase), usesCutoff),
            ("lcs_seq", _) => CrossLanguageLcsSeqAlgorithm.Measure(new CrossLanguageInputState(benchmarkCase), usesCutoff),
            ("jaro", _) => CrossLanguageJaroAlgorithm.Measure(new CrossLanguageInputState(benchmarkCase), usesCutoff),
            ("jaro_winkler", _) => CrossLanguageJaroWinklerAlgorithm.Measure(new CrossLanguageInputState(benchmarkCase), usesCutoff),
            ("ratio", _) => CrossLanguageRatioCoreAlgorithm.Measure(new CrossLanguageInputState(benchmarkCase), usesCutoff),
            _ => throw new InvalidOperationException("Unknown cross-language core scorer.")
        };
    }

    private static double MeasureFuzz(CrossLanguageBenchmarkCase benchmarkCase)
    {
        CrossLanguageInputState state = new(benchmarkCase);
        bool usesCutoff = benchmarkCase.Mode == "cutoff";

        return benchmarkCase.Algorithm switch
        {
            "ratio" => CrossLanguageRatioFuzzAlgorithm.Measure(state, usesCutoff),
            "partial_ratio" => CrossLanguagePartialRatioAlgorithm.Measure(state, usesCutoff),
            "token_sort_ratio" => CrossLanguageTokenSortRatioAlgorithm.Measure(state, usesCutoff),
            "token_set_ratio" => CrossLanguageTokenSetRatioAlgorithm.Measure(state, usesCutoff),
            "partial_token_sort_ratio" => CrossLanguagePartialTokenSortRatioAlgorithm.Measure(state, usesCutoff),
            "partial_token_set_ratio" => CrossLanguagePartialTokenSetRatioAlgorithm.Measure(state, usesCutoff),
            "qratio" => CrossLanguageQRatioAlgorithm.Measure(state, usesCutoff),
            "wratio" => CrossLanguageWRatioAlgorithm.Measure(state, usesCutoff),
            _ => throw new InvalidOperationException("Unknown cross-language fuzz scorer.")
        };
    }

    private static Scorer CreateScorer(string algorithm)
    {
        return algorithm switch
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

    private static (string Digest, double[] Samples) CalculateDigest(double[,] values)
    {
        ulong hash = FnvOffset;
        int middleIndex = values.Length / 2;
        double[] samples = new double[3];
        int flatIndex = 0;

        for (int row = 0; row < values.GetLength(0); row++)
        {
            for (int column = 0; column < values.GetLength(1); column++)
            {
                double value = values[row, column];

                if (flatIndex == 0)
                {
                    samples[0] = value;
                }

                if (flatIndex == middleIndex)
                {
                    samples[1] = value;
                }

                if (flatIndex == values.Length - 1)
                {
                    samples[2] = value;
                }

                long quantized = checked((long)Math.Round(value * QuantizationScale, MidpointRounding.AwayFromZero));
                ulong bits = unchecked((ulong)quantized);

                for (int byteIndex = 0; byteIndex < sizeof(long); byteIndex++)
                {
                    hash ^= (byte)(bits >> (byteIndex * 8));
                    hash *= FnvPrime;
                }

                flatIndex++;
            }
        }

        return (hash.ToString("x16", CultureInfo.InvariantCulture), samples);
    }

    private sealed record CrossLanguageValidationDocument(
        string Runtime,
        string CorpusSha256,
        IReadOnlyList<CrossLanguageValidationResult> Results);

    private sealed record CrossLanguageValidationResult(
        string CaseId,
        double? Value,
        string? Digest,
        int Count,
        IReadOnlyList<double> Samples);
}
