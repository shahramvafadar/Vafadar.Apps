# Vafadar.Authentication.Maui

Sign-in for backups in the user's **own** cloud storage. Implements the abstractions of
[Vafadar.Authentication](../Vafadar.Authentication/README.md) for .NET MAUI. Design:
[docs/architecture/authentication.md](../../../docs/architecture/authentication.md).

| Type | Purpose |
|---|---|
| `CloudSignInOptions` | The OAuth clients of the app (one Microsoft client, one Google client per platform); an empty value means the provider is not offered |
| `CloudSignIn` | Which providers are available on this platform with the configured clients (`CloudSignInMatrix`) |
| `MicrosoftSignInService` | Microsoft sign-in with MSAL.NET through the system browser (Android, iOS, Windows) |
| `GoogleSignInService` | Google sign-in with the Google Identity authorization API of Play services (Android) |
| `GoogleInstalledAppSignInService` (Vafadar.Authentication) | Google sign-in with the system browser and PKCE (iOS, Windows), registered by `AddVafadarCloudSignIn` |
| `SignInActivityResults` | Android: passes `OnActivityResult` to Google's consent dialog and to MSAL |
| `SignInUrlCallbacks` | iOS: passes `AppDelegate.OpenUrl` to MSAL |

| Provider | Android | iOS | Windows |
|---|---|---|---|
| Microsoft (OneDrive) | ✅ MSAL, redirect `msal{client-id}://auth` | ✅ MSAL, redirect `msauth.{bundle-id}://auth`, token cache in the keychain | ✅ MSAL, redirect `http://localhost`, token cache with DPAPI |
| Google (Google Drive) | ✅ authorization API (package name + signing certificate of the Android OAuth client) | ✅ `ASWebAuthenticationSession` + PKCE, redirect `{reversed iOS client id}:/oauth2redirect`, refresh token in the keychain | ✅ default browser + PKCE, loopback `http://127.0.0.1:{port}/` (Desktop app client, no secret), refresh token with DPAPI |

```csharp
builder.Services.AddVafadarCloudSignIn(options =>
{
    options.MicrosoftClientId = AppSecrets.MicrosoftEntraClientId;
    options.GoogleAndroidClientId = AppSecrets.GoogleOAuthClientIdAndroid;
    options.GoogleIosClientId = AppSecrets.GoogleOAuthClientIdIos;
    options.GoogleWindowsClientId = AppSecrets.GoogleOAuthClientIdWindows;
    options.BrowserCompletionMessage = () => "…";   // Windows: page shown in the browser after a Google sign-in
});
builder.Services.AddGoogleDriveBackupStorage();   // only when CloudSignIn.IsGoogleAvailable
builder.Services.AddOneDriveBackupStorage();      // only when CloudSignIn.IsMicrosoftAvailable
```

Android app integration:

* `MainActivity.OnActivityResult` calls `SignInActivityResults.OnActivityResult(requestCode, resultCode, data)`.
* An activity derived from MSAL's `BrowserTabActivity` with the intent filter `msal{client-id}://auth` (Zanance:
  `Platforms/Android/MsalRedirectActivity.cs`, compiled only when a Microsoft client is configured).
* The `INTERNET` permission; Zanance keeps it only in builds with configured clients (decision D-35).

iOS app integration:

* `CFBundleURLTypes` with the schemes `msauth.{bundle-id}` (Microsoft) and the reversed iOS client id
  `com.googleusercontent.apps.{…}` (Google). Zanance generates them at build time from the configured clients
  (target `ZananceCloudUrlSchemes`), so the client id stays out of tracked files and an offline build registers none.
* `Entitlements.plist` with `keychain-access-groups` = `$(AppIdentifierPrefix){bundle-id}`: MSAL's token cache
  (`WithIosKeychainSecurityGroup(bundle-id)`) and the Google refresh token (`SecureStorage`) live there.
* `AppDelegate.OpenUrl` returns `SignInUrlCallbacks.OpenUrl(url) || base.OpenUrl(…)`.
* No broker (Microsoft Authenticator) is used, so no `LSApplicationQueriesSchemes` are needed.

Windows app integration: nothing beyond the client ids. MSAL listens on `http://localhost`; Google's sign-in opens
the default browser and listens once on `127.0.0.1` with a free port.

Principles: only the app-folder scopes (`drive.appdata`, `Files.ReadWrite.AppFolder`) plus `openid`/`email` for Google
to show the account; no password and no client secret is ever handled by the app; tokens stay on the device (Play
services; MSAL's protected storage on Android; the keychain on iOS; DPAPI on Windows); signing out removes the cached
tokens (Google: the grant is revoked). Nothing here has been verified on a device with real OAuth clients yet.
