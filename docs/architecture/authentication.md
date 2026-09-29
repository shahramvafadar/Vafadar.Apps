# Authentication

There are two different reasons an app may need a user to sign in:

1. **Access to the user's cloud storage** (Google Drive, OneDrive) for backups. The app acts on the user's behalf;
   the developer never sees the data. This is needed by almost every app.
2. **An account with a Vafadar backend** (only for apps with a web version or sync). This is needed rarely and is
   designed when the first such app starts.

## 1. External accounts for cloud storage

### Abstractions (Vafadar.Authentication)

| Type | Role |
|---|---|
| `IAccessTokenProvider` | Returns a valid access token for scopes; refreshes silently; throws `AuthenticationRequiredException` if interaction is needed |
| `IExternalSignInService` | Adds interactive sign-in, sign-out and the current `ExternalAccount` |
| `ExternalIdentityProvider` | `Google`, `Microsoft`; used as the key for keyed DI registrations |

Storage implementations resolve their token provider by key:

```csharp
services.AddKeyedSingleton<IExternalSignInService, GoogleSignInService>(ExternalIdentityProvider.Google);
services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Google,
    (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
services.AddGoogleDriveBackupStorage();
```

### Implementations (Vafadar.Authentication.Maui)

See the [library README](../../src/Libraries/Vafadar.Authentication.Maui/README.md) for the platform matrix.

| Provider | Library / flow | Registration | Scope |
|---|---|---|---|
| Microsoft | [MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/) (`Microsoft.Identity.Client`) with the system browser; Android and Windows | Microsoft Entra app registration, *personal + work/school accounts*, public client (mobile/desktop), redirects `msal{client-id}://auth` (Android, with the package signature hash) and `http://localhost` (Windows) | `Files.ReadWrite.AppFolder` |
| Google | The **Google Identity authorization API** of Play services (`Xamarin.GooglePlayServices.Auth`) on Android: Google shows its own consent dialog, Play services returns access tokens; no redirect URI and no refresh token in the app | Google Cloud project with an **Android** OAuth client (package name `pro.vafadar.<app>` + SHA-1 of the signing certificate: upload key and Play app signing key) | `https://www.googleapis.com/auth/drive.appdata` plus `openid email` to show the account |

Decisions taken (Zanance D-35, 2026-09-29):

* **Google on Android** uses the authorization API, because Google blocks custom URI scheme redirects for Android
  OAuth clients. Google on Windows (needs a desktop client) and both providers on iOS (keychain group entitlement and
  URL scheme, needs a Mac) follow later.
* **Token storage**: Play services keeps Google's grant; MSAL keeps its cache in its own protected storage on Android
  and in a DPAPI-protected file on Windows (`Microsoft.Identity.Client.Extensions.Msal`). The app stores only the
  e-mail address of the Google account to show it.
* **Opt-in and offline builds**: a provider is offered only when its client id is configured for the build; a build
  without any client has no cloud backup and keeps removing the `INTERNET` permission.
* **Google OAuth verification**: an app used by the public needs a verified consent screen (name, logo, home page and
  privacy policy on `vafadar.pro`, justification per scope). `drive.appdata` is not a restricted scope, which keeps
  verification simple.

### Design principles

* Request **minimal scopes** – only the app's own folder, never the whole Drive/OneDrive.
* Sign-in is **optional**: the app is fully usable without an account; the account is only for backup.
* One sign-in per provider per app; the signed-in account is shown in backup settings with a "Sign out" action that
  also removes cached tokens.
* Client IDs are not secrets, but they are configured through the same build-time mechanism as secrets
  (`Directory.Secrets.props` / CI secrets) so the public repository stays free of project-specific values.

## 2. Backend accounts (future)

When an app gets a web version or sync (see [web-and-shared-data.md](web-and-shared-data.md)):

* Prefer **Microsoft Entra External ID** (managed, free tier, social logins with Google/Microsoft) or
  **ASP.NET Core Identity** hosted with the app's API, issuing tokens to the mobile app.
* Mobile apps use the same `IExternalSignInService` pattern against the backend's identity provider.
* A shared library (e.g. `Vafadar.Identity`) is created once two apps need it.
