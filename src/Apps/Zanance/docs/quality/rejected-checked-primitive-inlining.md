# Checked primitive inlining rejected at the host gate (D-600)

Status: closed before product change or native qualification. D-599 locates a
large inclusive block-reader interval, not an exclusive primitive-decoding cost.
Investigate only checked primitive inlining and the identical incomplete-slice
exception moved to a cold helper. This is distinct from the withdrawn tag-list,
pool, timestamp, ordering, shared-index and parallel-worker candidates.

Freeze the five original codec/buffer/domain sources at b42523ae. Both isolated
copies use actual original Encode/DecodeWithBuffer and complete 32-field rows.
Only the private candidate adds aggressive-inlining attributes to checked primitive
methods and a no-inline helper throwing the same InvalidDataException/message.
Bounds, UTF-8, dates, timestamps, tag ownership, physical order and persistence are
unchanged. Production sources are never patched; no model or migration changes.

Before either run, freeze a 5% combined median latency improvement gate, worst-shape
candidate/reference ratio at most1.05 and allocation ratio at most1.01. Each fresh
role runs once in a separate actual10.0.14/X64 process: two declared512-row shapes
(empty and mixed tags), at least two seconds warmup per shape, nine samples and
128 complete blocks/65536 rows per sample. Full-value serialization, source digest
and independently mutable per-row/per-read tag controls bracket the timed work.
Both strict isolated builds have zero warnings/errors; compiled method flags
confirm each actual role. No runtime-policy switch, database/key/native app or
financial command is used.

| Shape | Reference median ms | Candidate median ms | Allocated bytes, either role |
|---|---:|---:|---:|
| Empty |18.5411|18.7804|40382112|
| Mixed |21.1343|20.8198|43069088|

Combined improvement is only0.1895381%, below5%; allocations are unchanged.
Close the candidate without extra samples, rescue/reverse runs, app packaging,
device measurement or selection of only the favorable shape. This is not a
complete Home/phone/two-second result or a new shipped managed-test count.

Every shortened expanded single-row prefix is repacked as valid compression:
all410 incomplete rows are rejected. Another102 four-byte mutations compare the
complete accepted row output or rejected exception type/message. Both roles have
the same outcome digest29509b3303b7945747cd4a25f6980fa4517447b6823c6dd37f949c0b0d36d19d.
These bounded host controls do not establish general malformed-input acceptance.
All five production source hashes remain exact.

Standard Clean removes28 registered outputs/655940 logical bytes. The post-clean
read-only receipt lists20 residual files/423338bytes. A later removal attempt has
no retained completion receipt; removal is not claimed or retried through another
method. Preserve the evidence and residual scope, not a host free-space claim.
Evidence pointer: `%TEMP%\Zanance-D600-current.txt`.

D-599's one-use hosted response reports both CI40580671736 and CodeQL40580671760
completed successfully; its monitor is deleted. No second request is made.
The full delivery goal remains active; remaining performance, stability, security,
non-TalkBack accessibility and external release gates are not accepted. Further
TalkBack remains last, and Persian speech stays parked under D-594.
