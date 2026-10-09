# 07 – Acceptance test plan

Automated tests live in `test/Apps/Zanance/*` and carry the scenario id in their name or a `[Trait("AT", "AT-17")]`.
Manual scenarios are executed on a real Android device with the **release** build (Gate 1B). The reference device and
data set (10,000 entries, 20 accounts, 100 active plans) are defined in slice S14.

| AT | Scenario (short) | Requirements | Test type | Slice | Status |
|---|---|---|---|---|---|
| AT-01 | Fresh install offline, no login | PR-01, ONB-01 | Manual | S2 | Verified (Windows Debug snapshots including theme/experience and existing-backup restore entry points, D-62; empty-profile integration test; device run pending) |
| AT-02 | Simple income/expense | TX-01, FIN-12 | Unit | S1 | Verified (unit, domain level) |
| AT-03 | Multiple fast Save taps | TX-06 | Unit (view model) + manual | S3 | Verified (unit, domain level) |
| AT-04 | Leave form / save error keeps input | TX-06 | Unit + manual | S3 | Implemented (discard confirmation, input kept on error); device check pending |
| AT-05 | Transfer between two accounts | FIN-02 | Unit | S1 | Verified (unit, domain level) |
| AT-06 | Report only source account | FIN-03 | Unit | S1 | Verified (unit, domain level) |
| AT-07 | Card purchase and card payment | ACC-04 | Unit | S1 | Verified (unit, domain level) |
| AT-08 | Transfer fee | FIN-02 | Unit | S1 | Verified (unit, domain level) |
| AT-09 | Opening balance + older history | FIN-04, IO-12 | Unit | S2 | Verified (unit, domain level) |
| AT-10 | Archive account with history/plans | ACC-06 | Unit | S2/S4 | Verified (unit): history stays in reports; active plans are ended with the archive (confirmation lists them) |
| AT-11 | Balance adjustment | ACC-08 | Unit | S1 | Verified (unit, domain level) |
| AT-12 | Partial refund same month | REF-01/02 | Unit | S1 | Verified (unit, domain level) |
| AT-13 | Refund of last month's purchase | REF-03, REP-02 | Unit | S1/S8 | Verified (unit + report screen: gross chart never negative) |
| AT-14 | Refund to another account | REF-01 | Unit | S1 | Verified (unit, domain level) |
| AT-15 | Refund larger than purchase | REF-04 | Unit | S1 | Verified (unit, domain level) |
| AT-16 | One-time future plan | FIN-07, REC-02 | Unit | S4 | Verified (unit) |
| AT-17 | Every 2 weeks from 2027-01-01 | REC-04/05 | Unit | S4 | Verified (unit) |
| AT-18 | Month with three bi-weekly occurrences | BUD-10 | Unit | S4 | Verified (unit) |
| AT-19 | Day 31, last valid day | REC-08/09 | Unit | S4 | Verified (unit) |
| AT-20 | Day 31, skip policy | REC-08 | Unit | S4 | Verified (unit) |
| AT-21 | Last day of month | REC-08 | Unit | S4 | Verified (unit) |
| AT-22 | Leap days Gregorian and Persian | REC-10 | Unit | S4 | Verified (unit) |
| AT-23 | Change display calendar | REC-11, LOC | Unit | S4 | Verified (unit) |
| AT-24 | Persian monthly rule | REC-11 | Unit | S4 | Verified (unit) |
| AT-25 | Amount change from next occurrence | REC-15 | Unit | S4 | Verified (unit) |
| AT-26 | Postpone one occurrence | REC-14 | Unit | S4 | Verified (unit) |
| AT-27 | Skip in 12-occurrence plan | REC-06 | Unit | S4 | Verified (unit) |
| AT-28 | Early confirmation | FIN-07 | Unit | S4 | Verified (unit) |
| AT-29 | Link existing entry to occurrence | REC-17 | Unit | S4 | Verified (unit) |
| AT-30 | Auto-post runs twice | REC-21 | Integration (SQLite) | S4 | Verified (integration, SQLite) |
| AT-31 | Delete/undo auto-post | REC-18 | Integration | S4 | Verified (integration, SQLite) |
| AT-32 | Two equal plans | REC-21 | Unit | S4 | Verified (unit) |
| AT-33 | App not opened for a month | REC-22, REM-10 | Unit + manual | S4/S10 | Verified (integration: ledger; unit: no missed reminders sent later, pending capped, reminders at the same time grouped into one summary) |
| AT-34 | Notifications denied/off | REM-02 | Manual | S10 | Implemented (due centre and banner without permission); device check pending |
| AT-35 | Snooze notification | REM-04 | Manual | S10 | Verified (unit: snooze repeats the reminder with its own id, never changes the due date, ends when the occurrence closes; moving a due date moves the reminder); device check pending |
| AT-36 | Notification for settled occurrence | REM-04, REM-06 | Unit + manual | S10 | Verified (unit: settled or skipped occurrences have no reminder; taps only open the occurrence) |
| AT-37 | Reboot, travel, DST | REM-07/08 | Unit (scheduler) + manual | S10 | Implemented (rebuild on start/resume/restore/language, boot receiver, local time); device check pending |
| AT-38 | Notification on lock screen | REM-05 | Manual | S10 | Implemented (generic text by default, lock respected on tap); device check pending |
| AT-39 | Zero / no / exceeded budget | BUD-01/05 | Unit | S1/S7 | Verified (unit + budget screen) |
| AT-40 | Total and sub-category limits | BUD-02 | Unit | S1/S7 | Verified (unit + budget screen) |
| AT-41 | Monthly equivalent of yearly cost | BUD-09 | Unit | S7 | Verified (unit) |
| AT-42 | Forecast with settled occurrence | FOR-03 | Unit | S9 | Verified (unit) |
| AT-43 | Unknown amount / missing rate | FOR-05 | Unit | S9 | Verified (unit) |
| AT-44 | Month-end positive, mid-period negative | FOR-08 | Unit | S9 | Verified (unit) |
| AT-45 | USD purchase on EUR account | FX-01 | Unit | S1/S11 | Verified (unit, domain level) |
| AT-46 | Change report currency | FX-05 | Unit | S11 | Verified (unit: incomplete totals, no relabelling; budgets keep their currency) |
| AT-47 | Currency without / with 3 decimals | FIN-05 | Unit | S1 | Verified (unit, incl. conversion between 0- and 3-digit currencies) |
| Q-02 | 10,000 entries, 20 accounts, 100 plans | Q-02 | Unit + manual | S14/S16 | Verified (calculation budget test with the reference data set); device measurement pending |
| AT-48 | fa/de/en and digit systems | LOC-01..04 | Unit + manual | S3/S14 | Verified (unit: digits, separators, search, resource completeness and usage; snapshots of every screen in en/de/fa) |
| AT-49 | Switch Simple/Advanced | UX-02 | Unit + manual | S13 | Implemented (mode never deletes data; hidden plan and budget settings summarised); manual check pending |
| AT-50 | Drill-down equals report number | REP-01 | Unit | S8 | Verified (unit + Home and report drill-downs with the same filter) |
| AT-51 | Import with ambiguous date/decimal | IO-08 | Unit | S12 | Verified (unit) |
| AT-52 | Re-import own CSV | IO-10 | Unit | S12 | Verified (unit) |
| AT-53 | Two identical real purchases | IO-10 | Unit | S12 | Verified (unit) |
| AT-54 | Import failure / cancel | IO-11 | Integration | S12 | Verified (integration, SQLite) |
| AT-55 | Multi-line / formula-like CSV text | IO-06 | Unit | S12 | Verified (unit) |
| AT-56 | Wrong password / damaged / other app | BAK-10 | Unit (library: verified) + integration | S6 | Verified (library + integration, SQLite) |
| AT-57 | Restore on fresh install | BAK-12 | Integration + manual | S6 | Verified (integration: fresh install, balances/plans/states; D-62 adds protected/unprotected restore, preferences and no duplicate account. Windows UI Automation: cancel returns to step 3, encrypted restore opens Home with the original 3 accounts. Device uninstall/reinstall pending) |
| AT-58 | 11th backup, failed upload | BAK-07 | Unit (library: verified) | S6 | Library verified |
| AT-59 | Disconnect / switch Drive/OneDrive | BAK-13 | Manual | S15 | Ready to run: implemented (D-35, D-50), needs a build with OAuth clients on a device – connect, back up with protection on/off (D-62), list, restore both package types, delete a cloud backup, disconnect, connect another account; per provider on Android, iOS and Windows. Google's token protocol on iOS/Windows is unit-tested (`Vafadar.Authentication.Tests`) |
| AT-60 | Upgrade with old data | BAK-12 | Integration (migrations) | every slice | Verified (integration: first schema with data upgraded to latest; restore migrates older backups); re-run each slice |
| AT-61 | App lock with notification/export | SEC-02 | Manual | S13 | Implemented (lock on start/leave, taps after unlock, export/backup/restore confirmation); device check pending |
| AT-69 | Device-wide four-digit app PIN | SEC-01..04, D-63 | Unit + Windows UI + device | Maintenance | 20 PIN cases verified (set/change/remove, salts, delays/restart/rollback, concurrency, storage failure, recovery); Windows UI: cold start stays covered, incorrect PIN rejected, one-minute delay survives restart, change/removal need the current PIN, cancelled backup confirmation creates no file; en/fa/de both themes at 360/412/wide checked. Device authentication/recents and iOS pending |
| AT-72 | Clear plan and debt setup | D-65, REC, F2-DEBT, UX | Unit + running-app + device | Maintenance | 21 new unit cases cover calendar-anchored monthly dates, first-inclusive count, short months, translations, debt signs and unsaved transfer reminders. Windows fixtures check blank Once, monthly/count, explicit rule calendar, optional debt/receivable terms in en/fa/de light/dark at 360/412/wide. UI Automation: positive debt/receivable saves open details with correct signs, negative input rejected; monthly/custom follows the first day; saved reminder has unknown principal, automatic posting off and no new ledger entry; cancellation leaves schedules/ledger unchanged. Strict Windows/Android builds and 1121 tests pass. Physical-device acceptance remains open. |
| AT-71 | Receipt purchase-total evidence and review | D-64, F2, PRI | C# + Windows image/UI + device | Maintenance | Regressions first failed on the old parser/layout, then passed; six-language valid text samples, tax inclusion, token boundaries/signs/zero, non-money numbers, currency/unit conflicts and skew/separator/page fixtures checked. Windows actual image selection: inclusive-tax/EXIF receipts give 4.10, skew gives 24.90; two totals require a manual choice, USD/EUR conflict and missing total leave the draft blank. Saved JPEG reread gives 4.10; missing-total reread keeps existing 4.10. Choice/cancel, picker cancel, unreadable PDF and failed image preparation do not change ledger/attachments. 1100 tests and strict Windows/Android builds pass; en/fa/de both themes at 360/412/wide checked. Device/corpus acceptance remains ZCR-QA-05. |
| AT-70 | Android screenshot choice and ownership wording | SEC-02, PRI, D-63 | Windows UI + Android build/device | Maintenance | Settings/help/About verified in six-language resources and en/fa/de UI; Android build verified; physical recents/screenshot acceptance pending |
| AT-62 | Golden data §24 | §24 | Unit | S1 | Verified (unit: results, and their display in en/de/fa) |
| AT-65 | Allocate money to two goals | F2-GOAL-02/05 | Unit | P2-1 | Verified (unit: funded money never exceeds the balance; lower priority loses funding first; completed goals release their earmark) |
| AT-66 | Splits, partial payments, final settlement | F2-TX-01/02 | Unit | P2-3 | Verified (unit: split changes the balance once, each budget sees its share; data: partial payments keep the occurrence open with the outstanding rest, the forecast expects only the rest, the final payment settles, auto-post never pays twice, delete and undo adjust the paid amount) |
| AT-63, 64, 67, 68 | Other phase-2 scenarios | F2-* | – | Phase 2 | Not included |

