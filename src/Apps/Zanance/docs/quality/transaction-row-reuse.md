# Transaction row reuse - QA-06 / D-104

Repeated filters previously rebuilt every matching `EntryRow`, including formatted money, captions, colors and
selection objects. The complete source and native grouped list remain unchanged. A snapshot-local projection cache
reuses previously formatted rows, keyed by transaction identity. Every data reload discards the cache; culture and
translation culture identity, palette variant and the ordered display-unit values invalidate it before presentation.
Selection is always reapplied from the existing bulk flow. No data, filter scope, ledger rule or row is omitted.

## Actual bound Windows measurement

Temporary Debug instrumentation measured the real `TransactionsViewModel` attached to `TransactionsPage`, using
independently owned QA-06 fixtures: 10,000/100,000 transactions and 1,000 complete day groups. Three separate
processes per shape and build performed three Expense -> All cycles after the first All presentation: 18 warmed
refreshes per shape/build. All returned counts and realized native buttons matched the complete source. The native
collection's actual bound ItemsSource was verified. Original development DB/WAL/SHM files were restored with exact
hashes after both cohorts. Instrumentation was removed before final builds and is not shipped or committed.

| Median milliseconds | Reference before | Reference after | Tenfold before | Tenfold after |
| --- | ---: | ---: | ---: | ---: |
| Filter | 0.41 | 0.43 | 4.76 | 4.52 |
| Summary | 0.63 | 0.59 | 1.83 | 5.43 |
| Group and row construction | 19.49 | 13.13 | 219.33 | 38.31 |
| Synchronous native source publication | 23.26 | 26.89 | 247.32 | 311.35 |
| Whole refresh | 53.38 | 42.26 | 583.14 | 358.90 |

Whole-refresh medians are calculated per complete sample, not by adding component medians. Tenfold warm refresh
improves by about 38.5%; native source publication becomes slower in this cohort and remains a bottleneck. These are
Windows Debug observations, not cold-start, frame-presentation, physical ARM64, Release Android or a two-second gate.
First formatting of a previously unseen row still occurs; the cache retains rows for this snapshot and does not claim
to reduce initial database materialization or provide pagination. No artificial result cap was introduced.

## Verification and limits

Four AT-109 application tests cover 100,000 ordered sources with repeated filters, same-id fresh snapshots, independent
selection and failed projection retry. Main suite: 1,488 passed, zero failed/skipped; App.Tests: 252. The real Windows
walk-through additionally checks row identity across filters, display-unit invalidation, fresh objects after reload,
captions, money, icons, icon/tile/amount colors and review state against EntryPresenter, plus unchanged complete
fictitious ledger JSON. Existing
native Select all/Cancel and loading/failure/retry paths remain part of the transaction review.

An initial review helper compared separately allocated Color references instead of semantic values; it failed before
publishing proof and restored development data. Value equality corrected the helper without a product change. Its
failed folder is excluded from successful evidence.

Final Windows review: en/fa/de, light/dark at 360x800, 412x892 and 1280x820 with process-local 200% text,
plus all three languages at normal text/360x800/light: 21 contexts and 609 own-app renders. All actual row-reuse,
unit/fresh-load/field checks pass, along with the existing native Select all/Cancel and retry actions. Development
database files return with exact hashes. No OS text scaling or desktop input is changed.

Complete Release on the owned emulator: six normal-scale reference language/theme contexts plus an English/System
tenfold context, each with three unique native query rows and repeated Expense -> All filters; empty searches clear
old results. This proves native filter behavior, not Release timings or full Android All-list performance. An initial
driver required EditText while the real semantic Search is AutoCompleteTextView; correcting the typed native locator
needed no product change. That failed folder is excluded and the original sample was restored.

The first tenfold driver sent input while the actual Search was disabled by the loading cover; unconditional Back
then exposed the separate pre-existing isolated encryption-proof app. The process remained present and no owned
ANR marker was reported. No encryption-proof database was read or changed. The six completed normal reference contexts
were retained after verifying the identical APK hash. Only tenfold was resumed with a bounded observed enabled-input
gate and keyboard dismissal only when the keyboard is actually shown; it then passed. This does not close the
historical cold/ANR gate. The first tenfold attempt and failed restoration are retained as negative driver evidence.

All three owned fictitious profiles retain their complete original tables and settings values against D-103;
reference/tenfold/prior counts remain 10,001/100,001/3. Settings audit differences, if any, are recorded independently:
reference=false, tenfold=false, prior=false.
Final Release is reinstalled; actual English Home, three original transaction rows, System/Advanced, translated Back,
SECURE foreground flag, unchanged density 420/font scale 1.0 and Home-stopped handoff pass. No financial Save occurs.

Strict Windows and canonical complete Debug/Release Android builds pass with zero errors/warnings. Installable test
APK: `artifacts/android/zanance-d104-release.apk`, 80,918,443 bytes, SHA-256
`328d169aadd7d46f3df856d5f2263f895ca6845bd51ddf8df45b332214e60658`. Package `pro.vafadar.zanance` 0.1.0/code 1, min 24/target 36, ARM64/x86_64 full assembly stores
and app AOT, ZIP integrity, non-debuggable package and v2/v3 signatures verified. Certificate SHA-256
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is the local test certificate, not production approval.

Physical phone/iOS, real OS/readers and product/release acceptance remain separate. No CI success is inferred before
the single scheduled exact-commit check. QA-06 remains partial:
first entry materialization, native publication, cold duration/historical ANR and real platform/device gates are open.
No new schema, resource string, permission, SDK, network endpoint or portable/device-security preference is added.
