# Default Android Core integration and isolated Linux producer (D1187)

Date: 2026-11-15. This continues the retained
[source producer](private-maui-reproducible-producer-d1184.md) and
[retained-font correction](android-retained-font-configuration-d1186.md).
It changes build preparation and selected assets, not application UI, financial
behavior, runtime/GC configuration or the original native-fault diagnosis.
Phone/Main and protected cohorts receive no command.

## Default build path

Normal repository targets now select the prepared private Core for MAUI Android
consumers. Other platforms and the plain Android SDK bootstrap retain their
original references. The actual final evaluation has one private reference for
Android, original Core compile/runtime excluded, the prepared version3 digest
selected, and zero private references for Windows/bootstrap. The early attempt
to derive a selection filename from central PackageVersion items failed because
those items are imported later; the retained failure preceded compilation.
The explicit import-time version fixes that ordering.

`Build-AndroidApk.ps1` prepares/verifies Core before restore. CI/release first use
`eng/maui/AndroidDependencies.csproj` for SDK dependencies, without restoring a
MAUI private-Core consumer. Actual bootstrap restore passed even with its mapped
private feed absent. No SDK installation or licence acceptance was needed for
that control. Hosted dependency-install policy is unchanged. CI caches only
complete pinned public input archives, not successful outputs or partial files.

## Real Linux host and tools

The existing official SDK10.0.404 image was admitted against the anonymous MCR
index, Linux manifest, config and every local layer diffID. No image was pulled.
An initial admission comparison omitted the matching index ID and refused; the
corrected index/layer proof is retained. The image's actual SDK is10.0.404.

The owned container has a read-only root, limited CPU/memory/PIDs, dropped
capabilities and no device, published port, credential, Docker socket or general
repository mount. Only an owned minimal producer workspace is writable; six
public archives are individually mounted read-only. The first private-network
request failed because address pools were exhausted. No network retry or global
configuration occurred; the existing bridge was used and remains untouched.

Python3.12.3-1ubuntu0.17 was extracted from three official Ubuntu packages whose
SHA256 values came from signed repository indices. No package installation or
global configuration occurred. The producer helper projects use an explicitly
minimal build context; original Core source-local props/configuration remain in
the verified source closure. This is not a local full-Linux-application build.

The existing Android36 platform was copied read-only:11343files/106178939bytes,
with every copied SHA256 equal. Official Linux build-tools36.0.0 and stable
platform-tools37.0.1 were reviewed/downloaded into owned state. Their complete
licence text equals the owner's accepted standard SDK terms after only canonical
line-ending/outer-whitespace comparison. No sdkmanager or acceptance marker was
used. An initially selected preview37.0.3 with different terms was refused before
download. No adb command, phone command or host SDK mutation occurred.

## Preserved failures and final producer

The first Linux source build refused a missing real platform-tools directory.
After adding the reviewed actual package, aapt2 refused its missing execute bit.
Version2 restored execute bits only for verified Linux executables, without byte
changes; the next attempt exposed the original Gradle wrapper's missing execute
bit. Original executable-script modes are now recovered from the verified source
archive, with exact byte checks. Shared libraries are not made executable merely
because they are ELF. Successful restore was reused, not replayed.

The resulting actual Linux source build completed with zero errors/warnings.
PDB evidence binds672handwritten/706total documents to the original archive and
two approved patch results, with the pinned compiler profile and private version
stamps. Strict public API comparison completed with no diagnostics. The first
AAR gate then refused differences: only LF/CRLF in
`Microsoft.Maui.aar/.net/__res_name_case_map.txt` and
`maui.aar/AndroidManifest.xml` differ. Entry inventory and all other entry bytes
are exact. Version3 admits only those two text differences and packages the
complete official AAR bytes unchanged. It does not rewrite the built evidence.

Private version is `1.0.0-source10.0.120.3`; recipe SHA256:
`6700dd726ccfb347a54348c8396950fba899c341cd3a0e0a882446fdefd15039`.

