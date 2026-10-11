using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Dates;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-149: the actual goal editor, original rules and native-effect ports with isolated complete SQLite data.</summary>
[Trait("AT", "AT-149")]
public sealed class GoalEditorFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported { get; set; } = true;
        internal int Requests;
        internal Func<Task<bool>>? Pending;
        public Task<bool> EnableAsync() { Requests++; return Pending?.Invoke() ?? Task.FromResult(true); }
    }
    private sealed record Seed(Goal Goal, Account Account, AssetType Type, Category[] Categories,
        GoalStore Goals, Access Access, Notifications Notifications, GoalEditorViewModel Editor, IDbContextFactory<ZananceDbContext> Factory);
    private static async Task<Seed> SeedAsync(FlowFixture f, GoalType type = GoalType.Earmark,
        ContributionMethod method = ContributionMethod.SpendingCut, bool plan = true, bool advanced = true)
    {
        var account = new Account { Name = "Owned original funding", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), OpeningBalance = 10000, IsArchived = true };
        await f.Store.SaveAccountAsync(account, Ct);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var asset = new AssetType { Name = "Owned original weight", Dimension = AssetDimension.Mass, PriceCurrencyCode = "EUR", Divisible = true, IsArchived = true };
        Assert.True(await holdings.SaveTypeAsync(asset, Ct));
        var categories = new[] { new Category { Name = "Owned category one", Kind = CategoryKind.Expense, IsArchived = true },
            new Category { Name = "Owned category two", Kind = CategoryKind.Expense } };
        foreach (var category in categories) await f.Store.SaveCategoryAsync(category, Ct);
        var goal = new Goal { Name = "Owned original goal", CurrencyCode = "EUR", TargetAmount = 50000, Type = type,
            AccountId = type == GoalType.AccountBalance ? account.Id : null, AssetTypeId = type == GoalType.HoldingQuantity ? asset.Id : null,
            Priority = GoalPriority.High, HomePin = 1, Note = "Owned original note", TargetDate = new(2027, 12, 31) };
        var writer = f.Services.GetRequiredService<GoalStore>();
        await writer.SaveGoalWithContributionPlanAsync(goal, plan ? new ContributionPlan { GoalId = goal.Id, Method = method,
            Amount = 1000, Percent = 12.5m, CategoryIds = [.. categories.Select(c => c.Id)],
            AssumedPricePerUnitMilli = type == GoalType.HoldingQuantity ? 900000 : null,
            Rule = new() { Frequency = Frequency.Monthly, Interval = 3, Start = new(2026, 8, 31), End = EndKind.AfterCount, Count = 12,
                Calendar = PeriodCalendar.Persian, MissingDay = MissingDayPolicy.Skip, DayRule = MonthDayRule.LastDayOfMonth,
                WeekendDays = RecurrenceRule.MaskOf([DayOfWeek.Saturday, DayOfWeek.Sunday]), WeekendShift = WeekendShift.After, HolidayRegion = "DE" } } : null, Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = advanced ? ExperienceMode.Advanced : ExperienceMode.Simple;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var access = new Access();
        var goals = new GoalStore(factory, access); var notifications = new Notifications(); var dates = new DateFormatter(f.Localization);
        var editor = new GoalEditorViewModel(f.Store, goals, f.Translator, f.Localization, dates, f.Time, holdings,
            new HoldingText(f.Translator, f.Localization, dates), f.Platform, notifications);
        return new(goal, account, asset, categories, goals, access, notifications, editor, factory);
    }
    private static Task OpenAsync(Seed s) => s.Editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Goal.Id });
    private static Task<string> RowsAsync(FlowFixture f) => GoalChooserFlowTests.SnapshotAsync(f);
    private static async Task<ContributionPlan?> PlanAsync(Seed s) => (await s.Goals.GetContributionPlansAsync(Ct)).SingleOrDefault();
    private static void Enable(FlowFixture f, Seed s, ProductPlan plan) => s.Access.Current = new(f.Directory.Combine("zanance.db"),
        new(plan, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("78787878-3434-4545-6767-898989898989"))));

    [Theory]
    [InlineData("en", false)] [InlineData("fa", false)] [InlineData("de", false)]
    [InlineData("en", true)] [InlineData("fa", true)] [InlineData("de", true)]
    public async Task Opening_and_live_display_keep_complete_originals_and_never_request_permission(string language, bool advanced)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, GoalType.AccountBalance, advanced: advanced);
        var before = await RowsAsync(f); Assert.True(s.Editor.IsCovered); Assert.False(s.Editor.CanSave);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        await OpenAsync(s); Assert.False(s.Editor.IsCovered); Assert.True(s.Editor.CanSave);
        Assert.Equal(s.Account.Id, s.Editor.Account!.Id); Assert.Equal(3, s.Editor.ScheduleIndex);
        Assert.Equal((int)ContributionMethod.SpendingCut, s.Editor.MethodIndex); Assert.NotNull(s.Editor.RetainedCategoriesText);
        Assert.NotEmpty(s.Editor.UpcomingDates); var amount = s.Editor.AmountText; var contribution = s.Editor.ContributionText;
        var originalRule = JsonSerializer.Serialize((await PlanAsync(s))!.Rule);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == "de"));
        s.Editor.RefreshDisplay(); await OpenAsync(s); await s.Editor.EnsureLoadedAsync();
        Assert.Equal((amount, contribution, 3), (s.Editor.AmountText, s.Editor.ContributionText, s.Editor.ScheduleIndex));
        Assert.Equal(originalRule, JsonSerializer.Serialize((await PlanAsync(s))!.Rule));
        Assert.Equal(0, s.Notifications.Requests); Assert.False(s.Editor.IsDirty); Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData(ContributionMethod.FixedAmount)] [InlineData(ContributionMethod.ShareOfIncome)] [InlineData(ContributionMethod.SpendingCut)]
    public async Task Simple_metadata_save_preserves_complete_saved_rule_categories_and_hidden_method(ContributionMethod method)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, method: method, advanced: false); await OpenAsync(s);
        var original = await PlanAsync(s); s.Editor.Note = "Explicit correction"; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Single(f.Platform.Routes); var current = await PlanAsync(s);
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(current));
        Assert.Equal("Explicit correction", (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.Note);
        Assert.Empty(await s.Goals.GetAllocationsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.False(s.Editor.IsDirty); Assert.False(s.Editor.CanSave);
    }

    [Fact]
    public async Task A_metadata_correction_of_a_legacy_goal_does_not_create_a_contribution_scaffold()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, plan: false); await OpenAsync(s);
        s.Editor.Note = "Explicit metadata only"; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Null(await PlanAsync(s)); Assert.Empty(f.Platform.Failures);
        Assert.Equal("Explicit metadata only", (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.Note);
    }

    [Fact]
    public async Task Native_empty_attachment_query_retains_the_opened_identity_but_another_explicit_identity_is_rejected()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s);
        s.Editor.Note = "Retained unsaved native draft"; var before = await RowsAsync(f);
        await s.Editor.ApplyQueryAsync(new Dictionary<string, object>()); Assert.True(s.Editor.IsDirty);
        Assert.Equal("Retained unsaved native draft", s.Editor.Note);
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() }));
        Assert.Equal(before, await RowsAsync(f)); Assert.True(s.Editor.CanSave); Assert.Equal(3, s.Editor.ScheduleIndex);
    }

    [Theory]
    [InlineData(CalendarSystem.Gregorian, PeriodCalendar.Gregorian)]
    [InlineData(CalendarSystem.Persian, PeriodCalendar.Persian)]
    public async Task New_empty_plan_saves_the_explicit_monthly_anchor_in_its_original_named_calendar(CalendarSystem display, PeriodCalendar expected)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); f.Localization.SetCalendar(display);
        await s.Editor.ApplyQueryAsync(new Dictionary<string, object>()); s.Editor.TypeIndex = 1;
        s.Editor.Name = "Owned new draft"; s.Editor.AmountText = "100"; s.Editor.FirstDate = new(2026, 10, 31);
        f.Localization.SetCalendar(display == CalendarSystem.Persian ? CalendarSystem.Gregorian : CalendarSystem.Persian);
        s.Editor.RefreshDisplay(); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); var goal = (await s.Goals.GetGoalsAsync(Ct)).Single(g => g.Name == "Owned new draft");
        var plan = (await s.Goals.GetContributionPlansAsync(Ct)).Single(p => p.GoalId == goal.Id);
        Assert.Equal(expected, plan.Rule.Calendar); Assert.Equal(new DateOnly(2026, 10, 31), plan.Rule.Start);
        Assert.Null(plan.Amount); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("goal")] [InlineData("plan")] [InlineData("units")] [InlineData("rights")]
    public async Task Changed_original_inputs_reject_the_whole_save_and_keep_every_draft_field(string change)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s);
        s.Editor.Note = "Unsaved review"; s.Editor.AmountText = "444.25"; s.Editor.ContributionText = "7.25";
        if (change == "rights") Enable(f, s, ProductPlan.Free);
        else await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        {
            if (change == "goal") (await db.Goals.SingleAsync(Ct)).Note = "Other actor same clock";
            if (change == "plan") (await db.ContributionPlans.SingleAsync(Ct)).Rule.Count = 13;
            if (change == "units") (await db.AssetTypes.SingleAsync(Ct)).Divisible = false;
            await db.SaveChangesAsync(Ct);
        }
        var before = await RowsAsync(f); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.Single(f.Platform.Failures); Assert.False(s.Editor.CanSave);
        Assert.Equal(("Unsaved review", "444.25", "7.25", 3), (s.Editor.Note, s.Editor.AmountText, s.Editor.ContributionText, s.Editor.ScheduleIndex));
        Assert.True(s.Editor.IsDirty); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Successful_save_is_retired_before_a_failed_native_return_and_cannot_be_resubmitted()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.Note = "Committed once";
        f.Platform.PendingNavigation = () => throw new InvalidOperationException("Owned failed return.");
        await s.Editor.SaveCommand.ExecuteAsync(null); var before = await RowsAsync(f);
        Assert.False(s.Editor.IsDirty); Assert.False(s.Editor.CanSave); Assert.Equal(f.Translator["Common_Close"], s.Editor.CloseText);
        await s.Editor.SaveCommand.ExecuteAsync(null); await s.Editor.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.Single(f.Platform.Routes); Assert.Equal(f.Translator["Goal_SavedReturnFailed"], s.Editor.SaveError);
        f.Platform.PendingNavigation = null; await s.Editor.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    [Fact]
    public async Task Dirty_numeric_input_keeps_its_parse_culture_after_language_and_format_changes()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s);
        s.Editor.AmountText = "1,234.56"; s.Editor.ContributionText = "12.75";
        f.Localization.SetFormattingCulture("de-DE"); s.Editor.RefreshDisplay();
        Assert.Equal("1,234.56", s.Editor.AmountText); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Equal(123456, (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.TargetAmount);
        Assert.Equal(1275, (await PlanAsync(s))!.Amount);
    }

    [Fact]
    public async Task Changed_currency_requires_actual_reentry_instead_of_relabeling_raw_input()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.AmountText = "12.34";
        s.Editor.CurrencyCode = "JPY"; Assert.False(s.Editor.CanSave); Assert.NotNull(s.Editor.AmountError);
        var before = await RowsAsync(f); await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await RowsAsync(f));
        s.Editor.AmountText = string.Empty; s.Editor.AmountText = "13";
        s.Editor.ContributionText = string.Empty; s.Editor.ContributionText = "2";
        Assert.True(s.Editor.CanSave); await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        Assert.Equal(13, (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.TargetAmount); Assert.Equal(2, (await PlanAsync(s))!.Amount);
    }

    [Fact]
    public async Task Reload_merges_only_dirty_fields_and_requires_explicit_review_before_overwriting_saved_edits()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.Note = "My unsaved note";
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        { var goal = await db.Goals.SingleAsync(Ct); goal.Name = "Other actor name"; goal.Note = "Other actor note"; goal.TargetAmount = 77700; await db.SaveChangesAsync(Ct); }
        var before = await RowsAsync(f); await s.Editor.ReloadCommand.ExecuteAsync(null);
        Assert.Equal("Other actor name", s.Editor.Name); Assert.Equal("My unsaved note", s.Editor.Note);
        Assert.Equal("777.00", s.Editor.AmountText); Assert.True(s.Editor.RequiresReview); Assert.False(s.Editor.CanSave);
        Assert.Contains("Other actor note", s.Editor.ReloadedSavedSummary!); Assert.Equal(before, await RowsAsync(f));
        s.Editor.AcceptReviewCommand.Execute(null); Assert.True(s.Editor.CanSave); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Equal("My unsaved note", (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.Note);
    }

    [Fact]
    public async Task All_invalid_visible_fields_report_together_without_erasing_the_original_plan()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s);
        s.Editor.Name = " "; s.Editor.AmountText = "invalid"; s.Editor.ContributionText = "invalid";
        var before = await RowsAsync(f); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(s.Editor.NameError); Assert.NotNull(s.Editor.AmountError); Assert.NotNull(s.Editor.ContributionError);
        Assert.Equal(before, await RowsAsync(f)); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Free_retained_paid_corrections_stay_available_without_unlocking_new_methods_or_reminders()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, GoalType.AccountBalance); Enable(f, s, ProductPlan.Free); await OpenAsync(s);
        Assert.True(s.Editor.CanEditContribution); Assert.False(s.Editor.CanEditReminder); Assert.NotNull(s.Editor.WorkText);
        s.Editor.ContributionText = "11.25"; s.Editor.Note = "Retained correction"; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Equal(1125, (await PlanAsync(s))!.Amount);
    }

    [Fact]
    public async Task Free_empty_basic_goal_cannot_start_paid_configuration_even_through_direct_property_writes()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, GoalType.AccountBalance, plan: false); Enable(f, s, ProductPlan.Free); await OpenAsync(s);
        Assert.False(s.Editor.CanEditContribution); Assert.False(s.Editor.CanChooseMethod); Assert.False(s.Editor.CanEditReminder);
        s.Editor.ContributionText = "10"; s.Editor.ReminderEnabled = true; Assert.False(s.Editor.CanSave);
        var before = await RowsAsync(f); await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await RowsAsync(f));
        Assert.Equal(0, s.Notifications.Requests); s.Editor.ContributionText = ""; s.Editor.ReminderEnabled = false;
        s.Editor.Note = "Basic correction"; Assert.True(s.Editor.CanSave); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Null(await PlanAsync(s)); Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Paused_selected_paid_configuration_does_not_resume_or_request_notification_permission()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, plan: false); s.Goal.State = GoalState.Paused;
        await s.Goals.SaveGoalAsync(s.Goal, Ct); Enable(f, s, ProductPlan.Pro); await OpenAsync(s);
        s.Editor.ContributionText = "10"; s.Editor.ReminderEnabled = true; Assert.True(s.Editor.CanSave);
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        Assert.Equal(GoalState.Paused, (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.State); Assert.True((await PlanAsync(s))!.ReminderEnabled);
        Assert.Equal(0, s.Notifications.Requests); Assert.False(s.Editor.CanApplyCut);
    }

    [Fact]
    public async Task Explicit_native_permission_owns_the_busy_guard_but_never_saves_money_or_configuration()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); var before = await RowsAsync(f);
        var answer = new TaskCompletionSource<bool>(); s.Notifications.Pending = () => answer.Task; s.Editor.ReminderEnabled = true;
        Assert.True(s.Editor.IsBusy); Assert.False(s.Editor.CanSave); Assert.False(s.Editor.CanLeave);
        await s.Editor.CancelCommand.ExecuteAsync(null); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await RowsAsync(f)); answer.SetResult(false);
        for (var i = 0; i < 100 && s.Editor.IsBusy; i++) await Task.Delay(1, Ct);
        Assert.False(s.Editor.IsBusy); Assert.True(s.Editor.ReminderEnabled); Assert.Equal(1, s.Notifications.Requests);
        Assert.Equal(f.Translator["Reminder_PermissionDenied"], s.Editor.ReminderNote); Assert.Equal(before, await RowsAsync(f));
    }

    private static async Task<Budget> PrepareCutAsync(FlowFixture f, Seed s)
    {
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        { (await db.Categories.SingleAsync(c => c.Id == s.Categories[0].Id, Ct)).IsArchived = false; await db.SaveChangesAsync(Ct); }
        var budget = new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 20000,
            AccountIds = [s.Account.Id], Rollover = BudgetRollover.Surplus, CategoryLimits = [new() { CategoryId = s.Categories[0].Id, Limit = 5000 }, new() { CategoryId = s.Categories[1].Id, Limit = 7000 }] };
        await f.Store.SaveBudgetAsync(budget, Ct); await OpenAsync(s); Assert.True(s.Editor.CanApplyCut); return budget;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Applying_a_cut_changes_only_the_confirmed_category_and_never_saves_the_goal_draft(bool confirm)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await PrepareCutAsync(f, s);
        var before = await RowsAsync(f); s.Editor.Note = "Still unsaved"; f.Platform.Confirmations.Enqueue(confirm);
        await s.Editor.ApplyCutCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        var budget = await f.Store.GetBudgetAsync(2026, 10, PeriodCalendar.Gregorian, "EUR", Ct);
        Assert.Equal(confirm ? 4000 : 5000, budget!.CategoryLimits.Single(c => c.CategoryId == s.Categories[0].Id).Limit);
        Assert.Equal(7000, budget.CategoryLimits.Single(c => c.CategoryId == s.Categories[1].Id).Limit); Assert.Equal(20000, budget.TotalLimit);
        Assert.Equal("Owned original note", (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.Note);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(await s.Goals.GetAllocationsAsync(cancellationToken: Ct));
        if (!confirm) Assert.Equal(before, await RowsAsync(f));
        else
        {
            Assert.False(s.Editor.CanApplyCut); s.Editor.Name += " different"; s.Editor.RefreshDisplay(); Assert.False(s.Editor.CanApplyCut);
            var after = await RowsAsync(f); await s.Editor.ApplyCutCommand.ExecuteAsync(null); Assert.Equal(after, await RowsAsync(f));
        }
    }

    [Theory]
    [InlineData("budget")] [InlineData("settings")] [InlineData("date")] [InlineData("category")] [InlineData("goal")]
    public async Task Changed_inputs_during_budget_confirmation_never_overwrite_the_other_actor(string change)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await PrepareCutAsync(f, s); string? changed = null;
        f.Platform.PendingConfirmation = async () =>
        {
            if (change == "date") f.Time.Now = f.Time.Now.AddDays(1);
            else await using (var db = await s.Factory.CreateDbContextAsync(Ct))
            {
                if (change == "budget") (await db.Budgets.SingleAsync(Ct)).CategoryLimits[1].Limit = 7777;
                if (change == "settings") (await db.Settings.SingleAsync(Ct)).MonthStartDay = 15;
                if (change == "category") (await db.Categories.SingleAsync(c => c.Id == s.Categories[0].Id, Ct)).Name = "Changed same clock";
                if (change == "goal") (await db.Goals.SingleAsync(Ct)).Note = "Changed same clock";
                await db.SaveChangesAsync(Ct);
            }
            changed = await RowsAsync(f); return true;
        };
        await s.Editor.ApplyCutCommand.ExecuteAsync(null); Assert.Single(f.Platform.Failures); Assert.NotNull(changed);
        Assert.Equal(changed, await RowsAsync(f)); Assert.False(s.Editor.CanSave); Assert.Equal("Owned original note", s.Editor.Note);
    }

    [Fact]
    public async Task Budget_confirmation_cannot_race_save_close_reload_or_another_cut()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await PrepareCutAsync(f, s);
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task;
        var before = await RowsAsync(f); var pending = s.Editor.ApplyCutCommand.ExecuteAsync(null);
        for (var i = 0; i < 100 && f.Platform.ConfirmationDialogs.Count == 0; i++) await Task.Delay(1, Ct);
        Assert.True(s.Editor.IsBusy); Assert.False(s.Editor.CanApplyCut); Assert.False(s.Editor.CanSave);
        await s.Editor.ApplyCutCommand.ExecuteAsync(null); await s.Editor.SaveCommand.ExecuteAsync(null);
        await s.Editor.ReloadCommand.ExecuteAsync(null); await s.Editor.CancelCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(before, await RowsAsync(f));
        answer.SetResult(false); await pending; Assert.False(s.Editor.IsBusy); Assert.True(s.Editor.CanApplyCut);
    }

    [Fact]
    public async Task A_review_from_another_actual_file_is_rejected_before_goal_or_budget_mutation()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var s = await SeedAsync(f);
        var review = await s.Goals.GetWorkSnapshotAsync(Ct); var writer = other.Services.GetRequiredService<GoalStore>();
        var before = await RowsAsync(other);
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.SaveGoalWithContributionPlanAsync(s.Goal.Clone(), awaitPlan(), Ct, review));
        Assert.Equal(before, await RowsAsync(other));
        ContributionPlan awaitPlan() => new() { GoalId = s.Goal.Id, Amount = 999, Rule = new() { Start = new(2026, 10, 9), Frequency = Frequency.Monthly } };
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Quantity_reference_changes_clear_an_assumption_only_after_explicit_confirmation(bool confirm)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, GoalType.HoldingQuantity, ContributionMethod.FixedAmount);
        var holdings = f.Services.GetRequiredService<HoldingStore>(); var replacement = new AssetType
        { Name = "Owned replacement weight", Dimension = AssetDimension.Mass, Divisible = true, PriceCurrencyCode = "EUR" };
        Assert.True(await holdings.SaveTypeAsync(replacement, Ct)); await OpenAsync(s);
        s.Editor.HoldingType = s.Editor.HoldingTypes.Single(t => t.Id == replacement.Id);
        s.Editor.AmountText = ""; s.Editor.AmountText = "50";
        s.Editor.ContributionText = ""; s.Editor.ContributionText = "1";
        var before = await RowsAsync(f); f.Platform.Confirmations.Enqueue(confirm); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Failures);
        if (confirm) { Assert.Null((await PlanAsync(s))!.AssumedPricePerUnitMilli); Assert.Equal(replacement.Id, (await s.Goals.GetGoalAsync(s.Goal.Id, Ct))!.AssetTypeId); }
        else { Assert.Equal(before, await RowsAsync(f)); Assert.True(s.Editor.IsDirty); Assert.Equal(replacement.Id, s.Editor.HoldingType!.Id); }
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_failed_confirmation_keeps_the_unsaved_form_without_escaping_the_command()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, GoalType.HoldingQuantity, ContributionMethod.FixedAmount); await OpenAsync(s);
        s.Editor.CurrencyCode = "GBP"; s.Editor.Note = "Unsaved changed price currency";
        var before = await RowsAsync(f); f.Platform.PendingConfirmation = () => throw new InvalidOperationException("Owned failed confirmation.");
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Single(f.Platform.Failures); Assert.True(s.Editor.IsDirty);
        Assert.False(s.Editor.IsBusy); Assert.Equal(before, await RowsAsync(f)); Assert.Equal("GBP", s.Editor.CurrencyCode);
    }

    [Fact]
    public async Task An_explicit_start_date_change_keeps_saved_end_calendar_holidays_and_all_categories()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); var original = (await PlanAsync(s))!;
        s.Editor.FirstDate = new(2026, 11, 15); await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        var current = (await PlanAsync(s))!; original.Rule.Start = new(2026, 11, 15);
        Assert.Equal(JsonSerializer.Serialize(original.Rule), JsonSerializer.Serialize(current.Rule)); Assert.Equal(original.CategoryIds, current.CategoryIds);
    }

    [Fact]
    public async Task Reloading_changed_measurement_facts_requires_reentry_of_a_retained_numeric_draft()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, GoalType.HoldingQuantity, ContributionMethod.FixedAmount); await OpenAsync(s);
        s.Editor.AmountText = "49.5";
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        { (await db.AssetTypes.SingleAsync(Ct)).Divisible = false; await db.SaveChangesAsync(Ct); }
        var before = await RowsAsync(f); await s.Editor.ReloadCommand.ExecuteAsync(null); Assert.True(s.Editor.RequiresReview);
        Assert.Equal("49.5", s.Editor.AmountText); s.Editor.AcceptReviewCommand.Execute(null); Assert.False(s.Editor.CanSave);
        Assert.NotNull(s.Editor.AmountError); Assert.Equal(before, await RowsAsync(f));
    }
}
