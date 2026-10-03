namespace Vafadar.Zanance.Core.Accounts;

/// <summary>
/// What keeps the currency of an account from changing (ZEX-S0105, ZEX-P03): every stored amount in that currency.
/// Changing the currency would relabel those amounts without converting them, so it is refused while any exists.
/// </summary>
/// <param name="Entries">Ledger entries on the account, as source or destination of a transfer.</param>
/// <param name="Plans">Plans that pay from or into the account.</param>
/// <param name="Templates">Quick templates of the account.</param>
/// <param name="Earmarks">Goal earmarks (money set aside) on the account.</param>
/// <param name="Budgets">Budgets limited to accounts including this one.</param>
/// <param name="Installment">Whether a loan installment is stored for the account.</param>
public sealed record AccountCurrencyLock(int Entries, int Plans, int Templates, int Earmarks, int Budgets, bool Installment)
{
    /// <summary>Nothing locks the currency.</summary>
    public static AccountCurrencyLock None { get; } = new(0, 0, 0, 0, 0, false);

    /// <summary>Gets a value indicating whether the currency is locked.</summary>
    public bool IsLocked => Entries + Plans + Templates + Earmarks + Budgets > 0 || Installment;
}