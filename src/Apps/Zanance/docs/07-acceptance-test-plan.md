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

## AT-81 - Legacy valued-asset income/expense consent (D-74 / QA-04)

37 application cases compile the actual AssetEntryConfirmation used by the editor, with real SQLite continuations
and actual translations. Cancelled new/edit saves do not invoke draft mutation or change a persisted record; accepted
writes keep identity/kind/minor units. Pending/failed dialogs cannot write or overlap another save; retry requires
fresh consent. Non-asset accounts and non-Income/Expense kinds keep their existing paths. Accepted invalid entries
are rejected by the financial validator. A real transfer stays one entry with zero income/spending and unchanged
combined balances. All six UI languages request their existing explicit dialog labels. Main suite: 1,300 passed.

Runtime evidence: Strict Windows and complete Android Debug/Release builds finish with zero errors/warnings; final main suite
1,300 passed, zero skipped. Windows actual Save/native cancel via UI Automation: en/fa/de, light/dark, 360x800,
412x892 and 1280x820; 288 app-window captures, including 36 actual consent/cancel checks. Ledger identities, asset
opening value, amount/note draft and idle state remain unchanged after cancellation. Original development database
files restored with matching hashes; no desktop clicks/keys/focus requests.

Existing isolated API 36 x86_64 emulator: actual fictitious Asset account at 800,000 minor units; Debug native
expense Cancel preserves the complete account/ledger rows and the 12.34/account draft. Installed trimmed/AOT Release
asks for both Expense and Income; Cancel retains 12.34/20.00 and selected asset. A fresh Record anyway saves each
once and returns Home. Native account list shows 8,007.66 EUR. SQLite independently confirms only the original entry
plus one Expense 1,234 and one Income 2,000; original row and complete account rows unchanged, no cancelled/duplicate
entries, final asset balance 800,766. Final Release reinstalled after copying only the owned fictitious database via
an unlaunched Debug package. No PIN, stored verifier, owner profile or attached physical phone is inspected.

Phone handoff: artifacts/android/zanance-d74-release.apk, 80,639,915 bytes, SHA-256
82f2afa9cc4060d419bac64e082ccf74ca10429c645cffabc0bc4a50afcb7262. Package pro.vafadar.zanance; min SDK 24,
target 36; signature v2/v3, ZIP integrity, ARM64/x86_64 assembly stores and AOT libraries verified. Cloud permission
guard passes. FLAG_SECURE stays unchanged; emulator checks use accessibility XML. This is local/emulator engineering
evidence, separate from physical ARM64, real OS credential UI, iOS and release acceptance.

This is manual editor consent, not a persisted authorization flag or a new import/restore rule. Native consent,
physical ARM64, iOS and product acceptance remain separate; fictitious emulator data does not establish owner-device
acceptance. The device access gate and this warning do not encrypt the database.

## AT-82 - Large-ledger measurement safety and account indexing (D-75 / QA-06)

23 new cases: 12 Core cases compare the index with the existing financial balance formula across kinds, dates,
currency, confirmation and account scope; keep transfer identity/order, legacy self-transfers and checked arithmetic.
11 actual SQLite cases verify reference/tenfold shapes, exclusive fictitious paths, literal semicolons, deterministic
legacy rows, retained-field migration fingerprints, manual plans, queued profile capture and cancelled reads.
Main suite: 1,323 passed, zero failed/skipped. No schema, compiled-model, permission, SDK or portable-data change.

Strict Windows and complete Android Release builds: zero errors/warnings. 544 application-window captures cover
Home, Transactions, Forecast and Reports in en/fa/de, light/dark at 360/412/wide; original development database files
restored with matching hashes. Isolated API 36 x86_64 Release process-cold Home, actual search buttons and Save are
measured for both shapes. Complete database read-back preserves every original account/ledger row and proves
exactly one new Expense of 1,234 minor units per shape; previous three-entry fixture unchanged. Final Release
reinstalled. Package, v2/v3 signature, ZIP integrity, ARM64/x86_64 assembly stores/AOT and Cloud permission guard pass.
Handoff: artifacts/android/zanance-d75-release.apk; 80,644,011 bytes; SHA-256
d683c4f0e6c350ce7be0a290492a64fe5c8f9711d3784f23775712757487d7dd.

See [Q-02 evidence](quality/performance-q02.md) for exact raw samples, timer boundaries and failed observations.
Controlled no-match/unique-result searches pass after initial list loading; entering a query during the initial
tenfold load still exposed a counter/date without a native result row. The baseline ANR is retained for follow-up,
and Android loaded Home shows no demonstrated duration improvement. QA-06 findings and Q-02 remain open; these are
engineering observations, not a two-second device claim or physical ARM64/iOS/provider/store acceptance.

## AT-83 - Transaction snapshot loading, publication and retry (D-76 / QA-06)

Twelve new actual application cases: covered initial frame, shared pending reads, publication before ready,
synchronous repeat reads, failed first/reload/presentation paths, retry without transient readiness, cancellation/retry, two missing-callback cases
and real SQLite transfer identity/balance preservation with zero income/spending. App.Tests: 117; main suite:
1,335 passed, zero failed/skipped. No financial formula, search algorithm, schema, security, SDK or permission change.

Strict Windows build: zero warnings/errors. 216 final app-window captures review Transactions/list/loading/read
failure/reload plus existing filter/bulk states in en/fa/de, light/dark, 360x800/412x892/1280x820. Loading text, covered
rows, disabled controls, readable failure/retry and restored results are visible; the animated Windows indicator
glyph is not demonstrated by the still captures. Original development database files restored with matching hashes.
The failure is an explicit fictitious Debug read. An additional 36 captures at 360x800 dark verify the actual native
retry button through its UI Automation Invoke pattern in all three languages, followed by complete real snapshot
publication. Two earlier diagnostic runs could not locate the button through the window's dialog-oriented lookup;
the successful diagnostic uses the current page's real named control/handler, never desktop input or focus.
Every preservation guard restored original development files with matching hashes. Release result rows and physical/
device/iOS acceptance remain separate from this fixture and the helper tests.

Complete final Release on the existing isolated API 36 x86_64 emulator: tenfold loading XML exposes the loading text
and native progress indicator, with Search/More filters/period/kind/Add disabled, no result rows or false empty claim.
A native tap/text attempt on disabled Search is ignored; loaded Search retains its placeholder and the keyboard is
not opened. Actual rows appear after publication. One preceding loading hierarchy was unavailable and retained;
reference loading completed before the first hierarchy, so it does not separately demonstrate its loading frame.
Both 10,001/100,001-row fixtures pass no-match/three unique-transfer result-button transitions over All and returning
Home/Transactions retains the last query and exact row. No ANR dialog observed in these checked sequences.

Unlaunched Debug inspection reads only the three owned fictitious databases: complete Accounts/Entries/Schedules
equal D-75 read-back, counts 10,001/100,001/3, no new or changed financial row. Final Release reinstalled and the prior
native profile restored. Screenshot protection stays intact; no physical phone, owner database or SecureStorage read.
Handoff: artifacts/android/zanance-d76-release.apk, 80,664,491 bytes, SHA-256
d7e2c558159ed3e9c4bab2cf4b7e0f9b5c049527989768ad6dc16d1f9a7fae9b. Package pro.vafadar.zanance, 0.1.0/code 1,
min SDK 24/target 36; v2/v3 local signature, ZIP integrity, ARM64/x86_64 assembly stores/app AOT and Cloud permission
guard pass. Strict final Windows and complete Android Release builds have zero warnings/errors. CI, emulator and
local data proof do not close physical ARM64/iOS, baseline ANR follow-up, loaded Home or the two-second objective.

## AT-84 - Large-text Home and plan layout review (D-77 / A11Y-03, first slice)

Runtime scenario, not a new unit-test count. Main suite remains 1,335 passed, zero failed/skipped (App.Tests: 117).
Strict Windows Debug and complete Android Release builds finish with zero warnings/errors. Review includes Home,
plan rows, entry editor, Settings and onboarding: 220 final app-window captures in en/fa/de; 200% process-local
Windows stress in light/dark at 360x800, 412x892 and 1280x820, plus a 100% narrow baseline. Twenty-one actual native
Home layouts verify 84 untrimmed labels and targets at least 44x44 px. Two final English smoke walkthroughs add
30 captures and two Home checks: no transactions at 200% and the unchanged normal font path, with no override metadata
on the latter. The floating Add overlaps part of the large-text quick-account row and remains open. Original development data files restored with
matching hashes; no desktop input, focus request or OS setting change. See [evidence and limits](quality/font-scaling-a11y03.md).

Existing API 36 x86_64 emulator: temporary native font-context experiments exposed pinned locale and covered startup
and were removed from source before delivery. These are failures, not native 200%/RTL acceptance. Final normal Release
starts, displays all four Home actions, changes language and exposes Persian RTL Home/Settings and empty Plans.
Actual Expense opens its draft and visible Cancel closes it without Save; hardware Back did not close that blank
editor in the checked sequence. Populated native plan rows and large native RTL text remain unverified. English and
Use device setting were restored through the UI; a normal restart confirms English Home, then the app is force-stopped.

Pre-Release-navigation read-back preserves complete Accounts/Entries/Schedules in the owned three-entry QA03 fixture
against D-76. It is not a claim of a post-navigation database checkpoint. No physical phone, owner data, PIN/verifier
read or global text setting change. Screenshot protection remains enabled. Final Release stays installed.
Handoff: artifacts/android/zanance-d77-release.apk, 81,107,029 bytes, SHA-256
15f09bc889572b9fa0d1ab45138cf79d9c9055c2967e5aac063ed4d0d75871d0. Package pro.vafadar.zanance; min 24/target 36;
v2/v3 local signature, ZIP integrity and complete ARM64/x86_64 assembly stores/AOT verified. Remaining layouts,
screen readers, actual OS large-text settings, native RTL/200%, physical ARM64, iOS and release acceptance stay open.

## AT-85 - Persistent actions and financial identity (D-78 / A11Y-03 follow-up)

Runtime layout scenario, not new unit cases. Main suite remains 1,335 passing, zero failed/skipped. Strict Windows
Debug and complete Android Release builds have zero warnings/errors. Seven page action rows sit outside scroll
viewports; financial identity wraps above amount/status; group labels/totals and Undo message/action use separate
rows. Existing commands, semantic names, visibility rules, formatting and ledger calculations are retained.

En/fa/de, both themes, 360x800/412x892/1280x820 at process-local 200%, narrow 100% and Simple narrow-dark review:
840 matrix captures plus 420 transaction/Undo preview captures. The 860 actual-layout records pass 412 Add, 45 bulk
and 21 Undo dock checks; 1,576 entry, 192 account and 92 holding realized rows have useful identity width, untrimmed
native title and separate amount geometry. Repeated observations are not unique screens/data/test cases. Undo
preview never deletes/invokes Undo. Original Windows development files restored with matching hashes. See
[scope, diagnostic selection failures and open findings](quality/font-scaling-a11y03.md).

Existing API 36 x86_64 emulator, unchanged density/normal font: six English Add paths open/cancel their drafts;
empty Goals uses its existing primary Add goal. German/Persian Home/Transactions/Accounts expose readable two-row
financial content and a 147x147 px plus outside the actual viewport (44 dp minimum). No Save/delete/Undo or physical
phone interaction; populated native plan/holding/goal rows and large native RTL are not demonstrated. An ignored
Settings language selection and retained old-language theme/mode captions remain independent findings; a later
fresh-visit retry restores actual English. No theme choice was changed.

Post-navigation unlaunched Debug read-back copies only owned fictitious QA03 data: complete Accounts, Entries and
Schedules equal the D-76 baseline, with three entries. Final Release is reinstalled, normal Home/Transactions checked
and force-stopped. English restored and System theme confirmed selected through UI; screenshot protection retained and no
SecureStorage or owner-profile inspection. APK artifacts/android/zanance-d78-release.apk, 81,123,413 bytes, SHA-256
03892da524ac5d43eeda27561f4f6e71ae4f427dd220bb91e5c0566fe3803a22. Package pro.vafadar.zanance, 0.1.0/code 1,
min 24/target 36; v2/v3 local signature, ZIP and full ARM64/x86_64 stores/app AOT verified. Fixed actions, currency
grouping, other screens/controls, real OS text settings, screen readers, native large RTL, ARM64 phone/iOS and release
acceptance remain open. A11Y-03 is in progress; CI/build success is separate from those gates.

## AT-86 - Settings publication and live choice captions (D-79)

Ten actual-source cases cover delayed device/notification reads and one shared pending operation, availability
failure with covered retry, unsupported notification callbacks, saved-lock availability, six real translations
and isolated SQLite default-account eligibility/order. Complete account/entry identities and amounts, saved
preferences and estimate remain unchanged; a transfer creates neither income nor spending. App.Tests compiles
the real SettingsSnapshot/SettingsChoiceLabels, not SettingsViewModel or native controls. Main suite: 1,345 passed,
zero failed/skipped; App.Tests 127. Test output cleaned afterward.

Running Windows checks separately exercise the real view-model/bindings, held read, failure, actual native retry
button, live de/fa/en captions, unchanged choice indexes and unsaved estimate input, with stored preferences/entries
unchanged. See quality/font-scaling-a11y03.md for the final matrix, normal Release emulator proof and signed APK.
Keep physical-device, real OS 200%, screen-reader and iOS acceptance open; D-67's deferred Shell rebuild is retained.

D-79 final runtime: 462 Windows captures across en/fa/de, both themes, 360x800/412x892/1280x820 at process-local
200%, plus the 100% narrow baseline; 21 proof files pass. Normal final Release on emulator-5570 applies all three
languages through real pickers, persists them on cold restart and refreshes native theme/mode chips while preserving
System/Advanced. Complete QA03 Accounts/Entries/Schedules equal D-76 baseline after navigation (three entries).
Final Release reinstalled, English Home/Transactions checked and app stopped. Strict Windows/Android builds have
zero warnings/errors. Full APK artifacts/android/zanance-d79-release.apk: 80,713,643 bytes, SHA-256
989227ed5f8bee52c96cab755a53dfc737eb409f52976adf116a58e9364db242; pro.vafadar.zanance, min 24/target 36, ARM64/x86_64 full assembly stores/AOT,
ZIP integrity and v2/v3 local signature verified; Cloud permission boundary passes. Native toolbar Back captions,
German narrow/large header, other fixed controls and OS/device/screen-reader acceptance remain independent findings.

## AT-87 - Growing action captions and native selection commands (D-80)

Runtime scenario, not extra xUnit cases. Main suite remains 1,345 passed, zero failed/skipped (App.Tests 127).
Actual Settings, account/debt detail and Transactions bindings are reviewed with growing semantic action surfaces.
Check native untrimmed captions, positive caption width, at least 44 px target size, full spoken name and unchanged
command/argument binding. Record native disabled states with no selection and enabled states after selection.
Invoke the actual Select all and Cancel buttons through UI Automation; verify existing selection behavior and
unchanged full entry JSON. D-79's actual retry/live-caption/unsaved-input proof remains valid in this route.
See quality/font-scaling-a11y03.md for final cohorts, native checks and the complete signed APK. Real OS 200%, keyboard/
screen readers, physical ARM64 and iOS acceptance remain separate. No financial or destructive command is invoked.

AT-87 result: 1,722 rendered captures and 3,129 actual growing-action geometry records pass across en/fa/de, both
themes, 360/412/wide at process-local 200% plus a 100% baseline. Normal Release emulator checks actual All/Cancel,
account Edit/transaction drafts cancelled without Save, and translated PIN/delete native Button captions. Complete
owned fixture Accounts/Entries/Schedules remain unchanged (three entries). Full signed D-80 APK and final cold
Release checks are recorded in the quality report. Loan action runtime evidence is Windows-only; native QA03 has
no loan. No physical/OS 200% or screen-reader acceptance claim.

## AT-88 - Complete date parts and scrollable large amounts (D-81)

Runtime scenario, not extra xUnit cases. Main suite remains 1,345 passing, zero failed/skipped (App.Tests 127).
Check the actual native date Entries against all four/two digits at the real scale, useful 44 px targets, existing
calendar/culture order and complete preview. A valid draft updates DateOnly; a partial year retains the last valid
date. Inspect Gregorian/Persian/Hijri parts and verify complete entry JSON without Save. Programmatic draft tests
and actual external UI Automation ValuePattern input are separate evidence.

Large captions preserve the original MoneyText packet, sign, decimals/unit and full spoken description. Verify
realized single-line native glyph/caret boundaries inside the actual content and use the real native Scroll pattern
to reach both ends and return to the sign. Use the actual eligible split modal as well as Home/account/plan/asset/
budget/report readouts; the ordinary refunded expense fixture deliberately rejects splitting. Local geometry/CI
is separate from keyboard/screen-reader, actual OS 200%, physical ARM64 and iOS acceptance. Final rendered/native/
APK evidence and negative review findings are recorded in quality/font-scaling-a11y03.md.


## AT-89 - Repeated native language selection and Settings reopening (D-82)

Runtime regression, not extra copied-control unit cases. Use native Picker SelectionItem and header Invoke patterns
to choose de/fa/en/original language, return and reopen the actual Settings page. Retain old shells strongly so
collection cannot conceal a translated title still reaching the disconnected renderer. Old item/section/content
titles stay frozen; current titles and choice captions translate. The open page and unsaved estimate draft stay
intact until Back. Compare complete stored settings, accounts and entries, including the settings timestamp.

Normal full Release Android checks repeat de/fa/en without a cold restart between choices; actual picker/body
captions and returned navigation agree, RTL switches on the replacement root, and a final cold English restart
checks persistence. Keep screenshot protection and device settings. Main suite remains 1,345, App.Tests 127.
Final rendered/native/APK evidence and negative review findings are in quality/font-scaling-a11y03.md.
Physical ARM64, real OS 200%, keyboard/screen readers, provider and iOS acceptance remain separate.

## AT-90 - Growing headers, retained bodies and current Back descriptions (D-83)

Runtime scenario, not additional copied-control xUnit cases. At 360/412/wide, both themes and en/fa/de, measure
complete native title/caret bounds, scalable untrimmed text, a 44 px native target and the current translated
Button peer name. A real Settings page retains its body/bindings and unsaved estimate while resizing; nested
navigation returns to exactly one header and the same body. Invoke native Back and compare complete stored
settings/accounts/entries without Save. Settings' existing nested-return draft reload remains a separate finding.
Normal full Release Android repeats real de/fa/en picker/Back/reopen and checks the current native ImageButton
content description immediately after each choice. Preserve FLAG_SECURE, device settings and owned fixture rows.
Final matrix/build/test/APK evidence is in quality/font-scaling-a11y03.md. OS 200%, screen readers, physical ARM64
and iOS acceptance remain separate.

## AT-91 - Retain the unsaved Settings estimate after a nested return (D-84)

Thirteen linked-production-source cases cover exact partial/invalid/empty/native-digit text, period-only edits,
changed default currency, clean external refresh, profile/row boundaries, successful Save and later typing. SQLite
cases verify complete stored preferences and no entries without Save. Runtime headers checks retain the real
Settings draft after a nested return, plus existing body/resize/native Back/data equality assertions. Keep three
languages, both themes and 360/412/wide checks focused on this changed form. Final native/build/test/APK evidence
and limits are in quality/settings-estimate-draft.md; no physical OS/screen-reader/device/iOS acceptance inference.

## AT-92 - Visible Insights destinations and real route invocation (D-85)

Runtime tests, rather than copied UI-control unit cases: all four full native caption layout slots, at least 44 px
targets, current translated button names and exactly one selected underline/hint. Invoke each actual native target
to its existing route, check single attachment with retained body/bindings, and resize only the app's own window.
Compare complete stored accounts/entries/settings without Save. Focused en/fa/de, light/dark and 360/412/wide
matrix plus 100% narrow baseline; evidence and independent OS/screen-reader/phone/iOS limits are recorded in
quality/insights-navigation.md. Android/iOS retain their own native top tabs.

## AT-93 - Complete budget figures and signed boundary formatting (D-86)

