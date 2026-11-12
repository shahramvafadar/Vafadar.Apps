# Minimum Android transaction IME viewport - D1127

## Distinct boundary and original input

[D1124](transaction-search-ime-viewport-d1124.md) fixes the transaction results
behind the keyboard. [D1126](samsung-transaction-ime-viewport-d1126.md) qualifies
the Samsung condition, preserving its historical zero-baseline observer failure.
This step covers one distinct minimum API24 Activity condition; it does not
repeat that physical keyboard or the earlier modal PIN adapter cases.

The existing approved official API24/default/x86_64 image and emulator remain in
the retained necessary compatibility SDK. All new build, user-home and disposable
AVD output stays under `artifacts/d1127-minimum-transaction-ime` in this repository,
consistent with [generated-output ownership](generated-output-cleanup-2026-11-07.md).
No SDK download, installation, relocation or global environment change occurs.
Existing frozen AVDs and retained C7 data are not read or reused.

Initial official Windows launcher attempts fail in their nested findstr version
check and create no AVD. Java21.0.8 is independently verified; its check is not
skipped. Direct official Java CLI use initially locates a different SDK without
the admitted image, and an unconfigured help request also refuses. These failures
remain separate from success. The existing official CLI then uses its scoped
tools-directory SDK locator, creating exactly one fresh AVD. Locator behavior is
independently documented in [the Chromium emulator caller](https://chromium.googlesource.com/chromium/src/build/+/1537294faa4757a74545fdf0e86134d93bcfbb29/android/pylib/local/emulator/avd.py).
No directory is created at the nonexistent tools/version suffix. This corrects
CLI preparation, without a policy-refusal retry or SDK replacement.

The actual new ranchu/x64 device has API24,4096byte system pages,320x640/density160,
1536MiB/two cores and boot `bdafee26-3655-4c78-9aa3-8bbe66c40594`. Its independent
package `pro.vafadar.zanance.minimumkeyboardreview` is initially absent and receives
UID10062/PID2879. It uses original App/Data/Core/UI and protected startup without
an authenticator replacement. The original retained D802 loader copies its
digest-bound fictitious input once: one account's20fields, nineteen categories
and one Income1234minorEUR entry's32fields. Input SHA256 is
`126a2a28124f3b21d2ecf9ddb59b7836263a28262dea0476797272357bd043b1`.
This is existing fixture loading, not financial Save/import/setup replay or a new
corpus. Original periodAll navigation must load the actual retained entry ID.

## One keyboard case and independent native oracle

The observer waits for positive native width/height and stable pre-keyboard
layout before one actual SearchBar tap. One Android Back dismisses the keyboard.
The same process supplies complete before/after source and restoration.

| Measurement | Actual native value |
|---|---|
| Root before and during IME |[0,0,320,640]|
| Full and visible SearchBar |[69,97,235,144]|
| Full and visible results during IME |[0,202,320,346]|
| Native results height |144pixels at density1|
| Padding during IME |190pixels|
| Results before and after Back |[0,264,320,536]|
| Native field text size |15pixels|

An independent OS InputMethod window has frame[0,24,320,640], content insets
[0,322,0,0], a live surface and visible state. Its keyboard body starts at
24+322=346, matching the result bottom. This oracle does not merely repeat the
production root-height-minus-compat-inset formula. Full native location/size
equals separately obtained visible bounds; results remain above IME and over
44dp. Focus/model-ready are true. Back restores the original positive native
rectangle and exact padding, hides IME and returns through original Home.

The entire root did not shrink. The proposed already-resized root branch is
therefore not exercised; its conditional check is not separate coverage. No code
correction is justified, and no old keyboard success is replayed for another branch.

The terminal packet and independent geometry verdict pass. Complete actual
schema/application-table typed source, including all32entry fields, is identical
before/after without cookie normalization:
`78C4BAEBDA604782905784842B6783A7E8A11A8B64512E77D7B9722E245099AB`.
There is no restoration exception or pending owner/authentication prompt.

## Package, retirement and limits

One strict private native build passes without warnings/errors. Its used signed
APK has192AOT/204ELF/804static16KiBLOAD, offline/nondebuggable policy, v2/v3,
ZIPCRC and unchanged serviced x64 Mono15. The APK is56676203bytes, SHA256
`fa83d995bac53a7040e1dc8f46304f4e0903bae7f246aa37a39b31bbb144484e`.
One bounded read of the installed public APK matches those exact bytes. Thirteen
actual App compiler documents and four evaluated original XAML items are bound;
this is not whole-PE or private Main equality. No Windows/shipping rebuild, old
successful suite, permanent unit test, runtime/GC change or product edit occurs.

Only the fresh completed UID10062 package is removed. Original five OS values,
locale/absent-key presence,320x640/density160, boot and reverse remain exact;
no system-setting write is needed. No XML, screenshot or recording is created.
Normal emulator exit signals both original process handles and closes5794/5795.
Official scoped AVD retirement removes16files/8947049265logical bytes, leaving
zero AVD files. Existing SDK package.xml hash/mtime remains exact; this does not
audit every SDK byte. Shared ADB5037/PID59860 identity remains unchanged, without
reset/start/connect/reconnect/pair. No physical phone command occurs; Main5118/
e30365bb remains only the dated D1126 observation.

Two standard Cleans pass and retire13709files/823148714logical bytes, leaving
976registered build files/128980126bytes. Two explicitly scoped unused private
Store bundles retire111981652more bytes; the used signed APK stays hash-exact.
Used package/PDB/source, raw packets, CLI failures and cleanup receipts remain in
the owned root. Sparse AVD logical bytes are not a physical-free-space claim.
No frozen scope, old cohort or retained C7 AVD is touched.

This qualifies one minimum-platform loaded transaction keyboard/restoration
condition, not every font/theme/language/rotation, modal or retained-tab lifecycle
branch, whole accessibility/security/performance, original native-fault repair or
release. Launcher timing is not cold Home speed acceptance. Wider platform/provider
gates and the full delivery goal remain open.
