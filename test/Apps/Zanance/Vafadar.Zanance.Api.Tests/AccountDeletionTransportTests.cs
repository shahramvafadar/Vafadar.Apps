using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Client deletion receipt transport uses the original pinned peer and private identity.</summary>
public sealed class AccountDeletionTransportTests
{
    /// <summary>The maintained client reads and requests a receipt, then refuses a replaced account before any network effect.</summary>
    [Fact]
    public async Task Deletion_receipt_round_trips_over_pinned_https_and_refuses_a_replaced_identity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rsa = RSA.Create(2048);
        var certificateRequest = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddIpAddress(IPAddress.Loopback);
        certificateRequest.CertificateExtensions.Add(names.Build());
        certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        using var generated = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(15));
        var flags = OperatingSystem.IsWindows() ? X509KeyStorageFlags.UserKeySet : X509KeyStorageFlags.EphemeralKeySet;
        var pfx = generated.Export(X509ContentType.Pfx);
        using var certificate = X509CertificateLoader.LoadPkcs12(pfx, null, flags);
        CryptographicOperations.ZeroMemory(pfx);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.UseHttps(certificate)));
        await using var server = builder.Build();

        const string owner = "fictitious-owner";
        const string password = "Fictional-current-password9!";
        const string access = "fictional-access";
        const string route = "/identity/deletion-request";
        var receipt = new Vafadar.Zanance.Api.AccountDeletionReceipt(
            Guid.NewGuid(), new DateTimeOffset(2026, 10, 15, 13, 0, 0, TimeSpan.Zero));
        int reads = 0, submissions = 0, renewals = 0;
        server.MapGet(route, (HttpContext context) =>
        {
            Assert.Equal("Bearer " + access, context.Request.Headers.Authorization.ToString());
            Interlocked.Increment(ref reads);
            return Volatile.Read(ref submissions) == 0 ? Results.NoContent() : Results.Ok(receipt);
        });
        server.MapPost(route, (HttpContext context, AccountDeletionInput proof) =>
        {
            Assert.Equal("Bearer " + access, context.Request.Headers.Authorization.ToString());
            Assert.Equal(password, proof.Password);
            Interlocked.Increment(ref submissions);
            return Results.Accepted(value: receipt);
        });
        server.MapPost("/identity/refresh", () =>
        {
            Interlocked.Increment(ref renewals);
            return Results.Unauthorized();
        });
        await server.StartAsync(ct);
        var session = new FixtureSession(new() { AccessToken = access, UserId = owner, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) });
        using var transport = new PersonalSyncTransport(new LocalSyncPeer(server.Urls.Single(),
            Convert.ToHexStringLower(SHA256.HashData(certificate.RawData))), session);

        Assert.Null(await transport.ReadAccountDeletionAsync(owner, ct));
        var result = await transport.RequestAccountDeletionAsync(owner, password, ct);
        Assert.Equal(receipt.ReceiptId, result.ReceiptId);
        Assert.Equal(receipt.RequestedAt, result.RequestedAt);
        Assert.Equal(result, await transport.ReadAccountDeletionAsync(owner, ct));

        session.Current = new() { AccessToken = "fictional-replaced", UserId = "fictitious-other", ExpiresAt = DateTimeOffset.UnixEpoch };
        await Assert.ThrowsAsync<InvalidDataException>(() => transport.RequestAccountDeletionAsync(owner, password, ct));
        await Assert.ThrowsAsync<InvalidDataException>(() => transport.ReadAccountDeletionAsync(owner, ct));
        Assert.Equal(2, reads);
        Assert.Equal(1, submissions);
        Assert.Equal(0, renewals);
        Assert.Equal("fictitious-other", session.Current.UserId);
    }

    private sealed class FixtureSession(PersonalSyncSession original) : IPersonalSyncTokenStore
    {
        internal PersonalSyncSession Current { get; set; } = original;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult<PersonalSyncSession?>(Current);
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) => throw new InvalidOperationException("No write expected.");
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException("No removal expected.");
    }
}
