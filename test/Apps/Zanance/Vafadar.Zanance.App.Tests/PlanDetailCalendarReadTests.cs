using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original read-only plan-detail authority includes both representable calendar endpoints.</summary>
public sealed class PlanDetailCalendarReadTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(3652058)]
    public async Task An_explicit_endpoint_day_reads_and_revalidates_without_mutating_any_source_table(int dayNumber)
    {
        using var fixture = new FlowFixture();
        var day = DateOnly.FromDayNumber(dayNumber);
        var plan = new Schedule { Name = "Fictitious endpoint plan", Amount = 123,
            Rule = new() { Frequency = Frequency.Once, Start = day } };
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(fixture).CreateDbContextAsync(Ct))
        {
            var account = new Account { Name = "Fictitious zero opening", Type = AccountType.Checking,
                CurrencyCode = "EUR", OpeningDate = DateOnly.MinValue, OpeningBalance = 0 };
            db.Accounts.Add(account); plan.AccountId = account.Id; db.Schedules.Add(plan);
            await db.SaveChangesAsync(Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var store = fixture.Services.GetRequiredService<PlanStore>();

        var source = await store.ReadDetailSnapshotAsync(plan.Id, day, ct: Ct);
        await store.ValidateDetailSnapshotAsync(source, Ct);

        Assert.Equal(day, source.Source.Request.Today);
        Assert.Equal(plan.Id, source.Source.Inputs.Schedule!.Id);
        Assert.Equal(123, source.Source.Inputs.Schedule.Amount);
        Assert.True(source.Source.IsAvailable);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Fact]
    public async Task A_valid_first_day_does_not_make_an_empty_plan_identity_acceptable()
    {
        using var fixture = new FlowFixture();
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var store = fixture.Services.GetRequiredService<PlanStore>();

        await Assert.ThrowsAsync<ArgumentException>(() => store.ReadDetailSnapshotAsync(Guid.Empty, DateOnly.MinValue, ct: Ct));

        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }
}
