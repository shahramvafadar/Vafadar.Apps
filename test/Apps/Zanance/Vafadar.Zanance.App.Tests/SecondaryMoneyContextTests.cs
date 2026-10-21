using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Presentation;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-436: original secondary monetary fields must not borrow a later display unit.</summary>
[Trait("AT", "AT-436")]
[Collection("Entry validation units")]
public sealed class SecondaryMoneyContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<GoalDetailViewModel> GoalAsync(FlowFixture f, bool quantity, GoalStore? originalGoals = null)
    {
        f.Localization.SetFormattingCulture("en-US");
        var day = DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime);
        await f.Store.UpdateSettingsAsync(row => row.Mode = ExperienceMode.Advanced, Ct);
        var account = new Account { Name = "Original secondary money", CurrencyCode = "EUR", OpeningDate = day.AddDays(-90), OpeningBalance = 1000000 };
        await f.Store.SaveAccountAsync(account, Ct);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original assumed item", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", Divisible = true };
        await holdings.SaveTypeAsync(type, Ct);
        var goals = originalGoals ?? f.Services.GetRequiredService<GoalStore>();
        var goal = new Goal { Name = "Original secondary target", CurrencyCode = "EUR", Type = quantity ? GoalType.HoldingQuantity : GoalType.Earmark,
            AssetTypeId = quantity ? type.Id : null, TargetAmount = 10000000, TargetDate = day.AddMonths(3) };
        await goals.SaveGoalWithContributionPlanAsync(goal, new ContributionPlan { GoalId = goal.Id, Amount = 1000,
            AssumedPricePerUnitMilli = quantity ? 1000000 : null, Rule = new() { Start = day, Frequency = Frequency.Monthly } }, Ct);
        var dates = new DateFormatter(f.Localization);
        var presenter = new GoalPresenter(f.Translator, dates, f.Localization, holdings, new HoldingText(f.Translator, f.Localization, dates));
        var vm = new GoalDetailViewModel(f.Store, goals, f.Services.GetRequiredService<PlanStore>(), holdings, presenter,
            f.Translator, dates, f.Localization, f.Time, f.Platform);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = goal.Id }); await vm.LoadAsync();
        Assert.True(vm.CanInteract); Assert.False(vm.NotFound);
        if (!quantity) vm.SelectedAccount = vm.Accounts.Single(row => row.Id == account.Id);
        return vm;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Goal_contribution_and_assumption_refuse_a_later_display_unit(bool assumed)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await GoalAsync(f, assumed);
            if (assumed) { vm.AssumedPriceText = "12.34"; Assert.True(vm.CanSaveAssumedPrice); }
            else { vm.AmountText = "12.34"; Assert.True(vm.CanAllocate); }
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Theory]
    [InlineData("not money")]
    [InlineData("-1")]
    [InlineData("9223372036854775808")]
    public async Task Nonblank_invalid_goal_assumption_does_not_clear_the_original_saved_price(string raw)
    {
        using var f = new FlowFixture(); var vm = await GoalAsync(f, true);
        vm.AssumedPriceText = raw; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveAssumedPriceCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(raw, vm.AssumedPriceText); Assert.NotNull(vm.PriceError);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Unit_change_partial_edit_and_reload_keep_the_original_goal_draft_until_complete_reentry(bool assumed)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await GoalAsync(f, assumed);
            if (assumed) vm.AssumedPriceText = "12.34"; else vm.AmountText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            if (assumed) vm.AssumedPriceText = "12.35"; else vm.AmountText = "12.35";
            await vm.LoadAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
            Assert.False(assumed ? vm.CanSaveAssumedPrice : vm.CanAllocate);
            Assert.NotNull(assumed ? vm.PriceError : vm.AmountError);
            Assert.Equal("12.35", assumed ? vm.AssumedPriceText : vm.AmountText);
            await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            if (assumed) { vm.AssumedPriceText = ""; vm.AssumedPriceText = "12.35"; }
            else { vm.AmountText = ""; vm.AmountText = "12.35"; }
            Assert.True(assumed ? vm.CanSaveAssumedPrice : vm.CanAllocate);
            await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
            var goals = f.Services.GetRequiredService<GoalStore>();
            if (assumed) Assert.Equal(123500000, (await goals.GetContributionPlansAsync(Ct)).Single().AssumedPricePerUnitMilli);
            else Assert.Equal(123500, (await goals.GetAllocationsAsync(cancellationToken: Ct)).Single().Amount);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Display_only_format_reload_retains_the_original_goal_parser(bool assumed)
    {
        using var f = new FlowFixture(); var vm = await GoalAsync(f, assumed);
        f.Localization.SetFormattingCulture("de-DE");
        if (assumed) vm.AssumedPriceText = "12,34"; else vm.AmountText = "12,34";
        f.Localization.SetFormattingCulture("en-US"); await ((IThemeAware)vm).RefreshThemeAsync();
        Assert.NotNull(assumed ? vm.PriceFormatNote : vm.AmountFormatNote);
        await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
        var goals = f.Services.GetRequiredService<GoalStore>();
        if (assumed) Assert.Equal(1234000, (await goals.GetContributionPlansAsync(Ct)).Single().AssumedPricePerUnitMilli);
        else Assert.Equal(1234, (await goals.GetAllocationsAsync(cancellationToken: Ct)).Single().Amount);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Only_an_explicit_blank_assumption_removes_the_saved_price()
    {
        using var f = new FlowFixture(); var vm = await GoalAsync(f, true);
        vm.AssumedPriceText = ""; await vm.SaveAssumedPriceCommand.ExecuteAsync(null);
        Assert.Null((await f.Services.GetRequiredService<GoalStore>().GetContributionPlansAsync(Ct)).Single().AssumedPricePerUnitMilli);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Goal_notification_failure_keeps_known_acceptance_and_cannot_resubmit_the_draft(bool assumed)
    {
        using var f = new FlowFixture(); var vm = await GoalAsync(f, assumed);
        if (assumed) vm.AssumedPriceText = "12.34"; else vm.AmountText = "12.34";
        f.Services.GetRequiredService<GoalStore>().Changed += (_, _) => throw new InvalidOperationException("Original observer failure");
        await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
        Assert.True(vm.NeedsReload); Assert.False(vm.CanInteract); Assert.NotNull(vm.SaveError);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        if (assumed) Assert.Equal(1234000, (await f.Services.GetRequiredService<GoalStore>().GetContributionPlansAsync(Ct)).Single().AssumedPricePerUnitMilli);
        else Assert.Equal(1234, (await f.Services.GetRequiredService<GoalStore>().GetAllocationsAsync(cancellationToken: Ct)).Single().Amount);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Original_goal_writer_rolls_back_if_the_reviewed_draft_retires_after_the_save(bool assumed)
    {
        using var f = new FlowFixture(); _ = await GoalAsync(f, assumed);
        var goals = f.Services.GetRequiredService<GoalStore>(); var review = await goals.GetWorkSnapshotAsync(Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var checks = 0;
        bool Current() => ++checks == 1;
        if (assumed)
        {
            var original = review.ContributionPlans.Single();
            var plan = new ContributionPlan { GoalId = original.GoalId, Method = original.Method, Amount = original.Amount,
                Percent = original.Percent, Rule = original.Rule.Clone(), CategoryIds = [.. original.CategoryIds],
                ReminderEnabled = original.ReminderEnabled, AssumedPricePerUnitMilli = 1234000 };
            await Assert.ThrowsAsync<InvalidOperationException>(() => goals.SaveContributionPlanAsync(plan.GoalId, plan, Ct, review, isCurrent: Current));
        }
        else
        {
            var allocation = new GoalAllocation { GoalId = review.Goals.Single().Id,
                AccountId = (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().Id, Amount = 1234, Date = DateOnly.FromDateTime(f.Time.Now.DateTime) };
            await Assert.ThrowsAsync<InvalidOperationException>(() => goals.AddAllocationAsync(allocation, Ct, review, isCurrent: Current));
        }
        Assert.Equal(2, checks); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    private sealed class OriginalWriterAccess : ICommercialWriteAccessSource
    {
        internal Action? Entered;
        public CommercialWriteAccess Capture(string databasePath)
        {
            var entered = Entered; Entered = null; entered?.Invoke();
            return CommercialWriteAccess.Inactive;
        }
    }

    [Fact]
    public async Task Goal_allocation_notification_failure_readback_does_not_restore_the_accepted_amount()
    {
        using var f = new FlowFixture(); var vm = await GoalAsync(f, false); vm.AmountText = "12.34";
        f.Services.GetRequiredService<GoalStore>().Changed += (_, _) => throw new InvalidOperationException("Original observer failure");
        await vm.AllocateCommand.ExecuteAsync(null); Assert.True(vm.NeedsReload);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.LoadAsync(); Assert.False(vm.NeedsReload); Assert.True(vm.CanInteract); Assert.Empty(vm.AmountText);
        await vm.AllocateCommand.ExecuteAsync(null);
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(1234, (await f.Services.GetRequiredService<GoalStore>().GetAllocationsAsync(cancellationToken: Ct)).Single().Amount);
    }

    [Theory, InlineData(false, false), InlineData(true, false), InlineData(false, true), InlineData(true, true)]
    public async Task Original_goal_form_refuses_edit_return_or_unit_changes_at_the_writer(bool assumed, bool unitChanged)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var access = new OriginalWriterAccess();
            var goals = new GoalStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
            var vm = await GoalAsync(f, assumed, goals);
            if (assumed) vm.AssumedPriceText = "12.34"; else vm.AmountText = "12.34";
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            access.Entered = () =>
            {
                if (unitChanged) DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
                else if (assumed) { vm.AssumedPriceText = "99"; vm.AssumedPriceText = "12.34"; }
                else { vm.AmountText = "99"; vm.AmountText = "12.34"; }
            };
            await (assumed ? vm.SaveAssumedPriceCommand : vm.AllocateCommand).ExecuteAsync(null);
            Assert.Null(access.Entered); Assert.IsType<InvalidOperationException>(Assert.Single(f.Platform.Failures));
            Assert.Equal("12.34", assumed ? vm.AssumedPriceText : vm.AmountText);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        }
        finally { DisplayUnits.Set(units); }
    }

    private static async Task<SettlementViewModel> SettlementAsync(FlowFixture f)
    {
        var day = DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime);
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var account = new Account { Name = "Original bill money", CurrencyCode = "EUR", OpeningBalance = 1000000, OpeningDate = day.AddDays(-90) };
        await f.Store.SaveAccountAsync(account, Ct);
        var category = (await f.Store.GetCategoriesAsync(cancellationToken: Ct)).Single(row => row.SystemKey == "Food");
        var plan = new Schedule { Name = "Original bill advances", Kind = EntryKind.Expense, AccountId = account.Id,
            CategoryId = category.Id, Amount = 1000, Rule = new() { Start = day.AddDays(-7) } };
        await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct);
        Assert.True((await f.Store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id,
            CategoryId = category.Id, Date = day.AddDays(-7), ScheduleId = plan.Id, Amount = 1000, Title = "Original bill advance" }, Ct)).Succeeded);
        var vm = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        await vm.LoadAsync(plan.Id); return vm;
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Original_bill_keeps_its_parser_or_requires_complete_unit_reentry(bool changedUnit)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture();
            f.Localization.SetFormattingCulture("de-DE"); var vm = await SettlementAsync(f);
            vm.ActualText = "14,50"; Assert.True(vm.CanSave);
            f.Localization.SetFormattingCulture("en-US");
            if (changedUnit) DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            await ((IThemeAware)vm).RefreshThemeAsync();
            Assert.NotNull(vm.InputFormatNote);
            if (changedUnit)
            {
                vm.ActualText = "14.51";
                Assert.False(vm.CanSave); Assert.NotNull(vm.AmountError);
                var before = await GoalChooserFlowTests.SnapshotAsync(f);
                await vm.SaveCommand.ExecuteAsync(null);
                Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
                vm.ActualText = ""; vm.ActualText = "14.50";
            }
            Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
            Assert.True(vm.HasCommitted);
            var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
            Assert.Equal(2, entries.Count); Assert.Equal(changedUnit ? 145000 : 1450, entries.Sum(row => row.Amount));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Original_settlement_bill_refuses_a_later_display_unit()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var vm = await SettlementAsync(f); vm.ActualText = "14"; Assert.True(vm.CanSave);
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(vm.HasCommitted);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Changed_unit_bill_stays_field_local_without_reaching_the_unexpected_failure_port()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var vm = await SettlementAsync(f); var day = DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime);
            vm.From = day.AddDays(-7); vm.To = day; vm.ActualText = "12.34"; Assert.True(vm.CanSave);
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); await ((IThemeAware)vm).RefreshThemeAsync();
            Assert.False(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
            Assert.NotNull(vm.AmountError); Assert.Empty(f.Platform.Failures); Assert.False(vm.HasCommitted);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        }
        finally { DisplayUnits.Set(units); }
    }
}
