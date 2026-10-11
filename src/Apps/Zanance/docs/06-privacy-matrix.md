# 06 – Zanance privacy matrix

App `pro.vafadar.zanance`, version 0.1.0 (development). Documentation alignment refreshed 2026-10-09 (D-88), retaining the build-specific SDK review and D-62/D-63/D-67 policies; real-provider/device/iOS gates remain open.
Statuses: Implemented – verified / Implemented – unverified / Planned / Not included / Unknown – needs verification.
"Verified" means covered by automated tests or a reviewed build artifact; **device checks** (release APK on a real
phone) are still pending for every row and are a release gate (08).

This is the app's privacy profile required by MAT-01..04. The owner's starter template is
`spec/Zanance-Privacy-Matrix-Starter.md`; its flow ids DF-01..DF-15 are kept, DF-16..DF-20 were added here and are mirrored in the starter.

D-62: backup protection is on by default; the choice alone is remembered on this device. No backup password is
stored or uploaded. Cloud sign-in controls access to the provider account; an unprotected downloaded file remains
readable by anyone who obtains it. First-run restore creates no extra account and uses the existing safety copy.

## Portfolio summary

| Online account | Local ledger | Ads / Analytics / AI | Pro | Privacy policy | Data safety |
|---|---|---|---|---|---|
| No mandatory app account; optional configured provider backup sign-in | Plaintext SQLite in app-private storage | No application ads/analytics/AI feature; ML Kit diagnostics possible in Cloud (SEC-09) | Free/Plus/Pro design (D-61); no billing/quota enforcement | Draft (`docs/privacy/privacy-policy.md`) – must be published before release | To be completed from the release build |

## Data flows

