# Original receipt publication across access and page retirement (D-709)

## Actual defect and correction

The initial receipt unlock gate preceded asynchronous native recognition. A lock
arriving during that read did not guard the later empty-result notice, failure
feedback or original entry-editor navigation. The new-receipt Home command also
lacked an original-page owner. A queued access callback returns before execution;
using that return as completion released shared busy ownership prematurely.

`ReceiptReadingSession` now captures one original native presentation and keeps
busy ownership through the actual queued read, retained feedback and navigation.
Both new receipts and existing attachments use the original access queue. A result
completed while covered is retained at that drain's head, ahead of later queued
input. Unlock revalidates the same reviewed file and draft before publication;
recognition and an already answered empty notice are never repeated. Safe failures
are covered too. Retirement silently terminates waiting work and releases its
native subscriptions; no old callback adopts another page or error surface.

`MauiReceiptRuntime` captures the original Window/AppShell, current section,
navigation stack, pre-existing modal prefix, page and model. It permits only the
original temporary LockPage above that prefix. A modal cover can be exposed as
Shell.CurrentPage, so it is not mistaken for retirement. Successful navigation,
root/model replacement, foreign modal presentation and window destruction retire
the owner. Home refuses an already retired or non-current model before choosing a
source. Original lock FIFO, parser, native document reader, file/source validation,
attachment bounds, financial writer, schema, runtime, permissions and translations
are unchanged. This is no new authentication or recognition algorithm.

## Focused original-source regressions

Eight new real-SQLite cases use the actual AppLockService and ReceiptWorkStore:

- completed recognition after cover, retaining result order and busy lifetime;
- native-reader failure after cover, publishing safe feedback only after unlock;
- relock after an answered empty-result notice, without notice/OCR replay;
- retirement during successful recognition and during failed recognition;
- cancellation of an owner waiting for unlock, with no later replay;
- replacement of the original file while a result waits, preserving both sources;
- an already covered queued read whose owner retires before first recognition.

All eight fail against the original session before the operative correction. Only
unused interface hooks and the owner type had been added for compilation; this is
not a claim that every source byte was unchanged. The original session and native
runtime remained byte-identical for that baseline. All eight pass after correction.
The focused build has zero warnings/errors. The affected existing receipt-flow
class has34 distinct cases:33 pass initially; its one old expectation of zero lock
entries for an existing attachment is updated to one protected entry and that
single case then passes. The33 successes are not repeated. No whole-suite pass or
new historical test count is claimed.

## Original native application before and after

A fresh independent API36/x64/4096 caller uses actual original application pages,
AppLockService/LockPage, SQLite source, DocumentReader/ML Kit, layout and parser.
Its private source port creates a fictitious1200x1600 JPEG and passes it through
original bounded preparation. An explicit wait holds the actual completed native
OCR result, not invented parser output. No actual picker, camera or financial
acceptance is inferred from that port. Captured text is:

```text
D709 FICTITIOUS ACCESS SHOP
TOTAL EUR 24.50
```

One original account with opening0 and a separate fictitious app PIN are prepared
once. There is no ledger entry. The original reminder offer is declined once.
Baseline PID4885/UID10216 actually covers the held read with LockPage. Releasing
the result then opens EntryEditorPage above that cover while IsLocked remains true:
modal stack LockPage/EntryEditorPage, Reads1/Opens1/OpenedWhileLocked1. Native XML
shows the original New expense page and24.50 receipt text. All22 typed source
collections/records, including all32 ledger fields and empty attachments, remain
exact. Baseline HomeBusy queried through Shell.CurrentPage is not evidence about
the covered underlying Home model; the fixed observer retains that actual model.

One necessary signed no-reset update continues the same UID/boot/account/PIN/source
in PID5700. Protected startup still requires the real original fixture-PIN UI;
there is no unlock command port. After authentication, normal ShowMainShell restores
Home without replaying preparation. All22 source collections/records and four
catalogue values match the pre-read baseline exactly.

