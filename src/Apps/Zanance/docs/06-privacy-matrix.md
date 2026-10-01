# 06 – Zanance privacy matrix

App `pro.vafadar.zanance`, version 0.1.0 (development). Reviewed against the repository on 2026-09-28.
Statuses: Implemented – verified / Implemented – unverified / Planned / Not included / Unknown – needs verification.
"Verified" means covered by automated tests or a reviewed build artifact; **device checks** (release APK on a real
phone) are still pending for every row and are a release gate (08).

This is the app's privacy profile required by MAT-01..04. The owner's starter template is
`spec/Zanance-Privacy-Matrix-Starter.md`; its flow ids DF-01..DF-15 are kept, DF-16..DF-20 were added here and are mirrored in the starter.

## Portfolio summary

| Online account | Local ledger | Ads / Analytics / AI | Pro | Privacy policy | Data safety |
|---|---|---|---|---|---|
| None in phase 1 | SQLite in app-private storage | Not included (no such SDK in `Directory.Packages.props`) | Planned, no billing in phase 1 | Draft (`docs/privacy/privacy-policy.md`) – must be published before release | To be completed from the release build |

## Data flows

| ID | Flow | Data | Destination | Status | Evidence / open verification |
|---|---|---|---|---|---|
| DF-01 | Accounts and entries | Amounts, currencies, dates, account names, categories, titles, payees, notes; quick templates; saved list filters; who pays back a reimbursable expense (exported with payees and notes only when chosen); tags; categorization rules; interest rate and monthly installment of loans; lender or borrower of a loan | Local database (`FileSystem.AppDataDirectory`) | Implemented – verified | Store and ledger tests; no network code in Zanance projects. "Delete all data on this device" (Settings, two confirmations) empties the database and restarts onboarding; backup files already shared, exports and OS backups stay (BAK-15) |
| DF-02 | Plans, occurrence states, budgets, exchange rates, savings goals | Due dates, amounts, limits, rates, goal targets and earmarks | Local database | Implemented – verified | Plan, budget and rate tests |
| DF-03 | Local backup file | Full database (all of DF-01/02 and finance settings) in an AES-256-GCM encrypted package with manifest and content counts | Kept on the device (last 10) and shared by the user through the system share sheet to any destination they choose | Implemented – verified (library + fresh-install integration test) | Password warning (BAK-06); safety copy before restore; neutral file name (BAK-08); device check pending |
| DF-04 | Google Drive backup | Encrypted package (password always required), account e-mail | User's Drive app data folder (`drive.appdata`) over HTTPS | Implemented – unverified (Android; only in builds with `GoogleOAuthClientIdAndroid`, D-35) | Storage tested against fake HTTP; sign-in through the Play services authorization API, grant revoked on disconnect; device check with a real OAuth client pending |
| DF-05 | OneDrive backup | Encrypted package (password always required), account name | User's OneDrive app folder (`Files.ReadWrite.AppFolder`) over HTTPS | Implemented – unverified (Android, Windows; only in builds with `MicrosoftEntraClientId`, D-35) | Storage tested against fake HTTP; MSAL through the system browser, token cache on the device (DPAPI on Windows), removed on disconnect; device check pending |
| DF-06 | CSV and PDF export / share | CSV: entries of the chosen period, notes and payees optional. PDF: the report of a period (totals, categories, accounts, plans, tags) | App chosen by the user via the share sheet | Implemented – verified | Unencrypted-file warning (IO-05); formula neutralisation (AT-55); file in the app cache |
| DF-07 | CSV import | File chosen by the user | Local processing only | Implemented – verified | No upload; preview; atomic batch with undo (AT-54) |
| DF-08 | Reminders and budget alerts | Occurrence id; generic text by default; plan name, amount and due date only with "Show names and amounts" | Device notification service (Android/iOS, local, no push) | Implemented – verified (unit) | Several reminders at the same minute become one summary, names only with details allowed (REM-10); snoozes keep the same text and link in local preferences until they fire or the occurrence closes (REM-04); contract reminders use the same generic text by default (F2-CON-01); permission asked only when a reminder is turned on (REM-03); tap opens the app only after unlock; Windows: no notifications |
| DF-09 | App lock | Lock-enabled flag in the database; the credential stays with the OS | Device | Implemented – verified (build); device check pending | No PIN or biometric data stored (SEC-01); recent-apps preview hidden and screenshots blocked on Android at all times, iOS covers the app switcher snapshot (D-23); hides the UI only – the database is not encrypted (SEC-03). Earlier revisions listed this flow as DF-20; that id now belongs to attachments |
| DF-10 | Diagnostics / logs | Exception text to the debug output in Debug builds only; no crash-reporting SDK | Local debug output | Implemented – unverified | Verify the release build has no logging provider sending data (SEC-04) |
| DF-11 | Ads / tracking | – | – | Not included | No SDK referenced; no advertising id |
| DF-12 | Billing | – | – | Not included in phase 1 | – |
| DF-13 | AI | – | – | Not included | – |
| DF-14 | Sync / household | – | – | Not included | – |
| DF-15 | Online rates / bank sync / other network use | – | – | Not included | Exchange rates are entered manually; the only network use is DF-04/05 in builds with cloud backup clients; offline builds declare no INTERNET permission |
| DF-16 | **Android Auto Backup** | App database and preferences | User's Google account backup (Google) | Implemented – unverified (`allowBackup=true`, owner decision D-16) | Disclose in policy and Data safety; verify restore on a new device |
| DF-17 | **iOS device / iCloud backup** | App data | User's iCloud or computer backup (Apple) | Implemented – unverified (OS default) | Disclose; iOS release later |
| DF-23 | Local profiles | Profile names and the open profile (preferences, only with more than one profile); one database per profile | Local preferences and databases | Implemented – verified (data tests, build) | Names are visible to anyone who can open the app (said on the page); a profile with the app lock asks for the device owner before it opens (D-34) |
| DF-18 | Preferences | Language, calendar, optional region (never from location), first day of the week, theme, Persian digits, currency display units, Home layout, budget alert levels, dismissed Home guidance | Local preferences | Implemented – verified | Included in OS backups (DF-16/17), not in the app's backup package |
| DF-20 | Attachments | Receipt photos and PDF files the user picks for an entry (file name, type, size, content); on Android and iOS photos are re-encoded as JPEG without metadata, and a photo that cannot be re-encoded is not stored (the Windows development build keeps the original) | App database; included in the encrypted backup (DF-03) and OS backups (DF-16/17); never in CSV/PDF exports; opened through a temporary cache copy with an app the user chooses; the copies are deleted on the next start and by "Delete all data" | Implemented – verified (data tests) | System file picker, and on phones "Take a photo" (D-38): Android hands the photo to the device's camera app (no camera or storage permission; the photo goes into a cache folder shared only through Zanance's own file provider and is deleted once read), iOS asks for the camera with its usage description; no storage or photo-library permission; removed at the next start after their entry is deleted (undo can still restore them before) and by 'Delete all data' |
| DF-21 | Quick add widget (Android) | None: three buttons that open the entry editor | Home screen | Implemented – verified (build) | Shows no amounts and reads no data; the app lock applies before the editor opens (D-30) |
| DF-22 | Receipt reading | The photo of an attachment | On-device text recognition: Vision (iOS), Windows.Media.Ocr (Windows), Google ML Kit with the bundled model (Android) | Implemented – verified (parser tests; Windows engine checked with a rendered receipt); Android and iOS device checks pending | Values only fill the editor for review (D-31). ML Kit includes Google's usage-statistics transport; the offline release manifest has no INTERNET or ACCESS_NETWORK_STATE permission, so nothing can be sent – verify on the release build. A build with cloud backup keeps INTERNET, so ML Kit may send usage statistics while online (disclosed in the policy) |
| DF-19 | Syncfusion license validation | License key compiled into the app (build secret via `eng/AppSecrets.targets`, never tracked) | Local check | Implemented – verified (offline validation test `Vafadar.SyncfusionLicense.Tests`) | Confirm no network call in the release build (none possible without INTERNET on Android). Syncfusion's usage telemetry is switched off at startup (`Telemetry.Disable()`), also for builds with cloud backup |

