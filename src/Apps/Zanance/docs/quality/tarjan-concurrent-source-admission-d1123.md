# Tarjan concurrent-collector source admission - D1123

## New upstream premise

The [upstream report123537](https://github.com/dotnet/runtime/issues/123537)
now includes a6November2026 maintainer comment pointing to
[proposal137047](https://github.com/dotnet/runtime/pull/137047). The comment describes
a possible interaction between Tarjan bridge processing and concurrent marking;
it does not establish a reproduced customer repair. The selected live PR metadata
is open/unmerged, head `36d4b9143211a69e50cca4a8f2e4b348fccaeee9`, with no merged_at.
A computed merge_commit_sha in an open response is not evidence of a merged fix.
This is a new source premise after [D911](public-concurrent-vector-correlation.md),
not another original register classification or reproduction attempt.

The public proposal changes one predicate and its explanatory comment in
`src/mono/mono/metadata/sgen-tarjan-bridge.c`. It calls
`sgen_is_object_alive_for_current_gen` instead of `sgen_object_is_live` when
an object has no existing ScanData. The complete proposed file reconstructs the
complete original file after reversing precisely those two changed lines. No
other algorithm change is inferred from this finite whole-file comparison.

## Exact original and serviced source

The original admitted VMR is `0427b9abbead3fa64c6655eb4290c537397131fa`.
The current serviced runtime packages identify VMR
`8e0f8ed2312954727f8919d7d1c2f78b483b4ee7`. Actual local ARM64/x64 runtime10.0.15
nuspec repository metadata independently names `https://github.com/dotnet/dotnet`.
[D1080](mono15-signed-servicing-inputs-d1080.md) previously called this a
dotnet/runtime source commit; that repository label was inaccurate. The version,
commit value and earlier signature/package qualifications are not changed.

The first public fetch used the wrong repository for that serviced commit and
returned404; no serviced source was accepted from it. One subsequent guessed
header name returns404 at both VMR commits. Both refusals remain explicit. Correct immutable VMR
paths follow the actual repository metadata and original include declarations.
No source fallback is treated as matching without complete byte comparison.

| Complete public input | Original versus serviced15 |
|---|---|
| metadata/sgen-tarjan-bridge.c | Exact; SHA256 `26d40ac22ddf8cceeb5d890bbfa489a4d6cb9a7729fcc03d1c51b629250d2951` |
| sgen/sgen-gc.c | Exact; SHA256 `08d7ab89df3a3e1d39302899ada025b56d382c30bb05590afbc9d7e8852913d5` |
| sgen/sgen-gc.h | Exact; original generation-aware helper already exists |
| sgen/gc-internal-agnostic.h | Exact; separate public declarations |

Both source versions retain the old predicate. Thus the premise that this exact
proposed Tarjan correction is already present in current15 source is disproved.
This is package-declared source correspondence, not proof of every compiled
instruction, actual bridge selection, collector interleaving or product fault cause.

## Predicate distinction and original mutation

The original exported old predicate delegates to the current-collection helper
in sgen-gc.c. For an object outside the nursery during a nursery collection, it
returns false. The proposed existing helper in sgen-gc.h returns true for that
same condition. Both otherwise defer to the corresponding nursery/major liveness
predicate. Existing registered ScanData continues to bypass this new skip test.

A16-row abstract Boolean evaluation enumerates nursery membership, collection
generation, relevant liveness and registration. Only two rows differ: unregistered
old-generation objects during a minor collection, regardless of major liveness.
These are source-derived logical controls, not collector execution, product tests,
real object classification or a decoded original descriptor.

In both original and current15 Tarjan files, create_data saves the existing header
bits/lock word, tags the header and stores ScanData in the lock word. The new guard
prevents reaching that operation for the newly skipped objects. This explains the
public proposal's mechanism at source level. It does not prove that an actual
object from this project's crash reached create_data, remained concurrently
scannable, or produced the bad reference subsequently read at the admitted fault PC.
The [D908 exact reference load](new-accounts-field-word-provenance.md) remains
distinct from a claim that a tagged vtable caused that reference value.

## Preserved execution and delivery boundaries

No custom runtime is compiled, patched, installed or substituted. Runtime/GC
configuration, SDK and project code are unchanged. There is no app launch,
register/descriptor/map/memory read, signal, Q02/D923/frozen access, device action,
authentication, financial operation, old test/benchmark replay or cleanup.
The existing C7 AVD remains off with its completed D1122 observer stopped;
Main27545/e551e129 is only the dated D1121 observation.

Public source bytes/hashes, selected PR metadata, actual package metadata hashes,
whole-file inverse proof, Boolean rows and fetch refusals remain under
`artifacts/d1123-tarjan-concurrent-source-admission`. The authoritative maintainer
proposal is still unmerged at this observation; installing it or choosing a new
runtime/collector is not silently authorized by source similarity. A future
cause-specific runtime trial needs a separately concrete, bounded admission and
unchanged correctness/performance/source-preservation gates. No such trial or
accepted remedy is claimed here. Original fault, wider performance/security,
accessibility/platform/provider/release requirements and the full goal remain open.
