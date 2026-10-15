# Zanance — Privacy Matrix Starter Template

**Document version:** 1.1  
**Date:** 2026-09-25 (v1.0); revised 2026-09-26 (v1.1); current alignment refreshed 2026-10-15 (D-287)
**App:** `pro.vafadar.zanance`  
**Product reference:** `Zanance-Product-Specification.md`, particularly Sections 17–20 and 22  
**File status:** Review template with statuses verified against the repository; not a submission-ready Data Safety declaration. The maintained app profile is `src/Apps/Zanance/docs/06-privacy-matrix.md` in the repository.

> The original v1.1 review was dated 2026-09-26. Subsequent decisions and current rows are aligned with the maintained app privacy profile (D-287; historical D-88 evidence retained); targeted Windows/emulator evidence is recorded per scenario in the acceptance plan. Physical-device, real-provider and iOS acceptance remain release gates. `Not included` is scoped to the named feature/build and must be re-checked after dependency changes.

## 1. Allowed Statuses

| Status | Meaning |
|---|---|
| Implemented — verified | Actually implemented in a specified version and reviewed with evidence. |
| Implemented — unverified | Present in code or reported, but end-to-end behavior has not been verified. |
| Planned | A future capability; not yet part of shipped behavior. |
| Not included | Outside this version's scope; verify the absence of related code/SDKs and behavior. |
| Unknown — needs verification | Insufficient information to conclude; resolve before release. |

## 2. Portfolio Summary

| App | Primary online account | Local ledger | Ads / Analytics / AI | Pro | Privacy Policy | Data Safety |
|---|---|---|---|---|---|---|
| Zanance / pro.vafadar.zanance | No mandatory app account; optional configured Google/Microsoft backup connection (D-35/D-50). | SQLCipher financial databases on Android/Windows; iOS integration pending; implemented ledger and local profiles. | No application ads/analytics/AI; native ML Kit diagnostics possible in Cloud builds (SEC-09). | Free/Plus/Pro design (D-61); no billing or quota enforcement. | Draft exists; must be published at a stable URL before release. | Must be completed from the release build. |

## 3. Data Flows to Review

