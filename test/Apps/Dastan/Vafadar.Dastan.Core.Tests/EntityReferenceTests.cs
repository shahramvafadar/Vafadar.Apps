namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A reference to an entity of a collection (step 20.4, ADR 0023): the collection and the entity, written so
/// that it is read back as the same two parts and nothing else is taken for one.
/// </summary>
public sealed class EntityReferenceTests
{
    [Fact]
    public void A_reference_is_read_back_as_it_was_written()
    {
        var reference = new EntityReference("story-rostam-sohrab", new EntityId("place-university"));

        Assert.Equal("story-rostam-sohrab/place-university", reference.Write());
        Assert.True(EntityReference.TryRead(reference.Write(), out var read));
        Assert.Equal(reference, read);
        Assert.Equal(reference, EntityReference.Read("story-rostam-sohrab/place-university"));
    }

    [Fact]
    public void The_same_identifier_in_two_collections_is_two_references()
    {
        var one = new EntityReference("story", new EntityId("place-university"));
        var other = new EntityReference("club", new EntityId("place-university"));

        Assert.NotEqual(one, other);
        Assert.NotEqual(one.Write(), other.Write());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("place-university")]
    [InlineData("/place-university")]
    [InlineData("story/")]
    [InlineData("story/place/university")]
    [InlineData("story/place university")]
    [InlineData("story/place?x=1")]
    [InlineData("../story/place")]
    [InlineData("story/دانشگاه")]
    public void What_is_no_reference_is_read_as_none(string? written)
    {
        Assert.False(EntityReference.TryRead(written, out var read));
        Assert.Null(read);
        Assert.Null(EntityReference.Read(written));
    }

    [Fact]
    public void A_reference_that_is_too_long_is_none()
    {
        Assert.Null(EntityReference.Read("story/" + new string('a', EntityReference.Longest)));
        Assert.NotNull(EntityReference.Read("story/" + new string('a', EntityReference.Longest - 6)));
    }

    [Fact]
    public void A_directory_of_nothing_finds_nothing_and_calls_a_reference_the_same_as_itself_only()
    {
        var reference = new EntityReference("story", new EntityId("place-university"));

        Assert.Null(Institutions.None.Find(reference));
        Assert.Null(Institutions.None.Find(null));
        Assert.Empty(Institutions.None.Search("university"));
        Assert.Equal([reference], Institutions.None.Same(reference));
    }
}
