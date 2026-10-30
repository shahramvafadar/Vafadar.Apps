using Microsoft.Extensions.Time.Testing;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Backup.Tests;

/// <summary>Original stored portable receipts and profile-specific metadata/retention across actual awaits.</summary>
public sealed class ManualBackupOwnershipTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 30, 8, 0, 0, TimeSpan.Zero));
    private readonly InMemorySettingsStore _settings = new();
    private string? _set = "original";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();
    private BackupService Service(IBackupSource source, ISettingsStore? settings = null) => new([source],
        new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test device", "Tests"),
        settings ?? _settings, _time, new BackupOptions { FileSet = () => _set, MaxBackupsToKeep = 1 });
    private Storage Store() => new(new LocalFolderBackupStorage(_directory.Path));

    [Fact]
    public async Task Changed_profile_during_original_package_creation_cannot_upload_under_later_profile()
    {
        var storage = Store(); var service = Service(new Source(() => _set = "later"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBackupAsync(storage, cancellationToken: Ct));
        Assert.Equal(0, storage.Uploads); Assert.Empty(await storage.Inner.ListAsync(Ct));
    }

    [Fact]
    public async Task Stored_original_file_keeps_its_original_profile_receipt_after_upload_switch()
    {
        var storage = Store(); storage.AfterUpload = () => _set = "later";
        var file = await Service(new Source()).CreateBackupAsync(storage, cancellationToken: Ct);
        Assert.StartsWith("pro.vafadar.test~original_", file.FileName, StringComparison.Ordinal);
        Assert.NotNull(_settings.Get("backup.lastBackupAt.original"));
        Assert.Null(_settings.Get("backup.lastBackupAt.later"));
        Assert.Equal(0, storage.Deletes); Assert.Single(await storage.Inner.ListAsync(Ct));
    }

    [Fact]
    public async Task Cancelled_retention_after_actual_upload_cannot_erase_known_stored_receipt()
    {
        var storage = Store(); storage.BeforeList = () => throw new OperationCanceledException();
        var file = await Service(new Source()).CreateBackupAsync(storage, cancellationToken: Ct);
        Assert.Equal(file, Assert.Single(await storage.Inner.ListAsync(Ct)));
        Assert.Equal(1, storage.Uploads); Assert.Equal(0, storage.Deletes);
    }

    [Fact]
    public async Task Original_scoped_listing_cannot_borrow_profile_changed_while_storage_waited()
    {
        var storage = Store(); storage.BeforeList = () => _set = "later";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(new Source()).ListBackupsAsync(storage, Ct));
    }

    [Fact]
    public async Task Original_retention_refuses_profile_changed_during_listing_before_any_delete()
    {
        var storage = Store(); var source = new Source(); var service = Service(source);
        _set = "later";
        using (var bytes = new MemoryStream([1]))
        { await storage.Inner.UploadAsync(BackupFileName.Create("pro.vafadar.test~later", _time.GetUtcNow()), bytes, Ct); }
        using (var bytes = new MemoryStream([2]))
        { await storage.Inner.UploadAsync(BackupFileName.Create("pro.vafadar.test~later", _time.GetUtcNow().AddMinutes(-1)), bytes, Ct); }
        _set = "original"; storage.BeforeList = () => _set = "later";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyRetentionAsync(storage, Ct));
        Assert.Equal(0, storage.Deletes); Assert.Equal(2, (await storage.Inner.ListAsync(Ct)).Count);
    }

    [Fact]
    public async Task Actual_stored_receipt_survives_failure_to_record_last_backup_metadata()
    {
        var storage = Store(); var service = Service(new Source(), new FailedLastBackupSettings());
        var file = await service.CreateBackupAsync(storage, cancellationToken: Ct);
        Assert.Equal(file, Assert.Single(await storage.Inner.ListAsync(Ct))); Assert.Equal(1, storage.Uploads);
    }

    private sealed class FailedLastBackupSettings : ISettingsStore
    {
        public string? Get(string key) => null;
        public void Set(string key, string? value) => throw new IOException("Fictitious preference-store failure after upload.");
        public void Remove(string key) => throw new InvalidOperationException("No preference removal.");
    }

    /// <summary>Exports only fictitious bytes through the original package implementation.</summary>
    private sealed class Source(Action? before = null) : IBackupSource
    {
        public string Name => "database.sqlite";
        public async Task WriteAsync(Stream destination, CancellationToken ct)
        { before?.Invoke(); await destination.WriteAsync("original-fictional-source"u8.ToArray(), ct); }
        public Task RestoreAsync(Stream source, CancellationToken ct) => throw new InvalidOperationException("No restore is permitted.");
    }
    /// <summary>Delegates actual storage effects while holding the exact boundary under test.</summary>
    private sealed class Storage(LocalFolderBackupStorage inner) : IBackupStorage
    {
        internal LocalFolderBackupStorage Inner => inner;
        internal Action? AfterUpload, BeforeList;
        internal int Uploads, Deletes;
        public string Id => inner.Id;
        public async Task<BackupFileInfo> UploadAsync(string name, Stream content, CancellationToken ct = default)
        { Uploads++; var file = await inner.UploadAsync(name, content, ct); AfterUpload?.Invoke(); return file; }
        public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken ct = default)
        { BeforeList?.Invoke(); return inner.ListAsync(ct); }
        public Task<Stream> OpenReadAsync(string id, CancellationToken ct = default) => inner.OpenReadAsync(id, ct);
        public Task DeleteAsync(string id, CancellationToken ct = default) { Deletes++; return inner.DeleteAsync(id, ct); }
    }
}
