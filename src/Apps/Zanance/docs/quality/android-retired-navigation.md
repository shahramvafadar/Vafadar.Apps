# Retired Android navigation ownership (D-360)

## Observed owner and correction

The retained D-359 original GoalEditor/theme/language sequence stopped after twelve completed,
input-preserving groups with actual input-dispatch ANRs. Native collection samples alone did
not identify an owner. A separate original-root experiment now establishes retention: eight
replaced AppShell instances remain alive and translated toolbar subscriptions grow from one
to nine. Removing only the subscription leaves all eight roots alive; that candidate is discarded.
The Activity fragment manager also retains six original ShellItemRenderer roots after four
replacements and the original-theme restoration.

The exact MAUI 10.0.110 [ShellRenderer source](https://github.com/dotnet/maui/blob/10.0.110/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellRenderer.cs)
disconnects the current item during handler disconnection but does not remove its root fragment
from the Activity manager. ShellItemRendererBase.Disconnect clears its current section/page/context;
Android destruction performs the child-renderer and toolbar cleanup. The old native owner therefore
survives replacement of the application's managed root.

AccessibleShellRenderer records weak references only to the item fragments it creates. The existing
AppShell retirement boundary asks that renderer to remove those fragments before detaching its
container. Android retains responsibility for destruction/disposal. There is no global fragment
enumeration/removal, forced synchronous transaction, manual native disposal, runtime/GC-policy
change or replacement toolbar subscription mechanism.

An unpublished first native-removal candidate releases the eight roots, but later actually crashes
with `No view found for id ... for fragment ShellItemRenderer`: an initial add can still be queued
when a second display update replaces its container. The final correction includes not-yet-added
owned fragments and uses reordered transactions, matching the SDK's initial add. Android can then
collapse the pending add/remove before creating a view in a retired container. This follows the
[Android fragment transaction reordering contract](https://developer.android.com/guide/fragments/transactions#reordering).
The crash and all earlier completed observations are retained; they are not treated as timeouts.

## Scoped native qualification

The corrected independent Release/AOT process PID10333, comparison
`6a9cd612f897489c84ab2b2cba3c9527`, source MVID
`fd76a625-56a1-4c31-b1db-76821c293fbc`, completes eight original root replacements. Every replaced
root becomes unreachable; one active native root and one toolbar subscription remain. Collections
observe the same retired cohort: an initial three-sample guard was too early, and the last root
required seven samples. This observation correction does not create another replacement or fixture.

Six original About/back-arrow operations cover en/fa/de in Light/Dark. Display changes preserve
the open detail, the actual native arrow has its current translated name and performs the original
command, and returning Home preserves one native root/toolbar subscription. Stored fictitious
accounts, entries and settings remain identical; original theme/language are restored. No financial
command or system setting is changed by this phase. The APK SHA256 is
`53abb20b42308b08938a4a385b27bd5cc476370425cce18a8bf70e8f700a7e5e`;
all four native engine files match the prior D-358 caller and 192 AOT libraries are present.

## Original previously failing sequence

Continue the retained original D-359 fixture, never reseed its marked work-editor database. The
same original 36-group GoalEditor/theme/language/retirement method now completes under actual
emulator font_scale2.0: PID10607, comparison `2e078747336446f5be6cbe9ff78797ed`, caller MVID
`2f736427-dbfd-48e0-8b41-0e238320447d`. All 4860 original Labels become unreachable, every completed
group preserves the six stored collections, and no Save occurs. Original language/theme restore.
No AndroidRuntime error is recorded for that exact measured PID. This passes the previously
failing twelve-group boundary without restarting the process or changing native runtime bytes.

The complete independent signed APK SHA256 is
`0fbd6aa62079f36b495bd60f7b3825847b7e656d8b33495bfe828fc5538c9dfe`;
strict compilation has zero warnings/errors. All four native engine files still match D-358 and
192 AOT libraries remain. The host restores exactly font_scale1.0, accessibility_enabled0,
absent enabled_accessibility_services and touch_exploration_enabled0 after the final receipt.
The temporary original-sequence/stage instrumentation is removed before committing. The maintained
independent root-retirement phase remains an explicit ownership regression, never production GC.

## Ordinary final package and restoration

The main Windows strict build succeeds with zero warnings/errors in 30.95 seconds. The complete
offline Release APK built with Build-AndroidApk.ps1 is
`artifacts/android-root-retirement-d360/pro.vafadar.zanance-Signed.apk`, 95268767 bytes, SHA256
`4e8f2b73fcb15db7537f8ec3056baa492b357e9c8e57de816fd13543f30c690b`.
The certificate SHA256 is `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`.
Verify permanent package identity, minAPI24/target36, ARM64/x64 and 384 AOT libraries. The binary
manifest and all eight native engine entries match D-359 exactly; INTERNET/debuggable/profileable
are absent. The build script's warning gate passes.

Install-r without clearing data on the owned emulator opens ordinary main PID11333. Its native
Home hierarchy retains Recorded balance0.00EUR and the existing to-My-account caption. A first
observer asks the narrower `dumpsys window windows` output for a field it does not contain; no
input is sent. Correct the observer to the actual `dumpsys window` focus record and inspect the
same PID. The Home XML SHA256 is `2f53c71a57dffbebfc9954a0df1ca533ece7d5a0794fabb81edb48bfb1f155e4`;
remove its single owned remote path only after hash matching.

Restore the independent install to the already qualified root-retirement-only signed APK,
without clearing fixtures. Stop only its identified unmeasured notification-receiver process
if one starts. Ordinary main PID11333 remains unchanged; all four OS settings match their
captured original values. No Samsung input, financial Save, schema migration, runtime engine
change, full managed suite or unrelated successful matrix is repeated in this batch.

These independent native receipts are not additional managed unit tests. The original finalizer
exception, general ANR/cold-start/low-memory acceptance, full physical-device qualification,
spoken accessibility, Mac/iOS and release gates require their own evidence.
