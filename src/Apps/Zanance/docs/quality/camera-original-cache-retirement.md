# Abandoned camera-original cache retirement (D-596)

## Actual gap and correction

The original Android camera adapter writes a GUID-N JPEG into its private `camera/`
cache directory and deletes it in `finally`. Process termination cannot execute that
block. The original startup/data-deletion cleanup removed viewer attachments and
three temporary export patterns, but never visited `camera/`. An interrupted camera
original, potentially containing source metadata, could therefore survive startup.

Move that original cleanup unchanged into the actual shared `AttachmentCache`
source, with the platform cache root passed by `AttachmentFiles.ClearCache`.
Two selected regressions (an abandoned nonempty original and an empty capture file)
fail before correction; the original viewer/export and unrelated-file controls pass.
The corrected actual source passes all four distinct cases, zero skips/failures,
on the qualified host runtime14. No old receipt/financial suite is rerun.

The new cleanup visits only top-level GUID-N `.jpg` files under the original camera
cache directory. It refuses linked directories/files and preserves unrelated names,
other extensions, nested files and root files. The camera adapter uses the same
shared directory constant. Existing best-effort handling of IO/access failures and
viewer/export retirement remains; an unavailable temporary file is retryable on the
next startup. No recursive camera-directory deletion, personal-file path, permission,
SDK, schema, backup field, OCR rule or financial algorithm is added.

## Real original Android startup

A fresh independent `cameracachereview` package is absent before install and receives
UID10218/CE369056/DE40966. Its shell-protected, independent-only Activity creates one
original-format GUID JPEG containing eight fictitious bytes and two preservation
controls. These bytes are not a real photo/OCR sample. The Activity does not call
cleanup during verification, seed a profile, read a receipt or invoke a financial
command; the original app sources retain normal covered startup.

Stage process3930 records the original file. It is genuinely cached/adj900, then a
package-scoped `am kill` proves its death. Start the original normal launcher once,
without a selector/run: Android reports COLD and process4004. The original App
constructor performs its actual cleanup. The one terminal observation records the
raw file absent, unrelated camera text unchanged and outside root JPEG unchanged.
This proves bounded original startup cleanup, not camera sensor/URI quality,
natural low-memory behavior, a financial save or whole-app crash reliability.

Retire only the fresh package once and prove absence including uninstalled packages.
The first retirement parser requires obsolete `userId` instead of actual Android36
`appId` and refuses before uninstall; preserve that refusal and correct only the
metadata interpretation. No install, stage, kill, launcher or successful observation
is replayed. A first host XML serialization uses the wrong Windows text encoding;
retain its partial output and reconstruct the JSON from the original saved XML,
without another native capture. An initial package-only Main launcher request exits1;
retain this incomplete host observation and use the resolved original component.

## Default builds and no-reset adoption

Strict Windows and independent Android builds have zero warnings/errors. Preserve
Windows restore's initial unrelated iOS-target NU1201 refusal. Restrict the actual
Windows target; an unnecessary globally forced RID then mislocates a referenced PRI.
Remove that global RID, retaining the project's original RID behavior: the final
strict Windows build succeeds. These failed command configurations do not establish
a product/SQLite correction or authorize changing the original dependencies.

Complete two-ABI Offline Release shipping APK:97058719 bytes,
SHA256 `885f6df5c4f981afc83fe6ad06b9c86ec922471e5397f92922dd55d6b7ca5a40`,
384 own AOT/408 ELF/1608 statically16KiB-aligned LOAD segments, valid CRC/ZIP16KiB/
v2/v3 and the original owner certificate. It is non-debuggable and declares no
INTERNET permission. The independent build-admission target separately refuses shipping and retained
caller identities and accepts only the fresh identity (three direct target checks).
This target-only guard does not change the already-qualified native C# inputs; no
successful native build is replayed for that late admission restriction.

The independent signed package is59232162 bytes, SHA256
`7781748a02bc218c61c3456f5cddc6d6aa9e684586251a4fd828afc7f14167fe`,
192 own AOT/204 ELF/804 static segments. Static alignment is not hardware16KiB acceptance.

One full signed no-reset emulator Main upgrade preserves UID/CEDE/permission flags,
all120 original public attributes, zero recorded balance, five settings, display,
AVD configuration and original stopped caller. New Main4275 replaces2937. Stop only
the owned AVD after cleanup; actual serial5570 absence is verified. Shared ADB5038
and the phone are not altered by this emulator cohort.

Then one complete no-reset Samsung Main upgrade adopts the same shipping APK;
Main24382 replaces22461. Home979, all141 captions/geometry/enabled/focus/selection/
public values, UID10380/CE155299/DE154603, permission flags, all14 settings including
original key absence, boot/display and original caller metadata/process absence
are preserved. The caller package's `stopped=false` flag was already false before
this upgrade; process absence must not be mislabeled as package force-stop.
The first full attribute comparison fails on one ScrollView drawing-order2-to1
change at node11. Preserve the exact delta and failed strict result; do not claim
all141 attribute dictionaries byte-equal, rewrite the original baseline or replay
navigation to remove the difference. The normal launcher result is WARM, not an
accepted Main cold-start test. No owner authentication, setting change, financial
write, private Main database/key read or real camera invocation occurs.

## Preservation, cleanup and unfinished gates

Four new remote XMLs are individually hash-matched and removed:two emulator copies
88174 logical bytes and two phone copies103852 bytes. Preserve local originals.
Five standard Clean configurations remove4906 registered new output files totaling
1523573710 logical bytes, including the failed RID configuration's807 outputs.
Four exact unused unsigned/designer publish copies remove157450351 logical bytes.
Used signed APKs and original rollback/evidence remain hash-exact. These are logical
counts, not measured host free space.

Further retirement of the eight new D-596 bin/obj roots is rejected before process
creation with only `blocked by policy`:4030files/688445756 logical bytes remain.
No removal inventory/result from that command is created, deletion is not executed
and no alternate language, target, elevation, partial removal or retry follows.
The separate read-only refusal inventory preserves the remaining scope.

The camera-acquisition/real-corpus gate stays partial. This closes only abandoned
original cleanup. General security/biometric/OEM, physical16KiB, performance,
stability, external OAuth/providers/Store/publication and iOS gates remain open.
Further TalkBack remains last and Persian speech parked under D-594. No successful
old test, native receipt Save, seed, import, Undo or financial operation is repeated.

Evidence pointer:`%TEMP%\Zanance-D596-current.txt`. Preserve red/green XMLs, actual
stage/death/launch/verification, failed command/parser/strict public-attribute
comparisons, before/after package/setting identities, signed package proofs and
cleanup/refusal inventories. Publish the complete batch and check exact-head CI
once after16minutes, leaving the overall delivery goal active.


## One-use hosted follow-up

The single delayed exact-head response for e23cbb1b5eff3e78f82eeb344ceada359257c489
reports [CI40560476436](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40560476436)
and [CodeQL40560476388](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40560476388)
completed successfully. The marker was created before the only request and that
schedule is deleted; no second query is permitted. This does not extend the bounded
camera-cache result to physical camera quality, private Main data, whole-device
security/stability/accessibility, performance or publication.
