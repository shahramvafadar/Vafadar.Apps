using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-134: retained debt closing and exact actual-file financial correction rights in actual SQLite writers.</summary>
[Trait("AT", "AT-134")]
public sealed class RetainedDebtClosingWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static CapabilityContext Context(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private async Task<Fixture> FixtureAsync(bool trigger = false)
    {
        var path = _directory.Combine(Guid.NewGuid()+".db");
        var access = new AccessSource();
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path);
        if (trigger)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), access));
        }
        var provider = services.BuildServiceProvider();
        _providers.Add(provider); provider.MigrateLocalDatabase<ZananceDbContext>();
        var store = provider.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync(Ct);
        var accounts = Enumerable.Range(1, 4).Select(i => new Account
        { Name = "Owned account "+i, CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 100000 }).ToArray();
        foreach (var account in accounts) await store.SaveAccountAsync(account, Ct);
        return new(path, access, provider, store, accounts);
    }

    private static ResourceSelection Choice(params Guid[] ids) => new(QuotaKind.FinancialAccounts,
        new(QuotaScopeKind.PersonalProfile, ScopeId), ids);

    private sealed record Fixture(string Path, AccessSource Access, ServiceProvider Provider, ZananceStore Store, Account[] Accounts)
    {
        public LedgerEntry Entry(int account, EntryKind kind = EntryKind.Expense) => new()
        { Kind = kind, AccountId = Accounts[account].Id, Amount = 1000, Date = Day, Note = "Retained fictitious input", Tags = ["owned"] };
        public void Enable(ProductPlan plan = ProductPlan.Free) => Access.Current = new(Path, Context(plan));
        public void Select(params int[] indexes) => Access.Current = new(Path, Context(), Choice(indexes.Select(i => Accounts[i].Id).ToArray()));
    }

    private async Task<Fixture> DebtFixtureAsync(AccountType type = AccountType.Loan, bool trigger = false)
    {
        var f = await FixtureAsync(trigger); var debt = f.Accounts[3];
        debt.Type = type; debt.OpeningBalance = type == AccountType.Loan ? -1000 : 1000;
        debt.Counterparty = "Owned fictitious counterparty";
        Assert.True(await f.Store.SaveAccountAsync(debt, Ct));
        return f;
    }

    private static LedgerEntry Closing(Fixture f, long amount = 1000)
    {
        var lent = f.Accounts[3].Type == AccountType.Lent;
        return new() { Kind = EntryKind.Transfer, AccountId = f.Accounts[lent ? 3 : 0].Id,
            ToAccountId = f.Accounts[lent ? 0 : 3].Id, Amount = amount, Date = Day,
            Title = "Explicit retained principal closure", Note = "Owned fictitious closing input", Tags = ["retained"] };
    }

    [Theory]
    [InlineData(AccountType.Loan, false)]
    [InlineData(AccountType.Lent, false)]
    [InlineData(AccountType.Loan, true)]
    [InlineData(AccountType.Lent, true)]
    public async Task An_explicit_complete_existing_debt_payment_remains_available_without_account_choice_or_after_host_expiry(AccountType type, bool expiredHost)
    {
        var f = await DebtFixtureAsync(type);
        if (expiredHost) f.Access.Current = new(f.Path, Context(ProductPlan.Free, true, true, false)); else f.Enable();
        var events = 0; f.Store.Changed += (_, _) => events++;
        Assert.True((await f.Store.SaveEntryAsync(Closing(f), Ct)).Succeeded);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], entries, Day));
        Assert.Equal(EntryKind.Transfer, Assert.Single(entries).Kind); Assert.Equal(1, events);
        Assert.Equal(4, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Count); Assert.False(f.Accounts[3].IsArchived);
    }

    [Theory]
    [InlineData("partial")]
    [InlineData("overpay")]
    [InlineData("future")]
    [InlineData("unreviewed")]
    [InlineData("schedule")]
    [InlineData("import")]
    [InlineData("unknown")]
    [InlineData("zero")]
    [InlineData("wrong-sign")]
    [InlineData("before-opening")]
    [InlineData("mismatched-same-currency")]
    [InlineData("missing-foreign-amount")]
    [InlineData("wrong-direction")]
    public async Task Unproven_closing_never_bypasses_selected_accounts_or_changes_any_stored_row(string scenario)
    {
        var f = await DebtFixtureAsync(); var payment = Closing(f); var debt = f.Accounts[3];
        switch (scenario)
        {
            case "partial": payment.Amount = 999; break;
            case "overpay": payment.Amount = 1001; break;
            case "future": payment.Date = Day.AddDays(1); break;
            case "unreviewed": payment.Review = ReviewState.Unreviewed; break;
            case "schedule": payment.ScheduleId = Guid.NewGuid(); payment.OccurrenceDate = Day; break;
            case "import": payment.Source = EntrySource.Import; break;
            case "unknown": debt.OpeningBalanceKnown = false; break;
            case "zero": debt.OpeningBalance = 0; break;
            case "wrong-sign": debt.OpeningBalance = 1000; break;
            case "before-opening": payment.Date = debt.OpeningDate.AddDays(-1); break;
            case "mismatched-same-currency": payment.Amount = 800; payment.ToAmount = 1000; break;
            case "missing-foreign-amount": debt.CurrencyCode = "USD"; break;
            case "wrong-direction": payment.AccountId = debt.Id; payment.ToAccountId = f.Accounts[0].Id; break;
        }
        Assert.True(await f.Store.SaveAccountAsync(debt, Ct)); f.Enable();
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(payment, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(AccountType.Loan, false)]
    [InlineData(AccountType.Lent, false)]
    [InlineData(AccountType.Loan, true)]
    [InlineData(AccountType.Lent, true)]
    public async Task Actual_principal_can_be_closed_with_multiple_explicit_payments_and_explicit_foreign_amounts(AccountType type, bool foreign)
    {
        var f = await DebtFixtureAsync(type);
        if (foreign) { f.Accounts[3].CurrencyCode = "USD"; Assert.True(await f.Store.SaveAccountAsync(f.Accounts[3], Ct)); }
        var first = Closing(f, 400); var second = Closing(f, 600);
        if (foreign)
        {
            first.ToAmount = type == AccountType.Loan ? 500 : 320;
            second.ToAmount = type == AccountType.Loan ? 500 : 480;
        }
        f.Enable(); Assert.True((await f.Store.SaveEntriesAsync([first, second], [], Ct)).Succeeded);
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], await f.Store.GetEntriesAsync(cancellationToken: Ct), Day));
    }

    [Theory]
    [InlineData(AccountType.Loan)]
    [InlineData(AccountType.Lent)]
    public async Task Closing_uses_actual_borrowing_history_and_not_the_opening_estimate(AccountType type)
    {
        var f = await DebtFixtureAsync(type); f.Accounts[3].OpeningBalance = 0;
        Assert.True(await f.Store.SaveAccountAsync(f.Accounts[3], Ct));
        var opening = Closing(f); (opening.AccountId, opening.ToAccountId) = (opening.ToAccountId!.Value, opening.AccountId);
        opening.Date = Day.AddDays(-2); Assert.True((await f.Store.SaveEntryAsync(opening, Ct)).Succeeded);
        f.Enable(); Assert.True((await f.Store.SaveEntryAsync(Closing(f), Ct)).Succeeded);
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], await f.Store.GetEntriesAsync(cancellationToken: Ct), Day));
    }

    [Theory]
    [InlineData("backdated")]
    [InlineData("unreviewed-history")]
    [InlineData("new-borrowing")]
    [InlineData("delete-original")]
    [InlineData("adjustment")]
    public async Task Another_write_cannot_manufacture_retained_closing_from_a_changed_or_uncertain_baseline(string scenario)
    {
        var f = await DebtFixtureAsync(); var old = Closing(f); old.AccountId = f.Accounts[3].Id; old.ToAccountId = f.Accounts[0].Id;
        old.Date = Day.AddDays(-2); if (scenario == "unreviewed-history") old.Review = ReviewState.Unreviewed;
        Assert.True((await f.Store.SaveEntryAsync(old, Ct)).Succeeded);
        var payment = Closing(f, 2000); List<LedgerEntry> incoming = [payment]; Guid[] deleted = [];
        switch (scenario)
        {
            case "backdated": payment.Date = Day.AddDays(-3); break;
            case "new-borrowing": payment.Amount = 3000; var borrow = old.Copy(); borrow = new LedgerEntry { Kind = borrow.Kind, AccountId = borrow.AccountId, ToAccountId = borrow.ToAccountId, Amount = 1000, Date = Day }; incoming.Add(borrow); break;
            case "delete-original": payment.Amount = 1000; deleted = [old.Id]; break;
            case "adjustment": payment.Amount = 1000; incoming.Add(new() { Kind = EntryKind.Adjustment, AccountId = f.Accounts[3].Id, Direction = AdjustmentDirection.Increase, Amount = 1000, Date = Day }); break;
        }
        f.Enable(); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntriesAsync(incoming, deleted, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(AccountType.CreditCard, false)]
    [InlineData(AccountType.CreditCard, true)]
    [InlineData(AccountType.Asset, false)]
    [InlineData(AccountType.Loan, false)]
    [InlineData(AccountType.Lent, false)]
    public async Task Closing_does_not_grant_new_work_on_an_unselected_counterpart(AccountType type, bool selected)
    {
        var f = await DebtFixtureAsync(); f.Accounts[0].Type = type;
        Assert.True(await f.Store.SaveAccountAsync(f.Accounts[0], Ct));
        if (selected) f.Select(0); else f.Enable();
        if (selected) Assert.True((await f.Store.SaveEntryAsync(Closing(f), Ct)).Succeeded);
        else
        {
            var before = await SnapshotAsync(f.Provider);
            await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(Closing(f), Ct));
            Assert.Equal(before, await SnapshotAsync(f.Provider));
        }
    }

    [Fact]
    public async Task Explicit_net_off_can_close_both_existing_principals_without_creating_a_financial_relationship()
    {
        var f = await DebtFixtureAsync(); f.Accounts[0].Type = AccountType.Lent; f.Accounts[0].OpeningBalance = 1000;
        Assert.True(await f.Store.SaveAccountAsync(f.Accounts[0], Ct)); f.Enable();
        Assert.True((await f.Store.SaveEntryAsync(Closing(f), Ct)).Succeeded);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Single(entries); Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[0], entries, Day));
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], entries, Day));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Both_real_transfer_fees_are_preserved_and_principal_is_checked_after_the_destination_fee(bool lent)
    {
        var f = await DebtFixtureAsync(lent ? AccountType.Lent : AccountType.Loan);
        var feeCategory = (await f.Store.GetCategoriesAsync(cancellationToken: Ct)).Single(c => c.SystemKey == "Fees");
        var payment = Closing(f, lent ? 900 : 1100);
        var sourceFee = EntryActions.SyncTransferFee(payment, null, 100, feeCategory.Id)!;
        var destinationFee = EntryActions.SyncDestinationFee(payment, null, 100, feeCategory.Id)!;
        f.Enable(); Assert.True((await f.Store.SaveEntriesAsync([payment, sourceFee, destinationFee], [], Ct)).Succeeded);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(3, entries.Count); Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], entries, Day));
        Assert.Equal(200, entries.Where(e => e.Kind == EntryKind.Expense).Sum(e => e.Amount));
    }

    [Theory]
    [InlineData("category")]
    [InlineData("other-account")]
    [InlineData("different-date")]
    [InlineData("unreviewed")]
    [InlineData("duplicate-fee")]
    [InlineData("second-transfer")]
    [InlineData("stored-group")]
    public async Task Unrelated_group_rows_are_not_classified_as_retained_closing_fees(string scenario)
    {
        var f = await DebtFixtureAsync(); var category = (await f.Store.GetCategoriesAsync(cancellationToken: Ct)).Single(c => c.SystemKey == "Fees");
        var payment = Closing(f); var fee = EntryActions.SyncTransferFee(payment, null, 100, category.Id)!;
        List<LedgerEntry> batch = [payment, fee];
        switch (scenario)
        {
            case "category": fee.CategoryId = (await f.Store.GetCategoriesAsync(cancellationToken: Ct)).Single(c => c.SystemKey == "Food").Id; break;
            case "other-account": fee.AccountId = f.Accounts[1].Id; break;
            case "different-date": fee.Date = Day.AddDays(-1); break;
            case "unreviewed": fee.Review = ReviewState.Unreviewed; break;
            case "duplicate-fee": batch.Add(EntryActions.SyncTransferFee(payment, null, 10, category.Id)!); break;
            case "second-transfer": var another = Closing(f, 1); another.GroupId = payment.GroupId; batch.Add(another); break;
            case "stored-group": var old = f.Entry(1); old.GroupId = payment.GroupId; Assert.True((await f.Store.SaveEntryAsync(old, Ct)).Succeeded); break;
        }
        f.Enable(); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntriesAsync(batch, [], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Multiple_payments_close_Int64_minimum_without_overflowing_a_positive_magnitude()
    {
        var f = await DebtFixtureAsync(); f.Accounts[3].OpeningBalance = long.MinValue;
        Assert.True(await f.Store.SaveAccountAsync(f.Accounts[3], Ct)); f.Enable();
        Assert.True((await f.Store.SaveEntriesAsync([Closing(f, long.MaxValue), Closing(f, 1)], [], Ct)).Succeeded);
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], await f.Store.GetEntriesAsync(cancellationToken: Ct), Day));
    }

    [Fact]
    public async Task Inactive_test_build_keeps_ordinary_partial_debt_payments_unrestricted()
    {
        var f = await DebtFixtureAsync(); Assert.True((await f.Store.SaveEntryAsync(Closing(f, 100), Ct)).Succeeded);
        Assert.Equal(-900, LedgerCalculator.Balance(f.Accounts[3], await f.Store.GetEntriesAsync(cancellationToken: Ct), Day));
    }

    [Fact]
    public async Task An_unselected_unrelated_expense_rolls_back_the_entire_otherwise_valid_closure()
    {
        var f = await DebtFixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntriesAsync([Closing(f), f.Entry(1)], [], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Independent_writers_recheck_the_stored_principal_and_only_one_can_close_it()
    {
        var f = await DebtFixtureAsync(); f.Enable();
        var provider = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(f.Access).AddZananceData(f.Path).BuildServiceProvider();
        _providers.Add(provider); var other = provider.GetRequiredService<ZananceStore>();
        var events = 0; f.Store.Changed += (_, _) => Interlocked.Increment(ref events);
        other.Changed += (_, _) => Interlocked.Increment(ref events);
        async Task<bool> Attempt(ZananceStore store)
        {
            try { return (await store.SaveEntryAsync(Closing(f), Ct)).Succeeded; }
            catch (CommercialWriteRejectedException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => Attempt(f.Store), Ct), Task.Run(() => Attempt(other), Ct));
        Assert.Equal(1, results.Count(success => success)); Assert.Equal(1, events);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Single(entries);
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], entries, Day));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_membership_or_an_expired_host_cannot_grant_new_card_borrowing(bool member)
    {
        var f = await DebtFixtureAsync(); f.Accounts[0].Type = AccountType.CreditCard;
        Assert.True(await f.Store.SaveAccountAsync(f.Accounts[0], Ct));
        f.Access.Current = new(f.Path, Context(ProductPlan.Pro, true, member, false)); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(Closing(f), Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_SQL_or_retired_access_rolls_back_the_closure_and_explicit_retry_succeeds(bool retire)
    {
        var f = await DebtFixtureAsync(trigger: true); f.Enable(); var payment = Closing(f);
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(retire
            ? "CREATE TRIGGER DebtClosingFailure AFTER INSERT ON Entries BEGIN SELECT AfterDebtClosingSql(); END"
            : "CREATE TRIGGER DebtClosingFailure AFTER INSERT ON Entries BEGIN SELECT RAISE(ABORT, 'Owned fixture failure'); END", Ct);
        f.Access.AfterSql = () => { f.Access.AfterSql = null; f.Access.Current = new(f.Path, Context(ProductPlan.Plus)); };
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        if (retire) await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveEntryAsync(payment, Ct));
        else await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.SaveEntryAsync(payment, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER DebtClosingFailure", Ct); f.Enable();
        Assert.True((await f.Store.SaveEntryAsync(payment, Ct)).Succeeded); Assert.Equal(1, events);
        Assert.Equal(0, LedgerCalculator.Balance(f.Accounts[3], await f.Store.GetEntriesAsync(cancellationToken: Ct), Day));
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(25, tables.Count); var all = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); all[table] = rows;
        }
        return JsonSerializer.Serialize(all);
    }

    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Action? AfterSql = null;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    /// <summary>Only the fixture's actual SQLite connection can retire its cached choice from an insert trigger.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db=inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("AfterDebtClosingSql",()=>{source.AfterSql?.Invoke();return 1;});
            return db;
        }
    }
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose();
    }
}
