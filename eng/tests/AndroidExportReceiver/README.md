# Isolated physical export receiver

## Independent portable backup grant

`PortableBackupActivity` is a separate bounded receiver for the original full application compiled as
`pro.vafadar.zanance.portablerestorereview`. It accepts only ACTION_SEND/application/octet-stream with
a read grant from that exact FileProvider, a product `.vbak` filename and a plain manifest/data ZIP
bounded to 8 MiB compressed and expanded. It refuses the permanent Main authority and encrypted packets.
It writes only its own private file plus `portable-status.json`; inspect/copy only those explicitly
received fixture bytes after matching receipt length/hash. No password, source-private file, key,
network, arbitrary ZIP extraction or forwarding is involved. Start once without a grant to prove
refusal, then observe the same process/result. Preserve completed shares and never repeat creation
just because the observer is delayed. See `../AndroidPortableRestore/README.md` for the full reinstall
workflow and original-candidate restoration boundary.

This independent .NET Android fixture verifies the original native sharing grant and received fictitious
accountant ZIP or report PDF. It is not referenced by the application and must never become a production dependency.
The intentionally debuggable receiver has no declared network permissions or backup, removes inherited
AppSecret items and writes only its own private test storage. Minimum API33, ARM64, no MAUI dependency.

## Build and package

Use the repository's installed official .NET Android SDK and existing local test signing setup:

```powershell
dotnet build eng/tests/AndroidExportReceiver/AndroidExportReceiver.csproj -c Release -t:SignAndroidPackage -p:ContinuousIntegrationBuild=true
```

The complete signed local fixture is under bin/Release/net10.0-android/android-arm64. Verify its application
ID pro.vafadar.zanance.exportreview, signature and absence of network permissions before installing it.
Never confuse it with the full signed production candidate. Build/restore can consume existing local
configuration; do not inspect secret files or generated secret values. No global installation is required.

## Approved physical turn

Coordinate exclusive use of the authorized test phone. Confirm that this exact fixture package is absent
before a fresh installation; never clear an existing unknown fixture. Preserve main application data,
system settings and authentication. Select only fictitious profiles. Never inspect the main private
store, key files or FileProvider files through a debugging bypass.

First launch this fixture explicitly with ACTION_SEND/application/zip but no URI grant. Its private
files/status.json must report Received=false/InvalidOperationException; an immediate missing status may
mean startup has not finished. Observe the same invocation rather than relaunching it after a timeout.

In the original app, choose Germany/year2026 and the D-312 fictitious accounts. Keep notes/payees and
stored documents off. Check empty selection refusal and confirmation Cancel. Explicitly confirm the
reviewed export and choose only Zanance export review in Android's native share sheet (More if necessary).
Capture the generated accountant filename from that same chooser. The receiver finishes automatically;
read only its own files/status.json through adb exec-out run-as pro.vafadar.zanance.exportreview.
Require Received=true, ReadGrant=true and the exact observed filename. Copy the corresponding
files/received-<32-hex-token>.zip from that same owned fixture with binary-safe exec-out, verifying its
reported byte length and SHA256. Do not obtain files from the main package. No contacts or network
recipient should receive either packet.

Repeat only the distinct cash-only disclosure: changing selection invalidates the review; review and
confirm the one transfer/one additional linked identity. Match the second chooser filename to a fresh
receiver receipt. The receiver refuses overwriting a packet token and caps compressed/expanded data at
2 MiB. Its default branch accepts exactly seven known non-document members. D-380 adds only the exact known
fictitious PDF branch described below. It does not extract, execute or forward members and is not a
general ZIP or document viewer.

## Original-packet verification

```powershell
python eng/tests/AndroidExportReceiver/verify_packets.py <all-accounts.zip> <cash-only.zip>
```

The standard-library verifier checks the original D-312 fixture (five known amounts, two account names,
refund reference, grouped fee and one transfer), manifest digests, disclosure flags, account/category
references, blank tax classifications and cash-only exact-row equality. It does not implement the exporter,
open the database or calculate tax. It is fixture-specific; using it on other financial data is unsupported.

Return to the original Home, confirm the original displayed balance and uninstall only this fresh receiver.
Remove only the review's own temporary UI hierarchy files. Release the coordinated device turn. Keep local
fictitious evidence outside Git; never upload private artifacts or change main offered-cache expiry.
See src/Apps/Zanance/docs/quality/physical-accountant-export.md for D-328 results and remaining boundaries.

