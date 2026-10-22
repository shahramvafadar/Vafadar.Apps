# Source-built native cipher producers

Source, compiler, diagnostic, compatibility and immutable package checks are independent gates.
An approved compiler executable is insufficient to reuse an existing native package identity.
`Pack-Windows.ps1` refuses changed DLL or package-entry bytes under a pinned consumer version.
Keep baseline and serviced producers separate; never allow alternative bytes for one version.

## Hosted Windows drift observed on 2026-10-22 (D-478)

Native run39852707414 on commit8aeb8f02 qualifies the actual Microsoft-signed compiler
19.51.36260.0/SHA256b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57.
Installation18.11.12315.225 is listed in official windows-2025-vs2026 image20261021.278.1.
Two fresh outputs both have native SHA256
b02ca2545492b8584b2c15010ac24f6fc5a2b5e6e45a11c6c4ba825ccf277744.
Original schema/key/tamper/portable checks pass before the immutable consumer refuses those bytes:
candidate.8 remains exclusively45d430bcabce5daf3f8513fdc27ec25ff48590c064fe9ce55c7bbadd26ee6521.
The public evidence artifact12458666625 has ZIP SHA256
efabfe48fa8075b4f6404dcb993bfa9af4f421a2afabbfff3492e99db0c969af.
These observations establish host-output drift with the same reviewed compiler; they do not
identify an internal compiler, linker or SDK cause. Android native qualification already succeeds.

The existing native workflow supports producer-only branches named
`shahram/native-producer-<stable-source>-candidate.<number>`. That mode keeps actual compiler,
two-fresh-output, source, diagnostics and original-schema checks, exports an unpublished candidate
package, and skips unrelated Android/consumer workloads. It does not accept that package for Main.
For this drift, candidate.9 is prospective until the original producer result and exact archive
entries are inspected. Never infer qualification from branch creation, a local metadata fixture
or an elapsed wait; do not overwrite candidate.8 or weaken its guard to make the job pass.

Consumer adoption must preserve old identities, bind a new qualified host to its own immutable
version, and then pass ordinary main/PR package, production-assembly persistence and workload gates.
This engineering qualification does not accept the app, Android/iOS, private-key recovery or release.
