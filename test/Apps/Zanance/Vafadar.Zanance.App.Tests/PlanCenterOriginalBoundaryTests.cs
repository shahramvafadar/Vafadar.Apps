using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original plan-center review badge and row facts must belong to one actual configured file.</summary>
public sealed class PlanCenterOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static async Task SeedAsync(FlowFixture fixture, string name, int pending)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(fixture).CreateDbContextAsync(Ct);
        var account = new Account { Name = name, Type = AccountType.Checking, CurrencyCode = "EUR",
            OpeningDate = PlanDetailOriginalBoundaryTests.Today, OpeningBalance = 0 };
        db.Accounts.Add(account);
        db.Schedules.Add(new Schedule { Name = name, AccountId = account.Id, Amount = 1000,
            Rule = new() { Frequency = Frequency.Monthly, Start = PlanDetailOriginalBoundaryTests.Today } });
        for (var index = 0; index < pending; index++)
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 100,
                Date = PlanDetailOriginalBoundaryTests.Today, Review = ReviewState.Unreviewed, Note = name + index });
        await db.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Original_plan_center_review_count_cannot_publish_another_files_badge_beside_original_rows()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        await SeedAsync(first, "First original plan", 1); await SeedAsync(second, "Second original plan", 3);
        var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        {
            Select = () => Interlocked.Increment(ref calls) == 1
                ? AccountDetailOriginalBoundaryTests.Factory(first) : AccountDetailOriginalBoundaryTests.Factory(second),
        };
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var plans = new PlanStore(factory, store, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        // The failing published four-call composition is preserved in local evidence.
        // The corrected actual reader keeps the badge and rows in one first-file snapshot,
        // then refuses the replacement file before publication validation can query it.
        var snapshot = await plans.ReadCenterSnapshotAsync(PlanDetailOriginalBoundaryTests.Today, ct: Ct);
        Assert.Equal(1, calls);
        Assert.Equal("First original plan", Assert.Single(snapshot.Work.Schedules).Name);
        Assert.Equal(1, snapshot.UnreviewedCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.ValidateCenterSnapshotAsync(snapshot, Ct));
    }
}
