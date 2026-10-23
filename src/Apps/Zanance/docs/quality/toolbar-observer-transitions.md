# Original toolbar observer transitions (D-485)

## Reproduced defect

Extract the existing separate weak-owner global-layout listener without changing its initial
registration/current-tree retirement behavior. Compile that original behavior into a separate
permission-free Release/x64 native caller. The original toolbar moves from one live Activity
root into a second actual Dialog window. In process6114, the original behavior reports initial1,
detachedCallbacks1, movedCallbacks0 and retiredCallbacks0, with both roots still alive, then
refuses its lifetime contract. The outgoing root still drives the detached toolbar and the new
window never drives its title geometry. This is a native registration defect independently
reproduced on the emulator; no Samsung condition or general runtime diagnosis is inferred.

## Correction and focused native evidence

The original Shell tracker now owns AndroidToolbarLayoutObserver with a weak owner and a static
callback. Observe native attachment as well as layout. Remove the exact outgoing global listener
on detachment; add it to the new root on reattachment. For initial pre-attachment registration,
Android merges the dead floating observer's listeners into the window root: capture that root
without adding a duplicate. Retirement neutralizes callbacks, removes both native registrations
and leaves the Java peer valid for in-flight dispatch snapshots until the bridge releases it.
Do not retain a growing list of historical window roots for a long-lived toolbar.

The fixed linked production source in process6227 reports initial1, detached0, moved1, retired0
with both roots alive and accepted=True. A separate before-attachment case in6337 reports the
same counts and accepted=True, exercising the actual floating-tree merge. Keep the first fixed
case; do not rerun it for the second case. The final source uses explicit Android View aliases
after the first shipping compile exposes a MAUI/global-using name ambiguity. Preserve that build
failure; the alias correction changes no native behavior and does not require native replay.

Both independent Debug/Main-identity guards refuse. Strict Windows build has zero warnings/errors.
The helper is complete, v3-signed, x64 and permission-free. Remove only the newly installed helper;
original emulator Main28047 and all139 public Home tuples match the pre-run frame, and remove
the two exact SHA-matched owned XMLs. No emulator settings, financial actions or original APK change.

## Complete package and original physical continuation

Build-AndroidApk.ps1 -Offline completes without warnings after the explicit native type aliases.
The complete signed package is E:/Zanance.ReviewBuilds/D485-shipping-package/pro.vafadar.zanance-Signed.apk,
96,964,511 bytes, ZIP-valid with384 AOT libraries and valid v2/v3 signatures. Its SHA256 is
becc2226bc84fe79bc9e86bb87d4564b147e3eecaeef4abaa9c442cfb3371cce. Package identity remains
pro.vafadar.zanance, with ARM64/x64 and no INTERNET/ACCESS_NETWORK_STATE permission.

Upgrade Samsung Main without reset; the upgrade replaces process27860 with4933. Observe the
original startup cover, then continue the same process to its ready Home without a restart.
Original More/Accounts opens the native Accounts title and enabled Back arrow. Invoke original
Back once and Home once. All141 public Home tuples, including979EUR, equal the pre-upgrade frame;
font1.0,1080x2340,density450 and existing accessibility settings match exactly. The installed APK
hash equals the verified final candidate. No financial action, profile creation or system-setting
change occurs. Remove12 exact SHA-matched owned phone XMLs. This is a bounded English/Light
native title/Back/upgrade check, not a replay of accepted multilingual/OS-scale financial cohorts.

No schema, resource, SDK, permission or managed-test count changes. The only repeated build is
Android after its actual compile failure; the successful Windows/native/business results stand.
The pushed commit retains its own one-shot delayed CI gate.

This is two focused native transition cases, not new managed tests. Preserve previous successful
language/OS-scale/title geometry, validation, financial and business-suite evidence. General
ANR/SGen/GC/crash, whole-Home speed, natural low-memory, audible accessibility, other devices,
iOS/provider/Store and release acceptance remain open. The broad delivery goal remains active.
