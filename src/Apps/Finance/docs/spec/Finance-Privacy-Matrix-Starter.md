# Finance — Privacy Matrix Starter Template

**Document version:** 1.1  
**Date:** 2026-09-25 (v1.0); revised 2026-09-26 (v1.1: statuses reviewed against the repository)  
**App:** `pro.vafadar.finance`  
**Product reference:** `Finance-Product-Specification.md`, particularly Sections 17–20 and 22  
**File status:** Review template with statuses verified against the repository; not a submission-ready Data Safety declaration. The maintained app profile is `src/Apps/Finance/docs/06-privacy-matrix.md` in the repository.

> v1.0 was prepared without inspecting the repository. v1.1 records the state verified on 2026-09-26 from the source code, the package list (`Directory.Packages.props`) and the merged Android manifest. Device checks of the release package are still pending for every flow and remain a release gate. `Not included` means the capability and its SDKs are absent from the code; it must be re-checked for every release.

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
| Finance / pro.vafadar.finance | None in Phase 1; the app has no sign-in. | SQLite in app-private storage; complete Phase 1 ledger, plans, budgets and rates. | Not included; no such SDK is referenced. | Future plan; no payments in Phase 1. | Draft exists; must be published at a stable URL before release. | Must be completed from the release build. |

## 3. Data Flows to Review

| ID | Capability/flow | Potential data | Intended destination in the design | Status (2026-09-26) | Required evidence |
|---|---|---|---|---|---|
| DF-01 | Accounts and transactions | Amount, currency, date, account name, category, title, payee, notes; quick templates (title, payee, optional amount). | Local app database. | Implemented — verified (automated tests). | Storage location (app-private), OS backup behavior (DF-16/17), device check. "Delete all data on this device" (Settings, confirmed twice) empties the database; shared backup files, exports and OS backups are not affected, and the app says so. |
| DF-02 | Schedules and budgets | Due dates, amounts, income/expense plans, limits, exchange rates. | Local. | Implemented — verified (automated tests). | No transmission: no network SDK, no INTERNET permission on Android. |
| DF-03 | Local backup | Complete ledger package, finance settings and content counts; AES-256-GCM with a user password. | Kept on the device (last 10) and shared by the user to a destination of their choice. | Implemented — verified (library and fresh-install integration tests); device check pending. | Temporary files, restore on a new device. |
| DF-04 | Google Drive backup | Encrypted package and connection metadata. | User-selected cloud storage. | Not included in the Phase 1 release: hidden until real sign-in exists. | Authorization scopes, identity data, upload/download, deletion, account switching. |
| DF-05 | OneDrive backup | Encrypted package and connection metadata. | User-selected cloud storage. | Same as DF-04. | The same checks, performed independently for Microsoft. |
| DF-06 | Export / Share | Entries of a chosen period; notes and payees optional. | User-selected destination app via the share sheet. | Implemented — verified. | Unencrypted-file warning, formula neutralisation, temporary file in the app cache. |
| DF-07 | Import | Selected CSV file and its contents. | Local processing and storage. | Implemented — verified. | No upload, preview, atomic batch with undo. |
| DF-08 | Reminders | Occurrence ID and generic text; name and amount only if opted in. Several reminders at the same time become one summary (plan names only if opted in). A snoozed reminder keeps its text and link in local preferences until it fires or its occurrence closes. | Device notification service (local). | Implemented — verified (unit); device check pending. | Lock-screen text, permission on demand, rebuild after restore. Windows: no notifications. |
| DF-09 | App lock | Lock-enabled flag; the credential stays with the operating system. | Device (biometrics or device credential). | Implemented — build verified; device check pending. | No PIN or biometric data stored; locked on leaving the app; notifications and exports respect the lock; the lock does not encrypt the database. |
| DF-10 | Diagnostics / Crash reporting | Exception text in the debug output of Debug builds only. | No external service. | Implemented — unverified. | Confirm the release build contains no logging provider that sends data. |
| DF-11 | Ads / Tracking | None. | No destination. | Not included (verified by package list). | Re-check with every dependency change. |
| DF-12 | Billing | None in Phase 1. | Authorized store service, in the future. | Not included. | Before any release with purchases: purchase flow, validation, retention. |
| DF-13 | AI | None. | — | Not included. | — |
| DF-14 | Sync / Household | None. | — | Not included. | — |
| DF-15 | Online exchange rates / Bank Sync | None; rates are entered manually. | — | Not included. | The Android manifest declares no INTERNET permission. |
| DF-16 | Android Auto Backup | App database and preferences. | The user's Google account backup. | Implemented — unverified (enabled by owner decision). | Disclose in the Privacy Policy and Data Safety; test restore on a new device. |
| DF-17 | iOS device / iCloud backup | App data. | The user's iCloud or computer backup. | Implemented — unverified (operating-system default). | Disclose; verify with the iOS release. |
| DF-18 | Preferences | Language, calendar, optional region, first day of the week, alert levels, dismissed Home guidance. | Local preferences. | Implemented — verified. | Included in OS backups, not in the app's backup package. |
| DF-19 | Syncfusion license validation | License key compiled into the app (build secret, never in the repository). | Local check. | Implemented — offline validation verified by an automated test. | Confirm no network call in the release build (none possible on Android without the INTERNET permission). |

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

The per-flow details for the current version are maintained in `src/Apps/Finance/docs/06-privacy-matrix.md`.

## 5. Maintenance Rules

Any change to an SDK, permission, cloud service, export behavior, authentication, billing, or AI must trigger a review of the affected rows. A disabled UI option does not prove an SDK transmits no data. Record future plans separately from the current version's behavior.

A financial account in the ledger differs from an app login account. Connecting Drive/OneDrive storage does not automatically create a Finance cloud account; review the actual identity data and connection scope.

An encrypted backup in the user's own storage does not automatically exempt identity collection, metadata, or SDK behavior from Data Safety assessment. Evaluate every flow against current definitions; do not derive the declaration from marketing claims.

**Review sources:** S01 and S02 in Section 30 of the main specification. This file deliberately does not fabricate a legal conclusion or a submission-ready status.
