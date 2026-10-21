using System.Net;
using System.Net.Http.Json;
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
using Vafadar.Zanance.Data.Sync;

// D-436: only a new privately owned Integration database may create this fictional identity.
// Existing household revocations and personal/provider accounts are never changed.
if (args.Length != 2 || !Guid.TryParseExact(args[1], "N", out _)) throw new ArgumentException("Explicit independent root and run required.");
var root = Path.GetFullPath(args[0]); var run = args[1];
if (!root.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
    File.ReadAllText(Path.Combine(root, "fixture.marker")) != "Fictitious independent household money D436") throw new IOException("Owned integration marker required.");
for (var part = new DirectoryInfo(root); part is not null; part = part.Parent)
    if ((part.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected fixture refused.");
using var deadline = new CancellationTokenSource(TimeSpan.FromHours(1)); var ct = deadline.Token;
var stage = "private-database";
try
{
    var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ZANANCE_D436_TEST_DB"));
    if (connection.Host != "127.0.0.1" || connection.Database != "zanance_household_money_d436" || connection.Username != "fixture_admin") throw new IOException("Fresh integration database required.");
    await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection.ConnectionString).Options))
    {
        await schema.Database.MigrateAsync(ct);
        if (schema.Database.HasPendingModelChanges()) throw new IOException("Current schema required.");
    }
    stage = "private-loopback-host";
    var serviceRoot = Path.Combine(root, "service");
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = connection.ConnectionString,
        ["Service:Root"] = serviceRoot, ["Service:Household:LocalQualification"] = "true" });
    builder.AddZananceService(); builder.Logging.ClearProviders();
    builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
    await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(ct);
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(serviceRoot, "local-tls.fingerprint"), ct));
    var completed = Path.Combine(root, "identity-confirmed");
    stage = "fictional-integration-identity";
    if (!File.Exists(completed))
    {
        // Public fixture identity derives synthetic test input; no owner password is read or remembered.
        var email = "household-money-" + run + "@example.invalid"; var password = "Fictitious!" + run + "aA9";
        using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
        using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct);
        if (registration.StatusCode != HttpStatusCode.Accepted) throw new IOException("Fictional registration refused.");
        await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var user = await db.Users.SingleAsync(row => row.Email == email, ct);
        var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
        var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new IOException("Own fictitious mail required.");
        using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
        if (confirmation.StatusCode != HttpStatusCode.NoContent) throw new IOException("Fictional confirmation refused.");
        await File.WriteAllTextAsync(completed, run, ct);
    }
    await File.WriteAllTextAsync(Path.Combine(root, "ready.json"), JsonSerializer.Serialize(new { Run = run, Port = peer.Address.Port, Pin = peer.Fingerprint, LoopbackOnly = true, FictitiousIdentity = true }), ct);
    Console.WriteLine("Independent household money host ready; loopback only.");
    while (!File.Exists(Path.Combine(root, "stop"))) await Task.Delay(1000, ct);
    await app.StopAsync(ct); return 0;
}
catch (Exception error)
{
    // Never emit exception messages, connection strings, mail, credentials or private data.
    await File.WriteAllTextAsync(Path.Combine(root, "failure.json"), JsonSerializer.Serialize(new { Stage = stage, Type = error.GetType().FullName }));
    Console.WriteLine("Independent household money host failed at " + stage + ": " + error.GetType().Name); return 1;
}

/// <summary>Private Integration mail remains in memory and never enters diagnostics or native arguments.</summary>
internal sealed record PrivateMail(string Token);