| ID | Capability/flow | Potential data | Intended destination in the design | Current status (evidence scoped per row) | Required evidence |
|---|---|---|---|---|---|
| DF-01 | Accounts and transactions | Amount, currency, date, account name, category, title, payee, notes; quick templates (title, payee, optional amount); saved list filters; who pays back a reimbursable expense; tags; categorization rules (text and category); interest rate and installment of loans; lender or borrower of a loan. | Local app database. | Implemented — verified (automated tests). | Storage location (app-private), OS backup behavior (DF-16/17), device check. "Delete all data on this device" (Settings, confirmed twice) empties the database; shared backup files, exports and OS backups are not affected, and the app says so. |
| DF-02 | Schedules and budgets | Due dates, amounts, income/expense plans, limits, exchange rates, savings goals and the money set aside for them. | Local. | Implemented — verified (automated tests). | Local finance processing; offline Release has no INTERNET permission. Configured Cloud builds contain the separate authentication/storage and native SDK paths documented in the SDK review. |
| DF-03 | Local backup | Complete ledger package, finance settings and content counts; Optional AES-256-GCM with a user password (D-62). | Kept on the device (last 10) and shared by the user to a destination of their choice. | Implemented — verified (library and fresh-install integration tests); device check pending. | Temporary files, restore on a new device. |
| DF-04 | Google Drive backup | Package with optional password encryption and connection metadata (D-62). | User-selected cloud storage. | Implemented platform storage/sign-in; offered only with the configured provider client. Real end-to-end acceptance pending (D-35/D-50/D-67). | Authorization scopes, released signing certificate/client registration, upload/download/restore, deletion and account switching (AT-59). |
| DF-05 | OneDrive backup | Package with optional password encryption and connection metadata (D-62). | User-selected cloud storage. | Implemented platform storage/MSAL; offered only with configured Microsoft client. Real end-to-end acceptance pending (D-35/D-50/D-67). | The same checks, performed independently for Microsoft, including Entra platform configuration. |
| DF-06 | Export / Share | Entries of a chosen period (CSV; notes and payees optional) or the report of a period (PDF with totals, categories, accounts, plans and tags). | User-selected destination app via the share sheet. | Implemented — verified. | Unencrypted-file warning, formula neutralisation, temporary file in the app cache. |
| DF-07 | Import | Selected CSV file and its contents. | Local processing and storage. | Implemented — verified. | No upload, preview, atomic batch with undo. |
| DF-08 | Reminders | Occurrence ID and generic text; name and amount only if opted in. Several reminders at the same time become one summary (plan names only if opted in). A snoozed reminder keeps its text and link in local preferences until it fires or its occurrence closes. Contract reminders (last day to cancel, review date) follow the same privacy rule. | Device notification service (local). | Implemented — verified (unit); device check pending. | Lock-screen text, permission on demand, rebuild after restore. Windows: no notifications. |
| DF-09 | App access and screen protection | Profile device-lock flag; device-wide salted PIN verifier, failed-attempt count and retry deadline; screenshot preference | Platform SecureStorage for verifier/attempts, device preferences for screenshot choice; no PIN state in portable backups | Implemented - verified (20 PIN tests, builds and Windows UI); device/iOS acceptance pending | D-63/D-266: PIN is a separate UI gate; device-only unlock also requires Success, while at-rest encryption is independent; current PIN for change/removal; successful device authentication plus confirmation for recovery; read/write failure never unlocks. Android foreground screenshot block optional and on by default, recents always protected; iOS cover remains. Android secure-storage ciphertext excluded from OS backup/transfer; iOS keychain persistence follows OS policy and uninstall need not remove it. |
| DF-10 | Diagnostics / Crash reporting | Exception text in the debug output of Debug builds only. | No external service. | Implemented — unverified. | Confirm the release build contains no logging provider that sends data. |
| DF-11 | Ads / Tracking | None. | No destination. | Not included (verified by package list). | Re-check with every dependency change. |
| DF-12 | Billing | None in Phase 1. | Authorized store service, in the future. | Not included. | Before any release with purchases: purchase flow, validation, retention. |
| DF-13 | AI | None. | — | Not included. | — |
| DF-14 | Sync / Household | None. | — | Not included. | — |
| DF-15 | Online exchange rates / Bank Sync | None; rates are entered manually. | — | Not included. | Offline Release has no INTERNET permission; Cloud SDK/network review remains separate from the absent rate/bank feature. |
| DF-16 | Android OS backup and transfer | Financial databases, key envelopes, internal copies and sensitive preferences/staging | Excluded from Android cloud backup and device transfer | Implemented - locally verified (D-263); OEM/device acceptance pending | allowBackup=false plus complete backup/data-extraction exclusions. New devices restore from a portable Zanance export; do not promise OEM behavior from manifest checks alone. |
| DF-17 | iOS device / iCloud backup | App data in the current unqualified iOS scaffold | OS-default backup behavior remains unverified | Final exclusion planned under approved OD-10; iOS protected runtime/build acceptance open | Do not claim current iOS exclusion or encrypted main storage. Couple final policy to iOS native adoption and verify before that release. |
| DF-18 | Preferences | Profile finance/layout choices and device-wide language/calendar/region/theme/digit choices; no inferred location | Profile database and device-local preference/security stores according to each choice scope | Implemented - locally verified; device/iOS acceptance open | Android excludes local stores from OS backup/transfer. Only approved portable display/profile preferences enter Zanance packages; device security, credentials and theme do not become portable. iOS final exclusion remains pending. |
| DF-20 | Attachments | User-chosen receipt photos/PDFs, names/types/content; compact metadata-free stored photos | Protected financial DB on Android/Windows; optional-password portable backup; plaintext temporary external-viewer copies; not CSV/PDF report exports | Implemented - locally verified; phone/iOS acceptance open | Android OS backup/transfer excludes this storage. iOS policy is not yet accepted. Keep original picker/contextual camera behavior, Undo ownership and startup/deletion cleanup; encryption cannot protect copies already shared with another app. |
| DF-19 | Syncfusion license validation | License key compiled into the app (build secret, never in the repository). | Local check. | Implemented — offline validation verified by an automated test. | Offline license validation and disabled Syncfusion telemetry are reviewed separately from other native SDKs; offline Release has no INTERNET permission. See the build-specific SDK review for Cloud. |

## 4. Per-Flow Detail Form

Complete the following information for each row with actual behavior. This list defines the review requirements; the final storage format may follow the repository's documentation conventions.

