using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;
using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;

namespace RapidFuzzDotNet.Benchmarks;

internal static class OfficialCppRegistrationInventory
{
    private static readonly string[] RegistrationNames =
    [
        "LCS/LongSimilar",
        "LCS/LongDifferent",
        "LCS/Static/8",
        "LCS/Static/16",
        "LCS/Static/32",
        "LCS/Static/64",
        "LCS/Cached/8",
        "LCS/Cached/16",
        "LCS/Cached/32",
        "LCS/Cached/64",
        "LCS/SIMD/8",
        "LCS/SIMD/16",
        "LCS/SIMD/32",
        "LCS/SIMD/64",
        "Fuzz/Ratio1",
        "Fuzz/Ratio2",
        "Fuzz/PartialRatio1",
        "Fuzz/PartialRatio2",
        "Fuzz/TokenSort1",
        "Fuzz/TokenSort2",
        "Fuzz/PartialTokenSort1",
        "Fuzz/PartialTokenSort2",
        "Fuzz/TokenSet1",
        "Fuzz/TokenSet2",
        "Fuzz/PartialTokenSet1",
        "Fuzz/PartialTokenSet2",
        "Fuzz/Token1",
        "Fuzz/Token2",
        "Fuzz/PartialToken1",
        "Fuzz/PartialToken2",
        "Fuzz/WRatio1",
        "Fuzz/WRatio2",
        "Fuzz/WRatio3",
        "Jaro/Static/8/8",
        "Jaro/Static/16/16",
        "Jaro/Static/32/32",
        "Jaro/Static/64/64",
        "Jaro/Cached/8/8",
        "Jaro/Cached/16/16",
        "Jaro/Cached/32/32",
        "Jaro/Cached/64/64",
        "Jaro/SIMD/8/8",
        "Jaro/SIMD/16/16",
        "Jaro/SIMD/32/32",
        "Jaro/SIMD/64/64",
        "Jaro/Static/8/1000",
        "Jaro/Static/16/1000",
        "Jaro/Static/32/1000",
        "Jaro/Static/64/1000",
        "Jaro/Cached/8/1000",
        "Jaro/Cached/16/1000",
        "Jaro/Cached/32/1000",
        "Jaro/Cached/64/1000",
        "Jaro/SIMD/8/1000",
        "Jaro/SIMD/16/1000",
        "Jaro/SIMD/32/1000",
        "Jaro/SIMD/64/1000",
        "Jaro/LongSimilar",
        "Jaro/LongDifferent",
        "Levenshtein/LongSimilar",
        "Levenshtein/LongDifferent",
        "Levenshtein/WeightedDistance1",
        "Levenshtein/WeightedDistance2",
        "Levenshtein/WeightedNormalizedDistance1",
        "Levenshtein/WeightedNormalizedDistance2",
        "Levenshtein/Static/8",
        "Levenshtein/Static/16",
        "Levenshtein/Static/32",
        "Levenshtein/Static/64",
        "Levenshtein/Cached/8",
        "Levenshtein/Cached/16",
        "Levenshtein/Cached/32",
        "Levenshtein/Cached/64",
        "Levenshtein/SIMD/8",
        "Levenshtein/SIMD/16",
        "Levenshtein/SIMD/32",
        "Levenshtein/SIMD/64"
    ];

    public static int Count => RegistrationNames.Length;
}

public sealed class OfficialLongCase
{
    public OfficialLongCase(string name, int length, int scoreCutoff, bool similar)
    {
        Name = name;
        Length = length;
        ScoreCutoff = scoreCutoff;
        Similar = similar;
    }

    public string Name { get; }

    public int Length { get; }

    public int ScoreCutoff { get; }

    public bool Similar { get; }

    public override string ToString()
    {
        return $"{Name}/{Length}/{ScoreCutoff}";
    }
}

public sealed class OfficialJaroLengthCase
{
    public OfficialJaroLengthCase(int sourceLength, int targetLength)
    {
        SourceLength = sourceLength;
        TargetLength = targetLength;
    }

    public int SourceLength { get; }

    public int TargetLength { get; }

    public override string ToString()
    {
        return $"{SourceLength}/{TargetLength}";
    }
}

