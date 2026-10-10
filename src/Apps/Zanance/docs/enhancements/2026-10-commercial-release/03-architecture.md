# 03 – Architecture for entitlements, billing, local security, sync and sharing

Status: **ENT-01 Core policy implemented (D-117); remaining integrations are design, not implemented.** It builds on the existing layers (`docs/architecture/overview.md`): Core without
infrastructure, Data with EF Core/SQLite, MAUI app and shared `Vafadar.*` libraries. Names below are concepts; the
concrete types are chosen when a section is built, inside the existing projects, without parallel copies.

## 1. Entitlements and quotas

```text
Store adapter (Play Billing / StoreKit / Windows channel)  ──►  Purchase verification (server or store API)
        │                                                              │
        ▼                                                              ▼
 EntitlementCache (secure, signed state, offline)  ◄──────────  Entitlement state (plan, purchase kind, scope, expiry)
        │
        ▼
 PlanPolicy  ──►  FeaturePermission (may create / may analyse)  +  Quota (active counts)
        │
        ▼  enforced in services and commands (create, import, restore, deep link, widget, OCR, API)
```

* **Concepts:** `Plan` (Free, Plus, Pro), `PurchaseKind` (Subscription, Lifetime), `BillingPeriod` (Month, Year),
  `Entitlement` (plan + kind + source + validity + terms version), `EntitlementScope` (personal profile vs shared
  space), `FeaturePermission`, `Quota`, `Offer`, `AddOn` (AI credit, Tax country/year, bank).
* **Separate decisions, separate code:** display (`FeaturePolicy`, Simple/Advanced), ownership/role (shared space),
  entitlement (purchase), quota (counts) and platform readiness (is the feature built on this platform). A payment
  never replaces a security check; a missing entitlement never hides data.
* **One policy, many entry points:** quotas and permissions are checked where the work is done (the store/service
  that creates an account, goal, plan, budget; importers; restore; deep links; the Android widget; OCR; any API), and
  the UI only explains. Shared-space operations are additionally authorised by the server on every request.
* **Trust:** a local flag, a backup file, a client-sent value or a "payment successful" page is never proof of
  purchase. Purchases are verified with the store (Google Play Developer API / App Store Server API) through our
  service, or — for Plus without an online account — through on-device store verification (StoreKit 2 signed
  transactions; Play purchase token verified when the service exists). Purchase data is stored apart from the
  financial database and never in backups.
* **Offline:** a verified state is cached securely (keystore/keychain/DPAPI-protected, signed). The validity window
  and grace period during outages are fixed in an ADR (proposal: re-check when online; subscriptions stay valid until
  their store expiry plus a 7-day outage grace; the device clock alone never extends or revokes). Lifetime, once
  verified, never stops working because our service is down; restoring it on a new install needs the store.
* **No online account for Free and local Plus.** An identity exists only for sync/sharing (Pro) and is optional.
* **Development:** sandbox and production products, secrets and test entitlements are separate; Release builds
  contain no backdoor. No real purchase button ships before the provider and the sold capability are ready.

### Lifecycle states (each with its own state and tests)

Purchase, renewal, restore, refund, revocation, pending payment, payment error, grace period, account hold, paused
(Play), auto-renew off, expired, recovered. **Turning off auto-renew is not expiry**: access lasts until the period
end. Store notifications (Play RTDN, App Store Server Notifications v2) can arrive twice or out of order: verify
signatures, deduplicate by notification/transaction id, re-query the store, apply idempotently; acknowledge (Play) or
finish (StoreKit) in the trusted path; consumables (AI credit) are granted exactly once.

### Store mapping (to confirm with the store consoles)

| | Google Play | Apple App Store | Windows |
|---|---|---|---|
| Plus, Pro subscriptions | one subscription product per plan with monthly and yearly base plans; upgrade/downgrade with replacement modes | one subscription group with Pro above Plus (levels), monthly and yearly in the group | Microsoft Store add-ons, only if the app is published there; otherwise free development target |
| Plus Lifetime | one-time product, non-consumable | non-consumable | durable add-on |
| Offers | offers on base plans (introductory/eligibility tags) | introductory offers, offer codes, win-back; offer codes for Lifetime owners only if the 25 % rule fits | – |
| AI credit | consumable | consumable (credits purchased do not expire, App Review 3.1.1) | consumable |