## D-67 acceptance additions

| Scenario | Requirement | Evidence and remaining device acceptance |
|---|---|---|
| AT-73 | Discover and restore connected backups from previous profiles/installations; optional protection | Library regressions cover all-set discovery, foreign-app exclusion, unchanged set retention, protected/unprotected preference restore and old packages. Rendered destination empty/error/files and device-file fallback. Real Google/OneDrive account flows, upload/download/delete and fresh-phone recovery remain AT-59 device acceptance. |
| AT-74 | Language independent of regional display | English + German numeric formats; Persian RTL with Latin/Persian digits; explicit choices survive language/restart; invalid saved choices fall back; converted-calendar numeric order and date field order. Onboarding/settings examples and help checked in en/fa/de, light/dark, 360/412/wide. Android native pickers and device acceptance remain pending. |


## AT-75 - Independent encryption feasibility (D-68 / SEC-01)

Four separate Windows harness tests pass: encrypted DB/WAL marker controls, SHM leakage check (not encryption),
wrong/missing key, integrity, ciphertext change, rotation/export, EF entity round-trip, DPAPI profile context and
PBKDF2/AES-GCM envelope authentication. Fixture data only. Android complete signed Release AOT/trimmed APK passes
on API 36 x86_64: common encryption checks on two fresh processes, Keystore-wrapped profile recovery after force-stop,
unchanged envelope/database hashes and envelope-tamper rejection. No main package was installed on the isolated AVD.
No production encryption, ARM64 device, iOS runtime, hardware-backed guarantee or crash-safe migration acceptance
is implied. ADR acceptance remains pending owner review. See proposed ADR 0010 and experiments/Zanance.Encryption.


