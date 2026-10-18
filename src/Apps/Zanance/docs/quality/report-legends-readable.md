# Complete report legend rows (D-339 / A11Y-03)

## Observed defect and resulting behavior

The original category table gives a growing name and an Auto-sized monetary label the same row.
At 200% process-local Windows scale and 360-unit width, a fictitious long category name is natively
trimmed; an extreme negative amount loses its ending outside the page. Original PID93332 retains
the actual own-window capture and IsTextTrimmed=true observation. This is rendering evidence,
not a financial calculation failure.

Category name/detail and signed net amount now occupy separate growing rows. Names wrap at native
scale. The existing AmountReadout retains the complete MoneyText packet and makes oversized values
horizontally scrollable with the existing translated hint. Keep the original category command,
parameter, spoken identity/hint and semantic colors. Its real button covers only name/detail, has
a minimum 44-unit target and cannot cover the amount viewport. Composition names, shares and values
also receive independent rows, retaining original typography and secondary amount color.

No financial calculation, report/PDF dataset, schema, provider, key, policy, permission or translation
changes. Current Store drafts show the earlier period overview above these rows; their unchanged
visible content is not rerendered for this change.

## Final Windows qualification

`Run-StoreDrafts.ps1 -ReportRowsReview` uses the D-338 fresh independent package/storage/non-input
desktop guard, with explicit process-local font scale 2. It renders original ReportsPage bindings,
not copied controls. Extreme canonical values and long multilingual names are explicit fictitious
presentation inputs, not assertions about report calculations. No financial commands are invoked.

Final run `df0ee8e24b604d13aacaf3c1d96d05ca`, PID48820, passes all 18 contexts: en/fa/de,
Light/Dark, 360/412/1280. Both category and composition names pass native glyph bounds and scaling;
actual native amount viewports remain inside the page and both Scroll-pattern ends are reachable.
Category wiring and minimum target dimensions remain valid. Actual target rectangles do not overlap
the value viewport. Seeded ledger rows are unchanged; input desktop stays Default. Each context
retains category/composition own-window renders and its original observations.

Retained earlier observations are not final acceptance: PID19752 reached the repaired category but a
diagnostic ancestor-index check mistook its inner stack for the amount's grid row; that check now
uses the direct AmountReadout. PID5776 passed 18 text/scroll contexts before actual overlap checking
was added. PID79632 then exposed the original negative overlay margin crossing the value boundary.
A margin edit initially targeted a similar change-summary row; it was restored and the exact category
button was corrected before final qualification. All failures and original processes remain identified.

## Final Android qualification

The independent Release/AOT/trimmed AndroidHome caller compiles original application source/XAML.
Its `legends` phase uses one separately owned fictitious financial-table database, without preparing
or measuring the reference/tenfold Home workloads. It presents the original report view in en/fa/de
and both themes, checking native TextView allocation/ellipsis, complete packets, horizontal scroll
ends, page containment and actual category target/value separation. Settings, account and entry
inputs compare unchanged after each context.

Run `d00c63cc7c254f3abf3bbf99be51eb5a`, comparison `e28a527cfb2a4cc5af4e95a1dd7cf9a8`,
original PID32425 completes all six cases. Initial Activity observation timed out; the same live PID
later published startup-ready. The emulator displayed an actual ANR dialog. Its original native XML
was retained; selecting Wait continued that same process without resetting, reinstalling or reseeding.
The final exact-PID public report succeeds and explicitly does not claim physical-phone acceptance.
ANR remains a separate open QA-06 failure. Only the completed independent process was stopped; its
existing private fixtures remain. The hash-matched owned temporary ANR XML was removed from the emulator.

## Build and handoff boundary

Final independent Windows build (16.60s), native Android caller build (81.39s), ordinary Windows build
(28.02s) and complete offline Android publish pass strict warning-as-error checks. No unrelated passed
managed financial/OCR/backend suite is repeated; no managed unit-count increase is claimed.

Complete signed APK: `artifacts/android-report-legends-d339/pro.vafadar.zanance-Signed.apk`,
95,231,903 bytes, SHA256 `da11132d6136c454d9a42bc11fcc93c22fab96cef1dd23559473749c4403fc75`.
It contains ARM64/x64 and 384 AOT libraries, min API24/target36, without INTERNET or debuggable.
Signing certificate SHA256 remains
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` (development signing, not Store signing).
The actual SQLCipher and Mono engine bytes in both architectures match D-336. An initial broad filename
comparison included a rebuilt managed AOT bridge; engine equality is checked only on exact native engine names.

Main upgraded without reset. Its existing package-replaced process PID320 remained the same through
the warm Activity launch (3012ms), initial Preparing cover and later Home showing My account / 0.00 EUR.
No Save or preference change was invoked. Activity timing is not loaded-Home timing or cold acceptance.
The ordinary Windows development identity is restored. These results do not complete
A11Y-03, actual OS scaling/readers, all report controls, phone/ARM64/API24/16KiB qualification, iOS,
two-second Home, ANR resolution, OAuth, Store signing or publication. Windows commercial delivery
stays deferred; these Windows runs are development checks.
