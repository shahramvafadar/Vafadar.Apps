# 01 – Assessment and decision log

## 1. Verified state of the repository (2026-09-26)

| Area | Specification assumption | Verified reality |
|---|---|---|
| Shared libraries | `src/Libraries/Core`, `Localization`, … | Exist as `src/Libraries/Vafadar.*`; all build without warnings; 97 tests pass |
| Localization | Runtime language switch, RTL, independent calendar | Implemented – verified (unit tests): en/fa/de, Gregorian/Persian display, `IDateFormatter`. Missing: region, week start, number parsing, Persian/Arabic digit input |
| Data | SQLite, EF Core, audit timestamps | Implemented – verified: `LocalDbContext`, UTC-ticks `DateTimeOffset`, audit interceptor, startup migration. Zanance model is empty; no migration exists yet |
| Backup | AES-256, 10 versions, validation before restore | Implemented – verified at library level (package, AES-256-GCM, checksums, app id/version checks, retention after successful upload, SQLite snapshot/restore). **Gaps:** settings are not in the package (BAK-03); no safety copy before restore (BAK-09); uploaded copy is not re-read (BAK-07); no UI |
| Google Drive / OneDrive | Destinations in the user's space | Storage classes implemented and tested against fake HTTP only. **No sign-in exists**, so nothing works end to end (BAK-14) |
| Authentication | Interfaces only | Confirmed: abstractions only |
| Maui | Bootstrap, Syncfusion license, `{v:Translate}` | Confirmed. Only `Syncfusion.Maui.Core` referenced |
| Zanance app | Home and settings, DB and backup wiring | Confirmed: two tabs, language and calendar selection, database registered, backup service registered without a storage or UI |
| Platforms | Android first | Targets Android (API 24+), iOS 15+, Windows; Android and Windows build in CI; iOS builds on demand only |
| Branding | Not final (PR-10) | App icon and splash are the .NET template artwork (Microsoft trademark) – must be replaced by a neutral placeholder (done: D-19, final brand D-26) |
| Theme | Light theme complete in phase 1 (UX-08) | Template follows the system dark theme automatically – untested dark styles would appear |
| OS backup | Must be documented (SEC-03) | `android:allowBackup="true"`; iOS includes app data in iCloud/device backups by default |
| Permissions | Minimal | `INTERNET`, `ACCESS_NETWORK_STATE` (template) |

## 2. Decision log

