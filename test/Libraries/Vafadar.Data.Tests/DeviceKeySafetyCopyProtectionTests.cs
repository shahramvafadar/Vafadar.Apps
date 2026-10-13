using System.Security.Cryptography;
using Vafadar.Backup;

namespace Vafadar.Data.Tests;

/// <summary>Checks safety-key isolation and fail-closed reads through the actual protection adapter.</summary>
public sealed class DeviceKeySafetyCopyProtectionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Each_copy_has_a_distinct_key_and_cold_open_never_prepares_a_replacement()
    {
        var keys = new Keys();
        var protection = new DeviceKeySafetyCopyProtection(Guid.NewGuid(), Guid.NewGuid(), keys, () => { });
        var first = await protection.ProtectAsync("first"u8.ToArray(), Ct);
        var second = await protection.ProtectAsync("second"u8.ToArray(), Ct);
        Assert.Equal(2, keys.Prepared.Count);
        Assert.NotEqual(keys.Prepared[0], keys.Prepared[1]);
        keys.ForbidPrepare = true;
        Assert.Equal("first"u8.ToArray(), await protection.OpenAsync(first, Ct));
        Assert.Equal("second"u8.ToArray(), await protection.OpenAsync(second, Ct));
        Assert.All(keys.Returned, bytes => Assert.True(bytes.All(value => value == 0)));
        keys.Stored.Clear();
        await Assert.ThrowsAsync<IOException>(() => protection.OpenAsync(first, Ct));
        Assert.Equal(2, keys.Prepared.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Foreign_profile_or_installation_is_rejected_before_key_access(bool changeProfile)
    {
        var keys = new Keys(); var profile = Guid.NewGuid(); var installation = Guid.NewGuid();
        var original = new DeviceKeySafetyCopyProtection(profile, installation, keys, () => { });
        var packet = await original.ProtectAsync("content"u8.ToArray(), Ct);
        var foreign = new DeviceKeySafetyCopyProtection(changeProfile ? Guid.NewGuid() : profile,
            changeProfile ? installation : Guid.NewGuid(), keys, () => { });
        var reads = keys.Returned.Count;
        await Assert.ThrowsAsync<BackupException>(() => foreign.OpenAsync(packet, Ct));
        Assert.Equal(reads, keys.Returned.Count);
    }

    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal readonly Dictionary<DatabaseKeyBinding, byte[]> Stored = [];
        internal readonly List<DatabaseKeyBinding> Prepared = [];
        internal readonly List<byte[]> Returned = [];
        internal bool ForbidPrepare;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        {
            if (ForbidPrepare) throw new IOException("Read attempted key preparation.");
            Prepared.Add(binding); Stored.Add(binding, RandomNumberGenerator.GetBytes(32));
            return Task.CompletedTask;
        }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        {
            if (!Stored.TryGetValue(binding, out var key)) throw new IOException("Missing key.");
            var owned = key.ToArray(); Returned.Add(owned); return Task.FromResult(owned);
        }
    }
}
