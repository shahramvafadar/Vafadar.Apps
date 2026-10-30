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
public sealed class PersonalSyncSessionLifetimeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

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
        internal int Reads, Writes;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) { Reads++; return Task.FromResult(session); }
        public Task WriteAsync(string peer, PersonalSyncSession value, CancellationToken ct) { Writes++; session = value; return Task.CompletedTask; }
        public Task RemoveAsync(string peer, CancellationToken ct) { session = null; return Task.CompletedTask; }
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
        internal Action? BeforeIdentityResponse;
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
                case "/identity/login":
                    await context.Response.WriteAsJsonAsync(new { tokenType = "Bearer", accessToken = access, refreshToken = refresh, expiresIn = 3600 }, Ct); break;
                case "/sync/identity":
                    BeforeIdentityResponse?.Invoke();
                    await context.Response.WriteAsJsonAsync(new SyncIdentity("d660-synthetic-local-owner"), Ct); break;
                case "/sync/devices" when context.Request.Method == "POST":
                    Interlocked.Increment(ref Registrations);
                    await context.Response.WriteAsJsonAsync(new SyncDevice(deviceId, "Fictitious original device", DateTimeOffset.UtcNow, false), Ct); break;
                case "/sync/current-device":
                    await context.Response.WriteAsJsonAsync(new SyncDevice(deviceId, "Fictitious original device", DateTimeOffset.UtcNow, false), Ct); break;
                case "/sync/devices": await context.Response.WriteAsJsonAsync(Array.Empty<SyncDevice>(), Ct); break;
                case "/sync/profiles": await context.Response.WriteAsJsonAsync(Array.Empty<SyncProfile>(), Ct); break;
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
