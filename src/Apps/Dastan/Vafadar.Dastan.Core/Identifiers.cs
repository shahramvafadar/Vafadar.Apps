namespace Vafadar.Dastan.Core;

// The identifiers of a collection are the stable texts of its editorial source ("character-rostam", "relation-04").
// Each kind is a type of its own, so an event cannot be passed where an entity is expected (ADR 0001).

/// <summary>Identifies an <see cref="Entity"/>.</summary>
public readonly record struct EntityId
{
    /// <summary>Creates the identifier.</summary>
    public EntityId(string value) => Value = Identifier.Checked(value);

    /// <summary>Gets the stable text of the identifier.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Identifies a <see cref="Relation"/>.</summary>
public readonly record struct RelationId
{
    /// <summary>Creates the identifier.</summary>
    public RelationId(string value) => Value = Identifier.Checked(value);

    /// <summary>Gets the stable text of the identifier.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Identifies a <see cref="StoryEvent"/>.</summary>
public readonly record struct EventId
{
    /// <summary>Creates the identifier.</summary>
    public EventId(string value) => Value = Identifier.Checked(value);

    /// <summary>Gets the stable text of the identifier.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Identifies a <see cref="Source"/>.</summary>
public readonly record struct SourceId
{
    /// <summary>Creates the identifier.</summary>
    public SourceId(string value) => Value = Identifier.Checked(value);

    /// <summary>Gets the stable text of the identifier.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Identifies a <see cref="MediaItem"/>.</summary>
public readonly record struct MediaId
{
    /// <summary>Creates the identifier.</summary>
    public MediaId(string value) => Value = Identifier.Checked(value);

    /// <summary>Gets the stable text of the identifier.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>The rule every identifier follows.</summary>
internal static class Identifier
{
    /// <summary>Returns <paramref name="value"/> when it can be an identifier: not blank and without surrounding spaces.</summary>
    public static string Checked(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length != value.Trim().Length)
        {
            throw new ArgumentException("An identifier has no surrounding spaces.", nameof(value));
        }

        return value;
    }
}
