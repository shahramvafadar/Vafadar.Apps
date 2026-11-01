# Same-boot protected PIN retry time (D-722)

## Actual original SEC-01 failure

After five wrong attempts, the original UTC-only deadline can be bypassed by moving UTC
forward while only one real second passes. Both original cases fail after reconstructing
PinLock against the same stored state: a correct PIN clears failures5to0, and a wrong PIN
consumes another guess,5to6. The promised original elapsed minute has not passed. These are
two actual original-service failures with fictitious verifier storage, not owner credentials.

## Complete bounded correction and compatibility

Android registers an explicit IPinAttemptClock reading its coarse public boot generation and
sleep-inclusive elapsedRealtime. New penalties preserve their original UTC fallback and an
optional protected same-boot target. A known current boot uses actual remaining elapsed time,
even when UTC moves forward or backward. Success clears both deadlines. Five attempts, initial
one minute, original doubling/fifteen-minute cap, PBKDF2 parameters, owner checks, semaphore,
current-PIN/configuration/recovery and original secure writer remain unchanged.

PinState stays version1 with one optional RetryDeadline. Null is omitted, retaining the original
neutral six-field JSON shape. Existing source-generated SecurePinStorage reads old/new shapes;
no SQLite entity, financial migration, compiled model, SDK, permission, runtime or GC changes.
A known different boot and a legacy unanchored record retain their old absolute-UTC semantics,
without a guessed elapsed origin. If a persisted anchor requires a clock which is now missing,
invalid or impossibly far ahead, verification fails closed instead of falling through advanced
UTC. Unknown clocks before any anchor retain the old representation. Unconfigured platform ports
retain legacy behavior; this Android correction is not Windows/iOS clock qualification or a
promise of tamper-proof elapsed time across reboot. PIN state remains device-local and excluded
from portable backup. No unique device identifier or credential is added.

## Focused verification, not unrelated suites

Entire strict Release test-solution compilation has zero warnings/errors. **24 distinct selected
cases pass:16 new and8 affected existing**. They cover current/recreated gates, forward/backward
UTC, real expiry, new-boot fallback without a new origin, missing/invalid required clocks,
malformed protected anchors, legacy/current JSON and the unchanged progression/cap. The first
two corrected cases remain accepted; only the14 unfinished new cases and8 affected existing
cases subsequently run. A combined class/method selector reports zero tests and is rejected;
its log is retained, the selector alone is corrected and exact XML counts are required.
No successful D-72031-case/clock/grace, D-721 prompt or old financial cohort is repeated.

## Actual original secure storage and cold source

A fresh API36/x64/4096 emulator uses original registered SecurePinStorage, PinLock and
AndroidPinAttemptClock, not the earlier diagnostic memory verifier. It creates only one new
fictitious app PIN and the original five wrong guesses. There is no account, opening, plan,
ledger entry, import, payment or genuine owner authentication. Native source-generated storage
actually emits/reads the neutral six-field shape, then persists the optional anchor after the
five-guess threshold. Only bounded nonsecret metadata/booleans are observable; no raw verifier,
salt, PIN, key or encrypted-storage value is exported.

Boot a236c2a9-b0f8-4103-adaa-c49a2b994f68 / UID10216 begins in PID4497. The first cached-process
precondition refuses; no kill or clock mutation follows that refusal. Later actual cached/adj900
permits one scoped am-kill and proves death. One normal cold launch creates PID5332, retaining
exact original protected metadata, required marker, complete22typed financial-source packet and
actual LockPage. The first minute has naturally expired during the real wait. **An active
one-minute native forward-clock window is not accepted**, and that original deadline is never
rearmed, reset or recreated to fill the missing observation.

## Retained continuation through actual original penalties

Move only the owned emulator's clock behind the original UTC cutoff after elapsed expiry.
One distinct wrong submission through original LockPage advances5to6 and starts the original
120second penalty. The first observer reads before the asynchronous UI write completes;
retain that stale result. Read the actual terminal UI/status without repeating input and preserve
the accepted sixth guess. Its active observation window later expires while awaiting observation;
no clock-forward or correct-submission marker was issued for that expired window.

Continue the same accepted state only after its actual expiry. One distinct seventh wrong
submission advances6to7 and starts the original240second penalty. No old five-guess preparation,
accepted submission, credential configuration or financial action is replayed. A real one-hour
forward clock change inside this new penalty makes its absolute UTC cutoff past. **One correct
fictitious PIN submitted through the actual original native entry remains refused**; LockPage
shows “Too many attempts. Try again in234seconds.” The caption falls within actual before/after
counter brackets234…238seconds. Protected attempt/deadline metadata remains exact; full original
financial source is unchanged and the required PIN remains enabled/covered. The source cache and
elapsed targets, rather than the chosen observation timeout, determine every expiry/admission.

This is an actual active fourth-minute native result after original cold persistence, plus
managed one-minute boundaries and native expired-minute continuation. Keep those scopes distinct;
it is not an active first-/second-minute native result, an actual device reboot, every OEM,
physical owner/biometric/Keystore attestation or all SEC-01/04 acceptance.

## Packaging, restoration and retirement

Strict Windows and full offline Android compilation pass without warnings/errors. Shipping
SHA256 `9a9ac82178602625250706c23771a34d0d2e6515f8504fc53c2be18b1bd87fa8`, 99,413,919bytes,
384AOT/408ELF/1,608static16KiBLOADs, ZIP/CRC/nondebuggable/offline/v2v3 pass. Independent used
native SHA256 `f06ddc63636b053f0de5371f1030c47db0780bea2a4b2423979ef99e00bc7a92`,
192AOT/204ELF/804staticLOADs has the same package checks. The private diagnostic provider is
absent from shipping. Static16KiB alignment is not hardware16KiB execution.

Four standard Cleans pass. Official AVD retirement inventories 23files/
5,202,784,979logical bytes; 5owned XMLs/29,554bytes are hash-matched before
remote removal, with local evidence retained. Supplementary seven-scope deletion is rejected
before process creation with `blocked by policy`;1,112files/293,653,872logical bytes remain
frozen without retry, subset, alternate method, move, escalation or reuse. Earlier refused
scopes remain untouched. These logical counts are not host free-space measurements.

All real clock changes restore actual UTC and the original seven settings/zone/boot; the initial
failed precondition also records no clock mutation. Final financial-source equality and exact
declared protected failure delta5to7 precede retirement. Only the fresh UID/package is uninstalled;
active own pending/posted requests are absent. Only AVD5572 is stopped and officially deleted.
Used signed packages, three digest-matched named private-source files and every failure receipt
remain available outside refused scopes. No other emulator, ADB server, host/phone clock or owner
data is changed. No accepted financial command is repeated.

Samsung/Main adoption still waits for actual D-607 delivery/source/cleanup. Preserve all three
deadlines, consumed reboot and unread post-reboot source. Android completion precedes iOS under
D-714. General security/stability, two-second speed, deferred accessibility, providers/accounts,
iOS/Store/release remain separate; the overall delivery goal is active.

## Primary clock references

[Android boot count](https://developer.android.com/reference/android/provider/Settings.Global#BOOT_COUNT)
and [sleep-inclusive elapsedRealtime](https://developer.android.com/reference/android/os/SystemClock#elapsedRealtime())
explain the platform input. Native records above prove only the finite source/runtime outcomes,
not a unique identity, trusted wall-clock service or elapsed clock spanning a real reboot.