| ID | Decision | Reason |
|---|---|---|
| D-01 | Zanance domain lives in `Vafadar.Zanance.Core`; persistence in `Vafadar.Zanance.Data`; shared libraries change only for generic needs (week start, number parsing, app lock, notifications) | AR-01, HAND-04 |
| D-02 | Money is stored as `long` minor units with an ISO 4217 currency code; minor digits come from a currency table, never assumed to be 2 | FIN-05, SQLite has no decimal |
| D-03 | Financial dates are `DateOnly`; created/updated moments are UTC `DateTimeOffset` | FIN-08, REM-08 |
| D-04 | A transfer is **one** ledger entry with source and destination account and both amounts | FIN-02, FX-03: sides can never diverge |
| D-05 | Ledger entry kinds: Income, Expense, Transfer, Refund, IncomeReversal, Adjustment; amounts are positive magnitudes, the kind defines the sign | FIN-01, FIN-10, REF-05, ACC-08 |
| D-06 | Occurrences are computed from the schedule rule; only occurrences with a state (settled, skipped, moved, amount changed) are stored, keyed by `(ScheduleId, OriginalDate)` with a unique index | REC-13, REC-21 idempotency |
| D-07 | A ledger entry created from an occurrence stores `(ScheduleId, OccurrenceDate)` with a unique index – a second automatic posting is impossible at database level | REC-21, AT-30 |
| D-08 | Editing "this and future" splits the series: the old schedule ends before the occurrence, a new schedule continues from it | REC-15, history is never rewritten |
| D-09 | Review state: Confirmed / Unreviewed; manual entries are Confirmed, automatic postings are Unreviewed | FIN-11..14 |
| D-10 | Balances and reports are computed on the fly from the ledger (no stored running balances) | FIN-09 consistency; fast enough for 10,000 entries (to be measured, Q-02) |
| D-11 | Navigation: Home, Transactions, Plans, More + a persistent "Add" action | Specification §14.2 |
| D-12 | Phase 1 uses the light theme only (`UserAppTheme = Light`); superseded by D-22 in phase 2A | UX-08 |
| D-13 | Syncfusion controls are used where they add value: Charts (reports, forecast), Segmented control (kind/mode selectors), Calendar/Date picker (if Persian calendar support is verified), Numeric entry, Busy indicator, Popup, Chips | Owner decision; licensed |
| D-14 | Icons: Fluent UI System Icons font (MIT), bundled offline | VIS-03/04 |
| D-15 | Reminders: local notifications via `Plugin.LocalNotification` (MIT), no exact-alarm permission | REM-09 |
| D-16 | Android Auto Backup / iOS backup stay **enabled** and are disclosed in the privacy policy and Data safety | Owner decision (2026-09-26) |
| D-17 | Cloud backup destinations stay hidden until real sign-in and upload/download are verified; local backup (export/import of an encrypted file) is the phase-1 guarantee. Superseded by D-35: destinations are offered only in builds with OAuth clients, and such a build is released only after the device test | BAK-14 |
| D-18 | CSV writer/reader are implemented in `Zanance.Core` (no dependency) with formula-injection protection | IO-06 |
| D-19 | Temporary placeholder branding: neutral green icon with a simple ledger glyph; final name/logo pending; superseded by D-21 (name) and D-26 (logo) | PR-10 |
| D-20 | If no online feature ships in the first release, the `INTERNET` permission is removed from that release. Since D-35 this is decided per build: without cloud backup clients the release removes `INTERNET` | PRI-01, privacy |
| D-21 | Product name **Zanance** (owner, 2026-09-26), shown untranslated in every language; Android package and iOS bundle id `pro.vafadar.zanance` (replaces the unpublished `pro.vafadar.finance`). Code and folders first kept the working name *Finance* (superseded by D-24) | PR-10 |
| D-22 | Phase 2A adds a dark theme: semantic colors (Presentation/Palette.cs) in a light and a dark variant used as dynamic resources, a Settings choice "like the device / light / dark" stored as a local preference, shared controls follow the platform theme | UX-08 |
| D-23 | The recent-apps preview never shows the app content: Android sets FLAG_SECURE always (this also blocks screenshots), iOS covers the window while inactive (owner, 2026-09-28) | SEC-02 |
| D-24 | Code, folders, projects, namespaces and documents use the product name **Zanance** instead of the working name *Finance* (`src/Apps/Zanance`, `Vafadar.Zanance.*`, `ZananceStore`); the on-device database file becomes `zanance.db` and an existing `finance.db` is moved once on start (owner, 2026-09-28) | PR-10 |
| D-25 | UI controls: Syncfusion where it is clearly better for this app, native MAUI otherwise. **Syncfusion:** charts (doughnut with the total in the middle and tooltips, column and step-line trends), SfCalendar in DateField (Persian calendar; next to number boxes since D-59), SfLinearProgressBar for budget and goal progress (mirrored for right to left), SfComboBox with type-to-filter for the ~50 currencies, PDF. **Native or own:** short pickers (native feel, accessibility), Entry for amounts (own parsing of Persian digits and display units), own ChoiceChips (consistent chips that wrap long German labels; a segmented control would cut them), the category grid, system alerts and prompts, CollectionView. Revisit SfPopup for richer dialogs if the system prompts become limiting | UX-06, UX-08 |
| D-26 | Brand assets come only from the approved vector master (`branding/zanance`, geometry `ffae7f2ee01a0705`) through `branding/zanance/scripts/integrate.mjs`; generated files are not edited by hand. Launcher icon: colour symbol on neutral light `#F4F6F8` (Android adaptive icon inside the 66 dp safe circle; the themed icon of Android 13+ uses the foreground's alpha). Splash on `#F4F6F8`: Android shows the symbol only (Android 12+ crops to a circle), iOS the symbol with the Urbanist wordmark as outlines; the unpackaged Windows app has no splash. Android notification icon: white silhouette `ic_stat_zanance`, accent `#0C44A8`. The symbol appears in onboarding and on the More page | Owner approval of the master, background and splash (2026-09-28); PR-10 |
| D-27 | App design (owner approval of the "Zanance Design" canvas, 2026-09-29): neutral surfaces, colour only for a meaning and always the same one (blue action, green money in, red problem or debt, amber near a limit, violet plans and dates, teal savings, slate transfers, sky refunds and review); expenses neutral with "−" instead of red (deviation from VIS-01, see spec §31.3); a fifth tab "Insights" with Budget, Reports, Forecast and Goals as top tabs; More in four groups; a calm Home (category chart and account list start hidden, §21.5); icon and date tiles; Vazirmatn for Persian, Figtree with Urbanist titles for English and German; Persian digits in the Persian interface by default (setting) | Owner approval of the design (2026-09-29); UX-08, VIS-01..04 |
| D-28 | Flex budgets: a third budget method. Every expense category has a spending type (flexible by default; the default bills Housing, Energy, Communication and Subscriptions fixed, Insurance non-monthly; a sub-category follows its parent). Fixed bills are expected from the plans of the month, non-monthly bills get their monthly share (BUD-09), and the overall limit is the limit of flexible spending, which is all spending minus the two bill groups. Category limits are not used; Home, alerts and rollover measure the flexible spending | §10.3, BUD-11/12; the spec leaves Envelope/Flex to Phase 2 |
| D-29 | Public holidays: a plan with a weekend rule can also move off the public holidays of the user's region, fixed on the plan when it is saved. Supported: Germany (nationwide holidays only; rules around Easter and fixed dates) and Iran (Solar Hijri holidays exact; lunar Hijri holidays calculated with the tabular Hijri calendar and marked as possibly one day off the official announcement). No bank business days are claimed; other regions keep weekends only | F2-CON-05, REC-12 |
| D-30 | Home-screen widget: an Android "quick add" widget with the symbol and three buttons (expense, income, transfer) that open the entry editor after the app lock. It shows no amounts and reads no data, so nothing financial is visible on the home screen and no permission is needed. iOS needs a WidgetKit extension built with Xcode on a Mac and follows with the iOS release; Windows has no widget | §21.5 (quick-entry widgets), PRI |
| D-31 | Receipt OCR: a receipt photo attached to an expense can be read on the device with the system engine (Vision on iOS, Windows.Media.Ocr on Windows); the total, the date and the shop open the entry editor as suggestions with a notice, and nothing changes until the user saves. On Android (owner decision 2026-09-29) Google ML Kit text recognition with the model bundled in the app (`Xamarin.Google.MLKit.TextRecognition`, Latin script); ML Kit's network permissions are removed (ACCESS_NETWORK_STATE always, INTERNET in the release build through `ReleaseManifest.xml`), so its usage statistics cannot leave the device. The engines live in the shared library `Vafadar.Documents.Maui` since D-33 | §21.5 (on-device OCR with user review), PRI |
| D-32 | Android 13+ per-app language: the manifest declares the three languages (`locales_config.xml`), a language chosen in the system settings is used by the app at start and while it runs, and a language chosen in the app is written to the system setting; the activity handles the locale change itself and redraws the widget. Older Android versions keep the in-app setting only | LOC-01, roadmap |
| D-33 | Documents (owner decision 2026-09-29, after evaluating Syncfusion Smart Data Extractor and OCR Processor): receipts and invoices are read through the shared libraries `Vafadar.Documents` (rows from word boxes, PDF text layer with Syncfusion PDF) and `Vafadar.Documents.Maui` (system OCR engines, PDF pages rendered by the system). A digital PDF is read from its text layer without OCR; a scanned PDF is rendered (PdfRenderer, PDFKit, Windows.Data.Pdf) and recognised like a photo; at most four pages (the first three and the last). The Syncfusion OCR Processor (Tesseract) is not used: it has no Android/iOS runtime and would only wrap the same system engines there. The Smart Data/Table Extractor is not used on phones (about 180 MB of ONNX models); it stays an option for desktop features such as PDF bank statements. Interpreting the text stays in `ReceiptParser` | §21.5, PRI |
| D-34 | Local profiles (owner request 2026-09-29): each profile is its own SQLite file (`zanance.db` for the main profile, `zanance-{id}.db` for others) with its own data, settings, app lock and backups; nothing is shared. The profile list is kept in the app preferences, outside every profile, so the names are visible to anyone who can open the app (said on the page). Opening a profile moves `LocalDatabaseLocation` (no restart), applies its migrations and asks for the device owner when that profile has the app lock; undo is dropped, automatic posting is paused during the switch, reminders and automatic posting follow the open profile only. Each profile has its own backup set in the shared backup folders (retention and last backup per profile); a connected cloud storage is shared by the profiles of the device. The main and the open profile cannot be deleted; deleting another asks twice and removes its database and its backups on the device (cloud backups stay) | §3, SEC-01, PRI |
| D-35 | Cloud backup (owner request 2026-09-29): optional backups to the user's own OneDrive (MSAL, Android and Windows) and Google Drive (Google Identity authorization API of Play services, Android) through `Vafadar.Authentication.Maui`; iOS and Google Drive on Windows followed with D-50. Offered only when the build has the OAuth client (`MicrosoftEntraClientId`, `GoogleOAuthClientIdAndroid`); only such a build keeps `INTERNET` in release, and the MSAL redirect activity is compiled only with a Microsoft client (`CLOUD_MICROSOFT`). The user connects explicitly after being told where backups go; cloud backups always need the backup password; scopes are the app folders (`drive.appdata`, `Files.ReadWrite.AppFolder`) plus `openid`/`email` for Google to show the account; nothing is uploaded automatically. Connect, back up now, list, restore, delete a cloud backup and disconnect are separate actions (BAK-15); the last successful cloud backup is shown per destination (BAK-08). With `INTERNET` present, ML Kit may send usage statistics – disclosed in the policy | BAK-08, BAK-13..15, PRI-01/02/06 |
| D-36 | UI round after the owner's review (2026-10-01): help "?" buttons with full texts and examples for settings whose effect is not obvious (`Vafadar.Maui` `HelpButton`, texts `Help_{Topic}_*` in three languages); a visible back button before the title of pushed pages on Windows; a redesigned onboarding (centred column, progress bar, symbol halo, step icons, "Add your first account"); the icon choice of an account as a real button with a preview; light theme with a darker page (`#E8ECF2`) and stronger outlines; More rows as real buttons; the WinUI check box without its 120 px caption gap | UX-05, UX-06, VIS, ONB |
| D-37 | Home at a glance and quick add (owner review 2026-10-01): Home starts with the recorded balance and a quick add card – Expense (the everyday action, blue), Income (green) and Transfer (slate), each opening the editor with that kind, plus up to six quick templates that fill the editor in one tap (a template with an amount is two taps: template, Save). The period chips (this month / last month) moved into the income and expense section they change. Palette fixes: budget bars are green within the limit, amber near it and red over it (blue stays the action colour, never the state of money); the forecast path is violet; on Home the forecast shows the end balance and the lowest point on separate lines and only a negative balance is red; negative balances in an account's month summary are red. The budget card adds what is left per day of the running period. Chart date labels use the app's date formatter (language and calendar) instead of the chart's culture; a new entry is titled by its kind ("New expense"). Second step (owner request the same day): a Receipt button in quick add (a photo or PDF read on the device opens a new expense; the file is attached only when the entry is saved, and a failing attachment never turns the saved entry into a failed save), a Recent entries section (the three latest entries up to today, a `HomeSection` the user can hide or move; layouts stored before get it at the end) and the Windows focus ring in the action blue | UX-04, DASH-01..04, VIS-01, F2-TX-04 |
| D-38 | Dark ground and permissions (owner review 2026-10-01): the dark theme is built on the deep navy `#111827` proposed earlier (page `#111827`, card `#1C2536`, muted `#232D40`, line `#2B364B`, strong outline `#3D4A62`) instead of `#0D1219`, whose green channel above the red one read green-grey and did not match the blue symbol; the Android splash and the status bar follow it in dark mode. Notifications are offered once per device after the first start (also on the next start of an installation that finished onboarding earlier), with an explanation before the system dialog and only after the app lock; `Turn on` in Plans and Settings opens the app's notification settings when the system answers without a dialog. Receipt photos on phones: Android starts the device's camera app with `ACTION_IMAGE_CAPTURE` into a cache folder shared through the app's own `FileProvider` (no camera or storage permission; MAUI's `MediaPicker` would require both on Android 7–12), iOS uses `MediaPicker` with `NSCameraUsageDescription` in three languages and offers the settings after a refusal; Windows keeps the file picker. Whatever follows a trip to the camera or the picker runs only after the app lock | UX-05, VIS-01, REM-02/03, SEC-02, PRI |
| D-39 | More, Settings and About after the owner's review (2026-10-01): the More rows carry a one-line description; a fifth group `App` holds Settings and the new About Zanance (version, data statement, open-source licences); the version left More and Settings. Settings is grouped by what it changes: language and region · money and months · appearance · experience · privacy and security · notifications · delete data. The licence list left Settings for About, with the copyright holders and the full texts (MIT, Apache 2.0, SIL OFL 1.1) taken from the packages and font files into `Resources/Raw/ThirdPartyNotices.txt`, as those licences require. The floating add button has no shadow (Android clipped it to a light square). Windows time pickers use the 24-hour clock (the 12-hour one showed an empty AM/PM column on systems without those names). Owed-to-me amounts are sky blue (money back to the user). Persian digits keep the dots of versions and dotted dates (`۰.۱.۰.۱`) instead of turning them into decimal separators | UX-01, UX-05, UX-06, VIS-01, LOC-03 |
| D-40 | Whole-app review after D-39 (2026-10-01): colours by meaning in reports (a positive result green, a negative one and negative balances red, in the income-and-expense, trend and account reports; the trend table gets column titles); chart axes, grid lines and legends in the palette's outline and secondary text, so they stay calm in the dark theme; `HelpButton` no longer sets fallback colours in code, which outranked the theme's dynamic resources and kept the light colours in the dark theme; the currency box (`SfComboBox`, which draws its own almost white outline and ignores a stroke style) has its outline switched off and sits in a `FieldBox` border; Windows text boxes are outlined in the palette (light and dark). The further actions of an entry are one card of rows with icons instead of six equal buttons; quick templates in the editor show their category icon like on Home; the move buttons of Customize Home are small round buttons; dates in refund and goal history rows are written out | VIS-01, UX-05, UX-06 |
| D-41 | Wide windows and first days (review 2026-10-01): wider than 720 px (Windows, tablets in landscape) every page is a centred column of 720 px (`Presentation/ReadableWidth`, applied when a page first appears) instead of cards stretched across the window. A new user sees *Getting started* on Home – first expense, a regular payment, a monthly budget – ticked off as done and hidden when all are done or on *Hide*; it replaces the two tips that only appeared after three entries. Empty states: reports of amounts show one card with *Add entry*, the forecast without plans shows *Add plan* instead of a flat line, money owed to the user has an icon; the rate form starts from the currency of an account in another currency (else the US dollar, or the euro for a dollar report). The Windows focus ring is set on each control (WinUI 3 ignored the app resources of D-37). `Run-Snapshots.ps1` gains `-WindowSize` (e.g. `1280x820`) and `-Empty` (the empty states of a new user) | UX-05, UX-06, ONB-04 |
| D-42 | Keyboard and screen readers everywhere (review 2026-10-02): nothing reacts to a tap gesture alone any more. List rows and cards that open a page (Home attention rows, balance, income and expenses, forecast, budget, next due and categories; transactions, plans, plan dates, accounts, goals, categories, money owed to you, forecast items, report drill-downs, backups, profiles, rates, attachments) carry a transparent `OverlayButton` as their last child, so they are reached with Tab, announced as buttons and give touch feedback; `PlanRowView` gets `Command`/`CommandParameter`. Category pills in the entry and plan editors, colour and icon swatches, the transaction filter and saved-filter buttons are buttons too; the previous/next period arrows of Budget and Reports are round `MoveButton`s instead of bare icons; the "All"/"Details" links of Home sections are link-styled buttons (`SectionLink`). The Windows focus ring is now set when an element receives focus (`FocusManager.GettingFocus`), which also reaches the tabs of the shell that MAUI does not create, and uses the colours of the current theme. The plan editor shows a missing amount next to the field together with a missing name (it stopped at the name before); the category, colour and icon labels use the field label style. Home quick add labels are 12.5 pt so that the German "Umbuchung" fits | UX-05, UX-06 |
| D-43 | Insights on Windows (review 2026-10-02): the Windows shell lists Budget, Reports, Forecast and Goals only in a drop-down of the Insights tab, so they were easily missed. Their page header now shows the four as tabs (`Presentation/InsightsTabs`, set as the title view when one of these pages is shown): the current one bold with a bar below it, the others one click away, announced as buttons ("Selected" for the current one). Android and iOS keep their own top tabs. BoxViews leave the accessibility tree (an implicit style), so screen readers no longer read dividers and colour dots out as "Microsoft.Maui.Controls.BoxView". Countries are named in the app language ("Germany", "Deutschland", "آلمان"): `RegionInfo.DisplayName` gave each country in its own language, so the region list mixed languages and scripts; `Regions.DisplayName` now reads the name from the region of a culture in the app language. The currency boxes get the thin chevron of the other pickers (`ComboChevron`, one icon per box) and mark the chosen currency in the soft action blue instead of the income green; "Defaults for new reminders" uses the field label style | UX-05, UX-06, PR-05 |
| D-44 | Faster cold start (measured 2026-10-02 on an Android emulator: 12.3 s to the first screen, of which 4.2 s building the EF Core model from `OnModelCreating`, 1.35 s in `Migrate()` and 1.3 s in the first query): the EF Core compiled model (`Vafadar.Zanance.Data/CompiledModel`, `dotnet ef dbcontext optimize`) is picked up through its `DbContextModel` attribute and loads in about 1.2 s; it is built on the calling thread (`Microsoft.EntityFrameworkCore.Issue31751`), because EF's default second thread, joined inside the static constructor, hangs the app under Mono. `MigrateLocalDatabase` runs `Migrate()` only when the history lists a pending migration (the full model comparison of `Migrate()` stays in the tests, which start from an empty database). A test compares tables, columns, store types, keys, foreign keys and indexes of the compiled model with the model in code, so a forgotten regeneration fails the tests. Result on the same emulator: about 10 s. The emulator is slow in itself (registering the services alone takes 0.5–0.7 s there) | Q-02, UX-04 |
| D-45 | Windows title bar and list names (review 2026-10-02): the default window title keeps the Windows theme, so with Windows dark and the app light it was white on the light page (and black on the dark page in the opposite case); setting `RequestedTheme` on the window root did not change it. The window now has a MAUI `TitleBar` with the Zanance symbol and the title in the palette's text colour. Rows of a CollectionView (transactions, plans, loan schedule, onboarding languages) were read out as "Microsoft.Maui.Controls.Platform.ItemTemplateContext" and the day headers as "...GroupTemplateContext"; `Presentation/ListRowNames` names each row container after its item's `ToString()` (row types return title, amount and details) and each group header after its group. Debug builds gain `VAFADAR_CAPTURE_WINDOW`, a capture of the whole window rendered by the app itself (`RenderTargetBitmap`), because captures from outside are blank while other windows cover the app. Windows ignores the margin of a CollectionView item's root, so transaction and loan rows ran edge to edge while headers and filters were inset; the rows sit in a padded wrapper instead. Loan schedule rows show principal and interest and the remaining balance on lines of their own instead of wrapping beside each other | UX-05, UX-06 |
| D-46 | No database work while the app is built (2026-10-02): Android reported "Zanance isn't responding" on an emulator ("failed to complete startup") for starts right after installing; after an update Android also starts the process just to deliver scheduled notifications (`ScheduledAlarmReceiver`), which builds the app the same way. Android builds the MAUI app in `Application.onCreate`, with a time limit, and the migration (`DatabaseInitializer`, an `IMauiInitializeService`) ran there. It now runs at the start of `App.CreateWindow`, before the first page reads the database; nothing built earlier uses it (snoozes, theme and language read preferences). The cold start takes as long as before, but `onCreate` is short, and starting from an empty database (snapshots with and without `-Empty`) still works | Q-02 |
| D-47 | Windows dialogs in the app's font (review 2026-10-02): alerts, confirmations and the "?" help are WinUI dialogs; they follow the app theme and the reading direction, but used Segoe UI, also for Persian text. `App.ApplyFonts` sets WinUI's `ContentControlThemeFontFamily` to Vazirmatn or Figtree with the language, so dialogs, picker lists and the date picker use the app's font; MAUI controls set their font themselves and are unchanged. The debug window capture also saves open dialogs (`-popupN.png`), with `VAFADAR_CAPTURE_DELAY` to open one first. The round "?" buttons stay 30 px: larger touch targets would shift every form, to be decided separately. The Windows window is never narrower than 360 × 520 px, the smallest phone the layouts are made for; at that width "Savings goals" did not fit beside the other Insights tabs, which now say "Goals" (a horizontally scrolling header did not work: the header gives its view no width of its own). Rendering a menu (action sheet) fails in WinUI, so the debug capture skips such layers and notes the error instead of ending the app | UX-05, VIS-01 |
| D-48 | Larger "?" touch targets and the working rules (owner, 2026-10-02): the help buttons were 30 px, below the 44 px a finger needs. `HelpButton` keeps its 30 px circle but is a 44 px round button around it (taps, touch feedback, keyboard focus and the spoken name are on that button), so the rows next to it grow slightly. The owner also asked for the working rules in one file the repository carries: `AGENTS.md` now holds all of them (Persian in every message to the owner; English code, comments and documentation, with enough comments and always current docs; identity, secrets, verification, UI and tooling rules) and is listed in the solution | UX-05 |
| D-49 | Enhancement ZEX (owner, 2026-10-02): the owner accepted the direction of the requirements document *multi-unit holdings, trackable goals and explainable reports* (v0.1) – several currencies with native totals, a default currency and account for quick entry, gold by weight and countable holdings, account goals on Home with an ETA, Simple/Advanced for every area – but not yet the detailed design or any implementation. The design package `docs/enhancements/2026-10-multi-unit-goals-insights` (current state with evidence, domain and UI design, KPI catalog, Simple/Advanced matrix, reference backlog, acceptance plan, proposals ZEX-P01…P23) was **approved by the owner on 2026-10-03** with all proposals ZEX-P01…P23 as preferred; implementation runs in six phases (1: calculation contract, defaults, multi-currency and Home; 2: account goals; 3: quantity holdings; 4: reports and data quality; 5: quantity goals, trend ETA, wealth history and snapshots; 6: release validation) | – |
| D-50 | Cloud sign-in on every platform (owner request 2026-10-03): Microsoft (MSAL.NET) now also on iOS – redirect `msauth.pro.vafadar.zanance://auth`, token cache in the keychain group `$(AppIdentifierPrefix)pro.vafadar.zanance` (`Platforms/iOS/Entitlements.plist`, `WithIosKeychainSecurityGroup`), `AppDelegate.OpenUrl` passes the URL to MSAL (`SignInUrlCallbacks`); the same Entra client and scope `Files.ReadWrite.AppFolder`, no client secret, no broker. Google on iOS and Windows follows Google's *OAuth 2.0 for iOS & Desktop Apps* in `GoogleInstalledAppSignInService` (Vafadar.Authentication): authorization code + PKCE (`S256`), `state` checked, no client secret (iOS clients have none; for Desktop clients it is optional and not used). iOS signs in through `ASWebAuthenticationSession` (MAUI `WebAuthenticator`) with the reversed iOS client id as redirect scheme – the flow the Google Sign-In SDK runs internally, without its outdated .NET binding; Windows through the default browser and a one-time listener on `http://127.0.0.1:{port}/` (no administrator rights, at most 5 minutes). The refresh token, account e-mail and granted scopes are kept in the keychain (iOS) or a DPAPI file (Windows); access tokens only in memory; a sign-in without `drive.appdata` (Google's granular consent) is refused and revoked; signing out revokes the grant. New build secrets `GoogleOAuthClientIdIos` and `GoogleOAuthClientIdWindows` (GitHub `GOOGLE_OAUTH_CLIENT_ID_IOS` / `_WINDOWS`, passed to the iOS and Windows CI builds); each platform compiles only its own Google client id; the iOS URL schemes are generated at build time into a partial Info.plist, so no id is tracked and an offline build registers none. The availability matrix lives in one place (`CloudSignInMatrix`): Microsoft and Google on Android, iOS and Windows. The duplicate `CFBundleLocalizations` key in Info.plist was removed. Verified: unit tests for PKCE, the loopback listener (real sockets) and the Google token protocol; Windows and Android builds; the iOS app compiled on Windows (no Mac, so no app bundle or device test); no test with real OAuth clients yet | BAK-13, BAK-14, PRI-01/06 |
| D-51 | Lunar Hijri calendar (owner request 2026-10-05, to prepare languages such as Arabic): a third display and period calendar, Umm al-Qura (the official, published calendar of Saudi Arabia, shared by .NET, Android and Syncfusion) for 1318–1500 AH (1900–2077) and the tabular Hijri calendar outside, so a date never fails (`Vafadar.Core.Dates.LunarHijri`). It applies to dates, budget months, monthly and yearly plans (29- or 30-day months follow the missing-day rule), the date picker, CSV import and receipts (a receipt year 1300–1500 is read in the solar or lunar calendar, whichever is nearer to today). A one-day difference to local moon sighting is accepted. Stored dates stay Gregorian; the calendar is stored as an integer, so no migration. Verified: unit tests (Ramadan 1446 = 1 March 2025, 29 days), snapshots in English and Persian | LOC, REC-10/11, BUD-08, ADR 0007 |
| D-52 | English copy review (owner, 2026-10-05): the owner's review package of the English texts was applied after checking each change against the code – 325 resource texts, 5 platform texts (iOS permission prompts, Android widget) and 6 texts decided from their consumers. Style: plain US English; the financial record is a "transaction" in the UI (code and keys keep `Entry`); texts describe what is recorded, not the world outside ("No outstanding reimbursements are recorded", not "Nobody owes you money"); deleting and restoring name their scope, the current profile; a count that can be 1 gets a singular text or a count-neutral form ("Category limits kept: {0}"). Wording changes only English: the Persian and German texts of keys whose English meaning changed are reviewed in a later translation step (list in 03-ux-design §6). Verified: placeholder and key checks per text, tests, Windows and Android builds, English snapshots at 360 px in three calendars | UX-01, LOC |
| D-53 | Persian copy review (owner, 2026-10-05): the owner's review of the Persian texts was applied after checking it against the code – 540 resource texts, 4 platform texts (iOS permission prompts, Android widget) and 4 texts decided from their consumers (`Help_IncludeInTotals_Text`, `Account_IncludeInTotalsHint`, `Help_GoalType_Text`, `Cloud_BrowserDone`). The Persian texts now follow the corrected English of D-52 (profile scope of delete and restore, stage of a restore error, three goal types, net worth with priced holdings). Terms: «تراکنش» for a transaction ("ثبت" stays the verb), «برگشت وجه» for a refund, «نسخهٔ احتیاطی» for the safety copy, «برنامهٔ مالی» where "برنامه" could mean the app, «ارزش خالص دارایی» for net worth; the product name «Zanance» is never transliterated. The browser page after a cloud sign-in is written when the browser returns, before the token exchange and also after a refusal, so its text only says the browser step ended (the English "Sign-in complete" says too much and is left for the next English pass). Three proposed button labels did not fit ("Mark as completed" on a goal at 412 px, "Reviewed" in the selection bar at 360 px) and use the review's own pattern for actions that change a record instead: «ثبت تکمیل», «ثبت بررسی». English and German were not changed. Verified: placeholder, key and label checks per text, tests, Windows and Android builds, Persian snapshots in three calendars, both themes, Persian and Latin digits, 360 px and a wide window | UX-01, LOC |
| D-54 | German copy review (owner, 2026-10-05): the owner's review of the German texts was applied after checking it against the code – 499 resource texts, 2 platform texts (iOS camera prompt, Android widget) and 4 texts decided from their consumers (`Help_IncludeInTotals_Text` and `Account_IncludeInTotalsHint` describe the default account scope only; `Kpi_K03_Excluded` separates balance goals, where a transfer changes the balance but is no income, from quantity goals, where prices never count; `Report_ChangeRange` takes a full date, "Vom Tagesende am {0} bis {1}"). The German texts now follow the corrected English of D-52, which closes the translation follow-up of D-52. Formal "Sie" throughout; terms: Buchung (transaction), Umbuchung (transfer between own accounts), Erstattung / Kostenerstattung, Rückzahlung auf ein Kreditkartenkonto (never "Kartenzahlung"), Sicherung (backup) and Sicherheitskopie (safety copy), Prognose, Nettovermögen, Bewertungswährung, Bestände, Spielraum, Budgetübertrag, Archivierung aufheben; the quick-add "Transfer" stays short. Deviations from the review, decided from the code and the running app: the loan confirmations name the paying or receiving account (`{2}` is `from.Name`, not a category: "als Ausgabe vom Konto „{2}“"); three proposed button labels did not fit at 360 px and are shorter with the same meaning – "Bestand erfassen", "Ziel abschließen", "Geprüft" (selection bar). The English "as an expense of {2}" is ambiguous in the same way and is left for the next English pass. English and Persian were not changed. Verified: placeholder, key and label checks per text, tests, Windows and Android builds, German snapshots at 360 px and the default width, both themes, three calendars and a wide window | UX-01, LOC |
| D-55 | Spanish (owner, 2026-10-06): the owner's finished Spanish translation was added unchanged as the fourth language – 1,733 app strings, 63 shared strings, the Android widget (5) and the iOS permission prompts (2); the files were checked before they were added (payload hashes, the reviewed English sources unchanged, identical keys, placeholders and `|` lists, NFC) and never overwrite an existing file. One general translation with the informal `tú` serves every Spanish region: the device language is matched by its two-letter code (`es-MX` → `es`), and the language changes neither the country, the currencies nor the calendar (Gregorian by default; an explicit choice is kept). Wired: `AppLanguages.Spanish`, `SatelliteResourceLanguages`, iOS `CFBundleLocalizations`, Android `locales_config.xml`, `Add-Strings.ps1` (every new string now asks for Spanish). Dates: the connecting "de" comes from the culture's own patterns, also for the Persian and lunar calendars ("viernes, 3 de Mehr de 1405"); date tiles drop it ("25 sep"). Receipts: Spanish total words ("total a pagar", "importe total", "monto total" …) and the words that are never the total (subtotal, base imponible, descuento, efectivo entregado, cambio, vuelto, propina …); "TOTAL IVA INCLUIDO" stays a total, and without a total line the largest amount no longer comes from a cash, change, tax or discount line (in every language). The app shows currencies as codes only, so the pack's currency names are not used. Store texts added unchanged (offline and cloud variants). Verified: tests (Spanish dates in three calendars, composed texts, receipt cases), Windows and Android builds, Spanish resources in the build output and the APK, Spanish snapshots in three calendars, both themes, 360 px and a wide window | LOC, UX-01 |
| D-56 | French (owner, 2026-10-06): the owner's finished French translation was added unchanged as the fifth language – 1,733 app strings, 63 shared strings, the Android widget (5) and the iOS permission prompts (2) – after the same checks as Spanish plus the English platform sources and the pack's structural contract; Spanish and every other language stay as they were. One translation with the polite `vous` serves every French region (`fr-FR`, `fr-CA`, `fr-BE`, `fr-CH` → `fr`); the language sets no country, currency (no EUR default) or calendar. Wired like D-55 (`AppLanguages.French`, satellites, iOS, Android, `Add-Strings.ps1`). Dates: the separator after the weekday now comes from the culture's long pattern, so French reads "vendredi 3 Mehr 1405" in the converted calendars while English, German, Spanish and Persian are unchanged. Receipts: French total words ("total ttc", "net à payer" …) and words that are never the total ("total ht", "dont tva", "espèces reçues", "monnaie rendue" …); a "(dont TVA 4,00)" breakdown no longer hides its total; thousands groups with a no-break or narrow no-break space are read whole everywhere, with a plain space only on a total line (so a quantity is never glued to a price). Amounts typed with spaces, NBSP or narrow NBSP were already read correctly (tests added). Figtree and Urbanist lack the narrow no-break space; Windows falls back to a system font (seen in the snapshots), Android is still to be checked on a device. Two approved short labels where the original did not fit ("Terminer l’objectif", "Ajouter sans achat"). Verified: tests (French dates, 19 composed texts, amounts, 12 receipt cases), Windows and Android builds, French snapshots in three calendars, both themes, 360 px, 412 px and a wide window, and a regression run in en, fa, de and es | LOC, UX-01 |
| D-57 | Italian (owner, 2026-10-06): the owner's finished Italian translation was added unchanged as the sixth language – 1,733 app strings, 63 shared strings, the Android widget (5) and the iOS permission prompts (2) – after the same checks as French (payload hashes, keys, placeholders, English and platform source blobs, structural contract); every other language stays as it was. One translation with `tu` serves every Italian region (`it-IT`, `it-CH` → `it`); the language sets no country, currency or calendar. Weekday grammar: Italian "domenica" is feminine, so eight supplied keys (`Ordinal_Feminine_1…5`, `Rule_NthWeekday_Feminine`, `Rule_LastWeekday_Feminine`, `DayRule_LastWeekday_Feminine`) were added; the other languages copy their plain keys into them unchanged for key parity. `WeekdayGrammar` chooses them from the culture and `DayOfWeek` (never the weekday's spelling) for `PlanText` and the plan editor's day rules: "la prima domenica", "l’ultima domenica", "Ultima domenica", while other weekdays stay "il primo lunedì"; recurrence rules, dates and storage are unchanged. Receipts: Italian total words ("totale", "importo totale", "netto a pagare" …) and words that are never the total ("totale iva", "imponibile", "sconto", "resto", "contanti ricevuti" …); "TOTALE IVA INCLUSA/COMPRESA" and "NETTO A PAGARE" stay totals although "totale iva" and German "netto" are excluded, and "(di cui IVA …)" is skipped like "dont TVA". Three of the owner's approved short labels where the full text was cut at 360 px: "Già posseduto" (Holding_AddExisting), "Completa" (Goal_Complete), "Modifica" (Occurrence_Change). Spanish (owner): "Añadir sin compra" and "Editar vencimiento" replace the longer labels. Even these were one letter short at 360 px, so the paired buttons of the holding and occurrence pages use 8 instead of 16 px side padding (font size unchanged), which also lets the German and two French holding labels fit. Verified: tests (Italian dates in three calendars, 18 composed texts, 35 weekday phrases plus three "last" phrases, feminine-key selection by culture, alias parity, amounts, 15 receipt cases plus cash-only and German netto), Windows and Android builds, Italian snapshots at 360 px, 412 px dark, Persian calendar dark, Hijri and a wide window, a Sunday plan ("la seconda domenica", "l’ultima domenica") with temporary sample data, the Spanish, French and German buttons at 360 px and a regression run in en, fa, de and fr. French (owner, same day): "Saisir un achat", "Saisir une vente", "Corriger" and "Modifier" replace the four French labels that were still cut; checked at 360 and 412 px in light and dark | LOC, UX-01 |
| D-59 | Date input by numbers (owner, 2026-10-06): on Android the Syncfusion calendar dialog of `DateField` opened as an empty box (reproduced on the emulator: the dialog had no content; the calendar was created with width and height 0, which Windows ignored). The owner asked for the simplest control that can be changed by hand, with the month as a number. `DateField` now has three number boxes – day, month, year – in the display calendar and the full date below ("Tuesday, October 6, 2026"). Each box takes digits only (Persian and Arabic digits too), selects its number on focus so typing replaces it, and moves on when full. A complete, existing date is taken at once; a number that cannot be part of a date shows red and changes nothing; when the field is left, a day past the month's end becomes the last day and anything else returns to the stored date – nothing is guessed. Order: Gregorian as in the language (en month/day/year, de/es/fr/it day/month/year), Persian and Hijri year/month/day, in Persian laid out from the right to read `1405 / 07 / 03`. `CalendarDates` holds the calendar logic (tested); the box names reuse each language's existing words for day, month and year (`DateField_Day/Month/Year`). Owner follow-up (same day): the calendar stays as well – a calendar button next to the boxes opens the Syncfusion month view in the display calendar (Persian, Umm al-Qura or Gregorian), because seeing the month helps. The empty dialog had one cause: the dialog draws the calendar at the size of its own view, which was 0 × 0 to take no room; the view is now 1 × 1 behind the button and the popup has an explicit size (330 × 420). The dialog uses the app's colors and font in both themes (its month area through the Syncfusion theme key `SfCalendarNormalBackground`). One control serves all 26 date fields, so the fix applies everywhere. Two places with dates side by side (budget fortnight start, aggregated purchase period) now stack them, so the boxes fit at 360 px. Verified: tests, Windows and Android builds, snapshots in en/fa/de at 360 and 412 px, light and dark, Persian calendar, and on the Android emulator typing, an impossible date, the calendar dialog (filled, dark theme) and a date picked from it | UX-01, LOC |
| D-60 | Help texts rewritten (owner, 2026-10-07): the owner's finished copy for the explanations and examples of all 38 help topics in all six languages (414 replaced values, 42 new examples for seven topics) was applied with `Add-Strings.ps1` after checking that the reviewed resources, the behaviour code the texts describe and the help titles were unchanged; titles, button labels, screen reader hints and every other resource stayed as they were (checked value by value after applying). Every topic is used by a "?" button. `Run-Snapshots.ps1 -Help` now opens every help dialog and saves it, for reviews like this one. Verified: resource and usage tests, Windows and Android builds, all 228 dialogs at 360 px (light) and 412 px (dark) – text complete, the example label once, the close button visible; through UI Automation a sample of 44 dialogs exposed title, full text and a named close button. Not checked: a larger system font (a device setting) and TalkBack/VoiceOver themselves (planned accessibility pass) | UX-03, LOC |
| D-58 | Screen reader hints for short buttons (owner, 2026-10-06): six buttons whose labels were shortened for narrow screens (Holding_Buy, Holding_Sell, Holding_AddExisting, Holding_Correct, Occurrence_Change, Goal_Complete) keep their visible label as the accessible name and get a hint (`{Key}_A11yHint`) that TalkBack, VoiceOver and Narrator read after it. The owner wrote the texts in all six languages; English is the source. The goal button carries its hint only while it completes, not while it reactivates a completed goal. Verified: resource tests, Windows and Android builds, and the UI Automation help texts of all six buttons in the running app in French and Persian | UX-01, ACC |
| D-61 | Commercial model Free → Plus → Pro (owner, 2026-10-07): Free for everyday use, Plus for every local capability (monthly €2.99, yearly €24.99, or **Plus Lifetime** €69.99 once – local Plus only), Pro = Plus + personal sync + one shared space for up to 6 people (monthly €4.99, yearly €39.99, no lifetime). Prices are design/sandbox values until the store catalog and costs are confirmed. Security, languages, calendars, currencies, accessibility, history, corrections, backup, restore and export are never behind a plan; Simple/Advanced stays independent (MON-05); AI and Tax are separate add-ons; at least 20 new languages form the last feature wave. Supersedes MON-02 and MON-03 and the "Pro purchase" Phase 2B row; MON-01, MON-04…08 stay. Wave 0 audit found: no plan, quota or billing code; the local database is not encrypted; Android OS backup is on; safety copies are plaintext; app lock, profiles and widget have no automated tests. Plans, architecture, the canonical backlog, delivery plan and open decisions: `enhancements/2026-10-commercial-release/`. Nothing was implemented; work proceeds one owner-approved section at a time | MON, SEC, F2-SYNC, F2-SHARE |
| D-62 | Backup and first run (owner, 2026-10-08): optional password protection for local and connected cloud backups, on by default; remember only the choice on the device, never passwords. A password may be reused but must be entered and confirmed for each encrypted backup. An unprotected portable file can be read by anyone obtaining it; cloud sign-in does not encrypt it. Supersedes the mandatory cloud password in D-35. Keep three onboarding steps, offer theme (applied immediately) and Simple/Advanced (Simple suggested); offer existing-backup restore at the first and last steps, before creating an account. Use the existing validation, preview, confirmation and safety copy; restored accounts complete first run without duplication and keep profile preferences. Empty backups still need setup; cancelling a modal returns to the unchanged draft. Defer completion navigation to the next UI dispatch so Windows does not disconnect the form during layout; keep fixed progress/actions above scrolling content with an opaque theme background. About loses the long component list but retains a compact button to the full bundled notices: MIT, Apache-2.0 and OFL require distributing notices, without requiring that long inline list. No new SDK, permission, data field or schema change. Verified: 1021 tests, Windows and Android CI-mode builds with no warnings/errors, en/fa/de light/dark snapshots at 360/412 px and wide; Windows UI Automation selected theme/experience, cancelled back to step 3 and restored an encrypted sample directly to Home with its original 3 accounts (`OnboardingCompleted=true`, Advanced preserved). Evidence: `ZananceBackupTests`, D-62 build/snapshot artifacts; real cloud sign-in and physical-device/iOS verification remain pending | BAK-05/06/09, ONB-01..03, UX, PRI |

