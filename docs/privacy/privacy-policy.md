# Privacy Policy

> **Draft.** Source text for the policy published at `https://vafadar.pro/privacy`. Before publishing: fill in the
> contact address, effective date, exact online features and processors, and remove this note. Only an exact
> offline artifact with no network permission can use a "does not connect to the internet" statement; absent
> cloud-backup OAuth alone does not establish this when optional service features are included.

**Effective date:** _YYYY-MM-DD_

This policy covers the apps published by Shahram Vafadar ("I", "me") under the name *Vafadar*, currently:

* **Zanance** (`pro.vafadar.zanance`)

## Summary

* Your data is stored **on your device**.
* Local-only use keeps finance on the device. Optional identity, synchronization/sharing, purchase
  verification and explicitly consented service features have separate online data flows described below.
  The current service is qualified locally; its public activation and final processor declarations are pending.
  Cloud-backup builds can also access your own Google Drive or OneDrive. Google ML Kit diagnostics are described below.
* Financial files leave the device when **you** share an export, use your own cloud backup or expressly enable
  an optional online synchronization/sharing flow with its separate encryption and consent boundaries. Android excludes private app data from system backup and device transfer; the iOS release policy remains to be verified before release.
* Password protection is optional for portable local and cloud backup files. With it enabled, files are encrypted with
  your chosen password. Without it, anyone who obtains the file can read its contents.
* I add no advertising, application analytics, tracking or crash-reporting service. Android builds with cloud backup
  can permit Google ML Kit diagnostics; offline Android Release builds remove the network permission.

## Data the apps store

Each app stores the information you enter (for example, in Zanance: accounts, transactions, plans, budgets,
categories, tags, notes and attached receipt photos or PDF files) in a database on your device, together with your
settings (language, calendar, theme). Photos you attach are stored without their metadata (such as location) on
Android, iOS and Windows.

Android and Windows encrypt the financial database using a key protected for the current device/user.
This does not encrypt a file you explicitly export or protect it after sharing with another app.
An optional separate recovery key and a portable Zanance backup provide distinct recovery paths.
Without usable device keys or recovery material, encrypted data cannot simply be opened or reset
without losing access. The iOS implementation and its release acceptance remain unfinished.

## Backups and export

* **Portable backup file.** You can create a backup with optional password encryption (AES-256) and share it through your device's share menu to a
  destination you choose. The last backups are also kept on the device. The password is not stored; if you forget
  it, the backup cannot be recovered.
* **Automatic local copies.** If enabled, daily or weekly copies use device-key encryption without storing a password. They stay local with separate retention and explicit restore confirmation. They cannot replace a portable export when moving to a new device.
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

Android requests exclusion of private app data, financial databases, key files and internal backup
copies from system cloud backup and device transfer. Restore a new device from a portable Zanance
backup. Manufacturer-specific transfer behavior still needs device acceptance. The approved iOS
exclusion policy is not yet implemented and verified with the protected iOS runtime; no iOS release
acceptance is claimed by this draft.

## Reminders

Reminders are local notifications created on your device; no push service is used. By default they show only a
generic text; names and amounts appear only if you turn on "Show names and amounts".

## Reading receipts

If you choose "Read" on a receipt photo or PDF file, the text is read on your device. The text of a digital PDF is taken
from the file itself; a photo or a scanned PDF page is recognised – by the system on iOS and Windows and by
Google ML Kit, which is built into the app, on Android. The found amount, date and shop only fill the entry form for
you to check. The file and the text are not sent anywhere by the app. On Android, Google ML Kit may send SDK
diagnostics to Google in Android builds that offer cloud backup, even before a cloud account is connected. These
can include device/app information, per-installation identifiers, performance and SDK event metrics. Offline Android
Release builds have no internet permission. The current SDK/build-specific review is maintained in the privacy matrix.

## App lock

You can use your device's own screen lock, fingerprint or face recognition; the app does not receive or store the
device credential or biometric data. Zanance also offers a separate four-digit app PIN. It stores a salted verifier
and attempt-limit state in the platform's protected storage, not the plain PIN and not in portable backups.
Repeated incorrect attempts cause growing waits. PIN change/removal needs the current PIN; forgotten-PIN recovery
requires successful device authentication and confirmation. Without device authentication there is no in-app recovery.
The access gate is separate from Android/Windows database encryption. Unavailable or failed device
authentication does not unlock the app. Android excludes private app storage from OS backup/transfer;
iOS keychain persistence follows OS policy (uninstalling need not remove its entries).

Android screenshots are blocked by default, but you can allow them in Settings. A screenshot can contain financial
information; review it before sharing. Recent-apps protection remains enabled. iOS and Windows do not block screenshots.

## Purchases and donations

Where an app offers a "Pro" upgrade or a way to support the developer, payment is processed entirely by Google Play
or the Apple App Store. I receive no payment card details; the store may tell the app that a purchase exists so that
the purchased features can be unlocked. Zanance has no activated real-store purchases; local verification prerequisites are not a live payment service.

## Permissions

The apps request only the permissions they need. Zanance uses notifications (reminders, asked only when you turn
one on), restarting reminders after the device restarts, biometric unlock (for the optional app lock) and, only in
online-enabled builds, internet access for your chosen cloud backup and expressly enabled service features. It has no location,
contacts or photo-library permission. Android receipt photos use the camera app without a camera permission; iOS
asks for camera access only when you choose to take a receipt photo. Existing files use the system file picker. Each app's store listing shows the permissions it uses.
## Children

The apps are not directed at children under 13 and do not knowingly collect data from children.

## Your rights

Local-only records and shared export files remain under your control. Deleting local data or uninstalling
removes on-device data, not remotely retained account metadata, uploaded ciphertext or another recipient's
previously downloaded copy. Before activating an optional public service, publish the verified account/data
request and retention process, processor details and contact information. Do not treat local deletion as
proof of server erasure; final legal and service acceptance remain release gates.

## Changes

If this policy changes, the new version is published at the same address with a new effective date. Significant
changes are mentioned in the app's release notes.

## Contact

Questions about privacy: _privacy@vafadar.pro_ (to be set up), or open an issue at the project's GitHub repository.


## Optional hosted service and edge processor (D-313 draft)

The optional service can retain identity/account metadata, device/session and purchase-verification metadata,
plus encrypted synchronization/sharing objects. Disclosed optional analysis has its own explicit consent;
transport encryption does not make an intentionally submitted analysis packet opaque to its recipient.
Personal synchronization uses HTTPS and server-side protection; the selected service can process those
financial records and attachments, so it is not end-to-end encrypted. Household financial payloads use
application-layer E2EE; the service can still observe membership, timing and ciphertext-size metadata.
Device/private keys remain separate from TLS protection. A recipient can retain previously downloaded data.

The approved hosting path uses Cloudflare Tunnel on the current machine. Cloudflare terminates public TLS
and can process visitor IP, request/routing metadata and identity request plaintext. Its role is service
transport, not an advertising or application analytics SDK. The connector uses bounded error-level logs,
without request/header debug logs. Public Cloudflare/DNS, real mail and final processor/retention/legal
acceptance are not configured; this draft does not announce an active public service. Historical
cloud-backup-only/offline Store answers must not be reused for an optional identity/sync service build.
