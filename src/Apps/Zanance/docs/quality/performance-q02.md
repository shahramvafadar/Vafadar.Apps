# Q-02 performance evidence (D-75, D-76 / ZCR-QA-06)

Engineering measurements on 2026-10-09; physical reference-device selection and acceptance remain open. A two-second
Home objective is not an achieved or published claim. This report records failures as well as durations.

## Workload and environment

| Shape | Entries | EUR checking accounts | Active manual schedules |
|---|---:|---:|---:|
| Reference | 10,000 | 20 | 100 |
| Tenfold | 100,000 | 200 | 1,000 |

Fixed fixture date 2026-10-09, seed 42, 1,000 preceding days, 10% income/10% refund/10% transfer/70% expense.
No attachments, goals, holdings, budgets, enabled reminders, auto-posting, credentials or owner data. See
[the reproducible executable](../../../../../eng/benchmarks/Zanance.Performance/README.md) for exact timer boundaries.
It references actual Core/Data code and is not shipped or referenced by the app.

Host: AMD Ryzen 9 9950X, 16 cores/32 logical processors; Windows kernel build 26300; x64 .NET 10.0.12. Desktop app:
Windows Debug. Android: the existing isolated API 36 Google APIs x86_64 emulator, WHPX, complete trimmed/AOT Release
APK, explicitly selected emulator serial. No tools/SDK downloads, physical-phone actions or system-setting changes.
Native loading samples are process-cold with warm OS/file caches, not power-on or storage-cache-cold measurements.

Migration workers populate the first actual schema. It did not contain schedules, which are added after upgrade.
SHA-256 preservation covers all unchanged legacy account/entry fields plus retained settings; only retired WeekStart
is excluded. Every before/after fingerprint matches. Each warmed save is read back, removed outside its timer and the
unchanged fixture verified. Existing directories, redirected paths and accidental database replacement are refused.

## Desktop store and calculation measurements

Seven operation samples after warm-up; milliseconds, median with observed minimum/maximum. First validated save
is recorded separately. Three fresh migration worker processes per shape; fixture generation excluded. These are
not UI timings, system startup times, device budgets or release gates. Background load, caching and run order affect
the numbers; do not attribute every before/after difference to the changes below.

| Candidate operation | Reference median [min, max] ms | Tenfold median [min, max] ms |
|---|---:|---:|
| Legacy upgrade | 635.42 [632.04, 674.44] | 686.92 [665.61, 705.37] |
| Pending migration check | 1.16 [1.14, 6.27] | 1.11 [1.06, 1.19] |
| Read all entries | 80.03 [66.33, 95.90] | 790.43 [749.73, 864.60] |
| Read current month | 0.88 [0.85, 1.54] | 6.71 [6.24, 7.55] |
| Unique title search + grouping | 11.06 [7.73, 12.89] | 20.42 [19.43, 66.49] |
| Missing title search + grouping | 7.23 [6.85, 11.49] | 19.85 [18.93, 21.20] |
| Numeric search + grouping | 13.20 [9.32, 14.83] | 25.19 [23.89, 27.33] |
| Broad title search + grouping | 5.85 [4.05, 6.91] | 10.47 [9.34, 12.18] |
| Home balance/totals/90-day forecast calculation paths | 7.33 [5.89, 12.55] | 34.39 [27.29, 46.95] |
| Validated entry save | 4.81 [4.39, 9.66] | 5.13 [4.54, 5.77] |

Baseline same-host tenfold Home calculation median 855.54 ms [440.58, 1148.01]; candidate 34.39 ms [27.29, 46.95].
Other baseline measurements and all raw candidate samples remain in the local measurement archive. The original
baseline save cohort included first use; it is not a like-for-like warmed-save comparison.
Candidate first validated saves were 211.86 ms (reference) and 8.09 ms (tenfold), separate from the warmed cohorts.

## Baseline native observations and findings

Windows Debug, three process-cold observations per shape: native fixture account and recent-entry content appeared
at 2424.98/2501.52/2686.26 ms (reference) and 7954.06/8074.95/8520.23 ms (tenfold, values sorted). These include
UI Automation query time; they are upper observations of bound native content, not exact first-paint timestamps.
One earlier tenfold launch was not observed within 90 seconds; a separate check and the controlled three-sample
cohort passed. Cause unconfirmed. Original development databases restored with matching hashes after every run.

