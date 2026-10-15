using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Loopback-only TLS identity, protected by the existing service key ring without changing system trust.</summary>
internal sealed class LocalSyncCertificate : IDisposable
{
    /// <summary>Actual local TLS identity; Windows uses a runtime-owned current-user import container.</summary>
    internal X509Certificate2 Certificate { get; }
    /// <summary>Public SHA-256 fingerprint for explicit client pin review; never a credential.</summary>
    internal string Fingerprint { get; }

    /// <summary>Reopens the exact retained certificate or creates the first local identity, refusing missing/tampered retained keys.</summary>
    public LocalSyncCertificate(ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time)
    {
        for (var parent = new DirectoryInfo(storage.Root); parent is not null; parent = parent.Parent)
            if ((parent.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected service storage refused.");
        var privatePath = Path.Combine(storage.Root, "local-tls.protected");
        var publicPath = Path.Combine(storage.Root, "local-tls.fingerprint");
        var protector = protection.CreateProtector("local-sync-tls-identity-v1");
        var retained = File.Exists(privatePath);
        if (!retained && File.Exists(publicPath)) throw new InvalidOperationException("Retained local TLS key is missing; restore its original private service files.");
        byte[] pfx;
        if (retained)
        {
            var info = new FileInfo(privatePath);
            if (info.Length > 65536 || (info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Invalid protected local TLS storage.");
            pfx = protector.Unprotect(File.ReadAllBytes(privatePath));
        }
        else
        {
            using var key = RSA.Create(3072);
            var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback); names.AddIpAddress(IPAddress.IPv6Loopback); names.AddIpAddress(IPAddress.Parse("10.0.2.2"));
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, true));
            var now = time.GetUtcNow();
            using var generated = request.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(1));
            pfx = generated.Export(X509ContentType.Pfx);
        }
        try
        {
            // Windows Schannel refuses ephemeral private keys at handshake time, after a successful PFX import.
            // UserKeySet permits its native TLS use without PersistKeySet: the runtime deletes this import container
            // on disposal. The authoritative PFX remains wrapped by the service key ring; no plaintext PFX is written.
            // See runtime v10.0.13 X509CertificateLoader.Windows.ShouldDeleteKeyContainer.
            var flags = OperatingSystem.IsWindows() ? X509KeyStorageFlags.UserKeySet : X509KeyStorageFlags.EphemeralKeySet;
            Certificate = X509CertificateLoader.LoadPkcs12(pfx, null, flags);
            if (!Certificate.HasPrivateKey || Certificate.NotBefore.ToUniversalTime() > time.GetUtcNow().UtcDateTime ||
                Certificate.NotAfter.ToUniversalTime() <= time.GetUtcNow().UtcDateTime)
                throw new InvalidOperationException("Local TLS identity is unavailable or expired; explicit certificate review is required.");
            Fingerprint = Convert.ToHexStringLower(SHA256.HashData(Certificate.RawData));
            if (File.Exists(publicPath))
            {
                if ((File.GetAttributes(publicPath) & FileAttributes.ReparsePoint) != 0 || new FileInfo(publicPath).Length > 128 ||
                    !SyncProtocolDigest.IsDigest(File.ReadAllText(publicPath)) || File.ReadAllText(publicPath) != Fingerprint)
                    throw new InvalidOperationException("Local TLS public authority differs; preserve the original files.");
            }
            if (!retained) WriteNew(privatePath, protector.Protect(pfx));
            if (!File.Exists(publicPath)) WriteNew(publicPath, System.Text.Encoding.ASCII.GetBytes(Fingerprint));
        }
        catch { Certificate?.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }

    private static void WriteNew(string path, byte[] bytes)
    {
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough);
        output.Write(bytes); output.Flush(flushToDisk: true);
    }

    /// <inheritdoc />
    public void Dispose() => Certificate.Dispose();
}
