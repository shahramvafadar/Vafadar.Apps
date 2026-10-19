# 06 – Zanance privacy matrix

App `pro.vafadar.zanance`, version 0.1.0 (development). Current summary aligned 2026-10-15 (D-287), retaining the build-specific SDK review and D-62/D-63/D-67 policies; real-provider/device/iOS gates remain open.
Statuses: Implemented – verified / Implemented – unverified / Planned / Not included / Unknown – needs verification.
"Verified" means covered by automated tests or a reviewed build artifact; **device checks** (release APK on a real
phone) are still pending for every row and are a release gate (08).

This is the app's privacy profile required by MAT-01..04. The owner's starter template is
`spec/Zanance-Privacy-Matrix-Starter.md`; its flow ids DF-01..DF-15 are kept, DF-16..DF-20 were added here and are mirrored in the starter.

D-62: portable backup password protection is on by default; the choice alone is remembered on this device. No backup password is
stored or uploaded. Cloud sign-in controls access to the provider account; an unprotected downloaded file remains
readable by anyone who obtains it. First-run restore creates no extra account and uses the existing safety copy.

## Portfolio summary

| Online account | Local ledger | Ads / Analytics / AI | Pro | Privacy policy | Data safety |
|---|---|---|---|---|---|
| No mandatory app account; optional configured provider backup sign-in | SQLCipher financial databases on Android/Windows; iOS integration pending | No application ads/analytics/AI feature; ML Kit diagnostics possible in Cloud (SEC-09) | Free/Plus/Pro design (D-61); local quota boundaries present, commercial activation off | Draft (`docs/privacy/privacy-policy.md`) – must be published before release | To be completed from the release build |

## Data flows

| ID | Flow | Data | Destination | Status | Evidence / open verification |
|---|---|---|---|---|---|
| DF-01 | Accounts and entries | Amounts, currencies, dates, account names, categories, titles, payees, notes; quick templates; saved list filters; who pays back a reimbursable expense (exported with payees and notes only when chosen); tags; categorization rules; interest rate and monthly installment of loans; lender or borrower of a loan | Local database (`FileSystem.AppDataDirectory`) | Implemented – verified | Store and ledger tests; no network code in Zanance projects. "Delete all data on this device" (Settings, two confirmations) empties the database and restarts onboarding; backup files already shared, exports and OS backups stay (BAK-15) |
| DF-02 | Plans, occurrence states, budgets, exchange rates, savings goals | Due dates, amounts, limits, rates, goal targets and earmarks | Local database | Implemented – verified | Plan, budget and rate tests |
| DF-03 | Local portable backup file | Full database (all of DF-01/02 and finance settings) in a package with manifest and content counts; optional AES-256-GCM password encryption (D-62) | Kept on the device (last 10) and shared by the user through the system share sheet to any destination they choose | Implemented – verified (library + fresh-install integration test) | Password warning (BAK-06); safety copy before restore; neutral file name (BAK-08); device check pending |
| DF-04 | Google Drive backup | Package with optional password encryption (D-62), account e-mail | User's Drive app data folder (`drive.appdata`) over HTTPS | Implemented – unverified (Android, iOS, Windows; only on a platform whose Google client is configured: `GoogleOAuthClientIdAndroid`, `…Ios`, `…Windows`; D-35, D-50) | Storage tested against fake HTTP. Android: Play services authorization API. iOS and Windows: system browser (`ASWebAuthenticationSession` / default browser with a one-time `127.0.0.1` listener), authorization code + PKCE, no client secret; refresh token only in the keychain (iOS) or a DPAPI file (Windows); token protocol unit-tested against fake HTTP. Grant revoked on disconnect. Device checks with real OAuth clients pending |
| DF-05 | OneDrive backup | Package with optional password encryption (D-62), account name | User's OneDrive app folder (`Files.ReadWrite.AppFolder`) over HTTPS | Implemented – unverified (Android, iOS, Windows; only in builds with `MicrosoftEntraClientId`, D-35, D-50) | Storage tested against fake HTTP; MSAL through the system browser (no broker), token cache on the device (MSAL storage on Android, keychain group of the bundle id on iOS, DPAPI on Windows), removed on disconnect; device checks pending |
| DF-06 | CSV and PDF export / share | CSV: entries of the chosen period, notes and payees optional. PDF: the report of a period (totals, categories, accounts, plans, tags) | App chosen by the user via the share sheet | Implemented – verified | Unencrypted-file warning (IO-05); formula neutralisation (AT-55); file in the app cache |
| DF-07 | CSV import | File chosen by the user | Local processing only | Implemented – verified | No upload; preview; atomic batch with undo (AT-54) |
| DF-08 | Reminders and budget alerts | Occurrence id; generic text by default; plan name, amount and due date only with "Show names and amounts" | Device notification service (Android/iOS, local, no push) | Implemented – verified (unit) | Several reminders at the same minute become one summary, names only with details allowed (REM-10); snoozes keep the same text and link in local preferences until they fire or the occurrence closes (REM-04); contract reminders use the same generic text by default (F2-CON-01); permission asked only when a reminder is turned on (REM-03); tap opens the app only after unlock; Windows: no notifications |
| DF-09 | App access and screen protection | Profile device-lock flag; device-wide salted PIN verifier, failed-attempt count and retry deadline; screenshot preference | Platform SecureStorage for verifier/attempts, device preferences for screenshot choice; no PIN state in portable backups | Implemented - verified (20 PIN tests, builds and Windows UI); device/iOS acceptance pending | D-63/D-266: PIN is a separate UI gate; device-only unlock also requires Success, while at-rest encryption is independent; current PIN for change/removal; successful device authentication plus confirmation for recovery; read/write failure never unlocks. Android foreground screenshot block optional and on by default, recents always protected; iOS cover remains. Android secure-storage ciphertext excluded from OS backup/transfer; iOS keychain persistence follows OS policy and uninstall need not remove it. |
| DF-10 | Diagnostics / logs | Exception text to the debug output in Debug builds only; no crash-reporting SDK | Local debug output | Implemented – unverified | Verify the release build has no logging provider sending data (SEC-04) |
| DF-11 | Ads / tracking | – | – | Not included | No SDK referenced; no advertising id |
| DF-12 | Optional local Android billing (D-295; store/iOS acceptance pending) | Native store product/price/receipt references, opaque installation or confirmed-identity binding, signed private entitlement state | Selected native store and fixed owned verification service; provider credentials stay service-side | Explicit billing build only; no purchases or commercial quotas activated by local fixtures | Private device cache/keys and service purchase ownership/verification journals are separate from financial data and portable exports; real store/TLS/retention acceptance remains open |
| DF-13 | AI | – | – | Not included | – |
| DF-14 | Sync / household | – | – | Not included | – |
| DF-15 | Online rates / bank sync / other network use | – | – | Not included | Exchange rates are entered manually; application network flows are DF-04/05; Cloud builds may also permit native ML Kit diagnostics (DF-22); Offline Android Release declares no INTERNET permission |
| DF-16 | Android OS backup and transfer | Financial databases, key envelopes, internal copies and sensitive preferences/staging | Excluded from Android cloud backup and device transfer | Implemented - locally verified (D-263); OEM/device acceptance pending | allowBackup=false plus complete backup/data-extraction exclusions. New devices restore from a portable Zanance export; do not promise OEM behavior from manifest checks alone. |
| DF-17 | iOS device / iCloud backup | App data in the current unqualified iOS scaffold | OS-default backup behavior remains unverified | Final exclusion planned under approved OD-10; iOS protected runtime/build acceptance open | Do not claim current iOS exclusion or encrypted main storage. Couple final policy to iOS native adoption and verify before that release. |
| DF-23 | Local profiles | Profile names and the open profile (preferences, only with more than one profile); one database per profile | Local preferences and databases | Implemented – verified (data tests, build) | Names are visible to anyone who can open the app (said on the page); a profile with the app lock asks for the device owner before it opens (D-34) |
| DF-18 | Preferences | Profile finance/layout choices and device-wide language/calendar/region/theme/digit choices; no inferred location | Profile database and device-local preference/security stores according to each choice scope | Implemented - locally verified; device/iOS acceptance open | Android excludes local stores from OS backup/transfer. Only approved portable display/profile preferences enter Zanance packages; device security, credentials and theme do not become portable. iOS final exclusion remains pending. |
| DF-20 | Attachments | User-chosen receipt photos/PDFs, names/types/content; compact metadata-free stored photos | Protected financial DB on Android/Windows; optional-password portable backup; plaintext temporary external-viewer copies; not CSV/PDF report exports | Implemented - locally verified; phone/iOS acceptance open | Android OS backup/transfer excludes this storage. iOS policy is not yet accepted. Keep original picker/contextual camera behavior, Undo ownership and startup/deletion cleanup; encryption cannot protect copies already shared with another app. |
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