Android tenfold Release: system TotalTime first-frame samples 4924/4570/4896 ms. Fully populated native Home
observed at 16231.39/15670.52/16907.23 ms, including accessibility-dump overhead. Hierarchy creation was unavailable
during several loading observations; no stale dump was accepted. Actual recorded total agreed with the generated
ledger at -1,227,809,870 EUR minor units. System first frame is not loaded Home.

After no-match to unique-transfer filtering over All, the native counter and correct date group showed one match,
but its transaction button was not exposed in repeated hierarchy observations. This is a native result-row finding,
not a successful rendered-search measurement. A later Home return produced Android's native not-responding dialog.
The fixture remained isolated and screenshot protection unchanged. No speculative rendering workaround is included
in the performance changes; final candidate native checks must retain or resolve this finding explicitly.

## Candidate native observations

Windows Debug, three process-cold observations after the Android build completed: loaded fixture account/recent
content appeared at 2481.70/2238.32/2285.32 ms (reference) and 5174.47/5369.35/5443.75 ms (tenfold).
An exploratory cohort overlapped the build and is excluded from this comparison. Its restore initially encountered
a closing process's database handle; the preserved original files were restored with matching hashes, and the
controlled run waits for process exit before restoring. No original development data was lost or replaced.

| Android Release observation | Reference samples, ms | Tenfold samples, ms |
|---|---|---|
| System first frame (TotalTime) | 4241 / 4317 / 4407 | 5114 / 5001 / 4536 |
| Loaded Home, native upper observation | 9195.86 / 9438.28 / 9510.32 | 16108.93 / 16289.00 / 16981.71 |
| Three unique transfer searches after list loading | 2495.74 / 2515.57 / 2571.94 | 2839.62 / 2955.80 / 2956.80 |
| One Save to updated Home, native upper observation | 5747.84 | 19345.19 |

These include accessibility/input/navigation overhead. Android Home does not show a demonstrated duration
improvement over the baseline. Search samples select All, move through no-match results and verify exactly one
actual transaction button for each named fixture entry; neither input text nor the result counter counts as success.

The early-input tenfold repeat still exposed a counter/date group without its result button in eight hierarchies.
A separate controlled run first waited for the initial native transaction rows (observed by 8189.50 ms after
navigation, including three dumps), then all three no-match/unique-result transitions passed. This narrows the
observation to an input-during-initial-loading scenario, not a proven root cause or repaired native rendering bug.
No ANR dialog occurred during the controlled candidate navigation/search/save sequence; the baseline ANR and
early-input failure remain open findings. No speculative CollectionView workaround or package upgrade was added.

Release saves were followed by an unlaunched Debug reinstall solely to copy the three owned fictitious databases.
Read-back proves 10,001/100,001 entries: exactly one new Expense of 1,234 EUR minor units on account 000 per shape;
every complete original account and ledger row is unchanged. Totals are -122,339,055 / -1,227,811,104 minor units,
matching native Home. The 100/1,000 plans remain manual with no reminders. The prior three-entry native test profile
is unchanged. The complete candidate Release was then reinstalled; no SecureStorage or owner database was read.

## Changes and regression proof

