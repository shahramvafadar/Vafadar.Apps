# November servicing applicability - D-1070

[D1085 project/app servicing](project-servicing-adoption-d1085.md) applies the
qualified SDK404/runtime15 defaults and current no-reset Main delivery. This
supersedes the dated preparation-only state for those exact inputs; unrelated
global installations and broader security/provider acceptance remain separate.

## Dated conclusion and scope

The 11 November 2026 review completes the finite applicability comparison for the
new 10 November .NET security release: 25 new advisories plus the updated October
Identity advisory, 26 unique CVEs. It does not apply an operational patch or close
security acceptance. The [official release index](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)
reports runtime10.0.15/SDK10.0.404/security=true. The
[runtime announcement](https://github.com/dotnet/announcements/issues/498) and
[ASP.NET announcement](https://github.com/dotnet/announcements/issues/504) have
separate component/version/platform scopes; a blanket runtime update does not
repair generated Identity handlers or independently versioned packages.

Current selected SDK10.0.401 needs servicing for the bundled VSTest advisory,
even though global.json selects Microsoft.Testing.Platform. Installed Windows
NETCore/AspNetCore/WindowsDesktop10.0.11/12 are older than the Windows native-reader
fixed release10.0.15. These are outstanding toolchain servicing requirements,
not evidence of an exploited process or an Android fault cause. This review
performs no global SDK/workload/runtime installation and does not silently remove
older installations potentially required by unrelated projects.

No new affected application-code path is established by the finite source and
graph comparison below. Absence of a configured path is narrower than absence of
an affected framework component, and does not waive ordinary maintenance.

## Bound inputs and method

The baseline is main8888e00b9cf5977a13ddb30bf22f6ed8352f4626. Twenty-nine saved
primary-source HTTP200 receipts bind the release index, two announcement hubs and
26 advisory responses by URL/retrieval time/byte length/SHA256. The index receipt
is 1204142bytes/SHA256482b9d79e76b331ef4cd5bcab02af3188f874ce78fd7aae07cb6163650da87c0.
All26 advisory raw-byte digests match their original receipt and inventory;
classification uses the saved responses without another fetch.

Two existing assets graphs contain206App and35API library identities. Only
library identities/target names and whole-file digests are retained, not restore
configuration values. No restore is run and these existing graphs alone do not
prove every optional edition or future deployment. Ten explicit source/pin inputs
and150tracked API/OAuth C# files are hash-bound; the bounded registration/reader
search retains its line-level hits. It finds only the OAuth comment explaining
HttpListener avoidance and actual TcpListener declarations. A negative source
search is not a runtime reachability proof for framework internals.

Current API uses Kestrel TCP/TLS. SelfHostedService accepts only one known proxy's
X-Forwarded-For, not X-Forwarded-Prefix; no cookie/PathBase registration was found.
No SignalR/Redis hub, HttpSys, named-pipe listener, multipart/form binding,
HTTP-body logging or Blazor registration was found in the inspected API source.
The App/API graphs contain neither System.Formats.Cbor nor MVC scaffolding/Identity.UI
packages; API resolves Identity.EntityFrameworkCore10.0.13. The original custom
/identity/recover handler identifies a user using the submitted email but
AddChallenge serializes PrivateMail from that maintained user's stored Email.
This is source evidence for the actual mail-recipient boundary, not genuine mail,
provider delivery or a repeated successful identity operation.

Nine CVEs exclude this .NET10 product by their published version intervals;
eight have no corresponding configured path/dependency found in the inspected
inputs; two require retained stored-recipient source evidence; one SDK advisory
is an outstanding host requirement, one has a platform/tool-use boundary, and
five Windows native-reader advisories retain an outstanding servicing requirement.
The table records every CVE separately; these counts are not new product tests.

## Advisory-by-advisory result

| Advisory | Current boundary | Evidence |
| --- | --- | --- |
| [CVE-2026-105544](https://github.com/dotnet/announcements/issues/517) | sdk-servicing-open | Selected SDK 10.0.401 is in affected 10.0.400-403 interval; fixed SDK 10.0.404. MTP selection does not remove bundled VSTest. |
| [CVE-2026-109475](https://github.com/dotnet/runtime/security/advisories/GHSA-3g4v-f5gv-c7xf) | path-not-found | Managed HttpListener Linux/macOS path; current OAuth loopback uses TcpListener and API uses Kestrel. |
| [CVE-2026-109476](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-xhj5-c3g8-pfj8) | path-not-found | SelfHostedService forwards only X-Forwarded-For, not Prefix; no configured cookie/PathBase path found. |
| [CVE-2026-109478](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-r3qf-p3jm-8h4p) | version-excluded | .NET 8 and exact early .NET 9 previews; current product targets .NET 10. |
| [CVE-2026-109481](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-xw2x-23jm-j582) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-114146](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-67wp-ghc2-fw2g) | path-not-found | No SignalR hub/Redis scale-out registration or resolved standalone package in inspected API/App inputs. |
| [CVE-2026-114147](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-wr8v-9x5q-ggcm) | path-not-found | No SignalR hub/Redis scale-out registration or resolved standalone package in inspected API/App inputs. |
| [CVE-2026-114149](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-x33c-w48q-qrgc) | path-not-found | No multipart/form binding or reader in inspected API source; framework version remains in affected interval. |
| [CVE-2026-114150](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-6vjv-7wm8-p67r) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-114151](https://github.com/dotnet/runtime/security/advisories/GHSA-fpxj-5c99-g7vj) | windows-servicing-open | Installed Windows runtime 10.0.11/12 is below fixed 10.0.15; affected native PDB reader invocation differs per advisory. No untrusted-PDB execution or Android applicability inferred. |
| [CVE-2026-114152](https://github.com/dotnet/runtime/security/advisories/GHSA-xf35-v9jh-g65w) | windows-servicing-open | Installed Windows runtime 10.0.11/12 is below fixed 10.0.15; affected native PDB reader invocation differs per advisory. No untrusted-PDB execution or Android applicability inferred. |
| [CVE-2026-114154](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-3893-7fxc-qvff) | path-not-found | API configures Kestrel TCP/TLS; no HttpSys registration found. |
| [CVE-2026-114155](https://github.com/dotnet/announcements/issues/502) | sdk-platform-boundary | Current host Windows is outside stated Linux/macOS platform scope; pinned Linux build SDK 10.0.403 is old. No dotnet-watch invocation found; SDK refresh remains open. |
| [CVE-2026-114156](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-8w29-6cjc-h6w4) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-114157](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-7phj-vgxq-r67h) | path-not-found | API configures TCP/TLS; no named-pipe listener found. Framework component is not a standalone NuGet package. |
| [CVE-2026-114158](https://github.com/dotnet/runtime/security/advisories/GHSA-r8h3-p8p6-ggp7) | windows-servicing-open | Installed Windows runtime 10.0.11/12 is below fixed 10.0.15; affected native PDB reader invocation differs per advisory. No untrusted-PDB execution or Android applicability inferred. |
| [CVE-2026-114159](https://github.com/dotnet/runtime/security/advisories/GHSA-3cgx-w6gm-fqrx) | windows-servicing-open | Installed Windows runtime 10.0.11/12 is below fixed 10.0.15; affected native PDB reader invocation differs per advisory. No untrusted-PDB execution or Android applicability inferred. |
| [CVE-2026-118741](https://github.com/dotnet/announcements/issues/525) | stored-recipient-source | Custom recovery creates PrivateMail from user.Email; request email only identifies the user. No scaffolded UI/generator dependency found. Dated source inspection, not genuine mail acceptance. |
| [CVE-2026-118745](https://github.com/dotnet/runtime/security/advisories/GHSA-x8x8-p968-x646) | windows-servicing-open | Installed Windows runtime 10.0.11/12 is below fixed 10.0.15; affected native PDB reader invocation differs per advisory. No untrusted-PDB execution or Android applicability inferred. |
| [CVE-2026-118746](https://github.com/dotnet/runtime/security/advisories/GHSA-ff4h-7rhj-jrjf) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-118747](https://github.com/dotnet/runtime/security/advisories/GHSA-vmv3-wvjp-wqpj) | path-not-found | System.Formats.Cbor absent from both inspected resolved library graphs; runtime servicing alone would not update it. |
| [CVE-2026-118748](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-5h82-77p6-2wp3) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-118749](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-m28h-pj36-4364) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-118750](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-6527-9fp8-r4x6) | version-excluded | Standalone WebUtilities 2.3.0-2.3.13; no such resolved dependency. |
| [CVE-2026-120796](https://github.com/dotnet/runtime/security/advisories/GHSA-cpw3-h88j-9f6x) | version-excluded | Published affected ranges are .NET 11 prereleases, not this .NET 10 graph. |
| [CVE-2026-70175](https://github.com/dotnet/aspnetcore/security/advisories/GHSA-x3vp-85cm-q97r) | stored-recipient-source | Custom recovery creates PrivateMail from user.Email; request email only identifies the user. No scaffolded UI/generator dependency found. Dated source inspection, not genuine mail acceptance. |

## Concrete outstanding servicing work

1. Prepare the official fixed SDK10.0.404 in project-owned storage with its published
   checksum and verify actual selection/bundled tooling. A project-local preparation
   must not mutate global SDK/workloads or hide an unpatched global installation.
   Host Windows native-reader servicing remains separate from merely selecting MTP.
2. Refresh the currently pinned Linux SDK10.0.403 and ASP.NET10.0.14 container
   sources using official immutable10.0.404/10.0.15 digests, then qualify the actual
   new images in a fresh fictitious deployment. Source inspection of a Dockerfile
   is not evidence of which image an existing container runs. No existing
   container, public service, credentials, database or retained deployment was read
   or changed in this review; D556's exact-project servicing overlay is not reused.
3. Evaluate backend/self-contained host runtime selection independently of the
   Android Mono/AOT servicing set. The central VafadarSecurityRuntimeVersion10.0.14
   also binds Android's runtime/compiler/task through AndroidAotRuntime.targets;
   an edit there would be a real native toolchain change requiring matched packs,
   actual compiled-source/profile proof and newly affected platform qualification.
   This review supplies no basis for claiming10.0.15 repairs the original SGen fault
   or for changing GC, stale profiles, tokens or original fault artifacts.
4. Recheck the applicable graph/source boundaries only when a new dependency,
   feature, edition or deployment is introduced. Do not replay settled financial,
   authentication, enrollment, photo, catalog or successful test cohorts merely
   to attach this dated advisory assessment.

No runtime/package/source pin is changed here. The above requirements remain open;
they are not a claim that all installation maintenance is completed. The SDK
revision is a concrete next independent preparation task, not an owner-presence
or CI wait. Global installation still has its separate system-change boundary.

## Preservation and verification

The saved-source classifier checks26unique CVEs, matching receipts/raw digests,
150source-file identities and the two finite library graphs. Documentation local
links and whitespace checks pass; no product build or managed/native test is
needed or repeated for this documentation-only comparison. An initial guessed
Identity subdirectory was absent; the actual root IdentityRoutes.cs was located
and inspected. A long local advisory print was truncated; saved complete sources,
not that partial console view, supply the version-table checks. Both are reader
limitations, not product test failures or new vulnerability results.

D1069's sole consumed response reports
[CI in_progress/null](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43050431759)
and [CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43050431434).
No later CI conclusion is inferred and no second request or rerun follows.
The old unlock reply is reconciled with already completed
[Main adoption D1068](current-personal-creation-main-d1068.md); no duplicate phone
input or installation follows. Main20079/931c07bc is only that preceding dated
observation, not fresh liveness. [Ready-catalog qualification D1069](personal-sync-ready-catalog-large-text-d1069.md)
remains terminal without repeating its native matrix or consent.

All raw advisory/source inventory, bounded graph scan, classification and check
receipts remain in owned local evidence. There is no new build/large output,
cleanup/frozen-scope action, phone/emulator/ADB command, real authentication,
financial write, runtime/GC change or external service mutation. Protected
Brotli source, retained5606/Q02, original fault evidence, canonical parent States
and existing D607/D752/enrollment/financial checkpoints remain unchanged.
Broader security, actual provider/public activation, original native-fault
cause/remedy, whole-performance/accessibility, iOS/Store/release and the full
delivery goal remain open.

[Project-local serviced SDK D1071](local-serviced-sdk-d1071.md) completes the
checksum-bound SDK preparation and actual host probe; global installations,
container servicing and actual product/platform compatibility remain separate.


## Subsequent signed Android inputs

[D1080](mono15-signed-servicing-inputs-d1080.md) prepares five official MIT10.0.15 Mono/runtime/AOT archives with author/repository signature and bounded static admission. Android product/workload/runtime adoption remains unperformed and requires actual execution qualification; first-use side effects and their precise cleanup are explicit.
