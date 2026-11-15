# Current release-input advisory scope (D1194)

## Current input and official feed

The post-D1193 actual Android restore assets contain191 exact package ID/version
pairs, equal across base/ARM64/x64 targets. The complete input, assets hash and
all comparison records were retained locally. The preceding D948 feed snapshot
was dated November9; this check uses newly advertised November14 metadata,
rather than replaying its old successful controls.

The official NuGet service index, vulnerability index and both advertised pages
were each retrieved once with bounded anonymous HTTPS and retained raw bodies,
hashes and request/response receipts. Base and update declare
`2026-11-14T05:29:55.8176483Z`; base contains664 package IDs/8014 records and update
is empty. All advertised pages are complete and additive, as required by the
[official protocol](https://learn.microsoft.com/en-us/nuget/api/vulnerability-info).

The existing SDK NuGet.Versioning7.9.0.0 parser performed58 exact range
comparisons over the191 input pairs: **zero affected-version matches**. Parser
SHA256 is `adda58969d521ba89cbd47c3c1de03198403b23d7ffde9c98edf41be197001ba`.
The four IDs with advertised ranges are Microsoft.Extensions.Caching.Memory,
Microsoft.Identity.Client, System.Security.Cryptography.Pkcs and
System.Security.Cryptography.Xml. No historical controls, product/native code,
financial operations, restore/build or device action ran for this comparison.

The [manifest-case correction](official-core-manifest-case-d1194.md) subsequently
changed only the private Core version4 identity to version5. A fresh actual app
restore confirmed all190 public pairs unchanged in all three target graphs;
the retained public advisory comparisons therefore remain applicable without
another retrieval or comparison run. The source-modified private package has no
independent official-feed coverage, and no clean private-package claim follows
from its absence in that feed.

## Runtime metadata and existing external review

One bounded retrieval of the [official .NET10 release metadata](https://github.com/dotnet/core/blob/main/release-notes/10.0/releases.json)
lists runtime10.0.15/SDK10.0.404 as latest, dated November10. These match the
current selected host inputs. The current release is marked security and lists
16 CVEs; this metadata comparison does not prove individual applicability or
execution of every fix in shipped native modules. No SDK/runtime download or
runtime configuration change was justified by this version comparison.

The owner-authorized existing external review was inspected read-only. Access
was available and a repository connection existed, but the target repository
was not returned by repository/workflow discovery. A repository-scoped history
search returned zero repository/commit scans with no next cursor. No target
repository ID or findings were available for current-code correlation. Unrelated
repositories' findings were not inspected. No scan, fix, finding mutation or
spending was initiated. This is an unavailable target-evidence boundary, not
proof that no vulnerability or earlier scan exists.

## Remaining scope

Zero feed matches are a finite exact-version result, not full security acceptance.
Private source, runtime/native dependencies, genuine providers and production
configuration still require their separate evidence. Original native-fault
remedy, whole performance/accessibility, iOS after Android, company/account and
release gates remain open. Existing canonical parent States are unchanged.
Phone/Main, successful authentication/financial checkpoints, consumed one-shot
classifications and frozen resources remain untouched; the delivery goal stays active.
