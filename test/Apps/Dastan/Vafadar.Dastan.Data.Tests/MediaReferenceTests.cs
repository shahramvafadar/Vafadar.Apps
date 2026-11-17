using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// A work that may not be shown, as a reference (step 10.3): the projection names it by its catalogue record
/// within the reading boundary, and it is no media item of the visitor. Read from the sample of the repository,
/// whose three museum works are such references.
/// </summary>
public sealed class MediaReferenceTests
{
    private static readonly Catalogue Catalogue = SampleFiles.Read();

    private static Projection At(int level) => Projection.Of(Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    [Theory]
    [InlineData(0, new[] { "media-met-452419" })]
    [InlineData(1, new[] { "media-met-452419", "media-umma-1963-1-46" })]
    [InlineData(4, new[] { "media-met-452419", "media-met-453265", "media-umma-1963-1-46" })]
    public void A_reference_is_named_from_the_reading_stage_of_its_work_on(int level, string[] expected)
    {
        Assert.Equal(expected, At(level).References.Select(reference => reference.Id.Value));
    }

    [Fact]
    public void A_reference_is_no_media_item_of_the_visitor()
    {
        var projection = At(4);
        var id = new MediaId("media-met-452419");

        Assert.Null(projection.Find(id));
        Assert.Null(projection.MediaItem(id));
        Assert.Null(projection.Evidence(id));
        Assert.DoesNotContain(projection.Media, item => item.Id == id);
        Assert.DoesNotContain(projection.Timeline().Made, item => item.Id == id);
    }

    [Fact]
    public void A_reference_holds_the_catalogue_record_and_the_way_to_the_holder()
    {
        var reference = At(1).References.Single(item => item.Id.Value == "media-umma-1963-1-46");

        Assert.Equal("Tahmina Comes to Rustam", reference.CatalogueTitle);
        Assert.Equal("1963/1.46", reference.AccessionNumber);
        Assert.Equal(RightsStatus.PermissionNotVerified, reference.Rights);
        Assert.Equal("umma.umich.edu", reference.CataloguePage.Host);
        Assert.Equal(["character-tahmineh", "character-rostam"], reference.DepictedEntities.Select(entity => entity.Value));
    }

    [Fact]
    public void An_item_that_is_shown_is_not_also_a_reference_and_one_without_a_page_elsewhere_is_none()
    {
        var all = At(4);

        // The two diagrams of the project are shown in a demonstration and have no page at an institution.
        Assert.Equal(2, all.Media.Length);
        Assert.DoesNotContain(all.References, reference => all.Media.Any(item => item.Id == reference.Id));
        Assert.All(all.References, reference => Assert.NotNull(reference.CataloguePage));
    }

    [Fact]
    public void The_galleries_of_a_name_and_of_a_step_hold_the_references_that_show_them()
    {
        var projection = At(1);

        Assert.Equal(["media-met-452419"], projection.Gallery(new EntityId("character-rakhsh"))!.References.Select(item => item.Id.Value));
        Assert.Equal(["media-umma-1963-1-46"], projection.Gallery(new EventId("event-02"))!.References.Select(item => item.Id.Value));
        Assert.True(projection.Gallery(new EntityId("place-samangan"))!.IsEmpty);
        Assert.False(projection.Gallery(new EntityId("character-rakhsh"))!.IsEmpty);
    }
}
