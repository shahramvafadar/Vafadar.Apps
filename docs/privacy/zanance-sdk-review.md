# Zanance SDK and permission review (SEC-09, D-69)

Reviewed 2026-10-08 against the current application sources, resolved Android libraries and complete signed Release
APKs. This is the current local/cloud-backup baseline. Billing, identity backend, sync, AI and bank SDKs are absent;
repeat the review before any is introduced. Source checks do not prove native runtime traffic or store acceptance.

## Build boundaries

| Variant/platform | Network boundary | Disclosure / evidence |
|---|---|---|
| Android Release Offline | INTERNET removed; ACCESS_NETWORK_STATE removed | Explicit build with all OAuth configuration properties empty, without changing the local secrets file. Shipped APK permission guard verifies the result |
| Android Release Cloud | INTERNET present; ACCESS_NETWORK_STATE removed | Provider connection is optional, but INTERNET applies to the whole process before connection. SDK-native transport is not gated by the user's cloud sign-in |
| Android Debug | INTERNET may be added for debugging | Never use Debug as evidence of the offline Release boundary; the guard rejects debuggable APKs |
| iOS / Windows | No Android-style per-app INTERNET denial | Application providers require configured clients. OCR uses system engines. Sources reviewed; iOS final binary/runtime needs Mac and signing. Do not claim an OS-level offline network restriction |

Offline selection is build configuration, not a runtime privacy toggle. It removes provider clients from generated
application configuration, preserves the owner's local configuration and leaves Syncfusion licensing intact.
Portable backup password protection stays optional under D-62. An unprotected file is readable, even inside a
connected cloud account; HTTPS and portable-file encryption are different boundaries.

## Current SDK inventory

Versions below are resolved .NET wrapper packages, not an assertion of the latest native Maven version.

| SDK / dependency | Version reviewed | Data / endpoint / safeguard |
|---|---|---|
| Microsoft.Identity.Client (MSAL) | 4.90.1 | Account identity, scopes and authentication protocol go to Microsoft's identity service when signing in/refreshing; system browser, no broker. Token cache remains platform-protected. No app PII-logging callback enabled; this is not a claim of no provider diagnostics |
| Xamarin.GooglePlayServices.Auth | 122.0.0 | Android Google authorization API; minimal Drive app-data scope plus account display, Play services owns tokens. Google revocation/user-info endpoints used by the existing adapter |
| Google Drive / OneDrive adapters | Repository code, HttpClient | Own app-folder file metadata and optional-password backup bytes to www.googleapis.com / graph.microsoft.com over HTTPS. Listing after connection is automatic; uploading requires the user's action |
| Xamarin.Google.MLKit.TextRecognition | 116.0.1.9 | Bundled Latin model; image/text processed locally. Android usage/diagnostic collection may run through native transport whenever network access is granted |
| Xamarin.Google.MLKit.Common | 118.11.0.9 | ML Kit initialization/diagnostics dependency; compiled even in the offline APK |
| Xamarin.Google.Android.DataTransport.TransportApi / TransportRuntime / TransportBackendCct | 4.1.1.1 / 4.1.1.1 / 4.1.1.2 | Transitive native metrics transport. Absence of INTERNET blocks this process's direct network use in Offline; a custom application HttpClient handler cannot constrain it in Cloud |
| Firebase Annotations / Components / Encoders | Transitive support libraries | Present in the dependency graph; do not equate these with a Firebase Analytics or Crashlytics feature. No such application feature is configured |
| Syncfusion controls/PDF/Licensing/Telemetry | 35.1.37 | License checked locally; shared registration calls Telemetry.Disable before controls. Explicitly disabled in both variants |
| Plugin.LocalNotification | 14.1.2 (Core 1.1.2) | Local reminder scheduling/text; no push registration/backend. Generic content unless the user allows details |
| MAUI / AndroidX Biometric | Existing platform support | Files, pickers, protected storage, device authentication; no app handling of biometric templates or the device password |

ML Kit's [official disclosure](https://developers.google.com/ml-kit/android-data-disclosure) describes device/app
information, per-installation identifiers, performance/API configuration, input/output sizes and SDK event/error
metrics. It covers the latest SDK, so exact pinned-version collection still requires native review and device traffic
qualification. A bundled OCR model is not a guarantee of no diagnostics. No image/recognized text is uploaded by the
application's OCR code. Do not claim that no SDK analytics can leave an online build.

