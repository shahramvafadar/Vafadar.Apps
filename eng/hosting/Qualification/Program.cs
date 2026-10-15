using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

if (args is not ["initial" or "cold" or "restored" or "public" or "public-refused" or "boundary" or "attachment" or "restored-files" or "renewed", var proxyAddress] || !IPAddress.TryParse(proxyAddress, out var proxy))
    throw new InvalidOperationException("Explicit fictional stage and owned proxy address required.");
var stage = args[0];
SQLitePCL.Batteries_V2.Init();
if (Environment.GetEnvironmentVariable("ZANANCE_QUALIFICATION_PROJECT") is not
    ("zanance-selfhost" or "zanance-selfhost-restore-d293" or "zanance-selfhost-restore-files-d293"))
    throw new InvalidOperationException("Only the explicitly named fictional deployment projects can be qualified.");
const string root = "/qualification";
const string email = "hosting-qualification@example.invalid";
const string password = "Fictitious-Hosting-Qualification9!";
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4)); var ct = deadline.Token;
Directory.CreateDirectory(root);
var marker = Path.Combine(root, "fictitious.marker");
var proofPath = Path.Combine(root, "original.json");
if (stage == "initial")
{
    Check(!File.Exists(marker) && !File.Exists(proofPath), "Fresh isolated qualification required; existing fixtures are never reset.");
    await File.WriteAllTextAsync(marker, "Zanance fictional hosted qualification v1", ct);
}
else Check(await File.ReadAllTextAsync(marker, ct) == "Zanance fictional hosted qualification v1", "Retained fictional authority required.");
var output = Path.Combine(root, stage + "-proof.json");
Check(!File.Exists(output), "Successful stages must not be repeated.");
var connection = "Host=database;Database=zanance;Username=zanance_owner;SSL Mode=VerifyFull;GSS Encryption Mode=Disable;Root Certificate=/identities/authority.crt;SSL Certificate=/identities/database.crt;SSL Key=/identities/database.key";
await using var db = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
// This caller must never access a service that contains an owner account or unrelated identity.
var users = await db.Users.Select(user => user.Email).ToListAsync(ct);
Check(stage == "initial" ? users.Count == 0 : users.Count == 1 && users[0] == email, "Only the empty or exact fictional identity fixture is allowed.");
using var trust = X509CertificateLoader.LoadCertificateFromFile("/proxy-trust/root.crt");
if (stage == "boundary")
{
    using var authority = X509CertificateLoader.LoadCertificateFromFile("/identities/authority.crt");
    using var direct = new TcpClient(); await direct.ConnectAsync("api", 8443, ct);
    using var directTls = new SslStream(direct.GetStream());
    await directTls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
    {
        TargetHost = "api", CertificateChainPolicy = new X509ChainPolicy
        { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { authority }, RevocationMode = X509RevocationMode.NoCheck },
    }, ct);
    // Keep validated TLS SNI separate from the intentionally forged HTTP authority. HttpClient changes
    // SNI when Host is overridden, which tests a different certificate-name refusal before HTTP (D-293).
    var host = Environment.GetEnvironmentVariable("ZANANCE_EXPECTED_AUTHORITY") ?? "localhost:14543";
    await directTls.WriteAsync(Encoding.ASCII.GetBytes("GET /sync/devices HTTP/1.1\r\nHost: " + host
        + "\r\nX-Forwarded-For: " + proxyAddress + "\r\nX-Forwarded-Proto: https\r\nConnection: close\r\n\r\n"), ct);
    using var response = new StreamReader(directTls, Encoding.ASCII, leaveOpen: true);
    var line = await response.ReadLineAsync(ct);
    Check(line?.StartsWith("HTTP/1.1 421 ", StringComparison.Ordinal) == true, "A direct caller impersonated the known reverse proxy through headers.");
    var missing = new NpgsqlConnectionStringBuilder(connection); missing.Remove("SSL Certificate"); missing.Remove("SSL Key");
    await RefuseDatabaseAsync(missing.ConnectionString, null, "28000");
    var mismatch = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" };
    await RefuseDatabaseAsync(mismatch.ConnectionString, null, "28000");
    var runtime = new NpgsqlConnectionStringBuilder(connection)
    { Username = "zanance_runtime", SslCertificate = "/runtime-identities/database.crt", SslKey = "/runtime-identities/database.key" };
    await RefuseDatabaseAsync(runtime.ConnectionString, "CREATE TABLE public.qualification_forbidden(id integer)", "42501");
    await RefuseDatabaseAsync(runtime.ConnectionString, """DELETE FROM public."__EFMigrationsHistory" WHERE false""", "42501");
    await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { Stage = stage, Passed = true,
        SpoofedForwardedAuthorityRefused = true, MissingClientCertificateRefused = true, WrongRoleCertificateRefused = true,
        ActualRuntimeDdlRefused = true, ActualMigrationHistoryWriteRefused = true }), ct);
    Console.WriteLine("Original hosted fixture stage passed: " + stage); return;
}
if (stage is "public" or "public-refused")
{
    await RunAsync(new("https://api.zanance.vafadar.pro/", ""));
    return;
}
using (var tcp = new TcpClient())
{
    await tcp.ConnectAsync(proxy, 8443, ct);
    using var tls = new SslStream(tcp.GetStream());
    await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
    {
        TargetHost = "localhost", CertificateChainPolicy = new X509ChainPolicy
        { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { trust }, RevocationMode = X509RevocationMode.NoCheck },
    }, ct);
    Check(tls.RemoteCertificate is not null, "Actual private-CA proxy TLS identity required.");
    var fingerprint = Convert.ToHexStringLower(SHA256.HashData(tls.RemoteCertificate!.GetRawCertData()));
    await RunAsync(new("https://localhost:17443/", fingerprint));
}

