# Vafadar.Authentication

Abstractions for signing in with external accounts (Google, Microsoft) and getting access tokens, plus the
platform-neutral parts of the browser sign-in. Platform implementations (MSAL for Microsoft, the Play services
authorization API for Google on Android, the iOS browser session, protected token storage) are in
[`Vafadar.Authentication.Maui`](../Vafadar.Authentication.Maui/README.md).
Design: [docs/architecture/authentication.md](../../../docs/architecture/authentication.md).

| Type | Purpose |
|---|---|
| `IAccessTokenProvider` | Valid access token for scopes, refreshed silently |
| `IExternalSignInService` | Interactive sign-in / sign-out, current account (extends `IAccessTokenProvider`) |
| `ExternalIdentityProvider` | `Google`, `Microsoft` – also the key for keyed DI registrations |
| `ExternalAccount` | The signed-in account (id, e-mail, display name) |
| `AuthenticationRequiredException` | The user must sign in (again) |
| `CloudSignInMatrix`, `SignInPlatform` | Provider availability: Microsoft and Google on Android, iOS and Windows, each with its configured client |
| `OAuth.PkceCodes` | PKCE verifier and `S256` challenge (RFC 7636), random `state` values |
| `OAuth.IAuthorizationBrowser` | The browser step of a sign-in: decides the redirect URI, opens the page, returns the redirect parameters |
| `OAuth.LoopbackAuthorizationBrowser` | Desktop browser step (RFC 8252 §7.3): one-time listener on `http://127.0.0.1:{port}/`, no administrator rights |
| `OAuth.IProtectedValueStore` | One secret value (a refresh token) in the platform's protected storage |
| `Google.GoogleInstalledAppSignInService` | Google sign-in for iOS and Windows: authorization code + PKCE without client secret, refresh, revoke |
| `Google.GoogleClientIds` | Validates Google client ids; reversed client id and redirect of iOS clients |

```csharp
services.AddKeyedSingleton<IExternalSignInService, MicrosoftSignInService>(ExternalIdentityProvider.Microsoft);
services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Microsoft,
    (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
```

Tests: `test/Libraries/Vafadar.Authentication.Tests`.
