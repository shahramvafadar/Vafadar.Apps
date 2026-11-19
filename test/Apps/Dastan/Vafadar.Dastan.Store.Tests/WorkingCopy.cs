using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// A copy of the working files of the repository in a folder of a test – the collection, its Persian translation,
/// its media and the documents of the editors it points to – with a store beside it and a clock a test can move.
/// Nothing of the repository is written to.
/// </summary>
internal sealed class WorkingCopy : IDisposable
{
    /// <summary>The identifier of the collection of the repository.</summary>
    public const string Collection = "dastan-shahnameh-rostam-sohrab-pilot-v0.5";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-store-" + Guid.NewGuid().ToString("N"));

    public WorkingCopy()
    {
        var app = Path.Combine(RepositoryRoot(), "src", "Apps", "Dastan");
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(_folder, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy);
            }
        }
    }

    public TestClock Clock { get; } = new(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));

    public string Folder => _folder;

    public string Samples => Path.Combine(_folder, "content", "samples");

    public string Sample => Path.Combine(Samples, "shahnameh-pilot.json");

    public string Persian => Path.Combine(Samples, "shahnameh-pilot.fa.json");

    public string Database => Vafadar.Dastan.Tests.TestDatabase.For(Path.Combine(_folder, "store", "editors.db"));

    /// <summary>Opens the store; with <paramref name="files"/> it looks for the files of media in the copy.</summary>
    public EditorialStore Open(bool files = false) => EditorialStore.Open(Database, Clock, files ? Samples : null);

    /// <summary>Opens the store with the collection of the copy brought in.</summary>
    public EditorialStore Filled(bool files = false)
    {
        var store = Open(files);
        Assert.Empty(store.Import(Sample, "an editor", "The first import.").Problems);
        return store;
    }

    public JsonObject Working() => JsonNode.Parse(File.ReadAllText(Sample))!.AsObject();

    public static JsonObject Record(JsonObject sample, string group, string id) =>
        sample[group]!.AsArray().Select(node => node!.AsObject()).Single(record => (string?)record["id"] == id);

    /// <summary>Returns a record of the store as it is now, read from the collection the store writes out.</summary>
    public static JsonObject Stored(EditorialStore store, string group, string id) =>
        Record(JsonNode.Parse(store.Export(Collection)!.Json)!.AsObject(), group, id);

    public void Dispose()
    {
        // A copy is thrown away once, however often its owner is.
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Vafadar.Apps.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The root of the repository was not found.");
    }
}

/// <summary>A clock that stands still until a test moves it.</summary>
internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void Advance(TimeSpan by) => _now += by;
}
