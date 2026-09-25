using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests;

/// <summary>Builds accounts and entries in major units for readable tests (EUR unless stated).</summary>
internal sealed class LedgerBuilder
{
    public static readonly DateOnly Day1 = new(2026, 10, 1);

    public List<Account> Accounts { get; } = [];

    public List<LedgerEntry> Entries { get; } = [];

    public Account Account(string name, decimal opening, AccountType type = AccountType.Checking, string currency = "EUR", DateOnly? openingDate = null)
    {
        var account = new Account
        {
            Name = name,
            Type = type,
            CurrencyCode = currency,
            OpeningBalance = Minor(opening, currency),
            OpeningDate = openingDate ?? Day1,
        };
        Accounts.Add(account);
        return account;
    }

    public LedgerEntry Add(EntryKind kind, Account account, decimal amount, DateOnly? date = null, Guid? categoryId = null, ReviewState review = ReviewState.Confirmed)
    {
        var entry = new LedgerEntry
        {
            Kind = kind,
            AccountId = account.Id,
            Amount = Minor(amount, account.CurrencyCode),
            Date = date ?? Day1,
            CategoryId = categoryId,
            Review = review,
        };
        Entries.Add(entry);
        return entry;
    }

    public LedgerEntry Transfer(Account from, Account to, decimal amount, decimal? toAmount = null, DateOnly? date = null)
    {
        var entry = Add(EntryKind.Transfer, from, amount, date);
        entry.ToAccountId = to.Id;
        entry.ToAmount = toAmount is { } t ? Minor(t, to.CurrencyCode) : null;
        return entry;
    }

    public LedgerEntry Refund(LedgerEntry purchase, Account receivingAccount, decimal amount, DateOnly? date = null)
    {
        var entry = Add(EntryKind.Refund, receivingAccount, amount, date, purchase.CategoryId);
        entry.RefundOfId = purchase.Id;
        return entry;
    }

    public long Balance(Account account, DateOnly? at = null, bool confirmedOnly = false) =>
        LedgerCalculator.Balance(account, Entries, at ?? new DateOnly(2026, 10, 31), confirmedOnly);

    public PeriodTotals Totals(IReadOnlyCollection<Guid>? accounts = null, bool confirmedOnly = false) =>
        LedgerCalculator.Totals(Accounts, Entries, new LedgerFilter(Day1, new DateOnly(2026, 10, 31), accounts, confirmedOnly))
            .SingleOrDefault() ?? new PeriodTotals("EUR", 0, 0, 0, 0);

    public static long Minor(decimal amount, string currency = "EUR") =>
        Core.Money.MoneyAmount.ToMinor(amount, Core.Money.Currencies.Get(currency));
}
