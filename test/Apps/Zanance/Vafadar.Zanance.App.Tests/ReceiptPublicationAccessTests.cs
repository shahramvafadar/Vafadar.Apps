using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Documents;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Original receipt source and actual access drain across delayed native results, never a financial writer.</summary>
public sealed class ReceiptPublicationAccessTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class InactiveAccess : ICommercialWriteAccessSource
    { public CommercialWriteAccess Capture(string path) => CommercialWriteAccess.Inactive; }
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken token = default) => Current.CreateDbContextAsync(token);
    }
    private sealed class Native(AppLockService gate) : IReceiptRuntime, IDisposable
    {
        internal int Reads, Picks, Opens, Alerts, OpenedWhileLocked, AlertedWhileLocked;
        internal bool OwnerCurrent = true;
        internal readonly CancellationTokenSource Retired = new();
        internal readonly TaskCompletionSource Deferred = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Queued = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly List<string> Order = [];
        internal TextLayoutResult Document = new("FICTITIOUS ACCESS SHOP\nTOTAL EUR 24.50");
        internal Func<Task>? DuringRead, DuringAlert;
        public ReceiptReadOrigin CaptureOrigin() => new(() => OwnerCurrent, Retired.Token);
        public Task<ReceiptPickedFile?> PickAsync() { Picks++; return Task.FromResult<ReceiptPickedFile?>(new("Fictitious compact.jpg", "image/jpeg", [1], [2])); }
        public async Task RunWhenUnlockedAsync(Func<Task> action) { await gate.RunWhenUnlockedAsync(action); Queued.TrySetResult(); }
        public async Task<bool> QueueWhenUnlockedAsync(Func<Task> action) { await RunWhenUnlockedAsync(action); return true; }
        public bool RetainPublicationIfLocked(Func<Task> continuation)
        {
            var retained = gate.RetainCurrentDestinationIfLocked(continuation);
            if (retained) Deferred.TrySetResult(); return retained;
        }
        public async Task<TextLayoutResult> ReadAsync(byte[] data, string contentType)
        { Reads++; if (DuringRead is not null) await DuringRead(); return Document; }
        public async Task AlertAsync(string title, string message, string cancel)
        { Alerts++; if (gate.IsLocked) AlertedWhileLocked++; if (DuringAlert is not null) { var once = DuringAlert; DuringAlert = null; await once(); } }
        public Task OpenEditorAsync(ReceiptDraft draft)
        { Opens++; if (gate.IsLocked) OpenedWhileLocked++; Order.Add("receipt"); return Task.CompletedTask; }
        public void Dispose() => Retired.Dispose();
    }
    private static async Task<AppLockService> AccessAsync(FlowFixture f)
    {
        var settings = await f.Store.GetSettingsAsync(Ct); settings.AppLockEnabled = true;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var access = AppLockTests.Create(f).Lock; await access.StartAsync(); await access.UnlockedAsync(); return access;
    }
    private static ReceiptReadingSession Session(FlowFixture f, Native native, Factory? factory = null) =>
        new(new ReceiptWorkStore(factory ?? new(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()), new InactiveAccess()), native, f.Translator, f.Time);
    private static async Task ReachedAsync(Task running, Task checkpoint) =>
        await Task.WhenAny(running, checkpoint).WaitAsync(TimeSpan.FromSeconds(20), Ct);

    [Fact]
    public async Task Cover_during_completed_recognition_retains_publication_ahead_of_later_input_without_read_replay()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var native = new Native(access);
        native.DuringRead = async () => { await access.SleepAsync(); await access.RunWhenUnlockedAsync(() => { native.Order.Add("later"); return Task.CompletedTask; }); };
        var session = Session(f, native); var before = await GoalChooserFlowTests.SnapshotAsync(f); var running = session.PickAsync();
        await ReachedAsync(running, native.Deferred.Task);
        Assert.True(access.IsLocked); Assert.Equal(0, native.Opens); Assert.Equal(0, native.OpenedWhileLocked); Assert.True(session.IsBusy); Assert.False(running.IsCompleted);
        await access.UnlockedAsync(); await running.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.Equal(1, native.Reads); Assert.Equal(1, native.Opens); Assert.Equal(new[] { "receipt", "later" }, native.Order);
        Assert.False(session.IsBusy); Assert.Null(session.Error); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Reader_failure_after_cover_defers_only_safe_feedback_until_unlock()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var native = new Native(access);
        native.DuringRead = async () => { await access.SleepAsync(); throw new IOException("Fictitious private reader detail"); };
        var session = Session(f, native); var before = await GoalChooserFlowTests.SnapshotAsync(f); var running = session.PickAsync();
        await ReachedAsync(running, native.Deferred.Task);
        Assert.Equal(0, native.Alerts); Assert.True(session.IsBusy); Assert.False(running.IsCompleted);
        await access.UnlockedAsync(); await running.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.Equal(1, native.Reads); Assert.Equal(1, native.Alerts); Assert.Equal(0, native.AlertedWhileLocked); Assert.Equal(0, native.Opens);
        Assert.DoesNotContain("private", session.Error ?? ""); Assert.False(session.IsBusy); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Cover_after_answered_empty_notice_retains_only_editor_publication_without_notice_or_OCR_replay()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var native = new Native(access) { Document = new(null) };
        native.DuringAlert = access.SleepAsync;
        var session = Session(f, native); var before = await GoalChooserFlowTests.SnapshotAsync(f); var running = session.PickAsync();
        await ReachedAsync(running, native.Deferred.Task);
        Assert.Equal(1, native.Alerts); Assert.Equal(0, native.Opens); Assert.True(session.IsBusy);
        await access.UnlockedAsync(); await running.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.Equal(1, native.Reads); Assert.Equal(1, native.Alerts); Assert.Equal(0, native.OpenedWhileLocked); Assert.Equal(1, native.Opens);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Retired_original_presentation_during_read_never_adopts_another_page_or_shows_late_failure(bool fails)
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var native = new Native(access);
        native.DuringRead = () => { native.OwnerCurrent = false; return fails ? Task.FromException(new IOException("Fictitious retired reader")) : Task.CompletedTask; };
        var session = Session(f, native); var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.PickAsync();
        Assert.Equal(1, native.Reads); Assert.Equal(0, native.Opens); Assert.Equal(0, native.Alerts); Assert.Null(session.Error); Assert.False(session.IsBusy);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Retired_owner_cancels_waiting_publication_and_later_unlock_cannot_replay_it()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); using var native = new Native(access) { DuringRead = access.SleepAsync };
        var session = Session(f, native); var running = session.PickAsync(); await ReachedAsync(running, native.Deferred.Task);
        Assert.Equal(0, native.Opens); Assert.True(session.IsBusy);
        native.OwnerCurrent = false; native.Retired.Cancel(); await running.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.False(session.IsBusy); await access.UnlockedAsync(); Assert.Equal(0, native.Opens); Assert.Equal(0, native.Alerts); Assert.Equal(1, native.Reads);
    }
    [Fact]
    public async Task Changed_original_file_after_retained_OCR_is_revalidated_without_repeating_the_native_read()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var access = await AccessAsync(f); using var native = new Native(access) { DuringRead = access.SleepAsync };
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var session = Session(f, native, factory);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var otherBefore = await GoalChooserFlowTests.SnapshotAsync(other); var running = session.PickAsync();
        await ReachedAsync(running, native.Deferred.Task); Assert.Equal(0, native.Opens); factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await access.UnlockedAsync(); await running.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.Equal(1, native.Reads); Assert.Equal(0, native.Opens); Assert.Equal(1, native.Alerts); Assert.NotNull(session.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(otherBefore, await GoalChooserFlowTests.SnapshotAsync(other));
    }
    [Fact]
    public async Task Already_covered_queued_read_keeps_busy_and_owner_retirement_prevents_first_recognition()
    {
        using var f = new FlowFixture(); var access = await AccessAsync(f); await access.SleepAsync(); using var native = new Native(access);
        var session = Session(f, native); var running = session.PickAsync(); await ReachedAsync(running, native.Queued.Task);
        Assert.True(session.IsBusy); Assert.False(running.IsCompleted); Assert.Equal(0, native.Reads);
        native.OwnerCurrent = false; native.Retired.Cancel(); await running.WaitAsync(TimeSpan.FromSeconds(20), Ct); await access.UnlockedAsync();
        Assert.Equal(0, native.Reads); Assert.Equal(0, native.Opens); Assert.Equal(0, native.Alerts); Assert.False(session.IsBusy);
    }
}
