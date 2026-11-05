# Exact submitted quantity and derived mass boundaries (D-863)

## Three related original quantity defects

Quantities.TryParse validates precision after Decimal.TryParse. That parser can erase
a nonzero submitted fractional tail, accepting an overprecise quantity or rounding a
value just above long.MaxValue to the stored maximum. Preserve Normalize, the original
positive decimal syntax/range preflight, culture/units and indivisibility policy; use
its normalized original digits to derive stored units and detect any nonzero subunit
tail. An exact largest stored quantity plus a nonzero tail remains outsideRange=true;
other overprecision remains a precision refusal. Redundant zeros remain valid. The
existing fractional indivisible-type output semantics are unchanged.

MassMg multiplies quantity by unit weight before dividing by1000; FineMassMg multiplies
mass by purity before dividing by10000. Their checked long products can refuse a
supported final result. Widen only each product to Int128, divide with the same signed
truncation toward zero, then narrow checked to long. For example, quantity MaxValue
and unit weight1000 must yield mass MaxValue. Unsupported completed mass still throws;
missing weight/purity still returns null without evaluating an unused overflow.
Direct mass types still return the original quantity. No price rounding, financial
writer, persisted model/schema, holding eligibility or short-position policy changes.

Asset event/type/goal readers retain the same Quantities.TryParse and existing range/
precision messages; holding detail/text retain the same mass/fine-mass calls. This
source inspection does not count as a new native form/layout or accepted Save. No
translation, UI control, permission, runtime/GC or dependency changes occur.

## Actual baseline, oracle failure and host lineage

Twenty new cases on original code expose12 actual failures: six rounded-input cases,
three supported mass products and three supported purity products. The remaining
controls preserve redundant zeros, ordinary truncation, missing metadata and final
range refusal. The first corrected product run passes18/20; two manually transcribed
large-purity expected constants are wrong by10000. This is a test-oracle failure,
not two residual product defects. Independent integer quotient/remainder calculations
verify MaxValue*9999/10000=9222449699651090329 and
MinValue*9999/10000=-9222449699651090330 with truncation toward zero.

Only those two corrected expectation cases continue on host; all18 prior successes
are retained without replay. Fourteen directly related original quantity boundary/
range cases pass. Thus20new+14related selected host cases are qualified in lineage,
not all20new on one final host binary or a full-suite rerun. Original baseline Core/
Quantities source, incorrect expected values in raw test logs, independent integer
oracle and corrected current source remain. Nonexistent guessed import/report/viewmodel
path lookups are discovery failures, not product or permission failures.

## Same original native process and unchanged protected source

Retained emulator5606/API36/x64/kernel4096/boot
6663f98e-6546-4809-a0bc-285f11a96674 receives only one install/normal launch of fresh
pro.vafadar.zanance.quantityexactreview UID10251/PID31691. Twelve initial readiness
observations still show StartupPage; no check is issued. The PID is independently
confirmed live and unchanged. Continue that same process until original Onboarding;
no reinstall/relaunch, setup replay or automatic Retry. One exclusive check then runs
all20 exact current tracked case methods, including the corrected numeric oracles,
through the private original-app port. All20 pass on this final native binary.

All20 typed source components match before/after; Accounts/Entries/Schedules0, source
SHA256 C87FC598DA3A194F0C4F6433003339D8ADDA61D4CF634D0B830F5B2FE3C542A3.
No account/type/event/ledger preparation or financial Save occurs. Signed negative
helper controls qualify original arithmetic only, not new accepted negative holdings.
This is original calculation/empty-source evidence, not populated editor/layout,
physical ARM64, hardware16KiB, private Main or general security/performance acceptance.

Strict Windows/full offline Android/private builds have no warnings/errors. Full
shipping SHA256 a214b1041ea1a3a8ce00599eaed0a9c54b74506386b753a9f22d3f57393814ec,
99491743bytes/384AOT/408ELF/1608staticLOAD. Actual used private SHA256
ea843ca76db25b7b128af25785466800f24d37fee466fccff0e106a9b1f522cd,
56885154bytes/192AOT/204ELF/804staticLOAD. Offline/nondebuggable/v2v3/CRC/ZIP alignment,
original Mono and nine own App/Core/Data AOT bindings pass; no whole-PE identity is
inferred. Private MauiProgram reverses exactly after its sole DI-container capture.

## Terminal scope and remaining prerequisites

Only freshUID10251 retires after its same-PID/source proof. Original package UIDs,
five OS values, locale-key presence, boot, geometry, reverse and shell UID match.
Four standard Cleans pass. Used signed/current/private source and local evidence
remain; zero XML/supplemental/frozen deletion. No original record/R13/provider/history,
phone, reset/reboot/server start/reconnect/pair, authentication, photo or financial
operation is repeated. Existing Main10720/e848 is only the dated D861 checkpoint;
shippinga214b104 remains prepared, not installed. D838 decision/D841 unexecuted reader,
original fault, full performance/security/accessibility/external/platform/release
gates and the overall active goal remain separate. D861's sole response records
CodeQLsuccess/generalCIcancelled; no second request or successful-job log replay.
The new commit receives one marker-bound read-only CI check after its due time,
without polling or a job-tracking automation.
