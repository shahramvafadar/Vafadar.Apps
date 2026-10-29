using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original plan-center aggregate/work/reference facts and retained first-file Retry authority.</summary>
public sealed class PlanCenterSnapshotValidationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static PlanStore Plans(FlowFixture fixture) => fixture.Services.GetRequiredService<PlanStore>();

    [Theory]
    [InlineData("plan")]
    [InlineData("state")]
    [InlineData("account")]
    [InlineData("category")]
    [InlineData("ledger-same-count")]
    [InlineData("review")]
    public async Task Original_center_validation_refuses_changed_complete_facts_without_replacing_the_original_packet(string change)
    {
        using var fixture = new FlowFixture(); await PlanCenterOriginalBoundaryTests.SeedAsync(fixture, "Original center", 1);
        var source = await Plans(fixture).ReadCenterSnapshotAsync(PlanDetailOriginalBoundaryTests.Today, ct: Ct);
        Assert.Equal(1, source.UnreviewedCount); var originalName = Assert.Single(source.Work.Schedules).Name;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(fixture).CreateDbContextAsync(Ct))
        {
            switch (change)
            {
                case "plan": (await db.Schedules.SingleAsync(Ct)).Name = "Changed original rule owner"; break;
                case "state": db.OccurrenceStates.Add(new() { ScheduleId = source.Work.Schedules[0].Id,
                    OriginalDate = PlanDetailOriginalBoundaryTests.Today, Status = OccurrenceStatus.Skipped }); break;
                case "account": (await db.Accounts.SingleAsync(Ct)).IsArchived = true; break;
                case "category": db.Categories.Add(new Category { Kind = CategoryKind.Expense, Name = "Changed original reference" }); break;
                case "ledger-same-count": (await db.Entries.SingleAsync(Ct)).Amount = 200; break;
                case "review": (await db.Entries.SingleAsync(Ct)).Review = ReviewState.Confirmed; break;
            }
            await db.SaveChangesAsync(Ct);
        }
        if (change == "ledger-same-count") Assert.Equal(1, await fixture.Store.CountUnreviewedAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Plans(fixture).ValidateCenterSnapshotAsync(source, Ct));
        Assert.Equal(1, source.UnreviewedCount); Assert.Equal(originalName, Assert.Single(source.Work.Schedules).Name);
    }

    [Fact]
    public async Task A_mutated_original_count_reference_packet_cannot_gain_publication_authority()
    {
        using var fixture = new FlowFixture(); await PlanCenterOriginalBoundaryTests.SeedAsync(fixture, "Original center", 1);
        var source = await Plans(fixture).ReadCenterSnapshotAsync(PlanDetailOriginalBoundaryTests.Today, ct: Ct);
        Assert.Single(source.Accounts).Name = "Mutated detached reference";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Plans(fixture).ValidateCenterSnapshotAsync(source, Ct));
        Assert.Equal("Original center", Assert.Single(await fixture.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
    }

    [Fact]
    public async Task The_first_original_file_is_bound_even_when_its_first_aggregate_read_fails()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        await PlanCenterOriginalBoundaryTests.SeedAsync(first, "Original first", 1);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(first).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE HomeFinancialRevision SET Revision='zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz' WHERE Id=1", Ct);
        var access = first.Services.GetRequiredService<Vafadar.Zanance.Data.Commerce.ICommercialWriteAccessSource>();
        var switched = false; var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => switched ? AccountDetailOriginalBoundaryTests.Factory(second) : AccountDetailOriginalBoundaryTests.Factory(first) };
        var store = new ZananceStore(factory, first.Time, access); var plans = new PlanStore(factory, store, access); string? original = null;
        bool Accept(string path) { original ??= path; return original == path; }
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.ReadCenterSnapshotAsync(PlanDetailOriginalBoundaryTests.Today, Accept, Ct));
        Assert.NotNull(original); switched = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.ReadCenterSnapshotAsync(PlanDetailOriginalBoundaryTests.Today, Accept, Ct));
        Assert.Empty(await second.Store.GetAccountsAsync(cancellationToken: Ct));
    }
}
