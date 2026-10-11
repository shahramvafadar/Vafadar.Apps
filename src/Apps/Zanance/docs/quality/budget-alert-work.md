# Actual budget alert work - D-150 / AT-152

Status: selected current-period alert preparation/delivery implemented. Commercial registration remains
inactive and test builds remain unrestricted. ENT-02/03 remain in progress: budget view/editor affordances,
scoped budget notification returning-data and remaining operations/resources/import/restore are not finished.

## Complete original calculations and independent delivery

GetBudgetAlertSnapshotAsync reads the actual store's cached access source and every complete original budget
through BudgetChoiceStore. Current activity uses original period/calendar/month-start/account-definition
selection and separate method/rollover/period rights. No optional inactive fallback substitutes for this store.
Check every eligible original current row, across currencies, calendars and overlapping weekly/monthly scopes;
the default new-budget calendar never hides an original current period. Historical/future/disabled/unavailable
rows cannot deliver new alerts. Choices never filter original calculation inputs or grant paid methods.

The immutable batch retains original ids, currencies, statuses, actual local date, privacy and complete guarded
definition review. Read all original accounts/categories and bounded current/carry ledger entries on one context
off the UI thread. Explicit invariant financial packets work under Release/AOT; no reflected serialization,
portable grant, schema/model change or financial write. Recheck actual file, original periods/limits/reference
metadata, stored choice revision/value, local date/month boundary, cached rights, financial packets and privacy
before/after native effects. An inactive-to-active change, same-time financial edit or changed historical carry
invalidates the preparation. Budget choice commits also trigger the shared debounced refresh.

The page and alert batch share the original carry calculator and bounded consecutive-period chain. Match the
canonical account definition as well as period/currency/calendar; never carry another account set's spending
or limits into this one. Original per-period methods, rollover/category limits and all relevant historical
spending remain unchanged. Duplicate canonical period packets reject rather than select an arbitrary row.

Commit device-only threshold levels after successful publication and final batch validation. Refunds lower the
remembered level so later spending can alert again. The installed native SDK's Show contract returns Task<bool>; awaiting it without consuming its result can
complete normally after a rejected request. ReminderPublication now requires true, treating false as a retryable
rejection through the same real non-UI application source compiled in App.Tests. SDK acceptance alone is not
proof of OS delivery. Exceptions and explicit native rejection do not consume the level; a later batch failure
retracts every notification newly attempted by that batch through exact-id Cancel/Clear. No other app or unrelated
notification is removed. Previously delivered historical alerts remain until the user dismisses/taps them.
The existing budget returning-data destination continues through app lock; it is still unscoped and opens the
current budget view. Exact-file scoped budget routing is a remaining step, not acceptance supplied by this slice.

## Verification and limits

25 new AT-152 actual application/SQLite scenarios cover inactive/Plus/bounded/missing/empty/paid selection,
multiple original calendars and periods, original refunds/transfers, en/fa/de details, inflight amount/opening/
category/privacy/limit/date/access/activation changes, partial native failure/retry, explicit false SDK acceptance followed by true retry, later batch failure,
committed choice retirement, permission denial, exact-scope original carry/history, foreign-file review and
choice revision retirement/read failure. Ordinary parallel main 2,632 / App.Tests 575 pass without skips/failures;
normal test output cleaned. A final focused 24-case run also verifies a stored EUR-to-USD-to-EUR
choice ABA before retiring the original batch; cleanup repeated after that changed test. Strict Windows and Android builds and canonical complete Release pass with zero
warnings/errors. The initial focused test compile missed its MoneyText namespace; corrected before green checks. A Windows
second-theme attempt reused fictitious seeded rows and failed its unique budget index; restore exact original
bytes and run only the uncompleted dark case in a fresh independent review. Failed captures remain negative.
The initial native helper counted Android auto-group summary as a fifth alert, then tapped that summary rather
than a returning-data child. Retain that negative route evidence; do not reprepare any financial fixture.
Subsequent cases distinguish summary records and expand the actual group before invoking individual rows.

Windows hidden own-window budget renders cover en/fa/de, light/dark at 412x892 with local text stress. Existing
budget readout/period checks run within that route; development files/sidecars restore exactly. Presentation
structure is unchanged, so no unrelated chooser/layout matrix was repeated. Six normal signed Release Android
cases deliver four real budget alerts for EUR/USD/GBP, overlapping monthly/weekly periods and retained Flex,
independently of a different default calendar. Generic/light and explicit-details/dark texts are observed from
fresh emulator SystemUI hierarchies; individual alert taps in the subsequent five cases open Budget and resume does not repost; the first
case retains negative group-summary tap evidence, not individual route acceptance. Complete 25-table
row/column comparisons pass per case, followed by exact original owned sample/sidecars and Main/English/theme
restoration. After the adapter acceptance correction, rebuild/retest and run one additional normal signed
Release SDK-acceptance/delivery/four-tap case with complete original data/sidecar/display restoration. Successful
notification captions/layouts are unchanged; the earlier six-language/theme matrix is retained without rerunning
unaffected UI. Exact owned fixture cache packets are removed and absence of their known key names verified with bounded stream substitutions without reading or logging
other preference/credential values. No credential/security storage is read. Final rebuilt-case caches are likewise cleared and checked.

Native rights retirement and inflight native retraction are exercised through explicit application effect ports,
not activated paid/Free facts in production DI. The production Cancel/Clear adapter compiles on Android and is
not claimed as a real provider, physical-device, iOS, OS accessibility, performance or Store/commercial acceptance.

Logs: artifacts/budget-alerts-{all-tests,windows-build,android-build,apk-build}-final.log, test-clean-accepted.log and focused-final.log.
Windows: artifacts/budget-alerts-windows-412x892-light, windows-dark-retry-412x892-dark and corresponding runtime logs.
Android: artifacts/budget-alerts-native/final-proof.json, native-accepted/final-proof.json and per-case native hierarchies/notification records.
APK proof: artifacts/budget-alerts-apk-proof.json.

Phone artifact: artifacts/android/zanance-d150-release.apk; 81,557,419 bytes;
SHA-256 `96372683df57cc92077d6fb184dad16b45b443448cdd6192689995b44437233b`. ZIP/full ARM64/x86_64 stores and application AOT, non-debuggable package
pro.vafadar.zanance, minimum 24/target 36 and v2/v3 signature with the existing local test certificate verified.
This is an installable phone-test handoff, not a physical-device or publication result.
