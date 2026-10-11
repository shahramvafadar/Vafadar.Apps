# Selected budget definitions - D-148 / AT-150

ENT-02/03 remain in progress. This slice supplies canonical budget choice persistence and writer bindings,
not the chooser UI or effective presentation/alert bindings. Current release registration stays inactive and
unrestricted. No financial schema/model property, migration, compiled model, SDK, permission, portable security
preference, paid grant or commercial activation changes.

## Original data and separate work

BudgetSelection stores exact financial scope, normalized currency, period kind and canonical account set.
Row ids, calendar, method, limits and copied-month dates do not create extra definitions. Immutable canonical
values preserve cached fact equality; parse accepts only the exact sorted representation. The existing scoped
choice column's 8,447-character input guard is retained, with a distinct definition encoding for BudgetDefinitions.
No Guid hash stands in for a financial definition. BudgetAvailability evaluates original current periods through
BudgetPeriods.Range, the injected local date and stored month-start preference. Missing over-capacity, stale and
malformed limited choices enable no new work; unlimited upgrades ignore retained limited choices.

BudgetWorkPolicy independently checks basic/advanced methods, rollover and weekly periods, exact membership
and host activity. Selection never grants a paid tool. All original periods/limits remain present for history,
carry, forecasts and calculations; retained corrections remain available even on unselected paid budgets.
Future copies of a selected definition become eligible on their actual financial date; other definitions never
acquire automatic selection. The actual view/alert consumers follow separately.

## Actual-file reviewed choice and writers

BudgetChoiceStore reads every complete original period and limit plus the actual file, local date, month-start
preference, cached access and stored revision/value. It creates no Settings. Reread before publication rejects
mixed reads. Save rechecks complete original metadata and choice revision/value under the SQLite writer, then
saves only an explicitly reviewed current definition subset. Exact empty, no-op, stale/ABA, equal-audit-time
financial changes, foreign file and retired access have distinct checks. Changed follows a changed commit only.
Stored choice overrides only the corresponding cached choice, never purchase or membership facts.

Actual budget Save/Replace reads chosen definitions under its existing writer. A new current definition joins
one available chosen slot; an explicit scope change or replacement transfers a slot only when its old definition
has no other current row. New paid copies still require their feature. Existing same-definition corrections and
retained replacement remain available without reviving new work. Corrupt choice does not block retained edits;
explicit reviewed Save repairs it. Delete now uses the actual-file writer and DeleteData membership right, releasing
only the last deleted current definition. Failure rolls back budget, owned limits and selection together.
No choice action creates entries, moves funds, changes totals or picks a first budget automatically.

## Evidence

34 new AT-150 cases (15 Core / 19 Data) exercise canonical identities/immutability/input bounds, explicit empty
choices, selected current/future periods, paid tool separation, upgrades/shared expiry, complete original data,
durable reopen/no-op, above-retained-count chosen capacity, correction/copy rights, net transfer, duplicate current
definition protection, complete SQL rollback, last-definition deletion, live financial dates/month start, stale
metadata/ABA/value-only edits, malformed repair, foreign scope/file, cached override and two independent SQLite
writer races. Existing 26 AT-123 cases still pass. Final normal parallel main 2,594 / App.Tests 537 pass with zero
failures/skips; test output cleaned. Strict Windows/Android and canonical complete Release pass without warnings.

Normal signed Release on the exact owned emulator profile saves one EUR monthly budget beside its original USD
budget, opens/closes its unchanged editor in en/fa/de light/dark, and explicitly deletes only that new EUR budget.
All original complete financial/metadata rows match afterward; ResourceSelections stays empty and only the existing
D-141 migration is added. Restore exact original owned data/sidecar bytes and Main/English/System; final signed
Release remains installed/stopped. This proves unrestricted native writes, not an enabled native chooser.
No UI layout changed, so the completed layout matrices are retained rather than rerun.

The first post-profile-switch hierarchy was temporarily unavailable. The same retained Home was reacquired after
the preparation process ended; preparation and Save were not restarted. Keep these XML failure records as negative
evidence, not a product fix or acceptance. The first editor helper expected a bare value; the native node carries
the semantic field caption followed by the actual input. Read that complete retained node, verify its value and
resume only the unfinished case, without another Save. No desktop input, system-setting change or physical device was used.

Logs: artifacts/budget-choice-{baseline-tests,all-tests-final,test-clean,windows-build,android-build,apk-build}.log.
Native records: artifacts/budget-choice-native/final-proof.json and fresh retained hierarchies.
APK proof: artifacts/budget-choice-apk-proof.json.

Phone artifact: artifacts/android/zanance-d148-release.apk; 81950805 bytes, SHA-256 `118a0fd98bade9e35626df06b23e703e06dbf30ffec1dcff4b8edc91777e37d3`.
Full ARM64/x86_64 stores/application AOT, ZIP integrity, non-debuggable pro.vafadar.zanance, min 24/target 36 and
v2/v3 signature verified with the existing local test certificate. This is not Store signing, physical-device,
iOS, future native alert, provider, performance or product-release acceptance.

Next: the reviewed budget definition chooser and effective original budget/view/alert binding, followed by
remaining resources, operations/import/restore/native paths and commercial messages. ENT-02/03 are not complete.
