using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using RapidFuzz;
using RapidFuzz.Distance;

namespace RapidFuzzDotNet.UpstreamAudit;

public static partial class Program
{
    private const string ExpectedSha = "b5830af53bd1b3c7460a8de1e9f7095df99b3470";

    private static readonly string[] AlgorithmHeaders =
    [
        "DamerauLevenshtein.hpp",
        "Hamming.hpp",
        "Indel.hpp",
        "Jaro.hpp",
        "JaroWinkler.hpp",
        "LCSseq.hpp",
        "Levenshtein.hpp",
        "OSA.hpp",
        "Postfix.hpp",
        "Prefix.hpp"
    ];

    private static readonly Type[] AlgorithmTypes =
    [
        typeof(DamerauLevenshtein),
        typeof(Hamming),
        typeof(Indel),
        typeof(Jaro),
        typeof(JaroWinkler),
        typeof(LcsSeq),
        typeof(Levenshtein),
        typeof(Osa),
        typeof(Postfix),
        typeof(Prefix),
        typeof(Fuzz)
    ];

    private static readonly string[] FixtureFiles =
    [
        "test/tests-common.cpp",
        "test/distance/tests-Levenshtein.cpp",
        "test/distance/tests-Indel.cpp",
        "test/distance/tests-LCSseq.cpp",
        "test/distance/tests-Hamming.cpp",
        "test/distance/tests-OSA.cpp",
        "test/distance/tests-DamerauLevenshtein.cpp",
        "test/distance/tests-Jaro.cpp",
        "test/distance/tests-JaroWinkler.cpp",
        "test/tests-fuzz.cpp"
    ];

    private static readonly string[] FuzzTargets =
    [
        "lcs_similarity",
        "levenshtein_distance",
        "levenshtein_editops",
        "indel_distance",
        "indel_editops",
        "osa_distance",
        "damerau_levenshtein_distance",
        "jaro_similarity",
        "partial_ratio"
    ];

    private static readonly string[] BenchmarkFiles =
    [
        "bench/bench-lcs.cpp",
        "bench/bench-fuzz.cpp",
        "bench/bench-jarowinkler.cpp",
        "bench/bench-levenshtein.cpp"
    ];