Eleven Core cases independently reproduce the original signed-boundary exception and verify the correction across
ISO 0/2/3 minor digits, en/de culture, approximation, bidi isolation and IRR/Toman. Positive parsing still rejects
the unsigned magnitude beyond Int64.MaxValue. Actual Windows native checks cover complete wrapped budget names,
separate full-width spending/limit rows, compact/default scalable typography, original spoken monetary packets,
overflow feedback and both native Scroll endpoints. Restore in-memory collections and compare complete stored
accounts/entries/settings/budgets/plans. Cover en/fa/de, light/dark, 360/412/wide at process-local 200%, with a 100%
narrow baseline. Normal Android Release checks use an independently owned fictitious budget fixture and prove
its full restoration. Final counts/build/APK evidence and separate OS/screen-reader/phone/iOS limits are in
quality/budget-readouts.md. Interrupted route captures never count as acceptance.

## AT-94 - Every budget period choice is visible and selectable (D-87)

Retain the pre-change 360 px/200% hidden third-choice geometry/render failure. Check all three actual native
caption layout slots, target bounds (at least 44 px), complete translated selected names and bold state inside
the growing group. Use native Invoke for all three existing decisions, join the actual serialized reload and
verify selection, period text and TwoWeeks state. Restore original selection; compare complete accounts/entries/
settings/budgets/plans without Save. Focus en/fa/de, both themes and 360/412/wide process-local 200% with a 100%
narrow baseline. Signed Android Release checks and independent platform limits are in quality/budget-periods.md.
These runtime checks supplement the suite; do not invent unit-test counts or change shared ChoiceChips behavior.

### AT-04 focused Android hardware Back evidence (2026-10-09)

Normal signed D-87 Release on the isolated API 36 x86_64 emulator: first Back dismisses the amount keyboard,
next Back closes a clean Expense draft. An unsaved 17.25 draft asks before leaving; Keep editing preserves exact
input, a second request asks again, and Discard returns Home without Save. Complete six-table fictitious data
comparison and SQLite integrity pass. The historical D-77 blank-editor observation is resolved without a code
change; see quality/font-scaling-a11y03.md. This English normal-text sequence does not close physical-device,
other-editor or save-error acceptance. No application rebuild is necessary for this unchanged-binary check.

## AT-95 - Complete tag captions and original-value selection (D-89)

Retain the normal Release undersized target and narrow Windows large-text clipped-caption evidence. Exercise the
real editor with short Latin and admissible long Latin/German/Persian values. Validate full native names, caption
height, 44 px native target bounds inside the actual group and original raw command parameters. Invoke each actual
button, retain the unsaved selected value and remove it from suggestions, then restore original draft/dirty state
and compare full stored Accounts/Entries/Settings without Save. Focus en/fa/de, both themes and 360/412/wide with
process-local 200% and a 100% narrow baseline; include existing action variants after the shared appearance change.
Final native/build/APK evidence and acceptance boundaries are in quality/tag-suggestions.md.

AT-95 local status: final Windows matrix and normal signed Android Release pass; 84 native tag Invoke operations,
21 full original-draft/stored-row restorations, 1,369 main tests and strict Windows/equivalent-command Android builds.
No added unit-case count. Complete signed D-89 APK and preserved fictitious data are verified; canonical PowerShell
script startup, physical OS/screen readers/ARM64/iOS/provider/release acceptance remain separately unverified.

## AT-96 - Detail visibility, current action names and retained drafts (D-90)

Retain the expanded before-caption mismatch and the corrected interpretation of D-89's native helper observation.
Use real native Invoke for hide/show, check initial Simple/Advanced policy, one visible full-caption action, spoken
name, 44 px target and bounded native geometry. Enter fictitious payee/note/tag drafts; compare the editor's actual
complete unsaved-field fingerprint and bound inputs after both toggles. Restore exact original fields/suggestions/
visibility/dirty state and compare complete stored rows without Save. Evidence and independent acceptance limits:
quality/entry-details-disclosure.md.

AT-96 local status: final Windows matrix and normal signed Android Release pass; 48 actual native Invoke
operations, 24 complete draft/stored-row restorations, 1,369 main tests and zero-warning strict builds. Signed
D-90 APK and canonical privacy checks pass. No new unit count; platform/physical-device acceptance stays separate.

## AT-97 - Full restore alternatives and retained onboarding (D-91)

Measure both actual full-caption native targets on steps 1 and 3, Invoke Restore and the restore page's native Back,
check the same page/step, complete account/regional/theme/mode/currency/calendar drafts and complete stored rows.
Final local status: 84 native Windows invocations/42 proof files, signed Android Release en/fa/de round trips and
disposable-profile removal, 1,369 main tests and zero-warning builds. No new unit count or physical-device claim;
evidence and limits: quality/onboarding-restore-actions.md.

## AT-98 - Complete Home snapshots and indexed goal balances (D-92)

Eight collection cases prove one Reset, complete order, fresh equal contexts, self-source, empty source, notification
order, failed enumeration and reentrancy. Seven goal cases prove no-goal short-circuit, one original ledger pass,
currency/transfers/refunds/dates/assets, priority and overflow. Actual bound Home checks retain every native row,
rebind exact fresh values and compare complete stored data. All 1,384 main tests pass; 21 final Windows Home proof
files and normal signed Release large-profile/financial readbacks pass. Evidence/remaining QA-06 gates:
quality/performance-home-snapshots.md.

AT-98 follow-up (D-93): keep each Windows diagnostic call inside the same platform guard as its implementation.
Android Debug must compile as well as Release. Final strict builds and 1,384 tests pass; three current Windows Home
proofs retain rows/contexts/values, and signed Release readbacks preserve all 24 tables of all three owned samples.
No new unit count or cold-start/physical-device acceptance: quality/home-debug-platform-and-native-stages.md.

## AT-99 - Complete Home customization and persisted native actions (D-94)

Measure all eight actual section captions at full native scale, separate controls, 44 px targets and complete Reset
caption/name. Invoke native Down/Up, Toggle visibility and Invoke Reset; prove exact persisted order/visibility,
all default sections and unchanged complete financial rows/other preferences. Final local status: 21 proof files,
84 Windows native operations, full en/fa/de width/theme/200% matrix, 1,384 main tests, zero-warning builds and normal
signed Android Release/readbacks pass. Original development files restore with matching hashes. No new unit count,
real OS font-scale, screen-reader, physical-device or release acceptance: quality/home-customization-readable.md.

## AT-100 - Complete accounts across Home visibility (D-95)

Invoke Customize Home and the native Accounts switch: hidden -> shown -> hidden. Hidden means zero native rows and
no source; the complete VM snapshot remains. Shown means every account in original order, with fresh contexts and
retained native rows after warm reload. Invoke the actual row and Back, then the complete Accounts list from hidden
Home and Back. Restore the original layout; compare complete financial rows and every other preference. Final local
status: 21 proof files/294 native operations across en/fa/de themes/widths/200% and normal text, 1,384 main
tests, strict builds, normal signed Android Release and exact 24-table readbacks pass. Actual native before/after
counts/timings are separately recorded. No new unit count, real OS/screen-reader/phone/iOS/Q-02 or release acceptance.
Evidence: quality/home-hidden-account-views.md.

## AT-101 - Complete account type and status descriptions (D-96)

On the actual bound Home AccountRow, vary only the existing default/excluded/incomplete presentation flags through
all eight combinations. Keep original identity/amount and every account, retain native rows/fresh contexts, count
all shown captions, and check native caret/glyph bounds against native and MAUI width/height. Restore the original
snapshot; compare complete stored Accounts/Entries/Settings/Budgets without Save. Check genuine Accounts page rows.
Final local status: 21 proof files/168 cases, 1083 own-window renders across en/fa/de themes/widths/200% and
normal text, 1,384 tests, strict builds, signed emulator Release/native details/Back and exact 24-table readbacks pass.
Original slot-only false pass is not acceptance; tightened checks reject baseline clipping. No added unit count or
real OS/readers/phone/iOS/release acceptance. Evidence: quality/account-descriptions-readable.md.

## AT-102 - Complete debt entry action and unchanged unsaved draft (D-97)

On the actual Accounts page, check the complete translated caption, realized native glyphs within native target and
MAUI label, complete native name, scaling and at least 44 px target. UIA Invoke opens the existing new unsaved Loan
draft; wait for actual modal date arrangement, capture first view and scroll the reference date into view. UIA Cancel
returns to the same page. Compare full Accounts/Entries/Settings/Budgets/Schedules before and after, without Save.
Final local status: 573 renders, 21 proof files/42 open-cancel invocations, 1,384 tests, strict builds, signed
Release navigation and exact 24-table readbacks pass. Baseline clipping and unarranged-modal diagnostics are negative
evidence, excluded from final counts. No real OS/readers/phone/iOS/release acceptance. Evidence: quality/debt-entry-action-readable.md.

## AT-103 - Single readable modal headers and unchanged drafts (D-98)

Open thirteen named-route cases across all eleven existing modal page types with valid real query inputs.
Verify zero generic headers, one own title, native title/close glyph geometry, complete native peer name,
44 px target, no overlap and same body/context after repeated attachment. Native Close/Cancel returns to
the same Accounts parent/header; native Back succeeds. Compare all nine complete stored data sources.
Final local status: 1293 renders, 21 summaries, 273 native closes/21 parent backs/294 header checks,
1,384 tests, strict builds and signed APK pass. Android normal-scale debt/expense/plan has 18 language/theme
cases; original 24 tables in each of three fictitious profiles remain identical. Negative diagnostics excluded.
Actual OS scaling/readers/phone/iOS and remaining controls remain open. Evidence: quality/modal-headers-readable.md.

## AT-104 - Complete visible plan-field problems and retained corrections (D-99)

Invoke real native invalid Save from the footer: blank name, zero amount and missing destination must all appear.
Automatically reveal the first input; measure full scaled name/amount/destination glyphs. Correct earlier values
then retry with missing destination, same-account destination and zero required cross-currency amount. The next
input must be in the actual viewport; stale captions clear, typed values/notes/reminder drafts and full stored
Accounts/Entries/Settings/Schedules remain. No valid Save or financial posting; restore original presentation choices.
Final local status: 651 renders, 21 proofs/84 invalid-Save invocations, 24 new behaviour cases/1,408 tests,
strict builds and signed APK pass. Six normal-scale Android language/theme invalid-Save/cancel checks and exact
three-profile 24-table readbacks pass. Negative cohorts excluded; other OS/readers/phone/iOS acceptance remains.
Evidence: quality/plan-validation-visible.md.

## AT-105 - Complete visible transaction-field problems and corrected details (D-100)

Invoke actual native invalid Save from the footer, verify simultaneous amount/destination/fee errors, then correct
earlier fields and retry missing/same/cross-currency destinations and destination fees. Reopen invalid collapsed
original-currency/reimbursement fields and clear corrected errors. Check all realized scaled error glyphs and the
actual next-input viewport. Retain complete draft and Accounts/Entries/Settings; restore presentation choices.
Final local status: 1281 renders, 21 proofs/168 invalid Saves, 48 new cases/1,456 main tests, strict builds,
signed APK, six normal Android language/theme invalid-Save/cancel cases and three-profile 24-table readbacks pass.
Negative cohorts excluded; valid Simple destination-fee retention and other platform/control acceptance remain.
Evidence: quality/entry-validation-visible.md.

D-100 final callback review: native Windows callback failures rejoin the awaited action guard; timed-out queued
requests expire. Eight final German invalid Saves/61 renders, strict rebuilt Windows/Android Debug/Release,
one final-APK English Android invalid Save, repeated original 24-table readbacks and final native handoff pass.
The main validation tests are unchanged. Original full-matrix counts and the pre-guard Android candidate remain
distinct from these final checks; evidence: quality/entry-validation-visible.md.

## AT-106 - Successful Simple transfer edits retain existing destination fees (D-101)

Prepare a fictitious two-fee transfer. Invoke actual native Save after an unrelated note change: retain both
expense ids/amounts and the single transfer. Check exact localized source/destination net effects. Reopen/change the destination fee with the same id, then explicitly
clear/zero it. Reopen fee-free and new forms; verify Simple/Advanced creation policy and Cancel/Discard without
writes. Compare all financial fields and unrelated rows, allowing only intended changes and audit updates.
Final local status: 24 Windows contexts/72 valid Saves, native input/glyph checks, 1,456 tests, strict builds,
signed APK, 12 normal Android new-form checks and original three-profile financial/preference values and expected settings audit pass.
Successful stored-fee edits on Android/physical/iOS remain unaccepted. Evidence: quality/destination-fee-retention.md.

## AT-107 - Visible advance-settlement fields and safe actual differences (D-102)

Use a fictitious plan/advance. Invoke native Save with reversed dates and an invalid bill; see both errors and
the first affected input. Correct each, check empty/negative bills and no-advance periods; retain typed dates/text
and complete stored values. An exact bill writes nothing. Invoke real Save for extra payment, partial refund and
zero bill; check only intended entries, linked refunds, original fields/ids and unrelated rows. Restore the
fictitious scenario between Saves and preserve/restore original development files.
Final local status: 14 application cases, main 1,470/App.Tests 234; 24 Windows contexts/144 invalid and 72 valid
Saves; six normal-scale complete-Release emulator contexts/18 invalid Saves; strict builds, signed APK and three
profile financial/preference-value checks pass. Real OS/phone/iOS/readers and valid Android settlement acceptance
remain open. Evidence: quality/settlement-feedback.md.

## AT-108 - Complete due-item actions and safe correctly placed feedback (D-103)

Invoke invalid completion/partial/change actions in a fictitious occurrence. Check full native action captions,
names/targets, inline errors next to the attempted input and first-input visibility. Correct each independently;
retain the other draft/error and complete stored data. Invoke valid positive/omitted override saves: only the
intended new occurrence state changes, never ledger entries. Invoke a partial payment followed by completion:
exactly intended entries and the unique settled state, with every original financial field/id preserved. Restore
only identified fictitious scenario rows and verify the exact pre-state. Local final status: 14 application cases,
main 1,484/App.Tests 248, 24 Windows contexts/96 invalid and 96 valid native Saves, a separate 24-context/96-invalid
final contextual-message review, six normal-scale Release emulator
contexts/18 invalid Saves, strict builds, signed APK and original-data readbacks pass. Real phone/iOS/readers and
valid Android occurrence acceptance remain open. Evidence: quality/occurrence-feedback.md.

## AT-109 - Complete transaction presentation reuse (D-104)

Four application cases verify 100,000 complete ordered sources across filters, fresh same-id rows, independent
selection and retry after presentation failure. Main suite: 1,488 passed, App.Tests 252. Real Windows review compares
actual cached row fields against EntryPresenter, proves unit invalidation/fresh reload and no fictitious financial
writes; existing native selection/cancel/retry remains. Final matrix/Release emulator/APK evidence and open QA-06
limits: [quality/transaction-row-reuse.md](quality/transaction-row-reuse.md).

## AT-110 - Complete bulk-selection reload (D-105)

Three new cases exercise the actual bulk flow with 10,000/100,000 complete sources, fresh reversed objects,
removed/new/unknown ids, no write or Undo, and retained selection operating on fresh money/tags. Existing financial
and busy-dialog scenarios remain. Main suite: 1,491 passed, App.Tests 255. Actual native Select all/reload/Cancel
review and final matrix/APK/open limits: [quality/bulk-selection-reload.md](quality/bulk-selection-reload.md).

## AT-111 - Exact Settings suggestion history (D-106)

Twenty-one actual snapshot/SQLite cases compare complete and required history across calendars/pay-cycle starts,
boundaries/refunds/negative net, account/currency/history rules and fresh contexts, with complete stored rows retained.
Main: 1,512 passing/App.Tests 276. Bound Settings captions, loading/retry, live choices and unsaved estimate review,
full builds/APK and independent performance/platform limits: [quality/settings-suggestion-history.md](quality/settings-suggestion-history.md).

## AT-112 - Growing native selected and popup picker text (D-107)

Check actual selected/popup glyph bounds, matching inherited native fonts/scaling, complete semantic names and
44-unit targets. Settings reselects existing choices through real native SelectionItem, retaining raw estimate/period
drafts and complete stored Settings/Accounts/Entries without Save. Reopened language review separately changes choices
and preserves retired navigation/form behavior. Other picker pages require shared-template coverage. Main suite:
1,512 passed, App.Tests 276. Final 21 contexts/2083 own renders, 189 native existing choices,
84 language choices/returns, strict builds, original-data readbacks and complete signed APK pass:
[quality/native-picker-captions.md](quality/native-picker-captions.md).

## AT-113 - Complete report-scope controls and transaction results (D-108)

Use a fictitious query through the real transaction route. Check every complete scope/category/date caption's native
glyph bounds and the real Clear target/name inside its scrolled native viewport. Scroll the original result through
native UIA, invoke actual Clear, restore the exact query and compare complete result captions and stored Settings,
Accounts, Entries and SavedFilters without Save. Check ordinary/expanded filters and Add/bulk/Undo docks independently.
Final 21 contexts/1615 own-window renders, 63 complete scope captions/Clear targets and 21 native
result-scroll/Clear/query-restoration checks pass. Main suite 1,512 (App.Tests 276), strict builds, actual normal
Release Reports/Transactions/Clear, exact 24-table comparisons in three fictitious profiles and signed APK pass:
[quality/transaction-scope-readable.md](quality/transaction-scope-readable.md).

## AT-114 - Independent saved filters after report scopes (D-109)

Invoke the same real named filter before and after independent currency, account-set and confirmed-only report
restrictions. Each report must change its fictitious result; the native saved action must restore identical complete
rows and remove the old scope note. Compare complete Settings/Accounts/Entries/SavedFilters after every action.
The unreviewed fixture belongs only to the explicit Debug route, prepared before the checkpoint. Original developer
files are restored with matching hashes. Six native contexts/18 independent report restrictions, main 1,512
tests (App.Tests 276), strict builds, actual Release named-filter/report application, exact original-data readbacks
and signed APK pass: [saved-filter-report-scope.md](quality/saved-filter-report-scope.md).

## AT-115 - Complete unchanged native transaction sources (D-110)

Compare all group captions/boundaries and ordered row identities, including 100,000 rows and mutable selection.
Reject changed captions, ordering, counts and fresh equal-id rows. On the actual bound page, equivalent searches
retain MAUI/native sources; changed results and data/display snapshots publish fresh sources. Compare complete
result ids/order and stored rows; restore original developer files. Nine new cases pass (main 1,521/App.Tests 285).
Final runtime/platform/package checks: [quality/unchanged-transaction-source.md](quality/unchanged-transaction-source.md).

## AT-116 - Repeat from a new transaction draft (D-111)

Fourteen behavior cases pass, covering supported kinds, rule calendars, first date/minor units, detached values,
safe defaults and invalid handoffs. Native review invokes real Repeat/Cancel on expense, income and transfer;
checks selected dates/monthly preview, copied accounts/amounts/note, complete original draft fingerprints and
unchanged stored data without Save. Invalid amount must remain beside its field without navigation. Check action/
spoken name/44-unit target, all six translations/help and both modes; restore developer files after failures too.
Main 1,535/App.Tests 299; 33 financial contexts/99 handoffs, separate 33 focused presentation contexts/99 targets,
strict builds, normal signed Release explicit Plan Save/cleanup and exact original 24-table readbacks pass:
[quality/entry-repeat-draft.md](quality/entry-repeat-draft.md). Phone/iOS/OS-reader acceptance remains open.

## AT-117 - Complete native transaction category choices (D-113)

Review short, long translated and 98-character single-word fictitious choices in the actual entry form.
Check complete native glyph/slot/group/viewport geometry, 44 px targets, scalable text, full names and unchanged
raw-object command. Invoke real native buttons, retain only draft selection, restore complete private state and
compare complete stored rows without Save. Windows 33 contexts/99 selections pass. Normal signed Android Release selection/discard and exact 24-table original-data readbacks pass; physical-device/iOS/OS-reader acceptance remains open: [evidence](quality/entry-category-captions.md).

