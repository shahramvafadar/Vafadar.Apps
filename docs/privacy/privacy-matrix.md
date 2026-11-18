# Privacy matrix

One place that records, for every app, what data exists, where it goes and how it maps to the store privacy forms.
Update it in the same pull request as any change that affects data, permissions or SDKs.

Detailed, code-verified profiles per app: [Zanance](../../src/Apps/Zanance/docs/06-privacy-matrix.md).

Dastan is a public website without accounts and runs only locally in its first milestone. Its profile – cookies for
language, theme and reading position, and server logs – is recorded here before anything is hosted. As built on
2026-11-16 the site sets four cookies, all HttpOnly and SameSite, none identifying a person: `vafadar.language` (the
language of the last page opened), `vafadar.theme` (light or dark, only after a choice),
`dastan.reading.<collection>` (the reading stage, only after a choice) and the framework's antiforgery cookie for
its forms. It loads no script, no font and no other file from another site, has no analytics and no accounts.

Read again on 2026-11-17 against the way the site runs outside a developer's machine (one container behind a
proxy, [operations](../../src/Apps/Dastan/docs/operations.md)): the profile is unchanged. The container keeps
nothing about a visitor – no database, no file, no log of requests or addresses; its only stored data are the
keys of its forms, when a host gives it a folder for them, and they identify nobody. All four cookies are
`Secure` when the visitor came by https. The site takes the address of a visitor from a proxy only when it was
told to believe that proxy, and uses it for nothing. A gallery links to the page of a museum that holds a work
(the link is followed only when the visitor chooses it; nothing is loaded from the museum). What a proxy and a
host log – addresses, times, requested pages – is theirs and has to be entered here when a host is chosen; the
site is not hosted yet.

The tool of Dastan's editors (Dastan Studio, [ADR 0011](../../src/Apps/Dastan/docs/adr/0011-editors-store.md)) is
not for visitors. Entered on 2026-11-17, with its accounts: the store of the editors keeps, for each editor, a
name, a value derived from the password (PBKDF2 with SHA-256; the password itself is kept nowhere), when the
account was made and closed, and how many wrong passwords were given in a row; and with every change of a record
the name of the editor, the time and the reason. A signed-in editor has one more cookie, `dastan.studio.session`
(HttpOnly, SameSite, `Secure` over https, twelve hours, renewed while it is used), which holds the name and ends
with another password or a closed account. Nothing of this goes into a published revision or to the public site.
The store is one file on the machine of the tool; it is not hosted yet.
Since step 13.3 the store also keeps, for each editor, what the account may do (writer, reviewer, publisher), and
since step 13.4 the last 50 times the password of an account was tried: the account, the time and whether the
password was right – no address, no browser, nothing that was typed; a try with an unknown name is not kept.
Since step 15.1 the public site of Dastan writes one more cookie of a choice, `dastan.saved`: the pages a
visitor chose to keep (at most 30: the collection, the kind of page and its identifier), for a year, readable by
the site only. Nothing of it is kept on the server.
Since step 14.8 the public site of Dastan takes reports of visitors when it is told a folder for them: a report
is the statement it is about, what the visitor wrote (at most 1,000 characters) and the day – no name, no
address of the visitor's machine, no browser, no time of the day; the form says so and asks not to write
anything personal. The address a report came from is counted in the memory of the process for one hour, to
refuse more than five reports an hour, and is written nowhere. A report an editor has looked at is kept with
the name the editor acts under; how long such reports are kept is the owner's decision.
Since step 13.7 editors bring image and video files in through the tool: an image is kept without the notes it
carries about its making (EXIF, XMP, text chunks – they can name a place and a device); a video is kept as it is,
and the tool tells the editor so. The name a file had on the editor's machine is not kept.

Legend: ✅ yes · ❌ no · ⚙️ optional (user-enabled) · 🔜 planned · – not applicable

**Current engineering inventory, 2026-10-27 (D-604):** optional local service implementations
exist; public/commercial activation and final Store/legal declarations remain unaccepted.
Rows describe separate editions and destinations, not one already published feature set.
Personal sync is service-readable; household payload E2EE does not hide service metadata.
[Source reconciliation and remaining acceptance](../../src/Apps/Zanance/docs/quality/current-privacy-inventory-alignment.md).

## 1. Data inventory

