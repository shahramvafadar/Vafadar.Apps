# 0013. A shared library for web apps: language in the address, choices in cookies, no script

- Status: Accepted (implemented and verified on 2026-11-16)
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Related: [0011](0011-languages-per-app.md) (languages per app); Dastan decisions D-029 and D-033; Dastan
  [ADR 0003](../../src/Apps/Dastan/docs/adr/0003-server-rendered-pages.md)

## Context

Dastan is the first web app of the repository. Its pages need a language in the address, the direction of the
text, a light and a dark appearance, remembered choices and safe response headers. None of that is about stories; a
second web app would need the same. `Vafadar.Localization` cannot be used on a server: it keeps the language in
process-wide state, which is right for a phone and wrong for many visitors at once.

## Decision

1. **`Vafadar.Web` holds what is common to web apps and nothing of an app**: no texts, no colours, no vocabulary.
   It is a Razor class library so that it can carry a base style sheet.
2. **The language is the first segment of the address** and is moved into the path base of the request. Routes,
   links and components are written once; a relative link stays in its language; the other language is the same
   path under another prefix. The culture is set per request.
3. **A page address without a language is redirected**, not served: one address per page and language.
4. **Remembered choices are cookies the server reads** (language, theme, an app's own). They are written into the
   page by the server, so no script is needed and nothing flashes. They hold a choice, not an identity.
5. **Forms post to addresses without a language**, are protected by antiforgery tokens and go back to a path of the
   site only.
6. **Direction comes from the culture**, never from a list of languages or a test for Persian.
7. **Security headers are on for every response**, with a policy that allows only the site's own files.

## Consequences

* Because the language is the path base, the app has to call `UseRouting` after `UseSiteLanguage`, and the
  antiforgery cookie has to belong to the whole site. The second point was found in a real browser, not by the
  first HTTP tests; the test browsers now honour the path of a cookie.
* Static files are reachable with and without a language prefix (the same file under several addresses).
* `Vafadar.Localization` stays as it is for the MAUI apps. Web apps use ASP.NET Core localization with their own
  resource files.
* Component libraries that need a script or a live connection are not part of this library; an app adds them
  where it decides to.