D-115 clarifies the separate empty-form scroll limitation: a current IME window covers the failed gesture origin.
The same blank draft and gesture scroll to complete Title/date after native Back removes the keyboard; no typing
or Save. No production correction is needed for this hypothesis. This is not last-position category acceptance.

## AT-118 - Single complete savings-goal warning packet (D-116)

Before: the actual native 360 px/200 % card renders its combined funding/overdue packet twice. After: one complete
packet in each list template, unchanged native scaling, semantic color, raw command/row and full spoken name.
Scroll through the actual viewport and check realized glyph/slot bounds, including Persian display digits.
Restore original presentation collections/flags and compare complete financial/settings/goal/plan rows without Save.
Windows 27 contexts/54 packets pass; full delivery and separate platform boundaries:
[quality/goal-warning-packets.md](quality/goal-warning-packets.md).

## AT-119 - Final commercial model without enforcement (D-117 / approved OD-03)

76 Core cases cover the full valid product/duration x Simple/Advanced x personal/shared matrix and every feature;
unknown/impossible products, inclusive start/exclusive expiry/revocation and enduring Lifetime fallback; guest grants
limited to the exact accepted space; existing-data rights after host expiry; Free and paid quota boundaries/batch
overflow; actual paused/archived/completed/ended states; stable budget definitions across months/account ordering;
separate profile/device/identity/space counts; six owner/member/pending seats and expiry/withdrawal. Main suite 1,611
passes. No UI/store/enforcement claim; no purchase data enters backups. [Evidence](quality/entitlement-policy.md).

## AT-120 - Account writes inside the quota transaction (D-118)

15 real SQLite cases verify unrestricted inactive registration; Free's fourth active account rejection with complete
original metadata/no Changed; retained over-quota corrections/archive; unarchive and freed capacity; unlimited
Plus/Pro and exact shared access; expired host/missing membership; wrong-file/case-variant and changed snapshots;
required-column write failure/rollback followed by a successful final-slot write; independent-provider contention
with both contenders captured before either acquires the writer; and unchanged currency locking/ledger data.
Main suite 1,626 passes. Other ENT-02 creation/import/restore/native paths are not accepted by these account cases.
Final platform/APK and actual inactive Release form evidence: [quality evidence](quality/account-write-policy.md).

## AT-121 - Template/filter capacity and atomic replacement (D-119)

24 real SQLite cases cover inactive unlimited behavior; Free creation rejection before sort/audit/Changed;
complete other metadata preserved during over-quota edits; freed slots; Plus/Pro/exact active shared capacity;
confirmed case-insensitive trimmed-name replacement and same-id/name collision; database-trigger failure with
full original-row rollback/retry; expired-host corrections and missing-membership rejection; independent-provider
final-slot contention with correct sort order and one Changed. Main suite 1,650 passes. Final strict platform/APK
and actual unrestricted Release save paths: [quality evidence](quality/template-filter-write-policy.md).
Other ENT-02 resources/import/restore/read-only/native routes and release activation are not accepted by these cases.

## AT-122 - Goal/plan slots, whole batches and continuation history (D-120)

45 new real SQLite cases cover inactive above-Free creation; paused capacity; retained over-quota edits/end;
completed/archived goal reopening before pin/protection changes; ended-plan history and reactivation/freed slots;
Plus/Pro/exact accepted shared unlimited capacity; whole-batch rejection and net-slot input order; split/resume
at and above quota with complete early/late ledger metadata; required-column failure/retry; missing membership;
independent-provider final-slot contention; new advanced goal kinds and eight advanced plan tools; retained-tool
correction/split; expired-host corrections; new automation rejection; rollback after occurrence relocation;
conflicting split false/no writes; historical paid metadata corrections without enabling work. Main suite 1,695
passes. Final platform/APK and actual unrestricted Release editor checks: [evidence](quality/goal-plan-write-policy.md).
This does not accept contribution/allocation/occurrence automation, other ENT-02/03 paths or release activation.

## AT-123 - Current budget definitions and atomic limits replacement (D-121)

26 real SQLite cases cover inactive unrestricted saves; second current-definition rejection with complete no-write
comparison; canonical account order/currency case and three calendars; financial month start; historical/future
copies; stored identity against ignored incoming changes; retained over-quota corrections/replacement; unlimited
paid/exact shared scope; six new advanced tools; retained paid/history corrections; replacement rejection before
deletion; triggered new-limit failure with full rollback/retry; missing membership and expired-host rights; no
Settings creation/ledger writes; independent-provider final-slot contention. Main suite 1,721 passes. Final
strict platform/APK and actual unrestricted Release budget Save: [evidence](quality/budget-write-policy.md).
This does not accept explicit read-only selection, future activation, import/restore or release activation.

## AT-124 - Holding rights and complete atomic purchase Save (D-122)

46 real SQLite cases verify ten new-work rejections with complete no-write comparison, paid/exact shared and inactive
purchase Save, atomic payment/fee/derived price and correct money classification; latest same-date price metadata;
standalone Purchase-source price rejection; retained Free/expired-host corrections; reasoned no-money quantity
correction and six disguised-work rejections; ten missing-membership write paths; default-location scaffolding;
Free idempotent import and no revived paid right; complete delete/Undo; price insert/update failure; access retired
by a real SQL trigger before commit; independent-provider enabled/inactive sale contention; simultaneous duplicate
imports; conversion failure/rollback/retry without losing the legacy account. Main suite 1,767 passes. Final
strict platform/APK and actual Release purchase editor path: [evidence](quality/holding-write-policy.md).
This does not accept explicit selected read-only classification, other ENT-02/03 paths or release activation.

## AT-125 - Retained earmarks and atomic complete goal editor (D-123)

39 real SQLite cases cover inactive complete Save/earmark without ledger posting; positive Free rejection with
complete stored equality; Free/expired-host releases, removal and deletion; eight missing-membership paths;
basic Persian-calendar empty date scaffolding; seven new contribution/reminder rejections before draft mutation;
retained metadata/identity and reminder corrections; two new-method/date-tool rejections; three paid/exact shared
paths; retained-work/reminder reopening rejection and paid reopen; basic empty-date reopen; complete goal quota;
contribution insert/update failure and retry; actual SQL-trigger access retirement; whole delete rollback;
independent last-slot contention; existing/new contribution row contention; historical correction without
activation. Main 1,806 pass. Strict platforms, full APK and actual Release form/allocation evidence:
[evidence](quality/goal-contribution-write-policy.md). Selected read-only/new automation/limit UI remain open.

## AT-126 - Actual-file recovery and retained import rights (D-124)

28 new real SQLite cases verify reproduced/corrected profile redirection; inactive/Free/expired-host over-quota
imports and restarted Undo; four missing-membership boundaries with no input/output/row changes; four over-quota
backup/restore contexts preserving all columns and unchanged paid facts; two access changes during input;
bound-file rights after a profile move; two SQL-trigger retirement rollbacks; two journal failure rollbacks;
two independent duplicate-import contests; wrong-file snapshots; corrupted/cancelled input; native-copy/output
access rechecks; preservation of other registered sources; and first-schema restore/migration into the initial
file after a profile move. Main suite 1,834 passes. Strict platforms, full signed APK and actual linked import/Undo:
[evidence](quality/recovery-write-policy.md). Multi-source atomicity and selected read-only classification remain open.

## AT-127 - Exact-file category rule writes and serialized replacement (D-125)

28 added real SQLite cases cover reproduced/corrected Free new-rule acceptance; unrestricted inactive writes;
three paid/exact shared creation contexts; retained correction/delete and same-pattern new-id replacement in
Free/expired-host contexts; new text/kind refusal and distinct kind identity; four missing-membership paths;
two wrong-file paths; four trigger-driven access retirement rollbacks; two replacement SQL failures/retries;
two independent same-pattern writer races; bound initial file after provider movement; and short draft validation.
Main 1,862 pass. Full stored-column checks keep unrelated financial/preferences data unchanged. Strict platforms,
complete signed APK and actual Release rule form evidence:
[evidence](quality/category-rule-write-policy.md). Other ENT-02/03/04 paths and suggestion delivery remain open.

## AT-128 - Exact-file ledger Save, complete groups and final refunds (D-126)

45 added real SQLite cases cover split rejection before draft mutation; inactive split/join metadata; paid/shared
creation; retained corrections/join after expiry; additional/relocated/incremental parts; transfer/holding group
classification; four missing-membership overloads; corrective movements versus new work; wrong-file and bound-file
checks; SQL/access retirement and attachment rollback/retry; invalid batches; independent same-id and refund
writers; four reproduced refund defects; valid original/refund batch order, edits/replacement, missing targets,
Int64 boundaries and occurrence paid-total rollback/retry. Compare complete columns of all 24 tables.
Main 1,907 pass, App.Tests 299 unchanged. Platforms, APK and actual normal Release flow:
[evidence](quality/ledger-write-policy.md). Commercial deployment and remaining ENT-02/03/04 stay open.

## AT-129 - Original-file deletion and retry-safe short Undo (D-127)

29 added Data cases cover three reproduced refund defects; retained receipt bytes/metadata/ownership; five owned
data contexts; four membership paths; two wrong cached files; returned-batch wrong profile; two initial-file moves;
four SQL/post-SQL retirement rollbacks/retries; two independent writer races; whole split deletion/recovery; and
four derived occurrence failures. Nine actual app-source cases cover failed-action deadline/retry, old completion/
failure with replacement or dismissal, double taps, expired failure, observer failure and actual SQLite refund Undo
failure/retry. Main 1,945 pass, App.Tests 308. Strict platforms/full signed APK and actual Release flow:
[evidence](quality/ledger-undo-write-policy.md). Other ENT-02/03/04 and external acceptance remain open.

## AT-130 - Actual bound Undo command failure and native feedback (D-128)

Nine added application cases: original failure/offer/no refresh; success; duplicate invocation during action and
refresh; pending failure dialog with replacement; post-commit refresh failure; expiry; fatal memory propagation/
guard recovery; real SQLite failed Undo and complete refund/receipt retry with all 24 tables compared. Main 1,954,
App.Tests 317 pass. Strict Windows/Android, full signed APK and actual installed en/fa/de light/dark failure dialog,
dismissal/navigation and unchanged committed deletion: [evidence](quality/transaction-undo-feedback.md).
Native retry within the short deadline, phone/iOS and other commercial paths are not inferred from these checks.

## AT-131 - Immutable short Undo and later financial state (D-129)

17 Data cases cover three retained refund financial edits, account currency, returned row/tag mutation, four
metadata edits, deleted/recreated refund identity, explicit relinking, transfer destination currency, unchanged/
changed partial group recovery, post-completion no-op and renamed/archived-account recovery. One actual command
case verifies stale-refund feedback, no financial write/refresh, original deadline and subsequent expiry.
Main 1,972 / App.Tests 318 pass. Strict platforms/full signed APK, normal installed Delete/Undo and installed
stale-refund guard with unchanged committed rows/receipts: [evidence](quality/ledger-undo-conflicts.md).
Native editor timing, physical device/iOS and remaining commercial paths are not inferred from the fixture.

## AT-132 - Transaction Save failures visible without searching (D-130)

The actual Windows modal/native Invoke checks a purchase correction below its retained refund, explicit retry,
exact sample SQLite update failure, and subsequent input validation clearing the obsolete SaveError. At both
native body scroll ends, check full realized glyphs/spoken name and the enabled original Save target of at least
44 px; compare reviewed draft fields and all persisted rows/receipt bytes. The installed full Release repeats these
paths, cancellation/discard and exact owned-data restoration. [Evidence](quality/entry-save-feedback-visible.md).
No duplicated editor algorithm or control fake is introduced. Device/iOS/screen-reader acceptance remains open.

## AT-133 - Explicit resource choice and retained actual-file settlements (D-131)

24 Core cases cover counted pauses, scoped identities, no guessed selection, empty/smaller/stale/oversized choices,
upgrade restoration, immutable canonical snapshots and feature/membership separation. 55 actual SQLite cases cover
source/destination selection, complete-batch rollback, retained corrections/fees/Undo, verified open overdue full/
partial payments, invalid schedule-marker rejection, reviewed advance differences, stale reviews, SQL failure and
cached selection retirement after SQL. Main 2,051 / App.Tests 318 pass with no failures/skips.
Actual native Save failure/retry/stale-review and successful bill flows, strict builds and complete signed APK:
[evidence](quality/selected-account-write-policy.md). Limits remain inactive; no complete downgrade UI, other resource
bindings, automation, physical-device/iOS or release acceptance is inferred.

## AT-134 - Actual retained manual debt closure (D-132)

51 actual SQLite cases/main 2,102 pass, App.Tests 318. Verify known Loan/Lent baselines, exact multiple payments/
foreign amounts/fees, selected counterpart new-work rights, no manufactured closure, complete 24-table rollback,
retired cached access, explicit retry and independent competing writers. Installed normal repayment flows prove
actual app delivery independently from enabled commercial service tests. Current registration stays inactive;
remaining commercial/native/external gates are open. [Evidence](quality/retained-debt-closing.md).

## AT-135 - Atomic explicit linking and retained occurrence corrections (D-133)

39 actual SQLite cases/main 2,141 pass, App.Tests 318. Six missing-membership writes reject with all 24 complete
tables unchanged; twelve Free/expired-host operations retain rights without account selection. Verify state insert/
update failure rollback, complete metadata preservation/idempotent links, nine changed relationships, transfer
destination, five actual post-SQL access retirements/retries, no-op repair/history deletion, wrong-file facts and
independent writers assigning one payment only once. Normal installed en/fa/de light/dark explicit confirmations
retain actual 950 minor units, complete tags/notes/payee/receipts and one settled state without a second entry.
[Evidence](quality/occurrence-correction-writer.md). Strict builds/full signed APK do not complete all commercial/native/external gates.

## AT-136 - Atomic new planned payments (D-134)

16 actual SQLite cases compare complete 24-table snapshots on state failure, invalid inputs, changed actual
relationships and cached-access retirement after entry SQL, then retry without duplicate money. Cover old form
partial balance, actual changed/moved amount with retained metadata and two independent writers competing for
full/partial payments. One final settlement, correct prior paid total and preserved actual amount/date.
Main 2,157 pass, App.Tests 318. Normal Release en/fa/de light full Confirm and dark partial then final Pay part
are checked through native inputs/actions, plus complete stored rows. [Evidence](quality/atomic-occurrence-payments.md).
Strict builds and emulator evidence do not complete physical-device/iOS/production acceptance.

## AT-137 - Exact reviewed settlement reopening (D-135)

15 new SQLite cases: stale reviewed payment identity, changed actual link, no second write after committed
Changed retires scope, two SQL state failures, two membership rejections, four retained Free/expired-host paths,
two actual post-SQL access retirements/retries, complete refund/receipt Undo and earlier partial money/overrides.
Compare all 24 complete tables for rejected writes; Undo comparison excepts only the explicitly asserted retained
automatic-post suppression flag. Main 2,172 pass, App.Tests 318. Six normal Release en/fa/de light/dark explicit
native reopening confirmations and actual stored-row proof: [evidence](quality/atomic-occurrence-reopening.md). Device/iOS acceptance remains open.

## AT-138 - Rejected occurrence commands retain drafts (D-136)

Full/partial payment, explicit linking, Skip/Unskip, date/amount/note changes and reopening use actual
OccurrenceCommands with the existing PlanStore writers. One gate spans native consent, writer failure feedback
and successful display publication. Rejection/cancellation never reloads, navigates or replaces the draft; ledger
validation stays inline. Unexpected failure uses the native interaction port without raw user-facing exceptions.
Retire the writable reviewed snapshot before post-commit navigation/refresh and publish its replacement only after
all load reads finish. A refresh failure explicitly says the change was saved and requires closing/reopening the
screen before another change. Explicit Reopen/Cancel captions exist in all six languages; French/Italian no longer
present two cancellation captions. No commercial activation, schema, SDK, permission, backup or device-setting change.
29 added actual application-flow cases (AT-138), App.Tests 347. Main 2,201 pass with an independently owned,
ignored Data.Tests output configuration limiting collection concurrency to one. Explicit writer-race tests retain
their concurrent operations. Two normal parallel full-suite attempts failed in unrelated Data.Tests SQLite cleanup/
migration (locked file and disposed native handle). Preserve negative logs; D-137 later completes file-scoped pool cleanup isolation.
An unsupported argument ran zero tests and a help invocation failed; neither is acceptance. Output-only test
configuration was removed; no product workaround or test execution policy was committed.
Strict Windows/Android builds and complete signed Release APK verified. Native normal Release emulator checks cover
en/fa/de, light/dark, actual error dialogs, retained drafts and all 24 complete stored tables. Six additional normal
Release cases commit an actual partial payment, deliberately break only the owned fixture read, display the saved-
but-not-refreshed message and reject replay from enabled native actions. No Windows native
command, real phone, iOS, provider or publication acceptance claim. Selected-plan generation/automation, choice
persistence/UI, commercial activation and existing external gates remain open.
[Evidence](quality/occurrence-command-feedback.md).

## AT-139 - Independent profile and fixture SQLite pools (D-137)

Profile switching and inactive-profile deletion retire only the named file's known pools; restore clears the
exact captured destination connection before migrating that same context. Other SQLite files and active profile
sessions stay independent. Fixture cleanup uses the same production helper only for existing databases under its
unique owned directory, after disposing its providers. Default/factory/read-write/read-only strings are covered;
arbitrary connection-string variants remain their owner's exact-pool responsibility. No process-wide clearing,
collection serialization, schema, permission, backup format, commercial activation or visible layout change.
Nine added AT-139 cases use real TEMP-table session markers and actual profile/backup services. The original
MoveTo/restore behavior lost another database's marker; negative baseline retained. Main 2,210 / App.Tests 348
pass with the normal parallel runner and explicit writer-race tests unchanged (58.337 s; no output runner config).
Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified. Normal Release
English/light emulator profile creation/switch, first-run restore from an actual production-service package, return
and inactive-profile deletion succeed. All 24 restored tables are compared: financial rows and other metadata match the actual snapshot; only the existing
onboarding-completion Settings.UpdatedAt audit write differs. Original profile
files/sidecars, complete rows and English/original System theme are restored. Temporary helpers remain ignored.
Release correctly denied the initial inspection-only run-as inventory; the helper was corrected to inspect only an
unlaunched Debug package. One stale prompt-coordinate attempt cancelled before Save; later input used the actual
focused field. Transient hierarchy misses resumed the same created profile without replaying creation/restore.
The observed native document provider was checked before selecting the exact owned file. Negative helper/capture
logs are retained; unsuccessful attempts are not acceptance. One early nested fixture probe used a noncanonical mixed-separator connection string and failed cleanup; it was
corrected to the application's full-path convention before the successful full run. No Windows native, physical
phone, iOS, provider, 200-percent layout or publication acceptance claim. Planned entitlement bindings/automation,
choice persistence/UI and existing external gates remain open.
[Evidence](quality/local-sqlite-pools.md).

## AT-140 - Actual automatic payment writer (D-138)

Automatic payment discovery is only a candidate list. Recheck the stored active rule/slice, actual open state,
linked partial/full money, current amount/due date and accounts inside the same SQLite writer as ledger Save.
Manual partial money, including a legacy incorrect paid cache, prevents another automatic full payment. Removed,
paused, skipped, moved-future or already settled candidates post nothing. Use current overrides/account routing;
transfers remain one entry and automatic money stays unreviewed. Exact cached membership, selected plan and
AdvancedPlans rights apply when enforcement is enabled; choices grant no paid rights. Current registration stays
inactive. Snapshot plan choices are immutable, scoped and in memory only; no selection persistence/UI, schema,
backup format, SDK, permission or visible layout change.
31 added AT-140 actual SQLite cases; main 2,241 / App.Tests 348 pass with normal parallel execution (67.124 s).
Strict Windows/Android builds: zero warnings/errors. Complete signed Release APK verified. Six normal Release
emulator foreground flows cover en/fa/de, light/dark, actual posting/override/transfer and partial/paused/disabled
blocking. All 24 unrelated complete tables match; exact original sample files/sidecars and English/System theme
are restored, then the original Main profile is reopened. The first native assertion inspected an inactive sample
while Main was open; its fixture runs through actual services successfully. Selecting the exact owned sample
through native UI resolves the review setup. Transient hierarchy misses resume the same fixture; a local helper
syntax error executes no native action and is corrected. Preserve these negative attempts; they are not acceptance.
The original source reproduces automatic full money after independent manual partial payment; the final writer
prevents it. One expanded test initially expected a result instead of the existing missing-membership exception;
the corrected test retains the rejection. No Windows native, physical-phone, iOS, provider, performance or release
acceptance claim. Other resource bindings, generation/reminders, choice UI/persistence and external gates remain open.
[Evidence](quality/automatic-occurrence-writer.md).

