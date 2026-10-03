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
| `CloudSignInMatrix`, `SignInPlatform` | The provider availability matrix (which provider on which platform with which client id) |
| `OAuth.PkceCodes`, `OAuth.IAuthorizationBrowser`, `OAuth.LoopbackAuthorizationBrowser`, `OAuth.IProtectedValueStore` | Platform-neutral pieces of the browser flow: PKCE, the browser step (loopback listener on the desktop), protected storage of a refresh token |
| `Google.GoogleInstalledAppSignInService`, `Google.GoogleClientIds` | Google sign-in for installed apps (iOS, Windows): code + PKCE, refresh, revoke; the reversed client id of iOS |

The platform-neutral parts are unit-tested in `test/Libraries/Vafadar.Authentication.Tests` (RFC 7636 example,
real loopback sockets, the Google token protocol against a fake HTTP handler).

Storage implementations resolve their token provider by key:

```csharp
services.AddKeyedSingleton<IExternalSignInService, GoogleSignInService>(ExternalIdentityProvider.Google); // Android
services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Google,
    (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
services.AddGoogleDriveBackupStorage();
```

### Implementations (Vafadar.Authentication.Maui)

See the [library README](../../src/Libraries/Vafadar.Authentication.Maui/README.md) for the integration steps. Which
provider is offered where is decided in one place, `CloudSignInMatrix` (Vafadar.Authentication):

| Provider | Android | iOS | Windows |
|---|---|---|---|
| Microsoft (OneDrive) | MSAL, redirect `msal{client-id}://auth` | MSAL, redirect `msauth.{bundle-id}://auth`, keychain cache | MSAL, redirect `http://localhost`, DPAPI cache |
| Google (Google Drive) | Play services authorization API (Android client) | System browser + PKCE, redirect `{reversed iOS client id}:/oauth2redirect` (iOS client) | System browser + PKCE, loopback `http://127.0.0.1:{port}/` (Desktop app client) |

| Provider | Library / flow | Registration | Scope |
|---|---|---|---|
| Microsoft | [MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/) (`Microsoft.Identity.Client`) with the system browser on all three platforms; no broker (Authenticator) | One Microsoft Entra app registration, *personal + work/school accounts*, public client, no client secret; platforms: Android (`msal{client-id}://auth`, package name + signature hash), iOS/macOS (bundle id → `msauth.{bundle-id}://auth`), Mobile and desktop (`http://localhost`) | `Files.ReadWrite.AppFolder` |
| Google, Android | The **Google Identity authorization API** of Play services (`Xamarin.GooglePlayServices.Auth`): Google shows its own consent dialog, Play services returns access tokens; no redirect URI and no refresh token in the app | **Android** OAuth client (package name `pro.vafadar.<app>` + SHA-1 of each signing certificate) | `https://www.googleapis.com/auth/drive.appdata` plus `openid email` to show the account |
| Google, iOS | `GoogleInstalledAppSignInService` (Vafadar.Authentication): authorization code + PKCE (`S256`) in `ASWebAuthenticationSession` through MAUI's `WebAuthenticator`; the redirect uses the reversed client id as URL scheme | **iOS** OAuth client (bundle id `pro.vafadar.<app>`); iOS clients have no secret | same |
| Google, Windows | `GoogleInstalledAppSignInService` with `LoopbackAuthorizationBrowser`: the default browser, a one-time listener on `127.0.0.1` with a port chosen by Windows, authorization code + PKCE | **Desktop app** OAuth client; its secret is optional with PKCE and is **not** used | same |

Decisions taken (Zanance D-35, 2026-09-29; extended by D-50, 2026-10-03):

* **Google on Android** uses the authorization API, because Google blocks custom URI scheme redirects for Android
  OAuth clients.
* **Google on iOS and Windows** use the protocol of Google's *OAuth 2.0 for iOS & Desktop Apps*: the same flow the
  Google Sign-In SDK for iOS runs internally (system browser session, PKCE, reversed client id scheme). The SDK itself
  is not used because no maintained .NET binding of a current version exists; the flow needs no extra dependency.
  Google recommends its SDK for iOS; if a supported binding appears, only the iOS browser part has to change.
* **No client secret anywhere.** iOS clients have none; for Desktop clients Google documents the secret as optional,
  and with PKCE the authorization code is useless without the verifier that never leaves the app.
* **Loopback only on the desktop**: `http://127.0.0.1:{port}/` (not `localhost`, which Google warns may be blocked by
  firewalls), bound to the loopback interface, open only during one sign-in (at most 5 minutes). Google deprecated the
  loopback redirect for Android, iOS and Chrome clients, so it is never used there.
* **Token storage**: Play services keeps Google's grant on Android; MSAL keeps its cache in its own protected storage
  on Android, in the keychain on iOS (group `$(AppIdentifierPrefix){bundle-id}`, entitlement `keychain-access-groups`)
  and in a DPAPI-protected file on Windows (`Microsoft.Identity.Client.Extensions.Msal`). The Google refresh token of
  iOS and Windows is kept the same way (keychain through `SecureStorage`; DPAPI file through the MSAL extensions'
  `Storage`), together with the account e-mail and the granted scopes; access tokens stay in memory. Signing out
  removes the stored tokens and revokes the Google grant.
* **Granular consent**: Google lets the user untick single scopes. A Google sign-in without `drive.appdata` is
  refused (and its grant revoked), because the backup could not work.
* **Opt-in and offline builds**: a provider is offered only when its client id for that platform is configured for
  the build; a build without any client has no cloud backup and the Android release keeps removing the `INTERNET`
  permission. iOS URL schemes are generated from the configured clients at build time, so an offline iOS build
  registers none.
* **Google OAuth verification**: an app used by the public needs a verified consent screen (name, logo, home page and
  privacy policy on `vafadar.pro`, justification per scope). `drive.appdata` is not a restricted scope, which keeps
  verification simple. All three Google clients belong to the same Google Cloud project and consent screen.
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
