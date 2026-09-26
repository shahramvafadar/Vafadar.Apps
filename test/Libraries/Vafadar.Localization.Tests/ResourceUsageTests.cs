using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Localization.Tests;

/// <summary>
/// Every resource key used in code or XAML must exist, so a typo never shows a raw key to the user (S14, AT-48).
/// Keys built at run time (e.g. <c>$"EntryKind_{kind}"</c>) are not checked here.
/// </summary>
public sealed partial class ResourceUsageTests
{
    public static TheoryData<string> AppFolders() => ["src/Apps/Finance"];

    [Theory]
    [MemberData(nameof(AppFolders))]
    public void Every_key_used_in_an_app_exists(string appFolder)
    {
        var known = Keys(RepositoryPaths.Combine("src/Libraries")).Concat(Keys(RepositoryPaths.Combine(appFolder))).ToHashSet(StringComparer.Ordinal);
        var missing = new List<string>();
        var found = 0;

        foreach (var file in SourceFiles(RepositoryPaths.Combine(appFolder)).Concat(SourceFiles(RepositoryPaths.Combine("src/Libraries/Vafadar.Maui"))))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in UsagePattern().Matches(text))
            {
                found++;
                var key = match.Groups["key"].Value;
                if (!known.Contains(key))
                {
                    missing.Add($"{Path.GetFileName(file)}: {key}");
                }
            }
        }

        Assert.True(found > 200, $"Only {found} key usages found; the pattern may be broken.");
        Assert.Empty(missing.Distinct());
    }

    [Fact]
    public void No_translation_is_empty()
    {
        var empty = Directory.EnumerateFiles(RepositoryPaths.Combine("src"), "*.resx", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => XDocument.Load(path).Root!.Elements("data")
                .Where(data => data.Attribute("type") is null && string.IsNullOrWhiteSpace((string?)data.Element("value")))
                .Select(data => $"{Path.GetFileName(path)}: {(string)data.Attribute("name")!}"))
            .ToList();

        Assert.Empty(empty);
    }

    private static IEnumerable<string> Keys(string folder) =>
        Directory.EnumerateFiles(folder, "*.resx", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => XDocument.Load(path).Root!.Elements("data").Select(data => (string)data.Attribute("name")!));

    private static IEnumerable<string> SourceFiles(string folder) =>
        Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".xaml", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    // {v:Translate Key}, translator["Key"], _translator["Key"], Translator.Instance["Key"], translator.Format("Key", ...).
    [GeneratedRegex(@"(?:Translate\s+(?<key>[A-Za-z]+_[A-Za-z0-9_]+)\}|[tT]ranslator(?:\.Instance)?\[""(?<key>[A-Za-z]+_[A-Za-z0-9_]+)""\]|[tT]ranslator\.Format\(""(?<key>[A-Za-z]+_[A-Za-z0-9_]+)"")")]
    private static partial Regex UsagePattern();
}
