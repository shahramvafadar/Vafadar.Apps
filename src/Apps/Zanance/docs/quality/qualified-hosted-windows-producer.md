# Qualified hosted Windows producer and original compatibility closure

## D-683 actual failures and corrections

The original e1cbb1be Windows native job124491318973 observes the new actual Microsoft-signed
installer18.11.12328.437, compiler19.51.36260.0 with SHA256
`b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57`, pinned
MSVC14.51.36231 and Windows SDK10.0.26100.0. Compiler bytes were already reviewed, but
installer metadata and native outputs must be qualified separately. No signature,
compiler hash, tools/SDK or immutable package-entry guard is weakened.

The first isolated producer run41186798703 actually receives older installer18.11.12315.225.
Its two successful fresh DLLs are retained as old-host evidence, not new-host acceptance.
Original schema execution then exposes a real source-closure compile failure: both independent
compatibility callers omit the original `BackupRestorePublication` helper required by their
actual protected receiver. Both now link that exact production source, preserving its checks.
Strict local Windows compile-only and complete Android compatibility builds pass with unchanged
reviewed native inputs. The first Android invocation omits its directory and fails packaging
after C# compilation; the corrected invocation supplies both exact reviewed slices. Missing
native inputs now refuse during PrepareForBuild, before compiling. No local schema/device
acceptance or repeated financial setup is inferred from those builds.

A producer-only request binds the actual installer/compiler to its stated target before C++
compilation. Seventeen public-metadata fixture cases preserve refusals for a wrong rolling
image, signer, compiler, SDK and candidate. Existing27 compiler cases pass after this change.
Fixtures are not actual hosted qualification. Failed producer artifacts retain completed fresh
DLLs and notices so a later failure cannot erase successful work. Android producer jobs stay
skipped for this isolated Windows purpose.

## Actual successful new host and immutable identity

[Run41190613268](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41190613268),
Windows job124506475352 at c465169fd6032ca74753da8c917a0fb15d05c954, completes successfully.
Both actual host receipts name18.11.12328.437 and the exact reviewed compiler above. Both
fresh native outputs have SHA256
`b02ca2545492b8584b2c15010ac24f6fc5a2b5e6e45a11c6c4ba825ccf277744`, matching candidate9.
The original Windows compatibility execution passes53 migrations, actual compiled model,
correct/wrong/missing keys, tamper rejection, FK, Unicode/money/date reopen, encrypted
portable restore and complete staging schema/typed-row comparisons. Fresh-process protected
performance and Android jobs are intentionally not executed in this producer-only lane.

The archived ZIP digest is
`d8889d8115d8e6046350e040e1189d14f31f92cd868864138e56193289f037c7`; its candidate10
export digest is `2726d8f274d4a4cb055a7b92867b677c15c74d3ab93ddfe78ba62a538a9d8a75`.
The ZIP digest, package digest/CRC and exact five entry names are verified locally without
executing native code. DLL, manifest, targets and notices are byte-identical to pinned
candidate9. Only the nuspec version differs: restoring its single candidate10 version field
to candidate9 yields exactly the existing pinned nuspec hash, with no other normalization.
The native package is not republished; candidate10 is not selected. Only18.11.12328.437 is
added to candidate9's hosted provenance list. Candidate9's version and all five entry hashes
remain unchanged. Twenty-nine impacted compiler/producer checks pass, including the new
host's positive selection and wrong-reviewed-compiler refusal.

## Delivery and limits

The isolated producer commits are integrated with their history after actual artifact review.
The ordinary complete current Main CI remains a separate one-shot delayed check. Producer-only
green does not establish all Main tests, current protected performance, phone adoption,
whole database/key recovery, stability, security, accessibility, iOS/provider/Store or release.
No successful Android job, financial operation or native build is rerun locally for this
binding. Original failed logs, old/new host receipts, source-closure proof and exact artifacts
remain retained. D-607's three deadlines, consumed reboot and unread post-reboot source are
untouched. The one producer-head workflow-runs GET is consumed; its automation is deleted.

## Ordinary Main native result observed on31October

The separate one-shot response for20ed349a records
[Native cipher candidates41192020270](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41192020270)
completed/success and CodeQL completed/success. Actual Windows job124510565217 completes
original schema/immutable entries, diagnostic refusals, production fresh-process reopening
and protected reference/tenfold measurements; Android job124510565342 completes its normal
official slices and stock cipher suite. Producer export is skipped in this ordinary lane.
These are current hosted gate results, not another local execution or phone Home-speed proof.
General CI41192020298 is still in progress at that sole response, so overall CI green is
not claimed. The exact-head marker is consumed and its monitor deleted without a second
workflow-runs request. D-685's later presentation work retains this completed native result.
