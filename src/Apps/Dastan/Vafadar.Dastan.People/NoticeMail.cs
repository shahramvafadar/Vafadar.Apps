using System.Globalization;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.People;

/// <summary>
/// What the site writes to an account about what others did (step 19.4, ADR 0016; R-PER-03, PRIV-007): a
/// request to show a relationship, a tag on a photograph, a request to agree to something being shown to
/// everybody – messages an account can turn off – and what who runs the site decided about something of the
/// account, which is always sent. A message names who asked and where to answer, and carries nothing of what
/// it is about: no photograph, no name of a person of a tree.
/// </summary>
public static class NoticeMail
{
    /// <summary>The kinds of the messages an account can turn off.</summary>
    public static readonly IReadOnlyList<string> Optional = ["relationship-asked", "photo-tagged", "photo-everybody-asked", "tree-publication-asked"];

    /// <summary>The message that somebody asks to show a relationship.</summary>
    /// <param name="language">The language of who is asked.</param>
    /// <param name="site">The address of the site, without a slash at its end.</param>
    /// <param name="to">The address the message goes to.</param>
    /// <param name="from">The name of the account that asks.</param>
    /// <param name="key">The key of the message.</param>
    public static OutgoingMail RelationshipAsked(string language, string site, string to, string from, string key) =>
        IsPersian(language)
            ? AccountMail.Write(key, "relationship-asked", to, "fa", $"«{from}» در داستان از شما درخواستی دارد",
                [$"«{from}» می‌خواهد در داستان رابطه‌ای را با شما نشان دهد. تا شما نپذیرید، چیزی نشان داده نمی‌شود.", "این‌جا می‌توانید بپذیرید یا رد کنید:"],
                $"{site}/fa/account/people",
                [Off(true)])
            : AccountMail.Write(key, "relationship-asked", to, "en", $"{from} asks something of you on Dastan",
                [$"{from} would like to show a relationship with you on Dastan. Nothing is shown unless you accept.", "You can accept or decline here:"],
                $"{site}/en/account/people",
                [Off(false)]);

    /// <summary>The message that somebody tagged the account on a photograph.</summary>
    public static OutgoingMail Tagged(string language, string site, string to, string from, string key) =>
        IsPersian(language)
            ? AccountMail.Write(key, "photo-tagged", to, "fa", $"«{from}» شما را در داستان روی عکسی نشان کرده است",
                [$"«{from}» شما را روی یکی از عکس‌هایش نشان کرده است. تا شما تأیید نکنید، عکس به شما پیوند نمی‌خورد.", "این‌جا می‌توانید تأیید کنید یا نشان را بردارید:"],
                $"{site}/fa/account/photos",
                [Off(true)])
            : AccountMail.Write(key, "photo-tagged", to, "en", $"{from} tagged you on a photograph on Dastan",
                [$"{from} tagged you on one of their photographs. The photograph is connected with you only when you approve.", "You can approve the tag or take it away here:"],
                $"{site}/en/account/photos",
                [Off(false)]);

    /// <summary>The message that somebody wants to show everybody a photograph the account is on.</summary>
    public static OutgoingMail EverybodyAsked(string language, string site, string to, string from, string key) =>
        IsPersian(language)
            ? AccountMail.Write(key, "photo-everybody-asked", to, "fa", $"«{from}» می‌خواهد عکسی را که شما در آن هستید به همه نشان دهد",
                [$"«{from}» می‌خواهد عکسی را که شما روی آن نشان شده‌اید در داستان به همه نشان دهد. تا شما موافقت نکنید، عکس به همه نشان داده نمی‌شود.", "این‌جا می‌توانید موافقت کنید یا نکنید:"],
                $"{site}/fa/account/photos",
                [Off(true)])
            : AccountMail.Write(key, "photo-everybody-asked", to, "en", $"{from} would like to show everybody a photograph you are on",
                [$"{from} would like to show everybody a photograph on Dastan that you are tagged on. It is not shown to everybody unless you agree.", "You can agree or not here:"],
                $"{site}/en/account/photos",
                [Off(false)]);

