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
* On 2026-10-14 the owner requested one large end-to-end delivery at a time: plan the remaining
  batches first, implement the whole batch, then perform its final checks. Do not finish another
  isolated helper slice or repeat successful unrelated verification.
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
  database is prepared after window creation by the covered startup worker (D-222). Database path
  resolution must not move files; legacy naming upgrades run under drained maintenance ownership
  in that worker (D-262). Keep startup work small – measure before and after.
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
  Device-only authentication prompts also require Success: NotAvailable or Failed must not remove the cover or release pending work (owner decision 2026-10-14, D-266).
  Keep the startup frame covered until secure state is read. This gate does not encrypt the database.
* SEC-01 research (D-68): encryption probes use independent projects, fictitious directories and a distinct Android
  package; never reference them from the app or touch its database. The owner selected current official Community
  sources on 2026-10-12 (D-180); final key/recovery/migration and runtime acceptance remain open;
  the deprecated proof bundle remains excluded from production. Build proof is not runtime evidence.
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

## 56. Bound recovery destination and retained import rights (D-124)

* Bind the actual database context before reading restore input; use that same context for native replacement and
  migration. A profile change during input cannot select another destination. Verify cached access before native
  copying/output, without network work or a writer transaction around SQLite's backup API.
* Database backup/restore and owned import preserve data above quotas and after host expiry; exact accepted
  membership remains required. Portable data never changes paid facts. Replace only Zanance's registered database
  source, preserving other sources. CommercialFileAccess is shared by writers and the database recovery source.
* Import and durable Undo check under their existing actual-file writer before rows/journal reads. SQL failures or
  retired facts roll back aggregate/detail/journal changes with no Changed. Preserve explicit overlap decisions.
* Registration stays inactive. Read-only import/restore classification, selected resources, profile creation,
  automation and other ENT-02/03 paths remain open. Native replacement plus a later failed migration still requires
  the established safety-copy recovery; this does not make the whole portable package an atomic transaction.

## 57. Serialized retained category patterns (D-125)

* Category rule matching/replacement holds the actual SQLite writer, including unrestricted test builds. New text
  or kind requires CategorizationRules; changing the category of a stored same-text/kind pattern is a correction,
  including the existing new-id replacement UI path. Retain current trim/case matching and replacement identity.
* Check cached exact-file rights before draft trim/deletion/audit, preserve CreatedAt on an existing-id update,
  and recheck after SQL before commit. Failed replacements/deletes restore complete rows with no Changed.
* Rule deletion keeps owned-data rights and never reclassifies stored entries. Registration stays inactive;
  rule suggestion delivery/selected read-only classification and other ENT-02/03/04 paths remain unfinished.

## 58. Complete ledger Save groups and refund totals (D-126)

* Classify and validate Save under the actual SQLite writer before reads, including inactive test builds.
  New split groups/parts require SplitTransactions; retained part corrections and joining keep Corrections.
  Complete groups include untouched stored siblings. Holding payment/fee groups use their holding right;
  transfer fees remain part of retained transfer corrections. Ordinary new work requires Transactions.
* Validate refunds against the final batch, excluding edited/deleted old rows and including new peers/originals.
  Recheck edited purchases with retained refunds; missing originals keep translated validation errors.
  Widen exact minor-unit totals before addition/comparison so positive Int64 totals cannot wrap.
* Entries, derived paid totals and attachment moves commit together after cached access recheck, or roll back
  with no Changed. Deployment stays inactive. Ledger delete/Undo, selected resources, automation and other
  ENT-02/03/04 paths remain unfinished; this is not commercial activation or device/release acceptance.

## 59. File-bound deletion and retry-safe Undo (D-127)

* Delete and Restore hold the actual SQLite writer before reading groups, existing ids or occurrence state,
  including inactive builds. DeleteData and Corrections retain owned-data rights; exact membership still applies.
* Return committed deletions with their original file and explicit retained refund links. Never keep session-global
  refund-id relationships or guess a link; reject a returned batch targeting another profile. Restore only missing
  identities and relink only newly restored purchases. SQL/access failures preserve the batch for retry with no Changed.
* Keep receipts' established orphan/startup-purge policy; no attachment bytes are copied into the Undo offer.
  Occurrences, paid totals and refund links share the transaction. No-op/repeated Undo makes no write or Changed.
* The app permits one Undo at a time. Failure keeps the original eight-second deadline; success clears only its
  own offer. Completion never dismisses a replacement or revives a dismissed offer. No schema/portable paid facts
  or activation changes. Selected read-only data, automation and other ENT-02/03/04 paths remain unfinished.

## 60. Guarded native transaction Undo (D-128)

* The existing transaction notice binds the actual TransactionUndoViewModel command. Keep action, refresh and
  native failure feedback inside one execution guard, with no second invocation while a dialog is pending.
* Non-fatal Undo or list-refresh failures use IAppInteraction and the existing translated generic message. Refresh
  only after Undo returns successfully; a failed action retains its original offer/deadline. A refresh failure after
  commit never recreates the consumed offer. OutOfMemoryException propagates; execution guards always release.
* Compile the actual bound command in application tests with explicit native-effect ports and isolated SQLite.
  Review installed Release failure dialogs using a trigger only in the exact owned fictitious sample database;
  retain complete row/receipt comparisons and restore original bytes/display choices. No production fault switch.
* Later refund edits during Undo, visible ledger Save feedback and remaining commercial paths stay unfinished.

## 61. Immutable short ledger Undo and later financial edits (D-129)

* The committed deletion batch owns independent entry/tag copies; indexes and enumeration return fresh copies.
  Caller mutation or failed EF auditing cannot alter the original snapshot. Retain explicit refund expectations,
  creation identity and account currencies in this short in-memory batch, never receipt bytes or a portable journal.
* Before any restore write, compare current retained refunds and partially restored siblings under the same writer.
  Reject the entire obsolete Undo after edits, deletion/recreation or explicit relinking; preserve every later row.
  Compare complete semantic metadata plus CreatedAt, allowing only UpdatedAt auditing. Do not guess a relationship.
* Account currency changes or missing accounts require review; naming/archival alone still permit owned recovery.
  Fully restored batches remain no-ops after later edits. Plain row enumeration keeps its established row-only path.
* Native conflict fixtures edit only exact fictitious refund rows from an after-delete trigger. They prove the
  installed guard and error delivery, not native editor timing inside the eight-second window. Restore original data.

## 62. Visible transaction Save failures (D-130)

* Keep Entry SaveError in the growing fixed footer beside the original Save action, outside the body viewport.
  Retain complete wrapping/scalable text and spoken description. Ledger rejection, storage failure and receipt-unit
  review use the existing error value; field-adjacent input errors keep their established reveal behavior.
* A rejected Save preserves the complete draft and stored rows. Retrying still requires explicit Save; the next
  input-validation attempt clears obsolete general feedback. No alert, focus replacement or new write is introduced.
* The entry-save-feedback Debug route invokes actual native Save on fictitious rows, reads complete table/receipt
  values, and checks error glyphs/names plus the enabled 44 px Save at both native scroll ends. The exact sample
  trigger is Debug review only and is removed in finally. Full Release review uses only the owned sample database.

## 63. Explicit selected resources and retained financial settlement (D-131)

* Availability projects original scoped identities; never choose the first items automatically or mutate stored
  pause/archive/end state. Paused goals/plans consume slots. Missing/stale/oversized limited choices require review;
  unlimited upgrades ignore earlier limited choices. A selection never grants a feature, membership or host right.
* New ledger money checks selected source/destination accounts under the same actual SQLite writer. Owned corrections,
  reconciliation, linked refunds, transfer fees and Delete/Undo retain their existing rights. Current registration stays
  inactive; cached selection is immutable/value-equal, exact-file scoped and never portable paid data.
* Retained overdue payments require actual stored rule/slice, effective past due date, open state, matching account/kind
  and explicit review. Caller markers, future/moved-future/unreviewed/skipped/completed occurrences cannot bypass limits.
* Final advance bills recompute from stored rows under the same writer; compare reviewed plan/advance metadata and net
  paid total before creating only the difference. Stale review/storage/access failures preserve the explicit-Save draft.
  No automatic merge or arbitrary correction grant. Other resource bindings, choice UI/persistence and automation remain open.

## 64. Actual retained manual debt closure (D-132)

* Recognize manual reviewed Loan/Lent closure from the stored known principal and complete resulting batch inside
  the existing writer. Multiple payments and actual Fees expenses must exactly close it; never trust draft markers,
  estimates, guessed FX or changes to old rows that manufacture a baseline. Widen totals before negation/addition.
* Cash/checking/savings may fund closure; new card debt or other borrowed/lent counterpart work still requires its
  own feature/selection. Explicit net-off may close both original principals. Exact membership remains required.
* Keep ledger validation, atomic rollback and Changed after commit; no automatic archival or inferred relationship.
  Current registration remains inactive. Selection UI/persistence, other bindings and occurrence automation remain open.

## 65. Owner-authorized unattended delivery (2026-10-11)

* The owner authorizes independently performing all necessary approved implementation and verification actions
  while away. Do not wait for repeated routine permission; continue concrete planned work, tests, builds, safe
  native reviews, complete APK delivery and finished commits/pushes under the existing repository rules.
* This continues D-69, without temporary product policies or test-build commercial activation. New spending,
  identities, licensing, unresolved product direction and publication remain genuine owner decisions.

## 66. Retained occurrence corrections and explicit links (D-133)

* Bind Skip/Unskip/change, settlement repair and history-free deletion to the actual file's cached membership
  and the same SQLite writer. Corrections need no selected account or paid host. Reject retired facts after SQL
  with complete rollback; Changed follows commit. History check and deletion share the writer.
* Explicit linking rechecks the stored rule/slice and actual entry kind/accounts/destination/partial/link markers.
  Never steal another occurrence's payment. Entry markers and state commit together without posting another entry
  or changing financial metadata. Preserve normal idempotent links and actual paid totals.
* Current registration stays inactive. D-134 completes the new/full/partial settlement writer boundary.
  D-135 completes reviewed reopening; D-138 completes automatic posting. D-139 completes selected future occurrences; D-140 completes actual scoped Plan reminder/snooze delivery. Choice UI/persistence and other bindings remain open; no full enforcement claim.

## 67. Atomic new occurrence payments (D-134)

* New full, partial and final payments recheck the stored rule/slice, kind/accounts/destination and actual state
  under the same actual-file writer as ledger validation and Save. Never overwrite an existing entry identity.
* Classify partial/final payment from actual linked partial money and Core effective amount/date rules, preserving
  explicit actual amount/date. Commit money, paid total and settlement together; failures/retired facts roll back
  all rows and publish no Changed. Keep concurrent settlement uniqueness and legacy repair.
