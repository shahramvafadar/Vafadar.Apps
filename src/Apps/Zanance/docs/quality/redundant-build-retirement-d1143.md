# Owner-directed completed build retirement (D1143)

The owner requests another cleanup of unnecessary project-owned files before
continuing the retained Android work. This review targets only completed,
unfrozen generated outputs whose successful standard Clean and separately
retained used APK/compiler evidence are already recorded. No successful build,
test, financial action, device input or standard Clean is repeated.

## Actual removal

Two disjoint reviewed passes successfully remove19targets:10completed build
directories and9unused bundle/designer files, comprising10,120files and
1,893,498,559logical bytes. Every target is strictly inside the canonical
repository's artifacts directory. Exact file-size admission and reparse/parent
checks precede removal; each target receives only one deletion attempt. All19
targets are absent afterward, with zero failed targets.

The retired build roots are:

- D1139: `nb`.
- D1141: `native-build` and `build`.
- D1129 and D1133: each `nb`, `wb` and `ab`.
- D1131: `nb`.

The nine individual files are unused AAB/designer outputs from the two D1139
publish directories and the D1141 native publish directory. Actual used signed
APKs remain at their existing paths, with matching original hashes. Original
compiler PDBs, source snapshots, raw results and the dependency graphs needed
for provenance are retained outside removed build roots. The exact reviewed
inventories and per-target terminal receipts remain under
`artifacts/d1143-redundant-build-retirement`.

The first deletion admission measures1,373,257,728free D bytes; the final second
pass measures3,276,111,872free bytes. This observed volume difference includes
concurrent filesystem activity and is not an isolated physical-byte attribution
to the logical file total. The existing3,000,000,000byte native-build entry gate
is now satisfied at that dated observation. No Android build or emulator resume
occurs as part of this cleanup.

## Preserved resources and manual remainder

The retained C7 source/data/key/AVD, original fault evidence, used packages,
toolchains, unrelated untracked source and all previously frozen scopes remain
untouched. Nothing is created, moved or removed under E:\Zanance.ReviewBuilds.
There is no new failed deletion in these two passes.

The previously refused four build roots and32unused files in
[D1138](completed-reference-output-retirement-d1138.md) remain the existing
confirmed-redundant manual list. Their refusal is not retried, subsetted or
worked around. A copy of that complete absolute-path list is provided locally
with this cleanup's receipts, explicitly preserving its prior observation date
rather than claiming a new inspection of those frozen targets. Earlier broad
directory size alone does not establish that all remaining contents are disposable.

The cleanup does not qualify the20remaining footer contexts, actual memory
profile, original OOM/native-fault remedy or whole delivery. The prepared
[D1142 continuation](retained-footer-memory-continuation-preparation-d1142.md)
and every accepted-operation checkpoint retain their original scope.