## AT-141 - Selected future plan occurrences (D-139)

Project actionable future occurrences through exact-file scoped plan choices while keeping complete original
schedules/states for history and every forecast/budget/KPI calculation. Preserve open effective past-due payments;
today/future new money checks actual rule/slice/kind/accounts and selected/paid recurrence rights inside the same
ledger writer. Dated ended slices inherit only their actual unique non-overlapping continuation. No invented choices
or stored pause/end changes. Preserve account rejection feedback. Registration remains inactive; no layout or new resource key,
schema, backup, permission or SDK change. Core reminder eligibility is available; actual reminder/snooze delivery,
choice persistence/UI, remaining selected bindings and external acceptance gates remain open.

Profile/onboarding root replacement also reuses D-82 navigation retirement before detaching old Shell handlers;
initial native hot/cold language failures are negative evidence. Final Main/QA03 switching and hot language changes
are checked in the normal signed Release, without a locale override.

41 added AT-141 cases (19 Core / 22 real SQLite); normal parallel main suite 2,282 / App.Tests 348 pass, no failures
or skips (59.741 s). Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified.
Six actual normal Release emulator flows cover en/fa/de and light/dark: Home due count 11, six plans in Due/Upcoming/All
and detail future dates/settled history. Current inactive build remains unrestricted above Free's five-plan allowance.
No financial Save during review; all 24 complete prepared tables match. Exact original sample files/sidecars and
English/System plus the original Main profile are restored. Physical-phone, iOS, Windows native, provider,
performance and publication acceptance remain open. Old generic full/partial entry bypass reproduces two failures.
Initial invalid enum/runner attempts and six account-feedback ordering failures remain negative evidence; final
implementation retains established rejection order and the complete canonical suite passes.
[Evidence](quality/selected-plan-occurrences.md).

## AT-142 - Actual scoped Plan notification delivery

48 added cases: 16 Core routing/identity regressions and 32 real SQLite application flows. Cover delivery/contract
rights, explicit choice/no choice, fresh privacy/language/amount/moved date, original group narrowing, closed/paused/
removed/changed/disabled members, legacy cache retirement, wrong-file actions, stable repeat deadlines, permission,
retirement cancellation before and after native publication, membership loss and post-unlock tap routing. Main parallel suite 2,330 /
App.Tests 380 pass, zero failures/skips. Running native evidence and external limits: [quality evidence](quality/selected-plan-reminders.md).

## AT-143 - Durable scoped resource choices

44 added cases: 42 actual Data SQLite and two actual ReminderService flows. Cover persistence across independent
providers, complete non-choice row equality, explicit empty/no-op choices, duplicates/capacity/foreign/historical
ids, retired original list/rights/scope/file/revision and ABA, independent concurrent writers, SQL/post-SQL access
rollback/no Changed, unlimited/inactive retention, exact shared membership, actual new money/retained corrections,
atomic new-slot joining/release/split, malformed input/repair, scoped reminder narrowing and native-await retirement.
Main normal parallel suite 2,374 / App.Tests 382 pass with zero failures/skips. Native/package and external limits:
[quality evidence](quality/durable-resource-choices.md).

D-141 final strict Windows/Android builds have zero warnings/errors. Normal installed Release applies the additive
migration with all 23 complete preexisting non-migration tables unchanged, an empty choice table and six original
plans still available above Free capacity. Complete signed local-test APK/package verified; enabled chooser UI,
physical-device, iOS, provider and publication acceptance remain open. See the D-141 quality record.

## AT-144 - Reviewed account and Plan form

25 actual application-flow cases with real isolated SQLite/localization: construction/covered publication and a
pending read, inactive/unlimited availability, over-capacity draft feedback, history eligibility, explicit empty
and nonempty Save, exact no-money row equality, stale name/currency/type/Plan-kind/rights or SQL failure, six live
languages, accepted/rejected kind and reload discard, consent/navigation busy guard, committed navigation failure
retirement and covered read failure/retry. Main 2,399 / App.Tests 407 pass with no failures/skips. Actual native
form and registered Settings/Help/Back matrix plus six normal Release emulator cases: [evidence](quality/active-item-chooser.md).

## AT-145 - Durable goal choice and writer

31 real SQLite cases cover inactive original states, scoped single/empty choice, reopen/no-op without financial
writes, excess/foreign/historical ids, stale choice/name/type/currency/state/rights/scope, atomic creation/counting
pause, completion/archive/reopening, unselected resume with retained corrections, explicit deletion, SQL/plan/delete
and post-SQL rights rollback, malformed value/revision with retained releases and repair, independent last-slot
contention, exact-file switch, immutable scoped cached fallback, unlimited upgrades and no paid rights from choice.


31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](quality/goal-resource-choices.md).

## AT-146 - Actual reviewed goal-form flow

18 real application-flow cases cover unrestricted original states, distinct completed/paused/quantity metadata,
one-slot feedback, explicit empty/paused Save with all financial rows equal, six live language changes, stale
rights/name/type/currency/SQL failure retaining the exact draft, accepted/rejected goal-kind discard and explicit
malformed-choice repair. No duplicated application algorithm or fake UI controls.


18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](quality/goal-chooser.md).

## AT-147 - Actual goal work, publication and original reminder routing

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

Exact choices and paid rights stay separate; all original financial inputs remain. Parameterized concurrent state/rule/allocation writes actually change rows, then retire native publication. Locked taps resolve original single/group members after unlocking and reject another file even with a restored identical Guid. Retained corrections/disabling and paired new-goal joining are exercised under actual writers.

[Evidence](quality/goal-work-reminders.md). ENT-02/03 and external gates remain in progress.

## AT-148 - Original goal views and reviewed detail actions

36 new AT-148 actual-source/real SQLite cases pass; final normal parallel main 2,520 / App.Tests 497 pass,
zero failures/skips, test output cleaned. Strict Windows and Android builds have zero warnings/errors. Actual list,
detail and Home pass 18 English/Persian/German, light/dark, 360/412/wide cases with process-local 1.5 font stress;
18 additional focused cases check the corrected neutral earmark/release explanation. Native glyphs, original tones,
44-pixel action targets, original historical Release/account, dirty price/currency feedback and actual retry/Back are
checked. All 25 complete fixture tables remain equal; original development files/sidecars and display are restored.

Six normal signed Release owned-emulator cases verify original Home/list/detail, completed goal and archived funding,
initially selected Release, enabled Save and native Back. One explicit negative earmark release is saved; complete
original goals, accounts, ledger and other rows remain equal. Only the pre-existing D-141 migration and that one
expected owned allocation differ. Original owned bytes/sidecars plus Main/English/System are restored; the final
normal signed Release is installed/stopped. No physical-device, iOS, OS accessibility, provider, startup performance,
Store signing/publication or commercial-activation acceptance is implied.

Actual-source cases cover complete original financial packets/pins, unavailable positive work, historical releases, over-release rejection, consent cancellation/busy serialization, changed file/rights/choice/metadata/units, committed reload failure, price correction, numeric culture/currency drafts, paused choice and historical reopening.

[Evidence](quality/goal-work-views.md).

## AT-149 - Reviewed goal editor and explicit budget cuts

40 new AT-149 actual-source/real SQLite cases pass. Final normal parallel main 2,560 / App.Tests 537 pass,
zero failures/skips; test output cleaned. Strict Windows and Android builds and canonical Release APK build have zero
warnings/errors. Eighteen English/Persian/German, light/dark, 360/412/wide contexts check the actual Advanced and Simple
modal at process-local 1.5 font stress: complete changed glyphs, real 44-pixel Invoke targets, stale Save rejection,
retained reviewed reload/current saved summary and native Close. All 25 complete fixture tables and original development
data/sidecar bytes are restored. The review uses independent fictitious data and explicit native-effect answers, not
replacement MAUI controls or a production entitlement replacement.

Six normal signed Release owned-emulator cases check the original archived account, uncommon rule, multiple-category
warning, pinned Save and native Close/Back. One actual note-only Save succeeds; complete original rule/categories,
accounts, ledger, allocations and other metadata stay equal. Only that goal note/audit and the previously delivered
D-141 migration differ. Exact owned original bytes/sidecars and Main/English/System are restored; the normal Release
remains installed/stopped. No physical-device, iOS, OS accessibility, provider, performance, Store signing/publication
or commercial-activation acceptance is implied.

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

Evidence: `docs/quality/goal-editor-work.md`.

## AT-150 - Selected budget definitions (D-148)

34 actual Core/SQLite cases pass (15/19), including two independent provider races and complete original-row
comparisons. Main 2,594 / App.Tests 537, strict builds and signed APK pass. Normal installed Release checks actual
budget Save/Delete and unchanged editor in en/fa/de light/dark, with exact owned data/display restoration.
Enabled native chooser/view/alert, physical device and iOS acceptance remain open.
See [evidence](quality/budget-definition-choice.md).

## AT-151 - Actual reviewed budget chooser (D-149)

13 actual App/SQLite cases, including ordinal exact-name/count assertions, pass; main 2,607 / App.Tests 550 and strict builds pass. All 18 Windows native cases
verify complete captions/targets/names, capacity, rejected and canonical successful Save/return with complete
25-table restoration. Another 18 registered Settings/help/Back cases retain complete stored data. Six normal Release emulator cases show inactive original groups and no financial Save;
restore original owned bytes/display. Physical-device/iOS and remaining view/alert work acceptance stay open.
See [evidence](quality/budget-definition-chooser.md).

## AT-152 - Actual budget alert work (D-150)

24 new actual application/SQLite cases pass; main 2,632 / App.Tests 575 and strict builds pass. Six hidden
Windows budget renders and six normal signed Release emulator delivery/tap cases retain complete financial rows;
owned bytes/display restore. Native inflight rights/retraction uses explicit effect ports; actual production
commercial activation, physical/iOS and scoped budget returning-data/view/editor acceptance remain open.
See [evidence](quality/budget-alert-work.md).

## AT-153 - Original scoped budget notifications (D-151)

Budget alerts now carry the opaque actual-file identity and original saved period id. Resolve after unlock and
again in the real budget page; reject another file, deleted identities and failed reads without guessing a
replacement. Retain original currency/calendar/period and monthly navigation coordinates without changing
Home currency or financial rows. Historical/unselected paid methods remain readable without granting new work.
14 new AT-153 cases; main 2,646 / App.Tests 589 pass. Strict Windows and complete strict Android Release pass;
six hidden Windows language/theme cases plus targeted native month navigation, and six normal signed Release
Android cases each tap four exact original currency/calendar/period destinations; owned data/display restored.
Commercial activation stays inactive. Budget view/editor commercial affordances and other ENT-02/03 paths remain
open. See [evidence](quality/budget-reminder-routing.md).

## AT-154 - Reviewed original budget editor and paired writes (D-152)

The original budget editor publishes complete references and defaults behind a loading cover. Exact original
id/file handoff retains archived/missing account/category limits and explicit scopes in both experience modes.
Raw amounts keep their input culture through live display changes. Save rechecks the complete review under the
actual SQLite writer; stale failures retain input and require explicit reload/review. Dirty fields alone survive
reload, unchanged fields adopt current saved values, and a committed form retires before native return.
Current new-tool rights are separate from retained corrections and future-period capacity. Registration remains
inactive. Budget view copy/delete/other work affordances and the remaining ENT-02/03 paths are still open.

35 new AT-154 actual application/isolated SQLite cases pass. Final ordinary main 2,681 / App.Tests 624 pass,
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass
without warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, native
validation/stale Save/Reload/Keep edits/Close and complete 25-table restoration. Six normal Release Android
language/theme cases verify original corrections/archived references; one explicit original-total Save preserves
all other metadata and ledger rows. Final normal Release Create/Close and a targeted weekly Create/Save verify
explicit absent identity and canonical zero stored month coordinates; complete owned data/bytes/display restored.
Physical device, iOS, paid activation and product publication are not claimed.

See [evidence](quality/budget-editor-drafts.md).

### AT-155 - Reviewed original budget view actions (D-153)

Original budget view actions now use the complete displayed BudgetEditorReview for copy, confirmed replacement
and deletion, rechecked before/after confirmation and under the existing actual-file writer. Period lookup uses
original calendar/currency/explicit weekly dates and rejects ambiguity. Viewing, original correction and deletion
remain separate from paid new tools and net current-definition capacity; future periods do not consume today's
slot. Busy spans native confirmation through refresh. A committed operation whose refresh fails requires a
read-only Retry, without repeating Copy/Delete. Read-only opening no longer creates Settings; financial history
and all original calculation inputs remain visible. Current commercial registration remains inactive.

26 new AT-155 actual application/isolated SQLite cases pass. Final ordinary main 2,707 / App.Tests 650 pass
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass with
no warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, full native action
names/targets, retained paid history/correction, changed-confirmation rejection and read-only Retry. Six normal
Release Android language/theme cases verify actual original copy/delete confirmations; exactly one owned copy
and deletion of that copy preserve all original 25-table data. Original owned bytes/sidecars/Main/English/System
are restored, final signed Release installed/stopped. ENT-02/03 and remaining resource/operation/import/restore/
native paths stay open. Physical-device, iOS, provider, paid activation and publication acceptance are not claimed.

See [evidence](quality/budget-view-actions.md).


### AT-156 - Reviewed quick resources (D-154)

Durable explicit quick-template/saved-filter choices retain every original packet, recheck revision/value and
same-timestamp changes, and use the existing actual-file scoped writer. Chosen new slots and explicit filter
replacement are atomic; retained correction/delete/history stay separate. Actual template create/apply/delete
and saved-query save/delete commands use complete reviews and one busy dialog/write/refresh guard. Queries are
captured before prompts; committed refresh failure never resubmits. Template names wrap, publication is covered
and explicit Reload is available. Six growing chooser destinations and help explain retained query history.
Current Release registration stays inactive; no schema, network, SDK, permission or portable grant is added.

29 new AT-156 cases/main 2,736 behaviours (App.Tests 679) pass after repairing one stale four-kind expectation;
only that failed case reran. Strict Windows/complete Android Release have zero warnings/errors. Eighteen hidden
Windows locale/theme/width cases and six normal Release emulator cases restore complete owned rows/bytes/display.
Physical-device, iOS, OAuth, commercial activation and publication gates remain open. ENT-02/03 are still in progress.

See [evidence](quality/quick-resources.md).

### AT-157 - Original Home template destinations (D-155)

Home quick templates publish separate original activity availability after the complete parent read. Explicitly
available later definitions remain reachable within the existing six shortcuts; retained history and all original
financial calculations remain independent. A process-local TemplateVisit carries the reviewed actual-file/full
packet to the existing entry route. A changed/deleted original, reference, choice or right, or another actual file,
is rejected without guessing a newer same-id row or saving money. Failed navigation pins translated feedback and
requires explicit read-only Reload. The actual initial chooser receives only its typed resource kind, never a
selection. Destination feedback is visible beside Save; applying a template remains a detached draft.

Opening Home no longer creates or updates Settings for its layout fallback. Existing saved layouts remain exact;
the legacy device layout is read until an explicit profile-layout Save. Commercial registration stays inactive.
No schema, SDK, permission, network request, portable preference or entitlement grant is added.

21 new AT-157 actual-source/real SQLite cases pass. The ordinary main run passes 2,754 / App.Tests 697;
three focused name-order cases bring the verified total to 2,757 behaviours / App.Tests 700. The changed flow
class passes all 21 cases; the unchanged full suite is not repeated. Pending navigation awaits its actual event.
Outputs are cleaned. Strict Windows and complete strict Android Release have zero warnings/errors. Eighteen
hidden Windows Home/chooser contexts and eighteen focused destination continuations cover en/fa/de, light/dark,
360/412/wide. Six normal signed Release emulator cases open original Home templates, fill original amount/title
and cancel/discard without financial Save. Complete owned rows/bytes/display are restored. Physical-device, iOS,
OAuth, commercial activation and publication gates remain open; ENT-02/03 remain in progress.

See [evidence](quality/home-template-destinations.md).

## AT-158 - Reviewed original device profile catalogue (D-156)

Implemented and locally/native verified: exact empty main identity; explicit later/empty device choices;
inactive/unlimited original access; bounded create capacity; retained original rename/delete/open/backup;
malformed/foreign choice repair; distinct revisions; complete source retirement through prompts/authentication;
one preference Set failure; concurrent service slot validation; covered actual form; saved versus unsaved state;
duplicate-name feedback; busy/discard/reload; committed refresh failure with read-only Retry; live six-language
captions preserving original rows. 33 new behaviours, main 2,790/App.Tests 733 pass. Strict builds, actual native
Windows and normal signed Release emulator evidence: [local profile work](quality/local-profile-work.md).
Per-file financial bindings, commercial activation, physical-device, iOS, OAuth and publication remain open.

## AT-159 - Actual-file original profile work (D-157)

31 implemented actual-source/real SQLite cases cover default zero-read/acyclic DI, exact original main/file
identities and verified financial/resource facts, current-preference independence, missing/empty/foreign/malformed
choices, unknown-file history, actual new-money/account/Plan/Goal/Budget/template rejection, complete retained
financial projections, same-id corrections, refund and overdue settlement, real backup restore, saved-query
history/correction versus new definition, catalogue/provider change-and-return retirement, SQLite writer wait,
shared membership isolation, verified unlimited plans, paid/foreign-fact rejection and original DI lifetime.
Ordinary main 2,821 / App.Tests 764 pass; strict builds, original native pages, normal Release emulator and APK
proof are in [evidence](quality/profile-file-work.md). Native bindings, commercial activation and external
platform/provider/device/publication acceptance remain separate.

## AT-160 - Original catalogue reminder lifetime and period reviews (D-158)

25 cases compile actual source and use real isolated SQLite. They cover inactive/Free/Plus/Pro/unresolved-file
review policy, all six live languages with an explicit independent calendar, actual catalogue choice/rename
refresh, scoped pending/snooze withdrawal without financial actions, retirement inside native publication,
lifetime cancellation and no-read/idempotent registration, foreign-catalogue rejection, native action detachment,
privacy/opt-in/first-date/missing-settings/calendar/day retirement, no Settings recreation and rejected partial
batch cache preservation/retry. Final main 2,846 / App.Tests 789 pass; strict builds, actual native Save/watcher,
normal signed Release SDK evidence and exact restoration: [evidence](quality/profile-reminder-work.md).
Original Plan/Goal financial-input and other operation/native acceptance, commercial activation and external
platform/provider/device/publication gates remain open.

## AT-161 - Original reminder input retirement (D-159)

Original Plan rules/states, Goal/contribution/allocation packets and raw resource choices now retire a
reminder batch when any captured value changes, including changes at unchanged audit times or choice revisions.
Complete original accounts and the date-bounded ledger/quantity inputs used by the existing GoalProgress
calculator are captured in one deferred SQLite reader. Financial data is never filtered by activity selection.
The same coherent reader validates all original packets, preferences and first-data date before/after native
publication; a changed or failed preparation withdraws pending work and preserves the retryable Snooze cache.
Only needed ledger/quantity inputs are read. Future movements remain outside today's calculation.

Snapshot self-mutation rejection belongs to the reminder boundary: existing Goal editors can still mutate their
reviewed draft before the established explicit writer. Full raw selection values distinguish absent/empty
choices. No schema/model/migration, permission, SDK, network, credential or portable preference is added.
Commercial registrations remain inactive and test builds stay unrestricted.

