using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Fictitious same-id profile files exercise the original occurrence composition and command boundary.</summary>
public sealed class OccurrenceOriginFailureTests
{
    internal static readonly DateOnly Day = new(2026, 11, 7);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static IDbContextFactory<ZananceDbContext> Factory(FlowFixture f) => AccountDetailOriginalBoundaryTests.Factory(f);
    internal static async Task<(Guid Account, Guid Plan)> SeedAsync(FlowFixture f, string currency, (Guid Account, Guid Plan)? ids = null)
    {
        await using var db = await Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Fictitious " + currency, Type = AccountType.Checking, CurrencyCode = currency,
            OpeningDate = Day, OpeningBalance = 0 };
        db.Accounts.Add(account); if (ids is { } existing) db.Entry(account).Property(x => x.Id).CurrentValue = existing.Account;
        var plan = new Schedule { Name = "Fictitious " + currency + " plan", AccountId = account.Id, Amount = 123,
            Rule = new() { Frequency = Frequency.Once, Start = Day } };
        db.Schedules.Add(plan); if (ids is { } same) db.Entry(plan).Property(x => x.Id).CurrentValue = same.Plan;
        if (ids is not null) db.OccurrenceStates.Add(new OccurrenceState { ScheduleId = plan.Id, OriginalDate = Day, DueDate = Day.AddDays(2) });
        await db.SaveChangesAsync(Ct); return (account.Id, plan.Id);
    }
    internal static (ZananceStore Store, PlanStore Plans) Reader(FlowFixture f, IDbContextFactory<ZananceDbContext> factory)
    {
        var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var store = new ZananceStore(factory, f.Time, access); return (store, new(factory, store, access));
    }

    [Fact]
    public async Task Published_original_occurrence_read_must_not_join_a_plan_to_another_files_currency_and_override()
    {
        using var first = new FlowFixture(); using var other = new FlowFixture();
        var ids = await SeedAsync(first, "EUR"); await SeedAsync(other, "USD", ids);
        var beforeA = await GoalChooserFlowTests.SnapshotAsync(first); var beforeB = await GoalChooserFlowTests.SnapshotAsync(other); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(Factory(first))
            { Select = () => Interlocked.Increment(ref calls) == 1 ? Factory(first) : Factory(other) };
        var reader = Reader(first, factory);
        // The old published multi-read failure remains archived; one new source now
        // captures its original context before the replacement factory can be selected.
        var source = await reader.Plans.ReadOccurrenceSnapshotAsync(ids.Plan, Day, Day, ct: Ct);
        var occurrence = source.Occurrence!;
        var accounts = source.Source.Inputs.Accounts;
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.Plans.ValidateOccurrenceSnapshotAsync(source, Ct));
        Assert.Equal(beforeA, await GoalChooserFlowTests.SnapshotAsync(first));
        Assert.Equal(beforeB, await GoalChooserFlowTests.SnapshotAsync(other));
        Assert.Equal("EUR", accounts.Single(x => x.Id == ids.Account).CurrencyCode);
        Assert.Equal(Day, occurrence.DueDate);
    }

    [Fact]
    public async Task Original_skip_must_not_correct_an_identical_plan_in_a_replacement_profile_file()
    {
        using var first = new FlowFixture(); using var other = new FlowFixture();
        var ids = await SeedAsync(first, "EUR"); await SeedAsync(other, "USD", ids);
        var source = await first.Services.GetRequiredService<PlanStore>().ReadOccurrenceSnapshotAsync(ids.Plan, Day, Day, ct: Ct);
        var occurrence = source.Occurrence!;
        var beforeA = await GoalChooserFlowTests.SnapshotAsync(first); var beforeB = await GoalChooserFlowTests.SnapshotAsync(other);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(Factory(first)) { Select = () => Factory(other) };
        var reader = Reader(first, factory); var published = 0;
        var command = new OccurrenceCommands(reader.Plans, first.Translator, first.Platform);
        var accepted = await command.SkipAsync(occurrence, () => { published++; return Task.CompletedTask; }, original: source);
        Assert.Empty(await other.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(beforeA, await GoalChooserFlowTests.SnapshotAsync(first));
        Assert.False(accepted);
        Assert.Equal(beforeB, await GoalChooserFlowTests.SnapshotAsync(other));
        Assert.Equal(0, published);
    }
}
