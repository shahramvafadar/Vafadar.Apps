# Vafadar.Localization

UI language, right-to-left support, calendar selection and shared UI strings. Platform independent (MAUI XAML
integration is in Vafadar.Maui). Design: [docs/architecture/localization.md](../../../docs/architecture/localization.md).

| Type | Purpose |
|---|---|
| `AppLanguage`, `AppLanguages` | Supported languages (`en`, `fa`, `de`), native names, RTL flag |
| `CalendarSystem` | `Gregorian`, `Persian` |
| `ILocalizationService` / `LocalizationService` | Current language, calendar, optional region and first day of the week; persists the choice; applies cultures; `Changed` event |
| `Regions` | Region codes and names, and the conventional first day of the week per region (a region only suggests formats, never location) |
| `Translator` | String lookup (app resources → shared strings), refreshes bindings on language change |
| `Formatting.IDateFormatter` | Calendar-aware date formatting (`Short`, `Long`, `MonthYear`) |
| `Resources/SharedStrings*.resx` | Strings shared by all apps (`Common_*`, `Settings_*`, `Calendar_*`, `Backup_*`) |

```csharp
services.AddVafadarLocalization(options =>
{
    options.Resources.Add(AppStrings.ResourceManager);   // the app's own AppResources.resx
});

// startup
localization.Initialize();

// anywhere
translator["Settings_Title"];                         // "Settings" / "تنظیمات" / "Einstellungen"
translator.Format("Settings_Version", "1.0");         // "Version 1.0"
dateFormatter.Format(DateOnly.FromDateTime(DateTime.Today), DateFormatStyle.Long);
```

Requires an `ISettingsStore` registration (provided by `UseVafadar()` in MAUI apps).
