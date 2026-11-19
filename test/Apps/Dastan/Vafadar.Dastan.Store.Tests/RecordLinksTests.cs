using System.Text.Json.Nodes;
using Npgsql;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// What the database knows about the records beside their written form (step 19.3, ADR 0015, D-081): which
/// record names which, as rows with foreign keys; and the columns it reads out of a record itself. The rows
/// follow every act, and the database refuses what does not hold – whoever writes to it.
/// </summary>
public sealed class RecordLinksTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private T Ask<T>(string sql)
    {
        using var connection = new NpgsqlConnection(_copy.Database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)command.ExecuteScalar()!;
    }

    private PostgresException Refused(string sql) => Assert.Throws<PostgresException>(() => Ask<object>(sql));

    [Fact]
    public void Every_record_a_record_names_is_a_row_and_the_rows_say_what_the_collection_says()
    {
        using var store = _copy.Filled();
        var sample = _copy.Working();

        // Every link of every record of the file, and not one more.
        var written = EditorialStore.Groups
            .SelectMany(group => (sample[group] as JsonArray ?? []).SelectMany(record => EditorialStore.LinksOf(group, record!.AsObject())
                .Select(link => (group, (string)record["id"]!, link.Role, link.Ordinal, link.ToGroup, link.ToId))))
            .ToList();
        Assert.NotEmpty(written);
        Assert.Equal((long)written.Count, Ask<long>("""select count(*) from editorial."RecordLinks" """));

        var relation = WorkingCopy.Record(sample, "relations", "relation-01");
        var ends = store.LinksFrom(Collection, "relations", "relation-01");
        Assert.Contains(new RecordLinkInfo("relations", "relation-01", "from", "entities", (string)relation["from"]!), ends);
        Assert.Contains(new RecordLinkInfo("relations", "relation-01", "to", "entities", (string)relation["to"]!), ends);
        Assert.Equal(relation["sourceIds"]!.AsArray().Select(source => (string)source!), ends.Where(link => link.Role == "source").Select(link => link.ToId));

        // Asked from the other side: who names Rostam.
        var users = store.LinksTo(Collection, "entities", "character-rostam");
        Assert.Equal(written.Count(link => link.ToGroup == "entities" && link.ToId == "character-rostam"), users.Length);
        Assert.Contains(users, link => link.FromGroup == "events" && link.Role == "associated");
    }

    [Fact]
    public void The_rows_follow_an_act_a_changed_end_is_changed_and_the_links_of_a_removed_record_are_gone()
    {
        using var store = _copy.Filled();
        var relation = WorkingCopy.Stored(store, "relations", "relation-02");
        var before = (string)relation["to"]!;
        var other = before == "character-rakhsh" ? "character-rostam" : "character-rakhsh";
        relation["to"] = other;

        var changed = store.Put(Collection, "relations", relation, "a writer", "The other end.");
        if (changed.Done)
        {
            Assert.Contains(new RecordLinkInfo("relations", "relation-02", "to", "entities", other), store.LinksFrom(Collection, "relations", "relation-02"));
            Assert.DoesNotContain(store.LinksTo(Collection, "entities", before), link => link is { FromId: "relation-02", Role: "to" });
        }
        else
        {
            // The readers did not take the other end; then the rows are as they were.
            Assert.Contains(new RecordLinkInfo("relations", "relation-02", "to", "entities", before), store.LinksFrom(Collection, "relations", "relation-02"));
        }

        // A source nobody names is added, named by nobody, and taken out again.
        var source = JsonNode.Parse(
            """
            { "id": "src-linked", "title": "A source", "url": "https://example.org/linked", "kind": "primary_literary_text_transcription",
              "accessedAt": "2026-11-17", "spoilerLevel": 0 }
            """)!.AsObject();
        Assert.True(store.Put(Collection, "sources", source, "a writer", "A source.").Done);
        Assert.Empty(store.LinksTo(Collection, "sources", "src-linked"));
        var links = Ask<long>("""select count(*) from editorial."RecordLinks" """);
        Assert.True(store.Remove(Collection, "src-linked", "a writer", "Not needed.").Done);
        Assert.Equal(links, Ask<long>("""select count(*) from editorial."RecordLinks" """));
    }

    [Fact]
    public void The_database_reads_the_kind_the_name_the_level_and_the_review_out_of_the_record_itself()
    {
        using var store = _copy.Filled();
        var sample = _copy.Working();
        var rostam = WorkingCopy.Record(sample, "entities", "character-rostam");
        var relation = WorkingCopy.Record(sample, "relations", "relation-02");

        Assert.Equal((string)rostam["type"]!, Ask<string>("""select "Kind" from editorial."Records" where "RecordId" = 'character-rostam'"""));
        Assert.Equal((string)rostam["label"]!, Ask<string>("""select "Label" from editorial."Records" where "RecordId" = 'character-rostam'"""));
        Assert.Equal((int)rostam["spoilerLevel"]!, Ask<int>("""select "SpoilerLevel" from editorial."Records" where "RecordId" = 'character-rostam'"""));
        Assert.Equal((string)relation["reviewStatus"]!, Ask<string>("""select "ReviewStatus" from editorial."Records" where "RecordId" = 'relation-02'"""));
        Assert.Equal((string)sample["sources"]![0]!["kind"]!, Ask<string>($"""select "Kind" from editorial."Records" where "Group" = 'sources' and "Position" = 0"""));

        // A review changes the record; the column says so without anybody writing it.
        Assert.True(store.Review(Collection, "relation-02", ReviewAct.Reviewed, "a reviewer", new DateOnly(2026, 11, 17), "Read against the text.").Done);
        Assert.Equal("reviewed", Ask<string>("""select "ReviewStatus" from editorial."Records" where "RecordId" = 'relation-02'"""));
    }

    [Fact]
    public void The_database_refuses_what_does_not_hold_whoever_writes_to_it()
    {
        _copy.Filled().Dispose();

        // A link to a record the collection does not have.
        var missing = Refused($"""
            insert into editorial."RecordLinks" ("CollectionId", "FromGroup", "FromId", "Role", "Ordinal", "ToGroup", "ToId")
            values ('{Collection}', 'relations', 'relation-01', 'to', 9, 'entities', 'nobody')
            """);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, missing.SqlState);

        // A record that others name cannot go.
        var named = Refused("""delete from editorial."Records" where "Group" = 'entities' and "RecordId" = 'character-rostam'""");
        Assert.Equal(PostgresErrorCodes.RestrictViolation, named.SqlState);

        // A body that is no JSON, and a body that is the record of another identifier.
        Assert.Equal(PostgresErrorCodes.InvalidTextRepresentation, Refused("""update editorial."Records" set "Body" = '{ not json' where "RecordId" = 'character-rostam'""").SqlState);
        Assert.Equal(PostgresErrorCodes.CheckViolation, Refused("""update editorial."Records" set "Body" = '{ "id": "somebody-else" }' where "RecordId" = 'character-rostam'""").SqlState);

        // The columns read out of a record cannot be written.
        Assert.StartsWith("428", Refused("""update editorial."Records" set "Kind" = 'place' where "RecordId" = 'character-rostam'""").SqlState, StringComparison.Ordinal);
    }
}
