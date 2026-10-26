# Original StarMoney decoded-field boundary (D-571)

## Independent ready defect and honest reproduction

Continue approved CSV intake work while Loyan holds the physical phone. The original StarMoney
preset permits4096-character fields,10000 data rows,128 columns and an8MiB source. Review of the
current implementation initially suggested that limits were checked only after parsing. That
hypothesis was wrong: its existing ValidateSyntax preflight already enforces the limits before
building the table. Preserve the initial failed assertions; three wrongly demanded a new typed
exception where the established FormatException was already correct. No such API change is made.

The actual defect is that the preflight counts CSV syntax and separators as field content. A
permitted4096-character comment is refused when a delimiter follows it; surrounding/doubled quotes
also consume the wrong budget. Five corrected original-contract cases fail on unchanged production
source: plain text, quoted semicolons, escaped quotation marks, embedded CRLF and Persian text.
Five original-resource boundary/refusal cases already pass. This ten-case baseline, not the
incorrect initial exception-type expectation, establishes the real correction scope.

## Narrow correction and source preservation

Change only the preflight's decoded-character counter. Count ordinary text, one decoded character
per doubled quote, and embedded CR/LF characters; do not count separators or opening/closing quotes.
The original UTF-16 field-length unit remains unchanged. Keep all byte/record/column caps, strict
quote closure, complete-row validation, explicit encoding, original field contract, currency,
source IDs, posted-history acknowledgement and financial relationship refusals. Do not introduce
a guessed direct-bank preset, silently truncate a field or change financial rows. No schema/model,
compiled model, migration, permission, SDK, commercial quota, UI string or layout changes occur.

On actual runtime14, all10 corrected boundary cases pass. Two actual ImportExportViewModel flows
using isolated real SQLite and the original source interaction port accept full4096-character plain/
quoted comments, keep the complete review detail, leave acknowledgement off and create no ledger
entry. Five directly affected original unsupported-structure cases pass. These are12 distinct new
cases plus five existing cases; do not sum earlier retries or rejected assertions into the new count.
The D568 Home cases, full suite and successful native/financial cohorts are not replayed.

## Package and device boundaries

Strict Windows Release and complete signed offline Android builds succeed with zero warnings/errors.
The reviewed APK has SHA2564e79cc1d3388081a199d1da87e2ecd00f53af43c6cc287e699dcbf360921e67b,
384 own AOT modules/408 ELF files across both ABIs,1608 statically aligned load segments, valid ZIP
alignment/CRC and v2/v3 signatures, and no Internet/debug/profile declarations. All1422 Java resource
names/bytes match the retained healthy role; the four original native engines match qualified8fd5.
Current managed Core/Data identities are recorded separately and are not claimed equal to older8fd5.
This combined review APK includes unfinished D568; it is not a published-head or phone-adoption claim. No physical or D567-emulator command is used.
D568's Home body and all original source/financial receipts remain. This genuine new Core correction
makes its older frozen current-source admission historical: preserve those APKs/proofs and require
new matched current Core/Data inputs before the future phone pair. Do not waive a source mismatch
or present old timing packages as the current final product. The broad goal stays active; native
adoption, general stability/two-second performance/accessibility/platform/provider/Store/release
remain separate. Local evidence pointer: Zanance-D571-current.txt.

## Previously reported D561 CI: no historical failure replay

Read only the failed Libraries/tests job121523717527 from run40308573082. Its83 failure names and
multiplicities exactly match the original D562 packet:31 Data,52 application cases,35 method
families. All families have preserved selected passing XML evidence; no new failure name exists.
The published Data/comparator sources have no changes since qualified5e2e9384. Its original full
hosted CI/native-cipher/CodeQL success is already recorded, so do not rerun83 historical cases or
reapply a corrected trigger/oracle change. This is a historical correlation, not a new current-head
CI-success claim. D569's consumed response still reports CodeQL success and CI in_progress; its
monitor is deleted and no second exact-head status request occurs.

## Remaining original-bank contract admission

The next bank-format gate needs the actual institution/product/revision, complete CSV headers,
encoding/separators/date/sign/currency rules, posted status, source identity and fee/transfer/refund
semantics. Public export availability or JSON fields alone do not establish that CSV contract.
Only original institutional sources are used; third-party parser examples are not copied.

| Reviewed original source | Established scope | Still missing before a preset |
| --- | --- | --- |
| [N26 account reports](https://support.n26.com/en-eu/payments-transfers-and-withdrawals/balance-and-limits/how-to-get-bank-statement-n26) | Account/Space-specific web activity export over a selected period, with CSV/PDF alternatives | Complete current CSV field/encoding/date/identity/finality and relationship contract or an authorized original specimen |
| [Haspa export help](https://www.haspa.de/fi/home/login-online-banking/uebersicht-glossare.html?lightbox=%2Ffi%2Fhome%2Fonlinebanking%2Fumsaetze%2Fumsaetze%2Fglossar-umsaetze%3Fn%3Dtrue) | Distinct CSV-MT940 and CSV-CAMT routes; its CAMT section separates pending and booked exports | Exact CSV revision/headers/row-status/identity; do not substitute XML or assume one institution's export applies everywhere |
| [Wise balance statements,2026Q4](https://docs.wise.com/api-reference/balance-statement/balancestatementget) | Currency-selected statements with UTC period parameters; COMPACT and FLAT variants, the latter placing fees on separate rows | Actual CSV header/sample/locale/identity/fee linkage semantics; the displayed JSON schema is not a CSV sample |

For a future Wise file, fee rows cannot be silently deduplicated into principal or charged twice;
retain variant/provenance and require a complete source relationship contract. Reading public API
documentation does not authorize calling authenticated endpoints, handling credentials/SCA, opening
an account or activating a provider. No authenticated request, API token, bank account or customer's
transaction file is accessed. These three reviewed sources do not establish a new supported preset;
the existing generic explicit mapping and qualified software formats remain the current delivery.
This extends contract admission, not the completed fourteen-market discovery/ranking exercise.

## Owned-output cleanup

After terminal builds and package verification, standard clean removes 13162 registered files /
1263497093 logical bytes from the three owned D571 build/test contexts. Preserve the used signed
APK and local proof receipts. This is logical output accounting, not measured host free space.
