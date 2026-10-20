# Original protected Android Home review

This independent Release/AOT/trimmed caller (x64 emulator by default, explicit arm64 phone override) compiles the original application sources/XAML
and supplied production native package. The application never references it. Its distinct package
is `pro.vafadar.zanance.androidhomereview`; it must never run under the main application identity.
It keeps foreground screenshot protection, has no Internet permission and does not enable debugging.

Build with the existing Android workload and native package:

```powershell
dotnet build eng/benchmarks/Zanance.AndroidHome/Vafadar.Zanance.AndroidHome.csproj -c Release -f net10.0-android -p:ContinuousIntegrationBuild=true
```

By default use only the repository-owned `Zanance_Storage_Review` emulator (`emulator-5570`, adb port 5038).
Check its AVD name first. Verify the signed APK's package, signature, embedded AOT and qualified
native-library hash before installing that exact independent package. Resolve its actual activity
with `shell cmd package resolve-activity --brief`; never guess the generated Java activity name.

Launch once with `--es review_phase prepare --es review_run <new-32-hex-guid>`. This creates fresh
private, marked reference (10,000 entries, 20 accounts, 100 schedules) and tenfold (100,000, 200, 1,000)
fixtures through original migrations, then protects them with the actual Android generation/key
implementation. Preparation resolves the Android OS app-data alias before unchanged no-redirect
fixture guards. Existing roots are refused. No main package data is read or moved.

Record the exact PID and observe its public report, without clearing logcat or reading private files:

```powershell
./eng/benchmarks/Zanance.AndroidHome/Read-ReviewReport.ps1 -Run <guid> -Phase prepare -ProcessId <pid> -OutputPath <fresh-artifacts-file.json>
```

The observer only reads the exact live PID/tag/run's bounded public report. Each message holds an
indexed Base64 fragment of derived JSON, not key material or exception messages. The decoder refuses
conflicting/missing chunks and overwriting output. Observation timeout does not restart a process.

After successful preparation, force-stop only the independent package and launch its `measure`
phase with the same run id and a fresh `--es review_pass <32-hex-comparison>` identity. Record the new exact PID
and observe with `-Phase measure -Comparison <comparison>`. Measurement
requires existing ciphertext/identities/keys, never reseeds data or creates replacement keys. Failed
attempt reports and private failure files remain retained. Fix the observed cause, then continue
the same prepared fixtures; do not rerun preparation or reset app data. Completed language/shape
cases in private progress are retained and skipped; full final preservation still runs for both
shapes. Never reuse completed metrics after changing production sources; that requires a separately
identified comparison against the same retained financial data. `measure` captures the canonical financial
presentation; after an intended production change, `compare` uses a fresh `review_pass` and an explicit
`review_baseline` naming the original presentation packets. Omitting `review_baseline` retains the original
same-id behavior. Separate comparison identities prevent stale completed cases from becoming new timings.
Per-comparison progress and immutable final report names preserve every prior
result. Each final result records the loaded Data/caller module identities; do not reuse timings after a rebuild.

The original Home page runs twice in English, Persian and German for each shape. Navigation,
read/projection/publication and actual Android layout/frame callbacks are timed; complete native
balance text and actual covered publication are checked. It also measures dispatcher gaps and
compares the actual protected database directly to its existing authenticated encrypted rollback after timing,
including complete schema/header/integrity/typed rows. No in-memory backup image is held during measurement.
The raw read profile separates physical stepping from entity materialization, and reports the actual compiled
ledger constructor parameters without exposing keys or rows. No financial Save occurs. Stop the exact independent package
after reading the completed result. Preserve its fixtures for later same-data comparisons.

This is a loaded emulator baseline, not cold startup, hardware-rendered/GPU frame completion,
physical phone speed/ANR, arm64/API24/16 KiB OS, iOS or two-second product acceptance. Do not run
compilers or intrusive UI observers while measuring. No unchanged production APK/full test suite
needs repetition for this independent caller alone.


The D-306 `guard` phase uses the same run and explicit review_baseline with a fresh review_pass. It checks
one original native English Home publication per shape, rejects changed in-memory plan/goal reviews and
accepts that same review after exact restoration. It compares the complete raw original database afterward.
No DB Save, new preparation/key, repeated native-stepping profile or statistical performance claim belongs
to this phase. Observe its exact process with Read-ReviewReport.ps1 -Phase guard -Comparison <original-pass>.

## Separate unreviewed workload (D-373)

