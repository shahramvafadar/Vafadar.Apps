# Original forecast work and currency-safe saved comparisons (D-169 / AT-170)

## Behavior and original boundaries

ForecastWorkStore reads original accounts, entries, schedules/states, settings and saved forecasts in a
deferred SQLite reader. Missing Settings uses unsaved defaults. Capture/revalidate the configured file
and original cached access; never infer access from Simple/Advanced or shared host/personal aliases.
BasicForecast retains the Free financial-month-end result in the named calendar. AdvancedForecast
guards new 30/90-day/scenario computation, snapshot Save and comparison. Original calculators and all
financial inputs remain complete, independent of active-item selection. Free retains saved values,
assumptions, dates, metadata and owned deletion without computing a new paid comparison.

ForecastWorkspace is the actual non-UI application source compiled in App.Tests. IForecastInteraction
isolates original native prompts, menus, consent and navigation. Covered projection publishes before
enabling input. Refusal keeps raw horizon/scenario/name; explicit Reset/Refresh does not silently save.
Native effects are revalidated after awaits. Actual-file snapshot batches commit all currencies together,
with current access checks and immutable ids. Accepted writes retire the draft before fallible notification/
history effects; refresh cannot silently repeat the accepted Save. Delete retains approved owned-data rights.

## Confirmed financial defect

The previous Save put every currency's account ids in each snapshot. A reproduced legacy EUR comparison
expected 12345 minor units but returned 22221 after adding USD 9876. New saved scopes contain only the
original eligible accounts of that currency. SnapshotComparer also filters a legacy scope by its saved
currency before calculating balances. Legacy metadata is not rewritten, and no currency conversion or
financial relationship is guessed. The failing reproduction is negative evidence, followed by passing
real SQLite and native two-currency checks.

No schema/model/migration, portable format, credentials, permission, SDK or commercial activation change.
Original correction/history/recovery and app security remain separate from new paid analytical work.

## Verification

29 AT-170 cases and the ordinary 3,074-test suite pass (zero failures/skips); cleanup and strict
Windows/complete Android Release builds pass without warnings/errors. Thirty-six existing Windows
layout contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Three further native
two-currency contexts verify per-currency saves and immutable legacy comparison with complete original
25-table restoration. Normal Release Android name/Save/details/Delete consent was exercised once;
exact owned bytes/sidecars and Main/English/System were restored. Final updated Release is installed/stopped.
Commercial registration stays inactive. Phone, iOS, real OAuth, billing and release acceptance remain open.

The 29 real-flow cases cover six-language refusal and retained drafts, all three calendars, inactive/
Plus multi-currency batches, personal/shared access separation, changed rights and foreign file, noncreating
reads, direct writer refusal, real SQL-trigger rollback, retired access during a writer, accepted subscriber
failure without repeat, original Free history/Delete and legacy mixed-currency comparison.

Windows fixtures use independently owned real SQLite and native horizon/Refresh/Reset/Save controls.
Rights and prompt/consent/return answers are explicit fictitious ports. Complete rows and development
bytes/sidecars are restored. The new two-currency route repeats only the changed financial behavior;
unchanged 36-context layout verification is not repeated. Persian legacy-comparison capture was inspected.
Syncfusion chart pixels are absent in the hidden-root capture: this is not chart/GPU, OS-scale or screen-reader acceptance.

Normal Android Release uses the actual native name prompt, Save, saved details and Delete consent once,
with complete original row equality and existing D-141 migration only. Earlier one-currency native behavior
remains unchanged by the final currency filter. The updated final signed package is installed/stopped;
typed commercial Android refusal is not claimed. No device settings or owner desktop input were changed.

Negative preparations: initial test warnings were repaired; three Windows selector attempts confused a
horizontal chip scroll, the child-header wrapper or a hidden Retry action. They are not native acceptance.
Android bounded hierarchy reads and a non-Button Shell-tab assumption stopped before Save. Fresh current
hierarchy in the same process exposed its clickable parent; accepted Save/Delete were never repeated.

Evidence: forecast-work-complete-tests.log, forecast-work-all-tests-final.log, forecast-work-clean-final.log,
forecast-work-windows-currency-final.log, forecast-work-android-currency-final.log; original v4 Windows
matrix proofs, forecast-currency-windows-final.log and three currency proofs; forecast-work-native/final-proof.json;
forecast-work-mixed-currency-negative.log, forecast-work-release-source-proof.log and forecast-work-apk-proof.json.
All nine changed production source checksums match the final Release PDBs.

## Complete installable package

artifacts/android/zanance-d169-release.apk: 82286507 bytes; SHA-256
73c20c1746ce25c040a7732a12828e191982f42711ac5ad5674c15472c7aa60e. Non-debuggable pro.vafadar.zanance, API 24/36, complete
arm64-v8a/x86_64 assembly stores and application AOT. v2/v3 verify with the existing local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is not a production Store certificate.

## Remaining finite scope

Forecast/snapshot group is delivered locally. Next is the original receipt recognition boundary, then
loan/report/PDF computation. ENT-02/03/04 and the full continuous goal remain incomplete; encryption,
owner texts/markets/samples, provider/billing/device/iOS and publishing decisions stay separate.