    /// <summary>The message that somebody wants to make a family tree public that names the account.</summary>
    public static OutgoingMail PublicationAsked(string language, string site, string to, string from, string key) =>
        IsPersian(language)
            ? AccountMail.Write(key, "tree-publication-asked", to, "fa", $"«{from}» می‌خواهد شجره‌نامه‌ای را که نام شما در آن است عمومی کند",
                [$"«{from}» می‌خواهد شجره‌نامه‌ای را در داستان عمومی کند که نام شما در آن آمده است. تا شما موافقت نکنید، شجره‌نامه عمومی نمی‌شود.", "این‌جا می‌توانید شجره‌نامه را ببینید و موافقت کنید یا نکنید:"],
                $"{site}/fa/account/trees",
                [Off(true)])
            : AccountMail.Write(key, "tree-publication-asked", to, "en", $"{from} would like to make a family tree public that names you",
                [$"{from} would like to make a family tree on Dastan public that names you. It does not become public unless you agree.", "You can look at the tree and agree or not here:"],
                $"{site}/en/account/trees",
                [Off(false)]);

    /// <summary>
    /// The statement of reasons of a decision of who runs the site (Article 17 of the Digital Services Act):
    /// what was decided, what it means, the facts and the ground, how it came about, and what can be done
    /// about it. The same words as on the page of the account; always sent.
    /// </summary>
    /// <param name="language">The language of the account.</param>
    /// <param name="site">The address of the site, without a slash at its end.</param>
    /// <param name="to">The address the message goes to.</param>
    /// <param name="restriction">The decision.</param>
    /// <param name="key">The key of the message.</param>
    public static OutgoingMail Restricted(string language, string site, string to, RestrictionInfo restriction, string key)
    {
        ArgumentNullException.ThrowIfNull(restriction);
        var persian = IsPersian(language);
        var number = restriction.Number.ToString(CultureInfo.InvariantCulture);
        var (what, effect) = (restriction.Kind, persian) switch
        {
            (RestrictionKind.Photo, false) => ("A photograph of yours was hidden", "Nobody but you sees the photograph, everywhere this site is reachable, until the decision is lifted. You can remove the photograph yourself."),
            (RestrictionKind.Tree, false) => ("A family tree of yours is no longer public", "The tree is not shown to the public and cannot be made public, everywhere this site is reachable, until the decision is lifted. You and the people you shared it with still see it."),
            (_, false) => ("Your account was closed", "Nobody can sign in with the account and nothing of it is shown. It is kept for 30 days and then deleted with everything that belongs to it, unless the decision is lifted before."),
            (RestrictionKind.Photo, true) => ("یکی از عکس‌های شما پنهان شد", "تا وقتی تصمیم برداشته نشده، در هر جا که این سایت در دسترس است، جز شما کسی عکس را نمی‌بیند. خودتان می‌توانید عکس را بردارید."),
            (RestrictionKind.Tree, true) => ("یکی از شجره‌نامه‌های شما دیگر عمومی نیست", "تا وقتی تصمیم برداشته نشده، در هر جا که این سایت در دسترس است، شجره‌نامه به عموم نشان داده نمی‌شود و نمی‌توان آن را عمومی کرد. شما و کسانی که با آن‌ها به اشتراک گذاشته‌اید همچنان آن را می‌بینید."),
            (_, true) => ("حساب شما بسته شد", "کسی نمی‌تواند با این حساب وارد شود و چیزی از آن نشان داده نمی‌شود. حساب ۳۰ روز نگه داشته و سپس با هر چه به آن تعلق دارد پاک می‌شود، مگر آنکه تصمیم پیش از آن برداشته شود."),
        };
        return persian
            ? AccountMail.Write(key, "restricted", to, "fa", $"{what} (تصمیم شمارهٔ {number})",
                [
                    $"گردانندهٔ داستان دربارهٔ چیزی از حساب شما تصمیمی گرفته است. شمارهٔ تصمیم: {number}.",
                    $"چه شد: {what}.",
                    $"یعنی چه: {effect}",
                    $"دلیل: {restriction.Reason}",
                    $"مبنا: {restriction.Ground}",
                    $"چگونه پیش آمد: {(restriction.Noticed ? "کسی دربارهٔ این محتوا گزارش داد و یکی از گردانندگان سایت آن را بررسی کرد." : "یکی از گردانندگان سایت به ابتکار خود محتوا را بررسی کرد.")} برای یافتن محتوا یا تصمیم‌گیری هیچ ابزار خودکاری به کار نرفته است.",
                    "اگر موافق نیستید: تا شش ماه برای ما بنویسید و شمارهٔ تصمیم را ذکر کنید؛ یک انسان دوباره آن را بررسی می‌کند و به شما پاسخ می‌دهد. می‌توانید به دادگاه هم مراجعه کنید. نشانی ما در این صفحه است:",
                ],
                $"{site}/fa/imprint",
                [restriction.Kind == RestrictionKind.Account ? "همین متن را پس از دادن نام و گذرواژه در صفحهٔ حساب هم می‌بینید." : "همین متن در صفحهٔ حساب شما هم هست."])
            : AccountMail.Write(key, "restricted", to, "en", $"{what} (decision number {number})",
                [
                    $"Who runs Dastan decided something about what belongs to your account. Number of the decision: {number}.",
                    $"What was decided: {what}.",
                    $"What it means: {effect}",
                    $"The facts: {restriction.Reason}",
                    $"The ground: {restriction.Ground}",
                    $"How it came about: {(restriction.Noticed ? "Somebody gave notice of the content, and a person who runs the site looked at it." : "A person who runs the site looked at the content on their own initiative.")} No automated means were used to find the content or to decide.",
                    "If you disagree: write to us within six months and name the number of the decision; a person looks at it again and answers you. You can also go to court. Our address is on this page:",
                ],
                $"{site}/en/imprint",
                [restriction.Kind == RestrictionKind.Account ? "You read the same words on the page of the account after giving its name and password." : "The same words are on the page of your account."]);
    }

