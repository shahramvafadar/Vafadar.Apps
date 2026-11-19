using Vafadar.Zanance.Api;
using Microsoft.AspNetCore.DataProtection;

// Offline privacy restoration never constructs the database, listener, key ring or host.
if (args.Length > 0 && args[0] == "--merge-household-deletions")
{
    if (args.Length != 3) throw new IOException("Explicit current and restored private roots required.");
    var merged = await Vafadar.Zanance.Api.Sharing.HouseholdDeletionTransfer.MergeAsync(args[1], args[2]);
    Console.WriteLine("Validated permanent household deletion markers: " + merged.ToString(System.Globalization.CultureInfo.InvariantCulture));
    return;
}

// Operator actions are not key/value configuration options.
var selectAnalysis = args.FirstOrDefault() == "--select-analysis-provider";
if (selectAnalysis && (args.Length < 2 || args[1] is not ("openai" or "gemini" or "off") || args.Contains("--migrate")))
    throw new InvalidOperationException("Choose openai, gemini or off for the isolated analysis pilot.");
var hostArgs = selectAnalysis ? args[2..] : args.Where(arg => arg != "--migrate").ToArray();
var builder = WebApplication.CreateBuilder(hostArgs);
builder.AddZananceService(migrationOnly: args.Contains("--migrate", StringComparer.Ordinal));
var app = builder.Build();
if (selectAnalysis)
{
    var pilot = app.Services.GetService<Vafadar.Zanance.Api.Analysis.AiPilotProvider>()
        ?? throw new InvalidOperationException("Isolated analysis pilot is disabled.");
    var factory = app.Services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<ServiceDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await pilot.SelectAsync(db, args[1], Environment.UserName, CancellationToken.None);
    Console.WriteLine("Analysis pilot selection: " + args[1]);
    return;
}
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await ServiceHost.MigrateAsync(app.Services);
    await SelfHostedService.GrantRuntimeAsync(app.Services);
    return;
}
var root = app.Services.GetRequiredService<ServiceStorage>().Root;
var protector = app.Services.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>()
    .CreateProtector("service-installation-v1");
var installation = Path.Combine(root, "installation.protected");
if (File.Exists(installation))
{
    if (protector.Unprotect(await File.ReadAllTextAsync(installation)) != "Zanance private service v1")
        throw new InvalidOperationException("Private installation authority differs.");
}
else
{
    await using var authority = new FileStream(installation, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    await authority.WriteAsync(System.Text.Encoding.UTF8.GetBytes(protector.Protect("Zanance private service v1")));
    authority.Flush(flushToDisk: true);
}
File.Delete(Path.Combine(root, "shutdown.request"));
await File.WriteAllTextAsync(Path.Combine(root, "server.pid"), Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
app.UseZananceService();
app.MapZananceService();
await app.RunAsync();

/// <summary>Public entry point for tests of the actual server composition.</summary>
public partial class Program
{
}
