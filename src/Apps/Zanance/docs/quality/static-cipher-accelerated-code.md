# Static current cipher acceleration code admission - D-947

## Completed independent question

D921 prioritizes the recorded protected-startup critical path but has only source
Configure declarations and package hashes for native optimization. This step checks
whether the actual currently packed public cipher modules contain bounded hardware
instruction implementations. It does not run a startup workload or the inspected
library, inspect a private App module, or alter any KDF/check/pooling/runtime/GC rule.
Rejected D889/D894/D895/D922 candidates remain terminal and are not retuned/rescued.

The existing full D943 APKb39a520f20db743602ab74622c4ade4c5c46ec3f7e847bad9e89a65fff4b2e8c
is hash-bound. Only its two libsqlcipher.so members are read in memory. Both hashes
match the selected candidate5 inventory exactly:

| ABI | Module SHA256 | GNU BuildID |
| --- | --- | --- |
| ARM64 | 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c | 3168da11862e53564d23df14aa41ed00c71c6440 |
| x64 | 20b009b7ce0fc50bf31fe6e321216085a2c9ee3f3e14e0eb6757597380c2c568 | 13a00cd8bf95995205d2a8daf8ed8731e6851132 |

## Bounds and complete decoding

A registered finite symbol family selects12ARM64 and14x64 functions. Every extent
comes from its explicit nonzero STT_FUNC size, lies wholly inside the declared
executable section and exactly one executable PT_LOAD, and agrees with its file
offset. No neighbour-derived extent, zero-sized-symbol guess, data/configuration/
environment/key/private database, runtime memory/register value or CPU probe is used.
The cap is24functions per ABI and65536bytes per function.

All26declared extents decode completely:11032ARM64 plus25660x64 bytes,36692total,
9251instructions. Every instruction offset/length/byte sequence matches its source
slice and per-function digest. Sixteen new private reader controls pass: four fixed
architecture/crypto decoder vectors plus twelve extent/type/executable-section/LOAD
refusals. These are neither new product tests nor another cryptographic validation,
old workload or financial acceptance run. Static register names in public assembly
are not live register values or another R13 interpretation.

| Bounded finding | Actual static evidence | Limit |
| --- | --- | --- |
| ARM64 AES paths | aes_v8 key/encrypt/decrypt/CBC functions contain AESE/AESD/AESMC/AESIMC | Presence only; no executed key size, capability or branch result |
| ARM64 SHA512 path | sha512_block_armv8 contains SHA512H/H2/SU0/SU1; original dispatcher has a direct conditional branch into it | No runtime capability-word or selected-path read |
| x64 AES paths | aesni key/encrypt/decrypt/CBC functions contain AESENC/AESENCLAST/AESDEC/AESDECLAST/AESKEYGENASSIST | No execution or latency claim |
| x64 SHA512 vector path | registered AVX2 routine has233instructions with YMM operands; original dispatcher has conditional transfers to XOP/AVX/AVX2 routines | Static vector implementation, not a claim of a dedicated x64 SHA512 opcode or actual dispatch |
| Original KDF wiring | three direct static calls from sqlcipher_openssl_kdf to PKCS5_PBKDF2_HMAC in each ABI | No real key/KDF operation is executed |

The SHA dispatch transfer audit derives only from already-decoded records; it does
not decode the functions again. Counts describe static instruction occurrences,
not runtime operations. The premise that all acceleration code is absent from
these exact modules is refuted. Whether a particular observed process selects
those branches, and how much of its wall interval is KDF/integrity/cache/other work,
remain unproved. No exclusive source of latency or native-fault repair is inferred.

## Reader provenance and failures retained

Existing local disassemblers and the old native build image are unavailable; nothing
is installed globally and the missing image is not rebuilt or pulled. A pinned BSD
Windows decoder distribution5.0.9 is restored only under this fresh D947 artifact
root,1273459bytes. Official published wheel SHA256
732cedbbb56d42e723f14d7af6387f1454194a820b4b96b56d1e53f865ef85d0
and all62RECORD file hashes match; bundled redistribution terms remain retained.
This is integrity/provenance review, not publisher attestation or proof of no defects.
The inspected ELF is never loaded; only the scoped decoder executes.

The initial legacy licence-field assertion refuses before download: that field is
null, while the published BSD classifier and bundled licence provide the real
admission. A Python token-spacing SyntaxError occurs before imports/input/control
execution. The first decoder self-version check then refuses before controls/code:
distribution5.0.9 actually self-labels5.0.7 with native API5.0. The official5.0.9tag
wrapper has that same label. Exact wrapper-byte comparison also refuses because
the wheel has1260CRLFs; normalized content is exactly equal to the official LF
source. Neither exact-byte identity nor native-core source/reproducibility is
claimed. Wrapper and DLL remain unmodified, scoped paths and DLL digest bind before
the sixteen controls and first actual decode. No successful stage is replayed.

## Preservation and delivery frontier

Only small new local analysis/tool/receipt files are created. No product/library/
model/package/workflow source change, Windows/full APK build, old test, benchmark,
crypto operation, device command, lifecycle/cold/frame/auth/financial action,
supplemental cleanup or frozen-scope read/reuse occurs. No duplicate extracted ELF
is kept; original used signed APK and bound per-function records remain. There is
no build output requiring a new standard Clean.

D944phone7552/UID10439/data and its unresolved once-issued navigation remain held
while the owner is away. D945 current Main adoption is only file-admitted; no Main
install follows. Retained5606/Q02 and terminal register records remain protected.
Independent feature/fault/ANR/whole-performance/security/accessibility/platform/
provider/store/release gates and the full delivery goal remain open.

D946soleCI response at2026-11-09T10:44:37.6816344+00:00 is1087.223seconds after its push:
[CodeQL42600894479](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42600894479)
completed/success; [CI42600894460](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42600894460)
in_progress. Actual delay is recorded without another GET, final CI/job/platform
claim or successful-job rerun.
