using System.Security.Claims;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Vafadar.Zanance.Api;

/// <summary>Explicit private roots, without hosting credentials or public object configuration.</summary>
public sealed class ServiceStorage
{
    /// <summary>Private service-owned directory, separate from financial app data.</summary>
    public required string Root { get; init; }
    /// <summary>Development-only fictional mail delivery; never an external sender.</summary>
    public required bool LocalMailbox { get; init; }
    /// <summary>Finite technical bounds shared by all workers for personal synchronization.</summary>
    public Sync.SyncStorageLimits SyncLimits { get; init; } = new();
    /// <summary>Explicit fictional/local household acceptance only; this cannot manufacture a public paid hosting entitlement.</summary>
    public bool LocalHouseholdQualification { get; init; }
    /// <summary>Explicit independently owned billing qualification; it never enables real sale or commercial quotas.</summary>
    public bool LocalBillingQualification { get; init; }
    /// <summary>Private immutable object path; identifiers originate on the server only.</summary>
    public string ObjectPath(Guid id) => Path.Combine(Root, "objects", id.ToString("N"));
}

/// <summary>Composition and fail-closed service boundary.</summary>
public static class ServiceHost
{
    /// <summary>Session claim is distinct from identity and local profile identifiers.</summary>
    public const string SessionClaim = "zanance:session";
    /// <summary>Refresh generation prevents replay across workers.</summary>
    public const string GenerationClaim = "zanance:generation";

