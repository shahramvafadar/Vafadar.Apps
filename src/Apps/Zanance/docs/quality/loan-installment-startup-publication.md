# Original loan installment startup publication (D-949)

## Defect and resulting behavior

The original installment session publishes Busy and clears its previous Result/Error
before its first reviewed-source validation. A synchronous property subscriber can
retire and reactivate the same owner during these notifications. Before correction,
the retired command still acquires a source context; a throwing Busy subscriber also
escapes the cleanup lifetime and leaves Busy true. Three focused original failures
reproduce these boundaries without accepting a payment.

The startup publications now execute inside the existing try/finally lifetime and
recheck the captured owner revision/display/current command after each notification.
The original Result-then-Error order is preserved. A retired command ends before
source revalidation, and a notification failure releases Busy. The original reader,
commercial guard, native choice/confirmation, atomic writer and accepted-payment
receipt rules are retained. No Data/schema/model/migration/runtime/GC change occurs.

## Focused host evidence

Three new boundary cases fail against the retained original source: two unexpected
source acquisitions and one leaked Busy state. After correction all four new cases
pass, including a positive current-owner control that reaches original source
validation. These are four new tests, not seven; no successful old suite or financial
acceptance is replayed. Raw metadata supplies a new loan opening -25700 minor EUR,
rate12/installment4000 and a zero-opening cash account, with no ledger rows. Full
migrated-table snapshots remain equal. The context sentinel disposes the acquired
context and throws before opening a reader or reaching financial work. Actual test
PDB bindings retain both original and corrected source lineage.

Strict Release Windows and complete offline Android builds finish with zero warnings
or errors. Shipping APK SHA256 is
`c38842cef5210e44e23a029ecdc467f21bf5b642686bcf9cc2e6e5a8bf74ed6b`
(99573663 bytes),384AOT/408ELF/1608 static LOAD. The private original-app x64 APK is
`671ddcaece87ccaf2bab5e22430176cd6a7262f16c8295ebba4745e1e6d990b3`
(56950690 bytes),192AOT/204ELF/804 static LOAD. Offline/nondebuggable, v2/v3 signature,
ZIP CRC and static16KiB checks pass. Four shipping and six private actual compiler
source documents match their archived inputs. This is not whole-PE or hardware16KiB
runtime identity. Shipping is not installed on Main while D944 is held.

## Original native continuation

One fresh API36/x64/4096 cohort uses PID4980/UID10216 and boot
`82f14298-766b-4eae-990e-e2161b06d34d`,320x640/density160. Two new metadata accounts
(loan -33900 minor EUR,rate12/installment4100 and zero-opening cash) have entries0/
plans0. Original Home/account reading completes. An independent positive control
proves the actual DI factory probe acquires/disposes/refuses one original context.
Its AsyncLocal scope is separate from the target command.

The original button is initially clipped below its owning inner ScrollView. The
observer refuses before any tap; the retained inverted bounds are not accepted as a
visible button or declared a product defect. Only one necessary native scroll on
that same page follows, without setup/control/route replay. Then one original Record
installment tap publishes session Busy. Its scoped callback invokes the actual
page.RetireShellCommands; the command ends with owner retired, scope bound/ended,
ReadContexts0, Busy false, no accepted loan/result/error and no confirmation dialog.
This is controlled original native page retirement, not a claim about OS Back timing
or a successful payment. Full20 typed source remains exactly
`576527C3B8966B55346E43003724E3373A240668B16AB441EEF772B239BBD234`.
Original root/source restore completes with entries0 and plans0.

## Restoration and retention

Three XML captures total100855 bytes. Each retained local digest/length matches its
remote original before exact remote deletion; local evidence remains. Only the fresh
package is uninstalled, absence verified, original five OS settings/locale key
presence/geometry/boot/reverse are exact. Own host43056 exits after normal console
stop. Official AVD retirement removes24files/4557714705logical bytes with zero AVD
files remaining; small scoped user-home residuals remain. Existing ADB5038/PID50972
creation identity remains exact. Five standard Cleans pass (Windows,private Android,
shipping Android,focused tests,compiler reader); no supplemental/frozen deletion.
Signed used APKs, source/PDB lineage and raw results remain under
`artifacts/d949-loan-installment-publication`.

D948's sole CI GET is actually at2026-11-09T11:27:31.6061769Z,1266.393seconds after
observed push (246.393seconds later than its planned17minute time):
[CI42606302285](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42606302285)
in_progress and
[CodeQL42606302157](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42606302157)
completed/success. No second query/final CI inference/rerun follows.

D944 physical PID7552/UID10439 and its unresolved once-issued navigation stay held
while the owner is away; no phone command or Main adoption occurs. Retained5606/Q02,
all terminal financial checkpoints and frozen scopes remain untouched. Original
native fault/ANR, whole performance/security/accessibility, iOS/provider/Store/release
and overall delivery remain open. Independent work continues without owner waiting.
