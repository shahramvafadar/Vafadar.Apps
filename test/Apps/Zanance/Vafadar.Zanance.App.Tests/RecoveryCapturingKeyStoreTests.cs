using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks normal key authority and explicit recovery ordering in the actual shared application policy.</summary>
public sealed class RecoveryCapturingKeyStoreTests
{
    [Fact, Trait("AT", "AT-226")]
    public async Task Missing_device_key_read_propagates_without_preparing_a_replacement()
    {
        var native = new KeyEffects(); var policy = new RecoveryCapturingGenerationKeyStore(native, native);
        await Assert.ThrowsAsync<IOException>(() => policy.ReadAsync(Binding(), TestContext.Current.CancellationToken));
        Assert.Equal(0, native.Prepares); Assert.Equal(1, native.Reads); Assert.Equal(0, native.Imports);
    }

    [Fact, Trait("AT", "AT-226")]
    public async Task Explicit_import_verifies_existing_generation_before_native_import_completes()
    {
        var native = new KeyEffects(); var policy = new RecoveryCapturingGenerationKeyStore(native, native);
        var verified = false;
        await policy.ImportRecoveredAsync(Binding(), new byte[32], (key, token) =>
        {
            token.ThrowIfCancellationRequested(); Assert.Equal(32, key.Length);
            Assert.False(native.ImportCompleted); verified = true; return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        Assert.True(verified); Assert.True(native.ImportCompleted); Assert.Equal(0, native.Prepares);
    }

    [Fact, Trait("AT", "AT-226")]
    public async Task Existing_generation_verification_failure_refuses_import_completion()
    {
        var native = new KeyEffects(); var policy = new RecoveryCapturingGenerationKeyStore(native, native);
        await Assert.ThrowsAsync<IOException>(() => policy.ImportRecoveredAsync(Binding(), new byte[32],
            (_, _) => throw new IOException("Fixture generation differs."), TestContext.Current.CancellationToken));
        Assert.False(native.ImportCompleted); Assert.Equal(0, native.Prepares);
    }

    private static DatabaseKeyBinding Binding() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    /// <summary>Observes only platform calls; the application key policy is compiled from its actual source.</summary>
    private sealed class KeyEffects : IDatabaseGenerationKeyStore, IDatabaseGenerationKeyRecovery
    {
        internal int Prepares { get; private set; }
        internal int Reads { get; private set; }
        internal int Imports { get; private set; }
        internal bool ImportCompleted { get; private set; }
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) { Prepares++; return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { Reads++; throw new IOException("Fixture device key is missing."); }
        public async Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyExistingGeneration, CancellationToken ct)
        {
            Imports++; await verifyExistingGeneration(key, ct); ImportCompleted = true;
        }
    }
}
