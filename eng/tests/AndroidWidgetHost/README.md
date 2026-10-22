# Independent original Android widget host

This native-only ARM64 Release helper hosts only the installed original
`pro.vafadar.zanance/pro.vafadar.zanance.QuickAddWidget` provider. It does not copy application
routes, construct financial drafts, replace PendingIntents, read private app data or change the
launcher, system settings, accounts or authentication. Main never references this project.
No application secrets or network permission enter the package.

Start its fixed HostActivity with one explicit `review_run` of 32 lowercase hex characters.
Use Add original widget and the actual platform consent for this single addition. Do not choose
always-allow. The native AppWidgetHostView inflates the original RemoteViews and the framework
delivers their original PendingIntents. A retained host/run/ID is reused without reallocation;
another run on the same installed fixture refuses. Unsupported providers/configuration refuse.

Only the native binding permission is declared; never grant it using shell privileges. This is
the ordinary [Android host consent contract](https://developer.android.com/develop/ui/views/appwidgets/host).
Native role/bounds evidence is distinct from a real action, cold origin, app-lock or spoken pass.
Use only known fictitious original profiles and Cancel all drafts without Save. Preserve initial
Main/public history/profiles/APK and OS values. Remove owned widget deletes only this package's
fixed host ID; verify owned=0 before uninstalling this newly installed helper. Never delete other
widget IDs or alter the owner's home-screen arrangement. Build/test output stays outside source.

Build the complete independent package with:

```powershell
dotnet build eng/tests/AndroidWidgetHost/AndroidWidgetHost.csproj -c Release -t:SignAndroidPackage -p:ContinuousIntegrationBuild=true -p:UseArtifactsOutput=true -p:ArtifactsPath=E:/Zanance.ReviewBuilds/D479-widget-host
```

Verify the actual manifest, native ABI, embedded assembly store and signature before installation.
Use one newly owned run, and reuse its admitted ID after each native Main return. For a planned cold
case, establish Main is cached and actually absent after a package-bounded background kill before
the first original widget action. Never use force-stop or a direct Main launch fallback. Cancel
each financial draft once; no Save is needed. Do not infer whole-store equality from public UI.

[D-479 physical evidence](../../../src/Apps/Zanance/docs/quality/physical-widget-pending-intents.md)
records four original actions, three Cancels, one actual cold origin, owned widget removal and
helper uninstall. It does not establish placement on the owner's launcher, all lock variants,
large-text/spoken accessibility or general stability. The existing D-174 emulator cohort stands.
