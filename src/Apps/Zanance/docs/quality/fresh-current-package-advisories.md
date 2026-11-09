# Fresh known-advisory matching for the current offline graph - D-948

## Actual graph admission

D920 retains the offline resolved package graph and original audit configuration,
but explicitly leaves fresh advisory data unqualified. This step fills that finite
missing condition without another restore/build or successful test run.

The current original App project.assets.json is hash-bound:
7cd6c8ccd0bb795678c569d09f72a30cb5746253ddf4e1a958360bafd703ea5a,
last written9November2026 09:38:44.938608UTC. Actual net10.0-android/android-arm64 and
android-x64 package nodes contain exactly the same190lowercase ID/exact-version
pairs, including transitive packages. Project nodes are excluded. All tracked
props/targets/csproj/config/global.json inputs have no change since the prior audit
anchorf91b330a954701760a547caab7f58e4af1ab1a7b. The assets hash remains unchanged
after matching. No new restore is needed to duplicate that retained graph.

The inventory is resolved-package evidence. It does not establish DLL/whole-PE,
trimming, every build/runtime/workload dependency or every optional edition identity.
No private source configuration, NuGet credential, licence or App secret is exported.

## Fresh official data and exact version ranges

The [official vulnerability resource protocol](https://learn.microsoft.com/en-us/nuget/api/vulnerability-info)
provides an index and additive pages keyed by package ID with affected version
ranges. Only public HTTPS api.nuget.org endpoints are used: service index,
vulnerability index and all two advertised pages, four GETs total. All response
bodies/digests and decoded data remain locally retained. Initial successful index
responses are not fetched again after the gzip handling failure.

At9November10:53:22.327504UTC, the retrieved index advertises:

| Page | Declared update UTC | Package IDs | Advisory range records |
| --- | --- | ---: | ---: |
| base |2026-11-07T05:53:18.5017039Z |644 |7493 |
| update |2026-11-09T05:53:26.6388968Z |0 |0 |

All190resolved pairs are looked up. Four package IDs have58known range records:

| Package | Resolved version |
| --- | --- |
| Microsoft.Extensions.Caching.Memory |10.0.12 |
| Microsoft.Identity.Client |4.90.1 |
| System.Security.Cryptography.Pkcs |10.0.13 |
| System.Security.Cryptography.Xml |10.0.13 |

Actual SDK10.0.401 NuGet.Versioning7.9.0.0 evaluates all58ranges. Eight new private
controls cover positive/negative inclusive/exclusive endpoints, numeric ordering,
prerelease and build-metadata semantics, preventing a lexical or always-false
comparison from masquerading as an audit. These are0new product tests. The SDK
assembly path and digest bind; no product code or native crypto is executed.

Result at10:54:53.2967841UTC: **zero affected version/range matches**. The result
applies equally to both identical RID inventories. No dependency update is justified
by this finite result. It is a lookup against the actually retrieved known data,
with its original declared dates, not absence of undisclosed/new vulnerabilities.
An ID absent from the feed is not proof of complete security coverage.

## Failures and artifact preservation

A Python token-spacing SyntaxError is caught before any HTTP request. The corrected
unexecuted reader receives the vulnerability index as gzip and initially attempts
UTF8 directly; its original compressed body is preserved, and the first two response
headers were not archived. Those unavailable headers are not invented. The remaining
reader decodes the saved bodies under a16MiB limit and requests only the two missing
pages. Their actual headers/raw+decoded digests are retained; no successful GET is
replayed. Initial public service/index file-write times are labelled as such.

The empty PowerShell pipeline produces no matches file; an initial normalization
incorrectly expects a zero-byte file. A correct JSON empty array is then derived
only from the retained58comparison rows. The original absence and both normalization
states are recorded, with no range/control/network replay or false positive claim.
All advertised pages complete before any zero-match result is accepted.

## Remaining delivery gates

Only small new local metadata/receipts and these documentation changes are produced.
There is no package/model/migration/workflow/app change, restore, product build,
old test, crypto/financial/authentication/device action or output requiring Clean.
Other Billing/personal-sync/provider editions, native SQLCipher/OpenSSL source CVEs,
Mono/runtime/workload security, source-level review, private Main DB/key, exploit
reachability and independent platform/security acceptance remain separate.

D9447552/UID10439/data and its unresolved original navigation remain held while
the owner is away. D945Main adoption is file-admitted only; retained5606/Q02,
terminal native records and all frozen scopes stay untouched. Original fault/ANR,
whole performance/accessibility, iOS/provider/Store/release and overall goal stay open.

D947soleCI response at2026-11-09T11:05:31.4891389+00:00 is1070.051seconds after push:
[CodeQL42603795104](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42603795104)
completed/success and [CI42603795271](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42603795271)
in_progress. Actual delay remains explicit; no second GET, final conclusion,
successful-job log request or rerun follows.
