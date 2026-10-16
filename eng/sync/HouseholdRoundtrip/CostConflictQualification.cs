using System.Resources;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Original two-client cost conflict and actual common application review, retaining each completed mutation for continuation.</summary>
internal static class CostConflictQualification
{
    internal static async Task RunAsync(string root, LocalSyncPeer peer, IServiceProvider service, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The actual household cost conflict invariant failed."); }
        var progressPath = Path.Combine(root, "original-cost-conflict-progress.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(progressPath, ct)) ?? throw new IOException() : null;
        if (progress?.Phase == 6) throw new InvalidOperationException("Do not replay the completed original financial review.");
        var ownerRoot = Path.Combine(root, "recovered-owner"); var recipientRoot = Path.Combine(root, "recipient");
        using var owner = await ProtectedClient.OpenAsync(ownerRoot, peer, false, ct);
        using var recipient = await ProtectedClient.OpenAsync(recipientRoot, peer, false, ct);
        owner.Services.MigrateLocalDatabase<ZananceDbContext>(); recipient.Services.MigrateLocalDatabase<ZananceDbContext>();
        HouseholdServiceTransport Open(string path) => new(peer, new ProtectedSessions(path, () =>
        { if (File.ReadAllText(Path.Combine(path, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        using var local = Open(ownerRoot); using var remote = Open(recipientRoot);
        async Task<SyncIdentity> IdentityAsync(HouseholdServiceTransport transport, ProtectedClient client, string email)
        {
            try { return await transport.IdentityAsync(ct); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Unauthorized)
            {
                // Generated fixture passwords were deliberately never retained. Renew only this
                // marked fictional identity through the real local reset/outbox flow, not token fabrication.
                if (email is not ("household-native-cost-v2-owner@example.invalid" or "household-native-cost-v2-recipient@example.invalid")) throw;
                var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
                using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
                using var request = await http.PostAsJsonAsync("identity/recover", new IdentityInput(email, ""), ct);
                Require(request.StatusCode == HttpStatusCode.Accepted);
                await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(row => row.Email == email && row.EmailConfirmed, ct);
                var challenge = await db.Challenges.AsNoTracking().Where(row => row.UserId == user.Id && row.Purpose == "reset" && !row.Consumed)
                    .OrderByDescending(row => row.ExpiresAt).FirstAsync(ct);
                var mail = JsonSerializer.Deserialize<Vafadar.Zanance.Api.PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new IOException();
                Require(mail.Recipient == email);
                using var completed = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, password), ct);
                Require(completed.StatusCode == HttpStatusCode.NoContent);
                SyncIdentity signed;
                try { signed = await transport.SignInAsync(email, password, ct); }
                finally { password = ""; }
                Require(signed.Id == user.Id);
                var journal = client.Services.GetRequiredService<HouseholdJournal>();
                var existing = await journal.CaptureAsync(peer, signed.Id, ct) ?? throw new IOException();
                await client.Services.GetRequiredService<HouseholdWorkspace>().RebindCurrentDeviceAsync(transport, existing.Authority, ct);
                proof("original-fictitious-identity-session-renewed-through-real-reset-and-same-existing-device-possession-without-financial-or-key-reset");
                return signed;
            }
        }
        var ownIdentity = await IdentityAsync(local, owner, "household-native-cost-v2-owner@example.invalid");
        var remoteIdentity = await IdentityAsync(remote, recipient, "household-native-cost-v2-recipient@example.invalid");
        var ownJournal = owner.Services.GetRequiredService<HouseholdJournal>(); var remoteJournal = recipient.Services.GetRequiredService<HouseholdJournal>();
        var own = await ownJournal.CaptureAsync(peer, ownIdentity.Id, ct) ?? throw new IOException();
        var other = await remoteJournal.CaptureAsync(peer, remoteIdentity.Id, ct) ?? throw new IOException();
        var ownSync = owner.Services.GetRequiredService<HouseholdSyncCoordinator>(); var remoteSync = recipient.Services.GetRequiredService<HouseholdSyncCoordinator>();
        var ownCosts = owner.Services.GetRequiredService<HouseholdCostStore>(); var remoteCosts = recipient.Services.GetRequiredService<HouseholdCostStore>();
        async Task SaveProgressAsync(int phase)
        { progress = progress! with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct); }
        if (progress is null)
        {
            own = await ownSync.RefreshAuthorityAsync(local, own.Authority, ct);
            var roster = (await ownJournal.ReadCurrentMembershipAsync(own.Authority, ct)).Snapshot();
            if (roster.Members.Single(member => member.IdentityId == other.Authority.IdentityId).Role != HouseholdRole.Editor)
                await owner.Services.GetRequiredService<HouseholdWorkspace>().ChangeMemberAsync(local, own.Authority, roster.Version,
                    other.Authority.IdentityId, HouseholdRole.Editor, ct);
            Require((await ownSync.SynchronizeAsync(local, own.Authority, ct)).Applied);
            Require((await remoteSync.SynchronizeAsync(remote, other.Authority, ct)).Applied);
            var before = await ownCosts.ReadAsync(ct); var cost = before.Costs.Single(row => row.Kind == HouseholdCostKind.Expense);
            Require(before.CanWrite && (await remoteCosts.ReadAsync(ct)).CanWrite && before.Totals.Single().NetCost == 1001);
            progress = new(1, cost.Id, cost.Note);
            await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct);
            proof("actual-recovered-owner-and-approved-editor-aligned-before-new-cost-conflict");
        }
        async Task EditAsync(HouseholdCostStore costs, string? note)
        {
            var review = await costs.ReadAsync(ct); var cost = review.Costs.Single(row => row.Id == progress!.Cost);
            cost.Note = note; await costs.SaveCostAsync(review, cost, review.Allocations.Where(row => row.CostId == cost.Id).ToArray(), ct);
        }
        if (progress.Phase == 1)
        {
            await EditAsync(ownCosts, "Fictitious owner cost note for original conflict review");
            await EditAsync(remoteCosts, "Fictitious editor cost note for original conflict review");
            await SaveProgressAsync(2); proof("actual-two-client-complete-cost-share-group-edits-retained-without-cash-posting");
        }
        if (progress.Phase == 2)
        {
            Require((await ownSync.SynchronizeAsync(local, own.Authority, ct)).Applied);
            var result = await remoteSync.SynchronizeAsync(remote, other.Authority, ct);
            Require(!result.Applied && (await recipient.Sync.ConflictsAsync(ct)).Count != 0);
            await SaveProgressAsync(3); proof("original-encrypted-household-round-staged-complete-cost-conflict-for-explicit-review");
        }
        if (progress.Phase == 3)
        {
            var translator = new Translator(); translator.AddResources(new ResourceManager("HouseholdRoundtrip.Resources.AppResources", typeof(CostConflictQualification).Assembly));
            var localization = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), translator);
            var effects = new ReviewEffects(peer, recipient, ct); var dialogs = new ReviewDialogs();
            var page = new PersonalSyncViewModel(recipient.Sync, recipient.Coordinator, effects, dialogs, translator, new DateFormatter(localization), localization);
            await page.ActivateAsync(); Require(page.IsReady && page.IsHouseholdConnection && page.Error is null && !page.ShowPersonalConnection);
            foreach (var language in new[] { "en", "fa", "de" })
            {
                localization.SetLanguage(localization.SupportedLanguages.Single(item => item.CultureName == language));
                Require(page.ReviewItems.Count != 0 && page.ReviewItems.All(item => item.CanChooseRemote && item.Versions.Count != 0));
            }
            while (page.ReviewItems.Count != 0)
            {
                var item = page.ReviewItems.First(); Require(item.Review.Conflict.LinkedGroup == progress.Cost);
                dialogs.AcceptNext = true; await page.KeepRemoteCommand.ExecuteAsync(item.Review);
                Require(page.Error is null && !dialogs.AcceptNext);
            }
            Require(effects.ReadableOpens == 0 && dialogs.Confirmed > 0 && effects.Notifications == dialogs.Confirmed);
            page.Deactivate(); await SaveProgressAsync(4);
            proof("original-common-app-cost-review-preserves-complete-typed-groups-in-three-languages-and-records-explicit-remote-choice-with-no-readable-http");
        }
        if (progress.Phase == 4)
        {
            Require((await remoteSync.SynchronizeAsync(remote, other.Authority, ct)).Applied);
            var reviewed = await remoteCosts.ReadAsync(ct);
            Require(reviewed.Costs.Single(row => row.Id == progress.Cost).Note == "Fictitious owner cost note for original conflict review" && reviewed.Totals.Single().NetCost == 1001);
            await SaveProgressAsync(5); proof("reviewed-encrypted-cost-conflict-applied-on-original-native-editor-with-exact-shares-and-refund");
        }
        if (progress.Phase == 5)
        {
            await EditAsync(ownCosts, progress.OriginalNote);
            Require((await ownSync.SynchronizeAsync(local, own.Authority, ct)).Applied);
            Require((await remoteSync.SynchronizeAsync(remote, other.Authority, ct)).Applied);
            foreach (var client in new[] { owner, recipient })
            {
                var snapshot = await client.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
                Require(snapshot.Costs.Single(row => row.Id == progress.Cost).Note == progress.OriginalNote && snapshot.Totals.Single().NetCost == 1001 &&
                    snapshot.Costs.Count == 2 && snapshot.Allocations.Count == 4 && snapshot.Settlements.Count == 1);
                await using var db = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                Require(!await db.Entries.AnyAsync(ct));
            }
            await SaveProgressAsync(6); proof("original-cost-note-restored-on-both-native-clients-with-no-cash-entries-or-readable-financial-channel");
        }
    }

    private sealed record Progress(int Phase, Guid Cost, string? OriginalNote);
    private sealed class ReviewDialogs : IAppInteraction
    {
        internal bool AcceptNext;
        internal int Confirmed;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { if (!AcceptNext) throw new IOException("Unexpected financial confirmation."); AcceptNext = false; Confirmed++; return Task.FromResult(true); }
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException();
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException();
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException();
        public Task ShowFailureAsync(Exception exception) => throw new InvalidOperationException();
    }
    private sealed class ReviewEffects(LocalSyncPeer peer, ProtectedClient client, CancellationToken ct) : IPersonalSyncEnvironment
    {
        internal int ReadableOpens;
        internal int Notifications;
        public bool Available => true;
        public event EventHandler? StatusChanged { add { } remove { } }
        public PersonalSyncAccess Capture() => new(() => ct.ThrowIfCancellationRequested(), ct, "independent-original-native-cost-review");
        public PersonalSyncConnection ReadConnection() => new(peer.Address.AbsoluteUri, peer.Fingerprint);
        public PersonalSyncTransport Open(LocalSyncPeer target, Action verifyAccess) { ReadableOpens++; throw new InvalidOperationException(); }
        public Task PublishAppliedAsync(CancellationToken cancellation) { cancellation.ThrowIfCancellationRequested(); client.Store.ResetSession(); return Task.CompletedTask; }
        public void RememberPeer(LocalSyncPeer target) => throw new InvalidOperationException();
        public void ConnectionChanged() => Notifications++;
        public Guid CreationOperation(string digest) => throw new InvalidOperationException();
        public void CompleteCreation(string digest, Guid operation) => throw new InvalidOperationException();
        public Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int index, CancellationToken cancellation) => throw new InvalidOperationException();
    }
}
