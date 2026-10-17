using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

// This caller exercises the original proxy configuration; no identity or financial algorithm is duplicated.
if (args is ["server"])
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8443, listener =>
        listener.UseHttps(X509Certificate2.CreateFromPemFile("/identities/server.crt", "/identities/server.key"))));
    var app = builder.Build();
    app.MapFallback((HttpContext http) => Results.Json(new
    {
        ForwardedFor = http.Request.Headers["X-Forwarded-For"].ToString(),
        Host = http.Request.Host.Value,
        Https = http.Request.IsHttps,
        ResidualCloudflare = http.Request.Headers["CF-Connecting-IP"].ToString(),
        ResidualHost = http.Request.Headers["X-Forwarded-Host"].ToString()
    }));
    await app.RunAsync();
    return;
}
if (args is not ["client", var address, var mode] || mode is not ("trusted" or "foreign" or "remaining") ||
    !IPAddress.TryParse(address, out var proxy) || proxy.GetAddressBytes()[0] != 172)
    throw new InvalidOperationException("Explicit isolated proxy and caller mode required.");
using var root = X509CertificateLoader.LoadCertificateFromFile("/trust/origin-ca.crt");
using var handler = Handler(true);
using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
var cases = 0;
if (mode != "remaining") await Check("203.0.113.77", "https", mode == "trusted" ? 200 : 403);
if (mode is "trusted" or "remaining")
{
    if (mode == "trusted")
    {
        await Check("2001:db8::77", "https", 200);
        await Check(null, "https", 403);
        await Check("not-an-address", "https", 403);
        await Check("203.0.113.77, 198.51.100.8", "https", 403);
        await Check("203.0.113.77", "http", 403);
    }
    try { await Check("203.0.113.77", "https", 404, foreignHost: true); }
    catch (HttpRequestException) { cases++; }
    using var untrusted = new HttpClient(Handler(false)) { Timeout = TimeSpan.FromSeconds(10) };
    try { using var unexpected = await untrusted.GetAsync("https://api.zanance.vafadar.pro:8443/probe"); throw new InvalidOperationException("Untrusted origin CA accepted."); }
    catch (HttpRequestException) { cases++; }
}
Console.WriteLine($"Original tunnel proxy {mode} caller: {cases} bounded protocol cases passed.");

SocketsHttpHandler Handler(bool trust)
{
    var value = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false };
    value.ConnectCallback = async (_, cancellation) =>
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try { await socket.ConnectAsync(new IPEndPoint(proxy, 8443), cancellation); return new NetworkStream(socket, ownsSocket: true); }
        catch { socket.Dispose(); throw; }
    };
    if (trust)
    {
        value.SslOptions.CertificateChainPolicy = new X509ChainPolicy
        { TrustMode = X509ChainTrustMode.CustomRootTrust, RevocationMode = X509RevocationMode.NoCheck };
        value.SslOptions.CertificateChainPolicy.CustomTrustStore.Add(root);
    }
    return value;
}

async Task Check(string? originalClient, string protocol, int status, bool foreignHost = false)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.zanance.vafadar.pro:8443/probe");
    request.Headers.Host = foreignHost ? "foreign.invalid" : "api.zanance.vafadar.pro";
    request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", protocol);
    request.Headers.TryAddWithoutValidation("X-Forwarded-For", "198.51.100.9, 192.0.2.4");
    request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "foreign.invalid");
    if (originalClient is not null) request.Headers.TryAddWithoutValidation("CF-Connecting-IP", originalClient);
    using var response = await client.SendAsync(request);
    if ((int)response.StatusCode != status) throw new InvalidOperationException($"Original proxy status mismatch, expected {status}, observed {(int)response.StatusCode}.");
    if (status == 200)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var json = body.RootElement;
        if (json.GetProperty("forwardedFor").GetString() != originalClient || json.GetProperty("host").GetString() != "api.zanance.vafadar.pro" ||
            !json.GetProperty("https").GetBoolean() || json.GetProperty("residualCloudflare").GetString() != "" || json.GetProperty("residualHost").GetString() != "")
            throw new InvalidOperationException("Original proxy did not replace untrusted forwarding metadata: " + json.GetRawText());
    }
    cases++;
}
