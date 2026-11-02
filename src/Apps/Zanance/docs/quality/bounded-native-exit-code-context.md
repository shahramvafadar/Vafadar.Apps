# Bounded native exit code context (D-759)

2 November 2026. Owned API36/x64/4096 only. This diagnostic does not change the product,
runtime, financial policy or phone. D-743's original native failures remain failures.

## Missing evidence and rejected observation route

D-743 has actual Android native-crash exits but null ApplicationExitInfo traces. No
assertion text, terminating instruction or exclusive mark-bit cause was established.
The exact runtime source revision remains `0427b9abbead3fa64c6655eb4290c537397131fa`.
Its `mono_trace_set_log_handler` replaces the default logger, closes the old callback
and requires the fatal callback to abort. There is no public getter for chaining the
old callback. Reject that proposal before building; no logger or signal handler changes.

Use an out-of-process reader of Android's own protobuf record instead. Review the
[Android16 schema](https://github.com/aosp-mirror/platform_system_core/blob/android16-release/debuggerd/proto/tombstone.proto)
and [tombstoned creation policy](https://github.com/aosp-mirror/platform_system_core/blob/android16-release/debuggerd/tombstoned/tombstoned.cpp).
The installed platform's source identity is not established by that branch reference.

## Exact input and output boundary

The reader runs only on the fresh owned emulator, with API36/ranchu/x64/page4096 and
root required for the production record path. It never changes SELinux, a tombstone's
permissions or another device. The shared ADB5038/server66176 remains in place.
Input is a regular, no-follow, nonblocking opened file at a strict numbered
`/data/tombstones/tombstone_NN.pb` path, at most4MiB. Bind inode, nanosecond mtime and
size before/after reading. Require the observed system owner1058/group1000 and exact
creation mode0660 or0664. The whole buffer is wiped before release.

Before reading frames, require the exact owned package, UID, prior PID, crashed TID,
architecture and signal, independently correlated to Android's native-crash exit and
one new record. Strict bounded protobuf parsing rejects malformed lengths, duplicate
identity fields, invalid varints, missing/duplicate crashed-thread map entries and
overlong messages. Only the selected thread's current backtrace is interpreted.

Export only selected module basenames, exact original BuildIds, original frame ordinals
and relative PCs within preadmitted executable file-backed ELF ranges. Keep at most32
exported frames while validating the remaining selected frames up to256. Unlisted
modules are counted and skipped. Never relabel the first exported frame as frame0.
Absolute PC/SP, registers, memory, function/thread names, abort text, logs, file
descriptors and unrelated thread bodies are not exported. No raw record leaves Android.
No partial output is produced if a later frame fails validation.

## Component controls and preserved refusals

Actual Android execution passes204 distinct synthetic record cases and four filesystem
refusals. The first valid result was falsely rejected by a host substring check because
the fake-address substring also occurred inside the admitted BuildId. Correct the host
check to exact output keys, prefixed fake addresses and bounded relative PCs, reevaluate
that same saved packet, then continue only the203 unissued inputs. The native parser is
unchanged by that correction. These are component cases, not product unit tests.

The filesystem cases cover symlink/no-follow, outside-prefix, parent traversal and a
nonregular directory. Android denies creation of the proposed named FIFO; it is not
executed or qualified, and no permission/SELinux workaround is attempted.

The initial no-world-readable assumption refuses actual0664 metadata. Android's reviewed
creation code expressly permits0664; admit only the exact system identity and two stated
modes, with ten new passing positive/negative metadata cases. The original protobuf
parser bytes remain unchanged. No OS file permission is altered and no crash is repeated.

## Actual controlled native exit

A fresh plain Android package `pro.vafadar.zanance.exitcontextcontrol` has no MAUI, Core,
Data, account, authentication or financial setup. Its one visible button creates an
exclusive persistent marker and calls a small deliberately aborting native function.
The reviewed existing official NDKr30 is reused without a download/global installation.
Strict compilation succeeds; the initial ambiguous Environment compile is corrected
before any installation. PackageSHA256
`407110139f3c05a83c14d3a312e9da58ed7118e2a857d3e048732d4a4f0c24fb`
has10 own AOT/21ELF/72 statically16KiB-aligned LOADs, exact official Mono14, valid
v2/v3 signatures/CRC/ZIP alignment and no network permission. Static alignment is not
hardware16KiB acceptance.

The sole install succeeds. A host assertion wrongly expects the entire install output
to be just Success; verify actual UID10216 without reinstalling. Immediate PID lookup
is too early; the same launch becomes6341. The Material button is uppercase, so its
saved native XML is reevaluated without an extra dump or tap. Full window focus is used
after the narrower window scope omits it. The metadata command's shell quoting is
corrected before the fatal action. A newly pushed reader initially lacks execute mode;
chmod applies only to that owned reader, never the system record.

One actual button action yields Android reason5 APP CRASH(NATIVE)/status6 for PID6341,
UID10216 at the OS-reported local04:02:41.536. The unique new protobuf has inode16386/mtime
1793588561.520000000/260607bytes. Its crashed TID6341 has three original frames; two
selected code frames export: libc relative0x6a424 and the owned control relative0x628.
The latter's BuildId `b42e89eaf458ba234812403b3a68a1da53ee890f` matches the public built
ELF and resolves statically to `zanance_native_exit_control`. The unlisted frame is
counted without exporting it. This qualifies the finite code-context route, not D-743's
cause or a product repair. The finished empty control package is uninstalled once.

## Original diagnostic continuation

The controlled reader admission allows at most one materially changed original diagnostic
startup with passive OS collection. Copy only the already accepted fictitious tenfold
transport from a preserved used package, never a seed/financial replay or frozen output.
Ordinary preparation and a current full source control must precede diagnostic adoption.
The private check-mark-bits mode retains its known CLEAR_AT_GC nursery effect; it is not
ordinary production GC. Do not replace handlers, force collection, repeat old successful
context matrices or restart a failed diagnostic to repair observation.

### Actual original startup and native refusal

The fresh package is `pro.vafadar.zanance.nativeexitcontextreview`, UID10217,
CE361978/DE82149, boot `ee371aac-73d3-4dd3-9e22-cc39de12c793`. Its ordinary package
SHA256 `f92de434b28ed9cc967dce7660249075a813d5fdcaca8ff3dbc6696ef96e0cc9` and diagnostic
`cb840d03fb9521c7f1371453c8311355747a4d4072de7f8ad1cf04799bcb5da3` both pass strict
offline192 own AOT/204ELF/804 static16KiB LOADs, v2/v3/CRC/ZIP checks and exact official
Mono14/compression input checks. No whole linked-PE/AOT identity or performance-pair
claim follows. The diagnostic generated environment contains the selected mode; the
ordinary source guard rejects any nonempty GC-debug mode. An initial XA2001 asset-path
mistake is corrected before installation, without changing the accepted source bytes.

Ordinary process6763 performs original protected preparation once. The original Not now
rationale action grants nothing; the first window guard refuses because the dialog title
does not contain the package. Verify the exact focused window's ownerUID/sessionPID and
package, then issue only that previously unissued action. Navigate once to actual original
Transactions, read100000 entries and capture the complete post-startup typed control and
all32-field ledger stamp. The control is accepted before diagnostic adoption, avoiding
D-743's first-startup DisplayUnits assumption. No account/financial setup, key replay,
migration replay or successful native context matrix is performed.

One no-reset diagnostic upgrade preserves UID/CE/DE/boot. Android records the ordinary
process6763 ending for PACKAGE UPDATED/reason16, not native crash. The sole diagnostic
launcher yields actual priorPID7080, then Android reason5 APP CRASH(NATIVE)/status6 at
the OS-reported local04:18:15.290, trace null. It is not relaunched. A unique new record has
inode16388/mtime1793589495.272000000/410851bytes. Header ownership and prior OS native
reason are required before parsing it. Its requested crashed thread7166 contains exactly
two original frames, both admitted: libc relative0x6a424 and libmonodroid relative0xb30af.
No Mono frame or earlier caller is present in this finite native record.

The exact packaged libmonodroid BuildId
`ff7bf4af27e1ccfc88be6e5cbd98a6a2c5f5e4fc` resolves the second frame to
`xamarin::android::Helpers::abort_application`. Its public ELF symbol starts0xb2db0,
size0x33f; bounded static disassembly places the reported frame PC inside the five-byte
`call abort@plt` instruction at0xb30ab through0xb30af. This is a terminating wrapper
location, not proof of the originating assertion or a SIGSEGV faulting instruction.
The reviewed [Android36.1.69 helper source](https://github.com/dotnet/android/blob/36.1.69/src/native/mono/shared/helpers.cc)
is consistent with calling abort; exact installed-source lineage is not inferred merely
from the tag. Abort text, source-location argument values, stack memory and registers
are not read/exported to fill the missing caller. D-743's earlier cause and D-678's
referenced-object fault origin remain unresolved. No speculative production patch follows.

### Restoration and cleanup

Restore the original ordinary signed package once, with UID/CE/DE/boot preserved. Actual
ordinary7273 adopts the existing source and passes the complete original typed snapshot,
schema/source-cookie and100000-entry/all32-field ledger comparison against the control
captured before the diagnostic. No source-value/schema-cookie/revision normalization is
used. This is source restoration, not general stability or UI/performance acceptance.

The two fresh packages retire; no phone command occurs. Exactly three owned XMLs/16182bytes
are hash-matched and removed; the owned component-input directory is removed. Original
five settings/boot/320x640/density160/empty reverse mapping are exact, and adbd returns
to shellUID2000. Only owned emulator5586 shuts down; QEMU52436/launcher65644 exit and
shared server5038/PID66176 survives. Official AVD deletion removes25files/5030950170
logicalbytes. Three standard Clean commands remove27497files/1456721080 logicalbytes.
These counts do not establish a host free-space increase or virtual-disk compaction.

Supplementary deletion is rejected before process with `blocked by policy`; no reason
more specific than that is supplied. All13 planned targets/2005files/430177273logicalbytes
remain frozen, without retry, subset, alternate method, move, escalation or reuse:

- `E:/Zanance.ReviewBuilds/D759-control-build`
- `E:/Zanance.ReviewBuilds/D759-original-build`
- `E:/Zanance.ReviewBuilds/D759-diagnostic-build`
- `E:/Zanance.ReviewBuilds/D759-original-source-input`
- `E:/Zanance.ReviewBuilds/D759-owned-avd`
- `D:/_Projects/Vafadar.Apps/artifacts/d759-exit-context-control`
- `D:/_Projects/Vafadar.Apps/artifacts/d759-original-context`
- `E:/Zanance.ReviewBuilds/D759-control-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D759-control-package/pro.vafadar.zanance.exitcontextcontrol.apk`
- `E:/Zanance.ReviewBuilds/D759-original-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D759-original-package/pro.vafadar.zanance.nativeexitcontextreview.apk`
- `E:/Zanance.ReviewBuilds/D759-diagnostic-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D759-diagnostic-package/pro.vafadar.zanance.nativeexitcontextreview.apk`

Three used signed APKs, small used native binaries, public module/source evidence and
pre-refusal private source archives remain outside those targets. All older frozen
scopes remain untouched. Production files are exact; no Windows/product build or old
successful test/financial workload is repeated for these Android-only private helpers.
Zero permanent product tests are added. D-752 still needs genuine owner readiness;
D-749/D-751/D-607 successes stay settled. Overall delivery stays active. Root cause,
general security/stability, physical speed, accessibility breadth, iOS/provider/Store
and release acceptance remain separate and open.
