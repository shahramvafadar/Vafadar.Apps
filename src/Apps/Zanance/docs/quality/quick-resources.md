# Reviewed quick templates and saved queries - D-154 / AT-156

Status: implemented and locally/native verified. ENT-02/03 remain in progress; local-profile choices, remaining
operations/import/restore/Home affordances, commercial activation and external release gates stay open.

## Original packet, selection and paired writers

ResourceChoiceStore supports explicit QuickTemplates and SavedFilters choices in the existing scoped table.
Every original identity and full serialized resource packet remains visible; no subset is guessed after a
bounded downgrade. Reviewed reads and Save recheck the actual file/access, choice revision and value and
complete original packets, including changes at identical timestamps. The generic financial chooser now has
six translated growing destinations; retained query captions explain history use instead of implying hidden
money. Help explains selected draft activity and saved-definition slots in all six languages.

QuickResourceReview pairs both choices with all original accounts/categories, nullable raw Settings and an
optional complete source entry. It pins the original name comparison culture and complete input stamp. Opening
creates no Settings. The actual Save/Delete overloads validate under their SQLite writer. New definitions count
chosen slots, not every retained historical row; an explicit empty choice is valid. A committed new item adds
its identity to that choice atomically. A confirmed same-name filter replacement transfers only its selected
slot. Same-id corrections and deletion remain separate from new slots. Malformed values preserve history and
corrections/deletion while requiring explicit repair for new activity. SQL failure restores resources and choice
together. No schema/entity property changes or new migration/compiled model are required.

QuickResourceWorkPolicy separates selected template draft activity, fresh definitions, retained correction/
delete and history-only saved query use. Viewing a query never filters the ledger or grants transaction/account
rights. Applying a template only fills an unsaved draft; the existing account/ledger writer remains authoritative.

## Actual application paths

QuickResourceActions is the actual non-UI source used by Templates, EntryDetail, EntryEditor and Transactions,
compiled directly in App.Tests with real SQLite/localization and explicit native-effect ports. It validates
before/after dialogs and under the paired writers. A template comes from the exact original displayed source;
its unsaved name survives a rejected request. A current filter is copied completely before the menu/name
prompt, including custom dates, account, category ids/order/label, kind, review/totals and search. Later typing
cannot change that request. Duplicate retained names use unique ordinal options mapped back to original ids;
deletion requires confirmation. A full filter quota still permits a reviewed same-name correction.

One busy guard covers dialog/write/refresh. Failed or retired requests require an explicit fresh read. A
committed request retires before native feedback/refresh, so failure never resubmits its writer. The template
list uses a shared covered publication and explicit Reload; names wrap rather than truncate. Original retained
rows remain visible and original deletion is independent of creation slots. Entry template chips retain every
identity and separate selected activity enablement. Entry setup reads nullable original preferences without
creating a Settings row. Release commercial registration remains inactive and unrestricted.

## Verification and limits

29 new AT-156 real application/SQLite cases cover complete read-only opening, persistence/restart, explicit empty
choices and atomic new slots, full capacity, retained corrections/delete, unchanged-timestamp resource and
choice-value mutations, actual chooser captions/Save, complete reviewed replacement, pending prompt/busy draft
capture, changed source and delete confirmation, committed-refresh failure, unsaved template application,
cancellation, malformed choice repair, SQL rollback and retired file/rights. The main run has 2,735 cases, with
2,734 passing and one outdated four-kind expectation failing. Updating that expectation to six and rerunning
that one case passes: all 2,736 behaviours / App.Tests 679 are verified, zero skips; no unchanged whole-suite
rerun was performed. The earlier SQL trigger prototype named the wrong fictitious table; both repaired rollback
cases pass and the ordinary main run passes them. One additional targeted real-SQLite case verifies duplicate
restored filter names: choosing the second ordinal deletes only its original id and preserves the first full
query and every original ledger row. That case passes, bringing the verified total to 2,736 / App.Tests 679.
Test output is cleaned.

Strict Windows and canonical complete Android Release builds have zero warnings/errors. Eighteen hidden Windows
language/theme/width cases cover en/fa/de, light/dark, 360x800/412x892/1280x900, actual six-kind/row/Save targets,
complete native names, original selection, explicit saves and cancelled template deletion. Complete 25-table
independent fixture rows and exact development DB/sidecars are restored. An initial actual template caption
capture exposed truncation and the production title now wraps. A later queued kind switch initially observed
an unarranged native caption; the focused Debug review now waits at most one second for current native layout,
rejects retired pages and records native geometry before checking. Completed cases were retained; only missing
cases resumed. Failed captures remain negative evidence, not acceptance.

Six normal complete signed Release Android en/fa/de/light/dark cases cover the actual Settings entry, both new
kind buttons, every owned original without a subset/Save, real template deletion confirmation/cancellation
and the actual saved-filter menu/cancellation. The English/light case also fills and discards a new unsaved
entry with the original template amount/title. Initial helper assumptions about upward kind navigation, native
amount descriptions, modal Cancel and the closed More filters section were rejected by current hierarchy;
only the remaining read/draft/menu stage resumed. No financial/resource Save was repeated.
Complete 25-table comparison preserves all original template/filter/reference/preference/ledger rows apart from
the existing D-141 migration. Exact owned QA03 DB/sidecars and Main/English/System are restored; final Release
installed/stopped. FLAG_SECURE stays enabled, private adb 5038/tcp5571 only; no device settings change.

Final Release portable-PDB hashes match all six changed application sources. APK:
`artifacts/android/zanance-d154-release.apk`, 82,229,333 bytes,
SHA-256 `2ab014b74f3d6e3ac68239e44939332b082b285dc2f27c45df7e656091efbe34`;
package `pro.vafadar.zanance`, non-debuggable, complete ARM64/x64 stores/App AOT, ZIP integrity and v2/v3 signature
under the existing local test certificate. Build proof/CI is not physical-device, iOS, OAuth, commercial
activation, store signing/publication or complete entitlement acceptance.