* Current registration remains inactive. D-135 completes generated-entry reopening. D-139 completes selected future occurrences; D-140 completes scoped reminder delivery. D-141 supplies account/Plan choice persistence; chooser UI remains unfinished; D-136 supplies command feedback and D-138 automatic posting.

## 68. Reopening the exact reviewed settlement (D-135)

* Bind the reviewed EntryId, actual settled state and entry plan/date/partial markers to one actual-file writer.
  Reject stale identities/relationships before deleting or unlinking another payment. Retained history remains
  correctable without account selection or paid host; require exact membership.
* Generated reopening reuses the established complete ledger deletion pipeline, including groups, paid totals,
  refund links, receipt ownership and file-bound Undo journal. It already reopens states; no second post-commit write.
  Keep actual manual/imported money and metadata, releasing only its checked link atomically with the opened state.
* SQL/access failure rolls back all complete rows before notifications. Earlier partial payments and occurrence
  overrides remain. Undo retains automatic-post suppression. Current registration stays inactive; D-139 completes selected future occurrences; D-140 completes scoped reminder delivery. D-141 supplies account/Plan choice persistence; chooser UI remains unfinished; D-136 supplies command feedback and D-138 automatic posting.

## 69. Occurrence action failure and publication feedback (D-136)

* Keep one gate through consent, actual writer, error feedback and successful publication for occurrence actions.
  Cancellation/rejection retains the draft and page; ledger errors stay inline. Release busy in finally.
* Retire the writable reviewed snapshot before fallible post-commit navigation/reload; publish a replacement only
  after dependent reads succeed. Distinguish saved-but-not-refreshed feedback from rejected writes.
* Reopening consent uses explicit Reopen/Cancel captions, distinct in all six languages. No commercial activation.
* Preserve negative parallel SQLite cleanup/migration evidence. Owned output-only serialization is test evidence,
  not a fix to pool cleanup isolation or a product policy. Zero-test attempts are not acceptance.

## 70. Independent local SQLite pools (D-137)

* Never clear process-wide SQLite pools for profile switching, deletion, restore or test cleanup. Retire only the
  named full-path file's known pools; custom connection-string owners clear their exact pool themselves.
* Restore clears the actual captured destination connection and migrates the same context after input, even if
  the current profile moves. Keep authorization checks and online-copy/integrity behavior.
* Dispose test-owned providers before clearing only their existing owned files, including nested fixture/profile
  folders. Real TEMP-table markers verify foreign session retention; keep normal parallel execution and explicit
  concurrent writer tests. Preserve historical negative/zero-test evidence; do not turn serialization into a fix.

## 71. Actual automatic payment candidates (D-138)

* Discovery never authorizes money or supplies final values. Recheck stored rule/slice, active state, current due/amount, accounts and actual partial/full money under the ledger writer. Partial manual money, suppression and blocked rights require review; settled/removed/paused/future candidates post nothing.
* Keep original transfer/unreviewed semantics, atomic state/money, rollback and Changed after commit. Enabled cached facts require exact membership, explicit selected plan and AdvancedPlans; choices never grant rights. Test registration stays inactive. D-139/D-140 complete selected generation/reminder delivery; D-141 supplies account/Plan persistence; chooser UI and other selected bindings remain open.
* Native fixtures must select their exact independently owned profile through real UI before claiming its financial results. Preserve failed setup evidence, restore original active profile and exact owned data/display state.

## 72. Selected future plan work and retained calculations (D-139)

* Project only new actionable occurrences through exact-file cached plan choices. Keep original schedules/states
  for history, forecasts, budgets and KPIs; never invent choices, end/pause plans or erase money after downgrade.
* A dated ended slice can use only its unique non-overlapping selected continuation; ambiguous branches/cycles
  grant nothing. Paid recurrence needs its own feature; paused plans still consume slots. Effective past due dates
  retain explicit payment rights. Today's/future payments require actual plan work rights under the ledger writer.
* Generic new payment markers obey the same stored rule/slice/kind/accounts check as plan actions. Preserve existing
  account rejection feedback before additional plan checks. Registration stays inactive; choice persistence/UI
  and other selected bindings remain open. D-140 supplies actual scoped reminder/snooze delivery beyond the Core projection.

D-139 also applies D-82 navigation retirement to profile/onboarding root replacement, before old native handlers
are detached. Keep current forms, deferred display rebuilds and the startup lock cover unchanged.

## 73. Scoped native Plan reminders (D-140)

* Filter actual Plan/contract delivery before queue caps using the exact opened file's work snapshot. Keep original
  group member ids/dates in bounded versioned native links; a file hash is identity only, never authorization.
* Snooze rebuilds current rule/slice/state/choice/privacy/text, preserving its deadline and original members.
  Do not guess unscoped legacy groups/profiles or keep private cached text after a privacy change. Reserve the
  snooze id marker in generated occurrence ids. Pending delivery/taps/actions never move due dates or post money.
* Validate file/rights before and after native publication; cancel retired pending queues and membership-denied
  delivery. Taps recheck the current file after unlocking and never silently switch profiles. Current commercial
  registration remains inactive; D-141 supplies account/Plan persistence; chooser UI and other selected bindings stay open.

D-140 native review also requires retaining Android's first registered notification category while replacing its
complete action list. The 14.1.2 adapter appends categories rather than replacing them. Receiver initialization
registers translated actions without database work; Apple keeps its existing category refresh boundary.

Inexact Android requests use a bounded one-hour delivery acceptance window; the library default of one minute
can discard normal late OS delivery. Keep the existing inexact scheduling and permission policy (D-140).

Validate before and after publication through the same queue-retirement guard. A rejected pre-publication read
must cancel its earlier pending queue too, rather than retaining unavailable old work (D-140).

## 74. Durable financial choices (D-141)

* Store choices only in the actual profile database under the verified resource/scope key. Never invent a main
  identity, retag restored scope or persist paid/membership/security facts. Registration stays inactive.
* Reviewed Save rechecks actual file, rights, original complete identity/name/state list, revision and capacity
  under the SQLite writer. Preserve explicit empty/no-op and reject stale/ABA; Changed follows a changed commit.
* Persisted choice overrides cached choice only. New account/unarchive/Plan joins a free chosen slot atomically;
  archive/end releases its own identity and a checked split transfers only its selected predecessor slot.
  Pause consumes capacity. Malformed choice blocks bounded new work, not history correction; repair is explicit.
* Capture the stored Plan revision in native preparation and validate before/after publication; refresh on choice
  changes. No choice Save posts money or rewrites original states/calculation input. The reviewed UI follows section 75.

## 75. Reviewed growing active-item form (D-142)

* Keep original identity/details/state separate from selection. Pause counts without resuming; historical rows stay
  visible and unselectable. Current inactive release explains unrestricted availability without a required subset.
* Cover complete loading/failure, translate retry, preserve exact unsaved choices/index on errors and live display
  refresh, and Save only explicitly through the actual-file reviewed writer. Retire a committed review before return.
* One busy guard spans discard, reload/switch, write and navigation. Save/reload grow outside the scroll viewport;
  complete row captions have one real native button/name and at least 44-pixel targets. Save failure feedback stays
  beside the pinned action, outside the scroll viewport. Never post money here.
* Debug review uses an independently owned artifact database and page-local fictitious Free input, never a runtime
  entitlement replacement. Registered Settings/Help/Back review uses the unchanged inactive production registration.
  Compare all 25 complete tables; preserve original development data/sidecars and restore display theme as well.

## 76. Durable selected goals (D-143)

* Goal choices use the existing exact-file/scope reviewed row, retaining original measurement/currency/state.
  Selection grants no paid tool. Pause consumes its slot; unselected paused goals cannot resume.
* Read choices under the actual writer. New/reopened goals join a free chosen slot atomically; completion/archive
  and explicit deletion release only that identity. Keep original history, corrections and negative earmark releases.
* Positive earmarks independently require the actual paid feature and usable goal/account choices. Never post ledger
  money here. Roll back complete goal/contribution/pin/choice writes on failure; Changed follows committed writes.
* Registration remains inactive. D-144 delivers goal chooser UI; effective contribution/reminder projection remains open.

## 77. Reviewed goal choices (D-144)

* Goals are the third growing choice in the existing reviewed form. Keep original measurement, currency and state
  separate from selection; quantity goals display their measurement rather than a money currency. Use Completed
  for completed goals. Pause counts without resuming; historical identities stay readable and unselectable.
* Retain covered publication, explicit actual-file Save, pinned failure feedback, exact drafts/live index, shared
  discard/busy guard and committed-review retirement. No selection/navigation/display action posts financial data.
* Current registration stays unrestricted. Selection never unlocks paid goal tools. Effective contribution/reminder
  projection and other remaining resource bindings are separate unfinished work, not proof of activation.

## 78. Actual goal work and scoped reminder delivery (D-145)

* Read complete original goals/rules/allocations; project selected new work separately. Never filter financial
  funding/protection/history inputs. Pause consumes capacity without resuming; choosing a goal grants no paid tool.
* New paid contribution work checks the original selected goal under the actual writer, including a newly joined
  paired goal slot before pin/protection normalization. Retained corrections/disabling/removal stay available.
* Filter eligible reminders before the cap; retain opt-in, generic/detail privacy and 09:00 local saved rule dates.
  Bound original goal/date members plus opaque actual-file identity; resolve after unlock without switching files.
* Validate actual file/rights, stored revision and original goal/rule/allocation stamp before/after native publication;
  clear retired queues and failed preparation. No schema, portable security or financial movement. Registration stays
  inactive; goal-view affordances and other bindings follow separately. Native stored requests/Intent observations
  do not prove future alarm firing, physical devices or iOS.

## 79. Original goal views and reviewed actions (D-146)

* Keep every original goal/progress/history and Home pin separate from new-work availability. Only new suggestions
  and explicit new actions use work rights. Long Home goal identities wrap; financial tone and complete packets stay.
* Retained earmarks can be released from historical/unavailable goals and original archived funding accounts. A new
  release-only form starts with Release/original funding; reload keeps explicit direction/account/amount. Never move ledger money.
* Recheck the captured actual file/rights/choice, complete goal/rule/allocation metadata and account/holding units
  under the same detail writer. Use stable factory-bound connection settings across auto-closed reads/open native
  SQLite writers; another actual file must still be rejected. Equal audit times never authorize stale note/pin replacement.
  One busy guard spans consent/write/publication.
* Retain dirty culture/currency drafts; changed currency requires explicit reentry. Rejected writes keep local state;
  committed reload failure disables writes and exposes growing feedback/retry outside the body. Retry never reposts.
