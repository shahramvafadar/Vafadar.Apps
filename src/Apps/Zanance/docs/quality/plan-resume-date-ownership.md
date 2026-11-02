# Original plan-resume date ownership and visible refusal (D-800)

## Original defect and required behavior

2 November 2026. The original resume date field accepts a date before PausedFrom.
PlanActions.Resume previously ended the predecessor on PausedFrom minus one day,
but started its continuation on the earlier requested date. The two distinct plan
identities could then own the same original dates. Public SaveSplitAsync also accepted
a forged overlapping or unbounded predecessor. This demonstrates invalid ownership,
not an observed duplicate financial payment: no such payment is made in this batch.

Reject a restart before the original pause before copying or mutating the schedule.
At the atomic store boundary, require a bounded predecessor ending strictly before
the continuation starts, before opening the writer. Existing rule anchors, valid
same-day/later restarts, pause gaps, linked identities, retained settled history,
commercial checks and original atomic transfer of history remain unchanged. This is
an input/write safeguard, not a new financial policy, migration or historical repair.
Existing stored source is not silently rewritten.

The original action banner was visible only when Actions.Result was nonnull; a
refusal has no accepted result. Publish HasFeedback changes for either actual result
or refusal, and hide the empty result label independently. Add one explanatory date
message in all six languages through the original resource workflow. Source entries
are distinct and nonempty; six native language screens are not claimed.

## Focused baseline and final continuation

The original strict test build succeeds. Fourteen new cases execute: eleven fail and
three valid boundary controls pass. Six failures cover before-pause domain dates with
and without automatic posting; four cover forged SQLite overlap/unbounded ranges;
one original action wrongly accepts the invalid restart. The fixture has one zero-
opening account and one paused definition, with no ledger preparation or auto-post run.

The first correction passes all fourteen. Two existing affected cases pass separately:
the original pause/resume/end/delete flow without financial entries, and the isolated
SQLite recorded-settlement-history resume control. The latter is needed because the
shared split writer changes; it is not a physical financial replay or a broad suite.
Total sixteen distinct selected cases, fourteen new.

After the banner change, only the affected new action case continues, including actual
HasFeedback notification. Actual Android then discovers a remaining presentation
failure: an expected date refusal is classified as a failed source read. Preserve
PID4679's real Ready=false/read-cover result, unchanged raw date and full typed source.
A strengthened host assertion separately fails with ArgumentOutOfRangeException in
Failure. Classify only that known unaccepted resume-date input as inline refusal,
leaving Failure null; all technical/source/accepted-follow-through failures retain
their original covered handling. Only that changed action case continues and passes.
The counts are distinct cases across versioned checks, not sixteen executions on one
final binary. Unchanged successful domain/writer/history cases are not replayed.

## Original Android page, writer and retained-source limits

One fresh official API36/x64/ranchu/4096 fixture prepares one zero-opening account and
one paused daily plan once. Original startup, AppShell, PlanDetailPage, view model,
action and PlanStore run; a private owned review sets the original raw resume date to
1 November while the actual pause is 2 November. This is controlled date preparation,
not a claim about native date-picker input. A real visible Resume plan button is tapped.
Original UID10216/PID4679 refuses the input and preserves full20 typed source, but
incorrectly enters the read-failure cover. The same process then rejects one forged
overlapping writer, still with full20 source equal and zero entries. That successful
writer check is never repeated.

One necessary no-reset upgrade continues the same fixture at PID5528. UID, CE/DE and
public permission flags match; actual unlinked Core/Data PE bytes also match across
that UI-only correction. Original preparation/account/plan/writer operations are not
replayed. A read-only adoption validates the retained original plan identity and
selected shape, then captures the current full20 source for the remaining UI check.
The old producer exposed no full digest/packet for cross-version comparison:
**full typed source equality across the upgrade is unproven**. Do not turn matching
UID/CE/DE, selected facts or unlinked PE bytes into that stronger claim. Original V1
comparisons and current V2 comparisons remain separate; no comparison is weakened.