The original reference/tenfold corpus contains confirmed entries only. It cannot qualify the
unreviewed account-scope path. Use `prepare-unreviewed` only with a new run id: retain the same
original shapes/fields, mark that fresh plaintext fixture's rows Unreviewed before the original
protected bootstrap, and require the exact updated row count. This never modifies an existing
protected workload or regenerates its keys. The preparation report includes the actual Core MVID
and two original AOT calculator overflow refusals without stored writes.

Use the existing `measure`/`compare` phases on this newly protected run. Capture current complete
financial presentation using the original-reader/control package, then compare the changed
calculator on the same ciphertext, keys, date, languages and shape. Preserve full schema/typed
rows, actual Activity focus, covered publication and native-layout checks; unreviewed counts,
confirmed balances and all other financial fields remain in the canonical presentation. Keep
failed/partial observation receipts and the original live PID. Never claim this distinct workload
improves the previous all-confirmed cohort or closes general device/ANR/startup/release gates.

## Original native saved-filter actions

The `filters` phase qualifies the original TransactionsPage/TransactionsViewModel and unchanged saved-query
commands using a separate fictitious `filter-actions.db` under its explicit review run. It does not prepare
or measure the protected Home reference/tenfold workloads. Pass an explicit run/comparison to MainActivity,
then use Read-ReviewReport.ps1 with the same `-Phase filters`, `-Run`, `-Comparison` and actual `-ProcessId`.

The native original Shell must attach before theme/language root changes; logical publication alone does
not prove Android fragment attachment. Real accessibility nodes, visible native rectangles, caption layouts
and native button clicks are checked in en/fa/de and light/dark. Complete original fixture packets must be
unchanged after setup. No main data, system setting, security flag or financial Save belongs to selection.

After an actual failed caller, preserve its packet, original run directory and exact financial inputs. A
corrected caller can continue that same fixture after explicit value validation, with a fresh comparison
identity preserving the immutable failure packet. Completed comparisons refuse repetition. Observation
failure does not prove process death: verify the original authoritative process rather than restarting it.

## Original protected schema callback (D-309)

The `schema` phase takes the already prepared protected reference/tenfold run and a fresh `review_pass`.
It refuses absent original ciphertext, identities or keys and calls the actual production
ProtectedProfileStorage.UpgradeAsync on the original closed unpooled keyed connection under drained ownership.
It reports physical Open state transitions and callback elapsed time, then compares complete raw schema,
headers, integrity and typed rows against the existing encrypted rollback. It never prepares a replacement
fixture, saves money or changes the KDF. Observe its exact live PID with `-Phase schema -Comparison <pass>`.

For a production change, build a new caller and use a fresh comparison against the same retained run. Keep
before/after process/module identities and failure packets. A timeout is not process death. This phase is
not whole Home startup, rendered-frame, statistical timing, phone, iOS or two-second acceptance.

## Original verified purchase lifecycle (D-310)

The `billing-owned` phase uses a fresh explicit run/comparison and an independent SQLite fixture. Its
fictitious signed-metadata effect port never calls a store, payment, restore or management. The actual
production ProductPlansPage/view model/formatter render paid-through, grace, scheduled change, Lifetime and
cached-verification facts in en/fa/de and light/dark. Check original Android text layouts/ellipsis/geometry
and original stored settings/accounts/entries; never weaken flags, change device settings or use main data.
Observe the same exact live PID with `-Phase billing-owned -Comparison <pass>`. Preserve failure packets
and continue the same fixture after a caller correction; completed comparisons cannot be repeated. This
is native presentation qualification, not genuine signed-provider/store or physical/iOS acceptance.

## Original financial tables (D-311)

The `tables` phase uses a fresh explicit run/comparison and independent fictitious SQLite file. It renders
actual ReportsPage trend/history and SnapshotPage comparison/decomposition templates with canonical
MoneyText packets and original date formatting. Check native full text allocations, horizontal sign/unit
scroll ends and original settings/accounts/entries in en/fa/de and light/dark. In-memory stress packets do
not replace financial algorithms or persisted snapshots. No financial command or real source/provider call
belongs to the phase. Observe the exact live PID with `-Phase tables -Comparison <pass>`; timeouts retain it.
Never reset main data, recreate a failed fixture or change system settings/security flags. Completed
comparisons refuse repetition. This is not current phone, OS scale, reader, iOS or product acceptance.


## Owner-approved physical Android workload (D-314)

