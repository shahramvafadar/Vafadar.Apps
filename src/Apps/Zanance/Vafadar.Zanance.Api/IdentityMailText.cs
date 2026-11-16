using System.Globalization;
using System.Net;
using System.Resources;

namespace Vafadar.Zanance.Api;

/// <summary>Versioned identity instructions. Retained envelopes never borrow a later UI culture or template version.</summary>
internal static class IdentityMailText
{
    private static readonly ResourceManager Resources = new("Vafadar.Zanance.Api.Resources.IdentityMailV2", typeof(IdentityMailText).Assembly);

    /// <summary>Maps only supported six-language request metadata; missing/foreign/oversize values use neutral English.</summary>
    internal static string Language(string? requested)
    {
        if (requested is null || requested.Length > 16) return "en";
        var language = requested.Split('-', 2)[0].ToLowerInvariant();
        return language is "en" or "fa" or "de" or "es" or "fr" or "it" ? language : "en";
    }

    /// <summary>Preserves the exact first delivered format; new envelopes contain all language/template authority.</summary>
    internal static (string Subject, string Text, string Html) Render(IdentityMail mail)
    {
        if (mail.Purpose is not ("confirm" or "reset")) throw new IdentityMailUnavailable();
        if (mail.Format == 1)
        {
            var subject = mail.Purpose == "confirm" ? "Confirm your Zanance account" : "Recover your Zanance account";
            var text = $"{subject}\n\nOpen Zanance and complete the matching request using these values.\nRequest: {mail.Id:D}\nCode: {mail.Token}\n\nThis one-use code expires 15 minutes after the request. If you did not request this, ignore this email.";
            return (subject, text, "<pre>" + WebUtility.HtmlEncode(text) + "</pre>");
        }
        if (mail.Format != 2) throw new IdentityMailUnavailable();
        var language = Language(mail.Language); var culture = CultureInfo.GetCultureInfo(language);
        string Get(string key) => Resources.GetString(key, culture) ?? throw new IdentityMailUnavailable();
        var title = Get(mail.Purpose == "confirm" ? "Confirm" : "Recover");
        var intro = Get("Instruction"); var request = Get("Request"); var code = Get("Code"); var expiry = Get("Expiry");
        var plain = $"{title}\n\n{intro}\n{request}: {mail.Id:D}\n{code}: {mail.Token}\n\n{expiry}";
        static string Escape(string input) => WebUtility.HtmlEncode(input);
        var html = $"<html lang=\"{language}\" dir=\"{(language == "fa" ? "rtl" : "ltr")}\"><body><h1>{Escape(title)}</h1><p>{Escape(intro)}</p><p>{Escape(request)}: <bdi dir=\"ltr\">{mail.Id:D}</bdi></p><p>{Escape(code)}: <bdi dir=\"ltr\">{Escape(mail.Token)}</bdi></p><p>{Escape(expiry)}</p></body></html>";
        return (title, plain, html);
    }
}
