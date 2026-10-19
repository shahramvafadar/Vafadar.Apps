# Complete bounded CSV input and retained review (D-385)

## Observed defect and final behavior

The native CSV adapter previously decoded an unbounded source on the window thread. The original
view model cleared the previous source/review/result before parsing its replacement. A malformed
quoted tail could be salvaged as an incomplete financial row. Empty/header-only inputs also retired
the last complete review. Refuse these inputs before publication; preserve the original filename,
preview, import eligibility or already accepted outcome, and show explicit translated feedback.
A failed replacement never reactivates an already imported payload.

The original native picker stays on the window thread. Opening/reading its stream, BOM-aware decoding,
parsing and original ledger/holding analysis run on workers. Prepare all review data before publishing.
Existing store queries and algorithms are retained; this is not a new single-transaction read snapshot.
Recheck the owning profile/path and retirement epoch before publication. An independent interpretation
epoch guards generic mapping changes both during worker reads and across final profile validation;
an older sign/date/account interpretation requires explicit Refresh rather than being published as current.

## Technical budgets, compatibility and privacy

CsvReadLimits.Import is independent of commercial quotas. Defaults: 64 MiB actual source bytes,
64 Mi decoded UTF-16 characters, 250001 retained rows including header, 1024 columns per row,
4000000 cells including blank-record work and 65536 original characters per field. Exceeding any
budget refuses the entire source without truncating a financial record. Actual reads count even
when Length is unavailable or false; at most one excess sentinel byte is requested. Leave the
caller-owned stream open. Cancellation/provider failures propagate without replacing a complete review.

Retain UTF-8 fallback and BOM detection for UTF-8/16/32 in both byte orders. Fragmented providers
may return one byte per read; coalesce the initial four bytes within the byte budget so StreamReader
can identify the complete BOM. Quoted Unicode, embedded newlines, escaped quotes, separator detection
and existing export/formula neutralization stay intact. Unterminated quoted records now refuse the
whole source. The existing StarMoney byte/row restrictions remain stricter; bank consent and retirement
behavior are unchanged, and generic retained-review feedback is not a new bank acceptance claim.

No schema/compiled model, financial posting rule, SDK, network permission, security setting or stored
CSV cache changes. Selected text stays in the existing transient review flow; no new archive or raw
input logging is introduced. Sharing/export, Undo and aggregate relationship policies remain unchanged.

## Focused managed qualification

There are 46 new executed cases: 26 Core and 20 App, with every case's final result passing across
retained selective runs. This is not a repeat of the full suite. Twenty existing CSV/export cases and
94 related App/bank cases pass; six affected mapping cases are rerun after the late-publication guard.
The six-language feedback theory contains 36 limit checks, not 36 separately counted test cases.

Preserve initial failures: the old malformed-file regression loses its filename; four empty/header-only
cases also fail before the final guard. Initial stream qualification has four fragmented UTF-16/32 BOM
failures and one large-fixture oracle failure. Correct the real BOM handling and rerun only 13 affected
stream cases. The 100000-row export sorts by Date/CreatedAt, so input-order ID comparison is an invalid
oracle. Correct it to exact CSV reserialization plus unique-ID/complete mapped-field comparison; the
final complete producer/stream/parser/own-preview case passes in 1.651s. Do not call that whole-app speed.
Three additional stream cases cover cancellation during BOM coalescing, provider I/O and false Length.
Actual-source App cases also cover ledger/holdings retention, completed outcomes, preparation failures,
retired forms, 100000-row paging and generic changes at both asynchronous publication boundaries.

## Running original pages and final binaries

The original Windows page/command qualifies 126 refusal checks across en/fa/de, Light/Dark and
360/412/1280 px, with process-local font stress at 200 percent. A separate empty-only continuation
qualifies 18 new error surfaces without replaying those 126 successes. All complete native captions,
retained reviews and stored fictitious inputs match. The fresh private non-input desktops restore the
original desktop; images of only the application's own window are inspected. This is layout evidence,
not spoken-reader or real OS-scaling acceptance.

The independent original Release/AOT x64 page qualifies 42 refusal checks across six translated/theme
contexts. The empty-only final-source continuation adds six checks; complete native TextView geometry,
original command invocation, complete review/data and theme/language restoration pass. File-selection
ports supply only the selected source/error, so this does not establish the native provider adapter.
The independent default-GC process is stopped only after its terminal report; Main/OS are restored.

Final strict Windows Release: 21.34s, zero warnings/errors; independent final Debug: 43.72s.
Independent final Android Release/AOT: 95.94s, zero warnings/errors. Complete offline main APK:
`artifacts/android-csv-input-d385/pro.vafadar.zanance-Signed.apk`, SHA256
`e9448624cb9a60d60ec8fae4b5fbcf3d333f0fbc6b7e335a01aa0ed0d828a45f`, 94867189 bytes,
384 AOT libraries, API24/36, ARM64+x64, no Internet/debug/profile permission. Certificate SHA256
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` matches prior signed packages.
All eight engine libraries remain byte-identical; ordinary concurrent major GC is unchanged.

The first complete candidate (068ffada...) is retained separately. It includes the earlier valid,
limit and malformed branches but precedes the empty guard. Final no-reset upgrades preserve original
meaningful Home and exact OS: emulator11605 to12404, Samsung20937 to24043, Home0 and979EUR respectively.
Do not conflate earlier source-bound qualification with all checks executed on a single final APK.

## Original physical picker continuation

Coordinate an exclusive Samsung turn and install the final signed bytes without reset. On Main's
existing fictitious account, use the original native picker to select one controlled27-column own
export (495 bytes). Original review shows its unique title, account and125.43EUR expense, one new row,
zero known and zero invalid rows; do not invoke Import. Then select an unfinished quoted source,
64MiB+1-byte source and empty source. Every actual adapter refusal retains the valid filename,
complete review count and explicit eligibility; complete expected feedback is visible in native nodes.
The first lookup is in the indexed Downloads provider and returns no matching file. Use the observed
physical storage root/Download/owned directory instead; never retain unrelated filenames/accounts.
Two early observation guards find a transition or failed adb read; preserve the same process and
continue authoritative observation, without replaying selection or restarting. No runtime fix is claimed.

All four actual source selections qualify on the final APK in PID24043. Saved-observation assertions
are not additional unit tests. The complete meaningful visible Transactions projection exactly
matches its original baseline. Final meaningful Home matches exactly with979EUR, unchanged APK and
exact OS. No Import/Undo, transaction/profile/account or system-setting writes. This is native UI
preservation, not full private-database equality. Hash-check four controlled input files against their
original manifest, remove only them and the empty owned directory, and release the phone turn.
Artifacts/phone-csv-input-d385 retain controlled inputs, redacted picker controls, original page
hierarchies, final assertions and restoration. Real bank specimens, arbitrary native providers and
all-phone acceptance are not inferred.

## Scope and scheduling

Local artifacts/csv-input-review-d385 retain builds, immutable packages, test XML, negative results,
original native receipts, package proofs and exact restoration. Windows runs are
8653e96d7a424d72ad067cb31d38a1eb and f3845d1227504d318109a2b7a08b6d5f.
Independent Android run25b9dab64bf849fcb24e39246d773215 retains original and empty-only comparisons.

Arbitrary bank specimens, all hostile/provider inputs, total import memory/latency, full device
acceptance, iOS and publication remain open. D-386 schedules dedicated native ANR/crash and Android
accessibility work after remaining approved development; this batch does not reopen those tracks or
claim they are fixed. Existing successful financial/crypto/backend suites are not replayed.
