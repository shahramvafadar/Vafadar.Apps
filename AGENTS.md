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
* During approved continuous delivery, prioritize ready code changes and actual observed defects. Repeat or broaden
  verification only after a change, failure or unresolved concern; do not replace delivery with more planning.
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
* Regional display and cloud discovery (D-67): language/RTL stay independent from numeric formatting culture,
  calendar, digit shapes, holiday region, week start and currency. Offer the choices in onboarding and Settings with
  examples; preserve them through language changes and new backup packages. Portable preference sources use an
  explicit allowlist and never include device security or credentials. Connected backup lists load automatically with
  visible loading/empty/error feedback; restore discovery spans profile sets, retention does not. Germany holidays
  are nationwide-only until separately extended. Keep open forms during display changes.
* App access (D-63): optional four-digit PIN across all profiles on this device; salted verifier and durable growing
  attempt delays only in platform SecureStorage, never portable backups. Current PIN required to change/remove;
  recovery only after successful device authentication plus confirmation. No NotAvailable fallback for a PIN lock.
  Keep the startup frame covered until secure state is read. This gate does not encrypt the database.
* SEC-01 research (D-68): encryption probes use independent projects, fictitious directories and a distinct Android
  package; never reference them from the app or touch its database. ADR 0010 remains proposed until owner review;
  its deprecated Community native bundle is not a production dependency. Build proof is not runtime evidence.
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
* Aggregate import (D-72): every overlap requires an explicit link/keep-both choice. Recheck the reviewed rows in
  the write transaction; reduce existing aggregates only by new accepted details, and never link one detail twice.
  Preserve complete metadata and attachment ownership in a durable profile-local Undo journal. Reject stale previews
  and unsafe Undo after later edits/dependent imports; no automatic merge or guessed financial relationships.
* Goal contribution reminders (D-70): optional, off by default, 09:00 device-local on saved rule/calendar dates.
  Suppress inactive/reached/unavailable goals, bound the shared pending queue, and keep text generic unless opted in.
  Taps only open details through app lock. A reminder-only edit preserves the rule; never record financial movements.
* Period review reminders (D-71): profile opt-in, off by default; 09:00 device-local on the first day after the
  financial month closes, using the same display calendar and MonthStartDay as Home/review. Skip empty/finished periods
  and missed times; share the pending queue and privacy choice. A tap opens the currently due review through app lock,
  without completing steps or posting money. Plan reminder defaults do not control this fixed time.
* Receipt reading (D-64): use complete purchase-total evidence, never the largest item price or an unfiltered last
  number. Preserve OCR line/angle/page relationships and explicit currency/unit; uncertain totals remain for review.
  Recognition uses its own bounded upright image; only the compact metadata-free copy enters attachments/backups.
  Home and attachment rereads never save before confirmation or erase an existing amount when no new total is found.
* Tests accompany every feature; name tests by behaviour and tag acceptance scenarios with `[Trait("AT", "AT-xx")]`.
* Application flow tests (D-73): compile actual non-UI app sources in `Vafadar.Zanance.App.Tests` through explicit
  native-effect ports, with real isolated SQLite/localization. Never duplicate application algorithms or create fake
  MAUI controls. Native adapters, Intent delivery, authentication and rendering still require running-app checks.
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

* Before asking the owner to test on an Android phone, always build and provide a complete, signed, installable APK
  with `eng/scripts/Build-AndroidApk.ps1` (Release by default). Give its actual file path and verify the package and
  signature. A Fast Deployment APK or a successful Android build alone is not a phone-test handoff (D-66).
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
  then wait for the owner unless a later explicit instruction authorizes continuous delivery. On 2026-10-08 the owner
  authorized continuing all ready planned work without stopping between sections (D-69); unresolved product, licence,
  provider, spending and release decisions remain owner gates. New languages come last. No commercial limits on test
  builds before the owner approves.
* Remaining owner/external gates: provider registration and real OAuth backup acceptance for each released platform
  and certificate (D-50/AT-59), including Entra iOS; a verified Mac/Xcode and Apple signing setup for the iOS app build;
  tests on a physical device; a competitor
  and user-feedback review of reports and KPIs (postponed by the owner).

## 13. Large-text review (D-77 / D-78)