The owner authorized Samsung SM-A176B for fictitious-data qualification. Coordinate an exclusive phone
turn with other projects before installing, launching or driving this package, and release it afterward.
Build the complete independent Release package with the additional -p:RuntimeIdentifier=android-arm64.
Verify its exact independent identity, local signature, AOT, absent INTERNET/debugging and reviewed ARM64
SQLCipher bytes before a data-preserving install. Never clear/reset/uninstall the main application.
No system setting, screenshot-security exception, owner credential or personal data belongs to this run.

Read-ReviewReport.ps1 retains its original emulator guard. Explicit -PhysicalDevice -DeviceTransportId N
selects only the observed approved Samsung model and refuses an emulator; transport metadata is runtime
input, never a committed hardware identifier. Observe the exact PID and public result only. Lost observation
or a timeout never authorizes restarting a live phase or regenerating its ciphertext/keys.

A fresh prepare creates the same independent reference/tenfold workloads. The existing measure phase then
qualifies original loaded Home publication and full raw preservation in en/fa/de; its hardware/runtime
metadata distinguishes physical ARM64 evidence from the earlier x64 baseline. These timings exclude the
initial empty-install startup and preparation, not proof of complete loaded cold startup or the two-second target.

The new ledger phase uses that same retained run and a fresh review_pass. Original TransactionsPage and
native SearchView deliver exact/empty/wide queries through their real debounce, grouping and native layout;
the exact row must be realized. Four original validated store saves per shape are timed separately, read
back, then only their exact owned rows are removed. This is the production data write, not a timed native
Save-button/form interaction. Complete portable schema/typed rows before/after must match. Private sync
journal evolution is deliberately distinct from the unchanged financial portable projection. Before Save,
the exact fictitious pending intent is retained privately. If interrupted, preserve it for explicit same-fixture
inspection rather than guessing cleanup or repeating Save. Completed shapes are retained and skipped.

Largest dispatcher gaps cover the actual ledger phase and caller assertions; they are diagnostic samples,
not a statistical Android ANR or low-memory acceptance. Full cold startup, OS pressure, genuine provider,
spoken accessibility, other API/page-size matrices and iOS remain separate requirements.


## Same-data read diagnosis and read-only native ledger (D-315)

read-profile requires the retained protected run and a fresh review_pass. Set review_baseline to that
same pass for the initial actual-model timestamp/raw-CLR diagnosis. A new after pass uses the original
baseline identity and skips those already qualified component loops. Both actual public reads always
compare complete ordered entries and portable schema/typed rows before/after. Stream JSON digests to
a null sink, never a plaintext financial file. Compare original and final digests on the host.

ledger-read uses a fresh pass on that same run and original native TransactionsPage/SearchView/grouping.
It retains the ledger phase's exact/empty/wide native and portable-preservation checks, but deliberately
executes zero successful financial writes again. Per-shape progress validates the expected 0/4 write
count before continuation. Observe exact live PIDs with -Phase read-profile or ledger-read, same Run and
Comparison, explicit PhysicalDevice/DeviceTransportId. No reseeding, reset or new keys; observation timeout
is not process death. Finished comparisons refuse repetition. Actual receipts: app quality/physical-ledger-read.md.

## First-plan native publication (D-318)

plan-publication is a separate original protected-installation diagnostic, not a marked performance workload. Require the independent package and nonredirected run root, initially zero accounts/plans/entries and no prior intent. Record its fictitious account intent before setup; invoke actual original editor/native Save once and report stored count, VM/binding count, adapter/child count and realized caption independently. A blank native row does not imply missing persistence.

plan-publication-fixed requires the same retained account, original intent, one known unpaid definition and zero ledger entries. It writes an exclusive plan-retirement receipt before deleting only that definition through the original history guard, then exercises a new first-row transition with the original editor. It never resets/reseeds the installation or replaces keys. Native caption realization is required; preserve all failure reports and definition receipts. Use a fresh comparison id, the original run id and Read-ReviewReport.ps1 with the exact already-running PID. On the approved physical phone use explicit ARM64 build, PhysicalDevice and DeviceTransportId opt-in, after exclusive-input coordination. See [evidence](../../../src/Apps/Zanance/docs/quality/first-plan-publication.md).


## Original public Home boundaries (D-320)

