using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

public sealed partial class HouseholdServiceAcceptanceTests
{
    /// <summary>Even a currently authorized real signer cannot partially apply invalid money, duplicate encrypted JSON, incomplete manifests or replayed transactions.</summary>
    [Fact]
    public async Task Signed_invalid_financial_packets_preserve_the_complete_replica_and_original_SQLite_data()
    {
        var root = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_REPLICA_TEST_ROOT") ?? throw new InvalidOperationException("An independently owned refusal fixture is required.");
        Assert.True(Path.IsPathFullyQualified(root)); Assert.False(Directory.Exists(root)); Directory.CreateDirectory(root);
        using var source = new ServiceCollection().AddZananceData(Path.Combine(root, "source.sqlite")).BuildServiceProvider();
        source.MigrateLocalDatabase<ZananceDbContext>(); var finances = source.GetRequiredService<ZananceStore>(); var store = source.GetRequiredService<PersonalSyncStore>();
        await finances.GetSettingsAsync(Ct);
        var account = new Account { Name = "Fictitious integrity account", CurrencyCode = "EUR", OpeningBalance = 100_000, OpeningDate = new(2026, 10, 1) };
        await finances.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234, Date = new(2026, 10, 16), Title = "Fictitious integrity entry" };
        Assert.True((await finances.SaveEntryAsync(entry, Ct)).Succeeded);
        var room = Guid.NewGuid(); var deviceId = Guid.NewGuid(); var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
        await store.ConnectAsync(await store.ReviewAsync(Ct), peer, "fictitious-owner", deviceId, new(room, "Fictitious integrity room", HouseholdFinancialSchema.Hash, 0, true), Ct,
            transportKind: ProfileSynchronizationKind.Household);
        var work = await store.CaptureRoundAsync(peer, "fictitious-owner", Ct, ProfileSynchronizationKind.Household);
        using var rootKey = SigningRecipientIdentity.Create(); using var author = SigningRecipientIdentity.Create(); using var epoch = HouseholdEpochMaterial.Create();
        var crypto = new HouseholdCryptography(TimeProvider.System);
        var genesis = Membership(room, rootKey, "fictitious-owner", epoch, 0, Zero, HouseholdMembershipChange.Genesis,
            [new("fictitious-owner", HouseholdRole.Owner, Zero, [new(deviceId, Public(author))])]);
        var rootProof = Publish(genesis, rootKey, epoch, crypto);
        var trusted = HouseholdTrustChain.Genesis(room, rootKey.Public, new(rootProof.SignedStatement, null, null, HouseholdPublicationDigest.Grants(rootProof.Grants)));
        using var replica = new HouseholdFinancialReplica(room);
        var publication = replica.Prepare(trusted, epoch, deviceId, author, work.Operation, Guid.NewGuid(), work.Parts.SelectMany(part => part.Rows).ToArray(),
            work.Resolutions, new Dictionary<string, byte[]>(), crypto);
        Assert.True(publication.Plan.Succeeded); var roundId = Guid.NewGuid();
        var valid = Receipt(publication.Parts, publication.Commit.SignedCommit);
        var context = new HouseholdProtectionContext(room, 1, work.Operation, HouseholdProtectionPurpose.Financial, "members");
        var altered = publication.Parts.ToArray(); var changedMoney = false;
        for (var index = 0; index < altered.Length; index++)
        {
            var clear = epoch.Decrypt(crypto, context, altered[index].Ciphertext, author.Public);
            try
            {
                var payload = StrictJsonPayload.Deserialize(clear, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload, HouseholdCryptography.MaximumPlaintextBytes);
                if (payload.Record?.Record.Table != "Entries") continue;
                var body = JsonNode.Parse(payload.Record.Values!.Value.GetRawText())!.AsObject(); body["Amount"] = 0;
                var poisoned = payload with { Record = payload.Record with { Values = JsonSerializer.SerializeToElement(body) } };
                var bytes = JsonSerializer.SerializeToUtf8Bytes(poisoned, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload);
                try
                {
                    var ciphertext = epoch.Encrypt(crypto, context, bytes, author);
                    altered[index] = altered[index] with { Ciphertext = ciphertext, Digest = HouseholdPublicationDigest.Ciphertext(ciphertext) };
                }
                finally { CryptographicOperations.ZeroMemory(bytes); }
                changedMoney = true; break;
            }
            finally { CryptographicOperations.ZeroMemory(clear); }
        }
        Assert.True(changedMoney);
        var invalidMoney = Receipt(altered, ReSign(altered));
        Assert.Throws<CryptographicException>(() => replica.Apply(trusted, epoch, invalidMoney, Events(altered), crypto));
        Assert.Equal(0, replica.Cursor); Assert.Equal(0, replica.FinancialCursor); Assert.Empty(replica.Snapshot());
        var duplicate = publication.Parts.ToArray();
        var firstClear = epoch.Decrypt(crypto, context, duplicate[0].Ciphertext, author.Public);
        try
        {
            var text = Encoding.UTF8.GetString(firstClear); Assert.StartsWith("{", text);
            var bytes = Encoding.UTF8.GetBytes("{\"format\":1," + text[1..]);
            try
            {
                var ciphertext = epoch.Encrypt(crypto, context, bytes, author);
                duplicate[0] = duplicate[0] with { Ciphertext = ciphertext, Digest = HouseholdPublicationDigest.Ciphertext(ciphertext) };
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        finally { CryptographicOperations.ZeroMemory(firstClear); }
        var duplicated = Receipt(duplicate, ReSign(duplicate));
        Assert.Throws<CryptographicException>(() => replica.Apply(trusted, epoch, duplicated, Events(duplicate), crypto));
        Assert.Equal(0, replica.Cursor); Assert.Empty(replica.Snapshot());
        Assert.Throws<CryptographicException>(() => replica.Apply(trusted, epoch, valid, Events(publication.Parts).Skip(1).ToArray(), crypto));
        Assert.Equal(0, replica.Cursor); Assert.Empty(replica.Snapshot());
        replica.Apply(trusted, epoch, valid, Events(publication.Parts), crypto);
        var acceptedCursor = replica.FinancialCursor; var acceptedCount = replica.Snapshot().Count;
        Assert.Equal(1, replica.Cursor); Assert.Equal(publication.FinancialCursor, acceptedCursor);
        Assert.Throws<CryptographicException>(() => replica.Apply(trusted, epoch, valid, Events(publication.Parts), crypto));
        Assert.Equal(acceptedCursor, replica.FinancialCursor); Assert.Equal(acceptedCount, replica.Snapshot().Count);
        Assert.Equal(1234, Assert.Single(await finances.GetEntriesAsync(cancellationToken: Ct)).Amount);
        await File.WriteAllTextAsync(Path.Combine(root, "completed-stages.txt"), "signed-invalid-money-duplicate-JSON-missing-part-and-replay-refused-without-state-or-original-data-change", Ct);

        HouseholdPublicationReceipt Receipt(IReadOnlyList<HouseholdOpaquePart> parts, string signed) => new(roundId, signed,
            parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray());
        HouseholdOpaqueEvent[] Events(IReadOnlyList<HouseholdOpaquePart> parts) => parts.Select(part => new HouseholdOpaqueEvent(1, roundId, part.RecordId, 1, 1,
            deviceId, work.Operation, publication.Open.AttemptId, part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray();
        string ReSign(IReadOnlyList<HouseholdOpaquePart> parts)
        {
            var statement = StandardJoseSignatures.VerifyJson(publication.Commit.SignedCommit, author.Public, "zanance-household-commit+jwt", HouseholdLimits.MembershipBytes,
                HouseholdMetadataJsonContext.Default.HouseholdCommitStatement) with
            { ContentHash = HouseholdPublicationDigest.Manifest(parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray()) };
            return Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement, author, "zanance-household-commit+jwt");
        }
    }
}
