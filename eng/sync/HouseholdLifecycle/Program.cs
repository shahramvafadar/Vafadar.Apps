using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;
using static FinancialComparison;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

if (!OperatingSystem.IsWindows() || args.Length != 2 || args[1] is not ("prepare" or "delete-cut" or "resume" or "restore-check" or "audit-and-expired" or "retained-delete-observe" or "retained-delete-complete")) throw new ArgumentException("Explicit owned lifecycle root and phase required.");
var root = Path.GetFullPath(args[0]); var mode = args[1];
var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-lifecycle")) + Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new IOException("Independent lifecycle artifact root required.");
const string marker = "Fictional original protected household lifecycle process qualification v1";
if (mode == "prepare")
{
    if (Directory.Exists(root)) throw new IOException("Preserve original files and keys.");
    Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "fixture.marker"), marker);
}
void Validate()
{
    if (File.ReadAllText(Path.Combine(root, "fixture.marker")) != marker) throw new IOException("Original lifecycle ownership required.");
    for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
        if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected lifecycle roots refused.");
}
Validate();
void Require(bool condition) { if (!condition) throw new InvalidDataException("Original household lifecycle invariant failed."); }
void Proof(string text) { File.AppendAllText(Path.Combine(root, "checkpoints.txt"), text + Environment.NewLine); Console.WriteLine(text); }
var done = Path.Combine(root, mode + ".complete"); if (File.Exists(done)) throw new IOException("Never replay a completed qualification phase.");
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5)); var ct = deadline.Token;
var phase = "owned-service-composition";
try
{
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
    var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB"));
    Require(settings.Host is "127.0.0.1" or "localhost" && settings.Database == "zanance_identity_household_lifecycle_process_d294");
    if (mode == "prepare")
    {
        await using var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(settings.ConnectionString).Options);
        await schema.Database.MigrateAsync(ct); Require(!schema.Database.HasPendingModelChanges());
        await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime", ct);
    }
    settings.Username = "zanance_runtime";
    var serviceRoot = Path.Combine(root, "service");
    var port = mode == "prepare" ? 0 : int.Parse(await File.ReadAllTextAsync(Path.Combine(root, "service-port.txt"), ct), System.Globalization.CultureInfo.InvariantCulture);
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = settings.ConnectionString,
        ["Service:Root"] = serviceRoot, ["Service:Household:LocalQualification"] = "true" });
    builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, port));
    foreach (var descriptor in builder.Services.Where(row => row.ServiceType == typeof(IHostedService) && row.ImplementationType == typeof(PrivateMaintenance)).ToArray()) builder.Services.Remove(descriptor);
    if (mode == "delete-cut") builder.Services.Replace(ServiceDescriptor.Singleton<IHouseholdDeletionLedger>(services =>
        new CutAfterOriginalMarker(new HouseholdDeletionLedger(services.GetRequiredService<ServiceStorage>()), root)));
    await using var app = builder.Build(); var financialRequests = 0;
    app.Use(async (http, next) =>
    {
        var path = http.Request.Path.Value ?? "";
        if (mode == "retained-delete-complete" && http.Request.Method == "GET" && path.StartsWith("/households/", StringComparison.Ordinal) &&
            (path.EndsWith("/checkpoint", StringComparison.Ordinal) && !path.Contains("/deletion/", StringComparison.Ordinal) || path.Contains("/grants/", StringComparison.Ordinal)))
            Interlocked.Increment(ref financialRequests);
        await next(http);
    });
    app.UseZananceService(); app.MapZananceService(); await app.StartAsync(ct);
    try
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(serviceRoot, "local-tls.fingerprint"), ct));
        if (mode == "prepare") await File.WriteAllTextAsync(Path.Combine(root, "service-port.txt"), new Uri(address).Port.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
        var nativeRoot = Path.Combine(root, "client"); var controlRoot = Path.Combine(root, "control");
        using var client = await ProtectedClient.OpenAsync(nativeRoot, peer, mode == "prepare", ct);
        using var control = await ProtectedClient.OpenAsync(controlRoot, peer, mode == "prepare", ct);
        using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(nativeRoot, Validate));
        using var controlTransport = new HouseholdServiceTransport(peer, new ProtectedSessions(controlRoot, Validate));
        var workspace = client.Services.GetRequiredService<HouseholdWorkspace>(); var journal = client.Services.GetRequiredService<HouseholdJournal>();
        var coordinator = client.Services.GetRequiredService<HouseholdSyncCoordinator>();
        var controlWorkspace = control.Services.GetRequiredService<HouseholdWorkspace>(); var controlJournal = control.Services.GetRequiredService<HouseholdJournal>();
        if (mode == "prepare")
        {
            phase = "original-native-financial-and-identity-creation";
            async Task EnrollAsync(HouseholdServiceTransport channel, string email)
            {
                var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
                using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
                using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct); Require(registration.StatusCode == HttpStatusCode.Accepted);
                await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(row => row.Email == email, ct);
                var challenge = await db.Challenges.SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct); Require(confirmation.StatusCode == HttpStatusCode.NoContent);
                _ = await channel.SignInAsync(email, password, ct); password = "";
            }
            await EnrollAsync(transport, "household-lifecycle-native-owner@example.invalid");
            await EnrollAsync(controlTransport, "household-lifecycle-native-control@example.invalid");
            var draft = await workspace.BeginOwnerAsync(transport, ct); var state = await workspace.CompleteOwnerAsync(transport, draft.Authority, "Fictitious lifecycle owner", ct);
            var other = await controlWorkspace.BeginOwnerAsync(controlTransport, ct); other = await controlWorkspace.CompleteOwnerAsync(controlTransport, other.Authority, "Fictitious independent preserved room", ct);
            var account = new Account { Name = "Fictitious lifecycle EUR", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = new(2026, 10, 1) };
            await client.Store.SaveAccountAsync(account, ct);
            Require((await client.Store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 450, Date = new(2026, 10, 16), Title = "Fictitious preserved local expense" }, ct)).Succeeded);
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
            await File.WriteAllTextAsync(Path.Combine(root, "financial-digest.txt"), await FinancialDigestAsync(client), ct);
            await File.WriteAllTextAsync(Path.Combine(root, "control-financial-digest.txt"), await FinancialDigestAsync(control), ct);
            Proof("actual-protected-original-workspace-financial-publication-and-independent-control-prepared-before-snapshot");
        }
        else
        {
            phase = "same-native-files-keys-sessions-and-financial-baseline";
            var identity = await transport.IdentityAsync(ct); var state = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new InvalidDataException();
            var otherIdentity = await controlTransport.IdentityAsync(ct); var other = await controlJournal.CaptureAsync(peer, otherIdentity.Id, ct) ?? throw new InvalidDataException();
            Require(await FinancialDigestAsync(client) == await File.ReadAllTextAsync(Path.Combine(root, "financial-digest.txt"), ct));
            Require(await FinancialDigestAsync(control) == await File.ReadAllTextAsync(Path.Combine(root, "control-financial-digest.txt"), ct));
            if (mode == "retained-delete-observe")
            {
                phase = "actual-owner-deletion-during-retained-service-window";
                var retainedFactory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
                await using (var db = await retainedFactory.CreateDbContextAsync(ct))
                {
                    var expiry = DateTimeOffset.UtcNow.AddDays(-31);
                    await db.Set<HouseholdSpaceRow>().Where(row => row.Id == other.SpaceId).ExecuteUpdateAsync(update => update.SetProperty(row => row.HostExpiresAt, expiry), ct);
                }
                var retainedStatus = await controlWorkspace.ReadLifecycleAsync(controlTransport, other.Authority, ct);
                Require(retainedStatus.Retention == HouseholdRetentionState.Retained && !(await controlJournal.ReadAccessAsync(other.Authority, ct)).CanWrite);
                var denied = false;
                try { _ = await controlWorkspace.BeginDeletionAsync(controlTransport, other.Authority, other.MembershipVersion, other.Cursor, ct); }
                catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Forbidden) { denied = true; }
                Require(denied && await controlJournal.ReadDeletionAsync(other.Authority, ct) is null);
                await File.WriteAllTextAsync(Path.Combine(root, "retained-owner-original-financial-digest.txt"), await FinancialDigestAsync(control), ct);
                Proof("counterexample-actual-retained-owner-is-allowed-service-deletion-but-original-workspace-requires-forbidden-financial-refresh-before-retaining-consent");
                await File.WriteAllTextAsync(done, "Observed original retained-owner defect; preserve same files and expiry", ct); return 0;
            }
            if (mode == "retained-delete-complete")
            {
                Require(File.Exists(Path.Combine(root, "retained-delete-observe.complete")));
                phase = "same-retained-owner-new-authenticated-session-after-fictional-recovery";
                var freshSession = Path.Combine(root, "retained-owner-new-session.complete");
                if (!File.Exists(freshSession))
                {
                    const string email = "household-lifecycle-native-control@example.invalid";
                    using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
                    using var recovery = await http.PostAsJsonAsync("identity/recover", new IdentityInput(email, ""), ct); Require(recovery.StatusCode == HttpStatusCode.Accepted);
                    await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                    var challenge = await db.Challenges.AsNoTracking().Where(row => row.UserId == otherIdentity.Id && row.Purpose == "reset" && !row.Consumed)
                        .OrderByDescending(row => row.ExpiresAt).FirstAsync(ct);
                    var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                        .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                    var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
                    using var completion = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, password), ct); Require(completion.StatusCode == HttpStatusCode.NoContent);
                    Require((await controlTransport.SignInAsync(email, password, ct)).Id == otherIdentity.Id); password = "";
                    await File.WriteAllTextAsync(freshSession, "Only the original fictional identity's authenticated session changed; private device/database keys retained", ct);
                }
                var originalFingerprint = other.DeviceFingerprint;
                other = await controlWorkspace.RebindCurrentDeviceAsync(controlTransport, other.Authority, ct);
                Require(other.DeviceFingerprint == originalFingerprint && !(await controlJournal.ReadAccessAsync(other.Authority, ct)).CanWrite && financialRequests == 0);
                var deniedRead = false; var deniedGrant = false;
                try { _ = await controlTransport.CheckpointAsync(other.SpaceId, other.DeviceId, ct); }
                catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Forbidden) { deniedRead = true; }
                try { _ = await controlTransport.GrantAsync(other.SpaceId, other.DeviceId, 1, ct); }
                catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Forbidden) { deniedGrant = true; }
                Require(deniedRead && deniedGrant && financialRequests == 2);
                phase = "owner-only-privacy-review-with-financial-and-key-resources-still-refused";
                var reviewed = await controlWorkspace.ReviewDeletionCheckpointAsync(controlTransport, other.Authority, ct);
                var retainedIntent = await controlWorkspace.BeginDeletionAsync(controlTransport, other.Authority, reviewed, ct);
                Require(retainedIntent.Receipt is not null && financialRequests == 2);
                await app.Services.GetRequiredService<HouseholdRetirement>().RunOnceAsync(ct);
                var final = await controlWorkspace.ResumeDeletionAsync(controlTransport, other.Authority, ct);
                Require(final.Request == retainedIntent.Request && final.Receipt is { StorageRetired: true } && financialRequests == 2 &&
                    await FinancialDigestAsync(control) == await File.ReadAllTextAsync(Path.Combine(root, "retained-owner-original-financial-digest.txt"), ct));
                var localRefused = false;
                try { await control.Store.SaveAccountAsync(new Account { Name = "Fictitious refused retained/deleted write", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) }, ct); }
                catch (HouseholdWriteRejectedException error) when (error.Reason == HouseholdWriteBlockReason.RemoteRefused) { localRefused = true; }
                Require(localRefused);
                Proof("same-retained-owner-new-session-private-key-rebind-and-exact-privacy-deletion-succeed-without-financial-refresh-or-key-grant-and-preserve-original-local-money");
                await File.WriteAllTextAsync(done, "Original retained owner deletion completed; do not replay or reactivate room", ct); return 0;
            }
            if (mode == "audit-and-expired")
            {
                phase = "actual-original-audit-signature-verification-and-frozen-prefix";
                var first = await controlWorkspace.ReadAuditAsync(controlTransport, other.Authority, 0, null, ct);
                Require(first.Complete && first.Items.Length == 1 && first.Items[0].Action == HouseholdAuditAction.MembershipPublished);
                Require((await control.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(controlTransport, other.Authority, ct)).Applied);
                var oldPrefix = await controlWorkspace.ReadAuditAsync(controlTransport, other.Authority, first.Through, first.Through, ct);
                Require(oldPrefix.Complete && oldPrefix.Items.Length == 0 && oldPrefix.Through == first.Through);
                var publication = await controlWorkspace.ReadAuditAsync(controlTransport, other.Authority, first.Through, null, ct);
                Require(publication.Complete && publication.Items.Length == 1 && publication.Items[0].Action == HouseholdAuditAction.EncryptedUpdate);
                var baseline = await FinancialDigestAsync(control);
                var frozenWorkspace = new HouseholdWorkspace(controlJournal, control.Sync,
                    control.Services.GetRequiredService<HouseholdSyncCoordinator>(), new EarlierClientClock());
                other = await controlJournal.CaptureAsync(peer, otherIdentity.Id, ct) ?? throw new InvalidDataException();
                phase = "expired-original-client-deletion-remains-blocked-until-authenticated-review";
                var refused = false;
                try { _ = await frozenWorkspace.BeginDeletionAsync(controlTransport, other.Authority, other.MembershipVersion, other.Cursor, ct); }
                catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Conflict) { refused = true; }
                var pending = await controlJournal.ReadDeletionAsync(other.Authority, ct) ?? throw new InvalidDataException();
                Require(refused && pending.Receipt is null && !pending.Rejected && !(await controlJournal.ReadAccessAsync(other.Authority, ct)).CanWrite);
                var checkedIntent = await controlWorkspace.ReviewDeletionAsync(controlTransport, other.Authority, ct);
                Require(checkedIntent.Rejected && checkedIntent.Receipt is null && checkedIntent.Request == pending.Request);
                var anchor = await controlJournal.ReadMembershipVersionAsync(other.Authority, checkedIntent.AnchorVersion, ct);
                var declaration = Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(checkedIntent.Request.SignedDeclaration, anchor.Root, HouseholdLifecycleProtocol.DeleteType,
                    HouseholdLifecycleProtocol.DeclarationBytes, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement);
                await controlWorkspace.FinishRejectedDeletionAsync(controlTransport, other.Authority, declaration.OperationId, ct);
                Require(await controlJournal.ReadDeletionAsync(other.Authority, ct) is null && (await controlJournal.ReadAccessAsync(other.Authority, ct)).CanWrite && baseline == await FinancialDigestAsync(control));
                Require((await controlTransport.CheckpointAsync(other.SpaceId, other.DeviceId, ct)).SpaceId == other.SpaceId);
                Proof("original-audit-signatures-and-frozen-prefix-verified-expired-uncertain-consent-blocks-local-writes-until-explicit-authenticated-unaccepted-finish");
                await File.WriteAllTextAsync(done, "Original phase complete; never replay", ct); return 0;
            }
            if (mode == "delete-cut")
            {
                Require(await journal.ReadDeletionAsync(state.Authority, ct) is null);
                phase = "actual-original-owner-deletion";
                _ = await workspace.BeginDeletionAsync(transport, state.Authority, state.MembershipVersion, state.Cursor, ct);
                throw new InvalidDataException("The original effect cut was not exercised.");
            }
            phase = "original-retained-owner-intent-after-process-loss";
            var intent = await journal.ReadDeletionAsync(state.Authority, ct) ?? throw new InvalidDataException();
            Require(!(await journal.ReadAccessAsync(state.Authority, ct)).CanWrite);
            var factory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                var old = await db.Set<HouseholdSpaceRow>().AsNoTracking().SingleAsync(row => row.Id == state.SpaceId, ct);
                Require(!old.Deleted && !old.StorageRetired && await db.Set<HouseholdRecordRow>().AnyAsync(row => row.SpaceId == state.SpaceId, ct));
            }
            var blocked = false;
            try { _ = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Gone) { blocked = true; }
            Require(blocked);
            var observed = await workspace.ReviewDeletionAsync(transport, state.Authority, ct);
            Require(observed.Request == intent.Request && observed.Receipt is { StorageRetired: false } && !observed.Rejected);
            await app.Services.GetRequiredService<HouseholdRetirement>().RunOnceAsync(ct);
            var completed = await workspace.ResumeDeletionAsync(transport, state.Authority, ct);
            Require(completed.Request == intent.Request && completed.Receipt is { StorageRetired: true });
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                Require(!await db.Set<HouseholdRecordRow>().AnyAsync(row => row.SpaceId == state.SpaceId, ct) &&
                    !await db.Set<HouseholdKeyGrantRow>().AnyAsync(row => row.SpaceId == state.SpaceId, ct) &&
                    !await db.Set<HouseholdAuditRow>().AnyAsync(row => row.SpaceId == state.SpaceId, ct));
                Require(await db.Set<HouseholdKeyGrantRow>().AnyAsync(row => row.SpaceId == other.SpaceId, ct));
            }
            Require((await controlTransport.CheckpointAsync(other.SpaceId, other.DeviceId, ct)).SpaceId == other.SpaceId);
            var localBlocked = false;
            try { await client.Store.SaveAccountAsync(new Account { Name = "Fictitious forbidden deleted-room write", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) }, ct); }
            catch (HouseholdWriteRejectedException error) when (error.Reason == HouseholdWriteBlockReason.RemoteRefused) { localBlocked = true; }
            Require(localBlocked && await FinancialDigestAsync(client) == await File.ReadAllTextAsync(Path.Combine(root, "financial-digest.txt"), ct));
            Require(await FinancialDigestAsync(control) == await File.ReadAllTextAsync(Path.Combine(root, "control-financial-digest.txt"), ct));
            Proof(mode == "restore-check" ? "actual-old-postgresql-snapshot-cannot-resurrect-deleted-room-with-original-private-marker-and-cold-protected-local-data-preserved" :
                "real-process-cut-after-original-private-deletion-marker-before-postgresql-commit-resumes-same-protected-consent-and-retires-only-exact-remote-room");
        }
        await File.WriteAllTextAsync(done, "Original phase complete; never replay", ct);
    }
    finally { await app.StopAsync(CancellationToken.None); }
    return 0;
}
catch (Exception error)
{
    await File.WriteAllTextAsync(Path.Combine(root, "failure-" + mode + ".txt"), phase + Environment.NewLine + error.GetType().FullName + Environment.NewLine + error.StackTrace, CancellationToken.None);
    Console.Error.WriteLine("FAILED " + phase + " " + error.GetType().Name); return 1;
}