| Field | Required value |
|---|---|
| Flow ID and Feature | Stable identifier and capability name. |
| App / Version / Build | The app and version actually reviewed. |
| Data fields | Exact field names and data categories, including possible metadata and identifiers. |
| Source | User input, device, file, SDK, or service. |
| Processing location | Device only, our service, provider, destination app, or a combination. |
| Recipient / Provider | The actual recipient's name and role. |
| Purpose | A specific reason for processing, not a vague phrase such as “Improving services.” |
| Optional / Required | Whether optional or necessary for the capability, and the effect of refusal. |
| Authentication | No login, backup-service connection, app account, or another official path. |
| Permissions | Only actual permissions in the shipped package. |
| SDK / Dependency | Name, version, and effective configuration. |
| Retention | Retention of primary data, metadata, logs, and copies/versions. |
| Deletion | Local deletion, cloud-copy deletion, access revocation, or account deletion, including limitations. |
| Protection / Read access | Protection in transit/at rest and who can actually read the content. |
| User disclosure | Information text/screen and consent where required. |
| Data Safety mapping | Mapping against Google's current definitions, with reasons for each exception. |
| Evidence | Relevant file/configuration, tests performed, and results; not merely the existence of an interface. |
| Reviewer / Review date | Reviewer and date. |
| Release gate | Pass, Blocked, or Not applicable, with a reason. |

The per-flow details for the current version are maintained in `src/Apps/Zanance/docs/06-privacy-matrix.md`.

## 5. Maintenance Rules

Any change to an SDK, permission, cloud service, export behavior, authentication, billing, or AI must trigger a review of the affected rows. A disabled UI option does not prove an SDK transmits no data. Record future plans separately from the current version's behavior.

A financial account in the ledger differs from an app login account. Connecting Drive/OneDrive storage does not automatically create a Zanance cloud account; review the actual identity data and connection scope.

An encrypted backup in the user's own storage does not automatically exempt identity collection, metadata, or SDK behavior from Data Safety assessment. Evaluate every flow against current definitions; do not derive the declaration from marketing claims.

**Review sources:** S01 and S02 in Section 30 of the main specification. This file deliberately does not fabricate a legal conclusion or a submission-ready status.

D-64: original photo bytes are transient during recognition only; a separate upright copy is bounded to 3200 px,
while only the 1600 px metadata-free JPEG is attached and backed up. Source rows/candidates/currency evidence are
not additional database fields or exports. No new permission, SDK or network path. A stored-image reread cannot
recover lost detail. Recognition/rendering errors and cancellation do not post ledger entries.

D-65 uses existing account counterparty/reference balance/interest/installment and Schedule fields only. A reminder
draft and its preview are transient until explicit Save; opening it records no ledger entry. Portable backups retain
their existing schema. No new data category, SDK, network path or permission; notification permission is requested
through the existing contextual reminder flow. Estimates never become principal payments automatically.

### D-67 display and discovery delta

Portable language/calendar/region/formatting-culture/digit/week-start preferences enter the optionally protected
backup package via an explicit allowlist. Tokens, PIN verifiers, passwords and device security preferences do not.
Connected-provider metadata listing now runs on opening the backup page; connection remains explicit and there is
no automatic upload, broader scope, new SDK or location access.


SEC-01 / D-68 is a separate fictitious-only experiment and architecture proposal. No real-data or production
permission/SDK changes; local database encryption and financial-data OS-backup protection remain unimplemented.


D-69 / SEC-09: the current SDK/build review is [recorded here](../../../../../docs/privacy/zanance-sdk-review.md).
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

D-89 changes tag suggestion presentation only: raw existing tags remain separate from direction-safe captions,
with no new stored field, export, permission, SDK or recipient. Explicit Save and existing backup handling remain.

D-90 names the current transaction detail action and keeps in-memory values when hidden. No new stored field,
portable source, export, recipient, permission or SDK; financial persistence still requires explicit Save.

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
database/page and are absent from Release. [Evidence](../quality/active-item-chooser.md).

## D-143 goal-choice privacy boundary

Existing profile-local ResourceSelections can contain explicitly chosen original goal ids under verified financial
scope. No entitlement, membership, credential or device-security value is stored. Goal metadata projects existing
columns; no schema/entity, SDK, permission, network or telemetry change. Choice/lifecycle writes never write ledger
money; retained history remains. Registration stays inactive. Native checks use the exact owned fictitious QA03
file and restore every original byte/sidecar. [Evidence](../quality/goal-resource-choices.md).

## D-144 goal-form privacy boundary