* Preserve native scaling for readable text. Complete Home actions wrap; plan identity and amount/status have their
  own rows; date/template containers grow. Only decorative glyphs opt out of text scaling.
* Font review overrides are Debug-only, explicitly requested and local to the snapshot process. Never change
  OS/device settings for a review. Temporary native configuration probes must be removed before a finished step;
  an override that pins locale or leaves startup covered is not platform acceptance. Distinguish Windows layout
  stress, native conversion observations and real OS, screen-reader and device acceptance.

* Persistent Add/bulk/Undo actions reserve their own grid rows outside the scroll viewport. Financial identity and
  amount/status use separate rows; do not squeeze the title into a trailing-amount column. Debug review measures
  actual visible native rows, excluding hidden ancestors, and renders only the application's own window.

## 14. Settings publication and live choices (D-79)

* Keep the whole Settings form covered until all preferences, suggestions and device availability reads finish.
  Publish synchronously before enabling input; failures keep the form covered with translated retry feedback.
* Refresh the open form's translated choice captions without changing selection indexes, stored preferences or
  unsaved estimate/reminder inputs. Preserve nested publication guards; do not suppress user writes across awaits.
* The settings-display Debug route checks only fictitious display/loading states, not PIN or permission changes.

## 15. Growing action captions (D-80)

* Long action captions retain native text scaling and wrap inside a growing semantic surface. A real transparent
  button is the last child and owns the unchanged command, enablement, keyboard focus and complete spoken name.
* Keep existing compact bulk font size scalable; provide space through two rows/padding instead of clipping labels.
  Measure actual native captions and command/name/target geometry. Selection/layout review never writes money.

## 16. Growing dates and complete monetary packets (D-81)

* Reserve complete date-part digits at native scale, including entry chrome; reflow whole groups. Preserve real
  inputs, calendar/culture order, valid DateOnly and partial drafts. Calendar targets remain at least 44 px.
* Large readouts retain their original MoneyText packet, scalable display font and complete spoken description.
  Oversized amounts scroll horizontally with a translated hint; do not split decimals or discard a sign/currency.
  Keep navigation buttons off value viewports. Check both real native scroll ends and realized glyph boundaries.
* Debug reviews use fictitious drafts without Save and native Scroll patterns at clamped boundaries. Detached
  text measurement is not proof of the bundled font's realized geometry. Keep negative evidence and original data.


## 17. Retired navigation and Settings construction (D-82)

* Remove translated Title bindings from the retired Shell item/section/content graph before replacing its root.
  Keep active form bindings, deferred rebuilds and the lock cover; do not swallow translation failures.
* Initial Settings property publication is synchronous and write-suppressed, including constructor defaults.
  Opening or reopening the form must not save notification defaults or touch its settings timestamp.
* The settings-reopened Debug route uses native SelectionItem/Invoke patterns and retains old shells explicitly;
  check frozen retired titles, live choices/drafts and unchanged complete preference/account/entry JSON.

## 18. Growing child headers and translated native Back (D-83)

* Windows child headers use a growing Auto row above the same body, preserving inherited bindings and attaching
  once. ReadableWidth always sizes current Content, including header/body together; modal headers remain separate.
* Back names/tooltips follow live translations. Android changes only the native arrow's description through a
  lifetime-scoped toolbar tracker; preserve icons/commands, ignore retired shells and unsubscribe on disposal.
* The headers Debug route measures real glyph/target/name geometry, retained draft/body during own-window resize,
  single attachment after nested return and native Back without stored changes. Keep negative reload findings.

## 19. In-memory estimate drafts on Settings reload (D-84)

* Full covered reload still refreshes preferences/accounts/device availability. Retain only dirty explicit-Save
  estimate text/period/currency against the last published baseline, scoped by profile and settings-row identity.
* Do not reinterpret a retained estimate in a changed default currency or relabel another currency's suggestion.
  Accept a submitted baseline only after successful Save; later typing and retired-context completions stay separate.
* Keep draft state in memory, with no autosave/schema change. Verify actual nested return and complete no-write data.

## 20. Visible growing Insights navigation (D-85)

* Windows root Insights destinations use an Auto row above the same retained body, attached once; native Shell
  TitleView must not clip/hide tabs. Show two columns below 600 px and four in the readable wide column.
