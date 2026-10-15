using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Incomplete hosted input cannot create private storage or expose an original identity listener.</summary>
[Trait("AT", "AT-293")]
public sealed class SelfHostedBoundaryTests
{
    /// <summary>A public origin cannot silently promote missing, unclassified or fictional private data.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Public_activation_requires_exclusively_production_authority(bool fictional, bool production)
    {
        var root = Path.Combine(Path.GetTempPath(), "zanance-public-refusal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            if (fictional) File.WriteAllText(Path.Combine(root, "fictitious-hosting.marker"), "fictional");
            if (production) File.WriteAllText(Path.Combine(root, "production-hosting.marker"), "production");
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            { EnvironmentName = "SelfHosted", ApplicationName = typeof(ServiceHost).Assembly.FullName });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Mode"] = "isolated-self-hosted", ["Service:PublicOrigin"] = "https://api.zanance.vafadar.pro/",
                ["Service:ProxyAddress"] = "172.29.83.10", ["Service:Root"] = root,
            });
            var error = Assert.Throws<InvalidOperationException>(() => builder.AddZananceService());
            Assert.Equal("Public activation requires a fresh production authority, never a fictional deployment.", error.Message);
        }
        finally
        {
            foreach (var name in new[] { "fictitious-hosting.marker", "production-hosting.marker" })
                if (File.Exists(Path.Combine(root, name))) File.Delete(Path.Combine(root, name));
            Directory.Delete(root);
        }
    }

    /// <summary>Rejects port drift before attempting to load private listener authority.</summary>
    [Theory]
    [InlineData("https://localhost/")]
    [InlineData("https://api.zanance.vafadar.pro:8443/")]
    public void Unsupported_hosted_ports_are_refused_before_tls_identity(string origin)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "SelfHosted", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Service:Mode"] = "isolated-self-hosted", ["Service:PublicOrigin"] = origin,
            ["Service:ProxyAddress"] = "172.29.83.10",
        });
        var error = Assert.Throws<InvalidOperationException>(() => builder.AddZananceService());
        Assert.Equal("Explicit isolated origin and private reverse-proxy authority are required.", error.Message);
    }

    /// <summary>The deployment mode, exact origin and single private proxy must be explicit before native effects.</summary>
    [Theory]
    [InlineData("", "https://localhost:14543/", "172.29.83.10")]
    [InlineData("isolated-self-hosted", "http://localhost:14543/", "172.29.83.10")]
    [InlineData("isolated-self-hosted", "https://other.invalid/", "172.29.83.10")]
    [InlineData("isolated-self-hosted", "https://api.zanance.vafadar.pro/path", "172.29.83.10")]
    [InlineData("isolated-self-hosted", "https://api.zanance.vafadar.pro/?query=value", "172.29.83.10")]
    [InlineData("isolated-self-hosted", "https://fictional@api.zanance.vafadar.pro/", "172.29.83.10")]
    [InlineData("isolated-self-hosted", "https://localhost:14543/", "0.0.0.0")]
    [InlineData("isolated-self-hosted", "https://localhost:14543/", "127.0.0.1")]
    [InlineData("isolated-self-hosted", "https://localhost:14543/", "::1")]
    [InlineData("isolated-self-hosted", "https://localhost:14543/", "172.32.0.1")]
    public void Invalid_hosted_authority_is_refused_before_storage(string mode, string origin, string proxy)
    {
        var root = Path.Combine(Path.GetTempPath(), "zanance-refused-host-" + Guid.NewGuid().ToString("N"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "SelfHosted", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Service:Mode"] = mode, ["Service:PublicOrigin"] = origin,
            ["Service:ProxyAddress"] = proxy, ["Service:Root"] = root,
        });
        Assert.Throws<InvalidOperationException>(() => builder.AddZananceService());
        Assert.False(Directory.Exists(root));
    }
}
