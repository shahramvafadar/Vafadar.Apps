# Vafadar.Backup.GoogleDrive

`IBackupStorage` for the hidden **app data folder** of the user's Google Drive (Drive API v3, plain `HttpClient`,
source-generated JSON – trimming safe).

* Scope: `GoogleDriveScopes.AppData` (`https://www.googleapis.com/auth/drive.appdata`) – the app can only see its own folder.
* Resumable uploads (no practical size limit), paged listing, `404` on delete is ignored.
* `401` → `AuthenticationRequiredException`; other failures → `BackupStorageException` with the status code.

```csharp
// A Google IAccessTokenProvider must be registered with the Google key (Vafadar.Authentication):
services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Google, (sp, _) => ...);
services.AddGoogleDriveBackupStorage();
```

Setup (once per Google Cloud project): enable the Google Drive API, configure the OAuth consent screen with the
`drive.appdata` scope and create OAuth clients for Android / iOS / desktop. See
[authentication.md](../../../docs/architecture/authentication.md).
