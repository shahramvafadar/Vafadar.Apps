using System.Net;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using Vafadar.Dastan.Store;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site that reads its collections from the database (step 19.3, ADR 0015): what the editors published
/// and released is what the public sees – without a file of the collection in the folder of the site – and
/// the site shows nothing that was not released, was withdrawn, or is no longer the text it was published as.
/// </summary>
public sealed class ReleasedFromDatabaseTests : IDisposable
{
    private const string Collection = "dastan-shahnameh-rostam-sohrab-pilot-v0.5";
    private const string Story = "story-rostam-sohrab";
    private static readonly DateOnly Today = new(2026, 11, 19);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-released-" + Guid.NewGuid().ToString("N"));
    private readonly string _database;

    public ReleasedFromDatabaseTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "store", "dastan.db"));

        // The working files of the repository, for the editors; the site gets the media only.
        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(_folder, "editors", Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy);
            }
        }

        Directory.CreateDirectory(Path.Combine(SiteContent, "samples"));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_folder, "editors", "content", "media")))
        {
            Directory.CreateDirectory(Path.Combine(SiteContent, "media"));
            File.Copy(file, Path.Combine(SiteContent, "media", Path.GetFileName(file)));
        }
    }

    private string SiteContent => Path.Combine(_folder, "site", "content");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private EditorialStore Editors() => EditorialStore.Open(_database, samplesFolder: Path.Combine(_folder, "editors", "content", "samples"));

    // The collection brought in, every statement reviewed, and published as the next revision.
    private int Publish(EditorialStore store)
    {
        if (!store.Collections.Contains(Collection))
        {
            Assert.Empty(store.Import(Path.Combine(_folder, "editors", "content", "samples", "shahnameh-pilot.json"), "an editor", "The first import.").Problems);
            foreach (var statement in store.Statements(Collection))
            {
                Assert.True(store.Review(Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", Today.AddDays(-1), "Read against the text.").Done);
            }
        }

        var result = store.Publish(Collection, Today, "a publisher");
        Assert.Empty(result.Problems);
        return result.Number;
    }

    private sealed class Released(string content, string database) : DemoSite
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Dastan:Content", content);
            builder.UseSetting("Dastan:Published", "true");
            builder.UseSetting("Dastan:Database", database);
        }
    }

    private async Task<HttpStatusCode> StoryPage()
    {
        using var site = new Released(SiteContent, _database);
        return (await site.Visitor().Get($"/en/{Story}")).Status;
    }

    private async Task<string> WhyNot()
    {
        using var site = new Released(SiteContent, _database);
        var refused = await Assert.ThrowsAnyAsync<Exception>(() => site.Visitor().Get("/en/"));
        return string.Join(" | ", Chain(refused).Select(exception => exception.Message));

        static IEnumerable<Exception> Chain(Exception? exception)
        {
            for (; exception is not null; exception = exception.InnerException)
            {
                yield return exception;
            }
        }
    }

    [Fact]
    public async Task What_was_published_is_shown_only_once_it_is_released_and_no_longer_when_it_is_withdrawn()
    {
        using var store = Editors();
        var first = Publish(store);

        // Published, and not released: the site has nothing to show and says what is missing.
        Assert.Contains("store release", await WhyNot(), StringComparison.Ordinal);

        Assert.Null(store.Release(Collection, first, "a publisher"));
        Assert.Equal(first, store.Released(Collection));
        Assert.Equal(HttpStatusCode.OK, await StoryPage());

        // The same word twice says nothing new; a revision that was never published cannot be released.
        Assert.NotNull(store.Release(Collection, first, "a publisher"));
        Assert.NotNull(store.Release(Collection, first + 5, "a publisher"));

        // A later revision is shown when it is released, and the earlier one can be shown again.
        var second = Publish(store);
        Assert.Equal(first, store.Released(Collection));
        Assert.Null(store.Release(Collection, second, "a publisher"));
        Assert.Equal(HttpStatusCode.OK, await StoryPage());
        Assert.Null(store.Release(Collection, first, "a publisher"));
        Assert.Equal(first, store.Released(Collection));

        Assert.Null(store.Withdraw(Collection, "a publisher"));
        Assert.Null(store.Released(Collection));
        Assert.NotNull(store.Withdraw(Collection, "a publisher"));
        Assert.Contains("no released revision", await WhyNot(), StringComparison.Ordinal);

        // What was published is still kept.
        Assert.Equal(2, store.Revisions(Collection).Length);
    }

    [Fact]
    public async Task A_revision_that_is_no_longer_the_text_it_was_published_as_is_not_shown()
    {
        using (var store = Editors())
        {
            Assert.Null(store.Release(Collection, Publish(store), "a publisher"));
        }

        Assert.Equal(HttpStatusCode.OK, await StoryPage());

        // Somebody changes the revision in the database, past the store.
        using (var connection = new NpgsqlConnection(_database))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """update published."Revisions" set "Json" = replace("Json"::text, 'Rostam', 'Somebody')::json""";
            Assert.Equal(1, command.ExecuteNonQuery());
        }

        Assert.Contains("its hash does not match", await WhyNot(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_told_to_read_the_database_does_not_start_without_one_or_without_its_tables()
    {
        using (var site = new Released(SiteContent, string.Empty))
        {
            var refused = await Assert.ThrowsAnyAsync<Exception>(() => site.Visitor().Get("/en/"));
            Assert.Contains("Dastan:Database", refused.ToString(), StringComparison.Ordinal);
        }

        using var empty = new Released(SiteContent, TestDatabase.Empty());
        var early = await Assert.ThrowsAnyAsync<Exception>(() => empty.Visitor().Get("/en/"));
        Assert.Contains("database migrate", early.ToString(), StringComparison.Ordinal);
    }
}
