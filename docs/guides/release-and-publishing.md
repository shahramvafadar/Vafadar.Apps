# Release and publishing

## Versioning

Each app is versioned independently.

| Property | Meaning | Example |
|---|---|---|
| `ApplicationDisplayVersion` | User-visible version, [Semantic Versioning](https://semver.org) `major.minor.patch` | `1.2.0` |
| `ApplicationVersion` | Store build number (Android `versionCode`, iOS `CFBundleVersion`); **must increase with every upload** | `7` |

* The values in the project file are for development; release builds pass them in (the release workflow asks for
  both).
* Tag every store release: `zanance/v1.2.0`, and add the changes to the app's `CHANGELOG.md`.
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

   `SYNCFUSION_LICENSE_KEY` must be available too (repository or environment secret). `MICROSOFT_ENTRA_CLIENT_ID` and
   `GOOGLE_OAUTH_CLIENT_ID_ANDROID` are optional: set them only for a release **with cloud backup** – that release then
   declares `INTERNET` and needs the matching privacy texts and Data safety answers (Zanance D-35).

## Per app: first Play Console release

- [ ] Create the app in Play Console with the package name from `ApplicationId` (`pro.vafadar.<app>`)
- [ ] Store listing in English, Persian, German, Spanish, French and Italian: title, short and full description, screenshots (phone, tablet),
      feature graphic, icon (512×512)
- [ ] App content: privacy policy URL, **Data safety** form (from the [privacy matrix](../privacy/privacy-matrix.md)),
      ads (none), content rating questionnaire, target audience, financial features declaration if applicable
- [ ] Google OAuth consent screen verified (if the app offers Google Drive backup); the Android OAuth client lists the SHA-1
      of the Play app signing key as well as of the upload key
- [ ] With cloud backup: Data safety declares the backup files sent to the user's own Google Drive / OneDrive at the
      user's request (encrypted, not collected by the developer), and the store texts no longer say "no internet"
- [ ] Upload to the **internal testing** track first, test on real devices (English and Persian), then promote to
      closed / open testing and production. New personal developer accounts must run a closed test with testers
      for a period before production access is granted – check the current Play Console requirements.

## Building a release

For a direct Android handoff, run `eng/scripts/Build-AndroidApk.ps1`. The script reports the
full path, package ID and SHA-256 after the Android SDK verifies the APK signature and its
manifest matches the producing project. Each attempt publishes to a fresh staging directory.
Only its verified signed APK replaces the same-named handoff file in the output directory.
A failed or empty publish leaves the prior handoff in place. Release handoffs reject debuggable APKs. With
`-Offline`, both INTERNET and ACCESS_NETWORK_STATE must be absent. Device installation and
signer continuity with the installed app are separate checks.

1. Update `CHANGELOG.md` of the app and merge to `main`.
2. GitHub → Actions → **Release (Android)** → *Run workflow*: choose the app, enter the display version and the next
   build number. Google Play caps the Android version code at 2100000000. The workflow verifies the allowed range; check the last used code in Play Console before choosing the next one.
3. Approve the `production` environment deployment.
   AAB signing uses temporary restricted password files because the Android SDK does not support the
   `env:` password prefix for bundles. The keystore and password files are removed after verification or failure.
4. The workflow requires exactly one signed App Bundle and reads each payload entry to verify its signature against the
   configured upload certificate. It also verifies 16 KiB ELF load alignment for every ARM64 and x64 native library in
   that bundle. Each base ABI must contain the qualified SQLCipher bytes, and the included manifest and notices must
   match the reviewed native package. The base manifest must contain the permanent package ID, requested display version and build number,
   with debug and test-only flags off. The decoded base manifest must declare a numeric target SDK of at least 36,
   the [Google Play mobile submission minimum](https://support.google.com/googleplay/android-developer/answer/11926878)
   effective 31 August 2026. Recheck that policy before each release and update `PLAY_MIN_TARGET_SDK` when it changes.
   Both editions must retain `allowBackup=false` and reference Zanance's legacy backup and modern data extraction
   rules from the base manifest. The verifier resolves both resources from the bundle's base resource table and
   checks the full-domain exclusions in every packaged configuration, including the default. Android and OEM
   transfer behavior still require their separate acceptance checks.
   Without a cloud-backup client ID, the bundle must omit INTERNET. A cloud-backup
   bundle must request INTERNET without an SDK cap. Both editions must omit ACCESS_NETWORK_STATE. The verifier
   requires the exact OneDrive browser callback for the configured Entra client and rejects an MSAL callback when
   no Entra client is configured. A correctly packaged callback still requires an actual Microsoft sign-in check.
   This workflow leaves personal sync and billing disabled. Play-generated APK packaging and runtime page-size
   acceptance need their own checks. Download the
   verified `.aab` artifact, upload it in Play Console, then **delete the artifact** (artifacts of public
   repositories are downloadable by any signed-in GitHub user; the workflow keeps them only 3 days).
5. Tag the commit: `git tag zanance/v1.2.0 && git push origin zanance/v1.2.0`.

Future improvement: upload directly to the Play internal track from the workflow (Google Play Developer API with a
service account), so no artifact is needed.

### Local split APK evidence

On 20 November 2026, the Android SDK's bundletool 1.17.0 generated four split APKs from the retained Zanance x64 AAB
(SHA-256 `151179293cfef173fb438cbf30630acfec12daf13f41e2f506189fcba28`). The synthetic device specification
selected API 36, x86_64, English and density 420. This required the installed build-tools 36.0.0 AAPT2 path because
the SDK's bundletool archive did not embed an AAPT2 binary.

All four splits passed APK signature verification with the same disposable test certificate and passed
`zipalign -c -P 16 4`. The master split reported `pro.vafadar.zanance`, version 0.1.0 and build number 1.
Although the 13 native libraries in the input AAB are compressed, the generated x64 split stores them
without compression. Each starts at a 16 KiB offset, and each library's SHA-256 matches the input.
AAB compression alone does not predict how libraries will be carried in APK splits.

A separate strict ARM64 Release build used the offline configuration and a disposable upload key with
`file:` password inputs, with no real store or provider credential. The signed AAB SHA-256 is
`3d63d62a911e1311dc00fb8f43e73a1035dc739ee2db6c6b0ffb72cb0b6a916c`.
The workflow verifiers accepted its upload certificate, release identity, absence of network permissions and
all 204 ELF libraries. Its decoded base manifest declares minimum API 24 and target API 36.
Its four locally generated ARM64/API 36 splits passed signature checks and `zipalign -c -P 16 4`.
The master APK retained the offline permissions policy, package and version, and all
204 libraries in the ARM64 split were stored at 16 KiB offsets with SHA-256 values equal to the AAB entries.

The retained x64 input bundle was later used to generate a fresh local split set with the same device
specification and a disposable test certificate. A fresh API 36 x86_64 emulator at density 420 accepted all
four splits in a single Package Manager session. The launcher opened the app, and the same process reached
the native Welcome screen after its preparation cover. The input bundle hash was unchanged and the fixture
was retired. No account or transaction was entered. The x64 example declares INTERNET, so airplane mode and
disabled Wi-Fi isolated the launch. The emulator reported 4096-byte kernel pages. The install and launch
receipts and native hierarchy are retained in `artifacts/zsi`.

Local evidence also includes the earlier device specifications, source digests, split signature and alignment
receipts, and native-entry comparisons. Play signing, ARM64 installation and execution on a device with
16 KiB pages remain separate acceptance checks. See the
[Android bundletool guide](https://developer.android.com/tools/bundletool).

## iOS (after the Android version is complete)

* Apple Developer Program membership; App Store Connect app with bundle id `pro.vafadar.<app>`.
* Signing certificate and provisioning profile; build on a Mac (or a macOS GitHub runner with the certificates as
  `production` secrets).
* App privacy details in App Store Connect (from the privacy matrix); `PrivacyInfo.xcprivacy` is already in
  `Platforms/iOS/Resources`.
* With cloud backup (Zanance D-50): the build needs `MicrosoftEntraClientId` and `GoogleOAuthClientIdIos` (environment
  variables or `Directory.Secrets.props` on the Mac). The App ID needs no extra capability: the keychain group
  `$(AppIdentifierPrefix)pro.vafadar.<app>` of `Platforms/iOS/Entitlements.plist` is the app's own. Check the URL
  schemes in the built Info.plist (`msauth.…`, `com.googleusercontent.apps.…`).
* TestFlight before release.

## Windows

* For personal use: `dotnet publish src/Apps/<App>/Vafadar.<App>.App -c Release -f net10.0-windows10.0.19041.0`
  produces an unpackaged app. With cloud backup (Zanance D-50) set `MicrosoftEntraClientId` and
  `GoogleOAuthClientIdWindows` (a Google *Desktop app* client; its client secret is not used) before publishing.
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