async Task RunAsync(LocalSyncPeer peer)
{
    // A local-only TCP relay preserves the real TLS bytes and original hostname/pin policy.
    // It adapts only this independent Linux caller's namespace; the application transport is unchanged.
    var listener = new TcpListener(IPAddress.Loopback, peer.Address.Port); listener.Start();
    using var relayStop = CancellationTokenSource.CreateLinkedTokenSource(ct);
    var relays = new List<Task>();
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
    try
    {
        using var http = LocalSyncHttp.CreateClient(peer);
        if (stage == "public-refused")
        {
            try { using var unexpected = await http.GetAsync("health/ready", ct); }
            catch (HttpRequestException error) when (error.InnerException is System.Security.Authentication.AuthenticationException)
            {
                await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
                { Stage = stage, Passed = true, OriginalPublicTransport = true, UntrustedChainRefused = true, NoCredentialSubmitted = true }), ct);
                Console.WriteLine("Original hosted fixture stage passed: " + stage); return;
            }
            throw new InvalidOperationException("An untrusted hosted certificate was accepted by the original client.");
        }
        using var health = await http.GetAsync("health/ready", ct); Check(health.IsSuccessStatusCode, "Real reverse-proxy readiness failed.");
        if (stage == "initial")
        {
            using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct);
            Check(registration.StatusCode == HttpStatusCode.Accepted, "Original registration failed.");
            var user = await db.Users.SingleAsync(row => row.Email == email, ct);
            var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
            using var certificate = X509Certificate2.CreateFromPemFile("/identities/protection.crt", "/identities/protection.key");
            using var services = new ServiceCollection().AddDataProtection().SetApplicationName("Vafadar.Zanance.Service")
                .PersistKeysToFileSystem(new DirectoryInfo("/private/keys")).UnprotectKeysWithAnyCertificate(certificate).Services.BuildServiceProvider();
            var mail = JsonSerializer.Deserialize<PrivateMail>(services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail))!;
            using var confirmed = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
            Check(confirmed.StatusCode == HttpStatusCode.NoContent, "Original confirmation failed.");
            using var first = new OriginalClient(Path.Combine(root, "first.sqlite"), peer);
            var owner = await first.Transport.SignInAsync(email, password, ct);
            var device = await first.Transport.RegisterDeviceAsync("Fictitious hosted sender", ct);
            var account = new Account { Name = "Fictitious hosted KWD", CurrencyCode = "KWD", OpeningBalance = 12345, OpeningDate = new(2026, 10, 15) };
            await first.Finances.SaveAccountAsync(account, ct);
            var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1400, Date = new(2026, 10, 15), Title = "Fictitious hosted preserved expense" };
            Check((await first.Finances.SaveEntryAsync(entry, ct)).Succeeded, "Original financial write failed.");
            var profile = await first.Transport.CreateProfileAsync(new(device.Id, Guid.NewGuid(), "Fictitious hosted profile", SyncSchema.Hash), ct);
            await first.Sync.ConnectAsync(await first.Sync.ReviewAsync(ct), peer, owner.Id, device.Id, profile, ct);
            Check((await first.Coordinator.SynchronizeAsync(first.Transport, ct)).Applied, "Original hosted publication failed.");
            await File.WriteAllTextAsync(proofPath, JsonSerializer.Serialize(new Fixture(owner.Id, profile.Id, account.Id, entry.Id)), ct);
        }
        var original = JsonSerializer.Deserialize<Fixture>(await File.ReadAllTextAsync(proofPath, ct))!;
        using var receiver = new OriginalClient(Path.Combine(root, stage + "-receiver.sqlite"), peer);
        var identity = await receiver.Transport.SignInAsync(email, password, ct); Check(identity.Id == original.Owner, "Original identity changed.");
        var receiverDevice = await receiver.Transport.RegisterDeviceAsync("Fictitious hosted receiver " + stage, ct);
        var remote = (await receiver.Transport.ProfilesAsync(receiverDevice.Id, ct)).Single(row => row.Id == original.Profile);
        await receiver.Sync.ConnectAsync(await receiver.Sync.ReviewAsync(ct), peer, identity.Id, receiverDevice.Id, remote, ct, receiveFirstRun: true);
        Check((await receiver.Coordinator.SynchronizeAsync(receiver.Transport, ct)).Applied, "Original hosted reception failed.");
        var attachmentProof = Path.Combine(root, "attachment.json");
        if (stage == "attachment")
        {
            Check(!File.Exists(attachmentProof), "Existing attachment publication must not be repeated.");
            var attachment = new EntryAttachment { EntryId = original.Entry, FileName = "fictitious-hosted-receipt.pdf",
                ContentType = "application/pdf", Data = FictitiousPdf() };
            await receiver.Finances.AddAttachmentAsync(attachment, ct);
            Check((await receiver.Coordinator.SynchronizeAsync(receiver.Transport, ct)).Applied, "Original private attachment publication failed.");
            await File.WriteAllTextAsync(attachmentProof, JsonSerializer.Serialize(new AttachmentFixture(attachment.Id,
                attachment.Data.Length, Convert.ToHexStringLower(SHA256.HashData(attachment.Data)))), ct);
        }
        if (File.Exists(attachmentProof))
        {
            var expected = JsonSerializer.Deserialize<AttachmentFixture>(await File.ReadAllTextAsync(attachmentProof, ct))!;
            var received = await receiver.Finances.GetAttachmentAsync(expected.Id, ct);
            Check(received is not null && received.Data.Length == expected.Length &&
                Convert.ToHexStringLower(SHA256.HashData(received.Data)) == expected.Sha256,
                "Original private attachment bytes differ after publication or isolated restore.");
        }
        var accountReceived = (await receiver.Finances.GetAccountsAsync(cancellationToken: ct)).Single();
        var entryReceived = (await receiver.Finances.GetEntriesAsync(cancellationToken: ct)).Single();
        Check(accountReceived.Id == original.Account && accountReceived.CurrencyCode == "KWD" && accountReceived.OpeningBalance == 12345
            && entryReceived.Id == original.Entry && entryReceived.Amount == 1400, "Original complete financial values changed.");
        await using var sql = new NpgsqlConnection(connection); await sql.OpenAsync(ct);
        await using var grants = new NpgsqlCommand("""SELECT NOT has_schema_privilege('zanance_runtime','public','CREATE') AND NOT has_table_privilege('zanance_runtime','"__EFMigrationsHistory"','INSERT')""", sql);
        Check((bool)(await grants.ExecuteScalarAsync(ct))!, "Runtime unexpectedly owns migration/schema authority.");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { Stage = stage, Passed = true, OriginalIdentity = true,
            ActualProxyTls = true, OriginalProductionClients = true, CompleteFinancialValues = true, RuntimeCannotMigrate = true,
            ApprovedPublicSystemTrust = peer.UsesSystemTrust,
            PrivateAttachmentVerified = File.Exists(attachmentProof),
            MainAppDataTouched = false, PublicHostingOrReleaseAcceptance = false }), ct);
        Console.WriteLine("Original hosted fixture stage passed: " + stage);
    }
    finally
    {
        relayStop.Cancel(); listener.Stop(); await accepting;
        try { await Task.WhenAll(relays); } catch (Exception error) when (error is IOException or OperationCanceledException or SocketException) { }
    }
}
static async Task RelayAsync(TcpClient incoming, IPAddress proxy, CancellationToken ct)
{
    using (incoming)
    using (var upstream = new TcpClient())
    {
        await upstream.ConnectAsync(proxy, 8443, ct);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var left = incoming.GetStream().CopyToAsync(upstream.GetStream(), stop.Token);
        var right = upstream.GetStream().CopyToAsync(incoming.GetStream(), stop.Token);
        await Task.WhenAny(left, right); stop.Cancel();
        try { await Task.WhenAll(left, right); } catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }
}
static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
static byte[] FictitiousPdf()
{
    // A real minimal PDF with a large legal comment in its content stream exercises nonempty private files.
    var content = "BT /F1 12 Tf 36 100 Td (Fictitious hosted attachment) Tj ET\n%" + new string('X', 300_000) + "\n";
    var objects = new[]
    {
        "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
        "<< /Length " + System.Text.Encoding.ASCII.GetByteCount(content) + " >>\nstream\n" + content + "endstream",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    };
    using var bytes = new MemoryStream();
    void Write(string value) => bytes.Write(System.Text.Encoding.ASCII.GetBytes(value));
    Write("%PDF-1.4\n"); var offsets = new List<long>();
    for (var index = 0; index < objects.Length; index++)
    { offsets.Add(bytes.Position); Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n"); }
    var xref = bytes.Position; Write("xref\n0 6\n0000000000 65535 f \n");
    foreach (var offset in offsets) Write(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture) + " 00000 n \n");
    Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"); return bytes.ToArray();
}
async Task RefuseDatabaseAsync(string configuration, string? statement, string expectedState)
{
    try
    {
        await using var connectionToRefuse = new NpgsqlConnection(configuration); await connectionToRefuse.OpenAsync(ct);
        if (statement is not null)
        { await using var query = new NpgsqlCommand(statement, connectionToRefuse); await query.ExecuteNonQueryAsync(ct); }
    }
    catch (PostgresException error) when (error.SqlState == expectedState) { return; }
    throw new InvalidOperationException("Original database accepted a forbidden certificate or runtime operation.");
}

