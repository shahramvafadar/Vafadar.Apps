# Vafadar.Web

What every web app of this repository needs and no app should write twice. A Razor class library for ASP.NET Core
(`Microsoft.AspNetCore.App`), without texts and without the vocabulary of an app.

| Part | What it does |
|---|---|
| `AddSiteLanguages`, `UseSiteLanguage` | The language of a page is the beginning of its address (`/fa/…`). The middleware makes it the culture of the request, moves it from the path into the path base – so routes are written once and relative links stay in the language – and redirects a page address without a language to the visitor's language. |
| `SiteLanguage`, `TextDirection` | Language tag, culture and `dir` of the request. The direction comes from the culture: Persian, Arabic, Kurdish (Sorani), Hebrew and Urdu are right to left without being listed. |
| `ThemePreference`, `MapThemePreference` | The visitor's choice of light, dark or "like my device", as a cookie the server writes into the page (`data-theme`). No script, no flash of the wrong theme. |
| `PreferenceCookie`, `ReturnAddress` | One set of cookie options for remembered choices; a return address after a form that can only be a path of the site. |
| `UseSecurityHeaders` | Content security policy (only what the site serves itself), no content-type guessing, no framing, a short referrer, no device features. |
| `_content/Vafadar.Web/vafadar.css` | Reset, focus ring, skip link, `.visually-hidden`, reduced motion. No colours: the app defines the tokens named at the top of the file. Logical properties only. |

## Use

```csharp
builder.Services.AddSiteLanguages("en", "fa");            // the first is the default
builder.Services.Configure<SiteLanguageOptions>(o => o.UnlocalizedPaths.Add("/preferences"));
builder.Services.AddAntiforgery();

app.UseSecurityHeaders();
app.UseSiteLanguage();   // before UseRouting – call UseRouting explicitly after it
app.UseRouting();
app.UseAntiforgery();
app.MapThemePreference();
```

In the root component: `<html lang="@language.Tag" dir="@language.Direction" data-theme="@ThemePreference.Attribute(theme)">`
and `<base href="@(Request.PathBase)/">`, with `language = HttpContext.GetSiteLanguage()`.

## Rules the library keeps

* **One address per page.** `/FA/x` and `/fa` are redirected permanently to `/fa/x` and `/fa/`.
* **A page address always has a language.** Without one, `GET` and `HEAD` are redirected (302, `no-store`,
  `Vary: Accept-Language, Cookie`) to the language of the last page the visitor opened, else the first language of
  the browser that the site has, else the default. Files (an address with an extension) and the configured
  `UnlocalizedPaths` carry no language and are served as they are – in the visitor's language where they produce text.
* **The culture belongs to the request.** It is set on the request's own execution context; requests in different
  languages at the same time do not see each other (tested with concurrent requests).
* **Cookies of forms belong to the site.** Because the language is the path base, the framework would scope the
  antiforgery cookie to `/fa`; `AddSiteLanguages` sets its path to `/`, otherwise every form posting to an address
  without a language is refused by a real browser.
* **Cookies hold a choice, never a person**: `vafadar.language`, `vafadar.theme`; HttpOnly, SameSite=Lax, one year,
  Secure on HTTPS.

## Not in the library

Texts, colours, fonts and layout of an app; authentication; anything that needs a script. The Syncfusion license
registration for Blazor is added with the first web app that uses a Syncfusion component (Dastan's M0 uses none).

Tests: `test/Libraries/Vafadar.Web.Tests` (a small site in three languages on the test server).
Decision: [ADR 0013](../../../docs/adr/0013-web-library.md).
