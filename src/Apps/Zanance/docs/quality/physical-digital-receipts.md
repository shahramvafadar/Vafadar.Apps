# Physical six-language digital receipt reading (D-324 / QA-05 / AT-71)

## Complete native batch and unchanged binary

The approved Samsung SM-A176B (ARM64, API36, 4KiB) runs the unchanged D-322 complete offline signed
Release APK, commit 92b8a181014aaf629da73002a163577386c0ee15. The APK remains
artifacts/android-null-bitmap-d322/pro.vafadar.zanance-Signed.apk, SHA-256
7ee246c6e3d74eab4ca1041ba3ead3d3818e646a52cfa7f11932ce43e8331fa3.
D-322 strict builds/package/signature qualification are retained; no unchanged app build, installation,
full suite or successful financial/crypto workload was repeated. The original process 27876 stays alive
through all six new receipt operations. Its retained Main profile begins and ends at EUR 979.

The device turn was explicitly coordinated with the other project. No system setting, authentication,
new permission, personal document, account or financial Save was involved. Only exact newly created
fictitious input filenames were transferred, indexed, selected and later hash-checked before removal.

## Six distinct source-language inputs

Each one-page digital PDF is visibly labelled FICTITIOUS RECEIPT - NOT A PURCHASE and dated 2026-10-17.
Its order identifier 999999 is not money; the purchase total 24.50 EUR differs from cash 50 EUR and change 25.50 EUR.
The original report writer and embedded Vazirmatn font generate the fixtures with correct Persian shaping;
all six rendered pages are visually checked before transfer. The standalone fixture author has no app
licence bootstrap and its evaluation notices remain visible: no credential is read or watermark removed.
Those notices precede the merchant in extraction, so merchant/title accuracy is explicitly not accepted
from these fixtures. They qualify purchase-total evidence, not realistic receipt-corpus coverage.

| Source language | Purchase-total row | Native unsaved amount |
|---|---|---|
| English | TOTAL 24.50 EUR | 24.50 |
| German | GESAMTBETRAG 24,50 EUR | 24.50 |
| Spanish | TOTAL A PAGAR 24,50 EUR | 24.50 |
| French | NET À PAYER 24,50 EUR | 24.50 |
| Italian | TOTALE DA PAGARE 24,50 EUR | 24.50 |
| Persian | مبلغ قابل پرداخت, Persian digits ۲۴٫۵۰, EUR | 24.50 |

Host preflight uses the original PdfText.Extract and ReceiptParser on these exact bytes and gives 24.50
for every input. This is separate from the actual phone path: original Home receipt action -> contextual
Choose a photo or PDF -> Android DocumentsUI -> the exact owned file -> original digital-PDF extraction ->
original typed New expense editor. Each native editor displays its distinct complete total-source row,
Purchase total found / Check it before saving, amount 24.50, EUR and the unchanged original account.
The Persian native source normalizes the Persian numeric text while retaining its Persian total phrase.
No cash/change/order number is substituted. The English app presentation remains unchanged throughout;
these are six source-document languages, not six app-language/theme or spoken-reader contexts.

Each real Cancel is followed by the original Discard changes? dialog and explicit Discard. Home returns 979
after each case. Final Transactions retains the original five rows/net 979 and final Home remains 979.
Only displayed count/balance preservation is proven: the private release database is not exported for
raw schema/typed-row equality, and attachment-storage equality is not inferred from a cancelled draft.
No receipt is saved or reread as a persisted attachment in this batch.

## Continuation and exact cleanup

An initial strict foreground check refuses input before the own window is confirmed; a fresh native read
confirms the already-unlocked application and the original action continues. An initial English selection observes
an empty transitional native root; the same PID then publishes its editor, without selecting the PDF again.
ADB-transferred files initially are absent from the picker index. Media scanning is requested only for the
six owned paths, after which those same files appear; no app/picker policy is changed.

After four completed cases, the local observer incorrectly expects a RecyclerView for the picker grid.
It stops before scrolling or selecting the French file. The actual DocumentsUI ScrollView and exact owned
folder are inspected; only the incomplete French/Italian operations continue through observed bounded
scrolls. No successful English/Persian/German/Spanish operation or original fixture is restarted.

Before cleanup, every remote input hash must still match its recorded local hash. Exactly those six files
are removed individually; rmdir removes only the now-empty new folder. No recursive delete is used.
Final app state is Home, English/light, Main 979. The phone is released to the other project.

Local evidence: artifacts/phone-receipts-d324 contains inputs.json, rendered samples/review.png,
original author source, own-package UI hierarchies and acceptance.json. Six per-document assertions bind
actual EditText values, review notices, Discard dialogs and returned Home to those expectations. No new
managed unit-test count is claimed. No production receipt algorithm, schema, permission or binary changes.

QA-05 remains In progress: real merchant/receipt corpus, photos and image OCR, camera acquisition,
missing/ambiguous/tampered/scanned/rotated documents on the phone, all six app languages, other devices,
spoken output and iOS remain separate. This batch does not establish release acceptance.

## Immutable local input identities

| File | Bytes | SHA-256 |
|---|---:|---|
| zanance-d324-de.pdf | 14347 | a03d9e2419ec58e2cfb46cc0e9c05fa3afa76ec3823b908e9e593e341aa5e875 |
| zanance-d324-en.pdf | 13881 | 6f6dcffa10e283897f71d7892733bb2632986810346153aa78f728b2d86a6919 |
| zanance-d324-es.pdf | 14191 | 5137cf34862528ddd2181386d981dbd7a6c94f94b9e746ee1683a5516fd5a450 |
| zanance-d324-fa.pdf | 16290 | 0ba5e3250fe244b713a9543e4f82acf4c289a814c36dac74ad521751fd747c74 |
| zanance-d324-fr.pdf | 14009 | 1c2a75c1b65ab8733b786870ae275f610fcffad6831077dc0acbab0df8e08df0 |
| zanance-d324-it.pdf | 13919 | e75a70b41cc58128cfd2cb3e2aafdb721c6f5310947265504d3830fcab95e7c0 |