## D-142 choice-form privacy boundary

The new Settings form reads original local resource identity/details/state and explicitly saves only the existing
D-141 scoped choice row. Unsaved drafts remain in memory. No new data entity, schema, credential, permission, SDK,
telemetry or network operation; no money/state write on selection, reload, language/help/navigation. Commercial
registration remains inactive. Fictitious limited-layout inputs exist only in an independently owned Debug review
database/page and are absent from Release. [Evidence](quality/active-item-chooser.md).

## D-143 goal-choice privacy boundary

Existing profile-local ResourceSelections can contain explicitly chosen original goal ids under verified financial
scope. No entitlement, membership, credential or device-security value is stored. Goal metadata projects existing
columns; no schema/entity, SDK, permission, network or telemetry change. Choice/lifecycle writes never write ledger
money; retained history remains. Registration stays inactive. Native checks use the exact owned fictitious QA03
file and restore every original byte/sidecar. [Evidence](quality/goal-resource-choices.md).

## D-144 goal-form privacy boundary

The existing Settings form reads original local goal identity/measurement/currency/state and explicitly saves only
the D-143 scoped choice row. Unsaved choices stay in memory. No schema, credential, device-security, SDK, permission,
network or telemetry change. No financial write from selection, reload, language, Help or Back. Registration remains
inactive. Limited-layout input stays only in an independent Debug fixture/page, never Release DI. [Evidence](quality/goal-chooser.md).

## D-145 scoped goal-reminder privacy boundary

Existing native pending returning data now carries bounded original goal ids/rule dates and a SHA-256 opaque
actual-file identity, never the file path or an authorization token. Existing generic/detail opt-in, permission,
09:00 device-local scheduling and capped shared queue remain. Goal/rule/allocation validation stamps stay only in
memory; no schema, portable preference, credential, security, SDK, network or telemetry addition. Taps recheck the
current actual file after unlock and never switch profiles or post money. Current enforcement remains inactive.

[Evidence](quality/goal-work-reminders.md).

## D-146 goal-view and draft boundary

Existing goal/history/correction data remain profile-local. Complete goal/rule/allocation metadata and account/holding
unit/availability stamps are transient in-memory guards; no schema or portable/security preference is added. Dirty drafts
retain their original numeric culture/currency only in memory. Native-effect test boundaries introduce no SDK, permission,
telemetry, credential or network flow. An explicit earmark release writes one negative allocation, never ledger money or
an account/holding value. Current commercial registration stays inactive.

[Evidence](quality/goal-work-views.md).

## D-147 goal-editor data boundary

Transient exact-file review and in-memory drafts only; no new portable/device preferences, schema, SDK, permission or credential.
An explicit budget cut changes only the confirmed category limit after current-period/full-input validation. No implicit
goal, allocation, account, valuation or ledger write. Opening never requests permission; paused reminder configuration
never resumes delivery. Original data/sidecars/display are restored after independently owned native reviews.

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

## Budget definition choices (D-148)

The existing profile-local scoped choice row can contain canonical budget definitions and a revision, never paid,
membership or security grants. No new schema, SDK, permission or external transmission. Restore does not retag
financial scope or activate commercial rights. Original budget and ledger rows are unchanged by choice Save.
See [evidence](quality/budget-definition-choice.md).

## Reviewed budget chooser (D-149)

Original reference captions are captured in memory to reject stale reviews; no account balance or new financial
data is written. Explicit definition choices use the existing local scoped row/revision. No SDK, permission,
external transmission, schema or security/purchase grant changes. See [evidence](quality/budget-definition-chooser.md).

## Guarded budget alerts (D-150)

Existing financial/privacy inputs are captured temporarily in memory and rechecked locally before native work.
Threshold caches remain device-local, without passwords or paid authority. Generic/details choice remains
explicit. No new SDK, permission, external transmission, schema/model or portable grants. Existing notification
links remain unscoped for Budget and require a later file-bound routing slice. See [evidence](quality/budget-alert-work.md).

## D-151 - Device-local budget routing identity

An opaque actual-file SHA-256 digest and original budget Guid enter only device-local notification returning-data. No paths, credentials, financial values, portable preference, network SDK, permission or schema changes. Resolve after unlock and during page loading; another profile never inherits the destination. See [evidence](quality/budget-reminder-routing.md).

## Original budget editor review (D-152)

