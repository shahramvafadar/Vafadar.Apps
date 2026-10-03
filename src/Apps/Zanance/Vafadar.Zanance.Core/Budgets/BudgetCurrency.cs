using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Budgets;

/// <summary>
/// Which budget currency Home and the Budget page show first (ZEX-P02): the currency the user picked there, otherwise the
/// currency of the default account, otherwise the default currency for new items. The valuation currency never decides
/// it. Budgets stay per currency; when budgets exist in several currencies a switcher offers each of them.
/// </summary>
public static class BudgetCurrency
{
    /// <summary>Returns the budget currency to show.</summary>
    public static string Resolve(ZananceSettings settings, IEnumerable<Account> accounts)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(accounts);
        if (!string.IsNullOrEmpty(settings.HomeBudgetCurrencyCode))
        {
            return settings.HomeBudgetCurrencyCode;
        }

        var defaultAccount = accounts.FirstOrDefault(a => a.Id == settings.DefaultAccountId);
        return EntryAccountContract.IsValidDefault(defaultAccount) ? defaultAccount!.CurrencyCode : settings.DefaultCurrencyCode;
    }

    /// <summary>
    /// Returns the currencies to offer in the switcher, sorted: the shown one, every currency that has a budget and every
    /// currency of an active money account (so a first budget in another currency can be made).
    /// </summary>
    public static IReadOnlyList<string> Choices(string current, IEnumerable<Budget> budgets, IEnumerable<Account> accounts)
    {
        ArgumentNullException.ThrowIfNull(budgets);
        ArgumentNullException.ThrowIfNull(accounts);
        return
        [
            .. budgets.Select(b => b.CurrencyCode)
                .Concat(accounts.Where(a => !a.IsArchived && !a.Type.IsOutsideCash()).Select(a => a.CurrencyCode))
                .Append(current)
                .Select(c => c.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
    }
}
