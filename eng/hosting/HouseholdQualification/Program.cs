using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

if (args is not ["prepare" or "delete" or "restored" or "main-identity" or "main-retain" or "android-identity" or "android-retain", var address] || !IPAddress.TryParse(address, out var proxy))
    throw new ArgumentException("Explicit fictional household hosting stage and owned proxy required.");
var stage = args[0];
var project = Environment.GetEnvironmentVariable("ZANANCE_QUALIFICATION_PROJECT");
if (project != (stage is "restored" or "main-identity" or "main-retain" or "android-identity" or "android-retain" ? "zanance-selfhost-privacy-restore-d294" : "zanance-selfhost-privacy-d294"))
    throw new IOException("Only the independently named fictional household deployments are allowed.");
const string root = "/qualification";
const string authority = "Fictional original household private hosting lifecycle v1";
const string fixturePassword = "Fictitious-Household-Hosting9!";
var names = new[] { "household-hosting-owner@example.invalid", "household-hosting-control@example.invalid", "household-main-lifecycle@example.invalid", "household-main-android@example.invalid" };
var marker = Path.Combine(root, "fixture.marker"); var statePath = Path.Combine(root, "original.protected");
var complete = Path.Combine(root, stage + ".complete"); var phase = "owned-original-private-authority";
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4)); var ct = deadline.Token;
void Require(bool condition) { if (!condition) throw new InvalidDataException("The original household hosted lifecycle invariant failed at " + phase + "."); }
Require(Directory.Exists(root) && !File.Exists(complete));
if (!File.Exists(marker))
{
    Require(stage == "prepare" && !Directory.EnumerateFileSystemEntries(root).Any());
    await File.WriteAllTextAsync(marker, authority, ct);
}
Require(await File.ReadAllTextAsync(marker, ct) == authority);
using var certificate = X509Certificate2.CreateFromPemFile("/identities/protection.crt", "/identities/protection.key");
using var privateProtection = new ServiceCollection().AddDataProtection().SetApplicationName("Zanance.FictionalHouseholdHosting")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "client-protection"))).ProtectKeysWithCertificate(certificate).Services.BuildServiceProvider();
var protector = privateProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector("original-household-client-v1");
Room?[] rooms;
if (File.Exists(statePath))
{
    var encrypted = await File.ReadAllBytesAsync(statePath, ct); var clear = protector.Unprotect(encrypted);
    try { Require(clear.Length <= 1024 * 1024); rooms = JsonSerializer.Deserialize<Room?[]>(clear) ?? throw new InvalidDataException(); Require(rooms.Length == 2); }
    finally { CryptographicOperations.ZeroMemory(clear); CryptographicOperations.ZeroMemory(encrypted); }
}
else { Require(stage == "prepare"); rooms = new Room?[2]; }
async Task SaveAsync()
{
    var clear = JsonSerializer.SerializeToUtf8Bytes(rooms); var protectedBytes = protector.Protect(clear);
    try
    {
        var pending = statePath + ".pending";
        await using (var file = new FileStream(pending, FileMode.Create, FileAccess.Write, FileShare.None))
        { await file.WriteAsync(protectedBytes, ct); file.Flush(flushToDisk: true); }
        File.Move(pending, statePath, overwrite: true);
    }
    finally { CryptographicOperations.ZeroMemory(clear); CryptographicOperations.ZeroMemory(protectedBytes); }
}
var connection = "Host=database;Database=zanance;Username=zanance_owner;SSL Mode=VerifyFull;GSS Encryption Mode=Disable;Root Certificate=/identities/authority.crt;SSL Certificate=/identities/database.crt;SSL Key=/identities/database.key";
await using var db = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
Require((await db.Users.Select(user => user.Email).ToArrayAsync(ct)).All(email => names.Contains(email)));
using var trust = X509CertificateLoader.LoadCertificateFromFile("/proxy-trust/root.crt");
string pin;
using (var tcp = new TcpClient())
{
    await tcp.ConnectAsync(proxy!, 8443, ct); using var tls = new SslStream(tcp.GetStream());
    await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "localhost", CertificateChainPolicy = new X509ChainPolicy
        { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { trust }, RevocationMode = X509RevocationMode.NoCheck } }, ct);
    Require(tls.RemoteCertificate is not null); pin = Convert.ToHexStringLower(SHA256.HashData(tls.RemoteCertificate!.GetRawCertData()));
}
const int relayPort = 17444;
var listener = new TcpListener(IPAddress.Loopback, relayPort); listener.Start();
using var relayStop = CancellationTokenSource.CreateLinkedTokenSource(ct); var relays = new List<Task>();
var accepting = Task.Run(async () =>
{
    try
    {
        while (!relayStop.IsCancellationRequested)
        {
            var incoming = await listener.AcceptTcpClientAsync(relayStop.Token);
            relays.Add(RelayAsync(incoming, proxy!, relayStop.Token));
        }
    }
    catch (OperationCanceledException) when (relayStop.IsCancellationRequested) { }
    catch (SocketException) when (relayStop.IsCancellationRequested) { }
}, ct);
var peer = new LocalSyncPeer("https://localhost:" + relayPort + "/", pin);
var crypto = new HouseholdCryptography(TimeProvider.System); var zero = new string('0', 64);
try
{
    if (stage is "main-identity" or "main-retain" or "android-identity" or "android-retain")
    {
        // These later stages prepare only an explicitly named fictional main-app identity. Never
        // replace either original client's keys, rooms or completed restore evidence.
        Require(File.Exists(Path.Combine(root, "restored.complete")));
        phase = stage; using var http = LocalSyncHttp.CreateClient(peer);
        var name = names[stage.StartsWith("android-", StringComparison.Ordinal) ? 3 : 2];
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == name, ct);
        if (stage.EndsWith("-identity", StringComparison.Ordinal))
        {
            if (user is null)
            {
                using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(name, fixturePassword), ct);
                Require(registration.StatusCode == HttpStatusCode.Accepted);
                user = await db.Users.SingleAsync(user => user.Email == name, ct);
            }
            if (!user.EmailConfirmed)
            {
                var challenge = await db.Challenges.AsNoTracking().SingleAsync(item => item.UserId == user.Id && item.Purpose == "confirm", ct);
                using var serverProtection = new ServiceCollection().AddDataProtection().SetApplicationName("Vafadar.Zanance.Service")
                    .PersistKeysToFileSystem(new DirectoryInfo("/private/keys")).UnprotectKeysWithAnyCertificate(certificate).Services.BuildServiceProvider();
                var mail = JsonSerializer.Deserialize<PrivateMail>(serverProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
                Require(confirmation.StatusCode == HttpStatusCode.NoContent);
            }
            Require(!await db.Set<HouseholdSpaceRow>().AnyAsync(item => item.OwnerId == user.Id, ct));
        }
        else
        {
            Require(user is { EmailConfirmed: true } && File.Exists(Path.Combine(root, stage.StartsWith("android-", StringComparison.Ordinal) ? "android-identity.complete" : "main-identity.complete")));
            var space = await db.Set<HouseholdSpaceRow>().SingleAsync(item => item.OwnerId == user!.Id, ct);
            Require(!space.Deleted);
            space.HostExpiresAt = DateTimeOffset.UtcNow.AddDays(-31);
            await db.SaveChangesAsync(ct);
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Stage = stage, Passed = true, OnlyExplicitFictionalIdentity = true, IdentityId = user!.Id }), ct);
        Console.WriteLine("Independent main-app lifecycle fixture ready: " + stage); return;
    }
    if (stage == "prepare")
    {
        for (var index = 0; index < rooms.Length; index++)
        {
            phase = "original-identity-key-and-room-" + index;
            if (rooms[index] is null)
            {
                using var ownerKey = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create(); using var epoch = HouseholdEpochMaterial.Create();
                rooms[index] = new(Guid.NewGuid(), ownerKey.ExportForProtectedStorage(), device.ExportForProtectedStorage(), epoch.ExportForProtectedStorage()); await SaveAsync();
            }
            var row = rooms[index]!;
            using var http = LocalSyncHttp.CreateClient(peer);
            if (row.Access is null)
            {
                var user = await db.Users.SingleOrDefaultAsync(user => user.Email == names[index], ct);
                if (user is null)
                {
                    using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(names[index], fixturePassword), ct); Require(registration.StatusCode == HttpStatusCode.Accepted);
                    user = await db.Users.SingleAsync(user => user.Email == names[index], ct);
                }
                if (!user.EmailConfirmed)
                {
                    var challenge = await db.Challenges.AsNoTracking().SingleAsync(challenge => challenge.UserId == user.Id && challenge.Purpose == "confirm", ct);
                    using var serverProtection = new ServiceCollection().AddDataProtection().SetApplicationName("Vafadar.Zanance.Service")
                        .PersistKeysToFileSystem(new DirectoryInfo("/private/keys")).UnprotectKeysWithAnyCertificate(certificate).Services.BuildServiceProvider();
                    var mail = JsonSerializer.Deserialize<PrivateMail>(serverProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                    using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct); Require(confirmation.StatusCode == HttpStatusCode.NoContent);
                }
                using var login = await http.PostAsJsonAsync("identity/login", new IdentityInput(names[index], fixturePassword), ct); Require(login.StatusCode == HttpStatusCode.OK);
                var tokens = await login.Content.ReadFromJsonAsync<JsonElement>(ct);
                row = row with { Identity = user.Id, Access = tokens.GetProperty("accessToken").GetString() }; rooms[index] = row; await SaveAsync();
            }
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", row.Access);
            using var rootKey = SigningRecipientIdentity.ImportFromProtectedStorage(row.Root); using var deviceKey = SigningRecipientIdentity.ImportFromProtectedStorage(row.Device);
            using var material = HouseholdEpochMaterial.ImportFromProtectedStorage(row.Epoch);
            HouseholdPublicKeys Public(SigningRecipientIdentity key) => new(key.Public.SigningKey, key.Public.WrappingKey);
            if (row.Reservation is null)
            {
                row = row with { Reservation = await PostAsync(http, "households/", new HouseholdReserveSpace(row.Operation, new('a', 64), Public(rootKey), Public(deviceKey)),
                    HouseholdMetadataJsonContext.Default.HouseholdReserveSpace, HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation) };
                rooms[index] = row; await SaveAsync();
            }
            var reservation = row.Reservation!; var path = "households/" + reservation.SpaceId;
            if (row.Genesis is null)
            {
                var statement = new HouseholdMembershipStatement(1, reservation.SpaceId, row.Operation, HouseholdMembershipChange.Genesis, null, 0, 1, zero,
                    row.Identity!, row.Identity!, Public(rootKey), material.Epoch, material.KeyCommitment, material.AddressKeyCommitment,
                    DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(), [new(row.Identity!, HouseholdRole.Owner, zero, [new(reservation.DeviceId, Public(deviceKey))])]);
                var grant = material.Grant(crypto, statement, rootKey, statement.Members[0].Devices[0]);
                row = row with { Genesis = new(reservation.DeviceId, Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, rootKey, "zanance-household-membership+jwt"),
                    null, null, [new(1, reservation.DeviceId, grant, HouseholdPublicationDigest.Ciphertext(grant))]) };
                rooms[index] = row; await SaveAsync();
            }
            if (row.Checkpoint is null)
            {
                row = row with { Checkpoint = await PostAsync(http, path + "/membership", row.Genesis!, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint) };
                rooms[index] = row; await SaveAsync();
            }
            if (row.Publication is null)
            {
                var operation = Guid.NewGuid(); var attempt = Guid.NewGuid(); var clear = Encoding.UTF8.GetBytes("Fictitious independently preserved hosted household ciphertext " + index);
                var encrypted = material.Encrypt(crypto, new(reservation.SpaceId, 1, operation, HouseholdProtectionPurpose.Financial, "members"), clear, deviceKey);
                var part = new HouseholdOpaquePart(0, material.RecordAddress("FictitiousHostedRecord", index.ToString(System.Globalization.CultureInfo.InvariantCulture)), 0, encrypted, HouseholdPublicationDigest.Ciphertext(encrypted));
                var manifest = HouseholdPublicationDigest.Manifest([new(part.Index, part.RecordId, part.BaseVersion, part.Digest)]);
                var signed = Sign(new HouseholdCommitStatement(1, reservation.SpaceId, reservation.DeviceId, operation, attempt, 0, 1, 1, 1, zero, 1, manifest),
                    HouseholdMetadataJsonContext.Default.HouseholdCommitStatement, deviceKey, "zanance-household-commit+jwt");
                row = row with { Publication = new(new(reservation.DeviceId, operation, attempt, 0, 1, 1, new('a', 64)), part, new(reservation.DeviceId, signed), null, false,
                    Convert.ToHexStringLower(SHA256.HashData(clear))) }; CryptographicOperations.ZeroMemory(clear);
                rooms[index] = row; await SaveAsync();
            }
            var publication = row.Publication!;
            if (publication.Round is null)
            {
                var handle = await PostAsync(http, path + "/rounds", publication.Open, HouseholdMetadataJsonContext.Default.HouseholdOpenRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
                row = row with { Publication = publication with { Round = handle.Id } }; rooms[index] = row; await SaveAsync(); publication = row.Publication!;
            }
            if (!publication.Committed)
            {
                using var partResponse = await http.PutAsJsonAsync(path + $"/rounds/{publication.Round}/parts", publication.Part, HouseholdMetadataJsonContext.Default.HouseholdOpaquePart, ct); Require(partResponse.StatusCode == HttpStatusCode.NoContent);
                var committed = await PostAsync(http, path + $"/rounds/{publication.Round}/commit", publication.Commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
                Require(committed.CommittedCursor == 1);
                row = row with { Publication = publication with { Committed = true }, Checkpoint = await http.GetFromJsonAsync(path + $"/checkpoint?device={reservation.DeviceId}", HouseholdMetadataJsonContext.Default.HouseholdCheckpoint, ct) };
                rooms[index] = row; await SaveAsync();
            }
            await VerifyCiphertextAsync(http, row);
        }
    }
    else
    {
        Require(rooms.All(row => row is { Publication.Committed: true, Checkpoint.Cursor: 1 }));
        var original = rooms[0]!; using var http = Client(original); var reservation = original.Reservation!; var path = "households/" + reservation.SpaceId;
        if (stage == "delete")
        {
            phase = "exact-original-owner-deletion-before-latest-private-snapshot";
            if (original.Deletion is null)
            {
                using var signing = SigningRecipientIdentity.ImportFromProtectedStorage(original.Root);
                var point = original.Checkpoint!;
                original = original with { Deletion = new(reservation.DeviceId, Sign(new HouseholdDeleteStatement(1, reservation.SpaceId, Guid.NewGuid(), original.Identity!, reservation.DeviceId,
                    point.MembershipVersion, point.MembershipHead, point.Cursor, point.PublicationHead, DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()),
                    HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement, signing, HouseholdLifecycleProtocol.DeleteType)) };
                rooms[0] = original; await SaveAsync();
            }
            var receipt = await PostAsync(http, path + "/deletion", original.Deletion!, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, HouseholdMetadataJsonContext.Default.HouseholdDeletionReceipt);
            rooms[0] = original = original with { Receipt = receipt }; await SaveAsync();
        }
        phase = stage == "restored" ? "actual-full-snapshot-restored-service-original-session-and-deletion-authority" : "exact-room-retirement-and-independent-control";
        var session = await http.GetFromJsonAsync("households/session", HouseholdMetadataJsonContext.Default.HouseholdSessionIdentity, ct); Require(session?.IdentityId == original.Identity);
        for (var wait = 0; wait < 30; wait++)
        {
            var receipt = await http.GetFromJsonAsync(path + $"/deletion/{original.Receipt!.OperationId}", HouseholdMetadataJsonContext.Default.HouseholdDeletionReceipt, ct) ?? throw new InvalidDataException();
            Require(receipt with { StorageRetired = original.Receipt.StorageRetired } == original.Receipt);
            if (receipt.StorageRetired) break;
            Require(wait < 29); await Task.Delay(1000, ct);
        }
        using (var denied = await http.GetAsync(path + $"/feed?device={reservation.DeviceId}&afterCursor=0&afterRecord=&through=1", ct)) Require(denied.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone);
        using (var denied = await http.GetAsync(path + $"/grants/1?device={reservation.DeviceId}", ct)) Require(denied.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone);
        var status = await http.GetFromJsonAsync(path + $"/lifecycle?device={reservation.DeviceId}", HouseholdMetadataJsonContext.Default.HouseholdLifecycleStatus, ct);
        Require(status is { Retention: HouseholdRetentionState.Deleted, StorageRetired: true });
        Require(!await db.Set<HouseholdRecordRow>().AnyAsync(row => row.SpaceId == reservation.SpaceId, ct) &&
            !await db.Set<HouseholdKeyGrantRow>().AnyAsync(row => row.SpaceId == reservation.SpaceId, ct) &&
            !await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.SpaceId == reservation.SpaceId, ct));
        using var control = Client(rooms[1]!); await VerifyCiphertextAsync(control, rooms[1]!);
        Require(await db.Set<HouseholdRecordRow>().CountAsync(row => row.SpaceId == rooms[1]!.Reservation!.SpaceId, ct) == 1);
    }
    await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Stage = stage, Passed = true, ActualProxyTls = true, OriginalSignedEncryptedServiceProtocol = true,
        ActualStoredCiphertext = true, IndependentRoomPreserved = true, LatestPermanentDeletion = stage != "prepare", MainFinancialDomainAcceptance = false, PublicHostingOrRelease = false }), ct);
    Console.WriteLine("Original hosted household lifecycle passed: " + stage);
}
finally
{
    relayStop.Cancel(); listener.Stop(); await accepting;
    try { await Task.WhenAll(relays); } catch (Exception error) when (error is IOException or OperationCanceledException or SocketException) { }
    foreach (var row in rooms.OfType<Room>()) { CryptographicOperations.ZeroMemory(row.Root); CryptographicOperations.ZeroMemory(row.Device); CryptographicOperations.ZeroMemory(row.Epoch); }
}

