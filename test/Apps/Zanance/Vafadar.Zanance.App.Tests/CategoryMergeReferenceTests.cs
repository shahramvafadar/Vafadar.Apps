using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Full typed merge identity includes original non-reference money, names, rules and embedded limits.</summary>
public sealed class CategoryMergeReferenceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("entry")]
    [InlineData("schedule")]
    [InlineData("template")]
    [InlineData("rule")]
    [InlineData("contribution")]
    [InlineData("filter")]
    [InlineData("budget-limit")]
    public async Task Changed_non_reference_field_in_any_original_merge_table_rejects_without_overwriting_current_source(string changed)
    {
        using var f = new FlowFixture(); var (from, to) = await PrepareAsync(f);
        var categories = await f.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var review = await f.Store.ReadCategoryMergeReviewAsync(categories, from, to, () => true, Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            switch (changed)
            {
                case "entry": await db.Entries.ExecuteUpdateAsync(set => set.SetProperty(item => item.Amount, 1235L), Ct); break;
                case "schedule": await db.Schedules.ExecuteUpdateAsync(set => set.SetProperty(item => item.Amount, 2001L), Ct); break;
                case "template": await db.Templates.ExecuteUpdateAsync(set => set.SetProperty(item => item.Title, "Changed template title"), Ct); break;
                case "rule": await db.CategoryRules.ExecuteUpdateAsync(set => set.SetProperty(item => item.Match, "D638_CURRENT_MATCH"), Ct); break;
                case "contribution": await db.ContributionPlans.ExecuteUpdateAsync(set => set.SetProperty(item => item.Amount, 4001L), Ct); break;
                case "filter": await db.SavedFilters.ExecuteUpdateAsync(set => set.SetProperty(item => item.Search, "Changed search"), Ct); break;
                case "budget-limit": (await db.Budgets.SingleAsync(Ct)).CategoryLimits.Single().Limit = 5001; await db.SaveChangesAsync(Ct); break;
            }
        }
        var current = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.MergeReviewedCategoryAsync(review, () => true, Ct));
        Assert.Equal(current, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False((await f.Store.GetCategoriesAsync(Ct)).Single(item => item.Id == from).IsArchived);
    }

    private static async Task<(Guid From, Guid To)> PrepareAsync(FlowFixture f)
    {
        Guid from; Guid to;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            var account = new Account { Name = "D638 graph account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            var source = new Category { Name = "D638 graph source" }; var target = new Category { Name = "D638 graph target" };
            var goal = new Goal { Name = "D638 graph goal", CurrencyCode = "EUR", TargetAmount = 10000 };
            db.Accounts.Add(account); db.Categories.AddRange(source, target); db.Goals.Add(goal); (from, to) = (source.Id, target.Id);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, CategoryId = from, Kind = EntryKind.Expense,
                Amount = 1234, Date = new(2026, 10, 2), Title = "D638 graph entry", Tags = ["Original", "Ordered"] });
            db.Schedules.Add(new Schedule { Name = "D638 graph plan", AccountId = account.Id, CategoryId = from, Amount = 2000,
                Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = new(2026, 11, 1) }, ReminderEnabled = false, AutoPost = false });
            db.Templates.Add(new EntryTemplate { Name = "D638 graph template", AccountId = account.Id, CategoryId = from,
                Kind = EntryKind.Expense, Amount = 3000, Title = "D638 original template title" });
            db.CategoryRules.Add(new CategoryRule { Match = "D638_ORIGINAL_MATCH", CategoryId = from, Kind = CategoryKind.Expense });
            db.ContributionPlans.Add(new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.SpendingCut,
                Amount = 4000, CategoryIds = [from, to], Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new(2026, 10, 1) } });
            db.SavedFilters.Add(new SavedFilter { Name = "D638 graph filter", CategoryIds = [from, to], Search = "Original" });
            db.Budgets.Add(new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10,
                CategoryLimits = [new BudgetCategoryLimit { CategoryId = from, Limit = 5000 }] });
            await db.SaveChangesAsync(Ct);
        }
        return (from, to);
    }

    [Fact]
    public async Task Accepted_reviewed_merge_preserves_complete_original_payloads_and_the_same_review_cannot_replay()
    {
        using var f = new FlowFixture(); var (from, to) = await PrepareAsync(f);
        LedgerEntry entry; Schedule schedule; EntryTemplate template; CategoryRule rule;
        ContributionPlan contribution; SavedFilter filter; Budget budget;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            entry = await db.Entries.AsNoTracking().SingleAsync(Ct); schedule = await db.Schedules.AsNoTracking().SingleAsync(Ct);
            template = await db.Templates.AsNoTracking().SingleAsync(Ct); rule = await db.CategoryRules.AsNoTracking().SingleAsync(Ct);
            contribution = await db.ContributionPlans.AsNoTracking().SingleAsync(Ct); filter = await db.SavedFilters.AsNoTracking().SingleAsync(Ct);
            budget = await db.Budgets.AsNoTracking().SingleAsync(Ct);
        }
        entry.CategoryId = to; schedule.CategoryId = to; template.CategoryId = to; rule.CategoryId = to;
        contribution.CategoryIds = [to]; filter.CategoryIds = [to]; budget.CategoryLimits.Single().CategoryId = to;
        var source = await f.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var review = await f.Store.ReadCategoryMergeReviewAsync(source, from, to, () => true, Ct); var accepted = false;
        var receipt = await f.Store.MergeReviewedCategoryAsync(review, () => true, Ct, () => accepted = true);
        Assert.True(accepted); Assert.Equal(from, receipt.CategoryId);
        Assert.True(receipt.Source.Categories.Single(item => item.Id == from).IsArchived);
        await f.Store.ValidateCategorySnapshotAsync(receipt.Source, Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            Assert.Equal(JsonSerializer.Serialize(entry), JsonSerializer.Serialize(await db.Entries.AsNoTracking().SingleAsync(Ct)));
            Assert.Equal(JsonSerializer.Serialize(schedule), JsonSerializer.Serialize(await db.Schedules.AsNoTracking().SingleAsync(Ct)));
            Assert.Equal(JsonSerializer.Serialize(template), JsonSerializer.Serialize(await db.Templates.AsNoTracking().SingleAsync(Ct)));
            Assert.Equal(JsonSerializer.Serialize(rule), JsonSerializer.Serialize(await db.CategoryRules.AsNoTracking().SingleAsync(Ct)));
            Assert.Equal(JsonSerializer.Serialize(contribution), JsonSerializer.Serialize(await db.ContributionPlans.AsNoTracking().SingleAsync(Ct)));
            Assert.Equal(JsonSerializer.Serialize(filter), JsonSerializer.Serialize(await db.SavedFilters.AsNoTracking().SingleAsync(Ct)));
            Assert.Equal(JsonSerializer.Serialize(budget), JsonSerializer.Serialize(await db.Budgets.AsNoTracking().SingleAsync(Ct)));
        }
        var committed = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.MergeReviewedCategoryAsync(review, () => true, Ct));
        Assert.Equal(committed, await GoalChooserFlowTests.SnapshotAsync(f));
    }

}
