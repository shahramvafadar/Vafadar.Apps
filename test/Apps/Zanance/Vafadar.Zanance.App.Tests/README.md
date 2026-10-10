# Zanance application flow tests

This executable test project compiles the actual platform-independent application sources with explicit platform
ports. It does not reference the MAUI project, create substitute MAUI controls, or duplicate production algorithms.
SQLite migrations and stores, translations, localization, PIN verification and command implementations are real.
Every database, preference store, clock, verifier and dialog response is fictitious and isolated.

Run `dotnet test --project test/Apps/Zanance/Vafadar.Zanance.App.Tests -p:ContinuousIntegrationBuild=true` from the
repository root, or use `Vafadar.Tests.slnf`. The assembly disables parallelization because localization/digit display
changes process-wide culture. Each fixture disposes providers, clears SQLite pools and resets digit display.

| Flow | Cases | Evidence |
|---|---:|---|
| Onboarding | 6 | Draft/back/next, restore without account creation, validation, durable account identity on retry, regional/language independence |
| Profiles | 7 | Exclusive database switching, authentication refusal and migration rollback, catalogue/path validation, owned deletion, cancelled and duplicate dialog flows |
| Bulk transactions | 11 | Known/visible selection, snapshot copies, validation rejection, category/tag scope, transfer invariants, pending-dialog command exclusion, delete and timed Undo |
| App access | 13 | Secure startup gate, queued links, missing/rebuilt window, grace boundary and clock rollback, secure-storage failure, PIN confirmation, device setting and recovery |
| Widget/reminder links | 21 | Supported routes with typed identity/date, unlock and first-page ordering, rejected malformed input, repeated draft opening without ledger writes |
| Theme | 6 | Saved/system fallback, availability, subscription lifetime, reentrant native events and invalid choice rejection |
| Undo | 4 | Eight-second command boundary, clock rollback, once-only execution and offer replacement/dismissal |
| Valued-asset consent | 37 | Cancel/create/edit/accept, scope by account/kind, six translations, pending command exclusion and failure retry, real validator and transfer invariants |
| Settings reads/captions | 10 | Six live languages, pending availability/notification reads, shared publication, failure/retry, existing lock/unsupported notifications, valid defaults and real ledger preservation |
| Snapshot loading | 12 | Covered initial frame, shared pending reads, publish-before-ready, synchronous reloads, read/presentation failure and cancellation retry, no transient readiness on retry, real SQLite transfer identity/totals |

The original 68 cases carry `AT-80`; 37 valued-asset consent cases carry `AT-81` (D-74), and 12 snapshot-loading cases
carry `AT-83` (D-76), with ten settings read/caption cases carrying `AT-86` (D-79), for 127 total.
SettingsViewModel/native bindings remain running-app evidence; only its actual snapshot/label sources are linked here. A native port double supplies only window/dialog/authentication outcomes; it cannot prove
the native control rendering, Android Intent receiver, operating-system authenticator or device SecureStorage.
Those remain separate running-app and device checks in the acceptance plan. UI rendering, billing, real OAuth,
production encryption and physical-device acceptance are outside this project.

When extending a flow, add its actual source to the explicit compile list and keep native effects behind a documented
port. Keep resource links and central package versions aligned with the application. Never add a test-only copy of a
financial or security algorithm, connect to an owner account, or read an existing profile or verifier.

PlanDraftValidationTests compiles the real pre-save plan helper (D-99 / AT-104): 24 behaviour cases for one-pass
field feedback, existing money parsing/minor digits and recurrence rules, unknown/hidden inputs, missing/same
accounts, cross-currency amounts, unchanged drafts and corrected results. Main count is 1,408; App.Tests is 172.
Runtime layout/native validation remains separate in docs/quality/plan-validation-visible.md.

EntryDraftValidationTests compiles the actual transaction helper (D-100 / AT-105): 48 cases for independent
monetary problems, regional/digit/display-unit and ISO amounts, optional fees, original-currency/reimbursement
rules, hidden inapplicable values, immutable drafts and fresh corrections. Real EntryActions/LedgerValidator keep
one transfer and separate fee expenses. Current main count is 1,456; App.Tests is 220. Native rendering and valid
Simple destination-fee retention remain separate in docs/quality/entry-validation-visible.md.