* Keep four real native targets of at least 44 px, complete scaled captions, exactly one underline and a live spoken
  selected state. Native invocation must open the existing route without financial/settings writes.
* Centered native TextBlock caret bounds use its actual allocated layout slot; retain trim/viewport/target checks.

## 21. Complete compact budget figures (D-86)

* Budget identities wrap; spending, limits and envelope totals occupy separate full-width readouts. Retain their
  existing scalable body/secondary typography and original MoneyText packet, full spoken description and overflow hint.
* Format signed minor-unit magnitudes after decimal conversion: Int64.MinValue has no positive Int64 counterpart.
  Keep input parser limits, rounding and ledger calculations unchanged. Exercise ISO minor digits and display units.
* Budget review changes fictitious presentation collections only, restores them and compares complete stored data.
  Native fixture preparation/restoration touches only the exact independently owned sample database, never profiles
  or security storage. Failed or interrupted route captures are negative evidence, not acceptance.

Requested Windows snapshot runs hide their own native AppWindow and render its root directly: PowerShell
WindowStyle.Hidden alone does not hide WinUI. Never activate the review window or send desktop input.
Retired pages fail explicitly; do not wait on their stale native scroll controls or count interrupted captures.

## 22. Visible budget period choices (D-87)

* The three budget period decisions use wrapping ChoiceChips rather than a compact scrolling filter strip. Keep
  all original choices, two-way selection, translated selected names, native font scaling and at least 44 px targets.
* Check each actual caption/target within the growing group and invoke the real native buttons. Period navigation
  reloads the existing views without Save; compare complete stored data and restore the original in-memory choice.

## 23. Complete selectable tags (D-89)

* Suggested tag captions wrap inside a bounded growing row, with at least 44 px native targets. Keep raw tag identity
  separate from the direction-safe display marker; forward the original value to AddTag and save only explicitly.
* Native review reacquires regenerated controls, retains the caption checked before Invoke and restores the exact
  unsaved draft plus complete stored rows. Failed prototypes/helpers or unavailable script hosts are not acceptance.

## 24. State-matched transaction details (D-90)

* Name the actual action: Hide details while expanded, More details while collapsed. Keep one growing native action,
  the established Simple/Advanced/receipt/edit visibility policy and every unsaved field; saving stays explicit.
* Native review reads the current hierarchy before acting. An Advanced form can start expanded; do not treat an
  intended close as a failed open. Restore the complete draft and compare complete stored rows without Save.

## 25. Readable first-run restore alternatives (D-91)

* Both onboarding restore choices retain complete growing captions, native targets/names and the existing busy
  binding. Restore/return preserves the same step and every unsaved choice, without forcing account creation.
* The focused onboarding-action review ends before general account/financial seeding. Native disposable-profile
  checks remove only their own fictitious profile and compare the original sample's complete financial rows.

## 26. Complete Home snapshot publication (D-92)

* Publish a complete account snapshot once with fresh contexts, retaining native rows, every value/order/action and
  semantic theme refresh. Materialize before changing the collection; failed enumeration leaves original values.
* Empty goal evaluation avoids ledger work; nonempty goals route the original ledger once to the unchanged balance
  calculator. Actual runtime evidence uses the bound page VM, never a newly resolved transient instance.

* A Windows-only Debug helper's caller uses the same platform boundary. Changes to Debug diagnostics require a
  complete Android Debug build as well as Release; Release excludes those files and cannot verify Debug compilation.

## 27. Complete Home customization (D-94)

* Section identities use a full-width growing row above their original reorder/visibility controls, native text
  scaling/RTL and local 44 px targets. Reset keeps its complete growing semantic name and original command.
* The Home layout Debug review checks real glyph geometry and native move/visibility/Reset persistence only in the
  walk-through's fictitious profile, comparing complete financial rows/other preferences. Preserve original data
  and distinguish process-local layout stress from real platform/font-scale/screen-reader acceptance.

## 28. Hidden Home account view lifetime (D-95)

* Keep the complete account snapshot. Attach its BindableLayout source only while the chosen Accounts section
  can show content; detach/remove views when hidden. Visible reloads keep the same source and native rows.
