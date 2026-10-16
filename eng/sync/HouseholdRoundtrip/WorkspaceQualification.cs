using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Actual app-facing workspace operations across two independent protected clients and the real local service.</summary>
internal static class WorkspaceQualification
{
    /// <summary>Original workspace device-possession/session composition over its unchanged approved native key and financial horizon.</summary>
    internal static async Task RebindAsync(ProtectedClient client, HouseholdServiceTransport transport, Action<string> proof, CancellationToken ct)
    {
        var identity = await transport.IdentityAsync(ct); var journal = client.Services.GetRequiredService<HouseholdJournal>();
        var before = await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw new InvalidDataException();
        var after = await client.Services.GetRequiredService<HouseholdWorkspace>().RebindCurrentDeviceAsync(transport, before.Authority, ct);
        if (before.Authority != after.Authority || before.DeviceId != after.DeviceId || before.DeviceFingerprint != after.DeviceFingerprint ||
            before.PinnedRoot != after.PinnedRoot || before.MembershipVersion != after.MembershipVersion || before.MembershipHead != after.MembershipHead ||
            before.Cursor != after.Cursor || before.FinancialCursor != after.FinancialCursor || before.PublicationHead != after.PublicationHead ||
            (await journal.ReadAccessAsync(after.Authority, ct)) is not { Role: HouseholdRole.Owner, CanWrite: true })
            throw new InvalidDataException("Approved-device session reattachment changed membership, keys or finance.");
        proof("original-workspace-approved-private-device-session-proof-rebind-preserves-same-native-key-membership-and-financial-horizon");
    }
    /// <summary>Actual keyed factories switch during the identity request; the old empty-setup action cannot adopt the new profile.</summary>
    internal static async Task RefuseRetiredSetupAsync(string root, LocalSyncPeer peer, ProtectedClient original, Action<string> proof, CancellationToken ct)
    {
        var targetRoot = Path.Combine(root, "setup-switch-target");
        const string completed = "original-workspace-captures-generation-before-identity-HTTP-and-refuses-a-profile-switch-before-any-new-household-key-or-binding-write";
        if (File.Exists(Path.Combine(root, "checkpoints.txt")) && File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(completed))
            throw new IOException("Never repeat a completed setup-switch qualification.");
        using var target = await ProtectedClient.OpenAsync(targetRoot, peer, !Directory.Exists(targetRoot), ct);
        var first = original.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var second = target.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var factory = new SwitchingFactory(first); var journal = new HouseholdJournal(factory);
        var finances = new PersonalSyncStore(factory, original.Services.GetRequiredService<Vafadar.Zanance.Data.Commerce.ICommercialWriteAccessSource>(), TimeProvider.System);
        var workspace = new HouseholdWorkspace(journal, finances, new(finances, journal), TimeProvider.System);
        var switched = false; var ownerRoot = Path.Combine(root, "client");
        using var channel = new HouseholdServiceTransport(peer, new ProtectedSessions(ownerRoot, () =>
        { if (File.ReadAllText(Path.Combine(ownerRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }),
            verifyAccess: () => { if (!switched) { factory.Current = second; switched = true; } });
        var refused = false;
        try { _ = await workspace.BeginOwnerAsync(channel, ct); }
        catch (CryptographicException error) when (error.Message == "Household protected journal authority was refused.") { refused = true; }
        if (!refused || !switched) throw new InvalidDataException("A retired household setup adopted a different protected generation.");
        var identity = (await channel.IdentityAsync(ct)).Id;
        if (await target.Services.GetRequiredService<HouseholdJournal>().CaptureAsync(peer, identity, ct) is not null)
            throw new InvalidDataException("Retired setup left a household binding in the new protected profile.");
        await using var db = await second.CreateDbContextAsync(ct);
        if (await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM HouseholdLocalSecrets").SingleAsync(ct) != 0 || await db.Accounts.AnyAsync(ct))
            throw new InvalidDataException("Retired setup left private household material or financial rows in the new profile.");
        proof(completed);
    }

    /// <summary>Only the native factory-selection effect changes; every context/credential/SQLite transaction is original production code.</summary>
    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> current) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current { get; set; } = current;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Current.CreateDbContextAsync(cancellationToken);
    }
    /// <summary>Continues exact retained native setup/identity/finance; successful recorded phases do not create replacement accounts or invitations.</summary>
    internal static async Task RunAsync(string root, IServiceProvider service, LocalSyncPeer peer, ProtectedClient owner,
        HouseholdServiceTransport ownerTransport, Action<string> proof, CancellationToken ct)
    {
        var stage = "owner-original-workspace-creation";
        void Require(bool condition) { if (!condition) throw new InvalidDataException("The original app workspace invariant failed."); }
        bool Completed(string value) => File.Exists(Path.Combine(root, "checkpoints.txt")) && File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(value);
        try
        {
            var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>(); var journal = owner.Services.GetRequiredService<HouseholdJournal>();
            var coordinator = owner.Services.GetRequiredService<HouseholdSyncCoordinator>();
            var state = await workspace.BeginOwnerAsync(ownerTransport, ct);
            if (!Completed("workspace-original-genesis-response-loss-exact-replay"))
            {
                var original = state;
                if (await journal.ReadMembershipProposalAsync(state.Authority, ct) is null && state.MembershipVersion == 0)
                {
                    var lost = false;
                    try { _ = await workspace.CompleteOwnerAsync(ownerTransport, state.Authority, "Fictitious original household workspace", ct); }
                    catch (PersonalSyncHttpException error) { lost = error.Status == HttpStatusCode.ServiceUnavailable; }
                    Require(lost);
                }
                var pending = await journal.ReadMembershipProposalAsync(state.Authority, ct) ?? throw new InvalidDataException();
                state = await workspace.CompleteOwnerAsync(ownerTransport, state.Authority, "Fictitious original household workspace", ct);
                var history = await journal.ReadHistoryAsync(state.Authority, ct);
                Require(history.Count == 1 && history[0].SignedStatement == pending.SignedStatement && state.Authority == original.Authority &&
                    state.DeviceFingerprint == original.DeviceFingerprint && state.PinnedRoot == original.PinnedRoot &&
                    await journal.ReadMembershipProposalAsync(state.Authority, ct) is null);
                proof("workspace-original-genesis-response-loss-exact-replay");
            }
            stage = "owner-original-financial-seed";
            var factory = owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            Account? account; LedgerEntry? entry;
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                account = await db.Accounts.SingleOrDefaultAsync(row => row.Name == "Fictitious workspace EUR", ct);
                entry = await db.Entries.SingleOrDefaultAsync(row => row.Title == "Fictitious workspace expense", ct);
            }
            if (account is null)
            {
                account = new() { Name = "Fictitious workspace EUR", CurrencyCode = "EUR", OpeningBalance = 10_000, OpeningDate = new(2026, 10, 1) };
                await owner.Store.SaveAccountAsync(account, ct);
            }
            if (entry is null)
            {
                entry = new() { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 876, Date = new(2026, 10, 16), Title = "Fictitious workspace expense" };
                Require((await owner.Store.SaveEntryAsync(entry, ct)).Succeeded);
            }
            stage = "recipient-original-workspace-claim";
            var recipientRoot = Path.Combine(root, "recipient"); var fresh = !Directory.Exists(recipientRoot);
            using var recipient = await ProtectedClient.OpenAsync(recipientRoot, peer, fresh, ct);
            void ValidateRecipient() { if (File.ReadAllText(Path.Combine(recipientRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException("Original independent recipient required."); }
            using var recipientTransport = new HouseholdServiceTransport(peer, new ProtectedSessions(recipientRoot, ValidateRecipient));
            string identity;
            if (fresh)
            {
                const string email = "household-native-workspace-recipient@example.invalid";
                var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
                using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
                using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct);
                Require(registration.StatusCode == HttpStatusCode.Accepted);
                await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(row => row.Email == email, ct);
                var challenge = await db.Challenges.SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
                Require(confirmation.StatusCode == HttpStatusCode.NoContent);
                identity = (await recipientTransport.SignInAsync(email, password, ct)).Id; password = "";
            }
            else identity = (await recipientTransport.IdentityAsync(ct)).Id;
            var recipientWorkspace = recipient.Services.GetRequiredService<HouseholdWorkspace>();
            var recipientJournal = recipient.Services.GetRequiredService<HouseholdJournal>();
            var admitted = await recipientJournal.CaptureAsync(peer, identity, ct);
            if (admitted is null || admitted.MembershipVersion == 0)
            {
                var package = await workspace.InviteAsync(ownerTransport, state.Authority, HouseholdRole.Viewer, ct);
                admitted = await recipientWorkspace.ClaimAsync(recipientTransport, package, state.PinnedRoot.Fingerprint, ct);
                Require(!await recipientWorkspace.CompleteJoinAsync(recipientTransport, admitted.Authority, "Fictitious joined household", ct));
                var pending = (await ownerTransport.PendingClaimsAsync(state.SpaceId, state.DeviceId, ct)).Single();
                Require(pending.IdentityId == identity && pending.DeviceId == admitted.DeviceId);
                await workspace.ApproveAsync(ownerTransport, state.Authority, pending, admitted.DeviceFingerprint, ct);
                Require(await recipientWorkspace.CompleteJoinAsync(recipientTransport, admitted.Authority, "Fictitious joined household", ct));
            }
            if (!Completed("workspace-original-two-sided-admission-viewer-receive-and-owner-only-pending-list"))
            {
                Require((await coordinator.SynchronizeAsync(ownerTransport, state.Authority, ct)).Applied);
                Require((await recipient.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(recipientTransport, admitted.Authority, ct)).Applied);
                Require((await recipient.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 876);
                Require((await recipientJournal.ReadAccessAsync(admitted.Authority, ct)) is { Role: HouseholdRole.Viewer, CanWrite: false, BlockedBy: HouseholdWriteBlockReason.Viewer });
                var refused = false;
                try { _ = await recipientTransport.PendingClaimsAsync(state.SpaceId, admitted.DeviceId, ct); }
                catch (PersonalSyncHttpException error) { refused = error.Status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden; }
                Require(refused);
                proof("workspace-original-two-sided-admission-viewer-receive-and-owner-only-pending-list");
            }
            stage = "original-app-workspace-member-role-publication";
            state = await coordinator.RefreshAuthorityAsync(ownerTransport, state.Authority, ct);
            var membership = (await journal.ReadCurrentMembershipAsync(state.Authority, ct)).Snapshot();
            if (membership.Members.Single(row => row.IdentityId == identity).Role == HouseholdRole.Viewer)
                await workspace.ChangeMemberAsync(ownerTransport, state.Authority, membership.Version, identity, HouseholdRole.Editor, ct);
            Require(await recipientWorkspace.CompleteJoinAsync(recipientTransport, admitted.Authority, "Fictitious joined household", ct));
            Require((await recipientJournal.ReadAccessAsync(admitted.Authority, ct)) is { Role: HouseholdRole.Editor, CanWrite: true });
            var recipientFactory = recipient.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); Account? added;
            await using (var db = await recipientFactory.CreateDbContextAsync(ct)) added = await db.Accounts.SingleOrDefaultAsync(row => row.Name == "Fictitious workspace editor account", ct);
            if (added is null) await recipient.Store.SaveAccountAsync(new() { Name = "Fictitious workspace editor account", CurrencyCode = "KWD", OpeningDate = new(2026, 10, 16) }, ct);
            Require((await recipient.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(recipientTransport, admitted.Authority, ct)).Applied);
            Require((await coordinator.SynchronizeAsync(ownerTransport, state.Authority, ct)).Applied);
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                Require(await db.Accounts.CountAsync(ct) == 2 && await db.Accounts.AnyAsync(row => row.CurrencyCode == "KWD", ct));
                Require((await db.Entries.SingleAsync(ct)).Amount == 876);
            }
            proof("workspace-original-role-change-editor-financial-round-and-owner-receive-pass");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("WORKSPACE-STAGE " + stage);
            Console.Error.WriteLine(string.Join(" > ", new System.Diagnostics.StackTrace(error, true).GetFrames()
                .Where(frame => frame.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Vafadar", StringComparison.Ordinal) == true)
                .Select(frame => frame.GetMethod()!.DeclaringType!.Name + ":" + frame.GetFileLineNumber())));
            throw;
        }
    }
}
