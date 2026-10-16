using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Actual original cost writers, complete encrypted family groups and exact native receipts over two retained production clients.</summary>
internal static class CostQualification
{
    internal static async Task RunAsync(string root, IServiceProvider service, LocalSyncPeer peer, ProtectedClient owner,
        HouseholdServiceTransport transport, Action<string> proof, CancellationToken ct)
    {
        var stage = "original-cost-workspace";
        void Require(bool value) { if (!value) throw new InvalidDataException("The original native household cost invariant failed."); }
        bool Done(string value) => File.Exists(Path.Combine(root, "checkpoints.txt")) && File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(value);
        try
        {
            var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>();
            var journal = owner.Services.GetRequiredService<HouseholdJournal>();
            var state = await workspace.BeginOwnerAsync(transport, ct);
            if (state.MembershipVersion == 0) state = await workspace.CompleteOwnerAsync(transport, state.Authority, "Fictitious full household costs", ct);
            var store = owner.Services.GetRequiredService<HouseholdCostStore>();
            var coordinator = owner.Services.GetRequiredService<HouseholdSyncCoordinator>();
            const string original = "native-original-costs-shares-refund-and-private-repayment-save-with-zero-cash-postings";
            if (!Done(original))
            {
                stage = "original-cost-writer";
                var review = await store.ReadAsync(ct);
                var first = review.Contacts.SingleOrDefault(row => row.Name == "Fictitious cost payer") ?? new HouseholdContact { Name = "Fictitious cost payer" };
                if (!review.Contacts.Any(row => row.Id == first.Id)) await store.SaveContactAsync(review, first, ct);
                review = await store.ReadAsync(ct);
                var second = review.Contacts.SingleOrDefault(row => row.Name == "Fictitious cost recipient") ?? new HouseholdContact { Name = "Fictitious cost recipient" };
                if (!review.Contacts.Any(row => row.Id == second.Id)) await store.SaveContactAsync(review, second, ct);
                review = await store.ReadAsync(ct);
                var cost = review.Costs.SingleOrDefault(row => row.Kind == HouseholdCostKind.Expense) ?? new HouseholdCost
                { Title = "Fictitious exact actual cost", Kind = HouseholdCostKind.Expense, Date = new(2026, 10, 16), Amount = 1001,
                    CurrencyCode = "KWD", PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = first.Id };
                if (!review.Costs.Any(row => row.Id == cost.Id))
                    await store.SaveCostAsync(review, cost, [Share(cost, first, 333), Share(cost, second, 668)], ct);
                review = await store.ReadAsync(ct);
                var refund = review.Costs.SingleOrDefault(row => row.Kind == HouseholdCostKind.Refund) ?? new HouseholdCost
                { Title = "Fictitious actual refund", Kind = HouseholdCostKind.Refund, Date = cost.Date, Amount = 100, CurrencyCode = "KWD",
                    PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = first.Id, RefundOfCostId = cost.Id };
                if (!review.Costs.Any(row => row.Id == refund.Id))
                    await store.SaveCostAsync(review, refund, [Share(refund, first, 33), Share(refund, second, 67)], ct);
                review = await store.ReadAsync(ct);
                if (review.Settlements.Count == 0)
                    await store.SaveSettlementAsync(review, new() { FromContactId = second.Id, ToContactId = first.Id, Date = cost.Date,
                        Amount = 501, CurrencyCode = "KWD" }, ct);
                review = await store.ReadAsync(ct);
                Require(review.Totals.Single().NetCost == 901 && review.Members.Single(row => row.ContactId == first.Id).PrivateAdvanceBalance == 100);
                await using var db = await owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                Require(!await db.Entries.AnyAsync(ct)); proof(original);
            }
            const string invalid = "native-cost-partial-sum-and-stale-review-refusals-preserve-complete-previous-finance";
            if (!Done(invalid))
            {
                stage = "native-cost-review-refusals";
                var review = await store.ReadAsync(ct); var cost = review.Costs.Single(row => row.Kind == HouseholdCostKind.Expense);
                var shares = review.Allocations.Where(row => row.CostId == cost.Id).Select(row => new HouseholdCostAllocation
                { CostId = row.CostId, ContactId = row.ContactId, Amount = row.Amount }).ToArray();
                var amount = cost.Amount; cost.Amount++;
                var refused = false;
                try { await store.SaveCostAsync(review, cost, shares, ct); }
                catch (HouseholdCostRejectedException error) { refused = error.Issues.Any(issue => issue.Code == "HouseholdAllocationSumMismatch"); }
                Require(refused && (await store.ReadAsync(ct)).Costs.Single(row => row.Id == cost.Id).Amount == amount);
                cost.Amount = amount; cost.Note = "Fictitious first reviewed correction"; await store.SaveCostAsync(review, cost, shares, ct);
                refused = false;
                try { await store.SaveCostAsync(review, cost, shares, ct); } catch (InvalidOperationException error)
                { refused = error.Message == "The household costs changed after review."; }
                Require(refused); proof(invalid);
            }
            stage = "encrypted-complete-cost-publication";
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            var recipientRoot = Path.Combine(root, "recipient"); var fresh = !Directory.Exists(recipientRoot);
            using var recipient = await ProtectedClient.OpenAsync(recipientRoot, peer, fresh, ct);
            void ValidateRecipient() { if (File.ReadAllText(Path.Combine(recipientRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }
            using var remote = new HouseholdServiceTransport(peer, new ProtectedSessions(recipientRoot, ValidateRecipient));
            string identity;
            if (fresh)
            {
                const string email = "household-native-cost-v2-recipient@example.invalid";
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
                identity = (await remote.SignInAsync(email, password, ct)).Id; password = "";
            }
            else identity = (await remote.IdentityAsync(ct)).Id;
            var recipientJournal = recipient.Services.GetRequiredService<HouseholdJournal>();
            var recipientWorkspace = recipient.Services.GetRequiredService<HouseholdWorkspace>();
            var admitted = await recipientJournal.CaptureAsync(peer, identity, ct);
            if (admitted is null || admitted.MembershipVersion == 0)
            {
                var package = await workspace.InviteAsync(transport, state.Authority, HouseholdRole.Editor, ct);
                admitted = await recipientWorkspace.ClaimAsync(remote, package, state.PinnedRoot.Fingerprint, ct);
                var pending = (await transport.PendingClaimsAsync(state.SpaceId, state.DeviceId, ct)).Single(row => row.DeviceId == admitted.DeviceId);
                await workspace.ApproveAsync(transport, state.Authority, pending, admitted.DeviceFingerprint, ct);
                Require(await recipientWorkspace.CompleteJoinAsync(remote, admitted.Authority, "Fictitious cost household", ct));
            }
            var recipientCoordinator = recipient.Services.GetRequiredService<HouseholdSyncCoordinator>();
            var recipientStore = recipient.Services.GetRequiredService<HouseholdCostStore>();
            const string received = "native-two-client-full-household-contract-receives-all-cost-families-without-readable-personal-http";
            if (!Done(received))
            {
                stage = "native-full-cost-reception";
                Require((await recipientCoordinator.SynchronizeAsync(remote, admitted.Authority, ct)).Applied);
                var snapshot = await recipientStore.ReadAsync(ct);
                Require(snapshot.Contacts.Count == 2 && snapshot.Costs.Count == 2 && snapshot.Allocations.Count == 4 && snapshot.Settlements.Count == 1 &&
                    snapshot.Totals.Single().NetCost == 901);
                proof(received);
            }
            const string corrected = "native-editor-cost-and-complete-shares-publish-back-once-with-refund-and-repayment-preserved";
            if (!Done(corrected))
            {
                stage = "native-remote-cost-group-edit";
                var review = await recipientStore.ReadAsync(ct); var cost = review.Costs.Single(row => row.Kind == HouseholdCostKind.Expense);
                if (cost.Amount == 1001)
                {
                    cost.Amount = 1101;
                    var shares = review.Allocations.Where(row => row.CostId == cost.Id).Select(row => new HouseholdCostAllocation
                    { CostId = row.CostId, ContactId = row.ContactId, Amount = row.Amount == 668 ? 768 : row.Amount }).ToArray();
                    await recipientStore.SaveCostAsync(review, cost, shares, ct);
                }
                Require((await recipientCoordinator.SynchronizeAsync(remote, admitted.Authority, ct)).Applied);
                Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
                review = await store.ReadAsync(ct);
                Require(review.Totals.Single().NetCost == 1001 && review.Costs.Count == 2 && review.Allocations.Count == 4 && review.Settlements.Count == 1);
                proof(corrected);
            }
            Console.WriteLine("full-cost-contract-native-qualification-complete");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("COST-STAGE " + stage);
            Console.Error.WriteLine(string.Join(" > ", new System.Diagnostics.StackTrace(error, true).GetFrames()
                .Where(frame => frame.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Vafadar", StringComparison.Ordinal) == true)
                .Select(frame => frame.GetMethod()!.DeclaringType!.Name + ":" + frame.GetFileLineNumber())));
            throw;
        }
    }
    private static HouseholdCostAllocation Share(HouseholdCost cost, HouseholdContact contact, long amount) =>
        new() { CostId = cost.Id, ContactId = contact.Id, Amount = amount };
}
