using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Exercises original foreground coordination with actual isolated SQLite/packages and explicit key effects.</summary>
public sealed class AutomaticBackupFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-281")]
    public async Task Foreground_checks_preserve_sqlite_and_device_preferences_and_restore_original_data_explicitly()
    {
        using var f = new Fixture();
        var account = new Account { Name = "Fictitious automatic account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 12345 };
        await f.Flow.Store.SaveAccountAsync(account, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow);
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => f.Coordinator.RunAsync(Ct)));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
        var file = Assert.Single(await f.Storage.ListAsync(Ct)); Assert.NotNull(f.Coordinator.LastBackupAt);
        Assert.Null(f.Backup.LastBackupAt); Assert.False(f.Coordinator.HasFailure);
        account.Name = "Later explicit edit"; await f.Flow.Store.SaveAccountAsync(account, Ct);
        await using var input = await f.Storage.OpenReadAsync(file.Id, Ct); using var bytes = new MemoryStream(); await input.CopyToAsync(bytes, Ct);
        await f.Backup.RestorePackageAsync(bytes.ToArray(), cancellationToken: Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Disabled_and_unavailable_foreground_never_capture_or_create_and_weekly_waits_until_due()
    {
        using var f = new Fixture();
        f.Ready = false; await f.Coordinator.RunAsync(Ct); Assert.Equal(0, f.Captures);
        f.Ready = true; f.Coordinator.IntervalDays = 0; await f.Coordinator.RunAsync(Ct); Assert.Equal(0, f.Captures);
        f.Coordinator.IntervalDays = 7; await f.Coordinator.RunAsync(Ct);
        f.Flow.Time.Now += TimeSpan.FromDays(6); await f.Coordinator.RunAsync(Ct); Assert.Single(await f.Storage.ListAsync(Ct));
        f.Flow.Time.Now += TimeSpan.FromDays(1); await f.Coordinator.RunAsync(Ct); Assert.Equal(2, (await f.Storage.ListAsync(Ct)).Count);
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Failed_capture_is_profile_local_and_next_foreground_retries_without_changing_data()
    {
        using var f = new Fixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow);
        f.RefuseCapture = true; await f.Coordinator.RunAsync(Ct);
        Assert.True(f.Coordinator.HasFailure); Assert.Null(f.Coordinator.LastBackupAt); Assert.Empty(await f.Storage.ListAsync(Ct));
        f.Set = "other"; Assert.False(f.Coordinator.HasFailure); f.Set = null;
        f.RefuseCapture = false; await f.Coordinator.RunAsync(Ct);
        Assert.False(f.Coordinator.HasFailure); Assert.Single(await f.Storage.ListAsync(Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly FlowFixture Flow = new();
        private readonly KeyPort keys = new();
        internal readonly BackupService Backup;
        internal readonly AutomaticBackupCoordinator Coordinator;
        internal readonly LocalFolderBackupStorage Storage;
        internal bool Ready = true, RefuseCapture;
        internal string? Set;
        internal int Captures;
        internal Fixture()
        {
            Storage = new(Flow.Directory.Combine("automatic"));
            var protection = new Protection(new DeviceKeySafetyCopyProtection(Guid.NewGuid(), Guid.NewGuid(), keys, () => { }));
            var options = new BackupOptions { FileSet = () => Set };
            Backup = new(Flow.Services.GetServices<IBackupSource>(),
                new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test", "Tests"),
                Flow.Preferences, Flow.Time, options, safetyProtection: protection.Inner, automaticProtection: protection);
            Coordinator = new(Backup, Flow.Preferences, options, Storage, () =>
            {
                Captures++;
                if (RefuseCapture) throw new IOException("Fictitious capture failure.");
                var original = Flow.Services.GetRequiredService<LocalDatabaseLocation<Vafadar.Zanance.Data.ZananceDbContext>>().Path;
                return () =>
                {
                    if (Flow.Services.GetRequiredService<LocalDatabaseLocation<Vafadar.Zanance.Data.ZananceDbContext>>().Path != original)
                        throw new IOException("Fictitious generation changed.");
                };
            }, () => Ready, action => action());
        }
        public void Dispose() { keys.Dispose(); Flow.Dispose(); }
    }

    private sealed class Protection(DeviceKeySafetyCopyProtection inner) : IAutomaticLocalBackupProtection
    {
        internal DeviceKeySafetyCopyProtection Inner => inner;
        public Task<byte[]> ProtectAutomaticAsync(ReadOnlyMemory<byte> package, Action validate, CancellationToken ct)
        { validate(); return inner.ProtectAsync(package, ct); }
        public Task CompleteAutomaticAsync(BackupFileInfo file, Action validate, CancellationToken ct)
        { validate(); return Task.CompletedTask; }
    }

    private sealed class KeyPort : IDatabaseGenerationKeyStore, IDisposable
    {
        private readonly Dictionary<DatabaseKeyBinding, byte[]> keys = [];
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { keys.Add(binding, RandomNumberGenerator.GetBytes(32)); return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.FromResult(keys[binding].ToArray());
        public void Dispose() { foreach (var key in keys.Values) CryptographicOperations.ZeroMemory(key); }
    }
}