## AT-76 - Build-specific SDK and Android permission boundaries (D-69 / SEC-09)

Eleven PowerShell policy cases cover the current allowlist, wrong network variant, unexpected privileged capabilities,
wrong package, Debug and missing reminder capability. Complete signed Offline and Cloud Release APKs pass their
explicit guards; the Cloud artifact is rejected under the Offline policy. SDK inventory and qualified privacy copy
are documented. These tests do not prove native SDK traffic, real OAuth, signed iOS privacy labels or future SDKs.

## AT-77 - Goal contribution reminders (D-70 / LOC-02)

Twelve Core cases cover month-end anchors, two-week intervals/count endings, Persian rule dates, lifecycle and
source exclusions, reaching/withdrawal, opt-out, missed-date exclusion, stable ids and pending bounds. Seven linked
coordinator/SQLite cases use actual app texts in en/fa/de and check generic defaults, opted-in names, grouping,
permission denial without refresh-time prompts, pause/reached/archive/disable cancellation, and no ledger/allocation
writes. Main suite: 1,153 passed. Goal editor and help are checked in en/fa/de, light/dark, at 360/412/wide.
Android API 36 x86_64 fixture passes actual native 09:00 pending requests (October +02:00, November +01:00),
generic notification delivery, tap to goal details, pause cancellation, resume and saved opt-out after process restart;
ledger/allocation counts remain zero. Complete signed Release APK signature, ZIP, package, embedded arm64/x86_64
assemblies/app AOT and Cloud permission guard pass; actual emulator install/cold start pass. Release metadata excludes
both Debug fixture types. ARM64 phone, Doze/reboot and iOS acceptance remain open. The opt-in Android Debug fixture
requires an empty installation or its own marked single account, never existing user data. It verifies normal 09:00 pending requests before expediting one fictitious
notification for delivery/tap testing without changing system time. This does not prove exact-time or battery-policy
delivery.

