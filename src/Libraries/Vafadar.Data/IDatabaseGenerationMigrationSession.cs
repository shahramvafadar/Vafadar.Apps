namespace Vafadar.Data;

/// <summary>Native effects for an exclusively captured profile migration; implementations own durable storage and key access.</summary>
/// <remarks>Keep the writer gate held throughout the call. Effects must be idempotent after process interruption;
/// no method may overwrite an unrelated generation, log keys, or fall back to plaintext after selection.</remarks>
public interface IDatabaseGenerationMigrationSession
{
    /// <summary>Rejects a retired profile, access grant or exclusive writer lease using captured local state.</summary>
    void ValidateContext();
    /// <summary>Reads the durable selector/checkpoint; missing or damaged state must fail closed.</summary>
    Task<DatabaseGenerationCheckpoint> ReadCheckpointAsync(CancellationToken cancellationToken);
    /// <summary>Prepares and reopens the candidate's device key binding without returning its secret.</summary>
    Task PrepareKeyAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken);
    /// <summary>Creates or revalidates the exact candidate from a coherent original snapshot, including all typed content.</summary>
    Task CreateAndVerifyCandidateAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken);
    /// <summary>Reopens and validates only the candidate with its captured key; missing keys never choose the original.</summary>
    Task ReopenCandidateAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken);
    /// <summary>Compare-and-commits one checkpoint durably; the next read must return it before cleanup can proceed.</summary>
    Task PersistCheckpointAsync(DatabaseGenerationCheckpoint expected, DatabaseGenerationCheckpoint next, CancellationToken cancellationToken);
    /// <summary>Deletes only manifest-owned obsolete plaintext and sidecars after durable candidate selection and keyed reopen.</summary>
    Task CleanupOriginalAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken);
}
