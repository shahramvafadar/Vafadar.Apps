namespace Vafadar.Zanance.Core.Accounts;

/// <summary>Where the account of a new entry came from (ZEX-S0103).</summary>
public enum EntryAccountSource
{
    /// <summary>No account: the user must choose one; Save stays disabled until then.</summary>
    None = 0,

    /// <summary>The user chose it in the form.</summary>
    Explicit = 1,

    /// <summary>The context: a template, the account page, a plan, the original entry of an edit, duplicate or refund.</summary>
    Context = 2,

    /// <summary>The default account of the settings.</summary>
    Default = 3,
}

/// <summary>The account a new entry starts with, and why.</summary>
/// <param name="AccountId">The account, or <see langword="null"/> when the user must choose one.</param>
/// <param name="Source">Where it came from.</param>
/// <param name="ContextUnavailable">
/// Whether a context account was given but cannot be used (archived or deleted), e.g. a template of an archived account:
/// the editor then says so instead of silently using another account.
/// </param>
public sealed record EntryAccountChoice(Guid? AccountId, EntryAccountSource Source, bool ContextUnavailable = false);

/// <summary>
/// The one rule for the account of a new entry, used by every path that opens the entry editor (ZEX-S0103, design §4):
/// an explicit choice wins, then the context account, then a valid default account, and otherwise nothing – never a
/// silent fallback to the first account. Holdings, debts and receivables are never chosen as a default.
/// </summary>
public static class EntryAccountContract
{
    /// <summary>
    /// Returns whether <paramref name="account"/> can be the default account of new entries: it exists, is not archived
    /// and is a money account (cash, checking, savings or credit card) – optionally in <paramref name="currencyCode"/>.
    /// </summary>
    public static bool IsValidDefault(Account? account, string? currencyCode = null) =>
        account is { IsArchived: false }
        && account.Type.CanBeDefault()
        && (currencyCode is null || string.Equals(account.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

    /// <summary>Chooses the account of a new entry.</summary>
    /// <param name="accounts">All accounts, archived ones included.</param>
    /// <param name="explicitId">The account the user chose in the form, if any.</param>
    /// <param name="contextId">The context account, if any.</param>
    /// <param name="defaultId">The default account of the settings, if any.</param>
    /// <param name="currencyCode">A required currency, e.g. the purchase currency of a refund.</param>
    public static EntryAccountChoice Choose(IReadOnlyCollection<Account> accounts, Guid? explicitId, Guid? contextId, Guid? defaultId, string? currencyCode = null)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        Account? Find(Guid? id) => id is { } value ? accounts.FirstOrDefault(a => a.Id == value) : null;

        if (Find(explicitId) is { } chosen)
        {
            return new EntryAccountChoice(chosen.Id, EntryAccountSource.Explicit);
        }

        var contextUnavailable = false;
        if (contextId is not null)
        {
            var context = Find(contextId);
            if (context is { IsArchived: false } && (currencyCode is null || string.Equals(context.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)))
            {
                return new EntryAccountChoice(context.Id, EntryAccountSource.Context);
            }

            contextUnavailable = true;
        }

        // An unusable context (e.g. an archived template account) is reported, and no other account is guessed for it.
        if (!contextUnavailable && Find(defaultId) is { } fallback && IsValidDefault(fallback, currencyCode))
        {
            return new EntryAccountChoice(fallback.Id, EntryAccountSource.Default);
        }

        return new EntryAccountChoice(null, EntryAccountSource.None, contextUnavailable);
    }

    /// <summary>
    /// Returns whether moving the typed amount from <paramref name="from"/> to <paramref name="to"/> changes its currency,
    /// so the editor must say so (ZEX-P04). The digits are kept and never converted.
    /// </summary>
    public static bool ChangesCurrency(Account? from, Account? to) =>
        from is not null && to is not null && !string.Equals(from.CurrencyCode, to.CurrencyCode, StringComparison.OrdinalIgnoreCase);
}
