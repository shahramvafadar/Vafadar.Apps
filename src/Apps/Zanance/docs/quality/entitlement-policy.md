# Final commercial policy - D-117 / AT-119 / ENT-01

Owner approval on 2026-10-10 settles OD-03 and Section 2 of the capability/tool matrix. Core/Commerce implements
only the final pure model; the application never invokes it. There is no purchase UI, enforcement switch, SDK,
permission, schema, persistent commercial data or financial write. Current test builds remain unrestricted.

## Contracts

Entitlement records distinguish tier, purchase kind, cadence, provenance, validity and terms version. Free has no
paid record; Pro Lifetime and contradictory cadence/expiry facts are rejected. Resolution takes an explicit instant,
uses inclusive starts/exclusive ends, ignores revoked/expired/future grants and preserves Plus Lifetime after Pro.
This type consumes verified facts; construction is not payment verification. Secure cache, clock trust, offline
outage grace and billing lifecycle integrations remain later work, with no invented grace or local backdoor.

CapabilityContext separates a personal profile from one exact shared space. Active accepted matching membership
and an active Pro host supply local Plus within that space only. No personal purchase supplies membership or server
roles. Host expiry keeps retained-data commercial eligibility without inventing OD-05 server grace/retention.
History, correction, protection, basic export, backup/restore and existing holdings are Free commercial rights.
Advanced operations, personal sync, hosting and independent add-ons follow separate matrix decisions.

Quota projections distinguish financial profile, device, online identity and shared space. Actual account/goal/plan
states supply counting facts: pause consumes its slot; archive/completion/ended revisions do not. Explicit read-only
selection comes from future services, never from this pure model. Budget definitions canonically key account scope,
currency and period kind; historical row ids/dates/copies do not add definitions. Shared seats count owner/members
and unexpired pending invitations by identity, never devices; expired/left seats free capacity. Duplicate identical
reads count once; conflicting resource/member state is rejected. No automatic archive/deletion/choice occurs.

## Verification

76 policy cases pass (AT-119). The 24 valid product/duration x Simple/Advanced x personal/shared contexts each
exercise every feature and all quota kinds. Additional cases cover invalid products/facts, validity/fallback,
matching membership and host expiry, batch/overflow quota boundaries, actual source states with no mutation,
canonical historical budget copies, mixed scope domains, six seats and exact pending expiry. The complete strict
main suite passes 1,611 tests, no failures/skips; the established 299 application-flow tests are unchanged.
Evidence: artifacts/entitlement-focused-tests.log (initial 73 before the added host-expiry cases) and
artifacts/entitlement-all-tests.log (final 76 additional cases across the main suite). Test output is cleaned after
verification. No visible screen changed; the completed D-116 27-context native review remains presentation evidence.

Strict Windows and Android builds finish with zero warnings/errors. The canonical Build-AndroidApk.ps1 completes
Release with the same strict warning policy. Evidence: artifacts/entitlement-windows-build.log,
entitlement-android-build.log and entitlement-android-release.log. Full main test output is cleaned afterwards.
The full signed Release installs on the independently owned emulator-5570 and opens normal Home with its existing
expense/income/transfer/navigation actions; the D-116 fictitious goal is absent. No financial action is invoked.
Evidence: artifacts/entitlement-release-launch/proof.json. This is a startup smoke check, not a quota/purchase UI test.
No visible screen changed, so the accepted D-116 layout matrix is retained rather than repeated.

Phone-test APK: artifacts/android/zanance-d117-release.apk (80,979,883 bytes), SHA-256
`947dad1bd7a9286a2a7172c94198bc64739d1c2b8d9befceb32ca1771fc1b478`. ZIP, complete ARM64/x86_64 assembly stores
and app AOT, non-debuggable pro.vafadar.zanance package, unchanged cloud-build permission boundary and v2/v3 signature
are verified. The certificate is the existing local test certificate; no production-signing claim. Public fingerprint:
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`. Evidence: artifacts/entitlement-apk-proof.json.
Provider, physical-device, iOS, store/server, enforcement activation and release acceptance remain independent.
