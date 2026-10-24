# D-511 - Complete snapshot oracles after the Home revision migration

## Hosted failure and diagnosis

The D-509 hosted [CI run](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40093280558)
fails in Libraries and tests, job120870188607. Its retained original log reports5231 cases:
3875 succeeded,1331 failed and25 skipped. Windows and Android build jobs succeed; CodeQL and
the two independently triggered service qualification workflows succeed. D-510's documentation
commit also has a [failed CI run](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40096133747)
and [successful CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40096133742).
Neither earlier failure is rewritten as success.

The common failure is in test expectations. Complete SQLite snapshot helpers derive their
expected table inventory from the EF model plus migration history/lock tables. D-509 deliberately
creates HomeFinancialRevision through a migration without making it an EF entity. The old
inventories therefore reject the known private table before exercising the intended behavior.
The shared application snapshot helper propagates this assertion to many otherwise independent
flows. Some confirmation callbacks fail before publishing their expected observation for the
same reason; they do not establish a separate application-command failure.

A second expectation conflates financial restoration with restoring the opaque revision token.
Accepted delete/Undo, import/Undo and split/join writes must advance the token even when every
financial row returns to its original value. Comparing that token to its pre-write value would
contradict Home's stale-read protection. The production migration,18 atomic triggers and original
Home publication guard remain unchanged.

## Bounded oracle correction

Explicitly add only HomeFinancialRevision to the17 existing table inventories. Keep the exact
inventory equality: an unknown table still fails. Keep every table's complete rows and column
values in ordinary no-write, refusal, rollback, read-only, repeated-Undo and refresh comparisons,
including the exact revision bytes. Do not globally exclude the marker or silently accept every
migration-owned table.

A shared test-only comparison accepts a token difference only at explicitly identified successful
financial-restoration boundaries, or inside the existing comparison mode that intentionally omits
accepted ledger changes. It requires the known table, exactly one row, two values, singleton Id1
and an actual32-character N-format Guid. Normalize only that validated token; retain every other
table, row, financial value, attachment, link and audit field. Existing narrowly documented audit
timestamp allowances remain unchanged. After a restoration, capture its actual complete snapshot
again for subsequent no-op Undo/refresh checks so those checks also retain exact token equality.

Three new real isolated SQLite controls prove that a changed financial amount, malformed revision
and unexpected table still fail the appropriate comparison. These are oracle guards, not new
product features or physical financial acceptance.

## Focused verification and limits

Both actual application-flow and data-test projects build with ContinuousIntegrationBuild=true:
zero warnings/errors. Select the73 classes' failed methods from the original hosted log. This
selects1334 cases, including three passing theory siblings of affected methods:1275 pass and59
then fail on the now-reachable financial-token expectations. Preserve those failures and successes.
Correct only the explicit write/restoration comparisons. The subsequent12-class selection passes
69 cases: the remaining failures and necessary theory siblings, three new oracle guards, and six
cases affected by consolidation into the shared test-only comparator. All1337 distinct selected
cases have a retained successful result; this is not a new whole-suite pass.

Do not repeat the3875 successful hosted cases as a suite,25 skipped cases, unrelated application
builds, native cohorts or accepted financial commands on Samsung. The unchanged D-509 complete
signed APK and Main baseline remain installed. A host UTF-8 edit failure affects only one test
source during preparation; recover its original tracked bytes and reapply the exact change before
strict compilation. No fixture text or production behavior changes to repair that host failure.

The original D-508 one-shot check reports successful CI/CodeQL; the D-509 and D-510 one-shot checks
report their actual failures and are consumed/deleted. A new exact-head hosted result is required
after the finished correction is pushed. This work does not accept broader stability, performance,
security, accessibility, iOS, provider, Store or release gates.
