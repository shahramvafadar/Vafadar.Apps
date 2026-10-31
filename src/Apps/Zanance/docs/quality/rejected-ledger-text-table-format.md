# D-687 - Per-block text-table format rejected before native admission

## Outcome

The private version2 text-table hypothesis is closed at its fixed host gate. No product
codec/model/migration/runtime/GC change, APK, native/phone run or financial command
is made. Original production source files remain byte-identical. This is a rejected
host feasibility experiment, not a new product test suite or Home/two-second result.

## Distinct hypothesis and frozen scope

D-601 reused only two original text columns without changing the transport format;
its failed physical candidate remains closed. This experiment instead gives every
first strict UTF-8 text value a block-local index and writes a negative reference
token for repeated immutable strings. Null remains -1; empty text is an ordinary
first value. References must point to an already decoded value. Each block gets fresh
writer/reader tables; mutable Tags lists remain independent. No global intern pool
or plaintext retention is introduced. Both roles link exact current LedgerEntry,
Entity and audit types; the private candidate otherwise preserves all32 original
fields, physical ordering, strict primitive/UTF-8 guards and bounded expansion.

This representation is private only. Version1 remains the shipping format. This
screen does not implement or qualify production v1/v2 compatibility, maintenance,
cache-cookie identity, restore/migration or repair. Original scalar fallback and
SQLite conflict policy are unchanged.

Before compilation, freeze four shapes with32 distinct512-row blocks each: repeated
text, nullable/empty Unicode plus mixed tags, unique short text and unique long text.
Each role runs once in its own .NET10.0.14/X64 process using the same actual CoreLib,
two seconds warmup per shape and nine samples of256 complete blocks/131072 rows.
Input construction, encoding, full-value comparison and refusal controls are outside
the decode measurement. Separate sequential process order is reference then candidate;
there is no reverse/rescue after failure. These synthetic in-memory objects are not
accepted financial records or an exclusive measured Home bottleneck.

Admission requires combined median latency and allocation reductions of at least5%,
worst-shape latency ratio at most1.05 and allocation ratio at most1.01, encoded bytes
at most2x and extra encoded bytes at most8MiB. These cheap prerequisites cannot replace
the original native whole-Home/dispatcher/PSS/RSS gates.

## Actual results

| Shape | Reference median ms | Candidate median ms | Reference / candidate allocated bytes | Latency ratio |
|---|---:|---:|---:|---:|
| repeated | 71.7567 | 58.1735 | 154637856 / 99384192 | 0.810705 |
| nullable | 76.2558 | 76.1585 | 144399680 / 99484544 | 0.998724 |
| unique | 82.0174 | 113.6307 | 171576576 / 177444736 | 1.385446 |
| long | 343.2854 | 437.8086 | 1553309984 / 1559177504 | 1.275349 |

Combined latency ratio is1.196150; allocation ratio is0.956306.
Encoded byte ratio is0.975425, with-112694 extra bytes.
Unique short text exceeds both worst-shape latency and allocation limits; long
text exceeds the latency limit. The repeated-text benefit does not rescue the
failed input shapes. No alternative dictionary cap, threshold, quality/window or
format variant follows this rejected candidate.

Full original row ordering and all serialized public values match for every block
before and after timing; each shape has the same independent SHA256 value digest
across roles. Two fresh decodes retain separate mutable Tags lists from each other
and the source; mutating one result leaves the second/source exact. Every proper
compressed-payload prefix is refused, along with wrong bucket, cancelled read,
unknown version, trailing bytes and invalid original UTF-16. Refusal counts depend
on each representation length and are not called equal test counts. This finite
screen is not exhaustive malformed-reference fuzzing or a production security pass.

The sole private strict build has zero warnings/errors. Both process exits are0.
No unchanged successful app/resource/native suite or financial operation is replayed.
One standard private-project Clean retires ordinary build output; used private source,
JSON evidence and logs remain local. No supplemental deletion or previously refused
cleanup scope is attempted. Phone/D-607 three deadlines, consumed reboot and unread
post-reboot source remain untouched. The goal stays active; physical adoption, general
security/stability/accessibility, supported platforms and external release gates remain
separate.

Evidence is retained under the local D-687 pointer: pre-build plan/source hashes,
exact source copies, strict-build log, separate role outputs and final result JSON.


## D-687 sole CI follow-through

At2026-10-31T11:59:43.4310469+00:00, the single exact-head run-list response records:

- [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41197985016): completed/success.

- [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41197976481): completed/cancelled.

Its own consumed monitor is deleted. No second workflow-runs request or overall-green inference follows.
