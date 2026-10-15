using System.Security.Cryptography;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Backup.Security;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Backup.Tests;

/// <summary>Checks unattended package serialization, real device-envelope crypto and original bounded validation.</summary>
public sealed class AutomaticLocalBackupTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-281")]
    public async Task Device_copy_round_trips_without_portable_timestamp_and_reader_reentrancy_deadlock()
    {
        using var f = new Fixture();
        var file = await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.NotNull(file); Assert.True(AutomaticBackupFileName.Matches(file.FileName, null));
        Assert.Null(f.Service.LastBackupAt); Assert.Equal(f.Time.GetUtcNow(), f.Service.LastAutomaticBackupAt);
        Assert.Equal(1, f.Protection.Completed); Assert.True(f.Protection.Borrowed.Span.IndexOfAnyExcept((byte)0) < 0);
        var bytes = await f.ReadAsync(file);
        Assert.True(DeviceSafetyEncryption.IsEncrypted(bytes));
        Assert.Empty(await f.Service.DiscoverBackupsAsync(f.Storage, Ct));
        f.Source.Content = "later fictional ledger";
        await f.Service.RestorePackageAsync(bytes, cancellationToken: Ct);
        Assert.Equal("original fictional ledger", f.Source.Content);
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Concurrent_due_checks_create_only_one_complete_copy()
    {
        using var f = new Fixture();
        var result = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct)));
        Assert.Single(result, file => file is not null); Assert.Single(await f.Storage.ListAsync(Ct));
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Manual_export_does_not_postpone_automatic_copy_and_each_profile_has_its_own_clock()
    {
        using var f = new Fixture();
        await f.Service.CreateBackupAsync(f.Storage, cancellationToken: Ct);
        Assert.NotNull(await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        Assert.Null(await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        f.Set = "other"; Assert.Null(f.Service.LastAutomaticBackupAt);
        var other = await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct);
        Assert.NotNull(other); Assert.True(AutomaticBackupFileName.Matches(other.FileName, "other"));
        f.Set = null; Assert.NotNull(f.Service.LastAutomaticBackupAt);
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Long_absence_creates_one_new_copy_and_backward_clock_is_recovered_once()
    {
        using var f = new Fixture();
        await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct);
        f.Time.Advance(TimeSpan.FromDays(5));
        Assert.NotNull(await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        Assert.Equal(2, (await f.Storage.ListAsync(Ct)).Count);
        f.Time.AdjustTime(f.Time.GetUtcNow().AddDays(-2));
        Assert.NotNull(await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        Assert.Null(await f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Disabled_creation_does_not_read_live_source_or_require_protection()
    {
        using var f = new Fixture(withProtection: false);
        Assert.Null(await f.Service.CreateWhenDueAsync(f.Storage, null, () => throw new IOException("Unexpected capture"), Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        Assert.Empty(await f.Storage.ListAsync(Ct)); Assert.Null(f.Service.LastAutomaticBackupAt);
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Profile_change_inside_protection_refuses_upload_and_does_not_advance_either_clock()
    {
        using var f = new Fixture(); f.Protection.BeforeProtect = () => f.Set = "changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        Assert.Empty(await f.Storage.ListAsync(Ct)); Assert.Null(f.Service.LastAutomaticBackupAt);
        f.Set = null; Assert.Null(f.Service.LastAutomaticBackupAt);
        Assert.True(f.Protection.Borrowed.Span.IndexOfAnyExcept((byte)0) < 0);
    }

    [Fact, Trait("AT", "AT-281")]
    public async Task Failed_post_upload_qualification_keeps_copy_but_never_records_false_success()
    {
        using var f = new Fixture(); f.Protection.FailCompletion = true;
        await Assert.ThrowsAsync<IOException>(() => f.Service.CreateWhenDueAsync(f.Storage, TimeSpan.FromDays(1), () => { }, Ct));
        var file = Assert.Single(await f.Storage.ListAsync(Ct));
        Assert.True(DeviceSafetyEncryption.IsEncrypted(await f.ReadAsync(file)));
        Assert.Null(f.Service.LastAutomaticBackupAt); Assert.Null(f.Service.LastBackupAt);
    }

    [Theory, Trait("AT", "AT-281")]
    [InlineData("automatic-main-00000000000000000000000000000001.vbs", null, true)]
    [InlineData("automatic-main-00000000000000000000000000000001.vbs.tmp", null, false)]
    [InlineData("safety-00000000000000000000000000000001.vbs", null, false)]
    [InlineData("automatic-main-00000000000000000000000000000001.vbs", "main", false)]
    [InlineData("automatic-set-other-00000000000000000000000000000001.vbs", "other", true)]
    public void Discovery_names_separate_profile_safety_and_incomplete_files(string file, string? set, bool expected) =>
        Assert.Equal(expected, AutomaticBackupFileName.Matches(file, set));

    [Fact, Trait("AT", "AT-281")]
    public async Task Cloud_storage_and_cancelled_work_are_refused_before_any_source_or_key_effect()
    {
        using var f = new Fixture();
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service.CreateWhenDueAsync(new ForbiddenStorage(),
            TimeSpan.FromDays(1), () => throw new IOException("Unexpected capture"), Ct));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Service.CreateWhenDueAsync(f.Storage,
            TimeSpan.FromDays(1), () => throw new IOException("Unexpected capture"), cancelled.Token));
        Assert.Empty(await f.Storage.ListAsync(Ct)); Assert.Null(f.Service.LastAutomaticBackupAt);
        Assert.Equal(0, f.Protection.Completed);
    }

    private sealed class ForbiddenStorage : IBackupStorage
    {
        public string Id => "cloud";
        public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task DeleteAsync(string fileId, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Folder = new();
        internal readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero));
        internal readonly InMemoryBackupSource Source = new("database.sqlite", "original fictional ledger");
        internal readonly Protection Protection = new();
        internal readonly BackupService Service;
        internal readonly LocalFolderBackupStorage Storage;
        internal string? Set;
        internal Fixture(bool withProtection = true)
        {
            Storage = new(Folder.Path);
            Service = new([Source], new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test", "Tests"),
                new InMemorySettingsStore(), Time, new BackupOptions { FileSet = () => Set },
                safetyProtection: withProtection ? Protection : null, automaticProtection: withProtection ? Protection : null);
            Protection.Reader = Service; Protection.Storage = Storage;
        }
        internal async Task<byte[]> ReadAsync(BackupFileInfo file)
        { await using var input = await Storage.OpenReadAsync(file.Id, Ct); using var bytes = new MemoryStream(); await input.CopyToAsync(bytes, Ct); return bytes.ToArray(); }
        public void Dispose() { Protection.Dispose(); Folder.Dispose(); }
    }

    private sealed class Protection : IBackupSafetyCopyProtection, IAutomaticLocalBackupProtection, IDisposable
    {
        private readonly byte[] key = RandomNumberGenerator.GetBytes(32);
        private readonly byte[] identity = RandomNumberGenerator.GetBytes(48);
        internal IBackupPackageReader Reader = null!;
        internal IBackupStorage Storage = null!;
        internal ReadOnlyMemory<byte> Borrowed;
        internal Action? BeforeProtect;
        internal bool FailCompletion;
        internal int Completed;
        public Task<byte[]> ProtectAsync(ReadOnlyMemory<byte> package, CancellationToken ct) => Task.FromResult(DeviceSafetyEncryption.Protect(package.Span, key, identity));
        public Task<byte[]> OpenAsync(ReadOnlyMemory<byte> envelope, CancellationToken ct) => Task.FromResult(DeviceSafetyEncryption.Open(envelope.Span, key));
        public Task<byte[]> ProtectAutomaticAsync(ReadOnlyMemory<byte> package, Action validate, CancellationToken ct)
        { Borrowed = package; BeforeProtect?.Invoke(); validate(); return ProtectAsync(package, ct); }
        public async Task CompleteAutomaticAsync(BackupFileInfo file, Action validate, CancellationToken ct)
        {
            validate(); if (FailCompletion) throw new IOException("Fictitious qualification failure.");
            await using var input = await Storage.OpenReadAsync(file.Id, ct); using var bytes = new MemoryStream(); await input.CopyToAsync(bytes, ct);
            var clear = DeviceSafetyEncryption.Open(bytes.ToArray(), key);
            try { await Reader.ReadValidatedPortableAsync(clear, null, DeviceSafetyEncryption.MaximumPackageBytes, (_, _, _) => Task.CompletedTask, ct); }
            finally { CryptographicOperations.ZeroMemory(clear); }
            Completed++;
        }
        public void Dispose() => CryptographicOperations.ZeroMemory(key);
    }
}