internal static class OfficialCppBenchmarkData
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Generate(Random random, int length)
    {
        char[] result = new char[length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Alphabet[random.Next(Alphabet.Length)];
        }
        return new string(result);
    }

    public static string[] GenerateMany(int count, int length, int seed)
    {
        Random random = new(seed);
        string[] result = new string[count];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Generate(random, length);
        }
        return result;
    }

    public static string CreateLong(int length, bool similar, bool first)
    {
        if (!similar)
        {
            return new string(first ? 'a' : 'b', length);
        }
        return first ? "a" + new string('b', length - 2) + "a" : new string('b', length);
    }

    public static IEnumerable<OfficialLongCase> LcsLongCases()
    {
        yield return new OfficialLongCase("LCS/LongSimilar", 100, 30, true);
        yield return new OfficialLongCase("LCS/LongSimilar", 500, 100, true);
        yield return new OfficialLongCase("LCS/LongSimilar", 500, 30, true);
        yield return new OfficialLongCase("LCS/LongSimilar", 5000, 30, true);
        yield return new OfficialLongCase("LCS/LongSimilar", 10000, 30, true);
        yield return new OfficialLongCase("LCS/LongSimilar", 20000, 30, true);
        yield return new OfficialLongCase("LCS/LongSimilar", 50000, 30, true);
        yield return new OfficialLongCase("LCS/LongDifferent", 100, 30, false);
        yield return new OfficialLongCase("LCS/LongDifferent", 500, 30, false);
        yield return new OfficialLongCase("LCS/LongDifferent", 5000, 30, false);
        yield return new OfficialLongCase("LCS/LongDifferent", 10000, 30, false);
        yield return new OfficialLongCase("LCS/LongDifferent", 20000, 30, false);
        yield return new OfficialLongCase("LCS/LongDifferent", 50000, 30, false);
    }

    public static IEnumerable<OfficialLongCase> StandardLongCases(string prefix)
    {
        int[] lengths = [100, 500, 5000, 10000, 20000, 50000];
        for (int index = 0; index < lengths.Length; index++)
        {
            yield return new OfficialLongCase($"{prefix}/LongSimilar", lengths[index], 30, true);
        }
        for (int index = 0; index < lengths.Length; index++)
        {
            yield return new OfficialLongCase($"{prefix}/LongDifferent", lengths[index], 30, false);
        }
    }

    public static IEnumerable<OfficialJaroLengthCase> JaroLengths()
    {
        int[] lengths = [8, 16, 32, 64];
        for (int index = 0; index < lengths.Length; index++)
        {
            yield return new OfficialJaroLengthCase(lengths[index], lengths[index]);
        }
        for (int index = 0; index < lengths.Length; index++)
        {
            yield return new OfficialJaroLengthCase(lengths[index], 1000);
        }
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLcsLongBenchmarks
{
    private string source = string.Empty;
    private string target = string.Empty;

    [ParamsSource(nameof(Cases))]
    public OfficialLongCase Case { get; set; } = null!;

    public static IEnumerable<OfficialLongCase> Cases => OfficialCppBenchmarkData.LcsLongCases();

    [GlobalSetup]
    public void Setup()
    {
        source = OfficialCppBenchmarkData.CreateLong(Case.Length, Case.Similar, true);
        target = OfficialCppBenchmarkData.CreateLong(Case.Length, Case.Similar, false);
    }

    [Benchmark]
    public int Execute()
    {
        return LcsSeq.Distance(source, target, Case.ScoreCutoff);
    }
}

public abstract class OfficialLcsMatrixBenchmarks
{
    protected string[] Sources { get; private set; } = [];
    protected string[] Targets { get; private set; } = [];

    [Params(8, 16, 32, 64)]
    public int Length { get; set; }

    [GlobalSetup]
    public virtual void Setup()
    {
        Sources = OfficialCppBenchmarkData.GenerateMany(256, Length, 1800 + Length);
        Targets = OfficialCppBenchmarkData.GenerateMany(10000, Length, 1900 + Length);
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLcsStaticBenchmarks : OfficialLcsMatrixBenchmarks
{
    [Benchmark]
    public long Execute()
    {
        long total = 0;
        for (int targetIndex = 0; targetIndex < Targets.Length; targetIndex++)
        {
            for (int sourceIndex = 0; sourceIndex < Sources.Length; sourceIndex++)
            {
                total += LcsSeq.Distance(Sources[sourceIndex], Targets[targetIndex]);
            }
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLcsCachedBenchmarks : OfficialLcsMatrixBenchmarks
{
    [Benchmark]
    public long Execute()
    {
        long total = 0;
        for (int sourceIndex = 0; sourceIndex < Sources.Length; sourceIndex++)
        {
            CachedLcsSeq scorer = new(Sources[sourceIndex]);
            for (int targetIndex = 0; targetIndex < Targets.Length; targetIndex++)
            {
                total += scorer.Similarity(Targets[targetIndex]);
            }
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLcsSimdBenchmarks
{
    private string[] sources = [];
    private string[] targets = [];
    private int[] results = [];

    [Params(8, 16, 32, 64)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        sources = OfficialCppBenchmarkData.GenerateMany(384, Length, 2000 + Length);
        targets = OfficialCppBenchmarkData.GenerateMany(10000, Length, 2100 + Length);
        results = new int[sources.Length];
    }

    [Benchmark]
    public long Execute()
    {
        MultiLcsSeq scorer = new(sources);
        long total = 0;
        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            scorer.Similarities(targets[targetIndex], results);
            total += results[0];
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLevenshteinLongBenchmarks
{
    private string source = string.Empty;
    private string target = string.Empty;

    [ParamsSource(nameof(Cases))]
    public OfficialLongCase Case { get; set; } = null!;

    public static IEnumerable<OfficialLongCase> Cases => OfficialCppBenchmarkData.StandardLongCases("Levenshtein");

    [GlobalSetup]
    public void Setup()
    {
        source = OfficialCppBenchmarkData.CreateLong(Case.Length, Case.Similar, true);
        target = OfficialCppBenchmarkData.CreateLong(Case.Length, Case.Similar, false);
    }

    [Benchmark]
    public int Execute()
    {
        return Levenshtein.Distance(source, target, LevenshteinWeights.Default, Case.ScoreCutoff);
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLevenshteinWeightedBenchmarks
{
    private readonly string similar = "aaaaa aaaaa";
    private readonly string different = "bbbbb bbbbb";

    [Benchmark]
    public int WeightedDistanceSimilar()
    {
        return Levenshtein.Distance(similar, similar);
    }

    [Benchmark]
    public int WeightedDistanceDifferent()
    {
        return Levenshtein.Distance(similar, different);
    }

    [Benchmark]
    public double WeightedNormalizedDistanceSimilar()
    {
        return Levenshtein.NormalizedDistance(similar, similar);
    }

    [Benchmark]
    public double WeightedNormalizedDistanceDifferent()
    {
        return Levenshtein.NormalizedDistance(similar, different);
    }
}

public abstract class OfficialLevenshteinMatrixBenchmarks
{
    protected string[] Sources { get; private set; } = [];
    protected string[] Targets { get; private set; } = [];

    [Params(8, 16, 32, 64)]
    public int Length { get; set; }

    [GlobalSetup]
    public virtual void Setup()
    {
        Sources = OfficialCppBenchmarkData.GenerateMany(256, Length, 2200 + Length);
        Targets = OfficialCppBenchmarkData.GenerateMany(10000, Length, 2300 + Length);
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLevenshteinStaticBenchmarks : OfficialLevenshteinMatrixBenchmarks
{
    [Benchmark]
    public long Execute()
    {
        long total = 0;
        for (int targetIndex = 0; targetIndex < Targets.Length; targetIndex++)
        {
            for (int sourceIndex = 0; sourceIndex < Sources.Length; sourceIndex++)
            {
                total += Levenshtein.Distance(Sources[sourceIndex], Targets[targetIndex]);
            }
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLevenshteinCachedBenchmarks : OfficialLevenshteinMatrixBenchmarks
{
    [Benchmark]
    public long Execute()
    {
        long total = 0;
        for (int sourceIndex = 0; sourceIndex < Sources.Length; sourceIndex++)
        {
            CachedLevenshtein scorer = new(Sources[sourceIndex]);
            for (int targetIndex = 0; targetIndex < Targets.Length; targetIndex++)
            {
                total += scorer.Similarity(Targets[targetIndex]);
            }
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialLevenshteinSimdBenchmarks
{
    private string[] sources = [];
    private string[] targets = [];
    private int[] results = [];

    [Params(8, 16, 32, 64)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        sources = OfficialCppBenchmarkData.GenerateMany(64, Length, 2400 + Length);
        targets = OfficialCppBenchmarkData.GenerateMany(10000, Length, 2500 + Length);
        results = new int[sources.Length];
    }

    [Benchmark]
    public long Execute()
    {
        MultiLevenshtein scorer = new(sources);
        long total = 0;
        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            scorer.Similarities(targets[targetIndex], results);
            total += results[0];
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialJaroLongBenchmarks
{
    private string source = string.Empty;
    private string target = string.Empty;

    [ParamsSource(nameof(Cases))]
    public OfficialLongCase Case { get; set; } = null!;

    public static IEnumerable<OfficialLongCase> Cases => OfficialCppBenchmarkData.StandardLongCases("Jaro");

    [GlobalSetup]
    public void Setup()
    {
        source = OfficialCppBenchmarkData.CreateLong(Case.Length, Case.Similar, true);
        target = OfficialCppBenchmarkData.CreateLong(Case.Length, Case.Similar, false);
    }

    [Benchmark]
    public double Execute()
    {
        return Jaro.Similarity(source, target);
    }
}

public abstract class OfficialJaroMatrixBenchmarks
{
    protected string[] Sources { get; private set; } = [];
    protected string[] Targets { get; private set; } = [];

    [ParamsSource(nameof(Cases))]
    public OfficialJaroLengthCase Case { get; set; } = null!;

    public static IEnumerable<OfficialJaroLengthCase> Cases => OfficialCppBenchmarkData.JaroLengths();

    [GlobalSetup]
    public virtual void Setup()
    {
        Sources = OfficialCppBenchmarkData.GenerateMany(256, Case.SourceLength, 2600 + Case.SourceLength + Case.TargetLength);
        Targets = OfficialCppBenchmarkData.GenerateMany(10000, Case.TargetLength, 2700 + Case.SourceLength + Case.TargetLength);
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialJaroStaticBenchmarks : OfficialJaroMatrixBenchmarks
{
    [Benchmark]
    public double Execute()
    {
        double total = 0.0;
        for (int targetIndex = 0; targetIndex < Targets.Length; targetIndex++)
        {
            for (int sourceIndex = 0; sourceIndex < Sources.Length; sourceIndex++)
            {
                total += Jaro.Similarity(Sources[sourceIndex], Targets[targetIndex]);
            }
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialJaroCachedBenchmarks : OfficialJaroMatrixBenchmarks
{
    [Benchmark]
    public double Execute()
    {
        double total = 0.0;
        for (int sourceIndex = 0; sourceIndex < Sources.Length; sourceIndex++)
        {
            CachedJaro scorer = new(Sources[sourceIndex]);
            for (int targetIndex = 0; targetIndex < Targets.Length; targetIndex++)
            {
                total += scorer.Similarity(Targets[targetIndex]);
            }
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialJaroSimdBenchmarks
{
    private string[] sources = [];
    private string[] targets = [];
    private double[] results = [];

    [ParamsSource(nameof(Cases))]
    public OfficialJaroLengthCase Case { get; set; } = null!;

    public static IEnumerable<OfficialJaroLengthCase> Cases => OfficialCppBenchmarkData.JaroLengths();

    [GlobalSetup]
    public void Setup()
    {
        sources = OfficialCppBenchmarkData.GenerateMany(64, Case.SourceLength, 2800 + Case.SourceLength + Case.TargetLength);
        targets = OfficialCppBenchmarkData.GenerateMany(10000, Case.TargetLength, 2900 + Case.SourceLength + Case.TargetLength);
        results = new double[sources.Length];
    }

    [Benchmark]
    public double Execute()
    {
        MultiJaro scorer = new(sources);
        double total = 0.0;
        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            scorer.Similarities(targets[targetIndex], results);
            total += results[0];
        }
        return total;
    }
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class OfficialFuzzBenchmarks
{
    private readonly string similar = "aaaaa aaaaa";
    private readonly string different = "bbbbb bbbbb";
    private readonly string differentLengthSource = "aaaaa b";
    private readonly string differentLengthTarget = "bbbbb bbbbbbbbb";

    [Benchmark] public double Ratio1() => Fuzz.Ratio(similar, similar);
    [Benchmark] public double Ratio2() => Fuzz.Ratio(similar, different);
    [Benchmark] public double PartialRatio1() => Fuzz.PartialRatio(similar, similar);
    [Benchmark] public double PartialRatio2() => Fuzz.PartialRatio(similar, different);
    [Benchmark] public double TokenSort1() => Fuzz.TokenSortRatio(similar, similar);
    [Benchmark] public double TokenSort2() => Fuzz.TokenSortRatio(similar, different);
    [Benchmark] public double PartialTokenSort1() => Fuzz.PartialTokenSortRatio(similar, similar);
    [Benchmark] public double PartialTokenSort2() => Fuzz.PartialTokenSortRatio(similar, different);
    [Benchmark] public double TokenSet1() => Fuzz.TokenSetRatio(similar, similar);
    [Benchmark] public double TokenSet2() => Fuzz.TokenSetRatio(similar, different);
    [Benchmark] public double PartialTokenSet1() => Fuzz.PartialTokenSetRatio(similar, similar);
    [Benchmark] public double PartialTokenSet2() => Fuzz.PartialTokenSetRatio(similar, different);
    [Benchmark] public double Token1() => Fuzz.TokenRatio(similar, similar);
    [Benchmark] public double Token2() => Fuzz.TokenRatio(similar, different);
    [Benchmark] public double PartialToken1() => Fuzz.PartialTokenRatio(similar, similar);
    [Benchmark] public double PartialToken2() => Fuzz.PartialTokenRatio(similar, different);
    [Benchmark] public double WRatio1() => Fuzz.WRatio(similar, similar);
    [Benchmark] public double WRatio2() => Fuzz.WRatio(differentLengthSource, differentLengthTarget);
    [Benchmark] public double WRatio3() => Fuzz.WRatio(similar, different);
}
