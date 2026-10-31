# D-688 - Original managed resource-cache writer and alias inventory

## Outcome and original input

Continue only the missing other-writer/alias question from D682, using the exact
retained D678 PID4527 CoreLib managed image. This is neither a new reproduction nor
the separate later native fault. The packaged image remains2495488bytes, SHA256
`67bc3a835e66fd20a88d080f5f2e985b5a070cfc6ad1fe6e602e55f6d3086c88`,
MVID`6c6da183-caf7-4d1a-9fe8-4e42ea888ec3`. Its previously bound original APK is
`adc8a82af3a626af3cdc8e10995721fb39190b94718a53352d8d4bf5dc785937`.
The target managed image is read as PE metadata/IL; it is never loaded for execution.

Every16900 available managed body is decoded with bounded operands. The actual
three cache fields and concrete `Dictionary<string,ResourceLocator>` call sites
select ten methods. Concrete generic signatures are decoded, so an unrelated
dictionary is not silently included. This broadens the original single-GetObject
inspection without rerunning app/native/rendering/financial success.

## Actual field assignments and constructor alias

| Method | Field | Assignment IL | Value |
|---|---|---:|---|
| ResourceReader constructor | _resCache |8| Original dictionary argument2 |
| ResourceReader.Dispose | _resCache |10| null |
| RuntimeResourceSet constructor | _resCache |18| New dictionary |
| RuntimeResourceSet.Dispose | _resCache |38| null |
| RuntimeResourceSet.Dispose | _caseInsensitiveTable |45| null |
| RuntimeResourceSet.GetObject | _caseInsensitiveTable |298| Captured local table5 |

The RuntimeResourceSet constructor loads its exact cache field at IL26 and passes
it to ResourceReader's original constructor at32. That constructor's actual signature
is Stream, Dictionary<string,ResourceLocator>, Boolean; `ldarg.2` at7 is stored in
its cache field at8. These are the same ordinary managed dictionary reference,
not two independent caches assumed from matching field names.

No other direct field assignment or field-address load is found in these available
managed bodies. Initial private audit arithmetic expected seven assignments; the
unchanged inventory actually contains the six exact method/offset pairs above.
Preserve that observer failure and correct the explicit inventory, without rerunning
the original app or weakening source identity.

## Concrete dictionary operations and matching locks

| Method/cache | Operation IL | Monitor.Enter IL | Protected range | Monitor.Exit IL |
|---|---|---:|---|---:|
| RuntimeResourceSet.GetObject, primary | set_Item154 |53|49..168|175 |
| RuntimeResourceSet.GetObject, case-insensitive | Add281 |227|223..376|383 |
| RuntimeResourceSet.GetObject, case-insensitive | set_Item370 |227|223..376|383 |
| ResourceEnumerator.get_Entry, primary | TryGetValue129, read only |110|107..145|150 |

The primary writer uses local1 loaded from the cache field; lock local6 is copied
from that same local1. The case-insensitive writer and lookup use captured local5;
the second lock local6 is copied from local5. Each Exit is in the corresponding
finally handler. Enumerator Entry captures the reader cache into lock local2, then
reads the reader cache for its lookup. It contains no Add/set_Item/Remove/Clear/TryAdd
call. Other selected reader methods perform constructor/closed-state checks or value
reads rather than direct dictionary mutation. Disposal clears references; it is not
classified as dictionary-entry mutation or a proof of safe concurrent disposal.

The original GetObject read locks from D682 are preserved. This inventory supplies
no ordinary direct unguarded dictionary writer in the inspected managed scope.
It does not establish the executed AOT lock, exclude reflection/native/reference
corruption, prove a race-free runtime or identify the exclusive original cause.
No Translator lock, swallowed exception, translation fallback, runtime/GC switch or
product patch follows merely from the original concurrent-operations message.

## Reference and preservation

The actual packaged IL is primary. The exact package commit's
[RuntimeResourceSet reference](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/libraries/System.Private.CoreLib/src/System/Resources/RuntimeResourceSet.cs)
and [ResourceReader reference](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/libraries/System.Private.CoreLib/src/System/Resources/ResourceReader.cs)
are supplementary, not proof of executed native instructions. Original raw files,
hashes and line extents are retained; extracted web line numbers are not substituted
for raw-file numbering or a binary-to-source identity claim.

Two small strict inspector builds pass without warnings: direct field inventory,
then the expanded concrete typed-call/signature boundary. Both read-only executions
return0 on actual.NET10.0.12 while inspecting the original runtime14 image. These
are not new product tests. One standard Clean succeeds. Used source/JSON/raw-reference
evidence remains; no supplemental deletion, refused cleanup scope, APK/native app,
device, heap/register/private database/key or financial command is touched.

The next causal admission still needs an actual reference-origin/failed-condition
input or a verified official correction for the exact executed path. D607's three
deadlines, consumed reboot and unread post-reboot source remain protected. No general
stability/security/performance/accessibility/platform/provider/Store/release gate closes.