Microsoft.Data.Sqlite's async I/O executes synchronously; see the
[provider documentation](https://learn.microsoft.com/dotnet/standard/data/sqlite/async). GetEntriesAsync captures its
short-lived context/profile, materializes on one worker and disposes after completion/cancellation. It never uses
the context concurrently. A queued read retains its original profile even when LocalDatabaseLocation moves.

AccountEntryIndex routes original records to endpoint slices in original order. It does not clone a transfer into
two ledger entries or change a formula. The established Balance method still applies dates, confirmation scope,
currency and checked arithmetic. Home accounts, total balances and forecast starts avoid repeated full-ledger scans.
No schema, compiled-model, permission, SDK, security setting, commercial limit or portable-backup change.

AT-82: 12 Core index cases and 11 real SQLite fixture/read cases, 23 added in total. Main suite 1,323 passed, zero
failed/skipped. Strict Windows and complete Android Release builds: zero warnings/errors. Windows Home, Transactions,
Forecast and Reports reviewed in en/fa/de, light/dark at 360x800, 412x892 and 1280x820; 544 app-window captures including
related report/forecast states. Original development database files restored with matching hashes.

Complete signed Release handoff: artifacts/android/zanance-d75-release.apk, 80,644,011 bytes; SHA-256
d683c4f0e6c350ce7be0a290492a64fe5c8f9711d3784f23775712757487d7dd. Package pro.vafadar.zanance,
0.1.0/code 1, min SDK 24/target 36, ARM64/x86_64 embedded assembly stores/application AOT, ZIP integrity and v2/v3
signature verified; Cloud permission guard passes. The independent benchmark is not an app dependency. Screenshot
protection remains unchanged; emulator evidence uses native hierarchies, never an attached physical phone.

## Acceptance boundaries

Repeat loaded Home, native result rows, actual Save/read-back and input responsiveness on a physical ARM64 reference
device with a complete signed APK; retain raw samples and failed observations. Test iOS separately. Neither generous
calculation-budget tests, same-host timings, CI, system first frame nor emulator evidence establishes a two-second
device objective, production readiness, native provider acceptance or product publication.
Q-02 remains unaccepted. QA-06 retains the early-input native-row finding, the baseline ANR follow-up and loaded
Android duration work; completed measurement/optimization delivery does not close those acceptance gates.

## Initial-loading interaction follow-up (D-76)

Prevent input before the initial transaction snapshot is presented, without attributing the D-75 result-row finding
to an unproved native adapter cause. SnapshotLoadState starts covered, shares pending reads, publishes before ready,
propagates failures/cancellation and permits retry. Search/filter/Add/bulk controls and grouped results remain
disabled/hidden during loading or failure. The translated loading message replaces premature empty-ledger/no-match
claims; failure offers explicit retry. The prior query/filter choices and existing financial/search algorithms remain.

AT-83 adds 12 actual application cases, including a real SQLite transfer; main suite 1,335 passed, zero failed/skipped.
Strict Windows build has zero warnings/errors. Final 216 app-window captures cover en/fa/de light/dark 360/412/wide
loading, read failure, reload and existing list/filter/bulk states; original development data restored with matching
hashes. The Windows still images demonstrate loading text, not the animated indicator glyph. The Debug failure is
fictitious; it does not establish Release I/O failure. An additional 36 captures verify real native retry invocation
in en/fa/de at 360x800 dark through UI Automation, followed by the actual read/publication path. The initial two
diagnostic runs could not find the MAUI button with the dialog-oriented window lookup; the successful run uses the
current page's actual named control/handler. All original data restores match their hashes. No schema, native package,
permission, security setting or financial formula change. D-75 negative observations remain historical evidence;
the baseline ANR, Android loaded Home and physical/iOS objectives remain open independently of this input gate.

Final complete Release emulator repeat: the tenfold loading hierarchy shows six disabled controls (Search, More
filters, selected period, All, selected kind, Add), loading text and a native progress indicator, no prematurely
exposed result rows and no false empty/no-match message. A tap/text attempt on disabled Search is ignored; its initial
text remains and no keyboard appears. One earlier hierarchy is unavailable and retained. Loaded native rows are
observed by 8,535.76 ms after navigation, including hierarchy/input overhead; this is not a precise rendering budget.
The reference list is already ready at the first 3,244.07 ms observation, so its loading frame is not demonstrated.

Both fixtures verify no-match, then exactly one real button for each of QA06 item 000012/000022/000032 over All.
Reference search upper observations: 3,930.04/2,629.26/2,500.70 ms; tenfold: 2,690.65/2,833.76/2,923.98 ms. Include
input/dump overhead; do not interpret them as two-second search claims or a causal before/after improvement.
Returning Home/Transactions preserves the last query and actual result button. No ANR dialog is observed in these
checked sequences; the historical baseline ANR remains a follow-up, not a proven repaired defect.

An unlaunched Debug reinstall copies only the three owned fictitious databases. Complete Accounts/Entries/Schedules
equal the D-75 read-back: 10,001/100,001/3 entries, no financial change. Final Release reinstalled; prior test profile
restored. No owner data, SecureStorage, physical phone or screenshot-protection change. Final signed handoff:
artifacts/android/zanance-d76-release.apk, 80,664,491 bytes; SHA-256
d7e2c558159ed3e9c4bab2cf4b7e0f9b5c049527989768ad6dc16d1f9a7fae9b. Package/version/SDK, v2/v3 signature, ZIP,
ARM64/x86_64 embedded stores/app AOT and Cloud permission boundary verified. Strict Windows/Android: zero warnings/
errors. This delivers the input boundary; Q-02 and QA-06's native duration/ANR/device objectives remain open.