Cross-store transfer (Play ↔ App Store), OS family sharing and buying on one device for another are **not assumed**;
an identity-based link of a purchase to other devices is a Pro-service decision (OD-08).

## 2. Local data security

Current state (see [01-current-state.md](01-current-state.md) §2): the SQLite database is not encrypted; the app lock
uses the device credential/biometrics only; backups are AES-encrypted with a password when the user chooses one; the
safety copy before a restore is unencrypted.

Target, in small steps (wave 1):

1. Threat model and decision record: what is protected against whom (lost/stolen unlocked phone, device backup,
   malware with file access, shared computer), which files (database, `-wal`, `-shm`, attachments if outside the
   database, temporary exports, safety copies, receipt images in cache).
2. Library: an established SQLite encryption (e.g. SQLCipher via `SQLitePCLRaw.bundle_e_sqlcipher` or the
   commercially licensed SQLite Encryption Extension) – checked for .NET 10, MAUI on Android/iOS/Windows, AOT/trimming,
   licence and cost before adoption (owner approval for any paid licence). No home-made cryptography, no key in source.
3. Keys: a random data key; wrapped by the platform keystore (Android Keystore, iOS Keychain, Windows DPAPI/CNG); an
   optional app password with a standard KDF (Argon2id or PBKDF2-SHA256 with versioned parameters and salt) wraps a
   second copy. A short PIN without hardware protection is not treated as a strong key.
4. App password and biometrics: choose/change password, optional biometrics, invalidation after biometric changes,
   attempt limiting with growing delay, lock on return. Device credential, app password, backup password and online
   identity stay four separate things.
5. Forgotten password / lost device: decided before building (OD-04). Proposal: a one-time **recovery key** shown at
   set-up, plus the existing password-protected backup; no promise that "we can recover your password".
6. Migration of an existing plaintext database: stop access, check free space, write the encrypted copy, verify row
   counts and relations, switch atomically, recover on failure; the old plaintext file is deleted, without claiming
   secure erasure on flash storage.
7. Backups stay portable: they are encrypted with the backup password, never with a device-bound key; safety copies
   get encrypted too.
8. When billing or sync add network use, every SDK and any telemetry is reviewed again; the offline build's "nothing
   is sent" claim is never copied to the online build; privacy texts and help texts are updated with the behaviour.

## 3. Personal sync (Pro)

* Not a copy of the database in a cloud folder and not "last file wins". A modular backend (one deployable service
  with clear modules) is preferred over microservices; hosting, database and cost need owner approval (OD-02).
* Identity and devices: register/revoke devices; every record has an owner; private data syncs only when the user
  turns sync on.
* Change log: durable outbox, globally unique ids (already GUIDs), record versions, soft deletes (tombstones),
  retries, dependency order (account before entry, entry before split/attachment), conflict detection per field;
  two devices changing the amount of one entry are shown as a conflict, never silently overwritten.
* Idempotency: recurring auto-post and settling the same occurrence on two devices, repeated requests, loan
  instalments – the existing unique settlement index is extended to a server-side uniqueness rule, so nothing is
  posted twice. Splits, refunds, goal allocations and attachments move with their relations.
* Clocks, time zones, three calendars, long offline periods: financial dates are `DateOnly` and never derived from
  sync timestamps.
* Restoring an old backup into a synced profile is a separate, confirmed flow; deleted items and old history are not
  pushed to other devices automatically.
* Status: last sync, outgoing queue, errors, conflicts in plain words; an outage never blocks local recording.

## 4. Shared space and selective sharing (Pro owner)

* An owner shares chosen accounts into one space (up to 6 identities incl. owner). Joining never publishes the
  member's other accounts or history.
* Roles Owner, Editor, Viewer; permissions read, edit, delete, export, invite, transfer ownership checked by the
  server **for every request and every resource** (OWASP authorization), not by the UI.
