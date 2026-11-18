using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.People;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The accounts of the visitors on the site (step 16.1, D-048 to D-050). The site has accounts only when it was
/// told a file for their store (<c>Dastan:People</c>): a database of its own beside the site, apart from the
/// collections and from the store of the editors. Without it the site is what it was – no account, no page for one.
/// </summary>
public sealed class AccountDesk
{
    /// <summary>The address of the page of the account, without the language.</summary>
    public const string Page = "/account";

    /// <summary>The address the forms of the account are sent to. It has no language.</summary>
    public const string Root = "/preferences/account";

    /// <summary>The name of the cookie that holds a sign-in.</summary>
    public const string SessionCookie = "dastan.visitor.session";

    /// <summary>The name of the cookie that carries new recovery codes to the page that shows them once.</summary>
    public const string CodesCookie = "dastan.visitor.codes";

    /// <summary>How many accounts one address may ask for in an hour.</summary>
    public const int RegistrationsPerHour = 5;

    /// <summary>How many passwords and recovery codes one address may try in an hour, over all accounts.</summary>
    public const int AttemptsPerHour = 30;

    private const string IdClaim = "dastan.visitor.id";
    private const string StampClaim = "dastan.visitor.stamp";

    private readonly string? _file;
    private readonly TimeProvider _time;
    private readonly IDataProtector _codes;

    // Who asked how often in the present hour. It lives in the memory of the process and is thrown away with the
    // hour: an address is never written anywhere.
    private readonly ConcurrentDictionary<string, int> _asked = new(StringComparer.Ordinal);
    private long _hour;
    private long _purgedOn;

    /// <summary>Initializes a new instance of the <see cref="AccountDesk"/> class and brings the store to the form this program knows.</summary>
    public AccountDesk(IOptions<DastanOptions> options, IHostEnvironment environment, TimeProvider time, IDataProtectionProvider protection)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(protection);
        _time = time;
        _codes = protection.CreateProtector("dastan.visitor.codes");
        if (!string.IsNullOrWhiteSpace(options.Value.People))
        {
            _file = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.People));
            using var store = PeopleStore.Open(_file, time);
        }
    }

    /// <summary>Gets a value indicating whether the site has accounts.</summary>
    public bool IsOpen => _file is not null;

    /// <summary>
    /// Opens the store for one request. Once a day, the first request deletes for good the accounts that were
    /// closed long enough ago (D-055): the site needs no other clockwork for it.
    /// </summary>
    public PeopleStore Open()
    {
        var store = PeopleStore.Open(_file ?? throw new InvalidOperationException("The site has no accounts."), _time, migrate: false);
        var day = _time.GetUtcNow().ToUnixTimeSeconds() / 86400;
        if (Interlocked.Exchange(ref _purgedOn, day) != day)
        {
            store.Purge();
        }

        return store;
    }

    /// <summary>Counts a request of a kind for an address and returns whether it is still within what an hour allows.</summary>
    public bool Allows(HttpContext context, string kind, int most)
    {
        ArgumentNullException.ThrowIfNull(context);
        var hour = _time.GetUtcNow().ToUnixTimeSeconds() / 3600;
        if (Interlocked.Exchange(ref _hour, hour) != hour)
        {
            _asked.Clear();
        }

        var sender = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return _asked.AddOrUpdate($"{kind} {sender}", 1, (_, count) => count + 1) <= most;
    }

    /// <summary>
    /// Returns whether a password may travel with this request: it came encrypted, or from this machine. A
    /// request without an address comes from inside the process – the tests of the site.
    /// </summary>
    public static bool IsPrivate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Request.IsHttps || context.Connection.RemoteIpAddress is not { } address || IPAddress.IsLoopback(address);
    }

    /// <summary>Returns the sign-in a request carries, or nothing.</summary>
    public static VisitorSession? Session(ClaimsPrincipal? user) =>
        user?.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name }
        && user.FindFirstValue(IdClaim) is { } id
        && user.FindFirstValue(StampClaim) is { } stamp
            ? new VisitorSession(id, name, stamp)
            : null;

    /// <summary>Signs a visitor in for the browser of the request.</summary>
    public static Task SignIn(HttpContext context, VisitorSession session)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(session);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, session.Name), new Claim(IdClaim, session.Id), new Claim(StampClaim, session.Stamp)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    /// <summary>
    /// Hands new recovery codes to the page that shows them: sealed in a cookie that only this site can read and
    /// that lives a few minutes. The page takes it away when it has shown them.
    /// </summary>
    public void Carry(HttpContext context, ImmutableArray<string> codes)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = PreferenceCookie.Options(context);
        options.MaxAge = TimeSpan.FromMinutes(5);
        context.Response.Cookies.Append(CodesCookie, _codes.Protect(string.Join(' ', codes)), options);
    }

    /// <summary>Returns the recovery codes a request carries, and takes them away: they are shown once.</summary>
    public ImmutableArray<string> Shown(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Cookies[CodesCookie] is not { Length: > 0 } carried)
        {
            return [];
        }

        context.Response.Cookies.Delete(CodesCookie, PreferenceCookie.Options(context));
        try
        {
            return [.. _codes.Unprotect(carried).Split(' ', StringSplitOptions.RemoveEmptyEntries)];
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Not sealed by this site, or sealed before its keys changed.
            return [];
        }
    }
}

