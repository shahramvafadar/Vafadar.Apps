# Lazy text normalization admission (D-748)

Privately investigate SearchText.Normalize's unconditional StringBuilder copy even
when the already trimmed/digit-normalized immutable text needs no Arabic-letter or
half-space mapping. Allocate only at the first changed character, retaining the
original Digits.ToAscii and initial Unicode Trim. Original per-row amount parsing,
EntrySearch predicate/callback order, deferred account/display-unit access, fields,
money/date/group rules and production source remain unchanged. This is a distinct
normalization premise, not a rescue of D747's rejected parse-once query.

Before execution, require at least5% median latency reduction for every one of the
numeric/missing/broad queries at both10k and100k, no allocated-byte growth and at most
64MiB pair peak-working-set growth. Six fixed alternating pairs follow one warmup
per side. A second process/native/product step requires every first-process gate;
no adaptive tuning, smaller-input rescue or relaxed gate.

Actual original and private candidate outputs match for all65,536 UTF16 code units
alone and wrapped, plus22 contextual inputs in en-US/de-DE/fa-IR:131,138 finite
comparisons. Preserve initial trimming, Persian/Arabic digits and separators, mixed
letter forms, combining/non-BMP/unpaired-surrogate values and converted endpoint
spaces. In particular, endpoint half-spaces map to spaces after trimming; do not
invent a second trim or idempotence contract. These are private comparisons, not
new permanent app-unit tests, speech or native-language acceptance.

Official .NET10.0.14/X64 reads the existing already-current-schema D740 fictitious
protected sources through original keyed/query-only factories. No migration, new key,
seed, financial or device operation occurs. Existing key bytes zero in finally;
cipher bytes, complete32-field source stamp, row references/order/multiplicity, day
groups and per-currency nets remain exact. Category callback uses the original Q02
benchmark's constant fictitious category. Database loading, native input/render and
cold/Home work are outside the measured Apply/materialization/original-ByDay boundary.

| Shape/query | Original median ms | Candidate median ms | Latency change |
|---|---:|---:|---|
|10k/numeric12.50|10.34445|9.62630|6.942370% better|
|10k/missing|10.13020|7.77580|23.241397% better|
|10k/broad|5.57810|4.86970|12.699665% better|
|100k/numeric12.50|49.29085|49.48565|**0.395205% worse: reject**|
|100k/missing|55.26725|43.78005|20.784823% better|
|100k/broad|53.85685|58.12540|**7.925733% worse: reject**|

Tenfold numeric allocation falls52,002,064-to2,401,944bytes; broad allocation falls
22,666,320-to6,666,184bytes. Largest pair peak-working-set growth13,201,408bytes
fits the memory gate. Reduced allocation does not waive the failed fixed latency
gates or prove exclusive CPU attribution. Close before second process, retuning,
production patch/tests/shipping build or native/phone work. No speedup is shipped;
the actual product normalizer and parser remain byte-exact.

Strict private build has zero warnings/errors. Six used private source files and
hashes are archived separately. Standard Clean succeeds. Automatic review rejects
the safer file-only supplementary removal before process creation, including its
exact-file-manifest preamble. No directory-tree deletion was requested. Conservatively
freeze these three roots/70files/1,329,084logical bytes without retry, subset, alternate
method, move, escalation or reuse; they are not reclaimed host free space:

- `artifacts/d748-lazy-normalization`
- `E:/Zanance.ReviewBuilds/D748-lazy-normalization-build/bin`
- `E:/Zanance.ReviewBuilds/D748-lazy-normalization-build/obj`

Used source/evidence outside those roots remains preserved. Main, owner PIN, terminal
D607 and other cohorts are untouched. Overall goal and broader performance/security/
stability/platform gates remain open; do not replay accepted D746 native queries or
this unchanged rejected premise. iOS stays deferred and TalkBack last.