    /// <summary>The message that a decision of who runs the site was lifted. Always sent.</summary>
    public static OutgoingMail Lifted(string language, string site, string to, RestrictionInfo restriction, string key)
    {
        ArgumentNullException.ThrowIfNull(restriction);
        var number = restriction.Number.ToString(CultureInfo.InvariantCulture);
        return IsPersian(language)
            ? AccountMail.Write(key, "restriction-lifted", to, "fa", $"تصمیم شمارهٔ {number} برداشته شد",
                [restriction.Kind == RestrictionKind.Account
                    ? $"تصمیم شمارهٔ {number} برداشته شد: حساب شما در داستان دوباره باز است و می‌توانید وارد شوید."
                    : $"تصمیم شمارهٔ {number} دربارهٔ چیزی از حساب شما در داستان برداشته شد و دیگر اثری ندارد."],
                $"{site}/fa/account",
                [])
            : AccountMail.Write(key, "restriction-lifted", to, "en", $"Decision number {number} was lifted",
                [restriction.Kind == RestrictionKind.Account
                    ? $"Decision number {number} was lifted: your account on Dastan is open again, and you can sign in."
                    : $"Decision number {number} about something of your account on Dastan was lifted and has no effect any more."],
                $"{site}/en/account",
                []);
    }

    /// <summary>
    /// The invitation an account sends to an address (D-068): one mail, written because somebody with an
    /// account asked for it, to an address the site keeps for nothing else.
    /// </summary>
    /// <param name="language">The language of the page the invitation was made on.</param>
    /// <param name="site">The address of the site, without a slash at its end.</param>
    /// <param name="to">The address the invitation goes to.</param>
    /// <param name="from">The name of the account that invites.</param>
    /// <param name="code">The code of the invitation.</param>
    /// <param name="lastDay">The last day the invitation can be used.</param>
    /// <param name="key">The key of the message.</param>
    public static OutgoingMail Invitation(string language, string site, string to, string from, string code, DateOnly lastDay, string key)
    {
        var day = lastDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return IsPersian(language)
            ? AccountMail.Write(key, "invitation", to, "fa", $"«{from}» شما را به داستان دعوت کرده است",
                [$"«{from}» در داستان حساب دارد و خواسته است این دعوت‌نامه برای شما فرستاده شود. با این پیوند می‌توانید حساب بسازید:"],
                $"{site}/fa/account?invitation={Uri.EscapeDataString(code)}",
                [$"پیوند برای یک حساب است و تا {day} کار می‌کند.", "داستان این نشانی را برای همین یک نامه به کار برده است و آن را نگه نمی‌دارد. اگر فرستنده را نمی‌شناسید، کاری لازم نیست بکنید."])
            : AccountMail.Write(key, "invitation", to, "en", $"{from} invites you to Dastan",
                [$"{from} has an account on Dastan and asked for this invitation to be sent to you. With this link you can make an account:"],
                $"{site}/en/account?invitation={Uri.EscapeDataString(code)}",
                [$"The link is for one account and works until {day}.", "Dastan used this address for this one message and does not keep it. If you do not know the sender, there is nothing you need to do."]);
    }

    private static bool IsPersian(string language) => language.StartsWith("fa", StringComparison.OrdinalIgnoreCase);

    private static string Off(bool persian) => persian
        ? "این خبرها را می‌توانید در صفحهٔ حسابتان خاموش کنید."
        : "You can turn these messages off on the page of your account.";
}
