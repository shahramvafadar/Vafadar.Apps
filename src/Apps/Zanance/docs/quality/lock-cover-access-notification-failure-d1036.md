# Access observers cannot prevent the current native lock cover — D-1036

[D1037 lifecycle continuation](native-lifecycle-access-notification-d1037.md) fixes
observed Sleep/Resume notification failures before original access dispatch, with
seven new host and three controlled original native-method contracts.

## Actual defect and bounded correction

Original AppLockService.ShowAsync marks access locked, then invokes AccessChanged
before creating/showing the native cover. A failing cancellation or binding observer
could abort that method while the original visible root still had no new cover.
The requested-lock flag by itself was not evidence of actual native protection.
A callback that also retired startup could additionally surface the observer error
instead of the original startup ownership refusal.

Five new original-service host conditions cover startup, backgrounding from an
unlocked root, resume after unavailable hosting, an existing-cover control and
startup retired by the callback. Original results are5total/4failed/1passed/errors0.
These failures are preserved, not reclassified as native authentication or a
runtime/native-fault cause. The cancellation case uses an actual registered
CancellationTokenSource callback and preserves its AggregateException/inner identity.

Capture a non-OutOfMemory observer exception with ExceptionDispatchInfo, finish
the original still-owned native cover, then rethrow that same notification failure.
The extracted native continuation retains the original startup validation before
native effects and after native waits. A retired startup therefore still refuses
presentation with OperationCanceledException and never lends the cover to its
replacement root. Existing request identity, already-locked/grace state, PIN,
authentication, recovery, queue, writer and financial policies remain unchanged.
No exception payload is added to production logging or user-visible resources.

## Selected host and actual native evidence

The final strict host binary passes5new+47directly affected existing cases:
52passed/errors0/failed0/skipped0/not-run0, actual.NET10.0.12. The selected classes
cover AppLock/elapsed grace/current PIN continuation/cover retirement/startup
ownership; this is not the full suite or52new tests. Financial posting/import/Undo
and unrelated accepted suites are not replayed.

A fresh API36/x64/4096 native package compiles the original service, MauiAppLockHost,
LockPage and protection/viewport sources, with original MAUI native controls over
explicit private ContentPage roots. Fictitious memory PINs avoid real secure storage
or device authentication. A host availability port and a callback-retired actual
Window.Page bound the unavailable/resume and retired-root cases. This is controlled
original native presentation, not the full product Shell or genuine authentication.

Original PID3620/UID10216 produces the same3missing/replacement cover creation
counts0/1/0 where1/2/1are required, and the wrong retired-root IOException. Full
source is exact. Its existing-cover control lacks a mapped native handler after
rapid modal setup/teardown and remains unqualified; the original native packet has
0fulfilled contracts and is not represented as a passing baseline control.

One necessary no-reset helper upgrade to5489 preserves UID/boot/source. Startup
and retired-root conditions0/4pass with actual native evidence. Three other cases
have LockPage objects and correct creation counts but null native handlers;
their first final report remains unqualified. Do not infer a product remedy from
those object counts or count the three cases as native passes.

Only the remaining1/2/3conditions continue after a helper-only graph/observation
correction: independent private roots and explicit actual-handler readiness before
closing setup modals. One further no-reset upgrade to5853 retains the same source;
all three remaining conditions pass. The two earlier accepted conditions are not
replayed. Each accepted visible cover has an original MauiAppCompatEditText,
shown at256x40native pixels, with a shown ContentViewGroup; requested locking stays
true, pending effects0 and synthetic device-auth calls0. The retired-root condition
correctly has no new cover. These five accepted results span two helper versions,
not five results on one final helper binary. Original timing/handler failures and
the before-install CS0649 unused private observer field refusal are retained.

Only the first native phase migrates the fresh fictitious source and saves its
AppLockEnabled metadata. Both upgrades adopt it read-only without setup replay.
Every stage's complete sqlite_master schema/all55table typed57row source equals:
`05344E53C94A4295B97DB514F440CA8D03961CC5E716955BB74D60E88332F7BC`.
There is no account, schedule, ledger, financial command, real PIN/authentication,
genuine provider, photo or new native-fault reader. Original and corrected actual compiler
bindings each cover13named inputs, including the exact declared observer version;
the remaining helper has a further13bindings. Finite source evidence is not whole-PE
identity or Main private-data/key equality.

## Strict complete package and current Samsung adoption

Strict Windows and complete signed dual-architecture offline Android builds pass
with0warnings/0errors. Two actual current compiler documents per complete build
bind AppLockService and unchanged LockPage. Three used native packages pass
148AOT/159ELF/625static16KiBLOAD, offline/nondebuggable/v2/v3/ZIPCRC/alignment;
original x64 Mono remains exact.

Complete shipping APK is99614623bytes, SHA256
`d27a4100c8ec66c63f2a97db2f83319fcadbda9b4e1cf12c4e83353a23156e8d`:
384AOT/408ELF/1608static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment pass.
Both original Mono slices are unchanged. These are static alignment/source/package
facts, not hardware16KiB, every device or whole-app security acceptance.

After current read-only admission confirms Samsung unlocked and Main8288 on a
selected Home with141nodes/0editors/0loading, one install-r adopts the exact complete
package without clearing data. One normal launch reaches24358 on the same
6bd9dccf-976f-4a2c-afa0-bfceba0af22e boot. Its first accepted before/after XML and all
141native attribute maps are byte-exact, with both Home nodes selected. UID10380,
CE155299/DE154603, all captured grants/flags, every other captured Zanance package,
five OS values, locale/key presence,1080x2340/density450 and reverse state match.
Only the intended Main package hash differs. No Main private database/key read,
force-stop, authentication, financial action or old-success replay occurs.
Two remote XMLs/103864bytes are hash-bound before removal; raw local packets remain.

## Retirement, one-time CI and limits

Only the new native package/UID is retired after source and exact original OS
restoration. Its original four emulator handles become signalled; official AVD
retirement removes25files/4407702974logical bytes. ADB5038/PID65060creation identity
is preserved without reset/reconnect/pair. Five standard Cleans retire8962registered
files/1315457654logical bytes. 3252registered files,28host-tmp files3946209bytes and
six userhome files7732bytes remain. Every logged delete is inside the current owned
output roots. Used signed/source/compiler/result/failure evidence remains; no
supplemental/frozen/retained5606/Q02 cleanup or other lifecycle action occurs.
An initial documentation-preparation SyntaxError occurs before any tracked write;
only that unexecuted preparation is corrected, without runtime/check/cleanup replay.

The sole D1035response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42859844543)
completed/cancelled and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42859844578)
completed/success. The separate D1028execution response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42859914814)
in_progress/no conclusion and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42859914969)
completed/success. Both single checks are consumed, with no polling/job-log/rerun.

This closes only the observed access-notification/native-cover defect. The original
SGen/native-fault remedy, broader performance/accessibility/platform/provider/Store/
release gates and all canonical parent States remain open. D607/D752terminal
checkpoints, accepted enrollment successors, frozen scopes and the active full
delivery goal remain unchanged.