home-stages uses the retained protected performance run and two distinct review_pass identities. For the
baseline only, review_baseline equals review_pass; an after comparison supplies that original baseline.
Existing per-language complete financial presentation files are never overwritten. This phase skips the
already qualified raw native stepping loop and records existing public VM/property/collection events before
native bindings subscribe. Navigation, final model publication and Android layout/frame completion delimit
loaded Home readiness; notifications approximate stages rather than timing private algorithms.

Capture the exact live PID with Read-ReviewReport.ps1 -Phase home-stages and the matching Comparison.
Retain the same process after observation timeout and the same fixture identities/keys after package upgrade.
No preparation, financial Save, production timing hook, OS setting or screenshot-protection change is involved.
The existing full schema/typed-row and native balance/presentation checks remain mandatory after changed reads.
Do not run host builds during phone timing. See application quality/physical-home-compilation.md for the
qualified comparison and remaining cold-start/performance boundaries.

The managed AssemblyName and AndroidAotProfile inputs match production so its method/type signatures resolve.
The Android ApplicationId and private storage remain independent. KeepScreenOn applies only to explicit review
phases on that window; never change a device setting or bypass a genuine access gate. Historical D-314/315
caller timings do not represent production compilation. D-320 removes only extra CoreLib method roots from the captured profile after same-data native qualification; SDK defaults remain.

Release compiler/task downloads follow the same central servicing pin as the runtime. Restore with the same
Configuration as publish; the normal APK script does this explicitly. Diagnostic exception output is type/stack
only, never exception messages. The independent window stays awake only during its explicit foreground review.


## Bounded read-stage diagnostics (D-321)

The existing read-profile phase additionally observes fixed-table EF execution/reader/open durations for its exact owned connection path and separately times fresh Home validation. It never serializes SQL, parameter values, paths or keys. Duration boundaries overlap and cannot be summed as independent work; EfReadCalls records EF wrapper calls, not the complete direct-reader row count. Complete ordered-entry digests and before/after typed-row verification remain mandatory. Use a fresh comparison on retained ciphertext/keys; a read comparison does not replace final original Home/native/package qualification.


D-322 also records the actual Core module identity alongside Data/caller identities, separating an adopted materializer change from a rejected recurrence candidate. Temporary worker logging/defines and reduced-case loops are absent from the ordinary caller and shipping application. Limited read gains do not establish universal loaded-Home or cold-start acceptance.

## D-339 original report legend rows

The `legends` phase accepts a fresh review_run/review_pass and owns only a separate fictitious
financial-tables.db. Do not run prepare or repeat reference/tenfold performance phases for this UI
change. It renders original category/composition templates in en/fa/de and both themes with explicit
fictitious long names and extreme canonical amounts. Native text, both horizontal ends and actual
target/value separation are checked; source settings/accounts/entries remain unchanged.
Observe the exact original PID with Read-ReviewReport.ps1 -Phase legends -Comparison <pass>.
Observation expiry is not terminal. D-339 retained an actual initial ANR, selected Wait on that
independent emulator dialog and received the completed six-case report from the same original PID.
This is not ANR resolution, OS text scaling, phone/iOS or financial calculation acceptance.

## D-340 translated Flex group titles

Phase budget-titles uses a fresh review_run/review_pass and the existing separately owned
financial-tables.db fixture. It renders original BudgetPage/FlexLine captions in en/fa/de and both
themes, checking actual TextView allocation/ellipsis. Presentation-only IsFlex and rows never save
a budget method. Settings/accounts/entries/budgets compare unchanged. Observe the exact original
PID with Read-ReviewReport.ps1 -Phase budget-titles -Comparison <pass>. No Home preparation,
performance matrix, report calculation or system text-scale change belongs to this phase.
An observed startup ANR remains separate from completed caption checks on that same process.

## D-342 original optional Home category legends

Phase `home-legends` uses a fresh comparison and the retained separate financial-tables fixture,
without prepare or workload repetition. It renders the original HomePage category rows and total
using explicit fictitious long captions/extreme amounts. Check native complete glyphs/scroll ends,
original command/parameter/target and accessible name, detached hidden rows and complete visible
source. No layout or financial input is saved. Observe the original PID with Read-ReviewReport.ps1;
Activity observation expiry is not process failure. Native metadata is not spoken TalkBack evidence.

## D-343 shared original plan rows

Phase `plan-rows` qualifies original Home upcoming and PlansPage collection templates in six
language/theme contexts on the retained financial-tables fixture. Explicit fictitious date/icon
rows and signed extreme captions never save a schedule or entry. Check both amount scroll ends,
original commands/44-unit targets and actual native complete accessible descriptions. The original
empty Plans publication must finish before view-only rows replace it. No prepare/performance rerun,
OS setting change or spoken-reader acceptance belongs to this phase.

