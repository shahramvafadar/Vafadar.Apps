using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>Who may see a fact an account states about itself.</summary>
/// <param name="Shown">Nobody else, a circle, everybody.</param>
/// <param name="Circle">The circle, when it is for one of the account's.</param>
public sealed record FactAudience(ScopeKind Shown, long? Circle = null)
{
    /// <summary>Gets the audience every fact begins with: nobody else.</summary>
    public static FactAudience Nobody { get; } = new(ScopeKind.Private);
}

/// <summary>
/// The facts an account states about itself (delivery plan 25.5), as the account itself reads them: each with
/// an audience of its own.
/// </summary>
/// <param name="BornYear">The year of the common era the account says it was born in – a year, never a day – or nothing.</param>
/// <param name="Born">Who may see the year.</param>
/// <param name="Town">The town the account says it lives in, in its own words, or nothing.</param>
/// <param name="Country">The country, in its own words, or nothing.</param>
/// <param name="Lives">Who may see where it lives.</param>
/// <param name="Languages">The languages it says it speaks, as tags of <see cref="AccountLanguages"/>.</param>
/// <param name="Speaks">Who may see the languages.</param>
/// <param name="Link">The address of a page of its own, or nothing.</param>
/// <param name="Linked">Who may see the address.</param>
public sealed record AccountFacts(
    int? BornYear,
    FactAudience Born,
    string? Town,
    string? Country,
    FactAudience Lives,
    ImmutableArray<string> Languages,
    FactAudience Speaks,
    string? Link,
    FactAudience Linked)
{
    /// <summary>Gets the facts of an account that states none.</summary>
    public static AccountFacts None { get; } = new(null, FactAudience.Nobody, null, null, FactAudience.Nobody, [], FactAudience.Nobody, null, FactAudience.Nobody);
}

/// <summary>What a viewer is shown of the facts an account states about itself: only what each audience allows.</summary>
/// <param name="BornYear">The year of birth, when the viewer may see it.</param>
/// <param name="Town">The town, when the viewer may see where the account lives.</param>
/// <param name="Country">The country, when the viewer may see where the account lives.</param>
/// <param name="Languages">The languages, when the viewer may see them.</param>
/// <param name="Link">The address of the page of its own, when the viewer may see it.</param>
public sealed record ShownFacts(int? BornYear, string? Town, string? Country, ImmutableArray<string> Languages, string? Link)
{
    /// <summary>Gets what is shown of an account that states nothing, or nothing to this viewer: the two are the same (PRIV-012).</summary>
    public static ShownFacts None { get; } = new(null, null, null, [], null);

    /// <summary>Gets a value indicating whether nothing is shown.</summary>
    public bool IsEmpty => BornYear is null && Town is null && Country is null && Languages.IsEmpty && Link is null;
}

/// <summary>The languages an account can say it speaks: tags, each with the name the language gives itself.</summary>
public static class AccountLanguages
{
    /// <summary>The greatest number of languages an account names.</summary>
    public const int Most = 8;

    /// <summary>Gets the languages, in the order of their names in Latin letters.</summary>
    public static ImmutableArray<(string Tag, string Name)> Known { get; } =
    [
        ("sq", "Shqip"), ("am", "አማርኛ"), ("ar", "العربية"), ("hy", "Հայերեն"), ("az", "Azərbaycanca"), ("eu", "Euskara"), ("bn", "বাংলা"),
        ("bs", "Bosanski"), ("bg", "Български"), ("ca", "Català"), ("zh", "中文"), ("hr", "Hrvatski"), ("cs", "Čeština"), ("da", "Dansk"),
        ("nl", "Nederlands"), ("en", "English"), ("et", "Eesti"), ("fi", "Suomi"), ("fr", "Français"), ("ka", "ქართული"), ("de", "Deutsch"),
        ("el", "Ελληνικά"), ("gu", "ગુજરાતી"), ("he", "עברית"), ("hi", "हिन्दी"), ("hu", "Magyar"), ("is", "Íslenska"), ("id", "Bahasa Indonesia"),
        ("ga", "Gaeilge"), ("it", "Italiano"), ("ja", "日本語"), ("kk", "Қазақша"), ("ko", "한국어"), ("ku", "Kurdî"), ("lv", "Latviešu"),
        ("lt", "Lietuvių"), ("mk", "Македонски"), ("ms", "Bahasa Melayu"), ("mn", "Монгол"), ("ne", "नेपाली"), ("no", "Norsk"), ("ps", "پښتو"),
        ("fa", "فارسی"), ("pl", "Polski"), ("pt", "Português"), ("pa", "ਪੰਜਾਬੀ"), ("ro", "Română"), ("ru", "Русский"), ("sr", "Српски"),
        ("sk", "Slovenčina"), ("sl", "Slovenščina"), ("so", "Soomaali"), ("es", "Español"), ("sw", "Kiswahili"), ("sv", "Svenska"),
        ("tg", "Тоҷикӣ"), ("ta", "தமிழ்"), ("th", "ไทย"), ("tr", "Türkçe"), ("uk", "Українська"), ("ur", "اردو"), ("uz", "Oʻzbekcha"),
        ("vi", "Tiếng Việt"), ("cy", "Cymraeg"),
    ];