* Invitations: explicit acceptance, expiry, single use; leaving, revoking invitations and members, transferring
  ownership, deleting the space. Knowing an e-mail address or a record id grants nothing.
* Reports, search, goals, attachments, notifications and export never reveal accounts outside the member's
  permission. A transfer between a private and a shared account shows a neutral counterpart ("private account"),
  by an explicit display contract.
* Splitting a cost between people (who paid, who owes; F2-SHARE-05) is separate from category splits; household totals
  never count a shared balance once per member.
* Audit trail of sensitive changes (who changed what), itself scoped.
* Revocation stops future sync; offline edits of a revoked member are rejected; no promise of remote deletion of
  copies or exports already on a device.
* Transport and storage are encrypted. End-to-end encryption (key sharing/rotation, invitations, revocation,
  recovery) is a separate architecture decision (OD-09); **until it is fully built, "E2EE" or "zero-knowledge" is
  never claimed.** With E2EE the server cannot validate amounts, detect duplicates or run AI on content – those move
  to clients, by design.
* End of the owner's subscription, security revocation of a member and deletion of data are three different events,
  each with its own retention, export and recovery rule.

## 5. Operations before Pro is sold

Deployment and migrations of the service, backups and restore drills, secret management, rate limiting, capacity,
observability without amounts, notes, passwords or receipts in logs, alerting, incident response; documented data sent
to the service and its retention; support tooling without default access to anyone's ledger.

## 6. Service economics

Before Pro is sold, estimate per month for low/typical/high use and a full six-person space: identity, invitation
e-mails, sync storage and traffic, attachments, server backups, operations and support. Compare with the net revenue
of €39.99/€4.99 and of the €29.99 campaign after store fees (15–30 %), VAT and refunds. Cloud storage is never sold as
"unlimited" without a cost model; "unlimited" local items are not unlimited server storage. AI, Tax and bank prices
are set only after their own cost review and owner approval.

## 7. AI, Tax, bank and online rates (add-ons)

* **AI credit:** exchangeable provider behind an interface; cost per request; consent per use; minimal data (never
  the whole database by default; shared-space data only with its own permission); append-only credit ledger with a
  unique request id; reserve → settle → refund; timeouts and retries never charge twice. Purchased and gifted credit
  are kept apart; purchased credit does not expire on Apple. Users never enter API keys. AI explains and suggests;
  any change to data needs a preview and a separate confirmation; receipt text and model output are data, never
  instructions.
* **Tax:** three levels kept apart – (1) preparing data and documents for an accountant (basic export, part of Plus),
  (2) official calculation and reports, (3) official filing. Levels 2–3 need a versioned rule set per country and
  year, rounding and period rules, VAT where needed, corrections, traceable output and tests from official examples,
  reviewed by an expert of that country. The personal ledger is not the tax engine (cash vs accrual, depreciation,
  private vs business, closed years, later corrections live in a tax layer). A compressed receipt photo is not
  presented as the legal original. Licensed per country/year; documents already prepared stay accessible after the
  add-on ends. First country/year/scope: OD-01 (proposal: Germany, current tax year, employees/freelancers with
  income-surplus calculation, level 1 first, level 2 after expert review).
* **Bank (read-only):** provider and countries, cost, licence (PSD2/AIS), consent and revocation, matching with
  manual/CSV entries, pending vs posted, keeping the user's categories. No payments.
* **Online rates and prices:** source and redistribution rights, timestamp, caching, offline fallback to manual rates;
  a fetched rate never rewrites history without consent; markets with several rates (Iran) keep manual choice. Paid
  live data is never part of Lifetime.

## 7. Implemented Core policy boundary (D-117 / ENT-01)

Core/Commerce contains ProductPlan, purchase/cadence/source/validity facts, exact personal/shared capability contexts,
commercial operations/permission reasons, scoped quotas and immutable counting projections. The policy is pure and
receives an explicit instant; no clock reads, grace extension, payment verification, purchase cache or backup-derived
right exists. Plus Lifetime survives an overlapping Pro expiry. Active accepted membership in the exact space and
an active Pro host supply guest Plus only there; personal purchases cannot authorize another space. Host expiry
preserves existing-data commercial rights independently of server roles/retention (OD-05 is still open).

