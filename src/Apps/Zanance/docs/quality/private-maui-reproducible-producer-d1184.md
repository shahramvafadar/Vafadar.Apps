# D1184 — Pinned private Core producer and consumer refusal

## Remaining problem and scope

[D1182](private-maui-density-configuration-d1182.md) qualifies the isolated live
configuration correction. [D1183](private-maui-product-asset-admission-d1183.md)
shows that an apparently successful product compile can silently select the
original Core. This step prepares a reproducible, explicitly private source
package and validates actual compiler/linker inputs before persistent adoption.
It does not change the default product graph or phone/Main.

## Official versus private source/build boundary

Both original Core PDBs contain706 documents and6406 method definitions. All672
handwritten Core documents in the unmodified private build bind to the exact LF
archive bytes. The official PDB binds those same source texts only after explicit
LF-to-CRLF conversion. This is text correlation, not raw-byte equality. The first
suffix-based path comparison incorrectly included MauiContextExtensions among
the two changed files; exact full-path membership corrects that observer and the
refusal remains retained.

The official compiler is5.0.0-2.26454.107 and runtime10.0.13-servicing.26454.107;
the approved current repository host uses compiler5.9.0-1.26529.119 and runtime
10.0.15. All other captured public PDB compiler options match, including language
14.0, defines, nullable, optimization, platform and unsafe setting. Two original
compiler-generated inline-array types and generated regex names differ. These
facts do not establish whole vendor/private method-body identity.

Upstream declares tools.dotnet10.0.113-servicing.26454.107 without a global SDK
version. Two exact public SDK archive probes return404; no general unavailability
or alternative SDK identity is inferred. The recipe deliberately pins the current
repository SDK instead of manufacturing the original build host.

The official ApiCompat10.0.404 package is MIT,7116372bytes, SHA256
`41cac2f03e95d1423a40332df037fdccfeb22710cb362744210a359f3e064724`.
The initial strict API command exits0 with an unresolved resource-designer
diagnostic and is refused as acceptance. A real Android resource-library build
supplies the missing SDK-generated designer; strict API equality with parameter
names then exits0 with an empty diagnostic log and no suppression. No reference
stub or framework execution is used. [Tool scope](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview)
and [portable PDB metadata](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md)
describe these public checks; neither proves complete runtime behavior.

## Actual Windows producer

The tracked `eng/maui` recipe verifies pinned inputs, extracts only the complete
Core/Graphics/Essentials build closure, applies the exact two-source patch and
builds with original source-local editor rules. A scoped official Android36.1.115
toolchain/JDK17.0.12 is used without changing installed SDKs or accepting terms.
All commands use owned state and an allowlisted child environment.

The first build fails because Windows symbol-store conversion assumes a Pdb2Pdb
path in the new package root despite a fallback cache. The corrected Android
build sets PublishWindowsPdb=false, retaining its actual portable PDB and source
proof; successful restore is not replayed. Final source build is strict with zero
warnings/errors. The Core proof binds672 handwritten/706 total documents, exact
compiler profile and private version10.0.120.1183. Both AAR entry-content maps equal
the original; the producer's strict API command has no diagnostics.

Private package `Vafadar.Maui.Core.Android1.0.0-source10.0.120.1`:

| Witness | SHA256 |
| --- | --- |
| Recipe | `5e993935eeade8c0052bb8bd19ea2027c2c5e3992565bf7c17f70757f80db1d3` |
| Package | `e6122fb974a53d5828633429272cf2ba847f5db46410aa3d3c6ae89b5348db0b` |
| Core | `f4d3deafa3e0423e2b3cba3848903a133497e2950cf9618e4bd11fee924691de` |

The new Core and D1183's accepted stamped private Core have all5551 method bodies,
stack/local/EH metadata and public user-string mappings equal. Assembly bytes are
different and this comparison is not vendor identity or execution qualification.
Original license/notices and a private-modification notice travel in the package.

The first wrapper invocation refuses an ambiguous Python command path; selecting
the first actual executable corrects that boundary. The subsequent wrapper uses
the validated package cache without repeating successful source/API work.

