# Google Play declaration review

Prepared on 2026-11-20 for REL-01 and PRI-04. Use this draft with the final signed
artifact, active distribution tracks and the live Play Console questionnaire.
No release edition has been selected here and no Console answers have been submitted.
Sources: [Financial features declaration](https://support.google.com/googleplay/android-developer/answer/13849271),
[Financial Services policy](https://support.google.com/googleplay/android-developer/answer/9876821),
and [Data safety instructions](https://support.google.com/googleplay/android-developer/answer/10787469).

## Financial features

Review **Support services → Other** as the candidate selection for both editions.
Google lists this category without a budgeting-specific example. Verify the wording
and available choices in the live form before certifying. Suggested description if the
form requests detail:

> Zanance is a personal finance organizer. Users record income, spending and existing
> debts, maintain budgets and savings goals, forecast scheduled activity, and track
> assets and their manually entered valuations. The app does not execute payments,
> provide or arrange credit, connect to brokers, or execute trades.

| Capability visible in the app | Evidence and classification boundary |
| --- | --- |
| Budgeting, savings goals, reports and cash-flow forecasts | These are personal money-management features. Do not select the absence of financial features solely because the calculations are local. |
| Loans and money lent | Users enter existing balances and may estimate or record installments in their ledger. The app has no lender, application, referral, approval, disbursement or loan offer. |
| Transfers, settlements and credit-card accounts | These model activity in the ledger. A recorded transfer is not transmission of funds or a payment wallet. |
| Holdings and manual valuations | There are no brokerage connections or trades. Ask whether the final holdings functionality falls under the form's combined stock trading and portfolio management label if the live guidance is ambiguous. |
| Budget/goal calculations and Analysis | Local calculations use recorded values and assumptions. Separately reviewed pilot AI behavior does not establish financial advice in these editions. Reassess if individualized advice is enabled. |

Product evidence: [release and acceptance requirements](../spec/Zanance-Product-Specification.md#183-play-and-release),
[debts and assets](../spec/Zanance-Product-Specification.md#214-debts-receivables-loans-and-assets),
and [edition-specific store copy](listing.md).

## Data safety by edition

The signed offline ARM64 and cloud-backup x64 bundles in the
[release evidence](../../../../../docs/guides/release-and-publishing.md#local-split-apk-evidence)
are reference builds. The offline bundle omits INTERNET and the cloud-backup bundle requests it.
Both retain Android backup and transfer exclusions. The release workflow disables personal sync and
billing. Use the final release package and its enabled destinations when completing the form.

On 2026-11-21, the offline Release APK from `6ed9a9fa` and the
retained Billing Release APK from `41d2e273` both passed the canonical
exact-archive verifier. No app-producing source changed between those commits.
Both packages identify `pro.vafadar.zanance` version 0.1.0 (code 1) and
contain ARM64 and x86-64. The verifier checked the signature, 16 KiB archive
and ELF alignment, edition permissions, native storage payload, and the
packaged legacy backup, cloud backup and device transfer exclusions. The
offline build uses the existing private Core package and omits network permissions.

| Local Release artifact | SHA-256 |
| --- | --- |
| Offline APK, 100,278,175 bytes | `d56f265dba6b2eb973536b33244aba057f0fe49f66aa0004169b3a4b9a1dbd1a` |
| Billing APK, 100,057,198 bytes | `4bab1399502dbb3cb190e1184709c08cb6a35d6bde255edd550568447b092e2c` |

The offline handoff is under `artifacts/android-offline-6ed9a9fa-release-review`.
The exact offline APK also passed a fresh installation and one ordinary cold
launch on a disposable Android 16 x86-64 emulator with 4 KiB memory pages.
The first observation showed the preparing screen. The same process then
showed Welcome, step 1 of 3, with the local-use explanation and the existing
backup entry. No setup or financial action was invoked. Both captured UI trees
matched their guest hashes before the app and emulator were retired.
This establishes first-run readiness for that guest. The Samsung positive control, the
eventual store-signed package and its declaration remain open.

| Flow | Offline review | Cloud-backup review |
| --- | --- | --- |
| Financial and profile records, local OCR and local reports | On-device processing is outside Play's collection definition when the data stays on the device. | Same until a feature actually transmits a record. |
| User export, external attachment viewer or system share | Review each explicit handoff for the user-initiated sharing exception. Confirm the destination and contents are reasonably expected. | Same for the explicit handoffs. Review connected backup separately. |
| Connected Google Drive or OneDrive | Not configured in the offline edition. | Review email/account identifiers from OAuth and the optional, non-ephemeral backup payload as potentially collected data. A backup can include financial records, user content, settings, and receipt photos or files. Validate each data-type checkbox against the chosen provider and actual file format. |
| Backup protection | Local package protection is a user choice. | An unprotected export is possible, so optional password encryption cannot establish a blanket end-to-end encryption exemption. Confirm HTTPS and separately assess recipient status and any exception to the sharing declaration. |
| SDK and diagnostics | Direct app networking is excluded by the manifest. Review any system or SDK-mediated path and the exact dependency inventory before certifying no collection or sharing. | Assess exact SDKs and off-device traffic, including sign-in and diagnostics. INTERNET alone proves neither collection nor its absence. |
| Deletion | Local data deletion and independent exported copies have different lifetimes. | Distinguish local deletion, provider file deletion and disconnection. Disconnection does not delete existing copies or a Google/Microsoft account. Verify exact wording before answering deletion questions. |

Use the [privacy matrix](../06-privacy-matrix.md) as the source for data classes, flow owners
and verification status. If multiple variants are distributed under the same package, review the
combined disclosures required for the active distribution rather than copying the offline column.
An internal-testing-only app is exempt from the Data safety form under current guidance. Prepare
the evidence for closed testing, open testing or production.
Record the final bundle SHA-256, enabled features, SDK evidence, policy URL and owner-reviewed
answers alongside the release receipt. Console review, declarations and publication remain open.

## In-app explanation and public policy

About Zanance now distinguishes local use, user-owned cloud backups, personal
synchronization and explicitly confirmed online analysis in all six app
languages. It discloses that enabling personal synchronization can cause
records to be sent automatically to the Zanance service, and points to the
Settings switch for disabling AI.

Strict Windows and Android Release builds passed. The signed offline APK
`fa00cffc03cf2ecf2a14102245f38d982822ed7984810519cbc37c01c1b82cb4`
passed package verification. On an owned Android 16 x86-64 emulator, the
actual About page displayed the full English string exactly. All six resource
entries were checked in source and compiled. The other five were not rendered
on Android in this check. One synthetic zero-opening account was used. No
financial posting, provider request or sharing was invoked. The guest geometry
was restored and the owned package and emulator were retired. Local evidence
is in `artifacts/about-local-connected-disclosure`.

[Play's User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311)
requires in-app access and an active public policy URL in the Console. The
[policy draft](../../../../../docs/privacy/privacy-policy.md) names
`https://vafadar.pro/privacy` as its intended URL. On 2026-11-21 the hostname
did not resolve from the development environment, so no HTTPS policy response
was verified and no new external link was added to the app. Final policy content
and review, hosting, the in-app policy link and the Console declaration remain open.

## Account deletion for editions offering account creation

[PRI-05](../spec/Zanance-Product-Specification.md#182-data-policy)
requires a deletion request path both inside the app and on the public web for
an edition with Zanance account creation. Local profile deletion, disconnecting
sync and revoking a session have different effects. The service accepts one
pending deletion request for the current reauthenticated owner and gives it a
stable receipt. The signed-in Personal Sync and store account pages offer the
request and status lookup. Focused host tests cover the isolated HTTPS transport,
lost-response recovery, account replacement and retired-page feedback. An isolated
Windows run checked the store account deletion card in 18 Persian, English and
German layout contexts at widths 360, 412 and 1280 in light and dark themes.
Each native status action opened the empty password form without a submission.
The service also implements the English browser request form at
`/identity/delete-account`. Isolated PostgreSQL HTTP evidence confirms protected
submission and the same pending receipt through the app API. Users can check
the opaque reference on that page after their session is retired. The protected
POST returns only pending, completed active-service deletion, or unavailable.
The focused test opens the receipt before and after actual synthetic account
erasure. Personal Sync and the store account form link hosted receipts to
the browser status form.
The browser opens only the fixed address, so users enter the reference
themselves. A locally pinned Personal Sync peer displays its own receipt
without a hosted link. Both new actions passed 18 native Windows layout
contexts each with synthetic receipts, Persian, English and German, light
and dark themes, and widths of 360, 412 and 1280. The review verified the
native captions and touch targets. Publicly reachable deployment, deletion execution,
verified retention and the Console declaration remain open.
See [the service boundary](../../Vafadar.Zanance.Api/README.md#api-boundary)
and [Play User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311).
