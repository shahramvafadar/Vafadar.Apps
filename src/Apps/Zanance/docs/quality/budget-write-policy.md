# Current-period budget write policy - D-121 / AT-123

ENT-02 remains in progress. Budget Save and confirmed Replace prepare current-period definition checks under the
D-118 actual-file cached-access SQLite writer. Current registration remains inactive with no permanent paid grant;
test builds remain unrestricted. No visible control, schema/migration/compiled model, SDK, permission or backup
format changes. Explicit active/read-only selection and future-period activation are unfinished ENT-02/03 work;
this partial boundary does not introduce an automatic selection policy or complete budget enforcement.

## Enabled write contract

Read the existing Settings month-start value without creating/updating Settings. Injected device-local today and
each budget's own calendar/period identify current financial-period rows. Core BudgetDefinitionKey binds the actual
financial scope, normalized ISO currency, period and canonical account set; row ids, calendars and copied months
are not extra definitions. Historic and future rows are not current capacity; their later activation is not accepted
by this step. Unlimited contexts avoid definition-count reads.

Save retains its original persisted period/currency/calendar identity on update. Project this effective identity
before checking a changed account scope; ignored incoming fake historical dates/currency cannot bypass capacity
while the stored row remains current. Check before mutating owned category limits or audit. New advanced methods,
rollover and weekly periods require AdvancedBudgets; retained/current tool corrections and historical metadata
remain correctable. Existing over-quota edits and net-neutral replacement do not request another slot. Membership
still applies, even with personal Pro; expired-host retained corrections/replacements preserve their rights.

Replace retains its owned transaction. Check before deleting the old row/limits, preserve the original two-step
delete/insert uniqueness behavior and recheck cached access before commit. A failed new-limit insert rolls back
the complete old budget/limits. Notify Changed only after commit. No network/purchase work occurs inside the writer.

## Real SQLite acceptance

26 new cases carry AT-123. The focused suite passes; full main suite passes 1,721 with zero failures/skips,
App.Tests 299 unchanged. Cases cover unrestricted inactive saves; second current definition rejection before
audit/limits/Changed with complete stored-data equality; account order/duplicates and currency case; Gregorian,
Persian and Hijri current rows; shifted financial month start; history/future copies; ignored incoming stored-
identity bypass; retained over-quota corrections/net-neutral replacement and complete other metadata; paid/exact
shared unlimited capacity; six new advanced tools; retained paid/history corrections; missing membership and
expired-host data rights; no Settings creation or ledger writes.

A SQLite trigger rejects insertion of replacement limits after the old budget was deleted. Every complete stored
row matches the original, no Changed fires, and retry succeeds after dropping the owned trigger. Independent DI
providers/context factories capture their initial access before acquiring the same SQLite writer: exactly one
final-slot current definition commits, the other receives structured quota rejection and Changed fires once.
Counts are not mocked or serialized by an in-memory store semaphore.

Evidence: artifacts/budget-policy-focused-tests.log and artifacts/budget-policy-all-tests.log. Test output cleanup
passes with zero warnings/errors: artifacts/budget-policy-tests-clean.log.

## Platforms and normal installed Release

Strict Windows and Android builds plus canonical complete signed Release pass with zero warnings/errors.
Source hashes confirm ZananceStore.cs and CommercialWriteTransaction.cs stayed unchanged during all final builds.
Logs: artifacts/budget-policy-windows-final-build.log, budget-policy-android-final-build.log and
budget-policy-android-final-release.log. No visible UI changed; completed language/theme/width matrices are retained.

Prepare one current USD budget only in the exact independently owned emulator-5570 sample database. The actual
normal Release Insights page opens Create budget; its editor saves an EUR monthly limits budget of 5,000 minor
units and displays the original spending against the new 50.00 EUR limit. Compare complete columns/rows in all
24 tables: only the exact new budget and its own limits may differ. Original USD metadata, financial/settings rows
and ledger match. No profile/security storage or physical device is touched. Restore and verify exact original
database/sidecar bytes, reinstall full Release and return it to stopped Home. Evidence:
artifacts/budget-policy-release-native/proof.json and fresh native hierarchies.

The native fixture schema check initially used a nonexistent Settings Calendar column; use the actual
BudgetCalendar field. The same schema check shows audit fields are INTEGER UTC ticks; prepare numeric fictitious
audit values before any fixture placement. These local preparation corrections are not app defect/acceptance.

Phone-test artifact: artifacts/android/zanance-d121-release.apk, 81,442,901 bytes. SHA-256:
`9c71cc473b7d3a183ba5ce0ff0e6941f2fca2173ad66fe3697aa05edb9a60c3c`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permission boundary and v2/v3 signature pass. The existing local test certificate is not production
signing. Evidence: artifacts/budget-policy-apk-proof.json. Emulator/build proof is not physical-phone, iOS, OAuth,
store or product-release acceptance.

Remaining ENT-02/03: explicit selected active/read-only budget definitions and future-period activation;
contributions/allocations/occurrence automation, profiles/holdings, import/restore and automatic/native service paths.
Final translated limit UI, purchases, encrypted storage and owner/external release decisions remain open.
