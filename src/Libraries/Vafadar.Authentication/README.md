# Vafadar.Authentication

Abstractions for signing in with external accounts (Google, Microsoft) and getting access tokens. Platform
implementations (MSAL for Microsoft, the Play services authorization API for Google) are in
[`Vafadar.Authentication.Maui`](../Vafadar.Authentication.Maui/README.md).
Design: [docs/architecture/authentication.md](../../../docs/architecture/authentication.md).

| Type | Purpose |
|---|---|
| `IAccessTokenProvider` | Valid access token for scopes, refreshed silently |
| `IExternalSignInService` | Interactive sign-in / sign-out, current account (extends `IAccessTokenProvider`) |
| `ExternalIdentityProvider` | `Google`, `Microsoft` – also the key for keyed DI registrations |
| `ExternalAccount` | The signed-in account (id, e-mail, display name) |
| `AuthenticationRequiredException` | The user must sign in (again) |

```csharp
services.AddKeyedSingleton<IExternalSignInService, MicrosoftSignInService>(ExternalIdentityProvider.Microsoft);
services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Microsoft,
    (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
```
