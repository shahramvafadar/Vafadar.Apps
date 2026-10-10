# Independent saved filters after report navigation (D-109 / AT-114)

## Observed defect and correction

The same actual saved Food filter has a nonempty baseline on the bound transaction page. After a fictitious USD /
confirmed report query, invoking that same native saved-filter button returns an empty result and retains the old
report scope caption. The negative review records complete expected/actual rows (711 versus 2 serialized characters),
the actual native action, unchanged complete stored Settings/Accounts/Entries/SavedFilters and the failure. Original
developer database/WAL/SHM files return with matching hashes. This is a reproduced application defect.

Applying a named filter now clears only transient report currency, account-set, confirmed-only and scope note before
restoring its existing persisted combination. The same reset is shared with the existing Clear path. Saved period /
custom dates, kind, account, categories, review status, search and InTotalsOnly remain. No schema, portable format,
resource string, SDK, permission, security, quota or ledger-calculation change. Report-only fields are not newly saved.

## Verified checks

The initial corrected three-language native run passes with exact result and stored-data equality. The final review
separately exercises currency, account-set and confirmed-only report restrictions; each must first change the
independent result and then return exactly to the saved-filter baseline after native invocation. Its single
unreviewed fixture entry is prepared only in the explicit fictitious Debug route, before the stored-data checkpoint.
Reviewed actions do not save money. Complete developer files are restored even after failure.

Final six native Windows contexts (en/fa/de, light/dark, normal text/360) pass all 18 independent report
restrictions, with 234 own-window renders, identical complete saved-filter results, removed old scope
notes and unchanged complete stored rows. Every original developer database/WAL/SHM file returns with matching
hashes. Main suite: 1,512 passed, zero failed/skipped (App.Tests 276); strict Windows and canonical complete Android
Debug/Release builds pass without errors/warnings. Normal signed Release on the owned emulator creates a named
filter through the actual menu, follows Reports -> Transactions, applies that native filter and removes only its
fictitious saved-filter record. Original three transaction rows return and all 24 tables in each of three fictitious
profiles match the baseline exactly, including saved filters and Settings audit fields. No financial Save.

APK: `artifacts/android/zanance-d109-release.apk`, 81,365,077 bytes, SHA-256
`a33f277f62b6e1718a7dfe164f14935ee4daa6566ab302f3561e5971b0852462`. Package `pro.vafadar.zanance`, 0.1.0/code 1, min 24/target 36, non-debuggable,
complete ARM64/x86_64 assembly stores and app AOT, ZIP integrity and v2/v3 signatures pass. Certificate SHA-256
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is the local test certificate, not production signing approval.
The emulator retains density 420/font scale 1.0, original QA03 Native, English/System/Advanced and Home-stopped.

Final evidence: `artifacts/saved-filter-scope-delivery-proof.json`, `saved-filter-scope-apk-proof.json`,
`saved-filter-scope-release-native/proof.json` and `saved-filter-scope-all-financial-proof/unchanged-proof.json`.
Initial corrected combined-scope evidence remains historical; failed baseline evidence is not included in final counts.
D-108's complete 21-context large-text layout evidence remains separate; this change adds no layout or typography.

CI is one separate delayed exact-commit check after push.
Real OS/readers, physical phone/iOS, cold/ANR, provider and product/release acceptance remain independent gates.
