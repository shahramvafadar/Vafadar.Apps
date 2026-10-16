using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

if (!OperatingSystem.IsWindows() || args.Length != 2 || args[1] is not ("prepare" or "resume" or "finish" or "cold" or "race" or "race-cold" or "local-apply-cut" or "local-apply-resume" or "abandon-cut" or "abandon-resume" or "bulk" or "bulk-resume" or "bulk-cold" or "stream-refusal" or "wide-row" or "wide-row-cold" or "small-attachments" or "small-attachments-cold" or "row-bound-refusal" or "archive" or "archive-resume" or "archive-cold" or "ordered-replay" or "headers-cut" or "headers-resume" or "outbox-upgrade-cut" or "outbox-upgrade-resume" or "stream-seal-cut" or "stream-seal-resume" or "stream-seal-publish" or "no-op-race" or "roles" or "roles-resume" or "roles-revoke" or "roles-grace" or "workspace" or "workspace-resume" or "workspace-foreground" or "workspace-switch" or "workspace-rebind" or "costs" or "costs-resume" or "costs-app" or "costs-cold" or "recovery-enroll-cut" or "recovery-enroll" or "owner-recovery-cut" or "owner-recovery" or "recovery-cold" or "recovery-revoke" or "recovery-append") && args[1] is not ("history-native-bound" or "history-compact-export" or "history-export" or "history-prepare-resume" or "history-prepare" or "history" or "history-cold" or "costs-conflict" or "app-source-server" or "app-source-server-resume" or "device-admin-cut" or "device-admin-resume" or "device-admin-cold" or "pending-member-preservation" or "app-management-server" or "app-management-root-server" or "device-access-cut" or "device-access-resume" or "device-access-cold" or "app-device-access-server" or "first-device-receive-cut" or "first-device-receive-resume" or "ownership-resume" or "ownership-cold" or "app-ownership-server" or "app-lifecycle-server" or "ownership-catchup" or "ownership-catchup-auto"))
    throw new ArgumentException("An independently owned root and explicit qualification phase are required.");
