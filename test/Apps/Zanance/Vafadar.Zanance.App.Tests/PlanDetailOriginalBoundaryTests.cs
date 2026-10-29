using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original plan-detail store composition against distinct same-identity fictitious files.</summary>
public sealed class PlanDetailOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static readonly DateOnly Today = new(2026, 10, 9);
    internal static PlanStore Plans(FlowFixture f) => f.Services.GetRequiredService<PlanStore>();
    internal static async Task<(Guid Account, Guid Plan)> SeedAsync(FlowFixture f, string currency = "EUR", long amount = 1000,
        (Guid Account, Guid Plan)? originalIds = null)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original " + currency, Type = AccountType.Checking,
            CurrencyCode = currency, OpeningDate = Today, OpeningBalance = 10000 };
        db.Accounts.Add(account);
        if (originalIds is { } ids) db.Entry(account).Property(item => item.Id).CurrentValue = ids.Account;
        var plan = new Schedule { Name = "Original " + currency + " plan", AccountId = account.Id,
            Amount = amount, Rule = new() { Frequency = Frequency.Monthly, Start = Today }, Note = "Original unchanged note" };
        db.Schedules.Add(plan);
        if (originalIds is { } same) db.Entry(plan).Property(item => item.Id).CurrentValue = same.Plan;
        await db.SaveChangesAsync(Ct); return (account.Id, plan.Id);
    }

    private static (ZananceStore Store, PlanStore Plans) Reader(FlowFixture f, IDbContextFactory<ZananceDbContext> factory)
    {
        var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var store = new ZananceStore(factory, f.Time, access); return (store, new(factory, store, access));
    }

    [Fact]
    public async Task Original_detail_read_cannot_combine_one_files_plan_with_another_files_account_currency()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var ids = await SeedAsync(f); await SeedAsync(other, "USD", 2500, ids); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => Interlocked.Increment(ref calls) == 1 ? AccountDetailOriginalBoundaryTests.Factory(f) : AccountDetailOriginalBoundaryTests.Factory(other) };
        var reader = Reader(f, factory);
        // The exact failing published composition is preserved separately. The real corrected reader
        // must refuse final validation in the replacement file before any plan/currency publication.
        var snapshot = await reader.Plans.ReadDetailSnapshotAsync(ids.Plan, Today, ct: Ct);
        Assert.Equal("EUR", Assert.Single(snapshot.Source.Inputs.Accounts).CurrencyCode);
        Assert.Equal(1000, snapshot.Source.Inputs.Schedule!.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.Plans.ValidateDetailSnapshotAsync(snapshot, Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_detail_confirmation_cannot_pause_or_delete_a_same_identity_plan_in_a_replacement_file(bool delete)
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var ids = await SeedAsync(f); await SeedAsync(other, "USD", 2500, ids); var switched = false;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => switched ? AccountDetailOriginalBoundaryTests.Factory(other) : AccountDetailOriginalBoundaryTests.Factory(f) };
        var reader = Reader(f, factory); var original = await reader.Plans.ReadDetailSnapshotAsync(ids.Plan, Today, ct: Ct);
        var prepared = reader.Plans.PrepareDetailChange(original, delete ? PlanDetailOperation.Delete : PlanDetailOperation.Pause, Today);
        // The original confirmation has returned after another file became current. The actual writer must refuse.
        switched = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.Plans.ApplyDetailChangeAsync(prepared, () => true, Ct));
        var unchanged = Assert.Single(await Plans(other).GetSchedulesAsync(Ct));
        Assert.Equal("Original USD plan", unchanged.Name); Assert.Equal(2500, unchanged.Amount); Assert.Equal(ScheduleState.Active, unchanged.State);
    }
}