Complete original financial references/preferences remain in memory for the covered budget draft and reviewed SQLite writer. No network, SDK, permission, schema, credential, portable preference or export is added. The opaque file handoff grants no authority. Opening/reload/display changes never post money or create a settings row; only explicit Save changes the original budget configuration. Diagnostic fixtures are independent and restored completely.

D-153: budget action review stays local and contains only original budget/reference/preference data. Copy/Delete
change no ledger data; no network, SDK, permission, export, portable grant, credential or security-state change.


D-154: original quick-resource/reference/preference packets and an optional source entry stay in memory for
reviewed local configuration writes. Existing choice rows support two more resource kinds; no schema, network,
SDK, permission, credential, export, portable entitlement or security state is added. Template application is
only an unsaved draft; saved-query viewing posts no money. Independent native fixtures are completely restored.

## D-155 - Process-local original template destination

The reviewed original template handoff is process-local only; it adds no portable preference, credential, device security value, network request, SDK or permission. Template selection/application never saves ledger money. Complete original data and declined interim OS backup policy remain unchanged. Existing provider/certificate, device, iOS and publication acceptance remains open.

## D-156 - Device catalogue and profile activity choice

`profiles.work.catalogue` stores original local profile ids/names, catalogue revision and an optional scoped
device choice/revision in existing device preferences. It is not in the portable preference allowlist and contains
no credential, PIN verifier, financial value or provider identity. Device policy comes from a synchronous cached
port; production registration is inactive. No network, SDK, permission, EF schema or interim OS backup policy
change. Original backup-set resolution follows exact original ids. Native fixtures use independently owned
fictitious catalogues and restore their complete financial data and owned files.

## D-157 - Original file activity review

Existing device-only catalogue and verified access facts compose into an in-memory review stamp. Length-prefixed
nullable original fields are hashed; this stamp is not an authorization token, provider request, portable
preference, purchase or identity migration. It does not enter backups. Complete financial input/history remains
unchanged; backup restore and owned export retain their independently verified rights. Inactive production
capture performs no device-source/preference read. No data schema, permission, SDK, network, credential or interim
OS-backup policy change. Native fixtures remain independent and restore complete original rows/files/display.

## D-158 - Guarded original reminder publication

Complete preferences and first-recorded account date are rechecked in memory against the exact original file.
Display/day facts remain process-local; no new portable field, provider identity or credential is stored. The
existing device-only Snooze cache commits after accepted/current native publication and stays retryable after
refusal. Partial pending batches are removed without deleting delivered notifications. Catalogue events schedule
only an owned read/notification refresh; no financial write or review completion. No new permission, SDK,
network request, schema, analytics or interim OS-backup policy. Independent fixtures restore owned data/display.

## D-159 - Original reminder input witnesses

Complete original rule/state, raw selection and needed dated financial packets are process-local witnesses
for the actual file, never portable grants or credentials. Validation is a deferred read and writes no money,
Settings, allocation or review. No new schema, permission, SDK, network or interim OS-backup policy.
Independent fixtures restore complete owned rows/files/display; production registrations stay inactive.

## D-160 - Structured feedback and process-local name drafts

Known rejection messages use original rights and nonfinancial quota counts, never credentials, provider
responses, stored ids or exception details. Raw names and original file comparison remain process-local, never
portable preferences or grants. No schema, permission, SDK, network, purchase, analytics or OS-backup change.
Independent native fixtures preserve complete original data/owned bytes; test registrations remain inactive.

## D-161 - Captured readiness explanations

WorkRestriction carries existing feature decisions and nonfinancial quota counts in memory, never credentials,
provider responses or stored identities. Raw drafts stay in the existing explicit-Save forms. No schema,
portable format, permission, SDK, network, purchase, analytics or OS-backup change. Independent native fixtures
restore complete original data/owned bytes; production registrations remain inactive.

## D-162 - Read-only plan comparison

Read cached original access for the actual configured file without opening SQLite or creating settings.
Comparison scope/choice stays in memory and never reaches a provider, writer, profile or portable preference.
No price, purchase/renewal, credentials, stored ids, schema, permission, SDK, network, analytics or OS-backup
change. Native fixtures restore complete original data/owned bytes; production registrations stay inactive.

## D-163 - Original editor failure state

Copies and raw drafts stay process-local. Translated feedback exposes no stored identity, source exception
message, credential or purchase data. No EF property/schema, permission, SDK, network, analytics, OS-backup
or portable change. Native fixtures change only independently owned fictitious rows and restore exact data.

## D-164 - Process-local original archive review

Complete original packets remain in memory for one exact file/action/day. No credentials, stored ids or
exception text are shown. No EF property/schema, permission, SDK, network, analytics, OS-backup or portable
change. Writer reviews mutate only their independent fictitious files; original development bytes restore.

## D-165 - Process-local original editor readiness

Account/Plan reviews retain original rows, choices and cached access in memory for their actual file only.
Inactive tests open no financial database for this explanation. No preferences, credentials, portable paid
rights, profile data, financial rows or security settings are written. No new schema, SDK, permission,
network or export data. Fixtures are independently owned; production activation remains separately gated.

## D-166 - Original CSV effects and process-local outcome guards

Existing native CSV picking, app-lock export confirmation and neutral-named UTF-8 BOM CSV cache/share
behavior now sit behind explicit application ports. No new data, export format, SDK, permission, network,
credential or security setting. Accepted original counts/batches and Undo guards stay in memory, never
portable paid facts. Owned fixtures and complete byte/row restoration remain separate from real device,
provider and release acceptance. Test commercial registration is still inactive.

## D-167 - Process-local restore outcome

Accepted package/password/preview is retired before completion; retry state remains process-local.
No new data, SDK, network, permission, credential, portable field or encryption/OS-backup policy.
Existing mandatory safety-copy retention and optional portable protection remain unchanged.

## Forecast/saved-data boundary (D-169)

Existing local accounts, ledger, plans and forecast metadata only. No new portable field, schema,
permission, credential, SDK or network collection. Comparison filters foreign currency without rewriting
saved originals. Cached analytical access is separate from retained owned-data history/deletion.
Commercial registration remains inactive; existing security and backup policy are unchanged.

## Shared original receipt boundary (D-170)

Existing local source/entry/attachment data only. Full-content fingerprint and typed destination are
process-local, not a portable field or log. Recognition original remains transient; only existing
compact metadata-free pending data enters the explicitly saved attachment. No SDK/permission/network/
credential/schema or backup-policy change; original camera explanation and app lock stay intact.
Digital-PDF native verification is not camera/image OCR or physical-device corpus acceptance.

## Original loan review boundary (D-171)

