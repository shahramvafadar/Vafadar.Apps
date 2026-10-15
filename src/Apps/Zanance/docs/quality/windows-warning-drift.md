# D-289 Windows native diagnostic drift control

The Windows Community build retains 704 OpenSSL diagnostic occurrences at 689 distinct
path/line/code/message keys across 224 warning-bearing source files. The new baseline records
those exact diagnostics, multiplicity, official Git blobs, source SHA256 values, source commits
and crypto-profile hash. `securitySafetyAcceptance` is explicitly false. This is a change detector,
not approval of existing warnings, a warning-free claim or a replacement for semantic review.

## Production boundaries

`Build-Windows.ps1` checks the completed candidate before returning it. `Pack-Windows.ps1`
rechecks producer evidence before creating a package directory. Added/changed/missing warnings,
unknown warning formats, source-byte changes and unsafe/redirected source paths fail closed.
Fresh builds require exact occurrence counts. Resumed builds retain all original logs, require
the exact same saved command for every archived log and require at least every baseline
occurrence; repeated known diagnostics are retained rather than silently discarded.

CI caches only the immutable Windows NuGet package, not incomplete candidate fragments without
source/log history. Existing package reuse still verifies every entry name/hash against its
reviewed immutable version. The native workflow builds two fresh candidates, runs actual
packaging refusal cases after original-schema qualification, and uploads only public proof JSON.
No source flags, crypto algorithm, dependency version, compiler acceptance, DLL or package entry
is changed. No warning suppression or source fork is introduced.

## Maintenance commands

```powershell
./eng/native/sqlcipher/Test-WindowsWarnings.ps1 -RunName windows-ci-b
./eng/native/sqlcipher/Test-WindowsWarningPolicy.ps1 -RunName windows-ci-b
./eng/native/sqlcipher/Export-WindowsWarningProposal.ps1 -RunName reviewed-new-source
```

After an approved upstream update, a failed gate preserves the new producer. The export command
requires a complete fresh log, matching source/profile/native manifest and already restored
locked official Git objects. It verifies each warning-bearing file against its official blob
without filters, then writes a proposed inventory into a new ignored artifact directory. It
never edits the tracked baseline, fetches another source or approves warning safety. Review
source/diagnostic differences and relevant upstream security evidence before deliberately
changing the tracked inventory and immutable package qualification. An empty warning inventory
requires separate review rather than an automatic waiver.

## Local verification and limits

- 26 focused cases pass: full current diagnostic/source coverage, new/changed/missing diagnostics,
  fresh/resumed multiplicity, blank compiler lines, foreign/traversal paths, invalid source
  bindings, source drift, safety-waiver rejection and exact resume recipes. Actual packaging
  refuses a new warning and changed source in isolated copies before any package directory.
- The real retained D-260 producer passes the full check. Export verifies all 224 actual source
  files against official Git objects and reproduces all 704 occurrences/689 keys.
- A package built through the gate retains all five immutable candidate.5 entry hashes. Native
  bytes and original producers remain unchanged; synthetic negative fixtures stay isolated.
- PowerShell sources parse. Python/Node YAML packages are unavailable locally; hosted workflow
  parsing/execution remains the authoritative next check. No new parser download was needed.
- Initial focused runs exposed blank-line parameter binding and an upstream `.inc` include:
  the parser now accepts blank log lines and official C/header/include paths. A later assertion
  also requires original multiplicity when joining resumed history.

Local evidence: `artifacts/native-sqlcipher/warning-policy-1219876f870f4a21a9667c908776c5eb/proof.json`,
`artifacts/native-sqlcipher/build/windows-storage-d260-ec/warning-review-aef3f14b52a143bd980bc6ba6e5e5325/proof.json`,
`artifacts/native-sqlcipher/warning-proposal-603fca1449884e1e88a315eae069d9e4/proposed-baseline.json`,
and `artifacts/native-sqlcipher/build/windows-storage-d260-ec/warning-gate-final-d289/package-proof.json`.

Hosted execution is pending at commit time. No unrelated application tests, app builds, UI,
APK or upstream crypto suites were repeated locally. Native warning safety review, iOS/device
acceptance and the full development objective remain open.