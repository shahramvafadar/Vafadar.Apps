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
- [x] Persian text with the system fonts in the app and embedded Vazirmatn in PDF reports
- [ ] Optional Persian digits in the UI
- [x] App name (Zanance), icon, splash and colors (D-21, D-22, D-26)
- [x] Local backup: encrypted backup file, restore with safety copy, CSV import / export
- [ ] Cloud backup (Google Drive / OneDrive) with sign-in – hidden until verified (D-17), moved to a later phase
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
- [ ] App Store listing and privacy details, TestFlight, release

## Phase 4 – Monetization

- [ ] `Vafadar.Monetization`: Google Play Billing / StoreKit, Pro unlock, tip jar
- [ ] Verify current store policies for tips and purchases

## Later

- [ ] More apps (see [adding a new app](guides/adding-a-new-app.md))
- [ ] Web version / multi-device sync where an app needs it ([web-and-shared-data.md](architecture/web-and-shared-data.md))
- [ ] Android 13+ per-app language integration
- [ ] Direct upload to Google Play from the release workflow

## Open questions

| Question | Needed for |
|---|---|
| Recommended Google sign-in approach on Android at implementation time | Phase 1 |
| Contact e-mail for privacy / support (e.g. `privacy@vafadar.pro`) | Phase 2 |
