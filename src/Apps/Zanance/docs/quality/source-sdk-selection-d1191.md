# Source-directory SDK selection (D-1191)

## Actual defect and bounded correction

The same installed executable selected SDK10.0.404 from the repository directory
but SDK10.0.401 from the verified upstream Core source directory. The upstream
`global.json` has SDK search paths without a direct SDK version. Its separate
`tools.dotnet` declaration is not a CLI SDK pin. This is an actual reproducible
preparation defect; it is not established as the exclusive cause of the earlier
hosted source-proof refusal described in [D1189](hosted-core-failure-diagnosis-d1189.md).

Private packageversion4 pins the source CLI SDK to10.0.404, disables roll-forward
and prerelease selection, and explicitly names the selected installation root.
All other upstream JSON sections, including `tools` and `msbuild-sdks`, remain
unchanged. Original configuration bytes and both configuration digests are
retained. Drift in an existing prepared policy is refused without repairing it.
The executable installation must actually contain the pinned SDK before any
archive download/extraction/build; repository-directory resolution cannot borrow
another installation and satisfy that preflight. Actual source-directory SDK
selection is checked separately. Compiler/runtime/source/API guards are retained.

## Completed producer evidence

Windows and Linux select10.0.404 from the prepared source directory. Both prepare
version4 with recipe SHA256
`df4649bf7ac411dafd035ecfc8dea7130a647561cc28cbd96d160806e4aa3e60`.
Windows package SHA256 is
`96fe797fa16976629955807f3ebba2f96c8c16dd367a82ab1108fe560820f650`;
Linux package SHA256 is
`3a308b10061dea7a85baa29efb3956b1b0b9113b918ee13326e66741e5bdeea6`.

The Windows Core DLL retains its version3 SHA256
`72526E4B0C66FEA6721B10D509651BCD5183382CF05B101462BFD0652DC4BE73`;
Linux retains its own version3 SHA256
`B886024AF19AA10DB7F260BC49AA4E1A42279E378C7021B5D6FDB8D5DACAC148`.
Existing successful source-build and strict API phases were reused through their
original command/witness guards, not rerun. Both source proofs retain672
handwritten/706 total documents, the pinned compiler profile, strict API success
and complete original official packaged AAR bytes. Cross-host DLL/PE/body
equivalence is not claimed. Version1/2/3 package receipts and selections remain.

The actual installation lacking10.0.404 is refused before downloading or building.
A separate synthetic configuration control preserves all non-SDK JSON sections,
demonstrates source-directory selection of10.0.404, then changes that fixture's
configuration bytes and receives the strict no-repair refusal. This control
does not admit the deficient executable installation to production preparation.

Linux uses an already present pinned official SDK image, with no image pull,
network, ports, devices, credentials or general repository mount. Its writable
mount is the existing owned producer workspace. The new bounded container is
stopped and removed after the actual wrapper verifies version4 without repeating
successful build/API phases. All18 preexisting running containers remain present;
no other container, image or network is removed. Producer witnesses remain on disk.

## Consumer and scope

The central package and version-specific selection import move together to
`1.0.0-source10.0.120.4`. The framework assembly and packaged official AAR bytes
are unchanged on each host; this is SDK-selection and package-identity repair,
not another application runtime claim. The first isolated consumer command
incorrectly forced the Android target framework across plain library restore;
compile refused with NETSDK1005. That failure is retained, not called success.
The corrected graph restore removes that global override while keeping the MAUI
Android graph selection and the original strict private-Core digest guard.

Corrected graph restore and the actual application `Compile` target both finish
with exit0 and zero warning/error diagnostics. Both Android target asset graphs
select exactly version4; original Core compile/runtime assets remain excluded.
The existing compiler digest guard accepts the unchanged Windows Core SHA256.
The compiled app DLL/PDB and graph/producer receipts are retained before standard
consumer Clean. No full AOT/APK/native qualification is repeated solely for this
package identity change. Existing D1190 runtime evidence belongs to version3,
whose Core bytes equal version4 on Windows; it is not relabeled a version4 run.
Standard consumer Clean succeeds and removes11920files/302148845logical bytes;
174files/16809716bytes remain. Canonical producer witnesses and retained compiled
evidence are preserved. No supplementary or frozen-scope deletion is attempted.

The sole D1190 post-push response records CodeQL completed-success and CI still
in_progress (run43678923208). Its final outcome remains unknown; the request is
consumed and is not polled again. An initial local time-conversion refusal
occurred before marker creation or any GET; the subsequent single request and
exclusive marker/response/receipt are retained.

No phone/Main, protected stopped cohort, register classification, successful
financial operation, native font/minimum/padding matrix or unrelated successful
suite is repeated. This work does not establish the original native fault cause,
whole performance, accessibility/security/platform/provider or release acceptance.
The full delivery goal remains active.
