# Full code review – October 2026

Status: **Planned** (2026-10-04). Owner: Shahram Vafadar.

After enhancement ZEX (phases 1–6), every line of the repository is read once more, in a fixed order, to find bugs,
performance problems and code that can be simpler or clearer, to check that every screen uses the best control for its
task, and to review the UI/UX. The review changes no product scope: it fixes and improves what exists.

## 1. Scope

About 57,000 lines of C# and XAML (generated code excluded):

| Area | Files | Lines |
|---|---|---|
| Shared libraries (`src/Libraries`) | 95 | 6,050 |
| Zanance core (`Vafadar.Zanance.Core`) | 75 | 10,630 |
| Zanance data (`Vafadar.Zanance.Data`, without the compiled model and migrations) | 10 | 2,690 |
| Zanance reports (`Vafadar.Zanance.Reports`) | 1 | 180 |
| Zanance app (`Vafadar.Zanance.App`) | 196 | 27,600 |
| Tests (`test`) | 103 | 10,200 |

Generated code (`CompiledModel`, migration designer files) is not read line by line; the migrations are checked for
data safety. Engineering scripts and the CI workflow are part of the last section.

## 2. What every section checks

1. **Bugs.** Wrong results, unhandled cases (empty data, one account, several currencies, Persian calendar, month start
   day, archived items, deleted references), rounding, time zones and "today", null handling, `async void` without a
   guard, races between loads, events not unsubscribed, resources not disposed, missing cancellation.
2. **Performance.** Repeated or N+1 database reads, loading all entries where a filtered query suffices, work in loops
   that can be done once, missing `AsNoTracking`, allocations in hot paths, layouts nested more than needed,
   `BindableLayout` over lists that can grow (no virtualisation), work on the UI thread, start-up time.
3. **Code quality.** Duplicated logic that belongs in one place, dead code, unclear names, missing or outdated
   comments, magic numbers, consistency with the rules in `AGENTS.md`.
4. **Controls.** For each input and list: is the control the best fit (for example `CollectionView` vs.
   `BindableLayout`, `SfComboBox` vs. `Picker`, chips vs. segmented control, `Switch` vs. `CheckBox`, numeric entry,
   date and time fields, charts vs. tables), on Android and Windows, with keyboard and screen reader.
5. **UI/UX.** Clarity of texts in English, Persian and German; help ("?") with examples where a term needs it; empty,
   loading and error states; right-to-left layout and mirrored icons; dark theme and contrast; touch targets of at
   least 44 px; screen reader names; narrow (360 px) and wide windows; Simple and Advanced mode.
6. **Tests.** Each section reviews its own tests: missing cases, tests that pass for the wrong reason, slow tests.

## 3. Sections, in order

The order goes from the bottom of the dependency graph to the screens, so that a fix below is in place before the code
above it is read.

| # | Section | Contents | Lines |
|---|---|---|---|
| CR01 | Shared libraries | `Vafadar.Core`, `Data`, `Localization`, `Maui`, `Documents(.Maui)`, `Backup(.GoogleDrive/.OneDrive)`, `Authentication(.Maui)` and their tests | 6,050 |
| CR02 | Core – money and ledger | `Money`, `Ledger`, `Accounts`, `Categories`, `Rates`, `Settings`, `Dashboard` | 3,160 |
| CR03 | Core – plans and budgets | `Plans`, `Reminders`, `Budgets`, `Forecasts`, `Receipts` | 3,110 |
| CR04 | Core – goals, holdings, reports, files | `Goals`, `Holdings`, `Reports`, `DataFiles`, and the PDF project `Vafadar.Zanance.Reports` | 4,540 |
| CR05 | Data layer | `ZananceStore`, `PlanStore`, `GoalStore`, `HoldingStore`, `AutoPostProcessor`, configurations, backup summary, migrations (data safety) | 2,690 |
| CR06 | App foundation | `App`, `AppShell`, `MauiProgram`, `Presentation`, styles and resources, `Platforms`, `Security`, `Reminders`, `Profiles`, `Diagnostics` | 4,970 |
| CR07 | Recording | Home, Transactions, entry editor and details, split, templates | 5,450 |
| CR08 | Accounts and holdings | Accounts, account detail and editor, loans, categories, rates, holdings | 4,630 |
| CR09 | Plans and budget | Plans, plan editor and details, occurrences, settlement, budget and budget editor | 3,890 |
| CR10 | Insights | Goals, forecast and snapshots, reports, KPI sheets, period review | 5,390 |
| CR11 | Settings and data | Settings, backup and restore, import/export, onboarding, profiles, More, About | 3,280 |
| CR12 | Cross-cutting UI/UX and release | Walk-through of every screen (en/fa/de, light/dark, 360 px and wide, Simple and Advanced), consistency of controls and texts across screens, accessibility, engineering scripts and CI | – |

## 4. How each section is done

1. Every file of the section is read completely; nothing is sampled.
2. Findings are recorded in §5 with an id (`CR07-03`), the file and line, the kind (bug, performance, quality,
   control, UI/UX, test) and the severity:
   * **High** – a wrong number, data loss or a crash;
   * **Medium** – a visible problem or a clear performance cost;
   * **Low** – clarity, consistency, small improvements.
3. Bugs and clear improvements are fixed in the section. Every fixed bug gets a regression test where it can be tested
   outside the UI. A change of behaviour or design the owner has not decided (for example replacing a control the
   owner knows, or changing a stored format) is listed as a question for the owner and only done after approval.
4. The section ends like a ZEX phase: the full test suite, the Release builds of the CI, a Windows walk-through of the
   changed screens (English and Persian, light and dark), the documents updated, commit and push, one CI check, and
   for sections that change the app (CR06–CR12) a signed APK for the owner's phone. The next section starts after the
   owner's check.

Out of scope: new features, package upgrades (asked separately), changes to the backup or CSV format without a
migration and the owner's approval.

## 5. Findings

Filled in section by section.

| Id | File | Kind | Severity | Finding | Resolution |
|---|---|---|---|---|---|
