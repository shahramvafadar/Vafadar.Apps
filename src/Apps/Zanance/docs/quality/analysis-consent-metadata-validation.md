# Complete original analysis consent metadata - D-974

## Actual original failures

The original client validated returned offer metadata in OfferAsync but ReadAsync
accepted a retained offer after checking only its presence. SubmitAsync also did
not apply that complete offer contract before publishing a new private intent.
Continue/Close could therefore pass malformed retained consent to the refusing
credential port. An unsupported requested purpose also reached that port before
metadata transport. No actual credential, HTTP/provider, credit or financial
operation is used to demonstrate these defects.

Eleven malformed variants independently cover empty offer id, null/blank/65character
provider/model, zero/above1e12 maximum units, mismatched purpose and undefined
purpose. Their four original Read/Continue/Close/Submit entry points give44new
negative host cases. All44fail on the original client; two literal valid controls
pass on that baseline. One further unsupported-purpose case is then added/run
alone and fails before correction. These are separately versioned baseline runs,
not47cases all run on the first binary.

The tests use a refusing in-memory journal/token port and actual original
PersonalSyncTransport. Scope filenames are hash labels only; no file/database is
created and no real token exists. The baseline primary paths stop at those guards.
Journal-write and source-recheck counters cannot be mistaken for financial writes.

## Focused correction and verified scope

A shared bounded ValidateOffer retains the existing provider/model/id/unit policy
at returned service metadata, persisted consent and new submission entry. Purpose
must be defined and match the frozen disclosed payload. Unsupported requested
purpose refuses before metadata transport. New submission still freezes caller
lists before access callbacks, queue/private-journal waits or any possible effect.

Expiry is deliberately not reinterpreted: an originally valid expired literal
request remains readable; exact64character metadata and1e12technical maximum remain
accepted. No pricing, expiry, provider destination, entitlement, refund, financial
model or migration changes. An inverse source edit reconstructs the complete
original client exactly; semaphore/source-recheck/transport/save ordering and
original response validation remain unchanged.

Final host47new cases pass, plus only five directly affected existing submission
snapshot/cancellation cases on the same final host binary. No terminal TLS,
provider/credit/financial or unrelated successful suite is repeated. The same47new
conditions run in an actual offline Android SDK package with current Data input:
45refusals/2valid literal reads and credentials/private-writes/source-rechecks0.
The native credential/journal ports are synthetic refusals; this is not genuine
provider/authentication/native consent, a real durable journal or a credit charge.
D973's accepted actual storage-backend cases are not replayed.

API36/x64/4096/.NET10.0.14, fresh UID10216/PID4920, boot
15e8a54c-c0d1-4792-9315-59b6404c5fbe. No account/entry/plan/DB preparation or SQL
call occurs. Five OS values, locale/key presence, boot/geometry/reverse restore
exactly after fresh-UID uninstall. No phone/Main or retained5606/Q02 action occurs.
Main24752/07b0a043 remains only the last D972observation.

## Builds and compiler inputs

Strict Windows and full offline shipping384AOT/408ELF/1608static16KiBLOAD pass
without errors/warnings. Prepared shipping SHA256
38c54808c7b532e949a5b28a419f1de8a86a55861b725aa22efc29c3efcaa34f,
99598239bytes, is not installed on Main. Used standalone SDK SHA256
c884456ed1c9bcb220eeec5b312f239dd2bf345a39f4930a9dc3d15d11e3267d,
14030503bytes, has91AOT/101ELF/394static16KiBLOAD. Both are offline/nondebuggable,
v2/v3/CRC/ZIP aligned; original Mono remains unchanged. This is not runtime16KiB
or a cross-platform/hardware guarantee.

Eight exact current compiler-document checks cover host tests/probes/Data, Windows
and shipping Data inputs, native observer/probes/Data input. These are compiler
source/input bindings, not whole-PE equivalence or every packaged/AOT identity.
Used signed packages, input PDBs, archived source and raw failures/results remain.

## Owned retirement and cleanup failure retained

Only the fresh review UID is removed. One normal owned console stop yields all
four original retained host handles signalled: wrapper38828/conhost9080/QEMU74256/
SDK helper63064. Official AVD retirement removes24files/4339021073logical bytes;
six userhome files/7732bytes remain. Original ADB5038/PID65060/creation identity is
exact, with no reset/start/reconnect/pair. No XML capture or supplementary/frozen
delete occurs.

The initial broad metadata preflight refuses stale historical artifact entries,
without stat/reading those targets. The shell chain incorrectly continues after
that Python failure: tests Clean succeeds and Windows Clean partially removes
current output before NETSDK1005because the last restore selected Android. This
execution mistake and all original logs are retained. Actual logged deletions are
audited against current project bin/obj; no old artifact/frozen target is deleted.

The correction enforces explicit process-exit checks before subsequent mutations
and admits only current target output/intermediate paths, matching standard Clean's
own filtering. Original file lists are not edited and there is no alternate delete.
One metadata restore for each App target resolves cleanup assets; no build/test
is replayed. All five standard targets then complete (tests, Windows, shipping,
SDK and compiler), with the failed Windows attempt separately retained. Across
all attempts1837file-delete log entries stay inside current standard bin/obj.
Late admission proves806registered files/465242456logical bytes removed; initial
partial-delete sizes are unknown and are not added to that byte total. A relative
path log parser initially refuses and is corrected against each actual project
directory without performing any further deletion.

The single prior D973CI response at2026-11-09T23:41:30.374269Z reports
[CodeQL42720430930](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42720430930)
completed/success and
[CI42720430888](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42720430888)
in_progress/null; no second request/log/rerun or full CI success is inferred. Canonical parent State rows stay unchanged; genuine provider/platform,
whole security, original fault/ANR/performance/accessibility/iOS/Store/release
requirements and the full delivery goal remain open.
