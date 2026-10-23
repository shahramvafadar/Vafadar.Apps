# Original image-only PDF reading (D-503 / QA-05 remains partial)

## Fixed new scope

Nine synthetic PDFs contain fifteen raster pages and no text layer. Six single-page scans
reuse the visible English/German/Spanish/French/Italian/Persian D-324 source pages, with their
original hashes and evaluation notices retained. Render those originals at 200dpi and embed
only their pixels in fresh PDFs. The old successful digital-text/photo reads are not repeated.
Three new synthetic boundary documents cover a six-page last-page total, two-page conflicting
currencies, and cash/change/item text without a final total. All fifteen produced pages are
rendered again and visually inspected before the native operation. No personal receipt, new
SDK/model, external upload, credential or financial writer is involved.

The original native PdfText extractor independently confirms an empty text layer in all nine
actual packaged PDFs, including its sparse-text fallback. Actual page counts and SHA256 digests
match the fixed manifest. The original DocumentReader therefore takes its previously unqualified
PDF-rendering/OCR branch, rather than a digital text shortcut. Its original first-three/last-page
bound, page separators, receipt interpretation and currency/unit evidence remain unchanged.

## Actual Samsung observation

Samsung SM-A176B/API36/ARM64/4KiB runs the signed independent Release/AOT caller once, run
`440b8193e62f4c3780ca24fe2fae83bd`, comparison `049f895eef914c86bae1cb92491e1dc5`, PID2642.
Ground truth is compared only afterward; it is never supplied to the recognizer or parser.

| Image-only source | Actual original parser result |
| --- | --- |
| English, German, Spanish, French, Italian | Each Found24.50 with explicit EUR |
| Persian | NotFound; no amount suggested |
| Six-page English | Found42.75EUR from page6; reads pages1/2/3/6 |
| Two-page EUR24.50 / USD18.00 | Review; both currency candidates retained, no selected amount |
| Item99.99 / cash100 / change0.01, no total | NotFound; no inferred purchase amount |

The deliberately conflicting999.99EUR total on page4 is outside the original bounded page
selection and does not appear in recognized text; page6 does. This demonstrates the existing
bound, not acceptance of every long document or a claim that skipped middle pages are validated.
Cross-page EUR/USD remains a review outcome even though geometric column alignment is unambiguous.

The bundled Android engine explicitly uses Latin TextRecognizerOptions. Persian labels/digits
are not recognized reliably in this scan and no total is found. This capability remains open:
successful Persian digital-PDF parsing does not establish Persian image/scanned-PDF OCR. No
numeric repair, inferred currency, new engine or unsupported quality claim is introduced.
The six reused synthetic source pages retain evaluation watermarks; merchant/title quality is
not accepted from them. Clear synthetic raster cases do not replace real-camera/blur/skew/lighting
corpora, all six app-language/theme contexts, stored attachment flows or iOS acceptance.

Median366.07ms/max1060.37ms cover the original document-read operation only, not startup, native
picker, editor, full workflow latency or a product speed objective. Eighteen assertions compare
stored observations with the declared inputs; these are not eighteen new managed unit tests.
No OCR/document read or accepted financial operation is repeated to repair reporting.

## Package and original restoration

One strict independent Offline build passes without warnings/errors. The complete installable
APK contains192AOT modules, five byte-identical existing native engines, valid v2/v3 signatures,
no INTERNET permission and no debuggable declaration. SHA256:
`0bcd36e2b783b9ef7080c5ba7f92f940726e8f7ed6170fcfc69bfd74a53a40f1` (57,255,237bytes).
All nine PDF assets and manifest hashes match; D-501 public corpus assets are absent. The shipping
product never references these inputs; source PDFs, raster intermediates and reports stay outside
tracked code/product assets. No unchanged Windows/shipping build or unrelated suite is repeated.

Stop only the independent process and restore its exact old APK/data/stopped state. Main remains
the original PID25043, Home979, all141 public UI tuples, APK and exact OS settings. No Main private
database/key comparison is claimed. Two exact newly owned remote XMLs are hash-matched/removed;
local originals remain. D-494 still has PID24914 and all four original selected alarm/intent
handles, without provider read, window, rearm or subject replacement. D-496's inactive future-day
source remains. No Main upgrade occurs before timed-cohort completion/cleanup.

QA-05 remains partial for Persian images, genuine receipt/camera/corpus coverage and other
platforms. Security/stability/accessibility/public provider/store/release gates remain separate.
The one delayed D-501 response reports CI40007631698 and CodeQL40007631709 success; its consumed
monitor is deleted. New-head CI follows this completed observation batch.
