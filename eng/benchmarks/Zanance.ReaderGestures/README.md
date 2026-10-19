# Independent original-reader gestures

This service-only Android package qualifies original nonfinancial navigation on the explicitly
authorized `Zanance_Storage_Review` emulator (`emulator-5570`, adb port5038). It never enters the
application dependency graph. The x64 package refuses non-ranchu/goldfish hardware; the external
controller additionally verifies the exact owned AVD before every input. Do not install or enable
it on Samsung or another project/device.

Build with the installed toolchain, without entering credentials:

```powershell
dotnet build eng/benchmarks/Zanance.ReaderGestures/Vafadar.Zanance.ReaderGestures.csproj -c Release -p:ContinuousIntegrationBuild=true
```

The SDK signs this independent package with its test signing configuration. The centrally serviced
runtime and AOT compiler/task remain matched. Verify the actual APK identity/signature, no
INTERNET/debuggable/profileable flags, protected receiver/service and original caption assets.
It has no Activity, screenshot, microphone, private-file or network operations. Its only data
source is bounded password-redacted native metadata from `pro.vafadar.zanance` in the foreground.

A newly installed service-only package is initially stopped. Explicitly start its owned receiver
once with `am broadcast --include-stopped-packages --receiver-foreground`, before enabling the
service. This opens no window and performs no input while the service is unavailable. The
receiver requires Android's privileged `android.permission.DUMP`; the service binding requires
`android.permission.BIND_ACCESSIBILITY_SERVICE`. Capture all prior accessibility/TTS/font/display
values first. Enable this service alongside the already installed TalkBack only within the
owner's emulator exception, and restore exact prior values in mandatory cleanup.

Send only explicit requests to
`pro.vafadar.zanance.readergesturereview/.CommandReceiver` with action
`pro.vafadar.zanance.readergesturereview.COMMAND`. Each request has a fresh32-hex `request` id
and one `operation`: `snapshot`, `next`, `previous`, `explore`, `scroll-forward`, `activate`.
Input additionally binds the freshly observed original `window` id. Explore/scroll/activation
carry an exact `caption` from the original six-language resources. The allowlist contains only
New expense, Cancel, More, About, Back and Home; it never contains Save or a financial commit.
Explore requires one original visible enabled44-unit target; activation requires its exact
actual accessibility focus. Next/previous are physical right/left swipes, not a fabricated focus
assignment. Scroll-forward dispatches a real two-finger upward stroke through the reader.

All input uses the public
[AccessibilityService.dispatchGesture](https://developer.android.com/reference/android/accessibilityservice/AccessibilityService#dispatchGesture(android.accessibilityservice.GestureDescription,android.accessibilityservice.AccessibilityService.GestureResultCallback,android.os.Handler))
API. It never invokes node click/focus/scroll actions, changes kernel permissions, uses root or
repeats the rejected ordinary adb injection route. The original focused package/window is
required; commands serialize and expire after ten minutes or256 requests per connection.
Decode chunked public `ZananceReaderGesture` JSON by its emission id and index/count. A successful
dispatch/callback is only transport evidence. Require actual reader focus and the original
page/modal transition afterward, preserving the same application PID across observation loss.

Keep this distinct from the shell-only read-only AccessibilityObserver. Never use stock
uiautomator while TalkBack is enabled. Retain completed original scene receipts and failed
controller observations; reveal an offscreen original target with reader scrolling rather than
resetting the page or replaying already completed scenes. Remove only this exact independently
owned package after settings restoration and terminal evidence. Focus metadata and recorded
audio energy alone do not establish correct audible speech, all-reader, physical/iOS or release
acceptance.
