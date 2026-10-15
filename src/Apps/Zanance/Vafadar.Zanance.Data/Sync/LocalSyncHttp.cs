using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>A reviewed local or approved hosted peer, separate from credentials and financial profile identifiers.</summary>
public sealed class LocalSyncPeer
{
    /// <summary>Canonical local HTTPS endpoint or the explicitly approved public service origin.</summary>
    public Uri Address { get; }
    /// <summary>Reviewed local certificate SHA-256; empty only for the approved public origin's normal system trust.</summary>
    public string Fingerprint { get; }

    /// <summary>Whether maintained system PKI validates the approved public host, permitting ordinary leaf renewal.</summary>
    public bool UsesSystemTrust { get; }

    /// <summary>Rejects arbitrary hosting, cleartext, embedded credentials, paths and ambiguous public pin overrides.</summary>
    public LocalSyncPeer(string address, string fingerprint)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/")
            throw new ArgumentException("A reviewed HTTPS service origin is required.");
        ArgumentNullException.ThrowIfNull(fingerprint);
        if (uri.DnsSafeHost == "api.zanance.vafadar.pro")
        {
            if (uri.Port != 443 || !string.IsNullOrWhiteSpace(fingerprint))
                throw new ArgumentException("The approved public origin requires normal system trust without a local pin override.");
            Address = uri; Fingerprint = ""; UsesSystemTrust = true; return;
        }
        if (uri.Port is < 1024 or > 65535 || uri.DnsSafeHost is not ("localhost" or "127.0.0.1" or "::1" or "10.0.2.2"))
            throw new ArgumentException("Only a local peer or the approved public origin is permitted.");
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

/// <summary>Explicit local pin or normal approved-origin PKI; never changes global trust or accepts arbitrary certificates.</summary>
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
        // The hosted origin uses the maintained TLS stack's complete system/name validation. Installing a
        // local pin callback there would bypass public PKI and break normal automatic certificate renewal.
        if (!peer.UsesSystemTrust) handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
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
