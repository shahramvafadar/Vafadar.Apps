# D-305 complete typed Home ledger materialization

## Delivered read boundary

Home retains every persisted ledger property and the complete history. Its existing physical reverse scan,
stable date/time order, captured factory/context, deferred read transaction, operation-local cache and final
file/rights validation remain unchanged. Only per-row materialization changes: a small typed reader uses the
original EF relational command pipeline, provider getters, existing-id constructor and original model-owned
timestamp/tag conversions. There is no new date/JSON/financial algorithm, schema, migration, key handling,
connection pooling or encryption change. No partial snapshot is published after cancellation or malformed data.

The reader checks all 32 mapped property names/types/columns and scalar conversion shapes, table/inheritance
and query-filter boundaries. Mapping drift fails explicitly rather than silently losing new fields. Immutable
conversion metadata is weakly cached per actual EF model; rows, contexts and keys are never cached. The supported
EF command pipeline retains original connection/transaction, timeout, interception and command diagnostics.
The existing compiled model and D-303 existing-id materialization remain in use elsewhere.

## Actual same-data evidence

The independent Release/AOT Android caller reuses the original protected reference and tenfold generations,
keys and canonical financial presentations. A fresh comparison identity names the retained baseline explicitly;
completed old cases cannot become timings of changed code. Twelve original native Home publications across
English/Persian/German preserve complete financial presentation, native balances and complete raw schema,
header, integrity and typed rows against the authenticated original rollback. The loaded reference median is
2134.22 ms and tenfold 6436.50 ms, compared with D-303 2252.00 and 7418.54 ms. These observations have substantial
variation and are not a clean causal UI speed ratio, cold readiness, physical-device or two-second acceptance.

A narrower temporary experiment alternates the original EF and candidate typed source reads in the same
Release/AOT process over the same ciphertext: EF/typed/typed/EF/EF/typed. Every complete materialized ledger
row is serialized and compared in exact order by SHA256 outside the stopwatch; only equality/counts/times
are reported, never row bodies or digests/keys. Tenfold EF samples are 2657.47/2739.24/2707.56 ms; typed samples
2195.01/2692.00/2211.90 ms. Medians are 2707.56 versus 2211.90 ms, an 18.31% source-read reduction. Reference
samples have large first-use variation (including the first EF sample at 5333.91 ms); no stable small-data
improvement is claimed. This limited gain and the explicit mapping guard justify the bounded reader, rather
than assuming that all later native projection/layout cost has disappeared.

Evidence remains in artifacts/home-materialization-d304 (early candidate name) and
artifacts/home-materialization-d305 (same-process comparison). Native candidate/module/package identities,
original failed observations and all sample distributions are retained. The temporary read-compare phase
skips page publication and raw preservation; its inherited generic final-report page/preservation booleans
and boundary are inapplicable. Use only its Results and module identities; measurement-scope.txt records this
limitation. Page/raw-preservation evidence belongs to the separate twelve-case run. All temporary source
switches and experimental phase changes are restored byte-for-byte before the ordinary production build.

## Relevant behavior checks

Four new AT-305 cases cover all 32 fields against original EF reads: populated and absent optional fields,
all entry kinds, exact large signed integers, DateOnly limits, original timestamp conversion and independent
collection instances; cancellation after command execution releases the SQLite reader/transaction and returns
no partial Home; malformed stored tag JSON remains the original converter failure. The 24 distinct related
Home/model cases are qualified across retained runs, not represented as one new whole-suite run. Initial
fixture SQL interpolation and a mistaken exception assertion fail before the final corrected cases; only the
failed cases are repeated. The final original converter error is InvalidOperationException, not a guessed
JSON exception. Existing complete-value/tie ordering, captured transaction, concurrent writer, read-only/pool,
file retirement, authority and model checks retain their original coverage.

D-303's 881 Data cases and historical full-suite count are separate previous results; neither is relabeled as
a new broad run. No unchanged billing, provider, sync, native cipher/upstream, localization or layout matrix is
replayed for this nonvisual read change. Main runtime/package and refreshed startup-profile receipts follow
below. General remaining managed/native composition, memory/cold/ANR, phone, iOS and release gates stay open.


