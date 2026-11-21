using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>In-flight identity requests must observe account removal before committing new identity data.</summary>
public sealed class IdentityOwnerRetirementTests
{
    /// <summary>An owner removed under its transaction cannot acquire a new session or restart an identity challenge.</summary>
    [Theory]
    [InlineData("login", HttpStatusCode.Unauthorized)]
    [InlineData("recover", HttpStatusCode.Accepted)]
    [InlineData("complete", HttpStatusCode.BadRequest)]
    public async Task Identity_waiter_rechecks_removal_under_the_owner_lock(string action, HttpStatusCode expected)
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_IDENTITY_REMOVAL_TEST_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional identity removal database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55488, connection.Port);
        Assert.Equal("zanance_identity_removal_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_IDENTITY_REMOVAL_TEST_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional identity removal fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));

        var observer = new OwnerLockObserver();
        await using var host = new IdentityHost(configured!, Path.Combine(root, action), observer);
        using var http = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        await db.Database.MigrateAsync(ct);
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        var email = $"fictional-owner-removal-{action}@example.invalid";
        const string password = "Fictional-Owner-Removal9!";
        var original = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(original, password)).Succeeded);

        var token = await users.GeneratePasswordResetTokenAsync(original);
        var protector = fixture.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1");
        var challenge = new IdentityChallenge
        {
            Id = Guid.NewGuid(), UserId = original.Id, Purpose = "reset", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            ProtectedMail = protector.Protect(JsonSerializer.Serialize(new PrivateMail(email, token))),
        };
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        await using (var maintenance = new NpgsqlConnection(configured))
        {
            await maintenance.OpenAsync(ct);
            await using var transaction = await maintenance.BeginTransactionAsync(ct);
            await using var ownerLock = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@owner, 0))", maintenance, transaction);
            ownerLock.Parameters.AddWithValue("owner", original.Id);
            await ownerLock.ExecuteNonQueryAsync(ct);

            var pending = action == "complete"
                ? http.PostAsJsonAsync("/identity/complete", new ChallengeInput(challenge.Id, token, password), ct)
                : http.PostAsJsonAsync("/identity/" + action, new IdentityInput(email, password), ct);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            Assert.False(pending.IsCompleted);

            // This is a fictional account without financial or household data. Its session and challenge
            // rows follow the existing identity schema's cascade, while the service request remains waiting.
            await using var remove = new NpgsqlCommand("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = @owner", maintenance, transaction);
            remove.Parameters.AddWithValue("owner", original.Id);
            Assert.Equal(1, await remove.ExecuteNonQueryAsync(ct));
            await transaction.CommitAsync(ct);

            using var response = await pending.WaitAsync(TimeSpan.FromSeconds(10), ct);
            Assert.Equal(expected, response.StatusCode);
        }

        Assert.False(await db.Users.AsNoTracking().AnyAsync(row => row.Id == original.Id, ct));
        Assert.False(await db.Sessions.AsNoTracking().AnyAsync(row => row.UserId == original.Id, ct));
        Assert.False(await db.Challenges.AsNoTracking().AnyAsync(row => row.UserId == original.Id, ct));
    }

    private sealed class OwnerLockObserver : DbCommandInterceptor
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT pg_advisory_xact_lock(hashtextextended(", StringComparison.Ordinal))
                Entered.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class IdentityHost(string connection, string root, OwnerLockObserver observer) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");
            builder.UseSetting("ConnectionStrings:Service", connection);
            builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.ConfigureDbContext<ServiceDbContext>(options => options.AddInterceptors(observer));
            });
        }
    }
}
