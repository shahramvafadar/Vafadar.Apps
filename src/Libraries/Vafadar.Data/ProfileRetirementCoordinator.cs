namespace Vafadar.Data;

/// <summary>Resumes one existing reviewed retirement without inferring authority from absent metadata.</summary>
/// <remarks>The native host supplies exclusive maintenance ownership and verified idempotent effects.
/// The coordinator does not create requests, enumerate artifacts, remove files or invent recovery keys.</remarks>
public sealed class ProfileRetirementCoordinator(SqliteProfileRetirementJournal journal,
    IDatabaseGenerationFileDurability durability, IProfileRetirementEffects effects)
{
    private readonly SemaphoreSlim operations = new(1, 1);

    /// <summary>Requalifies native barriers and continues remaining actual effects in their required order.</summary>
    public async Task ResumeAsync(CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(journal); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(effects);
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            while (true)
            {
                var state = await journal.SynchronizeAndReadAsync(durability, ct).ConfigureAwait(false);
                await effects.ValidateAsync(state, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                // Recheck after host validation: no stale phase may authorize its next native effect.
                if (await journal.ReadAsync(ct).ConfigureAwait(false) != state)
                    throw new IOException("Retirement changed during host validation.");
                switch (state.Phase)
                {
                    case ProfileRetirementPhase.Authorized:
                        await effects.RemoveCatalogueAsync(state, ct).ConfigureAwait(false); break;
                    case ProfileRetirementPhase.CatalogueRemoved:
                        await effects.RemoveArtifactsAsync(state, ct).ConfigureAwait(false); break;
                    case ProfileRetirementPhase.ArtifactsRemoved:
                        await effects.RetireIdentityAsync(state, ct).ConfigureAwait(false); break;
                    case ProfileRetirementPhase.Completed: return;
                    default: throw new IOException("Unsupported retirement barrier.");
                }
                // A crash between effect and journal commit repeats only the host's idempotent operation.
                await effects.ValidateAsync(state.Advance(), ct).ConfigureAwait(false);
                await journal.CompareAndAdvanceAsync(state, durability, ct).ConfigureAwait(false);
            }
        }
        finally { operations.Release(); }
    }
}
