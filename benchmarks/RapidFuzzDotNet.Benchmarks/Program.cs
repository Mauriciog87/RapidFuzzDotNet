using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;

namespace RapidFuzzDotNet.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && string.Equals(args[0], "--cross-language-validate", StringComparison.Ordinal))
            {
                return CrossLanguageValidation.Run(args[1..]);
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Benchmark execution failed: {exception.Message.Trim()}");
            return 2;
        }
    }
}

[MemoryDiagnoser]
public class FuzzBenchmarks
{
    private readonly string first = "fuzzy wuzzy was a bear";
    private readonly string second = "wuzzy fuzzy was a bear";
    private readonly string longFirst = string.Concat(Enumerable.Repeat("abcdefghij", 8));
    private readonly string longSecond = string.Concat(Enumerable.Repeat("Zbcdefghij", 8));
    private readonly int[] sequenceFirst = [1, 2, 3, 4, 5, 6, 7, 8];
    private readonly int[] sequenceSecond = [0, 1, 2, 3, 4, 7, 8, 9];
    private readonly CachedRatio cachedRatio = new("fuzzy wuzzy was a bear");
    private readonly CachedRatio cachedLongRatio = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly CachedPartialRatio cachedPartialRatio = new("fuzzy wuzzy was a bear");
    private readonly CachedRatio<int> cachedSequenceRatio = new([1, 2, 3, 4, 5, 6, 7, 8]);
    private readonly CachedPartialRatio<int> cachedSequencePartialRatio = new([1, 2, 3, 4, 5, 6, 7, 8]);
    private readonly CachedTokenSortRatio cachedTokenSortRatio = new("fuzzy wuzzy was a bear");
    private readonly CachedTokenSetRatio cachedTokenSetRatio = new("fuzzy wuzzy was a bear");
    private readonly CachedTokenRatio cachedTokenRatio = new("fuzzy wuzzy was a bear");
    private readonly CachedPartialTokenRatio cachedPartialTokenRatio = new("fuzzy wuzzy was a bear");
    private readonly CachedWRatio cachedWRatio = new("fuzzy wuzzy was a bear");

    [Benchmark(Baseline = true)]
    public double Ratio()
    {
        return Fuzz.Ratio(first, second);
    }

    [Benchmark]
    public double RatioWithScoreCutoff()
    {
        return Fuzz.Ratio(first, second, 90.0);
    }

    [Benchmark]
    public double LongRatio()
    {
        return Fuzz.Ratio(longFirst, longSecond);
    }

    [Benchmark]
    public double TokenSortRatio()
    {
        return Fuzz.TokenSortRatio(first, second);
    }

    [Benchmark]
    public double TokenSetRatio()
    {
        return Fuzz.TokenSetRatio(first, second);
    }

    [Benchmark]
    public double PartialRatio()
    {
        return Fuzz.PartialRatio(first, second);
    }

    [Benchmark]
    public double PartialRatioWithScoreCutoff()
    {
        return Fuzz.PartialRatio(first, second, 90.0);
    }

    [Benchmark]
    public double PartialTokenRatio()
    {
        return Fuzz.PartialTokenRatio(first, second);
    }

    [Benchmark]
    public double QRatio()
    {
        return Fuzz.QRatio(first, second);
    }

    [Benchmark]
    public double WRatio()
    {
        return Fuzz.WRatio(first, second);
    }

    [Benchmark]
    public double CachedRatio()
    {
        return cachedRatio.Similarity(second);
    }

    [Benchmark]
    public double CachedLongRatio()
    {
        return cachedLongRatio.Similarity(longSecond);
    }

    [Benchmark]
    public double CachedPartialRatio()
    {
        return cachedPartialRatio.Similarity(second);
    }

    [Benchmark]
    public double GenericRatio()
    {
        return Fuzz.Ratio<int>(sequenceFirst, sequenceSecond);
    }

    [Benchmark]
    public double GenericRatioWithScoreCutoff()
    {
        return Fuzz.Ratio<int>(sequenceFirst, sequenceSecond, 70.0);
    }

    [Benchmark]
    public double GenericPartialRatio()
    {
        return Fuzz.PartialRatio<int>(sequenceFirst, sequenceSecond);
    }

    [Benchmark]
    public double GenericPartialRatioWithScoreCutoff()
    {
        return Fuzz.PartialRatio<int>(sequenceFirst, sequenceSecond, 70.0);
    }

    [Benchmark]
    public double GenericCachedRatio()
    {
        return cachedSequenceRatio.Similarity(sequenceSecond);
    }

    [Benchmark]
    public double GenericCachedPartialRatio()
    {
        return cachedSequencePartialRatio.Similarity(sequenceSecond);
    }

    [Benchmark]
    public double CachedTokenSortRatio()
    {
        return cachedTokenSortRatio.Similarity(second);
    }

    [Benchmark]
    public double CachedTokenSetRatio()
    {
        return cachedTokenSetRatio.Similarity(second);
    }