* Verify actual bound page counts/handlers, native visibility switches and account detail/list navigation, complete
  financial rows and unrelated preferences. Distinguish isolated publication gains from total cold duration/ANR.
  Temporary fictitious count/timing instrumentation is removed before final builds and its own cache is cleaned.

## 29. Growing complete account descriptions (D-96)

* Bound account type/default/excluded/incomplete labels before wrapped measurement; retain full native-scaled text,
  semantic colors, compact grouping and a growing identity above the balance. Do not flex-shrink a measured badge.
* Verify all eight existing presentation flag combinations and genuine Accounts rows. Check realized glyph bounds
  against both native and enclosing MAUI width/height, retain rows, restore the full snapshot and compare stored
  data without Save. A native slot or IsTextTrimmed alone can miss clipping by a shorter MAUI label.

## 30. Complete debt entry action and native modal review (D-97)

* Keep the full debt/receivable action text, native scaling, semantic Secondary appearance and existing command in
  a growing real button. Verify actual glyph bounds, complete spoken name and native open/cancel of the same draft.
* Wait for actual MAUI/native date input arrangement in newly opened animated modals before geometric assertions;
  scroll the reference date through native UIA. Retain full digit checks; do not treat zero-size controls as clipping.
* Opening/cancelling the unsaved debt form must leave complete stored accounts, entries, settings, budgets and
  schedules unchanged, with no principal posting or Save. Keep remaining platform/control acceptance gates open.

## 31. Single readable modal headers (D-98)

* Explicit Shell modal modes retain their own title/Cancel row; underlying ordinary stack depth is not a modal
  test. Ordinary child PageHeader/body/binding context and native Back stay intact.
* Modal title/Close groups wrap without shrinking the real Close button or disabling native text scaling.
  Bound the growing title by actual available width; retain subtitles, complete native peer names and commands.
* Native modal review uses valid route queries, actual glyph/target/no-overlap checks, native cancellation and
  complete nine-source stored-data comparison. Named-route opens are not native button-opening evidence.

## 32. Complete visible plan-field validation (D-99)

* Collect independent applicable field problems before entity mutation/writes. Destination feedback belongs beside
  its input, not behind name/amount early returns. Preserve money/recurrence rules and hidden unknown amounts.
* Invalid Save reveals the first affected input without focus or text replacement. Clearing captions changes native
  layout; resolve fresh geometry, scope requests to visible/latest attempts and remove bounded Android observers.
* Native reviews use invalid blocking input only, verify corrected errors/positions and retain full drafts/stored
  rows. Presentation-only foreign-currency choices never reach persistence and are restored before leaving.

## 33. Complete transaction-field validation (D-100)

* Collect all independent applicable monetary problems before entry/fee mutation. Keep original regional/display-
  unit/ISO parsing and consent/receipt protections; show captions beside their inputs and reopen invalid details.
* Reveal the next affected input without focus or text replacement. Windows caret/layout ordering needs a deferred
  native target request; preserve ordinary keyboard policy and latest/visible guards. Route deferred native exceptions through the awaited action
  guard and expire timed-out callbacks. Android observers stay bounded.
* Native invalid-Save checks retain drafts/full stored rows and restore presentation-only choices. Distinguish
  invalid-Save evidence from valid-edit fee retention and other OS/device/release acceptance.

## 34. Existing transfer fees in Simple (D-101)

* Show an existing destination fee in both presentation modes, preserving its value through unrelated edits.
  Creation remains Advanced-only when no fee exists; explicit blank/zero removal retains the transfer/source fee.
* Prove successful retention/edit/removal with actual native Save over fictitious data and compare financial
  fields/ids plus unrelated rows. Keep this separate from invalid-Save evidence and physical/platform acceptance.
* Native mode choices may advance Settings.UpdatedAt normally. Require exact restored preference values and
  financial rows; record that audit-only difference explicitly rather than claiming every table row is byte-identical.
* AT-106 snapshot fixtures perform valid financial Saves only in their fictitious database. Preserve and restore
  original development files and never apply these fixtures to personal data.

## 35. Visible advance-settlement feedback (D-102)

