using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Security;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Original source selection retains only its next actual native step across the real access drain.</summary>
public sealed class ReceiptSourceAccessTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Port : IAppInteraction
    {
        internal int Choices, Alerts, Files, Photos;
        internal Func<Task<string?>> Select = () => Task.FromResult<string?>(null);
        internal Func<Task> Acknowledge = () => Task.CompletedTask;
        internal Func<Task>? Opening;
        internal readonly List<string> Order = [];
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) { Choices++; return Select(); }
        public Task AlertAsync(string title, string message, string cancel) { Alerts++; return Acknowledge(); }
        internal async Task<string?> PhotoAsync() { Photos++; Order.Add("photo"); if (Opening is not null) await Opening(); return "photo"; }
        internal async Task<string?> FileAsync() { Files++; Order.Add("file"); if (Opening is not null) await Opening(); return "file"; }
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new NotSupportedException();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new NotSupportedException();
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new NotSupportedException();
        public Task ShowFailureAsync(Exception exception) => throw new NotSupportedException();
    }
    private static async Task<AppLockService> AccessAsync(FlowFixture f)
    {
        var settings = await f.Store.GetSettingsAsync(Ct); settings.AppLockEnabled = true; await f.Store.SaveSettingsAsync(settings, Ct);
        var access = AppLockTests.Create(f).Lock; await access.StartAsync(); await access.UnlockedAsync(); return access;
    }
    private static Task<string?> Start(FlowFixture f, AppLockService access, Port port, ReceiptReadOrigin origin, TaskCompletionSource deferred) =>
        ReceiptSourceChooser.RunAsync<string?>(f.Translator, port, true, true, origin, access.RunWhenUnlockedAsync,
            next => { var retained = access.RetainCurrentDestinationIfLocked(next); if (retained) deferred.TrySetResult(); return retained; },
            port.PhotoAsync, port.FileAsync, null);
    private static Task ReachedAsync(Task running, Task deferred) => Task.WhenAny(running, deferred).WaitAsync(TimeSpan.FromSeconds(20), Ct);
    [Theory][InlineData(true)][InlineData(false)]
    public async Task Chosen_source_after_cover_waits_ahead_of_later_input_without_reasking(bool photo)
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); var port = new Port(); using var origin = new ReceiptReadOrigin(() => true);
        port.Select = async () => { await access.SleepAsync(); await access.RunWhenUnlockedAsync(() => { port.Order.Add("later"); return Task.CompletedTask; }); return f.Translator[photo ? "Receipt_TakePhoto" : "Receipt_ChooseFile"]; };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var deferred = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = Start(f, access, port, origin, deferred); await ReachedAsync(running, deferred.Task);
        Assert.False(running.IsCompleted); Assert.Equal(0, port.Photos + port.Files); Assert.Equal(1, port.Choices); Assert.Equal(0, port.Alerts);
        await access.UnlockedAsync(); Assert.Equal(photo ? "photo" : "file", await running.WaitAsync(TimeSpan.FromSeconds(20), Ct));
        Assert.Equal(new[] { photo ? "photo" : "file", "later" }, port.Order); Assert.Equal(1, port.Choices);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Help_selected_before_cover_waits_without_late_alert_then_returns_to_the_same_source()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); var port = new Port(); using var origin = new ReceiptReadOrigin(() => true);
        port.Select = async () => { if (port.Choices == 1) { await access.SleepAsync(); return f.Translator["Receipt_ImageHelp"]; } return f.Translator["Receipt_ChooseFile"]; };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var deferred = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var running = Start(f, access, port, origin, deferred);
        await ReachedAsync(running, deferred.Task); Assert.False(running.IsCompleted); Assert.Equal(0, port.Alerts); Assert.Equal(1, port.Choices);
        await access.UnlockedAsync(); Assert.Equal("file", await running); Assert.Equal(1, port.Alerts); Assert.Equal(2, port.Choices); Assert.Equal(1, port.Files);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Cover_after_help_acknowledgement_never_replays_the_answered_notice()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); var port = new Port(); using var origin = new ReceiptReadOrigin(() => true);
        port.Select = () => Task.FromResult<string?>(f.Translator[port.Choices == 1 ? "Receipt_ImageHelp" : "Receipt_ChooseFile"]); port.Acknowledge = access.SleepAsync;
        var deferred = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var running = Start(f, access, port, origin, deferred);
        await ReachedAsync(running, deferred.Task); Assert.Equal(1, port.Alerts); Assert.Equal(1, port.Choices); Assert.False(running.IsCompleted);
        await access.UnlockedAsync(); Assert.Equal("file", await running); Assert.Equal(1, port.Alerts); Assert.Equal(2, port.Choices); Assert.Equal(1, port.Files);
    }
    [Fact]
    public async Task Owner_retired_while_initially_queued_never_shows_the_first_source()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); await access.SleepAsync(); using var retired = new CancellationTokenSource(); using var origin = new ReceiptReadOrigin(() => true, retired.Token); var port = new Port();
        var running = Start(f, access, port, origin, new(TaskCreationOptions.RunContinuationsAsynchronously)); Assert.False(running.IsCompleted); Assert.Equal(0, port.Choices);
        retired.Cancel(); Assert.Null(await running.WaitAsync(TimeSpan.FromSeconds(20), Ct)); await access.UnlockedAsync(); Assert.Equal(0, port.Choices + port.Alerts + port.Photos + port.Files);
    }
    [Fact]
    public async Task Owner_retirement_during_native_help_releases_the_result_without_reopening_selection()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var retired = new CancellationTokenSource(); using var origin = new ReceiptReadOrigin(() => true, retired.Token); var port = new Port();
        var acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        port.Select = () => Task.FromResult<string?>(f.Translator["Receipt_ImageHelp"]); port.Acknowledge = () => { shown.TrySetResult(); return acknowledged.Task; };
        var running = Start(f, access, port, origin, new(TaskCreationOptions.RunContinuationsAsynchronously)); await shown.Task.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        retired.Cancel(); Assert.Null(await running.WaitAsync(TimeSpan.FromSeconds(20), Ct)); acknowledged.SetResult();
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await access.RunWhenUnlockedAsync(() => { drained.TrySetResult(); return Task.CompletedTask; });
        await drained.Task.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.Equal(1, port.Choices); Assert.Equal(1, port.Alerts); Assert.Equal(0, port.Photos + port.Files);
    }
    [Fact]
    public async Task Retired_chosen_source_is_not_opened_by_a_later_unlock()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var retired = new CancellationTokenSource(); using var origin = new ReceiptReadOrigin(() => true, retired.Token); var port = new Port();
        port.Select = async () => { await access.SleepAsync(); return f.Translator["Receipt_ChooseFile"]; };
        var deferred = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var running = Start(f, access, port, origin, deferred); await ReachedAsync(running, deferred.Task);
        retired.Cancel(); Assert.Null(await running.WaitAsync(TimeSpan.FromSeconds(20), Ct)); await access.UnlockedAsync(); Assert.Equal(1, port.Choices); Assert.Equal(0, port.Photos + port.Files + port.Alerts);
    }
    [Fact]
    public async Task Original_native_source_failure_propagates_once_without_reasking_or_opening_another_source()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var origin = new ReceiptReadOrigin(() => true); var port = new Port();
        port.Select = () => Task.FromResult<string?>(f.Translator["Receipt_ChooseFile"]); port.Opening = () => Task.FromException(new IOException("Fictitious source failure"));
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<IOException>(() => Start(f, access, port, origin, new(TaskCreationOptions.RunContinuationsAsynchronously)));
        Assert.Equal(1, port.Choices); Assert.Equal(1, port.Files); Assert.Equal(0, port.Photos + port.Alerts); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
