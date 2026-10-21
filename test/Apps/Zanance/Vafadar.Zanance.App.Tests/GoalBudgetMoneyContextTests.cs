using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-431: original monetary meaning in actual goal and budget sources with isolated SQLite.</summary>
[Trait("AT", "AT-431")]
[Collection("Entry validation units")]
public sealed class GoalBudgetMoneyContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("No native permission belongs to monetary review.");
    }
    private static async Task SeedAsync(FlowFixture f)
    {
        await f.Store.SaveAccountAsync(new Account { Name = "Original money account", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) }, Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced;
        await f.Store.SaveSettingsAsync(settings, Ct);
        f.Localization.SetFormattingCulture("en-US");
    }
    private static GoalEditorViewModel GoalEditor(FlowFixture f)
    {
        var dates = new DateFormatter(f.Localization);
        return new(f.Store, f.Services.GetRequiredService<GoalStore>(), f.Translator, f.Localization, dates, f.Time,
            f.Services.GetRequiredService<HoldingStore>(), new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
    }
    private static async Task<BudgetEditorViewModel> BudgetEditorAsync(FlowFixture f)
    {
        var vm = new BudgetEditorViewModel(f.Store, f.Translator, f.Localization, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["year"] = 2026, ["month"] = 10, ["currency"] = "EUR" });
        Assert.True(vm.CanSave); return vm;
    }

    /// <summary>A changed display unit cannot multiply an existing goal target silently.</summary>
    [Fact]
    public async Task Goal_target_requires_reentry_after_display_unit_change()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.TypeIndex = 1; vm.Name = "Original target"; vm.AmountText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.RefreshDisplay(); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Empty(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
            Assert.Equal("12.34", vm.AmountText); Assert.False(vm.CanSave); Assert.NotNull(vm.AmountError);
        }
        finally { DisplayUnits.Set(original); }
    }

    /// <summary>Explicitly renewing the target must not also renew an untouched contribution's original unit.</summary>
    [Fact]
    public async Task Goal_contribution_keeps_its_independent_original_unit()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.TypeIndex = 1; vm.Name = "Original contribution"; vm.AmountText = "100"; vm.ContributionText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.AmountText = string.Empty; vm.AmountText = "1";
            vm.RefreshDisplay(); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Empty(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
            Assert.Equal("12.34", vm.ContributionText); Assert.False(vm.CanSave); Assert.NotNull(vm.ContributionError);
        }
        finally { DisplayUnits.Set(original); }
    }

    /// <summary>A changed display unit cannot silently reinterpret a total budget limit.</summary>
    [Fact]
    public async Task Budget_total_requires_reentry_after_display_unit_change()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f); var vm = await BudgetEditorAsync(f);
            vm.TotalText = "12.34"; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            vm.RefreshDisplay(); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Empty(await f.Store.GetBudgetsAsync(Ct)); Assert.Equal("12.34", vm.TotalText);
            Assert.False(vm.CanSave); Assert.NotNull(vm.TotalError);
        }
        finally { DisplayUnits.Set(original); }
    }

    /// <summary>Each original category amount has a separate context from an explicitly renewed total.</summary>
    [Fact]
    public async Task Budget_category_keeps_its_independent_original_unit()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var category = new Category { Name = "Original money category", Kind = CategoryKind.Expense };
            await f.Store.SaveCategoryAsync(category, Ct); var vm = await BudgetEditorAsync(f);
            vm.TotalText = "100"; var row = vm.Limits.Single(item => item.CategoryId == category.Id); row.Text = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.TotalText = string.Empty; vm.TotalText = "1";
            vm.RefreshDisplay(); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Empty(await f.Store.GetBudgetsAsync(Ct)); Assert.Equal("12.34", row.Text);
            Assert.False(vm.CanSave); Assert.NotNull(row.Error);
        }
        finally { DisplayUnits.Set(original); }
    }

    /// <summary>Partial edits cannot acknowledge a replacement unit; complete independent reentry saves exact minor amounts.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Partial_edits_refuse_but_complete_reentry_saves_original_minor_amounts(bool budget)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            if (budget)
            {
                var vm = await BudgetEditorAsync(f); vm.TotalText = "12.34";
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.TotalText = "12.345"; vm.RefreshDisplay();
                Assert.False(vm.CanSave); Assert.Equal("EUR", vm.TotalUnitName);
                vm.TotalText = string.Empty; vm.TotalText = "0.1234"; await vm.SaveCommand.ExecuteAsync(null);
                Assert.Equal(1234, Assert.Single(await f.Store.GetBudgetsAsync(Ct)).TotalLimit);
            }
            else
            {
                var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
                vm.TypeIndex = 1; vm.Name = "Explicit money"; vm.AmountText = "12.34";
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.AmountText = "12.345"; vm.RefreshDisplay();
                Assert.False(vm.CanSave); Assert.Equal("EUR", vm.TargetUnitName);
                vm.AmountText = string.Empty; vm.AmountText = "0.1234"; await vm.SaveCommand.ExecuteAsync(null);
                Assert.Equal(1234, Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct)).TargetAmount);
            }
            Assert.Empty(f.Platform.Failures); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>The original writer rolls back real SQLite changes on unit, edit revision or native-owner retirement.</summary>
    [Theory]
    [InlineData(false, "unit")] [InlineData(true, "unit")]
    [InlineData(false, "revision")] [InlineData(true, "revision")]
    [InlineData(false, "owner")] [InlineData(true, "owner")]
    public async Task Writer_rechecks_original_monetary_owner(bool budget, string mutation)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var goal = GoalEditor(f); await goal.ApplyQueryAsync(new Dictionary<string, object>());
            goal.TypeIndex = 1; goal.Name = "Original pending writer"; goal.AmountText = "12.34";
            var limit = await BudgetEditorAsync(f); limit.TotalText = "12.34"; var reached = false;
            f.Time.BeforeRead = () =>
            {
                if (!new System.Diagnostics.StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(Vafadar.Data.Auditing.AuditingSaveChangesInterceptor))) return;
                reached = true; f.Time.BeforeRead = null;
                if (mutation == "unit") DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                else if (mutation == "owner") { if (budget) limit.SetViewActive(false); else goal.SetViewActive(false); }
                else if (budget) { limit.TotalText = "99"; limit.TotalText = "12.34"; }
                else { goal.AmountText = "99"; goal.AmountText = "12.34"; }
            };
            if (budget) await limit.SaveCommand.ExecuteAsync(null); else await goal.SaveCommand.ExecuteAsync(null);
            Assert.True(reached); Assert.Empty(await f.Store.GetBudgetsAsync(Ct));
            Assert.Empty(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct)); Assert.Single(f.Platform.Failures);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Known acceptance remains Close-only when actual store notification fails after commit.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Accepted_money_is_not_rearmed_by_failed_notification(bool budget)
    {
        using var f = new FlowFixture(); await SeedAsync(f);
        var goal = GoalEditor(f); await goal.ApplyQueryAsync(new Dictionary<string, object>());
        goal.TypeIndex = 1; goal.Name = "Known accepted target"; goal.AmountText = "12.34";
        var limit = await BudgetEditorAsync(f); limit.TotalText = "12.34";
        EventHandler failure = (_, _) => throw new IOException("Owned post-commit notification failure.");
        if (budget) f.Store.Changed += failure; else f.Services.GetRequiredService<GoalStore>().Changed += failure;
        try
        {
            if (budget)
            {
                await limit.SaveCommand.ExecuteAsync(null); Assert.False(limit.CanSave); Assert.False(limit.IsDirty);
                await limit.SaveCommand.ExecuteAsync(null); Assert.Equal(1234, Assert.Single(await f.Store.GetBudgetsAsync(Ct)).TotalLimit);
            }
            else
            {
                await goal.SaveCommand.ExecuteAsync(null); Assert.False(goal.CanSave); Assert.False(goal.IsDirty);
                await goal.SaveCommand.ExecuteAsync(null); Assert.Equal(1234, Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct)).TargetAmount);
            }
            Assert.Single(f.Platform.Failures);
        }
        finally { if (budget) f.Store.Changed -= failure; else f.Services.GetRequiredService<GoalStore>().Changed -= failure; }
    }

    /// <summary>A field-feedback observer cannot lend the already pending Save a newer raw monetary draft.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Validation_observer_requires_a_new_explicit_save(bool budget)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var changed = false;
        if (budget)
        {
            var vm = await BudgetEditorAsync(f); vm.TotalText = "bad"; await vm.SaveCommand.ExecuteAsync(null);
            vm.PropertyChanged += (_, args) => { if (!changed && args.PropertyName == nameof(vm.TotalError) && vm.TotalError is null)
                { changed = true; vm.TotalText = "99.00"; } };
            await vm.SaveCommand.ExecuteAsync(null); Assert.True(changed); Assert.Empty(await f.Store.GetBudgetsAsync(Ct));
        }
        else
        {
            var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.TypeIndex = 1; vm.AmountText = "12.34";
            await vm.SaveCommand.ExecuteAsync(null); vm.Name = "Actual validation observer";
            vm.PropertyChanged += (_, args) => { if (!changed && args.PropertyName == nameof(vm.NameError) && vm.NameError is null)
                { changed = true; vm.AmountText = "99.00"; } };
            await vm.SaveCommand.ExecuteAsync(null); Assert.True(changed); Assert.Empty(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
        }
    }

    /// <summary>Reentering identical digits in a new unit remains a semantic edit across read-only reload.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Identical_digits_with_explicit_new_unit_survive_reload(bool budget)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            if (budget)
            {
                await f.Store.SaveBudgetAsync(new Vafadar.Zanance.Core.Budgets.Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 1234 }, Ct);
                var vm = await BudgetEditorAsync(f); var raw = vm.TotalText; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                vm.TotalText = string.Empty; vm.TotalText = raw; Assert.True(vm.IsDirty);
                await vm.ReloadCommand.ExecuteAsync(null); Assert.Equal(raw, vm.TotalText); Assert.Equal("Hundred euros", vm.TotalUnitName);
                await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(123400, Assert.Single(await f.Store.GetBudgetsAsync(Ct)).TotalLimit);
            }
            else
            {
                var store = f.Services.GetRequiredService<GoalStore>(); var original = new Vafadar.Zanance.Core.Goals.Goal
                    { Name = "Retained original", Type = Vafadar.Zanance.Core.Goals.GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = 1234 };
                await store.SaveGoalWithContributionPlanAsync(original, null, Ct);
                var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
                var raw = vm.AmountText; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                vm.AmountText = string.Empty; vm.AmountText = raw; Assert.True(vm.IsDirty);
                await vm.ReloadCommand.ExecuteAsync(null); Assert.Equal(raw, vm.AmountText); Assert.Equal("Hundred euros", vm.TargetUnitName);
                await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(123400, Assert.Single(await store.GetGoalsAsync(Ct)).TargetAmount);
            }
            Assert.Empty(f.Platform.Failures);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Native cut confirmations retain their original monetary input and known acceptance before notification.</summary>
    [Theory]
    [InlineData("unit")] [InlineData("revision")] [InlineData("owner")] [InlineData("notification")]
    public async Task Spending_cut_keeps_original_input_and_acceptance(string change)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var category = new Category { Name = "Original cut category", Kind = CategoryKind.Expense }; await f.Store.SaveCategoryAsync(category, Ct);
            await f.Store.SaveBudgetAsync(new Vafadar.Zanance.Core.Budgets.Budget { Year = 2026, Month = 10, CurrencyCode = "EUR",
                CategoryLimits = [new() { CategoryId = category.Id, Limit = 5000 }] }, Ct);
            var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.TypeIndex = 1; vm.Name = "Original cut"; vm.AmountText = "100"; vm.MethodIndex = 2;
            vm.ContributionText = "12.34"; vm.CutCategory = vm.CutCategories.Single(item => item.Id == category.Id);
            Assert.True(vm.CanApplyCut);
            f.Platform.PendingConfirmation = () =>
            {
                if (change == "unit") DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                if (change == "revision") { vm.ContributionText = "99"; vm.ContributionText = "12.34"; }
                if (change == "owner") vm.SetViewActive(false);
                return Task.FromResult(true);
            };
            EventHandler failure = (_, _) => throw new IOException("Owned cut acceptance observer failed.");
            if (change == "notification") f.Store.Changed += failure;
            try { await vm.ApplyCutCommand.ExecuteAsync(null); }
            finally { f.Store.Changed -= failure; }
            Assert.Equal(change == "notification" ? 3766 : 5000, Assert.Single((await f.Store.GetBudgetsAsync(Ct)).Single().CategoryLimits).Limit);
            Assert.Single(f.Platform.Failures); Assert.Empty(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
            if (change == "notification")
            {
                await vm.ReloadCommand.ExecuteAsync(null); Assert.False(vm.CanApplyCut);
                await vm.ApplyCutCommand.ExecuteAsync(null); Assert.Equal(3766, Assert.Single((await f.Store.GetBudgetsAsync(Ct)).Single().CategoryLimits).Limit);
            }
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>A hidden share contribution never requires reentry or posts an old fixed monetary amount.</summary>
    [Fact]
    public async Task Hidden_fixed_amount_does_not_block_or_leak_into_share_plan()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f); var vm = GoalEditor(f);
            await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.TypeIndex = 1; vm.Name = "Original share";
            vm.AmountText = "100"; vm.ContributionText = "12.34"; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            vm.AmountText = string.Empty; vm.AmountText = "1"; vm.MethodIndex = 1; vm.PercentText = "10"; vm.RefreshDisplay();
            Assert.True(vm.CanSave); Assert.Null(vm.ContributionError); Assert.Null(vm.ContributionUnitName);
            await vm.SaveCommand.ExecuteAsync(null); var plan = Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetContributionPlansAsync(Ct));
            Assert.Null(plan.Amount); Assert.Equal(10m, plan.Percent); Assert.Empty(f.Platform.Failures);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Explicit reload renews untouched saved hidden limits without silently rewriting their original minor values.</summary>
    [Fact]
    public async Task Hidden_saved_limits_offer_visible_review_then_reload_without_money_conversion()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var category = new Category { Name = "Retained hidden limit", Kind = CategoryKind.Expense }; await f.Store.SaveCategoryAsync(category, Ct);
            await f.Store.SaveBudgetAsync(new Vafadar.Zanance.Core.Budgets.Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 10000,
                CategoryLimits = [new() { CategoryId = category.Id, Limit = 1234 }] }, Ct);
            var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Simple; await f.Store.SaveSettingsAsync(settings, Ct);
            var vm = await BudgetEditorAsync(f); Assert.False(vm.ShowCategoryLimits);
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.RefreshDisplay(); Assert.True(vm.ShowCategoryLimits); Assert.NotNull(vm.MoneyReviewText);
            await vm.ReloadCommand.ExecuteAsync(null); Assert.False(vm.ShowCategoryLimits); Assert.Null(vm.MoneyReviewText);
            Assert.Equal(1234, Assert.Single(Assert.Single(await f.Store.GetBudgetsAsync(Ct)).CategoryLimits).Limit); Assert.False(vm.IsDirty);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Physical goal quantities retain their original gram semantics regardless of price-currency display units.</summary>
    [Fact]
    public async Task Quantity_goal_is_independent_of_monetary_display_unit()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await SeedAsync(f);
            var asset = new Vafadar.Zanance.Core.Holdings.AssetType { Name = "Original grams", Dimension = Vafadar.Zanance.Core.Holdings.AssetDimension.Mass,
                PriceCurrencyCode = "EUR", Divisible = true };
            Assert.True(await f.Services.GetRequiredService<HoldingStore>().SaveTypeAsync(asset, Ct));
            var vm = GoalEditor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.TypeIndex = 2;
            vm.Name = "Original quantity"; vm.AmountText = "12.34"; vm.ContributionText = "1.23";
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.RefreshDisplay(); Assert.True(vm.CanSave); Assert.Null(vm.TargetUnitName);
            await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(12340, Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct)).TargetAmount);
            Assert.Equal(1230, Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetContributionPlansAsync(Ct)).Amount); Assert.Empty(f.Platform.Failures);
        }
        finally { DisplayUnits.Set(units); }
    }
}