    /// <summary>Returns the name a language gives itself, or the tag for one the list does not have.</summary>
    public static string Name(string tag) => Known.FirstOrDefault(language => language.Tag == tag) is { Tag: not null } found ? found.Name : tag;

    /// <summary>Returns whether a tag is one of the list.</summary>
    public static bool Is(string tag) => Known.Any(language => language.Tag == tag);
}

/// <content>
/// The facts an account states about itself (delivery plan 25.5, D-088): a year of birth, where it lives, the
/// languages it speaks and one address of a page of its own. None is ever asked for. Each has an audience of
/// its own and begins with nobody else; a fact that is not stated has no audience. A viewer is shown what each
/// audience allows and is not told what else there is (PRIV-012). Nothing of it is used to find, sort or
/// suggest people, and no age is computed from the year.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>The earliest year of birth an account can state.</summary>
    public const int EarliestBornYear = 1900;

    /// <summary>The greatest length of the town and of the country an account states.</summary>
    public const int LongestPlaceOfAccount = 80;

    /// <summary>The greatest length of the address of the page an account names.</summary>
    public const int LongestLink = 200;

    /// <summary>Returns the facts an open account states about itself, as the account itself reads them; nothing for no such account.</summary>
    public AccountFacts? Facts(string id)
    {
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        return visitor is null
            ? null
            : new(
                visitor.BornYear,
                new((ScopeKind)visitor.BornScope, visitor.BornCircle),
                visitor.Town,
                visitor.Country,
                new((ScopeKind)visitor.LivesScope, visitor.LivesCircle),
                Tags(visitor.Languages),
                new((ScopeKind)visitor.LanguagesScope, visitor.LanguagesCircle),
                visitor.Link,
                new((ScopeKind)visitor.LinkScope, visitor.LinkCircle));
    }

    /// <summary>
    /// Keeps the facts an account states about itself. Returns <c>done</c>; <c>refused</c> for no such account
    /// or an audience that is none; <c>year</c> for a year that is no year of birth; <c>long</c> for a town or a
    /// country that is too long or no line of text; <c>languages</c> for a language the list does not have, one
    /// named twice or too many; <c>link</c> for an address that is no address of a page under https.
    /// </summary>
    /// <param name="id">The account.</param>
    /// <param name="facts">What it states; what is nothing or blank is not stated.</param>
    public string StateFacts(string id, AccountFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return Guarded([OfAccount(id)], () =>
        {
            var stored = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
            var audiences = new[] { facts.Born, facts.Lives, facts.Speaks, facts.Linked };
            if (stored is null || audiences.Any(audience => audience is null || !Enum.IsDefined(audience.Shown) || !OwnsCircle(id, audience.Shown, audience.Circle)))
            {
                _context.ChangeTracker.Clear();
                return "refused";
            }

            string Problem(string problem)
            {
                _context.ChangeTracker.Clear();
                return problem;
            }

            if (facts.BornYear is { } year && (year < EarliestBornYear || year >= Now.Year))
            {
                return Problem("year");
            }

            var (town, country) = (Line(facts.Town, LongestPlaceOfAccount), Line(facts.Country, LongestPlaceOfAccount));
            if (town.Refused || country.Refused)
            {
                return Problem("long");
            }

            var languages = facts.Languages.IsDefault ? [] : facts.Languages;
            if (languages.Length > AccountLanguages.Most || languages.Distinct(StringComparer.Ordinal).Count() != languages.Length || !languages.All(AccountLanguages.Is))
            {
                return Problem("languages");
            }

            var link = string.IsNullOrWhiteSpace(facts.Link) ? null : facts.Link.Trim();
            if (link is not null && !IsPage(link))
            {
                return Problem("link");
            }

            // A fact that is not stated is for nobody: no audience is kept for nothing.
            static (int Scope, long? Circle) Kept(bool stated, FactAudience audience) =>
                stated ? ((int)audience.Shown, audience.Shown == ScopeKind.Circle ? audience.Circle : null) : ((int)ScopeKind.Private, null);

            stored.BornYear = facts.BornYear;
            (stored.BornScope, stored.BornCircle) = Kept(facts.BornYear is not null, facts.Born);
            (stored.Town, stored.Country) = (town.Value, country.Value);
            (stored.LivesScope, stored.LivesCircle) = Kept(town.Value is not null || country.Value is not null, facts.Lives);
            stored.Languages = languages.IsEmpty ? null : string.Join(',', languages);
            (stored.LanguagesScope, stored.LanguagesCircle) = Kept(!languages.IsEmpty, facts.Speaks);
            stored.Link = link;
            (stored.LinkScope, stored.LinkCircle) = Kept(link is not null, facts.Linked);
            Save();
            return "done";
        });
    }

    // The address of a page somebody can open: absolute, under https, with a host and without a name or a password in it.
    private static bool IsPage(string written) =>
        written.Length <= LongestLink && !written.Any(char.IsControl) && !written.Any(char.IsWhiteSpace)
        && Uri.TryCreate(written, UriKind.Absolute, out var address) && address.Scheme == Uri.UriSchemeHttps
        && address.Host.Contains('.', StringComparison.Ordinal) && address.UserInfo.Length == 0;

    private static ImmutableArray<string> Tags(string? kept) =>
        string.IsNullOrEmpty(kept) ? [] : [.. kept.Split(',', StringSplitOptions.RemoveEmptyEntries)];

    // What a viewer is shown of the facts of an account: each where its audience allows, asked before anything
    // is made of it (PRIV-012).
    private ShownFacts FactsShown(StoredVisitor person, string? viewer)
    {
        AccountId? account = viewer is null ? null : new AccountId(viewer);
        bool Allows(int scope, long? circle) => Scope(person.Id, (ScopeKind)scope, circle).Allows(account);
        var lives = Allows(person.LivesScope, person.LivesCircle);
        var shown = new ShownFacts(
            person.BornYear is not null && Allows(person.BornScope, person.BornCircle) ? person.BornYear : null,
            lives ? person.Town : null,
            lives ? person.Country : null,
            person.Languages is not null && Allows(person.LanguagesScope, person.LanguagesCircle) ? Tags(person.Languages) : [],
            person.Link is not null && Allows(person.LinkScope, person.LinkCircle) ? person.Link : null);
        return shown.IsEmpty ? ShownFacts.None : shown;
    }

    // A circle that is given up takes what was for it back to nobody else.
    private void FactsOfCircleBackToNobody(long circle)
    {
        foreach (var stated in _context.Visitors.Where(candidate => candidate.BornCircle == circle || candidate.LivesCircle == circle || candidate.LanguagesCircle == circle || candidate.LinkCircle == circle))
        {
            if (stated.BornCircle == circle)
            {
                (stated.BornScope, stated.BornCircle) = ((int)ScopeKind.Private, null);
            }

            if (stated.LivesCircle == circle)
            {
                (stated.LivesScope, stated.LivesCircle) = ((int)ScopeKind.Private, null);
            }

            if (stated.LanguagesCircle == circle)
            {
                (stated.LanguagesScope, stated.LanguagesCircle) = ((int)ScopeKind.Private, null);
            }

            if (stated.LinkCircle == circle)
            {
                (stated.LinkScope, stated.LinkCircle) = ((int)ScopeKind.Private, null);
            }
        }
    }
}