| ID | Flow | Data | Destination | Status | Evidence / open verification |
|---|---|---|---|---|---|
| DF-01 | Accounts and entries | Amounts, currencies, dates, account names, categories, titles, payees, notes; quick templates; saved list filters; who pays back a reimbursable expense (exported with payees and notes only when chosen); tags; categorization rules; interest rate and monthly installment of loans; lender or borrower of a loan | Local database (`FileSystem.AppDataDirectory`) | Implemented – verified | Store and ledger tests; no network code in Zanance projects. "Delete all data on this device" (Settings, two confirmations) empties the database and restarts onboarding; backup files already shared, exports and OS backups stay (BAK-15) |
| DF-02 | Plans, occurrence states, budgets, exchange rates, savings goals | Due dates, amounts, limits, rates, goal targets and earmarks | Local database | Implemented – verified | Plan, budget and rate tests |
| DF-03 | Local backup file | Full database (all of DF-01/02 and finance settings) in a package with manifest and content counts; optional AES-256-GCM password encryption (D-62) | Kept on the device (last 10) and shared by the user through the system share sheet to any destination they choose | Implemented – verified (library + fresh-install integration test) | Password warning (BAK-06); safety copy before restore; neutral file name (BAK-08); device check pending |
| DF-04 | Google Drive backup | Package with optional password encryption (D-62), account e-mail | User's Drive app data folder (`drive.appdata`) over HTTPS | Implemented – unverified (Android, iOS, Windows; only on a platform whose Google client is configured: `GoogleOAuthClientIdAndroid`, `…Ios`, `…Windows`; D-35, D-50) | Storage tested against fake HTTP. Android: Play services authorization API. iOS and Windows: system browser (`ASWebAuthenticationSession` / default browser with a one-time `127.0.0.1` listener), authorization code + PKCE, no client secret; refresh token only in the keychain (iOS) or a DPAPI file (Windows); token protocol unit-tested against fake HTTP. Grant revoked on disconnect. Device checks with real OAuth clients pending |
| DF-05 | OneDrive backup | Package with optional password encryption (D-62), account name | User's OneDrive app folder (`Files.ReadWrite.AppFolder`) over HTTPS | Implemented – unverified (Android, iOS, Windows; only in builds with `MicrosoftEntraClientId`, D-35, D-50) | Storage tested against fake HTTP; MSAL through the system browser (no broker), token cache on the device (MSAL storage on Android, keychain group of the bundle id on iOS, DPAPI on Windows), removed on disconnect; device checks pending |
| DF-06 | CSV and PDF export / share | CSV: entries of the chosen period, notes and payees optional. PDF: the report of a period (totals, categories, accounts, plans, tags) | App chosen by the user via the share sheet | Implemented – verified | Unencrypted-file warning (IO-05); formula neutralisation (AT-55); file in the app cache |
| DF-07 | CSV import | File chosen by the user | Local processing only | Implemented – verified | No upload; preview; atomic batch with undo (AT-54) |
| DF-08 | Reminders and budget alerts | Occurrence id; generic text by default; plan name, amount and due date only with "Show names and amounts" | Device notification service (Android/iOS, local, no push) | Implemented – verified (unit) | Several reminders at the same minute become one summary, names only with details allowed (REM-10); snoozes keep the same text and link in local preferences until they fire or the occurrence closes (REM-04); contract reminders use the same generic text by default (F2-CON-01); permission asked only when a reminder is turned on (REM-03); tap opens the app only after unlock; Windows: no notifications |
| DF-09 | App access and screen protection | Profile device-lock flag; device-wide salted PIN verifier, failed-attempt count and retry deadline; screenshot preference | Platform SecureStorage for verifier/attempts, device preferences for screenshot choice; no PIN state in portable backups | Implemented - verified (20 PIN tests, builds and Windows UI); device/iOS acceptance pending | D-63: PIN masks UI only, not database encryption; current PIN for change/removal; successful device authentication plus confirmation for recovery; read/write failure never unlocks. Android foreground screenshot block optional and on by default, recents always protected; iOS cover remains. Android secure-storage ciphertext excluded from OS backup/transfer; iOS keychain persistence follows OS policy and uninstall need not remove it. |
| DF-10 | Diagnostics / logs | Exception text to the debug output in Debug builds only; no crash-reporting SDK | Local debug output | Implemented – unverified | Verify the release build has no logging provider sending data (SEC-04) |
| DF-11 | Ads / tracking | – | – | Not included | No SDK referenced; no advertising id |
| DF-12 | Billing | – | – | Not included in phase 1 | – |
| DF-13 | AI | – | – | Not included | – |
| DF-14 | Sync / household | – | – | Not included | – |
| DF-15 | Online rates / bank sync / other network use | – | – | Not included | Exchange rates are entered manually; application network flows are DF-04/05; Cloud builds may also permit native ML Kit diagnostics (DF-22); Offline Android Release declares no INTERNET permission |
| DF-16 | **Android Auto Backup** | App database and preferences | User's Google account backup (Google) | Implemented – unverified (`allowBackup=true`, owner decision D-16) | Disclose in policy and Data safety; verify restore on a new device |
| DF-17 | **iOS device / iCloud backup** | App data | User's iCloud or computer backup (Apple) | Implemented – unverified (OS default) | Disclose; iOS release later |
| DF-23 | Local profiles | Profile names and the open profile (preferences, only with more than one profile); one database per profile | Local preferences and databases | Implemented – verified (data tests, build) | Names are visible to anyone who can open the app (said on the page); a profile with the app lock asks for the device owner before it opens (D-34) |
| DF-18 | Preferences | Language, calendar, optional region (never from location), first day of the week, theme, Persian digits, currency display units, Home layout, budget alert levels, dismissed Home guidance | Local preferences | Implemented – verified | Included in OS backups (DF-16/17); allowlisted localization choices also enter new app backup packages (D-67), other device preferences remain local |
| DF-20 | Attachments | Receipt photos and PDF files the user picks for an entry (file name, type, size, content); on Android, iOS and Windows photos are re-encoded as JPEG without metadata, and a photo that cannot be re-encoded is not stored | App database; included in the backup (DF-03, optionally encrypted) and OS backups (DF-16/17); never in CSV/PDF exports; opened through a temporary cache copy with an app the user chooses; the copies are deleted on the next start and by "Delete all data" | Implemented – verified (data tests) | System file picker, and on phones "Take a photo" (D-38): Android hands the photo to the device's camera app (no camera or storage permission; the photo goes into a cache folder shared only through Zanance's own file provider and is deleted once read), iOS asks for the camera with its usage description; no storage or photo-library permission; removed at the next start after their entry is deleted (undo can still restore them before) and by 'Delete all data' |
| DF-21 | Quick add widget (Android) | None: three buttons that open the entry editor | Home screen | Implemented – verified (build) | Shows no amounts and reads no data; the app lock applies before the editor opens (D-30) |
| DF-22 | Receipt reading | Transient original for new-photo recognition; compact stored photo for rereading, extracted source rows and total candidates held only in the unsaved form | On-device text recognition: Vision (iOS), Windows.Media.Ocr (Windows), Google ML Kit with the bundled model (Android) | Implemented – verified (parser tests; Windows engine checked with a rendered receipt); Android and iOS device checks pending | Values only fill the editor for review (D-31). ML Kit includes Google's usage-statistics transport; the offline release manifest has no INTERNET or ACCESS_NETWORK_STATE permission, so nothing can be sent – verify on the release build. A build with cloud backup keeps INTERNET, so ML Kit may send usage statistics while online (disclosed in the policy) |
| DF-19 | Syncfusion license validation | License key compiled into the app (build secret via `eng/AppSecrets.targets`, never tracked) | Local check | Implemented – verified (offline validation test `Vafadar.SyncfusionLicense.Tests`) | Confirm no network call in the release build (none possible without INTERNET on Android). Syncfusion's usage telemetry is switched off at startup (`Telemetry.Disable()`), also for builds with cloud backup |

