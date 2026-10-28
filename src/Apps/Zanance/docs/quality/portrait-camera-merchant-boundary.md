# Portrait camera framing and a missing merchant (D-615)

## New missing assertion, original source retained

D-609's original landscape frame is incomplete. This separate bounded cohort tests
one genuinely new720x960 portrait receipt, not a padded/cropped replacement for that
historical image. All nine lines and four corner markers fit the original source.
Expected merchant NORTH HARBOR MARKET and total23.45EUR are fictitious. The new PNG
SHA256 is616f0552e68d6e0bfb3b67df9d5988dfa04454a976fe388ab0b092749907a332.
The direct original native reader returns all nine exact lines, that merchant,
Found and2345minorEUR. No financial or profile command is called.

A fresh Zanance_D615_Portrait API36/x64/page4096 emulator owns UID10216. One original
source choice, one actual Camera2 Shutter and one Done return request23107/Ok to
PID4748. No old Cancel, photo, Save, Import, Undo, seed or accepted financial command
is repeated. The original bounded camera reader and source/image preparation remain
unchanged. Metadata-free stored JPEG960x1280/46126bytes has SHA256
e9adcc399e9d46216b10987b1175a5bd6e9b2bc342c8a6423a4ab10525453f64;
recognition JPEG960x1280/84310bytes has SHA256
74c4fac7532b4b67a511ff0ba74c705579606755ba478127e33cfc285b20a19f.
Raw camera cache is empty; the raw camera output is not exported.

## Actual framing failure, not invented OCR recovery

Both actual camera reader paths return exactly these same five lines:

```
28 OCT 2026
BREAD EUR 12.00
FRUIT EUR 11.45
TOTAL EUR 23.45
CARD EUR 23.45
```

The stored pixels visibly lack both header lines and both footer lines; EXIF is empty.
Measure the original two files without saving altered images. Inclusive ink bounds:

| Row | Original PNG | Stored JPEG |
|---|---|---|
| Date |49,231,216,250|116,49,514,95|
| Bread |50,326,320,348|119,275,760,328|
| Fruit |51,391,306,413|120,429,727,482|
| Total |49,518,363,544|116,731,863,793|
| Card |50,591,287,612|117,904,682,954|

The five line starts fit y_out=2.3750948*y_in-499.5140 within0.234pixel.
This implies a visible original vertical range about210.31-749.24; the actual
merchant ink98-124 and footer ink765-858 lie outside it. This inferred range is not
an exported raw sensor crop rectangle. Source CameraCapture only supplies its
original output URI and reads the original result; DocumentImages prepares the
whole decoded bitmap through scaling/orientation, without a crop rectangle.
Missing header pixels cannot be recovered by changing OCR or inventing a merchant.
The exact exclusive emulator37.1.11/imagefile/Camera2 internal cause is unproven.
Do not call portrait imagefile input a supported complete-framing acceptance, retry
its shutter, pad either historical source or silently relabel this failure a pass.
A later independently planned supported virtual-scene/full-frame source is a separate
missing acquisition assertion; whole-camera/physical/corpus quality stays open.

## Reproduced product defect and finite correction

The old original parser proposes the surviving date28 OCT2026 as Merchant while
preserving Found/23.45EUR. This is a real suggestion defect independent of the crop.
Seven new regressions fail on original product source: the exact native five-line
text and six supported-language date-before-header boundaries. Three new controls
preserve actual names before a body, including Studio54 and28 OCT2026 MARKET.

Keep the original four-line search, merchant exclusions, amount/currency evidence,
financial-date parser, source text and existing field-application policy. A complete
valid numeric or exact named-month calendar-date row ends the search when no header
has been found; item rows below it are not guessed as a merchant. Recognize full
named-month rows only with explicit day/month/four-digit-year formats and the
existing six source cultures. No loose current-year inference or new financial-date
recognition is introduced. The missing merchant remains null for explicit user review;
this never edits or saves an existing entry. A legitimate name already found before
that date and a name merely containing date-like text remain intact.

Ten new cases and45 existing directly related receipt cases pass in selected runs:
27 cases for the new boundary plus original parser, then28 selected French/Italian/
Spanish merchant methods. No whole suite or unrelated success is repeated. The new
managed product test count is10; native packets/XML are observations, not unit tests.

