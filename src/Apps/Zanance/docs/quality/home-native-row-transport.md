# D-372 current-row native transport withdrawn

## Decision

Retain the original complete Home reader and D-369 main APK. A separate native current-row
transport does not provide a consistent original whole-Home gain sufficient to justify another
native library, unsafe buffer boundary and build dependency. No production source, financial
algorithm, schema, compiled model, date conversion, key/KDF, runtime policy or permission changes.
Do not repeat this unchanged prototype or promote its isolated read time into QA-06 acceptance.

The prototype moves the same 32 fields through one bounded native call. It owns no query,
connection, transaction, key, file or financial operation. Original provider DateOnly conversion,
EF command/interceptors, transaction, ordering and cancellation remain. Storage classes are checked
before conversion; noncanonical/required-null rows return to original scalar getters. Borrowed
SQLite buffers are decoded before advancing/finalizing the exact retained statement. This design
was tested independently and is withdrawn rather than added to application dependencies.

## Native protocol and actual Android scope

The already reviewed local source-build image supplies the official r30/API24 compiler, without
new downloads or installation. Both ARM64/x64 libraries compile with C11, O2, warnings as errors,
hidden visibility, stack protection, RELRO/NOW and 16 KiB ELF alignment. Compilation alone is not
runtime acceptance. Source SHA256 is
72fab86193a4c0fc8aa1f9546d67207aacef754b7ae1d98cc4017b330a61a54a.

A Linux real-SQLite 3.45.1 protocol run passes 57 cases: complete rows, optional nulls, empty and
long text, Persian/Latin accents/emoji/embedded zero bytes, signed 64-bit boundaries, BLOB GUIDs,
unchanged text/Julian/integer date storage, each required null, legacy scalar storage and protocol/
projection/size refusal. These are native protocol cases, not additional managed unit tests or
Android financial acceptance.

The independent Release/AOT x64 caller builds with zero warnings/errors. Its signed offline APK
has SHA256 cb390e4e30d30bed6ad6f79ccda101c0e4ff5e0ca155a8b133db29d66048da31,
54542022 bytes and 192 AOT images. The four existing runtime/SQLCipher libraries are byte-identical
to D-369. Only the independent package includes the prototype; ARM64 is compile-only and no phone
input occurs. Main package/source/APK stay unchanged.

Use the existing protected run ca28fce07b29472a95ff4da3cb66d2f0 without replacement keys or reseeding.
Read-profile comparison 67c8282b8a084a63a36305adf4ea63c9 completes on PID 30021. Public ledger reads
and Home snapshots have identical complete ordered entry digests, and full portable schema and
typed rows remain unchanged for 10000 and 100000 entries. Snapshot times 480.23/882.39ms are
isolated observations, not a paired whole-page improvement.

## Paired original Home result

The historical presentation baseline 90ce6cf3ce5f47d49e7e324dd5b45606 refuses the current first
reference/en presentation in comparison 3e3c7ebcfba34e7d9a02aa6a51629386, PID 30203. Preserve that
failure; do not guess its differing field or weaken the equality check. That comparison supplies
no accepted completed timings. A fresh original-reader control captures current canonical
presentation on the same unchanged protected fixtures; the candidate then compares against it.

Control comparison/baseline 8ec925156f6841738cad8844b4d9bd71, PID 30316, uses the existing qualified
D-369 independent APK, not another successful build. Its Data MVID is
d43d83b4-2635-4330-bd3e-564369a553f4. Candidate fbc5fe156c8744499bc49e1b740d4266 completes on
PID 30543 with Data MVID 350d1bd1-e97b-4479-9ded-5f70ac9d030f. Both use real original navigation,
covered publication, required Activity focus and native layout for twelve samples each.

| Shape/language | Original first/second ms | Candidate first/second ms |
| --- | --- | --- |
| reference/en | 1287.40 / 773.16 | 1093.60 / 761.71 |
| reference/fa | 664.22 / 833.71 | 676.94 / 785.46 |
| reference/de | 731.20 / 749.07 | 819.98 / 719.76 |
| tenfold/en | 2462.11 / 2487.94 | 2220.65 / 2476.94 |
| tenfold/fa | 2377.23 / 2483.82 | 2450.67 / 2503.52 |
| tenfold/de | 2334.24 / 2490.76 | 2289.49 / 2334.27 |

Reference median is 761.11 ->773.59ms; tenfold 2472.96 ->2392.47ms. Mixed reference observations and
only about 3.3% tenfold median reduction do not establish a useful persistent whole-Home gain.
Every canonical financial presentation/native balance matches the fresh original control;
complete raw schema and typed rows remain unchanged. This bounded emulator cohort is not a
statistical causal attribution, physical-device result or two-second acceptance.

## Restoration and limits

Host window-subcommand observation initially returns no focus and refuses before any installation;
use the actual current-focus report. Observation deadlines retain each exact live process; no
restart repairs an observation. Only after terminal candidate success stop the independent caller,
replace its experimental APK with the original qualified D-369 independent bytes and preserve all
private fixtures. Original main PID 12893, meaningful native Home, installed main APK
fc1ef747c15a9dbf5b26fb6244e2eb89df7dba3cf284112d8b9c046a38a171f3,
font/accessibility/display settings match exactly. No financial Save or phone/system input occurs.

Rebuild only ordinary Data output after removing the artifact-only build override: zero warnings/
errors, original row type present and prototype type/entry point absent. Tracked application and
benchmark sources remain unchanged. No unchanged successful main build or complete suite repeats;
managed test counts stay unchanged. Protocol reports, immutable APK, read/paired results, failure
and restoration receipts remain under artifacts/home-native-row-d372.

QA-06/general GC/ANR/startup/low-memory, correct audible reader speech, broader phone/main encrypted
adapter, iOS/provider/Store and release gates remain open. This work rejects an unhelpful transport;
it does not close the planned original Home performance requirement.