## Permissions

iOS: `NSFaceIDUsageDescription` (app lock) and `NSCameraUsageDescription` (receipt photos, D-38), localised in `Platforms/iOS/*.lproj/InfoPlist.strings`; notifications use the system authorisation dialog. Windows: no capability beyond `runFullTrust` (files only, no camera). Android manifest:

| Permission | Status | Reason |
|---|---|---|
| POST_NOTIFICATIONS | Present | Reminders; offered once after the first start with an explanation, then requested when the user turns a reminder on; once the system no longer asks, "Turn on" opens the app's notification settings (D-38) |
| RECEIVE_BOOT_COMPLETED | Present | Scheduled reminders are restored after a restart (REM-07) |
| INTERNET | Removed in offline builds (D-20); kept in builds with cloud backup clients (D-35) | Cloud APIs plus possible native SDK diagnostics before sign-in; Debug adds access for its debugger |
| ACCESS_NETWORK_STATE | Removed | Not needed |
| Exact alarms | Not requested | Reminders are inexact (REM-09) |
| USE_BIOMETRIC, USE_FINGERPRINT | Merged from AndroidX Biometric | App lock (SEC-01); the system dialog handles the credential, the app never sees it. Listed in the merged manifest (`obj/…/AndroidManifest.xml`); normal permissions, no runtime prompt |

Any change to SDKs, permissions, backup destinations, sign-in, billing or AI must update this file (MAT-05).

D-64: original photo bytes are transient during recognition only; a separate upright copy is bounded to 3200 px,
while only the 1600 px metadata-free JPEG is attached and backed up. Source rows/candidates/currency evidence are
not additional database fields or exports. No new permission, SDK or network path. A stored-image reread cannot
recover lost detail. Recognition/rendering errors and cancellation do not post ledger entries.

D-65 uses existing account counterparty/reference balance/interest/installment and Schedule fields only. A reminder
draft and its preview are transient until explicit Save; opening it records no ledger entry. Portable backups retain
their existing schema. No new data category, SDK, network path or permission; notification permission is requested
through the existing contextual reminder flow. Estimates never become principal payments automatically.

### D-67 – Portable display preferences and cloud discovery

New packages include allowlisted language, calendar, region, numeric formatting culture, digit shapes and week start
in display-settings.json. They share the package's optional protection and destination. Theme and device security
choices remain device-local; credentials, PIN state and tokens are excluded. Older packages do not contain this
source and leave current display choices intact. Connected cloud listing reads file metadata automatically when
the backup page opens and after connection; no automatic upload or wider file permission is introduced. No location,
SDK, network destination or database field was added. Germany's holidays remain nationwide-only.


D-68 / SEC-01: independent fictitious-only encryption experiment under a separate Android package. No production
SDK, permission, user-data field, connection, export or key handling changed. Current plaintext database/safety-copy
and OS-backup boundaries remain; proposed ADR 0010 must not be represented as implemented protection.


D-69 / SEC-09: the current SDK/build review is [recorded here](../../../../docs/privacy/zanance-sdk-review.md).
Android Cloud grants process-wide INTERNET before sign-in; ML Kit diagnostics may be transmitted independently of
a connected account. No application advertising/analytics/crash service is added. Complete Release APK permission
checks cover Offline and Cloud variants; iOS signed-binary/native-traffic and future billing/sync reviews remain gates.

