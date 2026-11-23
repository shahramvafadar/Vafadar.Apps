using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Resources;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The forms of the relationships and the circles of a visitor (step 16.3). Every one is for who signed in and
/// acts for that account only; what an act may do is decided by the store with the rules of step 15.2.
/// </summary>
public static class PeopleEndpoints
{
    /// <summary>The address of the page of the relationships, without the language.</summary>
    public const string Page = "/account/people";

    /// <summary>The address of the page about an account, without the language and the identifier.</summary>
    public const string Person = "/people";

    /// <summary>The address the forms are sent to. It has no language.</summary>
    public const string Root = "/preferences/people";

    /// <summary>Maps the forms. Each sends the visitor back to the page of the relationships, which says what happened.</summary>
    public static void MapPeople(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var forms = endpoints.MapGroup(Root).AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            return !context.RequestServices.GetRequiredService<AccountDesk>().IsOpen ? Results.NotFound()
                : AccountDesk.Session(context.User) is null ? Results.BadRequest()
                : await next(invocation).ConfigureAwait(false);
        });

        forms.MapPost("/ask", (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] string? name, [FromForm] string? kind, [FromForm] string? place, [FromForm] string? returnUrl) =>
        {
            // Asking by name could be used to try names: an address asks only so often.
            if (!desk.Allows(context, "ask", AccountDesk.AttemptsPerHour))
            {
                return Back(returnUrl, "ask", "later");
            }

            if (Terms(context.Request.Form, kind, place, library.Institutions, kept: null) is not var (chosen, terms))
            {
                return Back(returnUrl, "ask", "terms");
            }

            using var store = desk.Open();
            var outcome = store.Ask(Me(context), name ?? string.Empty, chosen, terms);
            return outcome == "asked" ? Said(returnUrl, "asked") : Back(returnUrl, "ask", outcome);
        });

        // An accepted relationship is to be called something else (ADR 0018): asked of the other side, and
        // nothing changes until they agree.
        forms.MapPost("/change", (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] string? relationship, [FromForm] string? kind, [FromForm] string? place, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();

            // A relationship may keep the entry it points at, also while that entry is not published.
            var kept = store.Relationships(Me(context)).FirstOrDefault(mine => mine.Id == relationship)?.Terms.Place;
            if (Terms(context.Request.Form, kind, place, library.Institutions, kept) is not var (chosen, terms))
            {
                return Back(returnUrl, "mine", "terms");
            }

            return store.AskChange(Me(context), relationship ?? string.Empty, chosen, terms) switch
            {
                "asked" => Said(returnUrl, "change-asked", "mine"),
                "refused" => Results.BadRequest(),
                var outcome => Back(returnUrl, "mine", outcome),
            };
        });

        forms.MapPost("/answer", (HttpContext context, AccountDesk desk, [FromForm] string? relationship, [FromForm] string? answer, [FromForm] string? returnUrl) =>
        {
            if (answer is not ("accept" or "decline"))
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Answer(Me(context), relationship ?? string.Empty, answer == "accept")
                ? Said(returnUrl, answer == "accept" ? "accepted" : "declined")
                : Results.BadRequest();
        });

        forms.MapPost("/cancel", (HttpContext context, AccountDesk desk, [FromForm] string? relationship, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.Cancel(Me(context), relationship ?? string.Empty) ? Said(returnUrl, "cancelled") : Results.BadRequest();
        });

        forms.MapPost("/end", (HttpContext context, AccountDesk desk, [FromForm] string? relationship, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.End(Me(context), relationship ?? string.Empty) ? Said(returnUrl, "ended") : Results.BadRequest();
        });

        forms.MapPost("/show", (HttpContext context, AccountDesk desk, [FromForm] string? relationship, [FromForm] string? scope, [FromForm] string? returnUrl) =>
        {
            var chosen = Scope(scope);
            if (chosen is null)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Show(Me(context), relationship ?? string.Empty, chosen.Value.Kind, chosen.Value.Circle) ? Said(returnUrl, "shown") : Results.BadRequest();
        });

        // What an account says about itself on its page (D-086): a few words, who reads them, and a picture.
        forms.MapPost("/describe", (HttpContext context, AccountDesk desk, [FromForm] string? about, [FromForm] string? shown, [FromForm] string? picture, [FromForm] string? returnUrl) =>
        {
            if (Scope(shown) is not { } chosen)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            var me = Me(context);

            // The page of the account, in the language the form came from, and nowhere else.
            var address = ReturnAddress.Safe(returnUrl, $"{Person}/{me}");
            var end = address.IndexOfAny(['?', '#']);
            address = end < 0 ? address : address[..end];
            var page = address.EndsWith($"{Person}/{me}", StringComparison.Ordinal) && address.Length <= Person.Length + me.Length + 13 ? address : $"{Person}/{me}";
            return store.DescribeAccount(me, about, chosen.Kind, chosen.Circle, picture) switch
            {
                "done" => Results.Redirect($"{page}?said=described"),
                "refused" => Results.BadRequest(),
                var problem => Results.Redirect($"{page}?form=describe&problem={problem}#describe"),
            };
        });

        // The facts an account states about itself (step 25.5): each with an audience of its own.
        forms.MapPost("/facts", (HttpContext context, AccountDesk desk, [FromForm] string? born, [FromForm] string? bornShown, [FromForm] string? town, [FromForm] string? country,
            [FromForm] string? livesShown, [FromForm] string? languagesShown, [FromForm] string? link, [FromForm] string? linkShown, [FromForm] string? returnUrl) =>
        {
            if (Scope(bornShown) is not { } bornFor || Scope(livesShown) is not { } livesFor || Scope(languagesShown) is not { } languagesFor || Scope(linkShown) is not { } linkFor)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            var me = Me(context);
            var address = ReturnAddress.Safe(returnUrl, $"{Person}/{me}");
            var end = address.IndexOfAny(['?', '#']);
            address = end < 0 ? address : address[..end];
            var page = address.EndsWith($"{Person}/{me}", StringComparison.Ordinal) && address.Length <= Person.Length + me.Length + 13 ? address : $"{Person}/{me}";

            // A year is typed as digits, also Persian ones; what is no number is no year.
            int? year = null;
            if (!string.IsNullOrWhiteSpace(born))
            {
                var digits = new string([.. born.Trim().Select(character => char.IsDigit(character) ? (char)('0' + char.GetNumericValue(character)) : character)]);
                // What is no number is no year: the store refuses the year zero like every year nobody was born
                // in, and what else was typed is kept as for every other fact that is none.
                year = int.TryParse(digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var typed) ? typed : 0;
            }

            var facts = new AccountFacts(
                year, new(bornFor.Kind, bornFor.Circle),
                town, country, new(livesFor.Kind, livesFor.Circle),
                // The languages are the boxes that were ticked: one field of the form, said several times.
                [.. context.Request.Form["languages"].Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag!.Trim())], new(languagesFor.Kind, languagesFor.Circle),
                link, new(linkFor.Kind, linkFor.Circle));
            var came = store.StateFacts(me, facts);
            if (came is "done" or "refused")
            {
                return came == "done" ? Results.Redirect($"{page}?said=stated") : Results.BadRequest();
            }

            // One fact is none: it stays as it was, and what else was typed is kept – a form of nine fields is
            // not typed again for one of them. What was typed never goes into an address. The first problem
            // is the one the page names; a second one is found the same way and stays as it was too.
            var first = came;
            for (var round = 0; round < 4 && came is not ("done" or "refused") && store.Facts(me) is { } kept; round++)
            {
                facts = came switch
                {
                    "year" => facts with { BornYear = kept.BornYear, Born = kept.Born },
                    "long" => facts with { Town = kept.Town, Country = kept.Country, Lives = kept.Lives },
                    "languages" => facts with { Languages = kept.Languages, Speaks = kept.Speaks },
                    _ => facts with { Link = kept.Link, Linked = kept.Linked },
                };
                came = store.StateFacts(me, facts);
            }

            return Results.Redirect($"{page}?form=facts&problem={first}#facts");
        });

        // Blocking (PRIV-009): one-sided, and the other account is not told.
        forms.MapPost("/block", (HttpContext context, AccountDesk desk, [FromForm] string? account, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.Block(Me(context), account ?? string.Empty) ? Said(returnUrl, "blocked", "blocked") : Results.BadRequest();
        });

        forms.MapPost("/unblock", (HttpContext context, AccountDesk desk, [FromForm] string? account, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.Unblock(Me(context), account ?? string.Empty) ? Said(returnUrl, "unblocked", "ask") : Results.BadRequest();
        });

        forms.MapPost("/circle-add", (HttpContext context, AccountDesk desk, [FromForm] string? name, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.AddCircle(Me(context), name ?? string.Empty) is null ? Back(returnUrl, "circles", "circle-name") : Said(returnUrl, "circle", "circles");
        });

        forms.MapPost("/circle-set", async (HttpContext context, AccountDesk desk, [FromForm] long? circle, [FromForm] string? returnUrl) =>
        {
            // One field "member" for each box that is ticked; none when nobody is.
            var members = (await context.Request.ReadFormAsync().ConfigureAwait(false))["member"];
            using var store = desk.Open();
            return circle is { } number && store.SetCircle(Me(context), number, members.OfType<string>()) ? Said(returnUrl, "circle-set", "circles") : Results.BadRequest();
        });

        forms.MapPost("/circle-remove", (HttpContext context, AccountDesk desk, [FromForm] long? circle, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return circle is { } number && store.RemoveCircle(Me(context), number) ? Said(returnUrl, "circle-removed", "circles") : Results.BadRequest();
        });
    }

    /// <summary>The ending of the value of a kind with a direction that says: the account that asks holds the role.</summary>
    public const string Mine = "-mine";

    /// <summary>
    /// Returns what a form can call the other side: every kind once, and a kind with a direction twice – "they
    /// are my parent" and "I am their parent", which a page writes as what the other is: parent, child.
    /// </summary>
    public static IEnumerable<(string Value, string Kind, bool OtherHolds)> Choices() =>
        RelationshipCatalogue.All.SelectMany(kind => kind.Directed
            ? new[] { (kind.Key, kind.Key, true), (kind.Key + Mine, kind.Key, false) }
            : [(kind.Key, kind.Key, false)]);

    /// <summary>Returns the value of a form for a relationship as one side reads it.</summary>
    public static string Choice(string kind, RelationshipTerms terms)
    {
        ArgumentNullException.ThrowIfNull(terms);
        return RelationshipCatalogue.Find(kind) is { Directed: true } && !terms.OtherHolds ? kind + Mine : kind;
    }

    /// <summary>
    /// Returns what the other side of a relationship is, in the words of a page: "colleague", "parent" or –
    /// read from the side that holds the role – "child".
    /// </summary>
    public static string Called(string kind, RelationshipTerms terms, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(text);
        return text[$"People_Kind_{Choice(kind, terms)}"];
    }

    /// <summary>
    /// Returns where and when a relationship holds, as a page writes it after the kind: "The Zabol Press,
    /// 2010 to 2018" – and nothing when a relationship says neither.
    /// </summary>
    /// <param name="terms">What the two state.</param>
    /// <param name="text">The words of the interface.</param>
    /// <param name="visitor">Who reads.</param>
    /// <param name="places">
    /// The places of the collections. With them, a relationship that points at an entry is written with what
    /// the entry is called today (step 20.4); without them, and while the entry is not published, with the
    /// words the two wrote.
    /// </param>
    public static string Held(RelationshipTerms terms, IStringLocalizer<Ui> text, Visitor visitor, Institutions? places = null)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(visitor);
        var where = places?.Find(terms.Place) is { } place ? visitor.Text(place.Entity.Label).Value : terms.Context;
        var (since, until) = (TreeEndpoints.Short(terms.Since, visitor, text), TreeEndpoints.Short(terms.Until, visitor, text));
        var time = (since, until) switch
        {
            ({ } from, { } to) => string.Format(CultureInfo.CurrentCulture, text["People_Held_Span"], from, to),
            ({ } from, null) => string.Format(CultureInfo.CurrentCulture, text["People_Held_Since"], from),
            (null, { } to) => string.Format(CultureInfo.CurrentCulture, text["People_Held_Until"], to),
            _ => null,
        };
        return string.Join(text["People_Held_And"], new[] { where, time }.Where(part => part is not null));
    }

    // What a form says about a relationship: the kind – with its direction, for a kind that has one – where it
    // holds, and from when until when. Nothing when a time is none.
    private static (string Kind, RelationshipTerms Terms)? Terms(IFormCollection form, string? kind, string? place, Institutions places, EntityReference? kept)
    {
        var chosen = kind ?? string.Empty;
        var mine = chosen.EndsWith(Mine, StringComparison.Ordinal);

        // The entry of a collection the place is, when the form names one (step 20.4): one the page offered.
        return TreeEndpoints.Time(form, "since", out var since) && TreeEndpoints.Time(form, "until", out var until)
            && AffiliationEndpoints.Chosen(form, places, kept, out var entry)
            ? (mine ? chosen[..^Mine.Length] : chosen, new RelationshipTerms(!mine, place, since, until, entry))
            : null;
    }

    /// <summary>Reads an audience as a form names it: <c>private</c>, <c>public</c>, or <c>circle-</c> and the number of a circle.</summary>
    internal static (ScopeKind Kind, long? Circle)? Scope(string? scope) => scope switch
    {
        "private" => (ScopeKind.Private, null),
        "public" => (ScopeKind.Public, null),
        _ when scope is not null && scope.StartsWith("circle-", StringComparison.Ordinal) && long.TryParse(scope.AsSpan(7), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var circle) => (ScopeKind.Circle, circle),
        _ => null,
    };

    // The filter of the group has seen that somebody signed in.
    private static string Me(HttpContext context) => AccountDesk.Session(context.User)!.Id;

    // The page of the relationships in the language the form came from, and nowhere else.
    private static string Home(string? returnUrl)
    {
        var address = ReturnAddress.Safe(returnUrl, Page);
        var end = address.IndexOfAny(['?', '#']);
        address = end < 0 ? address : address[..end];
        return address.EndsWith(Page, StringComparison.Ordinal) && address.Length <= Page.Length + 12 ? address : Page;
    }

    private static IResult Said(string? returnUrl, string what, string? at = null) =>
        Results.Redirect($"{Home(returnUrl)}?said={what}{(at is null ? string.Empty : "#" + at)}");

    private static IResult Back(string? returnUrl, string form, string problem) =>
        Results.Redirect($"{Home(returnUrl)}?form={form}&problem={problem}#{form}");
}