Existing local accounts, terms, ledger and categories only. Complete review/fingerprint is process-local,
not portable or logged. Ordinary explicitly confirmed principal/interest entries use existing schema
and ledger/backup policy. No SDK/permission/network/credential/format or commercial activation change.
Independent native fixtures contain fictitious data and restore exact owned original files/display.

## Report review and owned PDF cache (D-172)

No new financial schema, permission, network SDK, credential storage or commercial activation.
Coherent original input packets are process-local and are not exported as a database dump. Original
PDF remains a readable portable financial document using app-lock consent and the platform share sheet.
Recheck file/data/access/scope around native/render/cache awaits. Remove only this operation's failed
unpublished cache; preserve a file once native sharing starts because platform consumers may read later.
Native fixtures are fictitious and restore original rows/bytes/display/cache; no recipient is selected.
Encryption/OS-backup/provider/device acceptance and final release remain separately gated.

## Data-key context and Windows protection (D-192)

Profile/installation/generation IDs authenticate an OS-protected 32-byte DEK; neither keys nor
installation security metadata enter portable preferences/backups or logs. This slice performs
CurrentUser DPAPI wrapping only, with generated fictitious independent proof files; no live key
store/provider activation. No network/permission/financial schema change. Official .NET ProtectedData
10.0.12 is local Windows infrastructure; existing .NET notices cover it. Owned mutable key copies
are cleared, without claiming whole-process erasure. Permanent Android/iOS OS exclusion is approved
for completed encryption; not yet applied. Optional recovery material/UX remains to implement.

## Android key envelope foundation (D-193)

Only local installation-scoped non-exportable wrapping key and bound opaque DEK envelope; no
network/export/permission or portable credential added. Actual create/read checks occur solely
in the independent fictitious package, which is removed afterwards. Production adapter is not
called by main composition. No OS backup exclusion activated before final encryption. Software
emulator level is not hardware assurance; durable key/recovery/migration/iOS remain open.

## Android opaque key files (D-194)

Bound encrypted DEK envelope and transient ciphertext staging only in caller-owned private files;
no network, permission, key export or portable preference. Reads never accept linked/nonregular
files. Independent proof owns fictitious directories/keys; exact package removed after checking.
Main composition does not use store yet. Final OS exclusion remains coupled to completed encryption.

## Non-secret migration selection file (D-195)

A dedicated versioned private SQLite protocol file holds profile/original/candidate GUIDs and
phase only. No DEK, amount, currency, profile label, financial data, portable path/export preference,
network or permission. Its rollback sidecar contains only that same metadata. Caller provides
trusted private path/access lease; same-UID malicious modification is not cryptographically prevented.
Independent Windows/Android fictitious proof only; main creation/OS exclusion is not activated.
Final encrypted-data and approved OS-backup integration remain required before release.

## Captured encrypted migration and rollback (D-196)

Only caller-owned original database and exact sidecars are deleted after verified keyed selection;
independent encrypted rollback and bound device key remain. Coherent plaintext snapshot stays in
bounded cleared owned memory, never a plaintext temporary file. Native password strings cannot be
fully wiped; same-UID malicious code/flash erasure/hardware power-loss protection is not claimed.
Independent fixture only currently; no main data/key/provider/OS policy activation. No permission,
network, SDK or portable credential. Real backup/safety/attachments/recovery integration remains.

## Protected factory access and permanent backup-format guard (D-197)

Captured creator supplies owned keyed connection; credentials never enter path-only EF options
cache or portable preferences. Provider/plaintext/stale EF access refuses. Caller owns mutable key
and access lifetime; native connection password strings cannot be completely erased. Direct raw
SQL/in-flight commands are not covered by EF retirement. Legacy backup refuses keyed data before
stream/temp work; final generation-aware portable backup remains pending. Independent fixture only;
no main credential/data/permission/network/OS-backup policy activation.

## Keyed portable output (D-198)

Protected database plaintext exists only in owned memory and intentional caller output stream,
not a temporary disk snapshot. Portable password stays optional; unprotected completed package is
readable by its holder. Captured access validates each chunk; partial output on failure cannot be
published as success. Weak registry stores no key material; native strings/full erasure not promised.
Current main plaintext backups unchanged; no new permission/network/portable credential or OS policy.
Generation-aware encrypted restore/safety/recovery and final main adoption remain.

## Encrypted restore candidate (D-199)

Portable plaintext stays in owned memory; a new local bound DEK protects the explicit candidate
before native app migrations. No plaintext temporary database, original deletion, live selector
change, portable credential, permission or network effect. Mutable owned keys/buffers cleared;
full native/string erasure/total peak RAM not promised. Current main unchanged/plaintext; final
safety/package rollback/dispatch/recovery/OS exclusion and acceptance still required.

## Restored selection with previous-data protection (D-200)

Current protected data exports only into owned cleared memory and encrypted rollback. Existing
candidate key is read, never regenerated on Selected resume. No original deletion, plaintext temp,
portable key/credential, network/permission or main OS policy effect. Retain original/rollback/key
until outer transaction owns safe retention. Full process/string/flash erasure not promised.
Main unchanged/plaintext; package/catalogue/provider/recovery integration remains required.

D-202 operational restore capsule stores only allowlisted previous/restored display preferences,
encrypted through reviewed SQLCipher with candidate device key. It is private, retained with rollback,
not a portable source; no device PIN, credentials, SDK/network/permission additions. Main activation
and final retention/OS-backup policy integration remain open.

D-206 private non-secret protocol metadata holds installation/legacy/key-profile/initial generation
GUIDs only. No names, financial rows, key bytes, credentials, paid facts or portable preference entry.
No SDK/permission/network change. Main activation and final OS backup exclusion remain incomplete.

D-208 private restore reference history retains non-secret installation/profile/parent/target GUIDs,
sequence and selected-reference flag only. No keys, names, finance, paths, credentials or portable
entry; no SDK/network/permission changes. Main/final OS-policy and pruning remain open.

D-210: Android stores one device-local random preference persistence-generation marker to force
a checked synchronous snapshot write after MAUI asynchronous writes. It carries no financial
value, credential or user identifier and is excluded by the portable preference allowlist.
No new permission, network SDK or main encryption activation is introduced.


D-223: internal safety protection uses device-local per-copy platform keys, authenticated opaque
profile/installation/generation references and standard AES-256-GCM. Key material is neither
portable nor logged. Protected .vbs files stay outside portable discovery/pruning. Main registration
and final reference-aware retention/OS exclusion remain pending; portable passwords stay optional.

D-226: Windows generation keys persist only as CurrentUser DPAPI envelopes in private .key
files with owner-only non-inherited ACLs; bindings remain non-portable local identity metadata.
No new credential input, network permission, SDK, telemetry or portable preference is introduced.
The independent proof uses fictitious files; final main registration remains inactive.

