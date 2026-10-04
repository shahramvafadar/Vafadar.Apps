using Vafadar.Core.Text;

namespace Vafadar.Zanance.Core.Accounts;

/// <summary>
/// Account names are how people and files tell accounts apart: pickers show them, and the CSV format of the app refers to
/// accounts by name (an import uses the first account with the name). Two accounts with the same name are therefore not
/// allowed, also when one of them is archived.
/// </summary>
public static class AccountNames
{
    /// <summary>
    /// Returns whether <paramref name="name"/> is already used by another account than <paramref name="exceptId"/>. Case,
    /// surrounding spaces, digit scripts, the Arabic and Persian letter forms and half-spaces do not make a name different.
    /// </summary>
    public static bool IsTaken(IEnumerable<Account> accounts, string? name, Guid? exceptId = null)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        var wanted = SearchText.Normalize(name);
        return wanted.Length > 0 && accounts.Any(a => a.Id != exceptId
            && string.Equals(SearchText.Normalize(a.Name), wanted, StringComparison.CurrentCultureIgnoreCase));
    }
}
