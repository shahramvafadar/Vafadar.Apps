# Interpreted ledger CSV preview (D-331)

## Requirement and implementation

IO-09 requires actual interpreted samples before import. D-330 verified the original phone roundtrip,
but its original preview exposed counts only. Own-format and generic imports now expose ten read-only
cards per page, retaining every source row, including invalid rows beyond the eight-error summary.
Cards show the source line, interpreted date and kind, account identities, title/category, complete
source amount, separate destination/original-currency amounts where present, and the current inclusion
status. The original MoneyText packet remains intact in the existing scalable AmountReadout surface.
Paging reads the retained preview, without reopening the file or writing money. Bank and holdings
previews keep their distinct existing interpretation and consent paths.

A related observed source defect allowed generic mappings to change while their old interpretation
remained importable. Account, column, date/calendar, decimal and sign changes now invalidate that
review. The old cards remain visible as the retained interpretation; an inline reason blocks import
until explicit refresh. Duplicate skip choices update the retained page's inclusion statuses.
Accepted import/Undo retire the cards together with the original preview. Picker cancellation retains
the current page and source. Busy commands refuse paging. No schema, financial parser, import writer,
stable identity, overlap policy, privacy permission or native cipher changes.

## Focused managed evidence

`artifacts/import-preview-d331/tests-outcomes-final.log`:51 actual application-source cases pass,
including19 new cases for all six languages, bounded source/destination/original money,23-row traversal,
invalid rows beyond the summary, known IDs, duplicate inclusion, changed interpretation and cancelled
picker/busy/accepted retirement. The first compile exposed missing namespace imports and constructor
assignment order, corrected before the final run. An initial Persian generic test incorrectly assumed
Gregorian input; its fixture now selects that calendar explicitly. This is not a financial-parser change.

`tests-bank.log`:29 related bank-flow cases pass. `tests-resources-native-runner.log`:23 resource
completeness/usage cases pass. Earlier resource selection attempts ran zero tests and are not acceptance;
the original executable runner's supported `-class` selection supplies the actual result.
No full suite, backend, cipher, PDF, OCR or previous physical CSV/Undo acceptance is repeated.

## Native qualification boundary

The independent Windows review completes all18 en/fa/de x light/dark x360/412/1280 contexts at process-local
font scale2 (system scale remains1). Native Pick-command/Next/Previous Invoke and actual viewport Scroll
traverse the original13 source rows. Nine hundred realized identity/status captions retain native scaling,
nonzero allocation, complete glyph bounds and no trimmed text. Seventy-two own-window images are retained;
representative English/Persian/German images are inspected. Every context ends with zero stored ledger
entries and no Import/Undo invocation. `native-acceptance.json` and the18 context records qualify this exact
scope. Only the picker effect supplies the fictitious file; this is not Android file-sheet acceptance.

Managed tests and compilation alone do not establish rendering or phone acceptance.
The first Windows build passed without warnings/errors after a diagnostic namespace correction.
The first native review did not reach the page and is not accepted. It is replaced with an explicitly
guarded independent importpreview application identity and original-page/native-Invoke/Scroll review.
The subsequent review build collided with Android-only restore (NETSDK1047); preserve that failure and
finish builds sequentially. Successful unrelated checks are retained rather than rerun.

The sequential independent Windows build passes with zero warnings/errors. The native review's generic
viewport finder incorrectly resolves the modal NavigationPage instead of the original ContentPage;
the focused caller selects the actual vertical viewport directly and awaits startup before attaching its
modal page. Final native review exits0. The earlier caption/viewport and ensuing shutdown title-bar errors
remain original failed review evidence; no successful context is replayed. The raw proof's original
NativePickerAndPaging field means the original native Pick command with its explicit fixture effect,
not a real external file sheet; the maintained source names this NativePickCommandAndPaging.

The complete offline Android candidate builds without warnings/errors and upgrades the Samsung without
data reset. Its path is `artifacts/android-import-preview-d331/pro.vafadar.zanance-Signed.apk`,95211423bytes,
SHA256704cb49b87561676f7bb46a56880f6be0e486373d6b9b780781180eb42b5f783. Signature verification passes
with the existing certificate92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b,
minAPI24/target36,ARM64/x64,384AOT libraries and no INTERNET/debuggable declaration. ARM64 SQLCipher
and Mono hashes match D-329. Installation success does not prove the new page: the phone remains on
NotificationShade and subsequently its adb transport is offline. No Import or Save is invoked, and the
exclusive phone turn is explicitly released.

After independent qualification, the normal Windows ApplicationId is restored and rebuilt sequentially;
`build-windows-production.log` finishes with zero warnings/errors. No diagnostic identity is handed off
as the product. The full offline Android package above predates only Debug-only observation corrections;
its production code/resources are unchanged by those corrections.

This work does not accept arbitrary bank formats, aggregate financial choices, unsafe Undo, external
corpora, all device configurations, performance targets, iOS, OAuth, Store purchase or product release.


## D-381 physical continuation

[Original Samsung qualification](physical-import-preview.md) now closes the previously open English
normal-scale physical picker/page-arrival/stale-mapping scope, without Import or Undo. The original
D-331/D-332 failures and emulator receipts remain historical evidence. This continuation uses the
unchanged D-377 APK, preserves prior profiles/Main/OS and does not establish wider runtime stability,
arbitrary-bank, all-language/scaling, TalkBack, iOS or publication acceptance.
