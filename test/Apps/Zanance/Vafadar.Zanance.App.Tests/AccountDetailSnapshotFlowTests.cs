using System.Collections.Specialized;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Complete original detail source and publication boundaries, preserving original raw drafts and all money.</summary>
public sealed class AccountDetailSnapshotFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static IDbContextFactory<ZananceDbContext> Factory(FlowFixture f) => AccountDetailOriginalBoundaryTests.Factory(f);
    private static LoanWorkStore Reader(FlowFixture f) => f.Services.GetRequiredService<LoanWorkStore>();

    private static async Task<Account> CompleteSourceAsync(FlowFixture f, bool settings = false)
    {
        var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        await using var db = await Factory(f).CreateDbContextAsync(Ct);
        var goal = new Goal { Name = "Original saved goal", CurrencyCode = "EUR", TargetAmount = 5000, Type = GoalType.Earmark };
        db.Goals.Add(goal); db.GoalAllocations.Add(new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 1000, Date = new(2026, 10, 2) });
        db.Entries.Add(new LedgerEntry(Guid.NewGuid()) { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 500, Date = new(2026, 10, 2), Tags = ["Original complete tag"], Title = "Original one expense" });
        if (settings) db.Settings.Add(new ZananceSettings());
        await db.SaveChangesAsync(Ct); return account;
    }

    [Fact]
    public async Task Absent_settings_stay_absent_and_original_earmark_and_movement_publish_together_once()
    {
        using var f = new FlowFixture(); var account = await CompleteSourceAsync(f); var vm = AccountDetailOriginalBoundaryTests.Create(f);
        var resets = 0; vm.Movement.CollectionChanged += (_, e) => { Assert.True(vm.Loading.IsLoading); Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action); resets++; };
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(1, resets); Assert.Contains("95.00", vm.BalanceText, StringComparison.Ordinal);
        Assert.Contains("10.00", vm.EarmarkText, StringComparison.Ordinal); Assert.Contains("85.00", vm.EarmarkText, StringComparison.Ordinal);
        Assert.Equal(4, vm.Movement.Count); Assert.Contains("0.00", vm.Movement[0].Amount, StringComparison.Ordinal); Assert.Contains("100.00", vm.Movement[1].Amount, StringComparison.Ordinal); Assert.Contains("95.00", vm.Movement[^1].Amount, StringComparison.Ordinal);
        await using var db = await Factory(f).CreateDbContextAsync(Ct); Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Single(await db.Entries.ToListAsync(Ct));
    }

    [Theory]
    [InlineData("account")][InlineData("ledger-tags")][InlineData("goal")][InlineData("allocation")][InlineData("default-settings")][InlineData("stored-settings")]
    public async Task Caller_mutation_of_any_original_source_is_not_a_retryable_persisted_change(string part)
    {
        using var f = new FlowFixture(); await CompleteSourceAsync(f, settings: part == "stored-settings"); var reader = Reader(f); var snapshot = await reader.ReadAccountDetailAsync(Ct);
        switch (part)
        {
            case "account": snapshot.Inputs.Accounts[0].Name = "Caller replacement"; break;
            case "ledger-tags": snapshot.Inputs.Entries[0].Tags.Add("Caller replacement"); break;
            case "goal": snapshot.Inputs.Goals[0].Note = "Caller replacement"; break;
            case "allocation": snapshot.Inputs.Allocations[0].Amount = 2000; break;
            case "default-settings": snapshot.Loan.Settings.MonthStartDay = 15; break;
            case "stored-settings": snapshot.Inputs.Settings!.MonthStartDay = 15; break;
        }
        var error = await Record.ExceptionAsync(() => reader.ValidateAccountDetailAsync(snapshot, Ct));
        Assert.IsType<InvalidOperationException>(error); Assert.False(error is AccountDetailSupersededException);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Persisted_goal_or_allocation_edits_retire_the_old_complete_snapshot_without_audit_changes(bool allocation)
    {
        using var f = new FlowFixture(); await CompleteSourceAsync(f); var reader = Reader(f); var snapshot = await reader.ReadAccountDetailAsync(Ct);
        await using (var db = await Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync(allocation ? "UPDATE GoalAllocations SET Amount=1100" : "UPDATE Goals SET State=1", Ct);
        await Assert.ThrowsAsync<AccountDetailSupersededException>(() => reader.ValidateAccountDetailAsync(snapshot, Ct));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Same_file_goal_edits_retry_only_within_the_original_three_complete_attempts(bool continuous)
    {
        using var f = new FlowFixture(); var account = await CompleteSourceAsync(f); var actual = Factory(f); var calls = 0; var edits = 0;
        var controlled = new AccountDetailOriginalBoundaryTests.ControlledFactory(actual)
        { Before = async _ =>
            {
                calls++; if (calls % 2 != 0 || !continuous && edits > 0) return;
                await using var db = await actual.CreateDbContextAsync(Ct); edits++;
                await db.Database.ExecuteSqlRawAsync("UPDATE GoalAllocations SET Amount=Amount+100", Ct);
            }
        };
        var vm = AccountDetailOriginalBoundaryTests.Create(f, controlled); AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        if (continuous)
        { Assert.Equal(3, edits); Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.Empty(vm.Movement); Assert.Null(vm.Name); }
        else
        { Assert.Equal(1, edits); Assert.True(vm.Loading.IsReady); Assert.Contains("11.00", vm.EarmarkText, StringComparison.Ordinal); }
    }

    [Fact]
    public async Task Retirement_at_the_first_property_prevents_later_old_rows_and_ready_state()
    {
        using var f = new FlowFixture(); var account = await CompleteSourceAsync(f); var vm = AccountDetailOriginalBoundaryTests.Create(f); var retired = false;
        vm.PropertyChanged += (_, e) => { if (retired || e.PropertyName != nameof(vm.Name) || vm.Name is null) return; retired = true; vm.SetReconciliationActive(false); };
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        Assert.True(retired); Assert.False(vm.Loading.IsReady); Assert.Empty(vm.Movement); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Read_failure_and_explicit_retry_keep_the_original_observation_reason_and_date_without_a_write()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f); var reject = false;
        var controlled = new AccountDetailOriginalBoundaryTests.ControlledFactory(Factory(f))
        { Before = _ => reject ? Task.FromException(new IOException("Original owned read refusal")) : Task.CompletedTask };
        var vm = AccountDetailOriginalBoundaryTests.Create(f, controlled); AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        vm.ObservedText = "123.4500"; vm.Reason = "Original untouched reason"; vm.ReconcileDate = new(2026, 10, 3);
        reject = true; await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady);
        Assert.Equal("123.4500", vm.ObservedText); Assert.Equal("Original untouched reason", vm.Reason); Assert.Equal(new DateOnly(2026, 10, 3), vm.ReconcileDate);
        reject = false; await vm.RefreshLoanCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady);
        Assert.Equal("123.4500", vm.ObservedText); Assert.Equal("Original untouched reason", vm.Reason); Assert.Equal(new DateOnly(2026, 10, 3), vm.ReconcileDate);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Null(Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).LastReconciledOn);
    }

    [Fact]
    public async Task A_display_change_retires_the_held_read_and_shares_only_the_current_language_publication()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        var controlled = new AccountDetailOriginalBoundaryTests.ControlledFactory(Factory(f)) { Before = async ct =>
        { if (!first) return; first = false; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = AccountDetailOriginalBoundaryTests.Create(f, controlled); AccountDetailOriginalBoundaryTests.Open(vm, account.Id); var old = vm.LoadAsync(); await entered.Task.WaitAsync(Ct);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "de")); release.SetResult(); await old; await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(f.Translator["AccountType_Checking"], vm.TypeName); Assert.Contains("100,00", vm.BalanceText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("edit")][InlineData("entries")][InlineData("add-entry")]
    public async Task A_retired_native_command_cannot_navigate_after_its_actual_source_validation_wait(string command)
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f); var actual = Factory(f);
        var controlled = new AccountDetailOriginalBoundaryTests.ControlledFactory(actual); var vm = AccountDetailOriginalBoundaryTests.Create(f, controlled);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        controlled.Before = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); };
        var target = command == "edit" ? vm.EditCommand : command == "entries" ? vm.ShowEntriesCommand : vm.AddEntryHereCommand;
        var pending = target.ExecuteAsync(null); await entered.Task.WaitAsync(Ct); vm.SetReconciliationActive(false); release.SetResult(); await pending;
        Assert.Empty(f.Platform.Routes); Assert.False(vm.Loading.IsReady); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_same_id_balance_comparison_cannot_mark_metadata_in_another_actual_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        await using (var db = await Factory(other).CreateDbContextAsync(Ct))
        {
            var restored = new Account { Name = "Other source", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = account.OpeningDate, OpeningBalance = 10000 };
            db.Accounts.Add(restored); db.Entry(restored).Property(item => item.Id).CurrentValue = account.Id; await db.SaveChangesAsync(Ct);
        }
        var otherWriter = new ZananceStore(Factory(other), f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = AccountDetailOriginalBoundaryTests.Create(f, writer: otherWriter); AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        vm.ObservedText = "100.00"; await vm.CompareCommand.ExecuteAsync(null);
        Assert.NotNull(vm.Error); Assert.Null(Assert.Single(await other.Store.GetAccountsAsync(cancellationToken: Ct)).LastReconciledOn);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(await other.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
