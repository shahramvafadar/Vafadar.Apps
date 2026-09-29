# Vafadar.Authentication.Maui

Sign-in for backups in the user's **own** cloud storage. Implements the abstractions of
[Vafadar.Authentication](../Vafadar.Authentication/README.md) for .NET MAUI. Design:
[docs/architecture/authentication.md](../../../docs/architecture/authentication.md).

| Type | Purpose |
|---|---|
| `CloudSignInOptions` | The OAuth clients of the app; an empty value means the provider is not offered |
| `CloudSignIn` | Which providers are available on this platform with the configured clients |
| `MicrosoftSignInService` | Microsoft sign-in with MSAL.NET through the system browser (Android, Windows) |
| `GoogleSignInService` | Google sign-in with the Google Identity authorization API of Play services (Android) |
| `SignInActivityResults` | Android: passes `OnActivityResult` to Google's consent dialog and to MSAL |

| Provider | Android | Windows | iOS |
|---|---|---|---|
| Microsoft (OneDrive) | ✅ MSAL, redirect `msal{client-id}://auth` | ✅ MSAL, redirect `http://localhost`, token cache with DPAPI | 🔜 with the iOS release (keychain group entitlement, URL scheme) |
| Google (Google Drive) | ✅ authorization API (package name + signing certificate of the Android OAuth client) | – (needs a desktop OAuth client) | 🔜 with the iOS release |

```csharp
builder.Services.AddVafadarCloudSignIn(options =>
{
    options.MicrosoftClientId = AppSecrets.MicrosoftEntraClientId;
    options.GoogleAndroidClientId = AppSecrets.GoogleOAuthClientIdAndroid;
});
builder.Services.AddGoogleDriveBackupStorage();   // only when CloudSignIn.IsGoogleAvailable
builder.Services.AddOneDriveBackupStorage();      // only when CloudSignIn.IsMicrosoftAvailable
```

Android app integration:

* `MainActivity.OnActivityResult` calls `SignInActivityResults.OnActivityResult(requestCode, resultCode, data)`.
* An activity derived from MSAL's `BrowserTabActivity` with the intent filter `msal{client-id}://auth` (Zanance:
  `Platforms/Android/MsalRedirectActivity.cs`, compiled only when a Microsoft client is configured).
* The `INTERNET` permission; Zanance keeps it only in builds with configured clients (decision D-35).

Principles: only the app-folder scopes (`drive.appdata`, `Files.ReadWrite.AppFolder`) plus `openid`/`email` for Google
to show the account; no password is ever handled by the app; tokens stay on the device (Play services, MSAL's
protected storage on Android, DPAPI on Windows); signing out removes the cached tokens (Google: the grant is revoked).
Nothing here has been verified on a device with real OAuth clients yet.
