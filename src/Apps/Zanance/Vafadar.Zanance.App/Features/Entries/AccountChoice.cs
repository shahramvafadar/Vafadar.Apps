namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>An original account identity and currency displayed in an editor picker.</summary>
public sealed record AccountChoice(Guid Id, string Name, string CurrencyCode)
{
    /// <inheritdoc />
    public override string ToString() => $"{Name} ({CurrencyCode})";
}