    [Benchmark]
    public double CachedTokenRatio()
    {
        return cachedTokenRatio.Similarity(second);
    }

    [Benchmark]
    public double CachedPartialTokenRatio()
    {
        return cachedPartialTokenRatio.Similarity(second);
    }

    [Benchmark]
    public double CachedWRatio()
    {
        return cachedWRatio.Similarity(second);
    }
}

[MemoryDiagnoser]
public class TokenizationBenchmarkParityBenchmarks
{
    private readonly string denseWhitespaceSource = "   alpha   beta\tbeta\r\ngamma   ";
    private readonly string denseWhitespaceTarget = "gamma alpha   beta epsilon";
    private readonly string duplicateSource = "alpha alpha alpha beta beta gamma";
    private readonly string duplicateTarget = "gamma beta alpha alpha delta";
    private readonly string uniqueSource = string.Join(' ', Enumerable.Range(0, 32).Select(index => "token" + index));
    private readonly string uniqueTarget = string.Join(' ', Enumerable.Range(16, 32).Select(index => "token" + index));
    private readonly string longSource = string.Concat(Enumerable.Repeat("alpha beta gamma delta ", 24));
    private readonly string longTarget = string.Concat(Enumerable.Repeat("gamma beta epsilon delta ", 24));
    private readonly CachedTokenRatio cachedDenseWhitespaceTokenRatio;
    private readonly CachedPartialTokenRatio cachedDuplicatePartialTokenRatio;
    private readonly CachedWRatio cachedUniqueWRatio;
    private readonly CachedTokenSortRatio cachedLongTokenSortRatio;
    private readonly CachedTokenSetRatio cachedLongTokenSetRatio;

    public TokenizationBenchmarkParityBenchmarks()
    {
        cachedDenseWhitespaceTokenRatio = new CachedTokenRatio(denseWhitespaceSource);
        cachedDuplicatePartialTokenRatio = new CachedPartialTokenRatio(duplicateSource);
        cachedUniqueWRatio = new CachedWRatio(uniqueSource);
        cachedLongTokenSortRatio = new CachedTokenSortRatio(longSource);
        cachedLongTokenSetRatio = new CachedTokenSetRatio(longSource);
    }

    [Benchmark(Baseline = true)]
    public double DenseWhitespaceTokenRatio()
    {
        return Fuzz.TokenRatio(denseWhitespaceSource, denseWhitespaceTarget);
    }

    [Benchmark]
    public double DenseWhitespaceCachedTokenRatio()
    {
        return cachedDenseWhitespaceTokenRatio.Similarity(denseWhitespaceTarget);
    }

    [Benchmark]
    public double DuplicatePartialTokenRatio()
    {
        return Fuzz.PartialTokenRatio(duplicateSource, duplicateTarget);
    }

    [Benchmark]
    public double DuplicateCachedPartialTokenRatio()
    {
        return cachedDuplicatePartialTokenRatio.Similarity(duplicateTarget);
    }

    [Benchmark]
    public double UniqueWRatio()
    {
        return Fuzz.WRatio(uniqueSource, uniqueTarget);
    }

    [Benchmark]
    public double UniqueCachedWRatio()
    {
        return cachedUniqueWRatio.Similarity(uniqueTarget);
    }

    [Benchmark]
    public double LongTokenSortRatio()
    {
        return Fuzz.TokenSortRatio(longSource, longTarget);
    }

    [Benchmark]
    public double LongCachedTokenSortRatio()
    {
        return cachedLongTokenSortRatio.Similarity(longTarget);
    }

    [Benchmark]
    public double LongTokenSetRatio()
    {
        return Fuzz.TokenSetRatio(longSource, longTarget);
    }

    [Benchmark]
    public double LongCachedTokenSetRatio()
    {
        return cachedLongTokenSetRatio.Similarity(longTarget);
    }
}

[MemoryDiagnoser]
public class LevenshteinBenchmarks
{
    private readonly string shortFirst = "lewenstein";
    private readonly string shortSecond = "levenshtein";
    private readonly string mediumFirst = "fuzzy wuzzy was a bear in new york city";
    private readonly string mediumSecond = "wuzzy fuzzy was a bear near newark city";
    private readonly string longFirst = string.Concat(Enumerable.Repeat("abcdefghij", 16));
    private readonly string longSecond = string.Concat(Enumerable.Repeat("Zbcdefghij", 16));
    private readonly int[] sequenceFirst = [1, 2, 3, 5, 8, 13, 21, 34];
    private readonly int[] sequenceSecond = [1, 3, 5, 8, 13, 21, 55];
    private readonly CachedLevenshtein cachedShort;
    private readonly CachedLevenshtein cachedLong;
    private readonly CachedLevenshtein<int> cachedSequence;

    public LevenshteinBenchmarks()
    {
        cachedShort = new CachedLevenshtein(shortFirst);
        cachedLong = new CachedLevenshtein(longFirst);
        cachedSequence = new CachedLevenshtein<int>(sequenceFirst);
    }

