using Vafadar.Dastan.Store;
using Vafadar.Dastan.Studio.Components;
using Vafadar.Dastan.Studio.Site;
using Vafadar.Web;

var builder = WebApplication.CreateBuilder(args);

// Pages are rendered on the server for every request, without a connection that stays open and without a script,
// as the pages of the site are (ADR 0003).
builder.Services.AddRazorComponents();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

builder.Services.AddSiteLanguages("en", "fa");
builder.Services.Configure<SiteLanguageOptions>(options => options.UnlocalizedPaths.Add("/preferences"));

builder.Services.Configure<StudioOptions>(builder.Configuration.GetSection("Studio"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<StudioStore>();

// The store is opened for a request and closed with it: its database is one file, and nothing is held between
// two requests.
builder.Services.AddScoped<EditorialStore>(services => services.GetRequiredService<StudioStore>().Open());
builder.Services.AddScoped<Editor>();

var app = builder.Build();

// The store is opened now, and brought to the form this program knows: a tool that cannot open its store stops
// with the reason instead of failing for the first page.
app.Services.GetRequiredService<StudioStore>();

// The tool shows what is not published and does not know who asks: it answers this machine and nobody else.
app.UseOnlyThisMachine();
app.UseSecurityHeaders();
app.Use((context, next) =>
{
    // No page of the tool is for a search engine, and none is kept by a browser or a cache: what a page shows
    // is the store at the moment it was asked.
    context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    context.Response.OnStarting(() =>
    {
        if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true && context.Response.Headers.CacheControl.Count == 0)
        {
            context.Response.Headers.CacheControl = "no-store";
        }

        return Task.CompletedTask;
    });
    return next(context);
});

app.UseSiteLanguage();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseWhen(
    context => HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method),
    pages => pages.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapThemePreference();
app.MapRazorComponents<App>();

app.Run();

/// <summary>The entry point of the tool of the editors; public so that tests can start it.</summary>
public partial class Program;
