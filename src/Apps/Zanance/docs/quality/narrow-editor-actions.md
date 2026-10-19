# Complete editor actions at the minimum width (D-364)

## Original actual-OS baseline

Continue the retained D-358 work-editor fixture, not a newly seeded replacement. The independent
AndroidHome caller now accepts actual360dp as well as412dp at OS font_scale2.0, captures both
values before review and checks that they do not change. Case receipts bind width, font scale,
source MVID, comparison and complete stored-input hash; a412dp receipt cannot accept360dp.
At360dp, missing original data/preparation markers fail rather than seed replacement files.

Original PID14652/comparison`b448eca5723b40c58b14939500442bfc` visits12 original account, debt,
entry, transfer, once/monthly plan, balance/earmark/share/cut goal, budget and settlement states
in en/fa/de xLight/Dark:72 cases,3282 native leaf controls and522 original choice names. Accounts,
entries, plans, settings, goals and budgets remain unchanged; no Save occurs. The original root is
`a56435d26dfc49d38b112da69cf1796a`; caller MVID`a3e115d8-3259-45f7-b7ba-22850b80ab16`.
Initial APK SHA256`2684ab1c91a067de6ea132543c2a8cb5dc8624c882cfa0e1d37f263c5d44a2c2`.

Four actual static-caption failures occur in German, both themes:

- Account icon chooser: native text wraps to two lines66px high inside a48px button,234px wide.
- Monthly pattern disclosure: two lines66px high inside a44px button,294px wide.

Both are the known native Button measurement behavior, now demonstrated at the required minimum
width. Use the original WrappingAction rather than shrink fonts or captions. Keep the existing
commands, visibility, translated icon open/close trigger, semantic colors and real native button.
The decorative48px account preview stays centered/square when its adjacent caption grows.

## Long editable values are a different boundary

