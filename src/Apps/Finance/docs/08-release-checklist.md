# 08 – Release checklist (first public release)

## Product and data

- [ ] All phase-1 acceptance scenarios pass on the **release** build (07), except explicitly unshipped cloud destinations
- [ ] Golden data AT-62 exact in en/fa/de
- [ ] Upgrade from every earlier test build keeps data (AT-60) – integration test for the first schema passes; re-check with real test builds
- [ ] Encrypted backup → uninstall → reinstall → restore gives identical balances (AT-57) – fresh-install integration test passes; device check pending
- [ ] No known critical bug in balances, conversion, double counting, restore or data exposure (Q-01)
- [ ] Performance measured on the reference device with 10,000 entries (Q-02) and recorded – calculation budget test with 10,000 entries passes

## Privacy and security

- [ ] Privacy matrix (06) reviewed against the release APK/AAB: package list, merged manifest permissions, network traffic
- [x] `INTERNET` permission removed if no online feature ships (D-20) – Android manifest declares only notifications and boot
- [ ] Privacy policy published at a stable URL on vafadar.pro, reachable in the app and in Play Console (PRI-03)
- [ ] Android Auto Backup disclosed (D-16); decision re-checked
- [ ] No financial data, notes, tokens or passwords in logs (SEC-04)
- [ ] Recent-apps preview and notifications hide financial data by default (SEC-02, REM-05) – notifications generic by default; preview hidden when the app lock is on (FLAG_SECURE); decide whether to hide it without the lock

## Store

- [ ] Data safety form completed from the matrix (PRI-04)
- [ ] Financial features declaration completed per current Play guidance (REL-01)
- [ ] Target API level, signing, content rating, target audience per current Play requirements (REL-02)
- [ ] Final app name, icon (placeholder replaced), store texts and screenshots in en/fa/de with fictitious data (REL-03/04)
- [x] Third-party licences listed in the app (Settings → About)
- [ ] Internal → closed testing → production track
