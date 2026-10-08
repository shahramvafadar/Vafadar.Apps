# 08 – Release checklist (first public release)

## Product and data

- [ ] Every Android phone-test handoff includes a complete signed Release APK with package/signature verified
  and the actual file path supplied (D-66); installation and physical-device acceptance are checked separately.
- [ ] All phase-1 acceptance scenarios pass on the **release** build (07), except explicitly unshipped cloud destinations
- [x] Golden data AT-62 exact in en/fa/de (calculation and display tests: separators per language, Persian digits)
- [ ] Upgrade from every earlier test build keeps data (AT-60) – integration test for the first schema passes; re-check with real test builds
- [ ] Encrypted backup → uninstall → reinstall → restore gives identical balances (AT-57) – fresh-install integration test passes; device check pending
- [ ] No known critical bug in balances, conversion, double counting, restore or data exposure (Q-01)
- [ ] Performance measured on the reference device with 10,000 entries (Q-02) and recorded – calculation budget test with 10,000 entries, 20 accounts and 100 plans passes
- [ ] Deviations in spec Section 31.3 accepted by the owner or resolved

- [ ] D-62 device acceptance: fresh install → choose theme/experience or restore an existing backup; cancel and return
  without losing the draft; restore protected and unprotected files without an extra account. For each released cloud
  provider, upload/restore with protection on and off, with a real OAuth client. Windows checks do not close this gate.

- [ ] D-64 receipt acceptance (AT-71, ZCR-QA-05): physical Android/iOS camera and file selection, EXIF, skew,
  missing/conflicting totals, currency/unit review, stored-image reread, cancel/error and explicit-save checks on
  representative receipts. Windows synthetic-image/UI checks and C# regressions pass; they do not close this gate.

- [ ] D-65 device acceptance (AT-72): new/existing plans, monthly anchors in Gregorian/Persian/Hijri, short months,
  count/end date, custom rules, debt/receivable creation, actual repayment versus estimate and reminder permission;
  draft cancellation must not post or alter balances. Windows fixture checks do not close this gate.

## Privacy and security

- [ ] Privacy matrix (06) reviewed against the release APK/AAB: package list, merged manifest permissions, network traffic
- [ ] Decide the release variant (D-35, D-50): **offline** (no OAuth client ids) or **with cloud backup** (`MICROSOFT_ENTRA_CLIENT_ID` and the Google client of the platform – `GOOGLE_OAUTH_CLIENT_ID_ANDROID`, `…_IOS`, `…_WINDOWS` – set). Client ids only: no client secret exists for any of them
- [ ] Offline variant: release manifest still without INTERNET and ACCESS_NETWORK_STATE after package updates (ML Kit asks for both; D-31)
- [ ] Cloud variant: merged manifest has INTERNET and the MSAL redirect activity with `msal{client-id}`, no ACCESS_NETWORK_STATE; connect, back up, list, restore and disconnect verified on a device for each provider; privacy policy, Data safety and store texts switched to the cloud variant
- [ ] Cloud variant, iOS: the built Info.plist lists the URL schemes `msauth.pro.vafadar.zanance` and the reversed Google iOS client id (and none in an offline build); the signed app has `keychain-access-groups` = `<Team ID>.pro.vafadar.zanance`; Microsoft and Google connect, back up, restore, disconnect (and reconnect after an app restart) verified on an iPhone; App Privacy answers re-checked
- [ ] Cloud variant, Windows: Google opens the default browser, the page "Sign-in complete" appears after consent, and the app shows the account; the files `%LOCALAPPDATA%\…\google\google.token` and `msal\msal.cache` are DPAPI-protected and removed or emptied on disconnect; a firewall prompt does not appear for the `127.0.0.1` listener
- [x] `INTERNET` permission removed if no online feature ships (D-20) – the offline build's merged manifest declares only notifications, boot and biometric (USE_BIOMETRIC / USE_FINGERPRINT from AndroidX Biometric); re-checked 2026-09-29 with the cloud libraries referenced
- [ ] Privacy policy published at a stable URL on vafadar.pro, reachable in the app and in Play Console (PRI-03)
- [ ] Android Auto Backup disclosed (D-16); decision re-checked
- [x] No financial data, notes, tokens or passwords in logs (SEC-04) – code review 2026-09-29: only `Debug.WriteLine` (removed from release builds) and the debug logger in Debug builds
- [x] Recent-apps preview and notifications hide financial data by default (SEC-02, REM-05) – notifications generic by default; preview always protected (Android API 33+ recents exclusion and secure background flags, iOS cover; D-63). Foreground screenshots may be allowed on Android. Device check pending

- [ ] D-63 device acceptance: set/change/remove PIN, failed-attempt delay across restart, notification/backup/export gates, recovery success/cancel/unavailable, secure-storage failures; Android screenshot toggle and recents on supported Android versions; iOS keychain/reinstall and privacy cover

## Build secrets

- [ ] GitHub repository secret `SYNCFUSION_LICENSE_KEY` set; `Vafadar.SyncfusionLicense.Tests` passes in CI (the release workflow requires it)
- [ ] Android signing secrets in the `production` environment
- [ ] Cloud variant only: OAuth clients registered (Entra redirect with the signature hashes of upload and Play app signing key, iOS/macOS platform with bundle ID `pro.vafadar.zanance` → `msauth.pro.vafadar.zanance://auth`, `http://localhost`; Google Android clients for both SHA-1, a Google iOS client for bundle ID `pro.vafadar.zanance`, a Google Desktop app client whose secret is not used), Google consent screen verified
- [ ] iOS: App ID `pro.vafadar.zanance` registered in the Apple Developer account, signing certificate and provisioning profiles (development, App Store) for it; built and signed on a Mac with Xcode

## Store

- [ ] Data safety form completed from the matrix (PRI-04)
- [ ] Financial features declaration completed per current Play guidance (REL-01)
- [ ] Target API level, signing, content rating, target audience per current Play requirements (REL-02)
- [x] Final app name: **Zanance** (owner decision 2026-09-26)
- [x] Icon, splash and notification icon from the approved brand master (D-26)
- [x] Store texts and screenshots in en/fa/de with fictitious data (REL-03/04); Spanish, French and Italian store texts are in the listing, their store screenshots are still to be taken – `docs/store/listing.md`, `docs/store/screenshots/<language>/` (from the Windows development build at phone size; replace with device screenshots if Play asks for a higher resolution)
- [x] Third-party licences listed in the app (Settings → About)
- [ ] Internal → closed testing → production track

### D-67 device gate

- [ ] On the signed APK, connect each configured provider, see automatic/empty/error feedback, create a protected
  and unprotected backup, refresh, preview, restore on a fresh installation and delete only the chosen file.
- [ ] Verify Google package/certificate registration matches the installed APK; a changed debug certificate can
  require owner console configuration, independently of the phone model.
- [ ] English UI + German formats + Gregorian + German holidays + Latin digits; retain choices after restart and
  restore, with state-holiday coverage explicitly excluded. Repeat Persian RTL and native pickers.

Local automated/rendered checks and CI do not close these physical-device or provider gates.