MSAL's [PII logging default](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.client.baseapplicationoptions.enablepiilogging)
is false; the adapter adds no logging callback. Syncfusion documents
[Telemetry.Disable](https://help.syncfusion.com/maui/telemetry); the shared startup explicitly invokes it. None of
these controls establishes that every provider/platform process has zero traffic.

## Permission allowlist and repeatable checks

The complete APK guard accepts only POST_NOTIFICATIONS, RECEIVE_BOOT_COMPLETED, USE_BIOMETRIC, USE_FINGERPRINT,
the package-scoped DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION, and INTERNET in the explicitly selected Cloud variant.
The dynamic receiver permission is an AndroidX app-private receiver boundary, not access to user data. Both variants
reject ACCESS_NETWORK_STATE, location, camera, storage, advertising-id, exact alarms and every unknown permission.
Adding a future permission requires a deliberate policy and privacy review; never silently expand the allowlist.

```powershell
./eng/tests/AndroidPrivacy.Tests.ps1
./eng/scripts/Build-AndroidApk.ps1 -Offline -Output artifacts/android-offline
./eng/scripts/Test-AndroidPrivacy.ps1 -Path artifacts/android-offline/pro.vafadar.zanance-Signed.apk -Variant Offline
./eng/scripts/Build-AndroidApk.ps1
./eng/scripts/Test-AndroidPrivacy.ps1 -Path artifacts/android/pro.vafadar.zanance-Signed.apk -Variant Cloud
```

The default build uses existing provider configuration; without clients select Offline in the verifier. Only use
Cloud when the actual build is configured for cloud backup. The build script now always enforces the CI warning
policy and refuses warnings. It signs a complete installable APK; permission verification does not replace signature,
package/assembly checks or device tests. Tool output contains only permissions, package boundary and APK hash, never
configuration values, arbitrary manifest metadata or token caches.

Verified artifacts: Offline SHA-256 bb20b02a8410c10ba82063d277f1d7bc1c84019445f0c0fda3a4defd75225f39;
Cloud SHA-256 d42affda3de0159d781ad00fb2bc109d66492c26f662d23ca2328b0bdca20a03. Both package ids are
pro.vafadar.zanance, version 0.1.0/code 1, min SDK 24/target 36, signed with the local test certificate. Signature,
ZIP integrity and complete ARM64/x86_64 embedded assembly stores pass. Offline Release installed on the isolated
API 36 x86_64 AVD and reached the empty first-run Welcome UI, inspected with uiautomator without removing screen
protection. This is a startup check, not receipt/OAuth/native-traffic or physical-device acceptance.

AT-76 covers eleven policy cases: local/online success, wrong network variant, unexpected location/advertising-id/
camera/network-state permissions, fixture package, Debug and missing reminder capability. Actual Cloud APK is also
rejected when Offline is required; actual Offline/Cloud artifacts are checked separately. Main automated suite stays
separate from these PowerShell cases. No app UI, financial schema, production provider or backup policy changed.

## Store declarations and remaining gates

* Android Offline: verify the exact submitted Release artifact has no INTERNET. OS backups and user shares are
  separate flows; OD-10 is still open. Never reuse the Cloud declaration blindly.
* Android Cloud: declare user-initiated backup/account flows and applicable ML Kit SDK diagnostics under current Play
  definitions. Password is optional; transit encryption does not imply password-protected files. No developer server,
  application advertising, tracking or crash-reporting service is added.
* iOS: current source manifest declares no tracking and the framework required-reason APIs. It does not prove cloud
  backup or third-party SDK disclosure is complete. Review all merged SDK privacy manifests in the signed build;
  Apple console submissions and owner/account/signing actions remain release gates.
* Billing/sync/AI: no current runtime SDK evidence exists because these features are not implemented. Each section
  must repeat the dependency/permission/endpoint review and update this matrix before enabling its online build.
* Physical device/native traffic and real OAuth refresh/revoke/restore acceptance remain QA-02/QA-07. No new sign-in,
  purchase, provider account or production service was performed for this audit.
