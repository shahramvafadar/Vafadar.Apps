using System.Collections.Immutable;

namespace Vafadar.Dastan.Data;

/// <summary>One of the values a field of a content file may have.</summary>
/// <param name="Name">The value as it is written in the file.</param>
/// <param name="Note">What is special about the value, if anything.</param>
public sealed record ContentChoice(string Name, string? Note = null);

/// <summary>A field of a content file that takes one of a fixed set of values.</summary>
/// <param name="Field">The place of the field in the file, as a path: <c>relations[].type</c>.</param>
/// <param name="Meaning">What the field says.</param>
/// <param name="Values">The values the readers understand; any other value is refused.</param>
public sealed record ContentChoices(string Field, string Meaning, ImmutableArray<ContentChoice> Values);

/// <summary>The texts of the records of a group that a translation may give in its language.</summary>
/// <param name="Group">The group of the translation: a group of records, or <c>dated</c> for the statements with a date.</param>
/// <param name="Fields">The fields of a record that are translated.</param>
/// <param name="Roles">Whether the roles of what takes part in a record are translated, by the identifier of what takes part.</param>
/// <param name="Places">Whether the captions a record has in the places it stands in are translated, by the identifier of the place.</param>
/// <param name="Notes">Whether the notes of the stops of a record are translated, by the identifier of what the stop names.</param>
public sealed record ContentTexts(string Group, ImmutableArray<string> Fields, bool Roles = false, bool Notes = false, bool Facts = false, bool Places = false);

/// <summary>
/// The fixed sets of values of the content files, taken from the readers themselves – a list for writers that
/// cannot differ from what is accepted (D-042). A value that is missing here does not exist for the site: it is
/// added in code, with its meaning, before content may use it.
/// </summary>
public static class ContentVocabulary
{
    /// <summary>Gets the fixed sets of the file of a collection (<see cref="PilotSample.SchemaVersion"/>) and of its translations.</summary>
    public static ImmutableArray<ContentChoices> Collection { get; } = PilotReader.Choices();

    /// <summary>
    /// Returns the type of entity a record names in its field <c>type</c>, or nothing for a word that names
    /// none: the tool of the editors asks for the facts of the profile of that type (ADR 0017).
    /// </summary>
    public static Vafadar.Dastan.Core.EntityType? EntityType(string? written) => written is null ? null : PilotReader.TypeNamed(written);

    /// <summary>Gets the texts of a collection that its translations may translate; the readers refuse any other.</summary>
    public static ImmutableArray<ContentTexts> Translated => PilotReader.Translated;

    /// <summary>Gets the fixed sets of the file of a comparison (<see cref="ComparisonSample.FormatVersion"/>) and of its translations.</summary>
    public static ImmutableArray<ContentChoices> Comparison { get; } = ComparisonSample.Choices();

    /// <summary>
    /// Gets the fixed sets of the list of kinds (<see cref="KindsFile.FormatVersion"/>) and of its translations
    /// (ADR 0029). The kinds themselves are the list of a content root: a record names them in <c>kinds</c>.
    /// </summary>
    public static ImmutableArray<ContentChoices> Kinds { get; } = KindsFile.Choices();
}
