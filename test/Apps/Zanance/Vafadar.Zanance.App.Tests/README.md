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

All 68 cases carry `AT-80`. A native port double supplies only window/dialog/authentication outcomes; it cannot prove
the native control rendering, Android Intent receiver, operating-system authenticator or device SecureStorage.
Those remain separate running-app and device checks in the acceptance plan. UI rendering, billing, real OAuth,
production encryption and physical-device acceptance are outside this project.

When extending a flow, add its actual source to the explicit compile list and keep native effects behind a documented
port. Keep resource links and central package versions aligned with the application. Never add a test-only copy of a
financial or security algorithm, connect to an owner account, or read an existing profile or verifier.
