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

```csharp
public sealed class Account : Entity, IAuditableEntity
{
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Settings keys are namespaced by feature: `localization.language`, `backup.lastBackupAt`, `finance.defaultAccount`.
