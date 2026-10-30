using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-169: original package/source recovery keeps accepted outcomes separate from security/UI completion.</summary>
[Trait("AT", "AT-169")]
public sealed class BackupRestoreFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Runtime(FlowFixture f, IBackupService backup) : IBackupRestoreRuntime
    {
        internal string? Refuse;
        internal int Safety, Primary, Opened, Work;
        internal bool Consent = true;
        internal Func<Task<bool>>? PendingConsent;
        internal bool Authentication = true;
        internal bool BlockSafetyPath;
        internal string SafetyPath => f.Directory.Combine("safety");
        public Task<bool> ConfirmReplacementAsync() => PendingConsent?.Invoke() ?? Task.FromResult(Consent);
        public Task<bool> AuthenticateAsync() => Task.FromResult(Authentication);
        public async Task CreateSafetyCopyAsync()
        {
            Safety++; if (BlockSafetyPath) await File.WriteAllTextAsync(SafetyPath, "Fictitious owned path blocker", Ct);
            await backup.CreateSafetyCopyAsync(new LocalFolderBackupStorage(SafetyPath), Ct);
        }
        public async Task RestorePrimaryAsync(byte[] package, string? password)
        { Primary++; await f.Services.GetRequiredService<AutoPostProcessor>().RunExclusiveAsync(() => backup.RestorePackageAsync(package, password, Ct), Ct); }
        public Task ApplyDisplayAsync() { Fail("display"); f.Localization.Initialize(); return Task.CompletedTask; }
        public async Task CompleteOnboardingAsync() { Fail("onboarding"); await f.Store.CompleteRestoredOnboardingAsync(Ct); }
        public Task ReloadLockAsync() { Fail("lock"); return Task.CompletedTask; }
        public async Task RefreshFinancialWorkAsync(DateOnly day) { Work++; await f.Services.GetRequiredService<AutoPostProcessor>().RunAsync(day, Ct); Fail("work"); }
        public Task OpenRestoredProfileAsync(bool onboarding) { Fail("open"); Opened++; return Task.CompletedTask; }
        private void Fail(string stage) { if (Refuse == stage) throw new IOException("Fictitious original completion refusal."); }
    }
    private sealed class LaterSource : IBackupSource
    {
        internal bool Refuse;
        public string Name => "fictitious-later-source.txt";
        public Task WriteAsync(Stream destination, CancellationToken cancellationToken) => destination.WriteAsync("Fictitious nonfinancial source"u8.ToArray(), cancellationToken).AsTask();
        public async Task RestoreAsync(Stream source, CancellationToken cancellationToken)
        {
            if (Refuse) throw new BackupException(BackupError.Corrupted, "Fictitious later source refusal after database restore.");
            await source.CopyToAsync(Stream.Null, cancellationToken);
        }
    }
    private static BackupService Service(FlowFixture f, IBackupSource? last = null)
    {
        var sources = f.Services.GetServices<IBackupSource>().ToList();
        sources.Add(new SettingsBackupSource(f.Preferences, "display-settings.json", Vafadar.Localization.LocalizationService.PortableKeys));
        if (last is not null) sources.Add(last);
        return new(sources, f.Services.GetServices<IBackupSummaryProvider>(), new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new(0, 1, 0), "Fictitious device", "Tests"), f.Preferences, f.Time, new());
    }
    private static BackupRestoreSession Session(FlowFixture f, IBackupService backup, Runtime runtime, Func<bool>? available = null, Func<bool>? nativeOwnerCurrent = null) =>
        new(backup, runtime, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, available, nativeOwnerCurrent);
    private static async Task<(byte[] Package, string Expected)> PackageAsync(FlowFixture f, IBackupService backup)
    {
        await f.Store.GetSettingsAsync(Ct); await f.Store.UpdateSettingsAsync(row => row.OnboardingCompleted = true, Ct);
        var account = new Account { Name = "Fictitious complete restored account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 12345 };
        await f.Store.SaveAccountAsync(account, Ct); var expected = await GoalChooserFlowTests.SnapshotAsync(f);
        var package = await backup.CreatePackageAsync(cancellationToken: Ct); var target = account.CopyForEditing(); target.Name = "Fictitious current account"; target.OpeningBalance = 54321;
        await f.Store.SaveAccountAsync(target, Ct); return (package, expected);
    }
    private static async Task PrepareAsync(BackupRestoreSession session, byte[] package, string name = "fictitious-original.zanbak")
    { using var input = new MemoryStream(package, writable: false); await session.PrepareAsync(input, name); Assert.True(session.HasPreview); }

    [Theory]
    [InlineData("en", "display")][InlineData("fa", "display")][InlineData("de", "display")][InlineData("es", "display")][InlineData("fr", "display")][InlineData("it", "display")]
    [InlineData("en", "onboarding")][InlineData("fa", "onboarding")][InlineData("de", "onboarding")][InlineData("es", "onboarding")][InlineData("fr", "onboarding")][InlineData("it", "onboarding")]
    [InlineData("en", "lock")][InlineData("fa", "lock")][InlineData("de", "lock")][InlineData("es", "lock")][InlineData("fr", "lock")][InlineData("it", "lock")]
    [InlineData("en", "work")][InlineData("fa", "work")][InlineData("de", "work")][InlineData("es", "work")][InlineData("fr", "work")][InlineData("it", "work")]
    [InlineData("en", "open")][InlineData("fa", "open")][InlineData("de", "open")][InlineData("es", "open")][InlineData("fr", "open")][InlineData("it", "open")]
    public async Task Complete_original_sources_retire_the_package_before_follow_through_failure_and_retry_never_replaces_again(string language, string stage)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        var backup = Service(f); var (package, expected) = await PackageAsync(f, backup); var runtime = new Runtime(f, backup) { Refuse = stage };
        var session = Session(f, backup, runtime); session.IsOnboarding = true; await PrepareAsync(session, package); await session.RestoreCommand.ExecuteAsync(null);
        Assert.True(session.HasCompleted); Assert.False(session.HasPreview); Assert.False(session.CanRestore); Assert.False(session.CanSelect); Assert.True(session.CanFinish); Assert.False(session.CanLeave);
        Assert.Equal(f.Translator["Backup_RestoredFollowUpFailed"], session.Error); Assert.NotNull(session.Result); Assert.Null(session.PickedName); Assert.Empty(session.Password);
        Vafadar.Zanance.Testing.FinancialSnapshotAssert.EqualAfterPortableRoundTrip(expected, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, runtime.Safety); Assert.Equal(1, runtime.Primary);
        await session.RestoreCommand.ExecuteAsync(null); session.CancelCommand.Execute(null); await PrepareAcceptedNoopAsync(session, package);
        Assert.True(session.HasCompleted); Assert.Equal(1, runtime.Primary); runtime.Refuse = null; await session.FinishCommand.ExecuteAsync(null);
        Assert.Null(session.Error); Assert.True(session.CanLeave); Assert.False(session.CanFinish); Assert.Equal(1, runtime.Opened);
        Assert.Equal(1, runtime.Safety); Assert.Equal(1, runtime.Primary); Vafadar.Zanance.Testing.FinancialSnapshotAssert.EqualAfterPortableRoundTrip(expected, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    private static async Task PrepareAcceptedNoopAsync(BackupRestoreSession session, byte[] package)
    { using var input = new MemoryStream(package, writable: false); await session.PrepareAsync(input, "Must not become a second replacement.zanbak"); }

    [Fact]
    public async Task A_later_source_BackupException_after_database_replacement_is_uncertain_not_a_prewrite_validation_error()
    {
        using var f = new FlowFixture(); var last = new LaterSource(); var backup = Service(f, last); var (package, expected) = await PackageAsync(f, backup);
        var runtime = new Runtime(f, backup); var session = Session(f, backup, runtime); await PrepareAsync(session, package); last.Refuse = true;
        await session.RestoreCommand.ExecuteAsync(null); Assert.False(session.HasCompleted); Assert.False(session.HasPreview); Assert.False(session.CanRestore); Assert.True(session.NeedsReview);
        Assert.Equal(f.Translator["Backup_Error_RestoreFailed"], session.Error); Vafadar.Zanance.Testing.FinancialSnapshotAssert.EqualAfterPortableRoundTrip(expected, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, runtime.Primary);
        await session.RestoreCommand.ExecuteAsync(null); Assert.Equal(1, runtime.Primary); last.Refuse = false; await session.CheckCommand.ExecuteAsync(null);
        Assert.True(session.CanRestore); await session.RestoreCommand.ExecuteAsync(null); Assert.True(session.HasCompleted); Assert.Equal(2, runtime.Primary); Assert.Equal(1, runtime.Opened);
    }

    [Fact]
    public async Task Real_safety_path_failure_keeps_original_live_rows_and_review_without_starting_primary_restore()
    {
        using var f = new FlowFixture(); var backup = Service(f); var (package, _) = await PackageAsync(f, backup);
        var runtime = new Runtime(f, backup) { BlockSafetyPath = true }; var session = Session(f, backup, runtime); await PrepareAsync(session, package);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var preview = session.PreviewText; await session.RestoreCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Backup_Error_SafetyCopyFailed"], session.Error); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(preview, session.PreviewText); Assert.True(session.CanRestore); Assert.False(session.HasCompleted); Assert.Equal(0, runtime.Primary);
    }

    [Fact]
    public async Task Consent_and_authentication_are_one_busy_lifetime_and_decline_preserves_every_original_row_and_package()
    {
        using var f = new FlowFixture(); var backup = Service(f); var (package, _) = await PackageAsync(f, backup); var runtime = new Runtime(f, backup);
        var consent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); runtime.PendingConsent = () => consent.Task;
        var session = Session(f, backup, runtime); await PrepareAsync(session, package); var before = await GoalChooserFlowTests.SnapshotAsync(f); var preview = session.PreviewText;
        var pending = session.RestoreCommand.ExecuteAsync(null); Assert.True(session.IsBusy); Assert.False(session.CanLeave); Assert.False(session.CanCheck); Assert.False(session.CanSelect);
        session.CancelCommand.Execute(null); consent.SetResult(false); await pending; Assert.Equal(preview, session.PreviewText); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        runtime.PendingConsent = null; runtime.Authentication = false; await session.RestoreCommand.ExecuteAsync(null); Assert.Equal(0, runtime.Primary); Assert.Equal(0, runtime.Safety);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(session.HasPreview);
    }

    [Fact]
    public async Task Outer_native_read_availability_disables_replacement_without_clearing_the_successful_original_preview()
    {
        using var f = new FlowFixture(); var backup = Service(f); var (package, _) = await PackageAsync(f, backup); var runtime = new Runtime(f, backup); var available = false;
        var session = Session(f, backup, runtime, () => available, nativeOwnerCurrent: () => true); await PrepareAsync(session, package); Assert.False(session.CanRestore); Assert.False(session.CanLeave);
        available = true; session.OwnerStateChanged(); Assert.True(session.CanRestore); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        using var failed = new RefusedReadStream(); await session.PrepareAsync(failed, "Failed new selection.zanbak");
        Assert.Equal("fictitious-original.zanbak", session.PickedName); Assert.True(session.HasPreview); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Actual_automatic_posting_before_follow_through_failure_is_not_duplicated_by_open_profile_retry()
    {
        using var f = new FlowFixture(); var backup = Service(f); await f.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "Fictitious automatic-post source", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct); var day = DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime);
        await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(new() { Name = "Fictitious once original", AccountId = account.Id,
            Amount = 1234, AutoPost = true, AutoPostFrom = day, Rule = new() { Start = day, Frequency = Vafadar.Zanance.Core.Plans.Frequency.Once } }, Ct);
        var package = await backup.CreatePackageAsync(cancellationToken: Ct); var runtime = new Runtime(f, backup) { Refuse = "work" };
        var session = Session(f, backup, runtime); await PrepareAsync(session, package); await session.RestoreCommand.ExecuteAsync(null);
        Assert.True(session.HasCompleted); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)); var posted = await GoalChooserFlowTests.SnapshotAsync(f);
        runtime.Refuse = null; await session.FinishCommand.ExecuteAsync(null);
        Assert.Equal(posted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, runtime.Primary); Assert.Equal(2, runtime.Work);
    }

    private sealed class RefusedReadStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromException<int>(new IOException("Fictitious selected-file read failure."));
    }
}
