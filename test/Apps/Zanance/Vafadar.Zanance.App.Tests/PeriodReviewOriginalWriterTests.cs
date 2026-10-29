using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Real original checklist progress writer, actual SaveChanges retirement and accepted notification outcomes.</summary>
public sealed class PeriodReviewOriginalWriterTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<PeriodReviewReadSnapshot> PrepareAsync(FlowFixture f)
    {
        await f.Store.GetSettingsAsync(Ct);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        db.Accounts.Add(new Account { Name = "Original review account", CurrencyCode = "EUR",
            OpeningDate = new(2026, 1, 1), OpeningBalance = 10000 });
        await db.SaveChangesAsync(Ct);
        return await f.Store.ReadPeriodReviewAsync(cancellationToken: Ct);
    }

    private static Task<string> WriteAsync(ZananceStore store, FlowFixture f, PeriodReviewReadSnapshot source,
        bool finish, Func<bool> current, Action accepted) => store.SavePeriodReviewProgressAsync(source,
            PeriodCalendar.Gregorian, DateOnly.FromDateTime(f.Time.Now.DateTime), "2026-09",
            finish ? null : ReviewStep.Unreviewed, true, current, Ct, accepted);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retirement_at_actual_original_SaveChanges_rolls_back_progress_and_preserves_all_financial_rows(bool finish)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var active = true; var accepted = false;
        var factory = new RetiringFactory(AccountDetailOriginalBoundaryTests.Factory(f), () => active = false);
        var writer = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WriteAsync(writer, f, source, finish, () => active, () => accepted = true));
        Assert.False(accepted); Assert.Equal(1, factory.SavingEvents);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accepted_original_progress_survives_a_failed_notification_without_becoming_replayable(bool finish)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f); var accepted = 0;
        var expected = finish ? "2026-09" : "2026-09:Unreviewed";
        f.Store.Changed += (_, _) => throw new IOException("Fictitious original progress notification failure.");
        var failure = await Assert.ThrowsAsync<CommittedWriteNotificationException<string>>(() =>
            WriteAsync(f.Store, f, source, finish, () => true, () => accepted++));
        Assert.Equal(expected, failure.Result); Assert.Equal(1, accepted);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        Assert.Equal(expected, (await db.Settings.SingleAsync(Ct)).ReviewProgress);
        Assert.Equal(10000, (await db.Accounts.SingleAsync(Ct)).OpeningBalance);
        Assert.Empty(await db.Entries.ToListAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => WriteAsync(f.Store, f, source, finish, () => true, () => accepted++));
        Assert.Equal(1, accepted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_new_persisted_original_settings_choice_is_never_overwritten_by_old_progress(bool finish)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Settings.ExecuteUpdateAsync(set => set.SetProperty(s => s.MonthStartDay, 15), Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => WriteAsync(f.Store, f, source, finish, () => true, () => Assert.Fail("Must not commit")));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_progress_cannot_cross_the_reviewed_local_day(bool finish)
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        var reviewedDay = DateOnly.FromDateTime(f.Time.Now.DateTime);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.Now = f.Time.Now.AddDays(1);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Store.SavePeriodReviewProgressAsync(source,
            PeriodCalendar.Gregorian, reviewedDay, "2026-09", finish ? null : ReviewStep.Unreviewed,
            true, () => true, Ct, () => Assert.Fail("An old local day must not commit")));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    private sealed class RetiringFactory(IDbContextFactory<ZananceDbContext> original, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        internal int SavingEvents;
        public ZananceDbContext CreateDbContext() => Bind(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Bind(await original.CreateDbContextAsync(cancellationToken));
        private ZananceDbContext Bind(ZananceDbContext db)
        { db.SavingChanges += (_, _) => { SavingEvents++; retire(); }; return db; }
    }
}
