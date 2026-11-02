# Sparse native unwind metadata and the nursery-pinning diagnostic (D-760)

2 November 2026. Private owned API36/x64/4096 diagnostic only. Product source,
production runtime/GC policy, financial behavior and the global SDK remain unchanged.
This locates a specific diagnostic invariant; it does not repair the original crash.

## Evidence gained

D759's record stopped at the Android wrapper because the installed SDK wrapper lacks
`.eh_frame`, `.eh_frame_hdr`, `.debug_frame` and PT_GNU_EH_FRAME. Both official Release
and Debug wrappers lack these sections. A Debug-wrapper swap is rejected before build.
The exact Release wrapper retains RBP frame pointers. Sparse non-loadable `.debug_frame`
metadata now permits Android's own unwinder to cross its three admitted fatal paths.
No signal/logger replacement, heap/header/register export or changed instruction is used.

The modified wrapper preserves all program headers and `.text` bytes. PT_LOAD bytes
match except ELF section-index header fields at offsets40/60. Its linker BuildId remains
unchanged, but its whole-file digest changes; BuildId alone is not identity proof.
Original SDK SHA256 is `293eb7c8752bf678c6aa317e80f051305bb9c31926d83155a5807393fe7ae784`;
private metadata-bearing SHA256 is
`b513e27fbb367995b3fe02d4919c717813059de42d9e7948a04b085d04ad7d91`.
Actual APK extraction verifies the original/modified modules and the unchanged global SDK.
The admitted FDE ranges are `[0x61d34,0x61d46)`, `[0x85982,0x85996)` and
`[0xb2db4,0xb30ef)`; the second covers only the fatal branch, excluding its return epilogue.
CFA is RBP+16, saved RBP at CFA-16 and saved RIP at CFA-8. This is a private diagnostic
artifact, not a shipping candidate or complete SDK source reproducibility claim.

## Failed control retained, format boundary corrected

A fresh native three-function fatal control first fails the required C/B/A chain with
CIEv3: UID10216/PID4687 exports only C at0x5a8. Host LLVM acceptance was insufficient.
Android's DwarfSection defaults that implicit DWARF32 format to a four-byte address;
CIEv4 explicitly supplies address_size8/segment_size0. Correct only this metadata
boundary, keeping functions, ranges, CFA rules and acceptance criteria unchanged.
A second fresh control UID10217/PID5169 yields ordered C/B/A at0x5a8/0x598/0x588;
its five native frames export four admitted code frames and one unlisted frame.
The failed control remains failed; neither it nor earlier successful D759 parser cases
are rerun. The common bounded parser is unchanged, with new exact module catalogs.

The initial fresh-control CRT still contributed `.eh_frame` despite the compiler flag;
its guard refused before installation. Removing only that fresh control's unwind section
establishes the intended SDK-like baseline before metadata derivation. A timestamp guard
also refused before reading the record; final control correlation uses an exclusive
pre-input guest marker and exact inode/nanosecond mtime/size binding. No deadline changes.

## One actual original diagnostic startup

Ordinary preparation5509 and one diagnostic5757 share fresh UID10218, CE361888/DE66522
and boot `d055a4ea-01d9-409b-9de7-13db4e224ef8`. Android records reason5
APP CRASH(NATIVE), status6, crashed thread5850, trace null at local05:46:42.700.
The prior ordinary process ends reason16 PACKAGE UPDATED, not a crash.
The OS record has15 frames;14 admitted relative code frames are exported and one is
unlisted. Raw protobuf, memory, registers, absolute stack, object address/type/header
and private Main data never leave the emulator. The parser wipes skipped opaque data.

| Ordinal | Module | Relative PC | Resolved context |
|---|---|---|---|
| 0 | libc | 0x6a424 | Native termination |
| 1 | libmonodroid | 0xb30af | abort_application |
| 2 | libmonodroid | 0x61d45 | abort overload |
| 3 | libmonodroid | 0x85995 | fatal mono_log_handler |
| 4 | Mono | 0x1f9d9b | monoeg_g_logstr, goutput.c:151 |
| 5 | Mono | 0x1f9e4d | monoeg_g_log and inline logging contexts |
| 6 | Mono | 0x2e24cd | check_marked_callback, sgen-debug.c:578 |
| 7 | Mono | 0x2e2432 | sgen_check_heap_marked, sgen-debug.c:597 |
| 8 | Mono | 0x2dd6a3 | major_finish_collection, sgen-gc.c:2383 |
| 9 | Mono | 0x2d7e38 | major_finish_concurrent_collection, sgen-gc.c:2566 |
| 10 | Mono | 0x2d7cd2 | collection/free-space inline contexts |
| 11 | Mono | 0x2e6789 | sgen_los_alloc_large_inner, sgen-los.c:461 |
| 12 | Mono | 0x2d3da4 | sgen_alloc_obj_nolock, sgen-alloc.c:184 |
| 13 | Mono | 0x2c6845 | mono_gc_alloc_vector, sgen-mono.c:1119 |

