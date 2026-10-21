using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-429: complete original plan publication, exact-file/history writer and retained nested/raw ownership.</summary>
[Trait("AT", "AT-429")]
public sealed class PlanEditorOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 9);
    private static PlanStore Plans(FlowFixture f) => f.Services.GetRequiredService<PlanStore>();
    private static ReminderService Reminders(FlowFixture f) => new(new NoReminderScheduler(), f.Store, Plans(f),
        f.Services.GetRequiredService<GoalStore>(), f.Services.GetRequiredService<HoldingStore>(), f.Preferences, f.Translator,
        new DateFormatter(f.Localization), f.Localization, f.Time);
    private static PlanEditorViewModel Editor(FlowFixture f, ReminderService reminders) =>
        new(f.Store, Plans(f), f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
    private static async Task<Account> AccountAsync(FlowFixture f)
    {
        var account = new Account { Name = "Fictitious original cash", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct); return account;
    }
    private static Schedule Plan(Account account, string name = "Original plan") => new()
    { Name = name, AccountId = account.Id, Amount = 1200, Rule = new() { Frequency = Frequency.Monthly, Start = Today }, Note = "Original note" };
    private static async Task<ZananceDbContext> DbAsync(FlowFixture f) =>
        await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
    private sealed class InactiveAccess : ICommercialWriteAccessSource
    {
        internal bool Refuse;
        public CommercialWriteAccess Capture(string databasePath) => Refuse ? throw new InvalidOperationException("Fictitious cached read unavailable") : CommercialWriteAccess.Inactive;
    }

    /// <summary>Verifies that initial read failure has only explicit original retry and publishes complete fields without settings creation.</summary>
    [Fact]
    public async Task Initial_read_failure_has_only_explicit_original_retry_and_publishes_complete_fields_without_settings_creation()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var access = new InactiveAccess { Refuse = true };
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var store = new ZananceStore(factory, f.Time, access);
        var plans = new PlanStore(factory, store, access); await using var reminders = Reminders(f);
        var vm = new PlanEditorViewModel(store, plans, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object>());
        Assert.True(vm.Loading.HasFailed); Assert.True(vm.CanReloadForm); Assert.False(vm.FormVisible); Assert.False(vm.CanSave);
        access.Refuse = false; await vm.ReloadFormCommand.ExecuteAsync(null);
        Assert.True(vm.FormVisible); Assert.True(vm.CanInteract); Assert.Equal(account.Id, vm.Account!.Id); Assert.Empty(vm.Name);
        await using var db = await DbAsync(f); Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Schedules.ToListAsync(Ct));
    }

    /// <summary>Verifies that retired or saved original transaction cannot lend its file or writer to a nested plan.</summary>
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Retired_or_saved_original_transaction_cannot_lend_its_file_or_writer_to_a_nested_plan(bool savedParent)
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var parent = await f.Store.ReadEntryEditorSnapshotAsync(new(Today), ct: Ct);
        var current = true; var origin = new EntryPlanOrigin(parent, () => current);
        var draft = new EntryPlanDraft { Name = "Original raw transaction", AccountId = account.Id, CurrencyCode = "EUR", Kind = EntryKind.Expense, Amount = 1234, Date = Today };
        await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["entryDraft"] = draft, ["entryOrigin"] = origin }); Assert.True(vm.CanSave);
        var oldSave = vm.SaveCommand;
        if (savedParent)
        {
            var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 999, Date = Today };
            Assert.True((await f.Store.SaveEditorEntriesAsync(parent.Inputs.Resources, [entry], [], entry.Id, () => true, _ => { }, Ct, parent)).Succeeded);
        }
        else current = false;
        Assert.False(vm.CanSave); await oldSave.ExecuteAsync(null); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct));
        Assert.Equal("Original raw transaction", vm.Name); Assert.Equal(savedParent ? 1 : 0, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    /// <summary>Verifies that retired plan account writer rolls back both new account and default before publication.</summary>
    [Fact]
    public async Task Retired_plan_account_writer_rolls_back_both_new_account_and_default_before_publication()
    {
        using var f = new FlowFixture(); var plans = Plans(f); var source = await plans.ReadEditorSnapshotAsync(new(Today), ct: Ct);
        var current = true; var visit = new PlanAccountHandoff(plans, source, () => current);
        f.Time.BeforeRead = () => current = false;
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => visit.SaveAsync(f.Store,
            new Account { Name = "Uncommitted child", CurrencyCode = "EUR", OpeningDate = Today }, makeDefault: true));
        f.Time.BeforeRead = null; Assert.Null(visit.Accepted); Assert.Null(visit.AccountId);
        await using var db = await DbAsync(f); Assert.Empty(await db.Accounts.ToListAsync(Ct)); Assert.Empty(await db.Settings.ToListAsync(Ct));
    }

    /// <summary>Verifies that historical split before active start retains the core clamp and moves the recorded open override once.</summary>
    [Fact]
    public async Task Historical_split_before_active_start_retains_the_core_clamp_and_moves_the_recorded_open_override_once()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f); var original = Plan(account);
        original.ActiveFrom = Today.AddMonths(1); await plans.SaveScheduleAsync(original, Ct);
        await using (var db = await DbAsync(f))
        { db.OccurrenceStates.Add(new() { ScheduleId = original.Id, OriginalDate = Today.AddMonths(1), Status = OccurrenceStatus.Open, Amount = 1500 }); await db.SaveChangesAsync(Ct); }
        var source = await plans.ReadEditorSnapshotAsync(new(Today, PlanId: original.Id), ct: Ct);
        var before = original.CopyForEditing(); var next = PlanActions.SplitFrom(before, Today);
        Assert.True(await plans.SaveEditorAsync(source, before, next, () => true, () => { }, Ct));
        Assert.Equal(Today.AddMonths(1), (await plans.GetScheduleAsync(next.Id, Ct))!.ActiveFrom);
        var state = Assert.Single(await plans.GetStatesAsync(cancellationToken: Ct)); Assert.Equal(next.Id, state.ScheduleId); Assert.Equal(1500, state.Amount);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> initial) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = initial;
        internal int Opened;
        private ZananceDbContext Observe(ZananceDbContext db)
        { db.Database.GetDbConnection().StateChange += (_, e) => { if (e.CurrentState == System.Data.ConnectionState.Open) Opened++; }; return db; }
        public ZananceDbContext CreateDbContext() => Observe(Current.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Observe(await Current.CreateDbContextAsync(cancellationToken));
    }

    /// <summary>Verifies that foreign actual file is refused before opening its connection or reading same id rows.</summary>
    [Fact]
    public async Task Foreign_actual_file_is_refused_before_opening_its_connection_or_reading_same_id_rows()
    {
        using var original = new FlowFixture(); using var foreign = new FlowFixture(); var account = await AccountAsync(original);
        var factory = new SwitchingFactory(original.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var access = new InactiveAccess();
        var store = new ZananceStore(factory, original.Time, access); var plans = new PlanStore(factory, store, access);
        var source = await plans.ReadEditorSnapshotAsync(new(Today), ct: Ct);
        factory.Current = foreign.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); factory.Opened = 0;
        var before = await GoalChooserFlowTests.SnapshotAsync(foreign);
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.SaveEditorAsync(source, null, Plan(account), () => true, () => Assert.Fail("No acceptance allowed"), Ct));
        Assert.Equal(0, factory.Opened); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(foreign));
    }

    /// <summary>Verifies that trigger changed saved plan rolls back before acceptance and preserves all original rows.</summary>
    [Fact]
    public async Task Trigger_changed_saved_plan_rolls_back_before_acceptance_and_preserves_all_original_rows()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f);
        var source = await plans.ReadEditorSnapshotAsync(new(Today), ct: Ct); var save = new PlanEditorSaveSession(plans, source);
        await using var db = await DbAsync(f);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER AlterPlan AFTER INSERT ON Schedules BEGIN UPDATE Schedules SET Amount = Amount + 1 WHERE Id = NEW.Id; END", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<PlanEditorSourceChangedException>(() => save.SaveAsync(null, Plan(account), () => true));
        Assert.False(save.Accepted); Assert.False(source.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Verifies that original plan keeps its archived account when no active account remains.</summary>
    [Fact]
    public async Task Original_plan_keeps_its_archived_account_when_no_active_account_remains()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plan = Plan(account);
        await Plans(f).SaveScheduleAsync(plan, Ct);
        await using (var db = await DbAsync(f)) await db.Accounts.Where(a => a.Id == account.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsArchived, true), Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = plan.Id });
        Assert.True(vm.HasFormContent); Assert.False(vm.NeedsAccount); Assert.Equal(account.Id, vm.Account!.Id);
        Assert.Equal(plan.Amount, vm.FormReview!.Inputs.Schedule!.Amount); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Verifies that missing explicit plan is covered and never becomes a new plan or creates settings.</summary>
    [Fact]
    public async Task Missing_explicit_plan_is_covered_and_never_becomes_a_new_plan_or_creates_settings()
    {
        using var f = new FlowFixture(); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.CreateVersion7() });
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.FormVisible); Assert.False(vm.CanSave);
        Assert.Equal(f.Translator["Plan_SourceUnavailable"], vm.LoadError);
        await vm.SaveCommand.ExecuteAsync(null);
        await using var db = await DbAsync(f); Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Schedules.ToListAsync(Ct));
        Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Verifies that invalid or ambiguous original route cannot publish defaults.</summary>
    [Theory][InlineData("id")][InlineData("of")][InlineData("fromDebt")]
    public async Task Invalid_or_ambiguous_original_route_cannot_publish_defaults(string key)
    {
        using var f = new FlowFixture(); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { [key] = "invalid-original-id" });
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.FormVisible); Assert.False(vm.CanSave);
        await using var db = await DbAsync(f); Assert.Empty(await db.Settings.ToListAsync(Ct));
    }

    /// <summary>Verifies that fresh query resets optional raw fields rule and contract without writing display defaults.</summary>
    [Fact]
    public async Task Fresh_query_resets_optional_raw_fields_rule_and_contract_without_writing_display_defaults()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f);
        var first = Plan(account, "First original"); first.ContractProvider = "Fictitious provider"; first.ReminderEnabled = true;
        first.Rule.SecondDay = 20; first.Note = "First note"; await plans.SaveScheduleAsync(first, Ct);
        var second = Plan(account, "Second original"); second.Amount = null; second.AmountMode = AmountMode.Unknown; second.Note = null;
        await plans.SaveScheduleAsync(second, Ct); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = first.Id }); Assert.True(vm.FormVisible); Assert.True(vm.ShowContract);
        var oldDisclosure = vm.ToggleContractCommand;
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = second.Id });
        Assert.True(vm.FormVisible); Assert.Equal(second.Name, vm.Name); Assert.Empty(vm.Note); Assert.Empty(vm.AmountText);
        Assert.Empty(vm.ContractProvider); Assert.False(vm.HasSecondDay); Assert.False(vm.ReminderEnabled); Assert.False(vm.ShowContract);
        oldDisclosure.Execute(null); Assert.False(vm.ShowContract);
        await using var db = await DbAsync(f); Assert.Empty(await db.Settings.ToListAsync(Ct));
    }

    /// <summary>Verifies that explicit same source review keeps raw money notes and recurrence without save.</summary>
    [Fact]
    public async Task Explicit_same_source_review_keeps_raw_money_notes_and_recurrence_without_save()
    {
        using var f = new FlowFixture(); await AccountAsync(f); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Name = " Raw name "; vm.AmountText = " 12.34 "; vm.Note = "Retained\nsecond line"; vm.IntervalText = " 7 ";
        var raw = (vm.Name, vm.AmountText, vm.Note, vm.IntervalText, vm.Account!.Id, vm.FormDraftId);
        await vm.ReviewWorkCommand.ExecuteAsync(null);
        Assert.Equal(raw, (vm.Name, vm.AmountText, vm.Note, vm.IntervalText, vm.Account!.Id, vm.FormDraftId));
        Assert.True(vm.FormVisible); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Verifies that changed original reference cannot be adopted by explicit review or saved from old raw fields.</summary>
    [Fact]
    public async Task Changed_original_reference_cannot_be_adopted_by_explicit_review_or_saved_from_old_raw_fields()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Name = "Retained raw"; vm.AmountText = "15.67";
        account.Name = "Changed original reference"; await f.Store.SaveAccountAsync(account, Ct);
        await vm.ReviewWorkCommand.ExecuteAsync(null);
        Assert.False(vm.FormVisible); Assert.False(vm.CanSave); Assert.Equal("15.67", vm.AmountText); Assert.Equal("Retained raw", vm.Name);
        Assert.Equal(f.Translator["Editor_SourceChanged"], vm.LoadError); Assert.False(vm.CanReloadForm);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct));
    }

    /// <summary>Verifies that open changed occurrence is history and split moves only its identity not its money.</summary>
    [Fact]
    public async Task Open_changed_occurrence_is_history_and_split_moves_only_its_identity_not_its_money()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f); var original = Plan(account);
        await plans.SaveScheduleAsync(original, Ct);
        await using (var db = await DbAsync(f))
        { db.OccurrenceStates.Add(new() { ScheduleId = original.Id, OriginalDate = Today, Status = OccurrenceStatus.Open, DueDate = Today.AddDays(2), Amount = 2300 }); await db.SaveChangesAsync(Ct); }
        var source = await plans.ReadEditorSnapshotAsync(new(Today, PlanId: original.Id), ct: Ct); Assert.True(source.HasHistory);
        var before = original.CopyForEditing(); var next = PlanActions.SplitFrom(before, Today); next.Name = "Explicit continuation";
        var save = new PlanEditorSaveSession(plans, source); Assert.True(await save.SaveAsync(before, next, () => true));
        var state = Assert.Single(await plans.GetStatesAsync(cancellationToken: Ct));
        Assert.Equal(next.Id, state.ScheduleId); Assert.Equal(2300, state.Amount); Assert.Equal(Today.AddDays(2), state.DueDate);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.True(save.Accepted);
    }

    /// <summary>Verifies that new occurrence or payment after review refuses unsplit replacement.</summary>
    [Theory][InlineData(false)][InlineData(true)]
    public async Task New_occurrence_or_payment_after_review_refuses_unsplit_replacement(bool payment)
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f); var original = Plan(account);
        await plans.SaveScheduleAsync(original, Ct); var source = await plans.ReadEditorSnapshotAsync(new(Today, PlanId: original.Id), ct: Ct);
        await using (var db = await DbAsync(f))
        {
            if (payment) db.Entries.Add(new() { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 300, Date = Today, ScheduleId = original.Id, OccurrenceDate = Today });
            else db.OccurrenceStates.Add(new() { ScheduleId = original.Id, OriginalDate = Today, Status = OccurrenceStatus.Open, Amount = 300 });
            await db.SaveChangesAsync(Ct);
        }
        var proposal = original.CopyForEditing(); proposal.Name = "Must not replace original history";
        await Assert.ThrowsAsync<PlanEditorSourceChangedException>(() => plans.SaveEditorAsync(source, null, proposal, () => true, () => Assert.Fail("No commit allowed"), Ct));
        Assert.Equal(original.Name, (await plans.GetScheduleAsync(original.Id, Ct))!.Name); Assert.False(source.Accepted);
    }

    /// <summary>Verifies that retired writer rolls back and keeps stable destination for only explicit uncommitted retry.</summary>
    [Fact]
    public async Task Retired_writer_rolls_back_and_keeps_stable_destination_for_only_explicit_uncommitted_retry()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f);
        var source = await plans.ReadEditorSnapshotAsync(new(Today), ct: Ct); var target = Plan(account); var current = true;
        var save = new PlanEditorSaveSession(plans, source); f.Time.BeforeRead = () => current = false;
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => save.SaveAsync(null, target, () => current));
        f.Time.BeforeRead = null; Assert.Empty(await plans.GetSchedulesAsync(Ct)); Assert.False(save.Accepted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveAsync(null, Plan(account), () => true));
        Assert.True(await save.SaveAsync(null, target, () => true)); Assert.Single(await plans.GetSchedulesAsync(Ct));
    }

    /// <summary>Verifies that known plan notification failure is saved once before observer and cannot rearm the form.</summary>
    [Fact]
    public async Task Known_plan_notification_failure_is_saved_once_before_observer_and_cannot_rearm_the_form()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f);
        var source = await plans.ReadEditorSnapshotAsync(new(Today), ct: Ct); var target = Plan(account);
        var save = new PlanEditorSaveSession(plans, source);
        plans.Changed += (_, _) => { Assert.True(save.Accepted); Assert.True(source.Accepted); throw new IOException("Fictitious observer refusal"); };
        Assert.True(await save.SaveAsync(null, target, () => true)); Assert.True(save.NotificationFailed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveAsync(null, target, () => true));
        Assert.Single(await plans.GetSchedulesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Verifies that old discard answer and native disclosure cannot close or mutate a new query.</summary>
    [Fact]
    public async Task Old_discard_answer_and_native_disclosure_cannot_close_or_mutate_a_new_query()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f);
        var first = Plan(account, "First"); var next = Plan(account, "Next"); await plans.SaveSchedulesAsync([first, next], Ct);
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = first.Id });
        vm.Note = "Original unsaved input"; var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => answer.Task; var oldClose = vm.CancelCommand; var oldDisclosure = vm.ToggleContractCommand;
        var waiting = oldClose.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = next.Id }); answer.SetResult(true); await waiting;
        oldDisclosure.Execute(null); Assert.Equal("Next", vm.Name); Assert.False(vm.ShowContract); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Verifies that explicit plan account child returns known metadata only and preserves the original raw draft.</summary>
    [Fact]
    public async Task Explicit_plan_account_child_returns_known_metadata_only_and_preserves_the_original_raw_draft()
    {
        using var f = new FlowFixture(); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object>()); Assert.True(vm.NeedsAccount);
        vm.Name = "  Original raw plan  "; vm.AmountText = " 19.95 "; vm.Note = "Original pending note";
        var draft = vm.FormDraftId; await vm.AddAccountCommand.ExecuteAsync(null);
        var route = Assert.Single(f.Platform.Routes); var visit = Assert.IsType<PlanAccountHandoff>(route.Parameters!["originalAccountHandoff"]);
        vm.RetireForm(); var child = new AccountEditorViewModel(f.Store, Plans(f), f.Translator, f.Localization, f.Time, f.Platform);
        await child.ApplyQueryAsync(new Dictionary<string, object> { ["originalAccountHandoff"] = visit });
        child.Form.Name = "Explicit first plan account"; child.IsDefault = true; await child.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(visit.Accepted); await vm.ResumeFormAsync();
        Assert.True(vm.FormVisible); Assert.Equal(draft, vm.FormDraftId); Assert.Equal("  Original raw plan  ", vm.Name);
        Assert.Equal(" 19.95 ", vm.AmountText); Assert.Equal("Original pending note", vm.Note); Assert.Equal(visit.AccountId, vm.Account!.Id);
        Assert.Empty(await Plans(f).GetSchedulesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Verifies that missing debt payment account retains direction unknown principal and raw rule through explicit account return.</summary>
    [Theory]
    [InlineData(AccountType.Loan, false)][InlineData(AccountType.Lent, false)]
    [InlineData(AccountType.Loan, true)][InlineData(AccountType.Lent, true)]
    public async Task Missing_debt_payment_account_retains_direction_unknown_principal_and_raw_rule_through_explicit_account_return(AccountType type, bool otherCurrency)
    {
        using var f = new FlowFixture(); var debt = new Account { Name = "Original fictitious debt", Type = type, CurrencyCode = "USD",
            OpeningDate = Today, OpeningBalance = DebtSetup.OpeningBalance(type, 10000), DueDate = Today.AddDays(20), Installment = 1234 };
        await f.Store.SaveAccountAsync(debt, Ct); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["fromDebt"] = debt.Id });
        Assert.True(vm.NeedsAccount); Assert.False(vm.CanSave); Assert.Equal(debt.Name, vm.Name); Assert.True(vm.IsTransfer);
        Assert.Equal((int)AmountMode.Unknown, vm.AmountModeIndex); Assert.Empty(vm.AmountText); Assert.False(vm.AutoPost);
        Assert.Equal(f.Translator["Loan_NoCashAccount"], vm.AccountEmptyText);
        vm.Name = "  Retained raw reminder  "; vm.Note = "Raw debt reminder note"; vm.Start = Today.AddMonths(2); var draft = vm.FormDraftId;
        await vm.AddAccountCommand.ExecuteAsync(null); var route = Assert.Single(f.Platform.Routes);
        var visit = Assert.IsType<PlanAccountHandoff>(route.Parameters!["originalAccountHandoff"]); vm.RetireForm();
        var child = new AccountEditorViewModel(f.Store, Plans(f), f.Translator, f.Localization, f.Time, f.Platform);
        await child.ApplyQueryAsync(new Dictionary<string, object> { ["originalAccountHandoff"] = visit });
        Assert.Equal("USD", child.Form.CurrencyCode); child.Form.Name = "Explicit payment cash";
        if (otherCurrency) child.Form.CurrencyCode = "EUR";
        await child.SaveCommand.ExecuteAsync(null); Assert.NotNull(visit.Accepted); await vm.ResumeFormAsync();
        Assert.Equal("  Retained raw reminder  ", vm.Name); Assert.Equal("Raw debt reminder note", vm.Note); Assert.Equal(Today.AddMonths(2), vm.Start);
        Assert.Equal(draft, vm.FormDraftId); Assert.True(vm.IsTransfer); Assert.Equal(otherCurrency, vm.NeedsAccount);
        if (otherCurrency) { Assert.False(vm.CanSave); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct)); }
        else
        {
            Assert.Equal(type == AccountType.Loan ? visit.AccountId : debt.Id, vm.Account!.Id);
            Assert.Equal(type == AccountType.Loan ? debt.Id : visit.AccountId, vm.ToAccount!.Id);
            Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
            var saved = Assert.Single(await Plans(f).GetSchedulesAsync(Ct)); Assert.Null(saved.Amount); Assert.Equal(AmountMode.Unknown, saved.AmountMode);
            Assert.Equal(Today.AddMonths(2), saved.Rule.Start); Assert.False(saved.AutoPost); Assert.True(saved.ReminderEnabled);
        }
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(2, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Count);
    }

    /// <summary>Verifies that new query during known commit waits for that writer and old save cannot close the new plan.</summary>
    [Fact]
    public async Task New_query_during_known_commit_waits_for_that_writer_and_old_save_cannot_close_the_new_plan()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var plans = Plans(f); var other = Plan(account, "Next original query");
        await plans.SaveScheduleAsync(other, Ct); await using var reminders = Reminders(f); var vm = Editor(f, reminders);
        await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Name = "One accepted new plan"; vm.AmountText = "12.34";
        var oldSave = vm.SaveCommand; Task? nextQuery = null;
        void Changed(object? sender, EventArgs args)
        { plans.Changed -= Changed; nextQuery = vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = other.Id }); }
        plans.Changed += Changed; await oldSave.ExecuteAsync(null); Assert.NotNull(nextQuery); await nextQuery;
        Assert.Equal(other.Name, vm.Name); Assert.True(vm.FormVisible); Assert.Empty(f.Platform.Routes);
        await oldSave.ExecuteAsync(null); Assert.Equal(2, (await plans.GetSchedulesAsync(Ct)).Count);
    }
}
