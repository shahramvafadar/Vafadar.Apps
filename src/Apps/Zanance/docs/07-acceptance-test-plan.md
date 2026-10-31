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
| AT-07 | Card purchase and card payment | ACC-04 | Unit + physical | S1 | Verified at recorded scopes: unit and D-404 original Samsung expense60/payment90/positive credit30, cold balances and two ledger details; no actual bank settlement or whole-device acceptance |
| AT-08 | Transfer fee | FIN-02 | Unit | S1 | Verified (unit, domain level) |
| AT-09 | Opening balance + older history | FIN-04, IO-12 | Unit + bounded native integration | S2 | Domain baseline retained; D-405 original own/mapped preview refuses a changed opening boundary, explicit Refresh uses current facts and native import/Undo passes on independent Windows/Release AOT Android; D-406 original Samsung picker/opening20-to18/cold durable Undo and final retained-tab Refresh qualified; broader device/format acceptance remains separate |
| AT-10 | Archive account with history/plans | ACC-06 | Unit + physical | S2/S4 | Verified at recorded scopes: unit and D-404 Samsung dirty-draft cancel/accept, both active source/destination plans ended, default cleared, archived report/history retained and unarchive/cold without resuming plans; paused variants/platform acceptance separate |
| AT-11 | Balance adjustment | ACC-08 | Unit + native | S1 | D-396 local/native; D-472 bounded Samsung original adjustment/reason/cold lifecycle verified; full variants/platform gates remain |
| AT-12 | Partial refund same month | REF-01/02 | Unit | S1 | Verified (unit, domain level) |
| AT-13 | Refund of last month's purchase | REF-03, REP-02 | Unit + bounded physical Android | S1/S8 | Unit/report baseline retained; D-402 Samsung prior-period purchase/current refunds, current/all history, cold links and account movements pass; native chart pixels and broader platform acceptance remain separate |
| AT-14 | Refund to another account | REF-01 | Unit + bounded physical Android | S1 | Unit verified; D-402 Samsung refunds35/40 into a second EUR account, recorded balances minus75/plus75 and cold original links pass; other currency/platform acceptance remains |
| AT-15 | Refund larger than purchase | REF-04 | Unit + bounded physical Android + native source corrections D-409 | S1 | Unit verified; D-402 Samsung41 with40 remaining is refused, the original draft is kept, corrected40 is accepted once and zero refundable/four public cold packets pass; no private/transient SQL or all-race claim |
| AT-16 | One-time future plan | FIN-07, REC-02 | Unit + physical Android | S4 | Unit verified; D-316 original main future 20 EUR plan appears in Upcoming/forecast without a ledger posting; broader platform acceptance remains |
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
| AT-39 | Zero / no / exceeded budget | BUD-01/05 | Unit + scoped physical | S1/S7 | Unit/screen verified; D-460 original Samsung no/zero/exact/exceeded/refund/cold/deletion scope; broader device/platform gates remain |
| AT-40 | Total and sub-category limits | BUD-02 | Unit + scoped physical | S1/S7 | Unit/screen verified; D-460 original Samsung independent total/category/refund and category-only scope; other hierarchy/platform variants remain |
| AT-41 | Monthly equivalent of yearly cost | BUD-09 | Unit | S7 | Verified (unit) |
| AT-42 | Forecast with settled occurrence | FOR-03 | Unit | S9 | Verified (unit) |
| AT-43 | Unknown amount / missing rate | FOR-05 | Unit + scoped physical | S9 | Unit verified; D-466 original physical unknown amount and explicit temporary assumption/cold isolation; D-486 fixes the reproduced legacy missing-destination conversion and qualifies original x64 native warnings/saved SQLite snapshots; physical missing-rate and other platform variants remain separate |
| AT-44 | Month-end positive, mid-period negative | FOR-08 | Unit + scoped physical | S9 | Unit verified; D-466 original Samsung end150/minimum-50, temporary end130/minimum-70 and saved scenario/cold/native deletion; wider platform/variant acceptance remains |
| AT-45 | USD purchase on EUR account | FX-01 | Unit | S1/S11 | Verified (unit, domain level) |
| AT-46 | Change report currency | FX-05 | Unit | S11 | Verified (unit: incomplete totals, no relabelling; budgets keep their currency) |
| AT-47 | Currency without / with 3 decimals | FIN-05 | Unit | S1 | Verified (unit, incl. conversion between 0- and 3-digit currencies) |
| Q-02 | 10,000 entries, 20 accounts, 100 plans | Q-02 | Unit + manual | S14/S16 | Verified (calculation budget test with the reference data set); device measurement pending |
| AT-48 | fa/de/en and digit systems | LOC-01..04 | Unit + manual | S3/S14 | Verified (unit: digits, separators, search, resource completeness and usage; snapshots of every screen in en/de/fa) |
| AT-49 | Switch Simple/Advanced | UX-02 | Unit + manual | S13 | Implemented (mode never deletes data; hidden plan and budget settings summarised); manual check pending |
| AT-50 | Drill-down equals report number | REP-01 | Unit | S8 | Verified (unit + Home and report drill-downs with the same filter) |
| AT-51 | Import with ambiguous date/decimal | IO-08 | Unit | S12 | Verified (unit) |
| AT-52 | Re-import own CSV | IO-10 | Unit + bounded physical Android | S12 | Unit verified; D-317 same own-format fixture yields zero new/two known and a disabled action; actual producer-export roundtrip remains distinct |
| AT-53 | Two identical real purchases | IO-10 | Unit | S12 | Verified (unit) |
| AT-54 | Import failure / cancel | IO-11 | Integration + bounded physical Android | S12 | SQLite verified; D-317 native picker cancel and cold persisted ordinary Undo pass; other failure/aggregate/platform matrices remain distinct |
| AT-55 | Multi-line / formula-like CSV text | IO-06 | Unit + bounded physical Android | S12 | Unit verified; D-317 imported multiline note/tag and literal formula-like title display pass; export/spreadsheet acceptance is separate |
| AT-56 | Wrong password / damaged / other app | BAK-10 | Unit (library: verified) + integration | S6 | Verified (library + integration, SQLite) |
| AT-57 | Restore on fresh install | BAK-12 | Integration + manual | S6 | Verified at recorded scopes: integration and D-62 Windows protected/unprotected/preferences/no duplicate account; D-403 original unprotected same-phone independent full Release uninstall/reinstall, native first-run preview/cancel/accept, cold and51-table/74-row re-export equality with one original account, skipped plan and Monday. Permanent Main untouched; protected-password phone/new-device/iOS acceptance remains separate. |
| AT-58 | 11th backup, failed upload | BAK-07 | Unit (library: verified) | S6 | Library verified |
| AT-59 | Disconnect / switch Drive/OneDrive | BAK-13 | Manual | S15 | Ready to run: implemented (D-35, D-50), needs a build with OAuth clients on a device – connect, back up with protection on/off (D-62), list, restore both package types, delete a cloud backup, disconnect, connect another account; per provider on Android, iOS and Windows. Google's token protocol on iOS/Windows is unit-tested (`Vafadar.Authentication.Tests`) |
| AT-60 | Upgrade with old data | BAK-12 | Integration (migrations) | every slice | Verified (integration: first schema with data upgraded to latest; restore migrates older backups); re-run each slice |
| AT-61 | App lock with notification/export | SEC-02 | Manual | S13 | Implemented (lock on start/leave, taps after unlock, export/backup/restore confirmation); device check pending |
| AT-69 | Device-wide four-digit app PIN | SEC-01..04, D-63 | Unit + Windows UI + device | Maintenance | 20 PIN cases verified (set/change/remove, salts, delays/restart/rollback, concurrency, storage failure, recovery); Windows UI: cold start stays covered, incorrect PIN rejected, one-minute delay survives restart, change/removal need the current PIN, cancelled backup confirmation creates no file; en/fa/de both themes at 360/412/wide checked. Device authentication/recents and iOS pending |
| AT-72 | Clear plan and debt setup | D-65, REC, F2-DEBT, UX | Unit + running-app + device | Maintenance | 21 new unit cases cover calendar-anchored monthly dates, first-inclusive count, short months, translations, debt signs and unsaved transfer reminders. Windows fixtures check blank Once, monthly/count, explicit rule calendar, optional debt/receivable terms in en/fa/de light/dark at 360/412/wide. UI Automation: positive debt/receivable saves open details with correct signs, negative input rejected; monthly/custom follows the first day; saved reminder has unknown principal, automatic posting off and no new ledger entry; cancellation leaves schedules/ledger unchanged. Strict Windows/Android builds and 1121 tests pass. Physical-device acceptance remains open. |
| AT-71 | Receipt purchase-total evidence and review | D-64, F2, PRI | C# + Windows image/UI + device | Maintenance | Regressions first failed on the old parser/layout, then passed; six-language valid text samples, tax inclusion, token boundaries/signs/zero, non-money numbers, currency/unit conflicts and skew/separator/page fixtures checked. Windows actual image selection: inclusive-tax/EXIF receipts give 4.10, skew gives 24.90; two totals require a manual choice, USD/EUR conflict and missing total leave the draft blank. Saved JPEG reread gives 4.10; missing-total reread keeps existing 4.10. Choice/cancel, picker cancel, unreadable PDF and failed image preparation do not change ledger/attachments. 1100 tests and strict Windows/Android builds pass; en/fa/de both themes at 360/412/wide checked. D-473 adds bounded Samsung Save/PDF attachment/cold stored reread/Cancel; full device/corpus acceptance remains ZCR-QA-05. |
| AT-70 | Android screenshot choice and ownership wording | SEC-02, PRI, D-63 | Windows UI + Android build/device | Maintenance | Settings/help/About verified in six-language resources and en/fa/de UI; Android build verified; physical recents/screenshot acceptance pending |
| AT-62 | Golden data §24 | §24 | Unit | S1 | Verified (unit: results, and their display in en/de/fa) |
| AT-65 | Allocate money to two goals | F2-GOAL-02/05 | Unit | P2-1 | Verified (unit: funded money never exceeds the balance; lower priority loses funding first; completed goals release their earmark) |
| AT-66 | Splits, partial payments, final settlement | F2-TX-01/02 | Unit + bounded native/device D-407 | P2-3 | Verified (unit: split changes the balance once, each budget sees its share; data: partial payments keep the occurrence open with the outstanding rest, the forecast expects only the rest, the final payment settles, auto-post never pays twice, delete and undo adjust the paid amount) |
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
D-369 qualifies the independent original Android Release/AOT editor; D-371 extends that bounded standalone-SQLite editor/store evidence to Samsung. Main encrypted-adapter/full-device and iOS acceptance remain open. Historical evidence: quality/destination-fee-retention.md; Android continuation: quality/destination-fee-android-lifecycle.md.

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


### D-229 / AT-229 — Whole-package restored session adoption

Actual BackupService/package/source/receiver/history/session/native keys/files restore balance
321 after a deliberate fixture change to 777. Notification failure retains the newly installed
key/data/preferences; exact continuation does not persist preferences again. Old registered
connection closes before adoption; profile-switch completion cannot accept pending restore.
Separate cold processes use existing keys and preserve later locale fr. Windows proofs:
artifacts/native-sqlcipher/d229-native-runtime.log and d229-native-cold.log; Android proof:
build/android-d/android-migration-proof-4f192702b8794260919da1951279e423.json under the same
native artifacts root. The timed-out Android activity observation did not trigger a restart.
Two new ownership/session refusal contracts pass; full suite 3,380 pass, zero failures/skips.
File-backed fictitious preferences do not qualify final MAUI persistence, main activation,
recovery/retention, device/iOS or release acceptance.


### D-230 / AT-230 — Refuse incomplete protected profile deletion

Four actual profile-flow tests preserve catalogue packet, profile row, events and artifact bytes
for protected current storage and target generation/staged-history/ciphertext state. All 23
ProfileTests and full 3,384-case suite pass. Native Windows retained two-profile fixture and
Android initial/cold case invoke actual deletion after actual session switch to main; deletion
is refused and full retained-file hashes/active publication remain unchanged. Proofs:
artifacts/native-sqlcipher/d230-native-guard.log and build/android-d/
android-migration-proof-3577a018f28e42409c0e679b11872c8f.json. Activity observation timeout
did not reset the run; separate post-proof adb cleanup failure was resolved on the same port.
Protected retirement, orphan reconciliation, main adoption, recovery/retention/OS and release
acceptance remain unfinished; refusal is not completion of native deletion.

### D-231 — Persist and resume reviewed retirement barriers

SEC remains in progress. Exact non-main private retirement requests now record ordered catalogue,
artifact and identity barriers. Resume requalifies actual native synchronization before effects,
including a phase visible after a failed flush. Nine focused cases and a two-process actual Windows
file fixture pass. This is not complete native profile retirement: final catalogue/key-reference
inventory and application registration remain open, with recovery/retention/OS/platform gates.
See D-231 in the decision log for the observed evidence and its limits.

### D-232 — Exact identity removal after retirement reference checks

SEC remains in progress. Stable identities can now be retired only for the exact non-main request
at its natively requalified artifact barrier, after reference verification and transactional full
catalogue recheck. Other bindings are preserved; observable deletion/flush failure resumes without
replacement identities. Actual two-profile SQLite removal runs in the independent Windows caller.
Final native preference/artifact/key retirement and main registration remain pending. See D-232
in the decision log for exact evidence and remaining recovery/retention/OS/platform boundaries.

### D-233 — Persist exact file inventory before protected retirement

SEC remains in progress. Retirement now retains immutable direct filenames, native file identities,
lengths and SHA-256 before catalogue removal. Native Windows cleanup refuses changed/replaced files,
preserves foreign files and its own authority, synchronizes deletion, and resumes only the existing
catalogue-removal request. Real DPAPI-envelope deletion and cold independent continuation pass.
Complete native profile/reference discovery and final app/mobile/recovery/retention/OS integration
remain open. See D-233 in the decision log for observed scope and its limits.

### D-234 — Native Android cleanup and bound directory identity

SEC remains in progress. Android now removes only captured no-follow UID-private objects through
an anchored directory descriptor, with shared preflight/requalification and fsync. Root identity
prevents accepting absence in a replacement directory on both native adapters. Actual signed Release
Android lifecycle/cold main-key preservation and three focused changed-object refusals pass.
Final complete reference discovery, app catalogue integration, reconciliation/recovery/retention/OS
and platform/device acceptance remain open. Exact observed scope is recorded in D-234.

### D-235 — Capture complete known native profile references

SEC remains in progress. Actual identity/history metadata, authenticated native generation envelopes,
exact portable backup sets and authenticated opaque safety references now supply retirement inventory.
Unknown target namespace artifacts and changed snapshots fail closed; foreign safety/main references
are preserved. Actual Windows retained encrypted fixture capture succeeds without finance deletion.
Final app catalogue/startup reconciliation, recovery/retention/OS and platform/device integration
remain open. See D-235 in the decision log for exact observed coverage and limits.

### D-236 — Actual profile catalogue retirement and pre-startup continuation

SEC remains in progress. The explicitly configured native host now connects reviewed protected
DeleteAsync to actual catalogue Set/persistence, verified inventory/files/identity retirement and
pending reconciliation before startup capture. Digests protect exact complete catalogue inputs;
no plaintext names/packets enter request metadata. Missing inventory resumes only before publication,
retries reuse one request, and unrelated catalogue changes refuse overwrite. Normal plaintext behavior
and the unconfigured protected guard remain. Final main/recovery/retention/OS/creation/platform gates
are still open. See D-236 in the decision log for exact native and contract evidence.

D-237 adds explicit durable device Add intent before native profile preparation and pending startup
continuation with the same original identity. Catalogue persistence and completion precede request
cleanup; unrelated catalogue changes refuse replay. Main encryption remains inactive; historical
pre-intent orphan reconciliation, recovery/retention/OS and platform adoption remain open. See the
decision log for terminal evidence. This does not close SEC or the full backlog.

D-238 prepares standard RSA-OAEP-SHA256 optional generation recovery, DEK-keyed HMAC confirmation,
a SQLCipher-encrypted private vault and exact durable public recovery metadata. Device key reads
never silently fall back. Actual native recovery uses fictitious original-model finance and keeps
ciphertext unchanged. See decision log for terminal scope/evidence; final UX/re-enrollment/reference/
retirement/OS/provider adoption remain open, and main storage remains plaintext. SEC-06 is in progress.

D-239 connects exact recovery mapping capture/removal to actual protected ProfileService retirement.
Target keys are authenticated before catalogue publication, only captured target mappings are removed
before native key files, and other-profile recovery/private vault remain. Missing/changed references
refuse cleanup. See decision log for terminal evidence; main encryption, final recovery UI/re-enrollment/
rotation/reference-retention/OS/platform gates remain open. No whole-SEC completion is implied.

D-240 adds explicit verified native re-enrollment and cold device-key read without using the recovery
secret, preserving the original envelope and financial ciphertext. Missing/corrupt entries never
regenerate keys. Final recovery UX/all-profile orchestration, recovered-entry lifecycle/reference/
retention/OS/platform adoption remain open; main encryption stays inactive. See decision log evidence.

D-241 authenticates recovered-key native references with a DEK-keyed exact-object receipt and
connects original/recovered/receipt removal to actual protected ProfileService retirement. Missing/
changed/staged references refuse cleanup. See terminal evidence in decision log; main encryption,
final UX/all-profile recovery/alias lifecycle/retention/OS/platform adoption remain open.

D-242 composes complete captured-profile recovery preflight and immutable native imports before
actual covered startup, without quota filtering, session publication or catalogue writes. Unknown/
missing/changed authority fails; original buffers are cleared. See decision log for terminal proof;
main encryption, concrete full retained-reference verification, recovery UX/alias/retention/OS/
platform acceptance remain open. Whole SEC/goal scope is not declared complete.

D-243: actual Android wrapping-alias loss in the independent fictitious package is now verified,
including refused original reads, cold all-profile recovery/startup, unchanged original envelopes,
no alias recreation and balances 250/880. Strict native Release build is clean; see the decision
log and device-key guide for exact runtime proof. Product code/APK remain D-242/plaintext. Future
alias lifecycle, retained-reference validation, final recovery UI/adoption/OS and device/iOS gates
remain open; no unrelated test/build/UI suite was repeated.

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

D-246 fixes required recovery candidate presence at the journal/history interruption boundary.
CandidateVerified or Selected in the real journal requires the actual candidate even before history
Selected is mirrored. Two new cases, all 3463 tests and the actual original verifier on native Android
pass; journal/history remain unchanged and no replacement data/keys appear. See decision/device-key
proof. Multi-restore financial corpus, reconciliation/UI/retention/adoption/OS remain open; main is plaintext.

D-247 verifies an actual two-portable-restore chain and interrupted native preference persistence.
Cold original application recovery verifies/re-enrolls all three generation keys, resumes the selected
second restore without an old connection/package and preserves candidate/rollback balances. A stale
fictitious catalogue setup defect was corrected; its refusal and same-fixture continuation are retained,
and a fresh complete scenario passes. See decision/device-key guide for exact evidence. Product code
and verified signed APK remain D-246; reconciliation/retention/UI/adoption/OS/device gates remain open.

D-248 aligns Android key reads with existing native ownership checks: regular, single-link and
current-UID file descriptors only. Actual normal/cold/negative key-store observations pass; hard-link
creation is denied by the emulator, explicitly not coverage of its read-refusal branch. Same-key
negative continuation and failed harness evidence are retained. All 3463 tests pass. Final recovery/
cleanup/retention/adoption/OS and external platform gates remain open; main remains plaintext.

D-249 lets explicitly verified recovery preserve a mismatching bounded uncommitted receipt stage and
publish a separate read-back/synchronized receipt attempt without replacing final authority. Actual
Windows DPAPI and four Android staged-receipt recoveries pass; all old stages remain unchanged and
main/work balances remain 250/880. All 3464 tests pass. Reconciliation/retention/final adoption remain
open; published/oversized/unsafe receipt authority is not replaced and main registration remains plaintext.

D-250 lets authenticated reviewed profile retirement capture/remove only bounded canonical receipt
stage remnants through its existing immutable native inventory and durable barriers. Missing final
authority, oversized/unknown names and key-envelope staging remain refused. Three added actual app/
Windows cases, all 3467 tests and cold original Android profile retirement pass; main balance/recovery
remain. This closes receipt-only profile cleanup; automatic retention/orphan/key-stage/final adoption
and external gates remain open. Main stays plaintext.

D-251 removes implicit native-engine selection from shared persistence. The app/test hosts explicitly
retain the existing standard bundle; the design-time bundle is private and cannot flow to app hosts.
Actual dependency graphs, real EF context tooling, all 3467 tests and strict Windows/Android builds
pass. This is a final-provider integration prerequisite; current main engine/storage remain unchanged
and cipher host/library compatibility, recovery/retention/OS/platform adoption are still unfinished.

D-252 qualifies actual production persistence assemblies, not source copies, with SQLCipher bridge/core
3.0.5 on Windows x64 and signed Release AOT/trim Android x64. Actual 32 migrations, encrypted negative
money, binary portable backup and wrong-key refusal pass across fresh process reopen. Android sandbox
inspection failure was corrected only in its distinct fixture package, preserving creation/result/data
and continuing the same key. See decision/device-key guide for proof/scope. Main/APK remain D-251;
final host/recovery/retention/OS/native platform and device acceptance remain open.

### D-263 — Original Android protected application acceptance

The corrected permanent-package Release passes original enrollment navigation, retained cold opening,
covered key loss and actual backup-picker cancellation in18 en/fa/de light/dark contexts at actual
360x800,412x800,1280x820. German recovery captions grow into complete two-line native surfaces.
No review submits recovery or writes financial records; exact held envelopes and complete financial
hashes are preserved. Earlier positive original restore/retirement/interruption cases remain recorded.

All 3509 final dependency tests pass with no failures/skips; outputs cleaned. Strict builds and complete
signed/non-debuggable APK/runtime13/native payloads are checked. [Evidence](quality/android-protected-storage.md)
separates these observations from power-loss/all-crash-points, real phone/arm64/API24/16KiB, spoken
accessibility, OEM transfer, OAuth, hosted fresh construction, Windows/iOS and Store/publication gates.

## D-264 Windows protected storage status

Windows main protected startup, DPAPI recovery enrollment/key-loss/cold reopen, isolated profile creation/switch/retirement, encrypted safety retention and explicit portable replacement have native application evidence on an independent fictional installation. Shared Android effects remain platform adapters to the same application algorithms. Final local verification: 3521 tests passed with zero failures/skips, strict Windows/Android builds without warnings/errors, complete signed APK and scoped emulator upgrade/cold reopening passed; see [Windows quality record](quality/windows-protected-storage.md). No iOS, physical-device, OAuth, Store or hosted fresh compiler acceptance is implied. The continuous-development objective remains open.

## D-265 holding accessibility and retired Home reads

Eighteen native contexts for three changed pages and eighteen distinct bottom contexts cover fa/en/de, both themes and 360/412/wide at process-local 200% typography; Cancel and original command/row identities retain financial data. The actual original Home read waits behind SQLite maintenance, retires with its Shell and completes without publishing into the old collection; replacement Home and complete stored rows verify. 3521 tests pass (zero failures/skips); native rendering is separately verified. [Bounded evidence](quality/holdings-readable.md). Final artifact/build details accompany delivery; physical-device, actual OS scaling/spoken screen readers and iOS remain open.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.

D-266 final domain/application suite: 3,526 passed, zero failures/skips, 3m24.408s. Permanent-identity strict Windows build: zero warnings/errors, 23.49s. No unchanged cipher producer or upstream corpus was repeated.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.

D-267 final native review passed all 18 contexts and 36 original Home reloads without the observed COM failure; amount handlers and complete financial rows remain unchanged. Canonical signed Release upgrade/cold/More-to-Home return passed. The unchanged 3,526-test D-266 baseline was retained; native publication is directly verified. Exact evidence and external limits: docs/quality/home-balance-reload.md.


## D-268 sale-result help and truthful connection/account copy

The planned LOC-05 help sits beside an available realised holding result and explains recorded proceeds minus recorded average cost, unknown inputs and the absence of tax rules or official tax documents. The six existing languages include a concrete example. No tax calculation, financial posting, schema or new language is added.

LOC-10 corrects the browser-return message: it describes completion of the browser step and directs the user back to Zanance for the connection outcome, without claiming token exchange succeeded. English loan pay/receive confirmations identify the selected account explicitly, matching the original session argument and the other five translations.

Store drafts now name all six existing languages and describe optional password protection for local/connected backups, readable unprotected files, Android device-protected database keys and final system-backup/device-transfer exclusion. Real OAuth, physical-device, iOS and publication acceptance are explicitly separate; no store text was published.

D-268 final tests: 3,526 passed with zero failures/skips (3m21.454s); 18 original sale-help contexts and 12 English confirmation dialogs completed without financial writes. Independent Android help and final signed Release cold Home passed. Exact scope and retained observer failure: docs/quality/sale-and-connection-copy.md.


## D-270 shared Windows native toolchain qualification

Build, upstream validation and atomic-width validation now use one reviewed toolchain resolver.
Previously the build accepted both reviewed installer patch versions while the checks accepted
only the original installer and did not qualify compiler bytes. All three consumers now require
the same pinned MSVC, SDK, installer allowlist and exact compiler executable hash before execution.
Windows CI runs the qualification check before native preparation; its dependency cache includes
the shared resolver. No compiler installation, source fork, cryptographic flag or binary changed.

Eleven qualification cases passed, including both allowed installer metadata versions and refused
unreviewed versions, paths, SDK/tools, policy and compiler bytes. Actual installation remains
18.10.12201.205: the other patch version was tested as metadata only. The current candidate's
original upstream atomic operations passed the sentinel, 64-bit carry, four-thread update and
disassembly checks; native SHA256 remains
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21.
The 704 upstream diagnostics remain recorded and broader warning/security qualification remains
open. This tooling delivery does not imply hosted Windows acceptance or device/iOS/publication
acceptance. App code, the 3,526-test baseline and signed APK remain D-268; unrelated checks were
not repeated. Evidence: eng/native/sqlcipher/Test-WindowsToolchain.ps1 and
artifacts/native-sqlcipher/build/windows-storage-d260-ec/atomic-width-51c7a432ac70474da3e3206c04cd5e5a/proof.json.


## D-272 complete local identity and private service foundation

Implemented the approved optional ASP.NET Core/PostgreSQL/private-storage backend as an independent
Zanance API project. Maintained Identity password/token providers, protected bearer tickets,
transactional one-use challenges, absolute device grants, atomic refresh generation, immediate
stamp/session revocation, shared durable request limits and private encrypted immutable objects
are connected end to end. Capacity is serialized by real owner in PostgreSQL. No local profile id
or opaque object id establishes ownership. No financial sync/sharing/billing route is implemented.

The separate migration operator and runtime role, persisted wrapped Data Protection key ring,
leased encrypted fictional outbox, resumable deletion/cleanup, content-free readiness and hidden
local start/drain/snapshot/fresh restore operations are implemented. Missing service keys keep the
original installation closed; no replacement key authority is accepted. No real email, account,
paid hosting, app connection, entitlement enforcement or publication was enabled.

Final server Release build has zero warnings/errors. Four actual-host/PostgreSQL scenarios pass
with zero errors/failures/skips on the private .NET/ASP.NET 10.0.13 runtime (6.355 seconds); they cover concurrency,
expiry/replay, ownership, quota, protected delivery, corruption, revocation, cold key loss/recovery
and real pg_dump/pg_restore. Fictional restore was about 0.7 seconds with its committed object and
revocation state preserved. Windows Kestrel and the operator's independently restored destination
returned readiness 200; exact scopes and observed failures are in quality/local-identity-backend.md.

The existing 3,526 app-test baseline and complete signed mobile APK remain D-268; no unchanged app
build, native visual matrix or cipher corpus was replayed. Local ID-01 foundation is delivered;
external sender, hosted certificate/key/backup operations, provider/legal acceptance and mobile
service adoption remain separate gates. The whole development objective remains active.

## D-273 target currencies and source-qualified holiday calendars

Deliver LOC-07/08 as one app batch. The working Western Europe scope is DE, AT, BE, FR, LI,
LU, MC, NL, CH, GB and IE, plus US, CA and AU; the broader geographic boundary question
remains open. Existing EUR, CHF, GBP, USD, CAD and AUD already have ISO minor digits of two.
No stored amount, account currency or language is changed by selecting a country. Country
registration is additive, independent of the interface language. Existing Iran dates remain
compatible; an Iran release variant is future work.

Offline holiday sources and regular dates are reviewed for Gregorian 2026-2027. Settings,
onboarding and Plan display scope, authority, reviewed years and an unverified-year warning.
Switzerland has only August 1 federal coverage; Canada uses federal nominal statutory dates;
the US uses standard Monday-Friday observed federal dates. UK and Australia contain only dates
shared by all constituent regions. Regional, workplace, extraordinary announced closures and
bank-business-day guarantees are excluded explicitly. Legacy Germany/Iran calculations remain
compatible outside the reviewed window; new country datasets do not invent future dates.
Saved Plan country/weekend masks are retained when device regional preferences change.

All 3,569 app tests pass, including 43 new cases; final Windows and signed Android Release builds
have zero warnings/errors. The original regional view passed 18 hidden own-window layout checks
(en/fa/de, light/dark, 360/412/1280 px, Debug text scale 2), complete captions and 44 px picker
height, with unchanged financial rows. Eighteen actual native selections passed in the three
English light contexts. Hidden RTL popup automation failed and is not counted as selection
acceptance. Actual signed emulator Settings selections for Canada, US and Australia, live
Persian/German/English captions, light/dark app choices and return to no holiday calendar were
checked separately through native XML. No financial entry was posted. Emulator Home remains
0.00 EUR; English, Gregorian and automatic digits/week start were restored; theme ends at
Use device setting. This is not physical-device, spoken screen-reader or iOS acceptance.

Detailed official sources, exact scope and observed verification failures are recorded in
quality/target-holiday-calendars.md. No new SDK, network request, permission, schema, paid resource
or commercial enforcement was introduced. The full development objective remains active.

## D-274 operation-scoped encrypted quick-resource reads

Measured protected Release startup after D-273: system first frames 1664/1401/1462 ms,
while loaded Home is observed later (accessibility polling upper bounds, not exact paint).
The original quick-resource operation reopened native keyed connections between EF queries.
A single DbContext alone did not improve its measured warm median (~989 versus ~999 ms).
The complete operation now captures one original context and explicitly keeps its non-pooled
connection open through both resource choices, complete input read and every original validation.
Standalone resource-choice reads also keep their own operation's connection open. No key format,
KDF, schema, guard, commercial activation, global pool or cross-operation credential cache changes.
Later explicit actions still read and validate fresh original data through a new context.

The same independently encrypted fictional fixture and actual production Data assemblies show
warm complete-read median 999.38 ms before, 82.42 ms after. First read 1220.60/565.23 ms includes
process/JIT effects and is kept separate. Wrong-key refusal, 32 migrations, negative balance and
portable read-back pass. Two actual-SQLite cases prove one captured context, unchanged rows/no
default settings or resource-choice writes, fresh action validation and drained ownership.

A Windows review launch exposed stale ApplicationId metadata despite an independent build property.
Its exact process was stopped; that attempt is not independent-installation acceptance. Touch/rebuild
and compiled identity checks precede the corrected review. Debug Home/holiday review routes now
reject a main identity before covered storage startup, without changing Release behavior. Do not
inspect or restore an unrelated installation to turn that failed attempt into evidence.

Final affected tests/build/native results and artifact identity are recorded in
quality/quick-resource-read-performance.md. Earlier full 3,571-test pass precedes the explicit-open
refinement; final Data/App tests are repeated because their production dependency changed, while
unchanged successful test projects retain that baseline. QA-06, full loaded-Home timing, large-data
native publication, physical devices and iOS remain open. The development goal stays active.

## D-275 complete Home loading and retry

Original Home now uses the existing snapshot load state from its first frame through complete
read/publication. Empty initial defaults and dashboard actions remain unavailable until ready;
an actually empty completed snapshot retains its existing next step. Six-language loading,
Home-specific failure/retry, original serialized reloads and retirement guards preserve data.
No schema, permission, SDK, key, stored preference, commercial activation or money formula changes.

All 3,572 app tests pass (zero failures/skips), with the new AT-275 final-retirement publication
case and existing initial/reload failure/retry/cancellation coverage. The final caption refinement
passes 242 localization tests without repeating unchanged successful projects. Strict Windows and
Android builds have zero warnings/errors. Eighteen native language/theme/width contexts, 36 focused
loading/failure captures, 18 actual native retries and unchanged complete financial fingerprint
pass in an independent protected installation. The first partial review exposed a wrong reused
transaction retry caption and was superseded by the corrected complete matrix.

Complete signed Android Release cold launch on the owned emulator shows preparation, actual Home
loading, then ready balance/actions in the same process; no false empty-account card appears.
APK signature/package/payload and exact artifact are recorded in quality/home-loading-publication.md.
This closes the observed publication defect, not the two-second performance objective, Android
fault injection, physical device, spoken screen-reader, OS scaling, iOS or release acceptance.
The development goal remains active; independent owner/provider gates remain unchosen.

## D-276 complete protected work-read performance batch

Complete Plan/Goal reads and fresh validation, budget choice/form reads and validation,
quick-resource validation and bounded budget-alert inputs keep their own non-pooled connection
open through the original operation. Budget form choices and inputs capture the same original
context/file. Every existing rights/stale/current-file check and writer/financial formula stays;
there is no global key-bearing pool, KDF reduction, schema or commercial activation change.

Actual production-assembly native comparison on the same encrypted fixture measures complete
read plus fresh validation: Plan 251.36 to 140.24 ms, Goal 624.61 to 180.53 ms, budget form
1298.93 to 155.51 ms, budget alerts 2281.44 to 653.78 ms, quick resources 788.64 to 146.29 ms
(three warmed medians; first use separate). Negative balance, 32 migrations, portable read-back
and wrong-key refusal pass. These are operation timings, not full loaded Home or device acceptance.
All 3,573 app tests pass with one new actual-SQLite budget context/lifetime case; strict builds,
original native Home and signed Release evidence are in quality/work-snapshot-read-performance.md.
The whole development objective and physical/iOS/reference-workload gates remain open.

## D-277 existing-user upgrade and current test-build policy

ENT-05's current policy is already implemented: the default commercial source is inactive,
with no fake Plus/Pro right or permanent/portable test entitlement. The actual first-schema
upgrade scenario writes four accounts above the approved Free quota and an expense, upgrades
through original migrations, retains identities/balance, accepts an explicit fifth test account,
and restores the real portable source into a separate fresh provider. Both original and restored
providers remain unenforced with null paid context; no resource selections are invented.

One focused AT-277 real-SQLite upgrade/restore scenario passes under strict compilation. It adds
one behavior to D-276's 3,573-test baseline (3,574 covered behaviors, not a new full-suite run).
No app/runtime/schema/dependency/UI change; no unrelated build/native matrix or new APK. The
complete signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.
Exact evidence and scope: quality/test-build-upgrade-policy.md.

Customer activation and announcement remain explicit owner/BIL gates. Do not turn disabled test
enforcement into permanent paid facts or announce future limits as already enabled. LOC-09's
final unattended protection choice and separate local personal-sync approval are pending;
none follows from backend foundation approval. The full development goal remains active.


## D-278 protected reference and tenfold workload evidence

An independent Windows executable now measures the actual protected generation/bootstrap and
production store/validation/calculation paths on deterministic 10,000-entry / 20-account / 100-plan
and tenfold workloads. Complete original schema/header/integrity/typed rows match the authenticated
pre-migration encrypted rollback after measurements, and both plaintext originals are removed.
Warm medians: all-entry reads 158.617 / 1191.737 ms; Plan read/validation 131.983 / 138.041 ms;
quick resources 127.675 / 126.937 ms; original balances/totals 1.487 / 19.066 ms.

Fresh tenfold protected preparation is 27,561.853 ms. Reference 439.797 ms is explicitly a resumed
revalidation, not fresh migration time: the original run terminated on an invalid raw export-byte
comparison, and the corrected complete logical verifier resumed the same generation/keys. The
terminal corrected run passed both workloads; strict Release compilation has no errors/warnings.
No app code/UI/schema change, unrelated test replay or new APK. Application coverage remains
3,574 behaviors (D-276 full 3,573 plus D-277 focused one), not a new complete suite run.

See quality/protected-workload-performance.md and the executable README for samples, failures,
resume ownership and exact scope. QA-06 remains partial: actual loaded Home/search/save, cold/ANR,
reference phone, Android/iOS and the two-second objective are not established by store timings.
The current signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.


## D-279 hosted Windows native qualification

Native cipher candidates now includes a Windows lane using the existing exact compiler qualification,
two fresh native builds with matching DLL bytes, original schema/tamper checks, immutable package
entries, actual production-assembly create/cold reopening and protected reference/tenfold workloads.
Only allowlisted public logs/proofs/measurements are uploaded; databases and DPAPI/key material stay
private. No candidate cache, weaker package/compiler policy, new native dependency or app change.

Actual YAML and all five Windows PowerShell bodies parse. Hosted success is not inferred from syntax
or the existing local D-278 workloads; check the final pushed commit once after the normal delay.
Existing native warnings and the separate unprivileged Windows OpenSSL-suite scope remain explicit.
No unrelated successful app/UI/full-suite/APK replay: application coverage remains 3,574 behaviors
(3,573 full plus one focused), signed APK remains D-276. See quality/windows-native-ci.md for the
exact path, artifact policy and remaining platform/security/device/release gates. Full goal remains open.


## D-280 current protected search and validated-save workloads

The independent protected executable now exercises original complete search/group and validated entry
Save on fresh reference/tenfold data. Focused ledger mode omits unchanged D-278 read/calculation cohorts;
default hosted runs include all operations. Four searches per shape check unique transfer/missing/numeric/
complete results; four actual saves per shape verify stored fields and delete exactly their own row.
Complete schema/header/integrity/typed rows still match the pre-migration encrypted rollback afterward.

Actual warmed unique search/group medians are 9.084 / 17.202 ms; validated saves 68.810 / 69.033 ms.
Search timers include result checks and exclude initial read/native rendering. Both workloads finish,
all eight temporary rows are read back/removed and both original plaintext files are absent. Strict
Release builds have zero warnings/errors. See quality/protected-ledger-performance.md for all samples,
result counts, preservation and timing limits. No unrelated app/UI/APK/full-test replay or new app
change: 3,574 covered behaviors retain their qualified baseline. Native phone/Home/search/Save, cold/
ANR, iOS, the approximate two-second objective and full goal acceptance remain open.

## D-281 automatic device-encrypted local copies

LOC-09 is implemented locally under the owner's final device-key policy. Daily/weekly/off foreground
coordination, independent per-profile timestamps and automatic/pre-restore retention, six-language
status/help/discovery and explicit original protected restore are complete. No password storage,
readable automatic file, cloud fallback, sharing action, new crypto/provider/schema or permission.

Full strict suite: 3,590 passed; final focused Data publication case: one passed (3,591 covered
behaviors, no second full-suite claim). Strict Windows Debug and Android Release builds, 18 final Windows
contexts, native encryption/reference-aware retention, six Android changed-surface/help contexts,
original explicit restore and complete signed APK are recorded in quality/automatic-local-backup.md.
The initial Android live-picker publication loop and native validation lock issue were corrected;
only related checks were repeated. Local success is not hosted CI, physical-device/iOS/OEM,
power-loss, OAuth, store-signing or product-publication acceptance. The full goal remains open.

**AT-281:** original-source and native local/emulator scenarios are verified for automatic
creation/coalescing, off/weekly/daily choices, independent clocks/retention, failed qualification,
profile-publication changes, original preview/cancel/confirmed restore, and retirement preserving
the main recovery point. Physical-device, iOS, hardware-keystore, OS/OEM and hosted gates remain open.

## D-283 complete coherent protected Home read

Initial Home and reload now use one captured context/keyed connection/deferred SQLite reader for
all original sections, then revalidate the original reviews through a fresh operation before uncovering
actions. Reuse existing quick/plan/goal guards, exact monthly SQL selection and Core calculations;
no key-bearing global pool, lower KDF, dropped history, default-row write or schema/UI policy change.
All native opening/materialization and final validation run off the UI thread. Goal Home projection
uses the same full packet and preserves every original financial row and activity choice.

On the same protected reference/tenfold fixtures, complete read/validation warm medians changed
798.06 -> 223.96 ms and 1925.59 -> 1355.62 ms, including full packet equality checks. Full logical data
comparison passes. Ten real-SQLite cases and three original application-source cases are added;
affected Data 805 / App 1186 pass. Coverage 3,604 means previous 3,591 plus thirteen new, not a full-suite
rerun. Strict builds, 18 protected Windows native contexts, same-process Release emulator Home/entry/
Cancel and a complete signed APK pass. Exact evidence and retained negative observations:
[coherent-home-read.md](quality/coherent-home-read.md). No two-second/phone/iOS/release acceptance;
QA-06 and the full development goal remain open.

**AT-283:** coherent Home source/carry/rights/concurrent-writer, original goal projection, loading/failure/native retry and explicit no-Save entry return are locally verified. Physical, spoken accessibility and iOS acceptance remain separate.
## D-284 loaded native Windows Home baseline

The original Home page now has a Debug-only native reference/tenfold review using the same retained
protected D-280 files and device keys. Twelve actual publications (en/fa/de, two each per shape)
retain every account and match complete original ledger totals; both final full schema/header/
integrity/typed-row comparisons pass against authenticated encrypted rollbacks without a financial Save.
Reference publication is 627-929 ms; tenfold is 1849-2312 ms. Largest dispatcher gap is 683 ms.
These are page navigation/read/projection/native-layout measurements, excluding cold app startup,
authentication, page construction and PNG encoding; they do not prove phone/ANR/two-second acceptance.
The appearance's synchronous layout-preference read and remaining UI-thread work remain concerns.
No Release behavior, schema, native package or translation changes. D-283's signed APK and 3,604
behavior coverage are retained; unchanged successful suites are not rerun. QA-06 remains in progress.
See quality/loaded-native-home.md for exact samples and the original native evidence.
## D-285 responsive covered Home publication

Home appearance applies layout from the same validated financial snapshot before uncovering, with
null legacy fallback, explicit default and complete custom order preserved. It no longer performs
a synchronous settings/KDF read. Original ledger scans, balances, plan occurrences/commitments,
forecast and category aggregation run on workers; formatting, collection/native updates remain on
the window thread. Original scopes, rights, complete inputs and final lifetime/file validation remain.
Thirteen focused SQLite cases pass, including three new layout behaviors (3,607 covered behaviors,
not a new full-suite run). Twelve loaded native publications pass with no synchronous captures and
complete preserved rows. Tenfold maximum dispatcher gap changes from 683 to 187 ms, while overall
publication does not improve and reaches 3.11 seconds. Eighteen native cover/failure/retry cases and
nine saved/default/legacy layout cases pass with original data and handlers retained. QA-06 remains
open; no two-second, phone, cold-start, ANR or iOS acceptance. See quality/responsive-home-publication.md.
## D-286 operation-scoped Home page cache

Native stage measurements isolate complete ordered entry reading as the dominant tenfold cost:
1485/1549 ms, reduced to 681/846 ms by changing only that connection's cache target. Home suggests
32 MiB of SQLite page cache only on non-pooled readers; pooled callers keep their default. This is not a
process-memory cap, persistent setting, global pool, KDF reduction or history/column limit.
Final original native Home publications on the same protected reference/tenfold data pass all
12 cases: reference 542-795 ms, tenfold 1351-1837 ms, complete balances and all typed rows preserved.
Sixteen related SQLite cases pass, including read-only/read-write and pooled cache isolation (3,610 covered
behaviors, not a new full-suite run). Temporary timing code was removed before final qualification.
QA-06 still requires phone/low-memory/cold/Android ANR/iOS acceptance. See quality/home-reader-page-cache.md.
## D-287 current-status alignment

Correct current summaries that still described pre-integration plaintext storage, Android OS backup,
missing loan estimation and absent entitlement/identity code. Main Android/Windows protected runtime,
recovery/safety/automatic copies, P2-18 loan estimates and local identity/quota boundaries now match
source. Retain inactive commercial enforcement, optional portable passwords, iOS/device/provider/native
warning review and release gates. Both privacy matrices and the unpublished policy draft distinguish
Android exclusion from pending iOS integration. Initial inventories remain explicitly historical.
Only docs changed; no build/test/device/APK rerun. Baseline remains D-286 with 3,610 covered behaviors,
not a new complete-suite run. See quality/current-status-alignment.md for the finite source audit.

## D-291 original protected Release/AOT Android Home

An independent x64 caller compiles the original Home/App/data sources and measures two actual native
pages in en/fa/de on protected reference (10,000 entries/20 accounts/100 schedules) and tenfold
(100,000/200/1,000) data. The complete twelve-page result and both original schema/header/integrity/
typed-row export comparisons pass. Reference publication is 1104.51-1786.76ms; tenfold is
6586.67-7225.87ms, with largest dispatcher gap 363.15ms. Tenfold latency remains an observed concern.
This excludes page construction, cold startup, fixture preparation and generation verification;
frame callbacks are not GPU/phone/two-second/ANR acceptance.

Caller-only manifest/property/registration/path-alias/locale/native-digit expectation failures are
retained. Original Android locale handling and original NativeDigits presentation are preserved;
no ledger discrepancy or financial Save occurred. Already successful English samples were retained,
and the same prepared financial files/keys continued without reseeding or observation-timeout restart.
Final Release/AOT build has zero warnings/errors; independent signed package/native hashes verify.
Main source/APK remain D-290 and 3,614 covered behaviors remain unchanged, not a full-suite rerun.
No unrelated successful test, Windows/UI/native corpus or unchanged main APK build was repeated.

The owner separately approved complete local personal Pro sync on 2026-10-15: changes/deletions,
explicit conflicts without guessed amounts, idempotency, safe restore and device management.
ZCR-SYNC-01..03 is the next complete implementation batch; paid hosting, household sharing and
commercial activation remain separate gates. This decision is recorded, not implemented by D-291.
See quality/android-loaded-home.md for exact evidence, boundaries and retained failures.

## AT-292 local personal sync wire checkpoint (unpublished batch)

Two actual SQLite clients use the original service, least-privilege PostgreSQL runtime role and
original pinned HTTPS transport. Initial publication/pull, idempotent space creation, disjoint
merge, competing amounts held for explicit review, a lost response after an actual committed
transaction, exact receipt bytes, tombstone propagation and session revocation pass. A separate
continuation reopens the same protected service TLS identity and original financial files, then
proves two offline full settlements cannot become two server payments. Explicit existing-row
choices retain one payment and the matching original occurrence state.

Initial Windows TLS failure and the focused fix are retained. No previous successful wire scenario
was repeated for the settlement continuation. Tests are staged explicitly, not ordered by the test
runner: first `*Two_actual_clients*`, then `*Duplicate_full_settlements*`, with the same isolated
`ZANANCE_SYNC_TEST_DB` and `ZANANCE_SYNC_TEST_ROOT`. Do not point them at a development/user database.
These fixtures preserve failed/successful data; see quality/local-personal-sync.md for exact files.
Application conflict presentation, first-run restore, initialization takeover, full domain corpus,
encrypted native clients, lifecycle commit boundaries, platform/CI and final app acceptance remain
open. This does not complete ZCR-SYNC-01..03 or change the last delivered full-suite/APK baseline.

The third staged continuation, `*Changed_related_payment*`, also passes on the retained files.
Missing unique-counterpart approval is refused; a concurrent change to the existing payment
requires a fresh version-bound review. Other already accepted choices remain retained, preventing
an alternating conflict loop. Fresh approval applies the actual revised amount and clears the queue.

Fourth staged method `*Original_delete_and_undo*` reproduces and then passes the actual receipt
ownership case, using the same service/files/key ring. Ordinary deletion removes the receiver's
receipt while preserving the sender's original Undo bytes; Undo restores one entry/receipt with
identical bytes. A live service orphan left by the retained failed prototype required an explicit
protocol deletion review during continuation. Four Core restore/orphan-authority regressions pass.
This is not native startup, power loss, UI presentation or a completed sync feature.

### AT-292 unpublished attachment review extension

The real PostgreSQL/TLS case proves both exact attachment versions before a decision, original
file/money preservation, size bounds, tampered-stage refusal and stale preview rejection. Four
original app-flow cases cover optional preview, explicit unsupported-file choice, page navigation,
transient cleanup, retirement and concurrent edits. Six independent original Android Release cases
cover image/two-page PDF/native controls in en/fa/de light/dark. Evidence and exact limits are in
`quality/local-personal-sync.md`; complete native network sync, final whole-batch checks, 360/wide/
Windows runtime, physical/iOS/provider/Store gates are still open. No fresh full-suite count is inferred.

### AT-292 unpublished native HTTP and storage operations extension

Separate original protected Windows HTTP/cold-session and Android Release/AOT/trimmed HTTP/cold
process scenarios now pass basic transfer, exact attachment bytes, explicit competing money and
original tombstone propagation against the actual local service. Earlier caller-only manifest,
path-alias and first-run-mode failures remain with their same-fixture continuations; these are
distinct from page-only review or production defect claims. See `quality/local-personal-sync.md`.

One evolving real PostgreSQL/pinned-TLS operations case passes owner-wide staging/attachment bounds,
exact retry at capacity, abandoned nonce preservation, private orphan-only collection and actual
cross-worker publication commit/rollback. A worker observed waiting for the owner lock preserves
parts after session-row renewal; actual expiry and explicit device revocation retire only
unpublished staging while keeping committed financial rows/events/receipts. No unchanged native
or UI cases were repeated for server-only changes. Complete-batch domain/protocol/operations and
final build/test/UI/APK/documentation/commit/push remain open; no full-suite count is inferred.

### AT-292 continued: domain refusal, existing state and Linux persistence (unpublished)

Fourteen inconsistent planning proposals are refused over actual PostgreSQL/pinned TLS without records/events or
cursor publication; the valid complete graph reaches a second original client. Negative earmark releases, zero budgets
and empty contribution scaffolding stay valid. All 421 cases in the eleven snapshot classes affected by eight
new metadata tables pass; four identity scenarios pass explicit HTTPS and retain an HTTP refusal assertion.
Linux non-root qualification passes four cases, including a second OS process reopening retained protection/TLS
and financial state. Bounded XML reports and original fixture files remain. Hosted CI, final integrated app/UI/APK,
physical device/iOS, public deployment and overall D-292 completion are not established by these passes.

### AT-292 managed continuation and integrated build status

The managed solution now has 3,690 covered executions across thirteen projects, reconciled from the retained failed
initial full attempt and focused continuation (`sync-final-coverage.json`). All 1,258 App cases are covered:
648 initially passed and were explicitly excluded; 610 failed/unexecuted cases passed after the six snapshot-helper
corrections. Only the failed source-policy caption case was rerun in Core. This is not a fresh full-suite pass.
The personal-sync-enabled integrated Windows build passes strictly with no warnings/errors. Android APK/final native
UI/geometry, complete batch publication and hosted CI remain separate pending checks.

### AT-292 final native first-run/comparison and budget boundaries

Passed locally: 36 original Windows contexts (en/fa/de; light/dark; 360/412/1280) in the independently named protected
installation. Complete native captions/glyph allocations, original UI Automation first-run Back, disabled finish
without received accounts, exact comparison command parameters, native names/44-pixel targets and complete financial
fingerprints pass. Nineteen invalid complete planning graphs are refused by actual PostgreSQL/TLS without events,
records or cursor publication; the valid complete graph reaches the original receiving coordinator. Weekly zero
month/year remains valid. Final strict original main-identity Windows rebuild passes. Retained Android native cases
and the managed 3,690 coverage are not repeated. APK/publication/hosted CI evidence is recorded separately; these
passes do not grant physical-device, iOS, public service, E2EE or release acceptance.

### AT-293 - Isolated current-machine deployment

Locally qualified: original financial clients across process restart; public-chain positive/refusal; forged proxy
headers and missing/wrong-role database certificates; runtime DDL/history refusal; full quiesced PostgreSQL/private
PDF/key-ring restore to a separate target; retained-key TLS renewal with original auth/finances/PDF readback;
missing-key and fictional-public-promotion refusal; completed restore without replay. Fifteen API boundary and twelve
Data peer cases pass. Original Windows36 contexts/18 help dialogs and Android12 contexts/six help dialogs preserve
complete financial snapshots, secure flag and no opt-in. Strict main builds/current signed APK pass. Hosted CI,
public routing/mail, phone/iOS and release remain separate. See quality/self-hosted-stack.md for negative evidence.

### AT-294 - Household E2EE batch (in progress)

Qualified independently so far: standard maintained cryptography/pinned membership; original protected native journal,
financial capture/conflict/apply, bounded encrypted transfer/interruption and selected-source atomic copy; actual
accepted roles, local EF/SQLite guards, borrowed transaction expiry, scoped revocation and read-only service grace.
Original app workspace additionally passes exact retained genesis replay after real committed-response loss,
private two-sided invitation/device approval, viewer receive, owner-only pending-proof refusal and approved editor
publication on two actual SQLCipher/DPAPI clients over HTTPS/PostgreSQL. Original app foreground dispatch publishes
one real protected edit without readable personal transport. Actual setup refuses a profile switch during identity
HTTP before writing household keys/binding into the other protected profile. Six new app-flow cases pass separately.
See enhancements/2026-10-commercial-release/11-household-delivery.md for retained negative evidence and continuation.

Not yet accepted: native rendered household forms/roles/source copy/conflict UI, optional owner recovery, complete
transfer/audit/deletion/retention/cost persistence and UI, bounded whole history, actual Android/AOT, final signed
main APK, whole D-294 gates and physical-device/iOS/public service/purchase/release. Keep the complete batch open.

AT-294 actual-cost continuation: native full-contract two-client cost/share/refund/repayment roundtrip, stale/
partial-sum rollback, original app editor Save/culture/draft retirement and cold same-key reopening are qualified.
Three focused actual-SQLite transaction cases and19 focused Core behaviours are covered separately, without a
complete-suite rerun/count claim. Corrected Windows cost UI passes18 en/fa/de/theme/width cases and unchanged
complete stored rows; the first wrong-identity/configuration/viewport attempts remain negative evidence. No fake
household membership was used for layout. Android cost/AOT runtime, household source/conflict/recovery/transfer/
audit/delete/retention/history, current full APK and complete D-294 acceptance remain unfinished.

D-294 optional owner recovery now has actual same-fixture native evidence for original encrypted export intent,
committed archive prefix/process exit31 and exact continuation; independent new-device/root commit/process exit32
and identical-request continuation; complete original financial receive without cash reposting; old-device404;
cold reopening without kit import; active-kit ordinary membership/recipient verification; and kit retirement that
changes both owner key roles. Logs and retained failures are listed in11-household-delivery.md. Three dedicated
kit/material cases, one later owner-device/component invariant and one updated app password-retirement case pass
separately. Strict main Windows28.12s and Android59.79s compile with zero warnings/errors.21 recovery/help strings
have six real translations. Running Save/Pick/Cancel and full page/mobile/whole-batch acceptance remain pending;
these observations do not create a new complete-suite count or a physical-device/iOS/release claim.

D-294 continuation: independent Android Release/AOT actual native Save/Pick/Cancel passed with identical encrypted
kit readback and no INTERNET/storage permission; this supersedes only the adapter-pending portion above, not main
page/lock/network acceptance. Original Windows source selection/review passed18 en/fa/de, light/dark,360/412/wide
cases with actual Toggle/Invoke and exact full data restoration. Four unfinished cases resumed after observing
native layout before caption measurement;14 successful cases were not repeated. Maintenance-source handoff and
same original draft/key/pending-copy cold refusal passed separately. The draft's initial test stopped on the correct
cryptographic refusal because its assertion caught a different type; no data/key reset occurred. One new source
flow test and the changed household readable-route refusal test passed separately. No full-suite recount is claimed;
destination copy, authenticated cost conflict choices, main Android/AOT and whole-batch release gates remain open.

Subsequent D-294 cost conflict evidence supersedes only the authenticated application-source choice gate above:
the same recovered owner and approved editor staged a real complete cost conflict, the actual common VM/presenter
reviewed it in en/fa/de and explicitly chose the remote group, and the encrypted coordinator applied it. The
original note then returned through ordinary encrypted sync; exact money/shares/refund/private repayment and zero
cash entries remained. Actual rendered conflict/inline-file controls and main Android remain open. Current strict
Android compile passed without warnings/errors in28.63s; focused test outputs were cleaned. No new complete-suite
baseline, final APK, main household completion or physical-device/iOS/release acceptance is claimed.

Subsequent D-294 main Windows source/profile/publication gate passes on the actual independent protected app:
native account/history selection, original review/consent/flyout/prompt, actual durable profile creation/drained
source check, fresh destination copy consent, exact selected financial copy/new ids, actual owner membership and
encrypted-only publication, complete unchanged source fingerprint and same-key target reopen.29 native operations
were retained at412/light; the prior18 source-control layout cases were not repeated. Initial native observer
class/provider assumptions and host deadline were retained, and only the same incomplete source flow continued.
The generated test credential frame was deleted after completion. Main Android, full dialog variants, recovery
lock/file composition and remaining household gates are still incomplete; no new complete-suite baseline.


AT-294 continuation: retained native second-owner-device enrollment, wrong fingerprint refusal, exact approval, private-root retirement, lost membership acknowledgement and cold original-key continuation passed. A separate original pending-member claim survived an unrelated role update and completed before explicit removal. Original Windows approved/pending-device UI passed18 new layouts; current-root display and native confirmation finished the exact retained invitation, with complete allowlisted financial comparison in the distinct root case. Prior completed cases were not rerun. Full household/mobile/lifecycle acceptance remains open.

D-294 current owner-device continuation: current main Windows and Android strict compilation passed with zero warnings/errors; original application-source test-host strict compilation passed. No unrelated suite or completed layout case was repeated, no full-suite count increased, and no final main APK/phone/mobile acceptance was claimed.


AT-294 continuation: new same-key device adoption passed actual exit36/lost enrollment response, fresh actual fictional session, pending refusal, exact approval, full encrypted costs/shares/private repayment/private-root adoption and separate cold reopening. One new focused trust test passed. A native rolled-back pre-adoption writer counterexample was fixed; actual exit37 after receive-connection commit and same original first receive passed with full source financial/settings digest unchanged. Main native export/request/Cancel/admission/cold and18 new control layouts passed; owner fingerprint text entry is explicitly not accepted on hidden Windows. Changed resource completeness/placeholder class passed21 cases; full-suite baseline was not rerun or recounted.


D-294 ownership continuation remains part of the unfinished household batch. Exact old-owner offer, actual approved
target-device/new-root acceptance and original-source final publication now have native exits38/39/40/41, public
post-removal receipt, unchanged financial columns and cold committed-key/writer evidence. The general membership
endpoint cannot bypass actual target-device consent. A real encrypted startup schema-upgrade omission was fixed on
the same original app file/key; two callback regression cases and actual native status continuation passed. Eighteen
new status-only Windows layouts, three role/checkpoint trust cases, one changed actual service case, one model case,
21 resource-integrity cases and one literal-key case passed; no complete-suite baseline was recounted. Main complete
transfer prompts/mobile/recovery/file/conflict, target catch-up, audit/deletion/retention/history and whole-batch gates
remain open. See11-household-delivery.md for exact scope, retained failures and current original fixtures.


D-294 ownership reception continuation: a real target prepared ownership keys after downloading ciphertext but before
applying newer saved finance. The same original candidate/operation now completes bounded incoming-only financial
adoption. A fresh Offered-stage case receives before generating keys; wrong-operation/ordinary rotation sync and
normal writes remain refused. An already applied exact horizon is idempotent. Real Viewer and Editor former-owner
transfers passed with no current private-root delegation and complete original financial digests restored by actual
Save/sync. No successful previous suite/layout/cost/device case was repeated. Complete main transfer/mobile/lifecycle
and whole-household acceptance remains open; see11-household-delivery.md for retained failures and original fixture
state. No new complete-suite count or final APK is claimed.

D-294 lifecycle is in progress: focused actual service, native process-loss and real old-PostgreSQL-snapshot continuation passed with permanent access revocation, original intent/key/data preservation and exact-room retirement. Audit signature/frozen-prefix and authenticated expired-request finishing also passed. Main lifecycle UI has18 new native Windows Active/no-deletion status cases; destructive confirmation and other states remain unaccepted; mobile, complete lifecycle/identity erasure, operational latest-marker restore composition and scale/history gates remain. No previous successful unrelated suite was rerun. The complete-suite baseline is3702; no final D-294 APK or release is claimed. See11-household-delivery.md for exact fixtures and negative evidence.
The subsequent original main Windows lifecycle/audit status review completed18 new language/theme/width cases (fa/en/de, light/dark,360/412/1280),54 own-window captures and7992 native observations. Actual native Discover, lifecycle and audit reads preserved every financial column. Initial hidden-page publication/disabled-peer failures were retained; no protected file/key was reset. A bounded publication check and explicit independent ApplicationId build preceded completion; the broader lifetime cause is not proven. This is actual Active/no-deletion status acceptance only, not destructive confirmation, other lifecycle states, audit history completeness or Android acceptance. Strict current Windows and Android builds passed with zero warnings/errors;23 resource completeness/literal-usage cases passed. Successful earlier service/native/suite cases were not replayed. The whole household batch is still unfinished and uncommitted.
The operational historical-restore code now requires the exact latest stopped original private source and immutably merges its deletion markers before target startup. A freshly built capability-labeled service image passed the offline merge as nonroot with read-only root, no network and no capabilities; Windows idempotence, conflicting-original preservation and invalid-directory refusal also passed. Source/target markers are validated without transferring any key/mail/financial payload. Legacy images cannot serve newer permanent deletion authority. This closes the missing code path, but the full new compose/snapshot operation has not been executed; do not mark that operational acceptance or identity-erasure composition complete. No successful D-293 full hosting qualification was repeated merely for these targeted command cases.
D-294 full operational privacy restoration now passed on independent source/target Docker stacks, including actual complete snapshots before/after deletion, live-source refusal, latest-marker merge before old snapshot startup, permanent deleted-room receipt/retirement and unchanged independent-room ciphertext decryption. A concrete configuration omission was fixed: local-household opt-in now survives backup/restart/restore without enabling it publicly. No successful old D-293 hosting stage was rerun.

A separate real retained-owner counterexample showed that the original deletion workspace depended on forbidden financial refresh. The same original protected files/keys and31-day retained service state now complete owner-only opaque privacy checkpoint review, same-device fresh-session possession, original consent and retirement without fetching financial data or epoch grants. Actual financial/key resources and normal local writes stayed refused; every local financial column was preserved. The existing historical trusted root signs only this exact privacy request; newer opaque checkpoint metadata never grants roster/epoch/financial authority, and a missing/different root fails closed. Main confirmation refetches the exact reviewed checkpoint before persisting consent. No schema, dependency or resource changed, so no model regeneration or unchanged resource/layout matrix was repeated. New strict main Windows/Android builds passed; whole D-294 and native destructive-dialog/mobile acceptance remain unfinished. See11-household-delivery.md for current fixtures and exact scope.

### D-294 progress: original main privacy lifecycle

Scoped main Windows/Android Debug owner lifecycle passed: native cancellation without intent, actual31-day retention, original confirmed deletion, cold same-operation/hash/time receipt, current remote physical retirement, preserved local financial baseline and unrelated service record. Android en/fa/de and light/dark dialogs supplied twelve real native buttons with48-pixel minimum observed dimension. Windows changed status layouts cover360/412/1280. Completed cases and their keys/data must not be reset or replayed.

Seven focused actual-service temporal-validator cases cover the reproduced two-second future-clock counterexample and unchanged expiry/future rejection. They do not imply a new complete-suite run; baseline3702 remains separate. The final deleted-state deadline/audit presentation is being checked after its code change. Broader main source/transfer/recovery/conflict, identity erasure, malformed-marker isolation, scale/history, full signed Release and physical/iOS gates remain open; see enhancements/2026-10-commercial-release/11-household-delivery.md.

D-294 final changed-state follow-up: both strict builds passed. The changed Windows deleted-state layout passed18 cases with2,844 native observations and unchanged local financial columns. The new signed complete Android Debug binary then reopened the original retired receipt/local ledger, rendered no obsolete automatic deadlines, disabled the actual audit button and kept lifecycle metadata enabled. Original operation/hash/time and local money were unchanged, without another deletion or receipt request. This verifies the changed display/control gate only; six earlier native confirmation cases and unchanged active layouts were not repeated. Whole D-294, final Release/AOT, physical-device and iOS gates remain open. Canonical details and immutable evidence identities are in enhancements/2026-10-commercial-release/11-household-delivery.md.

D-294 main Android recovery-file continuation: the original main page now passes actual native Save/Cancel,
separate explicit enrollment, native Pick, wrong-password rejection without a draft/financial change, retry of
the same selected encrypted file, completed ownership/history recovery and cold reopening in a fresh protected
profile. All financial columns match the source; the former device is revoked, and returning to the completed
original lifecycle profile preserves its full financial hash. Both strict platform builds pass. No unchanged
suite or completed lifecycle/adapter cases were repeated; complete-suite baseline stays3702. This is Debug
same-installation separate-profile evidence, not device-loss/hardware, Release/AOT, iOS or whole-batch acceptance.
See11-household-delivery.md for exact fixtures and immutable public observations; D-294 remains unfinished.

D-294 retirement-isolation continuation: the actual PostgreSQL worker now preserves unverifiable original room
markers while rotating both finite queues and completing a later unrelated expiry. Unknown raw names advance
discovery but still refuse offline privacy transfer; no marker is deleted, replaced or treated as absent authority.
Five focused file/worker/database/transfer cases passed, with zero-warning strict API/test/caller compilation.
Successful prior suite, native lifecycle/UI and full hosting restore cases were not repeated. The complete-suite
baseline remains3702. Final composed hosted image and other D-294 gates remain open; see11-household-delivery.md.

D-294 main functional ownership continuation: actual Android/Windows main pages now complete invitation/device
approval, target-created independent keys, both-sided consent, explicit former-owner Leave, publication/receipt
adoption and cold owner reopening with identical full financial columns. Actual current root/epoch grants include
only the new device, the old kit is removed, former devices are revoked, and both original main profiles remain
unchanged. A real cold404 UI defect is fixed: known selected household context survives refused rebind rather than
exposing fresh setup/recovery. Six native en/fa/de light/dark cases on the same process keep sync/setup blocked and
financial columns unchanged, using cached-state Reload rather than repeating service rebind. Both strict builds
pass; unchanged successful suites and protocol/recovery/lifecycle cases were not repeated. Complete-suite baseline
stays3702. Final UI consolidation, other main gates and whole D-294 remain open. See11-household-delivery.md.

D-294 main existing-owner second-device request/approval and original cold adoption now pass on the retained
Windows/Android main pages and actual private service. Pending has no epoch/root grants; actual native confirmation
precedes approval. Original full financial hash matches after warm and cold receive; original Windows main stays
unchanged. See11-household-delivery.md for exact identities, preserved negative prompt/publication evidence and
scoped signed Debug APK. This does not qualify main revocation/conflict, final Release/AOT, phone or whole D-294;
no unchanged complete suite was rerun and baseline3702 remains historical.

D-294 main device/cost-group continuation: actual existing-owner device approval and cold receive, two actual
native competing cost Saves, one complete three-row conflict decision,18 new en/fa/de/light/dark/width cases and
original native delegated-device removal now pass. Both full financial hashes match; private costs post no cash
and accepting the remote group makes no new financial publication. A real warm stale-access hint is fixed by
publishing only the already recorded local refusal. Actual404 checks in both main platforms close sync/admin/
audit/setup and preserve data/keys; Android's actual Sync target is disabled. Both original main datasets remain
unchanged and review processes are stopped. See11-household-delivery.md for exact evidence, negative fixtures,
standard signed Debug APK and scoped denial versus signed removal. No successful unchanged suite was rerun;
baseline3702 is retained. Final storage/history/composed-image/Release-AOT and whole D-294 gates remain open.

D-294 history continuation: scoped original native warm/cold12009-version/96-page reads,680480-byte framed row,
old-baseline conflict, corrupt-position refusal, missing derived-position reconstruction and all11 private-table
portable scrub checks pass without changing original money or keys. Eight framing tests and one compiled-model
match test pass; these focused runs are not a new full-suite count. Main Android upgrade and final whole-batch
acceptance remain open. See11-household-delivery.md for exact fixtures, limitations and negative evidence.

D-294 composed service image5dec4a83 is running with preserved original checkpoints and complete pre-upgrade
snapshot. Current owner/member identity RESTRICT references are validated; no identity erasure is claimed.
Combined3753 existing-filter cases pass:2854 passed initially, only899 stale-inventory failures were corrected and
rerun exactly. This is not a second full run;8 separate framing cases remain scoped evidence. Compact protected
export now omits private history before allocation, preserves complete original financial values, and passes
native budget-refusal/source-reuse checks. Android AOT and rebuilt final artifact are recorded separately.

### D-294 final local delivery

The complete local household batch is implemented and qualified together: protected E2EE membership/device/key
and optional recovery lifecycle; selected separate financial adoption and complete costs/conflicts; two-sided
ownership transfer; audit, permanent privacy intent/retirement and latest-marker-aware full service restoration.
Final service composition, bounded historical row reads, compact pre-allocation portable scrubbing, real main
Android upgrade/export, separate Release AOT/trimmed native allocation and native transfer-dialog geometry pass.
The original full run plus only899 exact failed-case reruns yields3753 passing existing cases;2854 prior successes
were not repeated. Separate focused cases do not change that total. Final product APK:
artifacts/android-household-final-d294/pro.vafadar.zanance-Signed.apk;91356917 bytes;
SHA2566ebe1f0d8f963f1c1f18cd2ecee7d03a4a5cfc5325d25e3883bdeee960ac38f2; signature/permanent id/arm64+x64 verified.
Exact scope, retained failures and runtime evidence are in enhancements/2026-10-commercial-release/11-household-delivery.md.
Earlier in-progress notes are historical. Public/commercial activation, physical/iOS, store/provider/signing,
external security review and publication remain separate gates; identity erasure and ciphertext recall are not
claimed. Continue the already approved complete billing/future-web-prerequisite batch without helper-only slices
or repeated unrelated successful checks.


## AT-295: local billing composition and remaining store matrix

Local evidence passes for signed original device/current/operation separation; canonical ownership and exact
idempotent lost-response continuation; explicit local Plus versus actual identity Pro; native receipt/product/
replacement binding; deferred current/future terms; clock/cache/refund/outage boundaries; private-state recovery;
actual PostgreSQL/HTTP sessions, cross-distribution identity rights and other-user refusal. Source-linked actual
view models preserve financial rows and all six translations. Changed original Windows native geometry/Cancel
and main Android signed Release upgrade/current unavailable state are separately recorded in
enhancements/2026-10-commercial-release/12-billing-delivery.md.

Pending external/native acceptance: actual Google/Apple sandbox catalog/purchase/renewal/restore/refund/hold/grace/
revocation and trial/discount matrix, native StoreKit2 implementation/Mac build, real credentials/accounts, public
notification delivery, store signing, physical-device/runtime traffic and final commercial activation. Local fixture
facts and negative Apple signature cases do not pass those rows. Unchanged3753-case prior baseline was not replayed.


## AT-296 Germany accountant data/document export

Actual SQLite/source-flow/native-port acceptance has23 new cases, including selection, one-row cross-currency
transfers, refund links, default minimal disclosure, exact stored document bytes/hashes, no unrelated year/account/
orphan data, no source writes, stale original source/day/scope/rights refusal, native-await retirement/lock,
unknown precision/Int64/formula text and explicit whole-packet refusal for oversized/invalid choices. Only the two
changed document-index packet cases were rerun after its streaming refinement. The policy76-case matrix and
resource checks are separate focused evidence; historical full-suite3753 is not a newly executed total.

Native Windows18 en/fa/de/theme/width cases passed original selection/review/Cancel, caption/target checks and
unchanged complete financial fingerprint. Long native title clipping was observed, retained and corrected with
a short translated title; affected presentation was rechecked. Complete signed Release Android package/local
native checks are recorded in13-accountant-export-delivery.md. Genuine device/iOS/recipient/expert tax acceptance
remains separate. No payment, tax filing, financial write, credential or background upload belongs to this flow.


## AT-297 Documented bank-software ordinary cashflows

Focused source-flow qualification:29 new BankCsvFlowTests plus32 affected existing DataFileOutcomeFlowTests;
23 resource completeness/usage checks. Six-language consent/Cancel/import/reimport/new-model durable Undo,
complete paged review, explicit legacy/UTF-8 encoding, exact currency/minor units, unknown/split/malformed source,
invalid rows, stale finance/account/retired-form/payload and forbidden loan/asset target rejection are covered.
This is not a repeated full-suite run or actual bank-account acceptance. Native Windows/Release Android and
package results, including original failed fixture preparation, are recorded in enhancements/2026-10-commercial-release/14-bank-csv-delivery.md.

## AT-298 Optional personal analysis, consent and original credits

Actual application source/SQLite verifies selected aggregate math, per-currency refund/reversal rules, absence
of identities/notes/documents, coherent unchanged financial data, source-change refusal and native-effect gates.
The application-flow port checks manual Cancel/authentication, original scope/form/cover retirement, uncertainty,
provider consent/continuation and fresh balance versus cached history. Hostile/null/bounded output stays text.

Independent PostgreSQL stages verify protected original intent, immutable append-only credit events, global
receipt ownership, gift-first reserve/actual settle/refund, no double dispatch, metadata-only durable closure,
insufficient original consent and cold result. Actual Kestrel pinned TLS and identity middleware qualify private
client persistence before HTTP, same-id absent replay, lost reply continuation without another financial POST,
unusable output retention, expiry and session revocation. Separate worker stores qualify the actual owner lock.
The closed default has no automatic provider/model or public grant endpoint. Existing resource completeness/
usage checks cover all six languages. Native UI, actual Android private adapter/share and signed package state
remain separately recorded in15-ai-local-delivery.md; only its completed stages are accepted. Whole-suite3753
is historical, not a newly executed count. Real provider/credit purchases/public/iOS/phone acceptance is open.

## D-299 original reference acceptance (AT-299)

Sixty-four distinct focused unit/source cases plus one retained real TLS/PostgreSQL two-client personal
acceptance fact qualify bounded lookup/cache, exact source/target/conversion, immutable history, original-id lost
reply continuation, authority refusal and compatible signed shared source content. The independent protected
household executable also qualified three actual processes, original ciphertext/commit-reply continuation,
Viewer reception/refusal and cold exact history without readable personal financial HTTP. Eighteen actual
Windows en/fa/de light/dark360/412/1280 cases preserve the financial fingerprint through native stale consent
and Cancel. Android private adapter first/cold processes passed exact64KiB, isolation and refusal/preservation
checks. Resource completeness/usage23 and runtime enum keys6 are separately recorded existing checks.
Exact current native/package/main-app state and retained failures are in16-market-reference-delivery.md.
These are not a new whole-suite total, live-source/physical/iOS/performance/public-release acceptance.

## D-300 Actual startup and unchanged native navigation

Qualify the original retained protected installation through cold Activity, covered preparation, loaded Home, expense opening and native Cancel without Save. Compare actual process reasons; do not replace Home readiness with Activity timings. No new unit behaviors or historical full-suite total are asserted.

Current exact evidence: enhancements/2026-10-commercial-release/17-startup-responsiveness-delivery.md.

## D-301 actual native storage acceptance scope

Two fresh outputs per local native target match exactly. Required original storage behavior, complete current
50 migrations, Unicode/exact money/date, wrong/missing-key/tamper refusal, typed memory export/staging and retained
old Windows DPAPI data reopening pass. Official configured OpenSSL and stock SQLCipher suites retain all actual
skip counts and do not waive remaining diagnostics. The original main Android installation upgrades to the
ordinary signed new package and reaches its original loaded Home without data/key reset. Hosted servicing and
final navigation evidence are recorded in18-native-storage-profile-delivery.md. No unrelated successful
application, financial, sync, provider or layout suite was replayed; no new whole-suite count is asserted.

## D-302 complete original currency fields

The common editable Syncfusion currency input now explicitly retains native text scaling and a visible
44-unit real list button with the original translated field name. Original filtering/selection stays in the
vendor control. Its original currency rows have scalable app typography, real native actions and a translated
selected-state description. Amount/currency groups in transaction, goal and manual-rate forms use complete
rows rather than fixed 110-unit or split currency columns. Drafts, bindings, field errors and explicit Save
remain unchanged; code selection does not save money.

All 18 English/Persian/German light/dark 360/412/wide contexts qualify original native input and list actions
at the requested process-only 200% font stress, including the three actual constrained forms and complete
unchanged isolated account/entry rows. Current strict builds, signed main Android upgrade, native list/selection and typed search
and Cancel evidence are in19-currency-accessibility-delivery.md. No unrelated successful whole-suite, native
crypto, synchronization, billing or service qualification is replayed. This is no new unit-test count or actual
OS-scale, spoken screen-reader, physical-device, iOS or release acceptance; A11Y-03 remains In progress.

## D-303 complete ledger read without discarded identities

Home reads the entire ledger in physical row order, then applies the original stable newest-first date/time
order in memory. This avoids repeated encrypted-page visits through the date index without dropping history,
changing the operation-local cache, using SQLite temporary sort files or weakening encryption. The supported
compiled-model extension binds the existing-id LedgerEntry constructor, so materialization no longer creates
and discards a fresh identifier. New entries keep their original identifier generation. The compiled model is
regenerated; schema, financial rules, keys, backup and privacy policy are unchanged.

All 881 Data tests pass, including two new AT-303 complete-value/order cases. Actual Release/AOT Android
reference/tenfold Home in English/Persian/German retains every canonical financial presentation and complete
raw schema/header/integrity/typed rows on the same ciphertext and keys. Tenfold publication median decreases
from 14,828.37 to 7,418.54 ms; reference median from 3,066.82 to 2,252.00 ms. These are controlled loaded-emulator
observations, not two-second cold/phone/ANR/iOS acceptance. Windows native publication and complete portable
preservation also pass. Full-suite historical counts are not a new full run. Exact evidence and package:
[20-home-read-delivery.md](enhancements/2026-10-commercial-release/20-home-read-delivery.md).

## D-304 isolated billing HTTP fixture peers

The D-303 hosted backend qualifier failed because separate TestServer hosts shared a null remote peer and
consumed the same durable 30-request billing window. The same limited Linux sequence reproduces HTTP429;
standalone Windows/Linux cases pass. A test-only startup filter supplies a stable documentation peer per
fixture, without trusting headers or changing the production limiter. The previously failed original client
continues its retained pending command/authority. Separate Integration HTTP workers do not recreate a missing
ephemeral installation key. The exact failed retirement case and a new 30/31-request, independent-peer,
second-worker and next-window regression pass on the retained Linux fixture. Strict test builds pass;
production app/API code and the D-303 APK are unchanged. Its one-use hosted repair check later completed successfully for backend, CI and CodeQL.
Evidence: [21-billing-http-isolation.md](enhancements/2026-10-commercial-release/21-billing-http-isolation.md).


## D-305 complete typed Home materialization

Home retains complete 32-field ledger history and original order, transaction, conversion, cancellation and
file/rights boundaries while replacing per-row EF materialization with a guarded typed reader through EF's
original relational command pipeline. A mapping drift fails explicitly. Same-process Release/AOT tenfold
source-read medians are 2707.56 ms (original EF) and 2211.90 ms (typed), with complete materialized equality;
no stable reference improvement is claimed. Separate original Android Home publications in en/fa/de retain
all canonical financial presentation and full raw database preservation. Four new AT-305 boundary/value cases
and 24 distinct related Home/model cases are qualified across retained runs; no new whole-suite run is claimed.
Original hidden Windows Home and complete portable preservation also pass. The actual loaded main Android
startup method profile is refreshed, with development-only recording excluded from the ordinary candidate.
Evidence, package and remaining limits: [22-home-materialization-delivery.md](enhancements/2026-10-commercial-release/22-home-materialization-delivery.md).

QA-06 remains In progress for remaining composition, memory/cold/ANR, physical-phone and iOS acceptance.
The D-304 one-use hosted repair result is now completed/success for backend, CI and CodeQL; its timer is deleted.


## D-306 original reviewed inputs and detached goal lifecycle drafts

Original plan/goal reviews now reject changed in-memory model packets as well as changed database rows,
while retaining original file, choice and rights validation. Six Home mutations reproduce the missing
rejection before the fix; the goal allocation writer also refuses an altered source without money changes.
GoalDetail lifecycle actions now write a complete detached clone, preserving the original review through
confirmation and Save. Seven new regression cases and 179 distinct related Data / 169 original-source app
cases are qualified across retained runs; only the three affected failing app methods are repeated after
that caller correction. No new full-suite run or discarded prototype test count is claimed.

Twenty-four actual native goal actions/confirmations pass across en/fa/de and light/dark in a fresh Windows
fixture, preserving the unsaved amount and original ledger. The protected Release/AOT Android reference and
tenfold cases reject altered reviews, accept the same review after restoration, retain original native Home
presentation and complete raw data. Strict builds and the complete signed main APK pass; the original main
installation reaches unchanged Home after upgrade. A recurrence-cache trial is rejected for insufficient
benefit and completely removed; QA-06 and physical/iOS/release acceptance remain open. Exact evidence:
[23-reviewed-input-integrity-delivery.md](enhancements/2026-10-commercial-release/23-reviewed-input-integrity-delivery.md).

## D-307 bank research evidence, not importer acceptance

The fourteen target-market discovery rows, original bank/regulator/provider references and exact remaining
format/specimen gates are recorded in [target bank research](enhancements/2026-10-commercial-release/24-target-bank-research.md).
Country and link consistency are checked. No new preset, native action, bank connection or AT case is added;
existing D-297 import and D-306 runtime results are retained without a repeat run. A published CSV button is
not evidence for header/encoding, booked status, relationships, stable ids, completeness or Undo acceptance.

## D-308 saved-filter native regression coverage

The independently owned filter-actions route exercises the original Transactions page and saved queries.
Windows en/fa/de, both themes and 360/412/1280 widths at process-local 200% stress qualify 216 native
target observations and 54 saved-query invocations across 18 contexts. Complete original stored inputs
remain unchanged. This adds maintained native coverage, not a new unit-test count or a fresh whole-suite
run. Android/package results and separate platform gates: [evidence](quality/saved-filter-actions-readable.md).

## AT-309 - Original protected schema connection lifetime (D-309)

Eight new focused cases qualify current/old/unknown histories with original open/closed caller ownership
and cancellation. The two original schema facts remain qualified; the old-schema fact now proves original
financial ids, EUR expense and balance through actual pending migrations. Ten distinct class cases pass
across retained runs, with only the final strengthened fact rerun after its assertion change. This is not
a new full-suite run. Actual native keyed reference/tenfold files retain complete raw data and restore the
closed state with one physical open. Main-package and external limits are recorded in
[the delivery evidence](quality/protected-schema-connection-lifetime.md); QA-06 remains In progress.

## AT-310 - Verified purchase validity and renewal presentation (D-310)

The original plan screen now exposes original owned-purchase lifecycle alongside offers: paid/grace ends,
last verification, observed auto-renewal, Lifetime and future scheduled changes. Historical device and
identity packets remain separate from current effective rights; cached/outage facts explicitly require
refresh. No next debit, price, early scheduled access or cancellation is inferred. No raw purchase/account/
receipt identifiers are shown. Seven new focused cases cover all six languages and original financial
preservation, including live retranslation without store reads. Native/build/package evidence and retained
failures: [verified purchase lifecycle](quality/verified-purchase-lifecycle.md). Genuine store, Mac/iOS, phone and release gates stay open.

## D-311 readable report and forecast tables

The original trend/history and forecast comparison/parts templates now separate identity/date from whole
monetary packets, reusing the existing native horizontal AmountReadout and overflow hint. Native original
German month/amount overflow is reproduced at 360 width. Eighteen Windows en/fa/de, light/dark, 360/412/1280
contexts at process-local 200% stress and six original-source Release/AOT Android contexts pass with original
stored data unchanged. Charts/PDF/calculations and schema are unchanged. No new unit-test count or unrelated
successful suite replay is claimed. Exact scope, retained failures and package:
[financial table readouts](quality/financial-table-readouts.md). A11Y-03, spoken readers, actual OS/device/iOS and release gates remain open.

## AT-312 physical Android safety-copy recovery

Actual Samsung fresh setup without online identity, income/expense/refund/transfer-fee and automatic-copy
replacement exposed missing selection of pre-restore safety files. Seven new focused cases verify current
profile/installation discovery, exact bounds, unchanged files, forged candidate/authentication separation
and retired authority refusal. Original Windows layout has 18 qualified language/theme/width contexts.
Actual same-file phone recovery, signed package and cold continuation are separated from those narrow
checks in [physical Android safety recovery](quality/physical-android-safety-recovery.md). This does not
close all device scenarios, real notification/OAuth/store/reader or iOS acceptance.


## D-313 hosting gateway acceptance

Seven focused operator cases, nine distinct original-proxy TLS/forwarding cases and official ingress/no-port composition checks pass locally. Failed observations and selective continuation are retained. No live Cloudflare, public DNS/mail, phone-network or release acceptance. See [outbound tunnel origin](quality/outbound-tunnel-origin.md).


## D-314 physical protected reference/tenfold batch

Actual Samsung ARM64/API36/4KiB: six original Home language/shape groups, three native query outcomes per shape and eight validated store writes/read-back/exact cleanup pass with preserved data. Five complete groups were retained after the eight-frame caller limit, not replayed. Initial Home/ledger latency remains slow; no whole cold/ANR/low-memory or native Save-button timing acceptance. See [physical protected workload](quality/physical-protected-workload.md).


## AT-315 complete history retains its financial packet

34 related tests pass with independent EF expectations, optional inclusive date bounds, complete values/order and cancellation. Strict Windows/Android builds and complete signed main upgrade pass. On the same retained Samsung ciphertext/keys, 100,000-entry public read goes 7,773 -> 4,313 ms and original native load/layout 10,446 -> 6,964 ms, with complete preservation. Exact/empty/wide search and original main 979 EUR history remain correct; successful unchanged financial writes were not replayed. No full-suite rerun/count is claimed. QA-06 remains In progress for remaining latency, cold/ANR/low-memory/device/platform acceptance. See [physical ledger read](quality/physical-ledger-read.md).


## D-316 actual planned-work device scenario

The unchanged signed D-315 APK now has actual Samsung main-app evidence for original setup, optional budget and balance goal, manual 4/6 partial/final settlement, future-plan/forecast exclusion/reset, process restart and return to the original Main profile. The two actual expense rows total 10, balance is 990, budget left 40; future 20 gives forecast 970 without a third posting. Native displayed-state acceptance is distinct from full database equality. All data is fictitious and retained; no system setting, reinstall or personal data change. No unchanged builds, unit suites or APK qualification were repeated. Broader recurrence/notification/platform/performance/release gates remain open. See [physical planned work](quality/physical-planned-work.md).


## D-317 physical own-format CSV and cold Undo

Actual Samsung original file-picker cancellation/selection, own-format two-row preview/import, zero-new/two-known duplicate preview with disabled action and persisted Undo after process restart pass in the retained D-316 fictitious profile. Exact note/tag/literal formula-like text is retained; financial balance 990 -> 980 -> 990, original two payments, 40 budget left and 970 forecast are verified. Return to Main at 979 and hash-matched exact-file cleanup are complete. Unchanged app/APK and successful builds/tests remain D-315. No producer-export roundtrip, real-bank/aggregate/hostile-file/full raw equality or complete device/platform/release claim. See [physical CSV and Undo](quality/physical-csv-undo.md).

## D-318 native first-plan regression

The actual first-plan blank-list defect is fixed without persistence/financial changes. Independent original native Save on retained protected data and 18 final en/fa/de light/dark 360/412/1280 large-text layouts pass; standard Windows and complete signed Android builds have zero warnings/errors. Main upgrade without reset displays the retained unpaid plan before any segment toggle, handles empty Upcoming/return and preserves the displayed 979 EUR balance. No successful unrelated unit/device/crypto scenario is replayed or unit count increased. Broader device/platform/performance/release gates remain open. See [first plan publication](quality/first-plan-publication.md).

## D-319 bounded physical debt and installment qualification

Both original physical debt/lent lifecycles now have native evidence: positive setup signs, complete 11- and 6-row estimates, cancellation without posting, principal transfers versus 10 expense / 5 income, explicit final repayments, six retained rows and cash 1,495 after signed cold upgrade. The observed zero-balance missing-terms hint is corrected; 38 focused native Windows cases and zero-warning Windows/complete Android builds pass. Main returns 979. Unit counts and successful unrelated checks are retained; broader device/platform/performance/release gates remain open. See [physical loan lifecycle](quality/physical-loan-lifecycle.md).

## D-320 original profiled Home under retained load

The independent phone caller now matches production assembly/profile inputs. A captured extra CoreLib
AOT-method selection reproduces native failures under load; removing only those additional roots while
preserving SDK defaults completes all 12 en/fa/de reference/tenfold Home cases and full schema/typed-row
preservation on the same retained ciphertext/keys. Align restored runtime/compiler/task servicing versions,
fix matching-configuration restore, keep only the explicit review window awake and retain scoped failures.
Windows and complete signed Android builds pass without warnings/errors; the main data-preserving upgrade
shows 979 EUR and actual expense/Cancel returns unchanged. No financial C# algorithm, schema, KDF/native
cipher or full-suite count changes. Current loaded observations remain 1.96-2.65s reference/8.83-10.29s tenfold;
this is stability qualification, not a general speed improvement, full cold/ANR matrix or release approval. See [native evidence](quality/physical-home-compilation.md).


## D-321 / AT-321 complete native row compatibility

D-321 reduces the measured complete 100,000-row reader from 4231 to 3344 ms and complete Home input read from 4653 to 3811 ms on retained Samsung ciphertext/keys. All 12 final en/fa/de native Home cases preserve complete financial presentation and raw schema/typed rows; loaded Home still takes 1.83-2.73s reference/ 8.47-9.58s tenfold. Related validation is 42 passing cases plus one final interceptor-reader case (nine added behaviors), not a full-suite rerun. Final strict Windows/complete signed offline Android builds and data-preserving main Home/All-history/return pass. QA-06, cold/ANR statistics and external platform/release gates remain open. See [evidence](quality/physical-home-row-reading.md).


## D-322 / AT-322 exact prepared projection

D-322 reduces the measured tenfold complete Home input read from 3811 to 3306 ms using exact-query null metadata, retaining all original 32 fields/conversions and safe interceptor fallback. All 12 final en/fa/de phone Home cases retain full financial presentation and raw schema/typed rows; loaded Home remains 1.92-2.83s reference / 7.23-9.51s tenfold. 45 related cases pass (two added behaviors), strict Windows/complete signed offline Android builds and main Home/All-history/return upgrade pass. A recurrence candidate without reliable native gain and temporary worker probes are fully withdrawn. No whole-suite rerun, two-second/cold/ANR/platform or release acceptance is claimed. See [evidence](quality/physical-home-null-bitmap.md).


## AT-323 original physical report/filter/PDF handoff

The unchanged D-322 signed offline main APK passes an original Samsung report/filter/PDF handoff batch:
October income 1,000/spending 21/surplus 979 and five-row drill-down; empty September scope replaced by a
saved October expense filter (three rows/net -21); actual PDF chooser offered and cancelled without a
recipient; filter retained after cold PID 24409 -> 27876 and then explicitly deleted; original unpaid
plan 3/headroom 976 and cash-account zero income/spending retain transfer semantics. Final report/ledger
and Home return 979 without financial Save/reset. Eleven saved-device assertions pass; no new managed
test count, build, APK or unrelated successful checks are repeated. This is English/light displayed-state
acceptance, not raw main-store equality, inspected phone PDF pages, all reports/readers/iOS or release.
See [physical evidence](quality/physical-report-filter-pdf.md).


## AT-324 physical digital receipt totals and discard

The unchanged signed D-322 main APK passes six original Samsung digital-PDF receipt flows, one per
en/fa/de/es/fr/it source language: distinct total phrases and Persian digits yield the reviewed 24.50 EUR,
not cash 50/change 25.50/order 999999. Every original editor is explicitly cancelled/discarded without Save;
the same PID 27876 retains five rows/net 979 and final Home 979. The incomplete picker observation alone
continues, and only six exact hash-matched newly owned input files are removed. No managed test count,
app build/install or unrelated successful check is repeated. English/light presentation, digital text and
displayed preservation are the scope; evaluation notices limit merchant accuracy, and real corpus,
image OCR/camera, raw main-store equality, other native/platform and release acceptance remain open.
See [physical evidence](quality/physical-digital-receipts.md).


## AT-325 image OCR, accent evidence and safe refusal

Actual Samsung image OCR exposed French À -> Å in NET À PAYER while the number/unit remained correct.
D-325 folds Latin accents only for whole keyword/exclusion comparisons, preserving original source,
non-Latin marks and all numeric/currency/refusal rules. Ten added behaviors reproduce the failure and
retain safety; all 118 related cases pass, followed by test cleanup and zero-warning Windows/complete
signed offline Android builds. Same-file French recognition now gives 24.50 with original Å source;
seven original raster cases include German EXIF and blank missing-total/currency-conflict drafts.
Three pre-fix successes are retained; four final cases and upgrade preserve five rows/Home 979 without
Save/reset. Exact owned inputs and diagnostic package are removed. Real corpus/camera/scanned/iOS and
release gates remain open; a separate dangling transfer-summary caption is observed and queued next.
See [physical evidence](quality/physical-receipt-ocr-accents.md).


## D-326 AT-50 filtered result captions

D-326 fixes the observed transfer-only transaction summary: show the existing translated count-only
caption when the actual result-currency dictionary is empty. Retain a real zero result, separate currency
packets, original matching rows and unchanged income/spending/transfer semantics. No new string, schema,
financial write, dependency or native cipher change is introduced. All 18 focused application-source cases
pass across six languages, followed by test cleanup; the Windows and complete signed offline Android
builds have zero warnings/errors. The independent actual Windows page passes 18 en/fa/de light/dark
360/412/1280 contexts with complete scaled captions and unchanged stored inputs. Six successful English
contexts are retained; only the remaining contexts continue after correcting the diagnostic's Persian-digit
comparison and hidden-root measurement order. The original Samsung upgrade shows transfer count 1 with
no dangling total, expenses 3/net -21, income 1/net 1000 and final Home 979 without Save/reset. Phone
preservation is displayed-state evidence, not raw main-store equality. Remaining receipt corpus/camera,
performance/cold/ANR, iOS/provider/Store and release acceptance remain separate.
See [qualified evidence](quality/transaction-summary-result.md).


## D-327 AT-281 original phone continuation

D-327 qualifies the original physical automatic-copy lifecycle on SM-A176B and fixes an observed
maintenance overlap. A synchronous onboarding-readiness read raced the legitimate exclusive safety-file
discovery; the native type/source stack identifies DatabaseAccessGate.EnterRead through GetSettings.
Use cancellable GetSettingsAsync readiness, recheck the captured profile set and preserve the original
publication/encryption/interval guards. A successful not-due check clears stale transient feedback without
replacing its file/timestamp. Seven focused cases pass (six together, one final scope case alone; four
added cases), test outputs are cleaned once and strict Windows/final signed offline Android builds pass
without warnings/errors. Twenty-three saved-native observation assertions cover Off/no copy, Daily
creation, one-entry preview, explicit 36-to-37 recovery, separate two-entry safety preview, cold persistence,
final Weekly/Daily without the former error, original-copy preservation and retirement of only the new
fictitious profile. Original successful creation/restore is retained, not repeated after the readiness fix.
Final Main is 979 with its three original profiles/copies and original Daily/password choices; the phone
is released. No native crypto/schema/SDK/permission change or full-suite rerun. Raw main-store/key equality,
power loss, key loss, full hardware retention stress, other devices/iOS/OAuth/Store and release remain open.
See [qualified evidence](quality/automatic-copy-readiness.md).


## D-328 AT-296 physical receiving and content inspection

D-328 completes a scoped physical Germany accountant export on SM-A176B using the unchanged signed offline
D-327 app. The original native chooser delivers two readable ZIPs only to a separately installed offline
receiver: both accounts/five rows, then cash only/the same one transfer plus its unselected Main identity.
The host verifier checks actual CSV/manifest bytes, six member digests, default redactions, blank tax fields,
refund/fee references and separate recorded-kind totals. Empty selection, confirmation Cancel and stale-review
invalidation are exercised. Twelve saved-observation/receipt assertions pass; the receiver strict build has
zero warnings/errors. The new receiver is removed, final Home remains 979 and the phone turn is released.
No production code/APK, schema, permission or managed test count changes; successful unrelated tests/builds
are not repeated. Physical documents/notes-enabled/all-layout, iOS, tax specialist/calculation/filing and
release acceptance remain open.


## D-329 AT-329 actual continued PDF context

D-329 fixes an actual anonymous balance row on page2 of the original Samsung report PDF. Repeat each
table's account/section title, exact scope and column headings as one grid header, retaining all original
values, RTL mapping, row membership and footer exclusion. Three new en/fa/de continuation cases reproduce
the original failure; all eight related Reports cases pass. Strict Windows and the complete signed offline
Android build have no warnings/errors. Data-preserving upgrade and six pages from three original native
English/Persian/German PDF exports are checked; final Home979 and original display choices remain. The
receiver is removed and phone turn released. Seven saved-native/receipt assertions and an original-byte
host verifier retain actual context/figures/visible footer evidence. A suspected footer defect is disproved
by pixels; its experimental source/build are withdrawn and not installed. Initial stale uncompressed
Samsung chooser coordinates opened an unrelated app; no further interaction occurred there, and temporary
fictitious-file grant absence is not proven. Final selection uses a fresh compressed hierarchy and exact
local receiver. No financial/schema/native-crypto change or full-suite/performance replay. Wider device,
reader/iOS/provider/Store and release gates remain open.


## AT-330 original physical CSV roundtrip

D-330 qualifies the original Samsung CSV producer/consumer roundtrip on the unchanged D-329 offline APK:
five original rows, all27 exported fields/stable IDs equal after re-export, retained refund/transfer/fee
relationships, disabled five-ID duplicate import, cold original batch and explicit Undo to zero. Only the
new matching-account profile is retired; Main979 and three previous profiles remain. Twenty-three saved
observation assertions pass; only the independent offline receiver is rebuilt with zero warnings/errors.
No maintained unit count, main source/APK or unrelated successful suite/build changes. Bank/aggregate/
malformed-file/full-device/iOS/release gates remain separate.
Original native preview is count-only; source row values are checked independently. Notes inclusion is on, but the fixture has no notes. This is not bank-preset, arbitrary CSV or aggregate acceptance. See [evidence](quality/physical-csv-roundtrip.md).

## AT-331 interpreted ledger review

Verify actual row interpretation, full source/destination currency packets, bounded paging through every
invalid/known row, duplicate inclusion and rejection of a changed generic mapping before refresh. Preserve
the page after picker cancellation and retire it only after accepted import/Undo. Nineteen new behaviors
are covered by focused application tests. Native/device scope and pending gates remain explicit in
[qualification](quality/interpreted-csv-preview.md); this does not broaden D-330 to arbitrary bank files.

## AT-332 actual review-page arrival

Require first-row/header visibility after original Next and Previous, before any inspection scroll.
One new application-source behavior and18 native Windows contexts pass; actual Android three-page
traversal/refusal boundaries and eleven saved-observation assertions pass on the412x800 x64 emulator.
There is no financial import/Save. Startup ANR and Samsung/iOS/platform acceptance remain separate;
[exact evidence](quality/android-import-preview-arrival.md).

## AT-333 protected cold notification snooze

Original application-source regression: four valid pending notifications incorrectly clear before
protected startup. Final refused/missing/nonpublishing startup and disposal cases preserve the queue,
original SQLite rows and unsaved snooze state.100 related cases pass before the final disposal guard;
the four final changed cases pass afterward, not a new whole-suite run. Actual native In1hour starts
PID26475 directly from the original action receiver, with zero windows, protected publication, one
saved/native pending snooze and preserved fixture account/plan/zero ledger. PID26586 then reads the
persisted native/snooze metadata without preparing the financial store. Strict builds, complete signed
offline upgrade and original emulator Home0 pass. Phone/iOS, scheduled delivery and interruption during
the action remain separate. Startup ANRs are not fixed. See [evidence](quality/cold-protected-reminder-startup.md).


## D-334 QA-06 negative packaging evidence

Both finite packaging alternatives are rejected: disabling assembly compression fails before
managed startup with a missing compressed_assembly_count native symbol; direct native-library loading
records a real binding ANR and takes24.422s through OnCreate versus the17.755s control. Single samples,
native rebuild differences and public memory counters do not establish the original root cause.
All temporary source/manifest changes are removed; ordinary application/APK remain D-333. No unchanged
successful suite is repeated or new test count claimed. QA-06 and wider device/iOS/release gates remain open.
Neither experiment qualifies an ANR fix. Preserve their terminal crash/actual ANR rather than treating
a completed callback or signed build as acceptance. [Evidence](quality/android-startup-packaging.md).


## AT-335 complete More menu identity and explanation

Require complete realized native title/explanation glyphs at200% process-local scale and44-unit
original semantic targets. Both16-item offline and18-item personal-sync menus pass all18 language/
theme/width contexts, with unchanged route/command parameters and full names/hints. These are36
layout contexts, not a new unit-test total or whole-app/OS/phone/iOS acceptance.
See [scope and final Android/package evidence](quality/more-menu-readable.md).


## AT-336 compression-cache consistency

The observed pre-managed assembly-store abort comes from a cached CoreLib header retaining
index193 while the current descriptor table assigns CoreLib192 and Java.Interop193. Validate the
SDK's current compression-item metadata before its incremental timestamp check and regenerate only
incompatible owned cache entries. Seven focused behavior cases preserve valid cache timestamps,
invalidate stale index/size/magic/truncation, accept missing files and refuse outside destinations.
Keep the original SDK compression, serviced runtime, AOT profile, key/data and permissions. No unrelated
successful financial, OCR, backend or More-layout suite is repeated or new managed count claimed.
QA-06 startup ANR, two-second loaded Home, broader Android, Mac/iOS and owner release gates remain open.
[Evidence, failures and limits](quality/android-compression-cache.md).


## AT-337 original startup capture and clock control

Exact-PID, frame-pointer native captures retain the actual background startup ANR. Monotonic
phase filtering places75.34% of888 main-thread OnCreate samples in the clock-reading path; an
isolated native executable reproduces expensive precise clocks without application code in both
original and fresh same-image emulators. Broken callchains and single-sample limits remain explicit.
No clock substitution, kernel restriction, runtime/licence change or speculative app workaround is
shipped. The original AVD/data remain; ordinary D-336 signed APK and same-PID Home are restored.
App code, APK and managed counts remain D-336; no unchanged successful suite/build is repeated.
QA-06/phone/iOS/release remain open. Continue ready release preparation independently of those gates.
[Evidence and limitations](quality/android-startup-clock-cost.md).

## AT-338 isolated capture and native refusals

Eight current native Windows draft screenshots in each existing language (en/fa/de/es/fr/it)
are delivered with exact RGB/source hashes and independent fictitious package/storage/non-input
desktop isolation. Five actual native pre-startup refusals, strict Windows/Android Debug builds and
48 visual/pixel checks passed. Only the rejected English budget loading frame was recaptured from
the same fictitious data; 47 successful frames were retained. Shipping UI/APK and managed counts
remain D-336. These are development drafts, not mobile or Store acceptance/publication. QA-06,
broader physical Android, Mac/iOS, provider/accounts and owner release gates remain open.

[Evidence and limits](quality/store-drafts.md).

## AT-339 report legend native text and scroll

Category/composition report names, shares and monetary packets occupy independent growing rows.
Names retain native scaling and complete wrapping; oversized amounts use the existing horizontal
readout, outside the original category navigation button. The 18 final Windows contexts (en/fa/de,
Light/Dark, 360/412/wide at process-local 200%) and six original Release/AOT Android contexts pass
native glyph/viewport/scroll/target checks. Fictitious presentation inputs are explicitly separate
from report calculations; no financial commands execute. Strict builds and the full offline signed
APK pass. The Android caller's initial startup ANR is retained: Wait continued the same original PID,
without restarting its scenario. QA-06/ANR, broader A11Y/OS readers, physical Android, iOS and release
gates remain open. Managed test counts are unchanged; unrelated passed suites were not repeated.

[Evidence and limits](quality/report-legends-readable.md).

## AT-340 actual translated Flex group captions

The remaining inherited RowTitle ellipsis truncates the original Non-monthly bills caption at
200% Windows process-local scale/360 width. The shared default now wraps with its original 15-unit
font and semantic color. All 26 other current consumers already explicitly wrap, so their effective
behavior is unchanged. Eighteen final Windows contexts/36 native captions and six Release/AOT
Android contexts/12 captions pass; original account/entry/budget inputs stay unchanged and no method
is saved. Strict builds and a complete signed offline APK pass. The independent Android startup
ANR remains an actual failure; Wait continued the same PID 1753 to its completed caption result.
Actual OS text scale/TalkBack, broader device/iOS/performance and release gates remain open. No
system setting, financial algorithm, schema, translation or managed unit count changes.

[Evidence and limits](quality/budget-group-titles-readable.md).

## D-341 current coverage and rejected prototype

The current original Home regressions are rechecked after removing the prototype. Its 53-case
checks included eight experimental session behaviors that are removed, not added to current
coverage. Native baseline and three candidates each preserve twelve full Home presentations and
raw rows; their timing does not close QA-06. No broad unrelated suite or unchanged APK is repeated.

[Evidence and limits](quality/home-publication-session.md).

## D-342 Home category presentation qualification

Pass: original Home category template in en/fa/de, both themes, 360/412/1280 Windows at Debug
process-local 200%, and six original Release/AOT Android contexts. Check complete native names,
percentages, two original monetary packets, both scroll ends, original command/parameter/44-unit
target, hidden detachment, complete visible source and unchanged stored inputs. Six English Windows
cases are retained before fixing the RTL verifier; only 12 remaining contexts run afterward.
No financial Save or layout persistence is invoked. No new managed test count is claimed.
OS200%/spoken readers, physical Android, startup/ANR, iOS and release acceptance remain open.

[Exact runs, failures and limits](quality/home-category-legends-readable.md).

## D-343 original plan-row qualification

Pass:18 en/fa/de xLight/Dark x360/412/1280 Windows contexts at Debug process-local scale2, each
checking both original Home/Plans rows; six Release/AOT Android contexts check both hosts, date/icon
variants, native complete descriptions, original commands/targets and twelve monetary viewports.
Both scroll ends and unchanged stored inputs are checked; no financial command executes. A focused
English360 probe confirms the actual post-layout sign at nativeX0 after correcting diagnostic APIs.

[Evidence and limits](quality/plan-row-amounts-readable.md).

## D-344 original ledger/account qualification

Pass18 retained en/fa/de xLight/Dark x360/412/1280 Windows contexts at process-local scale2,
108 monetary checks across original templates, plus six Release/AOT Android contexts/36 viewports
with native colors/names and actual native selection/deselection. Original stored inputs stay intact.
The final four Windows contexts reveal/reacquire real collection targets before invocation. Native
Win2D root-change crashes and the independent Android startup ANR remain failures, not passing
whole-platform/stability acceptance. No unchanged managed suite or unit count is replayed/increased.

[Evidence and limitations](quality/ledger-account-amounts-readable.md).

## D-345 - Actual OS200% and TalkBack boundary

Six en/fa/de xLight/Dark cases at actual emulator OS2.0/412dp pass36 ledger/account monetary
viewports, original target/color/name/selection and unchanged stored inputs. Startup ANR and Wait
on the originalPID are retained. Exact captured OS settings restore; no phone input. TalkBack
service binding and five low-confidence offline fragments do not pass spoken navigation. No test
count increase or unrelated suite/build rerun. [Evidence](quality/android-os-accessibility-qualification.md).

## D-346 retained OS font cohorts and actual failure receipts

At real emulator OS2.0/412dp, six en/fa/de xLight/Dark cases pass48 table readouts,12 report-legend
readouts and12 Flex captions, with unchanged original stored inputs. Preserve native SIGSEGV and
ANR failures separately; no whole-runtime/whole-platform acceptance. Exact official BuildId/debuglink
CRC matching supports fault-location evidence, not a proven corrupt-object cause. Reject the
ineffective serial-major GC trial and silent TalkBack navigation attempt. No managed count or
unrelated suite/build is repeated.
[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-347 distinguish realized controls from unobscured native windows

The native observer pauses under the actual Android ANR dialog and continues only after observed
Wait restores the original Activity focus. Every native text assertion checks focus; retained
receipts without that contract are refused. Two guarded six-case groups pass with unchanged
original inputs, while the whole trial has a real mid-budget ANR. Do not convert provisional
Home/plan geometry into complete interactive acceptance or count a rejected GC policy as a fix.
[Evidence and ordinary follow-through](quality/android-os-accessibility-qualification.md).

D-347 final ordinary follow-through passes24 Home-category/total and original Home/Plans plan-row
amount viewports across12 en/fa/de xLight/Dark contexts at actual OS200%/412dp with real Activity
focus and unchanged stored inputs. These two original runs have no observed ANR through completion;
this is not a general runtime/stress fix. Captured settings restore and MainPID7654 is preserved.

## D-348 reader observation correction

Installed stock UI automation suppresses other accessibility services. The independent bounded
observer preserves TalkBack and serializes its single native client. Actual retained Main gestures
produce Recorded balance accessibility focus and captured audio; offline transcripts are unreliable,
so spoken-reader acceptance remains open. Settings restore exactly, notification flags and MainPID
are preserved, and the owned remote JAR is removed. Main/APK/count remain D-344. No unrelated
successful suites/builds repeat. A11Y-01/03, native ANR, remaining device/iOS and release gates stay open.

## D-349 actual reader input boundary

The retained Main assessment opens an unedited expense draft directly with ordinary adb injection,
not proven TalkBack activation. Further activation is refused without expected focus; original
Cancel returns to Home0.00EUR on the same PID. Console commands do not prove delivered reader input;
denied kernel input is not escalated. Captured settings/notification flags restore and exact remote
files are removed. No app/APK/count change or unrelated successful verification repeats. Stop these
unchanged injection trials; A11Y/native/device/iOS and external release gates remain open.

## D-350 complete original Home comparison

Fifteen new independent-EF date-storage cases pass on the restored original getter. The correct but
inconsistently slower decoder is rejected after24 original native Home observations across candidate
and control; complete data/presentation preservation passes. Measured pages now require actual window
focus and reject older unguarded receipts. Real startup/Main ANRs remain recorded; Main/APK stay D-344.
No full suite or successful unrelated verification is replayed. QA-06/device/iOS/release remain open.
[Evidence and limits](quality/home-date-materialization.md).

## AT-351 - Original reimbursement batch contract (D-351)

Two original-code cases reproduce excess ledger visits without timing assumptions. All15 new cases
then pass with the indexed batch, including exact single-entry equivalence, stable identity/date order,
unchanged complete inputs, duplicate identities and relevant/unrelated overflow boundaries. The focused
39-case run includes affected existing reimbursement/report/KPI cases; no full suite is replayed.
The independent original native page and exact delivery receipts are recorded in
[the reimbursement qualification](quality/reimbursement-batch.md). Large-list rendering, whole-Home
latency, runtime/ANR, physical phone, OS readers, iOS and release are separate open acceptance gates.

## AT-352 - Original reimbursement page

Use the actual application VM/page, real independent SQLite and original context factory. Qualify a
4,096-result list's original values/order, bounded realized rows and first/last targets, complete
native captions/amount scroll ends, effective loading cover, coalesced reads, worker context attempts,
failed-read preservation, native retry and covered/retired command refusal. Check empty presentation
and actual non-overlapping intro/text, not just full allocated rectangles. Windows contexts span
en/fa/de, Light/Dark and360/412/1280 at process-local200%; Android contexts span en/fa/de xLight/Dark
in independent Release/AOT. These are runtime assertions, not a new full-unit-suite count. Synthetic
empty presentation does not qualify empty-store recovery. Retain earlier false/partial geometry,
actual root failures and successful immutable cases. [Results and limits](quality/reimbursement-page.md).

## D-353 scoped template lifecycle regression

The unchanged-source Windows baseline fails retained-native-control assertions. The candidate checks six
original Home language/theme contexts, same-count reload, fresh reordered metadata, disabled original
commands and2/1/0/2/0/2 count transitions with stored inputs preserved. No new full-suite count is inferred.
[Receipts, failures and platform limits](quality/home-template-publication.md).

## AT-354 complete-history storage compatibility

Nine new maintained cases cover complete escaped/invalid/large text, textual numbers, legacy/malformed
identities, large tags, signed booleans and original conversion-failure order. Final restored-source
run:9passed,0failed/skipped. Earlier prototype60-case and simulated capability runs remain separate;
no full-suite/native user-acceptance count is inferred. [Evidence](quality/complete-history-row-packet.md).

## D-355 scoped actual-OS Settings picker regression

Retain the original clipped English Light negative baseline at412dp/font_scale2.0. Check the
corrected original page in English, Persian and German, light/dark, with unchanged fictitious
accounts/entries/settings and original account-dialog Cancel. Restore exact captured OS values.
This scoped native matrix does not close A11Y-01/03 or qualify every Settings control or spoken
reader. [Exact outcomes and limits](quality/android-settings-os-text.md).

## AT-356 complete original editor captions

The actual OS200%/412dp baseline spans account/debt/expense/transfer/once-plan/monthly-plan,
en/fa/de and both themes. Retain failed native geometry and input-equivalent comparisons. Final
checks include complete leaf captions, native node names and both original debt disclosure
commands without editor Save. Separate Windows native component/disclosure matrices retain
360/412/1280 development stress and real UI Automation, not system-scale/phone/reader acceptance.
[Exact receipts, refusals, restoration and limits](quality/android-editor-os-text.md).

## AT-358 complete measured goal and budget choices

Retain negative actual OS200%/412dp work-form evidence and compare original goal balance/earmark/
share/cut, empty-budget choices and zero-advance settlement in en/fa/de/both themes. Recheck
all changed ChoiceChips contexts, preserving unchanged settlement cases and stored financial
inputs. Check original goal-choice selection/restoration in18 independent Windows contexts
without Save. Exact settings restoration and ordinary signed main upgrade/Home are separate.
This adds native receipts, not a managed case count or closure of A11Y/QA-06/device/iOS/release.
[Failures, correction and final evidence](quality/work-editor-os-text.md).

## AT-359 incremental manifest variant fidelity

Exercise absent/added/unchanged/same-timestamp-edit/removed/reordered/missing project overlays through
the real imported target and SDK-style incremental inputs. Inspect actual signed independent APKs
when switching profiling on then off without clean, including runtime bytes and default privacy.
Preserve original runtime failures separately; native sampling is not a fixed finalizer or ANR.
Record the coordinated Samsung ordinary upgrade/unsaved Simple-goal Cancel and unchanged Home979
without broadening it to actual OS200%, advanced choices or whole-device acceptance. These are
process/native cases, not new managed Trait counts.
[Exact qualification and limits](quality/android-manifest-overlay-incremental.md).

## AT-360 retired Android navigation ownership

Retain weak references to original replaced Shell roots; await the same retired cohort and check
one active native root and toolbar subscription. Exercise original translated native Back after
display changes on an open detail in en/fa/de xLight/Dark. Preserve stored inputs/preferences,
actual failed candidates and pending add/remove coverage. No substituted controls/commands or
managed-count inflation. [Results and boundaries](quality/android-retired-navigation.md).

## AT-361 physical display navigation and goal draft

On the coordinated Samsung turn, upgrade the qualified complete APK without reset. Exercise
original Settings language/theme and translated native Back; inspect actual advanced goal type/
method choices and Cancel/Keep editing/Discard without financial Save. Preserve original input
data and return English/Light/Simple/Home979; verify installed bytes and actual OS readbacks.
The 51 derived receipt assertions are not unit tests, a full database comparison, spoken reader,
all phone settings/scales, general ANR or complete device acceptance.
[Exact physical scope](quality/physical-navigation-and-goal-drafts.md).

## AT-362 managed startup boundaries, not a speed acceptance

Bind each original process/trigger/APK to fixed monotonic phase markers. Keep Welcome separate
from retained Home; distinguish context/query and native pre-entry cost from Application timing.
Reject an unchanged-model overlap without end-to-end benefit, preserve the same native Home,
remove all probes/candidates and restore the ordinary package/settings. These are diagnostic
cases, not new unit tests or closure of general ANR/loaded-Home/physical/iOS/release acceptance.
[Results and unresolved causal split](quality/android-managed-startup-phases.md).

## AT-364 minimum-width original editors and footer

At actual emulator OS200%/360dp, use original en/fa/de Light/Dark forms and preserved accounts,
entries, plans, settings, goals and budgets. Continue only the failed disclosures, shared consumers
and flagged editable viewport endpoints. Require full native names, original commands, at-least44px
targets, exact draft restoration and no Save. Qualify every original wizard step and busy Next
disablement without invoking Back/Next/Finish. Windows uses isolated non-input desktops and own
rendered windows at 360/412/1280 with process-local font stress. Observation expiry preserves the
original process and receipts; explicit context subsets do not repeat completed matching cases.
These native observations add no managed unit count or complete device/reader/ANR acceptance.
[Case identities, original defects and results](quality/narrow-editor-actions.md).

Three constructor theme cases require no application/event for an unchanged saved System/Light/
Dark choice, then one actual subsequent user change. These are managed regressions; native footer
receipts alone do not accept a wrong-page image. Check the actual original rendered modal and
zero constructor theme events for every final Windows footer case.

## AT-365 original physical editor disclosures

Coordinate the Samsung turn and reuse the qualified complete signed candidate. Exercise account,
expanded debt account-details and monthly-rule disclosures in en/fa/de Light/Dark at unchanged
phone scale. Require complete original native names, visible 44dp targets, two original clicks,
restored edit values and actual original rule-panel topology. Cancel without Save; explicitly
discard monthly drafts. Keep the same process and restore original app/OS state and visible
account/plan/Home evidence. Do not count native receipt assertions as new unit tests or accept
whole-phone/OS200%/spoken reader/first-run/performance/iOS/release from this scoped batch.
[Physical cases, observation failures and restoration](quality/physical-editor-actions.md).

## AT-366 retained minimum-width financial and Settings surfaces

On the authorized independent emulator, capture exact prior settings and use actual OS200%/
360dp. Reuse original financial/Settings fixtures for all seven groups across en/fa/de and
both themes. Require original focused native pages, complete amount endpoints/captions,
command/name/target contracts and real nonfinancial selection or picker Cancel where applicable.
Bind each retained case to exact source/input/configuration and preserve all original inputs.
Restore settings and original Main Home; never infer spoken reader, full device/ANR/iOS or
release acceptance. No successful 412dp cohort, managed suite or unchanged main build is replayed.
[Complete 42-case evidence and limitations](quality/narrow-original-surfaces.md).

## AT-367 original decorative glyph exclusion and reader navigation

On the explicitly authorized independent emulator, retain installed TalkBack and verify actual
original accessibility focus through public gestures. Reproduce the private glyph before the
style correction; require no private glyph in focus or saved original trees afterwards. Qualify
expense/Cancel, More, About/Back and Home in six native Settings-bound en/fa/de Light/Dark contexts.
Require actual modal/page transitions and preserve successful receipts across observer failures.
All24 scenes/36 activations pass without financial Save; restore exact OS values and original
English/Dark/Simple Home, remove only the helper. No spoken-output/full reader/phone/ANR/iOS/
release acceptance follows from focus or audio energy.
[Original paths and build/package evidence](quality/android-reader-glyphs.md).

## AT-368 physical glyph correction and original navigation

Coordinate the Samsung turn; upgrade the complete signed D-367 APK without resetting fictitious
data. Compare the original Home: nine private glyphs become zero while30 meaningful node names,
roles and bounds remain identical. Qualify original expense/Cancel, history/Home and More/Home
through six actual targets in the same process. Require original979EUR/profile and exact
system-settings preservation, then release the phone. Initial unfocused-window refusal is
retained and the same launch continues. No phone reader/speech or precise startup claim follows.
[Original native evidence](quality/physical-reader-glyphs.md).

## AT-369 - Original Android stored fees and currency retirement (D-369)

AT-106's Android stored-fee gap is qualified on the independent original Release/AOT editor:
12 contexts,36 native Saves (nine retained,27 new), exact fee identities/net effects, all other
serialized ledger fields and unrelated Accounts/Categories/Settings preserved. Blank/zero removes
only the destination fee; reopening and original Cancel preserve creation policy without writes.
Retain terminal finalizer evidence and exact-source case receipts. Six main en/fa/de Light/Dark contexts complete 24 original owned native popup open/close pairs
without selecting a new currency before/after Settings child navigation. Original Home/app/OS
state and installed bytes match after restoration. No managed-count increase or phone/general GC/ANR/iOS/release claim.
[Detailed qualification](quality/destination-fee-android-lifecycle.md).

## AT-370 - Original physical currency-popup lifetime (D-370)

On the approved Samsung, qualify original DefaultCurrency/ReportCurrency popups before/after
Settings -> Available items -> Back in en/fa/de Light/Dark: six contexts/24 open-close pairs.
Actual owned native window/session PID, on-screen geometry, complete names/targets and unchanged
EUR pass. Restore original English/Light/Simple Home, native meaningful state and exact OS values;
keep 979 EUR/profile and matching installed D-369 bytes, then release the phone turn. No financial
Save, managed-count increase or successful unchanged suite/build replay. This does not qualify
physical stored-fee Saves, general GC/ANR, speech, other devices, iOS or release.
[Exact observations and preserved controller failures](quality/physical-currency-lifecycle.md).

## AT-371 - Original physical stored destination-fee continuation (D-371)

AT-106's original editor/store hardware scope completes 12 contexts/36 native Saves on Samsung:
retain 75, edit 125 with the same id, explicit blank/zero removal only, source fee 150 and one transfer.
Exact net effects and every unrelated serialized ledger/account/category/settings field match.
Original fee-free creation/Cancel and native navigation pass. Physical model/transport, actual
ARM64 APK/PID and source MVIDs qualify execution separately from the caller's generic boundary.
The fixture is standalone SQLite; main encrypted-adapter/full-device acceptance is not implied.
Preserve original main PID 11372/Home/profile 979 EUR/system/APK, stop only the terminal independent caller
and release the turn. No new managed count, repeated unchanged suite or general runtime/release claim.
[Exact proof](quality/physical-destination-fee.md).

## AT-372 - Reject unhelpful native row transport (D-372)

Status: experiment withdrawn; original Home performance requirement remains open. Verify all 32
fields/provider dates and complete protected schema/typed rows before paired original Home/native
focus/publication/layout.57 real-SQLite protocol cases are not new managed unit tests. Keep the
historical baseline refusal, capture a fresh exact original control and compare without weakened
presentation checks. Mixed page timings reject the extra native boundary. Restore original main
PID/Home/APK/OS, qualified independent bytes and ordinary Data output after terminal evidence.
No phone, two-second/general ANR, speech/iOS/provider/release acceptance is inferred.
[Exact paired result and restoration](quality/home-native-row-transport.md).

## AT-373 - Original unreviewed scope and checked total boundaries (D-373)

Fourteen new Core cases protect ordered duplicate/endpoint/self-transfer routing, explicit/default
scope, original per-currency counts, legacy metadata and overflow. All48 related cases pass;
original Release/AOT x64 and Samsung ARM64 each refuse Income/Expense cross-entry overflow without
storing those inputs. Native assertion counts are separate from managed tests. Distinct protected
unreviewed reference/tenfold workloads compare complete original Home/native focus/publication/
layout and raw schema/typed data. Preserve the failed raw pre-encryption update, original context
function/triggers, failed native processes and completed cases. Bounded tenfold improvement is
about 13.7%, with reference unchanged/mixed; general GC/ANR/two-second/cold/release is not accepted.
Strict builds, complete signed offline APK and no-reset main Home/system/profile restoration pass.
[Exact evidence and open gates](quality/unreviewed-account-scope.md).

## AT-374 - One review item per transaction (D-374)

Seven new Core cases cover same/FX-currency transfers, destination/explicit/default scope,
unrelated/confirmed rows, repeated records/occurrences, historical metadata, dates, one source
enumeration and count independence from monetary overflow. All36 related cases pass. Actual Home
publication passes18 Windows, six emulator and six Samsung contexts; preserve original fixture
fields and native count/confirmed captions. Retain the managed observer failures and the two passed
English cases when correcting Persian display-digit expectations. The affected protected large
Home path preserves original financial/native presentation and complete raw schema/typed rows.
Strict builds/full signed offline APK, no-reset main Home/profile/OS restoration and phone release
pass. No two-second, broader GC/ANR/device, spoken-reader, iOS or release claim.
[Exact evidence](quality/home-review-count.md).

## D-375 Home title action

Original Home geometry passes 18 en/fa/de Light/Dark Windows cases at 360/412/1280 and six Release/AOT
Android cases at actual OS 200%/360dp. Add remains 48 px, shares one growing title row, keeps its command/
readiness/name and reserves no lower band. Full bottom scroll remains reachable. Native Android
Add/Cancel preserves all independent fixture fields; one fictitious unsaved-draft confirmation is
explicitly discarded without Save. Main emulator/Samsung repeat only the changed action/viewport path
after no-reset upgrade, retaining Home 0/979 and exact settings. The Windows separate-desktop modal
COM failure remains a separate limitation; actual modal acceptance comes from Android. These native
observations are not new managed unit tests. [Evidence](quality/home-add-header.md).

## AT-376 / D-376 named getting-started tasks

Fourteen linked original application-policy cases distinguish all entry kinds, Once versus every
supported recurrence, Week/TwoWeeks versus Month, independent completion and removal/empty state.
Native fixture: two accounts, one FX transfer, one Once plan and one Week budget. All three original
steps remain incomplete, visible, enabled and unstruck in 18 Windows contexts (en/fa/de, Light/Dark,
360/412/1280) and six actual OS 200%/360 dp Release/AOT Android contexts. Original labels/actions and
complete account/entry/settings/plan/budget fields are preserved. Restore exact emulator settings,
upgrade main without reset and match original native Home. No financial Save in main, new phone
acceptance, benchmark claim or full-suite repetition. [Evidence](quality/home-getting-started.md).

## AT-377 - Original empty tag materialization

Nine new related cases compare exact/noncanonical/escaped/null/duplicate tag JSON and failure types
against independent EF, require separate mutable empty lists and preserve an intercepted nonempty
projection despite the original empty marker. All 78 HomeReadSnapshot cases pass; repeat only the
nine affected cases after the final binary-collation guard. Current original protected Home x64 and
Samsung reports compare all schema/typed rows and original native/financial presentation. Keep the
actual native SIGSEGV and retained-case continuation; this does not accept general device stability,
cold startup or a two-second Home target. [Evidence](quality/home-empty-tag-materialization.md).

## AT-378 - Bounded original native runtime diagnosis

Native-only evidence, no new unit cases: exact ELF/debuglink/DWARF identification, no installed
canary-dump mode, exact runtime remset-only marker, six full original Home cases in PID20385 with
complete protected schema/typed-row and presentation equality. Ordinary independent package and
unchanged Main979/OS restoration pass. No fatal remset assertion in this cohort is not acceptance
of general native stability or a loading target. [Evidence](quality/android-home-bridge-fault.md).

## AT-379 - Withdrawn constructor-owned empty tags

No new test cases: all78 related original HomeReadSnapshot cases pass for the candidate, including
independent mutable lists, noncanonical/error/intercepted representations and captured data. Six
original x64 Home contexts preserve full raw schema/typed rows and financial/native presentation.
Their slower timing cohort rejects promotion. Exact reader, ordinary caller/Main/OS restoration
passes; no ARM/phone input or general performance/stability acceptance.
[Evidence](quality/home-tag-list-candidate.md).


## AT-380 - Original document-copy and note disclosure

Original Samsung UI produces two accountant packets from one newly created fictitious profile: explicit
notes/known-PDF inclusion, then review invalidation and redaction. Actual packet digests, financial-field
equality and byte-exact PDF linkage pass; strict independent receiver build has no warnings/errors.
Retire only that profile and preserve original three profiles/Main979/samePID/OS/Main APK. Receiver and
hash-matched owned input are removed. No new unit count or production/APK change; D-377 remains current.
See [bounded evidence](quality/physical-accountant-document-disclosure.md).
Expert/legal, arbitrary documents, full-device stability, iOS and publication are not accepted here.


## AT-381 - Physical read-only import preview

Samsung original picker and retained three-page preview pass four native Next/Previous arrivals, visible
invalid final row/final-Next refusal, and stale generic sign invalidation until explicit Refresh. No
Import/Undo; all-history empty and zero balance before retiring only the fictitious profile and owned
inputs. Original Main979/three profiles/samePID/OS/APK match. No production change or new unit count.
[Evidence and remaining boundaries](quality/physical-import-preview.md). This is not all-device stability,
all languages/scales, arbitrary bank input, iOS or publication acceptance.


## AT-382 - Required identity storage and rejected descriptor

Eighteen new required-identity representation cases retain independent EF values or exact refusal types.
All96 related candidate cases pass; all18 new cases pass after exact source restoration. Six original
protected native Home cases preserve complete schema/typed rows/presentation, but the candidate is
withdrawn for lack of whole-page benefit. Restore ordinary independent package/Main/OS, clean owned
outputs and retain the negative receipts. No Main APK, phone trial, broader suite or performance claim.
[Evidence and remaining boundaries](quality/home-guid-storage-candidate.md). QA-06, native GC/ANR,
reader/full-device/iOS/provider/Store and release remain open; continue ready work from measured causes.

## AT-383 - Bounded original recent Home rows

Ten new actual-source recent-selection cases and strict Windows/complete offline Android builds pass.
The original six x64 and six Samsung contexts preserve full schema/typed rows, financial/native text and
explicit recent identities/packets. Native SIGSEGVs are retained; only unfinished cases continue on the
same immutable source/package/data. Targeted recent publication work shrinks; whole Home has no proven
speedup. No-reset Main upgrades preserve original Home0/Home979, settings and package identity.
[Evidence and boundaries](quality/home-recent-selection.md). General GC/ANR, cold/low-memory, reader,
full-device/iOS/provider/Store and release acceptance remain open.

## AT-384 - Unaccepted combined GC configuration

A distinct artifact-only serial-major/64MiB-nursery configuration completes the original six x64
Home contexts with complete financial/native/recent/data equality and eight root/six native Back
checks. Public dispatcher-gap medians worsen; memory observations are neither peak nor comparative
acceptance. Do not infer a native stability fix, extend to the phone or ship it. Restore the ordinary
D-383 caller and exact original Main/OS; preserve all actual old crashes/ANR. No new managed tests,
production/APK change or unrelated suite. [Evidence](quality/android-gc-nursery-trial.md).


## AT-385 - Complete bounded CSV input and retained review

Actual source tests cover all byte/decoded/shape boundaries, fragmented encodings, cancellation/I/O,
100000 original exported rows, complete ledger/holding review and accepted-outcome retention, empty/
header-only sources and mapping changes at both asynchronous validation boundaries. Forty-six new
cases pass selectively. Original Windows and independent Release/AOT Android pages prove translated
errors and retained state; actual native-provider scope is recorded separately. No Import/Undo is
invoked by the running-page qualification. [Exact receipts and limits](quality/csv-input-safety.md).
Unchanged successful suites, financial rules and external acceptance are not replayed or inferred.


## AT-387 - Malformed quotation cannot become a larger amount

The old generic path joins `"12"34` into1234 and previews123400 minor units; assert refusal before
financial preview and preserve original App filename/review/data. All22 new cases pass with complete
valid Unicode/quoted/empty/record-boundary compatibility. Only affected existing CSV/bank/large-source
cases repeat. Eighteen Windows and six original Release/AOT Android contexts qualify the changed
error and retained state; no Import/Undo. [Exact evidence and platform limits](quality/csv-quotation-integrity.md).

## D-389 - Explicit daily reference evidence

AT-299 adds bounded approved ECB decoding, all30 ordered pairs, shared feed/cancellation, unchanged
last-good cache, explicit date precision, version-one compatibility, original daily consent/cold history,
portable restore, signed encrypted household source transfer and six-language pre-payment notice checks.
The HTTP route check uses synthetic authentication/upstream effects; it is not real account/TLS acceptance.
The separate actual fixed-feed probe uses normal platform TLS and no account/financial payload.
Source layout cases and main-package upgrade evidence are recorded in quality/ecb-reference-integration.md;
these do not accept genuine purchases, public hosting, all phone behavior, iOS or release.

## D-390 - Distinct supported bridge candidate remains rejected

The complete original six-case protected Home native/data cohort passes in PID15917, including
full schema/typed-row preservation and original focus. All twelve corresponding historical dispatcher
gaps worsen; no causal speed or random-crash fix is established. Do not extend the rejected mode to
phone/lifecycle cohorts. Exact ordinary caller and same Main/Home/OS restoration plus owned cleanup
pass. No new managed case or unrelated successful suite/build replay. QA-06 stays open.
[Evidence and scope](quality/android-new-bridge-trial.md).

## D-391 - Speech request text is distinct from audible acceptance

Pass nine actual original en/fa/de action/Amount/Cancel text/locale/role packets, correlated with
original native focus, and six actual reader open/cancel callbacks with zero-EUR Home returns.
Same Main15560; exact reader ERROR/overlay/toggles, ten secure values, app choices/Home/OS/APK restored;
owned independent helper/JAR/XML removed after hash matching. No production change, new managed
case or unchanged successful suite/build replay. Pronunciation/intelligibility, remaining workflows,
physical/iOS and full A11Y-01 acceptance stay unchecked; native runtime gates remain open.
[Original receipts and limits](quality/android-reader-speech-requests.md).


## D-392 - AT-79/QA-03 actual overlap and unsafe-Undo correction

Original Samsung explicit link/keep-both review, disabled incomplete choices, five-row190EUR result,
dependent seed refusal and cold detail Undo restoring two aggregates180EUR pass. Actual ordinary
title-edit Undo incorrectly deletes the row; seven of nine initial new SQLite cases reproduce the
missing snapshot protection. Final39 related cases across selective runs include ten new cases,
attachment ownership, legacy refusal, extra identity and backup/cold guards; no full suite repeats.

36 retained Windows aggregate forms (nine preserved/27 continuation),12 original Release/AOT Android
forms and Simple ordinary-row guards pass. Actual final Samsung shows the original aggregate range
1-7Oct and refuses the changed one-row batch/legacy seed, preserving three packets187EUR. Strict
Windows/complete signed offline Android builds, Main979/five known public packets/original three
profiles/OS and owned cleanup pass. Profile transient empty-tree/footer-height/action-sheet observer
failures are retained; financial actions are not replayed to repair observations.
Broader QA-03, QA-06/A11Y, hostile/bank corpus, iOS/provider/store/release acceptance remain open.
[Exact original/final package boundaries](quality/import-undo-and-retained-aggregates.md).

## D-393 - AT-80/QA-03 original physical bulk lifecycle

Samsung original controls pass retained selection across searches/Home, tag/category cancellation,
two-row tag/review, one-row category and mixed-kind refusal. One positive Delete of purchase plus
fee-bearing transfer observes984EUR; original Undo returns within5.047seconds of Delete under the
unchanged eight-second deadline. Original day net/Home973 returns before planned cold persistence.
All six cold native details preserve complete original money/date/account/payee/multiline-note packets,
intended tags/category, refund link and transfer fee relation. No original accepted financial action
is replayed for observer failures; no new managed cases or unchanged test/build reruns.

Only the new profile and matched input are retired. Main979/five known public packets/three profiles,
meaningful Home/OS/D-392 APK and phone release pass. This closes the bounded physical bulk gap, not
all AT-80/device/runtime/A11Y/iOS/provider/store/release gates.
[Original evidence and observer limits](quality/physical-bulk-corrections.md).

## D-394 - AT-81 physical asset consent and retained account refresh

Bounded original Samsung new Expense/Income and edit cancellation preserve drafts and asset values;
fresh accepted writes and an internal100EUR transfer preserve original meanings/metadata. Actual
stale tab presentation is reproduced, corrected, and the same installed fixture verifies automatic
7,905.66EUR after only the changed follow-up edit. Six actual Windows Shell/account contexts preserve
reconciliation drafts; two successful English contexts are retained during native-digit continuation.
Strict builds/complete offline signed APK pass. Existing architecture case and two meaningful negative
controls pass; no new managed count or unchanged suite replay. Planned cold persistence and exact
restoration are qualified separately from broad device/iOS/runtime/release gates.
[Full evidence](quality/physical-asset-consent-and-account-refresh.md).

## D-395 - Plans title Add and remaining content height

Check original Plans in en/fa/de, light/dark and360/412/1280 Windows widths: same48px title action,
full command/name, no duplicate native header, unchanged chips/warnings and list/empty viewport to
the bottom. Eighteen contexts and36 original app-window images qualify the changed geometry while
stored accounts/entries/plans/settings remain unchanged. Android native open/Cancel, final package
and preserved Main/OS observations are bounded in [D-395](quality/plans-add-header.md). Native cases
do not add managed test counts or establish broader platform/stability/accessibility acceptance.

## D-396 - AT-11 original account reconciliation

Twenty original application/session and real SQLite cases qualify stale-input/consent/source/file/
access refusal, matching balances, positive/negative correction, required reason, monotonic metadata,
overlapping operations, rollback, post-commit notification failure and arithmetic bounds. Thirty
related original ledger regressions pass; no full-suite replay or implied new total from repeated cases.
Six original en/fa/de light/dark Windows native consent cases and eighteen scoped visible error images
pass. Original Android native edits retire old comparison, missing reason refuses, Cancel preserves
the draft and one Accept records112.34EUR correction to212.34EUR. The original row/reason/date/account
and reconciled date persist across one planned cold process without financial replay.
See [D-396](quality/reconciliation-review.md) for restoration, candidate and remaining platform gates.

## D-397 - AT-133 retained advance-bill commit

The original post-commit exception regression fails before correction. Sixteen new actual application/
SQLite cases then pass: extra/refund bills, committed subscriber failure, native-return refusal,
overlapping actions, stale advances, discard, independent validation, SQL rollback/retry, exact-file/
currency refusal, late input retirement, failed-load retry and accepted-form retirement. Fifty-five
related retained-write-policy cases pass; no full/unrelated suite replay or double-counted execution.
Six Windows original financial Save/Back cases and18 images retain accepted money. A separate Home
read failure remains explicitly unaccepted. Six Android compiled-source cases preserve first accepted
native evidence/readback while executing only the five unfinished contexts; Main0/OS/APK restore.
[Scope and retained failures](quality/settlement-commit-lifetime.md) do not establish phone/iOS/release
or all-histories/general runtime/accessibility acceptance.

## D-398 - AT-50 original Home supersession boundary

Seventeen focused application/SQLite behaviors pass, including seven persisted-source additions,
combined mutated plan/goal plus legitimate edits, same-file binding, bounded failure/explicit retry,
native retirement, ordinary failure and cancellation.96 related Home reader contracts and21 original
template-navigation cases pass; earlier subsets are not additional distinct tests or a full-suite run.
Six retained en/fa/de light/dark native contexts per platform plus one new account-reference context
prove original covered/current publication and preserved values. Windows7 own-window captures are
inspected; Android uses actual Release/AOT Home with an isolated plain SQLite fixture. Final Main
upgrade preserves Home0/meaningful controls and original OS state. This is not broader AT-50/device,
performance, generic stability, screen-reader, iOS or release acceptance.

## D-399 - AT-66/AT-138 committed occurrence notification outcomes

Fifty actual application/SQLite cases pass:21 new post-commit outcomes, generated/manual reopening,
busy/native-alert failure priority, six-language notification/cancellation semantics and29 retained
SQL rollback, consent, publication and inline validation behaviors.70 related occurrence write-policy
cases pass, including actual-file/access retirement, complete relationships and deletion journals.
Earlier ten-case subsets are not extra distinct cases or a full-suite run.

Eight original native commands per platform finish once: partial/full, link, skip, override, undo skip,
generated reopen and manual unlink. Original snapshots retire before the saved alert; partial400
refreshes to remaining600, full1234 settles, manual950 retains its identity/value, and reopen suppresses
automatic recreation.18 inspected Windows popup images and six Android message views are read-only
copy checks after accepted actions. Android17 exact answers, Main0/meaningful controls and OS restoration
pass. This is not all-history/phone/iOS, durable process-death idempotency, performance or release acceptance.

## D-400 - AT-66 / AT-138 retained state conflicts

Five new actual-command/SQLite cases preserve complete pre-attempt table snapshots when a newer
settlement or skip conflicts with the retained form.55 related application cases and70 existing
policy cases pass; tightened completion/exception assertions rerun only the five changed cases.
Four original VM/page/native-button refusals on each Windows and Release/AOT Android keep accepted
entries/state and the original draft, including the native consent boundary. Six en/fa/de light/dark
412px unchanged native messages per platform and11 single Android answers pass. Main/OS restored;
Samsung/iOS and broad runtime/accessibility acceptance are not inferred. See quality/occurrence-state-conflicts.md.

## D-401 - AT-26/28/29/31 physical correction and retained suggestions

Original Samsung override12/due21/note versus unchanged definition10/Once20, Skip/Unskip with zero
ledger rows, manual12 and cancel/accept link, cancel/accept reopen keeping/unlinking manual money,
and distinct generated7 cancel/accept reopening deleting only that generated row pass. One cold
read preserves original saved values, manual12 and both unresolved states; unsaved draft changes
do not persist. Main979/five known public rows/three profiles/OS match; only the new inactive test
profile is retired. This does not replay D-316 partial/final work or establish all QA-01/device,
automatic-posting timing, real authentication, power-loss, iOS or release acceptance.

The observed retained-tab defect is reproduced before correction. Same-page metadata-only return
then shows the new manual title with raw drafts retained. Six original native contexts per Windows/
Release AOT Android cover coalesced reads, translated error/Retry and target refusal; one affected
pending-writer/default check per platform is new qualification, not accepted-case replay. Thirteen
existing snapshot tests/21 resource cases pass; no new full-suite count is inferred.

## D-402 - Original physical prior-period/cross-account refunds

The original refund draft is cancelled before its first accepted Save. One old purchase100 and
current refunds25 to CashA plus35/40 to CashB retain their original relationship/category/date/note.
Attempted41 is refused with the same draft; only its amount is corrected to40. One cold read shows
zero refundable, current refunds100, complete-history net zero, account balances minus75/plus75 and
all four original public details. Main979/five known packets/three profiles/OS match and only the new
profile is retired. Native observer/input repairs do not replay money or the cold process. No app,
managed-count or build change; broad chart/device/iOS/runtime/release gates remain distinct. See
quality/physical-refund-lifecycle.md.

## D-403 - AT-57 independent physical reinstall and original portable restoration

Complete signed offline original Release is compiled under a fixed independent id and installed only
after both fixtures are absent. Original Cash37/expense2/Once9-skipped/Advanced/Monday data restores
from its exact native-shared unprotected export after actual uninstall/identical-APK reinstall, before
any first account is created. Original preview/cancel/positive consent, Home35, expense metadata,
skipped history, one cold read and portable Monday pass. A second original export matches all51-table
schema/74 typed rows except the existing Settings.UpdatedAt ticks; no duplicate account. Two targeted
verifier negatives and fixed-identity/Debug refusal pass. Strict builds/signatures/offline completeness
pass; production code/APK, managed counts and unchanged prior successes are retained. Main979/same
PID10005/three profiles/APK/OS match after exact owned cleanup and phone release. This is bounded
unprotected same-phone independent reinstall evidence, not another phone, protected-password/iOS,
whole QA-01, power-loss/ANR/A11Y/provider/store/release acceptance. See quality/physical-portable-fresh-install.md.

## D-404 - AT-07/AT-10 original physical credit-card and account archive lifecycle

The retained D-401 candidate qualifies one card purchase60 and Checking-to-Card payment90, with
Checking410/Card positive30/Home440. Dirty-name archive cancellation preserves the draft; one accepted
archive keeps stored identity/history, ends both future expense/source-destination transfer plans and
clears the exact default. Original archived Reports retains income0/spending60. Unarchive and one cold
read preserve both ledger details/notes/dates, both Ended plans and no default. Main979/five known public
packets/three profiles/OS/APK are restored after retiring only the new inactive profile and releasing
the phone. No source/APK/managed-count/build change or successful unrelated rerun. Wider device,
paused-plan/currency/iOS, D-386 runtime/A11Y, provider/store/release and exact-head CI remain separate.
See [bounded receipts and limits](quality/physical-credit-card-archive.md).

## D-405 - AT-09/IO-12 original ordinary CSV financial review

A changed opening date after own/mapped CSV preview could silently change an imported row's balance
interpretation. Two original app/SQLite regressions fail on unchanged production. Ordinary previews
now bind the existing coherent file/accounts/categories/history/journal packet to the same atomic
writer. Expected stale refusal keeps raw input/choices, disables Import and requires explicit Refresh;
departed forms retire their token. Fresh writer rights and bank/holdings/accepted-result contracts stay.
Eight new cases, retained103 related successes plus the one corrected file-recheck observer,12 native
financial contexts per Windows/Release AOT Android, strict builds/full signed offline APK and Main's
no-reset upgrade/Home0/settings pass. Accepted cases are retained across observer repairs; no financial
replay or full-suite rerun. No schema/model or permission change. Broader device/import/ANR/A11Y/iOS/
provider/release and exact-head CI remain separate. See [scope and preserved negatives](quality/original-csv-financial-review.md).

## D-406 - Original physical opening-boundary CSV and retained-tab review

Samsung's original picker/preview/import stores one older12.34 expense and opening-day20 income in
one new fictitious Cash900 profile. Home920 excludes the older row; explicitly moving only opening
October20 to18 gives907.66. One planned cold preserves both original public details/dates/notes and
the durable two-row batch. Original Undo restores Home900/empty ledger while opening18 remains.
A real retained More-tab departure did not call OnDisappearing and left Import enabled. The original
Shell now retires that review on uncancelled tab navigation after existing leave guards. Final full
signed offline Release upgrades without reset and physically proves disabled Import, retained source
and explicit Refresh without financial replay. Strict Windows and complete Android builds have no
warnings/errors. Managed algorithms/tests are unchanged; retained D-405 successes are not rerun or
counted as new cases. Main979/five public packets/three profiles/OS are restored; only the new inactive profile
and exact hash-matched public file are removed, and the coordinated phone turn is released. No schema/model, resource, permission or provider change.
Broader device/import/report, D-386 runtime/A11Y, iOS/store/release and exact-head CI remain separate.
See [bounded evidence](quality/physical-opening-csv.md) for original receipts, preserved failures and completion status.


## D-407 - Original split/join financial state and physical lifecycle

Two real SQLite regressions first prove that retained split/join could overwrite a newer original
note. The coherent actual-file/group/account/category/refund/attachment review is now rechecked in
the existing atomic writer before EntryActions mutates copies. Changed sources/pending input refuse
before money and retain raw shares; known committed notification failures leave the original form
terminal/Close-only. Original writer rights, exact total and attachment ownership remain preserved.
Sixteen new Data and3 actual app-session cases pass selectively, with retained two-case native
Windows/Release AOT Android finance,36 Windows observations across en/fa/de light/dark360/412/1280
and12 Android observations. Observer failures continue the original stored refusal and pending
acceptance without reseeding or replaying successful money. Strict builds and complete signed offline
Main APK pass. Samsung's original new Cash900 profile records one100 expense, splits60/40, reallocates
70/30, preserves both public metadata packets and Home800 through one cold read, then combines once
to one100 Food row/Home800. Main979/five known public packets/three profiles/OS are restored and only
the new inactive fixture is retired before releasing the phone. No full-suite/unchanged successful
matrix rerun, model/schema/permission/provider change or release claim. Broader QA-01, D-386 runtime/
A11Y, iOS/store/external and exact-head CI remain separate. See [original evidence and limits](quality/original-split-financial-review.md).


## D-408 - Original holding event review and retained acceptance

Two real SQLite regressions first reproduce a stale correction replacing newer source data and an
ordinary exception after a purchase already committed. The original event/type/history/locations/
accounts/group/refund/attachment/valuation packet is now rechecked in the existing atomic quantity/
payment/fee/price writer. Copies preserve raw drafts and immutable reviewed facts. Foreign money or
unreviewed event identity, changed sources/access and pending input refuse before writes. Known
accepted notification/native-return failures retire the original form with Close-only navigation.
One explicit new location can be adopted only while all original financial facts stay unchanged.
An observed native empty-query redelivery now preserves the loaded review instead of losing it.
Twenty new Data and3 actual app-session cases pass selectively, plus46 original related cases once.
Two original financial cases per independent Windows/Release AOT Android preserve exact quantity,
fee/group/derived price, Cash874/929 and income0/expense1.36 Windows observations cover en/fa/de,
both themes and360/412/1280;12 Android observations cover the new native feedback and retained state.
Original Windows pre-command data is continued without reseeding or accepted-money replay. Strict
builds and complete signed offline Main pass; the independent emulator restores Home0/My account,
exact OS values/size/density and final Main hash through one no-reset upgrade. No physical phone,
private installed key/database, schema/model/permission/provider change or full-suite replay. Wider
holding relationships/device/iOS, D-386 runtime/A11Y and external/release/CI gates remain separate.
See [original receipts and limits](quality/original-holding-event-review.md).


## D-409 - Original refunded-fee corrections (REF-04 / AT-15)

Three preserved pre-fix SQLite failures now refuse before writes. Selective8 Data and5 Core cases
cover amount/currency/both errors, partial/same-review correction, overflow-safe totals, final-batch
currency correction and valid fee removal with preserved refund metadata;17 affected old cases pass
once. Windows and Release/AOT Android each invoke two original native refused/corrected forms once.
All typed rows stay unchanged on refusal; raw drafts survive; valid acceptance keeps refund75 minor,
quantity1000/basis2500/derived price2500000, producing Cash87500/87475 minor and expense0/25 minor.
Four original live observations and36 Windows/12 Android read-only captions pass, with complete
existing translated field errors and no financial replay. Final complete signed offline Main and
one no-reset emulator Home0/OS restoration pass. AT-15's earlier physical D-402 evidence is retained;
this extends local/native correction coverage, not physical holding or all-device/iOS acceptance.
See quality/holding-fee-refund-limits.md for exact runs, package hashes and observer limits.


## D-410 - Actual optional holding-fee input (REF-04 / AT-410)

Seventeen new actual editor/SQLite cases cover malformed/negative/range refusal and same-form retry,
blank/zero/positive meaning, six regional negative drafts/corrections including Persian digits,
explicit dirty Cancel choices, nonfinancial hidden fees and no wrapped/guessed prediction. Three
baseline failures are preserved; six regional fixture failures before Save were corrected and only
those rerun, retaining the other successes. Native malformed/negative corrections preserve exact
refund75/fee100 minor, Cash87475, income0/expense25 and holding quantity/basis/price in both independent
Windows and Release/AOT Android. Four live assertions precede original accepted receipts. Windows22
successful images and Android6 final caption observations qualify complete field feedback in en/fa/de
and both themes with Windows360/412/wide. Caption continuations invoke no financial commands. Signed
offline Main and one no-reset emulator Home/OS restoration pass. This is bounded local/native
acceptance; physical holding variants, all-device/iOS, D-386 and external gates remain separate.

## D-411 - QA-01 original holding purchase, fee refund, correction and sale

Scoped Samsung acceptance: original counted purchase2/50EUR/fee2, linked fee refund1.50, raw amount
range refusal, below-refund refusal and valid same-form fee1.50, then sale1/30EUR/fee0.50. Cash879.50,
five known packets, quantity1/basis25/price25 and displayed realized5 remain after one planned cold.
The original missing fee-refund UI is fixed without replaying the accepted purchase. Main979/five
known packets/original three profiles and OS restore; only the new inactive fixture is retired.
Independent qualification includes26 Windows captures and24 final Android read-only action contexts,
with retained original rows and no new financial Save. Windows native open/Cancel succeeds; independent
Android modal/route failures remain unaccepted. No new unit-test count or successful/full-suite replay.
Other holding kinds, all-device, D-386 runtime/A11Y, iOS/external/release gates remain. Exact artifacts,
package hashes and limitations: quality/physical-holding-fee-lifecycle.md.

## D-412 - AT-412 original quantity input and correction

Sixteen distinct new cases cover parseable Decimal overflow, exact stored unit boundaries and ordinary
invalid semantics, plus actual editor/SQLite raw refusal and same-form valid correction. Twenty-one
existing resource checks cover the changed six-language feedback. Two original native cases per
Windows/Release AOT Android refuse without row/draft/review loss, then accept one explicit2unit
correction with original money/refund/price/source preserved, total3units/basis25EUR/Cash874.75.
Four original live observations per host remain retained. Field placement is then qualified only
read-only:18 final Windows contexts and6 final Android contexts, original Cases/PID, Live empty,
zero continuation commands and unchanged original stores. Final complete signed APK/builds and
no-reset emulator Home/OS/hash restoration pass. No full-suite replay or Samsung/iOS/general
runtime/A11Y/release claim. Details: quality/holding-quantity-input.md.

## D-413 - Original manual holding-price Save/delete and accepted result

Bind manual prices and confirmed deletion to a coherent original type/history/price/location packet,
revalidated in the existing actual-file writer with fresh rights and current draft/confirmation.
Keep raw price/date/mode on changed facts, retire departed authority and offer explicit Refresh.
Known accepted Save/delete survives notification/readback failure without another write; an old-row
deletion preserves a separate new-price draft. An ordinary empty return may refresh history; raw or
failed drafts require explicit review. Keep field-local feedback, scalable native actions and six
translations. Fifteen new selective actual app/SQLite cases,74 affected policy checks and21 resource
checks pass; original native receipts and read-only continuation keep successful financial operations
from being replayed. Full runtime evidence and limits: [original manual price review](quality/holding-price-review.md).

## D-414 - Original holding history deletion and file-bound short Undo

Complete the original holding delete/Undo cycle: coherent displayed-row/group confirmation, fresh
actual-file writer rights/source checks, exact committed deletion receipt and accepted restoration.
Preserve explicit fee-refund links and orphan receipt ownership/content; reject changed/foreign/
mutated/consumed Undo before money and recheck the restored original packet. Keep uncommitted retry,
retire departed actions and distinguish known acceptance from failed notification/readback. Twenty
new selective actual app/SQLite cases, four affected prior cases and21 resource checks pass. Original
native commands and retained read-only captions are qualified separately, without replaying accepted
work. Short Undo remains process-local; no schema/model, new permission/provider or release claim.
See [original evidence and remaining gates](quality/holding-history-undo.md).


## D-415 - Original holding archive confirmation and accepted result

The detail archive no longer saves a retained whole-type copy. Revalidate the original coherent
actual-file type/history/prices/locations packet with fresh Corrections rights and current confirmation,
then change only the tracked type's archive flag. Refuse stale/foreign/mutated/consumed or departed
review, preserve financial history/refund/receipt ownership and raw price drafts, and retain known
acceptance before fallible notification/return. Explicit Refresh reads only; absent/failed reads never
rearm a stale or accepted archive. Thirteen new actual app/SQLite cases pass in selective retained
cohorts, without repeating accepted flows;23 resource checks also pass. Original Windows/Android
Cancel/refusal/archive and read-only captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass. No schema/model, new permission/provider or release claim. Evidence and remaining platform gates: [original archive review](quality/holding-archive.md).


## D-416 - Original type editor creation, edit and accepted identity

The actual type form now owns one coherent actual-file original type/history/price review, or one
stable absent new identity. Recheck fresh rights, immutable source and exact pending raw draft in
the existing writer; copy before mutation and preserve noneditable metadata and dimension rules.
A missing original row never becomes a new form on empty query redelivery. Keep invalid/raw input
for correction, retire stale/departed authority, and require close/reopen after changed source.
Known creation/edit acceptance consumes the original form before notification or navigation; a failed
next step cannot create a duplicate or repeat Save. Nineteen new actual app/SQLite cases pass in two
selective cohorts, without replaying accepted cases;23 resource checks also pass. Original Windows/
Android refusal/edit/create and retired captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass; no schema/model, new permission/provider or release claim. See
[original editor evidence and remaining gates](quality/holding-type-editor.md).

## D-417 - Original physical holding lifecycle

The existing D-416 signed offline candidate passes one original Samsung type creation/edit,
quantity refusal/correction, manual price/delete, immediate purchase-group Undo and archive/cold
lifecycle. Cash849.50, original refund link, type name/note and history persist; Main979/five public
packets/exact three profiles/OS are restored and the phone turn released. Only the new inactive
fixture is retired. No new app/APK/tests/builds or wider D-386/iOS/external/release acceptance is
claimed. The observed refund category is Uncategorized, without a Fees-selection claim. See
[original evidence, observer continuations and remaining gates](quality/physical-holding-type-price-history.md).

## D-418 - One original coherent holding overview

Replace separate initializing/profile-following overview reads with one actual-file worker transaction
and a complete prepared row/value packet. Recheck original source/rights, bound same-file supersession,
cover loading/failure with explicit Retry, and retire departed publication/native targets. Open/Add
check the actual file even under inactive test policy; an empty read creates no settings. Nineteen
distinct new actual app/SQLite cases and23 resource checks pass in selective cohorts, without a full
suite or accepted financial replay. Original Windows and Release/AOT Android loading/error/Retry,
ready/empty, retirement and native targets preserve all typed fixture rows. Only changed six-language
loading/Retry captions are rechecked after visual correction. Strict builds, complete signed offline
APK and no-reset emulator Main/Home0/OS restoration pass; Samsung is untouched. No schema/model or
privacy boundary changes, speed/general ANR/crash/A11Y/device/iOS/store/release acceptance is claimed.
See [original evidence, observer continuation and limits](quality/holding-overview.md).

## D-419 - Complete original holding detail publication and routed identity

Read one actual-file detail/settings packet on a worker without initializing settings, prepare all
quantity/value/history/price fields and publish under an original loading/error/Retry cover. Retire
departed/retained-tab work and recheck original native navigation/file targets. Preserve raw price
input for explicit Refresh, existing accepted editor outcomes and theme-only color rebinding. A new
routed type receives its own editors; old drafts/short Undo/native targets never move to it. Twenty
distinct new actual app/SQLite cases pass in selective cohorts; no full suite or unchanged resource/
financial replay. Retained original fixtures qualify57 Windows views plus only two new query views
and23 Release/AOT Android observations, with no seed/UI financial command. Strict builds, complete
signed offline APK and no-reset emulator Main/Home0/exact OS restoration pass; Samsung is untouched.
A final known-commit/departure test and one retained-source AOT callback control close automatic
readback to a departed view, without replaying completed native matrices. No schema/model/privacy boundary change or general runtime/A11Y/device/iOS/release acceptance.
See [original failures, continuations and limits](quality/holding-detail-publication.md).

## D-420 - Original holding-place creation and known metadata outcome

Create a named place only under the original actual-file holding review and pending form owner,
with fresh rights and a stable absent identity. Prompt ownership closes financial Save before the
name dialog. Recheck source and owner in the same transaction, consume the old review on metadata
acceptance, and retain that known identity before notification/readback. A failed following step
shows saved-place feedback and an explicit read-only choice refresh; it never creates the place
again or resets raw financial inputs. Transfer keeps its original source and selects the new destination.
A departed/replaced form cannot adopt old metadata or recover financial authority.

Eighteen distinct new actual app/SQLite cases and 23 resource checks pass in selective cohorts.
The three original baselines fail before repair; later cohorts run only new boundaries/outcomes.
Original Windows UIA and independent Release/AOT Android native controls each create one place and
recover its choices read-only, with zero financial Save, then retain captions across the required
language/theme/width contexts. All typed rows outside that one location addition remain unchanged.
Strict builds, complete signed offline APK, no-reset Main/Home0 and exact emulator OS restoration
pass. No schema/model, new data/permission/SDK/provider or general ANR/crash/A11Y/phone/iOS/release
acceptance is claimed. D-386 and remaining external gates stand.

See [original failures, focused evidence and limits](quality/holding-location-creation.md).

## D-421 - Complete original holding-event form publication and routed draft ownership

Keep the event form covered until its original coherent read, actual-file revalidation and complete
field publication succeed. Failed reads offer explicit original-file Retry and Cancel; they do not
initialize display settings or leak an exception out of query delivery. Validate the original event/
type before missing-default scaffolding. Required default creation retains Corrections policy and
one known metadata receipt through readback failure, without recreating a deleted accepted default.
A published raw draft gets read-only Retry; unavailable kind/account/place is never replaced silently.

A new routed event/type owns a blank draft and separate financial session. Empty redelivery retains
the existing form. Original pending/departed reads and accepted financial callbacks cannot publish,
close or activate another view. Keep archived historical payment/place identities and rebind a
retained transfer destination by identity. 22 distinct actual app/SQLite cases pass in selective
cohorts, followed only by affected checks. Final strict builds, 56 actual Windows views and 20
independent Release/AOT Android observations pass with original typed rows unchanged. Complete signed
offline APK and no-reset Main/Home0/exact emulator restoration pass. No unchanged full suite or
resource matrix replay; no schema/model, new permission/provider or general ANR/crash/A11Y/phone/iOS/
release acceptance. D-386 and remaining external gates stand.

See [original failures, native observations and limits](quality/holding-event-publication.md).

## D-422 - Original physical holding-place move and dirty discard

The existing D-421 signed offline APK passes one original Samsung opening3/basis30EUR and move1:
Home2 plus D422_Storage1, total3 and average basis10 remain, with Cash900 and an empty current-month
ledger. One new place is adopted by the original move draft without losing quantity/date/note or
changing its source. Keep editing retains raw2; Discard keeps the saved move1. One terminal cold
retains original history and quantities. Restore Main979/exact five public packets/three profiles/
OS/hash, retire only the new inactive fixture and release the coordinated phone turn.
Twenty-four retained-observation assertions are not new unit tests. No new app/APK/build or successful
suite/accepted financial sequence replay. A separate unsaved USD-type draft exposes unexpected
native currency-search text and is discarded; USD selection/new event and its root cause are not
qualified. Next ready work diagnoses only this original input/list boundary. D-386 general runtime/
A11Y, broader QA-01, Mac/iOS, external/expert/store and release gates remain separate.
See [original sequence, failed observations and limits](quality/physical-holding-location-move.md).

## D-423 - Original native currency selection, without a product workaround

The independent Release/AOT Android caller opens the actual unchanged AccountFormView currency
control once. After actual native realization, the original visible 44-unit GBP button selects GBP
in both the model and control and closes the popup. Stored accounts and entries compare unchanged;
there is no Save surface. The early active-window dump and raw ContentDescription were incomplete
observers, not proof of a blank product list. Preserve failed observations; remove all speculative
production viewport changes. Main code/APK remain D-421, Main resumes with its original PID/Home0,
and no phone input or system setting changes occur. The final independent strict build has zero
warnings/errors. No unchanged Main build/full suite or accepted financial sequence is repeated;
no new unit-test count, physical USD-input, spoken accessibility or general runtime acceptance is
claimed. Continue remaining ready feature/device work with D-386 and external gates separate.

## D-424 - Original entry-detail attachment owner and complete file names

Bind detail attachment reads, native picking and confirmed removal to the original displayed entry,
actual file, metadata and view owner; recheck cached rights and original bytes under the writer.
A changed/departed picker cannot target another query/profile. Consume known commits before
notification/readback; show the saved/reopen outcome without resubmission. Give the complete file
name its own row, separate from reading/removal actions.13 focused actual-source cases pass; no full
suite replay or schema/model/permission/provider change. Retain54 original Windows caption/Cancel
observations and18 changed-row continuations over the same original file. The latter completes its
report and then exits with native0xC0000005: general runtime acceptance is not claimed. Final strict
builds and complete signed offline APK pass. The normal installed Android emulator exercises one
original PDF pick/add, removal Cancel, original ledger Delete/Undo within8s, cold without the input,
and document-only removal with unchanged expense/balance. Restore Main0/original profile set/OS and
retire only the fixture. These are not physical-device, OCR/viewer, general ANR/A11Y, iOS or release
acceptance. Continue remaining ready original entry-editor/detail work under D-386.

## D-425 - Original editor Save and pending receipt

The original editor financial writer records acceptance before notification and captures its exact
saved attachment destination. Submitted financial rows/document bytes are detached; late profile,
query, draft or view changes cannot adopt the pending write. Document failure stays separate from
saved money and never rearms Save.12 distinct focused cases pass in cohorts10+2; no full-suite replay.
One real Windows editor/native Save preserves expense1234/document bytes through two deliberate
postcommit notification failures; original fixture/account seed is reused after failed observations.
See [evidence](quality/entry-editor-save.md). Final Android evidence and wider gates are recorded
there. No schema, compiled model, portable format, permission or provider change. Continue ready
original entry/detail work under D-386; general ANR/crash/A11Y and external release gates remain open.

Final installed Android qualification (D-425): upgrade the complete signed offline APK without reset,
then one original Home/PDF receipt and Save in a separate fictitious profile. The stored24.50EUR/
17October/Food/D425_Cash and13KB document survive one cold after the exact input is removed.
Restore Main0/original public profile set/current-month history/OS and retire only the inactive
fixture.12 focused cases, final strict Windows/Android builds and one actual Windows Save pass;
prior negative observers remain recorded. No physical phone, merchant-corpus, general ANR/A11Y,
iOS/provider/store or release acceptance; original edit/overlap/Undo/detail gates remain separate.

## D-426 - Complete original transaction detail publication and actions

Prepare original entry/reference/group/refund/holding/document/rule facts through one captured
actual-file worker transaction, without creating settings for display. Validate the complete packet
before covered synchronous publication. Keep loading, failed read-only Retry and missing states
separate. A routed identity owns its own session and command instances; old commands, confirmations,
template prompts, OCR/viewer work and departed reads cannot adopt a different publication.

Review updates only the original flag; explicit rule creation uses original category/match and
refuses an already equivalent rule. Delete retains the original exact group/refund/document Undo
contract and deadline. Consume known commits before notification; recovery after saved work only
rereads. A refused viewer request removes only its newly created cache file, while a possibly accepted
native request retains the existing startup cleanup policy.

35 distinct new actual-source behaviors and selective affected/resource checks pass; no unchanged
full suite is replayed. Original Windows evidence includes18 language/theme/width ready contexts,
covered failure/native Retry, query/missing/retired-owner cases and two one-time native metadata
commands through deliberate postcommit notification failures. Preserve the first pre-action report
serialization refusal and continue only its exact original file without reseeding. Final Android,
APK and delivery evidence is recorded in [the detailed qualification record](quality/entry-detail-publication.md). No schema/model, portable format,
permission/provider or broader ANR/crash/A11Y/physical-device/Mac/iOS/store/release acceptance is
inferred. Continue ready original financial workflow work under D-386 and the existing external gates.

## D-427 - Complete original transaction editor publication

AT-427:33 distinct actual-source cases pass with selective affected verification, plus one related
core tag case and23 resource cases. Original native evidence is96 Windows observations and36
independent Release/AOT Android observations, including exactly two Save invocations per platform.
Windows prepares its fictitious raw amount programmatically; only Android checks native amount
setter-to-binding. Both use actual native Save. Preserve every accepted original fixture/action and
the negative observer/runtime evidence. The final installed protected form opens and Cancels without
input/Save; Main0 and OS return unchanged. See [detailed scopes and limits](quality/entry-editor-publication.md).
This is not aggregate-overlap/Undo, full-suite, physical-phone, general runtime/A11Y, iOS or release acceptance.

## D-428 - Original transaction account/aggregate workflow

Explicit nested account creation/default publication retains the original raw/receipt draft; cancelled
or retired children cannot write/adopt another file. Newly introduced aggregate coverage is reviewed
individually and rechecked atomically, including document/refund/prior-consumption boundaries.
The existing adjustment journal retains manual source identity; short original Undo restores only
aggregates and keeps details. Acceptance precedes observers; saved forms remain Close-only.
Full native review bodies and truthful transient Home/Transactions Undo notices replace clipped or
misleading captions.22 focused new behaviors and23 final resource cases pass; no full-suite replay.
See [original workflow qualification](quality/entry-workflow-ownership.md) for native actions, preserved
failures, APK and scope. No schema/model/portable-format, permission/provider or external acceptance
change. D-386 still defers general runtime and Android accessibility until after ready features.

## D-429 - Original plan/debt form publication and Save ownership

Complete covered actual-file publication, explicit Retry/missing states, original raw/query/command
lifetime, typed transaction-to-plan handoffs and known nested account return are implemented. Debt
payment requires an active original-currency money account; unknown principal is not guessed.
Temporary readiness errors retain editable raw fields with Save disabled. Original writers validate
new/edit/split sources and full saved semantics atomically, preserving recurrence/history and recorded
money. Known acceptance precedes observers and cannot rearm.27 focused new actual-source behaviors
pass across retained cohorts; affected prior failures are corrected selectively, without a full-suite
replay. [Source/native/build evidence and limits](quality/plan-editor-ownership.md). No schema/model,
compiled-model, portable-format, permission/provider/SDK change. D-386 and external gates remain.

## D-430 / AT-430 - Original plan monetary interpretation

22 new cases cover immutable explicit-unit interpretation and actual SQLite plan flows, including
independent transfer formats, explicit reentry, hidden/unknown input, original refresh, stored-value
initialization and two real SaveChanges retirement boundaries. Affected parser/validator/resource
checks pass selectively. Original native transfer/debt observations and accepted Save checkpoints
are retained without another financial command. See [qualified evidence and negative observations](quality/plan-money-context.md).
No physical-device, OS accessibility, notification, iOS, provider, store or release acceptance is inferred.

## D-431 - Original goal/budget monetary meaning

Each target/contribution and total/category raw amount retains its independent original currency,
display unit and parse culture. Unit changes require complete clearing/reentry; partial edits cannot
renew meaning. Dirty context meaning survives reload, including identical digits in a new unit.
Hidden limits expose actionable review; share-of-income ignores irrelevant fixed amounts and physical
quantities retain their units. Original raw revision/native owner is checked under paired writers.
Known goal/budget/cut acceptance precedes notification and cannot rearm.25 new and75 affected prior
source cases pass selectively, without a full-suite replay. [Native/build evidence and retained failures](quality/goal-budget-money-context.md).
No schema/model/portable-format, permission/provider/SDK change. D-386 and external gates stand.

## AT-432 - Original transaction monetary input

Each original transaction amount, destination amount, source/destination fee, ISO purchase amount and
reimbursable amount retains its own currency, applicable display unit and parse culture. Unit/currency
changes require complete clearing/reentry; partial edits never renew meaning. Original labels and
format examples remain visible. Receipt/template adoption starts from authoritative minor units;
account Undo and read-only/nested return retain original context. Repeating-plan handoff validates only
its supported primary/destination amounts, without borrowing unused fee meaning. Split shares retain
independent contexts; Join uses the original stored total. Raw/context revisions are rechecked under
original writers; known acceptance remains final.27 new focused source cases and51 related prior cases
pass across selective executions, with no full-suite or unrelated successful-check replay.
Windows:112 original observations across18 language/theme/width contexts; independent Release/AOT Android:40 observations across six language/theme contexts. Each platform invokes two original transaction Saves and one split Save, with exact monetary readback and unchanged base installation. Receipt/template and repeating-plan return do not post extra money. General Android ANR, physical-device coverage, iOS and release remain unaccepted. [Original native/build evidence and retained failures](quality/entry-money-context.md).

## D-433 - Original CI regression boundaries

Reproduce both original managed failures, then qualify the eight Core and 11 Data cases and17 local native-toolchain policy cases. Seven source-gate cases and one original-receipt/membership case are new. Hosted bd6e9484 CI and native Windows pass; its Android package refusal is preserved below, followed by final hosted recovery. App/APK and existing runtime evidence stay D-432; no accepted native action or successful unrelated suite is replayed. [Evidence](quality/ci-common-recovery.md).


### D-433 hosted follow-up - Original Linux host snapshot

Commit bd6e9484 passes the complete CI workflow, CodeQL and the native Windows reproducibility/package/
persistence job. The Android native job refuses a changed ca-certificates package before compilation.
The Linux recipe restores the original signed Ubuntu snapshot with an isolated exact-release
certificate bootstrap, strict complete-index retrieval and unchanged final tool/NDK/compiler locks.
A fresh local image passes the original host verifier. Final hosted clean candidates and stock-suite
recovery pass on 05dbecf0; no native binary or accepted package is replaced. [Evidence](quality/ci-common-recovery.md).


### D-433 final hosted recovery

Commit `05dbecf089dabc5f433b12dbede82f4e97b10d2b` passes all three workflows:
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906007),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906028) and
[Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906072).
The actual Windows job passes exact compiler qualification, two clean candidates, immutable package,
drift refusal and protected persistence/workloads. Android passes host restoration, two clean
candidates, malformed-slice refusals and the official stock suite with reviewed static crypto.
The single scheduled check is complete and deleted; no successful job is rerun manually. The earlier
bd6e9484 Android package refusal remains historical failure evidence. This establishes hosted CI
recovery for that exact commit, not app/device/provider/store/release acceptance.


## D-434 - Original account monetary contexts

Opening balance, optional installment and observed balance retain independent original numeric
culture/currency/display-unit contexts. Display changes preserve raw input, original captions and
format examples; currency/unit changes require complete clearing/reentry. Preserve unknown principal,
historical loan/lent signs and decimal-rate meaning. Original file/presentation/edit revision guards
survive writer waits and reconciliation consent. Reject stored-currency changes before comparison,
including the match path. Postcommit default selection remains bound to the original file and native
owner; a failed follow-up never rearms the accepted account Save.

23 new distinct actual-source cases and 49 prior affected cases pass selectively. Original running
matrices complete 110 Windows/38 independent Release/AOT Android observations; two additional default
scenes and four read-only currency views per platform pass: totals116 Windows/44 Android. Final
strict builds and complete signed offline APK qualification are recorded in the linked evidence. No successful financial action or unrelated full suite is replayed.
See the account-money evidence document for exact identities, failures and limits. D-386 priorities
and physical-device/iOS/provider/store/release gates remain open. Holding monetary inputs follow as
the next separate development batch.

[Account monetary evidence](quality/account-money-context.md).


## D-435 - Original holding monetary contexts

Payment, fee, price-currency amount and opening/gift basis retain independent original currency,
display unit and numeric culture. Currency/unit changes require complete clearing/reentry. Quantity
keeps its original numeric culture because per-unit money depends on it; examples remain numeric.
Hidden fields do not reinterpret another event kind. Native account/unit/mode/kind choice and raw
edit revisions remain bound under original writers, including change-and-return. Manual-price
Refresh retains original raw meaning and known acceptance; old confirmations cannot rearm it.
Only a blank optional second amount requests unknown value; malformed nonblank input is refused.

24 distinct new source cases and 46 prior affected cases pass selectively; eleven original failures
are reproduced. Strict Windows, Android Debug and independent Release/AOT builds and the complete
signed offline APK pass without warnings/errors. 147 distinct Windows and 51 independent Android
observations complete across retained original scenes and final continuations, without accepted
purchase/opening replay. All 50 stored table sets match the original final checkpoint; actual Manual
and automatic Purchase valuations are verified separately. Main upgrades without reset with its
original account/Home0.00EUR and unchanged OS settings. No full-suite baseline is invented. D-386 stability/accessibility order and physical-device/iOS/provider/store/
publication gates remain unchanged.

[Original holding evidence](quality/holding-money-context.md).

## D-436 - Original secondary monetary contexts

Goal contributions/assumed prices, essential estimates, forecast prompts, occurrence overrides/payments,
settlement bills and shared cost/repayment/allocation inputs retain their original numeric culture,
currency and display unit. A later currency/unit change requires complete clearing and reentry;
partial editing cannot silently renew interpretation. Blank optional assumptions remain different
from malformed nonblank input. Estimates and earmarks do not become ledger movements.

Original file, stored facts, raw input revision and native owner are rechecked through asynchronous
consent and the actual atomic writer, including a change and return to the original display value.
Known acceptance retires the submitted input before notification/readback; a follow-up failure never
rearms the financial Save. Display-unit preferences publish globally only after the selective durable
Settings write has committed for the original owner. Long unit actions use the existing wrapping
button. Unit-refusal feedback is republished beside both original shared main amounts and shares.

Selective actual-source evidence is101 distinct new cases and149 affected prior cases, with17 original
source defects reproduced. All367 applicable Windows observations and127 independent Release/AOT
Android observations complete without accepted-action replay. The extra36 superseded household
feedback frames remain historical evidence. Android household review uses an actual new signed owner
admission and protected native source: old recovery/ownership/device revocations remain effective.
Its two contacts and one private KWD1001 cost are explicit independent setup, not cash ledger entries
or UI saves; the18 presentation states preserve all50 table sets and record zero accepted commands.

Strict Windows/Android Debug, independent Release/AOT and complete signed offline APK builds are
warning/error free. No schema/entity model, permission, external provider, commercial activation or
shipping SDK is added. No unrelated full-suite baseline or unit-test count is inferred from native
observations. D-386 dedicated stability/accessibility order and physical-device/iOS/hardware-key/
power-loss/provider/store/publication gates remain unchanged.

[Secondary monetary evidence](quality/secondary-money-context.md).

## D-437 - Original physical and manual-rate numeric meaning

Free holding-type purity and per-item gram weight retain independent original numeric cultures
through display changes and partial typing. Complete clearing renews only that field. Original
format examples and all field-local validation messages remain visible; original stored type and
raw/choice revisions are rechecked under the actual writer.

Manual exchange rates retain the original decimal parser, pair, file and immutable stored-rate
review through loading, Save and delete consent. A later source/pair requires explicit reentry.
Plain ratios never use display-unit factors or change recorded money. Known typed acceptance
consumes the submitted draft before fallible notification/readback; Refresh reads only. Small
positive rates keep complete decimal precision in the list and deletion consent, with a separate
estimate-badge row. Error categories translate without repeating the original command.

Selective evidence is 42 distinct new actual-source cases and 19 affected prior cases; eight actual
source failures were reproduced. Twelve affected resource checks pass. Final native/package
continuation and retained failures are recorded in the linked evidence; native observations do not
increase the unit-test count. No schema/model/portable format, permission, provider or shipping SDK
changes. D-386 stability/accessibility ordering and external release gates remain unchanged.

[Original physical/rate evidence](quality/physical-numeric-context.md).

Final scoped evidence is 203 applicable Windows and 71 independent Release/AOT Android observations,
including read-only recovery of 22 original outcome frames after a report-name collision. Accepted
operations are not replayed. Strict platform/independent and complete signed Offline APK builds pass;
Main upgrades without reset and its actual rates page/return preserve My account/0.00 EUR. Native
Windows 0xC0000005 after completed note captures and Android ANR/Wait remain real unresolved failures,
not stability acceptance. Full source/checkpoint/package details are in the linked evidence.

## D-438 scoped numeric OS evidence; native stability unaccepted

Actual independent Release/AOT OS200%/360/412dp review completes96 distinct physical/rate input
observations and360 native readings in en/fa/de, Light/Dark. Actual label text/ellipsis/final-line
height and raw input/original placeholders are checked; accepted commands are zero. Original final
50-table fixture/base rows and all recorded OS/Main values are preserved. This adds native evidence,
not managed tests or a full control/spoken-reader/phone/iOS acceptance. Actual startup ANR remains
recorded despite later same-process success. Exact retained Windows symbols and a new bounded API35
clock control establish no common corruptor, general fix or two-second goal.

[Scope, original failures and restoration](quality/retained-native-fault-classification.md).

## D-439 - Original monetary geometry scopes

Actual OS200%360/412dp independent Release/AOT review completes288 native observations/2340 readings
for original account/debt/balance, holding/quantity/price, entry/fees/foreign/reimbursable/split and
goal/budget notes/errors/raw fields, en/fa/de and both themes. All complete original50-table sources
and base rows remain unchanged. Native command guards refuse new accepted writers; original receipt
reuse is explicit. Complete text/ellipsis/final-line height and raw-input equality pass. All original
OS/Main values restore. These are not new managed tests, full controls/voice/device/iOS acceptance
or general stability; see quality/original-money-os-qualification.md. Retain D-438's successful scopes
and exact-commit CI; do not replay accepted financial actions or older successful suites.

## D-440 - Actual numeric InputConnection regression

Three original de-DE comma-loss cases fail before the adapter repair; all four affected baseline
inputs pass afterward, followed only by13 new edge/adapter observations and two password/length guards.
A later guard-only scope adds three input transitions and two read-only focus checks, preserving
the completed cases;20 inputs/four guards are applicable. Native read-only focus mismatch is corrected,
without claiming a financial editing exploit. Original failures and same-source continuations remain.
Complete original source/base rows stay unchanged, with no accepted Save or ledger entry. Native
observations do not add managed tests or prove every keyboard/composition/accessibility path. Strict
platform/APK checks pass; retain original ANR and remaining stability/spoken/device/iOS/release gates.
See quality/android-numeric-input.md.

## D-441 - Targeted secondary monetary OS geometry

Six original scopes complete204 observations/900 native readings: field96, Settings/prompt48,
unit60 at actual OS200%,360/412dp,en/fa/de,Light/Dark. Full text/last-line height, raw inputs and
native prompt44dp answer targets pass; complete source/base equality and zero accepted writes pass.
Restore all recorded OS values and original Main3522/Home139 nodes except focus. Native observations
are not managed tests or all-controls/audible/device/stability acceptance. See [evidence](quality/secondary-money-os-qualification.md).

## D-442 - Current Home/Plans header geometry

Actual OS200%,360/412dp,en/fa/de,Light/Dark passes36 observations/60 header states/120 text readings.
24 actual Add/Cancel sequences preserve complete source/base data; filled Plans uses original rows
without a command. Full native title/glyph, visible target, non-overlap and ordinary viewport pass.
Exact OS/Main3522/Home139-node restoration passes. These are not new managed cases, active Undo,
all-controls/spoken/device/stability acceptance. See [evidence](quality/header-os-qualification.md).

A further36 read-only native-coordinate observations verify the action is physically right of the
complete title (minimum gap8px), without replaying any Add/Cancel. Total D-442 evidence is72
observations/96 header states/192 native text readings; accepted writes remain zero.

## D-443 - Historical rate outcome system-scale geometry

36 observations/144 native readings pass actual OS200%,360/412dp,en/fa/de,Light/Dark for three
source-proven feedback categories, precise historical ratio/date and estimate badge. Full text,
last-line height and disabled native Delete targets pass; no commands, accepted writes or new rows.
The original rate remains deleted and complete source/base rows match. Exact OS/Main139-node state
restores. This does not qualify a new active rate, consent, spoken or general stability workflow.
See [evidence](quality/rate-outcomes-os-qualification.md).

## D-444 - Protected household current-authority geometry

36 observations/312 readings pass OS200%,360/412dp,en/fa/de,Light/Dark: original cost, unit refusal
and unsaved repayment unit refusal. Both actual reads require protected storage and current CanWrite,
original one cost/two contacts/zero repayments. No re-admission/authority refresh or accepted write.
Full native text/height/raw values and complete source/base equality pass. Restore exact old fixture
APK and OS/Main139 nodes. This does not complete spoken/device/general stability/release acceptance.
See [evidence](quality/household-os-qualification.md).

## D-445 - Diagnose original currency query input ownership

Two original emulator scopes produce ten native observations: arrow-only list opening retains the
name's modal-root focus; text sent there appends USD to the name. Explicit actual currency focus,
filtered native USD selection and Cancel/Discard succeed with complete source/base equality and
zero accepted writes. Activity.CurrentFocus alone is insufficient for modal ownership. A separate
signed offline arm64 empty-source prerequisite builds without warnings; phone input waits for the
requested exclusive coordination reply. Historical D-422 phone cause remains unproved. Shipping
app/APK and managed baseline remain D-440; no successful financial action or geometry is replayed.

See [currency focus evidence](quality/currency-input-focus-boundary.md).

## D-446 - Move original Home goal progress off the window thread

The unchanged pure goal evaluator now runs on a worker; original metadata/formatting and covered,
current-owner publication remain on the caller. Five new and three affected prior cases pass
selectively, including actual task/thread identity and complete original goal output/source equality.
Three native Home contexts on two retained active goals preserve source/base rows and window-thread
publication; no accepted seed or payment is replayed. Strict Windows/Android/independent builds and
complete signed Offline Main upgrade pass without warnings. Original Main Home139 nodes and OS
remain; no general ANR/crash/two-second/low-memory or phone/iOS/release acceptance is inferred.

See [Home goal worker evidence](quality/home-goal-worker.md).

## D-447 - Publish the complete original goal list under a local cover

The goal list now reads one captured original file/transaction, keeps its existing financial
calculation on a worker and publishes active/history/funding rows under one translated loading/error
cover. Exact row/command/page lifetime guards refuse stale input; explicit retry and detail return
preserve the original source. No financial algorithm, schema, permission or runtime policy changes.
Fourteen new actual-source cases, three affected prior cases and23 actual resource checks pass
selectively. Eighteen Windows contexts produce84 native observations, six Android contexts produce
36 observations (34new/2retained); original source/base rows remain identical with zero new writes.
Complete signed Offline Main upgrade and actual empty-list Add/Cancel pass without reset; Home139
nodes match the old upgrade except focus, and final return differs only in one drawing-order value.
Strict builds and test-output cleanup pass without warnings. Failed native target/capture-path
observers are preserved and corrected against original receipts without repeating completed scenes.
No general ANR/crash, two-second, low-memory, spoken, physical-phone, iOS or release acceptance is inferred.

See [original goal-list evidence](quality/goal-list-publication.md).

## D-448 - Keep complete original goal details and actions in their page lifetime

Original goal details now use one complete captured file/transaction and worker financial
calculation under a local loading/error/retry cover. Page/identity revisions retire old commands
and pending reads while retaining original dirty money inputs. State/delete writers check the
original action inside their transaction before changes and commit, and preserve known accepted
results across observer failure. Three original regressions reproduce identity, pending-delete and
file-adoption defects;23 new focused cases and58 affected prior goal cases pass in selective runs,
plus19 original monetary-context and23 resource cases. Native Windows146 and Android50 observations
(31new/19retained Android) preserve retained fixture rows with no new fixture writes or replayed fixture
actions. Strict builds, signed Offline Main upgrade/Home139-node/OS checks and test cleanup pass.
Observer/compile failures are retained and repaired without replaying completed native actions.
General stability/performance, spoken/device/iOS, all protected-Main goal variants, native
destructive confirmations and genuine external gates are not accepted by this finite batch.

See [original goal-detail evidence](quality/goal-detail-publication.md).

Final source regressions also cover failed/busy/cancelled Edit validation and retained-stack theme
refresh. Only2 extra native observations per platform qualify the new Edit refusal/retry path;
completed cohorts remain. One new production-app test profile/account-opening/goal Save and one
cold read qualify positive protected-Main account-balance details100/150/50EUR and Edit/Cancel,
with All-history empty. Only that new profile is removed via full two-question UI cleanup. Main's
profile-list/Home139 nodes and0.00EUR return unchanged; English/Dark/Simple and OS are read back.
These are controlled new metadata saves, not repeated fixture payments or full protected-goal,
cryptographic, stability, spoken, physical/iOS or release acceptance.

### D-449 - Original goal-page OS text and native toolbar height

The original Persian title needs58px but received48px at actual Android OS200% text. The owned
native toolbar grows for its measured final line without reducing text size or replacing navigation.
Observers retire with the toolbar and growth requests are deduplicated within that lifetime.
Forty-eight immutable contexts contain216 native observations across360/412dp, en/fa/de and both
themes; completed contexts and native Retry are retained rather than replayed. Original fixture/base
rows remain unchanged with zero financial writes. Two unresponsive candidate sequences and a lost
log-chunk report are preserved; compact read-only durable receipt verification completes the report
without replay. Strict Windows/Android builds, complete signed Offline APK384 AOT entries, same-data
Main upgrade, original Goals read and Home139-node return pass. Exact emulator OS settings return.
No managed test baseline increase or general ANR/crash, speed, spoken/physical/iOS, provider/store
or release acceptance is claimed. See [native goal-page evidence](quality/goal-page-os-text.md).

### D-450 - Retire native toolbar listeners on the current Android tree

Three actual native roots reproduce a cached dead observer while the current observer is alive and
different. An exact separate native listener now removes from the current live tree and detaches
its weak owner before releasing the toolbar. Six focused root observations at actual OS200%/360
and412dp retain one active listener and retire four previous owners; two original Persian title
checks retain34px text,58px complete height and66px toolbar. Independent base rows and financial
writes remain unchanged/zero. The direct-tracker candidate's real native activation crash and two
caller compile repairs are preserved. Strict Windows/Android builds and complete signed Offline
APK384 AOT entries, no-reset Main upgrade, Goals/Home139-node return in the same process and exact
OS restoration pass. The transient null tree is continued read-only without replayed input/restart.
MAUI source references are corrected to the actual unchanged10.0.110 pin. No new managed-test count,
replayed D-449216-scene scope or general GC/ANR/speed/spoken/physical/iOS/release acceptance is claimed.
See [native toolbar lifetime evidence](quality/native-toolbar-observer-lifetime.md).

### D-451 - Complete one original read-only navigation pulse sequence

After D-450, one Release/AOT process completes18 original detail/Back visits with en/fa/de and both
themes, one current native root/subscription per cycle, no financial commands and exact source/base
equality. A500ms at-most-one UI probe records maximum callback age524ms and no ages over2000ms;
read/publication/Back/Home intervals are812-1436ms, median942ms on the small original source, not
whole Home/cold/tenfold or two-second acceptance. The exact PID's event buffer retains its start
with zero recorded ANR/crash events. Strict independent Android/Windows builds and signed Offline
caller pass. Main remains D-450: same PID15971, Home139 nodes, display and all original OS values.
No old216-scene matrix, accepted financial action, full suite or unchanged shipping APK is repeated;
no managed-test baseline is added. General stability/performance/spoken/physical/iOS/release gates
remain open. See [original navigation evidence](quality/original-navigation-pulse.md).

### D-453 - Original Main cold start and retained-process background return

The unchanged complete signed D-450 Offline APK passes one genuinely cold Main launch on the
owned emulator: Android COLD/Activity2908ms, new PID17030 and exact original139-node public Home.
The package-scoped background-kill request leaves that same PID alive (LAST/previous); its later
HOT89ms return also preserves the complete public tree. This does not qualify process reclaim,
natural low-memory, whole-Home latency or private database equality. The exact PID's event buffer
retains its start and zero recorded ANR/crash events in this finite scope. APK/OS remain exact;
no financial commands, managed tests, unchanged builds or successful old sequences are repeated.
Only three exact hash-matched remote captures are removed. General stability/performance/spoken,
physical continuation, Mac/iOS and external release gates remain open. See [cold lifecycle evidence](quality/main-cold-lifecycle.md).

### D-454 - Recover the original Main task after confirmed cached-process death

A phase-free existing owned Activity supplies the missing cache precondition; preserve its first
LAST result, then require actual CAC/cached=true/adjustment900 before package-scoped kill. PID17030
is confirmed absent and its native intentional-kill event retained before recovery as17431. Android
reports COLD/Activity1334ms; the original139-node public Main, APK and OS values match. No review
route, financial command, build or old successful sequence runs. The quiet owned process is stopped
and two hash-matched remote captures removed. Natural low-memory, private full-data equality,
whole-Home timing and broader stability/platform/release gates remain open. See [cached-process evidence](quality/main-cold-lifecycle.md).

### D-455 - Bound the native scroll-readiness candidate without an unsupported product change

The pinned MAUI scroll wait supplies a concrete candidate, not an application-cause proof. The
original16-request control clears its native flag after two of twelve samples; no scroll is armed
and original source/base rows match. A separate hidden-root condition refuses readiness twice;
three actual snapshots establish attached/focused but already-laid-out412x640 geometry. Those
refusals are preserved, not relabelled as a scroll hang or passing regression. Three strict signed
Offline builds pass; experimental source is removed and the exact D-451 independent APK restored.
Main17431/139 nodes/D-450 APK/OS match; no financial command, managed-test increase or successful old
scope is replayed. Stability/performance and external gates stay open. See [native scroll evidence](quality/native-scroll-layout-investigation.md).

### D-457 - Complete retained physical USD selection and native draft discard

Three actual owned-window observations explain the failed observer: the USD native button is
124px high while its popup exposes123px. Preserve the full-width/owned semantic native target and
44dp minimum within that one-physical-pixel rounding allowance; production controls are unchanged.
The final ARM64 Release/AOT caller passes without warnings/errors,192 AOT entries, offline manifest
and v2/v3 signatures. Actual Samsung selection and Cancel/Discard succeed; four prior scenes are
retained, one new selection observation is added, accepted financial writes stay zero and complete
original source/base rows match. Remove only the new probe and two hash-matched remote captures;
Main7361/Home141/979, original APK/OS and prior packages match. Both coordinated phone turns are
released. Retain the typed wait, initial build-flag and host-verifier/decoder failures separately;
no managed-test increase or successful old suite/financial/matrix replay. D-452 is complete for
this controlled input scope; historical D-422 cause, general stability/spoken/platform/provider and
release acceptance remain open. See [physical continuation evidence](quality/currency-input-focus-boundary.md).

### D-458 - Continuous owner-reserved phone and original physical goal lifecycle

The owner reserves Samsung for this project until further notice; cross-project coordination is
no longer required, while data/settings/foreground/authentication limits remain. Reuse the exact
complete signed Offline D-450 APK and upgrade without reset. One new fictitious profile/account
opening100 and goal150 produce the actual100/150/50 list/detail and positive pinned Home card.
Dirty native Discard, pause/resume, confirmed completion/reactivation, archive, one cold read,
declined/accepted native goal deletion and empty All-history pass. Only the new profile is removed
through both native confirmations; original three profiles/Main979/141 post-upgrade nodes/OS and
APK hash match.34 assertions qualify stored observations, not new managed tests. Retain native
null-tree/tab/encoding/host-precondition failures; do not replay accepted commands or old suites.
General stability/performance/spoken/other goal/platform/provider/release gates stay open.
See [original physical goal evidence](quality/physical-goal-lifecycle.md).

### D-459 - Original physical earmark and holding-quantity goals

Continue the two remaining goal kinds on the owner-reserved Samsung using unchanged signed
Offline D-450. One new fictitious profile opens with cash900. Earmark150 starts at0, accepts
set-aside25, refuses release26 while preserving that draft, and accepts corrected release10:
progress15/150/135, unassigned885 and original+25/-10 history. One countable holding records
already-owned3 with original basis30, without a cash movement. Quantity goal5 shows3/5/2;
assumed price12 changes only the estimate, not quantity or holding value. Both pinned Home
cards and cash900 agree. One cold launch preserves both original goals, allocation history and
assumption; All/All-types ledger history is empty. Remove only the new profile through both
native confirmations. Original three profiles/Main979/all141 public nodes/OS/APK hash match.
33 behavioral observation assertions and113 capture hashes pass; these are not new unit tests.
Retain navigation/publication/null-tree and host-verifier failures; repair observation against
stored originals without repeating accepted saves or cold launch. Code/APK remain D-450;
no successful old suite/build/emulator scene is replayed. Protected variants, general stability,
spoken accessibility, other physical workflows, iOS/provider/store/release gates remain open.
See [physical goal-kind evidence](quality/physical-goal-variants.md).

### D-460 - Original physical zero, exact and exceeded budgets

On the owner-reserved Samsung, one new original profile opens with900EUR and no budget.
Explicit overall/category zero limits show the no-spending hint. One Food expense10 exceeds
both zero limits by10. Editing the same budget to overall10/Food5 gives100% overall and5
category excess. One linked refund4 initially has Uncategorized after an observed mistaken
category toggle: overall6/Food10 is correct for that source. Correct only that refund's category;
overall/category net6, overall left4/60%, Food excess1 and cash894 then agree. One cold read
retains the complete packet. All-empty limits are refused; same-draft category-only5 succeeds
without an overall line. Cancel then accept original budget deletion; it preserves both10/4
ledger rows and894 cash. Remove only the new profile through both confirmations; original
three profiles/Main979/all141 public nodes/OS/APK hash match.35 behavioral assertions and135
capture hashes validate original observations, not new managed tests. Retain native transition,
invalid host-input and observation-verifier failures; do not replay accepted financial creates.
Program/APK remain D-450; no successful old test/build/emulator matrix is repeated. Broader
period/method/rollover, physical workflow, stability/accessibility/iOS/provider/release gates remain.
See [physical budget boundary evidence](quality/physical-budget-boundaries.md).

### D-461 - Original budget action lifetime and committed outcomes

Six original SQLite regressions reproduce retained Edit/Delete/Copy commands acting on a newly
published period and misreported accepted writes after Changed throws. Commands now retain their
original publication revision; native disappearance retires commands/consent and hidden Retry.
Pending reads cannot revive a retired page. Copy/replace/delete recheck current ownership in the
actual writer before commit; acceptance is recorded before notification, so explicit Retry reads
without repeating the write.15 new cases and26 existing action cases are verified distinctly. Four cases repeat after the hidden
Retry guard; two later callback/read regressions fail first, then only seven affected cases repeat and pass. Strict Windows and final Offline Android
builds have no warnings/errors. Retain failed native Windows attempts, including the first terminal
UI crash and later target-readiness failures. Three native metadata actions passed their financial
checks before the later observer failure; never replay them. The same source supplies six final
read-only en/fa/de Light/Dark Windows captures with original base/nonbudget rows unchanged.
Complete signed Offline APK has384 AOT libraries; Samsung upgrades without reset and original
copy-next/Edit dirty Discard/delete-cancel/delete/copy-previous/tab/period reads pass with cash900
and empty All-history. Remove only the new profile; Main979/three profiles/all141 public nodes/OS
and intermediate candidate hash match (26 assertions/78 captures). After the last callback/read
guards, upgrade the final signed candidate without reset; existing D316 Edit/Cancel preserves
its budget50/Home990 without any write. Return Main979/three profiles/all141 nodes/exact OS
and final APK hash in the same PID12903 (9 assertions/34 additional captures). These are
original observations, not unit cases; accepted financial/native writes are not replayed.
No schema/provider/permission or financial formula changes. General stability/accessibility,
complete budget financial snapshots, other platform/workflow and release gates remain open.
See [budget action lifetime evidence](quality/budget-action-lifetime.md).

### D-462 - Coherent original budget financial publication

The original Budget page now captures its complete review, accounts/categories/preferences, ledger,
plans/states and goal earmarks on one original-file lease and one deferred SQLite transaction.
SQLite materialization and the unchanged financial calculators run on workers. Complete rollover
history, envelope cash to today, refund netting, confirmed-only display, short-period limits and
monthly flex/planned arithmetic retain their original meaning. The covered page publishes the
complete calculated rows synchronously after original review validation; hidden/error/Retry state
never exposes a partial financial packet. No entity/schema, permission, export or provider changes.
Fourteen new actual-source SQLite/financial-worker cases and seven affected prior command cases
pass selectively; no complete suite or accepted financial commands repeat. Eighteen independent
Windows method/language/theme observations retain all fictional source and base rows; own-window
images and exact separate desktop restoration are verified. Android retains10 completed contexts
across a real ANR/timeout and runs only the8 missing contexts on its original source, yielding18
complete contexts; the exact original public report is recovered without replay or invented fields.
Final strict builds and a complete signed Offline APK with384 AOT libraries pass. Samsung upgrades
without reset and reads its retained50/10/40/planned30 budget, then returns Main18990/979/141 nodes,
three profiles/exact OS/final APK hash; no financial writes or profile creates/deletes occur.
The final order correction repeats only its one failing source case and final-source read/builds.
ANR remains open. Complete scope, artifact and retained-failure evidence is recorded in
[budget financial read evidence](quality/budget-financial-read.md). General ANR/crash,
speed/low-memory, full accessibility, other original budget workflows, iOS/provider/Store/release
acceptance remains separate and open.

### D-463 - Original budget periods and physical rollover

The actual Samsung weekly/two-week workflow exposed shared captions that still described months.
Use period-neutral editor/limit/empty/copy/delete/planned/rollover wording in all six languages,
show the exact original financial date range in the growing editor header, and omit monthly
calendar annotations for short periods. Keep financial rules, raw draft money and all stored
amounts unchanged; monthly methods and genuinely monthly navigation retain their own wording.
Four new actual-source date-range cases (calendar month, shifted financial month, week, fortnight)
and six affected live-caption cases pass; six resource contracts pass. No complete suite repeats.
Eighteen independent Windows caption contexts produce54 own-window images at360/412/1280 widths;
six Release/AOT Android contexts qualify original captions/date headers without financial saves.
The first Windows observer incorrectly required44px from the existing40px MoveButton; retain its
failure and first image, continue the original source, and qualify only the missing observations.
Samsung uses one fictitious profile, opening900 and one Food expense10, with ten budget metadata
writes: monthly positive carry90/30, Start fresh50/20, surplus-only50/20 and deficit carry45/15;
weekly next30 with carry10 and fortnight next50 with carry20. Final signed Offline APK upgrades
without reset; one cold source read retains Home890, one ledger expense and all three final
period results. Edit/Cancel and delete-confirmation Cancel verify corrected native text without
replaying accepted writes. Delete only the new profile with both native confirmations and retain
Main979/all141 public Home nodes/the original three profiles/exact OS in final PID26903.
Strict Windows/Android builds pass without warnings; complete APK has384 AOT libraries and local
v2/v3 signature. These observations are not additional unit tests. General ANR/crash, speed,
low-memory, full accessibility/TalkBack, iOS/provider/Store and release acceptance remain open.

Evidence: [original budget period and rollover qualification](quality/budget-period-rollover.md).

### D-464 - Original physical envelope and Flex budgets

Continue QA-01 on owner-reserved Samsung with the unchanged signed Offline D-463 APK. One new
fictitious profile opens900, posts one Food expense10, earmarks25 for a goal and saves two unpaid
plans: monthly Housing30 and yearly Insurance120, both reminder/autopost off. Original envelope
budget200/Food100 shows cash890, goal25, unspent90 and unassigned775. Correct only Food to5:
overspent5 assigns nothing extra, so unassigned865; spent cash is not deducted twice. Change the
same budget to Flex50: variable10/50/40, fixed0 paid/30 expected, nonmonthly share10/0 paid, month
plan90. Actual due expenses150/two items remain separate from the comparison-only monthly share10.
One original cold start retains Home890/Flex40 left, goal25, both unpaid plans and exactly one
ledger expense. Delete only the new profile through both native confirmations; Main979/all141
public nodes/original three profiles/exact OS/unchanged APK match in final PID3591. Twenty-eight
assertions validate retained observations, not new managed tests. No code/APK change, rebuild,
old suite, accepted financial replay or emulator matrix is needed. General stability, complete
device/workflow acceptance, scaling/TalkBack, iOS/providers/Store and release gates remain open.

Evidence: [original physical method qualification](quality/physical-budget-methods.md).

### D-465 - Original budget account and currency boundaries

The original Simple editor now explains its retained account restriction, including archived or
missing references and dirty scope, without changing raw amounts or financial writes. Six new
actual-source SQLite cases and six resource contracts pass; eighteen Windows contexts/images and
six independent Release/AOT Android contexts qualify only the changed note. Old financial/period
matrices and accepted commands are reused. Strict Windows/full signed Offline Android builds have
zero warnings/errors. One retained Samsung profile verifies EUR30 across both accounts, secondary20,
primary10, independent USD30/50 and secondary EUR0 after MonthStartDay15; Persian display and USD
valuation preserve original budget currencies/calendars. Final cold PID19994 retains the corrected
note, EUR0/100, USD30/50/20 and balances890/80EUR and670USD. Delete only the new profile with both
confirmations; Main979/all141 public nodes/original three profiles/exact OS/final APK match.
Twenty-three observation assertions are not unit tests. General stability, full device/workflow,
spoken accessibility, iOS/provider/Store and release acceptance remain open.

Evidence: [original account/currency/pay-cycle qualification](quality/budget-account-scope.md).

### D-466 - Original physical incomplete forecast and saved scenario

Reuse the unchanged signed Offline D-465 APK on owner-reserved Samsung. One new fictitious profile
opens100EUR and saves three Once plans: expense150 on23 October, unknown expense on24 October,
income200 on30 October, all reminder/autopost off. Original end150/minimum-50 on23 and one unknown
become end130/minimum-70 on24 only after explicit temporary assumption20. Save one original named
scenario; its accepted Save disables, original detail retains the assumption and correctly defers
future actual comparison until tomorrow. Reset restores150/-50/unknown without changing the saved
scenario. One actual cold launch retains current original forecast and independent saved-70 path.
Cancel then accept native snapshot deletion; original three plans, cash100 and empty All-history
persist. Delete only the new profile with both confirmations; Main979/all141 public nodes/original
three profiles/exact OS/unchanged APK match in PID27525.33 assertions over retained observations
are not unit tests. No code/APK/build/suite or accepted-action replay. Later-date comparison,
missing-rate/other scenario variants, full device/stability/spoken/iOS/provider/Store/release gates
remain open.

Evidence: [original physical forecast qualification](quality/physical-forecast-snapshot.md).

### D-467 - Original purchase currencies and valid amount examples

Original Samsung purchases expose an invalid two-decimal error example for JPY. Format only the
original-purchase error from its actual ISO currency and retained numeric culture, independently
of display units. Seven new distinct actual-source cases and selected existing/resource contracts
pass across retained runs.54 Windows error contexts use53 retained valid images plus one bounded
visual recapture;18 independent Release/AOT Android contexts change no financial rows. Preserve
review-timeout, fixture, image-index and native-input failures instead of replaying successful work.
Strict final Windows/complete signed Offline Android builds have zero warnings/errors. Upgrade
without reset; actual COLD PID15323 retains the three original EUR10/20/30 expenses with12.34USD,
123JPY and1.234KWD metadata, Home940, and the corrected JPY rejection/Cancel. Delete only the new
profile with both confirmations; Main979/all141 public nodes/original three profiles/exact OS and
final APK match.23 observation assertions are not unit cases. No accepted financial Save repeats.
Full financial/device/stability/spoken-accessibility/iOS/provider/Store/release gates remain open.

Evidence: [original purchase currency qualification](quality/physical-original-purchase-amounts.md).

### D-468 - Serviced native UI framework and original lifecycle qualification

Adopt stable MAUI Controls/Essentials 10.0.120; eight existing modules advance with no new SDK or
permission. Official signatures/licences pass; Preferences/FileSystem sources, ten runtime/cipher
engines and 25 branding assets remain unchanged. Strict Windows/full signed Offline Android
builds have zero warnings/errors. Eighteen independent Release/AOT navigation cycles preserve
complete original rows and one native root/subscription per cycle. Eighteen Windows contexts
produce 36 inspected images with unchanged ledger entries. Correct the observed null expected
count in the report driver and validate its same completed run without native replay.
Samsung and emulator upgrade without reset. Six physical language/theme contexts cancel six
expense drafts and six plan drafts, including six dirty-discard confirmations, with no financial
saves. Delete only the new profile through both confirmations. Phone PID21905 retains Main979,
141 public nodes, three original profiles and exact OS; emulator PID26264 retains Home0/139 nodes
and exact OS. Twenty-seven observation assertions are not new managed tests. Finite event buffers
record each start and no ANR/crash; general stability is not accepted. The ScrollToAsync retry-loop
fix is absent and Mono/.NET10.0.13 unchanged. General ANR/GC/crash, low-memory, complete/spoken
accessibility, two-second Home, iOS/provider/Store and release gates remain open.

Evidence: [bounded native servicing qualification](quality/maui-servicing-qualification.md).

### D-469 - Original physical cached-process Main recovery

Use the unchanged signed Offline D-468 APK on owner-reserved Samsung. A phase-free existing owned
Activity supplies the missing cached-process precondition without running a review route. Preserve
the initial adjustment700/cached=false refusal; require actual cached=true/state16/adjustment900
for exact Main21905 before one package-scoped kill. Confirm death before retained-task recovery
as9423/COLD/Activity1392ms. Preserve the initial cover and later equal141-node Home979 in the same
PID, five known original public history rows and three original profiles. Restore This month and
Home, exact OS/both APK hashes; stop only the quiet owned process and remove17 hash-matched XMLs.
No financial command, new profile, app change, build, managed case or successful old suite replay.
Finite native events contain the intentional kill/new start and no recorded ANR/crash; natural
low-memory, private full-data/key equality, general stability, whole-Home speed, spoken/platform/
provider/Store and release acceptance remain open.

Evidence: [physical cached-process recovery](quality/physical-cached-main-recovery.md).

### D-470 - Serviced original loaded Home and exact recent-row oracle

Remove only the accidental U+00C2 before the original U+00B7 separator in the independent recent
subtitle assertion; exact production-source extraction proves the old ordinal predicate rejects
the valid original packet. Production source/APK remain D-468. Strict signed Offline x64/ARM64
callers qualify the unchanged original protected10,000/100,000-row corpora after MAUI10.0.120.
Twelve native publications per target complete in one process each, with full schema/typed-row
equality, original balance/recent packets and no financial Save or old raw-reader/suite replay.
Emulator medians1040.70/2680.07ms; Samsung1739.94/4933.12ms. Largest dispatcher gaps487.90/405.60ms
and794.66/1094.35ms respectively. One reference phone sample is2011.86ms; two-second acceptance
remains open. Fresh current presentation baselines are not historical framework speedup proof.
Restore original independent APKs/stopped state; same emulator26264/Home139 and Samsung9423/
Home141/979, shipping APK and OS match. Four hash-matched XMLs removed. Native event scopes have
starts and no recorded ANR/crash; no new managed cases, general stability or cold/low-memory/
spoken/iOS/provider/Store/release acceptance. Next actual ready defect: two corrupted separators
in original household-cost captions, kept outside this frozen-source measurement.

Evidence: [serviced original loaded Home](quality/serviced-loaded-home.md).

### D-471 - Original household caption separators

Remove two corrupted separator prefixes from the existing refund-source and destination-value
captions without changing money, parsing or authority.36 final inspected Windows images reuse
36 successful original popup receipts;12 Release/AOT Android selected/popup checks preserve real
protected source/base rows and membership, with no financial Save. Destination-only packets are
explicitly transient formatter/control evidence, not stored cash-linkage acceptance. Preserve
first incomplete Windows captures and Android's terminal observer failure; continue only missing
visibility/readiness checks. Strict builds are warning-free. Complete signed Offline APK384 AOT
upgrades without reset; emulator28047/Home139 and Samsung17769/Home141/979 retain exact public UI
and OS. Restore the original independent APK/stopped state. No new managed cases or unrelated
successful-suite replay. Mac unavailable per owner22 October; independent Android work continues.
General stability, full financial/device/accessibility, iOS/provider/Store/release gates stay open.

Evidence: [bounded original caption qualification](quality/household-caption-separators.md).

### D-472 - Original physical account reconciliation

Close the missing Samsung AT-11 boundary on unchanged signed Offline D-471. One new fictitious
profile/account starts100EUR. Native112.34 comparison, change to212.34 invalidation, required
reason and Cancel preserve the draft. One accepted adjustment+112.34 gives212.34 and reconciled
10/22/2026; original row/reason/account/date and metadata persist after one real COLD17769-to21187
restart. Activity1457ms is not full Home. No accepted command is replayed. Delete only the new
profile with both original confirmations; restore Main979/all141 public Home tuples, five known
public history rows, three profiles, period and exact OS/APK hashes.32 observation assertions are
not new managed tests;106 hash-matched XMLs removed. App/APK remain D-471; no build or old suite
replay. Retain observer/viewport/modal-ownership refusals and same-process continuations. General
stability, full financial/device/accessibility, iOS/provider/Store/release gates remain open.

Evidence: [original physical reconciliation](quality/physical-account-reconciliation.md).

### D-473 - Original physical receipt Save and stored reread

Close the missing Samsung receipt Save/attachment/cold-reread boundary on unchanged D-471.
One new profile/account opens100EUR on16 October; retained fictitious17 October PDF gives24.50.
One original Save yields Food expense24.50,Home75.50 and one13KB PDF. Hash-delete only the original
input before one COLD21187-to26415 restart. Stored Scan receipt reads the original saved attachment
and opens Edit transaction with the same total; Cancel without Save preserves every public detail
tuple,one expense and75.50. Delete only the new profile with both questions; restore Main979/all141
public nodes,three original profiles,exact OS/APK.27 observation assertions are not new unit tests;
78 hash-matched XMLs removed. No new app code, APK, build or successful-suite replay. Trial watermark/
merchant,real corpus,camera,image OCR,all variants,stability/accessibility/iOS/provider/Store/release
acceptance remain explicitly outside this bounded result.

Evidence: [physical original receipt lifecycle](quality/physical-receipt-save-reread.md).

### D-474 - Original physical cold notification action

Close the missing Samsung ARM64 D-333 boundary with a complete signed Offline independent current-
source package and real permission/notification action. One fictitious account/Once plan is
prepared in29556; actual death precedes one In1hour tap. Native broadcast receiver starts30999
without Activity; completed status has protected storage,windows0,one saved/native pending snooze
and equal intentionally populated financial fields/zero ledger. A separate provider31245 preserves
one snooze without storage preparation. Preserve both early in-flight reads and observer refusals;
no accepted action/seed or old successful suite repeats. Independent strict build is warning-free.
Remove only the new fixture package; Main26415/Home979/all141 public tuples,OS and original APKs
remain unchanged.19 observation assertions are not new managed tests. General stability,OS timed
notification restrictions,interrupted-action/low-memory,full device/accessibility/iOS/provider/
Store/release gates remain open. Main code/APK stay D-471.

Evidence: [physical original cold action](quality/physical-cold-reminder-action.md).

### D-475 - Original physical timed alarm and notification route

Add scheduled-only mode to the existing independent caller; original application code is unchanged.
After native permission, actual Schedule time/day offset supplies a real future AlarmManager
request, with no immediate Show copy. One original alarm/PendingIntent survives preparationPID1388
death and read-only waiting. Native ScheduledAlarmReceiver starts3318 and posts original184973019
155216ms after requested time under the existing inexact/tolerance policy. No clock change/rearm.
Actual content tap opens the original upcoming12.34EUR/23October due page in the same process;
Back posts nothing. Populated fixture fields and zero ledger match; Home1000 remains. Remove only
the new package; Main26415/Home979/all141 public tuples,OS/APKs and charging-awake15 remain unchanged.
Independent strict signed Offline ARM64 build passes;26 observation assertions are not new managed
cases. Old successful Snooze/financial/Windows suites are not replayed. This awake/charging scope
is not exact timing,Doze,reboot,full lock/device/stability/accessibility/iOS/provider/Store/release
acceptance. Shipping Main/APK stay D-471.

Evidence: [physical original scheduled reminder](quality/physical-scheduled-reminder.md).

### D-476 - Exact native-review build boundaries

Repair the two current causes confirmed in actual D-471/D-474 hosted failures: three independent
fixtures now share their dependency's Windows Debug/independent-build gate, and their exact paths
join the source-policy list only after full outer-gate validation. The affected strict Android
Debug billing build and eight distinct existing boundary cases pass; no new tests or shipping
APK/visible behavior changes. Keep successful Release/native/financial evidence without replay.
Hosted completion remains pending at commit creation; external/stability/accessibility gates stand.

Evidence: [original hosted failures and focused recovery](quality/ci-common-recovery.md).

### D-477 - Hosted Windows installer metadata

Actual D-473 Windows logs expose a separate new hosted installer18.11.12315.225. Match the dated
official image inventory and add only that exact metadata version; preserve executable hashes,
Microsoft signature, pinned MSVC/SDK and immutable package checks. Eighteen focused qualification
cases pass against the existing signed local compiler. The new hosted compiler/package must still
pass on the exact pushed SHA; no app build/native scene replay or shipping APK change.

Evidence: [actual installer refusal and retained qualification](quality/ci-common-recovery.md).

### D-478 - Immutable native producers for serviced hosted Windows

D-476's actual hosted Android build and complete5097-case run now pass with25 skips/zero failures.
The remaining Windows failure is a different deterministic native output under the old candidate.8
identity, despite the same approved compiler. Preserve that refusal and old package identities;
qualify a separate candidate.9, compare every original archive entry, and select the explicit
hosted producer before cache/build use. Twenty-six local script cases,three actual central-version
evaluations and the real local-host/environment boundary checks pass; no new managed cases or
Android shipping APK change. Ordinary consumer and final exact-head CI remain distinct gates.

Evidence: [original drift, producer and consumer qualification](quality/ci-common-recovery.md).

D-478 producer follow-up: run39857667608 on9823a734 passes the original producer gates; all five
actual candidate.9 archive entries match the distinct inventory. Normal exact-head main consumer
CI remains pending at adoption commit creation; this is not platform/product/release acceptance.

### D-479 - Bounded physical original-widget continuation

QA-07 gains actual Samsung provider/RemoteViews/PendingIntent evidence through an independent
native host and real single-widget consent, without changing the owner's launcher. Four original
actions, three Cancel returns and actual cold Expense origin26415-to16655 pass; no financial Save.
Original141 public Home tuples/979EUR,OS and shipping D-471 APK match. Remove owned widget2/host47901,
confirm owned0, uninstall only the new helper and remove25 exact hash-matched XMLs. Strict signed
ARM64 helper build and two identity/configuration refusals pass; no new managed tests, Main build
or old financial/emulator cohort replay. Actual launcher placement, all widget/lock variants,
general stability, OS200%/spoken, other devices/API24/16KiB, iOS and release remain open.

D-478 final main109743f5: all five workflows completed successfully;5097 total,5072 succeeded,
25 skipped,zero failed, with actual hosted Windows immutable-package/cold persistence/workloads
and Android/Windows consumer builds passing. iOS is intentionally skipped, not accepted. Retain
historical failures and pending-at-commit records; do not repeat the consumed CI check.

Evidence: [physical widget scope](quality/physical-widget-pending-intents.md).

### D-480 - Original four-form Android validation listener lifetime

Actual native observer retirement reproduces three callbacks after the old event-wrapper cleanup.
All four original transaction/plan/occurrence/settlement waits now use the same explicit native
listener, current-live-tree removal and neutralized callback, preserving the existing one-second
bound and form/version guards. Three fixed native checks pass: zero retired callbacks, attached
completion and bounded unattached timeout. Strict Windows/Offline Main/helper builds and helper
identity guards pass. Complete signed384-AOT APK upgrades without reset; original invalid Expense
Save reveals its field/error, then Cancel/Discard preserves979EUR/all141 public Home tuples and OS.
Zero accepted financial Saves; no new managed cases or successful unrelated-suite replay. Remove
only the independent fixture and20 exact hash-matched XMLs. Preserve the real helper JNI failure
and IME/input observer failures. This is not a general ANR/GC/crash fix, full form matrix, speed,
spoken/device/iOS/provider/Store or release acceptance. Exact-head hosted CI follows the push.

Evidence: [native lifetime and final package](quality/android-validation-layout-lifetime.md).

### D-481 - Remaining native validation callers

Original plan/occurrence/settlement invalid actions pass on the retained independent x64 editor
source, with unchanged named source/base groups and zero accepted financial Saves. Keep the first
two receipts while correcting settlement's stale diagnostic constructor/public loader. Recover
all3 complete receipts with NativeActionsThisPass0; no successful case, old matrix, financial seed,
managed/resource/business suite or unchanged Main/Windows build is repeated. Strict complete
signed Offline192-AOT caller build passes. Restore exact old independent APK and stopped state,
Main28047/all139 public nodes/OS/APK; remove only2 hash-matched remote XMLs. Samsung is untouched.
No new managed cases or shipping behavior. Actual shipping head0e8c6fa6 has successful CI/CodeQL;
its one-shot monitor is consumed/deleted. General ANR/GC/crash, whole-Home speed, full language/
OS-scale/spoken/device/iOS/provider/Store and release acceptance remain open.

Evidence: [native callers and exact preservation](quality/validation-layout-callers.md).

### D-482 - Detached original validation listeners

The actual native baseline reproduces a callback into a disposed peer after detachment changes
observers while the original root remains alive. Track distinct live trees, retire exact callbacks
before disposal and return false on target detachment, preserving the one-second bound/caller guards.
Five new native cases (one retained plus four remaining) and three affected normal regressions pass;
no managed test-count increment or replay of accepted financial/caller cohorts. Strict Windows and
complete signed Offline384-AOT Main pass without warnings. Samsung no-reset upgrade and blank-amount
rejection/Cancel preserve Main27601/141 public Home nodes/balance979/OS; emulator Main28047/139nodes,
APK/OS and source equality remain. Both owned native helpers and2 emulator/8 phone XMLs are removed.
The reproduced fault is fixed; general ANR/crash/GC, speed, natural low-memory, full accessibility,
iOS/provider/Store and release acceptance remain open. Exact pushed-head CI remains a separate gate.

See the detailed [D-482 evidence](quality/detached-validation-listeners.md).

### D-483 - Physical key-protection cohort

The permission-free independent ARM64 Release fixture links the actual Android key protector and
binding. One retained run passes creation, real cold reopen,12 wrong-binding/tamper/purpose/length
refusals and original retirement with2 denied authorities/2 aliases absent and no lost-key creation.
Both keys report OS security level1 (Trusted Environment); no Main alias inspection or hardware
attestation is claimed. Strict native build/v3 package pass. Helper and2 hash-matched XMLs are
removed; Main27601/all141 public Home tuples/979/APK/OS stay equal. No financial command, managed
suite count or successful unrelated Main/Windows build is repeated. Shipping source/APK stay D-482.
Authentication, full SEC/platform/OEM/power-loss/API24/16KiB/iOS and release gates remain open.

Evidence: [bounded physical key evidence](quality/physical-key-protection.md).

### D-484 - Physical original plan/debt form continuation

One fresh Samsung profile qualifies original plan12.34, native zero rejection/correction22.34,
Keep editing/Discard99.99 and separate debt50/estimate5.25. The original repayment draft is cancelled
without Save; notification permission/flags remain unchanged. One actual cold27601-to27860 preserves
cash100 (new debt excluded by default), one22.34 plan, debt-50 and empty All-history. Restore Main,
delete only the new inactive profile with both confirmations, compare all original three-profile/
141 Home tuples/979/APK/OS and hash-remove144 XMLs.144 is an artifact count, not tests. Preserve
observer refusals; never replay accepted metadata or ledger actions to repair observation. Shipping
source/APK stay D-482; successful suites/builds are not repeated. Latest6c52cd67 CI/CodeQL succeed.
This is bounded physical English/Light form evidence, not the full currency/unit/race/notification/
security/stability/accessibility/iOS/provider/Store/release gate. The broad delivery goal stays open.

Evidence: [physical original forms](quality/physical-plan-and-debt-forms.md).

### D-485 - Follow the original Android toolbar between native roots

The actual native caller reproduces outgoing-root callbacks and missing callbacks after a toolbar
moves to a second window. Its separate weak-owner production observer now removes the exact old
registration at detachment and registers on the new root; the initial floating-tree merge is not
duplicated. Two focused native cases pass, for registration before/after attachment, with both
roots alive and zero detached/retired callbacks. Keep prior financial/title/scale/managed suites;
these are not new managed tests or general ANR/GC/performance/accessibility acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass; no-reset Samsung upgrade and
original Accounts title/Back preserve all141 public Home tuples/979EUR and OS. Remove only the
new helper and two emulator/twelve phone hash-matched XMLs; emulator Main28047 stays unchanged.

Evidence: [original toolbar transition qualification](quality/toolbar-observer-transitions.md).

### D-486 - Incomplete original foreign-transfer forecasts

Reproduce four false one-to-one destination results with eight new Core cases. Missing original
cross-currency destination amounts now keep null monetary effects; known source/same-currency/
explicit destination effects remain known. Two new actual SQLite/workspace cases retain that
warning through atomic currency snapshot Save.49 selected cases pass, including ten new cases.
Six original Release/AOT en/fa/de Light/Dark pages expose EUR90/USD50/incomplete1 on one retained
legacy source with all typed rows unchanged. Strict builds and the complete signed Offline384-AOT
APK pass; Samsung no-reset upgrade and read-only original forecast preserve Home979/all141 public
tuples/OS. Restore the exact old independent APK/stopped state and Main28047/all139 tuples.
No source financial rewrite, schema, resource, permission or unrelated successful cohort replay.
Priorb3fa1a0d CI/CodeQL succeed and its one-shot schedule is consumed/deleted. New-head CI and full
physical/platform/security/stability/accessibility/release acceptance remain separate.

Evidence: [original missing-destination conversion](quality/forecast-missing-destination.md).

### D-487 - Original saved forecast difference categories

Correct the reproduced cash-sign classification across all eight original ledger kinds. Refunds
reduce unplanned spending, income reversals reduce unplanned income, and transfers/capital trades/
corrections use a separate neutral account-movement row. The residual caption now says other
differences because earlier data edits as well as changed plans can contribute. Original scope,
saved paths, actual daily balances, recorded-later boundaries and source rows remain unchanged.
Fourteen new distinct Core cases and two affected regressions pass;23 resource cases pass,39
selected successes overall. Eighteen Windows contexts yield36 inspected images; six x64/412dp
and six physical ARM64/384dp Release/AOT contexts qualify156 original captions and reversal color
with full typed fixture/base equality. Historical timestamps are seeded once per independent
platform; this is not real elapsed-day comparison or financial form/Save acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass. Samsung no-reset upgrade
8764-to13827 retains Home979/all141 public tuples/OS; emulator28047/all139 tuples stays unchanged.
Restore both exact independent APKs/stopped states; remove five hash-matched owned XMLs. Prior
e310eadb has four successful workflows and its single-use schedule is consumed/deleted. Final
new-head CI, real elapsed-day/remaining physical variants, security/stability/accessibility/iOS/
provider/Store and release gates remain open. No unrelated successful cohort is replayed.

Evidence: [original saved comparison qualification](quality/snapshot-difference-kinds.md).

### D-488 - Physical original goal-reminder queue lifecycle

Qualify eleven original ReminderService/native scheduler states on one fresh independent Samsung
installation: default off, enable, pause/resume, complete/archive, derived reached/unreached,
detail consent, disable and generic restoration. Pending counts are0/1/0/1/0/0/0/1/1/0/1; every
queued request keeps the original scoped goal/date/stable id and24October2026 local09:00. Account
opening100EUR and contribution25/Once date remain unchanged, with zero entries/allocations/plans.
The reached case explicitly changes only the fictitious target150-to100 and restores150; no
money moves. Actual native AlarmManager/PendingIntent uses ScheduledAlarmReceiver with the
existing inexact one-hour window, not an immediate display. Eleven observations are not new
managed tests or financial-form/timed-delivery/lock acceptance. Strict complete signed Offline
ARM64 caller192 AOT passes; Main identity is refused. Retain the original live15501 after the
first observation timeout; no successful state/seed repeats. Remove only this new fixture and
its pending alarm/notification; Main13827/Home979/all141 public tuples/APK/OS match. Five owned
XMLs are hash-checked/removed. Main shipping code/APK remain D-487. Real09:00 delivery, doze/reboot,
notification tap through lock, remaining calendar/device/security/stability/accessibility/iOS/
provider/Store and release gates remain open.

Evidence: [physical goal queue qualification](quality/physical-goal-reminder-queue.md).

### D-489 - Physical original period-review reminder queues

Qualify22 original protected ReminderService/native queue states on Samsung in one retained20233:
empty/off and empty/enabled sources, source/off, six states in each Gregorian/Persian/Hijri
calendar, and final restored/off. Financial month starts24/2/13 schedule24October2026 local09:00;
the next two native requests follow their actual calendar, retaining09:00 across the stored DST
offset change. Partial review keeps three pending; finishing only the due period removes that
one and retains two future requests. Details, disable and missed-time cases pass. Compare every
settings field before/after publication; no review step is completed by a reminder. Original
account100EUR stays equal, with zero entries/goals/allocations/plans. These22 observations are
not new managed cases, actual timed delivery or financial-form acceptance. Strict signed Offline
ARM64 caller192 AOT/unchanged five runtime engines passes. Preserve initial caller compile error,
two live observation deadlines and premature cleanup refusal; continue only the same live PID.
The temporary owner-authorized cross-project phone handoff is respected; after a short exclusive
turn, remove only the fresh fixture and five hash-matched XMLs, retain Main13827/Home979/all141
public tuples/APK/OS, and explicitly release the phone for the other active project. Main shipping
code/APK remain D-487. Real09:00 delivery, lock/tap/profile-switch/restore/Doze/reboot, broader
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [physical period queue qualification](quality/physical-period-reminder-queue.md).

### D-490 - Preserve original imported-group dependencies on Undo

Reproduce unsafe Undo after a later fee is added to an imported transfer. Capture external ledger
and holding group context under the original import writer in internal ImportLinks version2;
refuse later changes before mutation, retaining valid Undo for unchanged pre-existing groups.
Ungrouped journals remain version1; legacy closed groups are supported and unverifiable external
groups require review without invented backfill. No EF model/schema/public CSV/permission change.
Ten new and five selected prior managed cases pass; strict Windows/complete signed Offline384-AOT
APK builds have zero warnings/errors. Six original native Undo cases pass on isolated fictional
Samsung sources with full typed-row/base equality. Preserve the first fixed-dock ScrollToAsync
observer failure and its accepted first outcome; resume only five unexecuted commands and read
that first result without replay. Consent is fictional, not native dialog/picker acceptance.
Restore the old independent APK/stopped state, then no-reset Main upgrade retains979/all141 public
Home tuples/OS in PID31885; remove three owned hash-matched XMLs. Prior ecde9366 CI/CodeQL succeed
once and its monitor is consumed/deleted. New-head CI and broader relationship/device/security/
stability/accessibility/iOS/provider/Store/release gates remain open. No unchanged successful
financial cohort or full suite is repeated.

Evidence: [original grouped Undo](quality/import-group-undo-dependencies.md).

### D-491 - Physical original goal calendars and unavailable sources

Twelve original protected/native queue observations qualify saved Gregorian/Persian/Hijri monthly
rules independently of display calendar, two-goal grouped routes, account/type archive/restore,
missed Once and final disabled state. First24October09:00 and second24November Gregorian /
23November Persian/Hijri stay local09:00 across DST offsets. Full original publication packets
remain equal; opening100/targets150EUR and5units stay, with zero ledger/allocations/holding events.
Preserve two actual coalesced-wait observer cancellations and two accepted states; final caller
follows the replaced handle after yielding and continues only ten missing stages in4354.
Strict complete signed Offline ARM64/192-AOT caller passes. Remove only the new fixture with
pending0 and2 Main/3 scoped owned XMLs; original Main31885/Home979/all141 public tuples/APK/OS
remain. Main code/APK stay D-490, no new managed count or successful unrelated suite/build replay.
Real timed/lock/profile/Doze/reboot, security/stability/accessibility/iOS/provider/Store and release
gates remain open. This is native queue/API evidence, not financial-form or whole-device acceptance.

Evidence: [physical calendar/source queues](quality/physical-goal-calendar-sources.md).

### D-492 - Physical original goal and current-review notification taps

Two actual native notification content taps start8272/9387 after confirmed cached deaths of7065/
8272, without provider-created cold processes. Original protected startup/router open the exact
goal100/150/50 and current September review0of5; complete fictional source remains equal and no
financial/contribution/review-step command occurs. Explicit immediate copies retain original
future request ids/payloads; they are not actual09:00 dispatch or credential-configured acceptance.
One strict complete signed Offline ARM64/192-AOT caller passes; Main/App/APK remain D-490.
Preserve early cache/shade/host guards and the cleanup observer's historical-alarm misclassification;
continue accepted state without repeating seed/Show/tap/uninstall. Active pending/blocked alarms
and posted notifications are absent; historical records remain untouched. Remove only the fresh
fixture and2 Main/10 scoped owned XMLs. Original Main31885/Home979/all141 public tuples/APK/OS
stay equal. Prior f21a5d61 CI/CodeQL succeed once and its monitor is consumed/deleted. No new
managed count or unchanged successful suite/build replay. Timed/lock/profile/grouped/Doze/reboot,
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [original content routes](quality/physical-goal-period-notification-routes.md).

### D-493 - Physical grouped goal tap and restored-profile scope

Two native content taps follow confirmed cached deaths12676-to14154-to15038. Original SDK events
retain the same two goal ids/dates and scopeA. The first opens both original GoalsPage rows.
A single original portable export/profile creation/switch/protected restore clones all financial
identities intoB with another file scope; the second actual old-A tap stays onB Home100 despite
both goals existing. Nine original financial arrays compare exactly. Original profile return/
inactive-B retirement restores scopeA/one-profile catalogue. Service restore/cleanup is distinct
from native dialogs/password/credential acceptance. An independent early tap subscription adds no
Application-time database/key work; production routing/security sources remain unchanged.
Final strict signed Offline ARM64/192-AOT caller passes after a preserved initial report-type
compile error. Remove only the fresh fixture and2 Main/8 scoped owned XMLs; pending/posted native
work is absent, historical records untouched, permanent Main31885/Home979/all141 public tuples/
APK/OS equal. Prior7abae129 CI/CodeQL succeed once and monitor is consumed/deleted. Main/APK stay
D-490; no new managed count or successful unrelated build/suite replay. Actual09:00/credentials/
remaining profile-race/security/stability/accessibility/iOS/provider/Store/release gates stay open.

Evidence: [original restored-file scope](quality/physical-goal-profile-notification-scope.md).

### D-495 - Retain pending destinations after a renewed app lock

Two new original-source cases reproduce a queued destination running beneath a renewed cover,
with an available or missing window. One drain owner now rechecks access before each unstarted
action and keeps later input in arrival order. Three new cases and41 existing lock/router cases
pass; the full unrelated suite is not repeated. Two actual Release/AOT Android native-cover cases
with three real button clicks preserve order1/2/3 and complete source/base equality. Authentication
responses are explicit fictional ports, not real credential acceptance. Retire only the new run;
restore the old independent APK/process absence and emulator Main28047/all139 public tuples/OS.
Strict Windows and complete signed Offline384-AOT APK builds pass without warnings/errors. Samsung
upgrades without reset31885-to25043, retaining Home979/all141 public tuples and exact OS; four owned
XMLs across both devices are hash-matched/removed. No financial Save, PIN/key/account/SDK/schema or
permission change. Preserve the initial caller namespace compile refusal and actual timed-fixture
Activity resumption during Main replacement: its interrupted initial D-494 lineage is not accepted
as uninterrupted cold evidence. Same source/run remains equal; current native requests and future
actual delivery are tracked separately. D-493 CI/CodeQL succeed once; its monitor is consumed/deleted.
D-494 timed delivery, real credentials, general security/stability/accessibility/iOS/provider/Store
and release gates remain open. New-head CI follows this completed correction.

Evidence: [original relocked destinations](quality/relocked-pending-destinations.md).

### D-497 - Refresh retained original forecast and saved comparison tabs

Physical D-496 preserves stale0EUR after its single accepted later expense, versus correct-10
on original Back/reopen. AppShell now includes both retained ForecastPage and SnapshotPage in
covered tab activation; appearance/navigation share PendingPublication. Four actual native tab
clicks qualify the same saved page publishing-10 and same forecast publishing97 while retaining
its what-if plan exclusion. The immutable original snapshot and complete original base match;
only the new independent protected profile is retired. Fictitious service writes are not native
financial-form or D-496 acceptance. No new managed cases or unrelated successful-suite replay.
Preserve the initial caller compile errors and live launch-observation guard; both cases finish
in11339. Restore exact old caller APK/process absence and emulator Main28047/all139 public tuples/
OS; two owned XMLs are hash-matched/removed. Strict Windows/Offline384-AOT shipping builds pass.
Samsung remains D-495 to preserve the pending timed cohort; no-reset phone adoption remains a
named integration action. D-494/D-496, phone/platform/security/stability/accessibility/provider/
Store and release gates stay open. Prior f6ad8543 CI/CodeQL succeed once; its monitor is consumed/
deleted. Exact new-head CI follows push.

Evidence: [original retained-tab qualification](quality/forecast-retained-tab-return.md).

### D-498 - Physical original renewed-cover destinations

Two previously emulator-qualified D-495 cases now pass on Samsung SM-A176B/API36/ARM64 with
actual original LockPage/MauiAppLockHost controls and three native unlock clicks. A renewed
cover retains later destinations; both cases finish in order1/2/3. Authentication responses are
explicit fictional ports: no real device credentials, PIN storage or financial writes. Complete
fixture/base packets remain equal. One strict signed Offline192-AOT independent build succeeds;
no unchanged Windows/shipping build, managed suite or financial cohort is repeated. Restore the
exact old independent APK/stopped state and Main25043/Home979/all141 public tuples/APK/OS.
Preserve the tuple-versus-list restoration-observer refusal: both original XML arrays already
match, and only local evidence comparison continues. Two owned remote XMLs are hash-matched/
removed; local originals remain. D-494 keeps24914 and its four selected resumed alarm identities;
no provider read, rearm, subject replacement or Main upgrade. D-496 remains inactive/intact.
This is physical native-cover adapter qualification, not real authentication, notification-tap,
Doze/reboot, general security/ANR/crash, accessibility, iOS, provider/store or release acceptance.
Prior3821870a CI39981472630/CodeQL39981472583 succeed once; that monitor is consumed/deleted.

Evidence: [physical native-cover scope](quality/physical-relocked-destinations.md).

### D-499 - Reversible original CSV text

Own CSV contract3 preserves apostrophes/whitespace and named version identity; legacy ambiguous
account aliases refuse rather than selecting another account. Generic human text remains literal.
Forty new/20 existing related cases and bounded original Windows/Samsung native imports pass,
with accepted actions retained through observation-only continuation. Strict Windows/signed
Offline384-AOT shipping builds pass. Main adoption waits for D-494; no schema/permission change
or broader stability/iOS/provider/release acceptance. See [scope and evidence](quality/csv-literal-identity.md).

### D-500 - Shared original Home balance index

One worker-local index serves account rows and both balance totals without changing the financial
formula or source/authority validation. Twelve new and29 related managed cases, retained Windows
and same-day Samsung native sources pass with complete data preservation and zero financial writes.
Isolated allocation falls about two thirds; whole-Home two-second/stability targets remain open.
Main adoption waits for D-494; remaining future-day/external gates are preserved.
See [bounded evidence](quality/home-shared-balance-index.md).

### D-501 - Fixed public receipt-corpus observation

Ten fixed CC-BY-4.0 public CORD v2 images are read once on Samsung through original offline
reader/parser: five correct Found, three Review, two NotFound including the no-total negative.
No financial save, new managed count, shipping source/SDK/permission or broader quality acceptance.
QA-05 remains partial for six-language/camera/scanned/Persian/merchant/currency/device coverage.
Original caller/Main/OS restored; D-494/D-496 preserved. [Evidence](quality/public-receipt-corpus.md).

### D-503 - Original image-only PDF reading on Samsung

Nine fixed synthetic image-only PDFs/15 raster pages exercise the unchanged native PdfRenderer,
DocumentReader and ReceiptParser once: five Latin-language24.50EUR totals and a six-page last42.75EUR
pass; cross-page EUR/USD remains Review; no-total and Persian scan remain NotFound. The latter is
an unaccepted script capability, not a six-language OCR pass. Original first3/last selection skips
an intentionally conflicting middle-page total. No financial Save, new managed test, shipping source,
SDK/model/permission or product APK change. One strict signed Offline192-AOT independent build and
exact asset hashes pass; restore original caller/data/stopped state and Main25043/Home979/all141
public tuples/APK/OS. Remove two owned XMLs. Four original D-494 handles and24914 remain; no provider
read/rearm/window/subject replacement. QA-05 broader corpus/camera/Persian/iOS gates stay open.
D-501 CI40007631698 and CodeQL40007631709 succeed once; its consumed monitor is deleted.
[Evidence](quality/scanned-receipt-pdfs.md).

### D-504 - Optional Android receipt-reading capability help

The original camera/file chooser now offers optional six-language reading help. Explain the approved
Latin image/scanned-PDF capability, unsupported Persian image text, selectable Persian PDF text and
explicit amount/currency review. Ordinary photo/file selection adds no confirmation; help returns to
the chooser and Cancel opens no source. Fifteen new original-source/resource cases pass; strict
Windows and complete signed Offline384-AOT shipping builds pass without warnings/errors. Original
native chooser/interaction in a new isolated UID qualifies24 contexts: en/fa/de, Light/Dark and actual
360/384/412/680dp dialogs,72 native help/OK/Cancel clicks. Retain the first successful case after a
focus refusal, then all18 completed cases while adding only six missing exact412dp dialogs; correct
an observer resume-file mistake without replaying source reads or financial commands. All text is
fully visible/unellipsized, with actual differing native theme colours. Restore original Main25043/
Home979/all141 public tuples/APK/OS and old caller/stopped state; uninstall the new caller and remove
seven owned XMLs. A retained landscape layout is returned through only the disposable caller's
portrait window; no system setting or source-help cohort is replayed. Four D-494 handles/PID24914
and D-496 remain intact. Main remains D-495 until D-494 terminal cleanup; the latest complete ready
APK is D-504 and includes D-497/D-499/D-500. Persian OCR, OS scaling/spoken, general stability,
iOS/provider/Store/publication and future-day acceptance remain open. No new SDK/model/schema/
permission/commercial restriction. [Evidence](quality/android-receipt-source-help.md).

## D-506 - Actual elapsed-day forecast comparison (2026-10-24; completes D-496)

Continue only the original23October Samsung D496_ElapsedForecast source on the actual24October
clock, without replacing D-495 or touching D-494. Saved D496_NextDay stays23-31October with
original next-day80EUR. Before new payment, day24 saved80/recorded90/difference+10 separates
recorded-later-10 and other differences+20. Pay only the original D496_Planned20 once on24October:
recorded70/difference-10, recorded-later-10 and residual0. Add only one original D496_Extra5 expense
and one D496_Extra7 income on24October: recorded72/saved80/difference-8; parts-10/-5/+7/0 sum to-8.
Original account/date/title/amount and individual pre-command/accepted receipts are retained.

Reopen the same profile after returning Main:72 and original snapshot80/difference-8 persist.
This is profile/database reopening, not a cold-process claim. Preserve the four ledger identities,
original saved metadata and all prior successful preparation; no snapshot resave or financial
replay. D-495 requires fresh Back/reopen for retained snapshot publication; D-497 phone adoption
remains open because D-494 is unfinished. No candidate upgrade is performed.

Return Main before deleting only the inactive owned profile through both original confirmations.
Verify the exact original three profile names, Main25043/Home979/all141 public tuples/APK/OS;
remove137 new owned remote XMLs only after local/remote SHA256 equality. Keep local originals.
D-494 package/process/future requests are untouched. These observations are not new managed tests;
no build, old test suite, authentication or unrelated accepted cohort is repeated.

Retain observation failures: a Home quick-action opened an empty expense draft and was cancelled
without input/Save; two early hierarchy captures were unavailable and continued without restart;
account identity is on the original due-row witness rather than the occurrence editor; category
chips do not expose a selected-name suffix; a retained transaction detail needs Back; the second
profile-delete confirmation is generic and is bound to the first named confirmation. All failed
guards preceded their intended input. Accepted commands are never replayed to repair observers.
This is not whole-device/security/ANR/TalkBack/iOS/provider/Store or release acceptance.

Evidence: [actual elapsed-day comparison](quality/physical-elapsed-forecast-comparison.md).

## D-507 - Same-day Android reminder delivery after actual D-494 failure (2026-10-24)

Owner accepts delayed Android delivery through the last instant of the originally scheduled local
calendar day, rejects next-day delivery and keeps inexact scheduling without new permission types.
Original D-49409:00 requests were dispatched by AlarmManager only at10:36:59.194/.200, beyond the
old one-hour allowance. The exact SDK binary rejects those original timestamps under that old
policy. The original timed criterion and interrupted cold lineage remain failed, not reclassified
as a pass. The precise OS/OEM reason for deferral is unproven.

ReminderDeliveryWindow computes the exclusive next-day boundary using the device zone, including
DST, invalid midnight and the earlier ambiguous midnight; the SDK's inclusive endpoint ends one
tick earlier. Original LocalNotificationScheduler.Request uses that allowance. Existing source/
profile/privacy/permission/cancellation guards, generic text, scheduling mode and missed-date
preparation remain unchanged. No exact-alarm permission, clock/configuration change, new SDK,
schema or financial write. Nine new distinct source cases pass in8+1 selected runs; only the
new ambiguous-boundary case runs after the first eight successes. Five separate actual-SDK
checks accept the two old late timestamps/last-day tick and refuse midnight/next morning.

A new notification-only signed Offline ARM64 package compiles the original request builder,
publication guard/window and SDK, without a database or financial fixture. Its one actual native
permission dialog applies only to that new UID. Two deliberately late09:00 requests on the real
24October clock enter AlarmManager at12:48:24, its original receiver dispatches at12:48:29.713/.724,
and native notification records show both unchanged generic messages at12:48:29.813/.856. The
original scheduler refuses yesterday's request before native publication. This is controlled
same-day late delivery, not a future09:00 punctuality, Doze, reboot, cold or general-device claim.
The native package has98 AOT modules and v2/v3 signatures; no INTERNET or exact-alarm permission.

Read the original D-494 protected source only after preserving actual dispatch: same24914/source
match true and original run. Retire both finished fixture packages after evidence, verify no
pending/blocked/posted work, and retain historical alarm records. Never replay successful account,
plan, profile, notification-content-tap or financial setup to repair observations.

Strict Windows and complete signed dual-architecture Offline APK builds pass without warnings.
Shipping APK has384 AOT modules,408 ELF libraries with1608 load segments meeting static16KiB
alignment and successful official ZIP alignment verification. This is not API24/16KiB runtime
acceptance. The final APK hash is984f01095439a6ca3ff979755085d6fd5af6208256dc2345b7924ba7a031d9a6.

Before this operation Main's earlier25043 was already absent; ordinary launch reaches27501 with
unchanged D-495 APK/Home979/141 public tuples. After fixture retirement, one complete signed
no-reset Main upgrade adopts D-507 (including D-497/D-499/D-500/D-504), yielding30005 and exactly
the same public Home/three original profile names/OS. This adoption is not an additional phone
acceptance of the deleted D-496 retained-tab scenario. Remove12 new owned XMLs after hash equality;
retain local originals. Release the coordinated Samsung turn. No financial commands occur.

Retain three observer issues without replaying native publication: notification record line
endings, an unbounded delivery-history section and an incorrect demand that compressed native
ZIP entries have mapping alignment. Official zipalign plus ELF checks resolve the static observer
issue on the same unchanged APK. Native receiver/notification receipts come from the first actual
accepted publication. General stability/accessibility/security/iOS/provider/Store/release gates
remain separate. [Evidence](quality/physical-timed-goal-period-reminders.md).

### D-508 - Physical retained forecast-tab acceptance

The two original D-497 retained-tab cases now pass on Samsung in a new independent Release/AOT
ARM64 package: four real tab clicks, same snapshot page showing-10EUR, same forecast page showing
97EUR with its what-if draft preserved. Immutable snapshot/base remain equal; only the new owned
profile/package is retired. Main D-507/Home979/all141 public tuples/OS match without reinstall.
No accepted financial cohort or unchanged successful suite/build is repeated; no new managed-test
count or broad security/stability/accessibility/platform/release acceptance is claimed. Evidence:
[physical retained-tab completion](quality/forecast-retained-tab-return.md).

### D-509 - Home financial-source revision

Reproduce Home accepting an old empty ledger after one original committed expense. Add a local
migration-owned revision row and18 atomic triggers for ledger/budget/category-limit/rate/holding
tables; capture it in the complete read and recheck before uncovering. Reuse bounded same-file
refresh and preserve original access/input guards, with no financial replay or second complete
history read.21 new distinct cases and115 related validation/migration/portable cases pass.
Original native Home shows90 after its one fictitious expense; retain the failed exact-publication
count guard. A read-only same-source continuation audits that accepted writer and deletes only
its owned profile; it does not claim the failed count as a pass. Strict builds and the signed
Offline384-AOT APK pass. Main no-reset migration preserves Home979/all141 public tuples/three
profiles/OS. Evidence: [financial-source revision](quality/home-financial-source-revision.md).
General runtime/performance/accessibility/platform/provider/Store/release gates stay separate.

### D-510 - Original monthly calendar/count acceptance on Samsung

Three distinct original fixed1EUR monthly definitions on one new zero-opening fictitious profile
qualify Gregorian31/30/31, Persian31/30/30 and Hijri30/29/30 previews, named rule calendars and an
ending count3. Each Save occurs once, Reminder/automatic recording remain off and All dates/All
types history is empty. One ordinary Main/profile reopen retains all three original stored rows
and Home0 without financial replay. Delete only the inactive fresh profile with both original
confirmations; original Main12153/Home979/all141 public tuples/three profiles/OS/D-509 APK match.
Remove154 hash-matched owned XMLs; no new managed-test count, build, existing successful cohort or
financial posting is repeated. Evidence: [monthly calendar anchors](quality/physical-monthly-calendar-anchors.md).
Skip/date-end/custom/holiday/timed/runtime/accessibility/iOS/provider/Store/release gates stay separate.

### D-511 - Preserve complete financial snapshots after the Home revision migration

Hosted D-509 CI has1331 failed/3875 successful/25 skipped cases; the shared test inventories
reject its known migration-owned HomeFinancialRevision table. Explicitly include that table
while retaining unknown-table refusal and exact complete no-write/rollback/repeated-action
comparisons. Only accepted financial round trips or existing intentionally omitted financial
writes allow a validated opaque-token difference. All financial rows/links/receipts stay compared.
Two strict test-project builds have zero warnings/errors; retain1275 initial successes, repair
59 newly reachable token expectations and pass69 focused continuation cases, including three
new real-SQLite oracle guards. All1337 distinct selected cases now have successful receipts;
this is not a whole-suite rerun. Production code/migration/APK and phone data remain D-509.
A new exact-head hosted result remains pending at commit creation. Security/stability/performance/
accessibility/iOS/provider/Store/release acceptance stays separate. Evidence: [CI snapshot oracles](quality/home-revision-ci-oracles.md).

### D-512 - Physical Skip and original-calendar ending

Two new original fixed1EUR monthly definitions on Samsung qualify missing-day Skip and date-based
ending: Gregorian31October/31December/31January with inclusive31January2027, and Persian31Shahrivar
only through30Aban1406. Independent platform calendar lengths agree with all four visible preview
rows. Each definition is saved once; Reminder/automatic recording are off and All dates/All types
history is empty. Ordinary Main/profile reopening retains both original stored-row descriptions
and Home0 without financial replay. Delete only the inactive fresh profile with both confirmations;
original Main12153/Home979/all141 public tuples/three profiles/OS/D-509 APK match. Remove158 fresh
hash-matched owned XMLs and release Samsung. Preserve early-frame/host Back/nonowned-tree refusals;
no system input or settings change is used to recover them. No new managed count/build/successful
cohort is repeated. D-511 exact-head CI/CodeQL succeed and its one-shot monitor is consumed/deleted;
this new documentation head awaits its own check. Evidence: [physical Skip/calendar ending](quality/physical-skip-calendar-ending.md).
Custom/second-day/holiday/future delivery/runtime/accessibility/iOS/provider/Store/release gates remain.

### D-513 - Original budget-source publication on Samsung

One new source-flow SQLite case passes: the captured10EUR budget changes once to20EUR under
cover, the first packet is retired and the second read publishes once without a ledger entry.
Only this new case runs; strict App.Tests and independent ARM64 Offline builds have zero
warnings/errors. The signed192-AOT caller owns a fresh package and refuses historical phases.
Fifteen synthetic host observer checks are separate from the one new application test.
After explicit phone handoff, actual original Home/store process26285 confirms covered-at-write,
one accepted budget change, one completed publication, ready/nonfailed state and the original
visible20EUR caption at895x51, empty ledger and complete original independent-base equality.
Delete only its new profile and package. Original Main12153/Home979/all141 public tuples/three
profile descriptions/current selection/APK/OS match;13 new owned XMLs are hash-matched and removed.
The initially retained blank expense closes through original Cancel without a dirty prompt or
financial Save/Discard. Preserve initial baseline/host label refusals and pre-device compile/
package-observer failures. No accepted writer, successful suite or financial cohort is replayed.
D-509's old failed expense publication count stays unaccepted; this is a distinct budget cohort,
not financial-form or general stability/performance/accessibility/security/platform acceptance.
Production code and shipping APK remain D-509. Prior D-511/D-512/D-514 CI/CodeQL succeed and their
one-shot monitors are consumed/deleted; this batch awaits its own exact-head CI at commit creation.
Evidence: [budget publication](quality/home-budget-publication.md). Continue genuinely uncovered work.


## AT-515 - Custom/second-day/holiday source and conditional captions

Four original fixed1EUR definitions qualify custom every3 days/count4, monthly day1 plus day15/count4,
and Germany nationwide holiday-forward/backward/count2. All12 native preview rows match independent
dates/weekdays. Each original Save occurs once, Reminder/automatic recording remain off and All dates/
All types history is empty. Ordinary Main/profile reopening retains all four exact stored-row strings
and Home0. Preserve early-frame, USB/WiFi, disclosure-state and host quoting/foreground refusals;
continue the same source rather than replaying preparation, accepted definitions or money.

The actual enabled-holiday preview contradicts old en/es/fr/it Weekend_Hint text. Correct only those
four values to match the existing optional-holiday policy; fa/de resource files are unchanged in this
batch. Four new actual-source SQLite cases fail against old text and pass after the repair. Thirty-six
original Windows caption contexts cover six languages, both themes and actual360/412/1280 widths,
with unchanged complete fixture rows, zero financial Saves and process4732 exit0. Twelve original
Samsung caption/Cancel contexts cover the same six languages and both themes on the preserved
holiday plan, matching full source strings, positive visible bounds and the checked holiday control.
No existing successful financial cohort or unrelated suite is repeated.

Strict Windows and canonical complete Offline Android builds have zero warnings/errors. The signed
two-ABI candidate has384 AOT/408 ELF, v2/v3, ZIP CRC/privacy and static ZIP/ELF16KiB proofs; these do
not establish16KiB runtime acceptance. No-reset adoption changes the APK SHA256 legitimately to
1f09c70fd8d27a7877c91b1a958ebf530caf6136fcdaac0a961c693d72ea4788 and Main PID to31197. Original
Home979/all141 public tuples, the three prior profiles, English/Light/Simple and recorded OS remain.
Delete only the inactive fresh profile with both original confirmations. D-517's owner-authorized
300000-to1800000ms temporary timeout is restored exactly to300000ms and verified. Remove465 unique
owned remote XMLs in two hash-matched batches; keep466 local copies. Standard build/test cleanup
succeeds and only two unused new shipping copies are removed; signed APK/evidence remain.

No recurrence algorithm, financial writer, schema, SDK, permission or commercial activation changes.
Broader financial/security/stability/performance/accessibility/iOS/provider/Store/publication gates
remain open. New-head hosted CI is pending at commit creation. The independent unfinished D-516
SUPA work is not part of this delivery. Evidence: [original recurrence and caption qualification](quality/physical-custom-second-day-holidays.md).

## D-516 - Original SUPA preset evidence

67 new selected actual-source cases qualify complete bounded original protocol,6-language
consent/Cancel/import/reimport/reopened Undo, privacy and stale-file/authority handling.29 directly
affected StarMoney cases and23 resource checks pass.36 native Windows contexts plus12 final
Help-only contexts use unchanged complete source rows and zero financial writes. Keep the2 RED
privacy results and initial resource output-location failure. Six original Android source/Help/consent
contexts pass in retained UID10395/PID30097 after two preserved terminal observer failures. Native
financial writes remain0. Complete signed adoption verifies Main31882/Home979/141 public tuples,
three original profiles, original Main chooser/Help, OS and timeout restoration;28 owned remote
XMLs/new caller are retired. Bank customer specimens, native external picker, API24/16KiB runtime,
all-platform security/stability/accessibility/provider/Store/release remain unqualified.

### D-519 - Independent runtime-smoke and large-text cohort

- Passed36 new SUPA Windows process-local scale2 contexts: six languages, two themes and three
  actual widths;324 native font observations, eight tall endpoints, footer targets and original
  typed rows verified. Zero financial writes; unchanged dialogs/native Import not replayed.
- Passed complete signed Offline app installation, original first-run and empty Home/Plans/Budget/
  Reports/Transactions navigation on actual API24/4096 and API35/16384 x86_64. App PIDs2754/4551
  remain; no matching app fatal/ANR in captured bounded logs. API24 font setting is unset.
- Keep full platform/native-engine/recovery/OCR, ARM64/phone, actual OS200%/TalkBack speech,
  general stability/performance, iOS/provider/Store/release gates open.
- Preserve pre-guest launch and observer failures; retire only new AVD fixtures and matched XMLs.

See [D-519 evidence](quality/android-runtime-supa-large-text.md).

### D-520 - Native key protection across two new runtimes

- Pass original create/cold/retire on each x86_64 API24/4096 and API35/16384 system, retaining one
  run per UID and proving actual helper process death before each subsequent phase.
- Pass24 binding/tamper refusals and four denied removals; four authorized aliases stay absent
  after unwrap refusal without replacement. Six native phase receipts are not managed test cases.
- Preserve null security metadata on API24 and software0 on API35; no hardware-attestation claim.
- Only public receipts leave the helpers. Main/private keys and successful phone/financial cohorts
  remain untouched. Remove both fresh helpers and stop owned AVDs after installed digest equality.

[Scope, command-line and digest-observer failures](quality/android-key-runtime-compatibility.md).

### D-521 - Original storage and controlled-fault runtime cohort

- Pass original first-schema through51migrations, compiled model, full typed-row portable staging,
  correct/wrong/missing keys, changed-page refusal and no plaintext markers on both new runtimes.
- Pass actual prearmed candidate/Selected process death and cold continuation, missing selected key
  with no replacement/plaintext fallback, and protected cold recovery with damaged/wrong vault refusal.
- Retain32final-cohort records, two late-original continuations and two read-only diagnostics;
  preserve two old host-poll failures. Exact source copies avoid new financial seeding.
- Pass ten targeted controller contracts. PS7 native execution and actual5.1 syntax-only evidence
  remain distinct; no execution-policy change or override.
- Remove only new helper UIDs, stop owned AVDs and unused copies; keep private fixtures off the host.

No managed count increase or broad Main/all-profile/hardware/OEM/ARM64/iOS/release acceptance.
[Evidence](quality/android-storage-runtime-compatibility.md).


### D-522 - Native profile and restore runtime subgates

Nine original two-profile/multi-generation cohorts pass in18 native phase receipts on each
actual x86_64 API24/4096 and API35/16384 system. Covered startup, interrupted creation, missing
envelopes, wrapping-alias loss, recovered retirement, validated/retained-preference restore,
pending-reference refusal and consecutive restore recovery preserve original model/money/key
invariants through proven cold process transitions. Reuse the complete D-521 signed package;
zero new managed cases, no source/schema/permission/shipping change or repeated passing build.
Prepare fictitious rows once per required new cohort; no accepted financial command is replayed.
Installed public bytes match, only the new helpers are uninstalled and owned AVDs stopped.
Physical Main/private keys, general SDK and existing ADB remain outside this turn.

Full arbitrary-profile/hardware/OEM/ARM64/biometric/authentication/UI/stability/accessibility,
iOS/provider/Store/release acceptance remains open. D-521 CI/CodeQL pass; its separate Windows
protected-workload workflow failure remains under investigation, with Android producer success
preserved. See [bounded original runtime evidence](quality/android-profile-runtime-compatibility.md).


### D-523 - Correct independent workload revision preservation

The failed Windows qualifier step reproduces locally: deliberate validated Save/Delete operations
restore all financial rows but correctly change HomeFinancialRevision. Keep production snapshot
verification strict. Normalize only the validated opaque singleton in a disposable benchmark
memory copy, then compare full schema/headers/triggers and every other typed row. Eight focused
new cases cover round-trip/read-only acceptance and financial/schema/header/revision/boundary
refusals. Revalidate the same retained reference from its original encrypted rollback projection
without replaying financial commands; execute only the previously unprepared tenfold shape.
Its four saves read back and remove exactly their own rows, and repeat admission refuses.
No app/MAUI/Main/phone/dependency/schema/permission change or unrelated passing suite repeats.
D-521 CI/CodeQL pass, Android producer succeeds, Windows protected measurement fails. The owner-opened authenticated browser now shows the same snapshot-content exception at original
Program.cs line195 after a zero-warning build. Corrected D-523 Native cipher candidates succeeds;
latest D-525 CI and CodeQL also succeed. Preserve original failures without rerunning accepted work. See [local defect and hosted boundary](quality/protected-workload-revision-preservation.md).


### D-524 - Native metadata durability on minimum-API/16KiB systems

Original identity staging, journal/history publication and hot-journal rollback/committed selection
pass on x86_64 API24/4096 and API35/16384 using unchanged signed441faf38. Twenty observations comprise
ten controlled synchronized native deaths and ten terminal receipts; zero new managed cases or
financial preparation/commands. Same-source cold lineage, complete original facts and installed
code match; remove only new helpers and stop owned AVDs, preserving ADB/Main/phone/SDK boundaries.
This closes only named controlled metadata subgates, not natural power loss/hardware/OEM/ARM64,
authentication, full platform/UI/stability/accessibility/iOS/provider/Store/release acceptance.
See [actual metadata receipts and limits](quality/android-metadata-runtime-compatibility.md).


### D-525 - Image/PDF native runtime compatibility

The original reader/parser process ten licensed public photographs and nine fixed image-only PDFs
once on each x86_64 API24/4096 and API35/16384 runtime. Preserve five correct Found/three Review/two
NotFound photographs, five Latin scan totals, bounded last-page total and conflict/no-total refusals;
Persian scan OCR remains unaccepted. Thirty-eight observations/four terminal batches add no managed
cases. One new strict Offline signed192AOT/204ELF16KiB package, exact source assets/licence and
installed-code digests pass; zero financial saves, no Main/phone/private-key access. Retire only new
helpers/owned AVDs, clean isolated output and61098430bytes of unused copies, preserving signed code.
Full camera/corpus/saved-flow/Persian-image/iOS/ARM64/stability/accessibility/provider/Store/release
qualification remains separate. See [exact original runtime evidence](quality/android-receipt-runtime-compatibility.md).


### D-526 - Original physical SUPA import and durable Undo

One new fictitious Samsung profile/account opens900EUR. The owner selects the exact original
SUPA CSV in the external picker; actual full source/target/two-row review, original consent and
one confirmation import30/-7.50, preserving quoted/UTF-8/CRLF narrative, two unreviewed ledger
rows and Home922.50. One proven cached16616 death and COLD24834 preserve that same data and
original durable batch; one original confirmed Undo returns900/All-history empty. Return Main979,
delete only the fresh inactive profile with both confirmations, and preserve original three
profiles/all141 public tuples/installed D-516 hash/OS/timeout300000. Delete97 owned hash-matched
XMLs and one owned CSV; keep local receipts and all guard refusals. Zero new managed cases,
financial replay, production changes, builds or passing-suite repeats. All bank/customer-file/
platform/security/stability/accessibility/iOS/provider/Store/release gates remain separately open.
See [original financial, cold, Undo and restoration evidence](quality/physical-supa-import-undo.md).

### D-527 - Bounded protected Home diagnostic evidence

The independent Windows home-stages mode separates actual complete Home reading from final source
validation on retained D-523 reference/tenfold generations. Four observations preserve complete
original packets and exact portable schema/typed rows with unchanged financial revision, zero
financial writes and no fixture recreation. Tenfold read463.71/570.91ms and validation146.28/123.70ms
are local store boundaries, not Android/UI/cold timings or a product speedup. Strict independent
build and missing-fixture refusal pass; no new managed cases or unchanged native suite/APK replay.
Physical D-526 preserves its original opening900EUR/empty ledger and is qualified separately.
Android projection/read diagnosis, whole-Home/two-second, ANR/accessibility/platform/external gates
remain open. See [exact observations and limits](quality/protected-home-stage-diagnosis.md).


### D-528 - Transactions Add beside the title

Owner-requested Transactions + now matches Home/Plans: original48px accessible command physically
right of a growing title, original pushed Back, no reserved bottom Add band/overlay. Preserve full
filters/results, empty invitations and separate contextual bulk/Undo rows; filter sizing reserves
only the actual header and contextual actions. Eighteen Windows contexts/54 images qualify populated,
filtered-empty and original selection states; six independent Samsung Release/AOT contexts qualify
native Add/Cancel/bulk with unchanged original stored inputs. Retain controller/count and observation
refusals without replaying completed work. Strict builds, complete signed Offline384AOT/408ELF16KiB
static checks and no-reset shipping upgrade24834->32206 pass. One original Main Add/Cancel, Home979,
all141 public tuples/three profiles/OS/timeout300000 remain; no Main financial writes or new managed
cases. Retire the new helper and15 hash-matched XMLs. Full stability/accessibility/iOS/external
release gates remain separate. See [complete runtime and package evidence](quality/transactions-title-add.md).


### D-529 - Original complete-ledger stage subgate

Twelve actual Windows/API36-x64/Samsung-ARM64 observations pass full32-field/order, original financial
controls and source preservation; a separate incomplete metadata fixture refuses before timing/key
reads. Restore exact original caller APK/UID/stopped/grants and Main/public/OS on both native targets,
with phone30047 preserved only during its fresh slot. Remove only new owned XMLs and return the phone;
stop only the owned emulator. Reject the typed-timestamp experiment without a shipping change. No
new managed cases or financial commands. Full Home/two-second, general QA-06/A11Y and platform gates
remain open. [Evidence and boundaries](quality/retained-ledger-stage-diagnosis.md).


### D-530 - Changed Transactions title at Windows process scale2

Eighteen new en/fa/de/Light-Dark/360-412-1280 populated/empty/bulk contexts yield54 own-window images
and native full-title/ratio2/height/viewport observations. Original source rows/settings/plans and
D-528 receipts match, PID19056 exits0 and Default input desktop is unchanged. No financial preparation/
commands, new managed cases or original normal-size replay. This named Windows layout subgate passes;
real OS scaling, Android/speech/all-controls/stability/platform and release acceptance remain open.
[Exact source and native evidence](quality/transactions-title-large-text.md).

### D-531 - Current retained Windows Home publication

Four new actual Home constructor/publication/native-layout timelines on the retained D-523 protected
10,000/100,000-row sources complete in process61988/exit0. Advanced/default layout, all20/200 VM
accounts, original balances, covered publication and no synchronous appearance reads match. Full
portable schema/typed rows, every original source/metadata/envelope file, the independent installation
inputs and original receipts remain unchanged; zero seed/financial commands or phone queries.
Tenfold native layout completes1226.20/1144.04ms from before construction on this Windows path,
not cold/Samsung/two-second or general stability acceptance. Strict Debug build/isolated cleanup and
prelaunch missing-source refusal pass; no managed count or shipping APK change. Retain the broader
performance/stability/accessibility/platform/external gates and pursue evidence-supported remaining
work without replaying these accepted samples. [Evidence](quality/home-publication-stages.md).

### D-532 - Current Android Home and preserved concurrent-major fault

Reuse original retained Android rows/keys through independently copied encrypted sources. Their
single D-509 additive migration uses the actual protected callback and preserves every original
schema/header/typed row after normalizing only new bookkeeping in disposable memory. Six current
API36/x64 native Home contexts/twelve samples retain all financial/native/layout checks. Preserve
three completed reference contexts across actual4070/SIGSEGV; continue only the unexecuted tenfold
contexts. Matched official symbols locate concurrent-major object scanning, without proving a cause
or fix. Tenfold1939.55-2353.59ms navigation publications do not close the two-second/cold/phone gate.
Recover the same completed inner report after an overbroad file guard counts new presentation
outputs; eight exact-source guard contracts and four fresh read-only source/control audits pass,
without UI or financial replay. Historical timing-interval exact-byte equality remains unaccepted.
Restore caller UID10221/SHA/stopped/grants, Main2963/Home0/all120 public states and captured OS;
remove three owned hash-matched XMLs and stop only the dedicated AVD. Strict builds/standard clean
pass; additional residual host deletion is refused and not claimed. Shipping D-528 is unchanged.
[Exact lineage, native fault and limits](quality/current-android-home-stages.md).


### D-533 - Current physical Home with original timing-byte evidence

Six actual Samsung current-Home contexts/twelve observations reuse the original encrypted10,000/
100,000-row sources, with zero seed/financial commands or new keys. The original additive callback
and complete schema/header/typed-row/native controls pass. Retain a create-once synchronized
pre-native source/code/day fingerprint and zero-case progress; ten focused actual-policy contracts
pass and the physical timing-interval byte/source guards pass. Preserve the undeclared Activity
controller refusal; start the resolved original launcher once and observe the same32040 after the
first report window expires. Separate clone and failed-adoption provenance. Tenfold4777.22-4992.71ms
publications keep the two-second/general stability gates open; original D-532 historical-byte refusal
and native death remain. Restore original caller UID10381/APK/stopped/grants/no-pending, Main24725/
Home979/all141 tuples and OS; remove two owned hash-matched XMLs and explicitly return the phone.
Three strict builds/standard clean pass;187287221bytes additional host deletion is refused before
execution. No unchanged Windows/full-suite or accepted financial replay; product test count and
shipping D-528 stay unchanged. Runtime/compiler/task10.0.13 identities match; no unsupported mix
or general crash fix is established. Broader performance/accessibility/platform/release gates remain.
[Original runtime evidence and remaining limits](quality/physical-current-home-stages.md).


### D-534 - Bounded current Home CPU sampling

Preserved original API36/x64 sources produce a code-address-only CPU sample with2733 samples,
zero lost and no raw stack/register payload. The actual SDK classifies69 empty AOT objects;
all123 method-bearing objects match the diagnostic signed artifact. Retain initial4010/SGen
SIGSEGV and Android EXIT_SELF/status255 separately; same-source4313 continuation keeps the three
accepted reference contexts and executes only missing tenfold contexts. Complete source/native
and premeasurement-byte controls pass without seed/financial replay. Flat CPU is Mono40.98% and
SQLCipher17.38%, but54.26% broken callchains preclude reliable inclusive/root attribution, KDF-only
claims or a whole-Home/cold/phone speed conclusion. No product/runtime change or new product tests.
Original caller restoration hits storage exhaustion; documented package-specific maintenance and
Android KEEP_DATA code removal/reinstallation restore exact original digest/UID10221/stopped/grants.
Main2878/Home0/all120 public tuples and OS match; three owned hash-matched remote files are removed
and only the dedicated AVD is stopped. Post-restoration private DB/key reopen is not claimed.
General stability, two-second, accessibility/platform/external gates and the broad goal stay open.
[Actual sample, lineage, restoration and limits](quality/current-home-cpu-sampling.md).


### D-535 - Protected-source reopen after data-preserving maintenance

One focused API36/x64 read-only audit verifies existing Android keys/generation bindings after
D-534's KEEP_DATA code maintenance. Reuse the exact signed D-532 audit caller and original
lineage; process3600 reopens four original/adopted sources, matching complete schema/headers/typed
rows and all source files. Zero financial/seed/key creation, native UI or timing replay; historical
D-532 timing-byte refusal remains false/unaccepted. Ordinary signed-r installations return the
original caller UID10221/digest/stopped/grants, Main2845/Home0/all120 public states and exact OS.
Two owned XMLs are hash-cleaned, one redundant57818872-byte host copy removed after equality and
only the dedicated AVD stopped. No rebuild/unchanged suite or phone query; shipping D-528 unchanged.
This bounds independent source retention, not Main-key attestation or general platform/stability/
performance/accessibility/release acceptance. D-533 CI/CodeQL succeed and its follow-up is consumed.
[Actual read-only outcome and scope](quality/post-maintenance-source-retention.md).


### D-536 - Withdraw reduced-allocation ledger ordering

A stable-merge sort reduces host allocation but slows every large shape; a cached UTC/day/ordinal
candidate reduces100000-row host sort allocation4000504 to1600168 bytes and passes thirty original
object-order shapes plus seven affected SQLite regressions. Actual original3844/candidate4548
API36/x64 cohorts retain all schema/typed-row/native/financial/premeasurement-byte controls with
zero seed/key/financial replay. Candidate report observation continues the same live4548 after
its first window expires. Large-pair ratios1.0035/1.0123/0.9659 show no stable material Home benefit;
withdraw all candidate product code and keep shipping D-528 unchanged, without phone repetition.
Preserve an initially missing installed APK after boot; signed restore, sync/orderly shutdown and
one new cold boot then retain exact original UID10221/APK/stopped/grants, Main2880/Home0/all120 states
and OS. Root cause/power-loss safety remain unproved. Retire only two completed inactive emulator
callers' code after exact APK backup; retain their data/keys/UIDs. Fresh returned phone Main27069/
Home979/all141 states remains untouched except read-only preflight/owned XML cleanup. Five emulator
XMLs and one phone XML are hash-cleaned; standard cleans and111079544-byte duplicate removal pass.
Automatic removal of251505548 bytes of three new output trees is refused before execution; no
alternate deletion. Latest D-535 CI/CodeQL succeed. Product test count and wider acceptance stay open.
[Actual comparison, storage recovery and limits](quality/withdrawn-ledger-ordering.md).

### D-537 - Physical Home CPU windows on retained sources

Add an explicit one-publication tenfold/en code-sampling scope with native-monotonic boundaries,
durable scope identity and a released strong full-ledger oracle reference. Ten exact-source scope
contracts, ten unsafe-header refusals, one oversized-input refusal and retained2733/1324/0 native
filter controls pass. Three necessary strict independent ARM64 publishes pass; only the final
signed offline123-packaged-AOT/135ELF artifact is installed. Preserve first recorder exit134 before
any samples and its completed4296.4346ms publication. One necessary CPU-only retry on the same APK
records8634 samples/zero lost; exact native/inspector filters retain2380 inside4140.808074ms,
partitioned2437.831921/1490.250461/212.725692ms. All original/adopted bytes, complete schema/typed
rows and native financial/recent packets match without seed/key/financial replay. Official Mono
BuildId/debuglink-CRC and packaged AOT symbols resolve actual function ranges; balance/index,
SQLCipher and concurrent GC work coexist, unknown samples and an uncollected oracle limit causal
claims. Restore original caller UID10381/APK/stopped/grants and Main27069/Home979/all141 states/OS,
remove eight owned hash-matched remote files and return the phone. Product source/APK remainD-528;
no unchanged Windows/full suite repeats or new product test count. D-536 CI/CodeQL succeed and its
one-shot is consumed/deleted. Two-second/general stability/accessibility/platform/release gates and
the broader goal remain open. [Complete code-window evidence and limits](quality/physical-home-cpu-windows.md).

### D-538 - Withdraw direct endpoint-index construction

A direct original-reference loop lowers host constructor cost but offers no material allocation
relief; its protected read-only-view variant adds8.46% allocation in the many-account shape.
Twenty-six focused actual-Core cases, including two new collection-mutation/captured-slice
contracts, and strict managed/independent ARM64 builds pass. Preserve one unexecuted fixture
compile refusal, the observer240-second parameter refusal and its same10999 continuation,
plus candidate12246's continued observation. Original/candidate single tenfold/en publications
4131.4756/4010.2595ms pass all encrypted-source/adopted-byte/schema/typed-row/native/financial
guards without seed/key/money replay. The2.933966% reduction fails the previously recorded5%
each-pair criterion; omit the unnecessary second pair, restore exact original product source
and pass only the two new tests on that rebuilt original. Restore caller UID10381/APK/stopped/
grants and Main27069/Home979/all141 public states/OS; hash-clean two XMLs and return the phone.
Three scoped cleans and177874001-byte unused-copy removal pass; automatic review refuses deletion
of132837195 residual output bytes before execution, with no alternate attempt. D-537 CI/CodeQL
succeed and its one-shot is consumed/deleted. Product/APK remainD-528; coverage adds two cases,
not a complete-suite pass. Two-second/general stability/accessibility/platform/release gates
and the broader goal stay open. [Complete evidence and limits](quality/withdrawn-account-index-loop.md).

### D-539 - Isolate complete Home controls in another process

The released100000-row oracle remains alive in D-537/D-538; add explicit preparation/measurement
scopes without changing product source, financial formulas or GC policy. Bind compact original
balance/recent/full financial-presentation controls to source-byte-fingerprint/payload hashes,
day, Data/Core/Caller MVIDs and preparation PID/UID. Thirty-eight exact-source identity/scope
contracts and strict initial/full AOT builds pass without warnings/errors; no product test count
or unchanged Windows/full-suite repeat. Complete signed offline/non-debuggable/non-profileable
ARM64 APK has192 packaged AOT/204ELF/804LOAD static16KiB checks and v2/v3/ZIP verification.
Actual15744/e9aac472 prepares original controls with zero target native publications; preserve
its expired observation and recover the same final report. Prove15744 dead, then the same APK,
without reinstall/adoption/seed/key/money replay, measures16357/275fc72d. No full target oracle
read occurs before timing; complete original/adopted bytes/schema/typed rows and independent
financial/native packets match. Elapsed4321.904ms/read2735.4251/model1304.2752/layout281.8074 and
max dispatcher536.1012ms still fail the two-second objective; no product improvement/regression
or causal GC claim. Restore caller UID10381/APK/stopped/grants and Main27069/Home979/all141 states/
OS, hash-clean two XMLs, remove53992832 duplicate bytes, perform scoped cleans and return the phone.
Residual native cache131261554 bytes is inventoried; earlier cleanup refusals remain untouched.
D-538 CI/CodeQL succeed and its one-shot is consumed/deleted. General stability/accessibility/
platform/release gates and the broader goal remain open. [Complete evidence and scope](quality/separate-process-home-oracle.md).

### D-540 - Complete encrypted ledger transport (locally qualified and adopted)

Preserve the complete32-field history and original physical/date/creation tie order while adding
bounded512-row internal encrypted read blocks. The original source/writer/financial formulas stay
authoritative; revision/schema/count/digest/type/pipeline guards fall back to scalar reads without
getter repair. Original transaction commit validates authority before/after affected-block upkeep;
cancellation/IO cannot leave a separately accepted financial edit. No EF entity/model/key/permission
or new SDK change. Focused codec/migration/source/rollback/backup/restore/authority/observer checks
pass in selective continuations; preserve original fixture/parameter/observation failures, not a
full-suite green claim. Strict Windows and full signed offline dual-ABI384AOT builds pass.

Two opposite-order, same-APK/source/day/module Samsung pairs use independent scalar controls in
other proven-dead processes:4710.9157to3323.291ms and4665.0514to3304.8118ms (29.4555%/29.1581%).
All original financial/native/recent/presentation and complete source guards match. Four single
tenfold-English publications do not accept cold/full-device/language coverage or two-second Home.
No accepted financial seed/key/command is repeated. The two-second objective remains open.

The exact old caller APK supports50 migrations ending20261017013925; reverse only M1/M2 internal
bookkeeping on empty independent Main/UID10381 using durable original request, actual memory Down
and full source/schema/header/typed-row proof. Native18045 passes with no shipping/workload touch.
Preserve an earlier undispatched17203 entry-registration omission; correct only its Activity
allowlist and retain original performance APK/Data/Core exact-byte continuity without replay.
Initial old-APK installation observation expires at35seconds with Play Protect foreground; retain
that pending result. After the owner's personal decision, the same install completes with original
b1d5 APK/UID10381; normal21974 startup reaches original onboarding, then stops with proven death.
Main27069/Home979/all141 states/OS remain exact. A local UTF8-reader refusal precedes any shipping
install; correct only encoding and preserve the baseline. After explicit handback, one complete signed no-reset upgrade adopts e0d3 shipping bytes and
Main27069 becomes27774. Preserve the initial16-node Preparing refusal; the same process then
matches Home979/all141 public states and all three original profile actions/selection/OS.
Independent original caller APK/UID/stopped state/storage identities remain exact; remove only17
owned hash-matched phone XMLs. No financial Save or private Main key/database read occurs.
Independent new-candidate bootstrap also reaches
original onboarding on API24/x64/4096 and API35/x64/simulated16384, with no UI/financial input;
remove only new fixtures/two owned XMLs and prove both AVDs stopped with sharedADB76812 retained.
Goal/full stability/accessibility/iOS/provider/Store/release remain open. [Evidence](quality/complete-ledger-read-blocks.md).

D-540 failed-case continuation while the phone gate remains: select only the eight initial store
cases lacking any successful terminal outcome. Actual strict rebuild and all eight cases, including
fixture disposal, pass; exact result-name comparison excludes previously successful/unrelated
cases. Preserve initial failure receipts; no native financial commands or full-suite claim.

### D-541 - Operation-local Home publication acceptance

Eight new real-SQLite session cases and four actual non-UI application-flow cases pass for context
reuse, fresh writer visibility, original input/authority refusal, cancellation, maintenance drain,
period changes and same-file reread under the original cover. One owned-only diagnostic case and
three affected original authority/file cases also pass:16 distinct selected receipts,13new case
variants, no full-suite or unrelated rerun. Initial two fixture-serialization failures are preserved
and only those cases continue after correction.

Four native original100000-row publications preserve complete source/financial/recent/presentation
equality and actual one-versus-two context counts. Opposite pairs improve5.9096%/5.2783%; the target
of two seconds remains unmet. Separate control PIDs finish and die before measurements.
Main a2d98e/PID29566 preserves Home979/all141 public states, all three original profiles and
exact OS settings after the one signed no-reset upgrade and authorized normal Resume. Original
independent caller bytes/UID/storage/grants/stopped state match; ten new owned hash-matched XMLs
are removed. The earlier foreground refusal and17997 external task removal remain recorded.
Two newly owned independent x64 compatibility fixtures qualify nonempty100000-row block reads
and actual original Home publication on API24/4096-byte pages and API35/simulated16384-byte
userspace pages over a4096-byte kernel. Each fixture is seeded once with110000 reference/tenfold
rows; scalar controls finish in a separate proven-dead PID before the one native publication.
Complete original schema/typed-row/financial/native guards pass with one context, one block read,
zero scalar reads and no post-setup financial command/key/seed replay. Both initially absent
packages are removed and only the two owned AVDs are stopped, preserving their settings.
This is not hardware16KiB, whole-platform, startup, stability or two-second acceptance.
No general ANR/crash, spoken reader, device, iOS or release acceptance follows from these cases.
[Full qualification](quality/home-publication-connection.md).

### D-542 - Complete row publication acceptance

Publish the original Home period/upcoming/recent/pinned-goal rows through the existing complete
snapshot collection, staging each original row set before one Reset. Keep all original mappings,
order, fresh theme objects, command identities, full reads, cover and final validation; chart/brush
publication and layout remain unchanged. Eight related existing cases pass after this changed
usage, not new-test/full-suite evidence. Strict Windows/full Android builds are warning-free.

On the retained x64 API36 sources,12 light and12 distinct dark native publications across
reference/tenfold and en/fa/de retain complete original schema/typed rows, financial/native
presentation and recent packets. Each of the four row sets produces one Reset and no per-row Add.
No successful seed, key or financial operation is replayed. These full-control elapsed samples
are not a causal speedup or physical two-second claim. The compatible independent caller stays
stopped on its additive schema because its ordinary Main is nonempty; empty-Main retirement is
inapplicable. Original APK backup/retained sources/Light remain, and shipping emulator Main's
120 original public states match the historical unchanged-APK baseline. Preserve its initial
15-node startup-only observation refusal.26 owned hash-matched XMLs are removed; only that AVD
is stopped.

The complete signed offline package has384AOT/408ELF/1608LOAD static16KiB/ZIP/v2/v3 guards and exact
actual packaged Data/Core LZ4 inputs matching the qualified independent APK. One Samsung no-reset
upgrade tobc1ea2 retains PID4090/Home979/all141 public states, all three profiles and original OS.
The unrelated-app foreground refusal is preserved and the owner's normal return resumes only
readback, never reinstall or money. Wider two-second/stability/accessibility/platform/provider/
Store/release gates and the broader goal remain open.
The eight selected cases are prior collection behaviours exercised after the changed Home usage;
the24 native observations are not24 unit tests. Physical nonempty pinned-goal rendering, new
geometry/OS scaling, spoken-reader and general stability are not newly accepted.
[Receipts and limits](quality/home-row-snapshot-publication.md).


### D-543 - Withdraw the shipping-equivalent large-nursery candidate

Keep production D-542 unchanged after a bounded independent32MiB concurrent-nursery experiment.
Earlier ARM64-library opposite-order results improve9.577468%/12.014471%, but their financial
images do not match shipping AnyCPU and are not promoted. A project-local diagnostic RID fixes
that input mismatch; all206 managed images match between the new profiles and actual packaged
Data/Core match the full shipping image. The shipping-equivalent pair improves3145.0593to
2801.7544ms, but terminal PSS/RSS increases112823/171516KiB exceed the predeclared65536KiB
ceilings. Reject without reverse/rescue pairs or relaxed criteria. These are post-audit residency
observations, not peak-memory or exclusive GC-causality evidence. The provisional app-only runtime
configuration is removed and no shipping Main install occurs; the two-second goal stays open.

Retain the fixed owned-query labels without SQL/parameters/values/paths/keys, one new actual
SQLite privacy case plus one affected original case, and17 actual compiled fingerprint cases
including four new runtime-profile refusals. Independent strict builds/full signed static
shipping witnesses pass; this is not a full-suite or product performance acceptance. No accepted
financial/seed/key operation or unchanged successful suite repeats. Original caller restoration
uses the unchanged guarded empty-Main retirement; broader goal/security/stability/accessibility/
platform/provider/Store/release gates remain open.
[Exact inputs, failures and qualification boundary](quality/concurrent-home-runtime-qualification.md).


Final D-543 restoration: original b1d5 caller22417 reaches unchanged English onboarding and is
proven stopped with UID10381/storage/grants intact. Main4090/D-542/Home979/all141 public states
and OS match; seven owned hash-matched phone XMLs are removed. Ten owned bin/obj trees totaling
4179578901 logical file bytes are cleaned. No Main installation/private key/database read or
financial replay occurs. The finite batch closes without a shipped runtime/performance change;
two-second and broader qualification/release gates remain open.


### D-544 - Withdraw bounded expansion scratch after original whole-Home comparison

A source-only scratch candidate reduces host codec allocation23.692979% but its actual original
Samsung100000-row publication3069.5168to3154.4552ms is2.767159% slower, failing the predeclared5%
gate. Original source/financial/native controls and memory/dispatcher gates pass; no reverse/
rescue pair follows. Restore production exactly; no Main installation or financial/seed/key
operation occurs. Keep one new real SQLite digest-valid incomplete-compression fallback test,
passing on the restored original codec. Eight scratch-only tests leave with the unshipped API;
31 distinct selected candidate checks and strict builds are not a full-suite claim.

Both same-head independent packages preserve original runtime/cipher and financial Core bytes;
metadata inspection bounds eight new pool method roots in the same official CoreLib version,
without claiming common IL/whole-AOT equality. The full uninstalled384AOT/408ELF signed candidate
matches actual independent Data/Core. Original caller27694/b1d5/UID10381/storage/stopped and
Main4090/D-542/Home979/all141 public states/OS are restored. Three owned hash-matched XMLs,
16 completed bin/obj trees and two duplicate audit images are removed. Two-second, general
security/stability/accessibility/platform/provider/Store/release and the broader goal stay open.
[Exact candidate, refusals and final restoration](quality/bounded-home-scratch-buffers.md).


### D-545 - Withdraw overlapped Home work after the fixed residency gate

The candidate overlaps the unchanged original plan/forecast computations with covered ledger
work, drains all siblings before operation disposal and classifies calendar-only covered retry
separately from persisted-data authority. Thirty distinct selected checks pass (13 new/17 affected
original), with strict Windows/full signed Android builds warning-free. Preserve initial test
compiler/cancellation and runtime-version-observer refusals; rerun only the failed observer case.

One same-day original Samsung100000-row pair improves3698.2818to3022.0211ms (18.285808%), but
terminal RSS increases113540KiB above the fixed65536KiB gate. PSS increases13066KiB; dispatcher
passes. Original source/schema/typed-row/financial/native guards pass; swap differs materially,
so no exclusive code/peak-memory cause is proven. Withdraw without reverse/rescue or threshold
change. Restore every production/test-project source exactly and remove the13 unshipped-API
cases; no new product-test count, Main install, financial/seed/key or runtime change is claimed.

Both independent APKs retain exact financial Data/Core/runtime engines; differing trimmed BCL
images have the same original official versions/method sets, without common-IL equality proof.
The uninstalled384AOT/408ELF full shipping candidate matches actual independent Data/Core.
Original caller5336/b1d5/UID10381/storage/grants/stopped, Main4090/D-542/Home979/all141 attributes
and OS are restored; three owned hash-matched XMLs are removed. The owner approves a temporary
shared-phone handoff only after restoration. Continue independent work; the broader goal and
performance/security/stability/accessibility/platform/provider/Store/release gates stay open.
[Exact inputs, refusals and restoration](quality/overlapped-home-projection.md).
Eight completed owned bin/obj trees (2322515022 logical bytes) and four duplicate audit
images are removed; actual packages/reports/backups stay. No host free-space claim.


### D-546 - Original Transactions title at actual Android OS200%

Close only the uncovered D-528/D-530 Android title/viewport subgate:36 distinct original native
observations across360/412dp, en/fa/de, Light/Dark and populated/view-only empty/selection states.
Actual OS font2.0 yields36px for this24dp title under Android nonlinear scaling; full original
translated text, physically right48dp accessible Add, separate contextual rows and at least144dp
results pass. Four original typed packets retain their digest; no seed/Save/Undo/native Add/Cancel
or successful normal-size case repeats. No shipping UI/financial source or product-test count changes.

Preserve the initial missing selector, later publication/source-precondition and host evidence/
locale assumptions. Three earlier English/Light360 successes are retained; only15 missing360 and
18 new412 states continue. Exact known comparison/source lineage is audited without scene replay.
Independent strict builds and192AOT/204ELF/ZIP/v2/v3/static16KiB guards pass; no unchanged shipping
Windows/Android build or managed suite repeats. Undo-visible/spoken/device/full A11Y remains open.

Two install-space failures precede qualification. Capacity attempts do not resize source images;
restore the exact original config and do not claim expansion. Retire only code of two ended owned
probes with hash-exact backups/KEEP_DATA, preserving UID/CE/DE and every data/key source; actual
free903608KiB admits install. Restore original compatible334ca caller5054/975 German/UID10221/
stopped without nonempty schema retirement. Shipping Main99ee/2904/all120 public states and OS
match; four owned emulator XMLs and one physical lease-guard XML are hash-cleaned, owned AVD stopped.
Unused verified recovery copy9232397904bytes, completed build770497442bytes and initial unadmitted
APK58990498bytes are removed; actually used packages/restore APKs remain. The owner-approved second
phone lease is active; no phone input follows. Broader goal/performance/security/stability/
accessibility/platform/provider/Store/release gates remain open.
[Original observations, refusals and restoration](quality/transactions-title-android-os-text.md).


### D-548 - AT-50 contextual-row safe-area regression

The actual ready Android selection-plus-Undo viewport originally measures120dp. Its arranged-
header budget now retains at least144dp in36 native Undo states at OS200%/360/412/800dp, en/fa/de,
Light/Dark, with complete translated title/notice and44dp original Undo target. Six source cases
pass; source packets and original command/Back/title placement are preserved. One prior success
and36 unaffected D-546 normal scenes are retained by exact lineage/actual-budget constraints,
not replayed. No financial delete/Undo/Save/seed/key occurs. Main/caller/OS emulator restoration
passes; physical latest-candidate adoption waits for pending original-caller Play Protect restoration
and a new safe phone turn; D-547's two matched native pairs are admitted. This is
geometry only, not journal/financial Undo, speech/general A11Y or release acceptance.
[Exact acceptance](quality/transactions-context-safe-area.md).


D-549 adds two new-platform nonempty-codec cohorts only: API24/native+managed4096 and simulated
API35/native+managed16384. Each preserves1031 new fictitious rows, compares complete original
scalar/block results, observes digest-valid malformed payload fallback/exact recovery and checks
original native Home against Core balance. Six native outcomes are not unit tests or zero
fixture writes. The original runs/receipts are retained, both fresh packages/fixtures removed,
prior packages/settings/config preserved and both AVDs stopped. No replay of prior protection,
migration, bootstrap or accepted financial cases. See [scope](quality/nonempty-codec-android-compatibility.md).
Hardware16KiB and general platform/stability/spoken-A11Y/release acceptance remain open.


D-550 accepts only the previously unperformed complete signed shipping896e no-reset adoption
on retained owned emulator Main99ee: UID/CEDE/permission flags/all120 public attributes/zero
balance/OS/config and stopped compatible334ca caller persist across original2969/new3751 normal
startups. No selector, financial command, managed/native cohort replay or private-key/DB export.
Two owned XMLs are hash-cleaned and the AVD stops. Physical restore/adoption and all broader
private-source, stability/performance/A11Y/platform/release gates remain open.


D-552 preserves first-pair failure rather than broadening native acceptance: baseline3070.9061ms
versus candidate3086.8201ms and dispatcher440.5617 versus473.4141ms fail the fixed gain/dispatcher
gates; lower boundary PSS/RSS pass. Original complete source/financial/native guards match and
both separate preparation processes end before their targets. No reverse pair, financial command,
current-product test-count increase or shipping adoption follows. Original source/tests restored;
[experimental evidence](quality/home-captured-index-projection.md) stays distinct from product acceptance.


D-553 accepts the previously uncovered emulator combination only: actual cold native goal/current-
review content taps held beneath original PIN cover, then one native PIN unlock each to original
ready destination with source equality and no financial posting/review completion. Cached=true/
adj900 and death precede each actual new process/SDK event. Null covered source match means unread;
the observer never forces an artificial passing destination. One new fictitious fixture/PIN is
retired after both receipts; Main896e/all120/identities/OS/config persist. Two native outcomes are
not managed tests or actual owner/device-authentication/physical-phone acceptance.
[Evidence and remaining gates](quality/pin-covered-cold-notification-routes.md).


D-554 qualifies only the new official runtime14 decoder component:31 affected existing cases
on an actual14 host plus complete nonempty original codec/malformed scalar fallback/cache recovery/
native Home on actual API24/x64/4096 and API35/x64 simulated16384. Full shipping and independent
Core/Data/Brotli input equality/native official14 identity pass; all BCL-byte equality is explicitly
not claimed because each own linked input differs. Fresh2062 total fixture rows are not managed
cases. Both new sources/AVDs are retired and prior context restored. Physical no-reset adoption,
whole stability/performance/A11Y/platform/service/authentication/release remain open.
[Actual receipts and refusals](quality/runtime14-brotli-security-servicing.md).

D-556 qualifies the changed local service runtime: strict images, identical original published
software, one new13-to14 original HTTPS/encrypted-confirmation cohort, then exact-config/private-
snapshot guarded retained API adoption. Actual14/health/HTTPS200 and unchanged original mounts/
other containers pass. No financial action, unchanged suite or phone cohort repeats; no managed-
count increase. Preserve original failed wait/lint/observer/cleanup lookups.
[Evidence and limits](quality/service-runtime14-exact-application.md).

D-557 completes retained ordinary emulator Main no-reset runtime14 adoption using already
qualified full signed50f5. Original UID/CEDE/permissions/all120 Home states/zero balance/OS/config
and compatible caller remain exact; three owned XMLs removed and AVD actually stopped. Preserve
boot/Preparing refusals without relaunch, financial or accepted native-cohort replay. No new
managed case. [Bounded evidence](quality/runtime14-retained-emulator-adoption.md).

D-558: twelve current runtime14 original large-Home publications pass six en/fa/de source contexts
with actual financial/runtime image identity and original complete raw/native packets. Tenfold
1511-1818ms is finite emulator evidence only. Six new real SQLite schema-probe/repair cases pass;
the actual original pending probe refuses before upgrading. Preserve the host's incorrect old
probe and its reference metadata mutation; authenticated full logical recovery/four protected
source audits pass without repeating Home or financial actions. Final original Main/OS/caller
restoration and owned cleanup pass; final code restoration does not imply a later workload key
audit. [Exact lineage, errors and limits](quality/runtime14-retained-large-home.md).

D-559: twelve complete six-language Offline/CloudBackupOnly listing packets pass focused length,
exact original receipt-limit and connected-provider disclosure checks. This is documentation
validation, not twelve managed tests or app/Store acceptance. No accepted native/financial scene,
build or full suite repeats. [Counts and remaining gates](quality/store-draft-factual-boundaries.md).

D-555 final original Samsung observation passes after the existing one-time50f5 upgrade:
Main18619/Home979/all141 public states, identity/storage/permissions/OS/caller and ready top-right
Transactions Add/header remain exact. Return Home and three owned XML removals pass. No native
matrix/financial/build/test replay or new managed-count claim. D-560 records later owner-scoped
foreground/install-prompt autonomy, not a newly exercised security or authentication acceptance.
[Evidence and open gates](quality/physical-runtime14-final-adoption.md).

D-561: four original physical runtime14 contexts in28422 plus only two resumed contexts in31321
complete twelve publications with full original raw/native/financial/byte controls. Preserve actual
SGen-worker SIGSEGV; no general stability or two-second acceptance. Nine new bounded diagnostic
reader cases pass, while native exit-only Activity accurately returns own EXIT_SELF1/status255
and no native trace acceptance. Original caller/OS and final Main18994/Home979/all141 restoration
pass; old18619 OS reason3 is explicit, no private Main equality claim. No successful finance or
native context replay. [Actual source/process lineage](quality/physical-runtime14-large-home-and-native-exit.md).

D-562: repair the actual CI writer defect with an additive duplicate-marker guard migration,
preserving original financial operations and exact source comparison. All83 original failures
pass locally;18 genuinely new managed cases cover conflict policies and narrow restore/workload
oracles. Strict builds and one new Samsung protected-store delete/receipt-Undo cohort pass;
complete signed8fd5 is adopted without reset in Main7028/Home979/all141 with original settings
and caller preserved. Hosted latest-head CI remains pending until its actual result; broader
stability/performance/accessibility/platform/provider/Store gates remain. [D-562 evidence](quality/ci-ledger-invalidation-and-exact-restore.md)

D-562 hosted follow-through: all three required jobs of CI40316474338 pass on785b151b;
iOS is skipped. Separate native-cipher Windows workload comparison still fails and is reproduced
by one new targeted compaction case. Its narrow source-cookie-bound correction passes five new
and11 affected comparison cases; original workload builds strict with zero warnings/errors.
No product/phone financial/build replay; corrected hosted native workload acceptance remains
pending. D-562 now has23 genuinely new managed cases across its two relevant corrections, without
a full-suite count or wider stability/platform acceptance claim.

D-562 final hosted acceptance on5e2e9384: required CI40319587370, Native cipher candidates
40319587614 and CodeQL40319587303 all complete successfully. The corrected Windows protected
workload now passes on the actual hosted job; earlier native producer/persistence and Android
checks remain successful. Both one-shot monitors are consumed/deleted. No failure is hidden by
rerunning an unchanged job. General crash/ANR, two-second performance, accessibility, broader
platform/provider/Store and release gates remain open.
[Required CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40319587370),
[native qualification](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40319587614).


### D-563 - Bounded independent native fault observation

Add an opt-in independent-package observer for reviewed module-relative instruction locations,
with original signal chaining, bounded private records and mandatory independent fatal correlation.
Fifteen new actual-reader cases and fresh refusal/recovery/fatal/concurrency controls on x64 and
ARM64 pass. One changed runtime14/x64/API36/4096 retained-source cohort arms successfully and
preserves all six native contexts/twelve publications, original encrypted sources and complete
financial presentation without seed/key/financial replay. The actual named record is empty and
the process lives: no random crash correction or general stability acceptance is claimed.
The original-code handback guard refuses nonempty independent Main before schema retirement;
keep the compatible new caller stopped with original UID/storage/permissions and old APK backup.
Ordinary emulator Main3035/50f5 and phone Main7028/8fd5 are preserved. Standard owned build cleanup
and exact-head CI are separate recorded gates. Shipping code/APK, permissions, UI and GC settings
are unchanged. QA-06, phone two-second, accessibility/platform/security/iOS/provider/Store/release
remain open. [Full scope and actual failures](quality/bounded-native-fault-code-observation.md).


### D-564 - Ordinary emulator ledger-guard adoption and owned code cleanup

Accept existing signed shipping8fd5 once without reset on ordinary emulator Main3035to6123,
with original all120 public attributes/zero balance, UID/CE/DE/permissions, size/density and five
settings exact. Preserve actual storage refusals. Retire only the stopped finished Billing
recovery caller's unused code after hash/certificate-verified APK backup, retaining its UID/data/
keys/stored grants; do not touch the live historical snapshot caller. Restore compatible D563
ebd9 stopped with original UID/storage/permissions, and hash-clean two new XMLs. The overly broad
uninstalled-permission-declaration observer refusal is preserved and corrected without reinstall.
No phone, private Main database/key, financial command, old successful suite or build is repeated.
No AVD resize, data clear or global cache purge occurs. This completes the distinct ordinary
emulator changed-APK gate; QA-06/A11Y/platform/iOS/provider/Store/release remain open.
[Full boundary and failures](quality/ordinary-emulator-ledger-guard-adoption.md).


### D-565 - Physical ARM64 bounded observer cohort and original handback

Qualify the previously unperformed managed ARM64 observer boundary with one original retained-source
cohort, no new financial/preparation/key commands. Strict complete signed dadba5/192AOT/205ELF and
actual financial/runtime identity pass; all6 native contexts/12 publications and full source/native
presentation guards pass in5178. The named record is empty and that process lives: random SGen
crash/root cause remains unproved. Tenfold median3512.0725ms still exceeds the two-second target.
Only the validated empty independent Main retires M1/M2/M3; original b1d5/UID/storage/permissions
return through the same installer with the owner-authorized scoped Don't send option. Preserve
German-onboarding observer refusal and restore English once in the same7667 without Next/financial
work. Main7028/Home979/all141/both APKs/OS exactly return,5 XMLs hash-clean, and phone is handed back.
One standard clean removes11586 files/637786410 logical bytes;4 unused unsigned copies remove
117540593 bytes, used packages/proofs retained. D563 one-shot CodeQL passes/CI cancels; D564 CI and
CodeQL pass, both schedules consumed/deleted. No old managed/native/financial success is replayed.
QA-06/A11Y/security/platform/iOS/provider/Store/release remain open.
[Full bounded evidence](quality/physical-bounded-fault-observer.md).


### D-566 - Original receipt-source Help at actual OS200% and bounded startup diagnosis

Qualify original native Help/OK/Cancel in en/fa/de, Light/Dark, exact360/412/680dp on API36/x64/4096.
Preserve18 original320/412/640 scenes; continue only12 missing exact widths and skip all6 accepted412.
All18 required exact contexts pass final-line reachability, original44dp actions and Persian RTL,
with zero source/camera/file/financial commands. Correct only the observer's29px coordinate mismatch.
The original covered startup failure is actual insufficient space below the unchanged553648128-byte
protected guard; after backed-up finished-code-only KEEP_DATA retirement, one same-process Retry
passes. Optional content-free independent startup diagnostics are compiler-elided from shipping;
five new managed cases and two affected existing cases pass. Default Windows/Android strict and
complete signed independent f288bc/192AOT/204ELF pass with actual financial/native identity to8fd5.
Restore font/display/five settings, Main2902/all120/UID/storage/grants/8fd5 and stopped ebd9 exactly;
remove only the fresh fixture,7 hash-matched XMLs, shut down only the owned AVD. Standard clean
removes24947 files/2306666041 logical bytes;8 unused copies remove238366227 bytes, used evidence kept.
D565 CI/CodeQL are green and its schedule consumed/deleted. Spoken/physical A11Y, QA-06, speed,
security/platform/iOS/provider/Store/release remain open; no old financial success is replayed.
[Full boundaries and preserved failures](quality/receipt-source-os-text-and-covered-startup.md).


### D-569 - Preserve Java resources during incremental Android APK packaging

Reproduce the installed36.1.69 BuildArchive defect directly: unchanged JAR resources/services
are present on the first call and missing on the second. Refresh only the validated generated
intermediate application APK before packaging; key on AndroidApplication, since the real SDK
changes application OutputType to Library. Preserve the initial complete-app admission refusal.
Three guarded resource/changed-and-removed-input cases, nine authority cases and one redirected
path refusal pass. Strict Windows and corrected complete offline Android builds have zero
warnings/errors;384 own AOT/408 ELF, static16KiB/ZIP/v2/v3 and actual native/financial identity
checks pass. All1422 Java resource bytes match the retained healthy role, including both coroutine
service descriptors. No application unit-test count, financial action, phone/emulator operation,
visible UI or general crash correction is claimed. D568 native timing/adoption and the D567 real
reboot/idle cohort remain unfinished; Loyan retains the physical lease. Preserve the broad goal.
[Full defect, correction, refusals and acceptance boundaries](quality/android-archive-resource-retention.md).


### D-571 - Preserve the complete permitted StarMoney field at CSV syntax boundaries

Correct original preflight counting so separators and CSV quote syntax do not consume the existing
4096-character decoded-field allowance. Preserve all original size/row/column bounds, strict syntax,
source identity and explicit financial review. Five corrected boundary shapes fail before the fix;
all10 boundary cases, two original non-UI application/real-SQLite review flows and five affected
existing structure refusals pass on runtime14. These are12 new cases plus five existing cases;
incorrect initial exception-type assumptions are retained separately and are not product failures.
Strict Windows/complete offline Android builds have zero warnings/errors;384 own AOT/408 ELF,
static16KiB/ZIP/v2/v3 and1422 exact Java resources pass. No Import/Undo, financial/native replay,
phone command or D567 emulator input occurs. The combined review APK contains unfinished D568;
refresh its matched financial inputs before physical comparison after explicit Loyan handback.
Read only the historical D561 failed job: all83 cases/35 families exactly match the already fixed
D562 packet; preserved selected passes and hosted green evidence avoid an unnecessary rerun.
No current-head CI success is inferred. Three original bank sources clarify remaining contract
admission, without a new preset, authentication, provider activation or support claim. The broad
goal and D567/D568, native adoption, stability/speed/A11Y/platform/iOS/Store/release gates stay open.
[Full corrected baseline, checks and source-admission limits](quality/starmoney-decoded-field-boundary.md).


### D-568 - Withdraw the prepared Home publication after failed physical gates

After explicit authorized Loyan handback, preserve the fresh owned Main3130 baseline and original
retained100000-row source. Same-day original/candidate ARM64 processes5215/6816 use separate completed
scalar controls4471/5980 and complete source/native/financial/byte guards. Original3015.6854ms becomes
3284.7178ms (-8.9211% gain); dispatcher441.1000 becomes474.6848ms, failing the1.05-relative gate.
PSS/RSS growth14397/14200KiB stays inside65536, but both main gates fail. No opposite-order/rescue
pair or candidate Main adoption occurs. Restore six exact original files; remove only four new
unpublished source/test files with private copies retained. Thirteen experimental cases are
withdrawn with the API and add0 final shipping tests; successful financial/native suites are not replayed.
Retire only validated empty independent Main bookkeeping M1/M2/M3 with full original source proof,
restore b1d5 in the same owned installer using authorized Don't send, and verify original61-node
English startup before stopping that caller. Strict restored Windows/full Android have0 warnings/
errors; complete signed4a6cb5be/384AOT/408ELF/1422 resources passes offline/ZIP/v2/v3/static16KiB.
Main upgrades once without reset3130to9856, with exact UID/CE/DE/permissions, all141 Home attributes/
Home979 and all three original profile-page states. CSV D571 stays; no clock/system setting/financial
command is changed. Pair26October and final adoption27October are explicit.15 owned XMLs hash-clean;
12769 final-build outputs/1098816790 logical bytes and two unused copies/97644359bytes are removed.
A prior574552517-byte residual deletion remains automatically rejected/unperformed; no bypass.
D567's real idle/reboot cohort is untouched. Whole-Home two seconds, general stability/audible/device/
iOS/provider/Store/release gates remain open. This closes the experiment, not the overall goal.
[Exact failed gates, preserved refusals and restored delivery](quality/home-prepared-publication.md).


### D-572 - Scope the first original physical reader gesture cohort

Use a distinct Samsung/API36/ARM64/current-Main-UID/APK-bound service; preserve the old emulator-only
helper and D567 idle/reboot cohort.24 new actual-policy AT-572 cases and strict complete offline
37AOT/47ELF/v3 packaging pass. With direct contextual owner authority and finite restoration,
English/Persian/German actual accessibility focus opens the original expense form and actual Cancel
returns Home, three opens/three Cancels and zero financial/profile/seed/key commands. Continue only
unfinished Cancels after preserved IME/host-observer refusals; no accepted activation is replayed.
Bounded local playback is retained without upload/transcription or claimed intelligibility.
Restore original system-setting values and absent-key presence on every connection; production
APK/runtime/source, original caller and financial data stay unchanged. Remaining spoken/field/
scroll/theme/width/full-workflow, stability/speed/platform/provider/Store/release gates stay open.
[Complete scope, refusals and actual evidence](quality/physical-reader-scope.md).


### D-573 - Local acoustic evidence without replaying accepted native gestures

Decode the five existing D572 playback WAVs locally with explicit original model paths and no
expected-caption grammar/prompt; verify original hashes and retain full hypotheses privately.
English expense/Cancel and German expense/Cancel/amount terms occur; Persian expected terms do
not. Only the two unresolved Persian clips receive a distinct English diagnostic pass, finding
role words without proving a cause. Three zero-PCM controls are empty. No transcript upload,
phone/emulator input, new voice/engine, application SDK/build/test/financial/gesture replay occurs.
This is machine acoustic evidence, not human intelligibility, pronunciation or complete A11Y.
Owner listening to the existing Persian clip remains pending while independent work continues.
The canonical A11Y-01 row now records D572's actual current-candidate native qualification.
Four redundant archives/155134714 logical bytes remain: automatic review rejects removal before
launch with no detailed reason; no alternate deletion or recovered-space claim follows. Used
runtime/models/notices, WAVs and proofs remain. D567 and the broader goal/gates stay active.
[Exact evidence, unresolved Persian boundary and refusal](quality/local-reader-acoustic-evidence.md).

### D-575 - Preserve omitted optional restore preferences

Correct the reproduced original protected-restore rejection of a package without optional
`display-settings.json`. Explicit nullable memory preserves missing choices; present empty/
malformed JSON still refuses, explicit null affects only allowlisted preferences, and device
security is excluded. Four new AT-575 behaviors pass across selected runs: one genuine failure
is corrected and only that failed case is rerun; the other three successful cases are retained.
Original native restore returns once after proving no earlier restore intent or accepted data;
complete copied rows match. Remaining observations preserve the original accepted restore and
all prepared source/cookie guards. Strict Windows/full offline Android have zero warnings/errors;
384 own AOT/408 ELF,1422 exact resources, ZIP/v2/v3/static16KiB and actual native/managed identity
are checked. Main receives the complete signed no-reset upgrade with all141 public Home
attributes/Home979, UID/CE/DE/permissions and exact system settings preserved. No ledger Save,
Import, Undo or old financial/seed sequence is replayed.

Generated project-relative output is excluded from source discovery after its removal was
rejected before execution;449 residual files/118359992 logical bytes remain, without bypass
or freed-space claim. D574's two SDK-native PID0 cold refusals and original prepared profile
remain retained; no accepted cold timing/two-second result is claimed. D567, spoken acceptance,
general stability/security, iOS/provider/Store/release and the broad goal remain open.
[Full correction, original native outcome and remaining boundaries](quality/optional-restore-preferences.md).


### D-574 - Original loaded cold boundary on retained Samsung sources

Completed the finite reference/tenfold cold cohort without repeating the accepted
source restores or financial/seed commands. The SDK's PID0 records remain refused.
A separately bound one-shot native shell-to-ready interval, including dispatch
overhead, measured8062.511917ms for10,000 entries and17756.488064ms for100,000;
one observation each, not OS-launch metrics, repeated medians or a two-second pass.
All prepared typed rows, financial/read/final cache cookies and original presentation
matched. The tenfold caller's mistaken old-reference presentation was preserved,
then corrected through an independent read-only receipt; no baseline was overwritten.
An ordinary covered startup failure required one native Retry in the same18144;
its unknown cause and the original refusal remain, not a general stability fix.

Five new provenance tests, the prior six lifetime/seven corrected storage cases,
strict caller builds and complete signed offline packages retain their distinct
evidence. Only two fresh profiles were deleted; original independent Main/source
and keys verified, necessary M1/M2/M3 bookkeeping retired before b1d5 restoration.
Main623b/11430/Home979/all141/UID/CEDE/permissions/settings preserved. No D567
emulator action, unrelated test/financial replay or production-schema downgrade.
General QA-06/two-second, ANR/crash, A11Y, platform/provider/Store/release remain open.

[Full source, failure, clock and retirement evidence](quality/loaded-cold-command-boundary.md).


### D-576 / D-577 - Isolated runtime feasibility and current retained Accounts

The original retained Accounts list was reproduced at97/0/97 after the accepted ledger
and Home had81/20/101. Route the actual retained Shell tab callback through the original
shared page read; preserve all calculations, writers, Back behavior and source rules.
Reuse the same four accepted rows: no account, money, seed, key or successful preparation
is repeated for the correction. Six native en/fa/de x Light/Dark contexts reuse one
Accounts instance with complete protected schema/header/typed-row fingerprints unchanged.
Six new AT-576 source-admission cases pass once; native contexts/commands are not unit tests.

The separate experimental .NET10 CoreCLR14/R2R package is never production adoption.
Preserve the observer's JNI readiness failure and other refusals; contain a refused
query without reading source or killing its process. Actual cached/adj900 death and
normal12938 return preserve all original source and101EUR. Only that fresh package is
removed; D567 is untouched. The product remains Mono/SGen, with strict Windows/full
Android zero-warning builds,384 own AOT/408 ELF,1422 exact resources and actual
Core/Data/compression/native engines equal to623b. Complete signed b577 is adopted
once without reset: Main12021/Home979/UID/CEDE/permissions/settings and original caller
are preserved. Relevant native navigation changes only one container drawing-order;
raw refusals/differences remain, without full-tree bitwise equality or private Main
DB/key claims.26,113 owned output files/9,306,774,621 logical bytes and104 owned remote
XML files are cleaned; used packages/proofs and earlier refused scopes remain.

General ANR/crash, speed/two-second, complete A11Y, platform/provider/Store/release and
the overall goal remain open. Category selected-state semantics are an observed
remaining A11Y issue. No unrelated successful suite or financial command is replayed.

[Runtime source, refusals and retirement](quality/coreclr-original-ui-feasibility.md).
[Original Accounts correction and actual Mono adoption](quality/retained-account-overview-navigation.md).

### D-578 - Original entry-category native state

Passed on the complete signed Mono candidate510a3197: en/fa/de x Light/Dark, each
Food selected, Housing exclusively selected, Food selected again and Food deselected.
All24 native captions and unchanged visible names pass. Original Cancel returns
without amount/title/Save; no financial preparation or accepted command is replayed.
Main22604/Home979/all141 public attributes, English/Light, original timeout/settings
and original caller metadata/process absence are restored. Covered-frame, observer
and incidental caller top-activity refusals remain. Managed test count unchanged;
intelligible TalkBack speech and the whole accessibility pass remain open.
[Full evidence and limits](quality/category-selection-semantics.md).

### D-579 - Original category state through the actual installed reader

The separate helper is freshly bound to signed Main510a3197/97,058,719 bytes and
original UID10380; production source/APK/runtime are unchanged. Eleven new actual
policy/resource cases pass in10+1 selected runs; old successful suites are retained.
Original English reader focus/gestures open one empty expense form, select Food,
select Housing, observe settled exclusivity, deselect Housing and Cancel without
financial Save. Preserve the old-length guard refusal, late IME, intermediate
two-selected native snapshot and failed initial recording; no accepted input is
replayed and no exclusive cache/product cause is asserted. Two complete local
recordings retain native provenance; one unconstrained post-decode observation
finds Housing/selected, without human or general spoken acceptance. All14 original
settings/presence and Main22604/Home979/all141 public attributes/caller are exact.
Only fresh UID10400 is retired;956 registered files and3 owned XMLs are cleaned.
Additional residual/loose deletions are rejected before execution and not retried.
D567, broader accessibility/stability/platform/external release gates and the goal
remain open. Native observations add no managed cases beyond the11 stated here.
[Complete finite reader cohort and limits](quality/original-category-reader-states.md).

### D-580 - Preserve distinct queued Android reminders

Five new actual source-preparation tests pass once. Distinct SDK-only and
original MAUI-bootstrap two-request cohorts each deliver2/2 after real reboot;
the causal four-request baseline delivers2/skips2 and corrected cohort delivers4/
skips0, with actual native queue history and exact financial/SDK Main inputs.
Twelve new scheduled requests total are native observations, not managed tests.
Original D567 review is3602055ms late and goal is skipped: original one-hour gate
FAIL remains, source match PASS and terminal retirement/restoration complete.
Strict Windows/full offline Android and one full signed Samsung no-reset adoption
pass, preserving Home979/all141 public states and14 settings. General punctuality,
Doze/OEM/cold/tap/reader/stability/hardware16KiB/iOS/external gates stay separate.
[Complete source, native evidence and limits](quality/reboot-paired-delivery-diagnosis.md).

### D-581 - Mandatory notification source admission under Python optimization

Two actual optimized-subprocess regressions first admit changed replacement/cache
inputs and fail. Replace removable Python assertions with explicit ValueError
rejection for every manifest/license/archive/path/source/cache check. All seven
related source guards pass (two new, five affected existing); one actual strict
Core/MSBuild preparation under PYTHONOPTIMIZE=1 passes without warnings/errors.
All SDK compilation bytes and used signed f7488d1d APK remain exact D580. Reuse
its full Windows/Android/native/adoption results; no app change, new package,
financial/native replay or phone/emulator command. Loyan retains the phone lease;
overall qualification and external gates/goal remain open.
[Complete reproduced defect and focused evidence](quality/notification-source-admission.md).

### D-582 - Retained ordinary API36 adoption of corrected notification Core

Reuse the verified complete signed f7488d1d product without rebuilding. On the
retained API36/x64/4096 ordinary Main, one no-reset upgrade preserves UID10217/
CE352809/DE82676/permission flags, all120 public Home attributes/zero balance,
caller ebd910c2 metadata/process absence, five settings and320x640/density160.
Actual Main3080 becomes3656; no historical PID equality, private database/key
comparison, financial command or new reminder is claimed. Two owned XMLs are
hash-cleaned and only emulator5570 is stopped, with original config hashes exact.
Current emulator Main is now f7488d1d; never downgrade to historical8fd5. Phone
stays with Dev Loyan1 until explicit return. No new managed tests, product builds
or financial/native-success replay; broad qualification/external gates stay open.
[Complete ordinary adoption and limits](quality/retained-notification-core-adoption.md).

### D-583 - Current corrected product bootstrap on isolated compatibility systems

Reuse complete signed f7488d1d without rebuilding for two fresh original-app
bootstrap cohorts. API24/kernel4096 UID10062/PID2745 reaches original Welcome/
Step1 with51 nodes; DE inode is unavailable, not invented. API35/userspace16384/
kernel4096 UID10207/PID3993 reaches original Welcome and exposes all six language
names after one owned body scroll; initial offscreen names are not a proven defect.
No language/Next/restore/profile/financial action or SDK request is sent. Preserve
metadata/tool/focus/label observer refusals without install or launch replay.
Both fresh Main fixtures are retired, original package absence/five settings/size/
density/config exact, four owned XMLs hash-cleaned and both owned AVDs stopped.
No new managed cases or full product build; nonempty data/full onboarding/hardware
16KiB/punctuality/stability/A11Y/iOS/external release gates and goal remain open.
Phone remains exclusive to Dev Loyan1 until explicit return.
[Complete current-product bootstrap and limits](quality/corrected-core-platform-bootstrap.md).

### D-585 - Current-candidate localized reader states and correlated refusals

The scoped physical reader is rebound to the exact current f748 product after
explicit phone return. Three distinct new policy cases pass in selected runs;
preserve the actual candidate regression, wrong runner option and synthetic
request-length mistake separately. Scoped strict helper builds and signed offline
packages pass; Main is not rebuilt/reinstalled and no accepted financial command
is replayed. Fixed refusal codes retain canonical request lineage without arbitrary
exception text, verified natively by a stale-window refusal before any gesture.
German/Light initial/selected/exclusive/deselected/Cancel states pass. Preserve the
Persian intermediate selection loss and generic refusal; the remaining retained
draft and later focused exclusive-transition diagnostic have separate receipts.
The later Cancel opens the expected dirty-draft dialog; preserve the wrong host
Home expectation and use only original owned Discard for cleanup, not reader
confirmation acceptance. The owner's historical recording feedback is English,
not Persian; textual legibility does not accept speech. Original English/Light,
Main/Home/settings identity, helper retirement and cleanup have independent proofs.
General speech/pronunciation and the unexplained earlier loss, stability/speed,
iOS/provider/Store/public release gates and the overall goal remain open.
[Complete native evidence, failures and limits](quality/current-candidate-localized-reader.md).

### D-586 - Distinguish public speech metadata from the actual Samsung reader

An independent no-permission/no-Activity/no-service signed package performs one
metadata request without voice selection, synthesis or financial commands. Public
Google reports473 voices, Persian unsupported/no Persian entry and English default;
actual approved TalkBackPID13984 instead binds private Samsung SMT. Do not confuse
these engine routes. The owner authorizes read-only system speech-language pages:
Samsung3.5.02.16 has14 currently exposed language/region entries, no Persian, and
Use system language selected with actual en-US. No language/voice/engine or download
entry is selected. Preserve API36 deprecation and copied-verifier refusals, plus
an empty-service-list restoration error corrected without reader reactivation.
Opening system settings normalizes an absent default-engine key; restore original
absence rather than claiming no side effect. Main18235/f748/Home979/all141/UIDCEDE/
permissions/EnglishLight, all14 values/presence and original caller are exact.
Only fresh inventoryUID10402 is retired. Standard Clean removes70 registered files/
12845517 logical bytes and nine hash-matched XMLs/262149 bytes. Additional retirement
of the new residual root is rejected before execution;88 files/5006367 bytes remain,
with no alternate deletion. No new managed-test count, shipping rebuild or financial
replay. Persian speech needs a supported engine/voice and separate actual-reader
qualification; current permission is read-only, not installation/selection approval.
General A11Y/stability/speed/iOS/provider/Store/public release and the goal stay open.
[Complete engine-route evidence, cleanup and limits](quality/installed-speech-engine-metadata.md).

### D-587 - Close the owner-cancelled legacy Persian engine candidate

Review the standalone F-Droid1.52.0/code22 APK and source against the pinned signed
repository index: exact digest/size/signer, no requested permissions, ARM64 and
bundled Persian data, minimum21/target33. Initial owner approval permits one
bounded temporary trial, but Android blocks the old target with Unsafe app blocked.
The owner declines Install anyway and requests a newer compatible option. The
original attempt is already terminal with VERIFICATION_FAILURE when cancellation
input is guarded; no stale tap, override, second install, engine/language choice,
synthesis or financial command occurs, and the package remains absent.
Preserve late Samsung default-key normalization rather than rewriting the baseline;
restore original absence with bounded stable observations and exact14-value/presence
checks. Actual Main22461 existed before the install attempt; OS metadata records
old18235 LOW_MEMORY/importance400/status0/trace null. Preserve this selected public
state recovery without claiming PID survival, crash cause or full DB/key equality.
Main f748/UIDCEDE/permissions/Home979/all141/EnglishLight and original caller are
exact. Three hash-matched remote XMLs/68374 logical bytes are removed; used failed
APK/source/provenance and local receipts remain. Host-only recorder review finds
three110-second captures terminated by a12-second final tail before their natural
limit; no audio is repaired, regenerated or replayed to fake acceptance.
Newer compatible review is authorized, but another install/selection needs fresh
approval. Persian speech and broader product/external gates stay open.
[Complete provenance, rejection and preservation](quality/legacy-persian-engine-candidate.md).

### D-588 - Correct the stale current-reader CI expectation

Both final D585/D586 hosted CI runs fail only the older current-category-candidate
method; native Windows/Android jobs and CodeQL pass. Preserve5576 total/one failure/
25 skipped, and delete the two consumed one-shot schedules. Current policy correctly
requires f748 while the old test expects510a. Reproduce only that exact method on
latest ef9d03bc, then update the independent current literal and explicitly reject
both prior510a/4a6c digests. One selected existing case changes from red to green;
no new case, policy relaxation, shipping change, financial test or successful native
build replay. Strict affected test-project build passes. Standard Clean removes393
registered outputs/164750580 logical bytes with no ownership outlier/remnant.
The single delayed exact-head check for a8c5db09 reports CI40538715325 and
CodeQL40538715374 completed/success; preserve the21.20-minute commit-to-observation
delay, consumed receipt and deleted schedule. Initial host DateTime-kind arithmetic
is corrected separately without changing the consumed marker or repeating GET.
The stale-test CI failure is closed; broader product/external gates remain open.
[Complete hosted cause and focused repair](quality/current-reader-ci-regression.md).


### D-589 - Review a genuine newer speech source without retrying the blocked APK

Read the actual Samsung download catalogue to a stable bottom:39 language/region
captions, no Persian/Farsi; no download/voice/engine selection. Return normally to
Main22461/f748/Home979/all141 exact public attributes, original14 values/presence
and caller state; preserve transient-focus/list/encoding/cleanup refusals without
replaying successful input. Ten owned hash-matched XMLs/310990 bytes are removed.
Official stable eSpeak remains1.52.0; pinned upstream ba90c8e9 has1.53.0/code23/
target36 and bundled Persian data, but no completed reviewed binary or runtime
acceptance. Inspect original source, manifest, extractor and official build metadata;
no source execution, build, install, voice selection, reader/financial/test replay.
Isolated SDK terms/scope await the owner; the prior legacy cancellation remains
binding. Persian speech and broader delivery gates stay open.
[Complete newer-source review and remaining approval](quality/newer-persian-speech-source-review.md).


### D-590 - Finish the remaining German capture without replaying accepted actions

Correct the actual host recorder-budget failure using monotonic start, the explicit
40-second natural limit and25-second finite margin under original window/watchdog
guards. One original German Home-title exploration gains actual reader focus;
there is no activation/form/category/financial replay or engine/voice choice.
The recorder exits0 naturally after43.3333 host seconds; original PCM16/stereo48k
WAV7577712 bytes has exact finite RIFF/chunks and39.4667 PCM seconds. The owner explicitly
confirms this exact title sound is German and understandable; this is not inferred
from focus, container validity or visible text and does not accept other utterances.
Main22461/f748/Home979/all141/UIDCEDE/permissions/EnglishLight, original14 settings/
presence and caller are exact; fresh helper10405 is removed once and16 owned XMLs/
675159 logical bytes hash-cleaned. No shipping rebuild or old successful test replay.
Persian and wider product/external gates remain open.
[Complete original recording and restoration](quality/natural-german-reader-recording.md).


### D-591 - Accept only the directly heard German title and retain the Persian gap

The owner explicitly confirms the original D590 sound is German and understandable;
bind acceptance to WAVf061e241/7577712 bytes and only Neue Ausgabe. Preserve the
negative historical Persian listening result and broader speech gates. Host-only
inspection of17 existing Persian packets shows no Housing activation, an
uncorrelated refusal and Food-selected focus records5523/8575ms after Housing focus.
This does not prove immediate clearing or the later managed-state/cached-caption
cause. Original toggle/theme/kind semantics stay unchanged; no speculative fix,
new Android command, gesture, recording, test/build or financial replay occurs.
[Direct acceptance and original packet limits](quality/direct-german-listening-and-fa-lineage.md).


### D-592 - Reject tag-list reuse before native work when the host gate fails

Freeze/link actual original codec/domain sources in two fresh isolated host roles;
only the private candidate reuses the entry's already-owned tag list. Strict builds
and full original-field/order/null/duplicate/mutability guards pass on actual14/X64.
One paired empty/mixed comparison allocates5047904/5383776 bytes for reference and
4785760/5159520 for candidate:combined4.6627% fails the predeclared5% host gate.
Close it without production change, app packaging, native/financial/seed/key input
or successful unrelated test replay. Shipping/model/migration inputs stay exact.
Standard Clean removes28files/593170 logical bytes; further residual-file removal
is rejected before process creation with blocked by policy,20files/422324bytes
remain and no alternate deletion runs. Complete Home speed and broader gates stay
open; no host result is a native/two-second acceptance.
[Complete host eligibility result and cleanup boundary](quality/rejected-tag-list-allocation.md).


### D-593 - Bound the current read-only primary-tab reader cohort

Add dedicated primary-tab operations and copied native Selected metadata only to
the independent helper. Full five-peer/native-role/viewport/unique-selection guards
reject financial-caption aliases; preserve the Italian Cancel/Undo collision and
block ambiguous legacy captions.17 distinct new selected cases plus six affected
existing category-resource cases pass. First native peer-clickability assumption
refuses before a tab gesture; reproduce its failure, allow only the selected peer's
missing redundant action, upgrade the fresh helper once and continue only the four
unperformed Transactions/Plans/Insights/Home steps. Actual focus/selected/title
observations pass; the accepted initial negative control is not replayed or rebound.
Natural original recording exits0; human acceptance of the new clip is not claimed.
Owner English Housing/selected acceptance applies only to historical clip8fed9094.
Main22461/f748/Home979/UIDCEDE/permissions/EnglishLight, original14 settings/presence
and caller match; strict141-node comparison preserves one drawing-order1-to2 delta,
not byte equality. Retire only fresh10406, hash-clean3XML/155778bytes and standard
Clean767files/220472438logical bytes. Extra build-root removal is rejected before
execution;422files/18765916bytes remain without alternate deletion. Product code,
financial writes/keys/voice choices and unrelated successful tests are not changed.
[Complete current cohort, refusals and preservation](quality/read-only-primary-tab-reader.md).


### D-594 - Put further TalkBack last and park Persian speech preparation

The owner considers current TalkBack testing sufficient for now. Finish only the
active cohort's terminal restoration, owned cleanup and documentation. Further
TalkBack tests/recordings/listening requests follow all other available backlog
work; Persian spoken TalkBack has no current priority given no present Iran rollout
plan and limited anticipated Persian-language usage in Europe. Park the proposed
alternate-engine/SDK work without assuming approval or chasing its question. Keep
existing Persian UI/features and completed evidence; this is an execution-order
deferral, not full accessibility/release acceptance or a change to jurisdictions,
financial behavior, accounts or publishing. Other work and the overall goal continue.


### D-596 - Retire camera originals left by an interrupted capture

Two original-filesystem regressions reproduce a gap in startup/data-deletion cleanup:
raw GUID JPEG camera outputs survive when process termination skips `finally`.
The actual shared cleanup now retires only that adapter's top-level opaque JPEGs,
without following links or deleting unrelated/nested/root files. Four new focused
cases pass after the two initial failures. Original Android startup in a fresh UID
removes one staged synthetic original after proven cached-process death; no actual
camera/OCR/financial scenario is replayed. Strict Windows/full signed Android pass.
One no-reset emulator upgrade preserves all120 public tuples; one Samsung upgrade
preserves Home979/141 public captions, geometry and states/UIDCEDE/permissions and
14 settings/presence, with an explicit ScrollView drawing-order2-to1 delta, not full
attribute-byte equality. Used signed885f6df5 and independent7781748a remain;4906 standard
Clean outputs/1523573710bytes and four unused copies/157450351bytes are removed.
Further eight-root retirement is rejected before execution with blocked by policy;
4030files/688445756bytes remain without an alternate attempt. Camera quality, wider
platform/release gates and the full goal remain open; D-594 scheduling is unchanged.
[Original cleanup, native preservation and exact limits](quality/camera-original-cache-retirement.md).


### D-597 - Autonomous emulator work and original camera-result qualification

The owner explicitly authorizes necessary project work in the Zanance-owned emulator
without repeated permission, and necessary low-risk real-phone actions autonomously;
high-risk phone actions still require the owner. Preserve other leases, data and
accepted checkpoints. The original receipt source/camera/provider/preparation/reader
adapters complete one real Camera2 cancellation and one shutter/Done on an owned
synthetic image in the same2222/UID10218. Native results0/-1 and cache-zero outcomes
are preserved. Found14.9/TryMinor(EUR)1490 is qualified without any financial command;
header/footer text loss remains explicit, not whole-camera/OCR acceptance. Reuse the
prepared signedd8b63158 once after approval, without reinstall or old scene replay.
Retire only the fresh helper; original Main3042/885f6df5/all120 public attributes,
UIDCEDE/permissions, five settings/config and caller match; the phone is untouched.
No default app change/build or new managed count: three build-admission checks and
two new native cases are distinct.1263 registered outputs/296045935bytes and two
unused copies/59824542bytes are removed. Further native bin/obj retirement is rejected
before execution;966files/130925594bytes remain with no alternate deletion. Physical
camera/corpus, merchant/text, remaining platform/release gates and the full goal stay
open. D-594 remains in force.
[Complete native boundary and preservation](quality/emulator-camera-acquisition.md).

### D-598 - Isolate fixed camera-input clipping from native text reading

The unchanged original synthetic PNG reads its complete header/footer through the
original native reader. One fresh quality-only Camera2 source/Shutter/Done, without
old cancellation or financial replay, produces a visibly right-clipped960x1280
frame. Recognition and stored-copy reads lose exactly the same text; missing letters
are not invented and no product OCR/financial change is made. Complete camera/merchant
quality remains open. Strict independent signed74460cf2/192AOT/204ELF/static16KiB and
three identity-admission outcomes are qualified; no new managed test count or default
app rebuild. Only fresh10218 is retired; Main2893/885f6df5/all120 public dictionaries,
UIDCEDE/actual permissions/caller/settings/config match. Volatile permission-object
headers caused the preserved first host refusal; raw dump equality is not claimed.
Five XMLs/110805bytes,1264 standard Clean outputs/296342657bytes and two unused
copies/59881004bytes are removed. Residual bin/obj removal is rejected before execution;
966files/130952293bytes remain without retry. Owned AVD is stopped; no phone action,
private Main DB/key read or financial write occurs. D-597's one-use hosted response
reports CI and CodeQL success and its monitor is deleted. The full goal and D-594
execution order remain active.[Complete fixed-input text boundary](quality/emulator-camera-text-boundary.md).

### D-599 - Bound original command and reader timing before another Home candidate

One new original100k English publication on the owned API36/x64/4096 emulator records
527 fixed typed EF events,109 inside its original publication window, with zero drops.
Whole1664.9301ms, command intervals9.1219ms and reader lifetimes433.7934ms are distinct
instrumented boundaries, not exclusive CPU costs or a phone/cold/two-second pass.
Eleven compilation starts/plans occur outside the window and none inside. Original
full financial/native/schema/typed-row/source/ciphertext controls pass; separate scalar
oracle4086 is proven dead before target4279. There is no financial/seed/key replay.
A refused provisional package is never installed; final53725d08 has exact adopted
Core/Data/Brotli PE/native-engine identity,192AOT/204ELF/static16KiB and strict build
proof. The observer records no SQL/parameters/payload strings/keys/native addresses.
Restore actual ebd9/114 German public dictionaries and stopped flag, Main2942/885f6df5/
all120/settings/config; no phone input. Twelve XMLs/484581bytes,1263 Clean outputs/
296011853bytes and five unused copies/178872682bytes are removed. Further bin/obj
retirement is rejected before execution;967files/130910659bytes remain without retry.
Protected comparison sources stay bound for continuation; no shared alias is retired.
No new managed tests or default app change/build; the full goal and D-594 stay active.[Complete original timing boundary](quality/home-query-reader-timing.md).

### D-601 - Withdraw ledger text reuse and recover the original retirement report

Bounded immutable Note/Payee reuse passes a four-shape host prerequisite and two owned
emulator pairs, but the physical first pair is only 3129.0962 to 3120.7219ms (0.2676%),
below the frozen 5% gate. Withdraw all product changes and five experimental cases;
final new managed count is zero. The 69 selected executions include those five cases,
not a larger retained suite. No phone reverse/rescue, shipping build or Main upgrade.
Reference Core/Data shipping-byte identity is not reproduced; pair checks cannot replace
D-543, and no shipping-equivalent performance acceptance is claimed.

Fix the actual read-only observer's missing retirement phase, require its Comparison,
and select only an already listening explicit ADB server (default 5038). Recover original
PID11081's accepted report without repeating retirement; three bounded observer outcomes
pass. Full empty independent-Main schema proof precedes original B1D5 restoration.
Main24382/885f6df5/Home979/all141, caller actual English onboarding/all61/UIDCEDE/flags/
absence/stopped=false and 14 settings/presence match; emulator/Main/caller/config also
restore exactly. No financial/seed/key replay or private shipping Main DB/key read.
Standard Clean removes 9148 registered files/2253169797 logical bytes; 18 new XMLs/
714460bytes retire. Three further removal scopes are rejected before execution with
`blocked by policy`; 6376 files/1413241028bytes remain without retry or alternate deletion.
Protected evidence/shared aliases remain. The overall goal and D-594 execution order stay
active; two-second, stability, security, remaining accessibility and external gates stay open.

[Complete candidate rejection, input limits and observer repair](quality/withdrawn-ledger-text-reuse.md).


### D-603 - Observe live Android backup eligibility without a payload request

The fixed shell-only observer binds the original owned package UIDs before typed
BackupManager status/eligibility queries. API36 emulator returns Main10217=false,
caller10221=false and Settings-provider1000=true while backup is disabled. Samsung
returns Main10380=false/caller10381=false while its original backup-enabled state
remains true. These are five real framework replies, not actual OEM transfer or
cloud-payload exclusion. A wrong expected UID refuses before service binding; errors
never substitute false. Strict corrected Java/D8 compilation passes; the initial
module-path warning refusal remains. No product code/dependency/permission change,
managed test addition, app rebuild, backup/restore, authentication or financial replay.

Both original APK/UID/CEDE/flags/stopped/PID states, eleven settings, boot/backup state
and display context remain unchanged. Two new hash-bound remote JARs retire with
6236 logical bytes removed; local used build witnesses remain. The owned AVD stops
with exact configuration preserved; both ADB servers and phone context remain.
SEC-07 actual OEM/device-transfer and iOS gates stay open. Overall delivery remains
active; further TalkBack stays last and Persian speech parked under D-594.

[Complete native evidence and limitations](quality/android-live-backup-eligibility.md).

### D-604 - Align current optional-service and backup disclosure inventories

Correct the obsolete absent-feature, active Android OS-backup and cloud-only data-flow
claims against current source. Personal service-readable sync, household payload E2EE,
aggregate-analysis prerequisites, explicitly fetched ECB references and inactive real
purchases retain distinct boundaries. Both app/starter flow rows agree; old SDK/build
versions remain explicitly historical. Submission answers and the public policy stay
drafts requiring the actual selected artifact/processor/platform acceptance. No product
change, new managed test, build, native/financial replay, publication or account action.
[Source-backed corrections and document verification](quality/current-privacy-inventory-alignment.md).

### D-605 - Preserve queue reconstruction across null saved snooze slots

Five fresh regressions reproduce valid-JSON null slots aborting Core/actual SQLite
reminder publication. Filter only null records before the original queue cap; retain
all fields, original source/privacy validation and deadlines. Five new plus one affected
existing managed case pass; no full-suite or successful financial/native replay. Strict
Windows and complete signed Offline384AOT/408ELF product checks pass. One fresh API36/x64
native cohort publishes original plan+snooze after20 nulls, preserves the complete typed
fictitious source and zero ledger, then removes both requests and only its new fixture.

Preserve real startup/storage/AOT/observer refusals; never weaken the staging-space
bound. Restore exact prior stopped portable code with KEEP_DATA ownership/flags unchanged.
Samsung no-reset adoption24382/885f6df5 to17668/1f9816c4 preserves all141 original public
attributes/Home979 and package/settings guards; the first51-node loading observation
continues only in the same process. Original emulated Main/zero/all120 is restored and
its AVD stops. Eight XMLs/207653bytes and3175 registered Clean outputs/1041974987logical
bytes retire; used evidence remains. Delivery, broad stability/security/OEM/other platform
and release gates stay open; TalkBack remains last. [Complete evidence](quality/null-snooze-cache-reconstruction.md).

### D-606 - AT-198/AT-222 covered storage refusal

One actual SQLite image reaches the original space guard one byte below its bound,
with unchanged bytes/no key/candidate effect. Six original-VM cases preserve covered
Retry/recovery classification and retained-command exclusion; four affected old recovery
cases pass. Three original failures are preserved. Eighteen synthetic native layouts
at OSfont2.0 and360/412/1024 validate full captions and Retry targets in en/fa/de,
light/dark. These18 observations are not unit tests or actual disk-full/authentication
acceptance. [Full limits](quality/startup-storage-guidance.md).

### D-609 - Existing camera-frame quality boundary

Six original ink-box pairs agree with a4/3 height mapping; header/footer endings fall
outside the stored width. This is retained-image analysis, not a new native/managed
test or full-text pass. Do not repeat accepted shutter/Save work or repair the input
to relabel D-598. [Exact witnesses and limits](quality/original-camera-frame-geometry.md).

### D-610 - Finite direct-output controls only

Both private roles preserve all32 fields/order, independent mutable tags, cancellation,
410 truncated rows and102 mutations plus wrong-bucket outcomes. Allocation eligibility
fails; these controls do not establish SQLite fallback, native publication or a new
shipped managed-test count.
[Scope, values and refusal](quality/rejected-direct-ledger-output.md).

### D-611 - AT-82 empty derived identities and complete source fallback

Baseline6/6 fails; corrected9/9 passes include first/second row, buffered/unbuffered and
actual SQLite no-partial/no-repair cases. One API36/x64/4096 original Home observes
blocks1/scalar1 and90EUR with complete typed schema/row preservation after the existing
cache mutation. Accepted two-row setup/cache damage are not repeated; the full baseline
begins after the read-only observer, not before the initial helper exception.
[Evidence and exact limits](quality/derived-ledger-empty-identity.md).

### D-612 - AT-82 unsupported optional column contracts

Fourteen new original-SQLite cases cover seven read faults and seven valid account
saves; three affected original cases pass. Preserve closed-connection and stale-build
harness failures. Native Home publishes90EUR with blocks0/scalar1 and full typed equality.
[Evidence and exact limits](quality/unsupported-derived-ledger-schema.md).

### D-613 - AT-171 actual camera source bound after length admission

Two original filesystem growth cases fail; six new selected cases pass after reusing
the unchanged stream bound. One actual native JPEG/callback/empty-cache proof covers
normal integration only; no oversized native attack or camera whole-text claim.
[Evidence and exact limits](quality/bounded-camera-source-read.md).

### D-614 - QA-06 frozen first-pair codec rejection

Six affected existing codec cases pass once; no new shipped test/full-suite rerun.
Both roles match actual complete-product Core/Data/compression inputs and original
financial/native/source controls. Native1908.2973 to2156.9117ms and dispatcher ratio
1.13058 reject quality0; original protected-source/schema-cookie guards are not weakened.
The diagnostic cache-cookie correction is declared only on verified fictitious copies.
Two-second, physical, cold/search/migration and broader gates remain open.
[Evidence and exact limits](quality/rejected-ledger-brotli-quality.md).

### D-615 - QA-05 unchanged-frame merchant boundary

One new portrait Camera2 result fails complete framing; direct input returns all9
lines, camera paths only5. Seven original parser regressions fail;10 new plus45
selected related cases pass after the finite date-boundary correction. Same accepted
JPEG/native reader now yields null merchant and unchanged23.45EUR without another
shutter or financial command. Editor opening from fresh onboarding fails; no form,
whole-camera/corpus/physical acceptance is inferred. Strict builds pass.
[Evidence and exact limits](quality/portrait-camera-merchant-boundary.md).

### D-616 - QA-05 complete primary virtual-scene camera text

Actual preconditioned scene preview, one native callback and one original recognition
read qualify all9 primary rows, NORTH HARBOR MARKET and2345minorEUR inPID4868. Extra
rotated-poster text is genuinely imperfect and retained. No stored/direct reread,
financial action or new unit-test count. This finite source assertion closes; broader
physical/corpus, whole-scene OCR and original editor acceptance remain separate.
[Evidence and exact limits](quality/complete-virtual-scene-camera-source.md).

### D-617 - Original onboarding transition and receipt editor fields

Reproduce the account header/name being hidden by the prior preferences offset.
The unchanged controls in independent step ScrollViews pass the exact original case
and18 en/fa/de Light/Dark360/412/680 contexts. One initial zero account and two real
Home/picker/reader/editor visits show the admitted complete merchant versus empty
missing-header payee, both23.45EUR. Discard both; All/All types remains empty.
Retain renderer admission and observer failures without successful operation replay.
[Evidence and exact limits](quality/original-receipt-drafts-and-onboarding-scroll.md).

### D-618 - Independent original onboarding content at actual OS200%

18 final en/fa/de Light/Dark360/412/680 contexts prove a genuinely scrolled preferences
source, independent account top, reachable complete original title/Name and fixed
Back/Next/Start caption/target separation with actual RTL ordering. A clipped-bounds
apparent pass is explicitly disqualified, not counted. No account Start, financial,
receipt, TalkBack, build or managed-suite operation is replayed. Same PID4260 and
exact original settings restoration/fresh UID/AVD retirement are recorded.
[Evidence and exact limits](quality/onboarding-independent-scroll-os-text.md).

### D-619 - Parameter values do not establish actual transport inputs

Two .NET10.0.14/X64 encode-only processes preserve the original complete input digests.
All256 reference headers encode22; all256 candidate headers encode18 despite requesting16.
This rejects the proposed actual16 premise before decode/timing/native work.15 private
RFC header discriminator self-checks are not new product tests. No speed or memory
acceptance is inferred, no old successful suite or financial operation repeats.
[Evidence and exact limits](quality/requested-brotli-window-admission.md).

### D-621 - Full source-named receipt dates with original numeric precedence

13 of24 new cases fail on original code; all24 pass after the bounded fallback.
Keep9 passed merchant controls, preserve the historical null-Date failure, then run
only its intentional exact-date expectation update and selected original date cases.
Final unique union67 passes. One fresh PNG dated27October2025 reaches actual original
Month10/Day27/Year2025 fields without typing; amount710EURminor/merchant/account stay
intact. Cancel/Discard, Home0 and All/All-types empty pass; no financial posting.
[Evidence and exact limits](quality/complete-named-receipt-date-suggestions.md).

### D-622 - Admit the native predicate before classifying pending scrolls

A fresh API36/x64/4096 package observes two original public scroll requests against
actually attached/focused,never-laid-out0x0 native controls. Pending250ms then layout
release completes the first; retirement leaves the second incomplete250ms afterward.
The older hidden-root assertion is not retrospectively accepted. Original framework
revision and native outcomes are checked beyond a Success flag. Preserve four API
analyzer refusals and the wrong all-ScrollView terminal guard, then verify restoration
from its same saved native packet. No financial or old successful test replay.
[Evidence and exact limits](quality/connected-unlaid-scroll-lifetime.md).

### D-623 - Check the original helper before changing page scrolling

One native original helper call returns false while its actual target stays attached,
focused,unlaid0x0/layout-requested. Later parent release lays it out; no second helper
or original stock scroll request occurs. This does not prove another window-global
callback, broad safety or a historical ANR fix. Original source and final settings/
page/owned retirement are exact; no new managed test or financial replay is counted.
[Evidence and exact limits](quality/original-validation-layout-refusal.md).


### D-624 - Actual Android draft provenance and retained failures

Review all48 actual API36/x64/4096 frames and exact RGB serialization, retaining47 successful originals and replacing one unfinished IME frame. Confirm the same three ledger entries, two unpaid plans, three accounts, budget and goal after capture/recovery, with no entity Save replay. Actual PID4188 ANR/native Close/terminal proof/one normal launch to18878 remains a failure, not a stability pass. Visible OS/profile restoration and fresh retirement pass; no new managed test/build count.
[Evidence and exact limits](quality/android-store-drafts.md).


### D-627 - Original Accounts source, cover and row ownership

Thirty focused real-store/VM cases and21existing resource checks pass. Keep the
initial actual held-tab failure; correct Shell departure retirement and repeat only
that failed predicate after one same-fixture no-reset independent upgrade. Initial
cover/error/Retry, current Accounts row and shared Home row routes, six language/
theme contexts,360dp/OS200% covers and800dp view pass with one unchanged100EUR opening
and empty ledger. Original display/OS settings return;212owned XMLs and fresh UID/AVD
retire. Five standard cleans pass; supplemental3790file cleanup is policy-refused.
No successful financial command, old receipt suite or physical/D-607 action repeats.
[Full original inputs and exact limits](quality/covered-original-account-overview.md).

### D-628 - Complete original conversion review, consent, commit and feedback

Four real original failures become corrected passes;28distinct new selected cases
and two affected old conversion/21resource checks pass. Keep harness/filter refusals
and repeat only four affected feedback cases after their final change. Strict
Windows/full offline Android pass. One original Asset100EUR onboarding, Cancel and
one actual conversion produce one archived account/opening holding/valuation with
empty ledger; deliberate post-commit Changed and route failures retain typed receipt.
Six same-source accepted-page contexts pass. Font200% returns the root to Home and
is not retained-footer acceptance. Fourteen separate native compiled-content layouts
prove360/800dp plus German/Persian OS200% wrapping with zero DB/financial outcome.
Both fresh UID/AVD sets and80owned XMLs retire; two supplemental refusals remain.
No accepted financial, old suite or physical/D-607 operation is repeated.
[Full evidence and exact limits](quality/original-account-conversion-ownership.md).

### D-629 – Original account-detail source and lifetime

The four original failures (late read publication, retired Edit route, retired installment
confirmation actually posting, mixed-file earmark) now pass.29distinct new focused cases
include actual WAL/SQLite transaction, mutation/refusal, bounded retry, draft, ordering,
free-permission and accepted-writer feedback coverage;6affected existing language cases
pass separately. One fresh original native cohort checks loading/error/Retry, held-reader
retirement, actual100/25/75, original90/10loan split and a single deliberately stale native
Save rejected after original Home navigation. No new ledger entry is accepted. Ten named
display contexts are observations, not unit tests. Strict builds and exact owned cleanup/
restoration are in [the full report](quality/complete-original-account-detail.md); same-page
draft preservation is not OS-recreated-page persistence or physical/shipping acceptance.

### D-630 – Original repayment schedule source and publication

Four real original failures now pass.14distinct new focused cases include exact original
Loan/Lent calculator/row equality, page/query/display/day retirement, error/Retry, foreign
handoff refusal, shared read and actual worker/WAL transaction;6affected existing free-
policy language cases pass separately. Original native acquisition cover/error/Retry and
actual Home/retained More return preserve full source and zero ledger entries.11estimated
rows/58.98EUR interest, six language/theme contexts and complete last-row reachability
are bounded observations, not new unit tests or a payment. Strict builds/restoration and
cleanup limits are [recorded here](quality/complete-original-loan-schedule.md); broader
OS/device/accessibility/security/performance/iOS/provider/publication gates stay open.

### D-631 - Original reimbursement page

Nine focused source/worker cases pass; the original independent API36/x64/4096 page
shows one real original expense10EUR with reimbursable10, actual local row-command
refusal/Retry, held covered read/departure cancellation/retained return, valid original
detail navigation, six en/fa/de x Light/Dark contexts and a separate failed-read cover.
Complete named financial packet and restored settings remain equal. These are native
observations, not extra unit tests, a new OS200/width matrix or physical acceptance.
[Full provenance, failures and retirement](quality/complete-original-reimbursements.md).
D-607 and all broader release gates remain open independently.

### D-632 - Complete original plan detail

Twenty-two distinct new selected SQLite/action/transaction cases and strict builds
pass. Actual original native source covers loading/cancel/retained return, occurrence/
edit/final-bill routes without writes, one Pause with post-commit notification failure,
six receipt contexts, corrected local read failure/Retry retaining2November draft,
one linked Resume/End/Delete, full financial equality and zero ledger entries. Native
observations/resource checks are not extra unit cases. Density-stale observation is
disqualified; actual OS text size is unmeasured. Root replacement and failed dump are
preserved. [Complete evidence](quality/complete-original-plan-detail.md); phone/D-607
and general release gates remain open.

D-632 completion addendum: the separate bounded layout cohort completes12missing
360/680dp receipt contexts and18loading/18error-Retry contexts across360/412/680dp,
en/de/fa and both themes. Current-window XML/native line-end/ellipsis/height/bounds
checks agree, without old financial form commands. Preserve the failed copy/managed
exit, committed-copy observation failure and21actual audit-time differences; complete
new-source comparisons remain exact. No old-source/key identity or OS200% claim.
Additional fresh UID/AVD and179XMLs retire; total five standard cleans remove
2588206899logicalbytes. Two supplemental refusals leave698380075logicalbytes with no
retry/subset/alternate/move/escalation. See the full D-632 quality record; D-607/phone,
physical adoption and broad release gates remain separate.

## D-633 - Original Plans center admission

Ten new selected cases cover the actual original mixed-file review-count boundary,
source/work/metadata/detached mutation, first-file Retry and original SQLite WAL worker
transaction. Native evidence covers original source acquisition cancellation, actual
permission-read retirement, diagnostic retired original commands, valid original row/Add/
Review routes and corrected failure retention/explicit Retry. No accepted financial setup
or payment is replayed. All54changed ready/loading/error/Retry states, exact terminal source/settings/OS
restoration and fresh retirement pass, with their actual scope tracked in
[the quality record](quality/complete-original-plans-center.md); do not promote unfinished
or indirect observations to acceptance. Phone/D-607 and broader release gates remain open.

## D-634 - Original template-center admission

Preserve original native old-row confirmation over Home and two real action/writer
regressions before correction. Nine new focused cases admit worker/WAL, first-file
Retry, complete-source mutation, retired confirmation, rollback and known commit.
Actual original native Home departure refuses the old command; held read cancellation,
retained return, error/Reload and72changed caption/window states pass. Exactly one
definition deletion admits its accepted result plus failed-read/reload without another
delete or financial entry. Original source baseline is then correctly changed; all
other captured facts and the declared one-definition delta match exactly.
Restoration/fresh retirement and supplemental refusal remain explicit in
[the quality record](quality/complete-original-template-center.md). No broad security,
stability, accessibility, phone/iOS/provider/Store or publication acceptance is inferred.

## D-638 original category management acceptance

Focused29new/four existing affected cases and48 original native read/layout states pass in the independent API36/x64/4096 scope. Six original metadata commands run once, including failed post-move read/Retry, archived-parent name Save, same-page draft display refresh and native parent-into-child merge. Ledger remains0; the actual departed editor Save is refused. Final source/restoration/UID/owned-AVD retirement proofs and cleanup refusal are retained. This does not accept physical Main adoption, whole private DB/keys or wider release gates.

## D-639 original account editor acceptance

Twenty-one new focused cases, five affected existing cases and strict Windows/full offline APK
checks pass. Fresh API36/x64/4096 original UI verifies checking/loan identity and raw fields,
six en/fa/de loading/error covers and explicit Retry, cancelled pending read and actual departed
Save refusal. One actual name Save survives notification failure with Save disabled; one native
archive clears the same original default. The complete observed source permits only declared
metadata/default deltas and keeps zero ledger entries. Same-UID correction follows the preserved
fast-failure defect; no accepted seed/Save/archive is replayed. Exact restore and fresh-only
retirement pass, with supplemental cleanup refusal recorded. Physical adoption and broad release
gates remain separate; D-607 is untouched. [Full evidence](quality/complete-original-account-editor-source.md).


## D-640 original goal editor

Twenty new focused and eighteen affected existing cases pass. Eighteen original ready contexts and three error covers retain source identity; exactly one original metadata Save survives notification failure. A separate empty-source final rights delta refuses one Save, preserves the raw draft through explicit Retry and discards without a financial write. Full-source/restoration/fresh-only retirement proofs pass; physical adoption and broader release gates remain open. [Evidence](quality/complete-original-goal-editor-source.md).


## D-641 original budget editor

Twenty-one new and twenty affected existing focused cases pass. Independent native original Budget/Edit verifies pending Cancel, departed Save refusal, explicit error Retry, eighteen ready layout contexts, cached-rights/raw-draft refusal and one total-limit Save with notification failure. Only declared budget total/update-time differs across seventeen observed tables; ledger remains empty. Exact restoration/fresh retirement pass. This is not whole-PE identity, physical adoption or broad release acceptance. [Evidence](quality/complete-original-budget-editor-source.md).


## D-642 original type-editor acceptance

Seventeen new focused cases and22affected existing cases pass. Independent original native pending Cancel, departed Save refusal, explicit Retry,18normal-scale ready contexts, retained raw rights/refusal/Retry and one metadata acceptance with notification failure/newer draft pass. Complete source changes only type Name/UpdatedAt; no ledger write. Physical adoption remains separate. [Evidence](quality/complete-original-asset-type-editor-source.md).


## D-643 original event-frame acceptance

17new and35affected existing cases pass. Original pending Back/departed Save refusal, explicit Retry,18normal-scale ready contexts, retained-source no-reset adoption, held duplicate-Retry refusal and one noncash event acceptance are qualified. Six separate accepted-feedback contexts and actual Close/discard preserve exactly one event and ledger0; general platform/release acceptance remains open. [Evidence](quality/original-asset-event-frame-and-receipts.md).


## D-644 original holding-price acceptance

20new and44affected existing cases pass. Native pending Back/departed Save refusal, actual corrected retained-tab return, failure/Retry,12normal-scale ready contexts and hardware Back/discard are qualified. One manual price acceptance retains newer raw input despite notification failure; six separate accepted-feedback contexts and final Home preserve one valuation and ledger/events0. No original accepted write is replayed. [Evidence](quality/original-holding-price-draft-ownership.md).


## D-645 Bounded saved-comparison acceptance

Complete source, current native frame, covered Retry and reviewed delete receipts are implemented. Fourteen new focused cases and31affected existing cases pass; original native pending/Retry/Cancel,12ready and6accepted-feedback contexts preserve the same source. One saved-definition deletion changes no financial rows. Physical adoption after D-607 and broad DB/key/security/performance/stability/accessibility/iOS/provider/Store/release gates remain separate. [Evidence](quality/original-saved-comparison-source-and-delete.md).


## D-646 Bounded original forecast workspace acceptance

Complete original source, covered Retry, actual row/native prompt ownership and accepted saved-path receipts are implemented. Twenty new focused and 31 affected existing cases pass; original native pending/Retry/Cancel/scenario, 12 ready and six final live receipt contexts pass with retained source. The real tab-root/caption failure is preserved and corrected. Two DISTINCT saved definitions serve the initial and changed-display branch; no financial posting or preparation is replayed. Physical adoption after D-607 and broad DB/key/security/performance/stability/accessibility/iOS/provider/Store/release gates remain separate. [Evidence](quality/original-forecast-workspace-prompt-and-receipts.md).

### D-647 - Complete original report workspace and KPI handoff (2026-10-29)

Original report reads and native commands now share the actual pinned-file packet, covered
publication, cancellable page/tab/root ownership and exact original row membership. All
native destinations freeze their original route/query before awaits. Source-bound KPI
explanations detach lines, retire replacement queries and validate their own normal-close
handoff without breaking the expected original parent return. Stale source stays covered
with explicit Retry; current captions and retained report choices refresh in place. An
incoming original drill scope survives deferred display rebuild until ordinary departure.
The actual contribution writer validates native ownership before/after SQL and records
commit receipt before Changed; original financial calculations and schema are unchanged.

Two original native failures and two original KPI host failures remain in the evidence.
Twenty-two distinct new focused cases and28 affected existing reader/export/access cases
pass. Actual API36/x64/4096 UID10216 boot1ffc2936-e728-4e88-b441-ba848600ef31 continues
from original PID4749 to corrected5947 by one no-reset update. The single original zero-EUR
unknown-opening account and all19 observed tables remain identical; no financial post,
setup replay, PDF/native share or accepted contribution command is repeated. Pending-read
cancellation, actual old issue refusal, read failure/Retry, valid frozen KPI navigation,
stale display feedback and retained current package captions are verified in original UI.
See [complete scope and limitations](quality/original-report-workspace-and-kpi-handoff.md).
Phone/D-607, all three deadlines and post-reboot unread source remain untouched. Later
physical adoption, broader DB/key/security/performance/stability/accessibility, iOS/provider/
Store/release gates remain separate. Overall delivery remains active; TalkBack stays last.

### D-648 - Complete original Settings source and current-profile reset (2026-10-30)

Original Settings now reads complete preferences/accounts and the unchanged three-month
suggestion history from one actual-file worker transaction, validates after native availability,
and retires page/tab/root reads and sensitive continuations. Independent field edits compose
while original reference/account/history identity stays guarded. Raw estimate/reminder drafts
survive failure/Retry; current calendar/display choices retain their original form meaning.
Original device authentication remains genuine. Rare explicit current-profile reset validates
opaque original typed source/schema and native ownership in the original writer transaction,
records commit before Changed and resumes follow-through without deleting again. The old
reset also removes omitted local contacts/cost graph/reference receipts/resource selections;
shared-file guards, device PIN, other profiles and external backups remain protected.

Twenty distinct new and35 affected existing focused cases pass; published and intermediate
failures remain documented. Strict Windows and full offline384AOT/408ELF/1608staticLOAD pass.
Fresh API36/x64/4096 UID10216 continues4304to6302 by one no-reset update and exact read-only
source adoption. Actual pending Back cancels, departed Delete and old native consent refuse,
raw123.4500 survives failure/Retry/display/calendar, one Day2 metadata choice changes only
that field/UpdatedAt, and one legitimate current fake-profile reset reaches empty onboarding.
No financial post/estimate Save/setup replay or genuine native authentication is claimed.

Original OS/display/boot restore; only fresh UID/data,48hash-matched XMLs1,705,935bytes and
owned AVD retire. Six standard cleans remove43,012files2,791,927,695logicalbytes; official
AVD deletion25files5,336,085,476bytes and one new17-target cleanup4,784files1,352,420,443bytes
succeed. Used signed packages/evidence/source archives remain; older refused scopes are
not retried or reused. Phone/D-607/three deadlines/post-reboot unread source remain untouched.
Physical adoption, broader DB/key/security/performance/stability/accessibility, iOS/provider/
Store/release gates stay separate. Overall delivery remains active; TalkBack stays last.
[Complete evidence](quality/original-settings-source-and-profile-reset.md).

### D-649 - Complete original manual-rate workspace (2026-10-30)

Original rates now pin the first actual settings file before initialization, read complete
original rate facts and consumed currency hints in one worker transaction, validate before
publication/navigation, and retire actual page/tab/root reads and reference commands. Failed
source stays covered with explicit read-only Refresh; raw pair/culture/ratio and original
writer/receipt semantics remain. Original rows require exact current instance identity for
native Delete consent, even after identical visible republication. One complete row replacement
keeps original formatting/order. The page consumes its owned failed cover rather than hiding
Refresh behind another generic modal. No FX algorithm, financial entry, model/migration,
provider, runtime or notification deadline changes.

Thirteen distinct new and26 affected existing cases pass; original and intermediate failures
remain. Strict Windows/full offline384AOT/408ELF/1608staticLOAD pass. Fresh API36/x64/4096
UID10216 continues4387to5908to7117 through two necessary no-reset updates of the same source.
Actual pending Back cancels, closed reference navigation refuses, current reference navigation
works, raw1.2500 survives failure/one read-only Retry, and the remaining corrected inline-error
boundary qualifies actual visible en/System/deDark/faLight captions and read-only Refresh.
Two preliminary hidden-under-dialog caption observations are disqualified. Earlier successful
native cases/raw/setup/rate and financial operations are not replayed for the final UI correction;
all19 observed source tables remain equal and native rate/ledger writes are zero.

Original OS/display/boot restore; only fresh UID/data,39hash-matched XMLs1,311,074bytes and
owned AVD retire. Five standard cleans remove42,642files2,638,963,151logicalbytes; official
AVD deletion25files5,491,864,523bytes and one new18-target cleanup4,694files1,492,565,393bytes
succeed. Actually installed signed packages/final shipping/evidence/source archives remain;
older refused scopes are not retried or reused. Phone/D-607/three deadlines/post-reboot unread
source remain untouched. Physical adoption and broader DB/key/security/performance/stability/
accessibility/iOS/provider/Store/release gates remain separate. Overall delivery stays active.
[Complete evidence](quality/original-manual-rate-workspace.md).

### D-650 - Complete original display-unit workspace (2026-10-30)

Original unit reads now pin the actual first settings file, validate consumed definitions/
default hints and retire page/tab/root readers and native commands. Explicit read-only
Refresh keeps raw name/currency/exponent, full source cover and current captions. Submitted
lists come from original captured definitions, with an ambient publication seal; a different
profile’s later global definitions cannot be copied into the original selective writer.
Remove requires exact original row identity. Unknown changed definitions require explicit
reentry; known own receipts admit only their actual readback, consume only unchanged submitted
input and preserve later typing. Post-commit source failure retains known saved/not-published
feedback without a new authority capture or another write. Legacy-null preference migration,
ISO money/export/FX semantics and unrelated newer preferences remain unchanged.

Four original real-source failures and two native failures remain preserved. Fourteen distinct
new,12 affected existing and21 changed-resource completeness cases pass. Strict Windows/full
offline384AOT/408ELF/1608staticLOAD pass. Fresh API36/x64/4096 UID10216/4870to5782 continues
once no-reset with exact original-source readonly adoption. Actual pending Back cancels,
closed Load refuses, initial read error/Refresh is inline, same raw D650 crates/EUR/exponent3
survives three ready and three failed display contexts and explicit Retry. Only one changed
original unit Add metadata command commits; only DisplayUnits/UpdatedAt change, the other18
observed tables stay equal, and native accounts/financial entries remain zero. Original
baseline is preserved separately from that accepted delta; old D-436 matrices are not replayed.

Original OS/display/boot restore; only fresh UID/data,34hash-matched XMLs1,214,444bytes and
owned AVD retire. Six standard cleans remove42,933files2,737,807,575logicalbytes; official
AVD deletion25files5,480,985,547bytes and one new16-target cleanup4,753files1,239,395,088bytes
succeed. Actually installed signed packages/final shipping/evidence/source archives remain;
older refused scopes are not retried or reused. Phone/D-607/three deadlines/post-reboot unread
source remain untouched. Physical adoption and broader DB/key/security/performance/stability/
accessibility/iOS/provider/Store/release gates remain separate. Overall delivery stays active.
[Complete evidence](quality/original-display-unit-workspace.md).

### D-651 - Complete original reference workspace and offline access lifetime (2026-10-30)

Original market-reference reads now pin the actual captured file, read assets and bounded
saved history in one deferred worker transaction, publish complete collections and retire
page/tab/root cache/network work. Old public Load cannot activate a departed page; equal
copied choices do not gain original review authority. Current native/source checks remain
in the exact selective reference writer and typed committed receipt. Original free ECB
provenance, precision, stale acknowledgment, asset mapping, manual fallback and explicit
provider/financial consent remain unchanged. Offline startup/Sleep/Resume/navigation now
maintain the same native access lifetime needed for local reference/analysis, without
enabling network work or a new permission. No model/schema/runtime/provider policy changes.

Three original actual-source failures, the actual offline native pre-read refusal and
readiness/selector refusals remain preserved.11distinct new and11affected existing cases
pass; host logs identify10.0.12, not a relabelled application servicing runtime. Final strict
Windows and full offline384AOT/408ELF/1608staticLOAD pass. Same fresh API36/x64/4096 UID10216
continues4422to5461 once no-reset with exact original source/consumed-marker adoption.
Actual pending Back cancels, old closed Load refuses, full inline source error/one Retry
recovers, and complete original19-table source stays equal. Accounts/rates/financial posts
are zero; no provider, preparation or earlier positive monetary/ECB matrix is replayed.

Exact original display/OS/boot restore; only fresh UID/data,18hash-matched XMLs442,981bytes
and owned AVD retire. Four Clean operations remove28,955files1,990,429,171logicalbytes and
official AVD deletion25files5,312,295,890bytes. Supplementary13-target cleanup is blocked
before process creation;3,733files997,346,279bytes remain and no retry/subset/alternate/move/
escalation/reuse is attempted. Used signed packages/final shipping/pre-refusal archives and
evidence remain. Phone/D-607/three deadlines/consumed reboot/unread source remain untouched.
Physical adoption and broad DB/key/security/performance/stability/accessibility/iOS/provider/
Store/release gates stay separate; overall delivery stays active. Complete evidence:
`quality/original-market-reference-workspace.md`.

### D-652 - Complete original accountant disclosure workspace (2026-10-30)

Original accountant choices now read minimal immutable active/archived account identities
under one captured-file worker transaction. Original Review validates those actual captions,
currencies/archive identities before scanning another packet. Page/tab/root retirement cancels
source/review/unoffered output; old public Load cannot reactivate disclosure authority and
old completion cannot clear newer busy state. Whole choices publish together. Literal year,
notes/documents opt-ins and unchanged explicit account choices survive failure/Retry/return;
changed identities require new disclosure and retired callbacks cannot erase current review.
Original tax/data-only scope, ZIP contents/bounds/digest/day/rights, redaction, real native
authentication/share and financial behavior remain unchanged. No model/schema/runtime change.

Four actual original-source failures remain preserved.10distinct new and4affected original
native-await refusal cases pass; successful earlier positive output/financial matrices are
not replayed. Strict Windows/full offline384AOT/408ELF/1608staticLOAD pass. Fresh API36/x64/
4096 UID10216/PID4765 has one zero-opening account metadata prepared once and ledger/rates0.
Actual pending Back cancels, closed Load refuses, one raw2025/account/notes input remains
through source failure/Retry with checked native binding and full feedback. One read-only
Review produces only summary; ZIP/share/authentication/financial writes0 and complete original
19-table source equal. Private read-only Entity.Id compile refusal and clipped-target input
refusal remain; only the latter unaccepted checkbox continues after bounded normal scroll.

Exact original OS/display/boot restore; fresh UID/data,25hash-matched XMLs1,001,492bytes and
owned AVD retire. Four Clean operations remove28,955files1,990,843,079logicalbytes; official
AVD deletion25files5,245,121,522bytes and one new10-target cleanup3,726files742,213,210bytes
succeed. Used signed native/final shipping/source archives/evidence remain; earlier refused
scopes are untouched. Phone/D-607/three deadlines/consumed reboot/unread source remain
untouched. Physical and broad DB/key/security/performance/stability/accessibility/iOS/provider/
Store/release gates remain separate; overall delivery stays active. Complete evidence:
`quality/original-accountant-disclosure-workspace.md`.


### D-653 - Complete original CSV workspace

Original CSV import/export now pins one actual file, reads complete workspace/export sources
on the worker, and binds picker/preview/native disclosure/Undo to the actual page/tab/root.
Read-only source Retry preserves raw filename/mapping/calendar and known committed receipts.
Original rules are read in the preview transaction; original financial writer/conflict policy,
all32 fields, journal/CSV formats and explicit native authentication remain. Fresh private CSV
copies preserve BOM/neutral names without overwriting earlier offered copies.16 new and10
related focused host cases, actual fresh native cancellation/retained raw Retry/delayed export
refusal with zero financial operations, strict builds and complete signed offline packages
are recorded in [D-653 evidence](quality/original-csv-workspace.md). Physical adoption waits
for actual D-607 completion; its three deadlines and unread post-reboot source are untouched.
This is not general stability, speed, A11Y/security/DB/key/iOS/provider/Store/release acceptance.


### D-654 - Complete original backup restore lifecycle

Original local/cloud restore selection pins its native frame and actual target publication
before selection. Stream/inspection, consent, authentication, safety, exclusive primary queue
and protected receiver refuse retired/replaced owners. Original accepted primary receipts
remain known before fallible follow-through and cannot replay replacement; actual return
retries only follow-through. Current raw review/error behavior and original restore/encryption/
recovery algorithms remain unchanged. Actual PIN modal authority is exact weak root/page/modal;
its owned cover survives until Cancel while an unrelated route is refused. No credential,
permission, schema, format or financial policy changed.

Twenty distinct new and4 affected host cases, original native root-retirement and actual
LockPage cancellation with full19-table source equality and zero financial/primary/requested
safety operations pass. Strict Windows, complete offline384AOT/408ELF/1608staticLOAD shipping
and independent signed native packages pass. Intermediate host/observer/presentation failures
remain preserved. General backup creation/cloud discovery and other sensitive-page lifetime
work are separate; this is not genuine authentication or full backup release acceptance.
[D-654 evidence](quality/original-backup-restore-lifecycle.md) records the exact boundaries.
Phone/D-607/three deadlines/consumed reboot and unread source are untouched; physical adoption
and broader DB/key/security/stability/speed/A11Y/iOS/provider/Store/release gates stay open.


### D-655 - Complete original portable backup creation

Original local/cloud creation captures actual native/source/input/destination before sensitive
confirmation and keeps an independent portable busy lifetime through the original operation. Exact owned PIN cover
survives Cancel; an unrelated route refuses. One-shot original creation guards every storage
effect, consumes only unchanged submitted input and accepts the actual stored receipt before
fallible listing/share. Original service filename/history/retention pins its first profile;
changed profiles cannot relabel/prune another set, and later timestamp/retention failure or
cancellation cannot erase a known upload receipt. Original format/encryption/defaults/source/
financial/permission/provider policy remain unchanged.

Six original-service failures,12 distinct new and3 affected cases, strict Windows/full offline
384AOT/408ELF/1608staticLOAD and original native LockPageCancel/busyBack/root-retired-confirmation
pass. Full19-table source equal; manual portable/share/financial0. Fresh UID/11hash-matched XMLs/
owned AVD retire with exact original OS. [D-655 evidence](quality/original-portable-backup-creation.md)
keeps failures, receipt and acceptance limits. General discovery/cloud-identity/deletion and
other sensitive-page work remain separate. Phone/D607/three deadlines/consumed reboot/unread
source are untouched; physical/provider/DBkey/security/ANR/speed/A11Y/iOS/Store/release gates
remain open. Overall delivery remains active.

### D-656 - Correct original CI readback and account recovery

The actual failed test job was reproduced on current source: 26 failures in 52
affected original cases. The correction retains the actual original committed CSV
writer's opaque readonly receipt for batch publication, preserves interpretation
feedback, and makes explicit stale-account review read the complete original source
while retaining raw input and account/default/currency guards. Historical assertions
now match original atomic account/default writes, owned route origins, quiet retired
callbacks and inline read covers; no source comparison or writer conflict policy is weakened.
The 53 distinct selected existing cases and eight new negative/receipt cases passed
in focused continuations. Strict Windows and complete offline Android packages passed.
Original native PID 4118 kept the rejected account draft through full explicit review,
then one distinct CSV import changed only Entries/ImportLinks among 20 source sets,
published one actual batch and produced no false follow-through failure. Fresh UID/AVD
and 29 hash-matched XML files retired with exact OS preservation. Supplementary cleanup
of 11 targets was rejected before process creation; 3715 files/947761224 logical bytes
remain and must not be retried, subdivided, moved, escalated, reused or deleted by another method.
Phone/D-607 and all three deadlines remain untouched. Physical adoption, general backup
discovery, sensitive-page work and broader release gates remain separate; pushed-head CI
has one delayed check, not an assumed pass. [D-656 evidence](quality/original-ci-readback-correction.md)

### D-657 - Original backup workspace and destination ownership

Original local/automatic/safety/connected discovery now retains its file/page/root,
cancellation and current row/account identity through native consent and publication.
Sensitive commands have independent ownership; readonly Back cancels pending discovery.
Loading/error covers require explicit Retry, preserve raw input and known actual deletion
receipts, and reject retired source, disclosure and destination effects. Discovery still
spans own-application profile sets independently of retention. Two new library cases,
one related existing case and 32 new application cases passed in focused continuations;
24 targeted resource checks and strict Windows/full offline Android builds passed.
Original native qualification used fictitious identity/local storage, not genuine OAuth:
12 narrow OS200 error contexts and six later receipt contexts, one native connection,
one disconnection and one confirmed fake-file deletion. Full 20-collection source equality,
zero accounts/ledger and exact original OS/preferences remained. Incomplete initial
preparation and necessary no-reset observer/receipt upgrades are separately documented;
successful operations were not replayed. Fresh UID/57 hash-matched XML/owned AVD retired.
Five standard cleans removed 29196 files/2113039306 logical bytes; one bounded supplementary
cleanup removed 3766 files/749948420 logical bytes. Earlier refused scopes were untouched.
Phone/D-607/three deadlines/post-reboot unread source are preserved. Physical adoption,
genuine providers, full DB/key/security/stability/speed/accessibility/iOS/Store/release
remain separate. D-656's sole exact-head CI response was green for all four workflows;
D-657 has its own single delayed pushed-head check. [D-657 evidence](quality/original-backup-workspace.md)

### D-658 - Original profile catalogue and native command lifetime

Original profile commands retain independent busy/page/root/frame/disclosure ownership
through prompts, exclusive waits, destination opening, authentication and publication.
Protected deletion rechecks after identity inventory before accepting its durable request;
accepted request recovery remains unchanged. Retired reads/known mutations cannot publish
into another root. One baseline shared-busy regression failed; 14 distinct new and 73
existing affected cases passed in focused continuations. Final strict Windows and complete
offline Android384AOT/408ELF/1608staticLOAD packages passed. A necessary no-reset private
upgrade preserved UID/full20source before preparation; PID5224 then refused a real old Add
prompt after actual root replacement with exact source/catalogue equality, and accepted
one fresh original profile creation plus both native deletion consents. New profile never
opened, Main/source20/account0/ledger0 preserved; envelope revision changes are expected.
Original OS/boot remained exact; freshUID/24hash-matchedXML/ownedAVD retired, fourCleans
28955files/2015040253logicalbytes and supplementary3711files/750087771logicalbytes removed.
A separate two-file D657 publication cleanup was blocked before process creation; no retry,
subset/alternate/move/escalation/reuse is allowed. Used signed packages/evidence remain.
Phone/D607/three deadlines/post-reboot unread source untouched; physical adoption, genuine
authentication/providers and broader DB/key/security/stability/speed/A11Y/iOS/Store/release
gates remain separate. D657 sole delayed CI response was pending and its schedule deleted;
no second query. D658 gets its own single delayed pushed-head check. [D-658 evidence](quality/original-profile-catalogue-lifetime.md)

### D-659 - Original active-resource workspace and owned parent return

All six original resource kinds retain their existing limits and writers. Original
read/revalidation uses one deferred SQLite transaction on a worker; page/root/frame/
disclosure and independent command ownership retain raw selections through native
consent, Retry and commit. One-use accepted receipts precede fallible Changed handlers.
An exact one-use native parent-return scope keeps the command lock held and corrects
the Shell guard blocking its own Pop; profile Leave uses the same scope, without a
new claim of native profile dirty-return acceptance. 20 new plus86 existing distinct
selected cases passed in focused continuations, with original failures preserved.
Final strict Windows and full offline APK384AOT/408ELF/1608staticLOAD/ZIP/signature pass.
Fresh API36/x64/4096 UID10216/PID4200 first proved old-root kind-consent refusal, one
account-empty commit with controlled Changed failure and six-kind reads. One budget-
empty commit exposed the actual blocked return; neither successful Save was replayed.
One necessary no-reset upgrade to7389 preserved all21 observed source parts; read-only
adoption did not reprepare. One different selected-budget payload changed only its
choice IdentitySet/Revision and reached ProductPlansPage with actual Pop/CanLeavefalse/
Cancelledfalse. A dirty original Back/Discard also returned with exact21source and no
Save. All20 financial/settings collections, account choice and ledger0 remained exact.
Original OS/boot/Home restored; freshUID/46XML2248787bytes hash-clean/AVD retired.
Six standardCleans43012files2831119963logicalbytes and12 supplementary targets4770files
881288496logicalbytes removed; used signed packages/source/evidence remain. The initial
shutdown console subcommand failed under adb shell; only that unfinished step was
corrected, without repeating completed retirement. All earlier refused scopes untouched.
Phone/D607/three deadlines/consumed reboot/post-reboot unread source remain protected.
Physical adoption, real tier/provider/authentication, complete DB/key/security/stability/
speed/A11Y/iOS/Store/release remain separate. D658 sole delayed CI and CodeQL green;
its schedule deleted, no second query. D659 has its own single delayed pushed-head CI.
[D-659 evidence](quality/original-active-resource-workspace.md)

### D-660 - Original personal synchronization workspace

Original connection/status/complete review relationships and first-run eligibility now use
one original-file worker transaction. Independent page/root/disclosure/input/session/row/
preview/Close ownership refuses retired continuations and retains literal nonsecret drafts.
Original writers revalidate around commit; known accepted results precede fallible follow-up,
with six-language explicit feedback and read-only Retry instead of successful-command replay.
52 distinct selected cases (20 new,32 affected existing), strict Windows/full offline Android
384AOT/408ELF/1608staticLOAD, original native consent/root/source/accepted receipt and all18
normal-font en/fa/de/Light/Dark/360/412.190476/680dp feedback contexts passed. Two independent
zero-account/ledger/plan local-binding cohorts each performed one distinct metadata Pause;
the first accepted operation was never replayed. All display contexts shared the second
accepted page/PID4720, exact20financial+9sync source and unchanged1notification/1failure/0remote.
Initial consent/TLS fixture/observer/theme failures are preserved; only unfinished cases
continued. Original OS/display/night/boot/Home restored; both fresh packages/AVDs and
51hash-matchedXML1609941bytes retired. FiveCleans 33175files/2457780345logicalbytes and bounded
supplementary15targets/14177files/1150915509logicalbytes completed; usedsigned/source/evidence kept,
earlier refused scopes untouched. Phone turn explicitly returned, no phone command; D607
three deadlines/consumed reboot/post-reboot unread source preserved. Real identity/provider,
physical adoption, fullDB/key/security/stability/speed/A11Y/iOS/Store/release remain separate.
D659 sole exact-head response was completed/success for all4workflows and its schedule
deleted; no second query. D660 receives one delayed exact-head CI check after push.
[D-660 evidence](quality/original-personal-sync-workspace.md)


### D-661 - Original household workspace

Original household connection/status and verified local metadata now use one captured
original-file worker transaction. Page/root/disclosure/literal-input/session/row/native
and file continuations retain independent ownership; returned primary acknowledgements
precede fallible follow-through, while unknown outcomes never receive a fabricated
receipt. Covered Retry reads the current original source without replaying accepted work.
Protected first-run completion changes only the oldest settings completion flag and its
UpdatedAt; a committed flag makes later Finish root-only, with no second Save or consent.

26 distinct selected cases (20 new, six affected existing), current strict Windows and
full signed offline Android384AOT/408ELF/1608staticLOAD pass. Actual original API/protected
Android owner setup and one accepted first-run commit with a subsequent Changed failure
passed without financial preparation. Same PID3873 retained 18 en/fa/de/Light/Dark/
360/412.190476/680dp error contexts, full source/metadata and one failure; XML text/native
bounds qualify these contexts, not the disqualified Activity-decor probe or glyph metrics.
Original Reload and root-only Finish reached actual Home with ledger/accounts empty;
20 source collections remained exact, normal foreground sync changed only LastSuccessAt.
OS/display/boot restored; fresh UID/41hash-matchedXML/AVD/API host/database/volume/two
networks retired, five original containers exact. Five standard Cleans passed; eight
supplementary scopes were rejected before process creation and remain frozen without
retry/subset/alternate/move/escalation/reuse. Used packages/evidence remain. No phone or
D607 command; three deadlines/consumed reboot/unread source protected. D660 sole CI result
is unknown after a preserved host marker/persistence failure, consumed and never queried
again. D661 receives one delayed exact-head check. WholeDB/key/security/stability/speed/
A11Y/iOS/genuine identity/provider/Store/release remain separate; overall goal active.

[Evidence](quality/original-household-workspace.md)


### D-662 bounded costs acceptance

46 selected cases and strict Windows/full offline APK passed. Native one-cost acceptance, 18 original error contexts, read-only Retry, after-DML rollback and final V2 read-only adoption passed on their recorded versions. Native refunds/repayments/nonempty protected cash and all writer branches are not qualified.
[Evidence and cleanup limits](quality/original-household-costs-workspace.md).


### D-663 bounded plan/access acceptance

53distinct selected cases (14new/39existing), strict Windows/full offline APK,18same-process native error covers, retained comparison/readonly Retry, actual pending-source cancellation and Continue return pass with complete20source/settings equality. Genuine billing, provider, identity and physical acceptance remain separate.
[Evidence and exact cleanup limits](quality/original-product-plans-workspace.md).


### D-664 - Original optional identity evidence

26 distinct selected checks (19 new, seven affected existing), strict Windows/offline Android and 18 same-PID native response/error contexts passed. One actual uniform anonymous recovery survived one follow-up fault; explicit session Retry and pending-read native Back passed with full original source equality and no replay. Genuine authentication and Store/provider acceptance remain open.
[Evidence and exact cleanup limits](quality/original-billing-identity-workspace.md).


### D-665 - Original analysis engineering evidence

43distinct selected checks (18new/25affected existing), strict Windows/full offline Android and bounded actual native source/review/Cancel/error/Retry/pending Back pass in their recorded versions. One original zero-opening account/ledger0 survived necessary no-reset continuations with exact20source equality. Host-caption and private-provider failures remain recorded; only two corrected language contexts continued, without repeating the old matrix or financial setup.
[Evidence, failures and exact cleanup limits](quality/original-analysis-workspace.md).


### D-666 - Original Android CSV file handoff

D666 closes D653's missing actual Android picker and owned-recipient boundary: one Cancel, one selected fake CSV/readonly preview and one empty-ledger export; full20source unchanged, no Import/Undo/financial Save. External recipient and real provider acceptance remain separate.
[Evidence and exact limits](quality/original-csv-native-file-handoff.md).


### D-667 - Original PIN settings lifetime

D667:39distinct selected cases (16new/23affected existing) and three native version-bound continuations cover original PIN queue/read ownership, secret clearing/newer drafts, pending Back, competing Save during close and saved-return receipt without a second write. Full20source and OS restoration pass; two PIN configurations are distinct changed branches, ledger0. Native failure injection and logical authentication ports do not establish genuine owner/biometric acceptance.
[Evidence, actual failures and limits](quality/original-pin-settings-lifetime.md).


### D-668 - Original recovery setup lifetime

D668 preserves one actual original unowned-generation failure, fixes it on the same UID/source without reset, and adds one distinct created-but-not-returned material-disposal case.5actual gate/source cases pass, including one preserved intermediate cancellation failure. No account/ledger/code/acknowledgment/Save/authentication input or crypto corpus replay. Genuine owner-code resume/enrollment and native saved-close acceptance stay open.
[Evidence and exact limits](quality/original-recovery-setup-lifetime.md).

Final D668 Window.Page replacement cancels the original queued work with generated0 and full20source equal. Final strict packages pass; actual enrollment/owner authentication remain separate. Phone/D607 and all deadlines stay untouched.


### D-669 - Original startup backup lifetime

Bind original picker/read/confirmation/staging and terminal navigation to the actual startup recovery owner. Nine actual-stream cases, final strict packages and the actual native late-picker/current-consent/pop continuations pass with full20source equal and no valid restore or financial write. Preserve genuine authentication and the original durable activation protocol; phone/D607 remain untouched.
[Actual original failures, version lineage and limits](quality/original-startup-backup-lifetime.md).


### D-670 - Original startup recovery command ownership

Capture the actual covered startup and original source before native confirmation; cancel retired prompt/feedback handoffs and retain original authority through protected recovery waits.13new and3affected original cases, final strict packages and actual native counterfactuals pass with source20equal and no valid key/portable recovery or financial write. Preserve genuine authentication and the original recovery/activation protocol; phone/D607 remain untouched.
[Original failures, corrected native continuations and limits](quality/original-startup-recovery-lifetime.md).


### D-671 - Original startup publication ownership

Retain actual startup window/root/model through original protected work, access setup and deferred publication; a retired callback cannot overwrite a replacement or consume its pending link.13new+4affected cases, final strict packages and original native counterfactual/current Retry pass with source20equal and no financial/authentication input. Preserve genuine access policy and all D607/phone state.
[Original failure, native current retry and limits](quality/original-startup-publication-lifetime.md).


### D-672 - Original unlocked destination order

The first already-unlocked destination now owns the same queue drain as pending work, so a later arrival cannot overtake its asynchronous navigation. Preserve direct errors, reentrant followups, failed queued-item isolation and unstarted work behind a new cover. Two original regressions fail;4new+23affected existing cases, strict Windows/full offline APK and actual original-router/native modal ordering pass with full20source unchanged. Original4478Second/First becomes5532First/Second on the sameUID/boot/source after necessary no-reset upgrades; the first corrected observer's preserved-marker refusal is retained without callback replay. No account/authentication/key/financial input; phone/D607 remain untouched. Broader device/security/stability/speed/platform/provider/release gates remain separate.
[Actual original failure, corrected ordering and limits](quality/original-unlocked-destination-order.md).


### D-673 - Original reminder access continuation

Scoped budget/goal/plan resolution now retains its original read-only resolver ahead of later arrivals when access closes during the actual source read, then resolves and validates again after unlock without replaying navigation or writers. Three original failures,9new+13affected cases and strict Windows/full offline Android pass. Original native5282displayed GoalDetailPage above LockPage while still locked; corrected5989kept only the PIN cover, then opened the original goal exactly once after fixture-PIN unlock on the sameUID/boot/full20source. One fictitious goal and fixture PIN were created once; accounts/ledger/financial/owner credentials0. Observe-only failures and exact limits remain recorded; phone/D607 untouched and broader gates open.
[Original failure, native access continuation and limits](quality/original-reminder-access-continuation.md).


### D-674 - Original account covers at native text scale

Qualified the original account-editor loading/read-failure covers in18real OS200contexts (en/fa/de, Light/Dark,360/412/680dp),36native text/target observations and one original visible Retry. Captions match original app/shared resources; actual line/ellipsis/visible bounds and viewport measurements pass with full20source unchanged and account/ledger/Save0.17contexts completed in5055before actual native crash/status11; only the last continued in9364on the sameUID/boot/APK/source. Cause/repair is unproven. Exact original display/source restored, freshUID/57XML699507capturebytes/officialAVD retired and one private Clean passed. Product code/full shipping/tests unchanged; phone/D607 and broader gates remain open.
[Native geometry, actual exit and limits](quality/original-account-cover-native-text-scale.md).


### D-675 - Original Shell page lifetimes

Thirteen original strong page-registration sets now track weak reference-identity owners while preserving
all original retirement guards/callbacks. Four new lifetime cases and14affected existing cases, strict
Windows/full offline Android and actual original category-modal collection/live-root retirement pass:
baseline3883retains the closed page; corrected5424collects it and still retires a live modal, with the same
UID/boot/full20source and no metadata/financial Save. The private adoption omission and observer/cleanup
failures remain recorded. Fresh fixture/device-XML/AVD retirement and five standard Cleans are complete; local XML deletion and supplementary
20target deletion is rejected before execution and frozen. This does not fix or explain D-674's native
crash or close broader device/performance/security/platform gates. Phone/D607 remain untouched.
[Actual lifetime comparison, candidate and exact limits](quality/original-shell-page-lifetimes.md).


### D-676 - Bounded owned native-exit library frames

An actual separate empty-UID native control exposes a diagnostic reader defect: a non-library
frame discards the whole trace. Preserve allowed library locations, omitted count and original
ordinals under unchanged identity/privacy/128-frame/8MiB bounds. Two original regression failures,
five new+nine affected cases and the same actual5526/10217SDK record pass after correction, with
one intentional signal only. Dumpsys trace=null does not preclude that actual SDK tombstone.
Twelve finite original-app read-failure/native-Cancel cycles in6071retain full20source without
a native crash; D674's random failure remains unresolved and this input is not repeated.
Exact OS/source restoration, two fixture removals, device XML/AVD retirement and three standard
Cleans complete;32target supplemental deletion is refused and frozen. Product code/shipping and
phone/D607 remain unchanged; broad release gates stay open.
[Actual reader failure, filtered native evidence and limits](quality/owned-native-exit-library-frames.md).


### D-678 - Original goal, budget and asset-type covers at native text scale

The three changed original editor loading/error footers from D-640/D-641/D-642 are qualified
at actual OS font scale 2.0: 54 contexts/108 states, en/fa/de, both themes and 360/412/680 dp,
with complete native captions/last lines, visible progress indicators and 44 dp Retry targets.
Three original visible Retry actions and equality across 20 source collections pass without
account/goal/budget/asset/ledger seed, Save, PIN, TalkBack or successful-command replay.
Product source/shipping remain unchanged; only private original-app callers are built,
with zero new product unit tests. Actual PID 4527's managed exception, 6432/12247's EXIT_SELF
status 255 and 10110's native status 11 remain recorded. Thirteen SDK frames match the
original packaged Mono BuildID/executable segments, without raw memory or a fresh signal.
Accepted contexts survive actual deaths; only unfinished contexts continue after proved
absence/read-only adoption and one necessary private reader upgrade. Causes remain unproved;
no general stability fix is claimed. Exact OS/source restoration, fresh UID/XML/AVD retirement
and standard/supplementary cleanup are complete. Phone/D-607 remain untouched; Main adoption
retains its original timed-cohort conditions and broader release gates remain open.
[Finite original native evidence, real failures and limits](quality/original-editor-cover-native-text-scale.md).


### D-679 - Exact symbols for the retained original native fault

The existing D-678 PID 10110/UID 10216 native fault is mapped without another native run:
the local runtime equals the used APK module, and the official debug file's BuildID and
GNU debug-link CRC match. All 13 PCs are inside exact function ranges; existing LLVM
resolves16 inline/location entries across seven actual package-repository source files.
Fault ordinal 0 is in SGen pointer scanning/copy-or-mark during major collection/card scanning
triggered by array allocation; its inner line 0 remains unavailable. Reference origin,
exclusive cause and a repair remain unproved. No source/runtime/GC/build/test/financial or
device change occurs. The temporary downloader is uninstalled; supplemental deletion of
two new cache/tool roots is rejected before execution and frozen, with one 15762568-byte
duplicate remaining. Used debug/source/evidence are retained. QA-06 and the delivery goal
remain active; D-607/phone and all broader acceptance gates remain untouched.
[Exact symbol/source binding, actual limits and cleanup refusal](quality/exact-original-native-fault-symbols.md).


### D-680 / D-681 - Original native headings and retired billing reviews

D-680 qualifies54 original rates/display-units/reference contexts and108 loading/error
states at actual OSfont2.0, en/fa/de, both themes and360/412/680dp, including original
Toolbar/Back and three visible Retry actions. Real German-unit and English-reference
title clipping is corrected with accurate compact resource captions, retaining detailed
source/financial disclosures. All21 typed source collections remain equal without seed,
Save, financial/provider work, PIN or TalkBack; affected accepted states alone are updated.
Strict Windows/full signed offline384AOT/408ELF/1608staticLOAD and21 resource contracts pass.
[Actual defects, native evidence and limits](quality/original-rate-cover-native-text-scale.md).

D-681 reproduces seven current CI failures, uses explicit Reload in the six-language
replacement test, and retires native-review reservations with their actual frame while
old completions retain their epoch. The32 affected existing cases and two new late-result
cases pass. One actual original page/native dialog in PID19111 accepts the retired dialog
without invoking restoration, with source21 equal; the precise lifecycle retirement is a
private diagnostic call, not accepted user navigation or a real store. The hosted Windows
compiler remains unqualified: new public metadata observation preserves the exact existing
signature/hash/SDK/native-package gates. No hosted-green or release claim is made.
Phone/D-607 deadlines, consumed reboot and unread source remain untouched; the goal is active.
[Actual CI failures, original review repair and hosted boundary](quality/retired-billing-review-and-hosted-compiler-drift.md).


### D-682 - Exact original managed resource-cache synchronization evidence

The exact retained D-678 PID4527 APK supplies its actual runtime14 CoreLib image,
version/MVID and394-byte original GetObject IL. Both dictionary lookups follow
Monitor.Enter inside protected regions and have matching finally/Exit. This contradicts
an absent lock in that packaged managed method; executed AOT instructions, reference
origin and exclusive managed/native cause remain unproved. The original AOT exposes no
matching function symbols. A trimmed parameter-table assumption is corrected from the
actual three-parameter method signature, preserving the initial observation. No product,
runtime/GC, device, native run, financial work or successful test/build is repeated; only
two small strict metadata-inspector builds occur, without executing the target CoreLib.
One standard Clean passes. Automatic review rejects three supplemental fresh targets
before process creation, leaving11files/3013350logical bytes frozen without retry,
subset, alternate method, move, escalation or reuse. Original APK/used CoreLib/proofs
remain; D-607 and the goal stay active and wider release gates remain open.
[Actual packaged IL, exact code identity and limits](quality/original-managed-resource-cache-lock-evidence.md).


### D-684 - Original CSV and accountant source covers at actual OS200

The finite changed-view scope completes36 contexts/72 loading-error states and two
original Retry actions across en/fa/de, both themes and360/412/680dp. Original duplicate
CSV progress, clipped accountant title and misleading packet-creation error during a
source-only read are preserved and corrected. The18 accepted CSV contexts are not
replayed after accountant-only changes. One new red/green SQLite regression plus ten
affected existing cases and23 resource contracts pass; shipping Windows and complete
offline Android384AOT/408ELF/1608staticLOAD are strict and warning-free. Source21 remains
exact with no financial/account/CSV/export/share/authentication setup or replay. Same
UID/boot survives necessary no-reset updates; original startup/observer/invocation and
absent-density-key failures remain recorded. Exact OS/source restoration,117 hash-matched
owned XML removals, fresh fixture/official AVD retirement and five standard Clean scopes
complete. Automatic review rejects two supplemental roots before process creation,
leaving840files/125400007logical bytes frozen without retry/subset/alternate/move/
escalation/reuse. Used signed packages and evidence remain; D-607's three deadlines,
consumed reboot and unread source stay protected. Physical adoption and broader
DB/key/security/stability/performance/A11Y/iOS/provider/Store/release gates remain open.
[Original changed source covers and actual preservation evidence](quality/original-export-source-cover-os-text.md).


### D-683 - Qualified new hosted Windows producer and original source closure

The isolated actual new-installer producer run41190613268 completes successfully after
linking original BackupRestorePublication in both compatibility callers and refusing
wrong requested-host metadata before native compilation. The first older-host run and
source-closure failure remain preserved. Two fresh new-host DLLs equal pinned candidate9;
all five package entries match, with only the exported candidate10 nuspec version field
restored for exact comparison. Candidate10 is not published/selected; candidate9's version
and entry hashes remain unchanged, with only reviewed installer18.11.12328.437 added to
its provenance. Actual original53-migration/key/tamper/portable/full typed-source gates
pass;17 request fixtures,27 intermediate and29 final impacted compiler/producer cases
are recorded as overlapping scopes, not summed new tests. Strict local Windows caller
compile-only and Android caller build pass with existing exact native inputs. No app,
financial/device, old native/Android success or performance cohort is replayed. Ordinary
complete Main CI is still separate; D-607 and wider release gates remain protected.
[Actual new host, exact immutable package identity and limits](quality/qualified-hosted-windows-producer.md).


### D-685 - Original backup source covers and growing Retry action

Two actual native presentation defects are preserved and corrected: Persian412 source
Retry clips its final line in a fixed native Button, and Spanish360 Toolbar title loses
three characters. Existing WrappingAction retains the original Retry command/key/enablement;
new page-only Backup_PageTitle keeps all shared menus/body text and every non-Spanish
native title exact. Eighteen loading and24 changed-Retry error observations pass, preserving
eight loading packets and18 corrected error results while continuing only unfinished cases.
No native Retry/file/backup/account/financial/authentication command or old successful logical
suite is replayed. Twenty-three existing resource contracts and strict Windows/full offline
Android384AOT/408ELF/1608staticLOAD/v2v3/CRC/ZIP16KiB pass; these are not42 new unit tests.
Same UID/boot/source21 survives necessary4395to6883to10825 upgrades. Original startup,
readiness, theme-disposed-renderer and clipping failures remain preserved, without claiming
a general crash fix. Exact OS/source restoration,80 hash-matched owned XML removals,
fresh fixture/official AVD retirement and four standard Clean scopes complete. Supplemental
three-target removal is rejected before process creation, leaving841files/224707169logical
bytes frozen without retry/subset/alternate/move/escalation/reuse. Final APKc4235bbb and
used packages/evidence remain; phone/D-607/three deadlines/consumed reboot/unread source
stay protected. General DB/key/security/stability/performance/A11Y/iOS/provider/Store and
release acceptance remain separate. D-683's ordinary Main Native cipher and CodeQL checks
complete successfully at20ed349a; general CI is still in progress at its sole observation.
[Actual clipping, retained native cases and exact restoration](quality/original-backup-source-cover-os-text.md).


### D-686 - Original category and rules source covers at native OS200

One fresh empty protected app qualifies54 contexts/108 loading-error states across Rules,
Categories and Category Editor, en/fa/de/both themes/360/412/680. Initial9narrow observations
find only clipped Rules titles: English4/German7 ellipsized characters. One page-only
Rules_PageTitle binding and six real translations fix them while shared menus/body/all
existing values stay exact; Persian retains its actual prior title. Unvisited original
caption rows are audited from captured packets, not another UI run; no further clipping
is found. Seven unchanged initial successes remain accepted and only47 unfinished/changed
contexts continue. Zero account/category/rule/financial/file/authentication setup, native
Retry or old metadata command is replayed. Scope uses the original Store/read/worker and
VM ownership; only three store acquisition factories are held/failed, with no global factory
replacement or write grant. Same UID/boot/source21 survives4848to7221 once.23existing resource
contracts and strict Windows/full offline384AOT/408ELF/1608staticLOAD/signature/CRC/ZIP16KiB
pass;54observations are not new unit tests. Original OS/source restore,169hash-matched XML
removals, fresh fixture/official AVD retirement and4standardCleans complete. Supplemental
2root deletion is rejected before process creation;839files/125402900logical bytes remain
frozen without retry/subset/alternate/move/escalation/reuse. FinalAPK5f566ecb/usedpackages/
canonicalevidence remain outside those scopes. Phone/D-607/three deadlines/consumed reboot/
unread source stay protected; broader DB/key/security/stability/performance/A11Y/iOS/provider/
Store/release gates remain open. D685soleCIresponse records CodeQLsuccess and generalCI
in_progress; its consumed monitor is deleted, without another workflow-runs request.
[Original titles, retained native cases and exact source/OS preservation](quality/original-category-source-cover-os-text.md).


### D-687 - Rejected private ledger text-table format

D687 preserves all32 original values/order and independent mutable Tags across four host shapes, plus finite malformed/cancel/bucket controls. These are private feasibility controls, not new product tests or Home/two-second acceptance. Latency/allocation gates fail; no native replay follows.
[Fixed scope and actual results](quality/rejected-ledger-text-table-format.md).


### D-688 - Original managed resource-cache writer inventory

D688 checks exact original PE identity, six field assignments, shared constructor alias and three protected direct dictionary writes. Two strict metadata-inspector builds are not new product tests; original native failure/reproduction and executed AOT acceptance remain unresolved.
[Actual inventory and limits](quality/original-managed-resource-cache-writer-inventory.md).


### D-689 - Original Plans and Templates source covers

D689 accepts36original OS200 contexts/72loading-error states across en/fa/de, both themes and360/412/680. Nine unchanged states are retained; only63remaining/affected states and one new relay tap run. Source21 stays exact with no financial/setup replay. Noncanonical push and ambient-culture observer failures are preserved, without changing final native-caption/route criteria.
[Actual failure, native continuation and limits](quality/original-plan-template-source-cover-os-text.md).


### D-690 - Original resource and product-plan source covers

D690 accepts36contexts/72original loading/error states, en/fa/de and two themes at360/412/680dp and OSfont2.0. Exact source21, native captions/lastlines/RTLBack and restoration pass;23 existing resource contracts pass, with no new unit-test count or broad accessibility claim.
[Actual original failure, native continuation and limits](quality/original-access-source-cover-os-text.md).


### D-691 - Original retained Android title and Insights source covers

D691 final en/fa/de, Light/Dark,360/412/680dp OS200 covers and retained360to412 Persian title regression pass native final-line/ellipsis/bounds/44dp targets with source21equal. Two corrected successes are retained;34affected/unfinished contexts continue. No new unit-test count or broad accessibility/stability proof.
[Actual original failure, native continuation and limits](quality/original-insights-source-cover-os-text.md).


### D-692 - Original saved-comparison covers and retired native flyout

D692 native en/fa/de, two themes,360/412/680dp and OS200 covers pass actual caption/final-line/ellipsis, pushed stack2, RTL/native Back and44dp Retry targets with source21equal. Three real managed-root transitions plus theme/width changes qualify the disposed-observer regression. No new unit-test count, populated saved-name, general stability or full accessibility claim.
[Original native failure, retained scope and exact limits](quality/original-saved-comparison-covers-and-retired-flyout.md).


### D-693 - Original Plan modal covers and flyout rebinding

D693 original en/fa/de, both themes,360/412/680dp OS200 modal loading/error captions, complete native lines/ellipsis/bounds and44dp Cancel/Retry pass with source21equal. The actual post-modal original theme transition passes; the original disposed failure and diagnostic refusals remain preserved. This is not general stability or full accessibility acceptance.
[Actual native failure, continuation and exact limits](quality/original-plan-editor-covers-and-flyout-rebinding.md).

### D-694 - Original account covers and wrapped title

35 new en/fa/de, Light/Dark,360/412/680dp OS200 account loading/error states pass; retain one historical D629 error. Actual title glyphs, lines/ellipsis/bounds, RTL/native Back and44dp Refresh pass on the same zero-opening source21. This is not36 new tests, all names/history/loans, full accessibility or general stability acceptance.
[Actual original failure and exact scope](quality/original-account-detail-covers-and-wrapped-native-title.md).

### D-695 - Original asset-event loading/error covers

The18 en/fa/de, Light/Dark,360/412/680dp OSfont2.0 contexts cover36 states:7 historical unchanged Loading states plus29 new states, including all18 final Error states. Check actual native lines, last-line height, ellipsis, visible bounds and44dp targets. The changed Persian Retry executes once to Ready. Preserve the failed original full-source assertion: only the original default-location row is added;20 other collections are exact. Remaining17 contexts compare all21 collections to that accepted checkpoint. This is bounded component-cover evidence, not all-new-package observations, shipping accountless onboarding, wholeDB/key, general stability or full accessibility acceptance.
[Actual metadata delta and restoration correction](quality/original-asset-event-covers-and-growing-retry.md).
