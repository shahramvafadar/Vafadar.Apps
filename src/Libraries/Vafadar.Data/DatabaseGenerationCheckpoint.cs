namespace Vafadar.Data;

/// <summary>Non-secret checkpoints in the final generation migration protocol; no state alone opens a database.</summary>
public enum DatabaseGenerationPhase
{
    /// <summary>The original generation remains authoritative.</summary>
    Original,
    /// <summary>The candidate key binding was durably prepared and reopened by its platform owner.</summary>
    KeyPrepared,
    /// <summary>The candidate was fully verified against its original snapshot.</summary>
    CandidateVerified,
    /// <summary>The durable selector names the candidate; original cleanup is still pending.</summary>
    Selected,
    /// <summary>Exact obsolete plaintext cleanup has completed.</summary>
    Completed
}

/// <summary>Describes one immutable, non-secret profile/generation transition for future durable storage.</summary>
/// <remarks>The storage coordinator must persist/reopen each checkpoint. This model never commits files or supplies recovery policy.</remarks>
public sealed record DatabaseGenerationCheckpoint
{
    /// <summary>Creates a transition bound to distinct existing/candidate generations and one profile.</summary>
    public DatabaseGenerationCheckpoint(Guid profileId, Guid originalGeneration, Guid candidateGeneration)
    {
        if (profileId == Guid.Empty || originalGeneration == Guid.Empty || candidateGeneration == Guid.Empty || originalGeneration == candidateGeneration)
            throw new ArgumentException("Migration requires a profile and distinct nonempty generations.");
        ProfileId = profileId;
        OriginalGeneration = originalGeneration;
        CandidateGeneration = candidateGeneration;
    }

    /// <summary>The immutable profile identity; never a path or display label.</summary>
    public Guid ProfileId { get; }
    /// <summary>The original generation kept authoritative until selector commit.</summary>
    public Guid OriginalGeneration { get; }
    /// <summary>The distinct new generation, never guessed from a filename.</summary>
    public Guid CandidateGeneration { get; }
    /// <summary>The last durably recorded protocol checkpoint.</summary>
    public DatabaseGenerationPhase Phase { get; private init; }
    /// <summary>True only after selector commit; it is not permission to fall back after a missing key.</summary>
    public bool CandidateSelected => Phase is DatabaseGenerationPhase.Selected or DatabaseGenerationPhase.Completed;
    /// <summary>The sole generation selected by this checkpoint, pending independent key/file verification.</summary>
    public Guid SelectedGeneration => CandidateSelected ? CandidateGeneration : OriginalGeneration;

    /// <summary>Advances exactly one already completed external checkpoint; rejects profile/generation drift and skipped states.</summary>
    public DatabaseGenerationCheckpoint Advance(Guid profileId, Guid candidateGeneration, DatabaseGenerationPhase next)
    {
        if (profileId != ProfileId || candidateGeneration != CandidateGeneration)
            throw new InvalidOperationException("Migration identity changed.");
        if (Phase == DatabaseGenerationPhase.Completed || next != Phase + 1)
            throw new InvalidOperationException("Migration checkpoint is out of order.");
        return this with { Phase = next };
    }
}
