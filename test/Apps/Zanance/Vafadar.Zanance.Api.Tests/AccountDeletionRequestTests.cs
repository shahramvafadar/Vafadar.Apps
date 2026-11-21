using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Authenticated account deletion intake retains one original receipt and never applies deletion itself.</summary>
public sealed class AccountDeletionRequestTests
{
    /// <summary>The additive schema preserves existing accounts and accepts only their own reauthenticated request.</summary>
    [Fact]
    public async Task Deletion_request_survives_concurrent_retry_and_keeps_other_accounts_separate()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));

        await using var host = new IdentityHost(configured!, Path.Combine(root, "service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        const string previous = "20261120003818_AiPilotCreateRefusal";
        await db.Database.MigrateAsync(previous, ct);
        const string password = "Fictional-Deletion-Request9!";
        var owner = await SeedAsync("request-owner@example.invalid");
        var other = await SeedAsync("request-other@example.invalid");
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        using var unauthenticated = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var ownerHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var otherHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        const string route = "/identity/deletion-request";
        using var noSession = await unauthenticated.PostAsJsonAsync(route, new AccountDeletionInput(password), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
        await LoginAsync(ownerHttp, owner);
        await LoginAsync(otherHttp, other);
        using var originalStatus = await ownerHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.NoContent, originalStatus.StatusCode);
        using var wrongPassword = await ownerHttp.PostAsJsonAsync(route, new AccountDeletionInput("Fictional-Incorrect-Password9!"), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        var results = await Task.WhenAll(RequestAsync(), RequestAsync());
        Assert.Equal(results[0], results[1]);
        Assert.NotEqual(Guid.Empty, results[0].ReceiptId);
        using var status = await ownerHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.True(status.Headers.CacheControl?.NoStore);
        Assert.Equal(results[0], await status.Content.ReadFromJsonAsync<AccountDeletionReceipt>(ct));
        using var otherStatus = await otherHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.NoContent, otherStatus.StatusCode);
        var recorded = Assert.Single(await db.AccountDeletionRequests.AsNoTracking().ToListAsync(ct));
        Assert.Equal(owner.Id, recorded.UserId);
        Assert.Equal(results[0].ReceiptId, recorded.ReceiptId);
        Assert.Equal(results[0].RequestedAt, recorded.RequestedAt);
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(2, await db.Sessions.CountAsync(row => !row.Revoked, ct));
        Assert.Empty(await db.Challenges.ToListAsync(ct));

        async Task<ServiceUser> SeedAsync(string email)
        {
            var user = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            return user;
        }

        async Task LoginAsync(HttpClient http, ServiceUser user)
        {
            using var response = await http.PostAsJsonAsync("/identity/login", new IdentityInput(user.Email!, password), ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var grant = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.GetProperty("accessToken").GetString());
        }

        async Task<AccountDeletionReceipt> RequestAsync()
        {
            using var response = await ownerHttp.PostAsJsonAsync(route, new AccountDeletionInput(password), ct);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            return Assert.IsType<AccountDeletionReceipt>(await response.Content.ReadFromJsonAsync<AccountDeletionReceipt>(ct));
        }
    }

    private sealed class IdentityHost(string connection, string root) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");
            builder.UseSetting("ConnectionStrings:Service", connection);
            builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        }
    }
}
