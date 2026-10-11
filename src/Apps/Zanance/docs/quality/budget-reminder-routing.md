# Original budget notification destinations - D-151 / AT-153

Status: exact original budget notification routing implemented. ENT-02/03 remain in progress; remaining budget
view/editor commercial bindings, other resources/operations/import/restore and future activation are not complete.

## Original identity and read-only resolution

BudgetReminderLink validates one bounded lowercase actual-file digest and one original saved budget Guid. The
native returning-data packet includes no path, credential, financial value or authority. BudgetChoiceSnapshot
hashes the configured absolute SQLite file identity, consistently with Plan/goal routing. Each actual alert uses
its own original candidate id, without changing native alert ids, privacy captions or threshold/delivery rules.

AppLinkRouter requires the actual ZananceStore; there is no optional inactive reader fallback. Resolve the current
actual file after secure startup/unlock and validate the reviewed originals/access/choices before navigating.
A restored id in another file is insufficient; missing targets never fall back to the generic current budget.
Explicit existing general budget navigation stays supported separately. Previously delivered historical budget
periods remain readable even when no longer selected or their paid method is unavailable for new work.

The real BudgetViewModel consumes the typed original destination and resolves it again during page loading.
Pick the exact original stored row and retain its currency, calendar and actual period start, rather than the
current Home currency or new-budget defaults. Recheck original metadata during preparation. Keep the visit
currency in memory on theme/period reloads; no Home currency write occurs from notification navigation. Weekly
rows without month coordinates initialize valid month navigation in their original calendar. A normal later
visit resets to the current saved defaults; returning from the editor retains its existing visit behavior.
Shell does not raise Appearing for a new query on an already visible root tab. MauiAppInteraction explicitly
completes the typed budget destination after GoToAsync; ShowReminderAsync publishes it inside the same load
gate so an older lifecycle reload cannot consume a newer native destination. No fake UI control or duplicated
application algorithm is used. No schema, encryption, commercial activation, portable preference or financial
write is introduced.

## Verification and negative evidence

14 AT-153 real application/isolated SQLite cases cover original Gregorian/Persian/Hijri month, custom week and
two-week identities after unlock/default changes, restored identical ids in another file, deletion while locked,
historical unselected paid methods, failed reads and malformed/extra/oversized payloads. AT-152 actual scheduler
cases additionally verify that every published returning-data packet carries the candidate's exact id/scope.
Initial two no-write test baselines preceded the app-lock startup's ordinary Settings creation; move only those
baselines after startup. Final focused 14 and ordinary parallel main 2,646 / App.Tests 589 pass, no failures/skips;
ordinary test output cleaned. No unaffected financial matrix or layout-width suite is repeated.

Strict Windows has zero warnings/errors. Canonical Build-AndroidApk.ps1 strict complete Release passes, signature,
package and full ARM64/x86_64 stores/app AOT verified. The final Release portable-PDB source checksum matches the
current BudgetViewModel and three other affected application sources; an earlier concurrently prepared package failed that check and was rebuilt, not handed off.

Six hidden own-window Windows en/fa/de/light/dark routes open the seeded original periods and compare complete
accounts/entries/settings/budgets/plans packets without Save. The first focused capture observed a transient
native readout before regenerated bindings completed; retain its negative evidence and wait after the serialized
reload before capturing. Focused onboarding bypass prevents unrelated onboarding review. The targeted final
German/dark route invokes the real native Monthly chip after a weekly destination, checks valid original-calendar
coordinates, then reopens the original weekly reminder in the retained tab through the production adapter without a diagnostic reload. Original development database and
sidecar bytes restore exactly. Existing width/font/chooser geometries are unchanged and not rerun.

The first actual native case found four taps displaying the first budget: a root-tab query did not cause
Appearing. Keep that negative evidence; correct the actual native adapter, rearm only exact owned
threshold keys. A later immediate panel read missed already posted native cards: owned Android notification
events and active records confirmed delivery. Wait for the background reminder refresh started on foreground
entry and for panel animation before reading its hierarchy. The
unsupported pending-queue hypothesis was removed, and its extra four tests are not part of the final change.
The first case retained its financial fixture with four fresh independently owned budget ids during diagnosis.
The remaining language/theme cases use separate owned fixtures. Six completed normal signed Release emulator
cases deliver/tap four actual notifications (EUR month/week, USD month,
GBP retained Flex), independently of a Persian new-budget default. Fresh uiautomator hierarchies verify every
original currency/calendar and selected period; all four destinations must be observed in each case. Generic
and opted-in details remain intact, no resume repost, all 25 complete owned sample table/column/row packets
unchanged. Exact owned database/sidecars, Main/English/original theme restore. Only exact independently owned
D151 threshold cache keys are removed with streaming substitutions; no credential/security preferences are read.

File identity/error/lock rejection uses actual non-UI application ports and SQLite; installed native delivery and
typed Shell binding are separate evidence. No physical-device, iOS, provider, commercial activation, OS
accessibility, performance target or Store/publication acceptance is claimed.

Logs: artifacts/budget-reminder-{focused-final,all-tests,windows-build-final-clean,apk-build-final-clean,
test-clean-final-clean}.log. Windows: budget-reminder-windows-final-412x892-{light,dark}, windows-adapter-412x892-dark and
matching runtime/restoration logs. Native: artifacts/budget-reminder-native/final-proof.json and per-case records.
APK/source proofs: artifacts/budget-reminder-apk-proof.json and release-source-proof.log.

Phone artifact: artifacts/android/zanance-d151-release.apk; 81,569,707 bytes; SHA-256 `4ddaaaf76b9aa3c1430b19fa63b96ba024cf441197a7fcaefac558bc23145c71`. Non-debuggable pro.vafadar.zanance, minimum 24/target 36, v2/v3 signature using the existing local test certificate. Installable phone-test handoff, not publication.