    [Benchmark(Baseline = true)]
    public int ShortStringDistance()
    {
        return Levenshtein.Distance(shortFirst, shortSecond);
    }

    [Benchmark]
    public int ShortStringDistanceWithCutoff()
    {
        return Levenshtein.Distance(shortFirst, shortSecond, 3);
    }

    [Benchmark]
    public int MediumStringDistance()
    {
        return Levenshtein.Distance(mediumFirst, mediumSecond);
    }

    [Benchmark]
    public int LongStringDistance()
    {
        return Levenshtein.Distance(longFirst, longSecond);
    }

    [Benchmark]
    public int LongStringDistanceWithCutoff()
    {
        return Levenshtein.Distance(longFirst, longSecond, 20, 16);
    }

    [Benchmark]
    public int CachedShortStringDistance()
    {
        return cachedShort.Distance(shortSecond);
    }

    [Benchmark]
    public int CachedLongStringDistance()
    {
        return cachedLong.Distance(longSecond);
    }

    [Benchmark]
    public int GenericSequenceDistance()
    {
        return Levenshtein.Distance(sequenceFirst, sequenceSecond);
    }

    [Benchmark]
    public int GenericCachedSequenceDistance()
    {
        return cachedSequence.Distance(sequenceSecond);
    }

    [Benchmark]
    public EditOperations GenericCachedSequenceEditops()
    {
        return cachedSequence.Editops(sequenceSecond);
    }
}

[MemoryDiagnoser]
public class LcsBenchmarks
{
    private readonly string shortFirst = "lewenstein";
    private readonly string shortSecond = "levenshtein";
    private readonly string mediumFirst = "fuzzy wuzzy was a bear in new york city";
    private readonly string mediumSecond = "wuzzy fuzzy was a bear near newark city";
    private readonly string longFirst = string.Concat(Enumerable.Repeat("abcdefghij", 16));
    private readonly string longSecond = string.Concat(Enumerable.Repeat("Zbcdefghij", 16));
    private readonly int[] sequenceFirst = [1, 2, 3, 5, 8, 13, 21, 34];
    private readonly int[] sequenceSecond = [1, 3, 5, 8, 13, 21, 55];
    private readonly CachedLcsSeq cachedShort;
    private readonly CachedLcsSeq cachedLong;
    private readonly CachedLcsSeq<int> cachedSequence;

    public LcsBenchmarks()
    {
        cachedShort = new CachedLcsSeq(shortFirst);
        cachedLong = new CachedLcsSeq(longFirst);
        cachedSequence = new CachedLcsSeq<int>(sequenceFirst);
    }

    [Benchmark(Baseline = true)]
    public int ShortStringSimilarity()
    {
        return LcsSeq.Similarity(shortFirst, shortSecond);
    }

    [Benchmark]
    public int ShortStringSimilarityWithCutoff()
    {
        return LcsSeq.Similarity(shortFirst, shortSecond, 7);
    }

    [Benchmark]
    public int MediumStringSimilarity()
    {
        return LcsSeq.Similarity(mediumFirst, mediumSecond);
    }

    [Benchmark]
    public int LongStringSimilarity()
    {
        return LcsSeq.Similarity(longFirst, longSecond);
    }

    [Benchmark]
    public int LongStringDistanceWithCutoff()
    {
        return LcsSeq.Distance(longFirst, longSecond, 20, 16);
    }

    [Benchmark]
    public int CachedShortStringSimilarity()
    {
        return cachedShort.Similarity(shortSecond);
    }

    [Benchmark]
    public int CachedLongStringSimilarity()
    {
        return cachedLong.Similarity(longSecond);
    }

    [Benchmark]
    public int GenericSequenceSimilarity()
    {
        return LcsSeq.Similarity(sequenceFirst, sequenceSecond);
    }

    [Benchmark]
    public int GenericCachedSequenceSimilarity()
    {
        return cachedSequence.Similarity(sequenceSecond);
    }

    [Benchmark]
    public EditOperations GenericCachedSequenceEditops()
    {
        return cachedSequence.Editops(sequenceSecond);
    }
}

[MemoryDiagnoser]
public class JaroWinklerBenchmarks
{
    private readonly string shortFirst = "lewenstein";
    private readonly string shortSecond = "levenshtein";
    private readonly string mediumFirst = "fuzzy wuzzy was a bear in new york city";
    private readonly string mediumSecond = "wuzzy fuzzy was a bear near newark city";
    private readonly string longFirst = string.Concat(Enumerable.Repeat("abcdefghij", 16));
    private readonly string longSecond = string.Concat(Enumerable.Repeat("Zbcdefghij", 16));
    private readonly int[] sequenceFirst = [1, 2, 3, 5, 8, 13, 21, 34];
    private readonly int[] sequenceSecond = [1, 3, 5, 8, 13, 21, 55];
    private readonly CachedJaroWinkler cachedShort;
    private readonly CachedJaroWinkler cachedLong;
    private readonly CachedJaroWinkler<int> cachedSequence;

