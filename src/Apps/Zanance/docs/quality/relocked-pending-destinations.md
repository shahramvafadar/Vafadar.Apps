# Preserve pending destinations after relocking (D-495)

## Reproduced defect and correction

The original AppLockService copied and cleared all pending destinations at unlock, then awaited
all of them without checking access again. If the first destination backgrounds/re-covers the
app, later destinations still execute while IsLocked=true. Two new original-source regressions
fail with expected order[1], actual[1,2], with both an available and missing window port.

Keep one drain owner and dequeue one destination only while access is unlocked. A new cover
retains every unstarted action in the original queue. New input during an awaited drain joins
that queue instead of overtaking earlier destinations. Nested drain calls do not duplicate work;
one refused destination still does not discard the rest. The running action is not replayed or
retrospectively cancelled; this is not cancellation of all native effects it already started.
No device credentials, private Main keys or financial actions are involved in reproduction.

## Focused evidence

Three new cases pass: two re-cover/window states and arrival order during suspended navigation.
Forty-one existing AppLockTests/AppLinkTests pass once against the candidate,44 selected successes
in total. This is +3 managed cases, not a rerun/new count for the whole suite. Initial direct DLL
invocation with MTP double-dash options was refused before execution; documented single-dash class
selection runs41 cases. The focused strict compilation and subsequent solution output clean pass.

The independent original Android Home caller runs two real native LockPage/MauiAppLockHost cases
on API36/x64. Three owned native unlock button clicks qualify the new cover stopping later work,
then order1/2/3, and new input waiting behind suspended navigation. Complete original source/base
packets stay equal and no financial command or PIN write occurs. Authentication outcomes use a
stated native-effect port; this does not accept real owner credentials, biometric hardware or PIN.
Preserve initial missing-namespace compile errors before native setup. One corrected complete
signed Offline192-AOT caller build passes. Accepted per-case receipts and original protected base
are retained; restore exact old caller APK/process absence, emulator Main28047/all139 public
Home tuples/APK/OS. Two exact owned XMLs are hash-matched/removed. No successful case is repeated.

Strict Windows has zero warnings/errors. The first complete signed ARM64 shipping build passes;
final delivery preserves both shipping architectures, with384 AOT assemblies and all ten native
engines equal to D-490. Full signed Offline package:
`E:\Zanance.ReviewBuilds\D495-shipping-all-package\pro.vafadar.zanance-Signed.apk`,96,980,895 bytes,
SHA256 `a77d28705e222775a3f4369c6ca31d41de5cb83bbb417ffa28be8997b96a4d99`, v2/v3, nondebuggable,
no INTERNET. No new SDK, permission, data model/schema, public resource or financial behavior.

Samsung no-reset upgrade31885-to25043 preserves Home979EUR/all141 original public tuples and exact
OS. Two owned phone XMLs are hash-matched/removed; no private Main row/key extraction, profile
creation, financial Save, native credential/PIN or account/system-setting change. This public
preservation is not full private-database or whole-device acceptance.

## Preserve the separate timed-cohort interruption

Replacing Main unexpectedly brings the retained D-494 Activity forward. Native events prove
PID24914 started as top-activity, without a provider-created process. Original protected startup
refreshes its native requests; first19085/dead alarm lineage is interrupted, not accepted as an
uninterrupted cold-delivery interval. The unchanged independent APK/UID/run remains installed.
Read its already existing24914 once only for diagnosis: complete original source matches, original
four ids/dates remain and no seed/financial command/manual rearm is repeated. Capture actual new
alarm/PendingIntent identities separately; preserve original identities and failed observation.

A cached-only kill guard refuses24914 at adj850/cached=false, so no kill is attempted. The future
observer explicitly records the interrupted lineage and actual live process; it must not assert
a new cold start if that process survives. Actual09:00 delivery is still pending. This correction
is separate from accepting that timed test. Main restoration for D-494 follows the authorized
D-495 upgrade witness rather than falsely claiming the old D-490 hash/PID is unchanged.

## CI and remaining gates

The single stored8ceebacf response returns CodeQL39965624278 and CI39965624215 success; its consumed
monitor is deleted, with no second GET. D-495 gets one delayed exact-head check after push.
Real credentials, remaining notification/profile races, Doze/reboot, general security/stability/
accessibility, iOS/provider/Store and release remain open. Continuous delivery remains active.