* Native semantic colors/query interfaces remain real MAUI partials; application tests compile actual non-UI sources.
  Review uses independent fictitious data, restores all original rows/bytes/display and never activates commercial limits.

## 80. Reviewed goal editor and explicit budget cuts (D-147)

* Keep complete original goal/rule/end/calendar/categories/hidden method/assumption and archived references. A metadata
  correction of a legacy no-plan goal creates no plan. New monthly drafts retain their explicit named anchor.
* Cover complete publication. Preserve raw input/culture/unit and only dirty fields on explicit reload; changed units
  require reentry and changed original inputs require review of current saved values. Empty native modal-attachment
  queries retain the opened identity; another explicit identity fails. Live captions never reinterpret unsaved input.
* Separate retained corrections from new contribution/reminder configuration; paused configuration never resumes or
  prompts. Recheck the actual file/rights/choice/full inputs under the paired writer; retire a committed draft before
  failed navigation. One busy guard spans permission/confirmation/write/return. Never post ledger money here.
* Apply to budget confirms the original category limit and rechecks full budget/period/day/category under one actual
  writer. Change only that limit; no goal/plan/allocation/ledger write or repeat of an unchanged already applied cut.
* Native review measures the changed actual captions after layout/real scroll realization, including Simple mode,
  uses an independent fixture and restores complete original data/display. Failed helpers are negative evidence.

## 81. Canonical selected budget definitions (D-148)

* Choose exact scope/currency/period/account-set definitions, never dates, calendars, paid methods or hashed row ids.
  Keep all original history/limits separate from current work; selected definitions grant no paid tool or membership.
* Reviewed choice Save rechecks full periods/limits, date/month start, actual file/access and revision/value under
  the writer. Preserve explicit empty/no-op and malformed repair; reject stale/ABA without financial changes.
* New Save/Replace joins or transfers chosen capacity only when the old definition's last current row retires.
  Retained same-definition corrections stay available. Delete uses the same actual-file writer/membership right
  and releases only its final current definition; failed SQL restores limits and choice together.
* Registration stays inactive. The budget chooser follows section 82; view/alert consumers follow separately. Native unrestricted
  Save/Delete is not enabled chooser or physical/iOS acceptance; restore exact owned data and display state.

## 82. Reviewed budget definition choices (D-149)

* Group original copied periods by canonical currency/period/account set without inventing display scope authority.
  A real original row id relays the native command only; Save stores definitions in the reviewed verified scope.
* Preserve original archived/missing account references, methods and history; choices grant no paid method and
  change no financial limit. Display count digits separately, never rewrite user-written account names.
* Recheck complete reference metadata with original periods/file/access/date/revision/value under the writer.
  Shared covered reads, explicit discard/Save/Reload and pinned failure feedback retain exact drafts.
* Registration remains inactive. Native fixture Save changes only its choice and restores complete original data;
  normal Release shows all current definitions without requiring Save. View/alert work bindings follow separately.

## 83. Guarded current budget alerts (D-150)

* Inspect every original current period/calendar/currency with separate chosen new-work rights. Default new-budget
  settings never filter saved alerts; choosing activity never filters financial spending/carry inputs.
* Match carry to the canonical account definition and retain original consecutive period calculations. Reject
  ambiguous periods rather than guessing a row. Bound ledger reads to current/carry ranges off the UI thread.
* Recheck actual file/access/choice revision/value, complete periods/references, date/month-start, financial inputs
  and privacy before/after native effects. Commit device levels only after successful complete batch validation.
* Consume the native SDK request result explicitly; a false result must not consume a budget level.
* On partial/retired publication, remove only attempted exact notification ids and leave levels retryable.
  Preserve generic/details opt-in, refund resets and unrelated notifications. Registration stays inactive;
  original scoped Budget returning-data follows D-151; view/editor affordances remain unfinished.

## 84. Original budget notification destinations (D-151)

* Budget alerts carry the opaque actual-file identity and original period id; resolve after unlock and again in
  the budget page. Deleted/foreign/error targets never guess another period or profile.
* Keep original currency/calendar/custom period dates. Historical viewing grants no new work; notification
  navigation never saves Home currency. Weekly rows still prepare valid original-calendar month navigation.
* The budget-reminder Debug route uses fictitious original rows, real native period invocation and no Save.
  Keep failed/transient captures as negative evidence; wait for regenerated native bindings before capture.
  Commercial view/editor bindings remain open and current release registration stays inactive.

## 85. Reviewed original budget editor drafts (D-152)

* Publish complete original references/preferences behind the cover; construction and opening never create Settings.
  Keep archived/missing limits and exact explicit account scopes, including hidden Simple-mode originals.
* Pin raw amount culture until user retyping; live captions never reinterpret input. Show field errors together.
  Dirty-only reload merges unchanged current fields and requires explicit review when saved inputs changed.
* Recheck complete original review under the actual Save/Replace/Delete SQLite writer. Reject retired file/access/
  choice/date/references even at unchanged timestamps. Keep correction rights separate from new paid tools/capacity.
* New weeks/two-week rows retain canonical zero year/month, never navigation month coordinates.
* Retire a committed draft before native return; failed return uses Close without another Save. Busy native Back
  shares the same discard guard. Review fixtures are independent; restore complete rows/bytes/display.
  Current registration remains inactive; budget view copy/delete/work bindings remain open.

## 86. Reviewed original budget view actions (D-153)

* Render every original financial input independently of work selection. Publish original action rights only after
  complete actual-file periods, references, preferences and presentation reads validate; failed loads retain history
  but require Retry. Opening never creates Settings.
* Bind Copy/Delete to the displayed review, including both source and replacement. Validate before/after native
  confirmation and under the existing paired writer; same-timestamp changes, retired publications and foreign files
  require a fresh explicit read, never a guessed period. Preserve named calendars and canonical weekly coordinates.
* Keep original correction/delete, paid new tools and net current-definition capacity separate. Future definitions
  do not consume today's slot; replacing an existing footprint or sharing a selected definition is not a new slot.
* One busy guard covers confirmation, write and reload. A committed operation with failed refresh offers read-only
  Retry and cannot repeat its write. Native growing captions keep complete names and 44-pixel targets. Independent
  fixture review restores complete rows/owned bytes/display; commercial registration stays inactive.


## 87. Reviewed original quick resources (D-154)

* Persist explicit quick-template/saved-filter choices in the existing actual-file scope; retain every original
  full packet and recheck value/revision, file/rights, references and unchanged-timestamp mutations.
* Separate selected template draft activity/new definition slots from retained correction/delete/query history.
  A confirmed replacement transfers only its chosen slot; malformed input requires explicit repair for new work.
* Capture the full query before prompts; map duplicate names to original ids. Revalidate before/after dialogs and
  under the paired writer. One busy guard covers write/refresh; retire committed requests before native effects.
* Cover shared template publication, wrap names and offer explicit Reload. Applying a template never saves money;
  the existing account/ledger writer remains authoritative. Independent native fixtures restore complete rows,
  owned bytes and display. Debug layout waits inspect only the current page; registration stays inactive.

## 88. Original Home template destinations (D-155)

* Publish shortcut availability only after the complete parent review. Prioritize explicitly available later originals within the existing six shortcuts; keep all retained history and financial inputs independent.
* Carry the process-local reviewed original file/packet to the entry destination. Recheck both source and current publication; changed, removed or foreign originals never guess a newer same-id row or save money.
* Shortcut policy availability stays separate from transient parent-command busy state; Retry must restore enabled original targets after publication. Failed navigation requires explicit read-only Reload; show destination failure beside Save. Initial chooser navigation receives only the typed resource kind, never guessed selection. Home layout fallback is read-only until explicit customization.
* Native fixtures await actual appearance and Shell initial-query publication before changing inputs/applying a recorded handoff. Require realized visible feedback and complete original form data; restore complete rows/owned bytes/display and retain failed captures as negative evidence. Registration stays inactive.

## 89. Reviewed original device profile catalogue (D-156)

* Keep the original empty main id and exact original catalogue ids/names; a device quota scope is separate.
* Publish catalogue plus explicit scoped choice with one preference Set; use separate catalogue/choice revisions.
  Startup, backup-set and all catalogue readers follow the composite packet after first explicit mutation.
* Capture complete original/current-file/provider facts before prompts; recheck both delete confirmations and
  after authentication before committing a switch. Restore the previous path when an authenticated review retires.
* Original navigation/correction remains distinct from unsaved activity toggles. Keep covered publication,
  growing native names, pinned Save/Add/Reload and busy/discard Back. Failed committed refresh retries reads only.
* Device choices never enter portable preferences or grant paid rights. Registration remains inactive;
  actual-file work composition follows D-157; remaining operation coverage stays open. Independent fixtures restore exact owned data.

## 90. Actual-file original profile work (D-157)

* Compose the original device choice with existing independently verified financial facts for the actual file.
  Keep the legacy empty main id separate from its financial scope; never manufacture a scope or paid grant.
* Retain complete catalogue/legacy/provider revision/value facts in immutable reviewed access. Existing writer
  and snapshot guards recheck them across SQLite waits and change-and-return activity; current-profile navigation
  never transfers access to another file. Shared membership does not inherit personal device choices.
* Gate new work independently of complete original calculations, history/query viewing, correction/refund/overdue
  settlement, deletion, backup/restore and owned export. Malformed/unlisted catalogues never guess main.
* Default inactive construction/capture performs no preference or device-source read. Preserve original singleton
  source ownership and disposal in DI; never substitute production facts with native fixture providers.
* Native file-work fixtures use the actual decorated DI and original pages over independent fictitious rows;
  compare complete data, retain raw drafts and restore catalogue/owned bytes/display. No financial Save or paid
  activation. Remaining operation/import/restore/native and commercial-message coverage follows separately.

## 91. Original catalogue reminder lifetime and period review (D-158)

* Subscribe the actual reminder watcher to original catalogue changes without a startup database read.
  Keep one catalogue per lifetime, cancel delayed work on disposal and drain current owned batches asynchronously.
  Valid native reminders survive watcher disposal; original SDK actions are detached.
* Period-review eligibility comes from captured actual-file paid/profile facts. Recheck complete original
  preferences, first-recorded account date, live display and local day before/after native publication.
  Revalidation never creates Settings or marks a review finished.
* Consume each SDK scheduling result. Withdraw partial pending batches after refusal; preserve delivered
  notifications and keep the original Snooze cache retryable until complete acceptance/current-input validation.
* Await the actual complete coalesced batch in native fixtures, not just the first adapter callback. Use real
  Profiles Invoke/Save and independent financial data; restore complete rows/catalogue/owned bytes/display.
  Registration remains inactive; broader original Plan/Goal financial-input retirement follows separately.

