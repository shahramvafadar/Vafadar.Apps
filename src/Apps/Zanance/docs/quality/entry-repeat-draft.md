# Repeat from a new transaction draft (D-111 / OD-12 / AT-116)

## Approved behavior

On 2026-10-10 the owner explicitly approved Repeat in a new transaction form, opening Plan with the entered
amount, account and selected date without posting a transaction. A real growing Repeat button after the complete
basic fields makes that path discoverable in Simple and Advanced. The existing Home Add plan target remains.

Supported new expenses, income and transfers validate the applicable base amounts/accounts through the existing
EntryDraftValidation and display-unit parser. A detached EntryPlanDraft carries minor-unit values and original
currency identities, accounts, selected date, title/category and note through in-memory navigation. Monthly is
initially selected, anchored to that first date in the named rule calendar. This unpaid draft is not shifted one
month forward as the existing already-recorded fromEntry path is. All existing rule, preview and Save logic remains.
Automatic posting and reminders are off initially. Opening/returning never invokes ledger or plan Save.

The original entry form, including fees, receipt state/files, tags, payee and other transaction-only fields, stays
on the navigation stack. Translated inline feedback/help states which supported fields are copied and which remain
only in that original draft. Returning preserves the same complete original fingerprint. New refunds/reversals and
editing existing transactions do not show this action; the existing recorded-entry Make recurring path is unchanged.
Repeat and Save share the existing receipt-unit guard, preventing reinterpretation after a display-unit change.
Plan rechecks current source/destination availability and original ISO currency before filling values; unavailable or
changed accounts leave amounts empty for explicit selection/re-entry rather than guessing a conversion.

No schema/migration, SDK, permission, device security, quota, portable format or financial rule changes. Only the
ordinary explicitly saved Schedule uses its existing storage/backup path; the navigation draft is not persisted.
All six languages have real action/hint/help/error translations. No commercial test limits are introduced.

## Verification

Fourteen behavior cases verify all supported kinds, Gregorian/Persian/Hijri rule calendars, unchanged first date,
minor units, detached identity, metadata, disabled posting/reminders and invalid unsupported/source/destination/
amount/date/name cases. Main suite: 1,535 passed, zero failed/skipped (App.Tests 299), outputs cleaned.
Strict initial Windows build passes without warnings/errors. A first new diagnostic helper referenced a local
Invoke method outside its scope; compilation failed and was repaired with the actual native Cancel peer. An initial
route insertion matched the help-only list, so the app explicitly rejected the route. That failed folder is retained;
the route was corrected without changing production behavior or overwriting negative evidence.

The initial normal English/native Windows check passes all three actual Repeat/Cancel flows and independently
rejects an invalid amount. Complete original drafts and stored Accounts/Entries/Settings/Schedules/SavedFilters match;
original developer database/WAL/SHM files return with exact hashes. This initial evidence precedes moving the action
below transfer-specific fields; the final checks below cover the completed placement.

The final financial matrix passes 33 contexts and 99 actual Repeat/Cancel handoffs, plus 33 invalid-input checks:
English/Persian/German, both themes, 360/412/1280 widths at process-local 200% stress; all six languages at normal
360 width in both themes; and the three primary languages in Simple. The 1,188 own renders include setup and
dialogs. Complete original drafts and stored rows remain; original developer files return with matching hashes.
Some captures after queued caret/validation scrolling show the action outside the image although its native
Invoke succeeds. These are not visibility acceptance. A separate focused route uses actual native Scroll and
requires the whole real target inside its viewport; it does not repeat financial handoffs. Its first transfer
capture failed before the selected picker finished realizing its caption. Waiting for arrangement before the
capture fixes that diagnostic timing; the negative folder remains. The corrected first three-case check passes.
Focused setup uses bounded real fictitious onboarding commands, avoiding unrelated restore walkthroughs.
Help examples now explicitly name the Gregorian calendar, rather than implying it for Persian/Hijri rules.

Evidence: artifacts/entry-repeat-first-native-corrected/en-entry-repeat.json, entry-repeat-final-* and
entry-repeat-first-presentation-corrected/en-entry-repeat-presentation.json. The final focused presentation
matrix passes the same 33 contexts/99 actual visible targets, with 627 own renders and complete no-write/draft restoration.
Strict final Windows and full canonical Android Debug/Release builds finish without warnings/errors. Normal
signed Release installs on the independently owned emulator and its actual Repeat/Cancel preserves the entry.
A separate explicit Plan Save creates the intended named monthly plan; the original transactions remain unchanged.
Only that exact fictitious plan is deleted through its native UI. Complete original columns/rows in all 24 tables
across three fictitious profiles match afterward. Full Debug is used only to read those owned files and is never
launched; full signed Release returns, starts and stops on Home. Foreground secure policy remains.

Release helper failures are retained: the first new-form hierarchy was unavailable while the native keyboard was
open; a bounded retry alone did not resolve it. Hiding only that app's shown keyboard made the hierarchy available.
A full-height swipe then skipped Title, so the helper uses short viewport-relative gestures and the actual current
captions. Android EditText exposes its semantic caption with the value; checks remove only that exact prefix.
The completed Save/return flow subsequently passed, but the cleanup helper opened the due occurrence rather than
the plan detail. Its real Plan command reaches the owning plan. Only cleanup was continued afterward; successful
financial flows were not repeated. No app source or financial rule changed for these helper corrections.

Phone-test APK: artifacts/android/zanance-d111-release.apk, 81,397,845 bytes; SHA-256
8ded42034462a631a4ccfdfadbec8685280a4e041a1310a9d48c16f68c154f68. ZIP, complete ARM64/x86_64 assembly stores/app AOT, non-debuggable package
pro.vafadar.zanance and v2/v3 signatures are verified. This is the existing local test certificate, not production
signing. Evidence: entry-repeat-delivery-proof.json, entry-repeat-apk-proof.json, entry-repeat-release-native-observed/proof.json
and entry-repeat-all-financial-proof/unchanged-proof.json. CI is checked separately once after pushing.
Physical device/iOS, real OS/readers, OAuth/provider, encrypted database/library, billing and release gates remain open.
