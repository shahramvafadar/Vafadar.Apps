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

## D-132 - Retained manual closure of actual loan principal (2026-10-11)

The approved downgrade matrix retains closing an existing loan. Recognize that operation from the actual known
Loan/Lent balance and the complete submitted/final ledger under the existing SQLite writer, rather than rejecting
every new transfer or trusting a repayment flag. Multiple payments, explicit foreign amounts and actual source/
destination Fees expenses may close principal. A backdated or uncertain baseline, partial/overpayment, imported/
automatic marker or unrelated group remains new work. Changing old rows/adding adjustments/new borrowing cannot
manufacture closure. Widen exact totals before negating Int64.MinValue.

Cash funds may settle retained debt; new card debt, asset or other borrowed/lent funds still need their own right.
Explicit net-off may close both actual principals without inferred relationships. Keep original validation,
membership, writer rollback and Changed boundary; never archive automatically. 51 added cases; main 2,102 pass,
App.Tests 318. [Evidence](quality/retained-debt-closing.md). Current deployment stays inactive, with remaining
selection UI/persistence, resource bindings and occurrence automation unfinished.

## D-133 - Atomic explicit payment links and retained occurrence correction rights (2026-10-11)

The owner authorizes unattended necessary implementation, tests, builds, safe reviews and finished commits/pushes
under D-69, without waiting for routine permission. Preserve genuine new cost/identity/licence/product/release gates
and the D-114 prohibition on temporary policies. Record this standing authority in AGENTS.md section 65.

Seven baseline SQLite cases reproduce missing-membership occurrence writes and an entry link committed before a
failed state insert. Skip/Unskip/change, settlement repair and history-free deletion now bind the same actual-file
writer and cached correction/delete right. Check history under that writer. Complete rollback includes access
retirement after SQL, with Changed only after commit. Retained corrections need no selected account or paid host.

Link the reviewed existing payment and occurrence state in one transaction. Recheck the actual stored rule/slice,
entry kind/source/destination accounts, partial flag and existing link; never overwrite another occurrence's payment.
Preserve complete actual financial metadata and receipts, without posting a second entry or changing paid totals.
39 added cases; main 2,141 pass, App.Tests 318. Native/strict platforms/full signed APK: [evidence](quality/occurrence-correction-writer.md).
No schema, SDK, permission, portable paid fact, caption/layout or activation change. New settlement/partial/final
posting, generated-entry reopening, selected-plan generation and automation remain unfinished, as does existing
occurrence command failure feedback. This is partial ENT-02/03 engineering evidence, not release acceptance.

## D-134 - Atomic new occurrence money and actual remaining payment (2026-10-11)

Two negative actual SQLite cases reproduce a full payment committed before a failed state insert, and a final
partial payment left open by an old form balance. Reuse the established complete ledger validation and writer
pipeline for Settle/PayPart, rechecking actual stored rule/slice, kind/accounts/destination, existing entry identity,
state and linked partial money. Core computes the effective amount/date and remaining payment, including moved
or changed occurrences. Full/final state and actual paid total commit with money; failure/access retirement
rolls back every row before notifications. Preserve actual money/date, partial totals and unique settlement;
legacy RepairSettlements remains for older data. No new quota grants or inactive-source activation.
16 added SQLite cases/main 2,157 pass, App.Tests 318; [native/package evidence](quality/atomic-occurrence-payments.md).
No schema, permission, SDK, caption or layout change. Generated-entry reopening, selected-plan automation,
choice persistence/UI and occurrence-command exception feedback remain unfinished; ENT-02/03 stay in progress.

## D-135 - Atomic reopening of the exact reviewed settlement (2026-10-11)

Reopen exactly the reviewed settlement under the actual-file writer. Check stored state EntryId/status and
actual payment plan/date/partial markers; reject stale identities without deleting or releasing another payment.
Generated entries reuse complete ledger deletion, groups, refund links, receipt ownership, paid totals and the
file-bound Undo journal. Manual/imported money is kept; unlink it with the opened state atomically. No second
writer after committed Changed. Preserve earlier partial money and complete occurrence overrides; retain automatic
posting suppression through Undo. SQL/access failure rolls back all rows before store/plan notifications.
15 added SQLite cases/main 2,172 pass; App.Tests 318. Current commercial registration stays inactive.
No schema, migration, SDK, permission, backup field, caption or layout change. Selected-plan generation/automation,
choice persistence/UI, command exception feedback and existing external acceptance gates remain unfinished.
[Evidence](quality/atomic-occurrence-reopening.md).

## D-136 - Retain occurrence drafts after failed commands (2026-10-11)

Full/partial payment, explicit linking, Skip/Unskip, date/amount/note changes and reopening use actual
OccurrenceCommands with the existing PlanStore writers. One gate spans native consent, writer failure feedback
and successful display publication. Rejection/cancellation never reloads, navigates or replaces the draft; ledger
validation stays inline. Unexpected failure uses the native interaction port without raw user-facing exceptions.
Retire the writable reviewed snapshot before post-commit navigation/refresh and publish its replacement only after
all load reads finish. A refresh failure explicitly says the change was saved and requires closing/reopening the
screen before another change. Explicit Reopen/Cancel captions exist in all six languages; French/Italian no longer
present two cancellation captions. No commercial activation, schema, SDK, permission, backup or device-setting change.
29 added actual application-flow cases (AT-138), App.Tests 347. Main 2,201 pass with an independently owned,
ignored Data.Tests output configuration limiting collection concurrency to one. Explicit writer-race tests retain
their concurrent operations. Two normal parallel full-suite attempts failed in unrelated Data.Tests SQLite cleanup/
migration (locked file and disposed native handle). Preserve negative logs; D-137 later completes file-scoped pool cleanup isolation.
An unsupported argument ran zero tests and a help invocation failed; neither is acceptance. Output-only test
configuration was removed; no product workaround or test execution policy was committed.
Strict Windows/Android builds and complete signed Release APK verified. Native normal Release emulator checks cover
en/fa/de, light/dark, actual error dialogs, retained drafts and all 24 complete stored tables. Six additional normal
Release cases commit an actual partial payment, deliberately break only the owned fixture read, display the saved-
but-not-refreshed message and reject replay from enabled native actions. No Windows native
command, real phone, iOS, provider or publication acceptance claim. Selected-plan generation/automation, choice
persistence/UI, commercial activation and existing external gates remain open.
[Evidence](quality/occurrence-command-feedback.md).

## D-137 - Keep local database pools independent (2026-10-11)

Profile switching and inactive-profile deletion retire only the named file's known pools; restore clears the
exact captured destination connection before migrating that same context. Other SQLite files and active profile
sessions stay independent. Fixture cleanup uses the same production helper only for existing databases under its
unique owned directory, after disposing its providers. Default/factory/read-write/read-only strings are covered;
arbitrary connection-string variants remain their owner's exact-pool responsibility. No process-wide clearing,
collection serialization, schema, permission, backup format, commercial activation or visible layout change.
Nine added AT-139 cases use real TEMP-table session markers and actual profile/backup services. The original
MoveTo/restore behavior lost another database's marker; negative baseline retained. Main 2,210 / App.Tests 348
pass with the normal parallel runner and explicit writer-race tests unchanged (58.337 s; no output runner config).
Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified. Normal Release
English/light emulator profile creation/switch, first-run restore from an actual production-service package, return
and inactive-profile deletion succeed. All 24 restored tables are compared: financial rows and other metadata match the actual snapshot; only the existing
onboarding-completion Settings.UpdatedAt audit write differs. Original profile
files/sidecars, complete rows and English/original System theme are restored. Temporary helpers remain ignored.
Release correctly denied the initial inspection-only run-as inventory; the helper was corrected to inspect only an
unlaunched Debug package. One stale prompt-coordinate attempt cancelled before Save; later input used the actual
focused field. Transient hierarchy misses resumed the same created profile without replaying creation/restore.
The observed native document provider was checked before selecting the exact owned file. Negative helper/capture
logs are retained; unsuccessful attempts are not acceptance. One early nested fixture probe used a noncanonical mixed-separator connection string and failed cleanup; it was
corrected to the application's full-path convention before the successful full run. No Windows native, physical
phone, iOS, provider, 200-percent layout or publication acceptance claim. Planned entitlement bindings/automation,
choice persistence/UI and existing external gates remain open.
[Evidence](quality/local-sqlite-pools.md).

## D-138 - Recheck automatic payments inside the ledger writer (2026-10-11)

Automatic payment discovery is only a candidate list. Recheck the stored active rule/slice, actual open state,
linked partial/full money, current amount/due date and accounts inside the same SQLite writer as ledger Save.
Manual partial money, including a legacy incorrect paid cache, prevents another automatic full payment. Removed,
paused, skipped, moved-future or already settled candidates post nothing. Use current overrides/account routing;
transfers remain one entry and automatic money stays unreviewed. Exact cached membership, selected plan and
AdvancedPlans rights apply when enforcement is enabled; choices grant no paid rights. Current registration stays
inactive. Snapshot plan choices are immutable, scoped and in memory only; no selection persistence/UI, schema,
backup format, SDK, permission or visible layout change.
31 added AT-140 actual SQLite cases; main 2,241 / App.Tests 348 pass with normal parallel execution (67.124 s).
Strict Windows/Android builds: zero warnings/errors. Complete signed Release APK verified. Six normal Release
emulator foreground flows cover en/fa/de, light/dark, actual posting/override/transfer and partial/paused/disabled
blocking. All 24 unrelated complete tables match; exact original sample files/sidecars and English/System theme
are restored, then the original Main profile is reopened. The first native assertion inspected an inactive sample
while Main was open; its fixture runs through actual services successfully. Selecting the exact owned sample
through native UI resolves the review setup. Transient hierarchy misses resume the same fixture; a local helper
syntax error executes no native action and is corrected. Preserve these negative attempts; they are not acceptance.
The original source reproduces automatic full money after independent manual partial payment; the final writer
prevents it. One expanded test initially expected a result instead of the existing missing-membership exception;
the corrected test retains the rejection. No Windows native, physical-phone, iOS, provider, performance or release
acceptance claim. Other resource bindings, generation/reminders, choice UI/persistence and external gates remain open.
[Evidence](quality/automatic-occurrence-writer.md).

## D-139 - Selected future plan occurrences (2026-10-11)

Project actionable future occurrences through exact-file scoped plan choices while keeping complete original
schedules/states for history and every forecast/budget/KPI calculation. Preserve open effective past-due payments;
today/future new money checks actual rule/slice/kind/accounts and selected/paid recurrence rights inside the same
ledger writer. Dated ended slices inherit only their actual unique non-overlapping continuation. No invented choices
or stored pause/end changes. Preserve account rejection feedback. Registration remains inactive; no layout or new resource key,
schema, backup, permission or SDK change. Core reminder eligibility is available; actual reminder/snooze delivery,
choice persistence/UI, remaining selected bindings and external acceptance gates remain open.

Profile/onboarding root replacement also reuses D-82 navigation retirement before detaching old Shell handlers;
initial native hot/cold language failures are negative evidence. Final Main/QA03 switching and hot language changes
are checked in the normal signed Release, without a locale override.

41 added AT-141 cases (19 Core / 22 real SQLite); normal parallel main suite 2,282 / App.Tests 348 pass, no failures
or skips (59.741 s). Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified.
Six actual normal Release emulator flows cover en/fa/de and light/dark: Home due count 11, six plans in Due/Upcoming/All
and detail future dates/settled history. Current inactive build remains unrestricted above Free's five-plan allowance.
No financial Save during review; all 24 complete prepared tables match. Exact original sample files/sidecars and
English/System plus the original Main profile are restored. Physical-phone, iOS, Windows native, provider,
performance and publication acceptance remain open. Old generic full/partial entry bypass reproduces two failures.
Initial invalid enum/runner attempts and six account-feedback ordering failures remain negative evidence; final
implementation retains established rejection order and the complete canonical suite passes.
[Evidence](quality/selected-plan-occurrences.md).

## D-140 - Actual scoped Plan reminder delivery (2026-10-11)

D-140 binds actual Plan/contract notification delivery and snooze rebuilding to the opened file and current
explicit plan choice. Original group members, recurrence identities and effective dates survive snooze; current
privacy/language/text is rebuilt from stored rows. Unprovable legacy snooze caches are discarded while normal
reminders are rebuilt; legacy navigation remains readable. Notification taps resolve the current file after
unlocking, without profile guessing or money writes. Reserve the snooze id marker so a hashed occurrence id cannot
keep the original deadline. Registration remains inactive. Choice UI/persistence, other selected resources and
external acceptance remain open. [Evidence](quality/selected-plan-reminders.md).

D-140 native Persian review also finds English action captions after live language changes. Keep the existing
Android category instance instead of appending a replacement that the library never selects; initialize current
translated actions for background receiver startup without database reads. Final main/App counts are 2,330/380,
with 48 new AT-142 cases. Preserve the failed native caption/action/cache inspection evidence.

The own-emulator inexact wakeup also arrives outside the native library one-minute acceptance default and produces
no visible notification. Configure a bounded one-hour native acceptance window matching ordinary Android inexact
delivery; retain current permissions and record this missed delivery as negative evidence.

A further actual application regression reproduces a retained earlier queue when choice facts change before native
publication. Reuse one retirement guard before/after publication, cancelling old pending work without financial
writes. Final 48 added cases / main 2,330 / App.Tests 380 pass; preserve the failed one-case proof.

## D-141 - Durable scoped account and Plan choices (2026-10-11)

Persist explicit account and plan choices in one additive profile-local ResourceSelections table, keyed by
resource kind and the verified financial scope kind/id. A fresh revision rejects stale and ABA drafts. Reviewed Save
rechecks actual file, membership/rights, original identities/names/states, row revision and capacity under the same
SQLite writer; no-op saves preserve revision and publish no Changed. Never archive/end or erase financial data to
represent read-only availability. Stored choices override cached fallback selections, never verified capabilities.

New account/unarchive and Plan creation join an existing bounded choice atomically when capacity remains. Explicit
archive/ending releases only its own chosen identity; a validated split transfers only the actual selected
predecessor slot. Pauses still count. Current work/reminder snapshots capture the stored revision and retire pending
publication after a change; choices trigger the existing reminder refresh. Retained corrections remain possible even
with malformed choice input, which grants no bounded new work until explicit reviewed repair.

Commercial registration remains inactive: no guessed main-profile identity, fake paid facts, new limits or purchase
activation. Inactive/unlimited loading writes nothing and requires no bounded choice. Choices are financial-profile
preference data in the normal database backup, not security, entitlement or credential facts; restore does not retag
a foreign financial scope. The chooser UI, remaining resource bindings and external/release gates remain open.

44 new AT-143 cases (42 Data / 2 actual application flows); main 2,374 / App.Tests 382 pass in normal parallel execution.
See [verification and delivery evidence](quality/durable-resource-choices.md).

D-141 final strict Windows/Android builds have zero warnings/errors. Normal installed Release applies the additive
migration with all 23 complete preexisting non-migration tables unchanged, an empty choice table and six original
plans still available above Free capacity. Complete signed local-test APK/package verified; enabled chooser UI,
physical-device, iOS, provider and publication acceptance remain open. See the D-141 quality record.

## D-142 - Reviewed account and Plan chooser (2026-10-11)

Settings now opens a covered account/Plan availability form. One growing real target per complete original
identity keeps type/currency or entry kind, original state and unsaved choice separate. Paused plans remain paused
and count; archived/ended items remain visible but cannot occupy a slot. Counters and inline capacity feedback
follow current display digits. Save and reload reserve their own growing rows outside the scroll viewport. Rejected Save feedback stays
visible beside those actions, including when the original list is scrolled to its end.

Loading/failure hides the complete form; translated retry publishes only a complete current snapshot. Explicit
Save uses the D-141 actual-file/scoped writer and preserves the exact draft after storage, rights or stale-review
failure. Retire a committed review before navigating, so a failed return cannot submit again. Dirty reload, kind
switch and Back require one native discard decision with one shared busy guard. Live captions and help returns
retain the original unsaved identities/index; no autosave, financial posting or state rewriting.

Current commercial registration remains inactive: all eligible items are available, no subset or Save is required,
and the form explains this. There is no fake paid entitlement or new test-build limit. The snapshot review's
fictitious Free context belongs only to its separate artifact SQLite database/page; it never replaces release DI.
The account/Plan chooser is delivered; other resource choices/bindings, activation and owner/external gates stay open.

25 added AT-144 actual application-flow cases; main 2,399 / App.Tests 407 pass, zero failures/skips. Strict Windows/Android have zero warnings/errors. Complete form and registered Settings/Help/Back each pass the 18-case language/theme/width matrix. Six normal Release own-emulator cases and complete signed APK are verified. [Evidence](quality/active-item-chooser.md).

## D-143 - Durable goal choices and actual goal writer (2026-10-11)

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

Current release registration remains inactive and unrestricted. Settings still exposes Accounts/Plans. Goal
chooser UI, effective goal/contribution projection and native reminder delivery follow in subsequent ENT-02/03
slices. Other selected resources, activation and owner/provider/device/iOS/publication gates remain open.

31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](quality/goal-resource-choices.md).

## D-144 - Reviewed goal chooser (2026-10-11)

The existing Settings availability form now has Accounts, Plans and Savings goals as three growing native choices.
Read the original goal name, measurement kind and state from the actual D-143 scoped snapshot; money goals also show
their original currency, while holding-quantity goals do not display a currency as their unit. Completed goals say
Completed rather than the Plan-specific Ended. Paused goals count toward Free's one-goal allowance without resuming.
Completed/archived identities remain readable and unselectable. No goal, allocation, contribution or ledger write
occurs while selecting, navigating, refreshing captions or discarding a draft.

Explicit Save uses the reviewed actual-file writer, including empty choices and malformed-choice repair. Preserve
the exact goal draft/index/identity on stale name/kind/currency/rights or storage failure and on live language change.
Keep existing consent/busy guard, covered publication/retry, pinned Save/error and committed-review retirement.
Current unrestricted registration requires no subset or Save and grants no fake paid rights. Choosing an advanced
goal never grants its paid tools; translated explanation/example make this explicit in all six existing languages.

Goal chooser UI is delivered. Effective goal/contribution projection and native reminder filtering/routing remain
the next ENT-02/03 binding slice; other resources/operations, activation and owner/external gates remain open.

18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](quality/goal-chooser.md).

## D-145 - Actual selected goal work and scoped reminder delivery (2026-10-11)

Read complete original goals, saved contribution rules and allocations from the actual opened database, with
new-work eligibility kept separate from original financial progress. Selection never changes funding/protection,
resumes a paused goal or grants a paid tool. Free balance-goal activity, advanced goal work and contribution reminders
are independent rights. Missing, empty, malformed, foreign and historical choices keep their original data.

New paid contribution work checks the actual chosen goal under the same SQLite writer. A paired new goal can join
its slot before that check; reject failed activity before pin/protection normalization. Retained amount corrections,
reminder disabling/removal and negative earmark releases stay available. These operations never post ledger money.

Prepare goal reminders only for eligible original progress before the pending cap. Keep saved opt-in, generic/details
privacy, fixed 09:00 device-local rule dates, existing permission and the shared bounded queue. Native returning data
contains bounded original goal/date members and an opaque actual-file identity. Taps resolve that file after unlocking,
open the existing single detail or Goals root, and never switch profiles or grant new work. Retained history remains
readable after a choice change. Existing legacy historical links remain supported.

Validate actual file, cached rights, persisted choice revision and original goal/rule/allocation input stamp before
and after native publication; clear a retired queue, including failed snapshot preparation. The stamp is transient,
invariant and reflection-free, not a new schema, portable preference or credential. Current commercial registration
remains inactive and unrestricted. Goal-view contribution affordances and other remaining bindings follow separately.

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

[Evidence](quality/goal-work-reminders.md).

## D-146 - Original goal views and reviewed detail actions (2026-10-11)

Keep all original goals, saved contribution rules, financial progress, funding/protection, history and original
Home pins. Show availability for new work separately from lifecycle state and financial tone; only new contribution
suggestions require current work rights. Current commercial registration remains inactive and unrestricted.

Historical and unavailable earmarks retain negative releases on their original funding account, including an archived
account. Newly opened release-only forms start with Release and an account holding the original earmark; display/choice
reloads retain explicit direction/account/amount. New positive earmarks remain independently gated. Reopening checks
free chosen capacity and the complete retained paid contribution/reminder; paused goals consume capacity without resuming.

Every detail write validates the loaded actual file, cached rights, stored choice revision, complete original goal/rule/
allocation metadata and account/holding units/availability inside the same SQLite writer. Use the actual factory-bound
connection setting for stable file identity across auto-closed reads and open native SQLite writers. Do not depend on audit time
alone: concurrent note or Home pin edits with identical timestamps must survive. One busy guard spans consent, write,
feedback and publication. Rejected writes retain drafts/lifecycle; committed writes with failed reload disable further
writes and expose growing translated feedback/retry outside the body viewport. Retry never resubmits the action.

Keep dirty explicit price/amount drafts with their original numeric culture and currency. Currency changes preserve
the draft but require explicit reentry before Save. Retained assumed-price corrections preserve the original contribution
rule and never write a holding valuation or ledger entry. Native colors/query effects remain in actual MAUI partials;
tests compile the actual non-UI presenter, view models and Home projection, never replacement UI controls or algorithms.

36 new AT-148 actual-source/real SQLite cases pass; final normal parallel main 2,520 / App.Tests 497 pass,
zero failures/skips, test output cleaned. Strict Windows and Android builds have zero warnings/errors. Actual list,
detail and Home pass 18 English/Persian/German, light/dark, 360/412/wide cases with process-local 1.5 font stress;
18 additional focused cases check the corrected neutral earmark/release explanation. Native glyphs, original tones,
44-pixel action targets, original historical Release/account, dirty price/currency feedback and actual retry/Back are
checked. All 25 complete fixture tables remain equal; original development files/sidecars and display are restored.

Six normal signed Release owned-emulator cases verify original Home/list/detail, completed goal and archived funding,
initially selected Release, enabled Save and native Back. One explicit negative earmark release is saved; complete
original goals, accounts, ledger and other rows remain equal. Only the pre-existing D-141 migration and that one
expected owned allocation differ. Original owned bytes/sidecars plus Main/English/System are restored; the final
normal signed Release is installed/stopped. No physical-device, iOS, OS accessibility, provider, startup performance,
Store signing/publication or commercial-activation acceptance is implied.

[Evidence](quality/goal-work-views.md). ENT-02/03 remain in progress; the next ready slice is the reviewed goal editor/contribution draft.

## D-147 - Reviewed goal editor and explicit budget cuts (2026-10-11)

Keep the original goal identity/state, archived references, hidden contribution method, complete saved recurrence
rule/end/calendar/holiday choices, all unchanged spending-cut categories and the retained price assumption. Metadata-only
corrections never create a contribution plan for a legacy goal without one. A new draft saves its explicit named monthly
anchor. Show the actual rule/end/next dates and original method even in Simple mode; multiple saved categories remain
explicit rather than silently becoming the first category.

Construction performs no database read. Cover complete initial/retry publication; preserve explicit dirty fields and their
numeric parse culture/unit. Live captions never replace selections or reinterpret raw input. Changed currency/measurement
facts require actual reentry. Explicit reload merges only dirty fields into fresh original values, displays the current
saved target/rule/note and requires the review action when original inputs changed. It never silently accepts an overwrite.
Shell's empty native modal-attachment query retains an already opened identity; another explicit identity is rejected.

Separate original corrections from new paid contribution/reminder configuration. Paused selected paid configuration stays
paused and never prompts or delivers a reminder. Retained corrections/disabling remain available. Production commercial
registration is still inactive and unrestricted. No choice, display change, opening, permission or navigation posts money.

The paired writer rechecks the captured actual file/rights/choice and complete goal/rule/allocation/reference stamps before
mutation. Clone the complete original goal; never mutate it before a successful write. A successful save retires the draft
before returning, so navigation failure cannot resubmit it. One busy guard spans consent, permission, write and navigation.
An explicit holding/quote-reference change confirms clearing only the previous assumed price; cancellation keeps the draft.

Apply to budget remains a separate explicit confirmation. Its original monthly category limit, full budget metadata/all
limits, financial period/day and category wording are rechecked inside the actual SQLite writer. Change only the confirmed
category limit, never a goal, contribution, allocation, account or ledger entry. Concurrent equal-time edits and duplicate
commands are rejected; unrelated name/display changes do not reenable the same already applied cut.

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

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

Evidence: `docs/quality/goal-editor-work.md`.

## D-148 - Canonical selected budget definitions (2026-10-11)

Canonical scoped budget definition choice and separate current-work projection are implemented with
reviewed actual-file persistence. Save/Replace join or transfer chosen capacity atomically; Delete retains exact
membership checks and releases only the last current definition. Original history, paid-method corrections,
limits and financial rows remain intact. 34 new AT-150 cases; normal parallel main 2,594 / App.Tests 537 pass;
strict builds, normal Release native Save/Delete and six unchanged-editor language/theme checks, complete signed
APK and exact owned data/display restoration verified. Registration remains inactive. Budget chooser/view/alert
binding and other ENT-02/03 paths remain open. See [evidence](quality/budget-definition-choice.md).

## D-149 - Reviewed native budget definition chooser (2026-10-11)

The actual active-items form includes canonical budget definitions, grouped across copied periods with
original scope names/current methods and unselectable history. Covered review captures full account references;
explicit Save rechecks original metadata and writes only chosen definitions. Original account names stay exact
when display digits change. 13 new AT-151 cases; main 2,607 / App.Tests 550 pass; strict builds, 18 Windows native
chooser cases plus 18 registered Settings/help cases, six normal Release emulator cases, exact data/display restoration and complete signed
APK verified. Registration remains inactive. Effective budget/view/alert bindings and other ENT-02/03 paths
remain open. See [evidence](quality/budget-definition-chooser.md).

## D-150 - Actual selected budget alert work (2026-10-11)

Actual alert orchestration checks every eligible original current budget across calendars/currencies/scopes,
with separate chosen-work rights and complete original net spending/carry. Read-only actual-file financial/privacy
reviews guard native publication; failed/retired batches retract attempted alerts and retain retryable device levels.
Carry no longer mixes another account definition. 25 new AT-152 cases; main 2,632 / App.Tests 575, strict builds,
six hidden Windows budget renders, six normal Release Android delivery cases (five with individual taps), a final rebuilt SDK-acceptance case and complete signed APK pass;
owned data/display restored. Enforcement remains inactive. Budget view/editor/scoped returning-data and other
ENT-02/03 paths remain open. See [evidence](quality/budget-alert-work.md).

## D-151 - Original scoped budget notifications (2026-10-11)

Budget alerts now carry the opaque actual-file identity and original saved period id. Resolve after unlock and
again in the real budget page; reject another file, deleted identities and failed reads without guessing a
replacement. Retain original currency/calendar/period and monthly navigation coordinates without changing
Home currency or financial rows. Historical/unselected paid methods remain readable without granting new work.
14 new AT-153 cases; main 2,646 / App.Tests 589 pass. Strict Windows and complete strict Android Release pass;
six hidden Windows language/theme cases plus targeted native month navigation, and six normal signed Release
Android cases each tap four exact original currency/calendar/period destinations; owned data/display restored.
Commercial activation stays inactive. Budget view/editor commercial affordances and other ENT-02/03 paths remain
open. See [evidence](quality/budget-reminder-routing.md).

## D-152 - Reviewed original budget editor drafts (2026-10-11)

The original budget editor publishes complete references and defaults behind a loading cover. Exact original
id/file handoff retains archived/missing account/category limits and explicit scopes in both experience modes.
Raw amounts keep their input culture through live display changes. Save rechecks the complete review under the
actual SQLite writer; stale failures retain input and require explicit reload/review. Dirty fields alone survive
reload, unchanged fields adopt current saved values, and a committed form retires before native return.
Current new-tool rights are separate from retained corrections and future-period capacity. Registration remains
inactive. Budget view copy/delete/other work affordances and the remaining ENT-02/03 paths are still open.

35 new AT-154 actual application/isolated SQLite cases pass. Final ordinary main 2,681 / App.Tests 624 pass,
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass
without warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, native
validation/stale Save/Reload/Keep edits/Close and complete 25-table restoration. Six normal Release Android
language/theme cases verify original corrections/archived references; one explicit original-total Save preserves
all other metadata and ledger rows. Final normal Release Create/Close and a targeted weekly Create/Save verify
explicit absent identity and canonical zero stored month coordinates; complete owned data/bytes/display restored.
Physical device, iOS, paid activation and product publication are not claimed.

See [evidence](quality/budget-editor-drafts.md).

### D-153 - Reviewed original budget view actions

Original budget view actions now use the complete displayed BudgetEditorReview for copy, confirmed replacement
and deletion, rechecked before/after confirmation and under the existing actual-file writer. Period lookup uses
original calendar/currency/explicit weekly dates and rejects ambiguity. Viewing, original correction and deletion
remain separate from paid new tools and net current-definition capacity; future periods do not consume today's
slot. Busy spans native confirmation through refresh. A committed operation whose refresh fails requires a
read-only Retry, without repeating Copy/Delete. Read-only opening no longer creates Settings; financial history
and all original calculation inputs remain visible. Current commercial registration remains inactive.

26 new AT-155 actual application/isolated SQLite cases pass. Final ordinary main 2,707 / App.Tests 650 pass
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass with
no warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, full native action
names/targets, retained paid history/correction, changed-confirmation rejection and read-only Retry. Six normal
Release Android language/theme cases verify actual original copy/delete confirmations; exactly one owned copy
and deletion of that copy preserve all original 25-table data. Original owned bytes/sidecars/Main/English/System
are restored, final signed Release installed/stopped. ENT-02/03 and remaining resource/operation/import/restore/
native paths stay open. Physical-device, iOS, provider, paid activation and publication acceptance are not claimed.

See [evidence](quality/budget-view-actions.md).


### D-154 - Reviewed quick templates and saved queries

Durable explicit quick-template/saved-filter choices retain every original packet, recheck revision/value and
same-timestamp changes, and use the existing actual-file scoped writer. Chosen new slots and explicit filter
replacement are atomic; retained correction/delete/history stay separate. Actual template create/apply/delete
and saved-query save/delete commands use complete reviews and one busy dialog/write/refresh guard. Queries are
captured before prompts; committed refresh failure never resubmits. Template names wrap, publication is covered
and explicit Reload is available. Six growing chooser destinations and help explain retained query history.
Current Release registration stays inactive; no schema, network, SDK, permission or portable grant is added.

29 new AT-156 cases/main 2,736 behaviours (App.Tests 679) pass after repairing one stale four-kind expectation;
only that failed case reran. Strict Windows/complete Android Release have zero warnings/errors. Eighteen hidden
Windows locale/theme/width cases and six normal Release emulator cases restore complete owned rows/bytes/display.
Physical-device, iOS, OAuth, commercial activation and publication gates remain open. ENT-02/03 are still in progress.

See [evidence](quality/quick-resources.md).

## D-155 - Original Home template destinations

Home quick templates publish separate original activity availability after the complete parent read. Explicitly
available later definitions remain reachable within the existing six shortcuts; retained history and all original
financial calculations remain independent. A process-local TemplateVisit carries the reviewed actual-file/full
packet to the existing entry route. A changed/deleted original, reference, choice or right, or another actual file,
is rejected without guessing a newer same-id row or saving money. Failed navigation pins translated feedback and
requires explicit read-only Reload. The actual initial chooser receives only its typed resource kind, never a
selection. Destination feedback is visible beside Save; applying a template remains a detached draft.

Opening Home no longer creates or updates Settings for its layout fallback. Existing saved layouts remain exact;
the legacy device layout is read until an explicit profile-layout Save. Commercial registration stays inactive.
No schema, SDK, permission, network request, portable preference or entitlement grant is added.

21 new AT-157 actual-source/real SQLite cases pass. The ordinary main run passes 2,754 / App.Tests 697;
three focused name-order cases bring the verified total to 2,757 behaviours / App.Tests 700. The changed flow
class passes all 21 cases; the unchanged full suite is not repeated. Pending navigation awaits its actual event.
Outputs are cleaned. Strict Windows and complete strict Android Release have zero warnings/errors. Eighteen
hidden Windows Home/chooser contexts and eighteen focused destination continuations cover en/fa/de, light/dark,
360/412/wide. Six normal signed Release emulator cases open original Home templates, fill original amount/title
and cancel/discard without financial Save. Complete owned rows/bytes/display are restored. Physical-device, iOS,
OAuth, commercial activation and publication gates remain open; ENT-02/03 remain in progress.

See [evidence](quality/home-template-destinations.md).

## D-156 - Reviewed original device profile catalogue

Original device profile choices now use the existing empty main id and exact original profile ids/names.
The actual Profiles form separates open/rename/delete from an explicit saved activity subset and unsaved choice.
One device preference publication contains the complete catalogue and scoped choice, with separate revisions;
legacy keys remain untouched and every catalogue/startup/backup-set reader uses the new packet after first mutation.
Opening reads only. Complete file/current-profile/provider/catalogue reviews guard publication, both delete
confirmations and creation; authentication is rechecked before committing a switch, with previous-path rollback
when that review retires. Unselected originals remain openable for retained history and correction.

Production device registration remains inactive. Per-file financial/new-money/automation bindings to the chosen
profile remain unfinished; this slice does not complete ENT-02/03 or activate commercial limits. No EF model,
schema, migration, permission, SDK, network request, credential or portable grant is added.

33 new AT-158 actual-source/real isolated SQLite behaviours pass. The ordinary main run passes all 2,790
tests (App.Tests 733), with no failures/skips; outputs are cleaned. Strict Windows and canonical complete Android
Release builds have zero warnings/errors. Eighteen hidden Windows en/fa/de, light/dark, 360/412/wide contexts
check actual native targets, complete growing captions, explicit choice Save, stale Save, retained drafts,
Cancel/Discard/Reload and unchanged complete fixture data. All review processes exit normally and exact owned
development bytes/sidecars are restored. Normal signed Release emulator checks and APK details are recorded in
the evidence document. Physical-device, iOS, OAuth, paid activation and publication gates remain open.

See [evidence](quality/local-profile-work.md).

## D-157 - Actual-file original profile work

The original device profile choice now composes with the existing verified actual-file commercial source.
The legacy empty main id remains separate from the independently verified financial scope; changing the current
profile preference cannot transfer rights to another file. The immutable context retains the original plan,
resource choices and scope, plus complete catalogue/legacy/provider review facts. Original writer/snapshot
validation therefore rejects retired choices, including change-and-return revisions and changes while waiting
for SQLite ownership. Device choices grant neither paid tools nor shared-space membership.

New money, definitions, selected Plan/Goal/Budget work, template activity and future Plan/Goal reminders consult the same
profile condition. Complete original financial inputs, history, query viewing, corrections, refunds, overdue
settlements, deletion, backup/restore and owned export remain independent. A malformed/unlisted catalogue never
guesses main: new work stops while separately verified historical rights remain. Both current registrations
stay inactive; this slice does not activate limits or complete every operation/import/native binding.
No EF model, schema, migration, SDK, permission, network request, credential or portable grant is added.

31 new AT-159 actual-source/real isolated SQLite cases pass. The ordinary main suite passes all 2,821
behaviours (App.Tests 764), without failures/skips; test output is cleaned. Strict Windows and canonical strict
Android Release builds have zero warnings/errors. Focused original-page Windows and normal Release owned-emulator
results, exact data restoration, retained negative evidence and the complete signed APK are recorded in
[the evidence document](quality/profile-file-work.md). Commercial activation, physical-device, iOS, OAuth,
Store signing/publication and the remaining original-operation bindings remain open.

## D-158 - Original catalogue reminder refresh and guarded period reviews

Original catalogue changes now reach the actual reminder watcher, so explicit activity choices rebuild the
queue without another financial action. The shared event port introduces no construction-time database read.
Subscriptions are lifetime-scoped and idempotent; a different catalogue cannot be substituted. Delayed work uses
the injected clock and exposes its actual full batch task for owned-lifetime checks. Disposal cancels future work,
detaches subscriptions and asynchronously drains current work without deleting valid native reminders.

Period-review delivery uses the same captured actual-file paid/profile facts. Complete original preferences,
first-recorded account date, language/formatting/calendar/digits and local day validate before/after native
publication. Revalidation never creates Settings. SDK request refusal is consumed for scheduled requests as well
as immediate alerts; partial pending work is withdrawn and Snooze cache commits only after successful complete
publication. Original financial rows and recovery remain independent. Current registrations stay inactive.
No EF/model/schema/migration, permission, SDK, network, credential or portable format change.

25 new AT-160 actual-source/real SQLite cases pass. The final ordinary main suite passes 2,846 tests
(App.Tests 789), with zero failures/skips, and output is cleaned. Strict Windows and canonical complete strict
Android Release build have zero warnings/errors. Original native Profiles Save drives the real watcher in an
independent fixture; complete rows/catalogue and owned development bytes are restored. Final normal Release
SDK delivery, exact owned restoration, APK details and negative evidence are recorded in
[evidence](quality/profile-reminder-work.md). Commercial activation, physical-device, iOS, OAuth and publication
remain separate. Original Plan/Goal financial-input retirement and other operation/native bindings remain open.

## D-159 - Original reminder input retirement

Original Plan rules/states, Goal/contribution/allocation packets and raw resource choices now retire a
reminder batch when any captured value changes, including changes at unchanged audit times or choice revisions.
Complete original accounts and the date-bounded ledger/quantity inputs used by the existing GoalProgress
calculator are captured in one deferred SQLite reader. Financial data is never filtered by activity selection.
The same coherent reader validates all original packets, preferences and first-data date before/after native
publication; a changed or failed preparation withdraws pending work and preserves the retryable Snooze cache.
Only needed ledger/quantity inputs are read. Future movements remain outside today's calculation.

Snapshot self-mutation rejection belongs to the reminder boundary: existing Goal editors can still mutate their
reviewed draft before the established explicit writer. Full raw selection values distinguish absent/empty
choices. No schema/model/migration, permission, SDK, network, credential or portable preference is added.
Commercial registrations remain inactive and test builds stay unrestricted.

24 new AT-161 actual-source/real SQLite behaviours pass; 100 related reminder/Goal editor/view cases pass.
Final ordinary main: 2,870 passed, zero failures/skips, App.Tests 813, 2m 03.711s; output cleaned.
Strict Windows and canonical complete Android Release have zero warnings/errors. Original running Goals pages
in en/fa/de at 412 pixels/dark verify stale Plan/financial retirement, reached-goal suppression, fresh retry and
complete 25-table restoration. Unchanged layout matrices and successful suites are not repeated.
APK signature/structure and all nine changed production-source PDB checksums match.
Native Release details and retained negative evidence are in the evidence document. Physical device, iOS,
OAuth, paid activation and publication are separate; ENT-02/03 and the commercial release are not complete.

See [evidence](quality/reminder-input-work.md).

## D-160 - Structured write feedback and retained filter retry

Original structured write rejections now explain quota counts, required Plus/Pro/add-on access, exact-space
membership and original profile/resource selection in all six languages. The shared native reporter keeps
unrelated failures on their original path and never guesses purchase, renewal, provider or activation state.
Pre-write Goal/Budget/quick-resource feedback retains the form; committed-return/reload feedback remains
distinct and never falsely says that the accepted write was not saved. Counts use safe Int64 addition,
current formatting/digit choices and direction marks; stored identities and exception details are not shown.

Saving a new filter above capacity now reaches the established reviewed atomic writer for its structured
reason. A rejected operation retires its review; the next explicit menu invocation rereads it without clearing
the current query. Exact raw filter names return to the name prompt. Name drafts are process-local and bounded
by original file identity, independently of rights; another file never inherits them. No automatic retry or
financial save, schema/model/migration, SDK, permission, network, credential or portable preference is added.
Production financial/device registrations remain inactive; test builds stay unrestricted.

19 new AT-162 cases pass with actual sources/resources and real isolated SQLite. Final ordinary main passes
2,889 / App.Tests 832, no failures/skips, 2m 49.493s, followed by cleanup. A preceding main run passed 2,885
before the real second-invocation fix; only the changed final behavior justified another run.
Strict Windows and canonical complete Android Release have zero warnings/errors. Eighteen actual Windows
reason/Close/second-Invoke cases cover en/fa/de, light/dark, 360/412/wide, with unchanged complete 25-table rows
and exact owned development bytes restored. Fictitious menu/name effects are explicit ports; reason presentation
uses the actual native adapter and buttons. Android evidence, negative artifacts and full signed APK are in
the evidence document. Platform/provider/device/commercial activation and publication gates remain separate.

See [evidence](quality/commercial-feedback.md).

Original configured SQLite targets now stay stable across open/closed Android reporting aliases.
The shared Data helper extends the existing D-146 approach to resource reviews, writers, Plan/reminder
packets, loan/backup boundaries and their unchanged foreign-file guards. Four added real SQLite cases
verify open readers/writers, coherent financial validation and rejection of another actual file.

## D-161 - Explain readiness before a blocked Save

New Goal and Budget editors explain their captured capacity, missing feature right or required explicit
selection before Save is disabled. The reason comes from the existing reviewed policy, uses all six real
translations and preserves complete raw drafts. WorkRestriction is an explanation, never a grant or a
replacement for the atomic writer. Existing budget net-definition/correction rules remain authoritative;
malformed bounded Goal choices require explicit repair, while unlimited access remains unaffected.
Production registrations stay inactive and test builds remain unrestricted.

Nineteen AT-163 actual-source/real SQLite cases pass. The final ordinary suite passes 2,908 tests,
App.Tests 851, zero failures/skips, in 2m 05.449s, followed by successful cleanup. Strict Windows and the
canonical complete signed Android Release build have zero warnings/errors. Thirty-six actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, retaining complete drafts and all
25 tables; exact owned development bytes restore. Native Android regression and negative evidence are
reported separately in the evidence document. No schema/model/migration, permission, SDK, credential,
network or portable preference is added. Physical-device, iOS, provider and activation gates stay separate.

See [evidence](quality/pre-save-readiness.md).

## D-162 - Read-only plan comparison and actual access

More now opens Plans and access: a read-only comparison of the owner-approved Free/Plus/Pro model and
original current local access for this exact file. Comparing tiers changes only in-memory presentation;
counts and local tools come from existing Core QuotaPolicy/PlanPolicy. Current inactive test access never
becomes a fake Free/Plus purchase. Personal rights, exact shared membership, the host's right and original
profile choice remain distinct. All history, correction, backup/restore and owned export rules stay intact.

Only built local tools are compared. Unavailable Pro online services are an explicitly separate roadmap;
optional cloud backup belongs to every tier. Plus Lifetime is the same local Plus set, without invented
renewal. No price, purchase, trial, renewal, cancellation, restore-purchase or provider status is inferred.
Continue returns without changing a plan or buying anything; actual Free has the Continue with Free caption.
The existing explicit active-item form opens without saving. Production registrations stay inactive.

Seventeen AT-164 actual-source/real SQLite cases pass. Final ordinary main: 2,925 passed, App.Tests 868,
zero failures/skips, 2m 05.555s, followed by successful cleanup. Strict Windows and canonical full signed
Android Release have zero warnings/errors. Eighteen actual Windows contexts cover en/fa/de, light/dark,
360/412/wide, all three native tier choices, complete realized row captions/values and native help/return.
Actual Release Android More/chooser/comparison/help/Continue and exact owned restoration are verified.
All eight changed production-source PDB checksums match. No schema/model/migration, permission, SDK,
network, credential or portable paid data is added. Device, provider, billing and activation gates remain
separate. ENT-04 and the commercial release are not complete.

See [evidence](quality/product-plan-comparison.md).

## D-163 - Original editor write feedback and committed return

Account and Plan Save now explain actual structured quota/permission rejection in all six languages,
retain complete raw drafts and place complete feedback beside the pinned Save. Successful writes cannot
be resubmitted after a failed return or follow-up; inputs become read-only and Cancel becomes Close.
Account/default-preference boundaries stay distinct, including a default update that committed before
its subscriber failed. Original change-notification failures after commit remain visible through a typed
committed outcome, never a fake pre-write rejection. Exceptions are not silently swallowed.

Both editors use original native-effect ports and actual platform-independent application sources.
Native query interfaces stay on pages. Plan picker rows retain only their actual plain-text identity/name/
selection, without unused native color/icon allocation. Account and Schedule edit copies preserve all
original fields/identities; Schedule also clones its mutable rule. A refused historical split cannot mutate
the original in-memory state, stop boundary or automatic-post anchor before an explicit corrected retry.
Original validation, signs, calendars, currency locks, category selection, split rules and atomic writers
remain authoritative. No EF property/schema, migration, permission, SDK, credential or portable-format change.

21 AT-165 cases pass; final main passes 2,946 / App.Tests 889, zero failures/skips, 2m 07.788s, followed by
cleanup. Strict Windows and canonical complete signed Android Release have zero warnings/errors.
36 actual Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Actual Save invokes
original atomic rejection/accepted writers over independent fictitious SQLite; raw drafts, pinned captions,
real input/Save enablement and Close are checked. Explicit fictitious return/consent effects are identified;
all 25 original tables and exact owned development bytes restore. Actual normal Android new-form/query
regression and exact owned restoration are separate from typed fixture/physical-device acceptance.
Production registrations remain inactive. Initial pre-attempt readiness and other operation/activation
coverage remain open; this does not complete ENT-04 or the commercial release.

See [evidence](quality/editor-write-feedback.md).

## D-164 - Reviewed atomic account archive

Account archive is now one reviewed SQLite transaction: original archive state, affected active/paused
plans in either account direction, exact default cleanup and original persisted activity choices commit
or roll back together. The reader captures complete original account/plan values and the local day without
creating settings. The writer reacquires/rechecks complete inputs under the actual file's writer and rejects
retired rows, day, cached access, another file or a different original notification owner.

Original Account/Plan feature/capacity decisions are shared, not duplicated. Archiving ends related plans
with established PlanActions; unarchive consumes the original account slot but never resumes ended plans.
Another concurrently chosen default remains untouched. Both original stores publish only after commit;
publication failures retain the accepted result and cannot cause a repeat. The UI blocks concurrent actions,
requires explicit consent before discarding a dirty draft, never silently saves those financial inputs,
and freezes an accepted result with Close after follow-up/return failure.

17 AT-166 cases pass. Final main: 2,963 passed, App.Tests 906, zero failures/skips, 2m 10.406s, followed by
cleanup. Strict Windows and complete signed Android Release have zero warnings/errors. 36 actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced with original native Archive/Close,
real SQL rollback and actual accepted account/plan/default writes over independent fictitious rows.
Explicit fictitious consent/alert/return effects are identified. Actual normal Android consent-decline
and exact owned restoration remain separate from typed fixture/physical-device acceptance. All four
changed production-source PDB checksums match. No EF property/schema, migration, permission, SDK,
credential, network or portable-format change. Production registration stays inactive; initial readiness
and other commercial paths remain open, not release completion.

See [evidence](quality/account-archive.md).

## D-165 - Original Account/Plan readiness before Save

Account and Plan forms now explain original feature/capacity/selection decisions before an attempted
Save. The read-only review uses each original writer's actual configured file and cached access. Active
reads capture complete original rows and persisted choices in a deferred SQLite reader; inactive test
access performs no financial-row/choice read or paid activation. A missing persisted choice counts all
original eligible rows, matching the writer instead of a cached fallback subset. Original correction,
retained advanced-rule and historical net-slot behavior stays available.

The original advanced Plan classification is shared through Core PlanEditPolicy. Save and readiness build
the same detached candidate; readiness does not parse money, validate financial input, mutate original
state or grant a write. Explicit Refresh access keeps every raw amount/date/note/reminder/selection.
A failed or retired read disables Save with translated feedback; an actual writer rejection still retains
the draft and requires explicit fresh review. Atomic writers remain the final authority. No EF property,
schema/migration, permission, SDK/network, credential or portable-format change. Test limits stay inactive.

Fifteen AT-167 cases pass with actual application sources and isolated SQLite. The original 21 AT-165
cases also pass: known full quotas now disable Save before attempting it, so original writer-rejection
cases capture a free slot and then exercise a real later addition/access change. They still prove original
atomic rejection and an explicit corrected retry. Final main: 2,978 passed / App.Tests 921, zero failures
or skips, 2m 12.110s, followed by cleanup. Strict Windows has zero warnings/errors.

See [focused evidence](quality/editor-readiness.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) prove native disabled Save, complete reasons/refresh, retained input/read-refusal/retry
and all 25 unchanged original tables. Normal final signed Release Android verifies actual refresh and
Cancel with retained native fields, unchanged original rows, existing D-141 migration only and exact
owned byte/sidecar restoration. Final Release is installed/stopped. Canonical full Android build has
zero warnings/errors; all six changed production-source PDB checksums match. The installable APK is
artifacts/android/zanance-d165-release.apk (82589781 bytes); signature/structure evidence is recorded
in the focused quality document. No physical-phone, iOS, OAuth, billing or release acceptance is inferred.

## D-166 - Original import and Undo outcomes

Original ledger import, holdings import and durable Undo now retain the original accepted result across
post-commit change-subscriber failure. A typed completed notification carries the exact counts/batch or
Undo result; original atomic writers, metadata, overlap rechecks, rights and rollback stay unchanged.
The application retires the accepted reviewed payload before any history/failure follow-through and
removes an accepted Undo row before fallible reload. A retained old command cannot repeat that batch.
A complete history read publishes only after success, preserving other original rows on failure.

Structured pre-write rejection is shown beside the retained preview in all six languages. Successful
follow-through failures state the completed outcome instead of pretending it rolled back. Picker
cancellation keeps the existing file/preview/choices. One busy lifetime spans Undo consent/write and
export authentication; explicit fresh review retires failed original evidence without silently saving.
Picker, existing app-lock confirmation and UTF-8 BOM cache/share effects are native adapters. Actual
non-UI application sources are compiled and tested through explicit ports, never stand-in MAUI controls.
Pinned complete feedback/review/import actions reserve a growing row outside the viewport. Original
history identity and growing Undo action have separate rows. No new feature policy, paid activation,
schema/model/migration, SDK/permission/network, credentials or portable-format change.

32 AT-168 cases pass with actual application sources, real isolated SQLite and all six resource languages.
They cover original ledger/holdings permission refusal and retry, post-commit publication outcomes,
nonrepeatable accepted import/Undo, real history-read failure with other batch retention, picker/consent
cancellation and busy guards, explicit read-refusal/retry, security/export ports and neutral complete CSV,
real SQL-trigger whole-transaction rollback, plus explicit aggregate links and complete restored metadata.
Final ordinary main: 3,010 passed / App.Tests 953, zero failures/skips, 2m 42.936s, followed by cleanup.
Strict Windows and canonical complete Android Release have zero warnings/errors. All seven changed
production-source PDB checksums match. Device/platform/provider acceptance remains separate.

See [focused evidence](quality/import-outcomes.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) verify original native review/import/Undo, retained refusal and cancellation, complete
pinned reasons/results/targets, accepted subscriber-failure semantics and all 25 original tables restored.
Normal final Release Android verifies DocumentsUI picking, actual preview refresh, picker cancellation,
Import and consented Undo, unchanged original rows, existing D-141 migration only, exact owned bytes/
sidecars and Main/English/System restored. The new owned CSV is removed; final Release is installed/
stopped. APK: artifacts/android/zanance-d166-release.apk (82175915 bytes), signature/structure and all
seven production-source checksums verified. Physical-phone/iOS/OAuth/billing/activation stay separate.

## D-167 - Accepted restore and explicit completion

Record the actual all-source restore before fallible display/onboarding/lock/work/root follow-through.
Retire accepted input and expose explicit completion retry without a second replacement or safety copy.
Distinguish uncertain partial-source failure from pre-primary refusal; preserve old review on read failure.
The original source/security/portable-policy algorithms are unchanged. Opening the form does not write
its protection default; growing pinned feedback/actions and Back/tab guards retain the completion flow.

35 AT-169 cases; main 3,045 / App.Tests 988, zero failures/skips; strict Windows/Android zero warnings/errors;
36 Windows contexts and actual normal Release Android original-source restore/complete-row readback pass.
Complete owned fixtures are restored and final Release is installed/stopped. Signed installable APK:
artifacts/android/zanance-d167-release.apk. See [bounded evidence](quality/restore-outcomes.md).

Next: finite current ENT-02/03/04 entry-point inventory, implementing confirmed gaps only. Generic mapped
CSV is already approved Free/recovery; the previous suggested extra gate is withdrawn as unnecessary.
External encryption/provider/device/iOS/billing/release gates and owner-text dependencies remain open.

## D-168 - Finite current commercial entry-point inventory

The owner reiterated completion of remaining work without unnecessary repetition. Current-source inspection
reconciles historical partial-slice notes with delivered boundaries. Three confirmed operation groups remain:
forecast/saved snapshots, both original receipt recognition callers, and loan/report/PDF analysis.
The snapshot Save/Delete writer currently lacks CommercialWriteTransaction; original analytical/native
computation callers lack their approved feature bindings. Implement these groups once, in that order.

Delivered resource writers/choices/history/reminders/recovery are listed separately with actual source and
existing suites. Generic mapped CSV stays Free/recovery. Provider purchase facts, activation, encryption,
owner texts/markets/samples and external device/platform gates remain distinct dependencies.
This is documentation-only inspection, not a fresh runtime claim: D-167's 3,045 passing tests, strict builds,
native checks and signed APK remain the engineering baseline and are not needlessly repeated.
See [current inventory](enhancements/2026-10-commercial-release/07-current-entry-point-inventory.md).

## D-169 - Original forecast access and currency-safe snapshots

Bind approved BasicForecast/AdvancedForecast to coherent original computation and saved-data paths.
Retain Free financial-month results/history/deletion, exact raw drafts on refusal and explicit refresh.
All-currency Save is atomic and accepted drafts cannot resubmit after follow-through failure.
Reproduced EUR 12345 becoming 22221 from foreign USD 9876: new scopes are currency-specific and old
comparisons filter by saved currency without rewriting metadata. No financial conversion is guessed.

29 AT-170 cases and the ordinary 3,074-test suite pass (zero failures/skips); cleanup and strict
Windows/complete Android Release builds pass without warnings/errors. Thirty-six existing Windows
layout contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Three further native
two-currency contexts verify per-currency saves and immutable legacy comparison with complete original
25-table restoration. Normal Release Android name/Save/details/Delete consent was exercised once;
exact owned bytes/sidecars and Main/English/System were restored. Final updated Release is installed/stopped.
Commercial registration stays inactive. Phone, iOS, real OAuth, billing and release acceptance remain open.
See [bounded evidence](quality/forecast-work.md). Next: receipt recognition, then loan/report/PDF.

## D-170 - Shared original receipt recognition and typed destinations

Complete the confirmed receipt group without rebuilding D-64 total parsing or manual attachments.
ReceiptWorkStore/ReceiptReadingSession bind actual-file cached rights before native work and recheck
full original source/destination afterward. Both original callers use the shared flow; EntryEditor
revalidates typed handoff before applying. Preserve bounded recognition, compact pending copy, old
amount on missing evidence and ordinary explicit Save. UI mode is not payment authority.

30 AT-171 cases and the ordinary 3,104-test suite pass (App.Tests 1,047, no failures/skips,
2m 30.954s), followed by cleanup. Strict Windows and canonical complete signed Android Release have no
warnings/errors. Thirty-six Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced.
Normal Release Android exercises actual Home source choice, DocumentsUI, original digital-PDF reading,
typed new editor, retained attachment rereading, existing editor and explicit discard without financial
Save. Complete prepared rows and exact owned baseline bytes/sidecars are restored; final Release stopped.
Commercial registration remains inactive; phone/iOS/camera/OCR corpus/provider/Store acceptance is separate.
APK: artifacts/android/zanance-d170-release.apk. [Evidence](quality/receipt-work.md).
Next finite ready group: original loan/report/PDF computation; the commercial release remains incomplete.

## D-171 - Original loan analysis and confirmed installments

Complete the loan portion of the confirmed loan/report/PDF group at its actual calculations and writer.
Preserve Free balances/manual repayment/reconciliation/essential warnings; paid schedule/split access is
independent of UI mode. Original confirmation and transfer/interest calculation stay intact, but full
file/input/access/day checks now occur after native awaits and under the actual whole-batch ledger writer,
including after inserts. Typed accepted outcomes cannot be mistaken for rollback or replayed.

37 AT-172 cases and ordinary main 3,141 / App.Tests 1,084 pass, zero failures/skips, 2m 54.955s;
cleanup and strict Windows/complete Android Release builds succeed without warnings/errors. Thirty-six
Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, with actual native payment
consent and original ledger readback. Normal signed Release Android verifies original account/schedule,
native payment consent and exact principal/interest entries. Complete owned fixtures and baseline bytes/
sidecars/Main/English/System are restored; final Release installed/stopped. Commercial registration
stays inactive; physical phone/iOS/provider/billing/Store and full-release acceptance remain separate.
APK: artifacts/android/zanance-d171-release.apk. [Evidence](quality/loan-work.md).
Next distinct ready operation: original report packages and advanced PDF; full commercial release remains incomplete.

## D-172 - Original report projection and detached PDF

Finish the confirmed report/PDF operation with coherent actual-file financial inputs and independent
commercial capability. Retain Free totals, original period balances and essential data status; keep
unavailable selected packages and explicit scope. Build PDF through the same original calculators in
a detached instance, with native consent/source/display checks and original cache/share policy. Basic
balances must not be accidentally grouped behind wealth analysis. Contribution suggestions retain
the original writer, complete reviewed inputs and named calendar, without posting money.

28 AT-173 application/real SQLite cases; ordinary main 3,169 / App.Tests 1,112 pass with zero
failures/skips (2m 42.749s), followed by test-output cleanup. Final strict Windows and canonical complete
Android Release builds have zero warnings/errors. Thirty-six en/fa/de, light/dark, 360/412/wide and
Simple/Advanced Windows contexts exercise real package targets, original calculations, Free refusal
without changing selection, detached actual PDF and unchanged original inputs. Final targeted PDFs
also retain Free account opening/movement/closing tables and per-account currency scopes; native goal
suggestion acceptance is checked in en/fa/de with its named-calendar rule and no ledger posting.
Normal signed Release Android exercises original report/history, actual PDF cache/platform share,
share-sheet cancellation and complete retained choices. Owned tables, cache, baseline bytes/sidecars
and Main/English/System are restored; final Release installed/stopped. Commercial registration remains
inactive. Physical phone/iOS/OAuth/billing/Store/performance/full-product acceptance remain separate.
APK: artifacts/android/zanance-d172-release.apk. [Evidence](quality/report-work.md).
The confirmed D-168 computation groups are locally delivered; bounded ENT-02/03 review is next.
Commercial activation and the full owner/external release objective remain incomplete.

## D-173 - Bounded local commercial completion review

Close the finite ENT-02/03 review after the three confirmed computation groups. Map original local
writers, explicit choices, history/recovery and automation to current behaviour tests and prior native
evidence. Keep activation, verified purchases, real shared-server lifecycle and physical-device/iOS
acceptance separate; no guessed entitlement or temporary policy. Reuse the unchanged ordinary D-172
3,169-test run and full signed APK instead of repeating builds/native matrices for a source-only review.
No production code, schema, permissions or APK change. Next ready engineering is A11Y-01 Android
accessibility. [Evidence](quality/commercial-completion-review.md).

## D-174 - Native quick widget actions and retained app language

Observed original clickable text roles, a 40 dp logo target and stale English widget captions while
both app and persisted Android locale were Persian. Use actual native buttons, a 44 dp logo target
with unchanged image size, explicit translated RemoteViews captions/descriptions/direction and the
persisted per-app locale before startup defaults. Preserve original PendingIntents and no-money drafts.
Main 3,169 passes, strict builds, twelve native widget cases and actual three-language original intents,
cold Persian receiver, complete unchanged owned rows/restoration and full signed APK are recorded.
No TalkBack activation or spoken/platform/phone acceptance; A11Y-01 stays in progress.
[Evidence](quality/android-widget-accessibility.md).

## D-175 - Contextual original date input names

Observed identical generic names on original Plan start/end dates. Give DateField an optional translated
purpose for each native input/calendar action and bind all 26 existing usages, including dynamic debt
reference captions. Keep Date/calendar/parser/part order/placeholders/raw drafts and fallback unchanged.
Main 3,169; strict builds, 18 Windows name/partial/calendar contexts, six normal Release Plan cases and
three original debt cases verified. Restore original Windows data and owned Android fixture/display;
full signed D-175 APK. Spoken screen readers/platform acceptance remain open.
[Evidence](quality/date-context-accessibility.md).

## D-176 - Current bounded Release startup baseline

Measure three current process-cold/warm-cache starts without changing the app, data or system settings.
Original Home readiness upper bounds include native observation overhead; retain missing-root evidence.
Source confirms original synchronous migration/display/settings/root work before CreateWindow returns.
Next is protected asynchronous original preparation, preserving secure gate, links, onboarding and
foreground ordering. No performance fix, two-second target, phone acceptance or new APK is claimed.
[Evidence](quality/startup-current-baseline.md).

## D-177 - Retain correctly ordered native Home sections

Original Home appearance retains every correctly placed section and moves only a displaced original
container; XAML starts in the existing Core default order. Preserve saved visibility/order and every
original binding/child. Native review checks actual child events/handlers and stored data rather than
inferring speed from source. Startup async/appearance-gating candidates were rejected after controlled
Release observations; their changes were removed and original startup/security kept. Main 3,169,
strict builds and 18 Windows contexts verified; six normal Release Android Home/return cases and exact owned-data/display restoration verified.
[Evidence](quality/home-layout-reuse.md). QA-06/device/release acceptance remains open.

## D-178 - One original asset-event index per Home projection

Group the unchanged holdings snapshot once by original asset type, preserving event identity/order;
keep HoldingsLedger.Quantity as the sole date/direction/checked-overflow calculator. No new financial
or commercial policy and no persisted cache/schema. Ten behavior cases and main 3,179 pass; bounded
Core computation preserves exact results while reducing repeated input visits. Six Windows native
caption contexts pass. Six normal Release Android caption/return cases pass; original Main and fixture data/display restored.
[Evidence](quality/asset-event-index.md). Whole startup/device/release gates remain open.

## D-179 - Live original icon accessibility names

The actual retained native picker reports English Default icon after a transient switch to Persian.
Keep original icon keys/buttons/drawings and selection bindings; resolve each name in the current
Translator culture on selection and attached language changes. Subscribe once while Loaded, detach
on Unloaded and refresh on reattachment. No form rebuild, financial Save, schema, permission or SDK.
Native Windows/Release Android checks and complete signed APK verified locally; spoken/platform acceptance remains open.
[Evidence](quality/icon-language-accessibility.md).

## D-180 - Maintained official Community source direction and bounded compatibility

The owner selected current official SQLCipher Community source, no custom cryptography/unnecessary
fork, reviewed dependencies and a repeatable native build/package/update/security process. Source
4.19.0 and OpenSSL 3.5.9 LTS commits are verified and pinned; portable native Windows Perl restores
with official size/hash/version checks without installation/global changes. Independent Windows C
and Microsoft.Data.Sqlite.Core 10.0.12/provider.sqlcipher 3.0.5 checks pass: original money/date,
correct-key reopen and wrong/missing-key/tamper refusal. Generic e_sqlite3 naming fails the driver's
encryption check. Encrypted backup targets work; plaintext BackupDatabase fails and memory export
works. The initial MSYS host and parser/locale issues remain recorded. This is not final native
hardening/reproducibility/EF/mobile/recovery/migration acceptance. App, APK, main test count 3,179 and
existing plaintext databases are unchanged. SEC-01/pipeline remain in progress; no interim policy.
[Compatibility](../../../../docs/architecture/sqlcipher-community-build.md),
[ADR](../../../../docs/adr/0010-database-encryption.md).

## D-181 - Independent Windows Community native producer and original EF schema

Add reusable official-source/tool restoration, pinned MSVC/SDK Windows recipe, local candidate
packaging and an independent original-source EF compatibility harness. Assembly remains enabled;
OpenSSL is static with no module/config autoload, and the DLL retains original codec/init/shutdown
hooks, memory temp storage, foreign keys and PE hardening. No custom crypto, upstream patch,
plaintext fallback or app dependency switch. Eleven hostile input/archive checks pass. Actual
compiled model and 32 original migrations preserve Unicode/money/date/settings; correct-key reopen
works and missing/wrong keys and modified page refuse reads. A missing /LD link flag was corrected.
Initial incremental builds retained folder-dependent OpenSSL strings; changed recipes now require
fresh directories, preserving negative evidence. Clean comparison and final package observations
are recorded in the build guide. Main app/test count 3,179/D-179 APK are unchanged and plaintext.
Android/iOS, upstream suites, keys, recovery and crash migration remain open.
[Guide](../../../../docs/architecture/sqlcipher-community-build.md).

Fresh Windows DLL/package hashes match across two clean builds; both final original-schema
harness runs, native export/system-only dependency inventory and package CRC/native checks pass.
Upstream logs retain 749 warning occurrences each; warning/security review and upstream suites
remain open. These are local candidates, not warning-free production binaries or app activation.

## D-182 - Official Windows OpenSSL suite and bounded warning review

The matching official-source candidate executes stock nmake test under a non-elevated account,
without configuration changes or app data. Result PASS: 347 files, 3,818 reported tests, 777 seconds
for the test phase; 59 wholly skipped recipes and 114 skip markers remain explicit, not exercised
coverage. Native candidate SHA stays unchanged. Record narrowing/intrinsic/shift warning origins
without blanket suppression or claiming all warnings safe. Main 3,179/D-179 APK remain unchanged.
Independent Linux host/official r30 tool identity preparation is retained for Android work; no Android
binary or app integration is accepted. SQLCipher upstream suite, native mobile, keys/recovery/crash
migration remain open. [Validation](../../../../docs/architecture/sqlcipher-security-validation.md).

## D-183 - Canonical Android Community native candidates

Independent Linux recipe uses locked official NDK r30/Clang 21, original SQLCipher/OpenSSL and API
24 arm64/x64 targets. Correct archive restoration's observed global-autocrlf conversion with scoped
Git overrides; original Configure blobs match. Initial shell CRLF/preparation errors are retained,
not hidden or fixed with upstream patches. Modern Clang's API macro alias is explicitly undefined
before the requested API define; target remains 24 without warning suppression. Two clean native
builds match per ABI; ELF 16 KiB alignment, hardening, original exports/system closure pass, plus six
negative ELF cases per ABI and eleven input cases. Compiler output is warning-free; llvm-ar's new
archive notification remains. Actual x64 API36/4 KiB emulator probe passes key/tamper/money/date/FK
checks, deployed hash and exact owned cleanup; it never opens app storage. Local package generation
is deterministic for identical candidate input, with required notices and verified CRC/slices.
Main 3,179/D-179 app/APK remain unchanged/plaintext. Arm64 runtime, API24/16 KiB devices, actual
managed Android schema, SQLCipher upstream suite, iOS/keys/crash migration remain open.
[Guide](../../../../docs/architecture/sqlcipher-android-build.md).

## D-184 - Official SQLCipher suite and initial Native CI

Stock testfixture/sqlcipher.test uses original official SQLCipher and source-built static OpenSSL,
application codec/init/shutdown/temp/FK/metadata/FTS options plus upstream test hooks. Actual Linux
suite: 226 tests, zero errors, reported no allocations leaked. Maintained runner verifies actual
4.19.0 community/OpenSSL 3.5.9 identity and excludes system libcrypto/libssl dependencies. First
extra identity probe queried provider before a key existed and failed; corrected by a fictitious
fixture key, with failure retained. Eleven input checks pass after portable filesystem free-space
preflight change. Initial separate Linux Native workflow builds two Android candidates, compares
native hashes, tests malformed ELF and runs the stock suite, retaining only candidate/evidence
artifacts. YAML parses and its commands are locally checked; hosted execution, Windows/iOS matrix
and Android managed/device/runtime packaging remain unaccepted. Main 3,179/D-179 app/APK unchanged,
databases plaintext; SQLCipher suite is abbreviated upstream scope, not full SQLite/app security.
[Validation](../../../../docs/architecture/sqlcipher-security-validation.md).

## D-185 - Original managed schema on Release Android

Extract the existing original-source EF probe into one shared schema implementation; Windows CLI
and a new distinct pro.vafadar.zanance.nativecompatibility package call it. No main app reference,
storage or credential access. Complete signed Release APK retains trimming/AOT, both exact native
candidates, no default SQLite bundle or INTERNET permission. Initial probe hangs before options
publication because it omitted actual data registration's D-44 Mono compiled-model setting; source
and scoped stage evidence identify this. Use actual AddZananceData without resolving its keyless
factory; no generated-model patch or app change. Release now passes all 32 actual migrations and
compiled-model Unicode/identifiers/money/currency/date/settings/FK plus wrong/missing-key/tamper
checks on x64 API36/4 KiB emulator. Maintained runner and shared Windows path pass; builds zero
errors/warnings. APK signature/CRC/native byte checks pass; only the independent package removed.
Main 3,179/D-179 app/APK unchanged/plaintext. This is not whole-app key/recovery/crash, arm64/API24/
16 KiB/iOS or release acceptance. [Validation](../../../../docs/architecture/sqlcipher-security-validation.md).

## D-186 - Final memory-only portable snapshot primitives

Add infrastructure primitives for keyed-source official sqlcipher_export/serialize, preserving a
consistent read view and user_version/application_id, bounded output and caller connection. The
reader owns a bounded read-only native memory image, verifies integrity, normalizes WAL mode only
in its memory copy per SQLite and clears owned allocations on disposal. Restore uses official
export with bound key parameters into an exact newly reserved encrypted staging file; existing
generations/sidecars are not overwritten. Plain-memory BackupDatabase to cipher fails and remains
negative evidence; no plaintext-disk fallback. Six behavior tests and full 3,185 pass; strict main
Windows/Android builds zero errors/warnings. Actual shared Windows and Release managed Android
probe verify original schema/history/header/Unicode and encrypted staged restore. No UI or app
provider/backup activation; original live databases remain plaintext. Keys, authorization, full
staging verification, durability/commit/rollback/recovery and remaining platforms are still open.
[Guide](../../../../docs/architecture/sqlcipher-portable-snapshot.md).

## D-187 - Complete typed staging verification

Reusable verifier compares logical schema (including index/view/trigger SQL), header identifiers
and every table's sorted storage classes/typed rows without guessing a primary key. Duplicate
multiplicity, signed integers, real bits, exact text/BLOB bytes remain significant. Native direct
statement spans avoid an observed fatal GetBytes expression-column metadata lookup in the driver;
negative crash evidence retained. Relational/cipher/native integrity checked, errors contain no
financial values. Twelve behavior cases added; full 3,197 pass, strict main Windows/Android builds
zero errors/warnings. Original-schema encrypted staging proof passes on Windows and independent
Release Android. No pointer/provider/backup/key activation or durable crash-recovery acceptance.
Live databases remain plaintext. [Guide](../../../../docs/architecture/sqlcipher-staging-verification.md).

## D-188 - Explicit immutable generation checkpoints

Add the final protocol model for one profile and distinct original/candidate identities. Ordered
KeyPrepared/CandidateVerified/Selected/Completed transitions reject skips, repeats, identity drift
and terminal advancement. Original remains selected until selection; immutable prior states remain
unchanged. Eight cases and full 3,205 pass. Strict main Windows/Android build results are recorded
in AT-187. This model never certifies external effects or touches files/keys. No UI, schema, live
provider or backup change; live databases remain plaintext. Durable key/selector/cleanup/restart
coordinator and OD-04/OD-10 policy gates remain open.
[Guide](../../../../docs/architecture/sqlcipher-generation-checkpoints.md).

## D-189 - Resumable native-effect migration coordinator

Implement the final coordinator over an explicitly exclusive native session, with actual stored
checkpoint reads, identity/regression rejection, context/cancellation checks after awaits and
compare-and-commit followed by exact readback. Reopen candidate before selection and again before
cleanup after Selected restart. Missing key stops without plaintext fallback; Completed restart
only reopens. Fifteen fault/retirement/cancellation cases added; full 3,220 pass, no failures/skips.
Strict main build results recorded in AT-188. No key/journal adapter, schema/UI/provider/backup
activation. Faultable ports are not real power-loss/restart proof. Native durable adapters and
OD-04/OD-10 remain prerequisites; main databases are still plaintext.
[Guide](../../../../docs/architecture/sqlcipher-generation-checkpoints.md).

## D-190 - Native CI Linux bind ownership failure

Read failed hosted job 115042317526 and artifact 11777219319 from run 38318994276. Reviewed host
versions already matched, then build-android.sh failed writing /output/host-proof.json: Permission
denied. Docker's fixed UID 10001 did not own runner bind output; Git tag warnings were not the
terminating error. Shared mount arguments now use Linux host non-root UID/GID, fresh owned work
binds at original recipe paths, read-only input/recipe and disabled network. Apply to both Android
producer and upstream suite; print last failed build lines in job output. Six actual Docker Linux
permission/mount/ownership-boundary cases pass; four PowerShell files parse and workflow YAML
validates. Add the focused workspace regression before native builds in CI. No source/version,
crypto/compiler flags, app code/UI/provider or APK changes. Previous 3,220 main tests/strict builds
remain the unchanged baseline, not rerun. Full corrected hosted native run remains pending.

## D-191 - Native CI private parent traversal

Job 115162355717/run 38357079252 now fails in the added workspace regression: /home/nativebuild/work
cannot be reached by matched runner UID. Actual image stat shows /home/nativebuild mode 750 owned
by 10001; the work mount alone cannot bypass its parent. Add a foreign UID 1001 actual-mount check,
which reproduces the failure locally on the old image. Set only the immutable build home to 0755
in Dockerfile; foreign UID still cannot write the parent. Rebuilt image passes all seven focused
workspace checks and unchanged exact host/NDK/compiler review. No root compilation, world-write,
source/crypto flag, app/UI/APK changes or unrelated retesting. Hosted workflow [38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) completed successfully on 2026-10-13 for
135d99386ef035fd63eedef3d735d6263a0ae387. Both clean ABI builds match original reviewed hashes;
all twelve negative ELF cases and 226 stock SQLCipher tests pass (zero errors, reported no leaks).
Artifact 11795943134 digest/actual library bytes independently verified. This closes the Linux
hosted native build/suite gate only; app encryption, other platforms and key/recovery gates remain.

## D-192 - Approved final recovery policy and Windows DEK protection

Owner approves OD-04 automatic device-protected key plus optional recovery key/Zanance backups,
and OD-10 permanent Android/iOS sensitive-data OS backup/transfer exclusion with completed
integration. Portable backup passwords stay optional; declined interim policy stays declined.
Do not ask these decisions again. Native adapters/durable migration/recovery UX remain work.

Add immutable profile/installation/generation context and Windows CurrentUser DPAPI protector
using the already pinned official ProtectedData 10.0.12 package. Only 32-byte DEKs and bounded
opaque envelopes accepted; wrong profile/installation/generation, tampering, missing/oversized
blob and wrong decrypted length refused. Owned temporary key buffers cleared; callers clear
successful returned keys. CurrentUser is not a hardware/device-only or same-user-code guarantee.
Twelve behavior cases added; focused 69 and full 3,232 pass. Actual source-built SQLCipher/key
creation then encrypted read in a second independent Windows process passes with native identity
and manifest hash checks. Strict main build evidence in AT-191. No UI/schema/live provider/key
storage/OS exclusion activated. Android/iOS adapters, final persistence/recovery/crash integration
remain. [Guide](../../../../docs/architecture/sqlcipher-device-keys.md).

## D-193 - Android Keystore DEK protection

Implement the original platform adapter using application-scoped AndroidKeyStore AES-256/GCM,
non-exportable wrapping key, native randomized nonce and D-192 authenticated binding. The fixed
versioned envelope contains only nonce/ciphertext/tag. Explicit preparation is separate from unwrap;
missing keys never trigger replacement. Automatic native access has no per-use authentication,
consistent with approved device-key policy; app PIN remains a separate gate. Native work runs off
UI under the future captured migration/access gate. Owned DEK copies are cleared.

The independent signed Release AOT/trimmed package compiles this exact source. Three stopped/new
processes create, reopen encrypted signed-money data, and exercise fifteen behaviors (fourteen
refusals and nonce randomization). Missing alias, wrong identities, tampering and malformed lengths
refuse. OS reports software security level 0 on this API36 x64/4 KiB emulator; not physical hardware
acceptance. Both native payload hashes match reviewed candidates; APK ZIP integrity and v2/v3
signatures pass. Only the independent package is removed afterwards. Initial ambiguous CipherMode
compile error was corrected using the explicit Java namespace; early report observation preceded
completion and was read after completion without restarting. Strict main build evidence: AT-192.

No live key creation/provider/backup policy or migration activation. Durable envelope storage,
optional recovery, crash coordinator, iOS and actual device gates remain. Main data stays plaintext.
Unchanged 3,232 managed-test baseline retained; unrelated suites/UI/APKs were not repeated.
[Guide](../../../../docs/architecture/sqlcipher-device-keys.md).

## D-194 - Immutable Android protected-envelope storage

Add actual private-files adapter for already authenticated D-193 envelopes. Caller supplies an
existing trusted installation-owned directory and exclusive access/profile lease; no portable path
or automatic folder/key creation. Create the final file exclusively, set 0600, flush ciphertext and fsync its directory. Existing
generations are never replaced. A torn write remains and refuses; no automatic repair/new DEK. Native no-follow/nonblocking reads reject
nonregular, missing, truncated, oversized and unauthenticated envelopes without key regeneration.
No format/schema change. This is storage plumbing, not selected live database or recovery UI.

Separate signed Release AOT/trimmed processes create and read original stored key/encrypted packet.
Eight storage cases check duplicate/no replacement, corruption, short/long/missing/nonregular file,
symlink refusal and an explicitly staged fictitious orphan ignored/preserved. That orphan case is
not an actual kill during publication; full crash/power-loss fault matrix remains open. Actual
OS level 0 software emulator scope remains. Initial native constant/namespace binding compile
failures retained and corrected against installed SDK metadata. The first actual Release run
refused native hard-link publication with EACCES; exclusive final creation replaces that approach.
This is not atomic whole-file publication; partial writes are fail-closed protocol work. AT-193 records final results.
No unrelated native crypto suites, managed baseline/UI matrices or main phone APK repeated.
Main remains plaintext; selector/journal, all-profile migration, Windows/iOS file adapters,
optional recovery and approved final OS exclusion still require final integration.

## D-195 - Transactional generation selection journal

Implement actual versioned SQLite protocol storage for the D-189 migration session. Prefer tested
SQLite/VFS atomic commit to unsupported Android hard links or assumed managed rename durability.
The dedicated private file contains only one bound profile/original/candidate GUID tuple and its
ordered phase, no financial data, DEK, path or portable preference. Format 1/application_id VZGJ,
strict original schema and singleton checks are independent from the unchanged EF finance model;
no EF entity/schema or compiled-model change. Explicit CreateNew never replaces an existing file.
Missing/interrupted/foreign/damaged state fails closed. Validate format/context before changing
existing journal mode. DELETE/EXTRA settings are read back. Immediate transaction compares actual
state before committing exactly the next phase. Caller still holds the exclusive access/profile
lease; records cannot certify key/snapshot/cleanup effects or authorize a plaintext fallback.

Thirteen new behavior cases pass; focused Data.Tests 82. Original code compiled against reviewed
SQLCipher on Windows and signed Release AOT/trimmed Android. Each five-process sequence flushes
actual native dirty pages and verifies a hot rollback journal header before external termination;
next process recovers CandidateVerified. A second terminated writer commits Selected through the
original store; next process retains Selected. These are actual process kills, not faultable-port
simulations or hardware power-loss/full-migration acceptance. Initial Windows observation failed
because File.ReadAllBytes refused SQLite's open writer handle; corrected bounded read permits
existing write/delete sharing, and the failure is retained. Native candidates are unchanged.
AT-194 records final test/build and independent package verification. Main provider stays plaintext;
actual session/key/candidate/cleanup integration, optional recovery, all-profile activation, iOS
and final OS exclusion remain. No unchanged upstream crypto suite or UI matrix repeated.

D-195 final managed evidence: all 3,245 tests pass with zero failures/skips; test output cleaned.
Main strict Windows/Android builds pass with zero warnings/errors. Records under
artifacts/native-sqlcipher: d195-{journal-tests,full-tests,test-clean,main-windows,main-android}.log.
Independent native strict builds also pass; no visible main screen changes or phone-test handoff.

## D-196 - Actual captured native migration session

Connect D-189 coordinator to a concrete SqlCipherGenerationMigrationSession and D-195 selector,
original D-193/194 Android protected key storage and native file/directory fsync/StatFs adapter.
Capture explicit private original ownership, bound installation/generation and exclusive access
validation callback. Derive exact encrypted candidate/rollback names from the captured generation.
Preflight bounded source/WAL size and room for candidate/rollback/journal growth. Export coherent
plaintext only in memory, clear owned buffers, preserve complete schema/headers/typed rows and
verify both encrypted outputs. Reuse authenticated key after interrupted unselected export; only
owned unselected output/sidecars can be recreated. Synchronize outputs before readiness. Recheck
source content before selection, retain encrypted rollback, reopen actual selection/key/rollback
before exact original/sidecar cleanup and synchronize directory. Never choose plaintext or create
a replacement key after selection. Context/cancellation guards remain around native awaits.

Initial actual Android run failed exporting writable memory from a ReadOnly original connection.
Correct to ReadWrite (never Create) under exclusive lease; exporter still reads main in its coherent
transaction. Original bytes and prepared envelope remain unchanged in the successful interrupted
run. No invented cipher/fork/provider fallback: reviewed SQLCipher identity is required. Native
password API strings cannot be fully erased; owned returned key/serialization buffers are cleared.

Ten guard cases added (focused 92), including retired/cancelled lease, invalid cleared key, low disk,
missing selected key, no plaintext selected candidate/default SQLite provider fallback and no early
cleanup. These port guards are not native conversion evidence. Separate signed Release AOT/trimmed
Android uses actual original registration/compiled model, first schema through all 32 migrations,
financial packets, Unicode, header IDs and a 2 MiB typed BLOB/Int64.MinValue fixture. Three distinct
fictitious profiles/process sequences prove actual export interruption/resume with unchanged source
and key, actual Selected interruption/resume through exact cleanup, and missing Selected envelope
refusal without replacement/fallback/cleanup. Candidate and retained rollback lack plaintext header
and fixture marker; unrelated file survives. APK signature/ZIP/native bytes and package removal
verified. AT-195 records final suite/build evidence. Main profiles/provider remain plaintext/inactive;
all-profile catalogue/startup/backup integration, optional recovery, Windows/iOS effects and wider
crash/disk/device/OS-exclusion acceptance remain.

D-196 final managed results: all 3,255 tests pass with zero failures/skips and output cleaned.
Main strict Windows/Android builds have zero errors/warnings. Evidence under artifacts/native-sqlcipher:
d196-{session-tests,full-tests,test-clean,main-windows,main-android}.log. Independent signed Release
native build/three actual migration scenarios pass; unrelated upstream/Windows key/UI suites not rerun.

## D-197 - Atomic protected EF factory publication

Extend actual LocalDatabaseLocation/factory with immutable atomic path/connection-creator/version
publications. A verified encrypted publication latches protected-only movement; no unkeyed fallback.
Same-path re-publication retires old contexts. Key-bearing options are never put in the path cache:
create one owned closed unpooled ReadWrite exact-path keyed connection per protected context, with
before/after creator validation. Async factory captures publication before scheduling native key
work off UI. Connection interception rejects missing/redirected/plaintext file and wrong native
cipher before EF use; command interception rejects retired already-open contexts. Caller still
owns captured key/access lifetime and global exclusive lease; direct native SQL bypass is not
claimed protected by EF interception. Native password strings are not completely erasable.

Legacy online BackupDatabase helper is permanently plaintext-only; keyed contexts refuse before
stream consumption/temp creation. This final format constraint is not a temporary OS-backup policy.
Generation-aware portable export/restore and final main activation remain required. No financial
model/schema/ledger or visible UI change. Ten new real-factory guard cases pass (focused 102).
Independent signed Release AOT/trimmed Android migrates one real-model/32-migration fictitious profile,
then uses actual AddZananceData/IDbContextFactory for keyed reads, an audited metadata write and
fresh readback. Already-open context query after same-path publication and unkeyed move refuse;
actual legacy backup emits no data. No unchanged three-scenario migration/upstream/Windows-key/UI
matrix repeated. APK signature/ZIP/reviewed ABI bytes and exact package removal pass. AT-196 records
final suite/build evidence. Main application remains plaintext pending lifecycle/catalogue/provider/
backup/recovery/all-profile adoption and remaining platform/runtime gates.

D-197 final results: all 3,265 tests pass with zero failures/skips; output cleaned. Main strict
Windows/Android builds have zero errors/warnings. Evidence: artifacts/native-sqlcipher/d197-
{factory-tests,full-tests,test-clean,main-windows,main-android}.log and native factory proof above.
No unchanged migration/crypto/Windows-key/UI matrices or main phone APK were repeated.

## D-198 - Protected portable backup writer

Connect original SqliteDatabaseBackupSource/Zanance commercial wrapper to memory-only SQLCipher
portable export for keyed factory contexts. No plaintext temporary path/file is created or cleaned
on this branch. Plaintext current profiles retain their unchanged online-copy path; keyed restore
remains fail-closed before input pending generation-aware receiver. Portable package password
remains optional. Supersedes D-197's keyed-write refusal only; legacy keyed restore refusal remains.

Actual factory native connection receives a weak captured-publication validator, no key material
in the registry. Export verifies it and cached commercial access before/after native phases and
every output chunk, including after destination awaits. Retired publication stops output without
rebinding. Native serialization is bounded, owned buffers cleared; no promise of complete native
string/memory erasure. Partial output belongs to caller and must not become a successful package.

Independent signed Release Android actual DI factory backup is opened in memory and fully compared
with live schema/header/typed rows, including real model/32-migration financial data and 2 MiB BLOB.
Retire same-path publication after first destination write: export refuses and emits at most 81,920
bytes. Initial test failed because ArrayPool can return a 131,072-byte bucket; output size now uses
fixed 81,920 rather than rented capacity. Failure retained; changed related native path passes.
Plain-source protected export and keyed legacy restore guard tests pass; focused 102. APK ZIP/v2/v3
signature/native hashes and package uninstall checked. AT-197 records full/build evidence. Main
provider/profiles remain plaintext pending catalogue/startup/provider/restore/recovery/all-profile
activation; current user backups are unchanged, with no temporary OS exclusion.

D-198 final managed evidence: all 3,265 tests pass with zero failures/skips and output cleaned.
Main strict Windows/Android builds have zero warnings/errors. Logs under artifacts/native-sqlcipher:
d198-{backup-tests,full-tests,test-clean,main-windows,main-android}.log. Changed native backup route
passes; unchanged migration/crypto/Windows-key/UI matrices and main phone APK were not repeated.

## D-199 - Encrypted restored candidate and actual schema upgrade

Add SqlCipherRestoreStaging for a captured exclusive owner: bounded portable memory validation,
explicit fresh candidate/sidecar refusal before input, local key preparation/reopen, encrypted
export and complete source/candidate schema/header/typed verification. Invoke actual application
migration callback only after imported content matches, then reopen for native/cipher/foreign-key
integrity and file/directory synchronization. Returned key cleared. Access/cancellation rechecked
around native awaits; no original/live pointer/data deletion or plaintext temporary staging.
Failed/unselected candidate remains caller-owned for journal reconciliation, not guessed cleanup.
Input bound does not promise total process peak RAM or complete native string erasure.

Six new guard cases pass (focused 108): existing output/sidecar, invalid/oversized input, retired
access and cancellation refuse before key/output changes. Separate signed Release AOT/trimmed
Android original registration/compiled model creates a real first-schema backup with fictitious
account/entry/settings. Portable image imports under a new profile/installation/generation key,
actual application migrations advance through all 32, and financial IDs/name/currency/opening amount,
entry amount/date and onboarding remain correct. Original backup file hash unchanged; encrypted
candidate has no plain header/marker. No live selection performed. Initial fixture span/await
compile error corrected before execution; no runtime workaround or native source change. APK ZIP/
v2/v3 signature/ABI hashes and package removal checked. AT-198 records final tests/builds. Main
still plaintext: receiver dispatch/durable selection/safety rollback/package atomicity, provider/
startup/catalogue/all-profile/recovery/OS policy and broader platform/runtime gates remain.

D-199 final managed evidence: all 3,271 tests pass with zero failures/skips; output cleaned.
Main strict Windows/Android builds have zero errors/warnings. Logs under artifacts/native-sqlcipher:
d199-{restore-tests,full-tests,test-clean,main-windows,main-android}.log. Changed native old-schema
restore route passes; unchanged migration/export/crypto/UI matrices and main phone APK not repeated.

## D-200 - Restored generation selection with encrypted rollback

Add actual SqlCipherRestoreSelection for already verified restored staging under captured exclusive
profile/access ownership. Bind profile/new generation, read existing key only, recheck cipher/native
integrity, snapshot current registered protected connection in memory, create/fully compare/sync
independent encrypted pre-restore rollback and sync candidate before ordered journal selection.
Compare-and-commit/read back every step, keyed reopen after Selected. Restart at Selected checks
candidate/rollback with existing key without requiring old connection, generating keys, falling back
to plaintext or deleting original. Retain Selected/original/rollback for outer package transaction.
No claim of preferences/attachments/package rollback or actual main publication/cleanup completion.

Initial native open-identity guard failed because configured path and native filename alias differed;
use captured ConnectionString identity (same approach as original actual-file commercial access).
Actual selected marker later appeared after observer timeout; process remained live. Did not recreate
or rerun staging/selection. Terminate only at that actual marker, then resume same dataset. Resume
process initially hung at Mono compiled model without original data registration (D-44). Diagnose
live no-result/no-error state, stop only that fixture, register actual AddZananceData before model,
rebuild and rerun only selected resume. Final same-run native result verifies new and old financial
packets (different amounts/names), encrypted rollback/original retained and no old connection needed.
Observation expiry is preserved as negative evidence, not called terminal or successful execution.

Six guard cases/focused 114 pass: foreign binding, missing journal/key, retired/cancelled access and
invalid cleared key never advance selector or prepare replacement. Signed independent Release
actual-model/32-migration Android selection/real process termination/same-run resume proof passes.
APK v2/v3/ZIP/reviewed ABI hashes and exact package removal checked. AT-199 records full/build evidence.
Main profiles remain plaintext; dispatch/catalogue/provider/startup/all-profile/recovery/OS policy,
package-source transaction, safety retention/pruning and broader platform/crash acceptance remain.

D-200 final managed evidence: all 3,277 tests pass, zero failures/skips; output cleaned. Main
strict Windows/Android builds zero warnings/errors. Logs under artifacts/native-sqlcipher:
d200-{selection-tests,full-tests,test-clean,main-windows,main-android}.log. Native same-run selected
resume proof passes after diagnosed fixes; existing candidate/selection was not re-created.

## D-201 — Validated whole-package restore ownership

**Decision.** Add an optional whole-package receiver to the existing BackupService rather than
calling the database selector inside its sequential source loop. Both constructors and normal DI
resolve the receiver. It receives every validated entry under the existing backup gate and worker
thread; failure cannot trigger legacy restores. Decoded buffers are cleared after completion/failure.

**Why.** Database selection cannot certify complete restore when another source such as portable settings
may still fail. Zanance attachment bytes remain inside the database source. The actual service must offer one exclusive owner for staging/recovery/publication.

**Verification boundary.** Real package/manifest/checksum service tests cover complete dispatch,
no fallback, borrowed-buffer clearing, corrupt-package rejection, missing/unknown source policy
and normal dependency injection. No receiver is yet registered in the main app. Native transaction,
full interrupted package acceptance, application encryption and device acceptance remain open.

D-201 verification: 42 backup tests and the complete 3,282-test suite pass with zero failures/skips.
Strict Windows and Android builds pass with zero warnings/errors. Logs: artifacts/native-sqlcipher/
d201-{backup-tests,full-tests,main-windows,main-android,test-clean}.log. No changed screens or native
code: no UI/unchanged native matrix rerun and no phone acceptance claim.

## D-202 — Protected package receiver and preference replay

**Decision.** Compose actual package dispatch with encrypted staging and selection. Preserve original
and restored allowlisted preferences in a separate generation-bound SQLCipher capsule before selecting
the restored database. Reapply durable settings before actual protected endpoint publication, including
selected-process restart without old connection/package. Retain all original/capsule/rollback/key files.

**Reason.** Platform preference APIs are not an atomic all-source transaction. A retained encrypted
packet makes partial preference writes recoverable before application access; the existing journal
selection and key binding remain the authority. Do not claim Completed or prune files in this receiver.

**Limits.** Caller still owns global covered/exclusive access, migrations, stable catalogue/key lifetime
and publication. Main registration and early interrupted-stage reconciliation are unfinished. A new
capsule schema is operational protocol storage, not an EF financial model change. No custom cipher.

D-202 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB fixture passed
actual BackupService package receiver, first-schema/current model migrations, external kill after
first native SharedPreferences committed value, reopened original/restored capsule, replay before
actual protected factory publication and restored money/device-state checks. Encrypted preference
and database rollback retained. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-
proof-96d1bcfdefd34c75b456534548dc4fa1.json, run 8cc8558c1a494c9f944d987e0e6b5445.
The fixture uses its own named SharedPreferences with synchronous Commit; it does not certify the
main MAUI adapter's asynchronous durability. Initial native compile lacked the MAUI preferences
reference; corrected fixture to native isolated adapter without adding a production package/bundle.
Native build zero warnings/errors; complete APK signature, ZIP and both reviewed ABI hashes pass;
independent package uninstalled. Driver can use an explicitly existing built APK and checks packaged
ABI identity before installation; no unchanged native suite or UI matrix rerun.

D-202 final managed evidence: 119 infrastructure tests pass. The complete run executes 3,287 tests:
3,286 pass and one pre-existing budget input-culture assertion fails because unordered child rows
were compared by enumeration order. Correct the assertion to compare category/value pairs sorted
by category identity (same established convention as the adjacent budget test). Focused rerun of
both input-culture variants passes; do not describe the original full run as green or repeat the
complete suite without another concern. Strict Windows/Android builds zero warnings/errors;
test outputs cleaned. Logs under artifacts/native-sqlcipher/d202-{data-tests,full-tests,
budget-order-tests,main-windows,main-android,test-clean}.log. Script parser and focused packaged
ABI hash helper pass after driver-only edits; unchanged native/runtime matrices not repeated.

## D-203 — Actual factory lifetime maintenance gate

**Decision.** Connect shared/exclusive ownership to the original DI factory and LocalDbContext
disposal rather than relying only on the autopost semaphore or remembered connection paths. An
exclusive request drains active contexts and blocks new admission; cancelled waiters release only
their claim. Explicit maintenance contexts come from the same factory with the exact current owner.
Owned queries and protected publication refuse retired ownership. Early owner disposal retains
exclusion until its native context is disposed. Synchronous callers fail closed instead of blocking UI.

**Reason.** Final migration/restore/profile publication cannot run while unrelated financial readers
or writers still own native contexts. Cover/background coordination remains an application lifecycle
responsibility. Main default-provider dependency was inspected and is still the SQLite bundle; its
final replacement also needs native candidate acceptance, including the recorded Windows warnings.
No production provider switch or main database encryption is claimed.

D-203 final evidence: 125 infrastructure tests and all 3,293 tests pass (zero failures/skips).
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Actual Android x64 API36/4 KiB protected factory additionally creates an owned maintenance context,
checks unchanged money, blocks ordinary admission, closes owner while its context is live, refuses
its next native command, then admits the waiter only after native/context disposal. Complete APK
signature and driver packaged native-ABI identity verify; owned package uninstalled and tests cleaned.
Native proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-a146cb94724947fb84be6f20740f02aa.json,
run 6c8e6cac24eb463e955482ba8ccd3d6c. The initial original-model observation expired; live PID 21141
subsequently reported OriginalModelCreated/32. Continued the same fixture with explicit completed-model
continuation, without recreating it. Preserve d203-native-factory.log and d203-native-factory-resume.log;
no timeout restart. Other logs: d203-{data-tests,full-tests,main-windows,main-android,native-build,test-clean}.log.
No UI/native crypto matrix rerun, main encryption activation or physical/iOS/performance acceptance.

## D-204 — Connect maintenance ownership to original profile switching

**Decision.** Take the actual factory maintenance owner before moving a reviewed profile, under the
existing autopost boundary. Carry ownership into actual migration/settings loading and path rollback,
then release it before lock reload/session events. Share the original settings initialization method.

**Why.** Without owned internal contexts a profile operation would wait behind its own exclusive
lease; without drain, unrelated work could access a temporary destination before authentication.
Queued readers must capture the final committed/restored path after admission.

**Boundary.** This connects the original main flow, not an encryption provider/catalogue activation.
The native fixture uses original ProfileService, data/store/migrations and autopost exclusion with
an explicit accept/refuse authentication decision port, not real biometrics or the owner credentials.
No money is posted by opening/switching. Final protected profile metadata/startup adoption remains open.

D-204 final evidence: 99 profile-related tests and all 3,295 tests pass with zero failures/skips.
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Original ProfileService/store/32 migrations/autopost/factory run in an independent Android x64
API36/4 KiB fixture: old context drains before path move, owned destination migration/settings
finish, unrelated work waits through refused authentication and reopens original main money/state,
with no ledger posting. Explicit native authentication decision port is simulated, not a real device
prompt. Full signature and reviewed packaged native-ABI checks pass; package uninstalled/tests cleaned.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-0e375f15d51e42fb955d1901d95dc132.json,
run 43b655df1e514b16a6a09209cde0ae87. Logs: d204-{profile-tests,full-tests,main-windows,
main-android,native-build,native-profile,test-clean}.log. No visible layout or unchanged crypto matrix
rerun. Remaining destructive inactive-profile file operations still need maintenance coordination,
alongside protected catalogue/provider/startup integration; main databases remain plaintext.

## D-205 — Coordinate inactive deletion with factory maintenance

**Decision.** The real confirmed profile Delete action awaits original context drain and revalidates
its captured review under ownership. Legacy synchronous callers acquire only immediately available
ownership. Both paths share original deletion logic and reject current/main profiles.

**Reason.** Inactive file deletion must not overlap an active/queued profile switch or a retained
factory context. A confirmation made before waiting does not authorize deleting changed catalogue
inputs afterwards. Cancelled/stale waiters do not mutate the catalogue or remove files.

**Boundary.** Existing local/cloud/unrelated file scope and file-error cleanup semantics remain.
This is maintenance coordination, not an atomic deletion journal or final protected key/generation
retention policy. No current/main deletion, mid-effect cancellation guarantee or encryption activation.

D-205 verification: 103 profile tests and all 3,299 tests pass, zero failures/skips. Strict Windows/
Android and independent Release AOT/trim builds zero warnings/errors. Original Android x64 API36/
4 KiB profile fixture confirms inactive deletion waits for an actual retained context, then removes
only inactive database/sidecars and retains original main/catalogue/money; original drain/authentication
refusal cases also pass. APK signature and driver packaged ABI identity verify; package uninstalled,
tests cleaned. The first observation expired while PID 22737 remained live; subsequent actual result
contained all six successful flags. Record same completed run without restart/recreation:
artifacts/native-sqlcipher/d205-native-profile-proof.json, run f3f5098787bd414da33b20f85a6cfe55.
Preserve d205-native-profile.log expiration evidence. Other logs: d205-{profile-tests,full-tests,
main-windows,main-android,native-build,test-clean}.log. Authentication is the decision port, not native
biometric acceptance; no new UI layout, unchanged crypto matrix, main encryption or protected deletion
lifecycle acceptance is claimed.

## D-206 — Persist nonempty local installation/profile key bindings

**Decision.** Use an explicit private versioned SQLite operational identity catalogue for automatic
installation GUID, legacy-to-key-binding profile mapping and captured initial generation pair.
The empty-id main profile gets a separate nonempty key-binding identity. Known profiles register
transactionally/idempotently; missing/corrupt state is never replaced by reads or registration.

**Reason.** Device key AAD and generation journal identity must survive process restart and cannot
be guessed from display names, account ids, commercial scope or a changing profile preference.
No keys/labels/financial facts or portable security preferences are added.

**Boundary.** This is identity bootstrap, not live selection/whole lifecycle activation. Main DI/
startup and later restored-generation references are not registered yet; protocol/journal/key/file
verification and final protected retention/recovery/deletion remain open. No EF financial model change.

D-206 final evidence: 131 infrastructure tests and all 3,305 tests pass with zero failures/skips.
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Independent Android x64 API36/4 KiB creates/commits actual two-profile identity catalogue and main
initial generation journal, externally terminates the completed process and reopens in a new one.
Exact identity fingerprint, nonempty main binding, existing registration and new-profile installation
preservation pass. No DEK/key preparation or main activation occurs. Signature and packaged reviewed
ABI identity verify; independent package uninstalled and test outputs cleaned. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b540cdeaaef44a2d82592e745cd8a3c3.json,
run ded8acc1a61547d19614cc82a3f3e58a. Logs: d206-{data-tests,full-tests,main-windows,main-android,
native-build,native-identity,test-clean}.log. Typed blob rejection was added during review before the
native fixture execution. No unchanged cipher/crypto suite, UI matrix, physical power-loss or main
whole-lifecycle acceptance is claimed.

D-206 negative build evidence: test-output cleanup was mistakenly started before Android build
finished, removing required copied assemblies/resources (MSB3030, 27 errors). Preserve
d206-main-android-clean-race.log; retry only the build after cleanup completes. No test/native
fixture restart or success claim for that failed attempt; final sequential Android build is separate.

## D-207 — Join initial identity, native migration and protected publication

**Decision.** Capture real durable identities under original location maintenance, resume existing
initial journal through actual key/durability/migration effects and verify the exact retained key
copy before atomically publishing the protected factory endpoint. Explicit fresh journal creation
is separate and requires the known plaintext source/no existing ciphertext. Missing read state never
creates replacements. A retained key owner outlives maintenance and rejects construction after disposal.

**Why.** Primitive success alone does not bind restarted installation/profile identity to the actual
selected generation/factory. Returned credentials must be the ones just verified, and must stay owned
until active contexts have drained. No guessed ids, plaintext fallback or read-time journal creation.

**Boundary.** Initial composition is not a later-restored live catalogue. Main startup/provider/
all-profile registration, recovery/retention/OS exclusion and physical/iOS acceptance remain open.
Clearing mutable managed key bytes does not promise erasure of immutable/native provider copies.

D-207 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB joins actual
stable identity, initial journal, original 32-migration model, native Keystore/envelope/fs effects,
full migration/cleanup and retained-key original factory. Saved account-name write/money survive
external process termination and cold reopen with an ExistingOnlyKeys port that forbids preparation.
Original is absent, encrypted rollback retained and disposed key owner rejects new factory access.
Initial observer expiry occurred while PID 23665 remained live; subsequent completed first-phase
result passed. Continue the same run via bootstrap-reopen only, without recreating data/key/journal.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-88e6c4bdb0fe4c3aaab969def6fcabee.json,
run a480c4301bbc4109bface2e6ab1530dc. Preserve d207-native-bootstrap.log expiration and
 d207-native-bootstrap-resume.log. Signature and reviewed packaged ABI identity pass; owned package
uninstalled. This is initial composition evidence, not main activation/live restored references,
hardware power-loss, physical arm64/iOS, final recovery/retention/OS policy or product release.

D-207 final managed evidence: 135 infrastructure tests and all 3,309 tests pass, zero failures/skips.
Strict main Windows/Android and independent Release builds zero warnings/errors. Tests cleaned
only after both main builds completed. Logs: d207-{data-tests,full-tests,main-windows,main-android,
native-build,test-clean}.log. Driver parser passes; no unchanged UI/cipher matrices repeated.

## D-208 — Persist restore intent and generation ancestry

**Decision.** Add an explicit installation/profile-bound reference history, retaining initial and
every parent/target restore generation. Reserve before effects; require latest identity in the actual
package receiver and mirror selected state only after actual journal/key/file checks plus replay.
Reject missing/changed/unresolved ancestry rather than replacing intent or choosing initial fallback.

**Reason.** Initial migration identity cannot locate later restored generations on restart. Durable
intent must survive interruption before settings/publication and retain ownership of older keys/files.
Reference status is not authorization or completed package/preference durability.

**Boundary.** History is operational metadata; main startup/live resolver/provider activation, later
preference-edit handling, key/reference pruning, recovery and complete lifecycle acceptance remain
open. Receiver's history argument is optional for existing callers; main final integration must use it.

D-208 evidence: 140 infrastructure tests and all 3,314 tests pass, zero failures/skips. Strict
main Windows/Android and independent Release AOT/trim builds zero warnings/errors. Actual Android
x64 API36/4 KiB package fixture reserves durable references before staging, externally terminates
at partial platform preference write, reopens same pending parent/target, verifies actual selected
journal/keys/files/replay and mirrors selection before protected factory publication. Original and
rollback/capsule references retained; money/device-state checks pass. The final observation expired
while PID 25465 remained live; actual result later contained all six successful flags. Record the
same completed fixture without restart: artifacts/native-sqlcipher/d208-native-restore-proof.json,
run ff097ff6eeb1401197918e20f2e57cff. Preserve d208-native-restore.log timeout evidence. APK signature
and reviewed packaged ABI identity pass, independent package uninstalled, test outputs cleaned only
after both main builds completed. Logs: d208-{data-tests,full-tests,main-windows,main-android,
native-build,test-clean}.log. No unchanged crypto/UI matrix or main startup/reference-retention/
physical/iOS/preference-durability acceptance is claimed.

## D-209 — Complete preference replay once after real persistence

**Decision.** Add a versioned encrypted replay marker and require an actual host persistence effect
before marking Applied. A failed persistence effect cannot mark capsule/reference/publication ready.
Applied capsules preserve later user edits on future opens rather than restoring obsolete settings again.

**Why.** Always replaying a selected backup would overwrite later language/culture/date choices.
Stopping after in-memory Set/Get would lose restored settings if platform writes had not reached storage.
Persist then durably mark/reopen/synchronize; retain pending replay across interruption.

**Boundary.** Protocol v2 rejects old independent v1 state; no silent recreation/upgrade. Main durable
platform adapters are not implemented/accepted merely by defining this callback. Native fixture uses
its own real synchronous SharedPreferences Commit, not the main MAUI adapter. Main lifecycle/provider,
recovery/retention and platform acceptance remain open.

D-209 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB actual package
receiver resumes externally interrupted partial SharedPreferences writes. Injected persistence failure
occurs before Applied/reference/publication; native guard confirms marker/reference remain pending.
Real synchronous SharedPreferences Commit then succeeds, encrypted marker readback passes and
protected publication/money/device-state checks pass. Later user language/culture edits are committed;
a third cold process reopens Applied state, preserves those edits and uses a persistence callback
that throws if old values are persisted again. No unchanged fixture/matrix repeat. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b3ab782cc57a4d8eb520356be0d66168.json,
run 53b6bb4b9a1a44a3ab0dfb4ec6481bda. Complete signature and driver reviewed ABI identity verify;
independent package uninstalled. This validates the independent synchronous adapter/ordering, not
main MAUI platform durability, device/iOS, main encryption/startup or recovery/retention acceptance.

D-209 final managed evidence: 142 infrastructure tests and all 3,316 tests pass with zero failures/
skips. Strict main Windows/Android and independent Release builds have zero warnings/errors.
Test outputs cleaned after both main builds completed. Logs: d209-{data-tests,full-tests,
main-windows,main-android,native-build,native-replay,test-clean}.log. Driver parser passes.

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
D-210 native evidence: original MAUI 10.0.110 Set/Remove and Android native persistence pass,
including cancellation refusal and cold restart value/removal checks. Run
098f4a594a9d4cd5b79703db501727f5; artifacts/native-sqlcipher/d210-native-cold-result.json.
The Windows PowerShell 5 observer stopped on expected temporary missing result stderr; the same
live process later produced both creation success flags. Only its cold-reopen phase was launched,
without recreating the fixture. Independent signed APK verified; package uninstalled afterward.
Strict Windows/Android/independent Release builds report zero warnings and errors.
D-210 final evidence: all 3,316 managed tests passed, zero failures or skips (3m32s).
Strict Windows (21.17s), Android (75.44s) and independent Release (77.46s) builds
passed with zero warnings/errors. Logs: artifacts/native-sqlcipher/d210-*.log.

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
D-211 evidence: two managed guards reject foreign history/installation before key/file effects;
all 3,318 tests pass (zero failures/skips, 3m07s). Strict Windows 30.69s, Android 56.77s and
independent Release 82.66s builds have zero warnings/errors. Signed Release AOT/trim Android
x64 API36/4 KiB runs the modified actual package restore: external partial-write interruption,
pending resume into the retained owner, real protected factory money 730 and device-state checks,
then cold Applied reopen preserving later choices. The obsolete publish callback throws if called;
both opens succeed without calling it. Exact restored binding/path are asserted before publication.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-e008a03fbb624134aa6ce18f65354aeb.json,
run bcd5d26ed6ad4da48fe20a7476f0854d. Signature/reviewed ABI identities verified; independent
package uninstalled. Test outputs cleaned after terminal builds. No unchanged native producer suite,
UI matrix or main phone APK was repeated. Main automatic startup routing/provider activation and
recovery/retention remain open; this is not complete application encryption or physical-device acceptance.
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
D-212 managed/build evidence: two new guards reject missing and foreign-initial history without
creating a migration journal or invoking native key work. All 3,320 tests pass (zero failures/skips,
3m02s). Strict Windows 29.55s, Android 25.64s and independent Release 77.38s builds have no
warnings/errors. Test outputs cleaned only after all builds/tests completed. Initial native routing
uses actual retained catalogue/history and an existing-only key port whose Prepare throws; cold
reopen preserves the saved account and financial packets and refuses access after owner disposal.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-60a8738158fb4efcb3de7a38de9ee1b6.json,
run 847826db8df04477b49f168da0e073b4. Creation observation expired while PID 28905 remained live;
its same completed three-success result was read, then only recorded cold continuation ran.
No initial fixture, identity or key was recreated because of observer expiry.
D-212 restored-routing evidence: actual stable identity bootstrap supplies the initial key context,
then the current resolver reads retained history after partial-write interruption and after Applied
cold reopen. Captured parent/target assertions and actual retained factory money/device-state checks
pass; later preference edits are retained and no obsolete publication/persistence callback executes.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-26a85b00255a4e55b4bf81f4acb93939.json,
run 13141ec1f63e4c078687bea65746dbc1. Signed APK and reviewed packaged ABI identities verified;
independent package uninstalled. Both current-generation branches now have actual original-model
cold process evidence; main startup/provider/profile registration and recovery/retention are still open.
No physical-device, iOS or product-release acceptance is inferred.
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
D-213 native evidence: the original ProfileService/Store/factory executes plaintext regression plus
actual encrypted main/destination switching under source-built SQLCipher and Android-bound keys.
Active context drain, owned migration/settings, refused authentication, queued keyed original reopen
(balance 1,250 with no ledger entries) and successful protected destination/return all pass.
Authentication is an explicit fixture decision port, not owner/device biometric acceptance.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-8ad234ba8cf8479c8b10a9d36869ba85.json,
run 63d29cca400447179541651a003d2527; ProtectedSwitchAndKeyedRollback=true. Original inactive
plaintext deletion remains tested separately; encrypted file/key deletion policy is not claimed.
Signed Release AOT/trim APK, reviewed ABI identities and signature verified; package uninstalled.
Strict Windows 65.03s, final Android 29.46s and independent Release 95.28s builds report zero
warnings/errors. Successful builds and the stock native producer suites were not repeated.
D-213 final managed evidence: all 3,322 tests passed with zero failures/skips (3m00s).
Two actual-factory tests retain the original bound key creator after rollback and reject repeated,
foreign-owner or protected-to-plaintext restoration. Final builds are warning/error-free; the
initial shared-DLL lock failures remain in d213-*-lock-failure.log. Test outputs cleaned only
after all builds/tests completed. No main encryption, physical/iOS authentication or release claim.

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
D-214 final evidence: all 3,324 tests pass (zero failures/skips, 3m23s). Two new session guards
reject unresolved/unstarted state and key retirement while actual owned contexts remain. Strict
Windows 24.08s, Android 19.02s and independent signed Release AOT/trim 57.60s builds have zero
warnings/errors; builds/tests were sequential where outputs overlap, with no DLL-lock retry.

Android x64 API36/4 KiB actual original-profile flow uses stable identities, actual initial migration
and history for both profiles. Session resolves through OpenCurrentAsync with an existing-only key
port (Prepare throws), reads/retains original money, rejects authentication, verifies rejected-owner
disposal while the original remains usable, then accepts destination and return and verifies all
previous owners disposed. Session close refuses an owned context and succeeds after disposal.
Native decision port is fictitious authentication, not device biometric acceptance. Independent
proof: artifacts/native-sqlcipher/d214-native-profile-proof.json; run
9434f1b35a1d438cb3cf9f76a1d60574. Observer expired; same PID 31527 completed all seven flags,
read directly without reinstall/restart/recreating data or keys. Profile observation bound increased
to 300 attempts for its now-longer two-profile migration; parser checked without repeating the run.
Reviewed ABI identities/signature verified; package uninstalled and test output cleaned after terminal
builds/tests. Main startup/provider/registration, recovery, files and OS policy remain unactivated/open.
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
D-215 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB records the
pending initial intent before native key work, then an external process stop/start completes the
same captured identity through OpenCurrentAsync and actual migration/factory. A third cold process
uses an existing-only key port (Prepare throws), retains the saved account and money, verifies
selected initial ancestry, and refuses new construction after key-owner disposal. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-7562abeb4ea24d4b9a90fbb7011b1916.json,
run 4467935ba0f242168d5dce1f3cb9d315. InitialIntent/Created/Result guards all pass. No scenario
restart/recreation or unchanged native suite/UI matrix repeat. Signature/reviewed packaged ABI
identities verified; package uninstalled. Strict Windows 19.00s, Android 18.10s and independent
Release 55.61s builds report no warnings/errors. Actual initialization is verified only after the
pending record commits; torn/failed metadata creation itself and final main activation remain open.
D-215 final managed evidence: all 3,327 tests pass with zero failures/skips (2m55s).
Three new cases verify pending initial persistence, refusal of restore before completion and
rejection of foreign/advanced journals before history creation. Outputs cleaned after terminal
builds/tests. Main activation, torn metadata creation, recovery/retention and platform gates remain.

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
D-216 final managed evidence: all 3,329 tests pass with zero failures/skips (2m47s).
Two new cases preserve staged identities after interrupted synchronization and reject a changed
profile set without rewriting staging or creating the final catalogue. Native interruption proof
is separate from these injected managed effects. Outputs cleaned after terminal builds/tests.

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
D-217 final managed evidence: all 3,333 tests pass with zero failures/skips (2m46s).
Four new cases cover captured journal/history resume and rejection of advanced or differently
selected staging without publishing or rewriting it. Outputs cleaned after terminal builds/tests.
Main startup/provider/recovery and physical/platform release acceptance remain unfinished.

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
D-218 final evidence: all 3,337 managed tests pass with zero failures/skips (2m58s). Four new
cases reject unauthorized missing installation, duplicate original paths, changed profile capture
and missing installation with retained migration state before native key/metadata effects. Final
strict Windows 10.58s, Android 16.48s and independent Release 58.51s builds are warning/error-free;
build iterations before final guard edits are not used as final verification. Tests run after builds.

Independent signed Release AOT/trim Android x64 API36/4 KiB prepares two actual original 32-migration
profiles through the complete new bootstrap. Original main/work entries 250/880 and opening balance
1,000 remain distinct. No application endpoint changes during the pass; retained candidate and encrypted
rollback verify for both. A cold pass with a key port whose Prepare throws preserves all stable identity
fingerprints, financial packets and rollback. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-0796e72e6cdc4fe98e3783c3ce11b8d5.json,
run 6f788670f1204d3d8e32072835e6a0a1. Created/Result all eight guards pass, with no scenario restart
or unchanged native suite/UI matrix. ABI identities/signature verified; independent package uninstalled.
Outputs cleaned after terminal tests/builds. This verifies captured existing profiles, not automatic
new-profile creation, inactive restore policy/UI, main activation, physical/iOS or release acceptance.

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

D-219 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB stops after
actual durable key preparation and resumes the same captured new-profile intent. All 32 real
migrations execute in memory; encrypted candidate/rollback match, original plaintext path is absent.
The saved real settings row survives cold reopen. The prepared native key envelope SHA-256 is
unchanged in both later processes; the cold key port cannot prepare keys. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-02ee3d0d3285402f86b1ba06858b6c33.json,
run 0dbe974b57f94fe6975c57e9331aba71. Both packaged ABI hashes and APK signature verified; independent
package uninstalled. No scenario restart or unchanged native suite/UI matrix. Strict final independent
Release 56.86s, Windows 21.77s and Android 20.19s builds have zero warnings/errors.

AT-219 managed guards reject existing originals, missing explicit intent and unknown ciphertext.
Two initial fixture cases used a sibling directory outside the captured private database directory
and failed before exercising their intended guards. Corrected fixture paths; all 166 Data tests
pass in 1.94s. No application code changed for this correction, so native/build checks were retained.

D-219 managed verification covers 3,340 cases: the full solution run passed all other assemblies
and reported only the two new fixture-path failures (3m02s). After the fixture-only correction,
all 166 Data cases pass with zero skips/failures. No unresolved failure remains; the unchanged
assemblies were not rerun. Test output cleaned only after all builds/tests became terminal.

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

D-220 verification: all 3,343 solution tests pass, zero failures/skips (3m03s), including three new
actual-flow cases for context drain/unpublished preparation, failure and catalogue retirement.
Strict final Windows 24.34s, Android 18.90s and independent Release 57.27s builds have zero warnings/errors.
The first Windows attempt exposed use of an internal ownership helper across assembly boundaries;
the final adapter uses the public captured-publication ownership check. Intermediate builds are not
final evidence. The session itself still enforces the real drained owner and retained publication.

Independent signed Release AOT/trim Android x64 API36/4 KiB runs actual ProfileService creation and
switching, native key preparation, identity registration, in-memory 32-migration schema and protected
session publication. New profiles are absent from product preferences until their encrypted files
verify; creation leaves the main endpoint unchanged. Distinct opening balances 250/880 survive a
cold process and a real product switch. Native MAUI preferences are explicitly durably persisted.
Proof: artifacts/native-sqlcipher/build/android-d/d220-profile-creation-proof.json,
run f036ded6af784345bf6047ebc3f8b39e. The runner advanced from successful creation to cold reopening,
then its bounded observation expired. PID 6074 remained live and its exact later cold-result JSON
passed all three checks. No process/scenario restart or fixture recreation was performed. The
observation failure is retained as negative harness evidence, not described as an application failure.
Both APK ABI hashes/signature verified; independent package uninstalled. No layout/strings changed;
this runtime proof does not certify main UI rendering, phone, arm64/API24/16KiB, iOS or main encryption.

Main Release APK built once with Build-AndroidApk.ps1: artifacts/android-d220/pro.vafadar.zanance-Signed.apk
(83,294,522 bytes). Signature, exact pro.vafadar.zanance package, API24/36 and both ABI embedded
libassembly-store.so plus application AOT libraries verified. This uses the local test certificate,
not Store signing; no main/owner database was read or installed over for this step. It still contains
the unactivated plaintext main provider. Test cleanup follows terminal builds/tests only.

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
D-222 managed verification: all 3,346 cases pass, zero failures/skips (2m55.9s), including actual
StartupViewModel overlap/completion, explicit failure retry and refusal of plaintext startup with
a configured protected session. Eighteen native Windows cases cover en/fa/de, light/dark and
360x800/412x892/1280x820 at process-local FontScale 2. Thirty-six own-window captures show complete
loading/failure captions. Real native retry Invoke, at least 44px targets, actual storage/window
thread separation and complete stored table-row equality all pass. Records: artifacts/d222-startup.
The original app-data directory was restored without reading its contents; final fictitious data
was parked separately. No desktop focus/input, whole-desktop capture or OS setting change was used.

The first diagnostic compilation passed a language string where AppLanguage was required; corrected
the exact language lookup. A later matrix caller mistook stale LASTEXITCODE for a failed skipped build;
the already successful light-360 case was retained, not repeated. Remaining cases used terminating
script failures and actual proof validation. Do not erase these negative harness observations.

Final ordering review found the early reminder watcher could schedule a read after the initial
settings write. WatchChanges now attaches only after storage/access preparation. This changes only
the MAUI host subscription order, not the tested non-UI sources or reviewed layout; their passing
checks are retained. Final strict builds and cold application QA cover the host change.

An isolated functional startup package overrides ApplicationId to
pro.vafadar.zanance.startupverification; it never installs over or reads the existing Android package.
The Debug variant failed OS process attach/start timeout before Activity startup, with no managed
startup error reported. Its negative observation remains; Release QA uses the phone build settings.
This is application startup QA, not an encryption research probe or acceptance of OAuth/device/PIN,
performance, iOS, complete encryption or release. No native FLAG_SECURE was removed.
D-222 Android completion: isolated signed Release AOT/trim QA visibly displayed the actual preparing
surface, then reached the real onboarding wizard with Welcome/Language/Next and no forced account.
The process remained live. A slow observed migration was initially treated as a possible dispatcher
problem; adding the real Window.Created dispatcher hook did not establish that diagnosis. Temporary
QA-only fixed milestone/thread records showed window/start/prepare/worker/storage/settings/UI
continuation/access/ready all completed. No deadlock or dispatcher failure is claimed. Slow emulator
observations do not meet the performance target. The per-window start hook is retained with the
existing duplicate-attempt guard; its normal startup and compatibility callback are verified together.

All temporary milestone source, compile flag and project override were removed before final delivery.
The isolated QA package and exactly owned dump files were removed. Existing main Android data/package
was never installed over or read. Records: artifacts/native-sqlcipher/d222-qa-{milestones.txt,
release-ui.xml,proof-ready.xml}. Final normal Windows 20.96s and Android 69.98s builds have no warnings
or errors; earlier intermediate build timings are not final delivery evidence. No non-UI tested source
or layout changed during these host/diagnostic corrections, so the passing 3,346 cases and 18 geometry
cases were retained. The normal phone APK is rebuilt after diagnostic removal.

Final normal phone artifact: artifacts/android-d222-delivery/pro.vafadar.zanance-Signed.apk.
Package pro.vafadar.zanance, local test signature, API24/36, arm64/x64 assembly stores verified.
It is a complete Release package; main storage is still plaintext. All temporary proof instrumentation
and build flags are absent from final source. Test outputs were cleaned after all builds/tests ended.

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
### D-224 — Compose all-profile preparation with covered application startup

ProtectedDatabaseStartup now implements the real IAppDatabaseStartup contract. It waits for drained
application ownership, captures every original profile, invokes the complete native bootstrap and
publishes only the selected original through SqlCipherProfileSession after all profiles verify.
Repeated completed callbacks do not resolve or replace keys again. Explicit first-install authority,
provider adoption and actual native key/file/restore effects are supplied by the final host; missing
metadata never grants that authority. No registration or main-app encryption activation occurs yet.

ProfileService.CaptureStorage reads only device catalogue preferences and retains an immutable path
map plus an actual catalogue recheck. It includes every retained profile regardless of commercial
availability. Financial access and commercial quota evaluation are inappropriate before storage is
prepared. A changed catalogue invalidates the captured preparation before session publication.
The native-independent tests compile these actual app sources; 19 ProfileTests pass including both
new capture scenarios. Native provider acceptance and successful composed startup are recorded
separately below, rather than inferred from these capture tests.

Main native/provider/startup registration, fresh installation, profile lifecycle reconciliation,
restore routing, optional recovery, safety retention and coupled OS exclusion remain in progress.
No visible layout changed and unchanged layout matrices/native producer suites are not repeated.
D-224 actual native verification: independent signed Release AOT Android x64/API36/4 KiB
initial and cold runs pass. The actual startup composes two existing originals before a single
selected-work publication; amount 880 and opening balance 1000 remain correct. Repeated startup
does not resolve or publish again; cold key preparation is forbidden. Both original plaintext
files are absent. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-0745a8874d4c489eb2e71304eb953135.json
(run 830eb2f81d5c4424a6447793485c02cd). Native build: zero warnings/errors, 69.24s.

D-224 final managed verification: all 3,363 tests pass with zero failures/skips (3m26.055s).
Strict Windows and Android builds have zero warnings/errors (21.11s and 83.05s). Main storage
remains plaintext because final native/recovery/retention/OS registrations are not activated.

### D-225 — Explicit encrypted fresh-install startup

ProtectedDatabaseStartup now distinguishes ExistingInstallation, LegacyPlaintextProfiles and
NewInstallation. Absence of an identity file never selects a mode. NewInstallation requires
one captured main profile and the real schema builder; identity metadata must stay in that
private directory. Retained database/journal/key artifacts refuse creation of replacement
identities. Its own staged identity publication may resume under the explicit new-install authority.

The actual SqlCipherNewProfile path creates all 32 application migrations in memory, writes
only verified encrypted candidate/rollback files and returns to the same all-profile bootstrap
and single session publication. No account or financial movement is manufactured. Existing
installation startup never runs the schema builder or prepares replacement keys on cold read.
Main host registration remains pending final recovery, retention and lifecycle adoption.

Three actual app-source authority tests pass. The first byte-preservation fixture check failed
because its completed Windows SQLite migration retained a pooled file handle; the fixture now
closes only its own scoped pool before comparing bytes. No production pooling change was made.
Independent signed Release AOT Android initial/cold startup preserved a fictitious opening balance
of 321 and refused missing identity metadata beside retained encrypted/key artifacts. The first
cold observation deadline expired; its same-process successful result was read before any restart
and retained in artifacts/native-sqlcipher/d225-native-first-observation.json (run
31fdee67b3ac43978d8033c16166c54f). Final code limits artifact matching to file names independently
of parent names and letter case; only the cold phase of that recorded fixture is rechecked.
The runner supports explicit recorded fresh-fixture continuation and a longer bounded observation.
Final proof and managed/build results are recorded below when terminal; no main encryption or
physical-device/iOS/recovery acceptance is inferred from an independent probe.
D-225 final evidence: cold-only verification on the same recorded fixture passes with the final
signed Release AOT build; no original/profile data was recreated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-96d18899dce344978fd693ea0b53cc69.json.
Its Created=null explicitly denotes continuation, not a repeated initial run; the first observation
is retained separately. Full managed suite: 3,366 pass, zero failures/skips (3m14.247s). Strict
Windows 26.09s and Android 44.32s: zero warnings/errors. Final independent native build 83.58s:
zero warnings/errors. Main runtime registration, recovery/lifecycle/retention/OS adoption, physical
phone, remaining native matrix and iOS acceptance remain unfinished.

### D-226 — Native Windows generation keys and file synchronization

WindowsGenerationKeyStore now implements the actual generation-key effects with CurrentUser
DPAPI, canonical captured bindings and immutable final file creation. The file receives a protected
owner-only ACL at creation, is flushed through the native handle and its directory, and is reopened
and authenticated before preparation returns. A missing read never prepares a key; existing damaged,
foreign or hard-linked envelopes fail without replacement. Caller-owned DEK buffers are cleared.
A torn exclusive final write is retained for reconciliation, never repaired by fresh random key material.

WindowsPrivateFiles uses exact no-follow native handles, regular-file/link-count checks, redirected
ancestry rejection and final handle/path identity checks. It holds a non-deletable root handle during
operations. WindowsGenerationFileDurability calls real FlushFileBuffers for captured regular files
and the directory; native failure propagates. A direct owned-folder probe first observed successful
directory flush on this development filesystem. This is observed API completion, not a universal
filesystem guarantee or physical power-loss acceptance. Same-user/privileged code is outside DPAPI's
protection boundary. Official API references:
https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-flushfilebuffers
https://learn.microsoft.com/en-us/windows/win32/fileio/obtaining-a-handle-to-a-directory

Nine Windows-native storage tests pass, including repeat preparation retaining exact envelope bytes,
missing/foreign/truncated/oversized state, retired access, unrelated-file refusal and hard-link refusal.
The independent Windows native caller now uses these actual effects. Two separate processes create
and reopen the exact encrypted SQLCipher packet (Int64.MinValue), using existing keys only in the
second process. It does not replace the original complete schema/migration algorithm or rerun an
unchanged producer/upstream suite. Proof:
artifacts/native-sqlcipher/build/windows-d/key-proof-563590e555544bed8b7802c7204859df/key-reopen-proof.json.
Actual ACL readback: protected DACL, one non-inherited Allow rule for current user only;
artifacts/native-sqlcipher/d226-acl-proof.json. No credential/key bytes are logged.

Main native/provider/startup registration, profile lifecycle reconciliation, optional recovery,
retention, coupled OS policy, remaining native matrix and Windows narrowing-warning review remain
unfinished. Main storage is plaintext; this does not establish final native adoption or release.
D-226 terminal verification: 3,375 tests pass, zero failures/skips (3m04.599s). Strict Windows
56.85s and Android 49.48s builds have zero warnings/errors. Nine native Windows storage behaviours
pass; independent two-process key/database reopen and actual current-user-only protected ACL
readback succeed. Test-WindowsKeys.ps1 now enforces that ACL and records its own acl-proof.json;
the new check was applied to the same existing fictitious file without regenerating keys/data.
No visible layout changed or unchanged Android runtime/upstream native suite was repeated.

### D-227 — Complete Windows native startup and scoped original-pool retirement

Independent Windows startup now uses the actual ProtectedDatabaseStartup, profile parser,
complete migrations/bootstrap/session and WindowsGenerationKeyStore/FileDurability. Shared
FreshStartupCompatibility, ProtectedStartupCompatibility and OriginalProfileFixture remove
platform-specific copies of the fixture flow; Android injects its actual native effects and
Windows injects its actual DPAPI/file effects. These callers are not production registrations.
The initial Windows caller build reported CA1416 at deferred platform factories; explicit Windows
checks inside those factories resolve the actual reachability issue without warning suppression.

Windows fresh initial/cold startup passes: all 32 real migrations build in memory, no plaintext
original or automatic account appears, explicit fictitious opening balance 321 survives, and
missing identity beside retained state refuses replacement. First legacy two-profile startup
then exposed a real cleanup failure: the disposed work factory left an idle pooled handle open,
so Windows refused deleting its original after durable encrypted selection. Retained state and
the original failure log were preserved; no fixture or key was regenerated to hide the failure.

SqlCipherProfileBootstrap now clears only each captured original's known pools after its actual
application owner drains contexts, before native preparation/cleanup. This fulfills the migration
session's existing caller obligation; it does not clear all pools or touch unrelated profiles.
The same interrupted Windows fixture resumes with an intentionally re-primed product idle pool
and existing-key-only effects, then a separate cold process reopens both prepared profiles.
Selected work amount 880 and opening balance 1000 remain correct; one session publication/resolution
occurs despite repeated startup. Managed regression uses actual SQLite TEMP session markers to
prove target retirement while the unrelated pool survives. Its first fixture placed identity
metadata outside the captured data directory and was correctly rejected; only that fixture path
was corrected, without weakening the production directory guard. All 16 bootstrap tests pass.

Proof: artifacts/native-sqlcipher/d227-windows-startup-proof.json; retained interrupted fixture
migration-startup-proof-8b789542ac4a468e8799d3b44b2978ce, original error d227-legacy-create.log,
actual pooled-handle resume d227-legacy-resume.log and later process d227-legacy-cold.log.
Test-WindowsStartup.ps1 supplies reproducible initial/cold wrapper phases and candidate hash
checks. Its parser is checked; this turn ran the actual CLI phases separately to retain/resume
the observed failure instead of regenerating test data through the wrapper.

Main registration, profile lifecycle reconciliation, recovery, retained-file policy, final OS
exclusion, remaining native matrix and Windows narrowing-warning review remain open. These
proofs do not activate main encryption or establish physical-device, iOS, power-loss or release
acceptance. Android/shared caller runtime and terminal build/test evidence are recorded below.
D-227 terminal evidence: Android signed Release AOT x64/API36/4 KiB initial and cold runs pass
with shared fixtures and actual Android effects; selected work amount 880 is retained. The harness
initially reported a missing process after a single failed adb observation; direct revalidation
found the same live process (10399) and completed initial result. Only its cold phase then ran,
without fixture/key recreation. The harness now revalidates the same package and completion target
before classifying missing-process termination; parser checked, no simulated branch acceptance
claimed. Proof: artifacts/native-sqlcipher/d227-android-final-proof.json (run
45123c35833f4c94a5d50c2c07eb2d9b). Shared Android probe build: 71.83s, zero warnings/errors.
Full managed suite: 3,376 pass, zero failures/skips (3m13.882s). Strict Windows 19.12s and Android
49.50s: zero warnings/errors. Main adoption and full security/platform/release gates remain open.

### D-228 — Retain and retry an initial protected publication after observer failure

The shared native caller injects a real first Changed-observer IOException after the protected
endpoint is installed. Before the fix, that original call disposed the already published key owner;
covered startup then rejected retry with 'requires the original unpublished endpoint'. The actual
Windows failure is retained in artifacts/native-sqlcipher/d228-reproduction.log; the existing
fictitious D-227 dataset was reused, not regenerated.

SqlCipherProfileSession retains a generation only when the actual endpoint/path/creator/version
still identify that generation after a failed initial publication. A failed-original marker admits
one startup recovery for that exact original under drained ownership. Ordinary duplicate starts
still fail. Close clears both the owned key and recovery marker. Unpublished generations still
clear on failure; no plaintext publication or replacement key is introduced.

ProtectedDatabaseStartup retains the complete prepared profile capture immediately before initial
publication. Retry requires that same actual catalogue and session owner, never merely an arbitrary
protected endpoint. It skips already completed bootstrap/schema/native key reads. The failed
notification is repeated for the exact retained endpoint without changing its identity or pools,
so later observers that did not run receive it; readiness is accepted only after notification and
publication validation succeed. Notification failures keep recovery available and startup covered.

Native Windows verification reuses the original encrypted account/balance 321 and forbids key
preparation. It proves the published key still opens actual data after failure, changed device
catalogue retry is rejected without key reads, valid retry preserves the exact endpoint and key-read
count, late observers run, and ordinary duplicate session start remains rejected. The same shared
case is available in the Android caller with StartupNotification mode. Managed tests separately
check retained-endpoint notification delivery/foreign-owner refusal and refusal of an unowned
already-protected startup. They do not substitute plaintext fixtures for native encryption proof.
Windows proof: artifacts/native-sqlcipher/d228-native-complete-proof.json. Actual final native
build/runtime and full managed results are recorded below when terminal. Main native/provider,
recovery/lifecycle/retention/OS adoption and remaining platform/warning gates remain open.
D-228 terminal verification: actual Windows and Android observer-failure cases pass. Android
proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-6b204a3f4b4c4c37bf842904a534ab33.json;
shared signed Release AOT x64/API36/4 KiB case confirms retained key/endpoint, unchanged balance
321, changed-catalogue refusal, observer delivery and no repeated key reads. Native Android
build: 66.96s, zero warnings/errors. All 3,378 managed tests pass, zero failures/skips
(3m13.563s); focused app authority cases 4/4 and publication contracts 15/15 pass. Strict
Windows 27.65s and Android 46.90s builds have zero warnings/errors. No visible layout changed,
no unchanged producer/upstream suite was repeated, and main storage remains plaintext.

### D-229 — Adopt a selected whole-package restore into the active protected session

SqlCipherProfileSession.AdoptRestoredAsync resolves/rechecks the exact selected same-profile
binding/path/version through the existing native generation resolver. It retains previous and
restored key owners until the actual publication notification and maintenance drain succeed.
A post-publication observer failure leaves the restored owner installed and alive; only that
exact pending restore may resume notification/adoption. It never rolls financial data back beneath
already applied portable preferences. Ordinary profile-switch completion cannot accept a pending
restore. Successful adoption retires only the previous in-memory owner, not its retained file/key
references; repeating the accepted exact adoption performs no new resolution or publication.

SqlCipherPackageRestoreReceiver now explicitly disposes the old registered connection after
selection/replay/history verification and before its publication callback. That callback can then
release its owning EF context and satisfy the actual session's drained-owner requirement. The
receiver's existing await-using remains an idempotent failure cleanup. No ledger algorithm or
portable password policy changes; the final native host still owns registration and UI covering.

The shared native RestoreSessionCompatibility runs the actual BackupService/source/package
receiver/history/session with actual platform keys/files. A real package captures the encrypted
profile's balance 321 and allowed locale en in memory. The fixture deliberately changes balance
to 777/locale de, restores the package, injects an actual observer IOException and reads restored
finance through the still-live pending key. Exact resume then adopts without repeated preference
persistence; device-only fake security remains unchanged. It verifies the old registered connection
was closed before adoption and rejects using profile-switch completion for a pending restore.
A separate cold process resolves the recorded restore with existing keys only and refuses old
connection/upgrade/publication/persistence effects. An explicit later fictitious locale choice fr
is preserved. Independent preference effects are native-flushed fictitious file storage, not
claims of final MAUI platform preference registration.

Windows retained D-227 dataset was used. Proofs: artifacts/native-sqlcipher/d229-native-runtime.log
and d229-native-cold.log. The first run preceded the later-choice fixture step; the later fr choice
was explicitly written/flushed into its owned fictitious preference file before cold verification,
without rebuilding its finance/key state. Two managed contract scenarios prove no adoption
resolution before a started session or with a foreign maintenance owner; all 17 publication/factory
contract tests pass. Shared Android runtime and final managed/build evidence are recorded below.
Main native/restore registration, profile reconciliation, optional recovery, safety retention,
final OS policy, remaining native/warning/device/platform and product release gates remain open.
D-229 terminal verification: shared signed Release AOT Android x64/API36/4 KiB whole-package
restore, notification-failure continuation and separate cold restored-session read pass.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-4f192702b8794260919da1951279e423.json
(run 0b6bd586086d46a793e0f4e4ee0b6039). Activity-launch observation timed out while native PID
13018 was confirmed live; its same restore completed and only the next cold phase followed,
without fixture/key reset. Native Android build: 73.14s, zero warnings/errors. All 3,380 managed
cases pass, zero failures/skips (4m00.879s); strict Windows 29.73s and Android 51.99s have zero
warnings/errors. Main native/restore registration and full security/platform acceptance remain
unfinished. Fictitious file preferences qualify that effect, not final MAUI preference integration.

### D-230 — Refuse legacy deletion of protected profile storage

ProfileService's legacy DeleteCore previously removed the device catalogue row before deleting
only its old original filename and portable backup prefix. Protected generations instead retain
ciphertext, journals/history, bound key envelopes and opaque safety references. Using that legacy
path would strand stable encryption identities and make the next complete bootstrap inconsistent.
That path now refuses deletion before any catalogue publication, file effect or Changed event if
the active endpoint is protected or the target has actual generation/new-profile/history/staged
metadata or ciphertext artifacts. The legacy path remains for ordinary plaintext profiles.

This is a permanent storage-authority boundary, not an implementation of protected retirement.
The final native deletion transaction must explicitly capture authorization, persist retirement
intent/catalogue outcome and reconcile its complete referenced artifact/key set across crashes.
It must also handle uncommitted creation identities. Main native activation remains gated on that
work; no deletion, reconciliation, retention or overall SEC completion is claimed here.

Four actual app-source regression scenarios preserve complete device catalogue packet, original
profile row, notification count and exact artifact bytes for protected/current and partially
prepared targets. All 23 ProfileTests pass. Native Windows uses the retained D-227 two-profile
fixture and actual encrypted session switch to main, then invokes actual deletion of inactive
work. It is refused, both profile rows/active endpoint remain and hashes of every owned retained
artifact are unchanged. Existing keys are read only. Proof:
artifacts/native-sqlcipher/d230-native-guard.log. Shared Android caller has the same focused guard
scenario. Main application still uses plaintext registration; no visible layout changes or
unchanged producer/upstream suites are repeated. Terminal platform/build evidence follows below.
D-230 terminal verification: all 3,384 tests pass, zero failures/skips (3m11.386s); strict
Windows 28.01s and Android 45.92s have zero warnings/errors. Actual signed Release AOT Android
x64/API36/4 KiB guard refuses inactive protected deletion after actual switch to main, leaving
profile catalogue/endpoint and hashes of all retained artifacts unchanged. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-3577a018f28e42409c0e679b11872c8f.json
(run f784a71ef4714eb7b293053e46de2304). Native probe build 66.88s, zero warnings/errors.
Activity observation timed out with PID 14037 confirmed live; the same run completed and cold
verification followed without reset. A later adb cleanup connection failed after the proof was
saved; the same local port was reconnected and only the owned independent package uninstalled.
This does not implement protected retirement or activate main encryption; those remain open.

### D-231 — Durable retirement barriers and ordered continuation

A reviewed non-main retirement now has an exact private SQLite request binding (legacy profile,
stable profile, installation and operation), with ordered Authorized / CatalogueRemoved /
ArtifactsRemoved / Completed barriers. Metadata is staged, verified, published without replacement
and synchronized through the native durability port. Missing, foreign, incorrectly typed or extra
schema state never creates deletion authority. Main retirement is refused.

ProfileRetirementCoordinator resumes only an existing request under the host's exclusive
maintenance ownership. Host operations must be idempotent and verify actual durable catalogue,
complete captured artifacts and key-reference reconciliation before the next barrier is committed.
No filename enumeration, key deletion, inferred identity or main-app registration is supplied here.
A committed SQLite phase may remain observable after native synchronization fails. Resume therefore
synchronizes the actual journal and directory and checks unchanged state before every native effect;
reading the phase alone cannot authorize cleanup. Completed resumes validate the host without
repeating deletion. Cancellation and failed ownership/phase validation preserve the retained intent.

Nine focused real-SQLite tests pass. They include observable commit followed by native flush failure,
repeated failed requalification with no cleanup, exact successful continuation, stale/foreign/missing
requests, extra schema and retired ownership. Test durability/effect ports prove ordering only.
The independent Windows caller uses the reviewed native SQLCipher DLL and actual private Windows
file/directory flushes in two processes. The first durably clears a fictitious catalogue file, commits
CatalogueRemoved, injects an explicit flush failure and leaves both captured fictitious files intact.
A separate cold process requalifies that exact journal, removes only the captured fixture files,
synchronizes each removal and completes; repeated completion does not repeat effects. Evidence:
artifacts/native-sqlcipher/d231-native-create.log and d231-native-resume.log; the retained independently
owned directory is recorded in d231-owned-retirement-path.txt. Native build has zero warnings/errors.
These fictitious file effects are not final MAUI preference publication, a complete profile/key
inventory, forced process termination, physical power-loss acceptance or Android/iOS retirement proof.

Main registration remains plaintext. Actual protected profile retirement, uncommitted creation
reconciliation, optional recovery, reference-aware safety retention, coupled OS exclusion and remaining
native/platform/device gates are still open. No visible layout changed; upstream native producer suites
and unchanged UI matrices were not repeated. Terminal managed/main-build evidence follows below.

D-231 managed verification: the complete suite passed 3,393 cases with zero failures/skips
(3m32.653s). Final review found that LIKE treats the underscore in sqlite_% as a wildcard,
which could conceal an unrelated sqliteAlias table. Exact GLOB filtering now keeps the underscore
literal; the extra-schema regression uses that name and all nine affected cases pass again
(1.048s). Unchanged application/UI scenarios were not repeated for this isolated validation fix.
The final native Windows caller was rebuilt without warnings/errors (1.91s); both actual
Windows fixture processes pass again. Final main Windows strict build has zero warnings/errors
(25.51s). Android/package verification is recorded below. Final Android/iOS retirement effects
and main encryption activation remain open.

D-231 final main Android strict build has zero warnings/errors (42.29s). No native producer,
unchanged UI matrix or unrelated acceptance suite was rerun for the final schema-filter correction.

D-231 final signed complete Release APK: artifacts/android-d231-delivery/pro.vafadar.zanance-Signed.apk
(83,417,818 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores were checked.
Test outputs were cleaned after terminal builds/tests. This is not phone installation, native
retirement integration, encrypted main registration, store signing or product release acceptance.

### D-232 — Exact stable identity retirement after verified references

SqliteDatabaseIdentityCatalogue now removes only the reviewed non-main binding after the existing
retirement journal has reached ArtifactsRemoved and been natively requalified. It verifies the
installation/profile bindings before the host inspects financial/key references, then rechecks the
complete captured identity catalogue inside the write transaction. Foreign installations, replaced
same-legacy identities, stale journal state, changed catalogue and retained references fail closed.
The deletion compares both legacy and stable profile identifiers. Main and every other complete
binding must survive exact readback after native file/directory synchronization. A prior deletion
visible after failed flush may resume with the same journal/reference verification and synchronization;
absence alone never authorizes retirement or creates replacement keys. The journal is left at its
artifact barrier until the coordinator accepts completion. No EF model/schema change is introduced.

The independent Windows retirement caller now uses an actual two-profile identity SQLite catalogue,
not a placeholder identity file. It records the main binding, preserves work identity and its captured
fictitious artifact through the injected catalogue-barrier flush failure, then a cold process uses the
actual RetireAsync and coordinator to remove exactly work identity and verify main unchanged. Its
preference/artifact files remain fictitious; this is not a complete final MAUI/native key inventory.
Proofs: artifacts/native-sqlcipher/d232-native-create.log and d232-native-resume.log; its fresh retained
owned folder is recorded in d232-owned-retirement-path.txt. Final managed/native evidence follows below.

This closes the missing exact identity-removal operation, not the whole protected retirement feature.
Main remains plaintext. Final catalogue preference persistence, durable complete artifact/key inventory,
uncommitted creation reconciliation, optional recovery, reference-aware retention, coupled OS exclusion
and native/platform/device acceptance remain open. Unchanged UI and upstream producer suites are not
repeated. No app source algorithm is duplicated by the independent native caller.

D-232 terminal verification: the full solution passed 3,400 cases, zero failures/skips (3m11.526s).
Final pre-inspection binding validation and changed-catalogue rejection were then tightened with
one additional regression; all 199 Data-library cases pass (1.955s), including all eight AT-232
retirement scenarios. Other unchanged application/UI scenarios were not repeated for that boundary.
The final independent Windows build has zero warnings/errors (1.76s), and both actual fixture
processes pass again: target identity is retained through failed synchronization, then removed on
cold continuation with exact main binding preserved. Final strict main Windows build has zero
warnings/errors (17.41s). Android/package evidence follows below. This is not final MAUI preference,
complete native key-reference inventory, Android/iOS retirement or main encryption acceptance.

D-232 final strict Android main build has zero warnings/errors (39.36s). The repository now
contains 3,401 test cases; the final changed library's 199 cases were actually rerun. No physical
phone, iOS, complete protected retirement, recovery or product release acceptance is implied.

D-232 complete signed Release artifact: artifacts/android-d232-delivery/pro.vafadar.zanance-Signed.apk
(82,979,376 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores were verified.
The native Windows candidate retains its reviewed SHA-256 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Test outputs were cleaned after terminal builds/tests. Main encryption registration, complete
profile/key-reference lifecycle, physical devices/iOS/store signing and product release remain open.

### D-233 — Immutable native retirement file inventory and handle-bound Windows deletion

The retirement journal now captures a bounded immutable artifact table before catalogue removal:
named private root, direct filename, native volume/file identity, byte length and SHA-256. Missing
inventory is not an empty set. Identical Authorized capture may requalify a failed native flush;
different/later files cannot replace it. Ordered continuation and identity retirement require the
existing table. Existing metadata-only requests remain readable, but cannot perform native cleanup
without the explicitly captured inventory. The operational SQLite schema is validated exactly,
including typed bounded rows, canonical fingerprints and case-insensitive duplicate refusal.

WindowsProfileRetirementFiles captures actual no-follow regular-file handles and preflights the
complete captured set before cleanup. Each deletion reopens and verifies that same native file object,
length and bytes under the host's maintenance ownership, then marks that very handle for deletion on
close. It never resolves another unchecked filename after verification. Redirected/hard-linked or
busy files fail through the existing native handle checks. Same-content replacement is refused by
volume/file identity; changed content is refused by length/hash. Already absent entries may resume
only under the exact durably requalified CatalogueRemoved request. Actual directory synchronization
and absence checks follow. The journal and its retained authority sidecars cannot be deleted.
Native DELETE access and FILE_DISPOSITION_INFO are used as documented by the Windows API:
https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle
This is namespace cleanup, not a physical sector-erasure or power-loss claim.

All 208 Data-library tests pass, including nine new AT-233 cases. Actual Windows tests delete a
synthetic DPAPI generation-key envelope, refuse reopening its missing key without replacement,
preserve a foreign file and journal, refuse changed bytes and same-content replaced file identity
before deleting any captured entry, and reject capturing its own authority for cleanup. Metadata
cases prove immutable reopen, no later additions, explicit missing-list refusal and safe filenames.
The independent Windows two-process caller now stores the actual captured artifact table and invokes
the production native cleanup class, then the real identity retirement operation. Initial injected
flush failure retains the target file/identity; cold continuation deletes only the captured object
and target identity, preserving main. Evidence: artifacts/native-sqlcipher/d233-native-create.log
and d233-native-resume.log; owned fixture location in d233-owned-retirement-path.txt.

The native host still must discover/authenticate the complete profile database, migration/restore,
portable/safety-copy and per-generation key reference set before capture. This class intentionally
never guesses ownership, scans filename patterns or deletes a shared installation wrapping key.
Final app catalogue persistence, complete reference discovery, Android/iOS native deletion,
uncommitted creation reconciliation, optional recovery/retention/OS policy and main registration
remain unfinished. Main remains plaintext. No visible layout changed and unchanged producer/UI
matrices are not repeated. Terminal main/test/package evidence follows below.

D-233 terminal managed verification: all 3,410 solution cases pass, zero failures/skips
(2m53.233s); focused Data suite passes 208/208. The independent final native Windows caller
builds without warnings/errors (2.16s) and its two real retained-state processes pass.
Strict main Windows build has zero warnings/errors (22.38s). No unchanged UI/native producer
suite was repeated. Main registration remains plaintext and final complete reference discovery,
app catalogue/native mobile effects, recovery/retention/OS integration remain open.

D-233 final strict main Android build has zero warnings/errors (40.20s). Native Windows cleanup
is verified in the actual host tests and independent caller; Android/iOS deletion adapters are
not supplied or claimed by this step. The complete-reference discovery requirement is unchanged.

D-233 complete signed Release APK: artifacts/android-d233-delivery/pro.vafadar.zanance-Signed.apk
(83,450,586 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores are verified.
Test outputs were cleaned after terminal checks. Main encryption, full reference discovery,
app/mobile retirement, recovery/retention/OS policy, device/store and product release acceptance
remain unfinished.

### D-234 — Descriptor-relative Android retirement and captured directory identity

AndroidProfileRetirementFiles now captures UID-private native directory/file identities, lengths
and SHA-256 through no-follow directory-relative file descriptors. It preflights the recorded set,
rechecks the anchored namespace against the inspected inode, calls Bionic unlinkat through the
same directory descriptor, fsyncs that directory and verifies absence. Native missing-file errors
are distinguished from access/ownership/type errors; linked, non-regular and foreign-owned files
are refused. Descriptor duplicates are explicitly closed. The operating system's app-data alias
is canonicalized within the app's actual UID-private data scope; redirected final roots are refused.
Native ABI reference: https://android.googlesource.com/platform/bionic/+/main/libc/include/unistd.h
This is not physical erasure or a defence against privileged/same-UID code.

Review identified another namespace issue: an empty replacement directory could otherwise make
all captured files appear absent while the original directory still retained them. The artifact
record now includes the native directory identity, verified before accepting absence on Windows
and Android. The exact inventory table shape changes for this unreleased infrastructure; earlier
independent inventory shapes are refused, never silently rebound to the currently found directory.
Main remains plaintext and has no production retirement metadata to convert. An actual Windows
regression retains the moved original file and rejects cleanup/absence in an empty replacement root.

The ordered requalification/preflight/removal algorithm is shared in ProfileRetirementFileCleanup;
the native adapters supply actual object effects without duplicating it. The independent runtime
fixture is also shared, with platform key/durability/file ports and trim-safe identity serialization.
It creates real main/work identity rows and native generation envelopes, records only the work
file/envelope, injects synchronization failure after catalogue publication, then cold continuation
removes work files/identity while the main native key remains readable. Catalogue publication in
this fixture is still native-flushed fictitious file storage, not final MAUI preference integration.

The signed Release AOT/trimmed Android x64/API36/4 KiB two-process scenario passes. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-21b20f76f70f4030acf58c25a46e5688.json
(run 9d934b3c3c2e4ec7a7af130b72ef9e8a). After adding focused negative scenarios, only that new
single-phase runtime was executed: changed bytes, same-content replacement and emptied replacement
root are refused before unrelated cleanup. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-0ac16a2050d8408289b1d45d71b8ff8b.json.
The existing native lifecycle was not repeated merely to exercise the added negative cases.
Initial and final independent Android builds have zero warnings/errors (69.96s / 55.93s).
Terminal Windows/test/main-build/package evidence follows below.

This provides real native cleanup effects, not complete profile-reference discovery or final
application activation. Actual complete authenticated reference inventory, durable app catalogue
publication, uncommitted creation reconciliation, optional recovery, reference-aware safety retention,
coupled OS policy and final main/native platform gates remain open. Main remains plaintext. No
unchanged UI or upstream producer suite was repeated. Physical arm64/API24/16 KiB OS, iOS, device
and product release acceptance are not supplied by this x64 emulator result.

D-234 terminal checks: all 3,411 solution tests pass, zero failures/skips (2m51.932s), including
the new actual empty-replacement-root regression. Strict main Windows build has zero warnings/
errors (24.83s); the Android main source was already built strictly without warnings/errors
(66.37s) and production code did not change afterward. Final signed packaging follows below.
The final independent Windows build is clean (1.81s); shared changed-object refusals and actual
main/work key-envelope retirement/cold main-key read pass. Evidence: d234-native-windows-create.log
and d234-native-windows-resume.log. The owned fixture path is recorded in d234-owned-retirement-path.txt.
The Android negative runtime run is e51949403ed6465c8b585ab2b03cdb54; its three new scenarios passed
without repeating the already observed Android lifecycle. No unrelated UI/native producer suite
was repeated; final app catalogue/reference discovery and main registration remain open.

D-234 final complete signed Release APK: artifacts/android-d234-delivery/pro.vafadar.zanance-Signed.apk
(83,044,912 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores are verified.
Test outputs were cleaned after terminal checks. Main encryption activation, complete reference
inventory/app catalogue integration, recovery/retention/OS policy and external platform/device/
store acceptance remain unfinished.

### D-235 — Native profile reference discovery before retirement

ProfileRetirementReferenceDiscovery now reads the actual stable identity catalogue and profile-bound
restore ancestry before capturing files. Known initial/new-profile/restore metadata, ciphertext,
rollback/preference capsules and their sidecars are classified from that ancestry. Unknown artifacts
in the target database/key/portable namespace fail closed rather than being guessed or silently left.
All native generation envelopes in the target stable profile namespace are authenticated against
that installation/profile/generation, including orphaned safety keys. Exact native fingerprints are
checked before/after authentication and again before publication; complete directory names and all
identity rows are rechecked under the host's exclusive maintenance validation.

Local portable/legacy safety backups use the actual BackupFileName parser and exact app/profile set.
Opaque VBS safety copies require bounded native reads and full AES-GCM authentication before target
ownership is accepted. Unverified foreign headers never cause foreign key access/deletion; a matching
header alone is insufficient. Owned plaintext package/key buffers are cleared. Retained backup files
and their separate generation envelopes join the immutable inventory. Shared installation wrapping
keys and cloud backups are excluded. This capture never advances retirement or deletes financial files.

The actual native reference-file ports provide bounded direct listings, regular-file fingerprints
and bounded complete/prefix reads. Windows uses existing no-follow native handles; Android uses
its UID-private descriptor-relative file effects. Four AT-235 actual SQLite/Windows tests cover a
pending restore with retained rollback/preferences, owned/foreign safety and portable separation,
unknown generation/malformed backup refusal and matching-header GCM tamper rejection. Initial test
compilation used an incorrect history method name and exposed native-platform warnings in existing
assertion lambdas; corrected to the actual ReserveAsync and explicit callback platform guards,
without suppression. Strict focused tests pass 4/4 with no warnings.

The independent Windows caller uses the retained D-227 real two-profile encrypted model and native
keys, without repeating migration/startup. It captures actual work ciphertext/history/key references
and a fully authenticated owned safety copy, excludes main ciphertext/keys/foreign safety, preserves
the target ciphertext hash and all identity rows, and remains Authorized. Observed seven objects:
artifacts/native-sqlcipher/d235-native-windows-runtime.log. Native build is clean (17.28s at that check).
The Android fixture uses actual original compiled EF model/migrations inside native SQLCipher and
actual native keys/safety/authentication/read effects, not fake controls or a duplicate app algorithm.
No main application registration or actual catalogue preference publication is introduced here.
Terminal platform/runtime/main/test/package evidence follows below.

Final exact app catalogue transaction and startup retirement reconciliation remain required, with
uncommitted creation recovery, optional recovery-key handling, reference-aware safety retention,
coupled OS policy and remaining native/device/platform acceptance. Main remains plaintext. Unknown
or unauthenticated ownership is refused, not repaired by generating replacement keys. No unchanged
UI/upstream producer suite is repeated; complete release/device acceptance is not claimed.

D-235 observed Android failure: first native fixture stopped before EF schema construction with
its encrypted candidate still zero length. The observation deadline expired, but native PID 15286
remained live and idle; no success was inferred and no restart was made solely for timeout. Source
comparison found the missing original AddZananceData registration before compiled-model Instance,
which sets the documented synchronous initialization switch for Mono (EF issue 31751). The probe
was stopped only after that concrete deadlock diagnosis. The fixture now uses the original data
registration and supports explicit recorded discovery continuation; no replacement profile IDs or
existing generation key preparation occurs in that continuation.

Signed Release AOT/trimmed Android x64/API36/4 KiB continuation of the same run
04eeecb451b84485a6e87b792a6daf3b succeeds. Actual encrypted original model/local portable/owned
safety references are captured, main safety is excluded and finance is not deleted. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-006dbb7b9c374e4188a69417743ac94e.json.
The existing target generation envelope SHA-256 is unchanged before/after continuation; retained
identity metadata was reused. Initial/native resume builds are clean (66.14s / 54.30s). Negative
observation evidence remains in d235-native-android-runtime.log; successful continuation in
 d235-native-android-resume-runtime.log. No unrelated migration/UI/producer acceptance was repeated.

All 3,415 solution cases pass with ContinuousIntegrationBuild, zero failures/skips (3m07.120s).
Strict main Windows (21.22s) and Android (50.57s) builds have zero warnings/errors. Corrected native
Windows scope validation build is clean; actual retained encrypted fixture capture was observed.
Final APK/signature/package evidence follows below. Main encryption/captured app-catalogue retirement
and remaining recovery/retention/OS/platform/device acceptance are still open.

D-235 final complete signed Release APK: artifacts/android-d235-delivery/pro.vafadar.zanance-Signed.apk
(83,507,930 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores are verified.
Test outputs were cleaned after terminal checks. The original native model-init deadlock and
failed observation are retained as negative evidence. Final app retirement/startup/recovery,
retention/OS and main encryption registration remain unfinished; phone/iOS/store acceptance is not implied.

### D-236 — Real catalogue retirement composition and startup continuation

Protected profile DeleteReviewedAsync now enters the application's actual runtime exclusive boundary
and drained database owner before invoking an explicitly supplied native retirement host. Without
that host, the permanent legacy boundary still refuses protected deletion. Plaintext deletion keeps
its existing behavior. The host captures the actual stable identity, reviewed inactive target and
exact before/removed device catalogue state, discovers authenticated native references, publishes
the actual atomic profile packet, requires ISettingsPersistence and unchanged readback, then removes
captured files and retires only the exact identity through the existing coordinator.

Catalogue authority stores SHA-256 fingerprints of complete preference inputs, not profile labels
or plaintext replacement packets. The target removal packet is deterministic for its retained
operation ID, preserves every other profile/name and current selection, and rechecks device choice
projection before Set. Request identity and catalogue digests are committed atomically in the staged
initial metadata transaction. A failed native preference persist retains the exact removal for
idempotent persistence retry; no file/identity cleanup follows until its actual barrier is recorded.
An unrelated catalogue change refuses continuation instead of overwriting that state.

Before complete startup profile capture, an explicitly supplied retirement host resumes pending
requests under the same drained owner. Missing inventory can be completed only while the exact
original catalogue still matches an Authorized request; it cannot be inferred as empty after
publication. Repeated Delete before publication finds the same pending request rather than creating
conflicting operations. Completed records are synchronized/read but do not replay historical
catalogue changes. Final completion notifies once for an actual resumed/deleted operation. Native
file/identity/reference checks remain exact; main/current profiles are refused.

Four actual non-UI app-source scenarios use real isolated SQLite catalogue/native Windows key/file
effects: normal inactive retirement, failed preference persistence followed by exact pending resume,
unrelated catalogue mutation refusal and missing-inventory retry without a second operation.
Settings persistence in those contract tests is an explicit effect, not native platform acceptance.
A real SQLite staged-publication test proves initial identity+digest atomicity and immutable retry.
Existing 23 ProfileTests pass. Public RequireDrainedOwner exposes the actual context-disposal guard
instead of weakening host checks. App tests explicitly compile the new production profile sources.
Terminal native/main/test/package evidence follows below.

The independent Android caller uses the actual ProfileService, ProtectedProfileRetirementHost,
protected session, original compiled model, native generation keys/files, MauiSettingsStore and
AndroidSettingsPersistence. A synthetic failure follows actual durable catalogue persistence so a
cold process must reconcile before session publication. It verifies main balance remains 250 and
only main identity remains; no fake MAUI control or copied product algorithm supplies this result.
Main registration remains plaintext until final key/recovery/retention/OS/provider adoption is ready.
Uncommitted creation reconciliation and other recovery/platform/device/release boundaries remain
open; this does not declare SEC or the whole planned scope complete. Unchanged UI/upstream producer
matrices are not repeated.

D-236 terminal evidence: all 3420 tests pass, zero failures/skips (3m12.507s). The strict
Windows build has zero warnings/errors (21.79s). The strict independent Android build
has zero warnings/errors (52.70s). Actual native Android create/cold continuation completed
on the same fictitious run 60623842398d490ba6f67fe015ffcb23: durable catalogue persistence
followed by injected failure retained target files; cold startup completed retirement,
kept main balance 250 and left only main identity. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-850fb67ec6bf4eca98e834b6aee50a25.json.
No unchanged UI or upstream native producer suite was repeated. Main encryption remains inactive.

The strict main Android build also completed with zero warnings/errors (39.69s).

D-236 complete signed Release APK:
artifacts/android-d236-delivery/pro.vafadar.zanance-Signed.apk (83,114,544 bytes).
Signature verification succeeds; package pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores are verified. The script requires a warning-free strict Release publish.
Test outputs were cleaned after all terminal builds/checks. Local test signing and independent
runtime evidence do not imply main encryption, physical phone/iOS, store or product acceptance.

### D-237 — Retain explicit profile creation before native preparation

An explicitly supplied ProtectedProfileCreationHost records a bounded device-only Add request and
requires actual native preference persistence before any identity/key/database preparation. The
request retains the proposed canonical profile id, name, deterministic atomic catalogue packet,
complete before/after preference fingerprints and original private catalogue path. Profile labels
follow the existing device preference privacy, not an unencrypted financial SQLite journal; this
key is not portable and contains no credentials, key bytes, balances or transaction data.

Preparation resumes the same target through the actual native callback. Missing/corrupt intent,
unrelated catalogue changes and changed choice projection refuse preparation/publication rather
than replacing history or generating a second original. Catalogue publication follows verified
preparation and is durably persisted before a completion marker. A retained completed marker only
requalifies/cleans that device request, so later legitimate catalogue changes are never replayed.
Covered startup resumes creation before capturing all profiles and before session publication.
The current financial endpoint and main profile remain unchanged throughout Add.

Cancellation or a native failure after durable Add intent retains that explicit request for exact
startup continuation. The host does not delete unknown old orphan artifacts, infer Add from missing
state or activate main encryption. Historical pre-intent orphans still require separate verified
reconciliation; final recovery, retention, coupled OS exclusion and platform/provider adoption remain
in the full scope. Existing hosts without this composition retain their current creation behavior.

Six application-source contract cases cover persistence interruption before preparation, after
catalogue publication, after completion, unrelated catalogue mutation, corrupt intent, and a native write that restores the original catalogue. Completion requires exact
published-state readback, not merely either allowed pre/post state. Native
settings effects are explicit ports in those cases, not platform acceptance. Actual Android native
continuation and terminal build/test/APK evidence are recorded separately below. Unchanged UI,
upstream native producer suites and unrelated device checks are not repeated.

D-237 verification to date: the complete solution run passes 3425 tests, zero failures/skips
(2m59.021s). After the final publication-readback guard, all six affected creation cases pass
(3.053s), adding one scenario for 3426 covered behaviors. The final Windows build has zero
warnings/errors (15.94s), main Android zero warnings/errors (42.98s). Independent Android
Release native builds were repeated only after relevant source/test-caller changes, not producer
or UI changes. The final caller uses actual ProtectedDatabaseStartup rather than manually duplicating
its ordering. Terminal actual startup/runtime/package evidence follows below.

D-237 final native startup evidence: independent strict Android Release AOT/trim build has zero
warnings/errors (53.31s). The actual ProfileService/native prepare path retained durable Add intent
when interrupted after encrypted schema/key preparation. Cold actual ProtectedDatabaseStartup
resumed that exact request before all-profile bootstrap and session publication; its completed
callback did not repeat resolution. Exactly two identities remain, new profile has no accounts,
main balance remains 250 and a read-only key port forbids replacement preparation on cold resume.
Same fictitious run: 943a0e349933421c8ed9693d3e0610fb. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b60a776f761a4fabab8c6c2e93aa6bba.json.
Earlier direct-host proofs are retained but do not substitute for this actual startup caller.

Complete signed Release APK: artifacts/android-d237-delivery/pro.vafadar.zanance-Signed.apk
(83,577,562 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. Native
runtime proof uses actual device preferences/persistence and fictitious encrypted finance in an
independent package; main application storage is still plaintext. This does not establish physical
phone/iOS, optional key recovery, historical orphan reconciliation, retention/OS or release acceptance.

### D-238 — Standard optional recovery wrapping and encrypted private vault

DatabaseRecoveryMaterial uses platform RSA-3072 with standard OAEP-SHA256. Each payload contains
its recovery id and the existing canonical DatabaseKeyBinding context plus the exact 32-byte DEK.
Public identity can wrap new generations after private material is disposed; private material is
needed only for explicit recovery. No application cipher, KDF or source fork is introduced.
Stored ciphertext has standard DEK-keyed HMAC-SHA256 confirmation over recovery id/context/ciphertext,
so the current native DEK can requalify an immutable existing mapping without the recovery secret.
Unwrap verifies both context and confirmation before returning an owned key. Inputs/exports/returned
keys have explicit ownership; owned private/key payload buffers are cleared and private RSA owners
are disposed. Managed connection-password strings cannot be completely erased.

SqlCipherRecoveryVault stores private PKCS#8 only inside official native SQLCipher keyed by a
caller-owned random 256-bit recovery key. It requires the reviewed cipher version, exact schema/
installation/recovery/public identity, bounded typed rows, keyed schema access, cipher integrity
and SQLite integrity. A staged no-replace metadata publication synchronizes and reopens exact
private material before final acceptance. Wrong/missing keys, plaintext, foreign or damaged vaults
fail without fallback or replacement. The recovery key itself is never persisted or logged.

SqliteRecoveryEnvelopeCatalogue contains only public identity, RSA ciphertext and HMAC confirmation,
with exact typed schema/identity and bounded immutable per-profile/generation mappings. Existing
capture authenticates ciphertext using the actual native DEK and requalifies native synchronization
after a visible commit/flush error. It refuses changed keys/ciphertext rather than overwriting them.
RecoveryGenerationKeyStore wraps every explicitly prepared native key before caller readiness
advances, while ordinary reads still require device keys. It never silently invokes recovery.
This applies to future database/safety generations when the final host supplies the decorator.

Thirteen focused cases verify standard encryption randomness, wrong profile/install/generation/
private identity, corruption/truncation/disposal, weak/noncanonical public keys, immutable SQLite
mapping/ciphertext/HMAC, failed synchronization without a replacement native key, absent authority
and missing/invalid vault keys. The full solution run passes 3438 behaviors before the final added
confirmation-tamper case; final affected suite passes 13 for 3439 covered behaviors. No financial
EF schema changed; operational recovery metadata does not alter the compiled model/migrations.

Independent native callers use the actual 32-migration model, native device envelope store and
SQLCipher current-generation owner. They preserve a negative fictitious opening amount through
cold recovery after deleting only the independently owned device envelope. A fixed synthetic key
exists only in these independent callers, never the product, a file or output. This tests recovery
material access, not a finished product recovery UI or durable device-key re-enrollment.

Native Windows actual recovery succeeds with unchanged financial ciphertext; wrong recovery key
and damaged private-vault copy fail authentication. Expected native HMAC failure diagnostics contain
page metadata, not keys. Android initially returned ENOENT after the intended fake-envelope removal;
the shared check failed to recognize that platform exception. Its explicit Android callback now
accepts only native ENOENT, not arbitrary exceptions. The original fixture is retained and resumed;
no replacement creation/key/schema is used to hide the failure. Terminal Android/build/APK evidence
follows below. An intermediate namespace qualification build error is retained separately.

Main storage remains plaintext. Final optional recovery enable/confirm UI, durable public/private
registration, native re-enrollment after actual wrapping-key loss, complete retained-reference/retirement
integration, key rotation, coupled OS exclusion and platform/device acceptance remain unfinished.
The vault/protocol is not registered by default; no PIN/device-authentication recovery is bypassed.
Zanance backup recovery and optional portable passwords remain unchanged. Owner OD-04/OD-10 stay
approved and do not require another decision. Unchanged UI and native producer suites are not repeated.

Primary API references: https://www.zetetic.net/sqlcipher/sqlcipher-api/ and
https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsaencryptionpadding.oaepsha256?view=net-10.0.

D-238 terminal evidence: independent strict Windows/Android callers have zero warnings/errors.
Main strict Windows completes in 19.94s, Android in 65.11s, both zero warnings/errors. Full
3438-test run has zero failures/skips (3m31.146s); final 13 affected tests pass (3.295s), including
the added confirmation-tamper scenario, for 3439 covered behaviors. The Android corrected caller
build completes in 62.11s with zero warnings/errors; the original namespace/error recognition
failures are retained, not counted as acceptance.

Windows cold reopen reuses the originally prepared fixture with its missing device envelope and
verifies both wrong recovery key and damaged private-vault copy refusal, actual signed money and
unchanged financial ciphertext. Android resumes the exact failed ENOENT fixture
b8c6121b68604d0692ba13a6a75afb5c without creation or key preparation. Its proof intentionally records
Created=null, not an invented successful initial phase. Cold existing-vault recovery succeeds,
wrong key/damaged vault fail, actual 32-migration finance and -321 opening balance remain, full
financial ciphertext hash is unchanged and no replacement device envelope is created. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-cfd01fd1aab141d9a6b3668efe90ed75.json.
Original expected-native-missing negative stack: d238-android-missing-native-error.log. Windows
final negative/positive log: d238-native-windows-final-reopen.log. Main encryption/recovery UX,
native re-enrollment after wrapping-key loss and full retention/retirement/OS adoption remain open.

D-238 complete signed Release APK: artifacts/android-d238-delivery/pro.vafadar.zanance-Signed.apk
(83,167,792 bytes). Signature verification passes; pro.vafadar.zanance package, API 24/36 and both
arm64-v8a/x86_64 embedded assembly stores are verified. Test output was cleaned after all terminal
checks. This APK does not activate the recovery host or main database encryption and uses local
test signing, not store release acceptance.

### D-239 — Retire captured per-profile recovery mappings before native key removal

ProtectedProfileRetirementHost accepts an explicitly configured recovery catalogue. While Authorized,
it authenticates each target recovery mapping with the actual retained native DEK and records a bounded
immutable recovery-id/count/SHA-256 reference fingerprint in the existing retirement journal. This
contains no private material or plaintext key. Strict journal validation permits only the exact new
optional schema; unknown tables, malformed/missing captures and another recovery identity fail.
Retries requalify an existing Authorized capture before publishing catalogue removal. Missing recovery
capture cannot be inferred as empty after publication, and a host lacking recorded recovery effects
cannot continue a recovery-bearing request.

After actual catalogue persistence, the native host removes only the captured target profile mappings
before deleting its captured device envelopes/files. The recovery transaction rechecks target bytes,
refuses new/changed target generations, preserves every other profile's complete ciphertext/confirmation
rows, synchronizes and reopens actual removal. A delete visible after failed flush can requalify under
the same retained request without replacing keys or touching other profiles. Empty captures remain
explicit authority. Artifact and identity retirement both require actual recovery target absence and
the original recovery identity; the shared encrypted private vault is retained for other profiles.

Five actual SQLite/recovery cases cover exact target removal with other-profile recovery, failure
after delete followed by native requalification, added target generation refusal, wrong native key
capture refusal, and missing/wrong-phase/wrong-request cleanup refusal. One actual ProfileService
flow uses native Windows DPAPI/files and proves target recovery removal before identity retirement.
Existing four profile-retirement application cases still pass. Full/native/build/APK evidence follows.
No financial model or EF migration changed. Main registration remains plaintext; final recovery UX,
native re-enrollment/rotation, broader retained-reference/retention policy and coupled OS/platform
adoption remain in the full scope. Unchanged upstream native suites/UI are not repeated.

D-239 terminal test/native evidence: all 3445 solution tests pass, zero failures/skips
(2m48.472s). The separate strict Android Release AOT/trim build has zero warnings/errors
(55.62s). Actual ProfileService/native settings interruption followed by cold retirement succeeds
on the same fictitious run d367dc82b37c4c1299e987a33e379cfb. Target recovery mappings and identity
are retired, main balance remains 250, and the retained shared encrypted vault still recovers
main's exact key. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-5316e6f94cdc45a8b1af58468e1dd032.json.
This confirms actual Android catalogue/key/file/recovery composition, not product recovery UI,
physical phone/iOS or main encryption activation. Final main build/package evidence follows below.

D-239 main strict builds: Windows zero warnings/errors in 18.87s, Android zero warnings/errors
in 39.16s. No changed screen or upstream native producer required another layout/producer run.

D-239 complete signed Release APK: artifacts/android-d239-delivery/pro.vafadar.zanance-Signed.apk
(83,639,002 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. Local test
signing and independent recovery retirement proof do not activate main encryption or constitute
physical phone/iOS, recovery UX or store/release acceptance.

### D-240 — Explicit native re-enrollment of verified recovered keys

IDatabaseGenerationKeyRecovery requires an owned 32-byte recovered DEK and an explicit host callback
that verifies the same existing financial generation before any key entry is published. The host
remains responsible for app/device authorization and drained access. Native callers exercise actual
SqlCipherNewProfile/current-generation verification with the exact owned candidate key, not an
invented schema or a permissive financial check.

Windows stores CurrentUser DPAPI recovery material in a separate immutable .key.recovered entry;
Android stores a version-2 recovery envelope under a fresh non-exportable Keystore alias, with the
version/purpose/wrapping identity and existing DatabaseKeyBinding context authenticated by native
AES-GCM. Old aliases and original envelopes remain untouched, including unavailable originals.
The separate entry is staged, authenticated against the verified DEK, synchronized, renamed without
replacement and reopened before acceptance. An existing entry must match, with no replacement; a
staged entry can only resume through explicit recovery. Owned key copies are cleared. Reads choose
an existing recovery entry, fail on its corruption and never silently fall back or invoke recovery.
Ordinary preparation cannot regenerate a binding with a pending recovered stage.

Four native Windows contract cases verify original-envelope preservation and cold read, no writes
before failed finance verification, refusal of a different replacement key, and damaged recovery
entry refusal without fallback. The actual independent original-model caller runs create/key-loss,
verified import and a separate native cold read that does not use the recovery secret, preserving
signed finance and complete ciphertext. Android uses the same financial verification with actual
Keystore/fsync effects; terminal evidence follows below.

This is native re-enrollment, not a complete product recovery UI or authorization controller. Final
all-profile recovery orchestration, interrupted malformed-stage reconciliation, wrapping-alias
lifecycle for future new generations after invalidation, recovered-entry reference/retirement and
retention integration remain open. Strict reference discovery still refuses unqualified recovered
namespace artifacts before catalogue removal, rather than guessing ownership of an unavailable
original. Main registration stays plaintext; no temporary OS-backup policy, new permission or SDK.
Unchanged UI/native producer suites are not repeated. OD-04/OD-10 remain approved.

D-240 terminal native/test evidence: all 3449 solution tests pass, zero failures/skips
(2m54.750s). Final independent Android strict Release AOT/trim build has zero warnings/errors
(62.01s). Three native Windows processes create, import and reopen without using the recovery
secret; signed money and complete ciphertext remain unchanged. Three actual Android processes
pass the same flow using the fresh version-2 Keystore recovery envelope on fictitious run
7c24641d9fac425381bce1af3621ce25. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-2817166d0a484a5bab73f02ef924cca1.json.
Four Windows native contract cases also pass. The initial strict test compile exposed platform
annotations across assertion lambdas; the assertions now call the platform-neutral ports after
native Windows construction is explicitly guarded, without disabling diagnostics. Main Windows
strict build is zero warnings/errors (17.70s); final main Android/package evidence follows.
No full app recovery UI, alias invalidation matrix, hardware power-loss/device/iOS or main
registration acceptance is claimed by these native controlled tests.

D-240 main Android strict build completes with zero warnings/errors in 39.62s.

D-240 complete signed Release APK: artifacts/android-d240-delivery/pro.vafadar.zanance-Signed.apk
(83,216,944 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. The APK
uses local test signing and does not activate main encryption/recovery UI or establish phone/iOS/
hardware power-loss/store acceptance. Recovered-entry reference/lifecycle integration remains open.

### D-241 — Authenticated native recovered-key references and retirement

KeyRecoveryReceipt records exact original/recovered native file identity, root identity, length and
SHA-256 under standard DEK-keyed HMAC-SHA256 and the existing profile/install/generation context.
It contains no plaintext key. The original object may explicitly be absent; missing receipt is never
inferred as empty authority. Same-content file/root replacement and changed bytes invalidate the
confirmation. A staged recovered object is normalized only to its final bound filename, retaining
its actual inode/file identity through no-replace rename.

Native import now publishes/resumes an immutable bounded receipt after actual finance verification
and recovered-envelope authentication, before final recovered-key publication. Native private file
creation and file/directory synchronization qualify the receipt. Existing and cold recovered reads
require exact authenticated receipt/current objects, without fallback or manufactured confirmation.
Explicit re-import can requalify the existing exact verified objects; ordinary reads never create
authority. Windows uses real owner-only native files; Android uses actual app-private no-follow
capture/fsync and the existing fresh wrapping alias. Original unavailable envelopes remain immutable.

ProfileRetirementReferenceDiscovery recognizes only exact recovered entry/receipt names, verifies
receipt HMAC and exact native originals/recovered objects with the actual DEK, then captures each
file for the existing native removal algorithm. Unknown or uncommitted staged recovery namespaces
still fail before catalogue publication. After catalogue removal the existing host removes captured
public recovery mappings, all original/recovered/receipt objects and finally the exact identity.
Other profiles and their shared private vault remain. RecoveryGenerationKeyStore forwards explicit
native import only after the same finance verification, then requalifies recovery mapping before
native publication; no public recovery write follows rejected finance verification.

Two new native Windows cases prove same-content original replacement refusal and missing receipt
refusal without fallback. Actual ProfileService source additionally discovers/removes original,
recovered entry and receipt alongside target recovery mappings. Existing native recovery/profile
cases pass. Independent actual Android finance/import/cold retirement evidence follows below.
No financial model/migration or visible UI changed. Main registration stays plaintext; final recovery
UX/all-profile orchestration, alias invalidation/future generation lifecycle, malformed-stage repair,
retention/OS/provider/platform acceptance remain in the full scope. No temporary OS policy or new SDK.

D-241 terminal verification: all 3452 solution tests pass, zero failures/skips (2m54.985s).
Six native Windows key/receipt cases and six actual app retirement cases pass. Final independent
Android strict Release AOT/trim build has zero warnings/errors (70.21s). The actual original
ProfileService/native recovery decorator imports the work profile's DEK after existing encrypted
finance verification, retains original/recovered/receipt objects during injected native preference
failure, and completes their captured cold retirement on the same fictitious run
40526ade78094cf3bcda86a7983ad21e. Main balance 250, its native identity and actual recovery through
the shared private vault remain. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-e36f0b4cd43349f4b243acfeca472b6a.json.
The first compile exposed a local name collision; final source uses a distinct original-key reference.
Unchanged producer suites/UI were not repeated. Main/recovery UX/all-profile/alias lifecycle/
retention/OS/platform gates remain open. Final main build/package evidence follows below.

D-241 strict main builds complete with zero warnings/errors: Windows 17.76s, Android 39.04s.

D-241 complete signed Release APK: artifacts/android-d241-delivery/pro.vafadar.zanance-Signed.apk
(83,671,770 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs cleaned after all terminal checks. The local
signed APK does not activate main encryption/recovery UI or establish phone/iOS/store acceptance.

### D-242 — Preflight complete captured-profile recovery before native writes

ProtectedProfileRecovery captures the actual original profile catalogue independently of commercial
selection and requires the unpublished covered startup endpoint plus its real drained database owner.
Stable native identities must match every captured legacy profile and the existing recovery
installation. All canonical recovery bindings are read from strict bounded metadata; unknown profiles
and a missing original-profile mapping fail without inferring a fresh database or empty authority.

An explicit native unlock port opens the existing private vault with the caller-owned recovery key.
Each DEK is unwrapped/confirmed and passed to the host's complete actual reference verifier before
the first native import. A failed profile preflight therefore writes no native recovered entry.
All retained key buffers are bounded by the existing 4096-binding inventory and cleared in finally;
private material is disposed. Before/within each import and after the batch, catalogue, identities,
bindings and unpublished/drained access are rechecked. Later legitimate profile edits are preserved,
not overwritten. Native imports remain immutable and verify their owned copies against existing
references. Recovery does not publish a session, modify profiles/preferences or complete financial
operations; only normal protected startup runs after the verified native batch.

Four actual application-source cases with real isolated SQLite/RSA metadata prove full-profile
preflight before first write, second-profile failure with zero imports, missing mapping refusal before
unlock, and catalogue mutation refusal without name overwrite. Native unlock/reference/import effects
are explicit ports in these cases, not fake MAUI controls or copied product algorithms.

The independent Android caller uses actual native preferences/persistence, two encrypted original
32-migration profiles, automatic/public recovery capture and actual ProtectedProfileRecovery. It
removes only its fictitious native envelope files, verifies/imports all original profile keys and then
runs actual ProtectedDatabaseStartup before real switching/financial reads. Terminal evidence follows.
Initial strict fixture compilation exposed startup-call ordering before local declarations; the
final caller invokes startup only after the owned recovery lease is released.

Main registration remains plaintext. This composes captured recovery and startup prerequisites but
does not finish product recovery/authentication UI, real alias invalidation/hardware acceptance,
complete concrete retained safety/preference/abandoned-reference verification, retained-file policy,
OS exclusion or platform/iOS/provider acceptance. The final host must verify every supplied referenced
binding; the fixture has current initial generations only and does not prove a full retention corpus.
No schema/model/permission/export/new SDK changes; unchanged UI/native producer suites are not repeated.

D-242 terminal evidence: all 3456 solution tests pass, zero failures/skips (2m55.483s), including
four actual application recovery cases. Final independent Android strict Release AOT/trim build
has zero warnings/errors (81.99s). The same fictitious two-profile fixture
4535123ccf8f4f2c8db30f79f4ea1f87 removes both native envelope files and, in a cold process, runs
actual ProtectedProfileRecovery/native vault/import plus actual ProtectedDatabaseStartup and
real switching. Original main/work balances remain 250/880 and no extra profile is created.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-b977a43500c74666b1e93a0e3afe068b.json.
The fixture proves current initial native generations; full retained safety/preferences/abandoned
reference and real alias-loss corpus remain separate unfinished host acceptance. Final Windows
strict build has zero warnings/errors (19.42s). Main encryption/recovery UI remain inactive;
final Android/package evidence follows below. Unchanged native producer/UI checks were not repeated.

D-242 main strict Android build has zero warnings/errors (38.31s).

D-242 delivery APK: artifacts/android-d242-delivery/pro.vafadar.zanance-Signed.apk, 83,257,904 bytes. Signature verification passed; package pro.vafadar.zanance, API 24/36, arm64-v8a/x86_64 and both complete assembly stores were checked. Local test signing is not store or physical-device acceptance. Test output cleanup passed with zero warnings/errors. Main protection remains inactive.

### D-243 — Recover after actual independent Android wrapping-alias loss

The independent Android native caller now has a WrappingKeyLoss scenario. It removes only the
Keystore alias for its freshly created fictitious installation in the distinct compatibility
package. It preserves both original encrypted key envelopes and requires actual native reads
of both bound DEKs to fail. It changes no global/device settings or product security data.

A cold process then runs the actual ProtectedProfileRecovery and protected startup compositions
from D-242, including native recovered-envelope/receipt publication and real profile switching.
Both original envelope SHA-256 values remain unchanged, the unavailable installation alias is
not recreated, no extra profile appears, and main/work balances remain 250/880. This proves actual
alias absence rather than merely deleted envelope files. It does not establish hardware-backed
invalidation, biometric change behavior, future-generation alias lifecycle or physical-device acceptance.

Verified independent strict Release AOT/trim build: zero warnings/errors, 55.90s. Actual two-phase
runtime proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-679ead1e4b7f46e699024d045d8a8a6d.json,
run c12bdc1cb2d04abf88b7f9cf93593c31. Script parsing and diff checks passed. No application source,
provider registration, UI, permission, financial schema or APK changed. Therefore unrelated solution
suites, main builds, UI matrices and native producer suites were not repeated. The complete signed
D-242 delivery APK remains the current product package; main storage remains plaintext. Final
recovery/authentication UI, complete retained-reference verification, lifecycle/retention, coupled
OS backup exclusion and platform/provider/release acceptance remain open.

### D-244 — Independent Android wrapping aliases for new generations

New native generation preparation no longer recreates the installation wrapping alias. Every new
DEK is wrapped by a fresh non-exportable Android Keystore AES-256-GCM alias. The existing version-2
header authenticates purpose 0 for ordinary generations and purpose 1 for explicit receipt-bound
recovery, along with the complete profile/installation/generation context. Reads accept immutable
legacy version-1 entries and new purpose-0 entries; recovered reads still require purpose 1 and their
native receipt. Ordinary and recovered entries cannot substitute for one another. Bounded native
reads accept only exact authenticated formats up to 82 bytes, not a truncated legacy packet.

The independent caller deliberately prepares legacy installation-bound envelopes for both fictitious
profiles, removes their actual installation alias, verifies refused original reads, recovers through
the actual application/native startup, then creates a new independent generation without recreating
the old alias. A third cold process reads that generation using existing native storage only and
reopens a SQLCipher packet containing Int64.MinValue; recovery-purpose confusion is refused.
This is lifecycle creation/reopen evidence, not final reference-aware alias retirement or hardware acceptance.

The initial fixture compile exposed local-name collisions; final strict native Release AOT/trim build
passed with zero warnings/errors (64.51s). The first runtime observation of the third process expired.
The same process subsequently returned successful completion, read without restart or regeneration:
artifacts/native-sqlcipher/d244-same-process-future-proof.json. Negative observation evidence remains
in d244-alias-runtime.log. Earlier phases passed the runner's actual assertions before its third
launch; no synthesized Created/Reopened report is supplied. The runner now makes one final read of
the same completion target before a non-barrier deadline failure; it never restarts to hide expiry.
Script parsing passed. All 3456 solution tests passed, zero failures/skips (2m53.266s).

Main protection registration remains inactive. No UI/schema/account/portable-password/permission
change. Complete retained-reference validation, recovery/authentication UI, malformed staging,
reference-aware key/alias retention, final provider/OS adoption and physical platform/iOS/provider
acceptance remain open. Unchanged UI and upstream native producer suites were not repeated.

D-244 strict main builds passed with zero warnings/errors: Windows 17.10s and Android 38.62s.

D-244 complete signed Release APK: artifacts/android-d244-delivery/pro.vafadar.zanance-Signed.apk, 83,696,346 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both embedded assembly stores verified. Solution test outputs cleaned successfully after terminal checks. Local test signing is not phone/iOS/store acceptance; main protection registration stays inactive.

### D-245 — Authenticate retained native references before recovery import

ProfileRecoveryReferenceVerifier supplies the concrete read-only reference check for captured native
recovery. It reads actual stable identities and strict restore ancestry, rejects unknown database
namespace entries and missing selected candidates, and requires selected restored journals plus
retained before-restore/preference references. Existing candidate, initial rollback, before-restore
and preference databases are reopened with the owned recovered DEK, reviewed cipher version,
cipher_integrity_check and full integrity_check. Preference capsules also pass their actual typed,
bound allowlisted ReadAsync validation; no settings replay occurs during key verification.

Matching device safety references require complete AES-GCM authentication, not only an untrusted
header. Foreign headers never cause foreign native key access. Safety-only generations can prove
authenticated retained reference without pretending to be financial database generations. Keys
with no authenticated reference fail. All decoded package/key buffers are cleared; native file/root
identities, lengths, fingerprints, SQLite sidecars, namespace listings, identities and ancestry are
rechecked after validation. Caller-owned drained access and actual private-root effects remain mandatory.

Five native Windows/private-file cases pass: genuine safety-only reference, tampered content refusal,
foreign reference refusal, unknown namespace refusal and missing selected generation refusal. These
cases use actual SQLite metadata and native file capture, not a copied verification algorithm.

The independent signed Release Android caller now uses this verifier in actual ProtectedProfileRecovery.
It creates two original encrypted profiles, two initial encrypted rollbacks, two real typed encrypted
preference capsules and two authenticated safety envelopes with separate captured recovery keys. All
four fixture device envelopes are removed. A cold process verifies every binding before native
imports and then runs actual covered startup/switching. Main/work balances remain 250/880; no extra
profile is created. Native file inventory confirms both capsules/rollbacks and safety references.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-6196395354e94738857ce12275c2ac91.json,
run 66f9c0a865d041b68b450403a753785b; inventory: artifacts/native-sqlcipher/d245-native-reference-inventory.log.
The safety fixture proves authenticated codec references, not ZIP manifest/restore acceptance. Its
preference capsules exercise the existing capsule format on initial candidates; a full multi-restore
history/pending-restoration corpus and staged/orphan reconciliation remain separate acceptance work.

All 3461 solution tests pass with zero failures/skips (2m55.599s). Final independent Android strict
Release AOT/trim build has zero warnings/errors (67.48s); Windows strict main build is clean (17.17s).
No UI/schema/permission or portable backup behavior changed. Main registration remains plaintext.
Final recovery UI/authentication, complete restore/crash corpus, reference-aware generation/alias
retention, final provider/adoption/OS policy and platform/provider/device gates remain open. Unchanged
UI matrices and upstream native producer suites were not repeated.

D-245 strict main Android build passed with zero warnings/errors (39.69s).

D-245 complete signed Release APK: artifacts/android-d245-delivery/pro.vafadar.zanance-Signed.apk, 83,262,000 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned successfully after terminal checks. Local signing is not store/phone/iOS acceptance; main protection remains inactive.

### D-246 — Require committed recovery candidates before history mirroring

Review of the real journal/history interruption boundary found a concrete omission in D-245:
required candidate presence followed only the history Selected flag. Verification/selection can
already have committed in the restore journal when history mirroring has not run. The verifier now
requires an actual candidate whenever the journal reached CandidateVerified or Selected/Completed,
independently of that history flag. An authenticated safety reference cannot replace the required
financial candidate. Existing selected rollback/preference requirements remain unchanged.

Two additional actual SQLite/native-file cases exercise CandidateVerified and Selected while history
remains unselected. Each requires the exact missing-candidate refusal before other reference reads,
without inventing data or changing the journal/history. The Selected fixture's intentionally unread
placeholder files isolate this ordering gate; they are not evidence of ciphertext/financial validation.
Seven focused cases pass. All 3463 solution tests pass, zero failures/skips (2m56.718s).

The independent signed Release AOT/trim Android caller executes the same original verifier with
real native private roots and real strict metadata for both interrupted phases. Both refuse missing
candidates, preserve journal/history and create no replacement data/key. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-363f2a58a6bb4a6a99ee8e25041a3305.json,
run 61aa8a42f371446692208249ebb47aa9. Final strict native build has zero warnings/errors (58.82s).
The independent entry point explicitly selects the reviewed native provider before metadata access.
Script parsing and diff checks pass. Unchanged positive financial/UI/producer matrices were not repeated.

This fixes a real missing-reference boundary; it does not complete the multi-restored financial corpus,
malformed/staged/orphan reconciliation, final recovery UI, reference-aware retention or main provider/
OS adoption. No schema/model/permission/UI change. Main storage registration remains plaintext.
Physical devices, iOS, external providers and release acceptance remain separate gates.

D-246 strict main builds have zero warnings/errors: Windows 17.06s and Android 38.94s.

D-246 complete signed Release APK: artifacts/android-d246-delivery/pro.vafadar.zanance-Signed.apk, 83,704,538 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not store/phone/iOS acceptance; main protection remains inactive.

### D-247 — Cold recovery of a real interrupted two-restore chain

The independent Android caller now creates the actual 32-migration model, writes initial account
balance 250, and restores two complete portable packages through the actual BackupService and
SqlCipherPackageRestoreReceiver. The source databases are in memory; the original exporter creates
bounded portable snapshots without plaintext disk staging. The first package restores balance 730.
The second restores balance 990 and deliberately fails after actual native preference persistence,
while its journal is Selected and history remains pending. All three captured device envelopes are
removed; original/candidate/rollback/preference files and private/public recovery material remain.

A cold process uses actual ProtectedProfileRecovery and ProfileRecoveryReferenceVerifier to preflight
and re-enroll every retained key before opening current storage. Actual OpenCurrentAsync resumes the
selected second restore without the portable package or old current connection, persists/replays the
native preference value and completes the same three-row ancestry. Actual compiled-model reads
confirm candidate balances 250/730/990 and before-restore balances 250/730. All retained ciphertext,
rollback and typed preference capsules pass native authentication/integrity/reference verification.
The source-only backup and unused profile-transition ports do not duplicate application algorithms.
This probe does not implement or bypass production UI/device authentication.

Initial cold recovery correctly refused stale independent-package profile metadata: fixture setup
removed profiles.catalogue rather than the real profiles.work.catalogue. Setup now clears the actual
fictitious catalogue. An explicit recorded-fixture continuation verifies the one-profile identity and
three-row history, refuses non-fictitious extra profiles, and repairs only that independent package's
stale catalogue before actual recovery. Existing generations/keys were not rebuilt. Negative runtime
and continuation evidence remain in d247-native-runtime.log and d247-native-continuation.log.
Continuation proof has Created=null, not invented creation evidence:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-39cbf89ea73f4a21b71d998352fecd12.json,
run 5e99e9f6a10944f0a286301b37d86733.

After the fixture correction, one fresh complete two-process scenario passes. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-89a5cc941e1842debb53350fb37f12d9.json.
Final independent strict Release AOT/trim build has zero warnings/errors (54.69s). Script parsing and
diff checks pass. Product source, financial model, UI, permissions, native producer binaries and main
APK are unchanged from D-246. Therefore unchanged 3463-test baseline, main builds/UI matrices and
producer suites were not repeated. The complete verified signed D-246 product APK remains current.

This verifies the real multi-restore/pending-replay recovery corpus. It does not complete malformed
staging/orphan reconciliation, generation/key/alias retention, final recovery/authentication UI,
main provider/OS adoption, physical-device/iOS/provider acceptance or publication. Main remains plaintext.

### D-248 — Match Android key reads to native file ownership requirements

AndroidDatabaseKeyEnvelopeStore now rejects non-regular, multiply linked and foreign-UID file objects
through the actual opened descriptor's fstat before reading/unwrapping. This matches the existing
native retirement/reference boundary instead of accepting a key object that cannot safely qualify for
later cleanup. No key format, crypto algorithm, profile data, native producer binary or UI changed.

The independent original key-store caller verifies creation, cold encrypted reopen and malformed/
missing/directory/symlink/no-overwrite/staged-file cases. Its additional hard-link case reports whether
the read-refusal branch was actually exercised. On this emulator, native link returned EACCES; the
initial negative phase stopped. The fixture now recognizes only EPERM/EACCES as explicit OS link
creation restrictions, not successful coverage of the link-count guard. Its first pattern form used
nonconstant Android errno properties and failed compilation; ordinary equality fixed that harness
compile issue. Both negative runtime/compile logs are retained.

The same fictitious key run fcf6548964f64553a8a8ba41af5a4979 resumes only the negative phase with the
existing key/encrypted fixture; no fresh DEK or creation/reopen replay. Proof:
artifacts/native-sqlcipher/build/android-d/android-key-proof-fcf6548964f64553a8a8ba41af5a4979.json.
HardLinkReadRefusalExercised=false is explicit. The original positive phases passed before the
OS restriction; resumed proof contains the negative phase only and does not invent those results.
Foreign-UID and hard-link read rejection still need an environment capable of creating such objects.
Final independent strict Release AOT/trim build is clean (57.56s). Harness parsing and diff checks pass.
All 3463 solution tests pass, zero failures/skips (2m54.568s); strict Windows main build is clean (23.24s).

Main registration remains plaintext. This read-boundary alignment does not finish malformed-stage/
orphan cleanup, reference-aware file/key/alias retention, final recovery UI/provider/OS adoption or
physical-device/iOS/provider/release acceptance. No unchanged UI or upstream producer suite repeated.

D-248 strict main Android build has zero warnings/errors (40.62s).

D-248 complete signed Release APK: artifacts/android-d248-delivery/pro.vafadar.zanance-Signed.apk, 83,266,096 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-249 — Continue verified recovery past an interrupted receipt stage

Explicit native recovery verifies the owned DEK against retained finance before receipt preparation.
When an existing uncommitted receipt stage has different bounded bytes, KeyRecoveryReceiptStore now
preserves it and creates a separate unique no-replace receipt stage. The new stage is read back,
synchronized and renamed without replacing final authority. Published receipts still require the
same exact native object/MAC and are never overwritten. Correct legacy stages still resume normally.
This does not infer ownership of or delete malformed objects, repair oversized/unsafe files or replace
a published receipt. Cipher-key staging itself remains under its existing stricter continuation.

One new actual Windows DPAPI/private-file case preserves torn stage bytes, publishes final authority,
cold-reads the same DEK and leaves no new unique receipt stage. Seven focused cases pass. The first
assertion used a Windows wildcard that also matched the retained legacy stage; an exact ordinal
predicate fixed the test, then the required collection assertion was aligned with the strict analyzer.
Original negative test/compile logs are retained; no cryptographic/provider code was changed for them.
All 3464 solution tests pass, zero failures/skips (2m48.082s).

The independent signed Release AOT/trim Android caller puts a three-byte interrupted receipt stage
beside each of its four removed fixture envelopes. Actual all-profile recovery authenticates native
references, imports the same keys, publishes valid receipts and opens original main/work balances
250/880 through covered startup and real switching. All four old stages remain with the SHA-256 of
bytes 1/2/3, verified from the same native private root after success. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-d60c0cd29ac9426d9008a60df6307368.json,
run cf79bce63f8546939f2686cebd92a45a; retained stages: artifacts/native-sqlcipher/d249-retained-stage-proof.log.
Independent strict native build is clean (59.44s); strict Windows main build is clean (21.99s).

Preserved rejected staging still needs reference-aware reconciliation before retirement; this is
functional explicit recovery continuation, not finished cleanup/retention. No schema/model/UI/permission
change. Main protection remains inactive; final recovery UI/provider/OS adoption and platform/device/
provider/release acceptance stay open. Unchanged UI and upstream producer suites were not repeated.

D-249 strict main Android build is clean (39.64s), zero warnings/errors.

D-249 complete signed Release APK: artifacts/android-d249-delivery/pro.vafadar.zanance-Signed.apk, 83,704,538 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-250 — Retire bounded temporary receipts under authenticated whole-profile authority

Native profile reference discovery recognizes only the exact legacy receipt-stage name and the
canonical lowercase nonempty GUID attempt suffix introduced in D-249. These are bounded temporary
receipt metadata, never key or selector authority. A reviewed whole-profile retirement may capture
them only after the actual final recovered key and its original/recovered native-object receipt pass
authentication. Oversized stages, invalid names, missing final recovered authority and staged key
envelopes remain refused. No age-based guess or directory-wide deletion is introduced.

Each permitted stage enters the existing immutable native-object inventory with its root/file ids,
length and full fingerprint. The existing durable catalogue-removal barrier and all-object preflight
still precede exact native deletion. Replaced/changed stages cannot be silently removed. This allows
receipt remnants preserved by successful explicit recovery to leave with their reviewed profile,
without giving those remnants permission to open financial data or authorizing automatic key cleanup.

Three actual application-source/native Windows cases cover removal of bounded legacy/unique stages,
refusal without a published recovered entry and refusal of oversized metadata. Nine focused cases
pass; all 3467 solution tests pass, zero failures/skips (2m53.771s).
The independent signed Release AOT/trim Android caller creates both receipt-stage forms, performs
actual finance-verified key recovery, captures real profile retirement and fails after actual native
catalogue persistence. Cold actual host continuation removes only the captured target artifacts and
recovery/identity mappings. Main balance 250 and its recovery material remain. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-150ac54a41354b7093e18671608572ad.json,
run 21bea656d24e4c618f8fc38f0dc53772. Strict native build is clean (66.92s).

This closes reviewed profile retirement of bounded receipt-only remnants. It does not classify or
remove malformed key stages, unknown artifacts, historic pre-intent orphans or files still referenced
by recovery/safety/history. Final automatic safety/generation/key/alias retention and main recovery/UI/
provider/OS adoption remain open, as do physical-device/iOS/provider/release gates. No schema/model/
permission/UI change; main registration remains plaintext. Unchanged UI/producer suites not repeated.

D-250 strict main builds have zero warnings/errors: Windows 17.78s and Android 91.82s.

D-250 complete signed Release APK: artifacts/android-d250-delivery/pro.vafadar.zanance-Signed.apk, 83,266,096 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-251 — Let persistence hosts select their native SQLite engine

Shared Vafadar.Data now references Microsoft.EntityFrameworkCore.Sqlite.Core instead of the package
that transitively selects a default native engine. The current standard SQLite bundle is explicit in
the Zanance app and the ordinary isolated test host. The Data project's design-time bundle has
PrivateAssets=all, so EF context tooling still runs without leaking its engine into application hosts.
The existing resolved bundle version 2.1.12 is pinned centrally; this is dependency ownership
separation, not an upgrade, new cipher bundle or approval of candidate/native production adoption.

Actual restored dependency graphs show only SQLitePCLRaw.core in shared Data. The app's referenced
Zanance.Data node depends only on Vafadar.Data/Core, not the design-time native bundle. The current
app explicitly retains its original standard engine. This makes final reviewed source-built provider
selection possible at the app boundary instead of importing two default/native choices from shared
persistence. Managed bridge/core compatibility still needs qualification when final cipher adoption
changes the host's packages; isolated source-compilation evidence is not that library binary proof.

All 3467 solution tests pass, zero failures/skips (2m48.982s), including actual isolated SQLite flows.
Actual dotnet ef dbcontext info with the Data project as startup succeeds without MAUI; it reports
the original context/provider and design-time datasource, without schema update or model generation.
Windows/Android strict main builds pass, zero warnings/errors (27.46s/60.67s). Dependency graph,
EF execution and test evidence are retained under artifacts/native-sqlcipher/d251-*.
No schema/model, UI, financial behavior, permission or key lifecycle change. Unchanged native producer
and layout matrices were not repeated. Main still uses standard plaintext SQLite; final host/recovery/
retention/OS adoption and native/platform/provider/device gates remain open.

D-251 complete signed Release APK: artifacts/android-d251-delivery/pro.vafadar.zanance-Signed.apk, 83,692,250 bytes. Signature, pro.vafadar.zanance package, API 24/36 and both complete arm64-v8a/x86_64 assembly stores verified. Both embedded standard libe_sqlite3.so hashes equal D-250 exactly. Test outputs cleaned after terminal checks. No UI change/layout replay or native candidate adoption; local signing is not phone/iOS/store acceptance.

### D-252 — Qualify actual persistence assemblies with the reviewed cipher bridge

Two independent binary hosts now reference the actual Vafadar.Zanance.Data project (and its
Vafadar.Data assembly) instead of compiling infrastructure/model/migration source copies. They
explicitly select SQLitePCLRaw.provider.sqlcipher 3.0.5 with its core 3.0.5 and reviewed native slices.
Restored graphs contain no default bundle/e_sqlite3. Runtime checks require the actual production
assembly names, not merged caller-source types. No friend-assembly grant or production probe reference.

Through public original APIs, each host creates encrypted storage with actual native keys and all
32 migrations, publishes the protected factory, writes a fictitious negative EUR account, exports
through the actual binary Zanance backup source and reopens the bounded portable snapshot in memory.
A fresh process opens the same native storage with a key port forbidding creation. The actual model/
negative money and binary raw snapshot APIs remain correct; a wrong DEK is refused. This qualifies
managed binary compatibility, rather than extrapolating prior same-assembly source proofs.

Windows strict host builds are clean (final shared-entry build 1.09s). Initial refactoring exposed
CA1416 in deferred native callbacks; explicit platform checks fixed the entry point without analyzer
suppression. The old output invoked after that failed build is retained as negative evidence, not
final acceptance. The corrected build and same-fixture reopen passed; the repeatable Windows harness
also passes creation/cold reopen under artifacts/native-sqlcipher/binary-host/40f965990678413cbb09246fe6581cd9.

Android is a distinct pro.vafadar.zanance.binarycompatibility package using the actual .NET assemblies,
original native effects, complete signed Release AOT/trim and two exact manifest-qualified native slices.
The first build is clean (55.90s). The application behavior completed successfully but the harness
could not read its sandbox because the independent manifest lacked debuggable=true. The process
was confirmed live and its exact result observed in package PID output. Only this independent package
was made inspectable/no-OS-backup; no product/device setting changed. Its existing creation result was
read after install-r preserved data, then only the same fixture's cold phase continued. No database
or key regeneration was used to hide this observation failure. Final manifest build is clean (9.99s).
Creation evidence: artifacts/native-sqlcipher/d252-preserved-create-result.json. Cold proof:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-39a41a98321941e5aab2cf2016f86afa.json,
run 5f3e7ed4ac08435090c499fd3bfec805; negative observation log remains d252-android-binary-runtime.log.
The driver now refuses an uninspectable sandbox before launch and requires stopped ownership before
cold launch. It validates embedded native hashes and refuses default engine leakage; script parsers pass.

Product source/behavior, schema, UI, permissions and APK remain D-251. The 3467-test baseline, main
builds/UI matrices and unchanged upstream producer suites were not repeated. Current signed product
APK remains artifacts/android-d251-delivery/pro.vafadar.zanance-Signed.apk. This closes this binary
bridge prerequisite on tested Windows x64 and Android x64 API36/4KiB; it does not validate arm64,
API24/16KiB OS, iOS, every key/recovery/crash route or whole-app adoption. Final host/recovery UI,
reference-aware retention/reconciliation, native warning review and coupled OS/platform/device/provider
acceptance remain open. Main registration is still plaintext.

### D-253 — Refuse plaintext contexts before protected startup publication

Protected data registration now accepts protectionRequired from location construction through
AddLocalDatabase and AddZananceData. The immutable original path remains available to maintenance
bootstrap, but ordinary/owned synchronous/asynchronous factory calls refuse before the first verified
protected publication. No directory, plaintext database or backup snapshot is created. Failed calls
release their access leases; plaintext MoveTo and captured plaintext rollback are also refused.
The flag is host authority, never inferred from missing keys/files or offered as a user setting.
Existing standard hosts keep their explicit current registration; main encryption is not activated.

Five new factory/location cases cover early refusal, no-write behavior and subsequent drained ownership.
The actual production-assembly cipher hosts now register protectionRequired and check pre-publication
refusal before their existing encrypted creation/cold reopen, typed money and portable snapshot checks.
Windows native runtime passes at artifacts/native-sqlcipher/binary-host/66f872a9b3bf4a8a83766e8810ee008c.
An initial test expectation used a mixed-separator path; canonical comparison corrected that fixture
assertion. No producer suite, unchanged UI matrix or platform security setting was repeated.

Final host/provider activation, optional recovery UI, reference-aware retention/reconciliation and
coupled OS exclusion remain open. OD-04 and OD-10 are already approved; this slice needs no new owner
decision. Native platform/device/provider/release acceptance remain separate from local verification.

D-253 runtime: strict actual-binary Android Release build has zero warnings/errors (1m1.63s).
Both fresh creation and cold same-key reopen pass with pre-publication refusal enforced:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-a97d94f80a684d3f85d7117741ecf846.json,
run 0eb40c84818342eaa7492b409fe889b8. All 3472 solution tests pass with zero failures/skips
(2m56.792s), including the five added boundary cases. Existing main registration remains plaintext.

D-253 delivery: strict main Windows build 26.89s and Android build 40.34s have zero warnings/errors.
Complete signed Release APK: artifacts/android-d253-delivery/pro.vafadar.zanance-Signed.apk,
83,249,712 bytes; signature, pro.vafadar.zanance package, API24/36 and both embedded assembly stores
verified. Test output cleanup completed successfully after terminal verification. Main provider remains
standard SQLite; this APK is not a whole-app encryption, physical-device, iOS or release acceptance.

### D-254 — Compose the protected application profile lifecycle

ProtectedProfileStorage now owns one SqlCipherProfileSession for covered all-profile startup,
current-generation resolution, switching and reviewed new-profile preparation. Construction is lazy
and requires the D-253 protected registration boundary; it performs no database/key/profile work.
The host supplies anchored native directory/key/durability effects and an owner-bound restore receiver.
ProtectedDatabaseStartup passes its actual drained lease to that receiver during cold bootstrap.
No obsolete process callback or missing metadata can choose an installation/restore generation.

The resolver reads actual identities/history and validates the retained profile-to-original mapping.
Its key owner's validation outlives the maintenance lease and permits legitimate later catalogue
Add/rename while refusing removal/redirection of that profile. New preparation requires the actual
bounded reviewed Add intent and canonical matching id/path before any identity/key effect. Existing
published-but-unfinished creation can resume its same identity. An independent unpublished protected
location runs the real compiled model/migrations in memory without moving the active endpoint.

Four original application-source cases cover lazy construction/plaintext-registration refusal,
missing reviewed intent and mismatched id/path with unchanged intent/metadata. The native Android
creation-continuation case now uses this actual host for preparation, cold startup and switching,
instead of a fixture-owned resolver/schema implementation. Native production activation and the
final receiver/recovery UI/retention/OS/platform acceptance remain unfinished; main remains plaintext.
No unchanged producer suite or visible UI matrix was repeated.

D-254 terminal baseline: all 3476 solution tests pass, zero failures/skips (3m4.576s). Strict main
Windows 23.21s and Android 41.19s have zero warnings/errors. Signed Release AOT Android composition
passes interrupted preparation, cold reviewed creation completion, exactly two identities, empty
new profile, main balance 250 and switching back through the shared host. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-76db958872af45289ad147a189e575c4.json,
run 865c036f665b4fdcae503257b800c050. Final creation host native build 65.54s is clean.

The cold restored-session qualification now uses the original ProtectedProfileStorage/owned startup
receiver rather than a fixture resolver. The old D-229 Android run 0b6bd586086d46a793e0f4e4ee0b6039
was absent from the current sandbox: DirectoryNotFoundException before host entry is retained in
artifacts/native-sqlcipher/d254-cold-restored-runtime.log. It is not a successful host test or a key
replacement. A fresh independently owned restore fixture is required for this current qualification;
unchanged general tests/main builds are not repeated. The driver permits an explicit cold-only
recorded restore run; it never chooses missing-key generation or financial fallback. Script parser passes.

Complete signed Release product APK: artifacts/android-d254-delivery/pro.vafadar.zanance-Signed.apk,
83,716,826 bytes; signature, pro.vafadar.zanance package, API24/36 and both complete assembly stores
verified. Test outputs cleaned successfully after terminal managed/build checks. Main remains
plaintext: native library adoption/receiver registration/recovery UI/retention/OS/platform/device and
release acceptance remain separate and unfinished. No new owner decision is required for this host.

The fresh retained restore case exposed a real host callback mismatch: SqlCipherProfileBootstrap
passes legacy profile ids, whereas the composed native receiver takes the exact original path.
ProtectedDatabaseStartup now maps the id through its unchanged complete ProfileStorageCapture before
passing the drained host owner. It never uses current selection or a filename guess. The failing
fresh dataset/run 70edbddd953f4a8ebca26e50c60fafb1 and its negative log d254-restored-runtime.log
are retained; only its cold restored-session phase resumes after this fix, without regenerating
backup, finance, identities or keys. Earlier baseline build/test results preceded this production
fix; final affected verification is recorded below rather than treating those old results as final.

D-254 final post-fix verification: the same retained run 70edbddd953f4a8ebca26e50c60fafb1 cold-opens
through actual composed startup with only existing keys, balance 321 and later fr preference preserved.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-512dc37bb32c48b096c1f8db7abc2a7b.json;
Created/Restored are explicitly null because those earlier effects were not repeated. Final Native
Release AOT build 84.50s and strict main Windows 35.02s/Android 60.31s have zero warnings/errors.
All 3476 solution tests pass, zero failures/skips (3m12.025s). The current original receiver path was
verified only with already selected/applied restore state; pending pre-selection and unapplied native
preferences remain receiver-integration work, not acceptance inferred from this cold read.

D-254 final signed product APK replaces the earlier baseline at the same delivery path:
artifacts/android-d254-delivery/pro.vafadar.zanance-Signed.apk, 83,278,384 bytes. Signature, package,
API24/36 and both embedded assembly stores were reverified after the callback fix. Final test-output
cleanup completed successfully. Main encryption is still inactive; this is the final APK for this
host-composition slice, not device/iOS/provider/retention/OS/release acceptance.

### D-255 — Compose retained restore rollback and native preference replay

SqlCipherRetainedRestore constructs an existing-record receiver from the captured stable identity,
original namespace and latest restore reference. Before selection it verifies the actual prior
selected ancestry, opens only its existing keyed ReadWrite/unpooled ciphertext, checks native version,
full cipher authentication and SQLite integrity, and registers captured access for bounded rollback
export. Missing/redirected prior storage, missing/wrong keys or foreign history refuse; no plaintext
connection, history initialization or replacement-key preparation is a fallback. Owned key buffers
are cleared. Already selected journals do not open the previous financial generation.

ProtectedProfileStorage can compose this receiver with the actual settings store/persistence port,
portable allowlist, real compiled-model migration and same-session restore adoption. Bootstrap now
passes its actual DatabaseProfileIdentity as well as original path/owner, without synchronous metadata
reads in the receiver builder. Explicit PrepareRestoreAsync requires a settled active session and
its exact selected history endpoint before recording/synchronizing the new intent/journal. Missing
package/capsule before staging remains a recovery UI/retry requirement, never inferred completion.
RequireStable refuses a new request during tentative switching or failed publication.

Resume checks recorded history before reading the preference capsule/key. Five added real metadata
cases refuse invalid/unrecorded references before key effects. All 3481 solution tests pass with zero
failures/skips (3m3.151s). Strict Windows 22.21s and Android 39.08s have zero warnings/errors. No schema,
permission or visible layout changed; unchanged layout/producer matrices were not repeated.

Two signed Release AOT Android x64/API36/4KiB scenarios use the actual composed application host,
actual BackupService and Android keys/durability/preferences/persistence. One interrupts before prior
key read/selection; the other after actual native preference persistence but before replay marking.
Cold processes continue the same recorded generations with existing-only key ports, restore balance
321, persist portable preferences once, retain a scoped device choice and make a completed startup
callback idempotent. Each hot fixture prepared exactly two keys; neither cold process prepared one.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-a348d8ec40184eaeba0b66a548f635b9.json,
runs a3fe7db634254e1c98b205d9a75f9e76 and 04388ff64c29463c9e9f5b41d20a8e58.
Final native build 72.83s is clean. This is real fault-port/cold-process evidence, not arbitrary
power-loss, whole UI/provider or physical-device/iOS acceptance. Main registration stays plaintext;
final native adoption/restore UI/recovery-key UI/retention/reconciliation/coupled OS policy remain open.

D-255 signed delivery: artifacts/android-d255-delivery/pro.vafadar.zanance-Signed.apk, 83,737,306 bytes.
Signature, pro.vafadar.zanance package, API24/36 and both complete assembly stores verified. Test outputs
cleaned after all managed/native/build processes were terminal. Main still uses standard SQLite;
this APK does not claim main encryption, phone/iOS/provider, full retention/OS or product release acceptance.

### D-256 — Deliver validated backup packages without premature restore intent

ProtectedBackupRestoreReceiver now connects BackupService's validated whole-package callback to the
actual protected host under its drained application lease. Construction/preview/password/ZIP/checksum
failure cannot reserve a financial restore intent. The host first checks supported sources/bounds,
opens the complete portable database read-only in bounded memory, requires a known ordered application
migration prefix and parses the allowlisted portable preferences without writing a capsule. Only after
that preflight does it reserve the existing-generation restore and invoke the original receiver.
Owned preflight buffers are cleared. No fallback to legacy sequential sources is introduced.

This is integrity/history/JSON preflight, not a claim that a forged matching migration history proves
every table's complete application semantics. Actual candidate migrations/full integrity checks remain
required. A later staging/persistence failure still needs the recorded recovery route; this change does
not abandon a committed intent or permit navigation past an unresolved selected generation.

The added actual app-source case verifies refusal before startup and release of the adapter's exclusive
lease with no metadata. All 3482 solution tests pass, zero failures/skips (3m0.101s); strict Windows
46.07s and Android40.48s builds are warning/error-free. No visible UI change or unchanged producer/layout
matrix replay. Native validation also exercises the actual whole-package receiver, not a fake MAUI UI.

Initial independent native compile failed with CS0136 in three fixture locals; explicit names corrected
only the fixture. The first runtime correctly rejected its invalid SQLite source, but the fixture caught
IOException without InvalidDataException. Its negative log d256-native-runtime.log and unchanged existing
run 18916586e9e9424f97020fd261bff73f are retained. The catch was corrected; that run's original database/
keys were not reset. Its earlier valid portable package existed only in memory and was cleared on failure,
so a distinct fresh fictitious dataset is required to complete positive delivery/cold continuation.
Main native registration and full restore/recovery UI/retention/reconciliation/OS adoption remain open.

D-256 terminal native qualification: original BackupService plus ProtectedBackupRestoreReceiver/host
reject damaged ZIP, valid-checksum invalid SQLite and valid-database malformed preferences before
adding a restore-history row or preparing another database generation key. Existing balance777 and
de preference remain. A valid package then records one intent, retains interrupted pre-selection
state and cold-restores balance321/en exactly once with existing keys and preserved scoped device
choice. This exercises actual native Android x64/API36/4KiB Release AOT/trim and ordinary app source.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-ee31c9650b864f1fbe7566b4d916ecba.json,
run73b022e4bc534359a84d8c8f4ade4c6b. The corrected native build is clean (57.23s). Old failure evidence
and its independent fixture remain. No main source changed after the passed 3482-test/strict-build
baseline; general tests/builds were not repeated for this fixture-only catch correction. Safety-copy
creation/keys belong to the separate UI consent path; this proof concerns financial-generation intent
and key effects, not a claim that every UI safety effect is absent. Main native registration remains
inactive; final UI/recovery/retention/OS and platform/device/release acceptance are still unfinished.

D-256 signed delivery: artifacts/android-d256-delivery/pro.vafadar.zanance-Signed.apk,83,315,248 bytes.
Signature, pro.vafadar.zanance package, API24/36 and both complete assembly stores verified. All managed,
native and build processes completed before successful test-output cleanup. Main remains plaintext;
this APK does not claim whole-app encryption, phone/iOS/provider/retention/OS or product release acceptance.

### D-257 — Qualify source-built Android package consumption

Android candidate packaging now includes framework-specific buildTransitive assets for bundled
component notices and source provenance. Native slices remain standard runtimes/android-arm64/native
and runtimes/android-x64/native assets; the Android SDK imports these itself. The actual production-
assembly caller now references Vafadar.Native.SqlCipher.Android rather than loose build-directory
AndroidNativeLibrary items. Package version4.19.0-candidate.3 is centrally pinned; managed bridge3.0.5
and official source-built native bytes remain unchanged. Candidate status is not changed to release
acceptance. No package is published to a remote feed.

The repeatable caller packs into a fresh owned output, populates an immutable local feed only when
its version/hash matches, and restores with explicitly named native-package.nuget.config. Exact
source mapping confines this package name to the local feed; other dependencies use the established
public source. The config's explicit name avoids changing implicit restore configuration for other
native/repository projects. Binary builds additionally require TreatWarningsAsErrors; APK inspection
requires both original ABI hashes, packaged notices/provenance and no default e_sqlite3 leakage.

The first edit damaged a LF project's closing XML while removing CRLF-counted text; restore refused
before native execution. The exact project was repaired, retaining d257-package-consumer.log. Candidate.2
then restored and ran correctly but explicit AndroidNativeLibrary items duplicated SDK runtime assets,
producing two XA4301 warnings. Original runtime/build evidence is retained, not clean-build acceptance.
Those duplicate items were removed from package targets and immutable version bumped to candidate.3;
no warning was suppressed or package cache replaced.

Final strict signed Release AOT/trim caller build has zero warnings/errors (50.72s). Only the cold
phase of the same fictitious run331ce8fb80634664ab4e32ae4e716d84 continues through the new package;
creation/database/native keys were not reset. Actual production persistence assemblies, 32 migrations,
negative money, binary portable snapshot and wrong-key refusal pass. Proof:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-b1c72b1eee9241c7bc79453ed129afae.json.
Earlier creation/runtime proof remains binary-runtime-proof-8999f5611cc04175a5e8ef4d3892ab5f.json.

Two packages of the final version are byte-identical, hash
a31d841fe87e14ed93d678b8d0b94baa72dffabfd5dae17949192416427aa1d8; both native slices exactly match
reviewed candidate files. Explicit renamed-config restore and script parsers pass. The actual main
asset graph contains the standard bundle and no Vafadar native cipher package. Main source/UI/engine/
APK remain D-256, with its3482-test baseline; no unrelated tests/main builds/APK or unchanged producer
suite was repeated. This qualifies package consumption on observed Android x64/API36/4KiB only;
arm64/API24/16KiB OS/iOS, native warning review, main registration/recovery/retention/OS/device/release
acceptance remain open. The full goal and SEC scope are not complete.

### D-258 — Bound Windows secure-heap list-index narrowing

The current unsuppressed Windows inventory contains749 OpenSSL occurrences, not direct SQLCipher
warnings.20 C4244/C4267 sites in official crypto/mem_sec.c narrow only list/slist indices into int
(or the sh_getlist return into int). Under valid allocator state their interval is0..59 on the
reviewed Windows x64 ABI; this is expression/range review, not whole allocator security acceptance.

SH_LIST has two x64 pointers, so effective power-of-two minsize is at least16. Accepted arena_size
is a representable positive power of two, at most2^63; minsize cannot exceed it, and sh_init rejects
bittables smaller than one byte. For arena=2^a and min=2^m, ratio=2^(a-m), bittable=2*ratio and
freelist_size=(a-m)+1. Thus k=a-m<=59. sh_malloc starts list at k, only decreases it, checks negative
list/slist before helper calls and splits back toward the bounded requested list. sh_getlist starts
at k and shifts an initial ratio<=bit<2*ratio until a valid allocated ancestor, returning0..k.
sh_free coalesces toward the root; its root buddy is bit0, never set by valid positive-node setbit
calls, so no valid coalescing step decrements below0. sh_actual_size consumes the same bounded return.
This reasoning assumes intact allocator metadata and valid pointers; it does not establish safety
for arbitrary corrupted pointers, pointer arithmetic generally or all OpenSSL entry points.

Review-WindowsSecureHeapRanges.py pins the exact official source SHA256, Windows/OpenSSL commit,
matching native DLL and all20 original occurrence lines/codes, then checks1711 admitted power-of-two
parameter classes. It refuses incomplete coverage rather than writing acceptance. It modifies no
source/binary/compiler flag or suppression and never opens product storage. Positive proof:
artifacts/native-sqlcipher/d258-accepted-secure-heap-ranges.json. Incomplete inventory is refused with
no output acceptance file. The earlier conservative-class artifact is retained; the final calculation
excludes tiny rejected bittables. Source hash:
2406f86ebc633aa7cdf27ab9617a8a49b6ec238e6d9dd3792b749cf7257e5d5e.
Native hash remains3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.

This bounds20 narrowing occurrences only:723 C4244/C4267 remain unreviewed in addition to the6
previously reviewed atomic/shift occurrences. All original749 compiler diagnostics remain present;
no warning-free native build, full Windows adoption or main encryption is claimed. No code/UI/APK,
main suite or unchanged native producer suite was repeated. Main remains D-256/plaintext with its
3482-test baseline. Full security/OS/platform/device/release and original goal scope remain open.

### D-259 — Triage legacy AES conversion sites without blanket approval

All16 C4267 occurrences in official OpenSSL crypto/evp/e_aes.c belong to GCM/TLS, CCM/TLS,
AES Wrap or OCB handlers (lines2960,3007,3164,3565,3573,3606,3612,3631,3641,3721,3731,3738,
4025,4041,4086,4093). The exact SQLCipher OpenSSL source chooses EVP_aes_256_cbc and calls
EVP_CipherInit_ex/Update/Final; these warning lines are not its CBC handler. That direct binding
observation is not proof that all compiled OpenSSL entry points are unreachable or safe.

Do not approve these conversions merely because EVP update inputs use int. The legacy padded-Wrap
output-length expression at3731 rounds input then adds8. At input2147483640 (itself within signed
int range), ((inlen+7)/8)*8+8 is2147483648, outside Windows int. This is a source-expression
counterexample to a blanket range proof, not a reproduced public-API vulnerability or exploit:
actual provider dispatch, output-query rules, error propagation and contextual bounds still matter.
EVP's ordinary block-update overflow guard is after the custom-cipher branch, so that later guard
cannot by itself qualify all custom handlers. OCB partial-buffer additions have local block-size
bounds, but this slice does not separately close them or infer whole handler safety.

Immutable source hashes and all16 original occurrences are captured in
artifacts/native-sqlcipher/d259-aes-warning-triage.json. e_aes.c hash:
02b2fdbd8b105c55ac78805806c027f60fe5d42603a706fc5741d177a8f3ae40;
SQLCipher provider hash:a429d5466fa3fe7bb7608c3f79e8e19ff5706b78f6251b44530171dc30e53f69.
No upstream patch, warning suppression, binary rebuild, large-buffer allocation or user-data access
was made. All16 remain unapproved inside the723 remaining narrowing occurrences. Further adoption
must qualify actual reachable/provider input constraints or an officially supported reduced build
profile, not equate unused-at-the-direct-call-site with full native acceptance. Windows/main encryption
remain unactivated. Main code/APK and3482-test baseline remain D-256; unchanged producer, UI, main
build/test/APK checks were not repeated. Full goal/security/platform/release scope remains open.

### D-260 — Reduce the Windows crypto build with supported upstream options

The Windows recipe now reads windows.crypto.profile.json and records its SHA256 with the actual
Configure options. Only the explicitly reviewed no-dsa/no-dh/no-cms/no-cmp/no-ocsp/no-ts/no-quic
options are allowed; arbitrary no-* removals and duplicate options are refused. The required SQLCipher
AES-CBC, SHA/HMAC, PBKDF2 and random primitives remain enabled. No source fork, patch or additional
warning suppression is introduced. Recovery RSA remains the platform implementation; native RSA
is not removed. An initial no-rsa Configure option was rejected by upstream before any native output;
that negative run is preserved and the supported profile used a fresh independent tree.

The initial supported run windows-storage-d260-supported produced SQLCipher DLL
SHA256 e9541bf30523f0c65126525c816d58863ffb0c8811d3509fed08f353af705ed9,4940800 bytes.
Actual configdata confirms all eight removals and no required primitive disabled. The original
production assemblies passed create/cold reopen, all32 migrations, original negative money,
portable memory snapshot and wrong-key refusal. Binary proof directory:
artifacts/native-sqlcipher/binary-host/d1f6b3bca79e4295af7037342df26008.
The new native build emits646 warnings (570 C4267,72 C4244,2 C4319,2 C4133), versus749 in the prior
profile. These are a new inventory, not transferred warning acceptance or a warning-free build.
Profile/configuration evidence: artifacts/native-sqlcipher/d260-profile-proof.json.

The first supported profile failed the official suite:347 files/2219 reported tests, failing
80-test_ssl_new.t compressed-certificate configuration and90-test_sslapi.t group configuration.
Official test source requires positive default supported groups; compressed-certificate fixtures
require TLS1.3 success. Removing both EC and DH left that protocol without usable groups.
No failed recipe is excluded or source-patched. The revised profile retains EC and removes only
DSA,DH,CMS,CMP,OCSP,TS,QUIC. Fresh run windows-storage-d260-ec keeps old objects/results separate.
Revised native output is5641216 bytes, SHA256
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21. Actual configdata confirms EC
retained and all seven requested removals. All704 diagnostics are OpenSSL:628 C4267,72 C4244,
2 C4319,2 C4133; they are not suppressed or blanket-approved. The actual production-assembly
create/cold proof is artifacts/native-sqlcipher/binary-host/3a4d29d9ace8421d80b22a20dfe91bce,
with zero managed build warnings/errors. The source-based schema/tamper/integrity suite also
passes32 migrations; its expected negative HMAC errors are retained.

Windows packaging now requires the current crypto-profile hash as well as source/native identity,
and uses immutable4.19.0-candidate.2. The old profile is refused before packaging. Package native
bytes, complete manifest and bundled component notices match; package SHA256:
7cfac1cc7ed8bb5b187a75049ceee51fce95ce9541413ffe723cd3e81950a5c6.
The revised official OpenSSL suite passes:347 files/3473 reported tests; 114 whole recipes
are skipped and 775 explicit skip markers remain. Both previously failing TLS recipes now pass,
including test_configuration_of_groups. Native DLL hash remains unchanged by the suite.
Full log:artifacts/native-sqlcipher/build/windows-storage-d260-ec/upstream-f17bbb9c64f2407a9ffaff830f24107a.log.
This qualifies the tested configuration; it is not full vulnerability, main encryption or release acceptance.

Main registration remains inactive/plaintext. Main code,3482-test baseline and signed APK remain
D-256; unrelated main builds/tests/UI/APK and unchanged Android producers were not repeated.
Final provider/bootstrap/recovery/retention/reconciliation and coupled OS exclusion, remaining native
platform/device acceptance and the original full goal remain unfinished.

### D-261 — Consume the Windows cipher through its immutable native package

The existing actual-assembly Windows host now restores Vafadar.Native.SqlCipher.Windows
4.19.0-candidate.3 from the explicitly mapped local feed, with win-x64 RID. Manual DLL content and
NativeSqlCipherPath are removed. SDK runtime resolution supplies the native library; buildTransitive
targets preserve exact manifest/component notices in build and publish output, without duplicate DLL
items. Pack-Windows accepts a fresh owned PackageName directory and preserves each package proof.
Two packages are byte-identical:SHA256 a33d8539973950671b88417feb36130e64fda502ef09b7450be00cb82fd3d8fe.
The local feed refuses different bytes at an existing immutable version.

Explicit restore and strict Release build pass with zero warnings/errors; actual dependency graph
contains the source-built package/provider3.0.5 and no standard native engine. Existing32 migrations,
negative money, portable memory snapshot and wrong-key rejection pass in create/cold processes.
Proof:artifacts/native-sqlcipher/binary-host/ae271a43b8984c5c8eced8afcbdad0d9. Native DLL, manifest and
notices match the package byte-for-byte. The strengthened notice check was run on the existing output,
without replaying financial scenarios. No native producer/upstream/main/UI/APK suite was repeated.
Main remains plaintext/D-256; final application host/recovery/retention/OS and external gates remain open.

### D-262 — Move legacy database naming under covered maintenance ownership

DatabasePath now resolves a name only; DI construction no longer renames finance.db or its sidecars.
The actual LocalDatabaseStartup acquires the original factory's drained exclusive lease, then performs
the existing D-24 naming upgrade and migrates through the maintenance-owned context. A configured
protected session or protection-required unpublished location refuses this plaintext host before any
rename. Destination sidecar conflicts and retained protected legacy identities are preflighted before
any family member moves. This fixes ordering/ownership; it is not a new crash-safe rename journal or
final cipher bootstrap, and interruption/power-loss acceptance is not inferred.

Five added cases exercise actual startup source: a held real context prevents all rename work,
release permits migration with the original negative balance/account, retry remains usable, and
conflicting/protected states preserve files. Focused8 and all3487 tests pass with no skips. Strict
Windows32.77s and Android62.29s builds have zero errors/warnings. Existing native startup route passes
en/fa/de loading/failure/retry and off-window storage; complete review rows remain unchanged. Six
own-window captures:artifacts/d262-startup-native. The snapshot script's prior development files were
returned; newly generated review files/marker are retained separately in artifacts/d262-generated-startup-data.
The marker is removed from restored original data so later reviews cannot mistake it for new sample data.
Complete signed Release APK:artifacts/android-d262-delivery/pro.vafadar.zanance-Signed.apk,83765978 bytes.
Signature v2/v3, permanent package pro.vafadar.zanance, API24/36 and nonempty arm64/x64 assembly stores
are verified. Local test signing is not store signing, phone/iOS or release acceptance.
No unchanged SQLCipher/native producer/upstream suite or unrelated UI matrix was repeated. Main remains
plaintext; final encrypted host/recovery/retention/OS/platform integration and the full goal remain open.

### D-263 — Adopt protected storage across the original Android application

Deliver the owner's approved Android encryption work as one complete application batch. Official
SQLCipher is selected before MAUI construction; covered lazy startup owns legacy migration and all
profile publication. The original profile, backup and safety contracts share one native owner. Missing
protected authority never creates a replacement key/profile or plaintext fallback. Windows/iOS main
adoption remains separate.

Integrate automatic per-generation native keys, optional acknowledged recorded recovery, retained-vault
enrollment continuation and independent PIN authorization. Bounded package/schema/preferences validation
precedes recovery effects. Activation preserves old financial bytes, retains hash-bound native receipts
and replays preferences once. Session consumers follow the current protected owner after root replacement.
Capture immutable authenticated file/public-map/alias retirement authority before catalogue publication;
recheck current, archived, recovered and staged roots and preserve financial ancestry/unrelated references.
Final OD-10 exclusions are coupled to the encrypted Android runtime; new devices use Zanance backup.
Portable passwords remain optional and exact component notices do not grant rights to owner source.

Observed native results include original startup/lifecycle/restore paths, retained old data, key-loss
recovery, retained-vault Save retry, process interruption at retirement/activation and exact enrolled
safety expiration. The final permanent-package Release retains the account and encrypted financial/
rollback/private-vault bytes through cold opening. Corrected 18 native contexts cover en/fa/de,
light/dark and 360/412/wide widths, original enrollment commands, key-loss cover and actual picker
cancellation without recovery/financial writes. Exact held envelopes and financial hashes are retained.

Late review found an empty enrolled-code/disabled-Save surface and long native captions that violated
D-80. The former is hidden when enrolled; existing growing actions preserve commands, scaling and
complete names for startup, Settings and file selection. Narrow German targets now grow to 58px with
two complete native text lines. No temporary review guard remains in production source.

A strict restore then rejected five October13 Microsoft advisories affecting transitive Xml/Pkcs10.0.11
from existing PDF libraries. Their standalone consumers explicitly reference central 10.0.13; audit is
not suppressed. Core-only framework metadata selects10.0.13 while preserving Android platform 36.1.69.
Actual package/Mono payloads are verified. Installed SDK/workload configuration was not changed.
All 3509 final tests passed, zero failures/skips(3m04.254s), outputs cleaned. Strict Windows passed17.50s
with zero errors/warnings; complete canonical Android Release succeeded. Final APK 88213830 bytes,
SHA256 1f58420b304788fec0dbca7982128de3221b55a76d22fa9aa9d20a85f18c03b5; permanent package, v2/v3, API24/36, no debug, both cipher payloads, assembly
stores and packaged OS exclusions checked. [Full evidence](quality/android-protected-storage.md).

Earlier failed fixtures/observations remain historical; successful cases do not erase them. Repeat
verification was limited to actual changes/failures, including security servicing and corrected actions.
No unchanged producer/upstream suite or unrelated successful app checks were repeated. Native process
termination is not hardware power-loss/all-crash-points; phone/arm64/API24/16KiB/OEM/spoken accessibility,
fresh hosted CI, Windows/iOS, real OAuth, Store signing and publication remain open. This batch does not
complete the continuous-development objective.
Sources: [XML advisory](https://github.com/advisories/GHSA-c399-9853-6jmj),
[PKCS advisory](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).

Final About review observed a stale claim that language/digits were excluded from backups. The
actual six-key portable source includes language/calendar/holiday region/week start/regional format/
digit shapes; it excludes theme/PIN/device security/credentials. The description was corrected in all
six languages through the resource script. Only affected About views were rechecked after this copy
change, preserving the successful unchanged recovery/runtime/backend evidence. Final signed copy APK
and strict Windows build passed; complete artifact/hash and scoped UI results are in the quality record.

### D-264 — Adopt protected Windows application storage

Windows composes the original protected startup/profile/backup/recovery algorithms with CurrentUser DPAPI and the qualified official SQLCipher package. Shared algorithms retain Android native effects; Windows never removes OS DPAPI master keys. Native root binding handles observed namespace virtualization without allowing root rebinding. Retirement preserves canonical authority filenames through SQLite's built-in long-path VFS. The actual independent Windows application verifies recovery enrollment/key-loss/cold reopen, profile isolation, authenticated safety retention, portable replacement and inactive retirement. [Evidence and limits](quality/windows-protected-storage.md). Final local checks and complete APK are recorded below; iOS/device/provider/Store gates are not closed.

D-264 final local verification: 3521 tests passed, zero failures/skips (3m45.293s); strict Windows permanent identity 34.63s and Android 65.70s, zero warnings/errors. Canonical complete signed APK and scoped upgrade/cold/Settings runtime passed. Runtime/API/native hashes and retained negative findings are recorded in the Windows quality record. No unchanged upstream/native producer corpus was repeated. Hosted fresh compiler reproduction remains a post-push gate.

### D-265 — Complete scaled holding actions and stop retired Home publication

Holding detail/type/change actions retain commands and native scaling through the existing growing surface. History quantity and valuation date use separate rows; Undo grows separately. Original semantic hints reach the native command owner. Eighteen three-page contexts and eighteen separate bottom contexts verify names, targets, original row identities and unchanged typed storage on an independent marked installation. An actual COM failure exposed late Home publication into disconnected controls; actual appearing Home instances register with their Shell and invalidate awaited publication before root disconnection. A native blocked-SQLite-read regression verifies zero retired balance changes and working replacement Home. [Evidence and limits](quality/holdings-readable.md). No financial algorithm, permission, schema or commercial activation changes.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.

D-266 final domain/application suite: 3,526 passed, zero failures/skips, 3m24.408s. Permanent-identity strict Windows build: zero warnings/errors, 23.49s. No unchanged cipher producer or upstream corpus was repeated.

Final D-266 signed Release upgrade/cold Home passed on the owned emulator; test outputs cleaned. APK signature/payload and the open Home renderer finding are recorded in docs/quality/device-unlock-policy.md. This does not complete SEC-04 platform acceptance.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.

D-267 final native review passed all 18 contexts and 36 original Home reloads without the observed COM failure; amount handlers and complete financial rows remain unchanged. Canonical signed Release upgrade/cold/More-to-Home return passed. The unchanged 3,526-test D-266 baseline was retained; native publication is directly verified. Exact evidence and external limits: docs/quality/home-balance-reload.md.


## D-268 sale-result help and truthful connection/account copy

The planned LOC-05 help sits beside an available realised holding result and explains recorded proceeds minus recorded average cost, unknown inputs and the absence of tax rules or official tax documents. The six existing languages include a concrete example. No tax calculation, financial posting, schema or new language is added.

LOC-10 corrects the browser-return message: it describes completion of the browser step and directs the user back to Zanance for the connection outcome, without claiming token exchange succeeded. English loan pay/receive confirmations identify the selected account explicitly, matching the original session argument and the other five translations.

Store drafts now name all six existing languages and describe optional password protection for local/connected backups, readable unprotected files, Android device-protected database keys and final system-backup/device-transfer exclusion. Real OAuth, physical-device, iOS and publication acceptance are explicitly separate; no store text was published.

D-268 final tests: 3,526 passed with zero failures/skips (3m21.454s); 18 original sale-help contexts and 12 English confirmation dialogs completed without financial writes. Independent Android help and final signed Release cold Home passed. Exact scope and retained observer failure: docs/quality/sale-and-connection-copy.md.

D-268 final permanent Windows build passed with zero warnings/errors (18.88s); test outputs cleaned after builds. The canonical complete Release artifact contains final production resources/XAML; later independent diagnostic-only hooks are excluded from Release.

## D-269 costed OD-02 backend proposal

Official provider prices and the 2026-10-15 PostgreSQL availability change inform three bounded USD scenarios and a complete first backend delivery/restore boundary. Arithmetic/overage checks pass; see docs/enhancements/2026-10-commercial-release/07-backend-cost-and-decision.md. Provider, region and test-environment budget remain owner choices. No backend, account, mail, hosted resource, SDK, app code or publication changes; ZCR-ID-01 and the full development objective remain incomplete. Application tests/build/native/APK evidence is unchanged D-268 and was not repeated.


## D-270 shared Windows native toolchain qualification

Build, upstream validation and atomic-width validation now use one reviewed toolchain resolver.
Previously the build accepted both reviewed installer patch versions while the checks accepted
only the original installer and did not qualify compiler bytes. All three consumers now require
the same pinned MSVC, SDK, installer allowlist and exact compiler executable hash before execution.
Windows CI runs the qualification check before native preparation; its dependency cache includes
the shared resolver. No compiler installation, source fork, cryptographic flag or binary changed.

Eleven qualification cases passed, including both allowed installer metadata versions and refused
unreviewed versions, paths, SDK/tools, policy and compiler bytes. Actual installation remains
18.10.12201.205: the other patch version was tested as metadata only. The current candidate's
original upstream atomic operations passed the sentinel, 64-bit carry, four-thread update and
disassembly checks; native SHA256 remains
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21.
The 704 upstream diagnostics remain recorded and broader warning/security qualification remains
open. This tooling delivery does not imply hosted Windows acceptance or device/iOS/publication
acceptance. App code, the 3,526-test baseline and signed APK remain D-268; unrelated checks were
not repeated. Evidence: eng/native/sqlcipher/Test-WindowsToolchain.ps1 and
artifacts/native-sqlcipher/build/windows-storage-d260-ec/atomic-width-51c7a432ac70474da3e3206c04cd5e5a/proof.json.


## D-271 approved local backend and extensible target markets

The owner selected complete local ASP.NET Core/PostgreSQL/private-storage development without
hosting cost. Implement the whole ZCR-ID-01 boundary before final service checks, including actual
isolated restore. Hosting/provider/mail/production spending remain separate gates. No server,
account, purchased resource or deployment was created by this decision record.

Germany and other Western European countries, USA, Canada and Australia are approved currency and
holiday markets. Research an explicit country/jurisdiction/year catalogue with official sources;
country choice stays independent from language and supports future additions. A separate later
Iran variant will include Iranian holidays. Implementation and runtime acceptance remain open.
The complete batch sequence is in enhancements/2026-10-commercial-release/05-delivery-plan.md.


## D-272 complete local identity and private service foundation

Implemented the approved optional ASP.NET Core/PostgreSQL/private-storage backend as an independent
Zanance API project. Maintained Identity password/token providers, protected bearer tickets,
transactional one-use challenges, absolute device grants, atomic refresh generation, immediate
stamp/session revocation, shared durable request limits and private encrypted immutable objects
are connected end to end. Capacity is serialized by real owner in PostgreSQL. No local profile id
or opaque object id establishes ownership. No financial sync/sharing/billing route is implemented.

The separate migration operator and runtime role, persisted wrapped Data Protection key ring,
leased encrypted fictional outbox, resumable deletion/cleanup, content-free readiness and hidden
local start/drain/snapshot/fresh restore operations are implemented. Missing service keys keep the
original installation closed; no replacement key authority is accepted. No real email, account,
paid hosting, app connection, entitlement enforcement or publication was enabled.

Final server Release build has zero warnings/errors. Four actual-host/PostgreSQL scenarios pass
with zero errors/failures/skips on the private .NET/ASP.NET 10.0.13 runtime (6.355 seconds); they cover concurrency,
expiry/replay, ownership, quota, protected delivery, corruption, revocation, cold key loss/recovery
and real pg_dump/pg_restore. Fictional restore was about 0.7 seconds with its committed object and
revocation state preserved. Windows Kestrel and the operator's independently restored destination
returned readiness 200; exact scopes and observed failures are in quality/local-identity-backend.md.

The existing 3,526 app-test baseline and complete signed mobile APK remain D-268; no unchanged app
build, native visual matrix or cipher corpus was replayed. Local ID-01 foundation is delivered;
external sender, hosted certificate/key/backup operations, provider/legal acceptance and mobile
service adoption remain separate gates. The whole development objective remains active.

## D-273 target currencies and source-qualified holiday calendars

Deliver LOC-07/08 as one app batch. The working Western Europe scope is DE, AT, BE, FR, LI,
LU, MC, NL, CH, GB and IE, plus US, CA and AU; the broader geographic boundary question
remains open. Existing EUR, CHF, GBP, USD, CAD and AUD already have ISO minor digits of two.
No stored amount, account currency or language is changed by selecting a country. Country
registration is additive, independent of the interface language. Existing Iran dates remain
compatible; an Iran release variant is future work.

Offline holiday sources and regular dates are reviewed for Gregorian 2026-2027. Settings,
onboarding and Plan display scope, authority, reviewed years and an unverified-year warning.
Switzerland has only August 1 federal coverage; Canada uses federal nominal statutory dates;
the US uses standard Monday-Friday observed federal dates. UK and Australia contain only dates
shared by all constituent regions. Regional, workplace, extraordinary announced closures and
bank-business-day guarantees are excluded explicitly. Legacy Germany/Iran calculations remain
compatible outside the reviewed window; new country datasets do not invent future dates.
Saved Plan country/weekend masks are retained when device regional preferences change.

All 3,569 app tests pass, including 43 new cases; final Windows and signed Android Release builds
have zero warnings/errors. The original regional view passed 18 hidden own-window layout checks
(en/fa/de, light/dark, 360/412/1280 px, Debug text scale 2), complete captions and 44 px picker
height, with unchanged financial rows. Eighteen actual native selections passed in the three
English light contexts. Hidden RTL popup automation failed and is not counted as selection
acceptance. Actual signed emulator Settings selections for Canada, US and Australia, live
Persian/German/English captions, light/dark app choices and return to no holiday calendar were
checked separately through native XML. No financial entry was posted. Emulator Home remains
0.00 EUR; English, Gregorian and automatic digits/week start were restored; theme ends at
Use device setting. This is not physical-device, spoken screen-reader or iOS acceptance.

Detailed official sources, exact scope and observed verification failures are recorded in
quality/target-holiday-calendars.md. No new SDK, network request, permission, schema, paid resource
or commercial enforcement was introduced. The full development objective remains active.

## D-274 operation-scoped encrypted quick-resource reads

Measured protected Release startup after D-273: system first frames 1664/1401/1462 ms,
while loaded Home is observed later (accessibility polling upper bounds, not exact paint).
The original quick-resource operation reopened native keyed connections between EF queries.
A single DbContext alone did not improve its measured warm median (~989 versus ~999 ms).
The complete operation now captures one original context and explicitly keeps its non-pooled
connection open through both resource choices, complete input read and every original validation.
Standalone resource-choice reads also keep their own operation's connection open. No key format,
KDF, schema, guard, commercial activation, global pool or cross-operation credential cache changes.
Later explicit actions still read and validate fresh original data through a new context.

The same independently encrypted fictional fixture and actual production Data assemblies show
warm complete-read median 999.38 ms before, 82.42 ms after. First read 1220.60/565.23 ms includes
process/JIT effects and is kept separate. Wrong-key refusal, 32 migrations, negative balance and
portable read-back pass. Two actual-SQLite cases prove one captured context, unchanged rows/no
default settings or resource-choice writes, fresh action validation and drained ownership.

A Windows review launch exposed stale ApplicationId metadata despite an independent build property.
Its exact process was stopped; that attempt is not independent-installation acceptance. Touch/rebuild
and compiled identity checks precede the corrected review. Debug Home/holiday review routes now
reject a main identity before covered storage startup, without changing Release behavior. Do not
inspect or restore an unrelated installation to turn that failed attempt into evidence.

Final affected tests/build/native results and artifact identity are recorded in
quality/quick-resource-read-performance.md. Earlier full 3,571-test pass precedes the explicit-open
refinement; final Data/App tests are repeated because their production dependency changed, while
unchanged successful test projects retain that baseline. QA-06, full loaded-Home timing, large-data
native publication, physical devices and iOS remain open. The development goal stays active.

## D-275 complete Home loading and retry

Original Home now uses the existing snapshot load state from its first frame through complete
read/publication. Empty initial defaults and dashboard actions remain unavailable until ready;
an actually empty completed snapshot retains its existing next step. Six-language loading,
Home-specific failure/retry, original serialized reloads and retirement guards preserve data.
No schema, permission, SDK, key, stored preference, commercial activation or money formula changes.

All 3,572 app tests pass (zero failures/skips), with the new AT-275 final-retirement publication
case and existing initial/reload failure/retry/cancellation coverage. The final caption refinement
passes 242 localization tests without repeating unchanged successful projects. Strict Windows and
Android builds have zero warnings/errors. Eighteen native language/theme/width contexts, 36 focused
loading/failure captures, 18 actual native retries and unchanged complete financial fingerprint
pass in an independent protected installation. The first partial review exposed a wrong reused
transaction retry caption and was superseded by the corrected complete matrix.

Complete signed Android Release cold launch on the owned emulator shows preparation, actual Home
loading, then ready balance/actions in the same process; no false empty-account card appears.
APK signature/package/payload and exact artifact are recorded in quality/home-loading-publication.md.
This closes the observed publication defect, not the two-second performance objective, Android
fault injection, physical device, spoken screen-reader, OS scaling, iOS or release acceptance.
The development goal remains active; independent owner/provider gates remain unchosen.

## D-276 complete protected work-read performance batch

Complete Plan/Goal reads and fresh validation, budget choice/form reads and validation,
quick-resource validation and bounded budget-alert inputs keep their own non-pooled connection
open through the original operation. Budget form choices and inputs capture the same original
context/file. Every existing rights/stale/current-file check and writer/financial formula stays;
there is no global key-bearing pool, KDF reduction, schema or commercial activation change.

Actual production-assembly native comparison on the same encrypted fixture measures complete
read plus fresh validation: Plan 251.36 to 140.24 ms, Goal 624.61 to 180.53 ms, budget form
1298.93 to 155.51 ms, budget alerts 2281.44 to 653.78 ms, quick resources 788.64 to 146.29 ms
(three warmed medians; first use separate). Negative balance, 32 migrations, portable read-back
and wrong-key refusal pass. These are operation timings, not full loaded Home or device acceptance.
All 3,573 app tests pass with one new actual-SQLite budget context/lifetime case; strict builds,
original native Home and signed Release evidence are in quality/work-snapshot-read-performance.md.
The whole development objective and physical/iOS/reference-workload gates remain open.

## D-277 existing-user upgrade and current test-build policy

ENT-05's current policy is already implemented: the default commercial source is inactive,
with no fake Plus/Pro right or permanent/portable test entitlement. The actual first-schema
upgrade scenario writes four accounts above the approved Free quota and an expense, upgrades
through original migrations, retains identities/balance, accepts an explicit fifth test account,
and restores the real portable source into a separate fresh provider. Both original and restored
providers remain unenforced with null paid context; no resource selections are invented.

One focused AT-277 real-SQLite upgrade/restore scenario passes under strict compilation. It adds
one behavior to D-276's 3,573-test baseline (3,574 covered behaviors, not a new full-suite run).
No app/runtime/schema/dependency/UI change; no unrelated build/native matrix or new APK. The
complete signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.
Exact evidence and scope: quality/test-build-upgrade-policy.md.

Customer activation and announcement remain explicit owner/BIL gates. Do not turn disabled test
enforcement into permanent paid facts or announce future limits as already enabled. LOC-09's
final unattended protection choice and separate local personal-sync approval are pending;
none follows from backend foundation approval. The full development goal remains active.


## D-278 protected reference and tenfold workload evidence

An independent Windows executable now measures the actual protected generation/bootstrap and
production store/validation/calculation paths on deterministic 10,000-entry / 20-account / 100-plan
and tenfold workloads. Complete original schema/header/integrity/typed rows match the authenticated
pre-migration encrypted rollback after measurements, and both plaintext originals are removed.
Warm medians: all-entry reads 158.617 / 1191.737 ms; Plan read/validation 131.983 / 138.041 ms;
quick resources 127.675 / 126.937 ms; original balances/totals 1.487 / 19.066 ms.

Fresh tenfold protected preparation is 27,561.853 ms. Reference 439.797 ms is explicitly a resumed
revalidation, not fresh migration time: the original run terminated on an invalid raw export-byte
comparison, and the corrected complete logical verifier resumed the same generation/keys. The
terminal corrected run passed both workloads; strict Release compilation has no errors/warnings.
No app code/UI/schema change, unrelated test replay or new APK. Application coverage remains
3,574 behaviors (D-276 full 3,573 plus D-277 focused one), not a new complete suite run.

See quality/protected-workload-performance.md and the executable README for samples, failures,
resume ownership and exact scope. QA-06 remains partial: actual loaded Home/search/save, cold/ANR,
reference phone, Android/iOS and the two-second objective are not established by store timings.
The current signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.


## D-279 hosted Windows native qualification

Native cipher candidates now includes a Windows lane using the existing exact compiler qualification,
two fresh native builds with matching DLL bytes, original schema/tamper checks, immutable package
entries, actual production-assembly create/cold reopening and protected reference/tenfold workloads.
Only allowlisted public logs/proofs/measurements are uploaded; databases and DPAPI/key material stay
private. No candidate cache, weaker package/compiler policy, new native dependency or app change.

Actual YAML and all five Windows PowerShell bodies parse. Hosted success is not inferred from syntax
or the existing local D-278 workloads; check the final pushed commit once after the normal delay.
Existing native warnings and the separate unprivileged Windows OpenSSL-suite scope remain explicit.
No unrelated successful app/UI/full-suite/APK replay: application coverage remains 3,574 behaviors
(3,573 full plus one focused), signed APK remains D-276. See quality/windows-native-ci.md for the
exact path, artifact policy and remaining platform/security/device/release gates. Full goal remains open.


## D-280 current protected search and validated-save workloads

The independent protected executable now exercises original complete search/group and validated entry
Save on fresh reference/tenfold data. Focused ledger mode omits unchanged D-278 read/calculation cohorts;
default hosted runs include all operations. Four searches per shape check unique transfer/missing/numeric/
complete results; four actual saves per shape verify stored fields and delete exactly their own row.
Complete schema/header/integrity/typed rows still match the pre-migration encrypted rollback afterward.

Actual warmed unique search/group medians are 9.084 / 17.202 ms; validated saves 68.810 / 69.033 ms.
Search timers include result checks and exclude initial read/native rendering. Both workloads finish,
all eight temporary rows are read back/removed and both original plaintext files are absent. Strict
Release builds have zero warnings/errors. See quality/protected-ledger-performance.md for all samples,
result counts, preservation and timing limits. No unrelated app/UI/APK/full-test replay or new app
change: 3,574 covered behaviors retain their qualified baseline. Native phone/Home/search/Save, cold/
ANR, iOS, the approximate two-second objective and full goal acceptance remain open.

## D-281 automatic device-encrypted local copies

LOC-09 is implemented locally under the owner's final device-key policy. Daily/weekly/off foreground
coordination, independent per-profile timestamps and automatic/pre-restore retention, six-language
status/help/discovery and explicit original protected restore are complete. No password storage,
readable automatic file, cloud fallback, sharing action, new crypto/provider/schema or permission.

Full strict suite: 3,590 passed; final focused Data publication case: one passed (3,591 covered
behaviors, no second full-suite claim). Strict Windows Debug and Android Release builds, 18 final Windows
contexts, native encryption/reference-aware retention, six Android changed-surface/help contexts,
original explicit restore and complete signed APK are recorded in quality/automatic-local-backup.md.
The initial Android live-picker publication loop and native validation lock issue were corrected;
only related checks were repeated. Local success is not hosted CI, physical-device/iOS/OEM,
power-loss, OAuth, store-signing or product-publication acceptance. The full goal remains open.

## D-282 CI platform contract and native provenance diagnosis

CI run 38698595330 on D-281 failed independently in Linux RSA exception assertions, Android
native packaging and Windows compiler qualification. Accept CryptographicException subtypes only
at native RSA refusal assertions; retain exact managed validation and fail-closed checks. Pin native
input-lock checkout endings to their reviewed CRLF bytes on every host: the prior platform-default
checkout changed both fingerprints under Linux, despite identical JSON and native source revisions.
Immutable package entries and observed ABI/runtime qualification remain required.

The Windows host compiler hash differs from the reviewed executable. Preserve refusal and report
only public SHA-256, file version and Authenticode provenance without executing unreviewed bytes.
Twenty focused recovery cases and all eleven existing toolchain cases pass locally. Hosted Windows
compiler review and final successful CI are still pending; this is diagnosis, not acceptance. No
application/UI/full-suite/APK repeat or production encryption/security/release claim is made.

### Reviewed Windows servicing producer and immutable package

Diagnostic run 38704482159 job 116320370636 observed compiler 19.51.36260.0, SHA-256
b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57, with valid Microsoft
Corporation Authenticode signature. Local reviewed 19.51.36257.0 remains pinned separately.
Policy schema 2 requires an exact hash/file-version pair AND valid Microsoft signature, plus the
existing installer/MSVC/SDK restrictions; there is no version wildcard or signature-only fallback.
Microsoft documents compiler servicing separately from folder/toolset versions:
https://learn.microsoft.com/en-us/cpp/overview/compiler-versions.

Windows package candidate.5 has a stable recipe manifest naming both exact reviewed producers.
The unchanged raw candidate manifest and compiler-host proof retain the actual observed producer;
neither is rewritten as another compiler. Every packaged entry, including the original native DLL
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21, remains independently pinned.
Different native output is still refused. Two fresh hosted outputs and the production-assembly
create/cold/workload lane remain required before calling the servicing producer qualified.

A related local consumer failure exposed PreserveNewest retaining old provenance because deterministic
NuGet entries have fixed historical timestamps. Notices/provenance now copy Always. Candidate.4 and its
failed consumer evidence are retained locally; candidate.5 is a new immutable version, never an overwrite.
Thirteen toolchain cases pass. The actual production-assembly consumer builds with zero warnings/errors,
creates with 32 migrations and reopens the same encrypted fixture in another process with exact money,
portable-source and wrong-key checks. Only this changed consumer was repeated after the copy correction.
Hosted completion remains pending; app/UI/APK and unrelated successful tests were not rerun locally.

### Distinct immutable servicing output

Run 38705256020 produced two identical fresh serviced DLLs:
ac0939dbe00bec809fcfc4c77587b509b2798002456bb4c54ed3ca579a12f288. Original-schema,
32-migration, Unicode/money/date, foreign-key, wrong/missing-key/tamper, memory export and complete
staging checks passed before the old immutable package correctly refused the changed manifest.
Its public manifest digest c58665de95ee66aa9907051e273651513e42f01d41048de484cb982872a9a5c8
exactly matches the prior stable recipe manifest with only the observed native SHA-256 replaced;
size is still 5,641,216 bytes. This binds the full metadata to the hosted log without importing a binary.

Keep candidate.5 bound exclusively to the original compiler/DLL. Introduce candidate.6 exclusively for
the exact serviced compiler/DLL, with every entry pinned separately in windows.servicing.package.contents.json.
VafadarWindowsCipherVariant=servicing selects that explicit producer in both hosted Windows jobs and
central NuGet versions; default local baseline remains candidate.5. Unknown variants and cross-producer
packing fail closed. No one package version accepts alternative bytes and no native binary is downloaded.
The 15 toolchain/variant cases and real mismatched-producer refusal are focused checks; full hosted
consumer/workload/main-Windows-build completion remains pending at this commit. Already successful Linux
suite (3,566 passed / 25 skipped / zero failed), Android Release, stock cipher, CodeQL and local backend
on dfae08a2 are retained evidence, not replaced by new local repeats.

### Isolated PostgreSQL TCP readiness

Run 38707538871 failed before service publishing: pg_isready on the default Unix socket observed
"accepting connections" followed immediately by "rejecting connections". The pinned official image
uses a temporary bootstrap server with listen_addresses='' before starting the final server. CI now
waits on 127.0.0.1 TCP for the explicit database, then creates the fixture runtime role over that same
transport. No arbitrary extra delay or blind job retry is used. An isolated, network-none container
from the already present exact image digest verified the entrypoint behavior, final TCP readiness and
actual CREATE ROLE; only that explicitly labeled fixture was removed. Service/app code and prior
successful identity/restore scenarios were not rerun locally. Final hosted completion is reported by
GitHub after this commit; these entries record evidence available at each delivery push.

## D-283 complete coherent protected Home read

Initial Home and reload now use one captured context/keyed connection/deferred SQLite reader for
all original sections, then revalidate the original reviews through a fresh operation before uncovering
actions. Reuse existing quick/plan/goal guards, exact monthly SQL selection and Core calculations;
no key-bearing global pool, lower KDF, dropped history, default-row write or schema/UI policy change.
All native opening/materialization and final validation run off the UI thread. Goal Home projection
uses the same full packet and preserves every original financial row and activity choice.

On the same protected reference/tenfold fixtures, complete read/validation warm medians changed
798.06 -> 223.96 ms and 1925.59 -> 1355.62 ms, including full packet equality checks. Full logical data
comparison passes. Ten real-SQLite cases and three original application-source cases are added;
affected Data 805 / App 1186 pass. Coverage 3,604 means previous 3,591 plus thirteen new, not a full-suite
rerun. Strict builds, 18 protected Windows native contexts, same-process Release emulator Home/entry/
Cancel and a complete signed APK pass. Exact evidence and retained negative observations:
[coherent-home-read.md](quality/coherent-home-read.md). No two-second/phone/iOS/release acceptance;
QA-06 and the full development goal remain open.
## D-284 loaded native Windows Home baseline

The original Home page now has a Debug-only native reference/tenfold review using the same retained
protected D-280 files and device keys. Twelve actual publications (en/fa/de, two each per shape)
retain every account and match complete original ledger totals; both final full schema/header/
integrity/typed-row comparisons pass against authenticated encrypted rollbacks without a financial Save.
Reference publication is 627-929 ms; tenfold is 1849-2312 ms. Largest dispatcher gap is 683 ms.
These are page navigation/read/projection/native-layout measurements, excluding cold app startup,
authentication, page construction and PNG encoding; they do not prove phone/ANR/two-second acceptance.
The appearance's synchronous layout-preference read and remaining UI-thread work remain concerns.
No Release behavior, schema, native package or translation changes. D-283's signed APK and 3,604
behavior coverage are retained; unchanged successful suites are not rerun. QA-06 remains in progress.
See quality/loaded-native-home.md for exact samples and the original native evidence.
## D-285 responsive covered Home publication

Home appearance applies layout from the same validated financial snapshot before uncovering, with
null legacy fallback, explicit default and complete custom order preserved. It no longer performs
a synchronous settings/KDF read. Original ledger scans, balances, plan occurrences/commitments,
forecast and category aggregation run on workers; formatting, collection/native updates remain on
the window thread. Original scopes, rights, complete inputs and final lifetime/file validation remain.
Thirteen focused SQLite cases pass, including three new layout behaviors (3,607 covered behaviors,
not a new full-suite run). Twelve loaded native publications pass with no synchronous captures and
complete preserved rows. Tenfold maximum dispatcher gap changes from 683 to 187 ms, while overall
publication does not improve and reaches 3.11 seconds. Eighteen native cover/failure/retry cases and
nine saved/default/legacy layout cases pass with original data and handlers retained. QA-06 remains
open; no two-second, phone, cold-start, ANR or iOS acceptance. See quality/responsive-home-publication.md.
## D-286 operation-scoped Home page cache

Native stage measurements isolate complete ordered entry reading as the dominant tenfold cost:
1485/1549 ms, reduced to 681/846 ms by changing only that connection's cache target. Home suggests
32 MiB of SQLite page cache only on non-pooled readers; pooled callers keep their default. This is not a
process-memory cap, persistent setting, global pool, KDF reduction or history/column limit.
Final original native Home publications on the same protected reference/tenfold data pass all
12 cases: reference 542-795 ms, tenfold 1351-1837 ms, complete balances and all typed rows preserved.
Sixteen related SQLite cases pass, including read-only/read-write and pooled cache isolation (3,610 covered
behaviors, not a new full-suite run). Temporary timing code was removed before final qualification.
QA-06 still requires phone/low-memory/cold/Android ANR/iOS acceptance. See quality/home-reader-page-cache.md.
## D-287 current-status alignment

Correct current summaries that still described pre-integration plaintext storage, Android OS backup,
missing loan estimation and absent entitlement/identity code. Main Android/Windows protected runtime,
recovery/safety/automatic copies, P2-18 loan estimates and local identity/quota boundaries now match
source. Retain inactive commercial enforcement, optional portable passwords, iOS/device/provider/native
warning review and release gates. Both privacy matrices and the unpublished policy draft distinguish
Android exclusion from pending iOS integration. Initial inventories remain explicitly historical.
Only docs changed; no build/test/device/APK rerun. Baseline remains D-286 with 3,610 covered behaviors,
not a new complete-suite run. See quality/current-status-alignment.md for the finite source audit.

## D-288 hosted Windows installer metadata repair

CI run 38723670276 failed in Windows toolchain qualification, before native compilation or the
application build. Libraries/tests and Android succeeded. The runner reports image
20261007.264.1; its versioned official inventory lists Visual Studio 18.10.12224.181, absent from
the explicit installer allowlist. Add only that version and retain exact Microsoft signer,
compiler file-version/SHA256, MSVC/SDK and immutable native package checks. Pin the inventory
reference to the observed image rather than a moving main-branch document. Include the observed
installer version and rooted-path result in future rejection messages.

The 16 focused toolchain cases pass on the actual local compiler, including new installer
metadata and refusal of unknown versions, changed compiler bytes, malformed policy, SDK/tools
drift and package-identity drift. Hosted compiler bytes and the final Windows build still need
the new hosted run; metadata acceptance is not executable qualification. No application code,
native source/flags/package identity, UI or APK changed; unrelated successful checks were not
repeated locally. Existing 704 upstream warnings remain unresolved.

Sources: [failed CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38723670276),
[exact runner inventory](https://github.com/actions/runner-images/blob/win25-vs2026/20261007.264/images/windows/Windows2025-VS2026-Readme.md).
## D-289 Windows native diagnostic drift gate

Bind the existing 704 upstream warning occurrences/689 diagnostic keys to 224 official-source
files and their hashes. Refuse diagnostic/source drift at native build completion and before
packaging; preserve exact resumed recipes and all original occurrences. Cache immutable packages
rather than incomplete candidate fragments. Export proposals into fresh ignored evidence only;
never suppress warnings or silently promote a new baseline. Twenty-six focused cases, real
producer/source checks and unchanged immutable package entries pass locally. No unrelated app,
UI/APK or upstream suites repeated; hosted result is pending at commit time. Existing warning
safety, platform/device and release gates remain open. See quality/windows-warning-drift.md.
## D-290 one selected-generation verification at covered startup

The all-profile pass opened/discarded the selected generation before the actual session reopened
it. Prepare all other profiles first, then verify and retain the selected key through that same
real session before publication. Require paired captured current-original/publisher authority and
an actual verified profile/installation owner afterward. Keep complete key/KDF/integrity/history,
migration/rollback and observer retry behavior; default bootstrap callers are unchanged.

Nineteen affected strict infrastructure tests, four application startup tests and the final added
no-op publisher case pass (four new covered behaviors; 3,614 coverage, not a full-suite rerun).
Independent Windows/Release AOT Android creation/cold/key-failure cases, same-key observer retry,
retained restored-session cold read, strict Windows build, complete signed APK and original
same-process Android Home/expense/Cancel pass. Cold key reads fall 4 -> 2 (one profile) and
6 -> 4 (two); final guarded small Windows two-profile sample is 338.27ms versus 496.62ms before.
No whole-app/two-second/phone/iOS/native-warning/release acceptance follows. See
quality/selected-startup-verification.md for retained evidence and precise measurement boundaries.
## D-291 original protected Release/AOT Android Home

An independent x64 caller compiles the original Home/App/data sources and measures two actual native
pages in en/fa/de on protected reference (10,000 entries/20 accounts/100 schedules) and tenfold
(100,000/200/1,000) data. The complete twelve-page result and both original schema/header/integrity/
typed-row export comparisons pass. Reference publication is 1104.51-1786.76ms; tenfold is
6586.67-7225.87ms, with largest dispatcher gap 363.15ms. Tenfold latency remains an observed concern.
This excludes page construction, cold startup, fixture preparation and generation verification;
frame callbacks are not GPU/phone/two-second/ANR acceptance.

Caller-only manifest/property/registration/path-alias/locale/native-digit expectation failures are
retained. Original Android locale handling and original NativeDigits presentation are preserved;
no ledger discrepancy or financial Save occurred. Already successful English samples were retained,
and the same prepared financial files/keys continued without reseeding or observation-timeout restart.
Final Release/AOT build has zero warnings/errors; independent signed package/native hashes verify.
Main source/APK remain D-290 and 3,614 covered behaviors remain unchanged, not a full-suite rerun.
No unrelated successful test, Windows/UI/native corpus or unchanged main APK build was repeated.

The owner separately approved complete local personal Pro sync on 2026-10-15: changes/deletions,
explicit conflicts without guessed amounts, idempotency, safe restore and device management.
ZCR-SYNC-01..03 is the next complete implementation batch; paid hosting, household sharing and
commercial activation remain separate gates. This decision is recorded, not implemented by D-291.
See quality/android-loaded-home.md for exact evidence, boundaries and retained failures.

## D-292 complete local personal synchronization (local implementation qualified)

The owner authorized the full local personal Pro synchronization batch: durable changes/deletions,
explicit financial conflict decisions, idempotency, safe profile restoration and device management.
This is one complete locally qualified batch; the final delivery record below supersedes its earlier
unpublished progress. Public hosting, household E2EE and commercial activation are separate approved batches. Current production-source implementation and original service/app/native qualification
are recorded in quality/local-personal-sync.md. Independent caller packages are not a production
APK handoff and the complete-batch acceptance gates remain open.

The operational design bounds active rounds, aggregate serialized proposals and declared attachment
bytes by actual owner across spaces. Exact replay precedes capacity refusal. Revocation/session
expiry retire only unpublished parts under the original owner transaction, with live session
rechecking after lock acquisition. Committed financial history, acknowledgements and nonce tombstones
remain. A separate transaction storage gate orders ciphertext publication and aged orphan-only
collection across workers, without decrypting files or inventing history/pending-work expiry.

A real PostgreSQL/pinned-TLS scenario observes the actual publication and owner-lock waits, preserves
committed files/records/events/receipts, permits rollback-orphan collection and preserves renewed
pending work. Original protected Windows HTTP and Android Release/AOT/trimmed cold-process scenarios
also pass; their caller-only negative evidence and same-fixture continuations are retained. Strict
API Release compilation is warning-free. These targeted results do not replace the final full-suite,
app/native UI, complete signed production APK, documentation/commit/push or external platform gates.
The prior 3,614 covered-behavior baseline is historical; no new full-suite count is claimed.

### Consolidated owner decisions, 2026-10-15

The owner accepted all seventeen questionnaire recommendations with explicit exceptions: current
Android/iOS billing and future unscheduled commercial Windows delivery; future identity-bound web
purchases with prerequisites now and checkout later; Germany-first expandable tax; current-machine
Docker Desktop hosting under vafadar.pro; a reported prepared recent MacBook Air; and independent
German-first bank-format research before USA/other markets. Household E2EE from first delivery,
30+90-day shared expiry, store trial/discount/cross-store policy, technical bounds, local AI/rate
prerequisites, existing-six-language scope and bounded official free downloads are approved.
The complete binding choices and remaining external gates are in commercial-release/06-open-decisions.md.
Commercial Windows and new languages are outside the current delivery, not abandoned product directions.
Future web grants must be independent of the store used to download an app; no checkout or paid grant
is implemented by recording that direction. Current personal sync is not described as E2EE.

Docker/Compose presence and public DNS were inspected without changing other projects, credentials,
records or system settings; container deployment/public TLS are not yet implemented. Primary German
bank publications establish an initial reach shortlist, with explicit group/retail/format evidence
limits; no guessed CSV schema or supported-preset claim follows. These documentation/research changes
do not require replaying successful builds/tests/native checks or create a new full-suite count.

### D-292 continued: complete graph and Linux service qualification (unpublished)

Complete sync graphs now enforce planning unit/amount/settings rules before publication, including the original
one-active-or-paused-balance-goal rule. Fourteen invalid original captures are refused without partial service
publication; valid negative earmark releases, zero budgets and null contribution scaffolding remain allowed.
The actual original coordinator receives the valid graph on a second client. No guessed financial repair occurs.

The affected no-write snapshots include all 33 migrated tables (421 targeted cases pass). Explicit HTTPS factory
options preserve the production transport guard (four identity cases pass; failed override attempt retained).
Certificate-protected Linux persistence is verified under non-root UID/GID 1654 in four actual cases, including
reopening original financial state/TLS keys in a second OS process. The backend CI adds independent protocol,
planning and operations jobs, with private key/data roots excluded from uploaded artifacts. Local qualification
is not hosted success or public deployment. See the qualification document for exact logs and limits.
The D-292 complete-batch final gates and single verified delivery remain unfinished; no baseline full-suite count
or release acceptance is inferred from these focused cases.

### D-292 final native and budget boundary qualification

The complete original Windows first-run/comparison route passes 36 contexts across en/fa/de, light/dark and
360/412/1280 widths in an independently compiled protected installation. Actual native glyph geometry, first-run
Back invocation, comparison command identity/semantic targets and unchanged complete financial fingerprints are
retained. No main installation, owner credentials or desktop input was used. The route is in the Debug inventory.
Final domain review found invalid typed budget periods; original `BudgetPeriods.Range` now validates the complete
received graph with its financial month start. Nineteen actual PostgreSQL/TLS refusals preserve an empty service
publication, while the valid graph is received without changing source rows. Weekly zero year/month remains valid.
Only this changed planning case was rerun. Main Windows identity is restored and the integrated strict rebuild
passes with zero errors/warnings (27.03 seconds). The final affected signed Android build follows this Core change;
its artifact qualification and complete delivery state are recorded in quality/local-personal-sync.md.

### D-292 delivered local scope and next batch

The complete local personal sync implementation is qualified with current main-identity strict Windows and signed
Android builds, 36 final Windows native contexts, nineteen real service planning refusals, retained original Windows/
Android HTTP/native/restore/revocation evidence and 3,690 reconciled managed executions. Exact current APK hash and
historical failures are recorded in quality/local-personal-sync.md. This delivery does not claim a new full-suite
rerun, hosted CI, phone/iOS, E2EE, public hosting or release. The next approved complete batch is isolated current-
machine Docker deployment under vafadar.pro; the remaining consolidated delivery order and external gates remain.

### D-293 - Complete isolated current-machine deployment (2026-10-15)

Deploy original personal service in dedicated non-root read-only containers, PostgreSQL TLS/client-role certificates,
persistent private storage and a narrowly trusted proxy. Use reviewed pinned official images; never reuse another
project or promote fictional accounts into production. Public api.zanance.vafadar.pro uses system certificate trust;
local peers retain explicit pins. Six-language help/defaults make server-readable personal scope explicit.
Actual original initial/cold/public-trust/refusal/private-file restore and retained-key certificate renewal pass.
Fifteen separate hosted API boundary cases and twelve new peer cases pass; 36 original Windows contexts and twelve
Android contexts/six native dialogs pass without financial changes. Current main strict builds and complete signed
APK qualify the affected app. No unrelated full-suite replay. Exact evidence, preserved failures and boundaries:
quality/self-hosted-stack.md and eng/hosting/README.md. Public DNS/routing/mail, hosted CI, phone/iOS and release
remain unaccepted. Continue directly to complete local household E2EE under the consolidated owner approval.

### D-294 - Complete local household sharing with E2EE

The owner approved household sharing with E2EE from the first delivered version, six identities including one
owner, explicit editor/viewer roles and selected separate finances. Service expiry permits30 days of read/export,
then90 further retained days; no real purchase/hosting acceptance is inferred. This is one complete delivery batch,
not separate helper releases. The active design, native evidence, retained failures and exact unfinished scope are
in enhancements/2026-10-commercial-release/11-household-delivery.md.

Use maintained standard JOSE, independently pinned complete membership, current epoch/device grants, protected
profile storage and the original financial capture/validation/conflict/apply path. Local write authority is separate
from commercial test allowances and covers actual SQLite/EF/raw writes plus final transaction deadline checks.
Owner/admin/admission operations retain original signed bytes and private invitation intent before network mutation.
The owner recipient grant inside an original retained proposal preserves initial key recovery across a lost genesis
response. Empty local history is accepted only before any verified local generation and with no unexplained proof.
Generation capture before identity HTTP is rechecked at draft creation, preventing cross-profile adoption.

The current original app flow implements explicit owner creation, private invitations, exact recipient claims,
owner-only pending-device review, two-sided fingerprint approval, role change/revocation and encrypted manual/
foreground routing. Its49 new strings/help entries have actual translations in all six existing languages.
Actual independent Windows SQLCipher/DPAPI clients plus HTTPS/PostgreSQL qualify original workspace creation after
lost acknowledgement, accepted viewer reception, owner-approved editor publication, role/expiry/revocation guards,
original app foreground routing without readable fallback and profile-switch refusal. Six new application cases
passed separately; unchanged successful unrelated suites were not repeated. Current native app rendering, Android
adoption/current final APK, actual Android cost runtime, optional owner recovery, complete audit/transfer/deletion/
retention and bounded full-history integration remain unfinished. No whole D-294 delivery, app-screen acceptance,
physical-device/iOS, public hosting or release is declared; no independent helper commit is complete.

The same batch now includes mapped atomic cost/shares/refund/repayment persistence, explicit household v2 finance
without changing personal v1, actual two-client protected cost/editor/cold qualification and corrected18-case
Windows form geometry. Exact source, retained failures and remaining gates are in the active delivery document.
Three focused actual-SQLite transaction cases and19 focused Core behaviours are covered separately; no new full
suite count or final household acceptance is inferred. Original editor writes do not create cash ledger rows.

The same D-294 batch now composes optional independent household recovery through maintained standard recipient
envelopes, protected original import/preparation journals, finite immutable archive pages, both owner-key roles,
actual account/session plus kit/device consent and exact root-change retry. All-devices-lost recovery atomically
retires prior owner devices and retains other members. Current-owner private root delegation is disjoint from
financial epochs and unavailable to editors/viewers. Public recovered-device anchors survive subsequent kit
revocation without retaining its private kit. Native retained Windows/HTTPS/PostgreSQL process-loss, cold, append
and kit-revocation cases pass with original finance preserved; the active delivery document records failures and
scope. Main file controls, Android/AOT, source/conflict/transfer/audit/deletion/retention/history and final whole-batch
acceptance remain open. No independent helper commit, new full-suite count or whole-household completion is claimed.

D-294 source/review continuation: the original app now offers unchecked account selection with independent
history/file consent and complete native growing disclosure. Recheck the actual source on a worker under drained
profile ownership; use the original maintenance factory rather than an ordinary reader inside its exclusive lease.
Bind the memory handoff to one actual destination/peer/identity, require fresh unlocked destination consent and
persist selected-copy obligation with first owner/device keys. The same native draft reopened cold and refused
empty remote publication; no key/data reset was used after correcting its initial wrong exception assertion.
Eighteen Windows source-selection/review cases passed with actual Toggle/Invoke and full fixture restoration,
resuming only four incomplete observations after native layout settled. Strict current Android compile passed
without warnings/errors in28.63s. The original common conflict VM/presenter also completed a new two-client native
cost-group review in en/fa/de with explicit remote choice, no readable financial HTTP or cash posting and ordinary
encrypted restoration of the original note. Session renewal touched only named fictional local identities through
the real reset/private-outbox/sign-in and existing-device-possession flow. Main destination/profile adoption,
rendered conflict/attachment controls, recovery lock/file composition and remaining household scope stay open.

D-294 actual main Windows source adoption now passes: independent identified app, actual registered local service
identity, native selection/review/consent/flyout/prompt, durable profile creation/drained switch, fresh destination
copy consent, first-commit copy obligation/keys and real encrypted household publication. Exactly one account and
one450-minor-unit historical expense obtained new ids; the private source account/history was excluded. Complete
source finance matched after switching back, and the original copied target reopened with its same keys/owner
authority. The active delivery records incomplete native observer assumptions and continuation of the same data,
session/service/keys, including ordinary six-minute host expiry; no successful case reset/replay. Explicit reviewed
labels pre-fill the still-editable new-profile prompt; existing destination labels show names/ordinals instead of
technical ids. The fictional private credential bridge was current-user protected, expiration-bound and deleted
after completion. This closes functional Windows source/profile/publication adoption, not Android, all native
dialog layout variants or remaining household/release acceptance.


D-294 continuation: exact accepted-member self-device pending approval and owner-device removal now use the original signed roster. Removing a delegated owner device rotates both owner root roles and refreshes an enabled recovery archive; native lost-response/cold continuation passed with unchanged cost data. Unrelated roster updates preserve unaccepted claims/pending devices; retired-root unaccepted invitations expire. Current-root display and exact protected invitation Finish were checked through original native controls. Eighteen new Windows device layouts completed without replaying previous source cases. The whole household delivery remains open; see11-household-delivery.md for precise evidence and retained failures.


D-294 continuation: existing-member devices now use a public owner-signed identity/checkpoint package, independent root/device comparison, retained same-key/session proof and later explicit owner approval. Native lost enrollment response/fresh actual fictional session/history/private-root/cold adoption passed. A real pre-adoption financial-write counterexample was reproduced and fixed: exact connection plus first atomic receive are required, while zero-proposal authenticated receiving uses only a local cutoff receipt. Exit37 and same original receiving continuation passed with complete source financial/settings digest unchanged. Main Windows native export/request/Cancel/admission/cold and18 new-control layouts passed; hidden-window owner fingerprint entry and complete Android remain open. No whole-household completion or new full-suite count is claimed.


D-294 ownership continuation remains part of the unfinished household batch. Exact old-owner offer, actual approved
target-device/new-root acceptance and original-source final publication now have native exits38/39/40/41, public
post-removal receipt, unchanged financial columns and cold committed-key/writer evidence. The general membership
endpoint cannot bypass actual target-device consent. A real encrypted startup schema-upgrade omission was fixed on
the same original app file/key; two callback regression cases and actual native status continuation passed. Eighteen
new status-only Windows layouts, three role/checkpoint trust cases, one changed actual service case, one model case,
21 resource-integrity cases and one literal-key case passed; no complete-suite baseline was recounted. Main complete
transfer prompts/mobile/recovery/file/conflict, target catch-up, audit/deletion/retention/history and whole-batch gates
remain open. See11-household-delivery.md for exact scope, retained failures and current original fixtures.


D-294 ownership reception continuation: a real target prepared ownership keys after downloading ciphertext but before
applying newer saved finance. The same original candidate/operation now completes bounded incoming-only financial
adoption. A fresh Offered-stage case receives before generating keys; wrong-operation/ordinary rotation sync and
normal writes remain refused. An already applied exact horizon is idempotent. Real Viewer and Editor former-owner
transfers passed with no current private-root delegation and complete original financial digests restored by actual
Save/sync. No successful previous suite/layout/cost/device case was repeated. Complete main transfer/mobile/lifecycle
and whole-household acceptance remains open; see11-household-delivery.md for retained failures and original fixture
state. No new complete-suite count or final APK is claimed.

D-294 lifecycle continuation remains unfinished. One focused actual HTTPS/PostgreSQL case passed for owner-only permanent deletion, lost acknowledgement after immutable private intent, database rollback, exact-room cleanup, unrelated-room preservation and 30/120-day retention. Two initial fixture preparations incorrectly attempted a second active room for one identity and hit the real host bound; those failures were preserved and the fixture order was corrected. They were not application fixes.

An independent Windows caller then used the original protected financial factory, real owner workspace, device-bound sessions and complete financial-column comparisons. A real process exit47 occurred after private deletion-marker publication and before PostgreSQL commit. Cold continuation used the exact original locally retained declaration/key/data. A real pre-deletion pg_dump/pg_restore restored the old service database; the unchanged newer private marker blocked access before worker replay and exact-room retirement resumed. Local downloaded money/keys and an independent control room were preserved. Current physical-retirement status may become pending again after old database restoration; permanent access revocation never does.

A separate one-time continuation verified membership/publication audit signatures and a frozen finite prefix through the original client. An intentionally earlier client proposal clock produced an expired original deletion: local writes stayed blocked until the actual service proved no acceptance after expiry and an explicit finish released only that request. No accepted deletion can be locally canceled. Fixed private local metadata slots are excluded from portable financial exports; no local model change was needed. Main lifecycle UI and six-language text are implemented;18 native Windows Active/no-deletion status layouts passed, while destructive confirmation, other states and Android remain unaccepted. Whole D-294, full main mobile/transfer/recovery/conflict and identity erasure, operational backup/latest-marker composition and scale/history bounds remain open. The complete-suite baseline remains3702; narrow/native cases are not a new complete-suite count. No final D-294 APK or release is claimed.
The subsequent original main Windows lifecycle/audit status review completed18 new language/theme/width cases (fa/en/de, light/dark,360/412/1280),54 own-window captures and7992 native observations. Actual native Discover, lifecycle and audit reads preserved every financial column. Initial hidden-page publication/disabled-peer failures were retained; no protected file/key was reset. A bounded publication check and explicit independent ApplicationId build preceded completion; the broader lifetime cause is not proven. This is actual Active/no-deletion status acceptance only, not destructive confirmation, other lifecycle states, audit history completeness or Android acceptance. Strict current Windows and Android builds passed with zero warnings/errors;23 resource completeness/literal-usage cases passed. Successful earlier service/native/suite cases were not replayed. The whole household batch is still unfinished and uncommitted.
The operational historical-restore code now requires the exact latest stopped original private source and immutably merges its deletion markers before target startup. A freshly built capability-labeled service image passed the offline merge as nonroot with read-only root, no network and no capabilities; Windows idempotence, conflicting-original preservation and invalid-directory refusal also passed. Source/target markers are validated without transferring any key/mail/financial payload. Legacy images cannot serve newer permanent deletion authority. This closes the missing code path, but the full new compose/snapshot operation has not been executed; do not mark that operational acceptance or identity-erasure composition complete. No successful D-293 full hosting qualification was repeated merely for these targeted command cases.
D-294 full operational privacy restoration now passed on independent source/target Docker stacks, including actual complete snapshots before/after deletion, live-source refusal, latest-marker merge before old snapshot startup, permanent deleted-room receipt/retirement and unchanged independent-room ciphertext decryption. A concrete configuration omission was fixed: local-household opt-in now survives backup/restart/restore without enabling it publicly. No successful old D-293 hosting stage was rerun.

A separate real retained-owner counterexample showed that the original deletion workspace depended on forbidden financial refresh. The same original protected files/keys and31-day retained service state now complete owner-only opaque privacy checkpoint review, same-device fresh-session possession, original consent and retirement without fetching financial data or epoch grants. Actual financial/key resources and normal local writes stayed refused; every local financial column was preserved. The existing historical trusted root signs only this exact privacy request; newer opaque checkpoint metadata never grants roster/epoch/financial authority, and a missing/different root fails closed. Main confirmation refetches the exact reviewed checkpoint before persisting consent. No schema, dependency or resource changed, so no model regeneration or unchanged resource/layout matrix was repeated. New strict main Windows/Android builds passed; whole D-294 and native destructive-dialog/mobile acceptance remain unfinished. See11-household-delivery.md for current fixtures and exact scope.

### D-294 progress: actual main owner privacy and client clock headroom

The original independent main Windows fixture passed native cancel, retained owner deletion and cold receipt with complete local financial columns preserved. Its deleted-state layouts covered en/fa/de, both themes and360/412/1280. The main Android Debug fixture subsequently completed six native translated confirmation cases, retained-time deletion and a separate cold receipt refresh; its original local account/entry baseline and the unrelated service record remained unchanged. These are scoped Debug/native results, not whole D-294, Release/AOT, phone or iOS acceptance.

A real Android clock one to two seconds ahead made full-budget proposals exceed the service's strict15-minute future bound; cold session reattachment returned409. Client deadlines now reserve one minute for ordinary skew, including invitations/recovery seed creation, without changing server ceilings or existing literal consent. Seven focused cases against the actual service temporal validator passed; the previous complete-suite baseline remains3702 rather than a repeated complete pass.

An explicit negative lifecycle observation can coexist with the last positive grant snapshot. UI and submitted sync/audit actions therefore honor RemoteRefused independently of that old retention value. Privacy metadata never renews financial authority. Deleted status hides future automatic-retention deadlines. Changed strict builds and final native state are tracked in enhancements/2026-10-commercial-release/11-household-delivery.md; the whole batch remains uncommitted until its end-to-end gates are complete.

D-294 final changed-state follow-up: both strict builds passed. The changed Windows deleted-state layout passed18 cases with2,844 native observations and unchanged local financial columns. The new signed complete Android Debug binary then reopened the original retired receipt/local ledger, rendered no obsolete automatic deadlines, disabled the actual audit button and kept lifecycle metadata enabled. Original operation/hash/time and local money were unchanged, without another deletion or receipt request. This verifies the changed display/control gate only; six earlier native confirmation cases and unchanged active layouts were not repeated. Whole D-294, final Release/AOT, physical-device and iOS gates remain open. Canonical details and immutable evidence identities are in enhancements/2026-10-commercial-release/11-household-delivery.md.

D-294 main Android recovery-file continuation: the original main page now passes actual native Save/Cancel,
separate explicit enrollment, native Pick, wrong-password rejection without a draft/financial change, retry of
the same selected encrypted file, completed ownership/history recovery and cold reopening in a fresh protected
profile. All financial columns match the source; the former device is revoked, and returning to the completed
original lifecycle profile preserves its full financial hash. Both strict platform builds pass. No unchanged
suite or completed lifecycle/adapter cases were repeated; complete-suite baseline stays3702. This is Debug
same-installation separate-profile evidence, not device-loss/hardware, Release/AOT, iOS or whole-batch acceptance.
See11-household-delivery.md for exact fixtures and immutable public observations; D-294 remains unfinished.

D-294 retirement-isolation continuation: the actual PostgreSQL worker now preserves unverifiable original room
markers while rotating both finite queues and completing a later unrelated expiry. Unknown raw names advance
discovery but still refuse offline privacy transfer; no marker is deleted, replaced or treated as absent authority.
Five focused file/worker/database/transfer cases passed, with zero-warning strict API/test/caller compilation.
Successful prior suite, native lifecycle/UI and full hosting restore cases were not repeated. The complete-suite
baseline remains3702. Final composed hosted image and other D-294 gates remain open; see11-household-delivery.md.

D-294 main functional ownership continuation: actual Android/Windows main pages now complete invitation/device
approval, target-created independent keys, both-sided consent, explicit former-owner Leave, publication/receipt
adoption and cold owner reopening with identical full financial columns. Actual current root/epoch grants include
only the new device, the old kit is removed, former devices are revoked, and both original main profiles remain
unchanged. A real cold404 UI defect is fixed: known selected household context survives refused rebind rather than
exposing fresh setup/recovery. Six native en/fa/de light/dark cases on the same process keep sync/setup blocked and
financial columns unchanged, using cached-state Reload rather than repeating service rebind. Both strict builds
pass; unchanged successful suites and protocol/recovery/lifecycle cases were not repeated. Complete-suite baseline
stays3702. Final UI consolidation, other main gates and whole D-294 remain open. See11-household-delivery.md.

D-294 main device/cost-group continuation: actual existing-owner device approval and cold receive, two actual
native competing cost Saves, one complete three-row conflict decision,18 new en/fa/de/light/dark/width cases and
original native delegated-device removal now pass. Both full financial hashes match; private costs post no cash
and accepting the remote group makes no new financial publication. A real warm stale-access hint is fixed by
publishing only the already recorded local refusal. Actual404 checks in both main platforms close sync/admin/
audit/setup and preserve data/keys; Android's actual Sync target is disabled. Both original main datasets remain
unchanged and review processes are stopped. See11-household-delivery.md for exact evidence, negative fixtures,
standard signed Debug APK and scoped denial versus signed removal. No successful unchanged suite was rerun;
baseline3702 is retained. Final storage/history/composed-image/Release-AOT and whole D-294 gates remain open.

D-294 bounded-history continuation: retain the original current graph, but move historical financial bodies to
bounded authenticated reads of the existing protected ciphertext. Add only derived private row positions and an
opaque-address predecessor index; keep full signed replay, financial validation and atomic acceptance. Read old
proposal baselines before the durable writer, and never hold its native connection across HTTP. Actual SQLCipher
warm/cold12009-version paging, large framed row, original conflict, inconsistent/missing pointer and portable
private-table scrub checks passed. Eight new framing cases and the existing compiled-model match case passed.
No full-suite total or whole D-294 completion is inferred. Exact evidence and preserved fixture failures are in
11-household-delivery.md; no unchanged successful UI/financial workflow was replayed.

### D-294 final local delivery

The complete local household batch is implemented and qualified together: protected E2EE membership/device/key
and optional recovery lifecycle; selected separate financial adoption and complete costs/conflicts; two-sided
ownership transfer; audit, permanent privacy intent/retirement and latest-marker-aware full service restoration.
Final service composition, bounded historical row reads, compact pre-allocation portable scrubbing, real main
Android upgrade/export, separate Release AOT/trimmed native allocation and native transfer-dialog geometry pass.
The original full run plus only899 exact failed-case reruns yields3753 passing existing cases;2854 prior successes
were not repeated. Separate focused cases do not change that total. Final product APK:
artifacts/android-household-final-d294/pro.vafadar.zanance-Signed.apk;91356917 bytes;
SHA2566ebe1f0d8f963f1c1f18cd2ecee7d03a4a5cfc5325d25e3883bdeee960ac38f2; signature/permanent id/arm64+x64 verified.
Exact scope, retained failures and runtime evidence are in enhancements/2026-10-commercial-release/11-household-delivery.md.
Earlier in-progress notes are historical. Public/commercial activation, physical/iOS, store/provider/signing,
external security review and publication remain separate gates; identity erasure and ciphertext recall are not
claimed. Continue the already approved complete billing/future-web-prerequisite batch without helper-only slices
or repeated unrelated successful checks.


## D-295: local Android billing and cross-store verification authority (2026-10-16)

The local Android/service billing composition is implemented: actual Play9.1 catalog/price phases and retained
native intent, explicit original subscription replacement/restore, account-free local Plus, confirmed-identity Pro,
canonical provider verification, signed current/private cache, notification/acknowledgement continuation and original
identity UI. Future web is an identity-bound reserved issuer, with no checkout or unverified grant route. Principal,
environment, purchase origin, app distribution and financial profile stay separate. Commercial quotas stay inactive.

Source-linked policy/application cases, actual PostgreSQL and authenticated HTTP continuation/ownership/refusal
cases, strict builds, changed native layouts and the current signed Release APK are qualified locally. Earlier
successful household/cipher/full-suite/native-private-storage scenarios were not repeated. The last whole existing
baseline remains3753; focused billing cases are separately listed, not a newly executed complete-suite total.

This does not finish cross-platform billing or ZCR-BIL-05: StoreKit2 native implementation/build in the actual Mac
clone, genuine store products/provider credentials/sandbox purchases/renewals/refunds, public callbacks/TLS routing,
physical devices, SDK traffic/privacy and production/store signing remain explicit gates. No owner's credential,
new account, payment or commercial activation was used. Windows commercial remains outside the current scope.
Android-first local delivery can be retained without holding already approved independent work for the unavailable
Mac/store environment; this changes execution order, not approved product scope.

Exact evidence, negative cases and remaining gates: enhancements/2026-10-commercial-release/12-billing-delivery.md.


## D-296 Germany accountant preparation

The approved Level 1 batch adds an explicit year/account review and readable ZIP containing exact ledger data,
separate currency/kind recorded totals, blank accountant-review columns and opt-in exact stored document copies.
Transfers remain one row; refunds keep their links; no deductibility/VAT/profit/filing is inferred. Notes/documents
start off, linked transfer identities are explained, original-source changes and retired form/file/rights/lock
refuse sharing, and no money/preferences are written. This is a Plus preparation feature distinct from later
TaxAddOn rules; test commercial enforcement stays inactive. Existing basic CSV and portable backup are retained.
Original invoices/structured e-invoices must be kept separately; this is not an original/GoBD/DATEV/ELSTER archive.
All six translations and the individually named future country queue are included without an ETA.
Implementation, official-source qualifications, bounds, failures and final evidence are recorded in
`enhancements/2026-10-commercial-release/13-accountant-export-delivery.md`. Expert calculations and physical/iOS/
public release acceptance remain separate. No new database schema, migration, SDK or network permission is added.


## D-297 Documented StarMoney Giro cashflow import

Deliver the approved German-first CSV batch through the actual existing importer rather than guess current
consumer-bank schemas. Original StarMoney help/field specification supports this explicit ordinary-cashflow
variant; complete transfer/refund/reversal/split and pending-status semantics remain unqualified and are refused
or explicitly excluded by per-import consent. Select encoding/account, inspect every row, confirm natively,
revalidate exact source history/rights/proposed entries in the writer, preserve idempotence and durable Undo.
No schema, migration, SDK or permission change. Current commercial test limits remain inactive. Broader IMP-01
and BANK-01 stay In progress; OFX/QIF/CAMT remain outside approved implementation. Exact original sources,
focused verification, retained negative evidence and native/package qualification: enhancements/2026-10-commercial-release/14-bank-csv-delivery.md.

## D-298 Optional personal aggregate analysis and local credit prerequisites

Implement OD-13 through one app/data/service composition: explicit personal scope, original ledger aggregates,
complete local disclosure, native consent, separate manual text sharing, pinned existing identity and device-private
original continuation. Read-only untrusted output never executes instructions or financial writes. Shared data is
refused until its separate provider-disclosure permission is qualified. No receipt/note/free-form instructions,
account names/ids, payees or documents enter a provider payload. No API key is requested from the user.

The separate service owns append-only purchased/gifted credits, gift-first reservations, actual-cost settlement,
unused allocation refunds and immutable original consent. Purchased credit has no expiry. An internal trusted
grant port is not a sale or client route. Literal request identity survives uncertain transport; never dispatch an
accepted id twice or guess a refund. An absent original may be explicitly replayed identically or durably closed
without financial upload; late packets cannot execute a closed id. Actual authentication/owner locks and unique
grant receipt identity prevent cross-user grants and duplicate reservation. Service schema uses two additive EF
migrations and the regenerated service compiled model; the financial application schema is unchanged.

No live model/provider, price/package units, credit purchase or public activation is selected. Local qualification
and exact negative evidence, native/package state and remaining gates are recorded in
`enhancements/2026-10-commercial-release/15-ai-local-delivery.md`. Historical whole-suite3753 was not repeated.

## D-299 Optional reference rates/prices and immutable source history

Implement the approved OD-14 local prerequisites as one app/data/service delivery: explicit public-symbol/date
lookup, original provider/licence/revision/unit/timestamps, bounded device-private cache, exact original native
review and transactional rate/price application. Never infer a provider, licence, coin premium, purity, holding
quantity or actual payment from a reference. Keep manual input independent; no automatic lookup or financial edit.
Fine-troy-ounce conversion uses31.1034768 grams and the explicitly matching metal/purity; gram/piece mappings
require explicit item consent. Amounts preserve exact financial units and direction-safe display packets.

Immutable MarketReferenceReceipts retain accepted source/conversion and exact target fingerprints through manual
correction, cache expiry, portable restoration and deletion of a target. A changed/deleted target retains history
without current attribution or permission to recreate finance. Additive application migrations and regenerated
compiled model add source metadata and separate source companion state; financial schema hashes stay unchanged.
Personal transfer uses authenticated profile/device authority with original-id/digest replay and a separate
source cursor. Shared transfer uses explicit format2 source companions in the existing signed/encrypted original
publication and signed membership horizon. Legacy format1 bytes/meaning are unchanged; old readers require
upgrade before reading source format2. Source metadata never creates a financial row or write grant.

Use the existing active-page native interaction adapter for confirmation, including original Windows dialog
target sizing. Native review is read-only until explicit confirmation and checks the same file, profile, lock,
household membership and target definition in the actual writer. Exact focused, TLS/PostgreSQL, protected
process-boundary, native and package evidence and remaining gates are recorded in
`enhancements/2026-10-commercial-release/16-market-reference-delivery.md`. The live provider, coverage, rights,
licence/redistribution, pricing, public activation and physical/iOS acceptance remain open. Current quotas remain
inactive; local evidence is not a product release or a newly repeated full-suite total.

## D-300 Measured Android cold-start method compilation

Preserve startup, license/privacy and protected-storage ordering. Actual Release phase measurements showed
substantial synchronous managed bootstrap cost before the window, including license initialization; storage
already ran behind the cover on a worker. Supplement the SDK profiles with a captured actual cold-start method
profile instead of moving cryptography, disabling telemetry too late or repeating rejected asynchronous roots.
The recorder/environment are development-only and removed before production compilation. No global emulator
property, OS setting, credentials, schema, financial algorithm or access rule changes.

The production candidate reaches its first Activity in10624ms and actual loaded Home is observed by33485ms
on the original fictitious installation, without an ANR in that process. These are distinct observations, not
a GPU-ready measurement or a repeatable reference-phone/two-second result. Original account/balance and native
expense/Cancel survive. Exact compilation, package tradeoff, final qualification and limitations are recorded
in enhancements/2026-10-commercial-release/17-startup-responsiveness-delivery.md. Historical full-suite counts
are unchanged; successful unrelated tests are not repeated.

## D-301 Reduced official storage crypto and immutable producer qualification

Reduce unnecessary deprecated/legacy/plugin/network/algorithm facilities through the current official OpenSSL
Configure interface, retaining SQLCipher's AES/SHA/HMAC/PBKDF2/RAND, secure memory, threads, assembly and format
parameters. Use the same closed profile on Windows/Android and the independent Linux suite. Local producers,
actual50-migration/native original-key evidence and official configured suites are qualified together; remaining
508 diagnostics have no safety waiver. Do not suppress warnings or introduce a custom cryptographic provider.

Reference provider-neutral production Data assemblies in compatibility hosts so real dependencies and frozen
migration resources cannot diverge through source globs. Keep complete current migration checks and public
publication validation. Native report paths bind an explicit fresh run and publish complete results atomically.
Producer-only branches/export versions qualify actual compiler output before immutable consumer entry pinning;
ordinary main/PR consumer gates stay intact. No automatic feed, application or release adoption follows.

The complete batch, real failures, evidence and remaining hosted/adoption work are recorded in
enhancements/2026-10-commercial-release/18-native-storage-profile-delivery.md. At this producer stage the main
APK is still D-300; application/package adoption, per-site warning safety and device/iOS/release gates remain.

### D-301 final native consumer adoption and observed workflow context repair

Actual producer run39002185961 passes on the reviewed source/profile and hosted servicing compiler. Verify its
artifact digest, two fresh outputs, current50-migration/native proofs and all nested package entries before
recording new servicing candidate.8; keep distinct baseline candidate.7 and Android candidate.5. Main strict
Windows and ordinary signed Android Release, exact embedded native hashes, retained installation cold Home
and real expense/Cancel pass locally without key/data replacement. Original diagnostic safety remains open.

The parallel backend workflow fails before any job because runner.temp is unavailable in a job env map.
Place the identical private path in the two consuming step env maps, retaining private fixtures/report-only
uploads. This is a parser/context repair, not a reason to repeat successful local backend/sync/app tests.
Current full main-hosted completion remains distinct from the successful producer run. Exact evidence,
failures, APK and open per-site/platform/release gates are in18-native-storage-profile-delivery.md.

### D-301 normal-main qualifier repairs after actual hosted failures

Actual main CI exposes three stale/environment-specific qualifier boundaries, while app builds, native source/
schema/package/binary qualification, Linux stock suite, identity backend and CodeQL pass. Refresh the delivered
20-row comparison expectation and require the accountant row in all six failed language cases. Make only the
copied public Caddy certificate readable by the independent Linux caller; never loosen private-key or TLS policy.
Separate complete raw pre-work migration preservation from complete before/after portable financial projection
in the benchmark, retaining the original strict generic verifier and all production export/privacy rules.
Six focused cases, the exact Linux UID/mode boundary and both actual protected reference/tenfold workloads pass;
no successful unrelated suite/build/UI/APK is replayed. The same D-301 app/APK/native bytes remain current.
Exact original failures and current evidence:18-native-storage-profile-delivery.md. Hosted completion is separate.

## D-302 complete original currency fields

The common editable Syncfusion currency input now explicitly retains native text scaling and a visible
44-unit real list button with the original translated field name. Original filtering/selection stays in the
vendor control. Its original currency rows have scalable app typography, real native actions and a translated
selected-state description. Amount/currency groups in transaction, goal and manual-rate forms use complete
rows rather than fixed 110-unit or split currency columns. Drafts, bindings, field errors and explicit Save
remain unchanged; code selection does not save money.

All 18 English/Persian/German light/dark 360/412/wide contexts qualify original native input and list actions
at the requested process-only 200% font stress, including the three actual constrained forms and complete
unchanged isolated account/entry rows. Current strict builds, signed main Android upgrade, native list/selection and typed search
and Cancel evidence are in19-currency-accessibility-delivery.md. No unrelated successful whole-suite, native
crypto, synchronization, billing or service qualification is replayed. This is no new unit-test count or actual
OS-scale, spoken screen-reader, physical-device, iOS or release acceptance; A11Y-03 remains In progress.

## D-303 complete ledger read without discarded identities

Home reads the entire ledger in physical row order, then applies the original stable newest-first date/time
order in memory. This avoids repeated encrypted-page visits through the date index without dropping history,
changing the operation-local cache, using SQLite temporary sort files or weakening encryption. The supported
compiled-model extension binds the existing-id LedgerEntry constructor, so materialization no longer creates
and discards a fresh identifier. New entries keep their original identifier generation. The compiled model is
regenerated; schema, financial rules, keys, backup and privacy policy are unchanged.

All 881 Data tests pass, including two new AT-303 complete-value/order cases. Actual Release/AOT Android
reference/tenfold Home in English/Persian/German retains every canonical financial presentation and complete
raw schema/header/integrity/typed rows on the same ciphertext and keys. Tenfold publication median decreases
from 14,828.37 to 7,418.54 ms; reference median from 3,066.82 to 2,252.00 ms. These are controlled loaded-emulator
observations, not two-second cold/phone/ANR/iOS acceptance. Windows native publication and complete portable
preservation also pass. Full-suite historical counts are not a new full run. Exact evidence and package:
[20-home-read-delivery.md](enhancements/2026-10-commercial-release/20-home-read-delivery.md).

## D-304 isolated billing HTTP fixture peers

The D-303 hosted backend qualifier failed because separate TestServer hosts shared a null remote peer and
consumed the same durable 30-request billing window. The same limited Linux sequence reproduces HTTP429;
standalone Windows/Linux cases pass. A test-only startup filter supplies a stable documentation peer per
fixture, without trusting headers or changing the production limiter. The previously failed original client
continues its retained pending command/authority. Separate Integration HTTP workers do not recreate a missing
ephemeral installation key. The exact failed retirement case and a new 30/31-request, independent-peer,
second-worker and next-window regression pass on the retained Linux fixture. Strict test builds pass;
production app/API code and the D-303 APK are unchanged. Its one-use hosted repair check later completed successfully for backend, CI and CodeQL.
Evidence: [21-billing-http-isolation.md](enhancements/2026-10-commercial-release/21-billing-http-isolation.md).


## D-305 complete typed Home materialization

Home retains complete 32-field ledger history and original order, transaction, conversion, cancellation and
file/rights boundaries while replacing per-row EF materialization with a guarded typed reader through EF's
original relational command pipeline. A mapping drift fails explicitly. Same-process Release/AOT tenfold
source-read medians are 2707.56 ms (original EF) and 2211.90 ms (typed), with complete materialized equality;
no stable reference improvement is claimed. Separate original Android Home publications in en/fa/de retain
all canonical financial presentation and full raw database preservation. Four new AT-305 boundary/value cases
and 24 distinct related Home/model cases are qualified across retained runs; no new whole-suite run is claimed.
Original hidden Windows Home and complete portable preservation also pass. The actual loaded main Android
startup method profile is refreshed, with development-only recording excluded from the ordinary candidate.
Evidence, package and remaining limits: [22-home-materialization-delivery.md](enhancements/2026-10-commercial-release/22-home-materialization-delivery.md).

QA-06 remains In progress for remaining composition, memory/cold/ANR, physical-phone and iOS acceptance.
The D-304 one-use hosted repair result is now completed/success for backend, CI and CodeQL; its timer is deleted.


## D-306 original reviewed inputs and detached goal lifecycle drafts

Original plan/goal reviews now reject changed in-memory model packets as well as changed database rows,
while retaining original file, choice and rights validation. Six Home mutations reproduce the missing
rejection before the fix; the goal allocation writer also refuses an altered source without money changes.
GoalDetail lifecycle actions now write a complete detached clone, preserving the original review through
confirmation and Save. Seven new regression cases and 179 distinct related Data / 169 original-source app
cases are qualified across retained runs; only the three affected failing app methods are repeated after
that caller correction. No new full-suite run or discarded prototype test count is claimed.

Twenty-four actual native goal actions/confirmations pass across en/fa/de and light/dark in a fresh Windows
fixture, preserving the unsaved amount and original ledger. The protected Release/AOT Android reference and
tenfold cases reject altered reviews, accept the same review after restoration, retain original native Home
presentation and complete raw data. Strict builds and the complete signed main APK pass; the original main
installation reaches unchanged Home after upgrade. A recurrence-cache trial is rejected for insufficient
benefit and completely removed; QA-06 and physical/iOS/release acceptance remain open. Exact evidence:
[23-reviewed-input-integrity-delivery.md](enhancements/2026-10-commercial-release/23-reviewed-input-integrity-delivery.md).

## D-307 target-market bank/export discovery and provider comparison

Complete the first source-based discovery pass for all fourteen current target markets, retaining the
Germany/USA-first order. Prefer original consumer export evidence over a guessed group-wide format or
asset-based popularity rank. Postbank/AIB consumer CSV, configurable George Business output, ABN AMRO's
non-CSV format list, NatWest's retired midata and unresolved small-market contracts lead to different
qualification actions. Compare Tink/Plaid/TrueLayer/Basiq as candidates only; no contract, live connector,
credentials, fees or financial HTTP is authorized or added.

Country-set/link/status checks qualify this documentation-only delivery. Exact consumer specimens and
financial relationship semantics remain required before another preset. No app/test/native/APK run is
repeated, no test count increases, and D-306 remains the actual runtime/package baseline. Evidence and
source limits: [target bank research](enhancements/2026-10-commercial-release/24-target-bank-research.md).

## D-308 complete native saved-filter actions

Original Transactions saved names now wrap on growing full-width rows inside the existing bounded filter
viewport. One real overlay button retains each original query, argument and complete spoken name. Native
normal-scale evidence reproduced a name/action wider than its viewport; shared compact choices already
passed native 44-unit checks and are unchanged. Original typography/colors, query logic and financial rows
are retained. The maintained independent Windows/Android callers qualify the original page without using
owner data. Exact results, negative evidence and package: [saved-filter actions](quality/saved-filter-actions-readable.md).

## D-309 one owned connection during protected schema review

The actual protected-startup schema callback previously let EF independently open its supplied closed,
unpooled keyed handle for history existence/read and a pending migration. Reproduced counts were two opens
for current/unknown history and three for a supported old prefix. Retain one original connection only for
this operation, close it on success/failure/cancellation if originally closed, and preserve already-open
caller ownership. Keep the original supported-prefix refusal, real migration suffix, compiled model,
maintenance scope, KDF and unpooled policy. No schema, key, native component or global cache changes.

Eight new related AT-309 cases and the original two cases qualify the ownership/history boundary. The old
schema fact also preserves original first-schema account, entry, settings and balance through migration.
Native same-ciphertext evidence, final package and explicit remaining limits are recorded in
[protected schema connection lifetime](quality/protected-schema-connection-lifetime.md).

## D-310 original verified purchase lifecycle on the plan screen

ENT-04's original current-plan/renewal requirement was only partially visible: the billing page showed a
verified tier and offers, but no original owned purchase validity/renewal/lifecycle details. Display those
facts separately from new offers and from the current access decision, preserving device/identity horizons.
Use original provider-confirmed paid/grace ends, last verification and scheduled changes with the current
regional date formatter. Lifetime never gets an invented expiry; automatic renewal is labeled as observed
at last verification, never as a predicted next debit. Saved verification/outage allowance explicitly needs
refresh. No purchase reference, receipt, account identity or provider diagnostic enters display text.

All six existing languages receive real translations. Original growing labels keep native font scaling,
commands and pinned Continue; no commercial activation, price, network provider, key, schema or permission
changes. Exact source/test/native/package qualification and remaining real-store/Mac/device gates:
[verified purchase lifecycle](quality/verified-purchase-lifecycle.md).

## D-311 complete report and forecast-table monetary packets

Original report trend/history and forecast comparison/decomposition tables still placed dates/identity
beside trailing amount columns. The original native German 360-width run loses the canonical month caption
and overflows monetary columns even at normal scale. Keep the alternative-to-chart tables readable with a
whole identity row and separately labeled full-width original monetary packets. Reuse AmountReadout's
existing native horizontal viewport/hint without parsing, splitting, changing or recomputing a value.
Retain original 12/13/14-point scalable typography, total emphasis and semantic colors. Charts, PDF/report
projections, source strings, snapshot content, permissions and financial calculations are unchanged.

Actual original-page Windows and Release/AOT Android qualification uses independent fictitious display
packets and preserves original stored data. No new unit count or repeated successful financial/backend/
crypto/full-suite checks are claimed. Native scope, retained failures, final package and external limits:
[financial table readouts](quality/financial-table-readouts.md).

### D-311 owner execution order and external gates (2026-10-17)

The owner authorized the connected Samsung SM-A176B for complete signed Android candidate installation/
upgrade without data reset and qualification only in fictitious profiles. Preserve system settings,
accounts and personal data; actual authentication remains the owner's action. Android qualification
comes first, Mac/iOS afterward. Company registration precedes owner-created publication/provider
accounts. Use Cloudflare Tunnel for the approved current-machine hosting path; the owner signs in and
enters tunnel credentials personally. These are execution decisions, not completed public DNS/tunnel,
phone, Store/OAuth or release acceptance. Do not ask these settled choices again.

## D-312 physical Android safety-copy recovery

Actual fresh Samsung qualification found that a pre-restore encrypted safety file was retained but absent
from the Backup page. Add a separate six-language current-profile safety candidate list. Capture exact
profile/installation under drained original storage ownership, read only the bounded nonsecret header,
and never read/create keys or decrypt financial packages for listing. A matching header is not authentication:
reuse original full validation, preview, access checks and explicit replacement consent on selection.
Automatic/portable cohorts remain separate; device-only rows cannot share a file. Existing keys/files are
preserved. Seven focused tests and 18 original Windows layout contexts qualify the changed boundary;
actual phone continuation and complete package evidence: [physical Android safety recovery](quality/physical-android-safety-recovery.md).


## D-313 outbound-only approved hosting path

Implement the approved Tunnel origin and separate credential-file connector with exact TLS/forwarding authority and production classification. Preserve gateway mode across snapshot/restore/maintenance; never expose fictional services or read owner credentials. Seven operational checks and nine distinct protocol cases pass locally. Public tunnel/DNS/real mail/legal acceptance remains open. See [outbound tunnel origin](quality/outbound-tunnel-origin.md).


## D-314 physical ARM64 protected workload

Use the owner-approved Samsung and existing independent original-source caller with a complete signed ARM64 package. Prepare once, retain the same ciphertext/keys, preserve five completed Home groups after a caller-only layout-window failure, and continue only the incomplete group. Original native search and eight validated writes/read-back/exact cleanup preserve full financial data. The reference/tenfold initial-publication latency remains a measured concern, not two-second or cold/ANR acceptance. See [physical protected workload](quality/physical-protected-workload.md).


## D-315 complete unbounded ledger reads on physical Android

Actual same-file phone profiling isolated the old public full-history path: 7.77 seconds versus the existing complete Home path's 4.56 seconds for 100,000 entries. Reuse its sequential full-field reader only without date bounds; preserve original bounded SQL, exact order/values, transaction/interception and cancellation. Do not change the small timestamp converter cost or weaken encryption. 34 focused tests use independent EF oracles. Physical public/native reads and the data-preserving complete main upgrade are verified; large Home/cold/ANR/platform acceptance remains open. See [physical ledger read](quality/physical-ledger-read.md).


## D-316 physical planned-work lifecycle without unrelated replay

Use the unchanged complete signed D-315 main APK and ordinary original forms in an isolated retained fictitious profile. Partial 4 + final 6 yields exactly 10 spending, 990 balance, 40 budget left and the original account-balance goal; process restart preserves it. A separate future 20 appears only in Upcoming/forecast (970); exclude/reset changes the forecast, not the ledger. Return to original Main at 979. Retain observer failures and the undiagnosed first due-publication observation without speculative source changes. No new build/full-suite count. See [physical planned work](quality/physical-planned-work.md).


## D-317 original physical CSV consumer and durable Undo

Continue the retained D-316 fictitious main-app profile with the unchanged signed D-315 APK. Actual DocumentsUI cancellation/selection, two-row own-format preview/import, fixed-ID duplicate refusal and cold persisted batch Undo pass. Exact multiline/tag/formula-like text and financial projections are checked; Undo preserves the prior two plan payments and restores 990 balance/40 budget left/970 forecast. Main returns to 979. Redact unrelated picker names and remove only the hash-matched owned CSV. This is not a producer-export roundtrip, real-bank/aggregate/hostile-file matrix or full raw equality. See [physical CSV and Undo](quality/physical-csv-undo.md).

## D-318 first-plan native publication

Actual main and independent original-source Samsung reproductions isolate correct stored/bound rows but blank native realization after leaving CollectionView.EmptyView. Retain the original list adapter and move existing empty feedback to a growing independent scroll viewport; reuse WrappingAction after the German large-text caption failed visually. Same protected fixture/native Save regression, 18 final hidden Windows contexts, zero-warning Windows/complete Android builds and the data-preserving main upgrade pass. No financial/model/crypto change or unrelated successful-suite replay. See [first plan publication](quality/first-plan-publication.md).

## D-319 physical debt/lent lifecycle and genuine missing terms

Qualify both directions through the original phone setup/account/estimate/native confirmation/ledger paths, including cancel without writes, principal versus actual interest, explicit final transfers and cold signed upgrade. All 17 displayed estimate rows satisfy independent integer arithmetic; six actual financial packets and final 1,495 EUR are retained, Main returns 979 EUR. Fix the observed false missing-terms hint at zero balance using actual installment configuration independently of estimate availability. Original calculators/writers/access remain unchanged. All 38 related native Windows cases and final zero-warning Windows/complete Android builds pass; unrelated successful suites are not replayed. See [physical loan lifecycle](quality/physical-loan-lifecycle.md).

## D-320 retained-load Android AOT stability

The independent phone caller now matches production assembly/profile inputs. A captured extra CoreLib
AOT-method selection reproduces native failures under load; removing only those additional roots while
preserving SDK defaults completes all 12 en/fa/de reference/tenfold Home cases and full schema/typed-row
preservation on the same retained ciphertext/keys. Align restored runtime/compiler/task servicing versions,
fix matching-configuration restore, keep only the explicit review window awake and retain scoped failures.
Windows and complete signed Android builds pass without warnings/errors; the main data-preserving upgrade
shows 979 EUR and actual expense/Cancel returns unchanged. No financial C# algorithm, schema, KDF/native
cipher or full-suite count changes. Current loaded observations remain 1.96-2.65s reference/8.83-10.29s tenfold;
this is stability qualification, not a general speed improvement, full cold/ANR matrix or release approval. See [native evidence](quality/physical-home-compilation.md).


## D-321 complete current-row ledger reading

D-321 reduces the measured complete 100,000-row reader from 4231 to 3344 ms and complete Home input read from 4653 to 3811 ms on retained Samsung ciphertext/keys. All 12 final en/fa/de native Home cases preserve complete financial presentation and raw schema/typed rows; loaded Home still takes 1.83-2.73s reference/ 8.47-9.58s tenfold. Related validation is 42 passing cases plus one final interceptor-reader case (nine added behaviors), not a full-suite rerun. Final strict Windows/complete signed offline Android builds and data-preserving main Home/All-history/return pass. QA-06, cold/ANR statistics and external platform/release gates remain open. See [evidence](quality/physical-home-row-reading.md).


## D-322 trusted current-row null metadata

D-322 reduces the measured tenfold complete Home input read from 3811 to 3306 ms using exact-query null metadata, retaining all original 32 fields/conversions and safe interceptor fallback. All 12 final en/fa/de phone Home cases retain full financial presentation and raw schema/typed rows; loaded Home remains 1.92-2.83s reference / 7.23-9.51s tenfold. 45 related cases pass (two added behaviors), strict Windows/complete signed offline Android builds and main Home/All-history/return upgrade pass. A recurrence candidate without reliable native gain and temporary worker probes are fully withdrawn. No whole-suite rerun, two-second/cold/ANR/platform or release acceptance is claimed. See [evidence](quality/physical-home-null-bitmap.md).


## D-323 complete physical report/filter/PDF handoff

The unchanged D-322 signed offline main APK passes an original Samsung report/filter/PDF handoff batch:
October income 1,000/spending 21/surplus 979 and five-row drill-down; empty September scope replaced by a
saved October expense filter (three rows/net -21); actual PDF chooser offered and cancelled without a
recipient; filter retained after cold PID 24409 -> 27876 and then explicitly deleted; original unpaid
plan 3/headroom 976 and cash-account zero income/spending retain transfer semantics. Final report/ledger
and Home return 979 without financial Save/reset. Eleven saved-device assertions pass; no new managed
test count, build, APK or unrelated successful checks are repeated. This is English/light displayed-state
acceptance, not raw main-store equality, inspected phone PDF pages, all reports/readers/iOS or release.
See [physical evidence](quality/physical-report-filter-pdf.md).


## D-324 complete physical digital-receipt batch

The unchanged signed D-322 main APK passes six original Samsung digital-PDF receipt flows, one per
en/fa/de/es/fr/it source language: distinct total phrases and Persian digits yield the reviewed 24.50 EUR,
not cash 50/change 25.50/order 999999. Every original editor is explicitly cancelled/discarded without Save;
the same PID 27876 retains five rows/net 979 and final Home 979. The incomplete picker observation alone
continues, and only six exact hash-matched newly owned input files are removed. No managed test count,
app build/install or unrelated successful check is repeated. English/light presentation, digital text and
displayed preservation are the scope; evaluation notices limit merchant accuracy, and real corpus,
image OCR/camera, raw main-store equality, other native/platform and release acceptance remain open.
See [physical evidence](quality/physical-digital-receipts.md).


## D-325 observed native OCR accent drift

Actual Samsung image OCR exposed French À -> Å in NET À PAYER while the number/unit remained correct.
D-325 folds Latin accents only for whole keyword/exclusion comparisons, preserving original source,
non-Latin marks and all numeric/currency/refusal rules. Ten added behaviors reproduce the failure and
retain safety; all 118 related cases pass, followed by test cleanup and zero-warning Windows/complete
signed offline Android builds. Same-file French recognition now gives 24.50 with original Å source;
seven original raster cases include German EXIF and blank missing-total/currency-conflict drafts.
Three pre-fix successes are retained; four final cases and upgrade preserve five rows/Home 979 without
Save/reset. Exact owned inputs and diagnostic package are removed. Real corpus/camera/scanned/iOS and
release gates remain open; a separate dangling transfer-summary caption is observed and queued next.
See [physical evidence](quality/physical-receipt-ocr-accents.md).


## D-326 — Complete transaction summaries without an empty total

D-326 fixes the observed transfer-only transaction summary: show the existing translated count-only
caption when the actual result-currency dictionary is empty. Retain a real zero result, separate currency
packets, original matching rows and unchanged income/spending/transfer semantics. No new string, schema,
financial write, dependency or native cipher change is introduced. All 18 focused application-source cases
pass across six languages, followed by test cleanup; the Windows and complete signed offline Android
builds have zero warnings/errors. The independent actual Windows page passes 18 en/fa/de light/dark
360/412/1280 contexts with complete scaled captions and unchanged stored inputs. Six successful English
contexts are retained; only the remaining contexts continue after correcting the diagnostic's Persian-digit
comparison and hidden-root measurement order. The original Samsung upgrade shows transfer count 1 with
no dangling total, expenses 3/net -21, income 1/net 1000 and final Home 979 without Save/reset. Phone
preservation is displayed-state evidence, not raw main-store equality. Remaining receipt corpus/camera,
performance/cold/ANR, iOS/provider/Store and release acceptance remain separate.
See [qualified evidence](quality/transaction-summary-result.md).


## D-327 — Await automatic readiness through native maintenance

D-327 qualifies the original physical automatic-copy lifecycle on SM-A176B and fixes an observed
maintenance overlap. A synchronous onboarding-readiness read raced the legitimate exclusive safety-file
discovery; the native type/source stack identifies DatabaseAccessGate.EnterRead through GetSettings.
Use cancellable GetSettingsAsync readiness, recheck the captured profile set and preserve the original
publication/encryption/interval guards. A successful not-due check clears stale transient feedback without
replacing its file/timestamp. Seven focused cases pass (six together, one final scope case alone; four
added cases), test outputs are cleaned once and strict Windows/final signed offline Android builds pass
without warnings/errors. Twenty-three saved-native observation assertions cover Off/no copy, Daily
creation, one-entry preview, explicit 36-to-37 recovery, separate two-entry safety preview, cold persistence,
final Weekly/Daily without the former error, original-copy preservation and retirement of only the new
fictitious profile. Original successful creation/restore is retained, not repeated after the readiness fix.
Final Main is 979 with its three original profiles/copies and original Daily/password choices; the phone
is released. No native crypto/schema/SDK/permission change or full-suite rerun. Raw main-store/key equality,
power loss, key loss, full hardware retention stress, other devices/iOS/OAuth/Store and release remain open.
See [qualified evidence](quality/automatic-copy-readiness.md).


## D-328 — Qualify original physical accountant ZIP disclosure

D-328 completes a scoped physical Germany accountant export on SM-A176B using the unchanged signed offline
D-327 app. The original native chooser delivers two readable ZIPs only to a separately installed offline
receiver: both accounts/five rows, then cash only/the same one transfer plus its unselected Main identity.
The host verifier checks actual CSV/manifest bytes, six member digests, default redactions, blank tax fields,
refund/fee references and separate recorded-kind totals. Empty selection, confirmation Cancel and stale-review
invalidation are exercised. Twelve saved-observation/receipt assertions pass; the receiver strict build has
zero warnings/errors. The new receiver is removed, final Home remains 979 and the phone turn is released.
No production code/APK, schema, permission or managed test count changes; successful unrelated tests/builds
are not repeated. Physical documents/notes-enabled/all-layout, iOS, tax specialist/calculation/filing and
release acceptance remain open.
See [actual packet evidence](quality/physical-accountant-export.md).


## D-329 — Keep financial context on continued PDF pages

D-329 fixes an actual anonymous balance row on page2 of the original Samsung report PDF. Repeat each
table's account/section title, exact scope and column headings as one grid header, retaining all original
values, RTL mapping, row membership and footer exclusion. Three new en/fa/de continuation cases reproduce
the original failure; all eight related Reports cases pass. Strict Windows and the complete signed offline
Android build have no warnings/errors. Data-preserving upgrade and six pages from three original native
English/Persian/German PDF exports are checked; final Home979 and original display choices remain. The
receiver is removed and phone turn released. Seven saved-native/receipt assertions and an original-byte
host verifier retain actual context/figures/visible footer evidence. A suspected footer defect is disproved
by pixels; its experimental source/build are withdrawn and not installed. Initial stale uncompressed
Samsung chooser coordinates opened an unrelated app; no further interaction occurred there, and temporary
fictitious-file grant absence is not proven. Final selection uses a fresh compressed hierarchy and exact
local receiver. No financial/schema/native-crypto change or full-suite/performance replay. Wider device,
reader/iOS/provider/Store and release gates remain open.
See [physical PDF evidence](quality/physical-report-pagination.md).


## D-330 — Qualify original CSV export/import and cold Undo

D-330 qualifies the original Samsung CSV producer/consumer roundtrip on the unchanged D-329 offline APK:
five original rows, all27 exported fields/stable IDs equal after re-export, retained refund/transfer/fee
relationships, disabled five-ID duplicate import, cold original batch and explicit Undo to zero. Only the
new matching-account profile is retired; Main979 and three previous profiles remain. Twenty-three saved
observation assertions pass; only the independent offline receiver is rebuilt with zero warnings/errors.
No maintained unit count, main source/APK or unrelated successful suite/build changes. Bank/aggregate/
malformed-file/full-device/iOS/release gates remain separate.
See [physical roundtrip](quality/physical-csv-roundtrip.md).

## D-331 — Expose interpreted ledger CSV rows before import

Close D-330's observed count-only IO-09 presentation gap with bounded ten-row pages containing actual
dates/kinds/accounts and separate source/destination/original-currency amounts. Retain invalid and known
rows, current duplicate choices, cancellation and accepted-outcome retirement. A changed generic mapping
invalidates the old review until explicit refresh; no financial writer/parser/schema change. Nineteen new
behaviors and the related51 application/29 bank/23 resource cases pass. Complete signed offline Android
build/upgrade pass; phone rendering remains pending after adb went offline. All18 native Windows contexts
pass with900 complete scaled captions and zero ledger writes; failed review attempts are preserved in
[evidence](quality/interpreted-csv-preview.md).

## D-332 — Return CSV page navigation to the review heading

Actual Android Next retained the old bottom offset and exposed row20 before row11. Signal only actual
published page changes; the appearing page dispatches a scroll to its review section and unsubscribes
when leaving. One focused regression,18 native Windows contexts/36 arrivals, strict builds and the
complete signed offline candidate pass. Original Android Next/Previous, last invalid row/disabled Next,
stale generic interpretation/refreshed income and unchanged Home0/current-month empty history pass.
Eleven saved-observation assertions retain this x64/API36 English/412x800 boundary. No previous full or
unrelated suite is replayed. Two real startup ANRs originate in ScheduledAlarmReceiver launches; Wait
preserves each process, without establishing a cause or fix. Samsung remains absent. Exact evidence:
[native CSV arrival](quality/android-import-preview-arrival.md).

## D-333 — Preserve protected notification snoozes after process loss

A real application-source/SQLite regression reproduced four valid pending notifications becoming zero
when a cold snooze read an unpublished protected endpoint. Use the original storage startup on a worker
before the read; missing/refused/unpublished startup and disposal preserve the queue and pending draft.
Keep original financial/source/privacy guards and no storage work in Application.onCreate. One hundred
related cases pass, followed by only the four final changed-state cases after the disposal correction.
Actual original Android action starts a new process with zero windows, prepares real protected storage,
saves/schedules exactly one snooze and preserves fixture amounts/identities/zero ledger. A separate cold
metadata read retains that snooze. Strict builds, full signed offline main upgrade and original Home0 pass.
Startup ANRs remain unresolved; temporary profiling does not justify changing licence/telemetry order or
repeating AOT capture. Samsung is absent; broader device/iOS/lifetime/release gates remain open.
See [native action and retained failures](quality/cold-protected-reminder-startup.md).


## D-334 — Reject unqualified startup packaging alternatives

Both finite packaging alternatives are rejected: disabling assembly compression fails before
managed startup with a missing compressed_assembly_count native symbol; direct native-library loading
records a real binding ANR and takes24.422s through OnCreate versus the17.755s control. Single samples,
native rebuild differences and public memory counters do not establish the original root cause.
All temporary source/manifest changes are removed; ordinary application/APK remain D-333. No unchanged
successful suite is repeated or new test count claimed. QA-06 and wider device/iOS/release gates remain open.
See [exact failures and cleanup](quality/android-startup-packaging.md).


## D-335 — Complete growing More menu captions

The More menu now grows complete titles and explanations at native font scale instead of
inheriting compact title ellipsis and a two-line explanation limit. The original360/200% native case
reproduces14 trimmed captions. Both offline and personal-sync editions pass18 en/fa/de, Light/Dark,
360/412/1280 contexts each:1224 native caption observations and612 original command targets. Commands,
routes, fonts, semantic names/hints, palette and financial data flow are unchanged. No unrelated successful
suite is repeated or new unit count claimed. Broader A11Y-03, real OS scaling/readers, phone/iOS, startup
ANR and release gates remain open.
See [native evidence](quality/more-menu-readable.md).


## D-336 - Validate incremental Android compression headers

The observed pre-managed assembly-store abort comes from a cached CoreLib header retaining
index193 while the current descriptor table assigns CoreLib192 and Java.Interop193. Validate the
SDK's current compression-item metadata before its incremental timestamp check and regenerate only
incompatible owned cache entries. Seven focused behavior cases preserve valid cache timestamps,
invalidate stale index/size/magic/truncation, accept missing files and refuse outside destinations.
Keep the original SDK compression, serviced runtime, AOT profile, key/data and permissions. No unrelated
successful financial, OCR, backend or More-layout suite is repeated or new managed count claimed.
QA-06 startup ANR, two-second loaded Home, broader Android, Mac/iOS and owner release gates remain open.
[Evidence, failures and limits](quality/android-compression-cache.md).


## D-337 - Scope the startup clock bottleneck

Exact-PID, frame-pointer native captures retain the actual background startup ANR. Monotonic
phase filtering places75.34% of888 main-thread OnCreate samples in the clock-reading path; an
isolated native executable reproduces expensive precise clocks without application code in both
original and fresh same-image emulators. Broken callchains and single-sample limits remain explicit.
No clock substitution, kernel restriction, runtime/licence change or speculative app workaround is
shipped. The original AVD/data remain; ordinary D-336 signed APK and same-PID Home are restored.
App code, APK and managed counts remain D-336; no unchanged successful suite/build is repeated.
QA-06/phone/iOS/release remain open. Continue ready release preparation independently of those gates.
[Evidence and limitations](quality/android-startup-clock-cost.md).

## D-338 - Isolated six-language store drafts

Eight current native Windows draft screenshots in each existing language (en/fa/de/es/fr/it)
are delivered with exact RGB/source hashes and independent fictitious package/storage/non-input
desktop isolation. Five actual native pre-startup refusals, strict Windows/Android Debug builds and
48 visual/pixel checks passed. Only the rejected English budget loading frame was recaptured from
the same fictitious data; 47 successful frames were retained. Shipping UI/APK and managed counts
remain D-336. These are development drafts, not mobile or Store acceptance/publication. QA-06,
broader physical Android, Mac/iOS, provider/accounts and owner release gates remain open.

[Evidence and limits](quality/store-drafts.md).

## D-339 - Complete report legend identities and amounts

Category/composition report names, shares and monetary packets occupy independent growing rows.
Names retain native scaling and complete wrapping; oversized amounts use the existing horizontal
readout, outside the original category navigation button. The 18 final Windows contexts (en/fa/de,
Light/Dark, 360/412/wide at process-local 200%) and six original Release/AOT Android contexts pass
native glyph/viewport/scroll/target checks. Fictitious presentation inputs are explicitly separate
from report calculations; no financial commands execute. Strict builds and the full offline signed
APK pass. The Android caller's initial startup ANR is retained: Wait continued the same original PID,
without restarting its scenario. QA-06/ANR, broader A11Y/OS readers, physical Android, iOS and release
gates remain open. Managed test counts are unchanged; unrelated passed suites were not repeated.

[Evidence and limits](quality/report-legends-readable.md).

## D-340 - Complete default row titles and Flex bill captions

The remaining inherited RowTitle ellipsis truncates the original Non-monthly bills caption at
200% Windows process-local scale/360 width. The shared default now wraps with its original 15-unit
font and semantic color. All 26 other current consumers already explicitly wrap, so their effective
behavior is unchanged. Eighteen final Windows contexts/36 native captions and six Release/AOT
Android contexts/12 captions pass; original account/entry/budget inputs stay unchanged and no method
is saved. Strict builds and a complete signed offline APK pass. The independent Android startup
ANR remains an actual failure; Wait continued the same PID 1753 to its completed caption result.
Actual OS text scale/TalkBack, broader device/iOS/performance and release gates remain open. No
system setting, financial algorithm, schema, translation or managed unit count changes.

[Evidence and limits](quality/budget-group-titles-readable.md).

## D-341 - Repair fresh Data-test compilation and reject unproven Home connection reuse

The standalone Data-test project linked the original ReminderService but omitted its actual
IAppDatabaseStartup dependency, causing fresh CS0234/CS0246 compilation failures. Link the same
original source already compiled by application-flow tests; do not invent a substitute contract.
The complete Home connection experiment passes financial preservation but fails reliable Android
loaded-Home improvement, including after profile-identity and cache/disposal corrections. Withdraw
all experimental application/benchmark APIs and eight experimental tests. Main remains D-340;
QA-06 and external acceptance stay open. Windows-only isolated gains are insufficient for adoption.

[Evidence and limits](quality/home-publication-session.md).

## D-342 - Complete optional Home category identities and values

Replace the constrained four-column category legend and chart-hole total with growing name/share
rows and original AmountReadout viewports. Preserve the real command/parameter, scalable fonts,
original financial packets and chart values. Attach the complete category source only while visible.
Eighteen Windows and six original Release/AOT Android contexts pass; preserve the failed RTL
verifier and continue only its remaining cases. Stored inputs remain unchanged and no layout is saved.
No financial algorithm, schema, translation or managed unit count changes. QA-06/ANR, OS readers,
physical Android, iOS and release remain open.

[Evidence and limits](quality/home-category-legends-readable.md).

## D-343 - Preserve complete plan-row monetary packets

The original shared PlanRowView splits an extreme amount and even EUR across lines. Use the existing
AmountReadout at the original scalable typography/color, outside the unchanged real navigation target.
Both Home upcoming and Plans hosts pass18 Windows and six Release/AOT Android contexts. Original
commands, native accessible identities and stored inputs remain intact; no financial calculation changes.

[Evidence and limits](quality/plan-row-amounts-readable.md).

## D-344 - Complete ledger/account packets and retain the Windows renderer boundary

Replace six constrained monetary captions across Home recent/accounts, grouped transactions/day net,
and shared account/group templates with existing AmountReadout surfaces. Preserve original actions,
selection, colors and typography. Eighteen retained Windows contexts and six native Android contexts
qualify the component changes with unchanged stored inputs. The separate Windows Win2D root-change
AV remains open. Reject the ineffective retired-handler cleanup and remove temporary observers.

[Evidence and limitations](quality/ledger-account-amounts-readable.md).

Owner follow-up on 2026-10-18 authorizes temporary accessibility settings only on the independent
Zanance emulator with restoration to exact prior values. It excludes Samsung, Windows, accounts
and authentication. This decision unblocks the next real OS font-scale/TalkBack qualification;
D-344 component checks above did not change those settings or establish that acceptance.

## D-345 - Actual emulator OS text scale and unaccepted TalkBack attempt

Apply the owner's scoped emulator accessibility exception. The existing independent Release/AOT
caller passes36 original monetary viewports across six language/theme cases at actual OS2.0/412dp,
with original stored inputs unchanged. Restore captured font/accessibility settings exactly. Preserve
the same-PID startup ANR/Wait and unaccepted TalkBack audio/prompt/connection evidence; no source or
APK change and no unrelated successful verification replay. Notification permission stays denied;
uncaptured permission flags are not claimed restored. A11Y-01/03 remain In progress.

[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-346 - Preserve actual OS cohorts and reject an unproven runtime workaround

Qualify remaining monetary/caption surfaces at real emulator OS200%/412dp on the retained fictitious
fixture. Preserve completed immutable cohorts and actual native/ANR failures instead of restarting
a live process after observation expiry. Exact matching public runtime symbols locate a Mono bridge
SIGSEGV; a supported serial-major GC trial still stalls with an actual ANR and is not adopted.
The native TTS control can speak, but the renewed TalkBack client path still produces silent output.
Production source, native libraries, complete APK and managed test count remain D-344. A11Y-01/03
and QA-06/07 remain open; no unrelated successful suite or build is repeated.

[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-347 - Require actual foreground focus for native financial checks

The original observer can measure realized controls while Android's ANR dialog covers the app.
Require Activity window focus before original root/context construction and every native text
assertion, bind it into new receipts, and reject older receipts without that proof. Preserve a
negative pause and positive continuation on the same process. A separate maintainer-guided
precise-Tarjan trial still has a persistent actual mid-budget ANR and is not adopted; its temporary
environment/property/marker are removed. Main runtime, financial algorithms and APK remain D-344.

[Evidence and limits](quality/android-os-accessibility-qualification.md).

D-347 final ordinary follow-through passes24 Home-category/total and original Home/Plans plan-row
amount viewports across12 en/fa/de xLight/Dark contexts at actual OS200%/412dp with real Activity
focus and unchanged stored inputs. These two original runs have no observed ANR through completion;
this is not a general runtime/stress fix. Captured settings restore and MainPID7654 is preserved.

## D-348 - Preserve the real reader while observing accessibility

Installed framework bytecode proves stock uiautomator connects with default service suppression.
Add an independent bounded shell observer using the explicit preservation flag; serialize clients,
handle missing shell looper/window metadata and decode Unicode correctly. Exercise retained fictitious
Main with actual TalkBack swipes after audio packets arrive, observe Recorded balance accessibility
focus and captured audio, but reject unreliable offline transcripts. Exact settings/permission
restoration, retained MainPID and hash-matched remote cleanup pass. No Main/APK/managed-count change
or unrelated successful verification repeats. Spoken-reader and native ANR acceptance stay open.
[Evidence and limitations](quality/android-os-accessibility-qualification.md).

## D-349 - Reject injected navigation as reader acceptance

The real Main entry/cancel assessment shows that ordinary adb tap directly opens an unedited draft,
without proven TalkBack focus. Refuse further activation, cancel only through original UI after
restoration, and preserve the same MainPID/data. Console key/touch trials do not establish delivery;
direct kernel input is denied and is not escalated. Exact settings/notification state and Home0.00EUR
restore; owned remote files are removed. Strengthen input-evidence and independent-cleanup rules.
No product/API/runtime/APK/count change or successful unrelated test/build replay.
[Actual observations and remaining acceptance](quality/android-os-accessibility-qualification.md).

## D-350 - Keep original date parsing after complete native comparison

Add fifteen independent-EF date compatibility/refusal cases and a real foreground contract for each
measured original Home page. Reject legacy unguarded case receipts. The temporary ASCII-date decoder
preserves all rows but has no consistent complete loaded-Home benefit, so both production reader files
are restored. Qualify the original-getter control in twelve native observations, retaining all actual
startup/Main focus ANRs and verified Android process deaths. New cases pass on the restored getter;
no full-suite run/count or new production APK is claimed.
[Evidence, exact scope and open gates](quality/home-date-materialization.md).

## D-351 - Index open reimbursements without repeated history scans

A deterministic original-code regression proves quadratic history enumeration for multiple reimbursable
expenses. Build a per-call refund index for only positive eligible expense identities, preserving checked
intermediate sums, stable oldest-first ordering, duplicate identities and original entry objects. Keep
the single-entry API as the independent amount oracle; do not drop any ledger fields or introduce cached
financial results. The shared Core change serves Home, Reimbursements, net worth and KPI consumers.
Fifteen new regression cases and the affected existing reimbursement/report/KPI cases pass in one focused
39-case run. No unchanged full suite is replayed or full-suite count inferred. No UI design, encryption,
key/recovery, schema, dependency or runtime change is included. Large-list rendering and QA-06/ANR remain
open separately from this algorithmic correction.
[Native follow-through and exact qualification limits](quality/reimbursement-batch.md).

## D-352 - Complete reimbursement-page loading and virtualized presentation

D-351 removes the shared quadratic aggregation, while the page still creates every native row and
publishes individual additions. Read complete facts on a worker, prepare every row and currency total,
then publish one complete snapshot through the existing loading/failure/retry coordinator. Use a bounded
CollectionView with growing identity and independent complete monetary viewports; refuse old/covered
navigation parameters. Actual Windows images reject Header/EmptyView overlap, so empty content uses a
mutually exclusive sibling ScrollView with sequential introduction/card layout. Require actual visible
native edges and no overlap rather than treating allocated bounds as visual acceptance. Keep financial
rules, history, provider/keys, schema, dependencies and unit-suite counts unchanged. Preserve actual
Home/native failures and scoped failed checks; do not reinterpret them as runtime fixes.
[Final evidence, retained failures and limits](quality/reimbursement-page.md).

## D-353 - Preserve original native Home templates on reload

The retained live-Home COM failure reaches QuickTemplates.Clear and native BindableLayout teardown.
Prepare fresh rows before one existing SnapshotCollection Reset, preserving reviewed identity/order,
metadata, enablement and command parameters. Verify unchanged native controls and legitimate shrink/empty/
restore transitions; do not suppress exceptions or claim a general runtime fix.
[Cause, negative baseline and qualification](quality/home-template-publication.md).

## D-354 - Withdraw slower complete-row packet; retain actual storage regressions

A bounded JSON transport keeps complete native data but regresses the100,000-row Android public read
and Home input. Withdraw it before main adoption; restore production/harness sources and keep nine new
provider-compatibility/error-order regressions against the original reader. Do not reinterpret a smaller
reference sample or passing unit tests as a speed result. [Exact evidence](quality/complete-history-row-packet.md).

## D-355 - Grow Android selected picker captions at actual OS text scale

Actual OS200% Settings exposes clipped native numeric-format/default-account captions. Preserve the
original read-only picker and dialog while allowing selected text to wrap and height to grow. Keep
font scale, selection, keyboard restrictions, direction and semantic names. The independent caller
uses fictitious Settings without invoking security or financial actions.
[Cause, retained failures, final checks and limits](quality/android-settings-os-text.md).

## D-356 - Complete original editor choices and disclosure captions

One real OS-scale matrix across six original editor states exposes debt disclosure, decorative
default-icon and intrinsic account-chip clipping. Separate chip drawing from text allocation,
reuse growing debt actions and keep only the drawn icon glyph unscaled. Preserve the original
commands, selected names, financial drafts/data and owner-authorized OS restoration.
[Negative baseline, measurements, final checks and limits](quality/android-editor-os-text.md).

## D-357 - Report the compression regression process result correctly

Hosted Windows jobs from D-336 through D-356 print the successful compression assertions and then
exit 1. The final native MSBuild call deliberately rejects an outside-cache path; PowerShell retains
its LASTEXITCODE for the runner epilogue. Exit successfully only after every positive and negative
assertion passes. Do not ignore native failures, remove the refusal or change the compression target.
The runner-equivalent baseline exits 1, the corrected process exits 0, and an injected assertion
failure still exits 1. Older Linux/Android missing-IAppDatabaseStartup compilation failures were
already corrected in D-341; both jobs pass at D-356. No application/native bytes or APK change.
Hosted verification follows this commit; it is not inferred from the local process check.

## D-358 - Preserve complete measured choice surfaces

Actual OS200% goal forms expose fractional Flex shrink after correct caption measurement.
Keep each existing chip's measured width with Shrink0 so the whole surface wraps without changing
fonts, padding, selected state, translated names or commands. Reject ineffective custom measurement
trials before production. Continue changed/missing native cohorts and retain the actual independent
process finalizer failure as an open runtime issue.
[Cause, exact cases, correction and limits](quality/work-editor-os-text.md).

## D-359 - Track optional Android manifest overlays during incremental builds

The requested independent profiling flag is absent from a successful incremental APK: the installed
Android SDK merger inputs omit project AndroidManifestOverlay items. Fingerprint ordered full paths
and content hashes, including the empty list, in an unchanged-preserving intermediate file and add
it to the merger's existing input list. Missing overlays fail; no SDK edits, forced clean/merge,
permission changes or production profiling are introduced. Seven focused process cases and real
signed profileable-on/off APK checks pass. Windows strict build is warning-free; final ordinary
APK verification is recorded with the complete evidence. Managed case counts are unchanged.

Bounded original binding/goal/Shell retirement retains actual ANRs and completed stored-input
checks. Exact-symbol native samples locate GC/JNI work without proving the finalizer's root cause;
no runtime/event/GC workaround is promoted. Temporary diagnostic source is removed. Scoped Samsung
upgrade, English Simple-goal Cancel and Home979 pass without Save or settings changes. QA-06,
broader A11Y/phone/iOS/release remain open.
[Original failures, correction and qualification](quality/android-manifest-overlay-incremental.md).

## D-360 - Release owned native fragments when retiring Android navigation

Actual root replacement retains eight AppShell trees and grows translated toolbar subscriptions
from one to nine. Removing a subscription alone does not release the roots. The Activity fragment
manager retains original item fragments after ShellRenderer disconnects. Track only weak references
to fragments created by this renderer and remove them at the existing managed retirement boundary;
leave destruction/disposal to Android. Pending initial adds must also be removed through reordered
transactions. A candidate that missed that boundary actually crashed and is not promoted.

Eight final native root-retirement groups and six original translated Back operations pass with
stored inputs and original preferences preserved. No financial algorithm, schema, runtime engine
or GC policy changes. This is a scoped ownership correction, not proof that every ANR/finalizer,
physical device, reader, iOS or release gate is closed.
[Original failures and qualification](quality/android-retired-navigation.md).

## D-361 - Qualify original physical display navigation and unsaved goal choices

The complete D-360 signed APK upgrades Samsung without reset. Original Settings language/theme
choices and translated Back cover en/fa/de and Light/Dark at the unchanged device scale. The
original advanced goal type/contribution choices and dirty Keep editing/Discard flow pass without
Save. One native root remains; initial English/Light/Simple and the exact Home hierarchy/account/
balance979 are restored. Fifty-one stored-observation assertions are not new managed cases.
App/APK/builds remain D-360; no unrelated successful verification repeats. General ANR/finalizer,
full financial/device/reader/iOS/release gates remain open.
[Original physical evidence and limits](quality/physical-navigation-and-goal-drafts.md).

## D-362 - Isolate original managed startup and reject unsupported model warming

Independent fixed markers separate original Application construction from protected storage, first
settings and lock reads. The original Home sample spends 443ms in the Application callback and
777ms in the first settings call after protected preparation. That call still includes actual
context construction and query work; no SQL-only cause is claimed. A bounded immutable-model
overlap initializes the model in 46ms and does not improve application-entry-to-ready (2747ms
original versus 2779ms candidate). Reject it and restore all original source/package bytes;
retain the same zero-balance independent Home. Application/APK remain D-360, physical evidence
D-361. No general startup/ANR/two-second acceptance or new managed test count follows.
[Actual phase boundaries, candidate and restoration](quality/android-managed-startup-phases.md).

## D-363 - Read complete startup settings without cold LINQ preparation

The original split measures context9ms/pre-command643ms/command4ms, so row SQL is not the dominant
first-settings cost. Replace only the two asynchronous earliest-row queries with a guarded complete
25-field reader on the original EF relational command. Retain converters, identity/nulls/order,
interception, cancellation, transaction, fresh app-lock preferences and maintenance/creation ownership.
Refuse future mapping/filter drift; cache only weakly owned immutable model metadata, never settings,
contexts or keys. Whole original Home publication is measured because shared EF cost moves later:
paired Application-entry-to-publication reductions are297ms and222ms on the retained emulator.
Eight new managed cases and two existing settings concurrency cases pass in selective runs. Final
Release/AOT reference/tenfold protected reads match all25 fields and complete schema/typed rows,
including exclusive maintenance, cancellation and refusal of retired generation key owners.
Remove all temporary phase code. No schema/SDK/KDF/licence/access fallback change or general
startup/ANR/two-second/device/iOS/release acceptance follows.
[Exact comparison, preservation and handoff](quality/settings-startup-reader.md).

## D-364 - Preserve complete minimum-width editor and wizard actions

Actual Android OS200%/360dp qualification of the original twelve editor states finds clipped
German icon and monthly-pattern disclosures. Reuse WrappingAction with the original commands,
full native names and scalable captions in account, debt-details, plan and onboarding consumers.
Retain the centered decorative icon tile. Windows image review additionally finds a clipped final
wizard action; reflow whole Back/Next/Start surfaces in a growing footer, preserving visibility,
busy disablement and original commands. Never shrink text to pass the review.

An editable goal name exceeds its viewport but is not a clipped static label: qualify both native
caret/ink endpoints and restore original text/selection. Keep the original input scrolling policy.
The 72-case baseline, 24 input endpoints, 24 changed-action cases and 18 Android footer cases use
retained fictional data; Windows changed-action qualification has 18 contexts. Final Windows
footer continuation and package evidence are recorded separately. Native observations add no unit count; three constructor regressions are new. No
financial rule, schema, permission, language or dependency change follows.
[Original failures, scoped continuations and remaining gates](quality/narrow-editor-actions.md).

The actual wizard constructor also reapplied an unchanged saved theme before modal publication,
queuing a competing Shell replacement. Skip only that redundant application; user changes still
apply. Three new real-source regressions first show two failures and then all pass, alongside the
existing open-draft language/theme case. Initial native footer assertions are insufficient where
the corresponding step2 image shows Home: reject those nine contexts and qualify their actual
rendered original modal under the corrected owner checks. Keep all earlier failures/receipts.

## D-365 - Qualify original changed disclosures on Samsung

Reuse the complete signed D-364 APK after coordinating the phone turn. Upgrade without reset and
qualify original account, expanded debt-details and monthly-rule actions in en/fa/de Light/Dark:
18 normal-scale forms, 36 disclosure clicks and 54 actual native name/target states. Preserve draft
inputs and original calendar/day panel behavior; discard unsaved monthly drafts without Save.
Keep the original process, visible account/plan/Home data and exact OS settings; restore English,
Light and Simple. Retain and correct only failed observations, not successful interactions.
This is physical scoped qualification, not new app/APK/managed tests, OS200% phone, first-run,
spoken reader, general ANR/performance, full database or release acceptance.
[Original physical evidence and limits](quality/physical-editor-actions.md).

## D-366 - Qualify retained original surfaces at actual minimum width

Complete seven original financial/Settings groups in en/fa/de Light/Dark at real Android
OS200%/360dp: 42 cases on the original fictitious financial and Settings fixtures. Reuse
original page readers with one explicit context; do not repeat successful 412dp/editor cohorts.
Bind each immutable case to exact comparison, source MVIDs, input hash and configuration;
retain failed evidence and require original native window focus. Preserve captured inputs,
original commands/selection/Cancel, restore exact OS values and original Main Home.
Only independent qualification sources change; production app/APK and managed counts remain.
[Exact matrix, observations and remaining gates](quality/narrow-original-surfaces.md).

## D-367 - Exclude decorative glyph descendants from Android readers

Actual TalkBack focused the private character below an already excluded SymbolIcon parent.
Exclude its descendants in the global decorative style, preserving meaningful labels/action owners.
Qualify original expense/Cancel, More, About/Back and Home in six native Settings-bound en/fa/de
Light/Dark contexts:24 scenes,36 real reader activations and209 original trees. Preserve controller
failures and reject the unbound-theme run; retain successful scenes across observation corrections.
Strict Windows/full offline Android builds pass. Restore exact authorized emulator settings,
original English/Dark/Simple Home and remove the independent service. No financial Save or new
managed tests; correct speech/general ANR/physical/iOS/release remain open.
[Original defect, exact evidence and limits](quality/android-reader-glyphs.md).

## D-368 - Qualify the original reader glyph correction on Samsung

Upgrade the signed D-367 package without reset under a coordinated exclusive phone turn.
Nine original decorative glyph nodes disappear while30 meaningful Home nodes retain original
names/roles/geometry. In one retained process, qualify expense/Cancel, history/Home and More/Home
with six native actions and no Save. Keep original979EUR/profile and exact system settings;
release the turn after terminal evidence. Preserve the initial unavailable-focus observation,
continue the same process and make no precise startup or phone TalkBack/speech acceptance claim.
App/APK remain D-367; no successful unchanged builds or managed suites repeated.
[Actual device scope and limits](quality/physical-reader-glyphs.md).

## D-369 - Qualify original Android fee edits and retire owned currency surfaces

The original Android editor completes 12 translated/theme/mode contexts and 36 native fee Saves,
retaining nine successful original Saves and executing only 27 remaining operations. Compare exact
fee identities, net effects and every unrelated serialized field; keep original Cancel policies.
The first terminal process exposes SfComboBox finalization removing an owned chevron from a
disposed Android group. Detach only that child on Android Unloaded, restore on Loaded while
ownership is unchanged, and retain cleanup. No GC/runtime/dependency or financial algorithm change.
Keep exact-source receipts, seven pending migrations on the original empty 43/50-schema fixture,
failed observations and corrected host UTF-8 expectations. Strict Windows/full signed offline
Android builds pass. Six original main contexts complete 24 currency open/close pairs before/after
child navigation; EUR selections, exact original Home/OS state and installed bytes match. No new managed tests; broader GC/ANR/phone/iOS/release gates remain open.
[Original sequence, package and bounded main qualification](quality/destination-fee-android-lifecycle.md).

## D-370 - Qualify original currency-button return on Samsung

Reuse D-369's complete signed APK without reset. Six en/fa/de Light/Dark contexts complete 24
original popup open/close pairs for both currency inputs before/after Settings child navigation.
Require owned native windows, actual phone geometry, unchanged EUR, original English/Light/Simple
Home and exact system state in the same process. Preserve initial focus/hierarchy uncertainty and
the corrected host coordinate bound; no app restart repairs an observer. No financial Save, new
managed tests, source/APK change or repeated unchanged build/suite. Release the coordinated phone
turn after terminal evidence. Physical fee Saves, general GC/ANR/readers/iOS/release remain open.
[Original device evidence and limits](quality/physical-currency-lifecycle.md).

## D-371 - Qualify original stored-fee edits on Samsung

The independent original Release/AOT ARM64 editor completes 12 en/fa/de/theme/mode contexts and 36
native Saves with exact retain/edit/remove fee identities, localized effects, unrelated serialized
fields and original Cancel policies. Original physical transport/model, signed bytes/PID and source
MVIDs bind the evidence; the fixture is standalone SQLite, not main encrypted-profile acceptance.
Two observation deadlines retain the same process and completed cases; the third reads success.
The independent build has zero warnings/errors. Stop only the independent caller after terminal
proof, preserve its fixtures, restore unchanged main PID/Home/profile 979 EUR/system/APK and release
the phone. Zero new managed tests; no unchanged successful main build or full suite repeated.
[Bounded original hardware evidence and limits](quality/physical-destination-fee.md).

## D-372 - Reject current-row native transport after paired original Home

Keep the original complete reader and main D-369 APK. A bounded native 32-field prototype passes
57 real-SQLite protocol cases, strict independent Android build and protected full-row comparisons,
but paired original Home medians are 761.11 ->773.59ms reference and 2472.96 ->2392.47ms tenfold.
Mixed whole-page results do not justify another native/unsafe/build boundary. Preserve historical
presentation refusal, use a fresh unweakened original control, retain live processes across
observation deadlines, restore original independent bytes/main PID/Home/OS and ordinary Data output.
No managed-count, product source/APK or unchanged full-suite repetition. QA-06 remains open.
[Exact experiment, original paired observations and withdrawal](quality/home-native-row-transport.md).

## D-373 - Index unreviewed scopes and reject overflowing totals

Replace per-entry full-account scans with one local lookup and ordered endpoint-group merge.
Preserve repeated account records, legacy/self/destination metadata, ordinal currency counts and
original EffectOn/checked within-entry sums; additionally refuse cross-entry long overflow rather
than wrapping. Fourteen new Core cases and all 48 related cases pass; original Release/AOT x64 and
Samsung ARM64 each refuse both boundary inputs. A distinct protected unreviewed corpus preserves
all other typed fields, context write guards and old fixtures/keys. Paired complete Home tenfold
medians reduce 3030.53 ->2613.96ms emulator and 7535.41 ->6500.89ms Samsung; reference is mixed/flat.
Retain both actual old/new native GC failures and continue only unfinished same-binary cases.
Strict Windows/full signed offline Android, no-reset main upgrades and exact native/OS restoration
pass; release the phone turn. No runtime/GC or crypto weakening; broader QA-06 remains open.
[Detailed failures, complete retained comparisons and packages](quality/unreviewed-account-scope.md).

## D-374 - Count an FX transfer once in Home review notices

The original Home expression sums currency review groups, incorrectly showing two transactions for
one EUR/USD transfer. Use a dedicated operation-local scope counter for both Home notices; preserve
all currency effects, checked sums, dates and explicit/default scope. Seven new/36 related cases pass.
Original publication passes18 Windows, six emulator and six Samsung contexts with complete fixture
field preservation. Preserve the Persian native-digit observer failures, then continue the same
fixture and retain successful English cases; the application's digit mapping does not change.
Only the changed candidate's complete protected Home is measured against retained D-373 results.
The tenfold median2714.35ms is not an accepted speedup or two-second result. Strict builds/full signed
offline APK and no-reset main upgrades pass; original Home0/979 and exact settings are restored and
the phone is released. No schema, permission, financial formula or layout change; wider gates stay open.
[Source, native evidence and limits](quality/home-review-count.md).

## D-375 - Move Home Add beside the title

The owner requested reclaiming Home's full-width bottom action band, allowing a floating button or
compact action beside the title. Choose the title area: one growing header replaces the native page
title bar, with the original Add action at its physical right, reduced locally from 56 px to 48 px.
Preserve Loading.IsReady, semantics, financial sections and other pages' docks. Eighteen Windows and
six OS 200%/360 dp Release/AOT Android contexts pass with complete fixture-field preservation. Main
emulator/Samsung native open/Cancel and no-reset upgrades pass; physical viewport gains 87.82 dp.
Retain Windows modal COM and diagnostic focus failures; do not call wider platform faults fixed.
No managed unit count increase or unrelated suite/financial benchmark repetition.
[Exact evidence and limits](quality/home-add-header.md).

## D-376 - Complete the named Home getting-started tasks

D-375's original FX-only screenshot exposes an incorrect completed first-expense step. The same
record-count shortcut marks any Once plan as a regular payment and a weekly budget as a monthly one;
all three unrelated records can hide guidance. Resolve the exact saved kinds in GettingStartedProgress
from the same original Home snapshot on its existing worker. Preserve account/dismissal guards and all
creation defaults. Fourteen original application-policy cases, 18 final Windows contexts and six actual
OS 200%/360 dp Android contexts pass with unchanged complete fixture fields. Strict builds/full signed
offline APK and emulator no-reset/native Home restoration pass. No money/schema/permission change or
unrelated full-suite/benchmark repetition; Samsung remains on D-375.
[Detailed evidence](quality/home-getting-started.md).

## D-377 - Avoid redundant parsing of original empty tag lists

Current original Home stage observations place substantial time before the first data publication.
Keep the complete existing row path; extend its null descriptor with an exact binary-text empty-tag
marker and recognize only the standard immutable model's list conversion. Return a fresh empty list
for that case; retain original parsing/refusals for all other storage and overridden projections.
Nine new behavior cases and all 78 related Data cases pass. Same-data original native Home cohorts
improve descriptively on x64 and Samsung ARM64, with full schema/typed-row and presentation equality.
Preserve Samsung's actual Mono SIGSEGV and the four completed cases before continuing only the two
unfinished ones. This is no stability, cold-start or two-second acceptance.
[Exact evidence and package qualification](quality/home-empty-tag-materialization.md).

## D-378 - Resolve the original ARM64 bridge fault precisely

D-377's actual ARM64 SIGSEGV resolves through exact official BuildId/debuglink/DWARF to
push_object+16, inlined bridge_object_forward at sgen-tarjan-bridge.c:373:7. This locates an invalid
object-header read but does not identify the corrupt object or its full cause. Current stable .NET10
runtime remains10.0.13; prior serial/precise-bridge candidates remain rejected. Inspect the local GC
validation implementation before execution: nursery-canaries writes raw memory and is not installed.
A remset-only independent cohort completes all six current original Home cases and full protected
schema/typed-row/presentation proofs in PID20385, without a fatal remset assertion. Do not infer a
fix or use its timings. Restore ordinary caller/Main979/exact settings and release the phone. No
application/runtime/schema/security change, new tests or repeated unrelated checks.
[Exact investigation](quality/android-home-bridge-fault.md).

## D-379 - Reject constructor tag-list reuse after original Home comparison

LedgerEntry already initializes an independent empty Tags list. A candidate avoids replacing it for
D-377's exact standard empty-array case, keeping parsing before later columns and all field/error/
interceptor contracts. All78 related cases and strict Windows/x64 builds pass. Nevertheless, complete
same-data original x64 Home is descriptively slower: reference median951.53->1086.01ms, tenfold
2664.23->2955.07ms; only one of six corresponding samples is shorter for each shape. Do not infer
benefit from the removed source allocation or fish for a faster cohort. Withdraw the candidate,
restore exact reader/default caller/Main6661/Home0/OS and recompile restored Windows. No ARM trial,
phone input, Main APK rebuild or new managed count. [Evidence](quality/home-tag-list-candidate.md).


## D-380 - Qualify physical accountant note and document disclosure

Use a fresh fictitious Samsung profile and two original exports to close the bounded D-328 notes/document
copy gap. The isolated receiver admits only the exact known PDF; the host checks actual original packet
bytes, financial equality and explicit redaction. Changing flags invalidates the review. Strict fixture
build passes without warnings; both phone packets pass. Retire only the new profile, remove owned input
and receiver, preserve original three profiles/Main979/samePID/OS/APK and release the phone. No production
change or new unit-test count. See [full evidence and limits](quality/physical-accountant-document-disclosure.md).


## D-381 - Close the physical CSV preview acceptance gap

Qualify the original D-331/D-332 interpretation and page-arrival behavior on Samsung with retained known
own-format/generic inputs and a fresh fictitious profile. Four Next/Previous arrivals expose the heading,
correct first row and amount without corrective scrolling. The invalid final row and disabled final Next
remain reachable. Changing generic sign leaves the old Expense visible but disables Import with feedback;
explicit Refresh publishes Income and re-enables it. No Import/Undo is invoked; all-history stays empty.
Retire only the new profile and hash-matched owned files. Original three profiles/Main979/samePID/OS/APK
match; release the phone. No production change, new managed count or repeated successful build/suite.
[Original observations, failures and limits](quality/physical-import-preview.md).


## D-382 - Reject required GUID storage markers after complete Home measurement

A candidate replaces two per-row native type calls with trusted descriptor bits for required Id/AccountId.
All96 related cases and strict Windows/x64 builds pass, and all six original protected Home/native/data
cases finish in one process. Full-page timings do not justify adoption: only1/6 reference and2/6 tenfold
samples improve; medians951.5312/2664.2326 become999.69905/2974.9265ms. Restore both production files
exactly and the ordinary independent package/Main/OS state. Retain18 required-identity compatibility
cases, all18 passing on restored production; restored Windows compilation is warning-free. No phone
trial, Main APK or unrelated suite. [Original receipts and limits](quality/home-guid-storage-candidate.md).

## D-383 - Select recent Home rows on the existing worker

The complete Home snapshot is already stable newest-first. Select its first three eligible original
records on the facts worker instead of filtering/sorting all history after the UI collection reset.
Preserve every original financial source and presenter. Ten new actual-source cases and strict builds
pass; all six original x64 and Samsung contexts match complete data/native/recent packets. Retain both
native SIGSEGVs and continue only missing cases. Measured recent-publication intervals shrink, but
whole-Home timings do not establish improvement. Complete offline APK and no-reset Main upgrades pass.
[Original evidence, control justification and limits](quality/home-recent-selection.md).

## D-384 - Do not adopt serial major plus a larger nursery from a clean sample

A distinct artifact-only64MiB nursery/serial-major trial preserves complete original Home in one x64
process and passes eight original root retirements/six native Back actions. Public dispatcher pauses
are longer in5/6 tenfold pairs; aggregate post-HomePSS is about818MiB, without a comparable baseline or
peak/low-memory claim. One clean finite cohort is not a native fix. Keep Main unchanged, restore the
ordinary independent package and exact Main/OS state, and retain prior actual failures. No phone,
new unit count or successful unrelated suite. [Evidence and limits](quality/android-gc-nursery-trial.md).

## D-386 - Finish feature development before dedicated Android qualification

On 2026-10-19 the owner moved dedicated ANR/crash work and Android accessibility completion after
all remaining approved feature development. Continue necessary focused verification of changed
features and retain any observed failure; avoid unrelated runtime experiments and repeated successful
qualification matrices during development. This changes execution order, not release acceptance.
The owner describes the connected Samsung as low-end and possibly unhealthy; that is a hypothesis,
not a verified root cause, and does not invalidate existing phone or emulator failures. External
provider/account gates remain separate from ready independent development. Updated AGENTS, canonical
backlog and delivery plan record the same order. Documentation-only decision: no runtime change,
build, test, device input or new acceptance claim.


## D-385 - Complete bounded CSV input without losing the previous review

Bound actual provider reads, decoded text and complete parsed shape without commercial restriction
or silent truncation. Preserve fragmented UTF-8/16/32 BOM decoding; reject unfinished quoted fields,
empty/header-only inputs and failed analyses before replacing original review/result. Move decoding,
parsing and original matching to workers, publish complete values and revalidate profile/retirement/
interpretation after all awaits. Forty-six new Core/App cases pass through selective continuations;
old failure/oracle evidence is retained. Original native error surfaces and complete signed builds
are qualified without replaying unrelated suites. No financial-write, schema, permission, SDK or raw
CSV cache change. Full receipts and physical scope: [qualification](quality/csv-input-safety.md).
D-386 schedules dedicated ANR/crash/accessibility completion later; this is a feature correctness batch.


## D-387 - Preserve CSV financial meaning at quote boundaries

The actual generic parser/preview turned the malformed `"12"34` into an importable123400-minor-unit
amount; three original App selection regressions also replaced a complete review. Enforce escaped/
nonescaped quote boundaries without guessing repairs, preserving valid writer output and all prior
review/outcome guards. Nine Core/three App pre-fix failures are retained; all22 new cases,20 existing
CSV/export cases, the complete100000-row round trip and six affected bank syntax/encoding cases pass.
The changed error caption and original retained review are checked in18 Windows/six Release/AOT
Android contexts. No writer/schema/budget/permission/SDK change or unrelated full-suite replay.
[Complete qualification and binary/platform limits](quality/csv-quotation-integrity.md).


## D-388 - Decision-ready ECB reference source, not activation

Review the actual free daily ECB FX feed, reuse conditions and complete six-target-currency mapping
before the still-open OD-14 source choice. A normally validated read returns29 currency entries,
including all five required non-EUR values, and a date-only2026-10-16 set. All30 ordered target pairs
are reviewed with exact rational arithmetic;25 are calculated and must be labelled. HTTP modified
time is not a market fixing timestamp. Paid-context free-original disclosure and compatible precise
source/derivation metadata are part of the whole future integration, not optional About copy.
[Concrete proposal and costs](enhancements/2026-10-commercial-release/25-ecb-reference-source-decision.md).
No source is selected, adapter/feature activated, account/payment created or runtime/APK changed.
D-387 evidence and managed count remain current; successful builds/suites are not replayed for this research.

## D-389 - Approved daily ECB references with explicit date precision

The owner selected the complete D-388 proposal on2026-10-19. Register the daily ECB source only
through explicit Market:Source=ecb; keep the unconfigured default and independent hosting gates.
All30 ordered EUR/USD/GBP/CHF/CAD/AUD pairs share one bounded original feed; five are direct and25
are explicitly calculated from same-date legs. Version-two quotes carry a null fixing timestamp,
original XML digest, date, retrieval/document metadata, original legs and declared decimal formula.
Version-one bytes/digests and frozen financial/source-companion envelopes remain compatible; old
clients refuse unsupported new quotes. No database/model migration or silent financial update.

Six-language free-original notices appear before affected payment and on reference/cache/history
access, with full external-browser links. Serialized source evidence preserves the notice and
calculation for export/sync. The original writer still rechecks exact consent, target, profile,
staleness and unchanged financial data. [Qualification and limits](quality/ecb-reference-integration.md).

## D-390 - Reject the supported new bridge responsiveness candidate

Test a distinct exact-runtime-supported new bridge while retaining concurrent major/default nursery,
only in the independent caller. All six complete protected Home cases preserve original native and
financial/schema/typed-row behavior in one process, but all twelve public dispatcher gaps exceed
their historical corresponding samples; reference maximum2465.7064ms. Reject promotion/phone extension
and further lifecycle replay. Ordinary caller, same Main15560, exact Home/OS/APK and owned cleanup
are verified. Production code/APK and managed counts remain D-389; general stability stays open.
[Full evidence and limits](quality/android-new-bridge-trial.md).

## D-391 - Qualify actual original reader speech requests

Close a specific gap beyond D-367 focus metadata: nine original en/fa/de New expense/Amount/Cancel
speech-fragment text/locale/role packets match saved original native focus. Six real reader open/
cancel activations preserve the same Main15560 and zero-EUR Home. Reject an unreadable overlay
observer and recover missing filtered log evidence from the same bounded original PID/time window,
without input replay. Restore native ERROR/overlay/toggles, all ten secure values, app choices,
exact Home/OS/APK and remove only hash-matched independent artifacts/helper. No production or managed
count change; audible pronunciation and broad A11Y/runtime/device gates remain open.
[Complete evidence and boundaries](quality/android-reader-speech-requests.md).


## D-392 - Preserve later edits during import Undo and expose retained aggregate ranges

Actual Samsung Undo deleted an ordinary imported row after a title edit: only aggregate-reducing
imports had immutable original snapshots. Journal every new ledger import atomically through the existing
version-one format; refuse changed rows, later attachments/extra batch rows and unverifiable legacy
batches rather than backfilling mutable current data. Keep existing dependency/refund guards.
Also publish retained aggregate capability after LoadFrom and expand existing aggregate details in
Simple mode; an unsaved off/on toggle retains access to its original range. No schema change.

39 distinct related managed cases pass across selective runs (ten added; seven original failures
retained),36 original Windows forms with only27 continuation cases and12 Release/AOT Android forms
qualify the editor. Final strict Windows/complete offline signed Android builds are warning-free.
On the same Samsung fixture, final ordinary edited Undo and legacy refusal preserve all three rows
and-187EUR; original restored aggregate fields1-7Oct remain. Only the failed one-row case is repeated.
Main979/five known native rows/three original profiles/OS/APK and owned cleanup pass; phone released.
No full-suite, general crash/ANR, all-device/iOS/provider/store or publication acceptance.
[Complete failures, evidence and limits](quality/import-undo-and-retained-aggregates.md).

## D-393 - Qualify original physical bulk corrections and Undo

Close the AT-80/QA-03 physical bulk gap on the authorized Samsung with a fresh two-account/six-row
fictitious fixture. Original selection survives search/Home and prompt cancellation; tag/review/category
changes affect only intended rows, and mixed category kinds are refused. One positive deletion observes
day net984EUR and original Undo returns within5.047seconds of Delete, before its unchanged eight-second
deadline. Original net/Home973 returns; planned cold details preserve all six original monetary/metadata
packets, only two added tags, only the intended Housing category, refund link and transfer fee relation.

Only the new profile/matched input are removed. Original Main979/five known public packets/three profiles,
meaningful Home/OS and D-392 APK remain; the phone turn is released. No production or managed-count change,
unchanged successful test/build replay or broad device/runtime/release acceptance.
[Original evidence and retained observer failures](quality/physical-bulk-corrections.md).

## D-394 - Correct retained account balances and qualify physical asset consent

Original Samsung AT-81 new/edit Income/Expense Cancel and fresh Record anyway preserve complete
drafts and write only explicitly accepted fictitious rows. Internal transfer remains one principal
movement. Actual accepted expense edit reveals stale More-tab account presentation despite correct
stored detail; reuse the covered account loader on tab activation, preserving reconciliation drafts.
Six native Windows contexts retain successful observations and continue the same partial fixture;
strict Windows/complete offline signed Android builds pass. Upgrade the same phone fixture without
reset and repeat only the failed path with the next edit: automatic balance7,905.66, no manual Refresh.

The D-393 hosted failure is the exact feature-policy scan of an independent diagnostic fixture.
Bound the one-path exception by its original build/package guards and prove refusal of Release
fallback/unapproved readers; the exact existing managed case passes, with no count increase or
unrelated/full-suite replay. Original failures, cold persistence/restoration and hosted-result limits:
[complete qualification](quality/physical-asset-consent-and-account-refresh.md). D-386 remains in force.

## D-395 - Put Plans Add beside its title

The owner requested the same UI as D-375 Home. Move the original Plans AddCommand button into a
48px physical-right title row with zero docking margin, hide the replaced native title, and retain
Back for pushed instances. Filters and permission/review warnings keep their own row; list and
separate empty content use the remaining height without a bottom band. Avoid a duplicate Windows
PageHeader. No financial algorithm, new translation, permission or schema changes.

Eighteen actual Windows contexts (en/fa/de, light/dark,360/412/1280) check filled and empty geometry,
original command/name and stored account/ledger/plan/settings preservation;36 app-window images are
inspected. Dedicated Release/AOT Android Add/Cancel and complete offline signed APK evidence are
recorded in [the scoped qualification](quality/plans-add-header.md). No unrelated suite replay or
new managed case count, broad A11Y/stability/iOS/store/release acceptance. D-386 priorities remain.

## D-396 - Original reviewed balance and atomic reconciliation

The original reconciliation result survived amount/date changes and was saved separately from the
account's reconciled metadata. Replace that stale authority with an opaque single-use actual-file
review of the complete original account/related ledger. Revalidate under the existing financial
writer and atomically save only the original correction plus current stored account metadata.
Preserve commercial/household guards, monotonic reconciliation dates and committed outcomes.
The actual application session invalidates edited consent and refuses overlapping requests.

Twenty new original application/SQLite behaviors and thirty related ledger regressions pass; strict
Windows/Android builds and the complete signed offline package are warning-free. Six Windows native
consent contexts, eighteen corrected visible-image observations and original Android amount editing,
required reason, Cancel/Accept and cold persistence qualify this path. Financial successes are retained
through diagnostic corrections. [Exact evidence and boundaries](quality/reconciliation-review.md).
No schema, new persisted data, privacy permission or provider change; broader device/iOS/release and
D-386's deferred stability/accessibility gates remain separate.

## D-397 - Preserve committed advance-bill outcomes

Actual SQLite regression first proves the original settlement writer reports an ordinary exception
after commit, while the original form can offer Save again after subscriber or navigation failure.
Retain the committed result, mark the form accepted before follow-through, lock its input and turn
the same original action into Back. Coherent original-file reads and currency/draft checks preserve
the reviewed context; rejected writes retain typed input. Keep the native route bridge separate so
the actual portable view model, original effects and real SQLite can be tested without fake controls.

Sixteen new original application/SQLite cases and55 related retained-policy cases pass. Strict
Windows Release/Android builds and the complete offline signed package are warning-free. Six
Windows financial cases and18 inspected images qualify original accepted feedback and Back-only
return; a separate Home stale-resource error remains recorded, so the overall Windows runner is
not claimed green. Android original compiled sources retain six distinct cases with first-case
observation repair/readback and no financial replay. Main0/OS/current-candidate restoration passes.
See [exact evidence and limitations](quality/settlement-commit-lifetime.md). No schema, permission,
provider, commercial activation or broader device/iOS/stability/accessibility acceptance follows.

## D-398 - Retain covered Home refresh through genuine same-file source changes

The retained D-397 Home failure is reproduced with real SQLite. Rename-only classification is
insufficient: account additions also change goal references, and plan/goal/template/filter changes
have their own guards. Classify persisted-source supersession only at Home's validation boundary,
after every original packet/file/access check and again before propagating that typed refusal.
Recompute the complete projection under the same original cover and file, at most three attempts.
Ordinary actions/writers keep their existing rejection semantics; mutated input and retired access
cannot be hidden by a concurrent legitimate edit. There is no model, migration or financial-rule change.

Seventeen new application/SQLite behaviors,96 related Home read contracts and21 template-navigation
contracts pass on final logic. Preserve earlier rename contexts, then qualify only the additional
reference-addition case: Windows6+1 inspected own-window images and Android Release/AOT6+1 contexts
retain one accepted publication, original financial values and zero ledger writes. Strict final
Windows and complete offline Android builds pass, Main/OS are restored, test output is cleaned and
the finished correction is committed/pushed. Exact-head hosted CI remains separate. General ANR,
crash/A11Y under D-386, physical phone/iOS and external release acceptance are not claimed.

## D-399 - Retain committed foreground occurrence outcomes

Controlled application/SQLite faults reproduce an accepted payment/correction returned as rejected
before the form's completion callback. Preserve accepted SaveResult and generated/manual reopening
receipts through the existing typed notification boundary in both store layers. State-only commands
also publish with committed semantics. OccurrenceCommands completes the original retirement before
native feedback and retains its gate through the alert; genuine SQL/validation/consent refusals stay
unchanged. Refresh failure keeps its existing saved message; notification-only failure has complete
translations in the six existing languages. No money algorithm, model/migration, permission or SDK change.

Fifty focused application cases pass, including21 new outcomes and29 retained command behaviors;
70 related SQLite write-policy cases pass. Eight original native commands finish once on independent
Windows and Release/AOT Android fixtures.18 Windows native dialog layers and six Android translated
dialog views qualify new feedback without repeating accepted commands. Android17 exact native
answers preserve initial observer/encoding refusals and the same PID. Strict builds, signed offline
APK, original Main/OS restoration, test cleanup, docs and push finish this batch. Exact hosted CI,
broader device/iOS, durable crash/replay and general runtime/A11Y acceptance remain separate.

## D-400 - Preserve accepted occurrence state against retained corrections

An original open/skipped form could overwrite a settlement accepted after it loaded; linking could
also replace a newly skipped occurrence after consent began. Recheck actual stored state inside the
existing writer before any link SQL or state mutation. Refuse skip/unskip/override of a settled
occurrence and linking a skipped occurrence; retain same-entry link idempotency and explicit reopen.
Five new original command cases,55 related app cases and70 actual SQLite policy cases pass. Four
original native correction refusals per Windows/Release AOT Android preserve accepted rows and drafts;
six unchanged native message views per platform and11 Android answers are qualified without replay.
Strict builds/full offline signed APK and Main/OS restoration pass. General stability/A11Y under
D-386, physical-device/iOS/provider/release gates remain separate. See [scope and retained evidence](quality/occurrence-state-conflicts.md).

## D-401 - Refresh the retained original occurrence without replacing its draft

On Samsung, recording one manual expense in another Shell tab left the original retained occurrence
without its link suggestion; closing/reopening showed the same accepted row. Refresh that original
page on actual retained-tab activation under its shared read cover. Preserve raw amount/date/note
inputs and expanded state, refuse changed financial targets, keep failed reads covered with six-language
Retry and leave pending financial command/consent ownership unchanged. Accepted canonical command
reloads still reset defaults. No schema, money algorithm, permission, SDK or network change.

Thirteen existing snapshot tests and21 resource cases pass. Six original Windows native contexts/42
inspected images and six Release/AOT Android contexts/18 native views/18 real tab inputs qualify
reads, error/Retry, target refusal and pending writer/default publication. Strict builds and the full
signed offline APK pass. The same Samsung fixture qualifies the corrected metadata-only return path
and remaining override/skip/unskip, cancel/accept link, manual keep/unlink and generated delete-on-
reopen paths, with one cold read. Main979/five known rows/three profiles/OS are restored; only the new
inactive test profile is retired and the phone released. No financial replay or full-suite count
increase. Exact hosted CI and wider runtime/A11Y/device/iOS/provider/release gates remain separate.
See [scope, negative evidence and restoration](quality/physical-occurrence-corrections.md).

## D-402 - Original physical prior-period and cross-account refunds

Close only the remaining bounded AT-13/14/15 device path with the existing D-401 complete candidate.
Two zero-opening EUR Cash accounts dated September1 precede the original September20 Food purchase100.
A changed refund draft is cancelled without posting; original refunds25 to CashA and35 to CashB leave40.
An attempted41 is refused with visible domain feedback and retained input. Correct only the same
amount to40; the purchase becomes fully refunded with no further refund action. One cold read preserves
all four public details/links, current-month refunds100 versus the prior-period purchase, complete-history
net zero and account balances minus75/plus75. Main979/five known rows/three profiles/OS match after
retiring only the new inactive profile; the phone is released. Native input/viewport observer corrections
retain accepted money and the same cold process. No app/APK, unit-count or build change; unchanged
successful checks are not repeated. Wider device/chart/iOS/provider/release gates remain separate.
[Evidence and negative observations](quality/physical-refund-lifecycle.md).

## D-403 - Original portable backup across a physical independent reinstall

Complete original offline Release under a guarded independent package, not a Main reset or an empty
profile substitute. Original Cash37/expense2/Once9-skipped/Advanced/Monday data is exported through an
explicit local native read grant. The exact retained file restores through original first-run preview,
cancel/accept consent and one accepted replacement after actual uninstall/identical-APK reinstall.
One terminal cold read and original re-export preserve the entire51-table schema/74 typed rows except
original Settings.UpdatedAt ticks; all other values and portable preferences match, with one account.
Strict caller/receiver builds and signatures, fixed-identity/Release refusals and two verifier negative
boundaries pass. Main979/PID10005/three profiles/APK/OS match; only owned fixtures/file are removed and
the phone released. Production source/APK, managed count and existing successful checks remain D-401.
[Original receipts, observer repairs and limits](quality/physical-portable-fresh-install.md).

## D-404 - Original physical credit-card and account archive lifecycle

The retained D-401 candidate qualifies one card purchase60 and Checking-to-Card payment90, with
Checking410/Card positive30/Home440. Dirty-name archive cancellation preserves the draft; one accepted
archive keeps stored identity/history, ends both future expense/source-destination transfer plans and
clears the exact default. Original archived Reports retains income0/spending60. Unarchive and one cold
read preserve both ledger details/notes/dates, both Ended plans and no default. Main979/five known public
packets/three profiles/OS/APK are restored after retiring only the new inactive profile and releasing
the phone. No source/APK/managed-count/build change or successful unrelated rerun. Wider device,
paused-plan/currency/iOS, D-386 runtime/A11Y, provider/store/release and exact-head CI remain separate.
See [bounded receipts and limits](quality/physical-credit-card-archive.md).

## D-405 - Original ordinary CSV financial review

A changed opening date after own/mapped CSV preview could silently change an imported row's balance
interpretation. Two original app/SQLite regressions fail on unchanged production. Ordinary previews
now bind the existing coherent file/accounts/categories/history/journal packet to the same atomic
writer. Expected stale refusal keeps raw input/choices, disables Import and requires explicit Refresh;
departed forms retire their token. Fresh writer rights and bank/holdings/accepted-result contracts stay.
Eight new cases, retained103 related successes plus the one corrected file-recheck observer,12 native
financial contexts per Windows/Release AOT Android, strict builds/full signed offline APK and Main's
no-reset upgrade/Home0/settings pass. Accepted cases are retained across observer repairs; no financial
replay or full-suite rerun. No schema/model or permission change. Broader device/import/ANR/A11Y/iOS/
provider/release and exact-head CI remain separate. See [scope and preserved negatives](quality/original-csv-financial-review.md).

## D-406 - Original physical opening-boundary CSV and retained-tab review

Samsung's original picker/preview/import stores one older12.34 expense and opening-day20 income in
one new fictitious Cash900 profile. Home920 excludes the older row; explicitly moving only opening
October20 to18 gives907.66. One planned cold preserves both original public details/dates/notes and
the durable two-row batch. Original Undo restores Home900/empty ledger while opening18 remains.
A real retained More-tab departure did not call OnDisappearing and left Import enabled. The original
Shell now retires that review on uncancelled tab navigation after existing leave guards. Final full
signed offline Release upgrades without reset and physically proves disabled Import, retained source
and explicit Refresh without financial replay. Strict Windows and complete Android builds have no
warnings/errors. Managed algorithms/tests are unchanged; retained D-405 successes are not rerun or
counted as new cases. Main979/five public packets/three profiles/OS are restored; only the new inactive profile
and exact hash-matched public file are removed, and the coordinated phone turn is released. No schema/model, resource, permission or provider change.
Broader device/import/report, D-386 runtime/A11Y, iOS/store/release and exact-head CI remain separate.
See [bounded evidence](quality/physical-opening-csv.md) for original receipts, preserved failures and completion status.


## D-407 - Original split/join financial state and physical lifecycle

Two real SQLite regressions first prove that retained split/join could overwrite a newer original
note. The coherent actual-file/group/account/category/refund/attachment review is now rechecked in
the existing atomic writer before EntryActions mutates copies. Changed sources/pending input refuse
before money and retain raw shares; known committed notification failures leave the original form
terminal/Close-only. Original writer rights, exact total and attachment ownership remain preserved.
Sixteen new Data and3 actual app-session cases pass selectively, with retained two-case native
Windows/Release AOT Android finance,36 Windows observations across en/fa/de light/dark360/412/1280
and12 Android observations. Observer failures continue the original stored refusal and pending
acceptance without reseeding or replaying successful money. Strict builds and complete signed offline
Main APK pass. Samsung's original new Cash900 profile records one100 expense, splits60/40, reallocates
70/30, preserves both public metadata packets and Home800 through one cold read, then combines once
to one100 Food row/Home800. Main979/five known public packets/three profiles/OS are restored and only
the new inactive fixture is retired before releasing the phone. No full-suite/unchanged successful
matrix rerun, model/schema/permission/provider change or release claim. Broader QA-01, D-386 runtime/
A11Y, iOS/store/external and exact-head CI remain separate. See [original evidence and limits](quality/original-split-financial-review.md).


## D-408 - Original holding event review and retained acceptance

Two real SQLite regressions first reproduce a stale correction replacing newer source data and an
ordinary exception after a purchase already committed. The original event/type/history/locations/
accounts/group/refund/attachment/valuation packet is now rechecked in the existing atomic quantity/
payment/fee/price writer. Copies preserve raw drafts and immutable reviewed facts. Foreign money or
unreviewed event identity, changed sources/access and pending input refuse before writes. Known
accepted notification/native-return failures retire the original form with Close-only navigation.
One explicit new location can be adopted only while all original financial facts stay unchanged.
An observed native empty-query redelivery now preserves the loaded review instead of losing it.
Twenty new Data and3 actual app-session cases pass selectively, plus46 original related cases once.
Two original financial cases per independent Windows/Release AOT Android preserve exact quantity,
fee/group/derived price, Cash874/929 and income0/expense1.36 Windows observations cover en/fa/de,
both themes and360/412/1280;12 Android observations cover the new native feedback and retained state.
Original Windows pre-command data is continued without reseeding or accepted-money replay. Strict
builds and complete signed offline Main pass; the independent emulator restores Home0/My account,
exact OS values/size/density and final Main hash through one no-reset upgrade. No physical phone,
private installed key/database, schema/model/permission/provider change or full-suite replay. Wider
holding relationships/device/iOS, D-386 runtime/A11Y and external/release/CI gates remain separate.
See [original receipts and limits](quality/original-holding-event-review.md).


## D-409 - Corrected source and retained refund boundaries

Three original real SQLite failures reproduce a corrected holding fee smaller than its retained
refund, a corrected holding source moved to an incompatible account currency and the same ordinary
expense-source currency gap. One shared REF-04 source validator now checks final linked refunds in
the existing atomic ordinary/holding writers before mutation. Explicit valid deletion/unlink and
same-batch source/refund correction remain supported. The actual editor shows amount/account errors
beside their fields, preserves raw input and accepts only an explicit valid correction of that same
original form. Known acceptance remains terminal; no conversion or guessed relinking is introduced.

Thirteen distinct new cases (8 Data, 5 Core) and17 affected existing cases pass in retained selective
cohorts, not a full-suite rerun. Two original native correction cases per independent Windows and
Release/AOT Android preserve exact refund identity/link/amount/currency/metadata, quantity/basis/price,
Cash875/874.75, income0 and net expense0/0.25. Four live observations prove refused Save remains enabled
and accepted Save is disabled. Afterwards36 Windows and12 Android read-only rejected-draft caption
observations cover en/fa/de, both themes and Windows360/412/1280; they rehydrate original refusal
presentation with retired authority and do not replay finance. Strict builds and the complete signed
offline Main pass. One no-reset emulator upgrade restores Home0/My account and exact OS/size/density.
No phone input, Main private database/key read, schema/model/permission/provider change or release
claim. Malformed optional fee text remains a source-inspected next concern, not fixed or reproduced
here. Broader financial/device, D-386 runtime/A11Y, iOS/external and exact-head CI gates remain separate.
See [original evidence and limits](quality/holding-fee-refund-limits.md).


## D-410 - Original optional holding-fee input

Three actual editor/SQLite regressions first prove malformed, negative and out-of-range nonblank
fees committed instead of refusing. The original editor now distinguishes invalid input from an
explicit blank/zero removal before building financial entries. It preserves raw fee/note, original
review and enabled correction with existing field-local feedback. Preview omits guessed zero-fee
or wrapped account changes. The actual non-UI editor is linked through its original native-effect
ports; native query delivery remains in its unchanged MAUI adapter. Seventeen distinct new actual
app-flow cases pass in retained selective cohorts, without replaying successful D-409 cases.

Two original native malformed/negative cases per independent Windows and Release/AOT Android each
refuse with complete rows unchanged, then correct that same original form once to fee1EUR. Both
preserve refund0.75EUR with complete original metadata/link, Cash874.75, quantity/basis/derived price,
income0/net expense0.25 and terminal accepted state. Four original live assertions precede accepted
receipts. Windows retains4 live plus6 English captures, then continues only12 pending fa/de captions;
Android continues only read-only presentation after its original observer failure, with6 final native
caption observations. The expectation now respects native Persian digits rather than raw Latin
resource examples; production text was complete. Original failures and accepted files are retained.

Strict builds, complete signed offline Main and no-reset independent emulator Home/OS restoration
pass. No phone input, private Main data/key read, full-suite rerun, schema/model/resource/permission/
provider change or ANR/crash resolution claim. Wider holding/device/iOS, D-386 runtime/A11Y and
external/release/exact-head CI gates remain separate.
See [original evidence and limits](quality/holding-fee-input.md).

## D-411 - Refund action for holding fees and original physical lifecycle

An original Samsung purchase exposed an eligible fee refund hidden by the managed-source actions
container. The existing CanRefund row now has its own card; purchase/sale principal edit/delete stay
in holding history. Four original accepted financial commands (purchase, linked refund, same-form
fee correction, sale) produce Cash900→848→849.50→850→879.50EUR. Out-of-range and below-refund fees
refuse with raw drafts retained. One terminal cold preserves five known public packets, one unit,
25EUR/unit original basis/price and displayed realized5EUR before fees, without a tax claim.

Twenty-six inspected Windows captures/native observations and24 final read-only Release/AOT Android
action contexts cover en/fa/de and both themes, plus Windows360/412/1280. Original Android semantic
observer and independent modal/route failures are retained; independent Android modal Cancel is not
accepted. Original seeded rows are reused read-only and accepted money is never replayed. Windows
native refund open/Cancel and the actual Samsung refund Save qualify the original action separately.
Strict builds, complete signed offline ARM64/x64 APK and no-reset emulator restoration pass. Samsung
Main979/five known public packets/original three profiles and exact OS values are restored; only the
new inactive fixture is retired and the coordinated phone turn released. No new unit-test count,
full-suite replay, schema/model/resource/permission/provider or Main private-data/key change. QA-01
overall, other holding variants, D-386 runtime/A11Y, iOS/external/release and hosted exact-head CI remain.

See [original evidence and limits](quality/physical-holding-fee-lifecycle.md).

## D-412 - Original holding quantity range input and field feedback

Three original parser and two actual editor/SQLite regressions reproduce Decimal base-unit conversion
and checked quantity-preview overflows. Check the original number against Int64/factor before
multiplication, distinguish range refusal from ordinary invalid quantities, omit an unrepresentable
preview and refuse the original proposed range before financial-entry construction. Keep raw drafts,
original review and explicit same-form correction; preserve the existing checked domain/atomic writer.
The new six-language feedback appears beside Quantity without a duplicate form message.

Sixteen distinct new cases and21 existing resource checks pass in retained selective cohorts; no
unchanged full suite or D-408..411 accepted money/matrix replay. Two original native refusal/correction
cases per independent Windows and Release/AOT Android preserve Cash874.75, source money/refund/price
and basis25 while each valid nonfinancial correction adds2units to the original1. Four original live
observations per platform precede only read-only presentation. After field placement changes, final
Windows18 and Android6 caption contexts reuse the exact original accepted receipts/files with zero
new commands; original Cases retain their original PID and Live is empty. Strict builds, complete
signed offline APK and no-reset emulator Home0/My account, OS and hash restoration pass. No Samsung
input, Main private database/key, schema/model/storage scale or provider change. Broader finance/
holding variants, D-386 general runtime/A11Y, physical/iOS/external/release and exact-head CI remain.

See [original evidence and limits](quality/holding-quantity-input.md).

## D-413 - Original manual holding-price Save/delete and accepted result

Bind manual prices and confirmed deletion to a coherent original type/history/price/location packet,
revalidated in the existing actual-file writer with fresh rights and current draft/confirmation.
Keep raw price/date/mode on changed facts, retire departed authority and offer explicit Refresh.
Known accepted Save/delete survives notification/readback failure without another write; an old-row
deletion preserves a separate new-price draft. An ordinary empty return may refresh history; raw or
failed drafts require explicit review. Keep field-local feedback, scalable native actions and six
translations. Fifteen new selective actual app/SQLite cases,74 affected policy checks and21 resource
checks pass; original native receipts and read-only continuation keep successful financial operations
from being replayed. Full runtime evidence and limits: [original manual price review](quality/holding-price-review.md).

## D-414 - Original holding history deletion and file-bound short Undo

Complete the original holding delete/Undo cycle: coherent displayed-row/group confirmation, fresh
actual-file writer rights/source checks, exact committed deletion receipt and accepted restoration.
Preserve explicit fee-refund links and orphan receipt ownership/content; reject changed/foreign/
mutated/consumed Undo before money and recheck the restored original packet. Keep uncommitted retry,
retire departed actions and distinguish known acceptance from failed notification/readback. Twenty
new selective actual app/SQLite cases, four affected prior cases and21 resource checks pass. Original
native commands and retained read-only captions are qualified separately, without replaying accepted
work. Short Undo remains process-local; no schema/model, new permission/provider or release claim.
See [original evidence and remaining gates](quality/holding-history-undo.md).


## D-415 - Original holding archive confirmation and accepted result

The detail archive no longer saves a retained whole-type copy. Revalidate the original coherent
actual-file type/history/prices/locations packet with fresh Corrections rights and current confirmation,
then change only the tracked type's archive flag. Refuse stale/foreign/mutated/consumed or departed
review, preserve financial history/refund/receipt ownership and raw price drafts, and retain known
acceptance before fallible notification/return. Explicit Refresh reads only; absent/failed reads never
rearm a stale or accepted archive. Thirteen new actual app/SQLite cases pass in selective retained
cohorts, without repeating accepted flows;23 resource checks also pass. Original Windows/Android
Cancel/refusal/archive and read-only captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass. No schema/model, new permission/provider or release claim. Evidence and remaining platform gates: [original archive review](quality/holding-archive.md).


## D-416 - Original type editor creation, edit and accepted identity

The actual type form now owns one coherent actual-file original type/history/price review, or one
stable absent new identity. Recheck fresh rights, immutable source and exact pending raw draft in
the existing writer; copy before mutation and preserve noneditable metadata and dimension rules.
A missing original row never becomes a new form on empty query redelivery. Keep invalid/raw input
for correction, retire stale/departed authority, and require close/reopen after changed source.
Known creation/edit acceptance consumes the original form before notification or navigation; a failed
next step cannot create a duplicate or repeat Save. Nineteen new actual app/SQLite cases pass in two
selective cohorts, without replaying accepted cases;23 resource checks also pass. Original Windows/
Android refusal/edit/create and retired captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass; no schema/model, new permission/provider or release claim. See
[original editor evidence and remaining gates](quality/holding-type-editor.md).

## D-417 - Original physical holding type, price, history Undo and archive

The existing complete signed offline D-416 candidate is qualified through one original Samsung
lifecycle, without a new APK, application/schema/model change or replay of successful tests/builds.
Only a new fictitious profile/cash account is used. The original type creation/opening route,
quantity overflow refusal/same-form correction, purchase2 for50EUR plus fee2, linked refund1.50,
existing name/note edit, manual60EUR/piece Save and old-price deletion with separate raw77 all pass.
Delete the original purchase/group and immediately Undo once in the same view/process; original
history/refund link and raw77 survive. Cancel Archive then confirm once. One terminal cold retains
Cash849.50, three public packets, quantity2/basis25, edited type name/note and archived history,
while the ordinary list excludes the type. No intermediate deleted cash or durable Undo is claimed.
Original Main979/five public packets/exact three profiles/OS and installed hash are restored; only
the new inactive profile is retired and the coordinated phone turn released. The cold refund reads
Uncategorized; tapping a chip alone is not evidence of selected category. No Fees-selection claim is
made. Original host observer failures continue only pending reads/commands, never accepted writes.
No unit-test count increase, private database/key/id equality, physical receipt Undo, all-device,
D-386 runtime/A11Y, Mac/iOS, external/store/expert or release acceptance is inferred. Exact-head hosted
CI is checked once separately. See [original evidence and limits](quality/physical-holding-type-price-history.md).

## D-418 - One original coherent holding overview

Replace separate initializing/profile-following overview reads with one actual-file worker transaction
and a complete prepared row/value packet. Recheck original source/rights, bound same-file supersession,
cover loading/failure with explicit Retry, and retire departed publication/native targets. Open/Add
check the actual file even under inactive test policy; an empty read creates no settings. Nineteen
distinct new actual app/SQLite cases and23 resource checks pass in selective cohorts, without a full
suite or accepted financial replay. Original Windows and Release/AOT Android loading/error/Retry,
ready/empty, retirement and native targets preserve all typed fixture rows. Only changed six-language
loading/Retry captions are rechecked after visual correction. Strict builds, complete signed offline
APK and no-reset emulator Main/Home0/OS restoration pass; Samsung is untouched. No schema/model or
privacy boundary changes, speed/general ANR/crash/A11Y/device/iOS/store/release acceptance is claimed.
See [original evidence, observer continuation and limits](quality/holding-overview.md).

## D-419 - Complete original holding detail publication and routed identity

Read one actual-file detail/settings packet on a worker without initializing settings, prepare all
quantity/value/history/price fields and publish under an original loading/error/Retry cover. Retire
departed/retained-tab work and recheck original native navigation/file targets. Preserve raw price
input for explicit Refresh, existing accepted editor outcomes and theme-only color rebinding. A new
routed type receives its own editors; old drafts/short Undo/native targets never move to it. Twenty
distinct new actual app/SQLite cases pass in selective cohorts; no full suite or unchanged resource/
financial replay. Retained original fixtures qualify57 Windows views plus only two new query views
and23 Release/AOT Android observations, with no seed/UI financial command. Strict builds, complete
signed offline APK and no-reset emulator Main/Home0/exact OS restoration pass; Samsung is untouched.
A final known-commit/departure test and one retained-source AOT callback control close automatic
readback to a departed view, without replaying completed native matrices. No schema/model/privacy boundary change or general runtime/A11Y/device/iOS/release acceptance.
See [original failures, continuations and limits](quality/holding-detail-publication.md).

## D-420 - Original holding-place creation and known metadata outcome

Create a named place only under the original actual-file holding review and pending form owner,
with fresh rights and a stable absent identity. Prompt ownership closes financial Save before the
name dialog. Recheck source and owner in the same transaction, consume the old review on metadata
acceptance, and retain that known identity before notification/readback. A failed following step
shows saved-place feedback and an explicit read-only choice refresh; it never creates the place
again or resets raw financial inputs. Transfer keeps its original source and selects the new destination.
A departed/replaced form cannot adopt old metadata or recover financial authority.

Eighteen distinct new actual app/SQLite cases and 23 resource checks pass in selective cohorts.
The three original baselines fail before repair; later cohorts run only new boundaries/outcomes.
Original Windows UIA and independent Release/AOT Android native controls each create one place and
recover its choices read-only, with zero financial Save, then retain captions across the required
language/theme/width contexts. All typed rows outside that one location addition remain unchanged.
Strict builds, complete signed offline APK, no-reset Main/Home0 and exact emulator OS restoration
pass. No schema/model, new data/permission/SDK/provider or general ANR/crash/A11Y/phone/iOS/release
acceptance is claimed. D-386 and remaining external gates stand.

See [original failures, focused evidence and limits](quality/holding-location-creation.md).

## D-421 - Complete original holding-event form publication and routed draft ownership

Keep the event form covered until its original coherent read, actual-file revalidation and complete
field publication succeed. Failed reads offer explicit original-file Retry and Cancel; they do not
initialize display settings or leak an exception out of query delivery. Validate the original event/
type before missing-default scaffolding. Required default creation retains Corrections policy and
one known metadata receipt through readback failure, without recreating a deleted accepted default.
A published raw draft gets read-only Retry; unavailable kind/account/place is never replaced silently.

A new routed event/type owns a blank draft and separate financial session. Empty redelivery retains
the existing form. Original pending/departed reads and accepted financial callbacks cannot publish,
close or activate another view. Keep archived historical payment/place identities and rebind a
retained transfer destination by identity. 22 distinct actual app/SQLite cases pass in selective
cohorts, followed only by affected checks. Final strict builds, 56 actual Windows views and 20
independent Release/AOT Android observations pass with original typed rows unchanged. Complete signed
offline APK and no-reset Main/Home0/exact emulator restoration pass. No unchanged full suite or
resource matrix replay; no schema/model, new permission/provider or general ANR/crash/A11Y/phone/iOS/
release acceptance. D-386 and remaining external gates stand.

See [original failures, native observations and limits](quality/holding-event-publication.md).

## D-422 - Original physical holding-place move and dirty discard

The existing D-421 signed offline APK passes one original Samsung opening3/basis30EUR and move1:
Home2 plus D422_Storage1, total3 and average basis10 remain, with Cash900 and an empty current-month
ledger. One new place is adopted by the original move draft without losing quantity/date/note or
changing its source. Keep editing retains raw2; Discard keeps the saved move1. One terminal cold
retains original history and quantities. Restore Main979/exact five public packets/three profiles/
OS/hash, retire only the new inactive fixture and release the coordinated phone turn.
Twenty-four retained-observation assertions are not new unit tests. No new app/APK/build or successful
suite/accepted financial sequence replay. A separate unsaved USD-type draft exposes unexpected
native currency-search text and is discarded; USD selection/new event and its root cause are not
qualified. Next ready work diagnoses only this original input/list boundary. D-386 general runtime/
A11Y, broader QA-01, Mac/iOS, external/expert/store and release gates remain separate.
See [original sequence, failed observations and limits](quality/physical-holding-location-move.md).

## D-423 - Original native currency selection, without a product workaround

The independent Release/AOT Android caller opens the actual unchanged AccountFormView currency
control once. After actual native realization, the original visible 44-unit GBP button selects GBP
in both the model and control and closes the popup. Stored accounts and entries compare unchanged;
there is no Save surface. The early active-window dump and raw ContentDescription were incomplete
observers, not proof of a blank product list. Preserve failed observations; remove all speculative
production viewport changes. Main code/APK remain D-421, Main resumes with its original PID/Home0,
and no phone input or system setting changes occur. The final independent strict build has zero
warnings/errors. No unchanged Main build/full suite or accepted financial sequence is repeated;
no new unit-test count, physical USD-input, spoken accessibility or general runtime acceptance is
claimed. Continue remaining ready feature/device work with D-386 and external gates separate.

## D-424 - Original entry-detail attachment owner and complete file names

Bind detail attachment reads, native picking and confirmed removal to the original displayed entry,
actual file, metadata and view owner; recheck cached rights and original bytes under the writer.
A changed/departed picker cannot target another query/profile. Consume known commits before
notification/readback; show the saved/reopen outcome without resubmission. Give the complete file
name its own row, separate from reading/removal actions.13 focused actual-source cases pass; no full
suite replay or schema/model/permission/provider change. Retain54 original Windows caption/Cancel
observations and18 changed-row continuations over the same original file. The latter completes its
report and then exits with native0xC0000005: general runtime acceptance is not claimed. Final strict
builds and complete signed offline APK pass. The normal installed Android emulator exercises one
original PDF pick/add, removal Cancel, original ledger Delete/Undo within8s, cold without the input,
and document-only removal with unchanged expense/balance. Restore Main0/original profile set/OS and
retire only the fixture. These are not physical-device, OCR/viewer, general ANR/A11Y, iOS or release
acceptance. Continue remaining ready original entry-editor/detail work under D-386.

## D-425 - Keep an editor receipt with its known financial commit (2026-10-20)

The old editor marked Save complete only after change notification, then added its pending receipt
through the currently configured store. A subscriber failure could present committed money as a
failed Save; a profile/query change could redirect the later document. The actual editor now owns a
single Save session over its original quick-reference/file/rights review, with detached submitted
entries and document bytes. The writer captures the saved source and attachment metadata before
commit and records acceptance before notifying subscribers. The following reviewed document writer
rechecks that source, file, rights and pending owner. Preserve the established partial-success
contract: failed document storage leaves the financial commit accepted and shows the existing
attachment-failure outcome. Retire late view callbacks without discarding raw drafts or reopening Save.

Twelve distinct focused actual-source/SQLite cases pass in cohorts10+2; no full-suite replay. A real
Windows editor/native Save runs once over the retained original fixture, saving one expense1234
and its exact document bytes despite two deliberate postcommit observer failures. Save remains
consumed and base-store accounts/entries unchanged. Initial observer failures (multiple scroll
surfaces, the fixed footer and wrong pre-page query construction) remain recorded; they made no
financial Save and their account seed was reused, never recreated. Final routed construction and
native target checks pass. See quality/entry-editor-save.md for final Android/build evidence and
limits. No schema/model, permission, provider, portable format or UI-layout change. D-386 priorities
and broader original edit/overlap/Undo/detail publication gates remain separate.

Final installed Android qualification (D-425): upgrade the complete signed offline APK without reset,
then one original Home/PDF receipt and Save in a separate fictitious profile. The stored24.50EUR/
17October/Food/D425_Cash and13KB document survive one cold after the exact input is removed.
Restore Main0/original public profile set/current-month history/OS and retire only the inactive
fixture.12 focused cases, final strict Windows/Android builds and one actual Windows Save pass;
prior negative observers remain recorded. No physical phone, merchant-corpus, general ANR/A11Y,
iOS/provider/store or release acceptance; original edit/overlap/Undo/detail gates remain separate.

## D-426 - Complete original transaction detail publication and actions

Prepare original entry/reference/group/refund/holding/document/rule facts through one captured
actual-file worker transaction, without creating settings for display. Validate the complete packet
before covered synchronous publication. Keep loading, failed read-only Retry and missing states
separate. A routed identity owns its own session and command instances; old commands, confirmations,
template prompts, OCR/viewer work and departed reads cannot adopt a different publication.

Review updates only the original flag; explicit rule creation uses original category/match and
refuses an already equivalent rule. Delete retains the original exact group/refund/document Undo
contract and deadline. Consume known commits before notification; recovery after saved work only
rereads. A refused viewer request removes only its newly created cache file, while a possibly accepted
native request retains the existing startup cleanup policy.

35 distinct new actual-source behaviors and selective affected/resource checks pass; no unchanged
full suite is replayed. Original Windows evidence includes18 language/theme/width ready contexts,
covered failure/native Retry, query/missing/retired-owner cases and two one-time native metadata
commands through deliberate postcommit notification failures. Preserve the first pre-action report
serialization refusal and continue only its exact original file without reseeding. Final Android,
APK and delivery evidence is recorded in [the detailed qualification record](quality/entry-detail-publication.md). No schema/model, portable format,
permission/provider or broader ANR/crash/A11Y/physical-device/Mac/iOS/store/release acceptance is
inferred. Continue ready original financial workflow work under D-386 and the existing external gates.

## D-427 - Complete original transaction editor publication

The editor prepares its original references, group, refunds, tags and rules through one actual-file
worker read transaction. Complete fields stay covered until publication; initial failure offers
Retry and an unavailable explicit source never becomes a new blank entry. A different query owns
fresh raw input and commands. Read-only refresh preserves the original draft; source changes refuse
an unsafe Save. Original detail/template/receipt handoffs and the writer validate exact file/rights
and original financial facts, including later refunds of sibling group entries. Both duplicate fees
keep their values with fresh identities. Pending writer waiting and known acceptance never rearm
saved money. Derived currency flags and optional icon reset finish before the form is uncovered.

33 distinct focused new source behaviors, the related core tag case and23 resource cases pass;
there is no full-suite replay. Original Windows evidence totals96 observations across retained
processes, including18 language/theme/width contexts and two one-time native Save invocations.
The independent Android sequence totals36 original observations, with two native Saves. Final
strict builds, complete signed offline APK, no-reset Main upgrade and protected original form
open/Cancel pass. Native observer failures and general Windows exits remain documented. No schema,
compiled model, portable format, permission or provider change. D-428 remains the next separate
aggregate-overlap/choice/writer/Undo batch. D-386 general runtime/A11Y priorities and external gates
stand.

[Original evidence and acceptance limits](quality/entry-editor-publication.md).

## D-428 - Original transaction workflow through account creation and aggregate review

The next complete D-427 batch retains the original raw/receipt form through an explicit nested
account/default commit and cancelled child. Only that child's known packet can refresh references;
departure/new query/file/rights changes refuse late ownership. Aggregate decisions cover new edges
individually and revalidate under the original writer. Unchanged edges are not subtracted again.
The existing adjustment journal prevents duplicate consumption while retaining manual source facts.
Original eight-second Undo restores aggregates and keeps details, with file/deadline/dependency checks.
Known commits precede notification; a failed Undo-offer observer leaves a clean saved form with Close.

Full monetary explanations use a native dialog body after actual Windows title/footer clipping.
Transient truthful adjustment notices are reachable on Home and Transactions without renewing time.
22 new focused actual-source behaviors and23 final resource cases pass; no full-suite replay.
Original Windows/Android actions, negative observations, final APK and exact acceptance limits are
recorded in [the workflow evidence](quality/entry-workflow-ownership.md). No schema/model/portable
format, permission or provider change. D-386 and external gates stand.

## D-429 - Complete original plan/debt/transaction-draft ownership

The complete editor reads original references, settings, schedule, overrides and recorded payments
through one actual-file worker transaction. Display reads never create settings; explicit missing
sources cannot become a new plan. Query-bound commands, raw snapshots, reminders and discard replies
retire with their original owner. Explicit readiness renewal requires unchanged original inputs and
retains the raw draft. A temporary readiness failure keeps the complete form visible/editable with
Save disabled. A nested account visit uses the original writer and known reference packet; an
unavailable debt payment account stays explicit until a matching money account is created.

Original new/edit/split writers validate the full reviewed source and read back saved semantic fields
under one transaction. Any stored occurrence, including an Open override, requires a historical split.
The original recurrence anchor, saved payment amounts and occurrence facts remain intact. Acceptance
precedes fallible notifications, so a saved form cannot rearm. No schema/model/compiled-model, portable
format, permission, provider or SDK change. Focused source/native/build evidence and limitations:
[original plan editor qualification](quality/plan-editor-ownership.md).

## D-430 - Original plan monetary context

Source and destination raw inputs retain immutable culture, known ISO currency and explicit display
unit through display changes and read-only publication. The existing central monetary parser gains
an explicit-unit overload; ambient callers keep their previous behavior. An example explains the
original format. A changed currency/unit retains its old label and requires complete clearing and
reentry. Hidden/unknown inputs do not post stale money. Writer guards retain both context identities
and a monetary revision; an edit followed by identical text cannot revive an old submission.
Known acceptance remains final.22 added focused cases, affected parser/validator/resource checks,
retained actual native matrices, final package and negative evidence are documented in
[monetary-context qualification](quality/plan-money-context.md). No schema/model/format, permission,
provider or SDK change. D-431 addresses the separate ambient-unit gap in goal/budget monetary drafts.
D-386 runtime/accessibility priority and external gates remain unchanged.

## D-431 - Original goal/budget monetary meaning

Each target/contribution and total/category raw amount retains its independent original currency,
display unit and parse culture. Unit changes require complete clearing/reentry; partial edits cannot
renew meaning. Dirty context meaning survives reload, including identical digits in a new unit.
Hidden limits expose actionable review; share-of-income ignores irrelevant fixed amounts and physical
quantities retain their units. Original raw revision/native owner is checked under paired writers.
Known goal/budget/cut acceptance precedes notification and cannot rearm.25 new and75 affected prior
source cases pass selectively, without a full-suite replay. [Native/build evidence and retained failures](quality/goal-budget-money-context.md).
No schema/model/portable-format, permission/provider/SDK change. D-386 and external gates stand.

## D-432 - Original transaction and split monetary meaning

Each original transaction amount, destination amount, source/destination fee, ISO purchase amount and
reimbursable amount retains its own currency, applicable display unit and parse culture. Unit/currency
changes require complete clearing/reentry; partial edits never renew meaning. Original labels and
format examples remain visible. Receipt/template adoption starts from authoritative minor units;
account Undo and read-only/nested return retain original context. Repeating-plan handoff validates only
its supported primary/destination amounts, without borrowing unused fee meaning. Split shares retain
independent contexts; Join uses the original stored total. Raw/context revisions are rechecked under
original writers; known acceptance remains final. 27 new focused source cases and 51 related prior cases
pass across selective executions, with no full-suite or unrelated successful-check replay.
[Original native/build evidence and retained failures](quality/entry-money-context.md). No schema/model/portable-format, permission/provider/SDK change. D-386 and external gates stand.

## D-433 - Recover shared CI fixture and installer boundaries

The D-422 and D-431 hosted test logs identify the same two failures: the mode-policy scanner treats
review-only source fixtures as shipping readers, and a shared-membership Undo fixture supplies a
fabricated group without the original deletion receipt required by D-414. D-431 Windows additionally
refuses installer 18.11.12306.390 before compiling, matching official image 20261018.275.1 inventory.

The scanner now names the 13 exact additional fixtures and validates their complete outer review-only
build gates; it does not exempt the Diagnostics directory or ordinary Release readers. Seven new
positive/negative gate cases prevent unsupported conditions, outer fallbacks and escaped source.
The Undo policy fixture first obtains one real committed deletion receipt, then revokes membership.
A new case proves refusal leaves both deleted rows and that original receipt intact, followed by
successful original restoration when membership returns. No production commercial or provenance
guard changes. Eight focused Core and 11 Data cases pass after the original failures are reproduced.

The official installer metadata is added without changing either approved compiler hash/file-version
pair, pinned MSVC/SDK, signature validation or immutable package contents.17 local toolchain cases
pass on the existing 18.10.12201.205 installation and compiler 19.51.36257.0; other installer versions
are metadata-only checks. Hosted bd6e9484 CI and native Windows pass; its Android package refusal is preserved below, followed by final hosted recovery.
Application sources, runtime behavior and the complete D-432 APK remain unchanged. Successful app,
Android, backend, native financial and device checks are not replayed locally. D-386 and external
gates remain unchanged.

[Failure evidence and hosted acceptance](quality/ci-common-recovery.md).


### D-433 hosted follow-up - Original Linux host snapshot

Commit bd6e9484 passes the complete CI workflow, CodeQL and the native Windows reproducibility/package/
persistence job. The Android native job refuses a changed ca-certificates package before compilation.
The Linux recipe restores the original signed Ubuntu snapshot with an isolated exact-release
certificate bootstrap, strict complete-index retrieval and unchanged final tool/NDK/compiler locks.
A fresh local image passes the original host verifier. Final hosted clean candidates and stock-suite
recovery pass on 05dbecf0; no native binary or accepted package is replaced. [Evidence](quality/ci-common-recovery.md).


### D-433 final hosted recovery

Commit `05dbecf089dabc5f433b12dbede82f4e97b10d2b` passes all three workflows:
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906007),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906028) and
[Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906072).
The actual Windows job passes exact compiler qualification, two clean candidates, immutable package,
drift refusal and protected persistence/workloads. Android passes host restoration, two clean
candidates, malformed-slice refusals and the official stock suite with reviewed static crypto.
The single scheduled check is complete and deleted; no successful job is rerun manually. The earlier
bd6e9484 Android package refusal remains historical failure evidence. This establishes hosted CI
recovery for that exact commit, not app/device/provider/store/release acceptance.


## D-434 - Original account monetary contexts

Opening balance, optional installment and observed balance retain independent original numeric
culture/currency/display-unit contexts. Display changes preserve raw input, original captions and
format examples; currency/unit changes require complete clearing/reentry. Preserve unknown principal,
historical loan/lent signs and decimal-rate meaning. Original file/presentation/edit revision guards
survive writer waits and reconciliation consent. Reject stored-currency changes before comparison,
including the match path. Postcommit default selection remains bound to the original file and native
owner; a failed follow-up never rearms the accepted account Save.

23 new distinct actual-source cases and 49 prior affected cases pass selectively. Original running
matrices complete 110 Windows/38 independent Release/AOT Android observations; two additional default
scenes and four read-only currency views per platform pass: totals116 Windows/44 Android. Final
strict builds and complete signed offline APK qualification are recorded in the linked evidence. No successful financial action or unrelated full suite is replayed.
See the account-money evidence document for exact identities, failures and limits. D-386 priorities
and physical-device/iOS/provider/store/release gates remain open. Holding monetary inputs follow as
the next separate development batch.

[Account monetary evidence](quality/account-money-context.md).


## D-435 - Original holding monetary contexts

Payment, fee, price-currency amount and opening/gift basis retain independent original currency,
display unit and numeric culture. Currency/unit changes require complete clearing/reentry. Quantity
keeps its original numeric culture because per-unit money depends on it; examples remain numeric.
Hidden fields do not reinterpret another event kind. Native account/unit/mode/kind choice and raw
edit revisions remain bound under original writers, including change-and-return. Manual-price
Refresh retains original raw meaning and known acceptance; old confirmations cannot rearm it.
Only a blank optional second amount requests unknown value; malformed nonblank input is refused.

24 distinct new source cases and 46 prior affected cases pass selectively; eleven original failures
are reproduced. Strict Windows, Android Debug and independent Release/AOT builds and the complete
signed offline APK pass without warnings/errors. 147 distinct Windows and 51 independent Android
observations complete across retained original scenes and final continuations, without accepted
purchase/opening replay. All 50 stored table sets match the original final checkpoint; actual Manual
and automatic Purchase valuations are verified separately. Main upgrades without reset with its
original account/Home0.00EUR and unchanged OS settings. No full-suite baseline is invented. D-386 stability/accessibility order and physical-device/iOS/provider/store/
publication gates remain unchanged.

[Original holding evidence](quality/holding-money-context.md).

## D-436 - Original secondary monetary contexts

Goal contributions/assumed prices, essential estimates, forecast prompts, occurrence overrides/payments,
settlement bills and shared cost/repayment/allocation inputs retain their original numeric culture,
currency and display unit. A later currency/unit change requires complete clearing and reentry;
partial editing cannot silently renew interpretation. Blank optional assumptions remain different
from malformed nonblank input. Estimates and earmarks do not become ledger movements.

Original file, stored facts, raw input revision and native owner are rechecked through asynchronous
consent and the actual atomic writer, including a change and return to the original display value.
Known acceptance retires the submitted input before notification/readback; a follow-up failure never
rearms the financial Save. Display-unit preferences publish globally only after the selective durable
Settings write has committed for the original owner. Long unit actions use the existing wrapping
button. Unit-refusal feedback is republished beside both original shared main amounts and shares.

Selective actual-source evidence is101 distinct new cases and149 affected prior cases, with17 original
source defects reproduced. All367 applicable Windows observations and127 independent Release/AOT
Android observations complete without accepted-action replay. The extra36 superseded household
feedback frames remain historical evidence. Android household review uses an actual new signed owner
admission and protected native source: old recovery/ownership/device revocations remain effective.
Its two contacts and one private KWD1001 cost are explicit independent setup, not cash ledger entries
or UI saves; the18 presentation states preserve all50 table sets and record zero accepted commands.

Strict Windows/Android Debug, independent Release/AOT and complete signed offline APK builds are
warning/error free. No schema/entity model, permission, external provider, commercial activation or
shipping SDK is added. No unrelated full-suite baseline or unit-test count is inferred from native
observations. D-386 dedicated stability/accessibility order and physical-device/iOS/hardware-key/
power-loss/provider/store/publication gates remain unchanged.

[Secondary monetary evidence](quality/secondary-money-context.md).

## D-437 - Original physical and manual-rate numeric meaning

Free holding-type purity and per-item gram weight retain independent original numeric cultures
through display changes and partial typing. Complete clearing renews only that field. Original
format examples and all field-local validation messages remain visible; original stored type and
raw/choice revisions are rechecked under the actual writer.

Manual exchange rates retain the original decimal parser, pair, file and immutable stored-rate
review through loading, Save and delete consent. A later source/pair requires explicit reentry.
Plain ratios never use display-unit factors or change recorded money. Known typed acceptance
consumes the submitted draft before fallible notification/readback; Refresh reads only. Small
positive rates keep complete decimal precision in the list and deletion consent, with a separate
estimate-badge row. Error categories translate without repeating the original command.

Selective evidence is 42 distinct new actual-source cases and 19 affected prior cases; eight actual
source failures were reproduced. Twelve affected resource checks pass. Final native/package
continuation and retained failures are recorded in the linked evidence; native observations do not
increase the unit-test count. No schema/model/portable format, permission, provider or shipping SDK
changes. D-386 stability/accessibility ordering and external release gates remain unchanged.

[Original physical/rate evidence](quality/physical-numeric-context.md).

Final scoped evidence is 203 applicable Windows and 71 independent Release/AOT Android observations,
including read-only recovery of 22 original outcome frames after a report-name collision. Accepted
operations are not replayed. Strict platform/independent and complete signed Offline APK builds pass;
Main upgrades without reset and its actual rates page/return preserve My account/0.00 EUR. Native
Windows 0xC0000005 after completed note captures and Android ANR/Wait remain real unresolved failures,
not stability acceptance. Full source/checkpoint/package details are in the linked evidence.

## D-438 - Distinguish retained native failures; qualify new numeric OS scopes

Exact original Windows module/PDB symbols locate fatal corrupted-state handling after a WinRT
finalizer boundary, not the original corruptor or Android's separately symbolized Mono bridge cause.
One fresh isolated API35 control still has expensive precise clock calls; it does not justify a
product clock/runtime/GC change. Retain actual Android startup ANR and same-process continuation.
New independent Release/AOT OS200%/360/412 checks complete96 observations/360 native readings for
physical purity/weight and manual-rate input in en/fa/de, Light/Dark. Actual complete source/base
rows remain unchanged and accepted commands are zero. Exact OS and Main17310/My account/0.00 EUR
restore. No shipping code/APK or managed-test count changes; prior successful cohorts are retained.
General stability and spoken/remaining accessibility acceptance stay open.

[Retained fault and numeric OS evidence](quality/retained-native-fault-classification.md).

## D-439 - Complete original monetary-text OS geometry scopes

Actual independent Release/AOT OS200% qualification resumes four original completed sources rather
than reseeding or repeating accepted commands. Account/debt/observed balance, holding/quantity/price,
entry/fees/foreign/reimbursable/split and goal/budget guides/errors complete288 observations and2340
native readings across360/412dp, en/fa/de and Light/Dark. Full text, ellipsis, final-line height and
raw values match; complete original50-table checkpoints/base rows remain unchanged. A narrow native
command gate permits reads and the known invalid-field refusal, not accepted writes. Exact original
OS/Main17310/My account/0.00 EUR restore. No source test count, shipping code/APK, model, permission or
provider change; successful older scopes are retained. QA-06 and remaining/audible accessibility and
external acceptance stay open.

[Original monetary OS evidence](quality/original-money-os-qualification.md).

## D-440 - Stop silent Android decimal-character loss

Three actual InputConnection failures remove German decimal commas before the form can validate them.
Preserve ordinary numeric raw text and numeric keyboard flags in the shared Android mapper; exclude
passwords and retain length filters.20 focused native input observations and four adapter guards pass
on the original protected source/base without accepted writes or replaying old financial/layout suites.
No new managed cases or parser/schema/permission/provider changes are claimed. Strict platform and
complete signed Offline APK checks pass; general ANR/crash and remaining acceptance stay open.

[Original native input evidence](quality/android-numeric-input.md).

## D-441 - Qualify retained secondary monetary layouts at system scale

Three completed original D-436 sources provide204 observations/900 native readings across actual
OS200%,360/412dp,en/fa/de and both themes. Targeted full text/line height/raw values and native
forecast prompt targets pass. Guard accepted receipts and complete source/base equality; permit only
known refusals and draft-only suggestions. A recovery-only marker is conditional on actual recovery.
No accepted write or old successful scope is replayed. Strict independent build/package checks pass;
exact OS/Main3522/Home restoration passes. Shipping code/APK and managed count remain D-440.

[Secondary monetary OS evidence](quality/secondary-money-os-qualification.md).

## D-442 - Qualify current title actions without source mutation

Home and empty/filled Plans complete36 observations,60 header states and120 native readings at
actual OS200%,360/412dp,en/fa/de and both themes.24 real Add/Cancel sequences preserve complete
base/source rows; filled Plans geometry uses its original accepted source without commands. Measure
actual viewport space instead of the obsolete two-row Home assumption; retain the Undo row. Strict
independent build/package and exact OS/Main3522 restoration pass. Shipping code/APK remain D-440.

[Current header OS evidence](quality/header-os-qualification.md).

A further36 read-only native-coordinate observations verify the action is physically right of the
complete title (minimum gap8px), without replaying any Add/Cancel. Total D-442 evidence is72
observations/96 header states/192 native text readings; accepted writes remain zero.

## D-443 - Read retained rate outcomes at system scale without restoring deleted data

36 observations/144 native readings qualify original rate feedback, exact historical precision/date
and estimate badge at OS200%,360/412dp,en/fa/de and both themes. Read immutable original accepted
rows/captions into the actual disabled presentation collection; never forge a writer review or
restore the deleted rate in SQLite. Complete source/base equality and disabled native commands pass.
Strict independent build/package and exact OS/Main3522 restoration pass. Main/APK and managed count
stay D-440; active/current-rate and general release acceptance are not inferred from this projection.

[Retained rate outcome evidence](quality/rate-outcomes-os-qualification.md).

## D-444 - Qualify original protected household geometry without authority renewal

Actual current signed test authority remains protected/writable with one cost,two contacts and no
repayment.36 OS200% observations/312 readings pass360/412dp,en/fa/de and both themes. Guard original
receipts, stop foreground sync before startup wait and skip all enrollment/identity paths. Only known
changed-unit refusals run; complete source/base/catalogue remain unchanged. Strict variant build,
exact old-APK restoration and exact OS/Main3522 restoration pass; Main/APK and managed count remain
D-440. Earlier D-442/D-443 CI/CodeQL succeed; broader stability/device/release gates remain open.

[Protected household OS evidence](quality/household-os-qualification.md).

## D-445 - Diagnose original currency query input ownership

Two original emulator scopes produce ten native observations: arrow-only list opening retains the
name's modal-root focus; text sent there appends USD to the name. Explicit actual currency focus,
filtered native USD selection and Cancel/Discard succeed with complete source/base equality and
zero accepted writes. Activity.CurrentFocus alone is insufficient for modal ownership. A separate
signed offline arm64 empty-source prerequisite builds without warnings; phone input waits for the
requested exclusive coordination reply. Historical D-422 phone cause remains unproved. Shipping
app/APK and managed baseline remain D-440; no successful financial action or geometry is replayed.

See [currency focus evidence](quality/currency-input-focus-boundary.md).

## D-446 - Move original Home goal progress off the window thread

The unchanged pure goal evaluator now runs on a worker; original metadata/formatting and covered,
current-owner publication remain on the caller. Five new and three affected prior cases pass
selectively, including actual task/thread identity and complete original goal output/source equality.
Three native Home contexts on two retained active goals preserve source/base rows and window-thread
publication; no accepted seed or payment is replayed. Strict Windows/Android/independent builds and
complete signed Offline Main upgrade pass without warnings. Original Main Home139 nodes and OS
remain; no general ANR/crash/two-second/low-memory or phone/iOS/release acceptance is inferred.

See [Home goal worker evidence](quality/home-goal-worker.md).

## D-447 - Publish the complete original goal list under a local cover

The goal list now reads one captured original file/transaction, keeps its existing financial
calculation on a worker and publishes active/history/funding rows under one translated loading/error
cover. Exact row/command/page lifetime guards refuse stale input; explicit retry and detail return
preserve the original source. No financial algorithm, schema, permission or runtime policy changes.
Fourteen new actual-source cases, three affected prior cases and23 actual resource checks pass
selectively. Eighteen Windows contexts produce84 native observations, six Android contexts produce
36 observations (34new/2retained); original source/base rows remain identical with zero new writes.
Complete signed Offline Main upgrade and actual empty-list Add/Cancel pass without reset; Home139
nodes match the old upgrade except focus, and final return differs only in one drawing-order value.
Strict builds and test-output cleanup pass without warnings. Failed native target/capture-path
observers are preserved and corrected against original receipts without repeating completed scenes.
No general ANR/crash, two-second, low-memory, spoken, physical-phone, iOS or release acceptance is inferred.

See [original goal-list evidence](quality/goal-list-publication.md).

## D-448 - Keep complete original goal details and actions in their page lifetime

Original goal details now use one complete captured file/transaction and worker financial
calculation under a local loading/error/retry cover. Page/identity revisions retire old commands
and pending reads while retaining original dirty money inputs. State/delete writers check the
original action inside their transaction before changes and commit, and preserve known accepted
results across observer failure. Three original regressions reproduce identity, pending-delete and
file-adoption defects;23 new focused cases and58 affected prior goal cases pass in selective runs,
plus19 original monetary-context and23 resource cases. Native Windows146 and Android50 observations
(31new/19retained Android) preserve retained fixture rows with no new fixture writes or replayed fixture
actions. Strict builds, signed Offline Main upgrade/Home139-node/OS checks and test cleanup pass.
Observer/compile failures are retained and repaired without replaying completed native actions.
General stability/performance, spoken/device/iOS, all protected-Main goal variants, native
destructive confirmations and genuine external gates are not accepted by this finite batch.

See [original goal-detail evidence](quality/goal-detail-publication.md).

Final source regressions also cover failed/busy/cancelled Edit validation and retained-stack theme
refresh. Only2 extra native observations per platform qualify the new Edit refusal/retry path;
completed cohorts remain. One new production-app test profile/account-opening/goal Save and one
cold read qualify positive protected-Main account-balance details100/150/50EUR and Edit/Cancel,
with All-history empty. Only that new profile is removed via full two-question UI cleanup. Main's
profile-list/Home139 nodes and0.00EUR return unchanged; English/Dark/Simple and OS are read back.
These are controlled new metadata saves, not repeated fixture payments or full protected-goal,
cryptographic, stability, spoken, physical/iOS or release acceptance.

### D-449 - Original goal-page OS text and native toolbar height

The original Persian title needs58px but received48px at actual Android OS200% text. The owned
native toolbar grows for its measured final line without reducing text size or replacing navigation.
Observers retire with the toolbar and growth requests are deduplicated within that lifetime.
Forty-eight immutable contexts contain216 native observations across360/412dp, en/fa/de and both
themes; completed contexts and native Retry are retained rather than replayed. Original fixture/base
rows remain unchanged with zero financial writes. Two unresponsive candidate sequences and a lost
log-chunk report are preserved; compact read-only durable receipt verification completes the report
without replay. Strict Windows/Android builds, complete signed Offline APK384 AOT entries, same-data
Main upgrade, original Goals read and Home139-node return pass. Exact emulator OS settings return.
No managed test baseline increase or general ANR/crash, speed, spoken/physical/iOS, provider/store
or release acceptance is claimed. See [native goal-page evidence](quality/goal-page-os-text.md).

### D-450 - Retire native toolbar listeners on the current Android tree

Three actual native roots reproduce a cached dead observer while the current observer is alive and
different. An exact separate native listener now removes from the current live tree and detaches
its weak owner before releasing the toolbar. Six focused root observations at actual OS200%/360
and412dp retain one active listener and retire four previous owners; two original Persian title
checks retain34px text,58px complete height and66px toolbar. Independent base rows and financial
writes remain unchanged/zero. The direct-tracker candidate's real native activation crash and two
caller compile repairs are preserved. Strict Windows/Android builds and complete signed Offline
APK384 AOT entries, no-reset Main upgrade, Goals/Home139-node return in the same process and exact
OS restoration pass. The transient null tree is continued read-only without replayed input/restart.
MAUI source references are corrected to the actual unchanged10.0.110 pin. No new managed-test count,
replayed D-449216-scene scope or general GC/ANR/speed/spoken/physical/iOS/release acceptance is claimed.
See [native toolbar lifetime evidence](quality/native-toolbar-observer-lifetime.md).

### D-451 - Complete one original read-only navigation pulse sequence

After D-450, one Release/AOT process completes18 original detail/Back visits with en/fa/de and both
themes, one current native root/subscription per cycle, no financial commands and exact source/base
equality. A500ms at-most-one UI probe records maximum callback age524ms and no ages over2000ms;
read/publication/Back/Home intervals are812-1436ms, median942ms on the small original source, not
whole Home/cold/tenfold or two-second acceptance. The exact PID's event buffer retains its start
with zero recorded ANR/crash events. Strict independent Android/Windows builds and signed Offline
caller pass. Main remains D-450: same PID15971, Home139 nodes, display and all original OS values.
No old216-scene matrix, accepted financial action, full suite or unchanged shipping APK is repeated;
no managed-test baseline is added. General stability/performance/spoken/physical/iOS/release gates
remain open. See [original navigation evidence](quality/original-navigation-pulse.md).

### D-453 - Original Main cold start and retained-process background return

The unchanged complete signed D-450 Offline APK passes one genuinely cold Main launch on the
owned emulator: Android COLD/Activity2908ms, new PID17030 and exact original139-node public Home.
The package-scoped background-kill request leaves that same PID alive (LAST/previous); its later
HOT89ms return also preserves the complete public tree. This does not qualify process reclaim,
natural low-memory, whole-Home latency or private database equality. The exact PID's event buffer
retains its start and zero recorded ANR/crash events in this finite scope. APK/OS remain exact;
no financial commands, managed tests, unchanged builds or successful old sequences are repeated.
Only three exact hash-matched remote captures are removed. General stability/performance/spoken,
physical continuation, Mac/iOS and external release gates remain open. See [cold lifecycle evidence](quality/main-cold-lifecycle.md).

### D-454 - Recover the original Main task after confirmed cached-process death

A phase-free existing owned Activity supplies the missing cache precondition; preserve its first
LAST result, then require actual CAC/cached=true/adjustment900 before package-scoped kill. PID17030
is confirmed absent and its native intentional-kill event retained before recovery as17431. Android
reports COLD/Activity1334ms; the original139-node public Main, APK and OS values match. No review
route, financial command, build or old successful sequence runs. The quiet owned process is stopped
and two hash-matched remote captures removed. Natural low-memory, private full-data equality,
whole-Home timing and broader stability/platform/release gates remain open. See [cached-process evidence](quality/main-cold-lifecycle.md).

### D-455 - Bound the native scroll-readiness candidate without an unsupported product change

The pinned MAUI scroll wait supplies a concrete candidate, not an application-cause proof. The
original16-request control clears its native flag after two of twelve samples; no scroll is armed
and original source/base rows match. A separate hidden-root condition refuses readiness twice;
three actual snapshots establish attached/focused but already-laid-out412x640 geometry. Those
refusals are preserved, not relabelled as a scroll hang or passing regression. Three strict signed
Offline builds pass; experimental source is removed and the exact D-451 independent APK restored.
Main17431/139 nodes/D-450 APK/OS match; no financial command, managed-test increase or successful old
scope is replayed. Stability/performance and external gates stay open. See [native scroll evidence](quality/native-scroll-layout-investigation.md).

### D-456 - Resolve the remaining original ECB test-capture race

Failed-job logs from delayed older CI reports match D-433's already-corrected mode-policy,
holding Undo fixture and Windows installer boundaries. The unchanged ECB cancellation test has
one separate race: handler-entry readiness precedes capture of its actual request. Await that
captured request explicitly before cancellation/response construction; retain every original
transport, one-request and cross-pair assertion. Production app/service/APK remain unchanged.
Strict affected API/test compilation has zero warnings/errors; exactly one changed test passes,
no new managed cases and no unrelated local suite/build/device replay. Exact correction-hosted
completion remains pending at commit creation. Preserve unfinished D-452 and all external/runtime
release gates. See [failed-job audit and verification](quality/ci-common-recovery.md).

### D-457 - Complete retained physical USD selection and native draft discard

Three actual owned-window observations explain the failed observer: the USD native button is
124px high while its popup exposes123px. Preserve the full-width/owned semantic native target and
44dp minimum within that one-physical-pixel rounding allowance; production controls are unchanged.
The final ARM64 Release/AOT caller passes without warnings/errors,192 AOT entries, offline manifest
and v2/v3 signatures. Actual Samsung selection and Cancel/Discard succeed; four prior scenes are
retained, one new selection observation is added, accepted financial writes stay zero and complete
original source/base rows match. Remove only the new probe and two hash-matched remote captures;
Main7361/Home141/979, original APK/OS and prior packages match. Both coordinated phone turns are
released. Retain the typed wait, initial build-flag and host-verifier/decoder failures separately;
no managed-test increase or successful old suite/financial/matrix replay. D-452 is complete for
this controlled input scope; historical D-422 cause, general stability/spoken/platform/provider and
release acceptance remain open. See [physical continuation evidence](quality/currency-input-focus-boundary.md).

### D-458 - Continuous owner-reserved phone and original physical goal lifecycle

The owner reserves Samsung for this project until further notice; cross-project coordination is
no longer required, while data/settings/foreground/authentication limits remain. Reuse the exact
complete signed Offline D-450 APK and upgrade without reset. One new fictitious profile/account
opening100 and goal150 produce the actual100/150/50 list/detail and positive pinned Home card.
Dirty native Discard, pause/resume, confirmed completion/reactivation, archive, one cold read,
declined/accepted native goal deletion and empty All-history pass. Only the new profile is removed
through both native confirmations; original three profiles/Main979/141 post-upgrade nodes/OS and
APK hash match.34 assertions qualify stored observations, not new managed tests. Retain native
null-tree/tab/encoding/host-precondition failures; do not replay accepted commands or old suites.
General stability/performance/spoken/other goal/platform/provider/release gates stay open.
See [original physical goal evidence](quality/physical-goal-lifecycle.md).

### D-459 - Original physical earmark and holding-quantity goals

Continue the two remaining goal kinds on the owner-reserved Samsung using unchanged signed
Offline D-450. One new fictitious profile opens with cash900. Earmark150 starts at0, accepts
set-aside25, refuses release26 while preserving that draft, and accepts corrected release10:
progress15/150/135, unassigned885 and original+25/-10 history. One countable holding records
already-owned3 with original basis30, without a cash movement. Quantity goal5 shows3/5/2;
assumed price12 changes only the estimate, not quantity or holding value. Both pinned Home
cards and cash900 agree. One cold launch preserves both original goals, allocation history and
assumption; All/All-types ledger history is empty. Remove only the new profile through both
native confirmations. Original three profiles/Main979/all141 public nodes/OS/APK hash match.
33 behavioral observation assertions and113 capture hashes pass; these are not new unit tests.
Retain navigation/publication/null-tree and host-verifier failures; repair observation against
stored originals without repeating accepted saves or cold launch. Code/APK remain D-450;
no successful old suite/build/emulator scene is replayed. Protected variants, general stability,
spoken accessibility, other physical workflows, iOS/provider/store/release gates remain open.
See [physical goal-kind evidence](quality/physical-goal-variants.md).

### D-460 - Original physical zero, exact and exceeded budgets

On the owner-reserved Samsung, one new original profile opens with900EUR and no budget.
Explicit overall/category zero limits show the no-spending hint. One Food expense10 exceeds
both zero limits by10. Editing the same budget to overall10/Food5 gives100% overall and5
category excess. One linked refund4 initially has Uncategorized after an observed mistaken
category toggle: overall6/Food10 is correct for that source. Correct only that refund's category;
overall/category net6, overall left4/60%, Food excess1 and cash894 then agree. One cold read
retains the complete packet. All-empty limits are refused; same-draft category-only5 succeeds
without an overall line. Cancel then accept original budget deletion; it preserves both10/4
ledger rows and894 cash. Remove only the new profile through both confirmations; original
three profiles/Main979/all141 public nodes/OS/APK hash match.35 behavioral assertions and135
capture hashes validate original observations, not new managed tests. Retain native transition,
invalid host-input and observation-verifier failures; do not replay accepted financial creates.
Program/APK remain D-450; no successful old test/build/emulator matrix is repeated. Broader
period/method/rollover, physical workflow, stability/accessibility/iOS/provider/release gates remain.
See [physical budget boundary evidence](quality/physical-budget-boundaries.md).

### D-461 - Original budget action lifetime and committed outcomes

Six original SQLite regressions reproduce retained Edit/Delete/Copy commands acting on a newly
published period and misreported accepted writes after Changed throws. Commands now retain their
original publication revision; native disappearance retires commands/consent and hidden Retry.
Pending reads cannot revive a retired page. Copy/replace/delete recheck current ownership in the
actual writer before commit; acceptance is recorded before notification, so explicit Retry reads
without repeating the write.15 new cases and26 existing action cases are verified distinctly. Four cases repeat after the hidden
Retry guard; two later callback/read regressions fail first, then only seven affected cases repeat and pass. Strict Windows and final Offline Android
builds have no warnings/errors. Retain failed native Windows attempts, including the first terminal
UI crash and later target-readiness failures. Three native metadata actions passed their financial
checks before the later observer failure; never replay them. The same source supplies six final
read-only en/fa/de Light/Dark Windows captures with original base/nonbudget rows unchanged.
Complete signed Offline APK has384 AOT libraries; Samsung upgrades without reset and original
copy-next/Edit dirty Discard/delete-cancel/delete/copy-previous/tab/period reads pass with cash900
and empty All-history. Remove only the new profile; Main979/three profiles/all141 public nodes/OS
and intermediate candidate hash match (26 assertions/78 captures). After the last callback/read
guards, upgrade the final signed candidate without reset; existing D316 Edit/Cancel preserves
its budget50/Home990 without any write. Return Main979/three profiles/all141 nodes/exact OS
and final APK hash in the same PID12903 (9 assertions/34 additional captures). These are
original observations, not unit cases; accepted financial/native writes are not replayed.
No schema/provider/permission or financial formula changes. General stability/accessibility,
complete budget financial snapshots, other platform/workflow and release gates remain open.
See [budget action lifetime evidence](quality/budget-action-lifetime.md).

### D-462 - Coherent original budget financial publication

The original Budget page now captures its complete review, accounts/categories/preferences, ledger,
plans/states and goal earmarks on one original-file lease and one deferred SQLite transaction.
SQLite materialization and the unchanged financial calculators run on workers. Complete rollover
history, envelope cash to today, refund netting, confirmed-only display, short-period limits and
monthly flex/planned arithmetic retain their original meaning. The covered page publishes the
complete calculated rows synchronously after original review validation; hidden/error/Retry state
never exposes a partial financial packet. No entity/schema, permission, export or provider changes.
Fourteen new actual-source SQLite/financial-worker cases and seven affected prior command cases
pass selectively; no complete suite or accepted financial commands repeat. Eighteen independent
Windows method/language/theme observations retain all fictional source and base rows; own-window
images and exact separate desktop restoration are verified. Android retains10 completed contexts
across a real ANR/timeout and runs only the8 missing contexts on its original source, yielding18
complete contexts; the exact original public report is recovered without replay or invented fields.
Final strict builds and a complete signed Offline APK with384 AOT libraries pass. Samsung upgrades
without reset and reads its retained50/10/40/planned30 budget, then returns Main18990/979/141 nodes,
three profiles/exact OS/final APK hash; no financial writes or profile creates/deletes occur.
The final order correction repeats only its one failing source case and final-source read/builds.
ANR remains open. Complete scope, artifact and retained-failure evidence is recorded in
[budget financial read evidence](quality/budget-financial-read.md). General ANR/crash,
speed/low-memory, full accessibility, other original budget workflows, iOS/provider/Store/release
acceptance remains separate and open.

### D-463 - Original budget periods and physical rollover

The actual Samsung weekly/two-week workflow exposed shared captions that still described months.
Use period-neutral editor/limit/empty/copy/delete/planned/rollover wording in all six languages,
show the exact original financial date range in the growing editor header, and omit monthly
calendar annotations for short periods. Keep financial rules, raw draft money and all stored
amounts unchanged; monthly methods and genuinely monthly navigation retain their own wording.
Four new actual-source date-range cases (calendar month, shifted financial month, week, fortnight)
and six affected live-caption cases pass; six resource contracts pass. No complete suite repeats.
Eighteen independent Windows caption contexts produce54 own-window images at360/412/1280 widths;
six Release/AOT Android contexts qualify original captions/date headers without financial saves.
The first Windows observer incorrectly required44px from the existing40px MoveButton; retain its
failure and first image, continue the original source, and qualify only the missing observations.
Samsung uses one fictitious profile, opening900 and one Food expense10, with ten budget metadata
writes: monthly positive carry90/30, Start fresh50/20, surplus-only50/20 and deficit carry45/15;
weekly next30 with carry10 and fortnight next50 with carry20. Final signed Offline APK upgrades
without reset; one cold source read retains Home890, one ledger expense and all three final
period results. Edit/Cancel and delete-confirmation Cancel verify corrected native text without
replaying accepted writes. Delete only the new profile with both native confirmations and retain
Main979/all141 public Home nodes/the original three profiles/exact OS in final PID26903.
Strict Windows/Android builds pass without warnings; complete APK has384 AOT libraries and local
v2/v3 signature. These observations are not additional unit tests. General ANR/crash, speed,
low-memory, full accessibility/TalkBack, iOS/provider/Store and release acceptance remain open.

Evidence: [original budget period and rollover qualification](quality/budget-period-rollover.md).

### D-464 - Original physical envelope and Flex budgets

Continue QA-01 on owner-reserved Samsung with the unchanged signed Offline D-463 APK. One new
fictitious profile opens900, posts one Food expense10, earmarks25 for a goal and saves two unpaid
plans: monthly Housing30 and yearly Insurance120, both reminder/autopost off. Original envelope
budget200/Food100 shows cash890, goal25, unspent90 and unassigned775. Correct only Food to5:
overspent5 assigns nothing extra, so unassigned865; spent cash is not deducted twice. Change the
same budget to Flex50: variable10/50/40, fixed0 paid/30 expected, nonmonthly share10/0 paid, month
plan90. Actual due expenses150/two items remain separate from the comparison-only monthly share10.
One original cold start retains Home890/Flex40 left, goal25, both unpaid plans and exactly one
ledger expense. Delete only the new profile through both native confirmations; Main979/all141
public nodes/original three profiles/exact OS/unchanged APK match in final PID3591. Twenty-eight
assertions validate retained observations, not new managed tests. No code/APK change, rebuild,
old suite, accepted financial replay or emulator matrix is needed. General stability, complete
device/workflow acceptance, scaling/TalkBack, iOS/providers/Store and release gates remain open.

Evidence: [original physical method qualification](quality/physical-budget-methods.md).

### D-465 - Original budget account and currency boundaries

The original Simple editor now explains its retained account restriction, including archived or
missing references and dirty scope, without changing raw amounts or financial writes. Six new
actual-source SQLite cases and six resource contracts pass; eighteen Windows contexts/images and
six independent Release/AOT Android contexts qualify only the changed note. Old financial/period
matrices and accepted commands are reused. Strict Windows/full signed Offline Android builds have
zero warnings/errors. One retained Samsung profile verifies EUR30 across both accounts, secondary20,
primary10, independent USD30/50 and secondary EUR0 after MonthStartDay15; Persian display and USD
valuation preserve original budget currencies/calendars. Final cold PID19994 retains the corrected
note, EUR0/100, USD30/50/20 and balances890/80EUR and670USD. Delete only the new profile with both
confirmations; Main979/all141 public nodes/original three profiles/exact OS/final APK match.
Twenty-three observation assertions are not unit tests. General stability, full device/workflow,
spoken accessibility, iOS/provider/Store and release acceptance remain open.

Evidence: [original account/currency/pay-cycle qualification](quality/budget-account-scope.md).

### D-466 - Original physical incomplete forecast and saved scenario

Reuse the unchanged signed Offline D-465 APK on owner-reserved Samsung. One new fictitious profile
opens100EUR and saves three Once plans: expense150 on23 October, unknown expense on24 October,
income200 on30 October, all reminder/autopost off. Original end150/minimum-50 on23 and one unknown
become end130/minimum-70 on24 only after explicit temporary assumption20. Save one original named
scenario; its accepted Save disables, original detail retains the assumption and correctly defers
future actual comparison until tomorrow. Reset restores150/-50/unknown without changing the saved
scenario. One actual cold launch retains current original forecast and independent saved-70 path.
Cancel then accept native snapshot deletion; original three plans, cash100 and empty All-history
persist. Delete only the new profile with both confirmations; Main979/all141 public nodes/original
three profiles/exact OS/unchanged APK match in PID27525.33 assertions over retained observations
are not unit tests. No code/APK/build/suite or accepted-action replay. Later-date comparison,
missing-rate/other scenario variants, full device/stability/spoken/iOS/provider/Store/release gates
remain open.

Evidence: [original physical forecast qualification](quality/physical-forecast-snapshot.md).

### D-467 - Original purchase currencies and valid amount examples

Original Samsung purchases expose an invalid two-decimal error example for JPY. Format only the
original-purchase error from its actual ISO currency and retained numeric culture, independently
of display units. Seven new distinct actual-source cases and selected existing/resource contracts
pass across retained runs.54 Windows error contexts use53 retained valid images plus one bounded
visual recapture;18 independent Release/AOT Android contexts change no financial rows. Preserve
review-timeout, fixture, image-index and native-input failures instead of replaying successful work.
Strict final Windows/complete signed Offline Android builds have zero warnings/errors. Upgrade
without reset; actual COLD PID15323 retains the three original EUR10/20/30 expenses with12.34USD,
123JPY and1.234KWD metadata, Home940, and the corrected JPY rejection/Cancel. Delete only the new
profile with both confirmations; Main979/all141 public nodes/original three profiles/exact OS and
final APK match.23 observation assertions are not unit cases. No accepted financial Save repeats.
Full financial/device/stability/spoken-accessibility/iOS/provider/Store/release gates remain open.

Evidence: [original purchase currency qualification](quality/physical-original-purchase-amounts.md).

### D-468 - Serviced native UI framework and original lifecycle qualification

Adopt stable MAUI Controls/Essentials 10.0.120; eight existing modules advance with no new SDK or
permission. Official signatures/licences pass; Preferences/FileSystem sources, ten runtime/cipher
engines and 25 branding assets remain unchanged. Strict Windows/full signed Offline Android
builds have zero warnings/errors. Eighteen independent Release/AOT navigation cycles preserve
complete original rows and one native root/subscription per cycle. Eighteen Windows contexts
produce 36 inspected images with unchanged ledger entries. Correct the observed null expected
count in the report driver and validate its same completed run without native replay.
Samsung and emulator upgrade without reset. Six physical language/theme contexts cancel six
expense drafts and six plan drafts, including six dirty-discard confirmations, with no financial
saves. Delete only the new profile through both confirmations. Phone PID21905 retains Main979,
141 public nodes, three original profiles and exact OS; emulator PID26264 retains Home0/139 nodes
and exact OS. Twenty-seven observation assertions are not new managed tests. Finite event buffers
record each start and no ANR/crash; general stability is not accepted. The ScrollToAsync retry-loop
fix is absent and Mono/.NET10.0.13 unchanged. General ANR/GC/crash, low-memory, complete/spoken
accessibility, two-second Home, iOS/provider/Store and release gates remain open.

Evidence: [bounded native servicing qualification](quality/maui-servicing-qualification.md).

### D-469 - Original physical cached-process Main recovery

Use the unchanged signed Offline D-468 APK on owner-reserved Samsung. A phase-free existing owned
Activity supplies the missing cached-process precondition without running a review route. Preserve
the initial adjustment700/cached=false refusal; require actual cached=true/state16/adjustment900
for exact Main21905 before one package-scoped kill. Confirm death before retained-task recovery
as9423/COLD/Activity1392ms. Preserve the initial cover and later equal141-node Home979 in the same
PID, five known original public history rows and three original profiles. Restore This month and
Home, exact OS/both APK hashes; stop only the quiet owned process and remove17 hash-matched XMLs.
No financial command, new profile, app change, build, managed case or successful old suite replay.
Finite native events contain the intentional kill/new start and no recorded ANR/crash; natural
low-memory, private full-data/key equality, general stability, whole-Home speed, spoken/platform/
provider/Store and release acceptance remain open.

Evidence: [physical cached-process recovery](quality/physical-cached-main-recovery.md).

### D-470 - Serviced original loaded Home and exact recent-row oracle

Remove only the accidental U+00C2 before the original U+00B7 separator in the independent recent
subtitle assertion; exact production-source extraction proves the old ordinal predicate rejects
the valid original packet. Production source/APK remain D-468. Strict signed Offline x64/ARM64
callers qualify the unchanged original protected10,000/100,000-row corpora after MAUI10.0.120.
Twelve native publications per target complete in one process each, with full schema/typed-row
equality, original balance/recent packets and no financial Save or old raw-reader/suite replay.
Emulator medians1040.70/2680.07ms; Samsung1739.94/4933.12ms. Largest dispatcher gaps487.90/405.60ms
and794.66/1094.35ms respectively. One reference phone sample is2011.86ms; two-second acceptance
remains open. Fresh current presentation baselines are not historical framework speedup proof.
Restore original independent APKs/stopped state; same emulator26264/Home139 and Samsung9423/
Home141/979, shipping APK and OS match. Four hash-matched XMLs removed. Native event scopes have
starts and no recorded ANR/crash; no new managed cases, general stability or cold/low-memory/
spoken/iOS/provider/Store/release acceptance. Next actual ready defect: two corrupted separators
in original household-cost captions, kept outside this frozen-source measurement.

Evidence: [serviced original loaded Home](quality/serviced-loaded-home.md).

### D-471 - Original household caption separators

Remove two corrupted separator prefixes from the existing refund-source and destination-value
captions without changing money, parsing or authority.36 final inspected Windows images reuse
36 successful original popup receipts;12 Release/AOT Android selected/popup checks preserve real
protected source/base rows and membership, with no financial Save. Destination-only packets are
explicitly transient formatter/control evidence, not stored cash-linkage acceptance. Preserve
first incomplete Windows captures and Android's terminal observer failure; continue only missing
visibility/readiness checks. Strict builds are warning-free. Complete signed Offline APK384 AOT
upgrades without reset; emulator28047/Home139 and Samsung17769/Home141/979 retain exact public UI
and OS. Restore the original independent APK/stopped state. No new managed cases or unrelated
successful-suite replay. Mac unavailable per owner22 October; independent Android work continues.
General stability, full financial/device/accessibility, iOS/provider/Store/release gates stay open.

Evidence: [bounded original caption qualification](quality/household-caption-separators.md).

### D-472 - Original physical account reconciliation

Close the missing Samsung AT-11 boundary on unchanged signed Offline D-471. One new fictitious
profile/account starts100EUR. Native112.34 comparison, change to212.34 invalidation, required
reason and Cancel preserve the draft. One accepted adjustment+112.34 gives212.34 and reconciled
10/22/2026; original row/reason/account/date and metadata persist after one real COLD17769-to21187
restart. Activity1457ms is not full Home. No accepted command is replayed. Delete only the new
profile with both original confirmations; restore Main979/all141 public Home tuples, five known
public history rows, three profiles, period and exact OS/APK hashes.32 observation assertions are
not new managed tests;106 hash-matched XMLs removed. App/APK remain D-471; no build or old suite
replay. Retain observer/viewport/modal-ownership refusals and same-process continuations. General
stability, full financial/device/accessibility, iOS/provider/Store/release gates remain open.

Evidence: [original physical reconciliation](quality/physical-account-reconciliation.md).

### D-473 - Original physical receipt Save and stored reread

Close the missing Samsung receipt Save/attachment/cold-reread boundary on unchanged D-471.
One new profile/account opens100EUR on16 October; retained fictitious17 October PDF gives24.50.
One original Save yields Food expense24.50,Home75.50 and one13KB PDF. Hash-delete only the original
input before one COLD21187-to26415 restart. Stored Scan receipt reads the original saved attachment
and opens Edit transaction with the same total; Cancel without Save preserves every public detail
tuple,one expense and75.50. Delete only the new profile with both questions; restore Main979/all141
public nodes,three original profiles,exact OS/APK.27 observation assertions are not new unit tests;
78 hash-matched XMLs removed. No new app code, APK, build or successful-suite replay. Trial watermark/
merchant,real corpus,camera,image OCR,all variants,stability/accessibility/iOS/provider/Store/release
acceptance remain explicitly outside this bounded result.

Evidence: [physical original receipt lifecycle](quality/physical-receipt-save-reread.md).

### D-474 - Original physical cold notification action

Close the missing Samsung ARM64 D-333 boundary with a complete signed Offline independent current-
source package and real permission/notification action. One fictitious account/Once plan is
prepared in29556; actual death precedes one In1hour tap. Native broadcast receiver starts30999
without Activity; completed status has protected storage,windows0,one saved/native pending snooze
and equal intentionally populated financial fields/zero ledger. A separate provider31245 preserves
one snooze without storage preparation. Preserve both early in-flight reads and observer refusals;
no accepted action/seed or old successful suite repeats. Independent strict build is warning-free.
Remove only the new fixture package; Main26415/Home979/all141 public tuples,OS and original APKs
remain unchanged.19 observation assertions are not new managed tests. General stability,OS timed
notification restrictions,interrupted-action/low-memory,full device/accessibility/iOS/provider/
Store/release gates remain open. Main code/APK stay D-471.

Evidence: [physical original cold action](quality/physical-cold-reminder-action.md).

### D-475 - Original physical timed alarm and notification route

Add scheduled-only mode to the existing independent caller; original application code is unchanged.
After native permission, actual Schedule time/day offset supplies a real future AlarmManager
request, with no immediate Show copy. One original alarm/PendingIntent survives preparationPID1388
death and read-only waiting. Native ScheduledAlarmReceiver starts3318 and posts original184973019
155216ms after requested time under the existing inexact/tolerance policy. No clock change/rearm.
Actual content tap opens the original upcoming12.34EUR/23October due page in the same process;
Back posts nothing. Populated fixture fields and zero ledger match; Home1000 remains. Remove only
the new package; Main26415/Home979/all141 public tuples,OS/APKs and charging-awake15 remain unchanged.
Independent strict signed Offline ARM64 build passes;26 observation assertions are not new managed
cases. Old successful Snooze/financial/Windows suites are not replayed. This awake/charging scope
is not exact timing,Doze,reboot,full lock/device/stability/accessibility/iOS/provider/Store/release
acceptance. Shipping Main/APK stay D-471.

Evidence: [physical original scheduled reminder](quality/physical-scheduled-reminder.md).

### D-476 - Exact native-review build boundaries

Repair the two current causes confirmed in actual D-471/D-474 hosted failures: three independent
fixtures now share their dependency's Windows Debug/independent-build gate, and their exact paths
join the source-policy list only after full outer-gate validation. The affected strict Android
Debug billing build and eight distinct existing boundary cases pass; no new tests or shipping
APK/visible behavior changes. Keep successful Release/native/financial evidence without replay.
Hosted completion remains pending at commit creation; external/stability/accessibility gates stand.

Evidence: [original hosted failures and focused recovery](quality/ci-common-recovery.md).

### D-477 - Hosted Windows installer metadata

Actual D-473 Windows logs expose a separate new hosted installer18.11.12315.225. Match the dated
official image inventory and add only that exact metadata version; preserve executable hashes,
Microsoft signature, pinned MSVC/SDK and immutable package checks. Eighteen focused qualification
cases pass against the existing signed local compiler. The new hosted compiler/package must still
pass on the exact pushed SHA; no app build/native scene replay or shipping APK change.

Evidence: [actual installer refusal and retained qualification](quality/ci-common-recovery.md).

### D-478 - Immutable native producers for serviced hosted Windows

D-476's actual hosted Android build and complete5097-case run now pass with25 skips/zero failures.
The remaining Windows failure is a different deterministic native output under the old candidate.8
identity, despite the same approved compiler. Preserve that refusal and old package identities;
qualify a separate candidate.9, compare every original archive entry, and select the explicit
hosted producer before cache/build use. Twenty-six local script cases,three actual central-version
evaluations and the real local-host/environment boundary checks pass; no new managed cases or
Android shipping APK change. Ordinary consumer and final exact-head CI remain distinct gates.

Evidence: [original drift, producer and consumer qualification](quality/ci-common-recovery.md).

D-478 producer follow-up: run39857667608 on9823a734 passes the original producer gates; all five
actual candidate.9 archive entries match the distinct inventory. Normal exact-head main consumer
CI remains pending at adoption commit creation; this is not platform/product/release acceptance.

### D-479 - Original physical widget actions without launcher changes

Use a fixed independent native AppWidgetHostView and actual per-widget platform consent to close
the missing Samsung original PendingIntent/cold-origin boundary without modifying the owner's
launcher. No Main reference, direct financial route, shell grant or private-store/key read. One
run retains widget2/host47901; malformed input and Debug/Main-identity build guards refuse. Strict
complete signed ARM64 host build passes without warnings. Four original Expense/Income/Transfer/
Logo actions and three Cancels preserve all141 public Home tuples and979EUR; Expense starts16655
after actual cached26415 death. Zero financial Saves; one owned widget removed and only the newly
installed helper uninstalled. Exact OS/Main APK match;25 hash-matched remote XMLs removed. Retain
package/foreground/consent observer corrections without accepted-action replay. No new managed
cases, shipping source/APK change or unrelated build/suite repetition. Actual launcher placement,
all lock/widget variants, general stability, spoken/large-text, other devices/iOS and release remain.

D-478's final exact-head109743f5 now has all five workflows successful,5097 cases with5072 successes,
25 skips and zero failures; original native consumer/schema/cold persistence/workload gates pass.
This closes the observed hosted failures, not product/platform release gates.

Evidence: [physical original widget](quality/physical-widget-pending-intents.md) and
[final hosted consumer outcome](quality/ci-common-recovery.md).

### D-480 - Release merged native validation listeners

Reproduce the common four-form event-wrapper defect on real Samsung native views: the initial
observer dies during attachment, old cleanup skips removal and three later layouts invoke the
retired callback three times. Use one original shared Android waiter with an explicit listener,
current-tree removal and neutralized completion; retain the original bound/form guards. Actual
fixed callback count0, attached completion and unattached bounded timeout pass. Strict Windows,
complete Offline Main and independent native helper builds pass; two identity/configuration guards
refuse. Final signed384-AOT APK upgrades without reset. Actual invalid Expense Save reveals the
original field/error; Cancel/Discard preserves Main979/all141 public tuples and OS. Retain failed
independent JNI registration and input/IME observer evidence, without replaying accepted cases.
No financial Save or managed-suite increment. General ANR/crash and other release gates remain.

Evidence: [actual original listener lifetime](quality/android-validation-layout-lifetime.md).

### D-481 - Complete the remaining original validation callers without replay

Continue D-480 only on the retained D-358 work-editor source in the independent x64 Release/AOT
app. Plan and occurrence native invalid actions pass, then settlement's obsolete diagnostic
constructor/loader refuses before its action. Update to the actual current constructor/public
LoadAsync, including the same stale EditorText setup. Preserve source/command/footer refusals;
use current commands and scroll only viewport descendants. Resume only settlement, retaining the
two completed receipts. Three named source/base-data comparisons pass with zero accepted financial
Saves. Final full receipt readback has all3 retained and NativeActionsThisPass0; no managed count
increase, Main build, old geometry matrix or financial replay. Strict signed Offline192-AOT caller
builds pass. Restore its original APK/stopped state, Main28047/all139 public nodes/OS/APK and remove
only2 hash-matched XMLs. Samsung is untouched. CI/CodeQL for original shipping head0e8c6fa6 succeed;
its one-shot monitor is deleted. General stability, full matrix/platform/release gates remain.

Evidence: [original remaining callers](quality/validation-layout-callers.md).

### D-482 - Retire validation callbacks across native detachment

An original real-native baseline crashes when a detached control returns a new live observer while
its registered root remains alive. Track distinct registration/attachment trees, remove the exact
global listener from every live tree before disposal, and use a separate weak attachment peer.
Detachment resolves false; keep the existing one-second bound and all original caller guards.
Five new native cases pass with one retained success and only four continuation cases; three
normal affected regressions also pass. Preserve actual baseline/registration/measurement failures.
Strict Windows and complete Offline384-AOT signed Main build without warnings. Samsung upgrades
once without reset; original blank-amount Save rejects and Cancel restores all141 public Home
nodes/balance979 and OS in PID27601, zero financial writes. Emulator Main28047/139nodes/APK/OS
remain unchanged, both newly owned helpers removed,2 emulator and8 phone hash-matched XMLs deleted.
No new managed-suite count or unrelated successful replay. Preceding head1c8d17d1 CI/CodeQL succeeds;
this pushed head requires its own check. General stability/performance/platform/release stays open.

Evidence: [detached validation listener ownership](quality/detached-validation-listeners.md).

### D-483 - Physical original native key protection

Qualify the actual protector/binding sources in one separate permission-free ARM64 Release UID,
never reading Main keys or databases. One original fictitious generation/recovered fixture passes
creation PID20062, actual cold PID20266, twelve binding/purpose/tamper/length refusals and retirement
PID20384: two denied-authority removals preserve keys, two original authorized removals prove absence,
and lost-key unwrap cannot create replacements. Both aliases report native security level1 (Trusted
Environment); this is OS metadata, not remote attestation, Main-key inspection or StrongBox proof.
Strict full native publish and actual v3 signature pass; preserve pre-install v2-observer refusal
and top-instance retirement delivery with no OnCreate. Continue only the unexecuted retirement,
without replaying accepted creation/cold. Remove the new helper and2 hash-matched XMLs, preserving
Main27601/all141 Home tuples/979/APK/OS. No managed count, Main build, financial command, schema,
SDK, permission or source algorithm change. Authentication/OEM/power-loss/iOS/release remain open.

Evidence: [physical original key protection](quality/physical-key-protection.md).

### D-484 - Physical retained plan and debt forms

Qualify original Samsung forms after D-429/D-430/D-482 on the unchanged signed Main APK. One new
fictitious profile has Cash100, one Once12.34 plan corrected to22.34 after native zero rejection,
Keep editing/Discard of99.99, and one debt50 with separate5.25 estimate. The original repayment
reminder draft remains unknown amount and is cancelled without Save; dismiss its contextual native
notification prompt without granting permission. One actual cold Main27601-to27860 preserves cash100,
one22.34 plan, debt-50/estimate5.25 and empty All-history. Debt defaults outside cash totals; retain
and correct the observer's wrong50 expectation without replay. Retain native scroll/retained-stack/
second-consent observer refusals. Switch back to original Main, delete only the new inactive profile
with both original confirmations, preserve all three-profile tuples/Main979/all141 Home tuples/APK/
OS/notification flags, and hash-remove144 XMLs. No code/build/managed count or accepted ledger replay.
Latest6c52cd67 CI/CodeQL succeed; the preceding D-482 CI is cancelled. Both one-shot schedules are
consumed/deleted. Full currency/race/platform/security/stability/accessibility/release gates remain.

Evidence: [physical original form and cold preservation](quality/physical-plan-and-debt-forms.md).

### D-485 - Follow the original Android toolbar between native roots

The actual native caller reproduces outgoing-root callbacks and missing callbacks after a toolbar
moves to a second window. Its separate weak-owner production observer now removes the exact old
registration at detachment and registers on the new root; the initial floating-tree merge is not
duplicated. Two focused native cases pass, for registration before/after attachment, with both
roots alive and zero detached/retired callbacks. Keep prior financial/title/scale/managed suites;
these are not new managed tests or general ANR/GC/performance/accessibility acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass; no-reset Samsung upgrade and
original Accounts title/Back preserve all141 public Home tuples/979EUR and OS. Remove only the
new helper and two emulator/twelve phone hash-matched XMLs; emulator Main28047 stays unchanged.

Evidence: [original toolbar transition qualification](quality/toolbar-observer-transitions.md).

### D-486 - Incomplete original foreign-transfer forecasts

Reproduce four false one-to-one destination results with eight new Core cases. Missing original
cross-currency destination amounts now keep null monetary effects; known source/same-currency/
explicit destination effects remain known. Two new actual SQLite/workspace cases retain that
warning through atomic currency snapshot Save.49 selected cases pass, including ten new cases.
Six original Release/AOT en/fa/de Light/Dark pages expose EUR90/USD50/incomplete1 on one retained
legacy source with all typed rows unchanged. Strict builds and the complete signed Offline384-AOT
APK pass; Samsung no-reset upgrade and read-only original forecast preserve Home979/all141 public
tuples/OS. Restore the exact old independent APK/stopped state and Main28047/all139 tuples.
No source financial rewrite, schema, resource, permission or unrelated successful cohort replay.
Priorb3fa1a0d CI/CodeQL succeed and its one-shot schedule is consumed/deleted. New-head CI and full
physical/platform/security/stability/accessibility/release acceptance remain separate.

Evidence: [original missing-destination conversion](quality/forecast-missing-destination.md).

### D-487 - Original saved forecast difference categories

Correct the reproduced cash-sign classification across all eight original ledger kinds. Refunds
reduce unplanned spending, income reversals reduce unplanned income, and transfers/capital trades/
corrections use a separate neutral account-movement row. The residual caption now says other
differences because earlier data edits as well as changed plans can contribute. Original scope,
saved paths, actual daily balances, recorded-later boundaries and source rows remain unchanged.
Fourteen new distinct Core cases and two affected regressions pass;23 resource cases pass,39
selected successes overall. Eighteen Windows contexts yield36 inspected images; six x64/412dp
and six physical ARM64/384dp Release/AOT contexts qualify156 original captions and reversal color
with full typed fixture/base equality. Historical timestamps are seeded once per independent
platform; this is not real elapsed-day comparison or financial form/Save acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass. Samsung no-reset upgrade
8764-to13827 retains Home979/all141 public tuples/OS; emulator28047/all139 tuples stays unchanged.
Restore both exact independent APKs/stopped states; remove five hash-matched owned XMLs. Prior
e310eadb has four successful workflows and its single-use schedule is consumed/deleted. Final
new-head CI, real elapsed-day/remaining physical variants, security/stability/accessibility/iOS/
provider/Store and release gates remain open. No unrelated successful cohort is replayed.

Evidence: [original saved comparison qualification](quality/snapshot-difference-kinds.md).

### D-488 - Physical original goal-reminder queue lifecycle

Qualify eleven original ReminderService/native scheduler states on one fresh independent Samsung
installation: default off, enable, pause/resume, complete/archive, derived reached/unreached,
detail consent, disable and generic restoration. Pending counts are0/1/0/1/0/0/0/1/1/0/1; every
queued request keeps the original scoped goal/date/stable id and24October2026 local09:00. Account
opening100EUR and contribution25/Once date remain unchanged, with zero entries/allocations/plans.
The reached case explicitly changes only the fictitious target150-to100 and restores150; no
money moves. Actual native AlarmManager/PendingIntent uses ScheduledAlarmReceiver with the
existing inexact one-hour window, not an immediate display. Eleven observations are not new
managed tests or financial-form/timed-delivery/lock acceptance. Strict complete signed Offline
ARM64 caller192 AOT passes; Main identity is refused. Retain the original live15501 after the
first observation timeout; no successful state/seed repeats. Remove only this new fixture and
its pending alarm/notification; Main13827/Home979/all141 public tuples/APK/OS match. Five owned
XMLs are hash-checked/removed. Main shipping code/APK remain D-487. Real09:00 delivery, doze/reboot,
notification tap through lock, remaining calendar/device/security/stability/accessibility/iOS/
provider/Store and release gates remain open.

Evidence: [physical goal queue qualification](quality/physical-goal-reminder-queue.md).

### D-489 - Physical original period-review reminder queues

Qualify22 original protected ReminderService/native queue states on Samsung in one retained20233:
empty/off and empty/enabled sources, source/off, six states in each Gregorian/Persian/Hijri
calendar, and final restored/off. Financial month starts24/2/13 schedule24October2026 local09:00;
the next two native requests follow their actual calendar, retaining09:00 across the stored DST
offset change. Partial review keeps three pending; finishing only the due period removes that
one and retains two future requests. Details, disable and missed-time cases pass. Compare every
settings field before/after publication; no review step is completed by a reminder. Original
account100EUR stays equal, with zero entries/goals/allocations/plans. These22 observations are
not new managed cases, actual timed delivery or financial-form acceptance. Strict signed Offline
ARM64 caller192 AOT/unchanged five runtime engines passes. Preserve initial caller compile error,
two live observation deadlines and premature cleanup refusal; continue only the same live PID.
The temporary owner-authorized cross-project phone handoff is respected; after a short exclusive
turn, remove only the fresh fixture and five hash-matched XMLs, retain Main13827/Home979/all141
public tuples/APK/OS, and explicitly release the phone for the other active project. Main shipping
code/APK remain D-487. Real09:00 delivery, lock/tap/profile-switch/restore/Doze/reboot, broader
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [physical period queue qualification](quality/physical-period-reminder-queue.md).

### D-490 - Preserve original imported-group dependencies on Undo

Reproduce unsafe Undo after a later fee is added to an imported transfer. Capture external ledger
and holding group context under the original import writer in internal ImportLinks version2;
refuse later changes before mutation, retaining valid Undo for unchanged pre-existing groups.
Ungrouped journals remain version1; legacy closed groups are supported and unverifiable external
groups require review without invented backfill. No EF model/schema/public CSV/permission change.
Ten new and five selected prior managed cases pass; strict Windows/complete signed Offline384-AOT
APK builds have zero warnings/errors. Six original native Undo cases pass on isolated fictional
Samsung sources with full typed-row/base equality. Preserve the first fixed-dock ScrollToAsync
observer failure and its accepted first outcome; resume only five unexecuted commands and read
that first result without replay. Consent is fictional, not native dialog/picker acceptance.
Restore the old independent APK/stopped state, then no-reset Main upgrade retains979/all141 public
Home tuples/OS in PID31885; remove three owned hash-matched XMLs. Prior ecde9366 CI/CodeQL succeed
once and its monitor is consumed/deleted. New-head CI and broader relationship/device/security/
stability/accessibility/iOS/provider/Store/release gates remain open. No unchanged successful
financial cohort or full suite is repeated.

Evidence: [original grouped Undo](quality/import-group-undo-dependencies.md).

### D-491 - Physical original goal calendars and unavailable sources

Twelve original protected/native queue observations qualify saved Gregorian/Persian/Hijri monthly
rules independently of display calendar, two-goal grouped routes, account/type archive/restore,
missed Once and final disabled state. First24October09:00 and second24November Gregorian /
23November Persian/Hijri stay local09:00 across DST offsets. Full original publication packets
remain equal; opening100/targets150EUR and5units stay, with zero ledger/allocations/holding events.
Preserve two actual coalesced-wait observer cancellations and two accepted states; final caller
follows the replaced handle after yielding and continues only ten missing stages in4354.
Strict complete signed Offline ARM64/192-AOT caller passes. Remove only the new fixture with
pending0 and2 Main/3 scoped owned XMLs; original Main31885/Home979/all141 public tuples/APK/OS
remain. Main code/APK stay D-490, no new managed count or successful unrelated suite/build replay.
Real timed/lock/profile/Doze/reboot, security/stability/accessibility/iOS/provider/Store and release
gates remain open. This is native queue/API evidence, not financial-form or whole-device acceptance.

Evidence: [physical calendar/source queues](quality/physical-goal-calendar-sources.md).

### D-492 - Physical original goal and current-review notification taps

Two actual native notification content taps start8272/9387 after confirmed cached deaths of7065/
8272, without provider-created cold processes. Original protected startup/router open the exact
goal100/150/50 and current September review0of5; complete fictional source remains equal and no
financial/contribution/review-step command occurs. Explicit immediate copies retain original
future request ids/payloads; they are not actual09:00 dispatch or credential-configured acceptance.
One strict complete signed Offline ARM64/192-AOT caller passes; Main/App/APK remain D-490.
Preserve early cache/shade/host guards and the cleanup observer's historical-alarm misclassification;
continue accepted state without repeating seed/Show/tap/uninstall. Active pending/blocked alarms
and posted notifications are absent; historical records remain untouched. Remove only the fresh
fixture and2 Main/10 scoped owned XMLs. Original Main31885/Home979/all141 public tuples/APK/OS
stay equal. Prior f21a5d61 CI/CodeQL succeed once and its monitor is consumed/deleted. No new
managed count or unchanged successful suite/build replay. Timed/lock/profile/grouped/Doze/reboot,
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [original content routes](quality/physical-goal-period-notification-routes.md).

### D-493 - Physical grouped goal tap and restored-profile scope

Two native content taps follow confirmed cached deaths12676-to14154-to15038. Original SDK events
retain the same two goal ids/dates and scopeA. The first opens both original GoalsPage rows.
A single original portable export/profile creation/switch/protected restore clones all financial
identities intoB with another file scope; the second actual old-A tap stays onB Home100 despite
both goals existing. Nine original financial arrays compare exactly. Original profile return/
inactive-B retirement restores scopeA/one-profile catalogue. Service restore/cleanup is distinct
from native dialogs/password/credential acceptance. An independent early tap subscription adds no
Application-time database/key work; production routing/security sources remain unchanged.
Final strict signed Offline ARM64/192-AOT caller passes after a preserved initial report-type
compile error. Remove only the fresh fixture and2 Main/8 scoped owned XMLs; pending/posted native
work is absent, historical records untouched, permanent Main31885/Home979/all141 public tuples/
APK/OS equal. Prior7abae129 CI/CodeQL succeed once and monitor is consumed/deleted. Main/APK stay
D-490; no new managed count or successful unrelated build/suite replay. Actual09:00/credentials/
remaining profile-race/security/stability/accessibility/iOS/provider/Store/release gates stay open.

Evidence: [original restored-file scope](quality/physical-goal-profile-notification-scope.md).

### D-495 - Retain pending destinations after a renewed app lock

Two new original-source cases reproduce a queued destination running beneath a renewed cover,
with an available or missing window. One drain owner now rechecks access before each unstarted
action and keeps later input in arrival order. Three new cases and41 existing lock/router cases
pass; the full unrelated suite is not repeated. Two actual Release/AOT Android native-cover cases
with three real button clicks preserve order1/2/3 and complete source/base equality. Authentication
responses are explicit fictional ports, not real credential acceptance. Retire only the new run;
restore the old independent APK/process absence and emulator Main28047/all139 public tuples/OS.
Strict Windows and complete signed Offline384-AOT APK builds pass without warnings/errors. Samsung
upgrades without reset31885-to25043, retaining Home979/all141 public tuples and exact OS; four owned
XMLs across both devices are hash-matched/removed. No financial Save, PIN/key/account/SDK/schema or
permission change. Preserve the initial caller namespace compile refusal and actual timed-fixture
Activity resumption during Main replacement: its interrupted initial D-494 lineage is not accepted
as uninterrupted cold evidence. Same source/run remains equal; current native requests and future
actual delivery are tracked separately. D-493 CI/CodeQL succeed once; its monitor is consumed/deleted.
D-494 timed delivery, real credentials, general security/stability/accessibility/iOS/provider/Store
and release gates remain open. New-head CI follows this completed correction.

Evidence: [original relocked destinations](quality/relocked-pending-destinations.md).

### D-497 - Refresh retained original forecast and saved comparison tabs

Physical D-496 preserves stale0EUR after its single accepted later expense, versus correct-10
on original Back/reopen. AppShell now includes both retained ForecastPage and SnapshotPage in
covered tab activation; appearance/navigation share PendingPublication. Four actual native tab
clicks qualify the same saved page publishing-10 and same forecast publishing97 while retaining
its what-if plan exclusion. The immutable original snapshot and complete original base match;
only the new independent protected profile is retired. Fictitious service writes are not native
financial-form or D-496 acceptance. No new managed cases or unrelated successful-suite replay.
Preserve the initial caller compile errors and live launch-observation guard; both cases finish
in11339. Restore exact old caller APK/process absence and emulator Main28047/all139 public tuples/
OS; two owned XMLs are hash-matched/removed. Strict Windows/Offline384-AOT shipping builds pass.
Samsung remains D-495 to preserve the pending timed cohort; no-reset phone adoption remains a
named integration action. D-494/D-496, phone/platform/security/stability/accessibility/provider/
Store and release gates stay open. Prior f6ad8543 CI/CodeQL succeed once; its monitor is consumed/
deleted. Exact new-head CI follows push.

Evidence: [original retained-tab qualification](quality/forecast-retained-tab-return.md).

### D-498 - Physical original renewed-cover destinations

Two previously emulator-qualified D-495 cases now pass on Samsung SM-A176B/API36/ARM64 with
actual original LockPage/MauiAppLockHost controls and three native unlock clicks. A renewed
cover retains later destinations; both cases finish in order1/2/3. Authentication responses are
explicit fictional ports: no real device credentials, PIN storage or financial writes. Complete
fixture/base packets remain equal. One strict signed Offline192-AOT independent build succeeds;
no unchanged Windows/shipping build, managed suite or financial cohort is repeated. Restore the
exact old independent APK/stopped state and Main25043/Home979/all141 public tuples/APK/OS.
Preserve the tuple-versus-list restoration-observer refusal: both original XML arrays already
match, and only local evidence comparison continues. Two owned remote XMLs are hash-matched/
removed; local originals remain. D-494 keeps24914 and its four selected resumed alarm identities;
no provider read, rearm, subject replacement or Main upgrade. D-496 remains inactive/intact.
This is physical native-cover adapter qualification, not real authentication, notification-tap,
Doze/reboot, general security/ANR/crash, accessibility, iOS, provider/store or release acceptance.
Prior3821870a CI39981472630/CodeQL39981472583 succeed once; that monitor is consumed/deleted.

Evidence: [physical native-cover scope](quality/physical-relocked-destinations.md).

### D-499 - Reversible original CSV text

Own ledger/holdings CSV contract3 preserves original apostrophes, whitespace and multiline human
text while neutralizing spreadsheet formula prefixes. Match the unique named version marker;
refuse duplicate/unknown contracts. Legacy decoding remains, with explicit refusal when literal
and decoded account names resolve to different existing accounts. Generic mapped titles/notes
remain literal; category padding trims without removing apostrophes. No financial algorithm,
schema, backup/journal version, permission, SDK or network change. New format3 requires a
compatible importer; do not guess lost legacy text or claim old-client compatibility.

Forty distinct new cases (34 Core/six original App-source real SQLite) and20 existing related
cases pass in selected runs; only affected cases repeat after fixes. Preserve11 reproduced
failures and intermediate compiler/analyser refusals. Windows/fa-Dark and Samsung/en-Light each
accept own-ledger and holdings once, then reuse the accepted source read-only for the shifted
named marker and accept only the previously unexecuted generic case. Ten Windows captures are
reviewed; actual native commands and complete source/base equality pass. Picker/consent ports
are explicit fictitious ports. Preserve the early physical fresh-root refusal and repair only
continuation authority against its original accepted receipt; no accepted financial replay.

Final strict Windows and complete signed Offline384-AOT APK builds have zero warnings/errors;
408 libraries pass static16KiB alignment, not runtime acceptance. The owner closes Play Protect
for the same waiting restore request; no bypass/system input is performed. Restore retained
independent APK/stopped state, Main25043/Home979/141 public tuples/APK/OS; remove four owned
hash-matched remote XMLs. Continue only the transient focus-refused restoration observation.
Main stays D-495 for D-494/D-496; subsequent no-reset adoption remains explicit work. D-494's
24914/four resumed alarm identities remain unchanged. iOS, minimum-API/16KiB runtime, stability,
authentication, spoken accessibility, real bank/corpus, providers/Store/release gates remain open.
Prior265d1460 CI39988941106/CodeQL39988940793 succeed once; monitor consumed/deleted.

Evidence: [bounded original CSV qualification](quality/csv-literal-identity.md).

### D-500 - Shared original Home balance index

Reuse one operation-local AccountEntryIndex for account rows and ordinary/confirmed total
balances. Existing callers delegate to the same original total implementation; balance effects,
scope/order/date/review/currency and checked-overflow semantics remain unchanged. No cross-read
cache, database/schema/permission/SDK or financial-writer change. Isolated actual-Core composition
allocation falls66.26%/66.66% on10k/100k entries, not process memory or whole-Home speed acceptance.
Twelve new and29 affected prior managed cases pass selectively; output is cleaned. Retained
Windows sources pass six native contexts/twelve publications with six reviewed app-only images.
Samsung uses same original protected sources: preserve the cross-date historical-presentation
refusal, then compare current-day original D-499 to D-500 in six contexts/twelve publications each.
Complete financial/native presentation and full source schema/typed rows match, without seed or
financial replay. Candidate reference median1712.36ms/max2059.99 and tenfold4953.51/max5590.87
leave the two-second/general stability objectives open; do not infer stable speedup from medians.
Strict Windows/signed Offline384-AOT APK and192-AOT caller builds pass; static408-library16KiB
checks are not runtime acceptance. Restore exact retained caller/stopped state and Main25043/
Home979/141 public tuples/APK/OS; remove two owned hash-matched XMLs. Preserve D-494 resumed24914/
four requests and D-496 inactive source. No-reset Main integration awaits timed-cohort completion.
D-499 single CI response remains in_progress; three other workflows succeed, monitor consumed/
deleted and no request repeated. Security/spoken/iOS/provider/release gates stay open.

Evidence: [original shared-index qualification](quality/home-shared-balance-index.md).

### D-501 - Fixed public receipt-corpus observation

Use the first ten official CORD v2 test images under CC-BY-4.0, without outcome selection.
Original public images/annotations stay outside the repository/product; licence/attribution
accompany only the independent caller. Ten actual Samsung original-reader/parser observations
produce five correct Found amounts, three Review and two NotFound, including one true no-total
negative. Perfect annotated-text parsing gives eight Found/one Review/one NotFound. Preserve
blurred/damaged/count-label ambiguities; no fabricated digit correction or product algorithm
change. Five of nine annotated totals found is insufficient to close QA-05. Latin-script public
Indonesian receipts do not qualify six languages, Persian image OCR, native camera acquisition,
scanned PDF, merchant/currency accuracy or saved financial forms. No new managed test count.
Strict signed Offline192-AOT independent build passes; no new SDK/permission/shipping source or
financial write. Restore exact old caller/data/stopped state and Main25043/Home979/141 public
nodes/APK/OS; remove two owned hash-matched XMLs. D-494 resumed24914/four requests and D-496
remain intact. No Main private database/key equality, stability/spoken/iOS/provider/release claim.
Prior4de95cfb CI40004246720/CodeQL40004246807/service40004246802/identity40004246884 succeed
once; monitor consumed/deleted. Continue remaining quality gates without repeating these images.

Evidence: [finite public corpus and limitations](quality/public-receipt-corpus.md).

### D-502 - Retire completed Docker fixtures and retain one desktop group

Owner decision 2026-10-23: clean unnecessary owned resources continuously and group necessary
containers for whole-stack management. Preserve the existing local privacy-restore identity and
its five Compose containers; retire 58 completed fixture containers, 9 unneeded images and 16 empty
networks. Preserve all private volumes/snapshots and available image dependencies; no global shared
engine cleanup. Add bounded ownership-checked status/start/stop commands without rebuilding or
replaying provision/migration. Actual stop/start and original readiness are documented in
`quality/docker-lifecycle.md`. No app/APK, unit-test count, public hosting or release gate changes.

### D-503 - Original image-only PDF reading on Samsung

Nine fixed synthetic image-only PDFs/15 raster pages exercise the unchanged native PdfRenderer,
DocumentReader and ReceiptParser once: five Latin-language24.50EUR totals and a six-page last42.75EUR
pass; cross-page EUR/USD remains Review; no-total and Persian scan remain NotFound. The latter is
an unaccepted script capability, not a six-language OCR pass. Original first3/last selection skips
an intentionally conflicting middle-page total. No financial Save, new managed test, shipping source,
SDK/model/permission or product APK change. One strict signed Offline192-AOT independent build and
exact asset hashes pass; restore original caller/data/stopped state and Main25043/Home979/all141
public tuples/APK/OS. Remove two owned XMLs. Four original D-494 handles and24914 remain; no provider
read/rearm/window/subject replacement. QA-05 broader corpus/camera/Persian/iOS gates stay open.
D-501 CI40007631698 and CodeQL40007631709 succeed once; its consumed monitor is deleted.
[Evidence](quality/scanned-receipt-pdfs.md).

### D-504 - Optional Android receipt-reading capability help

The original camera/file chooser now offers optional six-language reading help. Explain the approved
Latin image/scanned-PDF capability, unsupported Persian image text, selectable Persian PDF text and
explicit amount/currency review. Ordinary photo/file selection adds no confirmation; help returns to
the chooser and Cancel opens no source. Fifteen new original-source/resource cases pass; strict
Windows and complete signed Offline384-AOT shipping builds pass without warnings/errors. Original
native chooser/interaction in a new isolated UID qualifies24 contexts: en/fa/de, Light/Dark and actual
360/384/412/680dp dialogs,72 native help/OK/Cancel clicks. Retain the first successful case after a
focus refusal, then all18 completed cases while adding only six missing exact412dp dialogs; correct
an observer resume-file mistake without replaying source reads or financial commands. All text is
fully visible/unellipsized, with actual differing native theme colours. Restore original Main25043/
Home979/all141 public tuples/APK/OS and old caller/stopped state; uninstall the new caller and remove
seven owned XMLs. A retained landscape layout is returned through only the disposable caller's
portrait window; no system setting or source-help cohort is replayed. Four D-494 handles/PID24914
and D-496 remain intact. Main remains D-495 until D-494 terminal cleanup; the latest complete ready
APK is D-504 and includes D-497/D-499/D-500. Persian OCR, OS scaling/spoken, general stability,
iOS/provider/Store/publication and future-day acceptance remain open. No new SDK/model/schema/
permission/commercial restriction. [Evidence](quality/android-receipt-source-help.md).

## D-505 - Distinguish Docker cleanup from shared disk compaction (2026-10-24)

Owner requests removal of unnecessary physical files and approves a temporary shared Docker
outage with restoration of the original services. Retire two completed labelled/unmounted
fictional volumes; preserve original container/status/image sets, recovery sources/snapshots and
unrelated resources. Exact-ID cleanup of ten project cache records reclaims0B because retained
images share their layers; do not claim323.4GB of shared cache belongs to this project. Measure
the configured643440836608byte VHDX separately. Current non-admin session cannot compact it;
prepare a path-checked elevated maintenance script with graceful shutdown, disk access check,
finally restoration and before/after identity/size receipts. Syntax verification is not elevated
runtime/health/space-recovery acceptance. No app/test/APK change or successful cohort replay.
[Evidence](quality/docker-lifecycle.md#d-505---file-allocation-follow-up-2026-10-24).

## D-506 - Actual elapsed-day forecast comparison (2026-10-24; completes D-496)

Continue only the original23October Samsung D496_ElapsedForecast source on the actual24October
clock, without replacing D-495 or touching D-494. Saved D496_NextDay stays23-31October with
original next-day80EUR. Before new payment, day24 saved80/recorded90/difference+10 separates
recorded-later-10 and other differences+20. Pay only the original D496_Planned20 once on24October:
recorded70/difference-10, recorded-later-10 and residual0. Add only one original D496_Extra5 expense
and one D496_Extra7 income on24October: recorded72/saved80/difference-8; parts-10/-5/+7/0 sum to-8.
Original account/date/title/amount and individual pre-command/accepted receipts are retained.

Reopen the same profile after returning Main:72 and original snapshot80/difference-8 persist.
This is profile/database reopening, not a cold-process claim. Preserve the four ledger identities,
original saved metadata and all prior successful preparation; no snapshot resave or financial
replay. D-495 requires fresh Back/reopen for retained snapshot publication; D-497 phone adoption
remains open because D-494 is unfinished. No candidate upgrade is performed.

Return Main before deleting only the inactive owned profile through both original confirmations.
Verify the exact original three profile names, Main25043/Home979/all141 public tuples/APK/OS;
remove137 new owned remote XMLs only after local/remote SHA256 equality. Keep local originals.
D-494 package/process/future requests are untouched. These observations are not new managed tests;
no build, old test suite, authentication or unrelated accepted cohort is repeated.

Retain observation failures: a Home quick-action opened an empty expense draft and was cancelled
without input/Save; two early hierarchy captures were unavailable and continued without restart;
account identity is on the original due-row witness rather than the occurrence editor; category
chips do not expose a selected-name suffix; a retained transaction detail needs Back; the second
profile-delete confirmation is generic and is bound to the first named confirmation. All failed
guards preceded their intended input. Accepted commands are never replayed to repair observers.
This is not whole-device/security/ANR/TalkBack/iOS/provider/Store or release acceptance.

Evidence: [actual elapsed-day comparison](quality/physical-elapsed-forecast-comparison.md).

### D-505 follow-up - observed shared physical reclamation (2026-10-24)

A later external state change reduces the configured VHDX from643440836608 to45837451264bytes;
shared cache accounting is now0B. Execution/actor is unverified and no elevated-script receipt is
present, so do not attribute the operation or all shared cache to this project. Verify all31 IDs,
original20-running set,190 retained volumes and all nine previously available protected image
dependencies unchanged. Retained database/API are healthy, proxy running; historical missing
seven dependencies remain missing. No compaction/restart/test is replayed just to reproduce a
successful external result. [Evidence](quality/docker-lifecycle.md#d-505-later-physical-state-verification-24october0938utc).

## D-507 - Same-day Android reminder delivery after actual D-494 failure (2026-10-24)

Owner accepts delayed Android delivery through the last instant of the originally scheduled local
calendar day, rejects next-day delivery and keeps inexact scheduling without new permission types.
Original D-49409:00 requests were dispatched by AlarmManager only at10:36:59.194/.200, beyond the
old one-hour allowance. The exact SDK binary rejects those original timestamps under that old
policy. The original timed criterion and interrupted cold lineage remain failed, not reclassified
as a pass. The precise OS/OEM reason for deferral is unproven.

ReminderDeliveryWindow computes the exclusive next-day boundary using the device zone, including
DST, invalid midnight and the earlier ambiguous midnight; the SDK's inclusive endpoint ends one
tick earlier. Original LocalNotificationScheduler.Request uses that allowance. Existing source/
profile/privacy/permission/cancellation guards, generic text, scheduling mode and missed-date
preparation remain unchanged. No exact-alarm permission, clock/configuration change, new SDK,
schema or financial write. Nine new distinct source cases pass in8+1 selected runs; only the
new ambiguous-boundary case runs after the first eight successes. Five separate actual-SDK
checks accept the two old late timestamps/last-day tick and refuse midnight/next morning.

A new notification-only signed Offline ARM64 package compiles the original request builder,
publication guard/window and SDK, without a database or financial fixture. Its one actual native
permission dialog applies only to that new UID. Two deliberately late09:00 requests on the real
24October clock enter AlarmManager at12:48:24, its original receiver dispatches at12:48:29.713/.724,
and native notification records show both unchanged generic messages at12:48:29.813/.856. The
original scheduler refuses yesterday's request before native publication. This is controlled
same-day late delivery, not a future09:00 punctuality, Doze, reboot, cold or general-device claim.
The native package has98 AOT modules and v2/v3 signatures; no INTERNET or exact-alarm permission.

Read the original D-494 protected source only after preserving actual dispatch: same24914/source
match true and original run. Retire both finished fixture packages after evidence, verify no
pending/blocked/posted work, and retain historical alarm records. Never replay successful account,
plan, profile, notification-content-tap or financial setup to repair observations.

Strict Windows and complete signed dual-architecture Offline APK builds pass without warnings.
Shipping APK has384 AOT modules,408 ELF libraries with1608 load segments meeting static16KiB
alignment and successful official ZIP alignment verification. This is not API24/16KiB runtime
acceptance. The final APK hash is984f01095439a6ca3ff979755085d6fd5af6208256dc2345b7924ba7a031d9a6.

Before this operation Main's earlier25043 was already absent; ordinary launch reaches27501 with
unchanged D-495 APK/Home979/141 public tuples. After fixture retirement, one complete signed
no-reset Main upgrade adopts D-507 (including D-497/D-499/D-500/D-504), yielding30005 and exactly
the same public Home/three original profile names/OS. This adoption is not an additional phone
acceptance of the deleted D-496 retained-tab scenario. Remove12 new owned XMLs after hash equality;
retain local originals. Release the coordinated Samsung turn. No financial commands occur.

Retain three observer issues without replaying native publication: notification record line
endings, an unbounded delivery-history section and an incorrect demand that compressed native
ZIP entries have mapping alignment. Official zipalign plus ELF checks resolve the static observer
issue on the same unchanged APK. Native receiver/notification receipts come from the first actual
accepted publication. General stability/accessibility/security/iOS/provider/Store/release gates
remain separate. [Evidence](quality/physical-timed-goal-period-reminders.md).

## D-508 - Physical retained forecast-tab acceptance (2026-10-24)

Close only D-497's missing physical adapter boundary, using a fresh independent ARM64 package
`pro.vafadar.zanance.forecastreturnreview` and the unchanged original forecast-return cohort.
Do not replace the retained Home caller or replay the deleted D-496 financial profile. The new
package links current production sources and creates only its own fictitious protected source:
opening100EUR, one Once20 plan, one immutable snapshot and two distinct service writes, expense10
then income7. These are isolated fixture writes, not Main or financial-form acceptance.

On Samsung SM-A176B/API36/ARM64/4096-byte pages, four actual native bottom-tab PerformClick
activations retain the same two page objects. Snapshot detail changes from0 to-10EUR without
Back/reopen/direct post-change LoadAsync. Forecast retains the original plan-exclusion draft and
publishes97EUR after the single later income. Original snapshot JSON and complete base packet
remain identical; the owned profile is deleted before the result is emitted. Native navigation
records actual ShellSectionChanged events to the original SnapshotPage/ForecastPage. Both cases
complete once in PID6294/run f8f3f094e6244432baf884dd8a0bdd74; no accepted action is replayed.

One complete strict Offline ARM64 caller build passes without warnings/errors:192 AOT modules,
v2/v3 signatures, no INTERNET/debuggable declaration,56,656,390 bytes, SHA256
`25a6c09f1c8d78941ef2c1656f2466310b84ee33b3e5f7b1de38a9e7d6698eb1`.
No production code, layout, permission, schema or monetary formula changes. Successful D-507
Windows/shipping builds, old managed suites and prior financial cohorts are not repeated; these
two native cases are not new managed tests. Independent COLD/Activity1636ms/Wait1680ms is an
activity observation, not whole-Home timing or general stability acceptance.

Preserve initial host observation failures: Main pidof reports no process before launch; an early
native hierarchy helper exits137; a subsequent guard finds no Home target because the original
startup already presents Home. No input or financial write follows those refused guards. The
first restoration reader uses the Windows default text encoding and refuses UTF-8 tuple data;
correct only its explicit encoding, then compare the existing captures without UI/action replay.

After terminal receipts uninstall only the new package. Original Main APK remains D-507/hash
984f01095439a6ca3ff979755085d6fd5af6208256dc2345b7924ba7a031d9a6 without install/reset;
Home979/all141 original public tuples and exact OS settings match, MainPID5443. Remove five new
owned remote XMLs only after local/remote hash equality and retain local evidence. Release the
coordinated phone turn. Main private database/keys, profiles and financial writers are untouched.
General reminder/Doze/reboot, security/stability/performance/accessibility, API24/16KiB runtime,
iOS/provider/Store and publication remain open. D-496/D-506 actual elapsed-day success and D-494's
original failed time window stay unchanged. Continue genuinely uncovered backlog work.

Clean the owned native build tree with dotnet clean (zero warnings/errors). Remove only the new
unsigned APK/resource-designer copies (57,244,769 bytes); retain its signed acceptance APK and
local evidence. Previously rejected D-507 host-file removals are not retried.

## D-509 - Home financial-source publication revision

## Reproduced defect

The original complete read and final validation guard plans/goals, file authority, accounts,
categories, settings and quick-resource definitions. They do not compare later ledger, budget,
rate or holding writes. An actual application-flow regression captures an empty Home ledger,
commits one original expense, and observes ValidateHomeSnapshotAsync accepting the stale packet.
Retain that failing result (expected HomeSnapshotSupersededException; none thrown).

## Atomic local correction

Migration20261024115108 adds HomeFinancialRevision, one local SQLite bookkeeping row with an
opaque random32-hex revision. Eighteen AFTER INSERT/UPDATE/DELETE triggers cover Entries, Budgets,
BudgetCategoryLimits, ExchangeRates, AssetTypes and AssetEvents. The marker changes in the same
transaction as each financial mutation, including raw SQL/bulk writers, and rolls back with it.
It contains no amount, user identity, key, account, timestamp or external tracking identifier.

Read the revision first under the original complete-read transaction; all later rows belong to
that same SQLite snapshot. The original final validation reads its current revision after its
strict actual-file/access/unchanged-definition-input checks. A changed financial source raises
the existing classified supersession exception; HomeRefreshSession rereads under the original
cover with its existing finite retry policy. Never post money, change formulas or bypass rights
to refresh Home. This avoids a second complete financial-table materialization. It is not a
measured speedup or acceptance of the two-second/ANR target.

This is migration-owned SQLite bookkeeping, not a new EF financial entity. The generated migration
target model equals the preceding financial model; no compiled-model change is needed. Future
SQLite rebuilds of these six tables must preserve/recreate the18 revision triggers explicitly.
Logical financial exports/sync do not gain a new record or portable preference. Whole-database
backups naturally carry this internal row/table under their existing protection. No new network,
SDK, permission, public endpoint, key policy or financial field is introduced.

## Focused verification

Twenty-one new distinct actual-source cases pass in selected continuations: the original committed
expense regression,18 insert/update/delete cases across the six tables, transaction rollback and
a covered reread that executes its accepted financial writer exactly once. Preserve two fixture
errors: initial raw-table preparation omits a required holding location; later deleting a type
still referenced by a holding correctly refuses. Correct only those unaccepted fixtures; retain
17 successful trigger cases and rerun only the final unreferenced-type deletion case. Two earlier
rollback/covered-reread successes are also retained without replay.

Seventeen original Home refresh/source-boundary cases,96 original complete Home-read cases, the
first-schema account/entry upgrade and the existing upgrade/portable-restore/no-paid-rights case
all pass. These115 related cases verify the affected validation/migration boundary; no complete
managed suite or unrelated successful financial cohort is rerun. No whole-suite count is claimed.

Strict Windows and complete signed dual-architecture Offline Android builds have zero warnings
and errors. Shipping APK is96,997,279bytes,384 AOT modules,408 ELF libraries/1608 load segments;
v2/v3, official ZIP16KiB alignment and ELF16KiB static checks pass. Offline privacy guard passes.
SHA256: fd4291f06c8dd7808e3832e6d0544e115644ad1369eefe0892aa2e1d904f2998.
This is not minimum-API or16KiB runtime acceptance.

## Native protected source, retained failure and continuation

A separately identified complete ARM64192-AOT Offline caller owns only its fresh protected
financial profile. Compile the original Home page/view model/data adapter and use its actual
QuickAddTargetText change after the original source read, while Loading is true/IsReady false.
The independent host alone pauses that notification for one original expense writer. Opening100
and the sole accepted expense10 belong only to this fictitious source; no Main writer or native
financial-form acceptance is claimed. No production timing hook is added.

Original PID10072/run1bf0b21bf3f7432d85fceae7226fc87b commits its expense once. The terminal
exact-publication-count/pending-task guard fails and remains a real failed qualifier. A capture
from that same live process shows the original native Home90.00EUR and correct original account.
The failure did not persist each predicate's value, so its exact predicate and publication count
are unaccepted; do not assert an uninterrupted one-publication pass or replace it with unit tests.
The future observer waits for both ready state and completion and persists all counters, but that
fresh financial cohort is not replayed to repair this observation.

A complete signed no-reset update of only this new caller continues the same protected source
under explicit home-financial-resume. It requires the original failure/accepted-writer receipts,
owned profile/current identity and unique original base. It verifies opening100, exactly one
original expense10, amount/title/date/account and current balance90 with the actual original
Home read/validation. It has no preparation/account/entry writer. PID11146 reports one retained
accepted writer, zero replays and read-only completion, then returns to the unique base and deletes
only the owned profile. Original native publication-count acceptance remains open; the existing
unit race and final native amount/source checks have their narrower scopes. Preserve initial
missing MoneyText namespace and profile-id type compile errors, both before their respective
native invocation. No previous financial action is reseeded or replayed.

Retain original caller7ac758c0f6010c6826dbd14390ea04bb62bac037d06fe4045150c862aaddf7e9
and continuation d672a358266bc45bc39249f27f19ca300aa95cf72eac797eb8b73286c9d65a96.
The latter is56,242,524bytes/192AOT/v2/v3. Uninstall only this new identity after terminal source
retirement; keep the old retained independent Home package/data untouched.

## Main adoption and cleanup

One complete signed no-reset Main upgrade adopts D-509 after fixture retirement. Its actual
original public three-profile names/current selection match before/after; final Home979 and all141
public tuples equal the captured D-507 baseline. APK hash matches the verified new candidate,
MainPID12153, and font1.0/size/density/accessibility/services match exactly. Do not infer complete
private database/key equality from public UI evidence. An early copied observer's null PID was
unread, not process absence; a separate pre-upgrade live-process witness is retained.

Remove15 new owned remote XMLs only after local/remote SHA256 equality; retain local originals,
failed native report and accepted source evidence. Clean both focused test output trees and the
owned native/shipping/Windows build trees with dotnet clean: zero warnings/errors. Remove only
new unsigned APK/resource-designer copies (154,437,238bytes), retaining signed evidence packages
and the original failed caller archive. Previously rejected D-507 file removals are not retried.
Release the coordinated Samsung turn. Existing accounts, Main financial data, system settings and
other projects remain untouched. General stability/performance/accessibility/security/platform,
provider/company/Store/expert and release gates remain open. Continue genuinely uncovered work.

## D-510 - Physical monthly calendars and short-month anchors

## Bounded original-form scope

Close only the uncovered monthly calendar/count/short-month boundary of AT-72 on the owner's
Samsung SM-A176B. The unchanged complete signed D-509 Offline Main APK remains installed:
fd4291f06c8dd7808e3832e6d0544e115644ad1369eefe0892aa2e1d904f2998, PID12153.
No build, successful managed suite, existing financial posting, accepted plan Save, upgrade,
force-stop, device clock, display setting, permission, account sign-in or other package is replayed.

Ordinary Profiles creates D510_RecurrenceCalendars exactly once and opens the original setup.
Keep English/Light/Simple; create one original Checking/D510_ZeroCash/EUR with explicit known
opening0 on the actual24October2026 date. That opening creates no ledger transaction. Three new
original plan forms each receive fixed1.00EUR on that account, Monthly, specific starting day,
last-valid-day policy, end after3 occurrences, Reminder off and Record automatically off. No
occurrence/payment/financial transaction is submitted. Each distinct definition saves exactly once.

## Independent expectation and native previews

Before the accepted definitions, .NET GregorianCalendar/PersianCalendar/UmAlQuraCalendar on the
host supply the expected date conversions; the device clock is never replaced. Original native
preview labels are fully within the app's actual scroll viewport, with the named rule calendar,
first due date, short-month explanation and occurrence count retained.

| Original definition | Rule-calendar preview | Equivalent Gregorian dates |
| --- | --- | --- |
| D510_Gregorian31 |31October2026;30November2026;31December2026 |2026-10-31;2026-11-30;2026-12-31 |
| D510_Persian31 |31Shahrivar1406;30Mehr1406;30Aban1406 |2027-09-22;2027-10-22;2027-11-21 |
| D510_Hijri30 |30Jumada al-Ula1448;29Jumada al-Akhirah1448;30Rajab1448 |2026-11-10;2026-12-09;2027-01-08 |

The Gregorian native weekdays are Saturday/Monday/Thursday; Persian Wednesday/Friday/Sunday;
Hijri Tuesday/Wednesday/Friday, matching those independent dates. The original explanations
retain the starting31/30 anchor after a shorter month rather than drifting subsequent dates.
Switching the rule calendar changes its own date-field order/value/summary, not the app's global
Gregorian display or device settings. Stored original All plans rows retain their unique names,
1.00EUR, named calendar where distinct, occurrences3 and correctly converted next date.
These nine dates/three scenarios are native observations, not nine new managed tests.

## Persistence without financial replay

Original Transactions explicitly selects All dates/All types and shows its original empty-state
sentence. Home stays0 with D510_ZeroCash. Return through the ordinary profile menu to original
Main979, reopen only the same owned profile and reread All plans. All three stored row descriptions
match exactly, including amount/calendar/count/next date, and its Home remains0. This is actual
profile/database reopening, not a cold-process claim. No setup, definition Save, accepted input,
source-calendar scenario or financial command is repeated to repair observations.

## Preserved observer refusals

The More tab initially reveals an already retained Profiles page; an attempted Profiles target
is absent and the guard refuses before input. A host label containing percent20 also refuses
before input; use the actual ordinary Add profile caption. Early captures during original async
profile/setup/delete operations show the old tree or an empty frame; continue the same operation
without restart or repeated confirmation/Save. After the first accepted deletion, a later fresh
profile read confirms actual retirement rather than treating the early old list as completion.

Two occurrence-count entry attempts fail their empty-field guard while the original value stays12.
No new text is appended and no Save occurs after those refusals. Hide the observed IME, refocus
the same original field in the visible viewport and continue only its unaccepted edit to3. Keep
both failed/corrected observations; no product or hardware root cause is inferred. A terminal
console initially renders a separator as a replacement glyph; the original UTF-8 native XML has
U+00B7 and zero U+FFFD. It is a host output-encoding effect, not a product separator defect.

## Retirement and retained baseline

After accepted profile reopening, return Main and delete only inactive D510_RecurrenceCalendars
through both original confirmations. The first explicitly names its accounts/entries/plans/settings;
the second generic backup-retirement warning is bound to that named first confirmation. No other
profile or backup is targeted. Fresh Profiles shows the same three original names/current Main:
Main profile, D316_Fictitious_Work and D319_Fictitious_Loan. Original Main979/all141 public tuples,
PID12153/APK/font1.0/size/density/accessibility/services match exactly. No private Main database
or key is read; public UI preservation is not complete private-store equality.

Verify local/remote SHA256 for all154 new owned remote XMLs, remove only those files and retain
local evidence. These154 files are not unit tests. Release the coordinated Samsung turn explicitly;
perform no further device input until a fresh handoff. No unused caller/package/build tree is
created in this device-only batch. Source/APK/managed-test count remain D-509; no unchanged
successful build or whole business suite is repeated.

The scope does not accept skip-missing-day policy, date-based ending, every custom/second-day/
weekend/holiday rule, future actual payments/delivery, cold/low-memory/ANR/performance, scaling/
spoken accessibility, iOS, provider/company/Store/expert or publication. Preserve all previous
failures and continue genuinely uncovered independent work rather than replaying these cases.

## D-511 - Preserve complete financial snapshots after the Home revision migration

Hosted D-509 CI has1331 failed/3875 successful/25 skipped cases; the shared test inventories
reject its known migration-owned HomeFinancialRevision table. Explicitly include that table
while retaining unknown-table refusal and exact complete no-write/rollback/repeated-action
comparisons. Only accepted financial round trips or existing intentionally omitted financial
writes allow a validated opaque-token difference. All financial rows/links/receipts stay compared.
Two strict test-project builds have zero warnings/errors; retain1275 initial successes, repair
59 newly reachable token expectations and pass69 focused continuation cases, including three
new real-SQLite oracle guards. All1337 distinct selected cases now have successful receipts;
this is not a whole-suite rerun. Production code/migration/APK and phone data remain D-509.
A new exact-head hosted result remains pending at commit creation. Security/stability/performance/
accessibility/iOS/provider/Store/release acceptance stays separate. Evidence: [CI snapshot oracles](quality/home-revision-ci-oracles.md).

## D-512 - Physical Skip and original-calendar ending

Two new original fixed1EUR monthly definitions on Samsung qualify missing-day Skip and date-based
ending: Gregorian31October/31December/31January with inclusive31January2027, and Persian31Shahrivar
only through30Aban1406. Independent platform calendar lengths agree with all four visible preview
rows. Each definition is saved once; Reminder/automatic recording are off and All dates/All types
history is empty. Ordinary Main/profile reopening retains both original stored-row descriptions
and Home0 without financial replay. Delete only the inactive fresh profile with both confirmations;
original Main12153/Home979/all141 public tuples/three profiles/OS/D-509 APK match. Remove158 fresh
hash-matched owned XMLs and release Samsung. Preserve early-frame/host Back/nonowned-tree refusals;
no system input or settings change is used to recover them. No new managed count/build/successful
cohort is repeated. D-511 exact-head CI/CodeQL succeed and its one-shot monitor is consumed/deleted;
this new documentation head awaits its own check. Evidence: [physical Skip/calendar ending](quality/physical-skip-calendar-ending.md).
Custom/second-day/holiday/future delivery/runtime/accessibility/iOS/provider/Store/release gates remain.

## D-513 - Original budget-source publication on Samsung

One new source-flow SQLite case passes: the captured10EUR budget changes once to20EUR under
cover, the first packet is retired and the second read publishes once without a ledger entry.
Only this new case runs; strict App.Tests and independent ARM64 Offline builds have zero
warnings/errors. The signed192-AOT caller owns a fresh package and refuses historical phases.
Fifteen synthetic host observer checks are separate from the one new application test.
After explicit phone handoff, actual original Home/store process26285 confirms covered-at-write,
one accepted budget change, one completed publication, ready/nonfailed state and the original
visible20EUR caption at895x51, empty ledger and complete original independent-base equality.
Delete only its new profile and package. Original Main12153/Home979/all141 public tuples/three
profile descriptions/current selection/APK/OS match;13 new owned XMLs are hash-matched and removed.
The initially retained blank expense closes through original Cancel without a dirty prompt or
financial Save/Discard. Preserve initial baseline/host label refusals and pre-device compile/
package-observer failures. No accepted writer, successful suite or financial cohort is replayed.
D-509's old failed expense publication count stays unaccepted; this is a distinct budget cohort,
not financial-form or general stability/performance/accessibility/security/platform acceptance.
Production code and shipping APK remain D-509. Prior D-511/D-512/D-514 CI/CodeQL succeed and their
one-shot monitors are consumed/deleted; this batch awaits its own exact-head CI at commit creation.
Evidence: [budget publication](quality/home-budget-publication.md). Continue genuinely uncovered work.

## D-514 - Continue ready host work while the phone is unavailable

The owner explicitly requires continuing other ready approved tasks whenever phone access is
unavailable, then resuming the preserved unfinished device work when access returns. Do not wait
idly or substitute repeated successful/unrelated checks or financial commands. Keep accepted
operation identities/checkpoints and all existing foreground, scope, authentication, system/data
and external-decision boundaries. Phone unavailability does not complete the overall delivery.
The rule is recorded in AGENTS.md. This is an execution-order decision, not a product/runtime
acceptance claim; it needs no repeated application builds, business suite or device actions.


## D-515 - Original recurrence variants and truthful optional-holiday captions

Four original fixed1EUR definitions qualify custom every3 days/count4, monthly day1 plus day15/count4,
and Germany nationwide holiday-forward/backward/count2. All12 native preview rows match independent
dates/weekdays. Each original Save occurs once, Reminder/automatic recording remain off and All dates/
All types history is empty. Ordinary Main/profile reopening retains all four exact stored-row strings
and Home0. Preserve early-frame, USB/WiFi, disclosure-state and host quoting/foreground refusals;
continue the same source rather than replaying preparation, accepted definitions or money.

The actual enabled-holiday preview contradicts old en/es/fr/it Weekend_Hint text. Correct only those
four values to match the existing optional-holiday policy; fa/de resource files are unchanged in this
batch. Four new actual-source SQLite cases fail against old text and pass after the repair. Thirty-six
original Windows caption contexts cover six languages, both themes and actual360/412/1280 widths,
with unchanged complete fixture rows, zero financial Saves and process4732 exit0. Twelve original
Samsung caption/Cancel contexts cover the same six languages and both themes on the preserved
holiday plan, matching full source strings, positive visible bounds and the checked holiday control.
No existing successful financial cohort or unrelated suite is repeated.

Strict Windows and canonical complete Offline Android builds have zero warnings/errors. The signed
two-ABI candidate has384 AOT/408 ELF, v2/v3, ZIP CRC/privacy and static ZIP/ELF16KiB proofs; these do
not establish16KiB runtime acceptance. No-reset adoption changes the APK SHA256 legitimately to
1f09c70fd8d27a7877c91b1a958ebf530caf6136fcdaac0a961c693d72ea4788 and Main PID to31197. Original
Home979/all141 public tuples, the three prior profiles, English/Light/Simple and recorded OS remain.
Delete only the inactive fresh profile with both original confirmations. D-517's owner-authorized
300000-to1800000ms temporary timeout is restored exactly to300000ms and verified. Remove465 unique
owned remote XMLs in two hash-matched batches; keep466 local copies. Standard build/test cleanup
succeeds and only two unused new shipping copies are removed; signed APK/evidence remain.

No recurrence algorithm, financial writer, schema, SDK, permission or commercial activation changes.
Broader financial/security/stability/performance/accessibility/iOS/provider/Store/publication gates
remain open. New-head hosted CI is pending at commit creation. The independent unfinished D-516
SUPA work is not part of this delivery. Evidence: [original recurrence and caption qualification](quality/physical-custom-second-day-holidays.md).

## D-517 - Owner-authorized temporary screen-awake timeout during tests

The owner requests preventing screen sleep during testing. Record the original timeout, extend
only that setting temporarily for an authorized test turn, then restore the exact original value
at completion, interruption or handoff and verify it. No other OS/security/account/clock setting
or authentication action is authorized. Existing owned-foreground and fictitious-source guards
remain. D-515 records original300000ms, temporary1800000ms and successful exact restoration/readback;
this does not qualify unchanged power/Doze conditions during the temporary interval. AGENTS.md
contains the narrow exception. No application code, business suite or prior financial work needs
repeating for this execution rule.

## D-518 - Exact review fixture recognition after hosted architecture failure

D-515 head b6e9e100430b64e933210666045b31e27199572b fails hosted CI40121879246 in exactly
FeaturePolicyTests.No_page_reads_the_mode_itself: the new DebugSnapshots.PlanHolidayHint.cs
sets an independent fixture's mode but was omitted from the explicit fixture list. Both hosted
platform builds and the App tests pass; CodeQL40121879326 passes. Preserve that failure.

Register only the exact holiday fixture in the existing list. The unchanged validator checks its
whole outer DEBUG && WINDOWS gate and rejects ordinary Release fallback or escaped statements;
production pages remain subject to the same mode-read rule. No product behavior, permission,
financial data or build policy changes. Forty-three focused FeaturePolicy cases pass in a strict
zero-warning build, including the failed source check and negative gate cases. Current local
SUPA review is independently registered in unfinished D-516 and is excluded from this commit.
No successful platform/native/financial cohort is repeated for this source-contract correction.
Hosted confirmation belongs to the next pushed head's one-shot CI check.

## D-516 - Independently implemented original SUPA4 CSV contract

SUPA4 original posted cashflow is an explicit second software-export preset beside StarMoney,
not a direct-bank schema or automatic relationship inference. Preserve source dates/currencies,
complete paged review, explicit ordinary cashflow and same-software-history consent, source/target
identity, transactional stale-file/source/authority refusal and durable original Undo. Reject
pending/informational/batch/return data; do not silently discard equal expenses without original
row plus account identity. Unknown contracts use explicit mapping. No new SDK, schema, permission,
network operation or commercial activation is added. Source narrative may contain personal data;
structured owner-account identifiers do not enter notes, and the review shows at most a suffix.

Sixty-seven new actual-source cases and29 affected StarMoney cases pass in selected groups;23
resource checks pass. Windows36 original review/Help/native Cancel contexts and12 final changed
Help-only contexts pass, with complete source rows unchanged and zero financial writes. Broader
bank/customer-file/provider/security/stability/accessibility/iOS/Store/release gates remain open.
Six original ARM64 Release/AOT Android contexts at actual384dp qualify full source/warning/consent
text, Help/OK, picker and actual checked/command state on the same retained UID10395 source; every
fixture/base typed row stays equal and zero financial writes occur. Preserve both terminal observer
failures, repair only owned label selection and CompoundButton click-result interpretation, and
continue incomplete observations without reseeding. The signed dual-ABI Offline384AOT candidate
adopts Main without reset; Main31882/Home979/all141 public tuples, three prior profiles/current Main,
recorded OS and exact timeout300000 are verified. Original Main SUPA chooser/Help is checked without
file selection or financial commands. Only the new caller and28 hash-matched remote XMLs are retired;
local evidence and actual failed APKs remain. The physical turn is explicitly handed back. D-516 is
locally complete; new-head hosted CI remains separate. See [original SUPA evidence](quality/supa-csv-import.md).

## D-519 - Scoped SDK acceptance and new compatibility observations

The owner accepts the presented official SDK terms for the three isolated compatibility packages;
AGENTS.md records the narrow scope. General SDK/licence, existing ADB servers and physical phone
remain unchanged. A DEBUG Windows continuation qualifies36 new SUPA scale2 contexts,324 actual
native captions and eight tall endpoints with complete original typed rows and zero financial
writes. Real Windows scale is unchanged; this does not qualify Android OS200% or spoken access.

The unchanged complete signed D-516 candidate installs/runs on new actual API24/4096-byte and
API35/16384-byte x86_64 AVDs. Original first-run once, Home0, empty Plans/Budget/Transactions,
Reports and Home return preserve each app PID without ledger writes. This is bounded runtime
compatibility, not full platform/security/performance/accessibility/release acceptance. Original
setup/foreground/cleanup observer failures are retained and only incomplete work continues.
See [runtime and large-text evidence](quality/android-runtime-supa-large-text.md).

## D-520 - Original key-adapter runtime subgate

The unchanged original key protector and independent fixed-identity review run on actual API24/
4096 and API35/16384 x86_64 after D-519 SDK acceptance. One fresh run per UID completes creation,
actual cold reopen with twelve refusals, and denied/authorized retirement without lost-key recreation.
Six native receipts,24 tamper/binding refusals and four authority refusals add no managed test cases.
API24 metadata is intentionally null; API35 level0 is software, not physical hardware protection.
Preserve the command-line version-attribute and installed-digest observer failures; no phase or
successful financial/device cohort is replayed. Retire only the new helpers and stop owned AVDs.
No shipping source/APK or Main keys change. Full SEC/platform/authentication/iOS/release remains.
[Original runtime evidence](quality/android-key-runtime-compatibility.md).

## D-521 - Actual storage runtime cohort and prearmed export observation

The original Data/Shared compiled model and all51 migrations, encrypted snapshot/typed rows,
selected cold migration, missing-key refusal and recovery qualify on actual x86_64 API24/4096
and API35/16384. Each has16 final-cohort records; synthetic rows are prepared once per required
new-platform case, no accepted financial command or phone source repeats. Exact retained source
copies continue the unfinished export observation without new financial seeding.

The old host poll reaches Selected/CandidateVerified before stopping and correctly fails its
KeyPrepared assertion. Preserve both failures. Add read-only checkpoint observation and fixed-UID
source adoption in the independent helper, and arm a guarded device-local observer before launch.
The native post-kill check then proves original source/key preservation at KeyPrepared and the
same migration completes. Ten controller contracts and two additional original continuations
pass. No production source/APK changes; Main keys/phone stay outside scope. Full multi-profile,
hardware/OEM/authentication/ARM64/iOS/performance/accessibility/release gates remain.
[Whole cohort, actual failures and cleanup](quality/android-storage-runtime-compatibility.md).


### D-522 - Native profile and restore runtime subgates

Nine original two-profile/multi-generation cohorts pass in18 native phase receipts on each
actual x86_64 API24/4096 and API35/16384 system. Covered startup, interrupted creation, missing
envelopes, wrapping-alias loss, recovered retirement, validated/retained-preference restore,
pending-reference refusal and consecutive restore recovery preserve original model/money/key
invariants through proven cold process transitions. Reuse the complete D-521 signed package;
zero new managed cases, no source/schema/permission/shipping change or repeated passing build.
Prepare fictitious rows once per required new cohort; no accepted financial command is replayed.
Installed public bytes match, only the new helpers are uninstalled and owned AVDs stopped.
Physical Main/private keys, general SDK and existing ADB remain outside this turn.

Full arbitrary-profile/hardware/OEM/ARM64/biometric/authentication/UI/stability/accessibility,
iOS/provider/Store/release acceptance remains open. D-521 CI/CodeQL pass; its separate Windows
protected-workload workflow failure remains under investigation, with Android producer success
preserved. See [bounded original runtime evidence](quality/android-profile-runtime-compatibility.md).


### D-523 - Correct independent workload revision preservation

The failed Windows qualifier step reproduces locally: deliberate validated Save/Delete operations
restore all financial rows but correctly change HomeFinancialRevision. Keep production snapshot
verification strict. Normalize only the validated opaque singleton in a disposable benchmark
memory copy, then compare full schema/headers/triggers and every other typed row. Eight focused
new cases cover round-trip/read-only acceptance and financial/schema/header/revision/boundary
refusals. Revalidate the same retained reference from its original encrypted rollback projection
without replaying financial commands; execute only the previously unprepared tenfold shape.
Its four saves read back and remove exactly their own rows, and repeat admission refuses.
No app/MAUI/Main/phone/dependency/schema/permission change or unrelated passing suite repeats.
D-521 CI/CodeQL pass, Android producer succeeds, Windows protected measurement fails. The owner-opened authenticated browser now shows the same snapshot-content exception at original
Program.cs line195 after a zero-warning build. Corrected D-523 Native cipher candidates succeeds;
latest D-525 CI and CodeQL also succeed. Preserve original failures without rerunning accepted work. See [local defect and hosted boundary](quality/protected-workload-revision-preservation.md).


### D-524 - Native metadata durability on minimum-API/16KiB systems

Original identity staging, journal/history publication and hot-journal rollback/committed selection
pass on x86_64 API24/4096 and API35/16384 using unchanged signed441faf38. Twenty observations comprise
ten controlled synchronized native deaths and ten terminal receipts; zero new managed cases or
financial preparation/commands. Same-source cold lineage, complete original facts and installed
code match; remove only new helpers and stop owned AVDs, preserving ADB/Main/phone/SDK boundaries.
This closes only named controlled metadata subgates, not natural power loss/hardware/OEM/ARM64,
authentication, full platform/UI/stability/accessibility/iOS/provider/Store/release acceptance.
See [actual metadata receipts and limits](quality/android-metadata-runtime-compatibility.md).


### D-525 - Image/PDF native runtime compatibility

The original reader/parser process ten licensed public photographs and nine fixed image-only PDFs
once on each x86_64 API24/4096 and API35/16384 runtime. Preserve five correct Found/three Review/two
NotFound photographs, five Latin scan totals, bounded last-page total and conflict/no-total refusals;
Persian scan OCR remains unaccepted. Thirty-eight observations/four terminal batches add no managed
cases. One new strict Offline signed192AOT/204ELF16KiB package, exact source assets/licence and
installed-code digests pass; zero financial saves, no Main/phone/private-key access. Retire only new
helpers/owned AVDs, clean isolated output and61098430bytes of unused copies, preserving signed code.
Full camera/corpus/saved-flow/Persian-image/iOS/ARM64/stability/accessibility/provider/Store/release
qualification remains separate. See [exact original runtime evidence](quality/android-receipt-runtime-compatibility.md).


### D-526 - Original physical SUPA import and durable Undo

One new fictitious Samsung profile/account opens900EUR. The owner selects the exact original
SUPA CSV in the external picker; actual full source/target/two-row review, original consent and
one confirmation import30/-7.50, preserving quoted/UTF-8/CRLF narrative, two unreviewed ledger
rows and Home922.50. One proven cached16616 death and COLD24834 preserve that same data and
original durable batch; one original confirmed Undo returns900/All-history empty. Return Main979,
delete only the fresh inactive profile with both confirmations, and preserve original three
profiles/all141 public tuples/installed D-516 hash/OS/timeout300000. Delete97 owned hash-matched
XMLs and one owned CSV; keep local receipts and all guard refusals. Zero new managed cases,
financial replay, production changes, builds or passing-suite repeats. All bank/customer-file/
platform/security/stability/accessibility/iOS/provider/Store/release gates remain separately open.
See [original financial, cold, Undo and restoration evidence](quality/physical-supa-import-undo.md).

### D-527 - Independent original Home stage diagnosis

The independent Windows home-stages mode separates actual complete Home reading from final source
validation on retained D-523 reference/tenfold generations. Four observations preserve complete
original packets and exact portable schema/typed rows with unchanged financial revision, zero
financial writes and no fixture recreation. Tenfold read463.71/570.91ms and validation146.28/123.70ms
are local store boundaries, not Android/UI/cold timings or a product speedup. Strict independent
build and missing-fixture refusal pass; no new managed cases or unchanged native suite/APK replay.
Physical D-526 preserves its original opening900EUR/empty ledger and is qualified separately.
Android projection/read diagnosis, whole-Home/two-second, ANR/accessibility/platform/external gates
remain open. See [exact observations and limits](quality/protected-home-stage-diagnosis.md).


### D-528 - Transactions Add beside the title

Owner-requested Transactions + now matches Home/Plans: original48px accessible command physically
right of a growing title, original pushed Back, no reserved bottom Add band/overlay. Preserve full
filters/results, empty invitations and separate contextual bulk/Undo rows; filter sizing reserves
only the actual header and contextual actions. Eighteen Windows contexts/54 images qualify populated,
filtered-empty and original selection states; six independent Samsung Release/AOT contexts qualify
native Add/Cancel/bulk with unchanged original stored inputs. Retain controller/count and observation
refusals without replaying completed work. Strict builds, complete signed Offline384AOT/408ELF16KiB
static checks and no-reset shipping upgrade24834->32206 pass. One original Main Add/Cancel, Home979,
all141 public tuples/three profiles/OS/timeout300000 remain; no Main financial writes or new managed
cases. Retire the new helper and15 hash-matched XMLs. Full stability/accessibility/iOS/external
release gates remain separate. See [complete runtime and package evidence](quality/transactions-title-add.md).


### D-529 - Retained complete-ledger stage diagnosis

Twelve original Windows/API36-x64/Samsung-ARM64 observations separate actual SQL stepping, complete
materialization/sort and Core operations while preserving all32 fields/order, financial controls and
source snapshots; native original file/metadata/envelope bytes match authenticated rollback rows.
Samsung tenfold typed reads1365.95/1476.00ms are stages, not whole Home/two-second acceptance. The
emulator's exact caller/Main3341/Home0/120 public tuples/OS are restored and its AVD stopped. In the
fresh explicit phone slot, current Main30047 differs from historical32206 for an unestablished cause;
preserve its actual30047/APK/Home979/141 tuples throughout this turn. Restore original caller APK/
UID10381/stopped/grants/no-pending state and exact timeout300000/OS, remove only two new hash-matched
XMLs and explicitly return the phone. Reject typed timestamp dispatch after eight relevant comparison
observations show no stable material benefit; restore exact production source and touch it. No seed,
financial writer, new managed case or shipping change. General UI/cold/stability/platform/external
gates remain open. See [actual evidence and limits](quality/retained-ledger-stage-diagnosis.md).


### D-530 - Retained Transactions title at double-size Windows text

Close only the changed layout's Windows process-local large-text subgate while the Samsung is on
its authorized other-project turn. Reuse the completed D-528 independent source before its seed
branch, with separate scale2 observations and unchanged original receipts. Eighteen new en/fa/de,
Light/Dark,360/412/1280 contexts produce54 own-window images/native title/viewport checks. Actual
ratio2/full untrimmed title/allocated height, right48-unit Add, no bottom Add row and nonoverlapping
contextual bulk actions pass; original accounts/entries/settings/plans match. PID19056 exits0;
Default input desktop is unchanged, strict Debug build/cleanup have zero warnings/errors. No financial
preparation/commands, managed cases, OS setting change or Android/ordinary matrix replay. Shipping
D-528 is unchanged. This finite host subgate is delivered independently; D-529 separately qualifies
original reader stages. Actual Android OS200%, audible reader and full accessibility/stability/
platform/release acceptance remain open.
See [source, native observations and boundaries](quality/transactions-title-large-text.md).

### D-531 - Current retained Windows Home publication

Four new actual Home constructor/publication/native-layout timelines on the retained D-523 protected
10,000/100,000-row sources complete in process61988/exit0. Advanced/default layout, all20/200 VM
accounts, original balances, covered publication and no synchronous appearance reads match. Full
portable schema/typed rows, every original source/metadata/envelope file, the independent installation
inputs and original receipts remain unchanged; zero seed/financial commands or phone queries.
Tenfold native layout completes1226.20/1144.04ms from before construction on this Windows path,
not cold/Samsung/two-second or general stability acceptance. Strict Debug build/isolated cleanup and
prelaunch missing-source refusal pass; no managed count or shipping APK change. Retain the broader
performance/stability/accessibility/platform/external gates and pursue evidence-supported remaining
work without replaying these accepted samples. [Evidence](quality/home-publication-stages.md).

### D-532 - Current Android Home and preserved concurrent-major fault

Reuse original retained Android rows/keys through independently copied encrypted sources. Their
single D-509 additive migration uses the actual protected callback and preserves every original
schema/header/typed row after normalizing only new bookkeeping in disposable memory. Six current
API36/x64 native Home contexts/twelve samples retain all financial/native/layout checks. Preserve
three completed reference contexts across actual4070/SIGSEGV; continue only the unexecuted tenfold
contexts. Matched official symbols locate concurrent-major object scanning, without proving a cause
or fix. Tenfold1939.55-2353.59ms navigation publications do not close the two-second/cold/phone gate.
Recover the same completed inner report after an overbroad file guard counts new presentation
outputs; eight exact-source guard contracts and four fresh read-only source/control audits pass,
without UI or financial replay. Historical timing-interval exact-byte equality remains unaccepted.
Restore caller UID10221/SHA/stopped/grants, Main2963/Home0/all120 public states and captured OS;
remove three owned hash-matched XMLs and stop only the dedicated AVD. Strict builds/standard clean
pass; additional residual host deletion is refused and not claimed. Shipping D-528 is unchanged.
[Exact lineage, native fault and limits](quality/current-android-home-stages.md).


### D-533 - Current physical Home with original timing-byte evidence

Six actual Samsung current-Home contexts/twelve observations reuse the original encrypted10,000/
100,000-row sources, with zero seed/financial commands or new keys. The original additive callback
and complete schema/header/typed-row/native controls pass. Retain a create-once synchronized
pre-native source/code/day fingerprint and zero-case progress; ten focused actual-policy contracts
pass and the physical timing-interval byte/source guards pass. Preserve the undeclared Activity
controller refusal; start the resolved original launcher once and observe the same32040 after the
first report window expires. Separate clone and failed-adoption provenance. Tenfold4777.22-4992.71ms
publications keep the two-second/general stability gates open; original D-532 historical-byte refusal
and native death remain. Restore original caller UID10381/APK/stopped/grants/no-pending, Main24725/
Home979/all141 tuples and OS; remove two owned hash-matched XMLs and explicitly return the phone.
Three strict builds/standard clean pass;187287221bytes additional host deletion is refused before
execution. No unchanged Windows/full-suite or accepted financial replay; product test count and
shipping D-528 stay unchanged. Runtime/compiler/task10.0.13 identities match; no unsupported mix
or general crash fix is established. Broader performance/accessibility/platform/release gates remain.
[Original runtime evidence and remaining limits](quality/physical-current-home-stages.md).


### D-534 - Bounded current Home CPU sampling

Preserved original API36/x64 sources produce a code-address-only CPU sample with2733 samples,
zero lost and no raw stack/register payload. The actual SDK classifies69 empty AOT objects;
all123 method-bearing objects match the diagnostic signed artifact. Retain initial4010/SGen
SIGSEGV and Android EXIT_SELF/status255 separately; same-source4313 continuation keeps the three
accepted reference contexts and executes only missing tenfold contexts. Complete source/native
and premeasurement-byte controls pass without seed/financial replay. Flat CPU is Mono40.98% and
SQLCipher17.38%, but54.26% broken callchains preclude reliable inclusive/root attribution, KDF-only
claims or a whole-Home/cold/phone speed conclusion. No product/runtime change or new product tests.
Original caller restoration hits storage exhaustion; documented package-specific maintenance and
Android KEEP_DATA code removal/reinstallation restore exact original digest/UID10221/stopped/grants.
Main2878/Home0/all120 public tuples and OS match; three owned hash-matched remote files are removed
and only the dedicated AVD is stopped. Post-restoration private DB/key reopen is not claimed.
General stability, two-second, accessibility/platform/external gates and the broad goal stay open.
[Actual sample, lineage, restoration and limits](quality/current-home-cpu-sampling.md).


### D-535 - Protected-source reopen after data-preserving maintenance

One focused API36/x64 read-only audit verifies existing Android keys/generation bindings after
D-534's KEEP_DATA code maintenance. Reuse the exact signed D-532 audit caller and original
lineage; process3600 reopens four original/adopted sources, matching complete schema/headers/typed
rows and all source files. Zero financial/seed/key creation, native UI or timing replay; historical
D-532 timing-byte refusal remains false/unaccepted. Ordinary signed-r installations return the
original caller UID10221/digest/stopped/grants, Main2845/Home0/all120 public states and exact OS.
Two owned XMLs are hash-cleaned, one redundant57818872-byte host copy removed after equality and
only the dedicated AVD stopped. No rebuild/unchanged suite or phone query; shipping D-528 unchanged.
This bounds independent source retention, not Main-key attestation or general platform/stability/
performance/accessibility/release acceptance. D-533 CI/CodeQL succeed and its follow-up is consumed.
[Actual read-only outcome and scope](quality/post-maintenance-source-retention.md).


### D-536 - Withdraw reduced-allocation ledger ordering

A stable-merge sort reduces host allocation but slows every large shape; a cached UTC/day/ordinal
candidate reduces100000-row host sort allocation4000504 to1600168 bytes and passes thirty original
object-order shapes plus seven affected SQLite regressions. Actual original3844/candidate4548
API36/x64 cohorts retain all schema/typed-row/native/financial/premeasurement-byte controls with
zero seed/key/financial replay. Candidate report observation continues the same live4548 after
its first window expires. Large-pair ratios1.0035/1.0123/0.9659 show no stable material Home benefit;
withdraw all candidate product code and keep shipping D-528 unchanged, without phone repetition.
Preserve an initially missing installed APK after boot; signed restore, sync/orderly shutdown and
one new cold boot then retain exact original UID10221/APK/stopped/grants, Main2880/Home0/all120 states
and OS. Root cause/power-loss safety remain unproved. Retire only two completed inactive emulator
callers' code after exact APK backup; retain their data/keys/UIDs. Fresh returned phone Main27069/
Home979/all141 states remains untouched except read-only preflight/owned XML cleanup. Five emulator
XMLs and one phone XML are hash-cleaned; standard cleans and111079544-byte duplicate removal pass.
Automatic removal of251505548 bytes of three new output trees is refused before execution; no
alternate deletion. Latest D-535 CI/CodeQL succeed. Product test count and wider acceptance stay open.
[Actual comparison, storage recovery and limits](quality/withdrawn-ledger-ordering.md).

### D-537 - Physical Home CPU windows on retained sources

Add an explicit one-publication tenfold/en code-sampling scope with native-monotonic boundaries,
durable scope identity and a released strong full-ledger oracle reference. Ten exact-source scope
contracts, ten unsafe-header refusals, one oversized-input refusal and retained2733/1324/0 native
filter controls pass. Three necessary strict independent ARM64 publishes pass; only the final
signed offline123-packaged-AOT/135ELF artifact is installed. Preserve first recorder exit134 before
any samples and its completed4296.4346ms publication. One necessary CPU-only retry on the same APK
records8634 samples/zero lost; exact native/inspector filters retain2380 inside4140.808074ms,
partitioned2437.831921/1490.250461/212.725692ms. All original/adopted bytes, complete schema/typed
rows and native financial/recent packets match without seed/key/financial replay. Official Mono
BuildId/debuglink-CRC and packaged AOT symbols resolve actual function ranges; balance/index,
SQLCipher and concurrent GC work coexist, unknown samples and an uncollected oracle limit causal
claims. Restore original caller UID10381/APK/stopped/grants and Main27069/Home979/all141 states/OS,
remove eight owned hash-matched remote files and return the phone. Product source/APK remainD-528;
no unchanged Windows/full suite repeats or new product test count. D-536 CI/CodeQL succeed and its
one-shot is consumed/deleted. Two-second/general stability/accessibility/platform/release gates and
the broader goal remain open. [Complete code-window evidence and limits](quality/physical-home-cpu-windows.md).

### D-538 - Withdraw direct endpoint-index construction

A direct original-reference loop lowers host constructor cost but offers no material allocation
relief; its protected read-only-view variant adds8.46% allocation in the many-account shape.
Twenty-six focused actual-Core cases, including two new collection-mutation/captured-slice
contracts, and strict managed/independent ARM64 builds pass. Preserve one unexecuted fixture
compile refusal, the observer240-second parameter refusal and its same10999 continuation,
plus candidate12246's continued observation. Original/candidate single tenfold/en publications
4131.4756/4010.2595ms pass all encrypted-source/adopted-byte/schema/typed-row/native/financial
guards without seed/key/money replay. The2.933966% reduction fails the previously recorded5%
each-pair criterion; omit the unnecessary second pair, restore exact original product source
and pass only the two new tests on that rebuilt original. Restore caller UID10381/APK/stopped/
grants and Main27069/Home979/all141 public states/OS; hash-clean two XMLs and return the phone.
Three scoped cleans and177874001-byte unused-copy removal pass; automatic review refuses deletion
of132837195 residual output bytes before execution, with no alternate attempt. D-537 CI/CodeQL
succeed and its one-shot is consumed/deleted. Product/APK remainD-528; coverage adds two cases,
not a complete-suite pass. Two-second/general stability/accessibility/platform/release gates
and the broader goal stay open. [Complete evidence and limits](quality/withdrawn-account-index-loop.md).

### D-539 - Isolate complete Home controls in another process

The released100000-row oracle remains alive in D-537/D-538; add explicit preparation/measurement
scopes without changing product source, financial formulas or GC policy. Bind compact original
balance/recent/full financial-presentation controls to source-byte-fingerprint/payload hashes,
day, Data/Core/Caller MVIDs and preparation PID/UID. Thirty-eight exact-source identity/scope
contracts and strict initial/full AOT builds pass without warnings/errors; no product test count
or unchanged Windows/full-suite repeat. Complete signed offline/non-debuggable/non-profileable
ARM64 APK has192 packaged AOT/204ELF/804LOAD static16KiB checks and v2/v3/ZIP verification.
Actual15744/e9aac472 prepares original controls with zero target native publications; preserve
its expired observation and recover the same final report. Prove15744 dead, then the same APK,
without reinstall/adoption/seed/key/money replay, measures16357/275fc72d. No full target oracle
read occurs before timing; complete original/adopted bytes/schema/typed rows and independent
financial/native packets match. Elapsed4321.904ms/read2735.4251/model1304.2752/layout281.8074 and
max dispatcher536.1012ms still fail the two-second objective; no product improvement/regression
or causal GC claim. Restore caller UID10381/APK/stopped/grants and Main27069/Home979/all141 states/
OS, hash-clean two XMLs, remove53992832 duplicate bytes, perform scoped cleans and return the phone.
Residual native cache131261554 bytes is inventoried; earlier cleanup refusals remain untouched.
D-538 CI/CodeQL succeed and its one-shot is consumed/deleted. General stability/accessibility/
platform/release gates and the broader goal remain open. [Complete evidence and scope](quality/separate-process-home-oracle.md).

### D-540 - Complete encrypted ledger transport (locally qualified and adopted)

Preserve the complete32-field history and original physical/date/creation tie order while adding
bounded512-row internal encrypted read blocks. The original source/writer/financial formulas stay
authoritative; revision/schema/count/digest/type/pipeline guards fall back to scalar reads without
getter repair. Original transaction commit validates authority before/after affected-block upkeep;
cancellation/IO cannot leave a separately accepted financial edit. No EF entity/model/key/permission
or new SDK change. Focused codec/migration/source/rollback/backup/restore/authority/observer checks
pass in selective continuations; preserve original fixture/parameter/observation failures, not a
full-suite green claim. Strict Windows and full signed offline dual-ABI384AOT builds pass.

Two opposite-order, same-APK/source/day/module Samsung pairs use independent scalar controls in
other proven-dead processes:4710.9157to3323.291ms and4665.0514to3304.8118ms (29.4555%/29.1581%).
All original financial/native/recent/presentation and complete source guards match. Four single
tenfold-English publications do not accept cold/full-device/language coverage or two-second Home.
No accepted financial seed/key/command is repeated. The two-second objective remains open.

The exact old caller APK supports50 migrations ending20261017013925; reverse only M1/M2 internal
bookkeeping on empty independent Main/UID10381 using durable original request, actual memory Down
and full source/schema/header/typed-row proof. Native18045 passes with no shipping/workload touch.
Preserve an earlier undispatched17203 entry-registration omission; correct only its Activity
allowlist and retain original performance APK/Data/Core exact-byte continuity without replay.
Initial old-APK installation observation expires at35seconds with Play Protect foreground; retain
that pending result. After the owner's personal decision, the same install completes with original
b1d5 APK/UID10381; normal21974 startup reaches original onboarding, then stops with proven death.
Main27069/Home979/all141 states/OS remain exact. A local UTF8-reader refusal precedes any shipping
install; correct only encoding and preserve the baseline. After explicit handback, one complete signed no-reset upgrade adopts e0d3 shipping bytes and
Main27069 becomes27774. Preserve the initial16-node Preparing refusal; the same process then
matches Home979/all141 public states and all three original profile actions/selection/OS.
Independent original caller APK/UID/stopped state/storage identities remain exact; remove only17
owned hash-matched phone XMLs. No financial Save or private Main key/database read occurs.
Independent new-candidate bootstrap also reaches
original onboarding on API24/x64/4096 and API35/x64/simulated16384, with no UI/financial input;
remove only new fixtures/two owned XMLs and prove both AVDs stopped with sharedADB76812 retained.
Goal/full stability/accessibility/iOS/provider/Store/release remain open. [Evidence](quality/complete-ledger-read-blocks.md).

D-540 failed-case continuation while the phone gate remains: select only the eight initial store
cases lacking any successful terminal outcome. Actual strict rebuild and all eight cases, including
fixture disposal, pass; exact result-name comparison excludes previously successful/unrelated
cases. Preserve initial failure receipts; no native financial commands or full-suite claim.

### D-541 - Qualified and adopted bounded Home publication connection

Retain one original protected factory context only through the covered Home publication, with
separate snapshot and fresh validation transactions. Keep original file/access/input/revision/
definition checks, KDF and financial formulas. Dispose before period/source retry or uncovering;
the old context must not block pending maintenance while another factory read waits. Legacy
APIs retain their separate scopes and share unchanged query/validation bodies.

One independent-only numeric observer isolates owned EF contexts without SQL/data/key logging.
Same-APK opposite-order Samsung100000-row pairs measure3518.1483to3310.2394ms and3443.1894to3261.4481ms,
5.9096%/5.2783% improvement with complete original source/financial/native/presentation equality.
Separate scalar controls finish in other proven-dead PIDs; no financial seed/key/command repeats.
Sixteen distinct selected cases pass (13new/3affected original), strict Windows/full Android builds
are clean, and384 actual packaged AOT/408ELF/static16KiB/ZIP/v2v3/privacy checks pass. Preserve two
initial test-serialization failures and the separate artifact-path identity refusal; an aligned
package restores exact qualified Data/Core image equality without replaying successful cohorts.
Original independent caller retirement/startup/stopped state is restored. The final signed a2d98e
Main install succeeds once. Main a2d98e/PID29566 preserves Home979/all141 public states, all three original profiles and
exact OS settings after the one signed no-reset upgrade and authorized normal Resume. Original
independent caller bytes/UID/storage/grants/stopped state match; ten new owned hash-matched XMLs
are removed. The earlier foreground refusal and17997 external task removal remain recorded.
Two newly owned independent x64 compatibility fixtures qualify nonempty100000-row block reads
and actual original Home publication on API24/4096-byte pages and API35/simulated16384-byte
userspace pages over a4096-byte kernel. Each fixture is seeded once with110000 reference/tenfold
rows; scalar controls finish in a separate proven-dead PID before the one native publication.
Complete original schema/typed-row/financial/native guards pass with one context, one block read,
zero scalar reads and no post-setup financial command/key/seed replay. Both initially absent
packages are removed and only the two owned AVDs are stopped, preserving their settings.
This is not hardware16KiB, whole-platform, startup, stability or two-second acceptance.
No two-second/stability/accessibility/platform/release completion claim.
[Complete evidence](quality/home-publication-connection.md).

### D-542 - Complete Home row publication

Publish the original Home period/upcoming/recent/pinned-goal rows through the existing complete
snapshot collection, staging each original row set before one Reset. Keep all original mappings,
order, fresh theme objects, command identities, full reads, cover and final validation; chart/brush
publication and layout remain unchanged. Eight related existing cases pass after this changed
usage, not new-test/full-suite evidence. Strict Windows/full Android builds are warning-free.

On the retained x64 API36 sources,12 light and12 distinct dark native publications across
reference/tenfold and en/fa/de retain complete original schema/typed rows, financial/native
presentation and recent packets. Each of the four row sets produces one Reset and no per-row Add.
No successful seed, key or financial operation is replayed. These full-control elapsed samples
are not a causal speedup or physical two-second claim. The compatible independent caller stays
stopped on its additive schema because its ordinary Main is nonempty; empty-Main retirement is
inapplicable. Original APK backup/retained sources/Light remain, and shipping emulator Main's
120 original public states match the historical unchanged-APK baseline. Preserve its initial
15-node startup-only observation refusal.26 owned hash-matched XMLs are removed; only that AVD
is stopped.

The complete signed offline package has384AOT/408ELF/1608LOAD static16KiB/ZIP/v2/v3 guards and exact
actual packaged Data/Core LZ4 inputs matching the qualified independent APK. One Samsung no-reset
upgrade tobc1ea2 retains PID4090/Home979/all141 public states, all three profiles and original OS.
The unrelated-app foreground refusal is preserved and the owner's normal return resumes only
readback, never reinstall or money. Wider two-second/stability/accessibility/platform/provider/
Store/release gates and the broader goal remain open.
[Evidence and retained failures](quality/home-row-snapshot-publication.md).


### D-543 - Withdraw the shipping-equivalent large-nursery candidate

Keep production D-542 unchanged after a bounded independent32MiB concurrent-nursery experiment.
Earlier ARM64-library opposite-order results improve9.577468%/12.014471%, but their financial
images do not match shipping AnyCPU and are not promoted. A project-local diagnostic RID fixes
that input mismatch; all206 managed images match between the new profiles and actual packaged
Data/Core match the full shipping image. The shipping-equivalent pair improves3145.0593to
2801.7544ms, but terminal PSS/RSS increases112823/171516KiB exceed the predeclared65536KiB
ceilings. Reject without reverse/rescue pairs or relaxed criteria. These are post-audit residency
observations, not peak-memory or exclusive GC-causality evidence. The provisional app-only runtime
configuration is removed and no shipping Main install occurs; the two-second goal stays open.

Retain the fixed owned-query labels without SQL/parameters/values/paths/keys, one new actual
SQLite privacy case plus one affected original case, and17 actual compiled fingerprint cases
including four new runtime-profile refusals. Independent strict builds/full signed static
shipping witnesses pass; this is not a full-suite or product performance acceptance. No accepted
financial/seed/key operation or unchanged successful suite repeats. Original caller restoration
uses the unchanged guarded empty-Main retirement; broader goal/security/stability/accessibility/
platform/provider/Store/release gates remain open.
[Exact inputs, failures and qualification boundary](quality/concurrent-home-runtime-qualification.md).


Final D-543 restoration: original b1d5 caller22417 reaches unchanged English onboarding and is
proven stopped with UID10381/storage/grants intact. Main4090/D-542/Home979/all141 public states
and OS match; seven owned hash-matched phone XMLs are removed. Ten owned bin/obj trees totaling
4179578901 logical file bytes are cleaned. No Main installation/private key/database read or
financial replay occurs. The finite batch closes without a shipped runtime/performance change;
two-second and broader qualification/release gates remain open.


### D-544 - Withdraw bounded expansion scratch after original whole-Home comparison

A source-only scratch candidate reduces host codec allocation23.692979% but its actual original
Samsung100000-row publication3069.5168to3154.4552ms is2.767159% slower, failing the predeclared5%
gate. Original source/financial/native controls and memory/dispatcher gates pass; no reverse/
rescue pair follows. Restore production exactly; no Main installation or financial/seed/key
operation occurs. Keep one new real SQLite digest-valid incomplete-compression fallback test,
passing on the restored original codec. Eight scratch-only tests leave with the unshipped API;
31 distinct selected candidate checks and strict builds are not a full-suite claim.

Both same-head independent packages preserve original runtime/cipher and financial Core bytes;
metadata inspection bounds eight new pool method roots in the same official CoreLib version,
without claiming common IL/whole-AOT equality. The full uninstalled384AOT/408ELF signed candidate
matches actual independent Data/Core. Original caller27694/b1d5/UID10381/storage/stopped and
Main4090/D-542/Home979/all141 public states/OS are restored. Three owned hash-matched XMLs,
16 completed bin/obj trees and two duplicate audit images are removed. Two-second, general
security/stability/accessibility/platform/provider/Store/release and the broader goal stay open.
[Exact candidate, refusals and final restoration](quality/bounded-home-scratch-buffers.md).


### D-545 - Withdraw overlapped Home work after the fixed residency gate

The candidate overlaps the unchanged original plan/forecast computations with covered ledger
work, drains all siblings before operation disposal and classifies calendar-only covered retry
separately from persisted-data authority. Thirty distinct selected checks pass (13 new/17 affected
original), with strict Windows/full signed Android builds warning-free. Preserve initial test
compiler/cancellation and runtime-version-observer refusals; rerun only the failed observer case.

One same-day original Samsung100000-row pair improves3698.2818to3022.0211ms (18.285808%), but
terminal RSS increases113540KiB above the fixed65536KiB gate. PSS increases13066KiB; dispatcher
passes. Original source/schema/typed-row/financial/native guards pass; swap differs materially,
so no exclusive code/peak-memory cause is proven. Withdraw without reverse/rescue or threshold
change. Restore every production/test-project source exactly and remove the13 unshipped-API
cases; no new product-test count, Main install, financial/seed/key or runtime change is claimed.

Both independent APKs retain exact financial Data/Core/runtime engines; differing trimmed BCL
images have the same original official versions/method sets, without common-IL equality proof.
The uninstalled384AOT/408ELF full shipping candidate matches actual independent Data/Core.
Original caller5336/b1d5/UID10381/storage/grants/stopped, Main4090/D-542/Home979/all141 attributes
and OS are restored; three owned hash-matched XMLs are removed. The owner approves a temporary
shared-phone handoff only after restoration. Continue independent work; the broader goal and
performance/security/stability/accessibility/platform/provider/Store/release gates stay open.
[Exact inputs, refusals and restoration](quality/overlapped-home-projection.md).
Eight completed owned bin/obj trees (2322515022 logical bytes) and four duplicate audit
images are removed; actual packages/reports/backups stay. No host free-space claim.


### D-546 - Original Transactions title at actual Android OS200%

Close only the uncovered D-528/D-530 Android title/viewport subgate:36 distinct original native
observations across360/412dp, en/fa/de, Light/Dark and populated/view-only empty/selection states.
Actual OS font2.0 yields36px for this24dp title under Android nonlinear scaling; full original
translated text, physically right48dp accessible Add, separate contextual rows and at least144dp
results pass. Four original typed packets retain their digest; no seed/Save/Undo/native Add/Cancel
or successful normal-size case repeats. No shipping UI/financial source or product-test count changes.

Preserve the initial missing selector, later publication/source-precondition and host evidence/
locale assumptions. Three earlier English/Light360 successes are retained; only15 missing360 and
18 new412 states continue. Exact known comparison/source lineage is audited without scene replay.
Independent strict builds and192AOT/204ELF/ZIP/v2/v3/static16KiB guards pass; no unchanged shipping
Windows/Android build or managed suite repeats. Undo-visible/spoken/device/full A11Y remains open.

Two install-space failures precede qualification. Capacity attempts do not resize source images;
restore the exact original config and do not claim expansion. Retire only code of two ended owned
probes with hash-exact backups/KEEP_DATA, preserving UID/CE/DE and every data/key source; actual
free903608KiB admits install. Restore original compatible334ca caller5054/975 German/UID10221/
stopped without nonempty schema retirement. Shipping Main99ee/2904/all120 public states and OS
match; four owned emulator XMLs and one physical lease-guard XML are hash-cleaned, owned AVD stopped.
Unused verified recovery copy9232397904bytes, completed build770497442bytes and initial unadmitted
APK58990498bytes are removed; actually used packages/restore APKs remain. The owner-approved second
phone lease is active; no phone input follows. Broader goal/performance/security/stability/
accessibility/platform/provider/Store/release gates remain open.
[Original observations, refusals and restoration](quality/transactions-title-android-os-text.md).


### D-547 - Read-owned expansion buffer, native component admitted

One disposable buffer per original ledger-block read removes repeated raw expansions without a
global pool, cache, schema, key/KDF or financial change. It clears full retired/final capacities
before return/fallback/error/cancellation. Host allocation falls23.524924%;83 selected cases pass
(16 new product candidates,12 diagnostic contracts,55 original). Strict builds and exact complete
shipping Data/Core inputs pass. Two matched whole-Home/native-boundary memory pairs pass at6.228291%/7.010858%,
with opposite order on actual26October after a preserved25October midnight refusal. Do not adopt from host evidence,
change D-545's verdict or mark the broader active goal complete.
[Exact pending admission](quality/read-owned-home-expansion-buffer.md).

### D-548 - Actual safe-area space in the contextual Transactions viewport

A real OS200% selection-plus-Undo scene leaves120dp rather than144 because the original budget
omits the header's actual24dp top offset. Use its arranged bottom, not a hardcoded inset; preserve
all original controls/Back/commands and separate rows. Six focused cases and36 native Undo states
across360/412/800dp, three languages/two themes pass with exact source preservation. Retain one
prior success and continue35 states. Actual-source constraint audit retains36 D-546 normal
scenes without replay. Emulator Main/caller/OS and owned-output cleanup are verified. Full signed
latest package inputs pass; physical adoption awaits the pending original-caller Play Protect restore
and final Main handoff. The independently qualified repository batch proceeds to exact-head CI. No financial Undo, speech/general A11Y, stability/platform or release acceptance is added.
[Actual defect, refusals and restoration](quality/transactions-context-safe-area.md).


### D-549 - Nonempty codec on actual API24 and simulated16KiB runtime

The distinct owned Release/AOT caller preserves every original field in1031 new fictitious rows
per platform, observes original scalar/nonempty-block routes, digest-valid malformed payload
fallback/exact recovery and native Home balance. Native and managed pages both report4096 on
API24 and16384 on simulated API35. Packaged Data/Core exactly match shipping896e;192 own AOT
images/204ELF/static alignment/offline/signature pass. Six native case outcomes add no unit tests
or accepted phone financial replay. Both new packages/fixtures are removed, original package
paths/settings/config match and both AVDs stop. Preserve compile/preboot/UID/parser refusals.
[Original coverage and limits](quality/nonempty-codec-android-compatibility.md). Hardware16KiB,
full-platform, stability/performance/A11Y/iOS/provider/Store/release and pending phone restoration
remain separate.


### D-550 - No-reset complete shipping adoption on retained emulator Main

The full signed offline896e candidate upgrades owned Main99ee once without reset. Original
UID10217/CE/DE/permission declarations and grant flags, all120 original public attributes,
account/zero balance, OS/display/config and compatible334ca caller10221/stopped/CEDE persist.
One original2969 and one new3751 ordinary startup have no selector, financial command, fixture
or successful-cohort replay. Two owned XMLs are hash-cleaned and the owned AVD actually stops,
retaining896e as its current Main baseline. Reuse exact unchanged package/build proof; only a
legacy host UID spelling refusal precedes the first launch. Physical original-caller Play Protect
restore, final phone handback/adoption and all broader gates remain open.
[Adoption and limits](quality/shipping-read-buffer-safe-area-adoption.md).


### D-551 - Final original-caller restoration; physical shipping adoption in progress

The same pending original install now completes, without retry/reset or a changed decision gate.
Original b1d5/UID10381/CE134622/DE135779 starts ordinary onboarding in23985 and is then stopped.
Main remains D542/bc1e/PID4090/Home979 with all141 public attributes and exact original settings;
four newly owned hash-matched XMLs are removed. The phone is explicitly handed to the authorized
other-project turn; issue no further command until its explicit return. Prepare only the bounded
896e shipping no-reset adoption and directly changed Transactions header read for that next turn.
No financial seed/Save/delete/Undo or old native matrix replay belongs to it. Physical adoption
is not yet accepted; retain all broader gates and the active goal.

Standard registered-output Clean succeeds in five configurations after the original arm asset
refusal and proper project-local restore; no build/test runs. All3763 known registered files
(1323292168logical bytes) are absent and all nine used/restore APKs remain hash-exact. Recursive
folder deletion was policy-refused before execution and was not retried equivalently. Two obj
trees still contain1032675493logical bytes of generated caches; do not claim total cleanup or
causal host free-space recovery, and retain the unfinished cleanup boundary.


### D-552 - Withdraw shared captured Home index after native gate failure

One publication-owned unchanged index can serve original forecast/goal algorithms; host allocations
fall30.785900% and composition median36.364312%, with61 selected cases and strict builds/package
proofs. Actual same-day Samsung first pair instead regresses3070.9061 to3086.8201ms and dispatcher
440.5617 to473.4141ms, failing both fixed gates despite lower boundary PSS/RSS. No reverse/retry or
criterion change follows. Restore all five original production files and original tests exactly;
shipping4521 is never installed. Preserve the initial unadmitted own-AOT witness ordering error
and only its necessary repair. Experimental61 cases do not increase current-product test count.
[Complete original evidence and withdrawal](quality/home-captured-index-projection.md).


### D-553 - Qualify original PIN-covered cold goal/current-review content routes

During the physical Play Protect wait, complete only this uncovered combination on owned emulator
API36/x64/4096. One new UID10218 protected fixture/PIN, two actual cached deaths, two native SDK
content taps and two original PIN unlocks preserve full intentionally populated source packets,
original goal100/150/50 and review0/5. Observers do not read finance below the cover. No production
algorithm, permission or SDK changes; no managed-count increase/financial posting/old cohort replay.
Default caller Compile and final192-own-AOT Offline package pass. Preserve both pre-install build/
observer refusals, host observation refusals and the single actual tap lineage. Retire only new
fixture/PIN, verify native-work absence, preserve Main3018/896e/all120/identities/OS/config, remove
nine hash-matched XMLs and stop only owned AVD. Standard Clean removes1668 known files/311179421
logical bytes. Genuine owner authentication, physical-phone and broader gates remain open.
[Original observations and limits](quality/pin-covered-cold-notification-routes.md).


### D-554 - Service the original Brotli runtime to official10.0.14

The new25October OOB security release corrects Brotli APIs used by original Home blocks. Change
only the central runtime/compiler/task pin13 to14; keep SDK/workloads/MAUI/GC/financial algorithms/
schema/permissions and unrelated versions. Verify official MIT/source0427b9, actual both-ABI Mono/
Brotli native files, actual cross-compilers/task revision and Windows self-contained14.31 affected
existing cases run on isolated actual14; strict builds and384/192-own-AOT full packages pass.
New API24/4096 and API35/simulated16384 original codec/fallback/Home cohorts preserve1031 rows
each, retire only fresh fixtures and restore prior packages/settings/config/stop both AVDs.
Preserve metadata/BCL observer refusals; distinct linked BCL bytes are not called identical.
Standard Clean removes3425 known files/1157275260logical bytes, redundant verified archive only;
used APKs and scoped test runtime remain. Explicitly select50f5 for the future single Main upgrade
without rewriting896e historical baseline. Phone Play Protect/restoration/adoption remains open.
No general Mono crash fix, all-platform/security/service deployment or release claim.
[Exact source, native evidence and limits](quality/runtime14-brotli-security-servicing.md).

### D-556 - Service the retained API runtime without changing application bytes

Official ASP.NET14/SDK403 immutable pins replace13/402 in service build inputs. A runtime-only
image preserves every original /app and /probe byte, avoiding deployment of newer source features
or schema. One fresh13-to14 original HTTPS/encrypted-confirmation cohort passes without financial
writes. Preserve lint/observer/one-shot wait refusals; do not replay accepted work. Reproduce the
retained API config hash exactly, validate a fresh private drained snapshot, replace only API
runtime and restart the same proxy. Actual14/healthy/verified HTTPS200, exact five mount sets and
four unchanged other container identities pass. Remove two unused new images; retain live14,
original rollback and private snapshot. Broader security/data/platform/provider/release gates stay
open. [Original authority and finite evidence](quality/service-runtime14-exact-application.md).

### D-557 - Adopt the qualified runtime14 APK on retained ordinary emulator Main

Without rebuilding unchanged inputs, install complete signed50f5 once over896e on retained
API36/x64/4096 Main. UID10217/CEDE/permissions, all120 original Home attributes/zero balance,
five settings/size/density/AVD config and compatible caller334ca/UID10221/stopped state remain
exact; ordinary PID2451 becomes3691. Preserve pre-boot observer/dependent-receipt refusals and
initial15-node Preparing packet; same original2451 reaches ready without relaunch. Remove only
three new hash-matched XMLs, stop owned AVD and prove host/serial absent with shared5038 retained.
No financial write, managed-count increase, successful cohort replay or phone command.
[Original authority and limitations](quality/runtime14-retained-emulator-adoption.md).

### D-558 - Qualify current large Home and protect unchanged-source diagnostics

One finite original API36/x64/4096 runtime14 cohort passes twelve publications in six en/fa/de
reference/tenfold contexts, with complete original financial/raw/native packets and pre-native
byte fingerprint. Actual decoded Core/Data/Brotli and native engines match shipping50f5; preserve
unadmitted metadata/PDB preparation refusals. Six tenfold samples1511-1818ms are emulator-only,
not physical/cold/general stability or a causal runtime gain. No financial/key/seed replay.
An incorrectly selected post-KEEP_DATA schema probe then applies only the two additive metadata
migrations to original reference before rejecting its old control. Retain that failure and changed
cipher bytes. Add current-history admission, generation cleanup and six real SQLite regressions.
Exact owned virtual source repair first proves the authenticated full original control in memory,
then uses the original exclusive protected factory/durable retirement. Preserve its earlier
pre-write lease refusal. Full original schema/headers/typed rows and four existing protected-source
audits pass; no accepted native case repeats. Restore compatible334ca/stopped and shipping2021/
50f5/all120/OS/config, remove three owned XMLs and stop owned AVD. Four scoped Cleans and four
unused APK deletions retain all used artifacts and failure evidence. No broad completion claim.
[Evidence, source correction and limits](quality/runtime14-retained-large-home.md).

### D-559 - Correct store-copy factual boundaries without choosing a release

Correct all six Offline/CloudBackupOnly draft variants: local database versus explicit export/
provider backup, no Zanance account for local bookkeeping, exact approved Android receipt-reading
limits, all six supported languages and historical Windows asset provenance. Twelve assembled
packets fit official30/80/4000 limits; no app/financial/test/build replay or publication. Preserve
review-parser refusals and leave final edition/OAuth/mobile assets/declarations/Store gates open.
[Focused evidence and counts](quality/store-draft-factual-boundaries.md).

### D-555 - Retained Samsung runtime14 adoption completed

The single previously successful no-reset50f5 installation is now observed in original new
PID18619 after the owner returns Zanance. Home979/all141 public attributes, original UID/CEDE/
permissions/OS and stopped b1d5 caller match. Original ready Transactions title/Add identity and
bounds remain top-right, then original Home returns unchanged. Three new hash-matched XMLs
removed; no reinstall/build/test/financial replay. General acceptance gates remain separate.
[Final bounded evidence](quality/physical-runtime14-final-adoption.md).

### D-560 - Owner permits normal foreground restoration and scoped install prompt

Bring installed Zanance forward through its normal Android launcher or normally relaunch when
needed; preserve process/accepted-operation checkpoints and other-project leases. Before further
input verify actual owned UID/window. For an already authorized candidate, select the exact
per-install Play Protect install-without-scanning option when shown and bound to that attempt.
Do not disable global security, grant unrelated permissions or enter real authentication.
No such action or repeated installation was necessary during D-555's completed continuation.
[Authority, observations and limits](quality/physical-runtime14-final-adoption.md).

### D-561 - Preserve current physical native death and resume only incomplete Home contexts

Original runtime14/ARM64 large Home accepts four contexts in28422 before a real SGen-worker
SIGSEGV. An independent exit-only Activity observes actual own-PID/UID OS EXIT_SELF1/status255
and refuses to invent a native tombstone. Bounded allowlist parser has nine focused real-source
cases; no registers/memory or main database export. Exact original cohort APK resumes only two
unfinished contexts in31321, with complete original financial/native/byte guards. Tenfold median
3954.76ms is not two-second acceptance or a causal runtime gain; original crash remains open.
Original caller/bounded empty-Main retirement and four owned XML cleanup pass. Owner-authorized
per-install Don't send resumes the same Play Protect restore without global-security change.
Final Main18994 replaces18619 (OS reason3), with Home979/all141/APK/identity/storage/flags/OS
preserved. No uninterrupted-process or broad stability claim. [Full evidence](quality/physical-runtime14-large-home-and-native-exit.md).

### D-562 - Preserve original writer conflict policy and exact restore comparisons

Actual CI job121422229286 contains83 failures (31 Data/52 App). Reproduce the shared financial
delete/Undo error before fixing it. SQLite can override trigger OR IGNORE with the outer conflict
policy or foreign-key action. Add migration20261026104025 to guard duplicate derived bucket
insertion with NOT EXISTS; retain the revision update, original atomic writer and published old
migration. Require exact corrected trigger definitions in the reader. No entity/compiled-model change.
Portable restore normalizes only its validated local schema cookie; accepted-write workload
comparison normalizes only validated opaque revision tokens in memory and keeps source/payload
comparison exact. Eighteen new managed cases and all83 original failures pass selected local
runs; hosted success is still a separate post-push gate. Strict Windows/complete signed Android
builds have zero warnings/errors. A fresh distinct-package native protected-store cohort performs
one purchase/refund deletion/actual receipt Undo and passes the changed derived-reader boundary;
remove only that new fixture. One complete no-reset upgrade adopts8fd5 in Main7028 with original
Home979/all141/UID/storage/permissions/OS/caller preserved. Retain initial Preparing and host-key
observer refusals without reinstall or accepted-operation replay. General stability, two-second,
accessibility, iOS/provider/Store/release gates remain. [D-562 evidence](quality/ci-ledger-invalidation-and-exact-restore.md)

### D-562 hosted follow-through - original CI green, remaining protected workload correction

The exact original correction head785b151b passes all required CI jobs in run40316474338:
Libraries/tests, Windows and Android are successful; iOS is skipped, not qualified. CodeQL,
identity and retained service workflows also succeed. The one-shot monitor is consumed/deleted.
A separate native-cipher workflow40316473545 fails only its Windows protected workload snapshot
comparison; native reproducibility, protected persistence and official Android slices already pass.
Read only that failed job's log. A new focused compaction case reproduces the same comparison error.
The physical protected-file copy can leave a stale derived schema cookie until the first accepted
writer refreshes it. Keep every row, schema and compressed payload exact; normalize that one
cached cookie only when before/after independently captured actual protected-source cookies are
unchanged and the resulting cache cookie matches them. Missing evidence, changed source cookie,
wrong cache cookie and changed payload are refused. Five new cases plus the11 affected original
oracle cases pass16/16 on actual runtime14; strict original workload entrypoint build passes.
This adds five cases to D-562's original18; it is not a new full-suite or native financial run.
No production DLL, cipher, APK, UI, permission, financial algorithm or successful phone cohort
changes/repeats in this follow-through. Final head5e2e9384 passes CI40319587370, native candidates40319587614 (including the actual protected workload step), and CodeQL40319587303; no second polling request is made.
[Required CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40316474338),
[preserved native workload failure](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40316473545).

[Full bounded evidence](quality/ci-ledger-invalidation-and-exact-restore.md).


### D-563 - Bounded independent native fault observation

Add an opt-in independent-package observer for reviewed module-relative instruction locations,
with original signal chaining, bounded private records and mandatory independent fatal correlation.
Fifteen new actual-reader cases and fresh refusal/recovery/fatal/concurrency controls on x64 and
ARM64 pass. One changed runtime14/x64/API36/4096 retained-source cohort arms successfully and
preserves all six native contexts/twelve publications, original encrypted sources and complete
financial presentation without seed/key/financial replay. The actual named record is empty and
the process lives: no random crash correction or general stability acceptance is claimed.
The original-code handback guard refuses nonempty independent Main before schema retirement;
keep the compatible new caller stopped with original UID/storage/permissions and old APK backup.
Ordinary emulator Main3035/50f5 and phone Main7028/8fd5 are preserved. Standard owned build cleanup
and exact-head CI are separate recorded gates. Shipping code/APK, permissions, UI and GC settings
are unchanged. QA-06, phone two-second, accessibility/platform/security/iOS/provider/Store/release
remain open. [Full scope and actual failures](quality/bounded-native-fault-code-observation.md).


### D-564 - Ordinary emulator ledger-guard adoption and owned code cleanup

Accept existing signed shipping8fd5 once without reset on ordinary emulator Main3035to6123,
with original all120 public attributes/zero balance, UID/CE/DE/permissions, size/density and five
settings exact. Preserve actual storage refusals. Retire only the stopped finished Billing
recovery caller's unused code after hash/certificate-verified APK backup, retaining its UID/data/
keys/stored grants; do not touch the live historical snapshot caller. Restore compatible D563
ebd9 stopped with original UID/storage/permissions, and hash-clean two new XMLs. The overly broad
uninstalled-permission-declaration observer refusal is preserved and corrected without reinstall.
No phone, private Main database/key, financial command, old successful suite or build is repeated.
No AVD resize, data clear or global cache purge occurs. This completes the distinct ordinary
emulator changed-APK gate; QA-06/A11Y/platform/iOS/provider/Store/release remain open.
[Full boundary and failures](quality/ordinary-emulator-ledger-guard-adoption.md).


### D-565 - Physical ARM64 bounded observer cohort and original handback

Qualify the previously unperformed managed ARM64 observer boundary with one original retained-source
cohort, no new financial/preparation/key commands. Strict complete signed dadba5/192AOT/205ELF and
actual financial/runtime identity pass; all6 native contexts/12 publications and full source/native
presentation guards pass in5178. The named record is empty and that process lives: random SGen
crash/root cause remains unproved. Tenfold median3512.0725ms still exceeds the two-second target.
Only the validated empty independent Main retires M1/M2/M3; original b1d5/UID/storage/permissions
return through the same installer with the owner-authorized scoped Don't send option. Preserve
German-onboarding observer refusal and restore English once in the same7667 without Next/financial
work. Main7028/Home979/all141/both APKs/OS exactly return,5 XMLs hash-clean, and phone is handed back.
One standard clean removes11586 files/637786410 logical bytes;4 unused unsigned copies remove
117540593 bytes, used packages/proofs retained. D563 one-shot CodeQL passes/CI cancels; D564 CI and
CodeQL pass, both schedules consumed/deleted. No old managed/native/financial success is replayed.
QA-06/A11Y/security/platform/iOS/provider/Store/release remain open.
[Full bounded evidence](quality/physical-bounded-fault-observer.md).


### D-566 - Original receipt-source Help at actual OS200% and bounded startup diagnosis

Qualify original native Help/OK/Cancel in en/fa/de, Light/Dark, exact360/412/680dp on API36/x64/4096.
Preserve18 original320/412/640 scenes; continue only12 missing exact widths and skip all6 accepted412.
All18 required exact contexts pass final-line reachability, original44dp actions and Persian RTL,
with zero source/camera/file/financial commands. Correct only the observer's29px coordinate mismatch.
The original covered startup failure is actual insufficient space below the unchanged553648128-byte
protected guard; after backed-up finished-code-only KEEP_DATA retirement, one same-process Retry
passes. Optional content-free independent startup diagnostics are compiler-elided from shipping;
five new managed cases and two affected existing cases pass. Default Windows/Android strict and
complete signed independent f288bc/192AOT/204ELF pass with actual financial/native identity to8fd5.
Restore font/display/five settings, Main2902/all120/UID/storage/grants/8fd5 and stopped ebd9 exactly;
remove only the fresh fixture,7 hash-matched XMLs, shut down only the owned AVD. Standard clean
removes24947 files/2306666041 logical bytes;8 unused copies remove238366227 bytes, used evidence kept.
D565 CI/CodeQL are green and its schedule consumed/deleted. Spoken/physical A11Y, QA-06, speed,
security/platform/iOS/provider/Store/release remain open; no old financial success is replayed.
[Full boundaries and preserved failures](quality/receipt-source-os-text-and-covered-startup.md).


### D-569 - Preserve Java resources during incremental Android APK packaging

Reproduce the installed36.1.69 BuildArchive defect directly: unchanged JAR resources/services
are present on the first call and missing on the second. Refresh only the validated generated
intermediate application APK before packaging; key on AndroidApplication, since the real SDK
changes application OutputType to Library. Preserve the initial complete-app admission refusal.
Three guarded resource/changed-and-removed-input cases, nine authority cases and one redirected
path refusal pass. Strict Windows and corrected complete offline Android builds have zero
warnings/errors;384 own AOT/408 ELF, static16KiB/ZIP/v2/v3 and actual native/financial identity
checks pass. All1422 Java resource bytes match the retained healthy role, including both coroutine
service descriptors. No application unit-test count, financial action, phone/emulator operation,
visible UI or general crash correction is claimed. D568 native timing/adoption and the D567 real
reboot/idle cohort remain unfinished; Loyan retains the physical lease. Preserve the broad goal.
[Full defect, correction, refusals and acceptance boundaries](quality/android-archive-resource-retention.md).


### D-571 - Preserve the complete permitted StarMoney field at CSV syntax boundaries

Correct original preflight counting so separators and CSV quote syntax do not consume the existing
4096-character decoded-field allowance. Preserve all original size/row/column bounds, strict syntax,
source identity and explicit financial review. Five corrected boundary shapes fail before the fix;
all10 boundary cases, two original non-UI application/real-SQLite review flows and five affected
existing structure refusals pass on runtime14. These are12 new cases plus five existing cases;
incorrect initial exception-type assumptions are retained separately and are not product failures.
Strict Windows/complete offline Android builds have zero warnings/errors;384 own AOT/408 ELF,
static16KiB/ZIP/v2/v3 and1422 exact Java resources pass. No Import/Undo, financial/native replay,
phone command or D567 emulator input occurs. The combined review APK contains unfinished D568;
refresh its matched financial inputs before physical comparison after explicit Loyan handback.
Read only the historical D561 failed job: all83 cases/35 families exactly match the already fixed
D562 packet; preserved selected passes and hosted green evidence avoid an unnecessary rerun.
No current-head CI success is inferred. Three original bank sources clarify remaining contract
admission, without a new preset, authentication, provider activation or support claim. The broad
goal and D567/D568, native adoption, stability/speed/A11Y/platform/iOS/Store/release gates stay open.
[Full corrected baseline, checks and source-admission limits](quality/starmoney-decoded-field-boundary.md).


### D-568 - Withdraw the prepared Home publication after failed physical gates

After explicit authorized Loyan handback, preserve the fresh owned Main3130 baseline and original
retained100000-row source. Same-day original/candidate ARM64 processes5215/6816 use separate completed
scalar controls4471/5980 and complete source/native/financial/byte guards. Original3015.6854ms becomes
3284.7178ms (-8.9211% gain); dispatcher441.1000 becomes474.6848ms, failing the1.05-relative gate.
PSS/RSS growth14397/14200KiB stays inside65536, but both main gates fail. No opposite-order/rescue
pair or candidate Main adoption occurs. Restore six exact original files; remove only four new
unpublished source/test files with private copies retained. Thirteen experimental cases are
withdrawn with the API and add0 final shipping tests; successful financial/native suites are not replayed.
Retire only validated empty independent Main bookkeeping M1/M2/M3 with full original source proof,
restore b1d5 in the same owned installer using authorized Don't send, and verify original61-node
English startup before stopping that caller. Strict restored Windows/full Android have0 warnings/
errors; complete signed4a6cb5be/384AOT/408ELF/1422 resources passes offline/ZIP/v2/v3/static16KiB.
Main upgrades once without reset3130to9856, with exact UID/CE/DE/permissions, all141 Home attributes/
Home979 and all three original profile-page states. CSV D571 stays; no clock/system setting/financial
command is changed. Pair26October and final adoption27October are explicit.15 owned XMLs hash-clean;
12769 final-build outputs/1098816790 logical bytes and two unused copies/97644359bytes are removed.
A prior574552517-byte residual deletion remains automatically rejected/unperformed; no bypass.
D567's real idle/reboot cohort is untouched. Whole-Home two seconds, general stability/audible/device/
iOS/provider/Store/release gates remain open. This closes the experiment, not the overall goal.
[Exact failed gates, preserved refusals and restored delivery](quality/home-prepared-publication.md).


### D-572 - Scope the first original physical reader gesture cohort

Use a distinct Samsung/API36/ARM64/current-Main-UID/APK-bound service; preserve the old emulator-only
helper and D567 idle/reboot cohort.24 new actual-policy AT-572 cases and strict complete offline
37AOT/47ELF/v3 packaging pass. With direct contextual owner authority and finite restoration,
English/Persian/German actual accessibility focus opens the original expense form and actual Cancel
returns Home, three opens/three Cancels and zero financial/profile/seed/key commands. Continue only
unfinished Cancels after preserved IME/host-observer refusals; no accepted activation is replayed.
Bounded local playback is retained without upload/transcription or claimed intelligibility.
Restore original system-setting values and absent-key presence on every connection; production
APK/runtime/source, original caller and financial data stay unchanged. Remaining spoken/field/
scroll/theme/width/full-workflow, stability/speed/platform/provider/Store/release gates stay open.
[Complete scope, refusals and actual evidence](quality/physical-reader-scope.md).


### D-573 - Local acoustic evidence without replaying accepted native gestures

Decode the five existing D572 playback WAVs locally with explicit original model paths and no
expected-caption grammar/prompt; verify original hashes and retain full hypotheses privately.
English expense/Cancel and German expense/Cancel/amount terms occur; Persian expected terms do
not. Only the two unresolved Persian clips receive a distinct English diagnostic pass, finding
role words without proving a cause. Three zero-PCM controls are empty. No transcript upload,
phone/emulator input, new voice/engine, application SDK/build/test/financial/gesture replay occurs.
This is machine acoustic evidence, not human intelligibility, pronunciation or complete A11Y.
Owner listening to the existing Persian clip remains pending while independent work continues.
The canonical A11Y-01 row now records D572's actual current-candidate native qualification.
Four redundant archives/155134714 logical bytes remain: automatic review rejects removal before
launch with no detailed reason; no alternate deletion or recovered-space claim follows. Used
runtime/models/notices, WAVs and proofs remain. D567 and the broader goal/gates stay active.
[Exact evidence, unresolved Persian boundary and refusal](quality/local-reader-acoustic-evidence.md).

### D-575 - Preserve omitted optional restore preferences

Correct the reproduced original protected-restore rejection of a package without optional
`display-settings.json`. Explicit nullable memory preserves missing choices; present empty/
malformed JSON still refuses, explicit null affects only allowlisted preferences, and device
security is excluded. Four new AT-575 behaviors pass across selected runs: one genuine failure
is corrected and only that failed case is rerun; the other three successful cases are retained.
Original native restore returns once after proving no earlier restore intent or accepted data;
complete copied rows match. Remaining observations preserve the original accepted restore and
all prepared source/cookie guards. Strict Windows/full offline Android have zero warnings/errors;
384 own AOT/408 ELF,1422 exact resources, ZIP/v2/v3/static16KiB and actual native/managed identity
are checked. Main receives the complete signed no-reset upgrade with all141 public Home
attributes/Home979, UID/CE/DE/permissions and exact system settings preserved. No ledger Save,
Import, Undo or old financial/seed sequence is replayed.

Generated project-relative output is excluded from source discovery after its removal was
rejected before execution;449 residual files/118359992 logical bytes remain, without bypass
or freed-space claim. D574's two SDK-native PID0 cold refusals and original prepared profile
remain retained; no accepted cold timing/two-second result is claimed. D567, spoken acceptance,
general stability/security, iOS/provider/Store/release and the broad goal remain open.
[Full correction, original native outcome and remaining boundaries](quality/optional-restore-preferences.md).


### D-574 - Original loaded cold boundary on retained Samsung sources

Completed the finite reference/tenfold cold cohort without repeating the accepted
source restores or financial/seed commands. The SDK's PID0 records remain refused.
A separately bound one-shot native shell-to-ready interval, including dispatch
overhead, measured8062.511917ms for10,000 entries and17756.488064ms for100,000;
one observation each, not OS-launch metrics, repeated medians or a two-second pass.
All prepared typed rows, financial/read/final cache cookies and original presentation
matched. The tenfold caller's mistaken old-reference presentation was preserved,
then corrected through an independent read-only receipt; no baseline was overwritten.
An ordinary covered startup failure required one native Retry in the same18144;
its unknown cause and the original refusal remain, not a general stability fix.

Five new provenance tests, the prior six lifetime/seven corrected storage cases,
strict caller builds and complete signed offline packages retain their distinct
evidence. Only two fresh profiles were deleted; original independent Main/source
and keys verified, necessary M1/M2/M3 bookkeeping retired before b1d5 restoration.
Main623b/11430/Home979/all141/UID/CEDE/permissions/settings preserved. No D567
emulator action, unrelated test/financial replay or production-schema downgrade.
General QA-06/two-second, ANR/crash, A11Y, platform/provider/Store/release remain open.

[Full source, failure, clock and retirement evidence](quality/loaded-cold-command-boundary.md).


### D-576 - Keep experimental CoreCLR feasibility isolated from product delivery

Review the official experimental .NET10 Android runtime only in a new ARM64 package/UID,
with matching signed runtime/compiler14 and explicit research-only SDK opt-in, not a
warning suppression or production runtime swap. Source observation is shell/DUMP-bound,
requires original ready/unlocked focused storage and an empty initial seal, and exports
only a bounded fictitious packet plus complete logical typed-source digest. It never
prepares or writes finance. Six fresh policy cases pass; original account/opening and
four money forms run once each, six native language/theme contexts retain the same data,
and actual cached/adj900 process death recovers it through the ordinary launcher.
Preserve the observer JNI failure and fix its refusal boundary rather than replaying
an accepted operation. Reject incorrect build/runtime/input observations honestly.
Retire only the fresh package after source acceptance, keep all used evidence, restore
Main/caller/OS and clean only owned outputs. This does not diagnose or fix general SGen,
qualify a production CoreCLR move, or close stability/performance/A11Y/release gates.
[Complete evidence](quality/coreclr-original-ui-feasibility.md).

### D-577 - Refresh the original Accounts list on retained Shell activation

The same accepted D576 source exposed an actual original UI defect: Accounts remained
97/0/97 while four accepted ledger rows and Home had81/20/101. Its loader relied solely
on OnAppearing, which Android need not invoke on a retained tab. Add Accounts to the
existing guarded native Shell dispatch and share the original outstanding load with
appearance. No calculation, financial writer, source or visual layout is replaced.
Reuse the accepted rows; complete logical protected-source digests and six actual
language/theme contexts match. Strict Windows/full offline Android pass; final b577
preserves actual financial/compression PE and native bytes,1422 resources,384 AOT/408 ELF.
Adopt the Mono product once without reset, preserving original public Home979/141 nodes,
UID/CEDE/permissions/settings and b1d5 caller. Actual Mono tab return is verified; retain
one native container drawing-order delta and observer refusals without calling all final
attributes identical or claiming private Main database equality. Runtime/CoreCLR and
broader release qualifications remain separate; D567/the overall goal stay active.
[Complete correction, package and native boundary](quality/retained-account-overview-navigation.md).

### D-578 - Announce original entry-category selection

Reproduce the original selected Food caption missing its state on production Mono.
Use the existing six-language `Common_ChipSelected` format in a dependent semantic
name; notify it from original selection and bind only the overlay description.
No visible design, command, permission, source or financial calculation changes.
Strict Windows/full signed offline Android pass with zero warnings/errors; actual
Core/Data/compression/native engines and1422 resources match installed b577.
Adopt510a3197 once without reset. Six original en/fa/de x Light/Dark expense drafts
qualify24 native select/exclusive-switch/deselect states, with no amount or Save.
Restore English/Light, Main22604/Home979/all141 public attributes, original settings
and caller metadata; preserve covered-frame/caller-process/observer refusals and
the actual cached900 caller retirement. No general spoken/stability/release claim.
The D577 exact-head CI and CodeQL each succeeded once; its schedule was removed.
[Complete category correction and verification boundary](quality/category-selection-semantics.md).

### D-579 - Original category state through the actual installed reader

The separate helper is freshly bound to signed Main510a3197/97,058,719 bytes and
original UID10380; production source/APK/runtime are unchanged. Eleven new actual
policy/resource cases pass in10+1 selected runs; old successful suites are retained.
Original English reader focus/gestures open one empty expense form, select Food,
select Housing, observe settled exclusivity, deselect Housing and Cancel without
financial Save. Preserve the old-length guard refusal, late IME, intermediate
two-selected native snapshot and failed initial recording; no accepted input is
replayed and no exclusive cache/product cause is asserted. Two complete local
recordings retain native provenance; one unconstrained post-decode observation
finds Housing/selected, without human or general spoken acceptance. All14 original
settings/presence and Main22604/Home979/all141 public attributes/caller are exact.
Only fresh UID10400 is retired;956 registered files and3 owned XMLs are cleaned.
Additional residual/loose deletions are rejected before execution and not retried.
D567, broader accessibility/stability/platform/external release gates and the goal
remain open. Native observations add no managed cases beyond the11 stated here.
[Complete finite reader cohort and limits](quality/original-category-reader-states.md).

### D-580 - Preserve distinct queued Android reminders

Original D567 history proves Android canceled a queued goal with
mBroadcastConsumerSkipAndCanceled: the SDK applied Activity SingleTop's numeric
replace-pending bit to broadcasts. Keep it only in the Activity branch. Build the
original MIT Core from a SHA-pinned official archive plus one reviewed source
replacement, preserving public assembly identity and Android-only scope. Actual
four-request baseline/correction comparison is two delivered/two skipped versus
four delivered/zero skipped, with exact financial/SDK Main PE inputs. Five new
preparation guards and strict Windows/full offline Android pass; adopt f7488d1d
once without reset, preserving Main21746/Home979/all141 public attributes, UID/CEDE,
permissions and14 settings. Retain original D567 one-hour failure, full source match
and terminal fixture retirement; its continuation schedule is deleted after exact
restoration and owned AVD stop. No punctuality/Doze/general stability claim. Phone
is explicitly handed to Dev Loyan1 until return; the overall goal remains active.
[Complete source, native evidence and limits](quality/reboot-paired-delivery-diagnosis.md).

### D-581 - Mandatory notification source admission under Python optimization

Two actual optimized-subprocess regressions first admit changed replacement/cache
inputs and fail. Replace removable Python assertions with explicit ValueError
rejection for every manifest/license/archive/path/source/cache check. All seven
related source guards pass (two new, five affected existing); one actual strict
Core/MSBuild preparation under PYTHONOPTIMIZE=1 passes without warnings/errors.
All SDK compilation bytes and used signed f7488d1d APK remain exact D580. Reuse
its full Windows/Android/native/adoption results; no app change, new package,
financial/native replay or phone/emulator command. Loyan retains the phone lease;
overall qualification and external gates/goal remain open.
[Complete reproduced defect and focused evidence](quality/notification-source-admission.md).

### D-582 - Retained ordinary API36 adoption of corrected notification Core

Reuse the verified complete signed f7488d1d product without rebuilding. On the
retained API36/x64/4096 ordinary Main, one no-reset upgrade preserves UID10217/
CE352809/DE82676/permission flags, all120 public Home attributes/zero balance,
caller ebd910c2 metadata/process absence, five settings and320x640/density160.
Actual Main3080 becomes3656; no historical PID equality, private database/key
comparison, financial command or new reminder is claimed. Two owned XMLs are
hash-cleaned and only emulator5570 is stopped, with original config hashes exact.
Current emulator Main is now f7488d1d; never downgrade to historical8fd5. Phone
stays with Dev Loyan1 until explicit return. No new managed tests, product builds
or financial/native-success replay; broad qualification/external gates stay open.
[Complete ordinary adoption and limits](quality/retained-notification-core-adoption.md).

### D-583 - Current corrected product bootstrap on isolated compatibility systems

Reuse complete signed f7488d1d without rebuilding for two fresh original-app
bootstrap cohorts. API24/kernel4096 UID10062/PID2745 reaches original Welcome/
Step1 with51 nodes; DE inode is unavailable, not invented. API35/userspace16384/
kernel4096 UID10207/PID3993 reaches original Welcome and exposes all six language
names after one owned body scroll; initial offscreen names are not a proven defect.
No language/Next/restore/profile/financial action or SDK request is sent. Preserve
metadata/tool/focus/label observer refusals without install or launch replay.
Both fresh Main fixtures are retired, original package absence/five settings/size/
density/config exact, four owned XMLs hash-cleaned and both owned AVDs stopped.
No new managed cases or full product build; nonempty data/full onboarding/hardware
16KiB/punctuality/stability/A11Y/iOS/external release gates and goal remain open.
Phone remains exclusive to Dev Loyan1 until explicit return.
[Complete current-product bootstrap and limits](quality/corrected-core-platform-bootstrap.md).

### D-584 - Unused prototype standard cleanup and explicit retirement refusal

Five standard cleans remove all411 registered files/7868802 logical bytes
from exact unused notification prototype project roots, with zero ownership
outliers/registered remnants. Used signed f748 product, live pinned archive and
local native/failure proofs remain exact. Additional guarded retirement of the
remaining E NotificationCore root is rejected before process creation with only
blocked by policy; no deletion or planned backup/manifest occurs from that
command, and no alternate deletion or old refused scope is attempted. Additional
retirement remains incomplete. No build/test/device/financial/Docker replay.
Fresh official-source review finds no newly released MAUI fix beyond current
10.0.120; open upstream investigations do not identify our native cause. Keep
all qualification/external gates and goal open; Loyan retains the phone lease.
[Completed cleanup, refusal and source boundaries](quality/notification-prototype-cleanup.md).

### D-585 - Current-candidate localized reader states and correlated refusals

The scoped physical reader is rebound to the exact current f748 product after
explicit phone return. Three distinct new policy cases pass in selected runs;
preserve the actual candidate regression, wrong runner option and synthetic
request-length mistake separately. Scoped strict helper builds and signed offline
packages pass; Main is not rebuilt/reinstalled and no accepted financial command
is replayed. Fixed refusal codes retain canonical request lineage without arbitrary
exception text, verified natively by a stale-window refusal before any gesture.
German/Light initial/selected/exclusive/deselected/Cancel states pass. Preserve the
Persian intermediate selection loss and generic refusal; the remaining retained
draft and later focused exclusive-transition diagnostic have separate receipts.
The later Cancel opens the expected dirty-draft dialog; preserve the wrong host
Home expectation and use only original owned Discard for cleanup, not reader
confirmation acceptance. The owner's historical recording feedback is English,
not Persian; textual legibility does not accept speech. Original English/Light,
Main/Home/settings identity, helper retirement and cleanup have independent proofs.
General speech/pronunciation and the unexplained earlier loss, stability/speed,
iOS/provider/Store/public release gates and the overall goal remain open.
[Complete native evidence, failures and limits](quality/current-candidate-localized-reader.md).

### D-586 - Distinguish public speech metadata from the actual Samsung reader

An independent no-permission/no-Activity/no-service signed package performs one
metadata request without voice selection, synthesis or financial commands. Public
Google reports473 voices, Persian unsupported/no Persian entry and English default;
actual approved TalkBackPID13984 instead binds private Samsung SMT. Do not confuse
these engine routes. The owner authorizes read-only system speech-language pages:
Samsung3.5.02.16 has14 currently exposed language/region entries, no Persian, and
Use system language selected with actual en-US. No language/voice/engine or download
entry is selected. Preserve API36 deprecation and copied-verifier refusals, plus
an empty-service-list restoration error corrected without reader reactivation.
Opening system settings normalizes an absent default-engine key; restore original
absence rather than claiming no side effect. Main18235/f748/Home979/all141/UIDCEDE/
permissions/EnglishLight, all14 values/presence and original caller are exact.
Only fresh inventoryUID10402 is retired. Standard Clean removes70 registered files/
12845517 logical bytes and nine hash-matched XMLs/262149 bytes. Additional retirement
of the new residual root is rejected before execution;88 files/5006367 bytes remain,
with no alternate deletion. No new managed-test count, shipping rebuild or financial
replay. Persian speech needs a supported engine/voice and separate actual-reader
qualification; current permission is read-only, not installation/selection approval.
General A11Y/stability/speed/iOS/provider/Store/public release and the goal stay open.
[Complete engine-route evidence, cleanup and limits](quality/installed-speech-engine-metadata.md).

### D-587 - Close the owner-cancelled legacy Persian engine candidate

Review the standalone F-Droid1.52.0/code22 APK and source against the pinned signed
repository index: exact digest/size/signer, no requested permissions, ARM64 and
bundled Persian data, minimum21/target33. Initial owner approval permits one
bounded temporary trial, but Android blocks the old target with Unsafe app blocked.
The owner declines Install anyway and requests a newer compatible option. The
original attempt is already terminal with VERIFICATION_FAILURE when cancellation
input is guarded; no stale tap, override, second install, engine/language choice,
synthesis or financial command occurs, and the package remains absent.
Preserve late Samsung default-key normalization rather than rewriting the baseline;
restore original absence with bounded stable observations and exact14-value/presence
checks. Actual Main22461 existed before the install attempt; OS metadata records
old18235 LOW_MEMORY/importance400/status0/trace null. Preserve this selected public
state recovery without claiming PID survival, crash cause or full DB/key equality.
Main f748/UIDCEDE/permissions/Home979/all141/EnglishLight and original caller are
exact. Three hash-matched remote XMLs/68374 logical bytes are removed; used failed
APK/source/provenance and local receipts remain. Host-only recorder review finds
three110-second captures terminated by a12-second final tail before their natural
limit; no audio is repaired, regenerated or replayed to fake acceptance.
Newer compatible review is authorized, but another install/selection needs fresh
approval. Persian speech and broader product/external gates stay open.
[Complete provenance, rejection and preservation](quality/legacy-persian-engine-candidate.md).

### D-588 - Correct the stale current-reader CI expectation

Both final D585/D586 hosted CI runs fail only the older current-category-candidate
method; native Windows/Android jobs and CodeQL pass. Preserve5576 total/one failure/
25 skipped, and delete the two consumed one-shot schedules. Current policy correctly
requires f748 while the old test expects510a. Reproduce only that exact method on
latest ef9d03bc, then update the independent current literal and explicitly reject
both prior510a/4a6c digests. One selected existing case changes from red to green;
no new case, policy relaxation, shipping change, financial test or successful native
build replay. Strict affected test-project build passes. Standard Clean removes393
registered outputs/164750580 logical bytes with no ownership outlier/remnant.
The single delayed exact-head check for a8c5db09 reports CI40538715325 and
CodeQL40538715374 completed/success; preserve the21.20-minute commit-to-observation
delay, consumed receipt and deleted schedule. Initial host DateTime-kind arithmetic
is corrected separately without changing the consumed marker or repeating GET.
The stale-test CI failure is closed; broader product/external gates remain open.
[Complete hosted cause and focused repair](quality/current-reader-ci-regression.md).


### D-589 - Review a genuine newer speech source without retrying the blocked APK

Read the actual Samsung download catalogue to a stable bottom:39 language/region
captions, no Persian/Farsi; no download/voice/engine selection. Return normally to
Main22461/f748/Home979/all141 exact public attributes, original14 values/presence
and caller state; preserve transient-focus/list/encoding/cleanup refusals without
replaying successful input. Ten owned hash-matched XMLs/310990 bytes are removed.
Official stable eSpeak remains1.52.0; pinned upstream ba90c8e9 has1.53.0/code23/
target36 and bundled Persian data, but no completed reviewed binary or runtime
acceptance. Inspect original source, manifest, extractor and official build metadata;
no source execution, build, install, voice selection, reader/financial/test replay.
Isolated SDK terms/scope await the owner; the prior legacy cancellation remains
binding. Persian speech and broader delivery gates stay open.
[Complete newer-source review and remaining approval](quality/newer-persian-speech-source-review.md).


### D-590 - Finish the remaining German capture without replaying accepted actions

Correct the actual host recorder-budget failure using monotonic start, the explicit
40-second natural limit and25-second finite margin under original window/watchdog
guards. One original German Home-title exploration gains actual reader focus;
there is no activation/form/category/financial replay or engine/voice choice.
The recorder exits0 naturally after43.3333 host seconds; original PCM16/stereo48k
WAV7577712 bytes has exact finite RIFF/chunks and39.4667 PCM seconds. The owner explicitly
confirms this exact title sound is German and understandable; this is not inferred
from focus, container validity or visible text and does not accept other utterances.
Main22461/f748/Home979/all141/UIDCEDE/permissions/EnglishLight, original14 settings/
presence and caller are exact; fresh helper10405 is removed once and16 owned XMLs/
675159 logical bytes hash-cleaned. No shipping rebuild or old successful test replay.
Persian and wider product/external gates remain open.
[Complete original recording and restoration](quality/natural-german-reader-recording.md).


### D-591 - Accept only the directly heard German title and retain the Persian gap

The owner explicitly confirms the original D590 sound is German and understandable;
bind acceptance to WAVf061e241/7577712 bytes and only Neue Ausgabe. Preserve the
negative historical Persian listening result and broader speech gates. Host-only
inspection of17 existing Persian packets shows no Housing activation, an
uncorrelated refusal and Food-selected focus records5523/8575ms after Housing focus.
This does not prove immediate clearing or the later managed-state/cached-caption
cause. Original toggle/theme/kind semantics stay unchanged; no speculative fix,
new Android command, gesture, recording, test/build or financial replay occurs.
[Direct acceptance and original packet limits](quality/direct-german-listening-and-fa-lineage.md).


### D-592 - Reject tag-list reuse before native work when the host gate fails

Freeze/link actual original codec/domain sources in two fresh isolated host roles;
only the private candidate reuses the entry's already-owned tag list. Strict builds
and full original-field/order/null/duplicate/mutability guards pass on actual14/X64.
One paired empty/mixed comparison allocates5047904/5383776 bytes for reference and
4785760/5159520 for candidate:combined4.6627% fails the predeclared5% host gate.
Close it without production change, app packaging, native/financial/seed/key input
or successful unrelated test replay. Shipping/model/migration inputs stay exact.
Standard Clean removes28files/593170 logical bytes; further residual-file removal
is rejected before process creation with blocked by policy,20files/422324bytes
remain and no alternate deletion runs. Complete Home speed and broader gates stay
open; no host result is a native/two-second acceptance.
[Complete host eligibility result and cleanup boundary](quality/rejected-tag-list-allocation.md).


### D-593 - Bound the current read-only primary-tab reader cohort

Add dedicated primary-tab operations and copied native Selected metadata only to
the independent helper. Full five-peer/native-role/viewport/unique-selection guards
reject financial-caption aliases; preserve the Italian Cancel/Undo collision and
block ambiguous legacy captions.17 distinct new selected cases plus six affected
existing category-resource cases pass. First native peer-clickability assumption
refuses before a tab gesture; reproduce its failure, allow only the selected peer's
missing redundant action, upgrade the fresh helper once and continue only the four
unperformed Transactions/Plans/Insights/Home steps. Actual focus/selected/title
observations pass; the accepted initial negative control is not replayed or rebound.
Natural original recording exits0; human acceptance of the new clip is not claimed.
Owner English Housing/selected acceptance applies only to historical clip8fed9094.
Main22461/f748/Home979/UIDCEDE/permissions/EnglishLight, original14 settings/presence
and caller match; strict141-node comparison preserves one drawing-order1-to2 delta,
not byte equality. Retire only fresh10406, hash-clean3XML/155778bytes and standard
Clean767files/220472438logical bytes. Extra build-root removal is rejected before
execution;422files/18765916bytes remain without alternate deletion. Product code,
financial writes/keys/voice choices and unrelated successful tests are not changed.
[Complete current cohort, refusals and preservation](quality/read-only-primary-tab-reader.md).


### D-594 - Put further TalkBack last and park Persian speech preparation

The owner considers current TalkBack testing sufficient for now. Finish only the
active cohort's terminal restoration, owned cleanup and documentation. Further
TalkBack tests/recordings/listening requests follow all other available backlog
work; Persian spoken TalkBack has no current priority given no present Iran rollout
plan and limited anticipated Persian-language usage in Europe. Park the proposed
alternate-engine/SDK work without assuming approval or chasing its question. Keep
existing Persian UI/features and completed evidence; this is an execution-order
deferral, not full accessibility/release acceptance or a change to jurisdictions,
financial behavior, accounts or publishing. Other work and the overall goal continue.


### D-595 - Reconcile completed subgates before scheduling new qualification

The canonical A11Y-03 summary omitted D-566 even though its original receipt-source
Help/OK/Cancel cohort already completed18 exact OS200% contexts with native text,
44dp actions, RTL and scroll reachability. Restore that evidence reference rather
than repeating the matrix. Separate the D-580 paired queued-delivery correction and
D-553 covered cold routes from still-open physical punctuality/authentication; keep
failed timed windows explicit. Reconcile the stale enhancement overview: D-292 is
locally qualified, while real public/provider acceptance remains open. Add concrete
next-admission boundaries for receipts, security and performance to the canonical
backlog, without marking their parent gates complete or treating hour estimates as
new execution authority. D-594 remains in force; no TalkBack/native/financial action,
product change, new managed case or repeated successful build is needed for this
source-evidence correction. Verify the exact linked reports and document diff.


### D-596 - Retire camera originals left by an interrupted capture

Two original-filesystem regressions reproduce a gap in startup/data-deletion cleanup:
raw GUID JPEG camera outputs survive when process termination skips `finally`.
The actual shared cleanup now retires only that adapter's top-level opaque JPEGs,
without following links or deleting unrelated/nested/root files. Four new focused
cases pass after the two initial failures. Original Android startup in a fresh UID
removes one staged synthetic original after proven cached-process death; no actual
camera/OCR/financial scenario is replayed. Strict Windows/full signed Android pass.
One no-reset emulator upgrade preserves all120 public tuples; one Samsung upgrade
preserves Home979/141 public captions, geometry and states/UIDCEDE/permissions and
14 settings/presence, with an explicit ScrollView drawing-order2-to1 delta, not full
attribute-byte equality. Used signed885f6df5 and independent7781748a remain;4906 standard
Clean outputs/1523573710bytes and four unused copies/157450351bytes are removed.
Further eight-root retirement is rejected before execution with blocked by policy;
4030files/688445756bytes remain without an alternate attempt. Camera quality, wider
platform/release gates and the full goal remain open; D-594 scheduling is unchanged.
[Original cleanup, native preservation and exact limits](quality/camera-original-cache-retirement.md).


### D-597 - Autonomous emulator work and original camera-result qualification

The owner explicitly authorizes necessary project work in the Zanance-owned emulator
without repeated permission, and necessary low-risk real-phone actions autonomously;
high-risk phone actions still require the owner. Preserve other leases, data and
accepted checkpoints. The original receipt source/camera/provider/preparation/reader
adapters complete one real Camera2 cancellation and one shutter/Done on an owned
synthetic image in the same2222/UID10218. Native results0/-1 and cache-zero outcomes
are preserved. Found14.9/TryMinor(EUR)1490 is qualified without any financial command;
header/footer text loss remains explicit, not whole-camera/OCR acceptance. Reuse the
prepared signedd8b63158 once after approval, without reinstall or old scene replay.
Retire only the fresh helper; original Main3042/885f6df5/all120 public attributes,
UIDCEDE/permissions, five settings/config and caller match; the phone is untouched.
No default app change/build or new managed count: three build-admission checks and
two new native cases are distinct.1263 registered outputs/296045935bytes and two
unused copies/59824542bytes are removed. Further native bin/obj retirement is rejected
before execution;966files/130925594bytes remain with no alternate deletion. Physical
camera/corpus, merchant/text, remaining platform/release gates and the full goal stay
open. D-594 remains in force.
[Complete native boundary and preservation](quality/emulator-camera-acquisition.md).

### D-599 - Bound original command and reader timing before another Home candidate

One new original100k English publication on the owned API36/x64/4096 emulator records
527 fixed typed EF events,109 inside its original publication window, with zero drops.
Whole1664.9301ms, command intervals9.1219ms and reader lifetimes433.7934ms are distinct
instrumented boundaries, not exclusive CPU costs or a phone/cold/two-second pass.
Eleven compilation starts/plans occur outside the window and none inside. Original
full financial/native/schema/typed-row/source/ciphertext controls pass; separate scalar
oracle4086 is proven dead before target4279. There is no financial/seed/key replay.
A refused provisional package is never installed; final53725d08 has exact adopted
Core/Data/Brotli PE/native-engine identity,192AOT/204ELF/static16KiB and strict build
proof. The observer records no SQL/parameters/payload strings/keys/native addresses.
Restore actual ebd9/114 German public dictionaries and stopped flag, Main2942/885f6df5/
all120/settings/config; no phone input. Twelve XMLs/484581bytes,1263 Clean outputs/
296011853bytes and five unused copies/178872682bytes are removed. Further bin/obj
retirement is rejected before execution;967files/130910659bytes remain without retry.
Protected comparison sources stay bound for continuation; no shared alias is retired.
No new managed tests or default app change/build; the full goal and D-594 stay active.[Complete original timing boundary](quality/home-query-reader-timing.md).

### D-600 - Close checked primitive inlining at the fixed host gate

A private original-code pair preserves all fields and 410 incomplete/102 mutated outcomes,
but only 0.1895% combined latency improvement and unchanged allocation miss the frozen 5%
gate. Close it without app/native/financial replay; production inputs remain exact.
[Complete host result](quality/rejected-checked-primitive-inlining.md).

### D-601 - Withdraw ledger text reuse and recover the original retirement report

Bounded immutable Note/Payee reuse passes a four-shape host prerequisite and two owned
emulator pairs, but the physical first pair is only 3129.0962 to 3120.7219ms (0.2676%),
below the frozen 5% gate. Withdraw all product changes and five experimental cases;
final new managed count is zero. The 69 selected executions include those five cases,
not a larger retained suite. No phone reverse/rescue, shipping build or Main upgrade.
Reference Core/Data shipping-byte identity is not reproduced; pair checks cannot replace
D-543, and no shipping-equivalent performance acceptance is claimed.

Fix the actual read-only observer's missing retirement phase, require its Comparison,
and select only an already listening explicit ADB server (default 5038). Recover original
PID11081's accepted report without repeating retirement; three bounded observer outcomes
pass. Full empty independent-Main schema proof precedes original B1D5 restoration.
Main24382/885f6df5/Home979/all141, caller actual English onboarding/all61/UIDCEDE/flags/
absence/stopped=false and 14 settings/presence match; emulator/Main/caller/config also
restore exactly. No financial/seed/key replay or private shipping Main DB/key read.
Standard Clean removes 9148 registered files/2253169797 logical bytes; 18 new XMLs/
714460bytes retire. Three further removal scopes are rejected before execution with
`blocked by policy`; 6376 files/1413241028bytes remain without retry or alternate deletion.
Protected evidence/shared aliases remain. The overall goal and D-594 execution order stay
active; two-second, stability, security, remaining accessibility and external gates stay open.

[Complete candidate rejection, input limits and observer repair](quality/withdrawn-ledger-text-reuse.md).

### D-602 - Reserve Samsung without cross-chat coordination

The owner reserves the phone fully for Zanance until explicitly changed; earlier Loyan
turn/handoff requirements no longer require coordination or another availability question.
Necessary low-risk actions remain autonomous under D-597, preserving original state/data and
accepted-operation checkpoints. For an already authorized Zanance installation, select the
specific `Do not send` / `Don't send` security-scanning option independently after verifying
its current owned package/installer attempt. This reaffirms the per-install D-560 exception;
other high-risk actions and genuine authentication retain their existing owner boundaries.
No device command, installation, test or financial operation is needed to record this rule.
The overall delivery goal and D-594's TalkBack execution order remain active.

### D-603 - Observe live Android backup eligibility without a payload request

The fixed shell-only observer binds the original owned package UIDs before typed
BackupManager status/eligibility queries. API36 emulator returns Main10217=false,
caller10221=false and Settings-provider1000=true while backup is disabled. Samsung
returns Main10380=false/caller10381=false while its original backup-enabled state
remains true. These are five real framework replies, not actual OEM transfer or
cloud-payload exclusion. A wrong expected UID refuses before service binding; errors
never substitute false. Strict corrected Java/D8 compilation passes; the initial
module-path warning refusal remains. No product code/dependency/permission change,
managed test addition, app rebuild, backup/restore, authentication or financial replay.

Both original APK/UID/CEDE/flags/stopped/PID states, eleven settings, boot/backup state
and display context remain unchanged. Two new hash-bound remote JARs retire with
6236 logical bytes removed; local used build witnesses remain. The owned AVD stops
with exact configuration preserved; both ADB servers and phone context remain.
SEC-07 actual OEM/device-transfer and iOS gates stay open. Overall delivery remains
active; further TalkBack stays last and Persian speech parked under D-594.

[Complete native evidence and limitations](quality/android-live-backup-eligibility.md).

### D-604 - Align current optional-service and backup disclosure inventories

Correct the obsolete absent-feature, active Android OS-backup and cloud-only data-flow
claims against current source. Personal service-readable sync, household payload E2EE,
aggregate-analysis prerequisites, explicitly fetched ECB references and inactive real
purchases retain distinct boundaries. Both app/starter flow rows agree; old SDK/build
versions remain explicitly historical. Submission answers and the public policy stay
drafts requiring the actual selected artifact/processor/platform acceptance. No product
change, new managed test, build, native/financial replay, publication or account action.
[Source-backed corrections and document verification](quality/current-privacy-inventory-alignment.md).

### D-605 - Preserve queue reconstruction across null saved snooze slots

Five fresh regressions reproduce valid-JSON null slots aborting Core/actual SQLite
reminder publication. Filter only null records before the original queue cap; retain
all fields, original source/privacy validation and deadlines. Five new plus one affected
existing managed case pass; no full-suite or successful financial/native replay. Strict
Windows and complete signed Offline384AOT/408ELF product checks pass. One fresh API36/x64
native cohort publishes original plan+snooze after20 nulls, preserves the complete typed
fictitious source and zero ledger, then removes both requests and only its new fixture.

Preserve real startup/storage/AOT/observer refusals; never weaken the staging-space
bound. Restore exact prior stopped portable code with KEEP_DATA ownership/flags unchanged.
Samsung no-reset adoption24382/885f6df5 to17668/1f9816c4 preserves all141 original public
attributes/Home979 and package/settings guards; the first51-node loading observation
continues only in the same process. Original emulated Main/zero/all120 is restored and
its AVD stops. Eight XMLs/207653bytes and3175 registered Clean outputs/1041974987logical
bytes retire; used evidence remains. Delivery, broad stability/security/OEM/other platform
and release gates stay open; TalkBack remains last. [Complete evidence](quality/null-snooze-cache-reconstruction.md).

### D-606 - Keep the known staging-space refusal on a safe covered Retry surface

A fixed IOException subtype identifies only the original bounded staging-space guard;
its exact required-space predicate and all key/source/candidate checks remain unchanged.
Startup shows specific six-language guidance and suppresses both recovery choices and
retained callbacks until explicit Retry. Seven fresh tests and four affected existing
cases pass after three genuine baseline failures. Eighteen original-control synthetic
Android layouts cover three widths/languages and both themes at actual OS font2.0;
strict Windows/complete signed Offline builds and no-reset Samsung adoption preserve
Home979/all141 public attributes. This is not actual disk-exhaustion/authentication or
whole-security/stability/release acceptance. [Evidence](quality/startup-storage-guidance.md).

### D-609 - Measure the retained camera frame without replay

Original D-598 input/stored digests and all six ink boxes establish a4/3 top-left
height mapping with horizontal clipping, not complete-text camera acceptance. Exact
installed emulator source identity remains unproven; no OCR/financial/product change
is justified. No new build, native/managed test or financial command.
[Measured boundary and remaining input](quality/original-camera-frame-geometry.md).

### D-610 - Direct complete-ledger destination closes at its fixed host gate

Remove only intermediate physical-row lists and final-list growth in private copies.
Full fields/ownership/cancellation and malformed outcomes match; host latency improves
9.5956553% but allocation reduction3.6945719% misses the prior5% gate. No product or
native promotion; standard Clean succeeds and residual deletion is explicitly refused.
[Scope, values and refusal](quality/rejected-direct-ledger-output.md).

### D-611 - Decline empty derived ledger identities before the domain constructor

Six regressions reproduce ArgumentException escaping optional-cache fallback. Check only
the derived empty GUID and keep the original constructor/source identity policy unchanged.
Six new plus three affected cases, strict Windows/complete Offline Android and one
original native fallback pass. Preserve helper failures and post-observer baseline limits.
Physical adoption waits behind the unchanged D-607 cohort.
[Evidence and exact limits](quality/derived-ledger-empty-identity.md).

### D-612 - Admit optional column metadata before reading or maintaining it

Validate the exact three-table/eleven-column optional contract with one metadata query.
Unsupported shapes decline transport rather than failing the original Home/account writer.
Real IO/cancellation and all financial/source/writer guards stay unchanged.
[Evidence and exact limits](quality/unsupported-derived-ledger-schema.md).

### D-613 - Bound actual camera bytes after native source acceptance

A stat-only camera check does not bound a file that grows before reading. Reuse the
unchanged original picker stream reader for camera output, preserving the original
64MiB ceiling, URI/result/error and finally deletion. Two actual filesystem regressions
and six corrected cases, strict builds and one finite native callback/retirement pass.
[Evidence and exact limits](quality/bounded-camera-source-read.md).

### D-614 - Host codec eligibility cannot replace original native Home

Original private stage counters identify about46-49% host expansion wall time. Quality0
gains6.92095% on that host but original native Home1908.2973 to2156.9117ms and dispatcher
177.8566 to201.0806ms fail fixed gates. Reject without reverse/rescue or product change.
Preserve the actually observed local-cache cookie refusal and fully verified copy-only
admission; source/financial guards and all tracked code remain original.
[Evidence, retained failures and residual cleanup limits](quality/rejected-ledger-brotli-quality.md).

### D-615 - Missing camera headers do not turn dates into merchants

One new portrait source still loses its actual header/footer in the camera result.
Preserve that framing failure, exact source and one acquisition; do not invent OCR.
Seven original parser failures show the surviving date becomes a shop. End merchant
search at a full valid date before any header, without changing total/currency/date
or accepted writes. Ten new plus45 directly related cases and an exact retained-JPEG
native reread pass. Diagnostic editor navigation during fresh onboarding fails and
is not accepted. Strict builds pass; physical adoption waits for D-607.
[Evidence and exact limits](quality/portrait-camera-merchant-boundary.md).

### D-616 - Admit complete primary camera text through the supported scene route

The unchanged D-615 portrait source on official scene posters reaches the original
reader with all9 primary lines and correct merchant/2345EURminor after one shutter.
Preserve the extra corrupted rotated-poster OCR row and both historical imagefile
crop failures. Use the installed official room-walk macro after a real preview
precondition; do not read credentials, bypass a refused API or alter source pixels.
No product/test/financial change; broader physical/corpus/editor gates stay separate.
[Evidence and exact limits](quality/complete-virtual-scene-camera-source.md).

### D-617 - Keep onboarding step scroll ownership and qualify original receipt drafts

The original preferences offset hides the next account title/name before any write.
Give each unchanged step its own ScrollView; preserve all controls/commands and fixed
chrome, without ScrollToAsync or platform callbacks. Strict builds, exact original
failure input and18 changed native contexts pass. One fresh zero account enables
two original Home/picker/editor drafts: correct shop versus empty missing-header
payee, both23.45EUR; cancel both and retain an empty ledger. Cleanup refusals and
renderer/observer failures remain explicit; D-607 and physical adoption stay open.
[Evidence and exact limits](quality/original-receipt-drafts-and-onboarding-scroll.md).

### D-618 - Actual OS200% evidence for independent onboarding viewports

Reuse the complete D-617 signed product without a build, financial setup or receipt
read.18 distinct en/fa/de Light/Dark360/412/680 contexts pass actual OS200% content,
step-owned scroll, full Name reachability and separate fixed-footer geometry in the
same original PID4260. Disqualify the initial clipped-native-bounds apparent pass;
retain it and strengthen source-bound admission before final cases. Exact owned
restoration/retirement passes; six residual config files remain after policy rejection.
[Evidence and exact limits](quality/onboarding-independent-scroll-os-text.md).

### D-619 - Admit actual compressed input before performance claims

A separate requested quality1/window16 hypothesis preserves all original fields and
buffers, but every actual candidate header is18 versus reference22. Two strict private
encode-only processes preserve input digests; no decoder/timing/native work follows.
Official upstream fast-path source is consistent, not a proved installed-binary source
match. Keep production unchanged rather than relabel an unmet premise or tune a rescue.
[Evidence, primary sources and exact limits](quality/requested-brotli-window-admission.md).

### D-620 - Restore the published delivery plan's existing evidence link

A bounded audit of11 published primary planning/acceptance documents finds one
broken local file link among2576 local links: the D-507 delivery-plan evidence uses
quality/ at the nested enhancement directory instead of ../../quality/. Its existing
tracked timed-reminder report is the unique intended target and matches the original
paragraph; correct only that relative path. Five apparent file-only failures are
valid published directories and remain unchanged. Preserve the initial audit and
its directory classification, then verify the published proposal links, excluding
unfinished D-607 working content.
No fragment/remote availability or historical acceptance is inferred. No product,
status, test/build/device/financial operation or pending D-607 content changes.

### D-621 - Suggest a complete source-named date only after original numeric lookup

The same full named rows already bound the merchant header but never reached Date.
Keep the entire original numeric/calendar priority; add only a complete supported
source-calendar fallback with year bounds, no missing-year/UI-locale guess and no
choice between distinct named dates.13 original failures become24 new passes; preserve
and update the one historical boundary-only null-Date assertion.67 unique selected
passes, strict builds and one original new past-date PNG/editor/Discard route pass
without financial posting. Windows restore and cleanup failures remain explicit.
[Evidence and exact limits](quality/complete-named-receipt-date-suggestions.md).

### D-622 - Observe a genuinely attached, unlaid native scroll lifetime

The old hidden-root readiness failures remain unchanged. A new independent visible
native child with parent layout suppressed actually satisfies the stock10.0.120
handler's pending-layout predicate before two distinct original scroll requests.
Release completes one; handler retirement leaves the other pending at the bounded
250ms observation. No historical ANR cause, infinite task or product fix is inferred.
Strict native build, exact framework provenance, original settings/restoration and
fresh-UID/AVD retirement pass; compiler/observer and cleanup failures are retained.
The owner then explicitly authorizes a temporary Loyan phone turn protecting D-607;
communicate its constraints without early phone queries, and require explicit return.
[Evidence and exact limits](quality/connected-unlaid-scroll-lifetime.md).

### D-623 - Preserve the original validation helper after its actual refusal

Inspect original editor call sites before proposing a scroll workaround. One fresh
linked-helper API36/x64/4096 observation returns false for the genuinely attached/
focused/unlaid0x0/requested target. No false-positive or original-app fault is proved;
other-window global callback admission is not established. Close this candidate
without a production patch or passed-case replay. Strict independent build, exact
source, terminal/restoration and retirement evidence pass; cleanup refusal remains.
[Evidence and exact limits](quality/original-validation-layout-refusal.md).


### D-624 - Deliver actual Android drafts without reopening accepted writes

Complete the missing Android counterpart to historical Windows assets as48 actual frames in six languages, using the existing signed D-621 APK and one new original-UI demo dataset. Preserve ten accepted operations, close entity writing before capture, reject only one unfinished German IME frame and retain the actual French-navigation ANR/recovery. Exact original settings and fresh-UID/AVD retirement are recorded; supplemental cleanup refusal remains. The latest owner reply keeps Samsung with Zanance and supersedes a briefly issued temporary turn before any installation; explicit peer return is reported, not fresh device evidence.
[Evidence and exact limits](quality/android-store-drafts.md).


### D-625 - Use the supported progress theme surface above implicit styles

The installed control's local theme background wins over the existing transparent
implicit style. One actual original/corrected native comparison establishes that
its documented resource keys remove white corner slivers. Palette.Apply supplies
a transparent semantic surface before CommonTheme; no financial/style/private-API
change. Six contexts/eighteen actual arranged bars preserve progress, RTL, colors
and geometry. Strict builds/full offline APK pass; physical adoption waits for
D-607. Native retirement and supplemental cleanup rejection are explicit.
[Evidence and exact limits](quality/transparent-progress-theme-surface.md).


### D-626 - Complete printed timestamps do not invent a merchant

Eight real original parser failures show date/time rows becoming merchant or item
names. Strip only a bounded valid trailing clock before full original calendar/date
validation; keep actual source/merchant text, numeric precedence, conflict refusal
and money bodies unchanged.19new+34affected existing cases, strict builds and one
original PNG/reader/unsaved Date/empty-payee draft qualify the finite correction.
Continue the same draft after Simple visibility/clipped-bounds observer refusals;
Discard once, final history empty/Home0. Fresh retirement and cleanup refusal are
explicit; D-607 and broader gates stay open.
[Evidence and exact limits](quality/receipt-printed-timestamp-boundary.md).


### D-627 - Complete original account-list read and covered publication

Replace separate source/settings-creating reads with one original-file deferred
snapshot and worker projection; compare original financial revision and complete
Accounts/Settings separately before bounded same-file retry. Keep all financial
math/order/holding separation. Publish rows once under local Loading/error/Retry,
retire accepted retained-tab/root departures and guard actual current row commands,
including the shared Home route.30new focused cases,21existing resource checks,
strict builds and finite original-page native observations pass. Preserve the real
first tab-retirement failure and correct only that predicate on the same no-reset
fixture. Six language/theme contexts, actual360dp/OS200% covers,800dp frame, current
Accounts/Home detail routes and final source-visible/settings preservation pass.
Fresh UID/212XML/AVD retire; supplemental3790file deletion is policy-refused. No
financial replay, shipping-byte/performance/ANR or physical-adoption claim follows.
[Complete source, native outcome and limits](quality/covered-original-account-overview.md).

### D-628 - Original reviewed account conversion and accepted-result ownership

Bind complete original account/history/date/access/file before explicit native
confirmation. Validate inside the original atomic writer before mutation and
commit; only its proved own archive flag/audit may normalize. Preserve original
one-unit/basis/nonpositive rules and compatibility API with actual-balance guard.
Retire old page/tab/root consent, consume one accepted review and retain its typed
result across Changed/native-route failures. Three feedback captions translate in
all six languages without another write.28new focused cases, two affected existing
checks/21resources, strict builds, original native Cancel and one conversion pass.
Six same-result contexts and14separate content-only layouts do not establish font-
recreation persistence; preserve the actual return to Home. Both fresh environments
retire, two supplemental cleanup refusals stay explicit; phone/D-607 remain untouched.
[Complete original source, failures and limits](quality/original-account-conversion-ownership.md).

## D-629 – Complete original account-detail source and command ownership (2026-10-28)

Account detail now reads optional settings, accounts, all ledger fields, categories,
goals and allocations from one original-file SQLite snapshot, with worker projection,
covered complete publication and bounded retries only for persisted same-file edits.
Keep original formulas/order and raw reconciliation input; retire reads/routes/native
installment consent on departure. The original writer validates owner before Save/commit
and preserves known accepted results after failed notifications. Four original regressions
are corrected;29new and6affected existing cases pass. Original native UI verifies loading/
error/Retry, retired-read and stale-consent rejection,100/25/75, ten display contexts and
zero ledger writes. See [full evidence](quality/complete-original-account-detail.md).
D-607/phone, physical adoption and broad release gates stay separate.

## D-630 – Complete original loan schedule and current publication owner (2026-10-28)

Four original schedule failures are corrected: partial Add publication, Name/Rows assigned
before final source refusal, stale name for another identity and Ready after a pending
request's query changes. Preserve original LoanReadReview/calculator/permissions/day; move
original query/validation work off UI, stage all rows, validate before one covered Reset and
retire actual page/tab/root/display owners. Fourteen new focused cases and six affected
existing free-policy cases pass, as do strict builds and the original independent native
cohort with11rows/58.98EUR interest and zero ledger entries. See
[full original schedule evidence](quality/complete-original-loan-schedule.md).
D-607/phone/physical adoption and general release gates remain separate.

## D-631 - Coherent original reimbursement source and publication (2026-10-29)

Two actual original-store composition failures expose mixed-file/currency/amount
inputs. Capture one original context/deferred transaction on a worker, preserve
complete original facts/calculators/order, validate the original file/access/ledger
revision/account-category metadata and privately publish the complete native result.
Retire page/tab/root/display owners and revalidate retained original row commands;
expected refusal remains local with Retry. Nine distinct new selected cases, strict
builds and original native command/cover/cancellation/return/six-display contexts pass
with one original expense and no repeated accepted setup. See
[complete evidence](quality/complete-original-reimbursements.md). D-607/phone and broad
release gates remain separate; no migration/formula/runtime/permission change occurs.

## D-632 - Original plan-detail source and lifecycle ownership (2026-10-29)

Bind original complete detail/work policy to one file/worker transaction and covered
publication. Retire page/tab/root/query/display owners, rows and native consent.
Prepared Pause/Resume/End/Delete preserve original domain/writer policies, stable
continuation identity and known acceptance before notification/return. Same-page
resume drafts survive actual local failure/Retry; a replaced root is not retention.
Twenty-two new selected cases, strict builds and the original native cycle pass with
zero ledger entries. Correct actual six-language plan-loading/Retry wording. See
[full evidence and limits](quality/complete-original-plan-detail.md). D-607/physical
adoption and broad release gates remain separate.

D-632 completion addendum: the separate bounded layout cohort completes12missing
360/680dp receipt contexts and18loading/18error-Retry contexts across360/412/680dp,
en/de/fa and both themes. Current-window XML/native line-end/ellipsis/height/bounds
checks agree, without old financial form commands. Preserve the failed copy/managed
exit, committed-copy observation failure and21actual audit-time differences; complete
new-source comparisons remain exact. No old-source/key identity or OS200% claim.
Additional fresh UID/AVD and179XMLs retire; total five standard cleans remove
2588206899logicalbytes. Two supplemental refusals leave698380075logicalbytes with no
retry/subset/alternate/move/escalation. See the full D-632 quality record; D-607/phone,
physical adoption and broad release gates remain separate.

## D-633 - Original Plans center snapshot and command ownership (2026-10-29)

The original four independent reads can combine one file's plans and references with
another file's review count. Keep the original complete work, references, ledger revision
and aggregate count in one actual-file deferred worker transaction; bind initial failure
Retry to that file. Stage original calculations privately, retain native permission queries,
validate after their continuation and publish whole rows/flags under cover. Original row,
Add and review routes require current source and row-object authority. Actual page/tab/root/
display/segment retirement refuses old continuations. Share appearance/navigation for one
presence so an automatic second load cannot erase a local failure before explicit Retry.
Ten new distinct focused data cases and corrected strict Windows/offline signed Android
builds pass. Original native source/retirement/permission-read/routes,54changed-layout states,
exact source/settings/OS restoration and fresh retirement pass, as recorded in
[the full quality record](quality/complete-original-plans-center.md). No old financial
command replay, phone command, new permission, schema migration or formula change.
D-607's three original deadlines and all broad release gates remain separate.

## D-634 - Original template center and deletion ownership (2026-10-29)

The actual published page leaves its actions live after Home departure and opens an
old-row native confirmation. Original action regressions also delete after writer
retirement and misreport an accepted deletion when its notification fails. Preserve
all three failures. Bind the complete original quick-resource packet to one actual-file
worker transaction, first-file Retry and fresh complete validation; privately order and
publish whole original rows under cover. Retire real page/tab/root/display/row owners,
share one appearance/navigation presence and preserve known acceptance through reload.
The original confirmation, quota, delete-data rights and writer transaction remain;
current-owner checks now roll back retirement and postcommit notification retains
committed semantics. Four real six-language captions keep local error and known-deleted
feedback explicit. No model/schema/migration/financial formula/permission changes.
Nine new focused cases, corrected strict Windows/full offline signed Android packages
and the original independent native cohort pass. One original definition is deleted once
with zero ledger entries; its accepted result survives the following read failure and
explicit Retry. All72changed states across en/fa/de, both themes and360/412/680dp pass
actual native text/window checks, with original source matching exactly after the one
declared definition delta. Original settings/OS return; only the fresh UID/AVD and167
hash-matched XMLs retire. Four standard cleans remove1947378928logicalbytes. Supplemental
twelve-target removal is refused before process creation, leaving3720files/784791322
logicalbytes without retry/subset/alternate/move/escalation/reuse. These are engineering
facts, not broad release acceptance. D-607/phone/three original deadlines remain untouched.
[Full evidence and preserved failures](quality/complete-original-template-center.md).
