using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Components;
using Vafadar.Dastan.Web.Site;
using Vafadar.Web;

// Started with "--probe" the program is not the site: it asks the site of this machine whether it is alive and
// ends with the answer. A container has no other tool to ask with (docs/operations.md).
if (args is ["--probe"])
{
    return await SiteHealth.Probe();
}

var builder = WebApplication.CreateBuilder(args);

// Pages are rendered on the server for every request. No page keeps a connection open and none needs a script
// (ADR 0003), so nothing of one visitor can reach another.
builder.Services.AddRazorComponents();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
// The cookie of the forms is sent by https only wherever the visitor came by https, like every other cookie.
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

// English and Persian (Directory.Build.props: VafadarLanguages). The first language is used when nothing else decides.
builder.Services.AddSiteLanguages("en", "fa");
builder.Services.Configure<SiteLanguageOptions>(options =>
{
    options.UnlocalizedPaths.Add("/preferences");
    options.UnlocalizedPaths.Add(MediaAddress.Root);
    options.UnlocalizedPaths.Add(SiteHealth.Address);
    options.UnlocalizedPaths.Add(SearchEngineFiles.Robots);
    options.UnlocalizedPaths.Add(SearchEngineFiles.Sitemap);
});

builder.Services.Configure<DastanOptions>(builder.Configuration.GetSection("Dastan"));
builder.Services.AddSingleton<DastanSite>();

// The keys of the forms outlive the process only where a folder for them is named (docs/operations.md).
if (builder.Configuration["Dastan:Keys"] is { Length: > 0 } keys)
{
    builder.Services.AddDataProtection().SetApplicationName("dastan").PersistKeysToFileSystem(new DirectoryInfo(keys));
}
builder.Services.AddScoped<Visitor>();

// Behind a proxy that holds the certificate, the site learns from the proxy how the visitor came – by https, and
// from which address. It believes only the proxies it was told to (docs/operations.md): what anybody else says
// about that is not heard. Told nothing, it believes nobody.
var proxies = builder.Configuration.GetSection("Dastan:TrustedProxies").Get<string[]>() ?? [];
if (proxies.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (var proxy in proxies)
        {
            if (proxy.Contains('/', StringComparison.Ordinal))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(proxy));
            }
            else
            {
                options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            }
        }
    });
}

var app = builder.Build();

// The collection is read now: a collection that cannot be used stops the app with the list of what is wrong,
// instead of failing for the first visitor.
var site = app.Services.GetRequiredService<DastanSite>();

if (proxies.Length > 0)
{
    app.UseForwardedHeaders();
}

app.UseSecurityHeaders();
app.UseFreshPages();
if (site.Audience != Audience.Public)
{
    // A demonstration is not for search engines: no answer of it is to be indexed or followed.
    app.Use((context, next) =>
    {
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        return next(context);
    });
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// The language comes first, so that the pages for "not found" and for an error are in the language of the address.
app.UseSiteLanguage();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

// An address that leads nowhere gets the page for "not found" in its language. Only for a visitor who asks for a
// page: a form that is refused answers with its status and nothing else.
app.UseWhen(
    context => HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method),
    pages => pages.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

// A form that is refused leads to a page that says so.
app.UseRefusedFormPage();

// Routes are matched after the language was taken out of the path.
app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapThemePreference();
app.MapReadingPreference();
app.MapMediaFiles();
app.MapSiteHealth();
app.MapSearchEngineFiles();
app.MapRazorComponents<App>();

await app.RunAsync();
return 0;

/// <summary>The entry point of the website; public so that tests can start the app.</summary>
public partial class Program;
