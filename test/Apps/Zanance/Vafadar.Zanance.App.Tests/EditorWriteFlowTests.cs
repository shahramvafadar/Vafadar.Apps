using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-165: actual editor writers retain complete drafts and distinguish rejected writes from committed follow-ups.</summary>
[Trait("AT", "AT-165")]
public sealed class EditorWriteFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private static void Enable(FlowFixture f, Access access, ProductPlan tier = ProductPlan.Free) => access.Current = new(f.Directory.Combine("zanance.db"),
        new(tier, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("34343434-6767-8989-0101-121212121212"))));
    private static (ZananceStore Store, PlanStore Plans) Stores(FlowFixture f, Access access)
    {
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        return (store, new(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), store, access));
    }
    private static AccountEditorViewModel AccountEditor(FlowFixture f, ZananceStore store, PlanStore plans) =>
        new(store, plans, f.Translator, f.Localization, f.Time, f.Platform);
    private static ReminderService Reminders(FlowFixture f, ZananceStore store, PlanStore plans) => new(new NoReminderScheduler(), store, plans,
        f.Services.GetRequiredService<GoalStore>(), f.Services.GetRequiredService<HoldingStore>(), f.Preferences, f.Translator,
        new DateFormatter(f.Localization), f.Localization, f.Time);
    private static PlanEditorViewModel PlanEditor(FlowFixture f, ZananceStore store, PlanStore plans, ReminderService reminders) =>
        new(store, plans, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
    private static async Task<Account> SeedAccountAsync(FlowFixture f, string name)
    {
        var account = new Account { Name = name, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct); return account;
    }

    [Theory]
    [InlineData("en", false)][InlineData("fa", false)][InlineData("de", false)][InlineData("es", false)][InlineData("fr", false)][InlineData("it", false)]
    [InlineData("en", true)][InlineData("fa", true)][InlineData("de", true)][InlineData("es", true)][InlineData("fr", true)][InlineData("it", true)]
    public async Task Rejected_account_or_plan_keeps_raw_draft_all_rows_and_allows_only_an_explicit_fresh_retry(string language, bool plan)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        await f.Store.GetSettingsAsync(Ct); var account = await SeedAccountAsync(f, "Fictitious original funding");
        if (plan)
            for (var i = 0; i < 4; i++) await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(new Schedule
            { Name = "Fictitious original plan " + i, AccountId = account.Id, Amount = 1234, Rule = new() { Frequency = Frequency.Once, Start = new(2026, 10, 20) } }, Ct);
        else { await SeedAccountAsync(f, "Fictitious original second"); }
        var access = new Access(); Enable(f, access); var (store, plans) = Stores(f, access);
        await using var reminders = Reminders(f, store, plans);
        if (plan)
        {
            var vm = PlanEditor(f, store, plans, reminders); Assert.False(vm.CanSave); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Name = "  Complete raw new plan  "; vm.Note = "Complete raw note\nsecond line";
            vm.AmountText = " " + MoneyText.ForInput(45678, "EUR", f.Localization.CurrentCulture) + " "; vm.Start = new(2026, 11, 20);
            var raw = (vm.Name, vm.Note, vm.AmountText, vm.Start, vm.Account!.Id, vm.ReminderDaysText, vm.ReminderTime);
            // A real background addition retires the previously reviewed free slot at the actual writer.
            await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(new Schedule { Name = "Fictitious racing final plan", AccountId = account.Id,
                Rule = new() { Start = new(2026, 10, 20) } }, Ct);
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            await vm.SaveCommand.ExecuteAsync(null); Assert.Contains(f.Translator["Tab_Plans"], vm.SaveError!); Assert.Contains(f.Translator["Commercial_DraftKept"], vm.SaveError!);
            Assert.Equal(raw, (vm.Name, vm.Note, vm.AmountText, vm.Start, vm.Account!.Id, vm.ReminderDaysText, vm.ReminderTime)); Assert.False(vm.CanSave);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
            Enable(f, access, ProductPlan.Plus); await vm.ReviewWorkCommand.ExecuteAsync(null); await vm.SaveCommand.ExecuteAsync(null); Assert.Null(vm.SaveError); Assert.False(vm.CanSave); Assert.False(vm.CanInteract);
            var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(f.Translator["Common_Close"], vm.CloseText); Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
            Assert.Equal(6, (await plans.GetSchedulesAsync(Ct)).Count);
        }
        else
        {
            var vm = AccountEditor(f, store, plans); Assert.False(vm.CanSave); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Form.Name = "  Complete raw new account  "; vm.Form.OpeningText = " " + MoneyText.ForInput(45678, "EUR", f.Localization.CurrentCulture) + " ";
            vm.Form.OpeningDate = new(2026, 9, 20); vm.IsDefault = true; var raw = vm.Form.Snapshot();
            await SeedAccountAsync(f, "Fictitious racing final account"); var before = await GoalChooserFlowTests.SnapshotAsync(f);
            await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(f.Translator["Editor_WorkRetired"], vm.SaveError);
            Assert.Equal(f.Translator["Editor_WorkRetired"], vm.WorkText);
            Assert.Equal(raw, vm.Form.Snapshot()); Assert.True(vm.IsDefault); Assert.False(vm.CanSave); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
            await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.False(vm.CanSave);
            Assert.Contains(f.Translator["Accounts_Title"], vm.WorkText!); Assert.Equal(raw, vm.Form.Snapshot());
            Enable(f, access, ProductPlan.Plus); await vm.ReviewWorkCommand.ExecuteAsync(null); await vm.SaveCommand.ExecuteAsync(null); Assert.Null(vm.SaveError); Assert.False(vm.CanSave); Assert.False(vm.CanInteract);
            var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
            var saved = (await store.GetAccountsAsync(cancellationToken: Ct)).Single(row => row.Name == vm.Form.Name.Trim()); Assert.Equal(45678, saved.OpeningBalance);
            Assert.Equal(saved.Id, (await store.GetSettingsAsync(Ct)).DefaultAccountId); Assert.Equal(f.Translator["Common_Close"], vm.CloseText);
            Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        }
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(f.Platform.Alerts);
    }

    [Theory]
    [InlineData(false, false)][InlineData(true, false)][InlineData(false, true)][InlineData(true, true)]
    public async Task A_failed_return_or_post_commit_subscriber_cannot_be_reported_as_a_rejected_write_or_saved_twice(bool plan, bool subscriber)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); await SeedAccountAsync(f, "Fictitious original funding");
        var access = new Access(); var (store, plans) = Stores(f, access); await using var reminders = Reminders(f, store, plans);
        var rejection = new CommercialWriteRejectedException(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans, 5, 5, 1);
        if (subscriber) { if (plan) plans.Changed += (_, _) => throw rejection; else store.Changed += (_, _) => throw rejection; }
        else f.Platform.PendingNavigation = () => throw new InvalidOperationException("Fictitious native return refusal.");
        if (plan)
        {
            var vm = PlanEditor(f, store, plans, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Name = "Fictitious committed plan"; vm.AmountText = "12";
            await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(f.Translator[subscriber ? "Plan_SavedNeedsReview" : "Editor_SavedReturnFailed"], vm.SaveError);
            Assert.False(vm.CanSave); Assert.False(vm.CanInteract); Assert.Equal(f.Translator["Common_Close"], vm.CloseText);
            var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Single(await plans.GetSchedulesAsync(Ct)); f.Platform.PendingNavigation = null; await vm.CancelCommand.ExecuteAsync(null);
        }
        else
        {
            var vm = AccountEditor(f, store, plans); await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Form.Name = "Fictitious committed account"; vm.Form.OpeningText = "12";
            await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(f.Translator[subscriber ? "Account_SavedNeedsReview" : "Editor_SavedReturnFailed"], vm.SaveError);
            Assert.False(vm.CanSave); Assert.False(vm.CanInteract); Assert.Equal(f.Translator["Common_Close"], vm.CloseText);
            var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(2, (await store.GetAccountsAsync(cancellationToken: Ct)).Count); f.Platform.PendingNavigation = null; await vm.CancelCommand.ExecuteAsync(null);
        }
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Account/default are one original transaction; later observer failure cannot turn its known receipt into a retry.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Atomic_default_rejection_rolls_back_account_and_preference_while_committed_notification_failure_retains_both(bool afterCommit)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var access = new Access(); var (store, plans) = Stores(f, access);
        if (afterCommit) store.Changed += (_, _) => throw new InvalidOperationException("Fictitious original atomic-write subscriber refusal.");
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER OwnedRejectDefault BEFORE UPDATE ON Settings BEGIN SELECT RAISE(ABORT, 'Owned default preference refusal'); END;", Ct);
        }
        var vm = AccountEditor(f, store, plans); await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Form.Name = "Fictitious atomic preferred account";
        vm.Form.OpeningText = "12"; vm.IsDefault = true; var raw = vm.Form.Snapshot(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        if (!afterCommit)
        {
            Assert.Equal(f.Translator["Common_SaveFailed"], vm.SaveError); Assert.True(vm.CanSave); Assert.Equal(raw, vm.Form.Snapshot());
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(await store.GetAccountsAsync(cancellationToken: Ct));
            Assert.Null((await store.GetSettingsAsync(Ct)).DefaultAccountId);
            await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER OwnedRejectDefault;", Ct);
            // Only the failed, wholly rolled-back original attempt is explicitly retried.
            await vm.SaveCommand.ExecuteAsync(null); Assert.Null(vm.SaveError); Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        }
        else { Assert.Equal(f.Translator["Account_SavedNeedsReview"], vm.SaveError); Assert.False(vm.CanSave); }
        var saved = Assert.Single(await store.GetAccountsAsync(cancellationToken: Ct)); Assert.Equal(1200, saved.OpeningBalance);
        Assert.Equal(saved.Id, (await store.GetSettingsAsync(Ct)).DefaultAccountId); Assert.False(vm.CanSave);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(ScheduleState.Active)][InlineData(ScheduleState.Paused)]
    public async Task Rejected_historical_split_keeps_original_state_stop_boundary_and_auto_post_anchor_for_a_corrected_retry(ScheduleState state)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var account = await SeedAccountAsync(f, "Fictitious original funding");
        var original = new Schedule { Name = "Fictitious original historical plan", AccountId = account.Id, Amount = 1234, AutoPost = true, AutoPostFrom = new(2026, 9, 1),
            State = state, PausedFrom = state == ScheduleState.Paused ? new(2026, 10, 3) : null, ActiveUntil = new(2027, 8, 31),
            Rule = new() { Frequency = Frequency.Monthly, Start = new(2026, 9, 1), Calendar = Core.Budgets.PeriodCalendar.Gregorian } };
        var access = new Access(); var (store, plans) = Stores(f, access); await plans.SaveScheduleAsync(original, Ct);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.OccurrenceStates.Add(new() { ScheduleId = original.Id, OriginalDate = new(2026, 9, 1), Status = OccurrenceStatus.Skipped }); await db.SaveChangesAsync(Ct); }
        await using var reminders = Reminders(f, store, plans); Enable(f, access, ProductPlan.Plus);
        var vm = PlanEditor(f, store, plans, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
        vm.Name = "  Fictitious complete revised name  "; vm.Note = "Complete original revised draft"; vm.HasSecondDay = true; vm.SecondDayText = "20";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var raw = (vm.Name, vm.Note, vm.Start, vm.ApplyFrom, vm.Account!.Id, vm.AutoPost);
        Enable(f, access); await vm.SaveCommand.ExecuteAsync(null); Assert.Contains(f.Translator["Commercial_RequiresPlus"], vm.SaveError!); Assert.False(vm.CanSave);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(raw, (vm.Name, vm.Note, vm.Start, vm.ApplyFrom, vm.Account!.Id, vm.AutoPost));
        vm.HasSecondDay = false; await vm.ReviewWorkCommand.ExecuteAsync(null); await vm.SaveCommand.ExecuteAsync(null); Assert.Null(vm.SaveError);
        var next = (await plans.GetSchedulesAsync(Ct)).Single(row => row.PreviousScheduleId == original.Id);
        Assert.Equal(state, next.State); Assert.Equal(original.PausedFrom, next.PausedFrom); Assert.Equal(original.ActiveUntil, next.ActiveUntil);
        Assert.True(next.AutoPost); Assert.Equal(original.AutoPostFrom, next.AutoPostFrom); Assert.Equal(vm.Name.Trim(), next.Name);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct)); Assert.False(vm.CanSave);
    }

    [Fact]
    public void Original_edit_copies_preserve_complete_metadata_identity_and_independent_mutable_rule()
    {
        var original = new Schedule { Name = "Fictitious complete original", State = ScheduleState.Paused, PausedFrom = new(2026, 10, 1), ActiveUntil = new(2027, 10, 1),
            Rule = new() { Frequency = Frequency.Monthly, Start = new(2026, 9, 1), SecondDay = 20 }, ContractReference = "Fictitious complete reference", AutoPost = true };
        var packet = JsonSerializer.Serialize(original); var copy = original.CopyForEditing(); Assert.Equal(packet, JsonSerializer.Serialize(copy)); Assert.NotSame(original.Rule, copy.Rule);
        copy.Rule.SecondDay = 25; PlanActions.SplitFrom(copy, new(2026, 11, 1)); Assert.Equal(packet, JsonSerializer.Serialize(original));
        var account = new Account { Name = "Fictitious complete account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), InterestRate = 0.03m, Installment = 1234, LastReconciledOn = new(2026, 10, 1) };
        var raw = JsonSerializer.Serialize(account); var editable = account.CopyForEditing(); Assert.Equal(raw, JsonSerializer.Serialize(editable)); editable.OpeningBalance = 45678;
        Assert.Equal(raw, JsonSerializer.Serialize(account));
    }
}
