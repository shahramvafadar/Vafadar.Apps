# Original PIN cover above the actual keyboard (D-724)

## Actual uncovered failure

D-721 added automatic PIN-entry focus. The remaining named assertion is reachability of the
original cover's hint, entry, Unlock and recovery controls with the keyboard and real enlarged
OS text. On a fresh API35/x64/userspace16384/kernel4096 emulator, font_scale2.0/360x640/density160,
the original English Unlock occupies408..461 and Forgot PIN481..534 while the actual IME begins
at409. Both actions are behind the keyboard. An actual upward swipe changes no control bounds;
the original stack is not scrollable. Full native bounds alone would falsely pass this screen.

Cold/configuration-time presentation and a fast retained-cover resume do not focus the entry in
this cohort; preserve those observations without calling them an automatic-focus pass. One
explicit native focus tap, without entering or submitting a PIN, shows the actual numeric IME
and exposes the failure. This fixes the demonstrated keyboard viewport, not every automatic
focus/lifecycle path.

## Complete bounded correction

Only original LockPage presentation and its Android window adapter change. Keep all original
controls, accessible descriptions, password entry, translations, handlers, authentication,
growing attempt penalties, Back/confirmation ownership and screen-protection flags. The original
body becomes scrollable in a safe-area wrapper; added recovery/Cancel buttons stay in that body.
No new strings, permission, dependency, database model, migration, runtime or financial policy.

Two actual intermediate failures remain recorded. A scroll/safe-area-only candidate still has
a592-pixel viewport ending616 beneath the IME409. Enabling resize only on the actual modal's
decor changes its native mode to adjust=resize, while the separate Activity stays adjust=pan,
but MAUI's effective modal viewport still omits the IME. Neither candidate is native acceptance.

The final owned modal lease preserves its previous input mode, requests resize and observes its
own root's layout without replacing framework inset listeners. Original AndroidX root IME
insets and actual native scroll bounds supply only remaining occlusion in density-independent
units. Add back the last owned adjustment before recomputing, preventing padding oscillation;
hidden/floating keyboards need no invented height. Remove the observer and restore only owned,
unchanged padding/mode on disappearance. A queued appearance cannot change a replacement page.
There is no fixed keyboard height, Activity/global setting change or authentication shortcut.

[MAUI's safe-area guidance](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/safe-area?view=net-maui-10.0)
describes the wrapping container. The finite native failures above show why that alone is not
sufficient on this actual modal; they do not establish every framework/OEM internal cause.

## Retained source, actual themes and final geometry

One new fictitious app PIN/five-guess threshold is prepared once in UID10207, with no account,
opening, plan, ledger, payment, import or real authentication. Reuse the original D-722 package
for the original failure. Necessary signed no-reset candidate upgrades preserve the same protected
metadata/required marker and full22typed source; never rerun preparation or submit another PIN.

The initial PID4607 ends with Android **LOW_MEMORY/reason3/status0**. PID6848 continues that
source; do not call this a proven configuration restart or uninterrupted process. Later
6848→7297→7616→7924→8835 transitions have actual PACKAGE UPDATED/reason16/status0 records.
No manual process kill, force-stop or reboot is used. The source and all accepted setup survive.
General memory/stability acceptance remains open.

Final acceptance is **18 distinct original PIN-cover contexts**: en/fa/de, Light/Dark,
360/412/680x640, real OSfont2.0 and density160. Initial hint/entry and scrolled original Unlock/
Forgot controls are checked against actual visible IME geometry. At360, the IME starts409 and
final English actions are251..304/324..377; Persian225..291/311..377. At412 the actual IME starts
376, and at680 it starts388. These measured native values are not implementation constants.
Original labels, RTL, screenshots, semantic palette and minimum48dp targets are retained.

Three actual Light360 successes remain accepted on native68dab302. The private helper's first
requested Dark observations still render Light; explicitly disqualify them as Dark, without
discarding their geometry or repeating the accepted Light contexts. Correct only the private
helper to use original ThemeService and verify the actual choice/semantic background. Native
1d4eb135 continues only the15 unfinished real theme/width contexts. The first post-upgrade
status is refused before startup readiness; keep that failure and continue the same8835 process,
without another install or layout command. No accepted context or financial operation is replayed.

Every final context preserves the same original protected attempt metadata and complete22typed
financial source. Native presentation observations are not new unit tests: **new managed count0**.
No old successful PIN/clock/cold/financial test suite is rerun. This is not a TalkBack/speech test.

## Build and package boundaries

Strict Windows compilation has zero warnings/errors. Its effective source remains exactly the
already compiled scroll-body input after removing Android-only refinements; verify that projection
and do not rebuild an unchanged Windows input. Final strict native/shipping Android builds and
offline/nondebug/v2v3/CRC/ZIP16KiB checks pass. A nullable Android-inset compile error and a missing
verification-script invocation remain recorded; fix only those boundaries without rerunning a
successful build for the latter.

Used native1d4eb1354b7ac296532d83a43fad7c6bfe80135dc7a67650efdabf873499ea0e is56,913,826bytes,
192 own AOT/204ELF/804 static16KiB LOADs. Selected complete shipping
0fd9649e26f58b9a0da7bbb9951dc26d4d7508aa5f9c7245f33abb7585786958 is99,426,207bytes,
384 own AOT/408ELF/1608 static LOADs; the private review provider is excluded. The first unused
shipping547d46e5 candidate is not selected. Do not relabel all18 observations as the final native
APK: the same final production UI/adapter has3 retained earlier and15 later private-helper cases.

## Restoration, retirement and honest cleanup

Restore the captured local theme through its original service, then exact OSfont1.0, original
320x640/density160, zone, boot and all seven captured setting values. Final source/PIN metadata
and required cover remain exact in8835. An inherited diagnostic field named
NoSystemSettingOrFinancialWrite incorrectly claims no setting write; preserve its original
receipt but disqualify that field in the companion interpretation. Temporary font/size/theme
changes did occur and were restored; no financial write occurred.

Only the fresh package/active own pending/posted state and official owned AVD are retired;
port5572 closes, ADB5038/other AVDs/phone remain untouched. Official data inventory before deletion
is23files/5,103,338,031 logical bytes. Fifty owned remote XMLs/299,756bytes are hash-matched and
removed with local evidence retained. Five standard Clean commands pass, with6,310 registered
file-deletion events, not a unique-file or measured host free-space count.

Before supplementary cleanup, preserve digest-matched used source outside its target roots and
retain all actually used signed packages. The single request for14 new roots/files is rejected
before process creation with `blocked by policy`:855files/769,111,507logical bytes remain frozen.
No retry, subset, alternate method, move, escalation or build-input reuse. Earlier refused scopes
are untouched. Unused candidate outputs inside that refusal remain, not falsely reclaimed.

## Remaining boundaries and consumed hosted check

D-723's only due exact-head response reports [CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41275525663)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41275525585).
The marker/response are saved and its schedule deleted; no second query or later result assumed.
This finished batch receives its own single later exact-head check.

This is bounded API35 userspace16KiB/kernel4096 PIN-cover geometry, not hardware16KiB, API24
keyboard-inset acceptance, every error/confirmation/device-only state, genuine owner/biometric
authentication, all security/database/key/recovery paths, general stability/speed/accessibility,
physical Main adoption, iOS/provider/Store or release. The three D-607 deadlines, consumed reboot
and unread post-reboot source stay protected; Main adoption waits for actual D-607 completion.
Android precedes iOS under D-714; the overall delivery goal stays active.
