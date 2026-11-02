using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original continuation dates cannot overlap the predecessor or mutate a refused pause.</summary>
public sealed class PlanResumeRangeTests
{
    private static readonly DateOnly Today = PlanDetailOriginalBoundaryTests.Today;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Refuses a restart before the pause without changing any original domain field.</summary>
    [Theory]
    [InlineData(-1, false)] [InlineData(-7, false)] [InlineData(-40, false)]
    [InlineData(-1, true)] [InlineData(-7, true)] [InlineData(-40, true)]
    public void A_resume_before_the_pause_refuses_before_mutating_the_original(int offset, bool autoPost)
    {
        var plan = Paused(autoPost);
        var before = JsonSerializer.Serialize(plan);
        var failure = Assert.Throws<ArgumentOutOfRangeException>(() => PlanActions.Resume(plan, Today.AddDays(offset)));
        Assert.Equal("resumeFrom", failure.ParamName);
        Assert.Equal(before, JsonSerializer.Serialize(plan));
    }

    /// <summary>Keeps the pause day and later restart boundaries disjoint with their original anchor.</summary>
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(20)]
    public void A_valid_resume_retains_disjoint_original_ownership(int offset)
    {
        var plan = Paused(true);
        var anchor = plan.Rule.Start;
        var restart = Today.AddDays(offset);
        var next = Assert.IsType<Schedule>(PlanActions.Resume(plan, restart));
        Assert.Equal(Today.AddDays(-1), plan.ActiveUntil);
        Assert.Equal(restart, next.ActiveFrom);
        Assert.Equal(restart, next.AutoPostFrom);
        Assert.Equal(anchor, next.Rule.Start);
        Assert.Equal(plan.Id, next.PreviousScheduleId);
        Assert.False(plan.AutoPost);
        Assert.Equal(ScheduleState.Active, next.State);
        for (var day = Today.AddDays(-45); day <= Today.AddDays(25); day = day.AddDays(1))
            Assert.False(plan.Owns(day) && next.Owns(day));
    }

    /// <summary>Rejects overlapping or unbounded forged continuations before any SQLite row changes.</summary>
    [Theory]
    [InlineData(-7)] [InlineData(-1)] [InlineData(0)] [InlineData(null)]
    public async Task The_atomic_writer_refuses_an_overlapping_or_unbounded_predecessor(int? offset)
    {
        using var f = new FlowFixture();
        var plan = await SeedAsync(f);
        var plans = f.Services.GetRequiredService<PlanStore>();
        var before = await PacketAsync(f);
        var previous = plan.CopyForEditing();
        previous.State = ScheduleState.Ended;
        previous.PausedFrom = null;
        previous.ActiveUntil = offset is null ? null : Today;
        var next = plan.CopyForEditing();
        // A newly constructed identity models a caller bypassing the domain transition.
        next = new Schedule { Name = next.Name, AccountId = next.AccountId, Amount = next.Amount,
            Rule = next.Rule.Clone(), State = ScheduleState.Active, PreviousScheduleId = plan.Id,
            ActiveFrom = Today.AddDays(offset ?? 1) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.SaveSplitAsync(previous, next, Ct));
        Assert.Equal(before, await PacketAsync(f));
    }

    /// <summary>Original action feedback rejects the raw invalid date without acceptance or source mutation.</summary>
    [Fact]
    public async Task The_original_resume_action_reports_the_date_error_and_keeps_its_source()
    {
        using var f = new FlowFixture();
        var plan = await SeedAsync(f);
        var plans = f.Services.GetRequiredService<PlanStore>();
        var source = await plans.ReadDetailSnapshotAsync(plan.Id, Today, ct: Ct);
        var actor = new PlanDetailActions(plans, f.Translator, f.Localization, f.Time, f.Platform);
        actor.SetOwner(plan.Id, true);
        Assert.False(actor.HasFeedback);
        var bannerNotifications = 0;
        actor.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(actor.HasFeedback)) bannerNotifications++; };
        var before = await PacketAsync(f);
        Assert.Null(await actor.RunAsync(source, PlanDetailOperation.Resume, Today.AddDays(-1), () => true));
        Assert.False(source.Source.Accepted);
        Assert.False(source.Source.IsPending);
        Assert.Null(actor.AcceptedChange);
        Assert.False(actor.IsBusy);
        Assert.Null(actor.Failure);
        Assert.Equal(f.Translator["Plan_ResumeBeforePause"], actor.Feedback);
        Assert.True(actor.HasFeedback);
        Assert.True(bannerNotifications > 0);
        Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(before, await PacketAsync(f));
    }

    private static Schedule Paused(bool autoPost) => new()
    {
        Name = "Original paused plan", Amount = 123, State = ScheduleState.Paused,
        PausedFrom = Today, AutoPost = autoPost, AutoPostFrom = Today.AddDays(-50),
        Rule = new() { Start = Today.AddDays(-50), Frequency = Frequency.Daily }, Note = "Unchanged original note",
    };

    private static async Task<Schedule> SeedAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Zero-opening review", CurrencyCode = "EUR", OpeningDate = Today };
        var plan = Paused(false); plan.AccountId = account.Id;
        db.Accounts.Add(account); db.Schedules.Add(plan);
        await db.SaveChangesAsync(Ct);
        return plan;
    }

    private static async Task<string> PacketAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        return JsonSerializer.Serialize(new { Accounts = await db.Accounts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(Ct),
            Plans = await db.Schedules.AsNoTracking().OrderBy(x => x.Id).ToListAsync(Ct),
            States = await db.OccurrenceStates.AsNoTracking().OrderBy(x => x.Id).ToListAsync(Ct),
            Entries = await db.Entries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(Ct),
            Settings = await db.Settings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(Ct) });
    }
}