D-70 / LOC-02 extends DF-08 with optional goal-contribution notifications: goal id/deep link and contribution date
enter only the local device scheduler. Goal names/dates appear in notification text only with details opted in;
otherwise title and body are generic. The existing contribution-plan flag is in database backups; native permission
is device-local. Paused/completed/reached/unavailable goals stop pending reminders. Taps pass the app lock and never
record contributions or transfer money. No new SDK, permission, external recipient or schema.

D-71 / LOC-03 extends DF-08 with optional period review reminders. ReviewReminderEnabled is an off-by-default
profile/database setting and travels in existing portable backups. Pending requests contain a local delivery date,
stable id and generic review deep link; period labels are included only with the existing notification-details opt-in.
No amounts/accounts in these requests, no financial/automatic-review writes, new permission, SDK or network path.
Notification permission and scheduling stay on the device. The independent Debug fixture is not present in Release.

## Aggregate import linking (D-72)

DF-07 additionally retains a profile-local ImportLinks journal for explicitly linked imports: imported entry ids and
financial metadata, before/after aggregate amounts and the matched details. No attachment bytes are duplicated;
attachments of a consumed original aggregate remain available to Undo after restart. The journal contains historical
financial data until its import is undone or all profile data/the profile is deleted, and travels inside database
backups. It is absent from CSV exports. Same optional password protection and current plaintext-on-device boundary
apply. No credentials/device security, new permission, SDK, telemetry, recipient or network path.

## Local font-scale review (D-77)

No production data field, permission, SDK, portable preference, recipient or network path is added. Debug-only
explicit Windows snapshots record fictitious rendered text and native geometry in local artifacts.
Temporary emulator scale probes inspected only the separate QA profile and omitted editable contents; they were
removed from source after configuration failures. No global device setting was changed. Scale diagnostics are
absent from Release and neither encrypt nor export the database.

## Local layout review (D-78)

No production data field, portable preference, permission, export or SDK is introduced. Windows Debug-only review
writes local actual-layout JSON and renders its own application window with fictitious data. Undo preview only
sets/restores presentation visibility: no deletion, Undo invocation or ledger write. Android review uses only the
explicitly selected existing emulator and owned fictitious profile; screenshot protection and system settings
remain. Diagnostic collectors and previews are absent from Release. Original Windows data files are restored with
matching hashes. Screen-reader, OS and physical-device acceptance remain distinct from local engineering evidence.

## Settings display/read review (D-79)

No production data field, portable preference, permission, export, SDK, recipient or network path is added.
Existing preferences and financial rows are read to publish Settings; availability checks do not authenticate or
ask permission. The separate settings-display Debug route uses fictitious loading/failure/display states, invokes
the actual retry, preserves unsaved input and asserts unchanged stored preferences/entries. It never sets a PIN,
saves an estimate or changes notification permission. Own-window captures/local proof files are absent from Release.
Original Windows development data is restored with matching hashes; native inspection uses only the known owned
emulator fixture and retains screenshot protection. No owner database or SecureStorage content is inspected.

## Growing-action layout review (D-80)

No production data, permission, SDK, export, recipient or portable preference is added. The actual command and
security/financial confirmation boundaries remain. Debug-only own-window/native geometry review uses fictitious
data and invokes only Select all/Cancel, never Save/delete/Undo, PIN or permission changes. Full entry JSON remains
unchanged. Original Windows development databases are preserved/restored with matching hashes. Native checks use
only the owned emulator fixture, keep screenshot protection/system settings, and do not inspect owner data/secrets.
Collectors are absent from Release. UI geometry/CI is separate from physical OS and screen-reader acceptance.

## Date/amount presentation review (D-81)

No production data field, portable preference, permission, SDK, export, recipient or network path is added.
Date/amount controls preserve existing values and financial/security boundaries. Debug-only draft, native geometry
and own-window review uses fictitious profiles without Save; valid/partial dates and complete entry JSON are
checked separately. Original development files are restored with matching hashes. Native review uses only the
known owned emulator fixture, preserves screenshot protection/system settings and excludes owner data/secrets.
Collectors are absent from Release; presentation evidence is separate from OS/screen-reader/device acceptance.


## Reopened Settings language review (D-82)

