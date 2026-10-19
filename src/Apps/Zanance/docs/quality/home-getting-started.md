# D-376 Home guidance follows named saved facts

## Observed defect and final behavior

D-375's original FX-only Home screenshot strikes through Record your first expense and hides its
real action even though the only stored entry is a Transfer. Home's LoadGettingStarted also uses
any schedule and any budget counts. A Once plan and Week budget therefore complete the other two
named tasks and can hide the entire card without any of the intended steps being done.

Resolve GettingStartedProgress from the same captured Home snapshot on the existing Task.Run worker:
Expense, supported recurring Frequency (Daily/Weekly/Monthly/Yearly) and BudgetPeriod.Month. Ignore
unrelated record kinds; unknown recurrence does not invent completion. Do not sum money or change
scope, dates, creation defaults, translated captions, account guard or explicit dismissal key.
Deleting a qualifying record makes its incomplete step available again, as original count-based
publication already did. No stored-row rewrite, model/migration, permission, key or provider change.

## Actual-source and native qualification

GettingStartedProgress is actual application source linked into App.Tests, not a duplicated test
algorithm or fake MAUI control. All14 AT-376 cases pass, zero failed/errors/skipped: entry kinds,
Once/every supported frequency/unknown value, monthly versus weekly/fortnightly budgets, independent
completion and removal/empty state. The first linked build hits the sibling Budget namespace; an
explicit model alias fixes that compile error. No failed behavioural test or full-suite rerun is
claimed. Test output is cleaned after qualification; no inferred aggregate test total is published.

A fresh separately owned getting-started.db contains two accounts, one FX transfer, one future Once
plan and one Week budget. Existing count/performance fixtures and all keys remain untouched. Read
actual HomeViewModel/HomePage and require FirstEntryDone/PlanDone/BudgetDone=false and the card=true.
Measure original native captions and require visible/enabled real step buttons and no strikethrough.
Serialize every account/entry/settings/plan/budget field before/after original read-only publication.
This is complete preservation for those fixture fields, not all private main database rows/schema.

Final Windows run 7e327a5ff00b41099c2efbc2fc6293b5/process 70696 completes18 en/fa/de Light/Dark contexts
at 360/412/1280 with zero build warnings/errors; actual own-window images are reviewed. Input desktop
remains Default and rendering stays on the guarded separate non-input desktop. The earlier successful
matrix is retained; final publication is repeated only after moving the new scans to the existing
worker, so the qualification refers to final production behavior rather than the earlier UI scan.

Release/AOT x64 caller 5294, run 97e897bf556641e49e8d5c9da2675c67,
comparison 1348d64ff87f460f9e1189d6959e5f74 completes six en/fa/de Light/Dark contexts at actual
OS 200%/360 dp. Original native TextView paint flags are unstruck and all three real buttons remain
available. All captured fixture fields match. No modal, typing, financial Save or main package input
occurs in this fixture review. Restore exact captured font/display/accessibility values and stop only
the terminal successful caller. Main is then upgraded without reset and its original meaningful
native Home text/descriptions/geometry/state match in process5702; exact OS values and the
installed APK hash match. No phone turn is taken; Samsung remains on the already qualified D-375.

## Package and limits

Canonical complete offline Release APK: artifacts/android-home-getting-started-d376/pro.vafadar.zanance-Signed.apk.
SHA256: cd029cd09a00feedf574b9696696205f2b57c97538a9eb2fcfab105a849b6921; 94,838,517 bytes.
Certificate SHA256: 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
ARM64+x64, 384 AOT libraries, min API 24/target 36; no INTERNET/debuggable/profileable. Eight retained
engine binaries are byte-identical. Strict normal Windows and canonical Android builds have zero
warnings/errors. Independent caller SHA256:
e9e6ec5953396e11094a5c5bcf1dd80050a731706761a17cc1889d833f0521fc; 192 AOT libraries.

No unrelated successful suite, native Add/Cancel path, financial benchmark, backend, sync or crypto
verification is repeated. Native case observations are not unit-test counts. No speed, general
GC/ANR/cold/low-memory, spoken-reader, new phone/iOS/provider/Store or release acceptance is inferred.
Artifacts under artifacts/home-getting-started-d376 retain exact source/test/build/native/caption/
restoration/package evidence. CI is checked separately once against the pushed commit.
