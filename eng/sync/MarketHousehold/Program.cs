using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Rates;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

[assembly: SupportedOSPlatform("windows")]

// Each invocation advances one retained process boundary. It never resets a previous fixture or repeats completed stages.
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4)); var ct = deadline.Token;
var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_MARKET_QA_ROOT") ?? throw new InvalidOperationException("Owned fixture root required."));
Require(await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct) == "Zanance fictitious market qualification v1");
var root = Path.Combine(parent, "shared-protected"); Directory.CreateDirectory(root);
var marker = Path.Combine(root, "fixture.marker");
if (!File.Exists(marker))
{
    Require(!Directory.EnumerateFileSystemEntries(root).Any());
    await File.WriteAllTextAsync(marker, "Fictitious protected market household v1", ct);
}
Require(await File.ReadAllTextAsync(marker, ct) == "Fictitious protected market household v1");
Require(!File.Exists(Path.Combine(root, "complete.txt")));
var stagePath = Path.Combine(root, "stage.txt");
var stage = File.Exists(stagePath) ? int.Parse(await File.ReadAllTextAsync(stagePath, ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
var admin = await File.ReadAllTextAsync(Path.Combine(parent, "admin.connection"), ct);
var runtime = await File.ReadAllTextAsync(Path.Combine(parent, "runtime.connection"), ct);
var adminSettings = new NpgsqlConnectionStringBuilder(admin);
Require(adminSettings.Database == "zanance_market_d299" && adminSettings.Host is "localhost" or "127.0.0.1");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
    ["Service:Root"] = Path.Combine(parent, "service"), ["Service:Household:LocalQualification"] = "true" });