* Initialize settlement DateFields before binding. Explain reversed periods and invalid/missing bills independently
  beside their fields; distinguish a real no-advance period from reversed input. Keep drafts and correction feedback.
* An idle settlement action can explain invalid fields; reveal the first without moving desktop focus and keep its
  full scaled native caption/name/target. No financial Save proceeds until the existing settlement guard accepts it.
* Zero bills may refund remaining advances; equal bills write nothing. AT-107's valid native Save fixtures touch only
  their fictitious database and verify original financial fields/ids before removing their identified scenario rows.

## 36. Complete occurrence actions and correctly placed errors (D-103)

* Due-item actions keep full native-scaled captions/names/targets. Keep payment and optional override errors beside
  their own inputs, with independent corrections; reveal the actual attempted input without changing desktop focus.
* Initialize occurrence DateFields before binding. Empty override retains the plan amount; payment stays positive.
  Metadata Save never posts entries; partial payment and completion retain the original unique-settlement behavior.
* AT-108 native valid-Save fixtures touch only the walk-through's fictitious database. Compare original financial
  fields/ids/states and remove only the exact identified new scenario rows/state before the next case.

## 37. Complete transaction presentation reuse (D-104)

* Reuse formatted rows only within one source/display snapshot. Every fresh data read and culture/translation/theme/
  display-unit change invalidates the cache; reapply selection from the bulk flow. Keep complete rows/order/filter
  scope and totals. Do not cap results to meet a performance counter.
* Measure the actual bound page and native source separately. Temporary probes are removed before final builds;
  warm Windows improvements do not close cold-start, historical ANR or real device/platform gates.

## 38. Complete indexed bulk selection (D-105)

* Build membership from the entire fresh source snapshot once; keep known selection and prune only removed ids.
  Filtered Select all adds visible known rows without losing hidden selections; unknown ids remain excluded.
* Financial commands keep current snapshot copies, validation, cancellation, Undo and pending-dialog guards. Never
  trade complete selection/results for a timing target. LoadAsync timings exclude later native arrangement/painting;
  keep cold/ANR/platform/device gates open and remove temporary probes before final builds.

## 39. Exact Settings suggestion history (D-106)

* Read every entry in the last three complete financial months in the current calendar/MonthStartDay, using the
  existing inclusive indexed date query. Keep the suggestion calculator, currency/account/history rules and refunds.
* Retain covered complete Settings publication, device reads and explicit-Save drafts; reload never writes an
  estimate or money. Compare bounded and complete history, real bound captions/drafts and unchanged stored data.
  Warm LoadAsync measurements exclude later native arrangement/painting and do not close cold/device/ANR gates.
* Native language review uses the associated WinUI data peer after bounded popup arrangement. Reject retired
  pages/handlers explicitly; never swallow selection errors or replace native selection with property assignment.

## 40. Growing native picker captions (D-107)

* Windows picker selections and popup items wrap inside the existing native control. Keep source items, selection,
  semantic names, inherited fonts/scaling/direction/colors and native expand/selection patterns. Popup rows are at
  least 44 logical units high; extend the existing item style without replacing the native theme template.
* Review actual selected/popup character bounds, fonts and targets. Reselect existing choices through native UIA
  without saving estimates or money; retain complete drafts/stored rows and independently check real language changes.
  The shared presentation requires checks on other picker pages too. Process-local scaling is not OS/device acceptance.
* Native Back reviews use the peer associated with the actual control. Preserve timeout/root/page/navigation evidence;
  never extend the bound or substitute a programmatic return to make a failed native check pass.

## 41. Complete transaction scope and reachable results (D-108)

* Bound complete scope/category/date captions before wrapping; retain independent 44-unit native Clear targets and
  the existing shared command. Do not change the financial scope or hide an active filter to regain space.
* The complete filter form can scroll inside actual page space minus visible action docks, reserving up to 144
  logical units for results. Preserve native typography, horizontal choices and complete result values/order.
* Native review copies fictitious route queries before delivery, checks actual scrolled glyph/target geometry,
  invokes Clear and restores the same complete query/result plus stored rows. Restore failed review data as well.

## 42. Independent saved-filter application (D-109)

