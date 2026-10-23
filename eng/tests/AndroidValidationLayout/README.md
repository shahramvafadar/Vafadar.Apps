# Original native validation layout lifetime

This independent ARM64 Release package compiles the actual AndroidValidationLayout source used by
the transaction, plan, occurrence and settlement pages. Main never references this project. It has
no MAUI substitutes, financial algorithms, source database, secrets, network or declared permission.
The fixed package is `pro.vafadar.zanance.layoutreview`; Debug/Main-identity builds refuse.

```powershell
dotnet build eng/tests/AndroidValidationLayout/AndroidValidationLayout.csproj -c Release -t:SignAndroidPackage -p:ContinuousIntegrationBuild=true -p:UseArtifactsOutput=true -p:ArtifactsPath=E:/Zanance.ReviewBuilds/D480-committed-layout
```

Verify the complete signed package and absence of permissions before installing only a newly owned
fixture. The Activity requires `review_run=d480-fixed-observer`. Its native views are fictitious;
it reads no Main private data or keys and makes no OS, launcher, account or authentication changes.
Do not repeat an accepted run merely to repair reporting. Keep the initially reproduced old event
wrapper failure separate from the fixed-source checks.

The original LayoutListener is registered before a real native root attachment retires its first
observer. After Retire on the current tree, three fresh RequestLayout calls must produce zero old
callbacks. The actual shared WaitAsync must finish on an attached view and return false within its
existing one-second bound on an unattached view. Native text/log output reports only these counts
and booleans, never heap contents or financial values. Exact package/foreground guards precede
device input. Uninstall only this newly installed package, then verify original Main public UI,
OS and APK identity; remove only hash-matched owned XML captures.

This is a native lifetime regression check, not a new managed-suite count or general ANR/GC fix.
See [D-480 evidence](../../../src/Apps/Zanance/docs/quality/android-validation-layout-lifetime.md).

## Detachment cases (D-482)

Use full publish into fresh owned output, optionally overriding the default ARM64 RID with
-p:RuntimeIdentifier=android-x64 for the independent emulator:

```powershell
dotnet publish eng/tests/AndroidValidationLayout/AndroidValidationLayout.csproj -c Release -p:ContinuousIntegrationBuild=true -p:UseArtifactsOutput=true -p:ArtifactsPath=E:/Zanance.ReviewBuilds/validation-native -o E:/Zanance.ReviewBuilds/validation-native-package
```

The fixed original d480-fixed-observer mode retains its three normal checks. Mode d482-detach
runs five real native cases: registration after attachment, registration before attachment,
attachment/detachment before first layout, actual WaitAsync detachment and detachment with target
disposal. Mode d482-remaining runs only the latter four when the first accepted result is retained.
Never replay that first result to repair another observation. Read the exact process completion
and refusal logs; an Activity launch alone does not prove a case completed.

Collection checks use public Java ReferenceQueue/WeakReference, a disposed unregistered control,
live-root assertions and three process-local Java GC requests. Release only the measurement's
already-dead initial observer wrapper after Retire, so it cannot retain old merge arrays. Never
mutate a dead observer or inspect raw heap contents. Production callbacks stay on the linked original
source. No performance or general GC conclusion follows. Preserve independent JNI registration
failures before OnCreate as packaging failures, separately from native case results.

The complete helper is permission-free and newly owned; remove it after acceptance and restore
original Main public UI/APK/OS. D-482 adds five native cases, not five managed-suite tests. See
[detached listener evidence](../../../src/Apps/Zanance/docs/quality/detached-validation-listeners.md).
