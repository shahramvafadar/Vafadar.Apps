# D-388 - Decision-ready free reference FX source

Status: proposal, not selected or activated. Checked2026-10-19. This is the next concrete OD-14
source/licence/cost decision for FX-01, following the delivered provider-neutral D-299 framework.
It does not reopen current-machine hosting, company/store setup, the fourteen target countries,
the six UI languages or deferred Windows/web-checkout decisions. No source-specific adapter is
committed before the owner selects this source. App, service, APK and managed count remain D-387.

## Recommendation and alternatives

Recommend European Central Bank euro reference data for optional informational currency references.
It needs no upstream account or API credential, covers the six existing target-market currencies,
and can supply all30 ordered currency pairs through five direct EUR pairs and25 explicitly calculated
reverse/cross pairs. Maintain original units/date/source and distinguish calculated values from direct
publication. This is not an executable bank price, spread, retail conversion, tax rate or asset quote.
Manual input remains available; no ledger, historical conversion or holding value changes on fetching.

| Choice | Practical result | Cost and dependency |
| --- | --- | --- |
| ECB reference data (recommended) | Daily reference FX for EUR/USD/GBP/CHF/CAD/AUD; explicitly labelled reverse/cross computation; source/date/free-original disclosure | Public source has no data-access fee under its reviewed reuse terms. Existing service operation still has costs. Owner source/terms approval and complete adapter/native qualification precede activation. |
| Keep manual references for now | Current manual workflows and unconfigured online-source behavior remain | No new upstream or integration cost; automatic reference lookup stays unavailable. |
| Obtain a commercial source proposal later | Potential intraday or metal/item coverage must be separately verified | Exact provider rights, redistribution, scope and price quote needed first; no subscription, account or payment is authorized here. |

This choice is only the FX source. It does not select a bank connection, AI provider, gold/coin feed,
payment processor or company identity, and it does not publish a service. Future metal/item providers
need their own original unit/purity/market and cost evidence. [Machine-readable model](26-ecb-reference-source-model.json).

## Reviewed original source, terms and cadence

- [ECB reference-rate description](https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.sl.html)
  describes the daily reference and approximately16:00 central-European publication schedule on working
  days, with TARGET closures excluded. It warns against using the references as transaction prices.
- [Fixed daily XML](https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml) is the proposed upstream;
  no user-specific URL, query, account or financial payload is needed.
- [Current copyright conditions](https://www.ecb.europa.eu/services/using-our-site/disclaimer/html/index.en.html)
  allow free reuse with conditions: identify ECB, reproduce accurately, explain modifications, and tell
  purchasers before payment and whenever accessing the information that the original is freely available.
  Business links open a full browser view; do not frame the source or suggest endorsement/use its logo.

The last point affects the existing paid-plan and source-display flows, not only a small About credit.
Implement the free-original notice in every supported language before affected purchase confirmation,
every visible reference/history use and relevant exported source evidence. Derived cross rates need
an explicit calculation label. Final merchant/public-release acceptance remains separate.

## Actual read-only probe and date precision

At2026-10-19T13:31:23Z a fixed-URL HTTPS GET with normal platform certificate validation returned200,
text/xml,1547bytes, no redirect and Cache-Control max-age300. SHA256:
4422161311c3c02cf2d1571db07bdf9884b3f42fdeb16a4926903236ae6ab42d.
The public feed contains29 currency entries and reference date2026-10-16. All five required non-EUR
currencies are present. This is a source observation, not an app/provider acceptance or latest-rate
promise. No account, cookie, credential, owner financial data or production service was involved.

The source has a date, without an intraday fixing timestamp. Its HTTP Last-Modified value is
2026-10-16T13:56:43Z; this is document metadata, not proof of a market fixing at that instant.
Do not populate the current MarketObservation.ObservedAt with invented midnight,14:10 or retrieval
time and describe it as the original fixing time. Complete explicit timestamp-precision/provenance
handling, honest native labels and compatible versioned persistence/protocol boundaries before
activation. Existing immutable personal/household/source contracts must not silently change.

The initial independent client failed certificate-chain validation before an HTTP response. A separate
platform-trusted read succeeded with normal validation; no trust store, global setting or certificate
callback was changed. The browser reader's XML content-type refusal and English HTML retrieval timeout
are not upstream format/support failures; the actual validated XML and official alternate page were read.
Local artifacts/ecb-source-review-d388 retain exact public bytes, headers/digests and arithmetic evidence.

## Complete follow-through required after owner approval

Deliver one adapter/gateway/data/native batch, not a registered-but-unusable helper:

1. Keep the current UnconfiguredMarketProvider as the default. An explicitly chosen source uses a
   fixed HTTPS host/path, normal TLS, no cookies/credentials, redirects, user URLs or hidden retries.
   Bound actual/decompressed bytes, XML depth/rows/fields and time; prohibit DTD/external entities.
2. Verify source namespace/date, unique codes and strictly positive finite values. Missing required
   legs, foreign/future/too-old data, malformed rates or provider failures preserve the prior cache.
   Never substitute a different currency or derive a metal/item price from this FX source.
3. Retain five direct values; calculate the other25 from the same dated source set as
   rate(base -> quote) = ECB(quote per EUR) / ECB(base per EUR), with EUR=1. Record original legs,
   formula, rounding/precision and raw-source identity honestly through compatible source evidence.
   Keep financial-writer rounding and explicit native confirmation independent.
4. Coordinate one original in-flight feed read/cache across symbols/users. Explicit user fetch can
   request data; opening Home/cache/history cannot. Cache-Control is observed source metadata, not
   a promised rate-limit contract. Show market date, retrieval time, staleness and unavailable/manual
   states independently, with no fabricated source timestamp or silent historical update.
5. Add pre-payment/free-original and per-access attribution, derived-value and source-date disclosures
   in the existing six languages. Keep the source name/link separate from an endorsement or app brand.
6. Qualify affected decoder/decimal/date/provenance/concurrency/cache/storage and original native flows,
   strict Windows/Android, complete signed APK and appropriate original main paths. Preserve old passing
   unrelated suites. Public DNS/Tunnel/mail, genuine store and Mac/iOS remain their existing gates.

The existing source contract does not by itself encode every required date-precision/derivation detail;
that compatibility work belongs in the complete integration, not in an unsupported capability claim.
The source/provider fee model is zero at this reviewed snapshot. No hosting/electricity/maintenance,
tax, store commission or future provider-price guarantee is included. No paid resource is created.

## Arithmetic/source review, not product tests

Read the actual public set, verify the five selected positive currency values and compare all30 derived
ordered pairs against exact rational arithmetic at high precision. There are five direct and25 derived
pairs; no selected target is missing. These are proposal checks, not new managed tests, an implemented
adapter, native acceptance, paid-plan activation or public availability. Do not rerun successful builds,
financial suites, native matrices or D-387 package checks for this documentation-only decision batch.

Owner action: select the proposed source/usage conditions or keep the current manual-only behavior.
Implementation and exact activation configuration follow the selected choice; company account entry,
credentials and public publication are still performed/approved separately as already recorded.
