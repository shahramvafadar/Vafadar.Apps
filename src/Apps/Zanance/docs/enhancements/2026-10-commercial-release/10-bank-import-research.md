# Bank-file import research

Owner-approved research order: Germany, then USA, then the remaining approved markets. Review
widely used banks and actual current original export formats; the owner does not need to supply
the bank shortlist. This is file-import research, not authority for bank login, scraping, a live
bank connection, a paid provider or reading the owner's bank files. Date checked: 2026-10-15.

## Method and initial German priorities

Use primary bank/group publications for reach and original product documentation for formats.
Customer relationships, unique people, active banking users, accounts, assets and group/member
figures are different measures; they cannot be summed or used as one precise popularity ranking.
The table is an implementation-research priority assessment, not an exhaustive market-share table.

| Initial research group | Verified primary evidence | Consequence |
|---|---|---|
| Sparkassen | [Sparkasse's current consumer page](https://www.sparkasse.de/pk/produkte/sparen-und-anlegen/geld-anlegen/depot.html) reports over 50 million customers in Germany | First group priority; regional institutions and export versions need explicit coverage |
| Volksbanken/Raiffeisenbanken, including related cooperative brands | [BVR group profile](https://intern.bvr.de/Wer_wir_sind/Genossenschaftliche_FinanzGruppe) reports over 30 million customers and 17.5 million members | First group priority; membership is not another customer count; regional/export variants remain separate |
| ING Germany | [ING company facts](https://www.ing.de/ueber-uns/unternehmen/) reports over 10 million customers | Initial individual-bank priority; personal/business/depot formats are not interchangeable |
| DKB | [Original 2025-results release](https://www.dkb.de/presse/pressemitteilungen/pm-260305), published 2026-03-05, reports about 5.9 million customers at year-end 2025 | Initial individual-bank priority; scope to actual product/export version |
| Commerzbank/comdirect | [Original 2025-results release](https://www.commerzbank.de/konzern/newsroom/pressemitteilungen/bilanzpressekonferenz-fuer-2025.html) identifies the two German retail brands | Include in the first research set, without substituting mBank's foreign customer count for German reach |
| Deutsche Bank/Postbank | Large German retail-brand candidates; current comparable domestic reach and original export formats still need verification | Research queue, not a claimed numeric rank or implemented preset |

Then evaluate Sparda/PSD variants, N26, Revolut and other material German-market products using
country-specific evidence. A global fintech customer number does not establish German usage.

## What the original export evidence actually proves

- [Volksbank pur's original banking changes](https://www.volksbank-pur.de/banking-und-vertraege/banking/neuerungen-im-banking.html)
  describes CSV transaction export. This establishes an export route for that offering, not an
  exact universal cooperative-bank header/date/encoding contract.
- [ING's business-account FAQ](https://www.ing.de/business/geschaeftskonto/faq/) describes web CSV
  export for accounting. It is business-account evidence, not a personal-current-account format.
- [An original Sparkasse Erlangen export guide](https://www.sparkasse-erlangen.de/content/dam/myif/sksk-erlangen/work/download/fk/Hinweis_Checkliste_EKA_mit_qualifizierter_Signatur.pdf)
  distinguishes CSV-MT940 and CSV-CAMT choices. It is historical institution-specific evidence;
  do not present it as a current group-wide schema or silently treat CSV-CAMT as CAMT XML.

Current exact consumer CSV schemas, product distinctions, encodings, sign conventions, pending
versus booked records and stable transaction identifiers are not yet verified for the complete
shortlist. No new bank preset is implemented or advertised by this research. Third-party converters,
forum examples, company asset rankings and guessed headers are not authoritative specifications.

## Acceptance before a preset becomes supported

Record the bank/product/country/export revision and dated original source. Obtain a permissible
public example or independently sanitized fixture with exact header/encoding/date/decimal/sign
semantics; never log or commit real account numbers, references, counterparties or owner files.
Create fictional amounts/identities only after that format is verified. Test the actual importer:
preview/mapping, explicit source/target currency/account, booked/pending distinction, expenses,
refunds, transfers, duplicates/overlap review, transaction recheck and durable Undo. Preserve all
original financial invariants; a preset cannot guess transfers or automatically post a PDF total.
Unknown format revisions fall back to explicit mapping, never silent misclassification.

## USA and subsequent markets

Next research uses the [FDIC BankFind/SDI](https://banks.data.fdic.gov/bankfind-suite/financialreporting)
and [Federal Reserve large-bank tables](https://www.federalreserve.gov/releases/lbr/) as discovery
sources, with their measure/date stated. Assets/deposits are not retail-use counts. Verify current
institution/product export instructions directly before prioritizing implementation. No US bank
rank or preset is asserted yet.

Then repeat that source/format/fixture process for the existing approved country registry:
Austria, Belgium, France, Luxembourg, Monaco, Netherlands, Switzerland, United Kingdom, Ireland,
Canada and Australia, while Germany and USA lead the sequence. This is extensible and independent
of UI language. The later separate Iran variant remains separate. No country/bank roadmap date,
unverified-format support or live-connection availability is promised.


## D-297 original-format follow-through, checked 2026-10-17

The earlier absence of a qualified preset is historical. One explicit StarMoney Giro ordinary-cashflow
variant is now implemented; [exact delivery and gates](14-bank-csv-delivery.md) records original help/PDF,
required fields, refused split/storno revisions and user-confirmed posted-status/relationship boundary.
This does not establish the current direct CSV contract for every institution below.

| Current original evidence | Verified consequence, not broader support |
| --- | --- |
| [Haspa consumer export glossary](https://www.haspa.de/de/home/login-online-banking/uebersicht-glossare.html?lightbox=%2Fde%2Fhome%2Fonlinebanking%2Fumsaetze%2Fumsaetze%2Fglossar-umsaetze%3Fn%3Dtrue) | Distinct CSV-MT940, CSV-CAMT and XML/other export options; the label CAMT does not prove one interchangeable CSV schema |
| [SSKM format retirement](https://www.sskm.de/fi/home/produkte/banking-und-software/weitere-services/formatabkuendigung.html?n=true) | Current format revisions matter; this is not authority to implement out-of-scope CAMT/MT940 |
| [ING business FAQ](https://www.ing.de/business/geschaeftskonto/faq/) | Business-account CSV availability only; not a retail Giro field specification |
| [N26 statement/export instructions](https://support.n26.com/en-eu/payments-transfers-and-withdrawals/balance-and-limits/how-to-get-bank-statement-n26) | Web-app CSV and separate Spaces exports exist; exact stable ids/column/relationship semantics still need original specimens |

### USA discovery and format qualification

The [Federal Reserve large-bank table](https://www.federalreserve.gov/releases/lbr/current/default.htm)
is dated30 June2026 and was posted2 October2026. Consolidated-asset ranks1-4 are JPMorgan Chase, Bank of America,
Citibank and Wells Fargo; U.S.Bank/Capital One/PNC are also discovery candidates. This is an asset-ranked shortlist,
not a ranking of active retail users or permission to scrape accounts. No current retail-use count is inferred.

[Wells Fargo's original Account Activity FAQ](https://www.wellsfargo.com/help/online-banking/activity-faqs/)
describes per-account comma-delimited downloads with explicit date ranges. It separately explains pending vs
posted balances/amounts. It does not establish all exact CSV columns, stable transaction ids, currency and
relationship rules needed by a safe preset; do not copy a third-party converter's schema as bank authority.
Chase commercial documentation was found but is not substituted for its consumer product. Bank of America,
Citibank and the other candidates still require current original product/format qualification.

After Germany/USA, retain Austria, Belgium, France, Liechtenstein, Luxembourg, Monaco, Netherlands, Switzerland,
United Kingdom, Ireland, Canada and Australia in the individually extensible research queue without ETA. Iran
remains the later separate variant. Exact customer-export corpora are an external evidence gap, not a reason
to replay successful unrelated engineering or advertise unsupported formats. Live-bank provider/licence/cost,
consent and payment/account linking remain separate owner/provider gates.