## D-344 original ledger/account monetary rows

Phase `ledger-rows` reuses the retained financial-tables fixture with a fresh comparison. Six
language/theme contexts exercise original Home recent/accounts, Transactions item/day net and
Accounts item/group templates:36 complete monetary viewports, native semantic colors/names,
original actions and actual native selection/deselection. Presentation-only extreme and multi-currency
packets never save entries/accounts. Observe the same original process after a transient PID read
or timeout; the recorded startup ANR is not a pass. No prepare/performance replay or OS change belongs
in this phase. The Windows-only rejected root lifecycle candidate does not change Android source behavior.

## Remaining actual OS-scale matrix (D-346)

Phase `font-scale` requires a retained financial-tables.db and actual Activity configuration
font_scale2.0/width412dp. It never changes settings itself. Under the owner's emulator-only
exception, capture prior OS settings and restore them after completion. No phone opt-in is
implied. Use one fresh review_pass and the retained review_run, not prepare or a reset.

One original startup attempts tables, report legends, budget titles, Home legends and plan rows.
D-346 does not establish successful completion of all five in one process.
Each complete six language/theme-case cohort writes an immutable `font-scale-<group>-<pass>-result.json`
receipt before proceeding. On a failed observation, retain the original PID; after an actual
terminal failure, the same comparison can continue only missing cohorts after validating its
retained receipts, including the exact Mono GC mode. A complete aggregate is refused on replay. Ledger/account rows already have
D-345 real-OS evidence and are excluded from this remaining matrix. Observe the final public
result with Read-ReviewReport.ps1 -Phase font-scale and its exact original PID/comparison.

Do not equate font configuration with linearly doubled pixels, complete platform acceptance,
spoken TalkBack or absence of startup ANRs. No shipping app source or financial algorithm changes.

## Bounded native code sampling (D-346)

`-p:EnableCodeSampling=true` adds a shell-profileable overlay only to this independent fictitious
caller. It defaults off and never enters the shipping application. Use exact-PID user-mode
`simpleperf` frame-pointer/code-address sampling with bounded duration/output; never request raw
memory, registers, user stack bytes, dumps or kernel symbols. Verify the perf attribute mask before
retaining the result. A stripped nearest symbol is not exact fault attribution. Match the official
symbol file by both ELF BuildId and GNU debuglink CRC before resolving functions/source lines.
Hash-match and remove only the exact owned remote recording after collecting the public metadata.

D-346 records a real default-GC Mono bridge SIGSEGV and a separate serial-major-GC trial ANR.
The supported `AndroidEnableSGenConcurrent=false` trial is not a production fix and is not adopted.
Do not replay passed cohorts or mistake continued CPU activity/observation expiry for success or
process death. Standalone existing presentation phases can isolate only the missing cohorts on the
same fixture; retain the aggregate failures and never claim all cohorts passed in one process.

## Foreground-native observations (D-347)

The original Activity window must hold focus before root/context construction and synchronously
before every rendered-caption assertion. ANR-covered controls can have valid layouts, so realized
geometry alone is insufficient. Completed receipts now require OriginalWindowFocusRequired before
reuse, alongside the exact recognized GC mode. Earlier unguarded component reports remain
provisional and cannot silently become unobscured-window acceptance.

A separate upstream-recommended precise-Tarjan trial retains the concurrent major collector but
still has an actual persistent mid-budget ANR. The experimental environment file/property and
runtime marker are removed after preserving local evidence; neither enters the ordinary caller
or Main. No change to the native runtime, bridge implementation, crypto, SDK or nursery size is
adopted. Preserve the negative evidence and do not schedule the same ineffective trial again.

## D-350 actual measured Home foreground

measure/compare/home-stages now require actual Activity window focus before each measured page/read
factory and after native layout, before text assertions. Each result records OriginalWindowFocusRequired;
retained legacy cases without it refuse. The unmeasured initial empty shell and cold startup are outside
that contract. Preserve actual ANR failures even when a later resumed/recovered comparison completes.
Never promote a faster isolated store operation without the original loaded-page comparison. D-350
rejects its date decoder; production getters stay unchanged. Current original-getter control completes
all twelve original native page observations with complete financial presentation/typed-row preservation.

## D-351 reimbursement phase