The existing Settings form reads original local goal identity/measurement/currency/state and explicitly saves only
the D-143 scoped choice row. Unsaved choices stay in memory. No schema, credential, device-security, SDK, permission,
network or telemetry change. No financial write from selection, reload, language, Help or Back. Registration remains
inactive. Limited-layout input stays only in an independent Debug fixture/page, never Release DI. [Evidence](../quality/goal-chooser.md).

## D-145 scoped goal-reminder privacy boundary

Existing native pending returning data now carries bounded original goal ids/rule dates and a SHA-256 opaque
actual-file identity, never the file path or an authorization token. Existing generic/detail opt-in, permission,
09:00 device-local scheduling and capped shared queue remain. Goal/rule/allocation validation stamps stay only in
memory; no schema, portable preference, credential, security, SDK, network or telemetry addition. Taps recheck the
current actual file after unlock and never switch profiles or post money. Current enforcement remains inactive.

[Evidence](../quality/goal-work-reminders.md).

## D-146 goal-view and draft boundary

Existing goal/history/correction data remain profile-local. Complete goal/rule/allocation metadata and account/holding
unit/availability stamps are transient in-memory guards; no schema or portable/security preference is added. Dirty drafts
retain their original numeric culture/currency only in memory. Native-effect test boundaries introduce no SDK, permission,
telemetry, credential or network flow. An explicit earmark release writes one negative allocation, never ledger money or
an account/holding value. Current commercial registration stays inactive.

[Evidence](../quality/goal-work-views.md).

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
See [evidence](../quality/budget-definition-choice.md).

## Reviewed budget chooser (D-149)

Original reference captions are captured in memory to reject stale reviews; no account balance or new financial
data is written. Explicit definition choices use the existing local scoped row/revision. No SDK, permission,
external transmission, schema or security/purchase grant changes. See [evidence](../quality/budget-definition-chooser.md).

## Guarded budget alerts (D-150)

Existing financial/privacy inputs are captured temporarily in memory and rechecked locally before native work.
Threshold caches remain device-local, without passwords or paid authority. Generic/details choice remains
explicit. No new SDK, permission, external transmission, schema/model or portable grants. Existing notification
links remain unscoped for Budget and require a later file-bound routing slice. See [evidence](../quality/budget-alert-work.md).

## D-151 - Device-local budget routing identity

An opaque actual-file SHA-256 digest and original budget Guid enter only device-local notification returning-data. No paths, credentials, financial values, portable preference, network SDK, permission or schema changes. Resolve after unlock and during page loading; another profile never inherits the destination. See [evidence](../quality/budget-reminder-routing.md).

## Original budget editor review (D-152)

The reviewed budget form retains complete original references and pinned input cultures in memory. No new data class, permission, SDK, network, credential or backup/export field is introduced. Explicit Save rechecks the actual SQLite writer; opening and reload do not post money or create Settings. Original row/file identity does not grant commercial rights.

D-153: reviewed original budget view actions add no data category, network/SDK/permission or portable grant.
Complete original references stay local; paired Copy/Delete never mutate financial ledger rows.


D-154: reviewed local quick templates/saved queries add no data category, schema, network, SDK or permission.
Full packets exist only for in-memory review and existing explicit configuration/choice writes. No portable
grant or credentials are introduced; template draft/query viewing never writes the ledger.

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

Non-secret profile/installation/generation context binds local DEKs; device keys/installation
security metadata are excluded from portable preferences and logs. CurrentUser DPAPI primitive
and fictitious independent proof only, no live storage or provider change. No new network,
permission or financial schema. Owned key-copy clearing is best effort, not whole-process erasure.
OD-04/OD-10 approved; optional recovery UX and final Android/iOS OS exclusion remain implementation.

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

D-202: private SQLCipher restore capsule contains allowlisted previous/restored display preferences
only, retained with candidate key/rollback for startup replay. No credentials/PIN or new permission/
network SDK. Capsule is not a portable backup source; final main retention/OS policy remains open.

D-206 local identity bootstrap records only non-secret installation/legacy/key-profile/generation
GUIDs in private protocol storage. No keys/labels/financial facts or portable source and no SDK,
permission or network change. Main/final OS-policy integration remains open.

D-208 private reference metadata contains installation/profile/parent/target GUIDs, sequence and
selected-reference flag only, not keys/names/finance/paths or portable preferences. No SDK/network/
permission change; main final OS-policy and referenced key/file pruning are still open.

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
only captured authorized safety/profile objects. [Evidence](../quality/android-protected-storage.md).

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
