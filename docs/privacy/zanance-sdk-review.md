# Zanance SDK and permission review (SEC-09, D-69)

## Current signed Android review artifacts

These observations bind to the exact already signed review APKs listed below.
Android SDK AAPT2 reported the manifest permissions and SDK dexdump reported
class **definitions** across all three DEX files per archive. The check reads
the binaries without rebuilding or running them.

| Signed review APK | INTERNET | Play Billing permission | Play Billing definitions | DataTransport definitions | ML Kit definitions | Google legacy sign-in definitions | Google location definitions |
|---|---|---|---:|---:|---:|---:|---:|
| Offline `2eede0f5` | No | No | 0 | 317 | 203 | 39 | 0 |
| Personal sync `216204c3` | Yes | No | 0 | 317 | 203 | 39 | 0 |
| Billing sandbox `27b5a595` | Yes | Yes | 273 | 317 | 203 | 39 | 99 |

No inspected archive declares Android location, advertising ID or network-state
permission. Each declares notifications, boot completion, biometric and
fingerprint permissions plus its own dynamic-receiver permission. No DEX class
definition in the inspected Microsoft Identity Client, Firebase Analytics or
Firebase Crashlytics namespaces was found. These are namespace checks rather
than a complete inventory of managed assemblies or renamed code.

All three archives carry the same application notices asset, which includes
the optional Google Play Billing notice. Its text is shared across build
editions. The DEX and permission evidence distinguishes the packaged billing
edition. DataTransport and legacy Google sign-in definitions are present even
in the offline archive; their presence establishes package composition only.
The offline APK's original verifier separately checks the absence of the
network permission and Google/Microsoft sign-in component declarations.

The SHA-256 identities inspected were

- Offline `1e38ab9fe7734fd4f8b428e19389e43955207bb9a79b57caa059ac9f45e9d24e`
- Personal sync `95933e405db963bbe3b9de91e7bdcd340818f067991fae3cf552d10ad37e1ed2`
- Billing sandbox `2c4ff4bac60ba1993f949f934c02ca77bf33b70a65b1cf289977d1ba646767d0`

### Billing sandbox first-launch observation

The exact `27b5a595` review APK ran once on a fresh Android 16 x86-64 guest.
Guest firewall rules rejected outbound IPv4 and IPv6 traffic for the installed
application UID before launch. A separate, blocked TCP control incremented the
IPv4 counter to verify the detector. No packets or payloads were captured.

First launch displayed a Syncfusion trial warning. Both application counters
were zero at the warning. After dismissing it with OK, the same process reached
Welcome, and both counters still read zero after 32.16 seconds. There was no
sign-in, setup, store, OCR or financial action. The fixture was retired.

This covers attempted packets attributed to the application UID during those
first-launch observations. DNS delegated to platform services, other UIDs and
foreground opt-in flows were outside the measurement. External processor
configuration, real store behavior and the final release declaration remain open.


## Current optional billing boundary (D-295,2026-10-16)

The2026-10-08 absence inventory below is historical. Local personal/household services and the selected Android
billing edition now have their own disclosed scopes; this update concerns billing only. The explicit Billing
permission policy requires INTERNET and com.android.vending.BILLING, rejects location/advertising-id/network-state
and does not widen Offline or Cloud. Nine new policy cases and the actual selected complete Release APK guard
pass; unchanged prior permission cases were not replayed.

Microsoft's Xamarin.Android.Google.BillingClient9.1.0.2 binds official Play Billing9.1.0. The official Maven artifact
itself requires DataTransport/Play services support; a direct binding would not remove those native dependencies.
The selected graph resolves TransportApi4.1.1.1, TransportRuntime4.1.1.1, TransportBackendCct4.1.1.2 and Play services
Location121.4.0.1. Location is not declared in the final APK. Presence/absence of a permission or diagnostic package
is not proof of native traffic behavior. Store connection/catalog/purchase effects are lazy and require explicit
page work; real SDK traffic/device/store privacy acceptance remains pending. No application analytics, ads or
crash reporting feature is introduced. The offline edition omits the billing SDK.

Purchase references/opaque account binding travel only to the selected actual store and fixed owned HTTPS issuer.
They are excluded from general logs, financial backups and portable preferences. Google.Apis.Auth/Core1.77.0 are
service-side only; explicit provider credentials remain operator-owned private mounts. Apple uses authenticated
signed data and fresh Server API verification, not a public numeric transaction id as possession. Native StoreKit2
is not implemented/qualified by this Windows delivery. Production endpoints, genuine store sandbox/provider
credentials, public callbacks and commercial activation stay closed/unaccepted.

The actual package MIT/Google SDK terms and service Apache2.0 notices are bundled. These third-party terms grant
no rights to Zanance source, which remains all rights reserved. See the exact current artifacts/evidence in
src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/12-billing-delivery.md.

