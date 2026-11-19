using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>The providers a visitor can sign in through. Each is the name of a sign-in scheme of the app.</summary>
public sealed class ExternalSignInOptions
{
    /// <summary>Gets the providers, in the order the page offers them: <c>google</c>, <c>microsoft</c>.</summary>
    public IList<string> Providers { get; } = [];
}

/// <summary>
/// Signing in through Google or Microsoft (step 16.8, D-048). A provider is offered only when the owner entered
/// a client of it (<c>Dastan:SignIn:Google</c>, <c>Dastan:SignIn:Microsoft</c>: <c>ClientId</c> and
/// <c>ClientSecret</c> – the secret from the environment or a file the owner keeps, never from the repository).
/// The provider is asked who somebody is and for the mail address it confirmed, nothing else (ADR 0016): the
/// site keeps a hash of the identifier, and the address as the address of the account. No name and no picture
/// is asked for or kept; the visitor chooses the name of the account.
/// </summary>
public static class ExternalSignIn
{
    /// <summary>The scheme that holds what a provider said for the moment between its answer and the site's.</summary>
    public const string Arriving = "dastan.external";

    /// <summary>The name of the cookie that carries a sign-in of a provider that has no account yet to the form that makes one.</summary>
    public const string PendingCookie = "dastan.visitor.external";

    /// <summary>
    /// The name of a provider that is made up (step 18.2.2): it asks nobody and says that the visitor is whoever
    /// the setting <c>Dastan:SignIn:MadeUp</c> names. It is there so that the forms after a sign-in through a
    /// provider can be looked at on a developer's machine, where no client of Google or Microsoft is entered.
    /// It exists in the environment <c>Development</c> only: anywhere else the setting does nothing.
    /// </summary>
    public const string MadeUp = "made-up";

    private const string ProviderItem = "dastan.provider";
    private const string InvitationItem = "dastan.invitation";

    /// <summary>Adds the providers the owner entered a client for.</summary>
    public static void AddExternalSignIn(this AuthenticationBuilder authentication, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(authentication);
        ArgumentNullException.ThrowIfNull(configuration);
        authentication.AddCookie(Arriving, options =>
        {
            options.Cookie.Name = "dastan.visitor.arriving";
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });

        var google = configuration.GetSection("Dastan:SignIn:Google");
        if (google["ClientId"] is { Length: > 0 } googleId && google["ClientSecret"] is { Length: > 0 } googleSecret)
        {
            authentication.AddGoogle("google", options =>
            {
                (options.ClientId, options.ClientSecret, options.SignInScheme) = (googleId, googleSecret, Arriving);
                options.CallbackPath = "/signin-google";

                // Asked for who it is and for the address it confirmed, and for nothing else about them.
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.SaveTokens = false;
                KeepWhoAndTheAddress(options.ClaimActions, ["sub", "id"], GoogleAddress);
            });
            authentication.Services.Configure<ExternalSignInOptions>(options => options.Providers.Add("google"));
        }

        var microsoft = configuration.GetSection("Dastan:SignIn:Microsoft");
        if (microsoft["ClientId"] is { Length: > 0 } microsoftId && microsoft["ClientSecret"] is { Length: > 0 } microsoftSecret)
        {
            authentication.AddMicrosoftAccount("microsoft", options =>
            {
                (options.ClientId, options.ClientSecret, options.SignInScheme) = (microsoftId, microsoftSecret, Arriving);
                options.CallbackPath = "/signin-microsoft";
                options.SaveTokens = false;

                // Microsoft tells who somebody is only together with their profile. Of what it sends, the
                // identifier and the address are taken and everything else is dropped before anything is
                // written down.
                KeepWhoAndTheAddress(options.ClaimActions, ["id"], MicrosoftAddress);
            });
            authentication.Services.Configure<ExternalSignInOptions>(options => options.Providers.Add("microsoft"));
        }

        AddMadeUpProvider(authentication, configuration);
    }

    private static void AddMadeUpProvider(AuthenticationBuilder authentication, IConfiguration configuration)
    {
        if (configuration["Dastan:SignIn:MadeUp"] is not { Length: > 0 })
        {
            return;
        }

        authentication.AddScheme<AuthenticationSchemeOptions, MadeUpProvider>(MadeUp, null);

        // Offered only in development: the list of providers is what the pages show and what the forms take.
        authentication.Services.AddOptions<ExternalSignInOptions>().Configure<IHostEnvironment>((options, environment) =>
        {
            if (environment.IsDevelopment())
            {
                options.Providers.Add(MadeUp);
            }
        });
    }

