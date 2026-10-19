# Approved daily ECB reference integration (D-389)

## Owner choice and complete boundary

On2026-10-19 the owner selected the complete [D-388 proposal](../enhancements/2026-10-commercial-release/25-ecb-reference-source-decision.md)
with its source/date/calculation/free-original conditions. This delivers the optional daily reference
adapter, explicit source configuration, shared gateway/feed cache, compatible source evidence,
original financial review/history/export/sync handling and six-language pre-payment/per-access notices.
It does not approve another feed, paid resource, bank connection, public deployment or product release.

ECB publishes five required non-EUR legs. All30 ordered EUR/USD/GBP/CHF/CAD/AUD pairs use the same
dated set: five direct EUR publications and25 calculated inverses/crosses. EUR=1; the calculation
is quote-per-EUR divided by base-per-EUR. The managed decimal representation retains up to28–29
significant digits with its intrinsic division rounding; there is no additional display-to-storage
rounding or financial writer change. References are informational, never executable bank prices.

The source default remains closed. Explicit Market:Source=ecb selects the approved source and
authenticated HTTPS routes independently of fictitious Market:LocalQualification. Unknown sources
are refused. The optional eng/hosting/compose.ecb-reference.yml overlay provides API-only outgoing
connectivity and no new listening port. Compose validation passes; no image/container/public service
was deployed for this batch. Existing DNS/Tunnel/mail/company/store gates remain independent.

## Source safety, dates and compatibility

Only explicit Fetch requests can obtain the fixed ECB XML through normal platform TLS. No arbitrary
URL, query, cookie, credential, private financial identity, amount or quantity reaches ECB. No hidden
retry or redirect. Compression is refused; actual uncompressed body bytes are bounded at262144,
headers16KiB, XML depth4, currency rows64, fields/attributes and a12-second original-feed deadline.
DTD/external entities, foreign namespace, missing/duplicate required legs, invalid/nonpositive rates,
future or older-than-seven-day sets and regressed/changed same-date values are refused. Failed refresh
preserves last-good retained data. One original feed task/cache serves different symbols/users; caller
cancellation cannot replace live work. A new pair does not reset the feed freshness clock.

MarketQuote version2 explicitly stores ObservedAt=null with daily evidence: original source date,
raw XML SHA256, actual retrieval time, optional HTTP document modification, original numeric legs
and declared calculation. Document modification is never labelled a fixing time. Version1 retains
its required timestamp and omits null Daily, preserving original canonical bytes/digests. Existing
immutable QuoteJson storage and frozen financial/source-companion envelopes remain unchanged; no
EF entity, migration or compiled-model change. Old clients fail closed on unsupported new evidence.
Do not edit this registered revision's policy in place: future policies need a new revision and must
retain historical validation for already accepted documents.

The original writer still requires exact reviewed source, target/date/value, unlocked profile access,
unchanged financial state and explicit native consent, including stale-data acknowledgment. Fetch
does not post money or alter historical values. Cold history remains readable after cache expiry.
Portable restore and signed encrypted household source publications preserve the complete evidence
without granting financial authority. Cache, Home and history reads do not dispatch upstream HTTP.

## Attribution and original data access

The existing six languages explain free original ECB data, daily informational precision and direct
versus calculated values. Notices appear in the reference page, retained/reviewed/history descriptions,
plan comparison and the exact native pre-payment terms. Serialized original attribution/calculation
is retained in exported/synchronized source evidence. The growing source-link button opens a full
external browser without framing, logo or endorsement. Native checks inspect that button and source
caption; they do not launch a browser, store or payment provider.

