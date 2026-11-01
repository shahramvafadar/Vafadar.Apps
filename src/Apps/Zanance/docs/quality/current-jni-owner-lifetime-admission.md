# Current packaged JNI owner-lifetime correction admission (D-739)

## Concrete question and current source

Continue independent Android fault investigation while D737 awaits the owner's real
screen-lock decision. The owner reports None; no real authentication, global security
setting or competing phone input is performed. Existing protected search measurements
and native result-row evidence are retained, not rerun as new work.

The [D712 original fault](exact-original-fault-word-load.md) is the referenced-object
first-word load in SGen, not a proven JNI GetObjectClass or exception-proxy failure.
[D700](original-sgen-card-scan-correction-admission.md) already excludes a missing
non-array card-alignment correction. This admission asks whether a separate official
JNI owner-lifetime change is present in the actual current APK; it does not identify
that change as the original fault's remedy.

## Official corrections and causal boundary

The official [Android issue12765](https://github.com/dotnet/android/issues/12765)
concerns native GetObjectClass failures. Its discussion distinguishes borrowed native
handles from managed owners and links two source corrections. It does not establish
that our original SGen fault has the same origin.

[PR12315](https://github.com/dotnet/android/pull/12315) changes borrowed-handle owner
lifetimes and temporary-wrapper local-reference handling across several SDK paths.
[PR12754](https://github.com/dotnet/android/pull/12754) adds four owner-lifetime guards:
GetJniTypeName, JavaAs, managed-exception Throw and RaisePendingException. That PR
explicitly limits the claim: source-level gaps are addressed without a demonstrated
fix for the reported production crash or a deterministic timing reproducer.

The separate [runtime issue123537](https://github.com/dotnet/runtime/issues/123537)
remains a discussion of Mono GC failures and possible diagnostics/runtime alternatives,
not a cause-specific correction admitted for our original module. Other applications'
collector/nursery observations are not our before/after evidence. No collector switch,
preview-platform migration, copied workload or random stress loop is justified here.
This is a finite source/issue admission, not proof that every upstream change was searched.

## Inspect the current package rather than assuming from version labels

Revalidate the retained complete signed D734 APK:
`dc7741202ce5007fd8e70cb185bc16d90e4d25ecd9e03aa72184a13f002a138e`,
99,454,879 bytes. It is the existing current Main package; no install, device query,
process launch, build or financial operation is needed.

Installed public SDK 36.1.69 Mono.Android metadata contains original source identifier
`d549e1d` on `release/10.0.1xx`. Retrieve its exact official
[store documentation](https://github.com/dotnet/android/blob/d549e1d/Documentation/project-docs/AssemblyStores.md)
and [native structures](https://github.com/dotnet/android/blob/d549e1d/src/native/clr/include/xamarin-app.hh).
The initial observer incorrectly expected low format version 2 and refuses before
extracting an assembly. The actual source declares version 3 with 64-bit/ABI flags;
retain that refusal and correct only the static observer's expected header.

For both actual APK stores, admit ELF64 little endian, exactly one payload section,
206 descriptors/412 packed index entries, exact header version 0x80010003 ARM64 or
0x80030003 x64, bounded original descriptor/name ranges and only the two named public
SDK modules. No app assembly, licence/configuration value or private database/key is
inspected or exported. The store header/name metadata is used solely to locate
Java.Interop.dll and Mono.Android.dll.

Decode only those four compressed public images with the existing SDK's own
K4os.Compression.LZ4.dll. Validate original XALZ header, complete declared output,
MZ/PE metadata and method-body operand boundaries. The original compression-descriptor
index is retained separately from store mapping index, never assumed equal. The
public SDK modules are read as metadata, never loaded or executed as Android code.
A read-only PE reader resolves call/member tokens, including generic specifications,
without instantiating app code or accessing live memory.

| Actual linked APK module | ARM64 bytes / SHA256 | x64 bytes / SHA256 |
|---|---|---|
| Java.Interop.dll | 171008 / `18ac882f347887b84ba3848b50944815704c1e43764499ade8727f8ae16fe26c` | 171008 / `18ac882f347887b84ba3848b50944815704c1e43764499ade8727f8ae16fe26c` |
| Mono.Android.dll | 2252800 / `2a4b3ae95bbd9630b89e76637561b5a1ecb75fa2b6e3a62754e23f9820d24b96` | 2533376 / `f5395781b9d38f6bf05807a1305e27403e939e82e96fcea7d3f22e2232b56526` |

## Actual retained and removed paths

| Official target | Actual current linked result, both ABIs |
|---|---|
| PR12754 GetJniTypeName / JavaAs | These method definitions are absent after linking. Their presence in unlinked SDK input is not shipped-path evidence. |
| PR12754 JniEnvironment.Exceptions.Throw(Exception) | Original 43-byte body retains the proxy constructor, borrowed PeerReference and Throw call; no System.GC.KeepAlive call. |
| PR12754 AndroidRuntime.RaisePendingException | Original 29-byte body retains proxy creation, borrowed PeerReference and Throw call; no System.GC.KeepAlive call. |
| Selected PR12315 JNIEnv.ToLocalJniHandle | Original 38-byte body retains the existing borrowed-owner branch/NewLocalRef path with no System.GC.KeepAlive call. This is not a check of every PR12315 path. |
| Raw-reference Throw controls | Separate 53-byte Java.Interop and 37-byte Mono.Android overloads are recorded, not misidentified as the managed-exception overload. |

The same reader positively resolves the existing original SDK View.set_LayoutParameters
71-byte body's System.GC.KeepAlive call at IL offset 64. This distinguishes a missing
call in the selected methods from a decoder that never resolves KeepAlive. It is a
static public-SDK control, not another product test or native run.

All ten selected linked bodies are decoded completely. Two source-level guards from
PR12754 are absent in retained methods, while two other targeted methods were trimmed;
this is stronger and narrower than inferring wholesale exposure from an SDK version.
It establishes neither that the missing guards ran during our original crash nor that
adding them would repair SGen reference-array corruption. No replacement framework,
private SDK patch, exception suppression or application-level blanket KeepAlive is
adopted. A supported candidate would need exact official input/compatibility admission,
actual affected-path evidence and focused before/after qualification before any repair claim.

## Preservation, retirement and remaining requirements

Original signed used APK, installed SDK, exact official diffs/source hashes, static
reader source and bounded textual method/call evidence remain. After complete hash
verification, only eight redundant derived public-module extraction files retire:
6,728,741 logical bytes. This is not measured host free space. No recursive deletion,
new package/tool installation, product source/dependency/runtime change or retry of
any frozen cleanup scope occurs. No product tests or previous successful workload
are rerun; new app-unit-test count remains 0.

D737's genuine-authentication gate remains open with zero requests; None is an
owner-reported current setting, not authority to configure a credential. D607 remains
terminal with its original reboot consumed and fixture absent. Main and caller are
untouched. General stability, original bad-reference origin, cold/reference performance,
security, accessibility, hardware 16KiB, iOS/providers/Store/release and the overall goal
remain open. Android still precedes iOS and further TalkBack remains last.

Subsequent [D737 real device-credential qualification](physical-original-device-credential.md) completes the earlier owner-decision wait; the independent JNI admission and unresolved causal limits remain unchanged.
