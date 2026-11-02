# D-780 - Printed date-field merchant boundary

Subsequent D783 resolves only the retained joined-total amount failure using the exact same native OCR text.
The D780 results below remain historical; see [the later correction](receipt-joined-total-unit.md).

## Reproduced defect and exact scope

2 November2026. Continue independent Android development while D752 awaits the owner.
The proposed original native-callsite question is already covered by D757; do not
repeat that decode or manufacture another crash. Inspect the remaining receipt parser
boundary instead. Current FindMerchant recognizes complete unlabelled dates but lets
explicit printed fields such as Datum:28.10. become the merchant when a header is absent.
The initial item-row-fallback expectation is not the selected labelled baseline's
actual result: its14 failing cases return the date-field row itself as the merchant.
Keep the actual failures rather than changing their description to match that assumption.

Add explicit case-insensitive colon-labelled Date/Datum/Fecha/Data/تاریخ recognition
only inside the merchant boundary. Complete dates use the existing strict validation.
A labelled yearless row may contain bounded day1..31/month1..12 or a month name from
the existing six supported cultures. This identifies metadata, not a valid calendar
date: no purchase year or partial-date suggestion is manufactured. Existing FindDate,
FindTotal, currencies/precision, amount evidence, source rows and explicit Save remain
unchanged. Bare names such as28OCT, DATE PALM MARKET and unsupported label/value shapes
keep their original behavior. This is finite supported-source grammar, not universal
merchant recognition, missing-colon repair or a complete receipt corpus.

## Focused tests and strict builds

The first direct-DLL command uses unsupported --filter-class and exits before tests.
Inspect the actual xUnit in-process4.0.1 help and correct only that invocation to its
-class/-method syntax. Keep this CLI refusal separately; it is not a baseline test
failure. Verify exact selected console totals/errors/failures/skips/not-run counts.
No XML/TRX result is claimed where the requested report file was not emitted.

The new23-case class fails14 cases on original production code and passes9 header
controls. After the parser change, all23 pass. Fourteen affected existing full-date/
timestamp boundary cases pass separately. A later additional regression uses the
actual first native OCR text, asserting NotFound rather than borrowing an item price;
only that new case runs on its subsequent strict test assembly. Thus24 new retained
cases and14 affected existing cases pass in selected runs,38 distinct cases; this is
not a full-suite result or a claim that all38 ran on one final test binary. No already
successful case repeats merely for a report or after adding that last test-only case.

The initial Windows invocation restores unwanted iOS targets against the Android-only
notification project and refuses with NU1201. Correct only explicit Windows framework/
RID command properties; the strict Windows build then has0 warnings/errors. No package,
notification source, SDK or iOS setting is changed. The first private native build
refuses nullable OCR Text in its result-only DTO (CS8604); make that private observation
nullable, without changing product behavior. Subsequent native builds are strict.

The complete signed offline shipping APK is99458975bytes, SHA256
`7e014834bd9a292a4262f4082f522a89c6c24c203e075cf592dd9f0a8785118d`,
384AOT/408ELF/1608 static16KiB LOAD segments. It remains at
`E:\Zanance.ReviewBuilds\D780-shipping-package\pro.vafadar.zanance-Signed.apk`.
ZIP CRC, zipalign16KiB, v2/v3 signatures, nondebuggable and no INTERNET pass. It is
not installed on Main or promoted as release acceptance. The used native packages are
edf2222133d542b0040145cf5de23b7b9ad9aae95d3e6effc8990cdeef422842 and
fba603e04c6e4df6245bd0794a64c52f55a382221e3837e0136a0a0d0de7356c,
56958959/56983619bytes and192AOT/204ELF/804 static LOAD each, with the same policy
checks. Original x64 Mono remains edfa02c3; no runtime/GC change or crash repair claim.
Static16KiB alignment does not prove hardware16KiB execution.

## Actual native OCR, including the first partial result

Fresh owned API36/x64/ranchu/4096 emulator Zanance_D780_ReceiptLabel, port5584,
boot ee786f69-ead6-43bd-b682-59f8c6673e85 and UID10216 use the original covered startup.
One fresh install/normal launch creates4860. An API24-style userId metadata assumption
refuses after installation because API36 reports appId; read the same installation's
pm-U UID and issue only the still-unissued launch. Never reinstall to repair observation.
Wait for original Onboarding, capture20typed collections, and read one new owned16303byte
PNG through the actual original DocumentReader/native OCR and changed ReceiptParser.

