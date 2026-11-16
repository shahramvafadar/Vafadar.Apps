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

/// <summary>
/// The fixed sets of values of the content files, taken from the readers themselves – a list for writers that
/// cannot differ from what is accepted (D-042). A value that is missing here does not exist for the site: it is
/// added in code, with its meaning, before content may use it.
/// </summary>
public static class ContentVocabulary
{
    /// <summary>Gets the fixed sets of the file of a collection (<see cref="PilotSample.SchemaVersion"/>) and of its translations.</summary>
    public static ImmutableArray<ContentChoices> Collection { get; } = PilotReader.Choices();

    /// <summary>Gets the fixed sets of the file of a comparison (<see cref="ComparisonSample.FormatVersion"/>) and of its translations.</summary>
    public static ImmutableArray<ContentChoices> Comparison { get; } = ComparisonSample.Choices();
}