var root = Path.GetFullPath(args[0]); var prepare = args[1] is "history-prepare" or "prepare" or "roles" or "workspace" or "costs" or "app-source-server";
var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-network")) + Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new IOException("An independent owned household artifact root is required.");
const string marker = "Fictitious protected household HTTP process qualification v1";
void Validate()
{
    if (File.ReadAllText(Path.Combine(root, "fixture.marker")) != marker) throw new IOException("The original fixture marker is required.");
    for (var part = new DirectoryInfo(root); part is not null; part = part.Parent)
        if ((part.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected fixture roots are refused.");
}
if (prepare)
{
    if (Directory.Exists(root)) throw new IOException("Preserve and resume existing files and keys.");
    for (var part = new DirectoryInfo(root); part is not null; part = part.Parent)
        if (part.Exists && (part.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected fixture roots are refused.");
    Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "fixture.marker"), marker);
}
Validate();
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(args[1] is "app-device-access-server" or "app-ownership-server" or "app-lifecycle-server" ? 12 : 6)); var ct = deadline.Token;
var stage = "owned-service-configuration";
void Require(bool value) { if (!value) throw new InvalidDataException("The current protected household invariant failed."); }
void Proof(string name) { File.AppendAllText(Path.Combine(root, "checkpoints.txt"), name + Environment.NewLine); Console.WriteLine(name); }
try
{
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
    if (args[1] == "history-native-bound")
    {
        stage = "native-private-row-omission-and-allocation-bound";
        await HistoryQualification.NativeBoundAsync(root, Proof, ct); return 0;
    }
    if (args[1] is "history-export" or "history-compact-export")
    {
        stage = "original-history-private-portable-boundary";
        await HistoryQualification.ExportAsync(root, Proof, ct, args[1] == "history-compact-export"); return 0;
    }
    if (args[1] == "history-prepare-resume")
    {
        stage = "same-current-contract-original-history-continuation";
        await HistoryQualification.ResumePreparationAsync(root, Proof, ct); return 0;
    }
    if (args[1] == "history-prepare")
    {
        stage = "current-contract-independent-history-fixture";
        await HistoryQualification.PrepareAsync(root, Proof, ct); return 0;
    }
    if (args[1] is "history" or "history-cold")
    {
        stage = "same-original-bounded-financial-history";
        await HistoryQualification.RunAsync(root, args[1] == "history-cold", Proof, ct); return 0;
    }
    var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB"));
    Require(connection.Host is "127.0.0.1" or "localhost" && connection.Database == "zanance_identity_household_native_d294");
    if (prepare || args[1] is "recovery-enroll-cut" or "recovery-enroll" or "owner-recovery-cut" or "owner-recovery" or "ownership-resume" or "app-lifecycle-server")
    {
        await using var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection.ConnectionString).Options);
        await schema.Database.MigrateAsync(ct); Require(!schema.Database.HasPendingModelChanges());
        await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime", ct);
    }
    connection.Username = "zanance_runtime";
    var serviceRoot = Path.Combine(root, "service");
    var port = prepare ? 0 : int.Parse(await File.ReadAllTextAsync(Path.Combine(root, "service-port.txt"), ct), System.Globalization.CultureInfo.InvariantCulture);
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = connection.ConnectionString,
        ["Service:Root"] = serviceRoot, ["Service:Household:LocalQualification"] = "true" });
    builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, port));
    await using var app = builder.Build(); var feedRequests = 0; var readable = 0; var manifestRequests = 0; var withheldMembershipAck = false;
    app.Use(async (http, next) =>
    {
        var path = http.Request.Path.Value ?? "";
        if (args[1] == "ownership-resume" && http.Request.Method == "POST")
        {
            // Each one-shot marker belongs only to this new handoff case. Cut before
            // request execution to exercise the already committed private local intent.
            foreach (var (suffix, exit) in new[] { ("/prepare", 38), ("/authorize", 39), ("/accept", 40) })
                if (path.Contains("/ownership/", StringComparison.Ordinal) && path.EndsWith(suffix, StringComparison.Ordinal))
                {
                    var cut = Path.Combine(root, "ownership-before-" + suffix[1..] + ".cut");
                    if (!File.Exists(cut))
                    { File.WriteAllText(cut, "Original ownership local intent committed v1"); Proof("original-ownership-local-intent-retained-before-" + suffix[1..]); Environment.Exit(exit); }
                }
        }
        if (path.StartsWith("/sync/profiles", StringComparison.Ordinal)) Interlocked.Increment(ref readable);
        if (http.Request.Method == "GET" && path.Contains("/receipts/", StringComparison.Ordinal) && path.EndsWith("/parts", StringComparison.Ordinal))
            Interlocked.Increment(ref manifestRequests);
        if (http.Request.Method == "GET" && path.EndsWith("/feed", StringComparison.Ordinal))
        {
            var count = Interlocked.Increment(ref feedRequests);
            if (args[1] == "headers-cut" && count == 2)
            {
                var position = new HouseholdFeedPosition(long.Parse(http.Request.Query["afterCursor"].ToString(), System.Globalization.CultureInfo.InvariantCulture),
                    http.Request.Query["afterRecord"].ToString());
                Require(position.Cursor == 9 && position.RecordId.Length == 64 && position.RecordId != new string('f', 64));
                await File.WriteAllTextAsync(Path.Combine(root, "header-continuation-prefix.json"),
                    JsonSerializer.Serialize(position, HouseholdMetadataJsonContext.Default.HouseholdFeedPosition), ct);
                Proof("actual-new-ciphertext-header-prefix-persisted-before-process-loss-on-next-feed-request");
                Environment.Exit(29);
            }
            if (args[1] == "headers-resume" && count == 1)
            {
                var expected = JsonSerializer.Deserialize(await File.ReadAllTextAsync(Path.Combine(root, "header-continuation-prefix.json"), ct),
                    HouseholdMetadataJsonContext.Default.HouseholdFeedPosition) ?? throw new InvalidDataException();
                Require(http.Request.Query["afterCursor"].ToString() == expected.Cursor.ToString(System.Globalization.CultureInfo.InvariantCulture) &&
                    http.Request.Query["afterRecord"].ToString() == expected.RecordId);
                Proof("first-real-feed-request-resumes-after-exact-original-protected-header-prefix");
            }
            if (args[1] == "finish" && count == 1)
            {
                Require(http.Request.Query["afterCursor"] == "1" && http.Request.Query["afterRecord"].ToString().Length == 64 &&
                    http.Request.Query["afterRecord"].ToString() != new string('f', 64));
                Proof("first-resumed-actual-feed-request-starts-after-retained-authenticated-prefix");
            }
            if (args[1] == "resume" && count == 2)
            {
                Proof("actual-first-ciphertext-page-persisted-before-process-loss-during-next-download");
                Environment.Exit(24);
            }
        }
        if (args[1] == "workspace" && !withheldMembershipAck && http.Request.Method == "POST" && path.EndsWith("/membership", StringComparison.Ordinal))
        {
            var actual = http.Response.Body; await using var suppressed = new MemoryStream(); http.Response.Body = suppressed;
            try { await next(http); } finally { http.Response.Body = actual; }
            Require(http.Response.StatusCode == 200); withheldMembershipAck = true; http.Response.StatusCode = 503;
            Proof("workspace-actual-genesis-committed-before-withheld-membership-response"); return;
        }
        if (args[1] == "prepare" && path.EndsWith("/commit", StringComparison.Ordinal))
        {
            var actual = http.Response.Body; await using var suppressed = new MemoryStream(); http.Response.Body = suppressed;
            try { await next(http); } finally { http.Response.Body = actual; }
            Require(http.Response.StatusCode == 200);
            Proof("actual-postgresql-ciphertext-commit-completed-before-lost-http-response-and-process-exit");
            Environment.Exit(23);
        }
        await next(http);
        if (args[1] == "ownership-resume" && http.Request.Method == "POST" && path.EndsWith("/membership", StringComparison.Ordinal) && http.Response.StatusCode == 200 &&
            File.Exists(Path.Combine(root, "ownership-before-accept.cut")) && !File.Exists(Path.Combine(root, "ownership-final-publication.cut")))
        {
            File.WriteAllText(Path.Combine(root, "ownership-final-publication.cut"), "Original ownership publication committed v1");
            Proof("original-ownership-membership-committed-before-lost-final-response"); Environment.Exit(41);
        }
        if (args[1] == "owner-recovery-cut" && http.Request.Method == "POST" && path.EndsWith("/recovery/commit", StringComparison.Ordinal) && http.Response.StatusCode == 200)
        {
            Proof("actual-new-owner-device-and-root-recovery-committed-before-lost-response-and-process-exit");
            Environment.Exit(32);
        }
        if (args[1] == "recovery-enroll-cut" && http.Request.Method == "POST" && path.EndsWith("/epochs", StringComparison.Ordinal) &&
            path.Contains("/recovery/preparations/", StringComparison.Ordinal) && http.Response.StatusCode == 200)
        {
            Proof("actual-recovery-archive-prefix-committed-before-lost-upload-response-and-process-exit");
            Environment.Exit(31);
        }
        if (args[1] == "device-access-cut" && http.Request.Method == "POST" && path.EndsWith("/devices", StringComparison.Ordinal) &&
            http.Response.StatusCode == 200 && File.Exists(Path.Combine(root, "own-device-enrollment-cut.marker")))
        {
            Proof("actual-own-device-enrollment-committed-before-lost-response-and-local-remote-binding");
            Environment.Exit(36);
        }
        if (args[1] == "device-admin-cut" && http.Request.Method == "POST" && path.EndsWith("/membership", StringComparison.Ordinal) &&
            http.Response.StatusCode == 200 && File.Exists(Path.Combine(root, "owner-device-revoke-submitted.marker")))
        {
            Proof("original-owner-device-revocation-and-kit-root-update-committed-before-lost-response-and-process-exit");
            Environment.Exit(35);
        }
        if (http.Response.StatusCode >= 400)
            Console.Error.WriteLine("HTTP-REFUSAL " + http.GetEndpoint()?.DisplayName + " " + http.Response.StatusCode);
    });
    app.UseZananceService(); app.MapZananceService(); await app.StartAsync(ct);
    try
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(serviceRoot, "local-tls.fingerprint"), ct));
        if (prepare) await File.WriteAllTextAsync(Path.Combine(root, "service-port.txt"), new Uri(address).Port.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
        if (args[1] is "app-source-server" or "app-source-server-resume" or "app-management-server" or "app-management-root-server" or "app-device-access-server" or "app-ownership-server" or "app-lifecycle-server")
        {
            stage = "original-main-app-source-copy-local-service";
            await ApplicationSourceServer.RunAsync(root, peer, app.Services, prepare, Proof, ct, args[1].StartsWith("app-management-", StringComparison.Ordinal) || args[1] is "app-device-access-server" or "app-ownership-server" or "app-lifecycle-server", args[1] == "app-management-root-server", args[1] == "app-device-access-server", args[1] == "app-ownership-server", args[1] == "app-lifecycle-server");
            Require(readable == 0); return 0;
        }
        if (args[1] == "ownership-catchup-auto")
        {
            stage = "original-ownership-automatic-financial-reception";
            await OwnershipAutomaticCatchupQualification.RunAsync(root, peer, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "ownership-catchup")
        {
            stage = "original-ownership-financial-catchup";
            await OwnershipCatchupQualification.RunAsync(root, peer, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "ownership-resume" or "ownership-cold")
        {
            stage = "original-two-sided-ownership-handoff";
            if (args[1] == "ownership-cold") await OwnershipQualification.ColdAsync(root, peer, Proof, ct);
            else await OwnershipQualification.RunAsync(root, peer, app.Services, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "costs-conflict")
        {
            stage = "original-recovered-owner-cost-review";
            await CostConflictQualification.RunAsync(root, peer, app.Services, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "device-admin-cut" or "device-admin-resume" or "device-admin-cold")
        {
            stage = "original-owner-device-administration";
            await DeviceAdministrationQualification.RunAsync(root, peer, args[1], Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "first-device-receive-cut" or "first-device-receive-resume")
        {
            stage = "original-first-device-reception";
            await FirstDeviceReceiveQualification.RunAsync(root, peer, args[1], Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "device-access-cut" or "device-access-resume" or "device-access-cold")
        {
            stage = "original-existing-member-device-adoption";
            await DeviceAccessQualification.RunAsync(root, peer, args[1], app.Services, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "pending-member-preservation")
        {
            stage = "original-pending-member-consent";
            await PendingMemberQualification.RunAsync(root, peer, app.Services, Proof, ct);
            Require(readable == 0); return 0;
        }
        var clientRoot = Path.Combine(root, "client");
        stage = "original-protected-native-generation-open";
        using var client = await ProtectedClient.OpenAsync(clientRoot, peer, prepare, ct);
        stage = "original-protected-additive-schema-upgrade";
        await using (var captured = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            if (await captured.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM sqlite_schema WHERE type='table' AND name='HouseholdLocalBlobReferences'").SingleAsync(ct) == 0)
                Proof("original-retained-protected-file-needs-additive-household-archive-migration");
        }
        client.Services.MigrateLocalDatabase<ZananceDbContext>();
        using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(clientRoot, Validate));
        var journal = new HouseholdJournal(client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var coordinator = new HouseholdSyncCoordinator(client.Sync, journal);
        HouseholdJournalState state;
        if (prepare)
        {
            stage = "fictional-identity-and-native-key-enrollment";
            var email = args[1] == "costs" ? "household-native-cost-v2-owner@example.invalid" : args[1] == "workspace" ? "household-native-workspace-owner@example.invalid" : args[1] == "roles" ? "household-native-roles-owner@example.invalid" : "household-native@example.invalid";
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
            using (var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System))
            {
                using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct);
                Require(registration.StatusCode == HttpStatusCode.Accepted);
                await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(row => row.Email == email, ct);
                var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
                Require(confirmation.StatusCode == HttpStatusCode.NoContent);
            }
            var identity = await transport.SignInAsync(email, password, ct); password = "";
            if (args[1] == "costs")
            {
                await CostQualification.RunAsync(root, app.Services, peer, client, transport, Proof, ct);
                Require(readable == 0); return 0;
            }
            if (args[1] == "workspace")
            {
                stage = "actual-original-app-household-workspace";
                await WorkspaceQualification.RunAsync(root, app.Services, peer, client, transport, Proof, ct);
                Require(readable == 0); return 0;
            }
            using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
            var draft = await journal.BeginDraftAsync(peer, identity.Id, owner.Public, true, device, owner, null, ct);
            var reservation = await transport.ReserveAsync(new(draft.Authority.SetupOperation, HouseholdFinancialSchema.Hash,
                new(owner.Public.SigningKey, owner.Public.WrappingKey), new(device.Public.SigningKey, device.Public.WrappingKey)), ct);
            await journal.BindRemoteAsync(draft.Authority, reservation.SpaceId, reservation.DeviceId, ct);
            using var epoch = HouseholdEpochMaterial.Create(); var operation = Guid.NewGuid(); await journal.StageEpochAsync(draft.Authority, operation, epoch, ct);
            var statement = new HouseholdMembershipStatement(1, reservation.SpaceId, operation, HouseholdMembershipChange.Genesis, null,
                0, 1, new('0', 64), identity.Id, identity.Id, new(owner.Public.SigningKey, owner.Public.WrappingKey), 1, epoch.KeyCommitment,
                epoch.AddressKeyCommitment, TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds(),
                [new(identity.Id, HouseholdRole.Owner, new('0', 64), [new(reservation.DeviceId, new(device.Public.SigningKey, device.Public.WrappingKey))])]);
            var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement),
                owner, "zanance-household-membership+jwt");
            var encrypted = epoch.Grant(new(TimeProvider.System), statement, owner, statement.Members[0].Devices[0]);
            var proposal = new HouseholdPublishMembership(reservation.DeviceId, signed, null, null,
                [new(1, reservation.DeviceId, encrypted, HouseholdPublicationDigest.Ciphertext(encrypted))]);
            await journal.StageMembershipProposalAsync(draft.Authority, proposal, ct);
            _ = await transport.PublishMembershipAsync(reservation.SpaceId, proposal, ct);
            await journal.RecordMembershipAsync(draft.Authority, await transport.MembershipAsync(reservation.SpaceId, reservation.DeviceId, 0, ct), ct);
            await journal.PromoteStagedEpochAsync(draft.Authority, operation, ct); await journal.CompleteMembershipProposalAsync(draft.Authority, ct);
            state = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new InvalidDataException();
            await journal.RecordAccessCheckpointAsync(state.Authority, await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct), ct);
            if (args[1] == "roles")
            {
                stage = "actual-native-accepted-member-local-role-lifecycle";
                await NativeRoleQualification.RunAsync(root, app.Services, peer, client, journal, coordinator, transport, state, owner, epoch, statement, Proof, ct);
                Require(readable == 0); return 0;
            }
            var account = new Account { Name = "Fictitious native encrypted household", CurrencyCode = "EUR", OpeningBalance = 100_000, OpeningDate = new(2026, 10, 1) };
            await client.Store.SaveAccountAsync(account, ct);
            var entry = new LedgerEntry { AccountId = account.Id, Amount = 1234, Kind = EntryKind.Expense, Date = new(2026, 10, 16), Title = "Fictitious native household expense" };
            Require((await client.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-native.pdf", ContentType = "application/pdf",
                Data = RandomNumberGenerator.GetBytes(2 * 1024 * 1024 + 147_123) };
            await client.Store.AddAttachmentAsync(attachment, ct);
            await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(ct), peer, identity.Id, reservation.DeviceId,
                new(reservation.SpaceId, "Fictitious native household", HouseholdFinancialSchema.Hash, 0, true), ct, transportKind: ProfileSynchronizationKind.Household);
            var prepared = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            Require(prepared.Parts.Count > 16);
            await File.WriteAllTextAsync(Path.Combine(root, "public-financial-proof.json"), JsonSerializer.Serialize(new FinancialProof(
                prepared.Open.OperationId, prepared.Open.AttemptId, prepared.Cutoff, prepared.CaptureHash, await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, state.Authority, prepared, ct),
                attachment.Id, Convert.ToHexStringLower(SHA256.HashData(attachment.Data)))), ct);
            CryptographicOperations.ZeroMemory(attachment.Data);
            Proof("actual-original-native-cutoff-private-keys-and-exact-ciphertext-retained-before-first-financial-network-mutation");
        }
        else
        {
            stage = "cold-original-native-identity-key-and-ciphertext";
            var identity = await transport.IdentityAsync(ct); state = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new InvalidDataException();
            if (args[1] is "resume" or "finish")
            {
                var proof = JsonSerializer.Deserialize<FinancialProof>(await File.ReadAllTextAsync(Path.Combine(root, "public-financial-proof.json"), ct)) ?? throw new InvalidDataException();
                var retained = await journal.ReadAttemptAsync(state.Authority, ct) ?? throw new InvalidDataException();
                Require(retained.Open.OperationId == proof.Operation && retained.Open.AttemptId == proof.Attempt && retained.Cutoff == proof.Cutoff &&
                    retained.CaptureHash == proof.Capture && await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, state.Authority, retained, ct) == proof.Ciphertext);
                using var complete = await journal.ReadReplicaAsync(state.Authority, ct); Require(complete.Cursor == 0);
                Proof("cold-same-protected-native-key-operation-cutoff-and-literal-ciphertext-pass");
            }
        }
        if (args[1] == "recovery-append")
        {
            await RecoveryQualification.AppendAsync(root, transport, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "recovery-cold" or "recovery-revoke")
        {
            await RecoveryQualification.ColdAsync(root, transport, args[1] == "recovery-revoke", Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "owner-recovery-cut" or "owner-recovery")
        {
            await RecoveryQualification.RecoverAsync(root, client, transport, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "recovery-enroll-cut" or "recovery-enroll")
        {
            await RecoveryQualification.EnrollAsync(root, client, transport, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "costs-app")
        {
            await CostApplicationQualification.RunAsync(root, client, peer, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "costs-cold")
        {
            await CostApplicationQualification.ColdAsync(client, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "costs-resume")
        {
            await CostQualification.RunAsync(root, app.Services, peer, client, transport, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "workspace-rebind")
        {
            stage = "actual-original-app-approved-device-session-reattachment";
            await WorkspaceQualification.RebindAsync(client, transport, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "workspace-switch")
        {
            stage = "actual-original-app-household-setup-profile-switch-refusal";
            await WorkspaceQualification.RefuseRetiredSetupAsync(root, peer, client, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "workspace-foreground")
        {
            stage = "actual-original-app-encrypted-foreground-routing";
            await ForegroundQualification.RunAsync(root, peer, client, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "workspace-resume")
        {
            stage = "same-original-app-household-workspace-continuation";
            await WorkspaceQualification.RunAsync(root, app.Services, peer, client, transport, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "roles-grace")
        {
            stage = "same-native-real-host-grace-read-only";
            await NativeRoleQualification.GraceAsync(app.Services, client, coordinator, transport, state, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "roles-resume" or "roles-revoke")
        {
            stage = "same-native-accepted-member-role-continuation";
            using var keys = await journal.ReadKeysAsync(state.Authority, ct);
            var history = await journal.ReadHistoryAsync(state.Authority, ct);
            var genesis = HouseholdTrustChain.Genesis(state.SpaceId, keys.Owner!.Public, history[0]).Snapshot();
            using var originalEpoch = await journal.ReadEpochAsync(state.Authority, 1, ct);
            await NativeRoleQualification.RunAsync(root, app.Services, peer, client, journal, coordinator, transport, state, keys.Owner, originalEpoch, genesis, Proof, ct, args[1] == "roles-revoke");
            Require(readable == 0); return 0;
        }
        if (args[1] == "no-op-race")
        {
            stage = "original-stale-unpublished-no-op-replanning";
            await OutboundContinuation.NoOpRaceAsync(client, journal, coordinator, transport, state, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "stream-seal-cut" or "stream-seal-resume" or "stream-seal-publish")
        {
            stage = "actual-original-bounded-outbound-seal-continuation";
            await OutboundContinuation.RunAsync(args[1], client, journal, coordinator, transport, state, root, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "outbox-upgrade-resume")
        {
            stage = "same-original-outbound-ciphertext-after-additive-upgrade";
            var proof = JsonSerializer.Deserialize<OutboundUpgradeProof>(await File.ReadAllTextAsync(Path.Combine(root, "outbox-upgrade-proof.json"), ct)) ?? throw new InvalidDataException();
            var attempt = await journal.ReadAttemptAsync(state.Authority, ct) ?? throw new InvalidDataException();
            Require(state.Cursor == 9 && attempt.Open.OperationId == proof.Operation && attempt.Open.AttemptId == proof.Attempt &&
                attempt.Cutoff == proof.Cutoff && attempt.CaptureHash == proof.Capture &&
                await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, state.Authority, attempt, ct) == proof.Ciphertext);
            Proof("additive-outbound-header-upgrade-preserves-exact-original-operation-cutoff-signature-and-literal-ciphertext");
            var outcome = await coordinator.SynchronizeAsync(transport, state.Authority, ct);
            Require(outcome.Applied && !outcome.NeedsReview && (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1600 &&
                await journal.ReadAttemptAsync(state.Authority, ct) is null);
            using var replica = await journal.ReadReplicaAsync(state.Authority, ct); Require(replica.Cursor == 10);
            Require(readable == 0);
            Proof("original-retained-operation-publishes-once-through-bounded-native-part-reader-and-original-financial-apply"); return 0;
        }
        if (args[1] == "outbox-upgrade-cut")
        {
            stage = "original-retained-outbox-before-cipher-storage-upgrade";
            Require(state.Cursor == 9 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
            var entry = (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single(); entry.Amount = 1600;
            Require((await client.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var prepared = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            await File.WriteAllTextAsync(Path.Combine(root, "outbox-upgrade-proof.json"), JsonSerializer.Serialize(new OutboundUpgradeProof(
                prepared.Open.OperationId, prepared.Open.AttemptId, prepared.Cutoff, prepared.CaptureHash, await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(journal, state.Authority, prepared, ct))), ct);
            Proof("original-literal-signed-ciphertext-operation-and-cutoff-retained-before-outbox-storage-upgrade-and-process-exit");
            Environment.Exit(31);
        }
        if (args[1] is "headers-cut" or "headers-resume")
        {
            stage = "actual-bounded-protected-ciphertext-header-continuation";
            await HeaderContinuation.RunAsync(client, journal, coordinator, transport, state, root, args[1] == "headers-resume", Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] == "ordered-replay")
        {
            stage = "bounded-original-ordered-ciphertext-replay";
            await ArchiveQualification.RunAsync(client, journal, coordinator, transport, state, root, true, Proof, ct);
            await OrderedReplayRefusals.RunAsync(journal, state, Proof, ct);
            Require(readable == 0); return 0;
        }
        if (args[1] is "archive" or "archive-resume" or "archive-cold")
        {
            stage = "original-authenticated-historical-attachment-archive";
            await ArchiveQualification.RunAsync(client, journal, coordinator, transport, state, root, args[1] == "archive-cold", Proof, ct, args[1] == "archive-resume");
            Require(readable == 0); return 0;
        }
        if (args[1] == "row-bound-refusal")
        {
            stage = "reviewed-original-framed-row-allocation-bound";
            await PackedRefusals.RunAsync(journal, state, Proof, ct, allocationOnly: true); return 0;
        }
        if (args[1] is "small-attachments" or "small-attachments-cold")
        {
            stage = "original-bounded-small-attachment-packing";
            var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            if (args[1] == "small-attachments")
            {
                Require(state.Cursor == 6 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
                await using (var db = await factory.CreateDbContextAsync(ct))
                {
                    var expense = await db.Entries.SingleAsync(ct);
                    db.Set<EntryAttachment>().AddRange(Enumerable.Range(1000, 600).Select(index => new EntryAttachment { EntryId = expense.Id,
                        FileName = "fictitious-packed-tiny-" + index + ".bin", ContentType = "application/octet-stream", Data = BitConverter.GetBytes((ushort)index) }));
                    await db.SaveChangesAsync(ct);
                }
                var attempt = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
                Require(attempt.Parts.Count < 16);
                await PackedRefusals.RejectChangedSmallAttachmentAsync(journal, state, attempt, Proof, ct);
                Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            }
            using var verified = await journal.ReadReplicaAsync(state.Authority, ct);
            Require(verified.Cursor == 7);
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                Require(await db.Set<EntryAttachment>().CountAsync(ct) == 901);
                var rows = await db.Set<EntryAttachment>().Where(row => row.FileName.StartsWith("fictitious-packed-tiny-")).ToArrayAsync(ct);
                Require(rows.Length == 600);
                foreach (var row in rows)
                {
                    var index = int.Parse(row.FileName["fictitious-packed-tiny-".Length..^4], System.Globalization.CultureInfo.InvariantCulture);
                    Require(row.Data.AsSpan().SequenceEqual(BitConverter.GetBytes((ushort)index)));
                }
            }
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null && readable == 0);
            Proof(args[1] == "small-attachments" ? "600-original-small-attachments-use-fewer-than-16-ciphertext-parts-and-preserve-every-original-byte"
                : "cold-all-600-packed-original-attachment-bytes-and-complete-financial-graph-preserved");
            return 0;
        }
        if (args[1] is "wide-row" or "wide-row-cold")
        {
            stage = "original-complete-financial-row-above-personal-http-part-size";
            var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            if (args[1] == "wide-row")
            {
                Require(state.Cursor == 5 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
                await using var db = await factory.CreateDbContextAsync(ct);
                var snapshot = await db.Set<ForecastSnapshot>().SingleAsync(ct);
                var accounts = await db.Accounts.AsNoTracking().OrderBy(row => row.Id).Select(row => row.Id).ToArrayAsync(ct);
                Require(accounts.Length == 12001);
                snapshot.AccountIds = string.Join(',', accounts.Select(id => id.ToString("D")));
                snapshot.Path = string.Join(',', Enumerable.Repeat(long.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), 47619));
                snapshot.Horizon = snapshot.BaseDate.AddDays(47618); snapshot.Minimum = long.MinValue;
                await db.SaveChangesAsync(ct);
                var prepared = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
                Require(prepared.Parts.Count > 8);
                Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            }
            using var verified = await journal.ReadReplicaAsync(state.Authority, ct);
            var row = verified.Snapshot().Values.Single(item => item.Record.Table == "ForecastSnapshots" && item.Values is not null);
            Require(verified.Cursor == 6 && JsonSerializer.SerializeToUtf8Bytes(row, PersonalSyncJsonContext.Default.SyncRecord).Length > 1024 * 1024);
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                var snapshot = await db.Set<ForecastSnapshot>().SingleAsync(ct);
                Require(snapshot.Path.Length == 999998 && snapshot.Scope().Count == 12001 && snapshot.Points().Count == 47619 && snapshot.Minimum == long.MinValue);
                Require(row.Values!.Value.GetProperty("Path").GetString() == snapshot.Path && row.Values.Value.GetProperty("AccountIds").GetString() == snapshot.AccountIds);
            }
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null && readable == 0);
            Proof(args[1] == "wide-row" ? "original-valid-large-forecast-row-crosses-cipher-envelopes-with-complete-original-capture-apply-and-no-readable-http"
                : "cold-original-large-forecast-row-all-account-identities-and-47619-typed-points-preserved");
            return 0;
        }
        if (args[1] == "stream-refusal")
        {
            stage = "actual-bounded-paged-complete-ciphertext-manifest";
            var current = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
            Require(current.Cursor == 5);
            var firstPage = await transport.FeedAsync(state.SpaceId, state.DeviceId, new(4, new('f', 64)), 5, ct);
            Require(firstPage.Events.Length > 0);
            var manifest = await transport.ReceiptAsync(state.SpaceId, state.DeviceId, firstPage.Events[0].RoundId, ct);
            using (var original = await journal.ReadReplicaAsync(state.Authority, ct))
                Require(manifest.Parts.Length > 256 && manifestRequests >= 2 && HouseholdTrustChain.Digest(manifest.SignedCommit) == original.PublicationHead);
            Proof("actual-complete-author-manifest-crosses-two-bounded-HTTP-pages-and-matches-original-verified-publication-head");
            stage = "original-packed-native-financial-refusals";
            await PackedRefusals.RunAsync(journal, state, Proof, ct); return 0;
        }
        if (args[1] is "bulk" or "bulk-resume" or "bulk-cold")
        {
            stage = "original-large-financial-capture-and-paged-ciphertext-manifest";
            var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            if (args[1] == "bulk")
            {
                Require(state.Cursor == 4 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
                await using (var db = await factory.CreateDbContextAsync(ct))
                {
                    Require(!await db.Accounts.AnyAsync(row => row.Name.StartsWith("Fictitious household bulk "), ct));
                    db.Accounts.AddRange(Enumerable.Range(0, 12000).Select(index => new Account { Name = "Fictitious household bulk " + index,
                        CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 0 }));
                    var expense = await db.Entries.SingleAsync(ct);
                    db.Set<EntryAttachment>().AddRange(Enumerable.Range(0, 300).Select(index => new EntryAttachment { EntryId = expense.Id,
                        FileName = "fictitious-tiny-" + index + ".bin", ContentType = "application/octet-stream", Data = BitConverter.GetBytes((ushort)index) }));
                    var start = new DateOnly(2026, 10, 16);
                    db.Set<ForecastSnapshot>().Add(new ForecastSnapshot { Name = "Fictitious oversized complete row", BaseDate = start, Horizon = start.AddDays(39999),
                        CurrencyCode = "EUR", AccountIds = expense.AccountId.ToString("D"), Path = string.Join(',', Enumerable.Repeat("1000000000000000", 40000)),
                        Minimum = 1000000000000000, MinimumDate = start, UnknownCount = 0 });
                    await db.SaveChangesAsync(ct);
                }
                var attempt = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
                Require(attempt.Parts.Count > 256 && attempt.Parts.Count < 4096);
                Proof("original-12000-new-rows-oversized-row-and-300-tiny-attachments-fit-bounded-packed-ciphertext-before-upload");
            }
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied && readable == 0);
            stage = "original-bulk-final-financial-verification";
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                Require(await db.Accounts.CountAsync(ct) == 12001 && await db.Set<EntryAttachment>().CountAsync(ct) == 301);
                Require((await db.Set<ForecastSnapshot>().SingleAsync(ct)).Path.Length == 679999);
            }
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
            using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
            Require(replica.Cursor == 5 && replica.Snapshot().Values.Count(row => row.Record.Table == "Accounts" && row.Values is not null) == 12001);
            if (args[1] == "bulk") Require(manifestRequests >= 2);
            Proof(args[1] == "bulk" ? "actual-packed-native-financial-stream-fragmented-large-row-paged-complete-manifest-and-original-apply-pass"
                : "cold-packed-native-graph-large-row-and-attachments-replay-without-another-financial-publication");
            return 0;
        }
        if (args[1] == "abandon-cut")
        {
            stage = "actual-abandoned-attempt-before-local-acknowledgement";
            Require(state.Cursor == 3 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
            var entry = (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single(); entry.Amount = 1500;
            Require((await client.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var attempt = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            var abandoned = await transport.AbandonAttemptAsync(state.SpaceId, attempt.Open, ct);
            Require(abandoned.Abandoned && abandoned.CommittedCursor is null);
            Proof("actual-server-abandonment-tombstone-committed-before-local-acknowledgement-and-process-exit");
            Environment.Exit(26);
        }
        if (args[1] == "abandon-resume")
        {
            stage = "cold-unknown-abandonment-continues-original-cutoff";
            var retained = await journal.ReadAttemptAsync(state.Authority, ct) ?? throw new InvalidDataException();
            Require(!retained.Abandoned && !retained.Committed && retained.RemoteId is null);
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1500 && readable == 0);
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Require(await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM \"HouseholdRounds\" WHERE \"CommittedCursor\" IS NOT NULL").SingleAsync(ct) == 4);
            Proof("cold-actual-server-abandonment-recovered-and-same-logical-cutoff-published-once-with-new-attempt");
            return 0;
        }
        if (args[1] == "local-apply-cut")
        {
            stage = "original-applied-transaction-before-household-journal-retirement";
            var entry = (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single(); entry.Amount = 1400;
            Require((await client.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var attempt = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            var work = await client.Sync.CaptureRoundAsync(peer, state.Authority.IdentityId, ct, ProfileSynchronizationKind.Household);
            var handle = await transport.OpenAsync(state.SpaceId, attempt.Open, ct);
            await journal.RecordAttemptRemoteAsync(state.Authority, work.Operation, attempt.Open.AttemptId, handle.Id, ct);
            for (var offset = 0; offset < attempt.Parts.Count; offset += 8)
                foreach (var part in await journal.ReadAttemptPartsAsync(state.Authority, attempt.Open.OperationId, attempt.Open.AttemptId, offset, Math.Min(8, attempt.Parts.Count - offset), ct))
                    await transport.PutAsync(state.SpaceId, handle.Id, part, ct);
            handle = await transport.CommitAsync(state.SpaceId, handle.Id, attempt.Commit, ct);
            Require(handle.CommittedCursor == 3);
            await journal.RecordAttemptAcknowledgedAsync(state.Authority, work.Operation, attempt.Open.AttemptId, handle, ct);
            var receipt = await transport.ReceiptAsync(state.SpaceId, state.DeviceId, handle.Id, ct);
            var page = await transport.FeedAsync(state.SpaceId, state.DeviceId, new(2, new('f', 64)), 3, ct);
            Require(page.Complete && page.Events.Length == 1);
            await journal.StageInboxAsync(state.Authority, receipt, page.Events, ct); Require(await journal.AcceptInboxAsync(state.Authority, ct));
            using var verified = await journal.ReadReplicaAsync(state.Authority, ct);
            await client.Sync.RecordRemoteAsync(work, handle.Id, work.Parts.Length, ct);
            await client.Sync.RecordCommitAsync(work, new(true, verified.FinancialCursor, []), ct);
            var cursor = work.Cursor;
            while (cursor < verified.FinancialCursor)
            { var financial = await verified.FinancialFeedAsync(cursor, verified.FinancialCursor, ct); await client.Sync.StageFeedAsync(work, financial, ct); cursor = financial.NextCursor; }
            Require(await client.Sync.ApplyFeedAsync(work, ct));
            Require((await journal.ReadAttemptAsync(state.Authority, ct))!.Committed);
            Proof("original-actual-financial-transaction-committed-with-household-retirement-still-pending-before-process-exit");
            Environment.Exit(25);
        }
        if (args[1] == "local-apply-resume")
        {
            stage = "cold-applied-cutoff-retirement-before-any-new-capture";
            Require((await journal.ReadAttemptAsync(state.Authority, ct))!.Committed);
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1400 && readable == 0);
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Require(await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM \"HouseholdRounds\" WHERE \"CommittedCursor\" IS NOT NULL").SingleAsync(ct) == 3);
            Proof("cold-original-applied-cutoff-retires-before-new-capture-without-an-extra-financial-server-commit");
            return 0;
        }
        if (args[1] == "race")
        {
            stage = "actual-conflicting-commit-before-prepared-attempt-open";
            var originalEntry = (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single(); originalEntry.Amount = 1500;
            Require((await client.Store.SaveEntryAsync(originalEntry, ct)).Succeeded);
            var oldAttempt = (await journal.PrepareOriginalAsync(state.Authority, peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
            var oldRow = replica.Snapshot().Values.Single(row => row.Record.Table == "Entries" && row.Values is not null);
            var remoteValue = JsonNode.Parse(oldRow.Values!.Value.GetRawText())!.AsObject(); remoteValue["Amount"] = 1300;
            var proposal = new SyncProposal(oldRow.Record, oldRow.Version, JsonSerializer.SerializeToElement(remoteValue));
            using var keys = await journal.ReadKeysAsync(state.Authority, ct);
            var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct); using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
            var concurrent = await replica.PrepareAsync(membership, epoch, state.DeviceId, keys.Device, Guid.NewGuid(), Guid.NewGuid(), [proposal], [],
                new Dictionary<string, byte[]>(), new(TimeProvider.System));
            Require(concurrent.Plan.Succeeded && concurrent.Parts.Count == 1);
            var other = await transport.OpenAsync(state.SpaceId, concurrent.Open, ct);
            foreach (var part in concurrent.Parts) await transport.PutAsync(state.SpaceId, other.Id, part, ct);
            Require((await transport.CommitAsync(state.SpaceId, other.Id, concurrent.Commit, ct)).CommittedCursor == 2);
            var conflict = await coordinator.SynchronizeAsync(transport, state.Authority, ct);
            Require(!conflict.Applied && conflict.NeedsReview);
            Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1500);
            var review = (await client.Sync.ConflictsAsync(ct)).Single(); Require(review.Conflict.Fields.Contains("Amount"));
            var abandoned = await transport.ObserveAttemptAsync(state.SpaceId, state.DeviceId, oldAttempt.Open.OperationId, oldAttempt.Open.AttemptId, ct);
            Require(abandoned.Abandoned && abandoned.CommittedCursor is null && await journal.ReadAttemptAsync(state.Authority, ct) is null);
            var lateRefused = false;
            try { await transport.OpenAsync(state.SpaceId, oldAttempt.Open, ct); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Gone) { lateRefused = true; }
            Require(lateRefused);
            await client.Sync.ResolveAsync(review, false, ct);
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1300 && readable == 0);
            Proof("actual-CAS-race-tombstones-only-original-unpublished-attempt-explicit-money-review-and-remote-choice-without-second-payment-pass");
            return 0;
        }
        if (args[1] == "race-cold")
        {
            stage = "cold-original-resolved-financial-review";
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1300 && readable == 0);
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
            Proof("cold-exact-resolved-amount-and-empty-no-op-do-not-create-another-server-publication");
            return 0;
        }
        stage = "actual-protected-coordinator";
        var result = await coordinator.SynchronizeAsync(transport, state.Authority, ct);
        Require(result.Applied && !result.NeedsReview && readable == 0);
        if (args[1] == "prepare" || args[1] == "resume") throw new InvalidDataException("The deliberate process-loss checkpoint was not exercised.");
        var final = JsonSerializer.Deserialize<FinancialProof>(await File.ReadAllTextAsync(Path.Combine(root, "public-financial-proof.json"), ct)) ?? throw new InvalidDataException();
        var originalAttachment = await client.Store.GetAttachmentAsync(final.Attachment, ct) ?? throw new InvalidDataException();
        Require(Convert.ToHexStringLower(SHA256.HashData(originalAttachment.Data)) == final.AttachmentHash);
        CryptographicOperations.ZeroMemory(originalAttachment.Data);
        Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1234);
        Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
        using (var verified = await journal.ReadReplicaAsync(state.Authority, ct)) Require(verified.Cursor == 1);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Require(await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM \"HouseholdRounds\" WHERE \"CommittedCursor\" IS NOT NULL").SingleAsync(ct) == 1);
        }
        Proof("actual-native-household-coordinator-original-financial-apply-attachment-hash-single-server-commit-and-zero-readable-financial-http-pass");
    }
    finally { await app.StopAsync(CancellationToken.None); }
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine("NATIVE-STAGE-STACK " + string.Join(" > ", new System.Diagnostics.StackTrace(error, true).GetFrames()
        .Where(frame => frame.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Vafadar", StringComparison.Ordinal) == true)
        .Select(frame => frame.GetMethod()!.DeclaringType!.Name + ":" + frame.GetFileLineNumber())));
    if (error.InnerException is Microsoft.Data.Sqlite.SqliteException nativeWrite)
        Console.Error.WriteLine("NATIVE-WRITE-CODE " + nativeWrite.SqliteErrorCode + "/" + nativeWrite.SqliteExtendedErrorCode);
    if (error is Microsoft.Data.Sqlite.SqliteException sqlite)
        Console.Error.WriteLine("NATIVE-SQLITE-CODE " + sqlite.SqliteErrorCode + "/" + sqlite.SqliteExtendedErrorCode + " " +
            string.Join(" > ", new System.Diagnostics.StackTrace(error).GetFrames().Select(frame => frame.GetMethod()).Where(method => method?.DeclaringType?.Namespace?.StartsWith("Vafadar", StringComparison.Ordinal) == true)
                .Select(method => method!.DeclaringType!.Name + "." + method.Name)));
    Console.Error.WriteLine("FAILED " + stage + " " + error.GetType().Name + (error is PersonalSyncHttpException refusal ? " HTTP " + (int)refusal.Status : "")); return 1;
}



/// <summary>Public synthetic operation/digest identities only; no ciphertext, key or readable financial row is stored here.</summary>
internal sealed record FinancialProof(Guid Operation, Guid Attempt, long Cutoff, string Capture, string Ciphertext, Guid Attachment, string AttachmentHash);
/// <summary>Original synthetic outbound identity/digest only; retained ciphertext and keys stay inside actual protected storage.</summary>
internal sealed record OutboundUpgradeProof(Guid Operation, Guid Attempt, long Cutoff, string Capture, string Ciphertext);
/// <summary>Independent fixture mail is unprotected in memory only to complete its own fictitious identity enrollment.</summary>
internal sealed record PrivateMail(string Token);
