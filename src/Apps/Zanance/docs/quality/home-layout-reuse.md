# Original Home section reuse (D-177 / AT-176)

Status: locally verified; complete signed APK, tests, strict builds, Windows and Android follow-through passed.

## Change

The original appearance method previously removed all eight section containers and then reinserted
all eight, even when the saved order was already displayed. It now leaves correctly placed sections
attached and moves only a displaced original container. The original XAML starts in the existing
Core default order, so the default first appearance also avoids those operations. Saved ordering,
visibility, all original bindings/children, scalable text and money calculations remain unchanged.

The earlier asynchronous-startup candidates were rejected after native measurements and their
changes were removed. See [the finite investigation](startup-preparation-investigation.md).
No schema, permission, SDK, credential, commercial policy, network or portable-format change.

## Current evidence

- Seven existing HomeLayout behavior cases pass. Ordinary complete main: 3,169 passed, zero
  failures/skips, 2m 46.602s; test output cleaned.
- Strict current Windows: zero warnings/errors. Canonical complete signed Android Release built
  without warnings/errors; package pro.vafadar.zanance, API 24/36, v2/v3 signature verified.
- Eighteen focused own-window contexts: en/fa/de, light/dark, 360/412/wide. The real appearance
  method is invoked twice: zero native section-add/remove events, original native handlers/order
  retained, actual order matches saved Core layout, complete accounts/entries/settings/budgets unchanged.
- Earlier full Home customization/visibility/native-return checks passed at 360/light. Focused
  remaining contexts omit unrelated navigation. Original Windows database/sidecars restored exactly.
- Current APK: artifacts/android-home-layout-reuse/pro.vafadar.zanance-Signed.apk, 82,917,461 bytes,
  SHA-256 e6ae44b5ed84872788fc51c113e1960c36afd91ee07f6eed8308bfc6f0360c89.

Six normal Release Android Home/return cases pass in en/fa/de and light/dark. All 22 original financial/settings tables match the baseline; exact owned database/sidecars and English/System are restored. Final full signed Release is installed/stopped. The handoff APK is artifacts/android/zanance-d177-release.apk.
The removal of unnecessary native operations is proved; no faster cold Home, two-second performance,
ANR, physical phone, screen reader, iOS, provider or product-release acceptance is claimed.
