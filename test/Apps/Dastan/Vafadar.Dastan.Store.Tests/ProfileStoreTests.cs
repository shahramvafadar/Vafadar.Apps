namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The profile of an entity in the store of the editors (step 19.5, ADR 0017): its picture and the entities
/// its facts name are links like every other – what is named cannot be taken out from under it – and what a
/// profile lacks is a note for who publishes, never a fault.
/// </summary>
public sealed class ProfileStoreTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    [Fact]
    public void The_picture_and_what_a_fact_names_are_links_of_the_record()
    {
        using var store = _copy.Filled();

        var horse = WorkingCopy.Stored(store, "entities", "character-rakhsh");
        horse["picture"] = "media-met-452419";
        Assert.True(store.Put(Collection, "entities", horse, "a writer", "A picture of the profile.").Done);

        var links = store.LinksFrom(Collection, "entities", "character-rakhsh");

        Assert.Contains(new RecordLinkInfo("entities", "character-rakhsh", "picture", "media", "media-met-452419"), links);
        Assert.Contains(new RecordLinkInfo("entities", "character-rakhsh", "fact-entity", "entities", "character-rostam"), links);
        Assert.Contains(links, link => link is { Role: "fact-source", ToGroup: "sources", ToId: "src-ganjoor-s02" });
        Assert.Contains(store.LinksTo(Collection, "media", "media-met-452419"), link => link is { FromId: "character-rakhsh", Role: "picture" });
    }

    [Fact]
    public void A_media_item_that_is_a_picture_of_a_profile_is_not_taken_out_from_under_it()
    {
        using var store = _copy.Filled();
        var horse = WorkingCopy.Stored(store, "entities", "character-rakhsh");
        horse["picture"] = "media-met-452419";
        Assert.True(store.Put(Collection, "entities", horse, "a writer", "A picture of the profile.").Done);

        var removed = store.Remove(Collection, "media-met-452419", "a writer", "No longer wanted.", "media");

        Assert.False(removed.Done);
        Assert.Contains(removed.Problems, problem => problem.Contains("its picture media-met-452419 is no media item of the collection.", StringComparison.Ordinal));
        Assert.NotNull(store.Record(Collection, "media", "media-met-452419"));
    }

    [Fact]
    public void What_a_profile_lacks_is_a_note_for_who_publishes()
    {
        using var store = _copy.Filled();

        var notes = store.ProfileNotes(Collection);

        // Rostam states the fact his kind needs and has no picture; Zal has neither.
        var rostam = Assert.Single(notes, note => note.Entity == "character-rostam");
        Assert.True(rostam.NoPicture);
        Assert.Empty(rostam.Missing);
        var zal = Assert.Single(notes, note => note.Entity == "character-zal");
        Assert.True(zal.NoPicture);
        Assert.Equal(["role"], zal.Missing);
        Assert.Equal("Zal", zal.Label);
        var work = Assert.Single(notes, note => note.Entity == "work-shahnameh");
        Assert.Equal(["creator", "created"], work.Missing);
        Assert.Empty(store.ProfileNotes("no-such-collection"));
    }
}
