# Complete original goal-list publication (D-447)

## Observed defects and implementation

Three actual-source regressions first fail against the original list: an old row remains navigable
following refreshed source data, navigation becomes available during partial collection publication,
and old actions remain available after a failed read. The list previously reads its financial inputs
separately and calculates funding/progress on the window thread.

GoalStore.ReadListSnapshotAsync captures the actual factory context before scheduling its keyed
connection and deferred SQLite transaction on a worker. It reads original goals/work/allocations,
accounts, complete ledger and holding types/events from that same file/transaction. It does not
initialize Settings or write money. Validation also opens its connection on a worker and preserves
original work/reference/rights and configured-file checks. The established complete-ledger reader
is shared without changing its algorithm. No model/schema/migration or runtime policy changes occur.

GoalsViewModel retains its original file for the page lifetime, computes the existing financial
algorithms on a worker, then localizes/publishes active, finished and funding collections under one
loading/error cover. One indexed row-identity lookup refuses old value-equal rows. Native commands
belong to the original page revision; disappearance cancels pending publication and retires old
commands. Add/Open revalidate the original source before navigation. Read failures keep translated
local retry feedback and do not enable stale rows. The entire source remains covered until complete.
This does not establish a new original-file contract inside the separate goal detail/editor pages.

## Focused source evidence

Fourteen new actual-source cases pass in9.659s: the three original red regressions, page retirement,
reentrant retirement/rights refusal, failed-read retry, other-file validation, mutable account units,
empty source without Settings, equal-value old rows, original-file retention, factory selection change
and actual read/validation SQLite connection opening away from dedicated caller threads. The empty
case is extended to assert ready/empty/Add-enabled presentation and passes separately in2.777s;
that remains one case, not a fifteenth new test. Three affected prior financial-presentation cases
pass after the final file/worker changes in5.073s. No full-suite count or benchmark is inferred.

Twenty-three actual resource completeness/usage checks pass against repository sources. Initial
resource class selection executes zero cases and is rejected by the minimum-test policy. Direct
execution from alternate-volume output cannot locate repository ancestors and fails five path checks;
copying only owned compiled test output beneath the repository permits the unchanged checker to run.
Neither invocation is a translation pass. No successful financial test is replayed to fix that path.

## Android original native evidence

The exact original secondary-fields source82d32312e5494c2a99fa68b8160d6490 and all five accepted action
receipts are required. No seed, contribution, price, bill or payment is replayed. Six en/fa/de and
Light/Dark contexts exercise loading, ready, native detail visit, actual Back, declared read failure,
and native retry on two active original goals. Complete source/base rows remain identical; writes0.

The first run stops before qualified navigation because its helper requires the entire native overlay
rectangle visible. The original card deliberately gives its real transparent button margin-16; the
378x163 native button has346x131 visible pixels. A second diagnostic continuation establishes owned
package/window, shown/enabled state and that exact clipping. The corrected helper requires a visible
44dp touch area while preserving package/window/enabled checks; production layout is unchanged.
The two original successful loading/ready observations are retained from059ef9d87da443c9a2a37c41673b6ca9.
Final pass2473d59f191f4e9f938a8e1949b702e9/PID8806 records34 new observations plus those2 retained,
six detail visits, six native retries and refusal of retired commands. Accepted navigation is durably
recorded before fallible observers. Complete source/base equality and restored language/theme pass.

## Windows, signed Main package and final boundary

Original Windows sourcec7bc568bfbdb4850be85f8d3f9e0a08c is continued without replaying its five
accepted actions. Eighteen en/fa/de, Light/Dark and360/412/1280 contexts complete84 observations
in PID15812. All18 cover loading/ready/failure/nativeRetry; the six412 contexts additionally visit
original details and return through the actual native header Back. Actual current row/command
bindings remain enabled after publication/return/retry. Source/base equality and writes0 pass.
Images are captured only from the application's own window on its separate non-input desktop.

The terminal complete receipt is correct, but final capture validation initially expects the old
Language/File pair instead of the new Stage/goals.png path. Preserve all84 original PNGs and the
complete receipt; correct only that path predicate and validate every existing image without replay.
Native text is checked against original localized captions; representative Persian light/dark and
loading images are visually inspected. This is normal scale, not new OS-scaling/spoken acceptance.

Strict Windows build completes with0warnings/errors in32.02s; corrected independent Android
build completes with0warnings/errors in1:37.63. Complete Main Offline APK is96,960,415bytes,
SHA256 faf4d5b73c1cfe2c53c6e21a1fba4dcb5175164831c8352c3f5850e36d61e3cc,
with384AOT images, arm64/x64 engines identical to D-446, validv2/v3 signatures and no INTERNET
permission. Main upgrade without reset reaches HomePID8935. Actual Insights/Savings goals shows
its empty state, Add opens the original blank form and Cancel returns to the identical empty list;
no Save occurs. Main account/balance0.00EUR and139 nodes match the previous upgrade except focus.
Final Home retains the same values/bounds/count; one drawing-order value changes after navigation.
Exact font1.0/412x800/density160/accessibility0/servicesnull remain. Focused output cleanup passes.

General ANR/crash, two-second targets, low memory, real Samsung, spoken accessibility, iOS,
provider/store/account/publication acceptance remain open. No physical phone input occurs.
Exact pushed-head CI remains its separate one-request follow-up.
