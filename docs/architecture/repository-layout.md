# Repository layout and naming

## Folder structure

```text
Vafadar.Apps/
├── .config/dotnet-tools.json      Local .NET tools (dotnet-ef)          → dotnet tool restore
├── .github/                       CI workflows, Dependabot, issue / PR templates
├── docs/                          Repository-wide documentation
│   ├── adr/                       Architecture decision records
│   ├── architecture/              How the system is designed
│   ├── guides/                    How to build, test, release, add an app
│   └── privacy/                   Privacy policy and privacy matrix
├── eng/                           Build infrastructure (MSBuild imports)
│   ├── AppSecrets.targets         Build-time secrets → AppSecrets class
│   ├── Maui.props                 Common settings of all MAUI projects
│   └── git-hooks/                 Identity guards (pre-commit, pre-push), see guides/git-setup.md
├── src/
│   ├── Directory.Build.props      Settings for production code
│   ├── Libraries/                 Shared libraries: Vafadar.<Area>[.<Detail>]
│   │   └── Vafadar.Backup/        … each with its own README.md
│   └── Apps/
│       └── Finance/               One folder per app
│           ├── README.md          App overview
│           ├── CHANGELOG.md       App release notes
│           ├── docs/              App-specific documentation (requirements, design)
│           ├── Vafadar.Finance.Core/
│           ├── Vafadar.Finance.Data/
│           └── Vafadar.Finance.App/
├── test/
│   ├── Directory.Build.props      Settings for test projects
│   ├── Shared/Vafadar.Testing/    Test helpers
│   ├── Libraries/                 Vafadar.<Library>.Tests (mirrors src/Libraries)
│   └── Apps/Finance/              Vafadar.Finance.<Project>.Tests (mirrors src/Apps)
├── Directory.Build.props          Settings for every project
├── Directory.Build.targets        Targets for every project
├── Directory.Packages.props       All NuGet package versions (central package management)
├── Directory.Secrets.props.example  Template for local secrets (the real file is git-ignored)
├── global.json                    .NET SDK version and test runner
├── nuget.config                   Package sources
├── Vafadar.Apps.slnx              The solution (all projects)
├── Vafadar.Finance.slnf           Solution filter: Finance app + libraries + tests
├── Vafadar.Libraries.slnf         Solution filter: shared libraries + tests
├── Vafadar.Tests.slnf             Solution filter: everything except MAUI projects (used by CI)
├── README.md, LICENSE, SECURITY.md, CONTRIBUTING.md, CODE_OF_CONDUCT.md
└── .editorconfig, .gitattributes, .gitignore
```

## Naming

| Item | Convention | Example |
|---|---|---|
| Brand / root namespace | `Vafadar` | `Vafadar.Backup` |
| Shared library | `Vafadar.<Area>[.<Detail>]` | `Vafadar.Backup.GoogleDrive` |
| App project | `Vafadar.<App>.<Layer>` with layers `Core`, `Data`, `App` (+ `Web`, `Api`, `Contracts`) | `Vafadar.Finance.Data` |
| Test project | `<ProjectUnderTest>.Tests` | `Vafadar.Finance.Data.Tests` |
| App id (Android package, iOS bundle id, backup id) | `pro.vafadar.<app>` – **permanent** | `pro.vafadar.finance` |
| Namespaces | Match folders | `Vafadar.Finance.App.Features.Settings` |
| Solution filter | `Vafadar.<Product>.slnf` | `Vafadar.Finance.slnf` |
| Web addresses | `vafadar.pro/<app>` or `<app>.vafadar.pro` | `vafadar.pro/finance/privacy` |
| Git tags | `<app>/v<version>` | `finance/v1.0.0` |

## Where does new code go?

| I am adding… | Put it in… |
|---|---|
| A screen or view model of one app | `src/Apps/<App>/Vafadar.<App>.App/Features/<Feature>/` |
| A business rule or entity | `Vafadar.<App>.Core` |
| A table, query or migration | `Vafadar.<App>.Data` |
| Something a second app needs too | A new or existing `src/Libraries/Vafadar.*` library (with tests and README) |
| A MAUI control or helper used by several apps | `Vafadar.Maui` |
| A UI string | The app's `AppResources*.resx` (all languages) or `SharedStrings*.resx` if shared |
| A test helper used by several test projects | `test/Shared/Vafadar.Testing` |
| Build logic | `eng/` (imported from `Directory.Build.*` or `eng/Maui.props`) |
| A decision worth remembering | A new ADR in `docs/adr/` |
