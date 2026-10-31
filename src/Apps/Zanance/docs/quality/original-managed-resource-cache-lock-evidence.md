# Exact managed resource-cache lock evidence

## D-682 original failure and admission

This is a read-only continuation of D-678's actual PID4527/UID10216 managed exception,
not its later PID10110 native fault and not a new reproduction. The original bounded
stack names `Dictionary<string,ResourceLocator>.FindValue/TryGetValue`,
`RuntimeResourceSet.GetObject/GetString`, `ResourceManager.GetString`, Translator and
Home construction. The dictionary reports concurrent operations; that description
does not identify an application race, exclusive runtime defect or managed-reference
corruption origin.

The exact already-used signed APK SHA256 is
`adc8a82af3a626af3cdc8e10995721fb39190b94718a53352d8d4bf5dc785937`.
Its original x64 assembly-store ELF payload and bounded XALZ/LZ4 image are decoded
without starting the app. Only the original CoreLib managed image is written; no app
secret, private database, register, process memory or other personal data is extracted.
That image is2495488bytes, SHA256
`67bc3a835e66fd20a88d080f5f2e985b5a070cfc6ad1fe6e602e55f6d3086c88`,
MVID`6c6da183-caf7-4d1a-9fe8-4e42ea888ec3`. Its actual public informational version is
`10.0.14-servicing.26522.126+0427b9abbead3fa64c6655eb4290c537397131fa`.
This binds the observed managed image, rather than assuming a current build-directory
copy or declaring every linked BCL image identical across applications.

## Actual packaged IL, not an upstream-only assumption

The394-byte original `RuntimeResourceSet.GetObject` body contains these two lookup paths:

| Monitor.Enter IL offset | TryGetValue IL offset | Finally handler | Monitor.Exit IL offset |
| --- | --- | --- | --- |
| 53 | 62 | 169..180 | 175 |
| 227 | 308 | 377..388 | 383 |

Both lookups and preceding Enter calls lie inside their actual protected regions;
their Exit calls lie inside the matching `finally` handlers. The exact packaged
managed method therefore does contain the synchronization that the earlier upstream
reference described. Adding another Translator lock merely because the original error
mentions concurrency is not justified by this evidence. No exception is swallowed,
translation fallback introduced or resource/runtime/collector code changed.

The trimmed parameter metadata table has zero rows. The initial private report retains
that actual observation; method selection is corrected by reading the original method
signature, which has three parameters. The inspection host is actual.NET10.0.12 reading
the original runtime14 PE metadata/IL, not executing that target CoreLib. Both small
private builds are strict and warning-free; product test count does not increase.

The original AOT ELF exposes no matching GetObject/GetString function symbols in its
local symbol tables. No native offset is mapped to an IL offset, and the executed
machine-code lock, actual reference origin, another cache writer or an exclusive cause
is not established. The exact package-repository [reference source](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/libraries/System.Private.CoreLib/src/System/Resources/RuntimeResourceSet.cs)
is supplementary to the actual managed-byte evidence. D-679's SGen operation remains
a separate native-fault observation; this report does not join their causes.

## Preservation and limits

The original signed APK, used decoded CoreLib, public code identities, bounded JSON and
inspector source are retained. One standard Clean succeeds. Automatic approval review
rejects supplemental removal of the three fresh `bin`, `obj` and duplicate AOT targets
before process creation:11 files/3013350 logical bytes remain. The stated reason is
`blocked by policy`; no deletion happens. These exact scopes are frozen without retry,
subset, alternate method, move, escalation or reuse.

There is no device/emulator run, native signal, build of an old app, financial setup,
successful-test replay, runtime/GC/package replacement or product change. Samsung,
D-607's three original deadlines, consumed reboot and unread source remain protected.
Managed/native stability and all broader device, performance, accessibility, iOS,
provider, Store and release gates remain open.