D-231: retirement metadata contains only existing local non-secret profile/installation/request
identities and ordered phase values. No labels, financial data, keys, portable preferences, new
permission or SDK are added. Final native catalogue/artifact/key retirement is not registered.

D-233: private non-portable retirement metadata additionally records named root/direct filenames,
native file identifiers, lengths and SHA-256. It contains no financial payload or key bytes and
never enters portable preference sources. No new permission, SDK or network behavior is added.
Complete discovery/native mobile cleanup and main registration remain unactivated.

D-234: captured native directory identity additionally binds local retirement inventory. Android
checks actual application UID ownership and descriptor-relative inode identity. No financial
payload, credential input, permission, network SDK or portable preference is added. This local
operational metadata stays outside portable backup sources; main registration remains inactive.

D-235: private retirement discovery authenticates generation envelopes and opaque safety ownership
under maintenance ownership, clears its owned decrypted/key buffers and records only existing native
reference fingerprints. Foreign safety headers do not authorize key access. Cloud/shared installation
keys are outside removal. No new permission, network SDK or portable credential/preference is added.

D-236: private retirement catalogue authority records only before/removed SHA-256 state fingerprints,
not labels or replacement packets. Profile preferences stay in the existing device store and remain
outside portable credential/security sources. No permission, network SDK or new secret input is added.
The main native host is still unregistered; coupled OS policy/recovery adoption remains pending.

D-237: profiles.creation.pending is device-only operational Add metadata, bounded to 64 KiB.
It retains proposed profile names with existing device-profile preference privacy and no financial
rows, key material or credentials. It is outside the portable preference allowlist and is removed
only after native catalogue persistence/completion. No new network SDK or permission is introduced.

D-238 optional recovery: the caller-owned 256-bit recovery key is never persisted/portable. Private
RSA material exists only in a SQLCipher-encrypted local vault; public identity and RSA-wrapped DEKs/
HMAC confirmations live in bounded private operational metadata, without financial rows or labels.
Owned mutable private/key buffers are cleared; managed connection-password strings cannot be fully
erased. Final explicit UX/authentication/reference and coupled OS policy are not activated yet.
No new SDK, permission, network, backup password requirement or plaintext recovery-key file is added.

D-239 retirement captures only recovery id, target count and SHA-256 fingerprint of authenticated
ciphertext/confirmations; no recovery secret, private material or plaintext DEK enters the journal.
Target mappings are removed before native device key files, with other profiles and shared encrypted
private vault preserved. No new permission/export/SDK or interim OS-backup policy is introduced.

D-240 recovered DEKs are stored only as separately protected native recovery entries, with owned
mutable buffers cleared and originals/old aliases preserved. Existing finance verification precedes
publication; the caller must enforce explicit app/device authorization. Recovery secrets are not
saved or used by ordinary cold reads. No permission/SDK/export or interim OS exclusion is introduced.

D-241 receipts contain non-secret bound file/root identities, lengths, SHA-256 and HMAC confirmation,
without DEKs/recovery secrets. Originals remain immutable until explicit target retirement; exact
native receipts authorize only the captured target objects. Other profiles are retained. No new
permission, export, SDK or interim OS-backup exclusion is activated.

D-242 complete recovery retains bounded owned DEK buffers only through verification/native import,
clears them on all exits and disposes private material. Actual original catalogue/identity/binding
facts are rechecked without portable preference writes or new profiles. Explicit user/device
authorization and full native reference verification remain final host duties. No secret logging,
new SDK/permission or interim OS exclusion is added.

D-244: new Android generation keys use independently wrapped purpose-0 envelopes instead of
recreating an installation alias. Legacy reads and receipt-bound recovery remain distinct. Actual
native alias-loss recovery plus a later cold generation read passed; the third observation expired
before the same process's successful result was read without restart. All 3456 tests passed; see
D-244/device-key guide for exact negative and positive evidence. Final protection/adoption and
reference-aware alias retention remain open; main registration remains plaintext.

D-245 adds concrete read-only recovery reference authentication for current/rollback/preference
cipher databases and separately keyed safety references, with exact native object/namespace rechecks.
Five native-file cases and all 3461 tests pass. Actual Android cold all-profile recovery now verifies
two databases/rollbacks/capsules and two safety references after removal of all four fixture envelopes;
see the decision/device-key guide for proof and corpus limits. Main protection remains inactive;
full restore/crash/staged/orphan corpus, final recovery UI/retention/adoption/OS gates remain open.

### D-263 — Android protected application and final OS transfer policy

Original Android financial storage and internal restore safety use official SQLCipher with device-bound
keys and optional recorded recovery. Recovery codes are ephemeral and never logged/portable preferences;
PIN/device security/credentials remain outside the portable allowlist. Explicit backup recovery retains
old financial files, activates a new private root and preserves device access requirements. Password
protection of portable files remains optional, with the existing readable-file explanation.

Final OD-10 disables Auto Backup and excludes all credential/device-protected app root/file/database/
preferences and app external domains in legacy, cloud and device-transfer rules. Cache is already
platform-excluded. The exact signed Release package's manifest/XML rules were checked. These exclusions
are coupled to encrypted Android adoption, not the rejected interim restriction. New devices restore
Zanance backups. Native retirement preserves unrelated/staged/financial-ancestry references and removes
only captured authorized safety/profile objects. [Evidence](quality/android-protected-storage.md).

Windows/iOS adoption, actual phone/OEM transfer, real provider and publication acceptance remain open.
Historical plaintext/probe statements above describe their original commits, not current Android state.

## Windows protected storage (D-264)

Financial profile/rollback files and internal safety packages are device-encrypted on Windows as well as Android. CurrentUser DPAPI protects Windows key envelopes; optional recovery vaults remain separate and portable backup file encryption remains user-selectable. Recovery secrets never enter portable preference sources. Private directory/native identity controls refuse redirected or replaced authority. No permission, telemetry or network SDK was added. iOS implementation and physical-device/provider/Store acceptance remain open.

## D-266 device authentication boundary

Device-only unlock requires successful native authentication. An unavailable credential retains the cover and pending destinations. The last outcome exists in memory only; no new data flow, permission, credential storage or backup field is added. Independent diagnostic fixtures never change device credentials or native PIN storage.

D-268 updates explanations and store drafts to the existing optional backup protection and Android encrypted-storage/OS-exclusion policy. No new data, export, credential storage, SDK or permission is introduced.


## D-272 isolated local service data boundary

