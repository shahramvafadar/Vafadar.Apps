using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// Saying in the tool that an entity is the same as another one (step 13.6): the form on the page of an
/// entity, the way to the entity that stays, and a merge that is refused with the reason.
/// </summary>
public sealed class StudioMergeTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Other = $"/en/{Collection}/record/entities/person-other";

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioMergeTests()
    {
        _desk = new Desk(_studio);
        using var store = _studio.Store();
        foreach (var (id, label) in new[] { ("person-poet", "A poet of the tests"), ("person-other", "The same poet under another name") })
        {
            Assert.True(store.Put(Collection, "entities", JsonNode.Parse(
                $$"""
                { "id": "{{id}}", "type": "Person", "label": "{{label}}", "summary": "Test data.", "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0 }
                """)!.AsObject(), "a writer", "Test data.").Done);
        }

        Assert.True(store.Put(Collection, "relations", JsonNode.Parse(
            """
            {
              "id": "world-written", "from": "person-other", "to": "work-shahnameh", "type": "authorOf", "label": "Author of",
              "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "source_backed_editorial_review_pending",
              "scope": { "kind": "outside_the_story", "eventIds": [] }, "directionality": "directed"
            }
            """)!.AsObject(), "a writer", "Test data.").Done);
    }

    public void Dispose() => _studio.Dispose();

    [Theory]
    [InlineData("en", "The same as another name")]
    [InlineData("fa", "همان است که نامی دیگر")]
    public async Task The_page_of_an_entity_offers_to_say_that_it_is_the_same_as_another_and_no_other_page_does(string language, string title)
    {
        var entity = await _desk.Get($"/{language}/{Collection}/record/entities/person-other");
        var relation = await _desk.Get($"/{language}/{Collection}/record/relations/world-written");

        Assert.Contains(title, entity.Text, StringComparison.Ordinal);
        Assert.Contains("name=\"_handler\" value=\"merge\"", entity.Html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"person-poet\"", entity.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<option value=\"person-other\"", entity.Html[entity.Html.IndexOf("merge-into", StringComparison.Ordinal)..entity.Html.IndexOf("Merging.Reason", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"merge\"", relation.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_entity_is_merged_into_another_and_the_way_leads_to_the_one_that_stays()
    {
        var page = await _desk.Fill(Other, "merge", ("Merging.Into", "person-poet"), ("Merging.By", "an editor"), ("Merging.Reason", "Both are the poet."));
        var gone = await _desk.Get(Other);

        Assert.EndsWith("record/entities/person-poet?done=merged", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("The other record was merged into this one.", page.Text, StringComparison.Ordinal);
        Assert.Contains("The same poet under another name", page.Text, StringComparison.Ordinal);
        Assert.Contains("Both are the poet. (person-other merged into person-poet)", gone.Text, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal("person-poet", (string?)store.Record(Collection, "relations", "world-written")!.Body["from"]);
        Assert.Null(store.Record(Collection, "entities", "person-other"));
    }

    [Fact]
    public async Task A_merge_without_the_other_entity_or_one_the_collection_cannot_bear_is_refused_with_the_reason()
    {
        var nameless = await _desk.Fill(Other, "merge", ("Merging.Into", string.Empty), ("Merging.By", "an editor"), ("Merging.Reason", "A try."));
        var place = await _desk.Fill(Other, "merge", ("Merging.Into", "place-samangan"), ("Merging.By", "an editor"), ("Merging.Reason", "A try."));

        Assert.Contains("Choose the name this one is the same as.", nameless.Text, StringComparison.Ordinal);
        Assert.Contains("authorOf reads from", place.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", place.Arrived ?? string.Empty, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.NotNull(store.Record(Collection, "entities", "person-other"));
    }
}
