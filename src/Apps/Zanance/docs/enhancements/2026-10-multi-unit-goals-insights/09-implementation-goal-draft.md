# 09 – Implementation goal draft

> **Status: superseded on 2026-10-03** – the owner approved the design and the phase plan in [06](06-implementation-backlog.md#phases). Former status: draft, not executable. This text becomes an instruction only when the owner approves the design
> package and sends an implementation goal in a separate message. Its presence in the repository, the accepted
> direction of the requirements document or any older "complete everything" wording is no permission to start.
> No goal, task or automatic run was started.

## Draft text (to be confirmed or edited by the owner)

**Goal: implement the approved Zanance enhancement ZEX in full.**

1. **Reference.** Implement exactly the approved version of
   `src/Apps/Zanance/docs/enhancements/2026-10-multi-unit-goals-insights/` (state the approved commit), using
   [06 – Implementation backlog](06-implementation-backlog.md) as the only list of work. Implement **all** epics
   ZEX-E01 to ZEX-E09 and all their stories, in the dependency order of the backlog – not only the first wave.
   Decisions marked *approved* in [08](08-decisions-and-approval.md) are binding; a proposal the owner changed is
   implemented as changed.
2. **Order.** Start with ZEX-E01: the golden examples of [02 §11](02-product-and-domain-design.md#11-golden-examples)
   become failing tests before any calculation changes, then pass. ZEX-E05 (Simple/Advanced) and the security,
   accessibility and localization criteria are part of every story, not a final step. ZEX-E09 evidence is collected
   for every wave and every platform separately.
3. **Data safety.** Existing data must survive every step: additive EF Core migrations with the migration upgrade
   test extended; restore of backups made by earlier versions keeps working; the compiled model is regenerated
   after every model change; nothing is converted by guessing (old asset accounts keep their value, no grams are
   invented). Atomic linked operations, Undo and the backup/export coverage of [02 §10](02-product-and-domain-design.md#10-backup-export-and-import)
   are mandatory.
4. **Tests.** Never delete, skip or weaken a test to make the build green. Every story ships with the tests its
   acceptance criteria name; the ZEX-AT scenarios of [07](07-acceptance-and-validation.md) are covered and tagged.
5. **Evidence.** Report something as done only with evidence from the same platform and build (Windows Debug,
   Android release on the emulator or a device, iOS only on a Mac/device). Never present a simulated service, an
   untested platform or a design mock-up as a working feature. Update the status of each story in the backlog and
   the project documents (decision log, changelog, UX design, specification Section 31, privacy matrix, acceptance
   test plan) in the same commit.
6. **Blockers.** Record anything that cannot be done (for example no iOS device) as *Blocked* with the exact
   reason and continue with the rest; do not mark it done.
7. **Scope changes.** Stop and ask before adding scope: no online service, telemetry, payment, new OS permission or
   new dependency without a separate approval. A needed deviation from the approved design is proposed first.
8. **Reporting.** After each wave report (in Persian): what changed, what was tested and how, what was not tested,
   which decisions are still open.

## Waves suggested for that goal

| Wave | Epics | Exit evidence |
|---|---|---|
| 1 | ZEX-E01, ZEX-E02 (+ ZEX-E05 rules for them) | Golden examples pass; native totals and the account selection contract verified in the app (Windows, Android) |
| 2 | ZEX-E03, ZEX-E04 | Balance goals and Home card; quantity holdings with linked money side; migration and restore tests |
| 3 | ZEX-E06 | Core reports, headroom and data quality from shared definitions; Home/report/PDF parity tests |
| 4 | ZEX-E07, ZEX-E08 | Quantity goals and trend ETA; wealth history and forecast snapshots |
| 5 | ZEX-E09 closing | Release validation per platform; performance with the reference data set |
