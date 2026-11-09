using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Documents;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original receipt startup and failure publications cannot adopt a replacement native page or leak Busy.</summary>
public sealed class ReceiptStartupPublicationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Receipt_startup_busy_retirement_never_captures_the_replacement_page_or_reads()
    {
        using var f = new FlowFixture(); var s = Prepare(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Session.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ReceiptReadingSession.IsBusy) && s.Session.IsBusy) s.Native.Epoch++; };
        await s.Session.PickAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, s.Factory.Acquired); Assert.Equal(1, s.Native.Captured); Assert.Equal(1, s.Native.Released);
        Assert.False(s.Session.IsBusy); Assert.Equal(0, s.Native.Picks); Assert.Equal(0, s.Native.Alerts);
    }

    [Fact]
    public async Task Receipt_startup_busy_subscriber_failure_releases_the_original_lifetime()
    {
        using var f = new FlowFixture(); var s = Prepare(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Session.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ReceiptReadingSession.IsBusy) && s.Session.IsBusy) throw new IOException("D952 Busy publication"); };
        var error = await Record.ExceptionAsync(() => s.Session.PickAsync());
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Session.IsBusy); Assert.Null(error); Assert.Equal(1, s.Native.Released);
        Assert.Equal(0, s.Factory.Acquired); Assert.Equal(0, s.Native.Picks); Assert.Equal(1, s.Native.Alerts);
    }

    [Fact]
    public async Task Receipt_startup_cleared_error_retirement_never_reads_a_replacement_page()
    {
        using var f = new FlowFixture(); var s = Prepare(f);
        await s.Session.PickAsync(); Assert.NotNull(s.Session.Error); Assert.Equal(1, s.Factory.Acquired);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); s.Factory.Acquired = 0; s.Native.Alerts = 0;
        s.Session.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ReceiptReadingSession.Error) && s.Session.Error is null) s.Native.Epoch++; };
        await s.Session.PickAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, s.Factory.Acquired); Assert.Equal(0, s.Native.Picks); Assert.Equal(0, s.Native.Alerts);
        Assert.False(s.Session.IsBusy); Assert.Equal(2, s.Native.Released);
    }

    [Fact]
    public async Task Receipt_startup_failure_error_retirement_never_opens_a_stale_native_alert()
    {
        using var f = new FlowFixture(); var s = Prepare(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Session.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ReceiptReadingSession.Error) && s.Session.Error is not null) s.Native.Epoch++; };
        await s.Session.PickAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(1, s.Factory.Acquired); Assert.Equal(0, s.Native.Alerts); Assert.Equal(0, s.Native.Picks);
        Assert.False(s.Session.IsBusy); Assert.Equal(1, s.Native.Released);
    }

    [Fact]
    public async Task Receipt_startup_current_owner_reaches_original_source_failure_and_one_alert()
    {
        using var f = new FlowFixture(); var s = Prepare(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await s.Session.PickAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(1, s.Factory.Acquired); Assert.Equal(1, s.Native.Alerts); Assert.NotNull(s.Session.Error);
        Assert.False(s.Session.IsBusy); Assert.Equal(1, s.Native.Released); Assert.Equal(0, s.Native.Picks);
    }

    /// <summary>Uses the original migrated empty file without an account, attachment, OCR or financial setup.</summary>
    private static Prepared Prepare(FlowFixture f)
    {
        var factory = new Probe(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var native = new Native();
        return new(new ReceiptReadingSession(new ReceiptWorkStore(factory, new InactiveAccess()), native, f.Translator, f.Time), factory, native);
    }
    private sealed record Prepared(ReceiptReadingSession Session, Probe Factory, Native Native);
    private sealed class InactiveAccess : ICommercialWriteAccessSource
    { public CommercialWriteAccess Capture(string path) => CommercialWriteAccess.Inactive; }

    /// <summary>Counts and disposes the actual factory context before opening the reader or any native source.</summary>
    private sealed class Probe(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal int Acquired;
        public ZananceDbContext CreateDbContext() => Refuse(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken token = default) => Refuse(await original.CreateDbContextAsync(token));
        private ZananceDbContext Refuse(ZananceDbContext db)
        { Acquired++; db.Dispose(); throw new IOException("D952 original source sentinel"); }
    }

    /// <summary>Captures one immutable native epoch and records only attempted effects, without opening external UI.</summary>
    private sealed class Native : IReceiptRuntime
    {
        internal int Epoch, Captured, Released, Picks, Alerts;
        public ReceiptReadOrigin CaptureOrigin()
        { Captured++; var epoch = Epoch; return new(() => epoch == Epoch, release: () => Released++); }
        public Task<ReceiptPickedFile?> PickAsync() { Picks++; return Task.FromResult<ReceiptPickedFile?>(null); }
        public Task RunWhenUnlockedAsync(Func<Task> action) => action();
        public Task<TextLayoutResult> ReadAsync(byte[] data, string contentType) => throw new InvalidOperationException("D952 recognition must remain unreachable");
        public Task AlertAsync(string title, string message, string cancel) { Alerts++; return Task.CompletedTask; }
        public Task OpenEditorAsync(ReceiptDraft draft) => throw new InvalidOperationException("D952 editor must remain unreachable");
    }
}