## Refreshed compilation and final main package

The actual main recorder process 8155 reaches the unchanged My account / 0.00 EUR Home before collection.
The app-local recorder has no AOT libraries and supplies only method metadata, not arguments/heap/data.
The fresh profile contains 87 modules, 5840 types and 24359 methods in 3197424 bytes; SHA256
454e9b35b75bc7ee68233a2c5dd87b2861116862d7aa61cf71e2a34072e9e05d. The actual typed reader, async state
machine, mapping guard and conversion contract appear in its method listing. SDK default profiles remain;
Release evaluation confirms AndroidEnableProfiledAot=true and the exact project profile input. Ordinary
compilation emits fresh Data AOT ELF libraries on both ABIs (arm64 1428576 bytes; x64 1413240), changed from
D-303. The shipped libraries are stripped; a raw type-name string is not present and no symbol-level proof is
claimed. The actual new reader's independent Release/AOT behavior and final main startup remain separate
runtime evidence. There is no new claimed statistical startup speed improvement from this refresh.

An initial recording command incorrectly applied the Android-only runtime to the Windows restore target;
NETSDK1084 stops it before a recorder install. Narrowing frameworks fixes that, but project-local AOT defaults
still override the temporary recording property; that unnecessary AOT build is canceled. The final recorder
uses explicit Android-only/global recording properties and builds with zero warnings/errors. Only this failed
recording setup is repeated, not the successful data/native cases. Central package bytes are restored exactly,
the scoped 9915 forward is removed, and temporary recording inputs remain ignored outside the project.
An immediate pid observation is empty after each install; later observations identify the same already running
process instead of restarting it. No global device setting or owner data/key is changed.

Strict Windows and ordinary Android builds pass with zero warnings/errors. The original hidden Windows Home
publishes twice per unchanged reference/tenfold dataset: reference 537.37/513.73 ms; tenfold 1093.62/963.76 ms.
Original native balances/layout, covered publication and complete portable schema/typed rows remain equal.
These are loaded development observations, not a new broad UI/font matrix or device performance claim.

Complete signed main APK: artifacts/android-home-materialization-d305-qualified/pro.vafadar.zanance-Signed.apk.
Size 95797151 bytes; SHA256 dd28e8093504242276c2e608b62aa038fddd2b1e12030fd5f191fca1556a5377.
Identity pro.vafadar.zanance, min24/target36, arm64-v8a+x86_64; v2/v3 verify against the existing local test
certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is not store signing.
Both embedded source-built cipher digests remain the previously qualified candidate5 values; no crypto
component is rebuilt. There is no native profiler, packaged method-profile asset or recorder dependency.
The existing configured connected build retains its original network capability; it is not an offline APK.

Final upgrade preserves the original main installation. Process 8415 first shows the startup cover; a later
own-package native read observes 135 nodes and the unchanged My account / 0.00 EUR Home in the same process.
No Save, profile/settings mutation, data reset, security flag change or repeated navigation is used. Retained
events contain no ANR for this exact final process at observation; this is not general ANR acceptance.
Receipts: main-package-proof.json, main-signature.txt, main-badging.txt, main-native-proof-2.json,
profile-build-inputs.json, profile-compilation-proof.json and windows-qualified/home-workload-proof.json
under artifacts/home-materialization-d305. The initial package-inspection helper guessed an incorrect native
library entry name; listing actual entries corrected only that read, with no rebuild or repeated signature check.

All temporary timing branches are absent, the central package file equals its original bytes, and ordinary
production sources contain no recording dependency/environment. No unrelated successful fixture, broad test
suite, cipher qualification or layout matrix is rerun. This bounded read/compilation/runtime/package delivery
is locally complete. Its own hosted CI result must come from the one-use post-push check; physical-device,
iOS, remaining composition/memory/cold/ANR and product release remain open.
