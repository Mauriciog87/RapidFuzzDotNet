using System.IO.Compression;
using RapidFuzz;
using RapidFuzz.Distance;
using RapidFuzz.Distance.Experimental;

namespace RapidFuzzDotNet.Tests;

public sealed class UpstreamNamedFixtureTests
{
    [Fact]
    public void UpstreamRemoveAffixTestCase()
    {
        string first = "aabbbbaaaa";
        string second = "aaabbbbaaaaa";

        Assert.Equal(2, Prefix.Similarity(first, second));
        Assert.Equal(4, Postfix.Similarity(first, second));
        Assert.Equal(2, Levenshtein.Distance(first, second));
        Assert.Equal(2, Levenshtein.Distance("bbbb", "abbbba"));
    }

    [Fact]
    public void UpstreamLevenshteinTestCase()
    {
        Assert.Equal(3, Levenshtein.Distance("kitten", "sitting"));
        Assert.Equal(0.5714285714285714, Levenshtein.NormalizedSimilarity("kitten", "sitting"), 12);
    }

    [Fact]
    public void UpstreamLevenshteinCalculatesEmptySequenceSection()
    {
        Assert.Equal(0, Levenshtein.Distance("", ""));
        Assert.Equal(3, Levenshtein.Distance("", "abc"));
        Assert.Equal(3, Levenshtein.Distance("abc", ""));
        Assert.Equal(1.0, Levenshtein.NormalizedDistance("", "abc"));
        Assert.Equal(0.0, Levenshtein.NormalizedSimilarity("", "abc"));
    }

    [Fact]
    public void UpstreamLevenshteinCalculatesCorrectDistancesSection()
    {
        Assert.Equal(3, Levenshtein.Distance("kitten", "sitting"));
        Assert.Equal(2, Levenshtein.Distance("book", "back"));
        Assert.Equal(1, Levenshtein.Distance("abc", "ab"));
        Assert.Equal(1, Levenshtein.Distance("ab", "abc"));
        Assert.Equal(0, Levenshtein.Distance("same", "same"));
    }

    [Fact]
    public void UpstreamWeightedLevenshteinCalculatesCorrectDistancesSection()
    {
        LevenshteinWeights weights = new(1, 1, 2);

        Assert.Equal(0, Levenshtein.Distance("aaaa", "aaaa", weights));
        Assert.Equal(1, Levenshtein.Distance("aaaa", "aaa", weights));
        Assert.Equal(2, Levenshtein.Distance("abaa", "baaa", weights));
        Assert.Equal(2, Levenshtein.Distance("aaaa", "aaab", weights));
        Assert.Equal(8, Levenshtein.Distance("aaaa", "bbbb", weights));
    }

    [Fact]
    public void UpstreamWeightedLevenshteinCalculatesCorrectRatiosSection()
    {
        LevenshteinWeights weights = new(1, 1, 2);

        Assert.Equal(1.0, Levenshtein.NormalizedSimilarity("aaaa", "aaaa", weights));
        Assert.Equal(0.8571428571428571, Levenshtein.NormalizedSimilarity("aaaa", "aaa", weights), 12);
        Assert.Equal(0.75, Levenshtein.NormalizedSimilarity("abaa", "baaa", weights), 12);
        Assert.Equal(0.0, Levenshtein.NormalizedSimilarity("aaaa", "bbbb", weights));
    }

    [Fact]
    public void UpstreamLevenshteinMblevenImplementationSection()
    {
        Assert.Equal(1, Levenshtein.Distance("abcd", "abxd", 1));
        Assert.Equal(2, Levenshtein.Distance("abcd", "axyd", 2));
        Assert.Equal(4, Levenshtein.Distance("abcdef", "abqxyz", 3));
        Assert.Equal(4, Levenshtein.Distance("abcdef", "uvwxyz", 3));
    }