## 92. Coherent original reminder financial packets (D-159)

* Retain complete original Plan rules/states, Goal/contribution/allocation packets and raw choice values;
  audit times or choice revisions alone do not prove unchanged inputs.
* Capture original accounts and needed date-bounded ledger/quantity inputs in a deferred SQLite reader.
  Use established financial calculators without selection filtering or duplicated algorithms.
* Revalidate all original packets/preferences in the same reader before/after native publication. Withdraw
  retired pending batches; preserve Snooze retry and legitimate explicit Goal editor draft mutation.
* Native fixtures change only their independent fictitious database, restore complete rows/owned bytes and
  retain failed checks as negative evidence. Production registrations and external release gates stay separate.

## 93. Structured write reasons and explicit filter retry (D-160)

* Translate original structured quotas/rights/selection without guessing purchase/provider state or exposing
  stored ids/exception text. Keep unrelated failures on their original path.
* Promise no-save/retained input only at the pre-write boundary. Committed return/reload failures remain saved
  and cannot silently resubmit. Keep every dirty field and original financial calculations.
* A rejected filter retires its review. The next explicit menu Invoke obtains a fresh packet and keeps the raw
  name/current query; name drafts stay process-local and never cross original files.
* Native effect ports are identified explicitly; actual native reason/Close/second Invoke and complete owned
  restoration are separate from native menu/prompt, commercial activation and external release acceptance.

* Use DatabaseFileIdentity for the actual configured SQLite target across open/closed connection aliases;
  never guess another profile or replace a foreign-file guard with native reporting paths.

## 94. Captured readiness before Save (D-161)

* Explain blocked Goal/Budget drafts from the same original reviewed rights/capacity computation; a captured
  WorkRestriction never grants a write or replaces atomic revalidation. Keep complete raw inputs.
* Malformed bounded Goal choices require explicit repair; unlimited access and original corrections remain
  available. Preserve Budget net-definition replacements, future periods and original calculations.
* Keep Debug mode writes in the central coordinator. Run dependent tests, cleanup and builds sequentially;
  negative preparations are not acceptance. Native fixtures restore complete rows and exact owned bytes.

## 95. Read-only comparison and actual local access (D-162)

* Keep original actual-file access separate from in-memory Free/Plus/Pro comparison. Read cached current facts
  without opening SQLite; failed/retired reads remain covered. Inactive tests never become a fake paid grant.
* Use original Core counts/permissions only for the model; fictitious comparison scope never reaches a provider,
  writer or portable preference. Shared membership/host access and personal profile choices stay distinct.
* Compare built local tools only; identify unavailable online services separately. Never infer price, purchase,
  cadence, renewal, role or restore rights. Continue returns without cancelling, downgrading or buying.
* Native chip overlays retain complete names and 44-pixel targets. Grow identity/value rows; keep Continue
  outside the viewport. Capture actual help popup separately and invoke its real translated OK.

## 96. Original editor rejection and committed Close (D-163)

* Keep exact write reasons beside pinned Save and preserve every raw field. A saved primary write is separate
  from preference, change-subscriber and native-return completion; no accepted action may silently resubmit.
* Edit detached original fields/IDs and clone mutable Plan rules before split/write. Retain the established
  split algorithm, pause, stop date and automatic-post anchor after a rejected attempt.
* Keep native query/effect interfaces on adapters. Plain category pickers use original name/identity/selection
  without unused colored tiles. Freeze committed inputs in their actual parent context and expose Close.
* Writer fixtures mutate only their independent owned rows, restore complete packets/bytes and identify
  fictitious native return/consent effects. Initial readiness and other operation coverage stay separate.

## 97. Reviewed atomic account archive (D-164)

* Recheck complete original account/related-plan packets, file, day and cached access in one actual writer.
  Reuse original Account/Plan policy; archive, Plan ending, exact default and choice edits commit together.
* A different current default stays untouched; do not create settings or silently resume ended plans.
  Dirty financial fields require explicit discard consent and never become archive payloads.
* Hold one busy lifetime through consent/write; accepted notification/return failures retain the actual saved
  result, notify both original stores and cannot silently resubmit. Restore only independently owned fixtures.

## 98. Original editor readiness and retained explicit refresh (D-165)

* Read original writer inputs for the exact file; inactive tests do not open financial rows/choices.
  Missing persisted choices count all eligible original rows, never a cached fallback subset.
* Share original Plan classification and detached candidate with Save. Readiness does not parse money,
  write a draft or authorize a write; original atomic checks remain final.
* Refresh explicitly without resetting raw inputs; failed/retired reviews disable Save with translated
  feedback. Preserve correction/history and real changed-review writer-rejection cases.

## 99. Original import/Undo outcomes and retained picker review (D-166)

* Retain the exact accepted import/Undo result across original post-commit notification failure.
  Retire the reviewed payload and accepted Undo identity before fallible history/failure follow-through.
* Keep pre-write rejection beside the unchanged review; picker cancellation does not reset it.
  Publish history only after a complete read and retain other rows on failure.
* Native picker/app-lock/cache/share effects stay behind explicit ports. Preserve neutral CSV names,
  UTF-8 BOM and original security/financial policy. Busy guards cover consent and authentication.
* Pin complete feedback/review/import actions outside the viewport; give history identity/Undo separate
  growing rows. Measure newly visible captions after native layout, without weakening native checks.

## 100. Accepted restore before fallible completion (D-167)

* Retire the accepted all-source package/password/preview before display, onboarding, lock, automatic-post,
  reminder or root completion. Explicit completion retry must never replace data or create safety copies again.
* A later source can fail after an earlier source changed data, even with BackupException. Report uncertainty;
  require fresh explicit review/consent. Never promise rollback or unchanged data from the exception type.
* Keep old review on incomplete new stream reads and pre-primary refusal. Hold busy/navigation guards through
  completion; preserve original optional protection and constructor no-write behavior.
* Inventory current entry points finitely before claiming remaining gaps. Historical partial-slice notes are
  not fresh requirements; approved Free CSV import/recovery must not acquire an invented paid/quota gate.

## 101. Finite commercial completion scope (D-168)

* Use the current entry-point inventory linked from the canonical backlog. Complete confirmed forecast,
  receipt and loan/report computation groups; do not rebuild delivered account/resource/import boundaries.
* Preserve Free financial-month forecast, history, corrections, essential warnings and owned-data recovery.
  Never infer paid access from Simple/Advanced or fabricate provider facts to complete purchase UI.
* Source-only inventory changes do not justify repeating unchanged successful builds/native checks.

## 102. Original forecast work and currency-safe comparisons (D-169)

* Preserve the Free named-calendar financial-month forecast and complete original financial inputs;
  new advanced horizons/scenarios/saves/comparisons use actual-file cached rights independently of UI mode.
* Cover coherent reads/projection; retain raw refused horizon/scenario/name and offer explicit Refresh/Reset.
  Saved original values and owned Delete remain available without a new paid comparison.
* Commit all currency snapshots together under the original actual-file writer. Retire accepted drafts
  before fallible notification/history; a completed Save cannot repeat through retry.
* Scope each new snapshot to its currency. Filter legacy mixed scopes at comparison without rewriting
  saved metadata or adding foreign-currency minor units. Native fixtures restore complete owned data.
* Continue the finite receipt and loan/report/PDF groups; do not rebuild delivered resource boundaries.

## 103. Original shared receipt reading and late destinations (D-170)

* Home and attachment rereading share the actual ReceiptReadingSession and original reader/parser.
  Demand actual-file ReceiptReading before native source work; UI mode never grants access.
* Capture original expense/attachment ownership, full entry packet and complete content fingerprint;
  recheck current file/access/source after awaits. Equal audit times do not prove unchanged inputs.
* Keep one busy lifetime through picking/unlock/read/feedback/navigation. Retain original cancellation,
  empty reread and explicit-review/no-Save behavior; manual attachments remain separate Free work.
* Carry a typed process-local original destination and validate it again in EntryEditor. Only compact
  pending bytes reach a new draft; transient recognition never enters portable data.
* Native fixtures identify fictitious picker/recognition ports, await newly revealed native layout and
  restore complete owned rows/bytes/display. A digital PDF test is not camera/image-OCR corpus acceptance.

## 104. Original loan analysis and confirmed payment writer (D-171)

* Keep recorded balance, manual repayments/reconciliation and essential interest-coverage warning Free;
  paid loan schedules/splits are independent of Simple/Advanced. Preserve real terms and raw drafts.
* Capture coherent original file/accounts/ledger/categories and nullable Settings. Validate complete
  packets, cached access and day after native choices/consent; caller mutation or audit equality is not authority.
* Compute and save original confirmed installments inside the existing ledger writer, with its original
  selected-account/new-money checks. Principal is a transfer; only interest is spending/income.
* Recheck after inserts while excluding only this batch's exact new ids. Roll back changed original
  inputs/triggers; retain typed accepted outcomes after subscriber failure and never repeat an old review.
* Cover account/schedule publication, anchor typed destination/refresh to original file and keep valid
  DateField. Independent native fixtures restore complete rows/bytes/display. Report/PDF work remains next.

## 105. Original report packets and detached portable PDF (D-172)

* Capture all original financial report inputs in one actual-file reader; nullable Settings never
  become a new row. Keep financial inputs independent of quota choices and UI presentation mode.
* Free keeps totals, original period opening/movement/closing tables and essential data status.
  Demand AdvancedReports before new paid calculators; retain an unavailable selected package.
* Cover coherent publication; explicit Refresh keeps original report scope. Detached PDF walks its
  own projection, never the live page. Each account table labels its own currency.
* Hold busy before native authentication through render/cache/share; recheck original file/data/day,
  cached access and scope/display after awaits. Remove only failed unpublished owned cache files.
* Suggestions use the original contribution writer with report checks before and after Save, excluding
  only its own accepted rule. Preserve named calendar, no ledger posting and typed accepted outcomes.
* Retain real negative evidence and successful unchanged checks. Complete the finite original report
  group, then perform the bounded ENT-02/03 review; do not invent provider/purchase or activation facts.

## 106. Bounded local commercial review and separate release gates (D-173)

* The finite original ENT-02/03 local completion review is closed; do not re-open delivered operations
  from historical intermediate gap notes. Use the current inventory and bounded evidence record.
* Inactive registration is not a permanent customer entitlement. Activation, real purchases, actual
  shared-server grace/retention and device/iOS acceptance remain explicit separate work.
* Source-only review does not require repeating unchanged successful tests/builds/native checks or
  generating another identical APK. Next ready engineering is the Android accessibility review.

