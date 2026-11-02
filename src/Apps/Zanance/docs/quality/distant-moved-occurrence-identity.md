# Complete distant moved occurrence identity (D-787)

## Original defect and retained failures

2 November2026. REC-14/AT-26 allows changing one original occurrence's due date;
the original occurrence editor/writer imposes no400day distance limit. However,
Occurrences.Between searches only400days around the requested range. A valid
explicit override outside that original-date window disappears even when its new
due date lies inside the requested range. Forecast, due lists, reminder discovery
and original payment validation all use this calculator.

Fourteen new in-memory cases on the unmodified implementation expose10 failures.
They include moves in both directions, preserved finite-series numbers, equal-due
ordering, original status/partial amounts and a forecast whose expected8800minor
EUR remains10000 because its moved future occurrence is absent. Boundary400 and
invalid-original/slice controls pass; they are retained as controls, not failures.

The first correction passes13/14. Keep the remaining real forecast failure: its
consumer starts the due range at the future original series start, so an earlier
moved due date is still excluded. Do not weaken its expected amount or remove it.

## Complete original source change

Retain the original bounded generation and implicit weekend/holiday padding.
Then resolve only explicit overridden identities outside that generated window
whose effective due dates lie in the requested range. Use the existing Find resolver,
which checks original rule membership, finite count and owned series slice. Never
manufacture an occurrence from a state alone. Existing generated identities are not
added twice; final due-date/occurrence-number ordering remains unchanged.

DueRangeStart supplies the complete lower bound for original due work, including
valid explicit overrides before the original series start. Foreign, invalid-rule
and unowned states cannot expand it. Forecast, commitment totals, Home, Plans and
automatic discovery use this bound. Explicit caller-supplied range APIs keep their
range semantics; no unbounded recurrence/history scan replaces the original window.

Preserve original state/reference identity, amount/paid/suppression flags and stable
reminder IDs. Original financial writers, AutoPostFrom, eligibility/selection rights,
actual-date revalidation, settlement repair and unique settlement guards are unchanged.
No formula, recurrence rule, schema/model, migration, clock, permission or provider changes.

## Focused managed verification

22 new Core cases and one new actual-data case pass;43 affected existing pure Core
occurrence/forecast/reminder/KPI cases pass. This is66 distinct selected cases,23 new,
not a full suite or66 new tests. Preserve13 first corrected passes, then continue only
the remaining/new cases. The two new reminder fixtures initially forget the original
three-day lead, so their assumed future reminder is correctly absent. Set their
explicit intended lead to zero and continue only those two failed cases; production
reminder timing is not changed to satisfy the observer.

The new data case uses its own temporary source with an original future identity
moved overdue and AutoPostSuppressed. Original processor discovery returns Posted0 /
NeedsReview1, complete source equality, empty ledger and unchanged override/suppression.
No accepted financial posting or old successful native financial cohort is replayed.

Strict selected builds, Windows Release and complete offline Android packages pass
with0 warnings/errors. Preserve the initial Windows multi-target restore refusal;
select the intended Windows framework explicitly without modifying dependencies.
The private native manifest initially resolves an absolute forward-slash path as a
relative path; correct that private path before its first successful build/install.

## Exact complete packages

| Role | SHA256 | Bytes | Own AOT / ELF / static16KiB LOAD |
|---|---|---:|---|
|Full shipping|`d5a59bff39570ebd77635502726fe578813ef7db0d48c342adcd5f33a8956968`|99458975|384 /408 /1608|
|Independent original UI|`0190aea7131a830f2d40a8f969f742891ffbc2c4a808999589caf229cb9e4d6c`|56864674|192 /204 /804|

Each actual AOT image matches its own build output before cleaning. The first static
witness reader incorrectly expects the packaged libaot- prefix in the compiler output
filename; preserve that refusal and match the same files under their actual original
names. No package rebuild or installation repeats for that reader correction.
Actual ZIP CRC, v2/v3 signature, ZIP16KiB alignment, offline/no INTERNET and
nondebuggable policy pass. The native x64 Mono remains
edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4;
shipping ARM64 remains f03c6356b2412f4879005e8877c785441bf6380e6cd79700a39c0e7264bb4542.
This is not full trimmed-PE equality, hardware16KiB or physical Main acceptance.

## Original running Home and Plans

One fresh owned API36/x64/ranchu/4096 fixture, UID10216/PID4424, boot
318480ec-b474-4ef9-929a-39eb308ae171, uses original startup, sources, XAML, models,
commands and protected data. Its independent shell-only emulator/run-bound port is
absent from shipping. Prepare only once: one Checking/EUR/opening0 account, two unpaid
Once definitions with original dates800days before/after today, and their original
ChangeOccurrence overrides to today/yesterday. No ledger/payment/authentication,
alarm delivery, camera, import or unrelated feature command is exercised.

Original Home reports DueCount2. Original Plans due/overdue mode publishes both exact
schedule/original-date identities, with the overdue row before today's row. Native XML
contains both actual titles, statuses and23.00/12.00EUR. Original Home visibly shows
"Due or overdue: 2" and next30day commitments35.00EUR, with the same two titles.
Recorded balance remains0; these are unpaid plans, not accepted transactions.
Complete20typed-source packets compare equal before/after both views, same PID/UID/boot.

Preserve the first Home XML: an original reminder-introduction dialog covers it, so
it is not accepted as visible Home evidence. Choose original Not now, granting no
notification permission; the same source remains equal. After the accepted Plans
view, continue only unfinished visible Home through its normal tab and one read-only
scroll. A text-only tab observer initially misses its actual content-description;
resolve the same native XML without repeating a successful action. No setup/save replays.

## Exact restoration and terminal retirement

All five original OS values,1080x1920/density420, boot and empty reverse map remain
exact. The initial timeout is already2147483647; restore that recorded value rather
than inventing another baseline. Five XML files54191bytes match device/host SHA256;
local raw evidence stays, remote files retire with the fresh AVD. The fresh package
uninstalls once; no own alarm, pending intent or active/blocked/posted notification
record remains. A broad substring observer initially fails on two historical uninstall
removeAutomaticZenRules lines in Zen Log; retain that failure and both historical
lines, inspect their actual section, and never clear history or uninstall again.

Own launcher83196/QEMU42300 exit,port5592 closes and official AVD deletion removes
24files/4515509497logicalbytes. Shared ADB5038/PID66176 and the phone remain untouched.
Five scoped standard Clean commands remove29286inventoriedfiles/2096738105logicalbytes;
3778owned build files/590402369logicalbytes remain. Used signed packages, private used
source and evidence stay. No supplementary deletion, frozen-scope retry/reuse or host
free-space claim occurs. PID4424 is historical after uninstall, not current liveness.

The correction is qualified only within these original rules/source/native inputs.
It is not a measured whole-Home speed improvement, original SGen cause/repair, universal
stability/security/accessibility or iOS/provider/Store/release acceptance. D752 alone
waits for owner readiness; independent work and the overall delivery goal remain active.