| D-63 | Device privacy controls (owner, 2026-10-08): optional four-digit app PIN, independent of device authentication and backup passwords, device-wide across profiles; PIN takes precedence when both locks are enabled. Store a versioned PBKDF2-SHA256 verifier (600,000 iterations, random 16-byte salt, 32-byte hash) and attempt state in platform SecureStorage, outside financial databases and portable backups. After five failures delay one minute; subsequent failures double it to at most fifteen minutes; deadlines persist across restart and clock rollback does not grant access. Changing/removing requires the current PIN; forgotten-PIN recovery requires successful device authentication and explicit removal confirmation, never cancellation or NotAvailable. Storage failure keeps access covered, with device-authenticated recovery. Keep the first frame blank until the lock has been initialized; protect pending links and sensitive operations. Android screenshots blocked by default, with a persistent foreground toggle; supersedes the unconditional foreground screenshot block of D-23. Always hide recents: API 33+ SetRecentsScreenshotEnabled(false), secure flags before background snapshots; iOS cover unchanged. iOS/Windows do not prevent screenshots. Exclude device-bound SecureStorage ciphertext from Android OS cloud backup and device transfer; broader database OS-backup/encryption decisions remain open. Owner-written source remains all rights reserved; About explicitly distinguishes it from required third-party notices. No new SDK, permission or database schema. Verified: 1041 tests (20 PIN cases), Windows/Android CI-mode builds and en/fa/de light/dark UI at 360/412 px and wide; physical-device authentication/recents and iOS acceptance remain release gates | SEC-01..04, PRI, UX |

| D-64 | Receipt evidence and review (owner, 2026-10-08): replace last-number/largest-price selection with complete purchase-total candidates and explicit Found/Review/NotFound states. Filter identifiers, percentages, measurements, tax, discount, tender/change and payment-only totals; keep bounded inclusive-tax annotations, signs, zero and damaged-token boundaries. Preserve explicit ISO currency or ambiguous symbol, and rial/toman factor; never perform foreign exchange, infer receipt locale from UI language or confuse engine character confidence with total semantics. Keep at most four complete choices and source rows in the unsaved editor; no new amount preserves the old one. Retain source line/block/page/full bounds and engine angle/confidence where available, deskew only using supplied geometry; Windows already returns upright boxes. Flag suspicious legacy column alignment and preserve PDF form-feed page boundaries. New images use independent upright recognition pixels up to 3200 px / quality 95; storage remains 1600 px / quality 80 metadata-free JPEG on every platform. Native subsampling/thumbnail decoding bounds large photos. No new SDK, permission, schema or network path; no automatic crop, perspective correction or arithmetic total invention without validated evidence. The original 19 receipt regressions failed in 18 cases before implementation; the detached separator regression also failed, then passed after correction. Verified: 1100 tests, zero-warning Windows/Android CI-mode builds, en/fa/de light/dark at 360/412/wide, actual Windows image selection (inclusive tax, skew, EXIF, ambiguity and currency conflict), stored-image rereading and cancellation/error invariants (AT-71, ZCR-LOC-12). Physical Android/iOS and representative-corpus receipt recognition remain open | F2, PRI, UX, AT-71 |

| D-65 | Clear plan and debt setup: date-anchored recurrence calendar, nearby summary/preview, common endings, optional rules; dedicated debt direction, positive amounts, optional estimates and separate unsaved reminder. See the detailed D-65 validation below. | REC, F2-DEBT, UX, AT-72 |
| D-66 | Complete signed installable APK before every phone-test handoff; package/signature verified, device acceptance separate. | Delivery, Android |
| D-67 | Cross-profile cloud restore discovery and independent regional format/digits/holiday choices, shared onboarding/settings UI and allowlisted portable display preferences. | BAK, LOC, UX, AT-73/74 |
| D-68 | SEC-01 research/proof isolation approved; proposed SQLCipher/key-envelope architecture, no production adoption or real-data migration. | SEC, ADR 0010, AT-75 |

## 3. Conflicts found and their resolution

| Conflict | Resolution |
|---|---|
| Earlier repository docs said "support light and dark themes" | Superseded by D-12; coding conventions updated |
| Earlier `requirements.md` asked "Rial or Toman" | Answered by FX-07: unofficial display units are phase 2 |
| Earlier docs listed a tip jar under monetization | Remains phase 2+/out of phase-1 scope (MON-07) |
| Specification assumes library paths without the `Vafadar.` prefix | Documentation uses real paths |

## 4. Risks