Reviewed sources: [reference description](https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.en.html),
[fixed daily XML](https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml),
[reuse conditions](https://www.ecb.europa.eu/services/using-our-site/disclaimer/html/index.en.html).
The original data has no upstream access fee/account/key under the reviewed conditions. Existing
operation/maintenance costs and future provider pricing are not guaranteed or estimated as zero.

## Focused managed and actual source evidence

Forty-five new regular cases pass selectively:21 adapter/gateway cases, five route/configuration
cases, ten Core evidence/compatibility cases, three Data cases and six actual App daily-consent cases.
One additional explicitly invoked actual-public-feed case passes. Twenty existing version-one
gateway/cache cases and six affected existing pre-payment cases pass; nine other already passing
billing cases are retained. These are distinct cases, not an assertion of a new whole-suite total.

The HTTP route test uses actual routing/serialization/authorization enforcement with synthetic
authentication/upstream effects. Its HTTPS request scheme is not a real TLS/account qualification.
Separate actual adapter reads use normal platform trust without bypass/settings changes. The final
packet probe at2026-10-19T14:07:45.2391623Z returns reference date2026-10-19 and original-byte digest
02aaddcdcd7b76106eae23f0aedbc7c63d4f61b5f92aafb152c6a5fef09b1580. All30 pairs share that
single retrieval, with null fixing times. Independent90-digit arithmetic agrees within half the last
stored decimal place. A repeating inverse retains its full decimal value through actual SQLite and
portable restore. No source/account/financial payload was sent upstream.

Retain the first source probe before the nullable precision revision and the final changed-packet
probe as separate evidence, not two new cases. Retain the initial test-only record/list equality
failure and corrected digest comparison, the route-test BaseAddress mutation failure and corrected
absolute-URI check. Initial simultaneous Android builds locked shared PDBs; the failed package build
was not accepted. Final package/build checks pass. After the final in-flight local-reference capture
correction, only its three affected adapter cases are repeated and pass. No unchanged OCR, ledger,
bank-import, broad crypto or whole-suite replay.

## Original native pages and retained observation failures

The final Windows Debug caller completes54 changed surfaces: direct quote, calculated quote and
plan comparison in18 en/fa/de Light/Dark contexts at360/412/1280px. Original native Invoke/complete
growing text/source-link targets and stored-input preservation pass. Inspect all54 window captures.
Run5a061d7f16c343268ec84623573bfbcb, PID90488, independent package
pro.vafadar.zanance.storepreview.5a061d7f16c3. The private review desktop restores Default exactly.
Final strict ordinary Windows Release build passes in20.39s with zero warnings/errors.

The independent Release/AOT Android caller completes18 surfaces in six en/fa/de Light/Dark contexts
at412dp,72 complete native captions, original native Review-button clicks and unchanged original
synthetic rate/history data. No Fetch, payment, financial Save or OS setting change. Original theme
and language restore. Original completed run76faff2d2cf34dca8d6071f1a65fb9b5,
comparisonc048500c254f44798f3fcd36fab0471e, PID14788, source MVID
3306778a-bf87-497e-8b82-5f06bf4553e9. Original caller APK SHA256:
bcb032edba637a5af00e92ad60aa13fe4d347403608d0a24bfcd3633f6777dcd.

Preserve the failed diagnostic footer-scroll attempt and later shown/enabled-but-unfocused modal
button observations. Waiting for root rebuild alone did not solve the modal focus condition. The
successful Android review uses the actual Shell detail stack and retains the shown/focused native
input requirement. These diagnostic failures are not claims of a repaired product crash or ANR.

The original success report was saved before emission. Ten middle log chunks55–64 were missing
from82; observation expiry was not a failed native test or permission to replay. A report-only phase
validates the exact original comparison/MVID/18 observations/unchanged-data and no-write facts,
exports only that synthetic receipt and emits a compact summary. Recovery PID15295, comparison
1720ba2f0578403092aed5f2702bad95; no UI or financial work repeats. Exact original124853bytes hash
624f5cf8babe9de3c8bc01b92b893eeb00da91a1a81f6caae9388d43b3a0658a; all72 available original
log chunks match the recovered bytes. Remove only the hash-matched owned external report, retain
the private original/failed fixtures, and stop the independent caller after terminal recovery.

## Main package, upgrade and practical limits

Complete offline signed APK: artifacts/android-ecb-d389/pro.vafadar.zanance-Signed.apk,
94912245bytes, SHA256d89eebf4dc9c816ec603bde0c9edaedc2173ba11854af567b23d309e7aa0dd6c.
Both ARM64/x64,384 AOT libraries, API24/36, eight unchanged native engines, no INTERNET/debug/profile
permission or new native library. Signer SHA256 remains
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b, not a store-signing assertion.
Final ordinary main build succeeds without warnings. Independent final compilation also passes.

Main emulator upgrades without reset from PID13316 to15560; exact meaningful original Home/My account/
0.00EUR, installed APK digest and OS settings match. Keep the original protected data/key sets. Samsung
is not used or upgraded for D-389 and remains on its previously qualified D-385 package; no phone
lease is acquired. Existing device security/authentication settings remain untouched.

Artifacts/ecb-integration-d389 retain focused logs, original source/arithmetic packets, native receipts,
captures, partial/recovered reports and package/restoration evidence. This does not accept public
service availability, genuine identities/purchases, all phone/ARM64/API24/16KiB behavior, iOS,
spoken accessibility, general ANR/crash resolution or release. D-386's final stability/accessibility
work remains after approved feature development. Hosted CI is checked separately after this push.
