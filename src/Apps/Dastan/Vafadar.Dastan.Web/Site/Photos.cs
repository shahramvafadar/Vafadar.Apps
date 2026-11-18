using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The photographs of the visitors on the site (step 16.5): bringing one in, who may see it, who it shows – and
/// the one address a file is given by, which asks the audience of the photograph like every page does. The
/// files lie in a folder of their own (<c>Dastan:Photos</c>) that no web server shows; without it the site has
/// no photographs.
/// </summary>
public static class PhotoEndpoints
{
    /// <summary>The address of the page of the photographs, without the language.</summary>
    public const string Page = "/account/photos";

    /// <summary>The beginning of the address of the file of a photograph. It carries no language.</summary>
    public const string File = "/photo";

    /// <summary>The address the forms are sent to. It has no language.</summary>
    public const string Root = "/preferences/photos";

    /// <summary>The largest photograph, in bytes.</summary>
    public const long Limit = 10L * 1024 * 1024;

    /// <summary>Maps the forms and the address of a file.</summary>
    public static void MapPhotos(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // The file of a photograph: given to who its owner lets see it, and to nobody else – a photograph that
        // is not for the viewer and one that does not exist answer alike.
        endpoints.MapGet(File + "/{id}", (HttpContext context, AccountDesk desk, string id) =>
        {
            context.Response.Headers.CacheControl = "private, no-cache";
            context.Response.Headers.Vary = "Cookie";
            PhotoInfo? photo = null;
            if (desk.PhotoFolder is not null && id.Length <= 40)
            {
                using var store = desk.Open();
                photo = store.Photo(AccountDesk.Session(context.User)?.Id, id);
            }

            if (photo is null || desk.PhotoPath(photo.File.Name) is not { } path || !System.IO.File.Exists(path))
            {
                // A file is asked for by a page, not by a reader: the answer is the status and no page.
                if (context.Features.Get<IStatusCodePagesFeature>() is { } pages)
                {
                    pages.Enabled = false;
                }

                return Results.NotFound();
            }

            return Results.File(path, photo.File.ContentType);
        });

        var forms = endpoints.MapGroup(Root).AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            return context.RequestServices.GetRequiredService<AccountDesk>().PhotoFolder is null ? Results.NotFound()
                : AccountDesk.Session(context.User) is null ? Results.BadRequest()
                : await next(invocation).ConfigureAwait(false);
        });

        forms.MapPost("/add", (HttpContext context, AccountDesk desk, IFormFile? file, [FromForm] string? caption, [FromForm] string? taken, [FromForm] string? scope, [FromForm] string? returnUrl) =>
        {
            var day = Day(taken, out var known);
            using var store = desk.Open();
            if (file is null || file.Length == 0 || PeopleEndpoints.Scope(scope) is not { } shown || !known)
            {
                return Back(returnUrl, "add", "photo-form");
            }

            if (file.Length > Limit)
            {
                return Back(returnUrl, "add", "photo-large");
            }

            if (!store.CanAddPhoto(Me(context)))
            {
                return Back(returnUrl, "add", "photo-many");
            }

            // What the file is is read from its content, and what an image says about where and with which
            // device it was made is taken out before it is kept (MediaIntake). Its name is made here.
            var id = Guid.NewGuid().ToString("N");
            using var content = file.OpenReadStream();
            var (taken1, _) = MediaIntake.Take(Path.Combine(desk.PhotoFolder!, "in"), id, "file", null, content);
            if (taken1 is not { Kind: MediaFileKind.Image, Width: { } width, Height: { } height })
            {
                if (taken1 is not null)
                {
                    MediaIntake.Discard(taken1);
                }

                return Back(returnUrl, "add", "photo-file");
            }

            if (!store.AddPhoto(Me(context), id, new PhotoFile(Path.GetFileName(taken1.Path), taken1.ContentType, width, height, taken1.Length), caption ?? string.Empty, day, shown.Kind, shown.Circle))
            {
                MediaIntake.Discard(taken1);
                return Back(returnUrl, "add", "photo-form");
            }

            return Said(returnUrl, "added", "mine");
        });

        forms.MapPost("/share", (HttpContext context, AccountDesk desk, [FromForm] string? photo, [FromForm] string? scope, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return PeopleEndpoints.Scope(scope) is { } shown && store.SharePhoto(Me(context), photo ?? string.Empty, shown.Kind, shown.Circle)
                ? Said(returnUrl, "shared", "mine")
                : Results.BadRequest();
        });

        forms.MapPost("/remove", (HttpContext context, AccountDesk desk, [FromForm] string? photo, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            if (store.RemovePhoto(Me(context), photo ?? string.Empty) is not { } name)
            {
                return Results.BadRequest();
            }

            if (desk.PhotoPath(name) is { } path && System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            return Said(returnUrl, "removed", "mine");
        });

        forms.MapPost("/tag", (HttpContext context, AccountDesk desk, [FromForm] string? photo, [FromForm] string? account, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.TagPhoto(Me(context), photo ?? string.Empty, account ?? string.Empty) ? Said(returnUrl, "tagged", "mine") : Results.BadRequest();
        });

        forms.MapPost("/untag", (HttpContext context, AccountDesk desk, [FromForm] string? photo, [FromForm] string? account, [FromForm] string? returnUrl) =>
        {
            using var store = desk.Open();
            return store.UntagPhoto(Me(context), photo ?? string.Empty, account ?? string.Empty) ? Said(returnUrl, "untagged", "mine") : Results.BadRequest();
        });

        // A person a photograph names agrees that it is shown to everybody, or takes that back (D-053, step 16.9).
        forms.MapPost("/agree", (HttpContext context, AccountDesk desk, [FromForm] string? photo, [FromForm] string? answer, [FromForm] string? returnUrl) =>
        {
            if (answer is not ("agree" or "back"))
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.AgreeToEverybody(Me(context), photo ?? string.Empty, answer == "agree")
                ? Said(returnUrl, answer == "agree" ? "agreed" : "agreement-back", "asked")
                : Results.BadRequest();
        });

        forms.MapPost("/answer", (HttpContext context, AccountDesk desk, [FromForm] string? photo, [FromForm] string? answer, [FromForm] string? returnUrl) =>
        {
            if (answer is not ("approve" or "remove"))
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.AnswerTag(Me(context), photo ?? string.Empty, answer == "approve")
                ? Said(returnUrl, answer == "approve" ? "approved" : "tag-removed")
                : Results.BadRequest();
        });
    }

    // The filter of the group has seen that somebody signed in.
    private static string Me(HttpContext context) => AccountDesk.Session(context.User)!.Id;

    // An empty field is a day that is not said; anything else has to be a day of the calendar.
    private static DateOnly? Day(string? written, out bool known)
    {
        known = true;
        if (string.IsNullOrWhiteSpace(written))
        {
            return null;
        }

        known = DateOnly.TryParseExact(Vafadar.Core.Text.Digits.ToAscii(written.Trim()), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day);
        return known ? day : null;
    }

    // The page of the photographs in the language the form came from, and nowhere else.
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
