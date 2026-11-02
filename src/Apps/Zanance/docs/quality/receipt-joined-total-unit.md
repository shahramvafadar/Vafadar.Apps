# D-783 - Exact joined receipt total and currency evidence

## Real defect and complete boundary

2 November2026. D780's original native OCR returns exactly:

```text
Date: 28/10
BREAD EUR 7.10
TOTALEUR 7.10
```

D780 correctly rejects the date as merchant metadata but its amount remains NotFound:
the exact joined total/currency token has no whole-word separator. Preserve that
historical result and its evidence. D783 changes this specific lexical boundary,
without fuzzy keywords, substituting an item price or inventing an unknown currency.

In a private matching/amount view only, separate a word whose prefix is an exact
existing strong total word and whose suffix is a complete known three-ASCII-letter
ISO currency code. Retain existing invariant case/label-accent folding. A following
letter, digit or connector cannot hide an incomplete unit boundary. A complete
strong word must match; SUBTOTAL, SUMNE, TOTALENERGIES, unknown codes and compound
names are not partially repaired. Existing multiword phrases ending in a recognized
strong word keep their ordinary surrounding-word semantics. This does not claim
recovery of every joined phrase, currency symbol, damaged label or arbitrary OCR.

Original tax/subtotal/nonmoney/payment exclusions, signed/damaged-number review,
currency/precision safeguards and explicit financial review remain authoritative.
The original evidence rows retain the joined text; the input document/image is not
rewritten. Merchant matching uses the same lexical view so a standalone joined total
cannot become a merchant header. Purchase-date/year inference, image framing,
financial model/writer, source selection, permission, SDK/runtime/GC and privacy
choices do not change. There are no new UI strings or layout controls.

## Reproduction and selected managed checks

Before the fix,25 new selected cases yield14 actual failures and11 passes. They cover
six target currencies, German/Italian words, existing phrases, exact evidence source,
unknown/partial joins, original exclusions, damaged/signed review and no false merchant.
After the fix all25 pass. The retained actual D780 text regression is deliberately
updated for the new behavior and passes once separately; the historical report is not
rewritten. Another118 affected existing total/unit/accent/French/Italian/Spanish
cases pass. The result is144 distinct selected passes:25 new and119 affected existing,
not a full-suite result or144 new tests. No unrelated successful suite is repeated.

An exact-edit script initially refuses a mismatched merchant-condition anchor before
writing any product file. Its surrounding shell mistakenly proceeds with a no-op
build; that log is retained and is not corrected-source acceptance. Correct the
anchor and require the edit exit before the next build. A mixed class/method filter
then executes0 tests; refuse it rather than counting exit0 as success. Separate the
previously unissued class and exact-method selections, which actually run25 and1.
A later documentation title lookup refuses after prior edits; only its unfinished historical note is then applied, without replaying checks.
These are harness failures, not discarded product-test failures. Selected summaries
show zero errors/skips/not-run and the actual intended nonzero case counts.

## Strict packages and original native outcome

Corrected focused build and Windows Release build pass with0 warnings/errors.
The official full offline Android build succeeds without compiler warnings; actual
ZIP CRC, v2/v3 signature, nondebuggable/offline policy, ZIP16KiB alignment and every
static ELF LOAD are checked. Full shipping has384AOT/408ELF/1608LOAD:

- SHA2568c7dfe7904ad323777519aaf6184906892a704566750acb5e732f48a09e501a6,
  99458975bytes. It is not installed on Main or the real phone.
- The independently signed native caller has192AOT/204ELF/804LOAD, SHA256
  e41263b17e745f48123648ff648a1f4227bc6e13bdadcb4a3c39a76499656ad5,
  56983619bytes, with the same package integrity/policy checks.

Actual x64 Mono SHA256 remains
edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4.
Shipping ARM64 Mono remains
f03c6356b2412f4879005e8877c785441bf6380e6cd79700a39c0e7264bb4542.
Static16KiB alignment is not hardware16KiB/runtime or ARM64 execution acceptance.
Linking the same source is not a claim of whole trimmed Core/Data PE byte identity.

One fresh original-startup caller on API36/x64/ranchu/4096, UID10216/PID4054,
boot e048c391-4bbd-4f5b-90f9-4d3b59ac4520, performs one original
DocumentReader.ReadDocumentAsync OCR and changed ReceiptParser.Parse. It reads the
exact retained16303byte PNG, not a new camera capture or rewritten source image.
Raw OCR text is exactly equal to the historical output above. The actual result is
Found/7.10/one candidate/MinorAcceptedtrue/minor710, with Date/Merchantnull and
Ambiguousfalse. The original account/ledger/setup/Save/auth/editor/picker/camera
flows are not run. German consumption remains false; its old success is not replayed.
This is the changed original OCR-to-parser boundary, not full picker/editor/financial,
phone-camera, corpus or general OCR acceptance.

## Preservation and cleanup

All20 original typed source collections match before/after; account and ledger stay
empty. The original Onboarding root, same PID/boot, five OS values,320x640/density160
and reverse state remain exact. The helper's specialized result DTO has no Root
property; the generic host's printed Rootnull is not an actual lost-root assertion.
Actual state/source observations retain OnboardingPage.

Uninstall only the fresh package and prove absence. Launcher81988/QEMU79200 exit,
port5586 closes and the official scoped AVD deletion succeeds. At the stopped
pre-delete inventory its24 files total4579865843logicalbytes; this is not measured
host free space. Shared ADB5038/PID66176 stays alive. Emulator startup's missing
update-check.ini warnings are retained; they are not compiler-warning-free claims.

Four standard project Clean operations succeed with0 warnings/errors. Signed used
APKs, original/private source copies, inputs and complete evidence remain local.
Small project/restore/publish residuals are retained; no supplementary deletion or
old frozen scope access/retry/subset/alternate/move/escalation/reuse occurs. Owner
untracked files and the physical phone remain untouched. D752 still waits for owner
participation; D749/D751/D607 stay terminal. Original native-fault origin, two-second
performance, wider security/accessibility/platform/provider/Store/release gates and
the overall goal remain open.