## AT-78 - Optional period review reminder (D-71 / LOC-03)

Nine planner and six real-store/coordinator cases cover off-by-default/empty profiles, Gregorian/Persian/Hijri
year boundaries, financial month start, finished/partial reviews, missed-time suppression, stable rebuilds, generic
en/fa/de delivery, details opt-in, permission refusal and cancellation without ledger or review-progress changes.
Upgrade defaults existing settings to off; the compiled model matches, the backup round-trip preserves the opt-in,
and plan/goal/review requests share the bounded queue. Main suite: 1,168 passed. Settings/help are reviewed in en/fa/de,
light/dark, at 360/412/wide. Android API 36 x86_64 checks native future 09:00 requests, a real generic notification,
tap to review and UI opt-out persisted across process restart on fictitious data. One notification is expedited by a
Debug-only fixture; no system time/settings change. Complete signed Release APK is separately inspected and installed.
Physical ARM64 phone, exact-time/Doze/reboot and iOS acceptance remain open; no production-release claim.

## AT-79 - Aggregate import choices and durable Undo (D-72 / LOC-04)

Eight Core cases and nineteen real SQLite cases verify matching account/kind/category/inclusive dates, partial files,
known/repeated IDs, repeated batches without resubtraction, incoming and fully consumed aggregates, complete metadata
clones, attachment retention, durable restart/backup Undo and DeleteAll journal cleanup. Missing/stale choices, shared
details, previous links, later edits, dependent imports and external refunds leave every row unchanged. Balance and
expense invariants remain intact. Main suite: 1,195 passed; final localization/resources: 242 passed.