| Data | Zanance | Stored where | Leaves the device? | Who can read it |
|---|---|---|---|---|
| Financial entries (accounts, transactions, plans, budgets, categories, notes, tags) | ✅ | SQLCipher on Android/Windows; iOS integration unfinished | ⚙️ explicit backup/export or enabled personal/household sync; Android requests OS-backup exclusion, iOS acceptance open | User/export recipients; selected personal-sync service can process its data; household members decrypt authorized E2EE payloads |
| Attachments (receipt photos, PDF files) | ⚙️ | Protected local financial storage | ⚙️ selected backup/attachment sharing or opted-in sync; never embedded in CSV/report-PDF exports | Same destination boundaries as finance; explicit external-viewer copies are outside app encryption |
| Local profile names (only when more than one profile exists) | ⚙️ | Device preferences and profile catalogue | Android OS backup/transfer excluded by policy; iOS pending; explicit portable packages contain reviewed profile metadata | User and permitted package recipients; no implicit public profile upload |
| App settings (language, regional display, calendar, theme, units) | ✅ | Device preferences and profile settings | Explicit portable allowlist, excluding device security, credentials and theme; Android OS exclusion, iOS pending | User; portable file readability follows backup protection |
| App PIN verifier and attempt limit (D-63) | ⚙️ | Platform SecureStorage, outside financial databases | Not in portable backups; Android OS backup/transfer exclude device-bound ciphertext; iOS keychain follows OS policy | The app checks a salted verifier; no plain PIN is stored |
| Screenshot preference (D-63) | ✅ | Device preferences | Not portable; Android OS exclusion, iOS policy acceptance pending | The user |
| Backup files | ⚙️ | On the device (last 10), the connected cloud app folder or wherever the user shares them | ⚙️ when the user shares or uploads one | Anyone obtaining an unprotected file; encrypted files require the password (D-62) |
| CSV / PDF exports | ⚙️ | App cache, then the app the user picks in the share sheet | ⚙️ only when the user shares one (unencrypted, with a warning) | Whoever receives the file |
| Reminders | ⚙️ | Local notification service | ❌ | The user (generic text unless details are allowed) |
| Google / Microsoft backup account, OAuth tokens | ⚙️ | Device-managed/protected token caches | ⚙️ chosen provider over HTTPS; separate from optional Zanance identity | Only configured backup clients; real OAuth acceptance remains open; disconnect does not delete uploaded files |
| Optional Zanance identity and device sessions | ⚙️ local service implementation | Device-protected client state; own service identity/challenge/session records | Explicit own HTTPS service; real mail/public hosting not activated | Authorized service; processor/retention and genuine account acceptance pending |
| Purchase references and private entitlement state | ⚙️ explicit Android billing edition | Device-private cache; own verification service journals | Selected store and fixed owned verification issuer | Store/service; no card details collected by app; real purchases/commercial enforcement inactive, StoreKit2 pending |
| Household membership and protected finance | ⚙️ local implementation | Device keys/private state, service ciphertext and membership/audit metadata | Accepted members via own HTTPS service | Members decrypt payloads; service sees membership/timing/size, not household payload plaintext; downloaded copies cannot be recalled |
| Optional analysis packet and credit/request journal | ⚙️ local prerequisites | Reviewed aggregate-only packet and immutable operation/credit metadata | Only explicitly consented owned-service request; production provider unconfigured | Service/approved future provider can process reviewed aggregates; no raw ledger, notes, names or receipt upload |
| ECB daily reference requests and provenance | ⚙️ implemented integration | Bounded service/device cache and dated source records | Explicit public currency/date/symbol lookup via owned service to ECB; external source links use browser | Service/feed sees lookup metadata; no balances, quantities or bank files; public deployment unaccepted |
| Application crash reports / analytics | ❌ | – | – | No developer analytics/crash SDK configured |
| Android ML Kit diagnostics | Possible in cloud-enabled builds | Native SDK metrics | Possible whenever INTERNET is permitted, even before cloud sign-in | Google SDK; see [SDK review](zanance-sdk-review.md) |
| Advertising identifiers | ❌ | – | – | – |
| Location, contacts, photo library, microphone | ❌ | – | – | Attachments use the system picker |
| Camera capture | ⚙️ | Transient photo then metadata-free attachment | Only inside user-controlled backup/share flows | Android camera app, no camera permission; iOS contextual camera access (D-38) |

## 2. Permissions

Taken from the merged manifest of the release build; verify again on every SDK or package change.

| Permission | Zanance | Why |
|---|---|---|
| Android `POST_NOTIFICATIONS` | ✅ | Reminders; asked only when the user turns a reminder on |
| Android `RECEIVE_BOOT_COMPLETED` | ✅ | Restore scheduled reminders after a restart |
| Android `USE_BIOMETRIC`, `USE_FINGERPRINT` | ✅ (merged from AndroidX Biometric) | Optional app lock; the system dialog handles the credential |
| Android `INTERNET` | ⚙️ | Configured Cloud, personal-service or billing editions; exact Offline Release omits it; Debug adds debugger access. Permission alone does not prove traffic behavior |
| Android `com.android.vending.BILLING` | ⚙️ | Explicit Android billing edition only; native SDK graph/real-store privacy acceptance remains separate |
| Android `ACCESS_NETWORK_STATE` | ❌ | Removed from every build |
| iOS Face ID (`NSFaceIDUsageDescription`) | ✅ | Optional app lock |
| iOS camera (`NSCameraUsageDescription`) | ⚙️ | Receipt capture after contextual explanation and system permission (D-38); no Android camera permission |
| Anything else | ❌ | – |