24 new AT-161 actual-source/real SQLite behaviours pass; 100 related reminder/Goal editor/view cases pass.
Final ordinary main: 2,870 passed, zero failures/skips, App.Tests 813, 2m 03.711s; output cleaned.
Strict Windows and canonical complete Android Release have zero warnings/errors. Original running Goals pages
in en/fa/de at 412 pixels/dark verify stale Plan/financial retirement, reached-goal suppression, fresh retry and
complete 25-table restoration. Unchanged layout matrices and successful suites are not repeated.
APK signature/structure and all nine changed production-source PDB checksums match.
Native Release details and retained negative evidence are in the evidence document. Physical device, iOS,
OAuth, paid activation and publication are separate; ENT-02/03 and the commercial release are not complete.

See [evidence](quality/reminder-input-work.md).

## AT-162 - Structured original write feedback (D-160)

Original structured write rejections now explain quota counts, required Plus/Pro/add-on access, exact-space
membership and original profile/resource selection in all six languages. The shared native reporter keeps
unrelated failures on their original path and never guesses purchase, renewal, provider or activation state.
Pre-write Goal/Budget/quick-resource feedback retains the form; committed-return/reload feedback remains
distinct and never falsely says that the accepted write was not saved. Counts use safe Int64 addition,
current formatting/digit choices and direction marks; stored identities and exception details are not shown.

Saving a new filter above capacity now reaches the established reviewed atomic writer for its structured
reason. A rejected operation retires its review; the next explicit menu invocation rereads it without clearing
the current query. Exact raw filter names return to the name prompt. Name drafts are process-local and bounded
by original file identity, independently of rights; another file never inherits them. No automatic retry or
financial save, schema/model/migration, SDK, permission, network, credential or portable preference is added.
Production financial/device registrations remain inactive; test builds stay unrestricted.

19 new AT-162 cases pass with actual sources/resources and real isolated SQLite. Final ordinary main passes
2,889 / App.Tests 832, no failures/skips, 2m 49.493s, followed by cleanup. A preceding main run passed 2,885
before the real second-invocation fix; only the changed final behavior justified another run.
Strict Windows and canonical complete Android Release have zero warnings/errors. Eighteen actual Windows
reason/Close/second-Invoke cases cover en/fa/de, light/dark, 360/412/wide, with unchanged complete 25-table rows
and exact owned development bytes restored. Fictitious menu/name effects are explicit ports; reason presentation
uses the actual native adapter and buttons. Android evidence, negative artifacts and full signed APK are in
the evidence document. Platform/provider/device/commercial activation and publication gates remain separate.

See [evidence](quality/commercial-feedback.md).

Original configured SQLite targets now stay stable across open/closed Android reporting aliases.
The shared Data helper extends the existing D-146 approach to resource reviews, writers, Plan/reminder
packets, loan/backup boundaries and their unchanged foreign-file guards. Four added real SQLite cases
verify open readers/writers, coherent financial validation and rejection of another actual file.

## AT-163 - Captured pre-Save readiness (D-161)

New Goal and Budget editors explain their captured capacity, missing feature right or required explicit
selection before Save is disabled. The reason comes from the existing reviewed policy, uses all six real
translations and preserves complete raw drafts. WorkRestriction is an explanation, never a grant or a
replacement for the atomic writer. Existing budget net-definition/correction rules remain authoritative;
malformed bounded Goal choices require explicit repair, while unlimited access remains unaffected.
Production registrations stay inactive and test builds remain unrestricted.

Nineteen AT-163 actual-source/real SQLite cases pass. The final ordinary suite passes 2,908 tests,
App.Tests 851, zero failures/skips, in 2m 05.449s, followed by successful cleanup. Strict Windows and the
canonical complete signed Android Release build have zero warnings/errors. Thirty-six actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, retaining complete drafts and all
25 tables; exact owned development bytes restore. Native Android regression and negative evidence are
reported separately in the evidence document. No schema/model/migration, permission, SDK, credential,
network or portable preference is added. Physical-device, iOS, provider and activation gates stay separate.

See [evidence](quality/pre-save-readiness.md).

## AT-164 - Original access and read-only plan comparison (D-162)

More now opens Plans and access: a read-only comparison of the owner-approved Free/Plus/Pro model and
original current local access for this exact file. Comparing tiers changes only in-memory presentation;
counts and local tools come from existing Core QuotaPolicy/PlanPolicy. Current inactive test access never
becomes a fake Free/Plus purchase. Personal rights, exact shared membership, the host's right and original
profile choice remain distinct. All history, correction, backup/restore and owned export rules stay intact.

Only built local tools are compared. Unavailable Pro online services are an explicitly separate roadmap;
optional cloud backup belongs to every tier. Plus Lifetime is the same local Plus set, without invented
renewal. No price, purchase, trial, renewal, cancellation, restore-purchase or provider status is inferred.
Continue returns without changing a plan or buying anything; actual Free has the Continue with Free caption.
The existing explicit active-item form opens without saving. Production registrations stay inactive.

Seventeen AT-164 actual-source/real SQLite cases pass. Final ordinary main: 2,925 passed, App.Tests 868,
zero failures/skips, 2m 05.555s, followed by successful cleanup. Strict Windows and canonical full signed
Android Release have zero warnings/errors. Eighteen actual Windows contexts cover en/fa/de, light/dark,
360/412/wide, all three native tier choices, complete realized row captions/values and native help/return.
Actual Release Android More/chooser/comparison/help/Continue and exact owned restoration are verified.
All eight changed production-source PDB checksums match. No schema/model/migration, permission, SDK,
network, credential or portable paid data is added. Device, provider, billing and activation gates remain
separate. ENT-04 and the commercial release are not complete.

See [evidence](quality/product-plan-comparison.md).

## AT-165 - Original editor write rejection and accepted-return boundaries (D-163)

Account and Plan Save now explain actual structured quota/permission rejection in all six languages,
retain complete raw drafts and place complete feedback beside the pinned Save. Successful writes cannot
be resubmitted after a failed return or follow-up; inputs become read-only and Cancel becomes Close.
Account/default-preference boundaries stay distinct, including a default update that committed before
its subscriber failed. Original change-notification failures after commit remain visible through a typed
committed outcome, never a fake pre-write rejection. Exceptions are not silently swallowed.

Both editors use original native-effect ports and actual platform-independent application sources.
Native query interfaces stay on pages. Plan picker rows retain only their actual plain-text identity/name/
selection, without unused native color/icon allocation. Account and Schedule edit copies preserve all
original fields/identities; Schedule also clones its mutable rule. A refused historical split cannot mutate
the original in-memory state, stop boundary or automatic-post anchor before an explicit corrected retry.
Original validation, signs, calendars, currency locks, category selection, split rules and atomic writers
remain authoritative. No EF property/schema, migration, permission, SDK, credential or portable-format change.

21 AT-165 cases pass; final main passes 2,946 / App.Tests 889, zero failures/skips, 2m 07.788s, followed by
cleanup. Strict Windows and canonical complete signed Android Release have zero warnings/errors.
36 actual Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Actual Save invokes
original atomic rejection/accepted writers over independent fictitious SQLite; raw drafts, pinned captions,
real input/Save enablement and Close are checked. Explicit fictitious return/consent effects are identified;
all 25 original tables and exact owned development bytes restore. Actual normal Android new-form/query
regression and exact owned restoration are separate from typed fixture/physical-device acceptance.
Production registrations remain inactive. Initial pre-attempt readiness and other operation/activation
coverage remain open; this does not complete ENT-04 or the commercial release.

See [evidence](quality/editor-write-feedback.md).

## AT-166 - Reviewed account/plan/default archive transaction (D-164)

Account archive is now one reviewed SQLite transaction: original archive state, affected active/paused
plans in either account direction, exact default cleanup and original persisted activity choices commit
or roll back together. The reader captures complete original account/plan values and the local day without
creating settings. The writer reacquires/rechecks complete inputs under the actual file's writer and rejects
retired rows, day, cached access, another file or a different original notification owner.

Original Account/Plan feature/capacity decisions are shared, not duplicated. Archiving ends related plans
with established PlanActions; unarchive consumes the original account slot but never resumes ended plans.
Another concurrently chosen default remains untouched. Both original stores publish only after commit;
publication failures retain the accepted result and cannot cause a repeat. The UI blocks concurrent actions,
requires explicit consent before discarding a dirty draft, never silently saves those financial inputs,
and freezes an accepted result with Close after follow-up/return failure.

17 AT-166 cases pass. Final main: 2,963 passed, App.Tests 906, zero failures/skips, 2m 10.406s, followed by
cleanup. Strict Windows and complete signed Android Release have zero warnings/errors. 36 actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced with original native Archive/Close,
real SQL rollback and actual accepted account/plan/default writes over independent fictitious rows.
Explicit fictitious consent/alert/return effects are identified. Actual normal Android consent-decline
and exact owned restoration remain separate from typed fixture/physical-device acceptance. All four
changed production-source PDB checksums match. No EF property/schema, migration, permission, SDK,
credential, network or portable-format change. Production registration stays inactive; initial readiness
and other commercial paths remain open, not release completion.

See [evidence](quality/account-archive.md).

## AT-167 - Original Account/Plan readiness and retained refresh (D-165)

Fifteen AT-167 cases pass with actual application sources and isolated SQLite. The original 21 AT-165
cases also pass: known full quotas now disable Save before attempting it, so original writer-rejection
cases capture a free slot and then exercise a real later addition/access change. They still prove original
atomic rejection and an explicit corrected retry. Final main: 2,978 passed / App.Tests 921, zero failures
or skips, 2m 12.110s, followed by cleanup. Strict Windows has zero warnings/errors.

Six resource languages cover precise quota reasons, retained raw drafts and explicit refresh/read-refusal
retry. Additional cases cover original advanced rules/correction, persisted-choice counts/malformed repair,
unlimited Plus and inactive unopened-file behavior. Existing AT-165 races still exercise actual original
writers after a changed review. Native and package evidence: [editor-readiness](quality/editor-readiness.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) prove native disabled Save, complete reasons/refresh, retained input/read-refusal/retry
and all 25 unchanged original tables. Normal final signed Release Android verifies actual refresh and
Cancel with retained native fields, unchanged original rows, existing D-141 migration only and exact
owned byte/sidecar restoration. Final Release is installed/stopped. Canonical full Android build has
zero warnings/errors; all six changed production-source PDB checksums match. The installable APK is
artifacts/android/zanance-d165-release.apk (82589781 bytes); signature/structure evidence is recorded
in the focused quality document. No physical-phone, iOS, OAuth, billing or release acceptance is inferred.

## AT-168 - Original import/Undo refusal and accepted follow-through (D-166)

32 AT-168 cases pass with actual application sources, real isolated SQLite and all six resource languages.
They cover original ledger/holdings permission refusal and retry, post-commit publication outcomes,
nonrepeatable accepted import/Undo, real history-read failure with other batch retention, picker/consent
cancellation and busy guards, explicit read-refusal/retry, security/export ports and neutral complete CSV,
real SQL-trigger whole-transaction rollback, plus explicit aggregate links and complete restored metadata.
Final ordinary main: 3,010 passed / App.Tests 953, zero failures/skips, 2m 42.936s, followed by cleanup.
Strict Windows and canonical complete Android Release have zero warnings/errors. All seven changed
production-source PDB checksums match. Device/platform/provider acceptance remains separate.

Original overlap selection, currencies/signs, duplicate identity, complete metadata, atomic rollback and
durable Undo stay authoritative. Native/effect boundaries and remaining gates:
[focused evidence](quality/import-outcomes.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) verify original native review/import/Undo, retained refusal and cancellation, complete
pinned reasons/results/targets, accepted subscriber-failure semantics and all 25 original tables restored.
Normal final Release Android verifies DocumentsUI picking, actual preview refresh, picker cancellation,
Import and consented Undo, unchanged original rows, existing D-141 migration only, exact owned bytes/
sidecars and Main/English/System restored. The new owned CSV is removed; final Release is installed/
stopped. APK: artifacts/android/zanance-d166-release.apk (82175915 bytes), signature/structure and all
seven production-source checksums verified. Physical-phone/iOS/OAuth/billing/activation stay separate.

## AT-169 - Accepted restore, uncertain source failure and completion retry (D-167)

35 actual-flow/original-service/SQLite cases pass across all six languages: accepted follow-through
failures and nonrepeatable payload, explicit completion retry, real partial-source/safety/read failures,
busy/consent/authentication preservation and actual automatic-post idempotency. Main 3,045/App.Tests 988;
strict builds, 36 native Windows contexts and normal signed Release Android restore/readback pass.
Native Back is guarded in source; Windows review checks its CanLeave affordance, not actual Back Invoke.
Fixture effects, complete owned restoration and external gates: [evidence](quality/restore-outcomes.md).

## AT-170 - Original forecast drafts, atomic saved paths and currency comparison (D-169)

29 AT-170 cases and the ordinary 3,074-test suite pass (zero failures/skips); cleanup and strict
Windows/complete Android Release builds pass without warnings/errors. Thirty-six existing Windows
layout contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Three further native
two-currency contexts verify per-currency saves and immutable legacy comparison with complete original
25-table restoration. Normal Release Android name/Save/details/Delete consent was exercised once;
exact owned bytes/sidecars and Main/English/System were restored. Final updated Release is installed/stopped.
Commercial registration stays inactive. Phone, iOS, real OAuth, billing and release acceptance remain open.
Direct current-file guards, SQL-trigger rollback, committed nonrepeatability and legacy foreign-money
regression use original application/Core/Data sources and real SQLite. Fictitious native effects are
explicitly identified; unchanged layout checks are reused only for unchanged presentation.
See [bounded scenarios and negative evidence](quality/forecast-work.md).

## AT-171 - Shared original receipt work and late typed editor (D-170)

30 AT-171 cases and the ordinary 3,104-test suite pass (App.Tests 1,047, no failures/skips,
2m 30.954s), followed by cleanup. Strict Windows and canonical complete signed Android Release have no
warnings/errors. Thirty-six Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced.
Normal Release Android exercises actual Home source choice, DocumentsUI, original digital-PDF reading,
typed new editor, retained attachment rereading, existing editor and explicit discard without financial
Save. Complete prepared rows and exact owned baseline bytes/sidecars are restored; final Release stopped.
Commercial registration remains inactive; phone/iOS/camera/OCR corpus/provider/Store acceptance is separate.
Source mutation/foreign-file/access-retirement cases use actual application/Data/Core sources and
real SQLite, not copied parsing or fake controls. Native Windows effect ports and actual Android
digital-PDF engine are distinguished from camera/image OCR/device acceptance.
See [bounded scenarios, negative evidence and restoration](quality/receipt-work.md).

## AT-172 - Original loan work and atomic confirmed installment (D-171)

37 AT-172 cases and ordinary main 3,141 / App.Tests 1,084 pass, zero failures/skips, 2m 54.955s;
cleanup and strict Windows/complete Android Release builds succeed without warnings/errors. Thirty-six
Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, with actual native payment
consent and original ledger readback. Normal signed Release Android verifies original account/schedule,
native payment consent and exact principal/interest entries. Complete owned fixtures and baseline bytes/
sidecars/Main/English/System are restored; final Release installed/stopped. Commercial registration
stays inactive; physical phone/iOS/provider/billing/Store and full-release acceptance remain separate.
Original calculators, actual non-UI application flows and real SQLite cover source retirement, direct
writer access, whole-batch/trigger rollback, final clamp, transfer/interest invariants and accepted
subscriber failure without old-review replay. Native effects and restoration are bounded separately.
See [scenarios and negative evidence](quality/loan-work.md).

## AT-173 - Original report/PDF and contribution review (D-172)

28 AT-173 application/real SQLite cases; ordinary main 3,169 / App.Tests 1,112 pass with zero
failures/skips (2m 42.749s), followed by test-output cleanup. Final strict Windows and canonical complete
Android Release builds have zero warnings/errors. Thirty-six en/fa/de, light/dark, 360/412/wide and
Simple/Advanced Windows contexts exercise real package targets, original calculations, Free refusal
without changing selection, detached actual PDF and unchanged original inputs. Final targeted PDFs
also retain Free account opening/movement/closing tables and per-account currency scopes; native goal
suggestion acceptance is checked in en/fa/de with its named-calendar rule and no ledger posting.
Normal signed Release Android exercises original report/history, actual PDF cache/platform share,
share-sheet cancellation and complete retained choices. Owned tables, cache, baseline bytes/sidecars
and Main/English/System are restored; final Release installed/stopped. Commercial registration remains
inactive. Physical phone/iOS/OAuth/billing/Store/performance/full-product acceptance remain separate.
Covered: original Free balances/status/portable data; denied paid callback before authentication;
inactive/Plus/Pro and shared membership/host distinction; changes during consent (data/Settings/rights/
activation/day/form/caller packet/actual file); single-flight cancellation and failed unpublished-cache
cleanup; original contribution transaction rejects stale source and retains accepted subscriber failure.
Real native package selection, detached PDF/scope retention, basic balance/per-account currency, named
calendar suggestion Save/no ledger posting and actual Release share/cancel/cache readback are local
evidence. Phone/iOS/provider/billing/Store/performance remain pending. [Evidence](quality/report-work.md).

## Current local commercial completion review (D-173)

The finite mapping of ENT-02 boundaries and local ENT-03 downgrade scenarios is closed with existing
behaviour/native evidence through AT-173. This review adds no acceptance scenario or test execution;
main remains 3,169 passing tests. Native Intent, real shared-server lifecycle, activation, physical phone,
OAuth and iOS acceptance stay open. [Evidence](quality/commercial-completion-review.md).

## AT-174 - Native Android quick widget roles and original language

Locally checked in twelve actual widget instances/cases: three languages, app light/dark choices,
two widths; original three unsaved entry intents/logo, cold persisted Persian locale/covered intent,
complete original owned rows and exact fixture/display/widget restoration. Main remains 3,169;
this native scenario is not an additional automated test count. TalkBack remains inactive; spoken
order/announcements, other screens, system scaling and phone/platform acceptance are open.
[Evidence](quality/android-widget-accessibility.md).

## AT-175 - Original date purpose names and retained partial input

Checked locally: 18 actual Windows language/theme/width contexts; native start/end/fallback names
with partial year unchanged and original three-calendar values. Six original Release Plan cases and
three debt reference cases; Cancel/own discard without Save, complete original data restoration.
Main remains 3,169; this native scenario does not invent an additional automated count. Spoken
TalkBack/Narrator/OS-scaling/phone/iOS acceptance is separate. [Evidence](quality/date-context-accessibility.md).

## AT-176 - Original Home layout reuse

Locally verified: real appearance invoked twice across 18 Windows language/theme/width contexts;
zero section add/remove events, original native handlers retained, actual order equals saved layout
and complete accounts/entries/settings/budgets unchanged. Existing HomeLayout cases and full 3,169
suite pass. Final normal Release Android Home/return and exact owned-data/display restoration verified. No new automated count is invented;
QA-06, spoken/system-scaling/phone/iOS/provider and release gates remain open.
[Evidence](quality/home-layout-reuse.md).

## AT-177 - Original indexed asset-event quantities

Ten automated behavior cases and main 3,179 pass. Original calculator remains the oracle across
all kinds, types/dates, identity/order, neutral transfer and intermediate overflow. Core-only bounded
measurements show exact original quantities with one input enumeration. Six Windows native caption
contexts and original asset packets pass; Six normal Release Android scoped caption/return cases pass; original Main/fixture/display restored. No physical/iOS/whole-startup/ANR/release acceptance is inferred.
[Evidence](quality/asset-event-index.md).

## AT-178 - Retained original icon language and selection

Measure actual native names for every original key/default/selected state through en/fa/de changes
in both themes, retaining the same buttons and draft. Invoke original default/Home buttons through
native patterns and compare complete stored rows without Save. Native checks and complete
APK verification pass locally; no new non-UI test count or spoken/device acceptance is invented.
[Evidence](quality/icon-language-accessibility.md).

## AT-179 - Official Community independent compatibility scope