/// <summary>
/// The account of one request (step 16.2): who signed in and what follows them. While a visitor is signed in,
/// their reading position and their list are those of the account – the same on every device; what the device
/// holds itself is left as it is and counts again when they sign out. Read from the store once, when first asked.
/// </summary>
public sealed class AccountState
{
    private readonly AccountDesk _desk;
    private readonly HttpContext _context;
    private Belongings? _belongings;

    /// <summary>Initializes a new instance of the <see cref="AccountState"/> class from the current request.</summary>
    public AccountState(IHttpContextAccessor accessor, AccountDesk desk)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _context = accessor.HttpContext ?? throw new InvalidOperationException("An account is asked for during a request only.");
        _desk = desk;
    }

    /// <summary>Gets who signed in, or nothing.</summary>
    public VisitorSession? Session => _desk.IsOpen ? AccountDesk.Session(_context.User) : null;

    /// <summary>Gets what follows the visitor who signed in, or nothing when nobody did.</summary>
    public Belongings? Belongings
    {
        get
        {
            if (_belongings is null && Session is { } session)
            {
                using var store = _desk.Open();
                _belongings = store.Belongings(session.Id);
            }

            return _belongings;
        }
    }

    /// <summary>Returns the account of a request.</summary>
    public static AccountState Of(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.RequestServices.GetRequiredService<AccountState>();
    }

    /// <summary>
    /// Returns what the device of a request holds itself, whoever is signed in: the reading positions it chose
    /// for the collections of the site, and its list.
    /// </summary>
    public static Belongings Device(HttpContext context, DastanLibrary library)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(library);
        var positions = library.Collections
            .Select(site => (site.Collection, Level: ReadingPreference.Device(context, site)))
            .Where(position => position.Level is not null)
            .ToImmutableDictionary(position => position.Collection, position => position.Level!.Value, StringComparer.Ordinal);
        return new(positions, [.. SavedList.Device(context).Select(SavedList.Kept)]);
    }
}