Linux AndroidSDK36.1.115 and JDK17.0.12 official hashes are admitted, but the Linux
producer has not executed. No Linux qualification or default CI activation is
claimed. Separate platform caches remain necessary.

## Actual consumer checks

Original product Android restore and strict Compile succeed with the private
package and compiler digest guard. A separate wrong-selection root supplies a
zero expected hash; ResolveReferences and the guard fail with the exact prepared
source-proof mismatch before compilation. Existing successful project builds are
reused with BuildProjectReferences=false, not repeated to produce this refusal.

A separate guard harness supplies the actual private DLL with an intentionally
wrong expected hash. The trimmer guard refuses that exact digest mismatch. This
is a synthetic build-guard contract, not an application test or another product
compilation.

The first full publication stops before AOT: the existing serviced compiler/task
packages are in a fallback cache, while the repository AOT target requires them
in the primary package root. The three existing10.0.15 packages are materialized
into the owned primary cache, with all42files byte-equal. No toolchain version or
target guard is changed, and no restore or successful test is replayed. Only the
failed publication resumes; it completes without warnings/errors.

The complete offline signed APK is99659679bytes, SHA256
`20bfeac8a714e5526a195a4feeebf9b9404cfbedb23bb024c40eb5c9b80872f1`:
384AOT/408ELF/1608staticLOAD, v2/v3 signatures, ZIP CRC and static16KiB alignment
pass. Both Mono library hashes remain the original serviced10.0.15 values.
Both linked Core images retain private file version10.0.120.1183 and equal SHA256
`0ed288a7c8677db83893c77ce8a380141a90cb77158257302ee32d69ef165432`.
Each actual Core AOT output equals its corresponding APK entry. This binds the
selected private assembly through linking/AOT into this package, not the official
vendor binary or all runtime behavior.

## Bounded original product configuration flow

A fresh owned API36/x64/4096 emulator runs only this complete product APK.
Premature transport/boot observations refuse without another emulator launch;
the original20800/94924 processes continue. Startup reaches the original Welcome
in PID4326/UID10216. Two normal Next commands reach original setup step3. The
first window observer uses an Android subcommand without mCurrentFocus and
refuses before input; reading the actual full window summary corrects it. A
console encoding refusal also precedes input and is corrected in the observer.

One fictitious account-name draft, `D1184 retained name`, is entered without
Start/Save. Original density420 changes once to160 and is reset to the original
absent-override state. The same PID, original step3 and exact raw name remain at
both transitions. The native name field, footer and step indicator bounds stay
inside1080x1920. At density160 the bounded page spans x180..900 and the name field
x277..803; this is an actual product layout observation, not only the SDK caller.

The name field native height remains116pixels in the two captured density
contexts. This does not qualify updated glyph/font metrics or every cached
measurement. No Activity-instance count, every field/layout, multi-display or
whole live-density acceptance is inferred. Broader existing-control measurement
qualification remains open rather than being hidden by the successful draft and
bounds checks. No account, financial row, authentication, camera, consumed
one-shot or successful suite is repeated or committed by this flow.

## Retained boundary

Inputs, failed attempts, public source correlation, compiler options, source/API
proofs, package, consumer logs and context remain under
`artifacts/d1184-maui-vendor-source-admission`. The used signed APK, two actual
Core AOT images and linked Core are retained with exact hashes before standard
cleanup. Source producer output remains required for integration and is retained.
The fresh UID is removed, eight remote XML/189436bytes are hash-verified and
removed, and original OS/locale/key-presence/geometry/density/boot/reverse are
exact. Official AVD deletion removes25files/4324668981logicalbytes, leaving only
six user-home files/7732bytes. Three standard Cleans remove15804files/
828701281logicalbytes;2208files/343434852bytes remain in the recorded build roots.
No supplementary/frozen cleanup occurs. C7/Q02, phone/Main and all protected
cohorts remain untouched.

Persistent default integration, broader existing-control density qualification
and Linux producer execution remain open. Original native-fault cause/repair, whole
performance, security, accessibility, platform, provider and release acceptance
remain separate. The overall delivery goal stays active.

The sole D1183 CI response records CI and CodeQL completed-success. No job log,
rerun or second request is made. D1182's already consumed in-progress response
remains historically unchanged.
