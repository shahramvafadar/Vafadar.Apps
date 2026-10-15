using Vafadar.Zanance.Api;
using Microsoft.AspNetCore.DataProtection;

// The operator switch is not a key/value configuration option; do not pass it to the host parser.
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--migrate").ToArray());
builder.AddZananceService();
var app = builder.Build();
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
