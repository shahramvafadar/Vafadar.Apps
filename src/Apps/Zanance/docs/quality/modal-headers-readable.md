# Single readable modal headers (D-98 / AT-103)

## Observed problem and correction

The actual Windows debt modal reached from Accounts shows both the generic child PageHeader and its own
Cancel/title row. The underlying ordinary navigation stack has multiple pages; that depth does not identify
the presented modal. A German 360 px/200% baseline records one extra generic header. The own title also wraps
into five narrow lines beside Cancel, even though its realized glyphs are within the allocated text bounds.

PageHeader.Attach now excludes Shell Modal, ModalAnimated and ModalNotAnimated presentation modes.
Ordinary child pages keep their growing header, body, binding context and native Back. The eleven existing
modal title/Cancel rows use wrapping FlexLayout: Cancel does not shrink; the full scalable title grows and can
move onto a full-width row. The asset-event subtitle stays with its title. Existing captions, commands,
semantic colors, buttons, forms, footer actions and financial calculations remain unchanged.

## Verification

- AT-103 opens thirteen actual modal cases across eleven types with each route's real query contract.
  Check zero generic headers, one own title, native title and Close/Cancel caret/glyph bounds against native
  and MAUI allocation, full actual native spoken name, at least 44 px target, and no title/button overlap.
  A repeated generic attachment attempt preserves the same body/context. Invoke actual native Close/Cancel,
  return to the same Accounts parent with one ordinary header, then invoke native parent Back.
- Compare complete stored Accounts/Entries/Settings/Budgets/Schedules/Categories/Goals/HoldingTypes/HoldingEvents
  before and after every case. No input, Save, settlement or posting. These modal openings use named Shell
  routes; only the accompanying AT-102 debt opening invokes the actual native Add debt button.
- Final en/fa/de light/dark at 360x800, 412x892 and 1280x820 with process-local 200% text, plus en/fa/de
  360x800/light at 100%: 1293 own-window renders, 21 AT-103 summaries, 273 native Close/Cancel invocations,
  21 native parent Back invocations and 294 actual modal-header checks. AT-102 also passes 21 summaries and
  42 native debt open/cancel invocations. Original development DB/WAL/SHM files restore with matching hashes.
- Negative baseline duplication and narrow own-title captures are retained separately. An initial diagnostic
  manually constructed a base ButtonAutomationPeer and reported an empty name; the actual native control's
  peer reports the complete caption. Initial fixture compile errors and missing budget query inputs are
  corrected only in the review fixture, excluded from final counts; no production Budget behavior changes.
- All 1,384 main tests pass, zero failed/skipped; output cleaned. Strict Windows and canonical complete
  Android Debug and Release builds have zero warnings/errors. The native glyph fixture is Windows-only.
- Normal signed Release on the owned API 36 x86_64 emulator checks debt, expense and new plan forms in
  en/fa/de and light/dark: 18 native title/name/bounds and Cancel cases, one title, no title/button overlap,
  no financial input or Save. Language/theme are changed through app UI and restored to English/System.
  This is normal native scale, not Android glyph, 200%, screen-reader or all-eleven-form acceptance.
- Exact readback of all 24 tables/columns/rows and integrity pass for the original three fictitious profiles
  (10,001 entries, 100,001 entries and three entries). Unlaunched Debug is only for readback; complete
  Release is reinstalled. Final English Home/three Transactions, System/Advanced/native Back, secure window,
  density 420/font scale 1.0 and Home/stop pass. No OS or device-security setting changes.
- APK: `artifacts/android/zanance-d98-release.apk`, 80,770,987 bytes; SHA-256 `079c59c87cab086ce7272d18dae24c365869de8b96acdada89de451dd3a531f1`.
  Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 stores/app AOT,
  non-debuggable manifest, ZIP integrity and v2/v3 signatures pass. Existing local test/debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` remains test signing, with no production or Store claim.

A11Y-03 remains partial: other controls, real OS scaling, screen readers, ARM64 phone and iOS acceptance remain
open. Owner/provider/encryption/licence/commercial/release gates remain separate; no entire-backlog completion claim.