| Risk | Mitigation |
|---|---|
| Phase 1 turns into an endless project (RISK-01) | Vertical slices with gates (see 04) |
| Wrong early ledger model (RISK-02) | D-04/D-05/D-06 from the first migration; golden test AT-62 |
| Over-claiming security or cloud readiness (RISK-03) | Status words; cloud hidden until verified (D-17) |
| Persian calendar date input in Syncfusion controls unverified | Verify in slice S2; fall back to an own day/month/year picker |
| Notification behaviour differs per Android version | Manual device tests (AT-34..38) before release |

## D-65 - Clear plan and debt setup (owner, 2026-10-08)

The owner approved implementation after reviewing plan/debt UX. A blank plan starts as Once. Choosing Monthly
uses the selected first date's day in the explicitly named recurrence calendar; the start/end/effective-date inputs
and the next six actual dates use that calendar even when the global display calendar differs. The rule summary,
short-month explanation and inline validation sit next to Repeat. End date/count are common choices in Simple and
Advanced; unusual day/weekend/holiday/calendar rules expand locally. Switching to Custom preserves the current unit
and interval. Existing nonstandard rules remain expanded and unchanged. Category is a compact optional picker.

Debt creation has a dedicated entry point and two directions: I owe / Owed to me. Enter a positive amount at the
reference date; the direction supplies its accounting sign. Opening debt is not a new cash movement. Optional
interest/installment fields describe estimates, separately from actual repayments and reminders. Saving a new debt
opens its details. The explicit reminder action opens an unsaved transfer plan in the correct direction with unknown
principal, reminders on and automatic posting off; no estimated installment is silently posted as principal.
Notification permission follows the existing contextual flow. No new schema, SDK, permission or backup field.

Validation: 1121 tests pass, including 21 new recurrence/calendar/translation and debt-direction/draft cases (AT-72); running-app fixtures cover
blank/default, same-day monthly/count, calendar override/31st, positive debt/receivable and optional fields. Windows
and Android CI-mode builds have zero errors/warnings. en/fa/de light/dark at 360x800, 412x892 and 1280x820
were checked, plus Advanced mode. UI Automation verified debt -100.00 and receivable +200.00 from positive input,
negative input rejection, first-date/monthly/custom changes, saving a reminder with unknown principal and automatic
posting off, and cancellation: the fictitious ledger remained at ten entries. Combined modal dismissal/detail
navigation failed on Windows; awaiting dismissal and then opening details passed the repeated creation workflow.
Test output was cleaned. Physical-device acceptance remains a release gate.
This is owner-approved maintenance (ZCR-LOC-13), not approval of a commercial wave.

## D-66 - Installable APK for every Android phone test (owner, 2026-10-08)

Always prepare and provide a complete signed APK when handing work to the owner for Android phone testing.
Use the existing Release APK script, provide the actual artifact path, and verify package/signature and embedded
assemblies. An ordinary Fast Deployment APK or a successful platform build is insufficient. The owner installs
and performs physical-device acceptance; supplying the APK does not close that gate. This changes the delivery
procedure only, with no application, schema, permission or licensing change.

Verified for the D-65 handoff: the existing script published Release with the CI warning policy, without warnings
or errors. The signed package is `pro.vafadar.zanance`, version 0.1.0 / code 1, minimum Android API 24; arm64-v8a
phones and x86_64 emulators have embedded assembly stores and app AOT images. APK ZIP integrity and signature
verification pass. The approximately 77.1 MiB artifact is provided locally; physical-device installation is pending.

## D-67 – Cloud restore discovery and independent regional display (2026-10-08)

Owner-approved maintenance: keep password protection optional for local and connected cloud backup (D-62); make
cloud recovery discoverable; allow English UI with German date/number formats and German holidays. Cloud connection
and opening the backup page list existing backups automatically; they never create or upload one. Each destination
shows loading, empty, failure or completed-list feedback and a refresh action. Restore discovery includes every
profile set of this app; retention remains limited to the current profile. File restore is labelled as a device
.vbak file, with a cloud-list alternative. Preview, original password for protected files, safety copy and explicit
replacement confirmation remain required.

Language/resources/RTL stay independent from an optional specific formatting culture, calendar, digit shapes,
holiday region and week-start override. Numerical dates and entry-field order follow the selected pattern; month
and weekday names keep the UI language. Existing date values, money, currencies and saved plan recurrence rules
are unchanged. A shared form in onboarding/settings includes examples and help. Germany uses the existing
nationwide holiday rules only; no state coverage is claimed. Legacy choices survive, with invalid saved formatting
values falling back safely. Open forms are retained until a cached tab rebuild can occur safely.

A new allowlisted display-settings.json backup source carries language, calendar, holiday region, formatting culture,
digits and week start. Old packages without this optional source remain restorable. No PIN, token, password,
client id or screenshot preference enters the source. Snapshot runs retain and restore the prior portable choices
and password-protection preference; they use fictitious cloud list states. The snapshot launcher now stops on a
failed strict build instead of launching an older executable. No database schema or permission was added.

Validation: all 1,134 automated tests pass (including AT-73/74); Windows and Android CI-policy builds finish with
zero warnings/errors. Rendered en/fa/de checks cover light/dark at 360x800, 412x892 and 1280x820, including cloud
empty/error/file states and regional help. Native picker selection verifies live German/US samples, independent
Persian digit shapes and a retained settings page. Picker item collections remain stable between language changes.
Test output was cleaned; the original development database was restored and compared by hash after fixture review.
The complete Release APK at artifacts/android/pro.vafadar.zanance-Signed.apk is approximately 76.7 MiB, package
pro.vafadar.zanance version 0.1.0 / code 1, API 24 minimum / 36 target, signed with the local Android debug certificate.
Signature and ZIP integrity pass; both arm64-v8a and x86_64 contain assembly stores and the application AOT image.
Real provider
sign-in/upload/download/delete, Google signing-certificate registration and physical-device acceptance remain
owner-device checks; fixtures and successful builds do not prove those operations.

## D-68 - Independent encryption feasibility and proposed architecture (2026-10-08)

Owner approved ZCR-SEC-01 research, Windows/Android independent sample and safe migration design, while explicitly
excluding real data and the main database connection. ADR 0010 is proposed for review, not accepted. The harness
lives outside app/solution references and uses generated fictitious data; Android uses a different package id.
The deprecated 2.1.11 Community bundle proves mechanisms only: actual SQLCipher 4.5.2 / SQLite 3.39.2 is unsuitable
as a maintained EF 10 production baseline. Recommend maintained official SQLCipher builds subject to owner licence
approval and exact-binary Windows/Android/iOS verification, with random per-profile data keys and device envelopes.
No PIN-to-data-key derivation. D-62 optional portable-backup protection remains intact.
Four independent AT-75 Windows tests pass: native encryption/sidecar leakage controls, wrong/missing key, integrity,
tamper rejection, export, rekey, EF round-trip, CurrentUser DPAPI and authenticated PBKDF2 envelopes. Main 1,134 tests
pass. The signed Android Release AOT/trimmed fixture passes two process runs on an isolated Android API 36 x86_64
AVD using the existing installed emulator. Android Keystore recovers the same fictitious profile after process
restart; ciphertext database/envelope hashes remain unchanged and tampered envelopes are rejected. Strict Windows
and Android builds have no warnings/errors. No download, existing AVD or main app data was needed. ADR acceptance
remains an owner gate; hardware-backed keys, ARM64 device and iOS runtime are not inferred from this evidence.
The migration document defines crash/restart states and excludes new plaintext snapshots, but migration fault
injection and production adoption belong to later sections. Proposed next section after owner review: ZCR-SEC-07.


## D-69 - Continuous ready-section delivery and build-specific SDK review (2026-10-08)

Owner authorizes continuing all ready planned development without stopping between finished sections. This supersedes
per-section waiting, not unresolved product/licence/provider/spending/release decisions. ADR 0010 and OD-10 remain
pending owner choice; no production encryption or OS-backup change is inferred from the continuation instruction.
SEC-09 reviews the current SDK/permission graph and distinguishes Android Offline Release, Cloud Release, Debug and
source-only iOS evidence. ML Kit native diagnostics are independent of app cloud sign-in; qualify broad no-network
claims in the document library and policy. Add a shipped-APK allowlist guard with eleven AT-76 cases and an explicit
Offline build switch that overrides provider properties without reading or editing owner configuration. Strict
Windows/Android builds, main tests and complete signed variant artifacts verify the current baseline; future online
SDKs, physical traffic/real OAuth and signed iOS privacy manifests require repeated review. No new SDK or app UI.

## D-70 - Optional goal contribution reminders (2026-10-09)

ZCR-LOC-02 uses the existing ContributionPlan.ReminderEnabled field, off by default and visible in both modes with
help. Schedule at 09:00 device-local time on saved contribution dates; keep the saved rule calendar, anchor and
ending when only the reminder is edited. The planner shares the 62-day horizon and 30-pending bound. Current
progress suppresses active-but-reached, paused/completed/archived and missing/archived-source goals; a withdrawal
can resume reminders for an active goal. Missed dates never produce late bursts. Group goal contributions sharing
a minute and bound the combined device queue. Goal/holding changes rebuild it alongside entries/plans/settings,
start/resume, profile switch and restore. Reopening a form never prompts for permission.

Default notification title/body contain no goal name, account or amount; details require the existing opt-in.
A tap goes through the device app-lock gate and opens the goal, never recording a contribution or moving money.
No schema, new permission, SDK or network path. The saved opt-in travels with its existing contribution plan in
backups; runtime permission stays device-local. Windows retains the choice but has no system notification delivery.

AT-77 adds twelve planner cases and seven real-store/coordinator cases: month-end/Persian dates, weekly intervals,
rule endings, lifecycle/source/reached cancellation, withdrawal, opt-out, stable rebuilds, bounds, generic/details
privacy, grouping, permission denial and no ledger/allocation mutation. All 1,153 .NET tests pass.
Running-app en/fa/de light/dark editor/help review passes at 360x800, 412x892 and 1280x820; original development
DB files were restored with matching hashes. Android API 36 x86_64 native pending dates keep 09:00 at +02:00 and +01:00
across DST; a generic notification was delivered and tapping it opened its goal. UI pause cancels pending
requests, resume restores them, and saved opt-out survives process restart; ledger/allocation counts remain zero.
The diagnostic expedites only a fictitious notification, not system time; it cannot prove exact-time/Doze delivery.
Strict Windows and Android Debug/Release builds have no errors/warnings. Complete signed Release APK: 77.2 MiB,
package pro.vafadar.zanance 0.1.0 / code 1, minimum 24 / target 36, both arm64/x86_64 assembly stores and app AOT
images, signature/ZIP/Cloud permission guard pass, and actual emulator installation/cold start pass. Release metadata
contains neither DebugGoalReminders nor DebugSnapshots. Physical phone and iOS acceptance remain open.

## D-71 - Optional period review reminder (2026-10-09)

ZCR-LOC-03 implements the remaining ZEX-S0610 reminder under the D-69 continuation instruction. A profile-level
ReviewReminderEnabled switch in Settings is off by default, visible in both modes, with translated help and an
example. At 09:00 device-local on the first day of the next financial month, remind the user to review the month
that just closed. Follow the display calendar and MonthStartDay used by Home/review, independently of language,
numeric culture and the new-budget calendar. Fixed delivery time is independent of new-plan reminder defaults.
Require account data dated before the boundary, skip finished reviews and missed times, and bound future requests
by the existing 62-day horizon and shared 30-pending queue. No catch-up burst. Rebuild on ordinary data/settings,
calendar, startup/resume, profile and restore changes. Enabling explicitly requests platform notification permission;
denial preserves the choice with the existing permission feedback. Windows retains the choice without delivery.

Title/body are generic unless the existing notification-details opt-in permits the period label. Taps use the app-lock
gate and open the currently due review, including an empty state if already finished. A delayed tap never overwrites
old review progress. Reminders never mark steps or write financial data. Finishing a review now uses the serialized
settings update so it cannot overwrite a concurrent reminder choice. An additive migration defaults old profiles to
false; regenerate the compiled model. The profile choice is included in existing portable database backups, while
OS notification permission stays device-local. No new SDK, capability or network path.

AT-78 adds nine planner and six SQLite/coordinator cases. The 1,168-test main suite covers Gregorian/Persian/Hijri
year boundaries, pay-cycle dates, partial/finished reviews, empty/new profiles, missed times, stable IDs, generic
en/fa/de text, explicit details, denied permission, cancellation and absence of financial/review mutations. Existing
upgrade/portable-backup cases now cover the new field, and the busy queue case includes review reminders.
Strict Windows/Android builds, running-app en/fa/de light/dark 360/412/wide Settings/help review and isolated Android
native scheduler/delivery/tap and persisted opt-out checks are recorded with this section. The Debug fixture uses
only marked fictitious reminder data and expedites one notification without altering device time/settings. This
does not prove exact-time/Doze, reboot, ARM64 phone or iOS delivery; those remain device gates.

Native requests are 9 October (+02:00), 9 November and 9 December (+01:00), all at 09:00. A generic notification tap
opens September's review with 0 of 5 steps done. Explicit Finish removes October's pending request (3 to 2); UI opt-out
and process restart leave zero requests and zero ledger entries. The expedited diagnostic uses its own id so a normal
pending-queue rebuild cannot cancel that artificial delivery; normal production ids/dates are unchanged.
Complete signed Release APK: 80,484,267 bytes, SHA-256
6ed0e03be9cce3d57018ddce75290121817616f3bd1a31c0da220473c84ed1e4; pro.vafadar.zanance 0.1.0/code 1, min 24/target 36,
v2/v3 signatures, Cloud permission guard, ZIP integrity and embedded arm64/x86_64 assembly stores/app AOT pass.
All three Debug diagnostic types are absent from Release metadata. Actual Release installation and cold start pass.
Single emulator cold activity readings: 4.864 s before and 5.338 s after; not a repeated performance benchmark or
physical-device result. Test output is cleaned, and original Windows development DB files retain matching hashes.

## D-72 - Explicit aggregate linking during import with durable Undo (2026-10-09)

LOC-04 proceeds under D-69. Every aggregate overlap starts with no decision; show account, category, inclusive dates,
aggregate/detail totals and the selected remainder or double-counting consequence. Linking reduces an existing
aggregate only by this file's new accepted details; a new aggregate may cover existing details. One detail cannot
reduce two aggregates or be relinked while its earlier journal is active. Foreign amounts, reimbursements, refunds,
plan settlements and groups require separate review rather than guessed redistribution. Keep-both remains available.

Recheck every reviewed semantic row inside the write transaction. Missing/stale choices produce no writes and a
refresh action. Add ImportLinks with versioned source-generated financial snapshots and regenerate the compiled
model. Preserve all original metadata and attachment ownership when a remainder is zero; no attachment-byte copy.
Logical import history/known IDs include consumed aggregates. Undo survives restart/database backup, rejects later
edits, dependent imports/refunds or missing account/category identity, and restores the original aggregate atomically.
The journal is retained until Undo or profile/data deletion, travels with database backup and is excluded from CSV.
This introduces no SDK, permission, network access, password persistence or database encryption.

AT-79 adds eight Core and nineteen real SQLite cases: matching boundaries, partial/full coverage, repeated batches,
zero-remainder identities, complete clone metadata, attachment retention, backup round-trip, stale decisions,
double-link prevention, refund dependencies and safe Undo. All 1,195 main tests pass; 242 resource/localization tests
pass after final translated labels/help. Strict Windows and Android complete Debug/Release builds have zero warnings
or errors. Running Windows en/fa/de light/dark at 360x800, 412x892 and 1280x820 is reviewed; restored development DB
hashes match. The fictitious snapshot route never saves its preview or changes real data.

Isolated Android API 36 x86_64 native UI imports six 395 EUR details against 412 EUR: seven rows total 412 EUR,
aggregate 17 EUR, one journal. Process restart skips all six stable IDs. Install the complete Release APK over that
fixture, cold-start and confirm Undo in the native UI: one 412 EUR aggregate with its original ID remains, details
and journal are removed. Data inspection follows a Debug reinstall without launching the fixture. Release exercises
trimmed/AOT journal deserialization and semantic serialization; no fixture code exists in Release metadata.
Complete signed Release artifact: 81,037,397 bytes; SHA-256
f808c28b887482b7ccec307f263c10a9ce380101eacd483605286512369a78ea; package pro.vafadar.zanance,
0.1.0/code 1, min 24/target 36, arm64-v8a/x86_64, embedded assembly stores/app AOT, v2/v3 signatures, ZIP integrity
and Cloud permission guard pass. Native cold activity reading is 4.945 s, a single observation rather than a benchmark.
ARM64 phone, iOS runtime and production acceptance remain open; manual editor behaviour remains its separate baseline.