/// <summary>Public fictional identifiers only, retained across container replacement and isolated restore.</summary>
internal sealed record Fixture(string Owner, Guid Profile, Guid Account, Guid Entry);
/// <summary>Public fictional content identity only; no private byte payload or service session is persisted here.</summary>
internal sealed record AttachmentFixture(Guid Id, int Length, string Sha256);
/// <summary>Composes original client services; this caller implements no financial or protocol algorithm.</summary>
internal sealed class OriginalClient : IDisposable
{
    private readonly ServiceProvider services;
    /// <summary>Original endpoint-pinned transport.</summary>
    internal PersonalSyncTransport Transport { get; }
    /// <summary>Original financial writer and reader.</summary>
    internal ZananceStore Finances => services.GetRequiredService<ZananceStore>();
    /// <summary>Original durable client journal.</summary>
    internal PersonalSyncStore Sync => services.GetRequiredService<PersonalSyncStore>();
    /// <summary>Original resumable coordinator.</summary>
    internal PersonalSyncCoordinator Coordinator => services.GetRequiredService<PersonalSyncCoordinator>();
    /// <summary>Creates only this independently owned SQLite fixture.</summary>
    internal OriginalClient(string path, LocalSyncPeer peer)
    {
        services = new ServiceCollection().AddSingleton(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>(); Transport = new(peer, new SessionMemory());
    }
    /// <inheritdoc />
    public void Dispose() { Transport.Dispose(); services.Dispose(); }
    /// <summary>Fictional request-session memory, never an owner credential store.</summary>
    private sealed class SessionMemory : IPersonalSyncTokenStore
    {
        private readonly Dictionary<string, PersonalSyncSession> sessions = [];
        /// <inheritdoc />
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult(sessions.GetValueOrDefault(peer));
        /// <inheritdoc />
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) { sessions[peer] = session; return Task.CompletedTask; }
        /// <inheritdoc />
        public Task RemoveAsync(string peer, CancellationToken ct) { sessions.Remove(peer); return Task.CompletedTask; }
    }
}
