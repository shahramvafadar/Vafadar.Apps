# Germany accountant data and stored-document export (D-296)

## Approved batch and boundary

Deliver the complete Level 1 preparation path for employees and freelancers: choose a Gregorian calendar year
and original financial accounts, review the exact disclosure, optionally include stored document copies,
then explicitly prepare/share a readable ZIP. Germany is the only selectable jurisdiction. The default is the
current calendar year; historical ledger exports do not imply historical tax-rule support. The complete
Plus preparation feature is separate from the country/year TaxAddOn for later calculations. Current test
builds remain commercially inactive, and ordinary owned-data CSV and portable backup stay available.

This does not calculate tax, deductibility, VAT, depreciation, cash/accrual recognition, business proportions,
EÜR or a return. No DATEV/ELSTER compatibility, legal-original archive or GoBD certification is advertised.
An accountant must reconcile external source records, original invoices, completeness and applicable rules.
Level 2/versioned country-year calculations and expert acceptance remain open; filing stays outside this batch.
Commercial Windows is still an unscheduled future phase. Windows running checks here are development evidence.

## Official-source research, checked 2026-10-17

- [BMF consolidated GoBD 2026, Annex 33](https://amtliche-handbuecher.bundesfinanzministerium.de/ao/2026/Anhaenge/BMF-Schreiben-und-gleichlautende-Laendererlasse/Anhang-33/inhalt.html):
  electronic records, original state, traceable changes and evaluable structured data have requirements beyond
  a personal ledger CSV. Product inference: a newly generated packet cannot claim to replace the source archive.
- [BMF e-invoice FAQ](https://www.bundesfinanzministerium.de/Content/DE/FAQ/e-rechnung.html) and
  [15 October 2025 application guidance](https://www.bundesfinanzministerium.de/Content/DE/Downloads/BMF_Schreiben/Steuerarten/Umsatzsteuer/Umsatzsteuer-Anwendungserlass/2025-10-15-einfuehrung-obligatorische-e-rechnung.pdf?__blob=publicationFile&v=3):
  original structured invoice information matters. A photo or ordinary PDF must not be represented as equivalent
  structured original evidence. The app labels exact stored bytes as copies and asks users to retain originals.
- [Official ELSTER EÜR form service](https://portal.elster.de/eportal/formulare-leistungen/alleformulare/euer):
  statutory form submission is a separate workflow. This packet uses its own documented data format, not that form.

Direct handbook/FAQ/guidance fetches returned HTTP503 and the ELSTER fetch HTTP502 during this research;
current indexed official pages and their published guidance excerpts were available. No unofficial calculator, rates, deduction rule or future official form was
substituted. No statutory rate calculation is implemented, so there is no invented 2026 tax-rule acceptance.
A qualified expert and official examples remain necessary before Level 2 work can be accepted.

## Source and format

`AccountantExportStore` uses one deferred SQLite read transaction per review/build. Only explicitly selected
year/account entries, necessary account/category identities and opted-in attached documents are read. Orphan
Undo attachments and unrelated accounts, previous years, plans, estimates, settings, device security, private
sync/household/billing authority and credentials do not enter the packet. Counterparty identities are disclosed
explicitly when a complete transfer touches a selected account; other counterparty history/opening balances
are excluded. Archived accounts remain selectable. No financial row or preference is written.

- `transactions.csv`: stable original ids, recorded ISO dates/kinds, exact minor units and currency, known
  invariant decimal precision, source/destination legs, original currency, refund/group links, review/source,
  reimbursement/aggregate metadata, tags and original audit dates. Payees/notes/reimbursement names are off by
  default. Unknown precision leaves decimal blank and flags review rather than guessing two minor digits.
- `accounts.csv` / `categories.csv`: only relevant identity/context, with blank tax classification; no opening
  balances. User text is retained and spreadsheet formula markers, including indented markers, are neutralized.
- `accountant-review.csv`: incomplete/unreviewed, aggregate, refund/group and unknown-precision review hints;
  tax category, business share, VAT treatment and accountant comment are intentionally blank.
- `recorded-totals.csv`: source amounts and counts separated by recorded currency/kind using unbounded integer
  accumulation. Transfers, assets, adjustments and refunds are not turned into taxable income or a profit total.
- `documents.csv` / `documents/<opaque-id>.<trusted-suffix>`: only when explicitly enabled, exact stored bytes,
  entry links, original name as escaped metadata, size, media type and SHA256. External names never form paths;
  unrecognized types use `.bin`. One blob is loaded at a time; the index is a separate metadata-only scan.
- `manifest.json` / `README.txt`: format `zanance-accountant-data/1`, DE/calendar year, exact selection,
  disclosure choices/counts, actual file SHA256 values and clear boundaries. Headers/instructions are English;
  the six-language UI explains this. Hashes detect byte changes, not legal authenticity; the manifest is unsigned.

Review retains a digest and counts, not a second copy of all ledger rows/documents. Build recomputes the same
content in a fresh coherent reader and rejects changes before publication. After private cache writing, a fresh
validation rejects a changed source, actual file/profile, rights, local day or original form epoch before sharing.
No database reader survives a native confirmation or share await. Re-entry/reload clears disclosure choices;
selection/year/options changes retire the old review. Locks, cancellation and failed reads do not release data.

## Native data handling and limits

The native adapter first uses an explicit readable-ZIP confirmation and existing app-lock authentication.
Only then does a worker stream the packet to a unique file in the application's private cache. Failed
unpublished files are removed. An offered file remains readable for late Android consumers and is removed
once older than24 hours on the next export. Recipient-controlled copies cannot be recalled. The folder/file
must be ordinary, owned and neutral; at most16 retained files are permitted. No backup password is stored.

Technical safety bounds:128 MiB compressed archive including ZIP directory bytes,128 MiB total stored
attachments, each attachment at most the existing5 MiB limit,10,000 copies and10,000 relevant accounts,
512 MiB generated text and65,536 characters per field. Bounds refuse the whole packet with translated feedback;
no row/document is silently dropped. These are safety bounds, not commercial quotas. ZIP streaming keeps
working memory independent of the ledger-row count. There is no new schema, migration, native library or SDK.

Other approved working targets are named as being added without an ETA: AT, BE, FR, LI, LU, MC, NL, CH, GB,
IE, US, CA and AU. They are not selectable tax rules and are independent of language/formatting/holiday choices.

## Verification record

- Actual source flow class:20 new SQLite/localization/command/native-port cases passed. After the document-index
  streaming refinement, only its two packet/byte/hash cases were repeated and passed.
- Three new edge cases passed: exact Int64/unknown-currency behavior and indented formula text; oversized document
  refusal without silent partial export; future-year/missing-account refusal with unchanged stored data.
- Updated complete policy matrix76 cases passed; default test enforcement remains inactive. No full application
  suite was rerun or recounted. Earlier full-suite baseline3753 remains historical.
- Resource completeness21 cases passed. The literal-use check exposed an older scanner false positive on the
  computed `Billing_Replacement_` prefix. Literal `Format` keys now require an argument delimiter; all six actual
  enum-derived replacement families are checked separately. The two literal/empty checks and six runtime-key
  cases passed. No billing/provider purchase scenario was replayed.
- Windows strict build and Android Debug strict build passed with zero warnings/errors. Initial Windows assets
  mismatch required the actual Windows target restore; a missing diagnostic extension import was fixed after
  its compile failure. These failed logs are retained. Final native/package results follow below.

The native18-context and Android package/share continuations below complete local qualification. Current
documentation, commit/push and one deferred CI check accompany delivery. CI, Mac/iOS, physical devices and
external accountant/legal acceptance are separate.


### Completed native Windows and packaged Release evidence

`artifacts/accountant-native-d296/complete.json` records18 final contexts. Actual original SelectAll, Review and
native readable-ZIP Cancel were invoked with native automation providers; no authentication/cache/share effect
or financial write occurred. Complete financial fingerprint remained
`752b859c22c76e8c75b502438240f7a7d27cd6b522772aec2cf087e2883f09df`.
The first long native title was visibly clipped at360; original captures and cases remain under
`before-title-fix-*`/`cases.json`. The short translated native title was recompiled and affected presentation
rechecked with `cases-title-v2.json`; final en/fa/de captions and native targets passed. The own-window review
never activated the desktop or sent desktop input. This is not OS scaling/screen-reader/device acceptance.

Complete current APK: `artifacts/android-accountant-d296/pro.vafadar.zanance-Signed.apk`,92,848,685 bytes,
SHA256`d42d5812a506e0b248b46a4712f25774eb2d76b144dfa5b80082c584347f51ec`. ZIP CRC, permanent package
`pro.vafadar.zanance`, minimum24/target36, arm64-v8a+x86_64 and actual v2/v3 signature passed. Certificate
SHA256`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is the existing local test
certificate, not production/store signing. The selected billing-sandbox edition remains explicit; no genuine
purchase/provider scenario was replayed. `package-proof.json`, signer and badging evidence are local artifacts.

Upgrade of the exact existing fictitious emulator installation succeeded without data reset. First activity
observation timed out and the first hierarchy returned null; original process23329 continued to covered
startup and actual Home. Only subsequent hierarchy observation of that same process was used. A second
transition observation returned null before Import/export became available; no tap or successful scenario was
replayed to hide it. Actual accountant native continuation is recorded below after completion.


### Actual main Release Android continuation

On the same23329 process, original More -> Import/export -> Accountant packet navigation loaded the actual
protected existing fictitious My account/EUR profile. Year2026, no selected accounts and both optional
notes/documents off were observed. Native SelectAll and Review produced the honest0-row/0-copy summary for
that existing empty ledger; no financial fixture was seeded in the main app. Full nonempty transfer/refund/
document behavior is covered by the source-flow packet tests and independent native Windows reader.

Actual readable-file confirmation Cancel retained the original review and enabled Share. A separate explicit
confirmation then used the production runtime to build/validate the ZIP and open Android's real
`com.android.intentresolver` with `zanance-accountant-b71141b8d38b493fa3dc4003054fefca.zip`.
No recipient/account/service was selected; Back dismissed the share sheet, the original form returned without
an export error, and native Back left the page. This verifies main Release query/cache/native-share handoff,
not a receiving application's completed save, physical-device sharing, all Android language/width combinations,
real biometric authentication, iOS or tax/legal acceptance. APK installation preserved existing app data.
The private cache follows its documented expiry rather than deleting a successfully offered file too early.
`artifacts/accountant-d296/android-native-proof.json` and retained fresh hierarchy XML record the observations.

### Remaining execution

ZCR-TAX-02 Level1 is locally implemented and qualified. TAX-01 official-source boundary research is recorded;
expert examples/acceptance for statutory calculations remain open. TAX-03 Level2 is not implemented or claimed.
Next ready independent batch is approved bank-file import research/implementation, German-first then USA/other
working markets, followed by approved local AI consent/credit and FX/source/cache/manual prerequisites.
Native StoreKit2/Mac, genuine store/OAuth/provider, actual physical device and public/commercial release gates
remain retained; no current or future gate is marked accepted by these local results.


### D-328 actual physical recipient and byte inspection

D-328 completes a scoped physical Germany accountant export on SM-A176B using the unchanged signed offline
D-327 app. The original native chooser delivers two readable ZIPs only to a separately installed offline
receiver: both accounts/five rows, then cash only/the same one transfer plus its unselected Main identity.
The host verifier checks actual CSV/manifest bytes, six member digests, default redactions, blank tax fields,
refund/fee references and separate recorded-kind totals. Empty selection, confirmation Cancel and stale-review
invalidation are exercised. Twelve saved-observation/receipt assertions pass; the receiver strict build has
zero warnings/errors. The new receiver is removed, final Home remains 979 and the phone turn is released.
No production code/APK, schema, permission or managed test count changes; successful unrelated tests/builds
are not repeated. Physical documents/notes-enabled/all-layout, iOS, tax specialist/calculation/filing and
release acceptance remain open.
See [physical evidence](../../quality/physical-accountant-export.md). The earlier next-batch paragraph is historical; current remaining work is the canonical backlog.


## D-380 physical notes/document continuation

A fresh fictitious Samsung profile qualifies explicit notes/payees and one known stored-PDF copy, then
redaction after a new review. Original financial fields match and PDF bytes match the synthetic input.
See [physical evidence](../../quality/physical-accountant-document-disclosure.md). Main data is not edited;
original three profiles/Home979/OS/APK remain. Legal, tax expert and original-document acceptance remain open.