    [Fact]
    public void UpstreamLevenshteinBandedImplementationSection()
    {
        string source = Repeated("abcdefghij", 4);
        string target = Repeated("abcdxfghij", 4);

        Assert.Equal(4, Levenshtein.Distance(source, target));
        Assert.Equal(4, Levenshtein.Distance(source, target, 4, 4));
        Assert.Equal(4, Levenshtein.Distance(source, target, 3, 3));
    }

    [Fact]
    public void UpstreamLevenshteinEditopsTestCase()
    {
        string source = "qabxcd";
        string target = "abycdf";
        EditOperations editops = Levenshtein.Editops(source, target);
        Opcodes opcodes = Levenshtein.Opcodes(source, target);

        Assert.Equal(3, Levenshtein.Distance(source, target));
        Assert.Equal(target, editops.ApplyTo(source, target));
        Assert.Equal(target, opcodes.ApplyTo(source, target));
        Assert.Equal(editops, opcodes.ToEditOperations());
    }

    [Fact]
    public void UpstreamLevenshteinFindHirschbergPosSection()
    {
        string source = Repeated("abcdef", 32);
        string target = Repeated("abcxef", 32);
        Opcodes opcodes = Levenshtein.Opcodes(source, target);

        Assert.Equal(32, Levenshtein.Distance(source, target));
        Assert.Equal(target, opcodes.ApplyTo(source, target));
        Assert.NotEmpty(opcodes.GetMatchingBlocks());
    }

    [Fact]
    public void UpstreamLevenshteinBlockwiseSection()
    {
        string source = Repeated("abcdefghij", 8);
        string target = Repeated("Zbcdefghij", 8);

        Assert.Equal(8, Levenshtein.Distance(source, target));
        Assert.Equal(8, Levenshtein.Distance(source, target, 8, 8));
        Assert.Equal(8, Levenshtein.Distance(source, target, 7, 7));
    }

