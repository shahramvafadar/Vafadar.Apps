# Zanance

Personal finance manager: record income and expenses, organize them into accounts and categories, and understand
where the money goes with reports. Local-first and offline, multilingual (English, Persian, German, Spanish, French, Italian, with Gregorian, Persian or lunar Hijri calendar) and backed up
with optionally password-protected files the user keeps wherever they like, or – in builds with cloud backup (D-35) – in the user's own Google Drive or OneDrive. Connected destinations discover existing app backups automatically.
Interface language, regional date/number formats, digit shapes and holiday rules can be chosen independently.

| | |
|---|---|
| Product name | **Zanance** – shown untranslated in the app, stores and notifications; code and folders use the same name (D-24) |
| App id | `pro.vafadar.zanance` – Android package and iOS bundle id (permanent once published) |
| Platforms | Android (first), iOS, Windows |
| Status | 🚧 Phase 1 feature-complete; cloud backup (OneDrive and Google Drive on Android, iOS and Windows) implemented and waiting for OAuth clients and a device test (D-35, D-50): accounts, entries, transfers, refunds, categories, plans with Gregorian/Persian recurrence and automatic posting, Home dashboard, budget, reports, forecast, reminders, manual exchange rates, CSV import/export, optionally encrypted local/cloud backup files (D-62), first-run restore without a new account, theme/experience choices, Simple/Advanced, app lock. Phase 2A in progress: goals with optional contribution-date reminders, rollover and envelope budgets, splits, partial payments, contracts, reimbursements, tags, rules, loans with a repayment estimate, assets, receipts, weekday rules, display units such as the toman, forecast scenarios, saved filters, PDF reports, a customizable Home, a dark theme, flex, weekly and two-week budgets with pay-cycle months and limit suggestions, public holidays, a quick add widget, receipt and PDF invoice reading, and local profiles. Status: [phase 1 plan](docs/04-phase-1-plan.md), [phase 2 backlog](docs/05-phase-2-backlog.md). |
| Solution filter | [`Vafadar.Zanance.slnf`](../../../Vafadar.Zanance.slnf) |

## Projects

| Project | Contents |
|---|---|
| [`Vafadar.Zanance.Core`](Vafadar.Zanance.Core) | Domain model and calculations (platform independent): ledger, plans and recurrence, budget, reports, forecast, reminders, rates, CSV |
| [`Vafadar.Zanance.Data`](Vafadar.Zanance.Data) | `ZananceDbContext`, migrations, stores, automatic posting, backup summary |
| [`Vafadar.Zanance.Reports`](Vafadar.Zanance.Reports) | PDF reports with the embedded Vazirmatn font (right-to-left aware) |
| [`Vafadar.Zanance.App`](Vafadar.Zanance.App) | .NET MAUI app: pages, view models, platform code |

Tests: [`test/Apps/Zanance`](../../../test/Apps/Zanance).

## Run

```powershell
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -t:Run -f net10.0-android                 # emulator / device
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -t:Run -f net10.0-windows10.0.19041.0     # Windows
```