## D-73 - Application flow tests through explicit native ports (2026-10-09)

Complete ZCR-QA-03 under D-69. Add Vafadar.Zanance.App.Tests to the solution and non-MAUI test filter. Compile the
actual platform-independent application sources through explicit links; no substitute MAUI types, duplicate
algorithms or new application layer. Keep native modal pages/navigation, theme application, profile migration and
OS authentication in adapters registered by the existing MAUI composition root. Tests use real SQLite, migrations,
resource translations, localization, command implementations and the Core PIN verifier with isolated native ports.
FluentIcons.Common is a managed enum dependency already shipped transitively; centralize the same 2.1.341 version.

Extract bulk selection/writes so the actual transaction screen delegates to the tested flow. Clone selected records
before edits: rejected saves preserve loaded snapshots. Freeze selection and cross-command actions during a dialog.
Enforce the eight-second Undo window at the command write boundary, including a delayed tap or backwards clock.
Require catalogue GUID identities before forming a profile path; retain migration/authentication rollback and owned
file deletion. Keep the access gate closed when no native window exists or removing its cover fails. A backwards
clock cannot grant the 30-second short-absence grace. Keep existing device-only recovery behavior distinct from the
PIN gate, which never accepts NotAvailable. Route widgets/reminders through typed, supported destinations after
secure startup/unlock and only after the first real page is visible; reject malformed, empty-identity and numeric-kind links without navigation or ledger writes.
Theme policy subscribes once, rejects invalid choices and suppresses native-event reentrancy; open forms stay intact.

AT-80 adds 68 cases: onboarding 6, profiles 7, bulk 11, app access 13, widget/reminder links 21, theme 6, Undo 4.
All 1,263 main tests pass with no skips; strict Windows/Android builds and runtime evidence are recorded in AT-80.
No schema, portable preference allowlist, credential persistence, SDK, permission, commercial enforcement or
production encryption changes. Fictitious native/runtime evidence is separate from physical ARM64, real provider,
iOS, store and product acceptance. The next independent ready section is QA-04, asset-account confirmation tests.

## D-74 - Valued-asset entry consent coverage (2026-10-09)

Complete QA-04 / ZEX-S0408 under D-69, after the QA-03 native ports. Preserve the existing editor policy: only
Income/Expense on a legacy Asset account requires explicit consent; transfers, balance adjustments, refunds and
holding capital events retain their paths. This is an application confirmation, not a new persisted ledger flag or
an import/restore restriction. The financial validator remains mandatory after consent.

Move the actual confirmation and save-continuation boundary into AssetEntryConfirmation through IAppInteraction.
The real entry editor calls it before its draft mutation/write. Hold IsBusy across native consent and the continuation;
reject another save while either is pending, release on cancellation/failure, and require fresh consent on retry.
The accepted continuation contains the existing validation/write/attachment/navigation behavior; no duplicate editor
algorithm, schema, translation, encryption, commercial quota, permission or SDK change.

AT-81 adds 37 application cases with real isolated SQLite: cancel/create/edit/accept, all six other account types,
all six non-Income/Expense kinds, the six actual translations, pending/repeated commands, dialog/save failure retry,
validator rejection and a real one-entry transfer preserving balances and zero income/spending. Main suite: 1,300
passed, zero skipped. Native/strict-build/signed-APK evidence is recorded in AT-81. Physical phone and iOS acceptance
remain open. Next independent ready section: QA-06, measured startup/search/migration performance.

## D-75 - Measured large-ledger loading and account calculations (2026-10-09)

QA-06 proceeds under D-69 with an independent, non-shipped executable referencing actual Core/Data services.
Use only newly created fictitious directories and separately created emulator profiles: reference 10,000 entries,
20 accounts and 100 manual plans; tenfold scales all three counts. Populate the actual first schema, upgrade through
the production migration path and fingerprint all retained legacy account, entry and settings fields. Reject an
existing target or redirected ancestor. Validated save measurements include read-back and fixture restoration
outside the timer. Native first frame, loaded Home, operation calculations and physical-device acceptance are
different evidence; retain failed observations and never turn these samples into a two-second product claim.

Baseline native Android loading exposed unavailable accessibility hierarchies and a later not-responding dialog;
a filtered result counter/date group alone did not prove its row was exposed. Microsoft.Data.Sqlite's async I/O
is synchronous. Capture the short-lived context/profile before queuing entry materialization on one worker and
dispose only after completion or cancellation. AccountEntryIndex preserves original record identity and ordering,
routes a transfer to its two account slices and delegates every financial effect to the existing Balance method.
Home cards, totals and forecast starts avoid repeated full-ledger scans. No schema, security setting, backup format,
SDK, permission, production encryption or commercial policy changes.

AT-82 adds 12 Core and 11 real SQLite cases, including transfer identity, dates, confirmation and account scope,
legacy self-transfers, checked arithmetic, preserved migration data, queued profile reads and cancellation.
Measurement boundaries, native findings, rendered checks and signed APK evidence are recorded in
[Q-02](quality/performance-q02.md) and AT-82. Physical ARM64 and iOS acceptance remain open independently of CI.

## D-76 - Transaction snapshot publication before interaction (2026-10-09)

Follow up the D-75 input-during-initial-load observation under D-69. Cover the grouped native list until the whole
snapshot has been read and presented, disable search/filter/Add and bulk actions while covered, and show translated
loading text. A failed read or presentation stays covered with a retry action. Concurrent load requests share one
task; successful reloads retain the query/filter choices. The existing synchronous filter-batch guard remains
separate from the asynchronous read state and bulk-write state.

SnapshotLoadState is actual application source, also compiled by App.Tests; publication completes before IsReady
can become true. Register the shared task before starting the read so a synchronously completed read cannot prevent
later reloads. Cover a failed reload before clearing its error so the retained previous snapshot never briefly becomes
ready. Exceptions/cancellation reach the caller, release the pending operation and allow retry. No financial
formula, search algorithm, profile storage, schema, security setting, backup format, SDK or permission change.

AT-83 adds 12 application cases for initial coverage, shared reads, publication ordering, synchronous repeat loads,
first/reload/presentation failures, retry without transient readiness, cancellation, argument validation and a real SQLite transfer preserving original
identity/balances and zero income/spending. Main suite: 1,335 passed, zero failed/skipped. Rendered/native and APK
evidence is recorded in AT-83 and Q-02. This prevents premature input; it does not establish a MAUI rendering root
cause, repair the baseline ANR or meet the two-second Home objective. QA-06 and physical/iOS acceptance remain open.

## D-77 - Large-text Home actions and plan rows (2026-10-09)

Proceed with the first ZCR-A11Y-03 slice under D-69. At 200% and 360 px, four equal Home columns truncate all quick
names; date tiles clip and a trailing plan amount squeezes the title/subtitle into a nearly zero-width column.
Wrap complete quick actions, give plan identity and amount/status separate rows, allow date/template containers to
grow, and make section links at least 44 px high. Preserve native text scaling, action commands, semantic names,
translated resources, money/date formatting and financial calculations. Decorative glyphs keep their explicit size:
the current icon library measures an unscaled square even when its formatted span scales; both inner label and span
must opt out. The plus is likewise a named action's decorative symbol.

Debug review never changes a system text-scale setting. Windows substitutes its font manager only for an explicit
fictitious snapshot run, compensates the read-only OS factor and respects text scaling opt-outs. This is layout
stress, not real Windows OS accessibility acceptance. A temporary Android activity-context prototype recorded native 200% text for fixed English/German
content but pinned locale; the font-only-delta trial left startup covered with no native text. Both prototypes were
removed from source before delivery. Those failures are diagnostic findings, not production fixes or platform
acceptance. Only owned fictitious QA data was inspected and input contents were omitted. The final normal Release
has no font-scale override/collector and is verified separately; live native RTL/200% acceptance remains open.

See quality/font-scaling-a11y03.md for verified scope, rendered/native evidence and remaining findings. A11Y-03 is
in progress, not complete: transaction/account rows, bottom navigation, large currency tokens, fixed actions,
custom-drawn controls, other screens/dialogs and physical/OS/screen-reader acceptance remain independent work.

## D-78 - Persistent actions and readable financial rows (2026-10-09)

Continue A11Y-03 under D-69. Reserve a separate action row below the viewport on Home, Transactions, Accounts,
Plans, Holdings, Savings goals and Categories. Bulk actions occupy the same reserved row when selected; the Undo
notice has its own row and places its button below its text. Empty goals and Simple holdings retain their existing
visibility rules. Financial entry/account/holding identity wraps above amount/status; group date/name and net/totals
also use separate rows. Existing commands, semantics, calculations, formatting and stored data remain unchanged.

Windows Debug checks measure actual visible native financial rows and action geometry, and own-window renderings
include persistent actions/navigation. Hidden Home sections still have handlers but are unmeasured; exclude hidden
ancestors rather than misclassifying their negative dimensions as clipped production rows. Undo preview restores
its presentation flag and never deletes an entry or invokes Undo. No new xUnit count, model, translation or permission.

AT-85 and quality/font-scaling-a11y03.md record 1,260 rendered captures, 860 layout records, emulator observations,
negative diagnostic selection findings and the final signed APK. Main suite remains 1,335 passing. A11Y-03 stays
in progress: large currency tokens, fixed actions/bulk labels, native OS large text, custom controls and independent
screen-reader/device acceptance remain. A Settings language selection was not applied; a later fresh-visit retry succeeded.
Theme/mode choices also retained old-language captions while nearby labels changed. Record that finding separately without claiming its cause or a locale fix in this layout slice.

## D-79 - Complete Settings publication and live translated choices (2026-10-09)

Follow the actual D-78 selection/caption findings under D-69. Settings previously kept its write-suppression flag
set across asynchronous account/entry/device reads while exposing editable controls. An early selection could
therefore change the picker but not apply. Read one complete SettingsSnapshot behind SnapshotLoadState and publish
all fields synchronously before exposing the form. A failure stays covered with translated retry; repeated pending
loads share one read. Device availability checks never authenticate or request notification permission.

Rebuild mode/theme/freshness/month-start/estimate-period and default-account captions when display changes on the
open page. Restore native selection indexes under the synchronous guard, retaining its prior nested state. Do not
replace unsaved estimate/reminder text. Subscribe to localization only while the page appears. Keep D-67's deferred
Shell rebuild, current theme/security choices, estimate formula and all financial data semantics.

AT-86 adds ten cases compiling actual SettingsSnapshot/SettingsChoiceLabels with real isolated SQLite and
localization, including delayed reads, failure/retry, unsupported notifications, saved lock availability and all six
languages. No fake MAUI controls or copied view-model algorithms. Main suite: 1,345 passed, zero failed/skipped;
App.Tests: 127. Actual view-model/binding/native retry and caption behavior are checked separately in the running app.
See quality/font-scaling-a11y03.md and AT-86 for final rendered/native/APK evidence. No model, permission, SDK,
backup/security policy or commercial restriction change; A11Y-03, real OS/screen-reader/phone/iOS gates remain open.

## D-80 - Growing captions for Settings, account/debt and bulk actions (2026-10-09)

Continue the actual A11Y-03 clipping findings under D-69. At narrow 200% text the Settings delete caption is cut off;
half-width debt actions and four narrow bulk columns likewise constrain complete words. WrappingAction paints a
growing semantic Border/Label and places a real transparent Button last, following the existing D-42 chip pattern.
The label retains native scaling; the button owns the existing command/argument, CanExecute, keyboard and spoken
name. Theme colors remain dynamic semantic resources. Do not invoke destructive or financial commands to review it.

Account/debt detail actions use the whole width; Settings suggestion/permission actions have their own row. Bulk
selection count has its own line, Select all/Cancel another, and four actions use two rows. Retain the existing
12-point bulk text with native scaling and compact side padding; initial 15-point wrappers split short English words
at 360/200%, so do not turn that diagnostic prototype into the delivered baseline. Remove fixed 34/40 heights from
transaction review/selection actions without changing handlers or filters.

AT-87 is runtime evidence, not a new xUnit count. The actual native Select all/Cancel Invoke pattern reaches the
existing commands without writing entries. Actual caption geometry, complete spoken names and command/argument
bindings are checked; inherited disabled selection state is recorded. Main suite remains 1,345 passing. See the
large-text quality report for final matrix, native and full signed APK evidence. No data model, finance formula,
security policy, new resource key, permission, SDK or commercial rule change. Headers/navigation, currency layout,
other controls and physical OS/screen-reader/phone/iOS acceptance remain independent work.

## D-81 - Complete date parts and large amount packets (2026-10-09)

Continue the actual A11Y-03 fixed-date/currency findings under D-69. DateField reserves all four year digits and
both day/month digits at the actual native scale, including entry chrome. Whole input/separator groups reflow;
retain real Entries, calendar/culture order, validation, partial drafts and the Gregorian DateOnly binding. A
partially typed year must never replace the last valid date. No Save is required for presentation review.

AmountReadout retains the existing AmountLarge style, original MoneyText packet and full semantic description.
A real horizontal ScrollView exposes oversized sign, digits, decimal and currency without shrinking the native
font or inserting breaks inside the number. A translated hint appears only on overflow, in all six languages.
Account detail gives its amount the full row and separates movement labels/values. Home's account-link button
covers only its visible heading, leaving amount scrolling available; its existing command/name is unchanged.

AT-88 is actual runtime evidence, not extra copied/fake-control unit cases. Main suite remains 1,345 passing
(App.Tests 127). Check complete actual native date digit geometry, valid/partial/calendar drafts and unchanged
entries; use native Scroll patterns for both amount ends and measure realized glyph boundaries. A detached
TextBlock gave a different width for the bundled Persian font; it is not reliable proof of realized clipping.
Review awaits also use the native Scroll pattern rather than waiting for an event at an already clamped boundary.
See quality/font-scaling-a11y03.md for final captures, native checks, negative review findings and the signed APK.
No data model, financial formula, security/backup policy, permission, SDK or commercial restriction change.
Headers, other controls, real OS/screen-reader/phone/iOS and unresolved owner gates remain independent work.


## D-82 - Repeated Settings language changes and retired navigation (2026-10-09)

D-81's real reopened-language failure interrupts Translator.SetCulture: Android's retired ShellItemRenderer still
receives translated section titles after its Shell handler is cleared, then SetupMenu reads a missing MauiContext.
The picker/view-model and Translator already hold the requested language, but the synchronous exception prevents
remaining captions and the platform locale notification. A fresh cold page is not a repair for repeated navigation.

Before replacing the Shell root, remove only Title bindings from its retired item/section/content graph. Create the
replacement first and retain normal tab restoration, deferred rebuilding while a form is open, and the lock gate.
Do not blanket-disconnect controls, change framework packages or catch and hide the translation failure.

The real reopened-form regression also finds a constructor write: initializing ReminderDaysText invokes the same
save callback as user editing. Suppress writes only during synchronous construction/publication. The original
negative run changes only UpdatedAt; all actual preference fields, accounts and entries already match. Require
complete JSON equality, including that timestamp, after the repair. No financial or security rule changes.

AT-89 uses actual native Picker SelectionItem and header Invoke patterns, keeps retired shells strongly referenced
and checks frozen old titles, current captions, preserved open drafts and stored settings/accounts/entries. See
quality/font-scaling-a11y03.md for final Windows, normal Release emulator, strict build/test and signed APK evidence.
No new resource key, schema, data field, SDK, permission, portable preference or commercial restriction.

## D-83 - Growing Windows page titles and live native Back descriptions (2026-10-09)

Move Windows child-page headers out of Shell's fixed-height TitleView into an Auto row above the existing body.
Wrap complete scalable titles, including long German words; retain the 44 px native Back target and existing Shell
back handling. Bind its spoken name and tooltip to live translations. Preserve inherited body bindings during
reparenting, attach once, and size the current header/body root as one centered readable column on resize.
Modal editors keep their existing header/Cancel/discard handling; Insights tabs are a separate remaining finding.

Android uses a scoped toolbar tracker through the existing Shell renderer factory. Keep the native arrow and
navigation commands; update only its accessible Back description from the app's live Common_Back resource.
Retired/disconnected toolbars are ignored and translation subscriptions are removed during disposal. Changing
BackButtonBehavior.TextOverride would replace the arrow and is not used. No package, SDK, permission or data change.

AT-90 measures actual native title glyph bounds, full text and live Button peer names; it checks real Settings
body/bindings, unsaved drafts during resize, single attachment after nested return and native Back without writes.
The existing Settings Appearing reload resets an unsaved estimate after a nested return; retain that independent
negative finding for the next repair, rather than claiming this header change fixes its load policy.
See quality/font-scaling-a11y03.md for final runtime/build/test/APK evidence. Continue ready work under D-69.

