# Adding a new app

This checklist creates a new app `<App>` (PascalCase, e.g. `Habits`) with the same structure as Finance. Copying
the Finance projects and renaming is the quickest route; the steps below list everything that must change.

## 1. Decide the identity (permanent!)

* App id: `pro.vafadar.<app>` (lowercase, e.g. `pro.vafadar.habits`). It becomes the Android package name, the iOS
  bundle id and the backup identifier, and **cannot change after the first store release**.
* Display names in all languages (en / fa / de).
* UI technology: XAML (default) or Blazor Hybrid if a web version is planned ([ADR 0003](../adr/0003-ui-technology.md)).

## 2. Create the projects

```text
src/Apps/<App>/
├── README.md, CHANGELOG.md, docs/requirements.md
├── Vafadar.<App>.Core/      (copy of Vafadar.Finance.Core: <App>App.cs with AppId + DatabaseFileName)
├── Vafadar.<App>.Data/      (copy of Vafadar.Finance.Data: <App>DbContext, design-time factory, DI extension)
└── Vafadar.<App>.App/       (copy of Vafadar.Finance.App)
test/Apps/<App>/
├── Vafadar.<App>.Core.Tests/
└── Vafadar.<App>.Data.Tests/
```

In the copied app project:

- [ ] `RootNamespace`, `ApplicationTitle`, `ApplicationId`, `Description`, versions (`0.1.0` / `1`)
- [ ] Namespaces in all `.cs` / `.xaml` files (including `Platforms/`)
- [ ] `AppResources*.resx`: `App_Name` and app strings in all languages
- [ ] Icon, splash screen and colors (`Resources/AppIcon`, `Resources/Splash`, `Resources/Styles/Colors.xaml`,
      `Platforms/Android/Resources/values/colors.xml`)
- [ ] `MauiProgram.cs`: `options.AppId = <App>App.AppId`, data registration
- [ ] Windows `Package.appxmanifest` display name
- [ ] Tests: app id test points at the new project file

## 3. Wire it into the repository

- [ ] `dotnet sln Vafadar.Apps.slnx add <all new projects>`
- [ ] Create `Vafadar.<App>.slnf` (copy `Vafadar.Finance.slnf`, replace the app projects)
- [ ] Add the non-MAUI projects to `Vafadar.Tests.slnf`
- [ ] Add the app project to the `app` choices in `.github/workflows/release-android.yml`
- [ ] Add the app to the tables in the root `README.md` and `docs/architecture/overview.md`
- [ ] Add a column for the app in `docs/privacy/privacy-matrix.md`
- [ ] Add the app to the issue templates' app dropdowns

## 4. Verify

```powershell
dotnet build Vafadar.Apps.slnx
dotnet test --solution Vafadar.Apps.slnx
dotnet build src/Apps/<App>/Vafadar.<App>.App -t:Run -f net10.0-android
```

Switch the language to Persian in the app and check that the layout mirrors correctly.

## 5. Before the first release

Follow the [release guide](release-and-publishing.md): store listing, privacy policy page, privacy matrix, signing,
OAuth consent screens for backup.
