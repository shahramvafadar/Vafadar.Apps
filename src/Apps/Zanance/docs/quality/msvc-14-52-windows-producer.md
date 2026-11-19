# MSVC 14.52.37025 as a qualified Windows producer of the source-built cipher (D-1240)

## What changed on the hosted image

GitHub's hosted image `windows-2025-vs2026` moved to version 20261116.315. Its
[official inventory](https://github.com/actions/runner-images/blob/win25-vs2026/20261116.315/images/windows/Windows2025-VS2026-Readme.md)
names Visual Studio installation 18.12.12405.243. The job's own observation
(`Observe-WindowsToolchain.ps1`, raised as a notice) found these MSVC tool sets, each with a valid
Microsoft signature, and Windows SDK 10.0.26100.0:

| Tool set | `cl.exe` file version | `cl.exe` SHA-256 |
| --- | --- | --- |
| 14.29.30133 | 19.29.30159.0 | `8e4352b523f4ff37b429aea1d340094be1e17bbdf4f687247fc0be92d6eb9440` |
| 14.44.35207 | 19.44.35230.0 | `e87e78b3ef6580b75aebda843f8a22e874612d9f766344046551399f898eb226` |
| 14.52.37025 | 19.52.37029.0 | `87b094e4fb3c646247b8eb515b5daa4fd73f991fe7cc42e98c0c9e3d3805d6fb` |

The reviewed tool set 14.51.36231 is not on that image, so the step "Review Windows native toolchain"
of the job "Windows build" failed whenever a run received it. The image is rolled out gradually: during
this work runs still received the earlier installation 18.11.12328.437 as well.

The owner decided on 2026-11-19 to move to the newest tool set, 14.52.37025.

## The constraint: the shared input lock stays byte-identical

`eng/native/sqlcipher/inputs.lock.json` names `windowsCompiler.toolsVersion` 14.51.36231. The SHA-256
of that file (`770688af8a3774f695f8a2119574376e8c2b5b34bc6af4d777f62654b2d07de6`, of the CRLF working
copy) is bound into the Android native package that was qualified on a physical device
(`android.runtime.qualification.json`, `Pack-Android.ps1`, `Build-Android.ps1`). Changing one byte of the
lock would invalidate that package. The lock is unchanged; the same hash is recorded in the manifest of
the new Windows package.

## Design

* `windows.compiler.acceptance.json` has schema 3. Every accepted compiler names the tool set it belongs
  to (`toolsVersion`); there is no tool set for the whole policy any more.
* `Resolve-ReviewedWindowsToolchain` looks at the reviewed tool sets that are installed and qualifies
  the one whose `cl.exe` has exactly the reviewed bytes, the reviewed file version and the Microsoft
  signature for that tool set. No reviewed tool set installed, other bytes, another signer, an unknown
  installation, more than one qualifying tool set, an older or newer policy schema, a compiler without a
  tool set: all are refused before anything is executed.
* The lock's tool set must stay one of the reviewed ones (it is the history of the first producer), and
  the SDK of the policy must equal the lock's. The lock is no longer the source of the tool set that is
  used: `Build-Windows.ps1`, `Test-UpstreamWindows.ps1` and `Test-WindowsAtomicWidth.ps1` take tools and
  SDK from the qualified toolchain.
* Producers of the first tool set keep their original manifest record (the lock's `windowsCompiler`
  object) and list the two compilers of that tool set, so the manifests of candidate.7, .8 and .9 stay
  reproducible. A producer of another tool set records `{ toolsVersion, sdkVersion }` and lists only the
  compilers of its own tool set.
* Diagnostics belong to the compiler that raised them. `windows.warnings.baseline.json` remains the
  inventory of tool set 14.51.36231, unchanged; `windows.warnings.msvc-14.52.37025.baseline.json` is the
  inventory of the new one. The gate takes the inventory of the tool set named in the candidate's
  manifest and refuses a candidate whose tool set has none.
* A compiler that has no consumer package yet cannot select a variant. In the producer-only lane, and
  only there, `Select-HostedWindowsProducer.ps1 -ProspectiveCandidateVersion` admits exactly the host that
  `windows.producer.request.json` names; it selects and persists nothing, and `Pack-Windows.ps1` packs
  such a compiler only with the explicit candidate version of that request.
* Nobody could read job logs or artifacts during this work (no GitHub login on the development machine),
  so the producer lane raises its public evidence as notices of the job
  (`Publish-WindowsProducerEvidence.ps1`): hashes of the fresh outputs, the proposed diagnostic
  inventory (compressed, in parts), the package manifest and the hashes of the package entries, and the
  message of a failed step. No key, database or source byte is raised.
* A producer request names one installation, and an image is rolled out gradually. Producer
  qualification therefore makes five independent attempts, each on its own runner and behind every gate.

## What ran on GitHub

Branch `shahram/native-producer-4.19.0-candidate.11`, workflow "Native cipher candidates", producer-only.

| Commit | Job | Installation received | Result |
| --- | --- | --- | --- |
| 5842e4fa | 135540596053 | 18.11.12328.437 | refused before any native build (not the requested image) |
| 6b6ee158 | 135550645721 | 18.12.12405.243 | compiler qualified, two equal fresh outputs, schema passed; stopped at the diagnostic gate (no inventory recorded yet) |
| ff0fcabd | 135560368308 | 18.11.12328.437 | refused |
| c644c14a | 135562039478 | 18.11.12328.437 | refused |
| 70b3fde6 | 135563663430, 135563663734 | 18.11.12328.437 | refused |
| 70b3fde6 | 135563662849, 135563663445, 135563664010 | 18.12.12405.243 | success, every step |

A refusal on the earlier installation is the requested behaviour and qualifies nothing.

The three successful jobs are independent runners. Each of them:

* qualified installation 18.12.12405.243 with `cl.exe` 19.52.37029.0, SHA-256
  `87b094e4fb3c646247b8eb515b5daa4fd73f991fe7cc42e98c0c9e3d3805d6fb`, valid Microsoft signature, and
  passed the refusal fixtures of `Test-WindowsToolchain.ps1` and `Test-WindowsProducerRequest.ps1`;
* built two fresh `sqlcipher.dll` with the same SHA-256
  `2648334b7bf285e697571c486dbeb2d729d9ba57f7f6b8be99661643e841c87c` (4,103,680 bytes) – eight fresh
  builds in all, counting job 135550645721, with one result;
* passed the original schema qualification (`Test-Windows.ps1`: the actual compiled model, refusal of a wrong
  or missing key and of tampering, foreign keys, and reopening with the correct key);
* found the diagnostics equal to the recorded inventory, passed the diagnostic refusal cases
  (`Test-WindowsWarningPolicy.ps1`) and exported the same unpublished package
  `Vafadar.Native.SqlCipher.Windows.4.19.0-candidate.11.nupkg`, SHA-256
  `0de4f749c75418726d0bb67ce5739bfa5337a28627589ab96ebbc73a2b0d17f2`, 1,718,850 bytes.

Package entries (SHA-256), equal in all three jobs:

| Entry | SHA-256 | Compared with candidate.9 |
| --- | --- | --- |
| `Vafadar.Native.SqlCipher.Windows.nuspec` | `647de2e73a6847bbf8e75edea40ee6086dea2a47a0ced4a589f417db50c48ee9` | new (version) |
| `buildTransitive/Vafadar.Native.SqlCipher.Windows.targets` | `ae6bf2c63879b3742b664d7e591111a66e30d3f8860024db548095210298eb12` | equal |
| `licenses/ThirdPartyNotices.txt` | `bc6ff61372c8c7a98b0fce76dc7375026e5fd7076155323d8632534d4f62b22b` | equal |
| `manifest.json` | `e1f4b7b509ddf3c44992cd233be3a8907268274b0fbfeeed23b754c9122b2008` | new (tool set, compiler, library) |
| `runtimes/win-x64/native/sqlcipher.dll` | `2648334b7bf285e697571c486dbeb2d729d9ba57f7f6b8be99661643e841c87c` | new |

The package manifest was read from the notice: sources OpenSSL 45e844fa and SQLCipher c4b275a4 with the
locked tag objects, `inputLockSha256` 770688af…7de6, crypto profile 48709be0…4980, the unchanged Configure,
SQLite and linker options, native dependencies `kernel32.dll`, `advapi32.dll`, `user32.dll`, compiler
`{ toolsVersion 14.52.37025, sdkVersion 10.0.26100.0 }` and the one reviewed executable 87b094e4…d6fb.

## Diagnostics of the new compiler

Job 135550645721 raised the proposed inventory in eight notices. Reassembled, its SHA-256 is
`e7620eb221340da41f116fa1d775146b5b35379daaddc29eb012386ff45d210c` (149,354 bytes), the value the job
announced. Compared with the inventory of tool set 14.51.36231:

* 508 diagnostics at 503 sites in 160 official OpenSSL sources in both;
* by code C4133: 2, C4244: 65, C4267: 439, C4319: 2 in both;
* no site added, none removed, no multiplicity changed, no message changed;
* every warning-bearing source has the same Git blob and SHA-256.

So the new compiler raised no new class of diagnostics and no new site, in cryptographic code or
elsewhere, and nothing had to be decided by the owner. The inventory is recorded with its provenance. As
before, an inventory records drift only: it accepts the safety of no warning (`securitySafetyAcceptance`
stays false), and the earlier per-expression reviews (D-221) are not repeated or extended by it.

## Consumer pin

* `windows.hosted20261116.package.contents.json`: version 4.19.0-candidate.11, the five entry hashes
  above, compiler 87b094e4…d6fb, `hostedInstallationVersions` 18.12.12405.243.
* `Resolve-ReviewedWindowsProducer` selects `hosted20261116` for exactly that installation with that
  compiler, `hosted20261021` (candidate.9) for 18.11.12315.225 and 18.11.12328.437 as before, and
  baseline (candidate.7) and servicing (candidate.8) by compiler as before. The earlier compiler on the
  new installation, the new compiler on an earlier installation, and unknown hosts are refused.
* `Directory.Packages.props` maps the variant to the version; the cache key of `ci.yml` covers the new
  inventory files. Candidate.10 was never a consumer identity and is refused as a producer version too.
* An ordinary lane does not download the producer package: it builds from the official sources on its own
  runner and must reproduce every pinned entry.

## Ordinary lanes on main

Merge commit 49130231:

* "Windows build" (CI), job 135576183792, received installation 18.12.12405.243 with tool set 14.52.37025.
  It qualified the compiler, selected hosted20261116, built the package from the official sources, reproduced
  every pinned entry of candidate.11 ("Prepare reviewed Windows native dependency"), passed the portable
  snapshot budgets and built the Release solution. Every step succeeded.
* "Windows reproducibility and actual protected persistence" (Native cipher candidates), job 135576005580,
  received the earlier installation 18.11.12328.437 with tool set 14.51.36231. With the new policy schema it
  selected hosted20261021, built two equal fresh outputs, reproduced candidate.9, and passed the diagnostic
  refusals, fresh-process reopening and protected workloads. This shows that the earlier variant still works
  after the change; it does not show the ordinary native lane on the new installation.
* "Official Android slices and stock cipher suite", job 135576005079, succeeded with the unchanged lock.

Commit 2f8eaed5 (documentation only):

* "Windows reproducibility and actual protected persistence", job 135586343849, received installation
  18.12.12405.243 with tool set 14.52.37025 and completed the ordinary native lane: compiler qualification
  and refusal fixtures, two equal fresh outputs with the diagnostic gate inside each build, every pinned
  entry of candidate.11 reproduced, diagnostic refusals, fresh-process reopening of the production
  assemblies and the protected reference and tenfold workloads. The Android job 135586344156 succeeded.
  With this, both workflows have run the new variant on the new installation on commits of main.
* "Windows build" succeeded again on the later main commits defd7c34 (job 135588260980) and d5b96d91
  (job 135610736324); the installation those two jobs received was not read.
* Not observed: a complete CI workflow. "Libraries and tests" was cancelled on 49130231 and 2f8eaed5 by later
  pushes to main and was still running on defd7c34 and d5b96d91 when last checked. Those tests do not use
  the Windows native package.

## Limits

* The development machine has no Visual Studio C++ tools. Locally only the syntax of the scripts, the
  metadata fixtures (12 producer-identity and 19 producer-request cases) and the refusals of a made-up
  installation were run. Acceptance of the real compiler, the native builds, the schema qualification and
  the packaging ran on GitHub only.
* Logs and artifacts of the jobs were not read; the evidence above is the public check runs, their steps
  and their notices. The producer artifacts stay on GitHub for 14 days.
* A matching compiler is not a matching output: a later hosted installation with the same `cl.exe` needs
  its own producer run before it is bound to a package, as before (D-478, D-683).
* This qualifies a build tool and a package for the Windows development build. It is not acceptance of
  the app, of Android or iOS, of key recovery, or of a release. The Android package and its pins are
  unchanged.
