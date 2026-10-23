# Detached Android validation listeners (D-482)

## Observed defect and correction

The real native x64 baseline crashes in PID31509 after an attached ScrollView is removed from
its root: its current floating ViewTreeObserver differs from the original registration tree,
and both remain alive. Current-tree-only removal leaves the global callback on the original
root. Its next dispatch reaches the disposed LayoutListener Java peer and raises the observed
System.NotSupportedException/JavaProxyThrowable. This reproduces independently of Samsung.

AndroidValidationLayout now tracks the original tree and distinct trees encountered during
attachment/detachment. A separate attachment Java peer holds a weak listener owner. Detachment
ends the original wait with false; retirement neutralizes both callbacks and removes the global
listener from every tracked live observer before disposing its peer. The attachment peer is
neutralized and released through the native bridge rather than explicitly disposed inside a
potential native dispatch snapshot. Disposed targets remain supported. Preserve the one-second
bound, original caller ownership guards, field errors and financial behavior.

## Original native evidence, without replay

Five actual detachment cases pass against source SHA256
4140ced267b1408b91194d6c17cfd223aea34a4ec2505bd7123588ab257aa12c:

1. Register after attachment, detach and retire/dispose; original live root no longer retains
   the listener. Retain the successful PID31700 result.
2. Register before attachment, observe the merged tree, detach and retire/dispose.
3. Attach and detach before the first layout, with zero completion callbacks.
4. Detachment returns false from the actual shared WaitAsync.
5. Detachment followed by native target disposal also returns false.

Only the remaining four cases run in PID32039. Collection checks use public Java ReferenceQueue
and WeakReference, a disposed unregistered control, live-root assertions and three process-local
Java GC requests. They inspect no heap contents or private app data and change no runtime flags
or OS settings. The first pre-attachment collection observation refused because the measurement
retained its own dead original observer wrapper. Release only that already-dead measurement
wrapper after retirement and request a fresh root layout before collection checks; production
code stays unchanged for this observer correction. The earlier successful post-attachment case
is not replayed. Do not treat collection timings as performance or general GC acceptance.

An incremental independent package fails before OnCreate in PID31876 with n_onCreate symbol
registration failure. No case executes in that attempt. A clean of only owned native helper
output followed by full publish recovers; its internal incremental-registration cause is not
established. The final native source is promoted to eng/tests/AndroidValidationLayout with full
five-case and remaining-four modes; strict full publish succeeds. The same helper's three
affected normal checks pass in PID32356: merged callback retirement, attached completion and
unattached bounded timeout. These are native checks, not new managed-suite tests.

## Shipping integration and restoration

Strict Windows and complete Offline Android builds finish with zero warnings/errors. Final Main
APK is96960415bytes,384 AOT libraries, ZIP-verified, v2/v3 signed and no INTERNET permission,
SHA2565b6faeb08a09fbe8ada9e5e8e8c46b8c2c88e4412eea674a8217a730aea51512.

Samsung upgrades once without reset. A single blank-amount Expense Save displays the original
"Enter an amount greater than zero." error beside the original blank field; Cancel returns to
Main PID27601. All141 public Home tuples, original account/balance979 and recorded OS settings
match the baseline. No financial command is accepted, no profile is created, and no private
database/key equality is claimed. The first capture failure continues the same live form; an
observer expecting different error wording is corrected from the retained original XML, without
repeating Save. All8 owned XMLs are hash-matched and removed. Capture enumeration follows only
the /sdcard root symlink; the initial non-following enumeration found no files and deleted none.

Emulator Main remains PID28047 with all139 original public tuples, APK and OS unchanged; both
new native helper packages are removed and2 hash-matched XMLs deleted. No accepted D-481 caller,
financial fixture, language/geometry matrix or unrelated managed suite is replayed. This change
adds no schema, permission, SDK, network or financial data behavior.

This closes the reproduced detached-listener fault only. Historical Mono/SGen crashes, general
ANR/GC, whole-Home two-second performance, natural low-memory, full accessibility/spoken and all
other device/iOS/provider/Store/release gates remain open. Exact pushed-head CI is a separate
pending gate at commit creation; the preceding1c8d17d1 CI39874200053 and CodeQL39874200026 succeed.
