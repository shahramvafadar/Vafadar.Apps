# D1185–D1186 — Retained Android text fonts follow density changes

## Actual remaining failure

[D1184](private-maui-reproducible-producer-d1184.md) retains a116pixel entry
height after a density change; that bound alone does not prove its text size.
D1185 measures native TextSize on two fresh fields using the actual public MAUI
UpdateFont operation:20SP with automatic scaling and20DIP without it. No old
conversion query or product setup flow is reissued.

Fresh API36/x64/4096, UID10216/PID3747, one retained MauiAppCompatActivity:

| Phase | Native dpi | Scaled field px | Unscaled field px | Current expected px |
| --- | ---: | ---: | ---: | ---: |
| Initial |420|52.5|52.5|52.5 /52.5|
| Changed, no font remap |160|52.5|52.5|20 /20|
| Public UpdateFont reapplied |160|20|20|20 /20|
| Restored, no second remap |420|20|20|52.5 /52.5|

Both fictitious drafts remain exact, with one Activity creation and two actual
configuration callbacks. This proves a distinct retained-font failure and the
effect of reapplying the public mapping, not an automatic product repair. OS
font scale is1.0; enlarged text/nonlinear scaling is not qualified here.

The official [Android16 TextView source](https://android.googlesource.com/platform/frameworks/base/+/refs/tags/android-16.0.0_r1/core/java/android/widget/TextView.java)
is retained with its Apache-2.0 notice, digest and immutable tag. Its
configuration callback updates locale, weight and input-related state; the
separate size setter converts the requested unit using current metrics. This
public source is not installed-framework binary identity. MAUI's original
EntryHandler/LabelHandler font mapping calls that public operation; Window's
density notification does not itself remap these fields.

Initial helper compiles refuse ambiguous Font/Binder names. Explicit type
qualification corrects them; successful restore is not repeated. Final strict
SDK compilation has zero warnings/errors and one actual PDB source document
equals the final source. APK14895492bytes, SHA256
`d4773766db1193a10b5bd2a8698ba099fbe15df1acbb2f3dfc9ea1b2b72bc738`,
81AOT/91ELF/354staticLOAD, offline/nondebuggable/v2v3/ZIPCRC/static16KiB pass.
The provider is package/run/PID/emulator/shell-bound, UI-thread-only and admits
each phase once. No application database, authentication or financial operation
exists in this SDK caller.

## Product correction and ownership

MainActivity remembers initial native density and refreshes only when a later
positive density differs. Same-density theme/locale/size callbacks do not trigger
this new work. Existing lifecycle/localization/widget/system-bar behavior stays
in its original order. No font-scale configuration flag, restart, root rebuild
or private-field reset is introduced.

AndroidConfigurationFonts captures the current activity-bound window/content
root. Text-style elements must still have a handler owning that exact virtual
view before the original public Font mapping runs. Measurement is invalidated.
Window/root ownership is checked before each element and after handler/measure
callbacks. A reference-identity visited set prevents duplicate/cyclic traversal;
current children are sampled after each update. No text assignment, focus
request, Save, font-value change or old-pixel-to-unit inference occurs.

The scope is text-style handlers in current window content, not every native
drawable/minimum dimension/toolbar/overlay/formatted span. Public conversion and
private source-package integration retain their own boundary.

## Actual MAUI handlers and retired root

D1186 uses real Controls Entry instances, actual EntryHandler mappings and the
exact source-linked product correction. Fresh API36/x64/4096, UID10216/PID3744,
one Activity creation:

- Both initial native sizes match52.5pixels; the original retained handler path
  leaves both at52.5 after160dpi, where current metrics require20.
- The product correction maps each real handler once to20, preserving drafts.
- Enabling that source operation in the configuration callback makes reset to
  420dpi automatically map each once to52.5, with unchanged drafts.
- An actual mapper callback replaces Window.Page after the first old Entry
  visit. The correction stops with one old mapper visit and the replacement root
  current; the pending old Entry is not processed.

The retired packet's native-size slots are-1 because native getters are skipped
after retirement, not observed negative font sizes. This controlled callback is
not complete original navigation/modal/authentication acceptance. Native APK
17960611bytes, SHA256
`0c2b4df67e5d1eec604819892b6e871cee5a169009ceed77a6f9e3e69a01f634`,
90AOT/100ELF/390staticLOAD, passes the same signing/privacy/ZIP checks.

## Complete product and original screen

The first Windows restore accidentally selects the task-introduced servicing
variant, whose candidate.8 is absent from the prepared feed; it refuses before
compilation. Removing only that introduced environment choice restores the
canonical local default candidate.7. No native package/policy changes and no
servicing qualification is claimed. Strict default Windows then builds with
zero warnings/errors.

Strict complete offline Android also passes without warnings/errors, using the
already validated D1184 private Core without rebuilding it. APK99663775bytes,
SHA256`5763424e651a55bbf5e1291d64a4508e1a4e49494ae85142a6cbead8168c760b`,
384AOT/408ELF/1608staticLOAD, v2/v3/ZIPCRC/static16KiB pass. Both original
serviced10.0.15 Mono hashes remain exact. Four actual compiler source documents
bind the native caller/correction and original MainActivity/correction. Native
and product own AOT outputs equal their APK entries; whole PE identity or
general stability acceptance is not inferred.

After retiring the native caller, the same owned emulator installs the complete
product once. PID5226/UID10217 reaches original Welcome, then two normal Next
actions reach step3. A new draft, `D1186 original draft`, is entered without
Start/Save. A native-phase marker-name collision refuses the first proposed
product density action before any wm command. Its old marker remains intact;
only the unissued product action continues with a product-specific marker.

| Original product observation |420dpi initial|160dpi|420dpi restored|
| --- | --- | --- | --- |
| Step indicator native height |42px|17px|42px|
| Name draft |exact|exact|exact|
| Name field focused |true|true|true|
| Original setup step |3|3|3|

All four selected name/header/Back/Start bounds stay inside1080x1920 at160dpi.
After reset all four original bounds and focus values are exact in the same
PID. This checks changed code on the original screen, not an unchanged successful
matrix replay. It does not measure original TextSize/glyphs, all native
attributes or Activity-instance identity. The name's native minimum-height
bound remains116pixels at160dpi; broader dimensions remain open. No account,
financial row, import/undo/authentication/camera or financial seed is committed.

## Retirement and remaining delivery

D1185 retires its fresh UID/AVD with original public OS/locale/key-presence/
geometry/density/boot/reverse exact. Official AVD deletion removes25files/
10579687016logicalbytes, leaving six user-home files/7732bytes. One standard
Clean removes2825files/110449504bytes;336files/22629659bytes remain.

D1186 retires both fresh UIDs. Seven remote XML/197273bytes are hash-verified
and removed; original public state/shared ADB process and creation remain exact.
Official AVD deletion removes25files/10836194873logicalbytes, leaving six user-
home files/7732bytes. Three standard Cleans remove17986files/1288806559bytes;
3002files/487168668bytes remain. Used signed packages/source/AOT witnesses,
phase packets, refusals and logs remain in both named owned artifact roots.
No supplementary/frozen/C7/Q02/phone/Main/consumed-one-shot/runtime/GC action
occurs. The first combined documentation edit refuses before creating its
report because its update context was absent; the corrected edit changes only
this report and its allowed quality backlink.

No successful managed suite or financial operation is replayed. These finite
native observations are not new unit-test counts. Default private-Core pipeline
integration/Linux execution, broader native dimensions/typography and original
fault/performance/security/accessibility/platform/provider/release gates remain
open. The full delivery goal remains active.

Related: [original conversion failure](public-density-conversion-transition.md),
[private source correction](private-maui-density-configuration-d1182.md),
[subsequent default integration and Linux producer](default-android-core-linux-producer-d1187.md).
