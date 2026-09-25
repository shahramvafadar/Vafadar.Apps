# 06 – Finance privacy matrix

App `pro.vafadar.finance`, version 0.1.0 (development). Reviewed against the repository on 2026-09-26.
Statuses: Implemented – verified / Implemented – unverified / Planned / Not included / Unknown – needs verification.

## Portfolio summary

| Online account | Local ledger | Ads / Analytics / AI | Pro | Privacy policy | Data safety |
|---|---|---|---|---|---|
| None in phase 1 | SQLite in app-private storage | Not included (no such SDK referenced – verified by package list) | Planned, no billing in phase 1 | Draft (`docs/privacy/privacy-policy.md`) | To be completed from the release build |

## Data flows

| ID | Flow | Data | Destination | Status | Evidence / open verification |
|---|---|---|---|---|---|
| DF-01 | Accounts and entries | Amounts, currency, dates, account names, categories, notes | Local database | Planned (schema in S1) | App-private storage path `FileSystem.AppDataDirectory` |
| DF-02 | Plans and budgets | Due dates, amounts, limits | Local database | Planned | – |
| DF-03 | Local backup file | Full ledger + finance settings, AES-256-GCM encrypted | File chosen/shared by the user | Library: Implemented – verified (unit tests); UI: Planned | Fresh-install restore test (AT-57) |
| DF-04 | Google Drive backup | Encrypted package, account identity | User's Drive app data folder | Not included in phase-1 release until sign-in exists | BAK-14 |
| DF-05 | OneDrive backup | Encrypted package, account identity | User's OneDrive app folder | Not included in phase-1 release until sign-in exists | BAK-14 |
| DF-06 | CSV export / share | Selected columns, notes optional | App chosen by the user | Planned | Warning dialog, temp file cleanup |
| DF-07 | CSV import | Selected file | Local processing only | Planned | No upload |
| DF-08 | Reminders | Occurrence id, generic text by default | Device notification service | Planned | Lock-screen text (REM-05) |
| DF-09 | App lock | Lock enabled flag; device credential handled by OS | Device | Planned | Not part of backups |
| DF-10 | Diagnostics / logs | Debug logger only in Debug builds; no crash reporting SDK | Local debug output | Implemented – unverified | Verify release build has no logging provider sending data |
| DF-11 | Ads / tracking | – | – | Not included | No SDK in `Directory.Packages.props` |
| DF-12 | Billing | – | – | Not included in phase 1 | – |
| DF-13 | AI | – | – | Not included | – |
| DF-14 | Sync / household | – | – | Not included | – |
| DF-15 | Online rates / bank sync | – | – | Not included | – |
| DF-16 | **Android Auto Backup** | App database and preferences | User's Google account backup (Google) | Implemented – unverified (`allowBackup=true`, owner decision D-16) | Disclose in policy and Data safety; verify restore on new device |
| DF-17 | **iOS device / iCloud backup** | App data | User's iCloud / computer backup (Apple) | Implemented – unverified (OS default) | Disclose; iOS release later |
| DF-18 | Preferences (language, calendar) | UI settings | Local preferences | Implemented – verified | – |
| DF-19 | Syncfusion license validation | License key compiled into the app | Local check | Unknown – needs verification | Confirm no network call in the release build |

## Permissions (Android manifest)

| Permission | Status | Reason |
|---|---|---|
| INTERNET, ACCESS_NETWORK_STATE | Present (template) | Needed only for cloud backup; remove from a release without online features (D-20) |
| POST_NOTIFICATIONS | Planned (S10) | Requested when the user enables a reminder |

Any change to SDKs, permissions, backup destinations, sign-in, billing or AI must update this file (MAT-05).
