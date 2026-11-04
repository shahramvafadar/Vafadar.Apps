# Exact symbols for the retained original native fault (D-679)

## Outcome and unchanged workload

The actual uninduced D678 native exit, PID 10110/UID 10216, is now mapped to exact
runtime functions and available source locations. No application process, emulator, phone interaction, UI matrix,
financial fixture or crash is replayed. No app/runtime/GC change, Windows/Android build
or managed test is performed; the product test count changes by zero. This completes a
bounded symbol/source diagnosis, not the cause or repair of the original crash.

The fault is inside SGen pointer scanning/copy-or-mark while a large-object card-table
scan finishes a major collection triggered by array allocation. That locates the failing
runtime operation. It does not identify the invalid reference's origin, the managed array
type, a failing application writer, an application-level race or an exclusive runtime bug.
It does not connect the earlier managed resource-dictionary exception or EXIT_SELF255
records to this native fault. Stability/QA-06 remains In progress.

## Identity chain before interpreting symbols

The existing local official runtime package's x64 libmonosgen-2.0.so is byte-for-byte equal
to the module in D678's actually used adc8APK, SHA256
edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4.
All 13 original relative PCs lie inside its executable ELF segments and carry the same
GNU BuildID b9db63cc66e53a97dc8483280f7aab8a31672169. Fault ordinal 0 remains ordinal 0;
the one discarded non-library frame never becomes a guessed fault location.

