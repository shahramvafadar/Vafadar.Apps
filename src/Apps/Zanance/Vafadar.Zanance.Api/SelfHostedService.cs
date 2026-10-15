using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api;

/// <summary>Explicit isolated deployment authority, separate from development and fictional Integration settings.</summary>
internal sealed record SelfHostedBoundary(Uri Origin, IPAddress Proxy);

/// <summary>Configures an original TLS worker behind one explicitly identified project-owned reverse proxy.</summary>
internal static class SelfHostedService
{
    /// <summary>Refuses incomplete hosted configuration before a listener can accept identity or financial requests.</summary>
    internal static SelfHostedBoundary? Configure(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsEnvironment("SelfHosted")) return null;
        if (builder.Configuration["Service:Mode"] != "isolated-self-hosted" ||
            !Uri.TryCreate(builder.Configuration["Service:PublicOrigin"], UriKind.Absolute, out var origin) ||
            origin.Scheme != "https" || origin.UserInfo.Length != 0 || origin.AbsolutePath != "/" ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0 ||
            origin.DnsSafeHost is not ("localhost" or "api.zanance.vafadar.pro") ||
            (origin.DnsSafeHost == "localhost" ? origin.Port < 1024 : origin.Port != 443) ||
            !IPAddress.TryParse(builder.Configuration["Service:ProxyAddress"], out var proxy) ||
            proxy.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || proxy.GetAddressBytes()[0] != 172 ||
            proxy.GetAddressBytes()[1] is < 16 or > 31)
            throw new InvalidOperationException("Explicit isolated origin and private reverse-proxy authority are required.");
        var boundary = new SelfHostedBoundary(origin, proxy);
        if (origin.DnsSafeHost == "api.zanance.vafadar.pro")
        {
            var root = builder.Configuration["Service:Root"];
            if (root is null || !Path.IsPathFullyQualified(root) ||
                File.Exists(Path.Combine(root, "fictitious-hosting.marker")) ||
                !File.Exists(Path.Combine(root, "production-hosting.marker")))
                throw new InvalidOperationException("Public activation requires a fresh production authority, never a fictional deployment.");
        }
        builder.Services.AddSingleton(boundary);
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Upstream TLS establishes the scheme. Only the actual proxy can supply one client address.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear(); options.KnownProxies.Clear(); options.KnownProxies.Add(proxy);
        });
        var certificatePath = builder.Configuration["Service:ListenerCertificate"];
        var keyPath = builder.Configuration["Service:ListenerPrivateKey"];
        if (certificatePath is null || keyPath is null || !Path.IsPathFullyQualified(certificatePath) || !Path.IsPathFullyQualified(keyPath))
            throw new InvalidOperationException("Existing private worker TLS identity is required.");
        var certificate = X509Certificate2.CreateFromPemFile(certificatePath, keyPath);
        if (!certificate.HasPrivateKey || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow.AddDays(1))
            throw new InvalidOperationException("Worker TLS identity requires explicit maintenance; replacement refused.");
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8443, listener => listener.UseHttps(certificate)));
        return boundary;
    }

    /// <summary>Checks original transport/host before accepting forwarded client information or application work.</summary>
    internal static void UseBoundary(WebApplication app)
    {
        if (app.Services.GetService<SelfHostedBoundary>() is not { } boundary) return;
        app.Use(async (http, next) =>
        {
            if (!http.Request.Path.StartsWithSegments("/health"))
            {
                var address = http.Connection.RemoteIpAddress;
                if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
                if (!http.Request.IsHttps || !Equals(address, boundary.Proxy) ||
                    !string.Equals(http.Request.Host.Value, boundary.Origin.Authority, StringComparison.OrdinalIgnoreCase))
                { http.Response.StatusCode = StatusCodes.Status421MisdirectedRequest; return; }
            }
            await next(http);
        });
        app.UseForwardedHeaders();
    }

    /// <summary>Only the separately mounted database owner grants finite application DML after explicit migration.</summary>
    internal static async Task GrantRuntimeAsync(IServiceProvider services)
    {
        if (services.GetService<SelfHostedBoundary>() is null) return;
        var factory = services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync("""
            GRANT USAGE ON SCHEMA public TO zanance_runtime;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime;
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime;
            REVOKE INSERT, UPDATE, DELETE ON TABLE "__EFMigrationsHistory" FROM zanance_runtime;
            ALTER DEFAULT PRIVILEGES FOR ROLE zanance_owner IN SCHEMA public
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO zanance_runtime;
            ALTER DEFAULT PRIVILEGES FOR ROLE zanance_owner IN SCHEMA public
                GRANT USAGE, SELECT ON SEQUENCES TO zanance_runtime;
            """);
    }
}