reimbursements requires a fresh independent run/comparison and rejects an existing page fixture
instead of reseeding it. Compare actual unchanged single-entry results against the Core batch for
64/1,024/8,192 reimbursable expenses, preserving original ordered tuples and complete inputs. Timing
is descriptive, not a flaky pass threshold. Then load the original ReimbursementsViewModel/Page once
with an independent fictitious partial refund, require native window focus and title/amount, and
compare complete stored inputs. No financial UI command is activated. This is not large-list rendering,
protected-storage, whole-Home, phone, OS-reader, cold/ANR or release qualification.

## D-352 complete reimbursement-page modes

reimbursement-page seeds one independent4,096-expense/4,096-refund fixture and checks the original
VM/page with a real worker-only context factory, coalesced load and native retry after explicit factory
failure. Retain its result and inputs. reimbursement-visibility requires that completed baseline and
reuses its file without migration/reseed for visible original values/amount endpoints and synthetic
empty presentation. reimbursement-page-fixed combines the full current source contract on the retained
fixture, including original amounts/total, effective ancestor visibility and separate empty-flow
non-overlap. Give its original baseline through review_baseline. Matching completed case files bind
the caller MVID and complete fictitious input hash; reuse them after a real failure instead of replay.
Actual focus is required; a system dialog pauses acceptance on the same process. No phase touches Main
finance, changes OS settings or qualifies whole-Home, cold/ANR, physical/iOS or product release.

## D-353 Home template reload

The home-template-reload phase uses a fresh marked run and comparison identity, a small separate
SQLite fixture and original Release/AOT Home controls. An existing fixture is retained rather than
reseeded. Check same-count native row retention, fresh reordered captions/semantics and original
disabled commands, then counts 2/1/0/2/0/2 and original store reload in en/fa/de and both themes.
Require actual Activity focus and complete stored account/entry/settings/template equality. No
financial command, main-package data or OS setting is changed. This is a scoped lifecycle check,
not cold/GC/ANR, phone, reader, encryption or release acceptance.

## D-355 actual OS Settings text

The settings-text phase requires the owner-authorized independent emulator at actual font_scale2.0
and412dp. A marked separate database retains fictitious accounts/settings across failures; refuse
completed comparison packets. Inspect original Labels, selected Pickers and Entries in en/fa/de and
both themes, plus original default-account popup rows/Cancel, with complete stored inputs unchanged.
The synthetic LockAvailable flag exposes only a caption; no security/permission/financial action is
invoked. settings-text-page retains first-page failures before popup checks. Wait for actual dialog
focus rather than accepting elapsed time. The caller writes no OS setting: its external authorized
controller captures/restores exact previous values. Do not infer all controls, spoken TalkBack,
360dp, physical/iOS, ANR or release acceptance.

## D-356 original modal editor text

The editor-text phase requires actual authorized OS2.0/412dp, a marked separate editor-text.db
and a fresh comparison. Default states are account,debt,entry,transfer,plan,monthly in en/fa/de
and both themes. Explicit review_editor_forms/review_editor_contexts restrict an actual diagnosis.
Retain each completed case with exact caller MVID/stored-input hash; refuse changed or completed
comparisons rather than overwrite evidence. Leaf scope is original Label/selected Picker/nonempty
Entry/Button text, excluding placeholder hints/formatted spans and unshown combinations. Require
the original modal view's native window focus; MAUI uses a separate DialogFragment. Read names
through actual accessibility node info, and exercise only original nonfinancial disclosure toggles.
No editor Save, notification/security operation, Main package data or OS setting write occurs
inside the caller. The external authorized host restores exact captured OS settings.

## D-358 original work-editor continuation

Explicit editor forms additionally accept goal,earmark,goal-share,goal-cut,budget,settlement.
These states use a separate marked work-editor-text.db with two fictitious EUR accounts and
one unposted schedule. Compare accounts/entries/plans/settings/goals/budgets before/after each
complete case. Original source/input receipts and native ancestor geometry retain actual
fractional Flex allocation failures. No editor Save, reminder/security action, quantity-goal
or populated-budget coverage is inferred. Keep terminal runtime faults distinct from passed
native text cohorts; preserve the same fixture and restore exact captured OS settings.

## D-363 protected settings contract

