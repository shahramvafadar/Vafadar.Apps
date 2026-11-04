# R13 signal-field interpretation boundary (D-816)

4 November2026. This corrects a diagnostic scope defect; it does not read the original
system record again, repair the app/runtime or establish an original crash cause.
The owner-approved D809 extension admits saved R13 and two fixed labels at exact
Mono topPC2f0f66. Fault-address interpretation at that new PC is explicitly excluded.

## Actual defect and correction

D809 v2 returns refused-unknown from classify_fault before parsing the new PC.
However, classify_slot independently calls the shared class_signal helper. That helper
still interprets Signal.has_fault_address/fault_address and writes the latter into
ClassSignal.address, even though the final fault_class output is suppressed.
The prior claim that no fault-address interpretation occurs at this PC is too broad.
No raw address/register/memory was exported; the retained actual output is unchanged.
Do not infer that this defect caused the original two unknown labels or the app fault.

The official [Android16 schema](https://github.com/aosp-mirror/platform_system_core/blob/android16-release/debuggerd/proto/tombstone.proto)
separates Signal.code at field3 from has_fault_address/fault_address at8/9. Retrieve
and hash the actual source bytes with time; this branch reference is not claimed as
an immutable installed-system identity. The correction adds a code-only signal helper
for the exact R13 PC. Only field3 has semantic meaning there; other signal fields are
wire-skipped, including fault-address presence/value and adjacent memory metadata.
Code duplicates, wrong wire type, malformed encoding and unsupported/omitted codes
retain refusal. The existing RSI branch still calls the original full signal validator.

Only the reference header changes, in two reversible source edits: the dedicated
helper and exact-PC dispatch. Reversing both yields the prior header exactly. Original
record/package/file/thread/module/frame admission, signal-number parsing, fault-class
new-PC early refusal, scalar/input wiping,128-register/4096-map bounds and original
RSI behavior remain. There is no generic register selector or new actual-data authority.

## New synthetic evidence

Five new synthetic baseline inputs expose five actual boundary failures: absent/false
presence with nonzero address, nonboolean presence, duplicate presence, duplicate
address and an opaque address payload all wrongly influence R13 classification.
Each baseline output is preserved as unknown. Correcting the header makes those cases
independent of the excluded fields; the original package/source input is never used.

The corrected component passes eleven new synthetic cases plus two genuinely affected
old-dispatch controls. New cases also cover opaque adjacent-memory payload, duplicate/
wrong-wire/omitted/unsupported signal code and malformed skipped wire. The malformed
wire is rejected by the unchanged original record guard with exit67/no output; it is
not a successful record classification. Other results contain only the existing fixed
allowlisted protocol fields and no forbidden synthetic sentinel. The two RSI controls
preserve an ordinary mapped-readable result and duplicate-address refusal under its
unchanged earlier authorization. No old29/55/79-case suite is replayed.

Baseline synthetic, corrected synthetic and unbound actual paths compile with strict
warnings-as-errors and four static16KiBLOAD segments each. Only the two synthetic
binaries are transferred/executed as shell UID2000 on the existing owned API36/x64/
4096 emulator. Their catalog has a fictitious package and all-zero APK digest. The
corrected actual-bound binary is retained locally and never deployed or executed.
These thirteen are private diagnostic component cases, not new product unit tests,
real crash injection, actual record interpretation or a shipping/runtime change.

## Preservation and causal limit

Fifteen exact-hash temporary remote files/51278logicalbytes retire; their fresh remote
directory is absent. Local versioned source/binaries/new fake packets/raw results and
five failures remain retained. Preexisting package/UIDs, original D809 V3 APK, boot,
five OS values, geometry, reverse and shell UID2000 match exactly. The retained AVD,
userdata and original system record remain; no new app install/launch, root, actual
record access, phone, authentication, financial/photo replay, general cleanup or
frozen-scope action occurs. No full managed/Windows/shipping build is needed for this
private diagnostic-only change and no previously successful product test is repeated.

D809's actual unknown labels remain terminal and are not reread to seek a better
answer. Their underlying cause is still unknown; this source correction supplies no
actual alignment/readability, object validity, earlier writer, common fault origin,
crash remedy or whole performance/security/platform/release acceptance. Overall goal
remains active and independent work continues without waiting for CI or owner presence.

Related: [original exit and bounded R13 observation](current-large-budget-native-exit.md),
[original slot contract](bounded-reference-slot-classification.md),
[concurrent descriptor admission](original-concurrent-descriptor-correction-admission.md),
[remaining delivery frontier](remaining-delivery-evidence.md).
