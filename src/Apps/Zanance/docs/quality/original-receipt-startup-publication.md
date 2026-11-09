# Original receipt startup publication ownership - D-952

## Observed defects and correction

ReceiptReadingSession originally publishes Busy and clears Error before capturing
its original native origin and before entering cleanup. A synchronous Busy/error
subscriber can replace and restore the native page before that capture, allowing
the retired command to adopt the replacement and acquire a source context. A
throwing Busy subscriber escapes cleanup and leaves Busy true. Publishing failure
Error can itself retire the owner, yet the old code still opens a native alert.

Four focused original failures reproduce the two unexpected source acquisitions,
leaked Busy and stale alert. Startup now captures/subscribes the original origin
inside the existing try/finally, validates it, and rechecks after each Busy/Error
publication before the original source read. Failure feedback rechecks ownership
after publishing Error and before the native alert. The existing reader/parser,
native camera/picker, app-lock queue/retained completed publication, original editor
validation, original file/draft and financial policy are preserved. No successful
OCR/photo, authentication, payment or receipt Save is replayed.

## Host, compiler and complete package evidence

Five new focused contracts run against original source: four fail and the current-
owner positive control passes. All five pass after correction; the positive control
is repeated only because its startup ordering changes. The separate four existing
directly affected cases pass: original presentation retirement (two branches),
covered reader failure and owner retirement before queued recognition. Total nine
selected final outcomes are five new plus four existing, not the whole suite.
The new cases use the migrated empty file without account/attachment/ledger setup.
They compare every ordinary migrated table and row, including newly added private
and history/derived tables. An original-factory sentinel acquires/disposes a context
before opening a source reader; native effects are counted, never invoked.

Actual original/corrected test PDB source documents are archived and checksum-bound.
Windows Release scoped to its actual target passes zero warnings/errors. The first
unscoped multi-target restore refuses the Android-only notification dependency for
other targets; this failure is preserved. Only the intended Windows target property
is supplied on continuation, with no product/dependency change or successful build
replay. Five actual shipping and seven private actual PDB source documents match
current input. A private observer absolute-path suffix initially refuses normalized
PDB paths before device work; correcting only that suffix reuses the same PDB and
package, without rebuilding or inventing whole-PE identity.

The full offline shipping APK SHA256 is
`30f54a7072072168d2e08a836994785ac2685584e7ad4799eb4d25994b525093`
(99573663 bytes),384AOT/408ELF/1608 static LOAD. Private original-app x64 SHA256 is
`3f0f69658c3002151893a5f8c38af103f3d4d06669783b711530d594a73c2d6a`
(56938402 bytes),192AOT/204ELF/804 static LOAD. Both strict complete builds have no
warnings/errors; offline/nondebuggable/v2v3/ZIPCRC/static16KiB checks pass. Original
Mono modules remain f03c ARM64/edfa x64. This is not hardware16KiB acceptance.
Shipping is prepared but is not installed on Main while D944 is held.

## Original native Home command and original runtime

One fresh API36/x64/4096 cohort has PID3073/UID10216 and boot
`257c23dd-6868-4a32-a1ac-d01a2370f700`, actual320x640/density160/font1. One distinct
metadata-only zero-opening cash account makes the original Home receipt action
available; entries0/plans0. The original MauiReceiptRuntime is retained, not replaced
by a fake source. One independent scoped positive control verifies the actual DI
factory acquires/disposes/refuses exactly one context before reading.

The first boot property observation returns a shell refusal before transport is
ready; the launch is not declared dead or restarted. After revalidating that same
owned AVD, only the unexecuted install/start continuation proceeds. One installation
and one COLD launch succeed; the same once-issued preparation completes. Original
Home reading reaches Ready, then one visible original Scan receipt button is tapped
at native bounds[237,285][281,346]. At actual session Busy, a scoped callback changes
and restores that actual HomePage.BindingContext. The original runtime's real page
subscription invalidates its captured origin permanently; restoring the same model
does not create a new origin for the old command.

CommandEnded/Retired/ScopeBound/ScopeEnded are true, ReadContexts0 and both Busy
states false. Error stays null; no native dialog appears. Full20 typed source is
exact before/after at SHA
`18E8E22656566FE2AB17E42F6E8668DACADF2BAEB6DB33ABBC9D2ED601E04E2B`,
with accounts1/entries0/plans0. This is controlled actual native BindingContext
retirement/restoration, not all OS navigation timings, hardware camera/OCR, financial
acceptance or a whole private-database/schema proof. Original root/source restore
then completes without replaying the accepted control, setup or command.

## Exact retirement and preserved open work

Two XMLs/95324 bytes have equal local/remote digests and are retired remotely;
retained local evidence remains. Only the fresh package is uninstalled and absence
verified. Five OS values, locale/key absence, geometry/boot/reverse are exactly
original. Own host24948 exits after normal console stop. Official AVD retirement
removes24files/4487394599logical bytes with zero AVD files remaining; small scoped
user-home residuals remain. Existing ADB5038/PID50972 creation ticks stay exact.
Five standard Cleans pass: Windows, private/shipping Android, selected tests and
compiler reader. No supplementary/frozen deletion or retained5606 command occurs.
Used signed APKs, source/PDB lineage and raw outcomes remain under
`artifacts/d952-receipt-startup-publication`.

D949soleCI GET at11:56:09.159645Z reports
[CI42610668706](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42610668706)
completed/cancelled and
[CodeQL42610668835](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42610668835)
completed/success. D950soleGET at11:58:59.517659Z reports
[CI42611001586](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42611001586)
completed/cancelled and
[CodeQL42611001396](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42611001396)
completed/success. D951sole response has
[CI42611466830](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42611466830)
in_progress,
[CodeQL42611466908](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42611466908)
completed/success and
[CodeQL42612648426](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42612648426)
in_progress. No final result is inferred for running workflows, no second GET or
successful job-log/rerun follows. These are the actual returned workflow records.

D944 physical7552/UID10439/data and its original unresolved navigation remain held
while the owner is away; no phone command/authentication/Main adoption occurs. All
terminal financial checkpoints, retained5606/Q02 and frozen scopes remain untouched.
Original fault/ANR, whole performance/security/accessibility, iOS/provider/Store/
release and full delivery remain open; independent ready development continues.
