using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Resources;
using Vafadar.Temporal;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The forms of the affiliations of a visitor (step 19.6, ADR 0018, PRIV-010): where they were, what they were
/// there and when, and who may see that and find them through the place. Every one is for who signed in and
/// acts for that account only.
/// </summary>
public static class AffiliationEndpoints
{
    /// <summary>The address of the page of the affiliations, without the language.</summary>
    public const string Page = "/account/affiliations";

    /// <summary>The address the forms are sent to. It has no language.</summary>
    public const string Root = "/preferences/affiliations";

    /// <summary>What a form names as the place when it is none of the entries it was offered: the words stand alone.</summary>
    public const string OnlyWords = "none";

    /// <summary>How often an address may ask who was at a place, in an hour: a search is no way to try every place.</summary>
    public const int SearchesPerHour = 60;

    /// <summary>
    /// Returns when somebody was at a place, as a page writes it: "1998 to 2002, graduated 2002" – and nothing
    /// when an affiliation says no time.
    /// </summary>
    public static string When(AffiliationTerms terms, IStringLocalizer<Ui> text, Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(text);
        var held = PeopleEndpoints.Held(new RelationshipTerms(Since: terms.Since, Until: terms.Until), text, visitor);
        var graduated = TreeEndpoints.Short(terms.Graduated, visitor, text) is { } year ? string.Format(CultureInfo.CurrentCulture, text["Affiliations_Graduated"], year) : null;
        return string.Join(text["People_Held_And"], new[] { held.Length > 0 ? held : null, graduated }.Where(part => part is not null));
    }

    /// <summary>Reads the year a search asks about, in the calendar it names; nothing for a year that is none.</summary>
    public static YearOfEra? Year(string? written, string? calendar) =>
        int.TryParse(Vafadar.Core.Text.Digits.ToAscii((written ?? string.Empty).Trim()), NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is >= 1 and <= 9999
            ? new YearOfEra(calendar == "solar-hijri" ? Era.SolarHijri : Era.CommonEra, year)
            : null;

    /// <summary>Maps the forms. Each sends the visitor back to the page of the affiliations, which says what happened.</summary>
    public static void MapAffiliations(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var forms = endpoints.MapGroup(Root).AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            return !context.RequestServices.GetRequiredService<AccountDesk>().IsOpen ? Results.NotFound()
                : AccountDesk.Session(context.User) is null ? Results.BadRequest()
                : await next(invocation).ConfigureAwait(false);
        });

        forms.MapPost("/add", (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] string? place, [FromForm] string? role, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return Terms(context.Request.Form, place, role, library.Institutions, kept: null) is { } terms && store.AddAffiliation(Me(context), terms) is not null
                ? Said(returnUrl, "added", "mine")
                : Back(returnUrl, "add", "affiliation");
        });

        forms.MapPost("/change", (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] long? affiliation, [FromForm] string? place, [FromForm] string? role, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();

            // An affiliation may keep the entry it points at, also while that entry is not published.
            var kept = store.Affiliations(Me(context)).FirstOrDefault(mine => mine.Number == affiliation)?.Terms.Institution;
            return affiliation is { } number && Terms(context.Request.Form, place, role, library.Institutions, kept) is { } terms && store.ChangeAffiliation(Me(context), number, terms)
                ? Said(returnUrl, "changed", "mine")
                : Back(returnUrl, "mine", "affiliation");
        });

        forms.MapPost("/show", (HttpContext context, AccountDesk desk, [FromForm] long? affiliation, [FromForm] string? findable, [FromForm] string? dates, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return affiliation is { } number && PeopleEndpoints.Scope(findable) is { } found && PeopleEndpoints.Scope(dates) is { } when
                && store.ShowAffiliation(Me(context), number, found.Kind, found.Circle, when.Kind, when.Circle)
                ? Said(returnUrl, "shown", "mine")
                : Results.BadRequest();
        });

        // A place no collection has an entry for is proposed to the editors (D-085). What is kept is the place
        // in the visitor's words: who proposed it is not, so nothing here asks the store of the accounts.
        forms.MapPost("/propose", (HttpContext context, AccountDesk desk, ProposalDesk proposals, Visitor visitor,
            [FromForm] string? name, [FromForm] string? kind, [FromForm] string? where, [FromForm] string? page, [FromForm] string? note, [FromForm] string? returnUrl) =>
        {
            if (!proposals.IsOpen)
            {
                return Results.NotFound();
            }

            if (ProposalBox.Read(new ProposedPlace(name ?? string.Empty, kind ?? string.Empty, where, page, note), [.. ProposalDesk.Kinds]) is not { } place)
            {
                // The name goes back with the visitor, so that the form does not begin empty again.
                var typed = name is { Length: > 0 and <= PeopleStore.LongestOrganisation } && !name.Any(char.IsControl) ? $"&at={Uri.EscapeDataString(name.Trim())}" : null;
                return Results.Redirect($"{Home(returnUrl)}?form=propose&problem=proposal{typed}#propose");
            }

            return !desk.Allows(context, "propose", ProposalDesk.PerHour) ? Said(returnUrl, "proposal-later", "propose")
                : proposals.Take(place, visitor.Language.Tag) ? Said(returnUrl, "proposed", "propose")
                : Said(returnUrl, "proposal-full", "propose");
        });

        forms.MapPost("/remove", (HttpContext context, AccountDesk desk, [FromForm] long? affiliation, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return affiliation is { } number && store.RemoveAffiliation(Me(context), number) ? Said(returnUrl, "removed", "mine") : Results.BadRequest();
        });
    }

    // The filter of the group has seen that somebody signed in.
    private static string Me(HttpContext context) => AccountDesk.Session(context.User)!.Id;

    // What a form says about an affiliation; nothing when a time is none or the entry it names is none.
    private static AffiliationTerms? Terms(IFormCollection form, string? place, string? role, Institutions places, EntityReference? kept) =>
        TreeEndpoints.Time(form, "since", out var since) && TreeEndpoints.Time(form, "until", out var until) && TreeEndpoints.Time(form, "graduated", out var graduated)
            && Chosen(form, places, kept, out var institution)
            ? new AffiliationTerms(place ?? string.Empty, role, since, until, graduated, institution)
            : null;

    /// <summary>
    /// Reads the entry of a collection a form names as the place (step 20.4, ADR 0023): none – the words stand
    /// alone –, a place of the directory, or the entry the statement points at already. Anything else is no
    /// choice the page offered, and the form is refused: a statement points at nothing a visitor made up.
    /// </summary>
    internal static bool Chosen(IFormCollection form, Institutions places, EntityReference? kept, out EntityReference? chosen)
    {
        chosen = null;
        var written = form["institution"].ToString();
        if (written.Length == 0 || written == OnlyWords)
        {
            return true;
        }

        return written.Length <= EntityReference.Longest && EntityReference.TryRead(written, out chosen) && (chosen == kept || places.Find(chosen) is not null);
    }

    // The page of the affiliations in the language the form came from, and nowhere else.
    private static string Home(string? returnUrl)
    {
        var address = ReturnAddress.Safe(returnUrl, Page);
        var end = address.IndexOfAny(['?', '#']);
        address = end < 0 ? address : address[..end];
        return address.EndsWith(Page, StringComparison.Ordinal) && address.Length <= Page.Length + 12 ? address : Page;
    }

    private static IResult Said(string? returnUrl, string what, string at) => Results.Redirect($"{Home(returnUrl)}?said={what}#{at}");

    private static IResult Back(string? returnUrl, string form, string problem) => Results.Redirect($"{Home(returnUrl)}?form={form}&problem={problem}#{form}");
}