The settings-read phase requires an existing marked root with reference/tenfold protected generations,
identity catalogues and original keys. Never prepare/reseed a replacement to recover a failure. Pass
review_run and a fresh review_pass; a completed same-comparison receipt is refused. The phase runs on
a worker after original covered startup, compares all25 original settings properties with independent
EF and actual exclusive-maintenance reads, refuses cancellation and retired generation key owners,
and compares complete schema/typed rows in in-memory portable snapshots. It writes no financial data
or replacement keys and touches no main installation or OS settings. Observe the exact live PID with
Read-ReviewReport.ps1 -Phase settings-read; an observation deadline never permits a restart. This is
correctness/protection evidence, not whole-Home timing, phone/iOS or release acceptance.

## D-364 minimum-width editor continuations

The editor-text phase accepts actual OS font2 with360dp or412dp width. review_editor_controls
is all by default; entry-viewport,changed-actions,onboarding-footer select only affected original
controls. Explicit forms additionally accept debt-details,onboarding-account,onboarding-step1,
onboarding-step2. review_editor_fixture work-editor-text reuses the existing marked two-account
fixture. Bind review_editor_baseline and review_editor_baseline_mvid to its original private receipt
and exact stored-input hash before a scoped continuation. A missing360dp fixture is refused.

Editable viewport evidence uses original native caret scrolling and actual bundled glyph bounds;
it restores text/selection without a replacement input or scroll policy. Disclosure checks invoke
only the original native command twice and compare the original draft. Footer checks all original
step commands/names/targets and busy disablement without Back/Next/Finish or financial Save.
Preserve baseline failures, exact emulator settings and original process/comparison/MVID receipts.
These are native cases, not a managed-unit count or spoken-reader/whole-device acceptance.

## D-366 retained minimum-width surfaces

The narrow-surfaces phase requires actual OS font scale 2.0 and 360dp width on the authorized
independent emulator. Supply review_run for the existing financial-tables.db root and an explicit
review_settings_run for the existing marked settings-text.db root. Missing or redirected inputs
are refused; no fixture is created or migrated by this continuation. The application writes no
OS settings: the external authorized controller captures/restores their exact prior values.

Run the original tables, report legends, budget captions, Home legends, shared plan rows,
ledger/account rows and Settings in en/fa/de, Light/Dark: 42 separately retained cases. Financial
readers select just one original context; the prior 412dp cohorts are not replayed. Each private
case receipt binds comparison, group, context, actual width/scale, caller/Data MVIDs and complete
fixture-input hash. Continue only matching successful receipts; preserve a failed receipt as such.
Original native window focus, original amount endpoints/commands and complete stored inputs remain
required. Settings checks its original selected controls and actual account-picker Cancel, without
security or financial actions. Derived native checks are not a new managed-unit count, spoken
TalkBack, physical-phone, whole-Home/ANR, iOS or publication acceptance.

## D-369 original stored destination-fee sequence

The destination-fee phase uses fresh independently marked fictitious SQLite directories and the
original EntryEditorPage/ViewModel, actual Shell id-query delivery, original native input/Save/Cancel
and complete serialized financial readback. It completes en/fa/de, Light/Dark and Simple/Advanced;
no main-package data or OS settings are written. Retain source/input-bound successful receipts.
Historical destination-fee-* phases preserve unique terminal reports for observed corrections.
review_fee_baseline_mvid binds the original run intent; review_fee_passed_mvid and the finite
review_fee_passed_contexts bind actual original completed cases; review_fee_input_mvid binds the
interrupted prepared input. Never fill a missing native-operation receipt from persisted rows.

The review_fee_resume_empty/review_fee_dead_pid continuation is limited to the exact recorded
D-369 empty fa-Light-Advanced file and terminal PID 10391. It requires zero domain rows, the exact
43-of 50 migration prefix, original lock row/timestamp and explicit original comparison/run. Only
seven pending migrations and previously unwritten preparation continue. It is not a general
production lock-recovery or migration feature. No reset/replacement/replay of completed cases.

Observe the exact PID with Read-ReviewReport.ps1 -Phase destination-fee (or the actual historical
phase), original Run/Comparison and a fresh OutputPath. A timeout or empty tree never permits
restart. Actual terminal runtime failures require ApplicationExitInfo and exact mono-rt evidence,
not only AndroidRuntime/am_crash. Native case counts are not new managed tests.

## One review count per FX transaction (D-374)

