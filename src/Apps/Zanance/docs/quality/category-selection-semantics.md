# Original entry-category selection semantics (D-578)

## Observed defect and correction

On the installed production Mono package b57725fb, the original expense form's
Food/Housing/Food command sequence changed the visual category but left its native
button description at `Food`. It never exposed the existing translated selected
caption. The amount stayed blank and the draft was cancelled and discarded; Save
was never pressed. This is separate from the completed D-576 financial cohort.

`CategoryChoice.AccessibilityName` now uses the existing `Common_ChipSelected`
format only while selected. The original `IsSelected` property also notifies that
dependent name, so the original overlay button updates immediately after a tap.
Only its semantic description binding changes. Visible category labels, icons,
colors, hit targets, exclusive-selection commands and financial validation remain
the original implementation. All six existing translations contain one placeholder;
no resources, permissions, provider, schema, writer or monetary algorithm change.

## Package and verification boundary

The relevant strict Release Windows and complete offline Android builds both have
zero warnings and errors. Actual tracked production inputs were fingerprinted
before compilation and checked unchanged afterward. The signed Android candidate is
`510a3197cb6d77bf69df03d10f3d4079286726a82fecce557f8a4bebeef3b879`,
97,058,719 bytes: 384 own AOT images, 408 ELF files and 1,608 statically aligned
16 KiB load segments across ARM64/x64. ZIP integrity/alignment, v2/v3 signatures,
offline/non-debug/non-profile manifest and all 1,422 original resource bytes pass.

Actual Core, Data, compression managed PE and the four original native engines are
byte-equal to the installed b577 baseline. The original product remains Mono/SGen;
the isolated D-576 experimental runtime is absent. Static alignment is not new
hardware-page-size or platform acceptance. No unrelated successful suite is rerun,
and native observations do not increase the managed test count.

The candidate was installed once with `-r`, without reset. Main UID10380,
CE155299/DE154603, permission flags, original system settings and b1d5 caller were
preserved; normal launcher startup produced PID22604. No second installation was
needed. Original application preferences were English/Light; the only temporary
system change for this turn is the separately authorized screen-off timeout.

## Native changed-behavior cohort

The bounded cohort uses original empty expense drafts in en/fa/de and Light/Dark.
Each context checks Food selected, Housing selected exclusively, Food selected
again, and Food deselected. It checks both button descriptions and unchanged visible
category labels, then uses original Cancel (and Discard if required). No amount,
title, account, financial Save, Import, Undo, seed or key command is submitted.

All six contexts and 24 native states pass on the same installed Main PID22604.
Native selected names include `Food, selected`, `خوراک، انتخاب‌شده` and
`Lebensmittel, ausgewählt`; deselection immediately restores the plain name.
English/Light is restored, with Home979 and all 141 final public native attributes
equal to the pre-cohort observation. All five recorded system settings, including
the original timeout300000, and Main/caller package metadata remain exact. Existing
three-profile evidence is retained without unnecessary re-enumeration or private
database reads. The unchanged managed suites and prior financial commands are not
repeated to check this native binding; the managed test count does not increase.

## Preserved refusals and limits

Two initial native hierarchy reads produced no remote file, including a read after
the original Settings tap. The accepted navigation was not replayed: only the same
page was observed again, and the original refusal/request records remain. Later
capture refusals, if any, remain distinct from accepted button commands.

After restoring language/theme, one 51-node covered loading frame was refused as a
final Home observation. The same process completed without another navigation,
Retry, financial write or restart, returning the exact 141-node public Home.
The final process-absence guard also refused: the original independent caller had
been started by Android as `top-activity` during the installation interval, PID22547.
No caller launch had been requested. Its actual cached=true/raw and set adj900
state was recorded before one package-scoped `am kill`; exit1 with empty output/error
proved absence. No force-stop, reinstall, caller data/queue change or Main restart
was used. All original refusals remain separate from the completed restoration.

Two standard scoped cleans removed all 2,361 registered output files, with
752,959,634 preflight logical bytes. A subsequent request to remove the two new
residual build roots was rejected before execution by automatic policy review
(`blocked by policy`). Those roots remain; no alternate deletion was attempted.
The used signed package and local evidence remain. These are logical file counts,
not a measured host free-space gain, and earlier refused cleanup scopes are untouched.
Only the 96 new owned remote XML files with matching retained local/remote hashes
were removed (5,072,533 logical bytes); local evidence remains. No timed-emulator,
other-project, shared dependency or older refused cleanup path was changed.

Native descriptions alone do not establish intelligible TalkBack speech, complete
accessibility, general stability, performance, private Main database/key equality,
other devices, iOS, providers, Store or release acceptance. D-567's timed idle/reboot
cohort and the overall delivery goal remain active and separate.
