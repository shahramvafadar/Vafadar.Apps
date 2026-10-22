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
