# Incomplete foreign-transfer forecasts (D-486)

## Actual defect and correction

FOR-05 requires an unknown monetary effect to stay incomplete. The original calculator used
ToAmount ?? Amount for every transfer destination. A retained EUR10 transfer to USD with no
stored destination amount therefore fabricated USD10 and showed a complete USD60 path from USD50.
The current original plan form already requires an explicit cross-currency destination amount;
this correction concerns legacy/restored rows reaching analytical computation. It never weakens
that form validation or creates an incomplete financial submission.

Preserve the known source effect independently. Reuse source minor units only when the original
source currency equals the destination currency, including when the source account is outside
the selected scope. Otherwise keep a missing destination effect null through grouping, daily
projection and UnknownCount. A future legacy ledger item follows the same rule. A temporary
source-amount assumption cannot supply an unknown rate; an explicit scenario exclusion removes
that item's warning without changing its stored row. No FX fetch, guessed conversion, source-row
rewrite, schema/model/migration, resource, permission, SDK or commercial switch changes.

## Focused source and SQLite proof

Eight new Core cases cover missing destination, source outside scope, same-currency cancellation,
same-currency incoming scope, explicit destination amounts, source-only assumptions with saved
incompleteness, explicit exclusion and future legacy entries. The original code fails four of
them; the four same-currency/explicit/exclusion controls pass. After the correction all17 tests
in the affected forecast namespace pass. Two additional actual ForecastWorkspace/SQLite cases
read the same legacy null, retain every original row through reading and save one atomic two-
currency snapshot batch with and without an explicit source assumption. USD keeps9876 and
UnknownCount1, the EUR path stays known, per-currency saved scopes remain isolated and the ledger
stays empty. Both are included in31 passing actual forecast-flow cases. One affected saved-path/
later-entry comparison regression also passes. Total49 selected successes, ten new distinct
cases; no full-suite count or unrelated successful cohort replay is claimed.

Preserve the initial application-test compile mistakes (named account cancellation parameter and
PlanStore owner), the native publish command's missing target framework, and the direct runner's
unsupported double-dash method option. These fail before native/business scenario execution;
correct their callers and execute the intended cases once. No accepted financial action is repeated.

## Original Release/AOT native page

The independent forecast-missing-rate phase creates one explicitly fictitious legacy SQLite
source using the original model: EUR100 source, USD50 destination and one EUR10 transfer with
ToAmount null, due today. It uses original ForecastWorkStore, ForecastViewModel and ForecastPage,
not a copied calculator or synthetic control. Ordinary shipping Main never includes this caller.

Run2ad5e5cd0e984d7abf4901d9448930ae/comparison1b04e40dced04a3cb33f3d7956be72cc in process6958
qualifies six original en/fa/de and Light/Dark pages on the existing412dp emulator. Each exposes
EUR90 known, USD50 with exactly one incomplete item, the original unknown-amount row and visible
untruncated warning in the focused owned native window. Compare every original typed table before/
after each context and the independent base: all unchanged, zero financial Saves. Successful
receipts are retained and skipped on continuation. The first15-second report observation ends
before publication; read the completed report from the same process, without relaunch or replay.

The complete independent Offline/x64 package is signed v2/v3 with192 AOT libraries. Restore the
exact previously installed independent APK (SHA256e55a48f1e3c13abc4144afa326a922dfb117aad7dc2b644208b5a9b3a93df637)
and stopped state. Original Main28047/all139 public Home tuples/APK/OS match; remove two exact
SHA-matched owned XMLs. Retain the new independent run's private fictitious evidence; no Main
private database or key is accessed.

## Final package and physical ordinary forecast

Strict Windows build and Build-AndroidApk.ps1 -Offline finish without warnings/errors. Complete
shipping APK: E:/Zanance.ReviewBuilds/D486-shipping-package/pro.vafadar.zanance-Signed.apk,
96,526,069 bytes,384 AOT libraries, valid ZIP/v2/v3 signatures, permanent pro.vafadar.zanance
ARM64/x64 identity and no INTERNET/ACCESS_NETWORK_STATE. SHA256:
dcc09347ca1d4597f596958438391c0b95cfce2f1a3352a6aceaac0e240131b5.

Upgrade Samsung without reset,4933-to8764. Open original Insights/Forecast read-only: the existing
D318_First_Plan remains an overdue-3EUR item, forecast976EUR, while recorded Home stays979EUR.
Return Home; all141 public tuples, OS font1/density450/1080x2340/accessibility0/empty services and
installed final APK hash match the expected state. No financial Save, profile creation, source
edit or system change occurs. Remove eight exact SHA-matched owned phone XMLs. The new missing-
destination branch is native-qualified on x64; this ordinary phone path is not a legacy ARM64
branch/full-database acceptance claim.

Drive D fills during build output. Move only the two old owned D-334 APK artifacts to retained
E:/Zanance.ReviewBuilds/retained-d334-packages after exact source/copy SHA256 equality; source
paths are verified under this repository's artifacts root before removal. The unrelated backup
archive is untouched. Clean managed test output and preserve actual test/build/native receipts.

Prior shipping-headb3fa1a0d has successful
[CI39931709286](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39931709286) and
[CodeQL39931709364](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39931709364).
Its single delayed GET is consumed and ci-toolbar-observer-d485 is deleted; no second query.
The new correction retains a separate exact-head delayed CI gate.

This closes the reproduced missing-destination conversion defect and its named source/native
cohort. Later-date actual comparison, date-moving/multi-currency physical snapshot variants,
other malformed-data boundaries, full device/security/accessibility/ANR/crash/performance/iOS/
provider/Store and release gates remain open. The broad delivery goal remains active.