    public JaroWinklerBenchmarks()
    {
        cachedShort = new CachedJaroWinkler(shortFirst);
        cachedLong = new CachedJaroWinkler(longFirst);
        cachedSequence = new CachedJaroWinkler<int>(sequenceFirst);
    }

    [Benchmark(Baseline = true)]
    public double ShortStringSimilarity()
    {
        return JaroWinkler.Similarity(shortFirst, shortSecond);
    }

    [Benchmark]
    public double ShortStringSimilarityWithCutoff()
    {
        return JaroWinkler.Similarity(shortFirst, shortSecond, 0.1, 0.7, 0.7);
    }

    [Benchmark]
    public double MediumStringSimilarity()
    {
        return JaroWinkler.Similarity(mediumFirst, mediumSecond);
    }

    [Benchmark]
    public double LongStringSimilarity()
    {
        return JaroWinkler.Similarity(longFirst, longSecond);
    }

    [Benchmark]
    public double CachedShortStringSimilarity()
    {
        return cachedShort.Similarity(shortSecond);
    }

    [Benchmark]
    public double CachedLongStringSimilarity()
    {
        return cachedLong.Similarity(longSecond);
    }

    [Benchmark]
    public double GenericSequenceSimilarity()
    {
        return JaroWinkler.Similarity(sequenceFirst, sequenceSecond);
    }

    [Benchmark]
    public double GenericCachedSequenceSimilarity()
    {
        return cachedSequence.Similarity(sequenceSecond);
    }
}

[MemoryDiagnoser]
public class UpstreamBenchmarkParityBenchmarks
{
    private readonly string similarFirst = "aaaaa aaaaa";
    private readonly string similarSecond = "aaaaa aaaab";
    private readonly string differentFirst = "aaaaa aaaaa";
    private readonly string differentSecond = "bbbbb bbbbb";
    private readonly string differentLengthFirst = "aaaaa b";
    private readonly string differentLengthSecond = "bbbbb bbbbbbbbb";
    private readonly string longSimilarFirst = "a" + string.Concat(Enumerable.Repeat("b", 498)) + "a";
    private readonly string longSimilarSecond = "a" + string.Concat(Enumerable.Repeat("b", 498)) + "c";
    private readonly string longDifferentFirst = string.Concat(Enumerable.Repeat("a", 500));
    private readonly string longDifferentSecond = string.Concat(Enumerable.Repeat("b", 500));
    private readonly int[] genericSimilarFirst = [1, 2, 3, 4, 5, 6, 7, 8];
    private readonly int[] genericSimilarSecond = [1, 2, 3, 4, 5, 6, 7, 9];
    private readonly CachedLevenshtein cachedLevenshteinSimilar = new("aaaaa aaaaa");
    private readonly CachedLcsSeq cachedLcsSimilar = new("aaaaa aaaaa");
    private readonly CachedJaro cachedJaroSimilar = new("aaaaa aaaaa");
    private readonly CachedRatio cachedRatioSimilar = new("aaaaa aaaaa");
    private readonly CachedTokenRatio cachedTokenRatioSimilar = new("aaaaa aaaaa");
    private readonly CachedLevenshtein<int> cachedGenericLevenshtein;
    private readonly CachedLcsSeq<int> cachedGenericLcs;
    private readonly CachedJaro<int> cachedGenericJaro;

    public UpstreamBenchmarkParityBenchmarks()
    {
        cachedGenericLevenshtein = new CachedLevenshtein<int>(genericSimilarFirst);
        cachedGenericLcs = new CachedLcsSeq<int>(genericSimilarFirst);
        cachedGenericJaro = new CachedJaro<int>(genericSimilarFirst);
    }

