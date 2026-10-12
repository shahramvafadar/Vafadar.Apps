# Contextual native date names (D-175 / AT-175)

## Observed and implemented

The original normal Release Plan form names both first/end date inputs Month/Day/Year and both
calendar actions Select date. Native captures show the same generic names for different purposes.

DateField now accepts an optional AccessibilityLabel. Publish the existing translated field purpose
with each original child part and calendar action, without changing Date, CalendarOverride, part
order, parsing, numbers, visible placeholders/layout or raw drafts. Empty labels keep generic names.
Twenty-six original usages supply their existing visible purpose, including the dynamic debt balance
reference date, Plan start/end/apply/contract dates, transaction ranges, goal and export/settlement dates.
Budget no longer groups the container's description instead of labelling its real child inputs.
No new UI wording/keys, schema, data source, permission, SDK, security or commercial policy.

## Actual evidence

- Ordinary main: 3,169 pass, zero failures/skips, 2m 37.863s; output cleaned.
- Strict final Windows: zero warnings/errors, 17.90s. Canonical full Android Release succeeds
  without warnings/errors; signed package structure and metadata verified.
- Eighteen actual Windows contexts: en/fa/de, light/dark, 360/412/wide. Existing real DateField
  checks actual native TextBox/Button names for start/end/generic fallback while a partial year
  remains "2" and its stored date stays 2027-02-28. Existing three-calendar conversion and
  unchanged ledger checks also pass. Inspect the actual Persian own-window partial-year render.
- Six normal signed Release Plan cases: original first/end dates expose unique translated
  part/calendar names in en/fa/de and both app themes. Choose Monthly and On date in an unsaved
  draft, then Cancel and explicitly discard only that review draft; never invoke Save.
- Original debt editor in each language exposes the dynamic balance-reference purpose on all
  three actual inputs and its calendar action. Cancel without creating an account or ledger entry.
- Original Windows database/sidecars are moved aside once and restored byte-for-byte. Remove the
  temporary snapshot marker so original data cannot later be mistaken for fictitious samples.
  Complete original owned Android 24-table schema/metadata/rows unchanged; recorded bytes/sidecars
  and English/System restored, final normal complete signed Release installed/stopped.

Native names are directly measured, not inferred from a copied algorithm. These checks do not prove
spoken TalkBack/Narrator order, announcements, real OS font scaling, physical phone, iOS or release
acceptance. A11Y-01/02/03 remain open in the canonical backlog. No unchanged commercial operations
or other successful native matrices are rerun for this naming change.

## Negative evidence

A direct second Windows snapshot launch reused seeded budget data and failed its existing unique
index. Preserve the failed capture/error; reset only the newly created fictitious database before
remaining contexts. Do not change the financial index or production writer to accommodate a fixture.
A native Cancel returns the existing unsaved-change dialog; the first helper failed to consent to
its own discard. A selected More tab can retain its child route, so return through the real Back.
Both are corrected review navigation, not production navigation changes or fresh financial writes.
The original Windows data remains protected separately throughout.

## Files and complete package

artifacts/date-context-all-tests.log, date-context-clean.log, date-context-windows-review.log,
date-context-android.log; date-context-windows-360-light and remaining *-final native proofs/renders;
artifacts/date-context-native/*purpose-proof.json and final-proof.json; native baseline/cancel/child
route captures; date-context-apk-proof.json, date-context-signature.log, date-context-package.log.

artifacts/android/zanance-d175-release.apk: 82,479,019 bytes; SHA-256
09decfb12ad438e7482fce8e2fc2f87dfbec4d69e931788bffd3ea8d78c101e8.
Non-debuggable pro.vafadar.zanance, API 24/36, intact ZIP, full arm64-v8a/x86_64 assembly stores and
application AOT, v2/v3 verified with existing local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
This is not production/Store signing, spoken screen-reader, phone/provider/iOS or product release acceptance.
