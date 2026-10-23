# Optional Android receipt-reading help (D-504)

## Product behaviour

The original Android receipt camera/file chooser now offers optional reading help in all six
existing languages. It explains Latin-script image/scanned-PDF reading, unsupported Persian image
text, the separate selectable-Persian-text PDF path, and review of the amount/currency before Save.
No new recognition model, SDK, download, data field, permission, account or financial restriction
is introduced. This is a capability disclosure, not acceptance of Persian image OCR or every
Latin receipt. The original reading algorithms and explicit financial confirmation remain.

An ordinary camera/file choice adds no confirmation. Help opens an information dialog and returns
to the same sources. Cancel opens neither source. The native adapter retains actual camera/file
and permission ownership; the source chooser only uses explicit interaction ports. Platforms
without the Android Latin model do not show this Android-specific explanation. The existing
Windows file-only route still opens its picker directly. Stored attachment rereading bypasses
source selection as before.

## Focused application-source evidence

Fifteen new cases compile the actual ReceiptSourceChooser with actual translated resources and
explicit native-effect ports: ordinary camera/file choices, help followed by either source,
help/Cancel, camera absence, unchanged non-Android paths, waiting for acknowledgement and all six
resource languages. These cases use no database or process-wide culture/digit mutation. All15
pass; no unchanged financial/receipt/corpus suite is repeated. Focused test output is cleaned
afterward, also without warnings/errors. This is15 new managed cases, not a full-suite count.

Strict Windows and complete signed Offline shipping builds pass with zero warnings/errors.
Windows visible behaviour is unchanged; unrelated Windows snapshots are not repeated. The new
Android UI is checked using original production source and native interaction in a separate
disposable package, without replacing Main while its original timed cohort is pending.

## Actual native dialogs and bounded continuation

Samsung SM-A176B/API36/ARM64/4KiB runs the original source chooser and MauiAppInteraction dialogs
in `pro.vafadar.zanance.receiptsourcereview`. The original Home-review package/data stays separate.
Run `daa2eeac7aa644c5a3c68bb11dd6c01c`, final comparison
`06c287b279ea4d9b8e3d783169ab9b81`, PID9484 completes24 contexts: en/fa/de, Light/Dark,
and measured dialog widths360/384/412/680dp. Native rounding gives359.82/412.09/679.82dp for the
corresponding requested widths. There are72 actual native help/OK/Cancel activations across the
whole retained cohort. All original source/help text is fully visible and unellipsized; actual
native Light/Dark text colours differ. No camera/file opens and no financial Save occurs.

One first context completes before an actual native-focus guard refuses continuation. Preserve
that result/failure and add a finite wait for actual owned Activity focus. A first resume attempts
the wrong failure filename and is refused before UI work; correct it to the original
`failure-*.json` protocol. The source chooser, native adapter and all translated resource hashes
remain unchanged. PID8084 then retains the first context and completes only17 remaining scenes.
This does not establish the internal cause of the initial focus transition or general stability.

The current unchanged portrait viewport is384dp, so the initial requested412dp dialogs are
correctly measured as384dp. Do not relabel them as412dp. Preserve all18 successful observations
and continue only six missing exact412dp dialogs in this disposable caller's own landscape window.
No OS display/font/rotation setting changes. Final packets retain all18 originals and add six
new cases; no accepted source/UI/financial action is replayed to repair the observer or report.
The matrix covers owned dialog widths, not OS200% scaling, spoken TalkBack, camera acquisition,
recognition quality, a Main integration run or iOS acceptance.

## Restoration and cleanup

After terminal evidence, the new caller is removed. Original Main25043/APK/OS remain, but an
initial restoration capture has102 nodes in a2340x1080 landscape window instead of the baseline
141 nodes in1080x2340 portrait. Re-observation of the same process confirms the presentation
difference. Android can retain the last app-requested orientation after Unspecified is restored.

A fixed-package portrait-only entry point returns only the disposable caller's window to portrait,
then resumes original Main. It does not run a qualification phase, seed/replay a source or write
system settings. An early guarded edit did not apply; its packaging output was never installed.
The corrected portrait-only caller is built/verified and used solely for restoration. Complete
original Main portrait tuples now match before and after removing that temporary caller again.
All141 public tuples, Home979, PID25043, original APK, font/display/accessibility and rotation
settings match. The old Home-review APK and stopped state match. No private Main database/key
comparison or whole-device acceptance is claimed. Seven exact owned remote XMLs are hash-matched
and removed; local originals and both failed observer reports remain.

Original D-494 still has PID24914 and four unchanged alarm/intent handles, with no provider read,
window, rearm or subject replacement. D-496's inactive future-day source stays intact. Main remains
D-495 until D-494 terminal consumption/cleanup; then perform one no-reset adoption of the latest
verified complete candidate, including D-497/D-499/D-500/D-504, before dependent future actions.

## Complete signed candidate and remaining gates

Shipping APK: `E:\Zanance.ReviewBuilds\D504-shipping-package\pro.vafadar.zanance-Signed.apk`,
96,989,087bytes, SHA256 `9e3e887dbbe111ff33cdbfeff95892e97136e07133d92f92b6bf1dfd1d1068c4`.
It contains384AOT modules and ten unchanged native engines; v2/v3 signatures, non-debuggable/
no-INTERNET declarations, ZIP16KiB alignment and all408 ELF load-segment alignments pass.
This does not establish runtime16KiB/minimum-API acceptance or store signing/publication.

The exact-width UI caller is192AOT/five unchanged engines, valid v2/v3, Offline/non-debuggable:
SHA256 `f228b0f6ac21abdac41c8c5cbfbb7c1ccfea1ff5446d7bd3b5a7b430ef47b0cf`.
The separate portrait-only restoration caller is likewise verified:
SHA256 `b7c2a95cd9d31bb536de1e1002f298e0de0c0f357187356975d32340336e838e`.
These identities are temporary; neither remains installed. Product builds/tests are not repeated
for observer-only corrections. Intermediate unsigned/unneeded observer packages are removed.

D-503 CI40011534660 and CodeQL40011534736 succeed in one delayed response; its consumed monitor
is deleted. D-502 service40009956209/CodeQL40009956274 succeed and CI40009956242 is cancelled after
the newer push; no second query or speculative repair. New-head CI follows this completed change.
Future-day delivery/comparison, Persian image OCR, broader quality/security/stability/accessibility,
Mac/iOS, real providers/store accounts and publication remain explicit unfinished gates.
