# 0006. Encrypted backups to the user's own Google Drive / OneDrive

- Status: Accepted
- Date: 2026-09-25

## Context

Data on a phone can be lost (lost device, reset, broken phone). Operating a backup server would cost money, create
privacy obligations and give the developer access to personal data.

## Decision

* Backups are single files written to cloud storage **owned by the user**: Google Drive (hidden app data folder)
  or OneDrive (app folder), with minimal scopes that only reach the app's own folder.
* The format is a ZIP package with a manifest and SHA-256 checksums, optionally encrypted with **AES-256-GCM** using
  a key derived from a user password (PBKDF2-SHA256, 600,000 iterations). The password is never stored.
* A restore validates the password, app id, app version and checksums before replacing any data; older backups are
  migrated to the current schema, newer ones are refused.
* Retention keeps the newest 10 backups per storage; automatic backups run when due (daily by default).
* Everything is implemented once in shared libraries (`Vafadar.Backup*`, `Vafadar.Data`) and reused by every app.

## Consequences

* No server, no cost, and the developer cannot read backups.
* Users need a Google or Microsoft account for cloud backup (local export always works without one).
* A forgotten encryption password makes that backup unrecoverable – the UI must warn clearly.
* Backup is not multi-device sync; that would be a separate decision per app.
* Google OAuth consent-screen verification is needed before public release.
