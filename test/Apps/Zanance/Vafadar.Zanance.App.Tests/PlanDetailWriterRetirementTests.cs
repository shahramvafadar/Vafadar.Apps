using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original owner retirement inside actual save/delete execution rolls back complete plan/history state.</summary>
public sealed class PlanDetailWriterRetirementTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = PlanDetailOriginalBoundaryTests.Today;

    [Theory]
    [InlineData(PlanDetailOperation.Pause)]
    [InlineData(PlanDetailOperation.Resume)]
    [InlineData(PlanDetailOperation.Delete)]
    public async Task Original_retirement_inside_the_real_writer_rolls_back_rows_and_cannot_report_acceptance(PlanDetailOperation operation)
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            if (operation == PlanDetailOperation.Resume)
                await db.Schedules.ExecuteUpdateAsync(set => set.SetProperty(s => s.State, ScheduleState.Paused).SetProperty(s => s.PausedFrom, (DateOnly?)Today), Ct);
            db.OccurrenceStates.Add(new() { ScheduleId = ids.Plan, OriginalDate = Today.AddMonths(1), Status = OccurrenceStatus.Open, Amount = 800 });
            await db.SaveChangesAsync(Ct);
        }
        var signal = new Signal();
        var factory = new ActualFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False")
            .AddInterceptors(new DuringSave(signal), new DuringDelete(signal)).Options, f.Time);
        var plans = new PlanStore(factory, f.Store, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var source = await plans.ReadDetailSnapshotAsync(ids.Plan, Today, ct: Ct);
        var actor = new PlanDetailActions(plans, f.Translator, f.Localization, f.Time, f.Platform); actor.SetOwner(ids.Plan, true);
        var before = await PacketAsync(f); signal.Retire = () => actor.SetOwner(ids.Plan, false);
        signal.Delete = operation == PlanDetailOperation.Delete;
        if (operation != PlanDetailOperation.Resume) f.Platform.Confirmations.Enqueue(true);
        Assert.Null(await actor.RunAsync(source, operation, Today.AddDays(1), () => true));
        Assert.True(signal.Tripped); Assert.False(source.Source.Accepted); Assert.Null(actor.AcceptedChange);
        Assert.Equal(before, await PacketAsync(f)); Assert.False(actor.IsBusy);
    }

    private static async Task<string> PacketAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        return JsonSerializer.Serialize(new { Accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync(Ct),
            Plans = await db.Schedules.AsNoTracking().OrderBy(p => p.Id).ToListAsync(Ct),
            States = await db.OccurrenceStates.AsNoTracking().OrderBy(s => s.Id).ToListAsync(Ct),
            Entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(Ct) });
    }
    private sealed class ActualFactory(DbContextOptions<ZananceDbContext> options, TimeProvider time) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => new(options, time);
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    }
    private sealed class Signal
    {
        internal Action? Retire;
        internal bool Delete, Tripped;
        internal void Trip() { if (Tripped || Retire is null) return; Tripped = true; Retire(); }
    }
    private sealed class DuringSave(Signal signal) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { if (!signal.Delete) signal.Trip(); return ValueTask.FromResult(result); }
    }
    private sealed class DuringDelete(Signal signal) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (signal.Delete && command.CommandText.Contains("DELETE FROM \"Schedules\"", StringComparison.Ordinal))
            { Assert.NotNull(command.Transaction); signal.Trip(); }
            return ValueTask.FromResult(result);
        }
    }
}
