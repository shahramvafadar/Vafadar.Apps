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
/// The accounts of the visitors on the site (step 16.1, D-048 to D-050; on ASP.NET Core Identity with a
/// confirmed mail address since step 19.4, ADR 0016). The site has accounts only when it was told its database
/// (<c>Dastan:Database</c>). Without it the site is what it was – no account, no page for one.
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

    /// <summary>How many mails with a link one address may ask for in an hour, over all accounts.</summary>
    public const int MailsPerHour = 10;

    private const string IdClaim = "dastan.visitor.id";
    private const string StampClaim = "dastan.visitor.stamp";

    private readonly string? _file;
    private readonly string? _photos;
    private readonly string? _site;
    private readonly TimeProvider _time;
    private readonly IDataProtector _codes;
    private readonly ITimeLimitedDataProtector _arrivals;

    // Who asked how often in the present hour. It lives in the memory of the process and is thrown away with the
    // hour: an address is never written anywhere.
    private readonly ConcurrentDictionary<string, int> _asked = new(StringComparer.Ordinal);
    private long _hour;
    private long _purgedOn;

    /// <summary>Initializes a new instance of the <see cref="AccountDesk"/> class and brings the store to the form this program knows.</summary>
    public AccountDesk(IOptions<DastanOptions> options, IOptions<OperatorOptions> who, IHostEnvironment environment, TimeProvider time, IDataProtectionProvider protection)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(who);

        // A link in a mail begins with the address of the site, as who runs it entered it.
        _site = who.Value.Site;
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(protection);
        _time = time;
        _codes = protection.CreateProtector("dastan.visitor.codes");
        _arrivals = protection.CreateProtector("dastan.visitor.external").ToTimeLimitedDataProtector();
        InvitationOnly = options.Value.InvitationOnly;
        if (!string.IsNullOrWhiteSpace(options.Value.Database))
        {
            // The site does not change the form of a database (ADR 0015): one that is behind is said so, with
            // the command that brings it there, before anybody is served.
            _file = options.Value.Database;
            if (PeopleStore.Pending(_file) is { Count: > 0 } pending)
            {
                throw new InvalidOperationException($"The database lacks {pending.Count} change(s) of form (the first: {pending[0]}). Run \"dotnet Vafadar.Dastan.Content.dll database migrate\" with the connection in DASTAN_DATABASE, then start the site again.");
            }

            using var store = PeopleStore.Open(_file, time);
            if (!string.IsNullOrWhiteSpace(options.Value.Photos))
            {
                _photos = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.Photos));
                Directory.CreateDirectory(Path.Combine(_photos, "media"));
            }
        }
    }

    /// <summary>Gets the folder of the photographs of the visitors, or nothing when the site has none.</summary>
    public string? PhotoFolder => _photos;

    /// <summary>Returns where the file of a photograph lies, by the name the store keeps for it; nothing for a name that is no plain file name.</summary>
    public string? PhotoPath(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _photos is null || name.Length == 0 || name[0] == '.' || name != Path.GetFileName(name) ? null : Path.Combine(_photos, "media", name);
    }

    /// <summary>Gets a value indicating whether the site has accounts.</summary>
    public bool IsOpen => _file is not null;

    /// <summary>Gets a value indicating whether a new account needs an invitation (D-068).</summary>
    public bool InvitationOnly { get; }

    /// <summary>
    /// Returns what keeps somebody from making an account before anything about the account is looked at:
    /// they did not say that they are 16 or older (D-061), or the site takes new accounts by invitation only
    /// and they have none that can be used (D-068). Nothing: they may go on.
    /// </summary>
    public string? MayNotRegister(PeopleStore store, string? age, string? invitation, string? terms)
    {
        ArgumentNullException.ThrowIfNull(store);
        return age != "on" ? "age" : terms != "on" ? "terms" : InvitationOnly && !store.IsInvited(invitation) ? "invitation-needed" : null;
    }

    /// <summary>
    /// The version of the terms and of the privacy notice a new account says it read. It changes when one of
    /// the two changes in a way that matters to who uses the site, and is noted with the account.
    /// </summary>
    public const string TermsVersion = "2026-11-19";

    private const string ClosedCookie = "dastan.visitor.closed";

    /// <summary>
    /// Hands the statement of reasons of an account that who runs the site closed to the page, for the visitor
    /// who just proved to be its owner: its number, sealed for ten minutes in a cookie only this site reads.
    /// </summary>
    public void CarryClosed(HttpContext context, long restriction)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = PreferenceCookie.Options(context);
        options.MaxAge = TimeSpan.FromMinutes(10);
        context.Response.Cookies.Append(ClosedCookie, _arrivals.Protect(restriction.ToString(System.Globalization.CultureInfo.InvariantCulture), TimeSpan.FromMinutes(10)), options);
    }

    /// <summary>Returns the number of the statement of reasons a request carries, or nothing.</summary>
    public long? Closed(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Cookies[ClosedCookie] is not { Length: > 0 } carried)
        {
            return null;
        }

        try
        {
            return long.TryParse(_arrivals.Unprotect(carried), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    /// <summary>
    /// Opens the store for one request. Once a day, the first request deletes for good the accounts that were
    /// closed long enough ago (D-055): the site needs no other clockwork for it.
    /// </summary>
    public PeopleStore Open()
    {
        var store = PeopleStore.Open(_file ?? throw new InvalidOperationException("The site has no accounts."), _time, _site);
        var day = _time.GetUtcNow().ToUnixTimeSeconds() / 86400;
        if (Interlocked.Exchange(ref _purgedOn, day) != day)
        {
            try
            {
                store.Purge();
                Sweep(store);
            }
            catch (Exception failed) when (failed is Microsoft.EntityFrameworkCore.DbUpdateException or System.Data.Common.DbException or IOException or UnauthorizedAccessException)
            {
                // Not done today yet: the next request tries again. The request that asked goes on – it did
                // not come for this.
                Interlocked.Exchange(ref _purgedOn, 0);
            }
        }

        return store;
    }

    // A file that no photograph of the store names belongs to nothing – its account was deleted – and is
    // deleted. One that only just arrived is left alone: its photograph may not be kept yet.
    private void Sweep(PeopleStore store)
    {
        if (_photos is null)
        {
            return;
        }

        var named = store.PhotoFiles();
        var before = _time.GetUtcNow().UtcDateTime.AddHours(-1);
        foreach (var file in new DirectoryInfo(Path.Combine(_photos, "media")).EnumerateFiles())
        {
            // The smaller size of a photograph belongs to the photograph.
            if (!named.Contains(file.Name) && !(PhotoEndpoints.OfSmall(file.Name) is { } of && named.Contains(of)) && file.LastWriteTimeUtc < before)
            {
                file.Delete();
            }
        }
    }

    /// <summary>Counts a request of a kind for who sent it (<see cref="Sender"/>) and returns whether it is still within what an hour allows.</summary>
    public bool Allows(HttpContext context, string kind, int most)
    {
        ArgumentNullException.ThrowIfNull(context);
        var hour = _time.GetUtcNow().ToUnixTimeSeconds() / 3600;
        if (Interlocked.Exchange(ref _hour, hour) != hour)
        {
            _asked.Clear();
        }

        return _asked.AddOrUpdate($"{kind} {Sender.Of(context)}", 1, (_, count) => count + 1) <= most;
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

    /// <summary>
    /// Hands a sign-in of a provider that has no account yet to the form on which the visitor chooses a name,
    /// with the mail address the provider confirmed: sealed for ten minutes in a cookie only this site can read.
    /// </summary>
    public void CarryArrival(HttpContext context, string provider, string subject, string mail, string? invitation)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = PreferenceCookie.Options(context);
        options.MaxAge = TimeSpan.FromMinutes(10);
        context.Response.Cookies.Append(ExternalSignIn.PendingCookie, _arrivals.Protect($"{provider}\n{subject}\n{mail}\n{invitation}", TimeSpan.FromMinutes(10)), options);
    }

    /// <summary>Returns the sign-in of a provider a request carries, or nothing: none, not sealed by this site, or older than ten minutes.</summary>
    public (string Provider, string Subject, string Mail, string? Invitation)? Arrival(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Cookies[ExternalSignIn.PendingCookie] is not { Length: > 0 } carried)
        {
            return null;
        }

        try
        {
            return _arrivals.Unprotect(carried).Split('\n') is [var provider, var subject, var mail, var invitation] ? (provider, subject, mail, invitation.Length > 0 ? invitation : null) : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Takes the sign-in of a provider a request carries away: the account is made.</summary>
    public void ForgetArrival(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.Cookies.Delete(ExternalSignIn.PendingCookie, PreferenceCookie.Options(context));
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
    private int _waiting;

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
                _waiting = store.Waiting(session.Id);
            }

            return _belongings;
        }
    }

    /// <summary>Gets how many requests for a relationship wait for the answer of who signed in.</summary>
    public int Waiting => Belongings is null ? 0 : _waiting;

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
    public static void AddVisitorAccounts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AccountDesk>();
        services.AddScoped<AccountState>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddExternalSignIn(configuration);
        services.AddAuthentication().AddCookie(options =>
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

        forms.MapPost("/register", (
            HttpContext context,
            AccountDesk desk,
            [FromForm] string? name,
            [FromForm] string? mail,
            [FromForm] string? password,
            [FromForm] string? again,
            [FromForm] string? website,
            [FromForm] string? invitation,
            [FromForm] string? age,
            [FromForm] string? terms,
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
            if (desk.MayNotRegister(store, age, invitation, terms) is { } kept)
            {
                return Back(from, "register", kept);
            }

            // The account is one when the link that is mailed now was opened; until then nobody is signed in.
            // An address that has an account already is answered in the same words: who has it is told by mail.
            var problems = store.Accounts.Register(name ?? string.Empty, mail ?? string.Empty, password ?? string.Empty, Tongue(returnUrl), invitation, desk.InvitationOnly, AccountDesk.TermsVersion);
            return problems.IsEmpty ? Said(returnUrl, "confirm-sent") : Back(from, "register", [.. problems]);
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
            var answer = store.Accounts.SignIn(name ?? string.Empty, password ?? string.Empty);
            if (answer.Outcome == SignInOutcome.Unconfirmed)
            {
                // Said after the right password only: the page offers to mail the link once more.
                return Back(returnUrl, "sign-in", "unconfirmed");
            }

            if (answer.Session is not { } session)
            {
                // An account that who runs the site closed tells its owner why – the site has no other way to.
                if (store.ClosedByOperator(name ?? string.Empty, password ?? string.Empty) is { } statement)
                {
                    desk.CarryClosed(context, statement.Number);
                    return Back(returnUrl, "sign-in", "closed-by-operator");
                }

                return Back(returnUrl, "sign-in", "wrong");
            }

            await AccountDesk.SignIn(context, session).ConfigureAwait(false);
            return Said(returnUrl, "signed-in");
        });

        // The terms changed, or the account is older than the note of what it read: its visitor says so again.
        forms.MapPost("/terms", (HttpContext context, AccountDesk desk, [FromForm] string? terms, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.Unauthorized();
            }

            if (terms != "on")
            {
                return Back(returnUrl, "terms", "terms");
            }

            using var store = desk.Open();
            return store.AcceptTerms(session.Id, AccountDesk.TermsVersion) ? Said(returnUrl, "terms-agreed") : Results.Unauthorized();
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
            var recovered = store.Accounts.Recover(name ?? string.Empty, code ?? string.Empty, password ?? string.Empty);
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
            if (store.Accounts.Reopen(name ?? string.Empty, password ?? string.Empty) is not { } session)
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
            var changed = store.Accounts.ChangePassword(session.Id, current ?? string.Empty, password ?? string.Empty);
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
            var renewed = store.Accounts.NewRecoveryCodes(session.Id, current ?? string.Empty);
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
            var closed = store.Accounts.Close(session.Id, current ?? string.Empty);
            if (!closed.Done)
            {
                return Back(returnUrl, "close", [.. closed.Problems.Select(Current)]);
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return Said(returnUrl, "closed");
        });

        MapLinks(forms);
        MapAddress(forms);
        MapBelongings(forms);
        forms.MapExternalSignIn();
    }

    // What a mailed link leads to (step 19.4, ADR 0016). A link is opened with GET and changes nothing by
    // that – a program that looks at the links of a mail uses none up; the page it shows sends its form here.
    private static void MapLinks(RouteGroupBuilder forms)
    {
        forms.MapPost("/confirm", async (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] string? id, [FromForm] string? token, [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "try", AccountDesk.AttemptsPerHour))
            {
                return Link(returnUrl, "confirm", id, token, "later");
            }

            using var store = desk.Open();
            var answer = store.Accounts.Confirm(id ?? string.Empty, token ?? string.Empty);
            if (answer is not { Done: true, Session: { } session })
            {
                return Link(returnUrl, "confirm", null, null, "link");
            }

            // The new account begins with what the device its address was confirmed on holds: the list and
            // how far its visitor has read. The device keeps its own.
            store.BringIn(session.Id, AccountState.Device(context, library));
            await AccountDesk.SignIn(context, session).ConfigureAwait(false);
            desk.Carry(context, answer.RecoveryCodes);
            return Said(returnUrl, "welcome");
        });

        // The link once more, for an account that waits for its address. Who asks is told the same whether
        // there is such an account or not.
        forms.MapPost("/resend", (HttpContext context, AccountDesk desk, [FromForm] string? name, [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "mail", AccountDesk.MailsPerHour))
            {
                return Back(returnUrl, "resend", "later");
            }

            using var store = desk.Open();
            store.Accounts.SendConfirmationAgain(name ?? string.Empty);
            return Said(returnUrl, "resend-sent");
        });

        forms.MapPost("/forgot", (HttpContext context, AccountDesk desk, [FromForm] string? name, [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "mail", AccountDesk.MailsPerHour))
            {
                return Back(returnUrl, "forgot", "later");
            }

            using var store = desk.Open();
            store.Accounts.ForgotPassword(name ?? string.Empty);
            return Said(returnUrl, "forgot-sent");
        });

        forms.MapPost("/reset", async (HttpContext context, AccountDesk desk, [FromForm] string? id, [FromForm] string? token, [FromForm] string? password, [FromForm] string? again, [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "try", AccountDesk.AttemptsPerHour))
            {
                return Link(returnUrl, "reset", id, token, "later");
            }

            if (password != again)
            {
                return Link(returnUrl, "reset", id, token, "password-differs");
            }

            using var store = desk.Open();
            var reset = store.Accounts.ResetPassword(id ?? string.Empty, token ?? string.Empty, password ?? string.Empty);
            if (!reset.Done)
            {
                // A password that is too short leaves the link as it was; a link that does not work is gone.
                return reset.Problems.Contains("wrong") ? Link(returnUrl, "reset", null, null, "link") : Link(returnUrl, "reset", id, token, [.. reset.Problems]);
            }

            await AccountDesk.SignIn(context, reset.Session!).ConfigureAwait(false);
            return Said(returnUrl, "password");
        });

        forms.MapPost("/address-confirm", async (HttpContext context, AccountDesk desk, [FromForm] string? id, [FromForm] string? token, [FromForm] string? returnUrl) =>
        {
            if (!desk.Allows(context, "try", AccountDesk.AttemptsPerHour))
            {
                return Link(returnUrl, "address", id, token, "later");
            }

            using var store = desk.Open();
            if (store.Accounts.ConfirmNewAddress(id ?? string.Empty, token ?? string.Empty) is not { } session)
            {
                return Link(returnUrl, "address", null, null, "link");
            }

            await AccountDesk.SignIn(context, session).ConfigureAwait(false);
            return Said(returnUrl, "address-changed");
        });
    }

    // The address of an account, and its sessions: for a visitor who signed in.
    private static void MapAddress(RouteGroupBuilder forms)
    {
        forms.MapPost("/address", (HttpContext context, AccountDesk desk, [FromForm] string? current, [FromForm] string? mail, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            if (!desk.Allows(context, "mail", AccountDesk.MailsPerHour))
            {
                return Back(returnUrl, "address", "later");
            }

            using var store = desk.Open();
            var problems = store.Accounts.ChangeAddress(session.Id, current ?? string.Empty, mail ?? string.Empty);
            return problems.IsEmpty ? Said(returnUrl, "address-sent") : Back(returnUrl, "address", [.. problems.Select(Current)]);
        });

        forms.MapPost("/sign-out-everywhere", async (HttpContext context, AccountDesk desk, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            if (store.Accounts.SignOutEverywhere(session.Id) is not { } kept)
            {
                return Results.BadRequest();
            }

            // This browser stays signed in; every other one has ended.
            await AccountDesk.SignIn(context, kept).ConfigureAwait(false);
            return Said(returnUrl, "signed-out-everywhere");
        });
    }

    // More of what is for a visitor who signed in: what follows them, and their invitations.
    private static void MapBelongings(RouteGroupBuilder forms)
    {
        // A copy of what the store holds about the account (step 16.7), handed over as a file – proved by the
        // password, since it is everything at once.
        forms.MapPost("/export", (HttpContext context, AccountDesk desk, [FromForm] string? current, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            if (store.Export(session.Id, current ?? string.Empty) is not { } copy)
            {
                return Back(returnUrl, "export", "wrong-current");
            }

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.ContentDisposition = "attachment; filename=\"dastan-account.json\"";
            return Results.Text(copy.ToJsonString(Written), "application/json", System.Text.Encoding.UTF8);
        });

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

        // The same invitation, mailed by the site to an address the account names (step 19.4): one message,
        // and the address is kept for nothing else.
        forms.MapPost("/invite-by-mail", (HttpContext context, AccountDesk desk, [FromForm] string? mail, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            if (!desk.Allows(context, "mail", AccountDesk.MailsPerHour))
            {
                return Back(returnUrl, "invite", "later");
            }

            using var store = desk.Open();
            return store.InviteByMail(session.Id, mail ?? string.Empty, Tongue(returnUrl)) is var outcome and not "sent"
                ? Back(returnUrl, "invite", outcome)
                : Results.Redirect($"{Home(returnUrl)}?said=invite-sent#invite");
        });

        // Whether the site mails the account what somebody else asks of it (PRIV-007).
        forms.MapPost("/notices", (HttpContext context, AccountDesk desk, [FromForm] string? notices, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Accounts.SetNotices(session.Id, notices == "on")
                ? Results.Redirect($"{Home(returnUrl)}?said={(notices == "on" ? "notices-on" : "notices-off")}#notices")
                : Results.BadRequest();
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

    // A copy is for a person to read: on lines of its own, with the letters of every script as they are.
    private static readonly System.Text.Json.JsonSerializerOptions Written = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string Current(string problem) => problem == "wrong" ? "wrong-current" : problem;

    // The page of the account in the language the form came from; what a form names as its page is used only
    // when it is that page.
    internal static string Home(string? returnUrl)
    {
        var address = ReturnAddress.Safe(returnUrl, AccountDesk.Page);
        var end = address.IndexOfAny(['?', '#']);
        address = end < 0 ? address : address[..end];
        return address.EndsWith(AccountDesk.Page, StringComparison.Ordinal) && address.Length <= AccountDesk.Page.Length + 12 ? address : AccountDesk.Page;
    }

    internal static IResult Said(string? returnUrl, string what) => Results.Redirect($"{Home(returnUrl)}?said={what}");

    // The language of the page a form came from: a mail about the account is written in it.
    internal static string Tongue(string? returnUrl) => Home(returnUrl).StartsWith("/fa/", StringComparison.Ordinal) ? "fa" : "en";

    // Back to the page of a mailed link, with what was wrong. The link itself is kept in the address only while
    // it can still be used.
    internal static IResult Link(string? returnUrl, string page, string? id, string? token, params string[] problems)
    {
        static bool Plain(string? value, int longest) => value is { Length: > 0 } && value.Length <= longest && value.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '-');
        var link = Plain(id, 64) && Plain(token, 128) ? $"id={id}&token={token}&" : string.Empty;
        return Results.Redirect($"{Home(returnUrl)}/{page}?{link}problem={string.Join(',', problems)}");
    }

    internal static IResult Back(string? returnUrl, string form, params string[] problems)
    {
        // An invitation the visitor came by stays in the address.
        const string mark = "?invitation=";
        var code = returnUrl?.IndexOf(mark, StringComparison.Ordinal) is { } at and >= 0 ? returnUrl[(at + mark.Length)..] : string.Empty;
        var end = code.IndexOfAny(['&', '#']);
        code = end < 0 ? code : code[..end];
        var invitation = code is { Length: > 0 and <= 40 } && code.All(char.IsAsciiLetterOrDigit) ? $"&invitation={code}" : string.Empty;
        return Results.Redirect($"{Home(returnUrl)}?form={form}&problem={string.Join(',', problems)}{invitation}#{form}");
    }
}
