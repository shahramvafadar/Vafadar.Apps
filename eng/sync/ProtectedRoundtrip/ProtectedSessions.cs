using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Current-user protected fictional sessions for an explicit second-process continuation; no password is persisted.</summary>
internal sealed class ProtectedSessions(string root, Action validate) : IPersonalSyncTokenStore
{
    private string FileOf(string peer)
    {
        validate();
        if (peer.Length != 64 || peer.Any(value => !char.IsAsciiHexDigit(value))) throw new ArgumentException("A captured peer digest is required.");
        return Path.Combine(root, "session-" + peer + ".protected");
    }

    /// <inheritdoc />
    public async Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct)
    {
        var path = FileOf(peer); if (!File.Exists(path)) return null;
        var info = new FileInfo(path); if (info.Length > 65536 || (info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Invalid private session storage.");
        var encrypted = await File.ReadAllBytesAsync(path, ct); byte[]? plain = null;
        try
        {
            plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<PersonalSyncSession>(plain) ?? throw new InvalidDataException("The fictional session is unavailable.");
        }
        finally { CryptographicOperations.ZeroMemory(encrypted); if (plain is not null) CryptographicOperations.ZeroMemory(plain); }
    }

    /// <inheritdoc />
    public async Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct)
    {
        var path = FileOf(peer); var plain = JsonSerializer.SerializeToUtf8Bytes(session); byte[]? encrypted = null;
        try
        {
            encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            // The exact operation rotates before the next request; a fictional process restart reuses only this wrapped session.
            await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough);
            await file.WriteAsync(encrypted, ct); await file.FlushAsync(ct); file.Flush(flushToDisk: true); validate();
        }
        finally { CryptographicOperations.ZeroMemory(plain); if (encrypted is not null) CryptographicOperations.ZeroMemory(encrypted); }
    }

    /// <inheritdoc />
    public Task RemoveAsync(string peer, CancellationToken ct) { ct.ThrowIfCancellationRequested(); File.Delete(FileOf(peer)); return Task.CompletedTask; }
}