Actual first text is Date:28/10, BREAD EUR7.10 and TOTALEUR7.10 (the raw receipt stores
its exact spaces/lines). Merchant/date are correctly null. OCR joins the total keyword
and unit; original amount rules correctly return NotFound/no candidate. The intended
7.10 amount acceptance therefore fails. Preserve that real partial outcome; do not
repair OCR text, guess from the item price, rerun the source or call it full acceptance.
The additional new unit regression retains that exact native text and behavior.

Prepare a distinct17732byte German source with Datum:28.10., BROT EUR8.20 and a clearly
separated SUMME:EUR8.20. Only private image/control preparation changes; the product Core
and already successful shipping build are not rebuilt. One necessary no-reset private
upgrade preserves boot/UID and the full20typed source, then launches5219. Read-only
adoption confirms the English-consumed marker remains true and German false: no source,
profile, account, OCR success or financial setup is replayed. Read only the new German
source once. Actual output is Datum:28.10./BROT EUR8.20/SUMME:EUR8.20, unambiguous, with
Merchant=null/Date=null/Amount8.20/Found/one candidate/EUR820minor. Both distinct source
markers are consumed at the end.

This is native original OCR-plus-parser acceptance, not camera/file-picker/editor,
user-authentication, saved-entry or full receipt-session acceptance. No screenshot
privacy flag, layout, language/theme, permission, clock or system setting changes.
There is no need to repeat unchanged layout/TalkBack matrices. All five original OS
values, geometry, boot and reverse mappings match. All20typed collections match before,
after first read, after read-only adoption and at the end; account/ledger/Save0. This is
not every private database byte or key. No physical-phone/D752 command occurs.

## Retirement and exact cleanup refusal

Uninstall only the previously absent fresh package and confirm absence. No XML is
created. Launcher55928/QEMU74248 exit and port5584 closes. The official scoped manager
deletes the fresh AVD (25files/4664145230logicalbytes); logical size is not measured host
free space. Shared ADB5038/PID66176 stays alive. Five standard project Clean commands
(focused/Windows/shipping/native/native-German) succeed. Used signed APKs, input images,
private source and all bounded receipts stay outside supplemental cleanup targets.

One supplementary deletion is rejected before process creation with blocked by policy:
13 exact scopes,4669files/932506970logicalbytes. Freeze every scope below without retry,
subset, alternate method, move, escalation or reuse. Signed packages are retained;
only their unused unsigned/designer companions were proposed for deletion.

| Exact frozen scope | Files | Logical bytes |
| --- | ---: | ---: |
| `D:\_Projects\Vafadar.Apps\artifacts\d780-native-receipt-label` |6|95539|
| `E:\Zanance.ReviewBuilds\D780-focused-tests` |84|1643698|
| `E:\Zanance.ReviewBuilds\D780-windows-build` |454|116720306|
| `E:\Zanance.ReviewBuilds\D780-shipping-build` |2181|340610863|
| `E:\Zanance.ReviewBuilds\D780-native-build` |966|129134922|
| `E:\Zanance.ReviewBuilds\D780-native-german-build` |966|129153900|
| `E:\Zanance.ReviewBuilds\D780-owned-avd` |6|7732|
| `E:\Zanance.ReviewBuilds\D780-shipping-package\pro.vafadar.zanance.apk` |1|99295598|
| `E:\Zanance.ReviewBuilds\D780-shipping-package\_Microsoft.Android.Resource.Designer.dll` |1|732160|
| `E:\Zanance.ReviewBuilds\D780-native-package\pro.vafadar.zanance.receiptlabelreview.apk` |1|56811572|
| `E:\Zanance.ReviewBuilds\D780-native-package\_Microsoft.Android.Resource.Designer.dll` |1|732160|
| `E:\Zanance.ReviewBuilds\D780-native-german-package\pro.vafadar.zanance.receiptlabelreview.apk` |1|56836360|
| `E:\Zanance.ReviewBuilds\D780-native-german-package\_Microsoft.Android.Resource.Designer.dll` |1|732160|

No old frozen scope or protected owner file is touched. No new product data, permission,
export, SDK, persistence model, migration or privacy policy is introduced. The original
SGen fault, performance/two-second target and broader security/accessibility/platform/
provider/Store/release gates remain open. D752 still waits for owner presence;
D749/D751/D607 remain terminal and the overall independent goal stays active.
