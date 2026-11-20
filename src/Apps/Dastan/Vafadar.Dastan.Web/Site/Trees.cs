using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Resources;
using Vafadar.Temporal;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The forms of the family trees of a visitor (step 16.4, D-054). Every one is for who signed in; what that
/// account may do with a tree – see it, change it, own it – is decided by the store.
/// </summary>
public static partial class TreeEndpoints
{
    /// <summary>The address of the page of the trees, without the language. The page of one tree is below it.</summary>
    public const string Page = "/account/trees";

    /// <summary>The address the forms are sent to. It has no language.</summary>
    public const string Root = "/preferences/trees";

    /// <summary>The address of a tree its owner made public, without the language and the identifier.</summary>
    public const string Public = "/trees";

    /// <summary>
    /// Returns where the people of a tree stand in its drawing, or nothing for a tree without people.
    /// Generations follow from the links: parents above their children, partners in one row.
    /// </summary>
    public static TreePlacement? Placement(TreeView tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        if (tree.People.IsEmpty)
        {
            return null;
        }

        var parentage = tree.Links.Where(link => PeopleStore.IsParentage(link.Kind)).Select(link => new TreeShapeLink(link.From, link.To)).ToImmutableArray();
        var partnerships = tree.Links.Where(link => link.Kind == "partner").Select(link => new TreeShapeLink(link.From, link.To)).ToImmutableArray();
        var rows = TreeShape.Generations(tree.People.Select(person => person.Id), parentage, partnerships);
        return TreeShapeLayout.Of(new TreeShape([.. tree.People.Select(person => new TreeShapeMember(person.Id, rows[person.Id], person.Name))], parentage, partnerships));
    }

    /// <summary>The fields of a form that state one time of a record: what stands in them for a time that is kept.</summary>
    /// <param name="Year">The year, or the earliest year of a window.</param>
    /// <param name="Month">The month, when the time says one.</param>
    /// <param name="Day">The day, when the time says one.</param>
    /// <param name="Latest">The latest year, for a time known only to lie between two years.</param>
    /// <param name="Calendar">The calendar: <c>gregorian</c> or <c>solar-hijri</c>.</param>
    /// <param name="About">Whether the time is about, not exact.</param>
    /// <param name="Unknown">Whether the record says that the time is not known.</param>
    public sealed record TimeFields(int? Year, int? Month, int? Day, int? Latest, string Calendar, bool About, bool Unknown)
    {
        /// <summary>Gets a value indicating whether the time says more than a plain year of the calendar a new record starts with.</summary>
        public bool Finer(string calendar) => Month is not null || Latest is not null || About || Unknown || (Year is not null && Calendar != calendar);
    }

    /// <summary>Returns the calendar a new record starts with in a language: the one its readers count their years in.</summary>
    public static string CalendarOf(string language) => language == "fa" ? "solar-hijri" : "gregorian";

    /// <summary>Returns what stands in the fields of a form for a time of a record.</summary>
    public static TimeFields Fields(TemporalExpression? time, string calendar)
    {
        static string Named(CalendarPosition at) => at.Calendar == CalendarSystem.SolarHijri ? "solar-hijri" : "gregorian";
        return time switch
        {
            Occurrence { At: CalendarPosition at } => new(at.Year, at.Month, at.Day, null, Named(at), at.IsApproximate, false),
            OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => new(earliest.Year, null, null, latest.Year, Named(earliest), earliest.IsApproximate || latest.IsApproximate, false),
            UnknownTime => new(null, null, null, null, calendar, false, true),
            _ => new(null, null, null, null, calendar, false, false),
        };
    }

    /// <summary>
    /// Returns a time of a record as a line of a list writes it: the bare year for a year of the common era,
    /// the words of the time otherwise; nothing for a time that is not stated or not known.
    /// </summary>
    public static string? Short(TemporalExpression? time, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        return time switch
        {
            null or UnknownTime => null,
            Occurrence { At: CalendarPosition { Era: Era.CommonEra, IsApproximate: false } at } => visitor.Number(at.Year),
            _ => TimeText.Of(time, visitor, text),
        };
    }

    /// <summary>Returns the years of a record as a page writes them: "1300–1380", "born 1300", "died 1380", "deceased" – whatever is known, and nothing when nothing is.</summary>
    public static string Years(TreePerson person, IStringLocalizer<Ui> text, Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(visitor);
        return (Short(person.Born, visitor, text), Short(person.Died, visitor, text), person.Deceased) switch
        {
            ({ } born, { } died, _) => $"{born}–{died}",
            ({ } born, null, false) => string.Format(CultureInfo.CurrentCulture, text["Tree_Born"], born),
            ({ } born, null, true) => $"{string.Format(CultureInfo.CurrentCulture, text["Tree_Born"], born)}{text["Tree_And"]}{text["Tree_Deceased"]}",
            (null, { } died, _) => string.Format(CultureInfo.CurrentCulture, text["Tree_Died"], died),
            (null, null, true) => text["Tree_Deceased"],
            _ => string.Empty,
        };
    }