Windows en/fa/de light/dark pending/link/keep-both/help is reviewed at 360/412/wide; original development database
hashes match after fixture restoration. Android API 36 x86_64 UI import of six details totaling 395 EUR leaves a
17 EUR aggregate and total 412 EUR; restart preserves the history and skips those IDs. The trimmed/AOT Release APK
installs/cold-starts, displays the durable history and performs confirmed Undo. Native ledger shows one original
412 EUR row; subsequent unlaunched Debug data inspection confirms original identity, ledger count 1 and journal 0.
Signed complete APK package/signature/ZIP/ABIs/assembly stores/AOT/Cloud permission guard pass; all five Debug
diagnostic types are absent from Release. ARM64 phone and iOS acceptance remain open. No real profile or financial
data, screenshot policy, system settings or physical phone was touched.

## AT-80 - Application flows and command boundaries (D-73 / QA-03)

The new application test project links actual non-UI sources and native-effect interfaces, with real SQLite and
translations. Its 68 cases cover onboarding restore/retry/regional choices, exclusive profile isolation/rollback and
owned deletion, bulk selection/copies/validation/transfer scope/dialog exclusion, startup/PIN/device recovery and
missing/lost covers, strict widget/reminder routing, native-theme event policy and timed Undo. No MAUI control mocks
or duplicate application algorithms. Main suite: 1,263 passed, zero skipped; see App.Tests/README.md for the case map.

Runtime evidence: 522 application-window captures: en/fa/de, light/dark, 360x800, 412x892 and 1280x820 for the affected onboarding, settings/PIN, profiles and transaction screens; additional 412px startup/editor/theme checks after the final lifecycle change. Original development databases were restored with matching hashes. Existing API 36 x86_64 emulator: actual new-profile onboarding, wrong/correct fictitious PIN, cold widget drafts after unlocking in complete Debug and Release, native bulk tag/delete/Undo. The native startup test exposed pending widget navigation before the first real page; the fix passes new ordering tests and both installed variants. Opening a widget draft leaves the ledger unchanged. SQLite after Release Undo preserves the original expense identity, 1,234 minor units and QA03 tag, with no extra income. One initial Debug accessibility dump lacked the restored row although the database/day total were correct; immediate row restoration passed in Release and in a controlled Debug repeat. Its cause is unconfirmed; retain it in physical-device follow-up rather than claiming a separate fix. The fictitious emulator PIN was removed through Settings and the final Release reinstalled. Strict Windows and complete Android Debug/Release builds finish with zero warnings/errors; 1,263 tests pass, zero skipped. Signed Release handoff: artifacts/android/zanance-d73-release.apk, 81,074,261 bytes, SHA-256 75df49d16284bb6604cce740fd3b500ebd77789cfede0bfc0df850aa63fd10c3. Package pro.vafadar.zanance, min SDK 24/target 36, v2/v3 signature, ZIP integrity, ARM64/x86_64 assembly stores and AOT libraries verified; Cloud permission guard passes. FLAG_SECURE was unchanged; native checks use accessibility XML and only the isolated emulator, never attached physical phones.

Physical phone, native OS credential UI, real-provider restore, iOS and production acceptance remain separate gates.
The app access gate does not encrypt financial data. No existing owner profile, stored verifier or physical phone is
used in application tests; the complete signed Release APK remains the required phone-test handoff.
