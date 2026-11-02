# D-781 - Original interpreter small-integer return admission

## Concrete question and result

2 November2026. Continue independent native-fault investigation without replaying
original UI, financial preparation, authentication or a successful diagnostic cohort.
The previously unexamined [issue115859](https://github.com/dotnet/runtime/issues/115859)
reports an AOT/interpreter small-integer return-width problem on Windows/x64.
Its explicit correction is [PR117336](https://github.com/dotnet/runtime/pull/117336),
merged as [413d5b8446198557771e1548e72c6af505786115](https://github.com/dotnet/runtime/commit/413d5b8446198557771e1548e72c6af505786115).
Do not substitute the issue author's tentative blanket-clear workaround for the
actual final correction and its storage-width exception.

The complete corrected helper and both caller functions are already exactly equal
in the original package's retained VMR source. This specific missing-correction
premise is closed at source admission. No runtime replacement, application workaround,
new reproducer, build, test or native execution follows. It does not establish the
original failing method's execution engine or repair the original reference fault.

## Immutable inputs and exact comparison

Reuse only the safe retained original interp.c outside every frozen cleanup scope.
Revalidate its existing SHA256 before comparison; do not fetch or reconstruct a
frozen artifact. Original VMR is0427b9abbead3fa64c6655eb4290c537397131fa, and the
[original public interpreter source](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/interp/interp.c)
is303077bytes, SHA256
1f47513497020c60a404bfce7881d2847a98fad40c5b98660c55f1cd2a832578.
The newly retrieved immutable corrected interp.c is302965bytes, SHA256
5c83bd0049e7e81830d1813eb83e5c3410e7a32f73d48dc08384ffedd2f88659.
The full files differ; no full-file identity is claimed.

Extract complete C functions by balanced braces while excluding comments, escaped
strings and character literals from depth tracking. Compare complete extracted UTF-8
function text, not a keyword hit or whitespace-normalized fragment. Function byte
counts and hashes below exclude the preceding return-type line; line numbers identify
the function-name line in the exact original source.

| Complete function | Original line | Extracted bytes | SHA256 | Equal to corrected commit |
| --- | ---: | ---: | --- | --- |
| stackval_to_data_sign_ext |989|845|fcd2213558ddd8e159f5c11f62ad0f06f41615dae2d2566feaebc1c537d6e88d| Yes |
| interp_frame_arg_to_data |1689|673|f5c6bd6f7be8008ee224758c68791b6207cbdbf721b95025b63466081d24c224| Yes |
| interp_entry |2300|3340|5e33faa7cab1b837df0aa229a92b7a8b03983dc1de8b1e3e9c235d21aae23955| Yes |

The helper handles signed/unsigned8/16-bit values and, on64-bit targets, signed/
unsigned32-bit values. Its callers use native-width return storage where appropriate.
The final interp_entry exception retains the ordinary conversion for the narrow
actual-type gsharedvt wrapper; unconditional widening there could overrun that
return storage. Both branches and the complete surrounding functions match the
immutable correction. No speculative blanket clearing is introduced.

## Limits, failed lookup and preservation

Search-result excerpts initially carried inconsistent dates for the old issue.
Use the actual issue/merged-commit pages and immutable source, which identify the
2025 correction; do not describe it as a newly released2026 fix. An attempted local
quality-report filename lookup did not exist and yielded no source evidence; the
actual D771 pointer and recorded original-source receipt provide the verified input.
No product behavior is inferred from either lookup failure.

This is source-level admission. All paths in the native binary have not been newly
disassembled or executed. Presence of the correction does not prove that an original
frame traversed either interpreter return path, exclude every integer/reference
corruption, identify the bad-reference writer, or establish general runtime safety.
The original array-scan fault and two-second performance target remain open.

Only local diagnostic source/receipts and this documentation change. Product,
compiler/runtime/GC/SDK, test count, financial model and schema remain unchanged.
No physical/virtual device command, input, authentication, install, camera, source
seed, monetary write, old successful test/build or declined performance rescue is
performed. No cleanup or frozen-scope access is attempted. The new small public
source and exact-comparison proof remain outside all frozen scopes.

D752 stays pending genuine owner participation; D749/D751/D607 remain terminal.
The overall goal remains active, with independent work continuing and platform,
security/accessibility, provider, Store and release gates still separate.
