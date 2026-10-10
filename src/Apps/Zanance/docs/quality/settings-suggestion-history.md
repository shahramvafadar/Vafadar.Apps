# Exact settings-suggestion history (D-106 / AT-111)

## Change and financial scope

Settings previously materialized the complete ledger to suggest daily consumption, though SuggestPerDay uses only
the last three complete financial months. Settings now reads every entry in that inclusive date window through the
existing indexed GetEntriesAsync query. The range follows the current display calendar and profile MonthStartDay;
the current partial month, older history and future entries cannot affect this suggestion.

The existing calculator is unchanged: the median of daily net consumption, usable unarchived accounts in the estimate
currency (default currency only without an explicit estimate currency), refunds reducing consumption, transfers and
income excluded, plan settlements excluded, and null without eligible three-month account history. IncludeInTotals
does not override the explicit usable scope. No result cap, schema/model change, new storage, permission or SDK.

Settings still reads complete preferences/accounts/device availability before synchronous publication enables the
form. The covered loading/error/retry boundary, current PIN/notification state and unsaved estimate rules remain.
Opening/reloading never saves a suggested value or posts money. Suggestions remain optional explicit-Save drafts.

## Actual bound Windows measurements

Three independent hidden own-app processes per shape/build, one warm Settings reload per process. The fixed QA-06
fixtures contain 10,000/100,000 entries and 20/200 accounts across 1,000 days. The actual SettingsPage BindingContext
is measured after its first read. Every load reaches ready publication; complete settings/entries JSON stays equal.
Preserved development database/WAL/SHM files return with exact hashes. Temporary probes are removed before final builds.

Milliseconds, medians of three independent samples:

| Complete ledger | Measured stage | Before | After |
|---|---|---:|---:|
| 10,000 | Required-entry read | 114.80 | 13.74 |
| 10,000 | Bound LoadAsync | 266.08 | 99.86 |
| 100,000 | Required-entry read | 790.92 | 70.78 |
| 100,000 | Bound LoadAsync | 883.78 | 157.74 |

The 100,000-entry warm bound LoadAsync median falls from 883.78 to 157.74 ms, about 82.2%. The timer includes reads
and synchronous bound publication, not subsequent native arrangement/painting, cold startup or Android execution.
These observations do not establish the Q-02 two-second objective, close the historical ANR finding, or prove phone/iOS
acceptance. Remaining complete-history reads and native publication are separate QA-06 work.

## Behaviour and final verification

Twenty-one AT-111 cases compile the actual application snapshot and use isolated real SQLite/localization. They cover
Gregorian/Persian/lunar Hijri, month start 1/25/28, cross-year and leap-month boundaries, first/last-date refunds,
negative net consumption, plan settlements, transfers/income, archived/unusable/foreign accounts, explicit/default
currency, insufficient history, fresh calendar/start/currency/clock and complete stored-row equality. The bounded read
is compared with the unchanged calculator over full history and independent known medians/range boundaries.
The initial fixture tried writing into an already archived account; that write was correctly rejected. The fixture
now records history before archiving, without relaxing production validation. All 21 focused cases and main 1,512
tests pass, zero failed/skipped (App.Tests 276); main outputs are cleaned afterwards.

Final strict Windows and complete canonical Android Debug/Release builds pass with zero warnings/errors;
temporary timing probes are absent. Windows en/fa/de x light/dark x 360x800/412x892/1280x820 at process-local 200%
text, plus en/fa/de normal-text/360/light: 21 contexts, 1029 own-app renders. All 21 bound suggestion, 21 native
retry/live-choice and 21 reopened-language proofs pass; the latter invoke 84 native selections and 84 native Back
actions and retain retired shells explicitly. The standard small walk-through history has no eligible three-month
suggestion; its null caption is compared with full history. Raw estimate/period drafts and complete settings,
accounts and entries remain, with original development database/WAL/SHM files restored with exact hashes.

Complete signed Release on the owned emulator: six en/fa/de x light/dark reference contexts and English/System
tenfold. Actual positive suggestion captions and native Use suggestion update only the unsaved per-day estimate;
Back/reopen returns the stored estimate and the same freshly computed suggestion, with no Save. Only emulator-5570
is driven, at original normal scale; no system/security choice or foreground secure flag is changed. All three
original fictitious profiles retain complete original table contents (including financial metadata); Settings values
remain, and audit-only differences are reported separately:
reference=false, tenfold=false, prior=false.
The original QA03 Native English/System/Advanced profile is restored. Final Release cold English Home, three
native transaction rows, translated Back, secure foreground flag, density 420/font scale 1.0 and Home-stopped pass.

Installable local-test APK: `artifacts/android/zanance-d106-release.apk`, 80,918,443 bytes, SHA-256
`7542dbe80b6da16a3a16379f1dbc124368f1549ba9c6035211f6a291a2073b8c`. Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 assembly
stores and app AOT, ZIP integrity, non-debuggable package and v2/v3 signatures verified. Certificate SHA-256
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is the local test certificate, not production approval.

QA-06 remains in progress for cold/ANR/device/platform measurements and other native publication/history reads.
Physical phone/iOS, actual OS scaling/readers, provider/owner and release acceptance are independent. CI is reported
by one delayed exact-commit check after push; no result is inferred from local builds.

The first Windows driver's broad failure-file filter rejected the deliberate covered-error screenshots from the
settings-display fixture. Those complete first-context proofs were retained and the filter corrected. A later
reopened-language observation failed with UIA_E_ELEMENTNOTAVAILABLE on a manually constructed data peer; a visual
container peer prototype then correctly failed because it exposed no SelectionItem pattern. Neither failed folder
is accepted. The final Debug review waits for the current popup item, uses WinUI's associated ComboBox peer and its
existing data child, and still selects through the real native SelectionItem pattern. Retired pages/handlers fail
explicitly; selection errors are not caught or replaced by assigning a view-model selection. No product selection,
translation or financial behavior is changed by this diagnostic correction.

The associated-peer review completed the two 360 px cohorts and the 412 px light cohort, then encountered an earlier
language virtualized outside the popup viewport during the 412 px dark return. Waiting alone could not realize it.
The final helper uses the data peer's native ScrollItem.ScrollIntoView before bounded arrangement and SelectionItem;
the 412 px cohorts, wide cohorts and normal-scale cohort are checked with that helper. Completed 360 px contexts
retain their actual associated-peer proofs over the same production snapshot/calculator code; the incomplete dark
412 px cohort is excluded. [WinUI's documented data-peer patterns](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.comboboxitemdataautomationpeer?view=windows-app-sdk-1.8)
provide ScrollItem/VirtualizedItem and SelectionItem. This remains a native review correction, with no product rule
or financial-source change.

The first normal-scale cohort also ended before its third reopened-language proof because scrolling had been
requested immediately after Expand, before the popup item host had any arranged slot. That incomplete cohort is
excluded. The final helper observes an actual current popup slot before native ScrollIntoView, then the target slot
before native SelectionItem. The fresh normal-scale three-language cohort passes with all complete stored rows and
drafts retained. Completed large-text contexts exercised the same production code and native patterns; only the
Debug observation ordering changed.

The separate selected-picker caption clipping observed at 200% in the Persian 360 px capture remains A11Y-03 work;
the settings-suggestion checks do not claim full selected-caption geometry acceptance.

The first Android attempt failed before installation because the previously used emulator was stopped. That attempt
is not runtime acceptance. The same existing independently owned Zanance virtual machine was located and started
headlessly with its existing sample data; no download, physical-device action or device setting change was performed.
