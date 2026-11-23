using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Testing;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The themes of a gallery (R-MED-02): a theme is a record of the collection – an entity of the type
/// <c>Theme</c> – and a media item names the themes it is gathered under. A theme is a grouping of the editors:
/// it may rest on no source, nothing depicts it, and it is offered only with what a reader may see under it.
/// </summary>
public sealed class MediaThemesTests
{
    private const string Combat = "theme-combat";
    private const string Lineage = "theme-lineage";
    private const string Painting = "media-met-452419";
    private const string Ending = "media-met-453265";
    private const string HeldElsewhere = "media-umma-1963-1-46";
    private const string Video = "media-dastan-parentage-video";
    private const string Illustration = "media-dastan-family-illustration";

    private static JsonObject Theme(string id, string label, int level, string? source = null)
    {
        var theme = new JsonObject { ["id"] = id, ["type"] = "Theme", ["label"] = label, ["summary"] = $"What the editors gathered under {label}.", ["spoilerLevel"] = level };
        if (source is not null)
        {
            theme["sourceIds"] = new JsonArray(source);
        }

        return theme;
    }

    /// <summary>Writes two themes onto the sample: one from the start that rests on no source, one of the third stage with a source.</summary>
    internal static void Themed(JsonObject sample)
    {
        var entities = sample["entities"]!.AsArray();
        entities.Add(Theme(Combat, "Combat", 0));
        entities.Add(Theme(Lineage, "Lineage", 2, "src-ganjoor-s02"));
        sample.Record("media", Painting)["themeIds"] = new JsonArray(Combat);
        sample.Record("media", Ending)["themeIds"] = new JsonArray(Combat);
        sample.Record("media", HeldElsewhere)["themeIds"] = new JsonArray(Combat);
        sample.Record("media", Video)["themeIds"] = new JsonArray(Lineage);
        sample.Record("media", Illustration)["themeIds"] = new JsonArray(Lineage, Combat);
    }

    private static Projection At(int level) => Projection.Of(SampleFiles.Read(Themed), new ReadingLevel(level), Audience.LocalDemonstration)!;

    [Fact]
    public void A_theme_is_an_entity_that_may_rest_on_no_source_and_an_item_names_its_themes_in_their_order()
    {
        var catalogue = SampleFiles.Read(Themed);

        var combat = catalogue.Entities.Single(entity => entity.Id.Value == Combat);
        Assert.Same(EntityTypes.Theme, combat.Type);
        Assert.Equal(EntityNature.Subject, combat.Type.Nature);
        Assert.Empty(combat.Sources);
        Assert.Equal([new SourceId("src-ganjoor-s02")], catalogue.Entities.Single(entity => entity.Id.Value == Lineage).Sources);
        Assert.Equal([new EntityId(Lineage), new EntityId(Combat)], catalogue.Media.Single(item => item.Id.Value == Illustration).Themes);

        // An item that names none is under none, and what it shows is what it showed.
        Assert.Empty(SampleFiles.Read().Media.SelectMany(item => item.Themes));
        Assert.DoesNotContain(new EntityId(Combat), catalogue.Media.SelectMany(item => item.DepictedEntities));
    }

    [Fact]
    public void The_list_for_writers_names_the_type()
    {
        var types = ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].type");
        Assert.Contains(types.Values, value => value.Name == "Theme" && value.Note!.Contains("themeIds", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0, new[] { Combat }, new[] { 1 })]
    [InlineData(1, new[] { Combat }, new[] { 2 })]
    [InlineData(2, new[] { Combat, Lineage }, new[] { 3, 2 })]
    [InlineData(4, new[] { Combat, Lineage }, new[] { 4, 2 })]
    public void A_reader_is_offered_the_themes_that_have_something_under_them_as_far_as_was_read(int level, string[] themes, int[] items)
    {
        var reader = At(level);
        var offered = reader.Themes();

        Assert.Equal(themes, offered.Select(theme => theme.Theme.Id.Value));
        Assert.Equal(items, offered.Select(theme => theme.Items));
        Assert.Equal(offered, reader.Gallery().Themes);
    }

