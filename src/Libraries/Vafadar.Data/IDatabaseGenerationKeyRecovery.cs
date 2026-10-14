namespace Vafadar.Data;

/// <summary>Explicit native re-enrollment of a recovered DEK after actual existing-finance verification.</summary>
public interface IDatabaseGenerationKeyRecovery
{
    /// <summary>Verifies an owned key copy against the existing generation, then durably publishes its protected recovery entry.</summary>
    /// <remarks>The host supplies app/device authorization and drained access. Existing original envelopes are retained,
    /// existing recovery entries are never replaced, and reads never invoke this operation or manufacture a key.</remarks>
    Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> recoveredKey,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyExistingGeneration, CancellationToken cancellationToken);
}
