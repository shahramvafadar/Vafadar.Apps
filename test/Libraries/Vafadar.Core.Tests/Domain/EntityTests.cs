using Vafadar.Core.Domain;

namespace Vafadar.Core.Tests.Domain;

public sealed class EntityTests
{
    [Fact]
    public void New_entity_gets_a_version_7_identifier()
    {
        var entity = new Note();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);
    }

    [Fact]
    public void New_entities_get_unique_identifiers()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => new Note().Id).ToHashSet();

        Assert.Equal(1000, ids.Count);
    }

    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(new Note(id), new Note(id));
        Assert.True(new Note(id) == new Note(id));
        Assert.Equal(new Note(id).GetHashCode(), new Note(id).GetHashCode());
    }

    [Fact]
    public void Entities_of_different_types_are_never_equal()
    {
        var id = Guid.CreateVersion7();

        Assert.False(new Note(id).Equals(new Tag(id)));
        Assert.True(new Note(id) != new Tag(id));
    }

    [Fact]
    public void Empty_identifier_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Note(Guid.Empty));
    }

    private sealed class Note : Entity
    {
        public Note()
        {
        }

        public Note(Guid id)
            : base(id)
        {
        }
    }

    private sealed class Tag(Guid id) : Entity(id);
}