    [Fact]
    public void The_gallery_of_a_theme_holds_what_is_under_it_and_names_what_is_held_elsewhere()
    {
        var reader = At(4);
        var gallery = reader.Gallery(new EntityId(Combat))!;

        Assert.Equal(GalleryRule.OfTheme, gallery.Rule);
        // What may be shown is shown, in the order of the collection; what is held elsewhere is named with its holder.
        Assert.Equal([Illustration], gallery.Items.Select(item => item.Id.Value));
        Assert.Equal([Painting, Ending, HeldElsewhere], gallery.References.Select(item => item.Id.Value).Order(StringComparer.Ordinal).OrderBy(id => id == HeldElsewhere));
        Assert.DoesNotContain(gallery.Items, item => item.Id.Value == Video);
        Assert.Equal([Combat, Lineage], gallery.Themes.Select(theme => theme.Theme.Id.Value));

        // The same answer for the page of the theme; and the gallery of somebody shown is what it was.
        Assert.Equal(gallery.Items, reader.Entity(new EntityId(Combat))!.Media);
        var rostam = reader.Gallery(new EntityId("character-rostam"))!;
        Assert.Equal(GalleryRule.DepictsEntity, rostam.Rule);
        Assert.Empty(rostam.Themes);
        Assert.Equal(Projection.Of(SampleFiles.Read(), new ReadingLevel(4), Audience.LocalDemonstration)!.Gallery(new EntityId("character-rostam"))!.Items.Select(item => item.Id), rostam.Items.Select(item => item.Id));
    }

    [Fact]
    public void A_theme_of_a_later_stage_is_neither_offered_nor_found_and_an_item_names_the_themes_a_reader_has()
    {
        Assert.Null(At(1).Gallery(new EntityId(Lineage)));
        Assert.Null(At(1).Entity(new EntityId(Lineage)));

        var item = At(2).MediaItem(new MediaId(Illustration))!;
        Assert.Equal([Lineage, Combat], item.Themes.Select(theme => theme.Id.Value));
        Assert.DoesNotContain(item.Depicted, entity => entity.Type == EntityTypes.Theme);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void No_answer_about_themes_discloses_anything_of_a_later_stage(int level)
    {
        var catalogue = SampleFiles.Read(Themed);
        Assert.Empty(Disclosure.Leaks(Projection.Of(catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!, catalogue));
    }

    [Fact]
    public void A_theme_is_named_as_a_theme_and_nothing_else_is()
    {
        Assert.Equal(
            [$"{Painting}: its theme character-rostam is a character, not a theme."],
            SampleFiles.Problems(sample => sample.Record("media", Painting)["themeIds"] = new JsonArray("character-rostam")));

        Assert.Equal(
            [$"{Painting}: the theme theme-nowhere does not exist."],
            SampleFiles.Problems(sample => sample.Record("media", Painting)["themeIds"] = new JsonArray("theme-nowhere")));

        Assert.Equal(
            [$"{Painting}: {Combat} is a theme; an item names its themes in themeIds and depicts none."],
            SampleFiles.Problems(sample =>
            {
                Themed(sample);
                sample.Record("media", Painting)["depictedEntityIds"]!.AsArray().Add(Combat);
            }));

        Assert.Equal(
            [$"{Painting}: the theme {Combat} is named 2 times."],
            SampleFiles.Problems(sample =>
            {
                Themed(sample);
                sample.Record("media", Painting)["themeIds"] = new JsonArray(Combat, Combat);
            }));
    }

    [Fact]
    public void An_item_is_not_under_a_theme_of_a_later_stage_and_only_a_theme_may_rest_on_no_source()
    {
        // The painting is shown from the start; the theme of the third stage would give its name away.
        var later = SampleFiles.Problems(sample =>
        {
            Themed(sample);
            sample.Record("media", Painting)["themeIds"] = new JsonArray(Lineage);
        });
        Assert.Equal([$"{Painting}: its theme {Lineage} belongs to a later reading level and would give the story away."], later);

        var unsupported = SampleFiles.Problems(sample => sample.Record("entities", "character-rostam").Remove("sourceIds"));
        Assert.Contains(unsupported, problem => problem.Contains("sourceIds", StringComparison.Ordinal));
    }
}
