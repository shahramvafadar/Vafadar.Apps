# Null local snooze slots must not abort original reminder publication (D-605)

## Reproduced defect and complete change

`[null]` is valid JSON. Deserializing it as a nonnullable-record array does not stop
System.Text.Json from returning a null element. Original Keep then dereferences it;
the actual ReminderService also dereferences NotifyAt before reaching its native
replacement. Its outer failure handler preserves the failed refresh rather than
reconstructing the current queue. This is a real local-cache parsing defect, not a
missing time-zone feature or evidence of changed financial authority.

Before the fix, three new Core cases fail, including the actual Keep null-reference,
and two actual SQLite/store/service cases fail because the expected queue publication
does not occur. Parse a nullable-element array and retain only real records before
the caller's existing cap. Preserve their original fields, ordering and expiry.
Malformed JSON/root behavior remains unchanged; the original scope/link/current-plan/
privacy and publication guards still rebuild every accepted snooze. No record
normalization, financial write, schema/migration, localization, SDK, permission,
inexact scheduling or same-day deadline policy changes.

Five distinct new managed cases are retained. After correction all five and one
affected existing serialization/replacement case pass on actual runtime14: Core4,
App2, zero failed/skipped. The full old suite, DST/day-boundary cases, financial
scenarios and native tap/delivery cohorts are not rerun. Direct DLL runners use
`-class`/`-method` and result XML; the initial MTP-style option is refused before
test execution. Exact XML totals prevent a zero-test false success.

## Strict construction and preserved failures

The Windows build is clean after selecting only the MAUI Windows framework through
VafadarMauiTargetFrameworks. Preserve the initial unscoped iOS-reference NU1201 and
the incorrect global TargetFrameworks/plain-library asset failure separately; no
product targeting workaround is adopted. Correct strict Windows has zero warnings/errors.

The independent reminder project's explicit source glob initially includes two old
generated files under the previously retained D-575 artifacts directory. Exclude
artifacts from that project's Compile/XAML globs; do not remove or retry retirement
of the earlier rejected output. The corrected independent build is strict.

The complete product publish initially fails AOT of unchanged x64
System.Text.RegularExpressions after compiling407 methods. The attempted .exe name
does not exist; the installed official assembler is a .cmd wrapper. That actual
wrapper assembles the retained same input successfully, and the subsequent incremental
canonical product publish succeeds. The initial error remains; an exclusive cause
or general compiler/stability fix is not established.

The final complete signed Offline product APK has SHA256
1f9816c40d9330f619fd7506bec327d3db2aaff1dd236c7ee692036c6b1351e8.
All384 own AOT images match emitted output,408 ELF files satisfy static16KiB,
ZIP CRC, non-debuggable/no-INTERNET and original-owner v2/v3 checks pass.
Actual independent and shipping Core PDB document checksums match the fixed source.
This is neither hardware16KiB nor a new complete release acceptance.

## One fresh native queue reconstruction

Use only ReviewSnoozeCache and pro.vafadar.zanance.snoozecachereview, actual owned
API36/x64 emulator. Reject shipping/other identities, ARM64 and combination
with retained timed/locked cohorts. The default older reminder branches remain
unchanged. A first warm request is supported; an active observer or consumed marker
cannot replay preparation. A terminal observer can resume only its same unconsumed
run. Create-new markers bind the run before original fictitious metadata writes.
Three direct target admissions actually refuse shipping identity, ARM64 and the
timed combination without compiling or operating a device; these are not managed tests.

Run3e04ca753ef447b1976a2f06c9b1c6fd first encounters generic covered preparation
failures in3616, including one real original Retry, without preparing the fixture.
The bounded observer later records IOException inside SqlCipherRestoreStaging in4130.
Only exception type/own managed method names are retained: no message, source path,
key or native memory/register data. Fixed refusal labels in the final observer do
not retrospectively supply a missing label for that earlier exception.

