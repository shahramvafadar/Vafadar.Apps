# Original receipt source choice across cover and retirement (D-710)

## Actual gap and complete source handoff

D-709 protects completed receipt recognition. Its original source-selection adapter
still awaited a source/help answer and then opened a camera/file picker without
checking the creating page or renewed access. Help could reopen a retired source
sheet. A valid answer should survive temporary app cover without asking again;
retirement must instead stop that old native source from opening in a new graph.

`ReceiptSourceChooser` now owns the original choice/help/native-source sequence.
The existing ordinary choice API uses that same implementation. The production
`MauiReceiptRuntime.PickAsync` captures the original native owner and enters the
actual AppLockService drain. After each awaited choice/help acknowledgement, the
next unstarted step checks the same owner and retains itself at the current drain's
head if covered. An answered choice or help acknowledgement is not repeated. A
retired owner completes cancellation and cannot later open a source or feedback.
The actual queued callback may finish after cancellation; it is still guarded and
its exceptions are observed. Source failures propagate once to the existing
covered session feedback. No new confirmation, permission, translation, financial
writer, recognition algorithm, dependency, schema, runtime or lock policy is added.
D-709's separate OCR/draft validation and presentation ownership remain unchanged.

## Focused evidence

Six new original-choice regressions fail before the operative change: retirement
before an ordinary dialog or direct file route, retired photo/file answers, late
help selection and retirement during help acknowledgement. For that baseline only
an unused optional current-owner callback is added; the original body is unchanged.
All six then pass. Eight additional real-AppLockService/SQLite cases cover selected
photo/file retention ahead of later input, help selected before cover, cover after
answered help, retirement before initial queued choice, retirement during native
help, cancellation of a retained choice and a single original-source failure.

All14 new distinct cases and15 affected existing source-choice/resource cases pass.
One new help-retirement case is strengthened to await an actual later drain witness
rather than the public queue's immediate return; only that changed case is rerun.
It passes. These are29 distinct cases, not30. The focused build is strict and has
zero warnings/errors. No complete suite or D-709/old receipt-financial success is
replayed; native source effects in these focused cases are explicit ports.

## Original native before/after

A fresh independent full-application caller uses API36/x64/4096. The baseline
copies of the original source chooser and native adapter match their recorded
pre-change bytes. The private interaction wrapper awaits a real original native
source-sheet answer, then holds only that answer before returning it to the original
adapter. It neither fabricates a selected file nor bypasses authentication. The
reviewed paths use the original Home command, source dialog, Android file picker,
app lock and actual original source. No camera, file content, OCR or editor Save
is needed to prove this boundary.

In PID3788/UID10216, one opening0 account and an independent fictitious app PIN are
prepared once; ledger and attachments remain empty. The reminder offer is declined
once. The original native file choice is answered once in each distinct case:

- Normal original root replacement occurs while that answer is held. Releasing
  the retired answer opens actual `com.google.android.documentsui` anyway.
- Actual original LockPage is visible and IsLockedtrue before the second answer
  is released. Releasing it still opens the actual Android file picker.

Both real windows have package/UID ownership evidence. Only Back cancels each
picker; no file is selected. The complete22 typed source collections/records,
including original32-field ledger representation and attachments, plus four
catalogue values remain exact. These are two source-boundary failures, not an OCR
or financial result. The first picker also invokes the normal background cover;
its later IsLockedtrue alone is not used to prove the second case's prior cover.

One necessary signed no-reset upgrade continues the same UID, boot, account, PIN
and source in PID5874. Actual original fixture-PIN UI unlocks protected startup;
no authentication command port exists. Normal ShowMainShell restores Home without
repeating preparation. The original full source/catalogue match before and after.

The fixed distinct source cases prove:

- replacing the root cancels the pending command/busy lifetime; releasing its old
  answer leaves the replacement Home unchanged, with no native picker;
- after explicit cover, releasing the valid answer retains only LockPage and
  HomeBusytrue, with WorkCompletedfalse and the answered choice count unchanged;
- original native PIN unlock opens the actual Android file picker without another
  source sheet. Back cancels it, and normal original foreground/grace handling
  returns to Home. Choice count remains2 for the two distinct fixed cases.

Final Reads0/Opens0/Alerts0, actual busyfalse/work terminal, all22 typed source and
catalogue exact. There is no financial Save, receipt read, chosen file, Import,
Undo, accepted historic native replay or Main-package action. Native photo/help
variants are not separately qualified here; their continuation has the focused
port/service evidence above. Physical/iOS and broad receipt-corpus gates remain.

## Observation corrections, packages and exact retirement

The initial native startup guard refuses StartupPage before any preparation. The
same live PID3788 continues until real onboarding; no restart or preparation replay.
An age-only host diagnostic implicitly reparses a DateTime through culture-sensitive
text and swaps day/month. It issues no request or device action. The actual single
CI check instead uses the numeric automation timestamp, avoiding that parse.
The fixed review wrapper also forwards unused optional recovery-file capabilities;
those unrelated capabilities are not exercised or claimed as native acceptance.

Strict Windows and complete offline Android builds finish with zero warnings/errors.
Shipping SHA256 is
`09140d864f9c379e2bebf1022d427a78b0b0592d15be7d7ac12c4153476d9d9c`,
99,348,383bytes:384ownAOT/408ELF/1608static16KiBLOAD segments, v2/v3 signature,
CRC/ZIP16KiB and no private source-provider authority in its manifest. Used private
baseline `283a6f39` and fixed `dd145e85` each have192ownAOT/204ELF/804staticLOAD
segments and offline/non-debug/v2/v3/CRC/ZIP checks. Static alignment is not physical
16KiB acceptance or whole-assembly byte identity.

Original OS values, boot, locale, size/density, source and Home are exact. Only fresh
UID10216 and21 owned hash-matched XMLs/372,931bytes are removed. Actual host74660/
qemu66504 and console5572 close; official fresh-AVD deletion retires25files/
5,367,804,975logical bytes. Four scoped standard Cleans pass with3,545 registered
deletion lines. These are logical counts, not measured host free-space recovery.

One supplementary nine-target deletion is rejected before process creation with
`blocked by policy`:949files/342,213,714logical bytes remain frozen. Scope: this
batch's private source root, focused-test outputs, residual owned-AVD root and six
unused publication files. No retry/subset/alternate/move/escalation/reuse is allowed.
Used signed APKs, named source archives and evidence are retained outside that
scope; all earlier refused scopes remain untouched.

D-709's sole check at actual16.1022minutes reports
[CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41250737317)
and [CodeQL completed successfully](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41250737192).
Its schedule is deleted with no second request; later completion/cancellation is
not assumed. D-710 receives its own final exact-head check after the complete push.
Phone/D-607's three deadlines, consumed reboot, unread post-reboot source and Main
adoption condition remain untouched. Wider DB/key/security, stability, two-second
Home, accessibility, iOS, provider, Store and release gates remain open. Overall
delivery remains active.
