using System.Collections.Specialized;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual-file account overview reads, original calculations and covered native command lifetimes.</summary>
public sealed class AccountOverviewFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static IDbContextFactory<ZananceDbContext> Factory(FlowFixture f) => f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();

    internal static AccountsViewModel Create(FlowFixture f, ZananceStore? store = null) => new(store ?? f.Store,
        f.Translator, f.Localization, f.Time, new DateFormatter(f.Localization),
        new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Platform);

    internal static async Task<Account> SeedAsync(FlowFixture f)
    {
        await using var db = await Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original checking", Type = AccountType.Checking,
            CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = new(2026, 10, 1), IncludeInTotals = true };
        db.Accounts.Add(account);
        db.Entries.Add(new LedgerEntry(Guid.NewGuid()) { AccountId = account.Id, Date = new(2026, 10, 2),
            Kind = EntryKind.Expense, Amount = 1000, Title = "Original one expense", Tags = ["Original tag"] });
        await db.SaveChangesAsync(Ct);
        return account;
    }

    [Fact]
    public async Task Empty_read_and_publication_never_create_a_settings_row()
    {
        using var f = new FlowFixture(); var vm = Create(f);
        Assert.False(vm.Loading.IsReady); Assert.True(vm.Loading.IsLoading);
        await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.True(vm.IsEmpty); Assert.Empty(vm.Groups); Assert.Empty(vm.Totals);
        await using var db = await Factory(f).CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }

    [Fact]
    public async Task Original_balances_group_order_archives_and_excluded_totals_stay_separate()
    {
        using var f = new FlowFixture(); var checking = await SeedAsync(f);
        await using (var db = await Factory(f).CreateDbContextAsync(Ct))
        {
            db.Accounts.AddRange(new Account { Name = "Dollar cash", Type = AccountType.Cash, CurrencyCode = "USD",
                OpeningBalance = 20000, OpeningDate = new(2026, 10, 1), IncludeInTotals = true },
                new Account { Name = "Excluded savings", Type = AccountType.Savings, CurrencyCode = "EUR",
                    OpeningBalance = 5000, OpeningDate = new(2026, 10, 1), IncludeInTotals = false },
                new Account { Name = "Archived cash", Type = AccountType.Cash, CurrencyCode = "EUR", IsArchived = true,
                    OpeningBalance = 9000, OpeningDate = new(2026, 10, 1), IncludeInTotals = true },
                new Account { Name = "Original loan", Type = AccountType.Loan, CurrencyCode = "EUR",
                    OpeningBalance = -50000, OpeningDate = new(2026, 10, 1), IncludeInTotals = false });
            db.Settings.Add(new ZananceSettings { DefaultAccountId = checking.Id });
            await db.SaveChangesAsync(Ct);
        }
        var vm = Create(f); await vm.LoadAsync();
        Assert.Equal([AccountGroup.Money, AccountGroup.Debts], vm.Groups.Select(g => g.Group));
        Assert.Equal("Archived cash", Assert.Single(vm.Archived).Name); Assert.True(vm.HasArchived);
        var rows = vm.Groups.SelectMany(g => g.Items).Concat(vm.Archived).ToArray();
        var accounts = await f.Store.GetAccountsAsync(cancellationToken: Ct); var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        foreach (var account in accounts)
        {
            var row = Assert.Single(rows, row => row.Id == account.Id);
            Assert.Equal(MoneyText.Format(LedgerCalculator.Balance(account, entries, new(2026, 10, 9)), account.CurrencyCode, f.Localization.CurrentCulture), row.BalanceText);
        }
        Assert.True(Assert.Single(rows, row => row.Id == checking.Id).IsDefault);
        Assert.True(Assert.Single(rows, row => row.Name == "Original loan").IsNegative);
        Assert.Contains(vm.Totals, total => total.CurrencyCode == "EUR" && total.Text.Contains("90.00", StringComparison.Ordinal));
        Assert.Contains(vm.Totals, total => total.CurrencyCode == "USD" && total.Text.Contains("200.00", StringComparison.Ordinal));
        Assert.Null(vm.ConvertedText); Assert.Null(vm.HoldingsText); Assert.Single(entries);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("settings")]
    [InlineData("ledger")]
    public async Task Persisted_source_edits_refuse_the_old_snapshot(string table)
    {
        using var f = new FlowFixture(); await SeedAsync(f);
        var original = await f.Store.ReadAccountOverviewAsync(Ct);
        await using (var db = await Factory(f).CreateDbContextAsync(Ct))
        {
            if (table == "account") await db.Database.ExecuteSqlRawAsync("UPDATE Accounts SET Name='Changed original'", Ct);
            else if (table == "ledger") await db.Database.ExecuteSqlRawAsync("UPDATE Entries SET Amount=1100", Ct);
            else { db.Settings.Add(new ZananceSettings { MonthStartDay = 15 }); await db.SaveChangesAsync(Ct); }
        }
        await Assert.ThrowsAsync<AccountOverviewSupersededException>(() => f.Store.ValidateAccountOverviewAsync(original, Ct));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Caller_mutation_is_not_reclassified_as_a_retryable_source_edit(bool mutateLedger)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var original = await f.Store.ReadAccountOverviewAsync(Ct);
        if (mutateLedger) original.Inputs.Entries[0].Tags.Add("Caller change");
        else original.Inputs.Accounts[0].Name = "Caller change";
        await using (var db = await Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE Accounts SET Name='Persisted edit too'", Ct);
        var error = await Record.ExceptionAsync(() => f.Store.ValidateAccountOverviewAsync(original, Ct));
        Assert.IsType<InvalidOperationException>(error); Assert.False(error is AccountOverviewSupersededException);
    }

    [Fact]
    public async Task Another_file_is_refused_before_any_old_publication()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f);
        var controlled = new ControlledFactory(Factory(f)); var calls = 0;
        controlled.Select = () => ++calls == 1 ? Factory(f) : Factory(other);
        var store = new ZananceStore(controlled, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = Create(f, store); await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.Empty(vm.Groups); Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Pending_reads_are_shared_and_commands_cannot_cross_the_loading_cover()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var controlled = new ControlledFactory(Factory(f));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        controlled.Before = async ct => { calls++; if (calls == 1) { entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = Create(f, new ZananceStore(controlled, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        var first = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); var second = vm.LoadAsync();
        await vm.AddCommand.ExecuteAsync(null); await vm.AddDebtCommand.ExecuteAsync(null); await vm.OpenHoldingsCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Empty(vm.Groups); release.SetResult(); await Task.WhenAll(first, second);
        Assert.Equal(2, calls); Assert.True(vm.Loading.IsReady); Assert.Single(vm.Groups);
    }

    [Fact]
    public async Task Retired_read_cannot_publish_over_the_next_page_lifetime()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var controlled = new ControlledFactory(Factory(f));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        controlled.Before = async _ => { if (first) { first = false; entered.SetResult(); await release.Task; } };
        var vm = Create(f, new ZananceStore(controlled, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        var old = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); vm.Retire(); var current = vm.Loading;
        await vm.LoadAsync(); var row = Assert.Single(Assert.Single(vm.Groups).Items); release.SetResult(); await old;
        Assert.Same(current, vm.Loading); Assert.True(vm.Loading.IsReady); Assert.Same(row, Assert.Single(Assert.Single(vm.Groups).Items));
    }

    [Fact]
    public async Task Failed_reload_preserves_covered_rows_then_retry_replaces_them_once()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = Create(f); await vm.LoadAsync();
        var old = Assert.Single(Assert.Single(vm.Groups).Items); f.Time.BeforeRead = () => throw new IOException("Owned account read failure");
        await Assert.ThrowsAsync<IOException>(vm.LoadAsync); Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady);
        Assert.Same(old, Assert.Single(Assert.Single(vm.Groups).Items)); await vm.OpenAsync(old); Assert.Empty(f.Platform.Routes);
        f.Time.BeforeRead = null; await vm.LoadAsync(); Assert.True(vm.Loading.IsReady);
        Assert.NotSame(old, Assert.Single(Assert.Single(vm.Groups).Items));
    }

    [Fact]
    public async Task Three_collections_publish_one_reset_each_while_every_command_is_covered()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = Create(f); var events = new List<NotifyCollectionChangedAction>();
        NotifyCollectionChangedEventHandler observe = (_, args) =>
        {
            Assert.False(vm.Loading.IsReady); events.Add(args.Action);
            Assert.True(vm.AddCommand.ExecuteAsync(null).IsCompletedSuccessfully); Assert.Empty(f.Platform.Routes);
        };
        vm.Archived.CollectionChanged += observe; vm.Totals.CollectionChanged += observe; vm.Groups.CollectionChanged += observe;
        await vm.LoadAsync(); Assert.Equal([NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Reset], events);
    }

    [Fact]
    public async Task Native_retirement_during_first_reset_prevents_the_rest_of_old_publication()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = Create(f);
        vm.Archived.CollectionChanged += (_, _) => vm.Retire();
        await vm.LoadAsync(); Assert.False(vm.Loading.IsReady); Assert.Empty(vm.Totals); Assert.Empty(vm.Groups);
    }

    [Fact]
    public async Task Only_actual_current_row_identity_can_navigate_and_retirement_closes_all_commands()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = Create(f); await vm.LoadAsync();
        var old = Assert.Single(Assert.Single(vm.Groups).Items); await vm.LoadAsync(); var current = Assert.Single(Assert.Single(vm.Groups).Items);
        await vm.OpenAsync(old); await vm.OpenAsync(current with { }); Assert.Empty(f.Platform.Routes);
        await vm.OpenAsync(current); Assert.Equal(current.Id, Assert.Single(f.Platform.Routes).Parameters!["id"]);
        vm.Retire(); await vm.OpenAsync(current); await vm.AddCommand.ExecuteAsync(null); await vm.AddDebtCommand.ExecuteAsync(null);
        await vm.OpenHoldingsCommand.ExecuteAsync(null); Assert.Single(f.Platform.Routes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Same_file_metadata_edits_retry_only_within_the_original_three_attempts(bool continuous)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var actual = Factory(f); var controlled = new ControlledFactory(actual);
        var calls = 0; var edits = 0;
        controlled.Before = async _ =>
        {
            calls++;
            if (calls % 2 != 0 || !continuous && edits > 0) return;
            await using var db = await actual.CreateDbContextAsync(Ct); edits++;
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Accounts SET Name={"Changed " + edits}", Ct);
        };
        var vm = Create(f, new ZananceStore(controlled, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        if (continuous)
        {
            await Assert.ThrowsAsync<AccountOverviewSupersededException>(vm.LoadAsync); Assert.Equal(3, edits);
            Assert.True(vm.Loading.HasFailed); Assert.Empty(vm.Groups);
        }
        else
        {
            await vm.LoadAsync(); Assert.Equal(1, edits); Assert.Equal("Changed 1", Assert.Single(Assert.Single(vm.Groups).Items).Name);
        }
    }

    /// <summary>Controls only actual factory selection/waits while every persisted operation remains real SQLite.</summary>
    private sealed class ControlledFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal Func<IDbContextFactory<ZananceDbContext>>? Select;
        internal Func<CancellationToken, Task>? Before;
        public ZananceDbContext CreateDbContext() => (Select?.Invoke() ?? original).CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (Before is { } before) await before(cancellationToken);
            return await (Select?.Invoke() ?? original).CreateDbContextAsync(cancellationToken);
        }
    }
}
