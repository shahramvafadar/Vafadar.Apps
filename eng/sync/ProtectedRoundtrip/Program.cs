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
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

// Only explicitly owned fictional roots are accepted. Failed files and keys are retained for continuation.
if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] is not ("resume" or "android-host"))) throw new ArgumentException("An owned root and explicit mode are required.");
var root = Path.GetFullPath(args[0]); var resume = args.Length == 2 && args[1] == "resume";
var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "protected-sync")) + Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new IOException("An independent repository artifact root is required.");
var fixture = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The original fictional service root is required.");
if (await File.ReadAllTextAsync(Path.Combine(fixture, "fixture.marker")) != "Zanance fictional personal sync acceptance v1") throw new IOException("The service fixture marker is required.");
foreach (var owned in new[] { root, fixture })
    for (var part = new DirectoryInfo(owned); part is not null; part = part.Parent)
        if (part.Exists && (part.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected fixtures are refused.");
const string marker = "Fictitious protected HTTP sync v1";
if (resume)
{
    if (await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker")) != marker) throw new IOException("The retained original root is required.");
}
else
{
    if (Directory.Exists(root)) throw new IOException("The fresh root already exists; preserve it.");
    Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), marker);
}
if (args.Length == 2 && args[1] == "android-host") return await AndroidNetworkHost.RunAsync(root, fixture);
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(12)); var ct = deadline.Token;
var stage = "configuration";
void Check(bool value) { if (!value) throw new InvalidDataException("The current qualification invariant failed."); }
void Proof(string name) { File.AppendAllText(Path.Combine(root, "checkpoints.txt"), name + Environment.NewLine); Console.WriteLine(name); }
try
{
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
    var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB"));
    Check(connection.Host is "127.0.0.1" or "localhost" && connection.Database?.StartsWith("zanance_identity_sync_", StringComparison.Ordinal) == true);
    connection.Username = "zanance_runtime";
    var port = resume ? int.Parse(await File.ReadAllTextAsync(Path.Combine(root, "service-port.txt"), ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = connection.ConnectionString, ["Service:Root"] = Path.Combine(fixture, "service") });
    builder.AddZananceService(); builder.Logging.ClearProviders();
    builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, port));
    await using var app = builder.Build(); int loseCommit = 0, refusePiece = 0, pieceZeroReads = 0;
    app.Use(async (http, next) =>
    {
        var path = http.Request.Path.Value ?? "";
        if (http.Request.Method == "GET" && path.Contains("/blobs/", StringComparison.Ordinal))
        {
            if (path.EndsWith("/parts/0", StringComparison.Ordinal)) Interlocked.Increment(ref pieceZeroReads);
            if (path.EndsWith("/parts/1", StringComparison.Ordinal) && Interlocked.CompareExchange(ref refusePiece, 0, 1) == 1)
            { http.Response.StatusCode = 503; return; }
        }
        if (!path.EndsWith("/commit", StringComparison.Ordinal) || Interlocked.CompareExchange(ref loseCommit, 0, 1) != 1)
        { await next(http); return; }
        var actual = http.Response.Body; await using var suppressed = new MemoryStream(); http.Response.Body = suppressed;
        try { await next(http); } finally { http.Response.Body = actual; }
        http.Abort();
    });
    app.UseZananceService(); app.MapZananceService(); await app.StartAsync(ct);
    try
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(fixture, "service", "local-tls.fingerprint"), ct));
        if (!resume) await File.WriteAllTextAsync(Path.Combine(root, "service-port.txt"), new Uri(address).Port.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
        stage = "protected-clients";
        using var a = await ProtectedClient.OpenAsync(Path.Combine(root, "a"), peer, !resume, ct);
        using var b = await ProtectedClient.OpenAsync(Path.Combine(root, "b"), peer, !resume, ct);
        if (resume)
        {
            stage = "second-process-session";
            var identityA = await a.Transport.IdentityAsync(ct); var identityB = await b.Transport.IdentityAsync(ct); Check(identityA.Id == identityB.Id);
            Check((await a.Store.GetAccountsAsync(cancellationToken: ct)).Single().Name == "Fictitious protected cash");
            Check((await b.Store.GetAccountsAsync(cancellationToken: ct)).Single().Name == "Fictitious protected cash");
            Check((await a.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0 && (await b.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
            Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied && (await b.Coordinator.SynchronizeAsync(b.Transport, ct)).Applied);
            Proof("second-process-same-protected-generations-and-wrapped-sessions-pass");
        }
        else
        {
            stage = "fictional-enrollment";
            var email = "protected-" + Guid.NewGuid().ToString("N") + "@example.invalid";
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
            using (var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System))
            {
                using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct); Check(registration.StatusCode == HttpStatusCode.Accepted);
                await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(value => value.Email == email, ct);
                var challenge = await db.Challenges.AsNoTracking().SingleAsync(value => value.UserId == user.Id && value.Purpose == "confirm", ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct); Check(confirmation.StatusCode == HttpStatusCode.NoContent);
            }
            var identity = await a.Transport.SignInAsync(email, password, ct); Check((await b.Transport.SignInAsync(email, password, ct)).Id == identity.Id);
            password = "";
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious protected A", ct); var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious protected B", ct);
            var remote = await a.Transport.CreateProfileAsync(new SyncCreateProfile(deviceA.Id, Guid.NewGuid(), "Fictitious protected HTTP copy", SyncSchema.Hash), ct);
            var cash = new Account { Name = "Fictitious protected cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 15), OpeningBalance = 100000 };
            await a.Store.SaveAccountAsync(cash, ct);
            var entry = new LedgerEntry { AccountId = cash.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 15), Amount = 1234, Title = "Fictitious protected purchase" };
            Check((await a.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var bytes = RandomNumberGenerator.GetBytes(3 * 1024 * 1024);
            var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-opaque.pdf", ContentType = "application/pdf", Data = bytes };
            await a.Store.AddAttachmentAsync(attachment, ct);
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(ct), peer, identity.Id, deviceA.Id, remote, ct);
            stage = "lost-commit-response"; loseCommit = 1;
            Exception? lost = null; try { await a.Coordinator.SynchronizeAsync(a.Transport, ct); } catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException) { lost = ex; }
            Check(lost is not null && loseCommit == 0); Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied);
            Proof("protected-upload-lost-ack-resumes-original-round-pass");
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(ct), peer, identity.Id, deviceB.Id, remote, ct);
            stage = "interrupted-attachment-download"; refusePiece = 1;
            Exception? interrupted = null; try { await b.Coordinator.SynchronizeAsync(b.Transport, ct); } catch (PersonalSyncHttpException ex) when (ex.Status == HttpStatusCode.ServiceUnavailable) { interrupted = ex; }
            Check(interrupted is not null && refusePiece == 0 && pieceZeroReads == 1);
            Check((await b.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
            Check((await b.Coordinator.SynchronizeAsync(b.Transport, ct)).Applied && pieceZeroReads == 1);
            Check((await b.Store.GetAttachmentAsync(attachment.Id, ct))!.Data.AsSpan().SequenceEqual(bytes));
            Check((await b.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1234);
            Proof("protected-partial-download-no-partial-money-resumes-missing-piece-pass");
            stage = "competing-amounts";
            entry.Amount = 1300; Check((await a.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var other = (await b.Store.GetEntriesAsync(cancellationToken: ct)).Single(); other.Amount = 1400; Check((await b.Store.SaveEntryAsync(other, ct)).Succeeded);
            Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied); Check((await b.Coordinator.SynchronizeAsync(b.Transport, ct)).NeedsReview);
            var review = (await b.Sync.ConflictsAsync(ct)).Single(); await b.Sync.ResolveAsync(review, false, ct);
            Check((await b.Coordinator.SynchronizeAsync(b.Transport, ct)).Applied);
            Check((await b.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1300);
            Proof("protected-explicit-money-conflict-no-duplicate-pass");
            stage = "deletion";
            await a.Store.DeleteAttachmentAsync(attachment.Id, ct); await a.Store.DeleteEntryAsync(entry.Id, ct);
            Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied); Check((await b.Coordinator.SynchronizeAsync(b.Transport, ct)).Applied);
            Check((await b.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0 && await b.Store.GetAttachmentAsync(attachment.Id, ct) is null);
            CryptographicOperations.ZeroMemory(bytes);
            Proof("protected-original-tombstones-pass");
        }
    }
    finally { await app.StopAsync(CancellationToken.None); }
    Proof(resume ? "cold-process-complete" : "first-process-complete"); return 0;
}
catch (Exception error)
{
    // Never emit exception messages, request bodies, credentials, financial rows or connection strings.
    Console.Error.WriteLine("FAILED " + stage + " " + error.GetType().Name); return 1;
}
