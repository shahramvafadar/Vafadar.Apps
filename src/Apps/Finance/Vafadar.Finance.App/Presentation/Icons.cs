using FluentIcons.Common;
using Vafadar.Finance.Core.Accounts;

namespace Vafadar.Finance.App.Presentation;

/// <summary>Icon keys (Fluent symbol names) and defaults.</summary>
internal static class Icons
{
    /// <summary>Returns the symbol for a stored icon key, or <paramref name="fallback"/> when unknown.</summary>
    public static Symbol Parse(string? key, Symbol fallback) =>
        Enum.TryParse<Symbol>(key, ignoreCase: false, out var symbol) ? symbol : fallback;

    /// <summary>Default icon of an account type.</summary>
    public static Symbol For(AccountType type) => type switch
    {
        AccountType.Cash => Symbol.Money,
        AccountType.Savings => Symbol.Savings,
        AccountType.CreditCard => Symbol.Payment,
        _ => Symbol.BuildingBank,
    };
}