`home-review-count` uses a fresh run/comparison and separate original SQLite fixture, publishes actual
Home in en/fa/de Light/Dark, and verifies both count captions plus complete original fixture fields.
The Android context records actual logical display width; never relabel the Samsung384dp screen as
412dp. `home-review-count-fixed` may continue only the original fixture after a terminal observer
failure. Its retained English packet guard is intentionally bounded to the existing412dp failure;
do not infer source/configuration for unrelated cases. Preserve failed reports and old fixtures/keys.
Native captions use the same NativeDigits conversion as the original label handler, keeping bound
values unchanged. The main Release does not activate the independent diagnostic constant.
Full protected Home comparison remains a distinct `compare` phase on the retained unreviewed corpus;
reuse exact passed baselines rather than replaying them or claiming a speedup from mixed samples.

## D-383 original recent-entry oracle

The historical CapturePresentation projection does not include recent rows. The existing original
Home phase independently selects original expected rows with the prior stable Date/CreatedAt order,
then checks identities/count/HasRecent, title, amount, review state and original subtitle suffix.
Formatting oracles execute after measured clocks and heartbeat stop; keep original baseline JSON
unchanged and do not prewarm formatting. OriginalRecentEntryPacketsMatched qualifies this separate
proof. Public recent:Reset-to-first-Add intervals include formatting/event overhead, not exact CPU
profiling or full-Home speed. Native terminal failures retain completed cases before same-source
continuation, never observation-timeout restarts or a stability claim.


## D-385 - Original CSV error and retained-review phases

`csv-input` checks six technical refusals and unfinished quotes across six en/fa/de/theme contexts.
`csv-input-empty` checks only the added empty-source branch, without replaying successful refusals.
Both invoke the real focused native picker button but supply its selected-file effect; neither is
native-provider acceptance. Complete original review/input equality, visible full error text and
exact configuration/theme/language restoration are required. Receipts bind source MVID, comparison
and complete synthetic input hash. No Import/Undo or Main data access. Stop only the terminal caller
and restore ordinary Main/OS. [Qualification](../../../src/Apps/Zanance/docs/quality/csv-input-safety.md).


## D-387 - Preserve CSV financial meaning at quote boundaries

Phase `csv-quotation` qualifies the original malformed-amount refusal and translated error only,
using a fresh comparison with the retained independent fixture. Exact source MVID/input hash,
complete native caption, prior review/data and restoration remain mandatory. No replay of the
unchanged byte/shape-limit cases. [Exact scope](../../../src/Apps/Zanance/docs/quality/csv-quotation-integrity.md).


### D-392 retained aggregate editor

The retained-aggregate phase loads actual persisted Expense/Income aggregates and an ordinary Simple
negative control through the original VM/page. It exposes the original switch, captions and date
controls in en/fa/de Light/Dark at412dp, with no editor Save or OS change. A separate three-row SQLite
fixture may be continued after an observer correction only when the original known account/row shape
still matches; it is never reseeded or replaced. Modal native controls own focus instead of the
covered Activity. Preserve incomplete observations and continue only the unqualified cases.
Windows uses Run-StoreDrafts.ps1 -EditorTextReview -EditorRetainedAggregateOnly at360/412/1280; the
explicit unique EditorAggregateCompleted subset excludes only independently verified earlier cases.

## Original financial CSV review (D-405)

`import-financial-review` requires fresh run/comparison IDs and an empty owned root. It invokes original
own/mapped native pages and actual SQLite refusal/Refresh/import/Undo in en/fa/de and both themes.
The picker and Undo consent are supplied effects; no Main private data is read. Fixed dock actions
are not scroll descendants. Public reports bind comparison and original package/PID/run. Preserve a
live process on observation timeout and retain accepted fixtures on a terminal failure; never rerun
an accepted financial case to repair an observer.


## Original split/join review (D-407)

Phase `split-financial-review` invokes the original native modal on independent owned fixtures,
checks stale-source refusal and one fresh split/join acceptance, exact metadata/attachment ownership
and terminal postcommit notification failure. Twelve retained-form observations cover en/fa/de and
both themes; they do not repeat financial commands. Wait for the modal's own native focus.

The optional `review_resume_split_after_refusal=true` continuation requires a different exact
`review_baseline`, its matching stored failure and original successful refusal, unchanged original
seed identity/metadata/rows, and no accepted/join fixture. It rehydrates only refused presentation and
continues the pending original acceptance. Invalid predicates refuse before any financial action;
public `SplitPreInvokeEvidence` contains bounded booleans/counts only. Preserve old reports and
correct misclassified stages explicitly rather than replacing evidence or rerunning successful money.