## D-84 - Preserve the open Settings estimate across reload (2026-10-09)

The D-83 runtime review finds that Settings.OnAppearing reloads persisted preferences after a nested return and
resets an unsaved estimate. Keep full covered reads, retry and availability/account refresh; merge only this explicit
Save field's raw text, period and currency against its last published baseline. Clean fields take new saved values;
dirty fields keep exact incomplete/invalid text. Scope the baseline by profile and settings-row identity, since a
restored copy can share row ids. Keep all draft state in memory; no autosave or persistence/schema change.

Retain the input's currency when a default changes, and hide a newly computed suggestion in a different currency.
Save captures one submitted input and accepts it as the baseline only after persistence succeeds. Later typing
remains dirty; a late save for a retired profile cannot replace the new baseline or label a changed draft as saved.
Existing money parsing, explicit Save/clear validation and notification/security policies remain.

AT-91 adds 13 actual linked-source cases, including real SQLite no-write/currency checks, incomplete input, period-
only changes, clean reloads, copied profile ids and edits during Save. The real headers route now retains its draft
through nested return rather than restoring it before navigation. Final runtime/build/test/APK evidence is in
quality/settings-estimate-draft.md. D-69 continuous delivery and unresolved owner gates remain.

## D-85 - Keep all Insights destinations visible with large text (2026-10-09)

The current 360 px / process-local 200% Windows budget render clips Forecast and puts Goals outside the visible
Shell TitleView. Move the four Windows root destinations to a growing Auto row above the same retained body.
Narrow columns show two rows; at 600 px of actual navigation width, four destinations share one row. Keep the
existing route names, selected underline, scalable display font and semantic palette. Attach once and retain
body bindings through reparenting and own-window resizing. Android/iOS keep their existing native top tabs.

Every painted caption has a real native OverlayButton last, with full translated name, route parameter and a
44 px minimum target. Selected hints translate live; invoking an unselected target uses the existing Shell route.
AT-92 measures actual native glyph layout slots, target/name/selection geometry, all four native Invoke destinations,
single attachment and complete stored data equality. Centered WinUI caret bounds include allocation alignment;
a tight text ActualWidth alone is not the arranged caption viewport. Keep that initial diagnostic failure evidence.
See quality/insights-navigation.md for final build/runtime/APK evidence and remaining findings.

## D-86 - Complete budget figures and signed boundary formatting (2026-10-09)

The actual narrow Windows large-text budget render clips spending and limit units when two packets share one
horizontal row. Give the wrapped identity, spending and limit separate rows spanning the whole available width.
Envelope labels and values also use separate rows. Reuse AmountReadout's full signed packet, spoken description,
translated overflow hint and native horizontal viewport, with optional CaptionStyle retaining the original scalable
14 px body/bold and 13 px secondary typography. Existing large callers retain AmountLarge. No budget calculation,
progress, carry, method, schema, financial Save, security or policy changes.

The actual signed-boundary presentation fixture separately exposes Math.Abs(long.MinValue) throwing before
MoneyText can format a negative value or its input magnitude. Convert to decimal before taking the magnitude,
including display units. Eleven independent AT-93 cases fail before the fix and pass afterward: EUR/en/de, zero-
and three-digit ISO currencies, approximation/bidi packets, IRR/Toman and unchanged out-of-range positive parsing.
Unsigned input at that magnitude remains invalid; this does not expand the ledger or input range.

AT-93 uses actual native caption geometry and Scroll patterns, restored in-memory presentation fixtures and full
stored accounts/entries/settings/budgets/plans equality. Running-app evidence, negative captures, suite/builds,
normal signed Android Release checks and package limits are recorded in quality/budget-readouts.md.

## D-87 - Keep every budget period decision visible (2026-10-09)

After D-86, the actual 360 px/process-local 200% budget render leaves Every 2 weeks almost completely outside the
compact ChoiceChips viewport (x 260, width 196, viewport 261.33). The native caption itself is complete but the
choice is hidden. Keep the negative geometry/render evidence. These three decisions belong to the form: use the
existing wrapping, full-size ChoiceChips mode instead of the horizontal compact filter mode. Keep all original
items, two-way selection, translated selected names, semantic palette and native scaling. The existing full-size
mode supplies growing labels and 44 px minimum targets; no shared control, calculation, schema or policy change.

AT-94 measures each realized caption/layout slot and native target inside the growing group, full translated
selected names and bold state. Invoke each actual native button to Month/Week/TwoWeeks, join its existing
serialized reload and verify the selected view/period text. Restore the original choice and compare complete
accounts/entries/settings/budgets/plans without Save. Actual matrix, builds/suites and signed normal Android
Release evidence are recorded in quality/budget-periods.md. External platform/owner acceptance remains separate.

## D-88 - Align current documentation with implemented capabilities (2026-10-09)

Complete ZCR-GOV-02 under D-69 by correcting the original audit's obsolete assertions: approved Zanance branding,
proprietary source ownership, implemented independent profiles/platform sign-in, optional portable-backup protection,
allowlisted display preferences and D-61 Free/Plus/Pro/Plus Lifetime design. Date the original ZEX/ZCR review-only
state as historical and retain story/evidence links. Current pricing values are design/sandbox values, and no quota,
billing, production encryption or OS-backup decision is introduced.

Privacy documents distinguish Android external-camera capture from iOS contextual permission, offline Release from
configured Cloud SDK/network behavior, and protected device token caches from actual provider authorization/API
exchanges. Missing configuration and real-provider acceptance are separate from implementation. iOS source/target
presence is not a verified app build; Mac/Xcode/signing and physical/provider/release gates remain open.

A focused documentation assertion audit checks corrected requirements, current source/manifest anchors, relative
links, preserved historical evidence counts and absence of code/build changes. Existing D-87 logs/APK supply the
unchanged 1,369 main-test/App.Tests 140 engineering baseline; tests/builds are not repeated for text-only changes.
The correction matrix is in enhancement ZCR's 01-current-state.md Section 5. OD-10/12 and other open owner choices
remain pending, and no owner archive, finance data, security state or credential is accessed.

## D-89 - Full tag captions and usable native targets (2026-10-09)

An observed normal Release tag target is under 44 dp; the narrow Windows large-text editor also clips long suggestion
captions. Use bounded growing rows with the existing WrappingAction native overlay pattern, preserving the 13 px
scalable font, page surface/blue action meaning and rounded appearance. A separate Suggestion appearance leaves
existing action variants unchanged. Keep raw tag identity in TagSuggestion.Value, display through EntryTags.Display,
and forward the raw command parameter. No tag-limit, normalization, financial Save, schema, SDK or policy change.

Retain the failed FlexLayout wrapping prototype and recycled-caption helper evidence. AT-95 reacquires each real
native target, checks full caption/name and bounds, invokes AddTag on an unsaved fictitious draft and restores exact
original draft state with full stored-row equality. Final runtime/build/test/APK evidence is recorded in
quality/tag-suggestions.md before completing this delivery step. D-69 continuation and owner/platform gates remain.

D-89 verification: 84 real native tag invocations and 21 exact restorations pass across en/fa/de, both themes and
360/412/wide at process-local 200%, with a 100% narrow baseline. Main suite remains 1,369 (App.Tests 140); strict
Windows and equivalent-command Android Release builds have no warnings/errors. The normal signed Release verifies
the original tag value, Keep editing/Discard and complete unchanged fictitious financial rows. PowerShell startup
prevented the canonical script from running; no system setting was changed. See quality/tag-suggestions.md for the
complete APK, signature, helper failures and independent platform/tooling/owner limits.

## D-90 - Name the transaction detail action by its current state (2026-10-09)

D-89's normal Release hierarchies show detail inputs already expanded before the helper taps "More details";
Advanced initializes that state. The tap hides them correctly. Correct the misleading caption rather than claiming
a repaired tap failure: say "Hide details" while expanded, retain "More details" while collapsed, with six real
translations and mutually exclusive growing native actions. Keep the same ToggleDetails command, defaults,
receipt/edit behavior and unsaved fields; explicit Save remains. No model, schema, SDK, permission or policy change.

AT-96 retains the failed before-caption review, checks actual native commands/full names/targets and the editor's
complete draft fingerprint, preserves bound payee/note/tag inputs through hide/show and restores original draft
and complete stored rows. Final matrix/build/test/native/APK evidence: quality/entry-details-disclosure.md. D-69
continuation and the independent owner/platform gates remain.

D-90 verification: 48 real native visibility toggles and 24 complete draft/stored-row restorations pass across
en/fa/de, both themes, 360/412/wide at process-local 200%, narrow 100% and Simple initial visibility. Main suite
remains 1,369 (App.Tests 140); strict Windows and canonical Android Release have zero warnings/errors. Normal
Release passes retained payee/tag/note drafts, Keep editing/Discard in all three languages, unchanged complete
fictitious financial rows and the final secure-state handoff. Canonical APK/privacy scripts completed; full
package/signature evidence and independent gates are in quality/entry-details-disclosure.md.

## D-91 - Complete restore alternatives in onboarding (2026-10-09)

The actual German 360 px/200% first-account screen clips its existing restore caption. Use the existing growing
Secondary action in both places, retaining full existing translations, native scaling/command/name, busy binding
and minimum targets. Keep the wizard footer, editor actions, account/restore rules and data model unchanged.
AT-97 invokes actual Restore/Back on both steps, retains complete draft choices and compares stored rows. Final
evidence: 273 renders/84 native Windows invocations, normal signed Release en/fa/de round trips and removed disposable
profile, 1,369 passing tests, strict Windows/canonical Android zero-warning builds and verified full APK/privacy.
See quality/onboarding-restore-actions.md for evidence and separate physical/platform/provider/owner gates.

## D-92 - Complete Home snapshots without redundant work (2026-10-09)

Measured actual Home reloads rebuild every account row and empty goals rescan the ledger per account. Publish a
complete fresh account snapshot with one Reset, retaining native rows; skip empty goal work and route nonempty
goal balances through the existing index/calculator. Preserve every value, order, priority, action and ledger rule.
AT-98 adds 15 cases and actual-bound native row/data checks. Controlled tenfold Windows reload median falls from
2,727.70 to 1,228.28 ms. See quality/performance-home-snapshots.md for completed build/runtime/APK evidence and
excluded temporary diagnostic failures. Cold start, physical-device Q-02 and QA-06 remain open.

## D-93 - Match the Home diagnostic's platform boundary (2026-10-09)

D-92's Windows-only Home review was called outside WINDOWS, breaking Android Debug with CS0103. Guard the call at
the same boundary, retaining the existing Windows/runtime behavior. Android Release was unaffected. Final strict
Windows and complete Android Debug/Release builds, 1,384 tests, actual Windows Home and signed Release/data proofs
pass. Independent SQL/native Debug measurements reject an unhelpful index and select materialization/initial row
creation as the next QA-06 paths; no temporary instrumentation, production index or schema change remains.
Evidence and limits: quality/home-debug-platform-and-native-stages.md.

## D-94 - Complete Home customization names and controls (2026-10-09)

Give section identity a full-width growing row above its existing controls, keep native text scaling/RTL,
set local 44 px arrow/switch minimums and use the existing growing Reset action. Narrow D-92 names were truncated;
one-column wrapping still broke words, and actual geometry found 40 px arrow targets. Preserve all eight sections,
commands, defaults and profile preference behavior. AT-99 checks real native geometry and persisted move/visibility/
Reset actions on fictitious data. Final language/theme/width review, 1,384 tests, zero-warning Windows/Android Debug/
Release builds, complete signed APK and exact owned-sample readback pass. Remaining accessibility, cold-start,
physical-device and external gates are explicit: quality/home-customization-readable.md.

## D-95 - Avoid native rows for hidden Home accounts (2026-10-09)

Actual bound Android measurements found every default-hidden Account row constructed with a native handler.
Attach the unchanged complete snapshot only while the section can display it; detach when hidden. Keep the same
source and rows on visible reload, all values/actions/calculations and the existing layout preference. AT-100 invokes
the actual visibility switch, account detail/list and Back on fictitious data; AT-98 distinguishes visible native
retention from a hidden complete snapshot without views. Before/after stage evidence, final layout/native checks,
1,384 tests, strict Windows/Android Debug/Release builds, full signed APK and complete original-table readbacks pass.
The measured account-publication median for 200 accounts falls from 5,339.89 to 10.69 ms. This does not complete Q-02
or claim the remaining full entry read/visible row creation, ANR, physical device or release gates.
Evidence: quality/home-hidden-account-views.md.

## D-96 - Grow complete account type and status descriptions (2026-10-09)

Native German 360 px/200% review found a two-line excluded caption clipped by its shorter MAUI label. Bound each
badge to the actual group width before wrapped measurement, disable flex shrink and align at line start. Preserve
all complete captions, scalable typography/colors/flags, compact grouping, balance ordering and real row action.
AT-101 covers all eight flag combinations without Save, counts complete captions and checks actual glyphs against
both native and MAUI bounds. Full language/theme/width review, 1,384 tests, strict Windows/Android Debug/Release,
signed Release navigation and exact original-table readbacks pass. The initial slot-only false pass and tightened
baseline failure are retained as negative evidence. Real OS/readers/phone/iOS and remaining A11Y-03 controls stay open.
Evidence: quality/account-descriptions-readable.md.

## D-97 - Grow the complete debt entry action (2026-10-10)

Actual German 360 px/200% Accounts review found the Add debt/receivable caption extending beyond its native
button. Use the existing Secondary WrappingAction with unchanged complete text, native scaling and AddDebtCommand.
AT-102 checks actual glyph/MAUI/native bounds and spoken name, then invokes the existing unsaved Loan draft and
Cancel while comparing full stored accounts, entries, settings, budgets and schedules. Wait for actual modal date
arrangement before capture; retain the existing digit assertions and unchanged production date field. Final language,
theme/width review, 1,384 tests, strict builds, signed Release navigation and exact original-table readbacks pass.
Other A11Y-03 controls and actual OS/readers/phone/iOS gates remain open. Evidence: quality/debt-entry-action-readable.md.

## D-98 - Keep one readable modal header (2026-10-10)

Actual Windows debt review found a duplicate generic child header and an unnecessarily narrow own title.
Exclude explicit Shell modal presentation modes from PageHeader attachment; retain ordinary child headers.
All eleven existing own modal headers reflow complete scalable title and nonshrinking Cancel/Close, preserving
commands, subtitle, forms and footer actions. AT-103 checks actual native glyphs/name/target/header overlap,
retained bodies, native cancellation, same ordinary parent/Back and all nine complete stored data sources.
The full Windows matrix, 1,384 tests, strict builds, normal Android debt/expense/plan language/theme navigation,
signed APK and exact three-profile 24-table readbacks pass. Remaining A11Y/platform/release gates stay open.
Evidence: quality/modal-headers-readable.md.

## D-99 - Show all plan-field problems and reveal the first input (2026-10-10)

Actual invalid native transfer Save postpones destination feedback and leaves prior field errors offscreen at the
footer. Collect all applicable field problems before entity mutation; keep existing money/recurrence rules and
show each next to its input. Use a transfer-safe positive-amount caption in all six languages. Reveal the first
problem without focus/typing. A retry exposed stale layout after clearing earlier errors; resolve current native
layout before scrolling, scope requests to visible/latest validation, remove Android observers and avoid WinUI
redundant/clamped MAUI scroll awaits. The full language/theme/width matrix, 24 new behaviour cases/1,408 tests,
strict builds, normal Android invalid Save/cancel and exact original-table readbacks pass. Other platform/control
and owner/release gates remain. Evidence: quality/plan-validation-visible.md.

## D-100 - Collect transaction-field errors before mutation and reveal the next input (2026-10-10)

Actual invalid transfer Save postpones destination/fee feedback behind the amount; original-currency and
reimbursement problems also use general footer messages. Collect all independent applicable monetary problems
before entry/fee mutation, reuse existing parsers/domain restrictions, show each beside its input and reveal
collapsed invalid details without losing values. Use six-language transfer-safe positive-amount wording.
Native retries expose Windows caret/layout request ordering; request the actual target through deferred native
StartBringIntoView without focus or persistent keyboard policy changes. Keep latest/visible guards and bounded
removed Android observers. Forty-eight new cases/1,456 main tests, the full Windows matrix, strict builds,
normal Android invalid Save/cancel, signed APK and original 24-table readbacks pass. Simple existing destination
fee retention remains a separate pending runtime concern. Evidence: quality/entry-validation-visible.md.

