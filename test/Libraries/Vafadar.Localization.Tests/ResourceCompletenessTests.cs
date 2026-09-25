using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Localization.Tests;

/// <summary>
/// Checks every translated resource file in src/ against its neutral (English) file: same keys, same placeholders.
/// This covers all apps and libraries, including MAUI projects that cannot be referenced from tests.
/// </summary>
public sealed partial class ResourceCompletenessTests
{
    public static TheoryData<string, string> TranslatedResourceFiles()
    {
        var data = new TheoryData<string, string>();
        foreach (var neutral in NeutralResourceFiles())
        {
            foreach (var language in AppLanguages.All.Where(l => l != AppLanguages.English))
            {
                data.Add(Relative(neutral), language.CultureName);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(TranslatedResourceFiles))]
    public void Translation_has_exactly_the_keys_of_the_neutral_file(string neutralFile, string culture)
    {
        var neutral = ReadEntries(RepositoryPaths.Combine(neutralFile));
        var translated = ReadEntries(TranslatedPath(neutralFile, culture));

        Assert.Empty(neutral.Keys.Except(translated.Keys));
        Assert.Empty(translated.Keys.Except(neutral.Keys));
    }

    [Theory]
    [MemberData(nameof(TranslatedResourceFiles))]
    public void Translation_keeps_the_placeholders_of_the_neutral_file(string neutralFile, string culture)
    {
        var neutral = ReadEntries(RepositoryPaths.Combine(neutralFile));
        var translated = ReadEntries(TranslatedPath(neutralFile, culture));

        foreach (var (key, value) in translated)
        {
            Assert.True(
                Placeholders(neutral[key]).SetEquals(Placeholders(value)),
                $"'{key}' has different placeholders in the '{culture}' translation.");
        }
    }

    [Fact]
    public void Resource_files_were_found()
    {
        Assert.NotEmpty(NeutralResourceFiles());
    }

    private static IEnumerable<string> NeutralResourceFiles() =>
        Directory.EnumerateFiles(RepositoryPaths.Combine("src"), "*.resx", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetFileNameWithoutExtension(path).IndexOf('.') < 0)
            .Order(StringComparer.Ordinal);

    private static string TranslatedPath(string neutralFile, string culture)
    {
        var path = RepositoryPaths.Combine(neutralFile);
        var translated = Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}.{culture}.resx");
        Assert.True(File.Exists(translated), $"Missing translation file {Relative(translated)}.");
        return translated;
    }

    private static Dictionary<string, string> ReadEntries(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .Where(data => data.Attribute("type") is null)
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? string.Empty);

    private static HashSet<string> Placeholders(string value) =>
        [.. PlaceholderPattern().Matches(value).Select(match => match.Groups["index"].Value)];

    private static string Relative(string path) => Path.GetRelativePath(RepositoryPaths.Root, path);

    [GeneratedRegex(@"\{(?<index>\d+)(?:[,:][^}]*)?\}")]
    private static partial Regex PlaceholderPattern();
}