    public static int Main(string[] args)
    {
        AuditOptions options = AuditOptions.Parse(args);
        string[] apiBaseline = CreateApiBaseline();

        if (options.WriteApiBaseline)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(options.ApiBaselinePath)!);
            File.WriteAllLines(options.ApiBaselinePath, apiBaseline);
            Console.WriteLine($"Wrote API baseline with {apiBaseline.Length} entries.");
            return 0;
        }

        int failures = 0;
        string upstreamSha = ReadGitSha(options.UpstreamPath);
        failures += Check("SHA", upstreamSha == ExpectedSha, ExpectedSha, upstreamSha);
        failures += CheckAlgorithms(options.UpstreamPath);
        failures += CheckFixtures(options.RootPath, options.UpstreamPath);
        failures += CheckFuzzTargets(options.RootPath, options.UpstreamPath);
        failures += CheckBenchmarks(options.RootPath, options.UpstreamPath);
        failures += CheckApiBaseline(options.ApiBaselinePath, apiBaseline);
        return failures == 0 ? 0 : 1;
    }

    private static int CheckAlgorithms(string upstreamPath)
    {
        bool upstreamHeaders = AlgorithmHeaders.All(header => File.Exists(Path.Combine(upstreamPath, "rapidfuzz", "distance", header)))
            && File.Exists(Path.Combine(upstreamPath, "rapidfuzz", "fuzz.hpp"));
        bool localTypes = AlgorithmTypes.All(type => type.IsPublic && type.IsAbstract && type.IsSealed);
        return Check("algorithms", upstreamHeaders && localTypes, "11", $"headers={upstreamHeaders},types={localTypes}");
    }

    private static int CheckFixtures(string rootPath, string upstreamPath)
    {
        int upstreamCount = 0;

        for (int index = 0; index < FixtureFiles.Length; index++)
        {
            string content = File.ReadAllText(Path.Combine(upstreamPath, NormalizePath(FixtureFiles[index])));
            upstreamCount += TestCaseRegex().Count(content) + SectionRegex().Count(content);
        }

        string localContent = File.ReadAllText(Path.Combine(rootPath, "tests", "RapidFuzzDotNet.Tests", "UpstreamNamedFixtureTests.cs"));
        int localCount = UpstreamMethodRegex().Count(localContent);
        return Check("fixtures", upstreamCount == 64 && localCount == 64, "64/64", $"{upstreamCount}/{localCount}");
    }

    private static int CheckFuzzTargets(string rootPath, string upstreamPath)
    {
        string localContent = File.ReadAllText(Path.Combine(rootPath, "tools", "RapidFuzzDotNet.Fuzzing", "Program.cs"));
        bool valid = true;

        for (int index = 0; index < FuzzTargets.Length; index++)
        {
            string target = FuzzTargets[index];
            string upstreamFile = Path.Combine(upstreamPath, "fuzzing", "fuzz_" + target + ".cpp");
            valid &= File.Exists(upstreamFile) && localContent.Contains('"' + target + '"', StringComparison.Ordinal);
        }

        return Check("fuzz targets", valid, "9/9", valid ? "9/9" : "mismatch");
    }

    private static int CheckBenchmarks(string rootPath, string upstreamPath)
    {
        int upstreamCount = 0;

        for (int index = 0; index < BenchmarkFiles.Length; index++)
        {
            string content = File.ReadAllText(Path.Combine(upstreamPath, NormalizePath(BenchmarkFiles[index])));
            upstreamCount += BenchmarkRegistrationRegex().Count(content);
        }

        string localContent = File.ReadAllText(Path.Combine(rootPath, "benchmarks", "RapidFuzzDotNet.Benchmarks", "Program.cs"));
        Match block = LocalBenchmarkBlockRegex().Match(localContent);
        int localCount = block.Success ? QuotedValueRegex().Count(block.Groups[1].Value) : 0;
        return Check("benchmarks", upstreamCount == 77 && localCount == 77, "77/77", $"{upstreamCount}/{localCount}");
    }

    private static int CheckApiBaseline(string baselinePath, string[] actual)
    {
        if (!File.Exists(baselinePath))
        {
            return Check("API baseline", false, actual.Length.ToString(CultureInfo.InvariantCulture), "missing");
        }

        string[] expected = File.ReadAllLines(baselinePath);
        return Check(
            "API baseline",
            expected.SequenceEqual(actual),
            expected.Length.ToString(CultureInfo.InvariantCulture),
            actual.Length.ToString(CultureInfo.InvariantCulture));
    }

    private static int Check(string name, bool success, string expected, string actual)
    {
        if (success)
        {
            Console.WriteLine($"{name}: ok ({actual})");
            return 0;
        }

        Console.Error.WriteLine($"{name}: expected {expected}, actual {actual}");
        return 1;
    }

    private static string ReadGitSha(string upstreamPath)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(upstreamPath);
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("HEAD");

        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start git.");
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
        }

        return output;
    }

    private static string[] CreateApiBaseline()
    {
        Assembly assembly = typeof(Fuzz).Assembly;
        List<string> lines = [];

        foreach (Type type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add("T " + FormatType(type));

            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(constructor => constructor.ToString(), StringComparer.Ordinal))
            {
                lines.Add("  C " + FormatParameters(constructor.GetParameters()));
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ThenBy(method => method.ToString(), StringComparer.Ordinal))
            {
                string genericArguments = method.IsGenericMethodDefinition
                    ? "<" + string.Join(',', method.GetGenericArguments().Select(argument => argument.Name)) + ">"
                    : string.Empty;
                lines.Add($"  M {FormatType(method.ReturnType)} {method.Name}{genericArguments}{FormatParameters(method.GetParameters())}");
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                lines.Add($"  P {FormatType(property.PropertyType)} {property.Name}");
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                lines.Add($"  F {FormatType(field.FieldType)} {field.Name}");
            }
        }

        return lines.ToArray();
    }

    private static string FormatParameters(ParameterInfo[] parameters)
    {
        return "(" + string.Join(',', parameters.Select(parameter => FormatType(parameter.ParameterType))) + ")";
    }

    private static string FormatType(Type type)
    {
        if (type.IsByRef)
        {
            return FormatType(type.GetElementType()!) + "&";
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        string definitionName = type.GetGenericTypeDefinition().FullName ?? type.Name;
        int marker = definitionName.IndexOf('`', StringComparison.Ordinal);

        if (marker >= 0)
        {
            definitionName = definitionName[..marker];
        }

        return definitionName + "<" + string.Join(',', type.GetGenericArguments().Select(FormatType)) + ">";
    }

    private static string NormalizePath(string path) => path.Replace('/', Path.DirectorySeparatorChar);

    [GeneratedRegex(@"TEST_CASE\s*\(")]
    private static partial Regex TestCaseRegex();

    [GeneratedRegex(@"SECTION\s*\(")]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"public void Upstream\w+\(")]
    private static partial Regex UpstreamMethodRegex();

    [GeneratedRegex(@"(?m)^\s*BENCHMARK(?:_TEMPLATE)?\s*\(")]
    private static partial Regex BenchmarkRegistrationRegex();

    [GeneratedRegex(@"RegistrationNames\s*=\s*\[(.*?)\];", RegexOptions.Singleline)]
    private static partial Regex LocalBenchmarkBlockRegex();

    [GeneratedRegex("\"[^\"]+\"")]
    private static partial Regex QuotedValueRegex();
}

internal readonly record struct AuditOptions(
    string RootPath,
    string UpstreamPath,
    string ApiBaselinePath,
    bool WriteApiBaseline)
{
    public static AuditOptions Parse(string[] args)
    {
        string rootPath = Directory.GetCurrentDirectory();
        string? upstreamPath = null;
        string? apiBaselinePath = null;
        bool writeApiBaseline = false;

        for (int index = 0; index < args.Length; index++)
        {
            if (args[index] == "--root" && index + 1 < args.Length)
            {
                rootPath = Path.GetFullPath(args[++index]);
            }
            else if (args[index] == "--upstream" && index + 1 < args.Length)
            {
                upstreamPath = Path.GetFullPath(args[++index]);
            }
            else if (args[index] == "--api-baseline" && index + 1 < args.Length)
            {
                apiBaselinePath = Path.GetFullPath(args[++index]);
            }
            else if (args[index] == "--write-api-baseline")
            {
                writeApiBaseline = true;
            }
            else
            {
                throw new ArgumentException($"Unknown or incomplete argument '{args[index]}'.", nameof(args));
            }
        }

        upstreamPath ??= Path.Combine(rootPath, "artifacts", "upstream");
        apiBaselinePath ??= Path.Combine(rootPath, "tests", "RapidFuzzDotNet.Tests", "PublicApiBaseline.txt");
        return new AuditOptions(rootPath, upstreamPath, apiBaselinePath, writeApiBaseline);
    }
}
