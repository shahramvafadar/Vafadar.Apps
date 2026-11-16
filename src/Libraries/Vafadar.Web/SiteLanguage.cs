using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Vafadar.Web;

/// <summary>The languages of a site and how its addresses carry them.</summary>
public sealed class SiteLanguageOptions
{
    /// <summary>Gets the language tags of the site ("en", "fa"). The first is used when nothing else decides.</summary>
    public IList<string> Languages { get; } = [];

    /// <summary>
    /// Gets the beginnings of addresses that carry no language: endpoints that are the same in every language
    /// ("/preferences", "/media"). Addresses of files (anything with an extension) never carry one.
    /// </summary>
    public IList<string> UnlocalizedPaths { get; } = [];

    /// <summary>Gets or sets the name of the cookie that remembers the language of the last page a visitor opened.</summary>
    public string CookieName { get; set; } = "vafadar.language";
}

/// <summary>The language of the current request.</summary>
/// <param name="Tag">The language tag as it stands in the address.</param>
/// <param name="Culture">The culture of the language.</param>
public sealed record SiteLanguage(string Tag, CultureInfo Culture)
{
    /// <summary>Gets a value indicating whether the language is written from right to left.</summary>
    public bool IsRightToLeft => Culture.TextInfo.IsRightToLeft;

    /// <summary>Gets the value of the <c>dir</c> attribute for text in the language.</summary>
    public string Direction => TextDirection.Of(Culture);
}

/// <summary>Registration and access of the site language.</summary>
public static class SiteLanguageExtensions
{
    /// <summary>Declares the languages of the site; the first is the default.</summary>
    public static IServiceCollection AddSiteLanguages(this IServiceCollection services, params string[] languages)
    {
        ArgumentNullException.ThrowIfNull(languages);
        if (languages.Length == 0)
        {
            throw new ArgumentException("A site has at least one language.", nameof(languages));
        }

        // The language is part of the path base of a page (/fa), and the framework would scope the cookie of its
        // form tokens to that base. Forms post to addresses without a language, so the cookie belongs to the site.
        services.Configure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options => options.Cookie.Path = "/");
        return services.Configure<SiteLanguageOptions>(options =>
        {
            foreach (var language in languages)
            {
                options.Languages.Add(language);
            }
        });
    }

    /// <summary>
    /// Reads the language of a page from the beginning of its address (<c>/fa/…</c>), makes it the culture of the
    /// request and takes it out of the path, so that routes are written once for all languages and relative links
    /// stay in the language. The address of a page without a language is redirected to the visitor's language: the
    /// one of the last page they opened, else the first language of the browser that the site has, else the default.
    /// Has to run before routing.
    /// </summary>
    public static IApplicationBuilder UseSiteLanguage(this IApplicationBuilder app) => app.UseMiddleware<SiteLanguageMiddleware>();

    /// <summary>Returns the language of the request.</summary>
    public static SiteLanguage GetSiteLanguage(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Features.Get<SiteLanguage>()
            ?? throw new InvalidOperationException("The request has no language; UseSiteLanguage has to run before this code.");
    }
}

/// <summary>See <see cref="SiteLanguageExtensions.UseSiteLanguage"/>.</summary>
internal sealed class SiteLanguageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SiteLanguage[] _languages;
    private readonly string[] _unlocalized;
    private readonly string _cookieName;

    public SiteLanguageMiddleware(RequestDelegate next, IOptions<SiteLanguageOptions> options)
    {
        _next = next;
        var settings = options.Value;
        if (settings.Languages.Count == 0)
        {
            throw new InvalidOperationException("The site has no languages; call AddSiteLanguages.");
        }

        // An unknown tag fails here, when the app starts, and not for the first visitor.
        _languages = [.. settings.Languages.Select(tag => new SiteLanguage(tag, CultureInfo.GetCultureInfo(tag, predefinedOnly: true)))];
        _unlocalized = [.. settings.UnlocalizedPaths];
        _cookieName = settings.CookieName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value ?? string.Empty;
        var end = path.IndexOf('/', 1);
        var first = path.Length > 1 ? path[1..(end < 0 ? path.Length : end)] : string.Empty;
        var language = Array.Find(_languages, candidate => string.Equals(candidate.Tag, first, StringComparison.OrdinalIgnoreCase));

        if (language is not null)
        {
            // One address per page: the tag as declared, and the start of a language with its closing slash, so
            // that relative links resolve inside the language.
            if (first != language.Tag || end < 0)
            {
                var rest = end < 0 ? "/" : path[end..];
                context.Response.Redirect($"{request.PathBase}/{language.Tag}{rest}{request.QueryString}", permanent: true);
                return;
            }

            if (HttpMethods.IsGet(request.Method) && request.Cookies[_cookieName] != language.Tag && !Path.HasExtension(path))
            {
                context.Response.Cookies.Append(_cookieName, language.Tag, PreferenceCookie.Options(context));
            }

            var (pathBase, original) = (request.PathBase, request.Path);
            request.PathBase = pathBase.Add($"/{language.Tag}");
            request.Path = path[end..];
            Apply(context, language);
            try
            {
                await _next(context);
            }
            finally
            {
                request.PathBase = pathBase;
                request.Path = original;
            }

            return;
        }

        var visitor = Choose(context);
        if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) && IsPage(path))
        {
            // The answer depends on the visitor, so nothing between here and the visitor may reuse it for another.
            context.Response.Headers.Vary = "Accept-Language, Cookie";
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Redirect($"{request.PathBase}/{visitor.Tag}{path}{request.QueryString}", permanent: false);
            return;
        }

        // An endpoint or a file without a language in its address still answers in the visitor's language.
        Apply(context, visitor);
        await _next(context);
    }

    private static void Apply(HttpContext context, SiteLanguage language)
    {
        context.Features.Set(language);

        // The culture flows with this request only; another request running at the same time has its own.
        CultureInfo.CurrentCulture = language.Culture;
        CultureInfo.CurrentUICulture = language.Culture;
    }

    private bool IsPage(string path) =>
        !Path.HasExtension(path)
        && !Array.Exists(_unlocalized, prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && (path.Length == prefix.Length || path[prefix.Length] == '/'));

    private SiteLanguage Choose(HttpContext context)
    {
        var saved = context.Request.Cookies[_cookieName];
        if (Array.Find(_languages, language => language.Tag == saved) is { } remembered)
        {
            return remembered;
        }

        foreach (var wanted in context.Request.GetTypedHeaders().AcceptLanguage.OrderByDescending(entry => entry.Quality ?? 1))
        {
            var tag = wanted.Value.Value ?? string.Empty;
            var neutral = tag.Split('-')[0];
            if (Array.Find(_languages, language =>
                    string.Equals(language.Tag, tag, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(language.Tag, neutral, StringComparison.OrdinalIgnoreCase)) is { } spoken)
            {
                return spoken;
            }
        }

        return _languages[0];
    }
}