## Corrected native reread and preserved diagnostic navigation failure

After one complete signed no-reset upgrade of the same fresh UID, PID6072 reads the
exact previously accepted compact JPEG once through the original native reader and
corrected parser. Actual OCR text remains the same five lines; Merchant is null,
AmountStatus Found and Amount23.45. A create-once same-run marker requires the original
completed cohort and prohibits another camera acquisition. Zero new camera/financial
commands occur. The original photo is not retaken to improve an observation.

The subsequent diagnostic attempt to open the original editor fails with
NullReferenceException while this new installation still shows onboarding Welcome.
MauiAppInteraction navigation requires Shell.Current, which that onboarding window
does not provide. Preserve that actual failure; do not claim editor-field, onboarding
completion or a newly fixed product navigation defect. The successful native parser
result precedes it and is retained separately. No editor Save, onboarding/financial
fixture construction or second OCR run is used to rescue the diagnostic navigation.

## Build and original-input limits

Strict Windows finishes with zero warnings/errors. The complete offline signed
product APK40ce4f8e35056705a9d75af7d6748acb1139c76159304b0a7adc24506a739f2c
has384ownAOT/408ELF/1608 statically16KiB-aligned LOADs, valid ZIP and owner v2/v3.
Original camera diagnostic APKdd0b4cea and corrected0b87a6a8 each pass their own
192AOT/204ELF/804LOAD, signature/CRC/offline/nondebuggable witnesses. No new SDK,
permission, schema, financial model, runtime or GC policy is introduced. Exact
selected Core/Data PE hashes differ between the linked independent and complete APKs;
whole-binary identity, performance promotion and physical product adoption are not
claimed. Source-matched focused tests and the actual independent native parser result
are the finite evidence. The first copied shipping validator expects the old default
obj directory and refuses; correct only its isolated output path without rebuilding
or repeating accepted native operations.

The first owned-emulator readiness query sees offline during initial boot; preserve
that refusal and continue the same host after actual readiness. No restart follows it.
Temporary diagnostic source inputs are retained as local used-source witnesses, then
all tracked diagnostic changes return exactly to HEAD and the new temporary partial
source is removed. Only the product parser/regressions and documentation are published.

## Scoped retirement and open work

Only fresh UID10216 and its actual package are uninstalled. Four owned hash-matched
XMLs/43505bytes are removed; five original settings, size/density and fresh boot identity
remain exact before retirement. The24-file fresh AVD and its registration are deleted
through the official scoped manager after actual serial/port absence. Four standard
Clean commands remove4053registered files/1176166276logical bytes. Six unused unsigned/
loose publish files are removed; both used signed APKs and the prepared complete
product remain. The temporary source is removed after its used witness is retained.
Further removal of four D615 artifact roots and the residual SDK user home is rejected
before process creation with `blocked by policy`:3687files/575554430bytes remain.
No subset, alternate mechanism, repeated Clean or escalation follows that refusal.
Logical file totals are not exclusive measured host free-space gains.

The physical phone, D-607 package/run/three original deadlines and retained AVD are not
commanded by this cohort. Main remains the D-606 physical baseline after its separately
authorized controlled D-607 reboot; no product adoption occurs during pending reminders.
D-607 source comparison after reboot and real delivery remain open. Broader receipt/
camera/merchant corpus, public providers/Store, stability, two-second performance,
accessibility, iOS and release are separate gates; TalkBack remains last. The overall
delivery goal remains active. Evidence pointer:%TEMP%\Zanance-D615-current.txt.

## Subsequent independent complete-source evidence

D-616 preserves this exact unedited input in the supported virtual scene. One new
camera result returns all9 primary lines and the correct merchant/total, with one
additional imperfect rotated-poster line retained. This does not relabel either
imagefile failure or the diagnostic onboarding navigation failure.
[Original D-616 evidence](complete-virtual-scene-camera-source.md).

## Later complete named-date extension

D-621 later adds a full named-date fallback while keeping numeric precedence. The
historical null-Date assertion here belonged to D-615's boundary-only scope; retain
its original failure/passing evidence and original package. New dated input and
original editor fields have separate evidence, without rerunning this camera or
calling its old date field a new extraction result.
[Later named-date evidence](complete-named-receipt-date-suggestions.md).
