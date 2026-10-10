# Complete unchanged transaction source reuse (D-110 / AT-115)

## Observed cost and correction

A bound Windows transaction page with 1,000 date groups rebuilds its native grouped source for every equivalent
whitespace search despite retaining the exact complete result. Independent fictitious 10,000 / 100,000-entry
fixtures run in three fresh processes per shape and phase, with six equivalent inputs per process. Complete ids,
order, group count, actual MAUI/native sources, realized row ids and unchanged complete stored rows are checked.
Changed Expense/All results must publish a new native source and restore every original entry.

Retain Days only when all group counts, ordered boundaries, complete Header/NetText captions and every ordered row
object match. Compare object identity rather than financial ids or value equality: fresh data/display snapshots
must publish new rows. Existing observable selection still updates the same real row objects. Filtering, grouping,
summary, result totals, fresh reads, bulk selection, ledger calculations and financial validation remain unchanged.
No result cap, schema, SDK, permission, security, portable format or resource-string change.

The median publication stage falls from 36.56 to 0.38 ms for 10,000 entries, and from 407.51 to 1.61 ms for 100,000
entries (18 samples per shape/phase). The candidate stage includes the complete identity comparison. These warm
measurements end at synchronous source publication; they exclude filtering/group construction, later arrangement,
painting, cold start, physical devices and ANR acceptance. A real changed result still incurs native publication.
All original developer database/WAL/SHM files return with matching hashes in both phases. Temporary probe code and
its early route are removed before final builds. The first cleanup helper used an incorrect preserved-file path;
its failure is retained, corrected without repeating measurements and is not acceptance.

Pinned native implementation references: MAUI 10.0.110
[GroupedItemTemplateCollection](https://raw.githubusercontent.com/dotnet/maui/10.0.110/src/Controls/src/Core/Platform/Windows/CollectionView/GroupedItemTemplateCollection.cs) and
[TemplatedItemSourceFactory](https://raw.githubusercontent.com/dotnet/maui/10.0.110/src/Controls/src/Core/Platform/Windows/CollectionView/TemplatedItemSourceFactory.cs).
The measured application sources, not historical framework issues, establish the cost in this revision.

## Final verification

Nine behavior cases exercise the complete 100,000-row identity, mutable selection, empty results and changes to
captions, group/row count, ordering and fresh equal-id rows. Main suite: 1,521 passing, zero failed/skipped;
App.Tests 285. Test outputs are cleaned. The retained actual Windows transaction review checks equivalent bound
MAUI/native sources and fresh native publication after data reload/display-unit changes without Save.

Final six native Windows contexts (en/fa/de, light/dark, 360/normal text) pass with 186 own-window renders.
Equivalent search retains actual bound MAUI/native sources; display-unit replacement and fresh data reads publish
new native sources. Every original developer database/WAL/SHM file returns with matching hashes. Strict Windows
and canonical complete Android Debug/Release builds pass without warnings/errors. No layout or font change;
D-108's separate 21-context geometry evidence is retained rather than counted as new checks.

Normal signed Release on the owned emulator changes kind, restores the complete original result, enters whitespace
in the real SearchBar and clears it through the native target without Save. Its first helper incorrectly expected
an EditText; the actual hierarchy identifies AutoCompleteTextView/search_src_text. Corrected native checks pass;
the failed helper folder is negative evidence. All 24 tables in each of three original fictitious profiles match
their complete baseline exactly. Original English/System/Advanced, density 420/font scale 1.0, Home-stopped and
foreground SECURE remain. Debug comparison package is never launched; final complete Release is reinstalled.

APK: artifacts/android/zanance-d110-release.apk, 80,926,635 bytes, SHA-256
5b1f67b590f5d49ed0ec12e52650862ee425b511981b44dbc301c2d2c5542eb5. Package pro.vafadar.zanance, 0.1.0/code 1, min 24/target 36,
non-debuggable, full ARM64/x86_64 assembly stores/app AOT, ZIP integrity and v2/v3 signatures pass. Local test
certificate SHA-256 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b is not production signing approval.
Final evidence: artifacts/unchanged-transactions-delivery-proof.json, unchanged-transactions-final-native-proof.json,
unchanged-transactions-apk-proof.json, unchanged-transactions-release-native-corrected/proof.json and
unchanged-transactions-all-financial-proof/unchanged-proof.json. CI is one separately delayed exact-commit check.

Measurement evidence: artifacts/unchanged-transactions-before-after.json and the baseline/candidate folders.
Physical phone/iOS, cold/ANR, real OS/readers/provider, product/release and owner acceptance remain separate gates.
