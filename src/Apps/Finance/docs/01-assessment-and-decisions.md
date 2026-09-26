# 01 – Assessment and decision log

## 1. Verified state of the repository (2026-09-26)

| Area | Specification assumption | Verified reality |
|---|---|---|
| Shared libraries | `src/Libraries/Core`, `Localization`, … | Exist as `src/Libraries/Vafadar.*`; all build without warnings; 97 tests pass |
| Localization | Runtime language switch, RTL, independent calendar | Implemented – verified (unit tests): en/fa/de, Gregorian/Persian display, `IDateFormatter`. Missing: region, week start, number parsing, Persian/Arabic digit input |
| Data | SQLite, EF Core, audit timestamps | Implemented – verified: `LocalDbContext`, UTC-ticks `DateTimeOffset`, audit interceptor, startup migration. Finance model is empty; no migration exists yet |
| Backup | AES-256, 10 versions, validation before restore | Implemented – verified at library level (package, AES-256-GCM, checksums, app id/version checks, retention after successful upload, SQLite snapshot/restore). **Gaps:** settings are not in the package (BAK-03); no safety copy before restore (BAK-09); uploaded copy is not re-read (BAK-07); no UI |
| Google Drive / OneDrive | Destinations in the user's space | Storage classes implemented and tested against fake HTTP only. **No sign-in exists**, so nothing works end to end (BAK-14) |
| Authentication | Interfaces only | Confirmed: abstractions only |
| Maui | Bootstrap, Syncfusion license, `{v:Translate}` | Confirmed. Only `Syncfusion.Maui.Core` referenced |
| Finance app | Home and settings, DB and backup wiring | Confirmed: two tabs, language and calendar selection, database registered, backup service registered without a storage or UI |
| Platforms | Android first | Targets Android (API 24+), iOS 15+, Windows; Android and Windows build in CI; iOS builds on demand only |
| Branding | Not final (PR-10) | App icon and splash are the .NET template artwork (Microsoft trademark) – must be replaced by a neutral placeholder |
| Theme | Light theme complete in phase 1 (UX-08) | Template follows the system dark theme automatically – untested dark styles would appear |
| OS backup | Must be documented (SEC-03) | `android:allowBackup="true"`; iOS includes app data in iCloud/device backups by default |
| Permissions | Minimal | `INTERNET`, `ACCESS_NETWORK_STATE` (template) |

## 2. Decision log

| ID | Decision | Reason |
|---|---|---|
| D-01 | Finance domain lives in `Vafadar.Finance.Core`; persistence in `Vafadar.Finance.Data`; shared libraries change only for generic needs (week start, number parsing, app lock, notifications) | AR-01, HAND-04 |
| D-02 | Money is stored as `long` minor units with an ISO 4217 currency code; minor digits come from a currency table, never assumed to be 2 | FIN-05, SQLite has no decimal |
| D-03 | Financial dates are `DateOnly`; created/updated moments are UTC `DateTimeOffset` | FIN-08, REM-08 |
| D-04 | A transfer is **one** ledger entry with source and destination account and both amounts | FIN-02, FX-03: sides can never diverge |
| D-05 | Ledger entry kinds: Income, Expense, Transfer, Refund, IncomeReversal, Adjustment; amounts are positive magnitudes, the kind defines the sign | FIN-01, FIN-10, REF-05, ACC-08 |
| D-06 | Occurrences are computed from the schedule rule; only occurrences with a state (settled, skipped, moved, amount changed) are stored, keyed by `(ScheduleId, OriginalDate)` with a unique index | REC-13, REC-21 idempotency |
| D-07 | A ledger entry created from an occurrence stores `(ScheduleId, OccurrenceDate)` with a unique index – a second automatic posting is impossible at database level | REC-21, AT-30 |
| D-08 | Editing "this and future" splits the series: the old schedule ends before the occurrence, a new schedule continues from it | REC-15, history is never rewritten |
| D-09 | Review state: Confirmed / Unreviewed; manual entries are Confirmed, automatic postings are Unreviewed | FIN-11..14 |
| D-10 | Balances and reports are computed on the fly from the ledger (no stored running balances) | FIN-09 consistency; fast enough for 10,000 entries (to be measured, Q-02) |
| D-11 | Navigation: Home, Transactions, Plans, More + a persistent "Add" action | Specification §14.2 |
| D-12 | Phase 1 uses the light theme only (`UserAppTheme = Light`) | UX-08 |
| D-13 | Syncfusion controls are used where they add value: Charts (reports, forecast), Segmented control (kind/mode selectors), Calendar/Date picker (if Persian calendar support is verified), Numeric entry, Busy indicator, Popup, Chips | Owner decision; licensed |
| D-14 | Icons: Fluent UI System Icons font (MIT), bundled offline | VIS-03/04 |
| D-15 | Reminders: local notifications via `Plugin.LocalNotification` (MIT), no exact-alarm permission | REM-09 |
| D-16 | Android Auto Backup / iOS backup stay **enabled** and are disclosed in the privacy policy and Data safety | Owner decision (2026-09-26) |
| D-17 | Cloud backup destinations stay hidden until real sign-in and upload/download are verified; local backup (export/import of an encrypted file) is the phase-1 guarantee | BAK-14 |
| D-18 | CSV writer/reader are implemented in `Finance.Core` (no dependency) with formula-injection protection | IO-06 |
| D-19 | Temporary placeholder branding: neutral green icon with a simple ledger glyph; final name/logo pending | PR-10 |
| D-20 | If no online feature ships in the first release, the `INTERNET` permission is removed from that release | PRI-01, privacy |
| D-21 | Product name **Zanance** (owner, 2026-09-26), shown untranslated in every language. Code, folders and the app id keep the working name *Finance* (pro.vafadar.finance) | PR-10 |

## 3. Conflicts found and their resolution

| Conflict | Resolution |
|---|---|
| Earlier repository docs said "support light and dark themes" | Superseded by D-12; coding conventions updated |
| Earlier `requirements.md` asked "Rial or Toman" | Answered by FX-07: unofficial display units are phase 2 |
| Earlier docs listed a tip jar under monetization | Remains phase 2+/out of phase-1 scope (MON-07) |
| Specification assumes library paths without the `Vafadar.` prefix | Documentation uses real paths |

## 4. Risks

| Risk | Mitigation |
|---|---|
| Phase 1 turns into an endless project (RISK-01) | Vertical slices with gates (see 04) |
| Wrong early ledger model (RISK-02) | D-04/D-05/D-06 from the first migration; golden test AT-62 |
| Over-claiming security or cloud readiness (RISK-03) | Status words; cloud hidden until verified (D-17) |
| Persian calendar date input in Syncfusion controls unverified | Verify in slice S2; fall back to an own day/month/year picker |
| Notification behaviour differs per Android version | Manual device tests (AT-34..38) before release |
