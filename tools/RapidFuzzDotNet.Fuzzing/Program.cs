using System.Globalization;
using System.Text.Json;
using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;
using RapidFuzzDotNet.Tests;

namespace RapidFuzzDotNet.Fuzzing;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            FuzzOptions options = FuzzOptions.Parse(args);
            FuzzRunner runner = new(options);
            runner.Run();
            Console.WriteLine($"Completed {options.Iterations} iterations with seed {options.Seed} for {options.Target}, {options.SequenceType}, {options.Profile.Name}.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}

internal sealed class FuzzRunner
{
    private static readonly JsonSerializerOptions ReproductionJsonOptions = new() { WriteIndented = true };

    private static readonly TargetSeed[] TargetSeeds =
    [
        new("lcs_similarity", 101),
        new("levenshtein_distance", 211),
        new("levenshtein_editops", 307),
        new("indel_distance", 401),
        new("indel_editops", 503),
        new("osa_distance", 601),
        new("damerau_levenshtein_distance", 701),
        new("jaro_similarity", 809),
        new("partial_ratio", 907)
    ];

    private readonly FuzzOptions options;
    private readonly Random random;

    public FuzzRunner(FuzzOptions options)
    {
        this.options = options;
        random = new Random(options.Seed);
    }

    public void Run()
    {
        ValidateFixedCases();
        ValidateTargetSeedCases();
        ValidateLongProfileCases();
        ValidateTextCases();

        for (int iteration = 0; iteration < options.Iterations; iteration++)
        {
            int[] first = CreateSequence(random, options.MaximumLength);
            int[] second = iteration % 2 == 0
                ? Mutate(first, random)
                : CreateSequence(random, options.MaximumLength);
            ValidateWithReproduction(first, second);
            ValidateWithReproduction(second, first);

            if (options.Profile.RandomRepeatedFrequency > 0 && iteration % options.Profile.RandomRepeatedFrequency == 0)
            {
                ValidateWithReproduction(
                    Multiply(first, options.Profile.RepeatedCaseMultiplier),
                    Multiply(second, options.Profile.RepeatedCaseMultiplier));
            }
        }
    }

    private void ValidateFixedCases()
    {
        ValidateWithReproduction([], []);
        ValidateWithReproduction([], [1, 2, 3]);
        ValidateWithReproduction([1, 2, 3], []);
        ValidateWithReproduction([1], [1]);
        ValidateWithReproduction([1], [2]);
        ValidateWithReproduction(Multiply([1, 2, 3, 4], 32), Multiply([1, 3, 4, 5], 32));
        ValidateWithReproduction(Multiply([1, 2, 1, 2, 3], 24), Multiply([2, 1, 2, 4, 3], 24));
        ValidateWithReproduction(Enumerable.Range(0, 63).Select(value => value % 9).ToArray(), Enumerable.Range(0, 63).Select(value => (value + 1) % 9).ToArray());
        ValidateWithReproduction(Enumerable.Range(0, 64).Select(value => value % 9).ToArray(), Enumerable.Range(0, 64).Select(value => (value + 1) % 9).ToArray());
        ValidateWithReproduction(Enumerable.Range(0, 65).Select(value => value % 9).ToArray(), Enumerable.Range(0, 65).Select(value => (value + 1) % 9).ToArray());
    }

    private void ValidateTargetSeedCases()
    {
        for (int targetIndex = 0; targetIndex < TargetSeeds.Length; targetIndex++)
        {
            TargetSeed targetSeed = TargetSeeds[targetIndex];

            if (!options.Includes(targetSeed.Target))
            {
                continue;
            }

            Random seededRandom = new(options.Seed + targetSeed.SeedOffset);

            for (int iteration = 0; iteration < options.Profile.SeedCaseIterations; iteration++)
            {
                int[] first = CreateSequence(seededRandom, options.MaximumLength);
                int[] second = Mutate(first, seededRandom);
                ValidateWithReproduction(targetSeed.Target, first, second);
                ValidateWithReproduction(targetSeed.Target, second, first);
            }
        }
    }

    private void ValidateLongProfileCases()
    {
        if (options.Profile.Name != "long")
        {
            return;
        }

        int multiplier = options.Profile.LongCaseMultiplier;
        ValidateIfIncluded("levenshtein_editops", Multiply([1, 2, 3, 4, 5], multiplier), Multiply([1, 3, 2, 4, 6], multiplier));
        ValidateIfIncluded("indel_editops", Multiply([2, 4, 6, 8, 10], multiplier), Multiply([2, 6, 4, 8, 12], multiplier));
        ValidateIfIncluded("partial_ratio", Multiply([1, 2, 3, 4, 5, 6, 7, 8], multiplier), Multiply([0, 1, 2, 3, 4, 6, 7, 8, 9], multiplier));
        ValidateIfIncluded("jaro_similarity", Multiply([1, 1, 2, 3, 5, 8], multiplier), Multiply([1, 2, 1, 3, 8, 5], multiplier));
        ValidateIfIncluded("damerau_levenshtein_distance", Multiply([1, 2, 3, 4], multiplier), Multiply([1, 3, 2, 5], multiplier));
    }

    private void ValidateTextCases()
    {
        if (options.Includes("token_spans"))
        {
            ReadOnlySpan<char> first = " alpha  beta beta gamma ".AsSpan();
            ReadOnlySpan<char> second = "gamma alpha delta beta".AsSpan();
            RequireClose(Fuzz.TokenSortRatio(first, second), new CachedTokenSortRatio(first).Similarity(second), "token_spans");
            RequireClose(Fuzz.TokenSetRatio(first, second), new CachedTokenSetRatio(first).Similarity(second), "token_spans");
            RequireClose(Fuzz.TokenRatio(first, second), new CachedTokenRatio(first).Similarity(second), "token_spans");
            RequireClose(Fuzz.QRatio(first, second), new CachedQRatio(first).Similarity(second), "token_spans");
            RequireClose(Fuzz.WRatio(first.ToString(), second.ToString()), new CachedWRatio(first).Similarity(second), "token_spans");
        }

        if (options.Includes("process"))
        {
            string[] choices = ["new york mets", "new york yankees", "atlanta braves"];
            ExtractedResult<string>? result = Process.ExtractOne("new york mets", choices);

            if (result is null || result.Index != 0 || result.Score != 100.0)
            {
                throw new InvalidOperationException("process mismatch");
            }
        }
    }

    private void ValidateIfIncluded(string target, int[] first, int[] second)
    {
        if (options.Includes(target))
        {
            ValidateWithReproduction(target, first, second);
            ValidateWithReproduction(target, second, first);
        }
    }

    private void ValidateWithReproduction(int[] first, int[] second) => ValidateWithReproduction(options.Target, first, second);

    private void ValidateWithReproduction(string target, int[] first, int[] second)
    {
        try
        {
            ValidateTypes(target, first, second);
        }
        catch (Exception exception)
        {
            (int[] First, int[] Second) minimized = Minimize(target, first, second);
            WriteReproduction(target, minimized.First, minimized.Second, exception.Message);
            throw new InvalidOperationException(
                $"Mismatch for {target}/{options.SequenceType}: first=[{string.Join(',', minimized.First)}], second=[{string.Join(',', minimized.Second)}], {exception.Message}",
                exception);
        }
    }

    private void ValidateTypes(string target, int[] first, int[] second)
    {
        if (options.IncludesType("int"))
        {
            ValidateTarget(target, first, second);
        }

        if (options.IncludesType("byte"))
        {
            byte[] byteFirst = first.Select(value => (byte)value).ToArray();
            byte[] byteSecond = second.Select(value => (byte)value).ToArray();
            ValidateTarget(target, byteFirst, byteSecond);
        }

        if (options.IncludesType("char"))
        {
            char[] charFirst = first.Select(value => (char)('a' + value)).ToArray();
            char[] charSecond = second.Select(value => (char)('a' + value)).ToArray();
            ValidateTarget(target, charFirst, charSecond);
        }

        if (options.IncludesType("record"))
        {
            FuzzValue[] recordFirst = first.Select(value => new FuzzValue(value)).ToArray();
            FuzzValue[] recordSecond = second.Select(value => new FuzzValue(value)).ToArray();
            ValidateTarget(target, recordFirst, recordSecond);
        }

        if (options.IncludesType("cross"))
        {
            byte[] byteFirst = first.Select(value => (byte)value).ToArray();
            char[] charFirst = first.Select(value => (char)('a' + value)).ToArray();
            FuzzValue[] recordFirst = first.Select(value => new FuzzValue(value)).ToArray();
            ValidateCrossTarget(target, byteFirst, first, second, new ByteIntComparer());
            ValidateCrossTarget(target, charFirst, first, second, new CharIntComparer());
            ValidateCrossTarget(target, recordFirst, first, second, new FuzzValueIntComparer());
        }
    }

    private static void ValidateCrossTarget<TSource>(
        string target,
        TSource[] first,
        int[] canonicalFirst,
        int[] second,
        ISequenceEqualityComparer<TSource, int> comparer)
        where TSource : notnull, IEquatable<TSource>
    {
        if (target == "all" || target == "lcs_similarity")
        {
            RequireEqual(ReferenceScorers.LcsSimilarity<int>(canonicalFirst, second), LcsSeq.Similarity(first, second, comparer), "cross_lcs_similarity");
        }

        if (target == "all" || target == "levenshtein_distance")
        {
            RequireEqual(ReferenceScorers.LevenshteinDistance<int>(canonicalFirst, second), Levenshtein.Distance(first, second, comparer), "cross_levenshtein_distance");
        }

        if (target == "all" || target == "levenshtein_editops")
        {
            EditOperations editops = Levenshtein.Editops(first, second, comparer);
            RequireSequence(second, editops.ApplyTo<int>(canonicalFirst, second), "cross_levenshtein_editops");
            RequireSequence(second, editops.ToOpcodes().ApplyTo<int>(canonicalFirst, second), "cross_levenshtein_opcodes");
        }

        if (target == "all" || target == "indel_distance")
        {
            RequireEqual(ReferenceScorers.IndelDistance<int>(canonicalFirst, second), Indel.Distance(first, second, comparer), "cross_indel_distance");
        }

        if (target == "all" || target == "indel_editops")
        {
            EditOperations editops = Indel.Editops(first, second, comparer);
            RequireSequence(second, editops.ApplyTo<int>(canonicalFirst, second), "cross_indel_editops");
            RequireSequence(second, editops.ToOpcodes().ApplyTo<int>(canonicalFirst, second), "cross_indel_opcodes");
        }

        if (target == "all" || target == "osa_distance")
        {
            RequireEqual(ReferenceScorers.OsaDistance<int>(canonicalFirst, second), Osa.Distance(first, second, comparer), "cross_osa_distance");
        }

        if (target == "all" || target == "damerau_levenshtein_distance")
        {
            RequireEqual(
                ReferenceScorers.DamerauLevenshteinDistance<int>(canonicalFirst, second),
                DamerauLevenshtein.Distance(first, second, comparer),
                "cross_damerau_levenshtein_distance");
        }

        if (target == "all" || target == "jaro_similarity")
        {
            RequireClose(ReferenceScorers.JaroSimilarity<int>(canonicalFirst, second), Jaro.Similarity(first, second, comparer), "cross_jaro_similarity");
        }

        if (target == "all" || target == "partial_ratio")
        {
            RequireClose(ReferenceScorers.PartialRatio<int>(canonicalFirst, second), Fuzz.PartialRatio(first, second, comparer), "cross_partial_ratio");
        }

        if (target == "all" || target == "cached")
        {
            ValidateCrossCached(first, canonicalFirst, second, comparer);
        }

        if (target == "all" || target == "multi")
        {
            ValidateCrossMulti(first, second, comparer);
        }
    }

    private static void ValidateCrossCached<TSource>(
        TSource[] first,
        int[] canonicalFirst,
        int[] second,
        ISequenceEqualityComparer<TSource, int> comparer)
        where TSource : notnull, IEquatable<TSource>
    {
        RequireEqual(Levenshtein.Distance<int>(canonicalFirst, second), new CachedLevenshtein<TSource>(first).Distance(second, comparer), "cross_cached_levenshtein");
        RequireEqual(Indel.Distance<int>(canonicalFirst, second), new CachedIndel<TSource>(first).Distance(second, comparer), "cross_cached_indel");
        RequireEqual(LcsSeq.Distance<int>(canonicalFirst, second), new CachedLcsSeq<TSource>(first).Distance(second, comparer), "cross_cached_lcs");
        RequireEqual(Osa.Distance<int>(canonicalFirst, second), new CachedOsa<TSource>(first).Distance(second, comparer), "cross_cached_osa");
        RequireEqual(DamerauLevenshtein.Distance<int>(canonicalFirst, second), new CachedDamerauLevenshtein<TSource>(first).Distance(second, comparer), "cross_cached_damerau");
        RequireClose(Jaro.Similarity<int>(canonicalFirst, second), new CachedJaro<TSource>(first).Similarity(second, comparer), "cross_cached_jaro");
        RequireClose(JaroWinkler.Similarity<int>(canonicalFirst, second), new CachedJaroWinkler<TSource>(first).Similarity(second, comparer), "cross_cached_jaro_winkler");
        RequireClose(Fuzz.Ratio<int>(canonicalFirst, second), new CachedRatio<TSource>(first).Similarity(second, comparer), "cross_cached_ratio");
        RequireClose(Fuzz.PartialRatio<int>(canonicalFirst, second), new CachedPartialRatio<TSource>(first).Similarity(second, comparer), "cross_cached_partial_ratio");
        RequireClose(Fuzz.QRatio<int>(canonicalFirst, second), new CachedQRatio<TSource>(first).Similarity(second, comparer), "cross_cached_qratio");
    }

    private static void ValidateCrossMulti<TSource>(
        TSource[] first,
        int[] second,
        ISequenceEqualityComparer<TSource, int> comparer)
        where TSource : notnull, IEquatable<TSource>
    {
        TSource[][] sources = [first, first.ToArray(), []];
        RequireSequence(
            sources.Select(source => Levenshtein.Distance(source, second, comparer)).ToArray(),
            new MultiLevenshtein<TSource>(sources).Distances(second, comparer),
            "cross_multi_levenshtein");
        RequireSequence(
            sources.Select(source => Indel.Distance(source, second, comparer)).ToArray(),
            new MultiIndel<TSource>(sources).Distances(second, comparer),
            "cross_multi_indel");
        RequireSequence(
            sources.Select(source => LcsSeq.Similarity(source, second, comparer)).ToArray(),
            new MultiLcsSeq<TSource>(sources).Similarities(second, comparer),
            "cross_multi_lcs");
        RequireSequence(
            sources.Select(source => Osa.Distance(source, second, comparer)).ToArray(),
            new MultiOsa<TSource>(sources).Distances(second, comparer),
            "cross_multi_osa");
        RequireDoubleSequence(
            sources.Select(source => Jaro.Similarity(source, second, comparer)).ToArray(),
            new MultiJaro<TSource>(sources).Similarities(second, comparer),
            "cross_multi_jaro");
        RequireDoubleSequence(
            sources.Select(source => JaroWinkler.Similarity(source, second, comparer)).ToArray(),
            new MultiJaroWinkler<TSource>(sources).Similarities(second, comparer),
            "cross_multi_jaro_winkler");
        RequireDoubleSequence(
            sources.Select(source => Fuzz.Ratio(source, second, comparer)).ToArray(),
            new MultiRatio<TSource>(sources).Similarities(second, comparer),
            "cross_multi_ratio");
        RequireDoubleSequence(
            sources.Select(source => Fuzz.QRatio(source, second, comparer)).ToArray(),
            new MultiQRatio<TSource>(sources).Similarities(second, comparer),
            "cross_multi_qratio");
    }

    private static void ValidateTarget<T>(string target, T[] first, T[] second)
        where T : notnull, IEquatable<T>
    {
        if (target == "all" || target == "lcs_similarity")
        {
            RequireEqual(ReferenceScorers.LcsSimilarity<T>(first, second), LcsSeq.Similarity<T>(first, second), "lcs_similarity");
        }

        if (target == "all" || target == "levenshtein_distance")
        {
            RequireEqual(ReferenceScorers.LevenshteinDistance<T>(first, second), Levenshtein.Distance<T>(first, second), "levenshtein_distance");
        }

        if (target == "all" || target == "levenshtein_editops")
        {
            EditOperations editops = Levenshtein.Editops<T>(first, second);
            RequireSequence(second, editops.ApplyTo<T>(first, second), "levenshtein_editops");
            RequireSequence(second, editops.ToOpcodes().ApplyTo<T>(first, second), "levenshtein_opcodes");
        }

        if (target == "all" || target == "indel_distance")
        {
            RequireEqual(ReferenceScorers.IndelDistance<T>(first, second), Indel.Distance<T>(first, second), "indel_distance");
        }

        if (target == "all" || target == "indel_editops")
        {
            EditOperations editops = Indel.Editops<T>(first, second);
            RequireSequence(second, editops.ApplyTo<T>(first, second), "indel_editops");
            RequireSequence(second, editops.ToOpcodes().ApplyTo<T>(first, second), "indel_opcodes");
        }

        if (target == "all" || target == "osa_distance")
        {
            RequireEqual(ReferenceScorers.OsaDistance<T>(first, second), Osa.Distance<T>(first, second), "osa_distance");
        }

        if (target == "all" || target == "damerau_levenshtein_distance")
        {
            RequireEqual(
                ReferenceScorers.DamerauLevenshteinDistance<T>(first, second),
                DamerauLevenshtein.Distance<T>(first, second),
                "damerau_levenshtein_distance");
        }

        if (target == "all" || target == "jaro_similarity")
        {
            RequireClose(ReferenceScorers.JaroSimilarity<T>(first, second), Jaro.Similarity<T>(first, second), "jaro_similarity");
        }

        if (target == "all" || target == "partial_ratio")
        {
            RequireClose(ReferenceScorers.PartialRatio<T>(first, second), Fuzz.PartialRatio<T>(first, second), "partial_ratio");
        }

        if (target == "all" || target == "cached")
        {
            ValidateCached(first, second);
        }

        if (target == "all" || target == "multi")
        {
            ValidateMulti(first, second);
        }

        if (target == "all" || target == "slices")
        {
            ValidateSlices(first, second);
        }
    }

    private static void ValidateCached<T>(T[] first, T[] second)
        where T : notnull, IEquatable<T>
    {
        RequireEqual(Levenshtein.Distance<T>(first, second), new CachedLevenshtein<T>(first).Distance(second), "cached_levenshtein");
        RequireEqual(Indel.Distance<T>(first, second), new CachedIndel<T>(first).Distance(second), "cached_indel");
        RequireEqual(LcsSeq.Distance<T>(first, second), new CachedLcsSeq<T>(first).Distance(second), "cached_lcs");
        RequireEqual(Osa.Distance<T>(first, second), new CachedOsa<T>(first).Distance(second), "cached_osa");
        RequireClose(Jaro.Similarity<T>(first, second), new CachedJaro<T>(first).Similarity(second), "cached_jaro");
        RequireClose(JaroWinkler.Similarity<T>(first, second), new CachedJaroWinkler<T>(first).Similarity(second), "cached_jaro_winkler");
        RequireClose(Fuzz.QRatio<T>(first, second), new CachedQRatio<T>(first).Similarity(second), "cached_qratio");
    }

    private static void ValidateMulti<T>(T[] first, T[] second)
        where T : notnull, IEquatable<T>
    {
        T[][] sources = [first, second, []];
        RequireSequence(
            sources.Select(source => Levenshtein.Distance<T>(source, second)).ToArray(),
            new MultiLevenshtein<T>(sources).Distances(second),
            "multi_levenshtein");
        RequireSequence(
            sources.Select(source => Osa.Distance<T>(source, second)).ToArray(),
            new MultiOsa<T>(sources).Distances(second),
            "multi_osa");
        RequireDoubleSequence(
            sources.Select(source => Fuzz.Ratio<T>(source, second)).ToArray(),
            new MultiRatio<T>(sources).Similarities(second),
            "multi_ratio");
        RequireDoubleSequence(
            sources.Select(source => Fuzz.QRatio<T>(source, second)).ToArray(),
            new MultiQRatio<T>(sources).Similarities(second),
            "multi_qratio");
    }

    private static void ValidateSlices<T>(T[] first, T[] second)
        where T : notnull, IEquatable<T>
    {
        EditOperations editops = Levenshtein.Editops<T>(first, second);
        EditOperations fullSlice = editops.Slice(int.MinValue, int.MaxValue);
        RequireSequence(second, fullSlice.ApplyTo<T>(first, second), "editops_slice");
        Opcodes opcodes = editops.ToOpcodes();
        RequireSequence(second, opcodes.Slice(int.MinValue, int.MaxValue).ApplyTo<T>(first, second), "opcodes_slice");
        editops.RemoveSlice(0, editops.Count, 2);
    }

    private (int[] First, int[] Second) Minimize(string target, int[] first, int[] second)
    {
        int[] minimizedFirst = first;
        int[] minimizedSecond = second;
        bool changed = true;

        while (changed)
        {
            changed = TryRemoveOne(target, ref minimizedFirst, minimizedSecond)
                || TryRemoveOne(target, ref minimizedSecond, minimizedFirst, true);
        }

        return (minimizedFirst, minimizedSecond);
    }

    private bool TryRemoveOne(string target, ref int[] sequence, int[] other, bool reverse = false)
    {
        for (int index = 0; index < sequence.Length; index++)
        {
            int[] candidate = sequence[..index].Concat(sequence[(index + 1)..]).ToArray();
            int[] first = reverse ? other : candidate;
            int[] second = reverse ? candidate : other;

            try
            {
                ValidateTypes(target, first, second);
            }
            catch
            {
                sequence = candidate;
                return true;
            }
        }

        return false;
    }

    private void WriteReproduction(string target, int[] first, int[] second, string message)
    {
        if (options.ReproductionPath is null)
        {
            return;
        }

        FuzzReproduction reproduction = new(options.Seed, target, options.SequenceType, first, second, message);
        string json = JsonSerializer.Serialize(reproduction, ReproductionJsonOptions);
        File.WriteAllText(options.ReproductionPath, json);
    }

    private int[] CreateSequence(Random generator, int maximumLength)
    {
        int length = generator.Next(0, maximumLength + 1);
        int[] result = new int[length];

        for (int index = 0; index < result.Length; index++)
        {
            result[index] = generator.Next(options.AlphabetSize);
        }

        return result;
    }

    private int[] Mutate(int[] source, Random generator)
    {
        return generator.Next(6) switch
        {
            0 => Insert(source, generator),
            1 => Delete(source, generator),
            2 => Swap(source, generator),
            3 => Duplicate(source, generator),
            4 => Splice(source, generator),
            _ => Repeat(source, generator)
        };
    }

    private int[] Insert(int[] source, Random generator)
    {
        int position = generator.Next(source.Length + 1);
        int[] result = new int[source.Length + 1];
        source.AsSpan(0, position).CopyTo(result);
        result[position] = generator.Next(options.AlphabetSize);
        source.AsSpan(position).CopyTo(result.AsSpan(position + 1));
        return result;
    }

    private static int[] Delete(int[] source, Random generator)
    {
        if (source.Length == 0)
        {
            return source;
        }

        int position = generator.Next(source.Length);
        return source[..position].Concat(source[(position + 1)..]).ToArray();
    }

    private static int[] Swap(int[] source, Random generator)
    {
        int[] result = source.ToArray();

        if (result.Length > 1)
        {
            int position = generator.Next(result.Length - 1);
            (result[position], result[position + 1]) = (result[position + 1], result[position]);
        }

        return result;
    }

    private static int[] Duplicate(int[] source, Random generator)
    {
        if (source.Length == 0)
        {
            return source;
        }

        int start = generator.Next(source.Length);
        int length = generator.Next(1, source.Length - start + 1);
        return source.Concat(source.AsSpan(start, length).ToArray()).ToArray();
    }

    private int[] Splice(int[] source, Random generator)
    {
        int start = generator.Next(source.Length + 1);
        int stop = generator.Next(start, source.Length + 1);
        int replacementLength = generator.Next(0, Math.Max(2, options.MaximumLength / 4));
        int[] replacement = CreateSequence(generator, replacementLength);
        return source[..start].Concat(replacement).Concat(source[stop..]).ToArray();
    }

    private static int[] Repeat(int[] source, Random generator)
    {
        if (source.Length == 0)
        {
            return source;
        }

        return Multiply(source, generator.Next(2, 4));
    }

    private static int[] Multiply(int[] source, int count)
    {
        int[] result = new int[source.Length * count];

        for (int index = 0; index < count; index++)
        {
            source.CopyTo(result.AsSpan(index * source.Length, source.Length));
        }

        return result;
    }

    private static void RequireEqual(int expected, int actual, string target)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{target} expected {expected} but got {actual}");
        }
    }

    private static void RequireClose(double expected, double actual, string target)
    {
        double tolerance = Math.Max(Math.Abs(expected), Math.Abs(actual)) * 0.000001;

        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"{target} expected {expected} but got {actual}");
        }
    }

    private static void RequireSequence<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, string target)
        where T : IEquatable<T>
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException($"{target} produced a sequence mismatch");
        }
    }

    private static void RequireDoubleSequence(ReadOnlySpan<double> expected, ReadOnlySpan<double> actual, string target)
    {
        if (expected.Length != actual.Length)
        {
            throw new InvalidOperationException($"{target} produced a result length mismatch");
        }

        for (int index = 0; index < expected.Length; index++)
        {
            RequireClose(expected[index], actual[index], target);
        }
    }
}

