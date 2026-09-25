# Vafadar.Maui

Shared .NET MAUI infrastructure for all apps.

| Type | Purpose |
|---|---|
| `Hosting.VafadarMauiAppBuilderExtensions.UseVafadar()` | One call that sets up Syncfusion (license + handlers), `IAppEnvironment`, preferences-backed `ISettingsStore`, localization, `TimeProvider` |
| `Localization.TranslateExtension` | `{v:Translate Key}` in XAML; updates when the language changes |
| `Localization.FlowDirectionExtensions` | Applies left-to-right / right-to-left to pages and windows |
| `Mvvm.ViewModelBase` | CommunityToolkit.Mvvm base with `IsBusy` / `IsNotBusy` |

```csharp
builder
    .UseMauiApp<App>()
    .UseVafadar(options =>
    {
        options.AppId = FinanceApp.AppId;                              // permanent, same on all platforms
        options.SyncfusionLicenseKey = AppSecrets.SyncfusionLicenseKey; // build-time secret
        options.ConfigureLocalization = l => l.Resources.Add(AppStrings.ResourceManager);
    });
```

```xml
<ContentPage xmlns:v="http://vafadar.pro/schemas/maui" ...>
    <Label Text="{v:Translate Settings_Language}" />
</ContentPage>
```

At startup the saved language is applied before the first page is created, and the flow direction of all windows
follows later language changes automatically.
