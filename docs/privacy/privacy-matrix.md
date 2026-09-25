# Privacy matrix

One place that records, for every app, what data exists, where it goes and how it maps to the store privacy forms.
Update it in the same pull request as any change that affects data, permissions or SDKs.

Legend: ✅ yes · ❌ no · ⚙️ optional (user-enabled) · 🔜 planned · – not applicable

## 1. Data inventory

| Data | Finance | Stored where | Leaves the device? | Who can read it |
|---|---|---|---|---|
| Financial entries (accounts, transactions, categories, notes) | ✅ 🔜 | On-device SQLite | ⚙️ only in backups / exports | The user |
| App settings (language, calendar, backup preferences) | ✅ | On-device preferences | ⚙️ Android system backup | The user |
| Backup files | ⚙️ 🔜 | User's Google Drive app data folder / OneDrive app folder | ⚙️ to the user's own account | The user (encrypted: only with the password) |
| Google / Microsoft account identifier and e-mail | ⚙️ 🔜 | On device, to show the signed-in account | ❌ | The user |
| OAuth access / refresh tokens | ⚙️ 🔜 | On device, secure storage | Sent only to Google / Microsoft APIs | – |
| Purchase status (Pro, tips) | 🔜 | Store + cached entitlement on device | Handled by Google Play / App Store | Store |
| Crash reports / analytics | ❌ | – | – | – |
| Advertising identifiers | ❌ | – | – | – |
| Location, contacts, photos, microphone, camera | ❌ | – | – | – |

## 2. Permissions

| Permission | Finance | Why |
|---|---|---|
| Android `INTERNET` | ✅ | Cloud backup (Google Drive / OneDrive) |
| Android `ACCESS_NETWORK_STATE` | ✅ | Skip automatic backups while offline |
| Anything else | ❌ | – |

## 3. Third-party services and SDKs

| Service / SDK | Finance | Data involved | Notes |
|---|---|---|---|
| Google Drive API | ⚙️ 🔜 | Backup files | Scope `drive.appdata` (own folder only) |
| Microsoft Graph (OneDrive) | ⚙️ 🔜 | Backup files | Scope `Files.ReadWrite.AppFolder` (own folder only) |
| Google Play Billing / StoreKit | 🔜 | Purchase status | Only if the app has Pro / tips |
| Syncfusion controls | ✅ | None | UI components, run locally, no data collection |
| Android Auto Backup | ✅ | App data and settings | Operated by Google; decide per app whether to exclude financial data |

## 4. Google Play Data safety (draft answers)

Google's definitions decide what counts as "collected" (transmitted off the device) and "shared". Re-check the
current Play Console help texts before submitting; when in doubt, declare conservatively.

| Question | Finance (draft) |
|---|---|
| Does the app collect or share user data? | Backups are sent, at the user's request, to the user's own cloud account. Declare **Financial info → Other financial info** as collected, **optional**, purpose *App functionality* / *Account management (backup)*, unless Google's current guidance says this is not collection |
| Is data encrypted in transit? | ✅ (HTTPS; optionally also end-to-end with the user's password) |
| Can users request deletion? | ✅ Delete backups in the app or in their Drive / OneDrive; uninstalling deletes on-device data |
| Data shared with third parties | ❌ |
| Ads | ❌ |

## 5. Apple App Privacy (draft answers, for the iOS release)

| Question | Finance (draft) |
|---|---|
| Data used to track you | ❌ None |
| Data linked to you | Financial info – only inside backups the user sends to their own Google Drive / OneDrive, if enabled; re-check Apple's definitions at submission |
| Data not linked to you | None |
| Privacy manifest | `Platforms/iOS/Resources/PrivacyInfo.xcprivacy` (required reason APIs declared) |
