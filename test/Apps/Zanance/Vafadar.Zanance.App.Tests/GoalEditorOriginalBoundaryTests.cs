using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-640: actual original goal readers cannot publish into retired native form ownership.</summary>
public sealed class GoalEditorOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("A read must not request notification permission.");
    }

    /// <summary>A delayed original SQLite reader cannot uncover a form after native departure.</summary>
    [Fact]
    public async Task Departed_original_goal_reader_never_publishes_ready_or_replaces_fields()
    {
        using var f = new FlowFixture();
        await f.Store.GetSettingsAsync(Ct);
        var goal = new Goal { Name = "D640 original pending owner", Type = GoalType.Earmark,
            CurrencyCode = "EUR", TargetAmount = 15000 };
        await f.Services.GetRequiredService<GoalStore>().SaveGoalAsync(goal, Ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        {
            Before = async token => { if (!first) return; first = false; entered.TrySetResult(); await release.Task.WaitAsync(token); }
        };
        var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var goals = new GoalStore(factory, access); var holdings = f.Services.GetRequiredService<HoldingStore>();
        var dates = new DateFormatter(f.Localization);
        var editor = new GoalEditorViewModel(f.Store, goals, f.Translator, f.Localization, dates, f.Time,
            holdings, new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        await entered.Task.WaitAsync(Ct); editor.SetViewActive(false); release.TrySetResult(); await pending;
        Assert.False(editor.IsReady, "The original departed reader must remain covered.");
        Assert.Empty(editor.Name);
        Assert.False(editor.CanSave); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Returning temporarily to the first file cannot validate references read from another actual SQLite file.</summary>
    [Fact]
    public async Task Temporary_profile_switch_never_publishes_foreign_account_name_in_original_goal()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        await f.Store.GetSettingsAsync(Ct); await other.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "D640 original account", CurrencyCode = "EUR", Type = AccountType.Checking,
            OpeningDate = new(2026, 10, 1), OpeningBalance = 10000 };
        await f.Store.SaveAccountAsync(account, Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(other).CreateDbContextAsync(Ct))
        {
            var foreign = new Account { Name = "D640 foreign same identity", CurrencyCode = "EUR", Type = account.Type,
                OpeningDate = account.OpeningDate, OpeningBalance = 20000 };
            db.Accounts.Add(foreign); db.Entry(foreign).Property(row => row.Id).CurrentValue = account.Id;
            await db.SaveChangesAsync(Ct);
        }
        var goal = new Goal { Name = "D640 original balance goal", Type = GoalType.AccountBalance,
            CurrencyCode = "EUR", TargetAmount = 15000, AccountId = account.Id };
        await f.Services.GetRequiredService<GoalStore>().SaveGoalAsync(goal, Ct);
        var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => ++calls is >= 2 and <= 5 ? AccountDetailOriginalBoundaryTests.Factory(other) : AccountDetailOriginalBoundaryTests.Factory(f) };
        var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var store = new ZananceStore(factory, f.Time, access); var goals = new GoalStore(factory, access);
        var dates = new DateFormatter(f.Localization); var holdings = f.Services.GetRequiredService<HoldingStore>();
        var editor = new GoalEditorViewModel(store, goals, f.Translator, f.Localization, dates, f.Time,
            holdings, new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var foreignBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.DoesNotContain(editor.Accounts, row => row.Name == "D640 foreign same identity");
        if (editor.IsReady) Assert.Equal(account.Name, Assert.Single(editor.Accounts).Name);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(foreignBefore, await GoalChooserFlowTests.SnapshotAsync(other));
    }
}
