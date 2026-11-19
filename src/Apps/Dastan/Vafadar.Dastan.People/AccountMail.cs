using System.Net;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.People;

/// <summary>
/// What the site writes to a visitor about their account (ADR 0016), in the language of the visitor: as plain
/// text and, beside it, as simple HTML with the direction of the language. A message says only what its
/// receiver needs, loads nothing from anywhere, and a link in it is used once and for a short time.
/// </summary>
public static class AccountMail
{
    /// <summary>The message with the link that confirms a mail address.</summary>
    /// <param name="language">The language of the visitor.</param>
    /// <param name="site">The address of the site, without a slash at its end.</param>
    /// <param name="to">The address the message goes to.</param>
    /// <param name="name">The name of the account.</param>
    /// <param name="id">The identifier of the account.</param>
    /// <param name="token">The token of the link.</param>
    /// <param name="key">The key of the message.</param>
    public static OutgoingMail Confirm(string language, string site, string to, string name, string id, string token, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account/confirm?id={Uri.EscapeDataString(id)}&token={Uri.EscapeDataString(token)}";
        return persian
            ? Write(key, "confirm-address", to, "fa", "نشانی ایمیل خود را در داستان تأیید کنید",
                [$"{name} گرامی،", "برای این نشانی در داستان حسابی ساخته شده است. برای این‌که حساب باز شود، این پیوند را باز کنید:"],
                link,
                ["این پیوند یک بار و تا ۲۴ ساعت کار می‌کند.", "اگر شما این حساب را نساخته‌اید، کاری لازم نیست بکنید: بدون تأیید، حسابی باز نمی‌شود."])
            : Write(key, "confirm-address", to, "en", "Confirm your mail address for Dastan",
                [$"Hello {name},", "An account was made on Dastan with this address. To open the account, open this link:"],
                link,
                ["The link works once, and for 24 hours.", "If you did not make this account, there is nothing you need to do: without the confirmation no account is opened."]);
    }

    /// <summary>The message to who has an address that somebody tried to make another account with.</summary>
    /// <param name="language">The language of who has the address.</param>
    /// <param name="site">The address of the site, without a slash at its end.</param>
    /// <param name="to">The address the message goes to.</param>
    /// <param name="key">The key of the message.</param>
    public static OutgoingMail AddressInUse(string language, string site, string to, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account";
        return persian
            ? Write(key, "address-in-use", to, "fa", "کسی خواست با نشانی شما در داستان حساب بسازد",
                ["کسی خواست با این نشانی ایمیل در داستان حساب تازه‌ای بسازد. این نشانی از پیش حساب دارد و حساب تازه‌ای ساخته نشد.", "اگر خودتان بودید، می‌توانید این‌جا وارد شوید:"],
                link,
                ["اگر شما نبودید، کاری لازم نیست بکنید. چیزی از حساب شما به کسی گفته نشده است."])
            : Write(key, "address-in-use", to, "en", "Somebody tried to make a Dastan account with your address",
                ["Somebody tried to make a new account on Dastan with this mail address. The address already has an account, and no new one was made.", "If it was you, you can sign in here:"],
                link,
                ["If it was not you, there is nothing you need to do. Nothing about your account was told to anybody."]);
    }

    private static bool IsPersian(string language) => language.StartsWith("fa", StringComparison.OrdinalIgnoreCase);

    // The same words as plain text and as HTML: paragraphs, one link on a line of its own, paragraphs.
    private static OutgoingMail Write(string key, string kind, string to, string language, string subject, string[] before, string link, string[] after)
    {
        var text = string.Join("\n\n", [.. before, link, .. after, "Dastan"]);
        var direction = language == "fa" ? "rtl" : "ltr";
        var html =
            $"<!doctype html><html lang=\"{language}\" dir=\"{direction}\"><body style=\"font-family:sans-serif;line-height:1.6\">"
            + string.Concat(before.Select(Paragraph))
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(link)}</a></p>"
            + string.Concat(after.Select(Paragraph))
            + "<p>Dastan</p></body></html>";
        return new OutgoingMail(key, kind, to, language, subject, text, html);

        static string Paragraph(string words) => $"<p>{WebUtility.HtmlEncode(words)}</p>";
    }
}