    /// <summary>Maps the forms. Each sends the visitor back to the page it came from, which says what happened.</summary>
    public static void MapTrees(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var forms = endpoints.MapGroup(Root).AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            return !context.RequestServices.GetRequiredService<AccountDesk>().IsOpen ? Results.NotFound()
                : AccountDesk.Session(context.User) is null ? Results.BadRequest()
                : await next(invocation).ConfigureAwait(false);
        });

        forms.MapPost("/add", (HttpContext context, AccountDesk desk, [FromForm] string? name, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.AddTree(Me(context), name ?? string.Empty) is { } tree
                ? Results.Redirect($"{Home(returnUrl, list: true)}/{tree}?said=made")
                : Back(returnUrl, "add", "tree-name");
        });

        forms.MapPost("/rename", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? name, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.RenameTree(Me(context), tree ?? string.Empty, name ?? string.Empty) ? Said(returnUrl, "renamed") : Back(returnUrl, "rename", "tree-name");
        });

        forms.MapPost("/remove", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.RemoveTree(Me(context), tree ?? string.Empty) ? Results.Redirect($"{Home(returnUrl, list: true)}?said=removed") : Results.BadRequest();
        });

        forms.MapPost("/person-add", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? name, [FromForm] string? deceased, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return Time(context.Request.Form, "born", out var from) && Time(context.Request.Form, "died", out var until) && store.AddPerson(Me(context), tree ?? string.Empty, name ?? string.Empty, from, until, deceased is not null) is not null
                ? Said(returnUrl, "person-added", "people")
                : Back(returnUrl, "person-add", "person");
        });

        forms.MapPost("/person-change", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? name, [FromForm] string? deceased, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return Time(context.Request.Form, "born", out var from) && Time(context.Request.Form, "died", out var until) && store.ChangePerson(Me(context), tree ?? string.Empty, person ?? string.Empty, name ?? string.Empty, from, until, deceased is not null)
                ? Said(returnUrl, "person-changed", "people")
                : Back(returnUrl, "people", "person");
        });

        // What is said of a person beyond the name and the years (step 19.5, PRIV-003): the store decides who may.
        forms.MapPost("/person-describe", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? bornAt, [FromForm] string? diedAt, [FromForm] string? about, [FromForm] string? picture, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.DescribePerson(Me(context), tree ?? string.Empty, person ?? string.Empty, bornAt, diedAt, about, picture) switch
            {
                "done" => Said(returnUrl, "described", "profile"),
                "refused" => Results.BadRequest(),
                var problem => Back(returnUrl, "describe", problem),
            };
        });

        forms.MapPost("/person-remove", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.RemovePerson(Me(context), tree ?? string.Empty, person ?? string.Empty) ? Said(returnUrl, "person-removed", "people") : Results.BadRequest();
        });

        forms.MapPost("/link", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? kind, [FromForm] string? from, [FromForm] string? to, [FromForm] string? returnUrl) =>
        {
            if (!Time(context.Request.Form, "since", out var since) || !Time(context.Request.Form, "until", out var until))
            {
                return Back(returnUrl, "links", "link-time");
            }

            using var store = desk.Open();
            return store.Link(Me(context), tree ?? string.Empty, kind ?? string.Empty, from ?? string.Empty, to ?? string.Empty, since, until) switch
            {
                "done" => Said(returnUrl, "linked", "links"),
                "refused" => Results.BadRequest(),
                var problem => Back(returnUrl, "links", $"link-{problem}"),
            };
        });

        forms.MapPost("/unlink", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] long? link, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return link is { } number && store.Unlink(Me(context), tree ?? string.Empty, number) ? Said(returnUrl, "unlinked", "links") : Results.BadRequest();
        });

        forms.MapPost("/member", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? account, [FromForm] string? role, [FromForm] string? returnUrl) =>
        {
            TreeRole? chosen = role switch { "viewer" => TreeRole.Viewer, "editor" => TreeRole.Editor, "none" => TreeRole.None, _ => null };
            using var store = desk.Open();
            return chosen is { } named && store.SetTreeMember(Me(context), tree ?? string.Empty, account ?? string.Empty, named) ? Said(returnUrl, "member-set", "members") : Results.BadRequest();
        });

        MapClaims(forms);
        MapPublication(forms);
    }

    // Making a tree public (D-053): asked and done by its owner, agreed to by the accounts it names.
    private static void MapPublication(RouteGroupBuilder forms)
    {
        forms.MapPost("/publication", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? act, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            var (me, which) = (Me(context), tree ?? string.Empty);
            return act switch
            {
                "ask" when store.AskPublication(me, which) => Said(returnUrl, "publication-asked", "publication"),
                "publish" when store.Publish(me, which) => Said(returnUrl, "published", "publication"),
                "take-back" when store.Unpublish(me, which) => Said(returnUrl, "unpublished", "publication"),
                _ => Results.BadRequest(),
            };
        });

        forms.MapPost("/consent", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? answer, [FromForm] string? returnUrl) =>
        {
            if (answer is not ("agree" or "take-back"))
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Consent(Me(context), tree ?? string.Empty, answer == "agree")
                ? Said(returnUrl, answer == "agree" ? "consent-given" : "consent-withdrawn", "consent")
                : Results.BadRequest();
        });
    }

    // "This record is me" (D-052): asked by an account, answered by who made the record, taken back by the account.
    private static void MapClaims(RouteGroupBuilder forms)
    {
        forms.MapPost("/claim", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.Claim(Me(context), tree ?? string.Empty, person ?? string.Empty) switch
            {
                "claimed" => Said(returnUrl, "claimed", "people"),
                "refused" or "unknown" => Results.BadRequest(),
                var problem => Back(returnUrl, "people", $"claim-{problem}"),
            };
        });

        forms.MapPost("/claim-review", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] long? claim, [FromForm] string? answer, [FromForm] string? returnUrl) =>
        {
            if (answer is not ("confirm" or "refuse") || claim is not { } number)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.ReviewClaim(Me(context), tree ?? string.Empty, number, answer == "confirm")
                ? Said(returnUrl, answer == "confirm" ? "claim-confirmed" : "claim-refused", "claims")
                : Results.BadRequest();
        });

        forms.MapPost("/unclaim", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.Unclaim(Me(context), tree ?? string.Empty, person ?? string.Empty) ? Said(returnUrl, "unclaimed", "people") : Results.BadRequest();
        });
    }

    // The filter of the group has seen that somebody signed in.
    private static string Me(HttpContext context) => AccountDesk.Session(context.User)!.Id;

    // The time a form states in the fields of one name: a year, with a month and a day where they are known, in
    // the calendar that was chosen, exact or about – or a year and a latest year for a time that is only known
    // to lie between two – or the statement that it is not known. Nothing in the fields is a time nobody stated;
    // fields that contradict each other are refused rather than read one way or the other.
    internal static bool Time(IFormCollection form, string field, out TemporalExpression? time)
    {
        time = null;
        string? Of(string part) => form[part.Length == 0 ? field : $"{field}-{part}"].ToString() is { } written && !string.IsNullOrWhiteSpace(written) ? written.Trim() : null;
        if (!Number(Of(string.Empty), 9999, out var year) || !Number(Of("month"), 12, out var month) || !Number(Of("day"), 31, out var day) || !Number(Of("latest"), 9999, out var latest))
        {
            return false;
        }

        var unknown = Of("unknown") is not null;
        if (year is not { } from)
        {
            time = unknown ? new UnknownTime() : null;
            return month is null && day is null && latest is null;
        }

        if (unknown || (day is not null && month is null) || (latest is not null && month is not null))
        {
            return false;
        }

        // A form with several times can ask for their calendar once, in a field of that name.
        var (calendar, era) = (Of("calendar") ?? form["calendar"].ToString()) == "solar-hijri" ? (CalendarSystem.SolarHijri, Era.SolarHijri) : (CalendarSystem.Gregorian, Era.CommonEra);
        var about = Of("about") is not null;
        CalendarPosition As(CalendarPosition position) => about ? position.Approximately() : position;
        try
        {
            var at = As((month, day) switch
            {
                ({ } inMonth, { } onDay) => CalendarPosition.OfDay(calendar, era, from, inMonth, onDay),
                ({ } inMonth, null) => CalendarPosition.OfMonth(calendar, era, from, inMonth),
                _ => CalendarPosition.OfYear(calendar, era, from),
            });
            time = latest is { } until ? new OccurrenceWindow(at, As(CalendarPosition.OfYear(calendar, era, until))) : new Occurrence(at);
            return true;
        }
        catch (ArgumentException)
        {
            // A day the month does not have, or a latest year before the first.
            return false;
        }
    }

    // An empty field is a number that is not stated; anything else has to be a number from one to the most.
    private static bool Number(string? written, int most, out int? number)
    {
        number = null;
        if (written is null)
        {
            return true;
        }

        if (!int.TryParse(Vafadar.Core.Text.Digits.ToAscii(written), NumberStyles.None, CultureInfo.InvariantCulture, out var read) || read < 1 || read > most)
        {
            return false;
        }

        number = read;
        return true;
    }

    // The page a form came from – the list of the trees or one tree, in its language – and nowhere else.
    private static string Home(string? returnUrl, bool list = false)
    {
        var address = ReturnAddress.Safe(returnUrl, Page);
        var end = address.IndexOfAny(['?', '#']);
        address = end < 0 ? address : address[..end];
        var match = Pages().Match(address);
        return !match.Success ? Page : list ? match.Groups[1].Value : address;
    }

    private static IResult Said(string? returnUrl, string what, string? at = null) =>
        Results.Redirect($"{Home(returnUrl)}?said={what}{(at is null ? string.Empty : "#" + at)}");

    private static IResult Back(string? returnUrl, string form, string problem) =>
        Results.Redirect($"{Home(returnUrl)}?form={form}&problem={problem}#{form}");

    [GeneratedRegex("^((?:/[a-z]{2})?/account/trees)(?:/[a-f0-9]{32}(?:/person/[a-f0-9]{32})?)?$")]
    private static partial Regex Pages();
}
