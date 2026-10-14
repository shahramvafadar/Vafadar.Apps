# Device-only unlock policy (D-266)

The owner approved success-only device authentication on 2026-10-14. Original LockPage calls the covered AppLockService policy; unavailable, failed, unknown and exceptional responses never release queued destinations. Existing PIN paths and authenticated return grace remain unchanged. Unavailable feedback reuses the translated device-screen-lock guidance.

## Bounded local evidence

- Five new behavioral cases; complete suite: 3,526 passed, zero failures/skips, 3m24.408s.
- Permanent Windows strict build: zero warnings/errors, 23.49s.
- Original Windows LockPage/native Invoke matrix: 18 contexts (en/fa/de, light/dark, 360/412/1280, process-local 200% text), 1,800 observations. Refused responses retain the cover; success opens queued work exactly once. Separate final guidance review: 18 contexts, 1,554 observations. Financial fingerprint unchanged.
- Windows authentication responses are explicit ports, not actual Windows Hello. Independent Android package delegates the actual native adapter: NotAvailable after automatic prompt and native Unlock; Back does not dismiss, queued count remains zero. No device credential/PIN changes. The independent package was removed after its report was saved.
- Canonical Release APK: artifacts/android-device-unlock-final/pro.vafadar.zanance-Signed.apk, 88,213,830 bytes, SHA256 6ea5562af1efd8585d8e26a410c0f2c4b64c6cf5f61c9300571d4d1332f03480. Permanent package, API24/36, arm64/x64, v2/v3 local test signature, both embedded assembly stores and reviewed cipher hashes. Script rejects build warnings. Owned emulator upgrade and cold Home completed. No financial entry was posted.
- Test output cleaned after final builds. No unchanged upstream cipher corpus was repeated.

## Open findings and limits

The Home BindableLayout disconnect COM exception recurred during language/theme diagnostics. Lock checks and unchanged financial fingerprints completed; this is not clean whole-app rendering acceptance. Keep that observed defect open, separate from successful lock policy checks. Configured biometrics/enrollment changes, Windows Hello, physical phone, iOS, spoken screen readers and Store acceptance remain open. SEC-04 is in progress.