## Permissions

iOS: `NSFaceIDUsageDescription` (app lock) and `NSCameraUsageDescription` (receipt photos, D-38), localised in `Platforms/iOS/*.lproj/InfoPlist.strings`; notifications use the system authorisation dialog. Windows: no capability beyond `runFullTrust` (files only, no camera). Android manifest:

| Permission | Status | Reason |
|---|---|---|
| POST_NOTIFICATIONS | Present | Reminders; offered once after the first start with an explanation, then requested when the user turns a reminder on; once the system no longer asks, "Turn on" opens the app's notification settings (D-38) |
| RECEIVE_BOOT_COMPLETED | Present | Scheduled reminders are restored after a restart (REM-07) |
| INTERNET | Removed in offline builds (D-20); kept in builds with cloud backup clients (D-35) | Only for the user's own Google Drive / OneDrive after they connect; Debug builds add it for the debugger |
| ACCESS_NETWORK_STATE | Removed | Not needed |
| Exact alarms | Not requested | Reminders are inexact (REM-09) |
| USE_BIOMETRIC, USE_FINGERPRINT | Merged from AndroidX Biometric | App lock (SEC-01); the system dialog handles the credential, the app never sees it. Listed in the merged manifest (`obj/…/AndroidManifest.xml`); normal permissions, no runtime prompt |

Any change to SDKs, permissions, backup destinations, sign-in, billing or AI must update this file (MAT-05).
