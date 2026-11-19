using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// Acts of several editors on one collection that arrive at the same moment (step 19.3, ADR 0015): each has a
/// connection of its own, as each request of the tool has, and all begin together. An act keeps the collection
/// as it read it – so they are done one after the other, and none puts back what another has just changed.
/// </summary>
public sealed class EditorsAtTheSameMomentTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private const int Editors = 8;

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    [Fact]
    public async Task What_every_editor_put_at_the_same_moment_is_all_in_the_collection_afterwards()
    {
        _copy.Filled().Dispose();

        using var start = new ManualResetEventSlim();
        var acting = Enumerable.Range(0, Editors).Select(number => Task.Run(() =>
        {
            using var store = _copy.Open();
            var source = JsonNode.Parse(
                $$"""
                { "id": "src-moment-{{number}}", "title": "A source of editor {{number}}", "url": "https://example.org/moment-{{number}}",
                  "kind": "primary_literary_text_transcription", "accessedAt": "2026-11-17", "spoilerLevel": 0 }
                """)!.AsObject();
            start.Wait();
            return store.Put(Collection, "sources", source, $"editor {number}", "A source of my own.");
        })).ToArray();
        start.Set();
        var done = await Task.WhenAll(acting);

        Assert.All(done, act => Assert.True(act.Done, string.Join('\n', act.Problems)));
        using var after = _copy.Open();
        var sources = JsonNode.Parse(after.Export(Collection)!.Json)!["sources"]!.AsArray().Select(source => (string?)source!["id"]).ToList();
        for (var number = 0; number < Editors; number++)
        {
            Assert.Contains($"src-moment-{number}", sources);
            Assert.Single(after.History(Collection, "sources", $"src-moment-{number}"));
        }

        // Each act saw what the one before it kept: no two sources stand at the same place of their group.
        using var connection = new Npgsql.NpgsqlConnection(_copy.Database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""select count(*) - count(distinct "Position") from {EditorialStore.Schema}."Records" where "Group" = 'sources'""";
        Assert.Equal(0L, command.ExecuteScalar());
    }
}
