using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual complete billing registration graph, without operator credentials, network or database effects.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingCompositionTests
{
    /// <summary>Both official provider adapters and the actual command/worker graph resolve lazily under production registrations.</summary>
    [Fact]
    public void Actual_registration_graph_resolves_without_internal_constructor_or_private_effects()
    {
        var root = Path.Combine(Path.GetTempPath(), "zanance-billing-unopened-" + Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(new ServiceStorage { Root = root, LocalMailbox = false });
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton<IDbContextFactory<ServiceDbContext>>(new NoDatabase());
        BillingModule.Add(services);
        using var actual = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        Assert.Equal(new[] { BillingIssuer.GooglePlay, BillingIssuer.AppStore }, actual.GetServices<IBillingProvider>().Select(provider => provider.Issuer));
        Assert.NotNull(actual.GetRequiredService<BillingRegistry>()); Assert.NotNull(actual.GetRequiredService<BillingCommandStore>());
        Assert.NotNull(actual.GetRequiredService<BillingReconciliation>()); Assert.NotNull(actual.GetRequiredService<BillingNotifications>());
        Assert.False(Directory.Exists(root));
    }

    private sealed class NoDatabase : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => throw new InvalidOperationException("Construction must not open a database.");
    }
}
