# Android startup packaging investigation (D-334)

## Decision and scope

Retain the existing assembly compression and native-library extraction settings. Neither investigated
alternative qualifies as a startup fix. The main application code and final APK remain D-333; QA-06,
the two-second target, low-memory, physical-device, iOS and release acceptance remain open.

This is a finite investigation of the observed application-binding ANR, not another application test
matrix. SDK36.1.69 already selects lazy AOT loading, disabled assembly preloading, assembly stores and
profiled AOT. Re-enabling those existing choices or recording another profile is not justified.

## Method

Use the owned x64/API36 emulator, transport91, at412x800. Build the complete offline signed Release APK
sequentially, then finish the build before timing. Keep the existing application identity, installation,
financial data, keys, serviced runtime, native cipher and qualified D-320 profile. Do not reset data,
change device settings, drop caches, change licence/telemetry order or bypass storage/access protection.

Two temporary monotonic markers bracket the original Application.OnCreate call. Observe the actual
package-replacement ScheduledAlarmReceiver process through Android process events, matching PID and
those markers; no Activity request is sent during a sample. Capture only that process's public aggregate
memory/stat counters. A completed callback is not loaded Home, total cold start or absence of a later ANR.
An observation timeout alone never permits restarting the same live process.

The SDK supports both experimental switches, but the installed build/runtime combination must still be
qualified. The primary-source compression discussion measures .NET11/CoreCLR/ARM64, not this application's
.NET10/Mono/x64 configuration. It is a hypothesis source, not transferable performance evidence.

## Actual outcomes

| Package configuration | PID | Before OnCreate marker | OnCreate | Through OnCreate | Outcome |
|---|---:|---:|---:|---:|---|
| Existing compression/extraction | 27749 | 7386ms | 10369ms | 17755ms | Callback completes; no matching ANR observed |
| AndroidEnableAssemblyCompression=false | 28044 | unavailable | unavailable | unavailable | Native startup exception; process ends before the first marker |
| extractNativeLibs=false, original assembly compression | 28395 | 11734ms | 12688ms | 24422ms | Actual application-binding ANR before callback completion |

PID28044 fails with java.lang.UnsatisfiedLinkError: libmonodroid.so references the missing symbol
compressed_assembly_count. Android exit-info records APP CRASH(EXCEPTION). This is not a timeout or
failed observer; do not retry it, manufacture the symbol, fork the runtime or ship that configuration.

PID28395 has an actual am_anr event at295332.502 with reason failed to complete startup. Its final
marker arrives at295334.150. A subsequent completed callback does not erase that failure. The process
is still live when aggregate memory is observed; reinstalling the ordinary candidate afterward is
explicit experiment cleanup, not a restart caused only by observation expiry.

Control PSS/RSS are129173/201848KiB; direct-APK PSS/RSS are129828/202720KiB at the observed callback stage.
Public stat major-fault counts are2529/2268 and kernel ticks1009/1408 respectively. These snapshots do
not prove storage, compression, emulator hardware or the licence path caused the original ANR.

## Binary and size limits

All three APKs pass signature verification and retain the permanent package, offline manifest and
non-debuggable Release configuration. The native cipher/runtime payloads remain identical. Rebuilding
the same AOT inputs produces byte differences in all384 AOT libraries, mostly16 data bytes and small
text differences in some libraries; their file lengths remain equal. The compression-off archive also
has476 extra metadata/resource entries from the build. Therefore these are not byte-identical
controlled native-code benchmarks, and a single sample is not a statistical speed comparison.

| Temporary APK | Bytes | SHA256 |
|---|---:|---|
| Default control | 94785269 | 4c9b77546ea6a74f56e04d765086b754e678b9bdb335291883b5b1e541923170 |
| Assembly compression off | 94871455 | a9139a06d28d51aec83793642a17f70d7545016568b5748f8fc7575564648881 |
| Direct native-library loading | 194678517 | f4c94d5602023f01a4b5f020c5bde72bb7ed45e33771af8ed4dfee9629ff6453 |

The x64 assembly store grows from26781392 to57649080 bytes without assembly compression. With direct
native loading, the store stays26781392 bytes but becomes a ZIP Stored entry instead of DEFLATE.
The direct-loading APK has exactly the control's entry set. The larger side-loaded APK and its failed
startup do not justify adoption; Play-repackaged bundles, ARM64, API24 and16KiB systems are not qualified.

## Cleanup and continuation

Restore MainApplication.cs and AndroidManifest.xml byte-for-byte, touch their timestamps and verify no
source diff. Remove no owner data or unrelated package. Reinstall the already qualified D-333 APK with
install-r, without clearing data; its SHA256 remains
4bf9c3fc21976698c175ede0d3b26f92ffaeeb8f58ddd603d07605585d9056c2.
The final ordinary process is28583. An explicit Activity request initially reports timeout after10312ms
and the original process shows the covered preparation page. It is not restarted: a later read of that
same process shows the original My account and Recorded balance0.00 EUR with normal Home actions.
This observation is restoration acceptance, not a cold-start speed claim or a new financial Save.
Temporary markers and experimental properties are absent from the tracked source and that original APK.

No successful unchanged managed, Windows, bank, financial, crypto, backend or UI matrix is repeated;
there is no new unit-test count. Retain the actual crash/ANR evidence and keep the development goal
active. Next investigation must address the unresolved binding path from existing evidence, not
repeat these packaging variants or declare the fault environmental without proof.

Local evidence: artifacts/startup-packaging-d334, including control-1.json, candidate-1.json,
embedded-1.json, scoped process events, phase markers, memory snapshots, package comparisons and
signature/build receipts. Final own-window observations are retained under
artifacts/import-preview-d331/android/d334-final-restored-ready.xml. These temporary experimental APKs
are not owner handoff candidates.

Primary sources: [official native-library extraction guidance](https://github.com/dotnet/android/blob/main/Documentation/guides/extract-native-libraries.md),
[compression investigation and platform limits](https://github.com/dotnet/android/issues/11668),
[assembly-store format](https://github.com/dotnet/android/blob/main/Documentation/project-docs/AssemblyStores.md).
