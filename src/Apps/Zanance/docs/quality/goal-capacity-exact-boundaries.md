# Exact goal capacity, commitments and monthly needs (D-857)

## Complete corrected calculation path

Goal capacity is a read-only suggestion limit (ZEX-S0604/AT23), not an allocation or
ledger write. The original CapacityResult.Amount subtracts five long components
without checking; principal and other-goal commitment loops also add without checking.
Need adds months-1 to a supported remaining amount before dividing, so a large valid
remaining target can become negative and receive no distribution despite available money.

Final capacity now subtracts in local Int128 and narrows once with checked arithmetic.
Representable exact cancellation remains supported, while an unrepresentable completed
result refuses instead of wrapping sign. Missing history still returns null without
evaluating unused components. Monthly principal/other-goal totals use checked additions;
original installment splitting, monthly equivalence and half-away rounding stay intact.
Need uses a wide ceiling numerator before checked narrowing. Original month-count/date,
priority/distribution, exclusion, financial-calendar/history, quantities, interest,
recurrence, balance and commercial policies remain unchanged. No public stored type,
schema, migration, reader/writer, UI layout/string, runtime or GC change.

The initial quantity-rounding hypothesis is not reproduced by the new near-boundary,
exact-limit, overflow-refusal and nonpositive controls. QuantityFor is not changed and
is not claimed as a repaired defect. Existing Distribute bounds remain intact.

## Host evidence and valid-domain fixture correction

The first test-only build omits required Goal.CurrencyCode and refuses compilation
with CS9035; that is not product evidence. Correcting the test fixture permits18 actual
baseline cases:8 fail and10 pass. Failures cover two capacity overflow signs, three
ceiling needs, two commitment sums and the distribution consequence of a wrapped need.
After four original calculation corrections,18 pass on .NET10.0.14/X64. One directly
related existing capacity-income/spending/plan case also passes; no full suite runs.

Review then strengthens only the two commitment inputs: the goal case uses two distinct
active monetary goals, each with one plan (the actual database's unique GoalId policy);
the installment case includes a real payment-source account identity instead of an
unresolved source. To avoid relying on the weaker first inputs, compile the original
HEAD59b3f23a Capacity.cs in a private host with only its namespace relocated and the
original reports namespace imported. Exact normalized reversal is retained. The two
strengthened current cases still fail that original implementation because overflow
is not refused. Both pass the current correction. Only these two changed cases are
re-executed after strengthening; the16 unchanged successes/related old case are not
replayed. Final selected evidence is18 distinct new+1 related, with a two-case subset
continued after fixture changes, not20new or every observation on one host test binary.

The private initial Android candidate is never installed/executed and its signed bytes
remain. A necessary final private build links the strengthened tracked case source.
Product Windows/full shipping inputs do not include test fixtures and remain unchanged.

## Actual original Android calculations

On retained5606/API36/x64/kernel4096/boot6663f98e-6546-4809-a0bc-285f11a96674,
install only fresh pro.vafadar.zanance.capacityboundaryreview once, UID10247, and normally
launch once. An early focused-window observer assertion occurs before any check marker
or provider calculation. Observe the now-owned current window and continue that existing
launch, without restart, reinstall or any successful calculation replay. The validated
native PID29274 runs all18 cases exactly once through direct calls to the same tracked
case methods, with a small private assertion adapter; this is not a native test-runner
or a new claim of18 additional permanent product tests. Original current Onboarding
remains the actual root; no report screen, commercial override or financial action is
invoked. Goals/accounts/plans used by calculation cases are disposable in-memory objects,
not database preparation or accepted financial commands.

Before/after all20 typed source components match, with Accounts/Entries/Schedules0.
Source SHA256:
05120EAB6B833261861C47647A61816B0CFEB4DDF3ED99A8B375E10161EF7C12.
This qualifies original calculation execution on Android, not a changed Reports/Goal
screen or whole private-source payload preservation for nonexistent financial rows.
Source inspection confirms Reports retains its original covered workspace exception
boundary and GoalDetail retains HasReadFailed/needsReload; those unchanged UI routes
are not relabelled as fresh native UI acceptance.

## Qualified artifacts and preservation

Strict selected-host, original-source baseline, Windows and full/private Android builds
pass without warnings/errors. Complete offline shipping SHA256
a5442adaa9ffb3d8192b1d6a408b7b98fed6b705006d9f7e9b3ba0cb450c132e,
99487647bytes,384AOT/408ELF/1608staticLOAD is prepared, not installed on Main.
Actual used private SHA256
299a871426d628260bd4d20e19486a9812457cb6401b9ef7f54c3f58ea4bdf46,
56889250bytes,192AOT/204ELF/804staticLOAD passes alongside shipping v2/v3, CRC,
ZIP alignment, offline/nondebuggable and original Mono checks. Nine actual own-artifact
App/Core/Data AOT objects equal their APK members; whole-PE identity is not inferred.
Current source inputs, original-source baseline/private source archives, raw outcomes
and all used/unused signed candidates remain in bounded local evidence.

Only freshUID10247 retires after source/PID checks. Original emulator package UIDs,
five OS values, locale key presence, boot, geometry, reverse and shell identity match
preflight exactly. Six standard Cleans pass; no XML, supplemental deletion, frozen scope,
original D809/D833 source/provider/history/trace, register/R13 interpretation, reset,
reboot, server start/reconnect/pair, phone, authentication, camera or financial Save.
Main31707/abf remains only the dated D856 checkpoint. Existing D854 failed cost gates
and all other rejected candidates stay closed; no timing/resampling/retuning or whole
performance acceptance. D838 pending decision/D841 unexecuted reader and original fault,
security/accessibility/external/platform/release gates remain separate. Goal stays active.
CI follows one read-only check after push, without polling or successful job replay.
