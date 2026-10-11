using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-161: actual reminder batches retire when original rule/state or financial inputs change.</summary>
[Trait("AT", "AT-161")]
public sealed class ReminderInputFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task AddContributionAsync(ProfileReminderFlowTests.Fixture f) => f.Data.Goals.SaveGoalWithContributionPlanAsync(f.Data.Goal,
        new ContributionPlan { GoalId = f.Data.Goal.Id, Amount = 1000, ReminderEnabled = true,
            Rule = new() { Start = new(2026, 10, 10), Frequency = Frequency.Once } }, Ct);

    [Theory]
    [InlineData("name"), InlineData("amount"), InlineData("rule"), InlineData("paused"), InlineData("removed"), InlineData("skipped")]
    public async Task Original_plan_packets_changed_at_unchanged_audit_times_retire_the_native_batch_and_preserve_the_exact_changed_rows(string change)
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync(); f.Data.Enable();
        string? changedRows = null;
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            await using (var db = await f.Data.Factory.CreateDbContextAsync(Ct))
            {
                var sql = change switch
                {
                    "name" => "UPDATE Schedules SET Name='Fictitious reviewed new name'",
                    "amount" => "UPDATE Schedules SET Amount=6789",
                    "rule" => "UPDATE Schedules SET Start='2026-10-11'",
                    "paused" => "UPDATE Schedules SET State=1,PausedFrom='2026-10-10'",
                    "removed" => "DELETE FROM Schedules",
                    _ => null,
                };
                if (sql is not null) await db.Database.ExecuteSqlRawAsync(sql, Ct);
                else { db.OccurrenceStates.Add(new() { ScheduleId = f.Data.Plan.Id, OriginalDate = new(2026, 10, 10), Status = OccurrenceStatus.Skipped }); await db.SaveChangesAsync(Ct); }
            }
            changedRows = await f.RowsAsync();
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending); Assert.Equal(changedRows, await f.RowsAsync());
        await f.Service.RefreshAsync();
        if (change is "paused" or "removed" or "skipped") Assert.Empty(f.Native.Pending);
        else Assert.Single(f.Native.Pending);
        Assert.Equal(changedRows, await f.RowsAsync());
    }

    [Theory, InlineData("balance"), InlineData("currency"), InlineData("name"), InlineData("entry"), InlineData("refund")]
    public async Task Original_financial_inputs_changed_inside_publication_require_a_fresh_complete_goal_calculation(string change)
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync(); await AddContributionAsync(f); f.Data.Enable(ProductPlan.Plus);
        string? changedRows = null;
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            if (change == "refund")
                Assert.True((await f.Data.Store.SaveEntryAsync(new LedgerEntry { AccountId = f.Data.Account.Id, Kind = EntryKind.Refund,
                    Amount = 1000, RefundOfId = f.Data.Entry.Id, Date = new(2026, 10, 10) }, Ct)).Succeeded);
            else
            {
                await using var db = await f.Data.Factory.CreateDbContextAsync(Ct);
                await db.Database.ExecuteSqlRawAsync(change switch
                {
                    "balance" => "UPDATE Accounts SET OpeningBalance=20000",
                    "currency" => "UPDATE Accounts SET CurrencyCode='USD'",
                    "name" => "UPDATE Accounts SET Name='Fictitious current original name'",
                    _ => "UPDATE Entries SET Amount=100",
                }, Ct);
            }
            changedRows = await f.RowsAsync();
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending); Assert.Equal(changedRows, await f.RowsAsync());
    }

    [Theory, InlineData("plan"), InlineData("goal")]
    public async Task Raw_choice_value_changed_without_a_new_revision_cannot_keep_the_old_native_work(string kind)
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync();
        if (kind == "goal") await AddContributionAsync(f);
        f.Data.Enable();
        var choices = f.Data.Services.GetRequiredService<ResourceChoiceStore>();
        var quota = kind == "plan" ? QuotaKind.RecurringPlans : QuotaKind.Goals;
        await choices.SaveAsync(await choices.ReadAsync(quota, Ct), [kind == "plan" ? f.Data.Plan.Id : f.Data.Goal.Id], Ct);
        if (kind == "goal") f.Data.Enable(ProductPlan.Plus);
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null; await using var db = await f.Data.Factory.CreateDbContextAsync(Ct);
            await db.Database.ExecuteSqlRawAsync("UPDATE ResourceSelections SET IdentitySet='';", Ct);
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending);
    }

    [Theory, InlineData("plan"), InlineData("goal"), InlineData("financial")]
    public async Task Modified_in_memory_original_packets_are_rejected_without_writing_any_original_rows(string packet)
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync(); await AddContributionAsync(f); f.Data.Enable(ProductPlan.Plus);
        var plans = await f.Data.Plans.GetWorkSnapshotAsync(Ct); var goals = await f.Data.Goals.GetWorkSnapshotAsync(Ct);
        var financials = await f.Data.Store.GetReminderFinancialSnapshotAsync(plans, goals, new(2026, 10, 10), Ct);
        var settings = await f.Data.Store.GetSettingsAsync(Ct); var before = await f.RowsAsync();
        if (packet == "plan") plans.Schedules[0].Name = "Fictitious changed local packet";
        if (packet == "goal") goals.Goals[0].TargetAmount++;
        if (packet == "financial") financials.Entries[0].Amount++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Data.Store.ValidateReminderPreparationAsync(financials, plans, goals,
            JsonSerializer.Serialize(settings), financials.Accounts.Min(a => a.OpeningDate), Ct));
        Assert.Equal(before, await f.RowsAsync());
    }

    [Fact]
    public async Task Another_actual_file_cannot_validate_an_original_financial_reminder_packet()
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await using var other = new ProfileReminderFlowTests.Fixture();
        await f.SeedAsync(); await other.SeedAsync();
        var plans = await f.Data.Plans.GetWorkSnapshotAsync(Ct); var goals = await f.Data.Goals.GetWorkSnapshotAsync(Ct);
        var financials = await f.Data.Store.GetReminderFinancialSnapshotAsync(plans, goals, new(2026, 10, 10), Ct);
        var before = await other.RowsAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Data.Store.ValidateReminderPreparationAsync(financials, plans, goals,
            "null", financials.Accounts.Min(a => a.OpeningDate), Ct));
        Assert.Equal(before, await other.RowsAsync());
    }

    [Fact]
    public async Task Future_income_outside_the_original_calculation_date_does_not_rewrite_today_progress_or_block_its_reminder()
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync(); await AddContributionAsync(f); f.Data.Enable(ProductPlan.Plus);
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            Assert.True((await f.Data.Store.SaveEntryAsync(new LedgerEntry { AccountId = f.Data.Account.Id, Kind = EntryKind.Income,
                Amount = 100000, Date = new(2026, 10, 11) }, Ct)).Succeeded);
        };
        await f.Service.RefreshAsync(); Assert.Single(f.Native.Pending, n => n.Link.StartsWith("goal-reminder-v1", StringComparison.Ordinal));
    }

    [Theory, InlineData("quantity"), InlineData("date"), InlineData("removed")]
    public async Task Original_quantity_movements_changed_during_publication_retire_the_old_goal_without_ledger_posts(string change)
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync();
        var type = new AssetType { Name = "Fictitious original quantity", Dimension = AssetDimension.Count, Divisible = true, PriceCurrencyCode = "EUR" };
        var location = new AssetLocation { Name = "Fictitious original location" };
        await using (var db = await f.Data.Factory.CreateDbContextAsync(Ct))
        {
            db.AssetTypes.Add(type); db.AssetLocations.Add(location);
            db.AssetEvents.Add(new() { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening,
                Date = new(2026, 10, 10), Quantity = 1000 }); await db.SaveChangesAsync(Ct);
        }
        f.Data.Goal.Type = GoalType.HoldingQuantity; f.Data.Goal.AccountId = null; f.Data.Goal.AssetTypeId = type.Id; f.Data.Goal.TargetAmount = 5000;
        await AddContributionAsync(f); f.Data.Enable(ProductPlan.Plus); string? changedRows = null;
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null; await using (var db = await f.Data.Factory.CreateDbContextAsync(Ct))
                await db.Database.ExecuteSqlRawAsync(change == "quantity" ? "UPDATE AssetEvents SET Quantity=10000" : change == "date"
                    ? "UPDATE AssetEvents SET Date='2026-10-11'" : "DELETE FROM AssetEvents", Ct);
            changedRows = await f.RowsAsync();
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending); Assert.Equal(changedRows, await f.RowsAsync());
    }

    [Fact]
    public async Task Coherent_deferred_financial_read_preserves_the_real_calculator_balance_while_paired_writers_commit()
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync(); await AddContributionAsync(f); f.Data.Enable(ProductPlan.Plus);
        var plans = await f.Data.Plans.GetWorkSnapshotAsync(Ct); var goals = await f.Data.Goals.GetWorkSnapshotAsync(Ct);
        var writer = Task.Run(async () =>
        {
            for (var i = 1; i <= 20; i++)
            {
                await using var db = await f.Data.Factory.CreateDbContextAsync(Ct); await using var held = await db.Database.BeginTransactionAsync(Ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Accounts SET OpeningBalance={5000L + i}", Ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Entries SET Amount={1234L + i}", Ct);
                await held.CommitAsync(Ct);
            }
        }, Ct);
        try
        {
            for (var i = 0; i < 6; i++)
            {
                var original = await f.Data.Store.GetReminderFinancialSnapshotAsync(plans, goals, new(2026, 10, 10), Ct);
                var progress = GoalProgressService.Evaluate(goals.Goals, goals.Allocations, original.Accounts, original.Entries,
                    goals.ContributionPlans, original.Day, original.Events, original.Types);
                Assert.Equal(3766, Assert.Single(progress).Current);
            }
        }
        finally { await writer; }
    }

    [Fact]
    public async Task Disabling_original_plan_inside_native_publication_retires_the_whole_batch_without_financial_writes()
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync(); f.Data.Enable();
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            await using var db = await f.Data.Factory.CreateDbContextAsync(Ct);
            await db.Database.ExecuteSqlRawAsync("UPDATE Schedules SET ReminderEnabled=0", Ct);
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending);
    }

    [Fact]
    public async Task Income_reaching_an_original_balance_goal_inside_native_publication_withdraws_its_old_reminder()
    {
        await using var f = new ProfileReminderFlowTests.Fixture(); await f.SeedAsync();
        await f.Data.Goals.SaveGoalWithContributionPlanAsync(f.Data.Goal, new ContributionPlan
        { GoalId = f.Data.Goal.Id, Amount = 1000, ReminderEnabled = true, Rule = new() { Start = new(2026, 10, 10), Frequency = Frequency.Once } }, Ct);
        f.Data.Enable(ProductPlan.Plus);
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            var result = await f.Data.Store.SaveEntryAsync(new LedgerEntry
            { AccountId = f.Data.Account.Id, Kind = EntryKind.Income, Date = new(2026, 10, 10), Amount = 10000 }, Ct);
            Assert.True(result.Succeeded);
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending);
    }
}
