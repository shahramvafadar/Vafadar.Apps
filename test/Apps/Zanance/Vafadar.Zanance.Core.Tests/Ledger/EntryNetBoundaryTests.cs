using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Transaction totals must stay exact across cancellation and refuse unrepresentable public results.</summary>
public sealed class EntryNetBoundaryTests
{
    /// <summary>Rejects an unrepresentable currency/day sum instead of wrapping to a plausible wrong amount.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Unrepresentable_final_sum_refuses(bool negative, bool byDay)
    {
        var a = new Account { Name = "Bounded", CurrencyCode = "EUR" };
        var rows = new[] { Row(a, negative ? EntryKind.Expense : EntryKind.Income, long.MaxValue), Row(a, negative ? EntryKind.Expense : EntryKind.Income, 2) };
        var accounts = new Dictionary<Guid, Account> { [a.Id] = a };
        Assert.Throws<OverflowException>(() => { if (byDay) EntrySearch.ByDay(rows, accounts); else EntrySearch.NetByCurrency(rows, accounts); });
    }

    /// <summary>Wider intermediate totals preserve completed representable cancellation in original row order.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Representable_completed_cancellation_is_not_an_early_overflow(bool negative, bool byDay)
    {
        var a = new Account { Name = "Cancellation", CurrencyCode = "EUR" };
        var first = negative ? EntryKind.Expense : EntryKind.Income; var last = negative ? EntryKind.Refund : EntryKind.Expense;
        var rows = new[] { Row(a, first, long.MaxValue), Row(a, first, 2), Row(a, last, 2) };
        var accounts = new Dictionary<Guid, Account> { [a.Id] = a };
        var groups = byDay ? EntrySearch.ByDay(rows, accounts) : null;
        var net = groups is not null ? Assert.Single(groups).Net : EntrySearch.NetByCurrency(rows, accounts);
        Assert.Equal(negative ? -long.MaxValue : long.MaxValue, net["EUR"]);
        if (groups is not null) Assert.Equal(rows, Assert.Single(groups).Entries);
    }

    /// <summary>Every published day must fit even when the whole filtered interval cancels later.</summary>
    [Fact]
    public void Later_day_does_not_hide_an_invalid_day_total()
    {
        var a = new Account { Name = "Day boundary", CurrencyCode = "EUR" }; var rows = new[] { Row(a, EntryKind.Income, long.MaxValue), Row(a, EntryKind.Income, 2), Row(a, EntryKind.Expense, 2) };
        rows[2].Date = rows[2].Date.AddDays(1); var accounts = new Dictionary<Guid, Account> { [a.Id] = a };
        Assert.Equal(long.MaxValue, EntrySearch.NetByCurrency(rows, accounts)["EUR"]);
        Assert.Throws<OverflowException>(() => EntrySearch.ByDay(rows, accounts));
    }

    /// <summary>An individual public signed effect cannot negate the unrepresentable Int64 minimum.</summary>
    [Theory]
    [InlineData(EntryKind.Expense)]
    [InlineData(EntryKind.IncomeReversal)]
    public void Unrepresentable_signed_effect_refuses(EntryKind kind) =>
        Assert.Throws<OverflowException>(() => EntrySearch.SignedResult(new LedgerEntry { Kind = kind, Amount = long.MinValue }));

    /// <summary>Known currencies keep original key order, zero cancellation and row identities; unknown accounts and transfers stay excluded.</summary>
    [Fact]
    public void Currency_partition_and_original_entry_ownership_are_preserved()
    {
        var eur = new Account { Name = "EUR", CurrencyCode = "EUR" }; var usd = new Account { Name = "USD", CurrencyCode = "USD" };
        var rows = new[] { Row(usd, EntryKind.Income, 7), Row(eur, EntryKind.Income, long.MaxValue), Row(eur, EntryKind.Refund, 2), Row(eur, EntryKind.Expense, long.MaxValue), Row(eur, EntryKind.IncomeReversal, 2), Row(eur, EntryKind.Transfer, long.MaxValue), Row(new Account { Name = "Unknown", CurrencyCode = "EUR" }, EntryKind.Income, long.MaxValue) };
        var accounts = new Dictionary<Guid, Account> { [eur.Id] = eur, [usd.Id] = usd };
        var day = Assert.Single(EntrySearch.ByDay(rows, accounts)); Assert.Equal(new[] { "EUR", "USD" }, day.Net.Keys);
        Assert.Equal(0, day.Net["EUR"]); Assert.Equal(7, day.Net["USD"]); Assert.Equal(rows.Length, day.Entries.Count);
        for (var i = 0; i < rows.Length; i++) Assert.Same(rows[i], day.Entries[i]);
    }

    private static LedgerEntry Row(Account a, EntryKind kind, long amount) => new()
    { AccountId = a.Id, Kind = kind, Amount = amount, Date = new DateOnly(2026, 11, 5), Title = "Original row", Tags = ["bounded"] };
}

