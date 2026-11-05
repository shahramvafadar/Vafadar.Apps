# Exact liquidity and protected-money results (D-858)

## Full read-only correction

Liquidity/headroom implements ZEX-S0606/K02 and covered goal protection ZEX-P16/AT24,
under the existing ZEX-S0104 checked-money requirement. These are estimates, never
"safe to spend" or automatic allocations. Current Headroom, projected liquidity and
covered per-currency protection use unchecked long subtraction/addition. Unsupported
deficits can therefore appear positive, or an overflowing day can disappear from
minimum selection. Original grouped allocation Sum instead throws on a temporary
overflow even when unordered releases later cancel to a representable exact total
and actual account coverage is small.

Headroom and each projected deduction now use checked arithmetic; covered currency
totals also check before publication. Every unrepresentable projected day refuses
the result rather than selecting another apparently valid minimum. Original allocation
history aggregates in local Int128 before the original zero/positive-balance coverage
cap; only the bounded covered amount narrows. The covered result, not an uncapped raw
earmark history, is the contract. Existing writer/source validations are not weakened
and this is not permission to import malformed allocations. Account balance computation,
goal selection/state, currency partition/case handling, protected policy, date/estimate
rounding, unknown/excluded forecast items, explanation order and original objects remain.
No ledger/goal/allocation mutation, schema/model, migration, reader/writer, UI/string,
runtime, GC or dependency change.

## Actual failed baseline and selected host evidence

The first new test build refuses two collection-size assertions under xUnit2013.
Use Assert.Empty; this is test-only analyzer evidence, not a product failure. The
corrected original build runs17 new cases:8 actual failures and9 passing controls.
Two unsupported headroom cases wrap; two projected paths fail to refuse an overflowing
day; two EUR/eur multi-account covered totals wrap; two unordered allocation histories
throw before exact cancellation/current-balance coverage. The latter use one real
account/goal identity, dated allocation/release/restoration and a later Expense row:
current original balance is100 while exact remaining earmark is MaxValue. Both positive
input orders must return covered100 without modifying any original allocation.

After correction17 new cases pass on .NET10.0.14/X64. Two directly related existing
K02 cases also pass: the ordinary estimate/protected-money example and omission of
day-to-day spending/balance-goal reservation. Total19 selected host cases, not a full
suite. Other new controls preserve exact limits, another-currency omission, no-future
deduction, full release, archived/unusable exclusions, positive balance cap and original
unknown explanation item identities. Original baseline Core bytes/outcomes are retained.
No accepted D857 or older financial/auth/photo/test sequence is replayed.

## Original Android calculation execution

Use retained original5606/API36/x64/kernel4096 on unchanged boot
6663f98e-6546-4809-a0bc-285f11a96674 and existing5038. Install/normal-launch only fresh
pro.vafadar.zanance.liquidityboundaryreview once, UID10248. An initial PID lookup is
too early and refuses before any provider/check marker. A subsequent actual PID29683
and owned focused window are observed; the bounded filtered exit-metadata query has
zero matching records. This is not proof of general stability or an original-fault
remedy. Continue that existing launch without reinstall, relaunch or reset; the one
actual check runs17 direct original calculations through the same tracked case methods
and a small private assertion adapter, not a native test-runner or17 additional new
permanent tests. Current original Onboarding remains the root. All financial objects
in these cases are disposable in-memory inputs, not database preparation or saved
transactions/allocations. No commercial permission is overridden.

All20 typed source components before/after match with Accounts/Entries/Schedules0.
Source SHA256:
63348846C3BFEF11E7AF55F94A8BE7BBE93EBFAF76AA098BDDF887CEE5C6CD11.
This proves calculation execution and the empty-source boundary, not preservation of
arbitrary financial payloads absent from that database or native Reports-screen coverage.
Source inspection confirms the original Reports workspace keeps failed publication
covered and CanInteractfalse. Its existing magnitude sentence is left unchanged: the
Minimum<0 branch runs first, and otherwise nonnegative Minimum/protection bounded by
MaxValue cannot require negation of MinValue. This is source reasoning, not a new UI pass.
The sole private App source delta reverses exactly to original MauiProgram after
removing its original service-container capture; product App source remains unchanged.

## Packages, retirement and open gates

Strict host/Windows/full shipping/private builds pass without warnings/errors.
Complete offline shipping SHA256
4f7d3f8007d01e3c24bbb337dd43724d59010713116cc9cedc49a0bd28968ba2,
99487647bytes,384AOT/408ELF/1608staticLOAD is prepared, not installed on Main.
Actual used private SHA256
d8b8756f8a82532070f529e95b8e0173ccc84642958398c82c20f248c5408308,
56893346bytes,192AOT/204ELF/804staticLOAD passes alongside shipping v2/v3, CRC,
ZIP alignment, offline/nondebuggable and original Mono checks. Nine actual own-artifact
App/Core/Data AOT objects equal their APK members; no whole-PE identity is inferred.
Current source/private source archive, original baseline bytes, signed used packages
and raw outcomes remain in local bounded evidence.

Only freshUID10248 retires after source/PID proof. Original emulator package UIDs,
five OS values, locale key presence, boot, geometry, reverse and shell UID match exact
preflight. Four standard Cleans pass, without XML/supplemental deletion/frozen scope.
Original D809/D833 data/provider/history/trace and terminal records remain unread;
no register/R13 interpretation, intentional fault, reset/reboot/server start/reconnect/
pair, phone, genuine authentication, camera or financial Save occurs. Main31707/abf
is only the dated D856 checkpoint. D838 decision/D841 unexecuted reader, original
native-fault cause, whole performance, security/accessibility/external/platform/release
gates and the active overall goal remain separate. Closed rejected performance
candidates are not retuned or resampled. CI gets one read-only check after push.