No data field, SDK, permission, portable preference, network path, export or recipient is added. Retired navigation
translation bindings are detached; active forms, lock coverage and security choices retain existing behavior.
Synchronous constructor defaults no longer save notification settings. Debug-only native selection/back and
own-window proof use fictitious profiles without Save/PIN/permission actions; original development files are
restored with matching hashes. Native review uses only the owned emulator fixture, retains FLAG_SECURE/system
settings and excludes owner data/credentials/SecureStorage. Release excludes all review instrumentation.

## Growing headers and live Back descriptions (D-83)

Presentation-only: no data field, portable preference, permission, SDK, network path, export or recipient.
Windows retains the actual page body/bindings and native Back handling; Android retains its native arrow/commands
with a lifetime-scoped translation subscription. Debug-only own-window/native geometry checks use fictitious
profiles without Save; original development files are restored with matching hashes. Emulator review retains
FLAG_SECURE and system settings, excludes owner data/credentials/SecureStorage and physical devices.

## Open estimate draft retention (D-84)

Draft text/period/currency and publication context remain in memory only; no new stored field, portable preference,
export, network path, SDK or permission. Refresh never saves an estimate; existing explicit Save validation remains.
Profile/settings-row scope prevents draft carryover. Runtime review uses fictitious profiles and preserves original
development files/hashes; emulator evidence excludes owner data/credentials and system/security changes.

D-85 changes only the Windows visible Insights navigation. No data, SDK, permission, export or device-security
policy change. AT-92 uses fictitious route navigation without financial Save and compares complete stored rows.

D-86 changes budget layout and signed monetary formatting only. No SDK, permission, stored data model, export,
network or device-security change. Runtime presentation fixtures are in memory with no Save; compare complete
stored accounts/entries/settings/budgets/plans before and after. Android verification uses only the independently
owned fictitious database and restores its original complete financial rows; no private profile/security reads.
The owner archive and real financial data are outside these checks. Evidence: quality/budget-readouts.md.

D-87 changes the budget period choice layout only. No SDK, permission, stored data model, export, network or
security change. Actual native choice invocation is view navigation without Save; compare complete accounts/
entries/settings/budgets/plans and restore the original in-memory choice. Android checks use only the known
independently owned sample database with complete fixture restoration, never other profiles/private security
storage or the owner archive. Evidence: quality/budget-periods.md.

## Tag suggestion layout (D-89)

Growing captions and typed display/raw tag values introduce no new stored field, portable source, permission, SDK,
export, recipient or network behavior. Existing tag normalization/limits and explicit financial Save remain. Native
review modifies only an unsaved fictitious draft and compares stored rows; diagnostics are absent from Release.

## Transaction detail disclosure (D-90)

State-matched captions and growing targets add no stored data, portable field, export, permission, SDK or network
behavior. Hiding retains existing in-memory drafts; only explicit Save persists them. Native review uses fictitious
inputs and restores complete stored data; diagnostic routes remain absent from Release.

D-91 changes first-run action presentation only; no new stored/portable field, permission, SDK, recipient or
export. Restore/return retains the unsaved wizard and does not create an account or replace financial data.

D-92 changes in-memory Home snapshot publication and goal ledger routing only. No new stored/portable data,
permission, SDK, recipient or export; actual sample readbacks compare complete original tables without writes.

D-93 changes a Debug platform guard only, with no production data/permission/SDK/export change. Temporary QA
stage metadata is restricted to exact fictitious profiles, contains names/durations only and is removed from
source and the owned cache before final handoff. Complete original sample tables remain unchanged.

D-94 changes Home customization presentation only. It retains the existing profile HomeLayout setting and its
backup rules, with no new data, permission, SDK, security or export field. The Debug walk-through exercises native
layout actions only in fictitious settings; complete financial rows/other preferences stay equal and original
development database files restore with matching hashes. Independent read prototypes are not app dependencies.

D-95 changes view lifetime only: no data/permission/SDK/security/export field is added. Complete financial
snapshots remain; only hidden native account cards are absent. Temporary instrumentation records counts/timings for
exact independently owned fictitious samples, is removed before final builds, and its validated cache files are
removed. Native layout tests restore only their own fictitious HomeLayout/auditing time and compare every other
column of all original tables; complete original rows/integrity are verified after restoration.

D-96 changes account description layout only; no new data, permission, SDK, export, portable preference or
security field. The Debug geometry fixture changes only existing fictitious presentation flags, restores the full
snapshot and compares complete stored data without Save. Normal signed Release navigation is read-only; exact
original columns/rows of all 24 tables in three independently owned sample profiles and integrity are verified.