Available ordinary-shell storage540420KiB is below the unchanged staging requirement
540672KiB (twice the256MiB bound plus16MiB). This matches the observed refusal site;
the original exception's exact branch was not exported. Preserve the failed startup
and observer deadlines, not a claimed proven exclusive cause. The observer upgrade
also gets an actual insufficient-installation-storage refusal. Code-only KEEP_DATA
maintenance of this fresh installation preserves its original UID/CEDE. A mistaken
canonical-cache alias is refused before deletion; only its actual owned runtime
base.art is hash-matched/removed,452208bytes, with no source/journal/key/Main read.
Temporary emulator adbd root is returned to its original shell2000.

For sufficient original headroom, temporarily retire only the already stopped,
process/alarm-absent androidsyncportablereview code with KEEP_DATA. Its exact original
1e2ac643 APK already exists locally; do not recreate its seven-record fixture, launch
its provider, migrate it or read database/keys. After this cohort, restore that exact
APK and verify UID/CEDE, permission flags, stopped state and process absence. This is
metadata/code restoration, not a fresh whole-database/key equality assertion.
No AVD capacity, source guard or free-space requirement is weakened.

The final observer adds only System.Diagnostics.StackTrace to the independent
native assembly set:193 own AOT/205 ELF, all matched and verified. Its final launch
is WARM, not cold. Startup then completes and4651/UID10218 prepares only one zero-
opening account and one unposted future plan. No entry Save, settlement or old seed
is replayed. The normal notification permission is allowed only for this fresh
emulator installation.

Inject twenty null slots before one valid future snooze plus a trailing null, then
call original ReminderService once. Actual SDK pending list contains the original
plan and snooze. Identity/deadline/link remain original, captions come from current
source/privacy rather than cached text, and the complete typed account/plan/state/
settings/empty-ledger packet hash remains
92C5B7DC93EDB068658309A53B3BE92DD7EC419B0A24C2EB0DC61E8CD086A1AD.
Cancel only those fresh requests; the actual pending list is empty. Preserve its
single terminal result, then uninstall only the fresh package including its data.
The restoration continuation corrects pm-path's expected absence exit without
repeating retirement or native publication.

## Product adoption, restoration and cleanup

The complete product upgrades reserved Samsung without reset once: old24382/885f6df5
to17668/1f9816c4. Its first51-node loading capture is preserved as incomplete. Only
the same live17668's unfinished read is continued, without reinstall or financial
action; all141 original native attribute dictionaries then match exactly, including
Home979. UID/CEDE, caller, permission/stopped flags, five settings, boot and display
metadata match, apart from the separately recorded legitimate Main APK change.
No private shipping Main database/key or authentication is read/performed.

The emulator's original885f6df5 Main/zero/all120 and caller/package/settings/boot/
shell metadata are restored. The prior portable code is restored exactly as above.
Only the owned AVD stops and its original configuration hash remains
7b98e07e1fce0bacb029e882a0cb763600ec07e84fdf395c8a74f88def3f990d;
both existing ADB servers remain. Emulated Main is not claimed upgraded to this
product; its retained source/performance context remains on the original APK.

Eight new owned XMLs are hash-matched/removed:207653logical bytes. Four standard
Clean operations after a confined FileList preflight remove3175 registered files,
1041974987logical bytes; actual absence is confirmed. Used signed artifacts,
the existing portable original and local evidence remain. No earlier rejected
cleanup scope or protected archive is touched. These are logical sizes, not a
measured host free-space or full-disk cleanup claim.

Evidence pointer:`%TEMP%\Zanance-D605-current.txt`. Native queue publication is not
real delivery/Doze/reboot/punctuality, broad security/OEM, full database/key equality,
general ANR/crash, speed, accessibility, iOS, provider/Store or publication acceptance.
TalkBack stays last and Persian speech parked. The overall delivery goal stays active.