## 107. Actual quick widget roles and original app locale (D-174)

* Widget actions are native Button/ImageButton controls with unchanged IDs/PendingIntents. Keep a
  44 dp logo target around the original 40 dp approved image; preserve existing semantic colours.
* Launcher RemoteViews can retain a resource locale. Send actual translated captions/descriptions
  and layout direction; prefer Android 13+ persisted app locales before startup preferences publish.
* Distinguish native role/name/target/intent checks from spoken TalkBack/system scaling acceptance.
  Use real launcher callbacks, never bypass a rejected protected broadcast. Remove only owned test widgets.

## 108. Original date purposes on native inputs (D-175)

* DateField AccessibilityLabel labels each original day/month/year box and calendar action using
  existing translated purpose. Empty context retains generic names; publishing a name never writes
  Date, reorders parts or resets a raw draft. Preserve visible placeholders and original calendars.
* All original date usages bind their own purpose, including dynamic debt reference labels. Native
  review measures real names and retained partial input; do not infer a spoken screen-reader pass.
* Reset only owned fictitious snapshot data between direct launches; never weaken a unique financial
  index for duplicate seeding. Restore original data/sidecars and remove the temporary sample marker.

## 109. Retained original Home sections (D-177)

* Source default section order matches Core. Appearance leaves correctly placed native containers
  attached and moves only displaced ones; preserve saved visibility and all original financial bindings.
* Review actual child events, native handler identity, saved order and unchanged stored data. Native
  reuse does not prove a cold-start/device/ANR target. Rejected startup candidates remain removed;
  do not repeat them without new evidence or count their temporary tests as delivered features.

## 110. Original holdings event slices (D-178)

* Index only one unchanged projection snapshot by asset type; retain event identity/order and call
  HoldingsLedger.Quantity for date/direction/checked-overflow behavior. Never combine units or cache across profiles.
* Distinguish Core computation/input-visit evidence from startup/device targets. Native captions follow
  original digit shaping; explicitly select the known fixture profile and restore original Main/data/display.

## 111. Live retained icon names (D-179)

* Resolve original icon keys in the current language, including selected names. Keep original targets,
  selection and drafts; subscribe only while loaded, detach on unload and refresh on reattachment.
* Native names/Invoke evidence does not establish spoken TalkBack/Narrator, OS scaling or phone acceptance.

## 112. Maintained official SQLCipher Community sources (D-180)

* The owner selected SQLCipher Community Edition on 2026-10-12. Use current official source, without
  custom cryptography or an unnecessary fork. Review security and maintenance of every native/managed dependency;
  deprecated or unreviewed third-party encryption bundles are not an adoption path.
* First verify architecture, required platforms and long-term maintenance. Deliver reproducible source/tool
  restoration, native builds, packaging, updates and security tests, then integrate keys, crash-safe migration and CI.
* Keep independent compatibility probes fictitious and outside the app's databases. A source pin, compiler build
  or Windows proof does not establish Android/iOS, recovery, whole-app encryption or release acceptance.

## 113. Independent Windows Community native producer (D-181)

* Build pinned official sources with existing reviewed MSVC/SDK and checksum-verified portable Perl/NASM.
  Keep assembly enabled and original codec/init/shutdown hooks, static crypto and explicit memory hardening.
* An interrupted native build may resume only an identical recipe. Changed configuration requires a fresh
  directory: upstream incremental objects can retain old filename/configuration data. Never patch binary bytes.
* Package only a manifest-matched candidate after original compiled-model/schema/key/tamper checks and
  native export/dependency inventory. Local candidate packaging is not app activation or publication.
* Keep the complete app/APK baseline unchanged until native platform, security, keys and crash migration
  are verified; Windows compatibility does not accept Android/iOS/recovery or database encryption.

## 114. Official upstream crypto validation (D-182)

* Run the stock OpenSSL suite under an unprivileged account in the matching candidate tree. Preserve
  its configuration, original native hash, complete log, exit status and skipped recipe evidence.
* Upstream PASS is scoped to its exercised configuration. Do not count disabled/platform/fuzz skips
  as exercised, suppress unexplained warnings or infer SQLCipher/mobile/key/migration acceptance.

## 115. Canonical source and independent Android native producer (D-183)

* Generate official source archives with process-scoped autocrlf=false/eol=lf; do not change global Git
  configuration or patch upstream source. Check cross-host scripts against the original Git blobs.
* Locked official NDK/host tools stay inside the independent Linux build image. Target API 24 and both
  arm64/x64; verify actual ELF architecture, 16 KiB load alignment, RELRO/NOW, non-executable stack,
  original exports and system-only dependencies. Do not infer a 16 KiB device pass from file alignment.
* Native runtime probes use fresh UUID directories below /data/local/tmp/vafadar-sqlcipher- only,
  fictitious key/file data, deployed hash checks and exact owned cleanup. Never open app storage.
* Android candidates and local NuGet packages remain outside app composition; ABI build evidence,
  x64 emulator runtime, whole-app managed/key/migration and physical arm64 acceptance stay distinct.

## 116. Stock SQLCipher suite and Native CI scope (D-184)

* Run the documented SQLCipher-specific suite against the reviewed source-built static crypto, with
  test hooks only in its independent fixture. Do not call this the full SQLite or mobile/app suite.
* Initialize a fictitious key before querying provider identity. Check native version, no system
  crypto fallback, actual error/test count and reported allocation cleanup; preserve failed probes.
* Native CI publishes candidate/evidence artifacts only, never product packages, keys or releases.
  Distinguish locally verified commands/YAML from actual hosted job results and remaining matrix/runtime gates.

## 117. Independent managed Android schema compatibility (D-185)

* Share the same actual-source/schema probe between Windows and a distinct Android package; never
  reference that package from the app or open main app storage. Keep Release AOT/trimming and complete native bytes.
* Use actual AddZananceData registration before compiled-model loading (D-44); do not patch generated
  models or introduce a duplicate Mono workaround. Preserve negative initialization/stage evidence.
* Delete only the independent package's old report on launch, inspect its private report, and remove
  only that package after checking. Schema/provider proof is separate from keys/crash/recovery/device acceptance.

## 118. Memory-only portable database boundary (D-186)

* Portable SQLCipher export uses the official export/serialize APIs and a single source read transaction;
  preserve user_version/application_id and original schema/history. Create no plaintext temporary database.
* Bound portable input/output explicitly, validate read-only deserialized images and normalize WAL header
  mode only in the owned memory copy as SQLite documents. Clear owned buffers; do not promise total process-memory erasure.
* Encrypted restore exports into one exact new staging path, never an existing generation. Generic BackupDatabase
  cannot convert plaintext/cipher formats. Final profile authorization/key/crash-safe commit remains separate.

## 119. Complete typed staging verification (D-187)

* Before generation acceptance compare logical schema/header, all typed rows/duplicate multiplicity
  and exact text/BLOB bytes; verify relational and cipher integrity. Do not log financial values.
* Use direct native statement spans for expression BLOB columns; the maintained driver metadata
  lookup caused an observed native crash. Preserve its negative evidence and real platform checks.
* Verification is read-only and never commits a pointer or authorizes recovery/profile/key decisions.

## 120. Non-secret generation protocol checkpoints (D-188)

* Bind transitions to one profile and distinct explicit generations. Advance only in order; original
  stays selected until selector commit. Never infer plaintext fallback from a missing candidate key.
* The immutable model does not certify durable effects. Actual key persistence, staged verification,
  selector storage/reopen, exact cleanup and restart fault injection remain coordinator obligations.

## 121. Resumable migration coordinator (D-189)

* Coordinate only an exclusively captured profile writer session. Read actual stored progress,
  reject regression/foreign identities, and revalidate context/cancellation after native awaits.
* Compare-and-commit then read back every checkpoint before the next effect. Selected restart
  reopens the candidate with its key before exact cleanup; never fall back to plaintext.
* Native effects must be idempotent after interruptions. Faultable port tests prove coordination,
  not actual key/filesystem/power-loss durability; require real adapters and restart proof before activation.

## 122. Non-root Native CI bind ownership (D-190)

* Linux native containers use the numeric non-root host UID/GID and one fresh owned bind-mounted
  scratch directory at the recipe's original path. Keep source/recipe mounts read-only and network disabled.
* Test real Linux permission refusal/matched ownership plus actual Android/upstream mount arguments.
  Do not fix ownership by making output world-writable or running native compilation as root.
* Preserve and surface failed native build logs. Workspace proof is separate from full hosted native
  build/security suite results; do not repeat unchanged application tests/UI/APKs for a tooling-only fix.

## 123. Native work parent traversal (D-191)

* Matching bind UID is insufficient if its parent is private to the image user. The immutable
  build home permits traversal (0755), not foreign writes; actual work/output remain separately owned.
* Exercise a foreign non-root UID through the actual work mount even on Docker Desktop. Preserve
  the failing old-image evidence and require actual hosted native completion after corrections.

## 124. Approved key recovery and permanent OS-backup policy (D-192)

* OD-04 is settled: automatic local profile key, optional separate recovery key and Zanance backup
  recovery. Portable backup passwords remain optional. Do not ask this product choice again.
* OD-10 is settled: Android/iOS databases and sensitive files are excluded from OS backup/transfer
  with completed encryption integration; new devices restore Zanance backups with fresh local keys.
  No interim policy. Native storage/migration/recovery/platform acceptance still requires implementation.
* Bind platform envelopes to profile, installation, generation and format. Windows uses CurrentUser
  DPAPI, not LocalMachine; do not claim hardware/device-only protection or same-user malware resistance.

## 125. Android installation wrapping key (D-193)

* Use application-scoped AndroidKeyStore AES-256/GCM with native randomized nonces and bound
  profile/installation/generation context. Prepare explicitly; unwrap never creates/replaces a key.
* Reject malformed/tampered envelopes and exportable wrapping keys. Automatic access is independent
  of the app PIN; run native effects off the UI thread under the captured access/migration gate.
* Report actual OS security level without inferring hardware protection. Independent Release process
  proof is separate from durable storage/recovery/migration and main-app encryption acceptance.

## 126. Immutable Android envelope files (D-194)

* Store only authenticated opaque envelopes in an existing trusted app-private directory under the
  exclusive captured access/profile gate. Derive final names only from bound profile/generation IDs.
* Create the final envelope exclusively, flush it and fsync its directory. Android app-private link
  publication was refused with EACCES. Torn files fail closed and are never replaced with a new key.
* Reads use native no-follow/nonblocking open and regular-file validation with bounded fixed lengths.
  Never create missing keys or choose orphan staging files. Process restart is not power-loss acceptance.

## 127. Transactional non-secret generation journal (D-195)