An actual original startup reminder offer covers the V2 target. The first target
lookup refuses before any Resume tap; Not now dismisses only that owned app offer,
without permission grant, scheduling a reminder, changing OS settings or altering the
full20 source. Continue only the unfinished target. One final real Resume tap shows
the exact date-error TextView at [42,231][1038,319] inside1080x1920, with Ready=true,
raw1November retained, Resume still enabled, Result=null and no accepted transition.
Full20 typed source since the current read-only adoption remains equal, one account/
one paused plan/opening0/ledger0. This is caption/bounds/readiness evidence, not glyph,
ellipsis, all-language accessibility or a valid financial-action acceptance.

## Builds, packaging and retirement

Initial build commands incorrectly propagate TargetFrameworks to non-MAUI references
and fail NETSDK1005. Preserve them; corrected original single-target commands use only
the MAUI target selector. Later builds are necessary for the actual native-discovered
input/read classification correction. Final strict Windows and both Android publishes
have no warning/error diagnostics. No runtime/GC, schema/model or permission change.

Final complete offline shipping is99458975bytes, SHA256
6a5574d1c9c0345fcbdccaac088ec0eb6750710c8de51cbf323256cb4e713890,
384 own AOT/408 ELF/1608 static LOAD checks. Final independent native is56872866bytes,
SHA2568f5b3175037b1d09beb8bc3c7a2f5fab61b66e27477dbabbe94568d45a98f022,
192 own AOT/204 ELF/804 static LOAD checks. Both pass actual v2/v3 signature, CRC,
ZIP16KiB, offline/no INTERNET and nondebuggable checks. Used V1 native is retained
separately:56868770bytes/SHA30cbbc39fedad858bba428192421b33f9347a60e48d0f4201586d1f87486b4fd.
The first shipping candidate is not adopted or substituted for final UI acceptance.
Mono stays x64edfa02c3/ARM64f03c6356. Static alignment is not hardware16KiB acceptance.

The final five OS values, boot, geometry and reverse configuration match exactly.
Five owned XMLs/158448bytes are hash-matched before remote removal and proved absent;
local raw/source/packages stay. Only fresh UID10216 retires. Official AVD deletion
removes25files/4681774472logicalbytes; actual host83604/QEMU76436 exit and port5594
closes, while sharedADB5038/PID66176 stays. Five standard Clean operations remove
29150files/2114539488logicalbytes;3740files/588753669bytes remain across four new build
roots. No supplementary deletion or frozen-scope action is attempted. Counts are
logical bytes, not measured free-space gain.

## Current physical Main adoption

A public readiness observation finds the reserved Samsung awake/unlocked on D799/fa32ba0a.
The first Home observer incorrectly requires New expense as text rather than its
original content-desc; it refuses before installation. Preserve that failure and
use the same raw141-node capture with its actual semantic caption; no UI command or
financial case is replayed. Exactly one install-r succeeds and one normal launcher
entry starts Main19128, replacing16195. Every public attribute of the immediate Home
baseline matches after upgrade. Main UID10380/CE155299/DE154603, grant/stopped flags,
all other Zanance package bindings, five OS values, boot and geometry remain exact.
No setting write, private Main database/key read, extra Profiles matrix or installer
prompt interaction occurs. Two new phone XMLs/103864bytes are hash-matched before
remote removal; all are absent and local raw remains. This is separate from the five
owned-emulator XMLs. A separate D752 authorized Main-upgrade receipt records the
legitimate code difference without rewriting its original checkpoints or launching,
querying a provider or releasing that interrupted frame.

No owner authentication, native financial Save/payment/import/Undo, new OCR/camera,
notification timing or old successful native financial cohort is run. The complete
6a5574d1 candidate is now physically adopted, with public UI/package/OS acceptance;
private-source or every financial/device-path acceptance is not inferred. Accepted
successor CSV/backup cancellations stay retired. Legitimate PDF input, original SGen
cause, full Home speed/security/accessibility, iOS/provider/Store/release gates remain
open. Goal active.

Related: [previous physical Main](receipt-repeated-separator-boundary.md),
[accepted sensitive cancellations](physical-sensitive-cancellation-completion.md),
[current execution frontier](remaining-delivery-evidence.md).
