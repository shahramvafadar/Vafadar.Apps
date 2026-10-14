using System.Text;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;

namespace Vafadar.Backup.Tests;

/// <summary>Bounded original package validation neither opens live sources nor retains decoded financial buffers.</summary>
public sealed class PortableRecoveryReaderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static BackupService Service(InMemoryBackupSource source, string id = "pro.vafadar.test") =>
        new([source], new StaticAppEnvironment(id, "Test", new(1, 0), "Test device", "Tests"),
            new InMemorySettingsStore(), TimeProvider.System, new BackupOptions());

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-238")]
    public async Task Validated_portable_reader_borrows_complete_data_without_replacing_sources_and_clears_it(bool encrypted)
    {
        var source = new InMemoryBackupSource("database.sqlite", "fictitious portable content"); var service = Service(source);
        var package = await service.CreatePackageAsync(encrypted ? "fictitious-only" : null, Ct);
        ReadOnlyMemory<byte> borrowed = default;
        await service.ReadValidatedPortableAsync(package, encrypted ? "fictitious-only" : null, 4096, (manifest, entries, _) =>
        {
            Assert.Equal("pro.vafadar.test", manifest.AppId); borrowed = entries["database.sqlite"];
            Assert.Equal(source.Content, Encoding.UTF8.GetString(borrowed.Span)); return Task.CompletedTask;
        }, Ct);
        Assert.Equal(0, source.RestoreCount); Assert.True(borrowed.Span.IndexOfAnyExcept((byte)0) < 0);
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Failed_consumer_releases_gate_and_owned_buffers_without_source_writes()
    {
        var source = new InMemoryBackupSource("database.sqlite", "fictitious content"); var service = Service(source);
        var package = await service.CreatePackageAsync(cancellationToken: Ct); ReadOnlyMemory<byte> borrowed = default;
        await Assert.ThrowsAsync<IOException>(() => service.ReadValidatedPortableAsync(package, null, 4096, (_, entries, _) =>
        { borrowed = entries["database.sqlite"]; throw new IOException("Fictitious consumer interruption"); }, Ct));
        Assert.True(borrowed.Span.IndexOfAnyExcept((byte)0) < 0); Assert.Equal(0, source.RestoreCount);
        await service.ReadValidatedPortableAsync(package, null, 4096, (_, _, _) => Task.CompletedTask, Ct);
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Foreign_app_and_device_only_safety_never_reach_portable_consumer()
    {
        var source = new InMemoryBackupSource("database.sqlite", "fictitious"); var service = Service(source); var calls = 0;
        var foreign = await Service(source, "pro.vafadar.other").CreatePackageAsync(cancellationToken: Ct);
        foreach (var package in new[] { foreign, "VBS1not-a-portable-package"u8.ToArray() })
            await Assert.ThrowsAsync<BackupException>(() => service.ReadValidatedPortableAsync(package, null, 4096,
                (_, _, _) => { calls++; return Task.CompletedTask; }, Ct));
        Assert.Equal(0, calls); Assert.Equal(0, source.RestoreCount);
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Compressed_payload_cannot_expand_beyond_portable_recovery_bound()
    {
        var service = Service(new InMemoryBackupSource("database.sqlite", new string('x', 8000)));
        var package = await service.CreatePackageAsync(cancellationToken: Ct); var called = false;
        await Assert.ThrowsAsync<BackupException>(() => service.ReadValidatedPortableAsync(package, null, 4096,
            (_, _, _) => { called = true; return Task.CompletedTask; }, Ct));
        Assert.False(called);
    }
}
