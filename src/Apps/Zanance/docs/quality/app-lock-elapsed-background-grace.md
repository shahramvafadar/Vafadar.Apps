# Original app-lock background grace (D-720)

## Actual remaining security input

SEC-04's original grace permits a brief background switch for30seconds. The original
AppLockService measures that interval from UTC alone. A correction back into the short,
nonnegative interval can therefore uncover the app after a longer real absence and release
queued destinations without a new authentication. Four actual original-service cases fail:
elapsed31/wall1,3600/1,30/1 and31/29seconds. The short29/29 and negative-wall refusal controls
retain their expected result. These use a real disposable store and independent clocks,
not machine clock changes or genuine owner authentication.

## Bounded source correction

Capture the injected TimeProvider timestamp alongside original UTC before showing the sleep
cover. Grace requires both elapsed and wall intervals to be nonnegative and less than the
existing30seconds. Android's original MauiAppLockHost additionally captures sleep-inclusive
SystemClock.ElapsedRealtime. If that clock goes backward, reaches30seconds or disappears after
capture, keep the original cover. Other hosts keep their timestamp/wall checks. The original
already-locked, lost-cover, PIN, device-authentication and queued-navigation policies remain.
No grace extension, new permission, biometric/credential change, financial schema/writer,
runtime, GC or new device identifier is introduced.

## Focused checks and retained failures

Thirty-one distinct selected cases pass: eleven new elapsed/counter cases and twenty affected
existing AppLockTests. The first changed run passes27 and refuses four new counter inputs
because the fixture supplies Int32 to nullable Int64. Correct only those literals, rebuild
and run the affected five-input method; its four missing cases pass and its null-case
control is retained. Do not count the repeated null control as another test. Strict selected
project compilation has zero warnings/errors. No unrelated successful suite is rerun.

Private native compilation initially lacks IsAvailableAsync on its deliberately unavailable
authenticator. A first text correction misses the source newline and leaves the same failure;
retain both failed builds, then compile the actual corrected interface implementation. No
installation occurs before the corrected package is independently verified. An initial
window observer reads the wrong dumpsys subcommand and refuses before any PIN input marker;
correct only that observation, without replaying setup or an accepted submission.

## Actual original modal and clock behavior

A fresh API36/x64/4096 emulator, boot96b96262-4759-420d-b704-d01fbcb7fdef,
UID10216/PID4666 runs original production application sources. Only original AppLockEnabled
metadata and one independent fictitious app-PIN verifier are prepared. No account, transaction,
opening, plan, import, financial Save or device-owner authentication occurs. The memory verifier
is diagnostic input and does not qualify Android Keystore or the owner's real credentials.

The actual original LockPage accepts its initial fictitious app-PIN input. Original SleepAsync
then creates the real modal cover and retains one queued nonfinancial destination. After
**41,380ms actual elapsed time**, a real clock correction in only this owned emulator yields
**1.987969seconds wall time**. Original ResumeAsync keeps LockPage and refuses the queued
destination. This executes the original methods and actual native modal; it does not claim a
separate complete Android OnStop/OnResume lifecycle or actual deep-suspend experiment.

The initial native observer also requires unchanged verifier write count and refuses. Original
LockPage.PromptAsync actually verifies the empty field on automatic resume, adding one failed
attempt: this is a separate real **D-721** defect, not an accepted no-write result. Preserve the
failed observer and delta. The elapsed-grace assertion independently passes from the original
saved records; no clock, preparation or Resume command is repeated. After actual UTC and all
seven settings restore, a distinct required fictitious app-PIN input releases that cover and
only then opens the queued destination. One distinct100ms short-grace transition closes its
cover normally, without another PIN attempt. D-721 automatic-empty-attempt repair stays open.

The complete original22 typed collections/settings packet remains equal throughout both
native outcomes. Main/D607 are untouched. Native package is offline/nondebuggable/v2v3,
192AOT/204ELF/804static16KiBLOADs and ZIP/CRC verified; digest is recorded in local evidence.
Static alignment is not hardware16KiB acceptance.

## Restoration, retirement and delivery limits

Actual UTC, original zone/boot and all seven settings restore exactly. Three owned XMLs are
hash-matched before remote removal; local evidence remains. After final full-source/OS proof,
only the fresh UID/package is uninstalled; active own pending/posted requests are absent. Only
AVD5572 is stopped and officially deleted; its prior23files/5,196,165,853logical bytes are
inventoried, not host free-space proof. Three named used private source files are archived and
digest-matched before supplemental output retirement. Earlier frozen scopes are untouched.

Strict Windows and complete offline shipping compilation pass without warnings/errors.
Shipping SHA256 `a123e70734ecd12df9e89abe3df57b827ffa6b7676486bc94549d8f92bad9753`, 99,397,535bytes,
384AOT/408ELF/1,608static16KiBLOADs, ZIP/CRC and v2/v3 pass. The private provider is absent
from shipping. Main has not adopted this candidate. Four standard Cleans pass. Supplementary
removal of seven new scopes is rejected before process creation with `blocked by policy`;
941files/284,818,730logical bytes remain frozen without retry, subset, alternate method,
move, escalation or reuse. Signed used packages, archive and local evidence remain.

D-721 is the next named observed defect. Physical/Main adoption still waits for actual D-607
completion/cleanup; its three deadlines, consumed reboot and unread source remain protected.
No early phone command, reboot, provider, Activity or financial replay occurs. Full SEC-04,
owner/biometric/OEM acceptance, general stability, performance, deferred accessibility,
providers/iOS/Store/release are not established. iOS follows completed Android under D-714;
the overall delivery goal remains active.

## Timing sources

[Android SystemClock](https://developer.android.com/reference/android/os/SystemClock#elapsedRealtime())
defines the sleep-inclusive clock. [TimeProvider.GetElapsedTime](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider.getelapsedtime?view=net-10.0)
uses captured timestamps. The reviewed [.NET10 Unix stopwatch](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Unix.cs)
and [native clock implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/native/minipal/time.c)
explain why Android's explicit boot clock is an additional gate; this source review does not
prove every installed/OEM clock implementation or a real suspend experiment.


## Subsequent D-721 resolution

The automatic empty-attempt defect observed above is subsequently corrected by separating original automatic focus from explicit PIN submission. A distinct new native five-request sequence keeps verifier writes1to1, and only one explicit fictitious PIN unlocks/releases its queued destination; the D-720 clock/grace sequence is not replayed. [Exact subsequent source/native scope](automatic-pin-presentation.md).
