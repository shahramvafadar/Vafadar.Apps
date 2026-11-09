# Failed CI settlement cases and explicit source Retry - D-942

## Exact failed evidence and current source

The one D941 run response is consumed late on9November, not within15-18minutes:
[CI42408578076](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42408578076)
completed/failure and
[CodeQL42408577991](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42408577991)
completed/success. No second run-status request or rerun follows.

Latest-attempt job metadata shows Libraries and tests failed only its Test step;
Android Release/billing-sandbox builds and Windows build/guards succeed, while
iOS is skipped. Only the failed job128511621541 decoded log is fetched. Its exact
two failures are SettlementCommitFlowTests changed stored advances at line119
and rejected SQL insert at line155, each expecting CanEdittrue but observingfalse.
Failure excerpts and job metadata are retained; successful job logs are not read.

The failed commit cf726915 test blob and four relevant current product blobs
match the original failure source. The current original model invalidates its
review and covers the form after rejected writes; the actual XAML exposes source
Retry while that cover is active. This is the intended D938 source-ownership
contract. Re-enabling stale input or weakening source guards just to satisfy the
old assertions would be incorrect.

## Preserve the complete failed contracts

Only those two existing failed tests change. They now require Failed/CanEditfalse/
CanSavefalse, retained raw draft/dirty state and zero navigation before explicit
Reload. Complete migrated raw-table equality proves no rejected-write delta.
Changed history is re-reviewed on that same original file and keeps the draft.

The SQL case additionally observes the actual original-context SaveChangesFailed
SQLite19 trigger refusal. After dropping only its temporary trigger, explicit
source Reload preserves the complete row snapshot and returns Ready/editable.
Its previously unexecuted post-refusal continuation then accepts one new400minor
expense, original account/date and retained advance note. That accepted fixture
is terminal; no further Save/Undo/Reload follows. The positive recovery phase is
preserved rather than deleted to make the test easier to pass.

Both cases prepare minimally sufficient raw fictitious rows rather than replaying
successful default-category/account/plan/entry setup commands. Only the two failed
methods execute, one selected result each; other successful tests and financial
qualification are not rerun. No test is skipped or disabled, no product code,
financial/source/commit policy, model, migration, runtime or GC changes.

## Original native SQL failure and Retry

One fresh API36/x64/kernel4096 cohort runs PID2892/UID10216,
boot3ee019c7-15a4-492c-a479-250359090e10, actual320x640/density160. Original
Shell modal/page/model/store/native actions and original keyed SQLite contexts
remain. A private factory observer only listens to SaveChangesFailed, without
substituting SQL, failures or persisted rows. One distinct raw zero-opening account,
expense plan and123minor advance prepare this missing native branch.

The real original Save button is tapped once. The temporary SQLite trigger aborts
the actual original insert with SQLite19; one failure event, no accepted bill,
covered Failed state and raw2.34 are observed. Complete20typed source is still
exact, SHA256E9C5AE9FB9671CE960357F495ADBB1175F60280C07442B03691BBB209C46EFD7.

Only that fresh trigger is dropped. A declared fixture-only advance edit123to124
changes the expected source; every other field in all32entry fields and every
other20component value stays exact. The original schema structure restores, while
its real cookie96to98 is recorded without resetting or normalizing it. This is
not a schema/DB/WAL/key byte-equality claim, or a weakened product cookie check.
One actual native Try again then returns Ready/editable with retained raw2.34,
paid1.24EUR and difference1.10EUR, no error or acceptance. Expected source SHA256
403A4B22D6E6DFEF19949FA65910295ADFBA182FBC552022AE648632D184ABB6.
The base equality is false after the declared input change; expected-delta equality
is true. No second Save or native successful financial operation follows.

The original Onboarding root, five OS values, locale/key presence, geometry, boot
and reverse rules restore. Three XML files74951bytes hash-match retained local
originals before exact-path remote removal. Only fresh UID uninstalls; host21440
exits and official scoped AVD retirement removes24files/4558238953logicalbytes,
leaving zero AVD files. Six small userhome files remain. Sparse logical image size
is not physical freed space. Existing ADB50972/creation UTC identity stays exact;
no reset/start/reconnect/pair or retained-fault/phone action occurs.

An initial cleanup observer reads the UTF8 root receipt with default cp1252 and
refuses before issuing any XML removal, package retirement or console stop. Those
three issuance counts are verified zero. Explicit UTF8 reading continues only the
unexecuted cleanup phases; no deletion, install, source setup, UI action, successful
test or financial operation is replayed to repair this observer.

## Qualification, cleanup and limits

Used private APK SHA256
c06850e44d8b66194a786142e6cf85aaac9b3804d937bee79215938bfa776d20,
56958882bytes, passes192AOT/204ELF/804staticLOAD, original x64 Mono identity,
v2/v3 signatures, ZIP CRC and nondebuggable/offline policy. Four original App,
two actual consumed Data and two private observer/bootstrap compiler documents
match actual PDB/current source. This is selected source/compiler and static16KiB
admission, not wholePE identity or hardware16KiB/physical ARM64 proof.

The two existing failed methods pass separately with minimum expected count1 each.
There are zero new product unit tests; this is not a full suite or all old financial
qualification. Three scoped standard Cleans pass; used signed package/source/PDBs,
raw observations and failed-job evidence remain. No supplementary/frozen deletion
or new external output root occurs. Product source is unchanged, so already
successful Windows/full product Android/billing builds are not repeated.

The full delivery goal and broader original-fault/ANR, whole performance, security/
key/private DB, physical ARM64, accessibility, platform/provider/Store and release
gates remain open. Main, phone, retained5606/Q02/native records and frozen scopes
are not used, restarted or cleaned by this step.
