using System.Globalization;
using System.Net;
using System.Text.Json;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Complete versioned six-language identity mail contracts, with no real sender, credentials or financial actions.</summary>
public sealed class IdentityMailCompletionTests
{
    /// <summary>Each selected language supplies its original subject, complete instructions and safely directed Latin codes.</summary>
    [Theory]
    [InlineData("en", "confirm", "Confirm your Zanance account")]
    [InlineData("en", "reset", "Recover your Zanance account")]
    [InlineData("fa", "confirm", "تأیید حساب Zanance")]
    [InlineData("fa", "reset", "بازیابی حساب Zanance")]
    [InlineData("de", "confirm", "Bestätigen Sie Ihr Zanance-Konto")]
    [InlineData("de", "reset", "Stellen Sie Ihr Zanance-Konto wieder her")]
    [InlineData("es", "confirm", "Confirma tu cuenta de Zanance")]
    [InlineData("es", "reset", "Recupera tu cuenta de Zanance")]
    [InlineData("fr", "confirm", "Confirmez votre compte Zanance")]
    [InlineData("fr", "reset", "Récupérez votre compte Zanance")]
    [InlineData("it", "confirm", "Conferma il tuo account Zanance")]
    [InlineData("it", "reset", "Recupera il tuo account Zanance")]
    public void Original_language_owns_the_entire_versioned_message(string language, string purpose, string subject)
    {
        var id = Guid.NewGuid();
        var rendered = IdentityMailText.Render(new(id, purpose, "recipient@example.invalid", "fictional<&>code", 2, language));
        Assert.Equal(subject, rendered.Subject); Assert.Contains(subject, rendered.Text);
        Assert.Contains(id.ToString("D"), rendered.Text); Assert.Contains("fictional<&>code", rendered.Text);
        Assert.Contains("fictional&lt;&amp;&gt;code", rendered.Html);
        Assert.Contains("<bdi dir=\"ltr\">", rendered.Html);
        Assert.Contains("lang=\"" + language + "\"", rendered.Html);
        Assert.Contains("dir=\"" + (language == "fa" ? "rtl" : "ltr") + "\"", rendered.Html);
        Assert.DoesNotContain("http", rendered.Html); Assert.DoesNotContain("fictional<&>code", rendered.Html);
    }

    /// <summary>Already queued format-1 envelopes retain their exact previous body even after current UI language changes.</summary>
    [Fact]
    public void Legacy_pending_body_stays_exact_without_borrowing_new_templates()
    {
        var id = Guid.Parse("51c7a129-67d6-4e8b-bb09-08b10304a036");
        var result = IdentityMailText.Render(new(id, "reset", "recipient@example.invalid", "fictional-code", 1, "fa"));
        const string expected = "Recover your Zanance account\n\nOpen Zanance and complete the matching request using these values.\nRequest: 51c7a129-67d6-4e8b-bb09-08b10304a036\nCode: fictional-code\n\nThis one-use code expires 15 minutes after the request. If you did not request this, ignore this email.";
        Assert.Equal(expected, result.Text); Assert.Equal("<pre>" + WebUtility.HtmlEncode(expected) + "</pre>", result.Html);
    }

    /// <summary>A future persisted format cannot silently borrow today's template.</summary>
    [Fact]
    public void Unknown_persisted_format_refuses_instead_of_relabeling_the_original_message()
        => Assert.Throws<IdentityMailUnavailable>(() => IdentityMailText.Render(
            new(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-code", 3, "en")));

    /// <summary>The actual AOT-safe client DTO freezes UI language before serialization; the original server accepts that wire shape.</summary>
    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("fa-IR", "fa")]
    [InlineData("de-DE", "de")]
    [InlineData("es-ES", "es")]
    [InlineData("fr-FR", "fr")]
    [InlineData("it-IT", "it")]
    public async Task Actual_client_payload_captures_language_before_later_ui_change(string culture, string language)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var input = new BillingIdentityInput("recipient@example.invalid", "fictional-password");
            await Task.Yield(); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            var json = JsonSerializer.Serialize(input, BillingIdentityJson.Default.BillingIdentityInput);
            var server = JsonSerializer.Deserialize<IdentityInput>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal(language, input.Language); Assert.Equal(language, server.Language);
            Assert.Equal("recipient@example.invalid", server.Email); Assert.Equal("fictional-password", server.Password);
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    /// <summary>Foreign metadata does not select arbitrary resource/culture identifiers.</summary>
    [Theory]
    [InlineData(null, "en")]
    [InlineData("fa-IR", "fa")]
    [InlineData("DE-de", "de")]
    [InlineData("ja-JP", "en")]
    [InlineData("../../private", "en")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "en")]
    public void Language_metadata_is_bounded_to_supported_resources(string? input, string language)
        => Assert.Equal(language, IdentityMailText.Language(input));

    /// <summary>Credential-bearing request/envelope diagnostics do not format their actual protected values.</summary>
    [Fact]
    public void Identity_record_diagnostics_exclude_credentials_and_recipients()
    {
        object[] values = [new IdentityInput("recipient@example.invalid", "fictional-secret"),
            new ChallengeInput(Guid.NewGuid(), "fictional-secret", "fictional-password"),
            new RefreshInput("fictional-secret"), new PrivateMail("recipient@example.invalid", "fictional-secret"),
            new IdentityMail(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-secret")];
        foreach (var value in values)
        {
            Assert.DoesNotContain("fictional", value.ToString());
            Assert.DoesNotContain("recipient", value.ToString());
        }
    }
}
