# Home financial-source publication revision (D-509)

## Reproduced defect

The original complete read and final validation guard plans/goals, file authority, accounts,
categories, settings and quick-resource definitions. They do not compare later ledger, budget,
rate or holding writes. An actual application-flow regression captures an empty Home ledger,
commits one original expense, and observes ValidateHomeSnapshotAsync accepting the stale packet.
Retain that failing result (expected HomeSnapshotSupersededException; none thrown).

## Atomic local correction

Migration20261024115108 adds HomeFinancialRevision, one local SQLite bookkeeping row with an
opaque random32-hex revision. Eighteen AFTER INSERT/UPDATE/DELETE triggers cover Entries, Budgets,
BudgetCategoryLimits, ExchangeRates, AssetTypes and AssetEvents. The marker changes in the same
transaction as each financial mutation, including raw SQL/bulk writers, and rolls back with it.
It contains no amount, user identity, key, account, timestamp or external tracking identifier.

Read the revision first under the original complete-read transaction; all later rows belong to
that same SQLite snapshot. The original final validation reads its current revision after its
strict actual-file/access/unchanged-definition-input checks. A changed financial source raises
the existing classified supersession exception; HomeRefreshSession rereads under the original
cover with its existing finite retry policy. Never post money, change formulas or bypass rights
to refresh Home. This avoids a second complete financial-table materialization. It is not a
measured speedup or acceptance of the two-second/ANR target.

This is migration-owned SQLite bookkeeping, not a new EF financial entity. The generated migration
target model equals the preceding financial model; no compiled-model change is needed. Future
SQLite rebuilds of these six tables must preserve/recreate the18 revision triggers explicitly.
Logical financial exports/sync do not gain a new record or portable preference. Whole-database
backups naturally carry this internal row/table under their existing protection. No new network,
SDK, permission, public endpoint, key policy or financial field is introduced.

## Focused verification

Twenty-one new distinct actual-source cases pass in selected continuations: the original committed
expense regression,18 insert/update/delete cases across the six tables, transaction rollback and
a covered reread that executes its accepted financial writer exactly once. Preserve two fixture
errors: initial raw-table preparation omits a required holding location; later deleting a type
still referenced by a holding correctly refuses. Correct only those unaccepted fixtures; retain
17 successful trigger cases and rerun only the final unreferenced-type deletion case. Two earlier
rollback/covered-reread successes are also retained without replay.

Seventeen original Home refresh/source-boundary cases,96 original complete Home-read cases, the
first-schema account/entry upgrade and the existing upgrade/portable-restore/no-paid-rights case
all pass. These115 related cases verify the affected validation/migration boundary; no complete
managed suite or unrelated successful financial cohort is rerun. No whole-suite count is claimed.

Strict Windows and complete signed dual-architecture Offline Android builds have zero warnings
and errors. Shipping APK is96,997,279bytes,384 AOT modules,408 ELF libraries/1608 load segments;
v2/v3, official ZIP16KiB alignment and ELF16KiB static checks pass. Offline privacy guard passes.
SHA256: fd4291f06c8dd7808e3832e6d0544e115644ad1369eefe0892aa2e1d904f2998.
This is not minimum-API or16KiB runtime acceptance.

## Native protected source, retained failure and continuation

A separately identified complete ARM64192-AOT Offline caller owns only its fresh protected
financial profile. Compile the original Home page/view model/data adapter and use its actual
QuickAddTargetText change after the original source read, while Loading is true/IsReady false.
The independent host alone pauses that notification for one original expense writer. Opening100
and the sole accepted expense10 belong only to this fictitious source; no Main writer or native
financial-form acceptance is claimed. No production timing hook is added.

Original PID10072/run1bf0b21bf3f7432d85fceae7226fc87b commits its expense once. The terminal
exact-publication-count/pending-task guard fails and remains a real failed qualifier. A capture
from that same live process shows the original native Home90.00EUR and correct original account.
The failure did not persist each predicate's value, so its exact predicate and publication count
are unaccepted; do not assert an uninterrupted one-publication pass or replace it with unit tests.
The future observer waits for both ready state and completion and persists all counters, but that
fresh financial cohort is not replayed to repair this observation.

A complete signed no-reset update of only this new caller continues the same protected source
under explicit home-financial-resume. It requires the original failure/accepted-writer receipts,
owned profile/current identity and unique original base. It verifies opening100, exactly one
original expense10, amount/title/date/account and current balance90 with the actual original
Home read/validation. It has no preparation/account/entry writer. PID11146 reports one retained
accepted writer, zero replays and read-only completion, then returns to the unique base and deletes
only the owned profile. Original native publication-count acceptance remains open; the existing
unit race and final native amount/source checks have their narrower scopes. Preserve initial
missing MoneyText namespace and profile-id type compile errors, both before their respective
native invocation. No previous financial action is reseeded or replayed.

Retain original caller7ac758c0f6010c6826dbd14390ea04bb62bac037d06fe4045150c862aaddf7e9
and continuation d672a358266bc45bc39249f27f19ca300aa95cf72eac797eb8b73286c9d65a96.
The latter is56,242,524bytes/192AOT/v2/v3. Uninstall only this new identity after terminal source
retirement; keep the old retained independent Home package/data untouched.

## Main adoption and cleanup

One complete signed no-reset Main upgrade adopts D-509 after fixture retirement. Its actual
original public three-profile names/current selection match before/after; final Home979 and all141
public tuples equal the captured D-507 baseline. APK hash matches the verified new candidate,
MainPID12153, and font1.0/size/density/accessibility/services match exactly. Do not infer complete
private database/key equality from public UI evidence. An early copied observer's null PID was
unread, not process absence; a separate pre-upgrade live-process witness is retained.

Remove15 new owned remote XMLs only after local/remote SHA256 equality; retain local originals,
failed native report and accepted source evidence. Clean both focused test output trees and the
owned native/shipping/Windows build trees with dotnet clean: zero warnings/errors. Remove only
new unsigned APK/resource-designer copies (154,437,238bytes), retaining signed evidence packages
and the original failed caller archive. Previously rejected D-507 file removals are not retried.
Release the coordinated Samsung turn. Existing accounts, Main financial data, system settings and
other projects remain untouched. General stability/performance/accessibility/security/platform,
provider/company/Store/expert and release gates remain open. Continue genuinely uncovered work.
