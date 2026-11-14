# VMR native build profile and exact runtime-only APK pair - D1160/D1161

[D1159](private-runtime-remaining-candidate-d1159.md) prepared a private APK,
but its generated runtime version was 10.0.13-ci. This continuation resolves
the actual pinned VMR native build inputs before any deployment. The earlier
module, debug file, generated version files and APK remain historical evidence;
that APK is not selected for a retained-data trial. No device command occurs.

## Finite VMR source and build-profile admission

The upstream runtime commit a3788b71495457354b64ca8040b6ded5ff6ca4f9 is mapped
by VMR 8e0f8ed2312954727f8919d7d1c2f78b483b4ee7. Its standalone version inputs
say patch13, while the actual VMR runtime inputs say patch15. The native Mono
subtree comparison is complete: its only baseline difference is mini/CMakeLists.txt,
adding a macOS bundle identifier. This does not claim whole-repository equality:
the broader recursive comparison remains incomplete, with unrelated managed and
CoreCLR trees pending. Those trees are not silently treated as equal.

Ninja dependency metadata lists781 source paths. Exact Git subtree/file identities
cover763 baseline paths, explicitly excepting the intended private Tarjan change;
the remaining17 generated files and host_runtime_contract.h are separate. The
host contract matches the pinned VMR whole file. This is finite native dependency
coverage, not a claim that generated files or all managed components equal stock.

Five actual immutable VMR build inputs are applied and their Git blob identities
verified: eng/Versions.props, eng/Version.Details.props, eng/native/functions.cmake,
eng/native/Info.plist.in and src/mono/mono/mini/CMakeLists.txt. Original inputs are
preserved. The version file now comes from the real VMR input, without an isolated
version-number override or official-release branding. The original two-line
generation-aware Tarjan candidate remains the only intended engine code change.

The local NDK r27c/CMake3.30.5 strict Android x64 build exits0 with zero warnings,
including ENABLE_WERROR and CMake developer/deprecation errors. Its version is
10.0.15-ci, a private build, not the official packaged runtime. Module SHA256:
`25f732a5d06a27de8997f3c06198faa2ecceaad940a22441106d310a643a52a6`,
BuildId `cc459a10eef45f8513fced3d0101ee5147c12f19`,3302488bytes.
All1322 public exports, five DT_NEEDED names and SONAME equal stock. These checks
do not establish every ABI/layout or runtime behavior.

Reusing the old13 complete function hash fails and that refusal is preserved.
The new debuglink CRC/hash and actual bounded disassembly instead reconstruct
all658bytes/172instructions of push_object. Its generation-zero branch still
returns before marker mutation. The earlier gc_bits read remains; no dynamic
generation, header, writer, race or original fault cause is inferred.

## Exact pair, retained receipts and no replay

The new private helper has a stock-baseline mode. It validates all25 qualified
cached receipts, reads the full original C7 schema/typed source twice and captures
current display preferences, without issuing a case or writing display settings.
Only that new baseline receipt can admit the candidate, with the same UID/source/
preferences and a different process. The candidate permits only incomplete24 and
unissued25through34. Qualified0through23 and35 cannot be requested again. No seed,
financial Save, original exit reader or consumed register interpretation is compiled.
These are prepared guards, not yet observed runtime outcomes.

Both complete APK builds exit0. Comparing their ZIP entries initially finds
additional AOT differences after the second publish; that comparison is retained
as failed runtime-only admission. The final candidate is therefore packaged from
the actual stock APK payload, replacing only lib/x86_64/libmonosgen-2.0.so and
regenerating its signatures through the existing SDK signing target. This step
does not compile or rerun tests. Comparing every unsigned payload entry now proves
that the library is the only content difference; only the three regenerated
META-INF signature entries are separately excluded from that payload comparison.
Actual independent signature verification binds the same retained public signer.

Stock APK SHA256:
`e11b09de217ce2c43fe8fceb461024c79b739fc2bc84d631604fab132194399d`.
Exact candidate APK SHA256:
`2b9b719ed0f27fd9ffe9c90ac247692827cfd189b4bbbf6f4cc798e75566d21b`.
Both pass ZIPCRC, v2/v3 signature, zipalignP16,192AOT/204ELF/804 static16KiB LOADs,
owned package ID, nondebuggable/offline/no Billing. Thirteen actual App/private
PDB document checksums and the current MAUI10.0.120 graph pass. These are package
and compiler bindings, not new product tests or runtime acceptance.

Standard Clean exits0 and removes13693files/656522285logical bytes. Signed packages,
PDB/assets, source, module/debug and failed comparisons remain under
artifacts/d1160-vmr-build-profile and artifacts/d1161-runtime-profile-pair.
No supplementary/frozen cleanup, emulator deployment, phone/Main command, actual
source-baseline observation, case replay, authentication or financial operation
occurs. C7data/keys and all25 qualified/11 remaining contexts are preserved.
Stock-baseline and candidate execution remain unperformed; crash remedy, full
performance/security/platform qualification and the full delivery goal remain open.

## Consumed prior CI checks

Each due prior head receives exactly one read-only Actions request, after an
exclusive marker; raw responses and receipts are retained. No job log, rerun or
successful test is repeated. Actual outcomes: f7ed816d CI/CodeQL success;
669b4033 and afff116e CI cancelled/CodeQL success; 2b3a1788 CI/CodeQL success.
These are prior-head workflow outcomes, not execution of this new private pair.
