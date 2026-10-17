# Filtered transaction summary result (D-326)

## Observed cause and final behavior

The original Samsung transaction list filtered to Transfers showed `Transactions: 1 · total: `.
EntrySearch correctly excludes transfers from income/spending. TransactionsViewModel nevertheless
always selected the total-bearing format. The final application-source TransactionSummary formatter
selects the existing Report_TagCount resource only for an empty result dictionary. Actual zero-valued
currency results retain Tx_FilterTotal and the original MoneyText packets. Mixed entries keep the full
count and separate ordered currency results. No guessed transfer net, finance mutation or new locale
resource is involved; the actual view model calls this formatter.

## Focused qualification

- All 18 application-source tests pass, with no skips, under strict Release compilation: each of six
  languages checks non-result kinds, a real zero result and mixed currency/refund/transfer semantics.
  Four non-result kinds are exercised within each corresponding case. This is 18 added test cases,
  not a new full-suite run. Test outputs are cleaned once afterwards.
- The original standard Windows Debug build and final independent diagnostic build pass with zero
  warnings/errors. The actual TransactionsPage/ViewModel runs against one retained independent SQLite
  file under pro.vafadar.zanance.transactionsummary. All 18 en/fa/de, light/dark, 360/412/1280 contexts
  pass both transfer-count and expense/refund-zero captions, native scaled glyph bounds and exact
  stored account/entry preservation. Thirty-six caption captures are retained. Display states are
  set through the original view model; this matrix does not requalify filter-button interaction.
- The initial general snapshot launch was incorrectly scoped and accepted no case. It was stopped
  for that configuration error; moved development database files were restored with matching hashes.
  The independent diagnostic initially had an anonymous-property compile collision, then checked
  hidden native captions before rendering. These failures remain recorded. Rendering the original
  root before geometry checks resolved measurement. Six English contexts passed before a diagnostic
  compared Persian rendered digits with unshaped view-model text; retain those six contexts and use
  the existing NativeDigits rendering contract for the remaining twelve on the same fixture. A WinUI
  title-bar teardown exception from that failed diagnostic is retained, not declared a product fix.
- The complete offline signed Android Release build succeeds without warnings/errors. Signature v2/v3,
  embedded AOT count, permanent identity, offline permission boundary and unchanged native runtime/
  cipher bytes are verified before a data-preserving upgrade on the coordinated SM-A176B.
- The original physical main page shows `Transactions: 1` and the same 100 EUR transfer; expense filter
  shows three rows/net -21 and income one row/net +1000. Restore All types and Home 979. No financial
  Save, personal data, authentication, device settings, reset or new phone fixture is involved. The
  slot is explicitly released; only this review's temporary UI hierarchy file is removed.

## Complete local package

artifacts/android-transaction-summary-d326/pro.vafadar.zanance-Signed.apk, 94,711,541 bytes.
SHA256 b802e9f04a056bf59face646a7677369c0def6f64905d891b9adc2e6c9b1e25b.
Identity pro.vafadar.zanance; min API24/target36; ARM64/x64; 384 embedded AOT assemblies;
no INTERNET permission/debuggable flag. Existing local test certificate SHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
ARM64 SQLCipher SHA256 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c;
Mono SHA256 6532983fcd509af0c6fac4d39c37764128f5b12feedf9a9b32079635f70fc3e2.

Local evidence is under artifacts/phone-summary-d326 and artifacts/transaction-summary-d326/final.
The phone check covers English/light displayed preservation, not private raw-store/attachment equality,
all device locales or performance. The Windows matrix is development rendering, not OS text scaling,
spoken screen-reader or commercial Windows acceptance. Unrelated successful receipt/financial/backend/
crypto/performance suites are not repeated. Real corpus/camera, loaded/cold/ANR performance, Mac/iOS,
external providers, Store signing and product release remain open.
