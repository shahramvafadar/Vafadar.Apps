# Exact wealth adjustment cancellation (D-862)

## Observed independent defect and bounded correction

WealthHistory.Explain already accumulates corrections in Int128 and checks the
completed public component, but its nested adjustment-only Enumerable.Sum still uses
long. An opening balance can offset every valid chronological balance while that
adjustment-only prefix overflows. For example, opening-20 and adjustments MaxValue,
+20,-20 keep every balance supported and the completed correction exactly MaxValue;
the original explanatory sum refuses on the second adjustment. The negative mirror
and offsets2/20 establish four actual failures, without a persisted financial write.

Replace only that nested sum with an Int128 Aggregate. Keep the original exclusive
start/inclusive end, account effects, before-opening exclusion, source order, opening
corrections, flow/price/FX classifications and final checked long publication. The
original LedgerCalculator.Balance still checks every chronological prefix: an invalid
balance is never rescued by this explanatory change. No writer, stored value/type,
schema/migration, UI/translation, runtime, GC, dependency or financial policy changes.

## Failure lineage and focused host verification

Initial new-test compilation uses the enum name as a member and fails CS0117; correct
it to the existing LedgerEntry.Direction before executing any case. The corrected
baseline runs8 cases:4fail/4pass. After the product correction, all8 pass in actual
.NET10.0.14/X64. Two related original unsupported-component cases also pass. These are
10 selected host cases, not a full suite or another D857/D858/D859 suite execution.
Retain the original baseline Core/source, test outcomes and current input bytes.

The eight cases cover four representable signed cancellations, two unsupported final
corrections, unchanged range/account selection, and refusal of an invalid balance
prefix. The cancellation assertions also retain input balances/effects and verify
zero flows/remainder. Read-only discovery initially supplies an invalid wildcard path
and two nonexistent guessed filenames; those lookup failures are not product failures.
A separate official concurrent-SGen issue review establishes no new cause-specific
remedy; no collector/runtime trial or old record interpretation follows it.

## Actual original Android calculation and complete artifacts

On the retained API36/x64/kernel4096 emulator5606 with original boot
6663f98e-6546-4809-a0bc-285f11a96674, fresh
pro.vafadar.zanance.wealthadjustmentreview UID10250/PID31002 runs the exact eight
tracked case methods once through a private original-app calculation port. The
original Onboarding root remains; no financial database preparation occurs. All20
typed source components match before/after, Accounts/Entries/Schedules0, source SHA256
563653997AD707FCF7048C51C07A62404800928440519AD1436F3D1EF4CA7A8F.
This is calculation execution/empty-source preservation, not populated Reports UI,
private Main source or current physical ARM64 case acceptance. The private App source
reverses exactly to original MauiProgram after its sole service-container capture.

Strict Windows/full shipping/private Android builds pass without warnings/errors.
Shipping SHA256 bcd4b1e33cce355c6417cfe82d3649eca8626f78b40d876023322db5fd7a119b,
99491743bytes/384AOT/408ELF/1608staticLOAD; actual used private SHA256
c3f8fd3acf9cc2127ecb9381dc52377a069c97568b4e4008ee315455ed3f311b,
56885154bytes/192AOT/204ELF/804staticLOAD. Both pass offline/nondebuggable/v2v3,
ZIP CRC/alignment and original Mono checks. Nine actual own App/Core/Data AOT members
match their output artifacts. Whole-PE identity and runtime hardware16KiB are not
inferred. Package verification follows the original completed publishers; no accepted
calculation, signature or ZIP check is replayed to recover an observer error.

## Preserved boundary and completed cleanup

Only freshUID10250 retires after its source/PID proof. Original emulator public package
UIDs, five OS values, locale-key presence, boot, geometry, reverse and shell UID match
exactly. Four standard Cleans pass; used signed packages, private/current source and
local evidence remain. No XML, supplemental deletion, frozen-scope access, original
D809/D833 provider/history/record/R13 read, reset/reboot, server start/reconnect/pair,
phone/authentication/camera/financial Save or old successful cohort replay occurs.
Prepared shipping is not installed on Main; Main10720/e848 is only D861's dated public
checkpoint. D838 decision/D841 unexecuted reader, original native-fault cause, complete
performance/security/accessibility/external/platform/release gates and active overall
goal remain separate. One read-only CI check may consume its marker after the due time;
never poll or create a job-tracking automation. D860's sole response records CodeQL
success and general CI cancelled; its consumed marker remains and no second GET occurs.

The documentation observer initially expected ten Markdown links; nine actual links
and one AGENTS backtick path exist. All targets resolve; the count is corrected
without rewriting the finished runtime work.
