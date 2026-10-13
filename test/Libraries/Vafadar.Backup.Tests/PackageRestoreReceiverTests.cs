using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;

namespace Vafadar.Backup.Tests;

/// <summary>Checks real package dispatch, validation and lifetime without claiming native transaction durability.</summary>
public sealed class PackageRestoreReceiverTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-200")]
    public async Task Receiver_gets_complete_validated_package_instead_of_individual_source_writes()
    {
        var sources = new[] { new InMemoryBackupSource("database.sqlite", "database"), new InMemoryBackupSource("attachments.zip", "attachments") };
        var receiver = new Receiver(); var service = Service(sources, receiver);
        var package = await service.CreatePackageAsync(cancellationToken: Ct);
        await service.RestorePackageAsync(package, cancellationToken: Ct);
        Assert.Equal("database", receiver.Copies["database.sqlite"]);
        Assert.Equal("attachments", receiver.Copies["attachments.zip"]);
        Assert.All(sources, source => Assert.Equal(0, source.RestoreCount));
        Assert.All(receiver.Borrowed!.Values, value => Assert.True(value.Span.IndexOfAnyExcept((byte)0) < 0));
    }

    [Fact, Trait("AT", "AT-200")]
    public async Task Failed_receiver_does_not_fall_back_and_releases_gate_and_borrowed_buffers()
    {
        var source = new InMemoryBackupSource("database.sqlite", "original");
        var receiver = new Receiver { Fail = true }; var service = Service([source], receiver);
        var package = await service.CreatePackageAsync(cancellationToken: Ct);
        await Assert.ThrowsAsync<IOException>(() => service.RestorePackageAsync(package, cancellationToken: Ct));
        Assert.Equal(0, source.RestoreCount);
        Assert.All(receiver.Borrowed!.Values, value => Assert.True(value.Span.IndexOfAnyExcept((byte)0) < 0));
        receiver.Fail = false;
        await service.RestorePackageAsync(package, cancellationToken: Ct);
        Assert.Equal(2, receiver.Calls);
    }

    [Fact, Trait("AT", "AT-200")]
    public async Task Corrupt_package_never_reaches_receiver()
    {
        var receiver = new Receiver(); var service = Service([new InMemoryBackupSource("database.sqlite", "original")], receiver);
        var package = await service.CreatePackageAsync(cancellationToken: Ct);
        using var output = new MemoryStream(); output.Write(package);
        using (var zip = new ZipArchive(output, ZipArchiveMode.Update, true))
        {
            zip.GetEntry("data/database.sqlite")!.Delete();
            using var writer = new StreamWriter(zip.CreateEntry("data/database.sqlite").Open()); writer.Write("tampered");
        }
        await Assert.ThrowsAsync<BackupException>(() => service.RestorePackageAsync(output.ToArray(), cancellationToken: Ct));
        Assert.Equal(0, receiver.Calls);
    }

    [Fact, Trait("AT", "AT-200")]
    public async Task Missing_package_source_is_absent_and_unknown_source_is_preserved_for_receiver_policy()
    {
        var package = await Service([new InMemoryBackupSource("older.dat", "old")]).CreatePackageAsync(cancellationToken: Ct);
        var receiver = new Receiver();
        await Service([new InMemoryBackupSource("newer.dat", "keep")], receiver).RestorePackageAsync(package, cancellationToken: Ct);
        Assert.Equal("old", receiver.Copies["older.dat"]); Assert.False(receiver.Copies.ContainsKey("newer.dat"));
    }

    [Fact, Trait("AT", "AT-200")]
    public async Task Registered_receiver_is_used_by_normal_dependency_injection_resolution()
    {
        var receiver = new Receiver(); var services = new ServiceCollection();
        services.AddSingleton<IAppEnvironment>(Environment()); services.AddSingleton<ISettingsStore>(new InMemorySettingsStore());
        services.AddSingleton<IBackupSource>(new InMemoryBackupSource("database.sqlite", "original"));
        services.AddSingleton<IBackupPackageRestoreReceiver>(receiver); services.AddVafadarBackup();
        using var provider = services.BuildServiceProvider(); var service = provider.GetRequiredService<IBackupService>();
        var package = await service.CreatePackageAsync(cancellationToken: Ct);
        await service.RestorePackageAsync(package, cancellationToken: Ct); Assert.Equal(1, receiver.Calls);
    }

    private static StaticAppEnvironment Environment() => new("pro.vafadar.test", "Test", new Version(1, 0), "Test device", "Tests");
    private static BackupService Service(IEnumerable<IBackupSource> sources, Receiver? receiver = null) =>
        new(sources, [], Environment(), new InMemorySettingsStore(), TimeProvider.System, new BackupOptions(), receiver);

    private sealed class Receiver : IBackupPackageRestoreReceiver
    {
        public bool Fail { get; set; }
        public int Calls { get; private set; }
        public IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? Borrowed { get; private set; }
        public Dictionary<string, string> Copies { get; } = [];
        public Task RestoreAsync(BackupManifest manifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Calls++; Borrowed = entries;
            Assert.Equal("pro.vafadar.test", manifest.AppId);
            foreach (var entry in entries) Copies[entry.Key] = Encoding.UTF8.GetString(entry.Value.Span);
            if (Fail) throw new IOException("Fictitious receiver failure.");
            return Task.CompletedTask;
        }
    }
}
