using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

if (!OperatingSystem.IsWindows() || args.Length != 2 || args[1] is not ("prepare" or "resume" or "financial-prepare" or "financial-resume" or "financial-cold" or "projection-prepare" or "projection-prepare-resume" or "projection-resume" or "projection-cold" or "projection-replace" or "projection-write-boundary" or "projection-boundary-tamper" or "projection-maintenance-handoff" or "projection-intent-draft" or "projection-intent-draft-cold"))
    throw new ArgumentException("An independent owned root and prepare/resume mode are required.");
var root = Path.GetFullPath(args[0]);
var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-journal")) + Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new IOException("Only the independent household journal artifact root is allowed.");
for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
    if (ancestor.Exists && (ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected fixture roots are refused.");
const string marker = "Fictitious protected household journal v1";
var prepare = args[1] is "prepare" or "projection-prepare";
if (prepare)
{
    if (Directory.Exists(root)) throw new IOException("A fresh independently owned fixture is required; preserve existing keys/files.");
    Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), marker);
}
else if (await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker")) != marker) throw new IOException("Original fixture ownership is required.");
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3)); var ct = deadline.Token;
var stage = "original-protected-factory";
void Require(bool value) { if (!value) throw new InvalidDataException("The current fictional journal invariant failed."); }
void Proof(string value) { File.AppendAllText(Path.Combine(root, "checkpoints.txt"), value + Environment.NewLine); Console.WriteLine(value); }
try
{
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
    if (args[1].StartsWith("projection-", StringComparison.Ordinal))
    {
        stage = "original-selected-profile-projection";
        await ProjectionQualification.RunAsync(root, args[1], Proof, ct); return 0;
    }
    var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
    const string identity = "fictitious-protected-household-owner";
    using var client = await ProtectedClient.OpenAsync(Path.Combine(root, "client"), peer, prepare, ct);
    client.Services.MigrateLocalDatabase<ZananceDbContext>();
    var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
    var journal = new HouseholdJournal(factory);
    if (prepare)
    {
        stage = "plaintext-refusal";
        using var rootKey = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
        using (var plain = new ServiceCollection().AddZananceData(Path.Combine(root, "plaintext-refusal.sqlite")).BuildServiceProvider())
        {
            plain.MigrateLocalDatabase<ZananceDbContext>();
            var unprotected = new HouseholdJournal(plain.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var refused = false;
            try { await unprotected.BeginDraftAsync(peer, identity, rootKey.Public, true, device, rootKey, null, ct); }
            catch (InvalidOperationException) { refused = true; }
            Require(refused);
        }
        Proof("plain-factory-private-material-write-refused");
        stage = "original-key-draft";
        var draft = await journal.BeginDraftAsync(peer, identity, rootKey.Public, true, device, rootKey, null, ct);
        Require((await journal.BeginDraftAsync(peer, identity, rootKey.Public, true, device, rootKey, null, ct)).Authority == draft.Authority);
        var room = Guid.NewGuid(); var remoteDevice = Guid.NewGuid(); await journal.BindRemoteAsync(draft.Authority, room, remoteDevice, ct);
        using var epoch = HouseholdEpochMaterial.Create(); var operation = Guid.NewGuid();
        await journal.StageEpochAsync(draft.Authority, operation, epoch, ct);
        var statement = new HouseholdMembershipStatement(1, room, operation, HouseholdMembershipChange.Genesis, null, 0, 1, new('0', 64), identity, identity,
            new(rootKey.Public.SigningKey, rootKey.Public.WrappingKey), 1, epoch.KeyCommitment, epoch.AddressKeyCommitment,
            TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds(),
            [new(identity, HouseholdRole.Owner, new('0', 64), [new(remoteDevice, new(device.Public.SigningKey, device.Public.WrappingKey))])]);
        var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement), rootKey,
            "zanance-household-membership+jwt");
        var encrypted = epoch.Grant(new(TimeProvider.System), statement, rootKey, statement.Members[0].Devices[0]);
        var proposal = new HouseholdPublishMembership(remoteDevice, signed, null, null,
            [new(1, remoteDevice, encrypted, HouseholdPublicationDigest.Ciphertext(encrypted))]);
        await journal.StageMembershipProposalAsync(draft.Authority, proposal, ct);
        Require(ProposalHash(proposal) == ProposalHash(await journal.ReadMembershipProposalAsync(draft.Authority, ct) ?? throw new InvalidDataException()));
        var proof = new JournalProof(rootKey.Public.Fingerprint, device.Public.Fingerprint, epoch.KeyCommitment, epoch.AddressKeyCommitment, ProposalHash(proposal));
        await File.WriteAllTextAsync(Path.Combine(root, "public-proof.json"), JsonSerializer.Serialize(proof), ct);
        stage = "portable-private-authority-scrub";
        using var output = new MemoryStream();
        await client.Services.GetServices<IBackupSource>().Single(source => source.Name == "database.sqlite").WriteAsync(output, ct);
        output.Position = 0;
        await using (var portable = await SqliteMemorySnapshot.OpenAsync(output, 16 * 1024 * 1024, ct))
        {
            var tables = new List<string>(); await using (var names = portable.Connection.CreateCommand())
            {
                names.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name LIKE 'HouseholdLocal%' ORDER BY name";
                await using var reader = await names.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
            }
            Require(tables.ToHashSet(StringComparer.Ordinal).SetEquals(HouseholdLocalModel.PortableTables));
            foreach (var name in tables)
            {
                Require(name.All(character => char.IsAsciiLetterOrDigit(character)));
                await using var count = portable.Connection.CreateCommand(); count.CommandText = "SELECT COUNT(*) FROM \"" + name + "\"";
                Require(Convert.ToInt64(await count.ExecuteScalarAsync(ct)) == 0);
            }
        }
        foreach (var frame in new[] { rootKey.ExportForProtectedStorage(), device.ExportForProtectedStorage(), epoch.ExportForProtectedStorage() })
        { try { Require(output.GetBuffer().AsSpan(0, (int)output.Length).IndexOf(frame) < 0); } finally { CryptographicOperations.ZeroMemory(frame); } }
        Proof("portable-image-scrubs-all-household-private-keys-proposal-role-and-queue-authority");
        using var retained = await journal.ReadKeysAsync(draft.Authority, ct); using var originalEpoch = await journal.ReadStagedEpochAsync(draft.Authority, operation, ct);
        Require(retained.Device.Public.Fingerprint == proof.Device && retained.Owner?.Public.Fingerprint == proof.Root && originalEpoch.KeyCommitment == proof.Epoch);
        Proof("original-private-keys-and-exact-membership-ciphertext-durably-prepared-before-network");
        // Deliberate actual process termination after completed SQLite commits, without unwinding the
        // owning factory/generation/key-handle finally scopes. This is process-loss, not power-loss proof.
        Environment.Exit(23);
    }
    else if (args[1] == "resume")
    {
        stage = "cold-original-private-material";
        var proof = JsonSerializer.Deserialize<JournalProof>(await File.ReadAllTextAsync(Path.Combine(root, "public-proof.json"), ct)) ?? throw new InvalidDataException();
        var captured = await journal.CaptureAsync(peer, identity, ct) ?? throw new InvalidDataException();
        using var keys = await journal.ReadKeysAsync(captured.Authority, ct);
        Require(keys.Owner is not null && keys.Owner.Public.Fingerprint == proof.Root && keys.Device.Public.Fingerprint == proof.Device);
        var proposal = await journal.ReadMembershipProposalAsync(captured.Authority, ct) ?? throw new InvalidDataException();
        Require(ProposalHash(proposal) == proof.Proposal);
        var statement = StandardJoseSignatures.VerifyJson(proposal.SignedStatement, keys.Owner!.Public, "zanance-household-membership+jwt",
            HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement);
        using var staged = await journal.ReadStagedEpochAsync(captured.Authority, statement.OperationId, ct);
        Require(staged.KeyCommitment == proof.Epoch && staged.AddressKeyCommitment == proof.Addresses);
        Proof("cold-process-reopens-same-device-root-epoch-and-original-signed-encrypted-proposal");
        stage = "verified-key-promotion";
        var membership = await journal.RecordMembershipAsync(captured.Authority,
            [new(proposal.SignedStatement, null, null, HouseholdPublicationDigest.Grants(proposal.Grants))], ct);
        await journal.PromoteStagedEpochAsync(captured.Authority, statement.OperationId, ct);
        await journal.PromoteStagedEpochAsync(captured.Authority, statement.OperationId, ct);
        await journal.CompleteMembershipProposalAsync(captured.Authority, ct);
        Require(await journal.ReadMembershipProposalAsync(captured.Authority, ct) is null);
        using var epoch = await journal.ReadEpochAsync(captured.Authority, 1, ct);
        using var restoredGrant = membership.OpenGrant(new(TimeProvider.System), proposal.Grants[0].Ciphertext, captured.DeviceId, keys.Device);
        Require(epoch.KeyCommitment == proof.Epoch && restoredGrant.KeyCommitment == proof.Epoch);
        Require((await journal.CaptureAsync(peer, identity, ct))!.MembershipVersion == 1);
        var crossScopeRefused = false;
        try { using var refused = await journal.ReadKeysAsync(captured.Authority with { IdentityId = "fictitious-other-owner" }, ct); }
        catch (CryptographicException) { crossScopeRefused = true; }
        Require(crossScopeRefused);
        Proof("cold-verified-membership-key-promotion-exact-grant-and-cross-identity-refusal-pass");
    }
    else
    {
        stage = "captured-original-financial-round";
        var captured = await journal.CaptureAsync(peer, identity, ct) ?? throw new InvalidDataException();
        var financialProofPath = Path.Combine(root, "financial-proof.json");
        if (args[1] == "financial-prepare")
        {
            if (File.Exists(financialProofPath)) throw new IOException("Retain and resume the existing original financial fixture.");
            var account = (await client.Store.GetAccountsAsync(cancellationToken: ct)).SingleOrDefault(row => row.Name == "Fictitious retained household cash") ?? new Account { Name = "Fictitious retained household cash", CurrencyCode = "EUR", OpeningBalance = 100_000,
                OpeningDate = new(2026, 10, 1) };
            if (!(await client.Store.GetAccountsAsync(cancellationToken: ct)).Any(row => row.Id == account.Id)) await client.Store.SaveAccountAsync(account, ct);
            if (!(await client.Store.GetEntriesAsync(cancellationToken: ct)).Any(row => row.Title == "Fictitious retained ciphertext expense"))
                Require((await client.Store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234,
                    Date = new(2026, 10, 16), Title = "Fictitious retained ciphertext expense" }, ct)).Succeeded);
            stage = "original-household-connection";
            if (!(await client.Sync.StatusAsync(ct)).Connected)
                await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(ct), peer, identity, captured.DeviceId,
                    new(captured.SpaceId, "Fictitious protected household", HouseholdFinancialSchema.Hash, 0, true), ct, transportKind: ProfileSynchronizationKind.Household);
            stage = "original-household-ciphertext-preparation";
            var prepared = await journal.PrepareOriginalAsync(captured.Authority, peer, client.Sync, ct);
            Require(!prepared.NeedsReview && prepared.Attempt is not null && prepared.Attempt.Parts.Count >= 2);
            var attempt = prepared.Attempt!;
            var publicProof = new FinancialJournalProof(attempt.Open.OperationId, attempt.Open.AttemptId, attempt.Cutoff, attempt.CaptureHash,
                await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, captured.Authority, attempt, ct), attempt.FinancialCursor);
            await File.WriteAllTextAsync(financialProofPath, JsonSerializer.Serialize(publicProof), ct);
            // A subsequent original edit must remain outside this already frozen ciphertext/cutoff.
            Require((await client.Store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 567,
                Date = new(2026, 10, 16), Title = "Fictitious later unsent expense" }, ct)).Succeeded);
            var exact = (await journal.PrepareOriginalAsync(captured.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            Require(await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, captured.Authority, exact, ct) == publicProof.Ciphertext && exact.Cutoff == publicProof.Cutoff);
            var receipt = FixtureReceipt(exact); var events = await FixtureEventsAsync(journal, captured.Authority, exact, ct);
            var byAddress = events.OrderBy(item => item.RecordId, StringComparer.Ordinal).ToArray();
            await journal.StageInboxAsync(captured.Authority, receipt, [byAddress[^1]], ct);
            Require((await journal.InboxPositionAsync(captured.Authority, ct)).Cursor == 0);
            Require(!await journal.AcceptInboxAsync(captured.Authority, ct));
            await journal.StageInboxAsync(captured.Authority, receipt, [byAddress[0]], ct);
            Require((await journal.InboxPositionAsync(captured.Authority, ct)).RecordId == byAddress[0].RecordId);
            using var incomplete = await journal.ReadReplicaAsync(captured.Authority, ct);
            Require(incomplete.Cursor == 0 && incomplete.FinancialCursor == 0);
            Proof("original-frozen-financial-cutoff-exact-ciphertext-and-verified-partial-inbox-retained-without-application");
            Environment.Exit(23);
        }
        var proof = JsonSerializer.Deserialize<FinancialJournalProof>(await File.ReadAllTextAsync(financialProofPath, ct)) ?? throw new InvalidDataException();
        var retained = await journal.ReadAttemptAsync(captured.Authority, ct) ?? throw new InvalidDataException();
        Require(retained.Open.OperationId == proof.Operation && retained.Open.AttemptId == proof.Attempt && retained.Cutoff == proof.Cutoff &&
            retained.CaptureHash == proof.Capture && retained.FinancialCursor == proof.FinancialCursor && await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, captured.Authority, retained, ct) == proof.Ciphertext);
        if (args[1] == "financial-resume")
        {
            stage = "cold-partial-inbox-original-ciphertext";
            var exact = (await journal.PrepareOriginalAsync(captured.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            Require(await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, captured.Authority, exact, ct) == proof.Ciphertext);
            var receipt = FixtureReceipt(exact); var events = await FixtureEventsAsync(journal, captured.Authority, exact, ct);
            var damaged = events[0] with { Digest = new('0', 64) }; var refused = false;
            try { await journal.StageInboxAsync(captured.Authority, receipt, [damaged], ct); }
            catch (CryptographicException) { refused = true; }
            Require(refused);
            var clearRefused = false;
            try { await journal.CompleteAppliedAttemptAsync(captured.Authority, proof.Operation, proof.Attempt, ct); }
            catch (CryptographicException) { clearRefused = true; }
            Require(clearRefused);
            await journal.StageInboxAsync(captured.Authority, receipt, events, ct);
            await journal.StageInboxAsync(captured.Authority, receipt, events, ct);
            Require(await journal.AcceptInboxAsync(captured.Authority, ct));
            using var complete = await journal.ReadReplicaAsync(captured.Authority, ct);
            Require(complete.Cursor == 1 && complete.FinancialCursor == proof.FinancialCursor);
            Require(complete.Snapshot().Values.Count(row => row.Record.Table == "Entries" && row.Values is not null) == 1);
            Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Count == 2);
            Require(!(await journal.ReadAttemptAsync(captured.Authority, ct))!.Committed);
            Proof("cold-exact-ciphertext-resume-corruption-refusal-complete-inbox-verification-and-unacknowledged-cutoff-preservation-pass");
            Environment.Exit(23);
        }
        stage = "cold-verified-inbox-replay";
        using var replayed = await journal.ReadReplicaAsync(captured.Authority, ct);
        Require(replayed.Cursor == 1 && replayed.FinancialCursor == proof.FinancialCursor);
        Require(replayed.Snapshot().Values.Count(row => row.Record.Table == "Entries" && row.Values is not null) == 1);
        Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Count == 2);
        Require(!(await journal.ReadAttemptAsync(captured.Authority, ct))!.Committed);
        Proof("cold-complete-inbox-replays-original-graph-without-consuming-new-edits-or-inventing-network-acknowledgement");
    }
    return 0;
}
catch (Exception error)
{
    // No original message, inner exception, ciphertext, request, private frame or credential is logged.
    Console.Error.WriteLine("FAILED " + stage + " " + error.GetType().Name);
    if (error is InvalidDataException && error.Message.StartsWith("Household projection graph refusal: ", StringComparison.Ordinal)) Console.Error.WriteLine(error.Message);
    Console.Error.WriteLine("ORIGINAL-METHODS " + string.Join(" > ", new System.Diagnostics.StackTrace(error).GetFrames()
        .Where(frame => frame.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Vafadar", StringComparison.Ordinal) == true)
        .Select(frame => frame.GetMethod()!.DeclaringType!.Name + "." + frame.GetMethod()!.Name + ":" + frame.GetFileLineNumber()))); return 1;
}

static string ProposalHash(HouseholdPublishMembership proposal) => Convert.ToHexStringLower(SHA256.HashData(
    JsonSerializer.SerializeToUtf8Bytes(proposal, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership)));



// These synthetic receipts exercise retained client verification only. No hosted publication,
// network acknowledgement or original financial-apply acceptance is inferred from this fixture.
static HouseholdPublicationReceipt FixtureReceipt(HouseholdJournalAttempt attempt) => new(attempt.Open.AttemptId, attempt.Commit.SignedCommit,
    attempt.Parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray());
static async Task<HouseholdOpaqueEvent[]> FixtureEventsAsync(HouseholdJournal journal, HouseholdJournalAuthority authority, HouseholdJournalAttempt attempt, CancellationToken ct) =>
    (await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.SmallFixtureAsync(journal, authority, attempt, ct)).Select(part => new HouseholdOpaqueEvent(
    attempt.Open.ExpectedCursor + 1, attempt.Open.AttemptId, part.RecordId, attempt.Open.KeyEpoch, attempt.Open.MembershipVersion,
    attempt.Open.DeviceId, attempt.Open.OperationId, attempt.Open.AttemptId, part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray();

/// <summary>Public commitments only, never private key bytes, passwords, capabilities or bearer credentials.</summary>
internal sealed record JournalProof(string Root, string Device, string Epoch, string Addresses, string Proposal);

/// <summary>Public operation/cutoff/digest identity only; original financial values, ciphertext and keys remain in the captured protected database.</summary>
internal sealed record FinancialJournalProof(Guid Operation, Guid Attempt, long Cutoff, string Capture, string Ciphertext, long FinancialCursor);
