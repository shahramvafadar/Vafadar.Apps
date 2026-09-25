using System.IO.Compression;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Backup.Tests;

public sealed class BackupServiceTests : IDisposable
{
    private const string AppId = "pro.vafadar.test";

    private readonly TemporaryDirectory _directory = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero));
    private readonly InMemorySettingsStore _settings = new();
    private readonly InMemoryBackupSource _database = new("database.sqlite", "db-v1");
    private readonly InMemoryBackupSource _attachments = new("attachments.zip", "files-v1");
    private readonly LocalFolderBackupStorage _storage;

    public BackupServiceTests()
    {
        _storage = new LocalFolderBackupStorage(_directory.Path);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task Backup_and_restore_round_trip_all_sources()
    {
        var service = CreateService();
        var file = await service.CreateBackupAsync(_storage, cancellationToken: Ct);

        _database.Content = "db-changed";
        _attachments.Content = "files-changed";
        var manifest = await service.RestoreAsync(_storage, file, cancellationToken: Ct);

        Assert.Equal("db-v1", _database.Content);
        Assert.Equal("files-v1", _attachments.Content);
        Assert.Equal(AppId, manifest.AppId);
        Assert.Equal("1.2.0", manifest.AppVersion);
        Assert.Equal(_time.GetUtcNow(), manifest.CreatedAt);
        Assert.Equal(["database.sqlite", "attachments.zip"], manifest.Entries.Select(e => e.Name));
    }

    [Fact]
    public async Task Encrypted_backups_need_the_password()
    {
        var service = CreateService();
        var file = await service.CreateBackupAsync(_storage, password: "secret", cancellationToken: Ct);
        _database.Content = "db-changed";

        var missing = await Assert.ThrowsAsync<BackupException>(() => service.RestoreAsync(_storage, file, cancellationToken: Ct));
        var wrong = await Assert.ThrowsAsync<BackupException>(() => service.RestoreAsync(_storage, file, "guess", Ct));
        await service.RestoreAsync(_storage, file, "secret", Ct);

        Assert.Equal(BackupError.PasswordRequired, missing.Error);
        Assert.Equal(BackupError.InvalidPasswordOrCorrupted, wrong.Error);
        Assert.Equal("db-v1", _database.Content);
    }

    [Fact]
    public async Task Backups_of_another_app_are_rejected()
    {
        var package = await CreateService(appId: "pro.vafadar.other").CreatePackageAsync(cancellationToken: Ct);

        var error = await Assert.ThrowsAsync<BackupException>(() => CreateService().RestorePackageAsync(package, cancellationToken: Ct));

        Assert.Equal(BackupError.WrongApp, error.Error);
        Assert.Equal(0, _database.RestoreCount);
    }

    [Fact]
    public async Task Backups_of_a_newer_app_version_are_rejected()
    {
        var package = await CreateService(version: new Version(2, 0)).CreatePackageAsync(cancellationToken: Ct);

        var error = await Assert.ThrowsAsync<BackupException>(() => CreateService(version: new Version(1, 9, 9)).RestorePackageAsync(package, cancellationToken: Ct));

        Assert.Equal(BackupError.CreatedByNewerAppVersion, error.Error);
    }

    [Fact]
    public async Task Versions_with_a_different_number_of_components_compare_equal()
    {
        var package = await CreateService(version: new Version(1, 0, 0)).CreatePackageAsync(cancellationToken: Ct);

        await CreateService(version: new Version(1, 0)).RestorePackageAsync(package, cancellationToken: Ct);

        Assert.Equal(1, _database.RestoreCount);
    }

    [Fact]
    public async Task Damaged_content_is_detected_before_any_data_is_replaced()
    {
        var service = CreateService();
        var package = await service.CreatePackageAsync(cancellationToken: Ct);
        var tampered = ReplaceEntry(package, "data/attachments.zip", "evil");

        var error = await Assert.ThrowsAsync<BackupException>(() => service.RestorePackageAsync(tampered, cancellationToken: Ct));

        Assert.Equal(BackupError.Corrupted, error.Error);
        Assert.Equal(0, _database.RestoreCount);
        Assert.Equal(0, _attachments.RestoreCount);
    }

    [Fact]
    public async Task Files_that_are_not_backups_are_rejected()
    {
        var error = await Assert.ThrowsAsync<BackupException>(() => CreateService().RestorePackageAsync("hello"u8.ToArray(), cancellationToken: Ct));

        Assert.Equal(BackupError.InvalidFormat, error.Error);
    }

    [Fact]
    public async Task Only_the_newest_backups_are_kept()
    {
        var service = CreateService(maxBackups: 2);

        for (var i = 0; i < 4; i++)
        {
            await service.CreateBackupAsync(_storage, cancellationToken: Ct);
            _time.Advance(TimeSpan.FromHours(1));
        }

        var backups = await service.ListBackupsAsync(_storage, Ct);
        Assert.Equal(
            [BackupFileName.Create(AppId, _time.GetUtcNow().AddHours(-1)), BackupFileName.Create(AppId, _time.GetUtcNow().AddHours(-2))],
            backups.Select(b => b.FileName));
    }

    [Fact]
    public async Task Listing_ignores_other_apps_and_unrelated_files()
    {
        await File.WriteAllTextAsync(_directory.Combine("readme.txt"), "x", Ct);
        await CreateService(appId: "pro.vafadar.other").CreateBackupAsync(_storage, cancellationToken: Ct);
        _time.Advance(TimeSpan.FromMinutes(1));
        var own = await CreateService().CreateBackupAsync(_storage, cancellationToken: Ct);

        var backups = await CreateService().ListBackupsAsync(_storage, Ct);

        Assert.Equal([own.FileName], backups.Select(b => b.FileName));
    }

    [Fact]
    public async Task Automatic_backup_is_due_until_a_backup_was_made_within_the_interval()
    {
        var service = CreateService();
        Assert.True(service.IsAutomaticBackupDue());
        Assert.Null(service.LastBackupAt);

        await service.CreateBackupAsync(_storage, cancellationToken: Ct);
        Assert.False(service.IsAutomaticBackupDue());
        Assert.Equal(_time.GetUtcNow(), service.LastBackupAt);

        _time.Advance(TimeSpan.FromDays(1));
        Assert.True(service.IsAutomaticBackupDue());
    }

    [Fact]
    public async Task Duplicate_source_names_are_rejected()
    {
        var service = new BackupService(
            [_database, new InMemoryBackupSource("DATABASE.sqlite", "x")],
            Environment(),
            _settings,
            _time,
            new BackupOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePackageAsync(cancellationToken: Ct));
    }

    private BackupService CreateService(string appId = AppId, Version? version = null, int maxBackups = 10) =>
        new(
            [_database, _attachments],
            Environment(appId, version),
            _settings,
            _time,
            new BackupOptions { MaxBackupsToKeep = maxBackups });

    private static StaticAppEnvironment Environment(string appId = AppId, Version? version = null) =>
        new(appId, "Test", version ?? new Version(1, 2, 0), "Test device", "Tests");

    private static byte[] ReplaceEntry(byte[] package, string entryName, string content)
    {
        using var buffer = new MemoryStream();
        buffer.Write(package);
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Update, leaveOpen: true))
        {
            archive.GetEntry(entryName)!.Delete();
            using var writer = new StreamWriter(archive.CreateEntry(entryName).Open());
            writer.Write(content);
        }

        return buffer.ToArray();
    }
}
