# Release and publishing

## Versioning

Each app is versioned independently.

| Property | Meaning | Example |
|---|---|---|
| `ApplicationDisplayVersion` | User-visible version, [Semantic Versioning](https://semver.org) `major.minor.patch` | `1.2.0` |
| `ApplicationVersion` | Store build number (Android `versionCode`, iOS `CFBundleVersion`); **must increase with every upload** | `7` |

* The values in the project file are for development; release builds pass them in (the release workflow asks for
  both).
* Tag every store release: `finance/v1.2.0`, and add the changes to the app's `CHANGELOG.md`.
* Backups record the app version; a backup can only be restored by the same or a newer version.

## One-time setup: Google Play

1. **Developer account** – register at [Google Play Console](https://play.google.com/console) (one-time fee).
   Developer name: *Shahram Vafadar*. Verify identity as required by Google.
2. **Website** – publish a home page and privacy policy under `vafadar.pro` (see [privacy](../privacy/README.md)).
3. **Upload key** – create it once and keep it (and its passwords) in a password manager **and** an offline backup:

   ```powershell
   keytool -genkeypair -v -keystore vafadar-upload.keystore -alias vafadar-upload `
     -keyalg RSA -keysize 4096 -validity 10000
   ```

   With **Play App Signing** (default for new apps) Google holds the app signing key; this upload key only proves
   that uploads come from you and can be reset through Google support if it is lost.
4. **GitHub environment** `production` (Settings → Environments), with yourself as required reviewer, holding:

   | Secret | Value |
   |---|---|
   | `ANDROID_KEYSTORE_BASE64` | `[Convert]::ToBase64String([IO.File]::ReadAllBytes("vafadar-upload.keystore"))` |
   | `ANDROID_KEY_ALIAS` | `vafadar-upload` |
   | `ANDROID_KEYSTORE_PASSWORD` | keystore password |
   | `ANDROID_KEY_PASSWORD` | key password |

   `SYNCFUSION_LICENSE_KEY` must be available too (repository or environment secret).

## Per app: first Play Console release

- [ ] Create the app in Play Console with the package name from `ApplicationId` (`pro.vafadar.<app>`)
- [ ] Store listing in English, Persian and German: title, short and full description, screenshots (phone, tablet),
      feature graphic, icon (512×512)
- [ ] App content: privacy policy URL, **Data safety** form (from the [privacy matrix](../privacy/privacy-matrix.md)),
      ads (none), content rating questionnaire, target audience, financial features declaration if applicable
- [ ] Google OAuth consent screen verified (if the app offers Google Drive backup)
- [ ] Upload to the **internal testing** track first, test on real devices (English and Persian), then promote to
      closed / open testing and production. New personal developer accounts must run a closed test with testers
      for a period before production access is granted – check the current Play Console requirements.

## Building a release

1. Update `CHANGELOG.md` of the app and merge to `main`.
2. GitHub → Actions → **Release (Android)** → *Run workflow*: choose the app, enter the display version and the next
   build number.
3. Approve the `production` environment deployment.
4. Download the `.aab` artifact, upload it in Play Console, then **delete the artifact** (artifacts of public
   repositories are downloadable by any signed-in GitHub user; the workflow keeps them only 3 days).
5. Tag the commit: `git tag finance/v1.2.0 && git push origin finance/v1.2.0`.

Future improvement: upload directly to the Play internal track from the workflow (Google Play Developer API with a
service account), so no artifact is needed.

## iOS (after the Android version is complete)

* Apple Developer Program membership; App Store Connect app with bundle id `pro.vafadar.<app>`.
* Signing certificate and provisioning profile; build on a Mac (or a macOS GitHub runner with the certificates as
  `production` secrets).
* App privacy details in App Store Connect (from the privacy matrix); `PrivacyInfo.xcprivacy` is already in
  `Platforms/iOS/Resources`.
* TestFlight before release.

## Windows

* For personal use: `dotnet publish src/Apps/<App>/Vafadar.<App>.App -c Release -f net10.0-windows10.0.19041.0`
  produces an unpackaged app.
* Microsoft Store (optional, later): switch to MSIX packaging (`WindowsPackageType=MSIX`) and a Store publisher
  identity.

## Release checklist

- [ ] All tests green on `main`, no warnings
- [ ] Tested on a real Android device in English, Persian (RTL) and German
- [ ] Backup → reinstall → restore tested with the release build
- [ ] Database migrations tested by upgrading from the previous store version (not only fresh installs)
- [ ] Privacy matrix and Data safety form still correct
- [ ] Store listing and screenshots up to date in all languages
- [ ] `CHANGELOG.md` updated, version and build number bumped, tag pushed
