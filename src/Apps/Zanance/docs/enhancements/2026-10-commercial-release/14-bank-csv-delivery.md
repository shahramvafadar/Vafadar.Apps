# Documented German bank-software CSV import (D-297)

## Complete local delivery and explicit boundary

The original Import/export page now offers a separately selected StarMoney Giro ordinary-cashflow preset:
choose UTF-8 or Windows-1252, pick the original file through the platform picker, select a Cash/Checking/Savings
account, review every proposed row, acknowledge posted ordinary income/expenses, and confirm the exact count
and account through the real native dialog. The existing atomic importer, aggregate-overlap decisions, source
ids and durable batch Undo remain the writer. Reopening the form requires a fresh review. Generic CSV remains
an explicit separate choice; an unsupported bank revision never silently falls back to guessed columns.

This is one documented software-export variant, not universal German bank support. Transfers, refunds,
reversals, pending transactions and split parent/detail sets must be excluded from this source. The file has
no independently verified booked-status field; positive/negative amount alone proves no economic relationship.
The explicit per-import acknowledgment makes that boundary visible. Mixed relationship imports and bank-specific
revisions remain open until their original schemas and realistic fixtures establish unambiguous mapping.
OFX/QIF/CAMT/MT940 and live connections are not added under this CSV approval. BANK-01/IMP-01 remain In progress.
No bank login, provider account, SDK, network permission, commercial quota activation or database schema changes.

## Original sources checked 2026-10-17

