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


SEC-01 / D-68 gate: proposed ADR 0010 and independent Windows/Android emulator proof do not clear the encryption
release gate. Android API 36 x86_64 runtime and Keystore process-restart proof pass for fictitious data only. ARM64
phone, iOS, exact maintained native binaries, recovery, key loss, migration fault injection, OS backup and
no-plaintext-safety-copy checks remain. Never ship the deprecated feasibility provider through app dependencies.


D-69 / SEC-09: run the complete-APK permission guard for the selected Offline/Cloud Release variant; keep the SDK
review and store declarations matched to that binary. Native traffic, real OAuth and signed iOS SDK privacy-manifest
review remain gates; repeat before billing/sync SDKs. Never claim no SDK diagnostics in a cloud-enabled Android build.

D-70 / LOC-02 device gate: verify contribution reminder opt-in/refusal, local 09:00 scheduling with the saved calendar,
generic/details text, pause/reach/resume/disable cancellation and notification taps through the app lock on a signed
APK. Android emulator fixture delivery does not close ARM64 phone, Doze/timezone/reboot or iOS device acceptance.

D-71 / LOC-03 device gate: verify review-reminder opt-in/refusal, selected-calendar/pay-cycle boundaries and local
09:00 delivery, generic/details text, tap through app lock, finishing/opt-out cancellation, profile switch/restore,
Doze and reboot. Local tests and isolated API 36 x86_64 native delivery/tap do not close ARM64 phone or iOS acceptance.

D-72 / LOC-04 device gate: use the complete signed APK to preview/link/keep overlapping aggregates, confirm totals,
restart and Undo; exercise partial files, stale previews and dependent edits/imports with disposable data. Native
API 36 x86_64 import and Release Undo are verified; ARM64 phone and iOS runtime remain open. Historical metadata in
ImportLinks travels with database backups and is retained until its import is undone or profile/data deletion.

D-73 / QA-03: AT-80 application tests cover actual non-UI flows, not native platform acceptance. On the owner's phone
verify secure startup, widget drafts after unlocking, PIN/device confirmations, short/long background returns,
profiles, bulk validation/delete/Undo and theme changes with unsaved input using the complete signed APK. No result
from isolated SQLite, a native port double, the emulator or CI substitutes for those physical-device release checks.

D-74 / QA-04: repeat manual Income/Expense on a valued asset on the physical phone. Cancel must keep the draft and
asset value; Record anyway must save only once after validation. Transfers/adjustments must keep their meanings.
AT-81 local/emulator results and the complete signed APK do not close physical-device, iOS or product acceptance.

D-75 / Q-02: select an actual physical reference device and record process-cold Home, search and save with the
10,000/20/100 workload and explicit tenfold stress shape. Include native rendering and input response, not only
calculation/store timing or system first frame. Verify there is no ANR and that every matched result row is visible
and accessible after empty/nonempty filtering. Desktop/emulator timing and AT-82 do not close these device gates.

D-76 / AT-83: verify loading feedback, disabled search/filter/Add/bulk actions until publication, no false empty
state, failure/retry, retained filters on return and actual matched result buttons on the physical phone. The
publication helper tests, Windows captures and emulator observations do not close native rendering, performance,
ANR, iOS or device acceptance gates.

D-77 / A11Y-03: on the physical phone exercise 200% system text, complete quick-action names/targets, plan identity,
amount/date/status, both themes and RTL/language changes. Review remaining financial rows, currency tokens, fixed
actions, tabs, calendars, pickers, charts and dialogs. Process-local Windows stress and an emulator activity-only
configuration do not close real OS scaling, screen readers, ARM64 phone or iOS acceptance. Use the complete signed
Release APK; it has no font-scale review override or native text collector.