internal readonly record struct FuzzValue(int Value);

internal sealed class ByteIntComparer : ISequenceEqualityComparer<byte, int>
{
    public bool Equals(byte left, int right) => left == right;
}

internal sealed class CharIntComparer : ISequenceEqualityComparer<char, int>
{
    public bool Equals(char left, int right) => left - 'a' == right;
}

internal sealed class FuzzValueIntComparer : ISequenceEqualityComparer<FuzzValue, int>
{
    public bool Equals(FuzzValue left, int right) => left.Value == right;
}

internal readonly record struct FuzzReproduction(
    int Seed,
    string Target,
    string SequenceType,
    int[] First,
    int[] Second,
    string Message);

internal readonly record struct TargetSeed(string Target, int SeedOffset);

internal readonly record struct FuzzProfile(
    string Name,
    int SeedCaseIterations,
    int RepeatedCaseMultiplier,
    int LongCaseMultiplier,
    int RandomRepeatedFrequency)
{
    public static FuzzProfile Parse(string value)
    {
        return value switch
        {
            "quick" => new FuzzProfile("quick", 1, 2, 16, 0),
            "standard" => new FuzzProfile("standard", 2, 4, 32, 7),
            "long" => new FuzzProfile("long", 4, 8, 64, 3),
            _ => throw new ArgumentException($"Unknown fuzz profile '{value}'.", nameof(value))
        };
    }
}

