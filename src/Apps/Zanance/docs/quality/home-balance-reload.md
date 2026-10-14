# Original Home balance refresh (D-267)

## Observed cause and final behavior

D-266 diagnostics retained a Windows COM 0x8000FFFF failure from ContentViewHandler.DisconnectHandler during Balances.Clear in the original Home load. Unlike shell retirement, this ordinary covered-page reload still owns the active Home. Clearing first publishes an empty list and disconnects every AmountReadout; subsequent Add constructs new native trees. Home now uses the established complete SnapshotCollection replacement for currency totals. Existing same-count BindableLayout rows are rebound without handler disconnection. Financial calculation, inclusion policy, MoneyText formatting and retirement cancellation are unchanged.

## Final affected evidence

The independent marked Windows installation ran actual Home, not duplicate calculations or fake controls. Eighteen contexts cover en/fa/de, light/dark, 360/412/1280 at process-local 200% typography. Thirty-six explicit original reloads include a real modal cover and ordinary Home. Native AmountReadout handler identities and full formatted values remain current; no empty intermediate balance snapshot is published. The complete typed financial fingerprint remains a1f00084323125f47bc4cf0f3288df5bddc8b0da8c162172070b04efae3564f9. All 18 own-window captures and 360 native caption observations complete without error.txt. This verifies the observed refresh path, not every possible native lifetime race.

The actual source-built SnapshotCollection is unchanged; the 3,526-test suite passed immediately before this batch (D-266, zero failures/skips). HomeViewModel is a MAUI source and is covered here by original running-app assertions, not that managed suite. Unchanged domain/native producer tests were not repeated.

Canonical signed Release: artifacts/android-home-balance-final/pro.vafadar.zanance-Signed.apk; 88,656,368 bytes; SHA256 5fec48b13f5fe4b8155f014e8156d6d0a51e899adabddf8c1d9ec36dfc329376. Permanent package, API24/36, arm64/x64; v2/v3 local test signature. Both assembly stores are embedded, reviewed native cipher hashes match, and no plaintext SQLite engine is packaged. The script rejects warnings. Owned Android emulator upgrade/cold Home and actual More/Home return preserve the recorded amount and complete UI; no financial entry or settings write is performed.

Physical devices, native OS scaling/spoken accessibility, iOS, OAuth and publication remain separate gates. The full development goal remains active.

Final permanent-identity Windows strict build: zero warnings/errors, 20.92s. Earlier identity-only review build required touching the unchanged Windows manifest to invalidate stale package metadata; the reviewed and final generated identities were verified explicitly.