## 3. Third-party services and SDKs

| Service / SDK | Zanance | Data involved | Notes |
|---|---|---|---|
| Google Drive API + Google Identity | ⚙️ | Optionally password-protected backup files; the account e-mail | Only when the user connects Google Drive (Android, iOS, Windows; D-35, D-50); scopes `drive.appdata` (own folder only), `openid`, `email`; Android: tokens kept by Play services; iOS and Windows: system browser with PKCE, no client secret, refresh token in the keychain / under DPAPI, revoked on disconnect |
| Microsoft Graph (OneDrive) + Microsoft identity (MSAL) | ⚙️ | Optionally password-protected backup files; the account name | Only when the user connects OneDrive (Android, iOS, Windows; D-35, D-50); scope `Files.ReadWrite.AppFolder` (own folder only); token cache on the device (MSAL storage on Android, keychain on iOS, DPAPI on Windows) |
| Google Play Billing / StoreKit | ⚙️ / 🔜 | Purchase references and opaque binding | D-295 Android/service prerequisites locally qualified; real purchases inactive; native StoreKit2 pending. See current SDK review |
| Syncfusion controls | ✅ | None | UI components, run locally; the license is validated offline. The `Syncfusion.Telemetry` package they bring is switched off at startup (`Telemetry.Disable()` in `Vafadar.Maui`) |
| Plugin.LocalNotification | ✅ | Reminder text | Local notifications only, no push service |
| Google ML Kit text recognition (Android) | ⚙️ | A receipt photo or scanned PDF page the user chooses to read | Runs on the device with a bundled model; in offline builds its usage statistics cannot be sent (no network permission); in builds with cloud backup ML Kit may send usage statistics to Google while online |
| Android OS backup and transfer | Exclusion requested | Private app stores | D-263 supersedes D-16: allowBackup=false and full exclusions. D-603 actual framework eligibility=false on Samsung/emulator; OEM/payload transfer not qualified |
| iOS device / iCloud backup | Unqualified | App data | Approved exclusion and protected-runtime integration remain pending; do not infer Android policy on iOS |

## 4. Google Play Data safety (draft answers)

Google's definitions decide what counts as "collected" (transmitted off the device by the app) and "shared". Re-check
the current Play Console help texts before submitting; when in doubt, declare conservatively.

| Question | Zanance (draft) |
|---|---|
| Does the app collect or share user data? | Submission answer pending for the exact edition/artifact and current definitions. Offline direct-network absence cannot describe Cloud, personal-service or billing editions. Review selected account/backup/sync/member/purchase/consented-analysis flows and native SDK diagnostics separately |
| Is data encrypted in transit? | Declared service/provider adapters require HTTPS; verify actual selected endpoints. This is not E2EE for personal sync or optional-password portable files, nor proof of native SDK traffic |
| Can users request deletion? | Local profile deletion is implemented. Remote identity/service retention, recipient copies and platform key persistence are separate; publish verified account/data-request procedures before service activation |
| Data shared with third parties | Exact destinations/processor and user-directed flows need submission review; no blanket absence claim from local testing |
| Ads | ❌ |

## 5. Apple App Privacy (draft answers, for the iOS release)

| Question | Zanance (draft) |
|---|---|
| Data used to track you | ❌ None |
| Data linked to you | Pending signed iOS implementation/artifact and destination review; do not reuse the old cloud-backup-only answer for optional identity, purchases, sync or sharing |
| Data not linked to you | Pending complete selected SDK/processor review; no absence declaration inferred from Android evidence |
| Privacy manifest | `Platforms/iOS/Resources/PrivacyInfo.xcprivacy` (no tracking; required-reason API `UserDefaults` declared with reason `CA92.1`) |

## D-313 optional hosted edge processor (not active)

The approved Cloudflare Tunnel path terminates public TLS at Cloudflare; visitor IP/routing metadata and identity request plaintext can be processed there. Existing application-layer sync/share ciphertext is separate from TLS; disclosed analysis has its own consent boundary. No mobile SDK, permission, local schema or automatic upload is added. The connector mounts only the owner credential file and original public CA; bounded error logs never enable request/header debugging. No live tunnel, account, DNS or real mail is configured. Draft processor/retention and exact release-build declarations need review before publication; historical offline/cloud-backup-only answers do not apply to optional identity/sync builds.