HttpClient Client(Room row)
{
    var http = LocalSyncHttp.CreateClient(peer); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", row.Access); return http;
}
async Task<TOut> PostAsync<TIn, TOut>(HttpClient http, string path, TIn body, JsonTypeInfo<TIn> input, JsonTypeInfo<TOut> output)
{
    using var response = await http.PostAsJsonAsync(path, body, input, ct); Require(response.StatusCode == HttpStatusCode.OK);
    return await response.Content.ReadFromJsonAsync(output, ct) ?? throw new InvalidDataException();
}
async Task VerifyCiphertextAsync(HttpClient http, Room row)
{
    var reservation = row.Reservation!;
    var page = await http.GetFromJsonAsync($"households/{reservation.SpaceId}/feed?device={reservation.DeviceId}&afterCursor=0&afterRecord=&through=1", HouseholdMetadataJsonContext.Default.HouseholdFeedPage, ct);
    Require(page is { Complete: true, Events.Length: 1 } && page.Events[0].Ciphertext == row.Publication!.Part.Ciphertext);
    using var epoch = HouseholdEpochMaterial.ImportFromProtectedStorage(row.Epoch); using var device = SigningRecipientIdentity.ImportFromProtectedStorage(row.Device);
    var clear = epoch.Decrypt(crypto, new(reservation.SpaceId, 1, row.Publication!.Open.OperationId, HouseholdProtectionPurpose.Financial, "members"), page!.Events[0].Ciphertext, device.Public);
    try { Require(Convert.ToHexStringLower(SHA256.HashData(clear)) == row.Publication.ClearHash); } finally { CryptographicOperations.ZeroMemory(clear); }
}
static string Sign<T>(T value, JsonTypeInfo<T> json, SigningRecipientIdentity identity, string purpose) => StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(value, json), identity, purpose);
static async Task RelayAsync(TcpClient incoming, IPAddress proxy, CancellationToken ct)
{
    using (incoming)
    using (var upstream = new TcpClient())
    {
        await upstream.ConnectAsync(proxy, 8443, ct); using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var left = incoming.GetStream().CopyToAsync(upstream.GetStream(), stop.Token); var right = upstream.GetStream().CopyToAsync(incoming.GetStream(), stop.Token);
        await Task.WhenAny(left, right); stop.Cancel(); try { await Task.WhenAll(left, right); } catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }
}

/// <summary>Only independently encrypted fictional client state; no clear frame or credential enters the server or reports.</summary>
internal sealed record Room(Guid Operation, byte[] Root, byte[] Device, byte[] Epoch, string? Identity = null, string? Access = null,
    HouseholdSpaceReservation? Reservation = null, HouseholdPublishMembership? Genesis = null, HouseholdCheckpoint? Checkpoint = null,
    Publication? Publication = null, HouseholdDeleteRequest? Deletion = null, HouseholdDeletionReceipt? Receipt = null);
/// <summary>Original finite public/opaque publication intent, retained before every HTTP effect.</summary>
internal sealed record Publication(HouseholdOpenRound Open, HouseholdOpaquePart Part, HouseholdCommitRound Commit, Guid? Round, bool Committed, string ClearHash);
/// <summary>Only this exact synthetic identity confirmation is decrypted in memory.</summary>
internal sealed record PrivateMail(string Token);
