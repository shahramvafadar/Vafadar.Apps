# Existing current stress-search native exit correlation (D1220)

The current original-source stress search in [D1219](current-performance-acceptance-d1219.md)
loses PID8581/UID10216. Actual Android history records native reason5/status11,
importance100. This is a failed independent stress case, not a timeout converted
into a pass and not proof that Main/shipping reproduces the same event.

## Existing event only

After verifying that the original process is absent, its exact installed APK and
public signer, one same-UID preserving SDK-only upgrade reads only that existing
PID's original Android trace. The activity has no MAUI/Data references or database
startup. The unchanged original bounded reader enforces UID/PID/crashed-thread,
128frames/8MiB, skips other threads, registers, memory and unsupported fields, and
returns only allowed module basenames, relative PCs, BuildIds and original ordinals.
An exclusive marker precedes this inspection. No crash, search, successful parser
suite, intentional signal or consumed RSI/R13/RAX classification is repeated.

| Original frame ordinal | Module | Relative PC | Public symbol correspondence |
| --- | --- | --- | --- |
| 0 | libmonosgen-2.0.so | 0x2f35b4 | major_copy_or_mark_object_concurrent_no_evacuation; major_scan_object_concurrent_no_evacuation |
| 1 | libmonosgen-2.0.so | 0x313537 | drain_gray_stack_concurrent_no_evacuation; drain_gray_stack_concurrent |
| 2 | libmonosgen-2.0.so | 0x312521 | marker_idle_func; thread_func |
| 3 | libc.so | 0x7ac5f | No symbol claim |
| 4 | libc.so | 0x6d94d | No symbol claim |

The original runtime module BuildId is
`b1881505cd1c881ba16fec2997743cbc7033e60d`, matching its actual used APK module
SHA256 `b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034`.
Retained official public symbols match BuildId/CRC and SHA256
`ad8f10412be8b8efdd4d5f3b6bd1e4904c9acd23aaf46321104ed31ea2aab259`.
The first correspondence includes public sgen-marksweep-drain-gray-stack.h:192 and
sgen-scan-object.h:66; later frames include the public concurrent gray-stack and
worker source. No new download, raw trace file, application PE, data section,
register value, address or memory content is exported.

These actual frames locate a concurrent major-GC scan path. They do not establish
an invalid-header value, object type, nursery membership, earlier writer, allocation
cause, runtime defect or repair. This event must not be silently substituted into
the earlier Tarjan/one-shot questions or used to redeploy a rejected runtime patch.

## Reader provenance, refusals and source preservation

Two actual compiler documents bind the original reader and bounded inspection
activity. The SDK-only package is20AOT/30ELF/110static16KiBLOAD, offline/nondebuggable,
ZIP CRC/aligned, minimumAPI31 and valid v3 signature under the same public signer.
Its runtime module differs from the original full App runtime; no product-runtime
identity or shipping acceptance is claimed for that SDK reader. An initially
inherited full-App runtime/v2 expectation refuses before deployment; the actual
SDK/minimum-API/v3 facts are recorded, without resigning/rebuilding or rerunning
completed signature verification. Python string-format warnings likewise do not
cause another inspection. The first metadata filter expects equals rather than
colon delimiters and retains no accepted exit fields; corrected bounded fields
supply the actual reason/status without exporting descriptions or trace content.

After the one inspection, the exact original App APK is restored without reset.
A single necessary read-only source audit in PID9067 executes no cold/warm/search
role and verifies all100000entries/200accounts/1000plans, all32ledger fields,
DDL/header/sequence/table metadata and the original complete Home projection:
`9B4B012173B945D58A7EA19B7CB8DA4C12B8024AC09D8D666145AD7171CEFC00`.
The legacy application digest remains
`094FE4672762C9011770F00237E1BB26537B97F0A999ED0C5F480BF1FDCCB32E`.
No cookie normalization, source repair, key recreation, financial command or
replay of the failed search occurs. Source was unobserved immediately after the
crash; this later complete audit is its actual continuation evidence.

Original public OS/key presence, locale, geometry, boot and reverse mapping agree;
normal emulator shutdown preserves AVD/data/keys/consumed markers and original
ADB server identity. The reader's used signed package, sources/PDBs, bounded
packet, public-symbol bindings and restoration/audit receipts remain. Its standard
Clean is counted in D1219, with no supplemental/frozen cleanup. The overall goal
stays paused; current stress-search acceptance and fault remedy remain open.
