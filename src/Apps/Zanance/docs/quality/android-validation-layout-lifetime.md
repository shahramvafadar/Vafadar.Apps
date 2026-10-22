# Native validation layout lifetime (D-480)

## Observed defect and common correction

The original transaction, plan, occurrence and settlement pages use the same one-second native
validation-layout wait. They attach a GlobalLayout event wrapper to the initially captured
ViewTreeObserver and remove it only if that initial observer remains alive. Android attachment
can merge its listeners into another observer and kill the initial one. The existing D-450
toolbar correction does not cover these four waits.

On 23 October 2026 an independent native-only Samsung ARM64 control reproduces that exact
production pattern. Register before actual root attachment, request layout and wait for completion.
The initial observer is dead, the current observer is alive, and the old cleanup therefore skips
removal. Three subsequent actual native RequestLayout operations cause three additional retired
callbacks. This is an observed listener-lifetime defect, not evidence of the historical ANR cause.

AndroidValidationLayout now owns an explicit separate Java listener. Finally neutralizes its
completion action and removes that exact identity from the view's current live observer. The same
one-second bound, native RequestLayout, completed-task result, surrounding form/version guards and
existing validation/financial rules remain. All four original Android branches delegate to this
shared wait. Windows/iOS branches, text, colors, layout, monetary parsing and schema are unchanged.

## Original-source native regression evidence

The independent control compiles the actual shipping listener/waiter, with real native views and
no MAUI substitutes. Fixed native PID20874 establishes actual initial-observer retirement again.
After current-tree Retire, all three fresh RequestLayout operations leave old callback count0,
instead of3. The actual shared WaitAsync completes on an attached view; an unattached real view
returns false at its existing bounded timeout. These are three finite native regression checks,
not additional managed-suite cases or general runtime/stability acceptance.

Retain the real failed first fixed-helper launch20518: JNI reports UnsatisfiedLinkError for
LayoutActivity.n_onCreate before the scenario starts. No successful case is replayed. Clean only
that owned helper's output and rebuild; failed APK a3f5c211 and rebuilt7e1b7b69 differ. The rebuilt
native package runs the fixed cases. The internal registration cause is not established and the
failure is not relabelled as a production fix. The committed helper adds configuration/identity
build guards and an explicit source-type alias after a project-root namespace collision fails
compilation. Its final strict build succeeds; Debug and Main ApplicationId both refuse. Do not
repeat the unchanged admitted native cohort for these build-only guard/namespace changes.

The helper is pro.vafadar.zanance.layoutreview, API33/36, ARM64, embedded runtime, no AOT request,
no declared permission, no secrets or Main project reference. Its fixed explicit Activity run
creates only native fictitious controls. Verify its complete APK, signature and no-permission
manifest before installation. Remove only that newly installed package after verifying its exact
installed hash. Main16655, all141 original public Home tuples, OS and previous Main APK are equal
after this independent probe. No profile/account, authentication, launcher or private store/key
is touched. The committed repeatable contract is in eng/tests/AndroidValidationLayout/README.md.

## Final shipping builds, actual form and restoration

Strict Windows build succeeds without warnings/errors. The standard Build-AndroidApk.ps1 -Offline
produces the complete signed trimmed Release Main package,96,952,223bytes,384 AOT libraries:

```text
E:/Zanance.ReviewBuilds/D480-shipping-package/pro.vafadar.zanance-Signed.apk
SHA256 8244677eee328f3dfb3503739feafd1f3961969f38d602bb9e0303d4438b5980
signer 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b
```

ZIP integrity, v2/v3 signature, permanent identity, both ABIs and absence of INTERNET/network-state
permissions pass. Upgrade the original Samsung Main without reset. Startup initially remains
covered; continue its actual21723 instead of relaunching. Ready Home matches all141 original tuples
and979EUR. One original Expense form is opened without seeding or preparing another financial case.

Preserve an input-controller failure: the first intended Save coordinate reaches the still-visible
keyboard and enters # in the amount; it does not invoke Save. Dismiss that keyboard once. A first
observer wrongly treats the IME's retained mIsInputViewShown flag as current window visibility;
another expression has an escaped-whitespace typo. Both refuse before input. Continue the same
draft only after actual mImeWindowVis0 and mInputShown=false, with an explicit invalid-amount guard.
The first actual Save rejects # and reveals the original amount field/error through the changed
native waiter. Exact error is 'Enter a valid amount, for example, 12.50.' at[93,434][988,482].
Cancel and native Discard retire that same dirty draft. No valid financial Save, account/profile
creation or payment occurs. Other three callers share the compiled native adapter; this bounded
phone action does not claim a fresh complete four-form/language/theme matrix.

Final Main21723 has the expected new APK hash, unchanged OS and exactly the original141 public
Home tuples/979EUR. Twenty owned remote XML captures are removed only after exact local/remote SHA
checks. Local native/failed-launch/input-refusal/cleanup receipts remain private. The finite new
Main event window records zero ANR/crash events; the independent20518 failure remains preserved.
Public Home equality is not whole-database equality. No managed/resource/business/financial suite
or prior successful widget/report/phone matrix is repeated. Managed baseline remains5097 including
25 historical CI skips. General GC/ANR/crash, whole-Home speed, natural low-memory, spoken/OS-scale,
all device/platform workflows, iOS/provider/Store and release gates stay open.

Private receipts: probe-result.log, fixed-probe-first-failure.log, fixed-probe-result.log,
native-layout-proof.json, committed-probe-build-final.log, refusal logs, shipping-package-proof.json,
expense-errors-visible.xml, final-main-proof.json and capture-cleanup.json.

## D-481 original remaining caller continuation

[Remaining native callers](validation-layout-callers.md) completes plan, occurrence and settlement
invalid actions in an independent x64 original-source app without repeating this native observer
cohort or Samsung transaction case. Source/base named groups remain equal and financial Saves0.
Final full receipt recovery reports all3 retained, NativeActionsThisPass0. Current constructor,
loader, command-lifetime and footer observer corrections retain their original failures. Main,
OS and prior independent APK are restored. Shipping-source CI/CodeQL on0e8c6fa6 both succeed;
the consumed monitor is deleted. Broader stability/matrix/platform/release limits above stand.
