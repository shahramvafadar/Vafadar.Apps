using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>A tag action keeps its stored identity separate from its direction-safe display caption (D-89).</summary>
/// <param name="Value">The original normalized tag passed unchanged to AddTag.</param>
public sealed record TagSuggestion(string Value)
{
    /// <summary>Gets the complete action caption, including the existing bidirectional tag marker.</summary>
    public string Caption => $"+ {EntryTags.Display(Value)}";
}