The fixed original read is held once, covered once and released once. It leaves
only LockPage visible: Reads1, Opens0, Alerts0, WorkCompletedfalse and the actual
underlying HomeBusytrue. After original native PIN unlock, that completed result
opens the original EntryEditorPage exactly once: Reads1/Opens1, OpenedWhileLocked0,
WorkCompletedtrue. Native receipt text remains exact. Original Cancel/Discard then
returns to Home without Save; the full source/catalogue are unchanged.

A second distinct pending read is held across a normal original root replacement.
Releasing it leaves the replacement Home untouched, Reads2/Opens1/Alerts0 and real
terminal busyfalse. Full source/catalogue remain exact. This is native acceptance
of the covered-result and retired-root boundaries; other page/tab/error/file-switch
branches have the focused service evidence above, not separate physical acceptance.
No accepted historical receipt, payment, Import, Undo or financial Save is replayed.

## Preserved failures and observation limits

Private preparation first failed with duplicate-using CS0105. A host variable
collided with a read-only system variable; two non-recursive copy attempts created
only two verified empty output directories, not copied personal files. Those exact
empty directories were removed non-recursively and the intended original files
were copied explicitly. The consequent missing-main-Home compilation errors remain
separate. The first fixed native compile then caught CS8602; capturing and checking
the actual Shell removed it before the successful package. None caused an install
retry or source/preparation replay.

Initial XML-based Unlock/Cancel taps did not reach visible controls: the IME
occluded Unlock and panned the editor header outside the actually visible surface.
Actual screenshots disqualify the initial inference that Cancel itself was broken.
After dismissing the IME, original Unlock and original Cancel/Discard succeed.
The original baseline locked-editor failure remains valid; a separate unconfirmed
Cancel-product defect is not claimed. Original attempts, source packets and the
observation correction remain retained.

## Packages, restoration and cleanup

Strict Windows and complete offline Android builds have zero errors/warnings.
The complete shipping APK is SHA256
`eb7eb1563e081f74b667b5d10f6bf0604e891f0f416f2c3edb3c7b17a79919bb`,
99,331,999bytes,384ownAOT/408ELF/1608static16KiBLOAD segments, v2/v3 signature,
CRC and ZIP16KiB checks. Its manifest contains no private receipt review authority.
The used baseline `a522792d` and fixed `918ff719` independent APKs each have
192ownAOT/204ELF/804staticLOAD segments and the same offline/non-debug/signature/
ZIP/CRC checks. Static alignment is not physical16KiB runtime acceptance.

Original boot, locale, size/density and all captured settings remain exact. Only
the fresh UID10216 is uninstalled;24 owned/hash-matched XMLs totaling404,671bytes
are removed. Actual owned host72880/qemu35200 and console5572 close; official
fresh-AVD deletion retires25files/5,292,504,111logical bytes. Four standard scoped
Cleans succeed with3,545 registered deletion lines. These are logical counts,
not measured host free-space recovery.

One supplementary nine-target removal is rejected before process creation with
`blocked by policy`:948files/342,165,852logical bytes remain frozen. The targets
are this batch's private source root, focused-test output root, residual owned-AVD
root and six unused publication files. No retry, subset, alternate method, move,
escalation or reuse as input is allowed. Used named source archives, signed used/
shipping APKs, source packets and failure evidence are preserved outside the
requested removal scopes. Previously refused scopes are untouched.

D-708's sole CI check is observed at an actual32.1639minutes: CI and CodeQL both
completed successfully; its schedule is deleted with no second request. D-709's
own final exact-head check is scheduled only after this complete batch is pushed.
Phone/D-607's three deadlines, consumed reboot, unread post-reboot source and
physical-adoption gate remain untouched. This does not accept all database/key
security, general ANR/crash freedom, two-second Home, wider accessibility, iOS,
provider accounts, Store or publication. The overall delivery goal remains active.
