# Zanance — Privacy Matrix Starter Template

**Document version:** 1.1  
**Date:** 2026-09-25 (v1.0); revised 2026-09-26 (v1.1); current alignment refreshed 2026-10-09 (D-88)
**App:** `pro.vafadar.zanance`  
**Product reference:** `Zanance-Product-Specification.md`, particularly Sections 17–20 and 22  
**File status:** Review template with statuses verified against the repository; not a submission-ready Data Safety declaration. The maintained app profile is `src/Apps/Zanance/docs/06-privacy-matrix.md` in the repository.

> The original v1.1 review was dated 2026-09-26. Subsequent decisions and current rows are aligned with the maintained app privacy profile (D-88); targeted Windows/emulator evidence is recorded per scenario in the acceptance plan. Physical-device, real-provider and iOS acceptance remain release gates. `Not included` is scoped to the named feature/build and must be re-checked after dependency changes.

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
| Zanance / pro.vafadar.zanance | No mandatory app account; optional configured Google/Microsoft backup connection (D-35/D-50). | Plaintext SQLite in app-private storage; implemented ledger and local profiles. | No application ads/analytics/AI; native ML Kit diagnostics possible in Cloud builds (SEC-09). | Free/Plus/Pro design (D-61); no billing or quota enforcement. | Draft exists; must be published at a stable URL before release. | Must be completed from the release build. |

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
| DF-09 | App access and screen protection | Profile device-lock flag; device-wide salted PIN verifier, failed-attempt count and retry deadline; screenshot preference | Platform SecureStorage for verifier/attempts, device preferences for screenshot choice; no PIN state in portable backups | Implemented - verified (20 PIN tests, builds and Windows UI); device/iOS acceptance pending | D-63: PIN masks UI only, not database encryption; current PIN for change/removal; successful device authentication plus confirmation for recovery; read/write failure never unlocks. Android foreground screenshot block optional and on by default, recents always protected; iOS cover remains. Android secure-storage ciphertext excluded from OS backup/transfer; iOS keychain persistence follows OS policy and uninstall need not remove it. |
| DF-10 | Diagnostics / Crash reporting | Exception text in the debug output of Debug builds only. | No external service. | Implemented — unverified. | Confirm the release build contains no logging provider that sends data. |
| DF-11 | Ads / Tracking | None. | No destination. | Not included (verified by package list). | Re-check with every dependency change. |
| DF-12 | Billing | None in Phase 1. | Authorized store service, in the future. | Not included. | Before any release with purchases: purchase flow, validation, retention. |
| DF-13 | AI | None. | — | Not included. | — |
| DF-14 | Sync / Household | None. | — | Not included. | — |
| DF-15 | Online exchange rates / Bank Sync | None; rates are entered manually. | — | Not included. | Offline Release has no INTERNET permission; Cloud SDK/network review remains separate from the absent rate/bank feature. |
| DF-16 | Android Auto Backup | App database and preferences. | The user's Google account backup. | Implemented — unverified (enabled by owner decision). | Disclose in the Privacy Policy and Data Safety; test restore on a new device. |
| DF-17 | iOS device / iCloud backup | App data. | The user's iCloud or computer backup. | Implemented — unverified (operating-system default). | Disclose; verify with the iOS release. |
| DF-18 | Preferences | Language, calendar, optional region, first day of the week, theme, currency display units, Home layout, alert levels, dismissed Home guidance. | Local preferences. | Implemented — verified. | Allowlisted portable regional/display preferences also enter new app backup packages (D-67); credentials, PIN state and device security do not. Other preferences follow their recorded profile/device scope. |
| DF-20 | Attachments | Receipt photos and PDF files the user picks for an entry; on Android, iOS and Windows photos are re-encoded as JPEG without metadata; a photo that cannot be re-encoded is not stored. | App database, optionally password-protected portable backup (D-62) and OS backups; never in CSV/PDF exports; opened with a chosen app through a temporary copy deleted on the next start. | Implemented — verified. | System file picker without storage/photo-library permission. Phone capture (D-38): Android camera app without Zanance camera permission; iOS contextual camera permission with NSCameraUsageDescription. Deleted attachments are cleaned after the Undo window at the next start. |
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
