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

### Implementations (planned: Vafadar.Authentication.Maui)

| Provider | Library / flow | Registration | Scope |
|---|---|---|---|
| Microsoft | [MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/) (`Microsoft.Identity.Client`) with the system browser (and broker where available) | Microsoft Entra app registration, *personal + work/school accounts*, public client, redirect `msal{client-id}://auth` | `Files.ReadWrite.AppFolder` |
| Google | OAuth 2.0 authorization code + PKCE through the system browser (MAUI `WebAuthenticator`) or Google's native Android authorization API | Google Cloud project, OAuth clients per platform (Android, iOS, desktop) | `https://www.googleapis.com/auth/drive.appdata` |

Open points to settle when implementing (tracked in the [roadmap](../roadmap.md)):

* **Google on Android**: Google restricts custom URI scheme redirects for Android OAuth clients. The options are the
  native Google Identity Services authorization API (needs an Android binding) or an alternative redirect setup.
  Choose the currently recommended approach at implementation time.
* **Google OAuth verification**: an app used by the public needs a verified OAuth consent screen: app name, logo,
  a home page and privacy policy on the verified domain `vafadar.pro`, and a justification per scope. Minimal scopes
  (`drive.appdata` only) keep verification simple.
* **Token storage**: refresh tokens are stored with MAUI `SecureStorage` (Keychain / Android Keystore); MSAL gets a
  token cache backed by `SecureStorage`.

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
