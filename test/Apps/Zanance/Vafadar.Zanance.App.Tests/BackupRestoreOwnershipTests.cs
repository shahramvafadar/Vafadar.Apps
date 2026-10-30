using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original restore continuations against actual isolated packages and native-owner retirement.</summary>
public sealed class BackupRestoreOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Owner { internal bool Current = true; }
    private sealed class Runtime(IBackupService backup, Owner owner, string retire) : IBackupRestoreRuntime
    {
        internal int Safety, Primary, Opened, Display, Auth;
        internal Func<Action>? SourceGuard;
        internal Func<Task<bool>>? PendingConsent;
        internal Func<Task<bool>>? PendingAuthentication;
        public Action CaptureSourceValidation() => SourceGuard?.Invoke() ?? (static () => { });
        public Task<bool> ConfirmReplacementAsync() { if (PendingConsent is not null) return PendingConsent(); if (retire == "consent") owner.Current = false; return Task.FromResult(true); }
        public Task<bool> AuthenticateAsync() { Auth++; if (PendingAuthentication is not null) return PendingAuthentication(); if (retire == "auth") owner.Current = false; return Task.FromResult(true); }
        public Task CreateSafetyCopyAsync() { Safety++; if (retire == "safety") owner.Current = false; return Task.CompletedTask; }
        public async Task RestorePrimaryAsync(byte[] package, string? password)
        { Primary++; await backup.RestorePackageAsync(package, password, Ct); if (retire == "accepted") owner.Current = false;
            if (retire == "uncertain-retired") { owner.Current = false; throw new IOException("Fictitious later-source failure after actual replacement."); } }
        public Task ApplyDisplayAsync() { Display++; return Task.CompletedTask; }
        public Task CompleteOnboardingAsync() => Task.CompletedTask;
        public Task ReloadLockAsync() => Task.CompletedTask;
        public Task RefreshFinancialWorkAsync(DateOnly day) => Task.CompletedTask;
        public Task OpenRestoredProfileAsync(bool onboarding) { Opened++; return Task.CompletedTask; }
    }
    private static BackupService Service(FlowFixture f) => new(f.Services.GetServices<IBackupSource>(),
        f.Services.GetServices<IBackupSummaryProvider>(), new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new(0, 1, 0), "Fictitious D654 device", "Tests"), f.Preferences, f.Time, new());
    [Theory]
    [InlineData("consent")][InlineData("auth")][InlineData("safety")]
    public async Task Retired_original_native_await_cannot_begin_primary_source_replacement(string stage)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "D654 original source", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 };
        await f.Store.SaveAccountAsync(account, Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        account.Name = "D654 current live metadata"; await f.Store.SaveAccountAsync(account, Ct);
        var owner = new Owner(); var runtime = new Runtime(backup, owner, stage);
        var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, () => owner.Current);
        using var stream = new MemoryStream(package, writable: false); await session.PrepareAsync(stream, "D654-original.zanbak"); Assert.True(session.HasPreview);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.RestoreCommand.ExecuteAsync(null);
        Assert.Equal(0, runtime.Primary); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(session.HasCompleted); Assert.Equal(0, runtime.Opened);
    }
    [Fact]
    public async Task Accepted_primary_result_is_kept_but_retired_native_completion_cannot_open_another_root()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        var owner = new Owner(); var runtime = new Runtime(backup, owner, "accepted");
        var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, () => owner.Current);
        using var stream = new MemoryStream(package, writable: false); await session.PrepareAsync(stream, "D654-original-accepted.zanbak");
        await session.RestoreCommand.ExecuteAsync(null); Assert.True(session.HasCompleted); Assert.NotNull(session.Result); Assert.Null(session.PickedName);
        Assert.Equal(1, runtime.Primary); Assert.Equal(0, runtime.Opened); Assert.Equal(0, runtime.Display);
        await session.RestoreCommand.ExecuteAsync(null); Assert.Equal(1, runtime.Primary);
        owner.Current = true; session.SetPresentationActive(true); await session.FinishCommand.ExecuteAsync(null);
        Assert.Equal(1, runtime.Primary); Assert.Equal(1, runtime.Opened); Assert.True(session.CanLeave);
    }
    [Fact]
    public async Task A_replaced_target_publication_is_refused_even_when_the_original_path_returns_before_the_picker()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        var location = f.Services.GetRequiredService<Vafadar.Data.LocalDatabaseLocation<ZananceDbContext>>(); var path = location.Path;
        var owner = new Owner(); var runtime = new Runtime(backup, owner, "none") { SourceGuard = location.CaptureValidation };
        var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time);
        var original = session.CapturePreparationOwner();
        using (var maintenance = await location.Access.AcquireExclusiveAsync(Ct))
        { location.MoveTo(f.Directory.Combine("D654-other-unopened.db"), maintenance); location.MoveTo(path, maintenance); }
        using var stream = new MemoryStream(package, writable: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.PrepareAsync(stream, "Must not borrow returned path.zanbak", original));
        Assert.Null(session.PickedName); Assert.False(session.HasPreview); Assert.Equal(0, runtime.Primary);
        Assert.False(File.Exists(f.Directory.Combine("D654-other-unopened.db")));
    }
    private sealed class HeldRead(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationToken ActualToken { get; private set; }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken ct)
        { ActualToken = ct; Entered.TrySetResult(); await Release.Task.WaitAsync(ct); await base.CopyToAsync(destination, bufferSize, ct); }
    }
    [Fact]
    public async Task Actual_selected_stream_cancels_on_departure_and_an_old_owner_cannot_borrow_native_return()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        var runtime = new Runtime(backup, new Owner(), "none"); var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time);
        var original = session.CapturePreparationOwner(); using var stream = new HeldRead(package);
        var pending = session.PrepareAsync(stream, "D654 held original selection.zanbak", original); await stream.Entered.Task.WaitAsync(Ct);
        session.Deactivate(); await pending; Assert.True(stream.ActualToken.IsCancellationRequested); Assert.Null(session.PickedName); Assert.False(session.HasPreview);
        session.SetPresentationActive(true); using var old = new MemoryStream(package, writable: false);
        await Assert.ThrowsAsync<OperationCanceledException>(() => session.PrepareAsync(old, "Old owner after return.zanbak", original));
        Assert.Null(session.PickedName); Assert.Equal(0, runtime.Primary);
    }
    [Fact]
    public async Task Terminal_root_cannot_reactivate_or_publish_a_selected_original_package()
    {
        using var f = new FlowFixture(); var backup = Service(f); var runtime = new Runtime(backup, new Owner(), "none");
        var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time);
        session.RetireRoot(); session.SetPresentationActive(true); using var stream = new MemoryStream([1, 2, 3]);
        await session.PrepareAsync(stream, "Refused old root.zanbak"); Assert.Null(session.PickedName); Assert.False(session.CanSelect); Assert.Equal(0, runtime.Primary);
    }
    [Fact]
    public async Task Changed_original_password_while_native_consent_is_pending_cannot_replace_a_source()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        var runtime = new Runtime(backup, new Owner(), "none"); var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time);
        using var stream = new MemoryStream(package, writable: false); await session.PrepareAsync(stream, "D654 literal consent.zanbak");
        runtime.PendingConsent = () => { session.Password = "Fictitious changed input"; return Task.FromResult(true); };
        await session.RestoreCommand.ExecuteAsync(null); Assert.Equal(0, runtime.Primary); Assert.Equal(0, runtime.Safety); Assert.False(session.HasCompleted);
    }
    [Fact]
    public async Task Protected_receiver_rechecks_original_native_owner_after_its_actual_exclusive_queue()
    {
        using var f = new FlowFixture(); var location = new Vafadar.Data.LocalDatabaseLocation<ZananceDbContext>(f.Directory.Combine("D654-unopened-receiver.db"), protectionRequired: true);
        // This negative boundary must refuse before the protected host is entered at all.
        var receiver = new ProtectedBackupRestoreReceiver(location, null!); var retired = false;
        using var original = BackupRestorePublication.Enter(() => { if (retired) throw new OperationCanceledException("Fictitious retired queued native owner."); });
        var held = await location.Access.AcquireExclusiveAsync(Ct);
        var manifest = new BackupManifest { FormatVersion = 1, AppId = "pro.vafadar.zanance", AppVersion = "0.1", CreatedAt = DateTimeOffset.UnixEpoch, Entries = [] };
        var pending = receiver.RestoreAsync(manifest, new Dictionary<string, ReadOnlyMemory<byte>>(), Ct); Assert.False(pending.IsCompleted);
        retired = true; held.Dispose(); await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
        using var drained = await location.Access.AcquireExclusiveAsync(Ct); location.Access.RequireDrainedOwner(drained);
        Assert.False(File.Exists(location.Path)); Assert.False(location.IsProtected);
    }
    [Fact]
    public async Task An_uncertain_retired_primary_failure_requires_fresh_review_after_actual_return()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        var owner = new Owner(); var runtime = new Runtime(backup, owner, "uncertain-retired");
        var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, () => owner.Current);
        using var stream = new MemoryStream(package, writable: false); await session.PrepareAsync(stream, "D654 uncertain original.zanbak");
        await session.RestoreCommand.ExecuteAsync(null); Assert.False(session.HasCompleted); Assert.False(session.HasPreview);
        owner.Current = true; session.SetPresentationActive(true); Assert.True(session.NeedsReview); Assert.False(session.CanRestore);
        Assert.Equal(f.Translator["Backup_Error_RestoreFailed"], session.Error);
        await session.RestoreCommand.ExecuteAsync(null); Assert.Equal(1, runtime.Primary);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Owned_authentication_wait_is_explicit_but_never_revives_a_departed_original_frame(bool depart)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var backup = Service(f); var package = await backup.CreatePackageAsync(cancellationToken: Ct);
        var authentication = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new Runtime(backup, new Owner(), "none") { PendingAuthentication = () => authentication.Task };
        var session = new BackupRestoreSession(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time);
        using var stream = new MemoryStream(package, writable: false); await session.PrepareAsync(stream, "D654 owned authentication.zanbak");
        var pending = session.RestoreCommand.ExecuteAsync(null); Assert.True(session.IsAuthenticating); Assert.True(session.IsBusy); Assert.False(session.CanLeave);
        if (depart) session.Deactivate(); authentication.SetResult(depart); await pending;
        Assert.False(session.IsAuthenticating); Assert.False(session.IsBusy); Assert.Equal(0, runtime.Primary); Assert.Equal(0, runtime.Safety);
        Assert.Equal(!depart, session.HasPreview); Assert.Equal(!depart, session.CanRestore);
    }
}