## Database migrations

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project src/Apps/Zanance/Vafadar.Zanance.Data --startup-project src/Apps/Zanance/Vafadar.Zanance.Data
```

Migrations are applied automatically when the app starts. They must only add to the schema (AT-60, tested).

## Reviewing screens without a device

Debug builds can walk through every screen in English, German and Persian, in the light or dark theme, and save
screenshots (plus the report PDF) to `artifacts/snapshots`:

```powershell
./eng/scripts/Run-Snapshots.ps1 -Languages fa,en [-Theme dark]
```

The script builds the Windows Debug app, resets only its own development database, seeds fictitious data and closes
the app when done (`Diagnostics/DebugSnapshots.cs`).

## Documentation

* [Design documentation](docs/README.md): assessment, domain, UX, phase-1 plan, backlog, privacy matrix, test plan,
  release checklist
* [Requirements](docs/requirements.md)
* [Changelog](CHANGELOG.md)
* Shared concepts: [architecture](../../../docs/architecture/overview.md), [localization](../../../docs/architecture/localization.md),
  [data and backup](../../../docs/architecture/data-and-backup.md), [privacy matrix](../../../docs/privacy/privacy-matrix.md)

Privacy controls (D-63): optional four-digit app PIN across local profiles, protected verifier and restart-persistent
attempt limiting; separate Android foreground screenshot preference, on by default, with recents protection retained.
Owner-written source is proprietary and all rights reserved; bundled third-party notices apply only to dependencies.

Period review reminders (D-71): optionally enable a reminder in Settings for 09:00 after your financial month
closes, following the selected calendar and month start day. Off by default; no automatic review or money movements.

Aggregate CSV import (D-72): review each overlapping aggregate and choose linking or keeping both. Preview dates,
amounts and the remaining total before saving. Linked imports retain safe Undo across restart and database backup;
later changes or dependent imports require review. No automatic merging. See AT-79 for evidence and device gates.

Application flow coverage (D-73 / QA-03): 68 AT-80 cases compile actual non-UI onboarding, profiles, bulk commands,
access, widget/reminder routing and theme sources with explicit native ports and real isolated SQLite. The full
main suite has 1,263 passing tests. Native runtime/physical-device evidence remains separate; no commercial limits.

D-74 / QA-04: valued-asset consent now has 37 AT-81 application cases; App.Tests has 105 and the main suite 1,300.
The actual editor gate excludes repeated pending saves and preserves cancelled drafts. Local/emulator evidence and
signed Release handoff remain separate from physical phone/iOS acceptance. That slice was followed by QA-06 performance; see the canonical backlog for the current remaining work.

Historical local quality baseline (D-111): main suite 1,535 passing, App.Tests 299. Repeat opens a prefilled monthly
Plan from a new transaction draft without posting money; see [runtime evidence](docs/quality/entry-repeat-draft.md). Date parts grow/reflow and large
balances retain their complete signed decimal/currency packet with a real horizontal viewport when needed.
Repeated Settings language choices preserve the form and stored data; retired navigation titles are detached.
Windows child titles grow above the retained body; Windows/Android Back descriptions follow live language choices.
The open Settings estimate retains unsaved text/period/currency across reload; see docs/quality/settings-estimate-draft.md.
See docs/quality/font-scaling-a11y03.md for the actual running-app evidence and complete signed phone-test APK.
Ready accessibility/performance follow-ups continue under D-69; unresolved owner/provider/physical-device/iOS
acceptance stays separate. Earlier test counts above describe their historical slices.

Repeated transaction filters reuse complete snapshot rows; fresh data/display changes invalidate the cache.
See [transaction runtime evidence](docs/quality/transaction-row-reuse.md) for measured warm performance and its limits.

All four Windows Insights destinations remain visible with growing captions; see [runtime evidence](docs/quality/insights-navigation.md).

Budget names and complete compact figures use separate rows; signed boundary formatting no longer overflows.
See [budget runtime evidence](docs/quality/budget-readouts.md) for independent native/suite/package checks.

All three budget period choices stay visible in growing rows; see [period runtime evidence](docs/quality/budget-periods.md).

Tag suggestion captions grow with text and retain raw selection identity; normal Release verifies unsaved selection
and cancellation. See docs/quality/tag-suggestions.md for complete D-89 APK evidence and the local PowerShell-host
startup limitation. Physical-device, iOS and product acceptance remain open.

The detail disclosure names its current action and preserves unsaved values. D-90 canonical scripts, normal Release
and installable APK evidence are in docs/quality/entry-details-disclosure.md; the older D-89 console-host failures
remain dated evidence. Physical-device, iOS and product acceptance remain open.

Both onboarding restore captions grow with large text. D-91 native draft/navigation checks and the installable APK
are recorded in docs/quality/onboarding-restore-actions.md; physical-device/provider acceptance remains independent.

Measured Home reload work and complete native-row/data checks: docs/quality/performance-home-snapshots.md.
Cold-start and physical-device performance remain separate gates.

Android Debug platform guard and native stage evidence: docs/quality/home-debug-platform-and-native-stages.md.

Complete Home customization and native persisted-action evidence: docs/quality/home-customization-readable.md.

Hidden Home account view evidence: docs/quality/home-hidden-account-views.md; full snapshots and shown actions remain.

Complete account description evidence: docs/quality/account-descriptions-readable.md; balances and row actions remain.

Complete debt entry action evidence: docs/quality/debt-entry-action-readable.md; draft and financial behavior remain.

Single readable modal header evidence: docs/quality/modal-headers-readable.md; existing form and financial behavior retained.

Complete visible plan validation evidence: docs/quality/plan-validation-visible.md; drafts and existing financial rules remain.

Complete transaction-field validation evidence: docs/quality/entry-validation-visible.md; drafts and existing financial rules remain.

Existing destination-fee retention in Simple: docs/quality/destination-fee-retention.md; successful native edits preserve financial fields and row identities.

Complete final-bill field feedback and action: docs/quality/settlement-feedback.md; native valid differences/refunds preserve original financial rows.

Due-item actions, input feedback and safe native continuations: docs/quality/occurrence-feedback.md.

Complete bulk selection now uses one fresh-snapshot membership index. Actual bound full-selection reload evidence and timing limits: [bulk-selection-reload.md](docs/quality/bulk-selection-reload.md).

Settings reads the exact complete-month history needed for its unchanged spending suggestion. Actual bound measurements and runtime/acceptance limits: [settings-suggestion-history.md](docs/quality/settings-suggestion-history.md).

Windows selected/popup picker captions grow with inherited native typography and retain complete names/44-unit targets.
Actual matrix, choice/draft/data preservation and complete signed APK evidence: [native-picker-captions.md](docs/quality/native-picker-captions.md).

Complete report-scope captions, reachable Clear targets and transaction results: [runtime evidence](docs/quality/transaction-scope-readable.md).

Independent saved-filter results after report navigation: [runtime evidence](docs/quality/saved-filter-report-scope.md).

Complete growing transaction category choices: [runtime evidence](docs/quality/entry-category-captions.md).

Current local quality baseline (D-144): normal parallel main suite 2,448 / App.Tests 425 pass; strict Windows/Android builds have zero warnings/errors. Settings opens the reviewed account/Plan/goal chooser, while test builds remain unrestricted. Actual native matrix and complete signed APK: [evidence](docs/quality/goal-chooser.md). Other resource bindings and owner/provider/device/iOS/release gates remain open.

## Durable local resource choices (D-141)

Account and Plan choices now have scoped, reviewed persistence and actual writer/reminder binding; history remains
complete. Current test builds stay unrestricted. D-142 delivers the reviewed chooser UI; other resource bindings remain open.
[Evidence](docs/quality/durable-resource-choices.md).

## Reviewed account and Plan availability (D-142)

Settings opens the reviewed local chooser with growing identities, separate states, capacity feedback and explicit
Save. Current test builds need no subset. D-142 main/App test baseline: 2,399/407 pass, strict builds zero
warnings/errors; actual Windows matrix, normal Release own-emulator cases and complete signed APK are in
[the quality record](docs/quality/active-item-chooser.md). Other resource bindings and owner/external gates remain open.

## Durable goal choices (D-143)

Extend the existing ResourceSelections boundary to Goals without a schema/entity change. Reviewed original names,
measurement kinds, currency and active/paused/completed/archived states are rechecked with actual file, financial
scope, verified rights, stored revision and capacity. Free's one goal counts a pause; explicit empty is retained.
Invalid/stale choices need review, and choosing an advanced retained goal never grants its paid tools.

The goal writer reads stored choice under the actual SQLite writer; it overrides only cached choice, never
entitlement/membership. Explicit new or reopened goals join a free chosen slot atomically, rather than counting
unselected retained history again. Completion/archive/delete releases only that identity; pause keeps it.
Unselected paused goals cannot resume. Retained notes/state corrections, negative earmark releases and deletion
remain available. Positive earmarks independently require AdvancedGoals and usable goal/account choices; no ledger
or balance is written. Full editor/contribution failures roll back goal, pins, plan and choice together.

Current release registration remains inactive and unrestricted. D-144 adds the reviewed goal chooser to Settings;
effective goal/contribution projection and native reminder delivery follow in subsequent ENT-02/03 slices. Other
selected resources, activation and owner/provider/device/iOS/publication gates remain open.

31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](docs/quality/goal-resource-choices.md).

## Reviewed goal availability (D-144)

18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](docs/quality/goal-chooser.md).

Goal selection preserves original state/history and does not grant paid tools. Effective delivery bindings follow next.

## Actual goal work and scoped reminders (D-145)

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

New-work choices/paid tools remain separate from complete original progress. Reminder destinations resolve the original actual file through app lock; retired publications are cleared. Current enforcement stays inactive. Goal-view contribution affordances and remaining ENT-02/03 bindings follow next.

[Evidence](docs/quality/goal-work-reminders.md).

## Current goal-view evidence (D-146)

Complete original goal progress/history/Home pins remain separate from new-work availability. Historical releases, reviewed writes, dirty currency/culture drafts and growing feedback are verified in actual sources and running Windows/Android apps. Main 2,520 / App.Tests 497 pass; complete signed APK: artifacts/android/zanance-d146-release.apk. Current builds stay unrestricted. Remaining goal-editor and other ENT-02/03 bindings plus external gates remain open.

[Evidence](docs/quality/goal-work-views.md).

## D-147 goal-editor delivery

40 new AT-149 actual-source/real SQLite cases pass. Final normal parallel main 2,560 / App.Tests 537 pass,
zero failures/skips; test output cleaned. Strict Windows and Android builds and canonical Release APK build have zero
warnings/errors. Eighteen English/Persian/German, light/dark, 360/412/wide contexts check the actual Advanced and Simple
modal at process-local 1.5 font stress: complete changed glyphs, real 44-pixel Invoke targets, stale Save rejection,
retained reviewed reload/current saved summary and native Close. All 25 complete fixture tables and original development
data/sidecar bytes are restored. The review uses independent fictitious data and explicit native-effect answers, not
replacement MAUI controls or a production entitlement replacement.

Six normal signed Release owned-emulator cases check the original archived account, uncommon rule, multiple-category
warning, pinned Save and native Close/Back. One actual note-only Save succeeds; complete original rule/categories,
accounts, ledger, allocations and other metadata stay equal. Only that goal note/audit and the previously delivered
D-141 migration differ. Exact owned original bytes/sidecars and Main/English/System are restored; the normal Release
remains installed/stopped. No physical-device, iOS, OS accessibility, provider, performance, Store signing/publication
or commercial-activation acceptance is implied.

Canonical Build-AndroidApk.ps1 produces artifacts/android/zanance-d147-release.apk, package pro.vafadar.zanance,
nondebuggable Release, intact ZIP, full ARM64/x64 assembly stores and application AOT, verified v2/v3 signature with
the existing local test certificate. Bytes: 81491883. SHA-256: `d3aa44afc6f55aae1ea3b4a0fe82700c5120ad196343afcd7cdcfbdf0d6879aa`.
No schema/model/migration, portable preference, security policy, credential, SDK, paid grant or permission change.

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

Evidence: `docs/quality/goal-editor-work.md`.

Budget choices: D-148 supplies canonical definition persistence and actual writer checks, retaining all original
periods/limits and unrestricted test builds. D-149 supplies the chooser; effective view/alert integration remains open.
[Evidence](docs/quality/budget-definition-choice.md).

Budget choices: the D-149 native chooser groups copied periods, retains original scope/method/history and
preserves explicit drafts on failure. Effective budget/view/alert integration stays open; test builds unrestricted.
[Evidence](docs/quality/budget-definition-chooser.md).

Budget alerts: D-150 checks every eligible original current period with guarded spending/carry and privacy,
and retains retryable thresholds after failed or retired publication. Test builds unrestricted. Budget views,
editor and scoped returning-data integration remain open. [Evidence](docs/quality/budget-alert-work.md).

Original budget notifications: D-151 opens the saved period in its original currency/calendar without changing Home preferences. 2,646 / App.Tests 589 pass; complete signed artifacts/android/zanance-d151-release.apk and native emulator routing verified. Commercial budget view/editor bindings and other ENT-02/03 work remain open.

Original budget editor: D-152 retains full original scope/limits and raw input cultures behind a covered read, with paired stale-save rejection and explicit reviewed dirty-only reload. 2,681 / App.Tests 624 pass; strict builds, 18 Windows and six normal Release editor cases plus final Create/Close and weekly Save verified. Budget view commercial copy/delete/work bindings and other ENT-02/03 work remain open. See docs/quality/budget-editor-drafts.md; complete signed artifacts/android/zanance-d152-release.apk.
