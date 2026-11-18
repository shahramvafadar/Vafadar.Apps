using Microsoft.AspNetCore.Mvc;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.People;
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

        forms.MapPost("/ask", (HttpContext context, AccountDesk desk, [FromForm] string? name, [FromForm] string? kind, [FromForm] string? returnUrl) =>
        {
            // Asking by name could be used to try names: an address asks only so often.
            if (!desk.Allows(context, "ask", AccountDesk.AttemptsPerHour))
            {
                return Back(returnUrl, "ask", "later");
            }

            using var store = desk.Open();
            var outcome = store.Ask(Me(context), name ?? string.Empty, kind ?? string.Empty);
            return outcome == "asked" ? Said(returnUrl, "asked") : Back(returnUrl, "ask", outcome);
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
            // "private", "public" or "circle-" and the number of a circle of this account.
            (ScopeKind Kind, long? Circle)? chosen = scope switch
            {
                "private" => (ScopeKind.Private, null),
                "public" => (ScopeKind.Public, null),
                _ when scope is not null && scope.StartsWith("circle-", StringComparison.Ordinal) && long.TryParse(scope.AsSpan(7), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var circle) => (ScopeKind.Circle, circle),
                _ => null,
            };
            if (chosen is null)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Show(Me(context), relationship ?? string.Empty, chosen.Value.Kind, chosen.Value.Circle) ? Said(returnUrl, "shown") : Results.BadRequest();
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
