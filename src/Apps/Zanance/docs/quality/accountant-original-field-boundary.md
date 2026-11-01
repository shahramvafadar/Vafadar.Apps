# Accountant original-text boundary (D-715)

## Defect and unchanged contract

The original accountant packet builder checked source text before adding its spreadsheet-protection
apostrophe, then checked that already protected text again in `PacketFile.Row`. A valid 65,536-character
field beginning with `=`, whitespace followed by `+`, a tab, a carriage return or `@` therefore became
65,537 characters and was refused. Ordinary CSV import admits the original complete 65,536-character
field; this is an actual source contract, not permission to raise the export budget or truncate text.

Pass original values to `PacketFile.Row`, the single existing text validation/protection boundary.
The original 65,536-character limit, whitespace/formula neutralization, note/document opt-ins, selected
accounts/year, complete source validation, manifest hashes and archive budget remain unchanged. No
format, schema, migration, financial calculation, provider or localization change is introduced.

## Focused regressions

Five new original-import/real-SQLite cases fail before the product change with the source-bound
exception, then pass after it. Two additional controls preserve a maximum plain note and refuse an
oversized stored note without changing source rows. The eighth case preserves an ordinary line-feed
prefix under the existing accountant policy. Its first expected value incorrectly added an apostrophe;
that separate test expectation failure is retained, corrected, and only that case is rerun.

The final distinct set is eight new cases plus 23 affected existing accountant-flow cases, 31 total.
The first fixed run has 30 successes and the one mistaken line-feed expectation; the single corrected
case then passes. Successful cases are not replayed after that test-only correction. The host runner
actually uses .NET10.0.12; this is not a claim that those tests ran on host runtime14. Missing test
namespace and unsupported initial runner switches are retained separately from product failures.

## Actual Android execution

A fresh API36/x64/4096 emulator and UID10216 run the linked original application with one narrow,
independent process/run-bound diagnostic provider. Original covered startup reaches the actual
Welcome page. One new fictitious zero-opening account and one expense of100minor units are prepared
once: the actual generic CSV preview accepts a complete 65,536-character synthetic note, then the
fixture stores that preview row through EF. This is a new controlled data write, not a native Import
command, a financial UI acceptance, a replay of an old Save, or evidence about real accounts.

In the same actual PID4807, the original `AccountantExportStore.ReviewAsync` and `BuildAsync` produce
one ZIP. The original transactions CSV contains the entire note plus exactly one protective apostrophe,
65,537 encoded characters. Every member's actual hash matches the manifest. All22 captured typed
source collections/settings compare exactly before and after this read-only export. No ZIP is shared,
no recipient/authentication flow is exercised, and complete private database/key equality is not claimed.
The public Welcome page and original OS values remain unchanged.

The source is compiled from the actual current files. Do not infer byte identity of every linked PE
from that source admission. The native package contains the exact reviewed SGen
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`.

## Builds and packages

Strict Windows builds with zero errors/warnings. The complete signed offline Android package passes
384 own AOT/408 ELF/1608 static16KiB LOAD checks, ZIP integrity/alignment and v2/v3 signatures:

- Shipping: `dccb0480996678d8bebd7c7d5f891099afaa903552c3396f12d4662a5e590fa7`,99,348,383bytes.
- Used independent x64: `955df1c81e1c2a73d873ea6d323e4fc628f49c30f72294288890b658265137de`,56,819,618bytes;
  192 own AOT/204 ELF/804 static LOAD checks pass.

The shipping manifest has no independent accountant authority/provider. Neither package is adopted
on Samsung. The first Android build selected an SDK without build tools and failed before a candidate
was admitted; the corrected build uses the already installed build tools. The private provider's initial
nullable-path compile failure is also retained. No successful build is repeated for those failures.

## Restoration and retirement

Original boot `b6bcee09-71af-44d8-b6b8-75e5e18ba02d`, API/page size, locale,1080x1920/density420 and
all five captured settings remain exact. Two owned XML files/42,074bytes are hash-matched and removed;
only the fresh package/UID is uninstalled. Owned host83296 and console5572 close. Official AVD deletion
retires25files/5,222,511,727logical bytes, not a measured host free-space recovery.

Four scoped standard Cleans finish successfully. Initial Windows framework/RID metadata mismatches,
an overbroad forced framework graph, and an escaped Android-RID-list restore failure are preserved;
ordinary exact-platform restores resolve cleanup metadata. Successful checks/builds and earlier
successful Cleans are not replayed. Keep the recorded deletion-line counts separate from distinct
file counts and measured recovered bytes.

One supplementary seven-target removal is rejected before process creation with `blocked by policy`.
941files/284,647,145logical bytes remain frozen: this batch's private source/output root, focused-test
outputs, residual owned-AVD root and four unused publication files. No retry, subset, alternate method,
move, escalation or reuse is allowed. Used signed packages, named source archives and bounded evidence
were retained outside that scope before the attempt; earlier refused scopes remain untouched.

## Remaining boundaries

D-713's sole response records self-hosted service qualification and CodeQL success, CI cancelled;
D-714's sole response records CodeQL success and CI in progress. Both schedules are consumed/deleted
without another GET or later-result/cancellation-cause inference. D-715 receives its own final-head
one-shot follow-up after push.

Phone/D-607's three deadlines, consumed reboot, unread post-reboot source and Main-adoption condition
remain untouched. Broader corpus/device/DB/key/security/stability/performance/accessibility/provider/
Store/release gates remain open. Under the latest owner order, iOS execution waits for Android
completion. This fixes the demonstrated accountant field boundary, not those other gates.
