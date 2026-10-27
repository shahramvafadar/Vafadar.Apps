# Tag-list allocation candidate rejected at the host gate (D-592)

Status: candidate closed before product change, app packaging or native measurement.
The complete Home/two-second goal is not accepted by this small host experiment.

## Distinct source opportunity and fixed eligibility gate

Current derived block decoding constructs the original LedgerEntry, whose Tags
initializer already creates a fresh mutable empty list. The decoder then replaces
that list with another list even for zero tags. Investigate only reuse of the
already-owned instance: retain null for the original negative tag count, keep the
fresh empty list for zero, reserve its capacity for nonempty tags, and preserve
all original element/null/duplicate ordering. Do not introduce pooling, global
retention, a partial ledger projection, field omission or financial aggregation.
This is distinct from rejected scratch pools/shared indexes/parallel workers.

Freeze the actual original codec, owned expansion buffer, LedgerEntry, Entity and
auditable contract sources at d74f1f97. Keep both exact original copies and a local
candidate changing only that tag-list initialization line. Production sources are
never changed; the existing domain/EF model and released migrations stay exact.
Use identical isolated host harnesses, real original Encode/DecodeWithBuffer and
no duplicated financial algorithm, fake MAUI controls or private Main data.

Declare a5% host allocation eligibility gate before building/running. This is only
a cheap prerequisite to considering further work, not a replacement for the
separate existing complete native Home speed/dispatcher/memory/preservation gates.
A host-only improvement cannot adopt a candidate or establish the two-second goal.

## Original-code oracle and one paired host comparison

Both fresh isolated strict Release builds have zero warnings/errors. Execute each
role once in a distinct process on the actual10.0.14/X64 runtime. Test two declared
512-row source shapes: empty tags and mixed empty/null/duplicate/nonempty/null-
element tags. Include all32 original fields, undefined enums, extreme amounts,
nullable identities/dates and non-ASCII text. Original JSON field/order comparison
before/after the interval and source/decoded/per-read tag mutability/independence
guards pass. The encoded source SHA256 is identical between roles for each shape.
No synthetic benchmark result is presented as a new shipped application test count.

Warm only each fresh measured path, then allocate16 blocks/8192 rows per shape.
GC.GetAllocatedBytesForCurrentThread brackets only actual decoding; full oracles
and serialization remain outside the allocation interval. Preserve host elapsed
values as diagnostics, not a native speed claim or a reliable warm/cold median.

| Shape | Reference allocated bytes | Candidate allocated bytes | Reduction |
|---|---:|---:|---:|
| Empty tags |5047904|4785760|5.1931%|
| Mixed tags |5383776|5159520|4.1654%|
| Combined declared shapes |10431680|9945280|4.6627%|

The combined result is below the frozen5% host threshold, and the mixed case also
fails5%. Do not select only the favorable empty case, relax the gate, add samples,
run a rescue/reverse experiment or justify a shipping change from a tiny local
allocation saving. Close the candidate here: no production patch, app build, APK,
phone/emulator command, financial/seed/key replay or unrelated passing test suite.
Preserve the failed eligibility result and original local source/results. Every
production input still matches its original frozen SHA256.

## Owned-output cleanup and refusal

Standard Clean of only the two new isolated host projects succeeds and removes28
registered files/593170 logical bytes. An initial read-only post-clean verifier
wrongly assumes every generated cache remains byte-identical; it refuses before
any deletion when two323-byte assets caches changed after Clean, alongside empty
file lists. This is an owned build-cache observation, not protected source/financial
oracle normalization or evidence of a product fault.

An additional exact-owned residual-file removal command is rejected by automatic
approval review before process creation, with only blocked by policy stated. Its
verification/removal manifest and removal result are not created; no alternate
shell/wrapper/partial deletion/elevated retry follows. A distinct read-only inventory
confirms20 remaining files/422324 logical bytes in the new reference/candidate
bin/obj roots. Preserve this refusal scope and the original source/results. No
previously refused cleanup scope, shared SDK/cache, owner backup or unrelated file
is touched. Logical byte accounting does not prove host free-space improvement.

## Remaining work

Keep the full performance, general ANR/crash, broader spoken/large-text Android,
iOS/provider/Store/public-release requirements and goal open. The scoped German
title listening acceptance and pending newer Persian SDK decision are independent.
Do not turn this rejected small experiment into another successful full-product
qualification or repeat older accepted native/financial cohorts.
