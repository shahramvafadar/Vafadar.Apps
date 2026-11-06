# Current offline package audit scope from existing builds (D-920)

## Reuse the actual resolved graph

This is an admission of already produced restore/build evidence, not a new scan.
The complete D916 shipping build and D918 actual ARM64 private build both retain
project.assets.json. Select only net10.0-android/android-arm64 package nodes, excluding
project nodes. All190 resolved lower-case package IDs and exact versions match, with
no shipping-only or private-only package. Exact original asset-file hashes and the
complete sorted190pair inventories remain locally retained.

The same current graph includes:

| Resolved package | Version |
| --- | --- |
| System.Security.Cryptography.Xml |10.0.13 |
| System.Security.Cryptography.Pkcs |10.0.13 |
| Microsoft.Data.Sqlite.Core |10.0.12 |
| Microsoft.EntityFrameworkCore.Sqlite.Core |10.0.12 |

These are resolved package versions, not inferred DLL/runtime/native binary identities.
The original [protected-storage advisory correction](android-protected-storage.md)
remains separately versioned and is not replayed or changed.

## What the existing audit evidence actually says

Both actual restore graphs record enableAudit=true, auditLevel=low, auditMode=all.
The original current ARM64 strict publish succeeds; its retained assets log array
is empty and its exact original build log contains no NU19xx audit code. Repository
ContinuousIntegrationBuild enables warnings-as-errors. No new restore, advisory
network request/download, package update, build, test, installation or financial
operation is performed to duplicate that result.

[NuGet's vulnerability resource](https://learn.microsoft.com/en-us/nuget/api/vulnerability-info)
describes known advisory data indexed by package ID and version range. Configured
scope and a clean existing build log do not establish the advisory database's cache
age, freshness, completeness or absence of undisclosed vulnerabilities. There is no
fresh all-clear claim from this work. The build's original date and configuration
remain attached to it; package warnings are not a source-level security review.

## Explicit exclusions

The graph is the current offline ARM64 edition only, not every Billing/personal-sync/
provider edition, iOS or Windows graph. Equal package tuples do not prove whole-PE/
assembly, compile-option, native dependency or runtime/workload pack identity. Custom
native packages and upstream native CVEs require their own exact source/binary review;
public advisory coverage is not assumed for them. Original Mono/runtime/fault servicing
and actual exploit reachability are outside this finite package-graph admission.

No private Main database/key, licence value, credential, environment secret,
NuGet-source credential, frozen artifact scope or unrelated output is accessed. The
existing assets JSON is parsed only for target package IDs/versions, audit properties
and diagnostic codes; source/credential configuration is not exported. Only small new
local JSON inventories are produced, so no build-output cleanup is required.

This closes the need to rerun the same resolved-graph/audit-configuration check for
these unchanged artifacts. It does not close independent security review, fresh
advisory monitoring, Android fault/performance/accessibility, other platforms,
providers/store or release acceptance. The overall delivery goal remains active.
