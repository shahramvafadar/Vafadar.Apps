using System.Net;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original application session publication over its real pinned HTTPS client and an isolated protocol responder.</summary>
public sealed partial class PersonalSyncSessionLifetimeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Account_deletion_recovers_a_pending_receipt_after_an_ambiguous_response_without_resubmitting()
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { AllowPeerMemory = true, OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        try
        {
            model.Address = host.Peer.Address.AbsoluteUri;
            model.Fingerprint = host.Peer.Fingerprint;
            model.Email = "deletion@example.invalid";
            model.Password = "Synthetic-original-password9!";
            await model.SignInCommand.ExecuteAsync(null);
            Assert.True(model.ShowAccountDeletion);
            Assert.False(model.ShowAccountDeletionForm);

            await model.ReadAccountDeletionCommand.ExecuteAsync(null);
            Assert.True(model.ShowAccountDeletionForm);
            model.DeletionPassword = "Synthetic-original-password9!";
            await model.RequestAccountDeletionCommand.ExecuteAsync(null);
            Assert.NotNull(model.Error);
            Assert.Empty(model.DeletionPassword);
            Assert.False(model.ShowAccountDeletionForm);
            Assert.False(model.ShowAccountDeletionReceipt);
            await model.RequestAccountDeletionCommand.ExecuteAsync(null);
            Assert.Equal(1, host.DeletionSubmissions);

            await model.ReadAccountDeletionCommand.ExecuteAsync(null);
            Assert.Null(model.Error);
            Assert.True(model.ShowAccountDeletionReceipt);
            Assert.Equal(host.DeletionReceiptId.ToString("D"), model.AccountDeletionReference);
            Assert.False(model.ShowAccountDeletionWebsite);
            Assert.Equal("https://api.zanance.vafadar.pro/identity/delete-account#check-status",
                LocalSyncPeer.HostedAccountDeletionStatusPage.AbsoluteUri);
            Assert.Equal(1, host.DeletionSubmissions);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { model.Deactivate(); }
        Assert.Null(model.AccountDeletionReference);
    }

    [Fact]
    public async Task Refused_deletion_access_clears_the_submitted_password_before_any_http_request()
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { AllowPeerMemory = true, OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        try
        {
            model.Address = host.Peer.Address.AbsoluteUri;
            model.Fingerprint = host.Peer.Fingerprint;
            model.Email = "deletion-refused@example.invalid";
            model.Password = "Synthetic-sign-in-password9!";
            await model.SignInCommand.ExecuteAsync(null);
            await model.ReadAccountDeletionCommand.ExecuteAsync(null);
            Assert.True(model.ShowAccountDeletionForm);

            var requests = host.Requests;
            model.DeletionPassword = "Synthetic-deletion-password9!";
            environment.FailCapture = true;
            await model.RequestAccountDeletionCommand.ExecuteAsync(null);

            Assert.Empty(model.DeletionPassword);
            Assert.Equal(requests, host.Requests);
            Assert.Equal(0, host.DeletionSubmissions);
            Assert.NotNull(model.Error);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.ShowAccountDeletionForm);
        }
        finally { model.Deactivate(); }
    }

    [Theory]
    [InlineData("analysis")]
    [InlineData("billing")]
    [InlineData("deletion")]
    public async Task An_owner_bound_request_cannot_refresh_a_replaced_expired_session(string operation)
    {
        await using var host = await ProtocolHost.StartAsync();
        const string owner = "fictional-original";
        var replaced = new PersonalSyncSession
        {
            UserId = "fictional-replacement",
            RefreshToken = "synthetic-replacement-refresh",
            ExpiresAt = DateTimeOffset.UnixEpoch,
        };
        var key = host.Peer.BindingKey("session");
        await host.Tokens.WriteAsync(key, operation == "deletion"
            ? new PersonalSyncSession { UserId = owner, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) }
            : replaced, Ct);
        if (operation == "deletion")
            host.Tokens.BeforeRead = () =>
            {
                if (host.Tokens.Reads == 2) host.Tokens.ReplaceForTest(replaced);
            };
        using var transport = new PersonalSyncTransport(host.Peer, host.Tokens);
        var requests = host.Requests;

        Func<Task> invoke = operation switch
        {
            "analysis" => () => transport.AnalysisAvailabilityAsync(owner, Ct),
            "billing" => () => transport.BillingRegisterAsync(owner, new("fictional-public", "fictional-envelope"), Ct),
            _ => () => transport.RequestAccountDeletionAsync(owner, "Synthetic-deletion-password9!", Ct),
        };
        await Assert.ThrowsAsync<InvalidDataException>(invoke);
        Assert.Equal(requests, host.Requests);
        Assert.Equal(1, host.Tokens.Writes);
        Assert.Equal(0, host.Tokens.Removals);
    }

    [Fact]
    public async Task Original_sign_in_publishes_the_received_identity_and_device_and_local_Reload_does_not_reopen_credentials()
    {
        using var f = new FlowFixture(); await using var host = await ProtocolHost.StartAsync();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { AllowPeerMemory = true, OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var vm = Create(f, environment); await vm.ActivateAsync();
        vm.Address = host.Peer.Address.AbsoluteUri; vm.Fingerprint = host.Peer.Fingerprint;
        vm.Email = "fictitious@example.invalid"; vm.Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        await vm.SignInCommand.ExecuteAsync(null);
        Assert.Null(vm.Error); Assert.True(vm.SignedIn); Assert.Equal("", vm.Password);
        Assert.Equal(1, host.Registrations); Assert.Equal(1, host.Tokens.Writes); Assert.Equal(1, environment.RememberedPeers);
        var opens = environment.Opens; var reads = host.Tokens.Reads; var requests = host.Requests;
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.Null(vm.Error); Assert.True(vm.SignedIn); Assert.Equal(opens, environment.Opens);
        Assert.Equal(reads, host.Tokens.Reads); Assert.Equal(requests, host.Requests);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); vm.Deactivate();
    }

    [Theory, InlineData("root"), InlineData("input"), InlineData("display")]
    public async Task Received_original_identity_cannot_publish_or_register_a_device_after_its_owner_or_inputs_retire(string retirement)
    {
        using var f = new FlowFixture(); await using var host = await ProtocolHost.StartAsync();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { AllowPeerMemory = true, OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var vm = Create(f, environment); var root = true; vm.BindRootOwner(() => root); await vm.ActivateAsync();
        vm.Address = host.Peer.Address.AbsoluteUri; vm.Fingerprint = host.Peer.Fingerprint;
        vm.Email = "fictitious@example.invalid"; vm.Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        host.BeforeIdentityResponse = () =>
        {
            if (retirement == "root") root = false;
            else if (retirement == "input") vm.DeviceName = "Fictitious newer draft";
            else f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(row => row.CultureName == "de"));
        };
        await vm.SignInCommand.ExecuteAsync(null);
        Assert.False(vm.SignedIn); Assert.Equal(0, host.Registrations); Assert.Equal("", vm.Password);
        Assert.Null(vm.Error); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); vm.Deactivate();
    }

    private static PersonalSyncViewModel Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment) => new(
        f.Services.GetRequiredService<PersonalSyncStore>(), f.Services.GetRequiredService<PersonalSyncCoordinator>(),
        environment, f.Platform, f.Translator, new DateFormatter(f.Localization), f.Localization);

    /// <summary>In-memory synthetic sessions only; no operating-system, user or provider credential is read or persisted.</summary>
    private sealed class SyntheticSessions : IPersonalSyncTokenStore
    {
        private PersonalSyncSession? session;
        internal void ReplaceForTest(PersonalSyncSession replacement) => session = replacement;
        internal int Reads, Writes, Removals;
        internal Action? BeforeRead;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) { Reads++; BeforeRead?.Invoke(); return Task.FromResult(session); }
        public Task WriteAsync(string peer, PersonalSyncSession value, CancellationToken ct) { Writes++; session = value; return Task.CompletedTask; }
        public Task RemoveAsync(string peer, CancellationToken ct) { Removals++; session = null; return Task.CompletedTask; }
    }

    /// <summary>A finite loopback protocol responder, not the product backend, real sign-in or identity acceptance.</summary>
    private sealed class ProtocolHost : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly RSA key;
        private readonly X509Certificate2 certificate;
        internal readonly SyntheticSessions Tokens = new();
        internal LocalSyncPeer Peer = null!;
        internal int Requests, Registrations;
        internal readonly Guid DeletionReceiptId = Guid.NewGuid();
        internal int DeletionSubmissions;
        internal Action? BeforeIdentityResponse;
        internal Action? BeforeRefreshRefusal;
        internal SyncProfile[] CatalogProfiles = [];
        internal SyncDevice[] CatalogDevices = [];
        internal int ProfileCreations;
        internal Guid? RevocableDevice;
        internal int DeviceRevocations;
        internal bool TailAccessFixture;
        internal int TailRequests;
        internal SyncProfile? InitializationResponse;
        internal Action? BeforeInitializationResponse;
        internal int InitializationClaims;
        internal Guid CurrentDeviceId => deviceId;
        private readonly Guid deviceId = Guid.NewGuid();
        private readonly string access = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        private readonly string refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        private ProtocolHost(WebApplication app, RSA key, X509Certificate2 certificate)
        { this.app = app; this.key = key; this.certificate = certificate; }
        internal static async Task<ProtocolHost> StartAsync()
        {
            var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder(); names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, critical: true));
            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            if (OperatingSystem.IsWindows())
            {
                // Windows Schannel cannot serve this in-memory CNG key. Import only this generated
                // fixture key into a temporary user container; no trust store or owner key is accessed.
                var generated = certificate.Export(X509ContentType.Pkcs12);
                certificate.Dispose();
                try { certificate = X509CertificateLoader.LoadPkcs12(generated, null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable); }
                finally { CryptographicOperations.ZeroMemory(generated); }
            }
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Production" });
            builder.Logging.ClearProviders();
            var tlsErrors = new ConcurrentQueue<string>();
            builder.Logging.SetMinimumLevel(LogLevel.Debug).AddProvider(new TlsFailures(tlsErrors));
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
            var app = builder.Build(); var host = new ProtocolHost(app, key, certificate);
            app.Run(host.RespondAsync); await app.StartAsync(Ct);
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            host.Peer = new(address, certificate.GetCertHashString(HashAlgorithmName.SHA256));
            // Refuse an unusable protocol fixture before submitting any synthetic credential request.
            try
            {
                using var probe = LocalSyncHttp.CreateClient(host.Peer);
                using var response = await probe.GetAsync("fixture-readiness", Ct);
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); return host;
            }
            catch (Exception error)
            {
                await host.DisposeAsync();
                throw new InvalidOperationException("Owned protocol TLS readiness failed: " + string.Join(" | ", tlsErrors), error);
            }
        }
        private async Task RespondAsync(HttpContext context)
        {
            Interlocked.Increment(ref Requests);
            switch (context.Request.Path.Value)
            {
                case "/fixture-readiness": context.Response.StatusCode = 204; break;
                case "/identity/logout" when TailAccessFixture:
                    Interlocked.Increment(ref TailRequests); context.Response.StatusCode = 204; break;
                case var blobPath when TailAccessFixture && context.Request.Method == "GET" && blobPath!.EndsWith("/parts/0", StringComparison.Ordinal):
                    Interlocked.Increment(ref TailRequests);
                    context.Response.ContentType = "application/octet-stream";
                    await context.Response.Body.WriteAsync(new byte[] { 11, 23, 47 }, Ct); break;
                case "/identity/login":
                    await context.Response.WriteAsJsonAsync(new { tokenType = "Bearer", accessToken = access, refreshToken = refresh, expiresIn = 3600 }, Ct); break;
                case "/identity/refresh" when BeforeRefreshRefusal is not null:
                    BeforeRefreshRefusal.Invoke();
                    context.Response.StatusCode = 401; break;
                case "/identity/deletion-request" when context.Request.Method == "POST":
                    Interlocked.Increment(ref DeletionSubmissions);
                    context.Response.StatusCode = 503; break;
                case "/identity/deletion-request":
                    if (Volatile.Read(ref DeletionSubmissions) == 0) context.Response.StatusCode = 204;
                    else await context.Response.WriteAsJsonAsync(new AccountDeletionReceipt(DeletionReceiptId, DateTimeOffset.UtcNow), Ct);
                    break;
                case "/sync/identity":
                    BeforeIdentityResponse?.Invoke();
                    await context.Response.WriteAsJsonAsync(new SyncIdentity("d660-synthetic-local-owner"), Ct); break;
                case "/sync/devices" when context.Request.Method == "POST":
                    Interlocked.Increment(ref Registrations);
                    await context.Response.WriteAsJsonAsync(new SyncDevice(deviceId, "Fictitious original device", DateTimeOffset.UtcNow, false), Ct); break;
                case "/sync/current-device":
                    await context.Response.WriteAsJsonAsync(new SyncDevice(deviceId, "Fictitious original device", DateTimeOffset.UtcNow, false), Ct); break;
                case "/sync/devices": await context.Response.WriteAsJsonAsync(CatalogDevices, Ct); break;
                case "/sync/profiles" when context.Request.Method == "POST":
                    var input = await context.Request.ReadFromJsonAsync<SyncCreateProfile>(Ct)
                        ?? throw new InvalidDataException("A fictitious original creation request is required.");
                    Interlocked.Increment(ref ProfileCreations);
                    var created = new SyncProfile(Guid.NewGuid(), input.Name, SyncSchema.Hash, 0, false);
                    CatalogProfiles = [.. CatalogProfiles, created];
                    await context.Response.WriteAsJsonAsync(created, Ct); break;
                case "/sync/profiles": await context.Response.WriteAsJsonAsync(CatalogProfiles, Ct); break;
                case var revokePath when RevocableDevice is { } selected && context.Request.Method == "DELETE" &&
                    revokePath == "/sync/devices/" + selected.ToString("D"):
                    Interlocked.Increment(ref DeviceRevocations);
                    CatalogDevices = CatalogDevices.Select(row => row.Id == selected ? row with { Revoked = true } : row).ToArray();
                    context.Response.StatusCode = 204; break;
                case var path when InitializationResponse is not null && context.Request.Method == "POST" &&
                    path == "/sync/profiles/" + CatalogProfiles[0].Id.ToString("D") + "/initialization":
                    Interlocked.Increment(ref InitializationClaims);
                    BeforeInitializationResponse?.Invoke();
                    await context.Response.WriteAsJsonAsync(InitializationResponse, Ct); break;
                default: context.Response.StatusCode = 404; break;
            }
        }
        public async ValueTask DisposeAsync()
        {
            await app.StopAsync(CancellationToken.None); await app.DisposeAsync();
            certificate.Dispose(); key.Dispose();
        }
    }

    /// <summary>Captures only server transport exception types/messages, never requests, headers or synthetic credentials.</summary>
    private sealed class TlsFailures(ConcurrentQueue<string> errors) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new FailureLogger(categoryName, errors);
        public void Dispose() { }
        private sealed class FailureLogger(string category, ConcurrentQueue<string> errors) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => category.StartsWith("Microsoft.AspNetCore.Server.Kestrel", StringComparison.Ordinal);
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (exception is not null && IsEnabled(logLevel))
                    errors.Enqueue(exception.GetBaseException().GetType().Name + ": " + exception.GetBaseException().Message);
            }
        }
    }
}