builder.AddZananceService(); builder.Services.RemoveAll<IHostedService>(); builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 15499));
await using var app = builder.Build(); var loseReply = false; var readableFinancialRequests = 0;
app.Use(async (http, next) =>
{
    if (http.Request.Path.StartsWithSegments("/sync/profiles")) Interlocked.Increment(ref readableFinancialRequests);
    if (loseReply && http.Request.Method == "POST" && http.Request.Path.Value?.EndsWith("/commit", StringComparison.Ordinal) == true)
    {
        loseReply = false; var original = http.Response.Body; using var withheld = new MemoryStream(); http.Response.Body = withheld;
        try { await next(http); Require(http.Response.StatusCode < 300); http.Abort(); } finally { http.Response.Body = original; } return;
    }
    await next(http);
});
app.UseZananceService(); app.MapZananceService(); await app.StartAsync(ct);
try
{
    var peer = new LocalSyncPeer("https://localhost:15499/", await File.ReadAllTextAsync(Path.Combine(parent, "service", "local-tls.fingerprint"), ct));
    const string password = "Fictitious-Market-Household-Only9!";
    using (var scope = app.Services.CreateScope())
    {
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        foreach (var email in new[] { "fictitious-market-household-owner@example.invalid", "fictitious-market-household-viewer@example.invalid" })
            if (await manager.FindByEmailAsync(email) is null)
                Require((await manager.CreateAsync(new() { UserName = email, Email = email, EmailConfirmed = true }, password)).Succeeded);
    }
    var ownerRoot = Path.Combine(root, "owner");
    using var owner = await ProtectedClient.OpenAsync(ownerRoot, peer, !Directory.Exists(ownerRoot), ct);
    owner.Services.MigrateLocalDatabase<ZananceDbContext>();
    var sessions = new ProtectedSessions(ownerRoot, () => Require(File.ReadAllText(marker) == "Fictitious protected market household v1"));
    using var remote = new HouseholdServiceTransport(peer, sessions);
    if (await sessions.ReadAsync(peer.BindingKey("session"), ct) is null) await remote.SignInAsync("fictitious-market-household-owner@example.invalid", password, ct);
    var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>();
    var journal = owner.Services.GetRequiredService<HouseholdJournal>();
    var coordinator = owner.Services.GetRequiredService<HouseholdSyncCoordinator>();
    var identity = (await remote.IdentityAsync(ct)).Id;
    var state = await journal.CaptureAsync(peer, identity, ct) ?? await workspace.BeginOwnerAsync(remote, ct);
    if (state.MembershipVersion == 0) state = await workspace.CompleteOwnerAsync(remote, state.Authority, "Fictitious market household", ct);
    var sources = owner.Services.GetRequiredService<MarketReferenceStore>();
    if (stage == 0)
    {
        await using var db = await owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        if (!await db.MarketReferenceReceipts.AnyAsync(ct))
            await sources.ApplyAsync(await sources.ReviewRateAsync(Quote(), ct), false, () => { }, ct);
        await journal.PrepareOriginalAsync(state.Authority, peer, owner.Sync, ct);
        var retained = await journal.ReadAttemptAsync(state.Authority, ct) ?? throw new InvalidDataException("Original attempt required.");
        Require(retained.Parts.Count > 0);
        await File.WriteAllTextAsync(Path.Combine(root, "original-attempt.digest"), Digest(retained), ct);
        await Advance(1, "sealed-once-before-process-boundary"); return;
    }
    if (stage == 1)
    {
        var retained = await journal.ReadAttemptAsync(state.Authority, ct) ?? throw new InvalidDataException("Original retained attempt missing.");
        Require(Digest(retained) == await File.ReadAllTextAsync(Path.Combine(root, "original-attempt.digest"), ct));
        var lossMarker = Path.Combine(root, "reply-loss.attempted");
        if (!File.Exists(lossMarker))
        {
            await File.WriteAllTextAsync(lossMarker, "Original commit reply loss", ct); loseReply = true; var failed = false;
            try { await coordinator.SynchronizeAsync(remote, state.Authority, ct); } catch (HttpRequestException) { failed = true; }
            Require(failed && !loseReply);
        }
        Require((await coordinator.SynchronizeAsync(remote, state.Authority, ct)).Applied);
        await using var db = await owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        Require(await db.MarketReferenceReceipts.CountAsync(ct) == 1 && await db.ExchangeRates.CountAsync(ct) == 1 && !await db.Entries.AnyAsync(ct));
        await Advance(2, "original-ciphertext-and-commit-reply-loss-resumed");
    }
    var viewerRoot = Path.Combine(root, "viewer");
    using var viewer = await ProtectedClient.OpenAsync(viewerRoot, peer, !Directory.Exists(viewerRoot), ct);
    viewer.Services.MigrateLocalDatabase<ZananceDbContext>();
    var viewerSessions = new ProtectedSessions(viewerRoot, () => Require(File.ReadAllText(marker) == "Fictitious protected market household v1"));
    using var viewerRemote = new HouseholdServiceTransport(peer, viewerSessions);
    if (await viewerSessions.ReadAsync(peer.BindingKey("session"), ct) is null) await viewerRemote.SignInAsync("fictitious-market-household-viewer@example.invalid", password, ct);
    var viewerJournal = viewer.Services.GetRequiredService<HouseholdJournal>();
    var viewerWorkspace = viewer.Services.GetRequiredService<HouseholdWorkspace>();
    var admitted = await viewerJournal.CaptureAsync(peer, (await viewerRemote.IdentityAsync(ct)).Id, ct);
    if (stage == 2)
    {
        if (admitted is null || admitted.MembershipVersion == 0)
        {
            var invitation = await workspace.InviteAsync(remote, state.Authority, HouseholdRole.Viewer, ct);
            admitted = await viewerWorkspace.ClaimAsync(viewerRemote, invitation, state.PinnedRoot.Fingerprint, ct);
            var pending = (await remote.PendingClaimsAsync(state.SpaceId, state.DeviceId, ct)).Single(row => row.DeviceId == admitted.DeviceId);
            await workspace.ApproveAsync(remote, state.Authority, pending, admitted.DeviceFingerprint, ct);
            Require(await viewerWorkspace.CompleteJoinAsync(viewerRemote, admitted.Authority, "Fictitious market household", ct));
        }
        await VerifySources();
        var refused = false;
        try { await viewer.Services.GetRequiredService<MarketReferenceStore>().ReviewRateAsync(Quote(), ct); }
        catch (HouseholdWriteRejectedException) { refused = true; }
        Require(refused && readableFinancialRequests == 0);
        await Advance(3, "actual-protected-viewer-received-original-source-and-refused-write-before-review"); return;
    }
    Require(stage == 3 && admitted is not null);
    await VerifySources(); Require(readableFinancialRequests == 0);
    await File.WriteAllTextAsync(Path.Combine(root, "complete.txt"), "Actual protected two-client TLS source publication, cold ciphertext continuation, original lost reply and Viewer refusal passed. No live provider, phone or release acceptance.", ct);
    Console.WriteLine("protected-market-household-complete");

    async Task VerifySources()
    {
        await using var a = await owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await using var b = await viewer.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        var first = await a.MarketReferenceReceipts.AsNoTracking().SingleAsync(ct);
        var second = await b.MarketReferenceReceipts.AsNoTracking().SingleAsync(ct);
        Require(MarketReferenceDocument.From(first) == MarketReferenceDocument.From(second));
        Require((await b.ExchangeRates.SingleAsync(ct)).Rate == (await a.ExchangeRates.SingleAsync(ct)).Rate && !await b.Entries.AnyAsync(ct));
        Require((await viewer.Services.GetRequiredService<MarketReferenceStore>().CurrentAsync(first.Target, first.TargetId, ct)).Count == 1);
    }
}
finally { await app.StopAsync(CancellationToken.None); }

async Task Advance(int value, string proof)
{
    await File.AppendAllTextAsync(Path.Combine(root, "checkpoints.txt"), proof + Environment.NewLine, ct);
    await File.WriteAllTextAsync(stagePath, value.ToString(System.Globalization.CultureInfo.InvariantCulture), ct); stage = value;
    Console.WriteLine(proof);
}
static void Require(bool value) { if (!value) throw new InvalidDataException("Protected market household invariant failed."); }
static string Digest(HouseholdJournalAttempt attempt) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
    attempt.Open.OperationId.ToString("D") + attempt.Open.AttemptId.ToString("D") + attempt.Commit.SignedCommit +
    string.Join(";", attempt.Parts.Select(part => part.Digest)))));
static MarketQuote Quote()
{
    var now = DateTimeOffset.UtcNow; var day = DateOnly.FromDateTime(now.UtcDateTime);
    var symbol = new MarketSymbol("EURUSD", "Fictitious protected reference", "EUR", "USD", MarketUnit.Currency);
    var source = new MarketCatalog(1, "fictitious-protected-source", "fixture-v1", "Fictitious protected source",
        "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious qualification only",
        "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true, [symbol]);
    return new(1, new(source.Provider, source.Revision, symbol.Id, day), source, symbol,
        new("fictitious-protected-observation", day, now.AddMinutes(-1), 1.23456789m), now, now.AddMinutes(1), now.AddHours(1));
}
