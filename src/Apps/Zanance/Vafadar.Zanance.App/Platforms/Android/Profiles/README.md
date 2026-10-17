# Android cold-start AOT profile

`startup.aprof` is a Mono method profile captured from the actual Release Android cold-start path through
protected storage, access initialization and the fully loaded Home page (D-300). It supplements the SDK's
default profiles. It is a build input, not an application asset, database snapshot or production recorder.

The recording contains module, type and method metadata: 83 modules, 5,559 types and 24,170 methods in
3,104,542 bytes. It contains no recorded argument values, heap, credentials or financial records. Capture
used the existing fictitious review installation, SDK 36.1.69 and serviced .NET runtime 10.0.13. The temporary
recorder dependency and app-local execution environment were removed before the production candidate.

SHA256: `08fbdfe3e358a3bed5dabaebdbf25e9ecc03f6ff0f678ab27befb8160fc2e951`.

Mono's .NET 10 AOT compiler resolves profile entries by assembly/type/method signature; its reader discards
the recorded module MVID. Thus a new deterministic build identifier does not require rewriting tokens or
module identities. Removed or renamed methods may stop matching. After significant dependency/startup changes,
inspect compiler profile consumption and qualify a replacement against actual loaded Home, preserving the
original protected fixture. A profile does not establish a physical-device or two-second startup result.

Capture only on the dedicated owned emulator with an app-local `AndroidEnvironment` profiler property.
Do not use the SDK's default capture target, which writes a global device property. Collect only after actual
Home readiness, remove the dedicated forwarded port, remove the recorder/environment and build the ordinary
signed Release package. Never change system settings, screenshots, cryptography or access gates for capture.

References: [supported profile input](https://github.com/dotnet/android/blob/main/Documentation/guides/profiling.md),
[.NET 10 profile reader/resolver](https://github.com/dotnet/runtime/blob/v10.0.0/src/mono/mono/mini/aot-compiler.c).
Candidate runtime qualification and final file digest are recorded in the D-300 delivery document.
