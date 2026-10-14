using System.Security.Cryptography;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Actual native DPAPI re-enrollment retains original envelopes and refuses replacement or unverified finance.</summary>
public sealed class WindowsKeyRecoveryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-240")]
    public async Task Verified_recovery_preserves_unavailable_original_and_cold_read_uses_same_key()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var directory = new TemporaryDirectory(); var store = new WindowsGenerationKeyStore(directory.Path, () => { });
        var binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await store.PrepareAsync(binding, Ct); var key = await store.ReadAsync(binding, Ct);
        var original = directory.Combine($"{binding.ProfileId:N}-{binding.GenerationId:N}.key");
        var unavailable = await File.ReadAllBytesAsync(original, Ct); unavailable[0] ^= 1; await File.WriteAllBytesAsync(original, unavailable, Ct);
        try
        {
            var checks = 0;
            await store.ImportRecoveredAsync(binding, key, (candidate, _) => { checks++; Assert.Equal(key, candidate.ToArray()); return Task.CompletedTask; }, Ct);
            Assert.Equal(1, checks); Assert.Equal(unavailable, await File.ReadAllBytesAsync(original, Ct));
            var cold = await new WindowsGenerationKeyStore(directory.Path, () => { }).ReadAsync(binding, Ct);
            try { Assert.Equal(key, cold); } finally { CryptographicOperations.ZeroMemory(cold); }
            var before = await File.ReadAllBytesAsync(original + ".recovered", Ct);
            await store.ImportRecoveredAsync(binding, key, (_, _) => Task.CompletedTask, Ct);
            Assert.Equal(before, await File.ReadAllBytesAsync(original + ".recovered", Ct));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-240")]
    public async Task Rejected_financial_verification_does_not_publish_or_change_any_key_entry()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var directory = new TemporaryDirectory(); var store = new WindowsGenerationKeyStore(directory.Path, () => { });
        var binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            await Assert.ThrowsAsync<CryptographicException>(() => ((IDatabaseGenerationKeyRecovery)store).ImportRecoveredAsync(binding, key,
                (_, _) => throw new CryptographicException("Fictitious wrong database key."), Ct));
            Assert.Empty(Directory.EnumerateFiles(directory.Path));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-240")]
    public async Task Existing_recovered_entry_cannot_be_replaced_with_a_different_key()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var directory = new TemporaryDirectory(); var store = new WindowsGenerationKeyStore(directory.Path, () => { });
        var binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var key = RandomNumberGenerator.GetBytes(32); var changed = RandomNumberGenerator.GetBytes(32);
        try
        {
            await store.ImportRecoveredAsync(binding, key, (_, _) => Task.CompletedTask, Ct);
            var path = directory.Combine($"{binding.ProfileId:N}-{binding.GenerationId:N}.key.recovered"); var before = await File.ReadAllBytesAsync(path, Ct);
            await Assert.ThrowsAsync<CryptographicException>(() => ((IDatabaseGenerationKeyRecovery)store).ImportRecoveredAsync(binding, changed, (_, _) => Task.CompletedTask, Ct));
            Assert.Equal(before, await File.ReadAllBytesAsync(path, Ct));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(changed); }
    }

    [Fact, Trait("AT", "AT-240")]
    public async Task Damaged_recovery_entry_blocks_read_and_preparation_without_original_fallback()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var directory = new TemporaryDirectory(); var store = new WindowsGenerationKeyStore(directory.Path, () => { });
        var binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await store.PrepareAsync(binding, Ct); var key = await store.ReadAsync(binding, Ct);
        try
        {
            await store.ImportRecoveredAsync(binding, key, (_, _) => Task.CompletedTask, Ct);
            var path = directory.Combine($"{binding.ProfileId:N}-{binding.GenerationId:N}.key.recovered");
            await File.WriteAllBytesAsync(path, [], Ct);
            await Assert.ThrowsAsync<CryptographicException>(() => ((IDatabaseGenerationKeyStore)store).ReadAsync(binding, Ct));
            await Assert.ThrowsAsync<CryptographicException>(() => ((IDatabaseGenerationKeyStore)store).PrepareAsync(binding, Ct));
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
