# D-536 - Withdraw reduced-allocation ledger ordering after native comparison

## Hypothesis, scope and host rejection

D-534's bounded sample identifies substantial GC CPU but cannot attribute its origin. Investigate
only the extra reference/key/map/result arrays used by Home's existing stable LINQ ordering.
Keep every original LedgerEntry object and field, descending Date/CreatedAt instant and original
reverse-row tie order. There is no model/schema/financial-rule/runtime-policy change.

An isolated in-memory probe uses actual LedgerEntry types, sizes0/1/2/3/10000/100000 and five
mixed/tied/date-sequence/equal-instant-offset shapes, with alternating original/candidate samples.
Its first wrong project-reference path refuses compilation before measurement; the corrected
original run is preserved. No store, database, key, seed or financial command is called.
The first stable-merge candidate cuts100000-row sort allocation from4000504 to800024 bytes,
but every large host shape is slower (mixed11.801 to34.336ms). Reject it before native use.

The second candidate caches DayNumber/UTC ticks and sorts original ordinals, using each original
ordinal as the final stable key. It applies permutation cycles only to the reader's fresh private
list. Thirty original-object sequence shapes pass. Mixed100000-row median15.366 to14.072ms and
allocation4000504 to1600168 bytes improve on this host; all five large shapes have lower medians.
These are sort-only host boundaries, not Home/native allocation or a product performance pass.

Compile this actual candidate through Data's real reader. Strict managed build has zero warnings/
errors. Six selected existing SQLite order/tie/offset/deletion/full-field/cancellation cases and
one existing empty-read/no-default-write case pass. They are seven affected regressions, not new
product-test cases or a rerun of the whole suite. No public mutable cache or persisted key changes.

## Native pair and original source controls

Before changing product source, the original default-stripped independent package
SHA256`5414b6bccb2524fa17ab6d9e2819b5d6e6b9f4b954e434ffcd61d8d2895e325d` completes3844/
comparison6c93379a144d495ba54e83926600eca8. Candidate4548/comparison6eebfb9a28ac4e0a9c92482d6fb6f201
uses SHA256`2328a40dd26e84dec09e31e6b47c61fdd6e5c59cadc1932c0e453d3357f0710a`,58826658 bytes.
Both are engineering-only shell-profileable packages; no CPU sampler is active. Java DEX bytes,
actual Mono10.0.13 and SQLCipher native binaries match. Caller/Data build metadata differ and
actual Data MVIDs differ as expected; do not claim whole-native-byte equivalence. The candidate
strict publish and CRC/192AOT/204ELF/804 LOAD static16KiB/v2v3/ZIP alignment/offline guards pass.
This is API36/x64/4096-byte runtime evidence, not16KiB or minimum-API acceptance.

Each uses a new independently copied, already protected original source lineage from retained
run25b9dab64bf849fcb24e39246d773215. Actual original additive migration, complete schema/headers/
typed rows, financial/native/recent packets and original premeasurement-byte fingerprints pass.
No original preparation, accepted financial command or key creation repeats. Each native pair
contains only the required en/fa/de reference/tenfold contexts, two observations per context.
Candidate's first45-second report window expires with4548 alive. All51 stage lines later show
completion; all14 original report chunks arrive. Observe the same4548 again and recover its
original report, without process restart or UI/financial replay.

| Shape/language | Original first / second ms | Candidate first / second ms | Candidate/original pair median |
| --- | --- | --- | ---: |
| Reference/en |966.472 /813.3115|882.6231 /625.6742|0.8475|
| Reference/fa |710.0539 /810.1644|669.9629 /782.4498|0.9554|
| Reference/de |631.7821 /907.1537|776.8014 /693.6632|0.9555|
| Tenfold/en |2097.8316 /2185.6345|2233.3438 /2065.1365|1.0035|
| Tenfold/fa |2061.2988 /2111.7478|2088.9938 /2135.3821|1.0123|
| Tenfold/de |2178.4683 /2045.5586|2144.8560 /1935.0564|0.9659|

