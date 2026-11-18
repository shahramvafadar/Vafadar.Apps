using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Vafadar.Dastan.People;
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

        forms.MapPost("/person-add", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? name, [FromForm] string? born, [FromForm] string? died, [FromForm] string? deceased, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return Year(born, out var from) && Year(died, out var until) && store.AddPerson(Me(context), tree ?? string.Empty, name ?? string.Empty, from, until, deceased is not null) is not null
                ? Said(returnUrl, "person-added", "people")
                : Back(returnUrl, "person-add", "person");
        });

        forms.MapPost("/person-change", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? name, [FromForm] string? born, [FromForm] string? died, [FromForm] string? deceased, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return Year(born, out var from) && Year(died, out var until) && store.ChangePerson(Me(context), tree ?? string.Empty, person ?? string.Empty, name ?? string.Empty, from, until, deceased is not null)
                ? Said(returnUrl, "person-changed", "people")
                : Back(returnUrl, "people", "person");
        });

        forms.MapPost("/person-remove", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? person, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.RemovePerson(Me(context), tree ?? string.Empty, person ?? string.Empty) ? Said(returnUrl, "person-removed", "people") : Results.BadRequest();
        });

        forms.MapPost("/link", (HttpContext context, AccountDesk desk, [FromForm] string? tree, [FromForm] string? kind, [FromForm] string? from, [FromForm] string? to, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.Link(Me(context), tree ?? string.Empty, kind ?? string.Empty, from ?? string.Empty, to ?? string.Empty) switch
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
    }

    // The filter of the group has seen that somebody signed in.
    private static string Me(HttpContext context) => AccountDesk.Session(context.User)!.Id;

    // An empty field is a year that is not known; anything else has to be a number.
    private static bool Year(string? written, out int? year)
    {
        year = null;
        if (string.IsNullOrWhiteSpace(written))
        {
            return true;
        }

        if (!int.TryParse(Vafadar.Core.Text.Digits.ToAscii(written.Trim()), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        year = number;
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

    [GeneratedRegex("^((?:/[a-z]{2})?/account/trees)(?:/[a-f0-9]{32})?$")]
    private static partial Regex Pages();
}
