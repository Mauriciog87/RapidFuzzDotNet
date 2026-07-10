using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;

namespace RapidFuzzDotNet.PackageVerifier;

public static class Program
{
    private const string ExpectedVersion = "1.0.0-beta.2";
    private static readonly string ExpectedRepository = "https:" + "/" + "/github.com/Mauriciog87/RapidFuzzDotNet";
    private static readonly Guid SourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

    public static int Main(string[] args)
    {
        string packageDirectory = ParsePackageDirectory(args);
        string packagePath = FindSinglePackage(packageDirectory, ".nupkg");
        string symbolPackagePath = FindSinglePackage(packageDirectory, ".snupkg");
        int failures = VerifyPackage(packagePath) + VerifySymbols(symbolPackagePath);
        return failures == 0 ? 0 : 1;
    }

    private static int VerifyPackage(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        int failures = 0;
        failures += RequireEntry(archive, "LICENSE");
        failures += RequireEntry(archive, "NOTICE");
        failures += RequireEntry(archive, "README.md");
        failures += RequireEntry(archive, "lib/net8.0/RapidFuzzDotNet.dll");
        failures += RequireEntry(archive, "lib/net10.0/RapidFuzzDotNet.dll");

        ZipArchiveEntry nuspecEntry = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream nuspecStream = nuspecEntry.Open();
        XDocument document = XDocument.Load(nuspecStream);
        XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;
        XElement metadata = document.Root?.Element(ns + "metadata")
            ?? throw new InvalidOperationException("The package nuspec has no metadata element.");
        failures += RequireValue("version", metadata.Element(ns + "version")?.Value, ExpectedVersion);
        failures += RequireValue("readme", metadata.Element(ns + "readme")?.Value, "README.md");

        XElement? license = metadata.Element(ns + "license");
        failures += RequireValue("license type", license?.Attribute("type")?.Value, "file");
        failures += RequireValue("license file", license?.Value, "LICENSE");

        XElement? repository = metadata.Element(ns + "repository");
        failures += RequireValue("repository type", repository?.Attribute("type")?.Value, "git");
        failures += RequireValue("repository url", repository?.Attribute("url")?.Value, ExpectedRepository);
        string commit = repository?.Attribute("commit")?.Value ?? string.Empty;

        if (commit.Length != 40)
        {
            failures++;
            Console.Error.WriteLine($"repository commit: expected 40 characters, actual '{commit}'");
        }
        else
        {
            Console.WriteLine($"repository commit: ok ({commit})");
        }

        return failures;
    }

    private static int VerifySymbols(string symbolPackagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(symbolPackagePath);
        ZipArchiveEntry[] pdbEntries = archive.Entries
            .Where(entry => entry.FullName.EndsWith("RapidFuzzDotNet.pdb", StringComparison.Ordinal))
            .ToArray();

        if (pdbEntries.Length != 2)
        {
            Console.Error.WriteLine($"symbol PDBs: expected 2, actual {pdbEntries.Length}");
            return 1;
        }

        int failures = 0;

        for (int index = 0; index < pdbEntries.Length; index++)
        {
            ZipArchiveEntry entry = pdbEntries[index];
            using Stream stream = entry.Open();
            using MemoryStream copy = new();
            stream.CopyTo(copy);
            copy.Position = 0;
            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(copy, MetadataStreamOptions.LeaveOpen);
            MetadataReader reader = provider.GetMetadataReader();
            string? sourceLink = ReadSourceLink(reader);

            if (sourceLink is null
                || !sourceLink.Contains("raw.githubusercontent.com/Mauriciog87/RapidFuzzDotNet", StringComparison.Ordinal))
            {
                failures++;
                Console.Error.WriteLine($"Source Link: missing or invalid in {entry.FullName}");
            }
            else
            {
                Console.WriteLine($"Source Link: ok ({entry.FullName})");
            }
        }

        return failures;
    }

    private static string? ReadSourceLink(MetadataReader reader)
    {
        foreach (CustomDebugInformationHandle handle in reader.CustomDebugInformation)
        {
            CustomDebugInformation information = reader.GetCustomDebugInformation(handle);

            if (reader.GetGuid(information.Kind) == SourceLinkKind)
            {
                return Encoding.UTF8.GetString(reader.GetBlobBytes(information.Value));
            }
        }

        return null;
    }

    private static int RequireEntry(ZipArchive archive, string path)
    {
        bool exists = archive.Entries.Any(entry => string.Equals(entry.FullName, path, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            Console.Error.WriteLine($"package entry: missing {path}");
            return 1;
        }

        Console.WriteLine($"package entry: ok ({path})");
        return 0;
    }

    private static int RequireValue(string name, string? actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{name}: expected '{expected}', actual '{actual}'");
            return 1;
        }

        Console.WriteLine($"{name}: ok ({actual})");
        return 0;
    }

    private static string FindSinglePackage(string directory, string extension)
    {
        string[] files = Directory.GetFiles(directory, "RapidFuzzDotNet.1.0.0-beta.2*" + extension, SearchOption.TopDirectoryOnly);

        if (files.Length != 1)
        {
            throw new InvalidOperationException($"Expected one {extension} package in '{directory}', found {files.Length}.");
        }

        return files[0];
    }

    private static string ParsePackageDirectory(string[] args)
    {
        if (args.Length == 2 && args[0] == "--package-dir")
        {
            return Path.GetFullPath(args[1]);
        }

        throw new ArgumentException("Usage: --package-dir <path>", nameof(args));
    }
}
