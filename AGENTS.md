# AGENTS.md – working rules for this repository

This file is the starting point for anyone who works on this repository – a person or a tool, on any machine. It
collects the rules the owner has set so far; follow them without being reminded. Details live in the linked guides.

## 1. Project

* Monorepo of personal .NET apps by Shahram Vafadar: brand **Vafadar**, domain vafadar.pro, repository
  https://github.com/shahramvafadar/Vafadar.Apps (branch `main`). Public, but **all rights reserved** (not open source).
* First app: **Zanance** – a local-first personal finance manager (.NET 10, .NET MAUI; Android first, then iOS;
  Windows for development). Code and folders use the same name (`src/Apps/Zanance`, `Vafadar.Zanance.*`; the
  earlier working name "Finance" was replaced, D-24). App id / Android package / iOS bundle id: **`pro.vafadar.zanance`** (permanent once published).
  The product name "Zanance" is shown untranslated in every language.
* Requirements: `src/Apps/Zanance/docs/spec/Zanance-Product-Specification.md` (the owner's specification; Section 31
  records the implementation status) and the design documents `src/Apps/Zanance/docs/01…08`.

## 2. Communication with the owner

* The owner writes in Persian. **Answer in Persian**, laid out right to left.
* **Everything in the repository is English**: code, comments, documentation, commit messages, file names.
* Ask only when a decision is really the owner's (money, publishing, identities, third-party downloads, product
  direction). Otherwise choose sensibly, do the work and report what was decided.

## 3. No attribution – anywhere

* Never mention any tool, assistant, model or its vendor in the repository or its history: no `Co-Authored-By`
  trailers, no "generated with …" notes, no tool names in code, comments, docs, commit messages, pull requests,
  issues, file names or configuration. Commits are authored by the owner only.
* Local helper or configuration files of any tool are never committed; keep them out with `.git/info/exclude`.
* The `pre-commit` and `commit-msg` hooks in `eng/git-hooks` reject attribution lines; do not bypass them.

## 4. Identity and Git

* Commit only as **`Shahram Vafadar <shahramvafadar@gmail.com>`** and push only to
  `https://shahramvafadar@github.com/shahramvafadar/Vafadar.Apps.git`. The machine may also hold work repositories with
  another identity and GitHub account: never use that identity here, never change the global Git configuration, and
  never use, change or delete stored credentials of other accounts. Setup: `docs/guides/git-setup.md`
  (`git config core.hooksPath eng/git-hooks`).
* Never use `--no-verify`, never force-push `main`, never rewrite published history.
* **Never run `git clean`** – it once deleted every uncommitted file. Remove build output with `dotnet clean` or by
  deleting `bin`/`obj` folders explicitly.
* One commit per finished, verified step. Messages follow [Conventional Commits](CONTRIBUTING.md), e.g.
  `feat(zanance): add savings goals`, then a blank line and bullet points on what changed and why. Push after each
  commit.

## 5. Secrets and data

* Never commit secrets. Build-time secrets go through `<AppSecret>` items and the git-ignored
  `Directory.Secrets.props` (template: `Directory.Secrets.props.example`); in CI they come from GitHub secrets
  (`SYNCFUSION_LICENSE_KEY`, Android signing secrets in the `production` environment). Guide:
  `docs/guides/secrets-and-configuration.md`. Never print a key in output or logs.
* The Syncfusion license (Essential Studio Enterprise, community license) is registered once in `Vafadar.Maui`
  (`SyncfusionLicense.Register`); every MAUI app gets the key automatically. `Vafadar.SyncfusionLicense.Tests`
  validates it and the release workflow requires it.
* Never enter passwords or tokens for the owner; the owner adds GitHub secrets and signs in personally.
* Delete or overwrite only what this repository owns. The development machine's drive can run full: check free space
  before large builds and clean test output (`dotnet clean Vafadar.Tests.slnf`) after test runs. A failed write on a
  full disk can truncate files – check `git status` and restore from Git if needed.

## 6. Build, test, verify

```powershell
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-windows10.0.19041.0   # Windows (dev)
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-android               # Android
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-ios                   # iOS (workload required)
dotnet test --solution Vafadar.Tests.slnf                                          # all tests without MAUI
dotnet ef migrations add <Name> --project src/Apps/Zanance/Vafadar.Zanance.Data --startup-project src/Apps/Zanance/Vafadar.Zanance.Data
./eng/scripts/Run-Snapshots.ps1 -Languages fa [-Theme dark]                       # screenshots of every screen
./eng/scripts/Add-Strings.ps1 -JsonPath strings.json                               # strings in en, fa and de
```