/// <summary>What signing in needs, and the addresses the forms of the account are sent to.</summary>
public static class AccountEndpoints
{
    /// <summary>Adds the sign-in of a visitor: kept in a cookie, asked of the store with every request.</summary>
    public static void AddVisitorAccounts(this IServiceCollection services)
    {
        services.AddSingleton<AccountDesk>();
        services.AddScoped<AccountState>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = AccountDesk.SessionCookie;
            options.Cookie.HttpOnly = true;

            // The language is the first part of an address: a sign-in is for the whole site, not for one language.
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;

            // Every request asks the store whether the sign-in still holds: another password or a closed account
            // ends it at once, not when the cookie runs out.
            options.Events.OnValidatePrincipal = async context =>
            {
                var desk = context.HttpContext.RequestServices.GetRequiredService<AccountDesk>();
                var holds = false;
                if (desk.IsOpen && AccountDesk.Session(context.Principal) is { } session)
                {
                    using var store = desk.Open();
                    holds = store.Holds(session);
                }

                if (!holds)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
                }
            };
        });
    }

    /// <summary>
    /// Maps the forms of the account. Each answers by sending the visitor back to the page of the account, which
    /// says what happened; none says whether a name exists.
    /// </summary>
    public static void MapVisitorAccounts(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var forms = endpoints.MapGroup(AccountDesk.Root).AddEndpointFilter(async (invocation, next) =>
        {
            // No account, no form; and a password does not travel in the open.
            var context = invocation.HttpContext;
            var desk = context.RequestServices.GetRequiredService<AccountDesk>();
            return !desk.IsOpen ? Results.NotFound()
                : !AccountDesk.IsPrivate(context) ? Results.BadRequest()
                : await next(invocation).ConfigureAwait(false);
        });

        forms.MapPost("/register", async (
            HttpContext context,
            AccountDesk desk,
            DastanLibrary library,
            [FromForm] string? name,
            [FromForm] string? password,
            [FromForm] string? again,
            [FromForm] string? website,
            [FromForm] string? invitation,
            [FromForm] string? returnUrl) =>
        {
            // Who came by an invitation and has to try again still has it.
            var from = invitation is { Length: > 0 and <= 40 } && invitation.All(char.IsAsciiLetterOrDigit)
                ? $"{Home(returnUrl)}?invitation={invitation}"
                : returnUrl;

            // A program that fills in every field has filled in the one a person does not see.
            if (!string.IsNullOrEmpty(website))
            {
                return Back(from, "register", "later");
            }

            if (!desk.Allows(context, "register", AccountDesk.RegistrationsPerHour))
            {
                return Back(from, "register", "later");
            }

            if (password != again)
            {
                return Back(from, "register", "password-differs");
            }

            using var store = desk.Open();
            var made = store.Register(name ?? string.Empty, password ?? string.Empty, invitation);
            if (!made.Done)
            {
                return Back(from, "register", [.. made.Problems]);
            }

            // The new account begins with what the device it was made on holds: the list and how far its
            // visitor has read. The device keeps its own.
            store.BringIn(made.Session!.Id, AccountState.Device(context, library));
            await AccountDesk.SignIn(context, made.Session!).ConfigureAwait(false);
            desk.Carry(context, made.RecoveryCodes);
            return Said(returnUrl, "welcome");
        });

        forms.MapPost("/sign-in", async (
            HttpContext context,
            AccountDesk desk,
            [FromForm] string? name,
            [FromForm] string? password,
            [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "try", AccountDesk.AttemptsPerHour))
            {
                return Back(returnUrl, "sign-in", "later");
            }

            using var store = desk.Open();
            if (store.SignIn(name ?? string.Empty, password ?? string.Empty) is not { } session)
            {
                return Back(returnUrl, "sign-in", "wrong");
            }

            await AccountDesk.SignIn(context, session).ConfigureAwait(false);
            return Said(returnUrl, "signed-in");
        });

        forms.MapPost("/sign-out", async (HttpContext context, [FromForm] string? returnUrl) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return Said(returnUrl, "signed-out");
        });

        forms.MapPost("/recover", async (
            HttpContext context,
            AccountDesk desk,
            [FromForm] string? name,
            [FromForm] string? code,
            [FromForm] string? password,
            [FromForm] string? again,
            [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "try", AccountDesk.AttemptsPerHour))
            {
                return Back(returnUrl, "recover", "later");
            }

            if (password != again)
            {
                return Back(returnUrl, "recover", "password-differs");
            }

            using var store = desk.Open();
            var recovered = store.Recover(name ?? string.Empty, code ?? string.Empty, password ?? string.Empty);
            if (!recovered.Done)
            {
                return Back(returnUrl, "recover", [.. recovered.Problems.Select(problem => problem == "wrong" ? "wrong-code" : problem)]);
            }

            await AccountDesk.SignIn(context, recovered.Session!).ConfigureAwait(false);
            return Said(returnUrl, "recovered");
        });

        forms.MapPost("/reopen", async (
            HttpContext context,
            AccountDesk desk,
            [FromForm] string? name,
            [FromForm] string? password,
            [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "try", AccountDesk.AttemptsPerHour))
            {
                return Back(returnUrl, "reopen", "later");
            }

            using var store = desk.Open();
            if (store.Reopen(name ?? string.Empty, password ?? string.Empty) is not { } session)
            {
                return Back(returnUrl, "reopen", "wrong-closed");
            }

            await AccountDesk.SignIn(context, session).ConfigureAwait(false);
            return Said(returnUrl, "reopened");
        });

        // What follows is for a visitor who signed in; everybody else is refused.
        forms.MapPost("/password", async (
            HttpContext context,
            AccountDesk desk,
            [FromForm] string? current,
            [FromForm] string? password,
            [FromForm] string? again,
            [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            if (password != again)
            {
                return Back(returnUrl, "password", "password-differs");
            }

            using var store = desk.Open();
            var changed = store.ChangePassword(session.Id, current ?? string.Empty, password ?? string.Empty);
            if (!changed.Done)
            {
                return Back(returnUrl, "password", [.. changed.Problems.Select(Current)]);
            }

            // The sign-in of this browser goes on with the new password; every other one has ended.
            await AccountDesk.SignIn(context, changed.Session!).ConfigureAwait(false);
            return Said(returnUrl, "password");
        });

        forms.MapPost("/codes", (HttpContext context, AccountDesk desk, [FromForm] string? current, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            var renewed = store.NewRecoveryCodes(session.Id, current ?? string.Empty);
            if (!renewed.Done)
            {
                return Back(returnUrl, "codes", [.. renewed.Problems.Select(Current)]);
            }

            desk.Carry(context, renewed.RecoveryCodes);
            return Said(returnUrl, "codes");
        });

        forms.MapPost("/close", async (HttpContext context, AccountDesk desk, [FromForm] string? current, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            var closed = store.Close(session.Id, current ?? string.Empty);
            if (!closed.Done)
            {
                return Back(returnUrl, "close", [.. closed.Problems.Select(Current)]);
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return Said(returnUrl, "closed");
        });

        MapBelongings(forms);
    }

    // More of what is for a visitor who signed in: what follows them, and their invitations.
    private static void MapBelongings(RouteGroupBuilder forms)
    {
        forms.MapPost("/bring", (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            store.BringIn(session.Id, AccountState.Device(context, library));
            return Said(returnUrl, "brought");
        });

        forms.MapPost("/invite", (HttpContext context, AccountDesk desk, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Invite(session.Id) is null ? Back(returnUrl, "invite", "invitations") : Results.Redirect($"{Home(returnUrl)}?said=invited#invite");
        });

        forms.MapPost("/revoke", (HttpContext context, AccountDesk desk, [FromForm] string? code, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Revoke(session.Id, code ?? string.Empty) ? Results.Redirect($"{Home(returnUrl)}?said=revoked#invite") : Results.BadRequest();
        });
    }

    private static string Current(string problem) => problem == "wrong" ? "wrong-current" : problem;

    // The page of the account in the language the form came from; what a form names as its page is used only
    // when it is that page.
    private static string Home(string? returnUrl)
    {
        var address = ReturnAddress.Safe(returnUrl, AccountDesk.Page);
        var end = address.IndexOfAny(['?', '#']);
        address = end < 0 ? address : address[..end];
        return address.EndsWith(AccountDesk.Page, StringComparison.Ordinal) && address.Length <= AccountDesk.Page.Length + 12 ? address : AccountDesk.Page;
    }

    private static IResult Said(string? returnUrl, string what) => Results.Redirect($"{Home(returnUrl)}?said={what}");

    private static IResult Back(string? returnUrl, string form, params string[] problems)
    {
        // An invitation the visitor came by stays in the address.
        var invitation = returnUrl?.IndexOf("?invitation=", StringComparison.Ordinal) is { } at and >= 0 ? "&" + returnUrl[(at + 1)..] : string.Empty;
        return Results.Redirect($"{Home(returnUrl)}?form={form}&problem={string.Join(',', problems)}{invitation}#{form}");
    }
}
