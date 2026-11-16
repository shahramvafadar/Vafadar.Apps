using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The editorial sample of the repository, read from where it lives. Tests either load it as it is or change one
/// thing in a copy in memory and read that; the file itself is never written.
/// </summary>
internal static class SampleFiles
{
    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

    /// <summary>Gets the folder of the app in the repository.</summary>
    public static string App => Path.Combine(RepositoryRoot.Value, "src", "Apps", "Dastan");

    /// <summary>Gets the folder that holds the files of the collection.</summary>
    public static string Content => Path.Combine(App, "content");

    /// <summary>Gets the folder of the samples.</summary>
    public static string Samples => Path.Combine(Content, "samples");

    /// <summary>Gets the path of the pilot sample.</summary>
    public static string PilotPath => Path.Combine(Samples, "shahnameh-pilot.json");

    /// <summary>Gets the path of the fixtures that are test data only.</summary>
    public static string FixturesPath => Path.Combine(RepositoryRoot.Value, "test", "Apps", "Dastan", "Fixtures", "cross-domain-fixtures.json");

    /// <summary>Returns the options of the sample in the repository.</summary>
    public static PilotSampleOptions Options(bool checkFiles = true, params string[] translations) => new()
    {
        SampleDirectory = Samples,
        ContentRoot = Content,
        ReviewRoot = App,
        CheckFiles = checkFiles,
        Translations = translations,
    };

    /// <summary>Returns a copy of the pilot sample that a test may change.</summary>
    public static JsonObject Pilot() => JsonNode.Parse(File.ReadAllText(PilotPath))!.AsObject();

    /// <summary>Reads the pilot sample without translations, after an optional change.</summary>
    public static Catalogue Read(Action<JsonObject>? change = null, params string[] translations)
    {
        var sample = Pilot();
        change?.Invoke(sample);
        return PilotSample.Read(sample.ToJsonString(), Options(translations: translations));
    }

    /// <summary>Reads a changed pilot sample and returns the problems it is refused for.</summary>
    public static IReadOnlyList<string> Problems(Action<JsonObject> change, params string[] translations) =>
        Assert.Throws<SampleFormatException>(() => Read(change, translations)).Problems;

    /// <summary>Returns the record of a group ("entities", "media", …) with the identifier.</summary>
    public static JsonObject Record(this JsonObject sample, string group, string id) =>
        sample[group]!.AsArray().Select(node => node!.AsObject()).Single(record => (string?)record["id"] == id);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Vafadar.Apps.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root (Vafadar.Apps.slnx) was not found above the test output.");
    }
}
