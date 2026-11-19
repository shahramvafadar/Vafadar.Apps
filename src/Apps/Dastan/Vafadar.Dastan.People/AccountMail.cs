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

    /// <summary>The message with the link that sets a new password for one that was forgotten.</summary>
    public static OutgoingMail ResetPassword(string language, string site, string to, string id, string token, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account/reset?id={Uri.EscapeDataString(id)}&token={Uri.EscapeDataString(token)}";
        return persian
            ? Write(key, "reset-password", to, "fa", "گذرواژهٔ تازه برای حساب شما در داستان",
                ["برای حساب شما در داستان گذرواژهٔ تازه خواسته شده است. برای گذاشتن گذرواژهٔ تازه این پیوند را باز کنید:"],
                link,
                ["این پیوند یک بار و تا یک ساعت کار می‌کند.", "اگر شما نخواسته‌اید، کاری لازم نیست بکنید: گذرواژهٔ شما همان است که بود."])
            : Write(key, "reset-password", to, "en", "A new password for your Dastan account",
                ["A new password was asked for your account on Dastan. To set one, open this link:"],
                link,
                ["The link works once, and for one hour.", "If you did not ask for it, there is nothing you need to do: your password is what it was."]);
    }

    /// <summary>The message that tells an account that its password changed.</summary>
    public static OutgoingMail PasswordChanged(string language, string site, string to, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account";
        return persian
            ? Write(key, "password-changed", to, "fa", "گذرواژهٔ حساب شما در داستان عوض شد",
                ["گذرواژهٔ حساب شما در داستان عوض شد و حساب از همهٔ دستگاه‌ها خارج شد.", "اگر خودتان این کار را کرده‌اید، کاری لازم نیست. اگر نه، این‌جا با «گذرواژه را فراموش کرده‌ام» گذرواژهٔ تازه بگذارید:"],
                link,
                [])
            : Write(key, "password-changed", to, "en", "The password of your Dastan account was changed",
                ["The password of your account on Dastan was changed, and the account was signed out everywhere.", "If you did this, there is nothing to do. If you did not, set a new password here with \"I forgot my password\":"],
                link,
                []);
    }

    /// <summary>The message to a new address with the link that makes it the address of the account.</summary>
    public static OutgoingMail ConfirmNewAddress(string language, string site, string to, string id, string token, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account/address?id={Uri.EscapeDataString(id)}&token={Uri.EscapeDataString(token)}";
        return persian
            ? Write(key, "confirm-new-address", to, "fa", "نشانی تازهٔ حساب خود را در داستان تأیید کنید",
                ["برای یک حساب در داستان خواسته شده است که این نشانی، نشانی ایمیل آن شود. برای این کار این پیوند را باز کنید:"],
                link,
                ["این پیوند یک بار و تا ۲۴ ساعت کار می‌کند.", "اگر شما نخواسته‌اید، کاری لازم نیست بکنید: بدون تأیید چیزی عوض نمی‌شود."])
            : Write(key, "confirm-new-address", to, "en", "Confirm the new address of your Dastan account",
                ["An account on Dastan asked for this address to become its mail address. To make it so, open this link:"],
                link,
                ["The link works once, and for 24 hours.", "If you did not ask for it, there is nothing you need to do: without the confirmation nothing changes."]);
    }

    /// <summary>The message to the address an account has, saying that another address was asked for.</summary>
    public static OutgoingMail AddressChangeAsked(string language, string site, string to, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account";
        return persian
            ? Write(key, "address-change-asked", to, "fa", "برای حساب شما در داستان نشانی ایمیل تازه خواسته شد",
                ["برای حساب شما در داستان نشانی ایمیل دیگری خواسته شده است. تا آن نشانی تأیید نشود، چیزی عوض نمی‌شود.", "اگر شما نخواسته‌اید، کسی گذرواژهٔ شما را می‌داند: این‌جا گذرواژهٔ تازه بگذارید:"],
                link,
                [])
            : Write(key, "address-change-asked", to, "en", "Another mail address was asked for your Dastan account",
                ["Another mail address was asked for your account on Dastan. Nothing changes until that address is confirmed.", "If you did not ask for it, somebody knows your password: set a new one here:"],
                link,
                []);
    }

    /// <summary>The message to the address an account had, saying that it has another one now.</summary>
    public static OutgoingMail AddressChanged(string language, string site, string to, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/notice";
        return persian
            ? Write(key, "address-changed", to, "fa", "نشانی ایمیل حساب شما در داستان عوض شد",
                ["حساب شما در داستان اکنون نشانی ایمیل دیگری دارد و دیگر به این نشانی نامه‌ای نمی‌رود.", "اگر شما این کار را نکرده‌اید، از این راه به گردانندهٔ سایت بنویسید:"],
                link,
                [])
            : Write(key, "address-changed", to, "en", "The mail address of your Dastan account was changed",
                ["Your account on Dastan has another mail address now, and no more mail goes to this one.", "If you did not do this, write to who runs the site this way:"],
                link,
                []);
    }

    /// <summary>The message that tells an account that it was closed by its visitor, and until when it can be opened again.</summary>
    public static OutgoingMail Closed(string language, string site, string to, string key)
    {
        var persian = IsPersian(language);
        var link = $"{site}/{(persian ? "fa" : "en")}/account";
        return persian
            ? Write(key, "account-closed", to, "fa", "حساب شما در داستان بسته شد",
                ["حساب شما در داستان بسته شد. دیگر جایی نشان داده نمی‌شود و پس از ۳۰ روز با هرچه به آن تعلق دارد پاک می‌شود.", "اگر پشیمان شدید، تا آن زمان می‌توانید این‌جا با نام و گذرواژه دوباره آن را باز کنید:"],
                link,
                [])
            : Write(key, "account-closed", to, "en", "Your Dastan account was closed",
                ["Your account on Dastan was closed. It is shown nowhere any more, and is deleted with everything that belongs to it after 30 days.", "If you change your mind, you can open it again until then, with your name and password, here:"],
                link,
                []);
    }

    private static bool IsPersian(string language) => language.StartsWith("fa", StringComparison.OrdinalIgnoreCase);

    // The same words as plain text and as HTML: paragraphs, one link on a line of its own, paragraphs.
    internal static OutgoingMail Write(string key, string kind, string to, string language, string subject, string[] before, string link, string[] after)
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
