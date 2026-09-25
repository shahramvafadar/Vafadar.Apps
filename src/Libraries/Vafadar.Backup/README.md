# Vafadar.Backup

Backup and restore of app data. Design: [docs/architecture/data-and-backup.md](../../../docs/architecture/data-and-backup.md).

| Type | Purpose |
|---|---|
| `IBackupSource` | Something that is backed up (e.g. `SqliteDatabaseBackupSource` from Vafadar.Data) |
| `IBackupStorage` | Where backups are stored: `Storage.LocalFolderBackupStorage`, Google Drive, OneDrive |
| `IBackupService` / `BackupService` | Create, list, restore, retention, "is an automatic backup due?" |
| `BackupManifest` | Content description inside every package (app id, version, entries + SHA-256) |
| `Security.BackupEncryption` | AES-256-GCM with PBKDF2-SHA256 key derivation and Persian-aware password normalization |
| `BackupFileName` | `{appId}_{yyyyMMddTHHmmssZ}.vbak` naming and parsing |
| `BackupException` / `BackupError` | Errors the UI can translate (wrong password, other app, newer version, damaged) |

```csharp
services.AddVafadarBackup(options => options.MaxBackupsToKeep = 10);

var file = await backups.CreateBackupAsync(storage, password: userPassword, cancellationToken);
var list = await backups.ListBackupsAsync(storage, cancellationToken);           // newest first
var manifest = await backups.RestoreAsync(storage, list[0], userPassword, cancellationToken);
```

Requires `IAppEnvironment` and `ISettingsStore` (provided by `UseVafadar()` in MAUI apps).
