using System.Text.Json;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Opaque service sessions belong only to platform SecureStorage, outside every portable preference source.</summary>
internal sealed class SecurePersonalSyncTokens(Vafadar.Core.Settings.ISettingsStore settings) : IPersonalSyncTokenStore
{
    private const string Prefix = "personal-sync.session.";
    /// <inheritdoc />
    public async Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); RequireKey(peer);
        if (settings.Get("personalSync.sessionRetired." + peer) == "true") return null;
        var encoded = await SecureStorage.Default.GetAsync(Prefix + peer); ct.ThrowIfCancellationRequested();
        if (encoded is null) return null;
        if (encoded.Length > 32768) throw new InvalidDataException("The protected personal session exceeds its bound.");
        var session = JsonSerializer.Deserialize(encoded, PersonalSyncSessionJsonContext.Default.PersonalSyncSession)
            ?? throw new InvalidDataException("The protected personal session is unavailable.");
        if (session.AccessToken is not { Length: > 0 and <= 8192 } || session.RefreshToken is not { Length: > 0 and <= 8192 })
            throw new InvalidDataException("The protected personal session is invalid.");
        return session;
    }
    /// <inheritdoc />
    public async Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); RequireKey(peer);
        var encoded = JsonSerializer.Serialize(session, PersonalSyncSessionJsonContext.Default.PersonalSyncSession);
        await SecureStorage.Default.SetAsync(Prefix + peer, encoded);
        if (await SecureStorage.Default.GetAsync(Prefix + peer) != encoded) throw new IOException("Original identity publication was not confirmed.");
        settings.Set("personalSync.sessionRetired." + peer, null);
        // A completed platform write stays durable even if the caller was canceled during it.
        ct.ThrowIfCancellationRequested();
    }
    /// <inheritdoc />
    public Task RemoveAsync(string peer, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); RequireKey(peer);
        // A failed credential erase must not let cached identity or an automatic worker resume a deliberately retired session.
        settings.Set("personalSync.sessionRetired." + peer, "true");
        SecureStorage.Default.Remove(Prefix + peer); return Task.CompletedTask;
    }
    private static void RequireKey(string peer)
    { if (!Core.Sync.SyncProtocolDigest.IsDigest(peer)) throw new ArgumentException("A reviewed personal peer identity is required."); }
}
