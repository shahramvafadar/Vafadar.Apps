using System.Diagnostics.CodeAnalysis;

namespace Vafadar.Dastan.Core;

/// <summary>
/// Names one entity among everything the site has (step 20.4, ADR 0023): the collection and the entity in it.
/// The identifier of an entity is of its collection – two collections can each have a <c>org-school</c> – so
/// only the two together cannot be mistaken. A reference never comes from a name: two entities of one name are
/// two, and whether an entity of one collection is the same as an entity of another is said by the editors
/// (<see cref="Institutions"/>), not concluded.
/// </summary>
/// <param name="Collection">The identifier of the collection.</param>
/// <param name="Entity">The identifier of the entity in it.</param>
public sealed record EntityReference(string Collection, EntityId Entity)
{
    /// <summary>The greatest length of a reference as it is written.</summary>
    public const int Longest = 250;

    /// <summary>Writes the reference as one word: the collection, a slash, the entity.</summary>
    public string Write() => $"{Collection}/{Entity.Value}";

    /// <inheritdoc />
    public override string ToString() => Write();

    /// <summary>
    /// Reads a reference as <see cref="Write"/> writes it. Returns <see langword="false"/> for anything else:
    /// nothing, no slash, a part that is empty or has a letter an identifier never has, something too long.
    /// </summary>
    public static bool TryRead([NotNullWhen(true)] string? written, [NotNullWhen(true)] out EntityReference? reference)
    {
        reference = null;
        if (written is not { Length: > 2 and <= Longest } || written.Split('/') is not [var collection, var entity] || !IsIdentifier(collection) || !IsIdentifier(entity))
        {
            return false;
        }

        reference = new(collection, new EntityId(entity));
        return true;
    }

    /// <summary>Reads a reference, or returns nothing when it is none.</summary>
    public static EntityReference? Read(string? written) => TryRead(written, out var reference) ? reference : null;

    // What the identifiers of the collections are made of: letters, digits, and the marks between words.
    private static bool IsIdentifier(string part) =>
        part.Length > 0 && part.All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '-' or '_' or '.');
}
