namespace Vafadar.Dastan.Core.Tests;

public sealed class ModelTests
{
    [Fact]
    public void A_text_is_given_in_the_language_asked_for()
    {
        var name = LocalizedText.Of("en", "Rostam").With("fa", "رستم");

        Assert.Equal(new ResolvedText("رستم", "fa", IsFallback: false, TextReview.Reviewed), name.Resolve("fa"));
        Assert.Equal(new ResolvedText("Rostam", "en", IsFallback: false, TextReview.Reviewed), name.Resolve("en"));
    }

    [Fact]
    public void A_regional_language_finds_the_text_of_its_language()
    {
        var name = LocalizedText.Of("fa", "رستم");

        var resolved = name.Resolve("fa-IR");

        Assert.Equal("fa", resolved.Language);
        Assert.False(resolved.IsFallback);
    }

    [Fact]
    public void A_text_in_another_language_says_that_it_is_a_fallback()
    {
        var summary = LocalizedText.Of("en", "A warrior.");

        var resolved = summary.Resolve("fa", "en");

        Assert.Equal("A warrior.", resolved.Value);
        Assert.Equal("en", resolved.Language);
        Assert.True(resolved.IsFallback);
    }

    [Fact]
    public void Fallbacks_are_tried_in_their_order()
    {
        var text = LocalizedText.Of("en", "English").With("de", "Deutsch");

        Assert.Equal("de", text.Resolve("fa", "de", "en").Language);
        Assert.Equal("en", text.Resolve("fa", "es", "en").Language);
    }

    [Fact]
    public void Without_a_matching_fallback_any_language_is_given_and_marked()
    {
        var text = LocalizedText.Of("de", "Deutsch");

        var resolved = text.Resolve("fa", "en");

        Assert.Equal("de", resolved.Language);
        Assert.True(resolved.IsFallback);
    }

    [Fact]
    public void A_draft_stays_marked_as_a_draft()
    {
        var summary = LocalizedText.Of("en", "A warrior.").With("fa", "یک پهلوان.", TextReview.Draft);

        Assert.Equal(TextReview.Draft, summary.Resolve("fa").Review);
        Assert.Equal(TextReview.Reviewed, summary.Resolve("en").Review);
    }

    [Fact]
    public void Adding_a_language_leaves_the_original_text_as_it_was()
    {
        var english = LocalizedText.Of("en", "Rostam");

        var both = english.With("fa", "رستم");

        Assert.Equal(["en"], english.Languages);
        Assert.Equal(["en", "fa"], both.Languages);
        Assert.Null(english.In("fa"));
        Assert.Equal("رستم", both.In("FA")!.Value.Value);
    }

    [Fact]
    public void A_blank_text_or_language_is_refused()
    {
        Assert.Throws<ArgumentException>(() => LocalizedText.Of("en", " "));
        Assert.Throws<ArgumentException>(() => LocalizedText.Of(" ", "Rostam"));
    }

    [Fact]
    public void An_identifier_is_a_stable_text_without_blanks_around_it()
    {
        Assert.Equal("character-rostam", new EntityId("character-rostam").Value);
        Assert.Equal("character-rostam", new EntityId("character-rostam").ToString());
        Assert.Equal(new EventId("event-03"), new EventId("event-03"));
        Assert.Throws<ArgumentException>(() => new EntityId(" "));
        Assert.Throws<ArgumentException>(() => new SourceId(" src"));
        Assert.Throws<ArgumentException>(() => new MediaId("media "));
    }

    [Fact]
    public void A_reading_level_is_within_a_boundary_at_or_above_it()
    {
        var lineage = new ReadingLevel(2);

        Assert.True(lineage.IsWithin(new ReadingLevel(2)));
        Assert.True(lineage.IsWithin(new ReadingLevel(4)));
        Assert.False(lineage.IsWithin(new ReadingLevel(1)));
        Assert.True(ReadingLevel.Opening.IsWithin(ReadingLevel.Opening));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadingLevel(-1));
    }

    [Fact]
    public void Every_type_of_the_lists_has_its_own_name()
    {
        Assert.Equal(EntityTypes.All.Length, EntityTypes.All.Select(type => type.Key).Distinct().Count());
        Assert.Equal(RelationTypes.All.Length, RelationTypes.All.Select(type => type.Key).Distinct().Count());
    }

    [Fact]
    public void A_type_is_found_by_its_name_and_only_by_it()
    {
        Assert.Same(EntityTypes.Character, EntityTypes.Find("character"));
        Assert.Null(EntityTypes.Find("Character"));
        Assert.Null(EntityTypes.Find("dragon"));
        Assert.Null(RelationTypes.Find("friendOf"));
    }

    [Fact]
    public void Being_married_and_being_opponents_read_the_same_from_both_sides()
    {
        Assert.Equal(Directionality.Symmetric, RelationTypes.Find("spouseOf")!.Directionality);
        Assert.Equal(Directionality.Symmetric, RelationTypes.Find("opponentOf")!.Directionality);
        Assert.Equal(Directionality.Directed, RelationTypes.Find("parentOf")!.Directionality);
        Assert.Equal(Directionality.Symmetric, RelationTypes.Find("siblingOf")!.Directionality);
        Assert.Equal(3, RelationTypes.All.Count(type => type.Directionality == Directionality.Symmetric));
    }

    [Theory]
    [InlineData("character", EntityNature.Being)]
    [InlineData("animalCharacter", EntityNature.Being)]
    [InlineData("person", EntityNature.Being)]
    [InlineData("narrativePlace", EntityNature.Place)]
    [InlineData("narrativeRegion", EntityNature.Place)]
    [InlineData("narrativeObject", EntityNature.Object)]
    [InlineData("work", EntityNature.Work)]
    [InlineData("storyArc", EntityNature.Work)]
    [InlineData("company", EntityNature.Group)]
    [InlineData("sportsTeam", EntityNature.Group)]
    [InlineData("competitionSeason", EntityNature.Occasion)]
    public void A_type_says_what_kind_of_thing_its_entities_are(string key, EntityNature nature) =>
        Assert.Equal(nature, EntityTypes.Find(key)!.Nature);
}
