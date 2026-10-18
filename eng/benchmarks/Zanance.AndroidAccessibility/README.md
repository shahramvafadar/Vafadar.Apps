# Service-preserving Android accessibility observations

This independent shell-only observer uses the installed Android framework and never enters the
app dependency graph. From the repository root, run `./eng/benchmarks/Zanance.AndroidAccessibility/Build.ps1`.
Use a fresh output directory. Java21, Android platform36 and build-tools36.0.0 must already exist.
Compilation uses Java11 bytecode, all lint warnings as errors, and DEX minAPI24. The reviewed host
is Android16; this does not qualify the entire API24+ matrix.

Transfer the resulting observer.jar to a unique owned /data/local/tmp path. Verify its SHA256,
then invoke through adb shell with CLASSPATH pointing at that exact file:
`app_process /system/bin pro.vafadar.zanance.diagnostics.AccessibilityObserver pro.vafadar.zanance snapshot`.
Use `events 15` instead of snapshot for bounded focus events. Verify the owned AVD
Zanance_Storage_Review on port5038/emulator-5570 before transfer or input. Decode output as UTF-8.
Finally hash-match and remove only that exact owned remote JAR. Do not install an APK or permissions.

Only one UiAutomation client may exist: await events completion before taking a snapshot.
Never restart the app to repair an observer failure. The observer explicitly connects with
[FLAG_DONT_SUPPRESS_ACCESSIBILITY_SERVICES](https://developer.android.com/reference/android/app/UiAutomation#FLAG_DONT_SUPPRESS_ACCESSIBILITY_SERVICES).
The installed stock wrapper calls no-argument connect(), verified in D-348. Do not use stock
uiautomator dump/events during TalkBack qualification: service suppression invalidates observation.

The observer has no input, activation, screenshot, clipboard, private-file or network operations.
It reads only the two allowlisted Zanance packages, redacts password text and bounds nodes, depth,
text, events and duration. Use only approved fictitious fixtures. Accessibility focus is distinct
from keyboard focus. Outside-owner/unavailable-root statuses are observation failures, not app
failures or acceptance. Event streams can miss focus changes; an empty stream is not proof of none.
Capture and exactly restore authorized settings separately. Check actual foreground before input.
Accessible metadata and waveform energy do not establish correct audible speech or financial behavior.