The stripped package contains dynamic symbols and a GNU debug link, not the containing
private function symbols. The matching libmonosgen-2.0.so.dbg is retrieved once from the
[official public symbol service](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-symbol)
using the original public ELF alone, explicit project-owned output/cache and no token,
authentication, dump, private memory or financial input. The free MIT-licensed
[official symbol downloader 10.0.750501](https://www.nuget.org/packages/dotnet-symbol/10.0.750501)
is installed only in a project-owned tool directory, with an isolated official NuGet source;
there is no global tool, PATH, SDK or system configuration change. It is a host diagnostic,
not a product dependency. Its upstream [MIT licence](https://github.com/dotnet/diagnostics/blob/main/LICENSE.TXT)
was reviewed before installation.

The downloaded debug file is 15762568 bytes, SHA256
8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0.
Both its GNU BuildID and the original ELF's GNU debug-link CRC32 match before symbol use.
Each PC is inside the actual symbol's positive-size function range; no nearest-name guess
or offset extrapolation is used. An already installed Visual Studio x64 LLVM symbolizer
then resolves the same 13 PCs and preserves its inline/location entries. No new symbolizer
or Python package is downloaded.

## Exact native locations

| Original ordinal | Relative PC | Exact containing function | Actual source location |
|---|---|---|---|
| 0 | `0x2edd7d` | `major_scan_ptr_field_with_evacuation` | [sgen-marksweep-drain-gray-stack.h:321](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-marksweep-drain-gray-stack.h#L321) |
| 1 | `0x2c667b` | `sgen_client_cardtable_scan_object` | [sgen-mono.c:1051](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/metadata/sgen-mono.c#L1051) |
| 2 | `0x2d58e5` | `sgen_cardtable_scan_object` | [sgen-cardtable.c:620](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-cardtable.c#L620) |
| 3 | `0x2e7754` | `sgen_los_scan_card_table` | [sgen-los.c:813](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-los.c#L813) |
| 4 | `0x2de0e1` | `job_scan_los_mod_union_card_table` | [sgen-gc.c:1570](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.c#L1570) |
| 5 | `0x3130c9` | `sgen_workers_enqueue_job` | [sgen-workers.c:184](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-workers.c#L184) |
| 6 | `0x2ddc76` | `major_copy_or_mark_from_roots` | [sgen-gc.c:2214](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.c#L2214) |
| 7 | `0x2dd527` | `major_finish_collection` | [sgen-gc.c:2311](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.c#L2311) |
| 8 | `0x2d7e38` | `sgen_perform_collection` | [sgen-gc.c:2739](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.c#L2739) |
| 9 | `0x2d7cd2` | `sgen_ensure_free_space` | [sgen-gc.c:2616](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.c#L2616) |
| 10 | `0x2e6789` | `sgen_los_alloc_large_inner` | [sgen-los.c:461](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-los.c#L461) |
| 11 | `0x2d3da4` | `sgen_alloc_obj_nolock` | [sgen-alloc.c:184](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-alloc.c#L184) |
| 12 | `0x2c6845` | `mono_gc_alloc_vector` | [sgen-mono.c:1119](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/metadata/sgen-mono.c#L1119) |

The fault PC 0x2edd7d has an inlined major_copy_or_mark_object_with_evacuation entry with
line 0 (unavailable), inside major_scan_ptr_field_with_evacuation+61. Its outer DWARF
entry points to sgen-marksweep-drain-gray-stack.h:321, the HANDLE_PTR invocation.
The exact inner dereference/line is not recovered or guessed. The next frame's actual
sgen-mono.c:1051 calls scan_ptr_field_func while scanning reference-array card contents.
This supports the operation classification above, not the origin or ownership of a bad value.

The package nuspec identifies https://github.com/dotnet/dotnet at
0427b9abbead3fa64c6655eb4290c537397131fa, also recorded in Microsoft.NETCore.App.versions.txt.
An initial dotnet/runtime repository assumption returns404; it is not used as source evidence.
All seven source files are subsequently read from the actual package repository/commit.
Original raw source bytes/line numbers and hashes are checked against the DWARF entries;
web text extraction can collapse blank lines and is not used to number source statements.
Sixteen inline/location entries correspond to thirteen physical native frames, not sixteen
new native events. Actual private heap, registers, raw tombstone, absolute stack and unrelated
crashes remain unexported.

## Cleanup and preserved evidence

The one-off scoped downloader is successfully uninstalled through the standard tool command.
The used debug file, original-source copies, module/function/source identity proofs and
diagnostic outputs are retained for the unresolved original failure. No used app APK or
global runtime/NuGet cache is removed. A single supplemental deletion of the duplicate
symbol cache and empty scoped-tool root is rejected before process creation with
`blocked by policy`: two roots, one duplicate 15762568-byte file remain. No retry, subset,
alternate deletion, move, escalation or reuse is performed or authorized for that scope.

## Remaining admission

The next stability action needs a concrete original reference-origin hypothesis/minimal
failed-condition input, or a verified official fix that addresses this exact path. Function
identity alone does not authorize a speculative runtime/GC switch or revival of rejected
performance candidates. Successful D678 rendering/Retry/source checks are not repeated as
stability evidence; the source/data guards and actual failures remain intact.

Samsung/D607's three original deadlines, consumed reboot and unread post-reboot source are
untouched. Main adoption retains the existing D-607 completion/cleanup prerequisite. No new
claim is made for whole-device stability/security, two-second Home, broad accessibility,
iOS, genuine providers/public hosting/Store or publication. The delivery goal remains active.

## D-700 follow-through - exclude an already present official correction

Official SGen PR125116/release10 backport125213 changes non-array LOS card-start alignment. Exact original module/debug identity and complete decoding of its two relevant native functions prove that alignment is already present at0x2d5957. The original client frame remains the separate reference-array pointer-call path, not a new failure reproduction. Reapplying/upgrading for this missing-fix hypothesis is rejected; original bad-reference origin and general stability remain unresolved. No app/runtime/GC/financial or device change occurs.
[Actual native instructions, official source and causal limits](original-sgen-card-scan-correction-admission.md).

## D-708 - Distinct generated reference-array input

One unchanged-original-SGen/default-concurrent-collector native input performs53,891,361 generated old-to-young array stores/checks with2257 major-counter increments during120seconds, without observed failure. This does not reproduce, refute or fix the original fault. The missing-Delegate-barrier issue title was withdrawn and its actual non-array correction is the already excluded D700 path. No product/runtime/GC/financial change or original UI replay occurs.
[Exact input, counters, original-source qualification and exclusions](bounded-reference-array-runtime-origin.md).

## D-711 - Distinct growing reference-list input

Current original Home reader growth and exact runtime library source admit a separate bulk-copy-family question. A single unchanged-SGen/default-GC runtime-only input completes1467cohorts/146700000nodes and bounded identity checks with actual collection increases, without observed failure. It doesnot identify the original failing managed array or bad-reference origin, prove every native branch, reproduce/refute/fix the crash or justify another configuration switch. Product/financial/UI cases remain unchanged.
[Source admission, actual counters and remaining causal boundary](original-list-growth-runtime-origin.md).

## D-712 - Exact original top-frame instruction

The original1001byte function is now decoded completely into283byte-matched instructions with an existing signed static utility, without a new package or application/native run. OriginalPC0x2edd7d/488b3b is mov rdi,qword ptr[rbx], an eight-byte read of the expected referenced object's first word on the decoded normal path; the prior field load/zero test are separate. Actual pointer/header/register values and origin remain unknown, and optimized inner DWARF line0 still does not identify a unique C statement. This narrows the operation without claiming cause or repair; the one1061byte temporary container is removed, original artifacts/textual proof retained.
[Exact original byte mapping, method and limits](exact-original-fault-word-load.md).


## D-762 - Recent official fixes do not admit an Android fault remedy

2 November 2026. Review the actual merged changes before proposing a runtime upgrade.
[Runtime PR136352](https://github.com/dotnet/runtime/pull/136352), merged27October2026
as152ad965267240a3e23b6b3a24761e37aecc8c79, removes managed attribute-argument
allocation from UnmanagedCallConv decoding. Its demonstrated failure is inside the AOT
cross compiler: a64-bit host lays out classes for a32-bit browser-wasm target, so a
managed Type-array allocation and target layout disagree. The admitted original D678
record is an already built Android/x64 application's on-device SGen reference scan,
not that host compiler execution or a32-bit-target/64-bit-host allocation. The PR also
changes image resolution/null handling; neither is identified in the retained original
stack. This is not proof that the patch has no benefits elsewhere, only that the
published compiler reproducer does not establish a remedy for this recorded failure.

[Runtime PR132306](https://github.com/dotnet/runtime/pull/132306), merged19August2026
as7aac3c7ad953aaa23bad1d7867e7a0ec2c4a629c, changes OP_MOVE reference pin-area emission
for LLVM-AOT WebAssembly. Read its actual compiler patch rather than inferring a shared
cause from the words GC or stale reference. The changed emission and definition-count
setup are under TARGET_WASM. In our exact original revision
0427b9abbead3fa64c6655eb4290c537397131fa, emit_gc_pin at mini-llvm.c:3982 is likewise
under TARGET_WASM, and the OP_MOVE exclusion at12672 is inside the guard12670..12674.
Android/x64 is not this WebAssembly target. The upstream regression's ability to run
on another runtime does not make its wasm-only compiler fix an admitted Android remedy.
No new alias stress loop, target switch or replacement runtime is warranted here.

The issue123537 discussion remains open with heterogeneous reported signatures and
no attached standalone reproducer in its latest8October comment. Neither another
application's collector/nursery choice nor a preview-runtime report is a causal
before/after result for our source/module/workload. D700's already present non-array
card correction and D761's incompatible diagnostic phase remain separate exclusions.

These are two bounded target/phase admissions, not a complete audit of every upstream
fix. The original referenced object's type, earlier writer and applicable remedy remain
unknown. Only public source/PR metadata and an exact original compiler-source excerpt
are newly retained. No compiler/SDK download, package/build/test, device command,
financial operation, product/runtime/GC change or frozen-scope access occurs. Product
test count stays unchanged. D752 readiness stays owner-only; D749/D751/D607 remain
terminal and broader delivery stays active. Select a new concrete original-path input
before another runtime experiment; do not promote these unrelated reproducers as fixes.

## D-767 - Selected scanner-history and unrelated closure admission

2 November 2026. Continue QA06 with public source only, without another cold sample,
financial replay, collector/runtime change or device command. Search results and a
closed issue are candidate references, not a demonstrated product remedy.

[Runtime118896](https://github.com/dotnet/runtime/issues/118896)
is closed after maintainers locate its original ARM64/9.0.6 stack-frame write in
init_amodule_got and discuss probable stack overflow with insufficient further evidence.
The closure itself is not a published SGen reference-array correction. Our admitted
D712 operation is instead the x64 referenced-object first-word read. No identical
cause, runtime upgrade or repeated workload is justified by that closure. The issue
number, discussion and closure are authoritative; no fabricated patch is inferred.

Read bounded default-branch histories for exactly four original scanner files, then
retrieve each latest listed change by immutable SHA. Compare with original VMR
0427b9abbead3fa64c6655eb4290c537397131fa, using retained exact source where available.
These are finite selected paths, not an exhaustive search of runtime fixes or proof
that no correction elsewhere can affect the fault.

| File | Latest selected history SHA | Exact comparison with original |
|---|---|---|
| metadata/sgen-mono.c | b54a16e65ddd3f9580732ea03c942729e1c7808d | One background callback rename, guarded by HOST_WASM and DISABLE_THREADS |
| sgen/sgen-marksweep.c | a3fe47ef1a8def24e8d64c305172199ae5a4ed07 |96942 bytes exact; SHA256187186e75cce1955d03fed8ac888256ac79578928e64755338388dc8bb6e1ff3|
| sgen/sgen-marksweep-drain-gray-stack.h | ff4d3d4fa9b918a2a89d6cf614345e72a9cc453d |11295 bytes exact; SHA25650ab37ab96e90bffa48373ab18865a887b228219b29ab370296e4787cc3c26f6|
| sgen/sgen-scan-object.h |4c0a7c4c22cb09285c28f8b817263bc2765e5fc4|3324 bytes exact; SHA256c2d8b541237fb8d5c78c957c89dca9551bfc8679df4e691857355709b8e07793|

The original metadata file SHA2560400cded9cd7175318c5f4942e72df0ed770fd3e9b9795a0b02a0653be9610c4
and selected78340-byte file SHA256f893847934a6ab40e747ffa46c8120426815e7961b3992459fa74934dd9a7472
differ only in mono_main_thread_schedule_background_job becoming SystemJS_ScheduleBackgroundJob.
Exact single-replacement comparison and surrounding preprocessor guard are checked;
this is not an Android scanner change. The original blanket four-file equality
assumption fails and is retained, then actual differing source is reviewed rather
than normalized away. An earlier local serialization adds one final LF to six fetched
representations; those initial files/refusal are retained and only that extra LF is
removed to recover the exact fetched UTF8 lengths before comparison.

The selected histories, immutable source bytes, digests and actual-difference receipt
are retained locally. No temporary binary, SDK, test fixture, app/AVD, cleanup scope
or product source is created or changed. No old successful tests/builds or native
classifier run again. All frozen scopes remain untouched. D752 genuine readiness,
original reference origin and wider gates stay open; D749/D751/D607 stay terminal.


## D-809 - Current large-source Budget read and native exit

One retained100k source reaches original Ready/14category rows and Back, then
PID5101/UID10216 has actual OSnative5/status11. A real guarded record locates
concurrent object scanning at2f0f66; the old categorical guard correctly refuses
the new PC. Source-only SDK5913 verifies full original schema/typed rows/sequence/
32fields without normalization or UI/financial replay, with no MAUI window.
Owner-approved R13 observation returns both slot labels refused-unknown;29 private
component controls pass. One preserving restart retains the same package/data/record;
no app/financial replay occurs. Independent work and the overall active goal continue.
No product/runtime/GC/phone change or general repair/acceptance is claimed. See [actual evidence](current-large-budget-native-exit.md).
