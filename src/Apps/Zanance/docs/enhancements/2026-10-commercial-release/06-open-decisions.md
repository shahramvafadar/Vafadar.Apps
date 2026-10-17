# 06 – Open decisions for the owner

Current owner authority and remaining external gates. The owner approved the consolidated questionnaire's
recommendations on 2026-10-15 with the exceptions recorded below. Earlier proposals are historical, not conflicting
open decisions. Settled choices are never asked again. Approval is distinct from implementation or acceptance.

| Id | Current decision / remaining gate | Approved direction | Applies to |
|---|---|---|---|
| OD-01 | Approved, expandable by country | Germany, current tax year, employees/freelancers; accountant export first, expert-reviewed calculations later, no filing. Other target countries are being added individually without an ETA or a support claim | ZCR-TAX-01..03 |
| OD-02 | Current-machine container hosting approved | Deploy isolated project/dependencies on existing Docker Desktop and use vafadar.pro. DNS/TLS/reachability/restore must be verified before exposure; no paid hosting authorized. A separate app domain may follow later | ZCR-ID-01 |
| OD-05 | Approved | 30 days read-only/export, then 90 days retention; deletion after 120 total days with clear prior notice | ZCR-SHR-03 |
| OD-06 | Approved | 14-day store Pro trial with explicit auto-renewal disclosure and store eligibility | ZCR-BIL-04 |
| OD-07 | Approved | Verified Plus Lifetime owners: 25% off the first Pro annual year, standard renewal, no stacked offers | ZCR-BIL-04 |
| OD-08 | Approved with future web channel | Pro follows Zanance identity across Android/iOS; store Plus/Lifetime remain per store. Future web purchases follow Zanance identity regardless of download store; implement prerequisites now, checkout/provider later | ZCR-BIL-01 |
| OD-09 | Approved E2EE from the first shared delivery | Household data requires end-to-end encryption from the start, including membership/key/recovery lifecycle. The old transport/storage-only first-release proposal is superseded | ZCR-SHR-01..04 |
| OD-11 | Owner reports recent MacBook Air and required software installed | Verify exact hardware/macOS/Xcode/workloads in the Mac environment when reached. Build/test there and use Apple Developer for publication when ready. Account/signing/OAuth/physical-device readiness is not inferred | ZCR-GOV-02 |
| OD-13 | Local framework approved; live provider/costs remain gated | Consent, minimal data, provider boundary and credit accounting now; select provider, credit package prices/units after actual cost review | ZCR-AI-01 |
| OD-14 | File research and provider-neutral prerequisites approved | Research Germany's widely used banks first, then USA and other target markets; implement file import/review before a separately accepted live bank connection. Rates: provider/cache/manual fallback now, actual provider/licence/cost later | ZCR-BANK-01, ZCR-FX-01, ZCR-IMP-01 |
| OD-15 | Technical bounds approved; commercial allocation later | Configurable technical limits now; commercial storage shares after actual hosting costs, never unlimited | ZCR-SHR-01 |

## Consolidated owner approval on 2026-10-15 (D-292 in progress)

1. Complete local household implementation is approved after the current complete personal-sync batch.
2. E2EE is required for shared financial content from its first delivery, not postponed after release.
3. Shared subscription expiry follows the approved 30+90-day policy above; export stays available.
4. Technical configurable bounds proceed now; commercial allocations require the later cost model.
5. Android/iOS billing proceed. A commercial Windows edition remains a future product direction outside
   the current work list, without a scheduled phase/date. Existing Windows development builds/checks remain.
6. Separate purchase authority from app distribution. Future verified web grants belong to Zanance identity
   across download stores. Plan identity/verification/idempotency/revocation prerequisites now; do not add a
   checkout, payment provider, paid resource or unverified paid grant. Store-origin Plus/Lifetime policy stays.
7. Pro store trial: 14 days, with clear automatic-renewal and eligibility wording.
8. Lifetime discount: 25% on the first Pro annual year only, standard renewal, non-stackable.
9. Germany tax scope first; each other approved market is a separate versioned country/year expansion.
   A "being added" label has no ETA and never enables unsupported calculations or suggests current support.
   This is tax availability only; existing currency/display/holiday support is independent and stays available.
10. AI framework/consent/credit accounting locally; real provider/cost acceptance comes later.
11. Bank file import and provider evaluation first; no live bank connection follows from research.
12. Online-rate/price provider boundary, caching, provenance/staleness and manual fallback locally first.
13. Current machine/Docker Desktop is the initial service host under vafadar.pro. Isolate its containers,
    networks, ports, volumes and secrets from other projects. Paid hosting and a later dedicated domain
    remain separate. Public routing is a concrete deployment gate, not evidence supplied by domain ownership.
14. A recent MacBook Air with required software is owner-reported. Continue in that machine's actual repository
    environment when available; installation of a chat application alone is not a remote connection. Owner
    account entry remains personal; do not create accounts, enter secrets or infer signing/readiness.
15. Research widely used German banks and current original export formats independently, then USA and the
    remaining target markets. Do not guess a header schema from bank size or label an untested preset supported.
16. Finish and polish the existing six languages. Additional languages are deferred outside the current
    delivery scope; this supersedes wave 9's earlier minimum-twenty-new-languages requirement for this delivery.
