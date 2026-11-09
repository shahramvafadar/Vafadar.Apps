# Current SQLite maintenance applicability - D-951

## Completed follow-up to D950

The newer SQLCipher4.20 release moves to SQLite3.54.1. This review establishes the
actual current public SQLite version, matches the vendor's current finite CVE table,
and inspects the direct migration/export call surface before proposing a native
upgrade. No package/code/model/migration/runtime/GC/KDF change, device command,
financial action, app build or old-successful test is performed.

The [immutable pinned SQLCipher VERSION](https://github.com/sqlcipher/sqlcipher/blob/c4b275a47932888216bade83aff2bbc73df0ff85/VERSION)
is3.53.4. The actual D949 shipping APKc38842ce and both selected candidate5 cipher
hashes are rebound. Only the unique public `sqlite3_version` STT_OBJECT is admitted
from each ELF, with an explicit7byte extent, allocated/nonwritable PROGBITS section
and matching readable/nonwritable PT_LOAD file offset. The full object is exactly
ASCII3.53.4 plus its terminator in both ARM64 andx64. Both ELF classes are64-bit.
No arbitrary string search, neighbour-derived extent, module loading/native execution,
private App/configuration/environment/key/live memory/register read occurs.

An initial classifier requires PF_R alone and refuses a readable/executable but
nonwritable segment before reading the version object. The original refusal is
retained; only the nonwritable/readable flag criterion is corrected. The corrected
admission uses retained documents and the same immutable modules without network
replay. This static version is not proof of all compiler inputs or method execution.

## Published maintenance and CVE scope

The [official3.54.1 notes](https://sqlite.org/releaselog/3_54_1.html) limit their
specific heap-overflow correction to32-bit builds; that stated issue does not apply
to these two64-bit members. This does not infer that every3.54change is unnecessary,
or apply the upstream benchmark claim to our Android app. Other query-planner/schema/
API changes remain maintenance considerations rather than measured product gains.

The complete [official SQLite CVE table](https://sqlite.org/cves.html) is retained
with response time/headers/digest. All45rows/54unique IDs are dispositioned: entries
with a numeric fixed version have that fix no later than current3.53.4; the remaining
entries are explicitly classified by the vendor as outside SQLite/core-library
issues. A parser initially refuses the distinct CLI-only label; that row and refusal
are preserved before accepting the two explicit vendor labels. No row is silently
ignored. Table absence is not proof of no unknown CVEs, and vendor non-bug labeling
is recorded as attribution, not an independent reproduction. Malicious portable
input and source-level defense still require their own acceptance; this review does
not dismiss them merely because application SQL is controlled.

## Actual direct call surface

A case-insensitive literal scan of1492tracked source C# files records its complete
path/content-set digest. It finds no direct `cipher_migrate` or `hexkey` literal.
The two actual `sqlcipher_export` callers are inspected:
`SqlCipherPortableSnapshot.WriteAsync` and `SqliteMemorySnapshot.ExportEncryptedAsync`.
Their aliases are generated locally from GUID N strings with fixed portable_/encrypted_
prefixes; caller-supplied database aliases are not interpolated. Export destination
paths/passwords in the encrypted staging path use SQL parameters. SQLCipher's4.20
migration-error improvement therefore has no direct literal caller in this source
surface. This lexical search is not proof about dynamically assembled SQL, external
plugins or every possible native entry; it does not replace the already-required
full portable-input/schema authorization or financial/source invariants.

## Disposition, evidence and retained gates

For these exact current modules and retrieved vendor data, the reviewed version/
CVE/migration facts do not demonstrate an urgent native upgrade requirement. Keep
the qualified pinned package; do not replace it with a beta, change crypto parameters
or replay successful database/financial workloads solely for a version bump. A future
native update remains a separate source/ABI/compatibility qualification decision,
especially if a concrete affected path or relevant correction is established.

Three official documents are fetched once locally, with all raw bodies/headers and
both observer refusals preserved under
`artifacts/d951-sqlite-maintenance-applicability`. No repeated document request,
whole-image extraction, cleanup/frozen scope or protected untracked input is used.
The earlier browser lookup of an assumed upstream src/crypto.c path returns404 and
supplies no source evidence; no behavior is inferred from that missing file. Actual
migration applicability comes from tracked callers and published notes above.
Only documentation is committed; new local links/diff-check pass. D949/D950soleCI
observations remain scheduled locally without polling/waiting or final inference.
D944 physical7552/UID10439/original request stays held while the owner is away;
Main adoption and authentication are not attempted. Original fault/ANR, whole
performance/security/accessibility, platform/external gates and full delivery remain
open. Independent ready development continues.