    /// <summary>Registers the maintained authentication and independent service database.</summary>
    public static void AddZananceService(this WebApplicationBuilder builder)
    {
        var selfHosted = SelfHostedService.Configure(builder);
        if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Integration") && selfHosted is null)
            throw new InvalidOperationException("Hosted activation is not approved for this local delivery.");
        if (builder.Environment.IsDevelopment())
        {
            var port = builder.Configuration.GetValue<int?>("Service:Port") ?? 5047;
            var tlsPort = builder.Configuration.GetValue<int?>("Service:TlsPort") ?? port + 1;
            if (port is < 1024 or > 65535 || tlsPort is < 1024 or > 65535 || port == tlsPort)
                throw new InvalidOperationException("Invalid local service ports.");
            builder.WebHost.ConfigureKestrel(options =>
            {
                // The old loopback port remains content-free readiness only. Identity and finance use TLS.
                options.Listen(System.Net.IPAddress.Loopback, port);
                ListenLocalTls(options, tlsPort);
            });
        }
        var connection = builder.Configuration.GetConnectionString("Service") ?? throw new InvalidOperationException("Service database configuration required.");
        var root = builder.Configuration["Service:Root"] ?? throw new InvalidOperationException("Private service root required.");
        if (!Path.IsPathFullyQualified(root)) throw new InvalidOperationException("Private root must be absolute.");
        if (File.Exists(Path.Combine(root, "installation.protected")) &&
            (!Directory.Exists(Path.Combine(root, "keys")) || !Directory.EnumerateFiles(Path.Combine(root, "keys"), "key-*.xml").Any()))
            throw new InvalidOperationException("Existing service protection keys are unavailable; preserve private data and restore the original key ring.");
        var household = builder.Configuration.GetValue<bool>("Service:Household:LocalQualification");
        if (household && !(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Integration") ||
            selfHosted?.Origin.DnsSafeHost == "localhost" && File.Exists(Path.Combine(root, "fictitious-hosting.marker")) &&
            !File.Exists(Path.Combine(root, "production-hosting.marker"))))
            throw new InvalidOperationException("Household local qualification cannot activate public hosting.");
        var billing = builder.Configuration.GetValue<bool>("Billing:LocalQualification");
        if (billing && !(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Integration") ||
            selfHosted?.Origin.DnsSafeHost == "localhost" && File.Exists(Path.Combine(root, "fictitious-hosting.marker")) &&
            !File.Exists(Path.Combine(root, "production-hosting.marker"))))
            throw new InvalidOperationException("Billing local qualification cannot activate public purchases.");
        var storage = new ServiceStorage { Root = root, LocalMailbox = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Service:LocalMailbox"),
            LocalHouseholdQualification = household,
            LocalBillingQualification = billing,
            SyncLimits = Sync.SyncStorageLimits.Read(builder.Configuration) };
        var directory = Directory.CreateDirectory(root);
        if (OperatingSystem.IsWindows())
        {
            var owner = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("Private owner required.");
            var acl = new DirectorySecurity();
            // Project roots can retain an administrator-group owner. Restrict the DACL without requiring
            // an elevated owner change; privileged administrators remain outside this protection boundary.
            acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            acl.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            directory.SetAccessControl(acl);
        }
        else File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Directory.CreateDirectory(Path.Combine(root, "objects"));
        Directory.CreateDirectory(Path.Combine(root, "mailbox"));
        builder.Services.AddSingleton(storage);
        Billing.BillingModule.Add(builder.Services);
        builder.Services.AddSingleton<Sharing.IHouseholdDeletionLedger, Sharing.HouseholdDeletionLedger>();
        builder.Services.AddSingleton<Sharing.HouseholdRetirement>();
        builder.Services.AddSingleton<Sync.LocalSyncCertificate>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContextFactory<ServiceDbContext>(o => o.UseNpgsql(connection));
        // Identity owns its infrastructure scope; application operations create short-lived contexts via the factory.
        builder.Services.AddIdentityCore<ServiceUser>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.SignIn.RequireConfirmedEmail = true;
            o.Password.RequiredLength = 12;
            o.Lockout.MaxFailedAccessAttempts = 5;
            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<ServiceDbContext>().AddSignInManager().AddDefaultTokenProviders();
        builder.Services.Configure<PasswordHasherOptions>(o => o.IterationCount = 600_000);
        builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromMinutes(15));
        builder.Services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme, o =>
        {
            o.BearerTokenExpiration = TimeSpan.FromMinutes(15);
            o.RefreshTokenExpiration = TimeSpan.FromDays(14);
        });
        builder.Services.AddAuthorization();
        var protection = builder.Services.AddDataProtection().SetApplicationName("Vafadar.Zanance.Service")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "keys")));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        else if (builder.Configuration["Service:ProtectionCertificate"] is string certificate && builder.Configuration["Service:ProtectionPrivateKey"] is string key)
            protection.ProtectKeysWithCertificate(X509Certificate2.CreateFromPemFile(certificate, key));
        else if (!builder.Environment.IsEnvironment("Integration"))
            throw new InvalidOperationException("Protected persistent keys required on this host.");
        else protection.UseEphemeralDataProtectionProvider();
        builder.Services.AddHostedService<PrivateMaintenance>();
        builder.WebHost.ConfigureKestrel(o => { o.Limits.MaxRequestBodySize = 8 * 1024 * 1024; o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15); });
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
    }

    /// <summary>Uses the original protected local TLS identity, also for isolated real-socket integration checks.</summary>
    public static void ListenLocalTls(KestrelServerOptions options, int port)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (port is < 1024 or > 65535 && !(port == 0 && options.ApplicationServices.GetRequiredService<IHostEnvironment>().IsEnvironment("Integration")))
            throw new ArgumentOutOfRangeException(nameof(port));
        options.Listen(System.Net.IPAddress.Loopback, port, listener =>
            listener.UseHttps(options.ApplicationServices.GetRequiredService<Sync.LocalSyncCertificate>().Certificate));
    }

    /// <summary>Schema changes are an explicit operator action, not ordinary worker startup.</summary>
    public static async Task MigrateAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    /// <summary>Applies durable rate limits and validates session/stamp on every request.</summary>
    public static void UseZananceService(this WebApplication app)
    {
        SelfHostedService.UseBoundary(app);
        app.Use(async (http, next) =>
        {
            if (!http.Request.IsHttps && !http.Request.Path.StartsWithSegments("/health"))
            { http.Response.StatusCode = 426; return; }
            await next(http);
        });
        app.Use(async (http, next) =>
        {
            if (!http.Request.Path.StartsWithSegments("/health"))
            {
                try
                {
                    var time = http.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
                    var minute = DateTimeOffset.FromUnixTimeSeconds(time.ToUnixTimeSeconds() / 60 * 60);
                    var lane = http.Request.Path.StartsWithSegments("/identity") ? "identity" :
                        http.Request.Path.StartsWithSegments("/billing") ? "billing" : "service";
                    var partition = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(lane + ":" + http.Connection.RemoteIpAddress)));
                    var factory = http.RequestServices.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
                    await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
                    var counts = await db.Database.SqlQuery<int>($"""
                        INSERT INTO "RequestWindows" ("Partition", "StartsAt", "Count") VALUES ({partition}, {minute}, 1)
                        ON CONFLICT ("Partition", "StartsAt") DO UPDATE SET "Count" = LEAST("RequestWindows"."Count" + 1, 10001)
                        RETURNING "Count" AS "Value"
                        """).ToListAsync(http.RequestAborted);
                    if (counts[0] > (lane is "identity" or "billing" ? 30 : 200))
                    {
                        http.Response.StatusCode = 429; http.Response.Headers.RetryAfter = "60"; return;
                    }
                }
                catch (Exception e) when (e is Npgsql.NpgsqlException or InvalidOperationException)
                { http.Response.StatusCode = 503; return; }
            }
            await next(http);
        });
        app.UseAuthentication();
        app.Use(async (http, next) =>
        {
            if (http.User.Identity?.IsAuthenticated == true)
            {
                await using var scope = http.RequestServices.CreateAsyncScope();
                var manager = scope.ServiceProvider.GetRequiredService<SignInManager<ServiceUser>>();
                var user = await manager.ValidateSecurityStampAsync(http.User);
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
                await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
                var valid = Guid.TryParse(http.User.FindFirstValue(SessionClaim), out var session) &&
                    int.TryParse(http.User.FindFirstValue(GenerationClaim), out var generation) && user is not null &&
                    await db.Sessions.AnyAsync(s => s.Id == session && s.UserId == user.Id && s.Generation == generation && !s.Revoked &&
                        s.ExpiresAt > http.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow(), http.RequestAborted);
                if (!valid) { http.Response.StatusCode = 401; return; }
            }
            await next(http);
        });
        app.UseAuthorization();
    }

    /// <summary>Maps identity, private storage and content-free health endpoints.</summary>
    public static void MapZananceService(this WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Ok());
        app.MapGet("/health/ready", async (IDbContextFactory<ServiceDbContext> factory, CancellationToken ct) =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            try { return await db.Database.CanConnectAsync(ct) && ! (await db.Database.GetPendingMigrationsAsync(ct)).Any() ? Results.Ok() : Results.StatusCode(503); }
            catch (Npgsql.NpgsqlException) { return Results.StatusCode(503); }
        });
        IdentityRoutes.Map(app);
        ObjectRoutes.Map(app);
        Sync.SyncServiceRoutes.Map(app);
        Sharing.HouseholdRoutes.Map(app);
        Billing.BillingModule.Map(app);
    }
}
