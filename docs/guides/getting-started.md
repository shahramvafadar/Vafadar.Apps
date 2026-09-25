# Getting started

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.401 or a newer 10.0 feature band | See `global.json` |
| .NET MAUI workloads | `maui-android`, `maui-ios`, `maui-windows` | Installed with Visual Studio's ".NET Multi-platform App UI development" workload, or `dotnet workload install maui` |
| IDE | Visual Studio 2026 (Windows) or VS Code with the C# Dev Kit and .NET MAUI extensions | Rider also works |
| Android | Android SDK (API 36) and an emulator or a device with USB debugging; JDK 17 | Installed by Visual Studio's MAUI workload |
| iOS (later) | A Mac with Xcode, or Windows + a paired Mac | Only needed when iOS work starts |
| Git | 2.4x+ | |

Check the setup:

```powershell
dotnet --list-sdks
dotnet workload list
```

## First build

```powershell
git clone https://github.com/shahramvafadar/Vafadar.Apps.git
cd Vafadar.Apps
dotnet tool restore                      # dotnet-ef
copy Directory.Secrets.props.example Directory.Secrets.props
# edit Directory.Secrets.props: add your Syncfusion license key
dotnet build Vafadar.Apps.slnx
dotnet test --solution Vafadar.Apps.slnx
```

Without a Syncfusion key everything builds and runs; Syncfusion controls show a license banner.

## Running an app

* **Visual Studio**: open `Vafadar.Finance.slnf`, set `Vafadar.Finance.App` as the startup project, pick the target
  (Android emulator, a device, or *Windows Machine*) and press F5.
* **Command line**:

  ```powershell
  # Android (emulator or device must be running / connected)
  dotnet build src/Apps/Finance/Vafadar.Finance.App -t:Run -f net10.0-android

  # Windows
  dotnet build src/Apps/Finance/Vafadar.Finance.App -t:Run -f net10.0-windows10.0.19041.0
  ```

## Where to go next

* [Architecture overview](../architecture/overview.md) – how everything fits together
* [Building](building.md) and [testing](testing.md)
* [Coding conventions](coding-conventions.md)
* [Adding a new app](adding-a-new-app.md)
