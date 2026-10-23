# Literal CSV text and financial identity (D-499)

## Status

The completed bounded source/native qualification is recorded below. Main adoption is pending
the preserved D-494 timed cohort, not inferred from the independent caller. The existing Main
package remains D-495 to protect D-494/D-496. This is not release or whole-device acceptance.

## Reproduced defects and contract

Six of seven initial Core cases fail: an original leading apostrophe can be removed from an
account, title, payee, note, reimbursement name or holdings name. With both protected and literal
account names present, a roundtrip can select the other account. Two additional legacy-source/
destination cases reproduce that ambiguity. Three of four generic-mapping cases fail because
an undeclared protection contract changes literal text or selects a different category.

Own ledger/holdings exports now write format_version3, retaining the existing columns, ids and
money/quantity/date representation. Text protection is reversible, including original apostrophes,
leading/trailing whitespace and multiline text. Neutralize spreadsheet formula-like prefixes,
including leading whitespace. Null/empty optional cells retain their existing canonical meaning.
Versions without a marker,1 and2 retain their earlier decoding; when both raw and decoded legacy
account names identify different existing accounts, refuse the row with the existing account
error rather than choosing. Historical ambiguous text cannot be reconstructed by guessing.
Find the unique named version marker rather than assuming its final column index. Reject unknown
or duplicate contracts before publication. New format3 files require a compatible importer;
older-client interpretation is not accepted by this work.

Generic mapped titles/notes are literal source text. Trim category padding for name matching
without removing an apostrophe. Existing explicit date/amount/currency/sign interpretation stays.
No schema, compiled model, permission, SDK, network, entitlement or backup-version change. Export
consent/notes redaction and all financial relationship invariants remain unchanged.

## Managed evidence

Forty distinct new cases pass across selective executions:34 Core and6 original App-source flows
with isolated real SQLite. Twenty existing related CSV cases pass; only the affected existing
category-rule case is selected again after generic text changes. No full unrelated suite rerun.
Preserve all11 reproduced failing cases and intermediate compiler/analyser refusals, including
the missing literalText local, cancellation-token diagnostics and duplicate version local.
Final named-marker/versionless and retained-review flows pass. Test output is cleaned afterward.

## Actual native evidence so far

Windows initial run28489a9f3c834455ba823200886a134f/process14456 makes one original native
own-ledger import and one holdings import. Five actual application-window captures are reviewed
in fa/Dark. Complete literal text, target account, balance99/200EUR and holdings3units/basis30EUR
match. A fictitious picker/consent port is explicit; this is not native chooser/authentication.

Samsung original run9bbec9790ce2496a854660f552b0742d/comparison0484050d07144f0f9398c63b07c6658f/
process17406 passes the same two cases in en/Light. Restore the old independent APK/stopped state
and Main25043/Home979/all141 public tuples/APK/OS; two owned remote XMLs are hash-checked/removed.

Windows final rund967dd32ad1f4db4a129bbbe94c09647/process20712 reuses the accepted own source
read-only with a shifted named marker, then makes only the previously unexecuted generic import.
Five additional native captures are reviewed. Source/base equality, original title/note, account,
amount1EUR and balance99EUR pass. The two earlier imports are not replayed. Both independent
Windows desktops leave the owner's input desktop Default unchanged.

The first physical final continuation20634 refuses at the original fresh-root guard before any
native input or financial action. Its failed packet remains immutable. The repaired explicit
continuation requires the prior accepted comparison receipt; ordinary fresh execution retains
the original guard. The owner personally closes Play Protect while the original restore request
is pending. That same install completes; no duplicate install, scan consent, bypass or system
setting change is made for the dialog.

Final Samsung continuation619983f532b541a88201a2f0b16e3dd9/process24102 completes the read-only
named-marker case and only the previously unexecuted generic import. Original source/base and
literal text/financial meaning match. Native buttons are used; picker/consent ports are fictitious.
The retained independent APK/stopped state and Main25043/Home979/all141 public tuples/APK/OS
are restored. A transient focus guard refuses the final restoration capture; only that missing
observation continues after owned Main focus is confirmed, without another install or import.
Two additional owned remote XMLs are hash-matched/removed; four in total, with local originals
retained. No accepted money, seed or successful native scene is replayed. D-494 remains24914,
with all four selected resumed alarm identities unchanged and no delivery before its deadline.

## Final builds prepared

Final strict Windows build: zero warnings/errors. Complete signed Offline shipping APK has
384AOT modules, ten unchanged native engines, v2/v3, no INTERNET/debuggable declaration and
408 ZIP/ELF libraries passing static16KiB alignment. SHA256:
`a07fb93cbe86949c5b4aa07e0fa9089a462748c3f3295f23eb3569ff0bbb23f9` (96,984,991bytes).
Final guarded ARM64 caller:192AOT/five unchanged engines/v2/v3/Offline, SHA256:
`3eb48460f906310cc9ec4161fba508ac9aa49b39614410e8baea464422d2dca8` (56,590,854bytes).
Static alignment does not establish API24/16KiB runtime acceptance. SDK terms remain pending.

D-494 remains an independently preserved interrupted/resumed timed cohort; do not replace,
force-stop, rearm or query its provider. D-496 keeps its inactive future-day source. Security,
stability, real authentication, accessibility, actual bank specimens/corpus, iOS/provider/Store
and publication gates remain separate. Completed source/native evidence does not close the
future-day cohorts or conditional no-reset Main adoption. Check the exact pushed head once.

The single delayed265d1460 response reports CI39988941106 and CodeQL39988940793 success; its
ci-physical-relocked-d498 monitor is consumed/deleted. No second request is sent.
