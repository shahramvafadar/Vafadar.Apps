using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-641: complete original budget form reads belong to their actual native frame.</summary>
public sealed class BudgetEditorOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A delayed original SQLite acquisition cannot publish Ready or fields after form departure.</summary>
    [Fact]
    public async Task Departed_original_budget_reader_never_publishes_ready_or_replaces_fields()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 10000 };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var editor = new BudgetEditorViewModel(store, f.Translator, f.Localization, f.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); editor.SetViewActive(true);
        var read = editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = budget.Id });
        await entered.Task.WaitAsync(Ct); editor.SetViewActive(false); release.SetResult(); await read;
        Assert.False(editor.IsReady); Assert.NotEqual("100.00", editor.TotalText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Routes);
    }

    /// <summary>A transient same-identity foreign ledger must not become the original form's spending suggestion.</summary>
    [Fact]
    public async Task Temporary_profile_switch_never_publishes_foreign_budget_history_suggestions()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        await f.Store.GetSettingsAsync(Ct); await other.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "D641 original account", CurrencyCode = "EUR", OpeningDate = new(2026, 7, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 10000 };
        await f.Store.SaveBudgetAsync(budget, Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(other).CreateDbContextAsync(Ct))
        {
            var foreign = new Account { Name = "D641 foreign same identity", CurrencyCode = "EUR", OpeningDate = account.OpeningDate };
            db.Accounts.Add(foreign); db.Entry(foreign).Property(row => row.Id).CurrentValue = account.Id;
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 98765, Date = new(2026, 9, 15) });
            await db.SaveChangesAsync(Ct);
        }
        var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => ++calls == 2 ? AccountDetailOriginalBoundaryTests.Factory(other) : AccountDetailOriginalBoundaryTests.Factory(f) };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var editor = new BudgetEditorViewModel(store, f.Translator, f.Localization, f.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var foreignBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = budget.Id });
        Assert.Null(editor.TotalSuggestionText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(foreignBefore, await GoalChooserFlowTests.SnapshotAsync(other));
    }
}
