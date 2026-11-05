# Exact goal share-percent drafts (D-845)

5 November2026. Correct the complete original goal-editor path for stored share-of-
income percentages: display precision, exact input, retained culture and field feedback.
No Core/Data/writer/ledger/schema/migration/runtime/GC or third-party policy changes.

## Reproduced original defects and controls

Load formerly formats plan.Percent with `0.##`, hiding stored precision and displaying
valid tiny percentages as zero. Save then rejects that displayed zero, even for an
unrelated name-only correction. Partial typing also unconditionally captures the new
display culture: after German `0,5` changes to English formatting, appending a zero can
parse `0,50` as50 instead of0.5. Original NumberStyles.Number grouping/sign syntax is
otherwise an existing contract and is not silently removed.

The first40 new original-editor cases expose25 failures:18 rounded loaded-text cases,
four rounded fractional input cases in en/de, one tiny name-only metadata correction,
one changed-culture partial edit and one Persian keyboard/suffix case. Existing exact
range/underflow and accepted syntax controls are retained as controls. Arabic separator
normalization previously makes the fa formatting variants refuse before a precision
check; that is not counted as reproducing inexact acceptance in those variants.

Three additional distinct cases use `100.` plus26 zeroes plus`1` (100+1e-27). Their
framework precondition really parses100, and two original en/de goal saves wrongly
accept it; the fa variant already refuses under the old normalization mismatch. Thus
43 original cases expose27 real failures across40+3 executions, not one43-case run.
No existing financial success, payment, allocation or original acceptance is replayed.

This also corrects D843's example: `100.00000000000000000000000001` is100+1e-26,
exactly representable and already rejected by the maximum100 check. Its original
Android refusal is a range control, not new native rounding evidence. The historical
[report](exact-account-rate-editor.md) now says so; original packets/run counts remain.
D843 loaded native text/name-only memory precision and genuine host failures stand.

## Final implementation and host lineage

Display the complete actual stored decimal using its original culture. Capture that
culture immutably and renew it only after complete clearing, never partial typing.
A visible original-format note uses the existing six-language Numeric_OriginalInputFormat.
PercentError in all six languages now explains the existing >0/<=100 range and exact
storage without rounding. Native page uses the original wrapping/semantic styles.

After Digits.ToAscii keyboard normalization, normalize only Arabic decimal/group
separators in a parser NumberFormatInfo clone when necessary. Preserve original
NumberStyles.Number sign/grouping grammar, including its existing exact meanings.
Only after successful positive/range parsing compare exact fixed-point magnitude:
extract digits on each side of the actual decimal separator, ignoring accepted sign/
grouping spelling and redundant zeroes. The parsed invariant decimal must have the
same magnitude. No precision guess, epsilon, implicit percentage rounding or culture
switch is accepted. Plan/goal/consent/source/day/rights/receipt/financial policy remains.

The first corrected .NET10.0.14/X64 run passes44 of50 new cases. Six remaining string
mismatches expose an oracle mistake: the store already normalizes a redundant trailing
seed zero. Correct those expectations to the actual original stored decimal text plus
independent seed numeric equality; do not alter storage or weaken source comparison.
Preserve the first50-case result and rerun only those six corrected source cases.
Together with four directly affected prior hidden-plan/money-context cases, all10 pass.
Evidence is50new+4affected across this lineage, not54new, a full suite or all54 on a
single final binary. The44 successful cases are not replayed after the oracle correction.

## Actual native form and preservation

Fresh API36/x64/4096 UID10241 prepares exactly one fictitious earmark goal/one retained
share plan, with target1000EUR minor and percent1e-28, plus Advanced mode. No account,
ledger entry, schedule, allocation or payment is created. Original App/Core/Data/page/
model/XAML are included directly; the private bootstrap has one reversible DI capture
only, compared after explicit line-ending normalization.

Initial PID23666 publishes the page before its id query. Original OnAppearing calls
EnsureLoadedAsync with an empty query; a later different id correctly retires that
route. The failed/error-covered observation has0Save clicks and remains preserved.
Correct only the private caller ordering to query-before-page, matching navigation;
never weaken the original route guard. One necessary no-reset upgrade to23926 keeps
UID and requires the exact existing20source before opening the same goal. No setup or
successful operation is repeated; initial private nullable CS8604 refusal is separate.

The original form reaches Ready. Actual native Entry retains the entire1e-28 value
at [87,1553,994,1669]. Change only this disposable app's formatting to de-DE, append
one native text zero and retain the original dot parser/raw text. The original-format
note initially extends outside the scroll viewport, so that observation is disqualified.
One remaining owned ScrollView gesture brings the complete note to[87,900,994,1023],
no ellipsis. Neither the formatting change nor input is repeated to improve evidence.

A different native input100+1e-27 replaces the nonempty draft while retaining that
original parser. One visible enabled original native Save is pressed once. The page
remains Ready, raw text stays, no generic cover appears, and its complete current
English PercentError is visible at[87,1018,994,1106], no ellipsis or overlap with the
note. Six translated host captions are not six native screens or glyph/OS200%/whole
accessibility proof; bound Entry text is not a claim about every simultaneously visible glyph.

All20 declared typed protected-source components stay equal before/after validation,
SHA256`57EAE64AA2A031C6E6FCF67A8A2ACFD961EEE74839D26ED6432E23BC4C86A89C`, including
the no-reset admission. This is not whole schema/cipher/WAL/key/all-table equality.
Restore the original FormattingCultureName null, return to original Onboarding and
retire only fresh UID10241. Original AVD/packages/boot/five OS values/locale-key presence/
geometry/reverse/shell compare exactly. No phone/old cohort launch/R13/history read,
emulator/server/reconnect/pair/reboot/reset or accepted financial replay occurs.

## Strict artifacts and continuation

Windows, complete offline Android and corrected private builds pass without warnings/
errors. Nine actual App/Core/Data AOT ZIP members match current build artifacts;
no cross-build managed PE identity is inferred. Private provider absent from shipping.
Final packages pass original Mono identity, v2/v3, ZIP CRC/alignment, offline/nondebuggable
and static16KiB LOAD checks, not actual16KiB hardware qualification.

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Final private native |8e6c4f3621dc2ff32890c74eca1fbbfa006bb8fab2ae8a22296e299daeaf5c92|56901538|192/204/804|
| Complete shipping |868f06c090c1a9bc709a1f3b0abf03a00bdb96a9e16274d5f17aab0d821e321e|99479455|384/408/1608|

Four standard Clean profiles pass: tests384, windows1172, native1282, shipping1289 deletion lines, not unique physical
byte totals. Used versions/source/APKs/observations, the nullable/route/scale/clipping
failures and historical correction remain. No supplemental deletion or frozen scope.
Current868f06c0 is prepared, not Main; D8425939/22a is still only the last dated Main,
not fresh liveness/private-source evidence. D844 sole CI41972228423 is in_progress and
CodeQL41972228426 completed-success; no second request or successful job log is read.
Pending D838 actual authority/D841 unexecuted/D809 unknown/D833 retained source remain
protected. Broader original fault/performance/security/platform/provider/release gates
and the overall goal stay active.

Related: [manual-rate input](exact-manual-rate-input.md),
[original goal editor](complete-original-goal-editor-source.md),
[current Main](current-interest-arm64-main-adoption.md),
[canonical backlog](../enhancements/2026-10-commercial-release/04-backlog.md).

Later [D846 physical qualification/adoption](physical-goal-percent-main-adoption.md)
qualifies the current original goal page on ARM64 and adopts868f06c0 as Main15641
without reset. The prepared-only statement above is the historical D845 checkpoint.
