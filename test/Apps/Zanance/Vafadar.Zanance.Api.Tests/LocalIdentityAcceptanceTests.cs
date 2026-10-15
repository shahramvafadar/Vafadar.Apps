using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual PostgreSQL and actual server endpoints, without a mock financial app or public mail sender.</summary>
[Trait("AT", "AT-272")]
public sealed class LocalIdentityAcceptanceTests
{
    private static readonly System.Security.Cryptography.X509Certificates.X509Certificate2 TestCertificate = CreateTestCertificate();

    private static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateTestCertificate()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=Zanance fictional integration", rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        var now = TimeProvider.System.GetUtcNow();
        return request.CreateSelfSigned(now.AddMinutes(-1), now.AddDays(1));
    }
    /// <summary>Fictional identities exercise challenge/session races, ownership, storage, revocation and durable limits.</summary>
    [Fact]
    public async Task Actual_service_preserves_ownership_and_rejects_replayed_or_revoked_grants()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_TEST_DB") ?? "Host=127.0.0.1;Port=55473;Database=zanance_identity_d272;Username=postgres";
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" });
        var options = new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options;
        await using (var schema = new ServiceDbContext(options))
        {
            await schema.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(schema.Database.HasPendingModelChanges());
            await schema.Database.ExecuteSqlRawAsync("TRUNCATE \"AspNetUsers\", \"RequestWindows\" CASCADE", cancellationToken: TestContext.Current.CancellationToken);
        }
        var root = Path.Combine(Path.GetTempPath(), "zanance-identity-" + Guid.NewGuid().ToString("N"));
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        await using (var admin = new ServiceDbContext(options))
        {
            await admin.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime", cancellationToken: TestContext.Current.CancellationToken);
        }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        await using var host = new IdentityHost(runtime, root, clock);
        using var client = host.CreateSecureClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        using (var insecure = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") }))
            Assert.Equal(HttpStatusCode.UpgradeRequired, (await insecure.GetAsync("/objects/", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/objects/", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        var first = await EnrollAsync(client, host, "first@example.invalid");
        var second = await EnrollAsync(client, host, "second@example.invalid");
        var tokens = await LoginAsync(client, "first@example.invalid");
        var originalAccess = tokens.GetProperty("accessToken").GetString();
        var refresh = tokens.GetProperty("refreshToken").GetString();
        var race = await Task.WhenAll(client.PostAsJsonAsync("/identity/refresh", new { refreshToken = refresh }, cancellationToken: TestContext.Current.CancellationToken),
            client.PostAsJsonAsync("/identity/refresh", new { refreshToken = refresh }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Single(race, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(race, x => x.StatusCode == HttpStatusCode.Unauthorized);
        tokens = await race.Single(x => x.StatusCode == HttpStatusCode.OK).Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", originalAccess);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/sessions", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        var content = Encoding.UTF8.GetBytes("Fictional private object; not a user ledger.");
        using var body = new ByteArrayContent(content); body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var upload = await client.PostAsync("/objects/", body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var objectId = (await upload.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
        Assert.Equal(content, await client.GetByteArrayAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.DoesNotContain("Fictional", Encoding.UTF8.GetString(await File.ReadAllBytesAsync(Path.Combine(root, "objects", objectId.ToString("N")), cancellationToken: TestContext.Current.CancellationToken)));
        using var stranger = host.CreateSecureClient();
        var other = await LoginAsync(stranger, "second@example.invalid");
        stranger.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        var sessions = await client.GetFromJsonAsync<JsonElement>("/sessions", cancellationToken: TestContext.Current.CancellationToken);
        var sessionId = sessions.EnumerateArray().Single(s => !s.GetProperty("revoked").GetBoolean()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/sessions/{sessionId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        await using (var limited = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(runtime).Options))
        {
            await Assert.ThrowsAsync<PostgresException>(() => limited.Database.ExecuteSqlRawAsync("CREATE TABLE unauthorized_schema_write (id int)", cancellationToken: TestContext.Current.CancellationToken));
        }
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/identity/recover", new IdentityInput("first@example.invalid", ""), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        var reset = await ReadChallengeAsync(host, "first@example.invalid", "reset");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/identity/complete", new ChallengeInput(reset.Id, reset.Token, "Different-Fictional-Password9!"), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/identity/refresh", new { refreshToken = tokens.GetProperty("refreshToken").GetString() }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/identity/complete", new ChallengeInput(reset.Id, reset.Token, "Different-Fictional-Password9!"), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        var afterReset = await LoginAsync(client, "first@example.invalid", "Different-Fictional-Password9!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", afterReset.GetProperty("accessToken").GetString());
        Assert.Equal(content, await client.GetByteArrayAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/sessions/revoke-all", null, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/sessions", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        clock.Advance(TimeSpan.FromMinutes(2));
        var afterRevoke = await LoginAsync(client, "first@example.invalid", "Different-Fictional-Password9!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", afterRevoke.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/objects/{objectId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        clock.Advance(TimeSpan.FromDays(15));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/sessions", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        for (var i = 0; i < 30; i++)
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/identity/recover", new IdentityInput("missing@example.invalid", ""), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/identity/recover", new IdentityInput("missing@example.invalid", ""), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        // A second host shares only the real DB/root/clock: its counter must not reset on process composition.
        await using var reopened = new IdentityHost(runtime, root, clock);
        using var reopenedClient = reopened.CreateSecureClient();
        Assert.Equal(HttpStatusCode.TooManyRequests, (await reopenedClient.PostAsJsonAsync("/identity/recover", new IdentityInput("missing@example.invalid", ""), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.NotEqual(first, second);

    }

    /// <summary>Real pg_dump/pg_restore plus protected objects/keys, into a distinct empty destination.</summary>
    [Fact]
    public async Task Quiesced_database_and_private_files_restore_with_ownership_and_revocation_intact()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_TEST_DB") ?? "Host=127.0.0.1;Port=55473;Database=zanance_identity_d272;Username=postgres";
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_[a-z0-9_]+$", settings.Database ?? "");
        var container = Environment.GetEnvironmentVariable("ZANANCE_TEST_CONTAINER") ?? "zanance-backend-d272";
        Assert.Matches("^zanance-backend-[a-z0-9-]+$", container);
        var source = Path.Combine(Path.GetTempPath(), "zanance-restore-source-" + Guid.NewGuid().ToString("N"));
        var restored = Path.Combine(Path.GetTempPath(), "zanance-restore-destination-" + Guid.NewGuid().ToString("N"));
        var dump = Path.Combine(Path.GetTempPath(), "zanance-identity-" + Guid.NewGuid().ToString("N") + ".dump");
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        var options = new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options;
        await using (var db = new ServiceDbContext(options))
        {
            await db.Database.MigrateAsync(ct);
            await db.Database.ExecuteSqlRawAsync("TRUNCATE \"AspNetUsers\", \"RequestWindows\" CASCADE", ct);
        }
        JsonElement active, revoked;
        Guid objectId;
        var payload = Encoding.UTF8.GetBytes("Restore fixture bytes: private and independently owned.");
        await using (var host = new IdentityHost(connection, source, clock))
        {
            using var client = host.CreateSecureClient();
            await EnrollAsync(client, host, "restore@example.invalid");
            active = await LoginAsync(client, "restore@example.invalid");
            revoked = await LoginAsync(client, "restore@example.invalid");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", active.GetProperty("accessToken").GetString());
            using var body = new ByteArrayContent(payload); body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await client.PostAsync("/objects/", body, ct);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            objectId = (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", revoked.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/sessions/revoke-all", null, ct)).StatusCode);
            client.DefaultRequestHeaders.Authorization = null;
            active = await LoginAsync(client, "restore@example.invalid");
        }
        // The actual source host is disposed before snapshot: no request/outbox worker can race the copy.
        var watch = System.Diagnostics.Stopwatch.StartNew();
        await using (var file = File.Create(dump))
            await DockerAsync(container, new[] { "pg_dump", "-U", "postgres", "-Fc", settings.Database ?? throw new InvalidOperationException() }, file, null, ct);
        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(restored, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException());
            File.Copy(path, destination);
        }
        var destinationDb = "zanance_identity_restore_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(connection))
        {
            await admin.OpenAsync(ct);
            await using var command = new NpgsqlCommand("CREATE DATABASE " + destinationDb, admin);
            await command.ExecuteNonQueryAsync(ct);
        }
        await using (var file = File.OpenRead(dump))
            await DockerAsync(container, new[] { "pg_restore", "-U", "postgres", "--exit-on-error", "-d", destinationDb }, null, file, ct);
        var restoredConnection = new NpgsqlConnectionStringBuilder(connection) { Database = destinationDb }.ConnectionString;
        await using (var recovered = new IdentityHost(restoredConnection, restored, clock))
        {
            using var client = recovered.CreateSecureClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", active.GetProperty("accessToken").GetString());
            Assert.Equal(payload, await client.GetByteArrayAsync($"/objects/{objectId}", ct));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", revoked.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/objects/{objectId}", ct)).StatusCode);
            await using var scope = recovered.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Assert.Single(await db.Users.ToListAsync(ct));
            Assert.Equal(3, await db.Sessions.CountAsync(ct));
            Assert.Equal(2, await db.Sessions.CountAsync(x => x.Revoked, ct));
            Assert.Single(await db.Objects.ToListAsync(ct));
            Assert.Empty(await db.Database.GetPendingMigrationsAsync(ct));
        }
        watch.Stop();
        Assert.True(watch.Elapsed < TimeSpan.FromHours(4));
        var evidence = Path.Combine(restored, "restore-proof.json");
        await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new { Quiesced = true, RestoreSeconds = watch.Elapsed.TotalSeconds,
            PrivateObjectBytesMatched = true, RevocationPreserved = true, DistinctDatabase = true, RpoSnapshotSeconds = 0 }), ct);
        await using (var admin = new NpgsqlConnection(connection))
        {
            await admin.OpenAsync(ct);
            using var exactPool = new NpgsqlConnection(restoredConnection);
            NpgsqlConnection.ClearPool(exactPool); // Only the disposed restored fixture, never another database.
            await using var command = new NpgsqlCommand("DROP DATABASE " + destinationDb + " WITH (FORCE)", admin);
            await command.ExecuteNonQueryAsync(ct);
        }
        File.Delete(dump);
    }

    private static async Task DockerAsync(string container, string[] command, Stream? output, Stream? input, CancellationToken ct)
    {
        var start = new System.Diagnostics.ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardInput = input is not null,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("exec"); if (input is not null) start.ArgumentList.Add("-i"); start.ArgumentList.Add(container);
        foreach (var arg in command) start.ArgumentList.Add(arg);
        using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("Owned database tool did not start.");
        var error = process.StandardError.ReadToEndAsync(ct);
        if (input is not null) { await input.CopyToAsync(process.StandardInput.BaseStream, ct); process.StandardInput.Close(); }
        if (output is not null) await process.StandardOutput.BaseStream.CopyToAsync(output, ct);
        else await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        // Never include dump data or potentially sensitive diagnostic text in assertion output.
        Assert.True(process.ExitCode == 0, "Owned PostgreSQL backup/restore failed.");
        Assert.DoesNotContain("error", (await error).ToLowerInvariant());
    }

    /// <summary>Actual outbox lease replay, bounded ownership quota and tampered ciphertext rejection.</summary>
    [Fact]
    public async Task Expired_challenges_and_ciphertext_fail_closed_while_concurrent_uploads_obey_shared_capacity()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_TEST_DB") ?? "Host=127.0.0.1;Port=55473;Database=zanance_identity_d272;Username=postgres";
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_[a-z0-9_]+$", settings.Database ?? "");
        var options = new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options;
        await using (var db = new ServiceDbContext(options))
        {
            await db.Database.MigrateAsync(ct);
            await db.Database.ExecuteSqlRawAsync("TRUNCATE \"AspNetUsers\", \"RequestWindows\" CASCADE", ct);
        }
        var root = Path.Combine(Path.GetTempPath(), "zanance-identity-capacity-" + Guid.NewGuid().ToString("N"));
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        await using var host = new IdentityHost(connection, root, clock, mailbox: true);
        using var client = host.CreateSecureClient();
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/identity/register", new IdentityInput("expired@example.invalid", "Fictional-Password9!"), ct)).StatusCode);
        var expired = await ReadChallengeAsync(host, "expired@example.invalid", "confirm");
        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/identity/complete", new ChallengeInput(expired.Id, expired.Token, null), ct)).StatusCode);
        await EnrollAsync(client, host, "capacity@example.invalid");
        var challenge = await ReadChallengeAsync(host, "capacity@example.invalid", "confirm");
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            // The same fictional row is made pending again to exercise a failed delivery's lease recovery.
            await db.Challenges.Where(x => x.Id == challenge.Id).ExecuteUpdateAsync(x => x.SetProperty(c => c.Consumed, false).SetProperty(c => c.Delivered, false)
                .SetProperty(c => c.LeaseOwner, Guid.NewGuid()).SetProperty(c => c.LeaseUntil, clock.GetUtcNow().AddMinutes(-1)), ct);
            var worker = host.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<PrivateMaintenance>().Single();
            await worker.RunOnceAsync(ct);
            var row = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == challenge.Id, ct);
            Assert.True(row.Delivered);
            var delivered = await File.ReadAllTextAsync(Path.Combine(root, "mailbox", challenge.Id.ToString("N")), ct);
            Assert.Equal(row.ProtectedMail, delivered);
            Assert.DoesNotContain("capacity@example.invalid", delivered);
            await worker.RunOnceAsync(ct);
            Assert.Equal(delivered, await File.ReadAllTextAsync(Path.Combine(root, "mailbox", challenge.Id.ToString("N")), ct));
        }
        var token = await LoginAsync(client, "capacity@example.invalid");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
        var bytes = new byte[8 * 1024 * 1024]; bytes[0] = 17; bytes[^1] = 29;
        async Task<HttpResponseMessage> Upload()
        {
            using var body = new ByteArrayContent(bytes); body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return await client.PostAsync("/objects/", body, ct);
        }
        Guid id = Guid.Empty;
        for (var i = 0; i < 7; i++)
        {
            using var response = await Upload();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            id = (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        }
        var race = await Task.WhenAll(Upload(), Upload());
        Assert.Single(race, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(race, x => x.StatusCode == HttpStatusCode.Conflict);
        var path = Path.Combine(root, "objects", id.ToString("N"));
        var original = await File.ReadAllBytesAsync(path, ct);
        var corrupted = original.ToArray(); corrupted[^1] ^= 1;
        await File.WriteAllBytesAsync(path, corrupted, ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync($"/objects/{id}", ct)).StatusCode);
        await File.WriteAllBytesAsync(path, original, ct);
        Assert.Equal(bytes, await client.GetByteArrayAsync($"/objects/{id}", ct));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/objects/{id}", ct)).StatusCode);
        var maintenance = host.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<PrivateMaintenance>().Single();
        await maintenance.RunOnceAsync(ct);
        Assert.False(File.Exists(path));
        await using (var scope = host.Services.CreateAsyncScope())
            Assert.True((await scope.ServiceProvider.GetRequiredService<ServiceDbContext>().Objects.AsNoTracking().SingleAsync(o => o.Id == id, ct)).StorageRetired);
    }

    /// <summary>Actual cold host refuses missing persisted keys without creating replacement authority.</summary>
    [Fact]
    public async Task Lost_service_key_ring_stays_closed_and_original_keys_restore_the_original_grant()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_TEST_DB") ?? "Host=127.0.0.1;Port=55473;Database=zanance_identity_d272;Username=postgres";
        Assert.Matches("^zanance_identity_[a-z0-9_]+$", new NpgsqlConnectionStringBuilder(connection).Database ?? "");
        await using (var db = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        {
            await db.Database.MigrateAsync(ct);
            await db.Database.ExecuteSqlRawAsync("TRUNCATE \"AspNetUsers\", \"RequestWindows\" CASCADE", ct);
        }
        var root = Path.Combine(Path.GetTempPath(), "zanance-identity-key-loss-" + Guid.NewGuid().ToString("N"));
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        JsonElement token;
        await using (var source = new IdentityHost(connection, root, clock))
        {
            using var client = source.CreateSecureClient();
            await EnrollAsync(client, source, "key-loss@example.invalid");
            token = await LoginAsync(client, "key-loss@example.invalid");
        }
        var authority = await File.ReadAllBytesAsync(Path.Combine(root, "installation.protected"), ct);
        var keys = Path.Combine(root, "keys"); var retained = Path.Combine(root, "keys-retained-fixture");
        Directory.Move(keys, retained);
        await using (var unavailable = new IdentityHost(connection, root, clock))
            Assert.Throws<InvalidOperationException>(() => unavailable.CreateSecureClient());
        Assert.False(Directory.Exists(keys));
        Assert.Equal(authority, await File.ReadAllBytesAsync(Path.Combine(root, "installation.protected"), ct));
        Directory.Move(retained, keys);
        await using (var recovered = new IdentityHost(connection, root, clock))
        {
            using var client = recovered.CreateSecureClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/sessions", ct)).StatusCode);
        }
    }

    private static async Task<string> EnrollAsync(HttpClient client, IdentityHost host, string email)
    {
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/identity/register", new IdentityInput(email, "Fictional-Password9!"), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/identity/login", new IdentityInput(email, "Fictional-Password9!"), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        var challenge = await ReadChallengeAsync(host, email, "confirm");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/identity/complete", new ChallengeInput(challenge.Id, "incorrect", null), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/identity/complete", new ChallengeInput(challenge.Id, challenge.Token, null), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/identity/complete", new ChallengeInput(challenge.Id, challenge.Token, null), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        return email;
    }

    private static async Task<JsonElement> LoginAsync(HttpClient client, string email, string password = "Fictional-Password9!")
    {
        var response = await client.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task<(Guid Id, string Token)> ReadChallengeAsync(IdentityHost host, string email, string purpose)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email, cancellationToken: TestContext.Current.CancellationToken);
        var row = await db.Challenges.AsNoTracking().Where(c => c.UserId == user.Id && c.Purpose == purpose).OrderByDescending(c => c.ExpiresAt).FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(row.ProtectedMail));
        return (row.Id, mail?.Token ?? throw new InvalidOperationException("Fictional challenge unavailable."));
    }

    private sealed class IdentityHost(string connection, string root, TimeProvider clock, bool mailbox = false) : WebApplicationFactory<Program>
    {
        /// <summary>Uses an HTTPS request scheme to exercise the service's original transport guard.</summary>
        internal HttpClient CreateSecureClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            // TestServer has no socket or TLS handshake; actual pinned TLS is qualified separately.
            BaseAddress = new Uri("https://localhost"),
        });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");
            builder.UseSetting("ConnectionStrings:Service", connection);
            builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>(); services.AddSingleton(clock);
                services.RemoveAll<ServiceStorage>(); services.AddSingleton(new ServiceStorage { Root = root, LocalMailbox = mailbox });
                services.Configure<Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenOptions>(Microsoft.AspNetCore.Identity.IdentityConstants.BearerScheme, o => o.TimeProvider = clock);
                if (!OperatingSystem.IsWindows())
                {
                    // Linux CI uses a fictional in-process certificate to protect the persisted key ring.
                    // This is not a hosted certificate, hardware acceptance or cross-process key-provisioning claim.
                    services.RemoveAll<IDataProtectionProvider>();
                    services.AddDataProtection().SetApplicationName("Vafadar.Zanance.Service")
                        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "keys")))
                        .ProtectKeysWithCertificate(TestCertificate);
                }
            });
        }
    }
}
