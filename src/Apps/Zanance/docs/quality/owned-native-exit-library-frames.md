# D-676 - Bounded library frames from an owned native exit

## Original failure and diagnostic admission

D-674 retains a real native exit/status11 with no established function or origin. Before spending
another product reproduction interval without a qualified reader, verify the existing diagnostic
path in a separate, empty SDK-only package. This package links no MAUI, financial Core/Data,
database, account, signing-secret reader or product-bootstrap assemblies.

Fresh API36/x64/4096 AVD Zanance_D676_OriginalExit has
boota3717dc3-31b7-4276-929a-6013afb7ab16. The original-app observer is
`pro.vafadar.zanance.originalexitreview`/UID10216; the separate control is
`pro.vafadar.zanance.exitreadercontrolreview`/UID10217. Both bind
run5dec2104e88f46228ed5011221487dc7 and actual owned package/UID/emulator checks.

Exactly one issued-once libc signal11 is intentional in control PID5526/UID10217. It cannot
target original-app PID4522: the native call accepts only the signal number and kills its own
separate process. Original4522 remains alive. Android records reason5/APP CRASH(NATIVE),status11;
this is a diagnostic control, never a reproduced product failure or a stability count.

For this same record, dumpsys reports trace=null but the real self-owned SDK TraceInputStream
contains a tombstone. The original NativeExitFrames reader rejects the whole input as soon as a
frame's code identity is not an allowed library. The actual initial SDK result is
NON_MODULE_FRAME_REFUSED. No rejected name/path, raw tombstone, memory, registers, absolute PC/SP,
function string, unrelated thread or other-package crash is exported or saved.

## Exact reader correction and focused checks

Discard unsupported code locations instead of discarding later allowed library frames. Return
only their count; every original frame, including a discarded one, still consumes the128-frame
limit. Preserve expected PID/UID/crashed-thread identity,256-thread/8MiB bounds,1024-byte identity
strings, wire/length/integer/duplicate guards, strict UTF-8 and clearing discarded byte buffers.
An allowed library with an invalid BuildId remains refused; no surviving library frame remains
a refusal. This does not broaden the library-name/BuildId allowlists.

Each retained location includes its original zero-based frame ordinal. A filtered caller must
not silently become the presumed fault frame. Only relative code PC, library basename, BuildId,
original ordinal and omitted-frame count survive. A filtered subset is not a complete native stack.

Two new mixed-frame regression inputs fail on the original reader with its actual identity
refusal. Five distinct new cases and nine affected existing reader cases pass after correction.
The ordinal change justifies the final focused recheck;14 is the distinct total, not28 new tests.
These are synthetic protobuf/parser cases on the actual .NET10.0.12 host, not financial tests or
an assertion that that host runs14. Strict host/private Android builds have no warnings/errors.

Update only the separate control without reset and reread the same5526/10217 OS record. No second
signal or control launch occurs. The final SDK result retains one libc.so relative code location
at original ordinal0, reports one discarded non-library frame and preserves exact PID/UID/thread
identity. Independent host schema/bounds checks accept only those code/identity/count fields.
The source copied into both final observers is byte-identical to the changed tracked reader.

## Finite original application interval; random failure still unresolved

The original App source remains exact. The private caller wraps only the original account-editor
factory's first asynchronous read: hold it, then release to a scoped synthetic IOException so
the original loading/failure/Cancel path executes. No fake UI or financial writer is introduced.
One necessary reader-only no-reset update changes original-app4522to6071, preserving UID10216,
boot and full20collection source without account/setup replay. No product App/Core/Data source,
runtime/GC policy, migration, permission or full shipping candidate changes.

Only one de/Dark/actual OSfont2/360dp/density420 context is used. Native source enforces at most12
issued original editor opens. Host guards stop before another provider/UI operation if6071 truly
dies; an observation timeout is not permission to restart. Twelve original root/read-failure/
native-Cancel cycles complete in6071 with full20source equality and no native fault. This finite
input is finished and must not be rerun to manufacture a failure or a stability pass. No account,
metadata Save, ledger, settlement, import, PIN or genuine authentication command occurs.

D-674's random crash remains unresolved; its historical stack is not recovered or recreated here.
The corrected reader is admitted for a later real owned native exit, not proof of its root cause,
general ANR/crash correction, performance or platform release acceptance.

## Executable identity, restoration and cleanup

| Used signed APK | SHA256 |
| --- | --- |
| Initial original-app observer | 06fe84b2e38736104543567e2f1945d6a1a8466e1d5cc4e350b48f4f8753f12d |
| Ready original-app observer | fb55f9c9f2f711da091b48a6122ff8f4a5c53602c0576d4b94783e59de45587f |
| Initial empty-UID control | 939dbdab8835a5b6c1e905bb026a12d6a8463b0fede11a0c75796ca829c3ef07 |
| Library-subset control observer | 2a4c656090630b05c317be49dc23d3847d489cce06506ea03f0933ba995aeac2 |
| Final ordinal-preserving control observer | e70421178bc32702ca09a7659f1d358d62649d10d7313b98104a942d09c14deb |

Both original-app observers have192ownAOT/204ELF/804static16KiBLOADs, offline/nondebuggable,
ZIP CRC/16KiB and v2/v3 verification. The three separate SDK controls have20/30/110, are offline,
nondebuggable and v3-only at minimum SDK36, using the independent SDK Debug certificate rather
than a shipping owner identity. Initial borrowing of a product v2/v3 expectation is refused and
preserved; the artifact is qualified only for its actual36target/signature. No re-signing or
lowering of the product signature requirement occurs. Static16KiB is not hardware acceptance.

Retain the early log observation before the one signal callback was published, the pre-mutation
pidof-absence helper refusal, the signature-scope refusal and the rejected shell-quoting attempt.
Continue only pending observations of the same issued signal; no accepted action is replayed.

Return en/System, font1.0 and physical1080x1920/density420. All five original OS values, locale,
boot and full20source match exactly; native Root returns to original OnboardingPage. Uninstall
only the two fresh packages/UIDs; hash-match and remove14device XMLs/94866capturedbytes. Their
local copies remain. Only this fresh emulator closes; official AVD deletion removes24files/
5525812154logicalbytes. Three standard Cleans remove14198registeredfiles/994040329logicalbytes.
Five actually used signed APKs, canonical source archives and safe local evidence remain. Counts
are logical file sizes, not measured host free-space reclamation.

Supplementary deletion of32new targets is rejected before process creation with blocked by policy:
1197files/373862259logicalbytes remain across six residual roots,twelve unused publish files and
fourteen local XMLs. No retry, subset, alternate method, move, escalation or reuse of that entire
rejected scope is allowed. Earlier refused scopes remain untouched.

Phone/D-607/three deadlines/consumed reboot/unread post-reboot source and the explicit other-project
phone turn remain protected. General stability, two-second performance, whole database/keys,
accessibility, iOS, provider, Store and release gates stay open. Unchanged product Windows/full
shipping builds and old financial/successful suites are not repeated.