    // The made-up provider: no page of its own, no question – it says at once who the setting names.
    private sealed class MadeUpProvider(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration, IHostEnvironment environment)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // Checked here once more: outside development nobody is signed in by a setting, whatever asked.
            if (environment.IsDevelopment() && configuration["Dastan:SignIn:MadeUp"] is { Length: > 0 } subject)
            {
                // Its address is as made up as it is: the setting itself when that is an address.
                var mail = subject.Contains('@', StringComparison.Ordinal) ? subject : $"{subject}@example.org";
                await Context.SignInAsync(Arriving, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, subject), new Claim(ClaimTypes.Email, mail)], MadeUp)), properties);
            }

            Response.Redirect(properties.RedirectUri ?? "/");
        }
    }

    // Of what a provider answers, the identifier of the person and the address it confirmed become claims – no
    // name, no picture, nothing else.
    private static void KeepWhoAndTheAddress(Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection claims, string[] keys, Func<System.Text.Json.JsonElement, string?> address)
    {
        claims.Clear();
        foreach (var key in keys)
        {
            claims.MapJsonKey(ClaimTypes.NameIdentifier, key);
        }

        claims.MapCustomJson(ClaimTypes.Email, address);
    }

    private static string? Text(System.Text.Json.JsonElement user, string key) =>
        user.ValueKind == System.Text.Json.JsonValueKind.Object && user.TryGetProperty(key, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : null;

    private static bool Says(System.Text.Json.JsonElement user, string key) =>
        user.ValueKind == System.Text.Json.JsonValueKind.Object && user.TryGetProperty(key, out var value)
        && (value.ValueKind == System.Text.Json.JsonValueKind.True || (value.ValueKind == System.Text.Json.JsonValueKind.String && string.Equals(value.GetString(), "true", StringComparison.OrdinalIgnoreCase)));

    /// <summary>Returns the address Google says it confirmed; an address it did not confirm is none.</summary>
    internal static string? GoogleAddress(System.Text.Json.JsonElement user) =>
        Says(user, "email_verified") || Says(user, "verified_email") ? Text(user, "email") : null;

    /// <summary>
    /// Returns the address of a Microsoft account: its mailbox or, without one, the name it signs in with when
    /// that is an address of its own – not the name of a guest in somebody else's directory.
    /// </summary>
    internal static string? MicrosoftAddress(System.Text.Json.JsonElement user) =>
        Text(user, "mail") is { Length: > 0 } mail ? mail
        : Text(user, "userPrincipalName") is { Length: > 0 } name && name.Contains('@', StringComparison.Ordinal) && !name.Contains("#EXT#", StringComparison.OrdinalIgnoreCase) ? name
        : null;

    /// <summary>Maps the forms of signing in through a provider onto the forms of the account.</summary>
    public static void MapExternalSignIn(this RouteGroupBuilder forms)
    {
        ArgumentNullException.ThrowIfNull(forms);

        // The visitor leaves for the provider. What comes back is looked at below.
        forms.MapPost("/external", (HttpContext context, IOptions<ExternalSignInOptions> providers, [FromForm] string? provider, [FromForm] string? invitation, [FromForm] string? returnUrl) =>
        {
            if (provider is null || !providers.Value.Providers.Contains(provider))
            {
                return Results.BadRequest();
            }

            var properties = new AuthenticationProperties { RedirectUri = $"{AccountDesk.Root}/external/done?returnUrl={Uri.EscapeDataString(AccountEndpoints.Home(returnUrl))}" };
            properties.Items[ProviderItem] = provider;
            if (invitation is { Length: > 0 and <= 40 } && invitation.All(char.IsAsciiLetterOrDigit))
            {
                properties.Items[InvitationItem] = invitation;
            }

            return Results.Challenge(properties, [provider]);
        });

        forms.MapGet("/external/done", async (HttpContext context, AccountDesk desk, string? returnUrl) =>
        {
            var arrived = await context.AuthenticateAsync(Arriving).ConfigureAwait(false);
            await context.SignOutAsync(Arriving).ConfigureAwait(false);
            if (!arrived.Succeeded
                || arrived.Properties?.Items.TryGetValue(ProviderItem, out var provider) != true || provider is null
                || arrived.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) is not { Length: > 0 } subject)
            {
                return AccountEndpoints.Back(returnUrl, "sign-in", "external-failed");
            }

            using var store = desk.Open();
            if (AccountDesk.Session(context.User) is { } signedIn)
            {
                // Somebody who is signed in adds the provider to their account.
                return store.Accounts.Link(signedIn.Id, provider, subject)
                    ? AccountEndpoints.Said(returnUrl, "linked")
                    : AccountEndpoints.Back(returnUrl, "providers", "login-taken");
            }

            if (store.Accounts.SignInWith(provider, subject) is { } session)
            {
                await AccountDesk.SignIn(context, session).ConfigureAwait(false);
                return AccountEndpoints.Said(returnUrl, "signed-in");
            }

            // Nobody has this sign-in yet. An account needs an address somebody confirmed (ADR 0016): a
            // provider that names none makes no account.
            if (arrived.Principal.FindFirstValue(ClaimTypes.Email) is not { Length: > 0 } mail || !Data.MailOutbox.IsAddress(mail))
            {
                return AccountEndpoints.Back(returnUrl, "sign-in", "external-no-address");
            }

            // The visitor chooses a name, and the account is made then.
            arrived.Properties.Items.TryGetValue(InvitationItem, out var invitation);
            desk.CarryArrival(context, provider, subject, mail, invitation);
            return Results.Redirect($"{AccountEndpoints.Home(returnUrl)}?form=external#external");
        });

        forms.MapPost("/external-register", async (HttpContext context, AccountDesk desk, DastanLibrary library, [FromForm] string? name, [FromForm] string? website, [FromForm] string? age, [FromForm] string? terms, [FromForm] string? returnUrl) =>
        {
            if (desk.Arrival(context) is not var (provider, subject, mail, invitation))
            {
                return AccountEndpoints.Back(returnUrl, "sign-in", "external-failed");
            }

            if (!string.IsNullOrEmpty(website) || !desk.Allows(context, "register", AccountDesk.RegistrationsPerHour))
            {
                return AccountEndpoints.Back(returnUrl, "external", "later");
            }

            using var store = desk.Open();
            if (desk.MayNotRegister(store, age, invitation, terms) is { } kept)
            {
                return AccountEndpoints.Back(returnUrl, "external", kept);
            }

            var made = store.Accounts.RegisterWith(name ?? string.Empty, provider, subject, mail, AccountEndpoints.Tongue(returnUrl), invitation, desk.InvitationOnly, AccountDesk.TermsVersion);
            if (!made.Done)
            {
                return AccountEndpoints.Back(returnUrl, "external", [.. made.Problems]);
            }

            desk.ForgetArrival(context);
            store.BringIn(made.Session!.Id, AccountState.Device(context, library));
            await AccountDesk.SignIn(context, made.Session!).ConfigureAwait(false);
            desk.Carry(context, made.RecoveryCodes);
            return AccountEndpoints.Said(returnUrl, "welcome");
        });

        forms.MapPost("/unlink", (HttpContext context, AccountDesk desk, [FromForm] string? provider, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            using var store = desk.Open();
            return store.Accounts.Unlink(session.Id, provider ?? string.Empty) ? AccountEndpoints.Said(returnUrl, "unlinked") : AccountEndpoints.Back(returnUrl, "providers", "last-way-in");
        });

        forms.MapPost("/set-password", async (HttpContext context, AccountDesk desk, [FromForm] string? password, [FromForm] string? again, [FromForm] string? returnUrl) =>
        {
            if (AccountDesk.Session(context.User) is not { } session)
            {
                return Results.BadRequest();
            }

            if (password != again)
            {
                return AccountEndpoints.Back(returnUrl, "password", "password-differs");
            }

            using var store = desk.Open();
            var set = store.Accounts.SetPassword(session.Id, password ?? string.Empty);
            if (!set.Done)
            {
                return set.Problems.Contains("wrong") ? Results.BadRequest() : AccountEndpoints.Back(returnUrl, "password", [.. set.Problems]);
            }

            await AccountDesk.SignIn(context, set.Session!).ConfigureAwait(false);
            return AccountEndpoints.Said(returnUrl, "password");
        });
    }
}
