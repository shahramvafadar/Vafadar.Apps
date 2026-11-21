using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    [Fact]
    public async Task Web_deletion_requires_fresh_proof_and_shares_the_pending_receipt_with_the_app()
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
        connection.Database = "zanance_delete_web_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "web-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        await db.Database.MigrateAsync(ct);
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        const string email = "fictional-web-deletion@example.invalid";
        const string password = "Synthetic-web-deletion9!";
        var owner = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(owner, password)).Succeeded);

        const string page = "/identity/delete-account";
        using var browser = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var firstPage = await browser.GetAsync(page, ct);
        Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
        Assert.True(firstPage.Headers.CacheControl?.NoStore);
        Assert.Contains("frame-ancestors 'none'", firstPage.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        var html = await firstPage.Content.ReadAsStringAsync(ct);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1)).Groups[1].Value;
        Assert.NotEmpty(token);
        using var rejectedCsrf = await browser.PostAsync(page, Form(email, password, null), ct);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedCsrf.StatusCode);
        using var wrong = await browser.PostAsync(page, Form(email, "Incorrect-fictional9!", token), ct);
        using var unknown = await browser.PostAsync(page, Form("unknown@example.invalid", "Incorrect-fictional9!", token), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        using var accepted = await browser.PostAsync(page, Form(email, password, token), ct);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.True(accepted.Headers.CacheControl?.NoStore);
        var pending = await db.AccountDeletionRequests.AsNoTracking().SingleAsync(ct);
        var result = await accepted.Content.ReadAsStringAsync(ct);
        Assert.Contains(pending.ReceiptId.ToString("D"), result, StringComparison.Ordinal);
        Assert.DoesNotContain(email, result, StringComparison.Ordinal);
        Assert.DoesNotContain(password, result, StringComparison.Ordinal);
        Assert.Equal(owner.Id, pending.UserId);
        Assert.Empty(await db.Sessions.ToListAsync(ct));

        using var app = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await app.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var grant = JsonDocument.Parse(await login.Content.ReadAsStringAsync(ct));
        app.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.RootElement.GetProperty("accessToken").GetString());
        var receipt = await app.GetFromJsonAsync<AccountDeletionReceipt>("/identity/deletion-request", ct);
        Assert.Equal(new AccountDeletionReceipt(pending.ReceiptId, pending.RequestedAt), receipt);

        static FormUrlEncodedContent Form(string email, string password, string? token)
        {
            var fields = new Dictionary<string, string> { ["email"] = email, ["password"] = password };
            if (token is not null) fields["__RequestVerificationToken"] = token;
            return new(fields);
        }
    }

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