Quota scope is profile, device, online identity or space. Paused goals/plans count; archive/completion/ended revisions
and explicitly selected read-only items do not consume new-work slots. A budget definition uses canonical account
scope, currency and period kind; copied dates/row ids never create extra definitions. The future service supplies
active-definition/read-only selection; this model never invents a database migration or chooses/deletes data.
Shared seats include the owner and pending invitations until explicit expiry, never devices or declined invitations.

The application does not reference or enforce this policy. FeaturePolicy remains the independent Simple/Advanced
presentation policy. No new network SDK, permission, schema, financial write or portable purchase data is introduced.
See [policy evidence](../../quality/entitlement-policy.md).

## 8. Account write boundary implemented (D-118 / ENT-02 in progress)

Data/Commerce exposes ICommercialWriteAccessSource for synchronous already-resolved cached facts tied to the exact
opened SQLite file. The current DI provider is inactive; no paid/customer grant, secret, portable flag or UI switch
exists. CommercialWriteTransaction captures access, checks the exact normalized path spelling, obtains the database
writer before count reads, rechecks unchanged facts and commits before Changed. Its owned transaction rolls back
on rejection/failure; an outer transaction stays owned by its caller. No network/provider verification runs here.

SaveAccountAsync uses this boundary for every caller, including onboarding and account/debt forms. Creation and
unarchive check the same active count; correction/archive do not add capacity but still require scope permission.
Structured failures carry feature, permission or count details for later translated ENT-04 presentation.
No app activation, model/schema change or automatic read-only/active selection is introduced. Further resources,
restored/imported over-quota selection and native operation gates remain ENT-02/03 work, not accepted by this step.

## 9. Template/filter write boundaries implemented (D-119 / ENT-02 in progress)

SaveTemplateAsync and SaveSavedFilterAsync use the same actual-file cached-access writer gate before identity/count
reads. New rows consume capacity; existing ids and confirmed same-name replacements reuse it. Count before sort
assignment/removal, preserve full metadata and roll back failures before Changed. Unlimited contexts skip count
queries. Current registration is inactive. There is no schema, purchase data in backups or visible UI change.
The rest of ENT-02/03 and deployment activation remain open. [Evidence](../../quality/template-filter-write-policy.md).

## 10. Goal and plan batch/continuation boundaries (D-120 / ENT-02 in progress)

GoalStore and PlanStore capture the exact opened file's cached commercial facts. Goal reopening checks active/paused
capacity before normalization/pin writes. Plan batches compare actual stored affected states and requested states
inside the same writer, rejecting net growth before any partial edits. SaveSplitAsync retains its owned history
transaction and verifies a distinct linked continuation against the stored predecessor, never the UI's already
ended object. Check membership/new-tool capabilities, roll back failures and raise Changed only after commit.
Retained historical/tool corrections and slot-neutral continuations remain available after expiry. Current
registration stays inactive; other ENT-02/03 operations and activation remain open. [Evidence](../../quality/goal-plan-write-policy.md).

## 11. Current-period budget definitions (D-121 / ENT-02 in progress)

Save/confirmed Replace uses the exact opened file's cached-access writer. Bind canonical definitions to the actual
financial scope; count only rows covering injected device-local today using their calendar and stored month start.
Existing Save retains stored period/currency identity. Separate new advanced tools from retained corrections,
check before limits/removal, recheck access and notify after commit; replacement failure restores all old rows.
Inactive deployment adds no budget counting transaction or paid grant. No model/schema/backup changes. Explicit
active/read-only selection/future activation and remaining ENT-02/03 entry points are unfinished:
[evidence](../../quality/budget-write-policy.md).

## 12. Holding operation rights and atomic purchase prices (D-122 / ENT-02 in progress)