D-100 final callback review: native Windows callback failures rejoin the awaited action guard; timed-out queued
requests expire. Eight final German invalid Saves/61 renders, strict rebuilt Windows/Android Debug/Release,
one final-APK English Android invalid Save, repeated original 24-table readbacks and final native handoff pass.
The main validation tests are unchanged. Original full-matrix counts and the pre-guard Android candidate remain
distinct from these final checks; evidence: quality/entry-validation-visible.md.

## D-101 - Preserve existing destination fees when editing in Simple (2026-10-10)

Native successful Save reproduces silent destination-fee removal during an unrelated note edit in Simple.
Expose an existing fee in Simple as well as Advanced so validation/effect/save consume the loaded value; refresh
the localized net-effect summary when visibility changes.
Keep fee creation Advanced-only and explicit blank/zero removal. AT-106 proves retention/edit/removal with
unchanged financial fields, ids and unrelated rows, then reopened/new forms and native Cancel/Discard.
The 24-context Windows matrix/72 valid Saves, 1,456 tests, strict builds, signed APK, normal Android creation
policy checks and original financial/preference values and expected settings audit pass. Android successful stored-fee editing/physical/iOS acceptance
remain separate. Evidence: quality/destination-fee-retention.md.

## D-102 - Explain settlement fields and retain the complete action (2026-10-10)

Native execution reproduces unexplained disabled Save for reversed dates/invalid bills and a clipped scaled
action. Collect all independent errors next to their fields, initialize DateFields before binding, explain an
empty payment period and reveal the first affected input on invalid Save. Keep the action available while idle
and its full scaled caption/native spoken name. Preserve drafts and the original advance/refund continuation:
zero may refund remaining advances, equal bills write nothing, and only a difference becomes a new entry.
AT-107 adds 14 cases (main 1,470/App.Tests 234). The 24-context Windows matrix/144 invalid and 72 valid native
Saves, six Release emulator contexts/18 invalid Saves, strict builds and complete signed APK pass. Original
financial rows/preference values remain exact; normal display-choice audit timestamps are separate.
Physical/iOS/readers and valid Android financial settlement acceptance remain open. Evidence: quality/settlement-feedback.md.

## D-103 - Complete occurrence actions and correctly placed amount feedback (2026-10-10)

Real execution reproduces clipped due-item actions; the override explanation belongs beside its own field rather
than in payment. Keep full growing native action captions/names/targets and separate payment/override errors,
independent corrections and revelation of the actually attempted input. Initialize DateFields before binding.
An empty override retains the plan amount, positive payment rules and the original partial/unique-completion
continuation remain; metadata never posts ledger entries. Change Save uses the existing busy convention.
AT-108 adds 14 cases, main 1,484/App.Tests 248. The 24-context Windows matrix/96 invalid and 96 valid Saves,
plus a separate 24-context/96-invalid final contextual-message review after correcting the payment wording,
six normal-scale Release emulator cases/18 invalid Saves, strict builds, complete signed APK and original-data
checks pass. Real device/iOS/readers and valid Android occurrence acceptance remain open.
Evidence: quality/occurrence-feedback.md.

## D-104 - Reuse transaction rows within a complete source/display snapshot (2026-10-10)

Actual bound-page measurements identified repeated presentation construction: tenfold warm group/row median
219.33 ms. Reuse row identity within a snapshot, invalidate on every data read or display-context change, and reapply
existing bulk selection. All rows/order/filter scopes/amounts and day nets remain; there is no capped result list.
Measured tenfold group/row construction is 38.31 ms and complete warm refresh 583.14 -> 358.90 ms. Native publication
is slower in this cohort and remains open. AT-109 adds four tests: main 1,488/App.Tests 252. See
[quality/transaction-row-reuse.md](quality/transaction-row-reuse.md) for final checks and explicit performance limits.

D-104 final checks: main 1,488 tests, strict Windows/complete Debug+Release Android, 21 Windows contexts/609 own renders, seven Release emulator search/filter contexts, original data and a complete signed D-104 APK pass. Physical-device/cold/Release-timing and native-publication gates remain open.

## D-105 - Complete indexed bulk-selection reload (2026-10-10)

Replace repeated full-ledger membership searches with one complete id index per fresh bulk snapshot. Retain known
selection, prune only removed ids, refuse unknown ids, and preserve current financial copies/validation/Undo and busy
dialog guards. Actual bound full-selection Windows reload median: 100,000 entries 25.30 -> 1.42 seconds; this ends at
LoadAsync and is not completed-frame/cold/device/ANR acceptance. Three new behavior cases bring main tests to 1,491,
App.Tests 255. Final running-app/build/APK evidence and open limits: quality/bulk-selection-reload.md.

D-105 final checks: main 1,491 tests, strict Windows/complete Android Debug+Release, 21 Windows contexts/609 own renders, seven native Release bulk-selection contexts, original-data readbacks and a complete signed D-105 APK pass. Cold/ANR/native-publication/device/platform and owner acceptance gates remain open.

## D-106 - Read exact complete-month history for Settings suggestions (2026-10-10)

Measured Settings loading materialized all ledger history for a calculation that uses only three complete financial
months. Read every entry in that inclusive calendar/pay-cycle window, preserving the existing calculator, currency/
account/refund/plan rules and covered complete publication. No cap, schema, security, portable data or permission
change. Actual bound tenfold Windows warm LoadAsync median: 883.78 -> 157.74 ms, excluding later arrangement/painting.
AT-111 adds 21 cases: main 1,512/App.Tests 276. Final runtime/build/APK evidence and independent performance/platform
gates: [quality/settings-suggestion-history.md](quality/settings-suggestion-history.md).

D-106 final checks: 1,512 tests, strict Windows/complete Android Debug+Release, 21 Windows contexts/1029 own renders and 63 native/bound proofs, seven positive Release suggestion/reopen contexts, original-data readbacks and a complete signed D-106 APK pass. Selected-picker caption geometry and cold/ANR/device/platform/owner gates remain open.

## D-107 - Growing native picker selections and popup rows (2026-10-10)

Actual Persian 360/200% Settings captures clipped complete calendar and region names in WinUI's single-line picker
presentation. Use a wrapping native item template while retaining the same MAUI/native picker, source, selection,
names, inherited typography/direction/colors and native selection. Normal-text popup review found a 42.6667-unit
row; extend its existing style with a 44-unit minimum without replacing the native theme template. No financial,
data, schema, SDK, permission, security or portable preference change. AT-112 checks actual selected/popup glyphs,
fonts/targets, native reselection and retained complete drafts/stored rows; the shared template also requires other
picker pages to be checked. Final verification passes: [quality/native-picker-captions.md](quality/native-picker-captions.md).

D-107 final checks: main 1,512 tests, strict Windows/complete Android Debug+Release, 21 Windows contexts/2083 own renders, 189 native existing-choice reselections, 84 language choices/returns, full developer-file restoration, normal Release navigation and exact 24-table readbacks across three fictitious profiles pass. Complete signed D-107 APK supplied; OS/readers/device/iOS/other-control and owner acceptance remain open.

## D-108 - Complete report-scope captions and reachable transaction results (2026-10-10)

The actual German 360/normal-text report drill-down clips scope/category text and places its native Clear targets
outside the page (right edges 376.67 and 411.33 against a 345.33-unit native root); all three Clear buttons are
only 36 units wide. Bound complete wrapping captions in a star column with an independent 44-unit Clear column.
The first 200% candidate then leaves a zero-height result viewport. Keep the complete filter form in a vertical
viewport bounded by the actual page and current action docks, reserving up to 144 logical units for results.
Native text scaling, horizontal period/kind choices, result order/totals and the existing shared Clear command remain.
No schema, money, SDK, permission, security or saved-filter behavior changes. AT-113 reviews real native caption/
Clear geometry, header/result scrolling, Clear and exact query restoration without Save.
Final checks pass: 1,512 main tests (App.Tests 276), strict Windows and canonical complete Android Debug/Release;
21 Windows contexts/1615 own-window renders, 63 complete native scope captions/Clear targets, 21 native
result-scroll/Clear/query-restoration checks and 84 existing expanded-filter targets. Original developer
files return with exact hashes. Normal signed Release follows the actual Reports -> Transactions -> Clear route on
the owned emulator without preference, security or financial writes; all 24 tables in each of three fictitious
profiles are exactly unchanged. Complete signed D-108 APK verified.
Evidence: [quality/transaction-scope-readable.md](quality/transaction-scope-readable.md).

## D-109 - Independent named filters after report navigation (2026-10-10)

Native reproduction shows the same nonempty saved Food filter becomes empty after a USD/confirmed report query;
the old scope caption also remains. Applying a saved filter must restore its persisted combination independently
of a previous report. Share the existing transient report reset with Clear, preserving every saved field and
without adding report-only fields to the schema or backups. AT-114 compares actual native actions/results before
and after independent report restrictions, complete stored data and original-file restoration.
Final six native Windows contexts (en/fa/de, light/dark, normal text/360) pass all 18 independent report
restrictions, with 234 own-window renders, identical complete saved-filter results, removed old scope
notes and unchanged complete stored rows. Every original developer database/WAL/SHM file returns with matching
hashes. Main suite: 1,512 passed, zero failed/skipped (App.Tests 276); strict Windows and canonical complete Android
Debug/Release builds pass without errors/warnings. Normal signed Release on the owned emulator creates a named
filter through the actual menu, follows Reports -> Transactions, applies that native filter and removes only its
fictitious saved-filter record. Original three transaction rows return and all 24 tables in each of three fictitious
profiles match the baseline exactly, including saved filters and Settings audit fields. No financial Save.
Evidence:
[quality/saved-filter-report-scope.md](quality/saved-filter-report-scope.md).

## D-110 - Retain complete unchanged native transaction sources (2026-10-10)

Equivalent searches republish the same full grouped result to the native control. Compare complete ordered group
captions/boundaries and row object identity before replacing Days. Fresh data/display snapshots still publish new
sources; mutable selection and all financial behavior remain. Actual 100,000-entry warm publication median:
407.51 -> 1.61 ms; no result cap. AT-115 adds nine cases, main 1,521/App.Tests 285. Final native/build/package
verification passes: six native Windows contexts/186 own renders, strict builds, normal signed Release, exact
original 24-table readbacks across three fictitious profiles and complete signed APK. Evidence:
[quality/unchanged-transaction-source.md](quality/unchanged-transaction-source.md).

## D-111 - Repeat from a new transaction without posting it (2026-10-10)

OD-12 was explicitly approved by the owner: open a prefilled Plan from a new transaction draft, without recording
money. Use a real growing action after the complete basic fields. Validate existing base monetary inputs, carry
positive minor units/currency identity in memory, anchor Monthly to the selected first date/calendar and leave
automatic posting/reminders off. Keep original unsaved details on return and disclose fields not copied to plans.
Recheck account availability/currency and share the receipt-unit guard. The recorded-entry path is unchanged.
Fourteen new cases pass (main 1,535/App.Tests 299); initial native expense/income/transfer and invalid-input checks
pass. Final Windows/native presentation, strict builds, normal signed Release explicit Plan Save/cleanup and
exact original-data readbacks pass: [quality/entry-repeat-draft.md](quality/entry-repeat-draft.md).

## D-112 - Retain full entry reads after a bounded enumeration comparison (2026-10-10)

QA-06 compared the current async EF query with synchronous enumeration inside the same worker/context boundary.
Full ordered packets, date-bound results and fictitious stored rows match. The tenfold candidate is slower in
four of seven pairs, with effectively unchanged allocation despite a lower median. Retain production behavior;
do not infer Android or cold-start improvement or repeat without a new measured cause. Documentation only;
D-111 tests/build/native/APK evidence remains the app baseline. See
[quality/entry-read-enumeration-comparison.md](quality/entry-read-enumeration-comparison.md).

## D-113 - Complete growing transaction category choices (2026-10-10)

An observed native 360 px/200 % entry-form caption defect continues approved A11Y-03 work under D-69.
Bound each complete chip, wrap its scalable text column and let rows retain independent heights. Keep original
raw choice objects, semantic colors/icons, command and real last-child button; selection stays an unsaved draft.
33 contexts/99 native selections, full glyph/viewport/name checks and complete draft/store/developer-byte
restoration pass. Main 1,535/App.Tests 299, strict Windows/complete Android builds, signed APK, normal Release selection/discard
and exact original-data readbacks pass. Keep negative prototype evidence and broader platform gates: [quality evidence](quality/entry-category-captions.md).

## D-114 - Decline interim OS-backup exclusions (owner, 2026-10-10)

The owner will not use Zanance until all planned sections/phases are complete and explicitly declines the
temporary OD-10 proposal to exclude plaintext financial files from OS backup/device transfer until encryption.
Do not implement interim product policies or temporary substitutes for final planned behavior. No runtime
manifest/resource/iOS/backup change is made. Determine permanent backup/key/recovery handling alongside completed
SEC-02..08; this does not approve a final policy or mark those sections accepted. Ready permanent delivery continues.

## D-115 - Resolve the keyboard-covered native gesture hypothesis (2026-10-10)