    [Fact]
    public void UpstreamLevenshteinEditopsFuzzingRegressionsSection()
    {
        AssertLevenshteinEditopsApply("b", "aaaaaaaaaaaaaaaabbaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        AssertLevenshteinEditopsApply("aa", "abb");
        AssertLevenshteinEditopsApply(Repeated("abb", 512), Repeated("ccccca", 512));
    }

    [Fact]
    public void UpstreamLevenshteinSmallBandSection()
    {
        Assert.Equal(3, Levenshtein.Distance("abcdef", "azced", 2));
        Assert.Equal(3, Levenshtein.Distance("abcdef", "azced", 3));
    }

    [Fact]
    public void UpstreamLevenshteinLargeBandPythonLevenshteinIssue9Section()
    {
        byte[] example1 = ReadFixtureBytes("python-levenshtein-issue9-example1.bin.gz");
        byte[] example2 = ReadFixtureBytes("python-levenshtein-issue9-example2.bin.gz");
        string example1Text = ToFixtureString(example1);
        string example2Text = ToFixtureString(example2);
        string source = example1Text.Substring(3718, 1509);
        string target = example2Text.Substring(2784, 2785);

        Assert.Equal(5227, example1.Length);
        Assert.Equal(5569, example2.Length);
        Assert.Equal(1587, Levenshtein.Distance(source, target));
        Assert.Equal(target, Levenshtein.Editops(source, target).ApplyTo(source, target));
        Assert.Equal(2590, Levenshtein.Distance(example1Text, example2Text));
        Assert.Equal(example2Text, Levenshtein.Editops(example1Text, example2Text).ApplyTo(example1Text, example2Text));
    }

    [Fact]
    public void UpstreamLevenshteinLargeBandOcrExampleSection()
    {
        byte[] example1 = ReadFixtureBytes("ocr-example1.bin.gz");
        byte[] example2 = ReadFixtureBytes("ocr-example2.bin.gz");
        string example1Text = ToFixtureString(example1);
        string example2Text = ToFixtureString(example2);
        string source = example1Text.Substring(51, 6541);
        string target = example2Text.Substring(51, 6516);

        Assert.Equal(106514, example1.Length);
        Assert.Equal(107244, example2.Length);
        Assert.Equal(target, Levenshtein.Editops(source, target).ApplyTo(source, target));
        Assert.Equal(5278, Levenshtein.Distance(example1Text, example2Text));
        Assert.Equal(2501, Levenshtein.Distance(example1Text, example2Text, 2500));
    }

    [Fact]
    public void UpstreamLevenshteinSimdWraparoundSection()
    {
        string source = "a";
        string target256 = Repeated("b", 256);
        string target512 = Repeated("b", 512);

        Assert.Equal(256, Levenshtein.Distance(source, target256));
        Assert.Equal(512, Levenshtein.Distance(source, target512));
    }

    [Fact]
    public void UpstreamLevenshteinSimdSection()
    {
        string source = Repeated("abc", 48);
        string target = Repeated("axc", 48);

        Assert.Equal(48, Levenshtein.Distance(source, target));
        Assert.Equal(48, Levenshtein.Distance(source, target, 48, 48));
    }

    [Fact]
    public void UpstreamLevenshteinMultipleSequencesSection()
    {
        MultiLevenshtein scorer = new(["lewenstein", "kitten", "same"]);
        int[] scores = new int[3];

        scorer.Distances("levenshtein", scores);

        Assert.Equal(2, scores[0]);
        Assert.Equal(8, scores[1]);
        Assert.Equal(9, scores[2]);
    }

    [Fact]
    public void UpstreamIndelTestCase()
    {
        Assert.Equal(3, Indel.Distance("lewenstein", "levenshtein"));
        Assert.Equal(18, Indel.Similarity("lewenstein", "levenshtein"));
    }

    [Fact]
    public void UpstreamIndelSimilarStringsSection()
    {
        Assert.Equal(3, Indel.Distance("lewenstein", "levenshtein"));
        Assert.Equal(0.8571428571428571, Indel.NormalizedSimilarity("lewenstein", "levenshtein"), 12);
    }

    [Fact]
    public void UpstreamIndelCompletelyDifferentStringsSection()
    {
        Assert.Equal(8, Indel.Distance("abcd", "wxyz"));
        Assert.Equal(0, Indel.Similarity("abcd", "wxyz"));
    }

    [Fact]
    public void UpstreamIndelMblevenSection()
    {
        Assert.Equal(2, Indel.Distance("abcd", "abxd", 2));
        Assert.Equal(5, Indel.Distance("abcd", "wxyz", 4));
    }

    [Fact]
    public void UpstreamIndelCachedImplementationSection()
    {
        CachedIndel cached = new("lewenstein");

        Assert.Equal(3, cached.Distance("levenshtein"));
        Assert.Equal(18, cached.Similarity("levenshtein"));
        Assert.Equal(0.8571428571428571, cached.NormalizedSimilarity("levenshtein"), 12);
    }

    [Fact]
    public void UpstreamIndelBandedImplementationSection()
    {
        Assert.Equal(3, Indel.Distance("lewenstein", "levenshtein", 4));
        Assert.Equal(3, Indel.Distance("lewenstein", "levenshtein", 3));
        Assert.Equal(3, Indel.Distance("lewenstein", "levenshtein", 2));
    }

    [Fact]
    public void UpstreamLcsSeqTestCase()
    {
        Assert.Equal(9, LcsSeq.Similarity("lewenstein", "levenshtein"));
        Assert.Equal(2, LcsSeq.Distance("lewenstein", "levenshtein"));
    }

    [Fact]
    public void UpstreamLcsSeqSimilarStringsSection()
    {
        Assert.Equal(9, LcsSeq.Similarity("lewenstein", "levenshtein"));
        Assert.Equal(0.8181818181818181, LcsSeq.NormalizedSimilarity("lewenstein", "levenshtein"), 12);
    }

    [Fact]
    public void UpstreamLcsSeqCompletelyDifferentStringsSection()
    {
        Assert.Equal(0, LcsSeq.Similarity("abcd", "wxyz"));
        Assert.Equal(4, LcsSeq.Distance("abcd", "wxyz"));
    }

    [Fact]
    public void UpstreamLcsSeqMblevenSection()
    {
        Assert.Equal(3, LcsSeq.Similarity("abcd", "abxd", 3));
        Assert.Equal(0, LcsSeq.Similarity("abcd", "wxyz", 1));
    }

    [Fact]
    public void UpstreamLcsSeqCachedImplementationSection()
    {
        CachedLcsSeq cached = new("lewenstein");

        Assert.Equal(9, cached.Similarity("levenshtein"));
        Assert.Equal(2, cached.Distance("levenshtein"));
        Assert.Equal(0.8181818181818181, cached.NormalizedSimilarity("levenshtein"), 12);
    }

    [Fact]
    public void UpstreamLcsSeqSimdWraparoundSection()
    {
        Assert.Equal(256, LcsSeq.Distance("a", Repeated("b", 256)));
        Assert.Equal(512, LcsSeq.Distance("a", Repeated("b", 512)));
    }

    [Fact]
    public void UpstreamHammingTestCase()
    {
        Assert.Equal(3, Hamming.Distance("karolin", "kathrin"));
    }

    [Fact]
    public void UpstreamHammingCalculatesCorrectDistancesSection()
    {
        Assert.Equal(3, Hamming.Distance("karolin", "kathrin"));
        Assert.Equal(3, Hamming.Distance("karolin", "kerstin"));
        Assert.Equal(2, Hamming.Distance("1011101", "1001001"));
    }

    [Fact]
    public void UpstreamHammingHandlesDifferentStringLengthsAsInsertionsDeletionsSection()
    {
        Assert.Equal(3, Hamming.Distance("abc", "", pad: true));
        Assert.Equal(3, Hamming.Distance("", "abc", pad: true));
        Assert.Throws<ArgumentException>(() => Hamming.Distance("abc", "", pad: false));
    }

    [Fact]
    public void UpstreamHammingEditopsSection()
    {
        EditOperations editops = Hamming.Editops("karolin", "kathrin");

        Assert.Equal("kathrin", editops.ApplyTo("karolin", "kathrin"));
        Assert.Equal(3, editops.Count);
    }

    [Fact]
    public void UpstreamOsaSimpleSection()
    {
        Assert.Equal(1, Osa.Distance("CA", "AC"));
        Assert.Equal(3, Osa.Distance("CA", "ABC"));
        Assert.Equal(0, Osa.Distance("same", "same"));
        Assert.Equal(1.0, Osa.NormalizedSimilarity("same", "same"));
    }

    [Fact]
    public void UpstreamDamerauLevenshteinTestCase()
    {
        Assert.Equal(1, DamerauLevenshtein.Distance("CA", "AC"));
        Assert.Equal(2, DamerauLevenshtein.Distance("CA", "ABC"));
    }

    [Fact]
    public void UpstreamDamerauLevenshteinCalculatesCorrectDistancesSection()
    {
        Assert.Equal(1, DamerauLevenshtein.Distance("CA", "AC"));
        Assert.Equal(2, DamerauLevenshtein.Distance("CA", "ABC"));
        Assert.Equal(3, DamerauLevenshtein.Distance("kitten", "sitting"));
        Assert.Equal(0, DamerauLevenshtein.Distance("same", "same"));
    }

    [Fact]
    public void UpstreamDamerauLevenshteinCalculatesCorrectRatiosSection()
    {
        Assert.Equal(1.0, DamerauLevenshtein.NormalizedSimilarity("same", "same"));
        Assert.Equal(0.33333333333333337, DamerauLevenshtein.NormalizedSimilarity("CA", "ABC"), 12);
        Assert.Equal(0.5714285714285714, DamerauLevenshtein.NormalizedSimilarity("kitten", "sitting"), 12);
    }

    [Fact]
    public void UpstreamJaroTestCase()
    {
        Assert.Equal(1.0, Jaro.Similarity("", ""));
        Assert.Equal(0.9444444444444445, Jaro.Similarity("martha", "marhta"), 12);
    }

    [Fact]
    public void UpstreamJaroFullResultWithScoreCutoffSection()
    {
        Assert.Equal(0.9444444444444445, Jaro.Similarity("martha", "marhta", 0.9), 12);
        Assert.Equal(0.0, Jaro.Similarity("martha", "marhta", 0.95));
    }

    [Fact]
    public void UpstreamJaroEdgeCaseLengthsSection()
    {
        Assert.Equal(1.0, Jaro.Similarity("", ""));
        Assert.Equal(0.0, Jaro.Similarity("", "abc"));
        Assert.Equal(0.0, Jaro.Similarity("abc", ""));
        Assert.Equal(1.0, Jaro.Similarity("a", "a"));
    }

    [Fact]
    public void UpstreamJaroFuzzingRegressionsSection()
    {
        Assert.Equal(0.8222222222222223, Jaro.Similarity("dwayne", "duane"), 12);
        Assert.Equal(0.7666666666666666, Jaro.Similarity("dixon", "dicksonx"), 12);
    }

    [Fact]
    public void UpstreamJaroWinklerTestCase()
    {
        Assert.Equal(1.0, JaroWinkler.Similarity("", ""));
        Assert.Equal(0.9611111111111111, JaroWinkler.Similarity("martha", "marhta"), 12);
    }

    [Fact]
    public void UpstreamJaroWinklerFullResultWithScoreCutoffSection()
    {
        Assert.Equal(0.9611111111111111, JaroWinkler.Similarity("martha", "marhta", scoreCutoff: 0.9), 12);
        Assert.Equal(0.0, JaroWinkler.Similarity("martha", "marhta", scoreCutoff: 0.98));
    }

    [Fact]
    public void UpstreamJaroWinklerEdgeCaseLengthsSection()
    {
        Assert.Equal(1.0, JaroWinkler.Similarity("", ""));
        Assert.Equal(0.0, JaroWinkler.Similarity("", "abc"));
        Assert.Equal(0.0, JaroWinkler.Similarity("abc", ""));
        Assert.Equal(1.0, JaroWinkler.Similarity("a", "a"));
    }

    [Fact]
    public void UpstreamFuzzRatioTestCase()
    {
        Assert.Equal(100.0, Fuzz.Ratio("new york mets", "new york mets"));
        Assert.Equal(96.55172413793103, Fuzz.Ratio("this is a test", "this is a test!"), 12);
    }

    [Fact]
    public void UpstreamFuzzEqualSection()
    {
        Assert.Equal(100.0, Fuzz.Ratio("new york mets", "new york mets"));
        Assert.Equal(100.0, Fuzz.Ratio("", ""));
    }

    [Fact]
    public void UpstreamFuzzPartialRatioSection()
    {
        Assert.Equal(100.0, Fuzz.PartialRatio("test", "xx test yy"));
        Assert.Equal(100.0, Fuzz.PartialRatio("abcd", "xxxabcdyyy"));
        Assert.True(Fuzz.PartialRatio("new york mets", "new york city mets") > 80.0);
    }

    [Fact]
    public void UpstreamFuzzTokenSortRatioSection()
    {
        Assert.Equal(100.0, Fuzz.TokenSortRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear"));
    }

    [Fact]
    public void UpstreamFuzzTokenSetRatioSection()
    {
        Assert.Equal(100.0, Fuzz.TokenSetRatio("fuzzy was a bear but not a dog", "fuzzy was a bear"));
        Assert.Equal(50.0, Fuzz.TokenSetRatio("a{", "{b"));
    }

    [Fact]
    public void UpstreamFuzzPartialTokenSetRatioSection()
    {
        Assert.Equal(100.0, Fuzz.PartialTokenSetRatio("fuzzy was a bear but not a dog", "fuzzy was a bear"));
        Assert.Equal(100.0, Fuzz.PartialTokenSetRatio("alpha beta", "alpha gamma"));
    }

    [Fact]
    public void UpstreamFuzzWRatioEqualSection()
    {
        Assert.Equal(100.0, Fuzz.WRatio("fuzzy wuzzy was a bear", "fuzzy wuzzy was a bear"));
    }

    [Fact]
    public void UpstreamFuzzWRatioPartialMatchSection()
    {
        Assert.Equal(90.0, Fuzz.WRatio("new york mets", "the wonderful new york mets"));
    }

    [Fact]
    public void UpstreamFuzzWRatioMisorderedMatchSection()
    {
        Assert.Equal(95.0, Fuzz.WRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear"));
    }

    [Fact]
    public void UpstreamFuzzIssue452Section()
    {
        Assert.Equal(33.33333333333333, Fuzz.PartialRatio("001", "220222"), 10);
    }

    [Fact]
    public void UpstreamFuzzTwoEmptyStringsSection()
    {
        Assert.Equal(100.0, Fuzz.Ratio("", ""));
        Assert.Equal(100.0, Fuzz.PartialRatio("", ""));
        Assert.Equal(100.0, Fuzz.TokenSortRatio("", ""));
        Assert.Equal(0.0, Fuzz.TokenSetRatio("", ""));
    }

    [Fact]
    public void UpstreamFuzzFirstStringEmptySection()
    {
        Assert.Equal(0.0, Fuzz.Ratio("", "new york mets"));
        Assert.Equal(0.0, Fuzz.PartialRatio("", "new york mets"));
    }

    [Fact]
    public void UpstreamFuzzSecondStringEmptySection()
    {
        Assert.Equal(0.0, Fuzz.Ratio("new york mets", ""));
        Assert.Equal(0.0, Fuzz.PartialRatio("new york mets", ""));
    }

    [Fact]
    public void UpstreamFuzzPartialRatioShortNeedleSection()
    {
        Assert.Equal(100.0, Fuzz.PartialRatio("abcd", "xxxabcdyyy"));
    }

    [Fact]
    public void UpstreamFuzzIssue206Section()
    {
        Assert.Equal(65.0, Fuzz.Ratio("new york mets", "the wonderful new york mets"));
    }

    [Fact]
    public void UpstreamFuzzIssue210Section()
    {
        Assert.Equal(100.0, Fuzz.TokenSetRatio("new york mets vs atlanta braves", "atlanta braves vs new york mets"));
    }

    [Fact]
    public void UpstreamFuzzIssue231Section()
    {
        string source = "er merkantilismus f/rderte handel und verkehr mit teils marktkonformen, teils dirigistischen ma_nahmen.";
        string target = "ils marktkonformen, teils dirigistischen ma_nahmen. an der schwelle zum 19. jahrhundert entstand ein neu";
        ScoreAlignment alignment = Fuzz.PartialRatioAlignment(source, target);

        Assert.Equal(66.23376623376623, alignment.Score, 10);
        Assert.Equal(0, alignment.DestinationStart);
        Assert.Equal(51, alignment.DestinationEnd);
    }

    [Fact]
    public void UpstreamFuzzIssue257FirstSection()
    {
        Assert.Equal(75.86206896551724, Fuzz.Ratio("new york mets", "new york yankees"), 12);
    }

    [Fact]
    public void UpstreamFuzzIssue257SecondSection()
    {
        Assert.Equal(57.14285714285714, Fuzz.PartialRatio("abcd", "XbcY"), 12);
    }

    private static string Repeated(string value, int count)
    {
        return string.Concat(Enumerable.Repeat(value, count));
    }

    private static void AssertLevenshteinEditopsApply(string source, string target)
    {
        EditOperations operations = Levenshtein.Editops(source, target);

        Assert.Equal(target, operations.ApplyTo(source, target));
    }

    private static byte[] ReadFixtureBytes(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        using FileStream file = File.OpenRead(path);
        using GZipStream gzip = new(file, CompressionMode.Decompress);
        using MemoryStream output = new();

        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static string ToFixtureString(byte[] bytes)
    {
        char[] chars = new char[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}
