using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Data.Sync;

/// <summary>Owns only one fictional local identity for the original independently identified app source-copy flow.</summary>
internal static class ApplicationSourceServer
{
    /// <summary>Serves one bounded original app run and accepts only its explicit native completion marker.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, IServiceProvider service, bool fresh,
        Action<string> proof, CancellationToken ct, bool management = false, bool rootReview = false, bool deviceAccess = false, bool ownershipReview = false, bool lifecycleReview = false)
    {
        const string email = "household-native-app-source@example.invalid";
        var path = Path.Combine(root, "fictitious-app-identity.protected");
        var done = Path.Combine(root, "original-app-source-flow.complete");
        if (management)
        {
            if (fresh || !File.Exists(done) || await File.ReadAllTextAsync(done, ct) != "Original protected app source-copy and encrypted publication verified v1")
                throw new IOException("The completed original source fixture is required; do not recreate its identity.");
            var managementDone = Path.Combine(root, lifecycleReview ? "original-app-lifecycle-status.complete" : ownershipReview ? "original-app-ownership-status.complete" : deviceAccess ? "original-app-device-access.complete" : rootReview ? "original-app-root-management.complete" : "original-app-management.complete");
            if (File.Exists(managementDone)) throw new IOException("Completed native management review cannot be repeated.");
            proof("original-source-host-resumed-only-for-new-native-device-management-review");
            while (!File.Exists(managementDone)) await Task.Delay(500, ct);
            if (await File.ReadAllTextAsync(managementDone, ct) != "Original household device management verified v1") throw new IOException();
            if (rootReview)
            {
                await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var owner = await db.Users.SingleAsync(row => row.Email == email, ct);
                var room = await db.Set<HouseholdSpaceRow>().SingleAsync(row => row.OwnerId == owner.Id && !row.Deleted, ct);
                var invitations = await db.Set<HouseholdInvitationRow>().Where(row => row.SpaceId == room.Id).ToArrayAsync(ct);
                if (invitations.Length != 1 || invitations.Any(row => row.Accepted || !row.Revoked) ||
                    !await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.SpaceId == room.Id && !row.Approved && !row.Revoked && row.AdmissionProof != null, ct))
                    throw new IOException("Retired-root invitations must expire while independent accepted-member self-enrollment survives.");
                proof("retired-root-unaccepted-invitation-revoked-and-original-independent-pending-device-preserved");
            }
            proof("original-app-management-native-completion-observed");
            return;
        }
        if (File.Exists(done)) throw new InvalidOperationException("The completed original app flow cannot be repeated.");
        if (fresh)
        {
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
            using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct);
            if (registration.StatusCode != HttpStatusCode.Accepted) throw new IOException("Fictional local registration failed.");
            await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var user = await db.Users.SingleAsync(row => row.Email == email, ct);
            var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
            var mail = JsonSerializer.Deserialize<Vafadar.Zanance.Api.PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new IOException();
            using var accepted = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
            if (accepted.StatusCode != HttpStatusCode.NoContent) throw new IOException("Fictional local confirmation failed.");
            var clear = JsonSerializer.SerializeToUtf8Bytes(new FictitiousAppIdentity(1, peer.Address.AbsoluteUri, peer.Fingerprint,
                email, password, TimeProvider.System.GetUtcNow().AddMinutes(12).ToUnixTimeSeconds()));
            byte[]? wrapped = null;
            try
            {
                wrapped = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
                await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await file.WriteAsync(wrapped, ct); file.Flush(true);
            }
            finally { password = ""; CryptographicOperations.ZeroMemory(clear); if (wrapped is not null) CryptographicOperations.ZeroMemory(wrapped); }
        }
        else if (!File.Exists(path)) throw new IOException("Retain the original fictional app identity and actual session; no automatic replacement is allowed.");
        proof("original-app-source-local-host-ready-with-current-user-protected-fictional-identity-only");
        while (!File.Exists(done)) await Task.Delay(500, ct);
        if (await File.ReadAllTextAsync(done, ct) != "Original protected app source-copy and encrypted publication verified v1")
            throw new IOException("Original app completion evidence is invalid.");
        proof("original-app-source-copy-native-completion-observed-without-readable-financial-http");
    }

    private sealed record FictitiousAppIdentity(int Format, string Address, string Fingerprint, string Email, string Password, long ExpiresAt)
    {
        /// <inheritdoc />
        public override string ToString() => nameof(FictitiousAppIdentity);
    }
}
