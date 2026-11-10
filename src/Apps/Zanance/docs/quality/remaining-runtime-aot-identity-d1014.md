# Remaining installed-runtime managed/AOT identities — D-1014

The four remaining public runtime-pack names agree with their native AOT identities
in both architectures of the current complete package. This completes the finite
installed-runtime-pack/native-module intersection after [D1013](public-system-aot-identity-d1013.md),
without decoding its138completed pairs again. It supplies no native-fault remedy.

## Inventory and exact boundary

Each installed official10.0.14Android runtime pack's lib/net10.0directory contains
171DLLnames. Intersecting these names with the current APK's actual native AOT
members produces73names per ABI. The previous completed69System-framework names
are a subset; exact set subtraction produces only these four names:

| New public module | ARM64 decoded bytes | x64 decoded bytes | Managed MVID/native AOT GUID |
| --- | ---: | ---: | --- |
| Microsoft.CSharp.dll |229888|229888|edd687ab-94cd-4f4a-b195-ddb22e0550a5|
| Microsoft.Win32.Primitives.dll |5120|5120|a0f914f2-d5fe-40cf-ba30-20be611a62ce|
| mscorlib.dll |4608|4608|fc8018ae-1990-41a9-b5cd-deb886acd8f8|
| netstandard.dll |8192|8192|de5e4cd7-2baf-41ae-86c3-fea73173b092|

All eight new pairs pass. Each ABI decodes247808public PEbytes. Combining these
new results with the unchanged prior terminal proof covers146pairs total,73per
ABI; this is evidence reuse, not146new executions. The206-entry APK store also
contains assemblies outside this installed-runtime-pack intersection. Their images,
application code/configuration, resources, keys and live/private data are not read.
CoreLib is outside this lib/net10.0allowlist; prior separately scoped CoreLib
evidence is not silently extended to the current package.

## Independent bounded comparison

Reuse the D1011complete offline APK,99614623bytes, SHA256
`9ef33c3aea965e5027e1b51958df2ddf7e5f4115d24e73830eac53efef5ce937`.
The source-declared format187AOT header remains pinned to the same public source
hash and GUID/name offsets192/264. Named ELF objects, architecture-relative
relocations, allocated bounds and exact assembly names precede GUID comparison.
The original metadata reader definitions are reused, without old controls or old
APK execution. Store/name/index metadata is read; only the eight selected public
managed payloads and their native AOT members are decoded.

The wrapper and independent host reader both enforce the exact four-name allowlist.
The declared wrapper bounds are four pairs/ABI,4MiB/image and8MiB/ABI. The installed
SDK's independent LZ4decoder and host Module metadata reader agree on every new
module name, complete decoded-image SHA256, byte count and MVID. Every managed MVID
equals its named native AOT GUID. APK identity remains exact before/after the
comparison and after standard Clean. No decoded target PEis written or executed.

Before first execution, review catches the copied initial wrapper's incompatible
immutable-buffer cleanup and restores the final original reader's lifetime-only
cleanup. No pair had run. The single issued traversal and independent host exit0;
there is no failed new pair to retry. Host arrays clear, while Python immutable
buffer lifetime is not a claim of complete-memory erasure.

One strict private host-reader build and one standard own-output Clean pass with
zero warnings/errors. Twelve host project/build-residual files219250bytes and one
scoped host-tmp file1075bytes remain. Used source, original inventories and terminal
results remain. No supplemental/frozen cleanup, product/Windows/Android build,
successful unit test, device/ADB action, financial write, authentication or original
crash workload is repeated. No product code, model, runtime or GC setting changes.

## Previously completed requirements remain completed

The independent actionability review also finds that these are existing scoped
results, not reasons to recreate their cohorts:

- [D521](android-storage-runtime-compatibility.md) already records actual API35/x64
  page size16384and the five original encrypted-storage cohorts. Static alignment
  alone is insufficient, but that document contains actual runtime evidence.
- [D574](loaded-cold-command-boundary.md) already records physical ARM64 original
  cold Home at10k/100k:8062.511917/17756.488064ms. These are slow measured results,
  not two-second acceptance and not a reason to replay their accepted preparation.

Neither finite runtime inventory equality nor these earlier results establishes
every dependency, the historical faulting deployment, actual executed engine,
SourceGen correctness, original SGen fault cause/remedy, whole performance,
security/accessibility/platform/provider/Store or release acceptance. RetainedQ02,
terminal classification and all frozen scopes remain untouched. Main10742/9ef33c3a
is only the last D1012observation, not fresh liveness. Canonical parent States and
the complete delivery goal remain unchanged and open.

The single D1013response reports [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42802644160)
in_progress/no conclusion and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42802644280)
completed/success. It was observed after the requested15–18minute window; no timely
check claim or second request follows.
