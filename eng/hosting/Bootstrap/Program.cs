using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

if (args is ["probe", "https://api:8443/health/ready", "/identities/authority.crt"])
{
    using var rootCertificate = X509CertificateLoader.LoadCertificateFromFile(args[2]);
    using var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false };
    handler.SslOptions.CertificateChainPolicy = new X509ChainPolicy
    { TrustMode = X509ChainTrustMode.CustomRootTrust, RevocationMode = X509RevocationMode.NoCheck };
    handler.SslOptions.CertificateChainPolicy.CustomTrustStore.Add(rootCertificate);
    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
    using var response = await client.GetAsync(args[1]);
    Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    return;
}

// These are software-managed internal machine identities, never owner account credentials.
if (!OperatingSystem.IsLinux() || args is not ["provision" or "prepare-transport-renewal" or "renew-transport", "/secrets"])
    throw new InvalidOperationException("Only the explicit isolated Linux secret-volume provisioner is supported.");
const string root = "/secrets";
var authority = Path.Combine(root, "authority");
var issued = Path.Combine(authority, "issued");
Directory.CreateDirectory(authority);
File.SetUnixFileMode(authority, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
var manifestPath = Path.Combine(authority, "complete.json");
var renewalPath = Path.Combine(authority, "transport-renewal.json");
if (args[0] != "provision")
{
    RenewTransport(args[0] == "prepare-transport-renewal");
    return;
}
if (File.Exists(renewalPath)) throw new IOException("Explicit transport renewal is pending; complete it before starting workers.");
if (!File.Exists(manifestPath))
{
    if (Directory.Exists(issued) && Directory.EnumerateFileSystemEntries(issued).Any())
        throw new IOException("Incomplete first provision preserved; restore its authority before continuing.");
    Directory.CreateDirectory(issued);
    File.SetUnixFileMode(issued, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    using var issuerKey = RSA.Create(3072);
    var request = Request("Zanance isolated internal authority", issuerKey, ca: true);
    using var issuer = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(10));
    Save("authority.crt", issuer.ExportCertificatePem());
    Save("authority.key", issuerKey.ExportPkcs8PrivateKeyPem());
    var certificates = new Dictionary<string, string>();
    foreach (var (name, commonName, server) in new[]
    {
        ("database", "database", true), ("api", "api", true),
        ("runtime", "zanance_runtime", false), ("migrator", "zanance_owner", false),
        ("protection", "Zanance service protection", false),
    })
    {
        using var key = RSA.Create(3072);
        var leaf = Request(commonName, key, ca: false);
        if (server)
        {
            var names = new SubjectAlternativeNameBuilder(); names.AddDnsName(commonName);
            leaf.CertificateExtensions.Add(names.Build());
        }
        leaf.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        { new(server ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2") }, critical: true));
        using var certificate = leaf.Create(issuer, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(365), RandomNumberGenerator.GetBytes(16));
        Save(name + ".crt", certificate.ExportCertificatePem()); Save(name + ".key", key.ExportPkcs8PrivateKeyPem());
        certificates[name] = certificate.GetCertHashString(HashAlgorithmName.SHA256);
    }
    // The immutable manifest is published only after every private machine identity has been flushed.
    Save("complete.json", JsonSerializer.Serialize(certificates));
    File.Move(Path.Combine(issued, "complete.json"), manifestPath);
}
var expected = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath))
    ?? throw new IOException("Provisioning authority is unavailable.");
foreach (var name in new[] { "database", "api", "runtime", "migrator", "protection" })
{
    using var certificate = X509Certificate2.CreateFromPemFile(Path.Combine(issued, name + ".crt"), Path.Combine(issued, name + ".key"));
    if (!expected.TryGetValue(name, out var hash) || hash != certificate.GetCertHashString(HashAlgorithmName.SHA256)
        || name != "protection" && certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow.AddDays(1))
        throw new IOException("Existing machine identity differs or requires explicit certificate maintenance; replacement refused.");
}
foreach (var folder in new[] { "database", "api", "operator", "trust" })
{
    Directory.CreateDirectory(Path.Combine(root, folder));
    File.SetUnixFileMode(Path.Combine(root, folder), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    Publish("authority.crt", folder, "authority.crt");
}
Publish("database.crt", "database", "server.crt"); Publish("database.key", "database", "server.key");
Publish("api.crt", "api", "server.crt"); Publish("api.key", "api", "server.key");
Publish("runtime.crt", "api", "database.crt"); Publish("runtime.key", "api", "database.key");
Publish("migrator.crt", "operator", "database.crt"); Publish("migrator.key", "operator", "database.key");
foreach (var destination in new[] { "api", "operator" })
{
    Publish("protection.crt", destination, "protection.crt"); Publish("protection.key", destination, "protection.key");
}
Console.WriteLine("Internal machine identities verified and published; private material remains in isolated volumes.");

// Renewal retains every private key, the internal CA and the protection certificate used by historical
// key-ring entries. Only transport certificates change. A durable intent makes publication resumable (D-293).
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
void RenewTransport(bool prepareOnly)
{
    var names = new[] { "database", "api", "runtime", "migrator" };
    if (!File.Exists(manifestPath)) throw new IOException("Existing complete authority required for renewal.");
    var current = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath))!;
    var staged = Path.Combine(authority, "transport-renewal");
    if (!File.Exists(renewalPath))
    {
        if (Directory.Exists(staged)) throw new IOException("Incomplete renewal preparation preserved for inspection.");
        Directory.CreateDirectory(staged); File.SetUnixFileMode(staged, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        using var issuer = X509Certificate2.CreateFromPemFile(Path.Combine(issued, "authority.crt"), Path.Combine(issued, "authority.key"));
        if (issuer.NotAfter.ToUniversalTime() < DateTime.UtcNow.AddDays(367)) throw new IOException("Internal authority needs a separately planned trust-root migration.");
        var next = new Dictionary<string, string>(current);
        foreach (var name in names)
        {
            using var old = X509Certificate2.CreateFromPemFile(Path.Combine(issued, name + ".crt"), Path.Combine(issued, name + ".key"));
            if (current[name] != old.GetCertHashString(HashAlgorithmName.SHA256)) throw new IOException("Original certificate authority differs.");
            using var key = old.GetRSAPrivateKey()!;
            var request = new CertificateRequest(old.SubjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            foreach (var extension in old.Extensions) request.CertificateExtensions.Add(extension);
            using var renewed = request.Create(issuer, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(365), RandomNumberGenerator.GetBytes(16));
            WriteDurable(Path.Combine(staged, name + ".crt"), renewed.ExportCertificatePem(), replace: false);
            next[name] = renewed.GetCertHashString(HashAlgorithmName.SHA256);
        }
        WriteDurable(renewalPath, JsonSerializer.Serialize(new TransportRenewal(current, next)), replace: false);
    }
    if (prepareOnly) { Console.WriteLine("Transport renewal intent prepared; workers must remain stopped until completion."); return; }
    var intent = JsonSerializer.Deserialize<TransportRenewal>(File.ReadAllText(renewalPath))!;
    if (intent.Previous.Count != 5 || intent.Next.Count != 5 || intent.Previous["protection"] != intent.Next["protection"])
        throw new IOException("Renewal intent is invalid.");
    foreach (var item in current)
        if (!intent.Previous.TryGetValue(item.Key, out var previous) || item.Value != previous && item.Value != intent.Next[item.Key])
            throw new IOException("Renewal authority differs; original files preserved.");
    foreach (var name in names)
    {
        using var renewed = X509Certificate2.CreateFromPemFile(Path.Combine(staged, name + ".crt"), Path.Combine(issued, name + ".key"));
        if (renewed.GetCertHashString(HashAlgorithmName.SHA256) != intent.Next[name] || renewed.NotAfter.ToUniversalTime() <= DateTime.UtcNow.AddDays(1))
            throw new IOException("Staged transport identity differs or expired.");
        var destinations = name switch
        {
            "database" => new[] { Path.Combine(root, "database", "server.crt") },
            "api" => new[] { Path.Combine(root, "api", "server.crt") },
            "runtime" => new[] { Path.Combine(root, "api", "database.crt") },
            _ => new[] { Path.Combine(root, "operator", "database.crt") },
        };
        foreach (var target in destinations.Append(Path.Combine(issued, name + ".crt")))
        {
            using var actual = X509CertificateLoader.LoadCertificateFromFile(target);
            var fingerprint = actual.GetCertHashString(HashAlgorithmName.SHA256);
            if (fingerprint == intent.Next[name]) continue;
            if (fingerprint != intent.Previous[name]) throw new IOException("Published transport identity differs; replacement refused.");
            WriteDurable(target, renewed.ExportCertificatePem(), replace: true);
        }
    }
    WriteDurable(manifestPath, JsonSerializer.Serialize(intent.Next), replace: true);
    // Retain the completed public receipt and issued certificates as renewal evidence, without key duplication.
    File.Move(renewalPath, Path.Combine(authority, "transport-renewal-" + Guid.NewGuid().ToString("N") + ".json"));
    Directory.Move(staged, staged + "-" + Guid.NewGuid().ToString("N"));
    Console.WriteLine("Transport certificates renewed; existing keys and historical protection identity preserved.");
}

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
static void WriteDurable(string path, string value, bool replace)
{
    var pending = path + ".pending";
    if (File.Exists(pending)) throw new IOException("Interrupted certificate publication preserved; inspect before continuing.");
    using (var output = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    {
        File.SetUnixFileMode(pending, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        output.Write(System.Text.Encoding.ASCII.GetBytes(value)); output.Flush(flushToDisk: true);
    }
    File.Move(pending, path, overwrite: replace);
}

static CertificateRequest Request(string name, RSA key, bool ca)
{
    var request = new CertificateRequest("CN=" + name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(ca, false, 0, critical: true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(ca ? X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign
        : X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
    return request;
}
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
void Save(string name, string value)
{
    var path = Path.Combine(issued, name);
    using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    output.Write(System.Text.Encoding.ASCII.GetBytes(value)); output.Flush(flushToDisk: true);
}
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
void Publish(string source, string folder, string name)
{
    var bytes = File.ReadAllBytes(Path.Combine(issued, source));
    try
    {
        var target = Path.Combine(root, folder, name);
        if (File.Exists(target))
        {
            var actual = File.ReadAllBytes(target);
            try { if (!CryptographicOperations.FixedTimeEquals(bytes, actual)) throw new IOException("Existing provisioned material differs; replacement refused."); }
            finally { CryptographicOperations.ZeroMemory(actual); }
            return;
        }
        using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        File.SetUnixFileMode(target, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        output.Write(bytes); output.Flush(flushToDisk: true);
    }
    finally { CryptographicOperations.ZeroMemory(bytes); }
}

/// <summary>Public fingerprint authority for an explicit resumable transport-only renewal.</summary>
internal sealed record TransportRenewal(Dictionary<string, string> Previous, Dictionary<string, string> Next);