Historical 2026-10-08 inventory: the package versions and original Offline/Cloud artifacts below describe that
reviewed baseline, not the complete current optional-service graph. D-292/D-294/D-295/D-298/D-389 add locally
implemented personal/household, billing, aggregate-analysis and ECB boundaries. The current app privacy matrix
and D-604 reconciliation distinguish their actual activation and encryption scopes. Refresh exact dependency,
permission, endpoint and runtime-traffic evidence for the selected submission artifact; do not infer absence
from an older inventory or qualification from source alone.

[D-604 current inventory reconciliation](../../src/Apps/Zanance/docs/quality/current-privacy-inventory-alignment.md)
records the inspected implementation boundaries without repeating native or financial cohorts.

## Reviewed baseline build boundaries (2026-10-08)

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

## Historical 2026-10-08 SDK inventory

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

### Offline sign-in component boundary

The signed offline APK `9cf6c14f` still registered two exported Microsoft sign-in activities
even though it had no OAuth client. The offline Release overlay now removes both declarations.
The APK handoff verifier rejects them on a Zanance offline build. The old signed APK fails this
check, and signed offline APK `3af08d29` passes. Comparing the two packaged manifests showed
the same permission declarations and the same names and export flags for the other 26
components. The offline APK has neither INTERNET nor ACCESS_NETWORK_STATE.

The configured-cloud build evaluation with a fictional client excluded the offline overlay.
No cloud sign-in was attempted. The corrected signed APK reached the original first-run setup
on a fresh Android 16 emulator before creating an account. The evidence transport briefly
disconnected after the XML pull. The same device and boot later confirmed the original XML
hash without relaunching the app. The guest settings were restored and the package and owned
emulator were retired. Local evidence is under `artifacts/offline-sign-in-component-review`.
This covers the APK declarations and startup only. Cloud sign-in, the final store bundle and
runtime traffic acceptance are separate.

The Play workflow now checks the decoded base manifest for the same offline sign-in boundary.
Both focused offline fixtures passed the former verifier and were refused after the change.
The cloud fixture continued to pass with a library sign-in activity. The previously signed
offline AAB `3d63d62a` was refused by the tightened check. A new locally signed Release AAB
`ab1ff5de` passed the decoded manifest, packaged backup exclusions, 16 KiB native load
alignment and qualified SQLCipher checks for ARM64 and x86-64. Its local signature verified
with a self-signed development certificate. No production upload certificate or Play
submission was used. Local receipts are in `artifacts/offline-sign-in-component-bundle`.


### Offline Google sign-in component boundary

The previous signed offline APK `ae6a1bc5` still contained the Google sign-in hub
activity and the revocation service protected by its declared Google permission.
The offline overlay now removes both. The APK and bundle manifest checks reject
these components in the offline edition. Both negative bundle fixtures passed
before the new check and were rejected afterward. All 11 bundle verifier tests
pass, including the cloud control with those declarations. Project evaluation
with a fictional Android Google client confirmed that the cloud edition does
not use the offline overlay.

The signed offline Release APK
`2d3853ef8c7b1ba87cd2fcc1e9787ddab01f203e917e0b69a3382a5b42c0ccf3`
passed the Android release checks. Comparing decoded manifests after ignoring
line numbers found exactly those two removals, with all other manifest fields
and 24 remaining component declarations equal. The file installed by hash
and reached first-run Welcome on an owned Android 15 x86-64 emulator with
16,384-byte userspace pages. No account, sign-in, OCR or financial submission
was performed. The disposable package and guest were retired.

The previously retained offline AAB `ab1ff5de` was also refused by the updated
offline bundle verifier for the Google declaration. A newly built local Release
AAB with SHA-256
`bc668220352ff8bc3e538aeb659e420a64f27ad9462e9a02b1b0f52e8404aa5d`
passed bundletool validation, the decoded manifest and packaged backup and
sharing rules, explicit 16 KiB ZIP configuration and ELF checks, and managed
payload and SQLCipher checks for ARM64 and x86-64. Its local signature verified
with a self-signed development certificate. The production upload certificate,
Play artifact and runtime traffic review remain open. Evidence is in
`artifacts/offline-google-sign-in-boundary`.

The local Cloud Release AAB built with a fictional Android Google client has
SHA-256 `6c58d2d2d1021e0621f2d2ce15ee0541f93af84af18de8911fbafedb6fc631a4`.
The decoded manifest retains one non-exported Google sign-in hub activity and
one revocation service protected by the Google revocation permission. The
Cloud manifest policy, bundle structure, storage and native alignment checks
passed. A local development certificate was used. No account was used and
no live cloud request was sent.

Bundletool also generated a local universal APK from the identified offline
AAB. Its SHA-256 is
`6944efc816e639acec28593e1d2c288c42d533fb077f088566dfa1fd037b2961`.
The decoded binary manifest passed the offline permission policy and contained
neither the Microsoft nor Google sign-in components. The first conversion
stopped because the bundled tool could not locate AAPT2. Conversion succeeded
when given the project's resolved SDK AAPT2 path. The derived APK is unsigned
and was inspected without installing it. This does not inspect APK splits
generated or signed by Play.

