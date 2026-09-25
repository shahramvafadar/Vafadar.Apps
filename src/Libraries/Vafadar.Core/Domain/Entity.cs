namespace Vafadar.Core.Domain;

/// <summary>
/// Base class for domain entities identified by a <see cref="Guid"/>.
/// </summary>
/// <remarks>
/// New identifiers are version 7 GUIDs. They are time ordered, which keeps database indexes compact, and they are
/// globally unique, so records created on different devices (or restored from a backup) never collide.
/// </remarks>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>Initializes a new entity with a new version 7 identifier.</summary>
    protected Entity()
        : this(Guid.CreateVersion7())
    {
    }

    /// <summary>Initializes an entity with an existing identifier.</summary>
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entity identifier must not be empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Gets the unique identifier of the entity.</summary>
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public bool Equals(Entity? other) =>
        other is not null && (ReferenceEquals(this, other) || (other.GetType() == GetType() && other.Id == Id));

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Compares two entities by type and identifier.</summary>
    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    /// <summary>Compares two entities by type and identifier.</summary>
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
