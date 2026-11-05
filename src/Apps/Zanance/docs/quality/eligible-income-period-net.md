# Exact completed eligible-income period net (D-880)

## Original policy and reproduced failure

Original EligibleIncomeResult publishes one period net: Income minus IncomeReversal
for selected accounts/currency/categories/opening/date bounds (ZEX-GO09). Original
counted entries are date-ordered and retained for the contribution preview. Transfers,
refunds and capital movement remain excluded; the result is not a separately published
gross-income total. Baseline a7d0fa0f calls checked long Sum on intermediate effects.

A fictitious pair of checking accounts opens at zero before November. Account A has
Income Max on1November and its correctly linked full reversal on3November; account B
has Income Max on2November. Every account's original end-of-day Balance on every
represented day fits long and is nonnegative. The closing selected period net is Max.
The separate sum of gross incomes is2Max, but this API does not publish that gross
value. Other APIs that publish gross/combined intermediate-day totals retain their
own limits and are not inferred accepted here.

Fifteen new selected conditions run on the original .NET10.0.14/X64 build; seven fail
with actual OverflowException at EligibleIncome.cs/Sum: all six source permutations
and explicit account selection despite default membership exclusion. The other eight
controls already pass. Test source is identical across this baseline and final run;
no oracle/fixture correction, database setup or financial Save occurs in this step.

## Finite correction and preserved refusal

Only the accumulation of the original counted list changes to checked Int128, with
checked long conversion at final publication. Each income/reversal long effect keeps
its original sign expression. Original account membership/explicit scope, currency,
opening cutoff, period bounds, excluded categories, entry-kind/review behavior, stable
date order, original result row identities and share-of-income Suggestion are unchanged.
A genuinely out-of-range positive or negative completed net still throws; a future
or excluded reversal cannot rescue the requested period. No source/revision/cache
policy, writer, financial row, schema, model, migration or rounding changes.

Final15new host conditions pass: six valid source permutations, two true positive/
negative final overflow refusals, two default/explicit scope conditions and five
category/currency/opening/from/to controls. Source-generated JSON compares every
public account/all32ledger fields; original row references/order and tag-list ownership
remain exact. The linked reversal cannot exceed its original Income in the supported
fixture. One affected existing pure test also passes for exclusion of transfer/refund/
bonus and original percentage suggestion. No old63/54/17/28/53/full suite or successful
financial operation replays.

## Current original Android calculation

One fresh full-source pro.vafadar.zanance.periodincomereview installs/launches once on
retained emulator5606/API36/x64/kernel4096, original boot6663f98e-6546-4809-a0bc-
285f11a96674. Actual UID10257/PID7617 executes the current15test bodies once after
original Onboarding readiness. Their fixtures are in-memory only; no account/ledger/
schedule or other database fixture is prepared. Initial/final typed20-component source
matches exactly, account/entry/schedule0, SHA256 `12682C5D6470803400E64852CC83FC7F37FEEB911FABDA1F037ECFE4EC62813A`.
Full public input JSON/ref/tag checks execute within these original test bodies; the
generated native input identities are not claimed to be separately archived raw JSON.

This is actual current Android Core execution with original App/Data startup, not a
new original contribution-plan page/consent/writer journey, ARM64 result, financial
posting or whole database/key proof. No app UI code changes. The sole private
MauiProgram service-provider capture reverses exactly to current original source;
actual tests use bounded assertion adaptation. No original retained package, crash
record/classifier or saved register is read; original SGen cause is not qualified.

## Actual packages and preservation

Strict Windows/full shipping/private native builds have zero warnings/errors. Complete
offline APK `46f3dc515a0e888d297560abef297c310793e437598ba1c0aa19e13542c1527a`, 99286809bytes,
384AOT/408ELF/1608staticLOAD; private APK
`65d96013bdd37f0cd4aa9e1faa4a4d95655b7d5d0e411312ddb12e78f3eb2c30`, 56782620bytes,
192AOT/204ELF/804staticLOAD. Actual v2/v3, ZIPCRC/alignment, nondebuggable/offline and
original Mono hashes pass. Two current source/test hashes and nine actual own App/
Core/Data AOT objects match corresponding built APK entries. This is not whole managed-
PE identity or hardware16KiB runtime acceptance. Shipping is prepared, not Main.

Only fresh UID10257 retires. Original retained emulator package/OS/key-presence/boot/
geometry/reverse state matches exactly; original AVD/data stay running. Shared listener
5038/PID50972 is observed with no reset/start/reconnect/pair. Four standard Clean
profiles pass; source/used test binary/signed APKs/bindings/raw original failure and
current packets remain. No XML capture, supplemental/frozen deletion or reuse, phone,
authentication, photo, successful financial Save/Import/Undo or old-success test replay.

D879 already adopted38d2ba16/Main19466 with actualReady141/public preservation;
that remains this step's dated phone checkpoint, not fresh liveness. Current15ARM64
and existing complete candidate no-reset adoption remain the next independent changed-
platform conditions. Original contribution-preview integration beyond this core scope,
D838pending/D841unexecuted/original fault, whole performance/security/accessibility/
platform/provider/Store/release gates remain distinct; overall goal stays active.
D878's one due API response is retained: CI cancelled, self-hosted service qualification
and local identity backend completed-success, CodeQL in_progress; never request again.
D879 is not yet due. No CI wait or owner prompt displaces ready development.