internal readonly record struct FuzzOptions(
    int Seed,
    int Iterations,
    int MaximumLength,
    int AlphabetSize,
    string Target,
    string SequenceType,
    FuzzProfile Profile,
    string? ReproductionPath)
{
    public static FuzzOptions Parse(string[] args)
    {
        int seed = 1337;
        int iterations = 128;
        int maximumLength = 24;
        int alphabetSize = 8;
        string target = "all";
        string sequenceType = "all";
        string profileName = "standard";
        string? reproductionPath = null;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            if (argument == "--seed" && index + 1 < args.Length)
            {
                seed = int.Parse(args[++index], CultureInfo.InvariantCulture);
            }
            else if (argument == "--iterations" && index + 1 < args.Length)
            {
                iterations = int.Parse(args[++index], CultureInfo.InvariantCulture);
            }
            else if (argument == "--max-length" && index + 1 < args.Length)
            {
                maximumLength = int.Parse(args[++index], CultureInfo.InvariantCulture);
            }
            else if (argument == "--alphabet" && index + 1 < args.Length)
            {
                alphabetSize = int.Parse(args[++index], CultureInfo.InvariantCulture);
            }
            else if (argument == "--target" && index + 1 < args.Length)
            {
                target = args[++index];
            }
            else if (argument == "--type" && index + 1 < args.Length)
            {
                sequenceType = args[++index];
            }
            else if (argument == "--profile" && index + 1 < args.Length)
            {
                profileName = args[++index];
            }
            else if (argument == "--repro-json" && index + 1 < args.Length)
            {
                reproductionPath = args[++index];
            }
            else
            {
                throw new ArgumentException($"Unknown or incomplete argument '{argument}'.", nameof(args));
            }
        }

        ValidateTarget(target);
        ValidateSequenceType(sequenceType);

        if (iterations < 0 || maximumLength < 0 || alphabetSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(args), "Iterations and maximum length must be non-negative and alphabet must be positive.");
        }

        return new FuzzOptions(
            seed,
            iterations,
            maximumLength,
            alphabetSize,
            target,
            sequenceType,
            FuzzProfile.Parse(profileName),
            reproductionPath);
    }

    public bool Includes(string target) => Target == "all" || Target == target;

    public bool IncludesType(string sequenceType) => SequenceType == "all" || SequenceType == sequenceType;

    private static void ValidateTarget(string target)
    {
        if (target is "all"
            or "lcs_similarity"
            or "levenshtein_distance"
            or "levenshtein_editops"
            or "indel_distance"
            or "indel_editops"
            or "osa_distance"
            or "damerau_levenshtein_distance"
            or "jaro_similarity"
            or "partial_ratio"
            or "cached"
            or "multi"
            or "slices"
            or "token_spans"
            or "process")
        {
            return;
        }

        throw new ArgumentException($"Unknown fuzz target '{target}'.", nameof(target));
    }

    private static void ValidateSequenceType(string sequenceType)
    {
        if (sequenceType is "all" or "int" or "byte" or "char" or "record" or "cross")
        {
            return;
        }

        throw new ArgumentException($"Unknown sequence type '{sequenceType}'.", nameof(sequenceType));
    }
}