* Keep the migration selector in an explicitly created, versioned private SQLite protocol file;
  only bound profile/generation IDs and ordered phase belong there, never keys or financial data.
* Use rollback DELETE and synchronous EXTRA, validate captured schema/context before changing an
  existing file's mode, and compare-and-commit the next phase under an immediate transaction.
* Missing/unknown/corrupt journals never become Original automatically. Caller retains its exclusive
  access/profile lease; no file store alone authorizes opening/cleanup or a plaintext fallback.
* Verify actual native hot-journal and committed-selection process kills separately from full
  multi-resource migration, disk-pressure/hardware-power-loss and physical-platform acceptance.

## 128. Complete captured native migration effects (D-196)

* Use original coordinator/session with actual protected keys, coherent bounded memory snapshots,
  complete typed verification, encrypted candidate and encrypted rollback before selection.
* Recheck lease/identity/stored phase at native effects. Recreate only the bound unselected candidate
  with its existing key; missing selected keys never prepare replacements or choose plaintext.
* Source opens ReadWrite without Create because SQLCipher ATTACH inherits access flags for writable
  memory export/hot-journal recovery. Main is read in a snapshot transaction; no ledger writes.
* Keyed reopen and encrypted rollback verification precede exact original/sidecar cleanup. Synchronize
  closed encrypted files and directory through actual platform effects, not a managed rename claim.
* Independent original-model Android process interruption evidence does not activate main profiles,
  final backup/recovery UI, OS exclusion or certify power loss/other platforms.

## 129. Atomic protected context publication (D-197)

* Publish verified encrypted path and captured connection creator together; never cache key-bearing
  EF options by path. Key owners validate authorization and own mutable credential lifetime.
* Protected creators return closed unpooled keyed ReadWrite connections to the exact existing file,
  never Create mode. Verify ciphertext and reviewed provider before EF work; no plaintext fallback.
* Retire protected publications even at the same path; already-open EF contexts reject commands.
  This does not replace a global exclusive reader/writer lease or guard arbitrary direct native SQL.
* The legacy online-copy backup source remains permanently plaintext-only and refuses keyed contexts
  before stream/temp work. Final generation-aware portable backup/restore is a separate composition.

## 130. Protected portable backup export (D-198)

* Keyed factory sources export a coherent bounded portable image through SQLCipher memory only;
  no plaintext temporary file or online cipher/plain page copy. Portable password choice stays optional.
* Validate captured connection publication and cached commercial access before/after native effects
  and every output chunk. Keep chunk size independent from ArrayPool capacity; partial output is
  caller-owned on failure and must never be published as a successful backup.
* Weak connection/access association stores no key material. Existing-only encrypted generation
  restore/selection still requires its own receiver; legacy plaintext restore refuses keyed contexts.

## 131. Encrypted restored staging (D-199)

* Read portable images into bounded owned memory; refuse existing candidate/sidecars before input
  consumption and prepare a fresh captured device binding only after input integrity/access checks.
* Verify complete imported schema/header/typed rows before actual application migrations, reopen
  upgraded candidate for cipher/relational integrity and synchronize it. No plaintext temporary file.
* Candidate preparation alone never selects live data, deletes originals or certifies safety rollback;
  generation-aware selection/package transaction and restart acceptance remain separate obligations.

## 132. Restored selection and encrypted rollback (D-200)

* Restored candidate selection reads its existing bound key; it never generates a replacement.
  Preserve current data in fully verified/synchronized encrypted rollback before journal selection.
* Use configured connection identity, not SQLite open-time filename aliases. Compare/read back every
  ordered checkpoint; Selected restart opens candidate/rollback without old connection or plaintext fallback.
* Retain original/rollback and Selected state until complete package/lifecycle transaction finalizes;
  this primitive does not restore preferences, publish contexts, prune keys or certify full recovery.
* Observation timeout is not process failure. Resume the same live barrier; fix diagnosed Mono
  initialization before restarting only the stuck resume phase, never re-create its existing candidate.

## 133. Whole-package restore dispatch (D-201)

* An explicitly registered package receiver exclusively owns every validated entry under the backup gate.
  Receiver failure never falls back to per-source writes. Unknown entries stay available for receiver policy;
  absent entries do not imply deletion. No receiver retains borrowed portable buffers after completion.
* The service gate is not a global application lease. Receiver registration alone provides no durable
  package transaction; native staging, rollback, journal reconciliation and publication remain required.

## 134. Protected package preference replay (D-202)

* Stage the database and generation-bound encrypted original/restored portable-preference capsule
  before selection. Reject unsupported sources before effects; absent portable values preserve current choices.
* Resume selected packages without old connection or original portable input. Replay the exact allowlist
  before protected publication under a covered exclusive application lease. Retain capsule/database rollback/key.
* Platform preference writes alone are not durable completion. Startup must replay the capsule before unlock;
  unfinished pre-capsule staging fails closed for reconciliation, not guessed overwrite or regenerated keys.

## 135. Factory lifetime maintenance exclusion (D-203)

* Every original local-factory context owns one access lease until native/context disposal finishes.
  Pending exclusive work stops reader admission, drains existing contexts and supports cancellation.
* Maintenance contexts require the exact live owner from that location. Early owner disposal retires
  commands/publication but cannot release other work until its owned contexts close. No reader bypass.
* Publish with explicit ownership while maintenance is active. Covered lifecycle must coordinate
  background work and native effects outside the factory; the gate does not encrypt data by itself.

## 136. Original profile switch maintenance ownership (D-204)

* Take the location's maintenance owner inside automatic-posting exclusion before moving a profile.
  Destination migrations and settings reads use the same owned factory, never await ordinary admission.
* Keep the owner through authentication/catalogue commit or restoration of the previous path.
  Queued work captures its endpoint after admission; refusal never opens the temporary destination.
* Owned settings share the original settings initialization algorithm and semaphore. No guessed accounts,
  ledger posting, cipher fallback or ownership bypass is added; protected catalogue adoption remains separate.

## 137. Inactive profile deletion maintenance (D-205)

* The real profile form awaits context drain before deleting an inactive profile or its local backups.
  Recheck the captured catalogue/policy/current profile after waiting and before any mutation.
* Cancellation before deletion and stale reviews preserve original files/catalogue. Legacy synchronous
  calls acquire only immediately available ownership; never block the UI or bypass pending writers.
* Current/main deletion remains forbidden. Existing file-error cleanup policy is unchanged; final
  protected generation/key/rollback retention and deletion lifecycle are separate outstanding work.

## 138. Durable local key-binding identities (D-206)

* Private versioned identity bootstrap records one installation and nonempty key-binding profile ids,
  including the legacy empty-id main profile. Initial original/candidate generations are captured once.
* Read never creates or regenerates missing/corrupt/foreign/incorrectly typed identities. Explicit
  profile registration preserves existing committed bindings; no names, keys or paid facts are stored.
* These are initial migration identities, not a live selector. Actual journal/key/file verification
  and final catalogue/startup integration are still required; no portable or main activation follows.

## 139. Initial protected generation composition (D-207)

* Reopen actual stable identity and existing journal under the location's maintenance owner; do not
  fabricate captured ids or initialize missing selected state on read. Explicit first-journal creation
  requires the known plaintext original and no candidate/rollback.
* Join original migration/native key/durability effects, then verify the exact retained key read before
  atomic protected factory publication. Retain the key owner while active; drain contexts before disposal.
* Disposed key owners refuse further construction; no original/plaintext fallback. This initial
  composition is not a selector for later restored generations or main startup/provider activation.

## 140. Durable restore generation references (D-208)

* Reserve captured parent/target references before protected package input/key/staging effects. Keep
  ordered ancestry and initial installation/profile binding; unresolved intent cannot be replaced.
* Reopen and require the latest reference on receiver restart. Mirror selection only after actual
  journal/key/file verification and preference replay; Selected is not package/prefs durability.
* Never fall back to the initial generation after missing/corrupt reference state. Retain all prior
  references, key/capsule/rollback ownership; live startup resolution and pruning remain separate work.

## 141. One-time durable restore preference replay (D-209)

* A pending encrypted capsule replays settings, then requires an actual platform persistence callback
  before committing/readback/synchronizing Applied. Memory readback or a completed placeholder is not durability.
* Applied capsules preserve later user edits and skip replay/persistence on subsequent startup.
  Failed persistence leaves replay/reference publication pending; retained original values stay unchanged.
* Protocol v2 is explicit; old independent v1 capsules fail closed rather than silently upgrade or reset.
  Main platform persistence adapters and final startup/provider activation remain unqualified/open.

### D-210 — Persist original Android MAUI preferences before recovery completion

The Android host now registers ISettingsPersistence against the exact Preferences.Default backend
used by MAUI 10.0.110. Native PreferenceManager selects the same default SharedPreferences; a custom
IPreferences backend is refused rather than writing a guessed file. Persistence runs off the UI thread.
A fresh device-local operational marker forces a real snapshot write, rather than an empty editor
that may skip writing. Commit failure or cancellation throws; the protected receiver must not mark
its capsule Applied or publish until this completes under exclusive preference-writer ownership.
The marker is neither a secret nor a portable preference. Windows/iOS persistence, live generation
resolution, final provider/startup/recovery and sensitive-file OS exclusion remain open. The main
application database remains plaintext; registration alone does not activate encryption.

Focused acceptance compiles the original MauiSettingsStore and native persistence adapter with
actual MAUI Essentials 10.0.110 in the independent Android package. It writes only fictitious values,
checks cancellation, terminates the process, verifies a retained value and removed key in a cold
process, then cleans its own values. This is Android x64 API36/4 KiB evidence, not phone, iOS,
hardware-key, whole-application encryption or product-release acceptance.
### D-211 — Retain the restored generation through cold startup composition

SqlCipherProfileGeneration.OpenRestoredAsync resumes the actual latest package under the current
location's exclusive maintenance owner. It requires the receiver's exact private restore-history
store and installation/profile key namespace before native effects. Pending preference replay and
actual candidate/rollback/journal verification finish before retaining the exact existing key.
Readback requires the latest selected target; missing or mismatched history fails closed. No original
or initial-generation fallback, key preparation, directory guessing or early publication is added.
The retained key owner verifies the provider/integrity again and publishes through the same original
protected factory lifetime used by initial migration. Its disposal still refuses new connections.

The receiver has an internal no-publication resume path for this composition. A stale publication
callback from another process is never invoked; the current host publishes only after obtaining the
retained key owner. The public restore entry retains its existing behavior. Automatic all-profile
startup routing, main provider adoption, early staging reconciliation, optional recovery and retained
file/key lifecycle remain open. Main application storage remains plaintext. This change is not a
claim of complete encryption, device/iOS/OAuth or release acceptance.
### D-212 — Resolve the current protected generation from stable ancestry

