# Isolated Android runtime build - D1156

The owner explicitly accepts the official NDKr27c terms and authorizes local
download/isolated build, with phone/Main preserved and no closed classifier,
successful test or financial replay. The full goal is active. This supersedes
the pending prerequisite in [D1155](exact-runtime-build-inputs-d1155.md).

## Actual prerequisite and build

The781511249byte official Windows NDK archive matches its published SHA1
ac5f7762764b1f15341094e148ad4f847d050c38. Its recorded SHA256 is
`27e49f11e0cee5800983d8af8f4acd5bf09987aa6f790d4439dda9f3643d2494`.
ZIP CRC and extracted27.2.12479018 version pass. Extraction stays under the owned
D1155artifacts root; no global SDK, license file, PATH or system setting changes.

Six versioned attempts retain their distinct outcomes:

1. Traversal SDK resolution refuses inherited parent package-source mapping.
2. Private feed isolation exposes inherited central-package-management errors.
3. Process-local upstream package semantics restore successfully; the restricted
   process PATH lacks Windows PowerShell needed by the original build script.
4. The direct native target skips prerequisite targets and selects the default
   Visual Studio generator; configuration fails.
5. The proper BuildMono target selects Ninja and configures Android but cannot
   find the version source at CMake's original source-relative artifacts path.
6. Original source-relative output plus generated native version.c permits native
   compilation/install. Overall MSBuild then fails MSB3030 while copying absent
   llc.exe from the unbuilt AOT compiler output. No failure is relabelled success.

The private source NuGet.config clears inherited parent mapping while retaining
the exact upstream public feeds; its original bytes and adaptation hashes remain.
Central management is disabled only in this private invocation, matching the
upstream project rather than changing product package policy. No warning guard
or original financial/source safeguard is relaxed.

## Produced module, still unqualified

The sixth native install produces ELF machine62/x64,3302488bytes:
SHA256 `a44d465a2cdc664632f62663d64f39314f25d1d8147d270bd43018200301254e`,
GNU BuildId `bdb6b0fd9533256f1a272b192ece0796fc84845f`.
This is a private compiled artifact, not an official signed runtime or an accepted
package. CMake4.5/NDKr27c deprecation warnings remain explicit. Overall build exit1,
component/ABI/source/strict-policy admission and packaging still need resolution.
No runtime is installed or loaded, and no emulator/phone command is issued.

All versioned scripts/logs, process/terminal receipts and produced-module identity
remain under artifacts/d1156-isolated-runtime-build; actual build roots are owned
artifacts scopes. No cleanup is performed while this build question is incomplete.
Do not replay successful native work merely to obtain a green wrapper result.
The next action is the actual packaging/toolchain compatibility defect, with
original C7data/keys,25qualified/11remaining contexts and all frozen/terminal work
preserved. Native-fault remedy and broader delivery acceptance remain unproved.
