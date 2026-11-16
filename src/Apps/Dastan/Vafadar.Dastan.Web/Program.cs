using Vafadar.Dastan.Web.Components;
using Vafadar.Dastan.Web.Site;
using Vafadar.Web;

var builder = WebApplication.CreateBuilder(args);

// Pages are rendered on the server for every request. No page keeps a connection open and none needs a script
// (ADR 0003), so nothing of one visitor can reach another.
builder.Services.AddRazorComponents();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

// English and Persian (Directory.Build.props: VafadarLanguages). The first language is used when nothing else decides.
builder.Services.AddSiteLanguages("en", "fa");
builder.Services.Configure<SiteLanguageOptions>(options =>
{
    options.UnlocalizedPaths.Add("/preferences");
    options.UnlocalizedPaths.Add(MediaAddress.Root);
});

builder.Services.Configure<DastanOptions>(builder.Configuration.GetSection("Dastan"));
builder.Services.AddSingleton<DastanSite>();
builder.Services.AddScoped<Visitor>();

var app = builder.Build();

// The collection is read now: a collection that cannot be used stops the app with the list of what is wrong,
// instead of failing for the first visitor.
app.Services.GetRequiredService<DastanSite>();

app.UseSecurityHeaders();
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

// Routes are matched after the language was taken out of the path.
app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapThemePreference();
app.MapReadingPreference();
app.MapMediaFiles();
app.MapRazorComponents<App>();

app.Run();

/// <summary>The entry point of the website; public so that tests can start the app.</summary>
public partial class Program;
