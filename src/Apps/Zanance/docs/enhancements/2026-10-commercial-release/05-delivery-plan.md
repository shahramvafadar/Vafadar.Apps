# 05 – Delivery plan

**Way of working:** one section at a time. The owner approves a section; it is built, tested, documented, committed
and pushed; then exactly one next section is proposed and work stops until the next approval. One approval never
covers a whole wave. Item states live only in [04-backlog.md](04-backlog.md).

The owner approved D-62 maintenance (ZCR-LOC-11) on 2026-10-08 before continuing the commercial waves.
This covers only backup, first run and About; wave 1 and other sections still require their own approval.

## Waves (dependency order)

| Wave | Content | Exit criterion |
|---|---|---|
| 0 | Audit, plan matrix, architecture, backlog, delivery plan (ZCR-GOV-01); Tax and service-cost research start (TAX-01, OD-01/02) | Documents pushed; owner picks the first section |
| 1 | Local security: SEC-01 … SEC-09, highest risk first (database encryption, keys, app password, migration, recovery, OS backup rules) | Encrypted database on new and upgraded installs, recovery path tested, no plaintext safety copies |
| 2 | Release-critical local gaps: owner's bug list, ZEX leftovers (LOC-02…05), recurring entry point, import presets, device runs, accessibility, app tests, performance | All P1 of wave 2 Done or explicitly deferred by the owner |
| 3 | Plan policy and quotas (ENT-01…05), optional identity and backend foundation (ID-01); no real sale | Policy tests for every combination; enforcement switched off in release until wave 6 |
| 4 | Personal sync (SYNC-01…03) | Two-device conflict and idempotency tests pass |
| 5 | Shared space, roles, member lifecycle (SHR-01…04) | Authorization tests per request/resource pass |
| 6 | Billing in sandbox, restore/refund/upgrade, trial, offers, Lifetime, paywall, end of plan (BIL-01…05) | Sandbox matrix passes; production only by the owner |
| 7 | First approved Tax scope (TAX-02/03) or its own near milestone | Official examples pass; expert review recorded |
| 8 | Other approved release features; online rates/bank/AI only with approved scope and cost | Per item |
| 9 | **Last feature wave: at least 20 additional languages** | Display, calendars, numbers and receipts checked per language |
| 10 | Final QA, bug fixes, store metadata, staged rollout; no new features | Release checklist complete |

Deviations: Tax research and service-cost estimates start in wave 0 because they can change the plan; the
accessibility pass of the Phase 2 backlog is placed in wave 2 (release-critical), not after all features, because the
owner asked for it before release and it does not depend on the commercial waves. Existing-language copy fixes continue
in every wave.

## Release gates

1. Security gate (after wave 1): database encryption, recovery and migration tested; privacy matrix updated.
2. Local release gate (after wave 2): device acceptance runs, accessibility checklist, performance numbers.
3. Commercial gate (after wave 6): sandbox matrix, store review of texts, cost model, owner approval of prices.
4. Store gate (wave 10): release checklist `08-release-checklist.md`, privacy policy, Data safety, Financial features.

## Mandatory acceptance tests across the plan

* Free works without an online account, with the user's real data, without any service.
* Every combination of plan, purchase kind, Simple/Advanced, online/offline and personal/shared role is covered by
  policy tests.
* Quotas cannot be bypassed via UI, template, import, restore or concurrent actions; recovery and export never need a
  purchase.
* End of Plus/Pro, Lifetime with and after Pro, refund, revoked, pending, grace and duplicate notifications give the
  right result; turning off auto-renew does not end valid access early.
* Store/server/network errors never lose data or a form; changing the phone clock creates no fake subscription.
* Two devices settling one occurrence or recording one purchase never double-count.
* A Viewer or removed member cannot write; no report, file, search or API reveals an unshared account.
* An interrupted encryption migration, a wrong password, a biometric change and a restore on a new device never
  corrupt data.
* Plan expiry and theme/language changes while a form is open keep the input.
* "?" help, examples, short labels and screen reader hints stay connected; 200 % font, 360/412 px, both themes, RTL.
* Tax is tested with reference examples of the country/year; AI running out of credit or answering wrongly never
  changes data or amounts.

## Section template

Every section is written with: id and title · value and why now · prerequisites and decisions · current state and
source evidence · scope / out of scope · files and possible migrations · UI changes and translation keys · acceptance
tests and sample data · security/privacy/cost impact · relative size and uncertainty · rollback without data loss ·
Definition of Done and testable output · proposed next section.

## Proposed first section: ZCR-SEC-01 – Threat model, encryption decision and feasibility proof

* **Value and why now:** the local database (including attachments, WAL/SHM) is plaintext today, and Android's OS
  backup may copy it. Every later step of wave 1 depends on choosing a library and a key design that works on Android,
  iOS and Windows with .NET 10 MAUI.
* **Prerequisites:** none for the research; OD-04 (recovery) can be answered while it runs. Any paid licence needs the
  owner's approval before adoption.
* **Current state:** [01-current-state.md §2](01-current-state.md#2-security-and-data-protection).
* **Scope:** threat model; inventory of files with financial data; comparison of SQLCipher community build
  (`SQLitePCLRaw.bundle_e_sqlcipher`), commercial SQLCipher builds and the SQLite Encryption Extension (licence, cost,
  maintenance, Android/iOS/Windows, trimming/AOT, EF Core compatibility, performance); key design (random data key,
  keystore/keychain/DPAPI wrapping, optional password KDF, per profile); a feasibility test that creates an encrypted
  database, re-opens it, rejects a wrong key and checks WAL/SHM are not plaintext, run on Windows and in the Android
  emulator; ADR `docs/adr/0010-database-encryption.md`.
* **Out of scope:** any change to the app's database, migration of user data, UI, texts.
* **Files:** the ADR, the decision log entry, the backlog state; the feasibility test lives in a separate test project
  or is removed after the proof (decided in the section; nothing reaches the app).
* **UI/texts:** none.
* **Tests:** the feasibility test on Windows (dotnet test) and on the emulator (a small console/instrumented run);
  results written into the ADR with versions.
* **Security/privacy/cost:** no user data touched; possible licence cost flagged for approval.
* **Size:** M; uncertainty: Android native library packaging with .NET 10 and the EF Core provider combination.
* **Rollback:** documents only plus an unused test; nothing to roll back in user data.
* **Done:** ADR with chosen library, key flow, migration outline and measured feasibility; owner approves the ADR.
* **Next section after it:** ZCR-SEC-07 (OS backup rules, small and immediate risk reduction) or ZCR-SEC-02/03
  (encrypted database with keystore keys), depending on the ADR.

## Handoff

* 2026-10-07 – wave 0 done (ZCR-GOV-01): documents in this folder, D-61, MON-02/03 superseded. Waiting for the
  owner's approval of ZCR-SEC-01 and answers to [06-open-decisions.md](06-open-decisions.md) (OD-01, OD-04, OD-10 first).