The optional service holds fictional identity email, maintained password verifier/security stamp,
one-use challenges, protected mail envelopes, absolute device grants, hashed-IP request windows
and private opaque object manifests/bytes in its own PostgreSQL/private root. Requests carry
credentials/tokens in bounded bodies/headers, never URLs or logs. Files and outbox use framework
Data Protection; Windows wrapping is CurrentUser DPAPI. Identity metadata in PostgreSQL and
quiesced pg_dump is access-restricted, not represented as encrypted financial SQLite or E2EE.

No app SDK, permission, device credential, cloud provider client or mobile transport was added.
No real recipient mail, personal data upload, hosting resource or support ledger access exists.
Local backup includes the private key ring and objects; it is not a portable Zanance user backup.
Do not copy purchase/identity authority into financial backups. Hosted processors, TLS, certificate
provisioning, off-host encrypted snapshots/retention and a real sender require separate acceptance.
Current mobile privacy policy/Data Safety submission status is unchanged; see API README and
quality/local-identity-backend.md for the actual fictional test/restore boundaries.

### Unpublished approved personal-sync extension

The explicitly enabled local personal edition can transmit opted-in financial records and receipt
bytes to the reviewed loopback/emulator-alias HTTPS service. Device-local SQLite replay metadata
and native SecureStorage credentials are separate: portable copies clear all eight replay tables,
and restored generations require explicit reconnection. Authentication/financial payloads are not
logged. Server row/blob bodies use maintained Data Protection; this is not E2EE. No shared-space,
paid hosting, external mail, purchase or production activation follows from these local tests.
Default offline builds do not gain the personal edition's Android network permission.

Windows TLS imports its own wrapped certificate into a runtime-owned current-user key container
for Schannel, without PersistKeySet. The authoritative PFX remains protected; no plaintext PFX or
system trust entry is created. Non-Windows uses memory-only import. Current real wire evidence uses
fictional plaintext SQLite clients against an isolated PostgreSQL/private root; it does not qualify
the original encrypted mobile/native application or Linux certificate/key persistence.

## D-273 offline country calendars

Country choice and its existing portable preference remain unchanged in storage scope. The new
registry contains public statutory dates and public authority URLs only; URLs are provenance,
not runtime requests. No additional personal data, SDK, permission or network traffic. Existing
Iran records remain compatible; the future separate variant is not introduced here.

## D-281 automatic device-local copies

Owner-approved daily/weekly/off automatic snapshots contain the existing complete financial,
attachment and allowlisted portable-preference package in a device-key encrypted VBS1 envelope.
They remain in private `backups-safety` storage under existing protected-platform OS backup/transfer
exclusions. A separate independently wrapped key protects each copy; complete native recovery and
reference-aware profile retirement include it. Automatic/pre-restore cohorts each keep ten per
profile by default, without deleting cross-referenced keys. Device-only interval, success timestamps
and generic transient failure status are not portable security credentials or imported preferences.

No password is stored; no readable automatic file, sharing, cloud request, SDK or permission is added.
Existing explicit portable exports retain optional password encryption and readable-file disclosure.
Restoration still needs original preview, confirmation and authentication. Device/key loss requires
a separate portable Zanance export; local automatic copies do not promise recovery on a new device.
Physical-device/iOS/OEM/Store and hosted acceptance remain distinct from local evidence.

### Unpublished personal-sync attachment review

Exact authenticated service attachment versions may be privately staged in the opted-in profile's
encrypted synchronization metadata before a conflict is decided. This metadata remains outside
portable financial exports. Local/service preview verifies the complete content length/digest and
current reviewed file/profile authority. It uses bounded transient memory images, no external viewer,
no OCR or financial writes, and no new permission or network SDK. The existing Android system PDF
renderer briefly uses its private cache copy and removes it on normal completion/failure; abrupt
process termination and physical/native-memory acceptance remain separate. Closing/leaving clears
owned preview byte buffers. Preview is optional, including for valid locked/unsupported documents;
explicit complete-file choices remain bound to their exact versions.

### Unpublished personal-sync staging maintenance

The local service enforces owner-wide technical staging/declaration bounds without activating
commercial limits. Explicit abandonment and revoked/expired device authority remove only
unpublished proposal bodies; committed financial history, receipts and nonce tombstones remain.
Authority is rechecked under the original owner lock, including concurrent session-row renewal.
Private ciphertext files older than one day are collected only when they have no committed
manifest, under a storage-publication gate shared across workers. Collection never decrypts or
reads attachment contents and skips fresh, redirected, unknown-name or manifested files. No active
staging or committed history age policy, new recipient, permission or processor is introduced.

### D-293 - Isolated personal-service hosting

The optional explicitly connected personal service remains server-readable. Production origin uses normal system
TLS trust; local peers use explicit pins. Internal software CA/client-role machine keys and persistent Data Protection
key ring/private files stay in isolated Docker volumes. Complete private snapshots remain root-only Docker volumes,
not uploaded artifacts. Operator TLS renewal retains historical protection keys; qualification and live authorities
cannot be reclassified. No analytics/network SDK or new app permission is added. Personal-sync INTERNET is unchanged;
no automatic opt-in, real mail sender, public routing or production privacy acceptance is implied.

### D-294 in progress - Household and selected-source boundary

The unfinished local household implementation separates opaque E2EE financial payloads from service-visible
identity/role/device public keys, member consent, timing, ciphertext sizes and serial publication metadata.
The personal-service protocol remains server-readable; its protection is not relabeled as household E2EE.
Original device/root/epoch keys and source-copy consent/receipts are inside protected profile storage and scrubbed
from portable exports. No final household app connection, privacy submission or release acceptance is declared.

Explicit source selection reads only chosen personal accounts, optional related history/category ancestry and
separately opted-in files on the device. Its immutable review owns temporary memory, closes source readers before
UI/creation and rechecks source content/choices. Source financial data is unchanged. Copies have independent ids
and exclude personal preferences, other-account data, security, queues, plans/rules/filters and private counterpart
names/ids/balances/currencies. Only selected-side exact transfer units remain through neutral excluded endpoints.
Selected free text and explicitly included files are shared content, not automatically redacted. A device-only
pending receipt cannot silently become an empty shared ledger after interruption. These data-path checks are
independent native Windows evidence; main household UI/profile, Android/AOT/iOS and whole-batch acceptance remain.

Current D-294 private binding metadata also records actual authenticated host observation/expiry/retention,
rotation readiness and remote-scope refusal. Those device-only fields remain inside the existing scrubbed household
binding table, not portable preferences. They grant no membership or paid entitlement alone. Scoped authenticated
404/403/410 refuses further local financial writes while retaining the user's exact pending ciphertext and finance.
Device-only preferences remain separate from guarded financial Settings. Native independent accepted-role/grace
checks do not establish main household UI, mobile, recovery, publication or complete privacy acceptance.