OpenCurrentAsync reads the actual existing installation/profile catalogue and requires history's
complete initial key binding. Missing/corrupt/foreign history never implies initial selection and
cannot manufacture migration state. Row zero routes through actual initial migration/reopen;
any recorded restore intent routes through the captured latest receiver and retained restored owner.
Receiver target/parent/private directory and exact history are validated, including unresolved intents.
No enumeration, latest-file guess, key replacement or initial/plaintext fallback is introduced.

The host explicitly initializes history only after verified initial migration; interrupted bootstrap
still fails closed for reconciliation. This automatic composition is not yet registered in the main
startup/provider/profile lifecycle. Native producer acceptance, all-profile live adoption, recovery
and retained-file/key policy, early staging reconciliation and final sensitive OS exclusion remain.
No additional owner decision or commercial activation follows; main storage remains plaintext.
### D-213 — Preserve protected publication when a profile switch is refused

The original profile switch captures its complete publication inside the actual maintenance owner,
then asks the native runtime to open/migrate the destination. On authentication refusal, retired
catalogue or failed opening, rollback restores the original path and bound connection creator.
A path-only MoveTo cannot restore encrypted storage and is no longer used for this rollback.
Rollback is one-use, requires its exact live owner, never revives retired protected context identity,
and refuses a plaintext snapshot after protection is required. The host retains underlying key
owners until the transition finishes; no credentials are exposed by the publication token.

IProfileRuntime.OpenDatabaseAsync is the explicit native destination effect. The existing default
opens/migrates plaintext storage; a protected location rejects it. Final protected hosts supply
verified keyed publication, never strip protection or recreate missing plaintext. The actual MAUI
host's protected generation resolver/lifetime registration and final startup/all-profile/recovery/
retention/provider/OS-policy adoption still remain open. Main database encryption is not activated.

Concurrent initial Android/test builds hit CS2012/MSB3248 locked shared DLLs; those negative logs
are retained. Successful Windows and independent Release builds were not repeated. Only failed
Android and full test executions were retried once, sequentially after competing builds completed.
### D-214 — Own active and tentative profile keys through the real transition

SqlCipherProfileSession retains the active verified generation while a destination is tentative.
The actual ProfileService invokes finalization after committing its reviewed catalogue or restoring
its complete previous publication. The MAUI runtime forwards opening/finalization to the registered
protected session; existing plaintext hosts retain their original path and never bypass protection.
Accepted switching clears the previous mutable key owner; refused switching clears only the
candidate after the original publication is verified. No envelope, database or rollback is deleted.

Start, switch, acceptance, refusal and close require the actual live maintenance owner. Key retirement
also requires all owned contexts to be disposed. Concurrent resolution is serialized and synchronous
finalization refuses active resolution. Publication identity must belong to the exact retained owner;
keys cannot be retired based on a path string alone. Main registration/covered async startup, early
bootstrap/replay reconciliation, recovery/retained-file lifecycle, native provider and final OS policy
remain open. The main database is still plaintext; this is final lifecycle composition, not activation.

A protected host may resolve another catalogue-bound profile in the same private directory while
retaining the previous publication. It never passes through an unkeyed endpoint. Plain/foreign-root
initialization guards remain enforced, and actual journals, history, keys and files must still verify.
### D-215 — Record initial ancestry before native key migration

Initial ancestry can now be captured as pending against the exact existing Original journal,
before keys/candidates are prepared. Pending row zero survives restart but cannot reserve a
restore transition. OpenCurrentAsync resumes the actual initial migration, retains/reopens its
existing native key, then mirrors Completed journal state into selected initial ancestry before
returning a publication owner. Missing/foreign history still never falls back or regenerates.
Existing verified initial history remains supported; schema/version is unchanged and typed guards
still apply. New pending state is not evidence of a completed migration or permission to open data.

The old ordering left a crash interval after initial migration but before history creation. The
new pre-key intent closes that interval through actual captured resume. Interrupted metadata
creation itself, all-profile host bootstrap, recovery UX, provider/live registration and OS policy
remain separate unfinished work; missing or corrupt records still fail closed. Main application
storage remains plaintext. No owner decision, platform key policy or financial schema changes.
### D-216 — Publish complete identity metadata from captured staging

Identity bootstrap now commits and fully validates a private .creating catalogue before publishing
its final path with a no-replace same-directory move. A valid staged catalogue is resumed with its
original installation/profile/generation identities and exact known profile set; no new random ids
are generated on that path. Missing/corrupt/foreign incomplete staging fails closed rather than
being overwritten. Existing final metadata or sidecars still reject new bootstrap. Read never creates.

An explicit native durability overload synchronizes staged file/directory before rename and the
parent after publication. Managed-only callers retain transactional/typed/no-replace checks but do
not claim platform power-loss qualification. Failure after final rename requires actual existing
read/reconciliation, not another CreateNew or replacement. Other journal/history creation protocols
and main lifecycle/provider/OS policy adoption remain unfinished; main storage remains plaintext.

Independent Android x64 API36/4 KiB source-built SQLCipher probe pauses after actual fsync and typed
staged read, before final publication. The external runner terminates that process. Cold resume
requires the final path absent, publishes the same staged identity fingerprint, verifies actual
initial journal binding and idempotent/new profile registration preserving installation. No keys
are prepared, app encryption activated or scenario repeated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-58220434415e4174bc471b9760cd95e6.json,
run f93ba289610e4355aee1b0446d65bc9a. Reviewed APK ABI identities/signature verified; independent
package uninstalled. Strict Windows 26.63s, Android 17.07s and independent Release 71.92s builds
have zero warnings/errors. Script parser checked; test cleanup waits for terminal builds/tests.
### D-217 — Publish complete migration and history metadata

Generation journal creation and initial restore-history creation now use the same verified staged
publication as identities. Existing/corrupt/foreign final files and sidecars are never replaced;
read never creates. Captured Original journal state and exact initial pending/selected history
must match on staged resume. Advanced journal staging cannot become a fresh Original, and a pending
history cannot be republished as verified initial selection. Typed schemas/versions remain unchanged.

Native overloads synchronize file and directory around no-replace publication. Managed-only effects
do not certify native power loss. Incomplete/corrupt staging still fails closed for reconciliation;
this does not regenerate missing keys or certify complete startup/recovery. Main provider/all-profile
bootstrap/covered async startup, recovery/retention and final OS exclusion remain open/unactivated.

Independent signed Release AOT/trim Android x64 API36/4 KiB interrupts journal publication after
actual staged fsync, resumes that exact journal, then interrupts pending-history publication after
its actual fsync. Third process resumes both bindings, retains pending initial ancestry and confirms
staging aliases gone, without preparing keys or writing financial data. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-442b5d696a9a4a8699baf81c8b31218d.json,
run 18d182139b834d9b84bd34178fd54ef1. Runner clears only its own prior metadata barrier before
its second phase; no scenario repeat/reinstallation for failure or unrelated native suite/UI matrix.
ABI identities/signature verified and package uninstalled. Strict Windows 18.74s, Android 17.54s
and independent Release 59.16s builds report zero warnings/errors. Parser checked.
### D-218 — Compose preparation of all captured existing profiles

SqlCipherProfileBootstrap joins actual identities, staged journal/history creation, pre-key initial
intent, concrete migration/current-generation resolution and temporary verified key ownership for
every captured existing profile. The actual host maintenance owner excludes ordinary contexts;
private unpublished locations supply local ownership without moving the application's factory.
Only after this pass may the host's protected session publish the selected profile.

The original map must be complete, distinct, absolute and in the real location's private directory.
Existing metadata must match the captured profile catalogue. Missing installation is not inferred
as fresh: explicit initialization still refuses missing/redirected/non-plaintext originals or retained
key/migration/history/cipher state, unless resuming captured valid catalogue staging. Read-only
startup never creates identities. Missing financial databases are never manufactured here.

Native sync qualifies metadata creation; actual journals/history/current files and keys are reopened.
All encrypted rollback/envelopes remain retained, and temporary key owners are disposed before the
next profile. No intermediate application publication or fallback is added. New empty profile
creation, full restore callback/lifecycle registration, covered main async startup, provider adoption,
recovery/retention and OS policy remain open. Main application storage is still plaintext.

### D-219 — Initialize new profiles without plaintext financial files

SqlCipherNewProfile creates the actual current schema in an unpooled in-memory database and exports
bounded memory into verified encrypted candidate and rollback files. A dedicated new-profile intent
captures explicit creation separately from existing-profile migration. Missing existing state never
authorizes an empty replacement. Existing originals/sidecars, unknown ciphertext and foreign or
selected ancestry fail closed before schema/key work. Native synchronization qualifies checkpoints
and the no-replace rename into the existing generation journal. The normal current-generation owner
then verifies the retained key before application publication.

An interrupted unpublished empty-schema attempt may rebuild only under its captured KeyPrepared
intent, using the existing key. The original financial path never exists on disk. Both encrypted
packets are fully compared before selection; later ordinary writes are retained on cold open.
This infrastructure is not yet registered in the main product's profile creation/startup. Main
storage remains plaintext; provider adoption, lifecycle/catalogue registration, optional recovery,
retention and coupled final OS backup policy remain unfinished. No new owner decision is required
for this step; OD-04 and OD-10 remain the approved final policy.

### D-220 — Prepare new protected profiles before catalogue publication

The real Profiles Add command now awaits ProfileService.CreateReviewedAsync. Automatic posting is
serialized, existing contexts drain, native preparation finishes without moving the active endpoint,
and the original catalogue/rights are rechecked before publishing the new profile and selected slot.
No await holds the synchronous catalogue gate. Failed preparation, cancellation or a retired review
cannot publish the proposed profile. The synchronous legacy creation API refuses protected storage.

SqlCipherProfileSession retains its current key while an explicit native new-profile callback prepares
an unpublished destination; pending transitions or unavailable callbacks fail closed. MAUI forwards
to this path only when the protected session is registered. Existing plaintext hosts preserve lazy
creation. LocalDatabaseLocation exposes protection status without exposing credentials or creators.
No page layout, strings or financial algorithm changed.

The final native host must retain/reconcile uncommitted identity/creation artifacts after a rejected
publication, rather than deleting referenced keys or treating missing history as a fresh profile.
This step does not register that host, activate main encryption, implement orphan reconciliation,
optional recovery, retention or coupled OS policy. These remain in the full SEC scope.

### D-221 — Review Windows upstream atomic-width warnings

