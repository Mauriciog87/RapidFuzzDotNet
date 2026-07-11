using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace RapidFuzzDotNet.PackageVerifier;

public static class Program
{
    private const string ExpectedVersion = "1.0.0-beta.2";
    private static readonly string ExpectedRepository = "https:" + "/" + "/github.com/Mauriciog87/RapidFuzzDotNet";
    private static readonly string ExpectedRawRepository = "https:" + "/" + "/raw.githubusercontent.com/Mauriciog87/RapidFuzzDotNet/";
    private static readonly Guid SourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Package verification failed: {exception.Message.Trim()}");
            return 2;
        }
    }

    private static int Run(string[] args)
    {
        VerifierOptions options = VerifierOptions.Parse(args);
        string packagePath = FindSinglePackage(options.PackageDirectory, ".nupkg");
        string symbolPackagePath = FindSinglePackage(options.PackageDirectory, ".snupkg");
        int failures = VerifyPackage(packagePath, options.RepositoryCommit)
            + VerifySymbols(symbolPackagePath, options.RepositoryCommit, options.VerifyRemote);
        return failures == 0 ? 0 : 1;
    }

    private static int VerifyPackage(string packagePath, string expectedCommit)
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
        failures += RequireValue("repository commit", commit, expectedCommit);

        return failures;
    }

    private static int VerifySymbols(string symbolPackagePath, string expectedCommit, bool verifyRemote)
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
        string? sourceUrl = null;

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

            if (sourceLink is null || !TryResolveSourceUrl(sourceLink, reader, expectedCommit, out string? resolvedUrl))
            {
                failures++;
                Console.Error.WriteLine($"Source Link: missing or invalid in {entry.FullName}");
            }
            else
            {
                Console.WriteLine($"Source Link: ok ({entry.FullName})");
                sourceUrl ??= resolvedUrl;
            }
        }

        if (verifyRemote)
        {
            failures += VerifyRemoteSource(sourceUrl);
        }

        return failures;
    }

    private static bool TryResolveSourceUrl(string sourceLink, MetadataReader reader, string expectedCommit, out string? sourceUrl)
    {
        sourceUrl = null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(sourceLink);

            if (!document.RootElement.TryGetProperty("documents", out JsonElement documents))
            {
                return false;
            }

            string expectedPrefix = ExpectedRawRepository + expectedCommit + "/";

            foreach (JsonProperty mapping in documents.EnumerateObject())
            {
                string? template = mapping.Value.GetString();

                if (template is null || !template.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    return false;
                }

                foreach (DocumentHandle handle in reader.Documents)
                {
                    string documentName = reader.GetString(reader.GetDocument(handle).Name);

                    if (TryApplyMapping(mapping.Name, template, documentName, out sourceUrl))
                    {
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryApplyMapping(string pattern, string template, string documentName, out string? sourceUrl)
    {
        sourceUrl = null;
        int patternWildcard = pattern.IndexOf('*');
        int templateWildcard = template.IndexOf('*');

        if (patternWildcard < 0 || templateWildcard < 0)
        {
            if (!string.Equals(pattern, documentName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            sourceUrl = template;
            return true;
        }

        string prefix = pattern[..patternWildcard];
        string suffix = pattern[(patternWildcard + 1)..];

        if (!documentName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !documentName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            || documentName.Length < prefix.Length + suffix.Length)
        {
            return false;
        }

        string replacement = documentName.Substring(prefix.Length, documentName.Length - prefix.Length - suffix.Length).Replace('\\', '/');
        sourceUrl = template[..templateWildcard] + replacement + template[(templateWildcard + 1)..];
        return true;
    }

    private static int VerifyRemoteSource(string? sourceUrl)
    {
        if (sourceUrl is null)
        {
            Console.Error.WriteLine("Source Link remote: no source URL available");
            return 1;
        }

        using HttpClient client = new();
        using HttpResponseMessage response = client.GetAsync(sourceUrl).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Source Link remote: {response.StatusCode} ({sourceUrl})");
            return 1;
        }

        Console.WriteLine($"Source Link remote: ok ({sourceUrl})");
        return 0;
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

    private readonly record struct VerifierOptions(string PackageDirectory, string RepositoryCommit, bool VerifyRemote)
    {
        public static VerifierOptions Parse(string[] args)
        {
            string? packageDirectory = null;
            string? repositoryCommit = null;
            bool verifyRemote = false;

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] == "--package-dir" && index + 1 < args.Length)
                {
                    packageDirectory = Path.GetFullPath(args[++index]);
                }
                else if (args[index] == "--repository-commit" && index + 1 < args.Length)
                {
                    repositoryCommit = args[++index];
                }
                else if (args[index] == "--verify-remote")
                {
                    verifyRemote = true;
                }
                else
                {
                    throw new ArgumentException($"Unknown or incomplete argument '{args[index]}'.", nameof(args));
                }
            }

            if (packageDirectory is null || repositoryCommit is null || repositoryCommit.Length != 40)
            {
                throw new ArgumentException("Usage: --package-dir <path> --repository-commit <sha> [--verify-remote]", nameof(args));
            }

            return new VerifierOptions(packageDirectory, repositoryCommit, verifyRemote);
        }
    }
}
