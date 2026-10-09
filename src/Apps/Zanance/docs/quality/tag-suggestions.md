# Complete tag suggestions (D-89 / AT-95)

The normal signed D-87 Android Release exposes a roughly 37 dp tag button in the owned emulator fixture, below the
44 dp target rule. The corresponding Windows 360 px/process-local 200% negative review shows fixed 40 px actions
and clipped long Latin/German/Persian tags. Retain the actual native hierarchy, geometry and own-window render.

Tag suggestions now use bounded growing rows and the existing WrappingAction overlay-button pattern. Keep the
original scalable 13 px caption, blue action/page surface and rounded shape. The new optional Suggestion appearance
does not alter existing Primary/Secondary/Text/Destructive choices. Typed TagSuggestion keeps raw normalized Value
separate from the direction-safe EntryTags.Display caption; native Invoke passes the original value to AddTag.
The eight-suggestion/ten-tag/30-character rules and explicit financial Save remain unchanged.

The first wrapping prototype retained FlexLayout. Its large-text row height still clips the bottom of a three-line
caption after width reflow; it is not acceptance. Bounded vertical rows measure the caption at the available width
and grow to its full height. The prototype helper also serialized a recycled action's caption after Invoke. Final
checks preserve the immutable caption validated before Invoke and reacquire each current control after regeneration.

AT-95 uses the actual editor, real native peers and existing AddTag command. Four fictitious admissible values include
short Latin, a 30-character Latin value, long German and Persian. Check full display/spoken name, native Invoke,
44 px targets, bounded geometry and complete caption height; add raw tags only to an unsaved draft, restore original
text/suggestions/details/dirty state and compare full stored Accounts/Entries/Settings without Save. Own-window
renders and original development data restoration remain separate from physical OS/screen-reader acceptance.

## Verification

- Final Windows matrix: en/fa/de, light/dark at 360x800, 412x892 and 1280x820 with process-local
  200% text, plus all three languages at 360x800/light/100%. All seven cohorts pass: 1,140 own-window/caption
  renders across tag, loan and Settings action routes; these are observations, not distinct screens or unit cases.
  The 21 tag proof files contain 84 actual native Invoke operations with original raw-value identity and full spoken
  names. Targets range from 48 to 138 px high and stay inside the actual group. All 21 complete stored-row/original
  draft restorations pass; the outer guard restores original development files with matching hashes.
- Main suite: 1,369 passed, zero failed/skipped; App.Tests remains 140. Test output cleaned. Final strict Windows
  build: zero warnings/errors. AT-95 adds actual native runtime checks, not invented new unit cases.
- PowerShell host startup failed locally: simple commands time out without output; the canonical
  Build-AndroidApk.ps1 invocation also produced no output and was stopped. No system/security setting was changed.
  Android restore/publish completed using the same Release, APK, EmbedAssembliesIntoApk and
  ContinuousIntegrationBuild properties from that script, with zero warnings/errors. The script-host failure
  remains a tooling limitation; this is not a successful execution of the canonical script.
- Normal complete signed Release on owned API 36 x86_64 emulator-5570: existing QA03 suggestion exposes its complete
  direction-safe native caption and a 48.00 dp target. Actual tap adds exactly raw QA03 to the
  unsaved form; Keep editing retains it, Discard returns Home without Save. The first helper stopped at an
  off-screen More details action before any input; a second helper did not observe expanded details after its tap.
  Both failed logs remain negative evidence. The successful continuation reacquires the current visible action,
  opens details and checks the actual tag target; it does not establish a fix to the earlier expansion observation.
- An unlaunched independently owned Debug package is used only to copy the exact fictitious database for comparison,
  then Release is reinstalled. Full Accounts/Entries/Schedules/Settings/Budgets/BudgetCategoryLimits equal the
  pristine baseline; three entries, zero fixture budgets, SQLite integrity pass. No financial or preference write.
  Final cold English Home, three visible Transactions, System theme/Advanced mode, English Back, secure window,
  density 420/font scale 1.0, returned Home and force-stop all pass. No phone or OS-setting access.
- Installable APK: `artifacts/android/zanance-d89-release.apk`, 81,176,661 bytes, SHA-256
  `90892ba12d1c188cba20cc41cb8e72c19c9adb16b9f41fa0c8549df85de37ef3`. Package `pro.vafadar.zanance` 0.1.0/code 1, min 24/target 36; complete ARM64/x86_64 assembly stores and
  application AOT libraries, ZIP integrity and v2/v3 signature verified with the existing local debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`. The shipped Cloud permissions satisfy the existing AndroidPrivacy allowlist through
  an equivalent read-only binary check; its PowerShell host could not start. No production signing claim.

Physical OS large text, TalkBack/Narrator, ARM64 phone, iOS, provider and Store/product acceptance remain separate
gates. This slice does not complete A11Y-03, QA-06 or the unresolved encryption/OS-backup/product decisions.

D-90 clarification: the retained pre-tap hierarchy already contains Payee and Tags because Advanced starts
expanded. The subsequent tap correctly closes them. The actual defect was the unchanged More details caption for
both states; the state-matched action and its independent runtime proof are in [entry-details-disclosure.md](entry-details-disclosure.md).
