# 06 – Finance privacy matrix

App `pro.vafadar.finance`, version 0.1.0 (development). Reviewed against the repository on 2026-09-26.
Statuses: Implemented – verified / Implemented – unverified / Planned / Not included / Unknown – needs verification.
"Verified" means covered by automated tests or a reviewed build artifact; **device checks** (release APK on a real
phone) are still pending for every row and are a release gate (08).

This is the app's privacy profile required by MAT-01..04. The owner's starter template is
`spec/Finance-Privacy-Matrix-Starter.md`; its flow ids DF-01..DF-15 are kept, DF-16..DF-20 were added here.

## Portfolio summary

| Online account | Local ledger | Ads / Analytics / AI | Pro | Privacy policy | Data safety |
|---|---|---|---|---|---|
| None in phase 1 | SQLite in app-private storage | Not included (no such SDK in `Directory.Packages.props`) | Planned, no billing in phase 1 | Draft (`docs/privacy/privacy-policy.md`) – must be published before release | To be completed from the release build |

## Data flows

| ID | Flow | Data | Destination | Status | Evidence / open verification |
|---|---|---|---|---|---|
| DF-01 | Accounts and entries | Amounts, currencies, dates, account names, categories, titles, payees, notes; quick templates | Local database (`FileSystem.AppDataDirectory`) | Implemented – verified | Store and ledger tests; no network code in Finance projects. "Delete all data on this device" (Settings, two confirmations) empties the database and restarts onboarding; backup files already shared, exports and OS backups stay (BAK-15) |
| DF-02 | Plans, occurrence states, budgets, exchange rates | Due dates, amounts, limits, rates | Local database | Implemented – verified | Plan, budget and rate tests |
| DF-03 | Local backup file | Full database (all of DF-01/02 and finance settings) in an AES-256-GCM encrypted package with manifest and content counts | Kept on the device (last 10) and shared by the user through the system share sheet to any destination they choose | Implemented – verified (library + fresh-install integration test) | Password warning (BAK-06); safety copy before restore; neutral file name (BAK-08); device check pending |
| DF-04 | Google Drive backup | Encrypted package, account identity | User's Drive app data folder | Not included – hidden until sign-in exists (BAK-14) | Library tested against fake HTTP only |
| DF-05 | OneDrive backup | Encrypted package, account identity | User's OneDrive app folder | Not included – hidden until sign-in exists (BAK-14) | Same |
| DF-06 | CSV export / share | Entries of the chosen period; notes and payees optional | App chosen by the user via the share sheet | Implemented – verified | Unencrypted-file warning (IO-05); formula neutralisation (AT-55); file in the app cache |
| DF-07 | CSV import | File chosen by the user | Local processing only | Implemented – verified | No upload; preview; atomic batch with undo (AT-54) |
| DF-08 | Reminders and budget alerts | Occurrence id; generic text by default; plan name, amount and due date only with "Show names and amounts" | Device notification service (Android/iOS, local, no push) | Implemented – verified (unit) | Several reminders at the same minute become one summary, names only with details allowed (REM-10); snoozes keep the same text and link in local preferences until they fire or the occurrence closes (REM-04); permission asked only when a reminder is turned on (REM-03); tap opens the app only after unlock; Windows: no notifications |
| DF-09 | App lock | Lock-enabled flag in the database; the credential stays with the OS | Device | Implemented – verified (build); device check pending | No PIN or biometric data stored (SEC-01); screenshots blocked on Android while on; hides the UI only – the database is not encrypted (SEC-03). Same as DF-20 in earlier revisions |
| DF-10 | Diagnostics / logs | Exception text to the debug output in Debug builds only; no crash-reporting SDK | Local debug output | Implemented – unverified | Verify the release build has no logging provider sending data (SEC-04) |
| DF-11 | Ads / tracking | – | – | Not included | No SDK referenced; no advertising id |
| DF-12 | Billing | – | – | Not included in phase 1 | – |
| DF-13 | AI | – | – | Not included | – |
| DF-14 | Sync / household | – | – | Not included | – |
| DF-15 | Online rates / bank sync / any network | – | – | Not included | Exchange rates are entered manually; the Android manifest declares no INTERNET permission |
| DF-16 | **Android Auto Backup** | App database and preferences | User's Google account backup (Google) | Implemented – unverified (`allowBackup=true`, owner decision D-16) | Disclose in policy and Data safety; verify restore on a new device |
| DF-17 | **iOS device / iCloud backup** | App data | User's iCloud or computer backup (Apple) | Implemented – unverified (OS default) | Disclose; iOS release later |
| DF-18 | Preferences | Language, calendar, optional region (never from location), first day of the week, budget alert levels, dismissed Home guidance | Local preferences | Implemented – verified | Included in OS backups (DF-16/17), not in the app's backup package |
| DF-19 | Syncfusion license validation | License key compiled into the app (build secret via `eng/AppSecrets.targets`, never tracked) | Local check | Implemented – verified (offline validation test `Vafadar.SyncfusionLicense.Tests`) | Confirm no network call in the release build (none possible without INTERNET on Android) |

## Permissions (Android manifest)

| Permission | Status | Reason |
|---|---|---|
| POST_NOTIFICATIONS | Present | Reminders; requested only when the user turns a reminder on |
| RECEIVE_BOOT_COMPLETED | Present | Scheduled reminders are restored after a restart (REM-07) |
| INTERNET, ACCESS_NETWORK_STATE | Removed (D-20) | No online feature in phase 1; Debug builds add INTERNET for the debugger |
| Exact alarms | Not requested | Reminders are inexact (REM-09) |
| Biometric | Not declared by the app | BiometricPrompt uses the system dialog; verify the merged release manifest |

Any change to SDKs, permissions, backup destinations, sign-in, billing or AI must update this file (MAT-05).
