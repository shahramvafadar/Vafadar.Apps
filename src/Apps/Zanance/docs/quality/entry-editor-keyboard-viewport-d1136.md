# Original entry modal keyboard viewport - D1136

The original Samsung entry modal keeps its Save footer behind the actual IME.
This is an observed layout defect from the retained reference cohort, not a
reason to replay [D1135](physical-reference-search-save-continuation-d1135.md)'s
consumed tap or any earlier successful financial operation.

The original baseline9074/UID10455 has modal IME visible, root[0,0,1080,2340],
keyboard top1641 and Save[45,2025,1035,2160]. Amount owns focus. The private
observer then incorrectly assumes that blank amount must disable Save; the
original model instead validates on Save. That InvalidDataException is retained
as an observer failure, not a product disabled-state defect. Only its unfinished
cleanup continues: one original Cancel returns the same PID to selected Home with
no editor. No Save or search input occurs in this baseline.

EntryEditorKeyboardViewport now observes only the attached original modal root,
active page and actual footer. It compensates the remaining IME overlap in the
ready-form grid, adding back only its own previous adjustment to avoid alternating
padding. It changes no Activity/window soft-input mode, global keyboard policy,
text, translation, money, writer, consent or model. Departure revokes ownership
before fallible callbacks, releases the listener and restores only padding still
owned by this lease. Loaded/appearing reacquires; unloaded/departure releases.

The current candidate13351/UID10455 on the same boot/API36/ARM64/kernel4096 has
keyboard top1641 and Save[45,1461,1035,1596]: its whole button is above the IME.
Amount still owns focus. Actual hide restores original zero grid padding; original
navigation returns Home and leaves no lease on the departed page. All three new
show/hide/departure conditions pass; there is no Save-command invocation, search
input, fixture preparation, account setup or financial write.

Original protected recovery, before-layout and after-layout full typed source
all have the exact4,173,140-byte SHA256
`911BB3D60329EB2E647103CD85405AA76A1039108D35AC922E369A91D8F4121E`
and schema/row digest
`A13F032E62B1985FEAC11B2F440358F06B2DD16F0936563664275554DFC254B4`.
This includes every captured application table, sqlite_sequence and all32fields
of all10,000original transactions. Native whole-file SHA/length match the existing
complete local byte stream; duplicate transfer and normalization are unnecessary.
This does not inspect Main's private source or establish whole protected-database
or key equality for the owner's installation.

The first unused baseline restore has a global-TargetFramework NETSDK1005 and is
corrected before deployment. Original baseline C#/XAML bytes are bound to recorded
compiler hashes; Git LF versus actual CRLF is not silently normalized. A later
candidate observer starts during receiver-only application startup while a
transport-failed launch has no actual window; its immutable timeout and later
ready Home are preserved, with the exact historical timeout subcondition unknown.
No modal condition was accepted there. The observer now waits for a real owned
ready window before consuming a UI-case marker and records its exact phase. The
product candidate does not change between these observer versions. No successful
native input/context is repeated to manufacture acceptance.

Strict Windows and complete offline Android builds pass. The complete product has
384AOT/408ELF/1608static16KiB LOAD segments, ZIP/CRC, v2/v3 signatures and stock
Mono15 ARM64/x64. Three actual Windows/shipping compiler documents bind the changed
page and Android viewport;23private candidate documents bind the actual runtime
case. These are source checksums, not whole Core/Data PE identity. The used
private packages have192AOT/204ELF/804LOAD. No old managed suite or financial test
runs again; there are no new unit tests for this layout-only correction.

Complete product SHA256:
`7b28fc99c749a6320bf1c1b466bf9b12e7464b52d304015f6ab9290fc012878f`
(99,659,679bytes). One exact no-reset Main update succeeds from11835/26ade48f
to15295/7b28fc99 after actual selected ready Home with no editor. All141nested
native attribute maps and raw XML equal before/entry/after. UID10380/CE155299/
DE154603, grants, other captured packages, OS/key-presence/locale/boot/geometry/
reverse remain exact; only the declared Main package hash changes. The public
owner signer92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b
matches. The normal HOT foreground return and WARM704ms post-update Activity
are not ready-Home performance measurements. No private Main source/key,
authentication, photo, financial or prior successful case is invoked.

Three owned Main XML/155,796bytes are hash-matched and removed remotely; local
raw is retained. Three successful standard Cleans retire28,573files/
1,970,787,987logical bytes across the old private native, Windows and complete
Android outputs. The initial Windows long-path inventory error occurs before
cleanup and is corrected for the same absolute owned files. There is no
supplementary/frozen cleanup. Remaining registered-output residuals and the
current native cache are explicit, not erased by an unrecorded delete.

The retained UID10455/last observed13351 and current private package d93fc7eb
remain necessary for the unfinished reference Save measurement. Its complete
source is unchanged and must not be prepared/copied again. All D1135 intent/tap
markers remain consumed; no old financial action may be replayed. Main15295 is
the current dated observation, not a future-liveness claim. Original native-fault
remedy, the two-second whole-performance objective, whole accessibility/security,
iOS/provider/Store/release gates and the full delivery goal remain open.

[D1137](current-reference-save-d1137.md) subsequently completes the remaining
distinct reference Save on this corrected product source without financial
replay, proves the complete old-row/derived-cache delta and retires UID10455.
Main returns to the same15295/7b28fc99 ready Home with exact raw XML; the private
native cache is standard-cleaned after use. D1136's sole CI and CodeQL results
are completed-success; there is no second CI request.
