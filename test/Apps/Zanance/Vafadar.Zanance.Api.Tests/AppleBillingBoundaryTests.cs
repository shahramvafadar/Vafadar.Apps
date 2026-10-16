using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Vafadar.Zanance.Api.Billing;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Provider authentication refusals with no Apple developer key, network account or fabricated accepted receipt.</summary>
[Trait("AT", "AT-295")]
public sealed class AppleBillingBoundaryTests
{
    /// <summary>Wrong algorithms and remote key locators never select a key or initiate certificate discovery.</summary>
    [Theory]
    [InlineData("none")][InlineData("HS256")][InlineData("remote-key")]
    public void Untrusted_provider_key_selection_is_refused(string kind)
    {
        var header = kind == "remote-key" ? "{\"alg\":\"ES256\",\"jku\":\"https://example.invalid/key\"}" : "{\"alg\":\"" + kind + "\"}";
        var token = Base64Url(System.Text.Encoding.UTF8.GetBytes(header)) + "." + Base64Url("{}"u8.ToArray()) + "." + Base64Url(new byte[64]);
        var verifier = new AppleBillingSignatures(TimeProvider.System);
        Assert.Throws<BillingRefusal>(() => verifier.Verify(token));
    }

    /// <summary>A locally self-signed chain cannot become Apple's trust anchor even when all supplied claims look correct.</summary>
    [Fact]
    public void Supplied_self_signed_root_is_never_accepted_as_apple_authority()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Fictional billing refusal only", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509Extension("1.2.840.113635.100.6.11.1", [5, 0], false));
        request.CertificateExtensions.Add(new X509Extension("1.2.840.113635.100.6.2.1", [5, 0], false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var encoded = Convert.ToBase64String(certificate.RawData);
        var header = "{\"alg\":\"ES256\",\"x5c\":[\"" + encoded + "\",\"" + encoded + "\",\"" + encoded + "\"]}";
        var token = Base64Url(System.Text.Encoding.UTF8.GetBytes(header)) + "." + Base64Url("{}"u8.ToArray()) + "." + Base64Url(new byte[64]);
        Assert.Equal("AppleCertificatePurposeRefused", Assert.Throws<BillingRefusal>(() => new AppleBillingSignatures(TimeProvider.System).Verify(token)).Message);
    }

    /// <summary>The real Apple lower-camel wire member is preserved by the strict source-generated input parser.</summary>
    [Fact]
    public void Apple_notification_reads_the_actual_signed_payload_field()
    {
        var bytes = "{\"signedPayload\":\"fictional-not-an-accepted-receipt\"}"u8.ToArray();
        var value = Vafadar.Cryptography.StrictJsonPayload.Deserialize(bytes, BillingProviderJson.Default.AppleNotificationEnvelope, 1024);
        Assert.Equal("fictional-not-an-accepted-receipt", value.SignedPayload);
        Assert.DoesNotContain(value.SignedPayload, value.ToString());
    }
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
