# Automatic-copy readiness and physical lifecycle (D-327)

## Physical batch and retained original work

Use the installed complete signed offline D-326 main candidate on the approved SM-A176B, ARM64/API36,
4096-byte pages, with a coordinated exclusive device slot. Main starts with balance 979, three original
profiles, Daily automatic copies, no portable export and unchanged password protection. Its original
automatic copy (17 October 13:30, 25 KB) authenticates into a one-account/zero-transaction preview; cancel
instead of overwriting newer money. Temporarily disable the global device interval before creating
D327_Automatic_Copy_Test and its D327_Copy_Account through original onboarding. No earlier profile is
modified for fixture preparation.

With Off selected, completed onboarding and one explicit 37 EUR income still have no automatic copy.
Selecting Daily creates one device-protected 26 KB copy at 17 October 23:46; original preview has one
account/one transaction and no password entry or share path. Portable export remains never-created.
Preview/Cancel preserves Home 37. Save exactly one later 1 EUR food expense in this temporary profile;
Home 36 and both original rows survive a real main-package cold restart. The retained automatic copy
still has one transaction. Original native Replace confirmation restores that same copy: Home 37 and
only D327_Baseline_Income remain. The separate 23:49 safety copy authenticates with two transactions;
its preview is cancelled without a second restore. Do not replay those successful stages.

## Observed defect and actual cause

Selecting Weekly after restore reports that automatic creation failed even though no new copy is due.
A complete same-signature offline diagnostic upgrade preserves the exact profile/copies and reproduces
the same failure on a fresh Daily selection without another restore. Its local, original-PID-only log
records exception type/source stack only: InvalidOperationException -> DatabaseAccessGate.EnterRead ->
LocalDbContextFactory.CreateDbContext -> ZananceStore.GetSettings -> automatic readiness registration.
No exception messages, keys, credentials, financial payloads or unrelated app logs are collected.

ProtectedStorageHost.ListSafetyCopiesAsync deliberately holds exclusive maintenance while checking the
current original identity and bounded file headers. Interval publication refreshes that discovery as
foreground automatic work begins. The old synchronous readiness read correctly fails closed at that
same gate; interpreting the expected overlap as a failed copy is the bug. Cold start alone hides the
in-memory failure; it is not a fix.

## Final implementation and focused checks

Read onboarding readiness through the original cancellable GetSettingsAsync path. It waits for the
existing owner, then the coordinator rechecks its captured profile set before capturing/validating
publication and creating any package. Keep nonblocking request coalescing, disabled/unavailable behavior,
original exact publication checks, elapsed interval rules, encryption, retention and no plaintext fallback.
A successful validated not-due check clears an earlier transient failure without changing its copy or
last-success timestamp. No global gate is weakened and no InvalidOperationException is ignored.

Seven focused cases pass: the three retained original serialization/coalescing/off/weekly/retry cases
and four added cases for actual held maintenance, cancelled waiting, stale profile refusal and recovery
on a validated not-due result. Six run together and the last new scope case runs alone; none are skipped
and no full-suite rerun is claimed. Test outputs are cleaned once. The Windows strict build has zero
warnings/errors. The temporary Android log statement is removed before the final strict signed APK.

The final complete signed offline upgrade succeeds without reset. Both original native interval selections
Weekly -> Daily now retain Last automatic copy 17 October 23:46 without the failure caption. The original
one automatic/one safety row identities and sizes remain unchanged; no not-due copy is manufactured.
The restored 37 EUR fixture survives both diagnostic and final complete-package cold upgrades. The
original 36 -> 37 replacement is retained evidence, not replayed after the readiness-only change.

Return to Main, then explicitly retire only D327_Automatic_Copy_Test through both native confirmations.
The original three profile names remain; Main's Daily preference, untouched password-protection choice,
never-exported status, original 13:30 automatic copy and two 13:35/13:54 safety copies remain. Authenticate
Main's original automatic copy into its same zero-transaction preview and cancel; final Home is 979.
The phone slot is released and only this review's temporary UI hierarchy file is removed. Twenty-three
assertions on saved native observations retain successful stages, the original negative failure and final
correction. They are not new managed unit cases or a raw filesystem/key-file inspection.

Final APK: artifacts/android-automatic-readiness-d327-final/pro.vafadar.zanance-Signed.apk,
94,715,637 bytes; SHA256 6bf3f555ce39fc43d3bab0ef5a6bb7bac7fa3c3f329961a491ec172b264929e5.
Package pro.vafadar.zanance, min API24/target36, ARM64/x64, 384 embedded AOT libraries, no INTERNET or
debuggable flag; v2/v3 signature with existing local test certificate SHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. Native ARM64 SQLCipher and Mono
hashes remain the qualified D-326 bytes. The diagnostic package is separate and is not the final candidate.
Local original/final evidence is under artifacts/phone-auto-d327; the temporary type/source-only log
statement is absent from final tracked source and the final build. No credentials were entered/read.

## Boundaries

This is one physical English/light lifecycle and targeted original flow tests. It is not raw private
main-store/key-file equality, forced power loss, loss of a device key, a complete ten-file retention
stress test on hardware, a background guarantee, other devices/API24/16KiB, iOS, OAuth, Store signing
or release acceptance. Unchanged receipt/ledger/backend/performance and full layout matrices are not
repeated.
