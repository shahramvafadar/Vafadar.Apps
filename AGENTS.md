# AGENTS.md – working rules for this repository

This file is the starting point for anyone who works on this repository – a person or a tool, on any machine. It
collects every rule the owner has set so far; follow them without being reminded, from the first message on. Details
live in the linked guides. When the owner sets a new rule, add it here in the same commit as the work it came with.

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

* The owner writes in Persian. **Every message to the owner is in Persian**, laid out right to left: answers,
  explanations, questions, progress notes between steps, short status lines and reports after background work. Only
  code, identifiers, file paths and commands stay as they are. Never switch to English in the conversation.
* **Everything in the repository is English**: code, comments, documentation, commit messages, file names.
* Ask only when a decision is really the owner's (money, publishing, identities, third-party downloads, product
  direction, changes to an approved design). Otherwise choose sensibly, do the work and report what was decided.
* Report honestly: what was verified and how, what was not, and what failed. Never call something fixed before it was
  checked in the running app.

## 3. No attribution – anywhere

* Never mention any tool, assistant, model or its vendor in the repository or its history: no `Co-Authored-By`
  trailers, no "generated with …" notes, no tool names in code, comments, docs, commit messages, pull requests,
  issues, file names or configuration. Commits are authored by the owner only.
* Local helper or configuration files of any tool are never committed and never referenced from tracked files; keep
  them out with `.git/info/exclude`.
* The `pre-commit` and `commit-msg` hooks in `eng/git-hooks` reject attribution lines; do not bypass them. Before
  every commit, check the staged content and the message once more.

## 4. Identity and Git

* Commit only as **`Shahram Vafadar <shahramvafadar@gmail.com>`** and push only to
  `https://shahramvafadar@github.com/shahramvafadar/Vafadar.Apps.git`. The machine may also hold work repositories with
  another identity and GitHub account: never use that identity here, never change the global Git configuration, and
  never use, change or delete stored credentials of other accounts. Setup: `docs/guides/git-setup.md`
  (`git config core.hooksPath eng/git-hooks`).
* Never use `--no-verify`, never force-push `main`, never rewrite published history.
* **Never run `git clean`** – it once deleted every uncommitted file. Remove build output with `dotnet clean` or by
  deleting `bin`/`obj` folders explicitly. `git checkout -- <file>` only for files whose every change is yours.
* **Always commit and push when a piece of work is finished** – one commit per finished, verified step, without
  waiting to be asked. Messages follow [Conventional Commits](CONTRIBUTING.md), e.g. `feat(zanance): add savings
  goals`, then a blank line and bullet points on what changed and why.
* Never push temporary changes made only for checking something (a removed `FLAG_SECURE`, timing output, test data);
  remove them first and confirm with `git diff` that nothing of them is left.

## 5. Secrets, data and permissions

* Never commit secrets. Build-time secrets go through `<AppSecret>` items and the git-ignored
  `Directory.Secrets.props` (template: `Directory.Secrets.props.example`); in CI they come from GitHub secrets
  (`SYNCFUSION_LICENSE_KEY`, Android signing secrets in the `production` environment). Guide:
  `docs/guides/secrets-and-configuration.md`.
* Never print a key, password, token or client id in output or logs – to check one, print only its length.
* Never read, type, paste or write license keys, passwords or tokens for the owner, not even when asked: the owner
  enters them personally (local secrets file, GitHub secrets, store consoles, keystores).
* Never create accounts or sign in on the owner's behalf (Play Console, App Store, Microsoft Entra, Google Cloud).
* Ask before downloading anything that is not restored by the build itself (tools, SDKs, installers, files).
* The Syncfusion license (Essential Studio Enterprise, community license) is registered once in `Vafadar.Maui`
  (`SyncfusionLicense.Register`); every MAUI app gets the key automatically. `Vafadar.SyncfusionLicense.Tests`
  validates it and the release workflow requires it.
* Delete or overwrite only what this repository owns. The development machine's drive can run full: check free space
  before large builds and clean test output (`dotnet clean Vafadar.Tests.slnf`) after test runs. A failed write on a
  full disk can truncate files – check `git status` and restore from Git if needed.

## 6. Build, test, verify

