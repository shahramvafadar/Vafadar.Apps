namespace Vafadar.Data;

/// <summary>Coordinates resumable generation migration without assuming atomicity between keys, database files and the selector.</summary>
/// <remarks>Native sessions must provide durable compare-and-commit and exclusive writer ownership. This algorithm
/// does not create recovery policy or activate a provider. Callers resume writers only after successful keyed reopen.</remarks>
public static class DatabaseGenerationMigration
{
    /// <summary>Resumes the captured transition, verifies every persisted milestone and returns the selected candidate after keyed reopen.</summary>
    public static async Task<Guid> ResumeAsync(DatabaseGenerationCheckpoint captured,
        IDatabaseGenerationMigrationSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(captured);
        ArgumentNullException.ThrowIfNull(session);
        session.ValidateContext();
        var state = await session.ReadCheckpointAsync(cancellationToken).ConfigureAwait(false);
        RequireIdentity(captured, state);
        // A captured caller cannot resume an older durable state or reinterpret a rollback as success.
        if (state.Phase < captured.Phase) throw new InvalidOperationException("Migration checkpoint regressed.");
        session.ValidateContext();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            session.ValidateContext();
            switch (state.Phase)
            {
                case DatabaseGenerationPhase.Original:
                    await session.PrepareKeyAsync(state, cancellationToken).ConfigureAwait(false);
                    break;
                case DatabaseGenerationPhase.KeyPrepared:
                    await session.CreateAndVerifyCandidateAsync(state, cancellationToken).ConfigureAwait(false);
                    break;
                case DatabaseGenerationPhase.CandidateVerified:
                    await session.ReopenCandidateAsync(state, cancellationToken).ConfigureAwait(false);
                    break;
                case DatabaseGenerationPhase.Selected:
                    // Always reopen on restart. Losing the key after selection must stop before cleanup.
                    await session.ReopenCandidateAsync(state, cancellationToken).ConfigureAwait(false);
                    session.ValidateContext();
                    cancellationToken.ThrowIfCancellationRequested();
                    await session.CleanupOriginalAsync(state, cancellationToken).ConfigureAwait(false);
                    break;
                case DatabaseGenerationPhase.Completed:
                    await session.ReopenCandidateAsync(state, cancellationToken).ConfigureAwait(false);
                    session.ValidateContext();
                    cancellationToken.ThrowIfCancellationRequested();
                    return state.CandidateGeneration;
                default:
                    throw new InvalidOperationException("Unsupported migration checkpoint.");
            }
            session.ValidateContext();
            cancellationToken.ThrowIfCancellationRequested();
            var next = state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
            await session.PersistCheckpointAsync(state, next, cancellationToken).ConfigureAwait(false);
            session.ValidateContext();
            // A returned write is insufficient: confirm actual durable selection before any destructive effect.
            var reopened = await session.ReadCheckpointAsync(cancellationToken).ConfigureAwait(false);
            session.ValidateContext();
            if (reopened != next) throw new InvalidOperationException("Persisted migration checkpoint differs.");
            state = reopened;
        }
    }

    private static void RequireIdentity(DatabaseGenerationCheckpoint captured, DatabaseGenerationCheckpoint actual)
    {
        if (captured.ProfileId != actual.ProfileId || captured.OriginalGeneration != actual.OriginalGeneration ||
            captured.CandidateGeneration != actual.CandidateGeneration)
            throw new InvalidOperationException("Migration identity changed.");
    }
}
