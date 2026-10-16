using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.ProtectedSync;

internal static partial class HistoryQualification
{
    /// <summary>Creates a separate current-contract fixture; synthetic signed receipts exercise native storage, never hosted acceptance.</summary>
    internal static async Task PrepareAsync(string root, Action<string> proof, CancellationToken ct)
    {
        var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
        using var client = await ProtectedClient.OpenAsync(Path.Combine(root, "client"), peer, true, ct);
        client.Services.MigrateLocalDatabase<ZananceDbContext>();
        var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            db.Accounts.AddRange(Enumerable.Range(0, 12000).Select(index => new Account { Name = "Fictitious current history " + index,
                CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 0 }));
            var start = new DateOnly(2026, 10, 16);
            db.Set<ForecastSnapshot>().Add(new ForecastSnapshot { Name = "Fictitious current large row", BaseDate = start, Horizon = start.AddDays(39999),
                CurrencyCode = "EUR", AccountIds = db.ChangeTracker.Entries<Account>().First().Entity.Id.ToString("D"),
                Path = string.Join(',', Enumerable.Repeat("1000000000000000", 40000)), Minimum = 1000000000000000, MinimumDate = start, UnknownCount = 0 });
            await db.SaveChangesAsync(ct);
        }
        const string identity = "fictitious-current-contract-history-owner";
        using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
        var journal = new HouseholdJournal(factory); var draft = await journal.BeginDraftAsync(peer, identity, owner.Public, true, device, owner, null, ct);
        var room = Guid.NewGuid(); var remoteDevice = Guid.NewGuid(); await journal.BindRemoteAsync(draft.Authority, room, remoteDevice, ct);
        using var epoch = HouseholdEpochMaterial.Create(); var operation = Guid.NewGuid(); await journal.StageEpochAsync(draft.Authority, operation, epoch, ct);
        var statement = new HouseholdMembershipStatement(1, room, operation, HouseholdMembershipChange.Genesis, null, 0, 1, new('0', 64), identity, identity,
            new(owner.Public.SigningKey, owner.Public.WrappingKey), 1, epoch.KeyCommitment, epoch.AddressKeyCommitment,
            TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds(),
            [new(identity, HouseholdRole.Owner, new('0', 64), [new(remoteDevice, new(device.Public.SigningKey, device.Public.WrappingKey))])]);
        var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement), owner,
            "zanance-household-membership+jwt");
        var grant = epoch.Grant(new(TimeProvider.System), statement, owner, statement.Members[0].Devices[0]);
        var proposal = new HouseholdPublishMembership(remoteDevice, signed, null, null, [new(1, remoteDevice, grant, HouseholdPublicationDigest.Ciphertext(grant))]);
        await journal.StageMembershipProposalAsync(draft.Authority, proposal, ct);
        var membership = await journal.RecordMembershipAsync(draft.Authority, [new(signed, null, null, HouseholdPublicationDigest.Grants(proposal.Grants))], ct);
        await journal.PromoteStagedEpochAsync(draft.Authority, operation, ct); await journal.CompleteMembershipProposalAsync(draft.Authority, ct);
        await journal.RecordAccessCheckpointAsync(draft.Authority, new(room, identity, 1, membership.Hash, 1, 0, new('0', 64), false,
            HouseholdRetentionState.Active, TimeProvider.System.GetUtcNow().ToUnixTimeSeconds()), ct);
        await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(ct), peer, identity, remoteDevice,
            new(room, "Fictitious current-contract native history", HouseholdFinancialSchema.Hash, 0, true), ct, transportKind: ProfileSynchronizationKind.Household);
        await SeedPublicationsAsync(root, client, journal, draft.Authority, membership, epoch, device, owner, proof, ct);
    }

    /// <summary>Continues only the already created original fixture after its connection precondition was corrected.</summary>
    internal static async Task ResumePreparationAsync(string root, Action<string> proof, CancellationToken ct)
    {
        var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
        using var client = await ProtectedClient.OpenAsync(Path.Combine(root, "client"), peer, false, ct);
        var journal = new HouseholdJournal(client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var state = await journal.CaptureAsync(peer, "fictitious-current-contract-history-owner", ct) ?? throw new InvalidDataException();
        if (state.Cursor != 0 || File.Exists(Path.Combine(root, "history-prepare.complete.json"))) throw new IOException("Preserve the existing publication state.");
        using var keys = await journal.ReadKeysAsync(state.Authority, ct);
        var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
        await journal.RecordAccessCheckpointAsync(state.Authority, new(state.SpaceId, state.Authority.IdentityId, membership.Version, membership.Hash, epoch.Epoch, 0,
            new('0', 64), false, HouseholdRetentionState.Active, TimeProvider.System.GetUtcNow().ToUnixTimeSeconds()), ct);
        await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(ct), peer, state.Authority.IdentityId, state.DeviceId,
            new(state.SpaceId, "Fictitious current-contract native history", HouseholdFinancialSchema.Hash, 0, true), ct, transportKind: ProfileSynchronizationKind.Household);
        await SeedPublicationsAsync(root, client, journal, state.Authority, membership, epoch, keys.Device, keys.Owner ?? throw new InvalidDataException(), proof, ct);
    }

    /// <summary>Only this new archive case emits synthetic signed fixture receipts, without network or a service acknowledgement.</summary>
    private static async Task SeedPublicationsAsync(string root, ProtectedClient client, HouseholdJournal journal, HouseholdJournalAuthority authority,
        HouseholdVerifiedMembership membership, HouseholdEpochMaterial epoch, SigningRecipientIdentity device, SigningRecipientIdentity owner, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Current-contract native history publication failed."); }
        var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
        var state = await journal.CaptureAsync(peer, authority.IdentityId, ct) ?? throw new InvalidDataException();
        var room = state.SpaceId; var remoteDevice = state.DeviceId;
        var attempt = (await journal.PrepareOriginalAsync(authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
        var firstParts = await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.SmallFixtureAsync(journal, authority, attempt, ct);
        using var protocol = new HouseholdFinancialReplica(room);
        await AppendAsync(attempt.Open, attempt.Commit, firstParts);
        proof("current-contract-12000-accounts-and-large-framed-row-from-original-protected-capture-sealed-and-accepted");
        for (var round = 2; round <= 8; round++)
        {
            var selected = protocol.Snapshot().Values.First(row => row.Record.Table == "ForecastSnapshots" && row.Values is not null);
            var value = JsonNode.Parse(selected.Values!.Value.GetRawText())!.AsObject(); value["Name"] = "Fictitious historical forecast " + round;
            var prepared = protocol.Prepare(membership, epoch, remoteDevice, device, Guid.NewGuid(), Guid.NewGuid(),
                [new(selected.Record, selected.Version, JsonSerializer.SerializeToElement(value))], [], new Dictionary<string, byte[]>(), new(TimeProvider.System));
            Require(prepared.Plan.Succeeded); await AppendAsync(prepared.Open, prepared.Commit, prepared.Parts);
        }
        await File.WriteAllTextAsync(Path.Combine(root, "history-prepare.complete.json"), JsonSerializer.Serialize(new { Schema = HouseholdFinancialSchema.Hash,
            Cursor = protocol.Cursor, FinancialCursor = protocol.FinancialCursor, RootFingerprint = owner.Public.Fingerprint, NoNetworkRequests = true }), ct);
        proof("eight-current-contract-original-signed-publications-retained-for-bounded-history-without-api-or-financial-application-claim");

        async Task AppendAsync(HouseholdOpenRound opening, HouseholdCommitRound commit, IReadOnlyList<HouseholdOpaquePart> parts)
        {
            var receipt = new HouseholdPublicationReceipt(opening.AttemptId, commit.SignedCommit,
                parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray());
            var events = parts.Select(part => new HouseholdOpaqueEvent(protocol.Cursor + 1, receipt.RoundId, part.RecordId, epoch.Epoch, membership.Version,
                remoteDevice, opening.OperationId, opening.AttemptId, part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray();
            await journal.StageInboxAsync(authority, receipt, events, ct); Require(await journal.AcceptInboxAsync(authority, ct));
            protocol.Apply(membership, epoch, receipt, events, new(TimeProvider.System));
        }
    }
}
