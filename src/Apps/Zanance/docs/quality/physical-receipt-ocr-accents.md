# Physical receipt OCR and Latin keyword accents (D-325 / QA-05 / AT-71)

## Observed defect and bounded correction

Seven new raster-image receipt cases exercise the actual approved Samsung, independently of D-324's
successful digital PDFs. On the original D-322 APK, English, German and Spanish give 24.50 EUR, but the
French NET À PAYER image opens the real editor with no total and a blank amount. This is safe refusal,
not successful recognition. Its exact image SHA-256 is 793a511c6a1bb346ad4c20600c61875061cd74675a77594b9376f6d03878bc3a.

An independent offline Release/AOT package reads only this embedded fictitious image through original
DocumentImages, TextRecognizer, TextLayout and ReceiptParser. It confirms the same hash and captures
NET Å PAYER 24,50 EUR, an unambiguous layout and no total. The native engine changed the accent on À;
the digits and EUR survived. This narrow diagnostic is not production compilation/performance evidence.
Its package pro.vafadar.zanance.ocrreview never references or reads main storage and is removed after diagnosis.

ReceiptParser now folds combining marks attached to ASCII Latin bases only while comparing keyword
and exclusion tokens. The same normalization applies to positive total labels, tax/cash/payment exclusions
and merchant-label exclusions. Whole-word/phrase boundaries remain required. Non-Latin combining marks
are retained. Original review source, digits, signs, currency/unit association, precision, ambiguity and
financial storage remain unchanged. No spelling-distance matching, numeric repair, inferred currency,
new OCR model, network permission, provider or schema is introduced.

## Related checks

Ten added AT-71 behaviors cover observed composed/decomposed accent drift and original source/date/shop/unit
preservation, with cash, tax, misspelled words, signed/damaged numbers and conflicting currency still refused.
Before the fix, the ten-case run has three failures, including the exact observed Å label. After the fix,
all 118 original receipt-namespace cases pass with no failures/skips under strict compilation. The full
suite and successful unrelated database/financial/crypto checks are not repeated. Test output is cleaned once.

Strict Windows and complete signed offline Release Android builds have zero warnings/errors. There is no
changed layout, resource key or translated copy; the original receipt editor renders the changed result on
the actual phone. Earlier successful unrelated presentation matrices are retained, not replayed.

## Original physical continuation

The final APK upgrades the main app without clearing data. PID 27876 becomes 8200 and the covered startup
returns Home 979. The same failed French image now gives the reviewed 24.50 EUR in the original New expense
editor, while its source still visibly says NET Å PAYER. It is explicitly Cancelled/Discarded, never saved.
The three successful pre-fix image cases are retained; only the failed French case and the previously
unperformed Italian, missing-total and currency-conflict cases run on the final binary.

| Image case | Actual original-app result |
|---|---|
| English TOTAL | 24.50; explicit review and discard |
| German GESAMTBETRAG, source EXIF orientation 6 | 24.50 after original image preparation; discard |
| Spanish TOTAL A PAGAR | 24.50; discard |
| French NET À PAYER, OCR NET Å PAYER | Initially blank; final 24.50 with source retained; discard |
| Italian TOTALE DA PAGARE | Final 24.50; discard |
| No total; item 99.99, cash 100, change 0.01 | Final blank amount and missing-total explanation; discard |
| TOTAL 24.50 EUR and TOTAL 18.00 USD | Final blank amount and explicit total-review explanation; discard |

The app remains English/light; source labels span five Latin-script languages. These are synthetic clear
images, not a real-shop corpus, camera, scanned-PDF, skew/lighting, six app-language or spoken-reader matrix.
Android retains its bundled Latin model; [official script support](https://developers.google.com/ml-kit/vision/text-recognition/v2)
does not establish Persian-image recognition. D-324's Persian digital-PDF result is a separate text-layer path.
No new language-model download or language-support claim is made.

Every draft uses the real Cancel/Discard dialog. Final native filters report three expense/refund entries
net -21, one income 1,000 and one transfer 100 with no income/spending net. Original Home remains 979.
This is displayed count/balance preservation, not raw private database/attachment equality. Only exact
hash-matched newly owned seven input images are deleted individually, followed by their empty new folder.
The independent diagnostic package and owned UI-dump files are removed; the complete main app stays on Home.
No OS setting, credential, authentication or personal file is touched; exclusive device use is released.

Native observation refusals (transitional empty roots, picker focus not yet published, stale sleeping-window
focus) do not restart a source or app. Continue the same operation/PID. The diagnostic's initial nullable-log
compile error is repaired only in its local caller. An initial APK verifier guessed the cipher filename;
actual libsqlcipher.so is inspected instead and has the unchanged qualified hash. These are retained
observer/preparation failures, not additional app fixes or waived product checks.

## Final package and remaining work

APK: artifacts/android-ocr-accent-d325/pro.vafadar.zanance-Signed.apk; 95149983 bytes; SHA-256 fa9b6480c2ce83de3d395c0ba2d355c4c7f0450d1e447db7e3833767883d6891.

Permanent package pro.vafadar.zanance, ARM64+x64, 384 AOT libraries, no INTERNET/debugging and verified
v2/v3 local test signature. Certificate remains 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b;
ARM64 cipher remains 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c and Mono remains
6532983fcd509af0c6fac4d39c37764128f5b12feedf9a9b32079635f70fc3e2. This is not a Store signature/release.

Local artifacts/phone-ocr-d325 holds image source/manifest/rendered contact sheet, original native hierarchies,
fr-diagnosis.json, 118-test/cleanup/build logs, package proof and acceptance.json. QA-05 remains partial for
real corpus/camera/scanned images, other devices/iOS and platform acceptance. No latency or full release claim.

Separate observed ready defect: transfer-only Transactions currently prints Transactions: 1 · total: with
an empty monetary suffix. Its one-entry count is valid but its caption needs correction; this is not repaired
by the receipt change. Preserve count-transfers.xml and address the existing summary presentation next.