The baseline also reports24 horizontal overflows for the unchanged30-character fictitious goal
name. Do not rewrite the single-line input or suppress the observation. Qualify its actual native
selection scrolling first, using [Android TextView's existing caret scrolling](https://developer.android.com/reference/android/widget/TextView).

PID15592/comparison`c0200d150dbb43f6bd7fc3cbadea25d5` checks only these24 flagged inputs.
Both native caret endpoints and first/last ink bounds from the actual bundled typeface remain
inside the original viewport, with complete native text and exact restored selection. The first
case scrolls from0 to46px; visible endpoint ink is6..18px and280..290px. Do not enable a new scroll
policy or replace the control. No text, amount, stored data, font or input behavior is changed.
The original horizontal warning remains in the detailed observation beside actual endpoint proof.
This accepts the specified fictitious editable value, not arbitrary bidirectional text or speech.

## Scoped corrected action checks

PID16077/comparison`a8073d4e85334901b63fd0fcf3ef93f0` checks only the changed actions in original
account, expanded debt details, monthly plan and first-account onboarding consumers:24 cases,
246 leaf observations,72 caption/state observations and48 actual native command clicks. Existing
command ownership, complete original native names, at-least44px targets and open/close restoration
pass without draft or stored-input changes. Original baseline private receipts verify the exact
same complete stored-input hash before continuation. The new onboarding consumer additionally
checks its original visible leaf text. No earlier passed412dp matrix or financial suite repeats.

Both focused continuations use APK SHA256
`bd040aaee016bb0e41946c7e51c2e674f122ab865c437f205cd4f8b5d6443922`.
Each completes before its original process is stopped and the captured font/size/settings restored.

## Independent Windows rendering and the observed footer follow-up

Run`1dc8708d2e7e4b11823bd1303d45a51f`, PID75452, qualifies the same four changed consumers in18
en/fa/de xLight/Dark x360/412/1280 contexts at process-local200% text:72 original consumer checks,
216 complete caption/state reads and144 original native UI Automation invokes. Draft and stored
inputs remain unchanged. All72 own-window PNGs decode and are visually reviewed; owner desktop
input, focus and full-screen capture are never used. The input desktop remains Default; the
independent review uses its own non-input desktop and fresh fictional package/storage.

The host's300-second observation expires while this exact process still makes progress. Preserve
it: all18 final context receipts/images subsequently complete and the original process is confirmed
absent. Its exit code is not retained by that expired controller. Do not rerun successful UI cases
to obtain a green wrapper exit. Restore the ordinary Windows build identity afterward.

Visual review of the original German360 first-account image reveals a related third defect:
the primary footer shows Loslege rather than complete Loslegen. This was outside the two original
action assertions, so successful action receipts do not accept that footer. Preserve the image.
Replace the fixed half-width footer buttons with original growing native-command surfaces in a
wrapping row. Whole words/commands reflow when needed; retain all three step captions, Back
visibility, original command bindings and busy disablement. First-step growth no longer needs a
Grid column-span trigger. Padding plus child margins retain the previous vertical breathing room.

## Footer continuation

PID16629/comparison`43bc06655f2547d6a0be23fa41366264` qualifies all three original wizard steps
in six actual OS200%/360dp contexts:18 cases. Each visible native footer has a complete one-line
word, original command/name and at-least44px enabled target. Busy preview disables Next and
restores its previous state. Back/Next/Finish are not invoked, so no account, settings write or
navigation is simulated as acceptance. Original stored inputs/hash remain unchanged. The APK
SHA256 is`deb79672f52dcf4512ebac80969f8ccb21715497ed4b2e2fa8f9fda3063526ec`.
The exact captured font1.0, physical412x800/no size override, density160/no override,
accessibility0, absent enabled-services and touch-exploration0 are restored after this process.

The original Windows footer process PID43532 retains five complete contexts before remaining
live on Home without publishing the next modal. A disjoint continuation PID45808 retains four
further complete contexts; its progress identifies original-modal-publication at fa-Dark-360.
Managed stacks are idle, and no original process is restarted after observation expiry.

The real constructor copies Light/Dark into ThemeIndex while its change handler reapplies the
already active theme. ThemeService then emits Changed, allowing App's queued tab-root replacement
to race the modal publication. A three-choice regression first passes System and fails Light/Dark
with one unexpected event. Refuse only a redundant unchanged choice in the original handler;
real user theme changes remain active. All three regression cases and the existing open-draft
language/theme case pass with strict compilation. These are three new managed cases, separate
from all native observations. The first corrected Windows continuation checks zero constructor
theme changes and retention of the original published modal owner in nine unfinished contexts.
Visual review then rejects nine earlier step2 images showing Home rather than the wizard, despite
their successful native footer assertions. Recheck exactly those rejected contexts; do not replay
the nine genuinely completed corrected contexts. Keep the wrong-page images and their original
apparently successful receipts as negative evidence.

Early separate-output build attempts preserve the actual copy-contention warnings and PRI failures;
a later attempt also collides with a retained process's apphost. Use the SDK's complete artifacts
output layout per original run, including each dependency's own output and intermediate paths.
Run-StoreDrafts now owns its full build tree under its run, so subsequent reviews never overwrite
another live review's mapped resources or ordinary development outputs. Do not accept failed
builds or suppress their warnings. The final continuation builds with zero warnings/errors.

## Final constructor and footer qualification

Windows runs`b6a2f6bc0ff549b285362cb40a98619e`/PID93864 and
`ff251915f4df4e76a6b726b55c3d6d08`/PID18832 complete disjoint nine-context cohorts:18 contexts,
54 original wizard steps,90 native command/name/target checks and54 actual own-window PNGs.
Every final case records zero constructor theme events and the original live modal owner; all
three-step contact sheets are visually reviewed and show the actual wizard, not Home. Busy
disablement and stored-input preservation pass without navigation-command or financial Save.
Both wrappers retain successful exit codes and zero-warning/error builds. Input desktop remains
Default. Only after this complete evidence, close the two superseded old review windows through
their native Window patterns; both original processes are confirmed absent. They were never
restarted or accepted as terminal successes after an observation deadline.

The final original Android constructor source is qualified in PID17418/comparison
`eb2c846e2ef24b94ac85dcedb0ced3af`:18 original wizard cases/30 native actions, no failures, same
baseline input hash and complete stored values. The original wrapper proves full native words,
command ownership and busy restoration without Next/Finish/Save. APK SHA256
`e2326a0de4f0e09ef83559af37faa9c5d1e371ab1f1ebf44701b31cf167d7b70`,192 AOT libraries and four
unchanged runtime/cipher engines are checked. Exact captured emulator font/size/density and
accessibility settings are restored after its terminal report. This rerun follows the newly
observed constructor defect; earlier unrelated editor/actions/financial suites are not repeated.

## Final ordinary package and restoration

Final complete signed offline APK:
artifacts/android-narrow-actions-final-d364/pro.vafadar.zanance-Signed.apk,94826229 bytes,
SHA256`2276294bdee5f8bbfae6890111d76a00311ea48c117ef44f1c7c0cacc72391fd`.
Verify signature certificate`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`,
min24/target36, ARM64+x64,384 AOT libraries, eight engine entries equal to D-363, no Internet
permission and no debugging/profiling flags. Upgrade the existing independent emulator main installation
without reset, PID17014 to17814. Immediate pidof is initially empty; continue the same initial
launch without reinstall or restart. Original Home returns with the complete51-node hierarchy
SHA256`2f53c71a57dffbebfc9954a0df1ca533ece7d5a0794fabb81edb48bfb1f155e4`, matching prior Main
Home0.00EUR/My account. No exact-PID AndroidRuntime error is observed. This is UI preservation,
not a full main-database comparison. Ordinary Windows identity is restored to pro.vafadar.zanance
with a24.29-second strict zero-warning/error build; test-output cleanup also completes cleanly.

Local immutable packages, all source/fixture-bound receipts, native input
endpoint coordinates, original failures and own-window images remain under artifacts/editor-narrow-d364
and their identified store-draft runs. No new managed unit-test count follows from the UI cases.

This is minimum-width editor/action qualification, not completion of every A11Y-03 control,
spoken TalkBack/VoiceOver, general GC/finalizer/ANR, two-second startup, whole-phone or release gates.
No Samsung input, OS Windows setting, new language, dependency, data schema or financial rule changes.