```powershell
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-windows10.0.19041.0 -p:ContinuousIntegrationBuild=true   # Windows
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-android -p:ContinuousIntegrationBuild=true               # Android
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-ios                                                       # iOS (workload required)
dotnet test --solution Vafadar.Tests.slnf                                          # all tests without MAUI
dotnet clean Vafadar.Tests.slnf                                                    # afterwards: free the disk
dotnet ef migrations add <Name> --project src/Apps/Zanance/Vafadar.Zanance.Data --startup-project src/Apps/Zanance/Vafadar.Zanance.Data
./eng/scripts/Run-Snapshots.ps1 -Languages fa [-Theme dark] [-WindowSize 1280x820] [-Empty] [-Only budget,report] [-Mode simple] [-Calendar Hijri] [-Digits latin]   # screenshots of every screen
./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa' -Help [-WindowSize 360x800]   # every "?" help dialog
./eng/scripts/Add-Strings.ps1 -JsonPath strings.json                               # strings in en, fa, de, es, fr and it
./eng/scripts/Build-AndroidApk.ps1                                                 # installable APK in artifacts/android
```

* Build with `-p:ContinuousIntegrationBuild=true` locally: CI treats warnings as errors, a plain local build does not.
* A step is done only when: the Windows and Android builds have **no errors and no warnings**, all tests pass, the
  changed screens were checked in the running app (section 9), the documentation is updated (section 10), and the
  commit is pushed.
* Check CI (GitHub Actions) once after pushing – one background check after 15–18 minutes – and report the result.
  **Poll the GitHub API sparingly** (unauthenticated: 60 requests per hour). Newer pushes cancel older CI runs; that is
  expected.
* Visual Studio may be open on the solution; its background restore can race command-line builds (NETSDK1005,
  APT2126, locked files). Retry once; `Build-AndroidApk.ps1` already does.

## 7. Code rules (beyond `docs/guides/coding-conventions.md`)

* **Comments**: code carries enough English comments for a reader who did not write it. Public and internal types and
  members get XML documentation (`<summary>`); non-obvious logic gets a short comment on *why*, not on what the line
  does; every workaround names the platform behaviour it works around and, where one exists, the decision (`D-xx`).
  Match the comment density of the surrounding code; never leave stale comments behind.
* Architecture: `docs/architecture/overview.md`. Apps never reference apps; `*.Core` has no infrastructure; only MAUI
  projects reference MAUI. Package versions only in `Directory.Packages.props`.
* Money is `long` minor units plus an ISO 4217 code. Dates are `DateOnly`; time comes from an injected `TimeProvider`.
* Ledger rules: a transfer is one entry and never income or spending; refunds reduce spending; plans are not the
  ledger; occurrences are computed and only changed ones are stored; the unique settlement index prevents double
  posting. Keep these invariants when adding features, and prove them with tests.
* Every schema change needs an EF Core migration. Changes are additive; the migration test writes first-schema rows
  with raw SQL, so it keeps working when entities grow. **After every model change regenerate the compiled model**
  (`dotnet ef dbcontext optimize …`, command in `docs/architecture/data-and-backup.md`); a test fails when it is stale.
* Startup: no database work while the app is built (`IMauiInitializeService`, Android `Application.onCreate`); the
  database is migrated in `App.CreateWindow` (D-46). Keep startup work small – measure before and after.
* UI strings: every key in **all** of `.resx` en/fa/de/es/fr/it (tests enforce it); a new string needs a real translation
  in every language – never copy English into a translation file. Spanish uses the informal `tú`, French the polite `vous`, Italian the informal `tu` – use `eng/scripts/Add-Strings.ps1`.
  App strings: `Vafadar.Zanance.App/Resources/Strings/AppResources`; shared strings:
  `Vafadar.Localization/Resources/SharedStrings`. German texts must fit the narrowest layout. Weekday phrases go
  through `WeekdayGrammar` (Italian Sunday is feminine).
* Right to left: amounts go through `MoneyText` (isolates and marks keep their order on every platform); a Persian
  sentence must not start with an amount or a Latin value – rephrase or quote it (« »); show tags with
  `EntryTags.Display`. Windows ignores Unicode isolates without marks. Persian digits are for display only
  (`NativeDigits`); inputs and stored values stay Latin, parsing accepts both (`Digits.ToAscii`).
* Controls: follow D-25 (Syncfusion charts, calendar, progress bars, currency search; native or own controls elsewhere).
  Syncfusion controls that draw themselves do not follow the page direction: mirror them for right to left as
  `SfLinearProgressBar` does (`ReadingScaleX`), and give them explicit semantic colors for the dark theme.
