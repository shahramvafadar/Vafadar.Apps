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