Inline source contexts are not extra physical frames. Mono BuildId is
`b9db63cc66e53a97dc8483280f7aab8a31672169`, original SHA256
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`;
matching public debug-file SHA256 is
`8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0`.
The SDK BuildId is `ff7bf4af27e1ccfc88be6e5cbd98a6a2c5f5e4fc`.

At runtime revision `0427b9abbead3fa64c6655eb4290c537397131fa`,
[check_marked_callback](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-debug.c#L578)
requires every remaining nursery object to be pinned when its flag is true.
Exact original machine code tests that flag and pinned bit before its error call at
0x2e24c9..0x2e24cd. The reported PC is the error-call return, not a SIGSEGV fault
instruction. The caller is the heap-marked check during concurrent major finish,
after pinning completion, TLAB clearing, pin-stat reset and cement clearing.
This identifies the failed nursery-pinning invariant in this one `check-mark-bits`
diagnostic. Its known CLEAR_AT_GC effect remains material. Original D678's ordinary
SIGSEGV, D743's historical assertions, offending reference type and earlier writer
are not established by this record. No assertion suppression, pin-all workaround,
mode-disable repair or speculative production patch is accepted.

The next missing technical input is source-supported nursery pin lifecycle/concurrent-
finish investigation. Do not replay this closed diagnostic, successful native matrix,
financial input or rejected GC/performance premise. Limited exact public issue searches
found no match; that is not proof that no upstream bug or fix exists. No issue/upload
was made and no unrelated search result was used as evidence.

## Restoration and finite verification

The ordinary signed APK is restored once to5939, with the same UID/CE/DE/boot.
The full typed source, schema/source cookies and all32 fields of the original100000
ledger rows match the post-startup control exactly, without revision/schema/row
normalization. A normal Transactions route and one read establish that comparison;
no financial Save, seed, Import, Delete or Undo is replayed. This is not whole cipher,
key-lifecycle or general stability proof. The phone is untouched.

Four used signed APKs remain: failed control `affc01ec`, passed control `dc58c128`,
ordinary `6ffa4689` and diagnostic `96d302d0`. Full original/diagnostic APKs each pass
strict offline192AOT/204ELF/804 static16KiB LOAD/signature/CRC/ZIP checks; controls each
pass10AOT/21ELF/72 static LOADs. Static alignment is not hardware16KiB runtime acceptance.
No product tests, Windows build or unchanged full suite are repeated. Whole managed/AOT
PE identity and performance are not claimed. Used reader binaries and private source
archives are retained separately from every rejected cleanup scope.

All three fresh packages retire. Three owned hash-matched XMLs/12525bytes retire;
only the owned guest helper directory is removed. Five original OS values, geometry
320x640/density160, boot, empty reverse and shellUID2000 restore. Launcher7276/QEMU47196
exit; shared ADB5038/PID66176 remains. Official owned AVD deletion removes24files/
4987368616 logicalbytes. Four standard Clean commands remove27610files/1471504216
logicalbytes. These are logical owned-output counts, not measured host free-space gains.

The first launcher failed UnknownAVD because its scoped ini was under android-user/avd.
The corrected same launcher then needed its owned emulator-home directory. Creating
that missing directory allowed the same process to boot; no timeout restart occurred.
The missing directory produced a repetitive host log. Create all scoped directories
before launch and use bounded binary snapshots of live logs; a live Tail read streamed
repeated output and its read-only reader was stopped, not the emulator. Preserve these
host/observer failures rather than classifying them as product defects.

## Cleanup refusals: exact frozen scopes

Automatic review rejects two independent deletions before process with `blocked by policy`;
no more specific reason is supplied. No retry, subset, alternate deletion, move,
escalation or reuse is permitted. Do not read/hash/truncate the frozen repetitive log.
The exact log is:

`C:/Users/shahram/AppData/Local/Temp/Zanance-D760-16998eaa5d4f480cb517db25af551d7d/owned-avd-corrected-stdout.log`

It retains1894963078 logicalbytes. A separate twenty-target cleanup retains2075files/
440440547 logicalbytes. Every exact target is listed below; signed APKs and retained
source/evidence archives are outside. No older frozen scope is touched.

- `E:/Zanance.ReviewBuilds/D760-control-build`
- `E:/Zanance.ReviewBuilds/D760-control-v4-build`
- `E:/Zanance.ReviewBuilds/D760-original-build`
- `E:/Zanance.ReviewBuilds/D760-diagnostic-build`
- `E:/Zanance.ReviewBuilds/D760-original-source-input`
- `E:/Zanance.ReviewBuilds/D760-owned-avd`
- `E:/Zanance.ReviewBuilds/D760-sdk-unwind-metadata`
- `E:/Zanance.ReviewBuilds/D760-sdk-unwind-metadata-v4`
- `E:/Zanance.ReviewBuilds/D760-native-components`
- `D:/_Projects/Vafadar.Apps/artifacts/d760-unwind-control`
- `D:/_Projects/Vafadar.Apps/artifacts/d760-unwind-control-v4`
- `D:/_Projects/Vafadar.Apps/artifacts/d760-original-unwind`
- `E:/Zanance.ReviewBuilds/D760-control-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D760-control-package/pro.vafadar.zanance.unwindmetadatacontrol.apk`
- `E:/Zanance.ReviewBuilds/D760-control-v4-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D760-control-v4-package/pro.vafadar.zanance.unwindmetadatacontrolv4.apk`
- `E:/Zanance.ReviewBuilds/D760-original-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D760-original-package/pro.vafadar.zanance.nativeunwindreview.apk`
- `E:/Zanance.ReviewBuilds/D760-diagnostic-package/_Microsoft.Android.Resource.Designer.dll`
- `E:/Zanance.ReviewBuilds/D760-diagnostic-package/pro.vafadar.zanance.nativeunwindreview.apk`

## Continuation

D759's single previous-head CI response reports CodeQL and CI success; its monitor is
deleted. Actual elapsed time was18.6834minutes, not exact16 or within15-18minutes.
D760 receives one later exact-head check after push. D752 genuine owner readiness
remains pending; do not release its held SDK call or replay CSV on elapsed time.
D749/D751/D607 stay terminal and are not recreated. The broader delivery goal remains
active; this private diagnostic is not Android-wide security, stability, two-second
performance, accessibility, iOS, provider, Store or release acceptance.
