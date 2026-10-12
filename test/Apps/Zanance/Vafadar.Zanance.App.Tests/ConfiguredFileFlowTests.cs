using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-162: original configured targets remain consistent through real readers/writers; another file is still rejected.</summary>
[Trait("AT", "AT-162")]
public sealed class ConfiguredFileFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Original_closed_read_review_survives_the_actual_open_filter_writer_without_changing_unrelated_rows(bool holdReader)
    {
        using var f = new FlowFixture(); var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var review = await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        await using var db = await factory.CreateDbContextAsync(Ct);
        if (holdReader) await db.Database.OpenConnectionAsync(Ct);
        await f.Store.SaveSavedFilterAsync(new SavedFilter { Name = "Fictitious actual configured target", Search = "#original" }, Ct, review);
        Assert.Equal("#original", Assert.Single(await f.Store.GetSavedFiltersAsync(Ct)).Search);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var next = await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        Assert.True(next.BelongsToSameFile(review));
    }
    [Fact]
    public async Task Closed_original_rule_snapshots_validate_inside_the_coherent_open_financial_reader_and_period_review()
    {
        using var f = new FlowFixture();
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct); var settings = await f.Store.GetSettingsAsync(Ct);
        var plans = f.Services.GetRequiredService<PlanStore>(); var goals = f.Services.GetRequiredService<GoalStore>();
        await plans.SaveScheduleAsync(new Schedule { Name = "Fictitious original configured plan", AccountId = account.Id, Amount = 1000,
            Rule = new() { Start = new(2026, 10, 9) } }, Ct);
        var planReview = await plans.GetWorkSnapshotAsync(Ct); var goalReview = await goals.GetWorkSnapshotAsync(Ct);
        Assert.Equal(planReview.ReminderScope, goalReview.ReminderScope);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var financials = await f.Store.GetReminderFinancialSnapshotAsync(planReview, goalReview, new(2026, 10, 9), Ct);
        await f.Store.ValidateReminderPreparationAsync(financials, planReview, goalReview, JsonSerializer.Serialize(settings), account.OpeningDate, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Another_actual_configured_file_cannot_borrow_original_rule_reviews_or_filter_rights()
    {
        using var original = new FlowFixture(); using var other = new FlowFixture();
        var filterReview = await original.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.SaveSavedFilterAsync(
            new SavedFilter { Name = "Fictitious rejected foreign review" }, Ct, filterReview));
        var plans = await original.Services.GetRequiredService<PlanStore>().GetWorkSnapshotAsync(Ct);
        var goals = await original.Services.GetRequiredService<GoalStore>().GetWorkSnapshotAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.GetReminderFinancialSnapshotAsync(plans, goals, new(2026, 10, 9), Ct));
        Assert.Empty(await other.Store.GetSavedFiltersAsync(Ct)); Assert.Empty(await other.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
