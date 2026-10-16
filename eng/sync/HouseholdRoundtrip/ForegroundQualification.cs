using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Original application foreground coordinator against an actual retained native household, not a replacement sync algorithm.</summary>
internal static class ForegroundQualification
{
    /// <summary>Publishes one original protected edit only through the encrypted channel and applies original native-effect feedback.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, ProtectedClient client, Action<string> proof, CancellationToken ct)
    {
        var location = Path.Combine(root, "client");
        void Validate() { if (File.ReadAllText(Path.Combine(location, "client.marker")) != "Fictitious protected sync client v1") throw new IOException("Original protected foreground client required."); }
        var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
            if (await db.Accounts.AnyAsync(row => row.Name == "Fictitious native foreground edit", ct))
                throw new IOException("Never repeat an already submitted foreground qualification edit.");
        await client.Store.SaveAccountAsync(new Account { Name = "Fictitious native foreground edit", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) }, ct);
        var environment = new NativeEffects(peer, client, ct, Validate);
        var foreground = new PersonalSyncForegroundCoordinator(client.Sync, client.Coordinator, environment, TimeProvider.System,
            client.Services.GetRequiredService<HouseholdJournal>(), client.Services.GetRequiredService<HouseholdSyncCoordinator>(), new ProtectedSessions(location, Validate));
        var changes = 0; foreground.Changed += (_, _) => changes++;
        await foreground.RunAsync(true, ct);
        var status = await client.Sync.StatusAsync(ct);
        if (environment.ReadableOpens != 0 || environment.Applied != 1 || changes != 1 || status.Failure is not null || status.LastSuccessAt is null)
            throw new InvalidDataException("The original household foreground routing or applied feedback failed.");
        await foreground.RunAsync(true, ct);
        if (environment.Applied != 1 || changes != 1 || environment.ReadableOpens != 0)
            throw new InvalidDataException("Immediate duplicate foreground scheduling did not retain its original delay.");
        proof("original-app-foreground-coordinator-publishes-protected-household-edit-with-zero-readable-transport-opens-and-one-applied-notification");
    }

    /// <summary>Only native access/configuration/publication effects are supplied; all capture, crypto, HTTP and apply work is original production code.</summary>
    internal sealed class NativeEffects(LocalSyncPeer peer, ProtectedClient client, CancellationToken cancellation, Action validate) : IPersonalSyncEnvironment
    {
        internal int ReadableOpens;
        internal int Applied;
        public bool Available => true;
        public event EventHandler? StatusChanged { add { } remove { } }
        public PersonalSyncAccess Capture() => new(() => { cancellation.ThrowIfCancellationRequested(); validate(); }, cancellation, "independent-protected-household");
        public PersonalSyncConnection ReadConnection() => new(peer.Address.AbsoluteUri, peer.Fingerprint);
        public PersonalSyncTransport Open(LocalSyncPeer target, Action verifyAccess) { ReadableOpens++; throw new InvalidOperationException("Household finance must never select readable personal HTTP."); }
        public Task PublishAppliedAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); Applied++; client.Store.ResetSession(); return Task.CompletedTask; }
        public void RememberPeer(LocalSyncPeer target) => throw new InvalidOperationException();
        public void ConnectionChanged() => throw new InvalidOperationException();
        public Guid CreationOperation(string digest) => throw new InvalidOperationException();
        public void CompleteCreation(string digest, Guid operation) => throw new InvalidOperationException();
        public Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int index, CancellationToken ct) => throw new InvalidOperationException();
    }
}
