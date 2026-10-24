using Vafadar.Zanance.Testing;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-155: actual displayed budget action flow, complete real SQLite reviews and explicit native-effect ports.</summary>
[Trait("AT", "AT-155")]
public sealed class BudgetActionsFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string path) => Fail ? throw new InvalidOperationException("Owned read failure.") : Current;
    }
    private sealed class Session(ZananceStore store, Access access, Budget source, BudgetVisit visit)
    {
        internal readonly Access Access = access;
        internal readonly ZananceStore Store = store;
        internal readonly Budget Source = source;
        internal BudgetVisit Visit = visit;
        internal BudgetActionsViewModel Actions = null!;
        internal bool FailReload;
        internal Func<Task>? DuringReload;
        internal async Task ReloadAsync(BudgetVisit? next)
        {
            if (next is not null) Visit = next;
            Actions.BeginLoad();
            if (DuringReload is { } during) await during();
            if (FailReload) { Actions.FailLoad(); throw new InvalidOperationException("Owned refresh failure."); }
            var review = await Store.GetBudgetEditorReviewAsync(Ct);
            var original = BudgetActionsViewModel.Find(review.Choices.Items.Select(item => item.ToBudget()), Visit);
            Actions.Publish(review, Visit, original?.Id);
        }
    }
    private static async Task<Session> SeedAsync(FlowFixture f, BudgetPeriod period = BudgetPeriod.Month, bool paid = false, bool existingNext = false, IDbContextFactory<ZananceDbContext>? factory = null)
    {
        var account = new Account { Name = "Original archived account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), IsArchived = true };
        await f.Store.SaveAccountAsync(account, Ct);
        var source = new Budget { Year = period == BudgetPeriod.Month ? 2026 : 0, Month = period == BudgetPeriod.Month ? 10 : 0,
            Calendar = PeriodCalendar.Gregorian, Period = period, PeriodStart = period == BudgetPeriod.Month ? default : new(2026, 10, 5),
            CurrencyCode = "EUR", Method = paid ? BudgetMethod.Envelopes : BudgetMethod.Limits,
            TotalLimit = 12345, AccountIds = [account.Id], CategoryLimits = [new() { CategoryId = Guid.NewGuid(), Limit = 2345 }] };
        await f.Store.SaveBudgetAsync(source, Ct);
        if (existingNext)
        {
            var next = period == BudgetPeriod.Month ? BudgetPlanning.CopyTo(source, 2026, 11)
                : BudgetPlanning.CopyTo(source, BudgetPeriods.Step(period, source.PeriodStart, 1));
            next.TotalLimit = 99999; await f.Store.SaveBudgetAsync(next, Ct);
        }
        var access = new Access(); var store = new ZananceStore(factory ?? f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var session = new Session(store, access, source, new(2026, 10, source.Calendar, period, source.PeriodStart, "EUR"));
        session.Actions = new(store, f.Translator, new DateFormatter(f.Localization), f.Platform, session.ReloadAsync);
        await session.ReloadAsync(null); return session;
    }
    private static Task<string> RowsAsync(FlowFixture f) => GoalChooserFlowTests.SnapshotAsync(f);
    private static async Task<string> NonBudgetsAsync(FlowFixture f)
    {
        var all = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(await RowsAsync(f))!;
        all.Remove("Budgets"); all.Remove("BudgetCategoryLimits"); return JsonSerializer.Serialize(all);
    }
    private static void Enable(FlowFixture f, Session s, ProductPlan plan) => s.Access.Current = new(f.Directory.Combine("zanance.db"),
        new(plan, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("82828282-3434-4545-6767-898989898989"))));

    [Fact]
    public async Task Failed_original_read_keeps_actions_retired_until_successful_explicit_reload()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var before = await RowsAsync(f); s.Access.Fail = true;
        await s.Actions.EditCommand.ExecuteAsync(null); Assert.Single(f.Platform.Failures);
        Assert.True(s.Actions.NeedsReload); Assert.False(s.Actions.CanDelete); Assert.Empty(f.Platform.Routes);
        s.Access.Fail = false; await s.Actions.ReloadCommand.ExecuteAsync(null);
        Assert.True(s.Actions.CanInteract); Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("en")] [InlineData("fa")] [InlineData("de")]
    public async Task Opening_and_live_caption_refresh_never_create_settings_or_modify_any_table(string language)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var before = await RowsAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language)); s.Actions.RefreshDisplay();
        await s.ReloadAsync(null); Assert.True(s.Actions.CanEdit); Assert.True(s.Actions.CanCopyNext); Assert.True(s.Actions.CanDelete);
        Assert.Equal(before, await RowsAsync(f));
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct));
    }

    [Theory]
    [InlineData(BudgetPeriod.Month, false)] [InlineData(BudgetPeriod.Month, true)]
    [InlineData(BudgetPeriod.Week, false)] [InlineData(BudgetPeriod.TwoWeeks, true)]
    public async Task Confirmed_copy_preserves_source_scope_limits_and_canonical_target_coordinates(BudgetPeriod period, bool replacing)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, period, existingNext: replacing);
        var original = JsonSerializer.Serialize((await s.Store.GetBudgetsAsync(Ct)).Single(b => b.Id == s.Source.Id));
        var other = await NonBudgetsAsync(f); f.Platform.Confirmations.Enqueue(true);
        await s.Actions.CopyToNextCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        var budgets = await s.Store.GetBudgetsAsync(Ct); Assert.Equal(2, budgets.Count);
        Assert.Equal(original, JsonSerializer.Serialize(budgets.Single(b => b.Id == s.Source.Id)));
        var copy = budgets.Single(b => b.Id != s.Source.Id);
        Assert.Equal((s.Source.TotalLimit, s.Source.Calendar, s.Source.Method), (copy.TotalLimit, copy.Calendar, copy.Method));
        Assert.Equal(s.Source.AccountIds, copy.AccountIds); Assert.Equal(s.Source.CategoryLimits.Select(l => l.Limit), copy.CategoryLimits.Select(l => l.Limit));
        Assert.Equal(period == BudgetPeriod.Month ? (2026, 11) : (0, 0), (copy.Year, copy.Month));
        Assert.Equal(period == BudgetPeriod.Month ? default : BudgetPeriods.Step(period, s.Source.PeriodStart, 1), copy.PeriodStart);
        FinancialSnapshotAssert.EqualAfterWrites(other, await NonBudgetsAsync(f)); Assert.True(s.Actions.CanInteract);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Cancelled_copy_or_delete_changes_nothing_and_keeps_original_actions_available(bool deleting)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var before = await RowsAsync(f);
        await (deleting ? s.Actions.DeleteCommand : s.Actions.CopyToNextCommand).ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.True(s.Actions.CanInteract); Assert.False(s.Actions.NeedsReload);
    }

    [Theory]
    [InlineData("budget", false)] [InlineData("budget", true)] [InlineData("account", false)]
    [InlineData("settings", true)] [InlineData("date", false)] [InlineData("rights", true)]
    public async Task Change_during_confirmation_rejects_the_original_operation_without_another_write(string change, bool deleting)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, existingNext: true); string? changed = null;
        f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            switch (change)
            {
                case "budget": await db.Budgets.ExecuteUpdateAsync(p => p.SetProperty(b => b.TotalLimit, 77777), Ct); break;
                case "account": await db.Accounts.ExecuteUpdateAsync(p => p.SetProperty(a => a.Name, "Changed without timestamp"), Ct); break;
                case "settings": await s.Store.UpdateSettingsAsync(settings => settings.MonthStartDay = 5, Ct); break;
                case "date": f.Time.Now = f.Time.Now.AddDays(1); break;
                default: Enable(f, s, ProductPlan.Free); break;
            }
            changed = await RowsAsync(f); return true;
        };
        await (deleting ? s.Actions.DeleteCommand : s.Actions.CopyToNextCommand).ExecuteAsync(null);
        Assert.Equal(changed, await RowsAsync(f)); Assert.Single(f.Platform.Failures);
        Assert.True(s.Actions.NeedsReload); Assert.False(s.Actions.CanEdit); Assert.False(s.Actions.CanCopyNext);
        await s.Actions.DeleteCommand.ExecuteAsync(null); Assert.Equal(changed, await RowsAsync(f));
    }

    [Fact]
    public async Task Busy_spans_confirmation_and_post_commit_reload_and_prevents_a_second_action()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var confirm = new TaskCompletionSource<bool>();
        f.Platform.PendingConfirmation = () => confirm.Task;
        var operation = s.Actions.CopyToNextCommand.ExecuteAsync(null);
        for (var attempt = 0; attempt < 100 && f.Platform.ConfirmationDialogs.Count == 0; attempt++) await Task.Delay(10, Ct);
        Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.True(s.Actions.IsBusy); Assert.False(s.Actions.CanDelete); Assert.False(s.Actions.CanEdit);
        await s.Actions.DeleteCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs);
        var refresh = new TaskCompletionSource(); s.DuringReload = () => refresh.Task; confirm.SetResult(true);
        for (var attempt = 0; attempt < 100 && !s.Actions.IsLoading; attempt++) await Task.Delay(10, Ct);
        Assert.True(s.Actions.IsLoading); Assert.True(s.Actions.IsBusy); Assert.False(s.Actions.CanReload);
        refresh.SetResult(); await operation; Assert.True(s.Actions.CanInteract);
        Assert.Equal(2, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Committed_operation_with_failed_refresh_requires_read_only_retry_never_a_repeated_write(bool deleting)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); s.FailReload = true; f.Platform.Confirmations.Enqueue(true);
        await (deleting ? s.Actions.DeleteCommand : s.Actions.CopyToNextCommand).ExecuteAsync(null); var committed = await RowsAsync(f);
        Assert.Equal(f.Translator["Occurrence_SavedRefreshFailed"], s.Actions.Feedback); Assert.True(s.Actions.NeedsReload);
        await s.Actions.CopyToNextCommand.ExecuteAsync(null); await s.Actions.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(committed, await RowsAsync(f)); Assert.Single(f.Platform.ConfirmationDialogs);
        s.FailReload = false; await s.Actions.ReloadCommand.ExecuteAsync(null); Assert.True(s.Actions.CanInteract);
        Assert.Equal(committed, await RowsAsync(f)); Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Free_retains_paid_original_correction_and_delete_but_cannot_copy_paid_tools_to_a_new_period()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, paid: true); Enable(f, s, ProductPlan.Free); await s.ReloadAsync(null);
        var before = await RowsAsync(f); Assert.True(s.Actions.CanEdit); Assert.True(s.Actions.CanDelete); Assert.False(s.Actions.CanCopyNext);
        Assert.NotNull(s.Actions.WorkText); await s.Actions.CopyToNextCommand.ExecuteAsync(null); Assert.Equal(before, await RowsAsync(f));
        await s.Actions.EditCommand.ExecuteAsync(null); var route = Assert.Single(f.Platform.Routes);
        Assert.Equal(s.Source.Id, route.Parameters!["id"]); Assert.Equal(App.AppRoutes.BudgetEditorRoute, route.Route);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Future_basic_copy_is_available_when_todays_single_definition_slot_is_full()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Free); await s.ReloadAsync(null);
        Assert.True(s.Actions.CanCopyNext); f.Platform.Confirmations.Enqueue(true); await s.Actions.CopyToNextCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Equal(2, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Current_new_period_uses_the_selected_definition_without_granting_a_different_account_scope(bool sameScope)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        await s.Store.DeleteBudgetAsync(s.Source.Id, Ct);
        var previous = BudgetPlanning.CopyTo(s.Source, 2026, 9);
        if (!sameScope) previous.AccountIds = [];
        await s.Store.SaveBudgetAsync(previous, Ct);
        var (year, month) = PeriodMath.MonthOf(new(2026, 10, 9), PeriodCalendar.Persian, 1);
        var current = BudgetPlanning.CopyTo(s.Source, year, month); current.Calendar = PeriodCalendar.Persian;
        await s.Store.SaveBudgetAsync(current, Ct); Enable(f, s, ProductPlan.Free);
        s.Visit = s.Visit with { Month = 9 }; await s.ReloadAsync(null);
        Assert.Equal(sameScope, s.Actions.CanCopyNext); var before = await RowsAsync(f); f.Platform.Confirmations.Enqueue(true);
        await s.Actions.CopyToNextCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        if (!sameScope) { Assert.Equal(before, await RowsAsync(f)); Assert.Empty(f.Platform.ConfirmationDialogs); }
        else Assert.Equal(3, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    [Fact]
    public async Task Same_selected_definition_can_be_copied_from_previous_into_current_without_an_extra_slot()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        var previous = BudgetPlanning.CopyTo(s.Source, 2026, 9); await s.Store.SaveBudgetAsync(previous, Ct);
        await s.Store.DeleteBudgetAsync(s.Source.Id, Ct); Enable(f, s, ProductPlan.Free); await s.ReloadAsync(null);
        Assert.True(s.Actions.HasPrevious); Assert.True(s.Actions.CanCopyPrevious);
        await s.Actions.CopyPreviousCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        Assert.True(s.Actions.CanEdit); Assert.Equal(2, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    [Fact]
    public async Task Replacement_of_existing_paid_footprint_remains_a_correction_without_paid_new_tool_rights()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, paid: true, existingNext: true);
        Enable(f, s, ProductPlan.Free); await s.ReloadAsync(null); Assert.True(s.Actions.CanCopyNext);
        f.Platform.Confirmations.Enqueue(true); await s.Actions.CopyToNextCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Equal(2, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    [Fact]
    public async Task Retired_publication_during_confirmation_cannot_delete_the_newly_displayed_period()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, existingNext: true);
        f.Platform.PendingConfirmation = async () => { await s.ReloadAsync(s.Visit with { Month = 11 }); return true; };
        var before = await RowsAsync(f); await s.Actions.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.Empty(f.Platform.Failures); Assert.True(s.Actions.CanInteract);
    }

    [Theory]
    [InlineData("edit")] [InlineData("delete")] [InlineData("copy")]
    public async Task Command_captured_for_original_period_cannot_act_on_a_later_published_budget(string operation)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, existingNext: true);
        var command = operation switch
        {
            "edit" => s.Actions.EditCommand,
            "delete" => s.Actions.DeleteCommand,
            _ => s.Actions.CopyToNextCommand,
        };
        await s.ReloadAsync(s.Visit with { Month = 11 });
        var before = await RowsAsync(f); f.Platform.Confirmations.Enqueue(true);

        await command.ExecuteAsync(null);

        Assert.Equal(before, await RowsAsync(f));
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
        Assert.True(s.Actions.CanInteract);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task Original_budget_commit_remains_accepted_when_its_change_subscriber_throws(int operation)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, existingNext: operation == 1);
        var notifications = 0;
        s.Store.Changed += (_, _) => { notifications++; throw new IOException("Original accepted budget notification failure."); };
        f.Platform.Confirmations.Enqueue(true);

        await (operation == 2 ? s.Actions.DeleteCommand : s.Actions.CopyToNextCommand).ExecuteAsync(null);

        Assert.Equal(1, notifications);
        Assert.Equal(f.Translator["Occurrence_SavedRefreshFailed"], s.Actions.Feedback);
        Assert.Empty(f.Platform.Failures); Assert.True(s.Actions.NeedsReload);
        var accepted = await RowsAsync(f);
        await s.Actions.DeleteCommand.ExecuteAsync(null); await s.Actions.CopyToNextCommand.ExecuteAsync(null);
        Assert.Equal(accepted, await RowsAsync(f)); Assert.Equal(1, notifications);
        Assert.Equal(operation == 2 ? 0 : 2, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    private sealed class BeforeSaveFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        internal Action? BeforeSave;
        public ZananceDbContext CreateDbContext() => Attach(inner.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Attach(await inner.CreateDbContextAsync(cancellationToken));
        private ZananceDbContext Attach(ZananceDbContext db)
        {
            db.SavingChanges += (_, _) => { var callback = BeforeSave; BeforeSave = null; callback?.Invoke(); };
            return db;
        }
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task Original_budget_write_rolls_back_when_native_owner_retires_during_save_changes(int operation)
    {
        using var f = new FlowFixture();
        var factory = new BeforeSaveFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var s = await SeedAsync(f, existingNext: operation == 1, factory: factory);
        var before = await RowsAsync(f); var notifications = 0; s.Store.Changed += (_, _) => notifications++;
        factory.BeforeSave = s.Actions.Retire; f.Platform.Confirmations.Enqueue(true);

        await (operation == 2 ? s.Actions.DeleteCommand : s.Actions.CopyToNextCommand).ExecuteAsync(null);

        Assert.Equal(before, await RowsAsync(f)); Assert.Equal(0, notifications);
        Assert.False(s.Actions.CanInteract); Assert.Empty(f.Platform.Failures);
        Assert.False(s.Actions.NeedsReload);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Pending_original_budget_consent_is_retired_without_poisoning_another_page(bool deleting)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, existingNext: true);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => { entered.SetResult(); return response.Task; };
        var before = await RowsAsync(f);
        var operation = (deleting ? s.Actions.DeleteCommand : s.Actions.CopyToNextCommand).ExecuteAsync(null);
        await entered.Task.WaitAsync(Ct); s.Actions.Retire(); response.SetResult(true); await operation;

        Assert.Equal(before, await RowsAsync(f)); Assert.Empty(f.Platform.Failures);
        Assert.False(s.Actions.CanInteract); Assert.False(s.Actions.NeedsReload);
        await s.ReloadAsync(null); Assert.True(s.Actions.CanInteract);
    }

    [Fact]
    public async Task Original_previous_copy_command_does_not_follow_a_later_missing_period()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, existingNext: true);
        // Reuse the same two original rows; the first displayed missing period has an explicit previous alternative.
        await s.ReloadAsync(s.Visit with { Month = 12 }); var old = s.Actions.CopyPreviousCommand;
        Assert.True(s.Actions.CanCopyPrevious);
        await s.ReloadAsync(s.Visit with { Month = 9 }); var before = await RowsAsync(f);

        await old.ExecuteAsync(null);

        Assert.Equal(before, await RowsAsync(f)); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.False(old.CanExecute(null));
    }

    [Fact]
    public async Task Retired_reload_command_cannot_reload_or_rearm_the_original_page()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var old = s.Actions.ReloadCommand;
        var reads = 0; s.DuringReload = () => { reads++; return Task.CompletedTask; }; s.Actions.Retire();

        await old.ExecuteAsync(null);

        Assert.Equal(0, reads); Assert.False(old.CanExecute(null)); Assert.False(s.Actions.CanInteract);
        Assert.False(s.Actions.CanReload); Assert.False(s.Actions.ReloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task Original_editor_opening_callback_that_retires_page_cannot_navigate()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var before = await RowsAsync(f);
        s.Actions.EditorOpening += (_, _) => s.Actions.Retire();

        await s.Actions.EditCommand.ExecuteAsync(null);

        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures);
        Assert.False(s.Actions.CanInteract); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Original_reload_failure_after_page_retirement_cannot_rearm_retry_or_publish_failure()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var before = await RowsAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        s.DuringReload = async () => { entered.SetResult(); await response.Task; throw new IOException("Retired original read failed."); };
        var operation = s.Actions.ReloadCommand.ExecuteAsync(null); await entered.Task.WaitAsync(Ct);
        s.Actions.Retire(); response.SetResult(); await operation;

        Assert.False(s.Actions.CanReload); Assert.False(s.Actions.NeedsReload); Assert.Empty(f.Platform.Failures);
        Assert.Equal(before, await RowsAsync(f));
    }
}