Actual Windows C API and Microsoft.Data.Sqlite.Core 10.0.12/provider.sqlcipher 3.0.5 report pinned
SQLCipher/OpenSSL versions. Correct-key reopen preserves original Int64/date/currency; wrong/missing
keys and altered pages refuse reads, and fictitious sentinel is absent from encrypted DB/WAL.
Encrypted same/different-key backup succeeds; plaintext BackupDatabase refuses, original memory
export succeeds. Native Windows tool fresh restore/cache identity checks pass. Main suite remains
3,179 from unchanged D-179; no invented automated test count. Full-schema/EF, upstream suites,
reproducibility, native mobile, key/recovery, crash migration and product release are unaccepted.
See [compatibility](../../../../docs/architecture/sqlcipher-community-build.md).

## AT-180 - Independent official Windows native producer

Eleven hostile origin/digest/ZIP path/size/symlink cases refuse restoration. Original data/shared
sources and actual compiled model run 32 migrations from first-schema fictitious financial rows.
Correct-key reopen preserves identifiers/Unicode/currency/amount/date/settings, default foreign
keys remain enabled; missing/wrong key and modified page refuse reads, plaintext header/marker
are absent. Compare fresh native outputs and package hashes, recording failed incremental evidence.
See [guide](../../../../docs/architecture/sqlcipher-community-build.md) for actual current results.
Upstream suites, mobile/key/recovery/crash migration remain unaccepted; main 3,179 is unchanged.

Fresh Windows DLL/package hashes match across two clean builds; both final original-schema
harness runs, native export/system-only dependency inventory and package CRC/native checks pass.
Upstream logs retain 749 warning occurrences each; warning/security review and upstream suites
remain open. These are local candidates, not warning-free production binaries or app activation.

## AT-181 - Matching official OpenSSL suite

Execute stock nmake test in the original configured independent candidate tree, non-elevated.
Actual PASS: 347 files/3,818 reported tests, 777 test-phase seconds; candidate hash unchanged.
Retain 59 wholly skipped recipes and 114 skip markers (configuration/platform/FIPS/fuzz coverage
limits), compiler warnings and bounded source inspection. This does not exercise SQLCipher's
upstream suite, native Android/iOS, keys or crash-safe migration; main app/test/APK unchanged.
[Validation](../../../../docs/architecture/sqlcipher-security-validation.md).

## AT-182 - Independent official Android Native slices

API24 arm64/x64 clean native outputs match; actual ELF architecture/16 KiB segments, RELRO/NOW,
non-executable stack, original exports and system-only dependency checks pass. Six malformed ELF
cases per ABI refuse verification. Actual x64 API36/4 KiB emulator independent key/tamper/FK and
Int64.MinValue/EUR/date reopen pass; deployed bytes match, UUID-scoped fixtures removed. Identical
candidate packaging retains required notices and verifies CRC/native hashes. Main 3,179 unchanged.
Arm64 physical/runtime, minimum API/16 KiB OS, managed schema/key/recovery/crash migration and iOS
are unaccepted. [Guide](../../../../docs/architecture/sqlcipher-android-build.md).

## AT-183 - Official SQLCipher fixture and initial Native CI

Actual maintained runner: 226 stock tests, zero errors, reported no allocations leaked; cipher
4.19.0 community/OpenSSL 3.5.9 runtime identity matches and no system crypto dependency is loaded.
The initial provider identity read without a key failed and is retained; corrected scoped dummy
key succeeds. Eleven input checks pass. Native workflow YAML/job structure parses; commands are
locally exercised, hosted run and Windows/iOS/runtime matrix remain unaccepted. Main suite is
still 3,179; do not add these stock tests to its count or call this full SQLite/app security.
[Validation](../../../../docs/architecture/sqlcipher-security-validation.md).

## AT-184 - Release managed Android original schema

Actual maintained runner on a separate x64 API36/4 KiB probe package: all 32 migrations, actual
compiled model/source, Unicode/IDs/money/EUR/date/settings/FK and wrong/missing-key/tamper refusal
pass. Release AOT/trimming builds without warnings/errors; APK/native/signature/permission checks
pass. Shared Windows path also passes. Only independent package/fictitious files removed; no main
app data accessed. Preserve initial D-44 registration omission/deadlock diagnostics. Main suite
3,179 unchanged. Arm64/API24/16 KiB/iOS, whole-app keys/recovery/crash migration remain unaccepted.

## AT-185 - Memory-only portable database primitives

Six automated cases preserve signed amount/Unicode/read-only/WAL memory normalization, reject
oversized/non-database/canceled input, and tolerate double disposal. Full 3,185 pass. Actual
Windows and separate Release Android source/compiled-model/migration probe additionally verify
portable header/history/schema and official encrypted staged restore. Strict main builds zero
warnings/errors. Preserve unsupported plaintext BackupDatabase-to-cipher failure. No live
provider/backup switch, key/recovery/crash-commit or other device/platform acceptance is inferred.

## AT-186 - Complete typed staging verification

Twelve cases preserve reordered equivalent rows/duplicates and reject changed integer/storage
type/text/BLOB/duplicate count/header/schema, equal but invalid FK data and a changed large-BLOB
last byte. Cancellation leaves callers usable. Full 3,197 pass. Actual original-schema encrypted
staging checks pass in Windows and independent Release Android; strict main builds zero warnings/
errors. Preserve fatal driver expression-BLOB metadata-read evidence. No commit/recovery/device/
whole-app encryption inference; direct native comparisons do not report financial contents.

## AT-187 - Explicit generation checkpoints

Eight automated cases verify ordered selection, immutable original state, skipped/repeated phases,
identity drift, invalid generation identities and terminal-state refusal. Full 3,205 pass, zero
failures/skips. No visible screen changes require a new UI matrix. This is protocol-model evidence,
not key persistence, filesystem durability, restart recovery or whole-app encryption acceptance.

Strict main Windows and Android builds pass with zero errors and warnings.
Evidence: artifacts/native-sqlcipher/d188-{full-tests,windows-build,android-build}.log.

## AT-188 - Resumable migration coordination

Fifteen behavior cases cover interruption after eight native effects/commits, selected-key loss,
false successful persistence, cleanup ordering, retired context across awaits, identity/regression,
completed restart and cancellation. Focused 57/full 3,220 pass, zero failures/skips. The test native
port is faultable, not a filesystem or SecureStorage implementation. Real adapter restart and
power-loss checks remain open. No visible UI or app activation requires a repeated UI matrix.

D-189 strict main Windows and Android builds pass with zero errors and warnings.
Evidence: artifacts/native-sqlcipher/d189-{coordinator-tests,full-tests,windows-build,android-build}.log.

## AT-189 - Native CI Linux workspace ownership

Six real Docker cases: UID 10001 cannot write Linux fixture owned by 1001; matched non-root UID
can write work/output while recipe stays read-only; actual shared Android and upstream mount
arguments allow their work/output but refuse recipe/input writes; existing and nested workspace
requests are rejected. Four modified PowerShell sources parse and workflow YAML validates. Full
hosted native execution remains pending. App tests/builds/UI/APK are unchanged and not repeated.
Evidence: artifacts/native-sqlcipher/d190-workspace-check.log and failed hosted artifact 11777219319.

## AT-190 - Foreign non-root parent traversal

Old image mode 750 reproduces matched foreign-UID work mount refusal. Corrected image mode 0755
passes all seven focused workspace cases; foreign parent writes remain refused. Native host/NDK/
clang identity still matches the lock. Records: d191-parent-negative.log, d191-workspace-positive.log,
d191-host-rebuild.log under artifacts/native-sqlcipher. Hosted [38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) completed successfully; all seven workspace cases, two
matching clean ABI builds, twelve ELF refusals and 226 cipher tests/zero errors pass. Success
artifact digest and actual library bytes verified. This is not whole-app encryption acceptance.
No unchanged application tests/builds/UI/APK repeated.

## AT-191 - Bound Windows data-key protection

Four context cases and eight native Windows cases cover required identities, stable versioned
context, CurrentUser roundtrip, wrong profile/installation/generation, altered blob, absent/
oversized envelope and wrong plaintext length. Focused 69/full 3,232 pass on Windows; eight DPAPI
cases explicitly skip on non-Windows hosts, not pretend to run. Test-WindowsKeys creates a fictitious
protected envelope/encrypted DB then reopens both in a new process using actual source code and
reviewed native hash/version. No existing credentials/main app data accessed. Native Android/iOS,
durable production storage, recovery UX and full migration/crash acceptance remain open.

D-192 strict main Windows and Android builds pass with zero errors/warnings. No visible screen
changed. Evidence: artifacts/native-sqlcipher/d192-{windows-build,android-build}.log.

The separate Android compatibility project compiles the same changed infrastructure/managed
package under ContinuousIntegrationBuild with zero warnings/errors (d192-android-probe-build.log).
Its unchanged schema/native runtime checks were not repeated for unused Windows key code.

## AT-192 - Bound Android native key protection

Original adapter in distinct signed Release AOT/trimmed package: creation and encrypted reopen
across stopped/new processes pass, followed by fifteen behavior checks (fourteen refusals, one
random-nonce behavior). Missing wrapping key never regenerates. OS reports software level 0;
no hardware/device claim. Exact reviewed arm64/x64 APK bytes, ZIP and v2/v3 signatures checked;
only independent package removed. Strict main Windows/Android builds have zero errors/warnings
(artifacts/native-sqlcipher/d193-main-{windows,android}.log). Unchanged managed 3,232 baseline
not rerun. No visible UI/schema/main-key/provider change. Durable recovery/crash migration,
iOS/physical-device and product encryption acceptance remain open. See device-key guide.

## AT-193 - Original Android envelope file storage

Separate signed Release AOT/trimmed three-process create/stored-key reopen/storage-refusal route.
Eight storage cases: duplicate retains original key; corruption, truncated, oversized, absent,
nonregular and symlink file refuse; fictitious partial staging is ignored/preserved. Exclusive creation/native file and directory flush path exercised. Hard-link EACCES failed
approach retained. Torn final files refuse; not atomic whole-file publication. No mid-publication kill/power-loss acceptance; coordinator
restart matrix remains. Independent APK signature/native bytes and exact package cleanup checked.
Strict main build results recorded in d194-main-{windows,android}.log. Unchanged managed 3,232
and D-193 protector fifteen cases retained without rerunning unrelated suites. Main still plaintext.

## AT-194 - Real transactional selection and native process restart

Thirteen original-store SQLite behaviors pass (focused 82). Windows and independent Android
Release AOT/trimmed each use five fresh processes: create CandidateVerified, write/flush native
hot journal then external kill, reopen/rollback with original selected, actual commit Selected
then external kill, reopen with candidate selected. Original checkpoint code reads actual files.
Initial Windows writer-sharing observation failed and was corrected; no restart on observation
expiry. APK signature/ZIP/native hashes and exact independent package removal verified. Main
finance model/UI/provider unchanged; complete migration/key/cleanup/recovery, power loss, device,
iOS and all-profile acceptance remain open. Full/strict build evidence follows below.

D-195 final managed evidence: all 3,245 tests pass with zero failures/skips; test output cleaned.
Main strict Windows/Android builds pass with zero warnings/errors. Records under
artifacts/native-sqlcipher: d195-{journal-tests,full-tests,test-clean,main-windows,main-android}.log.
Independent native strict builds also pass; no visible main screen changes or phone-test handoff.

## AT-195 - Complete original native migration and interruptions

Ten original session guard cases pass (focused 92), distinct from native evidence. Independent
signed Release AOT/trimmed Android runs three fictitious actual-model profiles, 32 migrations,
financial fields/Unicode and typed 2 MiB BLOB/signed-boundary data. External kill during actual
candidate creation leaves KeyPrepared; fresh process checks unchanged source/envelope and resumes
full coordinator to Completed. External kill at actual Selected resumes keyed candidate/rollback,
exact source-sidecar cleanup and Completed. Missing Selected envelope refuses without replacement,
plaintext fallback or cleanup. Unrelated file preserved; encrypted candidate/rollback have no plain
header/fixture marker. Native signature/ZIP/hash/package cleanup verified. Initial ReadOnly-source
export failed; corrected source mode without Create passes. Wider crash/power/disk/physical/Windows/
iOS and real application lifecycle/backup/recovery/all-profile gates remain. Main still plaintext.

D-196 final managed results: all 3,255 tests pass with zero failures/skips and output cleaned.
Main strict Windows/Android builds have zero errors/warnings. Evidence under artifacts/native-sqlcipher:
d196-{session-tests,full-tests,test-clean,main-windows,main-android}.log. Independent signed Release
native build/three actual migration scenarios pass; unrelated upstream/Windows key/UI suites not rerun.

## AT-196 - Original protected EF context factory

Ten guard cases pass (focused 102): plaintext/missing file, Create/pooling/missing-key/foreign-path
creator, retired publication during creator, no unkeyed move and legacy keyed export/restore refusal
before streams change. Actual original DI factory in signed Release AOT/trimmed Android reads
migrated original model, writes audited metadata, reopens it and rejects already-open stale context
commands after same-path publication. Actual legacy backup outputs zero bytes. Native APK signature/
ZIP/ABI bytes and exact package cleanup pass. Whole migration matrix not repeated unchanged.
Main remains plaintext; native package/lifecycle/catalogue/portable backup/recovery/OS policy and
other platform/physical acceptance remain. Full/strict evidence is recorded below.

D-197 final results: all 3,265 tests pass with zero failures/skips; output cleaned. Main strict
Windows/Android builds have zero errors/warnings. Evidence: artifacts/native-sqlcipher/d197-
{factory-tests,full-tests,test-clean,main-windows,main-android}.log and native factory proof above.
No unchanged migration/crypto/Windows-key/UI matrices or main phone APK were repeated.

## AT-197 - Actual protected portable backup output

Original factory/Zanance commercial backup source exports protected live data through memory only.
Independent signed Release Android portable image opens/validates in memory and compares complete
schema/header/typed real-model/migration/financial/2 MiB BLOB data. Publication retirement in the
first destination write stops output within one 81,920-byte chunk. Larger ArrayPool bucket failure
retained and fixed; actual related path passes. Legacy keyed restore consumes zero input. Focused
102 pass. APK ZIP/signature/native hashes and exact package removal verified. Main unchanged and
plaintext; generation-aware receiver/startup/all-profile/recovery/OS policy and external gates remain.

D-198 final managed evidence: all 3,265 tests pass with zero failures/skips and output cleaned.
Main strict Windows/Android builds have zero warnings/errors. Logs under artifacts/native-sqlcipher:
d198-{backup-tests,full-tests,test-clean,main-windows,main-android}.log. Changed native backup route
passes; unchanged migration/crypto/Windows-key/UI matrices and main phone APK were not repeated.

## AT-198 - Encrypted portable import and actual migrations

Six guard cases/focused 108 pass: existing output/sidecar, invalid/oversized input, retired access,
cancellation refuse before key/output effects. Independent signed Release Android uses actual first
schema backup, new local device binding, complete import verification, actual upgrade through 32
migrations and keyed reopened financial/profile ID/value/date checks. Source digest unchanged and
candidate plain header/marker absent. No live selector/cleanup touched. Strict native build/APK ZIP/
signature/ABI hashes/package removal checked. Main receiver dispatch/selection/safety/package rollback,
provider/startup/all-profile/recovery/OS policy and physical/iOS/crash gates remain open.

D-199 final managed evidence: all 3,271 tests pass with zero failures/skips; output cleaned.
Main strict Windows/Android builds have zero errors/warnings. Logs under artifacts/native-sqlcipher:
d199-{restore-tests,full-tests,test-clean,main-windows,main-android}.log. Changed native old-schema
restore route passes; unchanged migration/export/crypto/UI matrices and main phone APK not repeated.

## AT-199 - Actual restored selection/rollback and selected restart

Six guards/focused 114 pass; no replacement key or implicit journal/selection. Native actual-model
current/restored packets differ; encrypted rollback fully typed-verified before actual Selected,
external process terminated at persisted marker and same-run selected resume preserves both packets
and original without old connection. Observer timeout while live was not a re-creation trigger.
Resume Mono D-44 registration omission diagnosed/fixed; only same selected dataset phase rerun.
Final signed Release proof/sig/ZIP/native ABI and package removal pass. This is not package Completed,
preferences/safety retention/full rollback/main dispatch/provider/all-profile/recovery/OS policy or
physical/iOS/crash matrix acceptance. Main remains plaintext.

D-200 final managed evidence: all 3,277 tests pass, zero failures/skips; output cleaned. Main
strict Windows/Android builds zero warnings/errors. Logs under artifacts/native-sqlcipher:
d200-{selection-tests,full-tests,test-clean,main-windows,main-android}.log. Native same-run selected
resume proof passes after diagnosed fixes; existing candidate/selection was not re-created.

### AT-200 — Validated package receiver dispatch (D-201)

Managed service tests pass complete two-source dispatch, checksum rejection before receiver access,
failure without legacy fallback, clearing borrowed buffers on success/failure, gate reuse, normal
DI resolution, and absent/unknown source delivery. These are real BackupService tests, not native
rollback/crash acceptance. Main receiver and full package restart/rollback acceptance remain open.

D-201 verification: 42 backup tests and the complete 3,282-test suite pass with zero failures/skips.
Strict Windows and Android builds pass with zero warnings/errors. Logs: artifacts/native-sqlcipher/
d201-{backup-tests,full-tests,main-windows,main-android,test-clean}.log. No changed screens or native
code: no UI/unchanged native matrix rerun and no phone acceptance claim.

### AT-201 — Protected package preference replay (D-202)

Managed behaviours cover missing/null values, imported device-key exclusion, malformed/oversized
preferences, foreign allowlist refusal and idempotent replay after partial writes. Native package
interruption/publication evidence is recorded separately; main application/device acceptance is open.

D-202 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB fixture passed
actual BackupService package receiver, first-schema/current model migrations, external kill after
first native SharedPreferences committed value, reopened original/restored capsule, replay before
actual protected factory publication and restored money/device-state checks. Encrypted preference
and database rollback retained. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-
proof-96d1bcfdefd34c75b456534548dc4fa1.json, run 8cc8558c1a494c9f944d987e0e6b5445.
The fixture uses its own named SharedPreferences with synchronous Commit; it does not certify the
main MAUI adapter's asynchronous durability. Initial native compile lacked the MAUI preferences
reference; corrected fixture to native isolated adapter without adding a production package/bundle.
Native build zero warnings/errors; complete APK signature, ZIP and both reviewed ABI hashes pass;
independent package uninstalled. Driver can use an explicitly existing built APK and checks packaged
ABI identity before installation; no unchanged native suite or UI matrix rerun.

D-202 final managed evidence: 119 infrastructure tests pass. The complete run executes 3,287 tests:
3,286 pass and one pre-existing budget input-culture assertion fails because unordered child rows
were compared by enumeration order. Correct the assertion to compare category/value pairs sorted
by category identity (same established convention as the adjacent budget test). Focused rerun of
both input-culture variants passes; do not describe the original full run as green or repeat the
complete suite without another concern. Strict Windows/Android builds zero warnings/errors;
test outputs cleaned. Logs under artifacts/native-sqlcipher/d202-{data-tests,full-tests,
budget-order-tests,main-windows,main-android,test-clean}.log. Script parser and focused packaged
ABI hash helper pass after driver-only edits; unchanged native/runtime matrices not repeated.

### AT-202 — Factory maintenance lifetime exclusion (D-203)

Actual isolated factory tests cover multiple live contexts/drain, pending sync refusal/async wait,
owned context queries, unauthorized publication, early owner closing, command refusal until context
disposal, cancelled writer/reader admission, foreign/retired identity and failed-construction release.
Native protected-factory evidence is separate; global covered lifecycle/device acceptance remains open.

D-203 final evidence: 125 infrastructure tests and all 3,293 tests pass (zero failures/skips).
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Actual Android x64 API36/4 KiB protected factory additionally creates an owned maintenance context,
checks unchanged money, blocks ordinary admission, closes owner while its context is live, refuses
its next native command, then admits the waiter only after native/context disposal. Complete APK
signature and driver packaged native-ABI identity verify; owned package uninstalled and tests cleaned.
Native proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-a146cb94724947fb84be6f20740f02aa.json,
run 6c8e6cac24eb463e955482ba8ccd3d6c. The initial original-model observation expired; live PID 21141
subsequently reported OriginalModelCreated/32. Continued the same fixture with explicit completed-model
continuation, without recreating it. Preserve d203-native-factory.log and d203-native-factory-resume.log;
no timeout restart. Other logs: d203-{data-tests,full-tests,main-windows,main-android,native-build,test-clean}.log.
No UI/native crypto matrix rerun, main encryption activation or physical/iOS/performance acceptance.