    [Benchmark]
    public double UpstreamBmFuzzRatio1()
    {
        return Fuzz.Ratio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzRatio2()
    {
        return Fuzz.Ratio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzRatioCutoff()
    {
        return Fuzz.Ratio(similarFirst, similarSecond, 90.0);
    }

    [Benchmark]
    public double UpstreamBmFuzzRatioCached()
    {
        return cachedRatioSimilar.Similarity(similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzRatioGenericSequence()
    {
        return Fuzz.Ratio<int>(genericSimilarFirst, genericSimilarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzPartialRatio1()
    {
        return Fuzz.PartialRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzPartialRatio2()
    {
        return Fuzz.PartialRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzTokenSort1()
    {
        return Fuzz.TokenSortRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzTokenSort2()
    {
        return Fuzz.TokenSortRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzTokenSet1()
    {
        return Fuzz.TokenSetRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzTokenSet2()
    {
        return Fuzz.TokenSetRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzPartialTokenSet1()
    {
        return Fuzz.PartialTokenSetRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzPartialTokenSet2()
    {
        return Fuzz.PartialTokenSetRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzToken1()
    {
        return Fuzz.TokenRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzToken2()
    {
        return Fuzz.TokenRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzTokenCached()
    {
        return cachedTokenRatioSimilar.Similarity(similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzTokenCutoff()
    {
        return Fuzz.TokenRatio(similarFirst, similarSecond, 90.0);
    }

    [Benchmark]
    public double UpstreamBmFuzzPartialToken1()
    {
        return Fuzz.PartialTokenRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzPartialToken2()
    {
        return Fuzz.PartialTokenRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzWRatio1()
    {
        return Fuzz.WRatio(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzWRatio2()
    {
        return Fuzz.WRatio(differentLengthFirst, differentLengthSecond);
    }

    [Benchmark]
    public double UpstreamBmFuzzWRatio3()
    {
        return Fuzz.WRatio(differentFirst, differentSecond);
    }

    [Benchmark]
    public int UpstreamBmLevWeightedDist1()
    {
        return Levenshtein.Distance(similarFirst, similarSecond, new LevenshteinWeights(1, 1, 2));
    }

    [Benchmark]
    public int UpstreamBmLevWeightedDist2()
    {
        return Levenshtein.Distance(differentFirst, differentSecond, new LevenshteinWeights(1, 1, 2));
    }

    [Benchmark]
    public double UpstreamBmLevNormWeightedDist1()
    {
        return Levenshtein.NormalizedDistance(similarFirst, similarSecond, new LevenshteinWeights(1, 1, 2));
    }

    [Benchmark]
    public double UpstreamBmLevNormWeightedDist2()
    {
        return Levenshtein.NormalizedDistance(differentFirst, differentSecond, new LevenshteinWeights(1, 1, 2));
    }

    [Benchmark]
    public int UpstreamBmLevLongSimilarSequence()
    {
        return Levenshtein.Distance(longSimilarFirst, longSimilarSecond, 4);
    }

    [Benchmark]
    public int UpstreamBmLevLongNonSimilarSequence()
    {
        return Levenshtein.Distance(longDifferentFirst, longDifferentSecond, 64);
    }

    [Benchmark]
    public int UpstreamBmLevenshtein()
    {
        return Levenshtein.Distance(similarFirst, similarSecond);
    }

    [Benchmark]
    public int UpstreamBmLevenshteinCached()
    {
        return cachedLevenshteinSimilar.Distance(similarSecond);
    }

    [Benchmark]
    public int UpstreamBmLevenshteinCutoff()
    {
        return Levenshtein.Distance(similarFirst, similarSecond, 4);
    }

    [Benchmark]
    public int UpstreamBmLevenshteinGenericSequence()
    {
        return Levenshtein.Distance(genericSimilarFirst, genericSimilarSecond);
    }

    [Benchmark]
    public int UpstreamBmLevenshteinGenericSequenceCached()
    {
        return cachedGenericLevenshtein.Distance(genericSimilarSecond);
    }

    [Benchmark]
    public int UpstreamBmLcsLongSimilarSequence()
    {
        return LcsSeq.Similarity(longSimilarFirst, longSimilarSecond);
    }

    [Benchmark]
    public int UpstreamBmLcsLongNonSimilarSequence()
    {
        return LcsSeq.Similarity(longDifferentFirst, longDifferentSecond);
    }

    [Benchmark]
    public int UpstreamBmLcs()
    {
        return LcsSeq.Similarity(similarFirst, similarSecond);
    }

    [Benchmark]
    public int UpstreamBmLcsCached()
    {
        return cachedLcsSimilar.Similarity(similarSecond);
    }

    [Benchmark]
    public int UpstreamBmLcsCutoff()
    {
        return LcsSeq.Similarity(similarFirst, similarSecond, 8);
    }

    [Benchmark]
    public int UpstreamBmLcsGenericSequence()
    {
        return LcsSeq.Similarity(genericSimilarFirst, genericSimilarSecond);
    }

    [Benchmark]
    public int UpstreamBmLcsGenericSequenceCached()
    {
        return cachedGenericLcs.Similarity(genericSimilarSecond);
    }

    [Benchmark]
    public double UpstreamBmJaro()
    {
        return Jaro.Similarity(similarFirst, similarSecond);
    }

    [Benchmark]
    public double UpstreamBmJaroCached()
    {
        return cachedJaroSimilar.Similarity(similarSecond);
    }

    [Benchmark]
    public double UpstreamBmJaroCutoff()
    {
        return Jaro.Similarity(similarFirst, similarSecond, 0.9);
    }

    [Benchmark]
    public double UpstreamBmJaroGenericSequence()
    {
        return Jaro.Similarity(genericSimilarFirst, genericSimilarSecond);
    }

    [Benchmark]
    public double UpstreamBmJaroGenericSequenceCached()
    {
        return cachedGenericJaro.Similarity(genericSimilarSecond);
    }

    [Benchmark]
    public double UpstreamBmJaroLongSimilarSequence()
    {
        return Jaro.Similarity(longSimilarFirst, longSimilarSecond);
    }

    [Benchmark]
    public double UpstreamBmJaroLongNonSimilarSequence()
    {
        return Jaro.Similarity(longDifferentFirst, longDifferentSecond);
    }
}

[MemoryDiagnoser]
public class DistanceBenchmarks
{
    private readonly string first = "lewenstein";
    private readonly string second = "levenshtein";
    private readonly string longFirst = string.Concat(Enumerable.Repeat("abcdefghij", 8));
    private readonly string longSecond = string.Concat(Enumerable.Repeat("Zbcdefghij", 8));
    private readonly string cutoffFirst = "abcdef";
    private readonly string cutoffSecond = "azced";
    private readonly string upstreamBandedFirst = "kkkkbbbbfkkkkkkibfkkkafakkfekgkkkkkkkkkkbdbbddddddddddafkkkekkkhkk";
    private readonly string upstreamBandedSecond = "khddddddddkkkkdgkdikkccccckcckkkekkkkdddddddddddafkkhckkkkkdckkkcc";
    private readonly CachedLevenshtein cachedLevenshtein = new("lewenstein");
    private readonly CachedLevenshtein cachedLongLevenshtein = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly CachedIndel cachedIndel = new("lewenstein");
    private readonly CachedIndel cachedLongIndel = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly CachedLcsSeq cachedLongLcsSeq = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly CachedOsa cachedLongOsa = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly CachedJaro cachedLongJaro = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly CachedJaroWinkler cachedJaroWinkler = new("lewenstein");
    private readonly CachedJaroWinkler cachedLongJaroWinkler = new(string.Concat(Enumerable.Repeat("abcdefghij", 8)));
    private readonly int[] multiLevenshteinBuffer = new int[3];
    private readonly double[] multiJaroBuffer = new double[3];
    private readonly double[] multiJaroWinklerBuffer = new double[3];
    private readonly MultiLevenshtein multiLevenshtein = new(
    [
        "lewenstein",
        "levenshtein",
        "kitten"
    ]);
    private readonly MultiJaro multiJaro = new(
    [
        "lewenstein",
        "levenshtein",
        "kitten"
    ]);
    private readonly MultiJaroWinkler multiJaroWinkler = new(
    [
        "lewenstein",
        "levenshtein",
        "kitten"
    ]);

    [Benchmark(Baseline = true)]
    public int LevenshteinDistance()
    {
        return Levenshtein.Distance(first, second);
    }

    [Benchmark]
    public int LongLevenshteinDistance()
    {
        return Levenshtein.Distance(longFirst, longSecond);
    }

    [Benchmark]
    public int LevenshteinDistanceWithScoreHint()
    {
        return Levenshtein.Distance(first, second, int.MaxValue, 2);
    }

    [Benchmark]
    public int LevenshteinSmallCutoffDistance()
    {
        return Levenshtein.Distance(cutoffFirst, cutoffSecond, 2);
    }

    [Benchmark]
    public int LongLevenshteinDistanceWithScoreHint()
    {
        return Levenshtein.Distance(longFirst, longSecond, int.MaxValue, 8);
    }

    [Benchmark]
    public int LongLevenshteinDistanceWithScoreCutoff()
    {
        return Levenshtein.Distance(longFirst, longSecond, 8, 8);
    }

    [Benchmark]
    public int UpstreamBandedLevenshteinDistance()
    {
        return Levenshtein.Distance(upstreamBandedFirst, upstreamBandedSecond, 31);
    }

    [Benchmark]
    public int IndelDistance()
    {
        return Indel.Distance(first, second);
    }

    [Benchmark]
    public int LongIndelDistance()
    {
        return Indel.Distance(longFirst, longSecond);
    }

    [Benchmark]
    public int LcsSimilarity()
    {
        return LcsSeq.Similarity(first, second);
    }

    [Benchmark]
    public int LongLcsSimilarity()
    {
        return LcsSeq.Similarity(longFirst, longSecond);
    }

    [Benchmark]
    public int LcsSimilarityWithScoreCutoff()
    {
        return LcsSeq.Similarity(cutoffFirst, cutoffSecond, 3);
    }

    [Benchmark]
    public int OsaDistance()
    {
        return Osa.Distance(first, second);
    }

    [Benchmark]
    public int LongOsaDistance()
    {
        return Osa.Distance(longFirst, longSecond);
    }

    [Benchmark]
    public int DamerauLevenshteinDistance()
    {
        return DamerauLevenshtein.Distance(first, second);
    }

    [Benchmark]
    public double JaroSimilarity()
    {
        return Jaro.Similarity(first, second);
    }

    [Benchmark]
    public double LongJaroSimilarity()
    {
        return Jaro.Similarity(longFirst, longSecond);
    }

    [Benchmark]
    public double JaroSimilarityWithScoreCutoff()
    {
        return Jaro.Similarity(first, second, 0.7);
    }

    [Benchmark]
    public double JaroWinklerSimilarity()
    {
        return JaroWinkler.Similarity(first, second);
    }

    [Benchmark]
    public double LongJaroWinklerSimilarity()
    {
        return JaroWinkler.Similarity(longFirst, longSecond);
    }

    [Benchmark]
    public int CachedLevenshteinDistance()
    {
        return cachedLevenshtein.Distance(second);
    }

    [Benchmark]
    public int CachedLongLevenshteinDistance()
    {
        return cachedLongLevenshtein.Distance(longSecond);
    }

    [Benchmark]
    public int CachedLongLevenshteinDistanceWithScoreCutoff()
    {
        return cachedLongLevenshtein.Distance(longSecond, 8, 8);
    }

    [Benchmark]
    public int CachedIndelDistance()
    {
        return cachedIndel.Distance(second);
    }

    [Benchmark]
    public int CachedLongIndelDistance()
    {
        return cachedLongIndel.Distance(longSecond);
    }

    [Benchmark]
    public int CachedLongLcsSimilarity()
    {
        return cachedLongLcsSeq.Similarity(longSecond);
    }

    [Benchmark]
    public int CachedLongOsaDistance()
    {
        return cachedLongOsa.Distance(longSecond);
    }

    [Benchmark]
    public double CachedLongJaroSimilarity()
    {
        return cachedLongJaro.Similarity(longSecond);
    }

    [Benchmark]
    public double CachedJaroWinklerSimilarity()
    {
        return cachedJaroWinkler.Similarity(second);
    }

    [Benchmark]
    public double CachedLongJaroWinklerSimilarity()
    {
        return cachedLongJaroWinkler.Similarity(longSecond);
    }

    [Benchmark]
    public int[] MultiLevenshteinDistances()
    {
        return multiLevenshtein.Distances(second);
    }

    [Benchmark]
    public int MultiLevenshteinDistancesSpan()
    {
        multiLevenshtein.Distances(second, multiLevenshteinBuffer);
        return multiLevenshteinBuffer[0];
    }

    [Benchmark]
    public double[] MultiJaroSimilarities()
    {
        return multiJaro.Similarities(second);
    }

    [Benchmark]
    public double MultiJaroSimilaritiesSpan()
    {
        multiJaro.Similarities(second, multiJaroBuffer);
        return multiJaroBuffer[0];
    }

    [Benchmark]
    public double[] MultiJaroWinklerSimilarities()
    {
        return multiJaroWinkler.Similarities(second);
    }

    [Benchmark]
    public double MultiJaroWinklerSimilaritiesSpan()
    {
        multiJaroWinkler.Similarities(second, multiJaroWinklerBuffer);
        return multiJaroWinklerBuffer[0];
    }
}

[MemoryDiagnoser]
public class ProcessBenchmarks
{
    private readonly string query = "new york mets";
    private readonly string[] choices =
    [
        "new york mets",
        "new york yankees",
        "atlanta braves",
        "new york city",
        "newark bears"
    ];
    private readonly string[] queries = ["new york mets", "atlanta braves", "newark bears"];
    private readonly string[] pairedChoices = ["new york yankees", "atlanta braves", "new york city"];

    [Benchmark]
    public ExtractedResult<string>? ExtractOneDefault()
    {
        return Process.ExtractOne(query, choices);
    }

    [Benchmark]
    public List<ExtractedResult<string>> ExtractRatio()
    {
        return Process.Extract(query, choices, scorer: Fuzz.Ratio, limit: 3);
    }

    [Benchmark]
    public double[,] Cdist()
    {
        return Process.Cdist(queries, choices, scorer: Fuzz.Ratio);
    }

    [Benchmark]
    public double[] Cpdist()
    {
        return Process.Cpdist(queries, pairedChoices, scorer: Fuzz.Ratio);
    }
}

[MemoryDiagnoser]
public class ExperimentalBenchmarks
{
    private readonly double[] scores = new double[3];
    private readonly string target = "wuzzy fuzzy was a bear";
    private readonly MultiWRatio scorer = new(
    [
        "fuzzy wuzzy was a bear",
        "new york mets",
        "atlanta braves"
    ]);

    [Benchmark]
    public double[] MultiWRatio()
    {
        return scorer.Similarities(target);
    }

    [Benchmark]
    public double MultiWRatioSpan()
    {
        scorer.Similarities(target, scores);
        return scores[0];
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class ManagedParityBenchmarks
{
    private int[] source = [];
    private int[] target = [];
    private int[][] sources = [];
    private int[] multiBuffer = [];
    private string tokenSource = string.Empty;
    private string tokenTarget = string.Empty;
    private CachedLevenshtein<int> cached = new(ReadOnlySpan<int>.Empty);
    private MultiLevenshtein<int> multi = new(0);
    private CachedTokenRatio cachedToken = new(string.Empty);

    [Params(16, 64, 256)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = Enumerable.Range(0, Length).Select(index => index % 17).ToArray();
        target = Enumerable.Range(0, Length).Select(index => (index + 1) % 17).ToArray();
        int batchCount = Math.Max(8, System.Numerics.Vector<ulong>.Count * 2);
        sources = Enumerable.Range(0, batchCount)
            .Select(offset => Enumerable.Range(0, Math.Min(Length, 64)).Select(index => (index + offset) % 17).ToArray())
            .ToArray();
        multiBuffer = new int[batchCount];
        cached = new CachedLevenshtein<int>(source);
        multi = new MultiLevenshtein<int>(sources);
        tokenSource = string.Join(' ', source.Select(value => "token" + value));
        tokenTarget = string.Join(' ', target.Select(value => "token" + value));
        cachedToken = new CachedTokenRatio(tokenSource.AsSpan());
    }

    [Benchmark]
    public int GenericStaticLevenshtein() => Levenshtein.Distance<int>(source, target);

    [Benchmark]
    public int GenericCachedLevenshtein() => cached.Distance(target);

    [Benchmark]
    public int GenericDamerau() => DamerauLevenshtein.Distance<int>(source, target);

    [Benchmark(Baseline = true)]
    public int GenericScalarBatch()
    {
        int total = 0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += Levenshtein.Distance<int>(sources[index], target);
        }

        return total;
    }

    [Benchmark]
    public int GenericMultiSimd()
    {
        multi.Distances(target, multiBuffer);
        int total = 0;

        for (int index = 0; index < multiBuffer.Length; index++)
        {
            total += multiBuffer[index];
        }

        return total;
    }

    [Benchmark]
    public double TokenSpanStatic() => Fuzz.TokenRatio(tokenSource.AsSpan(), tokenTarget.AsSpan());

    [Benchmark]
    public double TokenSpanCached() => cachedToken.Similarity(tokenTarget.AsSpan());

    [Benchmark]
    public int GenericMultiConstruction() => new MultiLevenshtein<int>(sources).Count;
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class PartialRatioOptimizationBenchmarks
{
    private string source = string.Empty;
    private string target = string.Empty;
    private int[] genericSource = [];
    private int[] genericTarget = [];
    private CachedPartialRatio cached = new(string.Empty);
    private CachedPartialRatio<int> genericCached = new(ReadOnlySpan<int>.Empty);

    [Params(64, 256)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = string.Concat(Enumerable.Range(0, Length).Select(index => (char)('a' + (index % 17))));
        target = "prefix" + source[..^1] + "suffix";
        genericSource = Enumerable.Range(0, Length).Select(index => index % 17).ToArray();
        genericTarget = [31, .. genericSource[..^1], 37];
        cached = new CachedPartialRatio(source);
        genericCached = new CachedPartialRatio<int>(genericSource);
    }

    [Benchmark(Baseline = true)]
    public double StaticPartialRatio() => Fuzz.PartialRatio(source, target);

    [Benchmark]
    public double CachedPartialRatio() => cached.Similarity(target);

    [Benchmark]
    public double GenericStaticPartialRatio() => Fuzz.PartialRatio<int>(genericSource, genericTarget);

    [Benchmark]
    public double GenericCachedPartialRatio() => genericCached.Similarity(genericTarget);
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class JaroSimdBenchmarks
{
    private int[][] sources = [];
    private int[] target = [];
    private double[] jaroBuffer = [];
    private double[] jaroWinklerBuffer = [];
    private MultiJaro<int> multiJaro = new(0);
    private MultiJaroWinkler<int> multiJaroWinkler = new(0);

    [Params(32, 64)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        int batchCount = System.Numerics.Vector<ulong>.Count * 2;
        sources = Enumerable.Range(0, batchCount)
            .Select(offset => Enumerable.Range(0, Length).Select(index => (index + offset) % 19).ToArray())
            .ToArray();
        target = Enumerable.Range(0, Length).Select(index => (index * 5) % 19).ToArray();
        jaroBuffer = new double[batchCount];
        jaroWinklerBuffer = new double[batchCount];
        multiJaro = new MultiJaro<int>(sources);
        multiJaroWinkler = new MultiJaroWinkler<int>(sources);
    }

    [Benchmark(Baseline = true)]
    public double GenericScalarBatch()
    {
        double total = 0.0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += Jaro.Similarity<int>(sources[index], target);
        }

        return total;
    }

    [Benchmark]
    public double GenericMultiSimd()
    {
        multiJaro.Similarities(target, jaroBuffer);
        return Sum(jaroBuffer);
    }

    [Benchmark]
    public double GenericJaroWinklerScalarBatch()
    {
        double total = 0.0;

        for (int index = 0; index < sources.Length; index++)
        {
            total += JaroWinkler.Similarity<int>(sources[index], target);
        }

        return total;
    }

    [Benchmark]
    public double GenericJaroWinklerMultiSimd()
    {
        multiJaroWinkler.Similarities(target, jaroWinklerBuffer);
        return Sum(jaroWinklerBuffer);
    }

    private static double Sum(ReadOnlySpan<double> values)
    {
        double total = 0.0;

        for (int index = 0; index < values.Length; index++)
        {
            total += values[index];
        }

        return total;
    }
}
