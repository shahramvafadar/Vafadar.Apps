# Mono15 original Home session source control (D1094)

This is a bounded current-runtime Android read-session investigation, not a
product optimization, whole-Home latency target, cold start or release acceptance.
The actual production source opens `BeginHomeReadSessionAsync`; final validation
uses a fresh transaction on that same retained connection. The earlier separate
snapshot/validation diagnostic is not substituted for that operation.

## Original inputs and runtime

Only the tracked original PerformanceFixture is explicitly compiled alongside the
original App/Data/Core sources. The private project excludes the unrelated
AndroidHome benchmark directory and its protected untracked source. The original
reference shape is10000entries/20accounts/100manual schedules; tenfold is
100000/200/1000. Legacy fictitious rows, migrations and original protected
bootstrap prepare each shape once. These are explicit synthetic inputs, not
financial Save/Import/Undo or accepted-operation replay. AutoPost/reminders are
false. Synthetic device-key material remains inside the original protected API;
no key, row, address or register is emitted.

The fresh API36/x64/4096emulator has2048MiB/two cores. SDK10.0.404/runtime10.0.15
and original Mono15x64 payload are bound to the used private packages. The
192AOT/204ELF/804static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC checks are
package evidence, not physical16KiB or shipping-identity acceptance. No runtime,
GC, writer, cookie guard, product source or central configuration changes.

## Disqualified reference observations

The first measured private package is
`a68e487498ad76aad1490f1a95a35fc908b4b38c516d9bb9985e5c8e465b15c7`.
Its original session read and same-connection validation return complete ordered
32field rows plus exact account/schedule/category/settings controls in PID4434,
UID10216. Two observations exist, but the following complete-source check fails.
Their read898.1547/123.8431ms and validation25.3541/10.3332ms remain disqualified;
they are not reclaimed as passing or repeated. The larger shape was not measured.
This is an observer/source-preservation failure, not a native crash; the same
process and both prepared protected inputs remain available for diagnosis.

The supposed source-read observer committed an EF transaction. The unchanged
original HouseholdCommitInterceptor invokes HomeLedgerReadBlocks.MaintainAsync
at commit. Static correspondence alone does not establish an exclusive historical
cause. The corrected diagnostic uses a raw deferred SQLite transaction enlisted
with UseTransactionAsync, never commits, and disposes it with rollback. No source
cookie normalization or weakening of the original writer is used.

## Isolated original-commit control

One no-reset private observer upgrade reaches PID5469 with the same UID10216,
bootf5a76aa0-f317-4283-933e-b78b80bf18a1 and exact captured public state. Existing
generated/protected markers skip all fixture/bootstrap work. A separate guarded
inspection command issues once; it performs no Home timing or financial command.
For each shape, two corrected source reads are stable. An original authenticated
rollback file is opened read-only and copied once into a fresh disposable
ciphertext file. Its hash and complete schema/typed rows match before the control.
Only on that copy, one empty EF transaction commits through the unchanged
interceptor, without a Home call. Original rollback ciphertext stays hash-exact,
and the retained original source remains exact after the control.

| Shape | Actual schema | Current cache cookie | Rollback cache cookie | Current versus rollback | Disposable commit | SQLite changes |
| --- | ---: | ---: | ---: | --- | --- | ---: |
| Reference |96|96|370|HomeLedgerReadState only|HomeLedgerReadState only|82|
| Tenfold |96|370|370|No difference|HomeLedgerReadState only|786|

Every other table and schema hash remains equal. Changes counts are SQLite
connection total_changes deltas, not numbers of changed financial rows. This
proves the observer commit can change derived state on these exact inputs. It
does not prove no other historical transient effect occurred, or that Home wrote
that cache. Neither original protected source is repaired to manufacture a pass.
The two reference timings stay disqualified. The previously unissued tenfold
measurement is a separate explicit continuation, with no reference replay.

## Previously unissued tenfold continuation

