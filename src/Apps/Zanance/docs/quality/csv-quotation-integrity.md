# Do not normalize malformed CSV into financial amounts (D-387)

## Original failure and rule

The remaining quote transition in Csv.Read accepted a quoted field followed by unquoted characters.
The controlled generic source `2026-10-19,"12"34,Fictitious source` parsed the amount as1234 and
produced an importable123400 minor-unit expense through the original CsvImport.PreviewGeneric.
No financial Save was needed to expose this IO-08 ambiguity. An unquoted field could also contain
an unescaped quote. Nine new Core regressions fail on the old parser; three actual App selection
cases replace the original complete review before the correction.

Refuse a quote inside an unquoted field and any non-delimiter/non-record character following a
closing quote. Do not strip stray quotes, combine amount digits or guess a repair. The selected
file must be corrected by its producer. Existing doubled quotes inside a quoted field, embedded
newlines, Unicode, UTF BOM, unquoted whitespace, separators and quoted EOF are preserved.
[Original RFC4180 section2](https://www.rfc-editor.org/rfc/rfc4180.html#section-2) specifies escaped
versus nonescaped fields and doubled quotes. This change concerns quoting, not a claim of complete
strict-ASCII RFC conformance; existing Unicode, alternate record endings and delimiter choices stay.

Reuse the existing CsvMalformedInputException and non-destructive selection path. Feedback in all
six languages now describes incomplete or misplaced quotation, rather than only an unfinished tail.
The original file/review or completed outcome remains unchanged and no import is silently accepted.
StarMoney keeps its existing stricter syntax/field/row/encoding contract and original exceptions.
No new bank preset or external bank-format support is inferred.

## Focused source qualification

Nineteen new Core cases pass, including eight malformed boundaries, the real amount-coalescing path,
six complete writer round trips with BOM/comma/semicolon/tab/Unicode/literal quote/whitespace/multiline
fields, three record endings and empty quoted-column/EOF preservation. Three new actual-source App
cases keep the original review, complete stored financial snapshot and import eligibility after
malformed amount selection. New managed-case count22, not the assertion/field count.

Twenty existing CSV/export cases pass because the shared parser changes. The existing100000-row
original export/stream/parser/preview test is selectively rerun and passes in1.604s with all cells
and mapped financial fields preserved. Six affected StarMoney original syntax/encoding cases pass.
No unchanged write/Undo, mapping concurrency, billing, crypto, backend or whole-suite replay.

Retain the initial test-only invalid enum compile failure, then the authoritative nine/three old-source
failures. Initial combined class/method filters selected zero tests; those runs establish no acceptance.
The corrected category-specific filters produce39 Core, three App, one large-case and six bank results.

## Original native pages, packages and restoration

The original Windows page and native Invoke command complete18 en/fa/de Light/Dark contexts at
360/412/1280px using only the changed malformed case. Complete original reviews/stored fictitious
inputs and untrimmed error captions pass; inspect all18 application-window images. The private
non-input desktop restores Default exactly. This uses process-local font stress, not real OS or
spoken-reader acceptance. Run4e855359883a443d8e43a9c8958e2649, terminalPID97096, strictDebug47.19s,
zero warnings/errors. The final ordinary strictWindowsRelease build also passes in20.24s.

The independent original Release/AOT x64 page completes six en/fa/de Light/Dark contexts with
actual focused native picker-button invocation and only the selected-file effect supplied. The
misplaced-quote text, complete old review/data and native visible TextView/ellipsis geometry match.
Original theme/language and exact OS restore; no Import/Undo or Main private-data access. Run
25b9dab64bf849fcb24e39246d773215, comparison687243e3fe4b4d57a3f850d490975445, terminalPID12926,
sourceMVIDad844ac3-aa60-440c-ba6b-392304595035. Stop only that terminal caller and return Main12404.
This does not repeat actual native-provider qualification or D-385's unchanged byte-limit matrices.

Strict independent Android build126.07s, zero warnings/errors. Immutable x64 APK SHA256
487e03fd3cd5d464ef091236d6f8d218a07ecbe56ca154f8ba6ef467f2aead4c,
192AOT/four unchanged engine libraries/offline/default concurrent major GC. The canonical complete
signed offline Main package is artifacts/android-csv-quote-d387/pro.vafadar.zanance-Signed.apk,
SHA256acc02b00a9bdc237e50e736f8b96a6eec5d50af25561a7eda23d4da01e51f05c,
95305631bytes,384AOT/ARM64+x64/API24/36. CertificateSHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b and all eight engine libraries
match the previous signed package. No Internet/debug/profile permission, prototype bridge or GC override.

No-reset emulator upgrade12404 to13316 matches every meaningful original native Home0 node, exact
OS settings and installed package hash. Samsung remains unchanged at D-385; there is no new physical
qualification claim. Keep source-bound old negative results, exact current signatures and independent
versus Main receipts distinct.

## Privacy and scope

No data schema, compiled model, financial-write algorithm, technical budget, SDK, permission or raw
input archive changes. This is original CSV correctness development under IMP-01/IO-06/IO-08, not a
new general ANR/crash or Android accessibility track. D-386 keeps those tracks scheduled after approved
feature development. Arbitrary bank/native-provider corpora, general stability, full phone/iOS,
external provider and release acceptance remain open. The current Samsung package is not changed
by independent emulator/Windows verification. Local artifacts/csv-quote-syntax-d387 retain negative
and terminal source-bound receipts, focused test XML, builds and immutable packages.
