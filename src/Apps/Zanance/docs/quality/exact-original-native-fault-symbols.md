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