D-294 original app setup retains a private invitation capability and exact recipient claim only in two fixed,
bounded device-protected household intent slots. The owner-only pending list exposes signed public consent/device
proofs and identity ids, never the capability or financial content. The app requires an independently compared
complete root/device fingerprint, clears visible invitation/password on page retirement, and never changes a
personal profile into a household implicitly. The actual binding kind selects manual/foreground encrypted financial
HTTP; personal readable transport is not a fallback. Six-language disclosures are implemented; current native app
rendering, mobile and complete household/privacy/release acceptance remain unfinished.

D-294 actual cost facts add encrypted financial contact names/optional historical identity labels, actual costs, exact shares and private repayments. They are protected local financial rows and household E2EE ciphertext only; financial labels never grant membership. Readable personal sync refuses these tables/history. Portable user snapshots retain the financial facts while original local authority/key/session/queue scrubbing remains intact. No new network SDK, analytics, permission or password storage is introduced. Corrected18-case Windows layout and actual protected writer/roundtrip evidence do not establish Android, physical-device, recovery or whole-household privacy acceptance.

D-294 recovery continuation: an optional off-device password-encrypted kit is distinct from identity authentication,
local database recovery and portable financial backups. The service receives only public enrollment/consent and
standard recipient-encrypted root/epoch frames; private import/preparation bytes remain in protected local slots
and are scrubbed from portable financial snapshots. Passwords are transient and never remembered. Native document
Save/Pick uses explicit per-file access, with no new storage permission, readable cache or retained document URI.
Device/root recovery, active-kit append and kit retirement pass the retained actual Windows/HTTPS/PostgreSQL
cases described in11-household-delivery.md; this does not establish final Android/iOS/phone privacy acceptance.
Revocation prevents future keys/access, not recall of previously decrypted/exported/downloaded content.

D-294 source/page continuation: an explicit selected personal snapshot may remain in process memory only for its
exact destination/peer/actual identity; source keys and sessions are not included. History/files are separate
opt-ins, and chosen free text is disclosed as shared content. No source/path/queue/device authority enters portable
preferences. Cancelling a memory handoff never deletes money or durable pending intent. A native picker may
retain only bounded encrypted kit bytes/public Save receipt across page retirement, never a password, clear kit,
document URI or prior foreground grant. Actual Android adapter Save/Pick/Cancel passed independently; main lock/
picker composition and complete mobile acceptance remain open. The common household conflict review refuses
readable personal session/publication actions; native two-client cost-group decisions used only encrypted HTTP.


D-294 continuation: the owner-only pending-device endpoint exposes bounded public device keys and self-possession proof only for accepted member identities. No new SDK, OS permission, private financial payload or device private key is returned. Retired-root unaccepted invitations expire server-side; local Finish erases only the reviewed protected setup intent, without claiming remote deletion of copied content. Device revocation cannot recall previously downloaded history.


D-294 device access packages contain public owner keys and a signed actual member identity/membership checkpoint/expiry only, without invitation capability, private keys or financial data. Login and pending session reattachment grant no financial resources; later explicit independent device approval remains required. Private setup keys/proofs stay in the original protected profile. No SDK, permissions or portable preference fields were added. Initial receive preserves defaults as local until verified incoming financial apply; it cannot silently publish them.


D-294 ownership metadata contains bounded public identity/device/checkpoint/expiry/signature proofs and standard
opaque recipient/root envelopes only. Target-device consent is independent of a newly chosen signing root. Private
candidates remain in protected local slots and are never portable preferences. A departed participant can observe
only its own original public terminal receipt with actual identity/session/private-device proof; that receipt returns
no key envelopes or financial rows. Previously downloaded history cannot be recalled. No new SDK, OS permission,
external provider, plaintext financial HTTP or automatic readable export was added. Current native evidence is
scoped; main mobile, complete lifecycle and release acceptance remain open.

D-294 lifecycle metadata adds bounded private room activity (actual actor/device/operation, service UTC, membership/publication checkpoints and optional public signatures; no recipient grant or decrypted finance). Exact owner deletion and expiry purge room ciphertext, grants, recovery, roster, invitations and audit. A minimal room tombstone/owner relation remains in PostgreSQL and an immutable opaque actor/request commitment remains in the service-private deletion directory. This is not identity anonymization/erasure or recall of downloaded files. The original deletion declaration/outcome is device-local protected metadata, scrubbed with other household metadata from portable financial exports. A latest private marker set must accompany old database restoration; restoring only an old complete service snapshot remains an open operational integration gate. No external SDK, permission or readable financial network payload was added.
The latest private-marker merge and complete historical service restore have now been executed successfully on separate fictional Docker stacks. Snapshot manifests additionally retain only the nonsecret local-household opt-in (never public activation). An actual current owner/device can review opaque deletion checkpoints during retained time and reattach its same private device to a fresh authenticated session for status/privacy actions. Neither endpoint serves financial rows, epoch grants or new financial write authority. The exact declaration still needs its existing independently trusted private owner root and actual current server ownership checks. Main/native destructive-dialog and mobile, identity erasure and whole D-294 remain open.
The household retirement worker reports only an aggregate count of unavailable private entries. It never logs
room/identity ids, file paths, protected bytes or exception messages. Unknown/corrupt markers remain untouched;
unrelated room retirement continues under separate transactions. Offline restoration still refuses unknown
source entries, and unreadable/redirected roots do not become an empty deletion ledger (D-294 in progress).

D-294 bounded-history continuation: derived household row positions (original table/key/version and ciphertext
position) remain solely inside the existing device-protected local database. They introduce no service plaintext,
permission, SDK or credential. All11 private household tables, including this index, are scrubbed from the portable
memory image; original keys, history and financial data remain untouched. Scoped native export acceptance is in
11-household-delivery.md; whole D-294/mobile/Release acceptance remains open.


## D-296 accountant data packet

Explicit Germany/year/account selection exports local financial rows and necessary account/category identities
to a readable ZIP only after review and confirmation. Both transfer account identities are disclosed together.
Payees/notes/reimbursement names and exact stored attachment copies are separate opt-ins, initially off. No
security, preferences, credentials or private service authority is included. Production private cache is bounded
and failed unpublished files are removed; offered files expire after24 hours on the next export. Native
recipients control their own copies. This is independent of encrypted automatic backup and optional-password
portable backup. No new SDK, permission, background upload or financial write is introduced. See the D-296
delivery record for source, coherent-read/stale-review guards and actual local/native evidence.