### AT-203 — Original profile switch with maintenance ownership (D-204)

Actual application source tests check drain before path change and queued work reopening the previous
path after refused destination authentication, alongside existing profile isolation/catalogue/failure
cases. Native original-flow fixture uses the explicit authentication decision port; device auth/UI
and full protected catalogue acceptance remain separate.

D-204 final evidence: 99 profile-related tests and all 3,295 tests pass with zero failures/skips.
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Original ProfileService/store/32 migrations/autopost/factory run in an independent Android x64
API36/4 KiB fixture: old context drains before path move, owned destination migration/settings
finish, unrelated work waits through refused authentication and reopens original main money/state,
with no ledger posting. Explicit native authentication decision port is simulated, not a real device
prompt. Full signature and reviewed packaged native-ABI checks pass; package uninstalled/tests cleaned.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-0e375f15d51e42fb955d1901d95dc132.json,
run 43b655df1e514b16a6a09209cde0ae87. Logs: d204-{profile-tests,full-tests,main-windows,
main-android,native-build,native-profile,test-clean}.log. No visible layout or unchanged crypto matrix
rerun. Remaining destructive inactive-profile file operations still need maintenance coordination,
alongside protected catalogue/provider/startup integration; main databases remain plaintext.

### AT-204 — Inactive deletion with maintenance (D-205)

Original application-source cases cover files/catalogue retained until context drain, stale review
after a concurrent rename, cancellation without leaked ownership and synchronous refusal while
busy. Existing profile deletion/isolation/confirmation cases remain. Native file effects are recorded
separately; final protected cleanup/retention and physical/iOS acceptance remain open.

D-205 verification: 103 profile tests and all 3,299 tests pass, zero failures/skips. Strict Windows/
Android and independent Release AOT/trim builds zero warnings/errors. Original Android x64 API36/
4 KiB profile fixture confirms inactive deletion waits for an actual retained context, then removes
only inactive database/sidecars and retains original main/catalogue/money; original drain/authentication
refusal cases also pass. APK signature and driver packaged ABI identity verify; package uninstalled,
tests cleaned. The first observation expired while PID 22737 remained live; subsequent actual result
contained all six successful flags. Record same completed run without restart/recreation:
artifacts/native-sqlcipher/d205-native-profile-proof.json, run f3f5098787bd414da33b20f85a6cfe55.
Preserve d205-native-profile.log expiration evidence. Other logs: d205-{profile-tests,full-tests,
main-windows,main-android,native-build,test-clean}.log. Authentication is the decision port, not native
biometric acceptance; no new UI layout, unchanged crypto matrix, main encryption or protected deletion
lifecycle acceptance is claimed.

### AT-205 — Durable initial key-binding identity (D-206)

Managed original-store cases cover stable main/other reopen, registration without replacement,
missing/partial refusal, corrupt/typed identity refusal without mutation and retired/invalid bootstrap
inputs. Native process-restart/journal binding evidence is recorded separately; main activation,
physical power-loss, later generation references and full lifecycle acceptance remain open.

D-206 final evidence: 131 infrastructure tests and all 3,305 tests pass with zero failures/skips.
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Independent Android x64 API36/4 KiB creates/commits actual two-profile identity catalogue and main
initial generation journal, externally terminates the completed process and reopens in a new one.
Exact identity fingerprint, nonempty main binding, existing registration and new-profile installation
preservation pass. No DEK/key preparation or main activation occurs. Signature and packaged reviewed
ABI identity verify; independent package uninstalled and test outputs cleaned. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b540cdeaaef44a2d82592e745cd8a3c3.json,
run ded8acc1a61547d19614cc82a3f3e58a. Logs: d206-{data-tests,full-tests,main-windows,main-android,
native-build,native-identity,test-clean}.log. Typed blob rejection was added during review before the
native fixture execution. No unchanged cipher/crypto suite, UI matrix, physical power-loss or main
whole-lifecycle acceptance is claimed.

### AT-206 — Initial identity-to-protected-factory composition (D-207)

Managed actual metadata/context guards cover explicit journal identity, no replacement, missing
journal before key preparation, foreign owner and existing ciphertext/foreign endpoint refusal.
Native initial migration/cold factory reopen is separate evidence; main lifecycle/platform acceptance
and later restored-generation selection remain open.

D-207 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB joins actual
stable identity, initial journal, original 32-migration model, native Keystore/envelope/fs effects,
full migration/cleanup and retained-key original factory. Saved account-name write/money survive
external process termination and cold reopen with an ExistingOnlyKeys port that forbids preparation.
Original is absent, encrypted rollback retained and disposed key owner rejects new factory access.
Initial observer expiry occurred while PID 23665 remained live; subsequent completed first-phase
result passed. Continue the same run via bootstrap-reopen only, without recreating data/key/journal.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-88e6c4bdb0fe4c3aaab969def6fcabee.json,
run a480c4301bbc4109bface2e6ab1530dc. Preserve d207-native-bootstrap.log expiration and
 d207-native-bootstrap-resume.log. Signature and reviewed packaged ABI identity pass; owned package
uninstalled. This is initial composition evidence, not main activation/live restored references,
hardware power-loss, physical arm64/iOS, final recovery/retention/OS policy or product release.

D-207 final managed evidence: 135 infrastructure tests and all 3,309 tests pass, zero failures/skips.
Strict main Windows/Android and independent Release builds zero warnings/errors. Tests cleaned
only after both main builds completed. Logs: d207-{data-tests,full-tests,main-windows,main-android,
native-build,test-clean}.log. Driver parser passes; no unchanged UI/cipher matrices repeated.

### AT-207 — Durable restore reference ancestry (D-208)

Managed store cases cover idempotent pending intent/reopen, unresolved replacement/stale parent/
foreign profile refusal, missing/partial state and corrupt ancestry without mutation. Native actual
package interruption/referral readback is separate evidence; status alone is not key/file/prefs
authority or main startup/lifecycle acceptance.

D-208 evidence: 140 infrastructure tests and all 3,314 tests pass, zero failures/skips. Strict
main Windows/Android and independent Release AOT/trim builds zero warnings/errors. Actual Android
x64 API36/4 KiB package fixture reserves durable references before staging, externally terminates
at partial platform preference write, reopens same pending parent/target, verifies actual selected
journal/keys/files/replay and mirrors selection before protected factory publication. Original and
rollback/capsule references retained; money/device-state checks pass. The final observation expired
while PID 25465 remained live; actual result later contained all six successful flags. Record the
same completed fixture without restart: artifacts/native-sqlcipher/d208-native-restore-proof.json,
run ff097ff6eeb1401197918e20f2e57cff. Preserve d208-native-restore.log timeout evidence. APK signature
and reviewed packaged ABI identity pass, independent package uninstalled, test outputs cleaned only
after both main builds completed. Logs: d208-{data-tests,full-tests,main-windows,main-android,
native-build,test-clean}.log. No unchanged crypto/UI matrix or main startup/reference-retention/
physical/iOS/preference-durability acceptance is claimed.

### AT-208 — One-time durable restore preferences (D-209)

Managed pending/applied packet behaviours cover later preference preservation and current access
checks. Native actual package/persistence-failure/marker/cold-later-edit cases are separate evidence;
main adapter durability and lifecycle/platform acceptance remain open.

D-209 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB actual package
receiver resumes externally interrupted partial SharedPreferences writes. Injected persistence failure
occurs before Applied/reference/publication; native guard confirms marker/reference remain pending.
Real synchronous SharedPreferences Commit then succeeds, encrypted marker readback passes and
protected publication/money/device-state checks pass. Later user language/culture edits are committed;
a third cold process reopens Applied state, preserves those edits and uses a persistence callback
that throws if old values are persisted again. No unchanged fixture/matrix repeat. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b3ab782cc57a4d8eb520356be0d66168.json,
run 53b6bb4b9a1a44a3ab0dfb4ec6481bda. Complete signature and driver reviewed ABI identity verify;
independent package uninstalled. This validates the independent synchronous adapter/ordering, not
main MAUI platform durability, device/iOS, main encryption/startup or recovery/retention acceptance.

D-209 final managed evidence: 142 infrastructure tests and all 3,316 tests pass with zero failures/
skips. Strict main Windows/Android and independent Release builds have zero warnings/errors.
Test outputs cleaned after both main builds completed. Logs: d209-{data-tests,full-tests,
main-windows,main-android,native-build,native-replay,test-clean}.log. Driver parser passes.

### D-210 — Persist original Android MAUI preferences before recovery completion

The Android host now registers ISettingsPersistence against the exact Preferences.Default backend
used by MAUI 10.0.110. Native PreferenceManager selects the same default SharedPreferences; a custom
IPreferences backend is refused rather than writing a guessed file. Persistence runs off the UI thread.
A fresh device-local operational marker forces a real snapshot write, rather than an empty editor
that may skip writing. Commit failure or cancellation throws; the protected receiver must not mark
its capsule Applied or publish until this completes under exclusive preference-writer ownership.
The marker is neither a secret nor a portable preference. Windows/iOS persistence, live generation
resolution, final provider/startup/recovery and sensitive-file OS exclusion remain open. The main
application database remains plaintext; registration alone does not activate encryption.

Focused acceptance compiles the original MauiSettingsStore and native persistence adapter with
actual MAUI Essentials 10.0.110 in the independent Android package. It writes only fictitious values,
checks cancellation, terminates the process, verifies a retained value and removed key in a cold
process, then cleans its own values. This is Android x64 API36/4 KiB evidence, not phone, iOS,
hardware-key, whole-application encryption or product-release acceptance.
D-210 native evidence: original MAUI 10.0.110 Set/Remove and Android native persistence pass,
including cancellation refusal and cold restart value/removal checks. Run
098f4a594a9d4cd5b79703db501727f5; artifacts/native-sqlcipher/d210-native-cold-result.json.
The Windows PowerShell 5 observer stopped on expected temporary missing result stderr; the same
live process later produced both creation success flags. Only its cold-reopen phase was launched,
without recreating the fixture. Independent signed APK verified; package uninstalled afterward.
Strict Windows/Android/independent Release builds report zero warnings and errors.
D-210 final evidence: all 3,316 managed tests passed, zero failures or skips (3m32s).
Strict Windows (21.17s), Android (75.44s) and independent Release (77.46s) builds
passed with zero warnings/errors. Logs: artifacts/native-sqlcipher/d210-*.log.

### D-211 — Retain the restored generation through cold startup composition

SqlCipherProfileGeneration.OpenRestoredAsync resumes the actual latest package under the current
location's exclusive maintenance owner. It requires the receiver's exact private restore-history
store and installation/profile key namespace before native effects. Pending preference replay and
actual candidate/rollback/journal verification finish before retaining the exact existing key.
Readback requires the latest selected target; missing or mismatched history fails closed. No original
or initial-generation fallback, key preparation, directory guessing or early publication is added.
The retained key owner verifies the provider/integrity again and publishes through the same original
protected factory lifetime used by initial migration. Its disposal still refuses new connections.

The receiver has an internal no-publication resume path for this composition. A stale publication
callback from another process is never invoked; the current host publishes only after obtaining the
retained key owner. The public restore entry retains its existing behavior. Automatic all-profile
startup routing, main provider adoption, early staging reconciliation, optional recovery and retained
file/key lifecycle remain open. Main application storage remains plaintext. This change is not a
claim of complete encryption, device/iOS/OAuth or release acceptance.
D-211 evidence: two managed guards reject foreign history/installation before key/file effects;
all 3,318 tests pass (zero failures/skips, 3m07s). Strict Windows 30.69s, Android 56.77s and
independent Release 82.66s builds have zero warnings/errors. Signed Release AOT/trim Android
x64 API36/4 KiB runs the modified actual package restore: external partial-write interruption,
pending resume into the retained owner, real protected factory money 730 and device-state checks,
then cold Applied reopen preserving later choices. The obsolete publish callback throws if called;
both opens succeed without calling it. Exact restored binding/path are asserted before publication.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-e008a03fbb624134aa6ce18f65354aeb.json,
run bcd5d26ed6ad4da48fe20a7476f0854d. Signature/reviewed ABI identities verified; independent
package uninstalled. Test outputs cleaned after terminal builds. No unchanged native producer suite,
UI matrix or main phone APK was repeated. Main automatic startup routing/provider activation and
recovery/retention remain open; this is not complete application encryption or physical-device acceptance.
### D-212 — Resolve the current protected generation from stable ancestry

OpenCurrentAsync reads the actual existing installation/profile catalogue and requires history's
complete initial key binding. Missing/corrupt/foreign history never implies initial selection and
cannot manufacture migration state. Row zero routes through actual initial migration/reopen;
any recorded restore intent routes through the captured latest receiver and retained restored owner.
Receiver target/parent/private directory and exact history are validated, including unresolved intents.
No enumeration, latest-file guess, key replacement or initial/plaintext fallback is introduced.

The host explicitly initializes history only after verified initial migration; interrupted bootstrap
still fails closed for reconciliation. This automatic composition is not yet registered in the main
startup/provider/profile lifecycle. Native producer acceptance, all-profile live adoption, recovery
and retained-file/key policy, early staging reconciliation and final sensitive OS exclusion remain.
No additional owner decision or commercial activation follows; main storage remains plaintext.
D-212 managed/build evidence: two new guards reject missing and foreign-initial history without
creating a migration journal or invoking native key work. All 3,320 tests pass (zero failures/skips,
3m02s). Strict Windows 29.55s, Android 25.64s and independent Release 77.38s builds have no
warnings/errors. Test outputs cleaned only after all builds/tests completed. Initial native routing
uses actual retained catalogue/history and an existing-only key port whose Prepare throws; cold
reopen preserves the saved account and financial packets and refuses access after owner disposal.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-60a8738158fb4efcb3de7a38de9ee1b6.json,
run 847826db8df04477b49f168da0e073b4. Creation observation expired while PID 28905 remained live;
its same completed three-success result was read, then only recorded cold continuation ran.
No initial fixture, identity or key was recreated because of observer expiry.
D-212 restored-routing evidence: actual stable identity bootstrap supplies the initial key context,
then the current resolver reads retained history after partial-write interruption and after Applied
cold reopen. Captured parent/target assertions and actual retained factory money/device-state checks
pass; later preference edits are retained and no obsolete publication/persistence callback executes.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-26a85b00255a4e55b4bf81f4acb93939.json,
run 13141ec1f63e4c078687bea65746dbc1. Signed APK and reviewed packaged ABI identities verified;
independent package uninstalled. Both current-generation branches now have actual original-model
cold process evidence; main startup/provider/profile registration and recovery/retention are still open.
No physical-device, iOS or product-release acceptance is inferred.
### D-213 — Preserve protected publication when a profile switch is refused

The original profile switch captures its complete publication inside the actual maintenance owner,
then asks the native runtime to open/migrate the destination. On authentication refusal, retired
catalogue or failed opening, rollback restores the original path and bound connection creator.
A path-only MoveTo cannot restore encrypted storage and is no longer used for this rollback.
Rollback is one-use, requires its exact live owner, never revives retired protected context identity,
and refuses a plaintext snapshot after protection is required. The host retains underlying key
owners until the transition finishes; no credentials are exposed by the publication token.

IProfileRuntime.OpenDatabaseAsync is the explicit native destination effect. The existing default
opens/migrates plaintext storage; a protected location rejects it. Final protected hosts supply
verified keyed publication, never strip protection or recreate missing plaintext. The actual MAUI
host's protected generation resolver/lifetime registration and final startup/all-profile/recovery/
retention/provider/OS-policy adoption still remain open. Main database encryption is not activated.

Concurrent initial Android/test builds hit CS2012/MSB3248 locked shared DLLs; those negative logs
are retained. Successful Windows and independent Release builds were not repeated. Only failed
Android and full test executions were retried once, sequentially after competing builds completed.
D-213 native evidence: the original ProfileService/Store/factory executes plaintext regression plus
actual encrypted main/destination switching under source-built SQLCipher and Android-bound keys.
Active context drain, owned migration/settings, refused authentication, queued keyed original reopen
(balance 1,250 with no ledger entries) and successful protected destination/return all pass.
Authentication is an explicit fixture decision port, not owner/device biometric acceptance.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-8ad234ba8cf8479c8b10a9d36869ba85.json,
run 63d29cca400447179541651a003d2527; ProtectedSwitchAndKeyedRollback=true. Original inactive
plaintext deletion remains tested separately; encrypted file/key deletion policy is not claimed.
Signed Release AOT/trim APK, reviewed ABI identities and signature verified; package uninstalled.
Strict Windows 65.03s, final Android 29.46s and independent Release 95.28s builds report zero
warnings/errors. Successful builds and the stock native producer suites were not repeated.
D-213 final managed evidence: all 3,322 tests passed with zero failures/skips (3m00s).
Two actual-factory tests retain the original bound key creator after rollback and reject repeated,
foreign-owner or protected-to-plaintext restoration. Final builds are warning/error-free; the
initial shared-DLL lock failures remain in d213-*-lock-failure.log. Test outputs cleaned only
after all builds/tests completed. No main encryption, physical/iOS authentication or release claim.

### D-214 — Own active and tentative profile keys through the real transition

SqlCipherProfileSession retains the active verified generation while a destination is tentative.
The actual ProfileService invokes finalization after committing its reviewed catalogue or restoring
its complete previous publication. The MAUI runtime forwards opening/finalization to the registered
protected session; existing plaintext hosts retain their original path and never bypass protection.
Accepted switching clears the previous mutable key owner; refused switching clears only the
candidate after the original publication is verified. No envelope, database or rollback is deleted.

Start, switch, acceptance, refusal and close require the actual live maintenance owner. Key retirement
also requires all owned contexts to be disposed. Concurrent resolution is serialized and synchronous
finalization refuses active resolution. Publication identity must belong to the exact retained owner;
keys cannot be retired based on a path string alone. Main registration/covered async startup, early
bootstrap/replay reconciliation, recovery/retained-file lifecycle, native provider and final OS policy
remain open. The main database is still plaintext; this is final lifecycle composition, not activation.

A protected host may resolve another catalogue-bound profile in the same private directory while
retaining the previous publication. It never passes through an unkeyed endpoint. Plain/foreign-root
initialization guards remain enforced, and actual journals, history, keys and files must still verify.
D-214 final evidence: all 3,324 tests pass (zero failures/skips, 3m23s). Two new session guards
reject unresolved/unstarted state and key retirement while actual owned contexts remain. Strict
Windows 24.08s, Android 19.02s and independent signed Release AOT/trim 57.60s builds have zero
warnings/errors; builds/tests were sequential where outputs overlap, with no DLL-lock retry.

Android x64 API36/4 KiB actual original-profile flow uses stable identities, actual initial migration
and history for both profiles. Session resolves through OpenCurrentAsync with an existing-only key
port (Prepare throws), reads/retains original money, rejects authentication, verifies rejected-owner
disposal while the original remains usable, then accepts destination and return and verifies all
previous owners disposed. Session close refuses an owned context and succeeds after disposal.
Native decision port is fictitious authentication, not device biometric acceptance. Independent
proof: artifacts/native-sqlcipher/d214-native-profile-proof.json; run
9434f1b35a1d438cb3cf9f76a1d60574. Observer expired; same PID 31527 completed all seven flags,
read directly without reinstall/restart/recreating data or keys. Profile observation bound increased
to 300 attempts for its now-longer two-profile migration; parser checked without repeating the run.
Reviewed ABI identities/signature verified; package uninstalled and test output cleaned after terminal
builds/tests. Main startup/provider/registration, recovery, files and OS policy remain unactivated/open.
### D-215 — Record initial ancestry before native key migration

