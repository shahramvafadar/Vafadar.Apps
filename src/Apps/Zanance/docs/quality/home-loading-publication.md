# D-275 complete Home loading and retry delivery

## Observed defect and resulting behavior

D-274 native startup exposed the original No accounts card before an existing account was read.
The complete financial overview is now gated by the existing SnapshotLoadState, from its first
frame through every complete read/publication. Initial defaults and dashboard actions are hidden
and disabled until ready; an actually empty completed snapshot still offers the existing next step.
Loading, failure and original retry are translated in all six languages. Retry never creates an
account, posts money or recreates protected storage. The existing load semaphore, profile retirement,
current-file/rights checks and original computation/navigation paths remain unchanged.

The new coordinator use does not manufacture a faster usable Home: it prevents an incomplete
frame from claiming to be financial data. No two-second objective, full performance/ANR acceptance,
platform parity or product publication is asserted. No schema, SDK, permission, key, backup format
or persisted preference changes.

## Tests

The full app filter passes 3,572 tests, zero failures/skips (3m25.635s), including the new AT-275
case refusing final publication after retirement. Existing coordinator cases cover first frame,
pending/complete reads, repeated loads, failed initial read/reload, retry and cancellation with
actual original SQLite data-preservation scenarios. The final Home retry caption refinement is
checked by the 242 localization tests; unrelated successful test projects are not replayed.

## Native Windows evidence

Independent storageacceptance PackageName is verified before launch; the Debug route also refuses
main identity before covered storage startup. Only the application's own hidden window is captured.
The original HomeViewModel, HomePage and protected SQLite factory are used. A forwarding native
observation port holds the captured original read lease before returning the first context, then
releases that same context. It can fail one later captured read without simulating queries or data.

The final matrix passes en/fa/de, light/dark, 360/412/1280 and process-local text scale 2:

- 18 actual first-read pauses keep the original empty defaults and Add unavailable.
- 18 failed original reloads keep content covered and show complete failure/retry captions.
- 18 real native button Invoke calls execute the original retry command and restore ready Home.
- 36 focused loading/failure captures and 2,868 native caption observations complete.
- The original amount-handler retention check and complete financial-row fingerprint pass;
  fingerprint a1f00084323125f47bc4cf0f3288df5bddc8b0da8c162172070b04efae3564f9.

The first partial visual run revealed the incorrect reused Load transactions again caption on
Home. It was stopped for that actual defect, Home-specific wording was added in all six languages,
and the affected matrix completed again. The incomplete earlier run is not final acceptance.
Final Persian narrow failure capture was inspected directly. Original generic unexpected-error
dialog handling remains separate; the injected reload is observed directly before native retry.

## Signed Android Release evidence

Only the owned Zanance_Storage_Review (adb port 5038, emulator-5570) is used. Complete signed Release
upgrade and cold launch keep the same live process through four fresh hierarchy observations:
startup preparation, actual Loading your overview, then ready Recorded balance/New expense.
No observation shows the false No accounts card. Old dump files are removed before capture so an
unavailable hierarchy cannot be mistaken for a current tree. Foreground screenshot protection
and device settings remain unchanged. Android injected read failure/retry is not separately accepted;
the original shared flow and visible Windows failure/retry are distinguished from Release cold UI.

Final APK: artifacts/android-home-loading-corrected-final/pro.vafadar.zanance-Signed.apk,
88,262,982 bytes; SHA-256 df09ab752d331fc69b3e75ad3fbc959b395c4df1aca8963e53fb247ddcb73ac7.
Package pro.vafadar.zanance, target SDK 36, arm64-v8a/x86_64; v2/v3 signature verifies.
The first APK preceded the corrected Home wording and is superseded, never the phone handoff.

Windows/Android strict builds have no warnings/errors. The normal Windows PackageName is restored
after independent review without a main review launch. Test outputs are cleaned after checks.
Physical device, spoken screen readers, real OS scaling, iOS and release acceptance remain open.
The full development objective stays active.
