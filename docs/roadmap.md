# Roadmap

## Phase 0 – Foundation ✅ (September 2026)

- [x] Repository structure, central build configuration, `.slnx` solution and solution filters
- [x] Shared libraries: Core, Localization, Data, Backup (+ Google Drive, OneDrive storage), Authentication abstractions, Maui
- [x] Zanance app skeleton (Android / iOS / Windows) with runtime language + calendar switching and RTL
- [x] Tests (xUnit v3 / MTP), CI (Linux, Windows, macOS on demand), CodeQL, Dependabot, release workflow for Android
- [x] Architecture documentation, ADRs, guides, privacy policy draft and privacy matrix

## Phase 1 – Zanance MVP (Android) ✅ (September 2026, device checks pending)

Details and status: [Zanance specification §31](../src/Apps/Zanance/docs/spec/Zanance-Product-Specification.md) and
[phase 1 plan](../src/Apps/Zanance/docs/04-phase-1-plan.md).

- [x] Requirements and product specification (`src/Apps/Zanance/docs/`)
- [x] Domain model: accounts, categories, entries (income / expense / transfer / refund / adjustment), currencies
- [x] Money type (`long` minor units + currency) and amount input that accepts Persian and Latin digits
- [x] Screens: Home, transactions, plans, budgets, accounts, categories, settings
- [x] Reports with Syncfusion charts and PDF export
- [x] Date input with Persian calendar support
- [x] Persian text in Vazirmatn in the app and in PDF reports
- [x] Persian digits in the Persian interface (setting)
- [x] App name (Zanance), icon, splash and colors (D-21, D-22, D-26)
- [x] App design: meaning colours, Insights tab, calm Home, Persian digits (D-27)
- [x] English and German fonts of the design (Figtree, Urbanist)
- [x] Local backup: encrypted backup file, restore with safety copy, CSV import / export
- [x] Cloud backup (OneDrive and Google Drive on Android, iOS and Windows) with sign-in (`Vafadar.Authentication.Maui`,
      D-35, D-50) – offered only in builds with OAuth clients; device verification with real clients pending
- [ ] Device checks of the release build (08 release checklist)
## Phase 2 – First store release

- [ ] Website `vafadar.pro` with home page and privacy policy
- [ ] Google Play developer account, app listing (en / fa / de), Data safety form
- [ ] Google OAuth consent screen verification; Microsoft Entra app registration
- [ ] Upload keystore, `production` environment secrets, internal → closed → production track
- [x] Android Auto Backup stays enabled for Zanance (D-16)

## Phase 3 – iOS

- [ ] Apple Developer Program, Mac build environment
- [ ] iOS-specific testing (Persian RTL, sign-in flows, backup)
- [x] Cloud backup sign-in on iOS (D-50): MSAL with keychain group entitlement and `msauth` URL scheme, Google with the
      iOS OAuth client (PKCE, reversed client id scheme) – built for iOS from Windows only as a library; app build and
      device test need a Mac
- [ ] App Store listing and privacy details, TestFlight, release

## Phase 4 – Monetization

- [ ] `Vafadar.Monetization`: Google Play Billing / StoreKit, Pro unlock, tip jar
- [ ] Verify current store policies for tips and purchases

## Later

- [ ] More apps (see [adding a new app](guides/adding-a-new-app.md))
- [ ] Web version / multi-device sync where an app needs it ([web-and-shared-data.md](architecture/web-and-shared-data.md))
- [x] Android 13+ per-app language integration (D-32)
- [ ] Direct upload to Google Play from the release workflow
- [x] Google Drive backup on Windows (Desktop app OAuth client, loopback + PKCE, no client secret; D-50)
- [ ] Automatic cloud backups (`Vafadar.Maui.Backup`)

## Open questions

| Question | Needed for |
|---|---|
| Contact e-mail for privacy / support (e.g. `privacy@vafadar.pro`) | Phase 2 |