The empty-entry gesture observation is caused by an IME-covered origin in the native review, not evidence for a
category-gap or blank-amount correction. Normal D-113 Release, the same blank draft and identical gesture expose
the complete Title/date after native Back removes the actual IME window. No typing or Save occurs. Read current
input-window state rather than one input-method flag, a compressed hierarchy or retained ANR history. Revert the
failed InputTransparent candidate and remove the diagnostic debugger/owned forward. No production workaround is
adopted; completed D-113 tests/build/APK evidence stays the baseline. Last-position and broader platform acceptance
remain open. See [quality evidence](quality/entry-category-captions.md#empty-form-gesture-investigation-d-115).

## D-116 - Show complete savings-goal warnings once (2026-10-10)

Actual native goal cards repeat the same full WarningText packet above and below progress. Remove the earlier
duplicate from both list templates, keeping all distinct warnings together after progress/date/suggestion, their
original typography/color/visibility and unchanged full-card command/name. No financial algorithm or schema change.
AT-118 checks both actual native templates with fictitious combined warnings, complete glyphs/viewport, native digit
shaping and original presentation/stored-row restoration. Windows 27 contexts/54 packets pass; full delivery
verification is recorded in [quality evidence](quality/goal-warning-packets.md). Broader platform gates remain open.

## D-117 - Approve final commercial policy and implement ENT-01 (owner, 2026-10-10)

The owner approves OD-03 and the Section 2 plan/tool matrix: paused goals/plans count, Free has one monthly limits
budget definition, three quick templates and one saved filter; financial month start and basic forecast to the
financial month end are Free. Other tools follow the confirmed matrix. This approves final Core types/policy/tests
only; test-build limit activation remains a separate decision. No temporary entitlement or production backdoor.

Implement immutable verified-input grants with explicit validity, Plus Lifetime fallback, exact personal/shared
contexts, exhaustive capability decisions and scoped quotas/counts. Membership/roles/payment verification/readiness
stay separate; an expired host's Pro cannot commercially hide retained data. Counting uses actual entity states,
canonical budget definitions and explicit pending-seat expiry without financial writes or guessed data selection.
76 new policy cases pass; full suite 1,611, App.Tests 299 unchanged. Strict platform builds and signed APK are
recorded in [quality evidence](quality/entitlement-policy.md). No app behavior, schema, permission or SDK change.

## D-118 - Enforce account capacity at the actual SQLite write boundary (2026-10-10)

Continue approved ENT-02 with a reusable write transaction and the account store boundary. An enabled, already
verified-input access snapshot is bound to the exact opened database path, not a later current-profile preference.
Acquire the SQLite writer before existing-state/count reads, check permission and capacity, save and commit before
Changed. Independent providers compete for the same database writer. Reject changed or mismatched snapshots;
provider capture is synchronous cached state, never purchase/network work inside the transaction.

Creation and unarchive consume active account slots; existing corrections and archival remain possible above quota.
Membership is checked even on corrections or newly archived accounts; personal Pro never substitutes for it. Failed
writes roll back without consuming a slot or notifying changes. The current registered provider is explicitly
inactive, grants no paid right and adds no quota transaction; test-build activation remains separately gated.
15 real SQLite cases pass; main suite 1,626. No schema, SDK, portable paid facts or visible UI change. ENT-02 remains
in progress for other resources, imports/restores/read-only classification and native service entry points.
Final build/APK/runtime evidence: [account-write-policy.md](quality/account-write-policy.md).

## D-119 - Guard template and filter capacity in the actual-file transaction (2026-10-10)

Continue ENT-02 through SaveTemplateAsync and SaveSavedFilterAsync. Enabled snapshots acquire the actual opened
SQLite file's writer before reading existing identities/counts. New templates and new uniquely named filters check
capacity before sort assignment, removals or audit. Existing-id edits and explicitly confirmed same-name filter
replacement reuse a slot; retained over-quota edits and duplicate-name consolidation remain possible. Membership
checks still apply. Recheck cached access, commit before Changed and roll back failed replacement atomically.

24 new real SQLite cases pass; full main suite 1,650, App.Tests 299 unchanged. Independent providers compete for
one final slot; a database trigger proves original filter recovery when replacement INSERT fails. Current
registration stays inactive with no fabricated paid grant. No UI, schema, SDK, permission or backup format changes.
ENT-02 remains in progress for other resources/import/restore/native paths; activation and translated limit UX
remain separate. Final platform/APK/runtime evidence: [template-filter-write-policy.md](quality/template-filter-write-policy.md).

## D-120 - Guard goal, plan batch and continuation writes (2026-10-10)

Continue ENT-02 with SaveGoalAsync, SaveSchedulesAsync and SaveSplitAsync under the actual-file cached-access writer.
Active/paused resources count; completed/archived goals and ended plan history do not. Reopening consumes capacity
before goal protection or Home pin changes. Whole plan batches compare stored affected states with requested states
and check net additions before any write. A verified split/resume compares the stored predecessor and transfers its
slot; existing earlier history stays, later recorded links move atomically. Failures roll back before Changed.

New earmark/quantity kinds and newly used nth/last weekday, second monthly day, weekend/holiday shift, contract or
auto-post tools require the confirmed Advanced capability. Retained tool and historical metadata corrections stay
available without enabling paid new work. Exact membership remains required; expired-host corrections do not run
a new-capacity check. No Simple/Advanced, UI language or regional calendar is used as a commercial right.

45 added real SQLite cases pass; main suite 1,695, App.Tests 299 unchanged. Tests preserve complete stored rows,
actual split ledger metadata and rollback after an occurrence move; independent providers compete for one slot.
Current test-build registration remains inactive. No schema, SDK, permission, portable entitlement or visible change.
ENT-02/03 contributions/allocations, occurrence work, other resources/import/restore/read-only/native routes and
activation remain open. Final platform/APK/runtime evidence: [goal-plan-write-policy.md](quality/goal-plan-write-policy.md).

## D-121 - Guard current-period budget saves and replacements (2026-10-10)

Continue ENT-02 through SaveBudgetAsync and confirmed ReplaceBudgetAsync. Acquire the actual-file SQLite writer
before reading existing rows/current canonical definitions. Count scope, normalized currency, period and account
set; calendar/date/row identities and copies of other months do not multiply a current definition. Device-local
injected time, each rule calendar and the existing financial-month start determine current rows without writing
Settings. Updates count their actual stored period/currency identity, not ignored incoming changes.

Check newly used advanced methods, rollover and weekly periods separately. Retained corrections and net-neutral
replacements remain possible above quota, including expired-host corrections. Check before deleting the replaced
budget; failed new limits restore every original row. Recheck cached access and notify only after commit.

26 real SQLite cases/main 1,721 pass, App.Tests 299 unchanged. Current registration remains inactive without a paid
grant. No schema, SDK, permission, backup or visible control change. Explicit selected active/read-only definitions,
future-period activation, import/restore and other resources/native paths remain ENT-02/03 work. This is a partial
write boundary, not a new automatic selection policy. Final platform/APK/runtime evidence:
[budget-write-policy.md](quality/budget-write-policy.md).

## D-122 - Guard holding writes and save purchase prices atomically (2026-10-10)

Continue ENT-02 at the actual HoldingStore boundary: direct new types/locations/events/prices and account-to-holding
conversion require ManageHoldings when checks are enabled. Existing corrections, delete and Undo retain their
approved data rights with exact membership checks. A new reasoned quantity correction has no money/group/basis or
proceeds and references an existing type. Default-location scaffolding supports retained corrections without
granting holding management. Imports preserve owned history; explicit read-only classification remains ENT-03.

Capture cached facts before acquiring the SQLite writer. Move the existing event/delete/import/conversion/Undo
transaction before full-history/id reads; do not add a separate quota transaction to inactive operations. Concurrent
sales cannot both validate the same quantity, and concurrent duplicate imports recheck actual stored ids.

The editor previously committed event/payment/fee before separately saving the purchase price. Replace that
follow-up with one atomic store call, deriving from the actual stored type currency and preserving the latest
same-type/date price identity/metadata. Keep the old overload's no-derived-price behavior for existing non-editor
callers. A failed price or retired access after SQL restores every group/old fee row; Changed fires after commit once.

46 new real SQLite cases/main 1,767 pass, App.Tests 299 unchanged. Current deployment remains inactive without a paid
grant. No schema, SDK, permission, string/layout or portable entitlement changes. ENT-02/03 selection, future
activation, contributions/occurrence/other resources/native paths and commercial activation remain open.
Final platforms/APK/normal Release evidence: [holding-write-policy.md](quality/holding-write-policy.md).

## D-123 - Guard earmarks and save the whole goal editor atomically (2026-10-10)

Continue approved ENT-02 at actual GoalStore allocation/contribution operations. Positive earmarks require
AdvancedGoals; releases, retained corrections, deletion and expired-host data rights stay available with exact
membership checks. An empty weekly/monthly date scaffold is part of the basic balance goal. Newly configured
amount/share/cut/categories/assumed-price/custom-date work uses AdvancedGoals; new contribution reminders use
ContributionReviewReminders. Historical reopening checks retained work/reminders before normalizing pins or
protection. Do not release earmarks, change balances or post contribution estimates automatically.

The editor previously committed Goal Save and ContributionPlan Save separately. One explicit store operation now
holds the actual SQLite writer across the complete draft, capacity and pin checks, goal/plan SQL and access
recheck. It retains atomicity when enforcement is inactive, without introducing a quota transaction for other
inactive individual writes. Reuse the established goal preparation rather than duplicating its financial rules.
Keep the existing contribution identity, creation metadata, cloned rule/categories and actual parent id on edits.
SQL failures and retired access restore all stored rows; Changed fires once after the complete commit.

39 new actual SQLite cases/main 1,806 pass; App.Tests 299 unchanged. Current deployment stays inactive. No model,
schema, SDK, permission, string/layout or portable paid-fact change. Explicit selected read-only resources and
automated contribution delivery plus remaining ENT-02/03/04 paths are unfinished. Build/APK and normal Release
evidence: [goal-contribution-write-policy.md](quality/goal-contribution-write-policy.md).

## D-124 - Bind recovery to its initial file and guard retained imports (2026-10-10)

An actual SQLite regression reproduced restore input changing the selected profile: the previous source created
its destination context after reading input and overwrote the second profile. Create the context first and reuse
it for native replacement and migrations. Old-schema restore also upgrades the same initial file. Optional cached
authorization callbacks run before consuming input and before native copying/output; the shared library remains
independent of commercial policy. Do not hold an external writer transaction around SQLite's online backup API.

Zanance registers a specialized database source for BackupRestore rights, replacing only its own generic source.
Preserve other backup sources. CommercialFileAccess centralizes exact-file snapshot validation for existing writer
transactions and the database source. Owned backup/restore/import preserve all data above quota and after host
expiry, with exact membership checks and no portable paid facts. Import/Undo use their existing writer before
data/journal reads, recheck after SQL and roll back complete aggregate/detail/journal changes on failure.

28 new real SQLite cases/main 1,834 pass; App.Tests 299 unchanged. No model/schema, SDK, permission, string/layout
or backup format changes; current deployment stays inactive. Explicit selected read-only import/restore data,
profile creation, automation and remaining ENT-02/03 boundaries remain unfinished. Database replacement followed
by a failed migration still follows established safety-copy recovery; the whole multi-source portable package is
not made atomic here. Platform/APK and actual linked import/Undo evidence:
[recovery-write-policy.md](quality/recovery-write-policy.md).

## D-125 - Guard new categorization patterns and serialize replacements (2026-10-10)

The first real SQLite policy regression proves the unguarded rule service accepts new automation in an enabled
Free context. New pattern/kind uses CategorizationRules; retained same-text/kind category edits and new-id
same-pattern replacement preserve Corrections rights, including expired-host data with exact membership.
Deletion uses DeleteData. Personal Pro cannot replace membership. Preserve existing trim/case matching and
new-id replacement semantics; an existing-id update retains its stored CreatedAt.

Hold the actual-file SQLite writer before matching and replacement, even in current unrestricted builds, so
independent providers cannot both insert an absent pattern. Check before draft mutation, recheck after SQL,
rollback the whole replacement/deletion on failure and raise Changed only after commit. The rule form and entry
details share this service; no UI-only gate. Rules never reclassify existing entries.

28 added AT-127 cases/main 1,862 pass; App.Tests 299 unchanged. Registration stays inactive. No schema, SDK,
permission, visible layout/string or portable paid-fact changes. Suggestion delivery, selected read-only data,
profiles, automation and other ENT-02/03/04 paths remain unfinished. Strict platform/APK and actual Release rule
creation/replacement/deletion evidence: [category-rule-write-policy.md](quality/category-rule-write-policy.md).

## D-126 - Guard complete ledger Save and validate final refund batches (2026-10-10)

The actual Save service initially accepts a new category split in an enabled Free context. Classify under its
actual-file writer, before reading stored groups or validating/cleaning drafts. New splits/additional parts use
SplitTransactions; retained part correction/join uses Corrections. Include untouched siblings, distinguish
holding groups (new fees use ManageHoldings) and transfer fees. Ordinary new work uses Transactions; corrective
adjustment/refund/income reversal remains available after host expiry with exact membership.

Four reproduced amount defects require final-batch validation: two refunds individually fit but jointly exceed
the purchase; a purchase is reduced below retained refunds; its kind changes away from Expense; and Int64 addition
wraps. Validate against incoming originals/refunds plus untouched stored refunds, excluding edited/deleted old
rows. Recheck edited purchases; use exact widened minor-unit totals. Preserve valid batch orders, replacements
and Int64 boundaries. No money storage, currency rounding, schema, SDK, permission or visible control changes.

45 added AT-128 cases/main 1,907 pass; App.Tests 299 unchanged. Entries, occurrence paid totals and attachment
ownership roll back together after SQL failure or retired cached access; Changed follows commit. Inactive builds
keep unrestricted deployment. Ledger delete/Undo, read-only selections, automation and remaining ENT-02/03/04
work remain open. Strict platform/full signed APK/native evidence: [ledger-write-policy.md](quality/ledger-write-policy.md).

## D-127 - File-bound ledger deletion and retry-safe Undo (2026-10-10)

Three real SQLite regressions expose consumed refund links on failed Undo, phantom links after a failed deletion
and cross-profile relinking when copied profiles share ids. One actual application-source regression exposes a
failed Undo losing its offer. Replace session-global relationships with explicit refund links in the committed,
original-file deletion batch. Reject the batch in another profile; preserve it unchanged for retry. Restore missing
ids only, with refund relinking limited to newly restored purchases; repeated Undo is a no-op. Plain row enumerables
retain row-only recovery without guessed refund relationships.

DeleteData/Corrections are checked under the actual SQLite writer before reads, including unrestricted builds.
Entries, occurrence state and paid totals commit together after cached recheck; SQL/access failure rolls back
complete stored rows with no Changed. Receipts already remain in the database for Undo and are purged as orphans
at startup; preserve this policy without loading their bytes into an offer. A failing receipt fixture initially
assumed deletion removes the receipt; corrected evidence disproves that assumption, not a product defect.

UndoService keeps its original eight-second deadline after failure, blocks concurrent taps and clears only the
successful offer's version. Replaced/dismissed offers survive old completions correctly; observer failure cannot
leave the service in progress. 38 added AT-129 cases (29 Data, 9 actual app flow); main 1,945 and App.Tests 308 pass.
No schema, SDK, permission, visible caption/layout, portable entitlement or activation change. Strict platforms,
full signed APK and normal Release deletion/Undo evidence: [ledger-undo-write-policy.md](quality/ledger-undo-write-policy.md).
Selected read-only data, automation, native commercial feedback and remaining ENT-02/03/04 stay unfinished.

## D-128 - Report native Undo failure without losing its offer (2026-10-10)

Actual TransactionsViewModel.UndoDeleteAsync awaited Undo and Load without a failure guard. Extract that exact
bound command into TransactionUndoViewModel through the existing IAppInteraction port; a regression demonstrates
the exception escaping command dispatch. Non-fatal failures now reach the existing translated error dialog.
Serialize action, refresh and failure feedback together; explicit duplicate invocation during a dialog does nothing.
Refresh follows successful Undo only. Preserve the original UndoService offer/deadline after failure, without
renewing it or reviving an offer already consumed by a successful write whose refresh later fails. Fatal memory
failures propagate and all execution guards release.

Nine added AT-130 actual-command cases pass; main 1,954 / App.Tests 317. Real SQLite failure keeps all 24 tables
unchanged; retry restores financial metadata, refund links and retained receipts, with expected auditing normalized.
Signed installed Release dialogs are checked in en/fa/de, light/dark on the owned emulator with a fictitious SQLite
trigger; actual dismissal and navigation remain responsive. Restore exact original database bytes and original
English/theme choices. No production fault switch, schema, permission, SDK, caption/layout or activation change.
[Evidence](quality/transaction-undo-feedback.md). Later refund edits during an outstanding Undo and D-126
bottom-of-form SaveError remain separate follow-ups.

## D-129 - Immutable ledger Undo and stale financial relationship checks (2026-10-10)

Five real SQLite regressions show old Undo relinking a refund after amount/kind/account changes, relabeling minor
units after the now-empty account changes currency, and accepting caller mutation of the returned deleted rows.
The file-bound committed batch now owns independent row/tag copies and retains expected complete unlinked refund
metadata, creation identity and original source/destination account currencies. Indexes/enumeration return copies.
Under the same actual writer, before inserts or occurrence changes, reject a changed/missing/recreated retained
refund or changed partially restored sibling, and missing/changed-currency accounts. Reject the entire operation
without writing or Changed. Ignore only subsequent UpdatedAt auditing, with CreatedAt checked independently.
Account naming/archival does not block recovery; fully restored batches remain no-ops after later edits. Existing
plain row enumerables retain row-only recovery, without this committed-batch conflict provenance.

17 Data cases and one actual application-command case are added; main 1,972 / App.Tests 318 pass. Strict builds,
full signed Release, normal installed Delete/Undo preserving receipts/refund links and installed stale-refund
guard/error feedback are verified. The native stale fixture uses an exact after-delete row update, not a claimed
native edit completed during the eight-second offer. No schema, SDK, permission, portable data or activation change.
[Evidence](quality/ledger-undo-conflicts.md). D-126 visible ledger Save feedback and remaining ENT-02/03/04 stay open.

## D-130 - Visible transaction Save failures beside the fixed action (2026-10-10)

D-126's installed purchase correction correctly rejects an amount below retained refunds, but its SaveError is
at the bottom of the long Advanced scroll body. Move that existing label into the fixed growing footer before
the existing effect/Save action, retaining full wrapping text, native scaling and an explicit complete spoken name.
No Save continuation, draft, validation rule, receipt-unit guard or financial algorithm changes. Existing input
errors still appear at their fields and reveal the first affected input; a later Save clears stale general feedback.
The review uses actual native Save for ledger rejection/retry and exact sample SQLite failure, comparing every
persisted table column/receipt and the reviewed editor fields. See [evidence](quality/entry-save-feedback-visible.md).
No schema, SDK, permission, portable preference or commercial activation changes; remaining delivery stays open.

## D-131 - Explicit resource availability and retained ledger settlements (2026-10-11)

Implement the approved OD-03 downgrade model as a projection of original scoped resource identities. The six
selection kinds include account, goal, recurring plan, quick template, saved filter and device-local profile;
budget definitions and hosted membership keep separate policies. Never guess which resources the owner retains.
Preserve counted pauses and historical state, reject stale/oversized choices, and restore availability on unlimited
upgrades without rewriting stored entities. Explicit choice alone grants no feature, membership or host access.

Bind account choice to immutable exact-file commercial facts and evaluate new ledger money inside its existing
SQLite writer, including both transfer accounts. Retained edits, reconciliation, refunds, fees and Delete/Undo stay
available. Actual reviewed open overdue payments are classified from the stored rule/slice/state and ledger rather
than trusting draft markers; future, moved-future, unreviewed, skipped or completed occurrences remain new work.

Advance-bill Save now uses a specialized actual writer: reread the owned expense plan and advances/refund totals,
reject changed reviewed plan/advance metadata or net paid amount, then generate only the existing algorithm's
difference. The normal native command retains its form and generic translated failure on a rejected Save.
79 added cases; main 2,051 pass, App.Tests 318. Native/strict build/APK evidence:
[selected-account write policy](quality/selected-account-write-policy.md). No schema, SDK, permission, portable
preference, paid fact or commercial activation change. Selection UI/persistence, other bindings and automation remain open.
