# 06 – Open decisions for the owner

Only questions the owner must answer; each with a proposal. Settled decisions (three plans, no Pro Lifetime, prices
as design values, languages last) are not asked again.

| Id | Question | Proposal | Needed before |
|---|---|---|---|
| OD-01 | Tax: first country, tax year, user type, scope level | Germany, the current tax year, employees and freelancers (income-surplus calculation); level 1 (accountant export) in Plus first, level 2 after review by a German tax expert; no filing (level 3) in the first scope | ZCR-TAX-01 |
| OD-02 | Hosting and backend for identity/sync/sharing; monthly budget | One modular ASP.NET Core service in an EU region with managed PostgreSQL and object storage; cost estimate for low/typical/high use before choosing a provider | ZCR-ID-01 |
| OD-05 | After a Pro owner's subscription ends: grace period, read-only, server retention | 30 days read-only for all members with export, then 90 days retention before deletion, announced before sale | ZCR-SHR-03 |
| OD-06 | Trial flow | Store introductory free trial of 14 days on Pro (auto-renews, store-enforced eligibility) | ZCR-BIL-04 |
| OD-07 | Lifetime owners' 25 % on Pro: duration and stores | First year of Pro yearly at 25 % off via a store offer for verified Lifetime owners; renewal at the standard price; not combinable | ZCR-BIL-04 |
| OD-08 | Purchases across devices and stores | A subscription belongs to its store account; Pro services are linked to the Zanance identity, so Pro bought on Android works on iOS through sign-in; Plus/Lifetime stay per store | ZCR-BIL-01 |
| OD-09 | End-to-end encryption for shared spaces | Transport + storage encryption in the first Pro release; E2EE as a separate decision after the first release, with no E2EE claims before | ZCR-SHR-04 |
| OD-11 | Current status of Mac/Xcode, Apple account and OAuth client ids | Owner confirms; documents updated accordingly | ZCR-GOV-02 |
| OD-13 | AI: credit packages, unit, minimum top-up, failed-request policy | Decide after a provider cost review; refund reserved credit on provider failure | ZCR-AI-01 |
| OD-14 | Bank connection and online rates: markets and providers | Evaluate after the first release; never part of Lifetime | ZCR-BANK-01, ZCR-FX-01 |
| OD-15 | Cloud storage per shared space | Set from the cost model (ZCR-ID-01); never "unlimited" | ZCR-SHR-01 |

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
