# Current optional-service and backup disclosure alignment (D-604)

## Actual remaining defect

The main specification resolution still says billing, quotas, sync and shared
services are not implemented. The maintained privacy matrices' leading DF-13/14/15
rows likewise say analysis, sync/households and online rates are absent, despite
their later D-292/D-294/D-298/D-389 evidence. The public inventory still describes
Android Auto Backup as enabled under superseded D-16. Its blanket no-developer-
service and cloud-only Store answers do not describe optional service editions.
These are current disclosure contradictions, not missing financial features or a
reason to repeat their successful engineering cohorts.

## Source-backed distinctions

| Boundary | Current source inspected | Correct claim |
|---|---|---|
| Personal sync | [TLS/session routes](../../Vafadar.Zanance.Api/Sync/SyncServiceRoutes.cs), [server blob protection](../../Vafadar.Zanance.Api/Sync/SyncBlobStore.cs) | Explicit owned HTTPS service processes selected records/attachments. Server Data Protection is not E2EE. |
| Household | [local activation and authorization](../../Vafadar.Zanance.Api/Sharing/HouseholdRoutes.cs), D-294 complete delivery | Application-layer E2EE protects financial payloads; membership, timing, sizes and audit metadata remain visible. Revocation cannot recall downloaded copies. |
| Aggregate analysis | [minimal payload](../../Vafadar.Zanance.Core/Analysis/AiProtocol.cs), [unconfigured provider](../../Vafadar.Zanance.Api/Analysis/AiProvider.cs), [registration](../../Vafadar.Zanance.Api/Analysis/AiModule.cs) | Local consent/request/credit prerequisites exist; production provider remains unconfigured. Only reviewed personal aggregates, not raw rows/names/notes/receipts, enter an explicitly consented request. |
| ECB references | [source selection and authenticated routes](../../Vafadar.Zanance.Api/Rates/MarketModule.cs), [bounded daily feed](../../Vafadar.Zanance.Api/Rates/EcbMarketProvider.cs) | Explicit approved ECB source is implemented; cached/history reads do not fetch implicitly. Public hosting and other providers are separate. Bank-file import is not a live bank connection. |
| Android editions | [project build conditions](../../Vafadar.Zanance.App/Vafadar.Zanance.App.csproj), current D-295 billing review | Cloud, personal-service and billing editions can have network access. Exact Offline Release is separate; old wrapper versions/artifacts are historical, not a new complete inventory. |
| OS backup | D-263 policy and [D-603 native evidence](android-live-backup-eligibility.md) | Android requests full exclusion and current framework reports ineligibility. Actual OEM/cloud/device-transfer payload acceptance and iOS integration remain open. |

Update the specification resolution, both maintained/starter flow rows and summaries,
the public technical inventory, SDK-review chronology and draft policy's personal/
household distinction together. Preserve historical dated evidence; label the old
2026-10-08 dependency/build table instead of silently changing its package versions.
Keep draft effective/contact fields, public routing, provider/legal/Store review and
genuine authentication open. Do not publish or submit anything.

## Verification and scope

Read current source and original delivery records, compare identical DF-13/14/15/16
rows in the two app matrices, verify the new relative source/evidence links and reject
the named obsolete current claims. Review the exact diff for limited text changes
and unchanged production inputs. These are document consistency checks, not new
managed tests, current runtime-traffic evidence or a rerun of old financial flows.

The initial summary-alignment helper expects a permissions row absent from the
starter template and refuses that file. The already completed maintained-matrix
edit is preserved. Apply only the actual starter summary/date and SDK heading
correction; do not insert a fictitious row or broaden another cohort to repair it.
The first column-count check also refuses an assumed seven-column layout. Both
original headers have six columns/seven separators; validate against those actual
unchanged headers rather than changing the documents to fit the observer's guess.

No product source, SDK, schema, APK, runtime, device, system setting, financial
operation, native test, external account or service activation changes. No app build
or unchanged managed suite is justified by these Markdown-only corrections. No
cleanup scope is retried. Local before/after findings and source fingerprints remain
under `%TEMP%\Zanance-D604-current.txt`. Full delivery remains active; further
TalkBack is last, Persian speech parked and all external/platform acceptance retained.
