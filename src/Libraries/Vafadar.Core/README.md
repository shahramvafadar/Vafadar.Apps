# Vafadar.Core

Foundation types used by every app and library. No dependencies besides .NET.

| Type | Purpose |
|---|---|
| `Domain.Entity` | Base class for entities: `Guid` version 7 id, equality by type + id |
| `Domain.IAuditableEntity` | `CreatedAt` / `UpdatedAt` (UTC), set automatically by Vafadar.Data |
| `Hosting.IAppEnvironment` | App id, name, version, device name, platform (MAUI implementation in Vafadar.Maui) |
| `Hosting.StaticAppEnvironment` | Fixed `IAppEnvironment` for tests, tools and web hosts |
| `Settings.ISettingsStore` | Key/value user preferences (MAUI implementation uses `Preferences`) |
| `Settings.InMemorySettingsStore` | Non-persistent store for tests |
| `Dates.LunarHijri` | Lunar Hijri dates: Umm al-Qura for 1318–1500 AH (1900–2077), the tabular calendar outside |
| `Text.Digits` | Persian and Arabic-Indic digits to ASCII for parsing, ASCII digits to Persian for display |
| `Text.SearchText` | Text for comparing, never for storing or showing: `Normalize` removes keyboard differences only (digit script, Arabic Yeh and Kaf, half-space); `Fold` is the broader search key that also ignores case, accents and Arabic-script spelling variants |

```csharp
public sealed class Account : Entity, IAuditableEntity
{
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Settings keys are namespaced by feature: `localization.language`, `backup.lastBackupAt`, `finance.defaultAccount`.

## Comparing text

```csharp
// Stored patterns and "name is already used" checks: only keyboard differences are ignored.
SearchText.Normalize("  كيك ۱۲  ");   // "کیک 12"
SearchText.Normalize("Café");          // "Café"

// Finding text: compare the keys with StringComparison.Ordinal.
SearchText.Fold("Café Müller");        // "cafe muller"
SearchText.Fold("رئیس") == SearchText.Fold("رييس");   // true
```

`Normalize` does not change: apps rely on what it keeps. Known gaps and what `Fold` does in detail:
[shared libraries backlog](../../../docs/libraries-backlog.md).
