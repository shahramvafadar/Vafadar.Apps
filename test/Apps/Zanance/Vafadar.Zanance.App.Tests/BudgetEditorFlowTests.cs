using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-154: actual original budget drafts, paired writers and native-effect ports with complete isolated SQLite data.</summary>
[Trait("AT", "AT-154")]
public sealed class BudgetEditorFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail ? throw new InvalidOperationException("Owned read failure.") : Current;
    }
    private sealed record Seed(Budget Budget, Account Account, Category Category, Guid MissingCategory,
        ZananceStore Store, BudgetEditorViewModel Editor, Access Access, IDbContextFactory<ZananceDbContext> Factory);
    private static async Task<Seed> SeedAsync(FlowFixture f, bool advanced = true, bool paid = true)
    {
        var account = new Account { Name = "Original archived account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), IsArchived = true };
        var category = new Category { Name = "Original archived child", Kind = CategoryKind.Expense, IsArchived = true, ParentId = Guid.NewGuid() };
        await f.Store.SaveAccountAsync(account, Ct);
        // A valid stored parent keeps the deliberately archived child reference complete.
        var parent = new Category { Name = "Original parent", Kind = CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(parent, Ct); category.ParentId = parent.Id;
        await f.Store.SaveCategoryAsync(category, Ct);
        var missing = Guid.NewGuid();
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 12345,
            Method = paid ? BudgetMethod.Envelopes : BudgetMethod.Limits, Rollover = paid ? BudgetRollover.Surplus : BudgetRollover.None,
            AccountIds = [account.Id], CategoryLimits = [new() { CategoryId = category.Id, Limit = 2345 }, new() { CategoryId = missing, Limit = 3456 }] };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = advanced ? ExperienceMode.Advanced : ExperienceMode.Simple;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var access = new Access();
        var store = new ZananceStore(factory, f.Time, access);
        return new(budget, account, category, missing, store, new(store, f.Translator, f.Localization, f.Platform), access, factory);
    }
    private static Task OpenAsync(Seed s) => s.Editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Budget.Id });

    /// <summary>The exact original financial interval remains visible through language changes without reinterpreting dirty amounts.</summary>
    [Theory]
    [InlineData(BudgetPeriod.Month, 1, 1, 31, 10)]
    [InlineData(BudgetPeriod.Month, 15, 15, 14, 11)]
    [InlineData(BudgetPeriod.Week, 1, 4, 10, 10)]
    [InlineData(BudgetPeriod.TwoWeeks, 1, 4, 17, 10)]
    public async Task Original_period_identity_is_published_without_changing_money_or_rows(BudgetPeriod period,
        int startDay, int firstDay, int lastDay, int lastMonth)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, paid: false);
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        {
            var budget = await db.Budgets.SingleAsync(Ct); budget.Period = period;
            if (period != BudgetPeriod.Month) { budget.Year = budget.Month = 0; budget.PeriodStart = new(2026, 10, 4); }
            (await db.Settings.SingleAsync(Ct)).MonthStartDay = startDay; await db.SaveChangesAsync(Ct);
        }
        var before = await RowsAsync(f); await OpenAsync(s);
        var dates = new DateFormatter(f.Localization);
        string Expected() => $"{dates.Format(new DateOnly(2026, 10, firstDay), DateFormatStyle.Short)} – {dates.Format(new DateOnly(2026, lastMonth, lastDay), DateFormatStyle.Short)}";
        Assert.True(s.Editor.IsReady); Assert.Equal(Expected(), s.Editor.PeriodText); Assert.False(s.Editor.IsDirty);
        s.Editor.TotalText = "234.56"; var raw = s.Editor.TotalText;
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(language => language.CultureName == "de"));
        s.Editor.RefreshDisplay();
        Assert.Equal(Expected(), s.Editor.PeriodText); Assert.Equal(raw, s.Editor.TotalText); Assert.True(s.Editor.IsDirty);
        Assert.Equal(before, await RowsAsync(f));
    }
    private static Task<string> RowsAsync(FlowFixture f) => GoalChooserFlowTests.SnapshotAsync(f);
    private static async Task<Budget> SavedAsync(Seed s) => (await s.Store.GetBudgetsAsync(Ct)).Single(b => b.Id == s.Budget.Id);
    private static void Enable(FlowFixture f, Seed s, ProductPlan plan) => s.Access.Current = new(f.Directory.Combine("zanance.db"),
        new(plan, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("82828282-3434-4545-6767-898989898989"))));

    [Theory]
    [InlineData("en", true)] [InlineData("fa", true)] [InlineData("de", true)]
    [InlineData("en", false)] [InlineData("fa", false)] [InlineData("de", false)]
    public async Task Covered_open_and_live_captions_preserve_complete_originals_without_writing(string language, bool advanced)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, advanced); var before = await RowsAsync(f);
        Assert.True(s.Editor.IsCovered); Assert.False(s.Editor.CanSave);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        await OpenAsync(s); Assert.False(s.Editor.IsCovered); Assert.True(s.Editor.CanSave); Assert.False(s.Editor.IsDirty);
        Assert.Contains(s.Editor.Limits, l => l.CategoryId == s.MissingCategory && l.Text.Length > 0);
        Assert.Contains(s.Editor.Limits, l => l.CategoryId == s.Category.Id && l.Name == s.Category.Name);
        Assert.Contains(s.Editor.ScopeAccounts, a => a.Id == s.Account.Id && a.IsIncluded);
        var raw = s.Editor.TotalText;
        f.Localization.SetFormattingCulture("de-DE"); s.Editor.RefreshDisplay(); await OpenAsync(s);
        await s.Editor.ApplyQueryAsync(new Dictionary<string, object>()); await s.Editor.EnsureLoadedAsync();
        Assert.Equal(raw, s.Editor.TotalText); Assert.Equal((1, 1), (s.Editor.MethodIndex, s.Editor.RolloverIndex));
        Assert.False(s.Editor.IsDirty); Assert.Equal(before, await RowsAsync(f)); Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Initial_read_never_creates_settings_and_explicit_failure_keeps_the_form_covered()
    {
        using var f = new FlowFixture(); var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(Ct)) await db.Settings.ExecuteDeleteAsync(Ct);
        var source = new Access { Fail = true }; var store = new ZananceStore(factory, f.Time, source);
        var vm = new BudgetEditorViewModel(store, f.Translator, f.Localization, f.Platform); var before = await RowsAsync(f);
        await vm.EnsureLoadedAsync(); Assert.True(vm.IsCovered); Assert.False(vm.CanSave); Assert.False(vm.IsLoading);
        Assert.NotNull(vm.Error); Assert.Single(f.Platform.Failures); Assert.Equal(before, await RowsAsync(f));
        source.Fail = false; await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsReady); Assert.True(vm.CanSave);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("id")] [InlineData("year")] [InlineData("scope")] [InlineData("currency")]
    public async Task Retained_draft_rejects_rebinding_to_another_financial_identity(string key)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.TotalText = "321.45";
        var before = await RowsAsync(f);
        object value = key switch { "id" => Guid.NewGuid(), "year" => 2027, _ => "foreign" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Editor.ApplyQueryAsync(new Dictionary<string, object> { [key] = value }));
        Assert.Equal("321.45", s.Editor.TotalText); Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Save_preserves_original_explicit_scope_and_hidden_archived_missing_limits(bool advanced)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, advanced); await OpenAsync(s); s.Editor.TotalText = "321.45";
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        var saved = await SavedAsync(s); Assert.Equal(32145, saved.TotalLimit); Assert.Equal(s.Budget.AccountIds, saved.AccountIds);
        // These child rows have no stored order; compare every original category/value pair independently of SQLite enumeration.
        Assert.Equal(s.Budget.CategoryLimits.OrderBy(l => l.CategoryId).Select(l => (l.CategoryId, l.Limit)),
            saved.CategoryLimits.OrderBy(l => l.CategoryId).Select(l => (l.CategoryId, l.Limit)));
        Assert.Equal((s.Budget.Calendar, s.Budget.Method, s.Budget.Rollover), (saved.Calendar, saved.Method, saved.Rollover));
        Assert.Empty(await s.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.False(s.Editor.CanSave); Assert.False(s.Editor.IsDirty);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
    }

    [Fact]
    public async Task Every_invalid_amount_is_reported_together_and_no_row_is_written()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); var before = await RowsAsync(f);
        s.Editor.TotalText = "invalid"; foreach (var row in s.Editor.Limits) row.Text = "-1";
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.NotNull(s.Editor.TotalError);
        Assert.All(s.Editor.Limits, row => Assert.NotNull(row.Error)); Assert.Equal(before, await RowsAsync(f)); Assert.True(s.Editor.CanSave);
    }

    [Theory]
    [InlineData(false, 123456)] [InlineData(true, 98765)]
    public async Task Amounts_keep_their_input_culture_until_the_user_explicitly_retypes(bool retype, long expected)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.TotalText = "1,234.56";
        f.Localization.SetFormattingCulture("de-DE"); s.Editor.RefreshDisplay();
        if (retype) s.Editor.TotalText = "987,65";
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures); Assert.Equal(expected, (await SavedAsync(s)).TotalLimit);
        // Category-limit children have no stored order; retain category/value identity across culture changes.
        Assert.Equal(s.Budget.CategoryLimits.OrderBy(l => l.CategoryId).Select(l => (l.CategoryId, l.Limit)),
            (await SavedAsync(s)).CategoryLimits.OrderBy(l => l.CategoryId).Select(l => (l.CategoryId, l.Limit)));
    }

    [Theory]
    [InlineData("budget")] [InlineData("account")] [InlineData("category")] [InlineData("settings")] [InlineData("date")]
    public async Task Paired_save_rejects_changed_complete_inputs_even_with_unchanged_timestamps(string change)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.TotalText = "321.45";
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        {
            switch (change)
            {
                case "budget": await db.Budgets.ExecuteUpdateAsync(p => p.SetProperty(b => b.TotalLimit, 99999), Ct); break;
                case "account": await db.Accounts.ExecuteUpdateAsync(p => p.SetProperty(a => a.Name, "Changed actual account"), Ct); break;
                case "category": await db.Categories.ExecuteUpdateAsync(p => p.SetProperty(c => c.Name, "Changed actual category"), Ct); break;
                case "settings": await db.Settings.ExecuteUpdateAsync(p => p.SetProperty(c => c.MonthStartDay, 5), Ct); break;
                default: f.Time.Now = f.Time.Now.AddDays(1); break;
            }
        }
        var changed = await RowsAsync(f); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(changed, await RowsAsync(f)); Assert.Single(f.Platform.Failures); Assert.Equal("321.45", s.Editor.TotalText);
        Assert.False(s.Editor.CanSave); Assert.True(s.Editor.CanReload); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Explicit_reload_merges_only_dirty_fields_and_requires_review_of_changed_saved_values()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.TotalText = "321.45";
        var categoryDraft = s.Editor.Limits.Single(l => l.CategoryId == s.Category.Id); categoryDraft.Text = "44.56";
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
            await db.Budgets.ExecuteUpdateAsync(p => p.SetProperty(b => b.TotalLimit, 99999).SetProperty(b => b.AlertsEnabled, false), Ct);
        var changed = await RowsAsync(f); await s.Editor.ReloadCommand.ExecuteAsync(null);
        Assert.Equal("321.45", s.Editor.TotalText); Assert.Equal("44.56", s.Editor.Limits.Single(l => l.CategoryId == s.Category.Id).Text);
        Assert.False(s.Editor.AlertsEnabled); Assert.True(s.Editor.RequiresReview); Assert.False(s.Editor.CanSave);
        Assert.NotNull(s.Editor.ReloadedSavedSummary); Assert.Equal(changed, await RowsAsync(f));
        s.Editor.AcceptReviewCommand.Execute(null); Assert.True(s.Editor.CanSave);
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Equal(32145, (await SavedAsync(s)).TotalLimit);
        Assert.False((await SavedAsync(s)).AlertsEnabled); Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Deleted_original_stays_covered_after_reload_and_cannot_be_recreated_by_the_draft()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.TotalText = "321.45";
        await s.Store.DeleteBudgetAsync(s.Budget.Id, Ct); var before = await RowsAsync(f);
        await s.Editor.ReloadCommand.ExecuteAsync(null); Assert.True(s.Editor.IsCovered); Assert.False(s.Editor.CanSave);
        Assert.Equal("321.45", s.Editor.TotalText); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Committed_save_retires_before_failed_native_return_and_cannot_repeat_the_write()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s);
        f.Platform.PendingNavigation = () => throw new InvalidOperationException("Owned return failure."); s.Editor.TotalText = "321.45";
        await s.Editor.SaveCommand.ExecuteAsync(null); var saved = await RowsAsync(f);
        Assert.Equal(32145, (await SavedAsync(s)).TotalLimit); Assert.False(s.Editor.CanSave); Assert.False(s.Editor.CanReload);
        Assert.False(s.Editor.IsDirty); Assert.Equal(f.Translator["Common_Close"], s.Editor.CloseText);
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Equal(saved, await RowsAsync(f)); Assert.Single(f.Platform.Routes);
        Assert.Single(f.Platform.Failures);
    }

    [Fact]
    public async Task Busy_covers_the_entire_native_return_and_discard_confirmation()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s); s.Editor.TotalText = "321.45";
        var completion = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => completion.Task;
        var close = s.Editor.CancelCommand.ExecuteAsync(null); Assert.True(s.Editor.IsBusy); Assert.False(s.Editor.CanLeave); Assert.False(s.Editor.CanSave);
        completion.SetResult(false); await close; Assert.True(s.Editor.CanSave);
        var navigation = new TaskCompletionSource(); f.Platform.PendingNavigation = () => navigation.Task;
        var save = s.Editor.SaveCommand.ExecuteAsync(null); await Task.Delay(50, Ct);
        Assert.True(s.Editor.IsBusy); Assert.False(s.Editor.CanLeave); Assert.False(s.Editor.CanSave);
        navigation.SetResult(); await save; Assert.False(s.Editor.IsBusy); Assert.True(s.Editor.CanLeave);
    }

    [Fact]
    public async Task Free_retains_paid_original_corrections_without_granting_new_paid_configuration()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Free); await OpenAsync(s);
        Assert.True(s.Editor.CanSave); Assert.NotNull(s.Editor.WorkText); s.Editor.TotalText = "321.45";
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures); Assert.Equal(BudgetMethod.Envelopes, (await SavedAsync(s)).Method);
        var editor = new BudgetEditorViewModel(s.Store, f.Translator, f.Localization, f.Platform);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Budget.Id }); editor.MethodIndex = (int)BudgetMethod.Flex;
        // Both are already-paid methods in the existing footprint, so a retained correction remains allowed.
        Assert.True(editor.CanSave); editor.RolloverIndex = 0; Assert.True(editor.CanSave);
        var basic = new BudgetEditorViewModel(s.Store, f.Translator, f.Localization, f.Platform);
        await basic.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = null!, ["year"] = 2026, ["month"] = 11, ["currency"] = "USD" });
        Assert.True(basic.CanSave); Assert.False(basic.CanChooseMethod); basic.MethodIndex = (int)BudgetMethod.Envelopes; Assert.False(basic.CanSave);
    }

    [Theory]
    [InlineData("save")] [InlineData("replace")] [InlineData("delete")]
    public async Task Every_reviewed_writer_rejects_caller_mutation_and_keeps_all_rows(string action)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var review = await s.Store.GetBudgetEditorReviewAsync(Ct);
        review.Accounts.Single(a => a.Id == s.Account.Id).Name = "Caller mutation"; var before = await RowsAsync(f);
        var copy = review.Choices.Items.Single().ToBudget(); copy.TotalLimit = 99999;
        await Assert.ThrowsAsync<InvalidOperationException>(() => action switch
        { "save" => s.Store.SaveBudgetAsync(copy, Ct, review), "replace" => s.Store.ReplaceBudgetAsync(s.Budget.Id, copy, Ct, review),
            _ => s.Store.DeleteBudgetAsync(s.Budget.Id, Ct, review) });
        Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Future_basic_draft_remains_available_when_todays_definition_capacity_is_full()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, paid: false); Enable(f, s, ProductPlan.Free);
        var editor = new BudgetEditorViewModel(s.Store, f.Translator, f.Localization, f.Platform);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = null!, ["year"] = 2026, ["month"] = 11, ["currency"] = "USD" });
        Assert.True(editor.CanSave); editor.TotalText = "123.45"; await editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Equal(2, (await s.Store.GetBudgetsAsync(Ct)).Count);
    }

    [Fact]
    public async Task Rights_retirement_requires_reload_and_never_uses_the_old_paid_snapshot()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f, paid: false); Enable(f, s, ProductPlan.Plus); await OpenAsync(s);
        s.Editor.MethodIndex = (int)BudgetMethod.Envelopes; s.Editor.TotalText = "321.45"; Assert.True(s.Editor.CanSave);
        Enable(f, s, ProductPlan.Free); var before = await RowsAsync(f); await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.Failures); Assert.Equal(before, await RowsAsync(f)); Assert.False(s.Editor.CanSave);
        await s.Editor.ReloadCommand.ExecuteAsync(null); Assert.Equal((int)BudgetMethod.Envelopes, s.Editor.MethodIndex);
        Assert.True(s.Editor.RequiresReview); s.Editor.AcceptReviewCommand.Execute(null); Assert.False(s.Editor.CanSave);
        s.Editor.MethodIndex = (int)BudgetMethod.Limits; Assert.True(s.Editor.CanSave); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Foreign_file_scope_never_opens_a_restored_identical_budget_id()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var before = await RowsAsync(f);
        await s.Editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Budget.Id, ["scope"] = new string('a', 64) });
        Assert.True(s.Editor.IsCovered); Assert.False(s.Editor.CanSave); Assert.Single(f.Platform.Failures);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData(BudgetPeriod.Month, 2026, 11)]
    [InlineData(BudgetPeriod.Week, 0, 0)]
    [InlineData(BudgetPeriod.TwoWeeks, 0, 0)]
    public async Task New_period_keeps_canonical_stored_coordinates_without_using_the_navigation_month(BudgetPeriod period, int year, int month)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var editor = new BudgetEditorViewModel(s.Store, f.Translator, f.Localization, f.Platform);
        var start = new DateOnly(2026, 11, 9);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = null!, ["year"] = 2026, ["month"] = 11,
            ["period"] = period, ["start"] = period == BudgetPeriod.Month ? default(DateOnly) : start, ["currency"] = "EUR" });
        editor.TotalText = "321.45"; await editor.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Failures);
        var saved = (await s.Store.GetBudgetsAsync(Ct)).Single(b => b.Id != s.Budget.Id);
        Assert.Equal((year, month, period), (saved.Year, saved.Month, saved.Period)); Assert.Equal(32145, saved.TotalLimit);
        Assert.Equal(period == BudgetPeriod.Month ? default : start, saved.PeriodStart);
        Assert.Empty(await s.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
