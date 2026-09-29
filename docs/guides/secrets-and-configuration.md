# Secrets and configuration

The repository is public. **Nothing secret or account-specific may ever be committed.**

## What counts as a secret here

| Value | Used by | Where it lives |
|---|---|---|
| Syncfusion license key | All apps (`SyncfusionLicenseKey`) | `Directory.Secrets.props` / GitHub secret `SYNCFUSION_LICENSE_KEY` |
| Google OAuth client id (Android) | Cloud backup: Google Drive (`GoogleOAuthClientIdAndroid`) | `Directory.Secrets.props` / GitHub secret `GOOGLE_OAUTH_CLIENT_ID_ANDROID` |
| Microsoft Entra client id | Cloud backup: OneDrive (`MicrosoftEntraClientId`) | `Directory.Secrets.props` / GitHub secret `MICROSOFT_ENTRA_CLIENT_ID` |
| Android upload keystore + passwords | Release signing | Password manager + GitHub `production` environment secrets |
| Apple certificates / profiles (later) | iOS release | Keychain + GitHub `production` environment secrets |

OAuth client ids of mobile apps are technically public identifiers, but they are kept out of the repository anyway
so that forks do not use the same Google/Microsoft projects.

## Local development

1. Copy `Directory.Secrets.props.example` to `Directory.Secrets.props` (repository root). The file is git-ignored.
2. Fill in the values you have. Empty values are fine; the related feature is then unavailable (or, for Syncfusion,
   shows a license banner).

## How secrets reach the app

At build time `eng/AppSecrets.targets` generates `obj/.../AppSecrets.g.cs` for every project that declares
`AppSecret` items:

```csharp
internal static class AppSecrets
{
    internal const string SyncfusionLicenseKey = """...""";
}
```

MSBuild properties come from `Directory.Secrets.props` locally and from environment variables in CI.
Other secrets (e.g. OAuth client ids) are declared by the app that needs them:

```xml
<ItemGroup Label="Build-time secrets (see eng/AppSecrets.targets)">
  <AppSecret Include="GoogleOAuthClientIdAndroid" Value="$(GoogleOAuthClientIdAndroid)" />
</ItemGroup>
```

### Cloud backup clients (Zanance, D-35)

Both values are optional. **Without them the app has no cloud backup and the Android release build has no
`INTERNET` permission**; with one of them the provider is offered (as an explicit choice of the user) and the release
build keeps `INTERNET`.

| Property | Where to create it | Notes |
|---|---|---|
| `MicrosoftEntraClientId` | Microsoft Entra admin center → App registrations → *Accounts in any organizational directory and personal Microsoft accounts* | Platform *Mobile and desktop*: redirect `msal{client-id}://auth` (Android, add the package name and the signature hash of the upload and the Play app signing key) and `http://localhost` (Windows). API permission `Files.ReadWrite.AppFolder` (delegated) |
| `GoogleOAuthClientIdAndroid` | Google Cloud console → APIs & Services → Credentials → OAuth client *Android* | Package name `pro.vafadar.zanance`, SHA-1 of the upload key **and** of the Play app signing key (one client each), Google Drive API enabled, consent screen with `drive.appdata`, `openid`, `email` |

The app reads them through `eng/AppSecrets.targets` (`AppSecrets.MicrosoftEntraClientId`,
`AppSecrets.GoogleOAuthClientIdAndroid`). A configured Microsoft client also compiles the MSAL redirect activity
(`CLOUD_MICROSOFT`). The release workflow passes the repository secrets `MICROSOFT_ENTRA_CLIENT_ID` and
`GOOGLE_OAUTH_CLIENT_ID_ANDROID`; leave them unset for an offline release.

### Syncfusion license (shared by all apps)

```
App (MauiProgram) -> UseVafadar (Vafadar.Maui) -> SyncfusionLicense.Register -> SyncfusionLicenseProvider.RegisterLicense
```

* Every MAUI app (a project with `UseMaui` and an `ApplicationId`) gets the `SyncfusionLicenseKey` app secret
  automatically from `eng/AppSecrets.targets`; app projects do not declare it.
* The app only passes it to the shared bootstrap: `options.SyncfusionLicenseKey = AppSecrets.SyncfusionLicenseKey;`.
  `Vafadar.Maui` registers it once, before any Syncfusion control is created. Registration and validation are
  offline; there is no network call.
* An app that ever needs a different key sets its own `SyncfusionLicenseKey` MSBuild property (or declares its own
  `AppSecret` item); the registration code stays shared.
* The key is never logged, shown in errors or written to documentation.
* `test/Libraries/Vafadar.SyncfusionLicense.Tests` validates the key against the Syncfusion version in
  `Directory.Packages.props`. It is skipped without a key, runs in CI, and is required by the release workflow, so an
  outdated key (Syncfusion keys are tied to a major version) stops a release instead of shipping a license notice.
  After a Syncfusion upgrade, run it locally: `dotnet test --project test/Libraries/Vafadar.SyncfusionLicense.Tests`.
> Values compiled into an app can be extracted from the app package. This mechanism keeps secrets out of the source
> code, not out of the binary. Never compile a value into an app that would be harmful if extracted (for example a
> server-side API key); such values belong on a server.

## GitHub configuration

Repository → Settings:

* **Secrets and variables → Actions → Repository secrets**: `SYNCFUSION_LICENSE_KEY`.
* **Environments → `production`** (with yourself as required reviewer): `ANDROID_KEYSTORE_BASE64`,
  `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_PASSWORD` (see the [release guide](release-and-publishing.md)).
* **Code security**: enable *Secret scanning*, *Push protection*, *Dependabot alerts* and
  *Private vulnerability reporting*.

## If a secret leaks

1. **Rotate it** immediately (new Syncfusion key in your Syncfusion account, new OAuth client secret/id, new keystore
   only if the upload key leaked – Google Play can reset the upload key).
2. Update the local file and GitHub secrets.
3. Removing the commit is not enough: forks and caches may already contain it.
