using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Captured foreground/unlocked/profile authority, independent of native controls.</summary>
public sealed record PersonalSyncAccess(Action Validate, CancellationToken Cancellation, string? Scope = null);
/// <summary>Public endpoint information only; never an identity password or service credential.</summary>
public sealed record PersonalSyncConnection(string Address, string Fingerprint);

/// <summary>One bounded native-rendered attachment page; its transient bytes are cleared when the inline preview closes.</summary>
public sealed record PersonalSyncRenderedPage(int PageCount, byte[] Data, int Width = 1, int Height = 1) : IDisposable
{
    /// <inheritdoc />
    public void Dispose() => System.Security.Cryptography.CryptographicOperations.ZeroMemory(Data);
}

/// <summary>Explicit native credential/lifetime effects used by the actual personal sync application flow.</summary>
public interface IPersonalSyncEnvironment
{
    /// <summary>Signals content-free local status changes on the native presentation thread.</summary>
    event EventHandler? StatusChanged;
    /// <summary>Whether the selected build permits the reviewed local network feature.</summary>
    bool Available { get; }
    /// <summary>Reads only saved public connection information, with a platform-specific local default.</summary>
    PersonalSyncConnection ReadConnection();
    /// <summary>Remembers a reviewed public peer after successful sign-in; never opts a profile in.</summary>
    void RememberPeer(LocalSyncPeer peer);
    /// <summary>Captures original unlocked foreground/profile lifetime before a dialog or network request.</summary>
    PersonalSyncAccess Capture();
    /// <summary>Constructs an endpoint-pinned transport backed by native secure token storage.</summary>
    PersonalSyncTransport Open(LocalSyncPeer peer, Action verifyAccess);
    /// <summary>Publishes an actual committed change to the original app services after local ownership ended.</summary>
    Task PublishAppliedAsync(CancellationToken ct);
    /// <summary>Signals that an explicitly connected profile can participate in future foreground sync.</summary>
    void ConnectionChanged();
    /// <summary>Retains a public creation nonce across a lost acknowledgement and process restart.</summary>
    Guid CreationOperation(string requestDigest);
    /// <summary>Forgets only the successfully connected exact creation intent.</summary>
    void CompleteCreation(string requestDigest, Guid operation);
    /// <summary>Renders exactly one image/PDF page locally; no external viewer, network or financial write is permitted.</summary>
    Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int pageIndex, CancellationToken ct);
}
