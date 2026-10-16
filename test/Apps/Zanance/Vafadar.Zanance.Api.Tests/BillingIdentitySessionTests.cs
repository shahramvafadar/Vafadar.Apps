using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual identity HTTP/session revocation against the retained isolated billing database, without resetting prior evidence.</summary>
[Trait("AT", "AT-295")]
[Trait("Qualification", "BillingLocal")]
public sealed class BillingIdentitySessionTests
{
    /// <summary>Current logout retires access and refresh together while the same identity's other device stays authorized.</summary>
    [Fact]
    public async Task Current_logout_refuses_old_access_and_refresh_without_retiring_another_device()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "identity-current-session"); Directory.CreateDirectory(root);
        Assert.False(File.Exists(Path.Combine(root, "complete.json")), "Completed HTTP session qualification must not be replayed.");
        await using var host = new IdentityHost(connection, root);
        using var first = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var second = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        const string email = "billing-current-session-d295@example.invalid";
        const string password = "Fictional-Billing-Session-Password9!";
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
            var existing = await users.FindByEmailAsync(email);
            if (existing is null)
                Assert.True((await users.CreateAsync(new ServiceUser { UserName = email, Email = email, EmailConfirmed = true }, password)).Succeeded);
        }
        var original = await Login(first); await Login(second);
        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/sessions", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.PostAsync("/identity/logout", null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/sessions", ct)).StatusCode);
        first.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.PostAsJsonAsync("/identity/refresh",
            new { refreshToken = original.GetProperty("refreshToken").GetString() }, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/sessions", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await second.PostAsync("/identity/logout", null, ct)).StatusCode);
        await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), "{\"CurrentAccessAndRefreshRetired\":true,\"OtherDevicePreserved\":true,\"RealTlsHandshake\":false}", ct);

        async Task<JsonElement> Login(HttpClient client)
        {
            using var response = await client.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var tokens = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
            return tokens;
        }
    }

    private sealed class IdentityHost(string connection, string root) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration"); builder.UseSetting("ConnectionStrings:Service", connection); builder.UseSetting("Service:Root", root);
            // No retention, mail or billing worker may mutate previous isolated qualification fixtures during a session-only check.
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        }
    }
}
