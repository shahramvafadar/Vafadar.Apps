# German consumer bank export admission update (D-706)

Checked31October2026 using public original bank documentation only. This follows
D-307's exact product/revision/specimen gate. It adds no preset, statement parser,
live connection or advertised bank support. Existing StarMoney/SUPA4 and generic
explicit mapping remain unchanged; accepted import/Undo/financial tests are not rerun.

## New original evidence and engineering consequence

| Original source | Verified boundary | Consequence for the next parser |
|---|---|---|
| [ING personal Online-Banking FAQ](https://www.ing.de/hilfe/internetbanking/kundenservice/) | The personal account activity page can be exported; the described displayed history reaches390days. Its balance can include earmarked/pending payments before final posting. | This establishes a personal-account export route independently of the older business-only evidence. It does not publish exact CSV columns/encoding, stable IDs or say whether exported rows include pending activity. Do not treat available balance or the presence of a date as finality. The390day display statement is not proof of a complete390day file export. |
| [N26 original EU statement help](https://support.n26.com/en-eu/payments-transfers-and-withdrawals/balance-and-limits/how-to-get-bank-statement-n26) | Monthly balance statements are PDF; separately, date-selected account activity reports are PDF/CSV via the web interface for the chosen account or Space, including joint-account choices in the guide. | Model product/account/Space and document kind explicitly. Do not parse monthly PDF as CSV or assume a CSV joins all Spaces. The page supplies no revisioned field specimen, debit/credit encoding, stable ID or transfer/refund relationship contract. |
| [Sparkasse Gummersbach original export guide](https://www.sparkasse-gm.de/content/dam/myif/sk-gm/work/dokumente/pdf/allgemein/Anleitung_Export_Umsatzdaten_IF.pdf) | The rendered one-page guide distinguishes CSV-CAMT, CSV-MT940, CAMT posted activity and MT940 options. Its illustrated activity is dated2020. | These menu labels identify different export choices, not interchangeable schemas. A dated screenshot is no current CSV field contract. No XML/MT940 parser is admitted or bank authentication performed. |
| [DKB original FAQ location](https://www.dkb.de/fragen-antworten/was-kann-das-banking) and [actual redirect destination](https://www.dkb.de/lp/dein-neues-banking) | Search-index text at the old FAQ describes an unsupported securities-activity CSV feature, but opening that URL now redirects to a general web-banking page without that contract. | Preserve the discrepancy. Do not promote the cached negative feature list to a verified-current claim that every DKB CSV export is unavailable, or infer the checking-account schema from securities. Current product-specific export evidence remains missing. |

The ING/N26 statements above are narrow paraphrases of the inspected public pages,
not claims about an authenticated current export or the owner's bank accounts.
No login, credential, owner statement, bank data, third-party schema/sample, subscription
or provider action is used. Search hits from import applications and community CSV
repositories are discovery only and are not adopted as bank-authored fixtures.

## Source limitations retained

The Postbank consumer route was already established in D-307 and rechecked in the
preceding local preflight; it still does not supply a complete original field/finality/
ID specimen. That successful route discovery is not a new preset or another import test.
The inspected Sparkasse template glossary is a different payment-template product;
its indexed header is not an account-transaction contract. Direct retrieval of that
specific glossary fails, so no new schema fact depends on it. The current DKB redirect
is stronger evidence of the retrieved document than a stale indexed snippet.

## Exact missing input and bounded next implementation

For one concrete bank/product/revision, retain a permissible original structural
specimen with encoding/BOM/delimiter/quoting/preamble/header/record boundaries and an
original explanation of amount currency/sign, posting/value dates, pending/final
status, stable IDs, overlap and own-account/refund/reversal relationships. If a source
omits relationship metadata, keep explicit review and refuse unsupported automatic
relationship reconstruction. A balance cannot replace independent rows.

Then add only that exact parser to the existing bounded source/preview/explicit-consent/
original-writer/durable-Undo pipeline, with fictitious structural fixtures and focused
new checks. No guessed bank-wide preset is ready from these pages. No additional
format beyond the approved CSV scope is silently admitted. Bank login or lawful owner
sample acquisition is a separate actual input, not a reason to repeat old financial
fixtures or wait instead of doing other ready work.

## Delivery scope

Documentation/source-admission only: application code, strings, financial policy,
schema, dependencies, signed shipping candidate and product-test counts are unchanged.
Local links and staged diff are checked; no build/native/device/financial/old-suite run
is justified by this documentation-only change. D705's prepared candidate and sole CI
follow-up remain separate. D607's three deadlines, consumed reboot and unread source,
physical adoption and all other outstanding platform/security/stability/performance/
accessibility/provider/Store/release gates remain open. The delivery goal stays active.
