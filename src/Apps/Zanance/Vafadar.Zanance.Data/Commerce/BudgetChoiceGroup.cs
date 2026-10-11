using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Original scope reference captions and availability, never a new account selection or financial value.</summary>
public sealed record BudgetChoiceAccount(Guid Id, string Name, string CurrencyCode, AccountType Type,
    bool IsArchived, bool IncludeInTotals, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    /// <summary>Captures original reference data without reading account balances or granting work rights.</summary>
    internal static BudgetChoiceAccount From(Account account) => new(account.Id, account.Name, account.CurrencyCode,
        account.Type, account.IsArchived, account.IncludeInTotals, account.CreatedAt, account.UpdatedAt);
}

/// <summary>One canonical displayed definition with every retained period; it is never a hashed financial identity.</summary>
public sealed class BudgetChoiceGroup
{
    /// <summary>Copies complete original period rows, retaining the actual current-row subset separately.</summary>
    internal BudgetChoiceGroup(IEnumerable<BudgetChoiceItem> rows, IReadOnlySet<Guid> currentIds)
    {
        var originals = rows.OrderBy(row => row.Id).ToArray();
        RepresentativeId = originals[0].Id;
        Periods = Array.AsReadOnly(originals);
        CurrentPeriods = Array.AsReadOnly(originals.Where(row => currentIds.Contains(row.Id)).ToArray());
        AccountIds = Array.AsReadOnly(originals[0].Accounts.Length == 0 ? []
            : originals[0].Accounts.Split(';').Select(id => Guid.ParseExact(id, "N")).Distinct().Order().ToArray());
        Identity = BudgetDefinitionKey.CanonicalIdentity(originals[0].CurrencyCode, originals[0].Period, AccountIds);
        CurrencyCode = originals[0].CurrencyCode.ToUpperInvariant(); Period = originals[0].Period;
    }
    /// <summary>Gets a real original row id used only as the retained native row's command relay.</summary>
    public Guid RepresentativeId { get; }
    /// <summary>Gets the scope-independent display grouping value, never an authorization grant.</summary>
    public string Identity { get; }
    /// <summary>Gets normalized original currency, without relabeling or converting any limit.</summary>
    public string CurrencyCode { get; }
    /// <summary>Gets the definition's original period kind.</summary>
    public BudgetPeriod Period { get; }
    /// <summary>Gets explicit original account references; empty retains the existing all-included-accounts meaning.</summary>
    public IReadOnlyList<Guid> AccountIds { get; }
    /// <summary>Gets every original period, including unselected, paid and historical limits.</summary>
    public IReadOnlyList<BudgetChoiceItem> Periods { get; }
    /// <summary>Gets current originals under the reviewed local date/month-start, without activating a method.</summary>
    public IReadOnlyList<BudgetChoiceItem> CurrentPeriods { get; }
    /// <summary>Gets whether this definition has a current period eligible for explicit choice.</summary>
    public bool IsEligible => CurrentPeriods.Count > 0;
    /// <summary>Creates an exact definition in the verified captured scope, never a guessed personal identity.</summary>
    internal BudgetDefinitionKey Definition(QuotaScope scope) => new(scope, CurrencyCode, Period, AccountIds);
}