The two C4133 diagnostics are in unchanged OpenSSL tsan_add calls for 32-bit new_nid and
global_seed. The MSVC macro type-checks both arms of a sizeof-based conditional; its unselected
64-bit arm causes the pointer diagnostic. Exact original obj_dat.obj disassembly uses lock xadd
dword ptr at new_nid. Exact property.obj uses lock add dword ptr at global_seed because its fetch
result is unused. Both original operations are 32-bit; no upstream source patch or warning suppression
was added. This is compiler/source evidence, not a claim that incompatible pointer warnings are
universally harmless. See the official [intrinsic signatures](https://learn.microsoft.com/en-us/cpp/intrinsics/interlockedexchangeadd-intrinsic-functions?view=msvc-170).

Test-WindowsAtomicWidth.ps1 verifies the exact header against the pinned official Git object, the
unchanged candidate DLL hash and pinned MSVC/SDK. A compiled upstream-macro probe exercises -1 + 1
with an adjacent sentinel (an incorrect 64-bit addition would carry into it), 64-bit values above
2^32 and 800,000 atomic additions across four threads. Probe disassembly separately verifies DWORD
and QWORD operations. The first verifier expected xadd for the unused global_seed result and failed;
inspection showed the valid lock add optimization. Corrected only that matching rule, then the related
probe passed. No unrelated native suite, main build/tests/UI or APK was repeated.

Static range review also bounds two C4334 diagnostics: the QUIC first-byte shift is 0..3 and its
result 1..8 fits 32 bits before widening. The two C4319 diagnostics complement a shifted unsigned
32-bit bit value, then intersect it with a final bit-0 mask; upper extension cannot affect that
condition. These are expression-level reviews, not independent acceptance of BN/QUIC algorithms.
The remaining 743 C4244/C4267 narrowing diagnostics still need review; the Windows native candidate
is not yet adopted or declared warning-free. Main encryption and the full recovery/platform scope
remain unfinished. Main tests (3,343) and signed APK are unchanged from D-220.

Proof: artifacts/native-sqlcipher/build/windows-d/atomic-width-4c3a49b4d702474aab6fc39a35172841/proof.json;
first verifier evidence retained in atomic-width-77cc4e16c9904d2ca91fda28fa00f259.
Full 749-occurrence inventory: artifacts/native-sqlcipher/d221-warning-inventory.json.
Native hash remains 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Review records source-header and both original-object hashes; the harness parser passes.

### D-222 — Cover the first window while preparing storage off its thread

CreateWindow now constructs only a non-financial StartupPage. OnStart runs the actual migration and
initial settings/display-unit persistence through a worker before creating financial content and
reading secure access state. Foreground posting, reminder refresh and theme/language rebuilds are
suppressed until preparation and the existing app-lock/router setup finish. Existing PIN protection
and deferred links remain separate; link navigation failures use the established generic feedback
and do not misclassify an already prepared database as failed startup.

The real loading/failure/retry surface uses dynamic theme colors, wrapping scalable text and a native
button. StartupViewModel retains the cover on preparation failure, exposes no exception payload,
rejects overlapping attempts and never reruns a completed preparation. Three new translated messages
exist in all six languages. The initial prepared settings row also supplies display units; no extra
startup settings read is added by display-unit publication.

IAppDatabaseStartup is the final initialization port. The existing plaintext host migrates only the
current legacy database and explicitly refuses a configured protected session/publication. The final
native host must replace this registration with complete captured-profile bootstrap. Main SQLCipher
provider/key/recovery/retention/OS integration remains unactivated. This is actual startup integration,
not a claim of completed encryption, the two-second performance target or platform/device acceptance.

Diagnostic routes now await actual startup before accessing data. A Debug-only thread observation
records actual storage work on a different thread from window creation. The startup review renders
real controls with fictitious preparation outcomes, invokes the native retry target and compares all
stored table rows. Run-Snapshots.ps1 -UseExistingBuild reuses only a caller-owned verified Windows
Debug executable for distinct layout cases; default behavior still builds and fails on build errors.
Callers must not infer success from a stale LASTEXITCODE when no native build command was run.

### D-223 — Device-key protection for internal restore safety packages

BackupService now accepts an explicit IBackupSafetyCopyProtection port. Safety-only creation builds
its ZIP in bounded memory, protects it before the first storage upload and clears its owned package
buffers. Portable backups retain the independent optional password choice. DeviceKeySafetyCopyProtection
uses a separate durable platform key per safety copy, bound to the captured profile/installation and
fresh generation; it never borrows the live database key. Opening reads only the existing key and
rejects foreign contexts before key access. No missing-key preparation or plaintext ZIP fallback occurs.
The envelope uses standard AES-256-GCM with a fresh nonce and authenticated non-secret binding/header.
Creation and safety extraction bound cumulative source bytes and package bytes to 256 MiB. Failed
manifest/checksum validation clears materialized entries. This is a format/storage integration, not
a custom cryptographic algorithm or a replacement for SQLCipher.

Two real safety uploads in one second exposed the existing timestamp filename overwrite. Protected
copies now have unique opaque .vbs names outside portable discovery/retention. Generic portable pruning
cannot delete these referenced recovery artifacts. Final native registration still needs private
storage, reference-aware retention and orphan reconciliation; skipping pruning is not that final policy.

Focused Backup tests pass 54/54 and Data tests 169/169. The independent signed Release AOT Android
package uses the actual BackupService, DeviceKeySafetyCopyProtection and AndroidGenerationKeyStore.
Two protected copies restore exact fictitious content, tamper fails authentication, and a cold process
reopens both using a port that forbids key preparation. The cold observation deadline expired; the
same process subsequently wrote its successful result, read without restart. Negative timeout evidence
is retained. Native build has zero warnings/errors. Proof: artifacts/native-sqlcipher/d223-safety-native-proof.json,
run 26da5f287da548f09acdf6abd6d37bf4. The runner now gives these cold phases its established longer bound.

Main protection registration remains inactive. Main database/provider/session/bootstrap, final
Windows/mobile matrix, optional recovery, key loss, reconciliation, retention and coupled OS exclusion
remain unfinished. SEC-08 is in progress; this does not close it or authorize product release.
No visible UI changed; unchanged layout matrices and upstream native producer suites were not repeated.
### D-224 — Covered all-profile native startup

Protected startup captures every original independently of commercial quotas, rechecks the device
catalogue and prepares all profiles before one selected session publication. Completed callbacks
do not repeat resolution. Explicit installation authority and final native/recovery/OS adoption
remain host duties; no missing metadata or availability decision permits plaintext fallback.


### D-225 — Explicit protected installation authority

Protected startup distinguishes reopening, migration of known plaintext profiles and explicitly
new installation. Missing metadata never chooses a mode. Fresh schema stays in memory and must
not manufacture an account. Retained financial/key artifacts refuse replacement installation
identity; cold reopen reads existing keys only. Final main registration/recovery/retention/OS
policy and other platform/device gates remain separate and unfinished.


### D-226 — Native Windows generation storage

Windows envelopes use CurrentUser DPAPI and owner-only protected ACLs from native CreateNew.
Existing damaged/foreign/linked entries never get replaced; missing reads never prepare keys.
Native file/directory flush errors fail closed. Observed API completion is not physical power-loss
acceptance; current-user DPAPI is not a same-user or privileged-code security boundary. Final
main/provider/recovery/lifecycle/retention/OS adoption remains separate and unfinished.


### D-227 — Drain original pools before encrypted cleanup

After the application owner drains contexts, all-profile bootstrap retires only each captured
original's known SQLite pools before native preparation/cleanup. Disposed Windows contexts can
otherwise retain file handles. Never use ClearAllPools or regenerate interrupted fixtures/keys
to hide a cleanup failure; resume their actual committed encrypted selection with existing keys.


### D-228 — Retry the retained initial protected publication

An initial observer error can follow successful protected publication. Keep its exact key owner
alive; only that failed original and this startup's unchanged prepared catalogue may recover.
Repeat failed observers for the same endpoint without new publication/pools/keys. Ordinary
duplicate starts and unrelated protected endpoints remain refused; failures keep startup covered.


### D-229 — Adopt the exact selected restored generation

Restore replay releases its old registered connection before session publication. Adopt only the
verified same-profile binding/path/version under drained ownership; retain both key owners until
notification and adoption finish. Observer failure retains the installed restored owner for exact
retry, without rolling finance back beneath applied preferences. Profile-switch completion cannot
accept pending restore. Retiring an in-memory key never deletes its durable retained references.


### D-230 — Protected profiles never use legacy filename deletion

The legacy delete path must refuse protected active storage or actual target migration/history/
ciphertext artifacts before removing a catalogue row. Final protected retirement needs durable
intent, verified catalogue outcome and complete file/key-reference reconciliation; a filename
loop cannot supply that authority. This boundary does not complete retirement or main adoption.

### D-231 — Requalify retirement barriers before cleanup

An observable retirement commit after failed native synchronization is not cleanup authority.
Resume synchronizes and rechecks the existing journal before the host's idempotent next effect.
Missing metadata cannot authorize retirement. Final host catalogue publication, complete captured
artifact/key references and identity reconciliation remain required before main integration.

### Android protected application adoption

Android main startup now requires the reviewed official source-built SQLCipher provider. Register one
lazy covered storage host for profile creation/switching/retirement, package restore, safety protection
and maintenance. Construction does not open financial storage. Windows/iOS adoption stays explicit;
never infer it from Android success. Missing protected authority must remain covered without creating
a replacement key, profile or plaintext fallback.

Optional recovery enrollment retains the original vault after interruption and never generates a new
code when already enrolled. App PIN authorization remains independent of inaccessible financial data.
Backup recovery validates a bounded complete source before native effects, activates a retained new
private root and replays portable preferences once. Keep the original unreadable financial bytes.

Capture exact authenticated key/public-map/alias retirement authority before catalogue publication;
never reconstruct a missing capture after publication. Recheck current, archived, recovered and staged
references before deleting only the captured objects. Final Android OS backup/transfer exclusions are
coupled to this protected runtime; new devices use Zanance backups. Preserve the distinctions between
native process-loss evidence, physical power loss, device/OS/OEM/iOS acceptance and product publication.


### Approved local backend and market scope (D-271)

The owner approved complete local ASP.NET Core/PostgreSQL/private-storage backend development
without hosting cost on 2026-10-14. Deliver its complete identity/authorization/operations/restore
batch before final checks; paid hosting, provider accounts, mail sender and publication remain
separate gates. Use independent repository-owned local resources, never another project's data.

LOC-07/08 target Germany and other Western European countries, USA, Canada and Australia, with
extensible countries independent of UI language and official jurisdiction/year sources. Iran and
its holidays belong to a later separate variant. These decisions do not claim implementation.
