using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>A reviewed local peer, separate from account credentials and financial profile identifiers.</summary>
public sealed class LocalSyncPeer
{
    /// <summary>Canonical loopback or emulator-alias HTTPS endpoint.</summary>
    public Uri Address { get; }
    /// <summary>Explicitly reviewed public certificate SHA-256, not an automatically discovered trust decision.</summary>
    public string Fingerprint { get; }

    /// <summary>Rejects cleartext, remote hosting, embedded credentials, path redirects and malformed pins.</summary>
    public LocalSyncPeer(string address, string fingerprint)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.Port is < 1024 or > 65535 || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            uri.AbsolutePath != "/" || uri.DnsSafeHost is not ("localhost" or "127.0.0.1" or "::1" or "10.0.2.2"))
            throw new ArgumentException("A local HTTPS service address is required.");
        ArgumentNullException.ThrowIfNull(fingerprint);
        var pin = fingerprint.Replace(":", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
        if (!SyncProtocolDigest.IsDigest(pin)) throw new ArgumentException("A reviewed SHA-256 certificate fingerprint is required.");
        Address = uri; Fingerprint = pin;
    }

    /// <summary>Binds cached state to peer and identity without storing a password or trusting a display label.</summary>
    public string BindingKey(string identity) => Convert.ToHexStringLower(SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(Address.AbsoluteUri + "\n" + Fingerprint + "\n" + identity)));

    /// <inheritdoc />
    public override string ToString() => Address.AbsoluteUri;
}

/// <summary>Local TLS transport policy; never changes global/device trust or accepts an arbitrary certificate.</summary>
public static class LocalSyncHttp
{
    /// <summary>Creates one peer-bound client; authentication is supplied only after this policy validates the endpoint.</summary>
    public static HttpClient CreateClient(LocalSyncPeer peer, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(peer);
        var expected = Convert.FromHexString(peer.Fingerprint);
        var clock = time ?? TimeProvider.System;
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxResponseHeadersLength = 16,
        };
        handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
        {
            if (certificate is null || (errors & (SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch)) != 0)
                return false;
            using var leaf = X509CertificateLoader.LoadCertificate(certificate.GetRawCertData());
            var now = clock.GetUtcNow().UtcDateTime;
            if (leaf.NotBefore.ToUniversalTime() > now || leaf.NotAfter.ToUniversalTime() <= now) return false;
            var usages = leaf.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToArray();
            if (usages.Length != 1 || !usages[0].EnhancedKeyUsages.Cast<Oid>().Any(usage => usage.Value == "1.3.6.1.5.5.7.3.1")) return false;
            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(leaf.RawData), expected);
        };
        return new(handler, disposeHandler: true) { BaseAddress = peer.Address, Timeout = TimeSpan.FromMinutes(2) };
    }
}
