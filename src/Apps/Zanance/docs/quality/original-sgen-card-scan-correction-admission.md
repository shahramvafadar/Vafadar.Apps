# D-700 - Exact original SGen card-scan correction admission

## Original failure and bounded question

Continue the unresolved original D678 PID10110/UID10216 native fault from
[D679's exact symbol/source chain](exact-original-native-fault-symbols.md).
Do not reproduce its successful UI matrix, induce another native failure, rebuild
the application or alter the runtime/collector. D682/D688's separate managed
resource-cache exception remains a different observation; no common cause is inferred.

The missing admission is whether an actual official SGen correction applies to the
already recorded failing module. An external issue title or similar stack is not
proof. Compare the precise correction, original package commit, actual native
bytes and original frame locations before proposing any runtime replacement.

The canonical preflight also retains D666's completed native CSV handoff, D73's
already qualified fictitious-PIN cold widget drafts and D479's physical original
widget actions. Their old successful commands are not rerun merely because parent
QA rows remain open. D699's initial incomplete PDF history was corrected explicitly;
D329's physical en/fa/de document acceptance remains authoritative.

## Official correction and exact source

[Runtime PR125116](https://github.com/dotnet/runtime/pull/125116) corrects the
overlapping-card scan of large non-array objects by aligning its starting address
down to the containing card boundary. Its
[release10 backport125213](https://github.com/dotnet/runtime/pull/125213) is merged.
The linked issue124941's original suspected delegate/interpreter explanation is
not substituted for the maintainer's actual correction or for this app's cause.

Retrieve the official backport diff and the actual package's original source at
0427b9abbead3fa64c6655eb4290c537397131fa, with raw-byte SHA256 and line receipts.
The diff adds one alignment statement. That exact statement already exists at
[sgen-cardtable.c:206](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-cardtable.c#L206).
This is an actual source comparison, not an assumption from the10.0.14 version number.

## Verify the actual native module, not source alone

The existing public package module is3290280bytes, SHA256
edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4.
D679 already proves it byte-equal to the originally used APK module. Revalidate the
actual local module and its preserved official15762568byte debug file, SHA256
8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0.
GNU BuildID b9db63cc66e53a97dc8483280f7aab8a31672169 and debug-link CRC both match.
Do not reuse the refused duplicate symbol cache/tool scope; read only the preserved
used debug artifact and original public package module.

Exact positive-size ELF function symbols admit only two ranges:

| Original function | Start | Bytes |
|---|---|---:|
| sgen_client_cardtable_scan_object |0x2c6450|810|
| sgen_cardtable_scan_object |0x2d58c0|203|

Map both ranges into the original executable PT_LOAD file bytes and decode every
instruction in64-bit mode without executing the ELF. The instruction lengths cover
their entire exact ranges and contain no invalid decode. A private MIT-licensed
Iced1.21.0 decoder is acquired from its official PyPI release, wheel SHA256
04e04a8cc49ca7d89448fb78e3bab4ae364cf1fc888c996540a791c9999077c1,
with official index hash/ZIP/path checks. It is extracted only into the new
diagnostic directory, with no dependency, global installation, PATH/SDK/config
change or product reference. Existing local symbol utilities lack a disassembler;
no unrelated toolchain is installed or modified.

At0x2d5957 the original executable contains bytes482500feffff:
and rax,0FFFFFFFFFFFFFE00h. The surrounding original instructions first retain
the unaligned object end, copy the original start into rax, apply this512-byte
alignment, compare to the end and advance by0x200. This is the actual native
correction already present, not a text-pattern hit inside unrelated data or a
source-only synchronization assumption.

## Preserve the failing path and causal limits

The outer original frame PC0x2d58e5 lies inside the call at0x2d58e1 to the exact
client function0x2c6450. Its return test exits when that client handled the object;
only the fallback can reach the non-array alignment above. The original client
frame0x2c667b lies inside the call-r14 instruction0x2c6679..0x2c667b, followed by
an8-byte element advance. D679's exact DWARF binds this to
[sgen-mono.c:1051](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/metadata/sgen-mono.c#L1051),
inside the original reference-array scan. It is not the non-array helper changed
by the cited PR. Keep the original top failing pointer-scanner ordinal0 unchanged.

Five bounded address-arithmetic illustrations check the correction's card-range
meaning, including an8-byte-aligned start504/size8704 whose old iteration misses
card17 and aligned iteration includes it. They are mathematical illustrations,
not new product tests, real heap objects, original failure reproduction, a timing
gain or proof of the app's invalid reference origin.

This rejects reapplying/upgrading for an allegedly missing PR125116/125213 fix.
It does not exclude a different runtime defect, an earlier corrupting operation,
another reference writer, native interop or an application error. No heap/register,
raw tombstone, private database/key or absolute process address is exported. The
actual bad-reference origin and general native/managed stability remain unresolved.

## Preservation, cleanup and next admission

There is no app/emulator/phone command, application build/test, financial setup,
writer replay, runtime/GC/package switch or new product-test count. The original
module, debug artifact, source copies, exact decoded instructions and receipts
remain. Only newly necessary public-source/decoder retrieval and read-only host
inspection occur; earlier refused cleanup scopes are untouched.

Automatic approval review rejects removal of the new scoped decoder directory
before process creation with blocked by policy.88files/4196690logicalbytes remain
frozen without retry, subset, alternate method, move, escalation or reuse. The
used source/identity/decoded-output evidence and licence remain outside that scope;
no process continues using the decoder. No host free-space reclamation is claimed.

The next causal step still requires an original reference-origin/failed-condition
input or another verified official correction matching the actual path. Do not
replace that requirement with arbitrary GC flags, swallowed resource exceptions,
successful UI loops or a speculative runtime upgrade. Phone/D607's three deadlines,
consumed reboot and unread post-reboot source remain protected. Its actual
completion/cleanup still precedes Main adoption. All wider DB/key/security,
two-second Home, accessibility/iOS/provider/Store/release gates remain separate;
the overall delivery goal stays active.

## Sole exact-head CI follow-through

The one consumed response for1a33f6b57d349fb30c4243175054143df33413da reports [CI in_progress/no conclusion](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41234363501) and [CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41234363469). It does not establish a later CI result. Only that automation is deleted after its one request; no second GET or successful job replay occurs.
