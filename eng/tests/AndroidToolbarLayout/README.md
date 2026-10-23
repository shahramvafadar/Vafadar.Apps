# Original Android toolbar observer lifetime

This independent permission-free Release package links the actual production
AndroidToolbarLayoutObserver source. Main never references the project. The native Toolbar,
Activity root and Dialog root contain fictitious labels only; there is no financial database,
authentication, network, OS configuration or MAUI substitute.

```powershell
dotnet publish eng/tests/AndroidToolbarLayout/AndroidToolbarLayout.csproj -c Release -p:ContinuousIntegrationBuild=true -p:UseArtifactsOutput=true -p:ArtifactsPath=E:/Zanance.ReviewBuilds/toolbar-native -o E:/Zanance.ReviewBuilds/toolbar-native-package
```

The fixed package is pro.vafadar.zanance.toolbarreview and the Activity is .ToolbarActivity.
Debug and Main-identity builds refuse. Verify the complete signed package and absent permissions
before installing only a newly owned helper. Mode review_run=d485 registers after attachment;
review_run=d485-before registers before attachment and qualifies the native floating-tree merge.
Each mode requires a first callback, zero callbacks from the outgoing live root while detached,
a callback after moving to a second actual window, and zero callbacks after retirement while
both window roots remain alive. Require accepted=True from the exact new process.

Retain each completed mode instead of rerunning it to repair observation. Remove only the new
helper, return to original Main, compare its public UI/APK/OS and remove only exact hash-matched
owned XML captures. Native cases are not managed-suite counts, general ANR/GC acceptance,
performance measurements, spoken accessibility or release evidence.

See [original toolbar transition evidence](../../../src/Apps/Zanance/docs/quality/toolbar-observer-transitions.md).
