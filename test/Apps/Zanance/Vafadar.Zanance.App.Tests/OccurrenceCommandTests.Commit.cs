using Vafadar.Zanance.Data;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class OccurrenceCommandTests
{
    /// <summary>A subscriber refusal after actual SQLite commit must retire the original command before any retry.</summary>
    [Theory]
    [InlineData("partial", false)]
    [InlineData("partial", true)]
    [InlineData("full", false)]
    [InlineData("full", true)]
    [InlineData("link", true)]
    [InlineData("skip", true)]
    [InlineData("change", true)]
    [InlineData("unskip", true)]
    [InlineData("unsettle", false)]
    [InlineData("unsettle", true)]
    [InlineData("unsettle-linked", false)]
    [InlineData("unsettle-linked", true)]
    public async Task Post_commit_notification_failure_publishes_the_original_accepted_outcome(string action, bool planObserver)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action);
        var before = await SnapshotAsync(f); var publications = 0; var notices = 0;
        void Refuse(object? sender, EventArgs args) { notices++; throw new IOException("Original accepted notification refusal"); }
        if (planObserver) seed.Plans.Changed += Refuse; else f.Store.Changed += Refuse;
        f.Platform.Confirmations.Enqueue(true);
        var commands = new App.Features.Plans.OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var accepted = await ActAsync(commands, seed, action, () => { publications++; return Task.CompletedTask; });
        Assert.NotEqual(before, await SnapshotAsync(f)); Assert.Equal(1, notices);
        Assert.True(accepted); Assert.Equal(1, publications); Assert.Empty(f.Platform.Failures);
        Assert.Single(f.Platform.Alerts);
        if (action == "partial")
        {
            var payment = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.Equal(400, payment.Amount); Assert.True(payment.IsPartialPayment);
            Assert.Equal(400, Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct)).PaidAmount);
        }
        if (action == "full")
        {
            Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.Equal(OccurrenceStatus.Settled, Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct)).Status);
        }
        if (action == "unsettle-linked")
        {
            var entry = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.Equal(seed.Candidate.Id, entry.Id); Assert.Equal(950, entry.Amount);
            Assert.Null(entry.ScheduleId); Assert.Null(entry.OccurrenceDate);
            Assert.True(Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct)).AutoPostSuppressed);
        }
    }

    [Fact]
    public async Task Accepted_notification_alert_holds_the_original_command_gate_after_retirement()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "partial");
        f.Store.Changed += (_, _) => throw new IOException("Owned committed subscriber refusal");
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var retired = 0;
        f.Platform.PendingAlert = () => { shown.TrySetResult(); return dismissed.Task; };
        var commands = new App.Features.Plans.OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var pending = ActAsync(commands, seed, "partial", () => { retired++; return Task.CompletedTask; });
        await shown.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.Equal(1, retired); Assert.False(await ActAsync(commands, seed, "skip", () => Task.CompletedTask));
        Assert.Equal(400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        Assert.Empty(f.Platform.Failures); dismissed.SetResult(); Assert.True(await pending);
        Assert.Single(f.Platform.Alerts);
    }

    [Fact]
    public async Task Accepted_write_with_failed_follow_through_uses_only_the_saved_refresh_message()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "partial"); var retired = 0;
        f.Store.Changed += (_, _) => throw new IOException("Owned committed subscriber refusal");
        Assert.True(await ActAsync(new(seed.Plans, f.Translator, f.Platform), seed, "partial", () =>
        { retired++; throw new IOException("Original native follow-through refusal"); }));
        Assert.Equal(1, retired); Assert.Empty(f.Platform.Failures);
        Assert.Equal(f.Translator["Occurrence_SavedRefreshFailed"], Assert.Single(f.Platform.Alerts).Message);
        Assert.Equal(400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Fact]
    public async Task Failed_native_saved_alert_cannot_put_the_accepted_write_back_inside_the_rejected_write_catch()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "partial"); var retired = 0;
        f.Store.Changed += (_, _) => throw new IOException("Owned committed subscriber refusal");
        f.Platform.PendingAlert = () => throw new IOException("Original native alert refusal");
        await Assert.ThrowsAsync<IOException>(() => ActAsync(new(seed.Plans, f.Translator, f.Platform), seed, "partial", () =>
        { retired++; return Task.CompletedTask; }));
        Assert.Equal(1, retired); Assert.Empty(f.Platform.Failures);
        Assert.Equal(400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), InlineData("es"), InlineData("fr"), InlineData("it")]
    public async Task Notification_refusal_has_a_complete_translated_saved_message(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var seed = await SeedAsync(f, "partial"); seed.Plans.Changed += (_, _) => throw new OperationCanceledException("Original post-commit observer cancellation");
        var retired = 0;
        Assert.True(await ActAsync(new(seed.Plans, f.Translator, f.Platform), seed, "partial", () => { retired++; return Task.CompletedTask; }));
        Assert.Equal(1, retired); Assert.Empty(f.Platform.Failures);
        var message = Assert.Single(f.Platform.Alerts).Message;
        Assert.Equal(f.Translator["Occurrence_SavedNotificationFailed"], message);
        Assert.NotEqual("Occurrence_SavedNotificationFailed", message); Assert.False(string.IsNullOrWhiteSpace(message));
    }
}
