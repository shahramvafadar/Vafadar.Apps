# D-307 target-market bank and export qualification

Checked 2026-10-17. This completes the first source-based discovery pass across the fourteen current target
markets, following Germany, USA, then the remaining registry. It does not complete bank import support or live
connections. The previously qualified D-297 StarMoney ordinary-cashflow implementation remains the only added
preset; no runtime code, package, account, credential, bank login or paid service changes in this delivery.

## Findings that change implementation priorities

Prefer a documented consumer transaction export over a larger institution's undocumented, corporate-only or
statement-only route. Bank reach, systemic importance, assets and actual retail usage remain different measures.
The groups below are engineering research candidates, not a numerical popularity ranking or a recommendation to
open an account. Country and institution coverage never imply a uniform product schema.

The German customer-reach evidence and original Wells Fargo/US asset discovery remain in the dated
[initial research](10-bank-import-research.md). This pass does not rerun those completed investigations or
silently refresh their numbers. New direct German consumer evidence now establishes Postbank's CSV route.
AIB and KBC document consumer CSV exports. In contrast, ABN AMRO's consumer page lists TXT/XLS and banking
formats rather than CSV; NatWest explicitly retires midata and documents Excel/PDF. These differences prevent
shipping a guessed universal European CSV preset.

## Country discovery and qualification order

Every row is a separate country qualification, independent of language, currency and holiday coverage. The
country set matches the fourteen reviewed target entries in Core/Plans/HolidayCalendars.cs; the old Iran
calendar and the later separate Iran product are not added to this delivery. Additional countries have no ETA.
The first candidate in each row is a research priority inferred from original documentation and practical
consumer-file availability. Other named candidates are a follow-up set, not verified export support.

