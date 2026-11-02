# Canonical Home schedule-time reader admission (D-756)

## Observed origin and fixed gates

Further analysis of the same D754 trace, without a new recording or workload, separates
190 Managed and1730 External samples inside the five exact markers. The original read
has86 Managed/954 External, projection70/530, validation34/239 and disposal0/7. None of
the Managed leaf frames is unresolved. External samples include waits and are not CPU
costs. Actual original read and validation stacks independently locate
SqliteValueReader.GetTimeOnly -> TimeOnly.Parse -> DateTimeParse.CheckDefaultDateTime ->
DateTime.Now. This is a concrete provider conversion boundary, not a new crash cause.

The exact10.0.12 provider's
[GetTimeOnly source](https://github.com/dotnet/efcore/blob/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteValueReader.cs#L103)
parses GetString using invariant culture for every storage type. A private candidate
recognizes only exact HH:mm:ss with optional one-to-seven fractional ASCII digits;
every other value retains that original conversion/parser. No time, format, model,
schema, permission, currency, financial rule or runtime/GC policy changes.

Before host execution, require at least5% median reader improvement at both10000 and
100000 values, no allocation growth and at most65536KiB working-set growth, six fixed
alternating pairs after one warmup per role. A second process requires first admission.
Native admission separately requires at least5% original Home command-to-ready gain in
both orders and both retained reference/tenfold shapes. Each candidate-minus-original
after-PSS/RSS-high-water and candidate own after-minus-before PSS must not exceed65536KiB.
The first tenfold pair must pass before reverse/reference/phone work; a failed later gate
closes the candidate without resampling, retuning or a relaxed requirement.

## Host contract and implementation evidence

Read only the kept D743 fictitious asset outside frozen scopes:37273600bytes, SHA256
aef54ce6b4fd62648a3034db64413291e631dd2269df8a774fb7383d54da513e.
Its1000 original schedules contain one distinct stored time. The private SQLite time
table repeats that actual distribution for the two stress shapes; it does not create
10000/100000 application plans or financial records. No source export, key, migration
or accepted financial setup is replayed.

Initial12985 actual-provider comparisons cover canonical ranges/fractions and original
legacy/null/numeric/blob/invalid behavior. The first private prototype unnecessarily
queries the storage type: it is40.2419%/37.5501% faster but allocates32bytes more per read,
so its fixed allocation gate fails. Preserve that result. The original getter source
then establishes that the additional type/schema query is not part of the provider's
time behavior. Remove only that extra query and use exactly GetString plus the original
invariant parser fallback. Do not change the canonical parser or gates. Retain12960
unchanged canonical successes and rerun only28 affected entry/fallback cases. The second
independent process repeats timing only, not successful contract cases.

| Process / values | Original median ms | Candidate median ms | Reduction | Both allocation counts |
|---|---:|---:|---:|---:|
|38880 /10000|6.83895|3.38195|50.5487%|400760bytes|
|38880 /100000|42.3415|27.6719|34.6459%|4000760bytes|
|63400 /10000|7.65925|4.09665|46.5137%|400760bytes|
|63400 /100000|78.9286|33.6407|57.3783%|4000760bytes|

These are component-only Windows/runtime10.0.14 results, not Home or phone performance.
The proposed production adapter is sealed, stateless and wraps only the explicitly
tagged original Home schedule read/validation on the exact SQLite reader. All other
commands/readers/getters, metadata, cursor/cancellation/disposal and original financial
writers delegate unchanged. Its named ownership is included without admitting arbitrary
observers to the original complete-ledger transport. Model/compiled model remain unchanged.

Sixteen temporary new wrapper cases qualify these boundaries:15 pass initially; the
missing-column case exposes ArgumentOutOfRangeException instead of the assumed exception.
Correct that branch and continue only that failed case. Eleven affected existing Home
source/session/cancellation/access/block-route cases pass. Strict Windows and complete
offline shipping builds have zero warnings/errors. Candidate shipping SHA256
a2a83a328f699383a7fdcdce8b872fd390ed803ddc81a33b4f0b9124e8c6f7ce,
99463071bytes,384AOT/408ELF/1608 static16KiB LOAD, ZIP/CRC/v2v3 and original runtime14
libraries in both ABIs pass. It is never installed on Main or otherwise adopted.

## Matched native inputs before installation

Two first independent packages are refused before device work because their actual linked
Core bytes differ. Original/candidate AnyCPU ILOnly, method counts and13045 method-name/
signature/IL hashes match, but MVIDs differ; that is not byte-equality admission. Preserve
the first host metadata inspector's empty-array failure and false zero inventory as invalid.
Correct only that reader against the same files. Explicit process-scoped build PathMap
then produces actual byte-equal Core, first in a small dependency check and then the
necessary native rebuild. No SDK/global configuration, product code or gate changes.

Actual matched linked Core SHA256 is
80e3045ef2b07e8d73dbc4cbc6e01252c2f61d3aaf51f2b55047ed6f8a80daf8.
The original Data role compiles the preserved original three files and excludes both
candidate-only helpers; actual compile selection is recorded. Both roles use the same
caller sources, original protected startup and complete source asset. Manifest/source
bytes and actual Mono/compression14 inputs match. No profiler/diagnostic component,
INTERNET permission or custom collector is present. Each192AOT/204ELF/804 static16KiB
LOAD offline/nondebuggable/v2v3 package passes. Whole App/Data PE equality is not claimed.

| Matched role | Signed APK SHA256 |
|---|---|
|Original|d8d8d511e8d1332e3def4982141d02979eebe4545996f6920805cc7c609dd004|
|Candidate|48efb57bb1178dcd1e1da3a0751e7b5c4631e3c7abb016998a15678825528780|

Fresh owned Zanance_D756_CanonicalTime/emulator-5582, API36/x64/page4096, UID10216,
CE352586/DE98307 and boot4af18aa3-4158-4807-92ad-700080227809 remain throughout.
Source transport/preparation occurs once; original Not now is selected once. The first
input guard incorrectly searches the dialog title for its package; it refuses before
input. Correct through the actual focused Window owner10216/session4993/package, keeping
the failure. Both later upgrades preserve original UID/CEDE/boot/source without reset.

Android automatically creates5547 after the first upgrade but leaves the launcher visible.
An early source query fails before real startup; the readiness guard refuses before the
candidate timing marker. Normally bring that same process forward once, SDK WARM587ms/
Wait590ms, then wait for actual Home Ready and complete source equality. The static
historical InvalidOperationException remains recorded; it is never cleared or relabelled.
Only its exactly bound historical pre-startup case, plus a fresh positive complete-source
verification, permits continuation. This is not a product failure or cold/speed acceptance.

## Actual original Home comparison: rejected

The caller invokes the original HomeViewModel.LoadAsync once per actual process and returns
the original read/validation/disposal operations unchanged. Timing ends on actual task/
Ready completion. Full32-field JSON comparison occurs after time; after-memory includes
that same oracle on both sides. Original source/schema/typed rows/sequence/cookies and
complete published ledger JSON match after every sample. Native Home Ready/118 owned
nodes/200 account model rows are observed. No financial command, old accepted query,
source preparation, key or clock is replayed.

| Fixed order | Original / candidate PID | Original / candidate ms | Reduction | After-PSS difference | RSS-high-water difference | Candidate own PSS growth |
|---|---|---:|---:|---:|---:|---:|
|Original then candidate|4993 /5547|1674.3254 /834.2779|50.1723%|-21448KiB|-252316KiB|18874KiB|
|Candidate then original|6386 /6048|741.5445 /715.0832|3.5684%|-7937KiB|1508KiB|21820KiB|

The reverse latency gain misses the fixed5% minimum. Reject without reference10k/phone
work, rescue or extra samples. The first before-PSS gap is136817KiB, reverse gap5022KiB;
warming/preparation/scheduling/GC spread prevents exclusive attribution or a representative
50% Home claim. Existing two-second/physical/cold/stability gates remain open.

For the fixed candidate reverse process only, initial adj200/700 observations leave5547
alive. Once cached=true/adj900 is actually proven, one package-limited am kill proves
death; normal launch gives6048 on the same installed APK. No force-stop, reinstall,
source rearm, reboot or global setting change substitutes for that evidence.

## Exact source restoration and retirement

Archive the actually used rejected product/private/native sources and all failures/results.
Restore the three tracked production files byte-exact to their originals; remove both
candidate helpers and the candidate-only test file. Product/runtime/model/schema/financial
algorithms remain unchanged and no new permanent product tests remain. Do not rebuild or
retest unchanged successful baseline merely to erase the rejected candidate's record.

Retire only the fresh fixture; no own package/pending intent/alarm/notification remains.
Five XMLs192530bytes are hash-matched on host/device and removed. All five OS values,
physical320x640/density160, boot and empty reverse map stay exact. Own QEMU67884/launcher75032
exit and port5582 closes; shared ADB5038/PID66176 remains. D756 sends no phone command;
D752 remains a separate owner-readiness wait before its first actual sensitive SDK prompt.
D749/D751 accepted callbacks and terminal D607 are not replayed.

The first final host archive copy uses the ambient stale checkout and fails before copying;
explicit canonical paths correct that host-only error before any clean. Native retirement
is already terminal and is not repeated. Ten standard Clean commands succeed, removing
70095files/4219278173logicalbytes. Used signed APKs/source/evidence remain outside refused
scopes. Logical bytes are not measured host free-space recovery.

One supplementary removal is rejected before process with blocked by policy. Exactly
24scopes/6682files/6560488073logicalbytes remain frozen. Never retry a subset, alternate
method, move, escalation or reuse. The exact scopes follow.

## Exact frozen supplementary scopes

| Exact frozen path | Files | Logical bytes |
|---|---:|---:|
|`D:\_Projects\Vafadar.Apps\artifacts\d756-canonical-time`|3|7885|
|`D:\_Projects\Vafadar.Apps\artifacts\d756-original-home-comparison`|4|52159|
|`E:\Zanance.ReviewBuilds\D756-host-admission-build`|10|227183|
|`E:\Zanance.ReviewBuilds\D756-product-verification-build`|93|2058154|
|`E:\Zanance.ReviewBuilds\D756-windows-candidate-build`|449|116117632|
|`E:\Zanance.ReviewBuilds\D756-android-candidate-build`|2181|340614929|
|`E:\Zanance.ReviewBuilds\D756-native-original-build`|966|128575449|
|`E:\Zanance.ReviewBuilds\D756-native-candidate-build`|966|128586791|
|`E:\Zanance.ReviewBuilds\D756-native-matched-original-build`|966|128576073|
|`E:\Zanance.ReviewBuilds\D756-native-matched-candidate-build`|966|128587415|
|`E:\Zanance.ReviewBuilds\D756-core-mapped-original-build`|18|135192|
|`E:\Zanance.ReviewBuilds\D756-core-mapped-candidate-build`|18|135197|
|`E:\Zanance.ReviewBuilds\D756-native-source-input`|1|37273600|
|`E:\Zanance.ReviewBuilds\D756-owned-avd`|31|5192766440|
|`E:\Zanance.ReviewBuilds\D756-shipping-candidate-package\_Microsoft.Android.Resource.Designer.dll`|1|732160|
|`E:\Zanance.ReviewBuilds\D756-shipping-candidate-package\pro.vafadar.zanance.apk`|1|99300630|
|`E:\Zanance.ReviewBuilds\D756-native-original-package\_Microsoft.Android.Resource.Designer.dll`|1|732160|
|`E:\Zanance.ReviewBuilds\D756-native-original-package\pro.vafadar.zanance.canonicaltimereview.apk`|1|63451877|
|`E:\Zanance.ReviewBuilds\D756-native-candidate-package\_Microsoft.Android.Resource.Designer.dll`|1|732160|
|`E:\Zanance.ReviewBuilds\D756-native-candidate-package\pro.vafadar.zanance.canonicaltimereview.apk`|1|63455446|
|`E:\Zanance.ReviewBuilds\D756-native-matched-original-package\_Microsoft.Android.Resource.Designer.dll`|1|732160|
|`E:\Zanance.ReviewBuilds\D756-native-matched-original-package\pro.vafadar.zanance.canonicaltimereview.apk`|1|63450106|
|`E:\Zanance.ReviewBuilds\D756-native-matched-candidate-package\_Microsoft.Android.Resource.Designer.dll`|1|732160|
|`E:\Zanance.ReviewBuilds\D756-native-matched-candidate-package\pro.vafadar.zanance.canonicaltimereview.apk`|1|63455115|

The overall goal remains active. This terminal rejection does not qualify general speed,
stability/security, complete DB/key lifecycle, accessibility, iOS, provider, Store or release.
