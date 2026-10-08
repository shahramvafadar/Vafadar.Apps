# Privacy Policy

> **Draft.** Source text for the policy published at `https://vafadar.pro/privacy`. Before publishing: fill in the
> contact address, the effective date and the list of apps, and remove this note. If the released Zanance build has no
> cloud backup (no OAuth clients configured, D-35), remove the cloud backup parts and keep "does not connect to the
> internet".

**Effective date:** _YYYY-MM-DD_

This policy covers the apps published by Shahram Vafadar ("I", "me") under the name *Vafadar*, currently:

* **Zanance** (`pro.vafadar.zanance`)

## Summary

* Your data is stored **on your device**.
* I do **not** operate servers that receive your data, and I cannot see it. Zanance connects to the internet only if
  you connect your own Google Drive or OneDrive for backups, and only for that.
* Your data leaves the device only when **you** share a backup or export file or store a backup in your own cloud
  storage, and through the device backup of your phone (Google or Apple), depending on your device settings.
* Password protection is optional for both local and cloud backup files. With it enabled, files are encrypted with
  your chosen password. Without it, anyone who obtains the file can read its contents.
* The apps contain **no advertising and no analytics or tracking**.

## Data the apps store

Each app stores the information you enter (for example, in Zanance: accounts, transactions, plans, budgets,
categories, tags, notes and attached receipt photos or PDF files) in a database on your device, together with your
settings (language, calendar, theme). Photos you attach are stored without their metadata (such as location) on
Android and iOS.

## Backups and export

* **Backup file.** You can create a backup with optional password encryption (AES-256) and share it through your device's share menu to a
  destination you choose. The last backups are also kept on the device. The password is not stored; if you forget
  it, the backup cannot be recovered.
* **CSV and PDF export.** You can export entries (CSV) or a report (PDF) and share them. These files are **not
  encrypted**; where they go is your decision. Attachments are never included in exports.
* **Cloud backup (optional).** You can connect your own Google Drive or OneDrive (Android, iOS, Windows) and store
  backups there. Nothing is uploaded until you connect and choose "Back up now". Cloud backups follow your password-protection choice. If enabled,
  they are encrypted with your password, which is never saved or uploaded. If disabled, the file is readable by
  anyone who obtains it, even though it is stored in your own cloud account. The app asks only for access to its own app folder
  (`drive.appdata`, `Files.ReadWrite.AppFolder`) and, for Google, your e-mail address to show which account is
  connected. Sign-in happens in Google's or Microsoft's own screen (on Windows and iOS in your browser); the app never sees your account password. The access it receives stays on your device (on iOS in the keychain, on Windows protected for your Windows user account). The
  files are stored under your Google or Microsoft account terms; I have no access to them. "Disconnect" signs out on
  the device and removes the stored access; backups already uploaded stay in your account until you delete them
  (Google Drive: Settings → Manage apps; OneDrive: the folder Apps/Zanance).

## Device backup (Android and iOS)

Android may include app data in the device backup to your Google account (Android Auto Backup), and iOS may include
it in your iCloud or computer backup, depending on your device settings. These backups are operated by Google or
Apple under your account terms.

## Reminders

Reminders are local notifications created on your device; no push service is used. By default they show only a
generic text; names and amounts appear only if you turn on "Show names and amounts".

## Reading receipts

If you choose "Read" on a receipt photo or PDF file, the text is read on your device. The text of a digital PDF is taken
from the file itself; a photo or a scanned PDF page is recognised – by the system on iOS and Windows and by
Google ML Kit, which is built into the app, on Android. The found amount, date and shop only fill the entry form for
you to check. The file and the text are not sent anywhere by the app. On Android, Google ML Kit may send usage
statistics to Google when the device is online in builds that offer cloud backup; builds without cloud backup
have no internet permission at all.

## App lock

If you turn on the app lock, unlocking uses your device's own screen lock, fingerprint or face recognition. The app
never receives or stores your fingerprint, face data or PIN.

## Purchases and donations

Where an app offers a "Pro" upgrade or a way to support the developer, payment is processed entirely by Google Play
or the Apple App Store. I receive no payment card details; the store may tell the app that a purchase exists so that
the purchased features can be unlocked. Zanance currently has no purchases.

## Permissions

The apps request only the permissions they need. Zanance uses notifications (reminders, asked only when you turn
one on), restarting reminders after the device restarts, biometric unlock (for the optional app lock) and, only in
builds that offer cloud backup, internet access for your own Google Drive or OneDrive. It has no location,
contacts, camera or photo library permission; attachments are picked with the system file picker. Each app's store listing shows the permissions it uses.
## Children

The apps are not directed at children under 13 and do not knowingly collect data from children.

## Your rights

Because I do not collect your personal data, there is nothing I hold that I could show, correct or delete. All data
is under your control on your device and in the files you share. "Delete all data on this device" in the app's
settings, or uninstalling the app, deletes its on-device data.

## Changes

If this policy changes, the new version is published at the same address with a new effective date. Significant
changes are mentioned in the app's release notes.

## Contact

Questions about privacy: _privacy@vafadar.pro_ (to be set up), or open an issue at the project's GitHub repository.