| Producer | Core SHA256 | Package SHA256 |
| --- | --- | --- |
| Windows | `72526e4b0c66fea6721b10d509651bcd5183382cf05b101462bfd0652dc4be73` | `d5c233eaa3e46200c8056f756052ed5b1fae1861374b25c679f9379085c7980e` |
| Linux | `b886024af19aa10db7f260bc49aa4e1a42279e378c7021b5d6fdb8d5dacac148` | `dafcc64f55cf33cb6f15cd6a051458ee2086c78541b3a988167cf880e2223c35` |

Earlier version1/version2 packages, receipts and selection files remain intact.
Version3 reuses matching successful build/API phases; the first Windows resume
omitted its prior fallback arguments and refused before rerunning restore.
Restoring the exact prior arguments resumed successfully. Linux wrapper warm
verification passed without repeating its successful source build/API check.

Windows version2's5551method bodies/exception metadata/public user-string
mapping equal its qualified version1; version3 keeps the exact Windows DLL.
The Windows-versus-Linux raw-body comparison failed. A diagnostic reader also
encountered differing method identity at matching row indices and did not finish.
Neither failure is turned into an equivalence claim or a diagnosed code defect.
Cross-host PE, method ordering and execution equality remain unproved; each host
has its separate source/PDB/API proof.

## Normal complete APK

The normal wrapper first hit APT2264 at its long intermediate path. That failure
is retained; a fresh shorter owned intermediate root fixes the actual path limit
without a machine-policy change. The subsequent selection-order refusal is also
retained. The final normal wrapper completed, verifying the existing version3
package instead of rebuilding it. No custom Core target/root/restore config was
needed for product selection.

APK SHA256 `b74b54582e46c8e728b0605a7216671144f214dd2e116909c7e640dffd1a7e08`,
99663775bytes:384AOT/408ELF/1608static16KiBLOAD, v2/v3 signatures, ZIP CRC and
alignment pass. It is offline/non-debuggable with the original serviced Mono
hashes on both ABIs. Two actual app PDB source documents match the current
MainActivity/font-refresh source; all four retained Core/App AOT outputs match
their actual packaged bytes on both ABIs. This is build/package qualification; the unchanged D1186
native/font observations are retained rather than replayed. No new app launch,
authentication, photo, account or financial operation is performed.

## Retirement and limits

After all owned Linux commands ended, the exact label/ID/name-bound builder was
stopped and removed. All18previous running containers remain running. The bridge,
image and unrelated resources were not changed. Public source/tool archives,
producer outputs/proofs and signed package remain retained for reproducibility.
Evidence is in `artifacts/d1187-linux-private-core-admission` and the canonical
Windows producer in `artifacts/maui-core`.

Five standard Clean commands passed for the completed product intermediates,
the earlier failed long-path intermediates, both bootstrap controls and the
bounded comparison helper:26373files/1157790738logical bytes removed;
4008files/565171039bytes remain in those scopes. No supplementary deletion or
frozen-scope action occurred. Canonical producer witnesses remain necessary
for immutable warm verification and are retained; cleaning them would invalidate
the successful-phase evidence. Signed APK/PDB/AOT/proof/source evidence is retained.

Python and changed PowerShell syntax checks pass; eight report/guide local links
resolve. The YAML library is unavailable in this host, so no YAML parse pass is
claimed and no additional dependency was installed merely for that check.

This does not qualify a full hosted Linux app run, hardware16KiB, the broader
native dimension/typography boundary, whole accessibility/security/platforms,
original fault remedy or whole performance. No successful managed suite or
financial write is repeated. The full delivery goal remains active.

[The sole hosted CI response and failed-job diagnosis](hosted-core-failure-diagnosis-d1189.md)
subsequently record a Windows artifact-harness failure and an Android source-proof
failure whose inner reason was not emitted. Local source/API/package qualification
above is not recast as hosted CI acceptance; all failures and guard refusals remain.
