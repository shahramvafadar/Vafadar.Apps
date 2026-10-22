# D-396 - Original balance review and atomic adjustment

## Observed source defect and delivery scope

AT-11/ACC-08 previously has domain-only acceptance. The original AccountDetailViewModel retains a
ReconciliationResult after ObservedText or ReconcileDate changes. Adjust then combines the old result
with the current date and separately saves the entry and account metadata. The source establishes a
stale-review and partial-write defect; no incorrect physical-phone save is claimed as observed.

Finish this complete financial correction before returning to D-386's deferred stability/accessibility
work. The already delivered D-395 Plans header and unrelated successful matrices are not repeated.

## Implementation

The data layer captures one coherent SQLite read of the original account and all its source/destination
ledger rows. An opaque single-use review retains original file/access identity, immutable serialized
inputs, date, observed amount and comparison. Caller edits cannot manufacture another reviewed packet.
Checked correction arithmetic refuses an unrepresentable signed difference before consent.

The actual existing commercial/household writer rechecks the original packet, validates the original
ledger correction, and commits adjustment plus current stored account metadata atomically. It never
overwrites a stale form account or regresses a later reconciled date. A match updates only metadata.
Application retirement and access snapshots are rechecked before save and commit. Successful writes
remain consumed after a subscriber failure; rejected transactions remain retryable.

The original application session invalidates consent on amount/date/account changes, coalesces financial
operations and checks reason/review again after native consent. The existing covered form disables
input while busy. Original commands use the native interaction port, with translated retry feedback in
all six existing languages. No schema, compiled-model, permission, export or provider change is needed;
review packets are transient and never added to portable backups.

## Completed focused evidence

- Twenty original application/session and real SQLite cases pass under strict compilation: positive
  and negative adjustments, stale input before/during consent, cancellation/changed reason, changed
  account/history, matching metadata, exact-file refusal, required reason, replay refusal, monotonic
  date, inactive-to-active access retirement, overlapping requests, transaction failure/rollback,
  post-commit observer failure, final draft retirement and arithmetic limits.
- Thirty existing ZananceStore ledger regressions pass. These are related to the existing shared writer;
  no complete solution or unrelated successful suite is replayed. Earlier 14/18-case executions are
  retained development evidence, not additional distinct tests.
- Independent strict Windows Debug build passes without warnings/errors. The first diagnostic build
  fails on an ambiguous IValueProvider name; that exact failed build is corrected with the fully
  qualified Windows type, without replaying managed financial tests.
- Initial native run aacc2fae0e2242b8b2872fa023e16e4f terminates before any comparison/financial command:
  its local TextBox peer exposes no usable Value pattern. Preserve this refusal and original single
  fictitious account. Do not claim successful native typing or change the owner desktop/focus.
- Continuation b0f4412e26224226aae1b1f9c5d3699d uses that same file/account, then completes all six
  en/fa/de light/dark original account contexts. Fictitious draft properties prepare input; original
  buttons and native Cancel/Accept use Invoke patterns. Amount/date changes retire old authority,
  missing reason refuses, Cancel posts nothing, and one accepted adjustment per context changes
  original 100.00 EUR to 212.34 EUR with 112.34 EUR correction and matching account metadata.
  The independent base installation remains unchanged. No accepted financial action is replayed.

## Corrected visual evidence and complete builds

The first eighteen images capture the page after width reflow, leaving the required-reason label
outside the viewport. They do not prove its visible layout. Visual continuation
ea4b73b2bd6d426a9d56d8866abd4c06 uses the same six accepted accounts, new nonmatching comparisons
and no financial Save. It scrolls the actual error into view after each width change. All eighteen
en/fa/de light/dark 360/412/1280 own-window images are inspected; the required-reason feedback is
visible and complete. All six accepted financial results and independent base inputs remain unchanged.
The owner input desktop remains Default. No native typing/OS scaling or general accessibility claim.

Strict Windows Release completes with zero warnings/errors. Complete offline Android APK is
artifacts/android-reconciliation-d396/pro.vafadar.zanance-Signed.apk, 95,403,935 bytes, SHA256
1204295fd1c7cc17dc7f6f611e216adaa46bc788001b1332b1971f391567b171. Package pro.vafadar.zanance,
ARM64/x64, no INTERNET/debuggable declaration. V2/v3 signature verifies with the existing local
development certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
This is an installable local candidate, not a store-signed release.

## Original Android persistence and restoration

Upgrade the complete Main APK once without reset on the independent x64/API36 emulator, retaining
original Home0EUR and exact four OS settings/412x800/density160. Initial splash focus refusals are
retained; continue the same PID27359, never reinstall or relaunch to repair observation. Use only a
new D396_Fictitious_Reconcile profile and original wizard-created D396_Checking account100EUR.

Original native EditText input112.34 and Compare show recorded100EUR/difference12.34EUR. Edit the
same field to212.34: old result/adjust action disappear. Fresh Compare shows112.34EUR difference.
Missing reason refuses. Enter D396_native_review, Cancel original native consent, and retain that
same draft. One subsequent Accept posts exactly one Balance adjustment row112.34EUR with original
date/account/reason; the actual detail page shows212.34EUR and Last reconciled10/19/2026. A duplicated
observer filename is refused before opening the account card; continue only that unperformed target.

After terminal accepted-write/detail/Home observations, one planned cold process starts PID31379.
Original Home212.34EUR, adjustment112.34EUR and reconciliation date persist. No comparison, accepted
Save or substitute fixture is replayed. Original native profile action-sheet rows expose no clickable
flag; retain that refusal before input, then select only the observed original Open list item, bounded
by its original native ListView. Restore Main0EUR, retire only the inactive owned profile through the
original Delete menu and both confirmations. Original meaningful Home and profile-list signatures
match their initial observations. Four OS settings, size/density and installed candidate hash match.
Every temporary external hierarchy file is hash-checked and removed by its own observation helper.
No private database/key extraction, financial reset, system-setting change or Samsung input occurs.

Ignored emulator-reconciliation-d396 artifacts retain original observations, intents, refused
observers and final-android-result.json. Test output is cleaned after the successful focused runs.
Native script scope refusal is checked before build/desktop work. Documentation and staged owner
identity/hooks are reviewed for the final commit; hosted success still requires the one prescribed
delayed exact-commit CI result. These finite checks do not establish physical-phone/iOS, all histories,
general ANR/crash/speed, accessibility, store signing or release acceptance. D-386 priorities remain.

## Physical continuation (D-472)

The formerly missing physical boundary now has a bounded original Samsung lifecycle on the D-471
candidate: amount-change invalidation, reason refusal, native Cancel, one accepted adjustment and
cold account/history metadata readback. No D-396 successful test/build/financial scenario is
replayed. See [physical evidence and remaining limits](physical-account-reconciliation.md).
