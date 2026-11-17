# Current concurrent mark-word operation (D1229)

The retained independent Android failure in [D1220](current-search-native-exit-d1220.md)
now has an exact static operation binding. No new crash, search, financial write,
device command or original-record interpretation is performed in this step.
Fault remedy and stress-search stability remain open. The overall delivery goal
is active; unavailable email/provider inputs do not block independent work.

## Exact original operation

The existing event is PID8581/TID8630/UID10216, native reason5/status11, from the
fictitious `pro.vafadar.zanance.quickcpureview` cohort. Original APK SHA256 is
`5d53bd889002fc6a22649fe3f32861dc0a43bbdcc7413978c97201ec338e849c`;
Mono SHA256 is `b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034`,
BuildId `b1881505cd1c881ba16fec2997743cbc7033e60d`.

The unique containing ELF function is `drain_gray_stack_concurrent`, starting
at relative `0x2f3420`. One complete bounded decode reconstructs all4966 original
code bytes across1280instructions. Six original public source files match the
retained source manifest/runtime commit `a3788b71495457354b64ca8040b6ded5ff6ca4f9`.

At relative `0x2f35b4`, `mov edi, dword ptr [rax + 4*rsi + 40]` reads a32-bit
`MSBlockInfo.mark_words` element. The bit test follows at `0x2f35bb`; a conditional
mark write is later at `0x2f36fd`. This faulting operation is not an object-header
or descriptor read. Symbol metadata identifies the block-size global, without
reading its stored value. Source initialization uses the platform page size with
a16KiB minimum; a stale source comment is not evidence of an actual32KiB value.

The object-bitmap path selects a child reference at `0x2f35e8`. Child-header and
untagged-descriptor reads at `0x2f362e`/`0x2f3639` execute earlier through backward
branches. Address ordering must not be mistaken for execution ordering. Both
fast-descriptor and slow-small-object admission paths remain possible. No actual
descriptor, parent/child type, register value, map, block-size value, earlier
writer or exclusive runtime/application cause is inferred.

## Separately proposed finite classification

The reviewable local proposal binds only this exact event/PC/BuildId and requests
three finite labels: block-base alignment, source-expected mark-index range, and
full-four-byte readability. It would interpret only saved RAX/RSI after original
admission, with checked arithmetic and map permissions, without dereferencing
an address or exporting values, addresses, paths, memory or shipping Main data.
The expected index range assumes the declared4KiB-page fixture and source minimum;
it does not establish the stored block-size global or a repair.

This is a new interpretation requiring an explicit owner decision. Earlier
RSI/R13/RAX grants and consumed markers are not reused. No actual record is read,
AVD launched, package installed or marker consumed while that decision is pending.
Missing/refused/unknown input will be terminal for any subsequently authorized
single attempt. The original100k source, keys, current APK and markers remain.

## Prepared reader and distinct controls

The original tracked reader is unchanged. A separate private reader preserves
the8MiB/128frame/128register/4096map bounds and skips other threads, register bodies,
signal-address fields, memory dumps and map names. Encoded selected words and map
ranges are interpreted only after exact UID/PID/TID/PC/module/BuildId admission;
private buffers are wiped on success and refusal.

The official [Android16 schema](https://android.googlesource.com/platform/system/core/+/refs/heads/android16-release/debuggerd/proto/tombstone.proto)
places thread registers in field3 and frames in field4. An older unexecuted private
prototype's field1 assumption is not reused; it does not explain a historical
wrapper refusal that never reached the stream. Proto3 omitted numeric zero is
handled explicitly. Only public wire facts are used: an initial inline-license
assumption and a root NOTICE404 remain recorded, with no generated dependency
or inferred license acceptance.

One strict framework-only host build and the final SDK-only build pass without
warnings. Exactly33distinct new synthetic conditions pass once, including
pre-admission noninterpretation, omitted zero, wrong identities/PC, signed/overflow
arithmetic, map adjacency/gaps/overlap, malformed inputs and original bounds.
These are private reader controls, not product tests or actual fault categories.
Final compiler checks bind both reader and activity source checksums; host source
bindings and used DLL/PDB outputs are retained.

## Prepared package and preserved refusals

The final offline/nondebuggable SDK-only package is19AOT/29ELF/106static16KiBLOAD,
minimumAPI31, CRC/aligned and valid v3-signed under the original public signer.
Its SHA256 is `10c9709057db9dbd9f7e449e73bdf7d9edea69e7991f7570447bbd8c5cd0c683`.
Its SDK runtime differs from the original product runtime; no product-runtime
identity or shipping acceptance is claimed. The package is prepared, not deployed.

Initial private source preparation refused an LF/CRLF assumption. Package metadata
initially expected `sdkVersion` rather than the actual `minSdkVersion` key. The
isolated signing profile then produced a different public debug signer; a first
command-line correction reused the old signed output incrementally and also
refused. Explicit private-project signing configuration subsequently used the
existing project-owned path through the SDK. No key contents were inspected or
copied; no reset, uninstall, weakened signer guard or device action occurred.
The rejected artifacts and original successful utility packets remain separate.

Standard host and Android Clean operations both pass after retaining the final
prepared APK, compiler outputs, sources and receipts. No supplemental deletion,
frozen scope, retained AVD, Samsung/Main or accepted financial checkpoint changes.

## Upstream status and remaining acceptance

At2026-11-17T06:38:19Z, the actual public metadata for
[runtime PR137047](https://github.com/dotnet/runtime/pull/137047) reports open and
unmerged, head `36d4b9143211a69e50cca4a8f2e4b348fccaeee9`. The related
[issue123537](https://github.com/dotnet/runtime/issues/123537) is context, not proof
that this application's fault has the same cause. No supported release, final fix,
private-patch redeployment or project repair is inferred.

Actual categories, earlier corruption origin, a verified repair and remaining
Android stability acceptance are open. Successful parser/financial/product suites,
consumed classifications and rejected runtime/performance trials were not replayed.