- [StarMoney Basic/Deluxe import/export help](https://hilfe.starmoney.de/hc/de/articles/360000475833-Import-und-Export),
  updated16 September2026, describes configurable CSV field selection and export source/period.
- Its [original field specification](https://hilfe.starmoney.de/hc/de/article_attachments/360013071654),
  Giro table/header example on pages1-2, establishes the supported field names/date/decimal conventions.
  This is an official structural sample, not customer data or runtime acceptance of every bank/product.
- Required: BuchDatum, Betrag, BtgWaehr, AgName1, Storno, ID, QkIBAN and all VWZ1..VWZ14.
  Recognized optional source fields remain separate from ledger algorithms. Unknown columns/revisions refuse;
  nonempty split columns/Lfd.Nr refuse the whole preset. Any nonempty Storno refuses because its complete
  original value semantics have not been qualified. Never invent an N/0 false marker.

Fixtures contain fictitious payees, amounts, ids and source identity constructed from those documented columns.
No owner bank file or third-party sample was downloaded into the checkout. StarMoney is the format's proper
name; it is not a claim of partnership, endorsement or live-bank integration.

## Data and financial correctness

Strict explicit decoding refuses conflicting BOMs, replacement/control characters, malformed quoting and
unsupported layouts. Bounds are8MiB/10000 data rows/128 columns/4096 characters per field, with no truncation.
The original file is read only. One normalized source-account identity and explicit matching ISO currency are
required. Dates are Gregorian dd.MM.yyyy, exact signed decimal-comma values use the existing minor-unit parser.
No FX conversion, loan principal interpretation or category-tax inference occurs. All imported rows start
Unreviewed; categories stay Uncategorized. Complete remittance lines, source category/comment/value-date
references, payee/title and valid optional original-currency amounts are retained. Source IBAN itself is not
copied to the ledger; original free text can contain sensitive identifiers and is disclosed as such.

Software ID + normalized source account + selected local account bind the stable import id using framework
SHA-256. This is an idempotence namespace, not bank authentication or new cryptography. Same ids skip and never
overwrite later edits; same date/amount/kind is only a possible duplicate, so distinct real equal expenses remain
possible. Moving software history to a different source namespace requires a reviewed import, not guessed merging.

One deferred SQLite snapshot reads the original account/category/entry/import-journal inputs. Exact source file,
cached commercial authority, form epoch and immutable proposed payload remain bound across native awaits.
The original writer revalidates those same financial inputs inside its transaction using its already loaded rows,
then preserves existing ledger validation, unique posting, aggregate choices and durable Undo. Source history is
retained in memory for complete review; this does not claim constant memory independent of ledger size. No
attachment blobs, security keys or unrelated private service authority enter the packet. No repeated SQL pass
is introduced inside the writer. A changed history/account/currency/rights or retired form refuses stale consent.

## Focused verification

29 new BankCsvFlowTests cover six-language confirmation/cancellation, exact amounts/remittance, invalid rows,
encoding/BOM/quoting, duplicate ids, unknown/split layouts, all paged rows, changed account/financial state/form
while native confirmation waits, refused loan/asset targets, before-opening warning, mutated proposal, original
file-picker cancellation, no overwrite, reimport and durable Undo after a new view model.
24 new cases plus32 directly affected existing DataFileOutcomeFlowTests passed together; the final five added
edge cases passed separately after their related change. Earlier successful unrelated cases were not repeated.
23 resource completeness/usage checks passed. The historical3753 full-suite count is not a new full-suite run.

Native Windows review uses the independent bankcsvreview identity, original controls/Invoke/Toggle and genuine
native Cancel on a fictitious independent database. The first case detected incomplete fixture preparation:
lazy default settings were created after its financial baseline. Complete settings initialization precedes the
baseline now; the same database is retained. Original failed captures/error are preserved with before-fixture-fix
names. This is a fixture correction, not evidence that real bank rows were silently changed.

Final native, strict-build and signed-package evidence is appended after completion. Physical phone, actual bank
customer exports, iOS, bank/provider acceptance and publication remain separate; Windows is development evidence.


### Strict/native Windows and package evidence

All18 independent native Windows contexts completed: en/fa/de, light/dark, widths360/412/1280. Original
Invoke/Toggle/native Cancel retained valid review/consent and identical complete financial hash
202879bd6d604d915b466181e7d02ef177303862fb3e847beb4afffc39759593. Source calls are the explicit
fixture picker port; this is not a real native-file-dialog claim on Windows. Evidence:
artifacts/bank-csv-native-d297/complete.json and per-context proof/cancel-state/captures. No owner desktop input.

Strict Windows and Android Debug passed with0warnings/0errors. Build-AndroidApk.ps1 -Billing -BillingSandbox
completed its warning-rejecting Release publish. Complete APK:
artifacts/android-bank-csv-d297/pro.vafadar.zanance-Signed.apk;92582132bytes;
SHA2566356a13f900c08e8f3b44af57e514474a2c3f6c8c8129fff9213a964c103df80. ZIP CRC valid,
v2/v3 signature verified with local test certificate SHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. Permanent id
pro.vafadar.zanance, minAPI24/target36 and arm64+x64 packaging verified. This is not a production-store signature.


### Actual main Release Android observations

The exact existing emulator-5570 on dedicated adb5038 was upgraded in place, without resetting its protected
data/keys. A wrong activity command was refused before launch; the actual manifest activity was then used.
The original live process24201 eventually reached the original zero-balance Home without restart. Transient
null-root observation failures did not lead to stale XML reads or repeated application starts.

The actual Android DocumentsUI selected the independently owned fictitious Windows-1252 source. The Release
AOT native port decoded Muller with umlaut, displayed all three original row amounts/remittance/value-date/
category/comment, masked source4321 and original My account/EUR. Explicit posted-cashflow acknowledgment and
real native Cancel retained the preview; subsequent explicit native approval recorded exactly3, skipped0.
The original success and batch count were observed, not inferred from tests.

During the continuation, Android reported input-focus dispatch timeout5005ms (ANR time01:07:06, originalPID24201).
The original evidence is retained in artifacts/bank-csv-d297/lastanr.txt and native XML. Wait did not clear the
observed dialog; no second import or fixture reset was attempted. Native ANR detail files were not readable
without extra privileges; no root/system-setting workaround was used. The already planned cold persistence/
Undo stage then continued with the same committed data/keys. This is not a claim that the focus timeout was
fixed or that all main-thread lifetime/performance behavior passed. Final cold/reimport/Undo results follow.


The planned cold continuation usedPID25154 and the original committed source. Home showed3 unreviewed entries
and exact balance2320.99EUR. The original batch survived. Picking the UTF-8 version through DocumentsUI showed
new0/already imported3/invalid0 and disabled import, without a second write. Actual native batch Undo then
removed the three original fictitious entries; Home returned to the original0.00EUR and My account. This
qualifies durable import/idempotence/Undo, not a new financial import after the timeout. All original XML is
retained; no original customer bank export was involved.

### Focus-return worker correction

The observed timeout exposed synchronous bank preparation reachable on the UI thread: SQLite async methods
still execute synchronous native work, as can native file decoding, format validation and AOT serialization.
Strict native-file decoding, parsing and the coherent source read/mapping/proposal binding now run on a worker.
Capture exact source/account/epoch before dispatch; recheck retirement before publishing after both worker
awaits. UI translations/collections/consent remain on their original context. Original writer/Undo are unchanged.
29 directly affected BankCsvFlowTests passed after this scheduling change; the already passed32 legacy writer,
23 resource and18 layout cases were not pointlessly replayed. Final strict/native picker evidence follows.
The earlier signed package is retained under before-worker-fix; its hash above is historical, not the handoff.


### Final worker build/package

Final worker Windows and Android Debug builds passed with0warnings/0errors; the complete warning-rejecting
Release publish also succeeded. Handoff APK supersedes the historical pre-worker package:
artifacts/android-bank-csv-d297/pro.vafadar.zanance-Signed.apk;93033005bytes;
SHA2564b9e42bc8b6e0555819cf307cb55400bb72db04f92494888f06d94f61a88c0dd. ZIP CRC and v2/v3
signature passed again for this changed binary; same local test certificate/permanent id/min24/target36 and
arm64+x64 package. Existing financial data was upgraded in place, without another import or reset.


Final worker package ran asPID25624 on the original preserved data. The actual native Windows-1252 picker
returned a complete three-row preview; the form scrolled and native Back retired it successfully, without
another import. Home still showed original0.00EUR/My account. Android's last-ANR timestamp remained the original
01:07:06; no new ANR was observed in this focused changed-path check. This verifies the worker correction's
actual picker continuation, not all focus/lifetime/performance races or a physical phone. A transient adb pull
failed during initial observation; the original remote XML was retrieved afterward and the same process
continued. No application-data reset, extra privileges or system-setting changes were used.

The actual original import/reimport/cold Undo was not replayed after this read-only worker change. No new
financial write occurred in the final probe. Exact main-runtime-proof.json, apk-worker-proof.json, signature,
strict-build/focused-test logs and native XML are retained in artifacts/bank-csv-d297. Only this batch's exact
owned emulator observation XML/two fictitious source files were removed afterward; local evidence remains.
Release test outputs were cleaned after the affected tests. The owner archive and unrelated devices/files,
accounts, credentials, Docker resources and schedules were not touched. Full suite3753 remains historical;
61 distinct relevant flow cases and23 resource cases are not a newly executed whole-suite total.

Next ready approved substantial work is local AI consent/minimal-data/credit accounting, then FX provenance/
cache/manual fallback, while original bank-format corpora and genuine provider/Mac/store/device/release gates
remain separately visible. Do not replace those deliveries with repetitions of passed import, billing,
sync/household, cipher, Home or accountant verification.
