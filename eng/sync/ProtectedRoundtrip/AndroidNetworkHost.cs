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

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Retained original TLS service and protected Windows counterpart for one independent Android HTTP scenario.</summary>
internal static class AndroidNetworkHost
{
    /// <summary>Waits for explicit native-stage observation before advancing original financial operations.</summary>
    internal static async Task<int> RunAsync(string root, string fixture)
    {
        var run = Path.GetFileName(root); var stage = "configuration";
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(30)); var ct = deadline.Token;
        void Check(bool value) { if (!value) throw new InvalidDataException("The current native counterpart invariant failed."); }
        void Proof(string name) { File.AppendAllText(Path.Combine(root, "checkpoints.txt"), name + Environment.NewLine); Console.WriteLine(name); }
        async Task WaitAsync(string name)
        {
            var file = Path.Combine(root, name);
            while (!File.Exists(file)) await Task.Delay(1000, ct);
            Check(new FileInfo(file).Length < 128 && (File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0);
            Check(await File.ReadAllTextAsync(file, ct) == run);
        }
        try
        {
            Check(run.Length == 32 && run.All(value => char.IsAsciiHexDigit(value)));
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB"));
            Check(connection.Host is "127.0.0.1" or "localhost" && connection.Database?.StartsWith("zanance_identity_sync_", StringComparison.Ordinal) == true);
            connection.Username = "zanance_runtime";
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = connection.ConnectionString, ["Service:Root"] = Path.Combine(fixture, "service") });
            builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
            await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(ct);
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                var fingerprint = await File.ReadAllTextAsync(Path.Combine(fixture, "service", "local-tls.fingerprint"), ct);
                var peer = new LocalSyncPeer(address, fingerprint);
                // Public run-derived synthetic credentials are restricted to this fictional account. Never use this for real identity.
                var email = "android-network-" + run + "@example.invalid"; var password = "Fictitious!" + run + "aA9";
                using (var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System))
                {
                    using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct); Check(registration.StatusCode == HttpStatusCode.Accepted);
                    await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                    var user = await db.Users.SingleAsync(value => value.Email == email, ct);
                    var challenge = await db.Challenges.AsNoTracking().SingleAsync(value => value.UserId == user.Id && value.Purpose == "confirm", ct);
                    var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                    using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct); Check(confirmation.StatusCode == HttpStatusCode.NoContent);
                }
                stage = "protected-windows-publish";
                using var a = await ProtectedClient.OpenAsync(Path.Combine(root, "windows"), peer, true, ct);
                var identity = await a.Transport.SignInAsync(email, password, ct); password = "";
                var device = await a.Transport.RegisterDeviceAsync("Fictitious native counterpart", ct);
                var remote = await a.Transport.CreateProfileAsync(new SyncCreateProfile(device.Id, Guid.NewGuid(), "Fictitious Android HTTP copy", SyncSchema.Hash), ct);
                var account = new Account { Name = "Fictitious cross-platform cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 15), OpeningBalance = 100000 };
                await a.Store.SaveAccountAsync(account, ct);
                var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 15), Amount = 1234, Title = "Fictitious cross-platform expense" };
                Check((await a.Store.SaveEntryAsync(entry, ct)).Succeeded);
                var bytes = RandomNumberGenerator.GetBytes(3 * 1024 * 1024); var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
                var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-network.pdf", ContentType = "application/pdf", Data = bytes };
                await a.Store.AddAttachmentAsync(attachment, ct);
                await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(ct), peer, identity.Id, device.Id, remote, ct);
                Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied); CryptographicOperations.ZeroMemory(bytes);
                await File.WriteAllTextAsync(Path.Combine(root, "host-ready.json"), JsonSerializer.Serialize(new { Run = run, Port = new Uri(address).Port,
                    Fingerprint = fingerprint, AttachmentHash = digest }), ct);
                Proof("protected-counterpart-ready-for-original-android-http");
                stage = "native-upload-observation"; await WaitAsync("advance-native-upload");
                Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied);
                entry = (await a.Store.GetEntriesAsync(cancellationToken: ct)).Single(); Check(entry.Amount == 1400);
                Check(Convert.ToHexStringLower(SHA256.HashData((await a.Store.GetAttachmentAsync(attachment.Id, ct))!.Data)) == digest);
                entry.Amount = 1300; Check((await a.Store.SaveEntryAsync(entry, ct)).Succeeded);
                Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied);
                await File.WriteAllTextAsync(Path.Combine(root, "host-conflict-ready"), run, ct); Proof("native-upload-received-and-competing-original-change-published");
                stage = "native-cold-delete-observation"; await WaitAsync("advance-native-delete");
                Check((await a.Coordinator.SynchronizeAsync(a.Transport, ct)).Applied);
                Check((await a.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0 && await a.Store.GetAttachmentAsync(attachment.Id, ct) is null);
                Check((await a.Store.GetAccountsAsync(cancellationToken: ct)).Single().Id == account.Id);
                Proof("native-cold-explicit-resolution-and-tombstones-received-pass");
            }
            finally { await app.StopAsync(CancellationToken.None); }
            Proof("native-counterpart-complete"); return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAILED " + stage + " " + error.GetType().Name); return 1;
        }
    }
}
