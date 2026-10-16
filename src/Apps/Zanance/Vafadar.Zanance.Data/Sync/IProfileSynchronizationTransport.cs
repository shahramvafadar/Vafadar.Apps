using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Persisted explicit publication boundary; a household profile must never fall through to readable personal HTTP.</summary>
public enum ProfileSynchronizationKind
{
    /// <summary>Original identity-owned readable personal service protocol.</summary>
    Personal = 0,
    /// <summary>Client-verified household finance, published only as standard ciphertext.</summary>
    Household = 1,
}

/// <summary>Original capture/review/blob/apply orchestration; adapters preserve its financial semantics and exact durable local authority.</summary>
public interface IProfileSynchronizationTransport
{
    /// <summary>Explicit mode matched to the captured original profile binding before financial network work.</summary>
    ProfileSynchronizationKind Kind { get; }
    /// <summary>Exact reviewed endpoint/certificate identity.</summary>
    LocalSyncPeer Peer { get; }
    /// <summary>Actual maintained identity without parsing bearer credential contents.</summary>
    Task<SyncIdentity> IdentityAsync(CancellationToken ct = default);
    /// <summary>Opens or resumes one exact logical operation.</summary>
    Task<SyncRoundHandle> OpenAsync(Guid profile, SyncOpenRound input, CancellationToken ct = default);
    /// <summary>Stages one complete original proposal part through this adapter's explicit protection boundary.</summary>
    Task PutPartAsync(Guid profile, Guid device, Guid round, SyncRoundPart part, CancellationToken ct = default);
    /// <summary>Returns atomic acknowledgement or original explicit financial conflicts, never guessed amounts.</summary>
    Task<SyncCommitResult> CommitAsync(Guid profile, Guid device, Guid round, SyncCommitRound input, CancellationToken ct = default);
    /// <summary>Abandons only uncommitted original work.</summary>
    Task AbandonAsync(Guid profile, Guid device, Guid round, CancellationToken ct = default);
    /// <summary>Delivers a complete authenticated original financial sequence for the unchanged local apply path.</summary>
    Task<SyncFeedPage> FeedAsync(Guid profile, Guid device, long after, long through, CancellationToken ct = default);
    /// <summary>Declares an exact original bounded attachment.</summary>
    Task<SyncBlobStatus> DeclareBlobAsync(Guid profile, Guid device, string hash, long length, CancellationToken ct = default);
    /// <summary>Stages one owned original attachment piece through the adapter's explicit protection boundary.</summary>
    Task PutBlobAsync(Guid profile, Guid device, string hash, int index, byte[] bytes, CancellationToken ct = default);
    /// <summary>Returns an exact immutable original attachment manifest.</summary>
    Task<SyncBlob> CompleteBlobAsync(Guid profile, Guid device, string hash, CancellationToken ct = default);
    /// <summary>Returns only one authenticated original bounded attachment piece.</summary>
    Task<byte[]> ReadBlobAsync(Guid profile, Guid device, string hash, int index, CancellationToken ct = default);
}