* A saved filter replaces its persisted combination; remove unrelated transient report currency, account-set,
  confirmed-only and scope-note restrictions. Preserve the saved dates, kind, categories, account, search, review
  and InTotalsOnly choices. Do not claim unsaved report-only fields are included in the portable saved-filter model.
* Compare the same native saved-filter action before and after independent report scopes. Fictitious review-state
  preparation belongs only to the explicit Debug snapshot route; the reviewed actions never write ledger entries.

## 43. Complete unchanged native transaction sources (D-110)

* Retain a bound grouped source only after comparing complete group captions/boundaries and every ordered row
  object. Equal ids or financial values do not authorize reuse across fresh data/display snapshots.
* Observable selection stays on the same rows. Changed results still publish; never cap or omit rows for timing.
  Remove temporary probes before final builds; synchronous publication timings exclude later rendering/device/ANR.

## 44. Repeat from a new transaction draft (D-111 / approved OD-12)

* Repeat validates applicable base amounts/accounts and opens a detached Plan without ledger or plan Save.
  Monthly starts on the selected date in the named rule calendar; automatic posting/reminders start off.
* Preserve the complete original entry draft on return, disclose unsupported plan fields and share the receipt-unit
  guard. Recheck accounts/original currencies before prefilling; never guess a conversion or move an unpaid draft
  one month forward. The existing recorded-entry Make recurring path remains separate.

## 45. Complete growing transaction category choices (D-113)

* Bound the whole chip before measuring text; reserve spacing inside it and retain native text scaling.
  Each row keeps its own height so short choices remain compact beside wrapped names.
* Preserve original category objects/ids/icons/colors and the real last-child selection button. Check actual
  native glyph/slot/group/viewport bounds, complete names and NoSave; restore complete drafts including rule
  hints and explicit-selection state, and compare complete stored rows. Fixture preparation/restoration may
  touch only its exact independently owned fictitious sample database, never profile/security storage.

## 46. Permanent delivery without interim product policies (D-114)

* The owner will not use the app until the planned sections and phases are complete. Do not implement temporary
  product workarounds in place of the planned final behavior. The interim OD-10 exclusion of financial databases,
  safety copies and caches from OS backups was explicitly declined; determine final backup/key handling with
  completed encryption and recovery. Existing runtime policy stays in place until that permanent decision.

## 47. Current native gesture ownership (D-115)

* Before interpreting Android gesture failures, verify the current input-window owner and touchable IME region.
  One input-method flag or a compressed app hierarchy does not prove that the keyboard is hidden. Keep retained
  last-ANR history separate from current windows; debugger-induced ANRs are negative diagnostic evidence.
* Reuse completed captures when correcting a proof assertion. A hidden IME may be absent rather than marked
  NOT_VISIBLE. Remove failed candidates and exact owned debugger forwards; do not ship speculative workarounds.

## 48. Single complete goal warnings (D-116)

* Each goal card shows its complete existing warning packet once, retaining all distinct funding/date/account
  warnings and the original text scaling, semantic color, visibility, raw command and spoken description.
* Native review keeps bound packets separate from Persian digit shaping; exercise actual warning glyph/viewport
  geometry and restore exact presentation collections/flags plus complete stored rows without Save.

## 49. Final commercial model without test-build activation (D-117 / approved OD-03)

* The owner confirms Section 2 capabilities/tools: paused goals/plans count; Free has one monthly limits-budget
  definition, three templates and one saved filter; financial month start and basic forecast to month end are Free.
* Core commerce policy is independent of Simple/Advanced, purchase verification, membership roles and readiness.
  Match guest grants to exactly one accepted shared space; never spread Plus or shared sync to private profiles.
  Keep retained-data rights after host expiry and Plus Lifetime after Pro expiry; do not invent offline grace.
* Count scope-bound resources, canonical budget definitions and identity seats; never delete/select/modify financial
  data in the pure model. Service enforcement and test-build/release activation remain separate; no limits are
  activated by ENT-01. Paid facts never come from portable financial preferences or backups.

## 50. Actual-file account write transactions (D-118 / ENT-02 in progress)

* Commercial write snapshots bind the exact opened SQLite file, never a subsequently changed profile preference.
  Capture only synchronous cached facts; no network or purchase verification inside a database transaction.