* Colors: only the semantic keys of `Presentation/Palette.cs`, used as `{DynamicResource Key}` in XAML and via
  `Palette.X` in code – no color literals, so light and dark themes both work. A value set in code outranks a dynamic
  resource; set theme colours as dynamic resources, not as local values.
* MAUI: compiled bindings (`x:DataType` everywhere; `RelativeSource` bindings need their own `x:DataType`);
  CommunityToolkit.Mvvm `[ObservableProperty]` on partial properties; a `DateField` must never be bound to an unset
  `DateOnly` (initialize dates).
* Backup (D-62): password encryption is optional for local and connected cloud backups, on by default. Remember
  only the protection choice on the device, never passwords. Explain that an unprotected portable file is readable
  by anyone who obtains it. First-run restore must not force or create an extra account; preserve restored preferences.
  About keeps a compact link to the required bundled notices, without an inline component list.
* App access (D-63): optional four-digit PIN across all profiles on this device; salted verifier and durable growing
  attempt delays only in platform SecureStorage, never portable backups. Current PIN required to change/remove;
  recovery only after successful device authentication plus confirmation. No NotAvailable fallback for a PIN lock.
  Keep the startup frame covered until secure state is read. This gate does not encrypt the database.
* Ownership: owner-written source remains all rights reserved; bundled third-party notices apply to their components
  only and grant no rights to Zanance source. Keep that distinction clear in the app.
* Privacy: no analytics, ads, crash reporting or network SDKs; offline builds declare no `INTERNET` permission
  (cloud backup builds only, D-35). Notifications are generic unless the user allows details. The recent-apps preview
  is always hidden (Android recents exclusion/secure background flags, iOS cover). Android foreground screenshots
  are blocked by default but can be allowed in Settings (D-63); iOS/Windows screenshots are not blocked. Permissions are asked in context, with the platform's own
  dialog after a short explanation (D-38); Android takes photos through the camera app without the camera permission.
* Plan/debt setup (D-65): blank plans start Once; Monthly anchors to the first date in the named rule calendar.
  Keep summary/actual dates and ending count/date visible in both modes, uncommon rules optional. Debt direction
  supplies the sign of positive reference-date input. Estimates, actual repayments and unsaved reminder drafts stay
  separate; never post an estimated installment as principal or create ledger entries when opening a draft.
* Receipt reading (D-64): use complete purchase-total evidence, never the largest item price or an unfiltered last
  number. Preserve OCR line/angle/page relationships and explicit currency/unit; uncertain totals remain for review.
  Recognition uses its own bounded upright image; only the compact metadata-free copy enters attachments/backups.
  Home and attachment rereads never save before confirmation or erase an existing amount when no new total is found.
* Tests accompany every feature; name tests by behaviour and tag acceptance scenarios with `[Trait("AT", "AT-xx")]`.
* The Debug snapshot walk-through (`Diagnostics/DebugSnapshots.cs`) seeds fictitious data only. Add new screens to its
  route list.
* Branding: app icons, splash images, the notification icon and the in-app symbol are generated by
  `branding/zanance/scripts/integrate.mjs` from the approved master (D-26); never edit them by hand or redraw the logo.

## 8. UI and UX rules

The owner reviews the running app and expects polish beyond "it works" (`src/Apps/Zanance/docs/03-ux-design.md`).

* Home shows the whole situation at a glance; frequent tasks (a new expense) take the fewest possible steps.
* Every colour has one meaning (D-27): action blue, green for money coming in, red for problems and debt, amber near a
  limit, violet for plans, teal for savings, slate for transfers, sky blue for refunds and items to review.
* Every setting whose effect is not obvious gets a round "?" (`HelpButton`) with a full explanation and an example
  (`Help_{Topic}_Title/_Text/_Example`).
* Actions look like buttons, and wording says what happens ("Add your first account").
* Everything that reacts to a tap is a real button – keyboard, screen readers, touch feedback (D-42): rows and cards
  carry a transparent `OverlayButton` as their last child; never a tap gesture alone. Aim for touch targets of at least
  44 px; a smaller visual gets a larger touch area around it (the "?" buttons).
