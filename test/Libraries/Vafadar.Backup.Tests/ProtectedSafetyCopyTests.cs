using System.Security.Cryptography;
using Vafadar.Backup.Security;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Backup.Tests;

/// <summary>Exercises actual package creation and restore while keeping portable protection independent.</summary>
public sealed class ProtectedSafetyCopyTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Safety_copy_round_trips_without_recording_portable_backup_or_pruning_recovery_files()
    {
        using var folder = new TemporaryDirectory();
        var storage = new LocalFolderBackupStorage(folder.Path);
        var source = new InMemoryBackupSource("database.sqlite", "fictitious money");
        var protection = new Protection(); var service = Service(source, protection);
        var first = await service.CreateSafetyCopyAsync(storage, Ct);
        await service.CreateSafetyCopyAsync(storage, Ct);
        Assert.Equal(2, (await storage.ListAsync(Ct)).Count);
        Assert.Null(service.LastBackupAt);
        await using var stream = await storage.OpenReadAsync(first.Id, Ct);
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes, Ct);
        var envelope = bytes.ToArray();
        Assert.True(DeviceSafetyEncryption.IsEncrypted(envelope));
        Assert.True(protection.Borrowed.Span.IndexOfAnyExcept((byte)0) < 0);
        source.Content = "changed";
        await service.RestorePackageAsync(envelope, cancellationToken: Ct);
        Assert.Equal("fictitious money", source.Content);
        var missingContext = Service(source, null);
        var error = await Assert.ThrowsAsync<BackupException>(() => missingContext.RestorePackageAsync(envelope, cancellationToken: Ct));
        Assert.Equal(BackupError.InvalidFormat, error.Error);
    }

    [Fact]
    public async Task Portable_password_choice_is_independent_and_failed_protection_writes_no_file()
    {
        using var folder = new TemporaryDirectory(); var storage = new LocalFolderBackupStorage(folder.Path);
        var source = new InMemoryBackupSource("database.sqlite", "content");
        var protection = new Protection { Fail = true }; var service = Service(source, protection);
        await Assert.ThrowsAsync<IOException>(() => service.CreateSafetyCopyAsync(storage, Ct));
        Assert.Empty(await storage.ListAsync(Ct));
        Assert.True(protection.Borrowed.Span.IndexOfAnyExcept((byte)0) < 0);
        var portable = await service.CreatePackageAsync(cancellationToken: Ct);
        Assert.True(portable.AsSpan().StartsWith("PK"u8));
        var password = await service.CreatePackageAsync("fictitious-password", Ct);
        Assert.True(BackupEncryption.IsEncrypted(password));
        await service.RestorePackageAsync(password, "fictitious-password", Ct);
        Assert.Equal(1, source.RestoreCount);
    }

    private static BackupService Service(IBackupSource source, IBackupSafetyCopyProtection? protection) => new([source],
        new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test", "Tests"),
        new InMemorySettingsStore(), TimeProvider.System, new BackupOptions { MaxBackupsToKeep = 1 }, safetyProtection: protection);

    [Fact]
    public async Task Oversized_source_is_refused_before_key_preparation_or_storage_upload()
    {
        using var folder = new TemporaryDirectory(); var storage = new LocalFolderBackupStorage(folder.Path);
        var protection = new Protection { Fail = true };
        await Assert.ThrowsAsync<BackupException>(() => Service(new OversizedSource(), protection).CreateSafetyCopyAsync(storage, Ct));
        Assert.True(protection.Borrowed.IsEmpty);
        Assert.Empty(await storage.ListAsync(Ct));
    }

    private sealed class OversizedSource : IBackupSource
    {
        public string Name => "oversized.sqlite";
        public Task WriteAsync(Stream destination, CancellationToken ct)
        {
            destination.SetLength((long)DeviceSafetyEncryption.MaximumPackageBytes + 1);
            return Task.CompletedTask;
        }
        public Task RestoreAsync(Stream source, CancellationToken ct) => throw new IOException("Unexpected restore.");
    }

    private sealed class Protection : IBackupSafetyCopyProtection
    {
        private readonly byte[] key = RandomNumberGenerator.GetBytes(32);
        internal bool Fail;
        internal ReadOnlyMemory<byte> Borrowed;
        public Task<byte[]> ProtectAsync(ReadOnlyMemory<byte> package, CancellationToken ct)
        {
            Borrowed = package;
            if (Fail) throw new IOException("Fictitious protection failure.");
            return Task.FromResult(DeviceSafetyEncryption.Protect(package.Span, key, new byte[48]));
        }
        public Task<byte[]> OpenAsync(ReadOnlyMemory<byte> envelope, CancellationToken ct) =>
            Task.FromResult(DeviceSafetyEncryption.Open(envelope.Span, key));
    }
}