A step is done only when: the Windows and Android builds have **no errors and no warnings**, all tests pass, the
changed screens were checked in the Debug snapshots (**Persian/RTL at least; dark theme for visual changes**), the
documentation is updated, and the commit is pushed. Check CI (GitHub Actions) afterwards, but **poll the GitHub API
sparingly** (unauthenticated: 60 requests per hour) – one check after 10–15 minutes is enough. Newer pushes cancel
older CI runs; that is expected.

## 7. Code rules (beyond `docs/guides/coding-conventions.md`)

* Architecture: `docs/architecture/overview.md`. Apps never reference apps; `*.Core` has no infrastructure; only MAUI
  projects reference MAUI. Package versions only in `Directory.Packages.props`.
* Money is `long` minor units plus an ISO 4217 code. Dates are `DateOnly`; time comes from an injected `TimeProvider`.
* Ledger rules: a transfer is one entry and never income or spending; refunds reduce spending; plans are not the
  ledger; occurrences are computed and only changed ones are stored; the unique settlement index prevents double
  posting. Keep these invariants when adding features, and prove them with tests.
* Every schema change needs an EF Core migration. Changes are additive; the migration test writes first-schema rows
  with raw SQL, so it keeps working when entities grow.
* UI strings: every key in **all** of `.resx` en/fa/de (tests enforce it) – use `eng/scripts/Add-Strings.ps1`.
  App strings: `Vafadar.Zanance.App/Resources/Strings/AppResources`; shared strings:
  `Vafadar.Localization/Resources/SharedStrings`.
* Right to left: amounts go through `MoneyText` (isolates and marks keep their order on every platform); a Persian
  sentence must not start with an amount or a Latin value – rephrase or quote it (« »); show tags with
  `EntryTags.Display`. Windows ignores Unicode isolates without marks.
* Controls: follow D-25 (Syncfusion charts, calendar, progress bars, currency search; native or own controls elsewhere).
  Syncfusion controls that draw themselves do not follow the page direction: mirror them for right to left as
  `SfLinearProgressBar` does (`ReadingScaleX`), and give them explicit semantic colors for the dark theme.
* Colors: only the semantic keys of `Presentation/Palette.cs`, used as `{DynamicResource Key}` in XAML and via
  `Palette.X` in code – no color literals, so light and dark themes both work.
* MAUI: compiled bindings (`x:DataType` everywhere; `RelativeSource` bindings need their own `x:DataType`);
  CommunityToolkit.Mvvm `[ObservableProperty]` on partial properties; a `DateField` must never be bound to an unset
  `DateOnly` (initialize dates).
* Privacy: no analytics, ads, crash reporting or network SDKs; the Android manifest declares no `INTERNET`
  permission. Notifications are generic unless the user allows details. The recent-apps preview is always hidden
  (Android `FLAG_SECURE`, iOS cover).
* Tests accompany every feature; name tests by behaviour and tag acceptance scenarios with `[Trait("AT", "AT-xx")]`.
* The Debug snapshot walk-through (`Diagnostics/DebugSnapshots.cs`) seeds fictitious data only. Add new screens to its
  route list.
* Branding: app icons, splash images, the notification icon and the in-app symbol are generated by
  `branding/zanance/scripts/integrate.mjs` from the approved master (D-26); never edit them by hand or redraw the logo.

## 8. Documentation duties

When behaviour, data flows or decisions change, update in the same commit:

* `src/Apps/Zanance/docs/spec/Zanance-Product-Specification.md` – Section 31 (status, deviations, test count);
* `src/Apps/Zanance/docs/04-phase-1-plan.md` / `05-phase-2-backlog.md` – slice status;
* `src/Apps/Zanance/docs/06-privacy-matrix.md` and `src/Apps/Zanance/docs/spec/Zanance-Privacy-Matrix-Starter.md` – any new data,
  permission, export or SDK;
* `src/Apps/Zanance/docs/07-acceptance-test-plan.md` – scenario status;
* `src/Apps/Zanance/docs/01-assessment-and-decisions.md` – every owner decision as `D-xx`;
* `08-release-checklist.md`, READMEs and guides where affected.

The repository copy of the specification is the source of truth; the owner may keep another copy outside the
repository.

## 9. Current state and open items

* Phase 1 is implemented except cloud backup; Phase 2A is in progress (see `05-phase-2-backlog.md` and spec
  Section 31). Phase 2B (sync, sharing, bank connections, AI, online rates, purchases) needs a separate owner decision
  per item.
* Waiting for the owner: Google/Microsoft OAuth client ids for cloud backup (S15); tests on a physical device.
