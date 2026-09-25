# Vafadar.Backup.OneDrive

`IBackupStorage` for the **app folder** (`Apps/<app name>`) of the user's OneDrive (Microsoft Graph v1.0, plain
`HttpClient`, source-generated JSON – trimming safe).

* Scope: `OneDriveScopes.AppFolder` (`Files.ReadWrite.AppFolder`) – the app can only see its own folder.
* The folder is visible to the user in OneDrive, who can download or delete backups there.
* Simple upload (up to 250 MB); downloads use the item's pre-authenticated URL without sending the token.
* `401` → `AuthenticationRequiredException`; other failures → `BackupStorageException` with the status code.

```csharp
// A Microsoft IAccessTokenProvider must be registered with the Microsoft key (Vafadar.Authentication):
services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Microsoft, (sp, _) => ...);
services.AddOneDriveBackupStorage();
```

Setup (once): a Microsoft Entra app registration for personal and work/school accounts, as a public client with the
`Files.ReadWrite.AppFolder` delegated permission. See [authentication.md](../../../docs/architecture/authentication.md).
