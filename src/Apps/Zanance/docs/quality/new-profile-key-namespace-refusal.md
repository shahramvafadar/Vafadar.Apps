# Retained new-profile intent refuses unknown key namespaces — D-1016

Eight actual failures in the separate SqlCipherNewProfile Original-intent branch
are corrected. The [D1015 migration-session correction](migration-key-namespace-refusal.md)
remains byte-for-byte unchanged as working source; its successful cases are not
replayed. This step changes no key format, financial data, migration, model, cipher,
KDF, integrity, pooling, runtime or GC policy.

## Original boundary and narrow correction

An existing new-profile intent in Original phase has not authorized candidate or
rollback ciphertext. Previously, its key-preparation branch checked only the two
base files. A directory at either base name and regular four-byte sentinel files
at each candidate/rollback -journal/-wal/-shm name all reached the counting fake
key preparer. All eight new host cases fail before correction: prepared1/reads0.
The fake preparer deliberately throws at that boundary, preventing subsequent
phase advance, staging cleanup or schema work. No real key is created, and the
baseline does not claim to have executed later deletion effects.

Inspect all eight namespace names with File.GetAttributes before preparing a key
for retained Original intent. Only file/directory-not-found proves absence; existing
objects and other metadata failures refuse. The same helper replaces the earlier
fresh-intent namespace check before its metadata publication. Missing-intent,
existing-original, ownership and explicit initialization guards remain intact.
KeyPrepared continuation, original staging reconciliation and later publication
are unchanged; this is not a rewrite of recovery policy.

## Focused host evidence

The eight occupied cases and one new genuinely-absent control all pass in the final
host binary. The absent control reaches exactly one counting fake PrepareAsync,
which intentionally stops execution; it proves the guard has not globally disabled
new-profile key preparation. No returned key, schema build or phase advance occurs.
Three directly affected existing fresh-intent refusal cases also pass. These are
9new+3affected selected tests, not a complete suite or12new tests.

Each fixture creates only three non-secret operational SQLite metadata files:
installation/profile identities, the retained Original journal and pending restore
ancestry. Metadata file names/bytes, original checkpoint and occupied namespace
remain exact; no original financial database or final generation journal appears,
no protected endpoint publishes, and durability/schema/key-read counters stay0.
The eight negative conditions prepare0keys; the positive control prepares only
the deliberate fake boundary once. There is no account, plan, entry or ledger setup.

The actual host runner reports.NET10.0.12; it is not relabelled10.0.14. An initial
strict test compilation rejects a nullable dictionary key before tests run. Correct
that fixture selector's declared non-null filename; retain the original refusal.
Baseline/corrected strict builds then pass without warnings/errors. The eight
baseline failures remain separate from the compilation refusal and final passes.

## Actual Android method and compiled source

One fresh API36/x64/4096 cohort, PID5103/UID10216/.NET10.0.14, runs only these nine
new-profile conditions through original compiled Vafadar.Data and real operational
metadata journals. All eight occupied cases return IOException before any key
prepare/read; the absent control reaches the single intentional fake-key exception.
All nine preserve complete captured metadata bytes, journal state, namespace and
unpublished endpoint, with durability/schema/read0. Each fixture retires after its
terminal result, and one exclusive provider marker prevents request replay.
There is no original migration-session, real Keystore, financial or earlier native
success replay. This does not qualify actual new-profile financial creation, ARM64,
16KiB, every permission/type failure or the entire key lifecycle.

Strict private Android build passes first time without warnings/errors. Package is
12587427bytes, SHA256
`82688debf6d219d99da44fdd336259c0a834c0bd2f2fb34e5affbf72ec61f1f6`,
74AOT/85ELF/329static16KiBLOAD, offline/nondebuggable, v2/v3/ZIPCRC/alignment pass.

Strict Windows and complete offline shipping pass without warnings/errors. Shipping
is99614623bytes, SHA256
`ac38a0eac6cf21021e898847b1d7f1d0f5d27ca83b44ed2a6bbfe586054cc556`,
384AOT/408ELF/1608staticLOAD, v2/v3/ZIPCRC/alignment pass. Both original Mono slices
remain exact. Actual private and shipping compiler documents bind corrected
SqlCipherNewProfile source SHA256
`C8D0F59B01042176DF4B1D2C24141071805FFC9CABBFA1B806A2F304350C0765`.
This is finite compiler/source binding, not whole-PE identity or phone adoption.

## Exact retirement and remaining delivery

Only the fresh UID is removed after all terminal results. The original captured OS
values, locale/key presence, boot, geometry and reverse mapping remain exact without
setting writes. One owned console stop leaves all four original host handles
signalled; the same ADB5038/PID65060creation identity survives without reset/start/
reconnect/pair. Official retirement removes24AVDfiles/4131075321logical bytes.

Five standard own-output Cleans retire5583registered files/1061518633logical bytes;
2860registered files, six scoped userhome files7732bytes and nine host-tmp files
8668bytes remain. Logged deletions remain inside current own output roots. Used
signed/source/compiler/failure/result evidence stays; no supplemental/frozen scope,
retained5606/Q02 or terminal register classification is read/retried/changed.

The cumulative complete package includes D1015and this correction, but is not
installed on Main here. Main10742/9ef33c3a remains only the dated D1012observation,
not fresh liveness. No phone, authentication, photo, successful financial operation
or unrelated test is repeated. Wider key/recovery/security/private-source, original
native-fault remedy, whole performance/accessibility/platform/provider/Store/release
requirements and canonical parent States remain open. The complete goal stays active.
