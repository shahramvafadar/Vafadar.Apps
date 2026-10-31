# Startup callbacks retain their creating window (D-703)

## Actual defect and complete correction

The original CreateWindow created each StartupViewModel with parameterless preparation,
key-recovery and backup-recovery method delegates. Those methods read mutable _startup
and Windows.FirstOrDefault when invoked. A queued request from an already replaced
window could therefore capture the new window/model as if it had created the request.
Existing post-capture ownership guards did not protect this earlier entry boundary.

Bind all three original callbacks to their creating Window and StartupViewModel.
Require that same window to remain in the application, the current startup pointer
to match that model and its actual StartupPage/BindingContext to match, before resolving
protected preparation or recovery authority. The compatibility OnStart dispatcher also
captures its original model before queuing. Window construction still performs no
financial database preparation: Created dispatches the same covered worker afterward.

Original startup preparation, storage, migration, key handling, access policy, native
confirmation, explicit retry, recovery receipt and publication owners are retained.
There is no new authentication fallback, permission, financial writer, resource text,
schema, compiled model, dependency, runtime, GC policy or personal/provider data change.
The new Window receives its captured cover before it is returned to the platform.

## Failure on the published baseline

Use one fresh owned API36/x64/4096 emulator, original1080x1920/density420. Only the
original Created/compatibility initialization entry is held in a private caller;
preparation/recovery bodies and original window/view-model objects remain real. This
is an explicitly controlled scheduling interleaving, not an artificial database or
substitute startup algorithm and not a frequency/timing measurement.

The original native application starts UID10216/PID4278, protected StartupPage and
one unreleased original startup model. A real font1to2 configuration change recreates
the Activity and original window; two distinct startup models now exist, with the
new actual window current and both callback entries still held. Release only the
oldest model's original callback. It prepares and publishes the new actual window's
OnboardingPage, although that new model's own callback is still unreleased/Busy=true.
Actual native XML agrees with the published page. This proves cross-window adoption,
not merely a concerning source pattern.

Releasing the new model's own original callback then fails with InvalidOperationException:
its protected StartupPage has already been replaced by the other request. The actual
new model becomes Failed=true while onboarding remains visible. Complete21 typed source
collections and four catalogue preference values remain equal after this failed second
request. Only default protected startup metadata exists; accounts and ledger are empty.
No financial/profile preparation port, onboarding completion, account or successful
financial action is used to construct or replay the case.

## Corrected original native continuation

A necessary signed no-reset update preserves the same UID, boot, catalogue and full
source. Android labels this update's launch WARM; process4278 becomes5301, not a claimed
cold or continuous process. Its initial callback is held at the same original boundary.
A real font2to1 restoration recreates the Activity/window and two original models.
The directions differ because the baseline's font change is now being restored;
this is a callback-owner regression, not a font/layout comparison.

Release only the old model's original callback entries. The retired model fails with
OperationCanceledException, while the new model remains unreleased and its actual
StartupPage remains covered. The actual preparation-body counter after the entry guard
is0. Both old Created/compatibility signals are observed; their refusals are not counted
as two distinct new tests or two preparations.

Invoke that actually refused old model's original Recover and RestoreBackup commands
once each. Both return without protected recovery-authority capture; the counter at
that original host boundary stays0 and the new window remains covered. No authentication
prompt, code, credential, backup selection, recovery submission or replacement storage
request occurs. This verifies retired entry refusal, not successful real-owner recovery.

Release the current model's own callback once. The original worker prepares exactly
once, its model becomes ready/not-failed and original onboarding is published. All21
original typed source collections and four catalogue values equal the baseline. There
are0 accounts and0 ledger rows. Native XML, actual window/model identities and counters
are retained. The successful callback outcome is not replayed to repair an observer.

This closes the demonstrated pre-entry cross-window defect. It does not establish the
exclusive cause of D702's earlier title-only StartupPage or of any SGen/managed-cache
failure, and it is not general Android recreation, crash, ANR or stability acceptance.

## Focused existing guards and packages

Six distinct existing original tests pass on .NET10.0.12: pending/completed startup
exclusion, explicit failed-preparation retry, retired deferred publication, replaced
model refusal, initially replaced root and departed recovery before secure read.
All6 execute, with0 failures/errors/skips. They protect retained behavior; the actual
native before/after interleaving above is the new regression evidence, not six new
unit tests. The focused host build has0 warnings/errors and uses isolated outputs.

Strict Windows Debug compilation passes with0 warnings/errors. The full signed offline
shipping APK is prepared at E:/Zanance.ReviewBuilds/D703-shipping-package/pro.vafadar.zanance-Signed.apk:
SHA25637586bc981da9f819e985ccbac8e9ee36934eec7dc85af979fcc7b1cdb871adb,
99307423bytes,384 own AOT libraries/408 ELF files/1608 statically16KiB-aligned LOADs.
ZIP CRC/16KiB alignment, v2/v3 signatures and offline/non-debuggable manifest pass.
The actual shipping manifest contains no private review authority/provider.

Both used private native callers pass strict compilation,192 own AOT/204 ELF/804
static LOADs, v2/v3, ZIP CRC/16KiB and offline/non-debuggable checks:

| Original native caller | SHA256 | Bytes |
|---|---|---:|
| Published baseline with held entry |1131c71fbd7fecbdf9e97e5f7dc372562bd7024c7dc0049d86f467b12acefb20|56864674|
| Corrected bound callbacks |e8fee06f51c4271722767e172f577620b5c1eca1ef3ec49767219d81b9f6fa15|56872866|

The private gate/counters are absent from production source. This is not whole-PE byte
identity, hardware16KiB, API24, physical-phone, iOS or owner-authentication acceptance.
No successful layout matrix, financial setup, PDF/CSV, notification, widget, restore
or unrelated full test suite is repeated. No visible text/layout changed.

## Exact preservation and cleanup

Original boot, locale, font, screen timeout, accessibility-enabled/service-list/TTS
values and1080x1920/density420 are exact. Final original source/catalogue and onboarding
are observed before removing only the fresh package/UID. Three owned native XML files,
41354bytes, match saved SHA values before remote/local cleanup. The actual owned
host57024/qemu54608 exit and only its official named AVD is deleted:25files,
5467354159logicalbytes. Phone, other ADB servers and retained cohorts are untouched.

Four scoped standard cleans succeed, with3545 registered deletion lines in total.
Windows assets are restored for only that target before its pending Debug clean,
without replaying a successful build. Deletion lines are not a unique-file or host
free-space total. Used signed packages, used source, native/full-source packets and
receipts remain preserved outside the following refused scopes.

Automatic approval review rejects the sole nine-target supplementary cleanup before
process creation with blocked by policy:947files342295663logicalbytes remain frozen.
Those scopes are this caller's private artifact root, isolated focused-test output root,
residual AVD root and six unused publication files. Used signed APKs are excluded.
No retry, subset, alternate method, move, escalation or reuse is permitted; previous
refused scopes remain untouched.

D607's three original deadlines, consumed reboot and unread post-reboot source remain
protected. Prepared shipping is not installed on the phone; Main adoption waits for
actual D607 completion/cleanup. Broader DB/key/security, stability, performance,
accessibility/iOS/provider/Store/release gates remain open and the delivery goal stays active.

## Sole D702 CI response

The single scheduled exact-head response shows [CI in_progress/no conclusion](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41238340564) and [CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41238340507). Its exclusive consumed marker, raw response and receipt are retained and only that schedule is deleted. No second query or later CI success is inferred.
