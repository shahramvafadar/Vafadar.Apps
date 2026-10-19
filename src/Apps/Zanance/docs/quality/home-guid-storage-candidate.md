# Required GUID storage candidate withdrawn (D-382)

## Hypothesis and retained original scope

The complete original reader calls sqlite3_column_type for each required Id and AccountId before the
same original GUID conversion. A bounded candidate carries their TEXT/BLOB storage classes in bits33-36
of the existing trusted row descriptor, avoiding those two per-row native type calls. Nullable GUIDs
keep the original path; every noncanonical representation and intercepted/replaced projection retains
provider parsing/refusal. All32 persisted values, original model/command/transaction/cancellation,
ordering, KDF/key, runtime/GC policy and financial calculations remain unchanged. No schema or compiled
model change. This is a hypothesis about the complete path, not a proven bottleneck from call counts.

Add18 real-storage behavior cases to HomeReadSnapshotTests: both required identity columns, each with
canonical/uppercase/compact/braced/whitespace text,16-byte GUID BLOB,36-byte UTF-8 BLOB, empty text and
numeric input under the real TEXT column affinity. Read an independent EF query as the value or exact
base-exception oracle. Inject only one isolated row, disabling foreign-key checking only on that test
connection to characterize noncanonical storage independently of relationship writes. No production
constraint is disabled. Numeric input here is subject to SQLite affinity, not proof of INTEGER-class
storage in a TEXT column. Compare both public complete-ledger and Home results. Existing projection,
custom converter, required-null, field/error-order and full financial-source guards remain.

All96 related candidate cases pass:0failures/errors/skips in31.014s. The strict test build has0warnings/
errors in14.44s. The old -xml alias emits a runner deprecation notice; later focused execution uses
-result-xml. This is not a compiler warning. No full financial/backend/crypto/resource/UI suite repeats.

## Complete original Android Home

Strict Windows build passes0warnings/errors in36.03s. Independent Release/AOT/trim x64 package build
passes0warnings/errors in127.31s. Package SHA256
0f8f6040d2f27cd2128c96a37405fea0d69bb0fd8a4424abf3d825aa552a7ee4,
55,017,244bytes,192AOT libraries, offline, no debuggable/profileable declaration. All four native engine
entries are byte-identical to the qualified ordinary package; no native prototype or diagnostic GC mode.
LedgerCalculator source remains77170d479a0daa655f9dd501c0295762d8053506580cbc6a186606e6f60f1cb4.

Reuse protected run25b9dab64bf849fcb24e39246d773215, original ciphertext/identities/keys and original
D-377 completed publication samples (comparison42410975b2ec44048eddd2f11174cc1c), without replaying
that successful baseline. Candidate comparison5f0bf040f83949088f2b5a2c8641d699 runs in one PID8016.
All six original en/fa/de reference/tenfold cases finish in that process. Complete raw schema/typed
rows and original financial/native presentation match, with original window focus required and no Save.
Data MVID48ecb887-6147-4ebf-9db7-659df6d6507a; caller9e9afef1-afd1-4314-a588-41de4d1c73bc.

| Shape | Retained D-377 samples, ms | Candidate samples, ms | Median before / after | Shorter pairs |
| --- | --- | --- | --- | ---: |
|10,000 entries|1384.4228,898.7945,956.8721,959.1042,946.1903,852.4272|1660.8702,959.5955,865.7898,1029.6219,1059.0938,969.7762|951.5312 /999.69905|1/6|
|100,000 entries|2661.3362,2593.6021,2843.4448,2524.884,2667.129,3737.6219|2989.978,2658.6938,2831.1847,2959.875,3209.8531,3295.0238|2664.2326 /2974.9265|2/6|

These sequential finite observations reject promotion for complete Home performance. They do not
establish a statistical causal regression or a specific compiler/runtime cause. Do not select only the
fast cases, replay the successful baseline to seek a favorable answer, or extend this rejected candidate
to ARM/phone. Lower native call counts alone do not justify extra SQLite expressions and managed branches.

## Exact withdrawal and useful characterization

Restore both production files byte-for-byte from original HEAD source. Reader SHA256
9b610e5af30e5c47f20c929224211011009102853802b213946ca33d8e610772;
row SHA256597ea67365f420a57b8b7075d91c77268facf119047ecfdd3183ee4ec0ef4924.
The restored strict Windows build passes0warnings/errors in15.31s.
Retain only the18 new required-identity characterization cases: all18 pass against restored production,
0failures/errors/skips in9.320s, after a strict0warning/error rebuild in3.04s. No already successful
unrelated case repeats. The rejected source and immutable package remain only as local evidence.

After the complete result, stop only independent8016, reinstall/verify ordinary D-377 independent
x64 package501ba347e2c9c94631fe35b3324aa1d96f5b9757a0e10c6ee681087ffaabd64f and keep it stopped.
Return original Main6661/My account/Home0. Meaningful native nodes, original settings/size/density and
Main APK d349a8c19b7e3d76c546565a19d781de353415b481542765d70401c29cc7c621 match exactly.
No phone input, Main reinstallation, reset, new keys, reseeding or financial write. Clean owned build/test
outputs after final restored compilation; keep original corpora and immutable evidence.

Local artifacts/home-guid-storage-d382 retain original/candidate sources, test XML, immutable signed
package, current result, paired summary and restoration receipt. This is a rejected optimization and
18 additional compatibility cases, not a shipped speed improvement or a full-suite execution count.
App/APK remain D-377. Q-02/native GC/ANR/cold/low-memory, full-device/reader/iOS/provider/Store and release
acceptance remain open.
