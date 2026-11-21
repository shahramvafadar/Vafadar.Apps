using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>The pending account-deletion acknowledgement returned by the original service.</summary>
public sealed record AccountDeletionReceipt(Guid ReceiptId, DateTimeOffset RequestedAt);

/// <summary>Fresh password proof; never persisted or written to diagnostics.</summary>
internal sealed record AccountDeletionProof(string Password)
{
    /// <inheritdoc />
    public override string ToString() => "Unretained account deletion proof";
}

public sealed partial class PersonalSyncTransport
{
    /// <summary>Looks up the original account's pending request, if it exists.</summary>
    public Task<AccountDeletionReceipt?> ReadAccountDeletionAsync(string expectedUser, CancellationToken ct = default) =>
        AccountDeletionAsync(expectedUser, null, ct);

    /// <summary>Submits one request for the original account. The acknowledgement is a pending receipt.</summary>
    public async Task<AccountDeletionReceipt> RequestAccountDeletionAsync(string expectedUser, string password, CancellationToken ct = default)
    {
        if (password is not { Length: >= 12 and <= 256 })
            throw new ArgumentException("Fresh account credential proof is required.", nameof(password));
        return await AccountDeletionAsync(expectedUser, new(password), ct)
            ?? throw new InvalidDataException("The deletion request did not return a receipt.");
    }

    private async Task<AccountDeletionReceipt?> AccountDeletionAsync(string expectedUser, AccountDeletionProof? proof, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(expectedUser) || expectedUser.Length > 256)
            throw new InvalidDataException("An original account is required.");
        await requests.WaitAsync(ct);
        try
        {
            verifyAccess?.Invoke();
            var retained = await tokens.ReadAsync(credentialKey, ct);
            ct.ThrowIfCancellationRequested();
            verifyAccess?.Invoke();
            // Do not renew another account's credentials or send a password after the original session changed.
            if (!string.Equals(retained?.UserId, expectedUser, StringComparison.Ordinal))
                throw new InvalidDataException("The original account changed.");
            var session = await SessionAsync(ct);
            ct.ThrowIfCancellationRequested();
            verifyAccess?.Invoke();
            if (!string.Equals(session.UserId, expectedUser, StringComparison.Ordinal))
                throw new InvalidDataException("The original account changed.");

            using var request = Request(proof is null ? HttpMethod.Get : HttpMethod.Post, "identity/deletion-request",
                proof is null ? null : JsonContent.Create(proof, SyncCredentialJsonContext.Default.AccountDeletionProof), session);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            Demand(response);
            ct.ThrowIfCancellationRequested();
            verifyAccess?.Invoke();
            if (proof is null && response.StatusCode == HttpStatusCode.NoContent) return null;
            if (response.StatusCode != (proof is null ? HttpStatusCode.OK : HttpStatusCode.Accepted))
                throw new InvalidDataException("The account deletion response is invalid.");

            var bytes = await ReadBoundedAsync(response.Content, 4096, ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                verifyAccess?.Invoke();
                var receipt = JsonSerializer.Deserialize(bytes, SyncCredentialJsonContext.Default.AccountDeletionReceipt);
                if (receipt is null || receipt.ReceiptId == Guid.Empty || receipt.RequestedAt == default || receipt.RequestedAt.Offset != TimeSpan.Zero)
                    throw new InvalidDataException("The pending deletion receipt is invalid.");
                return receipt;
            }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
        }
        finally { requests.Release(); }
    }
}