/// <summary>Only the native process termination effect changes; original private file/HTTP/PostgreSQL transactions are real.</summary>
internal sealed class CutAfterOriginalMarker(IHouseholdDeletionLedger original, string root) : IHouseholdDeletionLedger
{
    /// <inheritdoc />
    public Task<HouseholdDeletionMarker?> ReadAsync(Guid space, CancellationToken ct) => original.ReadAsync(space, ct);
    /// <inheritdoc />
    public async Task<HouseholdDeletionMarker> RecordAsync(HouseholdDeletionMarker marker, CancellationToken ct)
    {
        var result = await original.RecordAsync(marker, ct);
        await File.WriteAllTextAsync(Path.Combine(root, "delete-cut.complete"), "Original marker committed before process termination and database commit", ct);
        Console.WriteLine("actual-original-protected-intent-and-private-marker-retained-before-process-termination"); Environment.Exit(47); return result;
    }
    /// <inheritdoc />
    public HouseholdDeletionInspection Inspect(int offset, int maximum) => original.Inspect(offset, maximum);
}

/// <summary>Only synthetic fixture identity confirmation is decrypted in memory; no password is stored.</summary>
internal sealed record PrivateMail(string Token);

/// <summary>Only the synthetic client's operational proposal clock predates service UTC; actual server/session/native clocks remain unchanged.</summary>
internal sealed class EarlierClientClock : TimeProvider
{
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(-1);
}

/// <summary>Independent raw comparison of all shipped financial columns, without copying financial algorithms.</summary>
internal static class FinancialComparison
{
    /// <summary>Returns one opaque commitment without printing financial contents, keys or credential frames.</summary>
    internal static async Task<string> FinancialDigestAsync(ProtectedClient client)
    {
        await using var db = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
        await db.Database.OpenConnectionAsync(); using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new SortedDictionary<string, List<string>>();
        foreach (var table in HouseholdFinancialSchema.Manifest.Tables)
        {
            static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
            command.CommandText = "SELECT " + string.Join(",", table.Columns.Select(column => Quote(column.Name))) + " FROM " + Quote(table.Name);
            using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray()));
            rows.Sort(StringComparer.Ordinal); tables[table.Name] = rows;
        }
        var clear = JsonSerializer.SerializeToUtf8Bytes(tables); try { return Convert.ToHexStringLower(SHA256.HashData(clear)); } finally { CryptographicOperations.ZeroMemory(clear); }
    }
}
