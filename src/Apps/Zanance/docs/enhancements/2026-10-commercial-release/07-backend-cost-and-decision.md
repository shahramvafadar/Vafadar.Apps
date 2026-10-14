# OD-02 backend architecture and operating-cost proposal (D-269)

Status: decision-ready proposal, not approved hosting or spending. Prices observed 2026-10-14;
recheck the provider quote before provisioning. No account, service, email, deployment or purchase
was created. ZCR-ID-01 remains incomplete until implementation and its actual restore drill pass.

## Proposed implementation boundary

Build one modular ASP.NET Core service with PostgreSQL and a private S3-compatible object adapter.
Keep optional Pro identity separate from local profiles, cloud-backup provider login and app PIN.
Free and local Plus continue without a Zanance online account. No mobile application references
another application; shared domain contracts belong in libraries. Start with one service, not
independent identity, billing and sync deployments with separate infrastructure bills.

The proposed primary region is Frankfurt (`FRA`/`fra1`). The provider's
[regional matrix](https://docs.digitalocean.com/platform/regional-availability/) identifies it in
Germany. Reconfirm the exact App Platform, database edition and object-storage availability in the
owner's account before deployment; parsed public tables omit their graphical availability marks.
Region choice alone does not establish contractual data residency or legal acceptance.

For the first implementation, use local containers and fictitious users only. Provider selection
does not authorize creation of paid resources. A separate approval is required before provisioning,
uploading user data, sending mail or publishing services. Runtime secrets are entered by the owner.

## Current price inputs and the upcoming PostgreSQL change

The [App Platform catalog](https://www.digitalocean.com/pricing/app-platform) lists monthly shared
container prices of $10 for 1 GiB (fixed single instance), $25 for 2 GiB and $50 for 4 GiB.
Their per-instance outbound allowances are 100, 200 and 250 GiB respectively; additional API
outbound transfer is $0.02/GiB. These scenarios use explicit instance counts, not unlimited scaling.
Shared CPU counts are resource assumptions, not measured request-capacity promises.

The [database catalog](https://www.digitalocean.com/pricing/managed-databases) lists $15.15 for the
smallest Standard PostgreSQL configuration. The [database pricing guide](https://docs.digitalocean.com/products/databases/postgresql/details/pricing/)
gives Advanced's starting primary at $130/month, with a matching-rate standby and additional disk
at $0.115/GiB/month. The model uses that existing Advanced price, not an unverified future discount.

Crucially, the [announced plan change](https://docs.digitalocean.com/release-notes/upcoming/dbaas-plan-changes/)
restricts new Standard clusters for new accounts from 2026-10-15 and all accounts from 2026-11-30:
no standby/read replica and no plans above 4 GiB. Existing clusters are excepted, but this proposal
does not assume the owner has one. Do not quote the older inexpensive Standard standby topology
as a new production configuration. The pilot is single-node; the HA estimates use Advanced.

[Spaces pricing](https://docs.digitalocean.com/products/spaces/details/pricing/) supplies one
$5/month subscription, 250 GiB aggregate object storage and 1,024 GiB outbound. Extra storage is
$0.02/GiB/month and extra outbound $0.01/GiB. Primary and staging buckets in the same account
share that base subscription; it is not charged twice. Objects include attachments, retained
versions and backup copies. Object and API transfer allowances remain separate.

[Postmark pricing](https://postmarkapp.com/pricing) is an email cost reference: Basic at $15/month
for 10,000 messages, plus $1.80/1,000 extra. This is not email-provider approval. Processor,
retention and routing review is still required; messages must contain no financial content and
must not enable open/link tracking. Domain/sender setup and credentials remain owner actions.

## Three explicitly bounded monthly scenarios

All amounts are USD before taxes and currency conversion, for resources running the full month.
The [structured model](08-backend-cost-model.json) includes every input and intermediate amount.
The resource sizes below are planning assumptions that require real load/restore qualification.

| Component | Pilot / low use | Managed HA / usual footprint | Growth footprint |
|---|---:|---:|---:|
| API | 1 x 1 GiB: $10.00 | 2 x 2 GiB: $50.00 | 4 x 4 GiB: $200.00 |
| PostgreSQL | Standard single node: $15.15 | Advanced primary + standby: $260.00 | Same two-node base: $260.00 |
| Extra PostgreSQL storage | $0.00 | $0.00 | 200 GiB/node x 2: $46.00 |
| Object storage | 50 GiB: $5.00 | 500 GiB: $10.00 | 3,000 GiB: $60.00 |
| Object outbound | 100 GiB: $0.00 | 500 GiB: $0.00 | 3,000 GiB: $19.76 |
| API outbound | 50 GiB: $0.00 | 250 GiB: $0.00 | 1,500 GiB: $10.00 |
| Email reference | 100 messages: $15.00 | 10,000 messages: $15.00 | 50,000 messages: $87.00 |
| Separate staging API + database | $0.00 | $25.15 | $25.15 |
| Operations/logging allowance (assumed) | $10.00 | $20.00 | $40.00 |
| Subtotal | **$55.15** | **$380.15** | **$747.91** |
| 20% planning reserve | $11.03 | $76.03 | $149.58 |
| Estimated total | **$66.18** | **$456.18** | **$897.49** |
| Proposed planning ceiling | **$75** | **$500** | **$1,000** |

The pilot retains a paid email allowance rather than relying on a promotional/free tier. It is
suitable for a limited test environment, not an HA production promise. Separate staging uses
isolated database/bucket/credentials and fictitious data; it shares only the account's object
subscription allowance. Extra Advanced disk is conservatively costed per node; confirm the exact
invoice configuration. Do not multiply database-network transfer again: the provider excludes it
from its bandwidth allowance. Direct authorized object transfers avoid presumed API proxy copies;
actual deployment billing still needs measurement.

The operations amounts and reserve are engineering assumptions, not vendor products. Taxes,
exchange rates, labor, store commissions, AI/bank services, dedicated IPs and a second-region
service are excluded. A planning ceiling is not a vendor-enforced spending limit: alert thresholds
and bounded instances/quotas are necessary, and alert delivery does not stop billing automatically.
Use the forecast to seek approval before growth, rather than purchasing capacity against it now.

## Complete first backend delivery after the owner's decision

1. Implement optional identity, device sessions, expiring one-use verification/recovery challenges,
   revocation and abuse limits. Select the supported maintained authentication components after
   their own compatibility/security review; do not invent credential cryptography.
2. Keep identity and later entitlement ownership outside financial portable backups. Bound every
   request and token lifetime. Authorization must resolve the actual principal and scope; local
   profile identifiers alone never establish server ownership. Financial sync/sharing protocols
   and their product/retention/E2EE decisions remain their own backlog sections.
3. Provide the service's migrations, least-privilege database roles and private object port. Use
   transactional outbox/leases for background jobs when multiple instances run. Do not depend on
   process-local memory for idempotency or distributed limits. No public bucket or financial
   payload in logs/support/metrics; disable capture of request bodies, tokens and mail challenges.
4. Deliver repeatable local build/deployment configuration with no embedded secrets. Stage schema
   migrations separately from ordinary web-worker startup. Include health/readiness, graceful
   drain, bounded rollbacks, credential rotation and operator actions that do not read ledgers.
5. Perform real isolated identity/session/authorization/abuse and database tests, then restore a
   fictitious complete database/object set into a distinct destination and verify relationships,
   object hashes and access revocation. Propose test-environment RPO 24 hours / RTO 4 hours;
   accept those targets only after measuring the actual drill. HA is not a disaster-recovery test.
6. Before a hosted test, the owner approves provider/region/budget, processor agreements, sender
   and secrets setup. Validate the exact current product prices and reachability. Production
   activation, purchases, sync/sharing and any user-data upload remain separately gated.

## Owner decision needed (OD-02)

Approve the modular service/PostgreSQL/private-object design for local implementation and either
the proposed Frankfurt provider candidate or an alternative. State the monthly *test environment*
budget. The $75 pilot and $500 HA planning envelopes represent different availability choices;
choosing local development only incurs no provider bill. No proposal here approves production
spending or resolves OD-05/06/07/08/09/13/14/15.

## Validation and unchanged application baseline

The source pages and the dated plan change were inspected, each subtotal/overage/reserve was
recalculated, and the object subscription was counted once. Public region cells could not be
reliably extracted, so account-level availability remains explicit. No benchmark, capacity,
security, service deployment or legal acceptance is inferred from this cost calculation.
The application, 3,526-test result, native evidence and complete signed APK remain D-268;
unrelated app builds/tests/native suites were not repeated for this documentation-only proposal.
