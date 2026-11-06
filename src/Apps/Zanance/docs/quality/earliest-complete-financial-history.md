# Earliest complete financial history (D-904)

## Reproduced defect and final behavior

Capacity, essential coverage and daily-estimate suggestions consume three complete
financial months. Their original backwards traversal can construct year zero at
the earliest supported Gregorian, Persian or Hijri period. Twenty-seven new Core
cases initially produce18 actual failures and9 passes. The18 failing cases stop
at the first Capacity call; that baseline is not three independent runtime failures
per case. The final cases exercise all three original consumers successfully.

`PeriodMath.PreviousCompleteMonthsRange` validates the actual selected current
financial period, then returns only a complete preceding window. Fewer than three
supported earlier months yields no window, never a padded month or partial-history
median. Capacity retains `EnoughHistory=false`/unknown amount; essential coverage
retains `NotEnoughHistory` and the original usable balance; daily suggestions stay
null. First-year month4 has three complete months and retains the ordinary result.
All three calendars and start days1/28 are covered. An invalid selected current
financial period still throws: missing history does not make year-zero current
selection valid. Money, recurrence, account/currency/archive and financial-history
policies remain unchanged.

The existing strict `SuggestionRange` contract remains nonnullable and refuses
an unavailable window. The additive nullable `AvailableSuggestionRange` carries
absence through the original Settings snapshot constructor, worker read,
validation and guarded metadata writer. When no complete window exists, none of
the ledger rows can affect this unknown suggestion; preference and complete-account
references still participate in the original source comparison. An account added
after capture still refuses validation and a stale write. Available windows retain
the exact date predicate, complete row fields and ordering. No model/migration,
persisted schema, runtime or GC setting changes.

## Focused host and compiler evidence

- Actual .NET10.0.14/X64:27 new Core cases pass after the18-failure baseline.
- 21 new application cases pass:18 snapshot/read/validate/metadata cases across
  three calendars, two start days and first-year months1/3/4; three changed-account
  reference refusals. Settings changes are only the intended freshness metadata
  and timestamp, with complete original settings comparison and accounts/entries
  unchanged. No financial entries are saved.
- Three affected existing ordinary/year-crossing range cases pass once. No full
  suite or unrelated accepted financial workflow is replayed.
- These are48 new product tests, not75 after counting the27 Android executions.
- Strict Windows, full offline Android384AOT/408ELF/1608staticLOAD, and private
  Android192AOT/204ELF/804staticLOAD builds pass without warnings/errors. Both APKs
  pass v2/v3 signature, ZIP CRC, ZIP alignment, offline and nondebuggable checks.
  Static16KiB LOAD alignment is not hardware16KiB runtime acceptance.
- Eleven actual portable-PDB input checks bind all five changed production files
  in full/private builds and the new Core test source in the private build.
  Original x64 Mono SHAedfa02c3 and ARM64f03c6356 stay unchanged. This is source
  evidence, not whole-PE equivalence or an ARM64 execution claim.

## Actual original Settings page

A new private package on retained API36/x64/4096 runs once in PID19125/UID10267.
Its only private bootstrap changes are service capture and a clock returning
0001-03-01/UTC. Reversing these two declared textual changes gives the original
bootstrap exactly; product bootstrap and the Android system clock never change.
This is a controlled original-page check, not a real-device calendar/clock change.

The original startup first reaches Onboarding. Twenty-seven new calculations run
once and leave all20 complete source components exactly unchanged. No account,
plan or ledger setup is persisted. One new original Settings worker snapshot and
its validation run at the same injected day; the original DI SettingsPage then
becomes the actual window root. Its initial loading state is retained as pending,
not reported as a pass. The same PID subsequently reports Ready/no failure/null
suggestion and unchanged complete20component SHA
`25D59AA18FDAC784B454631CD1BC2E945CFB74E3DA3E9FF06EC04DE5F306DC97`.

The native XML contains the original Gregorian preview0001-03-01 and enabled
settings controls. One ordinary owned scroll exposes Currency and periods and
Month starts on1st. The original fresh Simple profile intentionally hides the
advanced estimate group: no visible estimate-row/glyph/ellipsis/whole-accessibility
claim is made. No hidden estimate invocation, Use suggestion, Save, auth, permission,
photo, backup, export or financial command occurs. Two source observations are
terminal state audits, not repeated calculations, page loads or setup.

## Retirement, artifacts and limits

The final same19125 packet preserves Ready/null suggestion/all20source equality.
Two exact remote XML files42287bytes are hash-bound and removed; local raw evidence
remains. Only fresh UID10267 is uninstalled. Original public package inventory,
five OS values, locale-key presence, boot, size/density and reverse state return
exactly. The retained Q02 package returns through its normal launcher in the same
PID15609; its full source/cold/performance acceptance is not repeated or inferred.
The retained large-Budget package/data remain untouched.

Five scoped standard Cleans pass for Core tests, application tests, Windows,
shipping and private native outputs. Used signed APKs, sources, compiler receipts,
raw XML and host/native output remain. No supplementary deletion or frozen scope
is touched. The full shipping APK SHA is
`fda944072a713665953dda252dd7253cbb23adb8b675273537e512a752bb907a`;
it is prepared and not installed on Main. There is no phone command in this step.
Main10611/9f8f480d remains only the last D903 observation, not fresh liveness.

Original fault cause/repair, D838 owner decision/D841 undeployed reader, whole
performance, security and remaining platform/provider/release gates stay open.
D607 and original D752/accepted successors remain terminal. The overall goal stays
active; this boundary correction is not complete delivery acceptance.

## Previous step CI receipt

The D903 single due request is consumed on6November2026. At that exact response,
[CI42154135799](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42154135799)
is in_progress, while
[service42154135875](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42154135875),
[backend42154135929](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42154135929)
and [CodeQL42154135867](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42154135867)
are completed-success. The exclusive marker/raw response/receipt remain local;
never request D903 again or infer a later conclusion. No successful job log/test
is reread. An initial local link checker omitted a same-directory link because it
required a nonempty prefix; only that checker is corrected and all four actual
new document targets resolve. This is not a product/test failure or a policy refusal.