### External component inventory for the signed offline APK

The signed APK used for the Android 16 goal opt-out check, SHA-256
`c6c1235324cb33626393c8231b981bbce05aca80895fe4e6cd2aac457020e4c4`,
declares 24 manifest components. Three are explicitly exported, 20 explicitly
set `exported=false`, and the remaining service, `KeepAliveService`,
omits the attribute and has no intent filter. No provider is exported.

| Explicitly exported component | Manifest boundary |
|---|---|
| Zanance `MainActivity` | One launcher filter. The widget route in source accepts only Expense, Income or Transfer and opens an editor. |
| Zanance `QuickAddWidget` | One widget-update filter. The source renders localized labels without amounts, uses immutable pending intents and leaves entry actions to the app. |
| AndroidX `ProfileInstallReceiver` | The manifest requires `android.permission.DUMP` and lists four profile-maintenance filters. |

The decoded manifest and all 24 declarations are retained in
`artifacts/goal-ai-toggle-native-bd0938b3`. The inventory records packaged
declarations and the two Zanance source boundaries. Runtime sender admission,
online-edition traffic and the final Play artifact remain separate checks.

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

The APK permission parser also inspects SDK-qualified permission declarations. An AAPT2-compiled
fixture with `uses-permission-sdk-23` for `READ_CONTACTS` was previously accepted because its line was
omitted by the parser. The same fixture now fails on the unexpected permission. The complete dual-ABI
Offline Release APK with SHA-256
`9f1c9df40e85763054eb3568be2fa2821c55e719cdcd28e744d0a3a7c7ed8bde` passed. Its permissions are
notifications, boot completion, the two Android biometric declarations and the private AndroidX
dynamic receiver permission. The fixture was compiled for static inspection only and was never
signed or installed. The original and corrected results are in `artifacts/zpv`.

The signed AAB base-manifest check and the APK audit now read the same Zanance permission policy.
The AAB check requires unrestricted declarations for the required permissions of the selected
edition. A decoded synthetic manifest with a conditional contacts permission passed the previous
AAB check and now fails. The focused fixtures also cover internet permission in an offline bundle,
restricted network permission for cloud backup, a valid synthetic cloud permission set and a missing
notification permission. All 20 existing APK policy cases passed against the shared policy. The already
signed ARM64 Offline AAB (SHA-256
`3d63d62a911e1311dc00fb8f43e73a1035dc739ee2db6c6b0ffb72cb0b6a916c`)
and complete dual-ABI Offline APK passed their respective new checks. Local receipts are in
`artifacts/zba`. The Libraries and tests CI job runs the bundle verifier against synthetic manifests for offline acceptance, SDK-qualified unexpected permissions, cloud internet restrictions and missing notifications. These checks cover manifest declarations. Runtime behavior is reviewed separately.

## Store declarations and remaining gates

* Android Offline: verify the exact submitted Release artifact has no INTERNET. OS backups and user shares are
  separate flows; OD-10 is settled and D-263 implements Android exclusions. OEM/device-transfer and iOS
  acceptance remain open; D-603 current framework ineligibility is not payload evidence. Never reuse another
  edition's declaration blindly.
* Android Cloud: declare user-initiated backup/account flows and applicable ML Kit SDK diagnostics under current Play
  definitions. Password is optional; transit encryption does not imply password-protected files. No developer server,
  application advertising, tracking or crash-reporting service is added.
* iOS: current source manifest declares no tracking and the framework required-reason APIs. It does not prove cloud
  backup or third-party SDK disclosure is complete. Review all merged SDK privacy manifests in the signed build;
  Apple console submissions and owner/account/signing actions remain release gates.
* Optional services: local implementations exist. Android billing has the separate D-295 SDK graph; personal
  sync is service-readable, household finance uses E2EE with visible membership metadata, and aggregate analysis
  has no configured production provider. ECB daily references are explicitly fetched through the service. Exact
  selected-artifact traffic, real store/provider acceptance and final processor/retention declarations remain
  gates. These are not absent features merely because their public/commercial activation is unfinished.
* Physical device/native traffic and real OAuth refresh/revoke/restore acceptance remain QA-02/QA-07. No new sign-in,
  purchase, provider account or production service was performed for this audit.


## D-313 optional hosted edge processor (not active)

The approved Cloudflare Tunnel path terminates public TLS at Cloudflare; visitor IP/routing metadata and identity request plaintext can be processed there. Existing application-layer sync/share ciphertext is separate from TLS; disclosed analysis has its own consent boundary. No mobile SDK, permission, local schema or automatic upload is added. The connector mounts only the owner credential file and original public CA; bounded error logs never enable request/header debugging. No live tunnel, account, DNS or real mail is configured. Draft processor/retention and exact release-build declarations need review before publication; historical offline/cloud-backup-only answers do not apply to optional identity/sync builds.