HoldingStore captures cached access before acquiring its actual-file writer. Direct new work uses ManageHoldings;
existing corrections/delete/Undo preserve data rights, including expired hosts, without substituting personal Pro
for membership. Imports preserve owned history; explicit read-only classification remains unfinished. An explicit
requireTransaction retains existing operation transactions when enforcement is inactive, without a quota transaction.
Full history/id reads occur inside the writer. The purchase editor's optional derived-price operation saves the
event/payment/fee and latest same-type/date price together; old non-editor callers retain the no-price overload.
Recheck cached access after SQL before commit; roll back full group/old-fee mutations on failure and notify once.
No schema, SDK, permission or portable paid facts change. Current registration remains inactive:
[evidence](../../quality/holding-write-policy.md).

## 13. Complete goal draft and retained contribution rights (D-123)

GoalStore applies exact-file cached facts to allocations/contribution Save and deletion. Positive earmarks/new
configured work use AdvancedGoals; negative releases, retained edits and deletion use retained data rights.
New reminders are separate. Empty basic dates never grant paid work; reopening checks retained work/reminders.
The whole editor Save owns one actual writer even with inactive enforcement. Checks precede pin/protection work;
SQL and access recheck precede commit and one Changed. Reuse existing goal preparation and copy complete plan
metadata with its original identity. Current registration remains inactive, without portable paid facts or
schema changes. Selection/automation/native/other resource paths remain unfinished:
[evidence](../../quality/goal-contribution-write-policy.md).

## 14. Database recovery source and retained import operations (D-124)

CommercialFileAccess centralizes cached actual-file rights for writer guards and the specialized database backup
source. Replace only the exact generic Zanance database source registration. The generic library accepts optional
cached authorization callbacks and binds one context before restore input, reusing it for copy/migration. No external
writer surrounds SQLite BackupDatabase. Keep other sources, integrity checking, temp cleanup and safety-copy flow.
Owned data above quota/after host expiry remains recoverable/importable with exact membership; paid facts stay
outside portable data. Import/Undo retain their existing writer and explicit aggregate decisions, with post-SQL
access recheck and complete rollback. Current registration is inactive; selected read-only data and other resources/
automation remain open: [evidence](../../quality/recovery-write-policy.md).

## 15. Categorization patterns under the actual writer (D-125)

SaveCategoryRuleAsync and DeleteCategoryRuleAsync use actual-file cached facts. New pattern/kind demands
CategorizationRules; correcting the category of an existing same-text/kind pattern uses Corrections, retaining
the existing new-id replacement contract. Deletion uses DeleteData. Capture before writer wait; matching and
replacement share that writer even while deployment stays inactive. Check before draft trim/removal, retain
stored CreatedAt on updates and recheck after SQL before commit. Complete rollback and Changed-after-commit
apply to enabled operations. No ledger reclassification or portable paid facts. Suggestion delivery and other
ENT-02/03/04 operations remain unfinished: [evidence](../../quality/category-rule-write-policy.md).

## 16. Ledger Save with complete groups (D-126)

All SaveEntriesAsync/SaveEntryAsync overloads reach one actual-file writer before existing rows, groups and refund
validation. New split/part requires SplitTransactions, retained correction/join uses Corrections; distinguish
holding groups and transfer fees. New ordinary money work uses Transactions, with exact membership required.
Final-batch refunds and edited purchases validate before cleanup/audit. SQL, derived paid totals and attachment
moves commit only after cached recheck; failure restores complete rows and emits no Changed. Deployment stays
inactive; delete/Undo, selected read-only resources, automation and other paths stay open:
[evidence](../../quality/ledger-write-policy.md).

## 17. File-bound owned deletion and Undo (D-127)

DeleteEntriesAsync and RestoreEntriesAsync acquire the actual-file writer before reads, with DeleteData and
Corrections respectively. Preserve exact membership and owned-data rights after host expiry. Return explicit
stored refund relationships with committed deleted rows and their original file, without global session links.
Wrong-profile batches reject; missing ids only are restored/relinked. Recheck after SQL before commit, rollback
complete rows on failure and emit Changed only after actual commit. Independent providers serialize existence
reads. App retry keeps its original deadline/version; replacement/dismissal and concurrent taps remain safe.
Registration inactive; selected read-only data, automation and other paths remain open:
[evidence](../../quality/ledger-undo-write-policy.md).