## D-297 explicit local bank CSV source

The user picks an original local file with explicit UTF-8/Windows-1252. No login/network/provider/new permission.
In-memory reviewed data includes source identity, exact dates/currencies/amounts/payee/remittance; only the
source ending is shown and source IBAN field is not copied to the ledger. Original free text can still contain
identifiers. Imported entries and Undo metadata follow existing profile-local encrypted storage/backup rules.
Original files are read only. No automatic payment, upload, raw-file retention or device-security export.

## D-298 Optional aggregate analysis

Before explicit consent, account choices and coherent calculations stay local. Only selected currency totals,
exact dates, account counts, confirmation scope, language and unreviewed counts enter the disclosed packet;
no names, transaction ids, payees, notes, documents, device security or sync ciphertext. Shared sources are
refused. Manual sharing deliberately offers readable exact JSON/prompt to a user-chosen system recipient;
its retention/forwarding is outside Zanance and it is not a built-in model call.

An explicitly enabled local service uses the existing pinned authenticated identity, protected original payload/
result and append-only purchased/gifted reservation history. No analytics/network SDK, API-key input, new
permission, auto-upload or financial write. Original private request state uses platform SecureStorage and a
public loss marker outside all portable preferences/financial sync. Marker/private loss fails closed. No raw
financial payload is sent for metadata offers or original closure. Live provider/retention/legal/cost approval,
real traffic, physical devices and iOS remain distinct gates; the default provider is unconfigured.

## Optional reference sources (D-299)

An explicit authenticated lookup sends only the approved public provider/revision/symbol/date. No account name,
holding id/quantity, balance, ledger row, private asset name, arbitrary URL or credential enters its DTO. There is
no automatic lookup, new SDK/permission, live provider selection or API-key input. Original quote cache is bounded
and device-private in platform SecureStorage, outside portable preferences. Accepted source/conversion history
is profile data and may contain the explicitly selected local asset definition. It follows the existing portable
backup protection choice and actual personal-profile authority; shared copies are inside the existing signed
encrypted membership/publication chain. Host ciphertext cannot grant financial authority. Do not imply erasure
of previously downloaded history after membership removal. Local-only service qualification is separate from
live provider/retention/redistribution/public/physical/iOS acceptance; see16-market-reference-delivery.md.

### D-310 original private purchase metadata display

The plan screen displays already verified device/identity purchase lifecycle and validity dates from the
existing private signed snapshots. No new collection, HTTP, storage, export or permission is introduced.
Raw purchase references, receipts, identity hashes and provider diagnostics remain absent from labels.
Fictitious native review metadata is independent from owner data and real store verification.

### D-312 safety candidate presentation

Only current profile/installation nonsecret envelope-header candidates and local file date/size appear in
Backup. Discovery opens no key or plaintext package. Selection retains complete authentication and access/
replacement checks. Device-only rows do not share/export files; no permission, SDK, network or new data class.


## D-313 optional hosted edge processor (not active)

The approved Cloudflare Tunnel path terminates public TLS at Cloudflare; visitor IP/routing metadata and identity request plaintext can be processed there. Existing application-layer sync/share ciphertext is separate from TLS; disclosed analysis has its own consent boundary. No mobile SDK, permission, local schema or automatic upload is added. The connector mounts only the owner credential file and original public CA; bounded error logs never enable request/header debugging. No live tunnel, account, DNS or real mail is configured. Draft processor/retention and exact release-build declarations need review before publication; historical offline/cloud-backup-only answers do not apply to optional identity/sync builds.


### D-314 independent physical qualification only

The separate offline signed package records model/API/architecture/page size, timings/counts and fictitious validation outcomes in bounded local public reports. No hardware identifier, key, owner financial record, new app collection/SDK/permission or network upload is introduced. Main package, security flags and OS settings remain unchanged. Retained independent ciphertext/keys permit same-data comparisons; failed write intents are preserved for explicit inspection.


### D-315 bounded local read diagnostics

Independent fictitious same-data diagnostics add only local derived counts, timings, packet digests and preservation outcomes. No owner records, hardware identifier, key, new app telemetry/SDK/permission or upload is introduced. Main unbounded reads retain every existing mapped field and original security boundaries; no new storage schema or backup content.


### D-328 local receiver qualification only

Two original fictitious readable accountant ZIPs are explicitly sent by the native chooser to an independent
offline test receiver, copied into only its private test storage and inspected locally. The receiver removes
inherited AppSecret items, has no network permission/backup and is uninstalled after the check. Main private
files/keys are not read. No production data collection, SDK, permission, forwarding or retention change;
main offered-cache expiry and recipient-copy disclosure remain unchanged. Local fictitious artifacts are not uploaded.


### D-329 original fictitious report PDF qualification

The independent offline test receiver adds only report PDF MIME/owned-name validation and bounded local
copies under native read grants; it is removed afterward. Main export disclosure, permissions, storage,
SDKs and network behavior do not change. One initial stale chooser action opened an unrelated app and
no temporary fictitious-file grant absence is claimed; subsequent explicit local selection is verified.
No real financial data or keys are inspected/uploaded. The original report footer and main privacy policy
are unchanged; local fictitious PDF/raster evidence stays outside Git.


### D-330 original fictitious CSV qualification only

The independent offline receiver additionally accepts only the bounded original ledger CSV name/header and
native read grant. Its fictitious local output and one hash-matched public picker input support an explicit
producer/consumer test; the input, fresh profile and receiver are removed afterward. Main collection/SDKs,
permissions and export defaults do not change. CSV notes inclusion was on, with no notes in this fixture;
nonempty-note redaction is not claimed. No owner data/keys are read or uploaded.

## D-333 device-only cold reminder access

A notification-only action prepares the existing device-bound protected store on a worker before its
financial read; it creates no window or unlock/navigation event. Original detail consent, profile/source
checks and existing device-local snooze/native queue remain. There is no new production SDK, permission,
portable field or network flow. The independent offline qualification provider exposes only synthetic
state/counts, is absent from main and is removed with its fixture package. Startup ANR, physical-device,
iOS and interrupted-action acceptance remain separate. See [scope](quality/cold-protected-reminder-startup.md).

## D-389 approved public FX source

Only an explicit reference Fetch can make the service request the fixed public ECB daily XML. No
user identity, account/holding name, amount, quantity, arbitrary URL, cookie or credential goes to ECB.
Source metadata/cache/Home/history reads do not dispatch upstream requests. Original public-byte hash,
date, legs and derivation join the existing device-private cache and consented portable/sync source
history. No new SDK, financial schema, Android permission or automatic financial update is introduced.