* When enabled after approval, acquire the database writer before quota reads and retain it through Save/commit.
  Creation and unarchive share active capacity; corrections/archive remain possible above quota with scope checks.
  Reject stale/mismatched access, roll back failures and raise Changed only after commit. Test independent providers.
* Current registration remains inactive without a permanent paid grant or extra quota transaction. ENT-02's other
  resources, import/restore/read-only/native paths remain open; this account boundary is not complete enforcement.

## 51. Template and filter slots under the SQLite writer (D-119)

* Enabled template/filter creation counts actual stored rows inside the same actual-file writer transaction.
  Existing ids and explicitly confirmed same-name filter replacements reuse slots, even above the Free quota.
* Check before sort assignment/removal/audit, commit before Changed and roll back failed replacements completely.
  Test independent providers competing for the last slot; do not substitute a store semaphore or fake counts.
* Current registration remains inactive. Retain complete template/query metadata and ledger data; no paywall,
  read-only selection, schema or portable entitlement is introduced. The rest of ENT-02/03 remains open.

## 52. Goal and whole-plan write capacity (D-120)

* Active and paused goals/plans retain slots. Reopening history checks capacity before goal protection/pin changes.
  Whole plan batches use net slots inside the actual-file writer; reject the whole batch before any stored edits.
* Split/resume compares the stored predecessor, verifies the distinct linked continuation and transfers one slot.
  Retain earlier history and move only existing later links atomically; rollback failures without Changed.
* New advanced kinds/tools require the confirmed commercial capability. Retained/history corrections remain Free;
  regional calendars/basic reminders are independent. Current deployment stays inactive. Contributions/allocations,
  occurrence automation, import/restore/read-only and other ENT-02/03 boundaries remain unfinished.

## 53. Current budget definitions under the writer (D-121)

* Budget Save/confirmed Replace checks actual current financial-period definitions before limits or deletion.
  Canonical scope/currency/period/account-set keys ignore row ids, calendars and copied months. Read local time and
  the existing month-start preference without creating Settings. Existing updates retain stored period/currency.
* Retained edits and net-neutral replacements work above quota; new methods/rollover/weekly periods check their
  capability. Failed replacement restores the full previous budget/limits; Changed follows commit.
* Current registration stays inactive. Explicit active/read-only selection and future-period activation remain
  ENT-02/03 work; this boundary is not complete budget enforcement or permission to activate test-build limits.

## 54. Holding permissions and atomic purchase prices (D-122)

* Holding direct creation/conversion/new prices use ManageHoldings; stored corrections/delete/Undo retain Free
  rights with exact-scope membership checks. New reasoned quantity corrections contain no money/group/basis/price.
  Default-location scaffolding supports retained corrections and never grants new holding/event/price rights.
* Capture access before acquiring the existing operation's writer. Validate full quantity history and import ids
  inside it; concurrent sales cannot both consume the same quantity and duplicate imports remain idempotent.
* The purchase editor saves its marked derived price with the event/payment/fee in one transaction. Preserve the
  latest same-type/date price identity/metadata and old non-price overload. Recheck access after SQL before commit;
  failures restore the whole group and Changed follows commit once.
* Current registration remains inactive. Import/Undo keep owned data; explicit read-only new-work classification,
  other ENT-02/03 paths and release activation remain unfinished. No fabricated paid grant or new schema.

## 55. Retained earmarks and complete goal editor Save (D-123)

* Positive earmarks and new configured contribution work require AdvancedGoals; negative releases, stored
  corrections and deletion retain Free rights with exact membership checks. Empty weekly/monthly contribution
  dates remain basic goal scaffolding, independent of regional calendar. New reminders use their separate right.
* Reopening history cannot silently reactivate retained paid contribution work/reminders. Never release old
  earmarks automatically or post a contribution estimate as money. Keep complete plan id/creation metadata.
* Save the goal, Home pin normalization and contribution draft in one actual-file writer transaction, even with
  current enforcement inactive. Reject before pin/protection changes; SQL/access failures roll back all rows.
  Changed fires once after commit. Independent enabled writers recheck capacity and the stored parent plan.
* Current registration stays inactive. Explicit selected read-only items, contribution reminder delivery,
  other ENT-02/03 paths and translated limit feedback remain unfinished; no schema or paid grant is introduced.
