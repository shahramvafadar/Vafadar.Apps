using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Original anonymous recovery entry captures the actual sender in its protected one-use outbox.</summary>
public sealed class IdentityMailRouteTests
{
    /// <summary>One genuine original service route queues the selected sender without mail, login, sessions or financial effects.</summary>
    [Fact]
    public async Task Recovery_route_captures_the_original_sender_in_the_protected_envelope()
    {
        var input = Environment.GetEnvironmentVariable("ZANANCE_MAIL_TEST_DB");
        Assert.SkipWhen(input is null, "Requires a dedicated fictional identity-mail database.");
        var connection = new NpgsqlConnectionStringBuilder(input!);
        Assert.Equal("zanance_identity_mail_d1226", connection.Database); Assert.Equal("127.0.0.1", connection.Host);
        connection.Database = "zanance_identity_mail_http_d1226";
        var ct = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "zanance-mail-route-" + Guid.NewGuid().ToString("N"));
        await using (var db = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection.ConnectionString).Options))
        {
            await db.Database.MigrateAsync(ct); Assert.Equal(0, await db.Users.CountAsync(ct));
            db.Users.Add(new ServiceUser { Id = Guid.NewGuid().ToString("N"), UserName = "recipient@example.invalid",
                NormalizedUserName = "RECIPIENT@EXAMPLE.INVALID", Email = "recipient@example.invalid",
                NormalizedEmail = "RECIPIENT@EXAMPLE.INVALID", EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString("N") });
            await db.SaveChangesAsync(ct);
        }
        try
        {
            await using var host = new Host(connection.ConnectionString, root);
            using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var response = await client.PostAsync("/identity/recover",
                new StringContent("{\"email\":\"recipient@example.invalid\",\"password\":\"\"}", System.Text.Encoding.UTF8, "application/json"), ct);
            Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var row = Assert.Single(await db.Challenges.AsNoTracking().ToArrayAsync(ct));
            var mail = IdentityMailEnvelope.Read(row, scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(), "brevo-v1:sender@example.invalid");
            Assert.Equal(row.Id, mail.Id); Assert.Equal("reset", mail.Purpose); Assert.Equal("recipient@example.invalid", mail.Recipient);
            Assert.False(row.Delivered); Assert.False(row.Consumed); Assert.Empty(await db.Sessions.ToArrayAsync(ct));
            Assert.Empty(await db.Objects.ToArrayAsync(ct));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private sealed class Host(string connection, string root) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration"); builder.UseSetting("ConnectionStrings:Service", connection); builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services =>
            {
                // No background delivery or real HTTP in this original endpoint qualification.
                foreach (var descriptor in services.Where(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(PrivateMaintenance)).ToArray())
                    services.Remove(descriptor);
                services.AddSingleton<IIdentityMailSender, RefusingSender>();
                if (!OperatingSystem.IsWindows()) services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        }
    }
    private sealed class RefusingSender : IIdentityMailSender
    {
        public string Authority => "brevo-v1:sender@example.invalid";
        public Task SubmitAsync(IdentityMail mail, CancellationToken ct) => throw new InvalidOperationException("Endpoint never submits mail directly.");
    }
}