A second necessary no-reset observer upgrade reaches PID5856 with the same
UID/boot and exact public state. Both original protected inputs are adopted;
no generation/bootstrap, reference read-session timing or financial action
repeats. Its separate one-shot command skips reference entirely and refuses
adoption of any earlier unqualified sample. Both tenfold calls use the original
BeginHomeReadSessionAsync and ValidateAsync on the retained connection. The
independent control connection is closed while timing; loaded-after-source-audit
and post-read-audit heap labels remain explicit.

| Tenfold sample | Read ms | Validate ms | Read all-thread allocation bytes | Validate allocation bytes | Managed heap after audit bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
|0|6675.2376|495.0850|100842912|11707040|366330160|
|1|2551.6594|200.0499|95398904|9573728|368834096|

Both samples preserve every original ledger field/order and other source entity
value. Complete source/schema before and after is exactly
`58C3BC51A1B8DE006244252E5AC0D661124867B81FC43D866C5BBF6468C0C742`.
Actual schema remains96and cache cookie370, unchanged: the original reader's
stale-cache refusal/fallback is retained, not repaired or hidden. Ledger digest
is`3B3C805D883E88D8A8C91747BE571084ACBFFE3E8EE0646D03452DCD1CC5CE5D`.
There is no baseline/candidate comparison, dedicated process-pair experiment,
UI projection/rendering or cold-start timing. These samples neither show a speed
improvement nor satisfy the whole-Home/two-second gate. Allocation includes all
threads during the component interval; post-audit heap is not exclusive session
retention or a leak verdict. Reference remains unavailable for qualified timing.

The inspection and tenfold signed package hashes are respectively
`50276347d07743b5426452c65bf41223c84bef7c6f6afa87048d3fb9b9713e9c`
and`918ae4f9ddf08a4b88429ea86fd058b6b5ecca1c998a4dac347614684066cd82`.
Nine logical compiler documents are bound in each continuation; original Data
and Core DLL/PDB copies remain byte-identical before selection. Strict private
builds finish without warnings or errors after the recorded diagnostic fixes.
Unchanged Windows/full shipping/old managed suites are not repeated.

Only the fresh package retires after same-PID terminal evidence and exact
original OS/key-presence/locale/boot/geometry/reverse comparison. Original
emulator49200and child QEMU80724handles signal after graceful owned stop;
shared ADB5037/PID59860/original creation remains alive, with no server reset,
reconnect or pairing. Official AVD deletion retires24files4708250987logical
bytes. Two standard Cleans retire13702files820172010logical bytes;
979files128887357bytes remain in those build roots. Signed packages, compiler
inputs/source, raw failures and outputs remain; there is no supplemental delete
or frozen-scope action. No live source or pending owner input remains in this
fresh cohort. Broader reference timing and whole-performance qualification are
still open; retirement does not mark the overall project complete.

## Provenance and retained failures

The first unused private candidate was corrected before deployment to preserve
converter-null output, close the independent control connection during timing,
and label post-audit heap/loaded-source observations accurately. Duplicate Data
and Core PDB locations initially refused a count-one assertion; all copied PDB
and DLL hashes are verified identical before selecting one. Eight original
measured and nine inspection logical compiler documents bind caller/fixture and
original Data/Core read sources. These are finite compiler-input checks, not
whole-shipping-PE identity.

Inspection compilation first refused a span crossing await; it was fixed before
that candidate was deployed. The separate tenfold command first refused an
optional-parameter method-group conversion; that compile failure is retained and
no package from it is installed. No successful reference observation, managed
suite, financial action or earlier terminal cohort is rerun.

Raw logs, private source/PDB lineages and signed packages are retained under
`artifacts/d1094-mono15-home-session-stages`. Main27703/9fbebe6b remains only the
dated [D1092 observation](current-cleared-input-main-d1092.md). No phone command,
authentication, source export, frozen-scope inspection or supplemental deletion
belongs to this investigation. Canonical parent States and the full delivery
goal remain open; original fault, whole performance/security/accessibility,
platform/provider/Store and release gates remain unaccepted.
