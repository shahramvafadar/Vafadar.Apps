# Roadmap

## Phase 0 – Foundation ✅ (September 2026)

- [x] Repository structure, central build configuration, `.slnx` solution and solution filters
- [x] Shared libraries: Core, Localization, Data, Backup (+ Google Drive, OneDrive storage), Authentication abstractions, Maui
- [x] Finance app skeleton (Android / iOS / Windows) with runtime language + calendar switching and RTL
- [x] Tests (xUnit v3 / MTP), CI (Linux, Windows, macOS on demand), CodeQL, Dependabot, release workflow for Android
- [x] Architecture documentation, ADRs, guides, privacy policy draft and privacy matrix

## Phase 1 – Finance MVP (Android)

- [ ] Requirements: collect in [`src/Apps/Finance/docs/requirements.md`](../src/Apps/Finance/docs/requirements.md)
- [ ] Domain model: accounts, categories, transactions (income / expense / transfer), currencies
- [ ] Money type (`long` minor units + currency) and amount input that accepts Persian and Latin digits
- [ ] Screens: dashboard, transaction list and editor, categories, accounts
- [ ] Reports with Syncfusion charts (by month, by category, balance over time)
- [ ] Date input with Persian calendar support (verify Syncfusion picker support)
- [ ] Persian font (e.g. Vazirmatn) and optional Persian digits
- [ ] App name, icon and colors
- [ ] `Vafadar.Authentication.Maui`: Microsoft (MSAL) and Google sign-in
- [ ] `Vafadar.Maui.Backup`: backup settings page, manual backup/restore, automatic daily backup, export/import file

## Phase 2 – First store release

- [ ] Website `vafadar.pro` with home page and privacy policy
- [ ] Google Play developer account, app listing (en / fa / de), Data safety form
- [ ] Google OAuth consent screen verification; Microsoft Entra app registration
- [ ] Upload keystore, `production` environment secrets, internal → closed → production track
- [ ] Decide whether Android Auto Backup should exclude the Finance database

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
| Finance feature details (accounts, currencies such as IRR / Toman / EUR, budgets, recurring entries, reports) | Phase 1 |
| Final app names and branding per language | Phase 1 |
| Recommended Google sign-in approach on Android at implementation time | Phase 1 |
| Contact e-mail for privacy / support (e.g. `privacy@vafadar.pro`) | Phase 2 |