* Form errors appear next to their field, all at once.
* Windows: every page opened from another one has a visible back button (`PageHeader`); the focus ring is the
  palette's blue; the window title, dialogs and pickers follow the app's theme and font; the window is never narrower
  than 360 px; Windows ignores the margin of a `CollectionView` item's root – use padding in a wrapper.
* Light theme: page and cards stay distinguishable on any screen; the dark theme is the deep navy of D-38.
* Wide windows show pages as a centred column (`ReadableWidth`, D-41); empty states offer the next step.

## 9. Checking the running app

* Check every visible change in the running app before reporting it: **Persian (right to left), English and German;
  light and dark; a phone width (412 px), the 360 px minimum and a wide window** where layout is affected.
* Windows Debug build: `VAFADAR_START_ROUTE` opens a screen, `VAFADAR_WINDOW_SIZE` sets the size,
  `VAFADAR_CAPTURE_WINDOW` (with `VAFADAR_CAPTURE_DELAY`) saves the whole window and open dialogs as rendered by the
  app itself (`docs/guides/building.md`); `Run-Snapshots.ps1` renders every screen.
* Drive the app only through UI Automation patterns (invoke, select, toggle) and capture only the app's own window.
  **Never take full-screen screenshots, never send mouse clicks or key presses to the owner's desktop and never take
  the keyboard focus from the owner's windows.**
* Android: the emulator may be driven with `adb` (`uiautomator dump`, `input tap`). `FLAG_SECURE` blocks screenshots;
  read the screen with `uiautomator dump` instead of removing the flag. Do not change system settings of a device.

## 10. Documentation duties

**Documentation is complete, in English, and always current** – update it in the same commit as the change:

* `src/Apps/Zanance/docs/01-assessment-and-decisions.md` – every owner decision and every notable design or technical
  choice as the next `D-xx` (what, why, how it was verified);
* `src/Apps/Zanance/CHANGELOG.md` – user-visible changes in plain words;
* `src/Apps/Zanance/docs/03-ux-design.md` – UI rules and patterns;
* `src/Apps/Zanance/docs/spec/Zanance-Product-Specification.md` – Section 31 (status, deviations, test count);
* `src/Apps/Zanance/docs/04-phase-1-plan.md` / `05-phase-2-backlog.md` – slice status;
* `src/Apps/Zanance/docs/06-privacy-matrix.md` and `src/Apps/Zanance/docs/spec/Zanance-Privacy-Matrix-Starter.md` – any new data,
  permission, export or SDK;
* `src/Apps/Zanance/docs/07-acceptance-test-plan.md` – scenario status;
* `docs/guides/*` and `docs/architecture/*` – tools, scripts, commands and data handling;
* `08-release-checklist.md`, READMEs and this file where affected.

The repository copy of the specification is the source of truth; the owner may keep another copy outside the
repository.

## 11. Tooling notes (Windows PowerShell 5.1)

* PowerShell 5.1 reads a script without a byte order mark as ANSI: save scripts that contain non-ASCII text (Persian,
  "–", "→") as UTF-8 **with** BOM. `Get-Content` without `-Encoding` shows UTF-8 files garbled – a display effect only.
* Keep each file's encoding and line endings as they are; do not add a BOM to files that have none.
* Prefer exact text edits for multi-line code changes; building strings with "`n" inside replacement pairs has lost
  text before.
* `Copy-Item` keeps the source's timestamp, so MSBuild may not recompile a restored file – touch it.
* The typographic apostrophe (’) ends a PowerShell string; avoid it in commands.

## 12. Current state and open items

* Phase 1 is implemented, including cloud backup (D-35), which is not yet verified with real OAuth clients; Phase 2A
  is in progress (see `05-phase-2-backlog.md` and spec Section 31). Phase 2B (sync, sharing, bank connections, AI,
  online rates, purchases) needs a separate owner decision per item.
* **Since 2026-10-07 all remaining work is tracked in one canonical backlog:**
  `src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/04-backlog.md` (Free/Plus/Pro, D-61). Work goes one
  approved section at a time (`05-delivery-plan.md`): build, test, document, push, propose exactly one next section,
  then wait for the owner. New languages come last. No commercial limits on test builds before the owner approves.
* Waiting for the owner: Google OAuth client ids (Android, iOS, Desktop app) and the Entra iOS platform for cloud
  backup (D-50); a Mac with Xcode and the Apple signing setup for the iOS build; tests on a physical device; a competitor
  and user-feedback review of reports and KPIs (postponed by the owner).