Two large-language pairs are slightly slower and one slightly faster; no stable material
whole-Home benefit or two-second acceptance is established. Native allocation is not measured.
Storage/code retirement also changes the environment between pairs, so do not interpret small
ratios as causal speedups. Withdraw the candidate rather than expand physical/Windows shipping
verification. Preserve its source/patch/results locally, remove the new Data helper, and verify
Data's complete tracked diff is empty. Shipping code/APK remain D-528. Do not replay this candidate.

## Preserved storage/boot failures and scoped code retirement

The initial owned cold boot has retained UID10221/installed metadata but no actual caller APK at
its recorded code path; PackageManager records parse/missing-package warnings. Preserve that
failure and its absent-file proof. Reinstall the exact original signed code without data clear,
verify UID/hash and sync. The first Main hierarchy is only the15-node startup cover; observe
that same live2933 until its original120 public tuples match, without restarting it.

Existing6GiB userdata has only467380KiB available after a completed copy. Retire only the code of
two completed, stopped project-owned callers after verifying no live PID, alarm, job or active
notification: androidsyncgroupreview UID10223 and androidsyncattachmentreview UID10225. Back up
and hash-match their exact signed APKs first; KEEP_DATA retains their data/keys/UIDs. Available
space becomes767436KiB, about293MiB more internally, not host-disk reclamation. Exact APK backups
remain for future data-preserving reinstallation. A first broad notification-name guard correctly
stops before mutation; inspection finds only inactive AppSettings, so require actual notification
records instead. No pending work is discarded or original financial fixture deleted.

Both native cohorts use signed-r without removing the measured caller's code or ART profiles
between them. After completion4548 is stopped; final original signed-r restore hits storage
policy again. Only then use KEEP_DATA code removal and reinstall the exact original caller,
UID10221/SHA`e55a48f1e3c13abc4144afa326a922dfb117aad7dc2b644208b5a9b3a93df637`/stopped/grants.
No own pending alarm or posted record remains. The actual final source-byte guards belong to
completed cohorts; do not describe them as a private database/key audit after final restoration.

To check actual code persistence, explicitly sync and request Android's orderly poweroff. Verify
the original emulator launcher exits before one new cold boot. This time the exact original APK,
UID/stopped state and both retired-code UID states persist. Ordinary Main returns as2880 after
old2933, with unchanged code path/APK SHA99ee6403, Home0/all120 public tuples and exact OS/display.
The original failure remains; neither its underlying cause nor arbitrary crash/power-loss safety
is proven. This is a necessary recovery/persistence check, not another Home timing observation.
Finish with sync/orderly shutdown of only this owned AVD, never a timeout-triggered restart.

## Phone, cleanup and remaining scope

The owner-authorized other-project turn explicitly returns Samsung. Fresh read-only preflight
matches Main27069/Home979/all141 public tuples, shipping SHA728c3788, original caller UID10381/
SHA b1d5d334/stopped/no pending work and exact recorded OS. The candidate is not installed on the
phone; no tap, financial command, authentication, setting write or phone process control occurs.
Final read-only PID remains27069. Remove only one owned hash-matched phone XML and five emulator
XMLs, retaining local evidence. No unchanged physical/native financial cohort is replayed.

Managed/native/probe standard cleans pass with zero warnings/errors. Two redundant original APK
backups totaling111079544 bytes are removed only after equality with retained originals. Required
retired-code backups and source/result receipts remain. Automatic review refuses recursive removal
of the three exact D536-native-package/native-build/managed-build trees before execution, returning
only blocked by policy. Their118239254/131213260/2053034 bytes (251505548 total) remain inventoried;
no alternate deletion is attempted and no reclamation of those bytes is claimed. Earlier D-531/
D-532/D-533 refusals are not retried.

D-534 CodeQL succeeds and its CI is cancelled; its exact-head follow-up is consumed/deleted.
Latest published D-535 [CI40164923624](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40164923624)
and [CodeQL40164923668](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40164923668)
both succeed; that one-shot follow-up is also consumed/deleted without a second request.
No product test count, model, permission or dependency changes. General ANR/crash, the two-second
objective, remaining accessibility/device/iOS/provider/Store/public-release gates remain open.
Evidence pointer: `Zanance-D536-current.txt`; both host variants, candidate source/patch, exact
native reports/identities, seven selected contracts, initial refusals, both boot lineages, storage
retirement, fresh phone baseline and cleanup receipts remain. The broader goal is active.
