# Automatic PIN presentation without a submitted guess (D-721)

## Actual observed original defect

D-720 retains the actual original LockPage after its clock-corrected long interval, but its
first no-verifier-write observation refuses: PIN writes increase from2to3 without any new
PIN input. Original MauiAppLockHost.Cover.PromptAsync calls LockPage.PromptAsync directly;
that path verifies the empty field and increments the existing original attempt state.
Repeated automatic Resume/display requests can therefore spend the five-attempt allowance.
Retain D-720's failed observer, original write delta and independently accepted grace proof.
Do not replay its clock mutation, native setup, short grace, accepted PIN submissions or
financial cohorts to reproduce this already observed source behavior again.

## Complete bounded correction

The original modal adapter now calls a separate internal automatic-presentation method.
With an app PIN configured, it focuses the original password entry and returns without
submitting the verifier. With no app PIN, it continues through the original device-authentication
prompt. Original Enter/Unlock handlers still call the same submitted PromptAsync path; actual
incorrect or explicitly empty submissions retain the existing attempt/rate-limit behavior.
No verifier/KDF, lock duration, disclosure, device-authentication outcome, current-cover rule,
financial writer, schema, permission, runtime, GC, new UI string or release policy changes.

Only two production UI files change. There is no new managed test count and no rerun of the
unchanged D-72031 selected cases, PIN algorithm suite or earlier native successes. The actual
new assertion belongs to the original MAUI modal and receives its own changed-source native
cohort, strict Windows compilation and complete offline Android package.

## Actual original native page and submission

A fresh owned API36/x64/4096 emulator, boot0abe35b5-1b5a-4089-bff3-9716b361272d,
UID10216/PID4224, compiles original production sources with a private process/run-bound caller.
It uses only original AppLockEnabled metadata and one new fictitious app PIN. No account,
opening, ledger transaction, plan, import or payment is created. Its independent in-memory
verifier is explicit diagnostic input, not Android Keystore or genuine owner authentication.

After actual startup and original LockPage presentation, verifier writes are1, the cover is
locked and one nonfinancial destination waits. One consumed command issues **five original
ResumeAsync requests**, separated by100ms for native focus delivery. All five finish with
writes still1, zero opened destinations and the same original LockPage. The native entry is
focused; actual own XML independently shows the original UnlockPin resource, password flag
and focused state. This is one original threshold-bound native sequence, not five unit tests.

Then **one explicit fictitious PIN submission** through that actual native field/Enter
accepts the original PBKDF2 verifier, raises writes to2, closes the cover and opens the queued
destination exactly once. The complete original22 typed collections/settings packet stays
exact across preparation, automatic prompts and that submission. No clock change, D-720
long/short transition, wrong-PIN cohort or accepted financial action is repeated.

The first fresh-emulator readiness observation refuses while ADB is offline. The original
emulator and QEMU processes remain live; retain them without a restart. Actual boot later
completes normally. No installation or preparation occurs before verified readiness and
package admission. Final native build has zero warnings/errors; independent used package
SHA256909bc0c21af9afb8cf0675c6a0dcc3bc99f807de6a912b3d73594d65823246d3,
192AOT/204ELF/804statically aligned16KiBLOADs, ZIP/CRC/offline/nondebuggable/v2v3 pass.
This is not actual hardware16KiB, genuine owner/biometric or every OEM lifecycle acceptance.

## Exact restoration and retirement

No clock, display, accessibility, language, permission, screen-timeout or other system setting
is changed for this cohort. Final original boot, zone, size/density and seven captured settings
are exact; full typed source equality precedes retirement. One owned XML is hash-matched before
remote removal; local evidence remains. Only the fresh package/UID is uninstalled and active own
pending/posted requests are absent. Only its AVD5572 is stopped and officially deleted. Three
named used private source files are archived and digest-matched outside every refused scope.

Strict Windows and complete offline Android compilation pass without warnings/errors.
Shipping SHA256 `4ea412ebf603b0200dabdbf090f6f77b2b138461ec81120d5feacaf7d9127fa8`, 99,401,631bytes,
384AOT/408ELF/1,608static16KiBLOADs, ZIP/CRC/offline/nondebuggable/v2v3 pass; the private
provider is absent. Three standard Cleans pass. Official AVD retirement inventories
23files/5,255,672,575logical bytes; the owned XML is
5,937bytes. These are not host free-space measurements. Supplementary removal of
six new scopes is rejected before process creation with `blocked by policy`;
839files/282,642,778logical bytes remain frozen without retry, subset, alternate method,
move, escalation or reuse. Earlier frozen scopes remain untouched; signed used packages,
source archive and evidence are preserved.

Samsung, Main, D-607's three deadlines, consumed reboot and unread post-reboot source are
untouched; phone adoption still waits for actual D-607 completion/cleanup. No early provider,
Activity, install, reboot, rearm or system-clock exception occurs on that phone. Whole SEC-04,
real authentication, general stability/performance/A11Y, provider/account/iOS/Store/release
requirements remain separate. Android completion still precedes iOS and the delivery goal
remains active. This closes only the named original automatic-empty-attempt defect.
