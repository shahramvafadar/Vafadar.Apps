# Privacy matrix

One place that records, for every app, what data exists, where it goes and how it maps to the store privacy forms.
Update it in the same pull request as any change that affects data, permissions or SDKs.

Detailed, code-verified profiles per app: [Zanance](../../src/Apps/Zanance/docs/06-privacy-matrix.md).

Legend: ✅ yes · ❌ no · ⚙️ optional (user-enabled) · 🔜 planned · – not applicable

## 1. Data inventory

| Data | Zanance | Stored where | Leaves the device? | Who can read it |
|---|---|---|---|---|
| Financial entries (accounts, transactions, plans, budgets, categories, notes, tags) | ✅ | On-device SQLite | ⚙️ only in backup files / exports the user shares, and in OS backups | The user |
| Attachments (receipt photos, PDF files) | ⚙️ | On-device SQLite | ⚙️ only inside the encrypted backup file and OS backups; never in CSV/PDF exports | The user |
| Local profile names (only when more than one profile exists) | ⚙️ | On-device preferences, outside the profiles' databases | ⚙️ OS backups (Android Auto Backup, iCloud / computer backup) | The user |
| App settings (language, calendar, theme, display units) | ✅ | On-device preferences | ⚙️ OS backups (Android Auto Backup, iCloud / computer backup) | The user |
| Backup files | ⚙️ | On the device (last 10) and wherever the user shares them | ⚙️ only when the user shares one | The user (encrypted: only with the password) |
| CSV / PDF exports | ⚙️ | App cache, then the app the user picks in the share sheet | ⚙️ only when the user shares one (unencrypted, with a warning) | Whoever receives the file |
| Reminders | ⚙️ | Local notification service | ❌ | The user (generic text unless details are allowed) |
| Google / Microsoft account, OAuth tokens | 🔜 | – | – | Not in this release (cloud backup DF-04/05 is hidden) |
| Purchase status (Pro, tips) | 🔜 | – | – | Not in this release |
| Crash reports / analytics | ❌ | – | – | – |
| Advertising identifiers | ❌ | – | – | – |
| Location, contacts, photo library, microphone, camera | ❌ | – | – | – (attachments use the system file picker only) |

## 2. Permissions

Taken from the merged manifest of the release build; verify again on every SDK or package change.

| Permission | Zanance | Why |
|---|---|---|
| Android `POST_NOTIFICATIONS` | ✅ | Reminders; asked only when the user turns a reminder on |
| Android `RECEIVE_BOOT_COMPLETED` | ✅ | Restore scheduled reminders after a restart |
| Android `USE_BIOMETRIC`, `USE_FINGERPRINT` | ✅ (merged from AndroidX Biometric) | Optional app lock; the system dialog handles the credential |
| Android `INTERNET` | ⚙️ | Only in builds with cloud backup clients (D-35), for the user's own Google Drive / OneDrive; offline builds remove it (D-20); Debug builds add it for the debugger |
| Android `ACCESS_NETWORK_STATE` | ❌ | Removed from every build |
| iOS Face ID (`NSFaceIDUsageDescription`) | ✅ | Optional app lock |
| Anything else | ❌ | – |

## 3. Third-party services and SDKs

| Service / SDK | Zanance | Data involved | Notes |
|---|---|---|---|
| Google Drive API + Google Identity (Play services) | ⚙️ | Encrypted backup files; the account e-mail | Only when the user connects Google Drive (Android, D-35); scopes `drive.appdata` (own folder only), `openid`, `email`; tokens kept by Play services |
| Microsoft Graph (OneDrive) + Microsoft identity (MSAL) | ⚙️ | Encrypted backup files; the account name | Only when the user connects OneDrive (Android, Windows, D-35); scope `Files.ReadWrite.AppFolder` (own folder only); token cache on the device (MSAL storage, DPAPI on Windows) |
| Google Play Billing / StoreKit | 🔜 | Purchase status | Only if the app gets Pro / tips |
| Syncfusion controls | ✅ | None | UI components, run locally; the license is validated offline |
| Plugin.LocalNotification | ✅ | Reminder text | Local notifications only, no push service |
| Google ML Kit text recognition (Android) | ⚙️ | A receipt photo or scanned PDF page the user chooses to read | Runs on the device with a bundled model; in offline builds its usage statistics cannot be sent (no network permission); in builds with cloud backup ML Kit may send usage statistics to Google while online |
| Android Auto Backup | ✅ | App database and preferences | Operated by Google under the user's account; kept on by owner decision D-16 |
| iOS device / iCloud backup | ✅ | App data | Operated by Apple under the user's account (OS default) |

## 4. Google Play Data safety (draft answers)

Google's definitions decide what counts as "collected" (transmitted off the device by the app) and "shared". Re-check
the current Play Console help texts before submitting; when in doubt, declare conservatively.

| Question | Zanance (draft) |
|---|---|
| Does the app collect or share user data? | Offline build: No – the app has no network access, and files leave the device only when the user shares them through the system share sheet. Build with cloud backup: backup files are sent, at the user's request and encrypted with the user's password, to the user's own Google Drive / OneDrive – declare them per Google's current definitions (user-initiated transfer to the user's own account; the developer receives nothing); ML Kit usage statistics may count as collected by an SDK. Android Auto Backup is operated by Google and is not collection by the app; re-check Google's current guidance |
| Is data encrypted in transit? | Offline build: – (nothing is transmitted). With cloud backup: ✅ HTTPS, and the backup files are encrypted with the user's password |
| Can users request deletion? | ✅ "Delete all data on this device" in Settings (for the open profile), deleting a profile in More › Profiles; uninstalling deletes on-device data |
| Data shared with third parties | ❌ |
| Ads | ❌ |

## 5. Apple App Privacy (draft answers, for the iOS release)

| Question | Zanance (draft) |
|---|---|
| Data used to track you | ❌ None |
| Data linked to you | None collected by the app; cloud backup on iOS is not offered yet (D-35); re-check Apple's definitions at submission |
| Data not linked to you | None |
| Privacy manifest | `Platforms/iOS/Resources/PrivacyInfo.xcprivacy` (no tracking; required-reason API `UserDefaults` declared with reason `CA92.1`) |