| Country | Initial candidate set and basis | Current implementation consequence |
| --- | --- | --- |
| DE Germany | Sparkassen; cooperative banks; ING; DKB; Commerzbank/comdirect; Deutsche Bank/Postbank. Existing D-297 reach/source research, plus [Postbank consumer CSV][F01] | Prioritize exact Postbank, Sparkasse institution/revision and cooperative consumer specimens. Do not reuse StarMoney fields for direct-bank CSV. ING business evidence does not qualify personal Giro. |
| US United States | Existing D-297 Federal Reserve asset discovery: Chase, Bank of America, Citibank, Wells Fargo; then U.S. Bank, Capital One, PNC | Retain Wells Fargo per-account download discovery. [Bank of America My Financial Picture][F02] is a separate aggregated product; its download instructions do not establish the direct checking CSV schema. |
| AT Austria | Erste/Sparkassen first because original George export documentation is available; Raiffeisen and Bank Austria next, also identified in [official systemic-institution rules][R01] | [George Business][F03] has configurable CSV/Excel/JSON and optional batch details; qualify separately from [consumer George statements][F04]. No universal fixed George header is inferred. |
| BE Belgium | KBC first for documented consumer files; BNP Paribas Fortis, Belfius, ING Belgium next. [NBB systemic discovery][R02] is not retail share | [KBC Touch][F05] describes CSV statements/search results, but also contains stale future-feature text. Confirm current UI/revision and complete original schema before a preset. |
| FR France | BNP Paribas first for original consumer export instructions; then Credit Agricole, Societe Generale, BPCE, Credit Mutuel and La Banque Postale through [ACPR discovery][R03] | [BNP consumer guide][F06] establishes account-level operation download only; it does not specify a machine-readable format or field contract. Regional cooperative variants remain separate. |
| LI Liechtenstein | LLB first for original export help; LGT and VP Bank next. [FMA historical systemic disclosures][R04] are discovery, not a current popularity rank | [LLB FAQ][F07] permits CSV/XLSX on certain pages in its Assets section. This is not proof that a current-account posted-transaction page exports the same schema. |
| LU Luxembourg | Spuerkeess, BGL BNP Paribas, BIL. All appear in [CSSF Regulation 25-05][R05]; exclude wholesale/custody-only entities from a retail shortlist | [BGL consumer e-documents][F08] proves PDF documents; [Spuerkeess help][F09] locates statements. Neither establishes consumer CSV columns. Do not substitute corporate MultiLine formats. |
| MC Monaco | CMB Monaco and CFM Indosuez first; then relevant local retail branches. [AMAF membership][R06] establishes institution presence, not retail market share | No exact current consumer machine-readable transaction contract was established. Keep the country visible in research with an explicit evidence gap; do not inherit a French/Swiss parent's format. |
| NL Netherlands | ABN AMRO for public consumer export instructions; ING and Rabobank next; ASN subsequently. [DNB systemic discovery][R07] supports discovery, not user counts | [ABN consumer guide][F10] lists PDF/TXT/MT940/XLS/camt.053 and an 18-month transaction window. No CSV claim or implied authorization for new TXT/XLS/CAMT parsers. |
| CH Switzerland | UBS, Raiffeisen, PostFinance, ZKB are the four in [SNB's current glossary][R08] | [UBS Swiss document help][F11] locates documents, not a complete current consumer CSV contract. Old German UBS wealth documentation and corporate KeyPort/CAMT are not substitutes. |
| GB United Kingdom | NatWest and Lloyds first for public consumer instructions; Barclays, HSBC, Santander UK and Nationwide next using [BoE discovery][R09] | [NatWest][F12] states midata is no longer offered and describes Excel/PDF. [Lloyds consumer download help][F13] does not prove CSV; its commercial CSV specification is a different product. |
| IE Ireland | AIB first for consumer CSV; Bank of Ireland and PTSB next using [Central Bank of Ireland discovery][R10] | [AIB Historical Transactions][F14] explicitly exports selected current/card-account results as CSV. Preserve account/product/date selection; do not equate the displayed two-year history with a guaranteed complete export. |
| CA Canada | TD first for public export help; then RBC, BMO, Scotiabank, CIBC and National Bank. [OSFI's six D-SIBs][R11] are systemic discovery, not usage ranking | [TD's hosted EasyWeb answer][F15] lists spreadsheet CSV and a maximum 45-day download window. It is version-sensitive hosted help, not a current header/credit-card contract. RBC's old software-help URL failed to retrieve. |
| AU Australia | Commonwealth Bank, NAB, ANZ, Westpac; [APRA register][R12] establishes supervised candidates, not retail-use ranks | [NAB consumer history][F16] confirms spreadsheet/QIF/PDF export and 731-day history; spreadsheet does not prove CSV. [CBA's indexed FAQ][F17] reports CSV but direct retrieval returned only a support shell. Treat exact format/limits as unresolved. |

The ordering within the later markets is a research scheduling choice, not a change to the owner's Germany-first
product rollout. No unsupported bank/country should be advertised as working. An unsuccessful retrieval means
unverified evidence, not that the bank has no export function.

## Exact export boundaries and remaining admissible evidence

The next parser is ready only when one actual country/bank/product/revision passes all of these source checks.
The existing D-297 importer already supplies bounds, explicit review, stale-consent refusal and durable Undo;
reuse that pipeline instead of creating a second financial writer or generic relationship heuristics.

| Evidence boundary | Required original evidence before implementation | Current concrete gap |
| --- | --- | --- |
| Product and revision | Consumer current account versus card/savings/business; bank/local subsidiary; dated original guide and permissible structural sample | Export buttons usually do not publish a revisioned header specimen. Parent/group brand is insufficient. |
| Encoding and record shape | Exact delimiter, quoting, BOM/encoding, preamble/footer/header, optional/reordered columns and multiline narrative | George Business explicitly permits field order, delimiter and batch-detail choices. Never assume one layout from a screenshot. |
| Amount and time | Explicit account currency and debit/credit semantics, date format, booking versus value date, credit-card payment and FX behavior | Displayed minus signs and a bank's default domestic currency cannot prove transaction kind/currency. |
| Finality and relationships | Posted versus pending, reversals/refunds, own-account transfers, batch parent/details and stable original identifiers | A pending display or aggregate export is not an independently bookable ledger entry. No largest-number/sign/name guess. |
| Completeness and overlap | Date/search/export limits, account scope, repeated ranges and stable ids; explicit overlap/keep-both decisions | TD's documented window and CBA's unresolved indexed cap require segmented completeness evidence, not silent truncation. |
| Admissible fixtures | Public bank specimen or independently sanitized, lawfully supplied sample; fictitious ids/payees/amounts; no owner account/file access | None of the new rows supplies all required schema/finality/id semantics. No new preset is qualified by this research. |

Near-term specimen priority is DE Postbank and institution-specific Sparkasse/cooperative exports, then the
already identified US Wells Fargo consumer product. AIB/KBC are the first later-market CSV specimen candidates.
Other formats stay explicit follow-up proposals; CSV approval does not quietly expand to PDF scraping,
OFX/QIF/CAMT/MT940 or spreadsheet conversion. Keep unreadable/unqualified originals available for explicit generic
mapping where that original format is already supported, without silently guessing or editing the source file.

## Provider evaluation without a provider selection

This is a source-qualified comparison for a later owner/provider decision. No SDK, API key, bank sign-in,
provider contact, contract, fee, account aggregation or live financial HTTP was added. Public marketing counts
are not evidence of one institution/product being available to this merchant in production.

| Candidate | What its current original source establishes | Evaluation outcome and unresolved gate |
| --- | --- | --- |
| Tink, Europe | [Account aggregation][P01] describes European aggregation and market-capability discovery | Candidate for Germany/Europe only after exact bank/product/data coverage, delegated regulatory role, allowed purpose, retention/deletion, outage behavior and a written quote. Payment-initiation country coverage is not account-data coverage. No universal CH/LI/MC/LU claim. |
| Plaid, USA/Canada and selected Europe | [Product/country matrix][P02] lists Transactions for US/CA and directs European institution/product checks to its coverage explorer | Shortlist for US/CA evaluation. A country entry is not production approval, full history or coverage of every card/account. Do not claim the remaining small Western European markets from an unrelated product list. |
| TrueLayer Data | [Data v1 overview][P03] directs exact supported countries/providers to Console and distinguishes regulated/non-regulated consent flows | Public overview alone does not close current coverage or commercial access. Do not log in/create an account or select it from historical coverage articles. |
| Basiq, Australia | [Supported access models][P04] describes principal/representative and other models; [CDR register][P05] identifies the accredited provider | Australian candidate, not an exemption for Zanance. Merchant legal role, principal agreement, actual data scope, consent/revocation/deletion and costs need separate acceptance. No scraping fallback is selected. |

A later connection must remain optional and read-only, disclose its actual recipient and scope, keep bank-login credentials
out of the app/backend and portable exports, and preserve provenance plus explicit review before financial
posting. Provider pending-to-posted changes, disappearing authorizations, amended amounts, deletions, repeated
callbacks and reconnection are not new independent expenses by default. The existing sync service is not a bank
provider or permission to receive bank credentials. These are qualification requirements, not legal advice,
regulatory approval or an implemented connector.

## Evidence quality and verification

Original pages were read in this pass; bank login and customer data were never accessed. Search-index content
was treated as discovery where direct retrieval failed or differed: CBA returned a generic support shell;
comdirect and RBC old help had retrieval errors; KBC contained conflicting old roadmap wording. No converter,
forum, screenshot-only header, global fintech headcount or commercial-bank guide was promoted into a consumer
schema. FMA's visible historical dates remain historical; no current buffer/rank is inferred from them.

The fourteen country codes, links, local relative targets, status consistency, unchanged code/package paths and
whitespace are checked. This documentation-only batch adds no application behavior or test case. Successful
D-306 builds/tests/native checks and its signed APK are retained; they are not rerun or attributed to D-307.
Its pending hosted result belongs to its original one-use check. BANK-01/IMP-01 remain In progress for exact
specimens/import qualification and any later separately approved live connector; physical/iOS/public release
and other ready engineering are not replaced by research or by repeated speculative format searches.

## Original sources

[F01]: https://www.postbank.de/privatkunden/services/online-banking/anleitungen/umsaetze-exportieren.html
[F02]: https://www.bankofamerica.com/online-banking/online-financial-management-faqs/
[F03]: https://www.sparkasse.at/de/george-help/george-business/transaktionen-und-suche/drucken-und-exportieren/export-ihrer-kontohistorie
[F04]: https://www.sparkasse.at/de/george-help/george/ihre-produkte/girokonten/kontoauszuege
[F05]: https://www.kbc.be/particuliers/fr/produits/paiements/self-banking/sur-votre-pc/comment-fonctionne-touch/effectuer.html
[F06]: https://mabanque.bnpparibas/fr/gerer/services-lies-compte/options-et-services/telecharger-vos-releves-d-operation
[F07]: https://llb.li/en/support/faq-ebanking
[F08]: https://www.bgl.lu/fr/particuliers/services-en-ligne/releve-compte-en-ligne.html
[F09]: https://www.spuerkeess.lu/de/privatkunden/info-hilfe/wie-kann-ich-meine-kontoauszuege-einsehen/
[F10]: https://www.abnamro.nl/en/personal/payments/credit-and-debit-transactions/download.html
[F11]: https://www.ubs.com/ch/de/help/account/banking-documents.html
[F12]: https://www.natwest.com/support-centre/payments/general/do-you-offer-the-ability-to-obtain-midata.html
[F13]: https://www.lloydsbank.com/help-guidance/everyday-banking/statements/download-statements.html
[F14]: https://www.aib.ie/help-and-guidance/historical-transactions-faqs
[F15]: https://td.intelliresponse.com/rewards/?id=352&question=How+do+I+download+my+account+information&requestType=NormalRequest&source=3
[F16]: https://www.nab.com.au/personal/online-banking/nab-internet-banking/transaction-history
[F17]: https://www.commbank.com.au/support.digital-banking.export-transaction-information.html
[R01]: https://www.ris.bka.gv.at/NormDokument.wxe?Abfrage=Bundesnormen&Anlage=&Artikel=&Gesetzesnummer=20012902&Paragraf=6&Uebergangsrecht=
[R02]: https://www.nbb.be/nl/nieuws-en-evenementen/nieuws/acht-belgische-banken-opnieuw-aangewezen-als-systeemrelevante-2
[R03]: https://acpr.banque-france.fr/fr/publications-et-statistiques/publications/liste-des-autres-etablissement-dimportance-systemique-eis-au-titre-de-larticle-l511-41-1-vii-du-code
[R04]: https://www.fma-li.li/de/finanzdienstleister/bereich-banken/banken/kapitalpuffer
[R05]: https://www.cssf.lu/wp-content/uploads/RCSSF-25_05-eng.pdf
[R06]: https://www.amaf.mc/fr/liste-des-membres
[R07]: https://www.dnb.nl/en/sector-information/open-book-supervision/open-book-supervision-sectors/banks/prudential-supervision/factsheet/buffers-for-systemically-important-banks-g-sii-o-sii/
[R08]: https://www.snb.ch/en/services-events/digital-services/glossary
[R09]: https://www.bankofengland.co.uk/prudential-regulation/publication/2025/july/o-sii-buffer-rates-for-ring-fenced-banks-and-large-building-societies
[R10]: https://www.centralbank.ie/macro-prudential-policies-for-bank-capital/other-systemically-important-institutions-buffer
[R11]: https://www.osfi-bsif.gc.ca/en/supervision/financial-institutions/banks/systemically-important-banks
[R12]: https://www.apra.gov.au/registers/list-significant-financial-institutions
[P01]: https://www.tink.com/account-aggregation/
[P02]: https://support.plaid.com/hc/en-us/articles/27895826947735-What-Plaid-products-are-supported-in-each-country-and-region
[P03]: https://docs.truelayer.com/docs/data-api-basics
[P04]: https://api.basiq.io/docs/supported-access-models
[P05]: https://www.cdr.gov.au/find-a-provider?provider=ADRBNK000208