Initial ancestry can now be captured as pending against the exact existing Original journal,
before keys/candidates are prepared. Pending row zero survives restart but cannot reserve a
restore transition. OpenCurrentAsync resumes the actual initial migration, retains/reopens its
existing native key, then mirrors Completed journal state into selected initial ancestry before
returning a publication owner. Missing/foreign history still never falls back or regenerates.
Existing verified initial history remains supported; schema/version is unchanged and typed guards
still apply. New pending state is not evidence of a completed migration or permission to open data.

The old ordering left a crash interval after initial migration but before history creation. The
new pre-key intent closes that interval through actual captured resume. Interrupted metadata
creation itself, all-profile host bootstrap, recovery UX, provider/live registration and OS policy
remain separate unfinished work; missing or corrupt records still fail closed. Main application
storage remains plaintext. No owner decision, platform key policy or financial schema changes.
D-215 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB records the
pending initial intent before native key work, then an external process stop/start completes the
same captured identity through OpenCurrentAsync and actual migration/factory. A third cold process
uses an existing-only key port (Prepare throws), retains the saved account and money, verifies
selected initial ancestry, and refuses new construction after key-owner disposal. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-7562abeb4ea24d4b9a90fbb7011b1916.json,
run 4467935ba0f242168d5dce1f3cb9d315. InitialIntent/Created/Result guards all pass. No scenario
restart/recreation or unchanged native suite/UI matrix repeat. Signature/reviewed packaged ABI
identities verified; package uninstalled. Strict Windows 19.00s, Android 18.10s and independent
Release 55.61s builds report no warnings/errors. Actual initialization is verified only after the
pending record commits; torn/failed metadata creation itself and final main activation remain open.
D-215 final managed evidence: all 3,327 tests pass with zero failures/skips (2m55s).
Three new cases verify pending initial persistence, refusal of restore before completion and
rejection of foreign/advanced journals before history creation. Outputs cleaned after terminal
builds/tests. Main activation, torn metadata creation, recovery/retention and platform gates remain.

### D-216 — Publish complete identity metadata from captured staging

Identity bootstrap now commits and fully validates a private .creating catalogue before publishing
its final path with a no-replace same-directory move. A valid staged catalogue is resumed with its
original installation/profile/generation identities and exact known profile set; no new random ids
are generated on that path. Missing/corrupt/foreign incomplete staging fails closed rather than
being overwritten. Existing final metadata or sidecars still reject new bootstrap. Read never creates.

An explicit native durability overload synchronizes staged file/directory before rename and the
parent after publication. Managed-only callers retain transactional/typed/no-replace checks but do
not claim platform power-loss qualification. Failure after final rename requires actual existing
read/reconciliation, not another CreateNew or replacement. Other journal/history creation protocols
and main lifecycle/provider/OS policy adoption remain unfinished; main storage remains plaintext.

Independent Android x64 API36/4 KiB source-built SQLCipher probe pauses after actual fsync and typed
staged read, before final publication. The external runner terminates that process. Cold resume
requires the final path absent, publishes the same staged identity fingerprint, verifies actual
initial journal binding and idempotent/new profile registration preserving installation. No keys
are prepared, app encryption activated or scenario repeated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-58220434415e4174bc471b9760cd95e6.json,
run f93ba289610e4355aee1b0446d65bc9a. Reviewed APK ABI identities/signature verified; independent
package uninstalled. Strict Windows 26.63s, Android 17.07s and independent Release 71.92s builds
have zero warnings/errors. Script parser checked; test cleanup waits for terminal builds/tests.
D-216 final managed evidence: all 3,329 tests pass with zero failures/skips (2m47s).
Two new cases preserve staged identities after interrupted synchronization and reject a changed
profile set without rewriting staging or creating the final catalogue. Native interruption proof
is separate from these injected managed effects. Outputs cleaned after terminal builds/tests.

### D-217 — Publish complete migration and history metadata

Generation journal creation and initial restore-history creation now use the same verified staged
publication as identities. Existing/corrupt/foreign final files and sidecars are never replaced;
read never creates. Captured Original journal state and exact initial pending/selected history
must match on staged resume. Advanced journal staging cannot become a fresh Original, and a pending
history cannot be republished as verified initial selection. Typed schemas/versions remain unchanged.

Native overloads synchronize file and directory around no-replace publication. Managed-only effects
do not certify native power loss. Incomplete/corrupt staging still fails closed for reconciliation;
this does not regenerate missing keys or certify complete startup/recovery. Main provider/all-profile
bootstrap/covered async startup, recovery/retention and final OS exclusion remain open/unactivated.

Independent signed Release AOT/trim Android x64 API36/4 KiB interrupts journal publication after
actual staged fsync, resumes that exact journal, then interrupts pending-history publication after
its actual fsync. Third process resumes both bindings, retains pending initial ancestry and confirms
staging aliases gone, without preparing keys or writing financial data. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-442b5d696a9a4a8699baf81c8b31218d.json,
run 18d182139b834d9b84bd34178fd54ef1. Runner clears only its own prior metadata barrier before
its second phase; no scenario repeat/reinstallation for failure or unrelated native suite/UI matrix.
ABI identities/signature verified and package uninstalled. Strict Windows 18.74s, Android 17.54s
and independent Release 59.16s builds report zero warnings/errors. Parser checked.
D-217 final managed evidence: all 3,333 tests pass with zero failures/skips (2m46s).
Four new cases cover captured journal/history resume and rejection of advanced or differently
selected staging without publishing or rewriting it. Outputs cleaned after terminal builds/tests.
Main startup/provider/recovery and physical/platform release acceptance remain unfinished.

### D-218 — Compose preparation of all captured existing profiles

SqlCipherProfileBootstrap joins actual identities, staged journal/history creation, pre-key initial
intent, concrete migration/current-generation resolution and temporary verified key ownership for
every captured existing profile. The actual host maintenance owner excludes ordinary contexts;
private unpublished locations supply local ownership without moving the application's factory.
Only after this pass may the host's protected session publish the selected profile.

The original map must be complete, distinct, absolute and in the real location's private directory.
Existing metadata must match the captured profile catalogue. Missing installation is not inferred
as fresh: explicit initialization still refuses missing/redirected/non-plaintext originals or retained
key/migration/history/cipher state, unless resuming captured valid catalogue staging. Read-only
startup never creates identities. Missing financial databases are never manufactured here.

Native sync qualifies metadata creation; actual journals/history/current files and keys are reopened.
All encrypted rollback/envelopes remain retained, and temporary key owners are disposed before the
next profile. No intermediate application publication or fallback is added. New empty profile
creation, full restore callback/lifecycle registration, covered main async startup, provider adoption,
recovery/retention and OS policy remain open. Main application storage is still plaintext.
D-218 final evidence: all 3,337 managed tests pass with zero failures/skips (2m58s). Four new
cases reject unauthorized missing installation, duplicate original paths, changed profile capture
and missing installation with retained migration state before native key/metadata effects. Final
strict Windows 10.58s, Android 16.48s and independent Release 58.51s builds are warning/error-free;
build iterations before final guard edits are not used as final verification. Tests run after builds.

Independent signed Release AOT/trim Android x64 API36/4 KiB prepares two actual original 32-migration
profiles through the complete new bootstrap. Original main/work entries 250/880 and opening balance
1,000 remain distinct. No application endpoint changes during the pass; retained candidate and encrypted
rollback verify for both. A cold pass with a key port whose Prepare throws preserves all stable identity
fingerprints, financial packets and rollback. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-0796e72e6cdc4fe98e3783c3ce11b8d5.json,
run 6f788670f1204d3d8e32072835e6a0a1. Created/Result all eight guards pass, with no scenario restart
or unchanged native suite/UI matrix. ABI identities/signature verified; independent package uninstalled.
Outputs cleaned after terminal tests/builds. This verifies captured existing profiles, not automatic
new-profile creation, inactive restore policy/UI, main activation, physical/iOS or release acceptance.
D-219 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB stops after
actual durable key preparation and resumes the same captured new-profile intent. All 32 real
migrations execute in memory; encrypted candidate/rollback match, original plaintext path is absent.
The saved real settings row survives cold reopen. The prepared native key envelope SHA-256 is
unchanged in both later processes; the cold key port cannot prepare keys. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-02ee3d0d3285402f86b1ba06858b6c33.json,
run 0dbe974b57f94fe6975c57e9331aba71. Both packaged ABI hashes and APK signature verified; independent
package uninstalled. No scenario restart or unchanged native suite/UI matrix. Strict final independent
Release 56.86s, Windows 21.77s and Android 20.19s builds have zero warnings/errors.

AT-219 managed guards reject existing originals, missing explicit intent and unknown ciphertext.
Two initial fixture cases used a sibling directory outside the captured private database directory
and failed before exercising their intended guards. Corrected fixture paths; all 166 Data tests
pass in 1.94s. No application code changed for this correction, so native/build checks were retained.

D-219 managed verification covers 3,340 cases: the full solution run passed all other assemblies
and reported only the two new fixture-path failures (3m02s). After the fixture-only correction,
all 166 Data cases pass with zero skips/failures. No unresolved failure remains; the unchanged
assemblies were not rerun. Test output cleaned only after all builds/tests became terminal.
D-220 verification: all 3,343 solution tests pass, zero failures/skips (3m03s), including three new
actual-flow cases for context drain/unpublished preparation, failure and catalogue retirement.
Strict final Windows 24.34s, Android 18.90s and independent Release 57.27s builds have zero warnings/errors.
The first Windows attempt exposed use of an internal ownership helper across assembly boundaries;
the final adapter uses the public captured-publication ownership check. Intermediate builds are not
final evidence. The session itself still enforces the real drained owner and retained publication.

Independent signed Release AOT/trim Android x64 API36/4 KiB runs actual ProfileService creation and
switching, native key preparation, identity registration, in-memory 32-migration schema and protected
session publication. New profiles are absent from product preferences until their encrypted files
verify; creation leaves the main endpoint unchanged. Distinct opening balances 250/880 survive a
cold process and a real product switch. Native MAUI preferences are explicitly durably persisted.
Proof: artifacts/native-sqlcipher/build/android-d/d220-profile-creation-proof.json,
run f036ded6af784345bf6047ebc3f8b39e. The runner advanced from successful creation to cold reopening,
then its bounded observation expired. PID 6074 remained live and its exact later cold-result JSON
passed all three checks. No process/scenario restart or fixture recreation was performed. The
observation failure is retained as negative harness evidence, not described as an application failure.
Both APK ABI hashes/signature verified; independent package uninstalled. No layout/strings changed;
this runtime proof does not certify main UI rendering, phone, arm64/API24/16KiB, iOS or main encryption.

Main Release APK built once with Build-AndroidApk.ps1: artifacts/android-d220/pro.vafadar.zanance-Signed.apk
(83,294,522 bytes). Signature, exact pro.vafadar.zanance package, API24/36 and both ABI embedded
libassembly-store.so plus application AOT libraries verified. This uses the local test certificate,
not Store signing; no main/owner database was read or installed over for this step. It still contains
the unactivated plaintext main provider. Test cleanup follows terminal builds/tests only.
D-222 managed verification: all 3,346 cases pass, zero failures/skips (2m55.9s), including actual
StartupViewModel overlap/completion, explicit failure retry and refusal of plaintext startup with
a configured protected session. Eighteen native Windows cases cover en/fa/de, light/dark and
360x800/412x892/1280x820 at process-local FontScale 2. Thirty-six own-window captures show complete
loading/failure captions. Real native retry Invoke, at least 44px targets, actual storage/window
thread separation and complete stored table-row equality all pass. Records: artifacts/d222-startup.
The original app-data directory was restored without reading its contents; final fictitious data
was parked separately. No desktop focus/input, whole-desktop capture or OS setting change was used.

The first diagnostic compilation passed a language string where AppLanguage was required; corrected
the exact language lookup. A later matrix caller mistook stale LASTEXITCODE for a failed skipped build;
the already successful light-360 case was retained, not repeated. Remaining cases used terminating
script failures and actual proof validation. Do not erase these negative harness observations.

Final ordering review found the early reminder watcher could schedule a read after the initial
settings write. WatchChanges now attaches only after storage/access preparation. This changes only
the MAUI host subscription order, not the tested non-UI sources or reviewed layout; their passing
checks are retained. Final strict builds and cold application QA cover the host change.

An isolated functional startup package overrides ApplicationId to
pro.vafadar.zanance.startupverification; it never installs over or reads the existing Android package.
The Debug variant failed OS process attach/start timeout before Activity startup, with no managed
startup error reported. Its negative observation remains; Release QA uses the phone build settings.
This is application startup QA, not an encryption research probe or acceptance of OAuth/device/PIN,
performance, iOS, complete encryption or release. No native FLAG_SECURE was removed.
D-222 Android completion: isolated signed Release AOT/trim QA visibly displayed the actual preparing
surface, then reached the real onboarding wizard with Welcome/Language/Next and no forced account.
The process remained live. A slow observed migration was initially treated as a possible dispatcher
problem; adding the real Window.Created dispatcher hook did not establish that diagnosis. Temporary
QA-only fixed milestone/thread records showed window/start/prepare/worker/storage/settings/UI
continuation/access/ready all completed. No deadlock or dispatcher failure is claimed. Slow emulator
observations do not meet the performance target. The per-window start hook is retained with the
existing duplicate-attempt guard; its normal startup and compatibility callback are verified together.

All temporary milestone source, compile flag and project override were removed before final delivery.
The isolated QA package and exactly owned dump files were removed. Existing main Android data/package
was never installed over or read. Records: artifacts/native-sqlcipher/d222-qa-{milestones.txt,
release-ui.xml,proof-ready.xml}. Final normal Windows 20.96s and Android 69.98s builds have no warnings
or errors; earlier intermediate build timings are not final delivery evidence. No non-UI tested source
or layout changed during these host/diagnostic corrections, so the passing 3,346 cases and 18 geometry
cases were retained. The normal phone APK is rebuilt after diagnostic removal.

Final normal phone artifact: artifacts/android-d222-delivery/pro.vafadar.zanance-Signed.apk.
Package pro.vafadar.zanance, local test signature, API24/36, arm64/x64 assembly stores verified.
It is a complete Release package; main storage is still plaintext. All temporary proof instrumentation
and build flags are absent from final source. Test outputs were cleaned after all builds/tests ended.

### D-223 — Device-key protection for internal restore safety packages

BackupService now accepts an explicit IBackupSafetyCopyProtection port. Safety-only creation builds
its ZIP in bounded memory, protects it before the first storage upload and clears its owned package
buffers. Portable backups retain the independent optional password choice. DeviceKeySafetyCopyProtection
uses a separate durable platform key per safety copy, bound to the captured profile/installation and
fresh generation; it never borrows the live database key. Opening reads only the existing key and
rejects foreign contexts before key access. No missing-key preparation or plaintext ZIP fallback occurs.
The envelope uses standard AES-256-GCM with a fresh nonce and authenticated non-secret binding/header.
Creation and safety extraction bound cumulative source bytes and package bytes to 256 MiB. Failed
manifest/checksum validation clears materialized entries. This is a format/storage integration, not
a custom cryptographic algorithm or a replacement for SQLCipher.

Two real safety uploads in one second exposed the existing timestamp filename overwrite. Protected
copies now have unique opaque .vbs names outside portable discovery/retention. Generic portable pruning
cannot delete these referenced recovery artifacts. Final native registration still needs private
storage, reference-aware retention and orphan reconciliation; skipping pruning is not that final policy.

Focused Backup tests pass 54/54 and Data tests 169/169. The independent signed Release AOT Android
package uses the actual BackupService, DeviceKeySafetyCopyProtection and AndroidGenerationKeyStore.
Two protected copies restore exact fictitious content, tamper fails authentication, and a cold process
reopens both using a port that forbids key preparation. The cold observation deadline expired; the
same process subsequently wrote its successful result, read without restart. Negative timeout evidence
is retained. Native build has zero warnings/errors. Proof: artifacts/native-sqlcipher/d223-safety-native-proof.json,
run 26da5f287da548f09acdf6abd6d37bf4. The runner now gives these cold phases its established longer bound.

Main protection registration remains inactive. Main database/provider/session/bootstrap, final
Windows/mobile matrix, optional recovery, key loss, reconciliation, retention and coupled OS exclusion
remain unfinished. SEC-08 is in progress; this does not close it or authorize product release.
No visible UI changed; unchanged layout matrices and upstream native producer suites were not repeated.
### D-224 — Covered all-profile startup evidence

AT-224: two new actual app-source tests pass for quota-independent complete storage capture and
changed catalogue rejection; all 19 ProfileTests pass. Native signed Release AOT Android x64
API36/4 KiB initial and cold process runs pass using actual ProtectedDatabaseStartup, profile
parser, native keys, durability, bootstrap and session. Two captured originals are encrypted;
work amount 880 and opening balance 1000 remain correct; one publication and one resolution
occur despite repeated startup. Cold preparation forbids new key preparation.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-0745a8874d4c489eb2e71304eb953135.json.
No main-app activation, phone, arm64/API24/16 KiB, iOS, key-loss/recovery or release acceptance.


### D-225 / AT-225 — Fresh encrypted startup authority

Three actual app-source tests pass: existing plaintext cannot be replaced by new-install authority,
fresh initialization requires a real schema builder, and missing metadata cannot initialize an
existing installation. Full suite: 3,366 cases pass. Native fresh startup runs the actual 32
migrations in memory, starts with no account, then cold startup retains the explicit fictitious
opening balance 321 without key preparation. Missing identity beside retained native artifacts
is rejected even with explicit NewInstallation mode. The initial observation deadline is retained;
its same-process completed result was read before restart. Only cold verification was repeated
after the file-name guard change, using the same fixture and final signed Release AOT binary.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-96d18899dce344978fd693ea0b53cc69.json.
Main registration, recovery, retention, other device/platform and release acceptance remain open.


### D-226 / AT-226 — Native Windows generation storage

Nine real Windows tests pass for immutable preparation/reopen, missing/foreign/corrupt envelopes,
retired access, captured native flush and hard-link refusal. The independent original native
caller uses actual WindowsGenerationKeyStore/WindowsGenerationFileDurability; separate-process
DPAPI reopen preserves the encrypted Int64.MinValue packet. ACL readback has one explicit
current-user FullControl rule and protected DACL. Full suite: 3,375 pass, zero failures/skips.
Proof: artifacts/native-sqlcipher/build/windows-d/key-proof-563590e555544bed8b7802c7204859df/
key-reopen-proof.json and acl-proof.json. Observed native directory flush is not physical power-loss
or all-filesystem acceptance. Main adoption/recovery/retention and native warning review remain open.


### D-227 / AT-227 — Native startup and pooled original cleanup

Actual SQLite TEMP markers prove scoped original-pool retirement and unrelated-pool retention;
all 16 bootstrap cases pass. Windows fresh/cold and retained two-profile interrupted migration
resume use actual app startup and final DPAPI/file effects. Resume deliberately re-primes the
product idle pool and forbids key preparation; work amount 880 and opening balance 1000 remain
correct. Shared Android initial/cold startup passes with the same implementation and native effects.
First Windows cleanup failure and transient Android observation failure remain recorded; neither
was hidden by replacing fixtures. Full suite: 3,376 pass, zero failures/skips.
Proofs: artifacts/native-sqlcipher/d227-windows-startup-proof.json and d227-android-final-proof.json.
Main encryption, remaining native matrix, recovery/retention, device/iOS and release remain open.


### D-228 / AT-228 — Retry an already installed initial protected generation

Actual Windows failure is retained in d228-reproduction.log. The fixed shared Windows/Android
native scenario injects a throwing initial observer, reads real encrypted data with the retained
key before retry, refuses a changed catalogue without key reads, then replays notification for
the exact same endpoint. Later observers run and ordinary duplicate starts remain rejected.
Proofs: artifacts/native-sqlcipher/d228-native-complete-proof.json and build/android-d/
android-migration-proof-6b204a3f4b4c4c37bf842904a534ab33.json. Managed tests cover unowned protected
startup refusal and same-endpoint/foreign-owner observer contracts; full suite 3,378 pass.
Main adoption, lifecycle/recovery/retention, remaining native/device/iOS and release remain open.