17. Free necessary tools/dependencies may be downloaded from official sources after security/licence review.
    Global installation, system configuration, new accounts and spending still require separate approval.

E2EE approval concerns the shared-space delivery discussed in the questionnaire. The current personal-sync
protocol uses transport/service-storage protection and is not retroactively described as E2EE. Shared financial
data must not be shipped through that readable-server protocol under an E2EE label.

No recommendation outside these approved choices grants account access, reveals credentials, selects a paid
provider or declares shipping readiness. See [current-machine deployment](09-current-machine-hosting.md)
and [German bank research](10-bank-import-research.md). These decisions unblock implementation, not acceptance.

## Settled on 2026-10-10

OD-12: the owner approved Repeat in the new transaction form, opening a prefilled Plan with the entered amount,
account and selected date, without posting a transaction. This is an in-memory draft until explicit Save.

OD-10 interim proposal: explicitly declined. The owner will not use the app before all sections/phases are
complete and requests permanent planned behavior without temporary product measures (D-114). No interim OS
backup exclusion is authorized or scheduled. The final encrypted-data/key/recovery policy still needs a decision.

OD-03: approved. Paused goals/plans count; Free has one limits-budget definition, three quick templates and one
saved filter. Financial month start and basic forecast through the financial month end are Free. The owner also
confirmed the Section 2 capability/tool matrix. ENT-01 builds the final Core model and tests only (D-117).
No commercial restriction in test builds is approved, and no billing, server, store or release decision follows.

## Community native source direction settled on 2026-10-12 (D-180)

The owner selected SQLCipher Community Edition from current official source, no custom cryptography
or unnecessary fork, with dependency security/maintenance review and a repeatable build/package/
update/security process. Architecture/platform/maintenance compatibility comes before final
integration. This settles source/licence direction; OD-04 recovery and OD-10 final OS-backup/key
policy remain open. No paid native package, old third-party binary or interim app policy is chosen.

## Settled on 2026-10-13 (D-192)

OD-04: automatic local device-protected profile key, optional separate recovery key, and recovery
from Zanance portable backups. Password encryption of portable backups remains optional (D-62).
Do not require a new remembered secret or silently recreate a database after key loss.

OD-10: exclude databases and sensitive files from Android/iOS OS backup/device transfer as the
permanent policy when encryption integration is completed. Restore a new device from Zanance
backup into fresh device-protected storage. The declined interim policy (D-114) stays declined;
no temporary exclusion is introduced before final integration. Do not ask these decisions again.

## OD-02 costed proposal (2026-10-14, D-269)

[The decision-ready model](07-backend-cost-and-decision.md) separates local implementation at no provider cost, a single-node test pilot (~$66.18/month with reserve), and managed HA (~$456.18/month with reserve). Growth scenario: ~$897.49/month. USD estimates exclude taxes/exchange/labor; ceilings are planning envelopes, not vendor caps. The 2026-10-15 Standard PostgreSQL standby restriction is included. Provider/region/test budget are still owner decisions; no resource/account was created and no spending authorized. ZCR-ID-01 remains open until actual implementation and restore qualification.


## Owner approvals on 2026-10-14 (D-271)

OD-02: implement the complete local modular ASP.NET Core backend with PostgreSQL and private
storage, without hosting cost. This settles local implementation authority, not paid provider,
mail sender, deployment, purchases, sync/sharing policy or production release.

LOC-07/08: Germany and other Western European countries, the United States, Canada and Australia
are the approved markets. Iran belongs to a later separate variant with Iranian holidays.
Countries must remain extensible and independent of UI language. Exact holiday jurisdictions,
official sources and validity years are implementation research, not completed acceptance.

## Current unattended-backup and next-wave decisions (D-277)

LOC-09's old password-in-keystore proposal is not authority to violate D-62: never remember a
backup password. An owner choice is requested between device-encrypted automatic local copies
with separate optional-password portable exports, and explicitly accepted readable portable
automatic copies. The owner selected device-encrypted automatic local copies with separate portable
exports on 2026-10-15. Implement the complete schedule/status/retention/explicit-recovery flow without
storing a password or implying recovery on a new device from a device-only copy.

The owner approved complete local personal Pro sync on 2026-10-15, including durable changes and
deletions, explicit conflicts without guessed amounts, idempotency, safe profile restore and device
management. This is implementation authority for ZCR-SYNC-01..03 on the local backend; it does not
authorize paid hosting, household sharing or commercial activation. Do not ask this decision again.
Purchase ownership/trial/discount,
shared-space retention/storage/E2EE, tax scope and hosted/provider gates stay in the table above.
Current ENT-05 test-build/upgrade evidence does not activate any customer commercial limit.

## D-298 local AI authority boundary

OD-13 permits local provider-neutral personal aggregates, native exact disclosure, private original continuation,
service-owned purchased/gifted credit accounting and independent fictional-provider qualification. This is not
selection of a live model, unit package/pricing, purchased consumable, API credential, public activation or shared
financial-data disclosure. The default provider remains unconfigured. Manual readable-summary sharing is a
separate deliberate recipient handoff, without integrated answer guarantee or Zanance credit use. Completed
local evidence and remaining external decisions are recorded in15-ai-local-delivery.md; settled choices stand.