D-97 changes only the existing debt entry action layout; no new data, permission, SDK, export, security field
or portable preference. The fictitious native review opens/cancels an unsaved debt draft, compares complete stored
data including schedules and never saves or posts principal. Exact 24-table readbacks of three owned samples pass.

D-98 changes only existing modal header presentation. No new data, permission, SDK, export, security field
or portable preference. Reviews open and cancel fictitious drafts without Save/posting and compare complete stored
data; Android app language/theme changes are restored through UI. Original three-profile 24-table readbacks pass.

D-99 changes existing plan validation/display only. No new data, permission, SDK, export, credential,
security field or portable preference. Review invokes Save only with independently invalid blocking input, never
posts money and compares complete stored data. Fictitious cross-currency choices are presentation-only and restored.
Android app language/theme are restored through UI; original three-profile 24-table readbacks pass.

D-100 changes existing transaction validation/display only: no new data, permission, SDK, export, credential,
security field or portable preference. Native reviews use independently invalid blocking input, compare complete
stored data and restore presentation-only foreign-account choices. No financial posting; original development
files and three fictitious Android profiles remain. App language/theme are restored through native UI.

D-111 / Repeat: detached existing transaction fields pass only through in-memory navigation into an unsaved
Plan. Unsupported transaction-only details remain in the original form. No new permission/SDK/security field,
portable preference/schema or backup format. Explicit Plan Save uses the existing Schedule storage/export policy;
opening/returning never writes money. Original ISO currencies prevent silent account-change reinterpretation.

D-131 / explicit resource choice: immutable in-memory scoped identity lists in the final commercial policy model,
with no persisted selection, portable preference, entitlement, credential, SDK, permission or network addition.
Actual writer rereads existing financial rows for retained payments and reviewed bill settlement. Existing money,
receipt, backup and erasure policies apply unchanged. The test-build source remains inactive; no paid facts enter backups.

D-133 uses existing occurrence/link fields and cached exact-file rights; no schema, migration, backup field,
credential, SDK, network call or permission addition. Actual financial metadata and receipt ownership/bytes remain.
The normal test-build source remains inactive; no paid facts or selection preferences enter portable backups.

D-134 reuses existing ledger/occurrence fields, exact-file cached access and actual payment rows. No schema,
backup field, credential, permission, SDK or network addition. No paid facts are stored in portable preferences;
normal commercial registration stays inactive. Complete rejected writes preserve all original financial rows.

D-135 reuses existing financial state/link fields and the file-bound deletion Undo journal. No schema, portable
preference, credential, permission, SDK or network addition. Preserve complete manual money, refund relationships
and receipt bytes/ownership; no commercial facts are stored in backups. Current registration remains inactive.

D-136 reuses existing local draft fields, writers and translated native dialogs. No schema, portable preference, credential, permission, SDK or network addition. User-facing errors contain no raw exception details. Commercial registration remains inactive.

D-138 adds immutable exact-scope cached plan choices in memory only and reuses existing plan/occurrence/ledger fields. No schema, portable preference, credential, SDK, network or permission addition. No paid fact or selection is exported; current commercial registration stays inactive.

D-139 projects original plan/state data through private in-memory selected-work identities. No new stored data,
portable preference, credential, SDK, network or permission. Forecasts/history retain complete original input;
rejected new occurrence writes preserve complete rows. Commercial registration remains inactive.

D-140 extends the existing device-local notification link/snooze cache with a SHA256 identifier of the actual
full database path and at most 30 original plan/date members. The identifier grants no right and contains no raw
path. No new schema, SDK, permission, network, security credential or portable backup setting. Existing snooze
text is refreshed from current privacy/display/state; unscoped legacy caches are retired without guessing a profile
or group. Shared pending/snooze caps stay 30/20. No ledger, due-date or profile write from delivery/taps/actions.

## Durable account and Plan choices (D-141)

ResourceSelections stores only kind, verified financial scope kind/id, canonical selected original entity ids and
a random change revision inside each profile database. It contains no paid facts, membership proof, credentials,
PIN or device security. The normal database backup includes these local financial choices; imported/restored scopes
are never guessed or reassigned. Current inactive registration creates no choice rows. No new SDK, network,
permission or security-storage use. Selection does not alter original financial data or grant service access.