## Original report PDFs (D-329)

The receiver also accepts ACTION_SEND/application/pdf from the same main FileProvider, with the exact
zanance-report-<32-hex-token>.pdf filename and a %PDF- signature, under the same 2 MiB read limit.
It never renders or executes PDF content on the device; use local PDF extraction and page rendering after
the original byte-length/hash match. MediaType in the receipt records the actual received native type.
Run the explicit no-grant check first after installing a fresh receiver and let its own activity finish;
do not bring another task forward during its first runtime initialization.

Samsung's expanded More app grid can leave the uncompressed hierarchy showing the underlying chooser.
Use a fresh `uiautomator dump --compressed` and the current focused chooser window, and require the
observed exact Zanance export review target before selection. Never reuse underlying-grid coordinates.
Refuse further input when a different package is foreground; bring only the original main task forward.
Do not enumerate direct-share contact suggestions or retain outside-app content.


## Rendered English report fixture verification

Use the bundled Python runtime with pypdf/Pillow and the already installed Poppler pdftoppm on PATH:

```powershell
python eng/tests/AndroidExportReceiver/verify_report_pages.py <original-two-page-English-D312-report.pdf>
```

This fixture-specific check rejects a continued cash row without its account/scope/headings, retains the
known original figures and renders both pages to verify actual footer pixels independently of extracted
text. It does not prove all document layout or legal correctness. D-329 also visually inspects the original
Persian/German exports. Temporary render files are removed when the verifier exits.


## Original ledger CSV roundtrip (D-330)

The receiver accepts text/csv only for zanance-export-YYYYMMDD-HHMM.csv from the original main FileProvider,
with a native read grant, strict UTF-8, the original ledger header and the existing2MiB bound. It neither
parses financial relationships nor executes/forwards CSV. Receipt MediaType, original filename/length/hash
must match before copying only this fixture's own private files. Same-minute tokens refuse overwrite.

Stage the exact received fictitious file only at a previously absent owned Downloads path. Create a fresh
profile with the two exact fixture account names and opening dates no later than17October2026; keep both
zero and included in totals. Use original picker/preview/import, original re-export, same-file duplicate
refusal and a real process-cold Undo under verified current-profile identity. Retire only that new profile,
require the external file's exact original hash before deletion and uninstall only the fresh receiver.
Notes inclusion is the main CSV's existing on default; this fixture has no nonempty notes.

```powershell
python eng/tests/AndroidExportReceiver/verify_csv_roundtrip.py <original.csv> <target-reexport.csv>
```

The standard-library verifier checks every27-column value against five stable IDs, known fixture money,
refund and transfer/fee relationships. Equal-date order may differ. This proves exported-field equality,
not database/key equality, a complete backup, arbitrary bank format or malformed-file acceptance.
See src/Apps/Zanance/docs/quality/physical-csv-roundtrip.md for the original native evidence.


## Original notes and known document disclosure (D-380)

Version0.4 additionally permits documents.csv and exactly one documents/<32-hex>.pdf member only when
its13881 bytes have SHA256 6f6dcffa10e283897f71d7892733bb2632986810346153aa78f728b2d86a6919.
All original URI/grant/envelope/size/overwrite restrictions remain. This accepts only the known synthetic
English receipt, not arbitrary attachments. Build/package verification and exclusive phone coordination
remain mandatory; do not inherit an unknown existing receiver installation.

Use a newly created fictitious profile, one explicitly reviewed24.50 EUR expense and controlled fictitious
payee/note. Export Germany2026 once with both disclosure flags enabled, then disable both, require a new
review and share only to this receiver again. Copy only its own received files after receipt/hash matching.

```powershell
python eng/tests/AndroidExportReceiver/verify_document_disclosure.py <disclosed.zip> <redacted.zip> <original-fictitious.pdf>
```

The standard-library verifier checks original financial-field equality, explicit redaction, manifest
coverage and byte-exact document/entry linkage. It does not calculate tax or prove original legal records.
Retire only the new profile through original confirmations; restore the original public profile list/Home,
verify unchanged Main/OS state, uninstall only the fresh receiver, delete only hash-matched owned input
and release the phone. Require IME dismissal before non-editor actions; inspect retained navigation state
instead of replaying Save or Back. See physical-accountant-document-disclosure.md in the quality docs.
