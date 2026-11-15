# Official Core manifest case on the hosted Linux producer (D1194)

## Actual failure and correction

The one consumed D1193 status response reported CodeQL success and
[CI failure](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43686154145).
Only the failed Android job132726938685 log was retrieved. Its
`Prepare reviewed Android Core source package` step reached packaging after
source proof, then failed with `FileNotFoundError` for
`official-core/microsoft.maui.core.nuspec`. Windows and library/test jobs succeeded;
their logs and successful checks were not replayed. iOS was skipped. Product Android
and Billing steps did not run. This is not a failed product test or JDK warning.

The pinned official archive SHA256
`2f5d3e39af6e7e59653ff1c43df733e19864864e927ef5401f8a2757b4fc657f`
contains `Microsoft.Maui.Core.nuspec`. The producer now uses this exact path,
without renaming upstream files, searching for a substitute or relaxing admission.
The immutable private package version advances to `1.0.0-source10.0.120.5` in
the profile, central package declaration and consumer-selection target.
Earlier packages, receipts and selection files remain preserved.

## Changed-path qualification

The existing pinned SDK image ran offline with read-only root, no privileges,
ports, devices, credentials or image pull. Its owned workspace reused the
already qualified Linux producer. Only the official archive extraction was on
a fresh case-sensitive tmpfs; other Windows bind-mounted paths are not claimed
case-sensitive. The unchanged official ZIP was hash-checked and extracted with
its original entry names. The old lowercase lookup actually refused, the exact
manifest parsed, and the complete corrected production packaging phase passed.

Windows preparation also passed. All six successful phase-terminal hashes on
each host stayed exact: restore, source build, proof restore/build, source proof
and API comparison were reused under their existing command/witness guards.
No framework compilation or successful API check was rerun. The ordinary Linux
wrapper then verified the new receipt and wrote its version-specific selection.

| Evidence | Windows | Linux |
| --- | --- | --- |
| Recipe SHA256 | `17689e6562289b0cf6fec98d1df19ee7f4cb286df6bbe449c0e18d70e7cccf1a` | Same |
| Package SHA256 | `0c1da5147679be7fdba6ef1b377bebb8f7e49ec53198708cd4dbb462c147133d` | `f24dee19a14227bcf60fb29157bf549477dce270f0f14c135bba3a328401bbc4` |
| Core SHA256 | `72526e4b0c66fea6721b10d509651bcd5183382cf05b101462bfd0652dc4be73` | `b886024af19aa10db7f260bc49aa4e1a42279e378c7021b5d6fdb8d5dacac148` |

All four library entries (DLL/PDB/two AARs), dependency metadata and complete
source-proof object equal each host's version4 evidence. Cross-host PE equality
remains unproven; no source patch, framework behavior or runtime/GC changed.

A fresh actual application restore selected version5 in all three Android
target graphs; all190 public ID/version pairs remained exact. A normal MAUI
Android class-library build passed strict compilation with zero warnings/errors.
The production compiler guard selected exactly one private Core, with the Windows
digest above. No isolated application `Compile`, complete shipping rebuild,
device installation or product-success suite replay was needed for this metadata
correction. The last retained shipping application remains version4-bound; it
must not be relabeled as a newly built version5 application.

## Preserved failures, retirement and limits

The first container lacked the original read-only input mounts, exposing existing
zero-byte placeholders. Its hash precondition refused before extraction or source
phases. That container was retired and the required verified read-only mounts
were supplied to a fresh owned container. After packaging passed, the first
wrapper invocation failed before execution because PowerShell attempted to use
read-only `/root/.cache`. Scoped HOME/XDG directories under tmpfs corrected only
that launch environment; the successful packaging phase was not repeated.

Both owned containers were stopped and removed normally. Other running container
IDs stayed exact; no image, volume, network or unrelated resource was removed.
No phone/Main, protected cohort, consumed register classification, financial
operation or frozen scope was touched. The original native-fault cause/remedy,
whole performance/security/accessibility and external/platform/release gates
remain open. See [release-input scope](current-release-input-advisories-d1194.md).
